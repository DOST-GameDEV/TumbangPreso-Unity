using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    // Owner, October 31: "replay is SO BLURRY". The picture ring stores 640x360 frames ten times a second. When the moment
    // was recorded whole, the replay is drawn from that record instead, at screen resolution with a shot that follows the
    // play, the same as halftime's. The ring stays as the fallback for anything with no record.
    public sealed partial class SpectatorCamera
    {
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
            StepClipWait();
        }

        private void UnwatchRecordedClips()
        {
            if (_boundArchive != null) _boundArchive.RecordedClip -= OnRecordedClip;
            _boundArchive = null;
        }

        /// <summary>For a capture: play the picture ring even when a recorded clip exists.</summary>
        public static bool ProbeUsePictureRing;

        // ⚠️ THE SHARP PICTURE IS WORTH A MOMENT'S WAIT (owner, November 1: "badtrip ang blurry neto"). A moment's clip is sealed
        // 1.4 s of game time after it, later still through a hit's slow motion, and a replay pressed before that took the ring.
        // The wipe holds over the wait (owner: "hold the replay card"). A host need not wait for a clip the archive will never
        // seal, a retrieval say: it cuts one from the history that every moment already has. A client can only be sent one.
        private const float ClipWaitSeconds = 4.0f, HostWaitSeconds = 2.2f;
        private UI.ReplayStinger _clipWaitCard;
        private float _clipWaitUntil = -1.0f;
        private string _clipWaitReason;
        private bool _clipWaitOver;

        private float NewestMarkedAt()
        {
            int marked = NewestHighlightIndex();
            return marked < 0 ? -1.0f : _replayFrames[marked].EventAt > 0.0f ? _replayFrames[marked].EventAt : _replayFrames[marked].CapturedAt;
        }

        private void StepClipWait()
        {
            if (_clipWaitUntil < 0.0f) return;
            if (_replaying) { _clipWaitUntil = -1.0f; ReleaseClipWaitCard(); return; }
            if (_freshClip != null && NewestMarkedAt() <= _freshClipAt || Time.unscaledTime >= _clipWaitUntil)
            {
                _clipWaitUntil = -1.0f; _clipWaitOver = true;
                StartReplay(_clipWaitReason);
                _clipWaitOver = false;
                ReleaseClipWaitCard();
            }
        }

        private void ReleaseClipWaitCard()
        {
            if (_clipWaitCard != null) _clipWaitCard.Release();
            _clipWaitCard = null;
        }

        // The wipe already over the screen if a wait held one, else a new one.
        private UI.ReplayStinger TakeReplayWipe()
        {
            var card = _clipWaitCard != null ? _clipWaitCard : UI.ReplayStinger.Cover(transform, UI.HudDraw.Honey);
            _clipWaitCard = null;
            return card;
        }

        private bool ClipHolds(float markedAt)
        {
            var match = GameServices.Match;
            return _freshClip != null && match != null && _freshClip.MatchId == match.PresentationMatchId
                && _freshClip.Round == match.RoundNumber && markedAt <= _freshClipAt;
        }

        private string _pendingReplayReason;

        private bool TryStartRecordedReplay()
        {
            var match = GameServices.Match;
            // ⚠️ A CLIP GOES OLD WHEN SOMETHING NEWER HAPPENS, NOT ON A TIMER. A fixed 14 s sent a watcher who pressed a little late to
            // the blurry ring for the very moment the clip holds. A clip arrives after its own marker, so a marker later than the
            // clip's arrival is a newer moment, and only then does the ring know more than the record.
            if (_clipWaitUntil >= 0.0f) return true;
            float markedAt = NewestMarkedAt();
            if (!ProbeUsePictureRing && match != null && !ClipHolds(markedAt))
            {
                bool host = NetAuthority.ShouldResolve();
                float wait = host ? HostWaitSeconds : ClipWaitSeconds, age = markedAt >= 0.0f ? Time.unscaledTime - markedAt : float.MaxValue;
                if (!_clipWaitOver && age < wait)
                {
                    _clipWaitUntil = markedAt + wait; _clipWaitReason = _pendingReplayReason;
                    _clipWaitCard = UI.ReplayStinger.Cover(transform, UI.HudDraw.Honey);
                    return true;
                }
                if (host && _boundArchive != null)
                {
                    int marked = NewestHighlightIndex();
                    var cut = _boundArchive.CaptureMoment(markedAt >= 0.0f ? Time.time - age : Time.time - 1.4f, marked >= 0 ? _replayFrames[marked].Slot : -1,
                        marked >= 0 ? _replayFrames[marked].Reason : _pendingReplayReason);
                    if (cut != null) { _freshClip = cut; _freshClipAt = Time.unscaledTime; }
                }
            }
            string refused = ProbeUsePictureRing ? "a capture asked for it"
                : _freshClip == null ? "no recorded clip, " + _boundArchive?.LastSkip
                : match == null || _freshClip.MatchId != match.PresentationMatchId || _freshClip.Round != match.RoundNumber ? "the clip is from another round"
                : markedAt > _freshClipAt ? "a newer moment has no clip yet, " + _boundArchive?.LastSkip
                : null;
            if (refused != null)
            {
                Debug.Log("[Replay] Spectator uses the picture ring: " + refused);
                return false;
            }
            RecordedWorldView view = null;
            try
            {
                view = new RecordedWorldView(transform, _freshClip);
                if (!view.Ready) { Debug.Log("[Replay] Recorded spectator view not ready: " + view.UnavailableReason); view.Dispose(); return false; }
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
            _replayWipe = TakeReplayWipe();
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
