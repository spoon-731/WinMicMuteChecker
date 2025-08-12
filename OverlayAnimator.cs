using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace WinMicMuteChecker
{
    public sealed class OverlayAnimator : IDisposable
    {
        private readonly Window _overlay;
        private CancellationTokenSource _cts = new();
        private bool _targetVisible;
        private bool _disposed;

        // tuning
        private static readonly TimeSpan Debounce = TimeSpan.FromMilliseconds(120);
        private static readonly Duration FadeIn = new(TimeSpan.FromMilliseconds(160));
        private static readonly Duration FadeOut = new(TimeSpan.FromMilliseconds(160));

        public OverlayAnimator(Window overlay)
        {
            _overlay = overlay;
        }

        public async Task SetVisibleAsync(bool visible)
        {
            if (_disposed) return;

            _targetVisible = visible;

            // 1) Debounce: cancel last request
            _cts.Cancel();
            _cts.Dispose();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try { await Task.Delay(Debounce, token); }
            catch (TaskCanceledException) { return; }
            if (token.IsCancellationRequested) return;

            // 2) Stop previous animation
            _overlay.BeginAnimation(UIElement.OpacityProperty, null, HandoffBehavior.SnapshotAndReplace);

            if (visible)
            {
                if (_overlay.Visibility != Visibility.Visible)
                {
                    _overlay.Opacity = 0.0;
                    _overlay.Visibility = Visibility.Visible;
                }

                var fadeIn = new DoubleAnimation
                {
                    From = _overlay.Opacity,
                    To = 1.0,
                    Duration = FadeIn,
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                _overlay.BeginAnimation(UIElement.OpacityProperty, fadeIn, HandoffBehavior.SnapshotAndReplace);
            }
            else
            {
                var fadeOut = new DoubleAnimation
                {
                    From = _overlay.Opacity,
                    To = 0.0,
                    Duration = FadeOut,
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
                    FillBehavior = FillBehavior.Stop
                };

                // 3) hide only if target is hidden and request wasn't cancelled
                fadeOut.Completed += (s, e) =>
                {
                    if (!token.IsCancellationRequested && _targetVisible == false)
                    {
                        _overlay.Opacity = 0.0;
                        _overlay.Visibility = Visibility.Hidden;
                    }
                };

                _overlay.BeginAnimation(UIElement.OpacityProperty, fadeOut, HandoffBehavior.SnapshotAndReplace);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _cts.Cancel();
            _cts.Dispose();

            // remove current animations
            _overlay?.BeginAnimation(UIElement.OpacityProperty, null, HandoffBehavior.SnapshotAndReplace);
        }
    }
}
