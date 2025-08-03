using System.Windows;

namespace WinMicMuteChecker
{
    public partial class App : Application
    {
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
            var micService = new MicrophoneService(overlay);

            // Avvia listener hotkey toggle mute
            //var hotkeyManager = new HotkeyManager(overlay, () => micService.ToggleMute());
            var lowLevelHotkeyManager = new LowLevelHotkeyManager(() => micService.ToggleMute(), SettingsManager.LoadHotkeyCombination());

            // Avvia icona nel system tray
            var trayManager = new TrayManager(overlay, lowLevelHotkeyManager);
        }
    }
}
