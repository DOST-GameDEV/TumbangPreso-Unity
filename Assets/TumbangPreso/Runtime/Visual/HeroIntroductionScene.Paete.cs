using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // PAETE, YAKAP NG MAKILING. The four beats of `docs/reports/amihan-kit-2026-09-25/direction.md`
        // § 7 (who, intent, gather, release), in his own world: Mount Makiling's forest in its white
        // mist (the mist Mariang Makiling is told to walk in, `ArtSource/paete/concept-20260925/
        // design-brief.md` § 2), never a textured backdrop.
        //
        // ⚠️⚠️ v2 (2026-09-26), AFTER ITS FIRST FILM (`UltimateIntroductionProbe.PaeteThrowsHisSeedFromTheForest`,
        // never filmed before): v1's forest was eight dark prism slabs with a block on each, and the
        // gather wrapped him in a green corkscrew, the exact shape the owner had just called out on the
        // sentry (*"why do they just twirl there for no reason"*, *"dont use vines use woven tree
        // branches"*). Now the forest IS the sentry's family (direction.md section 5):
        //
        //   WHO      four woven trees (the sentry's own model, smaller) screw up out of the ground
        //            behind him, one after another, their crowns opening; mist drifts in low.
        //   INTENT   a seed lights in his right palm, the same light as his eyes; leaves are pulled in
        //            to it; and one by one the light opens in the trees' hollows: Makiling waking up
        //            and looking at him.
        //   GATHER   bark roots break the ground in a ring round him and woven root-branches climb his
        //            legs to the waist (typed paths, no spiral); the trees lean in; the seed swells.
        //   RELEASE  he throws; bark spikes burst out down the court where it lands, the first frame of
        //            the live sentry; the climbing branches let go and sink.
        //
        // The only lights are the seed and the trees' eyes.
        // =========================================================================================
        private int _paeteGround, _paeteMist, _paeteSky, _paeteMountain;
        private readonly List<PaeteForestTree> _makiling = new List<PaeteForestTree>(4);
        private readonly List<int> _paeteRoots = new List<int>(8);
        private readonly List<int> _paeteLeaves = new List<int>(14);
        private readonly List<int> _paeteBurst = new List<int>(8);
        private readonly List<PaeteRope> _paeteClimbers = new List<PaeteRope>(3);
        private readonly List<Vector3> _paeteClimb = new List<Vector3>(32);
        private int _paeteSeed;

        // The forest: compass angle round him (180 is straight behind), distance, size, when it rises,
        // when its eyes open. Each tree its own row.
        private static readonly (float angle, float distance, float scale, float rise, float wake)[] MakilingTrees =
        {
            (146f, 6.3f, 0.80f, 0.02f, 0.72f), (171f, 5.4f, 0.66f, 0.11f, 0.96f),
            (197f, 5.9f, 0.90f, 0.19f, 1.16f), (223f, 6.6f, 0.72f, 0.27f, 1.34f),
        };

        // The climbing branches: keys of (compass angle round him, height, distance out). Typed.
        private static readonly Vector3[][] Climbers =
        {
            new[] { new Vector3(200f, -0.10f, 1.20f), new Vector3(230f, 0.20f, 0.80f), new Vector3(270f, 0.45f, 0.56f),
                    new Vector3(320f, 0.70f, 0.47f), new Vector3(380f, 0.95f, 0.44f), new Vector3(430f, 1.15f, 0.46f) },
            new[] { new Vector3(60f, -0.10f, 1.10f), new Vector3(95f, 0.25f, 0.76f), new Vector3(140f, 0.50f, 0.53f),
                    new Vector3(190f, 0.78f, 0.47f), new Vector3(240f, 1.00f, 0.45f), new Vector3(285f, 1.22f, 0.47f) },
            new[] { new Vector3(320f, -0.10f, 1.25f), new Vector3(345f, 0.20f, 0.86f), new Vector3(20f, 0.42f, 0.60f),
                    new Vector3(70f, 0.60f, 0.50f), new Vector3(120f, 0.85f, 0.47f) },
        };

        private static Mesh GrowthTube(IList<Vector3> points, IList<float> radii, int sides)
        {
            var mesh = new Mesh { name = "PaeteIntroTube" };
            PaeteInk.Tube(mesh, points, radii, sides);
            return mesh;
        }

        private void BuildPaete()
        {
            _paeteGround = Wall("PaeteGround", 0, 0.8f, new Color(0.10f, 0.14f, 0.07f, 0.92f), emission: 0.08f);
            _paeteMist = Wall("PaeteMist", 0.3f, 2.2f, new Color(0.86f, 0.90f, 0.84f, 0.42f), radius: 7.6f, emission: 0.35f);
            _paeteSky = Wall("PaeteSky", 2.2f, 12, new Color(0.30f, 0.38f, 0.30f, 0.6f), emission: 0.22f, cap: true);
            // The mountain's leaning shoulder (makiling: "leaning, uneven"), one low wide block on
            // the far wall, tipped.
            _paeteMountain = AddSolid("MakilingShoulder", VfxShapes.Prism(4, 1, 1), new Color(0.12f, 0.16f, 0.10f, 1));

            foreach (var t in MakilingTrees)
            {
                float a = t.angle * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Sin(a) * t.distance, 0f, Mathf.Cos(a) * t.distance);
                float yaw = Mathf.Atan2(-at.x, -at.z) * Mathf.Rad2Deg;
                _makiling.Add(new PaeteForestTree(_root.transform, at, yaw, t.scale, t.rise, t.wake));
            }

            // The seed in his palm: the one light he holds.
            _paeteSeed = Add("PaeteSeed", GrowthVfx.Leaf(1f, 0.75f, 0.6f), GrowthVfx.Glow, 0.9f, plain: true);

            // Leaves drawn in toward the seed, each its own size.
            for (int i = 0; i < 12; i++)
                _paeteLeaves.Add(AddSolid("PaeteIntroLeaf" + i, GrowthVfx.Leaf(0.16f + 0.02f * (i % 3), 0.09f, 0.015f),
                                          i % 2 == 0 ? GrowthVfx.LeafGreen : GrowthVfx.LeafDark));

            // The ring of roots: six square bark tubes bending up out of the ground, own heights, no vine green.
            float[] h = { 1.6f, 1.1f, 1.9f, 1.3f, 1.7f, 1.2f };
            for (int i = 0; i < h.Length; i++)
                _paeteRoots.Add(AddSolid("PaeteIntroRoot" + i,
                    GrowthTube(new List<Vector3> { new Vector3(0, -0.2f, 0), new Vector3(0, h[i] * 0.4f, 0.12f), new Vector3(0, h[i] * 0.8f, -0.06f), new Vector3(0, h[i], 0.08f) },
                               new List<float> { 0.16f, 0.12f, 0.07f, 0.01f }, 4),
                    i % 2 == 0 ? GrowthVfx.BarkDark : GrowthVfx.Bark));

            // The woven root-branches that climb him in the gather.
            var bark = new[] { GrowthVfx.BarkDark, GrowthVfx.Bark, GrowthVfx.BarkLit };
            for (int i = 0; i < Climbers.Length; i++)
                _paeteClimbers.Add(new PaeteRope(_root.transform, "PaeteIntroClimber" + i, bark, new[] { 0.036f, 0.032f, 0.028f }, 0.034f, 8f, i * 2.1f));

            // The release burst: eight bark spikes erupting down the court (+z), own lengths.
            float[] len = { 2.6f, 2.0f, 3.1f, 2.3f, 2.8f, 1.9f, 3.3f, 2.4f };
            for (int i = 0; i < len.Length; i++)
                _paeteBurst.Add(AddSolid("PaeteIntroBurst" + i,
                    GrowthTube(new List<Vector3> { Vector3.zero, new Vector3(0, len[i] * 0.3f, len[i] * 0.4f), new Vector3(0, len[i] * 0.5f, len[i]) },
                               new List<float> { 0.14f, 0.08f, 0.005f }, 4),
                    i % 3 == 0 ? GrowthVfx.BarkLit : i % 3 == 1 ? GrowthVfx.Bark : GrowthVfx.BarkDark));
        }

        private void SamplePaete(float t)
        {
            float leave = 1 - Ease(Seconds - .45f, Seconds - .05f, t);
            float stage = StagePresence(t, .3f);

            // WHO: ground, mist and sky arrive; the forest stands up out of the ground behind him.
            Tint(_paeteGround, stage); Tint(_paeteMist, stage * (0.8f + 0.2f * Mathf.Sin(t * 2f))); Tint(_paeteSky, stage);
            Place(_paeteMountain, new Vector3(-2.5f, 1.2f, 7.1f), new Vector3(9f, 3.2f * stage, 1f), Quaternion.Euler(0, 180, 8), stage);
            // The trees lean in toward him through the gather, and straighten as he throws.
            float lean = 8f * Ease(1.5f, 2.2f, t) * (1f - Ease(2.62f, 3.0f, t));
            foreach (var tree in _makiling) tree.Pose(t, lean, stage);

            // INTENT: the seed lights in his right palm at 0.6 s; leaves are drawn in to it.
            var palm = RightPalm + new Vector3(0, .06f, .08f);
            float seed = Ease(.6f, .8f, t) * (1 - Ease(2.62f, 2.7f, t));
            float swell = 0.07f + 0.08f * Ease(1.2f, 2.5f, t);
            Place(_paeteSeed, palm, Vector3.one * swell, Quaternion.Euler(t * 90f, t * 140f, 0), seed);
            for (int i = 0; i < _paeteLeaves.Count; i++)
            {
                float a = i * 2.39996f + t * 1.6f;
                float pull = Ease(.6f, 1.6f, t);
                float r = Mathf.Lerp(2.4f + (i % 3) * .5f, .25f, pull);
                var at = palm + new Vector3(Mathf.Cos(a) * r, (i % 4) * .18f - .2f + (1 - pull) * .8f, Mathf.Sin(a) * r);
                float on = Ease(.55f, .8f, t) * (1 - Ease(1.6f, 1.75f, t)) * leave;
                Place(_paeteLeaves[i], at, Vector3.one, Quaternion.Euler(30 + i * 20, a * Mathf.Rad2Deg, i * 15), on);
            }

            // GATHER: the roots break the ground round him, and the woven branches climb his legs.
            for (int i = 0; i < _paeteRoots.Count; i++)
            {
                float a = (i * 60 + 15) * Mathf.Deg2Rad;
                float grow = Ease(1.2f + i * .06f, 1.6f + i * .06f, t);
                var at = new Vector3(Mathf.Sin(a) * 1.3f, 0, Mathf.Cos(a) * 1.3f);
                var face = Quaternion.LookRotation(at.normalized, Vector3.up);
                Place(_paeteRoots[i], at, new Vector3(1, Mathf.Max(.001f, grow), 1), face, grow > .001f ? leave : 0);
            }
            for (int c = 0; c < _paeteClimbers.Count; c++)
            {
                // Up his legs from 1.3 s, each a beat after the last; they let go and sink after the throw.
                float climb = Ease(1.3f + c * .1f, 2.2f + c * .1f, t) * (1f - Ease(2.7f, 3.1f, t));
                var keys = Climbers[c];
                _paeteClimb.Clear();
                int samples = (keys.Length - 1) * 4;
                int shown = Mathf.RoundToInt(samples * climb);
                for (int s = 0; s <= shown && shown > 0; s++)
                {
                    float f = s / 4f;
                    int k = Mathf.Min(keys.Length - 2, Mathf.FloorToInt(f));
                    var key = Vector3.Lerp(keys[k], keys[k + 1], f - k);
                    float ang = key.x * Mathf.Deg2Rad;
                    _paeteClimb.Add(new Vector3(Mathf.Sin(ang) * key.z, key.y, Mathf.Cos(ang) * key.z));
                }
                if (_paeteClimb.Count < 2 || leave <= 0.002f) _paeteClimbers[c].Clear();
                else _paeteClimbers[c].Draw(_paeteClimb, 0.4f);
            }

            // RELEASE (2.62 s): the throw; bark spikes erupt down the court.
            for (int i = 0; i < _paeteBurst.Count; i++)
            {
                float go = Ease(2.7f + i * .025f, 3.0f + i * .025f, t);
                float spread = (-35 + i * 10) * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Sin(spread) * .6f, 0, 2.2f + Mathf.Cos(spread) * .6f);
                var face = Quaternion.Euler(0, spread * Mathf.Rad2Deg, 0);
                Place(_paeteBurst[i], at, Vector3.one * Mathf.Max(.001f, GrowthVfx.Pop(go)), face, go > .001f ? leave : 0);
            }
        }
    }
}
