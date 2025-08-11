using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Windows;

namespace WinMicMuteChecker
{
    public class MicrophoneService : IDisposable, IMMNotificationClient
    {
        private MMDevice? mic;
        private readonly OverlayWindow overlay;
        private readonly MMDeviceEnumerator enumerator;

        public MicrophoneService(OverlayWindow overlayWindow)
        {
            overlay = overlayWindow;
            enumerator = new MMDeviceEnumerator();
            BindToDefaultMic();
            enumerator.RegisterEndpointNotificationCallback(this);
        }

        private void BindToDefaultMic()
        {
            Unsubscribe();
            mic?.Dispose();

            mic = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
            if (mic != null)
            {
                mic.AudioEndpointVolume.OnVolumeNotification += VolumeChanged;
                UpdateOverlay();
            }
        }

        private void VolumeChanged(AudioVolumeNotificationData data)
        {
            Application.Current.Dispatcher.Invoke(UpdateOverlay);
        }

        private void UpdateOverlay()
        {
            bool muted = mic?.AudioEndpointVolume?.Mute ?? false;
            overlay.SetMuted(muted);
        }

        public void ToggleMute()
        {
            if (mic == null) return;
            mic.AudioEndpointVolume.Mute = !mic.AudioEndpointVolume.Mute;
        }

        private void Unsubscribe()
        {
            try
            {
                if (mic != null && mic.AudioEndpointVolume != null)
                {
                    mic.AudioEndpointVolume.OnVolumeNotification -= VolumeChanged;
                }
            }
            catch { }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Unsubscribe();
                mic?.Dispose();
                enumerator?.UnregisterEndpointNotificationCallback(this);
                enumerator?.Dispose();
            }
        }
        public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
        {
            if (flow == DataFlow.Capture && role == Role.Multimedia)
            {
                Application.Current.Dispatcher.Invoke(BindToDefaultMic);
            }
        }
        public void OnDeviceAdded(string pwstrDeviceId) { }
        public void OnDeviceRemoved(string pwstrDeviceId) { }
        public void OnDeviceStateChanged(string pwstrDeviceId, DeviceState dwNewState) { }
        public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) { }
    }
}
