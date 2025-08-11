using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace WinMicMuteChecker
{
    public partial class App : Application
    {
        private MicrophoneService? micService;
        private LowLevelHotkeyManager? lowLevelHotkeyManager;
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Carica impostazioni
            SettingsManager.LoadSettings();

            // Crea overlay trasparente
            var overlay = new OverlayWindow();
            overlay.Show();
            overlay.Hide();

            // Avvia listener microfono
            micService = new MicrophoneService(overlay);

            // Avvia listener hotkey toggle mute
            lowLevelHotkeyManager = new LowLevelHotkeyManager(() => micService.ToggleMute(), SettingsManager.LoadHotkeyCombination());

            // Avvia icona nel system tray
            var trayManager = new TrayManager(overlay);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            micService!.Dispose();
            base.OnExit(e);
        }
    }
}
