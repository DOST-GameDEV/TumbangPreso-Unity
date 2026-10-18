using System;
using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// WHAT THE PAYO MAP SOUNDS LIKE, from real recordings only (CC0, cut and levelled by `tools/build_payo_sfx.py`, which
    /// records every source and its licence in `tools/payo_sfx_sources.json`; nothing here is synthesized). The pattern
    /// is the run's and the alley's soundscapes: clips are serialized fields the scene builder fills, the sources are
    /// made at run time, and every level is the player's ambience volume times the street's own trim.
    ///
    ///   * BEDS (2D loops): mountain wind in leaves, always, louder in gusts; the DAY's birds and the NIGHT's frogs and
    ///     insects, crossed by `PayoNight.Factor`; the village, faint, nearer the gate.
    ///   * LOOPS IN THE WORLD: falling water at the foot of the fall nearest the ear.
    ///   * SHOTS IN THE WORLD, a few seconds apart, never the same kind twice running: the hawk's cry over the valley
    ///     and a rooster in the hamlets by day; single frogs close by at night.
    ///   * THE GAME'S OWN MOMENTS, from state every peer has: stone grinding while the monoliths travel in a break and
    ///     a thud as each locks (`PayoStageShow`), a gong from the gate when a round ends, the geyser's rush when it
    ///     throws (`PayoGeyser` asks for it), the hawk's cry when it comes for a fallen body.
    ///
    /// ⚠️ NOTHING IS SENT (never `NetCue`): every peer plays these for itself. Silent in the menus' map preview, in a
    /// replay's mix, and while the listener is paused.
    /// </summary>
    [DefaultExecutionOrder(1001)]
    public sealed class PayoSound : MonoBehaviour
    {
        public enum When { Always, Day, Night }

        [Serializable] public sealed class Loop { public string Name; public AudioClip Clip; public Vector3[] Points; [Range(0, 1)] public float Gain = 0.4f; public float Near = 4f, Far = 45f; public When When; }
        [Serializable] public sealed class Shot
        {
            public string Name; public AudioClip[] Clips; public Vector3[] Points; [Range(0, 1)] public float Gain = 0.4f;
            public float Weight = 1f, Near = 4f, Far = 60f, Scatter = 2f, Lift; public When When;
        }

        public static PayoSound Instance { get; private set; }

        public AudioClip Wind, Day, Night, Village;
        [Range(0, 1)] public float WindGain = 0.34f, DayGain = 0.36f, NightGain = 0.42f, VillageGain = 0.16f;
        [Range(0, 1.5f)] public float BedsGain = 0.6f, WorldGain = 0.7f, EventGain = 0.8f;
        public Vector3 VillageAt, GongAt;
        public Loop[] Loops = Array.Empty<Loop>();
        public Shot[] Shots = Array.Empty<Shot>();
        public Vector2 ShotGap = new Vector2(3.5f, 9f);
        public AudioClip Grind;
        public AudioClip[] Lock = Array.Empty<AudioClip>(), Gong = Array.Empty<AudioClip>(), Geyser = Array.Empty<AudioClip>(), Hawk = Array.Empty<AudioClip>();

        /// <summary>How many world shots and game moments have been played (for a probe).</summary>
        public int ShotsPlayed { get; private set; }
        public int EventsPlayed { get; private set; }

        private const int Voices = 6;
        private AudioSource _wind, _day, _night, _village, _grind;
        private AudioSource[] _loops = Array.Empty<AudioSource>();
        private readonly AudioSource[] _voices = new AudioSource[Voices];
        private readonly float[] _voiceGain = new float[Voices];
        private int _voice, _lastShot = -1;
        private float _fade, _mix, _nextShot = 4f, _grindLevel;
        private bool _playing, _made;
        private System.Random _random = new System.Random(5150);
        private MatchDirector _match;

        private bool IsPreview() => MatchInstaller.PreviewOnly || gameObject.layer == UI.MapPreviewSurface.PreviewLayer;
        private float Rand(float a, float b) => a + (b - a) * (float)_random.NextDouble();

        private void OnEnable()
        {
            Instance = this;
            PayoStageShow.Locked += OnLocked;
        }

        private void OnDisable()
        {
            if (Instance == this) Instance = null;
            PayoStageShow.Locked -= OnLocked;
            Unhook();
            StopAll();
        }

        private void Unhook()
        {
            if (_match != null) { _match.IntermissionStarted -= OnRoundEnd; _match.MatchEnded -= OnMatchEnd; }
            _match = null;
        }

        private AudioSource Source(string name, bool world, bool loop, int priority)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false; s.loop = loop; s.dopplerLevel = 0f; s.volume = 0f; s.priority = priority;
            s.spatialBlend = world ? 1f : 0f;
            if (world) s.rolloffMode = AudioRolloffMode.Logarithmic;
            return s;
        }

        private void Make()
        {
            if (_made) return;
            _made = true;
            _wind = Source("bed wind", false, true, 40); _day = Source("bed day", false, true, 40);
            _night = Source("bed night", false, true, 40); _village = Source("bed village", false, true, 40);
            _grind = Source("stone grinding", false, true, 60);
            _wind.clip = Wind; _day.clip = Day; _night.clip = Night; _village.clip = Village; _grind.clip = Grind;
            _loops = new AudioSource[Loops.Length];
            for (int i = 0; i < Loops.Length; i++)
            {
                _loops[i] = Source("loop " + Loops[i].Name, true, true, 120);
                _loops[i].clip = Loops[i].Clip; _loops[i].minDistance = Loops[i].Near; _loops[i].maxDistance = Loops[i].Far;
            }
            for (int i = 0; i < Voices; i++) _voices[i] = Source("voice " + i, true, false, 180);
        }

        private void StartBeds()
        {
            foreach (var s in new[] { _wind, _day, _night, _village })
            {
                if (s == null || s.clip == null) continue;
                s.Play();
                s.timeSamples = _random.Next(Mathf.Max(1, s.clip.samples));        // never the same start twice
            }
            foreach (var s in _loops) if (s != null && s.clip != null) { s.Play(); s.timeSamples = _random.Next(Mathf.Max(1, s.clip.samples)); }
        }

        private void StopAll()
        {
            if (!_made) return;
            foreach (var s in new[] { _wind, _day, _night, _village, _grind }) if (s != null) s.Stop();
            foreach (var s in _loops) if (s != null) s.Stop();
            foreach (var s in _voices) if (s != null) s.Stop();
            _playing = false; _fade = 0f;
        }

        private void LateUpdate()
        {
            if (IsPreview()) { if (_playing) StopAll(); return; }
            if (AudioListener.pause) return;
            Make();
            if (!_playing) { _playing = true; StartBeds(); }

            var match = GameServices.Match;
            if (match != _match) { Unhook(); _match = match; if (match != null) { match.IntermissionStarted += OnRoundEnd; match.MatchEnded += OnMatchEnd; } }

            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            _fade = Mathf.MoveTowards(_fade, 1f, dt / 2f);
            var director = GameServices.Audio;
            float slider = (director != null ? director.AmbienceVolume : 1f) * KantoStreetSound.AmbientGainScale;
            bool replay = director != null && director.IsInReplayMix;
            _mix = Mathf.MoveTowards(_mix, replay ? 0f : slider, dt / (replay ? 0.12f : 0.35f));
            float level = _mix * _fade * _fade * (3f - 2f * _fade);

            var main = Camera.main;
            Vector3 ear = main != null ? main.transform.position : Vector3.zero;
            float night = PayoNight.Factor;

            // ---- the beds
            float gust = 0.7f + 0.3f * Mathf.PerlinNoise(Time.unscaledTime * 0.11f, 3.7f) + 0.25f * Mathf.PerlinNoise(Time.unscaledTime * 0.43f, 9.1f);
            if (_wind != null) _wind.volume = level * BedsGain * WindGain * gust * Mathf.Lerp(1f, 0.75f, night);
            if (_day != null) _day.volume = level * BedsGain * DayGain * (1f - night) * (1f - night);
            if (_night != null) _night.volume = level * BedsGain * NightGain * night * night;
            if (_village != null)
            {
                float near = Mathf.Lerp(0.3f, 1f, 1f - Mathf.Clamp01(Vector3.Distance(ear, VillageAt) / 90f));
                _village.volume = level * BedsGain * VillageGain * near * Mathf.Lerp(1f, 0.35f, night);
            }

            // ---- loops in the world: each at its point nearest the ear
            for (int i = 0; i < Loops.Length; i++)
            {
                var loop = Loops[i]; var s = _loops[i];
                if (s == null || loop.Points == null || loop.Points.Length == 0) continue;
                float best = float.MaxValue; Vector3 at = loop.Points[0];
                foreach (var p in loop.Points) { float d = (p - ear).sqrMagnitude; if (d < best) { best = d; at = p; } }
                s.transform.position = at;
                s.volume = level * WorldGain * loop.Gain * Share(loop.When, night);
            }

            // ---- shots in the world
            for (int i = 0; i < Voices; i++) if (_voices[i] != null && _voices[i].isPlaying) _voices[i].volume = level * _voiceGain[i];
            _nextShot -= dt;
            if (_nextShot <= 0f)
            {
                _nextShot = Rand(ShotGap.x, ShotGap.y);
                TryShot(ear, night);
            }

            // ---- the stone grinding while the monoliths travel
            var stage = ArenaStage.Instance;
            bool moving = stage != null && stage.Travelling && HalftimePresentation.Playing;
            _grindLevel = Mathf.MoveTowards(_grindLevel, moving ? 1f : 0f, dt / (moving ? 0.25f : 0.5f));
            if (_grind != null && _grind.clip != null)
            {
                if (_grindLevel > 0.001f && !_grind.isPlaying) { _grind.Play(); _grind.timeSamples = _random.Next(Mathf.Max(1, _grind.clip.samples)); }
                if (_grindLevel <= 0.001f && _grind.isPlaying) _grind.Stop();
                _grind.volume = level * EventGain * 0.55f * _grindLevel;
            }
        }

        private static float Share(When when, float night) => when == When.Day ? (1f - night) * (1f - night) : when == When.Night ? night * night : 1f;

        private void TryShot(Vector3 ear, float night)
        {
            float total = 0f;
            for (int i = 0; i < Shots.Length; i++) total += Chance(i, ear, night);
            if (total <= 0f) return;
            float pick = Rand(0f, total);
            for (int i = 0; i < Shots.Length; i++)
            {
                float c = Chance(i, ear, night);
                if (c <= 0f) continue;
                pick -= c;
                if (pick > 0f) continue;
                var shot = Shots[i];
                _lastShot = i;
                // The point of this kind nearest the ear, a little off it.
                float best = float.MaxValue; Vector3 at = shot.Points[0];
                foreach (var p in shot.Points) { float d = (p - ear).sqrMagnitude; if (d < best) { best = d; at = p; } }
                at += new Vector3(Rand(-shot.Scatter, shot.Scatter), shot.Lift, Rand(-shot.Scatter, shot.Scatter));
                Play(shot.Clips, at, WorldGain * shot.Gain, shot.Near, shot.Far, true);
                ShotsPlayed++;
                return;
            }
        }

        private float Chance(int index, Vector3 ear, float night)
        {
            var shot = Shots[index];
            if (index == _lastShot && Shots.Length > 1) return 0f;
            if (shot.Clips == null || shot.Clips.Length == 0 || shot.Points == null || shot.Points.Length == 0) return 0f;
            float share = Share(shot.When, night);
            if (share < 0.5f) return 0f;
            foreach (var p in shot.Points) if ((p - ear).sqrMagnitude < shot.Far * shot.Far) return shot.Weight;
            return 0f;
        }

        /// <summary>One clip of a set at a place in the world (or flat, for what is watched through the break's camera).</summary>
        private void Play(AudioClip[] clips, Vector3 at, float gain, float near, float far, bool world)
        {
            if (!_made || !_playing || clips == null || clips.Length == 0) return;
            var clip = clips[_random.Next(clips.Length)];
            if (clip == null) return;
            var s = _voices[_voice]; int index = _voice;
            _voice = (_voice + 1) % Voices;
            if (s == null) return;
            s.Stop();
            s.clip = clip; s.transform.position = at;
            s.spatialBlend = world ? 1f : 0f; s.minDistance = near; s.maxDistance = far;
            s.pitch = Rand(0.96f, 1.04f);
            _voiceGain[index] = gain;
            s.volume = _mix * _fade * gain;
            s.Play();
        }

        // ------------------------------------------------------------------ the game's own moments

        private void OnLocked(Vector3 at, bool last)
        {
            // Watched through the break's camera, far from the game camera that carries the listener: flat, as the Arena's are.
            Play(Lock, at, EventGain * (last ? 1f : 0.7f), 4f, 80f, false);
            EventsPlayed++;
        }

        private void OnRoundEnd(int nextRound, int nextDefender) { Play(Gong, GongAt, EventGain * 0.8f, 12f, 260f, false); EventsPlayed++; }
        private void OnMatchEnd(int winner) { Play(Gong, GongAt, EventGain, 12f, 260f, false); EventsPlayed++; }

        /// <summary>A geyser threw a body (`PayoGeyser`): the rush of water, where it is.</summary>
        public static void GeyserBurst(Vector3 at)
        {
            var self = Instance;
            if (self == null) return;
            self.Play(self.Geyser, at, self.EventGain * 0.75f, 3f, 45f, true);
            self.EventsPlayed++;
        }

        /// <summary>The hawk comes for a fallen body (`PayoCatchBird`): its cry, from where it is.</summary>
        public static void HawkCry(Vector3 at)
        {
            var self = Instance;
            if (self == null) return;
            self.Play(self.Hawk, at, self.EventGain * 0.8f, 8f, 120f, true);
            self.EventsPlayed++;
        }
    }
}
