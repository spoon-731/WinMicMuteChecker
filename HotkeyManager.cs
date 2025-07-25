using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Keys = System.Windows.Forms.Keys;

namespace WinMicMuteChecker
{
    public class HotkeyManager
    {
        private readonly Window window;
        private HwndSource source;
        private readonly Action action;
        private const int HOTKEY_ID = 9000;

        public const uint MOD_NONE = 0x0000;
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;

        public HotkeyManager(Window window, Action onHotkeyPressed)
        {
            this.window = window;
            this.action = onHotkeyPressed;

            var helper = new WindowInteropHelper(window);
            if (helper.Handle == IntPtr.Zero)
                window.SourceInitialized += (s, e) => RegisterHotkey(SettingsManager.Modifier, SettingsManager.Hotkey);
            else
                RegisterHotkey(SettingsManager.Modifier, SettingsManager.Hotkey);
        }

        public void RegisterHotkey(uint modifier, Keys key)
        {
            Unregister(); // Per evitare doppia registrazione

            var helper = new WindowInteropHelper(window);
            source = HwndSource.FromHwnd(helper.Handle);
            source.AddHook(HwndHook);

            RegisterHotKey(helper.Handle, HOTKEY_ID, modifier, (uint)key);
        }

        public void Unregister()
        {
            var helper = new WindowInteropHelper(window);
            if (helper.Handle != IntPtr.Zero)
            {
                UnregisterHotKey(helper.Handle, HOTKEY_ID);
            }

            if (source != null)
            {
                source.RemoveHook(HwndHook);
                source = null;
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                action?.Invoke();
                handled = true;
            }

            return IntPtr.Zero;
        }

        // Importazioni Win32
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
