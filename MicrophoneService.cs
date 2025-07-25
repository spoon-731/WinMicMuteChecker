using NAudio.CoreAudioApi;
using System.Windows;

namespace WinMicMuteChecker
{
    public class MicrophoneService
    {
        private MMDevice mic;
        private OverlayWindow overlay;

        public MicrophoneService(OverlayWindow overlayWindow)
        {
            overlay = overlayWindow;
            var enumerator = new MMDeviceEnumerator();
            mic = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
            mic.AudioEndpointVolume.OnVolumeNotification += VolumeChanged;
            CheckInitialState();
        }

        private void CheckInitialState()
        {
            if (mic.AudioEndpointVolume.Mute)
                overlay.ShowOverlay();
            else
                overlay.HideOverlay();
        }

        private void VolumeChanged(AudioVolumeNotificationData data)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (data.Muted)
                    overlay.ShowOverlay();
                else
                    overlay.HideOverlay();
            });
        }

        public void ToggleMute()
        {
            mic.AudioEndpointVolume.Mute = !mic.AudioEndpointVolume.Mute;
        }
    }
}
