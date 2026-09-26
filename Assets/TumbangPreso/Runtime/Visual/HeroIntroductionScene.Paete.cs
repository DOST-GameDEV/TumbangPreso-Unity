using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // PAETE, MAKILING'S EMBRACE. ⚠️⚠️ v4 (2026-09-26 night), REDIRECTED AFTER HIS OWN SCREEN'S FILM.
        // The owner on `paete_ultimate_v3.mp4`: *"ur direction of the entire cutscene sucks"*, *"i dont get what
        // the 3 plants showing up and the big plant showing up means"*, *"make his eyes glow or smth"*, *"how she
        // looks needs to be refined"*; then *"thoroughly think abt hhow everythjingh should look and direct his
        // cutscene"* and *"makiling needs to be see thru tho okay? like a spirit thats js watching over"*.
        // The shot list is `docs/reports/paete-kit-2026-09-25/direction.md` section 5.13; the body and the three
        // shots are `tools/author_ultimate_intros.py` `paete`.
        //
        // ONE LIGHT, ONE JOURNEY. Everything on screen is one light travelling: it leaves Mariang Makiling's
        // hands, falls into his open hand, ignites his eyes, goes down his arm into the court, races under it as
        // three veins, gathers where the guardian will stand, and comes up as the guardian's eyes. Nothing else
        // appears. It always travels screen LEFT to RIGHT: she stands behind his RIGHT shoulder and every camera
        // stays on that side of the line between them.
        //
        //   CALL     0 to 1.4    she rises out of the mist behind him and bends over him; the light falls from her
        //                        parted hands into his open left hand (0.72 to 0.86); HIS EYES IGNITE (0.95) and
        //                        stay lit to the end.
        //   CONNECT  1.4 to 2.7  he drops to a knee and drives his palm into the court (1.5): the court cracks and
        //                        heaves, roots burst round his hand and dive back into the ground, and three veins
        //                        of light race under the road to the landing (1.55 to 2.55); she bends low with him.
        //   RISE     2.7 to 4.6  the light gathers in a pool at the landing; he rises, arms high; the court breaks
        //                        (3.0) and the guardian, the live tree itself, screws up to its full 9 m as he
        //                        heaves it out of the ground; she straightens to watch it and the mist takes her
        //                        back; the last beat is the guardian's eyes lighting (3.98).
        //
        // ⚠️ CUT, AND WHY: the four small woven trees (they read as copies of HIM, and the owner could not tell what
        // they meant), the stage cylinder's walls, sky, ground and mountain (their rim showed as a band across the
        // real sky; the payoff happens in the real court, so the whole scene does), the bark spikes at the landing
        // (the tree's own ground break says it), the deer. Every piece here is posed from the scene clock `t`:
        // during the shared phase the world is paused (`Time.timeScale` 0), so nothing may run on `Update`.
        // =========================================================================================

        // Where things stand, in the caster's space (+z ahead of him, +x to his right).
        private static readonly Vector3 PaeteLanding = new Vector3(0f, 0f, 5.5f);
        private static readonly Vector3 MakilingStand = new Vector3(1.0f, 0f, -1.2f);
        private const float MakilingYaw = -14f, MakilingScale = 1.1f;
        // The guardian faces out toward the rise shot's camera, a little more than a quarter turn from him, so
        // its hollows are seen lighting (straight at him it would be in profile).
        private const float PaeteTreeYaw = 105f;
        private const float PaeteEruptAt = 3.0f, PaeteTreeWakeAge = 0.98f;

        // The light's colours: his lime (the eye light, `D8FF6A`), a hotter near-white core, her jade.
        private static readonly Color PaeteLight = new Color(0.85f, 1.0f, 0.42f, 1f);
        private static readonly Color PaeteHot = new Color(1.0f, 1.0f, 0.78f, 1f);
        private static readonly Color MakilingJade = new Color(0.62f, 1.0f, 0.70f, 1f);

        private MakilingSpirit _makilingSpirit;
        private PaeteSentryBody _paeteTree;
        private readonly List<int> _mist = new List<int>(8);
        private readonly List<int> _fireflies = new List<int>(8);
        private int _giftLight, _giftHalo, _palmGlow, _poolGlow, _poolCrack, _eruptFlash;
        private readonly List<int> _giftTrail = new List<int>(5);
        private readonly int[] _paeteEyeCore = new int[2], _paeteEyeHalo = new int[2], _paeteEyeStreak = new int[2];
        private int _slamCrack, _slamHeave, _slamDust, _eruptDust;
        private readonly List<int> _slamChunks = new List<int>(9);
        private readonly List<int> _eruptChunks = new List<int>(12);
        private readonly List<int> _crownLeaves = new List<int>(14);
        private readonly List<MeshFilter> _paeteRoots = new List<MeshFilter>(6);
        private readonly List<int> _veinCrack = new List<int>(3), _veinGlow = new List<int>(3), _veinHead = new List<int>(3);
        private readonly List<Mesh> _veinCrackMesh = new List<Mesh>(3), _veinGlowMesh = new List<Mesh>(3);
        private readonly List<int> _veinPulse = new List<int>(9), _veinKick = new List<int>(6);
        private readonly List<Vector3> _veinLine = new List<Vector3>(48);
        private readonly List<Vector3> _rootLine = new List<Vector3>(16);
        private readonly List<float> _rootRadii = new List<float>(16);
        private PaeteEyes _paeteEyes;
        // Where his palm meets the court, read off the pose while it is down (1.5 to 2.3 s) and held after.
        private Vector3 _paetePalmGround = new Vector3(-0.75f, 0.02f, 0.35f);
        // ⚠️ THE COURT'S VISIBLE TOP ABOVE THE SCENE ROOT (film r12: the veins, the palm crack and the pool, laid 1 to 2 cm
        // over the root, were all under the plaza's court surface and never drew). Measured once with the same probe the
        // live veins use (`Slipper.GroundY`), and every flat piece sits a few centimetres over it.
        private float _paeteCourt;

        // ------------------------------------------------------------------ typed tables

        // Her mist: soft pools on the court round her hem (flat) and puffs about it (facing the lens). Each: angle
        // round her (degrees), distance out, height, size, how fast it turns.
        private static readonly Vector4[] MistRows =
        {
            new Vector4(20f, 0.55f, 0.02f, 2.3f), new Vector4(150f, 0.70f, 0.03f, 1.9f), new Vector4(260f, 0.45f, 0.02f, 2.6f),
            new Vector4(75f, 0.95f, 0.35f, 1.1f), new Vector4(205f, 0.85f, 0.25f, 1.3f), new Vector4(320f, 0.90f, 0.45f, 1.0f),
            new Vector4(115f, 0.60f, 0.60f, 0.9f),
        };
        // Her fireflies: angle round her, distance out, starting height, rise speed; each its own size.
        private static readonly Vector4[] FireflyRows =
        {
            new Vector4(10f, 0.9f, 0.4f, 0.55f), new Vector4(62f, 1.3f, 1.1f, 0.42f), new Vector4(118f, 1.0f, 0.2f, 0.62f),
            new Vector4(171f, 1.5f, 1.6f, 0.38f), new Vector4(226f, 1.1f, 0.8f, 0.50f), new Vector4(282f, 1.4f, 2.2f, 0.46f),
            new Vector4(333f, 1.2f, 2.8f, 0.52f), new Vector4(200f, 1.7f, 3.3f, 0.44f),
        };
        private static readonly float[] FireflySize = { .09f, .07f, .10f, .065f, .08f, .07f, .09f, .06f };

        // The roots that burst round his palm (1.5 s): each rises out of the court near the hand, arches over and
        // DIVES BACK INTO the court further out (owner, of the tree: *"make it look like the roots GO INT he
        // ground not float off of it"*). Each: compass out from the palm (degrees, 0 = ahead), where it breaks
        // out (m from the palm), how far it reaches, how high it arches, girth, when it bursts.
        private static readonly (float compass, float from, float reach, float arch, float girth, float burst)[] RootRows =
        {
            (-20f, 0.10f, 0.95f, 0.46f, 0.090f, 1.50f), (38f, 0.14f, 0.80f, 0.34f, 0.075f, 1.53f), (95f, 0.12f, 0.70f, 0.40f, 0.080f, 1.52f),
            (160f, 0.16f, 0.62f, 0.28f, 0.065f, 1.56f), (-105f, 0.12f, 0.85f, 0.42f, 0.085f, 1.51f), (-160f, 0.15f, 0.66f, 0.30f, 0.070f, 1.55f),
        };

        // The three veins from his palm to the landing: sideways wander (m) at the middle, a second wave's size and
        // phase, girth, and a small delay each so they are not one line tripled.
        private static readonly (float bow, float wave, float phase, float width, float delay)[] VeinRows =
        {
            (0.55f, 0.16f, 0.4f, 0.30f, 0.00f), (-0.42f, 0.12f, 2.1f, 0.26f, 0.05f), (0.08f, 0.20f, 4.0f, 0.22f, 0.09f),
        };

        // Chunks of court thrown up by the slam: (direction round the palm, out speed, up speed, size, spin).
        private static readonly Vector4[] SlamChunkRows =
        {
            new Vector4(12f, 1.4f, 2.6f, 0.10f), new Vector4(58f, 1.9f, 2.1f, 0.07f), new Vector4(104f, 1.2f, 3.0f, 0.09f),
            new Vector4(150f, 1.7f, 2.4f, 0.06f), new Vector4(196f, 1.5f, 2.8f, 0.08f), new Vector4(244f, 2.0f, 1.9f, 0.07f),
            new Vector4(290f, 1.3f, 3.2f, 0.11f), new Vector4(330f, 1.8f, 2.2f, 0.06f), new Vector4(80f, 0.9f, 3.6f, 0.05f),
        };
        // And by the eruption, bigger and further.
        private static readonly Vector4[] EruptChunkRows =
        {
            new Vector4(8f, 3.2f, 5.4f, 0.22f), new Vector4(40f, 4.1f, 4.2f, 0.16f), new Vector4(75f, 2.6f, 6.0f, 0.20f),
            new Vector4(110f, 3.7f, 4.8f, 0.14f), new Vector4(142f, 2.9f, 5.6f, 0.24f), new Vector4(178f, 4.4f, 3.8f, 0.15f),
            new Vector4(212f, 3.1f, 5.1f, 0.19f), new Vector4(246f, 3.9f, 4.5f, 0.13f), new Vector4(279f, 2.7f, 6.2f, 0.21f),
            new Vector4(305f, 4.2f, 4.0f, 0.17f), new Vector4(333f, 3.4f, 5.0f, 0.15f), new Vector4(355f, 2.4f, 6.6f, 0.12f),
        };
        // Leaves blown off the crown as it tops out (3.56 s): direction, out speed, up speed, size.
        private static readonly Vector4[] CrownLeafRows =
        {
            new Vector4(0f, 2.2f, 2.4f, 0.26f), new Vector4(26f, 3.0f, 1.6f, 0.22f), new Vector4(52f, 2.5f, 2.9f, 0.28f),
            new Vector4(79f, 3.4f, 1.2f, 0.20f), new Vector4(104f, 2.0f, 3.2f, 0.24f), new Vector4(131f, 2.8f, 2.0f, 0.27f),
            new Vector4(157f, 3.6f, 1.4f, 0.21f), new Vector4(183f, 2.3f, 2.6f, 0.25f), new Vector4(210f, 3.1f, 1.8f, 0.23f),
            new Vector4(236f, 2.6f, 3.0f, 0.26f), new Vector4(262f, 3.3f, 1.5f, 0.20f), new Vector4(289f, 2.1f, 2.7f, 0.28f),
            new Vector4(314f, 2.9f, 2.2f, 0.22f), new Vector4(340f, 3.5f, 1.3f, 0.24f),
        };

        private static Mesh GrowthTube(IList<Vector3> points, IList<float> radii, int sides)
        {
            var mesh = new Mesh { name = "PaeteIntroTube" };
            PaeteInk.Tube(mesh, points, radii, sides);
            return mesh;
        }

        private void BuildPaete()
        {
            _paeteCourt = Mathf.Clamp(Slipper.GroundY(_root.transform.position + Vector3.up * .3f) - _root.transform.position.y, -.2f, .3f);
            _paetePalmGround.y = _paeteCourt + .02f;
            // MARIANG MAKILING, behind his RIGHT shoulder, turned a little toward him (direction.md 5.13).
            _makilingSpirit = new MakilingSpirit(_root.transform, MakilingStand, MakilingYaw, MakilingScale);
            for (int i = 0; i < MistRows.Length; i++)
                _mist.Add(AddGlow("MakilingMist" + i, new Color(0.80f, 1.0f, 0.86f, 1f), billboard: i >= 3, falloff: 1.6f, core: 0f));
            for (int i = 0; i < FireflyRows.Length; i++)
                _fireflies.Add(AddGlow("MakilingFirefly" + i, MakilingJade, falloff: 2.4f, core: 0.8f));

            // THE LIGHT she gives him, its halo, and the short trail it leaves as it falls.
            _giftLight = AddGlow("PaeteGiftLight", PaeteHot, falloff: 2.0f, core: 1.2f, lift: .05f);
            _giftHalo = AddGlow("PaeteGiftHalo", PaeteLight, falloff: 1.4f, core: 0f, lift: .05f);
            for (int i = 0; i < 5; i++) _giftTrail.Add(AddGlow("PaeteGiftTrail" + i, PaeteLight, falloff: 2.2f, core: 0.4f));
            _palmGlow = AddGlow("PaetePalmGlow", PaeteLight, falloff: 1.8f, core: 0.8f, lift: .06f);

            // HIS EYES: a hot core, a soft halo and an anamorphic streak for each, found on his own face.
            _paeteEyes = PaeteEyes.Find(_bodyRenderers, _head);
            for (int e = 0; e < 2; e++)
            {
                _paeteEyeCore[e] = AddGlow("PaeteEyeCore" + e, new Color(0.80f, 1.0f, 0.55f, 1f), falloff: 2.0f, core: 1.4f, lift: .10f, facing: true);
                _paeteEyeHalo[e] = AddGlow("PaeteEyeHalo" + e, PaeteLight, falloff: 1.5f, core: 0f, lift: .10f, facing: true);
                _paeteEyeStreak[e] = AddGlow("PaeteEyeStreak" + e, PaeteLight, falloff: 1.3f, core: 0.5f, lift: .12f, facing: true);
            }

            // THE SLAM: the court cracking under his palm, its slabs heaving, a ring of dust and thrown chunks.
            var courtDark = new Color(0.20f, 0.14f, 0.09f, 1f);
            _slamCrack = AddSolid("PaeteSlamCrack", VfxShapes.Fracture(6, 3, 0.05f, 91), courtDark);
            _slamHeave = AddSolid("PaeteSlamHeave", VfxShapes.Upheaval(8, 0.08f, 0.30f, 17, 0.05f), GrowthVfx.Seed);
            _slamDust = Add("PaeteSlamDust", VfxShapes.Collar(18, 0.30f, 0.80f, 0.12f, 0f, 5), new Color(0.88f, 0.82f, 0.70f, 0.55f), 0.1f, plain: true);
            for (int i = 0; i < SlamChunkRows.Length; i++)
                _slamChunks.Add(AddSolid("PaeteSlamChunk" + i, VfxShapes.Prism(5, 0.6f, 0.7f, 0.15f, 20f * i, 30 + i), i % 3 == 0 ? courtDark : GrowthVfx.Seed));

            // THE ROOTS that burst round his palm and dive back in (his own bark, inked like the tree).
            var bark = new[] { GrowthVfx.BarkDark, GrowthVfx.Bark, GrowthVfx.BarkLit };
            for (int i = 0; i < RootRows.Length; i++)
                _paeteRoots.Add(PaeteInk.Part(_root.transform, "PaeteIntroRoot" + i, new Mesh { name = "PaeteIntroRoot" }, bark[i % 3]));
            foreach (var f in _paeteRoots) f.sharedMesh.MarkDynamic();

            // THE VEINS: a dark crack in the court and a band of light in it, a bright head, pulses running behind
            // it, and soil kicked up where the head passes.
            for (int v = 0; v < VeinRows.Length; v++)
            {
                var crackMesh = new Mesh { name = "PaeteVeinCrack" }; crackMesh.MarkDynamic();
                _veinCrackMesh.Add(crackMesh);
                _veinCrack.Add(AddSolid("PaeteVeinCrack" + v, crackMesh, courtDark));
                var glowMesh = new Mesh { name = "PaeteVeinGlow" }; glowMesh.MarkDynamic();
                _veinGlowMesh.Add(glowMesh);
                _veinGlow.Add(AddGlow("PaeteVeinGlow" + v, PaeteLight, mesh: glowMesh, billboard: false, band: true, falloff: 1.5f, core: 0.8f));
                _veinHead.Add(AddGlow("PaeteVeinHead" + v, PaeteHot, falloff: 1.8f, core: 1.0f, lift: .1f));
                for (int k = 0; k < 3; k++) _veinPulse.Add(AddGlow("PaeteVeinPulse" + v + k, PaeteLight, falloff: 2.0f, core: 0.6f, lift: .05f));
                for (int k = 0; k < 2; k++) _veinKick.Add(AddSolid("PaeteVeinKick" + v + k, VfxShapes.Prism(5, 0.6f, 0.7f, 0.2f, 0f, 70 + v * 3 + k), GrowthVfx.Seed));
            }

            // THE LANDING: the light pooling where the guardian will stand, the court cracking round it, the flash.
            _poolGlow = AddGlow("PaetePool", PaeteLight, billboard: false, falloff: 1.4f, core: 0.9f);
            _poolCrack = AddSolid("PaetePoolCrack", VfxShapes.Fracture(8, 3, 0.07f, 57), courtDark);
            _eruptFlash = AddGlow("PaeteEruptFlash", PaeteHot, falloff: 1.2f, core: 0.6f, lift: .5f);
            _eruptDust = Add("PaeteEruptDust", VfxShapes.Collar(22, 0.55f, 0.78f, 0.16f, 0f, 11), new Color(0.86f, 0.80f, 0.68f, 0.5f), 0.1f, plain: true);
            for (int i = 0; i < EruptChunkRows.Length; i++)
                _eruptChunks.Add(AddSolid("PaeteEruptChunk" + i, VfxShapes.Prism(5, 0.6f, 0.7f, 0.2f, 15f * i, 50 + i), i % 3 == 1 ? courtDark : GrowthVfx.Seed));
            for (int i = 0; i < CrownLeafRows.Length; i++)
                _crownLeaves.Add(AddSolid("PaeteCrownLeaf" + i, PaeteInk.Leaf(1f, 0.55f, 0.08f), i % 2 == 0 ? PaeteSentryBody.Leaf : PaeteSentryBody.Palette[3]));

            // THE GUARDIAN: the live tree itself, staged (no world effects), facing out toward the rise camera.
            _paeteTree = PaeteSentryBody.Build(_root.transform);
            _paeteTree.transform.localPosition = PaeteLanding;
            _paeteTree.Staged = true;
            _paeteTree.WakeAt = PaeteTreeWakeAge;
            _paeteTree.SetFacing(_root.transform.TransformDirection(Quaternion.Euler(0f, PaeteTreeYaw, 0f) * Vector3.forward));
        }

        /// <summary>The camera's shake for Paete's two blows: the palm (1.5 s) and the eruption (3.0 s), and the crown topping out.</summary>
        private Vector3 PaeteShake(float t)
        {
            float amp = 0.075f * Decay(t - 1.5f, 0.35f) + 0.15f * Decay(t - PaeteEruptAt, 0.6f) + 0.04f * Decay(t - 3.56f, 0.3f);
            return amp * new Vector3(Mathf.Sin(t * 53f), 0.8f * Mathf.Sin(t * 67f + 1f), 0.5f * Mathf.Sin(t * 41f + 2f));
        }

        private static float Decay(float since, float length) => since < 0f || since > length ? 0f : 1f - since / length;

        private void SamplePaete(float t)
        {
            float leave = 1 - Ease(Seconds - .45f, Seconds - .05f, t);

            // ---------------------------------------------------------------- MAKILING, watching over him.
            float rise = Ease(.05f, .60f, t);
            float sink = Ease(1.42f, 1.62f, t) * (1f - Ease(2.40f, 2.75f, t));
            float stand = Ease(2.50f, 3.00f, t);
            var look = new MakilingSpirit.Look
            {
                Presence = rise,
                Drift = new Vector3(0.10f, -0.20f, 0.05f) * sink + new Vector3(0f, 0.25f, 0f) * stand,
                Lean = 10f + 8f * Ease(.30f, .70f, t) - 6f * sink - 12f * stand * (1f - sink),
                Bow = 20f * Ease(.30f, .70f, t) + 6f * sink - 28f * Ease(2.60f, 3.20f, t),
                Turn = -12f * Ease(.30f, .70f, t) + 6f * Ease(2.60f, 3.20f, t),
                Reach = Mathf.Lerp(Mathf.Lerp(0.5f * Ease(.42f, .66f, t), 0.30f, Ease(.90f, 1.30f, t)), 0.62f, sink) * (1f - stand) + 0.36f * stand,
                Open = Mathf.Lerp(Ease(.66f, .76f, t) * (1f - 0.5f * Ease(.95f, 1.30f, t)), 0.3f, sink) * (1f - stand) + stand,
                Wind = 0.3f + 0.9f * Decay(t - 1.5f, 0.6f) + 1.0f * Decay(t - PaeteEruptAt, 0.9f),
                Fade = Ease(3.70f, 4.50f, t),
                Light = Ease(.12f, .42f, t) * (1f - Ease(.70f, .74f, t)),
            };
            _makilingSpirit?.Pose(t, look);
            var her = MakilingStand + look.Drift;
            float herHere = rise * (1f - look.Fade) * leave;
            // Her mist: pools on the court at her hem and puffs round it, turning slowly.
            for (int i = 0; i < _mist.Count; i++)
            {
                var row = MistRows[i];
                float a = (row.x + t * (i % 2 == 0 ? 9f : -7f)) * Mathf.Deg2Rad;
                var at = her + new Vector3(Mathf.Sin(a) * row.y, row.z, Mathf.Cos(a) * row.y);
                float size = row.w * MakilingScale * (0.9f + 0.1f * Mathf.Sin(t * 1.3f + i));
                bool flat = i < 3;
                PlaceGlow(_mist[i], at, flat ? new Vector3(size, size, 1f) : Vector3.one * size,
                          flat ? Quaternion.Euler(90f, 0f, 0f) : Quaternion.identity, (flat ? 0.22f : 0.16f) * herHere);
            }
            for (int i = 0; i < _fireflies.Count; i++)
            {
                var row = FireflyRows[i];
                float a = (row.x + t * 22f * (i % 2 == 0 ? 1f : -1f)) * Mathf.Deg2Rad;
                float y = Mathf.Repeat(row.z + t * row.w, 3.6f);
                var at = her + new Vector3(Mathf.Sin(a) * row.y, y, Mathf.Cos(a) * row.y);
                float twinkle = 0.6f + 0.4f * Mathf.Sin(t * 7f + i * 1.9f);
                float fadeTop = 1f - Mathf.Clamp01((y - 3.0f) / 0.6f);
                PlaceGlow(_fireflies[i], at, Vector3.one * FireflySize[i], Quaternion.identity, 0.9f * twinkle * fadeTop * herHere);
            }

            // ---------------------------------------------------------------- THE LIGHT, from her hands into his.
            var hands = _makilingSpirit != null ? _root.transform.InverseTransformPoint(_makilingSpirit.HandsWorld) : her + new Vector3(0f, 2.2f, 0.4f);
            var palm = FreePalm + new Vector3(0f, .07f, .04f);
            float fall = Ease(.72f, .86f, t);
            Vector3 lightAt = t < .72f ? hands : Vector3.Lerp(hands, palm, fall) + Vector3.up * 0.35f * Mathf.Sin(fall * Mathf.PI);
            // It glows in her hands until it falls, sits in his palm, FLARES as it goes into him (0.95), then is his.
            float held = t < .72f ? Ease(.12f, .42f, t) : 1f;
            float into = Ease(.95f, 1.06f, t);
            float flare = 1f + 0.9f * Mathf.Clamp01(1f - Mathf.Abs(t - .96f) / .05f);
            bool lightOn = t > .72f && t < 1.06f;
            PlaceGlow(_giftLight, lightAt, Vector3.one * 0.28f * flare * (1f - 0.7f * into), Quaternion.identity, lightOn ? held * (1f - into) : 0f);
            PlaceGlow(_giftHalo, lightAt, Vector3.one * 0.95f * flare * (1f - 0.5f * into), Quaternion.identity, lightOn ? 0.45f * (1f - into) : 0f);
            for (int i = 0; i < _giftTrail.Count; i++)
            {
                float back = Ease(.72f, .86f, t - 0.022f * (i + 1));
                var p = Vector3.Lerp(hands, palm, back) + Vector3.up * 0.35f * Mathf.Sin(back * Mathf.PI);
                PlaceGlow(_giftTrail[i], p, Vector3.one * (0.16f - 0.022f * i), Quaternion.identity, t > .72f && t < .9f ? 0.7f - 0.12f * i : 0f);
            }
            // The light that stays in his palm after it has gone into him, and flares as the palm hits the court.
            float palmOn = Ease(.86f, .95f, t) * (1f - 0.55f * Ease(.95f, 1.1f, t)) * (1f - Ease(1.52f, 1.62f, t));
            PlaceGlow(_palmGlow, palm, Vector3.one * (0.34f + 0.25f * Decay(t - 1.5f, 0.12f)), Quaternion.identity,
                      palmOn * (1f + 1.5f * Decay(t - 1.5f, 0.12f)));

            // ---------------------------------------------------------------- HIS EYES ignite and stay lit.
            SamplePaeteEyes(t, leave);

            // ---------------------------------------------------------------- THE SLAM, 1.5 s.
            if (t >= 1.5f && t <= 2.3f) { var g = FreePalm; _paetePalmGround = new Vector3(g.x, _paeteCourt + 0.02f, g.z); }
            var ground = _paetePalmGround;
            float crack = Ease(1.50f, 1.58f, t);
            Place(_slamCrack, ground + Vector3.up * 0.03f, new Vector3(1.15f, 1f, 1.15f) * Mathf.Max(.001f, crack), Quaternion.Euler(0f, 30f, 0f), crack > .001f ? leave : 0f);
            float heave = GrowthVfx.Pop((t - 1.50f) / 0.10f) * (1f - Ease(2.4f, 2.7f, t));
            Place(_slamHeave, ground, new Vector3(0.62f, Mathf.Max(.001f, heave), 0.62f), Quaternion.Euler(0f, 12f, 0f), heave > .001f ? leave : 0f);
            float dust = Ease(1.50f, 1.85f, t);
            Place(_slamDust, ground + Vector3.up * (0.02f + 0.15f * dust), new Vector3(0.3f + 1.6f * dust, 0.8f - 0.4f * dust, 0.3f + 1.6f * dust),
                  Quaternion.identity, t >= 1.5f ? (1f - dust) * leave : 0f);
            for (int i = 0; i < _slamChunks.Count; i++)
                PlaceChunk(_slamChunks[i], ground, SlamChunkRows[i], t - 1.5f, 0.75f, leave);

            // ---------------------------------------------------------------- THE ROOTS round his palm.
            for (int i = 0; i < _paeteRoots.Count; i++) PosePaeteRoot(i, t, ground);

            // ---------------------------------------------------------------- THE VEINS, 1.55 to 2.55.
            for (int v = 0; v < VeinRows.Length; v++) PosePaeteVein(v, t, ground, leave);

            // ---------------------------------------------------------------- THE LANDING and the eruption.
            float pool = Ease(2.45f, 2.95f, t) * (1f - Ease(3.0f, 3.08f, t));
            float beat = 1f + 0.18f * Mathf.Sin(t * Mathf.Lerp(14f, 34f, Ease(2.5f, 3.0f, t)));
            PlaceGlow(_poolGlow, PaeteLanding + Vector3.up * (_paeteCourt + 0.06f), new Vector3(0.6f + 2.0f * pool, 0.6f + 2.0f * pool, 1f) * beat,
                      Quaternion.Euler(90f, 0f, 0f), pool * 0.9f * leave);
            float landCrack = Ease(2.50f, 3.02f, t);
            Place(_poolCrack, PaeteLanding + Vector3.up * (_paeteCourt + 0.045f), new Vector3(1.9f, 1f, 1.9f) * Mathf.Max(.001f, landCrack), Quaternion.Euler(0f, -20f, 0f),
                  landCrack > .001f ? leave : 0f);
            PlaceGlow(_eruptFlash, PaeteLanding + Vector3.up * 1.2f, Vector3.one * 5.5f, Quaternion.identity,
                      _reducedEffects ? 0f : 1.3f * Decay(t - PaeteEruptAt, 0.16f));
            float edust = Ease(PaeteEruptAt, PaeteEruptAt + 0.7f, t);
            Place(_eruptDust, PaeteLanding + Vector3.up * (0.05f + 0.3f * edust), new Vector3(1.2f + 4.2f * edust, 1.4f - 0.6f * edust, 1.2f + 4.2f * edust),
                  Quaternion.identity, t >= PaeteEruptAt ? (1f - edust) * leave : 0f);
            for (int i = 0; i < _eruptChunks.Count; i++)
                PlaceChunk(_eruptChunks[i], PaeteLanding, EruptChunkRows[i], t - PaeteEruptAt, 1.3f, leave);
            // Leaves blown off the crown as it tops out, drifting down.
            for (int i = 0; i < _crownLeaves.Count; i++)
            {
                var row = CrownLeafRows[i];
                float s = t - 3.56f;
                if (s < 0f || s > 1.6f) { Place(_crownLeaves[i], Vector3.zero, Vector3.one * .001f, Quaternion.identity, 0f); continue; }
                float a = (row.x + PaeteTreeYaw) * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                // Out fast, slowed by the air, falling at a leaf's pace, swinging as it falls.
                float outDist = row.y * (1f - Mathf.Exp(-2.2f * s)) / 2.2f;
                float y = 8.0f + row.z * s - 1.6f * s * s;
                var at = PaeteLanding + dir * (1.2f + outDist) + Vector3.up * y + new Vector3(dir.z, 0f, -dir.x) * 0.3f * Mathf.Sin(s * 5f + i);
                Place(_crownLeaves[i], at, Vector3.one * row.w, Quaternion.Euler(40f * Mathf.Sin(s * 6f + i), row.x + s * 200f, 30f * Mathf.Sin(s * 4f + i * 2f)),
                      (1f - Mathf.Clamp01((s - 1.2f) / 0.4f)) * leave);
            }

            // ---------------------------------------------------------------- THE GUARDIAN.
            if (_paeteTree != null) _paeteTree.Pose(t - PaeteEruptAt, _paeteTree.transform.position);
        }

        /// <summary>His eyes: dark until her light goes into him (0.95), then lit to the end, flaring on each blow.</summary>
        private void SamplePaeteEyes(float t, float leave)
        {
            float lit = Ease(.95f, .99f, t);
            float pulse = 0.45f * Mathf.Clamp01(1f - Mathf.Abs(t - .975f) / .08f)
                        + 0.25f * Decay(t - 1.5f, 0.25f) + 0.40f * Decay(t - 2.62f, 0.35f) + 0.30f * Decay(t - PaeteEruptAt, 0.4f);
            float flicker = 1f + 0.05f * Mathf.Sin(t * 23f) + 0.03f * Mathf.Sin(t * 37f + 1f);
            float streak = Mathf.Clamp01(1f - Mathf.Abs(t - .985f) / .18f) + 0.6f * Mathf.Clamp01(1f - Mathf.Abs(t - 2.66f) / .16f);
            for (int e = 0; e < 2; e++)
            {
                Vector3 world = default, outward = Vector3.forward;
                bool found = _paeteEyes != null && _paeteEyes.TryEye(e, out world, out outward);
                var at = found ? _root.transform.InverseTransformPoint(world) : HeadPoint + new Vector3(e == 0 ? -.08f : .08f, -.3f, .2f);
                // `PlaceGlow` takes the facing in the scene's own space.
                var facing = found ? _root.transform.InverseTransformDirection(outward) : Vector3.forward;
                float on = lit * leave * (found ? 1f : 0f);
                PlaceGlow(_paeteEyeCore[e], at, new Vector3(0.13f, 0.055f, 1f) * (1f + 0.6f * pulse) * flicker, Quaternion.identity, on * (1.1f + pulse), facing);
                PlaceGlow(_paeteEyeHalo[e], at, Vector3.one * 0.28f * (1f + 0.5f * pulse), Quaternion.identity, on * (0.35f + 0.4f * pulse), facing);
                PlaceGlow(_paeteEyeStreak[e], at, new Vector3(0.95f, 0.035f, 1f) * (0.6f + 0.4f * streak), Quaternion.identity,
                          _reducedEffects ? 0f : on * 0.9f * streak, facing);
            }
        }

        /// <summary>A chunk thrown from <paramref name="from"/>: out and up by its row, falling, bouncing once, shrinking away.</summary>
        private void PlaceChunk(int piece, Vector3 from, Vector4 row, float s, float life, float leave)
        {
            if (s < 0f || s > life) { Place(piece, Vector3.zero, Vector3.one * .001f, Quaternion.identity, 0f); return; }
            float a = row.x * Mathf.Deg2Rad;
            var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            float y = row.z * s - 7f * s * s;
            float outDist = row.y * s;
            if (y < 0f)
            {
                // The bounce: where it lands, it hops once at a third of its speed.
                float land = row.z / 7f;
                float after = s - land;
                y = Mathf.Max(0f, row.z * 0.3f * after - 7f * after * after);
                outDist = row.y * land + row.y * 0.4f * after;
            }
            var at = from + dir * (0.15f + outDist) + Vector3.up * (0.02f + y);
            float shrink = 1f - Mathf.Clamp01((s - life * 0.7f) / (life * 0.3f));
            Place(piece, at, Vector3.one * row.w * shrink, Quaternion.Euler(s * 520f + row.x, s * 300f, row.x), leave);
        }

        /// <summary>One root round his palm: breaks out of the court, arches, dives back in; draws back into the court as he rises.</summary>
        private void PosePaeteRoot(int i, float t, Vector3 ground)
        {
            var row = RootRows[i];
            var mesh = _paeteRoots[i].sharedMesh;
            float grow = Ease(row.burst, row.burst + 0.28f, t);
            float back = Ease(2.30f + 0.02f * i, 2.62f + 0.02f * i, t);
            float shown = grow * (1f - back);
            if (shown <= 0.01f) { mesh.Clear(); return; }
            float a = row.compass * Mathf.Deg2Rad;
            var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            var side = new Vector3(dir.z, 0f, -dir.x);
            _rootLine.Clear(); _rootRadii.Clear();
            const int Samples = 14;
            int count = Mathf.Max(2, Mathf.RoundToInt(Samples * shown));
            for (int k = 0; k <= count; k++)
            {
                float u = k / (float)Samples;
                // Up out of the court, over the arch, and down into it again past the reach (u 1 is 8 cm under).
                float along = row.from + row.reach * u;
                float up = row.arch * Mathf.Sin(u * Mathf.PI) - 0.08f * u - 0.06f * (1f - u) * (1f - u);
                float writhe = 0.05f * Mathf.Sin(u * 7f + t * 6f + i) * Mathf.Sin(u * Mathf.PI);
                _rootLine.Add(ground + dir * along + side * writhe + Vector3.up * up);
                _rootRadii.Add(Mathf.Lerp(row.girth, row.girth * 0.3f, u));
            }
            PaeteInk.Tube(mesh, _rootLine, _rootRadii, 5);
        }

        /// <summary>One vein: the crack and its light, running from his palm to the landing, its head blazing.</summary>
        private void PosePaeteVein(int v, float t, Vector3 ground, float leave)
        {
            var row = VeinRows[v];
            float start = 1.55f + row.delay;
            float reach = Ease(start, 2.55f, t);
            float spent = Ease(3.1f, 3.6f, t);
            var from = new Vector3(ground.x, _paeteCourt, ground.z);
            var to = PaeteLanding + Vector3.up * _paeteCourt;
            var dir = (to - from); dir.y = 0f;
            var side = dir.sqrMagnitude > 1e-4f ? Vector3.Cross(Vector3.up, dir.normalized) : Vector3.right;
            _veinLine.Clear();
            const int Samples = 40;
            int shown = Mathf.RoundToInt(Samples * reach);
            for (int k = 0; k <= shown; k++)
            {
                float u = k / (float)Samples;
                float wander = row.bow * Mathf.Sin(u * Mathf.PI) + row.wave * Mathf.Sin(u * 9f + row.phase) * Mathf.Sin(u * Mathf.PI);
                _veinLine.Add(Vector3.Lerp(from, to, u) + side * wander);
            }
            float glow = (1f - spent) * leave;
            if (_veinLine.Count < 2 || spent >= 1f)
            {
                _veinCrackMesh[v].Clear(); _veinGlowMesh[v].Clear();
                Tint(_veinCrack[v], 0f); Tint(_veinGlow[v], 0f);
            }
            else
            {
                FlatBand(_veinCrackMesh[v], _veinLine, row.width * 0.55f, _paeteCourt + 0.045f);
                FlatBand(_veinGlowMesh[v], _veinLine, row.width * 1.4f, _paeteCourt + 0.058f);
                Place(_veinCrack[v], Vector3.zero, Vector3.one, Quaternion.identity, leave);
                PlaceGlow(_veinGlow[v], Vector3.zero, Vector3.one, Quaternion.identity, 0.85f * glow * (0.85f + 0.15f * Mathf.Sin(t * 20f + v)));
            }
            // The head: blazing while it races, sinking into the pool when it arrives.
            var head = _veinLine.Count > 0 ? _veinLine[_veinLine.Count - 1] : from;
            float racing = t >= start ? 1f - Ease(2.55f, 2.75f, t) : 0f;
            PlaceGlow(_veinHead[v], head + Vector3.up * 0.12f, Vector3.one * (0.55f + 0.1f * Mathf.Sin(t * 31f + v)), Quaternion.identity, 1.3f * racing * leave);
            // Pulses running along behind the head toward the landing, three per vein, until the tree takes them.
            for (int k = 0; k < 3; k++)
            {
                float phase = Mathf.Repeat(t * 1.6f + k / 3f + v * 0.21f, 1f);
                int index = Mathf.Clamp(Mathf.RoundToInt(phase * (_veinLine.Count - 1)), 0, Mathf.Max(0, _veinLine.Count - 1));
                bool on = _veinLine.Count > 3 && t > start + 0.25f;
                PlaceGlow(_veinPulse[v * 3 + k], on ? _veinLine[index] + Vector3.up * 0.06f : from, Vector3.one * 0.32f, Quaternion.identity,
                          on ? 0.9f * glow * Mathf.Sin(phase * Mathf.PI) : 0f);
            }
            // Soil kicked up as the head passes: two clods per vein on a short hop, cycling.
            for (int k = 0; k < 2; k++)
            {
                float cycle = 0.22f;
                float s = Mathf.Repeat(t - start - k * cycle * 0.5f, cycle);
                bool kicking = t > start && t < 2.55f;
                var at = head + side * (k == 0 ? 0.12f : -0.1f) + Vector3.up * (0.03f + 2.4f * s - 11f * s * s);
                Place(_veinKick[v * 2 + k], at, Vector3.one * 0.06f, Quaternion.Euler(s * 900f, s * 500f, 0f), kicking ? leave : 0f);
            }
        }

        /// <summary>A flat band laid on the court along <paramref name="line"/>, `width` across, at height `y`; uv v runs across it.</summary>
        private static void FlatBand(Mesh mesh, List<Vector3> line, float width, float y)
        {
            int n = line.Count;
            var vertices = new Vector3[n * 2]; var uv = new Vector2[n * 2]; var triangles = new int[(n - 1) * 6];
            for (int i = 0; i < n; i++)
            {
                var along = line[Mathf.Min(n - 1, i + 1)] - line[Mathf.Max(0, i - 1)]; along.y = 0f;
                var across = along.sqrMagnitude > 1e-6f ? Vector3.Cross(Vector3.up, along.normalized) : Vector3.right;
                float taper = Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(i / 4f)) * Mathf.Lerp(1f, 0.6f, Mathf.Clamp01((i - (n - 4)) / 4f));
                var p = new Vector3(line[i].x, y, line[i].z);
                vertices[i * 2] = p + across * width * 0.5f * taper; vertices[i * 2 + 1] = p - across * width * 0.5f * taper;
                float u = i / (float)Mathf.Max(1, n - 1);
                uv[i * 2] = new Vector2(u, 0f); uv[i * 2 + 1] = new Vector2(u, 1f);
                if (i == n - 1) continue;
                int b = i * 6, a = i * 2;
                triangles[b] = a; triangles[b + 1] = a + 2; triangles[b + 2] = a + 1;
                triangles[b + 3] = a + 1; triangles[b + 4] = a + 2; triangles[b + 5] = a + 3;
            }
            mesh.Clear();
            mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
        }

        /// <summary>
        /// ⚠️ HIS EYES, FOUND ON HIS OWN FACE (owner: *"make his eyes glow or smth"*; his face is fixed: no mask, no
        /// brows, no mouth, every part typed by hand, so the glow is ADDED in front of the eyes, never painted on).
        /// The eye light is palette slot 10 (`tools/build_paete_voxel.py` `EYE`), so the vertices of that cell bound
        /// to the head are his two eyes: split into two by the widest pair, each gives a centre and an outward normal
        /// in the head bone's space, and the glow is placed there every frame, wherever the pose has his head.
        /// </summary>
        private sealed class PaeteEyes
        {
            private Transform _bone;
            private readonly Vector3[] _centre = new Vector3[2], _normal = new Vector3[2];

            public static PaeteEyes Find(Renderer[] renderers, Transform head)
            {
                if (renderers == null || head == null) return null;
                foreach (var r in renderers)
                {
                    if (!(r is SkinnedMeshRenderer skin) || skin.sharedMesh == null) continue;
                    int bone = System.Array.IndexOf(skin.bones, head);
                    if (bone < 0) continue;
                    var mesh = skin.sharedMesh;
                    var weights = mesh.boneWeights; var vertices = mesh.vertices; var normals = mesh.normals; var uv = mesh.uv; var binds = mesh.bindposes;
                    if (weights.Length != vertices.Length || uv.Length != vertices.Length || bone >= binds.Length) continue;
                    var points = new List<Vector3>(); var faces = new List<Vector3>();
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        var w = weights[i];
                        float weight = (w.boneIndex0 == bone ? w.weight0 : 0f) + (w.boneIndex1 == bone ? w.weight1 : 0f)
                                     + (w.boneIndex2 == bone ? w.weight2 : 0f) + (w.boneIndex3 == bone ? w.weight3 : 0f);
                        if (weight < 0.5f) continue;
                        // Slot 10 by the toon shader's rule: columns 4 and 5, Unity rows 0 to 3.
                        int row = Mathf.FloorToInt(uv[i].y * 16f), column = Mathf.FloorToInt(uv[i].x * 16f);
                        if (row < 0 || row > 3 || column / 2 != 2) continue;
                        points.Add(binds[bone].MultiplyPoint3x4(vertices[i]));
                        faces.Add(normals.Length == vertices.Length ? binds[bone].MultiplyVector(normals[i]) : Vector3.zero);
                    }
                    if (points.Count < 4) continue;
                    var mid = Vector3.zero; foreach (var p in points) mid += p; mid /= points.Count;
                    var far = points[0]; foreach (var p in points) if ((p - mid).sqrMagnitude > (far - mid).sqrMagnitude) far = p;
                    var other = far; foreach (var p in points) if ((p - far).sqrMagnitude > (other - far).sqrMagnitude) other = p;
                    var axis = (other - far).normalized;
                    var eyes = new PaeteEyes { _bone = head };
                    int[] count = new int[2];
                    for (int i = 0; i < points.Count; i++)
                    {
                        int e = Vector3.Dot(points[i] - mid, axis) < 0f ? 0 : 1;
                        eyes._centre[e] += points[i]; eyes._normal[e] += faces[i]; count[e]++;
                    }
                    if (count[0] == 0 || count[1] == 0) continue;
                    for (int e = 0; e < 2; e++) { eyes._centre[e] /= count[e]; eyes._normal[e] = eyes._normal[e].normalized; }
                    return eyes;
                }
                return null;
            }

            /// <summary>Eye <paramref name="index"/>'s world centre, a little proud of the face, and its outward direction.</summary>
            public bool TryEye(int index, out Vector3 world, out Vector3 outward)
            {
                world = default; outward = Vector3.forward;
                if (_bone == null) return false;
                outward = _bone.TransformDirection(_normal[index]);
                if (outward.sqrMagnitude < 1e-6f) outward = _bone.forward;
                outward.Normalize();
                world = _bone.TransformPoint(_centre[index]) + outward * 0.03f;
                return true;
            }
        }
    }
}
