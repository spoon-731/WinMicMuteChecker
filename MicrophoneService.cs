using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace WinMicMuteChecker
{
    public class MicrophoneService : IDisposable, IMMNotificationClient
    {
        private MMDevice _microphone;
        private readonly MMDeviceEnumerator _deviceEnumerator;
        private readonly OverlayAnimator _overlayAnimator;

        private bool _callbackRegistered;
        private bool? _lastMuted;

        public MicrophoneService(OverlayAnimator overlayAnimator)
        {
            _overlayAnimator = overlayAnimator;

            _deviceEnumerator = new MMDeviceEnumerator();
            BindToDefaultMic();
            if (_microphone != null)
            {
                _deviceEnumerator.RegisterEndpointNotificationCallback(this);
                _callbackRegistered = true;
            }
        }

        private void BindToDefaultMic()
        {
            Unsubscribe();
            _microphone?.Dispose();
            _microphone = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
            
            _microphone ??= _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);

            if (_microphone != null)
            {
                Console.WriteLine(_microphone);
                _microphone.AudioEndpointVolume.OnVolumeNotification += VolumeChanged;
                _ = UpdateOverlay();
            }
        }
        private void VolumeChanged(AudioVolumeNotificationData data)
        {
            Application.Current.Dispatcher.BeginInvoke(async () => await UpdateOverlay());
        }

        private Task UpdateOverlay()
        {
            if (_microphone == null) return Task.CompletedTask;

            bool muted;
            try { muted = _microphone.AudioEndpointVolume?.Mute ?? false; }
            catch { return Task.CompletedTask; }

            if (_lastMuted == muted) return Task.CompletedTask;
            _lastMuted = muted;

            return _overlayAnimator.SetVisibleAsync(muted);
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
                if (_callbackRegistered)
                {
                    _deviceEnumerator.UnregisterEndpointNotificationCallback(this);
                    _callbackRegistered = false;
                }
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
