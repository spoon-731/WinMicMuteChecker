using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Keys = System.Windows.Forms.Keys;
using TextBox = System.Windows.Controls.TextBox;

namespace WinMicMuteChecker
{
    public class HotkeyDialog : Window
    {
        [DllImport("user32.dll")]
        static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        private TextBox hotkeyBox;
        public ModifierKeys currentModifiers;
        public Keys mainKey;

        public Keys VirtualKeyCode { get; private set; }
        public ModifierKeys Modifiers { get; private set; }

        const int HOTKEY_ID = 0xB00B; // some unique ID

        public HotkeyDialog()
        {
            Title = "Set shortcut";
            Width = 300;
            Height = 145;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Topmost = true;
            ShowInTaskbar = false;

            var stack = new StackPanel { Margin = new Thickness(20) };
            stack.Children.Add(new TextBlock { Text = "Insert key combination:" });

            hotkeyBox = new TextBox { IsReadOnly = true, Focusable = true };
            hotkeyBox.PreviewKeyDown += HotkeyBox_PreviewKeyDown;
            hotkeyBox.PreviewKeyUp += HotkeyBox_PreviewKeyUp;
            hotkeyBox.LostKeyboardFocus += (s, e) => e.Handled = true;
            stack.Children.Add(hotkeyBox);

            mainKey = SettingsManager.Hotkey;
            currentModifiers = (ModifierKeys)SettingsManager.Modifier;
            Modifiers = currentModifiers;

            UpdateDisplay(mainKey, currentModifiers);

            var saveButton = new Button { Content = "Save", Margin = new Thickness(0, 10, 0, 0) };
            saveButton.Click += Save_Click;
            stack.Children.Add(saveButton);

            Content = stack;
        }

        private void HotkeyBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            currentModifiers = Keyboard.Modifiers;

            // Determine actual key pressed (handling system keys like Alt)
            Key wpfKey = (e.Key == Key.System) ? e.SystemKey : e.Key;

            // Skip pure modifier keys
            if (wpfKey == Key.LeftCtrl || wpfKey == Key.RightCtrl ||
                wpfKey == Key.LeftAlt || wpfKey == Key.RightAlt ||
                wpfKey == Key.LeftShift || wpfKey == Key.RightShift ||
                wpfKey == Key.LWin || wpfKey == Key.RWin)
            {
                mainKey = Keys.None;
            }
            else
            {
                // Convert to WinForms Keys
                mainKey = KeyConverter.ToWinFormsKey(wpfKey);
            }

            UpdateDisplay(mainKey, currentModifiers);
        }

        private void HotkeyBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            if (mainKey == Keys.None) return;

            Modifiers = currentModifiers;
            VirtualKeyCode = mainKey;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // 1) Persist to your settings
            SettingsManager.Hotkey = VirtualKeyCode;
            SettingsManager.Modifier = (uint)Modifiers;
            SettingsManager.SaveSettings();

            // 2) Register the global hotkey right now
            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            uint fsMods = 0;
            if (Modifiers.HasFlag(ModifierKeys.Control)) fsMods |= 0x0002;
            if (Modifiers.HasFlag(ModifierKeys.Alt)) fsMods |= 0x0001;
            if (Modifiers.HasFlag(ModifierKeys.Shift)) fsMods |= 0x0004;
            if (Modifiers.HasFlag(ModifierKeys.Windows)) fsMods |= 0x0008;

            RegisterHotKey(helper.Handle, HOTKEY_ID, fsMods, (uint)VirtualKeyCode);

            DialogResult = true;
            Close();
        }

        private void UpdateDisplay(Keys key, ModifierKeys mods)
        {
            var parts = new List<string>();
            if (mods.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
            if (mods.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
            if (mods.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
            if (mods.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
            if (key != Keys.None) parts.Add(key.ToString());

            hotkeyBox.Text = parts.Count > 0
                ? string.Join(" + ", parts)
                : "Press a key combination…";
        }
    }
}
