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

        public OverlayWindow()
        {
            InitializeComponent();
        }

        // on overlay loaded - set properties
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;

            // Click-through + layered + no taskbar
            var exStyle = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE).ToInt64();
            exStyle |= NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_LAYERED;
            NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE, new IntPtr(exStyle));

            UpdateOverlay();
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

            // set position
            UpdatePosition();

            // Valore base (non animato)
            this.BeginAnimation(Window.OpacityProperty, null);
            // set opacity
            overlayWindow.Opacity = SettingsManager.Opacity;
        }

        private void UpdatePosition()
        {
            Rectangle screenArea = Screen.PrimaryScreen.WorkingArea;
            double screenW = screenArea.Width;
            double screenH = screenArea.Height;
            double offsetX = screenArea.Left;
            double offsetY = screenArea.Top;

            double overlayW = overlayWindow.Width;
            double overlayH = overlayWindow.Height;

            double left;
            double top;

            switch (SettingsManager.Position)
            {
                case "TopLeft":
                    left = offsetX + margin;
                    top = offsetY + margin;
                    break;
                case "Top":
                    left = offsetX + (screenW - overlayW) / 2;
                    top = offsetY + margin;
                    break;
                case "TopRight":
                    left = offsetX + screenW - overlayW - margin;
                    top = offsetY + margin;
                    break;
                case "Right":
                    left = offsetX + screenW - overlayW - margin;
                    top = offsetY + (screenH - overlayH) / 2;
                    break;
                case "BottomRight":
                    left = offsetX + screenW - overlayW - margin;
                    top = offsetY + screenH - overlayH - margin;
                    break;
                case "Bottom":
                    left = offsetX + (screenW - overlayW) / 2;
                    top = offsetY + screenH - overlayH - margin;
                    break;
                case "BottomLeft":
                    left = offsetX + margin;
                    top = offsetY + screenH - overlayH - margin;
                    break;
                case "Left":
                    left = offsetX + margin;
                    top = offsetY + (screenH - overlayH) / 2;
                    break;
                case "Center":
                    left = offsetX + (screenW - overlayW) / 2;
                    top = offsetY + (screenH - overlayH) / 2;
                    break;
                default:
                    left = offsetX + (screenW - overlayW) / 2;
                    top = offsetY + (screenH - overlayH) / 2;
                    break;
            }

            overlayWindow.Left = left;
            overlayWindow.Top = top;
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
