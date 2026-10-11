using System;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// The Eskinita Alley's ambience: a late-afternoon Manila neighbourhood heard from inside an
    /// alley (owner, 2026-10-09: "the map's ambience sfx is missing too"). The pattern is
    /// `LagoonSoundscape`'s and `KantoStreetSound`'s: 2D looping BEDS whose levels follow where the
    /// listener stands, and scattered 3D ONE-SHOTS at random gaps so nothing loops audibly.
    ///
    /// THE BEDS (five loops of different lengths, 37 to 61 s, so they never line up twice):
    ///  * Neighbourhood: birds, far traffic and a breeze, everywhere;
    ///  * Street: motorbikes on the road outside. ⚠️ LOUDER AT THE LOW END by the arch (the street is
    ///    beyond +z), thinner up on the top terrace and thinnest on the roofs, and it leans to the ear
    ///    on the street's side;
    ///  * Kids: children playing, far off, a little louder toward the street;
    ///  * Sparrows and Wind: ⚠️ MORE OF BOTH THE HIGHER AND THE FURTHER UP THE ALLEY the listener is.
    /// THE ONE-SHOTS, placed round and beyond the play area: a motorbike passing on the street, a far
    /// rooster, a dog a block away, a gate, dishes from a kitchen window, a vendor's hand bell and
    /// horn, cloth flapping overhead in a gust, pigeons' wings on the roofs.
    ///
    /// ⚠️ REAL RECORDINGS ONLY, each Creative Commons 0: tools/build_eskinita_life_sfx.py cuts them
    /// and tools/eskinita_life_sfx_sources.json records every source and licence. ⚠️ NOBODY HAS
    /// HEARD THIS MIX: every gain below is a starting point for the owner's ear, and public for it.
    ///
    /// ⚠️ BACKGROUND. On the player's AMBIENCE slider (`AudioDirector.AmbienceVolume` x
    /// `KantoStreetSound.AmbientGainScale`), ducked to nothing in a replay, SILENT in the menus' map
    /// preview, and kept well under the match's own cues (throws, tags, the can, voices).
    /// ⚠️ PRESENTATION ONLY: nothing networked, no match state, a private fixed-seed random.
    /// ⚠️ ORDER 1001, one after `AudioDirector` (1000), so it reads the listener's pose of this frame.
    /// </summary>
    [DefaultExecutionOrder(1001)]
    [DisallowMultipleComponent]
    public sealed class AlleySoundscape : MonoBehaviour
    {
        public enum Place { Street, Far, Houses, Overhead, Roofs }

        [Serializable]
        public sealed class Shot
        {
            public string Name;
            public AudioClip[] Clips = Array.Empty<AudioClip>();
            [Range(0, 1)] public float Gain = .5f;
            public float Weight = 1f;
            public Place Where;
            public float Near = 5f, Far = 70f;
        }

        public AudioClip Neighbourhood, Street, Kids, Sparrows, Wind;
        [Range(0, 1)] public float NeighbourhoodGain = .5f, StreetGain = .45f, KidsGain = .3f, SparrowsGain = .4f, WindGain = .3f;
        /// <summary>Every bed at once, and every one-shot at once: the two knobs to reach for first.</summary>
        [Range(0, 1.5f)] public float BedsGain = .7f, ShotsGain = .7f;
        public Shot[] Shots = Array.Empty<Shot>();
        /// <summary>Seconds between one-shots.</summary>
        public Vector2 ShotGap = new Vector2(3.5f, 10f);
        // The alley, for placing things: the street is beyond LowEndZ, the top terrace toward TopEndZ.
        public float LowEndZ = 17.5f, TopEndZ = -17.5f, HalfWidth = 8f, CourtY = 0f;

        /// <summary>How many one-shots have been started since the scene loaded (for probes).</summary>
        public int ShotsPlayed { get; private set; }

        private const float Smoothing = .35f, ReplaySmoothing = .12f, FadeInSeconds = 2f, ListenerRetrySeconds = .5f;
        private const int ShotVoices = 4;

        private AudioSource _neighbourhood, _street, _kids, _sparrows, _wind;
        private readonly AudioSource[] _shots = new AudioSource[ShotVoices];
        private readonly float[] _shotGain = new float[ShotVoices];
        private int _nextShotVoice, _lastShot = -1;
        /// <summary>⚠️ PRIVATE AND FIXED-SEED. Never `UnityEngine.Random`, which gameplay shares.</summary>
        private readonly System.Random _random = new System.Random(0x45534B4E);
        private bool _built, _playing;
        private float _fadeClock, _shotClock, _nextShot, _listenerRetry;
        private float _streetLevel, _kidsLevel, _sparrowsLevel, _windLevel, _baseLevel, _streetPan;
        private AudioListener _listener;

        private float Range(float a, float b) => a + (float)_random.NextDouble() * (b - a);

        private void OnEnable()
        {
            // Every enable starts from silence: the map preview parks and unparks whole scenes.
            _playing = false; _fadeClock = 0f; _shotClock = 0f;
            _streetLevel = _kidsLevel = _sparrowsLevel = _windLevel = _baseLevel = 0f;
            _nextShot = Range(2f, 5f);
        }

        private void OnDisable() { StopAll(); _playing = false; }

        private bool IsPreview() => MatchInstaller.PreviewOnly || gameObject.layer == UI.MapPreviewSurface.PreviewLayer;

        private void LateUpdate()
        {
            // ⚠️ SILENT BEHIND THE MENUS. `MapPreviewSurface.Silence` strips the sources a scene was loaded
            // with; these are made at runtime, so the component asks every frame (see LagoonSoundscape).
            if (IsPreview())
            {
                if (_playing) StopAll();
                _playing = false;
                return;
            }
            if (!_built) Build();
            if (!_playing) StartBeds();
            if (AudioListener.pause) return;

            float dt = Mathf.Min(Time.unscaledDeltaTime, .1f);
            _fadeClock += dt;
            float fade = Mathf.Clamp01(_fadeClock / FadeInSeconds);
            fade = fade * fade * (3f - 2f * fade);
            var director = GameServices.Audio;
            float slider = (director != null ? director.AmbienceVolume : 1f) * KantoStreetSound.AmbientGainScale;
            bool replay = director != null && director.IsInReplayMix;
            if (!TryListener(out Vector3 ear, out Vector3 right)) return;

            // Where the ear is: 0 at the top end of the alley .. 1 at the low end by the arch; 0 on the floor .. 1 up on the roofs.
            float low = Mathf.InverseLerp(TopEndZ, LowEndZ, ear.z);
            float high = Mathf.Clamp01((ear.y - CourtY - 2.2f) / 4.5f);
            float streetTarget = Mathf.Lerp(.4f, 1f, low * low) * Mathf.Lerp(1f, .55f, high);
            float kidsTarget = Mathf.Lerp(.6f, 1f, low);
            float open = Mathf.Clamp01(high + (1f - low) * .4f);
            float sparrowsTarget = Mathf.Lerp(.5f, 1f, open);
            float windTarget = Mathf.Lerp(.2f, 1f, open);
            float mute = replay ? 0f : 1f;
            float k = 1f - Mathf.Exp(-dt / (replay ? ReplaySmoothing : Smoothing));
            _baseLevel += (mute - _baseLevel) * k;
            _streetLevel += (streetTarget * mute - _streetLevel) * k;
            _kidsLevel += (kidsTarget * mute - _kidsLevel) * k;
            _sparrowsLevel += (sparrowsTarget * mute - _sparrowsLevel) * k;
            _windLevel += (windTarget * mute - _windLevel) * k;
            // The street is toward +z: it leans to the ear on that side.
            _streetPan += (Vector3.Dot(Vector3.forward, right) * .45f - _streetPan) * (1f - Mathf.Exp(-dt / Smoothing));

            // The slider is applied after the smoothing, so it moves the beds the instant it moves.
            float all = slider * fade * BedsGain;
            // A slow drift, so two laps of a loop never sit at quite the same level.
            float drift = .85f + .15f * Mathf.Clamp01(Mathf.PerlinNoise(_fadeClock * .06f, .41f));
            if (_neighbourhood != null) _neighbourhood.volume = _baseLevel * NeighbourhoodGain * all * drift;
            if (_street != null) { _street.volume = _streetLevel * StreetGain * all; _street.panStereo = Mathf.Clamp(_streetPan, -1f, 1f); }
            if (_kids != null) { _kids.volume = _kidsLevel * KidsGain * all; _kids.panStereo = Mathf.Clamp(_streetPan * .6f + .2f * Mathf.Sin(_fadeClock * .05f), -1f, 1f); }
            if (_sparrows != null) { _sparrows.volume = _sparrowsLevel * SparrowsGain * all; _sparrows.panStereo = Mathf.Clamp(-_streetPan * .5f, -1f, 1f); }
            if (_wind != null) _wind.volume = _windLevel * WindGain * all * (.8f + .2f * Mathf.Clamp01(Mathf.PerlinNoise(.73f, _fadeClock * .09f)));

            float shots = slider * fade * mute * ShotsGain;
            for (int i = 0; i < ShotVoices; i++) if (_shots[i] != null && _shots[i].isPlaying) _shots[i].volume = _shotGain[i] * shots;
            _shotClock += dt;
            if (_shotClock >= _nextShot && !replay)
            {
                _shotClock = 0f; _nextShot = Range(ShotGap.x, ShotGap.y);
                PlayShot(ear, low, shots);
            }
        }

        private void PlayShot(Vector3 ear, float low, float mix)
        {
            if (Shots == null || Shots.Length == 0) return;
            float total = 0f;
            for (int i = 0; i < Shots.Length; i++) if (Usable(i)) total += Shots[i].Weight;
            if (total <= 0f) return;
            float roll = Range(0f, total); int pick = -1;
            for (int i = 0; i < Shots.Length; i++)
            {
                if (!Usable(i)) continue;
                pick = i; roll -= Shots[i].Weight;
                if (roll <= 0f) break;
            }
            if (pick < 0) return;
            _lastShot = pick;
            var shot = Shots[pick];
            var clip = shot.Clips[_random.Next(shot.Clips.Length)];
            if (clip == null) return;
            Vector3 at;
            float side = _random.NextDouble() < .5 ? -1f : 1f;
            switch (shot.Where)
            {
                case Place.Street: at = new Vector3(Range(-12f, 12f), CourtY, LowEndZ + Range(5f, 14f)); break;
                case Place.Far: { float a = Range(0f, Mathf.PI * 2f), d = Range(30f, 50f); at = new Vector3(Mathf.Cos(a) * d, CourtY + 3f, Mathf.Sin(a) * d); break; }
                case Place.Houses: at = new Vector3(side * (HalfWidth + Range(2f, 6f)), CourtY + Range(1.5f, 4f), Range(TopEndZ, LowEndZ)); break;
                case Place.Overhead: at = ear + new Vector3(Range(-7f, 7f), Range(3f, 6f), Range(-7f, 7f)); break;
                default: at = new Vector3(side * Range(6f, 12f), CourtY + Range(5f, 8f), Range(TopEndZ, LowEndZ)); break;
            }
            var source = _shots[_nextShotVoice];
            int voice = _nextShotVoice;
            _nextShotVoice = (_nextShotVoice + 1) % ShotVoices;
            if (source == null) return;
            source.Stop();
            source.transform.position = at;
            source.clip = clip;
            source.minDistance = shot.Near; source.maxDistance = shot.Far;
            _shotGain[voice] = shot.Gain;
            source.volume = shot.Gain * mix;
            source.Play();
            ShotsPlayed++;
        }

        private bool Usable(int i)
            => i != _lastShot && Shots[i] != null && Shots[i].Clips != null && Shots[i].Clips.Length > 0 && Shots[i].Weight > 0f;

        private bool TryListener(out Vector3 ear, out Vector3 right)
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
            if (source == null) { ear = right = Vector3.zero; return false; }
            ear = source.position; right = source.right;
            return true;
        }

        private void Build()
        {
            _built = true;
            _neighbourhood = MakeBed("AlleyBedNeighbourhood", Neighbourhood);
            _street = MakeBed("AlleyBedStreet", Street);
            _kids = MakeBed("AlleyBedKids", Kids);
            _sparrows = MakeBed("AlleyBedSparrows", Sparrows);
            _wind = MakeBed("AlleyBedWind", Wind);
            for (int i = 0; i < ShotVoices; i++)
            {
                var go = new GameObject("AlleyShot" + i);
                go.transform.SetParent(transform, false);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false; src.loop = false;
                src.spatialBlend = 1f; src.rolloffMode = AudioRolloffMode.Logarithmic;
                src.dopplerLevel = 0f; src.priority = 180;
                _shots[i] = src;
            }
        }

        private AudioSource MakeBed(string name, AudioClip clip)
        {
            if (clip == null) return null;
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false; src.loop = true; src.clip = clip;
            // 2D on purpose: a bed is the whole neighbourhood, placed by the arithmetic above, not by a point.
            src.spatialBlend = 0f; src.dopplerLevel = 0f; src.volume = 0f;
            // High priority (lower is higher): a bed culled in a busy exchange is a hole in the world.
            src.priority = 40;
            return src;
        }

        private void StartBeds()
        {
            _playing = true; _fadeClock = 0f;
            // Random start points: a match never opens on the same bird.
            foreach (var bed in new[] { _neighbourhood, _street, _kids, _sparrows, _wind })
            {
                if (bed == null || bed.clip == null) continue;
                bed.volume = 0f;
                bed.Play();
                int n = bed.clip.samples;
                bed.timeSamples = n > 0 ? _random.Next(n) : 0;   // after Play, which rewinds
            }
        }

        private void StopAll()
        {
            if (_neighbourhood != null) _neighbourhood.Stop();
            if (_street != null) _street.Stop();
            if (_kids != null) _kids.Stop();
            if (_sparrows != null) _sparrows.Stop();
            if (_wind != null) _wind.Stop();
            for (int i = 0; i < ShotVoices; i++) if (_shots[i] != null) _shots[i].Stop();
        }
    }
}
