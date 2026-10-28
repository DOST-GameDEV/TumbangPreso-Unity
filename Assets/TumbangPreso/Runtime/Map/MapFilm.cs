using UnityEngine;
using UnityEngine.Rendering;
using TumbangPreso.UI;

namespace TumbangPreso.Map
{
    /// <summary>
    /// A MAP'S OWN MATCH OPENING (owner, 2026-10-22, TOURNAMENT-FEEDBACK-1022 "Map intro films": each map with its
    /// own mood, built to `ArenaIntro`'s quality). One base, one director per map:
    ///   Tondo (Eskinita) `TondoFilm`, Ermita (IlalimNgTulay) `ErmitaFilm`, Sitangkai (LagoonCove) `SitangkaiFilm`,
    ///   BGC (Kanto) `BgcFilm`, Banaue (Payo) `BanaueFilm`.
    ///
    /// The arrival (`MatchArrivalPresentation`) still owns the hold, the input, the curtain and the camera's return;
    /// it calls `Begin`, `Roll`, `Playing`, `Age` and `End` through `MatchOpening`, exactly as it does the Arena's.
    ///
    /// WHAT THE BASE GIVES EVERY FILM, SO NO DIRECTOR CAN FORGET ONE:
    ///   * THE CLOCK. Every frame is a pure function of `Age`, a time since `Began`, a stamp on the clock every peer
    ///     shares (`SharedUltimatePhase.Now`), and of what every peer already holds (the seats, their picks, round 1's
    ///     taya, the map). Like the Arena's, each peer stamps `Began` when its own curtain lifts (see `Roll`).
    ///   * TWO CUTS. The full film every play of the map; the short one under reduced UI motion: the director's own
    ///     short beats, still cameras and cuts only, no strobe and no shake. Camera motion off: no film at all.
    ///   * THE CLOSE (`Close`): an attacker's screen ends on themselves for `OwnSeconds` ("YOU · ATTACKER", framed
    ///     from their renderer bounds, a slow push), the taya's and a watcher's hold the taya for the same time, so
    ///     every peer's film is the same length. Then one deliberate return to this player's own view.
    ///   * A SKIP. Hold confirm (keyboard Space or Enter, pad South or Start, a held touch) and the ring fills: the
    ///     film jumps to its close on this screen only. Nothing travels; the ready gate waits for everyone as before.
    ///   * EVERYTHING PUT BACK (`End`, safe at any moment and any number of times): every model on its body, every
    ///     hidden renderer drawn, every light, the sky, the fog, the ambient and the grade as they were, every prop,
    ///     extra and ghost destroyed, the UI up, the sound stopped. Nothing here moves a body, a collider or anything
    ///     replicated.
    /// </summary>
    [DefaultExecutionOrder(880)]
    public abstract partial class MapFilm : MonoBehaviour, IMatchOpening
    {
        /// <summary>When the close begins, when this player's own beat ends, when the return begins, and the end.</summary>
        public struct Times
        {
            public bool Full;
            public float Close, Own, Handoff, End;
        }

        /// <summary>The attacker's own closing shot, as long as the arrival's (`MatchArrivalPresentation.OwnSeconds`).</summary>
        public const float OwnSeconds = MatchArrivalPresentation.OwnSeconds;
        public const float OwnHold = 0.4f, ReturnSeconds = 1.4f;

        public static MapFilm Instance { get; private set; }
        public static bool HidesUi => Instance != null && Instance._prepared;
        /// <summary>The last film this session: how long it was to be, and whether it ran out or was cut.</summary>
        public static float LastSeconds { get; private set; }
        public static bool LastCompleted { get; private set; }
        public static string LastMap { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Instance = null; LastSeconds = 0.0f; LastCompleted = false; LastMap = null; }

        /// <summary>This map's film, made if the map is loaded and has none. Null on a map without one.</summary>
        public static MapFilm Ensure(string mapId)
        {
            if (Instance != null && Instance.MapId == mapId) return Instance;
            System.Type kind = mapId == SceneFlow.Eskinita ? typeof(TondoFilm) : mapId == SceneFlow.IlalimNgTulay ? typeof(ErmitaFilm)
                : mapId == SceneFlow.LagoonCove ? typeof(SitangkaiFilm) : mapId == SceneFlow.Kanto ? typeof(BgcFilm)
                : mapId == SceneFlow.Payo ? typeof(BanaueFilm) : null;
            if (kind == null) return null;
            if (Instance != null) Destroy(Instance.gameObject);
            var go = new GameObject("Map film (" + mapId + ")");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.IsValid()) UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
            var film = (MapFilm)go.AddComponent(kind);
            film.MapId = mapId;
            return film;
        }

        public static void Stop() { if (Instance != null) Instance.End(); }

        // ------------------------------------------------------------------ what the arrival sees

        public string MapId { get; private set; }
        public bool Playing { get; private set; }
        public float Age { get; private set; }
        public Times Timeline { get; private set; }
        public float Handoff => Timeline.Handoff;
        public float Duration => Timeline.End;
        public bool Full => Timeline.Full;
        /// <summary>The stamp on the shared clock this peer's film runs from; NaN until `Roll`.</summary>
        public double Began { get; private set; } = double.NaN;
        /// <summary>The seat this screen plays as, set before `Begin`; -1 for a watcher. An attacker closes on themselves.</summary>
        public int ViewerSeat { get; set; } = -1;
        /// <summary>The beat the director says it is in, for traces and probes.</summary>
        public string Beat { get; protected set; } = "";

        /// <summary>For probes: hold the film at this age (negative: run on the clock).</summary>
        public float HeldAge { get; set; } = -1.0f;
        /// <summary>For probes and the skip: put this peer's film at `age` on the shared clock.</summary>
        public void Seek(float age)
        {
            if (!_prepared) return;
            Began = SharedUltimatePhase.Now - Mathf.Max(0.0f, age);
            Age = Mathf.Max(0.0f, age);
            _last = Age;
            SeekScore(Age);
        }

        // ------------------------------------------------------------------ what a director writes

        /// <summary>The map's name on the title card and its place line under it.</summary>
        protected abstract string Region { get; }
        /// <summary>The film's own colour: the title's brush, the chips' edge, the skip ring.</summary>
        protected abstract Color Accent { get; }
        /// <summary>When the close begins in the full film and in the short one.</summary>
        protected abstract float CloseAt(bool full);
        /// <summary>Under the curtain: build the props, the extras, the ghosts; plan the walks. The models are taken.</summary>
        protected abstract void Prepare();
        /// <summary>Each frame, after the animator: pose everything for `age` (the camera through `Shoot`).</summary>
        protected abstract void Stage(float age);
        /// <summary>Anything of the director's own that the base does not already put back.</summary>
        protected virtual void Restore() { }
        /// <summary>The taya's shot for the close on a taya's or watcher's screen. Default: a front three-quarter.</summary>
        protected virtual void TayaShot(float local, out Vector3 eye, out Vector3 aim, out float fov) => Framed("close taya", Taya, 0.7f, local, out eye, out aim, out fov);

        // ------------------------------------------------------------------ state

        protected Camera Camera { get; private set; }
        protected bool Still { get; private set; }
        protected int Taya { get; private set; }
        protected Vector3 Centre { get; private set; }
        /// <summary>The seat whose screen this is closes on itself.</summary>
        protected bool OwnClose => ViewerSeat >= 0 && ViewerSeat < Seats && ViewerSeat != Taya && _body[ViewerSeat] != null;
        protected const int Seats = Core.Balance.PlayerCount;
        private Vector3 _homePosition;
        private Quaternion _homeRotation;
        private float _homeFov;
        private bool _prepared;
        private float _last = -1.0f;

        private void Awake() => Instance = this;
        private void OnEnable() => Instance = this;
        private void OnDisable() { End(); if (Instance == this) Instance = null; }
        private void OnDestroy() { End(); if (Instance == this) Instance = null; }

        public bool Begin(Camera camera, CharacterMotor[] players, Vector3 homePosition, Quaternion homeRotation, float homeFov, bool reducedMotion)
        {
            End();
            var match = GameServices.Match;
            string refused = camera == null ? "no camera" : players == null ? "no players"
                : match != null && (match.MatchInProgress || match.RoundNumber > 0) ? $"match under way (round {match.RoundNumber})"
                : !Settings.SettingsStore.Current.CinematicCameraMotion ? "cinematic camera motion off" : null;
            if (refused != null) { MatchArrivalPresentation.Trace(MapId + " film refused: " + refused); return false; }

            Camera = camera;
            _homePosition = homePosition; _homeRotation = homeRotation; _homeFov = homeFov;
            Still = reducedMotion;
            Taya = Core.MatchRules.DefenderSlotFor(1);
            var can = GameServices.Round != null && GameServices.Round.Lata != null ? GameServices.Round.Lata : FindFirstObjectByType<Lata>();
            Centre = can != null ? can.transform.position : Vector3.zero;
            float close = CloseAt(!reducedMotion);
            Timeline = new Times { Full = !reducedMotion, Close = close, Own = close + OwnSeconds, Handoff = close + OwnSeconds + OwnHold, End = close + OwnSeconds + OwnHold + ReturnSeconds };
            LastSeconds = Timeline.End; LastCompleted = false; LastMap = MapId;
            GameServices.Match?.PreparePresentationMatch();
            Age = 0.0f; Began = double.NaN; _last = -1.0f; HeldAge = -1.0f; Beat = "";
            _ownFramed = false; _skipHeld = 0.0f; _framed.Clear();

            TakeModels(players);
            SnapshotLight();
            HideUi();
            // The can's protection ring is the round's, not the picture's (film r1 of Sitangkai: a yellow ring
            // round the can in every shot of it); `ShowForCapture` is its own switch for exactly this.
            foreach (var clock in FindObjectsByType<Visual.LataClockPresentation>()) clock.ShowForCapture(false);
            BuildOverlay();
            BuildSound();
            _prepared = true; Playing = true;
            UnityEngine.Camera.onPreCull -= PinCastBeforeRender;
            UnityEngine.Camera.onPreCull += PinCastBeforeRender;
            RenderPipelineManager.beginCameraRendering -= PinCastBeforeSrpRender;
            RenderPipelineManager.beginCameraRendering += PinCastBeforeSrpRender;
            try { Prepare(); }
            catch (System.Exception failure) { Debug.LogException(failure); End("failed to prepare"); return false; }
            Frame();
            return true;
        }

        /// <summary>
        /// The curtain is up: start the clock. ⚠️ THIS IS THE ONE PLACE THE STAMP IS TAKEN, as in `ArenaIntro.Roll`:
        /// a host-authored start would be read here and nothing else would change, since every beat is drawn from
        /// `Age` alone.
        /// </summary>
        public void Roll()
        {
            if (!_prepared || !double.IsNaN(Began)) return;
            Began = SharedUltimatePhase.Now;
            StartScore();
        }

        public void End() => End("stopped");

        private void End(string why)
        {
            if (!_prepared) return;
            MatchArrivalPresentation.Trace($"{MapId} film {why} at {Age:F1} of {Timeline.End:F1} s");
            _prepared = false; Playing = false;
            UnityEngine.Camera.onPreCull -= PinCastBeforeRender;
            RenderPipelineManager.beginCameraRendering -= PinCastBeforeSrpRender;
            try { Restore(); } catch (System.Exception failure) { Debug.LogException(failure); }
            ReleaseModels();
            RestoreHidden();
            DestroyOwned();
            RestoreLight();
            ShowUi();
            foreach (var clock in FindObjectsByType<Visual.LataClockPresentation>()) clock.ShowForCapture(true);
            DestroyOverlay();
            StopSound();
            Camera = null;
        }

        // ------------------------------------------------------------------ the frame

        private void Update()
        {
            if (!_prepared) return;
            if (HeldAge >= 0.0f) Age = HeldAge;
            else Age = double.IsNaN(Began) ? 0.0f : Mathf.Max(0.0f, (float)(SharedUltimatePhase.Now - Began));
            if (Age >= Timeline.End && HeldAge < 0.0f)
            {
                LastCompleted = true;
                End("ran out");
                return;
            }
            ReadSkip();
        }

        private void LateUpdate()
        {
            if (_prepared) Frame();
        }

        /// <summary>True on the frame the film's clock passes `at` (never on a jump made by the skip).</summary>
        protected bool Crossed(float at) => at >= 0.0f && _last < at && Age >= at && !double.IsNaN(Began);

        /// <summary>How far the film's own clock moved since the last frame drawn (never the wall clock, so a held or
        /// stepped film, a probe's or a recording's, breathes at the film's pace).</summary>
        protected float FrameDelta { get; private set; }

        private void Frame()
        {
            if (Camera == null) { End("lost its camera"); return; }
            float age = Age;
            FrameDelta = _last < 0.0f ? 0.0f : Mathf.Clamp(age - _last, 0.0f, 0.1f);
            _shot = false;
            try { Stage(age); }
            catch (System.Exception failure) { Debug.LogException(failure); End("failed to stage"); return; }
            if (!_prepared) return;
            Close(age);
            Picture(age);
            Overlay(age);
            Sound(age);
            RememberCastPose();
            _last = age;
        }

        // ------------------------------------------------------------------ the skip

        private float _skipHeld;
        private const float SkipHold = 0.6f;
        /// <summary>0 to 1 while confirm is held, for the ring.</summary>
        protected float SkipProgress => _skipHeld / SkipHold;

        private void ReadSkip()
        {
            bool held = false;
            var keys = UnityEngine.InputSystem.Keyboard.current;
            if (keys != null) held |= keys.spaceKey.isPressed || keys.enterKey.isPressed || keys.numpadEnterKey.isPressed;
            var pad = UnityEngine.InputSystem.Gamepad.current;
            if (pad != null) held |= pad.buttonSouth.isPressed || pad.startButton.isPressed;
            var touch = UnityEngine.InputSystem.Touchscreen.current;
            if (touch != null) held |= touch.primaryTouch.press.isPressed;
            bool open = !double.IsNaN(Began) && Age < Timeline.Close - 0.3f && HeldAge < 0.0f;
            _skipHeld = held && open ? _skipHeld + Time.unscaledDeltaTime : 0.0f;
            if (_skipHeld < SkipHold) return;
            _skipHeld = 0.0f;
            MatchArrivalPresentation.Trace($"{MapId} film skipped at {Age:F1} s to its close");
            Seek(Timeline.Close);
        }

        protected static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3.0f - 2.0f * t);
        }

        /// <summary>0 before `from`, 1 after `to`, smooth between.</summary>
        protected static float Span(float age, float from, float to) => Smooth((age - from) / Mathf.Max(0.0001f, to - from));

        /// <summary>Overshooting ease out (anticipation already spent): 0 to 1 with a kick past 1.</summary>
        protected static float Back(float t, float kick = 1.7f)
        {
            t = Mathf.Clamp01(t) - 1.0f;
            return 1.0f + t * t * ((kick + 1.0f) * t + kick);
        }
    }
}
