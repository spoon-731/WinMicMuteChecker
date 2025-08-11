using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using ContextMenu = System.Windows.Controls.ContextMenu;
using Icon = System.Drawing.Icon;
using MenuItem = System.Windows.Controls.MenuItem;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace WinMicMuteChecker
{
    public class TrayManager
    {
        private readonly TaskbarIcon trayIcon;
        private readonly OverlayWindow overlay;

        private Window? _panelWindow;
        private readonly QuickPanel _panel;
        private const double MarginFromEdges = 10;

        public TrayManager(OverlayWindow overlayWindow)
        {
            overlay = overlayWindow;

            trayIcon = new TaskbarIcon
            {
                Icon = new Icon("mic.ico"),
                ToolTipText = "WinMicMuteChecker",
                ContextMenu = BuildContextMenu
            };

            _panel = new QuickPanel(overlay);

            trayIcon.TrayLeftMouseUp += OnTrayLeftMouseUp;
        }

        private static ContextMenu BuildContextMenu
        {
            get
            {
                var menu = new ContextMenu();
                var exit = new MenuItem { Header = "Exit" };
                exit.Click += (s, e) => System.Windows.Application.Current.Shutdown();
                menu.Items.Add(exit);
                return menu;
            }
        }

        private bool TryGetTrayIconRect(TaskbarIcon icon, out RECT rc)
        {
            var sink = icon.GetType()
                .GetField("messageSink", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(icon);

            var hWnd = sink?.GetType()
                .GetProperty("MessageWindowHandle", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)
                ?.GetValue(sink) as IntPtr? ?? IntPtr.Zero;

            var idObj = icon.GetType()
                .GetField("id", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(icon);

            uint uID = idObj is int i ? (uint)i :
                       idObj is uint u ? u : 0u;

            var ident = new NOTIFYICONIDENTIFIER
            {
                cbSize = Marshal.SizeOf<NOTIFYICONIDENTIFIER>(),
                hWnd = hWnd,
                uID = uID,
                guidItem = Guid.Empty
            };

            var hr = Shell32.Shell_NotifyIconGetRect(ref ident, out rc);
            return hr == 0; // S_OK
        }
        private Point PixelsToDips(Point px)
        {
            var src = PresentationSource.FromVisual(_panel);
            return (src?.CompositionTarget is not null)
                ? src.CompositionTarget.TransformFromDevice.Transform(px)
                : px;
        }

        private Rect ScreenWorkingAreaDipsFromRect(RECT rcPx)
        {
            var screen = Screen.FromRectangle(Rectangle.FromLTRB(rcPx.Left, rcPx.Top, rcPx.Right, rcPx.Bottom));
            var wa = screen.WorkingArea;

            var topLeft = PixelsToDips(new Point(wa.Left, wa.Top));
            var botRight = PixelsToDips(new Point(wa.Right, wa.Bottom));
            return new Rect(topLeft, botRight);
        }

        private void OnTrayLeftMouseUp(object? sender, RoutedEventArgs e)
        {
            if (_panelWindow is { IsVisible: true })
            {
                _panelWindow.Close();
                _panelWindow = null;
                return;
            }

            // misura pannello
            _panel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double panelW = _panel.DesiredSize.Width > 0 ? _panel.DesiredSize.Width : (_panel.ActualWidth > 0 ? _panel.ActualWidth : 360);
            double panelH = _panel.DesiredSize.Height > 0 ? _panel.DesiredSize.Height : (_panel.ActualHeight > 0 ? _panel.ActualHeight : 318);

            // ricava il rect dell’icona (usa la tua TryGetTrayIconRect + conversioni in DIPs)
            RECT rc;
            bool ok = TryGetTrayIconRect(trayIcon, out rc);

            // work area del monitor dell’icona (in DIPs)
            Rect wa = ok ? ScreenWorkingAreaDipsFromRect(rc) : SystemParameters.WorkArea;

            // top dell’icona in DIPs (se fallisce, fallback al click)
            double iconTopDip;
            if (ok)
                iconTopDip = PixelsToDips(new Point(rc.Left, rc.Top)).Y;
            else
            {
                var mousePx = System.Windows.Forms.Control.MousePosition;
                iconTopDip = PixelsToDips(new Point(mousePx.X, mousePx.Y)).Y;
            }

            // POSIZIONAMENTO: top-right fisso e 10px sopra l’icona
            double left = wa.Right - MarginFromEdges - panelW;
            double top = iconTopDip - panelH - MarginFromEdges;

            // clamp verticale
            if (top < wa.Top + MarginFromEdges)
                top = wa.Top + MarginFromEdges;

            // crea window
            _panelWindow = new Window
            {
                Content = _panel,
                Width = panelW,
                Height = panelH,
                Left = left,
                Top = top,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                Topmost = true,
                AllowsTransparency = true,
            };

            // chiudi automaticamente alla perdita del focus
            _panelWindow.Deactivated += (_, __) => { _panelWindow?.Close(); _panelWindow = null; };

            _panelWindow.Show();
            _panelWindow.Activate();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    public struct NOTIFYICONIDENTIFIER
    {
        public int cbSize;
        public IntPtr hWnd;
        public uint uID;
        public Guid guidItem;
    }

    static class Shell32
    {
        [DllImport("shell32.dll", SetLastError = true)]
        public static extern int Shell_NotifyIconGetRect(ref NOTIFYICONIDENTIFIER identifier, out RECT iconLocation);
    }
}
