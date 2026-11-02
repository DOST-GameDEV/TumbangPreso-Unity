using UnityEngine;
using UnityEngine.Video;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// ⚠️ HOME MUST NEVER SIT FROZEN (owner, 2026-11-01: "it randomly freezes or pauses or doesnt play sometimes"). A video decoder can
    /// stop without raising an error, or never finish preparing. Nothing else would notice, so this watches the clock that matters
    /// while HOME is really on screen, and starts again: a stuck film gives way to the idle, a stuck idle gets a new decoder, and
    /// the poster is the last resort. Time is counted in capped steps, so a long frame (a map loading) is not a stall.
    /// </summary>
    public sealed partial class HubSceneVideo
    {
        private const float StallSeconds = 2.5f;
        private const float PrepareSeconds = 12f;
        private const int MaxRecoveries = 3;

        private float _stuckFor;
        private double _lastTime = -1;
        private int _recoveries;

        private void ResetWatchdog() { _stuckFor = 0; _lastTime = -1; }

        private void Watch()
        {
            if (_failed || _preloading || !Application.isFocused) { ResetWatchdog(); return; }
            float step = Mathf.Min(Time.unscaledDeltaTime, .1f);
            var active = _filmEnded ? _next : Player;
            if (active == null) { ResetWatchdog(); return; }
            float limit = StallSeconds;
            bool stuck;
            if (_filmEnded ? !_nextPrepared : !Prepared || !FirstFrameReady) { stuck = true; limit = PrepareSeconds; }
            else if (!active.isPlaying) stuck = _filmEnded || !IsAtFilmEnd(active);
            else
            {
                stuck = active.time == _lastTime;
                _lastTime = active.time;
            }
            _stuckFor = stuck ? _stuckFor + step : 0;
            if (_stuckFor < limit) return;
            Debug.LogWarning($"[HubSceneVideo] {active.clip?.name} stopped advancing at {active.time:F2} s; recovering.");
            ResetWatchdog();
            if (!Recover()) OnError(active, "Giving up after repeated stalls; showing the poster.");
        }

        private static bool IsAtFilmEnd(VideoPlayer player) => !player.isLooping && player.frame >= (long)player.frameCount - 1;

        /// <summary>Drop every decoder and start the idle fresh. False once it has been tried enough times this hub.</summary>
        private bool Recover()
        {
            if (_recoveries >= MaxRecoveries) return false;
            _recoveries++;
            if (_film != null) FilmsSeen.Add(Hero);
            DropPlayers();
            _film = null; _filmPoster = null; _filmEnded = false;
            _next = null; _nextPreparing = false; _nextPrepared = false;
            Prepared = false; FirstFrameReady = false;
            _image.texture = _poster;
            PreparePlayback();
            return true;
        }

        private void DropPlayers()
        {
            if (Player != null)
            {
                Player.prepareCompleted -= OnPrepared;
                Player.frameReady -= OnFirstFrame;
                Player.errorReceived -= OnError;
                Player.loopPointReached -= OnFilmEnded;
            }
            if (_next != null)
            {
                _next.prepareCompleted -= OnIdlePrepared;
                _next.frameReady -= OnIdleFrame;
                _next.errorReceived -= OnError;
            }
            ReleaseTarget();
            if (Player != null) { Player.Stop(); Destroy(Player); Player = null; }
            if (_next != null) { _next.Stop(); Destroy(_next); _next = null; }
        }
    }
}
