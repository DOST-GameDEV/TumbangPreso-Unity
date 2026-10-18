using System;
using UnityEngine;

namespace TumbangPreso.Run
{
    /// <summary>
    /// PAETE'S RUN, WHAT IS HEARD: a dry, windy bowl of sand and rock, and the race's own cues. The pattern is
    /// `AlleySoundscape`'s, to the letter where it can be:
    ///
    ///  * THREE 2D BEDS of different lengths: WIND over rock (more of it the higher the listener is), the BOWL
    ///    (a semi-desert's birds and air, everywhere), and a FESTIVAL far off (drums and a crowd, only as loud as
    ///    a rumour, a little more near the start and the finish).
    ///  * 3D LOOPS that sit at places: water running at the slides' ends and the pools, a geyser's bubbling at
    ///    each geyser, stone grinding at the mill wheels. One source each, kept at whichever of its places is
    ///    nearest the listener.
    ///  * SCATTERED 3D ONE-SHOTS at random gaps near where their things are: cloth flapping at the banners, chains
    ///    creaking at the hung boulders, wood knocking at the log roll, pebbles falling on the rock, a kite's cry
    ///    high over the summit, a splash where a slide ends.
    ///  * A GEYSER'S WHOOSH when it throws a racer, and WINGS when a perched bird takes off.
    ///  * THE RACE: a checkpoint taken, the catch of a fall, the set-down and the finish, on the game's own cues
    ///    through `AudioDirector` (so on the SFX slider, and never a new audio path). The 3, 2, 1 and GO already
    ///    sound: `UI.Hud.ShowCountdownTick`, which the mode calls, plays `countdown_tick` and `countdown_go`.
    ///
    /// ⚠️ REAL RECORDINGS ONLY, each Creative Commons 0: tools/build_paete_run_sfx.py cuts them and
    /// tools/paete_run_sfx_sources.json records every source and licence. A clip that is missing is silence here
    /// (the scene builder warns once for each). ⚠️ NOBODY HAS HEARD THIS MIX: every gain is a starting point for
    /// the owner's ear, and public for it.
    ///
    /// ⚠️ BACKGROUND. Beds, loops and one-shots are on the player's AMBIENCE slider (`AudioDirector.AmbienceVolume`
    /// x `KantoStreetSound.AmbientGainScale`), ducked to nothing in a replay, SILENT in the menus' map preview, and
    /// kept well under the vine, a jump and a landing. ⚠️ PRESENTATION ONLY: nothing networked, a private
    /// fixed-seed random, and the race is only read (which checkpoint the local racer is at, whether he is being
    /// carried back, whether he has finished).
    /// </summary>
    [DefaultExecutionOrder(1001)]
    [DisallowMultipleComponent]
    public sealed class RunSoundscape : MonoBehaviour
    {
        [Serializable]
        public sealed class Loop
        {
            public string Name;
            public AudioClip Clip;
            public Vector3[] Points = Array.Empty<Vector3>();
            [Range(0, 1)] public float Gain = .4f;
            public float Near = 4f, Far = 45f;
        }

        [Serializable]
        public sealed class Shot
        {
            public string Name;
            public AudioClip[] Clips = Array.Empty<AudioClip>();
            public Vector3[] Points = Array.Empty<Vector3>();
            [Range(0, 1)] public float Gain = .4f;
            public float Weight = 1f, Near = 4f, Far = 50f;
            /// <summary>How far from its point a shot may be placed, metres, and how far over it.</summary>
            public float Scatter = 2f, Lift = 0f;
        }

        public AudioClip Wind, Bowl, Festival;
        [Range(0, 1)] public float WindGain = .42f, BowlGain = .4f, FestivalGain = .14f;
        /// <summary>Every bed at once, every loop at once, and every one-shot at once: the knobs to reach for first.</summary>
        [Range(0, 1.5f)] public float BedsGain = .6f, LoopsGain = .6f, ShotsGain = .6f;
        public Loop[] Loops = Array.Empty<Loop>();
        public Shot[] Shots = Array.Empty<Shot>();
        /// <summary>Seconds between one-shots.</summary>
        public Vector2 ShotGap = new Vector2(3f, 8f);
        public AudioClip[] GeyserWhoosh = Array.Empty<AudioClip>();
        public Vector3[] Geysers = Array.Empty<Vector3>();
        [Range(0, 1)] public float GeyserGain = .5f;
        public AudioClip[] Wings = Array.Empty<AudioClip>();
        [Range(0, 1)] public float WingsGain = .35f;
        public LagoonFlocks Flocks;
        /// <summary>The start line (the festival is that way) and the heights the wind grows between.</summary>
        public Vector3 StartLine;
        public float LowY = 0f, HighY = 46f;
        /// <summary>The game's own cues for the race (`Audio.AudioCues`): empty is no sound.</summary>
        public string CheckpointCue = "ui_valid", FinishCue = "round_win", FallCue = "sfx_paete_vine_catch", SetDownCue = "land";

        /// <summary>How many one-shots have been started since the scene loaded (for probes).</summary>
        public int ShotsPlayed { get; private set; }
        /// <summary>How many race cues have been asked for (for probes).</summary>
        public int RaceCues { get; private set; }

        private const float Smoothing = .35f, ReplaySmoothing = .12f, FadeInSeconds = 2f, ListenerRetrySeconds = .5f;
        private const int Voices = 6;

        private AudioSource _wind, _bowl, _festival;
        private AudioSource[] _loops = Array.Empty<AudioSource>();
        private readonly AudioSource[] _voices = new AudioSource[Voices];
        private readonly float[] _voiceGain = new float[Voices];
        private int _nextVoice, _lastShot = -1;
        /// <summary>⚠️ PRIVATE AND FIXED-SEED. Never `UnityEngine.Random`, which gameplay shares.</summary>
        private readonly System.Random _random = new System.Random(0x5052554E);
        private bool _built, _playing;
        private float _fadeClock, _shotClock, _nextShot, _listenerRetry, _windLevel, _festivalLevel, _baseLevel;
        private float[] _geyserAt = Array.Empty<float>();
        private bool[] _wasTakingOff = Array.Empty<bool>();
        private AudioListener _listener;
        // The race, as last seen.
        private int _seenNode = -1;
        private bool _seenReturning, _seenFinished;

        private float Range(float a, float b) => a + (float)_random.NextDouble() * (b - a);

        private void OnEnable()
        {
            _playing = false; _fadeClock = 0f; _shotClock = 0f;
            _windLevel = _festivalLevel = _baseLevel = 0f;
            _nextShot = Range(2f, 5f);
            _seenNode = -1; _seenReturning = false; _seenFinished = false;
        }

        private void OnDisable() { StopAll(); _playing = false; }

        private bool IsPreview() => MatchInstaller.PreviewOnly || gameObject.layer == UI.MapPreviewSurface.PreviewLayer;

        private void LateUpdate()
        {
            // ⚠️ SILENT BEHIND THE MENUS (see AlleySoundscape): these sources are made at runtime, so it asks every frame.
            if (IsPreview())
            {
                if (_playing) StopAll();
                _playing = false;
                return;
            }
            if (!_built) Build();
            if (!_playing) StartLoops();
            RaceCuesStep();
            if (AudioListener.pause) return;

            float dt = Mathf.Min(Time.unscaledDeltaTime, .1f);
            _fadeClock += dt;
            float fade = Mathf.Clamp01(_fadeClock / FadeInSeconds);
            fade = fade * fade * (3f - 2f * fade);
            var director = GameServices.Audio;
            float slider = (director != null ? director.AmbienceVolume : 1f) * KantoStreetSound.AmbientGainScale;
            bool replay = director != null && director.IsInReplayMix;
            if (!TryListener(out Vector3 ear)) return;

            float high = Mathf.Clamp01(Mathf.InverseLerp(LowY, HighY, ear.y));
            float windTarget = Mathf.Lerp(.45f, 1f, high);
            float nearStart = 1f - Mathf.Clamp01(Vector3.Distance(ear, StartLine) / 160f);
            float festivalTarget = Mathf.Lerp(.35f, 1f, nearStart);
            float mute = replay ? 0f : 1f;
            float k = 1f - Mathf.Exp(-dt / (replay ? ReplaySmoothing : Smoothing));
            _baseLevel += (mute - _baseLevel) * k;
            _windLevel += (windTarget * mute - _windLevel) * k;
            _festivalLevel += (festivalTarget * mute - _festivalLevel) * k;

            float beds = slider * fade * BedsGain;
            // Gusts: the wind's level wanders, so a lap of its loop never sits at one level.
            float gust = .72f + .28f * Mathf.Clamp01(Mathf.PerlinNoise(_fadeClock * .11f, .37f) * 1.3f);
            if (_wind != null) _wind.volume = _windLevel * WindGain * beds * gust;
            if (_bowl != null) _bowl.volume = _baseLevel * BowlGain * beds;
            if (_festival != null) _festival.volume = _festivalLevel * FestivalGain * beds;

            float loops = slider * fade * mute * LoopsGain;
            for (int i = 0; i < _loops.Length; i++)
            {
                var source = _loops[i]; var loop = Loops[i];
                if (source == null || loop == null || loop.Points == null || loop.Points.Length == 0) continue;
                int best = 0; float bestSq = float.MaxValue;
                for (int p = 0; p < loop.Points.Length; p++)
                {
                    float sq = (loop.Points[p] - ear).sqrMagnitude;
                    if (sq < bestSq) { bestSq = sq; best = p; }
                }
                source.transform.position = loop.Points[best];
                source.volume = loop.Gain * loops;
            }

            float shots = slider * fade * mute * ShotsGain;
            for (int i = 0; i < Voices; i++) if (_voices[i] != null && _voices[i].isPlaying) _voices[i].volume = _voiceGain[i] * shots;
            _shotClock += dt;
            if (_shotClock >= _nextShot && !replay)
            {
                _shotClock = 0f; _nextShot = Range(ShotGap.x, ShotGap.y);
                PlayShot(ear, shots);
            }
            if (!replay) { GeysersStep(shots); WingsStep(ear, shots); }
        }

        // ------------------------------------------------------------------ the race's cues

        private void RaceCuesStep()
        {
            var mode = RunMode.Instance;
            var me = mode != null ? mode.Local : null;
            if (me == null || me.Body == null) { _seenNode = -1; _seenReturning = false; _seenFinished = false; return; }
            var audio = GameServices.Audio;
            Vector3 at = me.Body.transform.position;
            if (me.Finished != _seenFinished)
            {
                _seenFinished = me.Finished;
                if (me.Finished) Cue(audio, FinishCue, at, true);
                _seenNode = me.Node;
            }
            else if (me.Node != _seenNode)
            {
                // Node 0 is the start line: being put back on it (a restart) is not a checkpoint taken.
                if (_seenNode >= 0 && me.Node != 0 && mode.State == RunMode.Phase.Racing) Cue(audio, CheckpointCue, at, true);
                _seenNode = me.Node;
            }
            if (me.Returning != _seenReturning)
            {
                _seenReturning = me.Returning;
                Cue(audio, me.Returning ? FallCue : SetDownCue, at, false);
            }
        }

        private void Cue(AudioDirector audio, string cue, Vector3 at, bool flat)
        {
            RaceCues++;
            if (audio == null || string.IsNullOrEmpty(cue)) return;
            if (flat) audio.PlayUi(cue); else audio.PlayAt(cue, at);
        }

        // ------------------------------------------------------------------ one-shots

        private void PlayShot(Vector3 ear, float mix)
        {
            if (Shots == null || Shots.Length == 0) return;
            float total = 0f;
            for (int i = 0; i < Shots.Length; i++) if (Usable(i, ear)) total += Shots[i].Weight;
            if (total <= 0f) return;
            float roll = Range(0f, total); int pick = -1;
            for (int i = 0; i < Shots.Length; i++)
            {
                if (!Usable(i, ear)) continue;
                pick = i; roll -= Shots[i].Weight;
                if (roll <= 0f) break;
            }
            if (pick < 0) return;
            _lastShot = pick;
            var shot = Shots[pick];
            // One of its places the listener can hear: the first in reach from a random start.
            int n = shot.Points.Length, first = _random.Next(n);
            Vector3 at = shot.Points[first];
            for (int k = 0; k < n; k++)
            {
                var p = shot.Points[(first + k) % n];
                if ((p - ear).sqrMagnitude <= shot.Far * shot.Far) { at = p; break; }
            }
            at += new Vector3(Range(-shot.Scatter, shot.Scatter), shot.Lift, Range(-shot.Scatter, shot.Scatter));
            Play(shot.Clips[_random.Next(shot.Clips.Length)], at, shot.Gain, shot.Near, shot.Far, mix);
            ShotsPlayed++;
        }

        private bool Usable(int i, Vector3 ear)
        {
            var s = Shots[i];
            if (i == _lastShot || s == null || s.Clips == null || s.Clips.Length == 0 || s.Weight <= 0f || s.Points == null || s.Points.Length == 0) return false;
            for (int p = 0; p < s.Points.Length; p++)
                if ((s.Points[p] - ear).sqrMagnitude <= s.Far * s.Far) return true;
            return false;
        }

        private void Play(AudioClip clip, Vector3 at, float gain, float near, float far, float mix)
        {
            if (clip == null) return;
            int pick = -1;
            for (int i = 0; i < Voices && pick < 0; i++)
            {
                int v = (_nextVoice + i) % Voices;
                if (_voices[v] != null && !_voices[v].isPlaying) pick = v;
            }
            if (pick < 0) pick = _nextVoice;
            _nextVoice = (pick + 1) % Voices;
            var source = _voices[pick];
            if (source == null) return;
            source.Stop();
            source.transform.position = at;
            source.clip = clip;
            source.pitch = .96f + (float)_random.NextDouble() * .08f;
            source.minDistance = near; source.maxDistance = far;
            _voiceGain[pick] = gain;
            source.volume = gain * mix;
            source.Play();
        }

        private void GeysersStep(float mix)
        {
            var mode = RunMode.Instance;
            if (mode == null || Geysers == null || GeyserWhoosh == null || GeyserWhoosh.Length == 0) return;
            if (_geyserAt.Length != Geysers.Length) { _geyserAt = new float[Geysers.Length]; for (int i = 0; i < _geyserAt.Length; i++) _geyserAt[i] = -10f; }
            for (int i = 0; i < Geysers.Length; i++)
            {
                if (Time.time - _geyserAt[i] < 1.2f) continue;
                foreach (var r in mode.Racers)
                {
                    if (r == null || r.Body == null) continue;
                    Vector3 d = r.Body.transform.position - Geysers[i];
                    // Going up fast over the vent: he has just been thrown.
                    if (d.x * d.x + d.z * d.z > 2.2f * 2.2f || d.y < -0.5f || d.y > 5f || r.Body.Velocity.y < 9f) continue;
                    _geyserAt[i] = Time.time;
                    Play(GeyserWhoosh[_random.Next(GeyserWhoosh.Length)], Geysers[i], GeyserGain, 5f, 60f, mix);
                    break;
                }
            }
        }

        private void WingsStep(Vector3 ear, float mix)
        {
            if (Flocks == null || Wings == null || Wings.Length == 0) return;
            int n = Flocks.BirdCount;
            if (_wasTakingOff.Length != n) _wasTakingOff = new bool[n];
            for (int i = 0; i < n; i++)
            {
                bool now = Flocks.BirdTakingOff(i);
                if (now && !_wasTakingOff[i])
                {
                    Vector3 at = Flocks.BirdPosition(i);
                    if ((at - ear).sqrMagnitude < 45f * 45f) Play(Wings[_random.Next(Wings.Length)], at, WingsGain, 3f, 45f, mix);
                }
                _wasTakingOff[i] = now;
            }
        }

        // ------------------------------------------------------------------ sources

        private bool TryListener(out Vector3 ear)
        {
            Transform source = null;
            if (_listener == null || !_listener.isActiveAndEnabled)
            {
                _listener = null;
                _listenerRetry -= Time.unscaledDeltaTime;
                if (_listenerRetry <= 0f)
                {
                    _listenerRetry = ListenerRetrySeconds;
                    var all = FindObjectsByType<AudioListener>();
                    for (int i = 0; i < all.Length; i++)
                        if (all[i] != null && all[i].isActiveAndEnabled) { _listener = all[i]; break; }
                }
            }
            if (_listener != null) source = _listener.transform;
            else { var cam = Camera.main; if (cam != null) source = cam.transform; }
            if (source == null) { ear = Vector3.zero; return false; }
            ear = source.position;
            return true;
        }

        private void Build()
        {
            _built = true;
            _wind = MakeBed("RunBedWind", Wind);
            _bowl = MakeBed("RunBedBowl", Bowl);
            _festival = MakeBed("RunBedFestival", Festival);
            _loops = new AudioSource[Loops != null ? Loops.Length : 0];
            for (int i = 0; i < _loops.Length; i++)
            {
                var loop = Loops[i];
                if (loop == null || loop.Clip == null || loop.Points == null || loop.Points.Length == 0) continue;
                var src = MakePoint("RunLoop " + loop.Name, 120);
                src.loop = true; src.clip = loop.Clip; src.volume = 0f;
                src.minDistance = loop.Near; src.maxDistance = loop.Far;
                _loops[i] = src;
            }
            for (int i = 0; i < Voices; i++) _voices[i] = MakePoint("RunShot" + i, 180);
        }

        private AudioSource MakePoint(string name, int priority)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false; src.loop = false;
            src.spatialBlend = 1f; src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.dopplerLevel = 0f; src.priority = priority;
            return src;
        }

        private AudioSource MakeBed(string name, AudioClip clip)
        {
            if (clip == null) return null;
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false; src.loop = true; src.clip = clip;
            // 2D on purpose: a bed is the whole bowl, placed by the arithmetic above, not by a point.
            src.spatialBlend = 0f; src.dopplerLevel = 0f; src.volume = 0f;
            src.priority = 40;
            return src;
        }

        private void StartLoops()
        {
            _playing = true; _fadeClock = 0f;
            foreach (var bed in new[] { _wind, _bowl, _festival }) Begin(bed);
            foreach (var loop in _loops) Begin(loop);
        }

        private void Begin(AudioSource source)
        {
            if (source == null || source.clip == null) return;
            source.volume = 0f;
            source.Play();
            int n = source.clip.samples;
            source.timeSamples = n > 0 ? _random.Next(n) : 0;   // after Play, which rewinds
        }

        private void StopAll()
        {
            if (_wind != null) _wind.Stop();
            if (_bowl != null) _bowl.Stop();
            if (_festival != null) _festival.Stop();
            foreach (var loop in _loops) if (loop != null) loop.Stop();
            for (int i = 0; i < Voices; i++) if (_voices[i] != null) _voices[i].Stop();
        }
    }
}
