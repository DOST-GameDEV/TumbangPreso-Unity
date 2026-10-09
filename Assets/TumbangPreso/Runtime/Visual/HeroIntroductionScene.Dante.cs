using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // BASILIO, CONTINENTAL DRIFT, 7.0 s (was TITAN FISSURE, 3.8 s; plan.md § 5 is the first one).
        //
        // ⚠️ RESTAGED 2026-10-08. The owner, of the nine cutscenes after Paete's: a bolder staging idea chosen from
        // options. Of three for Dante he liked two ("dante i like 2 and 3") and agreed to them joined, at about 7 s:
        //   1. THE PUNCH (0 to 1.15): he plants and drives a fist into the court.
        //   2. THE CONTINENTS (1.15 to 3.0), from high above: the court is seven plates of stone on a molten sea, and
        //      they drift apart. This is the ultimate's own name made literal.
        //   3. THE RISE (3.4 to 5.3): out of the widest seam, under him, the horned basalt back shoulders up and lifts
        //      his plate; the plates round it are shoved aside.
        //   4. THE STOMP (5.3 to 7.0, one shot): it raises a fist and he loads both of his, held; they come down as one
        //      at 6.88 and the cutscene ENDS THERE, him still up on its back in the stamp pose.
        // ⚠️ NO BLAST IS SHOWN AND NOTHING GOES BACK UNDER HERE. The first cut (film d2) ran all five blasts and play then
        // fired the real five; asked, the owner: "end on the fist landing", then, of a cut that set him down inside the
        // cutscene: "the cutscene should end exactly when the fist lands before switching back to live first person
        // gameplay, by then dante should still be on the longhorn and only then will it disappeaer into the ground".
        // So the stage does not fade at its end as the others do: it is whole on the last frame, and the going under is
        // LIVE play's to show.
        //
        // What is kept of his earlier asks: basalt and molten seams with horn and claw silhouettes (AGENTS.md, "more
        // demonic skills"), stone that is SOLID and plain, no grain (the noisy basalt texture was refused), an
        // earthquake the lens feels. The two parted slabs and the ridge of the first cutscene are gone: that was
        // Bernardo Carpio's pose, the idea he did not pick this time.
        //
        // ⚠️ THE TITAN HERE IS BLOCKING, built from the shared prisms to judge staging and timing. It is to be
        // replaced by a modelled prop once the staging is agreed (the owner on Paete's props: "its js blocks").
        //
        // ⚠️ EVERY TIME BELOW IS ON THE TABLE'S CLOCK (`tools/author_ultimate_intros.py`, `_dante`): the punch, his lift
        // and the stamp are typed there, and the plate under him is carried by `LiftAt`, so it cannot leave his feet.
        // =========================================================================================
        private const float DnPunch = 1.10f, DnBreak = 1.12f, DnRiseFrom = 3.45f, DnRisen = 5.20f, DnStomp = 6.88f, DnTitanTop = 3.4f, DnFloat = .15f;

        // The centre plate's corners (x, z), then one kink and one far corner per crack running out from each of them.
        private static readonly Vector2[] DnCore =
        { new Vector2(1.55f, .45f), new Vector2(.70f, 1.75f), new Vector2(-.95f, 1.50f), new Vector2(-1.70f, .05f), new Vector2(-.80f, -1.55f), new Vector2(.95f, -1.40f) };
        private static readonly Vector2[] DnKink =
        { new Vector2(5.3f, .6f), new Vector2(1.2f, 5.6f), new Vector2(-3.9f, 4.1f), new Vector2(-5.6f, -.9f), new Vector2(-2.0f, -5.2f), new Vector2(3.7f, -4.3f) };
        private static readonly Vector2[] DnFar =
        { new Vector2(10.6f, 2.6f), new Vector2(3.9f, 10.1f), new Vector2(-7.0f, 8.2f), new Vector2(-10.7f, -.4f), new Vector2(-5.4f, -9.3f), new Vector2(6.6f, -8.5f) };
        // The far edge of each outer plate bulges through one point between its two cracks.
        private static readonly Vector2[] DnRim =
        { new Vector2(8.2f, 7.4f), new Vector2(-1.6f, 11.0f), new Vector2(-10.2f, 4.4f), new Vector2(-9.0f, -6.2f), new Vector2(.8f, -10.9f), new Vector2(10.4f, -3.6f) };
        // Each outer plate's own share of the drift, its tilt once adrift (degrees about x and z) and its turn.
        private static readonly float[] DnShare = { 1.00f, .82f, 1.18f, .90f, 1.10f, .95f };
        private static readonly Vector3[] DnTilt =
        { new Vector3(3.5f, 5f, -2.5f), new Vector3(-4f, -3f, 1.5f), new Vector3(2f, 6f, 4f), new Vector3(-2.5f, -4f, -3.5f), new Vector3(4.5f, 2f, 2f), new Vector3(-3f, -6f, 3f) };

        private int _dnDustLow, _dnDustHigh, _dnSea, _dnSeam, _dnSeamGlow, _dnEyeLeft, _dnEyeRight;
        private readonly List<Transform> _dnPlates = new List<Transform>(7);
        private readonly List<Vector2> _dnPlateHome = new List<Vector2>(7);
        private readonly List<int> _dnPuffs = new List<int>(5);
        private int _dnBlastGlow;
        private Transform _dnTitan, _dnTitanArm;

        private void BuildDante()
        {
            _dnDustLow = Wall("DustGround", 0, 1.4f, new Color(.2f, .14f, .09f, .8f), radius: 11.6f);
            _dnDustHigh = Wall("DustSky", 1.4f, 30, new Color(.42f, .32f, .22f, .72f), radius: 11.6f, emission: .08f, cap: true);
            _dnSea = Add("MoltenSea", VfxShapes.Prism(28, .02f, 1f), new Color(1f, .36f, .05f, 1f), 1f, plain: true);

            // The plates: the centre one he stands on, then six round it, each between two cracks.
            var stone = new Color(.31f, .27f, .24f, 1);
            _dnPlates.Add(DantePlate("PlateCentre", DnCore, stone, out var home)); _dnPlateHome.Add(home);
            for (int i = 0; i < 6; i++)
            {
                int j = (i + 1) % 6;
                var ring = new[] { DnCore[i], DnKink[i], DnFar[i], DnRim[i], DnFar[j], DnKink[j], DnCore[j] };
                var tone = stone * (i % 2 == 0 ? 1f : .9f); tone.a = 1;
                _dnPlates.Add(DantePlate("Plate" + i, ring, tone, out home)); _dnPlateHome.Add(home);
            }

            _dnSeamGlow = Add("MoltenSeamGlow", VfxShapes.Fracture(6, 3, .09f, 21), new Color(1, .42f, .08f, .55f), .8f);
            _dnSeam = Add("MoltenSeam", VfxShapes.Fracture(6, 3, .045f, 21), new Color(1, .62f, .18f, .95f), 1f);
            for (int i = 0; i < 5; i++)
                _dnPuffs.Add(Add("PunchDust" + i, VfxShapes.Splat(10, .25f, 30 + i), new Color(.62f, .52f, .4f, .7f), .05f));

            BuildDanteTitan();

            var strip = new[] { new Vector2(-.5f, -.5f), new Vector2(.5f, -.5f), new Vector2(.5f, .5f), new Vector2(-.5f, .5f) };
            _dnBlastGlow = Add("BlastSeam", DantePlateMesh(strip, Vector2.zero, .02f), new Color(1, .5f, .1f, .9f), 1f);
        }

        /// <summary>The thing under the court, as blocks: a hump, two shoulders, a low head with two horns, two arms.</summary>
        private void BuildDanteTitan()
        {
            _dnTitan = new GameObject("BasaltTitan").transform; _dnTitan.SetParent(_root.transform, false);
            var dark = new Color(.20f, .17f, .16f, 1); var mid = new Color(.26f, .22f, .20f, 1); var ember = new Color(1, .5f, .1f, .95f);
            void Block(string name, Mesh mesh, Color colour, Vector3 at, Vector3 scale, Vector3 euler, Transform parent = null)
            {
                int index = BasilioStone(name, mesh, colour);
                _pieces[index].Transform.SetParent(parent != null ? parent : _dnTitan, false);
                Place(index, at, scale, Quaternion.Euler(euler), 1);
            }
            Block("TitanBack", VfxShapes.Prism(6, 1, .72f, .12f, 0, 11), mid, new Vector3(0, 0, -.3f), new Vector3(2.3f, DnTitanTop, 2.0f), Vector3.zero);
            Block("TitanShoulderLeft", VfxShapes.Prism(5, 1, .8f, .15f, 0, 12), dark, new Vector3(-2.5f, 0, .5f), new Vector3(1.25f, 2.7f, 1.3f), new Vector3(0, -20, 8));
            Block("TitanShoulderRight", VfxShapes.Prism(5, 1, .8f, .15f, 0, 13), dark, new Vector3(2.5f, 0, .5f), new Vector3(1.25f, 2.7f, 1.3f), new Vector3(0, 20, -8));
            Block("TitanHead", VfxShapes.Prism(4, 1, .86f, .04f, 0, 14), mid, new Vector3(0, .35f, 2.35f), new Vector3(1.2f, 1.6f, 1.05f), new Vector3(14, 45, 0));
            Block("TitanBrow", VfxShapes.Prism(4, 1, 1f, 0, 0, 15), dark, new Vector3(0, 1.55f, 2.95f), new Vector3(1.15f, .3f, .5f), new Vector3(20, 45, 0));
            Block("TitanJaw", VfxShapes.Prism(4, 1, .9f, 0, 0, 16), dark, new Vector3(0, .25f, 3.0f), new Vector3(.8f, .55f, .5f), new Vector3(8, 45, 0));
            // Two horns, each a thick root and a tip that turns back up.
            for (int side = -1; side <= 1; side += 2)
            {
                var rootAt = new Vector3(side * 1.0f, 1.45f, 2.35f); var rootTurn = Quaternion.Euler(28, 0, side * -58);
                Block("TitanHornRoot" + side, VfxShapes.Prism(6, 1, .62f, .08f, 0, 17 + side), dark, rootAt, new Vector3(.42f, 1.25f, .42f), rootTurn.eulerAngles);
                Block("TitanHornTip" + side, VfxShapes.Spire(6, .08f, .12f, 19 + side), dark, rootAt + rootTurn * Vector3.up * 1.2f, new Vector3(.27f, 1.15f, .27f),
                      new Vector3(-6, 0, side * -14));
            }
            // Its left arm is planted; its right is the one that comes down.
            Block("TitanArmLeft", VfxShapes.Prism(5, 1, .8f, .1f, 0, 22), mid, new Vector3(-3.0f, 0, 2.2f), new Vector3(.8f, 2.4f, .8f), new Vector3(-14, 0, 6));
            Block("TitanFistLeft", VfxShapes.Prism(5, 1, .9f, .1f, 0, 23), dark, new Vector3(-3.05f, 0, 2.9f), new Vector3(1.0f, .9f, 1.0f), new Vector3(0, 12, 0));
            _dnTitanArm = new GameObject("TitanArmRight").transform; _dnTitanArm.SetParent(_dnTitan, false);
            _dnTitanArm.localPosition = new Vector3(2.75f, 2.5f, .6f);
            Block("TitanForearmRight", VfxShapes.Prism(5, 1, .8f, .1f, 0, 24), mid, new Vector3(0, -2.6f, 0), new Vector3(.82f, 2.6f, .82f), Vector3.zero, _dnTitanArm);
            Block("TitanFistRight", VfxShapes.Prism(5, 1, .9f, .1f, 0, 25), dark, new Vector3(0, -3.35f, 0), new Vector3(1.1f, 1.0f, 1.1f), new Vector3(0, 20, 0), _dnTitanArm);
            _dnEyeLeft = AddGlow("TitanEyeLeft", ember, falloff: 2.2f, core: .5f);
            _dnEyeRight = AddGlow("TitanEyeRight", ember, falloff: 2.2f, core: .5f);
            _pieces[_dnEyeLeft].Transform.SetParent(_dnTitan, false); _pieces[_dnEyeRight].Transform.SetParent(_dnTitan, false);
        }

        /// <summary>One plate of the court: a slab under its own holder, which sits on the plate's middle so it tilts about itself.</summary>
        private Transform DantePlate(string name, Vector2[] ring, Color colour, out Vector2 home)
        {
            home = Vector2.zero;
            foreach (var p in ring) home += p;
            home /= ring.Length;
            var holder = new GameObject(name).transform; holder.SetParent(_root.transform, false);
            int index = BasilioStone(name + "Slab", DantePlateMesh(ring, home, .55f), colour);
            _pieces[index].Transform.SetParent(holder, false);
            Place(index, Vector3.zero, Vector3.one, Quaternion.identity, 1);
            return holder;
        }

        /// <summary>A flat-topped slab from an outline (x, z) round its middle: a top fan and one wall per edge, every face its own.</summary>
        private static Mesh DantePlateMesh(Vector2[] ring, Vector2 middle, float thick)
        {
            var vertices = new List<Vector3>(ring.Length * 9); var triangles = new List<int>(ring.Length * 9);
            void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
            {
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0) { var keep = b; b = c; c = keep; }
                int n = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c);
                triangles.Add(n); triangles.Add(n + 1); triangles.Add(n + 2);
            }
            for (int i = 0; i < ring.Length; i++)
            {
                Vector2 p = ring[i] - middle, q = ring[(i + 1) % ring.Length] - middle;
                Vector3 a = new Vector3(p.x, 0, p.y), b = new Vector3(q.x, 0, q.y), down = Vector3.down * thick;
                Face(Vector3.zero, a, b, Vector3.up);
                var outward = (a + b) * .5f;
                Face(a, b, b + down, outward); Face(a, b + down, a + down, outward);
            }
            var mesh = new Mesh { name = "Basilio court plate" };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        private int BasilioStone(string name, Mesh mesh, Color colour)
        {
            int index = AddSolid(name, mesh, colour);
            // Keep opaque lit facets, with a small local fill so the dusk stage
            // cannot turn the moving slabs into indistinguishable black cutouts.
            var material = _pieces[index].Renderer.sharedMaterial;
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", colour * .45f);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            return index;
        }

        private static float DanteKick(float t, float at, float size, float fade)
            => t < at ? 0 : size * Mathf.Clamp01(1 - (t - at) / fade);

        /// <summary>The lens feels it: the punch, the long grind of the rise, and the stomp it ends on.</summary>
        private Vector3 DanteShake(float t)
        {
            float size = DanteKick(t, DnPunch, .05f, .3f) + DanteKick(t, DnStomp, .12f, .45f)
                       + .016f * Ease(DnRiseFrom, DnRiseFrom + .3f, t) * (1 - Ease(DnRisen - .1f, DnRisen + .2f, t));
            return new Vector3(Mathf.Sin(t * 71f), Mathf.Sin(t * 93f + 1.3f), 0) * size;
        }

        private void SampleDante(float t)
        {
            // Whole to the last frame: nothing of his stage leaves before the hand-back (the header says why).
            const float leave = 1f;
            float haze = Ease(.3f, 1.0f, t) * leave;
            Tint(_dnDustLow, haze); Tint(_dnDustHigh, haze);

            float lift = LiftAt(t);
            bool broken = t >= DnBreak;
            const float shut = 0f;
            // How far the plates stand off: a hairline as the cracks run, adrift on the sea, then shouldered aside by what rises.
            float adrift = Ease(DnBreak, 1.5f, t) * .16f + Ease(1.5f, 3.3f, t) * .72f;
            float shoved = Ease(DnRiseFrom, DnRiseFrom + .95f, t) * 2.3f;
            float open = (adrift + shoved) * (1 - shut);
            float sink = (1 - leave) * .7f;

            // The sea under everything, breathing.
            float glow = _reducedEffects ? .85f : .82f + .18f * Mathf.Sin(t * 5.2f);
            Place(_dnSea, new Vector3(0, .012f - sink, 0), new Vector3(11.3f, 1, 11.3f), Quaternion.identity, broken ? glow * leave : 0);

            for (int i = 0; i < _dnPlates.Count; i++)
            {
                var plate = _dnPlates[i]; var home = _dnPlateHome[i];
                plate.gameObject.SetActive(broken && leave > .002f);
                if (i == 0)
                {
                    // His own plate rides the thing's back.
                    plate.localPosition = new Vector3(home.x, DnFloat + lift - sink, home.y);
                    plate.localRotation = Quaternion.Euler(Mathf.Sin(t * 2.3f) * 1.2f * Mathf.Clamp01(lift), 0, Mathf.Sin(t * 1.9f + 1) * 1.2f * Mathf.Clamp01(lift));
                    continue;
                }
                int k = i - 1;
                Vector2 away = home.normalized * open * DnShare[k];
                float loose = Mathf.Clamp01(open / .9f), heel = Mathf.Clamp01(shoved / 2.3f) * (1 - shut);
                float bob = Mathf.Sin(t * 1.4f + k * 1.7f) * .035f * loose;
                // ⚠️ The real court is solid at 0 and the sea lies on it, so a plate may never dip: it floats a hand above the
                // sea and its rock is kept small enough that its lowest corner (5 m out) stays over it (film d1 tilted them
                // 5 degrees and half of every plate went under the sea).
                plate.localPosition = new Vector3(home.x + away.x, DnFloat + bob + heel * .42f - sink, home.y + away.y);
                // Adrift it rocks a little; shoved aside, its near edge is lifted by the shoulders.
                var rock = DnTilt[k] * .24f * loose * (.6f + .4f * Mathf.Sin(t * 1.1f + k));
                var heeled = Quaternion.AngleAxis(-4f * heel, new Vector3(-home.y, 0, home.x).normalized);
                plate.localRotation = heeled * Quaternion.Euler(rock.x, rock.y * .4f, rock.z);
            }

            // The punch: seams split out from his fist before the plates take over, and dust kicks up.
            float crack = Ease(DnPunch - .02f, DnPunch + .28f, t), crackOff = 1 - Ease(1.45f, 1.7f, t);
            var fist = new Vector3(.15f, .05f, .85f);
            var crackScale = Vector3.one * Mathf.Lerp(.3f, 3.4f, crack);
            Place(_dnSeamGlow, fist + Vector3.down * .005f, crackScale * 1.12f, Quaternion.identity, crack * crackOff * leave * .8f);
            Place(_dnSeam, fist, crackScale, Quaternion.identity, crack * crackOff * leave);
            for (int i = 0; i < _dnPuffs.Count; i++)
            {
                float age = t - DnPunch, u = Mathf.Clamp01(age / .5f);
                float angle = (i * 72 + 20) * Mathf.Deg2Rad;
                var at = fist + new Vector3(Mathf.Cos(angle) * (.4f + u * .8f), u * .3f, Mathf.Sin(angle) * (.4f + u * .8f));
                Place(_dnPuffs[i], at, Vector3.one * (.25f + u * .4f), Quaternion.Euler(0, i * 40, 0), age >= 0 ? (1 - u) * leave * .8f : 0);
            }

            // The thing under the court: it comes up exactly as far as he is lifted, shuddering, and goes back with him.
            bool risen = lift > .01f;
            _dnTitan.gameObject.SetActive(risen);
            if (risen)
            {
                float grind = _reducedEffects ? 0 : Mathf.Sin(t * 41f) * .03f * (1 - Ease(DnRisen - .1f, DnRisen + .2f, t));
                _dnTitan.localPosition = new Vector3(grind, lift - DnTitanTop, 0);
                // Its right arm: planted, raised overhead through his load, held trembling, brought down on the stomp.
                float raised = Ease(5.5f, 6.3f, t) * (1 - Ease(DnStomp - .1f, DnStomp, t));
                float tremble = _reducedEffects ? 0 : Mathf.Sin(t * 33f) * 1.6f * raised;
                _dnTitanArm.localRotation = Quaternion.Euler(Mathf.Lerp(-40f, -152f, raised) + tremble, 0, Mathf.Lerp(0, -10f, raised));
                float eyes = Ease(4.3f, 4.8f, t) * (.8f + .5f * Flash(t, DnStomp, .2f)) * leave;
                PlaceGlow(_dnEyeLeft, new Vector3(-.42f, 1.22f, 3.3f), new Vector3(.55f, .2f, 1), Quaternion.identity, eyes);
                PlaceGlow(_dnEyeRight, new Vector3(.42f, 1.22f, 3.3f), new Vector3(.55f, .2f, 1), Quaternion.identity, eyes);
            }
            // Where its fist lands: a lit seam across the court in front of it, on the last frames.
            {
                float age = t - DnStomp;
                Place(_dnBlastGlow, new Vector3(2.75f, DnFloat + .1f, 3.4f), new Vector3(3.2f * (1 + 4f * Mathf.Clamp01(age / .1f)), 1, .5f), Quaternion.identity,
                      age >= 0 ? 1 : 0);
            }
        }
    }
}
