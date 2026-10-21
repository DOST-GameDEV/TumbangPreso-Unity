using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TumbangPreso.Map
{
    /// <summary>
    /// PAYO'S OWN BREAK CAMERA (owner, 2026-10-19: "create more unique mid round cinematics, not only that but the camera
    /// cinematics end up going through the platform pillars", and "for halftime, its currently just the same change
    /// layout cinematic, i need something that puts more emphasis on the sun setting and the map becoming night time").
    ///
    /// Payo borrows the Arena's stage and its break camera (`ArenaBreakCamera`). The Arena's five shots were written for
    /// a stadium: low pushes across the stage at deck height, which on this map are low pushes through the stone pillars.
    /// This is a layer on that camera and nothing else: it registers `ArenaBreakCamera.Director`, and while a break's show
    /// plays on a stone stage it poses the break camera itself. The Arena never registers one, so its camera is exactly
    /// what it was.
    ///
    /// SIX AUTHORED SHOTS FOR AN ORDINARY BREAK, ONE PER BREAK, each a single smooth move matched to the stones' beats
    /// (`ArenaStage.BreakBeats`):
    ///   0. BULUL PASS      a low dolly along the plaza past the seated bulul as the stones rise behind it;
    ///   1. TERRACE CRANE   from down in the moat, a crane up the stone wall following one block out of the pit to its lock;
    ///   2. FALLS ORBIT     from over the basin, rising past the waterfall under the court and orbiting up to the stones;
    ///   3. TOP DOWN        straight down on the court as it rearranges, turning and settling lower;
    ///   4. TERRACE PUSH    a long push-in down the water stair between the paddies to the court;
    ///   5. LOCK WHIP       a long lens on one stone locking, a whip-pan to another locking a second later.
    /// WHICH SHOT IS PICKED FROM SHARED DATA ONLY: a shuffle of the six seeded by the match id, walked by the completed
    /// round (`Pick`), and a mirror bit from the same numbers. Every peer cuts the same shot on the same frame; a match of
    /// six rounds (four ordinary breaks) never repeats one. Nothing is sent.
    ///
    /// HALF TIME HAS ITS OWN FILM, NOT A LAYOUT SHOT (`HalftimeFrame`): the sun low over the valley with the court under
    /// it, the camera tilting down after it as the light goes amber then blue; a wide hold on the plaza and the terraced
    /// town as the fire bowls and the house lamps come on one by one; the moon over the north valley with the first stars
    /// and fireflies; then down to the court in the night. Its beats are placed on the dusk's own curve
    /// (`PayoNight.DuskAt`, the read-only twin of the clock `PayoNight.FactorFor` rides), so the camera and the sky agree
    /// on every peer. The sun and the moon are not touched: the camera only looks at them.
    ///
    /// EVERY SHOT ENDS WHERE THE ROUND'S CAMERA BEGINS. The last `HandbackSeconds` glide from the shot to this peer's own
    /// eye at its spawn for the next round (`SliceRunner.SpawnPointFor`, the floor read off the next layout's numbers,
    /// facing the can, the game's own lens), so the cut back to the game camera does not jump.
    ///
    /// ⚠️ NO PATH GOES THROUGH THE MAP. Every shot, on every pair of layouts a break travels between, with the moving
    /// stones posed at each sample, and every hand-back to every spawn, is checked offline against the drawn map
    /// (`Editor/MapKit/PayoBreakShots.cs`, verb `clip`): sphere sweeps along the path against every drawn mesh, a check
    /// that the eye is never inside a closed mesh or the bounds of a compact solid (a statue, a house, a stone pillar).
    /// And at run time `Guard` pulls the eye in along its look line when anything drawn stands in it: the stones' own
    /// bounds as they move, the compact solids' bounds, and any collider that is drawn (the invisible bounds walls and
    /// the layouts' stand-in colliders are not: the stones' drawn bounds are what the eye must clear).
    ///
    /// Presentation only: local, no random stream, no collider, nothing gameplay reads.
    /// </summary>
    [DefaultExecutionOrder(960)]
    public sealed class PayoBreakDirector : MonoBehaviour
    {
        public static PayoBreakDirector Instance { get; private set; }

        public enum Shot { BululPass = 0, TerraceCrane = 1, FallsOrbit = 2, TopDown = 3, TerracePush = 4, LockWhip = 5 }
        public const int ShotCount = 6;
        /// <summary>The half-time film, where a shot number is asked for.</summary>
        public const int Halftime = -1;
        /// <summary>The reduced-motion still, where a shot number is asked for.</summary>
        public const int Still = -2;
        public static readonly string[] ShotNames = { "bulul pass", "terrace crane", "falls orbit", "top down", "terrace push", "lock whip" };

        /// <summary>An ordinary break's last this-many seconds glide to the player's own eye.</summary>
        public const float HandbackSeconds = 1.6f;
        /// <summary>How high the eye stands over the floor at the spawn (`CameraRig`: half the capsule plus `FppEyeHeight`).</summary>
        public const float EyeOverFloor = CameraSystem.CameraRig.PersonCapsuleHeight * 0.5f + CameraSystem.CameraRig.FppEyeHeight;
        /// <summary>The run-time guard's ball, and how far it keeps the eye off what it found, metres.</summary>
        public const float GuardRadius = 0.35f, GuardMargin = 0.3f;

        /// <summary>
        /// ⚠️ OFF BY DEFAULT (2026-10-19, wrapped up before the owner's push): the shots and the half-time film are built and
        /// filmed, but the clip check still reported hand-back and low-shot hits when the session ended, so in a real match
        /// the Arena's own break camera runs exactly as before. Set true (or force a shot from a probe) to see the director.
        /// </summary>
        [System.NonSerialized] public static bool Enabled = false;
        /// <summary>A probe may force the shot (0 to 5, `Halftime` or `Still`) and the mirror bit; under 0 it follows the match.</summary>
        [System.NonSerialized] public static int ForceShot = -99, ForceVariant = -1;
        /// <summary>Frames in which the run-time guard moved the eye by more than a centimetre, and the most it moved it (metres).</summary>
        public static int GuardFrames { get; private set; }
        public static float GuardMost { get; private set; }
        /// <summary>The last pose the director gave, this frame or before (what the hand-back measure compares with the round's camera).</summary>
        public static Vector3 LastEye { get; private set; }
        public static Quaternion LastRotation { get; private set; }
        public static float LastFov { get; private set; }
        public static float LastAge { get; private set; } = -1.0f;
        public static int LastShot { get; private set; } = -99;

        /// <summary>One camera pose.</summary>
        public struct Frame
        {
            public Vector3 Eye;
            public Quaternion Rotation;
            public float Fov;
            public Vector3 Look => Eye + Rotation * Vector3.forward;
        }

        private ArenaStage _stage;
        private readonly Dictionary<int, int> _crane = new Dictionary<int, int>();
        private readonly Dictionary<int, Vector2Int> _whip = new Dictionary<int, Vector2Int>();
        private readonly List<Bounds> _solids = new List<Bounds>();
        private readonly List<Renderer> _stones = new List<Renderer>();
        private readonly RaycastHit[] _hits = new RaycastHit[24];
        private readonly Dictionary<Collider, bool> _drawnCollider = new Dictionary<Collider, bool>();
        private bool _gathered;
        private float _keep = 1.0f;

        // ------------------------------------------------------------------ install

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Install(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Install(scene);

        /// <summary>On a scene that carries Payo's stage show (the Payo map), and only there, the director joins it.</summary>
        private static void Install(Scene scene)
        {
            if (Instance != null || !scene.IsValid() || !scene.isLoaded) return;
            var show = FindFirstObjectByType<PayoStageShow>();
            if (show == null || show.gameObject.scene != scene) return;
            var go = new GameObject("Payo break director (made at run time)");
            SceneManager.MoveGameObjectToScene(go, scene);
            go.AddComponent<PayoBreakDirector>();
        }

        private void OnEnable()
        {
            Instance = this;
            ArenaBreakCamera.Director = Direct;
            GuardFrames = 0; GuardMost = 0.0f;
        }

        private void OnDisable()
        {
            if (ArenaBreakCamera.Director == (System.Func<Camera, float>)Direct) ArenaBreakCamera.Director = null;
            if (Instance == this) Instance = null;
        }

        // ------------------------------------------------------------------ the live picture

        /// <summary>
        /// `ArenaBreakCamera.Director`: pose the break camera for this frame of the show and answer how much of the
        /// camera's own lock shake to add (0 to 1), or under 0 to leave the frame to the Arena's own shots.
        /// </summary>
        private float Direct(Camera camera)
        {
            if (!Enabled && ForceShot < Still) return -1.0f;
            var stage = ArenaStage.Instance;
            var hp = HalftimePresentation.Instance;
            if (stage == null || !stage.Stone || hp == null || !stage.TryBreak(out var beats) || !beats.Showing) return -1.0f;
            _stage = stage;

            int shot = ForceShot >= Still ? ForceShot : Settings.SettingsStore.Current.ReducedUiMotion ? Still : beats.Halftime ? Halftime : Pick(hp.MatchId, hp.CompletedRound);
            int variant = ForceVariant >= 0 ? ForceVariant : Variant(hp.MatchId, hp.CompletedRound);
            int seat = NetAuthority.IsNetworked ? NetAuthority.LocalSlot : GameLaunch.SoloSeat;
            bool tpp = false;
            var rig = Camera.main != null ? Camera.main.GetComponent<CameraSystem.CameraRig>() : null;
            if (rig != null)
            {
                if (rig.Following != null) seat = rig.Following.PlayerSlot;
                tpp = rig.Mode == CameraSystem.CameraMode.Tpp;
            }

            var frame = FrameAt(shot, variant, beats, seat, hp.NextTaya, tpp);
            frame.Eye = Guard(frame.Eye, frame.Look);
            camera.transform.SetPositionAndRotation(frame.Eye, frame.Rotation);
            if (!Mathf.Approximately(camera.fieldOfView, frame.Fov)) camera.fieldOfView = frame.Fov;
            LastEye = frame.Eye; LastRotation = frame.Rotation; LastFov = frame.Fov; LastAge = beats.Age; LastShot = shot;

            // The lock thuds shake an ordinary break; at half time the sky shots take only a breath of it.
            return shot == Halftime ? (beats.Age > HalftimeReturn(beats) ? 0.6f : 0.15f) : shot == Still ? 0.0f : 1.0f;
        }

        /// <summary>
        /// The shot an ordinary break cuts: a shuffle of the six seeded by the match id, walked by the completed round,
        /// a fresh shuffle each six breaks. Integer arithmetic only (SplitMix64, `ArenaStage.LayoutFor`'s), so every peer
        /// and platform agree.
        /// </summary>
        public static int Pick(long matchId, int completedRound)
        {
            int index = Mathf.Max(0, completedRound - 1);
            int lap = index / ShotCount;
            System.Span<int> order = stackalloc int[ShotCount];
            for (int i = 0; i < ShotCount; i++) order[i] = i;
            ulong state = unchecked((ulong)matchId ^ (0xA24BAED4963EE407UL * (ulong)(lap + 1)));
            for (int i = ShotCount - 1; i > 0; i--)
            {
                int j = (int)(Mix(ref state) % (ulong)(i + 1));
                (order[i], order[j]) = (order[j], order[i]);
            }
            return order[index % ShotCount];
        }

        /// <summary>The mirror bit of a break (which side a shot is taken from), from the same shared numbers.</summary>
        public static int Variant(long matchId, int completedRound)
        {
            ulong state = unchecked((ulong)matchId * 31UL + (ulong)completedRound * 0x9E3779B97F4A7C15UL);
            return (int)(Mix(ref state) & 1UL);
        }

        private static ulong Mix(ref ulong state)
        {
            unchecked
            {
                state += 0x9E3779B97F4A7C15UL;
                ulong z = state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        /// <summary>
        /// The beats of a break the stage is not playing, for a probe or the offline check: the same numbers
        /// `ArenaStage.TryBreak` gives (0.3 s hologram lead, 2.3 s alarm, a 1.8 s settle, the reveal 0.15 s after the
        /// last lock). The probe compares them with a real break's.
        /// </summary>
        public static ArenaStage.BreakBeats Beats(int from, int to, float age, float duration, bool halftime)
        {
            var b = new ArenaStage.BreakBeats { From = from, To = to, Halftime = halftime, Showing = true, Duration = duration, Age = age };
            b.MoveStart = 0.3f + 2.3f;
            b.ScanStart = b.MoveStart - 2.3f;
            b.ScanEnd = b.ScanStart + ArenaStage.ScanSeconds;
            b.Undock = b.MoveStart - ArenaStage.UndockLead;
            b.MoveEnd = duration - 1.8f;
            b.Reveal = b.MoveEnd + ArenaStage.RevealLag;
            return b;
        }

        // ------------------------------------------------------------------ the shots

        /// <summary>
        /// The pose `beats.Age` seconds into a break's show: `shot` 0 to 5, `Halftime` or `Still`; `variant` the mirror
        /// bit; `seat` and `nextTaya` say whose spawn the hand-back ends on. Pure but for the stage's numbers: the offline
        /// check calls it over every shot, layout pair and seat.
        /// </summary>
        public Frame FrameAt(int shot, int variant, in ArenaStage.BreakBeats beats, int seat, int nextTaya, bool tpp = false)
        {
            if (_stage == null) _stage = ArenaStage.Instance;
            var target = HandbackFrame(seat, nextTaya, beats.To, tpp);
            if (shot == Still) return Look(new Vector3(26.0f, 20.0f, -40.0f), new Vector3(0.0f, 1.0f, 0.0f), 52.0f);

            float t = beats.Age, d = beats.Duration;
            if (shot == Halftime)
            {
                float back = HalftimeReturn(beats);
                var sky = HalftimeFrame(beats);
                return t <= back ? sky : Blend(sky, target, (t - back) / Mathf.Max(0.1f, d - back), 4.0f, Cruise(beats.To));
            }

            float hand = d - HandbackSeconds;
            Frame f = ShotFrame(shot, variant, beats, hand);
            return t <= hand ? f : Blend(f, target, (t - hand) / HandbackSeconds, 3.0f, Cruise(beats.To));
        }

        private Frame ShotFrame(int shot, int variant, in ArenaStage.BreakBeats beats, float hand)
        {
            float t = beats.Age;
            float s = variant == 0 ? 1.0f : -1.0f;
            float u = Mathf.Clamp(t / hand, 0.0f, 1.0f + HandbackSeconds / hand);
            float e = Ease(u);
            Vector3 c = _stage != null ? _stage.transform.position : Vector3.zero;

            switch ((Shot)shot)
            {
                case Shot.BululPass:
                {
                    // Along the plaza's edge outside the seated bulul (its plinth stands from x 31 to 43), low, the
                    // statue sliding across the frame between the eye and the rising stones.
                    Vector3 eye = c + new Vector3(s * 42.2f, Mathf.Lerp(0.8f, 3.2f, e), Mathf.Lerp(-18.0f, 22.0f, e));
                    Vector3 look = c + new Vector3(s * 5.0f, Mathf.Lerp(0.8f, 2.2f, e), Mathf.Lerp(-10.0f, 12.0f, e));
                    return Look(eye, look, Mathf.Lerp(44.0f, 50.0f, e));
                }
                case Shot.TerraceCrane:
                {
                    int piece = CranePiece(beats.From, beats.To);
                    if (piece < 0) return ShotFrame((int)Shot.TopDown, variant, beats, hand);
                    var shape = _stage.Pieces[piece].Shapes[beats.To];
                    var move = _stage.MoveOf(piece, beats.From, beats.To, beats.Travel);
                    float starts = beats.At(move.Starts), locks = beats.At(move.Locks);
                    Vector3 top = PieceTop(piece, beats);
                    bool alongX = Mathf.Abs(shape.X) >= Mathf.Abs(shape.Z);
                    float side = alongX ? Mathf.Sign(shape.X) : Mathf.Sign(shape.Z);
                    float lateral = Mathf.Clamp((alongX ? shape.Z : shape.X) + 6.0f * s, -16.0f, 16.0f);
                    float rise = Smooth((t - (starts - 0.5f)) / (locks - starts + 1.3f));
                    float y = Mathf.Lerp(-7.0f + 0.25f * Mathf.Min(t, starts), shape.Top + 5.0f, rise) + Mathf.Max(0.0f, t - locks - 0.3f) * 0.5f;
                    Vector3 local = alongX ? new Vector3(side * 26.0f, y, lateral) : new Vector3(lateral, y, side * 26.0f);
                    Vector3 eye = _stage.transform.TransformPoint(local);
                    Vector3 look = new Vector3(top.x, Mathf.Max(top.y, c.y - 12.0f) + 0.4f, top.z);
                    return Look(eye, look, 50.0f);
                }
                case Shot.FallsOrbit:
                {
                    // Over the basin north of the court: low by the waterfall that pours from under the court, rising
                    // and swinging round until the stones are in the frame.
                    float bearing = s * Mathf.Lerp(-30.0f, 26.0f, e);
                    float radius = Mathf.Lerp(33.0f, 30.0f, e);
                    Vector3 pivot = c + new Vector3(0.0f, 0.0f, 30.0f);
                    Vector3 eye = pivot + ArenaStageMesh.Direction(bearing) * radius + Vector3.up * Mathf.Lerp(-17.0f, 4.5f, Smooth(u));
                    Vector3 look = Vector3.Lerp(c + new Vector3(0.0f, -33.0f, 33.0f), c + new Vector3(0.0f, 1.5f, 2.0f), Smooth((u - 0.15f) / 0.55f));
                    return Look(eye, look, Mathf.Lerp(52.0f, 46.0f, e));
                }
                case Shot.TopDown:
                {
                    float radius = _stage != null ? _stage.Radius : 22.0f;
                    float yaw = (s > 0.0f ? 20.0f : 200.0f) + 28.0f * e;
                    float height = Mathf.Lerp(40.0f, 33.0f, Smooth(u));
                    Vector3 ahead = ArenaStageMesh.Direction(yaw);
                    Vector3 eye = c - ahead * 3.0f + Vector3.up * height;
                    float fov = Mathf.Clamp(2.0f * Mathf.Atan(radius * 1.15f / height) * Mathf.Rad2Deg, 40.0f, 75.0f);
                    return Look(eye, c, fov, ahead);
                }
                case Shot.TerracePush:
                {
                    // Down the water stair from the town, between the paddies and the lantern poles, to the court.
                    float p = Smoother(u);
                    Vector3 eye = c + new Vector3(s * Mathf.Lerp(2.5f, 1.0f, p), Mathf.Lerp(24.0f, 8.5f, p), Mathf.Lerp(-118.0f, -40.0f, p));
                    Vector3 look = c + new Vector3(0.0f, Mathf.Lerp(1.0f, 2.0f, p), Mathf.Lerp(-4.0f, 6.0f, p));
                    return Look(eye, look, Mathf.Lerp(36.0f, 48.0f, p));
                }
                case Shot.LockWhip:
                {
                    var pair = WhipPieces(beats.From, beats.To);
                    if (pair.x < 0) return ShotFrame((int)Shot.BululPass, variant, beats, hand);
                    float lockA = beats.At(_stage.MoveOf(pair.x, beats.From, beats.To, 0.0f).Locks);
                    float lockB = beats.At(_stage.MoveOf(pair.y, beats.From, beats.To, 0.0f).Locks);
                    Vector3 a = PieceTop(pair.x, beats), b = PieceTop(pair.y, beats);
                    Vector3 restA = PieceRest(pair.x, beats.To), restB = PieceRest(pair.y, beats.To);
                    Vector3 mid = (restA + restB) * 0.5f - c; mid.y = 0.0f;
                    float bearing = mid.sqrMagnitude > 4.0f ? Mathf.Atan2(mid.x, mid.z) * Mathf.Rad2Deg : Bearing(restA - c) + 90.0f * s;
                    bearing += 8.0f * s * e;
                    Vector3 eye = c + ArenaStageMesh.Direction(bearing) * 34.0f + Vector3.up * 14.0f;
                    Vector3 lookA = new Vector3(a.x, Mathf.Max(a.y, c.y - 6.0f) + 0.3f, a.z), lookB = new Vector3(b.x, Mathf.Max(b.y, c.y - 6.0f) + 0.3f, b.z);
                    float whip = Smoother((t - (lockA + 0.2f)) / 0.4f);
                    var ra = Quaternion.LookRotation(lookA - eye, Vector3.up);
                    var rb = Quaternion.LookRotation(lookB - eye, Vector3.up);
                    float fov = t < lockB + 0.4f ? Mathf.Lerp(46.0f, 24.0f, Smooth(t / Mathf.Max(0.5f, lockA - 0.9f)))
                                                 : Mathf.Lerp(24.0f, 44.0f, Smooth((t - lockB - 0.4f) / 1.2f));
                    return new Frame { Eye = eye, Rotation = Quaternion.Slerp(ra, rb, whip), Fov = fov };
                }
            }
            return Look(c + new Vector3(26.0f, 20.0f, -40.0f), c + Vector3.up, 52.0f);
        }

        // ------------------------------------------------------------------ half time

        /// <summary>When the half-time film starts down to the court: after the moon is up, before the show ends.</summary>
        public static float HalftimeReturn(in ArenaStage.BreakBeats beats) =>
            Mathf.Min(AgeAtDusk(0.995f, beats.Duration), beats.Duration - 1.7f);

        /// <summary>The show's age at which the dusk reaches `night` (the inverse of `PayoNight.DuskAt`, by halving).</summary>
        public static float AgeAtDusk(float night, float duration)
        {
            float lo = -1.0f, hi = duration;
            for (int i = 0; i < 24; i++)
            {
                float mid = (lo + hi) * 0.5f;
                if (PayoNight.DuskAt(mid, duration) < night) lo = mid; else hi = mid;
            }
            return (lo + hi) * 0.5f;
        }

        /// <summary>
        /// The half-time film's sky part, on the dusk's own curve:
        ///   SUNDOWN  until the dusk is nearly half way: from behind the court's south gate, high, the sun low over the
        ///            north valley; the camera cranes down and tilts after it as the light goes amber and then blue;
        ///   LAMPS    until the fire bowls are all lit: a wide from over the basin to the south, the court, both plaza arms
        ///            with their bowls, the gate's torches and the terraced town, its windows coming on;
        ///   MAYARI   until full night: low behind the court looking north, rising from the valley's dark rim up to the moon.
        /// </summary>
        public Frame HalftimeFrame(in ArenaStage.BreakBeats beats)
        {
            Vector3 c = _stage != null ? _stage.transform.position : Vector3.zero;
            float t = beats.Age, d = beats.Duration;
            float lamps = AgeAtDusk(0.47f, d), moon = AgeAtDusk(0.86f, d), back = HalftimeReturn(beats);
            Vector3 toSun = SunDirection(), toMoon = MoonDirection();

            if (t < lamps)
            {
                float u = Mathf.Clamp01(t / lamps), e = Ease(u);
                Vector3 eye = c + Vector3.Lerp(new Vector3(-5.0f, 12.5f, -42.0f), new Vector3(0.0f, 9.0f, -33.0f), e);
                float pitch = Mathf.Lerp(Elevation(toSun), 6.0f, Smoother(u));
                return Toward(eye, Bearing(toSun), pitch, Mathf.Lerp(48.0f, 42.0f, e));
            }
            if (t < moon)
            {
                float u = Mathf.Clamp01((t - lamps) / (moon - lamps)), e = Ease(u);
                Vector3 eye = c + Vector3.Lerp(new Vector3(12.0f, 16.0f, 66.0f), new Vector3(-10.0f, 14.0f, 62.0f), e);
                Vector3 look = c + Vector3.Lerp(new Vector3(0.0f, -2.0f, -10.0f), new Vector3(0.0f, 0.0f, -16.0f), e);
                return Look(eye, look, 50.0f);
            }
            {
                float u = Mathf.Clamp01((t - moon) / Mathf.Max(0.5f, back - moon)), e = Ease(u);
                Vector3 eye = c + Vector3.Lerp(new Vector3(6.0f, 5.5f, -30.0f), new Vector3(3.0f, 7.5f, -27.0f), e);
                float pitch = Mathf.Lerp(8.0f, Elevation(toMoon) - 2.0f, Smoother(u));
                float bearing = Mathf.LerpAngle(Bearing(toMoon) - 6.0f, Bearing(toMoon), e);
                return Toward(eye, bearing, pitch, Mathf.Lerp(44.0f, 38.0f, e));
            }
        }

        private static Vector3 SunDirection()
        {
            var night = FindNight();
            return night != null ? -(night.SunDay * Vector3.forward) : new Vector3(0.0f, 0.24f, 0.97f);
        }

        private static Vector3 MoonDirection()
        {
            var moon = PayoMoon.Instance;
            return moon != null ? PayoMoon.Direction(moon.Bearing, moon.Elevation) : new Vector3(0.22f, 0.37f, 0.9f);
        }

        private static PayoNight _night;
        private static PayoNight FindNight()
        {
            if (_night == null) _night = FindFirstObjectByType<PayoNight>();
            return _night;
        }

        // ------------------------------------------------------------------ the hand-back

        /// <summary>
        /// Where the round's camera will be when the break ends, for this seat: at its spawn for the next round
        /// (`SliceRunner.SpawnPointFor`), on the next layout's floor (read off the stones' numbers, not the colliders, so
        /// any layout can be asked), at the eye's height, facing the can, at the game's own lens.
        /// </summary>
        public Frame HandbackFrame(int seat, int nextTaya, int layout, bool tpp = false)
        {
            Vector3 mark = SliceRunner.SpawnPointFor(Mathf.Clamp(seat, 0, Core.Balance.PlayerCount - 1), nextTaya);
            Vector3 c = _stage != null ? _stage.transform.position : Vector3.zero;
            Vector3 feet = new Vector3(mark.x, FloorAt(mark, layout), mark.z);
            Vector3 toCan = c - feet; toCan.y = 0.0f;
            if (toCan.sqrMagnitude < 1e-4f) toCan = Vector3.forward;
            toCan.Normalize();
            if (!tpp)
            {
                float fov = Settings.GameSettings.ValidFirstPersonFov(Settings.SettingsStore.Current.FirstPersonFov);
                return new Frame { Eye = feet + Vector3.up * EyeOverFloor, Rotation = Quaternion.LookRotation(toCan, Vector3.up), Fov = fov };
            }
            float pitch = -CameraSystem.CameraRig.TppBasePitchDeg * Mathf.Deg2Rad;
            Vector3 pivot = feet + Vector3.up * CameraSystem.CameraRig.TppMountHeight;
            Vector3 back = -toCan * Mathf.Cos(pitch) + Vector3.up * Mathf.Sin(pitch);
            Vector3 eye = pivot + back * CameraSystem.CameraRig.TppBaseSpringLength;
            return new Frame { Eye = eye, Rotation = Quaternion.LookRotation(pivot - eye, Vector3.up), Fov = CameraSystem.CameraRig.TppFieldOfView };
        }

        /// <summary>The top of whatever stone stands under a world point in a layout, from the stones' numbers (the can's height where none does).</summary>
        public float FloorAt(Vector3 world, int layout)
        {
            if (_stage == null) return world.y;
            Vector3 p = _stage.transform.InverseTransformPoint(world);
            float best = float.NegativeInfinity;
            foreach (var piece in _stage.Pieces)
            {
                if (piece?.Shapes == null || layout < 0 || layout >= piece.Shapes.Length) continue;
                var shape = piece.Shapes[layout];
                if (!shape.Exists || !shape.Contains(p.x, p.z, 0.0f)) continue;
                best = Mathf.Max(best, shape.HeightAt(p.x, p.z));
            }
            if (float.IsNegativeInfinity(best)) best = layout >= 0 && layout < _stage.LayoutCount ? _stage.Layouts[layout].CanHeight : 0.0f;
            return _stage.transform.TransformPoint(new Vector3(p.x, best, p.z)).y;
        }

        /// <summary>The height a hand-back glides at over a layout: clear of its tallest stone and what floats over it.</summary>
        public float Cruise(int layout)
        {
            float top = 0.0f;
            if (_stage != null)
                foreach (var piece in _stage.Pieces)
                    if (piece?.Shapes != null && layout >= 0 && layout < piece.Shapes.Length && piece.Shapes[layout].Exists)
                        top = Mathf.Max(top, Mathf.Max(piece.Shapes[layout].Top, piece.Shapes[layout].Top1));
            return (_stage != null ? _stage.transform.position.y : 0.0f) + top + 3.0f;
        }

        /// <summary>From one pose to another, eased at both ends. The eye is lifted on an arc of up to `arc` metres and,
        /// for most of the glide, kept at least at `cruise` (over the stones and the pickups floating over them), then
        /// comes down onto the last pose nearly straight from above, so a glide across the court never passes through it.</summary>
        public static Frame Blend(in Frame from, in Frame to, float w, float arc, float cruise = float.NegativeInfinity)
        {
            float k = Smoother(w);
            Vector3 eye = Vector3.Lerp(from.Eye, to.Eye, k);
            float lift = Mathf.Min(arc, 0.25f * Vector3.Distance(from.Eye, to.Eye)) * Mathf.Sin(Mathf.Clamp01(w) * Mathf.PI);
            float hold = Smooth(w / 0.3f) * (1.0f - Smooth((w - 0.72f) / 0.28f));
            lift = Mathf.Max(lift, Mathf.Max(0.0f, cruise - eye.y) * hold);
            return new Frame
            {
                Eye = eye + Vector3.up * lift,
                Rotation = Quaternion.Slerp(from.Rotation, to.Rotation, k),
                Fov = Mathf.Lerp(from.Fov, to.Fov, k),
            };
        }

        // ------------------------------------------------------------------ which stones

        /// <summary>The block the crane follows: one on the rim that rises out of the pit and locks mid-move.</summary>
        public int CranePiece(int from, int to)
        {
            if (_stage == null) return -1;
            int key = from * 64 + to;
            if (_crane.TryGetValue(key, out int cached)) return cached;
            int best = -1; float bestScore = float.NegativeInfinity;
            for (int i = 0; i < _stage.Pieces.Length; i++)
            {
                var motion = _stage.MotionOf(i, from, to);
                if (!ArenaStage.Locks(motion)) continue;
                var shape = _stage.Pieces[i].Shapes[to];
                if (!shape.IsBlock) continue;
                float edge = Mathf.Max(Mathf.Abs(shape.X) + shape.SX * 0.5f, Mathf.Abs(shape.Z) + shape.SZ * 0.5f);
                var move = _stage.MoveOf(i, from, to, 0.0f);
                float score = edge + (motion == ArenaStage.Motion.Arrive || motion == ArenaStage.Motion.Swap ? 6.0f : 0.0f)
                            + (move.Starts > 0.05f && move.Starts < 0.45f ? 4.0f : 0.0f);
                if (score > bestScore) { bestScore = score; best = i; }
            }
            _crane[key] = best;
            return best;
        }

        /// <summary>The two blocks the whip-pan joins: the first to lock, and the first to lock a second or more after it.</summary>
        public Vector2Int WhipPieces(int from, int to)
        {
            if (_stage == null) return new Vector2Int(-1, -1);
            int key = from * 64 + to;
            if (_whip.TryGetValue(key, out var cached)) return cached;
            var locks = new List<(int index, float at)>();
            for (int i = 0; i < _stage.Pieces.Length; i++)
            {
                var motion = _stage.MotionOf(i, from, to);
                if (!ArenaStage.Locks(motion) || !_stage.Pieces[i].Shapes[to].IsBlock) continue;
                locks.Add((i, _stage.MoveOf(i, from, to, 0.0f).Locks));
            }
            locks.Sort((x, y) => x.at.CompareTo(y.at));
            var pair = new Vector2Int(-1, -1);
            if (locks.Count >= 2)
            {
                var a = locks[0];
                var b = locks[locks.Count - 1];
                // A second of the move is 1/3.6 of its share (the move is 3.6 s long on an 8 s break).
                foreach (var l in locks) if (l.at - a.at >= 0.27f) { b = l; break; }
                pair = new Vector2Int(a.index, b.index);
            }
            _whip[key] = pair;
            return pair;
        }

        /// <summary>The middle of a block's top as drawn at this moment of the travel (its lift on).</summary>
        public Vector3 PieceTop(int index, in ArenaStage.BreakBeats beats)
        {
            var piece = _stage.Pieces[index];
            var move = _stage.MoveOf(index, beats.From, beats.To, beats.Travel);
            int shown = move.Shown >= 0 ? move.Shown : beats.To;
            var shape = piece.Shapes[shown];
            if (!shape.Exists) shape = piece.Shapes[beats.To];
            return _stage.transform.TransformPoint(new Vector3(shape.X, shape.Top + move.Lift, shape.Z));
        }

        private Vector3 PieceRest(int index, int layout)
        {
            var shape = _stage.Pieces[index].Shapes[layout];
            return _stage.transform.TransformPoint(new Vector3(shape.X, shape.Top, shape.Z));
        }

        // ------------------------------------------------------------------ the run-time guard

        /// <summary>
        /// Pull the eye in toward what it looks at when anything drawn stands between: a sphere sweep from near the look
        /// point out to the eye against drawn colliders, and a ray against the bounds of the compact solids and of the
        /// stones as they are drawn this frame. It pulls in at once and lets out slowly, so a near miss does not pump.
        /// The offline check means it should rest; `GuardFrames` counts the frames it did not.
        /// </summary>
        private Vector3 Guard(Vector3 eye, Vector3 look)
        {
            Gather();
            Vector3 span = eye - look;
            float dist = span.magnitude;
            if (dist < 0.75f) return eye;
            Vector3 dir = span / dist;
            float start = Mathf.Min(1.5f, dist * 0.2f);
            Vector3 from = look + dir * start;
            float reach = dist - start, free = reach;

            int n = Physics.SphereCastNonAlloc(from, GuardRadius, dir, _hits, reach, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var hit = _hits[i];
                if (hit.collider == null || hit.distance <= 0.0f || !Drawn(hit.collider)) continue;
                free = Mathf.Min(free, hit.distance);
            }

            var ray = new Ray(from, dir);
            foreach (var b in _solids) Clip(b, ray, ref free);
            foreach (var r in _stones)
                if (r != null && r.enabled && r.gameObject.activeInHierarchy) Clip(r.bounds, ray, ref free);

            float keep = Mathf.Clamp01((start + Mathf.Max(0.0f, free - GuardMargin)) / dist);
            if (free >= reach) keep = 1.0f;
            _keep = keep < _keep ? keep : Mathf.MoveTowards(_keep, keep, Time.unscaledDeltaTime * 0.6f);
            Vector3 guarded = look + dir * dist * _keep;
            float moved = (guarded - eye).magnitude;
            if (moved > 0.01f) { GuardFrames++; GuardMost = Mathf.Max(GuardMost, moved); }
            return guarded;
        }

        private static void Clip(Bounds b, Ray ray, ref float free)
        {
            b.Expand(GuardRadius * 2.0f);
            if (b.Contains(ray.origin)) return;
            if (b.IntersectRay(ray, out float enter) && enter < free) free = enter;
        }

        /// <summary>A collider counts when it is drawn (a renderer on it): not the court's invisible bounds walls, not a
        /// layout's stand-in colliders (the stones' drawn bounds stand for them), not a body.</summary>
        private bool Drawn(Collider collider)
        {
            var id = collider;
            if (_drawnCollider.TryGetValue(id, out bool drawn)) return drawn;
            drawn = collider.GetComponent<Renderer>() != null && !GameplayItem(collider)
                    && (_stage == null || !collider.transform.IsChildOf(_stage.transform));
            _drawnCollider[id] = drawn;
            return drawn;
        }

        /// <summary>The compact drawn solids (statues, houses, posts, trees, bowls) as bounds, and the stones' renderers, once.</summary>
        private void Gather()
        {
            if (_gathered || _stage == null) return;
            _gathered = true;
            _solids.Clear(); _stones.Clear();
            foreach (var piece in _stage.Pieces)
                if (piece?.Solids != null)
                    foreach (var solid in piece.Solids)
                        if (solid != null) _stones.AddRange(solid.GetComponentsInChildren<Renderer>(true));
            foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                if (CompactSolid(r, _stage)) _solids.Add(r.bounds);
        }

        /// <summary>A renderer the guard and the offline check treat as a compact solid: drawn, static art no larger than a
        /// house (the land, the water, the sky and the night's life are none of these), not the stage, not a body.</summary>
        public static bool CompactSolid(Renderer r, ArenaStage stage)
        {
            if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) return false;
            if (stage != null && r.transform.IsChildOf(stage.transform)) return false;
            if (GameplayItem(r)) return false;
            var size = r.bounds.size;
            float most = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
            if (most < 0.6f || most > 30.0f) return false;
            string name = r.gameObject.name.ToLowerInvariant();
            string parent = r.transform.parent != null ? r.transform.parent.name.ToLowerInvariant() : "";
            foreach (var skip in SkipNames)
                if (name.Contains(skip) || parent.Contains(skip)) return false;
            return true;
        }

        /// <summary>A body, a slipper, the can, a pickup or a pad: things of the game, not of the map, that move or come
        /// and go (and the player's own body stands at the hand-back's last pose). The guard and the check skip them.</summary>
        public static bool GameplayItem(Component c) =>
            c.GetComponentInParent<CharacterMotor>() != null || c.GetComponentInParent<Camera>() != null || c.GetComponentInParent<Slipper>() != null
            || c.GetComponentInParent<Lata>() != null || c.GetComponentInParent<ArenaStaminaPickup>() != null || c.GetComponentInParent<ArenaSpeedPad>() != null
            || c.GetComponentInParent<PayoGeyser>() != null;

        private static readonly string[] SkipNames =
            { "land", "water", "river", "weir", "inlet", "sky", "cloud", "haze", "wisp", "ray", "blade", "grass", "cover_", "flame", "fire_bowl__", "torch__", "night", "star", "firefl", "mist", "foam", "spray", "smoke", "fx", "vine", "banner", "hologram", "glow", "swiftlet", "bird" };

        // ------------------------------------------------------------------ helpers

        private static Frame Look(Vector3 eye, Vector3 look, float fov) => Look(eye, look, fov, Vector3.up);

        private static Frame Look(Vector3 eye, Vector3 look, float fov, Vector3 up)
        {
            Vector3 forward = look - eye;
            if (forward.sqrMagnitude < 1e-6f) forward = Vector3.forward;
            if (Vector3.Cross(forward, up).sqrMagnitude < 1e-6f) up = Vector3.forward;
            return new Frame { Eye = eye, Rotation = Quaternion.LookRotation(forward, up), Fov = fov };
        }

        private static Frame Toward(Vector3 eye, float bearing, float pitch, float fov)
        {
            float p = pitch * Mathf.Deg2Rad;
            Vector3 flat = ArenaStageMesh.Direction(bearing);
            return new Frame { Eye = eye, Rotation = Quaternion.LookRotation(flat * Mathf.Cos(p) + Vector3.up * Mathf.Sin(p), Vector3.up), Fov = fov };
        }

        private static float Bearing(Vector3 v) => Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
        private static float Elevation(Vector3 v) => Mathf.Asin(Mathf.Clamp(v.normalized.y, -1.0f, 1.0f)) * Mathf.Rad2Deg;

        private static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3.0f - 2.0f * t);
        }

        private static float Smoother(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * t * (t * (t * 6.0f - 15.0f) + 10.0f);
        }

        /// <summary>A camera move's ease: mostly even, soft at the start and the end, and past 1 it keeps drifting gently
        /// (a shot carries on under the hand-back instead of stopping dead before it).</summary>
        private static float Ease(float u)
        {
            if (u <= 1.0f) return Mathf.Lerp(Mathf.Clamp01(u), Smooth(u), 0.65f);
            return 1.0f + (u - 1.0f) * 0.35f;
        }
    }
}
