using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;
using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;

namespace WinMicMuteChecker
{
    public class TrayManager
    {
        private TaskbarIcon trayIcon;
        private OverlayWindow overlay;
        private HotkeyManager _hotkeyManager;

        public TrayManager(OverlayWindow overlayWindow, HotkeyManager hotkeyManager)
        {
            overlay = overlayWindow;
            trayIcon = new TaskbarIcon
            {
                Icon = new Icon("mic.ico"),
                ToolTipText = "WinMicMuteChecker"
            };
            trayIcon.ContextMenu = BuildContextMenu();
            _hotkeyManager = hotkeyManager;
        }

        private ContextMenu BuildContextMenu()
        {
            ContextMenu menu = new ContextMenu();

            menu.Items.Add(BuildSubMenu("Position", new[]
            {
                "TopLeft", "Top", "TopRight", "Right", "BottomRight", "Bottom", "BottomLeft", "Left", "Center"
            }, SettingsManager.Position, val =>
            {
                SettingsManager.Position = val;
                SettingsManager.SaveSettings();
                overlay.UpdateOverlay();
            }));

            menu.Items.Add(BuildSubMenu("Color", new[]
            {
                "White", "Grey", "Black", "Purple", "Blue", "Yellow", "Green", "Red"
            }, SettingsManager.Color, val =>
            {
                SettingsManager.Color = val;
                SettingsManager.SaveSettings();
                overlay.UpdateOverlay();
            }));

            menu.Items.Add(BuildSubMenu("Opacity", new[]
            {
                "100", "90", "80", "70", "60", "50", "40", "30", "20", "10"
            }, ((int)(SettingsManager.Opacity * 100)).ToString(), val =>
            {
                SettingsManager.Opacity = int.Parse(val) / 100.0;
                SettingsManager.SaveSettings();
                overlay.UpdateOverlay();
            }));

            menu.Items.Add(new Separator());

            MenuItem hotkeyItem = new MenuItem
            {
                Header = "Shortcut",
                IsCheckable = false,
            };
            hotkeyItem.Click += (s, e) => ShowHotkeyDialog();
            menu.Items.Add(hotkeyItem);

            //menu.Items.Add(new Separator());

            //MenuItem startupItem = new MenuItem
            //{
            //    Header = "Run at Windows startup",
            //    IsCheckable = true,
            //    IsChecked = SettingsManager.RunAtStartup
            //};
            //startupItem.Click += (s, e) =>
            //{
            //    SettingsManager.RunAtStartup = !SettingsManager.RunAtStartup;
            //    SettingsManager.SaveSettings();

            //    string appName = "WinMicMuteChecker";
            //    string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;

            //    try
            //    {
            //        RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

            //        if (SettingsManager.RunAtStartup)
            //            rk.SetValue(appName, exePath);
            //        else
            //            rk.DeleteValue(appName, false);
            //    }
            //    catch (Exception ex)
            //    {
            //        MessageBox.Show("Errore durante la modifica dell'avvio automatico:\n" + ex.Message);
            //    }
            //};
            //menu.Items.Add(startupItem);

            menu.Items.Add(new Separator());
            MenuItem exitItem = new MenuItem { Header = "Exit" };
            exitItem.Click += (s, e) => System.Windows.Application.Current.Shutdown();
            menu.Items.Add(exitItem);

            return menu;
        }

        private MenuItem BuildSubMenu(string header, string[] options, string selected, Action<string> onSelect)
        {
            MenuItem submenu = new MenuItem { Header = header };

            foreach (var option in options)
            {
                MenuItem item = new MenuItem
                {
                    Header = option,
                    IsCheckable = true,
                    IsChecked = option == selected
                };
                item.Click += (s, e) =>
                {
                    foreach (MenuItem other in submenu.Items)
                        other.IsChecked = false;

                    item.IsChecked = true;
                    onSelect(option);
                };
                submenu.Items.Add(item);
            }

            return submenu;
        }

        private void ShowHotkeyDialog()
        {
            var dialog = new HotkeyDialog();
            if (dialog.ShowDialog() == true)
            {
                SettingsManager.Hotkey = dialog.mainKey;
                SettingsManager.Modifier = (uint)dialog.currentModifiers;
                SettingsManager.SaveSettings();

                _hotkeyManager?.Unregister();
                _hotkeyManager?.RegisterHotkey((uint)dialog.currentModifiers, dialog.mainKey);
            }
        }
    }
}
