using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // OVERCLOCK, THE BOLT WAITS FOR HIM, 6.4 s (was 2.8 s: a finger spark, a storm, a bolt on his head).
        //
        // ⚠️ RESTAGED 2026-10-09, UNPITCHED. The owner was away and asked for "something for the rest of the heroes ...
        // surprise me with something really good", so this was not chosen from options as batch 1 was. BLOCKING until he
        // says the idea stays. The first cutscene is whole in `tools/cutscene_rework/old/`.
        //
        // The ultimate makes him faster than everybody for the rest of the match, so the cutscene shows exactly that,
        // once, as a joke only he is in on:
        //   1. THE CALL (0 to 1.2): the cocky flick, the finger up, the storm comes in and answers far off (kept).
        //   2. THE STOP (1.2 to 1.94), from low, looking up past him: the real bolt comes down the sky at him, and
        //      the world STOPS with its tip an arm's length over his hand. The rain hangs. Everyone else is a statue.
        //   3. THE LOOK (1.94 to 2.7), close: he is the only thing moving. He looks at the lens.
        //   4. THE POKE (2.7 to 3.6): he reaches up and pokes the tip of the lightning with one finger. It wobbles.
        //   5. THE SHRUG (3.6 to 4.5), wide and travelling round the stopped court: hung rain, statues, the bolt held
        //      from the cloud to just over him, and him shrugging at it.
        //   6. THE LET GO (4.5 to 6.4): he sets his feet and lets the world go. The rain falls, the bolt finishes its
        //      trip into him, the ring goes out to the ultimate's own 4.5 m and whoever is inside it jumps, and he is
        //      left gold and crackling.
        //
        // ⚠️ THE STOP IS ONE CLOCK (`ZackWorld`): the rain, its splashes, the clouds, the light inside them and the
        // light behind the skyline all read it, so they stop and start together. His body, the lens, the bolt's tip and
        // everything that jumps between the bolt and him read the real one: they are his.
        //
        // ⚠️ WHO IS ZAPPED IS THE ABILITY'S RULE (`ZackHeroKit`, 4.5 m). This shows the ring at that size and jolts the
        // staged players who really stand inside it; it decides nothing.
        //
        // The free hand authors the gesture; the carrying hand keeps its actual shoe. Reduced effects keep the shapes
        // and drop the two flashes, as before.
        //
        // ⚠️ THE LOOK, 2026-10-10. The staging above stood on stock pieces: a flat zigzag for the bolt, two dark
        // cylinders and five splats for the storm, sixteen prisms for the city, forty hairlines for the rain. The owner,
        // once the staging stood: "continue polishing all the ultimates. i dont want to see anything bare in textures,
        // anything looking unpolished, no vfx etc." No time, pose or shot moved; what each beat is drawn with did:
        //   * THE BOLT is typed, knee by knee (`ZkBoltSpine`): a gold sheath, a pale core inside it, a band of light
        //     along it, forks that leave it at its knees. While the world is stopped the forks are stopped too, the tip
        //     burns, and small arcs feel their way from the tip to his head and his fist.
        //   * THE STORM is a sky that darkens upward (`ZackSkyMesh`), modelled clouds with light in their bellies, the
        //     cloud the bolt comes out of, and a city of five kinds of building in two rings
        //     (`tools/build_ultimate_props_zack.py`: round forms and a painted atlas, as the hand companions and Paete's
        //     plants are made, so every window and every cloud's underside is painted, not a flat fill).
        //   * NOTHING WITH WEIGHT MOVES IN A STRAIGHT SLIDE. The bolt most of all: `ZackTip`.
        //   * THE RAIN is drops, each with a glint, and crowns where they land; all of it hangs when the world does.
        //   * THE STOP is a ring, a second ring, a dome that passes over everybody, and a mark that closes under each
        //     player it has stopped.
        //   * THE STRIKE leaves a broken, glowing mark on the court, and HE is left with arcs jumping off him.
        // =========================================================================================
        private const float ZkSlow = 1.80f, ZkStopped = 1.94f, ZkPoke = 3.15f, ZkGo = 5.20f, ZkHit = 5.27f, ZkReach = 4.5f;
        private static readonly Vector3 ZkTop = new Vector3(-.25f, 13f, .1f), ZkHung = new Vector3(-.62f, 1.82f, .3f);
        // The far bolt that answers his finger: it comes down behind the near roofs, so the city is cut out against it.
        private static readonly Vector3 ZkAnswerTop = new Vector3(3.4f, 12.5f, -10.6f), ZkAnswerFoot = new Vector3(2.9f, 2.4f, -10.4f);
        // How wide the bolt is drawn. 3.2: at 1.1 it was a thread from the wide shot (film z1).
        private const float ZkGirth = 3.2f;

        private int _stormLow, _stormHigh, _fingerSpark, _fingerGlow, _answerBolt, _answerCore, _answerHalo, _answerFork, _answerSheet;
        private int _snapBolt, _snapCore, _snapHalo, _snapGlow, _zkTip, _zkTipSpark, _zkTick, _zkTick2, _zkDome, _zkRing, _zkRingTeeth, _zkRing2;
        private int _zkPop, _zkPopGlow, _zkColumn, _zkScorch, _zkCracks, _zkMark, _zkAura, _zkThroat;
        private readonly List<int> _zkRain = new List<int>(56), _zkRainShine = new List<int>(56), _zkJolts = new List<int>(4), _zkJoltGlows = new List<int>(4);
        private readonly List<int> _zkSplash = new List<int>(12), _zkRipple = new List<int>(12), _zkBellies = new List<int>(12), _zkSheets = new List<int>(4);
        private readonly List<int> _zkForks = new List<int>(6), _zkForkCores = new List<int>(6), _zkWire = new List<int>(2), _zkArcs = new List<int>(8);
        private readonly List<int> _zkHolds = new List<int>(4), _zkDials = new List<int>(4), _zkMotes = new List<int>(4);
        private readonly List<Transform> _zkTowers = new List<Transform>(40), _zkCloudProps = new List<Transform>(12);
        private Transform _zkCloudTop;
        private static readonly Color ZackGold = new Color(1, .82f, .2f, .95f);
        // The inside of the bolt. His own gold made pale, never white: white on this sky reads as a hole in the picture.
        private static readonly Color ZackCore = new Color(1f, .96f, .62f, 1f);
        private static readonly Color ZackLight = new Color(1f, .86f, .42f, 1f);
        // The light round the bolt, deeper than the bolt: a pale light over a pale core washed the whole thing out (film p1).
        private static readonly Color ZackDeep = new Color(1f, .66f, .16f, 1f);

        // ---------------------------------------------------------------------------------------- the typed shapes

        // The bolt, knee by knee, cloud (y 0) to tip (y 1): x and z are its sideways steps as a share of `ZkGirth`.
        // It swings wide high up, where the wide shots see it, and comes in to a short straight spear over his head.
        private static readonly Vector3[] ZkBoltSpine =
        {
            new Vector3(0f, 0f, 0f), new Vector3(.10f, .09f, .02f), new Vector3(-.06f, .17f, -.02f), new Vector3(.12f, .29f, .02f),
            new Vector3(-.03f, .38f, 0f), new Vector3(.13f, .50f, .03f), new Vector3(.01f, .58f, -.01f), new Vector3(.10f, .70f, .01f),
            new Vector3(-.04f, .78f, 0f), new Vector3(.06f, .87f, .01f), new Vector3(-.025f, .925f, 0f), new Vector3(.02f, .965f, 0f),
            new Vector3(0f, 1f, 0f),
        };
        // Fat at every knee that turns out, thin on the way back: the outline of a drawn bolt, not of a wire.
        private static readonly float[] ZkBoltHalf = { .034f, .040f, .030f, .042f, .030f, .040f, .028f, .036f, .026f, .030f, .022f, .014f, 0f };

        // The forks. `Knee` is the knee of the bolt each one leaves from; its points are steps from that knee in the bolt's
        // own space. The first four are still there when the world stops and hang with it; the last two fork off and die
        // on the way down, which is what a fork does when nothing has stopped it.
        private sealed class ZkFork { public int Knee; public float Born, Lives; public Vector3[] Spine; public float[] Half; }
        private static readonly ZkFork[] ZkForkTable =
        {
            new ZkFork { Knee = 3, Born = 1.56f, Lives = 0f, Half = new[] { .022f, .020f, .016f, .013f, .008f, 0f },
                Spine = new[] { new Vector3(0, 0, 0), new Vector3(.10f, .025f, .02f), new Vector3(.16f, .060f, 0), new Vector3(.27f, .075f, .03f), new Vector3(.33f, .115f, .01f), new Vector3(.42f, .125f, .02f) } },
            new ZkFork { Knee = 4, Born = 1.63f, Lives = 0f, Half = new[] { .020f, .018f, .015f, .011f, .007f, 0f },
                Spine = new[] { new Vector3(0, 0, 0), new Vector3(-.09f, .030f, -.02f), new Vector3(-.17f, .050f, 0), new Vector3(-.22f, .090f, -.03f), new Vector3(-.33f, .100f, 0), new Vector3(-.37f, .140f, -.02f) } },
            new ZkFork { Knee = 7, Born = 1.72f, Lives = 0f, Half = new[] { .017f, .015f, .011f, .007f, 0f },
                Spine = new[] { new Vector3(0, 0, 0), new Vector3(.08f, .030f, .02f), new Vector3(.11f, .060f, 0), new Vector3(.19f, .070f, .02f), new Vector3(.22f, .100f, 0) } },
            new ZkFork { Knee = 8, Born = 1.80f, Lives = 0f, Half = new[] { .015f, .013f, .009f, 0f },
                Spine = new[] { new Vector3(0, 0, 0), new Vector3(-.08f, .020f, 0), new Vector3(-.13f, .050f, .02f), new Vector3(-.20f, .055f, 0) } },
            new ZkFork { Knee = 1, Born = 1.52f, Lives = .20f, Half = new[] { .018f, .016f, .012f, .008f, 0f },
                Spine = new[] { new Vector3(0, 0, 0), new Vector3(.10f, .030f, 0), new Vector3(.17f, .045f, .02f), new Vector3(.24f, .080f, 0), new Vector3(.34f, .085f, 0) } },
            new ZkFork { Knee = 6, Born = 1.62f, Lives = .18f, Half = new[] { .016f, .013f, .009f, 0f },
                Spine = new[] { new Vector3(0, 0, 0), new Vector3(-.10f, .020f, 0), new Vector3(-.15f, .050f, 0), new Vector3(-.26f, .060f, 0) } },
        };

        // The far bolt, and the one fork it throws.
        private static readonly Vector3[] ZkAnswerSpine =
        {
            new Vector3(0f, 0f, 0f), new Vector3(-.11f, .12f, 0f), new Vector3(.07f, .24f, 0f), new Vector3(-.05f, .40f, 0f), new Vector3(.12f, .53f, 0f),
            new Vector3(-.02f, .66f, 0f), new Vector3(.08f, .80f, 0f), new Vector3(-.03f, .91f, 0f), new Vector3(0f, 1f, 0f),
        };
        private static readonly float[] ZkAnswerHalf = { .036f, .042f, .032f, .040f, .030f, .034f, .024f, .016f, 0f };
        private static readonly Vector3[] ZkAnswerForkSpine = { new Vector3(0, 0, 0), new Vector3(.12f, .04f, 0), new Vector3(.17f, .10f, 0), new Vector3(.30f, .13f, 0), new Vector3(.34f, .20f, 0) };
        private static readonly float[] ZkAnswerForkHalf = { .022f, .019f, .014f, .009f, 0f };

        // The small arcs: three shapes, root (y 0) to end (y 1). Which one an arc wears is its place in the pool.
        private static readonly Vector3[][] ZkArcSpines =
        {
            new[] { new Vector3(0, 0, 0), new Vector3(.10f, .16f, .04f), new Vector3(-.06f, .30f, -.05f), new Vector3(.12f, .47f, .02f), new Vector3(-.09f, .62f, .05f), new Vector3(.05f, .80f, -.03f), new Vector3(0, 1, 0) },
            new[] { new Vector3(0, 0, 0), new Vector3(-.12f, .12f, -.03f), new Vector3(.05f, .27f, .06f), new Vector3(-.08f, .41f, .01f), new Vector3(.13f, .58f, -.04f), new Vector3(.02f, .74f, .05f), new Vector3(-.05f, .88f, 0), new Vector3(0, 1, 0) },
            new[] { new Vector3(0, 0, 0), new Vector3(.07f, .20f, -.06f), new Vector3(.14f, .36f, .03f), new Vector3(-.10f, .52f, -.02f), new Vector3(.04f, .70f, .06f), new Vector3(-.06f, .86f, -.03f), new Vector3(0, 1, 0) },
        };
        private static readonly float[][] ZkArcHalves =
        {
            new[] { .020f, .030f, .024f, .032f, .024f, .018f, 0f },
            new[] { .018f, .028f, .022f, .030f, .034f, .024f, .016f, 0f },
            new[] { .022f, .030f, .034f, .026f, .028f, .018f, 0f },
        };

        // The city. Angle is round the stage from straight ahead of him (0), clockwise from above; every lens of the
        // cutscene looks across the back half (90 to 270), so that half is built up and the front is a few roofs.
        // ⚠️ KEPT LOW: from the call's lens a 5 m roof at this distance reaches the top of the frame, and the sky is where
        // the storm, the far bolt and the real one are. The near ring stops about level with his head.
        private static readonly (string model, float angle, float radius, float scale)[] ZkNearCity =
        {
            ("zack_row", 93f, 10.5f, .78f), ("zack_condo", 104f, 10.3f, .56f), ("zack_office", 116f, 10.5f, .66f), ("zack_flats", 129f, 10.3f, .62f),
            ("zack_mast", 139f, 10.6f, .50f), ("zack_row", 150f, 10.4f, .84f), ("zack_condo", 161f, 10.6f, .64f), ("zack_office", 172f, 10.3f, .60f),
            ("zack_flats", 184f, 10.5f, .70f), ("zack_row", 196f, 10.3f, .74f), ("zack_mast", 206f, 10.6f, .54f), ("zack_condo", 216f, 10.4f, .58f),
            ("zack_office", 228f, 10.5f, .70f), ("zack_flats", 240f, 10.3f, .60f), ("zack_row", 252f, 10.5f, .80f), ("zack_condo", 263f, 10.4f, .62f),
            ("zack_office", 277f, 10.5f, .64f), ("zack_flats", 296f, 10.5f, .66f), ("zack_row", 322f, 10.6f, .80f), ("zack_condo", 350f, 10.5f, .60f),
            ("zack_office", 18f, 10.6f, .66f), ("zack_flats", 44f, 10.5f, .64f), ("zack_row", 68f, 10.6f, .78f),
        };
        private static readonly (string model, float angle, float radius, float scale)[] ZkFarCity =
        {
            ("zack_condo", 98f, 11.5f, .90f), ("zack_mast", 110f, 11.5f, .74f), ("zack_flats", 123f, 11.5f, 1.02f), ("zack_condo", 134f, 11.5f, .80f),
            ("zack_office", 145f, 11.5f, 1.16f), ("zack_mast", 156f, 11.5f, .82f), ("zack_condo", 167f, 11.5f, .96f), ("zack_flats", 178f, 11.5f, .92f),
            ("zack_mast", 190f, 11.5f, .70f), ("zack_condo", 201f, 11.5f, .86f), ("zack_office", 211f, 11.5f, 1.10f), ("zack_flats", 222f, 11.5f, 1.04f),
            ("zack_condo", 234f, 11.5f, .92f), ("zack_mast", 246f, 11.5f, .78f), ("zack_flats", 258f, 11.5f, .96f), ("zack_condo", 270f, 11.5f, .84f),
            ("zack_mast", 300f, 11.5f, .76f), ("zack_condo", 335f, 11.5f, .90f), ("zack_flats", 28f, 11.5f, 1.0f), ("zack_mast", 58f, 11.5f, .74f), ("zack_condo", 80f, 11.5f, .88f),
        };

        // The clouds: which one, where it starts round the stage, how far out, how high, how big, and how fast it slides
        // (degrees a second of the WORLD's clock). The first seven are the low ring over the court; the last four are a
        // higher, slower ring going the other way, which is what gives the sky a back and a front.
        private static readonly (string model, float angle, float radius, float height, float scale, float drift)[] ZkCloudTable =
        {
            ("zack_cloud_a", 0f, 9.4f, 8.3f, 1.10f, 9f), ("zack_cloud_b", 52f, 9.8f, 9.1f, 1.20f, 9f), ("zack_cloud_a", 104f, 9.5f, 8.0f, 1.25f, 9f),
            ("zack_cloud_b", 152f, 9.6f, 8.8f, 1.30f, 9f), ("zack_cloud_a", 204f, 9.3f, 8.1f, 1.15f, 9f), ("zack_cloud_b", 256f, 9.7f, 9.2f, 1.20f, 9f),
            ("zack_cloud_a", 308f, 9.5f, 8.4f, 1.20f, 9f),
            ("zack_cloud_b", 30f, 11.0f, 11.6f, 1.90f, -5f), ("zack_cloud_a", 120f, 11.2f, 12.2f, 2.10f, -5f), ("zack_cloud_b", 210f, 11.0f, 11.4f, 2.00f, -5f),
            ("zack_cloud_a", 300f, 11.1f, 12.0f, 1.90f, -5f),
        };

        // Where the rain is seen landing: x, z, where in its splash each crown is when the world stops, and its size.
        // Every one is typed to be part of the way through at the stop, so the court is covered in crowns that never fall.
        private static readonly Vector4[] ZkSplashAt =
        {
            new Vector4(-1.3f, 1.2f, .42f, 1.3f), new Vector4(1.1f, .9f, .55f, .8f), new Vector4(.5f, 2.1f, .35f, 1.5f), new Vector4(-.6f, -1.4f, .62f, 1.0f),
            new Vector4(2.2f, -.4f, .48f, 1.6f), new Vector4(-2.4f, .3f, .58f, .9f), new Vector4(1.6f, 2.8f, .40f, 1.2f), new Vector4(-1.8f, 2.6f, .66f, .7f),
            new Vector4(3.1f, 1.6f, .50f, 1.4f), new Vector4(-3.2f, -1.5f, .36f, 1.7f), new Vector4(.2f, -2.6f, .60f, 1.1f), new Vector4(-.4f, 3.4f, .45f, .8f),
        };

        // ---------------------------------------------------------------------------------------- building it

        private void BuildZack()
        {
            _stormLow = Wall("StormGround", 0, 1.3f, new Color(.17f, .165f, .2f, 1f), radius: 12f);
            // The sky is one surface that darkens upward: a warm haze where the city's own light is, night at the top.
            _stormHigh = Add("StormSky", ZackSkyMesh(28), Color.white, .1f, plain: true);
            _pieces[_stormHigh].Transform.localScale = new Vector3(12f, 1f, 12f);
            // The storm is a backdrop, not a sun-lit surface: keep dark hair legible.
            ZackUnlitBackdrop(_stormLow); ZackUnlitBackdrop(_stormHigh);

            BuildZackCity();
            BuildZackClouds();
            // Light behind the far roofs, low on the sky, where the storm is still coming from.
            for (int i = 0; i < 4; i++) _zkSheets.Add(AddGlow("StormSheet" + i, new Color(.78f, .74f, 1f, 1f), falloff: 1.6f, core: .2f));
            _answerSheet = AddGlow("AnswerSheet", ZackLight, falloff: 1.5f, core: .35f);

            _fingerSpark = Add("FingerSpark", ZackSparkMesh(), ZackGold, .9f);
            _fingerGlow = AddGlow("FingerSparkGlow", ZackLight, falloff: 2.2f, core: .5f);
            _answerBolt = Add("AnswerBolt", ZackBoltMesh(ZkAnswerSpine, ZkAnswerHalf, 1f, "ZackAnswerBolt"), ZackGold, .9f);
            _answerCore = Add("AnswerBoltCore", ZackBoltMesh(ZkAnswerSpine, ZkAnswerHalf, .42f, "ZackAnswerCore"), ZackCore, 1.2f);
            _answerFork = Add("AnswerBoltFork", ZackBoltMesh(ZkAnswerForkSpine, ZkAnswerForkHalf, 1f, "ZackAnswerFork"), ZackGold, .9f);
            _answerHalo = AddGlow("AnswerBoltHalo", ZackLight, ZackRibbonMesh(ZkAnswerSpine, .22f), billboard: false, band: true, falloff: 1.5f, core: .3f);
            ZackDrawAfter(_answerCore, 1);

            _snapBolt = Add("SnapBolt", ZackBoltMesh(ZkBoltSpine, ZkBoltHalf, 1.15f, "ZackBolt"), ZackGold, 1f);
            _snapCore = Add("SnapBoltCore", ZackBoltMesh(ZkBoltSpine, ZkBoltHalf, .4f, "ZackBoltCore"), ZackCore, 1.3f);
            _snapHalo = AddGlow("SnapBoltHalo", ZackDeep, ZackRibbonMesh(ZkBoltSpine, .11f), billboard: false, band: true, falloff: 1.5f, core: .3f);
            ZackDrawAfter(_snapCore, 1);
            for (int i = 0; i < ZkForkTable.Length; i++)
            {
                var fork = ZkForkTable[i];
                _zkForks.Add(Add("BoltFork" + i, ZackBoltMesh(fork.Spine, fork.Half, 1f, "ZackFork"), ZackGold, 1f));
                _zkForkCores.Add(Add("BoltForkCore" + i, ZackBoltMesh(fork.Spine, fork.Half, .4f, "ZackForkCore"), ZackCore, 1.3f));
                ZackDrawAfter(_zkForkCores[i], 1);
            }
            _snapGlow = AddGlow("SnapGlow", new Color(1, .85f, .3f, .5f), billboard: false, falloff: 2.5f, core: .35f);
            _zkTip = AddGlow("HungBoltTip", new Color(1, .9f, .5f, 1f), falloff: 2.2f, core: .5f);
            _zkTipSpark = Add("HungBoltTipSpark", ZackSparkMesh(), ZackCore, 1.2f);
            ZackDrawAfter(_zkTipSpark, 2);
            _zkPop = Add("PokeSpark", ZackSparkMesh(), new Color(1, .95f, .7f, 1f), 1f);
            _zkPopGlow = AddGlow("PokeGlow", ZackLight, falloff: 2f, core: .6f);
            ZackDrawAfter(_zkPop, 2);
            // Poked, it rings: two hoops run up it from the tip.
            for (int i = 0; i < 2; i++) _zkWire.Add(Add("BoltRings" + i, VfxShapes.Collar(14, .12f, .78f), ZackCore, 1.1f));
            for (int i = 0; i < 8; i++)
                _zkArcs.Add(Add("Arc" + i, ZackBoltMesh(ZkArcSpines[i % 3], ZkArcHalves[i % 3], 1f, "ZackArc"), i % 2 == 0 ? ZackCore : ZackGold, 1.2f));

            // The stop: two rings over the court and a dome over everybody.
            _zkTick = Add("WorldStops", VfxShapes.Collar(40, .02f, .96f), new Color(.8f, .86f, 1f, .7f), .5f);
            _zkTick2 = Add("WorldStopsEcho", VfxShapes.Collar(40, .012f, .975f), new Color(.8f, .86f, 1f, .5f), .4f);
            _zkDome = Add("WorldStopsDome", VfxShapes.NovaShell(5, 14), new Color(.74f, .82f, 1f, .13f), .3f);

            // The strike: a column of light, what it leaves on the court, and the ring out to the ultimate's reach.
            _zkColumn = AddGlow("StrikeColumn", ZackLight, falloff: 1.7f, core: .5f);
            _zkScorch = Add("StrikeScorch", VfxShapes.Wedges(9, .22f, 9f, .05f, .3f, 41), new Color(.09f, .07f, .05f, .9f), 0f);
            _zkCracks = Add("StrikeCracks", VfxShapes.Fracture(6, 3, .05f, 57, .12f), ZackGold, 1.1f);
            ZackDrawAfter(_zkCracks, 1);
            _zkMark = AddGlow("OverclockMark", new Color(1, .8f, .25f, 1f), billboard: false, falloff: 1.8f, core: .3f);
            _zkAura = AddGlow("OverclockAura", new Color(1, .8f, .25f, 1f), falloff: 1.5f, core: .15f);
            _zkRing = Add("OverclockRing", VfxShapes.Collar(40, .09f, .9f), ZackGold, 1f);
            _zkRingTeeth = Add("OverclockRingTeeth", VfxShapes.TwoSided(VfxShapes.Corona(26, .84f, .55f, 23)), ZackCore, 1.1f);
            _zkRing2 = Add("OverclockRingEcho", VfxShapes.Collar(40, .03f, .95f), ZackGold, .8f);
            for (int i = 0; i < 4; i++) _zkMotes.Add(Add("ChargeSpark" + i, ZackSparkMesh(), ZackCore, 1.2f));

            // The rain. A drop when it hangs, pulled into a streak when it falls; each has a glint on its lit side.
            for (int i = 0; i < 56; i++)
            {
                _zkRain.Add(Add("Rain" + i, ZackDropMesh(), new Color(.70f, .80f, .96f, .78f), .4f));
                _zkRainShine.Add(Add("RainGlint" + i, VfxShapes.Prism(3, 1, .4f), new Color(1f, 1f, 1f, .95f), .9f));
                ZackDrawAfter(_zkRainShine[i], 1);
            }
            for (int i = 0; i < ZkSplashAt.Length; i++)
            {
                _zkSplash.Add(Add("Splash" + i, ZackCrownMesh(i), new Color(.78f, .86f, 1f, .8f), .45f));
                _zkRipple.Add(Add("SplashRing" + i, VfxShapes.Collar(12, .03f, .82f), new Color(.78f, .86f, 1f, .6f), .35f));
            }

            // Everyone else where they stand, for the stop to turn into statues and the ring to reach or not reach.
            StageOthers(2.6f, 9f);
            for (int i = 0; i < _others.Count && i < 4; i++)
            {
                _zkJolts.Add(Add("ZapJolt" + i, ZackSparkMesh(), ZackGold, 1f));
                _zkJoltGlows.Add(AddGlow("ZapJoltGlow" + i, ZackLight, falloff: 2f, core: .5f));
                // The mark the stop closes under each of them, and holds for as long as they are held.
                // A ring and a dial of twelve marks inside it: a clock face, stopped. The rain's own rings are plain.
                _zkHolds.Add(Add("Held" + i, VfxShapes.Collar(24, .03f, .9f), new Color(.86f, .9f, 1f, .85f), .6f));
                _zkDials.Add(Add("HeldDial" + i, VfxShapes.TwoSided(VfxShapes.Corona(12, .78f, 0f, 5)), new Color(.86f, .9f, 1f, .85f), .6f));
            }
        }

        /// <summary>
        /// The city: `Models/UltimateProps/zack_*`, five kinds of building typed part by part in
        /// `tools/build_ultimate_props_zack.py` and painted in one atlas beside them, dressed as the cast is. The far ring
        /// wears the same paint pulled into the storm's haze. A missing model leaves a gap in the skyline and the cutscene plays on.
        /// </summary>
        private void BuildZackCity()
        {
            for (int ring = 0; ring < 2; ring++)
                foreach (var (model, angle, radius, scale) in ring == 0 ? ZkFarCity : ZkNearCity)
                {
                    var go = UltimateProp.Spawn(model, _root.transform, ring == 0 ? UltimateProp.StormFarPalette : UltimateProp.StormPalette);
                    if (go == null) continue;
                    // A painted front takes no palette, so the far ring is also tinted into the haze.
                    if (ring == 0)
                    {
                        var haze = new MaterialPropertyBlock();
                        foreach (var surface in go.GetComponentsInChildren<Renderer>(true))
                        { surface.GetPropertyBlock(haze); haze.SetColor("_Color", UltimateProp.StormFarTint); surface.SetPropertyBlock(haze); }
                    }
                    float a = angle * Mathf.Deg2Rad;
                    var at = new Vector3(Mathf.Sin(a) * radius, 0, Mathf.Cos(a) * radius);
                    // Its front is +Z and every lens is inside the ring: turn it to face him.
                    go.transform.localPosition = at;
                    go.transform.localRotation = Quaternion.LookRotation(-at.normalized, Vector3.up);
                    go.transform.localScale = new Vector3(scale, .001f, scale);
                    go.SetActive(false);
                    _zkTowers.Add(go.transform);
                }
        }

        private void BuildZackClouds()
        {
            for (int i = 0; i < ZkCloudTable.Length; i++)
            {
                var go = UltimateProp.Spawn(ZkCloudTable[i].model, _root.transform, UltimateProp.StormPalette);
                _zkCloudProps.Add(go != null ? go.transform : null);
                if (go != null) go.SetActive(false);
                // The light inside it, seen on its underside: a patch of glow lying just under the belly.
                _zkBellies.Add(AddGlow("CloudBelly" + i, ZackLight, billboard: false, falloff: 1.7f, core: .35f));
            }
            var top = UltimateProp.Spawn("zack_cloud_top", _root.transform, UltimateProp.StormPalette);
            if (top != null) { _zkCloudTop = top.transform; top.SetActive(false); }
            _zkThroat = AddGlow("CloudThroat", ZackLight, billboard: false, falloff: 1.6f, core: .5f);
        }

        private void ZackUnlitBackdrop(int index)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) return;
            var piece = _pieces[index];
            // Drawn before every other see-through piece of the stage: sorted by distance with them, the sky was being
            // painted over whatever stood further from the lens than the stage's middle (the far bolt, the far rain).
            var material = new Material(shader) { name = "Isagani storm backdrop", color = piece.Color, renderQueue = 2900 };
            piece.Renderer.sharedMaterial = material;
            VfxRenderTag.Own(piece.Renderer.gameObject, material);
        }

        /// <summary>
        /// A piece that sits inside or on another see-through piece (the core in the bolt, the glint on a drop) is drawn
        /// after it. They share a middle, so the distance sort alone would let either hide the other from frame to frame.
        /// </summary>
        private void ZackDrawAfter(int index, int steps)
        {
            var material = _pieces[index].Renderer.sharedMaterial;
            if (material != null) material.renderQueue = 3000 + steps;
        }

        // ---------------------------------------------------------------------------------------- the meshes

        private static void ZkTri(List<Vector3> tris, Vector3 a, Vector3 b, Vector3 c, Vector3 inside)
        {
            // A face with no area has no normal, and a lit piece with no normal draws black: leave it out.
            var normal = Vector3.Cross(b - a, c - a);
            if (normal.sqrMagnitude < 1e-12f) return;
            if (Vector3.Dot(normal, (a + b + c) / 3f - inside) < 0) { var swap = b; b = c; c = swap; }
            tris.Add(a); tris.Add(b); tris.Add(c);
        }

        /// <summary>Every face its own three corners, so each one catches the light by itself: cut, not smooth.</summary>
        private static Mesh ZkFaceted(List<Vector3> tris, string name)
        {
            var mesh = new Mesh { name = name };
            var index = new int[tris.Count];
            for (int i = 0; i < index.Length; i++) index[i] = i;
            mesh.SetVertices(tris); mesh.SetTriangles(index, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// A limb of lightning through typed knees: a blade of four edges, `half` wide at each knee (times
        /// <paramref name="girth"/>) and a little over half that deep, closed to a point wherever a half is 0. Wide across
        /// x because the lenses stand in front of him; deep enough that the travelling shot never sees it edge on.
        /// </summary>
        private static Mesh ZackBoltMesh(Vector3[] spine, float[] half, float girth, string name)
        {
            var tris = new List<Vector3>(spine.Length * 24);
            var before = new Vector3[4]; var after = new Vector3[4];
            for (int i = 0; i + 1 < spine.Length; i++)
            {
                float h0 = half[i] * girth, h1 = half[i + 1] * girth;
                Vector3 p0 = spine[i], p1 = spine[i + 1], inside = (p0 + p1) * .5f;
                ZkBladeRing(p0, h0, before); ZkBladeRing(p1, h1, after);
                for (int k = 0; k < 4; k++)
                {
                    int n = (k + 1) % 4;
                    ZkTri(tris, before[k], before[n], after[n], inside);
                    ZkTri(tris, before[k], after[n], after[k], inside);
                }
                // The root is cut square, so an arc that starts in the open has an end and not a hole.
                if (i == 0 && h0 > 0)
                {
                    ZkTri(tris, before[0], before[1], before[2], inside);
                    ZkTri(tris, before[0], before[2], before[3], inside);
                }
            }
            return ZkFaceted(tris, name);
        }

        private static void ZkBladeRing(Vector3 at, float half, Vector3[] ring)
        {
            ring[0] = at + new Vector3(half, 0, 0); ring[1] = at + new Vector3(0, 0, half * .6f);
            ring[2] = at - new Vector3(half, 0, 0); ring[3] = at - new Vector3(0, 0, half * .6f);
        }

        /// <summary>A flat band along the same knees, for the light round a bolt: v runs across it, which is what the glow fades over.</summary>
        private static Mesh ZackRibbonMesh(Vector3[] spine, float half)
        {
            var mesh = new Mesh { name = "ZackBoltLight" };
            var vertices = new Vector3[spine.Length * 2]; var uv = new Vector2[spine.Length * 2];
            var triangles = new int[(spine.Length - 1) * 6];
            for (int i = 0; i < spine.Length; i++)
            {
                float u = i / (spine.Length - 1f);
                // It closes with the bolt at the tip instead of ending in a square of light.
                float w = half * (i == spine.Length - 1 ? .35f : 1f);
                vertices[i * 2] = spine[i] - new Vector3(w, 0, 0); vertices[i * 2 + 1] = spine[i] + new Vector3(w, 0, 0);
                uv[i * 2] = new Vector2(u, 0); uv[i * 2 + 1] = new Vector2(u, 1);
                if (i == spine.Length - 1) continue;
                int t = i * 6, n = i * 2;
                triangles[t] = n; triangles[t + 1] = n + 1; triangles[t + 2] = n + 2;
                triangles[t + 3] = n + 2; triangles[t + 4] = n + 1; triangles[t + 5] = n + 3;
            }
            mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>A spark with a shape from every side: three spindles crossed, the upright one longest. Fits a unit ball.</summary>
        private static Mesh ZackSparkMesh()
        {
            var tris = new List<Vector3>(72);
            ZkSpindle(tris, Vector3.up, Vector3.right, Vector3.forward, 1f, .13f);
            ZkSpindle(tris, Vector3.right, Vector3.forward, Vector3.up, .68f, .11f);
            ZkSpindle(tris, Vector3.forward, Vector3.up, Vector3.right, .5f, .10f);
            return ZkFaceted(tris, "ZackSpark");
        }

        private static void ZkSpindle(List<Vector3> tris, Vector3 axis, Vector3 side, Vector3 other, float reach, float waist)
        {
            Vector3 a = axis * reach, b = -axis * reach;
            Vector3[] ring = { side * waist, other * waist, -side * waist, -other * waist };
            for (int k = 0; k < 4; k++)
            {
                ZkTri(tris, ring[k], ring[(k + 1) % 4], a, Vector3.zero);
                ZkTri(tris, ring[k], ring[(k + 1) % 4], b, Vector3.zero);
            }
        }

        /// <summary>A raindrop: a round bottom drawn up to a point. One unit tall, one across at its fattest.</summary>
        private static Mesh ZackDropMesh()
        {
            var tris = new List<Vector3>(72);
            Vector3 bottom = Vector3.zero, top = Vector3.up, inside = new Vector3(0, .35f, 0);
            var low = new Vector3[6]; var high = new Vector3[6];
            for (int k = 0; k < 6; k++)
            {
                float a = k * Mathf.PI / 3f;
                low[k] = new Vector3(Mathf.Sin(a) * .5f, .26f, Mathf.Cos(a) * .5f);
                high[k] = new Vector3(Mathf.Sin(a) * .3f, .56f, Mathf.Cos(a) * .3f);
            }
            for (int k = 0; k < 6; k++)
            {
                int n = (k + 1) % 6;
                ZkTri(tris, bottom, low[k], low[n], inside);
                ZkTri(tris, low[k], low[n], high[n], inside); ZkTri(tris, low[k], high[n], high[k], inside);
                ZkTri(tris, high[k], high[n], top, inside);
            }
            return ZkFaceted(tris, "ZackDrop");
        }

        /// <summary>
        /// A drop landing: a crown of seven points thrown up and out, tall and short by turns, a hump in the middle and
        /// three beads thrown clear. One unit across its points and one tall. <paramref name="turn"/> only turns it, so
        /// two crowns side by side are not the same picture.
        /// </summary>
        private static Mesh ZackCrownMesh(int turn)
        {
            var tris = new List<Vector3>(140);
            for (int k = 0; k < 7; k++)
            {
                float a = (k / 7f + turn * .037f) * Mathf.PI * 2f;
                Vector3 left = ZkRound(a - .3f, .56f, 0), right = ZkRound(a + .3f, .56f, 0), back = ZkRound(a, .40f, 0);
                Vector3 point = ZkRound(a, 1f, k % 2 == 0 ? 1f : .68f);
                var inside = (left + right + back + point) * .25f;
                ZkTri(tris, left, right, point, inside); ZkTri(tris, right, back, point, inside);
                ZkTri(tris, back, left, point, inside); ZkTri(tris, left, back, right, inside);
            }
            for (int k = 0; k < 5; k++)
                ZkTri(tris, ZkRound(k * 1.2566f, .34f, 0), ZkRound((k + 1) * 1.2566f, .34f, 0), new Vector3(0, .42f, 0), new Vector3(0, .05f, 0));
            ZkBead(tris, new Vector3(.82f, 1.22f, .24f), .10f);
            ZkBead(tris, new Vector3(-.50f, 1.05f, -.74f), .08f);
            ZkBead(tris, new Vector3(-.36f, 1.36f, .66f), .07f);
            return ZkFaceted(tris, "ZackSplashCrown");
        }

        private static Vector3 ZkRound(float angle, float radius, float height) => new Vector3(Mathf.Sin(angle) * radius, height, Mathf.Cos(angle) * radius);

        private static void ZkBead(List<Vector3> tris, Vector3 at, float size)
        {
            Vector3 up = at + Vector3.up * size * 1.4f, down = at - Vector3.up * size;
            Vector3[] ring = { at + Vector3.right * size, at + Vector3.forward * size, at - Vector3.right * size, at - Vector3.forward * size };
            for (int k = 0; k < 4; k++)
            {
                ZkTri(tris, ring[k], ring[(k + 1) % 4], up, at);
                ZkTri(tris, ring[k], ring[(k + 1) % 4], down, at);
            }
        }

        /// <summary>
        /// The storm's sky: the stage wall from 1.3 m up, closed over the top, with its colour typed ring by ring in the
        /// vertices. A warm haze low down, where a city lights its own air, to night overhead. The backdrop shader takes
        /// vertex colour, so this is one surface with depth instead of one flat grey.
        /// </summary>
        private static Mesh ZackSkyMesh(int sides)
        {
            float[] heights = { 1.3f, 2.6f, 4.6f, 7.5f, 11.5f, 16f };
            Color[] bands =
            {
                new Color(.38f, .32f, .39f, 1f), new Color(.26f, .24f, .31f, 1f), new Color(.17f, .17f, .23f, 1f),
                new Color(.11f, .115f, .16f, 1f), new Color(.075f, .08f, .115f, 1f), new Color(.055f, .06f, .085f, 1f),
            };
            var vertices = new List<Vector3>(); var colours = new List<Color>(); var triangles = new List<int>();
            for (int r = 0; r < heights.Length; r++)
                for (int i = 0; i <= sides; i++)
                {
                    float a = i * Mathf.PI * 2 / sides;
                    vertices.Add(new Vector3(Mathf.Sin(a), heights[r], Mathf.Cos(a))); colours.Add(bands[r]);
                }
            int row = sides + 1;
            for (int r = 0; r + 1 < heights.Length; r++)
                for (int i = 0; i < sides; i++)
                {
                    int n = r * row + i;
                    triangles.AddRange(new[] { n, n + row, n + 1, n + 1, n + row, n + row + 1 });
                }
            int centre = vertices.Count; vertices.Add(new Vector3(0, heights[heights.Length - 1], 0)); colours.Add(new Color(.05f, .055f, .08f, 1f));
            int last = (heights.Length - 1) * row;
            for (int i = 0; i < sides; i++) triangles.AddRange(new[] { centre, last + i, last + i + 1 });
            var mesh = new Mesh { name = "Isagani storm sky" };
            mesh.SetVertices(vertices); mesh.SetColors(colours); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        // ---------------------------------------------------------------------------------------- playing it

        /// <summary>
        /// ⚠️ THE WORLD'S CLOCK. It runs with the real one, brakes to nothing between `ZkSlow` and `ZkStopped`, stands still
        /// until `ZkGo` and then runs again. Everything that is "the world" reads this, so it all stops as one thing.
        /// </summary>
        private static float ZackWorld(float t)
        {
            if (t <= ZkSlow) return t;
            float brake = ZkStopped - ZkSlow;
            float u = Mathf.Clamp01((t - ZkSlow) / brake);
            float stopped = ZkSlow + brake * (u - u * u * .5f);
            return t <= ZkGo ? stopped : stopped + (t - ZkGo);
        }

        /// <summary>The same number for the same input, between 0 and 1: what jumps where, without a random generator the film could not repeat.</summary>
        private static float ZkChance(float n) => Mathf.Repeat(Mathf.Sin(n * 12.9898f) * 43758.547f, 1f);

        /// <summary>The light inside the storm, on the world's clock: mostly dark, a slow swell and a stutter inside it.</summary>
        private static float ZackFlicker(float world, int which)
        {
            float swell = Mathf.Max(0f, Mathf.Sin(world * 6.1f + which * 2.4f)), stutter = Mathf.Sin(world * 14.7f + which * 5.1f);
            return Mathf.Pow(swell, 5f) * (.6f + .4f * stutter * stutter);
        }

        /// <summary>
        /// Turn a bolt's own up to run along <paramref name="along"/>, keeping its wide side across the stage. Turning it the
        /// shortest way instead spun the blade edge-on to the lens, and spun it again each time the poked tip moved.
        /// </summary>
        private static Quaternion ZackAim(Vector3 along)
        {
            var up = along.normalized;
            var depth = Vector3.Cross(Vector3.right, up);
            if (depth.sqrMagnitude < 1e-6f) depth = Vector3.forward;
            return Quaternion.LookRotation(depth.normalized, up);
        }

        /// <summary>
        /// ⚠️ WHERE THE BOLT'S TIP IS. It has weight, so none of its moves is a straight slide from one place to the next:
        ///   * it leaves the cloud with a short stab and a hesitation, then falls faster the further it has fallen;
        ///   * the stop catches it at speed, so it runs a hand's width past where it will hang, is pulled back above
        ///     that and settles;
        ///   * poked, it rings like a struck wire;
        ///   * before he lets it go it is drawn back up, as anything is before it is thrown, and then it goes into him
        ///     faster the nearer it gets.
        /// WHEN it leaves, hangs and lands are the stage's own times, and none of this moves them.
        /// </summary>
        private Vector3 ZackTip(float t)
        {
            float u = Mathf.Clamp01((t - 1.48f) / (ZkStopped - 1.48f));
            // The stab is the first fifth of the time and a twentieth of the way; the rest is the fall.
            float fall = Mathf.Clamp01((u - .2f) / .8f);
            float down = u < .2f ? .05f * Ease(0f, .2f, u) : .05f + .95f * fall * fall * (1.6f - .6f * fall);
            Vector3 way = (ZkHung - ZkTop).normalized;
            Vector3 tip = Vector3.Lerp(ZkTop, ZkHung, down);
            float caught = t - ZkStopped;
            if (caught > 0 && t < ZkGo) tip += way * (.24f * Mathf.Sin(caught * 15f) * Mathf.Exp(-caught * 6.5f));
            float since = t - ZkPoke;
            if (since > 0 && t < ZkGo) tip.x += Mathf.Sin(since * 34f) * .07f * Mathf.Exp(-since * 4.5f);
            tip -= way * (.22f * Ease(ZkGo - .3f, ZkGo, t));
            float land = Mathf.Clamp01((t - ZkGo) / (ZkHit - ZkGo));
            return Vector3.Lerp(tip, HeadPoint - Vector3.up * .25f, land * land);
        }

        /// <summary>One small arc of the pool from one point to another, as thick as it is long times <paramref name="girth"/>.</summary>
        private void ZackArc(int slot, Vector3 from, Vector3 to, float girth, float roll, float opacity)
        {
            int index = _zkArcs[slot];
            Vector3 along = to - from; float length = along.magnitude;
            if (opacity <= .002f || length < .02f) { Tint(index, 0); return; }
            Place(index, from, new Vector3(length * girth, length, length * girth),
                Quaternion.FromToRotation(Vector3.up, along / length) * Quaternion.Euler(0, roll, 0), opacity);
        }

        private void SampleZack(float t)
        {
            float leave = 1 - Ease(Seconds - .45f, Seconds - .05f, t);
            float world = ZackWorld(t);
            float hung = Ease(ZkSlow, ZkStopped, t) * (1 - Ease(ZkGo, ZkGo + .1f, t));
            // The storm rolls in as the finger goes up; before that he is just on the court.
            float storm = Ease(.45f, .95f, t) * leave;
            float answer = Flash(t, .82f, .07f), snap = Flash(t, ZkHit + .02f, .1f);
            float light = _reducedEffects ? .5f : 1f;
            var flicker = Color.white * (.86f + .4f * Mathf.Max(answer, snap));
            flicker.a = 1f;
            Tint(_stormLow, storm); Tint(_stormHigh, storm, flicker);

            SampleZackCity(storm);
            SampleZackClouds(t, world, storm, Mathf.Max(answer, snap), light);
            SampleZackRain(t, world, hung, storm);

            // The flick: one spark off the fingertip, and the light it throws on his hand.
            float spark = Ease(.36f, .42f, t) * (1 - Ease(.42f, .62f, t));
            Vector3 sparkAt = FreePalm + new Vector3(0, .1f, .12f) + Vector3.up * Ease(.42f, .62f, t) * .25f;
            Place(_fingerSpark, sparkAt, Vector3.one * (.06f + .1f * Ease(.36f, .44f, t)), Quaternion.Euler(t * 300, t * 400, 20), spark * leave);
            PlaceGlow(_fingerGlow, sparkAt, Vector3.one * .55f, Quaternion.identity, spark * leave * .9f * light);

            // The answer, far off: a whole bolt behind the near roofs, the sheet of light it throws, and one fork.
            float answering = (_reducedEffects ? .6f : 1) * Ease(.78f, .82f, t) * (1 - Ease(.9f, 1.2f, t)) * storm;
            Vector3 answerAlong = ZkAnswerFoot - ZkAnswerTop;
            var answerTurn = ZackAim(answerAlong);
            var answerSize = new Vector3(2.6f, answerAlong.magnitude, 2.6f);
            Place(_answerBolt, ZkAnswerTop, answerSize, answerTurn, answering);
            Place(_answerCore, ZkAnswerTop, answerSize, answerTurn, answering);
            Place(_answerFork, ZkAnswerTop + answerTurn * Vector3.Scale(ZkAnswerSpine[3], answerSize), answerSize * Ease(.79f, .86f, t), answerTurn,
                answering * (1 - Ease(.9f, 1.02f, t)));
            PlaceGlow(_answerHalo, ZkAnswerTop, answerSize, answerTurn, answering * .8f * light);
            PlaceGlow(_answerSheet, Vector3.Lerp(ZkAnswerTop, ZkAnswerFoot, .62f) - Vector3.forward * .9f, new Vector3(12f, 9f, 1f), Quaternion.identity,
                (answer * .55f + answering * .25f) * light);

            SampleZackBolt(t, storm, leave, snap, light);
            SampleZackStop(t, storm);
            SampleZackStrike(t, storm, leave, light);
            SampleZackArcs(t, leave);
        }

        /// <summary>The city comes up with the storm, a building at a time, and goes down with it.</summary>
        private void SampleZackCity(float storm)
        {
            for (int i = 0; i < _zkTowers.Count; i++)
            {
                var tower = _zkTowers[i];
                float rise = Mathf.SmoothStep(0, 1, Mathf.Clamp01(storm * 1.5f - .5f * ZkChance(i * 3.7f)));
                tower.gameObject.SetActive(rise > .01f);
                var scale = tower.localScale;
                // A little past its height and back, so it lands instead of stopping.
                scale.y = scale.x * rise * (1 + .07f * Mathf.Sin(rise * Mathf.PI));
                tower.localScale = scale;
            }
        }

        private void SampleZackClouds(float t, float world, float storm, float flash, float light)
        {
            // The clouds slide overhead on the world's clock, so they stop with it, and so does the light inside them.
            for (int i = 0; i < ZkCloudTable.Length; i++)
            {
                var (_, angle, radius, height, scale, drift) = ZkCloudTable[i];
                float a = (angle + world * drift) * Mathf.Deg2Rad;
                float puff = Mathf.SmoothStep(0, 1, Mathf.Clamp01(storm * 1.4f - .4f * ZkChance(i * 5.3f)));
                var at = new Vector3(Mathf.Sin(a) * radius, height + (1 - puff) * 2.2f, Mathf.Cos(a) * radius);
                // Its long side lies along the ring, so from inside the stage it is seen broadside.
                var face = Quaternion.LookRotation(-new Vector3(at.x, 0, at.z).normalized, Vector3.up);
                var prop = _zkCloudProps[i];
                if (prop != null)
                {
                    prop.gameObject.SetActive(puff > .01f);
                    prop.localPosition = at; prop.localRotation = face;
                    prop.localScale = Vector3.one * (scale * puff * (1 + .06f * Mathf.Sin(puff * Mathf.PI)));
                }
                float lit = (.10f + .85f * ZackFlicker(world, i) + .7f * flash) * puff * light;
                PlaceGlow(_zkBellies[i], at + Vector3.up * (.34f * scale), new Vector3(4.8f, 3f, 1f) * scale, face * Quaternion.Euler(90, 0, 0), lit * storm);
            }
            // The cloud the bolt comes out of: it turns slowly, and its throat is lit for as long as the bolt is in it.
            float throat = Ease(1.4f, 1.56f, t) * (1 - Ease(ZkHit + .1f, ZkHit + .5f, t));
            Vector3 over = ZkTop + Vector3.up * .15f;
            if (_zkCloudTop != null)
            {
                float puff = Mathf.SmoothStep(0, 1, Mathf.Clamp01(storm * 1.3f - .3f));
                _zkCloudTop.gameObject.SetActive(puff > .01f);
                _zkCloudTop.localPosition = over + Vector3.up * (1 - puff) * 2f;
                _zkCloudTop.localRotation = Quaternion.Euler(0, world * 4f, 0);
                _zkCloudTop.localScale = Vector3.one * (1.15f * puff);
            }
            PlaceGlow(_zkThroat, over + Vector3.up * .3f, new Vector3(5f, 5f, 1f), Quaternion.Euler(90, 0, 0),
                (.25f * ZackFlicker(world, 20) + throat * (.8f + .1f * Mathf.Sin(t * 5f)) + flash) * storm * light);

            // Sheet light low behind the far roofs, on the world's clock: the roofs are cut out against it.
            for (int i = 0; i < _zkSheets.Count; i++)
            {
                float a = (128f + i * 40f) * Mathf.Deg2Rad;
                PlaceGlow(_zkSheets[i], new Vector3(Mathf.Sin(a) * 11.85f, 3.4f + (i % 2) * .8f, Mathf.Cos(a) * 11.85f), new Vector3(9f, 5.5f, 1f), Quaternion.identity,
                    (.06f + .5f * ZackFlicker(world, 30 + i * 3) + .3f * flash) * storm * light);
            }
        }

        private void SampleZackRain(float t, float world, float hung, float storm)
        {
            // The rain: falling on the world's clock. Falling, each drop is pulled out into a streak; hung, it is a drop
            // again, fat, with the glint on it, and it stays exactly where the stop found it.
            const float ceiling = 8f;
            float raining = Ease(.8f, 1.2f, t) * storm;
            for (int i = 0; i < _zkRain.Count; i++)
            {
                float a = i * 2.39996f, r = 1.1f + (i * 37 % 23) / 23f * 7.4f;
                float y = ceiling - Mathf.Repeat(i * 1.37f + world * 10.5f, ceiling);
                float size = .8f + .5f * ZkChance(i * 1.9f);
                var at = new Vector3(Mathf.Sin(a) * r, y, Mathf.Cos(a) * r);
                var shape = Vector3.Lerp(new Vector3(.034f, .7f, .034f), new Vector3(.13f, .28f, .13f) * size, hung);
                Place(_zkRain[i], at, shape, Quaternion.Euler(4 * (1 - hung), 0, 3 * (1 - hung)), raining * .9f);
                Place(_zkRainShine[i], at + new Vector3(-.036f, .11f, .036f) * size, new Vector3(.03f, .09f, .03f) * size, Quaternion.Euler(0, 0, 14), raining * hung);
            }
            // Where it lands. Each crown is thrown up and falls back on the world's clock, so the stop catches every one
            // of them in the air, and the court is covered in crowns that do not come down until he lets them.
            for (int i = 0; i < _zkSplash.Count; i++)
            {
                var at = ZkSplashAt[i];
                float u = Mathf.Repeat((world - 1.87f) * 2.2f + at.z, 1f);
                // Thrown up fast, hanging at the top, falling back; spreading fast and slowing.
                float up = Mathf.Sin(u * Mathf.PI), spread = 1 - (1 - u) * (1 - u), wide = at.w * (.09f + .15f * spread);
                Place(_zkSplash[i], new Vector3(at.x, .02f, at.y), new Vector3(wide, Mathf.Max(.01f, at.w * .2f * up), wide), Quaternion.identity, raining * (1 - u * u));
                Place(_zkRipple[i], new Vector3(at.x, .025f, at.y), Vector3.one * (at.w * (.12f + .5f * spread)), Quaternion.identity, raining * (1 - u) * .5f);
            }
        }

        private void SampleZackBolt(float t, float storm, float leave, float snap, float light)
        {
            // THE BOLT. It comes down the sky, is stopped an arm's length over his hand, and is let go into him.
            Vector3 tip = ZackTip(t);
            float since = t - ZkPoke;
            float ringing = since > 0 && t < ZkGo ? Mathf.Exp(-since * 4.5f) : 0;
            Vector3 along = tip - ZkTop;
            float length = Mathf.Max(.05f, along.magnitude);
            float bolt = Ease(1.48f, 1.54f, t) * (1 - Ease(ZkHit + .1f, ZkHit + .45f, t)) * storm;
            // It swells as it lands and for the instant it is poked.
            float girth = ZkGirth * (1 + .7f * snap + .25f * ringing * Mathf.Clamp01(since / .04f));
            var size = new Vector3(girth, length, girth);
            var turn = ZackAim(along);
            Place(_snapBolt, ZkTop, size, turn, bolt);
            Place(_snapCore, ZkTop, size, turn, bolt);
            PlaceGlow(_snapHalo, ZkTop, size, turn, bolt * (.34f + .6f * snap + .3f * ringing) * light);

            // The forks leave it at its knees. The four that are out when the world stops hang there with it, and go with
            // it when it lands; the other two are gone before it ever stops.
            for (int i = 0; i < ZkForkTable.Length; i++)
            {
                var fork = ZkForkTable[i];
                float grown = Ease(fork.Born, fork.Born + .07f, t);
                float alive = fork.Lives > 0 ? 1 - Ease(fork.Born + .06f, fork.Born + fork.Lives, t) : 1 - Ease(ZkHit, ZkHit + .14f, t);
                var root = ZkTop + turn * Vector3.Scale(ZkBoltSpine[fork.Knee], size);
                // Out past its length and back, as it is thrown.
                var reach = size * (grown * (1 + .18f * Mathf.Sin(grown * Mathf.PI)));
                Place(_zkForks[i], root, reach, turn, bolt * alive * grown);
                Place(_zkForkCores[i], root, reach, turn, bolt * alive * grown);
            }

            // Its tip burns while it waits, slow and even: the one light in a stopped world.
            float waiting = Ease(ZkStopped - .05f, ZkStopped + .1f, t) * (1 - Ease(ZkGo, ZkHit, t));
            float beat = .5f + .5f * Mathf.Sin(t * 5f);
            PlaceGlow(_zkTip, tip, Vector3.one * (.8f + .1f * (beat * 2 - 1) + .25f * ringing), Quaternion.identity, waiting * leave * (1f + .4f * ringing));
            Place(_zkTipSpark, tip, Vector3.one * (.11f + .035f * beat + .1f * ringing), Quaternion.Euler(t * 40, t * 65, 0), waiting * leave);

            // The poke: a spark where his fist meets it, the light of it, and two hoops that run up the bolt as it rings.
            float pop = Mathf.Clamp01(since / .05f) * (1 - Mathf.Clamp01((since - .05f) / .28f));
            Vector3 met = Vector3.Lerp(FreePalm + Vector3.up * .12f, ZkHung, .5f);
            // It bursts out fast and slows as it fades, as a spark does.
            float burst = 1 - Mathf.Pow(1 - Mathf.Clamp01(since / .3f), 3f);
            Place(_zkPop, met, Vector3.one * (.1f + .26f * burst), Quaternion.Euler(since * 200, since * 300, 30), since > 0 ? pop : 0);
            PlaceGlow(_zkPopGlow, met, Vector3.one * (.4f + .6f * burst), Quaternion.identity, since > 0 ? pop * .55f * light : 0);
            for (int i = 0; i < _zkWire.Count; i++)
            {
                float age = since - i * .12f, u = Mathf.Clamp01(age / .6f);
                // Up the bolt from the tip, quick then slowing, widening as it goes.
                Vector3 at = tip - along.normalized * (.15f + 2.6f * (1 - (1 - u) * (1 - u)));
                Place(_zkWire[i], at, new Vector3(.22f + .5f * u, .5f, .22f + .5f * u), turn, age > 0 && t < ZkGo ? (1 - u) * .9f : 0);
            }
        }

        private void SampleZackStop(float t, float storm)
        {
            // The stop goes out from him across the court: a thin ring, a second one after it, and a dome over everybody.
            float tickAge = t - (ZkStopped - .06f), tickU = Mathf.Clamp01(tickAge / .55f);
            // Each goes out fast and slows as it spreads: a thing that is stopping.
            float tickOut = 1 - (1 - tickU) * (1 - tickU);
            Place(_zkTick, Vector3.up * .05f, Vector3.one * Mathf.Lerp(.4f, 11f, tickOut), Quaternion.identity, tickAge >= 0 ? (1 - tickU) * .9f * storm : 0);
            float echoAge = tickAge - .1f, echoU = Mathf.Clamp01(echoAge / .6f), echoOut = 1 - (1 - echoU) * (1 - echoU);
            Place(_zkTick2, Vector3.up * .05f, Vector3.one * Mathf.Lerp(.3f, 10f, echoOut), Quaternion.identity, echoAge >= 0 ? (1 - echoU) * .7f * storm : 0);
            Place(_zkDome, Vector3.zero, Vector3.one * Mathf.Lerp(.5f, 11.5f, tickOut), Quaternion.Euler(0, 20, 0), tickAge >= 0 ? (1 - tickU * tickU) * storm : 0);

            // A mark closes under each player as the dome passes them, and stays shut under them until the world goes.
            for (int i = 0; i < _others.Count && i < _zkHolds.Count; i++)
            {
                var other = _others[i];
                // When the spreading ring reaches them (it slows as it goes, so the far ones wait longer).
                float since = tickAge - (1 - Mathf.Sqrt(1 - Mathf.Clamp01(other.Reach / 11f))) * .55f;
                float shut = Ease(0, .26f, since), held = since >= 0 ? 1 - Ease(ZkGo, ZkGo + .12f, t) : 0;
                // It closes a little too far and comes back out to rest, and the dial on it turns to a stop as it does.
                float size = Mathf.Lerp(1.6f, .7f, shut) - .09f * Mathf.Sin(shut * Mathf.PI);
                Place(_zkHolds[i], other.Home + Vector3.up * .04f, Vector3.one * size, Quaternion.identity, held * (.45f + .55f * (1 - shut)) * storm);
                Place(_zkDials[i], other.Home + Vector3.up * .05f, Vector3.one * size * .93f, Quaternion.Euler(0, (1 - shut) * (1 - shut) * 140f, 0),
                    held * (.55f + .45f * (1 - shut)) * storm);
            }
        }

        private void SampleZackStrike(float t, float storm, float leave, float light)
        {
            // The landing: a column of light down the bolt's path, light on the ground under him, and the ring out to the
            // ultimate's own reach, with a ragged edge running ahead of it and a thin one after.
            float hit = t - ZkHit, ringU = Mathf.Clamp01(hit / .3f), ringOut = 1 - (1 - ringU) * (1 - ringU);
            float struck = hit >= 0 ? 1 - Ease(ZkHit + .04f, ZkHit + .42f, t) : 0;
            PlaceGlow(_zkColumn, new Vector3(0, 6.2f, .05f), new Vector3(2.4f + 1.6f * struck, 14f, 1f), Quaternion.identity, struck * .8f * storm * light);
            PlaceGlow(_snapGlow, new Vector3(0, .03f, 0), Vector3.one * (.8f + Ease(ZkHit, ZkHit + .35f, t) * 2.2f), Quaternion.Euler(90, 0, 0),
                hit >= 0 ? (1 - Ease(ZkHit + .2f, ZkHit + .9f, t)) * storm : 0);
            float ring = hit >= 0 ? (1 - Ease(ZkHit + .3f, ZkHit + .8f, t)) * storm : 0;
            Place(_zkRing, Vector3.up * .06f, Vector3.one * Mathf.Lerp(.3f, ZkReach, ringOut), Quaternion.identity, ring);
            Place(_zkRingTeeth, Vector3.up * .09f, Vector3.one * Mathf.Lerp(.36f, ZkReach * 1.06f, ringOut), Quaternion.Euler(0, hit * 90f, 0), ring * .85f);
            float echoU = Mathf.Clamp01((hit - .09f) / .36f);
            Place(_zkRing2, Vector3.up * .05f, Vector3.one * Mathf.Lerp(.2f, ZkReach * .82f, 1 - (1 - echoU) * (1 - echoU)), Quaternion.identity,
                hit >= .09f ? (1 - Ease(ZkHit + .3f, ZkHit + .75f, t)) * .7f * storm : 0);

            // What it leaves: the court broken under him, the breaks still gold, and gold on the ground round his feet.
            // These stay for as long as he does: it is the mark of what he is now.
            float charged = Ease(ZkHit, ZkHit + .12f, t);
            float burst = Mathf.Clamp01(hit / .12f), pulse = .5f + .5f * Mathf.Sin(t * 9f);
            Place(_zkScorch, Vector3.up * .015f, new Vector3(1.25f, 1f, 1.25f) * (.4f + .6f * burst), Quaternion.Euler(0, 25, 0), hit >= 0 ? leave : 0);
            Place(_zkCracks, Vector3.up * .035f, new Vector3(1.7f, 1f, 1.7f) * (.3f + .7f * burst), Quaternion.identity, hit >= 0 ? (.7f + .3f * pulse) * leave : 0);
            PlaceGlow(_zkMark, new Vector3(0, .04f, 0), Vector3.one * (2.6f + .25f * pulse), Quaternion.Euler(90, 0, 0), charged * (.5f + .2f * pulse) * leave * light);
            PlaceGlow(_zkAura, new Vector3(0, 1.0f, -.25f), Vector3.one * (2.5f + .2f * pulse), Quaternion.identity, charged * (.22f + .08f * pulse) * leave * light);

            // Sparks thrown off him, each popping somewhere new round his body and shrinking out.
            for (int i = 0; i < _zkMotes.Count; i++)
            {
                float beat = t * 6.5f + i * .27f, step = Mathf.Floor(beat), u = beat - step;
                float a = ZkChance(step * 3.1f + i * 17f) * Mathf.PI * 2f;
                var at = new Vector3(Mathf.Sin(a) * .62f, .35f + 1.5f * ZkChance(step * 5.7f + i * 9f), Mathf.Cos(a) * .62f) + Vector3.up * u * .2f;
                Place(_zkMotes[i], at, Vector3.one * (.14f * (1 - u) * (1 - u) + .02f), Quaternion.Euler(step * 77f, step * 131f, u * 90f), charged * (1 - u) * leave);
            }

            // Whoever really stands inside it jumps as the ring passes them, with a spark over their head.
            for (int i = 0; i < _others.Count && i < _zkJolts.Count; i++)
            {
                var other = _others[i];
                float reached = hit - other.Reach / ZkReach * .3f;
                float jolt = other.Reach <= ZkReach && reached >= 0 ? Mathf.Exp(-reached * 5f) * Mathf.Clamp01(reached / .03f) : 0;
                other.Holder.transform.localPosition = other.Home + new Vector3(Mathf.Sin(t * 91f + i), 0, Mathf.Cos(t * 77f + i * 2)) * (.07f * jolt)
                    + Vector3.up * (.18f * jolt);
                Vector3 over = other.Home + Vector3.up * 2.35f;
                Place(_zkJolts[i], over, Vector3.one * (.16f + .22f * jolt), Quaternion.Euler(t * 300, t * 500 + i * 60, 0), jolt * leave);
                PlaceGlow(_zkJoltGlows[i], over, Vector3.one * (.5f + .7f * jolt), Quaternion.identity, jolt * leave * light);
            }
        }

        /// <summary>
        /// The small arcs, one pool for three moments that never overlap: up off his finger while he calls, between the
        /// hung tip and him while it waits, and off his whole body once it has landed. Each is a typed shape redrawn in a
        /// new place a few times a second; none of them is the world's, so none of them stops.
        /// </summary>
        private void SampleZackArcs(float t, float leave)
        {
            for (int i = 0; i < _zkArcs.Count; i++) Tint(_zkArcs[i], 0);

            if (t < ZkSlow)
            {
                // The crackle up off his finger before the bolt comes.
                float up = Ease(.62f, .72f, t) * (1 - Ease(1.45f, 1.7f, t));
                float step = Mathf.Floor(t * 14);
                for (int i = 0; i < 3; i++)
                {
                    Vector3 from = FreePalm + new Vector3((i - 1) * .04f, .1f, 0);
                    Vector3 to = from + new Vector3((ZkChance(step * 2.3f + i) - .5f) * .5f, (.55f + .5f * ZkChance(step * 4.1f + i * 7f)) * up, (ZkChance(step * 6.7f + i * 3f) - .5f) * .3f);
                    ZackArc(i, from, to, .55f, step * 97f + i * 120f, up * leave);
                }
                return;
            }

            if (t < ZkHit)
            {
                // While it waits: arcs feel their way from the tip toward his head and his fist and do not quite arrive.
                // Poked, the one to his fist lands.
                float waiting = Ease(ZkStopped, ZkStopped + .2f, t) * (1 - Ease(ZkGo - .05f, ZkGo, t));
                if (waiting <= 0) return;
                float since = t - ZkPoke, touched = since > 0 ? Mathf.Exp(-since * 5f) : 0;
                Vector3 tip = ZackTip(t);
                Vector3 head = HeadPoint - Vector3.up * .12f, fist = FreePalm + Vector3.up * .1f;
                for (int i = 0; i < 3; i++)
                {
                    float beat = t * 2.6f + i * .37f, step = Mathf.Floor(beat), u = beat - step;
                    // Each one is there for the first part of its beat and gone for the rest: a slow, uneven feeling about.
                    float on = u < .55f ? Mathf.Sin(u / .55f * Mathf.PI) : 0;
                    Vector3 toward = i == 0 ? head : i == 1 ? fist : tip + new Vector3(ZkChance(step * 3.3f) - .5f, -.2f - .5f * ZkChance(step * 7.7f), ZkChance(step * 5.1f) - .2f) * .9f;
                    float far = i == 1 ? Mathf.Lerp(.5f + .2f * ZkChance(step * 2.9f), 1f, Mathf.Clamp01(touched * 1.6f)) : .45f + .3f * ZkChance(step * 1.7f + i);
                    if (i == 1 && touched > .05f) on = Mathf.Max(on, touched);
                    ZackArc(3 + i, tip, Vector3.Lerp(tip, toward, far), .5f, step * 113f + i * 70f, waiting * on * leave);
                }
                return;
            }

            // Overclocked: arcs jump from his body to the court round his feet, and two more run across him.
            float charged = Ease(ZkHit, ZkHit + .12f, t);
            for (int i = 0; i < _zkArcs.Count; i++)
            {
                float beat = t * 11f + i * .31f, step = Mathf.Floor(beat), u = beat - step;
                if (ZkChance(step * 1.3f + i * 4.9f) > .72f) continue;
                float a = ZkChance(step * 7.1f + i * 3.3f) * Mathf.PI * 2f;
                var from = new Vector3(Mathf.Sin(a) * .3f, .45f + 1.15f * ZkChance(step * 2.7f + i * 8.1f), Mathf.Cos(a) * .3f);
                Vector3 to;
                if (i < 6)
                {
                    float b = a + (ZkChance(step * 9.3f + i) - .5f) * 1.4f, r = .7f + .75f * ZkChance(step * 4.3f + i * 2.2f);
                    to = new Vector3(Mathf.Sin(b) * r, .03f, Mathf.Cos(b) * r);
                }
                else
                {
                    float b = a + 1.7f;
                    to = new Vector3(Mathf.Sin(b) * .36f, .5f + 1.2f * ZkChance(step * 6.1f + i), Mathf.Cos(b) * .36f);
                }
                ZackArc(i, from, to, .6f, step * 151f + i * 40f, charged * (1 - .6f * u) * leave);
            }
        }

        /// <summary>The colour goes out of the world while it is stopped, and comes back with the bolt.</summary>
        private void ZackGrade(float t, out float brightness, out float saturation)
        {
            float stopped = Ease(ZkSlow, ZkStopped, t) * (1 - Ease(ZkGo, ZkHit, t));
            saturation = Mathf.Lerp(1f, .35f, stopped);
            brightness = Mathf.Lerp(1f, .9f, stopped) * (1 + .15f * Flash(t, ZkHit + .03f, .12f));
        }
    }
}
