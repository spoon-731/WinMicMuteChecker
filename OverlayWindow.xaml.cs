using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Rectangle = System.Drawing.Rectangle;

namespace WinMicMuteChecker
{
    internal static class NativeMethods
    {
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_LAYERED = 0x00080000;
        public const int WS_EX_TOOLWINDOW = 0x00000080;

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
        private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
            => IntPtr.Size == 8 ? GetWindowLongPtr64(hWnd, nIndex) : new IntPtr(GetWindowLong32(hWnd, nIndex));
        public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
            => IntPtr.Size == 8 ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong) : new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
    }

    public partial class OverlayWindow : Window
    {
        private const double margin = 10;
        private const int WM_DPICHANGED = 0x02E0;

        public OverlayWindow()
        {
            InitializeComponent();

            // Se non hai già legato OnLoaded da XAML, scommenta:
            // Loaded += OnLoaded;

            // Hook al WndProc per intercettare WM_DPICHANGED
            SourceInitialized += (_, __) =>
            {
                var src = (HwndSource)PresentationSource.FromVisual(this);
                src.AddHook(WndProc);
            };

            // Se l'overlay cambia size dinamicamente, riposiziona
            SizeChanged += (_, __) => UpdatePosition();
        }

        // on overlay loaded - set properties
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;

            // Click-through + layered + no taskbar
            var exStyle = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE).ToInt64();
            exStyle |= NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_LAYERED;
            NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE, new IntPtr(exStyle));

            UpdateOverlay(); // imposta colore, posizionamento e opacità
        }

        public void ShowOverlay()
        {
            overlayWindow.BeginAnimation(Window.OpacityProperty, null);

            overlayWindow.Opacity = 0;
            overlayWindow.Visibility = Visibility.Visible;

            var animation = new DoubleAnimation(SettingsManager.Opacity, new Duration(TimeSpan.FromMilliseconds(120)));
            animation.Completed += (s, e) => {
                overlayWindow.Visibility = Visibility.Visible;
                overlayWindow.Opacity = SettingsManager.Opacity;
            };

            overlayWindow.BeginAnimation(
                Window.OpacityProperty,
                new DoubleAnimation(
                    SettingsManager.Opacity,
                    new Duration(TimeSpan.FromMilliseconds(120))
                )
            );
        }

        public void HideOverlay()
        {
            overlayWindow.BeginAnimation(Window.OpacityProperty, null);

            overlayWindow.Opacity = SettingsManager.Opacity;

            var animation = new DoubleAnimation(0, new Duration(TimeSpan.FromMilliseconds(120)));
            animation.Completed += (s, e) => {
                overlayWindow.Visibility = Visibility.Hidden;
                overlayWindow.Opacity = 0;
            };

            overlayWindow.BeginAnimation(
                Window.OpacityProperty,
                animation
            );
        }

        public void UpdateOverlay()
        {
            // set color
            overlayIcon.Fill = GetBrushFromName(SettingsManager.Color);

            // set position (DPI-aware)
            UpdatePosition();

            // reset animazioni pendenti e imposta opacità base
            this.BeginAnimation(Window.OpacityProperty, null);
            overlayWindow.Opacity = SettingsManager.Opacity;
        }

        /// <summary>
        /// Calcola la working area del monitor che ospita la finestra in PIXEL
        /// e la converte in DIPs usando la scala corrente del monitor (per-monitor DPI).
        /// Poi posiziona l'overlay in DIPs.
        /// </summary>
        private void UpdatePosition()
        {
            var hwnd = new WindowInteropHelper(this).Handle;

            // 1) Working area del monitor CORRENTE (in pixel)
            // Screen screen = hwnd != IntPtr.Zero ? Screen.FromHandle(hwnd) : Screen.FromPoint(Cursor.Position);
            Screen screen = Screen.PrimaryScreen;
            Rectangle waPx = screen.WorkingArea;

            // 2) Ottieni scala DIPs->pixel del monitor che ospita la window
            double scaleX = 1.0, scaleY = 1.0;
            var src = PresentationSource.FromVisual(this);
            if (src?.CompositionTarget != null)
            {
                scaleX = src.CompositionTarget.TransformToDevice.M11;
                scaleY = src.CompositionTarget.TransformToDevice.M22;
            }
            else
            {
                var dpi = VisualTreeHelper.GetDpi(this);
                scaleX = dpi.DpiScaleX;
                scaleY = dpi.DpiScaleY;
            }

            // 3) Converte working area da pixel -> DIPs
            double waLeftDip = waPx.Left / scaleX;
            double waTopDip = waPx.Top / scaleY;
            double waWidthDip = waPx.Width / scaleX;
            double waHeightDip = waPx.Height / scaleY;

            // 4) Dimensioni dell'overlay in DIPs (preferisci Actual* se disponibili)
            double overlayW = overlayWindow.ActualWidth > 0 ? overlayWindow.ActualWidth : overlayWindow.Width;
            double overlayH = overlayWindow.ActualHeight > 0 ? overlayWindow.ActualHeight : overlayWindow.Height;

            double left, top;

            switch (SettingsManager.Position)
            {
                case "TopLeft":
                    left = waLeftDip + margin;
                    top = waTopDip + margin;
                    break;
                case "Top":
                    left = waLeftDip + (waWidthDip - overlayW) / 2;
                    top = waTopDip + margin;
                    break;
                case "TopRight":
                    left = waLeftDip + waWidthDip - overlayW - margin;
                    top = waTopDip + margin;
                    break;
                case "Right":
                    left = waLeftDip + waWidthDip - overlayW - margin;
                    top = waTopDip + (waHeightDip - overlayH) / 2;
                    break;
                case "BottomRight":
                    left = waLeftDip + waWidthDip - overlayW - margin;
                    top = waTopDip + waHeightDip - overlayH - margin;
                    break;
                case "Bottom":
                    left = waLeftDip + (waWidthDip - overlayW) / 2;
                    top = waTopDip + waHeightDip - overlayH - margin;
                    break;
                case "BottomLeft":
                    left = waLeftDip + margin;
                    top = waTopDip + waHeightDip - overlayH - margin;
                    break;
                case "Left":
                    left = waLeftDip + margin;
                    top = waTopDip + (waHeightDip - overlayH) / 2;
                    break;
                case "Center":
                    left = waLeftDip + (waWidthDip - overlayW) / 2;
                    top = waTopDip + (waHeightDip - overlayH) / 2;
                    break;
                default:
                    left = waLeftDip + (waWidthDip - overlayW) / 2;
                    top = waTopDip + (waHeightDip - overlayH) / 2;
                    break;
            }

            // 5) Clamp per sicurezza
            left = Math.Max(waLeftDip + margin, Math.Min(left, waLeftDip + waWidthDip - overlayW - margin));
            top = Math.Max(waTopDip + margin, Math.Min(top, waTopDip + waHeightDip - overlayH - margin));

            overlayWindow.Left = left;
            overlayWindow.Top = top;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_DPICHANGED)
            {
                // La scala del monitor è cambiata o la finestra è stata spostata su monitor con DPI diverso
                UpdatePosition();
                // handled = false -> lascia a WPF eventuale gestione aggiuntiva
            }
            return IntPtr.Zero;
        }

        private Brush GetBrushFromName(string color)
        {
            var hex = color switch
            {
                "White" => "#ffffff",
                "Grey" => "#bdc3c8",
                "Black" => "#222f3d",
                "Purple" => "#7e40fd",
                "Blue" => "#2980b9",
                "Yellow" => "#f39c19",
                "Green" => "#2ecc70",
                "Red" => "#e84b3c",
                _ => "#ffffff",
            };

            return new BrushConverter().ConvertFromString(hex) as SolidColorBrush;
        }
    }
}
