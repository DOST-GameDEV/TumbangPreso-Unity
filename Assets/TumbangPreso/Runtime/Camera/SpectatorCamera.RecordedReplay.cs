using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    // Owner, October 31: "replay is SO BLURRY". The picture ring stores 640x360 frames ten times a second. When the moment
    // was recorded whole, the replay is drawn from that record instead, at screen resolution with a shot that follows the
    // play, the same as halftime's. The ring stays as the fallback for anything with no record.
    public sealed partial class SpectatorCamera
    {
        private const float RecordedClipFreshSeconds = 14.0f;
        private MatchReplayArchive _boundArchive;
        private RecordedMatchClip _freshClip, _recordedClip;
        private float _freshClipAt, _recordedElapsed;
        private RecordedWorldView _recordedView;

        private void OnRecordedClip(MatchReplayArchive.Retained retained) => NoteFreshClip(retained.Clip);

        private void NoteFreshClip(RecordedMatchClip clip)
        {
            if (clip == null || (_freshClip != null && _freshClip.Id == clip.Id && _freshClip.MatchId == clip.MatchId)) return;
            _freshClip = clip; _freshClipAt = Time.unscaledTime;
        }

        private void WatchRecordedClips()
        {
            if (_boundArchive == null)
            {
                _boundArchive = FindAnyObjectByType<MatchReplayArchive>();
                if (_boundArchive != null) _boundArchive.RecordedClip += OnRecordedClip;
            }
            if (!NetAuthority.ShouldResolve()) NoteFreshClip(Net.MatchRpc.Instance?.NewestReceivedReplay);
        }

        private void UnwatchRecordedClips()
        {
            if (_boundArchive != null) _boundArchive.RecordedClip -= OnRecordedClip;
            _boundArchive = null;
        }

        /// <summary>For a capture: play the picture ring even when a recorded clip exists.</summary>
        public static bool ProbeUsePictureRing;

        private bool TryStartRecordedReplay()
        {
            var match = GameServices.Match;
            if (ProbeUsePictureRing || _freshClip == null || match == null || Time.unscaledTime - _freshClipAt > RecordedClipFreshSeconds
                || _freshClip.MatchId != match.PresentationMatchId || _freshClip.Round != match.RoundNumber) return false;
            RecordedWorldView view = null;
            try
            {
                view = new RecordedWorldView(transform, _freshClip);
                if (!view.Ready) { view.Dispose(); return false; }
                view.ShowOnScreen(false);
                view.Draw(_freshClip.Start, false);
            }
            catch (System.Exception failure)
            {
                Debug.LogWarning("[Replay] Recorded spectator view unavailable: " + failure.Message);
                view?.Dispose();
                return false;
            }
            _recordedView = view; _recordedClip = _freshClip; _recordedElapsed = 0.0f;
            _replayWipe = UI.ReplayStinger.Cover(transform, UI.HudDraw.Honey);
            if (_replayWipe == null) _recordedView.ShowOnScreen(true);
            _replayExit = null;
            _replaying = true;
            return true;
        }

        private void StepRecordedReplay()
        {
            if (_replayWipe != null)
            {
                if (!_replayWipe.IsCovered) return;
                _recordedView.ShowOnScreen(true);
                _replayWipe.Release();
                _replayWipe = null;
            }
            if (_replayExit != null)
            {
                if (_replayExit.IsCovered) FinishReplay(true);
                return;
            }
            _recordedElapsed += Time.unscaledDeltaTime;
            float offset = HalftimePresentation.ReplayOffset(_recordedElapsed, _recordedClip.Contact - _recordedClip.Start);
            if (offset >= _recordedClip.Duration) { EndReplay(); return; }
            try { _recordedView.Draw(_recordedClip.Start + offset); }
            catch (System.Exception failure)
            {
                Debug.LogWarning("[Replay] Recorded spectator view failed: " + failure.Message);
                EndReplay(false);
            }
        }

        private void DisposeRecordedReplay()
        {
            _recordedView?.Dispose();
            _recordedView = null; _recordedClip = null;
        }
    }
}
