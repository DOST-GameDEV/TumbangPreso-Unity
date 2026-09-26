using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // PAETE, MAKILING'S EMBRACE: THE v7 BURST LAYER (2026-09-27). The owner on the v6 film: *"its almost perfect"*, then *"a lot
        // more VFX I MEAN"* and *"A LOT MORE VFX AND SHIT LIKE THE GENSHIN REFERENCES"*. v6 took the burst VOCABULARY (the element at
        // the lens, the mark, the streaks, the stroke, the grade); set beside a Genshin burst, what it lacked was DENSITY: in every
        // one of his reference frames the air itself is full (glints everywhere, rings racing over the floor, rays behind the
        // caster, pillars, a curtain of rising light, the frame's edges sunk into the hero's colour). This layer is that density,
        // and every piece still rides a beat that was already there (research section 4: acting before effects):
        //
        //   * GLINTS: four-pointed sparkles (a long cross and a short one) popping and twinkling out, on every beat, round whoever
        //     the beat is on: her as she forms, his eyes as they light, his palms on the slam, the court at each heartbeat, the spot,
        //     the trunk at each haul, the crown and the eyes as it wakes, the bound players as they hit it (Genshin's constant dust).
        //   * RINGS on the court: a shockwave for every blow (slam, heartbeats, send, the court bulging, each haul, the wake, the
        //     yank's thud), with a flat flash under the four biggest (Neuvillette's floor ring, every burst's impact).
        //   * RAYS: a sunburst of her jade behind her as she forms, and of his lime out of the crown as the guardian wakes, turning
        //     slowly (the cut-in glory behind a Genshin caster).
        //   * THE CHANNEL VORTEX: three ribbons of his light winding up round him from the court while he channels, tightening
        //     and quickening at every heartbeat, flung out at the send (Baizhu's ribbons, now his).
        //   * RISING LIGHT: motes rising off the court round him as he channels and round the tree from the moment it breaks
        //     the court to the end (Neuvillette's rising orbs: scale).
        //   * THE PILLAR: one broad column of his light up out of the spot as the roots arrive (Neuvillette's pillars).
        //   * THE CURTAIN: thin streaks rising round the tree while it hauls itself out, and a second wave as they all hit it (the
        //     owner's own frame of rising streaks).
        //   * THE TORNADO: two jade ribbons spiralling up the trunk through the hauls (Kazuha's vortex).
        //   * THE VEIL: the frame's edges sink into the forest's dark while the power is on screen (`SpiritVeil.shader`), deepest
        //     through the channel and the take (Kazuha's ink sky, Kinich's green wall).
        //
        // ⚠️ Every row below is typed, each its own time, place, size and colour (the owner's rule for his world: nothing stamped).
        // ⚠️ Nothing here runs on `Update`; every piece is posed from the scene clock. His lime and her jade only, never white.
        // ⚠️ Reduced effects keeps every shape, halves the light, and drops the veil and the ray flares.
        // =========================================================================================

        // Where a row is anchored: 0 her, 1 his palms, 2 the spot, 3 the crown's light, 4 his body at the court, 5 his head, 6 the tree's eyes.
        private Vector3 PbAnchor(int anchor)
        {
            float court = _paeteCourt;
            switch (anchor)
            {
                case 0: return MakilingStand + Vector3.up * court;
                case 1: return PaeteHandsMid;
                case 2: return _pvLanding + Vector3.up * court;
                case 3:
                    var crown = _paeteTree != null ? _paeteTree.CrownNode : null;
                    return crown != null ? _root.transform.InverseTransformPoint(crown.TransformPoint(new Vector3(0f, .78f, 0f)))
                                         : _pvLanding + Vector3.up * (court + 5.6f);
                case 4: return new Vector3(PaeteHandsMid.x * .5f, court, PaeteHandsMid.z * .45f);
                case 5: return HeadPoint;
                default:
                    var eyes = _paeteTree != null ? _paeteTree.EyesNode : null;
                    return eyes != null ? _root.transform.InverseTransformPoint(eyes.position) : _pvLanding + Vector3.up * (court + 3.1f);
            }
        }

        private Color PbColour(float kind) => kind > 1.5f ? PbHot : kind > .5f ? MakilingJade : PaeteLight;
        // The hottest the light gets: his lime pushed toward his eye light, still green (never white).
        private static readonly Color PbHot = new Color(0.90f, 1.0f, 0.58f, 1f);

        // GLINTS: birth time, life, anchor, offset (x, y, z, scene space), size, colour (0 lime, 1 jade, 2 hot).
        private static readonly float[,] PbGlintRows =
        {
            // CALL: round her as the column spins up, the burst as she forms, the light falling.
            { .06f, .40f, 0, -.55f, 1.10f, .30f, .22f, 1 }, { .10f, .36f, 0, .62f, 1.85f, -.20f, .18f, 1 }, { .14f, .44f, 0, -.30f, 2.55f, .15f, .26f, 0 },
            { .19f, .34f, 0, .80f, .70f, .35f, .16f, 1 }, { .24f, .42f, 0, -.85f, 1.60f, -.35f, .20f, 2 }, { .30f, .38f, 0, .25f, 2.95f, .10f, .24f, 1 },
            { .36f, .40f, 0, .95f, 2.30f, .25f, .19f, 0 }, { .44f, .50f, 0, -.60f, 2.10f, .45f, .30f, 1 }, { .46f, .46f, 0, .70f, 1.30f, .50f, .28f, 2 },
            { .47f, .44f, 0, -.20f, .85f, .60f, .22f, 1 }, { .49f, .52f, 0, .40f, 2.70f, .40f, .26f, 0 }, { .52f, .40f, 0, -1.05f, .95f, .10f, .18f, 1 },
            { .60f, .36f, 0, .15f, 2.35f, .55f, .20f, 2 }, { .66f, .34f, 0, -.45f, 1.75f, .70f, .16f, 0 },
            // His eyes light.
            { .78f, .40f, 5, -.35f, .10f, .25f, .30f, 0 }, { .79f, .36f, 5, .40f, -.15f, .20f, .26f, 2 }, { .81f, .42f, 5, .10f, .45f, .10f, .22f, 0 },
            { .84f, .38f, 5, -.55f, -.40f, .15f, .20f, 1 }, { .88f, .34f, 5, .60f, .30f, -.05f, .18f, 0 },
            // ROOT: the slam, the heartbeats, the send.
            { 1.22f, .36f, 1, -.60f, .15f, .20f, .28f, 0 }, { 1.23f, .32f, 1, .70f, .10f, -.10f, .24f, 2 }, { 1.24f, .40f, 1, .10f, .45f, .40f, .30f, 0 },
            { 1.26f, .34f, 1, -.95f, .30f, -.30f, .20f, 1 }, { 1.28f, .38f, 1, .95f, .55f, .25f, .22f, 0 },
            { 1.62f, .30f, 4, -.50f, 1.10f, .30f, .22f, 0 }, { 1.64f, .28f, 4, .55f, .65f, -.20f, .18f, 1 },
            { 1.80f, .30f, 4, .30f, 1.40f, .35f, .24f, 0 }, { 1.82f, .28f, 4, -.65f, .50f, .10f, .18f, 2 },
            { 1.95f, .32f, 4, -.20f, 1.65f, .20f, .26f, 0 }, { 1.96f, .30f, 4, .70f, 1.00f, -.15f, .22f, 1 }, { 1.98f, .28f, 4, -.80f, .85f, -.30f, .18f, 0 },
            { 2.10f, .34f, 1, .00f, .30f, .60f, .32f, 2 }, { 2.12f, .30f, 1, .45f, .20f, .95f, .24f, 0 },
            // RISE: the court breaking, each haul up the trunk, the top-out.
            { 2.50f, .40f, 2, -.70f, .40f, .30f, .40f, 0 }, { 2.51f, .36f, 2, .80f, .90f, -.40f, .34f, 2 }, { 2.53f, .44f, 2, .20f, 1.60f, .50f, .38f, 0 },
            { 2.55f, .38f, 2, -1.10f, 1.20f, -.60f, .30f, 1 },
            { 2.78f, .36f, 2, .95f, 1.80f, .30f, .34f, 0 }, { 2.80f, .34f, 2, -.85f, 2.40f, .10f, .30f, 1 },
            { 3.05f, .36f, 2, .60f, 3.00f, -.50f, .36f, 0 }, { 3.07f, .34f, 2, -.40f, 3.60f, .45f, .30f, 2 },
            { 3.33f, .38f, 2, .85f, 4.20f, .20f, .38f, 0 }, { 3.35f, .36f, 2, -.75f, 4.70f, -.30f, .32f, 1 },
            { 3.53f, .40f, 3, .50f, .40f, .30f, .40f, 0 }, { 3.55f, .36f, 3, -.60f, .10f, -.20f, .34f, 2 },
            // THE WAKE: its eyes and its crown.
            { 3.71f, .46f, 6, -.55f, .15f, .30f, .44f, 0 }, { 3.72f, .42f, 6, .60f, -.10f, .25f, .40f, 2 }, { 3.74f, .50f, 3, .10f, .90f, .10f, .50f, 0 },
            { 3.76f, .40f, 3, -.90f, .40f, .20f, .36f, 1 }, { 3.78f, .44f, 3, .95f, .20f, -.30f, .38f, 0 },
            // THE TAKE: everyone hits the trunk.
            { 4.50f, .44f, 2, -.90f, 1.20f, .60f, .42f, 0 }, { 4.51f, .40f, 2, 1.00f, .80f, -.40f, .38f, 2 }, { 4.52f, .46f, 2, .30f, 2.20f, .70f, .44f, 0 },
            { 4.54f, .40f, 2, -.50f, 2.90f, -.60f, .36f, 1 }, { 4.56f, .42f, 3, .70f, .30f, .30f, .40f, 0 }, { 4.60f, .40f, 2, 1.30f, 1.60f, .20f, .34f, 1 },
            { 4.66f, .36f, 3, -.65f, .60f, -.20f, .34f, 2 }, { 4.72f, .40f, 2, -1.25f, .60f, .30f, .32f, 0 }, { 4.78f, .34f, 2, .55f, 3.40f, .40f, .30f, 1 },
        };

        // RINGS on the court: time, anchor, radius it reaches, life, colour. Every blow, each its own size.
        private static readonly float[,] PbRingRows =
        {
            { 1.22f, 1, 4.2f, .50f, 0 }, { 1.25f, 1, 2.8f, .42f, 1 },
            { 1.62f, 1, 1.9f, .30f, 0 }, { 1.80f, 1, 2.2f, .30f, 0 }, { 1.95f, 1, 2.6f, .30f, 1 }, { 2.10f, 1, 3.4f, .36f, 0 },
            { 2.50f, 2, 4.6f, .50f, 0 }, { 2.53f, 2, 3.0f, .40f, 1 },
            { 2.78f, 2, 3.4f, .40f, 0 }, { 3.05f, 2, 3.9f, .40f, 0 }, { 3.33f, 2, 4.5f, .45f, 1 },
            { 3.71f, 2, 6.0f, .55f, 0 }, { 3.74f, 2, 4.0f, .45f, 1 },
            { 4.50f, 2, 5.4f, .50f, 0 }, { 4.53f, 2, 3.6f, .42f, 1 },
        };
        // FLASHES under the four biggest blows: time, anchor, size, life.
        private static readonly float[,] PbFlashRows = { { 1.22f, 1, 2.6f, .18f }, { 2.50f, 2, 3.4f, .22f }, { 3.71f, 2, 4.2f, .25f }, { 4.50f, 2, 3.8f, .22f } };

        // RAYS: angle round the centre (0 is up), length, width, how late each grows. Her nine, jade, as she forms.
        private static readonly float[,] PbHerRayRows =
        {
            { 8f, 2.4f, .14f, .00f }, { 47f, 1.7f, .09f, .03f }, { 83f, 2.1f, .12f, .01f }, { 124f, 1.5f, .08f, .04f }, { 160f, 2.3f, .13f, .02f },
            { 205f, 1.6f, .09f, .00f }, { 241f, 2.2f, .12f, .03f }, { 282f, 1.8f, .10f, .01f }, { 322f, 2.0f, .11f, .02f },
        };
        // And the crown's twelve, lime, as the guardian wakes (and again, smaller, as they all hit it).
        private static readonly float[,] PbCrownRayRows =
        {
            { 0f, 3.2f, .16f, .00f }, { 28f, 2.1f, .10f, .02f }, { 61f, 2.8f, .14f, .01f }, { 95f, 1.9f, .09f, .03f }, { 122f, 2.6f, .13f, .00f },
            { 150f, 2.2f, .10f, .02f }, { 182f, 3.0f, .15f, .01f }, { 211f, 2.0f, .09f, .03f }, { 239f, 2.7f, .13f, .00f }, { 268f, 1.8f, .09f, .02f },
            { 301f, 2.9f, .14f, .01f }, { 331f, 2.3f, .11f, .03f },
        };

        // THE CHANNEL VORTEX round him: start angle, radius, turns, top height, band height.
        private static readonly float[,] PbVortexRows = { { 30f, .95f, 1.5f, 1.9f, .09f }, { 150f, 1.15f, 1.3f, 1.6f, .07f }, { 270f, .80f, 1.7f, 2.2f, .06f } };
        // THE TORNADO up the trunk: start angle, radius, turns, top height, band height.
        private static readonly float[,] PbTornadoRows = { { 0f, 1.35f, 2.2f, 6.2f, .12f }, { 180f, 1.6f, 1.8f, 5.4f, .09f } };

        // RISING LIGHT: anchor, angle, distance out, from, until, start height, rise (m/s), size, colour.
        private static readonly float[,] PbOrbRows =
        {
            // Round him while he channels.
            { 4, 20f, .75f, 1.40f, 2.30f, .05f, .9f, .07f, 0 }, { 4, 75f, 1.05f, 1.46f, 2.36f, .10f, 1.1f, .06f, 1 }, { 4, 130f, .85f, 1.52f, 2.40f, .00f, .8f, .08f, 0 },
            { 4, 190f, 1.15f, 1.44f, 2.20f, .15f, 1.2f, .05f, 2 }, { 4, 240f, .70f, 1.58f, 2.42f, .05f, 1.0f, .07f, 0 }, { 4, 290f, .95f, 1.50f, 2.32f, .20f, .9f, .06f, 1 },
            { 4, 335f, 1.20f, 1.62f, 2.45f, .00f, 1.3f, .05f, 0 }, { 4, 50f, 1.35f, 1.66f, 2.40f, .10f, 1.0f, .06f, 2 }, { 4, 160f, 1.30f, 1.70f, 2.44f, .05f, 1.2f, .07f, 0 },
            { 4, 215f, .90f, 1.78f, 2.45f, .15f, .8f, .05f, 1 }, { 4, 265f, 1.40f, 1.82f, 2.40f, .00f, 1.1f, .06f, 0 }, { 4, 310f, .80f, 1.90f, 2.45f, .10f, 1.4f, .08f, 2 },
            { 4, 100f, 1.10f, 1.94f, 2.45f, .05f, 1.0f, .06f, 0 }, { 4, 5f, .95f, 2.00f, 2.45f, .20f, 1.2f, .05f, 1 },
            // Round the tree from the moment it breaks the court to the end.
            { 2, 10f, 1.6f, 2.55f, 3.80f, .10f, 1.4f, .10f, 0 }, { 2, 55f, 2.3f, 2.62f, 3.90f, .05f, 1.8f, .08f, 1 }, { 2, 100f, 1.9f, 2.70f, 4.00f, .20f, 1.2f, .11f, 0 },
            { 2, 150f, 2.8f, 2.78f, 4.10f, .00f, 2.0f, .09f, 2 }, { 2, 200f, 1.7f, 2.86f, 4.20f, .15f, 1.5f, .10f, 0 }, { 2, 245f, 2.5f, 2.95f, 4.30f, .05f, 1.7f, .08f, 1 },
            { 2, 290f, 2.1f, 3.05f, 4.40f, .10f, 1.3f, .12f, 0 }, { 2, 335f, 3.0f, 3.12f, 4.50f, .00f, 2.1f, .08f, 2 }, { 2, 30f, 2.7f, 3.25f, 4.70f, .20f, 1.6f, .09f, 1 },
            { 2, 80f, 1.8f, 3.35f, 4.80f, .10f, 1.9f, .10f, 0 }, { 2, 125f, 2.4f, 3.50f, 5.00f, .05f, 1.4f, .08f, 1 }, { 2, 175f, 3.2f, 3.62f, 5.00f, .00f, 2.2f, .09f, 0 },
            { 2, 220f, 2.0f, 3.75f, 5.00f, .15f, 1.5f, .11f, 2 }, { 2, 265f, 2.9f, 3.90f, 5.00f, .05f, 1.8f, .08f, 0 }, { 2, 310f, 1.6f, 4.05f, 5.00f, .10f, 1.3f, .10f, 1 },
            { 2, 355f, 2.6f, 4.20f, 5.00f, .00f, 2.0f, .09f, 0 }, { 2, 140f, 3.4f, 4.40f, 5.00f, .20f, 1.7f, .08f, 1 }, { 2, 60f, 3.1f, 4.52f, 5.00f, .05f, 2.3f, .10f, 0 },
        };

        // THE CURTAIN: time, angle round the spot, distance out, length, width, colour. The first wave rises while it hauls itself
        // out; the second as everyone hits it.
        private static readonly float[,] PbCurtainRows =
        {
            { 2.56f, 20f, 2.8f, 3.2f, .07f, 0 }, { 2.60f, 95f, 3.4f, 2.6f, .06f, 1 }, { 2.66f, 170f, 2.6f, 3.6f, .08f, 0 }, { 2.72f, 250f, 3.9f, 2.9f, .06f, 0 },
            { 2.80f, 320f, 3.1f, 3.3f, .07f, 1 }, { 2.88f, 55f, 4.2f, 2.4f, .05f, 0 }, { 2.96f, 130f, 3.0f, 3.8f, .08f, 0 }, { 3.04f, 210f, 3.6f, 2.8f, .06f, 1 },
            { 3.12f, 285f, 2.7f, 3.4f, .07f, 0 }, { 3.20f, 350f, 4.0f, 2.7f, .06f, 0 }, { 3.28f, 75f, 3.3f, 3.5f, .07f, 1 }, { 3.36f, 150f, 4.3f, 2.5f, .05f, 0 },
            { 3.44f, 230f, 2.9f, 3.7f, .08f, 0 }, { 3.52f, 300f, 3.7f, 2.9f, .06f, 1 }, { 3.62f, 10f, 3.2f, 4.0f, .08f, 0 }, { 3.70f, 190f, 3.5f, 4.2f, .08f, 0 },
            { 4.50f, 35f, 2.4f, 4.4f, .09f, 0 }, { 4.52f, 110f, 3.0f, 3.8f, .07f, 1 }, { 4.54f, 185f, 2.6f, 4.6f, .09f, 0 }, { 4.56f, 260f, 3.3f, 3.6f, .07f, 0 },
            { 4.58f, 330f, 2.8f, 4.2f, .08f, 1 }, { 4.62f, 70f, 3.6f, 3.4f, .06f, 0 }, { 4.66f, 150f, 3.9f, 3.2f, .06f, 0 }, { 4.70f, 225f, 2.5f, 4.8f, .09f, 1 },
            { 4.76f, 295f, 3.4f, 3.9f, .07f, 0 }, { 4.82f, 5f, 3.0f, 4.3f, .08f, 0 },
        };

        private readonly List<int> _pbGlints = new List<int>(64), _pbRings = new List<int>(16), _pbFlashes = new List<int>(4);
        private readonly List<int> _pbOrbs = new List<int>(32), _pbCurtain = new List<int>(26);
        private readonly List<int> _pbVortex = new List<int>(3), _pbTornado = new List<int>(2);
        private readonly List<Mesh> _pbVortexMeshes = new List<Mesh>(3), _pbTornadoMeshes = new List<Mesh>(2);
        private int _pbHerRays = -1, _pbCrownRays = -1, _pbCrownRays2 = -1, _pbPillar = -1, _pbPillarCore = -1, _pbVeil = -1;
        private Mesh _pbHerRayMesh, _pbCrownRayMesh, _pbCrownRayMesh2;
        private static Mesh _pbStarMesh, _pbRingMesh, _pbVeilMesh;

        private void BuildPaeteBurst()
        {
            for (int i = 0; i < PbGlintRows.GetLength(0); i++)
                _pbGlints.Add(PvTips(AddGlow("PaeteGlint" + i, PbColour(PbGlintRows[i, 7]), PbStar, billboard: true, band: true, falloff: 1.6f, core: .9f, lift: .1f), 1.1f));
            for (int i = 0; i < PbRingRows.GetLength(0); i++)
                _pbRings.Add(AddGlow("PaeteShockRing" + i, PbColour(PbRingRows[i, 4]), PbRing, billboard: false, band: true, falloff: 1.3f, core: .5f));
            for (int i = 0; i < PbFlashRows.GetLength(0); i++)
                _pbFlashes.Add(AddGlow("PaeteGroundFlash" + i, PaeteLight, billboard: false, falloff: 1.5f, core: .4f));
            _pbHerRayMesh = PbDynamic("PaeteHerRays");
            _pbHerRays = PvTips(AddGlow("PaeteHerRays", MakilingJade, _pbHerRayMesh, billboard: true, band: true, falloff: 1.4f, core: .5f, lift: -.45f), 1.2f);
            _pbCrownRayMesh = PbDynamic("PaeteCrownRays");
            _pbCrownRays = PvTips(AddGlow("PaeteCrownRays", PaeteLight, _pbCrownRayMesh, billboard: true, band: true, falloff: 1.4f, core: .6f, lift: -.3f), 1.2f);
            _pbCrownRayMesh2 = PbDynamic("PaeteCrownRays2");
            _pbCrownRays2 = PvTips(AddGlow("PaeteCrownRays2", MakilingJade, _pbCrownRayMesh2, billboard: true, band: true, falloff: 1.4f, core: .5f, lift: -.3f), 1.2f);
            for (int i = 0; i < PbVortexRows.GetLength(0); i++)
            {
                var mesh = PbDynamic("PaeteVortex" + i);
                _pbVortexMeshes.Add(mesh);
                _pbVortex.Add(PvTips(AddGlow("PaeteVortex" + i, i == 1 ? MakilingJade : PaeteLight, mesh, billboard: false, band: true, falloff: 1.4f, core: .5f), 1.2f));
            }
            for (int i = 0; i < PbTornadoRows.GetLength(0); i++)
            {
                var mesh = PbDynamic("PaeteTornado" + i);
                _pbTornadoMeshes.Add(mesh);
                _pbTornado.Add(PvTips(AddGlow("PaeteTornado" + i, MakilingJade, mesh, billboard: false, band: true, falloff: 1.4f, core: .4f), 1.3f));
            }
            for (int i = 0; i < PbOrbRows.GetLength(0); i++)
                _pbOrbs.Add(AddGlow("PaeteRisingLight" + i, PbColour(PbOrbRows[i, 8]), falloff: 2.3f, core: .9f));
            _pbPillar = PvTips(AddGlow("PaetePillar", PaeteLight, PvStreak, billboard: true, band: true, falloff: 1.2f, core: .3f), .8f);
            // Its core in his lime, not the hot near-white (film r20: against the sky the column read yellow-white).
            _pbPillarCore = PvTips(AddGlow("PaetePillarCore", PaeteLight, PvStreak, billboard: true, band: true, falloff: 1.8f, core: .7f), .9f);
            for (int i = 0; i < PbCurtainRows.GetLength(0); i++)
                _pbCurtain.Add(PvTips(AddGlow("PaeteCurtain" + i, PbColour(PbCurtainRows[i, 5]), PvStreak, billboard: true, band: true, falloff: 1.5f, core: .6f), 1.5f));
            // THE VEIL, in its own shader (it darkens; every glow here only adds).
            var veil = Resources.Load<Shader>("Shaders/SpiritVeil");
            if (veil != null)
            {
                var go = VfxShapes.Stand(_root.transform, "PaeteVeil", PbVeilQuad, 1);
                var renderer = go.GetComponent<Renderer>();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
                var m = new Material(veil) { name = "PaeteVeil" };
                m.SetFloat("_Inner", .34f); m.SetFloat("_Outer", 1.02f); m.SetFloat("_Tint", .12f);
                renderer.sharedMaterial = m;
                VfxRenderTag.Own(go, m);
                _pieces.Add(new Piece { Transform = go.transform, Renderer = renderer, Color = new Color(0.02f, 0.075f, 0.03f, 1f) });
                _pbVeil = _pieces.Count - 1;
            }
        }

        private static Mesh PbDynamic(string name)
        {
            var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
            mesh.MarkDynamic();
            return mesh;
        }

        private void SamplePaeteBurst(float t, float leave)
        {
            float court = _paeteCourt;
            var up = Vector3.up;
            float light = _reducedEffects ? .5f : 1f;

            // ---------------------------------------------------------------- GLINTS.
            for (int i = 0; i < _pbGlints.Count; i++)
            {
                float s = t - PbGlintRows[i, 0], life = PbGlintRows[i, 1];
                if (s < 0f || s > life) { PvHide(_pbGlints[i]); continue; }
                var at = PbAnchor((int)PbGlintRows[i, 2]) + new Vector3(PbGlintRows[i, 3], PbGlintRows[i, 4], PbGlintRows[i, 5]) + up * .35f * s;
                float pop = GrowthVfx.Pop(s / .07f);
                float fade = 1f - Ease(life * .45f, life, s);
                float twinkle = .72f + .28f * Mathf.Sin(t * 38f + i * 1.7f);
                PlaceGlow(_pbGlints[i], at, Vector3.one * PbGlintRows[i, 6] * pop * (.55f + .45f * fade), Quaternion.identity, 1.9f * fade * twinkle * light * leave);
            }

            // ---------------------------------------------------------------- RINGS and FLASHES on the court.
            for (int i = 0; i < _pbRings.Count; i++)
            {
                float s = t - PbRingRows[i, 0], life = PbRingRows[i, 3];
                if (s < 0f || s > life) { PvHide(_pbRings[i]); continue; }
                float u = s / life;
                float r = PbRingRows[i, 2] * (1f - Mathf.Pow(1f - u, 3f));
                var at = PbAnchor((int)PbRingRows[i, 1]); at.y = court + .03f + .004f * (i % 5);
                PlaceGlow(_pbRings[i], at, new Vector3(r, r, 1f), Quaternion.Euler(90f, 0f, 0f), 1.8f * Mathf.Pow(1f - u, 1.3f) * light * leave);
            }
            for (int i = 0; i < _pbFlashes.Count; i++)
            {
                float s = t - PbFlashRows[i, 0], life = PbFlashRows[i, 3];
                if (s < 0f || s > life || _reducedEffects) { PvHide(_pbFlashes[i]); continue; }
                float u = s / life, size = PbFlashRows[i, 2] * (.6f + .4f * u);
                var at = PbAnchor((int)PbFlashRows[i, 1]); at.y = court + .05f;
                PlaceGlow(_pbFlashes[i], at, new Vector3(size, size, 1f), Quaternion.Euler(90f, 0f, 0f), 1.3f * (1f - u) * (1f - u) * leave);
            }

            // ---------------------------------------------------------------- RAYS behind her as she forms.
            {
                float s = t - .40f;
                float strength = Ease(0f, .08f, s) * (1f - Ease(.40f, .62f, s));
                if (s < 0f || strength <= .002f) { _pbHerRayMesh.Clear(); PvHide(_pbHerRays); }
                else
                {
                    PbRays(_pbHerRayMesh, PbHerRayRows, s, 18f, .35f, 1f + .25f * s);
                    PlaceGlow(_pbHerRays, MakilingStand + up * (court + 1.95f * MakilingScale), Vector3.one, Quaternion.identity, 1.2f * strength * light * leave);
                }
            }
            // ---------------------------------------------------------------- RAYS out of the crown as it wakes, and again as they hit it.
            {
                var crown = PbAnchor(3);
                float s = t - PaeteWakeAt;
                float strength = Ease(0f, .06f, s) * (1f - Ease(.3f, .7f, s));
                if (s < 0f || strength <= .002f || _paeteTree == null) { _pbCrownRayMesh.Clear(); PvHide(_pbCrownRays); }
                else
                {
                    PbRays(_pbCrownRayMesh, PbCrownRayRows, s, 14f, .45f, 1f + .3f * s);
                    PlaceGlow(_pbCrownRays, crown, Vector3.one, Quaternion.identity, (_reducedEffects ? .5f : 1.5f) * strength * leave);
                }
                float s2 = t - PtArriveAt;
                float strength2 = Ease(0f, .05f, s2) * (1f - Ease(.25f, .5f, s2));
                if (s2 < 0f || strength2 <= .002f || _paeteTree == null) { _pbCrownRayMesh2.Clear(); PvHide(_pbCrownRays2); }
                else
                {
                    PbRays(_pbCrownRayMesh2, PbCrownRayRows, s2, -22f, .4f, .75f + .3f * s2);
                    PlaceGlow(_pbCrownRays2, crown, Vector3.one, Quaternion.identity, (_reducedEffects ? .45f : 1.3f) * strength2 * leave);
                }
            }

            // ---------------------------------------------------------------- THE CHANNEL VORTEX round him.
            var body = PbAnchor(4);
            float beats = 0f, kick = 0f;
            foreach (float p in PaetePulses) { beats += Ease(p, p + .06f, t); kick = Mathf.Max(kick, Decay(t - p, .15f)); }
            for (int k = 0; k < _pbVortex.Count; k++)
            {
                float head = Ease(1.42f + .04f * k, 1.72f + .04f * k, t), tail = Ease(1.55f + .03f * k, PaeteSendAt + .05f, t) * .55f;
                float flung = Mathf.Max(0f, t - PaeteSendAt);
                float strength = 1.3f * (1f - Ease(PaeteSendAt + .08f, PaeteSendAt + .3f, t)) * light * leave;
                if (head - tail < .02f || strength <= .002f) { _pbVortexMeshes[k].Clear(); PvHide(_pbVortex[k]); continue; }
                _pvLine.Clear(); _pvWidths.Clear();
                const int Samples = 36;
                float spin = 260f * t + 90f * beats * beats;
                for (int j = 0; j <= Samples; j++)
                {
                    float u = Mathf.Lerp(tail, head, j / (float)Samples);
                    float a = (PbVortexRows[k, 0] + 360f * PbVortexRows[k, 2] * u + spin) * Mathf.Deg2Rad;
                    float r = PbVortexRows[k, 1] * (1f - .08f * beats) * (.8f + .4f * u) * (1f + 3.0f * flung);
                    _pvLine.Add(body + new Vector3(Mathf.Sin(a) * r, .05f + PbVortexRows[k, 3] * u + 1.1f * flung, Mathf.Cos(a) * r));
                    _pvWidths.Add(PbVortexRows[k, 4] * (.55f + .45f * Mathf.Sin(u * Mathf.PI)));
                }
                PvRibbonMesh(_pbVortexMeshes[k], _pvLine, _pvWidths);
                PlaceGlow(_pbVortex[k], Vector3.zero, Vector3.one, Quaternion.identity, strength * (1f + .5f * kick));
            }

            // ---------------------------------------------------------------- THE TORNADO up the trunk through the hauls.
            var spot = PbAnchor(2);
            for (int k = 0; k < _pbTornado.Count; k++)
            {
                float head = Ease(2.55f + .05f * k, 2.95f + .05f * k, t), tail = Ease(2.95f, 3.62f, t);
                float strength = 1.1f * (1f - Ease(3.5f, 3.7f, t)) * light * leave;
                if (head - tail < .02f || strength <= .002f) { _pbTornadoMeshes[k].Clear(); PvHide(_pbTornado[k]); continue; }
                _pvLine.Clear(); _pvWidths.Clear();
                const int Samples = 44;
                for (int j = 0; j <= Samples; j++)
                {
                    float u = Mathf.Lerp(tail, head, j / (float)Samples);
                    float a = (PbTornadoRows[k, 0] + 360f * PbTornadoRows[k, 2] * u + 320f * t) * Mathf.Deg2Rad;
                    float r = PbTornadoRows[k, 1] * (1f - .35f * u);
                    _pvLine.Add(spot + new Vector3(Mathf.Sin(a) * r, .2f + PbTornadoRows[k, 3] * u, Mathf.Cos(a) * r));
                    _pvWidths.Add(PbTornadoRows[k, 4] * (.5f + .5f * Mathf.Sin(u * Mathf.PI)));
                }
                PvRibbonMesh(_pbTornadoMeshes[k], _pvLine, _pvWidths);
                PlaceGlow(_pbTornado[k], Vector3.zero, Vector3.one, Quaternion.identity, strength);
            }

            // ---------------------------------------------------------------- RISING LIGHT.
            for (int i = 0; i < _pbOrbs.Count; i++)
            {
                float from = PbOrbRows[i, 3], until = PbOrbRows[i, 4];
                if (t < from || t > until) { PvHide(_pbOrbs[i]); continue; }
                float s = t - from;
                float a = (PbOrbRows[i, 1] + 14f * s) * Mathf.Deg2Rad, d = PbOrbRows[i, 2];
                var at = PbAnchor((int)PbOrbRows[i, 0]);
                at.y = court;
                at += new Vector3(Mathf.Sin(a) * d + .05f * Mathf.Sin(t * 5f + i), PbOrbRows[i, 5] + PbOrbRows[i, 6] * s, Mathf.Cos(a) * d);
                float fade = Ease(0f, .12f, s) * (1f - Ease(until - .25f, until, t));
                float twinkle = .7f + .3f * Mathf.Sin(t * 9f + i * 2.3f);
                PlaceGlow(_pbOrbs[i], at, Vector3.one * PbOrbRows[i, 7], Quaternion.identity, 1.2f * fade * twinkle * light * leave);
            }

            // ---------------------------------------------------------------- THE PILLAR at the spot as the roots arrive.
            {
                float s = t - PaeteArriveAt + .02f;
                if (s < 0f || s > .6f) { PvHide(_pbPillar); PvHide(_pbPillarCore); }
                else
                {
                    float height = 10f * Ease(0f, .1f, s);
                    float thin = 1f - Ease(.18f, .6f, s);
                    var at = spot + up * (height * .5f);
                    PlaceGlow(_pbPillar, at, new Vector3(2.1f * (.35f + .65f * thin), Mathf.Max(.02f, height), 1f), Quaternion.identity, .9f * thin * light * leave);
                    PlaceGlow(_pbPillarCore, at, new Vector3(.55f * (.45f + .55f * thin), Mathf.Max(.02f, height * 1.04f), 1f), Quaternion.identity, 1.15f * thin * light * leave);
                }
            }

            // ---------------------------------------------------------------- THE CURTAIN of rising streaks round the tree.
            for (int i = 0; i < _pbCurtain.Count; i++)
            {
                float s = (t - PbCurtainRows[i, 0]) / .55f;
                if (s < 0f || s > 1f) { PvHide(_pbCurtain[i]); continue; }
                float a = PbCurtainRows[i, 1] * Mathf.Deg2Rad, length = PbCurtainRows[i, 3];
                float top = length * Ease(0f, .25f, s) + 2.2f * s;
                float bottom = length * .85f * Ease(.12f, .8f, s) + .8f * s;
                var at = spot + new Vector3(Mathf.Sin(a) * PbCurtainRows[i, 2], (top + bottom) * .5f, Mathf.Cos(a) * PbCurtainRows[i, 2]);
                PlaceGlow(_pbCurtain[i], at, new Vector3(PbCurtainRows[i, 4], Mathf.Max(.02f, top - bottom), 1f), Quaternion.identity,
                          1.5f * (1f - Ease(.55f, 1f, s)) * light * leave);
            }

            // ---------------------------------------------------------------- THE VEIL.
            if (_pbVeil >= 0)
            {
                PaeteGrade(t, out float brightness, out _);
                // The grade's step back (0 to 0.32) sets the veil; each blow throbs it, and the channel and the take sink it deepest.
                float away = Mathf.Clamp01((1f - brightness) / .32f);
                float throb = 0f;
                foreach (float p in PaetePulses) throb = Mathf.Max(throb, Decay(t - p, .2f));
                throb = Mathf.Max(throb, Mathf.Max(Decay(t - PaeteSlamAt, .25f), Decay(t - PtArriveAt, .3f)));
                float deep = Ease(1.35f, 1.6f, t) * (1f - Ease(PaeteSendAt, PaeteArriveAt, t)) + Ease(PaeteWakeAt, 3.9f, t);
                float strength = _reducedEffects ? 0f : (.42f * away + .14f * deep + .12f * throb) * (1f - Ease(PaeteEnd - .14f, PaeteEnd, t));
                Place(_pbVeil, Vector3.zero, Vector3.one, Quaternion.identity, strength);
            }
        }

        /// <summary>A sunburst of rays in XY round the origin, in metres (billboarded by `SpiritGlow`, so it faces any lens): each ray
        /// grows from its own delay, the whole turns by <paramref name="spinPerSecond"/>, and starts <paramref name="gap"/> out.</summary>
        private static void PbRays(Mesh mesh, float[,] rows, float s, float spinPerSecond, float gap, float scale)
        {
            int n = rows.GetLength(0);
            var v = new Vector3[n * 4]; var uv = new Vector2[n * 4]; var tris = new int[n * 6];
            for (int i = 0; i < n; i++)
            {
                float grow = Mathf.Clamp01((s - rows[i, 3]) / .1f);
                grow = 1f - (1f - grow) * (1f - grow);
                float a = (rows[i, 0] + spinPerSecond * s) * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0f);
                var across = new Vector3(dir.y, -dir.x, 0f) * rows[i, 2] * .5f * scale;
                var from = dir * gap * scale;
                var to = dir * (gap + rows[i, 1] * grow) * scale;
                int b = i * 4;
                v[b] = from - across; v[b + 1] = from + across; v[b + 2] = to + across; v[b + 3] = to - across;
                uv[b] = new Vector2(0f, 0f); uv[b + 1] = new Vector2(0f, 1f); uv[b + 2] = new Vector2(1f, 1f); uv[b + 3] = new Vector2(1f, 0f);
                int k = i * 6;
                tris[k] = b; tris[k + 1] = b + 2; tris[k + 2] = b + 1; tris[k + 3] = b; tris[k + 4] = b + 3; tris[k + 5] = b + 2;
            }
            mesh.Clear();
            mesh.vertices = v; mesh.uv = uv; mesh.triangles = tris;
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 12f);
        }

        /// <summary>
        /// A GLINT: a long cross and a short diagonal one, each arm's u along it (so `_Tips` sharpens its points) and v across it
        /// (so `_Band` lights its middle). Unit size; billboarded, so it is always the four-pointed star a lens would see.
        /// </summary>
        private static Mesh PbStar
        {
            get
            {
                if (_pbStarMesh != null) return _pbStarMesh;
                var v = new List<Vector3>(); var uv = new List<Vector2>(); var tris = new List<int>();
                void Arm(float angle, float length, float width)
                {
                    float a = angle * Mathf.Deg2Rad;
                    var dir = new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0f);
                    var across = new Vector3(dir.y, -dir.x, 0f) * width * .5f;
                    var from = -dir * length * .5f; var to = dir * length * .5f;
                    int b = v.Count;
                    v.Add(from - across); v.Add(from + across); v.Add(to + across); v.Add(to - across);
                    uv.Add(new Vector2(0f, 0f)); uv.Add(new Vector2(0f, 1f)); uv.Add(new Vector2(1f, 1f)); uv.Add(new Vector2(1f, 0f));
                    tris.AddRange(new[] { b, b + 2, b + 1, b, b + 3, b + 2 });
                }
                Arm(0f, 1f, .10f);
                Arm(90f, .82f, .09f);
                Arm(45f, .42f, .06f);
                Arm(135f, .38f, .06f);
                _pbStarMesh = new Mesh { name = "PaeteGlintStar", hideFlags = HideFlags.DontSave };
                _pbStarMesh.SetVertices(v); _pbStarMesh.SetUVs(0, uv); _pbStarMesh.SetTriangles(tris, 0);
                _pbStarMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 2f);
                return _pbStarMesh;
            }
        }

        /// <summary>A shockwave ring in XY, unit outer radius, a fifth of it thick (v across it, so the band lights its middle).</summary>
        private static Mesh PbRing
        {
            get
            {
                if (_pbRingMesh != null) return _pbRingMesh;
                const int Sides = 72;
                var v = new Vector3[(Sides + 1) * 2]; var uv = new Vector2[v.Length]; var tris = new int[Sides * 6];
                for (int i = 0; i <= Sides; i++)
                {
                    float a = i * Mathf.PI * 2f / Sides;
                    var d = new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0f);
                    v[i * 2] = d * .8f; v[i * 2 + 1] = d;
                    uv[i * 2] = new Vector2(i / (float)Sides, 0f); uv[i * 2 + 1] = new Vector2(i / (float)Sides, 1f);
                    if (i == Sides) continue;
                    int b = i * 6, k = i * 2;
                    tris[b] = k; tris[b + 1] = k + 1; tris[b + 2] = k + 2; tris[b + 3] = k + 2; tris[b + 4] = k + 1; tris[b + 5] = k + 3;
                }
                _pbRingMesh = new Mesh { name = "PaeteShockRing", hideFlags = HideFlags.DontSave, vertices = v, uv = uv, triangles = tris };
                _pbRingMesh.bounds = new Bounds(Vector3.zero, new Vector3(2.2f, 2.2f, .2f));
                return _pbRingMesh;
            }
        }

        /// <summary>The veil's quad: the screen's corners in clip space (`SpiritVeil.shader` ignores every transform), never culled.</summary>
        private static Mesh PbVeilQuad
        {
            get
            {
                if (_pbVeilMesh != null) return _pbVeilMesh;
                _pbVeilMesh = new Mesh { name = "PaeteVeilQuad", hideFlags = HideFlags.DontSave };
                _pbVeilMesh.vertices = new[] { new Vector3(-1, -1, 0), new Vector3(1, -1, 0), new Vector3(1, 1, 0), new Vector3(-1, 1, 0) };
                _pbVeilMesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
                _pbVeilMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
                _pbVeilMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);
                return _pbVeilMesh;
            }
        }
    }
}
