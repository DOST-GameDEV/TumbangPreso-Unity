using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // PAETE, MAKILING'S EMBRACE. ⚠️⚠️ v5 (2026-09-26 night), CALLED FROM THE GROUND. The owner on the v4 film: *"i also dont
        // like that paete just throws seeds in his ult"*, *"REDIRECT IT I WANNT IT TO LOOK LIKE HE GOES TO THE GHHROUND AND HIS ROOTS
        // CONNECT TO IT AND HE IS CHANNELLING HIS POWER AND HE GLOWS AND SHIT AND THEN HIS ROOTS TRAVEL TO THE GROUND AND THEN THE tree
        // slowly show up"*, and *"dont go past 5 seconds for cutscene and u can make some parts of it faster"*. Before that, of her:
        // *"i dont mind if u show hher briefly full form and she vanishes back"*, *"when maria makiling starts coming into the pic flowers
        // start sprouting and lushh greenery"*, *"and they disappear slowly as she disappears"*; of the tree: *"i also dotn want the tree to
        // jsut spawn in or teleport in"*. The design and the reasons for every length are `docs/reports/paete-kit-2026-09-25/direction.md`
        // section 5.14; the body and the three shots are `tools/author_ultimate_intros.py` `paete`.
        //
        //   CALL   0 to 1.1   her meadow grows out from her (0.1 on); she rises (0.03 to 0.45) and FORMS in her own colours (0.30 to 0.50)
        //                     holding the light over him; it falls into his hand (0.58 to 0.70); HIS EYES IGNITE (0.78); she turns back
        //                     to spirit as it passes into him (0.82 to 1.08).
        //   ROOT   1.1 to 2.3 he drops and plants both palms (1.22, the slam; the makahiya fold shut); his roots pour out of his forearms
        //                     and knee into the court (1.25 to 1.6); he CHANNELS (1.6 to 2.3): the light sweeps down him into the ground and
        //                     pulses three times, quickening (1.72, 1.96, 2.14); she comes close behind him, her hands over his shoulders.
        //   RISE   2.3 to 5.0 his roots race to the spot under the court (2.3 to 2.75); the court bulges (2.75); the tree CRAWLS out: claws,
        //                     three heaves (3.15, 3.55, 3.95), the crown (4.1 to 4.45), its eyes (4.5), the last beat. She lets go and the
        //                     mist takes her (3.6 to 4.4) as her meadow wilts from its edge in (3.6 to 4.5). He stays down, joined to the
        //                     ground, and lifts his head to it; play picks up from exactly that pose.
        //
        // ⚠️ Every piece is posed from the scene clock `t`: during the shared phase the world is paused (`Time.timeScale` 0), so nothing
        // may run on `Update`. The tree is the live `PaeteSentryBody` in `Staged` mode, so what crawls out here is what crawls out in play.
        // ⚠️ CUT IN v5, AND WHY: v4's roots bursting UP round his palm (the roots are his now and go DOWN into the court), the three lit
        // veins with a blazing head (a lit head racing along the ground is a seed rolling), the light pooling at the landing (the court
        // bulges and leaks light instead), her stream of light over him (she stays with him; the roots carry the power).
        // =========================================================================================

        // Where things stand, in the caster's space (+z ahead of him, +x to his right).
        private static readonly Vector3 PaeteLanding = new Vector3(0f, 0f, 5.5f);
        private static readonly Vector3 MakilingStand = new Vector3(1.0f, 0f, -1.2f);
        // ⚠️ FOR THE ROOT SHOT SHE COMES IN CLOSE, DIRECTLY BEHIND HIM (the handoff's note on v4: she took up the left of the overhead
        // frame). The first placement, behind his right shoulder, left only a sliver of her hem in the corner of the shot
        // (`PaeteSpiritReviewProbe` v4), which reads as a mistake. Directly behind him and bent over him, her hands come down over his
        // shoulders and her face looks down on him from the top of the frame: the spirit over the guardian, centred, not beside him.
        private static readonly Vector3 MakilingClose = new Vector3(0.05f, 0f, -1.15f);
        private const float MakilingYaw = -14f, MakilingScale = 1.1f;
        // The guardian faces out toward the rise shot's camera, a little more than a quarter turn from him, so its hollows are seen lighting.
        private const float PaeteTreeYaw = 105f;
        // The beats (direction.md 5.14).
        private const float PaeteSlamAt = 1.22f, PaeteSendAt = 2.3f, PaeteArriveAt = 2.75f;
        private static readonly float[] PaetePulses = { 1.72f, 1.96f, 2.14f };
        private static readonly float[] PaeteHauls = { 3.15f, 3.55f, 3.95f };

        // The light's colours: his lime (the eye light, `D8FF6A`), a hotter near-white core, her jade.
        private static readonly Color PaeteLight = new Color(0.85f, 1.0f, 0.42f, 1f);
        private static readonly Color PaeteHot = new Color(1.0f, 1.0f, 0.78f, 1f);
        private static readonly Color MakilingJade = new Color(0.62f, 1.0f, 0.70f, 1f);

        private MakilingSpirit _makilingSpirit;
        private PaeteMeadow _paeteMeadow;
        private PaeteGroundRoots _paeteRoots;
        private PaeteChannelGlow _paeteGlow;
        private PaeteRootRidge _paeteRidge;
        private PaeteSentryBody _paeteTree;
        private readonly List<int> _mist = new List<int>(8);
        private readonly List<int> _fireflies = new List<int>(8);
        private readonly List<int> _motes = new List<int>(10);
        private int _giftLight, _giftHalo, _palmGlow, _poolGlow, _poolCrack;
        private readonly List<int> _giftTrail = new List<int>(5);
        private readonly int[] _paeteEyeCore = new int[2], _paeteEyeHalo = new int[2], _paeteEyeStreak = new int[2];
        private int _slamCrack, _slamHeave, _slamDust;
        private readonly List<int> _haulDust = new List<int>(3);
        private readonly List<int> _slamChunks = new List<int>(9);
        private readonly List<int> _eruptChunks = new List<int>(12);
        private readonly List<int> _crownLeaves = new List<int>(14);
        private PaeteEyeLight _paeteEyes;
        // Where his palms and knee meet the court, read off the pose while he is down and held after.
        private Vector3 _paeteLeftPalm = new Vector3(-0.40f, 0.02f, 0.98f), _paeteRightPalm = new Vector3(0.36f, 0.02f, 1.0f);
        private static readonly Vector3 PaeteKnee = new Vector3(0.25f, 0f, -0.38f);
        // ⚠️ THE COURT'S VISIBLE TOP ABOVE THE SCENE ROOT (film r12: flat pieces laid 1 to 2 cm over the root were all under the plaza's
        // court surface and never drew). Measured once with the same probe the live pieces use (`Slipper.GroundY`).
        private float _paeteCourt;
        private readonly List<float> _paetePulseList = new List<float>(PaetePulses);

        // ------------------------------------------------------------------ typed tables

        // Her mist: soft pools on the court round her hem (flat) and puffs about it (facing the lens). Each: angle round her (degrees),
        // distance out, height, size.
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
        // The motes that rise off HIM while he channels: across his shoulders and back (x, z in his space), where they start (height),
        // how fast they rise, size. Each typed; they are the light leaving him upward as it pours down into the ground.
        private static readonly Vector4[] MoteRows =
        {
            new Vector4(-0.42f, 0.10f, 0.9f, 0.55f), new Vector4(0.36f, -0.05f, 1.1f, 0.48f), new Vector4(-0.12f, -0.30f, 1.3f, 0.62f),
            new Vector4(0.18f, 0.28f, 0.8f, 0.40f), new Vector4(-0.30f, -0.18f, 1.2f, 0.58f), new Vector4(0.44f, 0.16f, 1.0f, 0.52f),
            new Vector4(0.02f, 0.40f, 0.7f, 0.46f), new Vector4(-0.52f, -0.02f, 1.4f, 0.50f), new Vector4(0.26f, -0.36f, 1.2f, 0.44f),
        };
        private static readonly float[] MoteSize = { .07f, .06f, .08f, .05f, .07f, .06f, .05f, .08f, .06f };

        // Chunks of court thrown up by the slam: (direction round his hands, out speed, up speed, size).
        private static readonly Vector4[] SlamChunkRows =
        {
            new Vector4(12f, 1.4f, 2.6f, 0.10f), new Vector4(58f, 1.9f, 2.1f, 0.07f), new Vector4(104f, 1.2f, 3.0f, 0.09f),
            new Vector4(150f, 1.7f, 2.4f, 0.06f), new Vector4(196f, 1.5f, 2.8f, 0.08f), new Vector4(244f, 2.0f, 1.9f, 0.07f),
            new Vector4(290f, 1.3f, 3.2f, 0.11f), new Vector4(330f, 1.8f, 2.2f, 0.06f), new Vector4(80f, 0.9f, 3.6f, 0.05f),
        };
        // And by the tree's hauls, bigger and further: a third of them on each haul, in its own order.
        private static readonly Vector4[] EruptChunkRows =
        {
            new Vector4(8f, 3.2f, 5.4f, 0.22f), new Vector4(40f, 4.1f, 4.2f, 0.16f), new Vector4(75f, 2.6f, 6.0f, 0.20f),
            new Vector4(110f, 3.7f, 4.8f, 0.14f), new Vector4(142f, 2.9f, 5.6f, 0.24f), new Vector4(178f, 4.4f, 3.8f, 0.15f),
            new Vector4(212f, 3.1f, 5.1f, 0.19f), new Vector4(246f, 3.9f, 4.5f, 0.13f), new Vector4(279f, 2.7f, 6.2f, 0.21f),
            new Vector4(305f, 4.2f, 4.0f, 0.17f), new Vector4(333f, 3.4f, 5.0f, 0.15f), new Vector4(355f, 2.4f, 6.6f, 0.12f),
        };
        // Leaves blown off the crown as it tops out: direction, out speed, up speed, size.
        private static readonly Vector4[] CrownLeafRows =
        {
            new Vector4(0f, 2.2f, 2.4f, 0.26f), new Vector4(26f, 3.0f, 1.6f, 0.22f), new Vector4(52f, 2.5f, 2.9f, 0.28f),
            new Vector4(79f, 3.4f, 1.2f, 0.20f), new Vector4(104f, 2.0f, 3.2f, 0.24f), new Vector4(131f, 2.8f, 2.0f, 0.27f),
            new Vector4(157f, 3.6f, 1.4f, 0.21f), new Vector4(183f, 2.3f, 2.6f, 0.25f), new Vector4(210f, 3.1f, 1.8f, 0.23f),
            new Vector4(236f, 2.6f, 3.0f, 0.26f), new Vector4(262f, 3.3f, 1.5f, 0.20f), new Vector4(289f, 2.1f, 2.7f, 0.28f),
            new Vector4(314f, 2.9f, 2.2f, 0.22f), new Vector4(340f, 3.5f, 1.3f, 0.24f),
        };

        private Vector3 PaeteHandsMid => (_paeteLeftPalm + _paeteRightPalm) * 0.5f;

        private void BuildPaete()
        {
            _paeteCourt = Mathf.Clamp(Slipper.GroundY(_root.transform.position + Vector3.up * .3f) - _root.transform.position.y, -.2f, .3f);
            _paeteLeftPalm.y = _paeteRightPalm.y = _paeteCourt + .03f;
            // HER MEADOW first, so she rises out of it (direction.md 5.13).
            _paeteMeadow = new PaeteMeadow(_root.transform, MakilingStand, PaeteHandsMid, _paeteCourt);
            // MARIANG MAKILING, behind his RIGHT shoulder, turned a little toward him.
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
            _paeteEyes = PaeteEyeLight.Find(_bodyRenderers, _head);
            for (int e = 0; e < 2; e++)
            {
                _paeteEyeCore[e] = AddGlow("PaeteEyeCore" + e, new Color(0.80f, 1.0f, 0.55f, 1f), falloff: 2.0f, core: 1.4f, lift: .10f, facing: true);
                _paeteEyeHalo[e] = AddGlow("PaeteEyeHalo" + e, PaeteLight, falloff: 1.5f, core: 0f, lift: .10f, facing: true);
                _paeteEyeStreak[e] = AddGlow("PaeteEyeStreak" + e, PaeteLight, falloff: 1.3f, core: 0.5f, lift: .12f, facing: true);
            }
            // HE GLOWS: a second drawing of his own body in `SpiritVeins.shader` (his vines, his moss, his edges), on this stage.
            var skins = new List<Renderer>();
            foreach (var r in _bodyRenderers) if (r is SkinnedMeshRenderer) skins.Add(r);
            _paeteGlow = PaeteChannelGlow.Attach(skins, _root.transform, ownPalette: true);
            for (int i = 0; i < MoteRows.Length; i++)
                _motes.Add(AddGlow("PaeteMote" + i, PaeteLight, falloff: 2.3f, core: 0.9f));

            // THE SLAM: the court cracking under his palms, its slabs heaving, a ring of dust and thrown chunks.
            var courtDark = new Color(0.20f, 0.14f, 0.09f, 1f);
            _slamCrack = AddSolid("PaeteSlamCrack", VfxShapes.Fracture(6, 3, 0.05f, 91), courtDark);
            _slamHeave = AddSolid("PaeteSlamHeave", VfxShapes.Upheaval(8, 0.08f, 0.30f, 17, 0.05f), GrowthVfx.Seed);
            _slamDust = Add("PaeteSlamDust", VfxShapes.Collar(18, 0.30f, 0.80f, 0.12f, 0f, 5), new Color(0.88f, 0.82f, 0.70f, 0.55f), 0.1f, plain: true);
            for (int i = 0; i < SlamChunkRows.Length; i++)
                _slamChunks.Add(AddSolid("PaeteSlamChunk" + i, VfxShapes.Prism(5, 0.6f, 0.7f, 0.15f, 20f * i, 30 + i), i % 3 == 0 ? courtDark : GrowthVfx.Seed));

            // HIS ROOTS INTO THE COURT (from his hands and his knee; his own bark).
            _paeteRoots = new PaeteGroundRoots(_root.transform, PaeteProp.Palette);

            // HIS ROOTS RACING TO THE SPOT, staged: posed from this clock only.
            _paeteRidge = PaeteRootRidge.Race(_root.transform, new Vector3(PaeteHandsMid.x, _paeteCourt, PaeteHandsMid.z + 0.15f),
                                              PaeteLanding + Vector3.up * _paeteCourt, PaeteArriveAt - PaeteSendAt, staged: true, court: _paeteCourt);

            // THE LANDING: light leaking up through the court as it bulges, and the court cracking round it; the dust of each haul.
            _poolGlow = AddGlow("PaetePool", PaeteLight, billboard: false, falloff: 1.4f, core: 0.9f);
            _poolCrack = AddSolid("PaetePoolCrack", VfxShapes.Fracture(8, 3, 0.07f, 57), courtDark);
            for (int i = 0; i < PaeteHauls.Length; i++)
                _haulDust.Add(Add("PaeteHaulDust" + i, VfxShapes.Collar(22, 0.55f, 0.78f, 0.16f, 0f, 11 + i), new Color(0.86f, 0.80f, 0.68f, 0.5f), 0.1f, plain: true));
            for (int i = 0; i < EruptChunkRows.Length; i++)
                _eruptChunks.Add(AddSolid("PaeteEruptChunk" + i, VfxShapes.Prism(5, 0.6f, 0.7f, 0.2f, 15f * i, 50 + i), i % 3 == 1 ? courtDark : GrowthVfx.Seed));
            for (int i = 0; i < CrownLeafRows.Length; i++)
                _crownLeaves.Add(AddSolid("PaeteCrownLeaf" + i, PaeteInk.Leaf(1f, 0.55f, 0.08f), i % 2 == 0 ? PaeteSentryBody.Leaf : PaeteSentryBody.Palette[3]));

            // THE GUARDIAN: the live tree itself, staged (no world effects), facing out toward the rise camera. Its own clock: it crawls
            // out from the roots' arrival, its eyes last (`PaeteSentryBody.WakeAt`, 1.75 s later: 4.5 here).
            _paeteTree = PaeteSentryBody.Build(_root.transform);
            _paeteTree.transform.localPosition = PaeteLanding + Vector3.up * _paeteCourt;
            _paeteTree.Staged = true;
            _paeteTree.SetFacing(_root.transform.TransformDirection(Quaternion.Euler(0f, PaeteTreeYaw, 0f) * Vector3.forward));
        }

        /// <summary>The camera's shake for Paete's blows: the slam, each pulse, the send, the court bulging, each haul (harder each time), the top-out.</summary>
        private Vector3 PaeteShake(float t)
        {
            float amp = 0.07f * Decay(t - PaeteSlamAt, 0.35f) + 0.03f * Decay(t - PaeteSendAt - 0.02f, 0.25f) + 0.05f * Decay(t - PaeteArriveAt, 0.4f);
            foreach (float p in PaetePulses) amp += 0.018f * Decay(t - p, 0.18f);
            for (int k = 0; k < PaeteHauls.Length; k++) amp += (0.07f + 0.025f * k) * Decay(t - PaeteHauls[k], 0.5f);
            amp += 0.05f * Decay(t - (PaeteArriveAt + PaeteSentryBody.Heaves[PaeteSentryBody.Heaves.Length - 1].y), 0.3f);
            return amp * new Vector3(Mathf.Sin(t * 53f), 0.8f * Mathf.Sin(t * 67f + 1f), 0.5f * Mathf.Sin(t * 41f + 2f));
        }

        private static float Decay(float since, float length) => since < 0f || since > length ? 0f : 1f - since / length;

        private void SamplePaete(float t)
        {
            float leave = 1 - Ease(Seconds - .45f, Seconds - .05f, t);

            // ---------------------------------------------------------------- MAKILING, watching over him.
            float rise = Ease(.03f, .45f, t);
            // For the ROOT shot she comes in close behind him and bends over him; for the RISE she stands back to watch the tree.
            float close = Ease(1.0f, 1.3f, t) * (1f - Ease(2.3f, 2.7f, t));
            float watchTree = Ease(2.7f, 3.2f, t);
            var look = new MakilingSpirit.Look
            {
                Presence = rise,
                Drift = (MakilingClose - MakilingStand) * close + new Vector3(0f, 0.18f, 0f) * watchTree,
                Lean = 10f + 8f * Ease(.25f, .55f, t) + 6f * close - 12f * watchTree * (1f - close),
                Bow = 20f * Ease(.25f, .55f, t) + 10f * close - 34f * watchTree,
                Turn = -12f * Ease(.25f, .55f, t) + 10f * watchTree,
                // Her arms: raised over him with the light (0.3 to 0.6), lowering after she lets it fall, then over his shoulders.
                Reach = Mathf.Lerp(Mathf.Lerp(0.5f * Ease(.30f, .52f, t), 0.30f, Ease(.72f, 1.05f, t)), 0.34f, close) * (1f - watchTree) + 0.30f * watchTree,
                Open = Mathf.Lerp(Ease(.54f, .62f, t) * (1f - 0.5f * Ease(.78f, 1.05f, t)), 0.15f, close) * (1f - watchTree) + watchTree,
                Wind = 0.3f + 0.9f * Decay(t - PaeteSlamAt, 0.6f) + 0.5f * Ease(1.6f, 2.3f, t) * (1f - Ease(2.3f, 2.7f, t)) + 0.8f * Decay(t - PaeteHauls[2], 0.9f),
                Fade = Ease(3.60f, 4.40f, t),
                Light = Ease(.12f, .35f, t) * (1f - Ease(.56f, .60f, t)),
                // HER FULL FORM, BRIEFLY: she forms while she holds the light over him and turns back to spirit as it passes into him.
                Form = Ease(.30f, .50f, t),
                Unform = Ease(.82f, 1.08f, t),
            };
            _makilingSpirit?.Pose(t, look);
            var her = MakilingStand + look.Drift;
            float herHere = rise * (1f - look.Fade) * leave;
            // Her mist: pools on the court at her hem and puffs round it, turning slowly.
            for (int i = 0; i < _mist.Count; i++)
            {
                var row = MistRows[i];
                float a = (row.x + t * (i % 2 == 0 ? 9f : -7f)) * Mathf.Deg2Rad;
                var at = her + new Vector3(Mathf.Sin(a) * row.y, row.z + _paeteCourt, Mathf.Cos(a) * row.y);
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

            // ---------------------------------------------------------------- HER MEADOW: up with her, down with her.
            _paeteMeadow?.Pose(t, PaeteSlamAt, PaeteHauls, 3.6f, 4.5f, PaeteLanding);

            // ---------------------------------------------------------------- THE LIGHT, from her hands into his.
            var hands = _makilingSpirit != null ? _root.transform.InverseTransformPoint(_makilingSpirit.HandsWorld) : her + new Vector3(0f, 2.2f, 0.4f);
            var palm = FreePalm + new Vector3(0f, .07f, .04f);
            float fall = Ease(.58f, .70f, t);
            Vector3 lightAt = t < .58f ? hands : Vector3.Lerp(hands, palm, fall) + Vector3.up * 0.35f * Mathf.Sin(fall * Mathf.PI);
            // It glows in her hands until it falls, sits in his palm, FLARES as it goes into him (0.78), then is his.
            float held = t < .58f ? Ease(.12f, .35f, t) : 1f;
            float into = Ease(.78f, .88f, t);
            float flare = 1f + 0.9f * Mathf.Clamp01(1f - Mathf.Abs(t - .79f) / .05f);
            bool lightOn = t > .58f && t < .88f;
            PlaceGlow(_giftLight, lightAt, Vector3.one * 0.28f * flare * (1f - 0.7f * into), Quaternion.identity, lightOn ? held * (1f - into) : 0f);
            PlaceGlow(_giftHalo, lightAt, Vector3.one * 0.95f * flare * (1f - 0.5f * into), Quaternion.identity, lightOn ? 0.45f * (1f - into) : 0f);
            for (int i = 0; i < _giftTrail.Count; i++)
            {
                float back = Ease(.58f, .70f, t - 0.02f * (i + 1));
                var p = Vector3.Lerp(hands, palm, back) + Vector3.up * 0.35f * Mathf.Sin(back * Mathf.PI);
                PlaceGlow(_giftTrail[i], p, Vector3.one * (0.16f - 0.022f * i), Quaternion.identity, t > .58f && t < .74f ? 0.7f - 0.12f * i : 0f);
            }
            // The light that stays in his palm after it has gone into him, and flares as his palms hit the court.
            float palmOn = Ease(.70f, .78f, t) * (1f - 0.55f * Ease(.78f, .92f, t)) * (1f - Ease(PaeteSlamAt + .02f, PaeteSlamAt + .12f, t));
            PlaceGlow(_palmGlow, palm, Vector3.one * (0.34f + 0.25f * Decay(t - PaeteSlamAt, 0.12f)), Quaternion.identity,
                      palmOn * (1f + 1.5f * Decay(t - PaeteSlamAt, 0.12f)));

            // ---------------------------------------------------------------- HIS EYES ignite and stay lit.
            SamplePaeteEyes(t, leave);

            // ---------------------------------------------------------------- THE SLAM, both palms, 1.22 s.
            if (t >= PaeteSlamAt && t <= PaeteSlamAt + 0.8f)
            {
                var l = FreePalm; var r = RightPalm;
                _paeteLeftPalm = new Vector3(l.x, _paeteCourt + 0.03f, l.z);
                _paeteRightPalm = new Vector3(r.x, _paeteCourt + 0.03f, r.z);
            }
            var ground = PaeteHandsMid;
            float crack = Ease(PaeteSlamAt, PaeteSlamAt + 0.08f, t);
            Place(_slamCrack, ground + Vector3.up * 0.015f, new Vector3(1.4f, 1f, 1.4f) * Mathf.Max(.001f, crack), Quaternion.Euler(0f, 30f, 0f), crack > .001f ? leave : 0f);
            float heave = GrowthVfx.Pop((t - PaeteSlamAt) / 0.10f) * (1f - Ease(2.4f, 2.8f, t));
            Place(_slamHeave, ground, new Vector3(0.75f, Mathf.Max(.001f, heave), 0.75f), Quaternion.Euler(0f, 12f, 0f), heave > .001f ? leave : 0f);
            float dust = Ease(PaeteSlamAt, PaeteSlamAt + 0.35f, t);
            Place(_slamDust, ground + Vector3.up * (0.02f + 0.15f * dust), new Vector3(0.3f + 1.8f * dust, 0.8f - 0.4f * dust, 0.3f + 1.8f * dust),
                  Quaternion.identity, t >= PaeteSlamAt ? (1f - dust) * leave : 0f);
            for (int i = 0; i < _slamChunks.Count; i++)
                PlaceChunk(_slamChunks[i], ground, SlamChunkRows[i], t - PaeteSlamAt, 0.75f, leave);

            // ---------------------------------------------------------------- HIS ROOTS INTO THE COURT, and the channel.
            float dig = Ease(PaeteSlamAt + 0.03f, 1.6f, t);
            float haulTaut = 0f;
            foreach (float h in PaeteHauls) haulTaut = Mathf.Max(haulTaut, GrowthVfx.Envelope(t, h - 0.04f, 0.08f, h + 0.3f, 0.18f));
            float channel = Ease(1.5f, 1.7f, t) * leave;
            var knee = PaeteKnee + Vector3.up * (_paeteCourt + 0.04f);
            // ⚠️ The roots stay in the court to the very end: play picks up from this pose with his roots still in (`PaeteGroundCall`).
            _paeteRoots?.Pose(t, _paeteLeftPalm, _paeteRightPalm, knee, 0f, _paeteCourt, dig, haulTaut, 0f, channel, _paetePulseList);
            SamplePaeteGlow(t, leave);

            // ---------------------------------------------------------------- HIS ROOTS TRAVEL, 2.3 to 2.75.
            if (_paeteRidge != null) _paeteRidge.Pose(t - PaeteSendAt);

            // ---------------------------------------------------------------- THE LANDING: the court bulges and leaks light, then cracks.
            float leak = Ease(PaeteArriveAt - 0.1f, PaeteArriveAt + 0.1f, t) * (1f - Ease(PaeteArriveAt + 0.5f, PaeteArriveAt + 1.0f, t));
            float beat = 1f + 0.2f * Mathf.Sin(t * 30f);
            PlaceGlow(_poolGlow, PaeteLanding + Vector3.up * (_paeteCourt + 0.07f), new Vector3(2.4f, 2.4f, 1f) * beat,
                      Quaternion.Euler(90f, 0f, 0f), leak * 0.7f * leave);
            float landCrack = Ease(PaeteArriveAt, PaeteArriveAt + 0.3f, t);
            Place(_poolCrack, PaeteLanding + Vector3.up * (_paeteCourt + 0.045f), new Vector3(2.1f, 1f, 2.1f) * Mathf.Max(.001f, landCrack), Quaternion.Euler(0f, -20f, 0f),
                  landCrack > .001f ? leave : 0f);
            // Each haul throws dust out round it and a third of the chunks.
            for (int k = 0; k < _haulDust.Count; k++)
            {
                float d = Ease(PaeteHauls[k], PaeteHauls[k] + 0.7f, t);
                Place(_haulDust[k], PaeteLanding + Vector3.up * (_paeteCourt + 0.05f + 0.3f * d), new Vector3(1.4f + (3.6f + 0.8f * k) * d, 1.2f - 0.5f * d, 1.4f + (3.6f + 0.8f * k) * d),
                      Quaternion.identity, t >= PaeteHauls[k] ? (1f - d) * 0.8f * leave : 0f);
            }
            for (int i = 0; i < _eruptChunks.Count; i++)
                PlaceChunk(_eruptChunks[i], PaeteLanding + Vector3.up * _paeteCourt, Vector4.Scale(EruptChunkRows[i], new Vector4(1f, 0.7f, 0.8f, 1f)), t - PaeteHauls[i % PaeteHauls.Length], 1.2f, leave);
            // Leaves blown off the crown as it tops out, drifting down.
            float topOut = PaeteArriveAt + PaeteSentryBody.Heaves[PaeteSentryBody.Heaves.Length - 1].y;
            for (int i = 0; i < _crownLeaves.Count; i++)
            {
                var row = CrownLeafRows[i];
                float s = t - topOut;
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

            // ---------------------------------------------------------------- THE GUARDIAN crawls out of the court.
            if (_paeteTree != null) _paeteTree.Pose(t - PaeteArriveAt, _paeteTree.transform.position);
        }

        /// <summary>
        /// HE GLOWS (direction.md 5.14, beat 3): from 1.55 s the light sweeps down him from above his head to the court (his vines, his
        /// moss, his edges); three pulses run down his arms into the ground, quickening; it holds while the roots travel and flares on
        /// each of the tree's hauls; motes rise off his shoulders while he channels.
        /// </summary>
        private void SamplePaeteGlow(float t, float leave)
        {
            float rootY = _root.transform.position.y;
            float headY = HeadPoint.y + 0.25f, courtY = _paeteCourt;
            float strength = Ease(1.5f, 1.75f, t);
            float flare = 0f;
            foreach (float h in PaeteHauls) flare = Mathf.Max(flare, GrowthVfx.Envelope(t, h, 0.05f, h + 0.35f, 0.25f));
            strength *= (0.95f + 0.45f * flare + 0.3f * Decay(t - PaeteSendAt, 0.3f)) * leave;
            float sweep = Mathf.Lerp(headY, courtY - 0.3f, Ease(1.55f, 1.95f, t));
            float pulseY = rootY - 100f, pulseStrength = 0f;
            foreach (float p in PaetePulses)
            {
                float s = (t - p) / 0.26f;
                if (s < 0f || s > 1f) continue;
                pulseY = rootY + Mathf.Lerp(headY - 0.35f, courtY + 0.05f, s);
                pulseStrength = 1.7f * Mathf.Sin(s * Mathf.PI);
            }
            {
                float s = (t - PaeteSendAt) / 0.22f;
                if (s >= 0f && s <= 1f) { pulseY = rootY + Mathf.Lerp(headY - 0.35f, courtY + 0.05f, s); pulseStrength = 2.1f * Mathf.Sin(s * Mathf.PI); }
            }
            _paeteGlow?.Set(strength, rootY + sweep, pulseY, pulseStrength);
            // The motes: the light leaving him upward off his shoulders and back while it pours down into the ground.
            float motes = Ease(1.6f, 1.8f, t) * (1f - Ease(2.6f, 3.2f, t)) * leave;
            for (int i = 0; i < _motes.Count; i++)
            {
                var row = MoteRows[i];
                float y = row.z + Mathf.Repeat(t * row.w + i * 0.37f, 1.3f);
                float fade = 1f - Mathf.Clamp01((y - row.z - 0.9f) / 0.4f);
                var at = new Vector3(row.x + 0.06f * Mathf.Sin(t * 3f + i), y + _paeteCourt, row.y + 0.06f * Mathf.Cos(t * 2.6f + i));
                PlaceGlow(_motes[i], at, Vector3.one * MoteSize[i], Quaternion.identity, 0.9f * motes * fade * (0.7f + 0.3f * Mathf.Sin(t * 9f + i)));
            }
        }

        /// <summary>His eyes: dark until her light goes into him (0.78), then lit to the end, flaring on each blow.</summary>
        private void SamplePaeteEyes(float t, float leave)
        {
            float lit = Ease(.78f, .82f, t);
            float pulse = 0.45f * Mathf.Clamp01(1f - Mathf.Abs(t - .80f) / .08f) + 0.25f * Decay(t - PaeteSlamAt, 0.25f)
                        + 0.40f * Decay(t - PaeteSendAt, 0.35f);
            foreach (float p in PaetePulses) pulse += 0.2f * Decay(t - p, 0.2f);
            foreach (float h in PaeteHauls) pulse += 0.25f * Decay(t - h, 0.4f);
            float flicker = 1f + 0.05f * Mathf.Sin(t * 23f) + 0.03f * Mathf.Sin(t * 37f + 1f);
            float streak = Mathf.Clamp01(1f - Mathf.Abs(t - .81f) / .18f) + 0.6f * Mathf.Clamp01(1f - Mathf.Abs(t - (PaeteSendAt + .04f)) / .16f);
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
    }
}
