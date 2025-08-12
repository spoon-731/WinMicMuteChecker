using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WinMicMuteChecker
{
    public class HotkeyManager
    {
        private readonly Action _action;
        private readonly IntPtr _hookId = IntPtr.Zero;

        private readonly HotkeyCombination _hotkey;
        private readonly HashSet<Keys> pressedKeys = [];

        private readonly LowLevelKeyboardProc _callbackDelegate;

        public HotkeyManager(Action action, HotkeyCombination hotkey)
        {
            _action = action;
            _hotkey = hotkey;
            _callbackDelegate = HookCallback;
            _hookId = SetHook(_callbackDelegate);

        }

        public void Unregister()
        {
            UnhookWindowsHookEx(_hookId);
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            Process curProcess = Process.GetCurrentProcess();

            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curProcess.MainModule!.ModuleName), 0);
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Keys key = (Keys)vkCode;
                Keys normalized = NormalizeKey(key);

                bool isKeyDown = wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN;
                bool isKeyUp = wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP;

                if (isKeyDown)
                    pressedKeys.Add(normalized);
                else if (isKeyUp)
                    pressedKeys.Remove(normalized);

                if (_hotkey.IsMatch(pressedKeys))
                {
                    _action?.Invoke();
                    return (IntPtr)1;
                }
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private static Keys NormalizeKey(Keys key)
        {
            if (key == Keys.LShiftKey || key == Keys.RShiftKey) return Keys.ShiftKey;
            if (key == Keys.LControlKey || key == Keys.RControlKey) return Keys.ControlKey;
            if (key == Keys.LWin || key == Keys.RWin) return Keys.LWin;
            if (key == Keys.LMenu || key == Keys.RMenu) return Keys.Menu;

            return key;
        }

        public void Dispose()
        {
            Unregister();
        }

        // Costanti e P/Invoke
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
