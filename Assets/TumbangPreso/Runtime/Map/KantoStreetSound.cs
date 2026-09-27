using System;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// Kanto's street sound: the city around the park, the engines of the traffic that drives past
    /// it, and the horns of drivers who are waiting.
    ///
    /// Owner brief, 2026-09-27, about Kanto's moving traffic: *"needs sfx, bustling city ambience,
    /// car engine and driving sounds, beep/horns"*. The clips are authored by
    /// `tools/build_kanto_street_audio.py`; the scene builder adds this component next to
    /// <see cref="KantoTraffic"/> and fills every public field.
    ///
    /// Three layers:
    /// 1. THE CITY BED, one 2D loop whose level rises as the listener walks from the court toward
    ///    the kerb (the streets are 22 m out on all four sides, the play walls at +/-13).
    /// 2. ENGINES, a small pool of 3D loops handed to whichever vehicles are nearest the listener,
    ///    pitched by each vehicle's speed and lifted a little under acceleration.
    /// 3. HORNS, one-shots from vehicles that are waiting: a beep or two when their light turns
    ///    green and the car in front has not moved, plus the odd impatient honk.
    ///
    /// ⚠️⚠️ PRESENTATION ONLY. Nothing here is networked, nothing reads or writes match state, and
    /// every random choice uses its own `System.Random`, never a gameplay stream: a seeded probe
    /// (`BotBehaviourProbe`) must read the same numbers with or without this component in the map.
    /// It only READS <see cref="KantoTraffic"/> through the accessors that class exposes for it, and
    /// never changes the traffic.
    ///
    /// ⚠️⚠️ THE CLIPS ARE PROVISIONAL UNTIL THE OWNER HEARS THEM IN PLAY (CLAUDE.md section 6).
    /// `docs/reports/kanto-street-audio-authoring.json` records the measurements and that listening
    /// acceptance is still open. The three gains below are the knobs for that session.
    /// </summary>
    /// ⚠️ ORDER 1001, ONE AFTER `AudioDirector` (1000), the same as `LagoonSoundscape`. The
    /// director's `LateUpdate` copies the camera pose onto the one `AudioListener`; running after it
    /// means the nearest-vehicle choice is made from the pose the listener hears this frame.
    [DefaultExecutionOrder(1001)]
    [DisallowMultipleComponent]
    public sealed class KantoStreetSound : MonoBehaviour
    {
        public KantoTraffic Traffic;
        public AudioClip CityBed, EngineCar, EngineDiesel, EngineTricycle;
        public AudioClip[] HornsCar;
        public AudioClip HornJeepney, HornTricycle;
        [Range(0, 1)] public float BedGain = 0.45f, EngineGain = 0.5f, HornGain = 0.55f;
        public int EngineVoices = 8;

        // ---------------------------------------------------------------------------------------
        // Tuning: the city bed. Distances are flat (XZ) from the court centre, the world origin
        // that `KantoTraffic` lays its lanes around.
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// ⚠️ A FLOOR, NOT SILENCE. The court sits in the middle of a busy block; a city that
        /// switched off at the centre spot would read as a bug, not as a park.
        /// </summary>
        private const float BedFloor = 0.45f;
        /// <summary>Inside this (the court) the bed holds at its floor.</summary>
        private const float BedQuietWithin = 6.0f;
        /// <summary>
        /// The bed is full this far INSIDE the road centre line: 22 - 4 = 18 m, the kerb, with the
        /// brief's "fuller at the kerb, around 17 to 22 m out". Measured on the SQUARE distance
        /// (the larger of |x| and |z|), because the four roads make a square round the park and a
        /// round distance would call a corner of the park quieter than the middle of a side.
        /// </summary>
        private const float BedFullInsideRoad = 4.0f;
        /// <summary>Used when `Traffic` is missing: the road centre line of the map's # grid.</summary>
        private const float DefaultRoad = 22.0f;

        // ---------------------------------------------------------------------------------------
        // Tuning: engines.
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// ⚠️ HOW OFTEN THE ENGINE VOICES ARE HANDED OUT, NOT EVERY FRAME. Four times a second is
        /// fast enough that a car coming round the corner is picked up before it is close (at a
        /// 9 m/s cruise it moves 2.3 m between passes) and slow enough that the cost is nothing.
        /// </summary>
        private const float ReassignSeconds = 0.25f;
        /// <summary>
        /// ⚠️ HYSTERESIS: a vehicle that already has a voice competes as if it were 20 per cent
        /// closer than it is. Without it two cars at nearly the same distance trade one voice back
        /// and forth every pass, and each trade is a fade out and in on both.
        /// </summary>
        private const float HeldDistanceFactor = 0.8f;
        /// <summary>Vehicles further than this never get a voice (the rolloff has made them faint).</summary>
        private const float EngineReach = 60.0f;
        /// <summary>A voice changing vehicle fades out, moves, and fades in, each half this long.</summary>
        private const float SwapFadeSeconds = 0.2f;
        /// <summary>Pitch while waiting at a light or in a queue: the loops are authored at idle.</summary>
        private const float PitchIdle = 0.85f;
        /// <summary>Extra pitch at full throttle: the revs run ahead of the road speed under load.</summary>
        private const float LoadPitch = 0.08f;
        /// <summary>Extra level at full throttle, the "volume up a little under load".</summary>
        private const float LoadGain = 0.35f;
        /// <summary>The acceleration read as full throttle: `KantoTraffic`'s own amax, 1.6 m/s^2.</summary>
        private const float FullThrottleAccel = 1.6f;
        /// <summary>Pitch glides at this time constant, so a gear of speed never steps the note.</summary>
        private const float PitchSmoothing = 0.15f;
        private const float LoadSmoothing = 0.25f;
        private const float MinPitch = 0.7f, MaxPitch = 1.9f;
        private const int MaxEngineVoices = 16;

        // ---------------------------------------------------------------------------------------
        // Tuning: horns.
        // ---------------------------------------------------------------------------------------

        /// <summary>When an axis turns green: one horn this often, two horns this much more often.</summary>
        private const float GreenOneHorn = 0.45f, GreenTwoHorns = 0.20f;
        private const float GreenDelayMin = 0.6f, GreenDelayMax = 1.5f;
        private const float ImpatientMin = 12.0f, ImpatientMax = 35.0f;
        /// <summary>No waiting vehicle in reach, or the window was full: look again this soon.</summary>
        private const float ImpatientRetryMin = 3.0f, ImpatientRetryMax = 6.0f;
        /// <summary>
        /// ⚠️⚠️ NEVER MORE THAN TWO HORNS INSIDE THIS WINDOW (the brief's 1.5 s). A green light on
        /// a busy axis plus an impatient honk could otherwise stack three or four beeps into one
        /// second, which stops reading as a street and starts reading as an alarm.
        /// </summary>
        private const float HornWindow = 1.5f;
        /// <summary>Two voices suffice: the window allows two at once, and the longest clip (1.2 s
        /// at the lowest pitch, 1.25 s) has ended before a third is allowed.</summary>
        private const int HornVoices = 2;
        private const float HornReach = 70.0f;
        private const int PendingHorns = 4;
        /// <summary>How many random waiting vehicles are sampled to pick the one that honks; the
        /// nearest of them wins, so a honk usually comes from a car the player can see.</summary>
        private const int HornSamples = 3;

        // ---------------------------------------------------------------------------------------
        // Mix.
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// ⚠️ GAIN SMOOTHING TIME CONSTANT, IN SECONDS, as in `LagoonSoundscape`: a camera snapping
        /// round (a role swap, the emote orbit, the spectator cycling seats) otherwise steps the bed
        /// in one frame, which is an audible zipper on a noise loop.
        /// </summary>
        private const float Smoothing = 0.35f;
        /// <summary>A replay ducks faster than a walk changes the mix, so the cue lands with the cut.</summary>
        private const float ReplaySmoothing = 0.12f;
        private const float FadeInSeconds = 2.0f;
        private const float ListenerRetrySeconds = 0.5f;

        private const int KindCar = 0, KindDiesel = 1, KindTricycle = 2;
        private const int HornCar = 0, HornJeep = 1, HornTrike = 2;

        // ---------------------------------------------------------------------------------------
        // State. Allocated in `Build` and `CacheDrivers`; the per-frame path allocates nothing.
        // ---------------------------------------------------------------------------------------

        /// <summary>⚠️ PRIVATE AND FIXED-SEED. Never `UnityEngine.Random`, which gameplay shares.</summary>
        private readonly System.Random _random = new System.Random(0x4B414E54);

        private bool _built, _playing;
        private float _fadeClock, _clock, _reassignClock;
        private float _bedLevel, _mix;
        private float _listenerRetry;
        private AudioListener _listener;
        private AudioSource _bed;

        // Per engine voice.
        private AudioSource[] _voices = Array.Empty<AudioSource>();
        private int[] _vDriver = Array.Empty<int>(), _vNext = Array.Empty<int>(), _vKind = Array.Empty<int>();
        private float[] _vGain = Array.Empty<float>(), _vPitch = Array.Empty<float>(),
                        _vLoad = Array.Empty<float>(), _vPrevSpeed = Array.Empty<float>();
        private int[] _pick = Array.Empty<int>();

        // Per vehicle, read once from its model name (reading a Unity object's name allocates).
        private int _cachedCount = -1;
        private int[] _engineKind = Array.Empty<int>(), _hornKind = Array.Empty<int>(), _heldBy = Array.Empty<int>();
        private float[] _basePitch = Array.Empty<float>(), _topPitch = Array.Empty<float>(),
                        _modelGain = Array.Empty<float>(), _score = Array.Empty<float>();
        private bool[] _wanted = Array.Empty<bool>(), _served = Array.Empty<bool>();

        // Horns.
        private readonly AudioSource[] _horns = new AudioSource[HornVoices];
        private readonly int[] _hornDriver = new int[HornVoices];
        private readonly float[] _hornClipGain = new float[HornVoices];
        private int _nextHornVoice;
        private readonly float[] _hornTimes = new float[2];
        private readonly int[] _pendAxis = new int[PendingHorns];
        private readonly int[] _pendExclude = new int[PendingHorns];
        private readonly float[] _pendAt = new float[PendingHorns];
        private readonly bool[] _pendUsed = new bool[PendingHorns];
        private float _nextImpatient;
        private KantoTraffic _subscribed;
        private Action<int> _onGreen;

        private void OnEnable()
        {
            // ⚠️ EVERY ENABLE STARTS FROM SILENCE, like the lagoon: the map preview parks and
            // unparks whole scenes by toggling their roots, and a scene returning from a park must
            // fade in again rather than resume at the level it was cut off at.
            _playing = false;
            _fadeClock = 0.0f;
            _bedLevel = 0.0f;
            _mix = 1.0f;
            _hornTimes[0] = _hornTimes[1] = -100.0f;
            ClearPending();
            _nextImpatient = _clock + Range(ImpatientMin, ImpatientMax);
        }

        private void OnDisable()
        {
            Unsubscribe();
            StopAll();
            _playing = false;
        }

        private void LateUpdate()
        {
            // ⚠️⚠️ THE MAP PREVIEW LOADS THIS SCENE BEHIND THE MENUS, AND IT MUST STAY SILENT THERE.
            // The same two signs `LagoonSoundscape` reads: `MapPreviewSurface.Silence` only strips
            // the sources a scene was LOADED with, and these are made at runtime after that pass.
            if (IsPreview())
            {
                if (_playing) StopAll();
                _playing = false;
                ClearPending();
                return;
            }

            if (!_built) Build();
            EnsureSubscribed();
            CacheDrivers();

            if (!_playing) StartBed();

            // ⚠️ `AudioListener.pause` pauses every source here already. What it does not stop is
            // this method, so the horn clocks are held too: a pause menu left open for a minute
            // must not release a queue of honks the instant it closes.
            if (AudioListener.pause) return;

            // Unscaled: a hit-stop or slow-motion `timeScale` must not stretch a fade. Clamped so a
            // hitch (a scene load, a debugger break) cannot jump a whole fade or fire every horn.
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            _clock += dt;
            _fadeClock += dt;
            float fade = Mathf.Clamp01(_fadeClock / FadeInSeconds);
            fade = fade * fade * (3.0f - 2.0f * fade);

            var director = GameServices.Audio;
            float sfx = director != null ? director.SfxVolume : 1.0f;
            // ⚠️ `IsInReplayMix` IS THE HOOK THE REPLAY LEASE EXPOSES, the one `LagoonSoundscape`,
            // `WorldContactPresentation` and `ColourGrade` read. The lease only mutes the director's
            // own pooled voices, so a component driving its own sources has to ask. Everything here
            // ducks to zero for the replay and fades back after it, and no horn fires inside one.
            bool replay = director != null && director.IsInReplayMix;
            float tau = replay ? ReplaySmoothing : Smoothing;
            float k = 1.0f - Mathf.Exp(-dt / tau);
            _mix += ((replay ? 0.0f : 1.0f) - _mix) * k;

            if (!TryListener(out Vector3 ear))
            {
                // No ears anywhere (a headless probe, a scene mid-transition): hold the last mix.
                return;
            }

            // ---- The bed ---------------------------------------------------------------------
            float road = Traffic != null ? Traffic.Road : DefaultRoad;
            float square = Mathf.Max(Mathf.Abs(ear.x), Mathf.Abs(ear.z));
            float toKerb = Mathf.InverseLerp(BedQuietWithin, Mathf.Max(BedQuietWithin + 1.0f, road - BedFullInsideRoad), square);
            float bedTarget = Mathf.Lerp(BedFloor, 1.0f, Mathf.SmoothStep(0.0f, 1.0f, toKerb));
            _bedLevel += (bedTarget - _bedLevel) * (1.0f - Mathf.Exp(-dt / Smoothing));
            // ⚠️ THE SFX SLIDER IS APPLIED AFTER THE SMOOTHING, NOT INSIDE IT, so the pause-menu
            // slider moves the street the instant it moves, like every other sound in the game.
            float scale = sfx * fade * _mix;
            if (_bed != null) _bed.volume = _bedLevel * BedGain * scale;

            // ---- Engines and horns -----------------------------------------------------------
            UpdateEngines(dt, ear, scale);
            UpdateHorns(ear, scale, replay);
        }

        // ---------------------------------------------------------------------------------------
        // Vehicles.
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Reads each vehicle's model ONCE into its engine clip, horn, pitch and level. Runs again
        /// only if the vehicle count changes (never, in a match; the builder places them).
        /// </summary>
        private void CacheDrivers()
        {
            int n = Traffic != null ? Traffic.DriverCount : 0;
            if (n == _cachedCount) return;
            _cachedCount = n;

            // ⚠️ THE ONLY ALLOCATIONS OUTSIDE `Build`, and they happen once per traffic set.
            _engineKind = new int[n];
            _hornKind = new int[n];
            _heldBy = new int[n];
            _basePitch = new float[n];
            _topPitch = new float[n];
            _modelGain = new float[n];
            _score = new float[n];
            _wanted = new bool[n];
            _served = new bool[n];

            for (int i = 0; i < n; i++)
            {
                string model = Traffic.DriverModel(i) ?? string.Empty;
                // ⚠️ `Contains`, NOT EQUALITY: the model is the dressing group's name, and a
                // duplicated group comes back as "jeepney (1)" or "sedan_red.001".
                int engine = KindCar, horn = HornCar;
                float pitch = 1.0f, top = 1.6f, gain = 0.8f;
                if (Has(model, "tricycle")) { engine = KindTricycle; horn = HornTrike; top = 1.7f; gain = 0.9f; }
                else if (Has(model, "jeepney")) { engine = KindDiesel; horn = HornJeep; top = 1.45f; gain = 1.0f; }
                else if (Has(model, "bus")) { engine = KindDiesel; horn = HornJeep; pitch = 0.9f; top = 1.35f; gain = 1.15f; }
                // ⚠️ A DELIVERY VAN OR PICKUP IS DIESEL IN MANILA (the L300 and Hilux class), but a
                // lighter one, so it takes the diesel loop pitched up, and a car's horn.
                else if (Has(model, "van") || Has(model, "pickup")) { engine = KindDiesel; pitch = 1.14f; top = 1.5f; gain = 0.85f; }
                else if (Has(model, "hatch")) pitch = 1.08f;
                else if (Has(model, "taxi")) pitch = 0.97f;

                // ⚠️ A FEW PER CENT EACH WAY PER VEHICLE, so two red sedans idling side by side are
                // two engines and not one engine doubled (which phases like a flanger).
                _basePitch[i] = pitch * Range(0.96f, 1.04f);
                _topPitch[i] = top;
                _modelGain[i] = gain;
                _engineKind[i] = engine;
                _hornKind[i] = horn;
                _heldBy[i] = -1;
            }

            for (int v = 0; v < _voices.Length; v++)
            {
                _vDriver[v] = -1;
                _vNext[v] = -1;
                _vGain[v] = 0.0f;
                if (_voices[v] != null) _voices[v].Stop();
            }
        }

        private static bool Has(string s, string part) => s.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;

        /// <summary>
        /// ⚠️ A VEHICLE WITH NO BODY REPORTS THE WORLD ORIGIN (`KantoTraffic.DriverPosition`), which
        /// is the court centre: exactly where no vehicle can ever be (the nearest lane is 20 m out).
        /// Treated as absent, or the missing car would be "nearest" to every listener on the court.
        /// </summary>
        private bool Present(int i, out Vector3 at)
        {
            at = Traffic.DriverPosition(i);
            return at != Vector3.zero;
        }

        // ---------------------------------------------------------------------------------------
        // Engines.
        // ---------------------------------------------------------------------------------------

        private void UpdateEngines(float dt, Vector3 ear, float scale)
        {
            int n = _cachedCount;
            if (_voices.Length == 0) return;

            _reassignClock -= dt;
            if (_reassignClock <= 0.0f)
            {
                _reassignClock = ReassignSeconds;
                Reassign(ear, n);
            }

            float kPitch = 1.0f - Mathf.Exp(-dt / PitchSmoothing);
            float kLoad = 1.0f - Mathf.Exp(-dt / LoadSmoothing);
            float fadeStep = dt / SwapFadeSeconds;

            for (int v = 0; v < _voices.Length; v++)
            {
                var src = _voices[v];
                if (src == null) continue;

                int d = _vDriver[v];
                float target = d >= 0 && _vNext[v] == d ? 1.0f : 0.0f;
                _vGain[v] = Mathf.MoveTowards(_vGain[v], target, fadeStep);

                if (_vNext[v] != d && _vGain[v] <= 0.0f)
                {
                    // Silent now: move the voice to its new vehicle (or park it) and fade in.
                    SwitchVoice(v, _vNext[v]);
                    d = _vDriver[v];
                }

                if (d < 0 || d >= n || !Present(d, out Vector3 at))
                {
                    src.volume = 0.0f;
                    continue;
                }

                src.transform.position = at;

                float speed = Traffic.DriverSpeed(d);
                float cruise = Mathf.Max(1.0f, Traffic.DriverCruise(d));
                float ratio = Mathf.Clamp01(speed / cruise);
                float accel = dt > 0.0f ? (speed - _vPrevSpeed[v]) / dt : 0.0f;
                _vPrevSpeed[v] = speed;
                _vLoad[v] += (Mathf.Clamp01(accel / FullThrottleAccel) - _vLoad[v]) * kLoad;

                // ⚠️ THE CURVE IS CONCAVE (ratio^0.8): an engine's note climbs fastest pulling away
                // from the line, then the gears flatten it. Linear sounded like a siren ramp.
                float pitch = _basePitch[d] * Mathf.Lerp(PitchIdle, _topPitch[d], Mathf.Pow(ratio, 0.8f))
                              + LoadPitch * _vLoad[v];
                if (Traffic.DriverWaiting(d))
                {
                    // ⚠️ AN IDLE HUNTS. A waiting engine holding one exact pitch is a drone; a
                    // slow wobble of a per cent and a half is the idle governor. Perlin is
                    // allocation-free and not a gameplay stream.
                    pitch *= 1.0f + 0.03f * (Mathf.PerlinNoise(_clock * 0.9f + v * 7.3f, v * 1.7f) - 0.5f);
                }
                pitch = Mathf.Clamp(pitch, MinPitch, MaxPitch);
                _vPitch[v] += (pitch - _vPitch[v]) * kPitch;
                src.pitch = _vPitch[v];

                float drive = 0.7f + 0.3f * ratio + LoadGain * _vLoad[v];
                src.volume = _vGain[v] * EngineGain * _modelGain[d] * drive * scale;
            }
        }

        /// <summary>
        /// Chooses which vehicles hold a voice: the nearest `EngineVoices` within reach, with a
        /// held vehicle counted as 20 per cent closer. A voice keeps its vehicle if it is still
        /// chosen; the others are pointed at the chosen vehicles nobody is serving, nearest first,
        /// and fade across in `UpdateEngines`.
        /// </summary>
        private void Reassign(Vector3 ear, int n)
        {
            int voices = _voices.Length;
            float reachSq = EngineReach * EngineReach;
            float heldSq = HeldDistanceFactor * HeldDistanceFactor;

            for (int i = 0; i < n; i++)
            {
                _wanted[i] = false;
                _served[i] = false;
                _heldBy[i] = -1;
            }
            for (int v = 0; v < voices; v++)
            {
                int d = _vDriver[v];
                if (d >= 0 && d < n && _vNext[v] == d) _heldBy[d] = v;
            }
            for (int i = 0; i < n; i++)
            {
                if (!Present(i, out Vector3 at)) { _score[i] = float.MaxValue; continue; }
                float sq = (at - ear).sqrMagnitude;
                _score[i] = sq > reachSq ? float.MaxValue : sq * (_heldBy[i] >= 0 ? heldSq : 1.0f);
            }

            // Partial selection: `voices` passes over a few dozen vehicles, four times a second.
            int picked = 0;
            for (int p = 0; p < voices; p++)
            {
                int best = -1;
                float bestScore = float.MaxValue;
                for (int i = 0; i < n; i++)
                {
                    if (_wanted[i] || _score[i] >= bestScore) continue;
                    best = i;
                    bestScore = _score[i];
                }
                if (best < 0) break;
                _wanted[best] = true;
                _pick[picked++] = best;
            }

            // Keep what is still wanted.
            for (int v = 0; v < voices; v++)
            {
                int d = _vDriver[v];
                if (d >= 0 && d < n && _wanted[d]) { _vNext[v] = d; _served[d] = true; }
                else _vNext[v] = -2;   // to be decided below
            }
            // Hand the rest out, nearest first.
            for (int v = 0; v < voices; v++)
            {
                if (_vNext[v] != -2) continue;
                int next = -1;
                for (int p = 0; p < picked; p++)
                {
                    int d = _pick[p];
                    if (_served[d]) continue;
                    next = d;
                    _served[d] = true;
                    break;
                }
                _vNext[v] = next;
            }
        }

        private void SwitchVoice(int v, int driver)
        {
            var src = _voices[v];
            _vDriver[v] = driver;
            _vGain[v] = 0.0f;
            _vLoad[v] = 0.0f;

            if (driver < 0 || driver >= _cachedCount || src == null)
            {
                if (src != null) src.Stop();
                _vKind[v] = -1;
                return;
            }

            _vPrevSpeed[v] = Traffic.DriverSpeed(driver);
            var clip = EngineClip(_engineKind[driver]);
            if (clip == null)
            {
                src.Stop();
                _vDriver[v] = _vNext[v] = -1;
                return;
            }

            if (src.clip != clip || !src.isPlaying)
            {
                src.clip = clip;
                src.volume = 0.0f;
                src.Play();
                // ⚠️ `timeSamples` AFTER `Play` (Play on a stopped source rewinds it), and a RANDOM
                // point: eight voices on the same loop started together would be one engine, louder.
                int samples = clip.samples;
                if (samples > 0) src.timeSamples = _random.Next(samples);
            }
            _vKind[v] = _engineKind[driver];

            // Start at the right note rather than gliding up from the last vehicle's.
            float cruise = Mathf.Max(1.0f, Traffic.DriverCruise(driver));
            float ratio = Mathf.Clamp01(_vPrevSpeed[v] / cruise);
            _vPitch[v] = Mathf.Clamp(_basePitch[driver] * Mathf.Lerp(PitchIdle, _topPitch[driver], Mathf.Pow(ratio, 0.8f)),
                                     MinPitch, MaxPitch);
            src.pitch = _vPitch[v];
            if (Present(driver, out Vector3 at)) src.transform.position = at;
        }

        /// <summary>The loop for an engine kind, falling back to the car loop if one is missing.</summary>
        private AudioClip EngineClip(int kind)
        {
            AudioClip clip = kind == KindDiesel ? EngineDiesel : kind == KindTricycle ? EngineTricycle : EngineCar;
            return clip != null ? clip : EngineCar;
        }

        // ---------------------------------------------------------------------------------------
        // Horns.
        // ---------------------------------------------------------------------------------------

        private void EnsureSubscribed()
        {
            if (_subscribed == Traffic) return;
            Unsubscribe();
            if (Traffic == null) return;
            if (_onGreen == null) _onGreen = OnGreenStarted;
            Traffic.GreenStarted += _onGreen;
            _subscribed = Traffic;
        }

        private void Unsubscribe()
        {
            if (_subscribed != null && _onGreen != null) _subscribed.GreenStarted -= _onGreen;
            _subscribed = null;
        }

        /// <summary>
        /// A road axis turned green. Now and then one or two drivers still waiting on it beep, a
        /// moment later (0.6 to 1.5 s: the time it takes to notice the car in front has not moved).
        /// The honking vehicle is chosen when the horn FIRES, from those still waiting then, so a
        /// driver who has already pulled away never honks.
        /// </summary>
        private void OnGreenStarted(int axis)
        {
            if (!_playing || AudioListener.pause || _mix < 0.5f) return;
            double roll = _random.NextDouble();
            int count = roll < GreenOneHorn ? 1 : roll < GreenOneHorn + GreenTwoHorns ? 2 : 0;
            float at = _clock + Range(GreenDelayMin, GreenDelayMax);
            for (int h = 0; h < count; h++)
            {
                Queue(axis, at);
                // The second driver joins in a beat after the first, not in unison.
                at += Range(0.25f, 0.6f);
            }
        }

        private void Queue(int axis, float at)
        {
            for (int p = 0; p < PendingHorns; p++)
            {
                if (_pendUsed[p]) continue;
                _pendUsed[p] = true;
                _pendAxis[p] = axis;
                _pendAt[p] = at;
                _pendExclude[p] = -1;
                return;
            }
        }

        private void ClearPending()
        {
            for (int p = 0; p < PendingHorns; p++) _pendUsed[p] = false;
        }

        private void UpdateHorns(Vector3 ear, float scale, bool replay)
        {
            // A horn already sounding follows its vehicle and tracks the slider and a replay duck.
            for (int h = 0; h < HornVoices; h++)
            {
                var src = _horns[h];
                if (src == null || !src.isPlaying) continue;
                src.volume = HornGain * _hornClipGain[h] * scale;
                int d = _hornDriver[h];
                if (d >= 0 && d < _cachedCount && Present(d, out Vector3 at)) src.transform.position = at;
            }

            if (Traffic == null || _cachedCount <= 0) return;

            // ⚠️ NO HORN IS STARTED DURING A REPLAY OR THE FADE-IN: the queue is dropped rather than
            // held, because a honk saved from before a replay means nothing after it.
            if (replay || scale <= 0.001f)
            {
                ClearPending();
                return;
            }

            for (int p = 0; p < PendingHorns; p++)
            {
                if (!_pendUsed[p] || _clock < _pendAt[p]) continue;
                _pendUsed[p] = false;
                TryHonk(_pendAxis[p], ear, scale);
            }

            if (_clock >= _nextImpatient)
            {
                bool honked = TryHonk(-1, ear, scale);
                _nextImpatient = _clock + (honked ? Range(ImpatientMin, ImpatientMax) : Range(ImpatientRetryMin, ImpatientRetryMax));
            }
        }

        /// <summary>
        /// One honk from a waiting vehicle on <paramref name="axis"/> (or any axis for -1), inside
        /// the window rule. Returns false if the window is full or nobody suitable is waiting.
        /// </summary>
        private bool TryHonk(int axis, Vector3 ear, float scale)
        {
            float oldest = Mathf.Min(_hornTimes[0], _hornTimes[1]);
            if (_clock - oldest < HornWindow) return false;

            int n = _cachedCount;
            float reachSq = HornReach * HornReach;
            int sounding0 = _horns[0] != null && _horns[0].isPlaying ? _hornDriver[0] : -1;
            int sounding1 = _horns[1] != null && _horns[1].isPlaying ? _hornDriver[1] : -1;

            // Two passes, no list: count the candidates, then pick random ones by index.
            int candidates = 0;
            for (int i = 0; i < n; i++)
                if (Candidate(i, axis, ear, reachSq, sounding0, sounding1)) candidates++;
            if (candidates == 0) return false;

            int chosen = -1;
            float chosenSq = float.MaxValue;
            for (int s = 0; s < HornSamples; s++)
            {
                int want = _random.Next(candidates);
                for (int i = 0; i < n; i++)
                {
                    if (!Candidate(i, axis, ear, reachSq, sounding0, sounding1)) continue;
                    if (want-- > 0) continue;
                    Present(i, out Vector3 at);
                    float sq = (at - ear).sqrMagnitude;
                    if (sq < chosenSq) { chosenSq = sq; chosen = i; }
                    break;
                }
            }
            if (chosen < 0) return false;

            float clipGain;
            AudioClip clip = HornClip(_hornKind[chosen], out clipGain);
            if (clip == null) return false;

            int voice = _nextHornVoice;
            if (_horns[voice] != null && _horns[voice].isPlaying)
            {
                int other = (voice + 1) % HornVoices;
                if (_horns[other] == null || !_horns[other].isPlaying) voice = other;
            }
            _nextHornVoice = (voice + 1) % HornVoices;
            var src = _horns[voice];
            if (src == null) return false;

            Present(chosen, out Vector3 pos);
            src.transform.position = pos;
            src.clip = clip;
            // A few per cent of pitch per honk: the same clip twice in a row must not be a copy.
            src.pitch = Range(0.96f, 1.04f);
            _hornClipGain[voice] = clipGain;
            _hornDriver[voice] = chosen;
            src.volume = HornGain * clipGain * scale;
            src.Play();

            // Replace the older of the two stamps.
            if (_hornTimes[0] <= _hornTimes[1]) _hornTimes[0] = _clock;
            else _hornTimes[1] = _clock;
            return true;
        }

        private bool Candidate(int i, int axis, Vector3 ear, float reachSq, int sounding0, int sounding1)
        {
            if (i == sounding0 || i == sounding1) return false;
            if (!Traffic.DriverWaiting(i)) return false;
            if (axis >= 0 && Traffic.DriverAxis(i) != axis) return false;
            if (!Present(i, out Vector3 at)) return false;
            return (at - ear).sqrMagnitude <= reachSq;
        }

        /// <summary>
        /// Tricycles take the tricycle horn, jeepneys and buses the jeepney's air horn, everything
        /// else one of the car horns. ⚠️ The jeepney's call is the loudest thing in the set by
        /// design, so it plays a little under the rest; the tricycle's is thin and plays a little
        /// over, or it disappears under the bed.
        /// </summary>
        private AudioClip HornClip(int kind, out float gain)
        {
            gain = 1.0f;
            if (kind == HornTrike && HornTricycle != null) { gain = 1.1f; return HornTricycle; }
            if (kind == HornJeep && HornJeepney != null) { gain = 0.85f; return HornJeepney; }
            if (HornsCar == null || HornsCar.Length == 0) return null;
            return HornsCar[_random.Next(HornsCar.Length)];
        }

        // ---------------------------------------------------------------------------------------
        // Listener.
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// The ears: the enabled `AudioListener` (there is exactly one, on `AudioDirector`'s "Ears",
        /// which follows `Camera.main`), else `Camera.main` itself.
        /// </summary>
        private bool TryListener(out Vector3 ear)
        {
            if (_listener == null || !_listener.isActiveAndEnabled)
            {
                _listener = null;
                _listenerRetry -= Time.unscaledDeltaTime;
                if (_listenerRetry <= 0.0f)
                {
                    // ⚠️ THE ONLY ALLOCATION ON THIS PATH, AND IT IS RARE: only when the cached
                    // listener is gone, and throttled to twice a second.
                    _listenerRetry = ListenerRetrySeconds;
                    var all = FindObjectsByType<AudioListener>();
                    for (int i = 0; i < all.Length; i++)
                        if (all[i] != null && all[i].isActiveAndEnabled) { _listener = all[i]; break; }
                }
            }

            Transform source = null;
            if (_listener != null) source = _listener.transform;
            else
            {
                var cam = Camera.main;
                if (cam != null) source = cam.transform;
            }

            if (source == null)
            {
                ear = Vector3.zero;
                return false;
            }
            ear = source.position;
            return true;
        }

        // ---------------------------------------------------------------------------------------
        // Sources.
        // ---------------------------------------------------------------------------------------

        private void Build()
        {
            _built = true;

            if (CityBed != null)
            {
                var go = new GameObject("KantoCityBed");
                go.transform.SetParent(transform, false);
                _bed = go.AddComponent<AudioSource>();
                _bed.playOnAwake = false;
                _bed.loop = true;
                _bed.clip = CityBed;
                // ⚠️ 2D ON PURPOSE: the city is all round the park, and any one point for it would
                // be wrong from most of the court. Its level is placed by the arithmetic above.
                _bed.spatialBlend = 0.0f;
                _bed.dopplerLevel = 0.0f;
                _bed.volume = 0.0f;
                // ⚠️ High priority (lower is higher): a bed culled by the voice limit during a busy
                // exchange drops out as a hole in the world.
                _bed.priority = 40;
            }

            int voices = Mathf.Clamp(EngineVoices, 0, MaxEngineVoices);
            _voices = new AudioSource[voices];
            _vDriver = new int[voices];
            _vNext = new int[voices];
            _vKind = new int[voices];
            _vGain = new float[voices];
            _vPitch = new float[voices];
            _vLoad = new float[voices];
            _vPrevSpeed = new float[voices];
            _pick = new int[voices];
            for (int v = 0; v < voices; v++)
            {
                var go = new GameObject($"KantoEngine{v}");
                go.transform.SetParent(transform, false);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = true;
                // ⚠️ 3D, LOGARITHMIC, 3.5 m INNER RADIUS, NO DOPPLER. The nearest lane is 20 m from
                // the court centre, where a 3.5 m log rolloff leaves about -15 dB: present, under
                // the play. The director's 2 to 32 m linear rolloff for court impacts would leave a
                // car across the road at a third of that. Doppler is off because Unity computes it
                // from the SOURCE transform's motion, and a voice jumping to a new vehicle would
                // read as a 100 m/s move and squeal.
                src.spatialBlend = 1.0f;
                src.rolloffMode = AudioRolloffMode.Logarithmic;
                src.minDistance = 3.5f;
                src.maxDistance = EngineReach;
                src.dopplerLevel = 0.0f;
                src.volume = 0.0f;
                // Below the beds and horns: the engine pool is the first thing to give way.
                src.priority = 160;
                _voices[v] = src;
                _vDriver[v] = _vNext[v] = _vKind[v] = -1;
            }

            for (int h = 0; h < HornVoices; h++)
            {
                var go = new GameObject($"KantoHorn{h}");
                go.transform.SetParent(transform, false);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = false;
                src.spatialBlend = 1.0f;
                // Horns carry further than engines: a 5 m inner radius, heard to 80 m.
                src.rolloffMode = AudioRolloffMode.Logarithmic;
                src.minDistance = 5.0f;
                src.maxDistance = 80.0f;
                src.dopplerLevel = 0.0f;
                src.priority = 120;
                _horns[h] = src;
                _hornDriver[h] = -1;
            }

            // Force the vehicle cache to rebuild against the new voice arrays.
            _cachedCount = -1;
        }

        private void StartBed()
        {
            _playing = true;
            _fadeClock = 0.0f;
            _reassignClock = 0.0f;
            if (_bed != null && _bed.clip != null)
            {
                _bed.volume = 0.0f;
                _bed.Play();
                // ⚠️ A RANDOM START, so every match does not open on the same distant horn.
                int n = _bed.clip.samples;
                if (n > 0) _bed.timeSamples = _random.Next(n);
            }
        }

        private void StopAll()
        {
            if (_bed != null) _bed.Stop();
            for (int v = 0; v < _voices.Length; v++)
            {
                if (_voices[v] != null) _voices[v].Stop();
                _vDriver[v] = _vNext[v] = _vKind[v] = -1;
                _vGain[v] = 0.0f;
            }
            for (int h = 0; h < HornVoices; h++)
            {
                if (_horns[h] != null) _horns[h].Stop();
                _hornDriver[h] = -1;
            }
        }

        private float Range(float a, float b) => a + (b - a) * (float)_random.NextDouble();

        private bool IsPreview()
            => MatchInstaller.PreviewOnly || gameObject.layer == UI.MapPreviewSurface.PreviewLayer;
    }
}
