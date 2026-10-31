using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ TELEPORT, DIRECTION A, "MOONLIT MOTHS" (`PhaisterLook.Style.A`). Her body is drawn out toward where she is going,
    /// comes apart into ONE ribbon of large moths that streams over the court and packs back into her at the far end, and
    /// the pose she left drips away where she stood.
    ///
    /// Built from two references on the board (`drafts/phref/BOARD.md` T1 and T2): Pataya's RTVFX Sketch #35 for the beat
    /// (the body pulled to the target, a ghost left behind that drips and erodes) and Black Cat Wizard's Sketch #26 for the
    /// construction (a whole swarm as one static mesh, every insect's path worked out in the vertex program).
    ///
    /// | Beat | Time | What |
    /// |---|---|---|
    /// | Drawn | 0.00 to 0.09 | her pose is baked once into a ghost and pulled toward the destination; her body is hidden |
    /// | Ribbon | 0.03 to 0.50 | eight big moths leave her silhouette in a typed order and stream along one bowed arc, twisting clockwise about it |
    /// | Knit | 0.30 to 0.50 | each packs into her at its own height; her body shows again at `Reveal` |
    /// | Drip | 0.10 to 1.20 | the ghost drips, pools on the ground and is eaten from the top down |
    /// | Stragglers | 0.45 to 1.30 | three moths fly on up off her and are eaten away |
    ///
    /// ⚠️ THE MOTHS ARE BIG AND FEW ON PURPOSE. The old swarm's own note: 28 small props "read as confetti" at 6 m. Eight
    /// moths of 0.9 m span, flying one arc, read as one shape: a ribbon.
    ///
    /// ⚠️ EVERY MOTH IS A TYPED ROW (the owner's standing rule for her insects), baked into the mesh.
    ///
    /// Cost: four renderers. The moths are one draw (24 cards, 48 triangles, one material); the path two ribbons (about 1,000 triangles); the ghost is her own triangle
    /// count in two passes, one material.
    /// </summary>
    public sealed class PhaisterMothRibbon : MonoBehaviour, IVfxTimeline
    {
        public const float Travel = 0.34f, Reveal = 0.36f, Life = 1.35f;

        // ⚠️ TYPED BY HAND, 2026-10-30 (round 4: eight, not fourteen; the coordinator's "big and few, wing shapes legible").
        // Leaves top first and front first, so the silhouette reads as pulled out from the head.
        private static readonly PhaisterLook.MothRow[] Rows =
        {
            new PhaisterLook.MothRow { Delay = 0.000f, LaneSide =  0.08f, LaneUp =  0.18f, Phase =   0f, LeaveHeight = 1.45f, LeaveSide =  0.05f, KnitHeight = 1.40f, Size = 1.10f, WingHz = 4.4f },
            new PhaisterLook.MothRow { Delay = 0.025f, LaneSide = -0.22f, LaneUp =  0.05f, Phase =  70f, LeaveHeight = 1.20f, LeaveSide = -0.18f, KnitHeight = 1.18f, Size = 1.00f, WingHz = 5.0f },
            new PhaisterLook.MothRow { Delay = 0.045f, LaneSide =  0.26f, LaneUp =  0.10f, Phase = 150f, LeaveHeight = 1.00f, LeaveSide =  0.20f, KnitHeight = 0.98f, Size = 1.05f, WingHz = 3.8f, Straggler = true },
            new PhaisterLook.MothRow { Delay = 0.065f, LaneSide = -0.06f, LaneUp =  0.24f, Phase = 220f, LeaveHeight = 0.85f, LeaveSide =  0.00f, KnitHeight = 0.82f, Size = 1.15f, WingHz = 4.6f },
            new PhaisterLook.MothRow { Delay = 0.085f, LaneSide =  0.18f, LaneUp = -0.08f, Phase = 290f, LeaveHeight = 0.65f, LeaveSide =  0.16f, KnitHeight = 0.62f, Size = 0.95f, WingHz = 5.4f },
            new PhaisterLook.MothRow { Delay = 0.105f, LaneSide = -0.28f, LaneUp =  0.12f, Phase =  20f, LeaveHeight = 0.48f, LeaveSide = -0.16f, KnitHeight = 0.46f, Size = 1.05f, WingHz = 4.0f },
            new PhaisterLook.MothRow { Delay = 0.125f, LaneSide =  0.10f, LaneUp =  0.20f, Phase = 110f, LeaveHeight = 1.30f, LeaveSide = -0.04f, KnitHeight = 1.30f, Size = 1.00f, WingHz = 4.8f, Straggler = true },
            new PhaisterLook.MothRow { Delay = 0.145f, LaneSide = -0.12f, LaneUp = -0.04f, Phase = 200f, LeaveHeight = 0.30f, LeaveSide =  0.04f, KnitHeight = 0.30f, Size = 0.95f, WingHz = 5.2f },
        };

        private static Mesh _mesh;
        private Material _moths;
        private PhaisterGhostBody _ghost;
        private Transform _model;
        private readonly List<Renderer> _hidden = new List<Renderer>();
        private bool _revealed;
        private float _age;
        public float LifeSeconds => Life;

        /// <summary>
        /// Plays the act. <paramref name="model"/> is her drawn model (baked for the ghost and hidden until `Reveal`; may be
        /// null); <paramref name="from"/> and <paramref name="to"/> are ground points.
        /// </summary>
        public static PhaisterMothRibbon Play(Transform model, Vector3 from, Vector3 to)
        {
            var go = new GameObject("PhaisterMothRibbon");
            var fx = go.AddComponent<PhaisterMothRibbon>();
            fx._model = model;
            if (_mesh == null) _mesh = PhaisterLook.Moths("PhaisterMothRibbon", Rows);
            var moths = PhaisterLook.Draw("Moths", go.transform, _mesh);
            fx._moths = PhaisterLook.Paint(moths, "PhaisterMoths");
            if (fx._moths != null)
            {
                fx._moths.SetFloat("_Mode", 0f);
                fx._moths.SetVector("_From", from);
                fx._moths.SetVector("_To", to);
                fx._moths.SetFloat("_Travel", Travel);
            }
            fx._ghost = PhaisterGhostBody.Bake(model, go.transform, from, to, 0);
            fx.BuildPath(from, to);
            PhaisterLook.Hide(model, fx._hidden);
            fx.StepTo(0f);
            return fx;
        }

        /// <summary>
        /// ⚠️ THE ONE SHAPE THE MOTHS RIDE (round 4). Eight moths alone still read as eight things; under them a broad soft
        /// band of her smoke runs the same bowed arc, drawn head first and let go from the tail, so the eye reads ONE path
        /// from her to where she lands. The bench's own ribbon (`FxStudyRibbon`, `trail_smoke` scrolled along it, eroded by
        /// `erode_cloud`), two copies crossed at right angles so it never shows as a sliver from any side.
        /// </summary>
        private void BuildPath(Vector3 from, Vector3 to)
        {
            Vector3 along = to - from; along.y = 0f;
            float len = along.magnitude;
            Vector3 dir = len > 0.01f ? along / len : Vector3.forward;
            Vector3 side = Vector3.Cross(Vector3.up, dir).normalized;
            var knots = new List<FxStudyKit.Knot>();
            for (int i = 0; i <= 8; i++)
            {
                float u = i / 8f;
                Vector3 leave = from + Vector3.up * 1.05f, land = to + Vector3.up * 0.95f;
                Vector3 mid = Vector3.Lerp(leave, land, 0.5f) + Vector3.up * (0.62f + 0.10f * len);
                Vector3 at = (1f - u) * (1f - u) * leave + 2f * (1f - u) * u * mid + u * u * land;
                // Wide in the middle of the flight, narrow where it leaves her and where it lands.
                _knots.Add(at);
                knots.Add(new FxStudyKit.Knot(at, Mathf.Lerp(0.35f, 1.05f, Mathf.Sin(u * Mathf.PI)), u * 160f));
            }
            int order = 0;
            foreach (var face in new[] { Vector3.up, side })
            {
                var mesh = FxStudyKit.Sweep(knots, face, 10, 6, 0.15f);
                var piece = new FxPiece("Path", transform, mesh, FxStudyKit.Ribbon("trail_smoke", "erode_cloud"), 8 + order++);
                piece.Colours(new Color(0.16f, 0.05f, 0.26f), new Color(0.46f, 0.17f, 0.68f), new Color(0.90f, 0.74f, 1f), new Color(0.50f, 0.20f, 0.78f))
                     .Set("_Scroll", -1.6f, 0f).Set("_Tile", 1.6f, 1f).Set("_Cut", 0.10f, 0.45f).Set("_Stops", 0.40f, 0.68f, 0.95f)
                     .Set("_Ends", 0.10f, 0.20f, 0.30f).Set("_Add", 0.55f).Set("_Halo", 0.5f).Set("_Soft", 0.14f).Set("_Sweep", 1.2f)
                     .Set("_ErodeTile", 1.4f, 1f).Set("_ErodeScroll", -0.4f, 0f).WindowSoft(0.12f);
                _paths.Add(piece);
                _owned.Add(mesh); _owned.Add(piece.Material);
            }
        }

        private readonly List<Vector3> _knots = new List<Vector3>();
        private readonly List<FxPiece> _paths = new List<FxPiece>();
        private readonly List<Object> _owned = new List<Object>();

        private void Update()
        {
            StepTo(_age + Time.deltaTime);
            if (_age >= Life) Destroy(gameObject);
        }

        public void StepTo(float seconds)
        {
            _age = seconds;
            if (_moths != null) _moths.SetFloat("_Age", seconds);
            _ghost?.StepTo(seconds);
            // The path: its head races out with the leading moths, its tail lets go behind the last, then the cloud eats it.
            float head = FxStudyKit.EaseOut(FxStudyKit.Run(seconds, 0.02f, 0.36f)) * 1.1f;
            float tail = FxStudyKit.Ease(FxStudyKit.Run(seconds, 0.16f, 0.70f)) * 1.05f - 0.05f;
            foreach (var path in _paths)
            {
                path.Open(tail, head);
                path.Frame(seconds, 0.9f, FxStudyKit.Run(seconds, 0.40f, 0.95f));
            }
            if (!_revealed && seconds >= Reveal) { PhaisterLook.Show(_hidden); _revealed = true; }
        }

        private void OnDestroy()
        {
            if (!_revealed) PhaisterLook.Show(_hidden);
            _ghost?.Free();
            foreach (var thing in _owned) PhaisterLook.Free(thing);
        }
    }
}
