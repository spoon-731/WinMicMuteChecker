using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Windows;

namespace WinMicMuteChecker
{
    public class MicrophoneService : IDisposable, IMMNotificationClient
    {
        private MMDevice _microphone;
        private readonly OverlayWindow _overlay;
        private readonly MMDeviceEnumerator _deviceEnumerator;

        public MicrophoneService(OverlayWindow overlayWindow)
        {
            _overlay = overlayWindow;

            _deviceEnumerator = new MMDeviceEnumerator();
            BindToDefaultMic();
            if (_microphone != null)
            {
                _deviceEnumerator.RegisterEndpointNotificationCallback(this);
            }
        }

        private void BindToDefaultMic()
        {
            Unsubscribe();
            _microphone?.Dispose();

            _microphone = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
            if (_microphone != null)
            {
                Console.WriteLine(_microphone);
                _microphone.AudioEndpointVolume.OnVolumeNotification += VolumeChanged;
                UpdateOverlay();
            }
        }

        private void VolumeChanged(AudioVolumeNotificationData data)
        {
            Application.Current.Dispatcher.Invoke(UpdateOverlay);
        }

        private void UpdateOverlay()
        {
            if (_microphone.AudioEndpointVolume.Mute)
            {
                _overlay.ShowOverlay();
            }
            else
            {
                _overlay.HideOverlay();
            }
        }

        public void ToggleMute()
        {
            if (_microphone == null) return;
            _microphone.AudioEndpointVolume.Mute = !_microphone.AudioEndpointVolume.Mute;
        }

        private void Unsubscribe()
        {
            try
            {
                if (_microphone != null && _microphone.AudioEndpointVolume != null)
                {
                    _microphone.AudioEndpointVolume.OnVolumeNotification -= VolumeChanged;
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
                _microphone?.Dispose();
                _deviceEnumerator?.UnregisterEndpointNotificationCallback(this);
                _deviceEnumerator?.Dispose();
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
