using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // PAETE, MAKILING'S EMBRACE. The four beats of `docs/reports/amihan-kit-2026-09-25/direction.md`
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
        //   GATHER   bark roots break the ground in a ring round him and curl in (nothing climbs HIM: the
        //            caster is never rooted); the trees lean in; the seed swells.
        //   RELEASE  he throws; the seed arcs down the court and bark spikes burst where it lands.
        //   PAYOFF   (3.05 to 4.6 s) the full 9 m tree screws up out of the court there, its crown opening
        //            and the light coming on in its hollows, the camera tilting up at it past his shoulder.
        //
        // ⚠️ v3 (same day): MARIANG MAKILING WATCHES OVER HIM (owner: *"make it seem like the spirit of maria
        // makiling or smth is watching over him"*, Aphelios and Alune as the model). She rises out of the
        // mist behind him, a tall spirit with a glowing edge, head bowed to him, her deer beside her; the
        // seed of light is in HER cupped hands first and she gives it to him (the painting's gesture).
        //
        // The only lights are the seed and the trees' eyes.
        // =========================================================================================
        private int _paeteGround, _paeteMist, _paeteSky, _paeteMountain;
        private readonly List<PaeteForestTree> _makiling = new List<PaeteForestTree>(4);
        private readonly List<int> _paeteRoots = new List<int>(8);
        private readonly List<int> _paeteLeaves = new List<int>(14);
        private readonly List<int> _paeteBurst = new List<int>(8);
        private int _paeteSeed, _paeteVeinLight;
        private readonly List<PaeteRope> _paeteVeins = new List<PaeteRope>(3);
        private readonly List<Vector3> _paeteVein = new List<Vector3>(40);
        private MakilingSpirit _makilingSpirit;
        private PaeteForestTree _payoffTree;
        private readonly List<int> _makilingMotes = new List<int>(14);
        // Her motes: fireflies of his jade light rising round her (the Alune pictures' sparks, the painting's
        // fireflies). Each: angle round her, distance out, starting height, rise speed, size. Typed.
        private static readonly Vector4[] MoteRows =
        {
            new Vector4(10f, 0.9f, 0.4f, 0.55f), new Vector4(48f, 1.3f, 1.1f, 0.42f), new Vector4(95f, 1.0f, 0.2f, 0.62f),
            new Vector4(140f, 1.5f, 1.6f, 0.38f), new Vector4(182f, 0.8f, 0.8f, 0.50f), new Vector4(221f, 1.2f, 2.2f, 0.46f),
            new Vector4(262f, 1.6f, 0.5f, 0.58f), new Vector4(300f, 1.0f, 1.4f, 0.40f), new Vector4(333f, 1.4f, 2.8f, 0.52f),
            new Vector4(70f, 1.7f, 3.0f, 0.36f), new Vector4(200f, 1.8f, 3.4f, 0.44f), new Vector4(160f, 0.7f, 2.0f, 0.60f),
        };
        private static readonly float[] MoteSize = { .07f, .05f, .08f, .045f, .06f, .05f, .075f, .04f, .06f, .05f, .045f, .065f };
        // Where the thrown seed lands in the staged court, and the tree that rises there.
        private static readonly Vector3 PaeteLanding = new Vector3(0f, 0f, 5.5f);

        // The forest: compass angle round him (180 is straight behind), distance, size, when it rises,
        // when its eyes open. Each tree its own row.
        private static readonly (float angle, float distance, float scale, float rise, float wake)[] MakilingTrees =
        {
            (146f, 6.3f, 0.80f, 0.02f, 0.72f), (171f, 5.4f, 0.66f, 0.11f, 0.96f),
            (197f, 5.9f, 0.90f, 0.19f, 1.16f), (223f, 6.6f, 0.72f, 0.27f, 1.34f),
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
            // A dusk forest canopy, darker than v2, so the spirit glows against it as Alune does against the night.
            _paeteSky = Wall("PaeteSky", 2.2f, 12, new Color(0.16f, 0.22f, 0.16f, 0.82f), emission: 0.18f, cap: true);
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

            // Her motes (see `MoteRows`).
            for (int i = 0; i < MoteRows.Length; i++)
                _makilingMotes.Add(Add("MakilingMote" + i, GrowthVfx.Leaf(1f, 1f, 0.6f), new Color(0.72f, 1f, 0.66f, 0.9f), 1.1f, plain: true));

            // CONNECT and SUMMON: three root veins from under his hands to the landing, and the light at their front.
            var veinBark = new[] { GrowthVfx.BarkDark, GrowthVfx.Bark, GrowthVfx.BarkLit };
            for (int i = 0; i < 3; i++)
                _paeteVeins.Add(new PaeteRope(_root.transform, "PaeteIntroVein" + i, veinBark, new[] { 0.060f, 0.052f, 0.046f }, 0.065f, 5.5f, i * 2.1f));
            _paeteVeinLight = Add("PaeteVeinLight", GrowthVfx.Leaf(1f, 0.75f, 0.6f), GrowthVfx.Glow, 1.2f, plain: true);

            // THE PAYOFF: the full-size tree (the live sentry's model at its size) rises where the seed lands.
            _payoffTree = new PaeteForestTree(_root.transform, PaeteLanding, 180f, PaeteSentryBody.Scale, 2.95f, 3.55f);

            // Mariang Makiling, close behind him and over his LEFT shoulder as Alune stands (direction.md section
            // 5.10), 1.3 times her 3 m model: her head just above his, looming, not a tower far off. (v3 stood her over the right shoulder, where the release shot's
            // camera sits, and that shot filmed the inside of her.)
            _makilingSpirit = new MakilingSpirit(_root.transform, new Vector3(-0.6f, 0f, -1.2f), 12f, 1.3f);

            // The seed in his palm: the one light he holds.
            _paeteSeed = Add("PaeteSeed", GrowthVfx.Leaf(1f, 0.75f, 0.6f), GrowthVfx.Glow, 0.9f, plain: true);

            // Leaves drawn in toward the seed, each its own size.
            for (int i = 0; i < 12; i++)
                _paeteLeaves.Add(AddSolid("PaeteIntroLeaf" + i, GrowthVfx.Leaf(0.16f + 0.02f * (i % 3), 0.09f, 0.015f),
                                          i % 2 == 0 ? GrowthVfx.LeafGreen : GrowthVfx.LeafDark));

            // The ring of roots: six bark roots breaking the ground and CURLING IN toward him, grasping, each
            // its own height and hook. (v2 stood them up straight at 1.1 to 1.9 m and the film read them as
            // horns round him.) Each is authored along its +Z, which faces AWAY from him, so the curl runs -Z.
            float[] h = { 0.95f, 0.70f, 1.10f, 0.80f, 1.00f, 0.75f };
            float[] hook = { 0.42f, 0.30f, 0.50f, 0.36f, 0.46f, 0.32f };
            for (int i = 0; i < h.Length; i++)
                _paeteRoots.Add(AddSolid("PaeteIntroRoot" + i,
                    GrowthTube(new List<Vector3> { new Vector3(0, -0.2f, 0.10f), new Vector3(0, h[i] * 0.45f, 0.06f), new Vector3(0, h[i] * 0.85f, -hook[i] * 0.45f),
                                                   new Vector3(0, h[i], -hook[i]), new Vector3(0, h[i] * 0.9f, -hook[i] * 1.25f) },
                               new List<float> { 0.15f, 0.11f, 0.07f, 0.035f, 0.008f }, 4),
                    i % 2 == 0 ? GrowthVfx.BarkDark : GrowthVfx.Bark));

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
            // The trees lean in toward him while he is connected, and straighten as he rises.
            float lean = 8f * Ease(1.5f, 2.2f, t) * (1f - Ease(2.62f, 3.0f, t));
            foreach (var tree in _makiling) tree.Pose(t, lean, stage);

            // MAKILING: she rises out of the mist behind him through WHO, bows her head to him, gives him the
            // seed at 0.58 s (it arrives in his palm as his own seed lights), lifts her hands with the
            // gather, and sinks back into the mist as the stage leaves.
            var palmWorld = _root.transform.TransformPoint(BothPalms + new Vector3(0, .06f, .06f));
            // She sinks as the gather ends and is gone by the cut to the release (2.5 s): over his shoulder she
            // would fill the lens.
            float spirit = Ease(.12f, 1.0f, t) * (1 - Ease(2.3f, 2.5f, t));
            _makilingSpirit?.Pose(t, spirit, .58f, palmWorld, Ease(1.5f, 2.2f, t) * (1 - Ease(2.62f, 3.0f, t)));
            // Fireflies of jade light rising round her while she is there, each on its own loop.
            var her = new Vector3(-0.6f, 0f, -1.2f);
            for (int i = 0; i < _makilingMotes.Count; i++)
            {
                var row = MoteRows[i];
                float a = (row.x + t * 22f * (i % 2 == 0 ? 1f : -1f)) * Mathf.Deg2Rad;
                float y = Mathf.Repeat(row.z + t * row.w, 4.2f);
                var at = her + new Vector3(Mathf.Sin(a) * row.y, y, Mathf.Cos(a) * row.y);
                float twinkle = 0.6f + 0.4f * Mathf.Sin(t * 7f + i * 1.9f);
                float fadeTop = 1f - Mathf.Clamp01((y - 3.6f) / 0.6f);
                Place(_makilingMotes[i], at, Vector3.one * MoteSize[i] * twinkle, Quaternion.Euler(t * 80f + i * 30f, t * 120f, 0f), spirit * fadeTop);
            }

            // THE GIFT: her light arrives in his cupped hands at 0.8 s and glows there; at the CONNECT (1.5 s) he
            // presses it into the court and it goes into the ground under his palms (no throw, owner 2026-09-26).
            var palm = BothPalms + new Vector3(0, .06f, .06f);
            float seed = Ease(.72f, .82f, t) * (1 - Ease(1.5f, 1.62f, t));
            float swell = 0.07f + 0.06f * Ease(.82f, 1.45f, t);
            var seedAt = Vector3.Lerp(palm, new Vector3(palm.x, 0.05f, palm.z), Ease(1.42f, 1.55f, t));
            Place(_paeteSeed, seedAt, Vector3.one * swell, Quaternion.Euler(t * 90f, t * 140f, 0), seed);
            // The veins: out from under his hands at 1.6 s, racing to the landing by 2.62 s as he rises, then
            // sinking into the court as the tree comes up.
            var under = new Vector3(palm.x, 0.05f, palm.z);
            float reach = Ease(1.6f, 2.62f, t);
            float sunk = Ease(3.0f, 3.5f, t);
            for (int v = 0; v < _paeteVeins.Count; v++)
            {
                _paeteVein.Clear();
                int shown = Mathf.RoundToInt(36 * reach);
                Vector3 side = Vector3.Cross(Vector3.up, (PaeteLanding - under).normalized);
                for (int k = 0; k <= shown && shown > 0; k++)
                {
                    float u = k / 36f;
                    float wander = (v - 1) * 0.35f * Mathf.Sin(u * Mathf.PI) + 0.18f * Mathf.Sin(u * 9f + v * 2f) * Mathf.Sin(u * Mathf.PI);
                    _paeteVein.Add(Vector3.Lerp(under, PaeteLanding, u) + side * wander + Vector3.up * (0.04f - 0.3f * sunk));
                }
                if (_paeteVein.Count < 2 || sunk >= 1f) _paeteVeins[v].Clear(); else _paeteVeins[v].Draw(_paeteVein, 0.6f);
            }
            var front = Vector3.Lerp(under, PaeteLanding, reach) + Vector3.up * 0.14f;
            Place(_paeteVeinLight, front, Vector3.one * 0.2f * (1f + 0.3f * Mathf.Sin(t * 30f)), Quaternion.Euler(0, t * 400f, 0),
                  Ease(1.58f, 1.7f, t) * (1 - Ease(2.62f, 2.8f, t)));
            for (int i = 0; i < _paeteLeaves.Count; i++)
            {
                float a = i * 2.39996f + t * 1.6f;
                float pull = Ease(.6f, 1.6f, t);
                float r = Mathf.Lerp(2.4f + (i % 3) * .5f, .25f, pull);
                var at = palm + new Vector3(Mathf.Cos(a) * r, (i % 4) * .18f - .2f + (1 - pull) * .8f, Mathf.Sin(a) * r);
                float on = Ease(.55f, .8f, t) * (1 - Ease(1.6f, 1.75f, t)) * leave;
                Place(_paeteLeaves[i], at, Vector3.one, Quaternion.Euler(30 + i * 20, a * Mathf.Rad2Deg, i * 15), on);
            }

            // GATHER: the roots break the ground round him and curl in. ⚠️ NOTHING CLIMBS HIM (owner, 2026-09-26:
            // *"it shouldnt root the caster"*): v2's woven branches up his legs read as the caster rooted.
            for (int i = 0; i < _paeteRoots.Count; i++)
            {
                float a = (i * 60 + 15) * Mathf.Deg2Rad;
                float grow = Ease(1.5f + i * .04f, 1.8f + i * .04f, t) * (1 - Ease(2.5f + i * .03f, 2.75f + i * .03f, t));
                var at = new Vector3(Mathf.Sin(a) * 1.1f, 0, Mathf.Cos(a) * 1.1f);
                var face = Quaternion.LookRotation(at.normalized, Vector3.up) * Quaternion.Euler(8f * Mathf.Sin(t * 9f + i), 10f * Mathf.Sin(t * 6f + i * 2f), 0f);
                Place(_paeteRoots[i], at, new Vector3(1, Mathf.Max(.001f, grow), 1), face, grow > .001f ? leave : 0);
            }
            // PAYOFF: the tree screws up out of the court, its crown opens, the light comes on in its hollows.
            _payoffTree?.Pose(t, 0f, stage > .01f ? 1f : 0f);

            // RISE (2.62 s): bark spikes erupt round where the roots arrive, and the tree follows.
            for (int i = 0; i < _paeteBurst.Count; i++)
            {
                float go = Ease(2.95f + i * .025f, 3.25f + i * .025f, t);
                float spread = (-35 + i * 10) * Mathf.Deg2Rad;
                var at = PaeteLanding + new Vector3(Mathf.Sin(spread) * 1.4f, 0, Mathf.Cos(spread) * 1.4f);
                var face = Quaternion.Euler(0, spread * Mathf.Rad2Deg, 0);
                Place(_paeteBurst[i], at, Vector3.one * Mathf.Max(.001f, GrowthVfx.Pop(go)), face, go > .001f ? leave : 0);
            }
        }
    }
}
