using UnityEngine;

namespace TumbangPreso.Audio
{
    /// <summary>
    /// The music bed.
    ///
    /// ⚠️⚠️ EVERY TRACK CHANGE IS A CUT. THERE IS NO CROSSFADE, AND REMOVING IT IS THE ASK ON
    /// BOTH BUILDS. 🧑 on this port: *"please remove music fade, js let it end abruptly"*, and
    /// on the original before it: *"pls js abruptly cut it"*. `audio_manager.gd` carries the
    /// same conclusion at all three of its edges — the menu bed starts with `play_music("menu",
    /// 0.0)`, the match bed with `play_music("match", 0.0)`, and entering the arena calls
    /// `stop_music_now()` rather than fading. Its own notes record why: a cross-fade needs
    /// something to fade FROM, and at every one of these edges there is nothing, so the ramp was
    /// pure latency. *"Remove the audio playback delay when transitioning from the intro
    /// video"*, *"Remove the audio latency during round initialization"*. The 1.5 s constant
    /// survives in the cue table and is used by nothing.
    ///
    /// ⚠️ TWO SOURCES ARE KEPT EVEN THOUGH NOTHING CROSSFADES. Swapping the clip on a single
    /// playing source is a stall on the audio thread while the new clip loads; handing the new
    /// track to the idle source and stopping the old one is a clean frame-accurate cut, which
    /// is what "abruptly" has to mean to be an improvement rather than a click.
    ///
    /// ⚠️ THE BED STARTS QUIETER THAN EVERYTHING ELSE, DELIBERATELY. The SFX bus was measured
    /// clipping at +2.0 dBFS with music silent, and the delivered OST masters' own peak was
    /// never measured. Rather than ship the bed at the SFX table's reference and risk repeating
    /// that the moment a real match layers impacts, the tag, voice and music at once, it starts
    /// low. **This is a starting point, not a measurement**, and the thing that should move it
    /// is a mix probe against a recorded match with the OST actually playing.
    /// </summary>
    public sealed class MusicDirector : MonoBehaviour
    {
        /// <summary>See the class note: a starting point, not a measured value.</summary>
        public const float BedLevel = 0.55f;

        private AudioSource _a;
        private AudioSource _b;
        private bool _aIsActive;

        public string Current { get; private set; }

        private void Awake()
        {
            _a = gameObject.AddComponent<AudioSource>();
            _b = gameObject.AddComponent<AudioSource>();

            foreach (var s in new[] { _a, _b })
            {
                s.loop = true;
                s.playOnAwake = false;
                s.volume = 0.0f;

                // ⚠️ 2D, NOT POSITIONAL. A music bed panned by where the listener happens to
                // stand is a bug that presents as "the music is quiet on one side of the map".
                s.spatialBlend = 0.0f;
            }
        }

        /// <summary>
        /// Swap to a track by cue name ("menu" or "match"). Re-requesting the track that is
        /// already playing does nothing, so a screen that calls this in OnEnable does not
        /// restart the bed every time the player opens a submenu.
        /// </summary>
        public void Play(string cue, AudioClip clip)
        {
            if (Current == cue) return;
            Current = cue;

            Cut(clip);
        }

        /// <summary>
        /// Drop the bed under a voice line, then bring it back.
        ///
        /// ⚠️⚠️ THE RECOVERY IS SLOWER THAN THE DROP, AND THAT IS MEASURED. The duck used to
        /// recover in about the time a line takes to say, so the bed was climbing back
        /// underneath the last word and the whole thing pumped. Dropping fast and recovering
        /// slowly is inaudible; the reverse is the artefact.
        ///
        /// ⚠️ AND IT DOES NOT OUT-SHOUT THE LINE INSTEAD. Raising the voice alone was tried:
        /// it made the announcer louder without making it clearer, because the bed was still
        /// sitting under it at the same level.
        /// </summary>
        public void Duck(float depthDb, float hold)
        {
            if (_duck != null) StopCoroutine(_duck);
            _duck = StartCoroutine(DuckRoutine(depthDb, hold));
        }

        private Coroutine _duck;
        private float _duckScale = 1.0f;

        /// <summary>The duck multiplier the crossfade must respect, or a fade would write
        /// straight over it and undo the duck mid-line.</summary>
        public float DuckScale => _duckScale;

        private System.Collections.IEnumerator DuckRoutine(float depthDb, float hold)
        {
            float target = Mathf.Pow(10.0f, depthDb / 20.0f);

            const float dropTime = 0.08f;
            const float recoverTime = 0.45f;

            float t = 0.0f;
            float from = _duckScale;

            while (t < dropTime)
            {
                t += Time.unscaledDeltaTime;
                _duckScale = Mathf.Lerp(from, target, t / dropTime);
                yield return null;
            }

            _duckScale = target;

            yield return new WaitForSecondsRealtime(hold);

            t = 0.0f;
            while (t < recoverTime)
            {
                t += Time.unscaledDeltaTime;
                _duckScale = Mathf.Lerp(target, 1.0f, t / recoverTime);
                yield return null;
            }

            _duckScale = 1.0f;
            _duck = null;
        }

        /// <summary>Silence, immediately. An alias for <see cref="StopNow"/> now that every
        /// transition is a cut and there is no other kind of stop left.</summary>
        public void Stop() => StopNow();

        /// <summary>
        /// Both players dead, this frame, with the name cleared.
        ///
        /// ⚠️⚠️ CLEARING <see cref="Current"/> IS NOT BOOKKEEPING. <see cref="Play"/> opens by
        /// returning early when the requested cue is already the current one, so stopping the
        /// sources while the name still reads "menu" makes the next `Play("menu")` a no-op and
        /// the menu bed never comes back — silence for the rest of the session, produced by the
        /// function whose whole job is to be quiet. `stop_music_now()` carries the same warning.
        /// </summary>
        public void StopNow() { Current = null; Cut(null); }

        private void Cut(AudioClip next)
        {
            AudioSource outgoing = _aIsActive ? _a : _b;
            AudioSource incoming = _aIsActive ? _b : _a;
            _aIsActive = !_aIsActive;

            outgoing.Stop();
            outgoing.clip = null;
            outgoing.volume = 0.0f;

            if (next == null) return;

            incoming.clip = next;
            incoming.volume = MusicVolume();
            incoming.Play();
        }

        /// <summary>
        /// The ONE place a music level is computed: bed level, the player's music slider, the
        /// master slider, and the announcer duck, multiplied together.
        ///
        /// ⚠️ THE DUCK BELONGS IN HERE, NOT WRITTEN ONTO THE SOURCE. Update re-applies this
        /// every frame so the settings sliders are audible at once — which means a duck poked
        /// directly onto `volume` is erased on the very next frame, and the announcer plays
        /// over an undimmed bed. That bug is invisible in code review and obvious in play.
        /// </summary>
        private float MusicVolume()
        {
            var s = Settings.SettingsStore.Current;
            return BedLevel * s.MusicVolume * s.MasterVolume * _duckScale;
        }

        private void Update()
        {
            // Track the settings sliders and the duck without needing either to notify us.
            AudioSource active = _aIsActive ? _a : _b;
            if (active.isPlaying) active.volume = MusicVolume();
        }
    }
}
