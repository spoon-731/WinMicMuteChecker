using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WinMicMuteChecker
{
    public class LowLevelHotkeyManager
    {
        private readonly Action action;
        private IntPtr hookId = IntPtr.Zero;

        private bool winPressed = false;
        private bool shiftPressed = false;

        private HotkeyCombination hotkey;
        private readonly HashSet<Keys> pressedKeys = new HashSet<Keys>();

        private readonly LowLevelKeyboardProc hookCallbackDelegate;

        public LowLevelHotkeyManager(Action onHotkeyPressed, HotkeyCombination hotkey)
        {
            this.action = onHotkeyPressed;
            this.hotkey = hotkey;
            this.hookCallbackDelegate = HookCallback;
            this.hookId = SetHook(hookCallbackDelegate);

        }

        public void Unregister()
        {
            UnhookWindowsHookEx(hookId);
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            Process curProcess = Process.GetCurrentProcess();
            ProcessModule curModule = curProcess.MainModule;

            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
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

                if (hotkey.IsMatch(pressedKeys))
                {
                    action?.Invoke();
                    return (IntPtr)1;
                }
            }

            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }
        public void UpdateHotkey(HotkeyCombination newCombo)
        {
            UnhookWindowsHookEx(hookId);
            this.hotkey = newCombo;
            this.pressedKeys.Clear();
            this.hookId = SetHook(hookCallbackDelegate);
        }
        private Keys NormalizeKey(Keys key)
        {
            if (key == Keys.LShiftKey || key == Keys.RShiftKey)
                return Keys.ShiftKey;
            if (key == Keys.LControlKey || key == Keys.RControlKey)
                return Keys.ControlKey;
            if (key == Keys.LWin || key == Keys.RWin)
                return Keys.LWin;
            if (key == Keys.LMenu || key == Keys.RMenu)
                return Keys.Menu;
            return key;
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

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
