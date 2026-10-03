using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// Lagoon Cove's environmental sound: surf, coastal wind and seabirds, placed by where the
    /// listener stands and which way they face.
    ///
    /// Owner brief, 2026-09-27: *"add wind, birds and waves sfx (environmental sounds, so being
    /// near/facing the water you hear more waves, same with wind when facing/nearer into the
    /// land)"*. The clips are authored by `tools/build_lagoon_ambience.py`; the scene builder adds
    /// this component and fills every public field.
    ///
    /// ⚠️⚠️ PRESENTATION ONLY. Nothing here is networked, nothing reads or writes match state, and
    /// the bird timing uses its own `System.Random`, never a gameplay stream: a seeded probe
    /// (`BotBehaviourProbe`) must read the same numbers with or without this component in the map.
    ///
    /// ⚠️⚠️ THE CLIPS ARE PROVISIONAL UNTIL THE OWNER HEARS THEM IN PLAY (CLAUDE.md section 6).
    /// `docs/reports/lagoon-ambience-authoring.json` records the measurements and that listening
    /// acceptance is still open. The three gains below are the knobs for that session.
    /// </summary>
    /// ⚠️ ORDER 1001, ONE AFTER `AudioDirector` (1000). The director's `LateUpdate` copies the
    /// camera pose onto the one `AudioListener`; running after it means this reads the pose the
    /// listener will actually hear with this frame, not last frame's.
    [DefaultExecutionOrder(1001)]
    [DisallowMultipleComponent]
    public sealed class LagoonSoundscape : MonoBehaviour
    {
        public AudioClip Waves, Wind;
        public AudioClip[] BirdCalls;
        public Vector3[] Shoreline;      // world points every ~3 m along the coast, at water level
        public Vector3[] ShoreNormals;   // per point: horizontal unit vector pointing out to SEA
        public float WaterY;
        public LagoonFlocks Flocks;      // may be null; exposes `int BirdCount` and `Vector3 BirdPosition(int i)`
        [Range(0, 1)] public float WavesGain = 0.55f, WindGain = 0.4f, BirdsGain = 0.5f;

        // ---------------------------------------------------------------------------------------
        // Tuning. Every distance is in metres against the Shoreline points, measured flat (XZ).
        // ---------------------------------------------------------------------------------------

        /// <summary>Within this distance of the water line the surf is at full level.</summary>
        private const float SurfFullWithin = 6.0f;
        /// <summary>By this distance the surf has fallen to <see cref="SurfFloor"/>.</summary>
        private const float SurfFloorBy = 70.0f;
        /// <summary>
        /// ⚠️ A FLOOR, NOT SILENCE. The cove is small and the sea is always there; a surf bed
        /// that switched off in the middle of the island would read as a bug, not as distance.
        /// </summary>
        private const float SurfFloor = 0.12f;
        /// <summary>Back to the sea: the brief's 0.6. Facing it: 1.0.</summary>
        private const float SurfFacingAway = 0.6f;
        /// <summary>How hard the surf leans toward the ear on the water's side.</summary>
        private const float SurfPanDepth = 0.55f;
        /// <summary>
        /// ⚠️ THE TWO SURF BEDS SIT THIS FAR EITHER SIDE OF THE SURF PAN. They play the same loop
        /// half a lap apart, so they are uncorrelated and the pair reads as a wide beach instead
        /// of one mono point; the spread is what keeps it wide while the pan still steers it.
        /// </summary>
        private const float SurfBedSpread = 0.3f;

        /// <summary>Inland distance at which the land wind is at its fullest.</summary>
        private const float WindFullInland = 40.0f;
        /// <summary>Height above the water that adds the most wind (a rooftop or a hill top).</summary>
        private const float WindFullHeight = 20.0f;
        /// <summary>Over the water and on the sand the wind drops to this, never to nothing.</summary>
        private const float WindFloor = 0.3f;
        /// <summary>Back to the land: 0.7. Facing inland: 1.0.</summary>
        private const float WindFacingAway = 0.7f;

        /// <summary>
        /// ⚠️ GAIN SMOOTHING TIME CONSTANT, IN SECONDS. Every level and pan is eased toward its
        /// target instead of set, because a camera snapping round (a role swap, the emote orbit, the
        /// spectator cycling seats) otherwise steps the surf by up to 4 dB in one frame, which is
        /// an audible zipper on a noise bed.
        /// </summary>
        private const float Smoothing = 0.35f;
        /// <summary>A replay ducks faster than a walk changes the mix, so the cue lands with the cut.</summary>
        private const float ReplaySmoothing = 0.12f;
        /// <summary>The brief's 2 s fade in from the first audible frame.</summary>
        private const float FadeInSeconds = 2.0f;

        private const float BirdGapMin = 4.0f, BirdGapMax = 12.0f;
        private const int BirdVoices = 3;
        /// <summary>
        /// ⚠️ HOW MANY FLOCK BIRDS ARE SAMPLED TO FIND ONE NEAR THE LISTENER. Scanning the whole
        /// flock for the true nearest is pointless for a sound that plays every few seconds; six
        /// random picks keeping the nearest lands a call at a plausibly close bird without making
        /// the cost scale with the flock.
        /// </summary>
        private const int BirdSamples = 6;
        /// <summary>
        /// Consecutive shoreline points further apart than this are not one coast. The array may
        /// hold several strips back to back (the main beach, an islet), and interpolating across
        /// the gap between two strips would invent a coast in the middle of the lagoon.
        /// </summary>
        private const float MaxSegment = 6.0f;
        /// <summary>How often a missing or disabled listener is looked for again.</summary>
        private const float ListenerRetrySeconds = 0.5f;

        // ---------------------------------------------------------------------------------------
        // State. Everything is allocated once in `Build`; the per-frame path allocates nothing.
        // ---------------------------------------------------------------------------------------

        private AudioSource _wavesA, _wavesB, _wind;
        private readonly AudioSource[] _birds = new AudioSource[BirdVoices];
        private readonly int[] _birdIndex = new int[BirdVoices];
        private int _nextBirdVoice;

        /// <summary>⚠️ PRIVATE AND FIXED-SEED. Never `UnityEngine.Random`, which gameplay shares.</summary>
        private readonly System.Random _random = new System.Random(0x4C41474E);

        private bool _built, _playing;
        private float _fadeClock, _birdClock, _nextBird;
        private float _wavesLevel, _wavesPan, _windLevel, _windPan;
        private float _listenerRetry;
        private AudioListener _listener;

        private void OnEnable()
        {
            // ⚠️ EVERY ENABLE STARTS FROM SILENCE. The map preview parks and unparks whole scenes by
            // toggling their roots, and a scene returning from a park must fade in again rather
            // than resume at the level it was cut off at.
            _playing = false;
            _fadeClock = 0.0f;
            _wavesLevel = _windLevel = 0.0f;
            _birdClock = 0.0f;
            _nextBird = NextBirdGap();
        }

        private void OnDisable()
        {
            StopAll();
            _playing = false;
        }

        private void LateUpdate()
        {
            // ⚠️⚠️ THE MAP PREVIEW LOADS THIS SCENE BEHIND THE MENUS, AND IT MUST STAY SILENT THERE.
            // `MapPreviewSurface.Silence` strips every AudioSource the scene was LOADED with, but the
            // sources here are made at runtime, after that pass, so they would slip past it and the
            // hub would play surf over its own music. Two signs a scene is a preview, and either is
            // enough: `MatchInstaller.PreviewOnly` is set for the length of a preview load, and
            // `MapPreviewSurface.Confine` puts every object of a previewed scene on its private
            // layer permanently, which is the sign that is still true after the load finishes.
            // Checked every frame (a bool and an int), not once, because a parked copy can be
            // unparked long after its load.
            if (IsPreview())
            {
                if (_playing) StopAll();
                _playing = false;
                return;
            }

            if (!_built) Build();

            if (!_playing) StartBeds();

            // ⚠️ `AudioListener.pause` already pauses every source that does not opt out, these
            // included. What it does NOT stop is this method, so the bird timer is held here too:
            // otherwise a pause menu left open for a minute queues a call that fires the instant it
            // closes, on top of whatever the resume itself sounds like.
            if (AudioListener.pause) return;

            // Unscaled: a hit-stop or slow-motion `timeScale` must not stretch a fade or starve the
            // birds. Clamped so a hitch (a scene load, a debugger break) cannot jump a whole fade.
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            _fadeClock += dt;
            float fade = Mathf.Clamp01(_fadeClock / FadeInSeconds);
            fade = fade * fade * (3.0f - 2.0f * fade);

            var director = GameServices.Audio;
            float sfx = director != null ? director.AmbienceVolume : 1.0f;
            // ⚠️ `IsInReplayMix` IS THE HOOK THE REPLAY LEASE EXPOSES (`AudioDirector.CourtDanger.cs`),
            // the same one `WorldContactPresentation` and `ColourGrade` read. The lease itself only
            // mutes the director's own pooled voices, so a component driving its own sources has to
            // ask; this one ducks to zero for the length of the replay and fades back after it.
            bool replay = director != null && director.IsInReplayMix;

            if (!TryListener(out Vector3 ear, out Vector3 forward, out Vector3 right))
            {
                // No ears anywhere (a headless probe, a scene mid-transition): hold the last mix.
                return;
            }

            Hear(ear, forward, right, out float surfTarget, out float surfPan, out float windTarget, out float windPan);

            float tau = replay ? ReplaySmoothing : Smoothing;
            float k = 1.0f - Mathf.Exp(-dt / tau);
            float kPan = 1.0f - Mathf.Exp(-dt / Smoothing);
            float mute = replay ? 0.0f : 1.0f;

            _wavesLevel += (surfTarget * mute - _wavesLevel) * k;
            _windLevel += (windTarget * mute - _windLevel) * k;
            _wavesPan += (surfPan - _wavesPan) * kPan;
            _windPan += (windPan - _windPan) * kPan;

            // ⚠️ THE SFX SLIDER IS APPLIED AFTER THE SMOOTHING, NOT INSIDE IT, so the pause-menu
            // slider moves the surf the instant it moves, like every other sound in the game.
            // ⚠️ AND THE TWO SURF BEDS EACH TAKE 1/sqrt(2): they are uncorrelated (half a lap apart),
            // so their powers add, and the pair then sums to WavesGain rather than 3 dB over it.
            float surf = _wavesLevel * WavesGain * sfx * fade * 0.7071f;
            if (_wavesA != null)
            {
                _wavesA.volume = surf;
                _wavesA.panStereo = Mathf.Clamp(_wavesPan - SurfBedSpread, -1.0f, 1.0f);
            }
            if (_wavesB != null)
            {
                _wavesB.volume = surf;
                _wavesB.panStereo = Mathf.Clamp(_wavesPan + SurfBedSpread, -1.0f, 1.0f);
            }

            if (_wind != null)
            {
                // A slow drift on top of the loop's own gusts, so two laps of the 36 s clip never
                // land at quite the same level. `Mathf.PerlinNoise` is allocation-free and not a
                // gameplay stream.
                float drift = 0.82f + 0.18f * Mathf.Clamp01(Mathf.PerlinNoise(_fadeClock * 0.07f, 0.37f));
                _wind.volume = _windLevel * WindGain * sfx * fade * drift;
                _wind.panStereo = Mathf.Clamp(_windPan, -1.0f, 1.0f);
            }

            UpdateBirds(dt, ear, forward, sfx * fade * mute);
        }

        // ---------------------------------------------------------------------------------------
        // Where the listener is, against the coast.
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// The surf and wind targets for a listener at <paramref name="ear"/>.
        ///
        /// ⚠️ SIDE OF THE COAST COMES FROM THE NEAREST POINT'S NORMAL, NOT FROM THE GROUND. The
        /// brief defines "over land" as ground above the water; the shoreline already encodes
        /// exactly that boundary, and a dot product against the sea normal answers it with no
        /// raycast and no collider dependency. Negative is inland, positive is out over the water.
        /// </summary>
        private void Hear(Vector3 ear, Vector3 forward, Vector3 right,
                          out float surfTarget, out float surfPan, out float windTarget, out float windPan)
        {
            surfTarget = SurfFloor;
            surfPan = 0.0f;
            windTarget = WindFloor;
            windPan = 0.0f;

            if (!NearestShore(ear, out Vector3 shore, out Vector3 normal, out float distance)) return;

            Vector3 offset = ear - shore;
            offset.y = 0.0f;
            float side = Vector3.Dot(offset, normal);
            bool inland = side <= 0.0f;

            // ---- Surf -------------------------------------------------------------------------
            float t = Mathf.InverseLerp(SurfFullWithin, SurfFloorBy, distance);
            // ⚠️ SQUARED, NOT LINEAR: loudness falls fastest over the first metres off the sand,
            // the way a real source does, instead of holding near full for half the island.
            float nearness = (1.0f - t) * (1.0f - t);
            float proximity = SurfFloor + (1.0f - SurfFloor) * nearness;

            // Toward the water from here. Inland that is the sea normal. Out over the water the surf
            // is BEHIND a swimmer, back at the beach, so it bends toward the shore point as they
            // move out; blended rather than switched so crossing the water line does not flip it.
            Vector3 toWater = normal;
            if (!inland && distance > 0.01f)
            {
                Vector3 toShore = -offset / distance;
                toWater = Vector3.Lerp(normal, toShore, Mathf.Clamp01(distance / 10.0f));
                float m = toWater.magnitude;
                toWater = m > 0.001f ? toWater / m : normal;
            }

            float faceSea = forward.sqrMagnitude > 0.0f ? Vector3.Dot(forward, toWater) : 0.0f;
            float surfFacing = Mathf.Lerp(SurfFacingAway, 1.0f, 0.5f * (faceSea + 1.0f));
            surfTarget = proximity * surfFacing;
            surfPan = Vector3.Dot(toWater, right) * SurfPanDepth;

            // ---- Wind -------------------------------------------------------------------------
            float landness = inland ? Mathf.SmoothStep(0.0f, 1.0f, distance / WindFullInland) : 0.0f;
            float height = Mathf.Clamp01((ear.y - WaterY - 2.0f) / WindFullHeight);
            // The sand itself gets a little (0.15) so the beach is not windless; the rest is inland
            // distance and height, the brief's "further inland" and "higher up".
            float exposure = Mathf.Clamp01((inland ? 0.15f + 0.7f * landness : 0.0f) + 0.4f * height);
            float windLevel = Mathf.Lerp(WindFloor, 1.0f, exposure);

            // Facing into the land is facing against the sea normal.
            float faceLand = forward.sqrMagnitude > 0.0f ? -Vector3.Dot(forward, normal) : 0.0f;
            float windFacing = Mathf.Lerp(WindFacingAway, 1.0f, 0.5f * (faceLand + 1.0f));
            windTarget = windLevel * windFacing;
            // The wind leans gently toward the land side, opposite the surf, so the two beds are
            // never stacked in the same ear.
            windPan = -Vector3.Dot(normal, right) * 0.3f;
        }

        /// <summary>
        /// The nearest point on the coast, flat, with its sea normal. Walks the points once (a few
        /// hundred at 3 m spacing) and refines onto the neighbouring segment so the distance and the
        /// normal glide rather than step every 3 m.
        /// </summary>
        private bool NearestShore(Vector3 ear, out Vector3 point, out Vector3 normal, out float distance)
        {
            point = Vector3.zero;
            normal = Vector3.forward;
            distance = float.MaxValue;

            var pts = Shoreline;
            var nrm = ShoreNormals;
            if (pts == null || nrm == null || pts.Length == 0 || nrm.Length != pts.Length) return false;

            int best = -1;
            float bestSq = float.MaxValue;
            for (int i = 0; i < pts.Length; i++)
            {
                float dx = pts[i].x - ear.x, dz = pts[i].z - ear.z;
                float sq = dx * dx + dz * dz;
                if (sq < bestSq) { bestSq = sq; best = i; }
            }

            point = pts[best];
            normal = nrm[best];
            distance = Mathf.Sqrt(bestSq);

            // Refine against the two segments either side of the nearest point.
            for (int side = -1; side <= 1; side += 2)
            {
                int j = best + side;
                if (j < 0 || j >= pts.Length) continue;

                Vector3 a = pts[best], b = pts[j];
                float sx = b.x - a.x, sz = b.z - a.z;
                float len2 = sx * sx + sz * sz;
                if (len2 < 1e-6f || len2 > MaxSegment * MaxSegment) continue;

                float u = Mathf.Clamp01(((ear.x - a.x) * sx + (ear.z - a.z) * sz) / len2);
                float px = a.x + sx * u, pz = a.z + sz * u;
                float dx = ear.x - px, dz = ear.z - pz;
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                if (d < distance)
                {
                    distance = d;
                    point = new Vector3(px, a.y, pz);
                    normal = Vector3.Lerp(nrm[best], nrm[j], u);
                }
            }

            normal.y = 0.0f;
            float m = normal.magnitude;
            normal = m > 0.001f ? normal / m : Vector3.forward;
            return true;
        }

        /// <summary>
        /// The ears: the enabled `AudioListener` (there is exactly one, on `AudioDirector`'s "Ears",
        /// which follows `Camera.main`), else `Camera.main` itself. Forward and right come back flat
        /// and unit length, or zero when the view is straight up or down.
        /// </summary>
        private bool TryListener(out Vector3 ear, out Vector3 forward, out Vector3 right)
        {
            Transform source = null;

            if (_listener == null || !_listener.isActiveAndEnabled)
            {
                _listener = null;
                _listenerRetry -= Time.unscaledDeltaTime;
                if (_listenerRetry <= 0.0f)
                {
                    // ⚠️ THE ONLY ALLOCATION ON THIS PATH, AND IT IS RARE: only when the cached
                    // listener is gone, and throttled to twice a second. In a running match the
                    // cache holds and this never runs.
                    _listenerRetry = ListenerRetrySeconds;
                    var all = FindObjectsByType<AudioListener>();
                    for (int i = 0; i < all.Length; i++)
                        if (all[i] != null && all[i].isActiveAndEnabled) { _listener = all[i]; break; }
                }
            }

            if (_listener != null) source = _listener.transform;
            else
            {
                var cam = Camera.main;
                if (cam != null) source = cam.transform;
            }

            if (source == null)
            {
                ear = forward = right = Vector3.zero;
                return false;
            }

            ear = source.position;
            forward = Flat(source.forward);
            right = Flat(source.right);
            return true;
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0.0f;
            float m = v.magnitude;
            return m > 0.05f ? v / m : Vector3.zero;
        }

        // ---------------------------------------------------------------------------------------
        // Birds.
        // ---------------------------------------------------------------------------------------

        private void UpdateBirds(float dt, Vector3 ear, Vector3 forward, float scale)
        {
            float level = BirdsGain * scale;
            int flock = Flocks != null ? Flocks.BirdCount : 0;

            // A call already in the air follows its bird and tracks the slider and a replay duck.
            for (int v = 0; v < BirdVoices; v++)
            {
                var voice = _birds[v];
                if (voice == null || !voice.isPlaying) continue;
                voice.volume = level;
                int b = _birdIndex[v];
                if (b >= 0 && b < flock) voice.transform.position = Flocks.BirdPosition(b);
            }

            if (BirdCalls == null || BirdCalls.Length == 0) return;

            _birdClock += dt;
            if (_birdClock < _nextBird) return;
            _birdClock = 0.0f;
            _nextBird = NextBirdGap();

            // Nothing to hear yet (still fading in, muted, slider at zero): skip this call rather
            // than start one that is inaudible for its whole length.
            if (level <= 0.001f) return;

            var clip = BirdCalls[_random.Next(BirdCalls.Length)];
            if (clip == null) return;

            var src = _birds[_nextBirdVoice];
            int slot = _nextBirdVoice;
            _nextBirdVoice = (_nextBirdVoice + 1) % BirdVoices;
            if (src == null) return;

            int bird = -1;
            Vector3 at;
            if (flock > 0)
            {
                float bestSq = float.MaxValue;
                for (int s = 0; s < BirdSamples; s++)
                {
                    int i = _random.Next(flock);
                    float sq = (Flocks.BirdPosition(i) - ear).sqrMagnitude;
                    if (sq < bestSq) { bestSq = sq; bird = i; }
                }
                at = Flocks.BirdPosition(bird);
            }
            else
            {
                // No flock: a point in the sky, biased toward the sea side, 15 to 35 m out and
                // 12 to 22 m above the higher of the ear and the water, so it is always overhead.
                float angle = (float)(_random.NextDouble() * Mathf.PI * 2.0);
                Vector3 dir = new Vector3(Mathf.Cos(angle), 0.0f, Mathf.Sin(angle));
                if (NearestShore(ear, out _, out Vector3 sea, out _)) dir = (dir + sea * 0.8f).normalized;
                float reach = 15.0f + 20.0f * (float)_random.NextDouble();
                at = ear + dir * reach;
                at.y = Mathf.Max(ear.y, WaterY) + 12.0f + 10.0f * (float)_random.NextDouble();
            }

            _birdIndex[slot] = bird;
            src.transform.position = at;
            src.clip = clip;
            src.pitch = 0.92f + 0.16f * (float)_random.NextDouble();
            src.volume = level;
            src.Play();
        }

        private float NextBirdGap() => BirdGapMin + (BirdGapMax - BirdGapMin) * (float)_random.NextDouble();

        // ---------------------------------------------------------------------------------------
        // Sources.
        // ---------------------------------------------------------------------------------------

        private void Build()
        {
            _built = true;
            _wavesA = MakeBed("LagoonSurfA", Waves);
            _wavesB = MakeBed("LagoonSurfB", Waves);
            _wind = MakeBed("LagoonWind", Wind);

            for (int i = 0; i < BirdVoices; i++)
            {
                var go = new GameObject($"LagoonBird{i}");
                go.transform.SetParent(transform, false);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = false;
                // ⚠️ 3D, so a gull on the left is heard on the left. Logarithmic with a 6 m inner
                // radius: birds are 15 to 40 m up, and the director's 2 to 32 m linear rolloff for
                // court impacts would leave most calls nearly silent.
                src.spatialBlend = 1.0f;
                src.rolloffMode = AudioRolloffMode.Logarithmic;
                src.minDistance = 6.0f;
                src.maxDistance = 90.0f;
                src.dopplerLevel = 0.0f;
                src.priority = 160;
                _birds[i] = src;
                _birdIndex[i] = -1;
            }
        }

        private AudioSource MakeBed(string name, AudioClip clip)
        {
            if (clip == null) return null;
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = true;
            src.clip = clip;
            // ⚠️ 2D ON PURPOSE (spatialBlend 0). The beds are placed by the arithmetic in `Hear`,
            // not by a transform: a surf "source" is the whole coastline, and any single point for
            // it would be wrong from most of the map.
            src.spatialBlend = 0.0f;
            src.dopplerLevel = 0.0f;
            src.volume = 0.0f;
            // ⚠️ High priority (lower is higher): a bed culled by the voice limit during a busy
            // exchange drops out as a hole in the world, which is far more noticeable than a
            // missing one-shot.
            src.priority = 40;
            return src;
        }

        private void StartBeds()
        {
            _playing = true;
            _fadeClock = 0.0f;

            // ⚠️ `timeSamples` IS SET AFTER `Play`, because Play on a stopped source rewinds it.
            // ⚠️ RANDOM START POINTS, AND THE SECOND SURF BED HALF A LAP BEHIND THE FIRST. Starting
            // every match at sample 0 would make the first swell the same swell every time, and two
            // beds in phase would simply be one bed 6 dB louder.
            if (_wavesA != null && _wavesA.clip != null)
            {
                int n = _wavesA.clip.samples;
                int start = n > 0 ? _random.Next(n) : 0;
                _wavesA.volume = 0.0f;
                _wavesA.Play();
                _wavesA.timeSamples = start;
                if (_wavesB != null)
                {
                    _wavesB.volume = 0.0f;
                    _wavesB.Play();
                    _wavesB.timeSamples = n > 0 ? (start + n / 2) % n : 0;
                }
            }
            if (_wind != null && _wind.clip != null)
            {
                int n = _wind.clip.samples;
                _wind.volume = 0.0f;
                _wind.Play();
                _wind.timeSamples = n > 0 ? _random.Next(n) : 0;
            }
        }

        private void StopAll()
        {
            if (_wavesA != null) _wavesA.Stop();
            if (_wavesB != null) _wavesB.Stop();
            if (_wind != null) _wind.Stop();
            for (int i = 0; i < BirdVoices; i++) if (_birds[i] != null) _birds[i].Stop();
        }

        private bool IsPreview()
            => MatchInstaller.PreviewOnly || gameObject.layer == UI.MapPreviewSurface.PreviewLayer;
    }
}
