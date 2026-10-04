using System;
using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// The Arena's stage: a fixed set of box pieces that rearranges between a few layouts, one
    /// per round (docs/ARENA_MAP_BRIEF.md, "The design for rotation, the fall and the pads").
    ///
    /// NOTHING IS SENT. The layout is a pure function of the match id and the round number, both
    /// of which every peer already holds (`SyncWorld`, 5 Hz and the late-join snapshot), and the
    /// stage polls it every frame: a dropped packet or a late join cannot leave a peer on the
    /// wrong stage.
    ///
    /// TWO HIERARCHIES. The COLLIDER pieces snap to the wanted layout the frame it changes (the
    /// change happens in a break, when nothing simulates). The VISUAL pieces travel from the old
    /// layout to the new one during the break, on the break's shared clock
    /// (`SharedUltimatePhase.Now` against `HalftimePresentation.Began`): `Time.timeScale` is 0 in
    /// a break, so `deltaTime` and `Time.time` do not move. Outside a break, and on a late join,
    /// the visuals snap.
    ///
    /// The layout data is written by `ArenaSceneBuilder` from tools/arena_greybox_layout.json.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class ArenaStage : MonoBehaviour
    {
        /// <summary>Null on every other map.</summary>
        public static ArenaStage Instance { get; private set; }

        /// <summary>The ordinary break on this map, in place of 3.5 s. Halftime stays 10 s.</summary>
        public const float BreakSeconds = 8.0f;

        /// <summary>A body below this has fallen off the stage (the host catches it). Above
        /// y -5, where `MatchRpc.AcceptMove` refuses a pose.</summary>
        public const float CatchY = -3.0f;

        /// <summary>The visuals are still this long before a break ends.</summary>
        private const float SettleSeconds = 1.4f;
        /// <summary>An ordinary break's travel starts this long in; halftime's replay comes first,
        /// so its travel takes the last seconds before the settle.</summary>
        private const float TravelLead = 0.6f, HalftimeTravelSeconds = 3.2f;
        /// <summary>Each piece moves for this share of the travel; the rest is the stagger.</summary>
        private const float PieceShare = 0.5f;
        /// <summary>How far in from an edge `TryNearestStandable` answers.</summary>
        private const float StandInset = 1.0f;

        [Serializable]
        public struct Pose
        {
            public bool Exists;
            public Vector3 Position;
            /// <summary>The box's own x, y, z before rotation.</summary>
            public Vector3 Size;
            public Vector3 Euler;
        }

        [Serializable]
        public sealed class Piece
        {
            public string Id;
            public Transform Collider;
            public Transform Visual;
            /// <summary>One per layout.</summary>
            public Pose[] Poses;
        }

        [Serializable]
        public sealed class Layout
        {
            public string Name;
            /// <summary>The can's floor height at the centre, metres above the stage top.</summary>
            public float CanHeight;
            /// <summary>This layout's pads and pickups; active only with it.</summary>
            public GameObject Features;
        }

        public Piece[] Pieces = Array.Empty<Piece>();
        public Layout[] Layouts = Array.Empty<Layout>();
        /// <summary>How far a leaving piece sinks and an arriving one rises from, metres.</summary>
        public float PitDepth = 8.0f;

        public int LayoutCount => Layouts != null ? Layouts.Length : 0;

        /// <summary>The layout whose COLLIDERS are live.</summary>
        public int Applied { get; private set; } = -1;

        /// <summary>The applied layout's floor height under the can.</summary>
        public float CanHeight => Applied >= 0 && Applied < LayoutCount ? Layouts[Applied].CanHeight : 0.0f;

        /// <summary>True while the visuals are still on their way to the applied layout.</summary>
        public bool Travelling { get; private set; }

        public event Action<int> LayoutApplied;

        private int _shown = -1;

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

        /// <summary>Snap the colliders to that round's layout NOW. The visuals follow in
        /// `LateUpdate`: they travel if a break is playing, and snap otherwise.</summary>
        public void ApplyForRound(int round)
        {
            int count = LayoutCount;
            if (count == 0) return;

            int layout = LayoutFor(MatchId(), round, count);
            if (layout == Applied) return;

            Applied = layout;
            foreach (var piece in Pieces)
            {
                if (piece == null || piece.Collider == null || piece.Poses == null || layout >= piece.Poses.Length) continue;

                var pose = piece.Poses[layout];
                if (pose.Exists) Place(piece.Collider, pose.Position, Quaternion.Euler(pose.Euler), pose.Size);
                if (piece.Collider.gameObject.activeSelf != pose.Exists) piece.Collider.gameObject.SetActive(pose.Exists);
            }

            Physics.SyncTransforms();
            LayoutApplied?.Invoke(layout);
        }

        private void Update() => ApplyForRound(WantedRound());

        private void LateUpdate()
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

            float duration = hp.Duration;
            float end = duration - SettleSeconds;
            float start = hp.IsHalftime ? Mathf.Max(TravelLead, end - HalftimeTravelSeconds) : TravelLead;
            if (end <= start) return false;

            float age = (float)(SharedUltimatePhase.Now - hp.Began);
            float u = (age - start) / (end - start);
            if (u >= 1.0f) return false;

            u = Mathf.Max(0.0f, u);
            Travelling = true;
            _shown = -1;
            SetFeatures(-1);

            int n = Pieces.Length;
            for (int i = 0; i < n; i++)
            {
                var piece = Pieces[i];
                if (piece == null || piece.Visual == null || piece.Poses == null
                    || from >= piece.Poses.Length || to >= piece.Poses.Length) continue;

                // Staggered in list order: each piece moves for PieceShare of the window.
                float offset = n > 1 ? (1.0f - PieceShare) * i / (n - 1) : 0.0f;
                float t = Mathf.Clamp01((u - offset) / PieceShare);
                float e = t * t * (3.0f - 2.0f * t);

                var a = piece.Poses[from];
                var b = piece.Poses[to];
                bool visible = a.Exists || b.Exists;

                if (a.Exists && b.Exists)
                {
                    Place(piece.Visual, Vector3.Lerp(a.Position, b.Position, e),
                          Quaternion.Slerp(Quaternion.Euler(a.Euler), Quaternion.Euler(b.Euler), e),
                          Vector3.Lerp(a.Size, b.Size, e));
                }
                else if (a.Exists)
                {
                    // Leaving: down into the pit, and gone once it is under.
                    Place(piece.Visual, a.Position + Vector3.down * (PitDepth * e), Quaternion.Euler(a.Euler), a.Size);
                    visible = e < 1.0f;
                }
                else if (b.Exists)
                {
                    // Arriving: up out of the pit.
                    Place(piece.Visual, b.Position + Vector3.down * (PitDepth * (1.0f - e)), Quaternion.Euler(b.Euler), b.Size);
                    visible = e > 0.0f;
                }

                if (piece.Visual.gameObject.activeSelf != visible) piece.Visual.gameObject.SetActive(visible);
            }

            return true;
        }

        private void ShowApplied()
        {
            Travelling = false;
            _shown = Applied;
            if (Applied < 0) return;

            foreach (var piece in Pieces)
            {
                if (piece == null || piece.Visual == null || piece.Poses == null || Applied >= piece.Poses.Length) continue;

                var pose = piece.Poses[Applied];
                if (pose.Exists) Place(piece.Visual, pose.Position, Quaternion.Euler(pose.Euler), pose.Size);
                if (piece.Visual.gameObject.activeSelf != pose.Exists) piece.Visual.gameObject.SetActive(pose.Exists);
            }

            SetFeatures(Applied);
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

        private void Place(Transform piece, Vector3 position, Quaternion rotation, Vector3 size)
        {
            piece.SetPositionAndRotation(transform.TransformPoint(position), transform.rotation * rotation);
            piece.localScale = size;
        }

        /// <summary>
        /// A point on the applied layout's walkable top nearest in plan to `from`, 1 m in from
        /// the piece's edges. Flat pieces only: a ramp is never an answer.
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
                if (piece == null || piece.Poses == null || Applied >= piece.Poses.Length) continue;

                var pose = piece.Poses[Applied];
                if (!pose.Exists) continue;
                if (Mathf.Abs(Mathf.DeltaAngle(pose.Euler.x, 0.0f)) > 0.5f || Mathf.Abs(Mathf.DeltaAngle(pose.Euler.z, 0.0f)) > 0.5f) continue;

                // Boxes are yawed in quarter turns only; a quarter turn swaps the plan extents.
                float quarter = Mathf.Abs(Mathf.DeltaAngle(pose.Euler.y, 0.0f));
                bool straight = quarter < 0.5f || quarter > 179.5f;
                bool turned = Mathf.Abs(quarter - 90.0f) < 0.5f;
                if (!straight && !turned) continue;

                float halfX = Mathf.Max(0.0f, (turned ? pose.Size.z : pose.Size.x) * 0.5f - StandInset);
                float halfZ = Mathf.Max(0.0f, (turned ? pose.Size.x : pose.Size.z) * 0.5f - StandInset);
                float x = Mathf.Clamp(local.x, pose.Position.x - halfX, pose.Position.x + halfX);
                float z = Mathf.Clamp(local.z, pose.Position.z - halfZ, pose.Position.z + halfZ);
                float distance = (x - local.x) * (x - local.x) + (z - local.z) * (z - local.z);
                if (distance >= best) continue;

                best = distance;
                found = true;
                point = transform.TransformPoint(new Vector3(x, pose.Position.y + pose.Size.y * 0.5f, z));
            }

            return found;
        }
    }
}
