using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms; // Per monitor principale
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace WinMicMuteChecker
{
    internal static class NativeMethods
    {
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TRANSPARENT = 0x20;
        public const int WS_EX_LAYERED = 0x80000;
        public const int WS_EX_TOOLWINDOW = 0x80;

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    }

    public class OverlayWindow : Window
    {
        private static Dictionary<string, BitmapImage> _imageCache = new Dictionary<string, BitmapImage>();

        private Image image = new Image();
        private const int margin = 10; // distanza dal bordo

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
            //Content = image;
            Visibility = Visibility.Hidden;
            Owner = null;

            var border = new Border
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(192, 0, 0, 0)), // nero ~75%
                CornerRadius = new CornerRadius(8), // bordi arrotondati
                Padding = new Thickness(5),
                Child = image
            };

            Content = border;

            Loaded += (s, e) =>
            {
                // Rendi la finestra click-through
                var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                int style = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
                NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE,
                    style | NativeMethods.WS_EX_LAYERED | NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_TOOLWINDOW);
            };
        }

        public void UpdateOverlay()
        {
            var imagePath = $"Assets/no_mic_{SettingsManager.Color.ToLower()}.png";
            //image.Source = new BitmapImage(new Uri(imagePath, UriKind.Relative));
            image.Source = GetCachedImage(imagePath);

            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
            PositionWindow(SettingsManager.Position);
            Opacity = SettingsManager.Opacity;
        }

        public void ShowOverlay()
        {
            UpdateOverlay();
            BeginAnimation(Window.OpacityProperty, null); // cancella eventuali animazioni
            Visibility = Visibility.Visible;
            Opacity = SettingsManager.Opacity;
        }

        public void HideOverlay()
        {
            DoubleAnimation fade = new DoubleAnimation(Opacity, 0, TimeSpan.FromMilliseconds(500));
            fade.Completed += (s, e) =>
            {
                Visibility = Visibility.Hidden;
                Opacity = SettingsManager.Opacity;
            };
            BeginAnimation(Window.OpacityProperty, fade);
        }

        private void PositionWindow(string position)
        {
            // Usa il monitor principale per calcolare WorkArea (esclude taskbar)
            var area = Screen.PrimaryScreen.WorkingArea;
            double screenW = area.Width;
            double screenH = area.Height;
            double offsetX = area.Left;
            double offsetY = area.Top;

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
                case "Center":
                    Left = offsetX + (screenW - Width) / 2;
                    Top = offsetY + (screenH - Height) / 2;
                    break;
            }
        }

        private BitmapImage GetCachedImage(string imagePath)
        {
            if (_imageCache.ContainsKey(imagePath))
                return _imageCache[imagePath];

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad; // carica tutto in RAM
            bmp.UriSource = new Uri(imagePath, UriKind.Relative);
            bmp.EndInit();
            bmp.Freeze(); // thread-safe, più leggero

            _imageCache[imagePath] = bmp;
            return bmp;
        }

    }
}
