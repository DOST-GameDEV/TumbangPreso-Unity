using System.Collections;
using UnityEngine;

namespace TumbangPreso.Audio
{
    /// <summary>
    /// The music bed, and the crossfade between menu and match.
    ///
    /// ⚠️ TWO SOURCES, NOT ONE. A single source cannot cross-fade with itself: swapping its
    /// clip cuts the old track dead on the frame the new one starts, and "round 1 begins" would
    /// chop the menu bed off mid-bar. Two sources, one fading down while the other fades up.
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
        public const float CrossfadeTime = AudioCues.MusicCrossfadeTime;

        /// <summary>See the class note: a starting point, not a measured value.</summary>
        public const float BedLevel = 0.55f;

        private AudioSource _a;
        private AudioSource _b;
        private bool _aIsActive;
        private Coroutine _fade;

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

            if (_fade != null) StopCoroutine(_fade);
            _fade = StartCoroutine(Crossfade(clip));
        }

        public void Stop()
        {
            Current = null;

            if (_fade != null) StopCoroutine(_fade);
            _fade = StartCoroutine(Crossfade(null));
        }

        private IEnumerator Crossfade(AudioClip next)
        {
            AudioSource outgoing = _aIsActive ? _a : _b;
            AudioSource incoming = _aIsActive ? _b : _a;
            _aIsActive = !_aIsActive;

            float target = MusicVolume();

            if (next != null)
            {
                incoming.clip = next;
                incoming.volume = 0.0f;
                incoming.Play();
            }

            float startOut = outgoing.volume;
            float t = 0.0f;

            while (t < CrossfadeTime)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / CrossfadeTime);

                outgoing.volume = Mathf.Lerp(startOut, 0.0f, k);
                if (next != null) incoming.volume = Mathf.Lerp(0.0f, target, k);

                yield return null;
            }

            outgoing.Stop();
            outgoing.clip = null;
            outgoing.volume = 0.0f;

            if (next != null) incoming.volume = target;
            _fade = null;
        }

        /// <summary>Applied live, so moving the slider in the settings panel is audible at once.</summary>
        private static float MusicVolume()
        {
            var s = Settings.SettingsStore.Current;
            return BedLevel * s.MusicVolume * s.MasterVolume;
        }

        private void Update()
        {
            // Track the settings sliders without needing them to notify us.
            if (_fade != null) return;

            AudioSource active = _aIsActive ? _a : _b;
            if (active.isPlaying) active.volume = MusicVolume();
        }
    }
}
