using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // PAETE, MAKILING'S EMBRACE: v9, THE DIVE (2026-10-08). The owner on the remodelled cutscene: *"this is essentially just the
        // same cutscene textured better ... it could use some more creativity on it"*, and, choosing this shot from three ideas:
        // *"The camera goes underground. When he slams the court, the camera drops through it and rides his root through the dark
        // soil, then bursts back up with the tree's first claw."* Until today the roots' journey was a wide hold of a ridge crossing
        // the court; now the lens IS the light he sends: down his arms, through the court ahead of his palms, along his root, and
        // up through the cracks beside the guardian's first claws.
        //
        //   PLUNGE  1.93 to 2.025  from high behind his right shoulder (him whole, his hands on the court, for one frame) the lens
        //                          swoops past his arm to the court ahead of his palms.
        //   UNDER   2.025 to 2.10  it is through. Three root stubs hang under his hands, lit, the walls banded in beds of soil.
        //   RIDE    2.10 to 2.47   he SENDS them: the heads race off down the tunnel and the lens chases the middle one, under a
        //                          dead root, past stones, a buried slipper, a rusted can and two worms that flinch from it;
        //                          rootlets shoot out of the root as its head passes, pulses of light overtake the lens, grit falls.
        //   BREACH  2.40 to 2.60   the head climbs under the spot and hits the court (2.50, `PaeteArriveAt`); the ceiling cracks with
        //                          light; the lens swings out to the right of the spot and tips up at the cracks.
        //   BURST   2.603          it goes through, on a lime flash of two frames, and ends above the court (2.70) low beside
        //                          the spot as the claws breach, on the same side as the wide crane it cuts to.
        //
        // ⚠️ THE SET IS A CLOSED SHELL TURNED INWARD. Under a court the map's ground is seen from behind and is culled, so without
        // a set the lens would look out at the sky and the whole world from below. `PuShell` is one mesh from behind his hands to
        // past the spot, its faces wound to look INWARD, its ceiling 6 cm under the court's top so nothing of it shows from above;
        // it is wide and symmetric under the spot because `UltimatePhaseView.ChooseShot` may mirror the shot about its focus.
        // ⚠️ NO LIGHT REACHES IT, so nothing here is lit by the scene: `PaeteSoil.shader` lights the walls and everything in them
        // from his root alone (a line of light along its lit length, its head, the cracks). The first thought, `AddSolid`'s lit
        // Standard, is black wherever a map shadows the ground and sunlit wherever it does not.
        // ⚠️ Every piece is posed from the scene clock `t` (the world is paused under the cutscene); every row below is typed, each
        // its own numbers. Lengths along the tunnel are typed as (a, b): a + b times the distance from his hands to the spot, so
        // the set follows a spot pushed off the can (`_pvLanding`) and never assumes +z.
        // ⚠️ His lime and her jade only, never white; the soil is browns. Reduced effects keeps every shape, halves the light and
        // drops the flash.
        // =========================================================================================

        private const float PuFrom = 1.93f, PuUpAt = 2.603f, PuTo = 2.70f;
        // The tunnel's ceiling under the court's top, and the shortest run the set is laid out for (a spot pushed back toward him
        // closer than this moves the set's start back behind his hands instead of folding the tunnel on itself).
        private const float PuCeil = -0.06f, PuMinLen = 3.4f;

        // ------------------------------------------------------------------ typed tables

        // THE CAMERA. Time, the eye (x to his right, y over the court's top, along a, along b), the focus the same way, how much
        // of the focus is the racing head instead (0 the typed point, 1 the head), the lens. ⚠️ The four points of the ride
        // (2.20 to 2.47) are typed as shares (0.16, 0.48, 0.75, 0.89) of the way from where the lens levels out (0.92 along) to
        // where it turns for the spot (0.9 short of it), so they stay in order for any run from 3.4 m up; typed as plain
        // fractions of the run they crossed each other under 3.8 m and the lens backed up (`check.py`, 2026-10-08).
        private static readonly float[,] PuCamRows =
        {
            { 1.930f,  2.00f,  1.90f, -2.60f, 0f,     0.00f,  0.20f, 0.30f, 0f,   0.00f, 54f },
            { 1.980f,  0.85f,  0.62f, -0.55f, 0f,     0.04f, -0.30f, 0.95f, 0f,   0.00f, 60f },
            { 2.025f,  0.32f,  0.00f,  0.42f, 0f,     0.00f, -0.95f, 1.20f, 0f,   0.00f, 66f },
            { 2.070f,  0.28f, -0.46f,  0.70f, 0f,    -0.02f, -1.05f, 1.32f, 0f,   0.15f, 72f },
            { 2.130f,  0.36f, -0.66f,  0.92f, 0f,    -0.08f, -1.02f, 1.90f, 0f,   0.70f, 78f },
            { 2.200f,  0.42f, -0.63f,  0.629f, 0.16f, 0.00f, -1.00f, 1.20f, 0.25f, 1.00f, 80f },
            { 2.300f,  0.38f, -0.66f,  0.046f, 0.48f, 0.00f, -1.00f, 1.00f, 0.50f, 1.00f, 80f },
            { 2.400f,  0.60f, -0.68f, -0.445f, 0.75f, 0.00f, -0.80f, 0.00f, 1f,   1.00f, 78f },
            { 2.470f,  1.05f, -0.70f, -0.700f, 0.89f, 0.00f, -0.30f, 0.00f, 1f,   0.80f, 76f },
            { 2.530f,  1.62f, -0.68f, -0.90f, 1f,     0.00f,  0.15f, 0.00f, 1f,   0.25f, 74f },
            { 2.570f,  1.96f, -0.48f, -0.70f, 1f,     0.00f,  0.50f, 0.00f, 1f,   0.00f, 72f },
            { 2.603f,  2.20f,  0.00f, -0.50f, 1f,     0.00f,  0.60f, 0.00f, 1f,   0.00f, 70f },
            { 2.650f,  2.48f,  0.52f, -0.42f, 1f,     0.00f,  0.52f, 0.00f, 1f,   0.00f, 64f },
            { 2.700f,  2.78f,  0.84f, -0.34f, 1f,     0.00f,  0.50f, 0.00f, 1f,   0.00f, 60f },
        };

        // THE TUNNEL, ring by ring: along (a, b), its middle's x, its half width, its floor. A burrow from behind his hands that
        // opens into a hollow under the spot, where the guardian waits; no two rings alike.
        private static readonly float[,] PuRingRows =
        {
            { -1.55f, 0f,     0.00f, 0.42f, -1.05f },
            { -1.05f, 0f,    -0.06f, 0.82f, -1.42f },
            { -0.35f, 0f,     0.05f, 0.96f, -1.58f },
            {  0.40f, 0f,     0.12f, 0.98f, -1.66f },
            {  1.15f, 0f,     0.04f, 0.96f, -1.72f },
            {  0.90f, 0.20f,  0.02f, 1.00f, -1.64f },
            {  0.55f, 0.44f,  0.10f, 1.04f, -1.70f },
            {  0.30f, 0.56f,  0.04f, 1.30f, -1.66f },
            { -1.05f, 1f,     0.00f, 2.05f, -1.72f },
            { -0.70f, 1f,     0.05f, 2.95f, -1.78f },
            {  0.00f, 1f,    -0.04f, 3.15f, -1.84f },
            {  0.95f, 1f,     0.06f, 2.70f, -1.70f },
            {  1.75f, 1f,     0.00f, 1.50f, -1.30f },
            {  2.20f, 1f,     0.00f, 0.50f, -0.90f },
        };
        // A ring's outline, floor middle round by the right wall, the ceiling and the left wall: x as a share of the half width,
        // y as a share of the way from floor to ceiling. A loaf, flat on top where it lies under the court. ⚠️ Flat almost to
        // its shoulders: the lens leaves 2.2 m to the side of the spot, and under the first, rounder roof it was out of the
        // shell 15 cm before it was through the court.
        private static readonly Vector2[] PuProfile =
        {
            new Vector2(0.00f, 0.00f), new Vector2(0.45f, 0.02f), new Vector2(0.80f, 0.12f), new Vector2(0.97f, 0.34f),
            new Vector2(1.00f, 0.58f), new Vector2(0.97f, 0.86f), new Vector2(0.87f, 0.985f), new Vector2(0.50f, 1.00f),
            new Vector2(-0.48f, 1.00f), new Vector2(-0.85f, 0.98f), new Vector2(-0.96f, 0.87f), new Vector2(-1.00f, 0.57f),
            new Vector2(-0.96f, 0.33f), new Vector2(-0.78f, 0.11f), new Vector2(-0.42f, 0.02f),
        };
        // How far each point of a ring bulges or caves (a share of the half width); each ring reads the table from its own start.
        private static readonly float[] PuLumps = { .06f, -.05f, .09f, -.03f, .11f, -.08f, .04f, .02f, -.07f, .10f, -.04f, .08f, -.10f, .05f, -.06f };

        // THE BEDS of the soil, from the court down: the height of a bed's top under the court, its colour, how far its top
        // wanders, how fast along the tunnel, where the wander starts. The lens rides at about -0.7, between the sand and the loam.
        private static readonly float[,] PuStrataRows =
        {
            {  0.00f, 0.235f, 0.205f, 0.175f, 0.000f, 0.0f, 0.0f },   // the court's own packed fill
            { -0.20f, 0.150f, 0.098f, 0.062f, 0.030f, 3.1f, 0.4f },   // black topsoil
            { -0.44f, 0.400f, 0.215f, 0.125f, 0.050f, 2.3f, 1.9f },   // red clay
            { -0.68f, 0.520f, 0.390f, 0.205f, 0.028f, 4.4f, 3.3f },   // a thin bed of ochre sand
            { -0.80f, 0.295f, 0.195f, 0.118f, 0.060f, 1.7f, 0.9f },   // brown loam
            { -1.14f, 0.205f, 0.132f, 0.092f, 0.045f, 2.9f, 5.1f },   // dark clay
            { -1.38f, 0.335f, 0.285f, 0.225f, 0.038f, 3.7f, 2.6f },   // stony grey
            { -1.62f, 0.118f, 0.082f, 0.062f, 0.050f, 2.1f, 4.2f },   // the deep
        };

        // HIS ROOTS: x, y, along (a, b). The middle one comes down from between his palms; the two beside it from each palm
        // (his roots are plural: `PaeteRootRidge` races three). They wander, and climb to the court under the spot.
        private static readonly float[,] PuMainRows =
        {
            {  0.00f, -0.02f,  0.05f, 0f }, {  0.02f, -0.45f,  0.22f, 0f }, { -0.03f, -0.85f,  0.50f, 0f }, { -0.08f, -1.04f,  0.95f, 0f },
            { -0.10f, -1.08f,  1.30f, 0f }, {  0.06f, -1.10f,  0.95f, 0.20f }, {  0.14f, -0.98f,  0.60f, 0.40f }, {  0.02f, -1.08f,  0.30f, 0.56f },
            { -0.10f, -1.00f, -1.15f, 1f }, { -0.04f, -1.04f, -0.62f, 1f }, {  0.02f, -0.90f, -0.24f, 1f }, {  0.00f, -0.50f, -0.05f, 1f },
            {  0.00f, -0.06f,  0.00f, 1f },
        };
        private static readonly float[,] PuLeftRows =
        {
            { -0.38f, -0.02f,  0.00f, 0f }, { -0.40f, -0.40f,  0.25f, 0f }, { -0.34f, -0.70f,  0.70f, 0f }, { -0.30f, -0.80f,  1.30f, 0f },
            { -0.38f, -0.72f,  0.90f, 0.25f }, { -0.26f, -0.84f,  0.60f, 0.45f }, { -0.34f, -1.00f,  0.30f, 0.60f }, { -0.30f, -1.02f, -1.00f, 1f },
            { -0.22f, -0.52f, -0.35f, 1f }, { -0.18f, -0.06f, -0.12f, 1f },
        };
        private static readonly float[,] PuRightRows =
        {
            {  0.36f, -0.02f,  0.02f, 0f }, {  0.40f, -0.50f,  0.20f, 0f }, {  0.44f, -1.00f,  0.55f, 0f }, {  0.40f, -1.26f,  1.20f, 0f },
            {  0.34f, -1.34f,  0.95f, 0.24f }, {  0.42f, -1.26f,  0.60f, 0.44f }, {  0.30f, -1.36f,  0.32f, 0.58f }, {  0.28f, -1.20f, -0.90f, 1f },
            {  0.20f, -0.60f, -0.30f, 1f }, {  0.14f, -0.06f, -0.10f, 1f },
        };
        // Per root: how far along the tunnel its stub already reaches when the lens arrives (it dug in at the slam), how late it
        // leaves at the send, how late it reaches the court, its girth under his hands and near its head, the cords' spread.
        private static readonly float[,] PuRootRows =
        {
            { 1.25f, 0.00f, 0.00f, 0.064f, 0.038f, 0.55f },
            { 1.02f, 0.03f, 0.02f, 0.040f, 0.024f, 0.50f },
            { 1.12f, 0.05f, 0.04f, 0.044f, 0.026f, 0.50f },
        };

        // THE ROOTLETS that shoot out as a head passes: which root (0 the middle, 1 left, 2 right), where along it (a share of
        // its length), which way round it (degrees: 0 to his right, 90 up, 180 to his left), its length, its girth, how far
        // it sweeps forward. None points long at the lens (which rides up and to the right of the middle root, about 50 degrees).
        private static readonly float[,] PuRootletRows =
        {
            { 0, .34f, 200f, .26f, .016f, .50f }, { 0, .38f, -35f, .24f, .014f, .30f }, { 0, .43f, 150f, .30f, .018f, .65f },
            { 0, .47f, 265f, .22f, .013f, .40f }, { 0, .51f, 100f, .12f, .011f, .20f }, { 0, .55f, 215f, .28f, .016f, .55f },
            { 0, .59f, -15f, .26f, .015f, .35f }, { 0, .63f, 170f, .22f, .013f, .60f }, { 0, .67f, 290f, .30f, .017f, .45f },
            { 0, .71f, 130f, .20f, .012f, .25f }, { 0, .75f, 20f, .10f, .010f, .30f }, { 0, .78f, 235f, .26f, .015f, .50f },
            { 1, .40f, 160f, .20f, .011f, .40f }, { 1, .55f, 250f, .18f, .010f, .55f }, { 1, .68f, 110f, .16f, .009f, .30f },
            { 2, .45f, -40f, .22f, .012f, .45f }, { 2, .60f, 280f, .18f, .010f, .35f }, { 2, .72f, 10f, .16f, .009f, .50f },
        };

        // THE PULSES that run along the middle root from his hands to its head: when each leaves, how fast (metres a second on
        // this clock), its size, its strength. The first is his third heartbeat running down the stub; the rest overtake the lens.
        private static readonly float[,] PuPulseRows =
        {
            { 1.95f, 5.5f, .22f, 1.0f }, { 2.17f, 16f, .20f, 1.1f }, { 2.25f, 19f, .26f, 1.3f }, { 2.33f, 22f, .18f, 1.0f },
            { 2.40f, 26f, .24f, 1.2f }, { 2.46f, 30f, .30f, 1.5f },
        };

        // THE STONES in the walls and the floor: x, y, along (a, b), size, how squat, yaw, tilt, which of the three rocks, tone.
        private static readonly float[,] PuStoneRows =
        {
            { -0.82f, -0.95f,  0.55f, 0f,    .22f, .80f,  20f,  10f, 0, 0 },
            {  0.99f, -0.60f,  0.95f, 0f,    .18f, .90f, 110f, -15f, 1, 1 },
            {  0.30f, -1.66f,  1.30f, 0f,    .30f, .60f,  65f,   5f, 2, 2 },
            { -0.70f, -0.42f,  1.55f, 0f,    .16f, .85f, 200f,  25f, 1, 0 },
            {  0.93f, -1.10f,  0.95f, 0.20f, .26f, .75f, 310f, -10f, 0, 1 },
            { -0.86f, -1.25f,  0.40f, 0.40f, .34f, .70f,  40f,  18f, 2, 2 },
            {  1.07f, -0.50f,  0.47f, 0.44f, .20f, .95f, 150f, -22f, 0, 0 },
            {  0.10f, -1.70f,  0.30f, 0.56f, .28f, .55f, 260f,   8f, 1, 1 },
            { -1.38f, -1.22f,  0.20f, 0.64f, .30f, .80f,  95f,  14f, 2, 0 },
            {  1.90f, -1.60f, -0.90f, 1f,    .45f, .65f, 175f,  -6f, 0, 2 },
            { -2.30f, -1.36f, -0.30f, 1f,    .50f, .70f, 330f,  12f, 1, 1 },
            {  0.90f, -1.74f,  0.60f, 1f,    .55f, .60f,  15f,  -9f, 2, 0 },
            { -0.90f, -1.74f, -0.20f, 1f,    .35f, .65f, 235f,  20f, 0, 1 },
            {  2.80f, -0.72f,  0.10f, 1f,    .32f, .85f,  80f, -18f, 1, 2 },
        };
        private static readonly Color[] PuStoneTones =
        {
            new Color(0.36f, 0.33f, 0.29f, 1f), new Color(0.27f, 0.24f, 0.21f, 1f), new Color(0.45f, 0.39f, 0.31f, 1f),
        };
        // The three rocks: how far each of a rock's eighteen points stands out from its middle.
        private static readonly float[][] PuRockLumps =
        {
            new[] { 1.00f, .82f, .74f, .90f, 1.08f, .78f, .95f, .86f, 1.04f, .80f, .92f, .98f, .76f, 1.02f, .88f, .84f, .96f, .90f },
            new[] { .88f, 1.06f, .92f, .70f, .84f, 1.00f, .78f, .98f, .86f, 1.04f, .82f, .90f, .94f, .76f, 1.02f, .96f, .80f, .88f },
            new[] { 1.05f, .90f, .66f, .82f, .94f, .88f, 1.02f, .76f, .92f, .84f, .98f, .72f, .90f, 1.00f, .80f, .94f, .86f, .78f },
        };

        // THE DEAD ROOTS of whatever grew here before: each a line of points (x, y, along a, along b) and a girth. The lens
        // ducks under the first; the rest stay to the sides of its path.
        private static readonly float[][] PuDeadRows =
        {
            new[] { -0.55f, -0.10f, 0.95f, 0f,   -0.30f, -0.30f, 1.05f, 0f,   0.12f, -0.25f, 1.20f, 0f,   0.62f, -0.34f, 1.25f, 0f,   0.96f, -0.74f, 1.42f, 0f },
            new[] { -0.75f, -1.50f, 0.95f, 0.20f,   -0.52f, -1.05f, 1.00f, 0.20f,   -0.46f, -0.60f, 1.10f, 0.20f,   -0.58f, -0.18f, 1.05f, 0.20f },
            new[] {  0.80f, -1.30f, 0.55f, 0.44f,   0.30f, -1.44f, 0.60f, 0.44f,   -0.20f, -1.40f, 0.50f, 0.44f,   -0.70f, -1.20f, 0.62f, 0.44f },
            new[] { -1.10f, -0.08f, 0.20f, 0.62f,   -1.02f, -0.40f, 0.28f, 0.62f,   -1.14f, -0.66f, 0.22f, 0.62f,   -1.06f, -0.98f, 0.30f, 0.62f },
            new[] { -1.60f, -0.10f, 0.50f, 1f,   -1.30f, -0.60f, 0.70f, 1f,   -1.50f, -1.10f, 0.90f, 1f,   -1.20f, -1.62f, 1.10f, 1f },
            new[] {  2.40f, -0.08f, 0.60f, 1f,   2.10f, -0.50f, 0.80f, 1f,   2.30f, -1.00f, 0.90f, 1f,   2.00f, -1.70f, 1.20f, 1f },
            // Hair roots hanging from the hollow's ceiling (film un3: its top half was bare ceiling), clear of where the lens climbs
            // and short enough (none under -0.45 near the hollow's mouth) for a mirrored or pushed-in lens to pass under them.
            new[] {  0.92f, -0.06f, -1.50f, 1f,   0.96f, -0.24f, -1.47f, 1f,   0.88f, -0.42f, -1.50f, 1f },
            new[] {  0.20f, -0.06f, -1.10f, 1f,   0.16f, -0.22f, -1.06f, 1f,   0.24f, -0.40f, -1.10f, 1f },
            new[] {  0.55f, -0.06f,  0.45f, 1f,   0.60f, -0.24f,  0.50f, 1f,   0.52f, -0.44f,  0.46f, 1f },
            new[] { -0.55f, -0.06f, -0.80f, 1f,  -0.50f, -0.24f, -0.84f, 1f,  -0.60f, -0.42f, -0.78f, 1f },
            new[] {  0.70f, -0.06f,  0.30f, 1f,   0.74f, -0.28f,  0.34f, 1f,   0.66f, -0.50f,  0.30f, 1f },
            new[] { -1.25f, -0.06f, -0.20f, 1f,  -1.20f, -0.34f, -0.16f, 1f,  -1.30f, -0.60f, -0.22f, 1f },
            new[] {  1.55f, -0.06f,  0.55f, 1f,   1.50f, -0.30f,  0.60f, 1f,   1.58f, -0.52f,  0.56f, 1f },
        };
        private static readonly float[] PuDeadGirth = { .035f, .028f, .040f, .022f, .050f, .030f, .016f, .020f, .014f, .018f, .015f, .022f, .017f };

        // A LOST SLIPPER AND A RUSTED CAN (the game is tumbang preso: a slipper thrown at a can): where each lies (x, y, along a,
        // along b), its size against the real thing, and how it is turned: the slipper by where its toe points and where its
        // top faces, the can by where its mouth points. Both sit beside the lens's path at its own height, turned to FACE the
        // lens as it comes, large enough to be known in the two or three frames they are in. ⚠️ Film un1 had the slipper
        // edge on, orange with a green strap: a carrot. It shows its top now, the strap standing off it, in one faded red.
        private static readonly float[] PuSlipperRow = { -0.44f, -0.56f, 0.72f, 0.26f, 2.3f,   -0.20f, 0.85f, -0.30f,   0.66f, 0.02f, -0.75f };
        private static readonly float[] PuCanRow = { 0.93f, -0.92f, -0.172f, 0.60f, 2.0f,   -0.55f, 0.28f, -0.80f };
        // The slipper's sole, heel to toe (x across, z along), typed round its edge.
        private static readonly Vector2[] PuSoleOutline =
        {
            new Vector2(0.000f, -0.130f), new Vector2(0.030f, -0.122f), new Vector2(0.042f, -0.095f), new Vector2(0.040f, -0.050f),
            new Vector2(0.036f, 0.000f), new Vector2(0.046f, 0.055f), new Vector2(0.052f, 0.095f), new Vector2(0.040f, 0.125f),
            new Vector2(0.012f, 0.138f), new Vector2(-0.022f, 0.134f), new Vector2(-0.044f, 0.112f), new Vector2(-0.050f, 0.070f),
            new Vector2(-0.042f, 0.010f), new Vector2(-0.040f, -0.050f), new Vector2(-0.040f, -0.098f), new Vector2(-0.026f, -0.123f),
        };
        // The can turned on a lathe: radius, height. Two ribs round its waist and a rim; it is open, its inside dark (`BuildPuCan`).
        private static readonly float[,] PuCanProfile =
        {
            { 0.000f, 0.000f }, { 0.034f, 0.000f }, { 0.037f, 0.004f }, { 0.037f, 0.036f }, { 0.040f, 0.040f }, { 0.037f, 0.044f },
            { 0.037f, 0.070f }, { 0.040f, 0.074f }, { 0.037f, 0.078f }, { 0.037f, 0.106f }, { 0.039f, 0.110f }, { 0.033f, 0.111f },
        };

        // THE MOTES: specks of his light hanging in the burrow, drawn as streaks along the lens's own motion so they give its
        // speed. x, y, along (a, b), width, whose light (0 his lime, 1 her jade). Each within half a metre of the lens's path.
        private static readonly float[,] PuMoteRows =
        {
            { 0.52f, -0.52f, 0.90f, 0f, .016f, 0 }, { 0.02f, -0.48f, 1.15f, 0f, .012f, 1 }, { 0.48f, -0.95f, 1.40f, 0f, .020f, 0 },
            { 0.10f, -0.60f, 1.00f, 0.16f, .014f, 0 }, { 0.55f, -0.60f, 0.95f, 0.22f, .018f, 1 }, { 0.30f, -0.40f, 0.90f, 0.28f, .013f, 0 },
            { -0.02f, -0.82f, 0.80f, 0.34f, .017f, 0 }, { 0.60f, -0.86f, 0.70f, 0.40f, .012f, 1 }, { 0.36f, -0.50f, 0.60f, 0.46f, .021f, 0 },
            { 0.05f, -0.62f, 0.50f, 0.52f, .015f, 0 }, { 0.70f, -0.58f, 0.40f, 0.58f, .013f, 1 }, { 0.50f, -0.96f, 0.30f, 0.64f, .019f, 0 },
            { 0.95f, -0.50f, 0.10f, 0.70f, .014f, 0 }, { 1.10f, -0.90f, 0.00f, 0.76f, .017f, 1 }, { 1.55f, -0.45f, -1.00f, 1f, .013f, 0 },
            { 1.35f, -0.85f, -0.85f, 1f, .020f, 0 }, { 1.95f, -0.35f, -0.70f, 1f, .015f, 1 }, { 2.30f, -0.60f, -0.60f, 1f, .012f, 0 },
        };

        // THE GRIT shaken loose as the root passes, and the clods the breaking court drops: x, the height it falls from, along
        // (a, b), size, when it lets go, which rock. ⚠️ Each at least 30 cm to one side of the lens's line: film un3 dropped the
        // first one through the lens itself, and a 3 cm crumb a hand from the glass was a black boulder filling one frame.
        private static readonly float[,] PuGritRows =
        {
            { 0.10f, -0.35f, 1.30f, 0f, .035f, 2.05f, 0 }, { 0.72f, -0.40f, 0.95f, 0.20f, .030f, 2.12f, 1 }, { 0.76f, -0.50f, 0.70f, 0.36f, .040f, 2.20f, 2 },
            { 0.06f, -0.30f, 0.50f, 0.50f, .030f, 2.26f, 0 }, { -0.45f, -0.40f, 0.30f, 0.62f, .045f, 2.22f, 1 }, { 0.95f, -0.30f, -1.00f, 1f, .040f, 2.40f, 2 },
            { 0.30f, -0.10f, -0.20f, 1f, .070f, 2.50f, 1 }, { -0.40f, -0.10f, 0.10f, 1f, .090f, 2.51f, 0 }, { 0.60f, -0.10f, 0.25f, 1f, .060f, 2.53f, 2 },
            { 0.90f, -0.10f, -0.45f, 1f, .080f, 2.52f, 0 }, { 1.50f, -0.10f, -0.50f, 1f, .070f, 2.55f, 1 }, { -0.20f, -0.10f, -0.60f, 1f, .050f, 2.54f, 2 },
        };

        // THE WORMS, the only other things alive down here: one out of the left wall at the lens's height, one hanging out of
        // the ceiling over the root. Where each leaves the soil (x, y, along a, along b), the way it points (x, y), its
        // length, its girth. Each writhes, and FLINCHES back into the soil as the root's head comes at it: the root is seen
        // to be something that the ground's own creatures get out of the way of.
        private static readonly float[,] PuWormRows =
        {
            { -0.98f, -0.62f, -0.099f, 0.56f,   0.95f,  0.22f,   .44f, .028f },
            {  0.16f, -0.05f, -0.400f, 0.72f,   0.10f, -1.00f,   .32f, .022f },
        };

        // THE CRACKS in the ceiling under the spot: each a line of points out from the spot (x, along), then its width and how
        // late it opens. The first runs out to where the lens goes through; the last is its answer on the other side.
        // ⚠️ Film un1: at 7 to 11 cm, seen from a lens half a metre under the ceiling, they were hairlines. They are twice
        // that now and each lets a CURTAIN of light fall through it (`PuCurtain`, its drop typed below), which is what a
        // lens beside them sees.
        private static readonly float[] PuCurtainDrop = { .14f, .34f, .28f, .38f, .26f, .30f, .30f };
        private static readonly float[][] PuCrackRows =
        {
            new[] { 0.10f, -0.02f, 0.55f, -0.14f, 1.05f, -0.30f, 1.60f, -0.48f, 2.15f, -0.52f, 2.70f, -0.40f, .26f, .00f },
            new[] { -0.08f, 0.05f, -0.50f, 0.22f, -0.95f, 0.18f, -1.45f, 0.40f, -1.90f, 0.38f, .21f, .02f },
            new[] { 0.02f, 0.12f, 0.18f, 0.60f, -0.05f, 1.05f, 0.22f, 1.50f, .18f, .03f },
            new[] { -0.05f, -0.10f, -0.30f, -0.55f, -0.12f, -0.98f, -0.42f, -1.40f, -0.30f, -1.80f, .23f, .01f },
            new[] { 0.12f, 0.08f, 0.62f, 0.42f, 0.95f, 0.85f, 1.50f, 1.00f, .16f, .04f },
            new[] { 0.06f, -0.12f, 0.40f, -0.60f, 0.70f, -1.15f, 0.62f, -1.70f, .17f, .05f },
            new[] { -0.12f, -0.04f, -0.60f, -0.25f, -1.20f, -0.32f, -1.75f, -0.55f, -2.30f, -0.50f, .20f, .03f },
        };

        // ------------------------------------------------------------------ state

        private Transform _puRig;
        private float _puLen = 4.5f;
        private Material _puSoilMat, _puThingMat;
        private MaterialPropertyBlock _puBlock;
        private readonly Vector3[] _puCamEye = new Vector3[PuCamRowCount], _puCamLook = new Vector3[PuCamRowCount];
        private const int PuCamRowCount = 14;
        private readonly List<Vector3>[] _puPath = { new List<Vector3>(112), new List<Vector3>(88), new List<Vector3>(88) };
        private readonly List<float>[] _puCum = { new List<float>(112), new List<float>(88), new List<float>(88) };
        private readonly float[] _puStub = new float[3];
        private readonly Mesh[][] _puCords = new Mesh[3][];
        private readonly List<Mesh> _puRootletMeshes = new List<Mesh>(18), _puCrackMeshes = new List<Mesh>(7), _puCurtainMeshes = new List<Mesh>(7);
        private readonly List<int> _puCurtains = new List<int>(7);
        private readonly List<int> _puRootletTips = new List<int>(18), _puCracks = new List<int>(7), _puPulses = new List<int>(6), _puMotes = new List<int>(18);
        private readonly List<Transform> _puGrit = new List<Transform>(12);
        private readonly List<Mesh> _puWormMeshes = new List<Mesh>(2);
        private readonly int[] _puHeadCore = new int[3], _puHeadHalo = new int[3];
        private int _puPool = -1, _puFlash = -1;
        private readonly List<Vector3> _puLine = new List<Vector3>(48), _puPts = new List<Vector3>(48);
        private readonly List<float> _puRadii = new List<float>(48);
        private readonly Vector4[] _puStrata = new Vector4[8], _puStrataWave = new Vector4[8];

        private Vector3 PuAt(float x, float y, float a, float b) => new Vector3(x, y, a + b * _puLen);
        /// <summary>A point of the set (x to his right, y over the court's top, z along the tunnel) in the scene's own space.</summary>
        private Vector3 PuScene(Vector3 rig) => _puRig.localPosition + _puRig.localRotation * rig;
        private static Vector4 PuLinear(Color c, float w) => QualitySettings.activeColorSpace == ColorSpace.Linear
            ? new Vector4(c.linear.r, c.linear.g, c.linear.b, w) : new Vector4(c.r, c.g, c.b, w);

        // ------------------------------------------------------------------ build

        private void BuildPaeteUnder()
        {
            // The tunnel's line, from between his palms to the spot, on the court. Never assume +z: the spot is pushed off the can.
            var hands = PaeteHandsMid;
            var from = new Vector3(hands.x, 0f, hands.z);
            var to = new Vector3(_pvLanding.x, 0f, _pvLanding.z);
            var line = to - from;
            var dir = line.magnitude > .1f ? line.normalized : Vector3.forward;
            _puLen = Mathf.Max(PuMinLen, line.magnitude);
            var origin = to - dir * _puLen;
            _puRig = new GameObject("PaeteUnder").transform;
            _puRig.SetParent(_root.transform, false);
            _puRig.localPosition = new Vector3(origin.x, _paeteCourt, origin.z);
            _puRig.localRotation = Quaternion.LookRotation(dir, Vector3.up);
            _puBlock = new MaterialPropertyBlock();

            // The one surface everything down here wears (`PaeteSoil.shader`): the walls with their beds painted, the things inked.
            var soil = Resources.Load<Shader>("Shaders/PaeteSoil");
            var shader = soil != null ? soil : (Shader.Find("Standard") ?? Shader.Find("Diffuse"));
            _puSoilMat = new Material(shader) { name = "PaeteUnderSoil" };
            _puThingMat = new Material(shader) { name = "PaeteUnderThing" };
            if (soil != null) { _puSoilMat.SetFloat("_Strata", 1f); _puSoilMat.SetFloat("_Ink", 0f); _puThingMat.SetFloat("_Strata", 0f); }
            else _puSoilMat.color = new Color(0.30f, 0.20f, 0.12f, 1f);
            VfxRenderTag.Own(_puRig.gameObject, _puSoilMat);
            VfxRenderTag.Own(_puRig.gameObject, _puThingMat);
            for (int k = 0; k < 8; k++)
            {
                _puStrata[k] = PuLinear(new Color(PuStrataRows[k, 1], PuStrataRows[k, 2], PuStrataRows[k, 3], 1f), PuStrataRows[k, 0]);
                _puStrataWave[k] = new Vector4(PuStrataRows[k, 4], PuStrataRows[k, 5], PuStrataRows[k, 6], 0f);
            }

            // THE SHELL: one closed mesh, wound to be seen from inside.
            var shell = VfxShapes.Stand(_puRig, "PaeteUnderShell", PuShell(), 1);
            var shellRenderer = shell.GetComponent<Renderer>();
            shellRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; shellRenderer.receiveShadows = false;
            shellRenderer.sharedMaterial = _puSoilMat;
            VfxRenderTag.Attach(shell);

            // THE STONES, the dead roots, the slipper and the can.
            var rocks = new[] { PuRock(PuRockLumps[0]), PuRock(PuRockLumps[1]), PuRock(PuRockLumps[2]) };
            for (int i = 0; i < PuStoneRows.GetLength(0); i++)
            {
                var stone = PuThing("PaeteUnderStone" + i, rocks[(int)PuStoneRows[i, 8]], PuStoneTones[(int)PuStoneRows[i, 9]], 0f, .008f).transform;
                stone.localPosition = PuAt(PuStoneRows[i, 0], PuStoneRows[i, 1], PuStoneRows[i, 2], PuStoneRows[i, 3]);
                stone.localRotation = Quaternion.Euler(PuStoneRows[i, 7], PuStoneRows[i, 6], PuStoneRows[i, 7] * .5f);
                stone.localScale = new Vector3(PuStoneRows[i, 4], PuStoneRows[i, 4] * PuStoneRows[i, 5], PuStoneRows[i, 4] * 1.15f);
            }
            for (int i = 0; i < PuDeadRows.Length; i++)
            {
                var row = PuDeadRows[i];
                _puPts.Clear(); _puRadii.Clear();
                int count = row.Length / 4;
                for (int k = 0; k < count; k++)
                {
                    Vector3 P(int j) { j = Mathf.Clamp(j, 0, count - 1); return PuAt(row[j * 4], row[j * 4 + 1], row[j * 4 + 2], row[j * 4 + 3]); }
                    int steps = k == count - 1 ? 1 : 4;
                    for (int s = 0; s < steps; s++)
                    {
                        _puPts.Add(PuCatmull(P(k - 1), P(k), P(k + 1), P(k + 2), s / 4f));
                        float u = (k + s / 4f) / (count - 1);
                        // Thick where it leaves the wall, thin at its far end, with a knuckle a third of the way.
                        _puRadii.Add(PuDeadGirth[i] * (1.15f - .65f * u + .18f * Mathf.Max(0f, Mathf.Sin(u * 9f + i))));
                    }
                }
                var mesh = new Mesh { name = "PaeteUnderDeadRoot", hideFlags = HideFlags.DontSave };
                PaeteInk.Tube(mesh, _puPts, _puRadii, 5);
                PuThing("PaeteUnderDeadRoot" + i, mesh, i % 2 == 0 ? new Color(0.43f, 0.37f, 0.29f, 1f) : new Color(0.35f, 0.29f, 0.23f, 1f), 0f, .007f);
            }
            BuildPuSlipper();
            BuildPuCan();

            // HIS ROOTS: three cords each for the middle one (his bark, his dark bark, and one cord of his light), two for the others.
            var palette = PaeteProp.Palette;
            Color bark = palette != null && palette.Length == 16 ? palette[13] : GrowthVfx.Bark;
            Color dark = palette != null && palette.Length == 16 ? palette[14] : GrowthVfx.BarkDark;
            Color lit = palette != null && palette.Length == 16 ? palette[15] : GrowthVfx.BarkLit;
            var rows = new[] { PuMainRows, PuLeftRows, PuRightRows };
            for (int r = 0; r < 3; r++)
            {
                PuPath(rows[r], _puPath[r], _puCum[r]);
                // The stub: how much of the root is already there, as a length along it.
                _puStub[r] = _puCum[r][_puCum[r].Count - 1] * .25f;
                for (int i = 0; i < _puPath[r].Count; i++)
                    if (_puPath[r][i].z >= PuRootRows[r, 0]) { _puStub[r] = _puCum[r][i]; break; }
                var colours = r == 0 ? new[] { bark, dark, PaeteLight } : new[] { r == 1 ? lit : bark, PaeteLight };
                _puCords[r] = new Mesh[colours.Length];
                for (int c = 0; c < colours.Length; c++)
                {
                    _puCords[r][c] = new Mesh { name = "PaeteUnderRoot", hideFlags = HideFlags.DontSave };
                    _puCords[r][c].MarkDynamic();
                    bool light = c == colours.Length - 1;
                    PuThing("PaeteUnderRoot" + r + "-" + c, _puCords[r][c], colours[c], light ? 1f : .22f, light ? 0f : .006f);
                }
                _puHeadHalo[r] = PuGlow("PaeteUnderHeadHalo" + r, PaeteLight, falloff: 1.5f, core: 0f);
                _puHeadCore[r] = PuGlow("PaeteUnderHead" + r, PbHot, falloff: 2.0f, core: 1.2f, lift: .04f);
            }
            for (int i = 0; i < PuRootletRows.GetLength(0); i++)
            {
                var mesh = new Mesh { name = "PaeteUnderRootlet", hideFlags = HideFlags.DontSave };
                mesh.MarkDynamic();
                _puRootletMeshes.Add(mesh);
                PuThing("PaeteUnderRootlet" + i, mesh, i % 3 == 0 ? bark : lit, .3f, .004f);
                _puRootletTips.Add(PuGlow("PaeteUnderRootletTip" + i, i % 4 == 1 ? MakilingJade : PaeteLight, falloff: 2.2f, core: .9f, lift: .03f));
            }
            for (int i = 0; i < PuPulseRows.GetLength(0); i++)
                _puPulses.Add(PuGlow("PaeteUnderPulse" + i, i % 3 == 2 ? PbHot : PaeteLight, falloff: 1.9f, core: 1.0f, lift: .12f));

            // THE MOTES, the grit, the cracks, the light through the ceiling, and the flash.
            for (int i = 0; i < PuMoteRows.GetLength(0); i++)
                _puMotes.Add(PvTips(PuGlow("PaeteUnderMote" + i, PuMoteRows[i, 5] > .5f ? MakilingJade : PaeteLight, PvStreak, billboard: false, band: true, falloff: 1.6f, core: .7f), 1.1f));
            for (int i = 0; i < PuGritRows.GetLength(0); i++)
                _puGrit.Add(PuThing("PaeteUnderGrit" + i, rocks[(int)PuGritRows[i, 6]], PuStoneTones[i % 3], 0f, .004f).transform);
            for (int i = 0; i < PuWormRows.GetLength(0); i++)
            {
                var mesh = new Mesh { name = "PaeteUnderWorm", hideFlags = HideFlags.DontSave };
                mesh.MarkDynamic();
                _puWormMeshes.Add(mesh);
                PuThing("PaeteUnderWorm" + i, mesh, i == 0 ? new Color(0.68f, 0.42f, 0.38f, 1f) : new Color(0.60f, 0.36f, 0.34f, 1f), .12f, .004f);
            }
            for (int i = 0; i < PuCrackRows.Length; i++)
            {
                var mesh = new Mesh { name = "PaeteUnderCrack", hideFlags = HideFlags.DontSave };
                mesh.MarkDynamic();
                _puCrackMeshes.Add(mesh);
                _puCracks.Add(PuGlow("PaeteUnderCrack" + i, i % 3 == 1 ? MakilingJade : PaeteLight, mesh, billboard: false, band: true, falloff: 2.0f, core: 1.3f));
                var curtain = new Mesh { name = "PaeteUnderCurtain", hideFlags = HideFlags.DontSave };
                curtain.MarkDynamic();
                _puCurtainMeshes.Add(curtain);
                _puCurtains.Add(PuGlow("PaeteUnderCurtain" + i, PaeteLight, curtain, billboard: false, band: true, falloff: 2.6f, core: .2f));
            }
            _puPool = PuGlow("PaeteUnderPool", PaeteLight, billboard: false, falloff: 1.5f, core: .5f);
            // THE FLASH as the lens goes through the court: the whole frame toward his lime for two frames, in `SpiritVeil`'s
            // clip space quad (it blends, it does not add, so it cannot clip to white, and it covers whatever camera draws it).
            var veil = Resources.Load<Shader>("Shaders/SpiritVeil");
            if (veil != null)
            {
                var go = VfxShapes.Stand(_root.transform, "PaeteUnderFlash", PbVeilQuad, 1);
                var renderer = go.GetComponent<Renderer>();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
                var m = new Material(veil) { name = "PaeteUnderFlash" };
                m.SetFloat("_Inner", -1f); m.SetFloat("_Outer", -.5f); m.SetFloat("_Tint", 1f);
                renderer.sharedMaterial = m;
                VfxRenderTag.Own(go, m);
                _pieces.Add(new Piece { Transform = go.transform, Renderer = renderer, Color = new Color(PaeteLight.r, PaeteLight.g, PaeteLight.b, 1f) });
                _puFlash = _pieces.Count - 1;
            }

            // The camera's typed points, laid along this tunnel.
            for (int i = 0; i < PuCamRowCount; i++)
            {
                _puCamEye[i] = PuAt(PuCamRows[i, 1], PuCamRows[i, 2], PuCamRows[i, 3], PuCamRows[i, 4]);
                _puCamLook[i] = PuAt(PuCamRows[i, 5], PuCamRows[i, 6], PuCamRows[i, 7], PuCamRows[i, 8]);
            }
            // ⚠️ THE PLUNGE STARTS 2.6 M BEHIND HIS HANDS AND NOBODY ELSE JUDGES IT: `UltimatePhaseView.ChooseShot` tests a shot at
            // its END only (this one's is above the court beside the spot). With his back to a wall its first frames would be shot
            // from inside the wall. So its first two points are drawn in along their own line, toward where the lens enters the
            // court, to 40 cm short of whatever solid stands on that line (bodies, slippers and the can do not count, as in
            // `ClearShot`). ⚠️ Unfilmed: the editor film's stage has no colliders.
            var enter = _puCamEye[2] + Vector3.up * .3f;
            var enterWorld = _puRig.TransformPoint(enter);
            var startWorld = _puRig.TransformPoint(_puCamEye[0]);
            float run = Vector3.Distance(enterWorld, startWorld), clear = run;
            if (run > .1f)
                foreach (var hit in Physics.RaycastAll(enterWorld, (startWorld - enterWorld) / run, run, ~0, QueryTriggerInteraction.Ignore))
                {
                    var solid = hit.collider;
                    if (solid == null || solid.GetComponentInParent<CharacterMotor>() != null || solid.GetComponentInParent<Slipper>() != null
                        || solid.GetComponentInParent<Lata>() != null) continue;
                    clear = Mathf.Min(clear, hit.distance);
                }
            if (clear < run)
            {
                float keep = Mathf.Clamp((clear - .4f) / run, .3f, 1f);
                for (int i = 0; i < 2; i++) _puCamEye[i] = Vector3.Lerp(enter, _puCamEye[i], keep);
            }
            _puRig.gameObject.SetActive(false);
        }

        /// <summary>A thing in the soil: its own colour, how much of it shines by itself, its ink. Lit only by his root (`PaeteSoil`).</summary>
        private Renderer PuThing(string name, Mesh mesh, Color colour, float self, float ink, Transform parent = null)
        {
            var go = VfxShapes.Stand(parent != null ? parent : _puRig, name, mesh, 1);
            var renderer = go.GetComponent<Renderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
            renderer.sharedMaterial = _puThingMat;
            _puBlock.Clear();
            _puBlock.SetColor("_Color", colour); _puBlock.SetFloat("_Self", self); _puBlock.SetFloat("_Ink", ink);
            renderer.SetPropertyBlock(_puBlock);
            VfxRenderTag.Attach(go);
            return renderer;
        }

        /// <summary>A piece of light that lives in the tunnel's own space (so `PlaceGlow` takes the set's coordinates).</summary>
        private int PuGlow(string name, Color colour, Mesh mesh = null, bool billboard = true, bool band = false, float falloff = 2f, float core = .6f, float lift = 0f)
        {
            int piece = AddGlow(name, colour, mesh, billboard, band, falloff, core, lift);
            _pieces[piece].Transform.SetParent(_puRig, false);
            return piece;
        }

        private void BuildPuSlipper()
        {
            var holder = new GameObject("PaeteUnderSlipper").transform;
            holder.SetParent(_puRig, false);
            holder.localPosition = PuAt(PuSlipperRow[0], PuSlipperRow[1], PuSlipperRow[2], PuSlipperRow[3]);
            holder.localRotation = Quaternion.LookRotation(new Vector3(PuSlipperRow[5], PuSlipperRow[6], PuSlipperRow[7]).normalized,
                                                           new Vector3(PuSlipperRow[8], PuSlipperRow[9], PuSlipperRow[10]).normalized);
            holder.localScale = Vector3.one * PuSlipperRow[4];
            // The sole: the typed outline, 22 mm thick.
            const float Half = .011f;
            int n = PuSoleOutline.Length;
            var v = new List<Vector3>(n * 2 + 2); var tris = new List<int>(n * 12);
            foreach (var p in PuSoleOutline) v.Add(new Vector3(p.x, Half, p.y));
            foreach (var p in PuSoleOutline) v.Add(new Vector3(p.x, -Half, p.y));
            int top = v.Count; v.Add(new Vector3(0f, Half, 0f));
            int bottom = v.Count; v.Add(new Vector3(0f, -Half, 0f));
            for (int i = 0; i < n; i++)
            {
                int k = (i + 1) % n;
                tris.AddRange(new[] { top, i, k, bottom, n + k, n + i, i, n + i, k, k, n + i, n + k });
            }
            PuFace(v, tris, 0, mid => new Vector3(0f, 0f, mid.z * .6f), false);
            var sole = new Mesh { name = "PaeteUnderSole", hideFlags = HideFlags.DontSave };
            sole.SetVertices(v); sole.SetTriangles(tris, 0); sole.RecalculateNormals(); sole.RecalculateBounds();
            PuThing("PaeteUnderSole", sole, new Color(0.64f, 0.25f, 0.22f, 1f), .10f, .004f, holder);
            // Its strap: from the toe post back to each side of the sole, standing up off it.
            for (int side = -1; side <= 1; side += 2)
            {
                _puPts.Clear(); _puRadii.Clear();
                _puPts.Add(new Vector3(.002f * side, Half - .002f, .082f)); _puRadii.Add(.0065f);
                _puPts.Add(new Vector3(.012f * side, Half + .020f, .060f)); _puRadii.Add(.0065f);
                _puPts.Add(new Vector3(.032f * side, Half + .026f, .018f)); _puRadii.Add(.0070f);
                _puPts.Add(new Vector3((side > 0 ? .041f : .043f) * side, Half + .012f, -.030f)); _puRadii.Add(.0070f);
                _puPts.Add(new Vector3((side > 0 ? .040f : .041f) * side, Half - .004f, -.048f)); _puRadii.Add(.0060f);
                var strap = new Mesh { name = "PaeteUnderStrap", hideFlags = HideFlags.DontSave };
                PaeteInk.Tube(strap, _puPts, _puRadii, 5);
                PuThing("PaeteUnderStrap" + side, strap, new Color(0.36f, 0.12f, 0.11f, 1f), .10f, .003f, holder);
            }
        }

        private void BuildPuCan()
        {
            const int Sides = 10;
            int rows = PuCanProfile.GetLength(0);
            var v = new List<Vector3>(rows * Sides); var tris = new List<int>(rows * Sides * 6);
            for (int r = 0; r < rows; r++)
                for (int j = 0; j < Sides; j++)
                {
                    float a = j * Mathf.PI * 2f / Sides;
                    v.Add(new Vector3(Mathf.Cos(a) * PuCanProfile[r, 0], PuCanProfile[r, 1], Mathf.Sin(a) * PuCanProfile[r, 0]));
                }
            for (int r = 0; r < rows - 1; r++)
                for (int j = 0; j < Sides; j++)
                {
                    int a = r * Sides + j, b = r * Sides + (j + 1) % Sides, c = a + Sides, d = b + Sides;
                    tris.AddRange(new[] { a, c, b, b, c, d });
                }
            PuFace(v, tris, 0, mid => new Vector3(0f, .055f, 0f), false);
            var mesh = new Mesh { name = "PaeteUnderCan", hideFlags = HideFlags.DontSave };
            mesh.SetVertices(v); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var can = new GameObject("PaeteUnderCanHolder").transform;
            can.SetParent(_puRig, false);
            can.localPosition = PuAt(PuCanRow[0], PuCanRow[1], PuCanRow[2], PuCanRow[3]);
            can.localRotation = Quaternion.FromToRotation(Vector3.up, new Vector3(PuCanRow[5], PuCanRow[6], PuCanRow[7]).normalized);
            can.localScale = Vector3.one * PuCanRow[4];
            // Tin gone dull, not rust orange (film un1: an orange cylinder read as a cork).
            PuThing("PaeteUnderCan", mesh, new Color(0.50f, 0.47f, 0.40f, 1f), .06f, .003f, can);
            // Its inside: the wall and the bottom seen through its open mouth, dark, turned inward.
            var iv = new List<Vector3>(Sides * 2 + 1); var itris = new List<int>(Sides * 9);
            for (int j = 0; j < Sides; j++) { float a = j * Mathf.PI * 2f / Sides; iv.Add(new Vector3(Mathf.Cos(a) * .033f, .111f, Mathf.Sin(a) * .033f)); }
            for (int j = 0; j < Sides; j++) { float a = j * Mathf.PI * 2f / Sides; iv.Add(new Vector3(Mathf.Cos(a) * .033f, .022f, Mathf.Sin(a) * .033f)); }
            int floor = iv.Count; iv.Add(new Vector3(0f, .022f, 0f));
            for (int j = 0; j < Sides; j++)
            {
                int k = (j + 1) % Sides;
                itris.AddRange(new[] { j, Sides + j, k, k, Sides + j, Sides + k, floor, Sides + j, Sides + k });
            }
            PuFace(iv, itris, 0, mid => new Vector3(0f, mid.y + .03f, 0f), true);
            var inside = new Mesh { name = "PaeteUnderCanInside", hideFlags = HideFlags.DontSave };
            inside.SetVertices(iv); inside.SetTriangles(itris, 0); inside.RecalculateNormals(); inside.RecalculateBounds();
            PuThing("PaeteUnderCanInside", inside, new Color(0.10f, 0.075f, 0.055f, 1f), 0f, 0f, can);
        }

        // ------------------------------------------------------------------ sample

        private void SamplePaeteUnder(float t, float leave)
        {
            if (_puRig == null) return;
            bool on = t > PuFrom - .04f && t < PuTo + .02f && leave > .002f;
            if (_puRig.gameObject.activeSelf != on) _puRig.gameObject.SetActive(on);
            // The flash: two frames round the moment the lens goes through the court.
            if (_puFlash >= 0)
                Place(_puFlash, Vector3.zero, Vector3.one, Quaternion.identity, on && !_reducedEffects ? .55f * Mathf.Clamp01(1f - Mathf.Abs(t - PuUpAt) / .036f) * leave : 0f);
            if (!on) return;
            float gain = (_reducedEffects ? .5f : 1f) * leave;
            PuLens(t, out var eye, out _, out _);
            PuLens(t + .012f, out var ahead, out _, out _);
            PuLens(t - .012f, out var behind, out _, out _);
            var motion = (ahead - behind) / .024f;
            // ⚠️ The v6 leaf that drifts across the lens through the roots' race (`PvLensRows`) is laid along THIS lens now; a
            // green leaf hanging in front of it under the ground is wrong, so it is put away while the lens is under.
            if (eye.y < 0f) foreach (int leaf in _pvLens) PvHide(leaf);

            // ---------------------------------------------------------------- HIS ROOTS, their heads, and their light on the soil.
            var headAt = Vector3.zero;
            for (int r = 0; r < 3; r++)
            {
                float total = _puCum[r][_puCum[r].Count - 1];
                float reach = PuReach(r, t);
                var head = PuAlong(_puPath[r], _puCum[r], reach);
                if (r == 0) headAt = head;
                PuDrawRoot(r, reach, total, t);
                // The head burns hot while it runs and dies into the court as it arrives.
                float run = Ease(PaeteSendAt + PuRootRows[r, 1] - .02f, PaeteSendAt + PuRootRows[r, 1] + .05f, t);
                float arrive = 1f - Ease(PaeteArriveAt + PuRootRows[r, 2] - .02f, PaeteArriveAt + PuRootRows[r, 2] + .06f, t);
                float beat = .35f + .25f * Decay(t - PaetePulses[2], .2f);
                float burn = Mathf.Lerp(beat, 1f, run) * arrive * gain;
                float flicker = 1f + .12f * Mathf.Sin(t * 61f + r * 2.1f);
                float size = r == 0 ? 1f : .6f;
                PlaceGlow(_puHeadCore[r], head, Vector3.one * (.10f + .07f * run) * size * flicker, Quaternion.identity, 1.5f * burn);
                // ⚠️ Small: film un1's halos (1.3 m, .55) were three green discs that hid the roots and the walls they light.
                PlaceGlow(_puHeadHalo[r], head, Vector3.one * (.34f + .30f * run) * size, Quaternion.identity, .34f * burn);
            }
            PuLight(t, headAt, gain);

            // ---------------------------------------------------------------- THE ROOTLETS shoot out as a head passes.
            for (int i = 0; i < _puRootletMeshes.Count; i++)
            {
                int r = (int)PuRootletRows[i, 0];
                float total = _puCum[r][_puCum[r].Count - 1];
                float at = PuRootletRows[i, 1] * total;
                float reach = PuReach(r, t);
                // It starts 12 cm behind the head and is out in a tenth of a second.
                float grow = Mathf.Clamp01((reach - at - .12f) / (.1f * PuSpeed(r)));
                if (grow <= .01f) { _puRootletMeshes[i].Clear(); PlaceGlow(_puRootletTips[i], Vector3.zero, Vector3.one, Quaternion.identity, 0f); continue; }
                grow = 1f - (1f - grow) * (1f - grow);
                var p0 = PuAlong(_puPath[r], _puCum[r], at);
                var tangent = (PuAlong(_puPath[r], _puCum[r], at + .08f) - PuAlong(_puPath[r], _puCum[r], at - .08f)).normalized;
                var side = Vector3.Cross(Vector3.up, tangent).normalized;
                var up = Vector3.Cross(tangent, side);
                float angle = PuRootletRows[i, 2] * Mathf.Deg2Rad;
                var outward = side * Mathf.Cos(angle) + up * Mathf.Sin(angle);
                var across = Vector3.Cross(tangent, outward);
                float length = PuRootletRows[i, 3] * grow;
                _puPts.Clear(); _puRadii.Clear();
                const int Samples = 7;
                for (int k = 0; k <= Samples; k++)
                {
                    float u = k / (float)Samples;
                    _puPts.Add(p0 + outward * (length * u) + tangent * (PuRootletRows[i, 5] * length * u * u)
                               + across * (.07f * length * Mathf.Sin(u * 8f + i * 1.7f) * u));
                    _puRadii.Add(PuRootletRows[i, 4] * (1f - .82f * u));
                }
                PaeteInk.Tube(_puRootletMeshes[i], _puPts, _puRadii, 4);
                float spark = (1f - grow) + .25f * Decay(t - PaeteArriveAt, .2f);
                PlaceGlow(_puRootletTips[i], _puPts[_puPts.Count - 1], Vector3.one * (.05f + .08f * spark), Quaternion.identity, (.35f + 1.3f * spark) * gain);
            }

            // ---------------------------------------------------------------- THE PULSES along the middle root.
            float mainReach = PuReach(0, t);
            for (int i = 0; i < _puPulses.Count; i++)
            {
                float s = t - PuPulseRows[i, 0];
                float d = s * PuPulseRows[i, 1];
                if (s < 0f || d > mainReach + .05f) { PlaceGlow(_puPulses[i], Vector3.zero, Vector3.one, Quaternion.identity, 0f); continue; }
                float fade = Mathf.Clamp01((mainReach - d) / .35f) * Mathf.Clamp01(s / .02f);
                var at = PuAlong(_puPath[0], _puCum[0], d);
                PlaceGlow(_puPulses[i], at, Vector3.one * PuPulseRows[i, 2], Quaternion.identity, PuPulseRows[i, 3] * fade * gain);
            }

            // ---------------------------------------------------------------- THE MOTES, streaked along the lens's own motion.
            float speed = motion.magnitude;
            var along = speed > .01f ? motion / speed : Vector3.forward;
            for (int i = 0; i < _puMotes.Count; i++)
            {
                var at = PuAt(PuMoteRows[i, 0], PuMoteRows[i, 1], PuMoteRows[i, 2], PuMoteRows[i, 3]);
                at.y += .03f * Mathf.Sin(t * 5f + i * 1.3f);
                var toLens = eye - at;
                var facing = toLens - Vector3.Dot(toLens, along) * along;
                float near = toLens.magnitude;
                if (facing.sqrMagnitude < 1e-5f || near > 3.2f || eye.y > 0f) { PlaceGlow(_puMotes[i], Vector3.zero, Vector3.one, Quaternion.identity, 0f); continue; }
                // A frame's worth of travel long, so the streaks join from one frame to the next.
                float length = Mathf.Clamp(speed * .03f, .03f, .42f);
                float twinkle = .7f + .3f * Mathf.Sin(t * 43f + i * 2.3f);
                PlaceGlow(_puMotes[i], at, new Vector3(PuMoteRows[i, 4], length, 1f), Quaternion.LookRotation(facing.normalized, along),
                          1.1f * twinkle * (1f - Mathf.Clamp01((near - 2.2f) / 1f)) * gain);
            }

            // ---------------------------------------------------------------- THE GRIT shaken loose, and the court's clods.
            for (int i = 0; i < _puGrit.Count; i++)
            {
                float s = t - PuGritRows[i, 5];
                var at = PuAt(PuGritRows[i, 0], PuGritRows[i, 1], PuGritRows[i, 2], PuGritRows[i, 3]);
                // This clock runs `PaeteStretch` slower than real, so a real fall is 9.8 times its square on it.
                at.y -= Mathf.Max(0f, s) * (1.2f + 8.3f * Mathf.Max(0f, s));
                bool shown = s > 0f && at.y > -1.7f;
                _puGrit[i].localPosition = at;
                _puGrit[i].localScale = shown ? new Vector3(1f, .8f, 1.2f) * PuGritRows[i, 4] : Vector3.zero;
                _puGrit[i].localRotation = Quaternion.Euler(s * 700f + 40f * i, s * 420f, 25f * i);
            }

            // ---------------------------------------------------------------- THE WORMS writhe, and flinch from the head.
            for (int i = 0; i < _puWormMeshes.Count; i++)
            {
                var from = PuAt(PuWormRows[i, 0], PuWormRows[i, 1], PuWormRows[i, 2], PuWormRows[i, 3]);
                var point = new Vector3(PuWormRows[i, 4], PuWormRows[i, 5], 0f).normalized;
                var sway = Vector3.Cross(point, Vector3.forward).normalized;
                // Gone to a third of its length by the time the head is 15 cm short of it; it starts when the head is 60 cm off.
                float flinch = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((headAt.z - (from.z - .6f)) / .45f));
                float length = PuWormRows[i, 6] * (1f - .68f * flinch);
                _puPts.Clear(); _puRadii.Clear();
                const int Samples = 9;
                for (int k = 0; k <= Samples; k++)
                {
                    float u = k / (float)Samples;
                    float writhe = (.045f - .03f * flinch) * Mathf.Sin(u * 6.5f - t * 27f + i * 2.4f) * u;
                    _puPts.Add(from + point * (length * u) + sway * writhe + Vector3.forward * (.035f * Mathf.Sin(u * 5f + t * 19f + i) * u)
                               + Vector3.down * (.07f * u * u * (1f - Mathf.Abs(point.y))));
                    // Blunt at its head, a saddle a third of the way along.
                    _puRadii.Add(PuWormRows[i, 7] * (1f + .5f * flinch) * (k == Samples ? .55f : 1f + .16f * Mathf.Max(0f, 1f - Mathf.Abs(u - .34f) / .12f)));
                }
                PaeteInk.Tube(_puWormMeshes[i], _puPts, _puRadii, 6);
            }

            // ---------------------------------------------------------------- THE COURT BREAKS over the spot: cracks of light in the ceiling.
            var spot = new Vector3(0f, PuCeil - .012f, _puLen);
            for (int i = 0; i < _puCracks.Count; i++)
            {
                var row = PuCrackRows[i];
                int points = (row.Length - 2) / 2;
                float width = row[row.Length - 2], delay = row[row.Length - 1];
                float open = Ease(PaeteArriveAt - .06f + delay, PaeteArriveAt + .07f + delay, t);
                if (open <= .01f)
                {
                    _puCrackMeshes[i].Clear(); _puCurtainMeshes[i].Clear();
                    PlaceGlow(_puCracks[i], Vector3.zero, Vector3.one, Quaternion.identity, 0f);
                    PlaceGlow(_puCurtains[i], Vector3.zero, Vector3.one, Quaternion.identity, 0f);
                    continue;
                }
                _puLine.Clear();
                _puLine.Add(spot);
                float shown = open * points;
                for (int k = 0; k < points; k++)
                {
                    var p = spot + new Vector3(row[k * 2], 0f, row[k * 2 + 1]);
                    if (k + 1 <= shown) { _puLine.Add(p); continue; }
                    _puLine.Add(Vector3.Lerp(_puLine[_puLine.Count - 1], p, shown - k));
                    break;
                }
                PuFlatRibbon(_puCrackMeshes[i], _puLine, width);
                float throb = 1f + .25f * Mathf.Sin(t * 70f + i * 1.9f);
                PlaceGlow(_puCracks[i], Vector3.zero, Vector3.one, Quaternion.identity, (1.3f + 1.0f * Decay(t - PaeteArriveAt - delay, .12f)) * throb * gain);
                PuCurtain(_puCurtainMeshes[i], _puLine, PuCurtainDrop[i] * (.5f + .5f * open));
                PlaceGlow(_puCurtains[i], Vector3.zero, Vector3.one, Quaternion.identity, (.30f + .35f * Decay(t - PaeteArriveAt - delay, .15f)) * throb * gain);
            }
            float leak = Ease(PaeteArriveAt - .08f, PaeteArriveAt + .05f, t);
            PlaceGlow(_puPool, spot + Vector3.down * .01f, new Vector3(2.6f, 2.6f, 1f) * (.6f + .4f * leak), Quaternion.Euler(90f, 0f, 0f), .8f * leak * gain);
        }

        /// <summary>How far along root <paramref name="r"/> its head is: the stub until the send, the court under the spot at the arrival.</summary>
        private float PuReach(int r, float t)
        {
            float total = _puCum[r][_puCum[r].Count - 1];
            float u = Mathf.InverseLerp(PaeteSendAt + PuRootRows[r, 1], PaeteArriveAt + PuRootRows[r, 2], t);
            // Off the mark fast and a little slower into the spot: something alive, never a rod pushed at one speed.
            return Mathf.Lerp(_puStub[r], total, u * (1.35f - .35f * u));
        }

        /// <summary>A root's mean speed along itself on this clock (m/s), to turn a time into a length behind its head.</summary>
        private float PuSpeed(int r)
            => (_puCum[r][_puCum[r].Count - 1] - _puStub[r]) / Mathf.Max(.05f, PaeteArriveAt + PuRootRows[r, 2] - PaeteSendAt - PuRootRows[r, 1]);

        /// <summary>
        /// One of his roots as far as its head: cords wound round its line (`PaeteRope`'s weave, drawn here because these wear the
        /// soil's shader and one cord is his light), thick under his hands, tapering over its last 1.3 m to the head, writhing there.
        /// ⚠️ Film un2: at 8.5 cm with a 70 cm taper, the metre of it the lens sees from behind its head was a tusk, not a root.
        /// </summary>
        private void PuDrawRoot(int r, float reach, float total, float t)
        {
            var cords = _puCords[r];
            const int Samples = 36;
            _puLine.Clear();
            for (int i = 0; i <= Samples; i++)
            {
                float d = reach * i / Samples;
                var p = PuAlong(_puPath[r], _puCum[r], d);
                float live = Mathf.Clamp01(1f - (reach - d) / .9f);
                p.x += .022f * live * Mathf.Sin(d * 9f - t * 38f + r);
                p.y += .016f * live * Mathf.Cos(d * 7f - t * 31f + r * 2f);
                _puLine.Add(p);
            }
            float girth0 = PuRootRows[r, 3], girth1 = PuRootRows[r, 4], spread = PuRootRows[r, 5];
            for (int c = 0; c < cords.Length; c++)
            {
                _puPts.Clear(); _puRadii.Clear();
                bool light = c == cords.Length - 1;
                Vector3 side = Vector3.zero;
                for (int i = 0; i <= Samples; i++)
                {
                    float d = reach * i / Samples;
                    var tangent = _puLine[Mathf.Min(Samples, i + 1)] - _puLine[Mathf.Max(0, i - 1)];
                    if (tangent.sqrMagnitude < 1e-8f) tangent = Vector3.forward;
                    tangent.Normalize();
                    side = i == 0 ? Vector3.Cross(tangent, Mathf.Abs(tangent.x) < .9f ? Vector3.right : Vector3.up) : side - Vector3.Dot(side, tangent) * tangent;
                    if (side.sqrMagnitude < 1e-8f) side = Vector3.Cross(tangent, Vector3.up);
                    side.Normalize();
                    var up = Vector3.Cross(tangent, side);
                    float girth = Mathf.Lerp(girth0, girth1, d / Mathf.Max(.01f, total)) * Mathf.Lerp(1f, .16f, Mathf.SmoothStep(0f, 1f, 1f - (reach - d) / 1.3f));
                    float a = c * Mathf.PI * 2f / cords.Length + d * 5.5f + r * 1.3f;
                    _puPts.Add(_puLine[i] + (side * Mathf.Cos(a) + up * Mathf.Sin(a)) * girth * spread);
                    _puRadii.Add(girth * (light ? .42f : c == 0 ? .78f : .68f));
                }
                PaeteInk.Tube(cords[c], _puPts, _puRadii, 6);
            }
        }

        /// <summary>
        /// HIS LIGHT ON THE SOIL, handed to `PaeteSoil.shader`: the lit length of the middle root as a line of light from under his
        /// hands to its head, the head again, hotter, and the light through the cracks under the spot. Each pulse leaving his
        /// hands lifts the whole line for a moment.
        /// </summary>
        private void PuLight(float t, Vector3 head, float gain)
        {
            float beat = 0f;
            for (int i = 0; i < PuPulseRows.GetLength(0); i++) beat = Mathf.Max(beat, Decay(t - PuPulseRows[i, 0], .1f));
            beat = Mathf.Max(beat, Decay(t - PaeteSendAt, .14f));
            float run = Ease(PaeteSendAt - .02f, PaeteSendAt + .05f, t);
            float arrived = Ease(PaeteArriveAt - .03f, PaeteArriveAt + .06f, t);
            var under = _puRig.TransformPoint(PuAlong(_puPath[0], _puCum[0], .45f));
            var headWorld = _puRig.TransformPoint(head);
            var spot = _puRig.TransformPoint(new Vector3(0f, PuCeil - .10f, _puLen));
            float crack = Ease(PaeteArriveAt - .08f, PaeteArriveAt + .06f, t) * (1f + .5f * Decay(t - PaeteArriveAt, .15f));
            var rootA = new Vector4(under.x, under.y, under.z, (.62f + .50f * beat) * gain);
            var rootB = new Vector4(headWorld.x, headWorld.y, headWorld.z, Mathf.Lerp(.9f, 2.3f, run) * (1f - .6f * arrived) * gain);
            var spotLight = new Vector4(spot.x, spot.y, spot.z, 3.2f * crack * gain);
            // .34 of its own colour with no light on it: the phase camera's grade (`PaeteGrade`) takes a quarter off the whole
            // frame through these beats, and the editor film that tuned this has no grade.
            var light = PuLinear(PaeteLight, .34f);
            var deep = PuLinear(new Color(0.035f, 0.030f, 0.022f, 1f), 7.5f);
            var toRig = _puRig.worldToLocalMatrix;
            for (int m = 0; m < 2; m++)
            {
                var material = m == 0 ? _puSoilMat : _puThingMat;
                if (material == null) continue;
                material.SetMatrix("_PuToRig", toRig);
                material.SetVector("_PuRootA", rootA); material.SetVector("_PuRootB", rootB); material.SetVector("_PuSpot", spotLight);
                material.SetVector("_PuLight", light); material.SetVector("_PuDeep", deep);
                material.SetVectorArray("_PuStrata", _puStrata); material.SetVectorArray("_PuStrataWave", _puStrataWave);
            }
        }

        // ------------------------------------------------------------------ the camera

        /// <summary>The lens at <paramref name="t"/> in the set's own space: the typed points joined by a smooth curve through time.</summary>
        private void PuLens(float t, out Vector3 eye, out Vector3 look, out float fov)
        {
            int n = PuCamRowCount;
            t = Mathf.Clamp(t, PuCamRows[0, 0], PuCamRows[n - 1, 0]);
            int i = 0;
            while (i < n - 2 && t > PuCamRows[i + 1, 0]) i++;
            int a = Mathf.Max(0, i - 1), d = Mathf.Min(n - 1, i + 2);
            float t1 = PuCamRows[i, 0], t2 = PuCamRows[i + 1, 0];
            float t0 = a == i ? t1 - (t2 - t1) : PuCamRows[a, 0], t3 = d == i + 1 ? t2 + (t2 - t1) : PuCamRows[d, 0];
            float u = Mathf.InverseLerp(t1, t2, t);
            eye = PuHermite(_puCamEye[a], _puCamEye[i], _puCamEye[i + 1], _puCamEye[d], t0, t1, t2, t3, u);
            var typed = PuHermite(_puCamLook[a], _puCamLook[i], _puCamLook[i + 1], _puCamLook[d], t0, t1, t2, t3, u);
            float smooth = u * u * (3f - 2f * u);
            float chase = Mathf.Lerp(PuCamRows[i, 9], PuCamRows[i + 1, 9], smooth);
            fov = Mathf.Lerp(PuCamRows[i, 10], PuCamRows[i + 1, 10], smooth);
            // The racing head, and a little past and above it: the root runs up the lower middle of the frame and the tunnel
            // it is about to light is the top of it (film un1 looked AT the head and showed mostly floor).
            // ⚠️ The lead dies as the head reaches the climb under the spot (film un2 kept it, and the hollow's frames were ceiling).
            float reach = PuReach(0, t);
            float lead = Mathf.Clamp01((_puCum[0][_puCum[0].Count - 1] - 1.3f - reach) / 1.0f);
            var head = PuAlong(_puPath[0], _puCum[0], reach + .8f * lead) + Vector3.up * (.06f + .16f * lead);
            look = Vector3.Lerp(typed, head, chase);
            // ⚠️ SQUARE TO THE TUNNEL WHILE IT IS IN THE BURROW (2.025 to 2.40): the focus keeps the eye's own x, so the lens tilts
            // to follow the head and never turns. `UltimatePhaseView` mirrors a blocked shot by reflecting the eye about the focus
            // across his left and right, for the WHOLE shot, judged only at its end; a lens turned toward the root would then
            // ride the root's other side, through the slipper and the left root. Square, the mirrored ride is this ride. It only
            // turns (to the spot) once it is in the hollow, which is as wide to the left as to the right for that reason.
            float square = Ease(1.985f, 2.025f, t) * (1f - Ease(2.40f, 2.50f, t));
            look.x = Mathf.Lerp(look.x, eye.x, square);
            // A lens never looks straight down its own up: keep the focus at least 25 cm ahead on the flat.
            var flat = new Vector2(look.x - eye.x, look.z - eye.z);
            if (flat.magnitude < .25f) look.z = eye.z + .25f;
        }

        private static Vector3 PuHermite(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t0, float t1, float t2, float t3, float u)
        {
            float span = t2 - t1;
            Vector3 m1 = (p2 - p0) / Mathf.Max(1e-4f, t2 - t0) * span, m2 = (p3 - p1) / Mathf.Max(1e-4f, t3 - t1) * span;
            float u2 = u * u, u3 = u2 * u;
            return (2f * u3 - 3f * u2 + 1f) * p1 + (u3 - 2f * u2 + u) * m1 + (-2f * u3 + 3f * u2) * p2 + (u3 - u2) * m2;
        }

        /// <summary>
        /// ⚠️⚠️ THE DIVE'S LENS (owner, 2026-10-08: *"The camera goes underground ... rides his root through the dark soil, then
        /// bursts back up with the tree's first claw."*), in the scene's own space; `PaeteFrame` hands it to whichever authored shot
        /// covers the moment, as `PtCamera` does for THE TAKE. True only from 1.93 to 2.70 on the 5.0 s clock. ⚠️ By 2.66 it is
        /// above the court again: `UltimatePhaseView.ChooseShot` judges a shot at its end, and a lens still under the ground then
        /// would be "blocked" by the court's own collider and the whole dive swapped for another shot.
        /// </summary>
        private bool PuCamera(float t, out Vector3 eye, out Vector3 look, out float fov)
        {
            eye = look = Vector3.zero; fov = 60f;
            if (_puRig == null || t < PuFrom - .0005f || t >= PuTo - .0005f) return false;
            PuLens(t, out var e, out var l, out fov);
            eye = PuScene(e); look = PuScene(l);
            return true;
        }

        // ------------------------------------------------------------------ meshes and paths

        /// <summary>A typed line of points (x, y, along a, along b) as a smooth path with its running length.</summary>
        private void PuPath(float[,] rows, List<Vector3> path, List<float> cum)
        {
            int n = rows.GetLength(0);
            Vector3 At(int i) { i = Mathf.Clamp(i, 0, n - 1); return PuAt(rows[i, 0], rows[i, 1], rows[i, 2], rows[i, 3]); }
            path.Clear(); cum.Clear();
            const int Sub = 8;
            for (int i = 0; i < n - 1; i++)
                for (int k = 0; k < Sub; k++) path.Add(PuCatmull(At(i - 1), At(i), At(i + 1), At(i + 2), k / (float)Sub));
            path.Add(At(n - 1));
            float d = 0f; cum.Add(0f);
            for (int i = 1; i < path.Count; i++) { d += Vector3.Distance(path[i], path[i - 1]); cum.Add(d); }
        }

        private static Vector3 PuCatmull(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float u)
            => .5f * (2f * b + (c - a) * u + (2f * a - 5f * b + 4f * c - d) * u * u + (3f * b - a - 3f * c + d) * u * u * u);

        private static Vector3 PuAlong(List<Vector3> path, List<float> cum, float d)
        {
            int n = path.Count;
            if (d <= 0f) return path[0];
            if (d >= cum[n - 1]) return path[n - 1];
            int lo = 0, hi = n - 1;
            while (hi - lo > 1) { int mid = (lo + hi) >> 1; if (cum[mid] <= d) lo = mid; else hi = mid; }
            return Vector3.Lerp(path[lo], path[hi], Mathf.InverseLerp(cum[lo], cum[hi], d));
        }

        /// <summary>
        /// Turn every triangle from <paramref name="from"/> on to face toward (or away from) the point its middle is given.
        /// ⚠️ <paramref name="together"/> is for a surface laid in ONE winding (the shell's walls, each of its ends): the whole of it
        /// is judged at once and turned or left as one. Judged triangle by triangle, the few of the shell's faces that look mostly
        /// along the tunnel (where the hollow closes behind the spot) were turned the wrong way and culled: film un4 had slivers of
        /// sky in the far wall.
        /// </summary>
        private static void PuFace(List<Vector3> v, List<int> tris, int from, System.Func<Vector3, Vector3> centre, bool inward, bool together = false)
        {
            float all = 0f;
            if (together)
                for (int i = from; i + 2 < tris.Count; i += 3)
                {
                    Vector3 a = v[tris[i]], b = v[tris[i + 1]], c = v[tris[i + 2]];
                    var mid = (a + b + c) / 3f;
                    all += Vector3.Dot(Vector3.Cross(b - a, c - a), centre(mid) - mid);
                }
            for (int i = from; i + 2 < tris.Count; i += 3)
            {
                Vector3 a = v[tris[i]], b = v[tris[i + 1]], c = v[tris[i + 2]];
                var mid = (a + b + c) / 3f;
                float facing = together ? all : Vector3.Dot(Vector3.Cross(b - a, c - a), centre(mid) - mid);
                if ((facing > 0f) != inward) { int k = tris[i + 1]; tris[i + 1] = tris[i + 2]; tris[i + 2] = k; }
            }
        }

        /// <summary>
        /// ⚠️ THE SOIL ROUND THE LENS: the typed rings joined into one closed tube, EVERY FACE TURNED INWARD (back faces are culled,
        /// so from above the court, or from anywhere outside, none of it draws). Each ring is the typed loaf, bulged by the typed
        /// lumps read from its own start; four rings are laid between each typed pair so the walls bend and do not crease. Its
        /// normals are smooth and point in, which is what `PaeteSoil` lights.
        /// </summary>
        private Mesh PuShell()
        {
            int rows = PuRingRows.GetLength(0), m = PuProfile.Length;
            const int Sub = 4;
            var v = new List<Vector3>(); var tris = new List<int>();
            Vector4 Ring(int i) { i = Mathf.Clamp(i, 0, rows - 1); return new Vector4(PuRingRows[i, 0] + PuRingRows[i, 1] * _puLen, PuRingRows[i, 2], PuRingRows[i, 3], PuRingRows[i, 4]); }
            int rings = 0;
            for (int i = 0; i < rows; i++)
            {
                int steps = i == rows - 1 ? 1 : Sub;
                for (int s = 0; s < steps; s++)
                {
                    float u = s / (float)Sub;
                    Vector4 a = Ring(i - 1), b = Ring(i), c = Ring(i + 1), d = Ring(i + 2);
                    Vector4 ring = .5f * (2f * b + (c - a) * u + (2f * a - 5f * b + 4f * c - d) * u * u + (3f * b - a - 3f * c + d) * u * u * u);
                    // Along never runs backward and a wall never closes, whatever the curve does between two typed rings.
                    ring.x = Mathf.Lerp(b.x, c.x, u); ring.z = Mathf.Max(.3f, ring.z);
                    for (int j = 0; j < m; j++)
                    {
                        float lump = Mathf.Lerp(PuLumps[(j + 4 * i) % PuLumps.Length], PuLumps[(j + 4 * (i + 1)) % PuLumps.Length], u);
                        float deep = Mathf.Lerp(PuLumps[(j + 7 + 3 * i) % PuLumps.Length], PuLumps[(j + 7 + 3 * (i + 1)) % PuLumps.Length], u);
                        float low = 1f - PuProfile[j].y;
                        v.Add(new Vector3(ring.y + ring.z * PuProfile[j].x * (1f + lump),
                                          Mathf.Min(PuCeil, Mathf.Lerp(ring.w, PuCeil, PuProfile[j].y) + .9f * deep * low * low), ring.x));
                    }
                    rings++;
                }
            }
            for (int r = 0; r < rings - 1; r++)
                for (int j = 0; j < m; j++)
                {
                    int a = r * m + j, b = r * m + (j + 1) % m, c = a + m, d = b + m;
                    tris.AddRange(new[] { a, c, b, b, c, d });
                }
            PuFace(v, tris, 0, mid => new Vector3(0f, -.85f, mid.z), true, together: true);
            // Its two ends, closed.
            int walls = tris.Count;
            int back = v.Count; v.Add(new Vector3(0f, -.6f, v[0].z - .05f));
            for (int j = 0; j < m; j++) tris.AddRange(new[] { back, j, (j + 1) % m });
            PuFace(v, tris, walls, mid => mid + Vector3.forward, true, together: true);
            int caps = tris.Count, last = (rings - 1) * m;
            int front = v.Count; v.Add(new Vector3(0f, -.5f, v[last].z + .05f));
            for (int j = 0; j < m; j++) tris.AddRange(new[] { front, last + j, last + (j + 1) % m });
            PuFace(v, tris, caps, mid => mid + Vector3.back, true, together: true);
            var mesh = new Mesh { name = "PaeteUnderShell", hideFlags = HideFlags.DontSave };
            mesh.SetVertices(v); mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>A rock: an eight-sided ball split once (eighteen points), each point pushed out by its typed share. Faces out.</summary>
        private static Mesh PuRock(float[] lumps)
        {
            var v = new List<Vector3> { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            int[] faces = { 0, 2, 4, 4, 2, 1, 1, 2, 5, 5, 2, 0, 4, 3, 0, 1, 3, 4, 5, 3, 1, 0, 3, 5 };
            var tris = new List<int>(96);
            var mids = new Dictionary<int, int>();
            int Mid(int a, int b)
            {
                int key = a < b ? a * 64 + b : b * 64 + a;
                if (mids.TryGetValue(key, out int at)) return at;
                at = v.Count; v.Add(((v[a] + v[b]) * .5f).normalized); mids[key] = at;
                return at;
            }
            for (int f = 0; f < faces.Length; f += 3)
            {
                int a = faces[f], b = faces[f + 1], c = faces[f + 2];
                int ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
                tris.AddRange(new[] { a, ab, ca, ab, b, bc, ca, bc, c, ab, bc, ca });
            }
            for (int i = 0; i < v.Count; i++) v[i] *= lumps[i % lumps.Length];
            PuFace(v, tris, 0, mid => Vector3.zero, false);
            var mesh = new Mesh { name = "PaeteUnderRock", hideFlags = HideFlags.DontSave };
            mesh.SetVertices(v); mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// The light falling through a crack: a ribbon hanging from <paramref name="line"/>, as far above the ceiling as below it
        /// so `SpiritGlow`'s band (brightest down its middle) is brightest AT the crack; the ceiling hides the upper half.
        /// </summary>
        private static void PuCurtain(Mesh mesh, List<Vector3> line, float drop)
        {
            int n = line.Count;
            if (n < 2) { mesh.Clear(); return; }
            var vertices = new Vector3[n * 2]; var uv = new Vector2[n * 2]; var triangles = new int[(n - 1) * 6];
            for (int i = 0; i < n; i++)
            {
                float u = i / (float)(n - 1);
                float half = drop * (1f - .65f * u);
                vertices[i * 2] = line[i] + Vector3.up * half; vertices[i * 2 + 1] = line[i] - Vector3.up * half;
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

        /// <summary>A flat ribbon along <paramref name="line"/> on the ceiling, widest where it starts; u along, v across (`SpiritGlow`'s band).</summary>
        private static void PuFlatRibbon(Mesh mesh, List<Vector3> line, float width)
        {
            int n = line.Count;
            if (n < 2) { mesh.Clear(); return; }
            var vertices = new Vector3[n * 2]; var uv = new Vector2[n * 2]; var triangles = new int[(n - 1) * 6];
            for (int i = 0; i < n; i++)
            {
                var tangent = line[Mathf.Min(n - 1, i + 1)] - line[Mathf.Max(0, i - 1)];
                var across = Vector3.Cross(Vector3.up, tangent).normalized;
                float u = i / (float)(n - 1);
                float half = width * (1f - .8f * u) * .5f;
                vertices[i * 2] = line[i] - across * half; vertices[i * 2 + 1] = line[i] + across * half;
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
    }
}
