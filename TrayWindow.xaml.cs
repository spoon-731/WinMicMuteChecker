using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WinMicMuteChecker
{
    public partial class TrayWindow : Window
    {
        private readonly OverlayWindow _overlay;
        private bool _initializing;

        public TrayWindow(OverlayWindow overlay)
        {
            _initializing = true;
            InitializeComponent();

            _overlay = overlay;
        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            int classStyle = GetClassLong(hwnd, GCL_STYLE);
            SetClassLong(hwnd, GCL_STYLE, classStyle | CS_DROPSHADOW);
        }

        // on popup loaded
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            OpacitySlider.Value = Math.Round(SettingsManager.Opacity * 100);
            HighlightPosition(SettingsManager.Position);
            HighlightColor(SettingsManager.Color);

            _initializing = false;
        }

        private void HighlightPosition(string pos)
        {
            foreach (var btn in PositionGrid.Children.OfType<Button>())
            {
                var isSelected = string.Equals(btn.Tag as string, pos, StringComparison.OrdinalIgnoreCase);
                btn.BorderBrush = isSelected
                    ? Brushes.White
                    : new SolidColorBrush(Color.FromRgb(0x5A, 0x5A, 0x5A));
            }
        }

        private void HighlightColor(string name)
        {
            foreach (var btn in ColorPanel.Children.OfType<Button>())
            {
                var isSelected = string.Equals(btn.Tag as string, name, StringComparison.OrdinalIgnoreCase);
                btn.BorderBrush = isSelected ? Brushes.White : Brushes.Transparent;
            }
        }

        private static System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(DependencyObject dep) where T : DependencyObject
        {
            if (dep == null) yield break;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(dep); i++)
            {
                var child = VisualTreeHelper.GetChild(dep, i);
                if (child is T t) yield return t;
                foreach (var c in FindVisualChildren<T>(child)) yield return c;
            }
        }

        private void OnPositionClick(object sender, RoutedEventArgs e)
        {
            if (_initializing) return;

            if (sender is Button b && b.Tag is string pos)
            {
                SettingsManager.Position = pos;
                SettingsManager.SaveSettings();
                _overlay?.UpdateOverlay();
                HighlightPosition(pos);
            }
        }

        private void OnColorClick(object sender, RoutedEventArgs e)
        {
            if (_initializing) return;

            if (sender is Button b && b.Tag is string name)
            {
                SettingsManager.Color = name;
                SettingsManager.SaveSettings();
                _overlay?.UpdateOverlay();
                HighlightColor(name);
            }
        }

        private void OnOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_initializing) return;

            SettingsManager.Opacity = e.NewValue / 100.0;

            SettingsManager.SaveSettings();
            _overlay?.UpdateOverlay();
        }

        private const int GCL_STYLE = -26;
        private const int CS_DROPSHADOW = 0x00020000;

        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetClassLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern int SetClassLong(IntPtr hWnd, int nIndex, int dwNewLong);
    }
}
