using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;
using Knot = TumbangPreso.Visual.FxStudyKit.Knot;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ AIRBURST'S LIVE LOOK, REVAMPED, BEHIND <see cref="Enabled"/> (on). The cutscene is not this and is unchanged.
    /// False draws exactly what a match drew before: the tail of the cutscene's fan (`AmihanStormFan`), and the quick
    /// pass `UltimateLiveLook.AmihanAirburst` only if that switch is on (it is off).
    ///
    /// What the picture has to tell the truth about (`AmihanRules`, `AmihanStorm.InsideFan`): play resumes ON the hit
    /// (v3.2), so every player in the half of the court in front of her (180 degrees, the whole court that way) and every
    /// player within 10 m of her, behind her too, has already been Whirled and is flying outward, 16 m at 15 m/s with a
    /// 7 m/s lift, for `StormSurgeHoldSeconds`. Loose slippers in the same area fly the same way. Behind her and more
    /// than 10 m away nobody is touched. So the live picture is the AFTERMATH, the gale still blowing them outward, and
    /// its one hard line is the edge of what it took: the 10 m arc behind her (and, on a court wide enough, the line
    /// through her out to the sides beyond it).
    ///   * THE EDGE: trail ribbon lying flat on the court along that boundary, in the whirlwind's ground greens (its darkest
    ///     family, so it holds on a pale road), drawn on from the sides toward her back in a fifth of a second, held while
    ///     the thrown are in the air, then eaten away tip first. Only the part inside the playable box is drawn.
    ///   * THE GALE: fifteen low arched trail ribbons (the approved t32 "bands" and "ink" masks) streaming outward along
    ///     the ground, inside the area only: eleven in front, four behind that stop short of the 10 m arc. Each draws on
    ///     outward, lets go of its root, and unravels in place where it is (a trail is history: nothing turns or slides).
    ///     Each turns about its own line to the eye, so no view sees a sliver.
    ///   * HER FEET: the wisp's dish streaks thrown out round her, as DRIFT and FEATHERFALL throw them, and one cloud puff.
    ///
    /// Every piece is a bench piece at its own scale: the ribbons keep the cell size they were approved at (their tiling
    /// follows their length), the dish is 2 m across as it is under FEATHERFALL's landing. What does NOT fit a bench
    /// piece is the 10 m arc: a trail ribbon was approved standing 2 m long, and the arc is 15 to 31 m of it lying flat, so
    /// it is tiled along its length and is the piece to look at first.
    ///
    /// ⚠️ GPU-DRIVEN. The ribbons' one mesh and the dish are made once for the process; the edge is one sweep made once
    /// per cast (it depends on where she stands). Per frame only the shader's clock and window and a transform turn
    /// about each ribbon's line. Nothing on the wire, nothing decided: it is drawn on every peer from the accepted pose,
    /// at `HeroAbility.OnActivate`, which is where the host throws everyone.
    /// </summary>
    public sealed class AmihanAirburstLook : MonoBehaviour
    {
        public static bool Enabled = true;
        /// <summary>With the new look on, the fan's tail left over from the cutscene is not drawn under it.</summary>
        public static bool HideOldFan = true;
        /// <summary>
        /// ⚠️ HER OWN BRUSH (`AmihanAirburstPainted`), on. The owner, of the picture this replaces (still drawn when this is
        /// false): "theres nothing unique and its all just the same reused effects".
        /// </summary>
        public static bool Painted = false;
        /// <summary>
        /// ⚠️ THE LOCKED BENCH SAMPLE, CALLED AND NOT COPIED (`FxStudyAirburst`, r20, `tools/cutscene_rework/approved/amihan_airburst_r20`),
        /// on, and it wins over `Painted` and over the picture below. The owner: "lock in airburst". False goes back to those two.
        /// It draws the 10 m she takes all round her (`AmihanRules.StormSurgeAroundRadius`), as the sample was approved. It has no
        /// puff at her feet and no edge line, because the sample has neither.
        /// </summary>
        public static bool Bench = true;
        public const float Light = AmihanWhirlwind.DayLight;

        private AmihanAirburstPicture _picture;
        private AmihanAirburstPainted _painted;
        private FxStudyAirburst _bench;
        // The bodies it threw, and where each has been since, for the woven ribbons that carry them.
        private readonly List<Transform> _thrown = new List<Transform>();
        private readonly List<AmihanWindKit.History> _flown = new List<AmihanWindKit.History>();
        private Abilities.AmihanStorm _storm;
        private float _age;
        private readonly List<Renderer> _renderers = new List<Renderer>();

        public static AmihanAirburstLook Play(Vector3 origin, Vector3 forward, Abilities.AmihanStorm storm)
        {
            var go = new GameObject("AmihanAirburstLook") { hideFlags = HideFlags.DontSave };
            var look = go.AddComponent<AmihanAirburstLook>();
            var ground = new Vector3(origin.x, VfxShapes.GroundAt(origin, origin.y - 2f, 2f), origin.z);
            look._storm = storm;
            if (Bench) look._bench = BuildBench(go.transform, origin, forward);
            else if (Painted)
            {
                // Every peer works out who was thrown the way the host did (`AmihanStorm.InsideFan` from the accepted pose),
                // only to draw the wind round them; nothing here moves anybody.
                var round = GameServices.Round;
                var paths = new List<System.Func<float, Vector3>>();
                if (round != null)
                    foreach (var p in round.Players)
                    {
                        if (p == null || (storm != null && p.PlayerSlot == storm.OwnerSlot)) continue;
                        if (!Abilities.AmihanStorm.InsideFan(origin, forward, p.transform.position)) continue;
                        var kept = new AmihanWindKit.History();
                        kept.Keep(0f, p.transform.position + Vector3.up * 0.8f);
                        look._thrown.Add(p.transform); look._flown.Add(kept);
                        paths.Add(kept.At);
                    }
                look._painted = new AmihanAirburstPainted(go.transform, ground, forward, AmihanAirburstPicture.CourtBox, paths);
            }
            else
            {
                look._picture = new AmihanAirburstPicture(go.transform, ground, forward, AmihanAirburstPicture.CourtBox, Light);
                foreach (var renderer in go.GetComponentsInChildren<MeshRenderer>(true)) VfxRenderTag.Attach(renderer.gameObject);
                var puff = FxStudyFeatherfall.LandingPuff(ground);
                puff.Scale = 2.2f; puff.Reach = 0.9f;
                CloudPuffs.Spawn(puff);
            }
            look.Step(0f);
            return look;
        }

        /// <summary>
        /// The sample set down where she cast it, on the court under her. A film outside Play calls this too, so what it films
        /// is what a match places. ⚠️ THE FLOOR IS ASKED FOR WITH THE WHOLE DROP OF A FLIGHT: she can cast from the air, and the
        /// sample lies on the floor at knee height at most, so a height kept from the air would float the whole 10 m.
        /// </summary>
        public static FxStudyAirburst BuildBench(Transform holder, Vector3 origin, Vector3 forward)
        {
            forward.y = 0f;
            forward = forward.sqrMagnitude > 1e-4f ? forward.normalized : Vector3.forward;
            float floor = VfxShapes.TryGroundUnder(origin, 2f, 32f, out float height) ? height : origin.y;
            holder.SetPositionAndRotation(new Vector3(origin.x, floor, origin.z), Quaternion.LookRotation(forward, Vector3.up));
            var bench = new FxStudyAirburst(holder, false, false, true);
            foreach (var renderer in holder.GetComponentsInChildren<MeshRenderer>(true)) VfxRenderTag.Attach(renderer.gameObject);
            return bench;
        }

        private void LateUpdate() => Step(Time.deltaTime);

        private void Step(float dt)
        {
            if (_picture == null && _painted == null && _bench == null) { Destroy(gameObject); return; }
            _age += dt;
            if (HideOldFan && _storm != null)
            {
                _storm.GetComponentsInChildren(true, _renderers);
                foreach (var r in _renderers) if (!r.forceRenderingOff) r.forceRenderingOff = true;
            }
            if (_bench != null)
            {
                var eyes = Camera.main;
                _bench.Sample(_age, eyes != null ? eyes.transform.position : transform.position + new Vector3(0f, 8f, -8f));
                if (_age >= _bench.Seconds) Destroy(gameObject);
                return;
            }
            if (_painted != null)
            {
                for (int i = 0; i < _thrown.Count; i++) if (_thrown[i] != null) _flown[i].Keep(_age, _thrown[i].position + Vector3.up * 0.8f);
                _painted.Pose(_age);
                if (_age >= _painted.Seconds) Destroy(gameObject);
                return;
            }
            var lens = Camera.main;
            Vector3 eye = lens != null ? lens.transform.position : transform.position + new Vector3(0f, 8f, -8f);
            _picture.Pose(_age, eye);
            if (_age >= _picture.Seconds) Destroy(gameObject);
        }

        private void OnDestroy() { _picture?.Dispose(); _picture = null; _painted?.Dispose(); _painted = null; _bench?.Dispose(); _bench = null; }
    }

    /// <summary>The picture itself, posed as a pure function of its age and the eye, so a film can draw it outside Play.</summary>
    public sealed class AmihanAirburstPicture : FxStudyEffect
    {
        /// <summary>The edge draws on in this long; everything has unravelled this long after the thrown have landed.</summary>
        public const float DrawOn = 0.18f, Unravel = 0.55f;
        private const float EdgeWidth = 0.55f, Inset = 0.3f;
        // The thrown are carried for the hold (0.82 s) and then slide 0.5 s more (`CarryRules`): the edge stays through the
        // carry and half the slide, while they are still visibly going.
        private static float Hold => AmihanRules.StormSurgeHoldSeconds + 0.5f * AmihanRules.StormSurgeSpeed / Balance.Friction;
        private static float Around => AmihanRules.StormSurgeAroundRadius;

        /// <summary>The playable box as (min x, max x, min z, max z).</summary>
        public static Vector4 CourtBox => new Vector4(AIController.PlayableMinX, AIController.PlayableMaxX, AIController.PlayableMinZ, AIController.PlayableMaxZ);

        private struct Stream { public FxPiece Piece; public Transform Holder; public float Delay, Life, Mid, Base; }
        private readonly List<Stream> _streams = new List<Stream>();
        private readonly List<FxPiece> _edges = new List<FxPiece>();
        private readonly FxPiece _kick;

        // THE GALE: angle from her facing (degrees, + to her right), where it starts (m from her), how long (m), how wide (m),
        // how high its arch (m), which approved trail mask (`FxStudyTrails.MaskOf`), and how late it starts (s).
        private static readonly float[,] Rows =
        {
            { -78f, 1.4f, 6.0f, 0.70f, 0.55f, 4, 0.00f }, { -52f, 1.2f, 7.0f, 0.85f, 0.75f, 2, 0.03f }, { -26f, 1.6f, 7.5f, 0.80f, 0.70f, 4, 0.01f },
            {   0f, 1.3f, 8.0f, 0.90f, 0.85f, 4, 0.02f }, {  24f, 1.5f, 7.0f, 0.80f, 0.65f, 2, 0.00f }, {  50f, 1.2f, 6.5f, 0.85f, 0.75f, 4, 0.04f },
            {  76f, 1.6f, 5.5f, 0.70f, 0.55f, 2, 0.02f },
            { -40f, 8.5f, 7.0f, 0.80f, 0.70f, 2, 0.12f }, { -12f, 9.5f, 8.0f, 0.90f, 0.80f, 4, 0.14f }, {  14f, 8.8f, 7.5f, 0.85f, 0.75f, 4, 0.10f },
            {  42f, 9.2f, 6.5f, 0.80f, 0.65f, 2, 0.16f },
            { 112f, 1.4f, 6.5f, 0.70f, 0.55f, 4, 0.02f }, { 150f, 1.2f, 7.5f, 0.80f, 0.65f, 2, 0.04f }, { -150f, 1.5f, 7.0f, 0.80f, 0.60f, 4, 0.03f },
            { -112f, 1.3f, 6.5f, 0.70f, 0.55f, 2, 0.01f },
        };

        private static readonly Dictionary<string, Mesh> Meshes = new Dictionary<string, Mesh>();
        private static Mesh Once(string key, System.Func<Mesh> make)
        {
            if (Meshes.TryGetValue(key, out var mesh) && mesh != null) return mesh;
            mesh = make();
            if (mesh != null) mesh.hideFlags = HideFlags.HideAndDontSave;
            Meshes[key] = mesh;
            return mesh;
        }

        public override float Seconds => Hold + Unravel + 0.1f;
        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov)
        { focus = Root.position + Root.forward * 6f; eye = Root.position - Root.forward * 6f + Vector3.up * 7f; fov = 55f; }
        public override void Sample(float age, Vector3 eye) => Pose(age, eye);

        /// <param name="ground">Her feet on the court.</param><param name="box">The playable box (`CourtBox`).</param>
        /// <param name="light">How much of every piece is light laid on (`AmihanAirburstLook.Light` in a match).</param>
        public AmihanAirburstPicture(Transform parent, Vector3 ground, Vector3 forward, Vector4 box, float light) : base("airburst", parent)
        {
            forward.y = 0f;
            forward = forward.sqrMagnitude > 1e-4f ? forward.normalized : Vector3.forward;
            Root.SetPositionAndRotation(ground, Quaternion.LookRotation(forward, Vector3.up));
            var look = FxStudyWhirlwind.Look.Picture;
            look.Light = light;
            var o = new Vector2(ground.x, ground.z);

            // THE GALE. Its one mesh: a unit arch along +z (0 to 1), a unit wide, lying flat, made once for the process.
            var arch = Once("arch", () => FxStudyKit.Sweep(new[]
            {
                new Knot(0f, 0.00f, 0.00f, 0.35f), new Knot(0f, 0.75f, 0.25f, 0.90f), new Knot(0f, 1.00f, 0.50f, 1.00f),
                new Knot(0f, 0.72f, 0.75f, 0.85f), new Knot(0f, 0.18f, 1.00f, 0.40f),
            }, Vector3.up, perKnot: 10, across: 4));
            var gale = Group("gale");
            for (int i = 0; i < Rows.GetLength(0); i++)
            {
                float angle = Rows[i, 0], start = Rows[i, 1], length = Rows[i, 2], width = Rows[i, 3], high = Rows[i, 4], delay = Rows[i, 6];
                int mask = (int)Rows[i, 5];
                Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * forward;
                // Inside the area only: a streak behind her stops short of the 10 m arc, and none leaves the playable box.
                float exit = Exit(o, new Vector2(dir.x, dir.z), box) - Inset;
                if (Mathf.Abs(angle) > 90f) exit = Mathf.Min(exit, Around - 0.4f);
                length = Mathf.Min(length, exit - start);
                if (length < 1.8f) continue;
                var holder = new GameObject("stream-" + i) { hideFlags = HideFlags.DontSave }.transform;
                holder.SetParent(gale, false);
                holder.localRotation = Quaternion.Euler(0f, angle, 0f);
                float lift = 0.15f + 0.5f * width;
                holder.localPosition = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, lift, start);
                var piece = AddShared("stream-" + i, arch, FxStudyKit.Ribbon(FxStudyTrails.MaskOf(mask)), 20 + i, holder);
                piece.At.localScale = new Vector3(width, high, length);
                float seed = i * 0.37f;
                // The approved ribbon, in the whirlwind's silk, its cells the size they were approved at whatever its length.
                FxStudyTrails.Dress(piece, mask).Colours(look.SilkDeep, look.Silk, look.SilkHot, look.SilkHalo)
                    .Set("_Tile", length / 2.6f, 1f, seed, 0f).Set("_Scroll", 2.6f, 0f)
                    .Set("_Add", Mathf.Min(0.7f, look.Light)).Set("_Hue", look.HueDrift, 6f, seed, 1f)
                    .Set("_Ends", 0.10f, 0.22f, 0.08f).Set("_Sweep", 0.9f).Set("_Warp", 0.16f, 2f, seed, 1.4f).WindowSoft(0.08f);
                _streams.Add(new Stream { Piece = piece, Holder = holder, Delay = delay, Life = Hold * 0.8f + 0.35f, Mid = 0.5f * length, Base = lift });
            }

            // THE EDGE, made for where she stands: the 10 m arc behind her, split at her back so each half draws on from
            // its side, and only the runs of it inside the box; and the line out to each side past it, where the box is wider.
            var edges = Group("edge");
            var inside = new List<float>();
            for (float a = 90f; a <= 270.01f; a += 2f)
            {
                Vector3 p = ground + Quaternion.AngleAxis(a, Vector3.up) * forward * Around;
                bool inBox = p.x > box.x + Inset && p.x < box.y - Inset && p.z > box.z + Inset && p.z < box.w - Inset;
                if (inBox) inside.Add(a);
                if ((!inBox || a > 270f - 1f) && inside.Count > 0) { ArcRuns(inside, edges, look); inside.Clear(); }
            }
            foreach (float side in new[] { -90f, 90f })
            {
                Vector3 dir = Quaternion.AngleAxis(side, Vector3.up) * forward;
                float reach = Exit(o, new Vector2(dir.x, dir.z), box) - Inset;
                if (reach < Around + 1f) continue;
                var knots = new List<Knot>();
                for (int k = 0; k <= 4; k++) knots.Add(new Knot(Quaternion.AngleAxis(side, Vector3.up) * Vector3.forward * Mathf.Lerp(Around, reach, k / 4f) + Vector3.up * 0.04f, EdgeWidth));
                Edge("line", knots, reach - Around, edges, look);
            }

            // HER FEET: the wisp's dish streaks thrown out round her.
            var dish = new[] { new Vector2(0.15f, 0.03f), new Vector2(0.45f, 0.03f), new Vector2(0.80f, 0.03f), new Vector2(1.0f, 0.03f) };
            _kick = AddShared("kick", Once("dish", () => FxStudyKit.Revolve(dish, 48)), FxStudyKit.Ribbon("erode_streak", "erode_cloud"), 2, Group("feet"));
            FxStudyWisp.DressDishStreaks(_kick).Colours(look.SilkDeep, look.Silk, look.SilkHot, look.SilkHalo)
                .Set("_Add", look.Light).Set("_Ends", 0f, 0f, 0.32f).Set("_Hue", look.HueDrift, 4f, 0.3f, 1f);
        }

        // One run of in-box arc angles, split at her back (180) into halves that each run from the side toward the back.
        private void ArcRuns(List<float> run, Transform parent, FxStudyWhirlwind.Look look)
        {
            var low = new List<float>(); var high = new List<float>();
            foreach (float a in run) (a <= 180f ? low : high).Add(a);
            high.Reverse();
            foreach (var half in new[] { low, high })
            {
                if (half.Count < 4) continue;
                var knots = new List<Knot>();
                int step = Mathf.Max(1, half.Count / 8);
                for (int k = 0; k < half.Count; k += step) knots.Add(new Knot(Quaternion.AngleAxis(half[k], Vector3.up) * Vector3.forward * Around + Vector3.up * 0.04f, EdgeWidth));
                var last = half[half.Count - 1];
                if ((half.Count - 1) % step != 0) knots.Add(new Knot(Quaternion.AngleAxis(last, Vector3.up) * Vector3.forward * Around + Vector3.up * 0.04f, EdgeWidth));
                Edge("arc", knots, Around * Mathf.Abs(last - half[0]) * Mathf.Deg2Rad, parent, look);
            }
        }

        private void Edge(string name, List<Knot> knots, float length, Transform parent, FxStudyWhirlwind.Look look)
        {
            if (knots.Count < 3) { if (knots.Count == 2) knots.Insert(1, new Knot((knots[0].At + knots[1].At) * 0.5f, EdgeWidth)); else return; }
            var piece = Add("edge-" + name + _edges.Count, FxStudyKit.Sweep(knots, Vector3.up, perKnot: 8, across: 4), FxStudyKit.Ribbon(FxStudyTrails.MaskOf(4)), 10 + _edges.Count, parent);
            FxStudyTrails.Dress(piece, 4).Colours(look.GroundDeep, look.Ground, look.GroundHot, look.Ground)
                .Set("_Tile", Mathf.Max(1f, length / 2.2f), 1f, _edges.Count * 0.41f, 0f).Set("_Scroll", 0.6f, 0f)
                .Set("_Add", Mathf.Min(0.6f, look.Light)).Set("_Hue", look.HueDrift, 6f, _edges.Count * 0.41f, 1f)
                .Set("_Ends", 0.04f, 0.04f, 0.12f).Set("_Sweep", -1.0f).WindowSoft(0.04f);
            _edges.Add(piece);
        }

        /// <summary>How far from `o` along `d` the box's edge is (m).</summary>
        private static float Exit(Vector2 o, Vector2 d, Vector4 box)
        {
            float t = 1000f;
            if (d.x > 1e-4f) t = Mathf.Min(t, (box.y - o.x) / d.x); else if (d.x < -1e-4f) t = Mathf.Min(t, (box.x - o.x) / d.x);
            if (d.y > 1e-4f) t = Mathf.Min(t, (box.w - o.y) / d.y); else if (d.y < -1e-4f) t = Mathf.Min(t, (box.z - o.y) / d.y);
            return Mathf.Max(0f, t);
        }

        /// <summary>Poses it. `age` is the time since the hit (play resuming); `eye` is where it is seen from.</summary>
        public void Pose(float age, Vector3 eye)
        {
            float a = Mathf.Max(0f, age);
            bool on = age >= 0f;

            // The edge: drawn on, held while the thrown fly, then eaten from her back outward to the walls.
            float drawn = FxStudyKit.EaseOut(FxStudyKit.Run(a, 0f, DrawOn)) * 1.1f;
            float eaten = FxStudyKit.Ease(FxStudyKit.Run(a, Hold, Hold + Unravel));
            foreach (var edge in _edges)
            {
                edge.Open(-0.1f, drawn);
                edge.Frame(a, on ? 0.95f : 0f, eaten);
            }

            // The gale: each streak draws on outward, lets go of its root, and unravels where it is.
            foreach (var s in _streams)
            {
                float u = a - s.Delay;
                float head = FxStudyKit.EaseOut(FxStudyKit.Run(u, 0f, 0.30f)) * 1.1f;
                float tail = FxStudyKit.Ease(FxStudyKit.Run(u, s.Life * 0.35f, s.Life)) * 1.1f - 0.1f;
                s.Piece.Open(tail, head);
                s.Piece.Frame(a, on && u >= 0f && u < s.Life ? 0.9f : 0f, FxStudyKit.Run(u, s.Life * 0.55f, s.Life) * 0.85f);
                // Turned about its own line to the eye (never past 70 degrees, so the arch still stands).
                Vector3 local = s.Holder.InverseTransformPoint(eye) - new Vector3(0f, 0f, s.Mid);
                float turn = Mathf.Clamp(Mathf.Atan2(-local.x, Mathf.Max(0.01f, local.y)) * Mathf.Rad2Deg, -70f, 70f);
                s.Piece.At.localRotation = Quaternion.Euler(0f, 0f, turn);
            }

            // Her feet: the dish streaks thrown out and spun, then torn away.
            float thrown = FxStudyKit.EaseOut(FxStudyKit.Run(a, 0f, 0.30f));
            _kick.At.localScale = new Vector3(Mathf.Lerp(0.5f, 2.0f, thrown), 1f, Mathf.Lerp(0.5f, 2.0f, thrown));
            _kick.At.localRotation = Quaternion.Euler(0f, -a * 160f, 0f);
            _kick.Frame(a, on && a < 0.8f ? 0.85f : 0f, Mathf.Max(0.3f, FxStudyKit.Run(a, 0.10f, 0.8f)));
        }
    }
}
