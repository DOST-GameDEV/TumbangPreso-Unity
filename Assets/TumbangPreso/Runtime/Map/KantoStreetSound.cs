using System;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// Kanto's street sound: the city around the park, the engines of the traffic that drives past
    /// it, the horns of drivers who are waiting, and the odd siren across the block.
    ///
    /// Owner brief, 2026-09-27, about Kanto's moving traffic: *"needs sfx, bustling city ambience,
    /// car engine and driving sounds, beep/horns"*. After hearing the first version in play, the
    /// same day: *"the sfx for the city is too calm. need to be more bustling, add some sirens here
    /// and louder and more variety of beeps"*. The clips are authored by
    /// `tools/build_kanto_street_audio.py`; the scene builder adds this component next to
    /// <see cref="KantoTraffic"/> and fills every public field.
    ///
    /// Four layers:
    /// 1. THE CITY BED, one 2D loop whose level rises as the listener walks from the court toward
    ///    the kerb (the streets are 22 m out on all four sides, the play walls at +/-13).
    /// 2. ENGINES, a small pool of 3D loops handed to whichever vehicles are nearest the listener,
    ///    pitched by each vehicle's speed and lifted a little under acceleration.
    /// 3. HORNS, one-shots from vehicles that are waiting or crawling: beeps when their light turns
    ///    green, impatient honks, and now and then a second driver answering the first.
    /// 4. SIRENS, a whole pass by an emergency vehicle that is heard (never seen) driving along one
    ///    of the far roads every 35 to 90 s.
    ///
    /// ⚠️⚠️ PRESENTATION ONLY. Nothing here is networked, nothing reads or writes match state, and
    /// every random choice uses its own `System.Random`, never a gameplay stream: a seeded probe
    /// (`BotBehaviourProbe`) must read the same numbers with or without this component in the map.
    /// It only READS <see cref="KantoTraffic"/> through the accessors that class exposes for it (and
    /// its public road geometry for the siren's path), and never changes the traffic.
    ///
    /// ⚠️⚠️ THE CLIPS ARE PROVISIONAL UNTIL THE OWNER HEARS THEM IN PLAY (CLAUDE.md section 6).
    /// `docs/reports/kanto-street-audio-authoring.json` records the measurements and that listening
    /// acceptance is still open. The four gains below are the knobs for that session.
    /// </summary>
    /// ⚠️ ORDER 1001, ONE AFTER `AudioDirector` (1000), the same as `LagoonSoundscape`. The
    /// director's `LateUpdate` copies the camera pose onto the one `AudioListener`; running after it
    /// means the nearest-vehicle choice is made from the pose the listener hears this frame.
    [DefaultExecutionOrder(1001)]
    [DisallowMultipleComponent]
    public sealed class KantoStreetSound : MonoBehaviour
    {
        // Shared city-map trim; preserve authored voice balance and the user SFX slider.
        internal const float AmbientGainScale = .75f;
        public KantoTraffic Traffic;
        public AudioClip CityBed, EngineCar, EngineDiesel, EngineTricycle;
        public AudioClip[] HornsCar, HornsJeepney, HornsTricycle;
        public AudioClip HornBus, HornTruck;
        public AudioClip[] Sirens;
        [Range(0, 1)] public float BedGain = 0.7f, EngineGain = 0.5f, HornGain = 0.8f, SirenGain = 0.6f;
        public int EngineVoices = 8;

        /// <summary>
        /// ⚠️⚠️ LEGACY, NOT PLAYED. The first set had one jeepney horn and one tricycle horn; the
        /// rework replaced them with the `HornsJeepney` and `HornsTricycle` arrays and deleted the
        /// two files. These two fields stay only so the editor's scene author, which still assigns
        /// them, keeps compiling in the open editor. Nothing here reads them. Delete them in the
        /// same commit that moves `KantoTrafficAuthor` onto the arrays.
        /// </summary>

        // ---------------------------------------------------------------------------------------
        // Tuning: the city bed. Distances are flat (XZ) from the court centre, the world origin
        // that `KantoTraffic` lays its lanes around.
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// ⚠️ A FLOOR, NOT SILENCE, AND A HIGH ONE. The court sits in the middle of a busy block. The
        /// first version held 0.45 here and the owner heard the middle of the court as "too calm";
        /// 0.6 keeps the city loud at the centre spot while still opening up toward the kerb.
        /// </summary>
        private const float BedFloor = 0.6f;
        /// <summary>Inside this (the court) the bed holds at its floor.</summary>
        private const float BedQuietWithin = 6.0f;
        /// <summary>
        /// The bed is full this far INSIDE the road centre line: 22 - 4 = 18 m, the kerb. Measured on
        /// the SQUARE distance (the larger of |x| and |z|), because the four roads make a square
        /// round the park and a round distance would call a corner quieter than a side.
        /// </summary>
        private const float BedFullInsideRoad = 4.0f;
        /// <summary>Used when `Traffic` is missing: the map's # grid (KantoTraffic's defaults).</summary>
        private const float DefaultRoad = 22.0f, DefaultExtent = 128.0f, DefaultLaneOffset = 1.8f;

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
        /// <summary>A voice changing vehicle fades out, moves, and fades in, each this long.</summary>
        private const float SwapFadeSeconds = 0.2f;
        /// <summary>Pitch while waiting at a light or in a queue: the loops are authored at idle.</summary>
        private const float PitchIdle = 0.85f;
        /// <summary>Extra pitch at full throttle: the revs run ahead of the road speed under load.</summary>
        private const float LoadPitch = 0.08f;
        /// <summary>Extra level at full throttle, the "volume up a little under load".</summary>
        private const float LoadGain = 0.35f;
        /// <summary>The acceleration read as full throttle: `KantoTraffic`'s own amax, 1.6 m/s^2.</summary>
        private const float FullThrottleAccel = 1.6f;
        /// <summary>Pitch glides at this time constant, so a change of speed never steps the note.</summary>
        private const float PitchSmoothing = 0.15f;
        private const float LoadSmoothing = 0.25f;
        private const float MinPitch = 0.7f, MaxPitch = 1.9f;
        private const int MaxEngineVoices = 16;

        // ---------------------------------------------------------------------------------------
        // Tuning: horns. ⚠️⚠️ ALL RAISED 2026-09-27 AFTER THE OWNER'S "louder and more variety of
        // beeps": the first version honked at under half of the greens and once every 12 to 35 s
        // otherwise, which on a short round could mean hearing two horns in total.
        // ---------------------------------------------------------------------------------------

        /// <summary>When an axis turns green, horns sound this often; then 1, 2 or 3 of them.</summary>
        private const float GreenHornChance = 0.7f;
        private const float GreenDelayMin = 0.6f, GreenDelayMax = 1.5f;
        /// <summary>An impatient honk from a waiting or crawling vehicle, every 4 to 12 s.</summary>
        private const float ImpatientMin = 4.0f, ImpatientMax = 12.0f;
        /// <summary>No candidate in reach, or the window was full: look again this soon.</summary>
        private const float ImpatientRetryMin = 1.5f, ImpatientRetryMax = 3.0f;
        /// <summary>
        /// ⚠️ AN EXCHANGE: after a honk, this often a second driver answers within half a second. It
        /// is the most "Manila" sound in the set: nobody honks alone. An answer never gets answered,
        /// or one honk could chain into a whole street leaning on their horns.
        /// </summary>
        private const float AnswerChance = 0.3f;
        private const float AnswerDelayMin = 0.15f, AnswerDelayMax = 0.5f;
        /// <summary>A vehicle below this share of its cruise speed counts as crawling (and may honk).</summary>
        private const float CrawlFraction = 0.35f;
        /// <summary>
        /// ⚠️⚠️ NEVER MORE THAN THREE HORNS INSIDE THIS WINDOW (the rework's 3 in 1.5 s; the first
        /// version allowed 2). Busy, but a green light on a full axis plus an exchange could
        /// otherwise stack five beeps into one second, which stops reading as a street and starts
        /// reading as an alarm.
        /// </summary>
        private const float HornWindow = 1.5f;
        private const int HornsPerWindow = 3;
        /// <summary>
        /// Four voices: three may start inside any 1.5 s, and the longest clip (the 1.7 s lean-on,
        /// 1.77 s at the lowest pitch) can still be sounding when a fourth is allowed.
        /// </summary>
        private const int HornVoices = 4;
        private const float HornReach = 90.0f;
        private const float HornMaxDistance = 100.0f;
        private const int PendingHorns = 8;
        /// <summary>How many random candidates are sampled to pick the one that honks; the nearest of
        /// them wins, so a honk usually comes from a car the player can see.</summary>
        private const int HornSamples = 3;

        // ---------------------------------------------------------------------------------------
        // Tuning: sirens.
        // ---------------------------------------------------------------------------------------

        private const float SirenGapMin = 35.0f, SirenGapMax = 90.0f;
        /// <summary>Blocked (a replay, the fade-in, the slider at zero): try again this soon.</summary>
        private const float SirenRetrySeconds = 5.0f;
        private const float SirenSpeedMin = 15.0f, SirenSpeedMax = 20.0f;
        /// <summary>
        /// ⚠️ WHERE IN THE CLIP THE VEHICLE IS NEAREST. The authored passes swell to their nearest
        /// point at 45 to 52 per cent of the file, so the moving source is started half a clip's
        /// travel before the listener's side of the road: the level the file bakes in and the level
        /// the 3D rolloff gives peak together instead of fighting.
        /// </summary>
        private const float SirenNearestAt = 0.5f;
        /// <summary>A siren sits on a roof bar, above the traffic.</summary>
        private const float SirenHeight = 2.2f;
        /// <summary>
        /// ⚠️ WIDE ON PURPOSE: heard across the block. A 12 m inner radius and 150 m reach leave a
        /// siren on the far road (about 22 to 45 m away) loud enough to turn a head, which is the
        /// point of a siren.
        /// </summary>
        private const float SirenMinDistance = 12.0f, SirenMaxDistance = 150.0f;

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
        private const int HornCar = 0, HornJeep = 1, HornTrike = 2, HornBigBus = 3, HornPickup = 4;

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
        private readonly float[] _hornStarted = new float[HornVoices];
        private readonly float[] _hornTimes = new float[HornsPerWindow];
        private readonly int[] _pendAxis = new int[PendingHorns];
        private readonly int[] _pendExclude = new int[PendingHorns];
        private readonly float[] _pendAt = new float[PendingHorns];
        private readonly bool[] _pendUsed = new bool[PendingHorns];
        private readonly bool[] _pendSlowOk = new bool[PendingHorns];
        private readonly bool[] _pendAnswer = new bool[PendingHorns];
        private int _lastCarHorn = -1, _lastJeepHorn = -1, _lastTrikeHorn = -1;
        private float _nextImpatient;
        private KantoTraffic _subscribed;
        private Action<int> _onGreen;

        // Siren.
        private AudioSource _siren;
        private float _nextSiren, _sirenAlong, _sirenSpeed;
        private Vector3 _sirenOrigin, _sirenDir;
        private int _lastSiren = -1;

        private void OnEnable()
        {
            // ⚠️ EVERY ENABLE STARTS FROM SILENCE, like the lagoon: the map preview parks and
            // unparks whole scenes by toggling their roots, and a scene returning from a park must
            // fade in again rather than resume at the level it was cut off at.
            _playing = false;
            _fadeClock = 0.0f;
            _bedLevel = 0.0f;
            _mix = 1.0f;
            for (int i = 0; i < HornsPerWindow; i++) _hornTimes[i] = -100.0f;
            ClearPending();
            _nextImpatient = _clock + Range(ImpatientMin, ImpatientMax);
            _nextSiren = _clock + Range(SirenGapMin, SirenGapMax);
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
            // this method, so the horn and siren clocks are held too: a pause menu left open for a
            // minute must not release a queue of honks (or a siren) the instant it closes.
            if (AudioListener.pause) return;

            // Unscaled: a hit-stop or slow-motion `timeScale` must not stretch a fade. Clamped so a
            // hitch (a scene load, a debugger break) cannot jump a whole fade or fire every horn.
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            _clock += dt;
            _fadeClock += dt;
            float fade = Mathf.Clamp01(_fadeClock / FadeInSeconds);
            fade = fade * fade * (3.0f - 2.0f * fade);

            var director = GameServices.Audio;
            float sfx = director != null ? director.AmbienceVolume : 1.0f;
            // ⚠️ `IsInReplayMix` IS THE HOOK THE REPLAY LEASE EXPOSES, the one `LagoonSoundscape`,
            // `WorldContactPresentation` and `ColourGrade` read. The lease only mutes the director's
            // own pooled voices, so a component driving its own sources has to ask. Everything here
            // ducks to zero for the replay and fades back after it, and no horn or siren starts
            // inside one.
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
            float scale = sfx * fade * _mix * AmbientGainScale;
            if (_bed != null) _bed.volume = _bedLevel * BedGain * scale;

            // ---- Engines, horns, sirens ------------------------------------------------------
            UpdateEngines(dt, ear, scale);
            UpdateHorns(ear, scale, replay);
            UpdateSiren(dt, ear, scale, replay);
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
                else if (Has(model, "bus")) { engine = KindDiesel; horn = HornBigBus; pitch = 0.9f; top = 1.35f; gain = 1.15f; }
                // ⚠️ A DELIVERY VAN OR PICKUP IS DIESEL IN MANILA (the L300 and Hilux class), but a
                // lighter one, so it takes the diesel loop pitched up, and the truck horn.
                else if (Has(model, "van") || Has(model, "pickup")) { engine = KindDiesel; horn = HornPickup; pitch = 1.14f; top = 1.5f; gain = 0.85f; }
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
        /// A road axis turned green. Seven times in ten, one to three drivers still waiting on it
        /// beep, a moment later (0.6 to 1.5 s: the time it takes to notice the car in front has not
        /// moved), each a beat after the last. The honking vehicle is chosen when the horn FIRES,
        /// from those still waiting then, so a driver who has already pulled away never honks.
        /// </summary>
        private void OnGreenStarted(int axis)
        {
            if (!_playing || AudioListener.pause || _mix < 0.5f) return;
            if (_random.NextDouble() >= GreenHornChance) return;
            double roll = _random.NextDouble();
            int count = roll < 0.5 ? 1 : roll < 0.8 ? 2 : 3;
            float at = _clock + Range(GreenDelayMin, GreenDelayMax);
            for (int h = 0; h < count; h++)
            {
                Queue(axis, at, -1, false, false);
                at += Range(0.2f, 0.7f);
            }
        }

        private void Queue(int axis, float at, int exclude, bool slowOk, bool answer)
        {
            for (int p = 0; p < PendingHorns; p++)
            {
                if (_pendUsed[p]) continue;
                _pendUsed[p] = true;
                _pendAxis[p] = axis;
                _pendAt[p] = at;
                _pendExclude[p] = exclude;
                _pendSlowOk[p] = slowOk;
                _pendAnswer[p] = answer;
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
                int honked = TryHonk(_pendAxis[p], ear, scale, _pendExclude[p], _pendSlowOk[p]);
                if (honked >= 0 && !_pendAnswer[p]) MaybeAnswer(honked);
            }

            if (_clock >= _nextImpatient)
            {
                int honked = TryHonk(-1, ear, scale, -1, true);
                _nextImpatient = _clock + (honked >= 0 ? Range(ImpatientMin, ImpatientMax) : Range(ImpatientRetryMin, ImpatientRetryMax));
                if (honked >= 0) MaybeAnswer(honked);
            }
        }

        /// <summary>Now and then another driver, anywhere nearby, honks back within half a second.</summary>
        private void MaybeAnswer(int first)
        {
            if (_random.NextDouble() >= AnswerChance) return;
            Queue(-1, _clock + Range(AnswerDelayMin, AnswerDelayMax), first, true, true);
        }

        /// <summary>
        /// One honk from a waiting (or, when <paramref name="slowOk"/>, crawling) vehicle on
        /// <paramref name="axis"/> (or any axis for -1), never <paramref name="exclude"/>, inside the
        /// window rule. Returns the vehicle that honked, or -1.
        /// </summary>
        private int TryHonk(int axis, Vector3 ear, float scale, int exclude, bool slowOk)
        {
            // The window: the oldest of the last three stamps must be at least 1.5 s ago.
            float oldest = _hornTimes[0];
            int oldestSlot = 0;
            for (int i = 1; i < HornsPerWindow; i++)
                if (_hornTimes[i] < oldest) { oldest = _hornTimes[i]; oldestSlot = i; }
            if (_clock - oldest < HornWindow) return -1;

            int n = _cachedCount;
            float reachSq = HornReach * HornReach;

            // Two passes, no list: count the candidates, then pick random ones by index.
            int candidates = 0;
            for (int i = 0; i < n; i++)
                if (Candidate(i, axis, ear, reachSq, exclude, slowOk)) candidates++;
            if (candidates == 0) return -1;

            int chosen = -1;
            float chosenSq = float.MaxValue;
            for (int s = 0; s < HornSamples; s++)
            {
                int want = _random.Next(candidates);
                for (int i = 0; i < n; i++)
                {
                    if (!Candidate(i, axis, ear, reachSq, exclude, slowOk)) continue;
                    if (want-- > 0) continue;
                    Present(i, out Vector3 at);
                    float sq = (at - ear).sqrMagnitude;
                    if (sq < chosenSq) { chosenSq = sq; chosen = i; }
                    break;
                }
            }
            if (chosen < 0) return -1;

            AudioClip clip = HornClip(_hornKind[chosen], out float clipGain);
            if (clip == null) return -1;

            // A free voice, else the one that started longest ago.
            int voice = -1;
            float earliest = float.MaxValue;
            for (int h = 0; h < HornVoices; h++)
            {
                if (_horns[h] == null) continue;
                if (!_horns[h].isPlaying) { voice = h; break; }
                if (_hornStarted[h] < earliest) { earliest = _hornStarted[h]; voice = h; }
            }
            if (voice < 0) return -1;
            var src = _horns[voice];

            Present(chosen, out Vector3 pos);
            src.transform.position = pos;
            src.clip = clip;
            // A few per cent of pitch per honk: the same clip twice in a row must not be a copy.
            src.pitch = Range(0.96f, 1.04f);
            _hornClipGain[voice] = clipGain;
            _hornDriver[voice] = chosen;
            _hornStarted[voice] = _clock;
            src.volume = HornGain * clipGain * scale;
            src.Play();

            _hornTimes[oldestSlot] = _clock;
            return chosen;
        }

        private bool Candidate(int i, int axis, Vector3 ear, float reachSq, int exclude, bool slowOk)
        {
            if (i == exclude || Sounding(i)) return false;
            if (!Traffic.DriverWaiting(i))
            {
                // ⚠️ CRAWLING COUNTS FOR IMPATIENCE AND ANSWERS, NOT FOR A GREEN: the green's horns
                // are the drivers stuck behind someone who has not moved yet.
                if (!slowOk) return false;
                if (Traffic.DriverSpeed(i) > CrawlFraction * Mathf.Max(1.0f, Traffic.DriverCruise(i))) return false;
            }
            if (axis >= 0 && Traffic.DriverAxis(i) != axis) return false;
            if (!Present(i, out Vector3 at)) return false;
            return (at - ear).sqrMagnitude <= reachSq;
        }

        private bool Sounding(int driver)
        {
            for (int h = 0; h < HornVoices; h++)
                if (_hornDriver[h] == driver && _horns[h] != null && _horns[h].isPlaying) return true;
            return false;
        }

        /// <summary>
        /// Tricycles take a tricycle horn, jeepneys a jeepney air horn, buses the bus horn, vans and
        /// pickups the truck horn, everything else a car horn. ⚠️ Missing clips fall back down the
        /// same line (bus to jeepney, truck to car) so a half-filled component still honks.
        /// ⚠️ The jeepney and bus air horns are the loudest things in the set by design, so they play
        /// a little under the rest; the tricycle's is thin and plays a little over, or it disappears
        /// under the bed.
        /// </summary>
        private AudioClip HornClip(int kind, out float gain)
        {
            gain = 1.0f;
            AudioClip clip = null;
            if (kind == HornTrike)
            {
                clip = PickFresh(HornsTricycle, ref _lastTrikeHorn);
                gain = 1.1f;
            }
            else if (kind == HornBigBus)
            {
                clip = HornBus;
                gain = 0.9f;
                if (clip == null) clip = PickFresh(HornsJeepney, ref _lastJeepHorn);
            }
            else if (kind == HornJeep)
            {
                clip = PickFresh(HornsJeepney, ref _lastJeepHorn);
                gain = 0.85f;
            }
            else if (kind == HornPickup)
            {
                clip = HornTruck;
            }

            if (clip == null)
            {
                gain = 1.0f;
                clip = PickFresh(HornsCar, ref _lastCarHorn);
            }
            return clip;
        }

        /// <summary>
        /// A random clip from the set, never the one this set played last: with eight car horns,
        /// hearing the same one twice in a row reads as one car honking again, not a second car.
        /// </summary>
        private AudioClip PickFresh(AudioClip[] set, ref int last)
        {
            if (set == null || set.Length == 0) return null;
            int i = _random.Next(set.Length);
            if (set.Length > 1 && i == last) i = (i + 1 + _random.Next(set.Length - 1)) % set.Length;
            last = i;
            return set[i];
        }

        // ---------------------------------------------------------------------------------------
        // Sirens.
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// One siren at a time, every 35 to 90 s. The source is MOVED along a lane of one of the
        /// two roads furthest from the listener at 15 to 20 m/s, the same lane geometry
        /// `KantoTraffic` drives (lines at x and z = +/-Road, a lane `LaneOffset` to the right of
        /// travel, clamped to +/-Extent), so the pass is heard crossing the block.
        ///
        /// ⚠️ DOPPLER IS OFF ON THIS SOURCE. The clips already bake a gentle Doppler bend and a
        /// swell into the pass; Unity's own Doppler on top would double the bend.
        /// </summary>
        private void UpdateSiren(float dt, Vector3 ear, float scale, bool replay)
        {
            if (_siren == null) return;

            if (_siren.isPlaying)
            {
                float extent = Traffic != null ? Traffic.Extent : DefaultExtent;
                _sirenAlong = Mathf.Min(_sirenAlong + _sirenSpeed * dt, extent);
                _siren.transform.position = _sirenOrigin + _sirenDir * _sirenAlong + Vector3.up * SirenHeight;
                _siren.volume = SirenGain * scale;
                return;
            }

            if (_clock < _nextSiren) return;
            if (Sirens == null || Sirens.Length == 0) { _nextSiren = _clock + SirenGapMax; return; }
            if (replay || scale <= 0.001f) { _nextSiren = _clock + SirenRetrySeconds; return; }

            int pick = _random.Next(Sirens.Length);
            if (Sirens.Length > 1 && pick == _lastSiren) pick = (pick + 1 + _random.Next(Sirens.Length - 1)) % Sirens.Length;
            var clip = Sirens[pick];
            if (clip == null) { _nextSiren = _clock + SirenRetrySeconds; return; }
            _lastSiren = pick;

            StartSiren(clip, ear, scale);
            _nextSiren = _clock + clip.length + Range(SirenGapMin, SirenGapMax);
        }

        private void StartSiren(AudioClip clip, Vector3 ear, float scale)
        {
            float road = Traffic != null ? Traffic.Road : DefaultRoad;
            float extent = Traffic != null ? Traffic.Extent : DefaultExtent;
            float laneOffset = Traffic != null ? Traffic.LaneOffset : DefaultLaneOffset;
            float groundY = Traffic != null ? Traffic.GroundY : 0.0f;

            // The four road lines, by how far each is from the listener; take one of the two
            // furthest (a siren across the block, not in the player's lap).
            // Road r: axis 0 (runs along X at z = line) for r = 0, 1; axis 1 (along Z at x = line)
            // for r = 2, 3; line = -road for even r, +road for odd.
            int far1 = -1, far2 = -1;
            float d1 = -1.0f, d2 = -1.0f;
            for (int r = 0; r < 4; r++)
            {
                float line = (r & 1) == 0 ? -road : road;
                float d = r < 2 ? Mathf.Abs(ear.z - line) : Mathf.Abs(ear.x - line);
                if (d > d1) { far2 = far1; d2 = d1; far1 = r; d1 = d; }
                else if (d > d2) { far2 = r; d2 = d; }
            }
            int chosen = _random.NextDouble() < 0.5 ? far1 : far2;
            if (chosen < 0) chosen = far1;
            float chosenLine = (chosen & 1) == 0 ? -road : road;
            float sign = _random.NextDouble() < 0.5 ? -1.0f : 1.0f;

            Vector3 dir = chosen < 2 ? new Vector3(sign, 0.0f, 0.0f) : new Vector3(0.0f, 0.0f, sign);
            Vector3 linePoint = chosen < 2 ? new Vector3(0.0f, groundY, chosenLine) : new Vector3(chosenLine, groundY, 0.0f);
            // ⚠️ RIGHT OF TRAVEL, derived exactly as `KantoTraffic.BuildLanes` derives it.
            Vector3 right = Vector3.Cross(Vector3.up, dir);
            _sirenOrigin = linePoint + right * laneOffset;
            _sirenDir = dir;
            _sirenSpeed = Range(SirenSpeedMin, SirenSpeedMax);

            // Nearest to the listener at the clip's swell: start half a clip's travel before the
            // listener's own position along the road.
            float listenerAlong = Vector3.Dot(ear - _sirenOrigin, dir);
            _sirenAlong = Mathf.Clamp(listenerAlong - _sirenSpeed * clip.length * SirenNearestAt, -extent, extent);

            _siren.transform.position = _sirenOrigin + _sirenDir * _sirenAlong + Vector3.up * SirenHeight;
            _siren.clip = clip;
            _siren.pitch = 1.0f;
            _siren.volume = SirenGain * scale;
            _siren.Play();
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
                // the play. Doppler is off because Unity computes it from the SOURCE transform's
                // motion, and a voice jumping to a new vehicle would read as a 100 m/s move and
                // squeal.
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
                // ⚠️ Horns carry: a 5 m inner radius, heard to 100 m (it was 80 before the owner
                // asked for louder beeps). Doppler off for the same reason as the engines.
                src.rolloffMode = AudioRolloffMode.Logarithmic;
                src.minDistance = 5.0f;
                src.maxDistance = HornMaxDistance;
                src.dopplerLevel = 0.0f;
                src.priority = 120;
                _horns[h] = src;
                _hornDriver[h] = -1;
                _hornStarted[h] = -100.0f;
            }

            {
                var go = new GameObject("KantoSiren");
                go.transform.SetParent(transform, false);
                _siren = go.AddComponent<AudioSource>();
                _siren.playOnAwake = false;
                _siren.loop = false;
                _siren.spatialBlend = 1.0f;
                _siren.rolloffMode = AudioRolloffMode.Logarithmic;
                _siren.minDistance = SirenMinDistance;
                _siren.maxDistance = SirenMaxDistance;
                _siren.dopplerLevel = 0.0f;
                _siren.priority = 100;
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
            if (_siren != null) _siren.Stop();
        }

        private float Range(float a, float b) => a + (b - a) * (float)_random.NextDouble();

        private bool IsPreview()
            => MatchInstaller.PreviewOnly || gameObject.layer == UI.MapPreviewSurface.PreviewLayer;
    }
}
