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
        //   WHO      the forest stands up round him: blocky trunks rise out of the ground on the far
        //            wall, the mountain's leaning shoulder behind them, mist drifting in low.
        //   INTENT   a seed lights in his right palm, green, the same light as his eyes; leaves are
        //            pulled in toward it.
        //   GATHER   roots break the ground in a ring round him and climb, a vine spirals up round
        //            him, the seed swells.
        //   RELEASE  he throws; spiked vines burst out down the court where it lands, the first frame
        //            of the live sentry.
        //
        // Every piece of growth here is a `GrowthVfx` tube or leaf, the family his live effects use,
        // so the cutscene and the ability are one language. The only light is the seed.
        // =========================================================================================
        private int _paeteGround, _paeteMist, _paeteSky, _paeteMountain;
        private readonly List<int> _makilingTrunks = new List<int>(16);
        private readonly List<int> _paeteRoots = new List<int>(8);
        private readonly List<int> _paeteLeaves = new List<int>(14);
        private readonly List<int> _paeteBurst = new List<int>(8);
        private int _paeteSeed, _paeteSpiral;

        private static Mesh GrowthTube(IList<Vector3> points, IList<float> radii, int sides)
        {
            var mesh = new Mesh { name = "PaeteIntroTube" };
            GrowthVfx.Tube(mesh, points, radii, sides);
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

            // The forest: eight trunks round the far wall, each its own height, with a canopy block.
            var bark = new Color(0.22f, 0.16f, 0.10f, 1);
            var canopy = new Color(0.16f, 0.26f, 0.10f, 1);
            for (int i = 0; i < 8; i++)
            {
                _makilingTrunks.Add(AddSolid("MakilingTrunk" + i, VfxShapes.Prism(4, 1, 1), bark));
                _makilingTrunks.Add(AddSolid("MakilingCanopy" + i, VfxShapes.Prism(4, 1, 1), canopy));
            }

            // The seed in his palm: the one light.
            _paeteSeed = Add("PaeteSeed", GrowthVfx.Leaf(1f, 0.75f, 0.6f), GrowthVfx.Glow, 0.9f, plain: true);

            // Leaves drawn in toward the seed, each its own size.
            for (int i = 0; i < 12; i++)
                _paeteLeaves.Add(AddSolid("PaeteIntroLeaf" + i, GrowthVfx.Leaf(0.16f + 0.02f * (i % 3), 0.09f, 0.015f),
                                          i % 2 == 0 ? GrowthVfx.LeafGreen : GrowthVfx.LeafDark));

            // The ring of roots: six, each a square tube bending up out of the ground, own heights.
            float[] h = { 1.6f, 1.1f, 1.9f, 1.3f, 1.7f, 1.2f };
            for (int i = 0; i < h.Length; i++)
                _paeteRoots.Add(AddSolid("PaeteIntroRoot" + i,
                    GrowthTube(new List<Vector3> { new Vector3(0, -0.2f, 0), new Vector3(0, h[i] * 0.4f, 0.12f), new Vector3(0, h[i] * 0.8f, -0.06f), new Vector3(0, h[i], 0.08f) },
                               new List<float> { 0.16f, 0.12f, 0.07f, 0.01f }, 4),
                    i % 2 == 0 ? GrowthVfx.BarkDark : GrowthVfx.Vine));

            // The vine spiralling up round him.
            var spiral = new List<Vector3>(); var radii = new List<float>();
            for (int k = 0; k <= 40; k++)
            {
                float u = k / 40f, a = u * Mathf.PI * 5f;
                spiral.Add(new Vector3(Mathf.Cos(a) * (1.0f - 0.35f * u), u * 2.4f, Mathf.Sin(a) * (1.0f - 0.35f * u)));
                radii.Add(Mathf.Lerp(0.07f, 0.015f, u));
            }
            _paeteSpiral = AddSolid("PaeteIntroSpiral", GrowthTube(spiral, radii, 5), GrowthVfx.Vine);

            // The release burst: eight spiked vines erupting down the court (+z), own lengths.
            float[] len = { 2.6f, 2.0f, 3.1f, 2.3f, 2.8f, 1.9f, 3.3f, 2.4f };
            for (int i = 0; i < len.Length; i++)
                _paeteBurst.Add(AddSolid("PaeteIntroBurst" + i,
                    GrowthTube(new List<Vector3> { Vector3.zero, new Vector3(0, len[i] * 0.3f, len[i] * 0.4f), new Vector3(0, len[i] * 0.5f, len[i]) },
                               new List<float> { 0.14f, 0.08f, 0.005f }, 4),
                    i % 2 == 0 ? GrowthVfx.Vine : GrowthVfx.Bark));
        }

        private void SamplePaete(float t)
        {
            float leave = 1 - Ease(Seconds - .45f, Seconds - .05f, t);
            float stage = StagePresence(t, .3f);

            // WHO: ground, mist and sky arrive; the forest stands up out of the ground.
            Tint(_paeteGround, stage); Tint(_paeteMist, stage * (0.8f + 0.2f * Mathf.Sin(t * 2f))); Tint(_paeteSky, stage);
            Place(_paeteMountain, new Vector3(-2.5f, 1.2f, 7.1f), new Vector3(9f, 3.2f * stage, 1f), Quaternion.Euler(0, 180, 8), stage);
            for (int i = 0; i < 8; i++)
            {
                float angle = (180 - 70 + i * 20) * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Sin(angle) * 7.0f, 0, Mathf.Cos(angle) * 7.0f);
                var face = Quaternion.LookRotation(-at.normalized, Vector3.up) * Quaternion.Euler(0, 12 * (i % 3), 0);
                float rise = Ease(.04f + i * .03f, .34f + i * .03f, t);
                float height = 3.2f + (i % 3) * 0.7f;
                float on = stage > .01f ? 1 : 0;
                Place(_makilingTrunks[i * 2], at, new Vector3(.45f + .1f * (i % 2), height * rise, .45f), face, on);
                Place(_makilingTrunks[i * 2 + 1], at + Vector3.up * height * rise, new Vector3(1.6f + .3f * (i % 2), 1.1f * rise, 1.4f), face, on);
            }

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

            // GATHER: the roots break the ground round him, the vine spirals up.
            for (int i = 0; i < _paeteRoots.Count; i++)
            {
                float a = (i * 60 + 15) * Mathf.Deg2Rad;
                float grow = Ease(1.2f + i * .06f, 1.6f + i * .06f, t);
                var at = new Vector3(Mathf.Sin(a) * 1.3f, 0, Mathf.Cos(a) * 1.3f);
                var face = Quaternion.LookRotation(at.normalized, Vector3.up);
                Place(_paeteRoots[i], at, new Vector3(1, Mathf.Max(.001f, grow), 1), face, grow > .001f ? leave : 0);
            }
            float spiral = Ease(1.35f, 2.2f, t) * (1 - Ease(2.65f, 2.85f, t));
            Place(_paeteSpiral, Vector3.zero, new Vector3(1, Mathf.Max(.001f, spiral), 1), Quaternion.Euler(0, t * 120f, 0), spiral > .001f ? leave : 0);

            // RELEASE (2.62 s): the throw; spiked vines erupt down the court.
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
