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
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class ArenaStage : MonoBehaviour
    {
        /// <summary>Null on every other map.</summary>
        public static ArenaStage Instance { get; private set; }

        /// <summary>The ordinary break on this map, in place of 3.5 s. Halftime stays 10 s.</summary>
        public const float BreakSeconds = 8.0f;

        /// <summary>`MatchRpc.AcceptMove` refuses an owner's pose under this height.</summary>
        public const float MoveFloorY = -5.0f;

        /// <summary>
        /// The highest the catch may be set, whatever the data says. ⚠️ A CATCH UNDER y -5 NEVER
        /// FIRES FOR A JOINING PLAYER: the host only sees a remote body where its last ACCEPTED
        /// pose put it, `AcceptMove` refuses every pose under -5 and answers each with the stale
        /// pose, so the owner is pulled back up and falls again while the host's copy hangs above
        /// the line. So the data's `catchY` is used, but never lower than this. The lowest deck's
        /// underside is y -2.2, so this still clears every deck.
        /// </summary>
        public const float LowestCatchY = -4.5f;

        /// <summary>A body below this has fallen off the stage (the host catches it). From the
        /// layout data, held above `LowestCatchY`.</summary>
        public static float CatchY => Instance != null ? Mathf.Max(Instance.CatchHeight, LowestCatchY) : LowestCatchY;

        /// <summary>The visuals are still this long before a break ends.</summary>
        private const float SettleSeconds = 1.4f;
        /// <summary>The hologram comes up this long into a break, over `HologramFade`, and stands
        /// alone for `HologramSeconds` before the first piece moves.</summary>
        private const float HologramLead = 0.3f, HologramFade = 0.5f, HologramSeconds = 2.0f;
        /// <summary>Halftime's replay comes first, so its travel takes the last seconds before the settle.</summary>
        private const float HalftimeTravelSeconds = 3.2f;
        /// <summary>Each piece moves for this share of the travel; the rest is the stagger.</summary>
        private const float PieceShare = 0.5f;
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
        /// <summary>The data's `catchY`. Read it through `CatchY`, which holds it above `LowestCatchY`.</summary>
        public float CatchHeight = LowestCatchY;
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

        private int _shown = -1;
        private Renderer[][][] _hologramRenderers;
        private GameObject[] _morphs;
        private Mesh[] _morphMeshes;
        private MaterialPropertyBlock _block;

        private void Awake()
        {
            Instance = this;
            AIController.EdgeSense = true;
            ApplyForRound(WantedRound());
            ShowApplied();
        }

        private void OnEnable()
        {
            Instance = this;
            AIController.EdgeSense = true;
        }

        private void OnDisable()
        {
            AIController.EdgeSense = false;
            if (Instance == this) Instance = null;
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

            bool roundActive = GameServices.Round != null && GameServices.Round.RoundActive;
            return match.MatchInProgress && !roundActive ? match.RoundNumber + 1 : match.RoundNumber;
        }

        private static long MatchId() => GameServices.Match != null ? GameServices.Match.PresentationMatchId : 0L;

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
        }

        private void Update() => ApplyForRound(WantedRound());

        private void LateUpdate() => Present();

        /// <summary>Pose the visuals for this moment: the break's travel, or the applied layout at rest.</summary>
        public void Present()
        {
            if (Applied < 0) return;

            if (TryTravel()) return;
            if (Travelling || _shown != Applied) ShowApplied();
        }

        /// <summary>
        /// Pose the visuals for the break that is playing, if it is the break INTO the applied
        /// layout. The old layout is read off the break's own completed round, so a peer that
        /// joins in the middle of a break lands on the same frame of the travel as everyone.
        /// </summary>
        private bool TryTravel()
        {
            var hp = HalftimePresentation.Instance;
            var match = GameServices.Match;
            if (hp == null || !hp.Active || match == null || hp.MatchId != match.PresentationMatchId) return false;

            int count = LayoutCount;
            int from = LayoutFor(hp.MatchId, hp.CompletedRound, count);
            int to = LayoutFor(hp.MatchId, hp.CompletedRound + 1, count);
            if (to != Applied || from == to) return false;

            float end = hp.Duration - SettleSeconds;
            float start = HologramLead + HologramSeconds;
            if (hp.IsHalftime) start = Mathf.Max(start, end - HalftimeTravelSeconds);
            if (end <= start) return false;

            float age = (float)(SharedUltimatePhase.Now - hp.Began);
            if (age >= end) return false;

            PoseTravel(from, to, Mathf.Clamp01((age - start) / (end - start)),
                       Mathf.Clamp01((age - (start - HologramSeconds)) / HologramFade));
            return true;
        }

        /// <summary>
        /// The visuals `travel` (0 to 1) of the way from one layout to another, with the next
        /// layout's hologram `appear` (0 to 1) of the way up. Public so a probe or a film can
        /// pose any frame of the transformation without a break.
        /// </summary>
        public void PoseTravel(int from, int to, float travel, float appear)
        {
            Travelling = true;
            _shown = -1;
            SetFeatures(-1);

            int n = Pieces.Length;
            for (int i = 0; i < n; i++)
            {
                var piece = Pieces[i];
                if (piece == null || piece.Shapes == null || from >= piece.Shapes.Length || to >= piece.Shapes.Length) continue;

                var a = piece.Shapes[from];
                var b = piece.Shapes[to];

                // A piece that stands the same in both layouts neither moves nor needs announcing.
                if (a.Exists && b.Exists && Shape.Same(a, b))
                {
                    ShowSolid(piece, to, 0.0f, 0.0f);
                    ShowMorph(i, false);
                    ShowHologram(i, -1, 0.0f, 0.0f);
                    continue;
                }

                // Staggered in list order: each piece moves for PieceShare of the window.
                float offset = n > 1 ? (1.0f - PieceShare) * i / (n - 1) : 0.0f;
                float t = Mathf.Clamp01((travel - offset) / PieceShare);
                float e = t * t * (3.0f - 2.0f * t);

                ShowHologram(i, b.Exists && appear > 0.0f && t < 1.0f ? to : -1, e, appear);

                bool morph = false;
                if (t <= 0.0f) ShowSolid(piece, a.Exists ? from : -1, 0.0f, 0.0f);
                else if (t >= 1.0f) ShowSolid(piece, b.Exists ? to : -1, 0.0f, 0.0f);
                else if (a.Exists && b.Exists)
                {
                    bool authored = IsAuthored(piece, from) || IsAuthored(piece, to);
                    if (!authored && a.IsRamp == b.IsRamp && PieceMaterials != null && PieceMaterials.Length > 0)
                    {
                        // Changing shape: one mesh, rebuilt between the two sets of numbers.
                        morph = true;
                        ShowSolid(piece, -1, 0.0f, 0.0f);
                        ArenaStageMesh.Build(Shape.Lerp(a, b, e), MorphMesh(i), true);
                    }
                    else if (Shape.SameSolid(a, b))
                    {
                        // The same solid somewhere else: it turns about the can and rises or drops.
                        ShowSolid(piece, from, Mathf.DeltaAngle(a.A0, b.A0) * e, (b.Top - a.Top) * e);
                    }
                    else if (t < 0.5f) ShowSolid(piece, from, 0.0f, -SinkDepth * Fall(t * 2.0f));
                    else ShowSolid(piece, to, 0.0f, -SinkDepth * Fall(2.0f - t * 2.0f));
                }
                // Leaving: down into the shaft, gathering speed. Arriving: up out of it, slowing.
                else if (a.Exists) ShowSolid(piece, from, 0.0f, -SinkDepth * Fall(t));
                else ShowSolid(piece, b.Exists ? to : -1, 0.0f, -SinkDepth * Fall(1.0f - t));

                ShowMorph(i, morph);
            }
        }

        private static float Fall(float t) => t * t;

        private static bool IsAuthored(Piece piece, int layout) =>
            piece.Authored != null && layout < piece.Authored.Length && piece.Authored[layout];

        private void ShowApplied()
        {
            Travelling = false;
            _shown = Applied;
            if (Applied < 0) return;

            for (int i = 0; i < Pieces.Length; i++)
            {
                var piece = Pieces[i];
                if (piece == null || piece.Shapes == null || Applied >= piece.Shapes.Length) continue;

                ShowSolid(piece, piece.Shapes[Applied].Exists ? Applied : -1, 0.0f, 0.0f);
                ShowMorph(i, false);
                ShowHologram(i, -1, 0.0f, 0.0f);
            }

            SetFeatures(Applied);
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
