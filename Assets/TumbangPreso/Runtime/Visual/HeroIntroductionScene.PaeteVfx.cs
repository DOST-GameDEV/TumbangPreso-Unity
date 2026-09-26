using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // PAETE, MAKILING'S EMBRACE: THE v6 EFFECTS PASS (2026-09-27). The owner on the v5 film: *"add more special effects and vfx on
        // his ult cutscene IDK maybe open it with leaves or smth u figure it out u direct it make it better"*, then five frames from
        // Genshin bursts (Ayaka's petals riding her blade, Neuvillette's pillars and rising orbs, Raiden's emblem and brush slashes, a
        // curtain of rising light streaks) with *"theres liek a lot of ways u could go abt the vfx u figure it out"* and *"focus on
        // direction and vfx and sfx"*. The research and the direction are `docs/reports/paete-kit-2026-09-25/direction.md` section
        // 5.15. What it took from them, in one line each:
        //
        //   * OPEN ON THE ELEMENT ALREADY IN THE AIR (every 5-star burst: Nahida's clasped hands with leaves past the lens, Baizhu's palm
        //     in a swirl of leaves): frame 0 is a gust of leaves and sampaguita petals streaming in from behind her; it winds into a
        //     column round her spot, she rises INSIDE it, and it bursts outward as she forms. That is the owner's "open it with leaves".
        //   * PARTICLES RIDE THE MOTION, NEVER HANG (Ayaka's petals along the blade, Kazuha's vortex): the petals spiral round her light
        //     as it falls; leaves orbit him as he channels and ride out with his roots when he sends them; each haul of the tree throws
        //     a spiral of leaves up its trunk.
        //   * ONE EMBLEM, AND IT IS THE LIGHT'S SHAPE (Raiden's tomoe, Neuvillette's floor ring): the MARK OF THE MOUNTAIN, three leaves
        //     in a whorl inside two rings, drawn three times as the light travels: in the air behind him as it goes into him (0.78), on
        //     the court under his palms as he gives it to the ground (1.22), and behind the guardian's head as its eyes open (4.5).
        //   * VERTICAL LIGHT FOR POWER (Neuvillette's pillars, the rising streaks): each heartbeat of the channel shoots thin streaks of
        //     his light up out of the court round him, more each time (2, 3, 4), and the send throws three together; light spears up
        //     out of the court at the spot as the roots arrive.
        //   * A BRUSH STROKE FOR THE STRIKE (Raiden's slashes, Kinich's horizontal flash): at the send a stroke of light is painted
        //     along the court from his hands to the spot, left to right, ahead of the roots, and wiped off from its start.
        //   * DEPTH: leaves and petals drift right past the lens in every shot, so each frame has a near layer, him in the middle and
        //     the light behind.
        //   * THE WORLD TAKES A STEP BACK: the phase camera's grade dims and cools the court while her light, his eyes and the mark
        //     burn (the burst "backdrop drops away" of Kazuha and Zhongli), and gives the daylight back for the payoff.
        //
        // ⚠️ Nothing here runs on `Update`: during the shared phase the world is paused, so every piece is posed from the scene clock.
        // ⚠️ Colours: his lime and her jade only, never white: `AbilityShowcaseProbe`'s 12 per cent blown-to-white rule is the same
        // spirit even where it does not measure (the cutscene is not a gameplay frame), and white is where Genshin's frames lose the
        // subject.
        // =========================================================================================

        // Where the staged guardian comes up: 5.5 m ahead, pushed off the real can by the rule the live cast uses (owner, 2026-09-27:
        // *"dont let it be placed in a place it STANDS on can"*). The RISE shots follow it (`PaeteFrame`).
        private Vector3 _pvLanding = PaeteLanding;

        private static readonly Color PvPetal = new Color(0.95f, 0.93f, 0.84f, 1f);
        private static readonly Color PvLeafLit = new Color(0.66f, 0.80f, 0.30f, 1f);
        // The cutscene's own living greens (her forest in the air), kept bright on purpose: the guardian's crown was muted for PLAY.
        private static readonly Color PvLeaf = new Color(0.435f, 0.647f, 0.196f, 1f), PvLeafDark = new Color(0.247f, 0.455f, 0.141f, 1f);
        // ⚠️ Film r16: at their typed sizes the gust's leaves were six specks at five metres and the opening did not read as leaves.
        // One measured factor on the whole table rather than retyping it: 1.7 for leaves, 1.6 for petals.
        private const float PvGustLeafScale = 1.7f, PvGustPetalScale = 1.6f;
        // THE WIND ROUND HER (Baizhu's ribbons, Kazuha's vortex): three spiralling ribbons of her light that draw the column the leaves
        // ride on. Each: start angle, radius, turns, top height, band height.
        private static readonly float[,] PvWindRows = { { 0f, .72f, 1.6f, 2.9f, .10f }, { 120f, .86f, 1.4f, 2.5f, .08f }, { 240f, .64f, 1.8f, 3.2f, .07f } };

        private struct PvGust
        {
            public Vector3 From; public float Arrive, Angle, Radius, Height, Spin, Out, Up, Size; public int Kind;
            public PvGust(float x, float y, float z, float arrive, float angle, float radius, float height, float spin, float @out, float up, float size, int kind)
            { From = new Vector3(x, y, z); Arrive = arrive; Angle = angle; Radius = radius; Height = height; Spin = spin; Out = @out; Up = up; Size = size; Kind = kind; }
        }

        // THE OPENING GUST (0 to about 1.4 s): where each piece is at frame 0 (his space; +x is her side, screen left in the CALL
        // shot), when it reaches the column round her, where on the column (angle, radius, height), how fast it orbits (degrees a
        // second), how hard it is flung out when she forms, and its size and kind (0 leaf, 1 dark leaf, 2 lit leaf, 3 petal).
        // Five start close to the lens (z over 1.8) so the first frame already has a near layer. Every row typed.
        private static readonly PvGust[] PvGustRows =
        {
            new PvGust(3.4f, 1.6f, -2.2f, .16f, 20f, .62f, .50f, 520f, 3.2f, 1.4f, .20f, 0),
            new PvGust(4.1f, 0.8f, -0.6f, .22f, 75f, .78f, .30f, 480f, 3.6f, 1.1f, .17f, 1),
            new PvGust(2.9f, 2.3f, -3.0f, .14f, 130f, .55f, 1.20f, 560f, 2.8f, 1.8f, .09f, 3),
            new PvGust(3.8f, 1.2f, 1.2f, .24f, 190f, .86f, .70f, 450f, 3.9f, 1.0f, .22f, 0),
            new PvGust(2.6f, 1.1f, 2.9f, .28f, 240f, .70f, .90f, 500f, 3.3f, 1.5f, .26f, 2),
            new PvGust(4.5f, 2.0f, -1.6f, .20f, 300f, .66f, 1.50f, 540f, 3.0f, 1.9f, .08f, 3),
            new PvGust(3.2f, 0.5f, -3.4f, .12f, 345f, .92f, .20f, 430f, 4.1f, 0.9f, .19f, 1),
            new PvGust(2.3f, 1.5f, 3.4f, .30f, 40f, .58f, 1.10f, 510f, 3.5f, 1.3f, .30f, 0),
            new PvGust(3.6f, 2.6f, -0.2f, .19f, 100f, .80f, 1.80f, 470f, 2.9f, 2.1f, .08f, 3),
            new PvGust(4.3f, 0.6f, 0.4f, .26f, 160f, .74f, .40f, 530f, 3.7f, 1.2f, .18f, 1),
            new PvGust(2.8f, 1.9f, -2.6f, .17f, 215f, .60f, 1.40f, 490f, 3.1f, 1.6f, .09f, 3),
            new PvGust(3.9f, 1.4f, -2.9f, .15f, 270f, .88f, .80f, 555f, 3.8f, 1.0f, .21f, 2),
            new PvGust(3.0f, 2.2f, 0.8f, .23f, 320f, .64f, 1.70f, 505f, 3.0f, 2.0f, .07f, 3),
            new PvGust(4.6f, 1.0f, -1.0f, .25f, 10f, .95f, .60f, 440f, 4.2f, 1.1f, .20f, 0),
            new PvGust(2.5f, 0.9f, 2.2f, .27f, 60f, .52f, .50f, 575f, 3.4f, 1.4f, .24f, 1),
            new PvGust(3.5f, 2.8f, -1.8f, .18f, 115f, .70f, 2.00f, 495f, 2.7f, 2.3f, .08f, 3),
            new PvGust(4.0f, 1.7f, 0.9f, .21f, 175f, .83f, 1.30f, 525f, 3.6f, 1.5f, .17f, 2),
            new PvGust(3.1f, 0.7f, -1.4f, .13f, 225f, .57f, .30f, 545f, 3.2f, 1.0f, .19f, 0),
            new PvGust(4.4f, 2.4f, -2.4f, .20f, 285f, .77f, 1.90f, 465f, 3.5f, 2.2f, .08f, 3),
            new PvGust(2.7f, 1.3f, 1.8f, .24f, 335f, .68f, 1.00f, 515f, 3.3f, 1.7f, .22f, 1),
            new PvGust(3.3f, 0.4f, 0.2f, .16f, 50f, .90f, .25f, 535f, 3.9f, 0.8f, .18f, 2),
            new PvGust(3.7f, 1.8f, -3.2f, .19f, 200f, .62f, 1.60f, 485f, 3.0f, 2.0f, .07f, 3),
        };

        // The petals that spiral round her light as it falls into his hand: how far each trails the light, where round it, how far out,
        // its size. They circle her hands while she holds it (from 0.3) and scatter off his hand when it lands.
        private static readonly Vector4[] PvRibbonRows =
        {
            new Vector4(.00f, 0f, .16f, .085f), new Vector4(.02f, 60f, .20f, .075f), new Vector4(.04f, 130f, .14f, .095f),
            new Vector4(.05f, 200f, .22f, .075f), new Vector4(.07f, 260f, .18f, .085f), new Vector4(.09f, 320f, .15f, .070f),
        };

        // The light streaks of the channel and the send: when each fires, where round him (angle, distance), how tall, how wide, and
        // whose light (0 his lime, 1 her jade). Two on the first heartbeat, three on the second, four on the third, three on the send.
        private static readonly float[,] PvStreakRows =
        {
            { 1.72f, 40f, 1.05f, 1.9f, .070f, 0 }, { 1.74f, 215f, 1.25f, 1.6f, .060f, 1 },
            { 1.96f, 120f, 1.15f, 2.1f, .075f, 0 }, { 1.98f, 300f, 0.95f, 1.7f, .060f, 0 }, { 2.00f, 175f, 1.40f, 1.5f, .050f, 1 },
            { 2.14f, 70f, 1.30f, 2.3f, .080f, 0 }, { 2.15f, 250f, 1.10f, 1.9f, .065f, 1 }, { 2.17f, 10f, 1.45f, 1.6f, .055f, 0 },
            { 2.18f, 150f, 0.90f, 2.0f, .070f, 0 },
            { 2.30f, 90f, 0.80f, 2.6f, .090f, 0 }, { 2.31f, 270f, 0.85f, 2.4f, .080f, 0 }, { 2.33f, 0f, 1.00f, 2.2f, .070f, 1 },
        };

        // The leaves the slam blows flat out along the court from his palms: direction, speed, spin, size, kind.
        private static readonly float[,] PvShockRows =
        {
            { 15f, 3.4f, 720f, .16f, 0 }, { 52f, 2.8f, -640f, .14f, 1 }, { 88f, 3.9f, 810f, .18f, 2 }, { 130f, 3.1f, -700f, .15f, 0 },
            { 168f, 2.6f, 590f, .13f, 1 }, { 205f, 3.6f, -760f, .17f, 0 }, { 240f, 2.9f, 680f, .15f, 2 }, { 282f, 3.3f, -720f, .16f, 1 },
            { 318f, 2.7f, 610f, .14f, 0 }, { 350f, 3.8f, -790f, .17f, 2 },
        };

        // The leaves and petals that orbit him while he channels (start angle, distance, height, degrees a second, size, kind), and then
        // ride out with his roots to the spot.
        private static readonly float[,] PvOrbitRows =
        {
            { 0f, .95f, .45f, 160f, .15f, 0 }, { 36f, 1.10f, .80f, 150f, .080f, 3 }, { 72f, .85f, 1.15f, 175f, .14f, 1 },
            { 108f, 1.05f, .35f, 140f, .16f, 2 }, { 144f, .90f, .95f, 185f, .075f, 3 }, { 180f, 1.15f, .60f, 155f, .15f, 0 },
            { 216f, .80f, 1.30f, 170f, .13f, 1 }, { 252f, 1.00f, .50f, 145f, .085f, 3 }, { 288f, 1.20f, 1.05f, 180f, .16f, 2 },
            { 324f, .88f, .70f, 165f, .12f, 0 },
        };

        // The brush stroke at the send, three bristle lines: height over the court, the stroke's own height, sideways bow, how much of
        // the way it reaches, and how late it starts.
        // ⚠️ Film r16: the first stroke (0.36 high) was a thread at eight metres; a brush stroke is fat. 0.62 now, the bristles 0.14 and 0.11.
        private static readonly float[,] PvSlashRows = { { .60f, .62f, .25f, 1.00f, .00f }, { .98f, .14f, .35f, .82f, .03f }, { .30f, .11f, -.20f, .66f, .05f } };

        // The light that spears up out of the court at the spot as the roots arrive: offset, height, width, delay.
        private static readonly float[,] PvShaftRows = { { .20f, -.10f, 3.0f, .22f, .00f }, { -.35f, .25f, 2.4f, .16f, .04f }, { .10f, .40f, 2.0f, .12f, .08f } };

        // The leaves each haul throws spiralling up the trunk: haul, start angle, distance, degrees a second, rise speed, size, kind.
        private static readonly float[,] PvSpiralRows =
        {
            { 0, 20f, 1.20f, 260f, 2.6f, .20f, 0 }, { 0, 80f, 1.40f, 240f, 2.2f, .085f, 3 }, { 0, 150f, 1.10f, 280f, 2.9f, .22f, 1 },
            { 0, 205f, 1.50f, 230f, 2.4f, .18f, 2 }, { 0, 265f, 1.25f, 270f, 2.7f, .080f, 3 }, { 0, 320f, 1.35f, 250f, 2.3f, .21f, 0 },
            { 1, 45f, 1.30f, 250f, 2.8f, .21f, 1 }, { 1, 110f, 1.15f, 290f, 3.1f, .085f, 3 }, { 1, 170f, 1.45f, 240f, 2.5f, .23f, 0 },
            { 1, 230f, 1.20f, 275f, 2.9f, .19f, 2 }, { 1, 290f, 1.50f, 235f, 2.6f, .080f, 3 }, { 1, 345f, 1.30f, 265f, 3.0f, .22f, 1 },
            { 2, 10f, 1.40f, 270f, 3.2f, .23f, 2 }, { 2, 70f, 1.25f, 245f, 2.8f, .090f, 3 }, { 2, 135f, 1.55f, 285f, 3.4f, .24f, 0 },
            { 2, 195f, 1.30f, 255f, 3.0f, .21f, 1 }, { 2, 255f, 1.60f, 240f, 2.7f, .085f, 3 }, { 2, 315f, 1.35f, 280f, 3.3f, .20f, 0 },
        };

        // Near-lens drifters, one small set per shot: from, to (times), screen from (x, y), screen to, how far along the lens line
        // (a share of eye to focus, so a push-in never puts one behind the lens), size, kind.
        private static readonly float[,] PvLensRows =
        {
            { 0.00f, 0.50f, -1.25f, 0.55f, 0.95f, 0.10f, .30f, .10f, 0 },
            { 0.06f, 0.62f, -1.20f, -0.35f, 0.70f, -0.75f, .38f, .050f, 3 },
            { 0.30f, 1.08f, 0.30f, 1.15f, 0.62f, -1.15f, .34f, .040f, 3 },
            { 1.25f, 2.28f, 0.85f, -1.10f, 1.20f, 0.80f, .35f, .10f, 1 },
            { 1.60f, 2.28f, -0.70f, -1.10f, -0.90f, 1.10f, .40f, .035f, 3 },
            { 2.30f, 2.80f, -1.20f, -0.20f, 1.20f, 0.10f, .28f, .12f, 0 },
            { 4.25f, 5.00f, -0.20f, 1.15f, 0.25f, -1.10f, .35f, .045f, 3 },
            { 4.40f, 5.00f, 0.95f, 1.15f, 0.55f, -1.15f, .42f, .12f, 2 },
        };

        private readonly List<int> _pvGust = new List<int>(24), _pvRibbon = new List<int>(6), _pvStreaks = new List<int>(12);
        private readonly List<int> _pvShock = new List<int>(10), _pvOrbit = new List<int>(10), _pvShafts = new List<int>(3);
        private readonly List<int> _pvSpiral = new List<int>(18), _pvLens = new List<int>(8), _pvSlash = new List<int>(3);
        private readonly List<Mesh> _pvSlashMeshes = new List<Mesh>(3), _pvWindMeshes = new List<Mesh>(3);
        private readonly List<int> _pvWind = new List<int>(3), _pvSlashInk = new List<int>(3);
        private readonly List<Vector3> _pvLine = new List<Vector3>(32);
        private readonly List<float> _pvWidths = new List<float>(32);
        private int _pvMarkAir, _pvMarkAirRing, _pvMarkCourt, _pvMarkLanding, _pvMarkTree;
        private readonly int[] _pvTreeEye = new int[2], _pvTreeStreak = new int[2];

        private Color PvKind(int kind) => kind == 1 ? PvLeafDark : kind == 2 ? PvLeafLit : kind == 3 ? PvPetal : PvLeaf;
        private Mesh PvMesh(int kind) => kind == 3 ? PvPetalMesh : PvLeafMesh;
        private static Mesh _pvLeafMesh, _pvPetalMesh, _pvMarkMesh, _pvStreakQuad, _pvRingXY;
        private static Mesh PvLeafMesh => _pvLeafMesh != null ? _pvLeafMesh : _pvLeafMesh = PaeteInk.Leaf(1f, 0.55f, 0.06f);
        private static Mesh PvPetalMesh => _pvPetalMesh != null ? _pvPetalMesh : _pvPetalMesh = PaeteInk.Leaf(1f, 0.70f, 0.03f);

        /// <summary>The v6 pieces. Called at the end of `BuildPaete`, after the landing has been pushed off the can.</summary>
        private void BuildPaeteVfx()
        {
            foreach (var row in PvGustRows) _pvGust.Add(AddSolid("PaeteGust", PvMesh(row.Kind), PvKind(row.Kind)));
            for (int i = 0; i < PvWindRows.GetLength(0); i++)
            {
                var mesh = new Mesh { name = "PaeteWind", hideFlags = HideFlags.DontSave };
                mesh.MarkDynamic();
                _pvWindMeshes.Add(mesh);
                _pvWind.Add(PvTips(AddGlow("PaeteWind" + i, MakilingJade, mesh, billboard: false, band: true, falloff: 1.4f, core: .4f), 1.2f));
            }
            foreach (var row in PvRibbonRows) _pvRibbon.Add(AddSolid("PaeteRibbonPetal", PvPetalMesh, PvPetal));
            _pvMarkAir = AddGlow("PaeteMarkAir", PaeteLight, PvMark, billboard: false, band: true, falloff: 1.3f, core: .5f);
            _pvMarkAirRing = AddGlow("PaeteMarkAirRing", MakilingJade, PvRing, billboard: false, band: true, falloff: 1.6f, core: .3f);
            _pvMarkCourt = AddGlow("PaeteMarkCourt", PaeteLight, PvMark, billboard: false, band: true, falloff: 1.3f, core: .5f);
            _pvMarkLanding = AddGlow("PaeteMarkLanding", PaeteLight, PvMark, billboard: false, band: true, falloff: 1.3f, core: .4f);
            _pvMarkTree = AddGlow("PaeteMarkTree", PaeteLight, PvMark, billboard: false, band: true, falloff: 1.3f, core: .5f);
            for (int i = 0; i < PvStreakRows.GetLength(0); i++)
                _pvStreaks.Add(PvTips(AddGlow("PaeteStreak", PvStreakRows[i, 5] > .5f ? MakilingJade : PaeteLight, PvStreak, billboard: true, band: true, falloff: 1.5f, core: .6f), 1.6f));
            for (int i = 0; i < PvShockRows.GetLength(0); i++) _pvShock.Add(AddSolid("PaeteShockLeaf", PvLeafMesh, PvKind((int)PvShockRows[i, 4])));
            for (int i = 0; i < PvOrbitRows.GetLength(0); i++) { int k = (int)PvOrbitRows[i, 5]; _pvOrbit.Add(AddSolid("PaeteOrbitLeaf", PvMesh(k), PvKind(k))); }
            for (int i = 0; i < PvSlashRows.GetLength(0); i++)
            {
                var mesh = new Mesh { name = "PaeteSlash", hideFlags = HideFlags.DontSave };
                mesh.MarkDynamic();
                _pvSlashMeshes.Add(mesh);
                // ⚠️ THE STROKE HAS A BODY OF ITS OWN COLOUR UNDER ITS LIGHT (film r16: additive lime alone on the sunlit beige court
                // clipped toward white and barely showed). A translucent lime stroke (lit, alpha-blended, so it keeps its colour on a bright
                // ground) with the additive light down its middle. Film r17 tried a dark ink body and it came out a white slab with black
                // dashes: the ribbon then shared its vertices between its two sides, so its normals cancelled to zero and the lit shader
                // broke on them. `PvRibbonMesh` now gives each side its own vertices.
                _pvSlashInk.Add(Add("PaeteSlashBody" + i, mesh, new Color(0.62f, 0.86f, 0.28f, i == 0 ? .62f : .48f), .75f, plain: true));
                _pvSlash.Add(PvTips(AddGlow("PaeteSlash" + i, PaeteLight, mesh, billboard: false, band: true, falloff: i == 0 ? 1.6f : 2.0f, core: i == 0 ? 1.0f : .4f), .7f));
            }
            for (int i = 0; i < PvShaftRows.GetLength(0); i++) _pvShafts.Add(PvTips(AddGlow("PaeteShaft", PaeteLight, PvStreak, billboard: true, band: true, falloff: 1.4f, core: .7f), 1.3f));
            for (int i = 0; i < PvSpiralRows.GetLength(0); i++) { int k = (int)PvSpiralRows[i, 6]; _pvSpiral.Add(AddSolid("PaeteSpiralLeaf", PvMesh(k), PvKind(k))); }
            for (int i = 0; i < PvLensRows.GetLength(0); i++) { int k = (int)PvLensRows[i, 8]; _pvLens.Add(AddSolid("PaeteLensLeaf", PvMesh(k), PvKind(k))); }
            for (int e = 0; e < 2; e++)
            {
                _pvTreeEye[e] = AddGlow("PaeteTreeEye" + e, new Color(0.80f, 1.0f, 0.55f, 1f), falloff: 2.0f, core: 1.3f, lift: .15f);
                _pvTreeStreak[e] = AddGlow("PaeteTreeEyeStreak" + e, PaeteLight, falloff: 1.3f, core: .5f, lift: .2f);
            }
        }

        /// <summary>`SpiritGlow`'s `_Tips`: fades a band glow out toward both ends of its length (u), so a streak has no hard end.</summary>
        private int PvTips(int piece, float tips)
        {
            var m = _pieces[piece].Renderer.sharedMaterial;
            if (m != null && m.HasProperty("_Tips")) m.SetFloat("_Tips", tips);
            return piece;
        }

        /// <summary>The staged guardian's spot, pushed off the real can (the live cast's rule), and its ground branches turned away from it.</summary>
        private void PaeteLandingOffTheCan()
        {
            var lata = GameServices.Round?.Lata;
            if (lata == null) return;
            var world = _root.transform.TransformPoint(PaeteLanding);
            var from = _root.transform.position;
            var pushed = Abilities.PaeteVine.ClearOfCan(world, lata.transform.position, from);
            var local = _root.transform.InverseTransformPoint(pushed); local.y = 0f;
            // The RISE is framed for a tree about 5.5 m ahead; never follow a push further than the clearance itself.
            var shift = Vector3.ClampMagnitude(local - PaeteLanding, Core.PaeteRules.SentryCanClearance);
            _pvLanding = PaeteLanding + shift;
        }

        /// <summary>The RISE shots follow a pushed landing: the focus by all of it, the lens by half, so he stays in the frame too.</summary>
        private void PaeteFrame(int index, ref Vector3 eye, ref Vector3 look)
        {
            if (_performance == null || index < 0 || _performance.Shots[index].Start < PaeteSendAt - .01f) return;
            var shift = _pvLanding - PaeteLanding;
            look += shift; eye += shift * .5f;
        }

        /// <summary>
        /// The phase camera's grade for this moment (`UltimatePhaseView` applies it through `ColourGrade.SetEventGrade`): the court dims
        /// and cools as the gust comes in, so her light, his eyes and the mark carry the frame; it lifts as the roots leave, and the
        /// payoff (the eyes) gets the daylight back.
        /// </summary>
        private void PaeteGrade(float t, out float brightness, out float saturation)
        {
            // ⚠️ Film r16 measured the first version (0.76) as a 12 per cent darker sky: too little to read as the world stepping back.
            // It is deeper now and HOLDS through the roots and the hauls (the power is still on screen), giving the daylight back only
            // for the payoff at the eyes.
            float away = Ease(0f, .3f, t) * (1f - .3f * Ease(2.3f, 2.8f, t)) * (1f - Ease(4.4f, 4.9f, t));
            brightness = 1f - .32f * away;
            saturation = 1f - .22f * away;
        }

        /// <summary>The current authored shot's lens in the scene's space (no shake; the probe shots and mirrors are the view's business).</summary>
        private bool PvLens(float t, int shot, out Vector3 eye, out Vector3 look, out float fov)
        {
            eye = look = Vector3.zero; fov = 50f;
            if (_performance == null || shot < 0) return false;
            _performance.Shot(shot, t, out eye, out look, out fov);
            PaeteFrame(shot, ref eye, ref look);
            return true;
        }

        private void PvHide(int piece) => Place(piece, Vector3.zero, Vector3.one * .001f, Quaternion.identity, 0f);

        private void SamplePaeteVfx(float t, float leave)
        {
            float court = _paeteCourt;
            var up = Vector3.up;
            var her = MakilingStand + up * court;

            // ---------------------------------------------------------------- THE OPENING GUST, the column round her, the burst as she forms.
            for (int i = 0; i < _pvGust.Count; i++)
            {
                var row = PvGustRows[i];
                float burst = .44f + .02f * (i % 3);
                float life = row.Kind == 3 ? 1.65f : 1.30f + .08f * (i % 4);
                if (t > life) { PvHide(_pvGust[i]); continue; }
                Vector3 Column(float at)
                {
                    float s = Mathf.Max(0f, at - row.Arrive);
                    float a = (row.Angle + row.Spin * s) * Mathf.Deg2Rad;
                    float r = row.Radius * (1f - .25f * Ease(row.Arrive, burst, at));
                    return her + new Vector3(Mathf.Sin(a) * r, row.Height + 1.6f * s, Mathf.Cos(a) * r);
                }
                Vector3 p;
                if (t < row.Arrive)
                {
                    // Streaming in on a curve: a sideways swing that dies as it reaches the column.
                    float u = Ease(0f, row.Arrive, t);
                    var target = Column(row.Arrive);
                    p = Vector3.Lerp(row.From, target, u) + new Vector3(0f, .35f * Mathf.Sin(u * Mathf.PI), -.4f * Mathf.Sin(u * Mathf.PI));
                }
                else if (t < burst) p = Column(t);
                else
                {
                    float s = t - burst;
                    float a = (row.Angle + row.Spin * (burst - row.Arrive)) * Mathf.Deg2Rad;
                    var outward = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                    var across = new Vector3(Mathf.Cos(a), 0f, -Mathf.Sin(a));
                    float fall = row.Kind == 3 ? .55f : .95f;
                    p = Column(burst) + outward * row.Out * (1f - Mathf.Exp(-2.6f * s)) / 2.6f + across * 1.2f * (1f - Mathf.Exp(-2f * s)) / 2f
                        + up * (row.Up * s - fall * s * s);
                }
                float shrink = 1f - Ease(life - .25f, life, t);
                var spin = Quaternion.Euler(t * (200f + 13f * i), row.Angle + t * (170f + 11f * i), t * (90f + 7f * i));
                Place(_pvGust[i], p, Vector3.one * row.Size * (row.Kind == 3 ? PvGustPetalScale : PvGustLeafScale) * shrink, spin, leave);
            }

            // ---------------------------------------------------------------- THE WIND ROUND HER: the column drawn in her light, then flung.
            for (int k = 0; k < _pvWind.Count; k++)
            {
                float head = Ease(.02f + .03f * k, .34f + .03f * k, t), tail = Ease(.22f + .03f * k, .50f, t);
                float burst = Mathf.Max(0f, t - .46f);
                float strength = (_reducedEffects ? .45f : 1.25f) * (1f - Ease(.5f, .72f, t)) * leave;
                if (head - tail < .02f || strength <= .002f) { _pvWindMeshes[k].Clear(); PvHide(_pvWind[k]); continue; }
                _pvLine.Clear(); _pvWidths.Clear();
                const int Samples = 40;
                for (int j = 0; j <= Samples; j++)
                {
                    float u = Mathf.Lerp(tail, head, j / (float)Samples);
                    float a = (PvWindRows[k, 0] + 360f * PvWindRows[k, 2] * u + 420f * t) * Mathf.Deg2Rad;
                    float r = PvWindRows[k, 1] * (0.85f + 0.3f * u) * (1f + 3.2f * burst);
                    _pvLine.Add(her + new Vector3(Mathf.Sin(a) * r, .15f + PvWindRows[k, 3] * u + 1.2f * burst, Mathf.Cos(a) * r));
                    _pvWidths.Add(PvWindRows[k, 4] * (0.6f + 0.4f * Mathf.Sin(u * Mathf.PI)));
                }
                PvRibbonMesh(_pvWindMeshes[k], _pvLine, _pvWidths);
                PlaceGlow(_pvWind[k], Vector3.zero, Vector3.one, Quaternion.identity, strength);
            }

            // ---------------------------------------------------------------- THE PETAL RIBBON round her light.
            var hands = _makilingSpirit != null ? _root.transform.InverseTransformPoint(_makilingSpirit.HandsWorld) : her + new Vector3(0f, 2.2f, .4f);
            var palm = FreePalm + new Vector3(0f, .07f, .04f);
            var along = (palm - hands).normalized;
            var n1 = Vector3.Cross(along, up).normalized; if (n1.sqrMagnitude < .1f) n1 = Vector3.right;
            var n2 = Vector3.Cross(n1, along);
            for (int i = 0; i < _pvRibbon.Count; i++)
            {
                var row = PvRibbonRows[i];
                float show = Ease(.30f, .45f, t);
                if (show <= .001f || t > 1.3f) { PvHide(_pvRibbon[i]); continue; }
                float local = t - row.x;
                Vector3 p;
                if (local < .58f)
                {
                    // Circling her hands while she holds the light.
                    float a = (row.y + t * 300f) * Mathf.Deg2Rad;
                    p = hands + (n1 * Mathf.Cos(a) + n2 * Mathf.Sin(a)) * (row.z + .08f) + up * .05f * Mathf.Sin(t * 9f + i);
                }
                else if (local < .70f)
                {
                    // Spiralling round the light as it falls.
                    float f = Ease(.58f, .70f, local);
                    var centre = Vector3.Lerp(hands, palm, f) + up * .35f * Mathf.Sin(f * Mathf.PI);
                    float a = (row.y + t * 1500f) * Mathf.Deg2Rad;
                    p = centre + (n1 * Mathf.Cos(a) + n2 * Mathf.Sin(a)) * row.z * (1f - .4f * f);
                }
                else
                {
                    // Off his hand as it lands: flung round and out, floating down.
                    float s = local - .70f;
                    float a = (row.y + (.70f + row.x) * 1500f) * Mathf.Deg2Rad;
                    var outward = (n1 * Mathf.Cos(a) + n2 * Mathf.Sin(a)); outward.y = Mathf.Abs(outward.y);
                    p = palm + outward * 1.4f * (1f - Mathf.Exp(-3f * s)) / 3f + up * (.9f * s - .7f * s * s);
                }
                float shrink = 1f - Ease(1.05f, 1.3f, t);
                Place(_pvRibbon[i], p, Vector3.one * row.w * show * shrink, Quaternion.Euler(t * 400f + 50f * i, t * 260f, 30f * i), leave);
            }

            // ---------------------------------------------------------------- THE MARK, 1: in the air behind his head as the light goes in.
            {
                float s = t - .78f;
                int shot = ShotIndexAt(t);
                if (s < 0f || s > .40f || !PvLens(t, shot, out var eye, out _, out _)) { PvHide(_pvMarkAir); PvHide(_pvMarkAirRing); }
                else
                {
                    // ⚠️ Higher and wider than first cut (film r16: centred at his neck, his body hid the three leaves and only the rings
                    // showed): its heart sits behind the top of his head and the leaves reach out past his shoulders.
                    var at = HeadPoint + new Vector3(0f, .10f, -.60f);
                    var toLens = (eye - at).normalized;
                    float pop = GrowthVfx.Pop(s / .12f);
                    float radius = 1.65f * pop + .35f * s;
                    float strength = (_reducedEffects ? .6f : 1.7f) * (1f - Ease(.10f, .40f, s)) * leave;
                    var face = Quaternion.LookRotation(toLens, up) * Quaternion.Euler(0f, 0f, 25f * pop + 60f * s);
                    PlaceGlow(_pvMarkAir, at, new Vector3(radius, radius, 1f), face, strength);
                    float ring = .6f + 2.2f * Ease(0f, .25f, s);
                    PlaceGlow(_pvMarkAirRing, at, new Vector3(ring, ring, 1f), face, _reducedEffects ? 0f : .8f * (1f - Ease(.04f, .22f, s)) * leave);
                }
            }

            // ---------------------------------------------------------------- THE MARK, 2: stamped on the court under his palms.
            {
                float s = t - PaeteSlamAt;
                if (s < 0f || t > 2.75f) PvHide(_pvMarkCourt);
                else
                {
                    float beat = 0f;
                    foreach (float p in PaetePulses) beat = Mathf.Max(beat, Decay(t - p, .22f));
                    float send = Decay(t - PaeteSendAt, .2f);
                    float radius = 1.15f * GrowthVfx.Pop(s / .14f) + .14f * beat + .2f * send;
                    float drain = 1f - Ease(PaeteSendAt + .05f, PaeteSendAt + .45f, t);
                    float strength = (.9f + .9f * beat + 1.2f * send + .8f * Decay(s, .18f)) * drain * leave * (_reducedEffects ? .6f : 1f);
                    float spin = 20f * s + 90f * Mathf.Max(0f, t - 1.6f) + 160f * Mathf.Max(0f, t - PaeteSendAt);
                    var at = PaeteHandsMid; at.y = court + .025f;
                    PlaceGlow(_pvMarkCourt, at, new Vector3(radius, radius, 1f), Quaternion.Euler(0f, spin, 0f) * Quaternion.Euler(90f, 0f, 0f), strength);
                }
            }

            // ---------------------------------------------------------------- THE LIGHT STREAKS of the channel and the send.
            var body = new Vector3(PaeteHandsMid.x * .5f, 0f, PaeteHandsMid.z * .45f);
            for (int i = 0; i < _pvStreaks.Count; i++)
            {
                float s = (t - PvStreakRows[i, 0]) / .42f;
                if (s < 0f || s > 1f) { PvHide(_pvStreaks[i]); continue; }
                float a = PvStreakRows[i, 1] * Mathf.Deg2Rad, length = PvStreakRows[i, 3];
                float top = court + length * Ease(0f, .25f, s) + 1.6f * s;
                float bottom = court + length * .85f * Ease(.12f, .8f, s) + .6f * s;
                var at = body + new Vector3(Mathf.Sin(a) * PvStreakRows[i, 2], (top + bottom) * .5f, Mathf.Cos(a) * PvStreakRows[i, 2]);
                PlaceGlow(_pvStreaks[i], at, new Vector3(PvStreakRows[i, 4], Mathf.Max(.02f, top - bottom), 1f), Quaternion.identity,
                          (_reducedEffects ? .5f : 1.5f) * (1f - Ease(.55f, 1f, s)) * leave);
            }

            // ---------------------------------------------------------------- THE SLAM blows leaves flat out along the court.
            for (int i = 0; i < _pvShock.Count; i++)
            {
                float s = t - PaeteSlamAt;
                if (s < 0f || s > .95f) { PvHide(_pvShock[i]); continue; }
                float a = PvShockRows[i, 0] * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                float dist = .2f + PvShockRows[i, 1] * (1f - Mathf.Exp(-3f * s)) / 3f;
                float y = court + .04f + .28f * Mathf.Sin(Mathf.Min(s * 5f, Mathf.PI)) * Mathf.Exp(-2f * s);
                var p = PaeteHandsMid + dir * dist; p.y = y;
                Place(_pvShock[i], p, Vector3.one * PvShockRows[i, 3] * (1f - Ease(.7f, .95f, s)),
                      Quaternion.Euler(8f * Mathf.Sin(s * 20f + i), PvShockRows[i, 0] + PvShockRows[i, 2] * s, 12f), leave);
            }

            // ---------------------------------------------------------------- THE ORBIT round him while he channels, then out with the roots.
            for (int i = 0; i < _pvOrbit.Count; i++)
            {
                float t0 = 1.45f, send = PaeteSendAt + .02f * i;
                if (t < t0 || t > PaeteArriveAt + .55f) { PvHide(_pvOrbit[i]); continue; }
                Vector3 Orbit(float at)
                {
                    float s = at - t0;
                    // Faster with every heartbeat (the three pulses), tighter as the channel builds.
                    float quick = 0f; foreach (float pl in PaetePulses) quick += Ease(pl, pl + .1f, at);
                    float angle = (PvOrbitRows[i, 0] + PvOrbitRows[i, 3] * s * (1f + .45f * quick)) * Mathf.Deg2Rad;
                    float r = PvOrbitRows[i, 1] * (1f - .22f * Ease(1.7f, 2.3f, at));
                    float y = PvOrbitRows[i, 2] * Ease(t0, t0 + .3f, at) + .06f * Mathf.Sin(at * 6f + i);
                    return body + new Vector3(Mathf.Sin(angle) * r, court + y, Mathf.Cos(angle) * r);
                }
                Vector3 p;
                if (t < send) p = Orbit(t);
                else
                {
                    // Out with the roots: an arc to the spot on their 0.45 s, then a spiral up it.
                    var from = Orbit(send);
                    float ride = Ease(send, PaeteArriveAt, t);
                    float a = (PvOrbitRows[i, 0] + 90f) * Mathf.Deg2Rad;
                    var goal = _pvLanding + new Vector3(Mathf.Sin(a) * .6f, court + .3f, Mathf.Cos(a) * .6f);
                    p = Vector3.Lerp(from, goal, ride) + up * .8f * Mathf.Sin(ride * Mathf.PI);
                    float after = t - PaeteArriveAt;
                    if (after > 0f)
                    {
                        float aa = a + after * 6f;
                        p = _pvLanding + new Vector3(Mathf.Sin(aa) * (.6f + .5f * after), court + .3f + 2.4f * after, Mathf.Cos(aa) * (.6f + .5f * after));
                    }
                }
                float shrink = Ease(t0, t0 + .2f, t) * (1f - Ease(PaeteArriveAt + .3f, PaeteArriveAt + .55f, t));
                Place(_pvOrbit[i], p, Vector3.one * PvOrbitRows[i, 4] * shrink, Quaternion.Euler(t * 300f + 30f * i, t * 220f + PvOrbitRows[i, 0], 20f * i), leave);
            }

            // ---------------------------------------------------------------- THE BRUSH STROKE painted along the court at the send.
            var start = new Vector3(PaeteHandsMid.x, 0f, Mathf.Max(.5f, PaeteHandsMid.z) + .3f);
            var end = new Vector3(_pvLanding.x, 0f, _pvLanding.z - .7f);
            for (int k = 0; k < _pvSlash.Count; k++)
            {
                float delay = PvSlashRows[k, 4];
                float head = Ease(PaeteSendAt + delay, PaeteSendAt + .20f + delay, t) * PvSlashRows[k, 3];
                float tail = Ease(2.68f + delay, 3.05f, t) * PvSlashRows[k, 3];
                float fade = (1f - Ease(2.9f, 3.08f, t)) * leave;
                float strength = (_reducedEffects ? .5f : 1.9f - .6f * k) * fade;
                if (head - tail < .02f || strength <= .002f) { _pvSlashMeshes[k].Clear(); PvHide(_pvSlash[k]); PvHide(_pvSlashInk[k]); continue; }
                _pvLine.Clear(); _pvWidths.Clear();
                const int Samples = 24;
                for (int j = 0; j <= Samples; j++)
                {
                    float u = Mathf.Lerp(tail, head, j / (float)Samples);
                    var p = Vector3.Lerp(start, end, u);
                    p.x += PvSlashRows[k, 2] * Mathf.Sin(u * Mathf.PI);
                    p.y = court + PvSlashRows[k, 0] + .15f * u;
                    _pvLine.Add(p);
                    // A brush: pressed hard at the start, thinning out toward the flick at the end.
                    _pvWidths.Add(PvSlashRows[k, 1] * (.55f + .45f * Mathf.Max(0f, Mathf.Sin(Mathf.Pow(Mathf.Clamp01(u), .6f) * Mathf.PI))) * (1f - .55f * u));
                }
                PvRibbonMesh(_pvSlashMeshes[k], _pvLine, _pvWidths);
                Place(_pvSlashInk[k], Vector3.zero, Vector3.one, Quaternion.identity, fade);
                PlaceGlow(_pvSlash[k], Vector3.zero, Vector3.one, Quaternion.identity, strength);
            }

            // ---------------------------------------------------------------- THE MARK, 3: under the court at the spot as the roots arrive.
            {
                float s = t - PaeteArriveAt;
                if (s < -.05f || s > .9f) PvHide(_pvMarkLanding);
                else
                {
                    float radius = 1.5f * GrowthVfx.Pop((s + .05f) / .16f);
                    float strength = (1.3f * Decay(s, .25f) + .6f) * (1f - Ease(.35f, .9f, s)) * leave * (_reducedEffects ? .6f : 1f);
                    var at = _pvLanding; at.y = court + .09f;
                    PlaceGlow(_pvMarkLanding, at, new Vector3(radius, radius, 1f), Quaternion.Euler(0f, -40f * s, 0f) * Quaternion.Euler(90f, 0f, 0f), strength);
                }
            }

            // ---------------------------------------------------------------- LIGHT SPEARS UP out of the court at the spot.
            for (int i = 0; i < _pvShafts.Count; i++)
            {
                float s = (t - PaeteArriveAt + .03f - PvShaftRows[i, 4]) / .5f;
                if (s < 0f || s > 1f) { PvHide(_pvShafts[i]); continue; }
                float length = PvShaftRows[i, 2];
                float top = court + length * Ease(0f, .3f, s);
                float bottom = court + length * .6f * Ease(.4f, 1f, s);
                var at = _pvLanding + new Vector3(PvShaftRows[i, 0], (top + bottom) * .5f, PvShaftRows[i, 1]);
                PlaceGlow(_pvShafts[i], at, new Vector3(PvShaftRows[i, 3], Mathf.Max(.02f, top - bottom), 1f), Quaternion.identity,
                          (_reducedEffects ? .5f : 1.6f) * (1f - Ease(.5f, 1f, s)) * leave);
            }

            // ---------------------------------------------------------------- EACH HAUL throws a spiral of leaves up the trunk.
            for (int i = 0; i < _pvSpiral.Count; i++)
            {
                float s = t - PaeteHauls[(int)PvSpiralRows[i, 0]];
                if (s < 0f || s > 1.1f) { PvHide(_pvSpiral[i]); continue; }
                float a = (PvSpiralRows[i, 1] + PvSpiralRows[i, 3] * s) * Mathf.Deg2Rad;
                float r = PvSpiralRows[i, 2] + .9f * s;
                float y = court + .3f + PvSpiralRows[i, 4] * s - .9f * s * s;
                var p = _pvLanding + new Vector3(Mathf.Sin(a) * r, y, Mathf.Cos(a) * r);
                Place(_pvSpiral[i], p, Vector3.one * PvSpiralRows[i, 5] * (1f - Ease(.8f, 1.1f, s)),
                      Quaternion.Euler(s * 520f + 40f * i, s * 300f + PvSpiralRows[i, 1], 25f * i), leave);
            }

            // ---------------------------------------------------------------- THE MARK, 4, and the guardian's eyes: the last beat.
            {
                var eyes = _paeteTree != null ? _paeteTree.EyesNode : null;
                float s = t - 4.50f;
                if (eyes == null || s < -.02f)
                {
                    PvHide(_pvMarkTree);
                    for (int e = 0; e < 2; e++) { PvHide(_pvTreeEye[e]); PvHide(_pvTreeStreak[e]); }
                }
                else
                {
                    // The mark stamped wide on the court at its foot as it wakes: the ground has answered. (First cut hung it behind its
                    // head, where the trunk hid the leaves and the rings read as a target on its face; film r16.)
                    float pop = GrowthVfx.Pop(Mathf.Max(0f, s) / .14f);
                    float radius = 2.1f * pop + .3f * Mathf.Max(0f, s);
                    float strength = (_reducedEffects ? .5f : 1.5f) * (.45f + .55f * Decay(s, .35f)) * leave;
                    var foot = _pvLanding; foot.y = court + .1f;
                    PlaceGlow(_pvMarkTree, foot, new Vector3(radius, radius, 1f), Quaternion.Euler(0f, 25f * pop + 30f * s, 0f) * Quaternion.Euler(90f, 0f, 0f), strength);
                    for (int e = 0; e < 2; e++)
                    {
                        var at = _root.transform.InverseTransformPoint(eyes.TransformPoint(new Vector3(e == 0 ? -.19f : .19f, 0f, .04f)));
                        float flash = Flash(t, 4.52f, .12f);
                        PlaceGlow(_pvTreeEye[e], at, new Vector3(.24f, .09f, 1f) * (1f + .6f * flash), Quaternion.identity, (1.1f + flash) * pop * leave);
                        PlaceGlow(_pvTreeStreak[e], at, new Vector3(1.8f, .05f, 1f) * (.5f + .5f * flash + .2f * pop), Quaternion.identity,
                                  _reducedEffects ? 0f : (.4f + 1.2f * flash) * pop * leave);
                    }
                }
            }

            // ---------------------------------------------------------------- NEAR THE LENS: the layer in front of everything.
            for (int i = 0; i < _pvLens.Count; i++)
            {
                float t0 = PvLensRows[i, 0], t1 = PvLensRows[i, 1];
                int shot = ShotIndexAt(Mathf.Max(t0, .001f));
                if (t < t0 || t > t1 || ShotIndexAt(t) != shot || !PvLens(t, shot, out var eye, out var look, out var fov)) { PvHide(_pvLens[i]); continue; }
                float u = Ease(t0, t1, t) * .7f + .3f * Mathf.InverseLerp(t0, t1, t);
                float sx = Mathf.Lerp(PvLensRows[i, 2], PvLensRows[i, 4], u), sy = Mathf.Lerp(PvLensRows[i, 3], PvLensRows[i, 5], u);
                var fwd = (look - eye).normalized;
                var right = Vector3.Cross(up, fwd).normalized;
                var lensUp = Vector3.Cross(fwd, right);
                float d = PvLensRows[i, 6] * Vector3.Distance(eye, look);
                float halfH = d * Mathf.Tan(fov * .5f * Mathf.Deg2Rad), halfW = halfH * 16f / 9f;
                var p = eye + fwd * d + right * sx * halfW + lensUp * (sy * halfH + .05f * Mathf.Sin(t * 7f + i));
                Place(_pvLens[i], p, Vector3.one * PvLensRows[i, 7], Quaternion.Euler(t * 260f + 40f * i, t * 180f, t * 120f + 15f * i), leave);
            }
        }

        // ------------------------------------------------------------------ meshes

        /// <summary>
        /// ⚠️ THE MARK OF THE MOUNTAIN (v6): three leaves in a whorl inside two rings, in the XY plane, unit radius, uv v running ACROSS
        /// every stroke so `SpiritGlow`'s band lights each line down its middle and lets its edges go. The three leaves are typed, each
        /// its own start, sweep, reach and width (the owner's rule for his world), and each tapers to a point at both ends in the
        /// geometry, so the shape reads at any size without a texture. It is his emblem, not a borrowed one: a leaf for each of the
        /// three things the light passes through (her, him, the ground).
        /// </summary>
        private static Mesh PvMark
        {
            get
            {
                if (_pvMarkMesh != null) return _pvMarkMesh;
                var v = new List<Vector3>(); var uv = new List<Vector2>(); var tris = new List<int>();
                void Ring(float inner, float outer, int sides)
                {
                    int b = v.Count;
                    for (int i = 0; i <= sides; i++)
                    {
                        float a = i * Mathf.PI * 2f / sides;
                        var d = new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0f);
                        v.Add(d * inner); v.Add(d * outer);
                        uv.Add(new Vector2(i / (float)sides, 0f)); uv.Add(new Vector2(i / (float)sides, 1f));
                        if (i == sides) continue;
                        int k = b + i * 2;
                        tris.AddRange(new[] { k, k + 1, k + 2, k + 2, k + 1, k + 3 });
                    }
                }
                void Blade(float startDeg, float sweepDeg, float r0, float r1, float width)
                {
                    const int N = 18;
                    int b = v.Count;
                    for (int i = 0; i <= N; i++)
                    {
                        float s = i / (float)N;
                        float a = (startDeg + sweepDeg * s * s) * Mathf.Deg2Rad;
                        float r = Mathf.Lerp(r0, r1, s);
                        var spine = new Vector3(Mathf.Sin(a) * r, Mathf.Cos(a) * r, 0f);
                        float a2 = (startDeg + sweepDeg * (s + .01f) * (s + .01f)) * Mathf.Deg2Rad;
                        var next = new Vector3(Mathf.Sin(a2) * Mathf.Lerp(r0, r1, s + .01f), Mathf.Cos(a2) * Mathf.Lerp(r0, r1, s + .01f), 0f);
                        var tangent = (next - spine).normalized;
                        var normal = new Vector3(-tangent.y, tangent.x, 0f);
                        // ⚠️ Max(0): Sin(PI) is a hair below zero in floats, and Pow of a negative is NaN (film r15 stalled on it).
                        float w = width * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(s * Mathf.PI)), .8f) * .5f;
                        v.Add(spine + normal * w); v.Add(spine - normal * w);
                        uv.Add(new Vector2(s, 0f)); uv.Add(new Vector2(s, 1f));
                        if (i == N) continue;
                        int k = b + i * 2;
                        tris.AddRange(new[] { k, k + 1, k + 2, k + 2, k + 1, k + 3 });
                    }
                }
                Ring(.88f, 1.0f, 72);
                Ring(.60f, .655f, 56);
                Blade(12f, 74f, .10f, .84f, .30f);
                Blade(131f, 68f, .12f, .81f, .27f);
                Blade(249f, 79f, .09f, .86f, .32f);
                _pvMarkMesh = new Mesh { name = "PaeteMark", hideFlags = HideFlags.DontSave };
                _pvMarkMesh.SetVertices(v); _pvMarkMesh.SetUVs(0, uv); _pvMarkMesh.SetTriangles(tris, 0);
                _pvMarkMesh.RecalculateNormals();
                _pvMarkMesh.bounds = new Bounds(Vector3.zero, new Vector3(2.2f, 2.2f, .2f));
                return _pvMarkMesh;
            }
        }

        /// <summary>A thin ring in XY (the shockwave off the mark in the air).</summary>
        private static Mesh PvRing
        {
            get
            {
                if (_pvRingXY != null) return _pvRingXY;
                const int Sides = 64;
                var v = new Vector3[(Sides + 1) * 2]; var uv = new Vector2[v.Length]; var tris = new int[Sides * 6];
                for (int i = 0; i <= Sides; i++)
                {
                    float a = i * Mathf.PI * 2f / Sides;
                    var d = new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0f);
                    v[i * 2] = d * .93f; v[i * 2 + 1] = d;
                    uv[i * 2] = new Vector2(i / (float)Sides, 0f); uv[i * 2 + 1] = new Vector2(i / (float)Sides, 1f);
                    if (i == Sides) continue;
                    int b = i * 6, k = i * 2;
                    tris[b] = k; tris[b + 1] = k + 1; tris[b + 2] = k + 2; tris[b + 3] = k + 2; tris[b + 4] = k + 1; tris[b + 5] = k + 3;
                }
                _pvRingXY = new Mesh { name = "PaeteRingXY", hideFlags = HideFlags.DontSave, vertices = v, uv = uv, triangles = tris };
                _pvRingXY.RecalculateNormals();
                _pvRingXY.bounds = new Bounds(Vector3.zero, new Vector3(2.2f, 2.2f, .2f));
                return _pvRingXY;
            }
        }

        /// <summary>A unit quad whose u runs up it and v across it: a vertical streak for `SpiritGlow`'s band plus `_Tips`.</summary>
        private static Mesh PvStreak
        {
            get
            {
                if (_pvStreakQuad != null) return _pvStreakQuad;
                _pvStreakQuad = new Mesh { name = "PaeteStreakQuad", hideFlags = HideFlags.DontSave };
                _pvStreakQuad.vertices = new[] { new Vector3(-.5f, -.5f, 0), new Vector3(.5f, -.5f, 0), new Vector3(.5f, .5f, 0), new Vector3(-.5f, .5f, 0) };
                _pvStreakQuad.uv = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
                _pvStreakQuad.triangles = new[] { 0, 2, 1, 0, 3, 2 };
                _pvStreakQuad.RecalculateNormals();
                _pvStreakQuad.bounds = new Bounds(Vector3.zero, Vector3.one * 2f);
                return _pvStreakQuad;
            }
        }

        /// <summary>
        /// A STANDING ribbon along <paramref name="line"/> (the brush stroke, the wind round her), each point its own height across; u along,
        /// v up it. ⚠️ TWO SIDES, EACH WITH ITS OWN VERTICES (film r17): with one set shared by both windings `RecalculateNormals` averaged
        /// opposite normals to zero and the lit translucent body rendered as a white slab with black dashes. The same fix as
        /// `VfxShapes.TwoSided`, for the same reason.
        /// </summary>
        private static void PvRibbonMesh(Mesh mesh, List<Vector3> line, List<float> widths)
        {
            int n = line.Count;
            if (n < 2) { mesh.Clear(); return; }
            int back = n * 2;
            var vertices = new Vector3[n * 4]; var uv = new Vector2[n * 4]; var triangles = new int[(n - 1) * 12];
            for (int i = 0; i < n; i++)
            {
                var low = line[i] - Vector3.up * widths[i] * .5f; var high = line[i] + Vector3.up * widths[i] * .5f;
                float u = i / (float)(n - 1);
                vertices[i * 2] = low; vertices[i * 2 + 1] = high; vertices[back + i * 2] = low; vertices[back + i * 2 + 1] = high;
                uv[i * 2] = uv[back + i * 2] = new Vector2(u, 0f); uv[i * 2 + 1] = uv[back + i * 2 + 1] = new Vector2(u, 1f);
                if (i == n - 1) continue;
                int b = i * 12, a = i * 2, c = back + i * 2;
                triangles[b] = a; triangles[b + 1] = a + 2; triangles[b + 2] = a + 1;
                triangles[b + 3] = a + 1; triangles[b + 4] = a + 2; triangles[b + 5] = a + 3;
                triangles[b + 6] = c; triangles[b + 7] = c + 1; triangles[b + 8] = c + 2;
                triangles[b + 9] = c + 1; triangles[b + 10] = c + 3; triangles[b + 11] = c + 2;
            }
            mesh.Clear();
            mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
        }
    }
}
