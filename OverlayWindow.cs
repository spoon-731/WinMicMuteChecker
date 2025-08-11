using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace WinMicMuteChecker
{
    internal static class NativeMethods
    {
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TRANSPARENT = 0x20;
        public const int WS_EX_LAYERED = 0x00080000;
        public const int WS_EX_TOOLWINDOW = 0x00000080;

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
        public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
    }

    public class OverlayWindow : Window
    {
        private const int margin = 10;

        private readonly Path _iconPath = new();
        private readonly Viewbox _viewbox = new();

        public OverlayWindow()
        {
            Width = 40;
            Height = 40;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Topmost = true;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            Background = null;
            Visibility = Visibility.Hidden;
            Owner = null;

            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(192, 0, 0, 0)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8),
                Child = _viewbox
            };

            _iconPath.Data = BuildMicMutedGeometry;
            _iconPath.Stretch = Stretch.Uniform;

            _viewbox.Child = _iconPath;
            _viewbox.Stretch = Stretch.Uniform;

            Content = border;

            Loaded += (s, e) =>
            {
                var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                var exStyle = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE);
                var style = exStyle.ToInt64();
                style |= NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_LAYERED;
                NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE, new IntPtr(style));
            };
        }

        private static Geometry BuildMicMutedGeometry
        {
            get
            {
                const string d1 = "M502 4670 c-46 -28 -72 -76 -72 -133 0 -27 5 -58 11 -70 6 -12 294 -305 639 -652 l629 -630 4 -385 c3 -385 3 -385 30 -473 91 -296 307 -508 602 -588 117 -32 319 -33 427 -1 89 26 171 61 226 96 l39 24 124 -124 124 -125 -25 -19 c-43 -33 -167 -101 -230 -127 -216 -87 -554 -105 -795 -43 -219 56 -423 178 -558 333 -132 151 -214 307 -261 492 -17 68 -36 248 -36 345 0 85 -22 127 -81 158 -75 39 -150 20 -203 -52 -19 -26 -21 -41 -20 -199 1 -134 6 -195 23 -274 75 -358 276 -670 567 -879 190 -136 366 -210 594 -249 52 -9 105 -18 118 -21 l22 -4 0 -253 c0 -242 1 -255 23 -297 32 -63 66 -85 135 -85 70 0 115 28 143 89 16 36 19 69 19 292 0 277 -3 264 63 264 44 1 196 35 292 66 129 43 278 122 403 214 l32 24 473 -471 c506 -505 491 -493 579 -478 53 9 101 55 119 113 28 95 182 -68 -2004 2120 -1096 1095 -2006 1999 -2024 2007 -44 21 -112 19 -151 -5z";
                const string d2 = "M2375 4671 c-289 -68 -522 -276 -616 -548 l-19 -57 827 -828 828 -828 8 46 c4 25 6 380 5 788 l-3 741 -28 90 c-75 238 -224 418 -439 526 -120 60 -203 80 -353 85 -103 3 -147 0 -210 -15z";
                const string d3 = "M3818 2750 c-58 -32 -78 -74 -78 -168 0 -120 -18 -267 -43 -364 l-24 -87 118 -117 c65 -65 120 -116 123 -114 10 11 65 161 84 230 35 127 46 219 46 382 1 143 -1 159 -20 184 -51 70 -136 92 -206 54z";

                var g = new GeometryGroup();
                g.Children.Add(Geometry.Parse(d1));
                g.Children.Add(Geometry.Parse(d2));
                g.Children.Add(Geometry.Parse(d3));

                var tg = new TransformGroup();
                tg.Children.Add(new ScaleTransform(0.1, -0.1));
                tg.Children.Add(new TranslateTransform(0, 512));
                g.Transform = tg;

                return g;
            }
        }

        private static Brush GetSelectedColorBrush()
        {
            Brush? brush;
            brush = SettingsManager.Color switch
            {
                "White" => new BrushConverter().ConvertFromString("#ffffff") as SolidColorBrush,
                "Grey" => new BrushConverter().ConvertFromString("#bdc3c8") as SolidColorBrush,
                "Black" => new BrushConverter().ConvertFromString("#222f3d") as SolidColorBrush,
                "Purple" => new BrushConverter().ConvertFromString("#7e40fd") as SolidColorBrush,
                "Blue" => new BrushConverter().ConvertFromString("#2980b9") as SolidColorBrush,
                "Yellow" => new BrushConverter().ConvertFromString("#f39c19") as SolidColorBrush,
                "Green" => new BrushConverter().ConvertFromString("#2ecc70") as SolidColorBrush,
                "Red" => new BrushConverter().ConvertFromString("#e84b3c") as SolidColorBrush,
                _ => Brushes.White,
            };

            brush ??= Brushes.White;

            return brush;
        }

        public void ShowOverlay()
        {
            _iconPath.Fill = GetSelectedColorBrush();

            Opacity = 0;
            Visibility = Visibility.Visible;
            PositionWindow(SettingsManager.Position);

            Console.WriteLine("Opacity: " + SettingsManager.Opacity.ToString());

            var fade = new DoubleAnimation(SettingsManager.Opacity, new Duration(TimeSpan.FromMilliseconds(120)));
            BeginAnimation(Window.OpacityProperty, fade);
        }

        public void HideOverlay()
        {
            var fade = new DoubleAnimation(Opacity, 0, TimeSpan.FromMilliseconds(500));
            fade.Completed += (s, e) =>
            {
                Visibility = Visibility.Hidden;
                Opacity = SettingsManager.Opacity;
            };
            BeginAnimation(Window.OpacityProperty, fade);
        }

        public void SetMuted(bool muted)
        {
            if (muted) ShowOverlay();
            else HideOverlay();
        }

        public void UpdateOverlay()
        {
            _iconPath.Fill = GetSelectedColorBrush();
            PositionWindow(SettingsManager.Position);
            BeginAnimation(Window.OpacityProperty, null);
            Opacity = SettingsManager.Opacity;
        }

        private void PositionWindow(string position)
        {
            var screen = Screen.PrimaryScreen;
            if (screen == null) return;

            var area = screen.WorkingArea;

            var dpi = VisualTreeHelper.GetDpi(this);
            double scaleX = dpi.DpiScaleX;
            double scaleY = dpi.DpiScaleY;

            double screenW = area.Width / scaleX;
            double screenH = area.Height / scaleY;
            double offsetX = area.Left / scaleX;
            double offsetY = area.Top / scaleY;

            switch (position)
            {
                case "TopLeft":
                    Left = offsetX + margin;
                    Top = offsetY + margin;
                    break;
                case "Top":
                    Left = offsetX + (screenW - Width) / 2;
                    Top = offsetY + margin;
                    break;
                case "TopRight":
                    Left = offsetX + screenW - Width - margin;
                    Top = offsetY + margin;
                    break;
                case "Right":
                    Left = offsetX + screenW - Width - margin;
                    Top = offsetY + (screenH - Height) / 2;
                    break;
                case "BottomRight":
                    Left = offsetX + screenW - Width - margin;
                    Top = offsetY + screenH - Height - margin;
                    break;
                case "Bottom":
                    Left = offsetX + (screenW - Width) / 2;
                    Top = offsetY + screenH - Height - margin;
                    break;
                case "BottomLeft":
                    Left = offsetX + margin;
                    Top = offsetY + screenH - Height - margin;
                    break;
                case "Left":
                    Left = offsetX + margin;
                    Top = offsetY + (screenH - Height) / 2;
                    break;
                default:
                    Left = offsetX + (screenW - Width) / 2;
                    Top = offsetY + (screenH - Height) / 2;
                    break;
            }
        }
    }
}