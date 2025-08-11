using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WinMicMuteChecker
{
    public partial class QuickPanel : UserControl
    {
        private readonly OverlayWindow? _overlay;
        private bool _initializing;

        public QuickPanel(OverlayWindow overlay)
        {
            _initializing = true;
            InitializeComponent();

            _overlay = overlay;

            Loaded += (_, __) =>
            {
                OpacitySlider.Value = Math.Round(SettingsManager.Opacity * 100);
                HighlightPosition(SettingsManager.Position);
                HighlightColor(SettingsManager.Color);
                _initializing = false;
            };
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

            SettingsManager.Opacity = Math.Clamp(e.NewValue / 100.0, 0.1, 1.0);

            SettingsManager.SaveSettings();
            _overlay?.UpdateOverlay();
        }
    }
}
