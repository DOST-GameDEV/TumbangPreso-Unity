using System;
using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// The Arena's stage: round platforms (discs, rings, arcs) joined by ramps and bridges, which
    /// rearrange between a few layouts, one per round (docs/ARENA_MAP_BRIEF.md, "The design for
    /// rotation, the fall and the pads"). The numbers are tools/arena_layouts.json, read by
    /// `ArenaSceneBuilder`; a piece keeps its id across the layouts it is in.
    ///
    /// NOTHING IS SENT. The layout is a pure function of the match id and the round number, both
    /// of which every peer already holds (`SyncWorld`, 5 Hz and the late-join snapshot), and the
    /// stage polls it every frame: a dropped packet or a late join cannot leave a peer on the
    /// wrong stage.
    ///
    /// COLLIDERS SNAP. Each layout has its own set of mesh colliders (`Layout.Colliders`, one
    /// exact prism per piece, `ArenaStageMesh`). The wanted layout's set is switched on and the
    /// rest off the frame it changes, which is in a break, when nothing simulates.
    ///
    /// VISUALS TRAVEL, on the break's shared clock (`SharedUltimatePhase.Now` against
    /// `HalftimePresentation.Began`): `Time.timeScale` is 0 in a break, so `deltaTime` and
    /// `Time.time` do not move. First the NEXT layout stands over the stage as a hologram
    /// (`Piece.Holograms`, a second set of renderers whose material takes `_Appear` and `_Solid`),
    /// then the pieces go to it one after another: a piece in both layouts changes shape (its
    /// mesh is rebuilt each frame between its two sets of numbers), a piece leaving sinks into
    /// the shaft, a piece arriving rises out of it, and each piece's hologram turns solid as the
    /// piece arrives. Outside a break, and on a late join, the visuals snap.
    ///
    /// THE BREAK IS A SHOW IN THREE BEATS (owner, 2026-10-05: "map transformation is so dull,
    /// theres no emphasis on it"): the alarm, the move, the reveal. This file owns the beats'
    /// times (`BreakBeats`) and where each piece is (`MoveOf`); `ArenaShow` puts the light, the
    /// sound and the shake on them, and `ArenaBreakCamera` cuts to them.
    ///
    /// HALFTIME IS TWO ACTS (owner, 2026-10-05: "halftime replay is interfereing with the
    /// transformation animation"): first the halftime package every map has (replay, then
    /// standings), with this stage held on the layout of the round just played, colliders and
    /// visuals, and nothing of the show; THEN the same 8 s show. The show's clock is
    /// `HalftimePresentation.StageShowBegan`, read here by `TryBreak` and nowhere else. A REPLAY
    /// is drawn through the live stage, so while one is up the stage stands in the layout of
    /// the clip's own round (`BeginReplay`).
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class ArenaStage : MonoBehaviour
    {
        /// <summary>Null on every other map.</summary>
        public static ArenaStage Instance { get; private set; }

        /// <summary>The ordinary break on this map, in place of 3.5 s: the show. Halftime is its
        /// own 10 s and then this (`HalftimePresentation.DurationFor`).</summary>
        public const float BreakSeconds = 8.0f;

        /// <summary>How much of this map's halftime is the package (replay to 5.8 s, then
        /// standings) before the show takes the stage. The one number to trim the standings by.</summary>
        public const float HalftimeLeadSeconds = HalftimePresentation.HalftimeDuration;

        /// <summary>
        /// THE FALL IS A REAL FALL (owner, 2026-10-05: "falling off threshold is too high, you
        /// need to fall further"). Three heights, top to bottom, each from the map:
        ///   `CatchLine`  the data's `catchY` (about -22): the host's drone takes a body under it;
        ///   `MoveFloor`  (about -40): `MatchRpc.AcceptMove` believes an owner's pose down to here
        ///                on THIS map (`MatchRpc.MoveFloorY`, -5 everywhere else);
        ///   `KillPlaneY` (about -60): the `KillPlane`, the last resort, and the walls' feet.
        /// The shaft is open to y -80 and its first ledge is at y -26 at radius 37.6, outside
        /// the walls (22 m, 31 m at a corner), so nothing is in the way of a body down to there.
        ///
        /// ⚠️ THE CATCH MUST STAY WELL ABOVE THE MOVE FLOOR. The host sees a remote body only
        /// where its last ACCEPTED pose put it; under the floor every pose is refused and
        /// answered with the stale one, so the owner is pulled back up and falls again while the
        /// host's copy hangs above the line and the drone never comes. `CatchMargin` is what a
        /// body at terminal speed (25 m/s) covers in half a second of lost poses.
        /// </summary>
        public const float DefaultCatchY = -22.0f, DefaultMoveFloorY = -40.0f, DefaultKillPlaneY = -60.0f, CatchMargin = 12.0f;

        /// <summary>A SLIPPER is taken sooner than a body: just under the lowest deck (its
        /// underside is y -2.2), far above `Balance.VoidY` (-12), where the slipper's own flight
        /// would otherwise put it back on its mark before `ArenaFallRecovery` could start the
        /// delayed return the design asks for.</summary>
        public const float SlipperCatchY = -4.5f;

        /// <summary>`MatchRpc.AcceptMove` refuses an owner's pose under this height, here and now.</summary>
        public static float MoveFloorY => Net.MatchRpc.MoveFloorY;

        /// <summary>The lowest the catch can be on this stage, whatever the data says.</summary>
        public static float LowestCatchY => (Instance != null ? Instance.MoveFloor : DefaultMoveFloorY) + CatchMargin;

        /// <summary>A body below this has fallen off the stage (the host catches it). From the
        /// layout data, held above `LowestCatchY`.</summary>
        public static float CatchY => Instance != null ? Mathf.Max(Instance.CatchLine, LowestCatchY) : DefaultCatchY;

        /// <summary>How far under the lowest underside a body is "in the shaft".</summary>
        public const float ShaftFallClearance = 1.0f;

        /// <summary>True for a body dropping through the shaft, under every deck, before the
        /// drone has it: what the fall camera, the tumble and the wind streaks read. Every peer.</summary>
        public static bool IsShaftFall(CharacterMotor who) =>
            Instance != null && who != null && !who.IsGrounded && !who.IsEdgeRecovering
            && who.transform.position.y < Instance.LowestUnderside - ShaftFallClearance;

        /// <summary>The visuals are still this long before a break ends: the reveal's beat.</summary>
        private const float SettleSeconds = 1.8f;
        /// <summary>The hologram comes up this long into a break, over `HologramFade`, and stands
        /// alone for `HologramSeconds` before the first piece moves: the alarm's beat.</summary>
        private const float HologramLead = 0.3f, HologramFade = 0.5f, HologramSeconds = 2.3f;
        /// <summary>Each piece moves for this share of the travel; the rest is the stagger
        /// between one piece's lock and the next's.</summary>
        public const float PieceShare = 0.42f;
        /// <summary>How far in from an edge `TryNearestStandable` answers.</summary>
        private const float StandInset = 1.0f;

        private static readonly int SolidId = Shader.PropertyToID("_Solid");
        private static readonly int AppearId = Shader.PropertyToID("_Appear");

        public enum ShapeKind { Disc, Ring, Arc, Ramp }

        /// <summary>
        /// One piece in one layout. Round kinds: `R0`..`R1` are the radii (a disc's `R0` is 0),
        /// `A0` the bearing the sweep starts at and `Sweep` its degrees clockwise (360 for a disc
        /// and a ring), `Top` the walking surface. A ramp: `A0` is its bearing, it runs from true
        /// distance `R0` at height `Top` to `R1` at height `Top1`, `Width` across.
        /// </summary>
        [Serializable]
        public struct Shape
        {
            public bool Exists;
            public ShapeKind Kind;
            /// <summary>Only a jump pad reaches it: never a place to set a fallen body down.</summary>
            public bool Bonus;
            public float R0, R1, A0, Sweep, Top, Top1, Width, Thick;

            public bool IsRamp => Kind == ShapeKind.Ramp;
            public bool Full => Sweep >= 359.99f;
            public float Inner => Mathf.Max(0.0f, Mathf.Min(R0, R1));
            public float Outer => Mathf.Max(R0, R1);

            /// <summary>True where the piece's top covers the plan point, `inset` in from every edge.</summary>
            public bool Contains(float x, float z, float inset)
            {
                if (!Exists) return false;

                float r = Mathf.Sqrt(x * x + z * z);
                if (r > Outer - inset) return false;
                if (Inner > 0.01f && r < Inner + inset) return false;

                if (IsRamp)
                {
                    Vector3 along = ArenaStageMesh.Direction(A0);
                    float forward = x * along.x + z * along.z, side = x * along.z - z * along.x;
                    return forward > 0.0f && Mathf.Abs(side) <= Width * 0.5f - inset;
                }

                if (Full) return true;

                float off = Mathf.Repeat(Mathf.Atan2(x, z) * Mathf.Rad2Deg - A0, 360.0f);
                if (off > Sweep) return false;
                return off * Mathf.Deg2Rad * r >= inset && (Sweep - off) * Mathf.Deg2Rad * r >= inset;
            }

            /// <summary>The top's height over a plan point the piece covers.</summary>
            public float HeightAt(float x, float z)
            {
                if (!IsRamp || Mathf.Abs(R1 - R0) < 1e-4f) return Top;
                return Mathf.Lerp(Top, Top1, Mathf.Clamp01((Mathf.Sqrt(x * x + z * z) - R0) / (R1 - R0)));
            }

            /// <summary>The point of a round piece's top nearest in plan to (x, z), `inset` in
            /// from every edge. A piece narrower than twice the inset answers on its centre line.</summary>
            public Vector3 Nearest(float x, float z, float inset)
            {
                float low = Inner > 0.01f ? Inner + inset : 0.0f, high = Outer - inset;
                if (low > high) low = high = (Inner + Outer) * 0.5f;

                float r = Mathf.Sqrt(x * x + z * z);
                float bearing = r > 1e-4f ? Mathf.Atan2(x, z) * Mathf.Rad2Deg : A0 + Sweep * 0.5f;
                if (Full) return ArenaStageMesh.Direction(bearing) * Mathf.Clamp(r, low, high) + Vector3.up * Top;

                float margin = inset / Mathf.Max((low + high) * 0.5f, 0.1f) * Mathf.Rad2Deg;
                float first = margin, last = Sweep - margin;
                if (first > last) first = last = Sweep * 0.5f;

                float off = Mathf.Repeat(bearing - A0, 360.0f);
                // Past the sweep: whichever end is nearer round the circle.
                if (off > Sweep) off = off - Sweep < 360.0f - off ? Sweep : 0.0f;
                if (off >= first && off <= last) return ArenaStageMesh.Direction(bearing) * Mathf.Clamp(r, low, high) + Vector3.up * Top;

                // Beside an end: the nearest point of that end's inset line.
                Vector3 edge = ArenaStageMesh.Direction(A0 + Mathf.Clamp(off, first, last));
                return edge * Mathf.Clamp(x * edge.x + z * edge.z, low, high) + Vector3.up * Top;
            }

            /// <summary>The shape `t` of the way from `a` to `b`: both round, or both ramps.</summary>
            public static Shape Lerp(in Shape a, in Shape b, float t)
            {
                Shape s = b;
                float from = a.A0;
                // A whole ring has no start: it opens from opposite the middle of the arc it becomes.
                if (!a.IsRamp && a.Full && !b.Full) from = b.A0 + b.Sweep * 0.5f - 180.0f;
                float to = !b.IsRamp && b.Full && !a.Full ? a.A0 + a.Sweep * 0.5f - 180.0f : b.A0;

                s.R0 = Mathf.Lerp(a.R0, b.R0, t);
                s.R1 = Mathf.Lerp(a.R1, b.R1, t);
                s.A0 = from + Mathf.DeltaAngle(from, to) * t;
                s.Sweep = Mathf.Lerp(a.Sweep, b.Sweep, t);
                s.Top = Mathf.Lerp(a.Top, b.Top, t);
                s.Top1 = Mathf.Lerp(a.Top1, b.Top1, t);
                s.Width = Mathf.Lerp(a.Width, b.Width, t);
                s.Thick = Mathf.Lerp(a.Thick, b.Thick, t);
                return s;
            }

            /// <summary>True when the two are the same solid, wherever it is turned to or raised to.</summary>
            public static bool SameSolid(in Shape a, in Shape b) =>
                a.IsRamp == b.IsRamp && Near(a.R0, b.R0) && Near(a.R1, b.R1) && Near(a.Sweep, b.Sweep)
                && Near(a.Top1 - a.Top, b.Top1 - b.Top) && Near(a.Width, b.Width) && Near(a.Thick, b.Thick);

            public static bool Same(in Shape a, in Shape b) =>
                SameSolid(a, b) && Near(a.Top, b.Top) && (Mathf.Abs(Mathf.DeltaAngle(a.A0, b.A0)) < 0.01f || (!a.IsRamp && a.Full));

            private static bool Near(float a, float b) => Mathf.Abs(a - b) < 0.001f;
        }

        [Serializable]
        public sealed class Piece
        {
            public string Id;
            /// <summary>One per layout.</summary>
            public Shape[] Shapes;
            /// <summary>One per layout, null where the piece is absent: `stage_LAYOUT_ID`, the
            /// piece as it stands in that layout, in the stage's frame.</summary>
            public GameObject[] Solids;
            /// <summary>One per layout, null where absent: the same shape in the hologram material.</summary>
            public GameObject[] Holograms;
            /// <summary>One per layout: true where the solid is an art model, which cannot change
            /// shape. It turns and rises if the two are the same solid, else one sinks and the other rises.</summary>
            public bool[] Authored;
        }

        [Serializable]
        public sealed class Layout
        {
            public string Name;
            /// <summary>The can's floor height at the centre.</summary>
            public float CanHeight;
            /// <summary>This layout's mesh colliders; active only with it.</summary>
            public GameObject Colliders;
            /// <summary>This layout's pads and pickups; active only with it.</summary>
            public GameObject Features;
        }

        public Piece[] Pieces = Array.Empty<Piece>();
        public Layout[] Layouts = Array.Empty<Layout>();
        /// <summary>The data's `catchY`. Read it through `CatchY`, which holds it above `LowestCatchY`.
        /// ⚠️ NOT THE OLD `CatchHeight`: a scene built before the deeper fall saved -4.5 under
        /// that name, and this one's default is what such a scene plays with until it is rebuilt.</summary>
        public float CatchLine = DefaultCatchY;
        /// <summary>What `MatchRpc.MoveFloorY` is while this stage is loaded.</summary>
        public float MoveFloor = DefaultMoveFloorY;
        /// <summary>Where this map's `KillPlane` sits and the walls reach down to.</summary>
        public float KillPlaneY = DefaultKillPlaneY;
        /// <summary>The furthest walking edge from the can in any layout, metres: the break camera frames it.</summary>
        public float Radius = 22.0f;
        /// <summary>The lowest underside of any piece in any layout: a body in the air under it has nothing to land on.</summary>
        public float LowestUnderside = -1.0f;
        /// <summary>How far a leaving piece sinks into the shaft and an arriving one rises from, metres.</summary>
        public float SinkDepth = 30.0f;
        /// <summary>Top, side, underside: what a piece wears while it is changing shape.</summary>
        public Material[] PieceMaterials = Array.Empty<Material>();

        public int LayoutCount => Layouts != null ? Layouts.Length : 0;

        /// <summary>The layout whose COLLIDERS are live.</summary>
        public int Applied { get; private set; } = -1;

        /// <summary>The applied layout's floor height under the can.</summary>
        public float CanHeight => Applied >= 0 && Applied < LayoutCount ? Layouts[Applied].CanHeight : 0.0f;

        /// <summary>True while the visuals are still on their way to the applied layout.</summary>
        public bool Travelling { get; private set; }

        public event Action<int> LayoutApplied;

        /// <summary>A layout to stand in whatever the round is, or -1. ⚠️ FOR A PROBE OR A FILM
        /// ONLY, AND ONLY OFFLINE: it is this peer's alone, so in a session it would put this peer
        /// on a different floor from everyone else.</summary>
        [NonSerialized] public int HoldLayout = -1;

        private int _shown = -1, _replay = -1;
        private Renderer[][][] _hologramRenderers;
        private GameObject[] _morphs;
        private Mesh[] _morphMeshes;
        private MaterialPropertyBlock _block;

        private void Awake()
        {
            Instance = this;
            AIController.EdgeSense = true;
            Net.MatchRpc.MoveFloorY = MoveFloor;
            DeepenTheFall();
            ApplyForRound(WantedRound());
            ShowApplied();
        }

        private void OnEnable()
        {
            Instance = this;
            AIController.EdgeSense = true;
            Net.MatchRpc.MoveFloorY = MoveFloor;
        }

        private void OnDisable()
        {
            AIController.EdgeSense = false;
            Net.MatchRpc.MoveFloorY = Net.MatchRpc.DefaultMoveFloorY;
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// A scene built before the deeper fall has its `KillPlane` at y -10 and walls that end
        /// there, above the catch: a body would be sent to its spawn, or drift out under a wall
        /// where `AcceptMove` refuses it for being outside the walls. So the plane and the walls
        /// are put where this stage says, whatever the scene saved. `ArenaSceneBuilder` builds
        /// them there, and then this changes nothing. Runs before `KillPlane.Awake` (this
        /// component's order is -200), which moves the plane to its own `Height`.
        /// </summary>
        private void DeepenTheFall()
        {
            var root = transform.parent;
            if (root == null) return;

            var plane = root.GetComponentInChildren<KillPlane>(true);
            if (plane != null && plane.Height > KillPlaneY)
            {
                plane.Height = KillPlaneY;
                var at = plane.transform.position;
                plane.transform.position = new Vector3(at.x, KillPlaneY, at.z);
            }

            var bounds = root.Find("Bounds");
            if (bounds == null) return;
            float feet = KillPlaneY - 2.0f;
            foreach (var wall in bounds.GetComponentsInChildren<BoxCollider>(true))
            {
                // Unscaled boxes, as the builder makes them: only the foot is lowered.
                float y = wall.transform.position.y + wall.center.y, top = y + wall.size.y * 0.5f, bottom = y - wall.size.y * 0.5f;
                if (bottom <= feet + 0.01f) continue;
                wall.center = new Vector3(wall.center.x, (top + feet) * 0.5f - wall.transform.position.y, wall.center.z);
                wall.size = new Vector3(wall.size.x, top - feet, wall.size.z);
            }
        }

        private void OnDestroy()
        {
            AIController.EdgeSense = false;
            if (Instance == this) Instance = null;
            if (_morphMeshes != null)
                foreach (var mesh in _morphMeshes) if (mesh != null) Destroy(mesh);
        }

        /// <summary>
        /// Which layout a round is played on. Pure: the match id seeds one shuffle of the layouts
        /// and the round walks it, wrapping. A permutation has no equal neighbours and its last
        /// entry is not its first, so no layout ever follows itself, across the wrap included.
        ///
        /// Integer arithmetic only (SplitMix64), so every peer and every platform agree.
        /// </summary>
        public static int LayoutFor(long matchId, int round, int count)
        {
            if (count <= 1) return 0;
            if (round < 1) round = 1;

            Span<int> order = count <= 64 ? stackalloc int[count] : new int[count];
            for (int i = 0; i < count; i++) order[i] = i;

            ulong state = unchecked((ulong)matchId);
            for (int i = count - 1; i > 0; i--)
            {
                unchecked
                {
                    state += 0x9E3779B97F4A7C15UL;
                    ulong z = state;
                    z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                    z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                    z ^= z >> 31;
                    int j = (int)(z % (ulong)(i + 1));
                    (order[i], order[j]) = (order[j], order[i]);
                }
            }

            return order[(round - 1) % count];
        }

        /// <summary>
        /// The round whose layout the stage should hold now: the round being played, the NEXT one
        /// while the match is between rounds, and round 1 before a match or with no match object.
        /// </summary>
        private static int WantedRound()
        {
            var match = GameServices.Match;
            if (match == null || match.RoundNumber < 1) return 1;

            // Halftime's package (the replay, the standings) comes before the show: the stage
            // stays as it was for the round just played until the show takes it.
            var hp = HalftimePresentation.Instance;
            if (hp != null && hp.Active && !hp.StageShowPlaying && hp.MatchId == match.PresentationMatchId && hp.CompletedRound == match.RoundNumber)
                return match.RoundNumber;

            bool roundActive = GameServices.Round != null && GameServices.Round.RoundActive;
            return match.MatchInProgress && !roundActive ? match.RoundNumber + 1 : match.RoundNumber;
        }

        /// <summary>
        /// The id the layouts are drawn from. ⚠️ ONLINE IT IS THE TRANSPORT'S (`MatchRpc.PresentationMatchId`),
        /// the one number every peer is certain to share: the host stamps it when the lobby starts the match
        /// and every client adopts it from the start message, before the arena has even loaded. The match
        /// director's copy arrives later on a client (with the first world snapshot) and, on the host, was for a
        /// time a different number altogether (`MatchDirector.PreparePresentationMatch`): the first online test
        /// had the host on one layout and the others on another.
        /// </summary>
        private static long MatchId()
        {
            var rpc = Net.MatchRpc.Instance;
            if (NetAuthority.IsNetworked && rpc != null && rpc.PresentationMatchId != 0) return rpc.PresentationMatchId;
            return GameServices.Match != null ? GameServices.Match.PresentationMatchId : 0L;
        }

        /// <summary>Switch the colliders to that round's layout NOW. The visuals follow in
        /// `LateUpdate`: they travel if a break is playing, and snap otherwise.</summary>
        public void ApplyForRound(int round)
        {
            int count = LayoutCount;
            if (count == 0) return;

            ApplyLayout(HoldLayout >= 0 ? HoldLayout : LayoutFor(MatchId(), round, count));
        }

        private void ApplyLayout(int layout)
        {
            if (layout < 0 || layout >= LayoutCount || layout == Applied) return;

            Applied = layout;
            for (int i = 0; i < LayoutCount; i++)
            {
                var set = Layouts[i] != null ? Layouts[i].Colliders : null;
                if (set != null && set.activeSelf != (i == layout)) set.SetActive(i == layout);
            }

            Physics.SyncTransforms();
            LayoutApplied?.Invoke(layout);

            // ⚠️ BEFORE A MATCH, THE BODIES ARE SEATED AGAIN (owner, 2026-10-05: "on entablado sometimes
            // during the count down ur in the floor"). They are seated on the floor when they spawn, on the
            // layout standing then, and nothing seats them again until the round starts. The layout can now
            // change in between (the match's id is stamped before the opening), and where the new deck is
            // higher the body was left inside it through the opening and the 3, 2, 1. In a match the round's
            // own reset does this, so this is only for the time before one.
            var match = GameServices.Match;
            if (match != null && !match.MatchInProgress)
                foreach (var motor in FindObjectsByType<CharacterMotor>())
                    if (motor != null) MatchHost.SeatOnFloor(motor);
        }

        /// <summary>The map's effects (`ArenaFx`, and with it the show and the ambience) install
        /// themselves here if the scene was built before they existed.</summary>
        private void Start() => ArenaFx.Ensure();

        private void Update()
        {
            // Before the first match the id is stamped early, so the stage standing here (and the one the
            // opening builds) is the one round 1 is played on, not the layout of id 0.
            var match = GameServices.Match;
            if (match != null && match.PresentationMatchId == 0 && !match.MatchInProgress) match.PreparePresentationMatch();

            if (_replay >= 0 && PresentationClock.Held) ApplyLayout(_replay);
            else ApplyForRound(WantedRound());
        }

        /// <summary>
        /// A replay is about to be drawn through this stage (`RecordedWorldView` renders the live
        /// scene with recorded bodies): stand in the layout of the round the clip was recorded
        /// in, from the clip's own match id and round, until `EndReplay`. The visuals always;
        /// the colliders too (the replay's camera and ground marks cast against them) while the
        /// simulation is held, which is every replay a player sees.
        /// </summary>
        public void BeginReplay(long matchId, int round)
        {
            int count = LayoutCount;
            if (count == 0) return;

            _replay = HoldLayout >= 0 ? HoldLayout : LayoutFor(matchId, round, count);
            if (PresentationClock.Held) ApplyLayout(_replay);
            Show(_replay);
        }

        /// <summary>The replay is over: back to the layout the match is on, this frame.</summary>
        public void EndReplay()
        {
            if (_replay < 0) return;
            _replay = -1;
            ApplyForRound(WantedRound());
            Present();
        }

        private void LateUpdate() => Present();

        /// <summary>Pose the visuals for this moment: the break's travel, or the applied layout at rest.</summary>
        public void Present()
        {
            if (Applied < 0) return;

            if (_replay >= 0)
            {
                if (Travelling || _shown != _replay) Show(_replay);
                return;
            }

            if (TryTravel()) return;
            if (Travelling || _shown != Applied) ShowApplied();
        }

        /// <summary>
        /// The break that is playing, as the beats every part of the show reads (the stage's
        /// travel here, `ArenaShow`'s light and sound, `ArenaBreakCamera`'s cuts), all in seconds
        /// from the SHOW's start (`HalftimePresentation.StageShowBegan`: the host's `Began` stamp
        /// on an ordinary break, the end of the package at halftime), so every peer is on the
        /// same frame of the same beat, and the show is the same 8 s in both:
        ///   ALARM   `ScanStart` to `ScanEnd`: the next layout's hologram sweeps in from the can;
        ///   THE MOVE `Undock` (every moving piece jolts), then `MoveStart` to `MoveEnd`: the
        ///           pieces go one after another and each LOCKS at the end of its own window;
        ///   REVEAL  `Reveal`, just after the last lock; the stage is still from `MoveEnd` on.
        /// </summary>
        public struct BreakBeats
        {
            public int From, To;
            public bool Halftime;
            /// <summary>ARENA-INTRO. True for the match's opening (`TryOpening`): the stage is built
            /// from `Nothing`, so `From` is -1 and every reader of `Shapes[From]` checks it.</summary>
            public bool Opening;
            /// <summary>False through halftime's package, before the show: the stage is still,
            /// on the old layout, and nothing of the show is drawn or heard. `Age` is negative.</summary>
            public bool Showing;
            public float Age, Duration, ScanStart, ScanEnd, Undock, MoveStart, MoveEnd, Reveal;
            /// <summary>0 to 1 through the move.</summary>
            public float Travel => Mathf.Clamp01((Age - MoveStart) / (MoveEnd - MoveStart));
            /// <summary>The second a piece's own window (a share of the move) ends: its lock.</summary>
            public float At(float share) => MoveStart + (MoveEnd - MoveStart) * share;
        }

        /// <summary>The scan takes this long to cross the stage, and the pieces jolt this long before the first goes.</summary>
        public const float ScanSeconds = 1.4f, UndockLead = 0.2f, RevealLag = 0.15f;

        /// <summary>
        /// The break that is playing on this stage. The layouts are read off the break's own
        /// completed round, so a peer that joins in the middle of a break lands on the same
        /// frame of the travel as everyone. False outside a break, and for a break of another match.
        /// ⚠️ TRUE THROUGH HALFTIME'S PACKAGE TOO, with `Showing` false: every reader checks it.
        /// </summary>
        public bool TryBreak(out BreakBeats beats)
        {
            beats = default;
            var hp = HalftimePresentation.Instance;
            var match = GameServices.Match;
            int count = LayoutCount;
            if (hp == null || !hp.Active || match == null || hp.MatchId != match.PresentationMatchId || count == 0) return false;

            beats.From = LayoutFor(hp.MatchId, hp.CompletedRound, count);
            beats.To = LayoutFor(hp.MatchId, hp.CompletedRound + 1, count);
            beats.Halftime = hp.IsHalftime;
            beats.Showing = hp.StageShowPlaying;
            beats.Duration = hp.StageShowDuration;
            beats.MoveEnd = beats.Duration - SettleSeconds;
            beats.MoveStart = HologramLead + HologramSeconds;
            beats.ScanStart = beats.MoveStart - HologramSeconds;
            beats.ScanEnd = beats.ScanStart + ScanSeconds;
            beats.Undock = beats.MoveStart - UndockLead;
            beats.Reveal = beats.MoveEnd + RevealLag;
            beats.Age = hp.StageShowAge;
            return beats.MoveEnd > beats.MoveStart;
        }

        // ------------------------------------------------------------------ ARENA-INTRO: the opening
        //
        // THE MATCH OPENS ON AN EMPTY SHAFT AND THE STAGE IS BUILT IN FRONT OF THE PLAYERS (owner,
        // 2026-10-05: "then the arena gets built"). `ArenaIntro` owns that film and its clock; it
        // tells the stage each frame how far into the build it is (`HoldOpening`, negative before
        // the build's beat), and the stage answers with the same `BreakBeats` a break gives, from
        // `Nothing` to the applied layout, so the travel here and `ArenaShow`'s light, sound, shake
        // and drones are the break's own and nothing is written twice. The colliders are never
        // touched: round 1's layout is live from `Awake`, only what is drawn waits in the shaft.
        // A stamp two frames old is dead, so an opening that stops for any reason leaves the stage
        // standing whole on the next frame with nothing to put back.

        /// <summary>The layout a stage is built from in the opening: none. Every piece arrives.</summary>
        public const int Nothing = -1;
        /// <summary>The opening's own beats, in seconds from the build's start: shorter than a break's.</summary>
        public const float OpeningScanStart = 0.15f, OpeningScanSeconds = 1.1f, OpeningUndock = 1.3f, OpeningMoveStart = 1.5f, OpeningSettle = 1.5f;

        private static int _openingFrame = -9;
        private static float _openingAge, _openingSeconds;

        /// <summary>Called every frame of the opening by `ArenaIntro`: `age` seconds into the
        /// build (negative while the shaft is still empty), of `seconds`.</summary>
        public static void HoldOpening(float age, float seconds)
        {
            _openingFrame = Time.frameCount; _openingAge = age; _openingSeconds = seconds;
        }

        /// <summary>The opening that is playing on this stage, as a break's beats. False on every
        /// frame `ArenaIntro` did not ask for it, which is every frame outside the match's opening.</summary>
        public bool TryOpening(out BreakBeats beats)
        {
            beats = default;
            if (Time.frameCount - _openingFrame > 1 || Applied < 0 || LayoutCount == 0) return false;

            beats.Opening = true;
            beats.From = Nothing;
            beats.To = Applied;
            beats.Showing = true;
            beats.Duration = _openingSeconds;
            beats.Age = _openingAge;
            beats.ScanStart = OpeningScanStart;
            beats.ScanEnd = OpeningScanStart + OpeningScanSeconds;
            beats.Undock = OpeningUndock;
            beats.MoveStart = OpeningMoveStart;
            beats.MoveEnd = beats.Duration - OpeningSettle;
            beats.Reveal = beats.MoveEnd + RevealLag;
            return beats.MoveEnd > beats.MoveStart;
        }

        /// <summary>The show that is playing: a break's, or the opening's. What the travel here and
        /// `ArenaShow` read. Everything that is the BREAK's alone (its camera, the PA's call, the
        /// spots' chase) still reads `TryBreak`.</summary>
        public bool TryShow(out BreakBeats beats) => TryBreak(out beats) || TryOpening(out beats);

        /// <summary>How far out from the can the hologram's scan has reached, metres; past the
        /// whole stage once it is done.</summary>
        public float ScanRadius(in BreakBeats beats)
        {
            float t = Mathf.Clamp01((beats.Age - beats.ScanStart) / (beats.ScanEnd - beats.ScanStart));
            return t >= 1.0f ? float.PositiveInfinity : (Radius + 3.0f) * t * t * (3.0f - 2.0f * t);
        }

        /// <summary>The jolt of the undock, metres, `since` seconds after it: a drop and a short ring.</summary>
        public static float UndockJolt(float since) =>
            since <= 0.0f || since > 1.0f ? 0.0f : -0.24f * Mathf.Exp(-since * 6.0f) * Mathf.Sin(since * 22.0f);

        /// <summary>
        /// Pose the visuals for the break that is playing, if it is the break INTO the applied
        /// layout.
        /// </summary>
        private bool TryTravel()
        {
            if (!TryShow(out var beats) || !beats.Showing || beats.To != Applied || beats.From == beats.To) return false;
            if (beats.Age >= beats.MoveEnd) return false;

            PoseTravel(beats.From, beats.To, beats.Travel, Mathf.Clamp01((beats.Age - beats.ScanStart) / HologramFade),
                       ScanRadius(beats), UndockJolt(beats.Age - beats.Undock));
            return true;
        }

        /// <summary>What a piece does between two layouts.</summary>
        public enum Motion
        {
            /// <summary>In neither layout.</summary>
            Absent,
            /// <summary>Stands the same in both: it neither moves nor needs announcing.</summary>
            Still,
            /// <summary>Only in the old layout: down into the shaft, gathering speed.</summary>
            Leave,
            /// <summary>Only in the new one: up out of the shaft, and it slams home.</summary>
            Arrive,
            /// <summary>In both, a different shape: one mesh rebuilt between the two sets of numbers.</summary>
            Morph,
            /// <summary>In both, the same solid somewhere else: it turns about the can and rises or drops.</summary>
            Turn,
            /// <summary>In both, and it can do neither: the old one sinks, then the new one rises.</summary>
            Swap,
        }

        /// <summary>Where one piece is at one moment of the move.</summary>
        public struct PieceMove
        {
            public Motion Motion;
            /// <summary>The share of the move (0 to 1) at which this piece starts and locks.</summary>
            public float Starts, Locks;
            /// <summary>0 to 1 through this piece's own window, and the same eased (`Slam`).</summary>
            public float T, E;
            /// <summary>Which layout's solid is the one drawn now (-1: none, or the changing mesh).</summary>
            public int Shown;
            /// <summary>That solid's lift in metres and turn in degrees from where it stands.</summary>
            public float Lift, Yaw;
        }

        public Motion MotionOf(int index, int from, int to)
        {
            var piece = index >= 0 && index < Pieces.Length ? Pieces[index] : null;
            if (piece == null || piece.Shapes == null || from < Nothing || to < 0 || from >= piece.Shapes.Length || to >= piece.Shapes.Length) return Motion.Absent;

            // ARENA-INTRO: from `Nothing` no piece was there, so each one in the new layout arrives.
            var a = from >= 0 ? piece.Shapes[from] : default;
            var b = piece.Shapes[to];
            if (!a.Exists && !b.Exists) return Motion.Absent;
            if (!b.Exists) return Motion.Leave;
            if (!a.Exists) return Motion.Arrive;
            if (Shape.Same(a, b)) return Motion.Still;

            bool authored = IsAuthored(piece, from) || IsAuthored(piece, to);
            if (!authored && a.IsRamp == b.IsRamp && PieceMaterials != null && PieceMaterials.Length > 0) return Motion.Morph;
            return Shape.SameSolid(a, b) ? Motion.Turn : Motion.Swap;
        }

        private static bool Moves(Motion motion) => motion != Motion.Absent && motion != Motion.Still;

        /// <summary>True for a piece that ends the move standing in the new layout: it LOCKS. A
        /// leaving piece only goes.</summary>
        public static bool Locks(Motion motion) => Moves(motion) && motion != Motion.Leave;

        /// <summary>The piece whose lock is the last of the move (they go in list order), or -1
        /// when nothing locks: the break camera's last shot is on it.</summary>
        public int LastLocking(int from, int to)
        {
            for (int i = Pieces.Length - 1; i >= 0; i--)
                if (Locks(MotionOf(i, from, to))) return i;
            return -1;
        }

        /// <summary>
        /// One piece at `travel` (0 to 1) of the way from one layout to another. The pieces that
        /// move go one after another in list order, each for `PieceShare` of the move, and each
        /// LOCKS at the end of its own window: `ArenaShow` rings each lock at `Locks`.
        /// </summary>
        public PieceMove MoveOf(int index, int from, int to, float travel)
        {
            var move = new PieceMove { Motion = MotionOf(index, from, to), Shown = -1 };
            if (move.Motion == Motion.Absent) return move;
            if (move.Motion == Motion.Still) { move.Shown = to; move.T = move.E = 1.0f; return move; }

            int order = 0, moving = 0;
            for (int i = 0; i < Pieces.Length; i++)
            {
                if (!Moves(MotionOf(i, from, to))) continue;
                if (i < index) order++;
                moving++;
            }

            move.Starts = moving > 1 ? (1.0f - PieceShare) * order / (moving - 1) : (1.0f - PieceShare) * 0.5f;
            move.Locks = move.Starts + PieceShare;
            float t = move.T = Mathf.Clamp01((travel - move.Starts) / PieceShare);
            float e = move.E = Slam(t);

            var a = from >= 0 ? Pieces[index].Shapes[from] : default;
            var b = Pieces[index].Shapes[to];
            switch (move.Motion)
            {
                case Motion.Leave:
                    move.Shown = t >= 1.0f ? -1 : from;
                    move.Lift = -SinkDepth * Fall(t);
                    break;
                case Motion.Arrive:
                    // Out of sight until it starts: it would otherwise hang at the bottom of its rise.
                    move.Shown = t <= 0.0f ? -1 : to;
                    move.Lift = -SinkDepth * (1.0f - e);
                    break;
                case Motion.Morph:
                    move.Shown = t <= 0.0f ? from : t >= 1.0f ? to : -1;
                    break;
                case Motion.Turn:
                    move.Shown = t >= 1.0f ? to : from;
                    if (t < 1.0f) { move.Yaw = Mathf.DeltaAngle(a.A0, b.A0) * e; move.Lift = (b.Top - a.Top) * e; }
                    break;
                case Motion.Swap:
                    if (t < 0.5f) { move.Shown = from; move.Lift = -SinkDepth * Fall(t * 2.0f); }
                    else { move.Shown = to; move.Lift = -SinkDepth * (1.0f - Slam(t * 2.0f - 1.0f)); }
                    break;
            }

            return move;
        }

        /// <summary>
        /// The visuals `travel` (0 to 1) of the way from one layout to another, with the next
        /// layout's hologram `appear` (0 to 1) of the way up and swept in out to `scan` metres
        /// from the can, and every piece that has not gone yet dropped by `jolt` metres (the
        /// undock). Public so a probe or a film can pose any frame of the transformation
        /// without a break.
        /// </summary>
        public void PoseTravel(int from, int to, float travel, float appear, float scan = float.PositiveInfinity, float jolt = 0.0f)
        {
            Travelling = true;
            _shown = -1;
            SetFeatures(-1);

            int n = Pieces.Length;
            for (int i = 0; i < n; i++)
            {
                var piece = Pieces[i];
                if (piece == null || piece.Shapes == null || from >= piece.Shapes.Length || to >= piece.Shapes.Length) continue;

                var move = MoveOf(i, from, to, travel);
                if (move.Motion == Motion.Absent || move.Motion == Motion.Still)
                {
                    ShowSolid(piece, move.Shown, 0.0f, 0.0f);
                    ShowMorph(i, false);
                    ShowHologram(i, -1, 0.0f, 0.0f);
                    continue;
                }

                var a = from >= 0 ? piece.Shapes[from] : default;
                var b = piece.Shapes[to];

                // The hologram of where the piece is going: it comes up as the scan passes over
                // it, and turns solid as the piece arrives.
                float swept = b.Exists ? appear * Mathf.Clamp01((scan - b.Inner) / 2.5f) : 0.0f;
                ShowHologram(i, swept > 0.0f && move.T < 1.0f ? to : -1, move.E, swept);

                bool morph = move.Motion == Motion.Morph && move.T > 0.0f && move.T < 1.0f;
                if (morph)
                {
                    // Changing shape: one mesh, rebuilt between the two sets of numbers.
                    ShowSolid(piece, -1, 0.0f, 0.0f);
                    ArenaStageMesh.Build(Shape.Lerp(a, b, move.E), MorphMesh(i), true);
                }
                else ShowSolid(piece, move.Shown, move.Yaw, move.Lift + (move.T <= 0.0f ? jolt : 0.0f));

                ShowMorph(i, morph);
            }
        }

        private static float Fall(float t) => t * t;

        /// <summary>A piece's arrival: it gathers speed and stops dead where it locks (its
        /// speed at the end is nearly twice its average), so the lock reads as a hit.</summary>
        public static float Slam(float t) => Mathf.Lerp(t * t * (3.0f - 2.0f * t), t * t * t, 0.6f);

        /// <summary>
        /// Up to `into.Length` points along a piece's walking surface in one layout, in the
        /// WORLD, at rest (add a `PieceMove`'s lift): where its hover emitters and its lock's
        /// sparks are drawn. A disc: its middle and three round it; a ring or an arc: along its
        /// centre line; a ramp: along its length.
        /// </summary>
        public int PiecePoints(int index, int layout, Vector3[] into)
        {
            var piece = index >= 0 && index < Pieces.Length ? Pieces[index] : null;
            if (piece == null || piece.Shapes == null || layout < 0 || layout >= piece.Shapes.Length || into == null || into.Length == 0) return 0;

            var shape = piece.Shapes[layout];
            if (!shape.Exists) return 0;

            int count = 0;
            if (shape.IsRamp)
            {
                int along = Mathf.Min(into.Length, 2);
                for (int k = 0; k < along; k++)
                {
                    float r = Mathf.Lerp(shape.R0, shape.R1, (k + 1.0f) / (along + 1.0f));
                    Vector3 at = ArenaStageMesh.Direction(shape.A0) * r;
                    into[count++] = transform.TransformPoint(at + Vector3.up * shape.HeightAt(at.x, at.z));
                }
                return count;
            }

            if (shape.Inner < 0.01f)
            {
                into[count++] = transform.TransformPoint(Vector3.up * shape.Top);
                for (int k = 0; k < 3 && count < into.Length; k++)
                    into[count++] = transform.TransformPoint(ArenaStageMesh.Direction(shape.A0 + 60.0f + 120.0f * k) * (shape.Outer * 0.7f) + Vector3.up * shape.Top);
                return count;
            }

            float mid = (shape.Inner + shape.Outer) * 0.5f;
            // One point about every 7 m of arc, and the two ends of an arc always.
            int steps = Mathf.Clamp(Mathf.CeilToInt(shape.Sweep * Mathf.Deg2Rad * mid / 7.0f), shape.Full ? 4 : 2, into.Length);
            for (int k = 0; k < steps; k++)
            {
                float share = shape.Full ? (float)k / steps : (k + 0.5f) / steps;
                into[count++] = transform.TransformPoint(ArenaStageMesh.Direction(shape.A0 + shape.Sweep * share) * mid + Vector3.up * shape.Top);
            }
            return count;
        }

        private static bool IsAuthored(Piece piece, int layout) =>
            piece.Authored != null && layout < piece.Authored.Length && piece.Authored[layout];

        private void ShowApplied() => Show(Applied);

        /// <summary>The visuals at rest in one layout: the applied one, or a replay's.</summary>
        private void Show(int layout)
        {
            Travelling = false;
            _shown = layout;
            if (layout < 0) return;

            for (int i = 0; i < Pieces.Length; i++)
            {
                var piece = Pieces[i];
                if (piece == null || piece.Shapes == null || layout >= piece.Shapes.Length) continue;

                ShowSolid(piece, piece.Shapes[layout].Exists ? layout : -1, 0.0f, 0.0f);
                ShowMorph(i, false);
                ShowHologram(i, -1, 0.0f, 0.0f);
            }

            SetFeatures(layout);
        }

        /// <summary>Only that layout's solid of the piece is drawn (none for -1), turned `yaw`
        /// degrees about the can and raised `lift` metres from where it stands.</summary>
        private static void ShowSolid(Piece piece, int layout, float yaw, float lift)
        {
            if (piece.Solids == null) return;

            for (int l = 0; l < piece.Solids.Length; l++)
            {
                var solid = piece.Solids[l];
                if (solid == null) continue;

                bool on = l == layout;
                if (solid.activeSelf != on) solid.SetActive(on);
                if (on) solid.transform.SetLocalPositionAndRotation(Vector3.up * lift, Quaternion.Euler(0.0f, yaw, 0.0f));
            }
        }

        /// <summary>Only that layout's hologram of the piece is drawn (none for -1). `solid` is
        /// how far the piece itself has arrived, `appear` how far the whole hologram has come up.</summary>
        private void ShowHologram(int index, int layout, float solid, float appear)
        {
            var holograms = Pieces[index].Holograms;
            if (holograms == null) return;

            for (int l = 0; l < holograms.Length; l++)
            {
                var hologram = holograms[l];
                if (hologram == null) continue;

                bool on = l == layout;
                if (hologram.activeSelf != on) hologram.SetActive(on);
                if (!on) continue;

                if (_block == null) _block = new MaterialPropertyBlock();
                _block.SetFloat(SolidId, solid);
                _block.SetFloat(AppearId, appear);
                foreach (var renderer in HologramRenderers(index, l)) renderer.SetPropertyBlock(_block);
            }
        }

        private Renderer[] HologramRenderers(int index, int layout)
        {
            if (_hologramRenderers == null) _hologramRenderers = new Renderer[Pieces.Length][][];
            if (_hologramRenderers[index] == null) _hologramRenderers[index] = new Renderer[Pieces[index].Holograms.Length][];
            return _hologramRenderers[index][layout] ??= Pieces[index].Holograms[layout].GetComponentsInChildren<Renderer>(true);
        }

        private void ShowMorph(int index, bool on)
        {
            if (_morphs == null || _morphs[index] == null) return;
            if (_morphs[index].activeSelf != on) _morphs[index].SetActive(on);
        }

        /// <summary>The piece's changing mesh, made the first time the piece changes shape.</summary>
        private Mesh MorphMesh(int index)
        {
            if (_morphs == null)
            {
                _morphs = new GameObject[Pieces.Length];
                _morphMeshes = new Mesh[Pieces.Length];
            }

            if (_morphs[index] == null)
            {
                var go = new GameObject("Morph " + Pieces[index].Id);
                go.transform.SetParent(transform, false);
                var mesh = new Mesh { name = go.name };
                mesh.MarkDynamic();
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterials = PieceMaterials;
                _morphs[index] = go;
                _morphMeshes[index] = mesh;
            }

            return _morphMeshes[index];
        }

        /// <summary>Pads and pickups stand on the platforms, so they are off while the platforms
        /// travel (nothing simulates in a break) and on with the layout once it is still.</summary>
        private void SetFeatures(int layout)
        {
            for (int i = 0; i < LayoutCount; i++)
            {
                var features = Layouts[i] != null ? Layouts[i].Features : null;
                if (features != null && features.activeSelf != (i == layout)) features.SetActive(i == layout);
            }
        }

        /// <summary>
        /// A point on the applied layout's walkable top nearest in plan to `from`, 1 m in from
        /// the piece's edges. Discs, rings and arcs only: a ramp is never an answer, and neither
        /// is a bonus piece, which only a jump pad reaches (a body set down there could not walk
        /// back).
        ///
        /// EVERY edge of a piece is inset, not only the open ones: a point 1 m inside a shared
        /// edge is still a safe place to stand, and the stage does not have to know which edges
        /// touch. A piece under 2 m across answers along its centre line.
        /// </summary>
        public bool TryNearestStandable(Vector3 from, out Vector3 point)
        {
            point = default;
            if (Applied < 0) return false;

            Vector3 local = transform.InverseTransformPoint(from);
            float best = float.PositiveInfinity;
            bool found = false;

            foreach (var piece in Pieces)
            {
                if (piece == null || piece.Shapes == null || Applied >= piece.Shapes.Length) continue;

                var shape = piece.Shapes[Applied];
                if (!shape.Exists || shape.IsRamp || shape.Bonus) continue;

                Vector3 near = shape.Nearest(local.x, local.z, StandInset);
                float distance = (near.x - local.x) * (near.x - local.x) + (near.z - local.z) * (near.z - local.z);
                if (distance >= best) continue;

                best = distance;
                found = true;
                point = transform.TransformPoint(near);
            }

            return found;
        }
    }
}
