using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.Map
{
    /// <summary>
    /// THE ARENA'S MATCH OPENING (owner, 2026-10-05, with a frame of Blue Lock's players at a
    /// tunnel mouth: "the players walk out onto a field, they walk into the bright glare of the
    /// stadium lights alongside a drowning out of the cheers before the glare disappears and the
    /// crowd cheers get louder. a screen billboard flikers through all the characters and then
    /// lands on the person playing taya to display whos defending.. then the arena gets built").
    ///
    /// On this map it REPLACES the arrival every map plays (`MatchArrivalPresentation`: a wide
    /// shot, then a portrait of each seat with a caption naming the taya). Its four portraits
    /// are folded into the screen's shuffle here, which says the same thing (who is who, who
    /// defends) as part of the stadium. The arrival still owns the hold, the input, the curtain
    /// and the camera's return, and calls this in place of its own shots (`Begin`, `Roll`,
    /// `Playing`, `End`), so the ready gate and the 3 · 2 · 1 after it are untouched.
    ///
    /// FIVE BEATS ON ONE CLOCK (`Timeline`; seconds, the full opening / the short one):
    ///   1. THE TUNNEL   0.0 / none   Low at the four players' backs in the players' tunnel
    ///      (south, 4.2 m wide, its mouth 83.9 m from the can). The picture is held dim, the
    ///      stadium is the bright opening ahead, the crowd is heard through concrete.
    ///   2. THE WALK OUT 1.8 / none   They walk; the camera follows and rises. From 3.6 the north
    ///      canopy's floodlights flare in the lens and a white veil comes up to its peak at 5.1.
    ///   3. THE BOWL     5.35 / 0.0   Cut under the white: over the players' heads, craning up
    ///      and out over the field. The muffle lifts, the stands erupt, fireworks, the welcome.
    ///   4. THE TAYA     8.2 / 2.0    The north-east screen (and every screen): the four seats'
    ///      cards shuffle, slowing, and land on round 1's taya at 10.4 / 3.7 with the stamp.
    ///      10.95 / 4.2: a spot on that player's model on the turf.
    ///   5. THE BUILD    11.6 / 4.8   The stage rises out of the empty shaft and locks, piece by
    ///      piece (`ArenaStage.TryOpening`, `ArenaShow`: the break's own show from nothing), the
    ///      drones carry the players to their marks, the can appears. 17.0 / 10.2: the camera
    ///      goes home to this player's own view; 17.8 / 11.0: the ready gate takes over.
    ///
    /// ⚠️ THE CLOCK. Every moment is a time since `Began`, a stamp on the clock every peer shares
    /// (`SharedUltimatePhase.Now`), and everything shown is a pure function of that age and of
    /// what every peer already holds (the picks, the names, `MatchRules.DefenderSlotFor(1)`, the
    /// match's layout). WHAT IS NOT SHARED IS THE STAMP ITSELF: the game has no host-authored
    /// time for a match's start before `BeginCountdown`, so each peer stamps `Began` when its
    /// own loading curtain lifts, exactly as the arrival it replaces starts on each peer. Peers
    /// see the same film, offset by the difference in their loading. Making them frame-equal
    /// needs one host stamp on the wire, which this deliberately does not add (see `Roll`).
    /// A peer still in it when the host's countdown arrives is cut to the end state by the
    /// ready gate, as before (`ReadyGate.StartLocalCountdown`, `End`).
    ///
    /// ⚠️ PRESENTATION ONLY, AND EVERYTHING IS PUT BACK. Nothing here moves a body, a collider
    /// or anything replicated: the players' real bodies stand on their marks on the (unseen)
    /// stage throughout; only each `CharacterVisual.ModelRoot` and four limb bones are posed,
    /// after the animator, and returned. `End` is safe to call at any moment and any number
    /// of times: models, bones, the can's renderers, the camera's grade, the map's bloom, the
    /// screens, the overlay, the sounds and the listener's low pass all go back or go away.
    ///
    /// THE SOUND IS MUFFLED FROM OUTSIDE. `ArenaCrowdAudio` is not edited: an
    /// `AudioLowPassFilter` on the listener's own object closes over everything for the tunnel
    /// and the glare and is destroyed at the reveal. This opening's own sounds
    /// (tools/synth_arena_intro_sfx.py) play on its own sources, which bypass that filter.
    ///
    /// THE SHORT ONE: reduced UI motion, or any opening after the first full one this session
    /// (a rematch, the next match): beats 3 to 5 only. With cinematic camera motion switched
    /// off there is no opening at all, and the arrival plays as on every other map.
    /// </summary>
    [DefaultExecutionOrder(880)]
    public sealed class ArenaIntro : MonoBehaviour
    {
        public enum Beat { None, Tunnel, Walk, Glare, Reveal, Taya, Spot, Build, Handoff, Done }

        /// <summary>When each beat starts, seconds. -1 for a beat the short opening leaves out.</summary>
        public struct Times
        {
            public bool Full;
            /// <summary>The full opening begins on a black screen for this long, with only the heartbeat.</summary>
            public float Black;
            public float Walk, Glare, Peak, Reveal, Taya, Land, Spot, Build, Handoff, End;
        }

        /// <summary>The build is the break's show on shorter beats (`ArenaStage.TryOpening`).</summary>
        public const float BuildSeconds = 5.4f;

        // ⚠️ THE TAYA'S CARD IS HELD (owner, 2026-10-05: "the showing of the taya is too fast. it shows whos taya
        // and immediately cuts to next scene. also i need the roll to flicker more. doesnt matter if it loops
        // through the players multiple times"). The roll was 2.2 s and the landed card stayed 0.55 s; the roll is
        // 3.3 s of faster cards (`BuildTicks`) and the landed card stays about 2 s before the cut.
        // ⚠️ THE GLARE IS TIMED TO THE WALK, NOT THE WALK TO THE GLARE (owner, 2026-10-05: "the glare happens
        // after the characters already exit the tunnel"). The tunnel is 10 m deep and the camera needs four of
        // them, so the line has 5 m to the mouth: at the walk's pace (`Line`, 1.28 m/s) that is 3.9 s. The
        // white's peak and the cut to the bowl sit there, and the glare builds through the whole walk to it.
        // ⚠️ IT OPENS ON BLACK (owner, 2026-10-05: "the opening scene needs to start on a full black screen, sounds
        // of heartbeats for like 3 seconds and then fade in to the characters walking"). `Dark` seconds of black
        // with the heart alone, then the picture comes up on a line already walking. Every later time is counted
        // from the end of the black; the short opening has none.
        public const float Dark = 3.0f;

        public static Times TimesFor(bool full) => full
            ? new Times { Full = true, Black = Dark, Walk = Dark + 0.6f, Glare = Dark + 1.6f, Peak = Dark + 3.7f, Reveal = Dark + 3.95f, Taya = Dark + 6.8f, Land = Dark + 10.1f, Spot = Dark + 12.2f, Build = Dark + 14.85f, Handoff = Dark + 14.85f + BuildSeconds, End = Dark + 14.85f + BuildSeconds + 0.8f }
            : new Times { Full = false, Walk = -1.0f, Glare = -1.0f, Peak = -1.0f, Reveal = 0.0f, Taya = 2.0f, Land = 4.7f, Spot = 6.6f, Build = 9.0f, Handoff = 9.0f + BuildSeconds, End = 9.0f + BuildSeconds + 0.8f };

        public static ArenaIntro Instance { get; private set; }

        /// <summary>True from `Begin` until the film has run out or been ended.</summary>
        public bool Playing { get; private set; }
        public Times Timeline { get; private set; }
        public float Age { get; private set; }
        public Beat Now { get; private set; }
        /// <summary>The stamp on the shared clock this peer's film runs from; NaN until `Roll`.</summary>
        public double Began { get; private set; } = double.NaN;
        /// <summary>The seat whose card is on the screens, and the seat the stamp landed on (-1 before it lands).</summary>
        public int ScreenSeat => _screens.Shown;
        public int LandedSeat { get; private set; } = -1;
        /// <summary>The name the screens show for a seat.</summary>
        public string ScreenName(int seat) => _screens.NameOf(seat);
        /// <summary>True while this opening's low pass is on the listener.</summary>
        public static bool Muffled => _filter != null;
        /// <summary>The last opening this session: how long it was to be, and whether it ran out or was cut.</summary>
        public static float LastSeconds { get; private set; }
        public static bool LastCompleted { get; private set; }

        private static bool _fullSeen;
        private static AudioLowPassFilter _filter;

        /// <summary>The next opening is the full one again. For a probe.</summary>
        public static void ResetSession() => _fullSeen = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _fullSeen = false; _filter = null; Instance = null; LastSeconds = 0.0f; LastCompleted = false; }

        // The stadium's own numbers (docs/ARENA_ART_BRIEF.md; tools/author_arena_bowl.py for the tunnel).
        private const float MouthZ = -83.9f, TunnelHalf = 2.1f, TunnelTop = 2.6f, Ground = 0.04f;
        private const float LineStart = -88.6f, LineEnd = -79.5f, Stride = 1.2f;
        private const float FloodRadius = 151.3f, FloodHeight = 66.75f, SpotRadius = 160.0f, SpotHeight = 62.0f;
        private static readonly float[] FloodBearing = { -27.0f, -17.0f, -7.125f, 7.125f, 17.0f, 27.0f };
        private static readonly Color FloodColour = new Color(0.86f, 0.93f, 1.0f);
        /// <summary>Each seat's place across the tunnel and how far ahead of the line. TWO FILES OF TWO (owner,
        /// 2026-10-05, of four abreast: "the hall is too crowded. split the characters into 2 lines like the
        /// bluelock reference"): the taya's seat leads the left file, the camera follows the rear pair.</summary>
        private static readonly float[] LaneX = { -0.85f, 0.85f, -0.85f, 0.85f }, LaneZ = { 1.9f, 1.65f, 0.1f, -0.12f };
        private const int Seats = Core.Balance.PlayerCount, MaxTicks = 72;
        private const string WelcomeLine = "arena_welcome";

        // ---- what it was given
        private Camera _camera;
        private ColourGrade _grade;
        private Vector3 _homePosition;
        private Quaternion _homeRotation;
        private float _homeFov;
        private bool _still;
        private int _taya;
        private Vector3 _centre;
        private float _last;
        private bool _prepared;

        // ---- the players' models
        private readonly CharacterMotor[] _body = new CharacterMotor[Seats];
        private readonly Transform[] _root = new Transform[Seats];
        private readonly Vector3[] _rest = new Vector3[Seats], _stand = new Vector3[Seats];
        private readonly Quaternion[] _restTurn = new Quaternion[Seats];
        private readonly Transform[] _limb = new Transform[Seats * 4];
        private readonly Quaternion[] _limbRest = new Quaternion[Seats * 4];
        private readonly CharacterAnimator[] _anim = new CharacterAnimator[Seats];
        private static readonly string[] LimbNames = { "leg-left", "leg-right", "arm-left", "arm-right" };
        private bool _posing;

        // ---- the can
        private Renderer[] _canRenderers;
        private bool[] _canWas;
        private bool _canHidden;

        // ---- the picture
        private readonly ArenaIntroScreen _screens = new ArenaIntroScreen();
        private Canvas _overlay;
        private Image _ink, _veil;
        private WorldLookProfile.MapLook _look;
        private float _lookBloom;
        private bool _bloomHeld;
        private float _punch, _punchAt = -99.0f;
        private Vector3 _eye, _aim;
        private float _fov = 50.0f;

        // ---- the shuffle
        private readonly float[] _tickAt = new float[MaxTicks];
        private int _ticks, _tick = -2;

        // ---- the sound
        private AudioSource _loop, _ringer, _shots;
        private AudioClip _rumble, _heart, _whoosh, _ring, _tickClip, _stamp;
        private int _beats;
        private static readonly float[] HeartAt = { 0.35f, 1.3f, 2.25f, Dark + 0.2f, Dark + 1.0f, Dark + 1.7f, Dark + 2.25f, Dark + 2.7f, Dark + 3.05f, Dark + 3.35f, Dark + 3.6f };

        /// <summary>This map's opening, made if the map is loaded and has none. Null on every other map.</summary>
        public static ArenaIntro Ensure()
        {
            if (Instance != null) return Instance;
            if (ArenaStage.Instance == null) return null;

            var fx = ArenaFx.Ensure();
            return fx != null ? fx.gameObject.AddComponent<ArenaIntro>() : null;
        }

        private void Awake() => Instance = this;
        private void OnEnable() => Instance = this;

        private void OnDisable()
        {
            End();
            if (Instance == this) Instance = null;
        }

        private void OnDestroy() => End();

        // ------------------------------------------------------------------ what the arrival calls

        /// <summary>
        /// Stand the film on its first frame, under the arrival's black curtain: the models in
        /// the tunnel, the stage out of sight, the camera behind them. The clock does not run
        /// until `Roll`. False when there is no opening to play here, and nothing was touched:
        /// the arrival plays its own shots, as on every other map.
        /// </summary>
        public bool Begin(Camera camera, CharacterMotor[] players, Vector3 homePosition, Quaternion homeRotation, float homeFov, bool reducedMotion)
        {
            End();
            var stage = ArenaStage.Instance;
            var match = GameServices.Match;
            // A match already under way (a join in its middle) has no opening to walk out to.
            if (stage == null || camera == null || players == null || stage.Applied < 0 || ArenaFx.Ensure() == null
                || (match != null && (match.MatchInProgress || match.RoundNumber > 0))
                || !Settings.SettingsStore.Current.CinematicCameraMotion) return false;

            _camera = camera;
            _grade = camera.GetComponent<ColourGrade>();
            _homePosition = homePosition; _homeRotation = homeRotation; _homeFov = homeFov;
            _still = reducedMotion;
            _centre = stage.transform.position;
            _taya = Core.MatchRules.DefenderSlotFor(1);
            Timeline = TimesFor(!reducedMotion && !_fullSeen);
            LastSeconds = Timeline.End; LastCompleted = false;
            LandedSeat = -1; _tick = -2; _beats = 0; _punch = 0.0f; _last = -1.0f;
            // The match's id now, so the stage this opening builds is the one round 1 is played on.
            GameServices.Match?.PreparePresentationMatch();
            Age = 0.0f; Began = double.NaN; Now = Timeline.Full ? Beat.Tunnel : Beat.Reveal;

            TakeModels(players);
            HideCan();
            HideUi();
            _screens.Build(transform, _centre, players);
            BuildOverlay();
            BuildSound();
            PlanShuffle();

            var world = WorldLookPresentation.Current;
            _look = world != null && WorldLookPresentation.HandlesCamera(camera) ? world.Look : null;
            _lookBloom = _look != null ? _look.Bloom : 0.0f;
            _bloomHeld = false;

            _prepared = true; Playing = true;
            ArenaStage.HoldOpening(-Timeline.Build, BuildSeconds);
            Sample();
            return true;
        }

        /// <summary>
        /// The curtain is up: start the clock. ⚠️ THIS IS THE ONE PLACE THE STAMP IS TAKEN, and it
        /// is this peer's own moment on the shared clock. A host-authored start (one double in a
        /// message every peer receives before its curtain lifts) would be read here instead, and
        /// nothing else in this file would change: a peer whose curtain lifts late would land in
        /// the beat the others are in, since every beat is drawn from `Age` alone.
        /// </summary>
        public void Roll()
        {
            if (!_prepared || !double.IsNaN(Began)) return;
            Began = SharedUltimatePhase.Now;
            if (Timeline.Full && _loop != null && _rumble != null) { _loop.clip = _rumble; _loop.loop = true; _loop.volume = 0.0f; _loop.Play(); }
            if (Timeline.Full) Muffle();
        }

        /// <summary>
        /// Stop, at any moment, and put everything back: the end state is the stage standing on
        /// round 1's layout, every model on its body, the can drawn, the sound open. Called by
        /// the arrival when the film runs out, when the host's countdown cuts it short, when the
        /// scene goes, and by this component's own `OnDisable`.
        /// </summary>
        public static void Stop() { if (Instance != null) Instance.End(); else Unmuffle(); }

        public void End()
        {
            Unmuffle();
            if (!_prepared) return;
            _prepared = false; Playing = false;
            Now = Beat.Done;

            ReleaseModels();
            ShowCan();
            ShowUi();
            _screens.Destroy();
            if (_overlay != null) { Destroy(_overlay.gameObject); _overlay = null; _ink = null; _veil = null; }
            if (_grade != null) _grade.SetEventGrade(1.0f, 1.0f);
            if (_bloomHeld && _look != null) _look.Bloom = _lookBloom;
            _bloomHeld = false; _look = null;
            if (_loop != null) _loop.Stop();
            if (_ringer != null) _ringer.Stop();
            _camera = null; _grade = null;
        }

        // ⚠️ THE FILM PLAYS WITHOUT THE GAME'S UI (owner, 2026-10-05: "can you also hide the ui at the start
        // cinematic"): the HUD, the prompts and the touch controls were drawn over it. Every screen canvas
        // under the arrival's own curtain (order 150) is switched off for the film and put back by `End`,
        // and the match readout, which sets its own canvas every frame, asks `HidesUi`.
        private readonly System.Collections.Generic.List<Canvas> _hiddenUi = new System.Collections.Generic.List<Canvas>();

        /// <summary>True while an opening is on the screen: the game's UI stays down.</summary>
        public static bool HidesUi => Instance != null && Instance._prepared;

        private void HideUi()
        {
            _hiddenUi.Clear();
            foreach (var canvas in FindObjectsByType<Canvas>())
            {
                if (canvas == null || !canvas.enabled || !canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace || canvas.sortingOrder >= 150) continue;
                canvas.enabled = false;
                _hiddenUi.Add(canvas);
            }
        }

        private void ShowUi()
        {
            foreach (var canvas in _hiddenUi) if (canvas != null) canvas.enabled = true;
            _hiddenUi.Clear();
        }

        /// <summary>A shake for the opening's picture, from `ArenaShow` (a lock, the reveal). Nothing outside an opening.</summary>
        public static void Punch(float strength)
        {
            var self = Instance;
            if (self == null || !self._prepared || self._still) return;

            float left = self._punch * Mathf.Exp(-(Time.unscaledTime - self._punchAt) * PunchDecay);
            if (strength < left) return;
            self._punch = strength; self._punchAt = Time.unscaledTime;
        }

        private const float PunchDecay = 5.5f, ShakeDegrees = 0.5f;

        /// <summary>Where the opening left a seat's model standing on the turf, and the bearing
        /// it faces: `ArenaShow`'s drones take it from there. False outside an opening.</summary>
        public static bool TryStand(int slot, out Vector3 at, out float yaw)
        {
            at = default; yaw = 0.0f;
            var self = Instance;
            if (self == null || !self._prepared || slot < 0 || slot >= Seats || self._root[slot] == null) return false;
            at = self._stand[slot];
            return true;
        }

        // ------------------------------------------------------------------ the frame

        private void Update()
        {
            if (!_prepared) return;

            // A probe or a film that takes the stage for itself (`ArenaStage.HoldLayout`) gets it whole, at once.
            var stage = ArenaStage.Instance;
            if (stage == null || stage.HoldLayout >= 0) { End(); return; }

            Age = double.IsNaN(Began) ? 0.0f : Mathf.Max(0.0f, (float)(SharedUltimatePhase.Now - Began));
            if (Age >= Timeline.End)
            {
                LastCompleted = true;
                End();
                return;
            }

            // The stage reads this in its own LateUpdate, before this component's.
            ArenaStage.HoldOpening(Age - Timeline.Build, BuildSeconds);
        }

        private void LateUpdate()
        {
            if (_prepared) Sample();
        }

        private bool Crossed(float at) => at >= 0.0f && _last < at && Age >= at;

        private void Sample()
        {
            var stage = ArenaStage.Instance;
            var fx = ArenaFx.Instance;
            if (stage == null || fx == null || _camera == null) { End(); return; }

            var t = Timeline;
            float age = Age, build = age - t.Build;
            Now = !t.Full ? (age < t.Taya ? Beat.Reveal : age < t.Spot ? Beat.Taya : age < t.Build ? Beat.Spot : age < t.Handoff ? Beat.Build : Beat.Handoff)
                : age < t.Walk ? Beat.Tunnel : age < t.Glare ? Beat.Walk : age < t.Reveal ? Beat.Glare : age < t.Taya ? Beat.Reveal
                : age < t.Spot ? Beat.Taya : age < t.Build ? Beat.Spot : age < t.Handoff ? Beat.Build : Beat.Handoff;

            var settings = Settings.SettingsStore.Current;
            float flash = settings.EffectiveFlashIntensity;

            // How far into the glare, 0 to 1: up to the white's peak, held to the cut, then away.
            float glare = 0.0f;
            if (t.Full)
            {
                float up = Smooth((age - t.Glare) / (t.Peak - t.Glare));
                glare = age < t.Reveal ? up * up : Mathf.Lerp(0.3f, 0.0f, Smooth((age - t.Reveal - 0.5f) / 1.6f)) + 0.7f * (1.0f - Smooth((age - t.Reveal) / 0.9f));
            }
            else glare = 0.5f * (1.0f - Smooth(age / 1.2f));

            Models(age, t, build);
            Shot(stage, age, t, build);
            Light(stage, fx, age, t, build, glare, flash);
            Screens(age, t, build, settings.ReducedEffects, flash);
            Sound(age, t, glare);
            Moments(stage, fx, age, t, build);

            _last = age;
        }

        // ------------------------------------------------------------------ the players' models

        private void TakeModels(CharacterMotor[] players)
        {
            for (int s = 0; s < Seats; s++)
            {
                _body[s] = null; _root[s] = null;
                for (int k = 0; k < 4; k++) _limb[s * 4 + k] = null;

                var body = s < players.Length ? players[s] : null;
                if (body == null || !body.gameObject.activeInHierarchy) continue;
                var visual = body.GetComponent<CharacterVisual>();
                var root = visual != null ? visual.ModelRoot : null;
                // A model hung on the seat itself cannot be moved without moving the body: left on its mark.
                if (root == null || root == body.transform) continue;

                _body[s] = body; _root[s] = root;
                _rest[s] = root.localPosition; _restTurn[s] = root.localRotation;
                // ⚠️ THE BODY IS PUT IN ITS IDLE, AND KEPT BREATHING (owner, 2026-10-06, of the cut to the taya:
                // "the idle animations arent playing on any of the characters so theyre just A-posing"). The
                // arrival this film replaced asked each animator for its idle (`SetArrivalPose`); the film did
                // not, so a body whose graph had not yet run stood in its bind pose, and the game's clock is
                // held for the whole film, so nothing would have moved it on. The idle is asked for here and
                // run on each frame (`Models`).
                _anim[s] = body.GetComponent<CharacterAnimator>();
                if (_anim[s] != null) _anim[s].SetArrivalPose(s, 0.001f);
                if (visual.Model == null) continue;
                foreach (var bone in visual.Model.GetComponentsInChildren<Transform>(true))
                    for (int k = 0; k < 4; k++)
                        if (_limb[s * 4 + k] == null && bone.name == LimbNames[k]) { _limb[s * 4 + k] = bone; _limbRest[s * 4 + k] = bone.localRotation; }
            }

            _posing = true;
        }

        /// <summary>How far along the walk the line is, metres from the tunnel's back, and how much it is striding (0 standing).</summary>
        private void Line(float age, in Times t, out float z, out float stride)
        {
            if (!t.Full) { z = LineEnd; stride = 0.0f; return; }

            // ⚠️ THEY ARE WALKING FROM THE FIRST FRAME (owner, 2026-10-05: "i dont want to see the players
            // standing still at all i nthe opening scene, they should immediately be walking"). They stood
            // for 1.8 s before the first step. The same ground over the whole time, already in stride as
            // the picture comes up from black, easing only into the stop out on the turf.
            // One steady pace the whole way (9.1 m in 7.1 s), easing only into the stop over the last tenth.
            float p = Mathf.Clamp01((age - t.Black) / (t.Taya + 0.3f - t.Black));
            float eased = p < 0.9f ? p * (0.95f / 0.9f) : 0.95f + 0.05f * (1.0f - (1.0f - (p - 0.9f) / 0.1f) * (1.0f - (p - 0.9f) / 0.1f));
            z = Mathf.Lerp(LineStart, LineEnd, eased);
            stride = Mathf.Clamp01((1.0f - p) * 14.0f);
        }

        private void Models(float age, in Times t, float build)
        {
            if (!_posing) return;

            // The drones take over: let go this frame, before `ArenaShow` (order 900) reads each root's rest.
            if (build >= ArenaStage.OpeningUndock - ArenaShow.LiftLead) { ReleaseModels(); return; }

            Line(age, t, out float line, out float stride);
            float walked = line - LineStart;
            for (int s = 0; s < Seats; s++)
            {
                var body = _body[s];
                var root = _root[s];
                if (body == null || root == null) continue;

                // Two files in the tunnel, spreading once they are out on the turf.
                float z = line + LaneZ[s];
                float spread = Mathf.Lerp(1.0f, 1.9f, Smooth((z - MouthZ - 1.0f) / 6.0f));
                float phase = (walked / Stride + s * 0.31f) * Mathf.PI * 2.0f;
                float bob = Mathf.Abs(Mathf.Sin(phase)) * 0.035f * stride;
                Vector3 at = _centre + new Vector3(LaneX[s] * spread, Ground + bob, z);
                _stand[s] = _centre + new Vector3(LaneX[s] * spread, Ground, z);

                // ONLY the drawn model moves: its root's rest pose plus the offset, turned to face the field.
                var parent = root.parent;
                Quaternion parentTurn = parent != null ? parent.rotation : Quaternion.identity;
                Vector3 offset = at - body.transform.position;
                root.localPosition = _rest[s] + (parent != null ? parent.InverseTransformVector(offset) : offset);
                root.localRotation = Quaternion.Inverse(parentTurn) * Quaternion.Euler(0.0f, -body.transform.eulerAngles.y, 0.0f) * parentTurn * _restTurn[s];

                // The walk: legs and arms swung about the model's own side axis, opposite pairs
                // together. Written whole each frame from the rest pose, after the animator
                // (which holds its idle through the arrival), so nothing accumulates.
                // The idle, a frame on; the limbs' rest is what it has just written, so the walk is laid over it.
                if (_anim[s] != null)
                {
                    // ⚠️ THE IDLE STARTS AT THE CUT TO THE PLAYERS, NOT AT THE FILM'S START (owner, 2026-10-06: "the idle
                    // animations play too early so it ends when the camera cuts to the player"). Until then the pose is
                    // held at its first frame under the walk; from just before the spotlight's shot it runs.
                    _anim[s].AdvanceHeld(age >= t.Spot - 0.15f ? Time.unscaledDeltaTime : 0.0f);
                    for (int k = 0; k < 4; k++) if (_limb[s * 4 + k] != null) _limbRest[s * 4 + k] = _limb[s * 4 + k].localRotation;
                }
                if (stride <= 0.001f) continue;
                Vector3 side = root.right;
                float leg = Mathf.Sin(phase) * 27.0f * stride, arm = Mathf.Sin(phase) * 19.0f * stride;
                Swing(s * 4, side, leg); Swing(s * 4 + 1, side, -leg);
                Swing(s * 4 + 2, side, -arm); Swing(s * 4 + 3, side, arm);
            }
        }

        private void Swing(int index, Vector3 axis, float degrees)
        {
            var bone = _limb[index];
            if (bone == null) return;
            var parent = bone.parent;
            Quaternion rest = (parent != null ? parent.rotation : Quaternion.identity) * _limbRest[index];
            bone.rotation = Quaternion.AngleAxis(degrees, axis) * rest;
        }

        /// <summary>Every model back exactly where its body is, every limb as the animator left it.</summary>
        private void ReleaseModels()
        {
            if (!_posing) return;
            _posing = false;
            for (int s = 0; s < Seats; s++)
            {
                if (_root[s] != null) { _root[s].localPosition = _rest[s]; _root[s].localRotation = _restTurn[s]; }
                for (int k = 0; k < 4; k++)
                    if (_limb[s * 4 + k] != null) _limb[s * 4 + k].localRotation = _limbRest[s * 4 + k];
                if (_anim[s] != null) { _anim[s].SetArrivalPose(s, 0.0f); _anim[s] = null; }
            }
        }

        // ------------------------------------------------------------------ the can

        private void HideCan()
        {
            _canHidden = false;
            var can = GameServices.Round != null && GameServices.Round.Lata != null ? GameServices.Round.Lata : FindFirstObjectByType<Lata>();
            if (can == null) return;

            _canRenderers = can.GetComponentsInChildren<Renderer>(false);
            _canWas = new bool[_canRenderers.Length];
            for (int i = 0; i < _canRenderers.Length; i++)
            {
                _canWas[i] = _canRenderers[i].enabled;
                _canRenderers[i].enabled = false;
            }
            _canHidden = true;
        }

        private void ShowCan()
        {
            if (!_canHidden) return;
            _canHidden = false;
            for (int i = 0; i < _canRenderers.Length; i++)
                if (_canRenderers[i] != null) _canRenderers[i].enabled = _canWas[i];
        }

        // ------------------------------------------------------------------ the camera

        private void Shot(ArenaStage stage, float age, in Times t, float build)
        {
            float radius = stage.Radius, can = stage.CanHeight;
            float scale = Mathf.Max(1.0f, radius / 14.0f);
            Line(age, t, out float line, out _);

            if (t.Full && age < t.Reveal)
            {
                // 1 and 2. Low at their backs, following them out and rising toward the mouth.
                float p = _still ? 0.0f : Smooth((age - t.Black) / (t.Reveal - t.Black));
                float bob = _still ? 0.0f : Mathf.Sin((line - LineStart) / Stride * Mathf.PI * 4.0f) * 0.018f;
                Pose(_centre + new Vector3(0.0f, Mathf.Lerp(0.95f, 1.75f, p) + bob, line - Mathf.Lerp(4.0f, 3.1f, p)),   // dead centre between the two files
                     _centre + new Vector3(0.0f, Mathf.Lerp(1.9f, 4.4f, p), line + 40.0f), 50.0f);
            }
            else if (age < t.Taya)
            {
                // 3. From the tunnel's lintel, over their heads, craning up and out over the field.
                float p = _still ? 0.6f : Smooth((age - t.Reveal) / (t.Taya - t.Reveal));
                Pose(_centre + new Vector3(0.0f, Mathf.Lerp(2.95f, 11.0f, p), Mathf.Lerp(MouthZ + 0.6f, -67.0f, p)),
                     _centre + new Vector3(0.0f, Mathf.Lerp(15.0f, 9.0f, p), Mathf.Lerp(44.0f, 2.0f, p)), Mathf.Lerp(64.0f, 58.0f, p));
            }
            else if (age < t.Spot)
            {
                // 4. The north-east screen, from the air over the turf's edge, pushing in.
                float p = _still ? 0.5f : Smooth((age - t.Taya) / (t.Spot - t.Taya));
                Vector3 outward = ArenaStageMesh.Direction(45.0f);
                Pose(_centre + outward * Mathf.Lerp(64.0f, 73.0f, p) + Vector3.up * Mathf.Lerp(16.0f, 18.5f, p),
                     ArenaIntroScreen.CornerCentre(_centre, 0), 24.0f);
            }
            else if (age < t.Build)
            {
                // The spot on the taya: low on the turf in front of the line, looking back at them.
                // ⚠️ AND THEN THE OTHER THREE (owner, 2026-10-06: "need you to extend when it cuts back to the player, so
                // it also shows the other players"). It was 0.65 s on the taya alone. Now about 2.6 s: it holds on
                // the taya for the first third, then draws back and across until all four stand in the frame.
                float p = _still ? 0.5f : (age - t.Spot) / (t.Build - t.Spot);
                Vector3 who = _root[_taya] != null ? _stand[_taya] : _centre + new Vector3(0.0f, Ground, LineEnd);
                Vector3 all = Vector3.zero; int counted = 0;
                for (int s = 0; s < Seats; s++) if (_root[s] != null) { all += _stand[s]; counted++; }
                all = counted > 0 ? all / counted : who;
                float wide = _still ? 1.0f : Smooth((p - 0.3f) / 0.6f);
                Vector3 eye = Vector3.Lerp(who + new Vector3(2.3f - 0.3f * p, 1.0f, 5.2f - 0.5f * p), all + new Vector3(0.9f, 1.7f, 8.6f), wide);
                Vector3 look = Vector3.Lerp(who + Vector3.up * 1.25f, all + Vector3.up * 1.1f, wide);
                Pose(eye, look, Mathf.Lerp(38.0f, 44.0f, wide));
            }
            else if (build < ArenaStage.OpeningUndock)
            {
                // 5a. The blueprint: from behind the line, rising over it as the hologram sweeps the shaft.
                float p = _still ? 1.0f : Smooth(build / ArenaStage.OpeningUndock);
                Pose(_centre + new Vector3(Mathf.Lerp(3.2f, 2.0f, p), Mathf.Lerp(2.9f, 9.5f, p), Mathf.Lerp(-80.6f, -70.0f, p)),
                     _centre + Vector3.up * (can + 0.5f), 46.0f);
            }
            else if (build < BuildSeconds - ArenaStage.OpeningSettle + ArenaStage.RevealLag)
            {
                // 5b. The pieces rising and locking: an orbit on the players' side, over every deck.
                float p = _still ? 0.5f : Smooth((build - ArenaStage.OpeningUndock) / (BuildSeconds - ArenaStage.OpeningSettle - ArenaStage.OpeningUndock));
                Pose(Polar(Mathf.Lerp(197.0f, 229.0f, p), radius * Mathf.Lerp(1.8f, 1.5f, p), Mathf.Lerp(13.0f, 10.0f, p) + Mathf.Max(0.0f, can)),
                     _centre + Vector3.up * (can + 0.4f), 52.0f);
            }
            else
            {
                // 5c. The reveal: a rising wide shot from the south, and then home.
                float p = _still ? 0.5f : Smooth((age - (t.Handoff - ArenaStage.OpeningSettle + ArenaStage.RevealLag)) / (ArenaStage.OpeningSettle - ArenaStage.RevealLag));
                Pose(Polar(Mathf.Lerp(190.0f, 181.0f, p), 26.0f * scale * Mathf.Lerp(1.0f, 1.1f, p), 11.0f * scale * Mathf.Lerp(0.75f, 1.05f, p)),
                     _centre + Vector3.up * 2.0f, Mathf.Lerp(52.0f, 56.0f, p));
            }

            Quaternion turn = Quaternion.LookRotation(_aim - _eye, Vector3.up);
            Vector3 eye = _eye;
            float fov = _fov;

            // Its own shake (the hold stops `CameraRig`), a few tenths of a degree, by the player's setting.
            float level = Settings.SettingsStore.Current.EffectiveCameraShake;
            float ring = _punch * Mathf.Exp(-(Time.unscaledTime - _punchAt) * PunchDecay);
            if (!_still && level > 0.0f && ring > 0.01f && age < t.Handoff)
            {
                float clock = Time.unscaledTime, by = ShakeDegrees * ring * level;
                turn *= Quaternion.Euler(Mathf.Sin(clock * 61.3f) * by, Mathf.Sin(clock * 47.9f + 1.3f) * by, Mathf.Sin(clock * 53.1f + 0.5f) * by * 0.5f);
            }

            // One deliberate handoff to this player's own view.
            if (age >= t.Handoff)
            {
                float home = Smooth((age - t.Handoff) / (t.End - t.Handoff));
                if (_still) home = home >= 0.5f ? 1.0f : 0.0f;
                eye = Vector3.Lerp(eye, _homePosition, home);
                turn = Quaternion.Slerp(turn, _homeRotation, home);
                fov = Mathf.Lerp(fov, _homeFov, home);
            }

            _camera.transform.SetPositionAndRotation(eye, turn);
            _camera.fieldOfView = fov;
        }

        private void Pose(Vector3 eye, Vector3 aim, float fov)
        {
            _eye = eye; _fov = fov;
            _aim = (aim - eye).sqrMagnitude > 1e-4f ? aim : eye + Vector3.forward;
        }

        private Vector3 Polar(float bearing, float radius, float height) =>
            _centre + ArenaStageMesh.Direction(bearing) * radius + Vector3.up * height;

        // ------------------------------------------------------------------ the light

        private void Light(ArenaStage stage, ArenaFx fx, float age, in Times t, float build, float glare, float flash)
        {
            // The tunnel is dim and the eye opens as they walk out: the camera's own event grade.
            float dim = t.Full ? 1.0f - Smooth((age - (t.Glare - 0.8f)) / (t.Peak - t.Glare + 0.8f)) : 0.0f;
            if (_grade != null) _grade.SetEventGrade(Mathf.Lerp(1.0f, 0.72f, dim), Mathf.Lerp(1.0f, 0.88f, dim));   // was 0.5: dark hair went to solid black shapes in the tunnel (owner, 2026-10-05)

            // The map's own glow, up with the glare and back exactly where it was.
            if (_look != null && _lookBloom > 0.0f)
            {
                _bloomHeld = true;
                _look.Bloom = _lookBloom * (1.0f + 3.0f * glare * glare * Mathf.Max(0.25f, flash));
            }

            // The north canopy's banks in the lens: this map's own lamp, pointed at this camera.
            if ((glare > 0.01f || (t.Full && age < t.Reveal)) && ArenaGlare.Begin(_camera))
            {
                Vector3 eye = _camera.transform.position;
                for (int i = 0; i < FloodBearing.Length; i++)
                {
                    Vector3 lamp = _centre + ArenaStageMesh.Direction(FloodBearing[i]) * FloodRadius + Vector3.up * FloodHeight;
                    Vector3 to = eye - lamp;
                    float metres = to.magnitude;
                    if (metres < 1.0f) continue;
                    float seen = SeenFromTunnel(eye - _centre, lamp - _centre);
                    ArenaGlare.Lamp(fx, lamp, to / metres, FloodColour, Mathf.Lerp(0.45f, 1.0f, glare) * Mathf.Clamp01(glare * 4.0f), 4.0f, 12.0f,
                                    1.0f + 2.4f * glare, true, flash > 0.3f && seen > 0.5f, seen);
                }

                // ⚠️ THE GLARE THEY WALK INTO (owner, 2026-10-05: "the glare doesnt happen on the starting
                // cinematic"). The canopy's banks above are 15 degrees up from here and the tunnel's lintel
                // hides them until the camera is out, which is when the white has already taken the frame:
                // so nothing flared. This is the far side's light as the mouth frames it: one lamp low
                // across the field, square in the opening, growing from the first step of the walk.
                if (t.Full && age < t.Reveal)
                {
                    // ⚠️ IT GROWS AS THEY NEAR THE MOUTH, NOT BEFORE (owner: "big glare effect grows before the
                    // players even move forward"): nothing for the first half second, then with every step to the mouth.
                    float grow = Smooth((age - t.Black - 0.5f) / (t.Peak - t.Black - 0.5f));
                    grow = Mathf.Pow(grow, 1.3f);
                    Vector3 sun = _centre + new Vector3(0.0f, Mathf.Lerp(3.6f, 5.2f, grow), -44.0f);
                    Vector3 from = eye - sun;
                    // ⚠️ AND IT IS LIGHT IN THE TUNNEL, NOT ONLY A PICTURE ON THE GLASS ("it still looks
                    // unrealistic and like still and flat images not real lighting"). Three things a real
                    // light does that a picture cannot: the walkers' heads cross it and it DIMS AND FLARES
                    // as they do (`Clear`); it lies on the floor in a pool from the mouth; and it stands in
                    // the tunnel's air as shafts, which the walkers pass in front of and behind.
                    float clear = Clear(eye, sun);
                    if (grow > 0.02f && from.sqrMagnitude > 1.0f)
                        ArenaGlare.Lamp(fx, sun, from.normalized, FloodColour, Mathf.Lerp(0.4f, 1.0f, grow) * Mathf.Lerp(0.3f, 1.0f, clear), 6.0f, 20.0f,
                                        1.1f + 2.8f * grow, true, flash > 0.3f, 1.0f);
                    float spill = Smooth((age - t.Black - 0.4f) / 2.6f) * (0.35f + 0.65f * grow);
                    Vector3 mouth = _centre + new Vector3(0.0f, Ground + 0.03f, MouthZ);
                    fx.DrawFlat(ArenaFx.Cell.Dot, mouth + new Vector3(0.0f, 0.0f, -3.5f), 5.2f, 15.0f, 0.0f, FloodColour, 0.34f * spill);
                    fx.DrawFlat(ArenaFx.Cell.Dot, mouth + new Vector3(0.0f, 0.0f, -1.0f), 4.0f, 6.0f, 0.0f, ArenaFx.White, 0.30f * spill);
                    float drift = age * 0.35f;
                    for (int k = 0; k < 5; k++)
                    {
                        float across = (k - 2.0f) * 0.78f + 0.12f * Mathf.Sin(drift + k * 1.9f);
                        Vector3 top = _centre + new Vector3(across * 0.8f, TunnelTop - 0.05f, MouthZ + 0.6f);
                        Vector3 foot = _centre + new Vector3(across * 1.25f, Ground, MouthZ - 5.5f - 1.3f * k);
                        float beat = 0.75f + 0.25f * Mathf.Sin(age * (0.9f + 0.17f * k) + k * 2.3f);
                        fx.DrawBeam(top, foot, 0.35f, 1.5f, FloodColour, 0.085f * spill * beat);
                    }
                }

                // The bright air beyond the mouth: what the dark tunnel is looking out at.
                if (t.Full && age < t.Reveal)
                    fx.DrawBillboard(ArenaFx.Cell.Dot, _centre + new Vector3(0.0f, 5.0f, -58.0f), 54.0f, ArenaFx.White, 0.10f + 0.30f * glare);
            }

            // The white: one rise and one fall, never a flicker, by the Flash intensity setting.
            // Black for `t.Black`, then the picture comes up over a second.
            float white = 0.0f, ink = 1.0f - Smooth((age - t.Black) / (t.Full ? 1.0f : 0.5f));
            if (t.Full)
            {
                float up = Smooth((age - (t.Glare + 0.45f)) / (t.Peak - t.Glare - 0.45f));
                white = age < t.Reveal + 0.1f ? up : 1.0f - Smooth((age - t.Reveal - 0.1f) / 0.95f);
                white *= 0.94f * flash;
                // With the white turned down the cut under it would show: a short dip covers it instead.
                ink = Mathf.Max(ink, (1.0f - flash) * 0.9f * (1.0f - Mathf.Clamp01(Mathf.Abs(age - t.Reveal) / 0.16f)));
            }
            else white = 0.5f * flash * (1.0f - Smooth(age / 0.7f));

            // The other cuts take a breath of ink, as the arrival's do.
            ink = Mathf.Max(ink, 0.85f * (1.0f - Mathf.Clamp01(Mathf.Abs(age - t.Taya) / 0.1f)));
            ink = Mathf.Max(ink, 0.85f * (1.0f - Mathf.Clamp01(Mathf.Abs(age - t.Spot) / 0.08f)));
            ink = Mathf.Max(ink, 0.85f * (1.0f - Mathf.Clamp01(Mathf.Abs(age - t.Build) / 0.1f)));
            if (_still) ink = Mathf.Max(ink, 1.0f - Mathf.Clamp01(Mathf.Abs(age - (t.Handoff + t.End) * 0.5f) / 0.2f));
            if (_veil != null) _veil.color = new Color(1.0f, 1.0f, 1.0f, Mathf.Clamp01(white));
            if (_ink != null) _ink.color = new Color(0.01f, 0.012f, 0.03f, Mathf.Clamp01(ink));

            // The spot on the taya, from the south canopy, from the stamp until the drones come.
            if (age >= t.Land && build < ArenaStage.OpeningUndock && _root[_taya] != null)
            {
                float on = Mathf.Clamp01((age - t.Land) / 0.08f) * Mathf.Clamp01((ArenaStage.OpeningUndock - build) / 0.4f);
                Vector3 feet = _stand[_taya], chest = feet + Vector3.up * 1.2f;
                Vector3 lamp = _centre + ArenaStageMesh.Direction(194.0f) * SpotRadius + Vector3.up * SpotHeight;
                Vector3 along = chest - lamp;
                float reach = along.magnitude;
                fx.DrawBeam(lamp, chest, 1.1f, 3.4f, ArenaFx.White, 0.16f * on);
                fx.DrawBeam(lamp, chest, 0.4f, 1.3f, ArenaFx.Cyan, 0.10f * on);
                fx.DrawFlat(ArenaFx.Cell.Disc, feet + Vector3.up * 0.05f, 5.6f, 5.6f, 0.0f, FloodColour, 0.30f * on);
                fx.DrawFlat(ArenaFx.Cell.ThinRing, feet + Vector3.up * 0.06f, 6.4f, 6.4f, 0.0f, ArenaFx.Gold, 0.55f * on);
                if (reach > 1.0f && ArenaGlare.Begin(_camera))
                    ArenaGlare.Lamp(fx, lamp, along / reach, FloodColour, on, 2.5f, 9.0f, 1.0f, true, false, 1.0f, 0.22f);
            }

            // The can's mark lights half a second before the can is there.
            float reveal = BuildSeconds - ArenaStage.OpeningSettle + ArenaStage.RevealLag;
            if (build >= reveal - 0.6f && build < reveal)
            {
                float lit = (build - (reveal - 0.6f)) / 0.6f;
                Vector3 mark = _centre + Vector3.up * (stage.CanHeight + 0.07f);
                fx.DrawFlat(ArenaFx.Cell.Target, mark, 2.6f - 0.8f * lit, 2.6f - 0.8f * lit, age * 90.0f, ArenaFx.Gold, 0.9f * lit);
                fx.DrawBillboard(ArenaFx.Cell.Dot, mark + Vector3.up * 0.4f, 1.6f, ArenaFx.White, 0.5f * lit);
            }
        }

        /// <summary>How much of a lamp the tunnel's mouth lets through to an eye inside it (both
        /// in the stadium's frame): where the line to the lamp crosses the mouth, against its
        /// lintel and jambs. 1 from outside.</summary>
        /// <summary>How much of a lamp an eye in THIS opening's tunnel can see past the lintel and the jambs (world
        /// points); 1 with no opening playing or from outside. For `ArenaGlare`'s flares, which nothing in the world hides.</summary>
        public static float SeenInOpening(Vector3 eye, Vector3 lamp)
        {
            var self = Instance;
            if (self == null || !self._prepared) return 1.0f;
            return SeenFromTunnel(eye - self._centre, lamp - self._centre);
        }

        /// <summary>
        /// How clear the line from the eye to a light is of the four walkers, 0 to 1: each is a head and a
        /// body (two balls) where its model is drawn. Their colliders are on the stage, not here, so this is
        /// by distance from the line, not a ray. It is what makes the flare gutter as a head crosses it.
        /// </summary>
        private float Clear(Vector3 eye, Vector3 light)
        {
            Vector3 along = light - eye;
            float reach = along.magnitude;
            if (reach < 0.5f) return 1.0f;
            along /= reach;
            float clear = 1.0f;
            for (int s = 0; s < Seats; s++)
            {
                if (_root[s] == null) continue;
                for (int part = 0; part < 2; part++)
                {
                    Vector3 at = _stand[s] + Vector3.up * (part == 0 ? 1.4f : 0.75f);
                    float radius = part == 0 ? 0.55f : 0.42f;
                    float t = Vector3.Dot(at - eye, along);
                    if (t <= 0.2f || t >= reach) continue;
                    float away = (eye + along * t - at).magnitude;
                    clear *= Smooth((away - radius * 0.55f) / (radius * 0.9f));
                }
            }
            return clear;
        }

        private static float SeenFromTunnel(Vector3 eye, Vector3 lamp)
        {
            if (eye.z >= MouthZ || lamp.z <= eye.z) return 1.0f;

            float share = (MouthZ - eye.z) / (lamp.z - eye.z);
            float x = eye.x + (lamp.x - eye.x) * share, y = eye.y + (lamp.y - eye.y) * share;
            return Mathf.Clamp01((TunnelTop - y) / 0.3f) * Mathf.Clamp01((TunnelHalf - Mathf.Abs(x)) / 0.3f);
        }

        private void BuildOverlay()
        {
            var go = new GameObject("Arena opening overlay");
            go.transform.SetParent(transform, false);
            _overlay = go.AddComponent<Canvas>();
            _overlay.renderMode = RenderMode.ScreenSpaceOverlay;
            // Over the arrival's own curtain (150), under the loading screen (900).
            _overlay.sortingOrder = 151;
            _veil = Cover(go.transform, "Glare", new Color(1.0f, 1.0f, 1.0f, 0.0f));
            _ink = Cover(go.transform, "Ink", new Color(0.01f, 0.012f, 0.03f, 1.0f));
        }

        private static Image Cover(Transform parent, string name, Color colour)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = colour; image.raycastTarget = false;
            var rect = image.rectTransform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return image;
        }

        // ------------------------------------------------------------------ the screens

        /// <summary>
        /// When each card comes up, seconds from the shuffle's start: quick at first, each gap
        /// a hair longer than the last through the first half, then slowing hard, the final one ON the landing. ⚠️ THE RATE IS CAPPED:
        /// about fourteen cards a second at its fastest (only the portrait and the name change; the card's ground never does), three under reduced effects or reduced motion.
        /// </summary>
        private void PlanShuffle()
        {
            var settings = Settings.SettingsStore.Current;
            float span = Timeline.Land - Timeline.Taya - 0.25f;
            float gap = settings.ReducedEffects || settings.ReducedUiMotion ? 0.32f : 0.07f, at = 0.0f;
            _ticks = 0;
            while (_ticks < MaxTicks && at <= span)
            {
                _tickAt[_ticks++] = at;
                at += gap;
                gap = Mathf.Min(gap * (gap < 0.3f && at < span * 0.55f ? 1.02f : 1.22f), 0.48f);   // a long fast flicker, then it slows into the landing
            }

            // Stretched so the last card lands exactly on the stamp (a stretch only widens the gaps).
            float last = _tickAt[_ticks - 1];
            if (last > 0.01f) for (int i = 0; i < _ticks; i++) _tickAt[i] *= span / last;
        }

        /// <summary>The seat on card `tick`: the seats in turn, arranged so the last card is the taya's.</summary>
        private int SeatAt(int tick) => ((_taya - (_ticks - 1 - tick)) % Seats + Seats) % Seats;

        private void Screens(float age, in Times t, float build, bool reduced, float flash)
        {
            float reveal = BuildSeconds - ArenaStage.OpeningSettle + ArenaStage.RevealLag;
            float alpha = Mathf.Clamp01((age - (t.Taya - 0.4f)) / 0.25f) * Mathf.Clamp01((reveal - build) / 0.4f);
            _screens.SetVisible(alpha);
            if (alpha <= 0.004f) return;

            float local = age - t.Taya - 0.25f;
            int tick = -1;
            for (int i = 0; i < _ticks; i++) if (local >= _tickAt[i]) tick = i;
            bool landed = tick >= _ticks - 1;
            _screens.Show(SeatAt(Mathf.Max(0, tick)), landed);

            if (tick != _tick)
            {
                // A card came up: its tick, a step higher each time; the last is the stamp.
                if (tick > _tick && tick >= 0 && _tick > -2)
                {
                    if (landed) Play(_stamp, 0.95f, 1.0f);
                    else Play(_tickClip, 0.5f, 0.9f + 0.035f * tick);
                }
                _tick = tick;
            }

            float since = tick >= 0 ? local - _tickAt[tick] : 1.0f;
            float glitch = reduced || landed ? 0.0f : 1.0f - Mathf.Clamp01(since / 0.07f);
            float slam = landed ? Mathf.Clamp01((age - t.Land) / 0.2f) : 0.0f;
            float white = landed ? 0.6f * flash * (1.0f - Mathf.Clamp01((age - t.Land) / 0.4f)) : 0.0f;
            _screens.Animate(glitch, tick * 7.31f + 1.7f, slam, white);
        }

        // ------------------------------------------------------------------ the sound

        private void BuildSound()
        {
            if (_loop == null)
            {
                _loop = Source("Arena opening room");
                _ringer = Source("Arena opening ring");
                _shots = Source("Arena opening cues");
            }

            if (_rumble == null) _rumble = Clip("sfx_arena_intro_rumble");
            if (_heart == null) _heart = Clip("sfx_arena_intro_heart");
            if (_whoosh == null) _whoosh = Clip("sfx_arena_intro_whoosh");
            if (_ring == null) _ring = Clip("sfx_arena_intro_ring");
            if (_tickClip == null) _tickClip = Clip("sfx_arena_intro_tick");
            if (_stamp == null) _stamp = Clip("sfx_arena_intro_stamp");
        }

        private AudioSource Source(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0.0f;
            // Heard clean over the muffled stadium: these are not behind the concrete.
            source.bypassListenerEffects = true;
            source.bypassReverbZones = true;
            return source;
        }

        /// <summary>A clip of this opening's own, read now (under the curtain) so its first play does not hitch.</summary>
        private static AudioClip Clip(string name)
        {
            var clip = Resources.Load<AudioClip>("Sfx/" + name);
            if (clip != null && clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
            return clip;
        }

        private static float Level => GameServices.Audio != null ? GameServices.Audio.SfxVolume : 1.0f;

        private void Play(AudioClip clip, float volume, float pitch = 1.0f)
        {
            if (clip == null || _shots == null || double.IsNaN(Began)) return;
            _shots.pitch = pitch;
            _shots.PlayOneShot(clip, Mathf.Clamp01(volume * Level));
        }

        /// <summary>The concrete: a low pass on the listener's own object, over everything it
        /// hears. Only if that object has none already, so nobody else's filter is ever touched.</summary>
        private static void Muffle()
        {
            Unmuffle();
            AudioListener ears = null;
            foreach (var listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
                if (listener.isActiveAndEnabled) { ears = listener; break; }
            if (ears == null || ears.GetComponent<AudioLowPassFilter>() != null) return;

            _filter = ears.gameObject.AddComponent<AudioLowPassFilter>();
            _filter.cutoffFrequency = TunnelCutoff;
            _filter.lowpassResonanceQ = 1.0f;
        }

        private static void Unmuffle()
        {
            if (_filter == null) return;
            _filter.enabled = false;
            Destroy(_filter);
            _filter = null;
        }

        private const float TunnelCutoff = 520.0f, GlareCutoff = 230.0f, OpenCutoff = 21000.0f, OpenSeconds = 0.5f;

        private void Sound(float age, in Times t, float glare)
        {
            if (double.IsNaN(Began)) return;

            float level = Level;
            if (_loop != null && _loop.isPlaying)
            {
                // The room: up in the tunnel, gone under the glare.
                float room = t.Full ? Mathf.Clamp01(age / 0.5f) * (1.0f - Smooth((age - t.Glare) / (t.Peak - t.Glare))) : 0.0f;
                _loop.volume = 0.5f * room * level;
                if (age > t.Reveal) _loop.Stop();
            }

            if (_filter != null)
            {
                if (age < t.Reveal) _filter.cutoffFrequency = Mathf.Lerp(TunnelCutoff, GlareCutoff, Smooth((age - t.Glare) / (t.Peak - t.Glare)));
                else if (age < t.Reveal + OpenSeconds)
                {
                    // Opened by ear (a ratio, not a line): the roar arrives as the white falls.
                    float open = (age - t.Reveal) / OpenSeconds;
                    _filter.cutoffFrequency = GlareCutoff * Mathf.Pow(OpenCutoff / GlareCutoff, open * open);
                }
                else Unmuffle();
            }

            if (!t.Full) return;

            // A heart, quickening toward the white.
            while (_beats < HeartAt.Length && age >= HeartAt[_beats])
            {
                if (age - HeartAt[_beats] < 0.25f) Play(_heart, 0.55f + 0.04f * _beats, 1.0f);
                _beats++;
            }

            if (Crossed(t.Peak - 1.7f)) Play(_whoosh, 0.7f, 1.0f);
            if (Crossed(t.Peak - 0.35f) && _ringer != null && _ring != null)
            {
                _ringer.clip = _ring; _ringer.volume = 0.3f * level * Mathf.Max(0.35f, Settings.SettingsStore.Current.EffectiveFlashIntensity); _ringer.Play();
            }
        }

        // ------------------------------------------------------------------ the moments

        private void Moments(ArenaStage stage, ArenaFx fx, float age, in Times t, float build)
        {
            if (double.IsNaN(Began)) return;

            // The crowd swells behind the concrete as they come.
            if (t.Full)
            {
                if (Crossed(0.05f)) ArenaCrowd.Excite(0.35f, t.Reveal);
                if (Crossed(t.Walk)) ArenaCrowd.Excite(0.6f, t.Reveal - t.Walk);
                if (Crossed(t.Glare)) ArenaCrowd.Excite(0.85f, t.Reveal - t.Glare + 0.5f);
            }

            // The bowl: the stands erupt, fireworks, the welcome.
            if (Crossed(t.Reveal + 0.02f))
            {
                if (t.Full) _fullSeen = true;
                ArenaCrowd.Excite(1.0f, 6.0f);
                ArenaCrowd.Wave();
                ArenaCrowdAudio.Erupt(1.25f);
                ArenaFx.CueFlat("sfx_arena_pyro", 0.92f, 1.0f, 0.8f);
                if (!_still) { _punch = 1.2f; _punchAt = Time.unscaledTime; }
            }

            for (int k = 0; k < 5; k++)
                if (Crossed(t.Reveal + 0.3f + 0.34f * k))
                {
                    Vector3 over = _centre + ArenaStageMesh.Direction(-40.0f + 20.0f * k + fx.Rand(-8.0f, 8.0f)) * fx.Rand(98.0f, 124.0f) + Vector3.up * fx.Rand(46.0f, 60.0f);
                    fx.Firework(over, k % 2 == 0 ? ArenaFx.Gold : ArenaFx.Magenta, 1.6f);
                    if (k == 2) ArenaFx.CueFlat("sfx_arena_pyro", 1.0f, 1.1f, 0.7f);
                }

            if (Crossed(t.Reveal + 0.9f)) Welcome();

            // The stamp: the crowd answers, the picture is struck.
            if (Crossed(t.Land))
            {
                LandedSeat = _taya;
                ArenaCrowd.Excite(0.95f, 3.5f);
                ArenaCrowdAudio.Erupt(0.7f);
                if (!_still) { _punch = 0.8f; _punchAt = Time.unscaledTime; }
            }

            // The can appears as the stage's reveal strikes (`ArenaShow` flashes its spot on the same beat).
            if (build >= BuildSeconds - ArenaStage.OpeningSettle + ArenaStage.RevealLag) ShowCan();
        }

        /// <summary>
        /// The announcer's welcome, through the stadium (`ArenaCrowdAudio` takes the line for the
        /// PA as it does every line). ⚠️ `ArenaCrowdAudio` says the same line a few seconds into
        /// round 1; that file is not this one's to edit, so the second saying is closed off
        /// here: the line's cooldown is a minute for this one call (`VoiceDirector` stamps it
        /// when the line starts), and the table is put back as it was. With no recording nothing
        /// is said or stamped, and round 1's own call stands.
        /// </summary>
        private static void Welcome()
        {
            var voice = GameServices.Voice;
            if (voice == null) return;

            var table = Audio.VoiceDirector.CooldownMs;
            bool had = table.TryGetValue(WelcomeLine, out int was);
            table[WelcomeLine] = 60000;
            voice.Play(WelcomeLine);
            if (had) table[WelcomeLine] = was; else table.Remove(WelcomeLine);
        }

        private static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3.0f - 2.0f * t);
        }
    }
}
