using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using ContextMenu = System.Windows.Controls.ContextMenu;
using MenuItem = System.Windows.Controls.MenuItem;
using Point = System.Windows.Point;

namespace WinMicMuteChecker
{
    public sealed class TrayManager : IDisposable
    {
        private readonly TaskbarIcon _trayIcon;
        private readonly OverlayWindow _overlay;
        private readonly TrayWindow _trayWindow;
        private readonly Icon _icon;

        private const double MarginFromEdges = 10;
        private bool _disposed;

        public TrayManager(OverlayWindow overlayWindow)
        {
            _overlay = overlayWindow;

            // load trayIcon icon
            _icon = new Icon("mic.ico");

            _trayIcon = new TaskbarIcon
            {
                Icon = _icon,
                ToolTipText = "WinMicMuteChecker",
                ContextMenu = BuildContextMenu()
            };

            _trayWindow = new TrayWindow(_overlay)
            {
                Topmost = true,
                ShowInTaskbar = false,
            };
            _trayWindow.Deactivated += (s, e) =>
            {
                _trayWindow.Hide();
            };

            _trayIcon.TrayLeftMouseUp += OnTrayLeftMouseUp;
        }

        private static ContextMenu BuildContextMenu()
        {
            var menu = new ContextMenu();
            var exit = new MenuItem { Header = "Exit" };
            exit.Click += (_, __) => System.Windows.Application.Current.Shutdown();
            menu.Items.Add(exit);
            return menu;
        }

        private void OnTrayLeftMouseUp(object sender, RoutedEventArgs e)
        {
            // get trayWindow sizes
            double panelW = _trayWindow.Width;
            double panelH = _trayWindow.Height;

            // anchor point position in pixel
            System.Drawing.Point mousePx = Control.MousePosition;
            var midPx = new POINT { X = mousePx.X, Y = mousePx.Y };

            // monitor target + DPI
            IntPtr hMon = DpiUtil.MonitorFromPoint(midPx, 2);

            // working area in PX, converted in DIPs
            var screen = Screen.FromPoint(mousePx);
            Rectangle waPx = screen.WorkingArea;

            Point waDipTopLeft = DpiUtil.PxToDip(new Point(waPx.Left, waPx.Top), hMon);
            Point waDipBotRight = DpiUtil.PxToDip(new Point(waPx.Right, waPx.Bottom), hMon);
            var wa = new Rect(waDipTopLeft, waDipBotRight);

            // coordinates of anchor point in DIPs
            Point anchorDip = DpiUtil.PxToDip(new Point(mousePx.X, mousePx.Y), hMon);

            // get taskbar position
            var edge = TaskbarUtil.GetEdge();

            double left = wa.Right - MarginFromEdges - panelW;
            double top;

            switch (edge)
            {
                case TaskbarUtil.Edge.Bottom:
                default:
                    // Taskbar bottom → show to top of icon
                    top = anchorDip.Y - panelH - MarginFromEdges;
                    break;

                case TaskbarUtil.Edge.Top:
                    // Taskbar top → show to bottom of icon
                    top = anchorDip.Y + MarginFromEdges;
                    break;

                case TaskbarUtil.Edge.Left:
                    // Taskbar left → show to right of icon
                    left = anchorDip.X + MarginFromEdges;
                    top = anchorDip.Y - panelH;
                    break;

                case TaskbarUtil.Edge.Right:
                    // Taskbar right → show to left of icon
                    left = anchorDip.X - panelW - MarginFromEdges;
                    top = anchorDip.Y - panelH;
                    break;
            }

            // clamp X/Y inside working area
            left = Math.Max(wa.Left + MarginFromEdges, Math.Min(left, wa.Right - MarginFromEdges - panelW));
            top = Math.Max(wa.Top + MarginFromEdges, Math.Min(top, wa.Bottom - MarginFromEdges - panelH));

            // change trayWindow coordinates
            _trayWindow.Left = left;
            _trayWindow.Top = top;

            // show trayWindow
            _trayWindow.Show();
            _trayWindow.Activate();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // remove events
            _trayIcon.TrayLeftMouseUp -= OnTrayLeftMouseUp;

            // close trayWindow if visible
            if (_trayWindow.IsVisible) _trayWindow.Hide();

            // release resources
            _trayIcon.Dispose();
            _icon.Dispose();
        }
    }

    #region DPI & Taskbar helpers

    internal static class DpiUtil
    {
        // GetDpiForMonitor: https://learn.microsoft.com/windows/win32/api/shellscalingapi/nf-shellscalingapi-getdpiformonitor
        [DllImport("Shcore.dll")]
        private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

        // MonitorFromPoint: https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-monitorfrompoint
        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

        public static Point PxToDip(Point px, IntPtr hMonitor)
        {
            // MDT_EFFECTIVE_DPI = 0
            const int MDT_EFFECTIVE_DPI = 0;
            if (GetDpiForMonitor(hMonitor, MDT_EFFECTIVE_DPI, out uint dx, out uint dy) == 0 && dx > 0 && dy > 0)
            {
                return new Point(px.X * 96.0 / dx, px.Y * 96.0 / dy);
            }
            // fallback (96 DPI)
            return new Point(px.X, px.Y);
        }
    }

    internal static class TaskbarUtil
    {
        // SHAppBarMessage: https://learn.microsoft.com/windows/win32/api/shellapi/nf-shellapi-shappbarmessage
        [DllImport("shell32.dll")]
        private static extern IntPtr SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

        private const uint ABM_GETTASKBARPOS = 0x00000005;

        [StructLayout(LayoutKind.Sequential)]
        private struct APPBARDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public uint uCallbackMessage;
            public uint uEdge;
            public RECT rc;
            public int lParam;
        }

        public enum Edge { Unknown, Left, Top, Right, Bottom }

        public static Edge GetEdge()
        {
            var abd = new APPBARDATA { cbSize = Marshal.SizeOf<APPBARDATA>() };
            var res = SHAppBarMessage(ABM_GETTASKBARPOS, ref abd);
            if (res == IntPtr.Zero) return Edge.Unknown;

            return abd.uEdge switch
            {
                0 => Edge.Left,
                1 => Edge.Top,
                2 => Edge.Right,
                3 => Edge.Bottom,
                _ => Edge.Unknown
            };
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT { public int X, Y; }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT { public int Left, Top, Right, Bottom; }

    #endregion
}
