using System;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// The voices of the Eskinita Alley's animals: the chickens of <see cref="AlleyChickens"/> (clucks,
    /// the rooster's crow, the squawk of a scare or a burst, wings) and the cats and dogs of
    /// <see cref="AlleyPets"/> (meows, a hiss, a purr, barks, a growl, a whine, panting, a yelp, the
    /// scrabble of claws). They ask by cue name; a cue with no clips is silence, never an error.
    ///
    /// ⚠️ REAL RECORDINGS ONLY (the owner has thrown out synthesised sound twice). Every clip is cut
    /// from a Creative Commons 0 recording by tools/build_eskinita_life_sfx.py; the sources and their
    /// licences are in tools/eskinita_life_sfx_sources.json. The only thing done to a clip here is a
    /// few percent of playback-rate variation so one bark is not the same bark every time.
    ///
    /// ⚠️ THE STREET'S MIX, NOT THE MATCH'S, exactly as `SidewalkLife.Sound`: 3D voices of its own
    /// (logarithmic rolloff, no Doppler, priority 200: the first to give way), on the player's
    /// AMBIENCE slider (`AudioDirector.AmbienceVolume` x `KantoStreetSound.AmbientGainScale`), ducked
    /// to nothing in a replay, held while the game is paused, silent in the menus' map preview, and
    /// under the match's own cues. <see cref="Gain"/> is the one knob for the owner's listening.
    /// ⚠️ PRESENTATION ONLY: nothing networked, no gameplay random stream.
    /// </summary>
    [DefaultExecutionOrder(1001)]
    [DisallowMultipleComponent]
    public sealed class AlleyLifeSound : MonoBehaviour
    {
        [Serializable]
        public sealed class Bank
        {
            public string Cue;
            public AudioClip[] Clips = Array.Empty<AudioClip>();
            [Range(0, 1)] public float Gain = .6f;
            public float Near = 2f, Far = 24f;       // metres: full level within Near, gone by Far
        }

        public Bank[] Banks = Array.Empty<Bank>();
        /// <summary>Every animal voice at once, for the owner's listening session.</summary>
        [Range(0, 1.5f)] public float Gain = .8f;

        /// <summary>How many cues have been asked for since the scene loaded, played or not (for probes).</summary>
        public int Asked { get; private set; }
        /// <summary>The last cue asked for (for probes and the motion sheet's log).</summary>
        public string LastCue { get; private set; } = "";

        private const int VoiceCount = 8;
        private AudioSource[] _voices = Array.Empty<AudioSource>();
        private readonly float[] _voiceGain = new float[VoiceCount];
        private int _nextVoice;
        private readonly System.Random _random = new System.Random(0x414C4C59);

        private bool IsPreview() => MatchInstaller.PreviewOnly || gameObject.layer == UI.MapPreviewSurface.PreviewLayer;

        private static float Mix()
        {
            var director = GameServices.Audio;
            if (director == null) return KantoStreetSound.AmbientGainScale;
            return director.IsInReplayMix ? 0f : director.AmbienceVolume * KantoStreetSound.AmbientGainScale;
        }

        /// <summary>Plays one of the cue's clips at a place. Quietly does nothing when it cannot.</summary>
        public void Play(string cue, Vector3 at, float gain = 1f)
        {
            Asked++; LastCue = cue;
            if (!Application.isPlaying || !isActiveAndEnabled || IsPreview() || AudioListener.pause) return;
            Bank bank = null;
            foreach (var b in Banks) if (b != null && b.Cue == cue) { bank = b; break; }
            if (bank == null || bank.Clips == null || bank.Clips.Length == 0) return;
            var clip = bank.Clips[_random.Next(bank.Clips.Length)];
            float level = Mathf.Min(1f, bank.Gain * gain * Gain);
            if (clip == null || level <= .001f) return;
            var camera = Camera.main;
            if (camera != null && (camera.transform.position - at).sqrMagnitude > bank.Far * bank.Far * 1.3f) return;
            if (_voices.Length == 0) Build();
            int pick = -1;
            for (int i = 0; i < VoiceCount && pick < 0; i++)
            {
                int v = (_nextVoice + i) % VoiceCount;
                if (!_voices[v].isPlaying) pick = v;
            }
            if (pick < 0) pick = _nextVoice;
            _nextVoice = (pick + 1) % VoiceCount;
            var source = _voices[pick];
            source.Stop();
            source.transform.position = at;
            source.clip = clip;
            source.pitch = .96f + (float)_random.NextDouble() * .09f;
            source.minDistance = bank.Near; source.maxDistance = bank.Far;
            _voiceGain[pick] = level;
            source.volume = level * Mix();
            source.Play();
        }

        private void Build()
        {
            _voices = new AudioSource[VoiceCount];
            for (int i = 0; i < VoiceCount; i++)
            {
                var go = new GameObject("AlleyLifeVoice" + i);
                go.transform.SetParent(transform, false);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false; src.loop = false;
                src.spatialBlend = 1f; src.rolloffMode = AudioRolloffMode.Logarithmic;
                src.dopplerLevel = 0f; src.priority = 200;
                _voices[i] = src;
            }
        }

        private void LateUpdate()
        {
            if (_voices.Length == 0) return;
            // The slider and a replay's duck reach a voice already in the air; a preview silences them all.
            bool preview = IsPreview();
            float mix = Mix();
            for (int i = 0; i < VoiceCount; i++)
            {
                if (!_voices[i].isPlaying) continue;
                if (preview) _voices[i].Stop(); else _voices[i].volume = _voiceGain[i] * mix;
            }
        }

        private void OnDisable()
        {
            foreach (var v in _voices) if (v != null) v.Stop();
        }
    }
}
