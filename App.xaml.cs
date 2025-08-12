using System;
using System.Windows;

namespace WinMicMuteChecker
{
    public partial class App : Application
    {
        private OverlayAnimator _overlayAnimator;
        private OverlayWindow _overlayWindow;
        private TrayManager _trayManager;
        private MicrophoneService _microphoneService;
        private HotkeyManager _hotkeyManager;


        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            SettingsManager.LoadSettings();

            _overlayWindow = new OverlayWindow();

            // overlay animator
            _overlayAnimator = new OverlayAnimator(_overlayWindow);

            // tray icon + menu
            _trayManager = new TrayManager(_overlayWindow);

            // microphone listener
            _microphoneService = new MicrophoneService(_overlayAnimator);

            // hotkey listener
            _hotkeyManager = new HotkeyManager(() => _microphoneService.ToggleMute(), SettingsManager.LoadHotkeyCombination());
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _overlayAnimator.Dispose();
            _trayManager.Dispose();
            _microphoneService.Dispose();
            _hotkeyManager.Dispose();

            base.OnExit(e);
        }
    }
}
