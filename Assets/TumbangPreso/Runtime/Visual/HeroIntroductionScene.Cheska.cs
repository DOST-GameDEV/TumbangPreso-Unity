using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // YASMIN, ABSOLUTE ZERO, 6.5 s (was 3.2 s; plan.md § 6 is the first one, staged as GLACIAL NOVA).
        //
        // ⚠️ THE SIXTH STAGING, 2026-10-08. The tone is the owner's: "cheeky but surprisingly strong". How it got here, each
        // one filmed: a crystal with the court in miniature in it ("cheska's is just boring overall"); she taps the lens and
        // the court is a gallery of ice statues ("the whole animation just feels dead. there's no pzazz or character to
        // it"); she stands and each snap is a hard cut to a player in a flat colour panel ("maybe do something where its
        // like her pov and shes pointing to and snapping at each character's live ingame positions. and then do that star
        // burst effect. i like the idea of her skating around too"); she skates a whole lap seen over her shoulder ("i was
        // thinking she only skates at the start, then its fpv one by one looking at each character, it zooms in to focus
        // on them, and then does that graphical starburst effect").
        // ⚠️ AND THE SEVENTH, the same day, which is this file, 11.5 s (9.0 at first; see the last note of this list). Of the sixth (6.5 s, a quick loop, three looks of 0.8 s
        // each with a hand built here that had fingers) he said: "its a bit too sped up and linear for my liking. first of all
        // the first skating part is just not graceful enough, next the fingers are weird, i was thinking just use the original
        // fpv hand, instead make her whole elbow bent and then extend to point towards the person. after do a top view of her
        // skating circling around, then do the glacier stuff".
        //   1. SKATE (0 to 2.3): one long slow loop on a ribbon of ice, a leg held out behind her, her arms wide, and a
        //      turn on the spot to stop where she began.
        //   2. ONE, TWO, THREE (2.3, 3.6, 4.5), THROUGH HER EYES, and not three alike: the first is slow (1.3 s), the next
        //      two quicker (0.9, 0.8). She turns to another player where they really stand, her arm comes up with the
        //      elbow bent, the view zooms in on them, and the arm straightens out at them. That point is the trigger: the frame goes to one flat colour, a two-colour
        //      star bursts behind them and a block of ice comes down over them. There is no snap and no fingers.
        //   3. THE HEART (5.3 to 7.95), from straight above: a one-line heart on a long waving stroke (`CkRoute`): she skates a heart on the ice, inside everyone, from its point
        //      where she stands round both lobes and home again.
        //   4. THE STOMP (7.95 to 8.55), wide and low: one foot down and the whole court goes up in ice out of that ring.
        //   5. (THE BLOW is cut: the heart needed its time, 2.65 s for a line 30 m long. `CkBlow` is past the end.) It was: she blows the frost off her fingertip.
        //   6. THE SNAP (8.55 to 9.0), high and wide: her hands snap apart, the glacier bursts, and everyone is left
        //      standing in their block of ice. Play resumes with them frozen (`HeroAbility.IntroductionIsTheWindup`).
        // ⚠️ THE TIMES IN THE LIST ABOVE ARE THE 9.0 s CUT'S. It is 11.5 s now, on his word ("ykw we can extend the ult cutscene
        // until everything looks smooth"), and then 12.6 (of the blow, "its ending too fast"): the skate to 2.8, the looks at
        // 2.8, 4.1 and 5.0, the heart 5.8 to 9.9, the stomp at 10.05, the blow 10.55 to 11.9, the last snap at 12.28. And then
        // 11.9: "i think the heart is a tad bit too slow for someone skating", so the heart is 3.4 s (was 4.1) and everything
        // after it is 0.7 s sooner. The constants below are the truth.
        //
        // ⚠️ THE ARM IN HER VIEW IS HER MODEL'S OWN, ELBOW AND ALL. The first cut of this used the first-person arm play draws
        // (`Models/RosterArms/cheska_*`), which is her arm cut out as ONE rigid mesh, and I told him it had no joint; he:
        // "doesnt cheska's model have elbows?". It does (`forearm-left`, `forearm-right`, her authoring script's rule 8).
        // Posing the arm on her body where it stands did not work either (film c14): her arm is short and hangs a forearm's
        // length off the line of sight, so the zoom looks straight past it. So what is in her view is a SECOND COPY OF HER
        // WHOLE BODY, held to the frame the way a first-person arm is in play: its shoulder at the bottom corner whatever the
        // zoom, its head shrunk to nothing, everything else of it behind or below the lens. Its upper arm and forearm are
        // posed here: up with the elbow closed, the fist by her cheek, then out straight at them.
        // ⚠️ HER BODY TRAVELS in the skate and the circle (the clip writes only the bones under the copy's root, and the game
        // samples the clip before this stage, so the root is this file's to move). It is home, turned as it began, after each.
        // ⚠️ THE ICE IS MODELLED AND PAINTED NOW (2026-10-10; every shape here was a stock prism while the staging was judged).
        // The block on each player, the glacier, the spikes round the court, the chunks it breaks into and the pines are built
        // as the hand companions and Paete's plants are (`tools/build_ultimate_props_cheska.py`: a painted atlas, round forms
        // for snow, the ice's planes painted face by face) and loaded by `UltimateProp.Cheska.cs`; the ribbon she skates, her
        // spray, the blown frost, the frost on the court and her sky are built in this file. Nothing of the staging moved:
        // not a time, not a shot, not the heart, not her path, not the arm. The owner, once it stood: "continue polishing all
        // the ultimates. i dont want to see anything bare in textures, anything looking unpolished, no vfx etc."
        // ⚠️ NOTHING WITH WEIGHT MOVES ON A STRAIGHT LINE OF TIME. A block falls faster as it falls, flattens where it lands,
        // springs a little too tall and settles; a peak shoots up past its height, drops under it and settles, rocking; the
        // glacier draws in for an instant before it breaks, and what it breaks into is thrown on arcs.
        // ⚠️ WHAT IS DRAWN OVER WHAT (`sortingOrder`): the frozen court and the ribbons under 0; the colour panels at 0,
        // sorted by distance as they always were; a block at 1, over the live restraint that stands inside it; the frost at
        // its foot at 2; any small see-through piece that may stand in front of ice (the blown frost, a shard) at 3.
        // ⚠️ The colour panels are stood for the AUTHORED lens; a shot the
        // game mirrors or pushes in on a real map (`UltimatePhaseView.ChooseShot`) would see past their edge.
        // ⚠️ LUNA SNOW (the reference he named) WAS NOT LOOKED AT: the footage would not draw in the browser pane.
        // ⚠️ EVERY TIME IS ON THE TABLE'S CLOCK (`tools/author_ultimate_intros.py`, `_cheska`). The last pose is `snap`.
        // =========================================================================================
        private const float CkSpinFrom = 2.24f, CkSpinTo = 2.72f, CkCircleFrom = 5.80f, CkCircleTo = 9.16f, CkHeartMost = 4.4f, CkHeartLeast = 4.4f,
                            CkStomp = 9.35f, CkBlow = 10.02f, CkSnap = 11.58f;
        private const int CkFirstLook = 1, CkWideShot = 5;
        // The skate: one loop in front of her, and when she is at each point of it.
        private static readonly float[] CkWayAt = { .26f, .74f, 1.20f, 1.63f, 2.00f, 2.33f };
        // ⚠️ Kept inside 2.6 m of where she stands: the others are staged no nearer than 3 m, so she never skates through one.
        private static readonly Vector3[] CkWay =
        { Vector3.zero, new Vector3(1.9f, 0, .9f), new Vector3(1.3f, 0, 2.3f), new Vector3(-1.3f, 0, 2.2f), new Vector3(-1.9f, 0, .8f), Vector3.zero };
        // The three looks: when each starts, how long it is given, and when in it her arm points (the first is the slow one).
        private static readonly float[] CkLooks = { 2.80f, 4.10f, 5.00f }, CkLookFor = { 1.30f, .90f, .80f }, CkPointIn = { .86f, .54f, .46f };
        private static readonly Color CheskaIce = new Color(.62f, .9f, 1, .9f);
        // Each panel's flat colour, and the star behind the white one.
        private static readonly Color[] CkPanelColour = { new Color(.30f, .78f, .95f, 1), new Color(.98f, .96f, .86f, 1), new Color(.16f, .40f, .62f, 1) };
        private static readonly Color[] CkPanelStar = { new Color(.16f, .40f, .62f, 1), new Color(.30f, .78f, .95f, 1), new Color(.30f, .78f, .95f, 1) };
        // The crown behind her: (x, z, height, lean), typed, the middle one the tallest.
        private static readonly Vector4[] CkCrown =
        {
            new Vector4(-3.6f, -2.6f, 4.6f, 12f), new Vector4(-1.7f, -3.5f, 7.2f, 5f), new Vector4(.2f, -3.9f, 9.0f, -2f),
            new Vector4(2.1f, -3.4f, 6.6f, -7f), new Vector4(3.9f, -2.4f, 4.1f, -13f),
        };
        // The rings that race out from her foot: (distance, how many, height).
        private static readonly Vector3[] CkRings = { new Vector3(3.3f, 7, 1.5f), new Vector3(5.8f, 10, 2.3f), new Vector3(8.5f, 13, 3.2f) };
        private static readonly Vector3[] CkMotes =
        {
            new Vector3(-1.6f, 1.9f, 2.2f), new Vector3(1.1f, 2.6f, 3.4f), new Vector3(2.8f, 1.2f, 1.6f), new Vector3(-3.4f, 2.3f, 4.6f),
            new Vector3(.4f, 3.1f, 5.8f), new Vector3(3.9f, 1.7f, 5.1f), new Vector3(-2.2f, 1.1f, 6.9f), new Vector3(1.9f, 2.2f, 7.8f),
            new Vector3(-4.8f, 1.6f, 2.1f), new Vector3(5.2f, 2.8f, 3.0f), new Vector3(-.8f, 1.5f, 9.2f), new Vector3(3.1f, 3.3f, 9.9f),
            new Vector3(-3.9f, 2.9f, 8.4f), new Vector3(.9f, 1.0f, 4.4f), new Vector3(-5.6f, 2.0f, 6.0f), new Vector3(4.6f, 1.3f, 7.2f),
            new Vector3(-1.3f, 2.7f, -1.8f), new Vector3(2.2f, 1.8f, -2.6f), new Vector3(-3.0f, 1.4f, -3.4f), new Vector3(.3f, 3.4f, 1.0f),
        };

        // ---- the look (2026-10-10). Every row below is typed; none is a formula's.
        // Her pink: the one warm colour in her world, and used as an accent only.
        private static readonly Color CkPink = new Color(1f, .62f, .80f, 1f);
        // Where the glacier stands (the middle of its crown), and when each peak of it starts up after the stomp.
        private static readonly Vector3 CkGlacierAt = new Vector3(0, 0, -3.4f);
        private static readonly string[] CkPeakName = { "foot", "peak", "left-a", "right-a", "left-b", "right-b", "front-l", "front-r" };
        private static readonly float[] CkPeakFrom = { .02f, .04f, .07f, .08f, .10f, .11f, .13f, .14f };
        // The ice the peaks shed as they stop: where each leaves from (near a cap) and which way it is knocked.
        private static readonly Vector3[] CkShedFrom =
        {
            new Vector3(-.5f, 8.5f, -3.7f), new Vector3(.4f, 7.0f, -3.3f), new Vector3(2.4f, 6.8f, -3.4f), new Vector3(1.9f, 5.2f, -3.1f),
            new Vector3(-3.0f, 6.2f, -3.3f), new Vector3(-2.4f, 4.8f, -3.0f), new Vector3(4.6f, 4.2f, -2.5f), new Vector3(-4.9f, 3.8f, -2.3f),
        };
        private static readonly Vector3[] CkShedGo =
        {
            new Vector3(-.6f, 1.2f, 1.4f), new Vector3(.9f, .6f, 1.9f), new Vector3(1.3f, 1.0f, 1.2f), new Vector3(-.4f, .4f, 2.1f),
            new Vector3(-1.4f, .9f, 1.1f), new Vector3(.5f, .5f, 1.8f), new Vector3(1.6f, .8f, .9f), new Vector3(-1.7f, .7f, 1.0f),
        };
        // What it breaks into on her last snap: where each chunk starts in the crown, how it is thrown (m/s), its size, and
        // the axis it tumbles about.
        private static readonly Vector3[] CkChunkFrom =
        {
            new Vector3(-.4f, 7.4f, -3.9f), new Vector3(-.2f, 4.6f, -3.7f), new Vector3(.3f, 2.2f, -3.4f), new Vector3(2.3f, 5.6f, -3.5f), new Vector3(1.9f, 2.6f, -3.3f),
            new Vector3(-2.8f, 5.0f, -3.4f), new Vector3(-2.3f, 2.2f, -3.3f), new Vector3(4.4f, 3.0f, -2.6f), new Vector3(-4.6f, 2.8f, -2.4f), new Vector3(.6f, 5.8f, -3.6f),
        };
        private static readonly Vector3[] CkChunkGo =
        {
            new Vector3(-2f, 11f, 4f), new Vector3(-6f, 6f, 8f), new Vector3(2f, 4f, 11f), new Vector3(7f, 9f, 4f), new Vector3(4.5f, 5f, 9f),
            new Vector3(-8f, 8f, 5f), new Vector3(-5f, 3f, 10f), new Vector3(11f, 5f, 6f), new Vector3(-11f, 4.5f, 6f), new Vector3(3f, 10f, 7f),
        };
        private static readonly Vector3[] CkChunkSpin =
        {
            new Vector3(1, .3f, .2f), new Vector3(.2f, 1, .5f), new Vector3(.6f, .2f, 1), new Vector3(-1, .4f, .3f), new Vector3(.3f, -1, .6f),
            new Vector3(.8f, .5f, -1), new Vector3(-.4f, 1, .2f), new Vector3(1, -.6f, .4f), new Vector3(.2f, .7f, 1), new Vector3(-1, -.3f, .5f),
        };
        private static readonly float[] CkChunkSize = { 2.8f, 2.3f, 2.0f, 2.5f, 1.9f, 2.4f, 2.0f, 2.2f, 2.2f, 1.9f };
        // Chips kicked out as a block lands (out, up, across; turned to the block), and her spray as she glides
        // (to the side, how high, how far back, when in the cycle).
        private static readonly Vector3[] CkChipGo = { new Vector3(.9f, .55f, .5f), new Vector3(-.8f, .7f, .6f), new Vector3(.4f, .45f, -.9f), new Vector3(-.5f, .6f, -.8f) };
        private static readonly Vector4[] CkSprayGo =
        {
            new Vector4(.55f, .50f, .5f, 0f), new Vector4(-.5f, .42f, .6f, .17f), new Vector4(.3f, .7f, .8f, .34f),
            new Vector4(-.28f, .6f, .7f, .5f), new Vector4(.75f, .3f, .4f, .67f), new Vector4(-.7f, .34f, .45f, .83f),
        };
        // The fan of shaved ice she throws up stopping: each blade (which way in degrees off her line, how long, how high, how wide).
        private static readonly Vector4[] CkFanBlade =
        {
            new Vector4(-64f, .62f, .30f, .20f), new Vector4(-40f, .92f, .52f, .24f), new Vector4(-17f, 1.18f, .78f, .26f), new Vector4(4f, 1.30f, .92f, .28f),
            new Vector4(24f, 1.08f, .70f, .25f), new Vector4(46f, .84f, .46f, .23f), new Vector4(68f, .56f, .26f, .19f),
        };
        // A frost fern, the six-armed kind that grows on a pane: each arm (which way in degrees, how long), and the barbs
        // every arm carries (where along it, how long). No two arms are a mirror of each other.
        private static readonly Vector2[] CkFernArm = { new Vector2(0f, 1f), new Vector2(57f, .86f), new Vector2(122f, .95f), new Vector2(181f, .9f), new Vector2(238f, 1f), new Vector2(301f, .84f) };
        private static readonly Vector2[] CkFernBarb = { new Vector2(.30f, .36f), new Vector2(.52f, .27f), new Vector2(.72f, .17f) };
        // The ferns on the court after the stomp: (x, z, how wide, turned). The first is the one under her foot.
        private static readonly Vector4[] CkCourtFern =
        {
            new Vector4(0f, 0f, 6.4f, 11f), new Vector4(-4.2f, 3.1f, 2.6f, 20f), new Vector4(3.8f, 4.6f, 3.0f, -14f), new Vector4(-1.6f, 7.4f, 2.4f, 33f),
            new Vector4(5.6f, -.8f, 2.2f, 8f), new Vector4(-5.8f, -1.6f, 2.8f, -25f), new Vector4(1.4f, -5.2f, 2.4f, 41f), new Vector4(7.2f, 6.6f, 2.6f, -6f), new Vector4(-7.0f, 5.8f, 2.4f, 27f),
        };
        // The cold curls blown off her fingertip: each one's line in its own flat (across, up), and how wide it is along it.
        private static readonly Vector2[][] CkCurlLine =
        {
            new[] { new Vector2(0f, 0f), new Vector2(.25f, .05f), new Vector2(.52f, .19f), new Vector2(.70f, .45f), new Vector2(.66f, .73f), new Vector2(.44f, .86f), new Vector2(.26f, .75f), new Vector2(.27f, .57f), new Vector2(.40f, .52f) },
            new[] { new Vector2(0f, 0f), new Vector2(.20f, -.03f), new Vector2(.44f, .06f), new Vector2(.62f, .26f), new Vector2(.60f, .50f), new Vector2(.42f, .60f), new Vector2(.30f, .49f), new Vector2(.36f, .38f) },
            new[] { new Vector2(0f, 0f), new Vector2(.30f, .08f), new Vector2(.60f, .30f), new Vector2(.74f, .62f), new Vector2(.60f, .88f), new Vector2(.36f, .90f), new Vector2(.24f, .72f), new Vector2(.34f, .60f), new Vector2(.46f, .64f) },
        };
        private static readonly float[][] CkCurlWide =
        {
            new[] { .03f, .12f, .17f, .18f, .15f, .12f, .09f, .055f, .02f },
            new[] { .028f, .10f, .15f, .15f, .12f, .09f, .055f, .02f },
            new[] { .03f, .11f, .16f, .17f, .14f, .11f, .08f, .05f, .02f },
        };
        // Each curl: (turned about the breath in degrees, size, how late it leaves, how fast it turns).
        private static readonly Vector4[] CkCurlGo = { new Vector4(15f, .60f, 0f, 46f), new Vector4(138f, .46f, .10f, -62f), new Vector4(252f, .52f, .20f, 38f) };
        // The flakes carried on it: (across, up, how far it goes, size), and how fast each turns.
        private static readonly Vector4[] CkFlakeGo =
        {
            new Vector4(.16f, .12f, .95f, .12f), new Vector4(-.20f, .05f, .80f, .095f), new Vector4(.05f, -.14f, 1.05f, .08f), new Vector4(-.10f, .22f, .70f, .13f),
            new Vector4(.26f, -.04f, .62f, .085f), new Vector4(-.28f, -.12f, .88f, .10f), new Vector4(.02f, .30f, .55f, .08f),
        };
        private static readonly float[] CkFlakeSpin = { 70f, -95f, 120f, -60f, 85f, -110f, 75f };
        // The four-pointed glints round a panel's star: (across, up, size, how late), and the two at her hands on the last snap.
        private static readonly Vector4[] CkTwinkle = { new Vector4(-1.55f, 1.05f, .52f, .05f), new Vector4(1.7f, .72f, .36f, .09f), new Vector4(1.25f, -.78f, .30f, .13f), new Vector4(-1.75f, -.52f, .42f, .11f) };
        // Her sky, from the ground up: a height and the colour there. Pale haze on the horizon, deep cold blue overhead.
        private static readonly float[] CkSkyHigh = { 0f, 1.3f, 3.6f, 8f, 16f, 30f };
        private static readonly Color[] CkSkyColour =
        {
            new Color(.84f, .95f, .99f, .94f), new Color(.64f, .86f, .96f, .94f), new Color(.38f, .67f, .89f, .94f),
            new Color(.23f, .47f, .78f, .94f), new Color(.15f, .31f, .64f, .94f), new Color(.11f, .21f, .50f, .94f),
        };
        // The two ridges on her horizon, round the whole ring: a valley, a peak, a valley, a peak. Metres.
        private static readonly float[] CkFarRidge =
        {
            2.2f, 5.6f, 3.0f, 4.4f, 2.4f, 6.6f, 3.4f, 5.0f, 2.0f, 4.2f, 2.8f, 7.2f, 3.6f, 5.4f, 2.6f, 4.6f, 2.2f, 6.0f, 3.2f, 4.8f, 2.4f, 6.8f, 3.0f, 5.2f,
        };
        private static readonly float[] CkNearRidge =
        {
            .9f, 2.6f, 1.2f, 1.9f, .8f, 3.2f, 1.4f, 2.2f, 1.0f, 2.8f, .7f, 1.8f, 1.3f, 3.4f, 1.1f, 2.4f, .9f, 3.0f, 1.2f, 2.0f, .8f, 2.7f, 1.4f, 3.3f, 1.0f, 2.3f,
        };
        // The pines in front of them: (which way round in degrees, how far out, how big).
        private static readonly Vector3[] CkPineAt =
        {
            new Vector3(118f, 12.6f, 1.5f), new Vector3(133f, 12.9f, 1.15f), new Vector3(152f, 12.4f, 1.7f), new Vector3(171f, 12.8f, 1.3f), new Vector3(189f, 12.5f, 1.55f),
            new Vector3(205f, 12.9f, 1.2f), new Vector3(222f, 12.5f, 1.75f), new Vector3(240f, 12.8f, 1.35f), new Vector3(66f, 12.7f, 1.45f), new Vector3(293f, 12.6f, 1.5f),
        };
        // The lights in her sky: the lower edge of each band along its arc, in metres. The pink one is the only pink in her world.
        private static readonly float[] CkAuroraPinkLow = { 6.4f, 5.6f, 5.2f, 5.8f, 6.8f, 7.2f, 6.6f, 5.8f, 5.4f, 6.0f, 7.0f };
        private static readonly float[] CkAuroraIceLow = { 11.0f, 10.2f, 10.6f, 11.6f, 12.0f, 11.2f, 10.4f, 10.8f, 11.8f };

        // ---- THE LOTUS (2026-10-10). The owner pointed at a set of magic effects (a cold orb gathers with a ring round it,
        // a lotus of long pointed crystals blooms in layered rings, pale blue-white with darker blue depths, and
        // shatters into shards and glitter) and at a "wispy explosion" (thin ribbons swept along curves, a mask scrolled
        // along them, eaten by a threshold), and said "see if you can pull off any of those"; then, of the effects kit
        // that came of it: "give me the next effects". Hers is the lotus, and it is on THE STOMP, because that is the
        // one beat where ice FORMS out of nothing at a point she chooses, and the two shots after it (her face, close;
        // the court from high up) are the two a flower at her feet is best seen from:
        //   the charge (the last of the heart, and the wide shot before her foot comes down): a seed crystal turning in
        //     a ball of cold where she will stop, two rings round it, the frost on the court drawn in to it;
        //   the bloom (her foot): three rings of petals, the inner first, each petal shooting up closed, a sixth past its
        //     length and back, and opening outward a little too far and back; frost thrown out low round it;
        //   standing (the blow): lit from inside, the light going out through the rings, a glint running up each petal
        //     on its own beat, cold mist circling low and tearing away, light on the court and on her;
        //   the shatter (her last snap): it draws in and brightens as her hands do, then every petal is eaten from its
        //     tip in four frames and what is left is splinters on arcs, glitter, and a crown of frost torn off upward.
        // Nothing of the staging moved for it: not a time, not a shot, not a pose.
        // ⚠️ NO COMIC POP ON HER (`UltimateFx.Pop`): the flat black shards are another hero's language, and he refused
        // them on Rafi ("oh no, the 2d comic pop doesnt fit rafi's").
        private struct CkPetalRow { public int Kind; public float Turn, Out, Long, Tip, Late, Roll; }
        // Petal by petal: which of the three models (0 slim, 1 full, 2 broad), where round her (degrees), how far out its
        // root is, how long it is (m), how far it is tipped out from upright when open (degrees), how late after her foot
        // it starts, and how much it is turned about its own length. No two neighbours alike.
        // ⚠️ THE FRONT OF THE FLOWER LIES LOW. The inner ring has no petal toward the lens of the close shot (the gap from 307
        // round to 52), and the petals of the two rings outside it are tipped nearly flat there and stand up toward her back:
        // with every petal at one angle the close shot had her standing behind a hedge of ice to the waist (film e1).
        private static readonly CkPetalRow[] CkPetals =
        {
            new CkPetalRow { Kind = 0, Turn = 52f, Out = .40f, Long = .80f, Tip = 27f, Late = .030f, Roll = 8f },
            new CkPetalRow { Kind = 0, Turn = 128f, Out = .42f, Long = .92f, Tip = 25f, Late = .050f, Roll = -6f },
            new CkPetalRow { Kind = 0, Turn = 181f, Out = .38f, Long = 1.04f, Tip = 21f, Late = .040f, Roll = 4f },
            new CkPetalRow { Kind = 0, Turn = 233f, Out = .42f, Long = .94f, Tip = 24f, Late = .060f, Roll = -9f },
            new CkPetalRow { Kind = 0, Turn = 307f, Out = .40f, Long = .82f, Tip = 28f, Late = .035f, Roll = 7f },
            new CkPetalRow { Kind = 1, Turn = 2f, Out = .62f, Long = .90f, Tip = 73f, Late = .110f, Roll = 5f },
            new CkPetalRow { Kind = 1, Turn = 49f, Out = .60f, Long = 1.06f, Tip = 60f, Late = .120f, Roll = -7f },
            new CkPetalRow { Kind = 1, Turn = 101f, Out = .58f, Long = 1.10f, Tip = 46f, Late = .135f, Roll = 6f },
            new CkPetalRow { Kind = 1, Turn = 156f, Out = .60f, Long = 1.22f, Tip = 41f, Late = .115f, Roll = -4f },
            new CkPetalRow { Kind = 1, Turn = 207f, Out = .62f, Long = 1.14f, Tip = 44f, Late = .140f, Roll = 8f },
            new CkPetalRow { Kind = 1, Turn = 259f, Out = .58f, Long = 1.16f, Tip = 47f, Late = .125f, Roll = -6f },
            new CkPetalRow { Kind = 1, Turn = 311f, Out = .60f, Long = 1.02f, Tip = 61f, Late = .130f, Roll = 5f },
            new CkPetalRow { Kind = 2, Turn = 24f, Out = .84f, Long = 1.30f, Tip = 80f, Late = .190f, Roll = -5f },
            new CkPetalRow { Kind = 2, Turn = 66f, Out = .80f, Long = 1.50f, Tip = 70f, Late = .205f, Roll = 6f },
            new CkPetalRow { Kind = 2, Turn = 104f, Out = .82f, Long = 1.42f, Tip = 62f, Late = .215f, Roll = -8f },
            new CkPetalRow { Kind = 2, Turn = 143f, Out = .80f, Long = 1.62f, Tip = 57f, Late = .195f, Roll = 4f },
            new CkPetalRow { Kind = 2, Turn = 182f, Out = .84f, Long = 1.50f, Tip = 60f, Late = .220f, Roll = -6f },
            new CkPetalRow { Kind = 2, Turn = 221f, Out = .80f, Long = 1.60f, Tip = 58f, Late = .200f, Roll = 7f },
            new CkPetalRow { Kind = 2, Turn = 262f, Out = .82f, Long = 1.44f, Tip = 63f, Late = .225f, Roll = -4f },
            new CkPetalRow { Kind = 2, Turn = 300f, Out = .80f, Long = 1.48f, Tip = 71f, Late = .210f, Roll = 6f },
            new CkPetalRow { Kind = 2, Turn = 340f, Out = .84f, Long = 1.28f, Tip = 80f, Late = .230f, Roll = -7f },
        };
        private static readonly string[] CkPetalModel = { "lotuspetal_a", "lotuspetal_b", "lotuspetal_c" };
        // How much fatter than its model a petal is drawn: at its true width a 0.8 m petal is a splinter from the wide shot.
        private const float CkPetalWide = 1.25f;
        // Where the cold gathers before her foot comes down: just in front of where she stops, so she is not stood in it.
        private static readonly Vector3 CkSeedAt = new Vector3(0f, .30f, .34f);
        // The charge starts as she comes off the heart's last sweep, and the cracks of light before the shatter as the
        // lens goes high. Neither is a time of the staging: nothing of hers moves on them.
        private const float CkChargeFor = .62f, CkCrackFor = .34f;
        // The frost drawn in to the seed: (from which way round in degrees, how late, how long it starts, how far it winds in).
        private static readonly Vector4[] CkDrawnIn = { new Vector4(18f, 0f, 1.7f, 150f), new Vector4(112f, .07f, 1.4f, 120f), new Vector4(203f, .03f, 1.8f, 165f), new Vector4(291f, .10f, 1.5f, 135f) };
        private static readonly string[] CkDrawnShape = { "whip", "arc", "whip", "hook" };
        // The mist that circles the flower while it stands: (where round her it starts in degrees, how far out, how long
        // it grows to, where in its own cycle it begins), and which way each one's curl is thrown.
        private static readonly Vector4[] CkMist = { new Vector4(35f, 2.5f, 3.0f, 0f), new Vector4(128f, 2.9f, 2.5f, .27f), new Vector4(214f, 2.6f, 3.3f, .55f), new Vector4(305f, 3.0f, 2.7f, .80f) };
        private static readonly string[] CkMistShape = { "whip", "arc", "hook", "whip" };
        // What the flower breaks into: where each splinter leaves from, how it is thrown (m/s), how long it is, and the
        // axis it tumbles about.
        private static readonly Vector3[] CkSplinterFrom =
        {
            new Vector3(.5f, .50f, .6f), new Vector3(-.6f, .45f, .5f), new Vector3(.9f, .35f, -.3f), new Vector3(-.9f, .40f, -.4f),
            new Vector3(.2f, .60f, -.8f), new Vector3(-.3f, .30f, 1.2f), new Vector3(1.5f, .25f, .6f), new Vector3(-1.5f, .25f, .7f),
            new Vector3(1.2f, .30f, -1.2f), new Vector3(-1.1f, .30f, -1.3f), new Vector3(.1f, .70f, .2f), new Vector3(.6f, .25f, 1.6f),
        };
        private static readonly Vector3[] CkSplinterGo =
        {
            new Vector3(3.2f, 7.5f, 4.0f), new Vector3(-4.2f, 6.5f, 3.4f), new Vector3(6.0f, 5.5f, -1.8f), new Vector3(-5.6f, 6.0f, -2.4f),
            new Vector3(1.2f, 8.0f, -4.6f), new Vector3(-1.6f, 5.0f, 6.2f), new Vector3(7.4f, 4.2f, 2.6f), new Vector3(-7.0f, 4.6f, 3.0f),
            new Vector3(5.0f, 5.2f, -5.4f), new Vector3(-4.6f, 5.6f, -5.8f), new Vector3(.8f, 9.5f, 1.4f), new Vector3(2.8f, 4.4f, 7.0f),
        };
        private static readonly Vector3[] CkSplinterSpin =
        {
            new Vector3(1, .2f, .4f), new Vector3(.3f, 1, -.5f), new Vector3(-.6f, .3f, 1), new Vector3(1, -.4f, .2f),
            new Vector3(.2f, .8f, 1), new Vector3(-1, .3f, .5f), new Vector3(.5f, 1, .2f), new Vector3(.4f, -.6f, 1),
            new Vector3(1, .5f, -.3f), new Vector3(-.3f, 1, .6f), new Vector3(.7f, .2f, 1), new Vector3(-1, .6f, .3f),
        };
        private static readonly float[] CkSplinterLong = { .62f, .54f, .70f, .48f, .58f, .66f, .44f, .52f, .60f, .46f, .74f, .50f };
        // The glitter: where each grain leaves from, where it is carried to before it hangs (m), and its size.
        private static readonly Vector3[] CkGlitterFrom =
        {
            new Vector3(.3f, .6f, .4f), new Vector3(-.5f, .5f, .2f), new Vector3(.7f, .4f, -.5f), new Vector3(-.4f, .7f, -.6f), new Vector3(.0f, .9f, .1f),
            new Vector3(1.0f, .3f, .7f), new Vector3(-1.1f, .3f, .8f), new Vector3(.4f, .5f, 1.1f), new Vector3(-.8f, .4f, -1.0f), new Vector3(.9f, .5f, -1.0f),
        };
        private static readonly Vector3[] CkGlitterGo =
        {
            new Vector3(.9f, 1.9f, 1.2f), new Vector3(-1.5f, 1.5f, .7f), new Vector3(1.8f, 1.2f, -1.1f), new Vector3(-1.0f, 2.2f, -1.4f), new Vector3(.1f, 2.6f, .3f),
            new Vector3(2.4f, .9f, 1.5f), new Vector3(-2.5f, 1.0f, 1.6f), new Vector3(.8f, 1.3f, 2.4f), new Vector3(-1.9f, 1.1f, -2.2f), new Vector3(2.0f, 1.4f, -2.1f),
        };
        private static readonly float[] CkGlitterSize = { .20f, .15f, .22f, .16f, .26f, .14f, .18f, .21f, .15f, .19f };
        // ⚠️ THE BLOCK OVER EACH PLAYER LEAVES IN THE LAST FRAMES, AS FROST. Play resumes with them frozen, and what the
        // live Frozen status draws is the shards at their feet and the frost on them, with no block; so a block that
        // stood to the last frame was gone on the next one, and that was a pop at every hand-back. From here to here it
        // thins while two ribbons of frost unwind up off it and are torn away. What is left on the last frame is what
        // play draws on the next. Set both past the end to have the standing block back.
        private const float CkShellFrom = 11.60f, CkShellTo = 11.87f;

        private sealed class CkStatue
        {
            public Other Body;          // null for the can
            // The block of ice over them: the model, the shards round its foot, and what it is drawn with. `Cube` is the
            // plain prism that stands in if the model is missing, and is -1 otherwise.
            public Transform Ice, Rim; public Material Look;
            // What the live game leaves at a frozen player's feet (`CheskaIceVisuals.BuildRestraint`), here already.
            public GameObject Held;
            public int Cube = -1, Fern, Ring, Glow;
            public readonly int[] Chips = new int[4];
            // The two ribbons of frost its block leaves as (`CkShellFrom`).
            public readonly UltimateFx.Piece[] Shed = new UltimateFx.Piece[2];
            public Vector3 Home; public float Height, Facing, FrozenAt = float.MaxValue;
        }

        private sealed class CkSpire
        {
            public int Piece = -1;
            public Transform Model;
            public Vector3 At; public float Height, Width, Lean, Turn, From;
        }

        /// <summary>A ribbon of ice she has skated: the frost either side of it, the ice, the blade's own cut, and the bright new end.</summary>
        private sealed class CkRibbon
        {
            public LineRenderer Bloom, Core, Cut, Fresh;
            public Vector3[] Points;
        }

        private int _ckSky, _ckFarRidge, _ckFarSnow, _ckNearRidge, _ckNearSnow, _ckAuroraPink, _ckAuroraIce, _ckPlates, _ckFloor, _ckBackdrop, _ckBackFloor, _ckBurst, _ckBurstBack;
        private int _ckBurstLine, _ckPanelShadow, _ckStompRing, _ckStompGlow, _ckBreakRing, _ckBreakGlow, _ckFan, _ckSkateGlow, _ckTipFrost, _ckTipGlow;
        private readonly List<int> _ckPines = new List<int>(10), _ckShards = new List<int>(6), _ckMoteGlows = new List<int>(20);
        private readonly List<int> _ckCourtFerns = new List<int>(9), _ckShed = new List<int>(8), _ckSpray = new List<int>(6), _ckCurls = new List<int>(3), _ckCurlGlows = new List<int>(3),
                                   _ckFlakes = new List<int>(7), _ckDust = new List<int>(6), _ckTwinkles = new List<int>(4), _ckSnapGlints = new List<int>(2);
        private readonly List<Transform> _ckPineModels = new List<Transform>(10), _ckPeaks = new List<Transform>(8);
        private readonly Transform[] _ckChunks = new Transform[10];
        private Transform _ckGlacier, _ckChunkRoot;

        private readonly List<CkStatue> _ckStatues = new List<CkStatue>(5);
        // Who she turns to, in order.
        private readonly List<CkStatue> _ckPanels = new List<CkStatue>(3);
        private readonly List<CkSpire> _ckSpires = new List<CkSpire>(40);
        // Which side of her the wide shot is taken from: the side with nobody standing in the lens (`CheskaClearSide`).
        private float _ckWideSide = 1f, _ckSkateSide = 1f;
        // How wide the heart is skated, in metres: as near `CkHeartMost` as leaves her clear of everyone (`CheskaHeartSize`).
        private float _ckHeartSize = CkHeartLeast;
        private CkRibbon _ckTrail, _ckRing;
        // Her copy's root as it was handed over.
        private Vector3 _ckBodyAt; private Quaternion _ckBodyTurn;
        // The copy of her that is held to the frame through her eyes, and on it the arm she points with (the free one when
        // she holds a slipper): its upper arm and forearm, which way along their own X they run, and how they rest.
        private GameObject _ckSelf;
        private Transform _ckUpper, _ckFore;
        private float _ckArmAlong = 1f;
        private Quaternion _ckSelfTurn, _ckUpperRest;
        private Vector3 _ckSelfScale;
        // The lotus: the one look all its ice wears, its petals in the order of `CkPetals`, what it breaks into, the seed
        // it grows from, and the kit's ribbons round it.
        private Material _ckLotusLook;
        private readonly List<CkIce> _ckPetalIce = new List<CkIce>(21), _ckSplinters = new List<CkIce>(12);
        private CkIce _ckSeed;
        private int _ckSeedGlow, _ckSeedRingA, _ckSeedRingB, _ckLotusLight, _ckLotusHer;
        private readonly List<int> _ckGlitter = new List<int>(10);
        private UltimateFx.Piece _ckFrostDisc, _ckBloomCrown, _ckBloomSpiral, _ckBreakCrown;
        private readonly UltimateFx.Piece[] _ckDrawn = new UltimateFx.Piece[4], _ckMistFx = new UltimateFx.Piece[4], _ckCurlFx = new UltimateFx.Piece[3];

        private void BuildCheska()
        {
            BuildCheskaWorld();
            BuildCheskaBlow();
            BuildCheskaLotus();
            // ⚠️ A STEP UNDER WHITE. At (.86, .96, 1) with more light of its own the court was white snow, and the close shot of
            // the blow had a fifth of its frame blown out (film e0: 21.9 per cent of it above 245). Pale blue holds the frost,
            // the ferns and the flower's light, which white cannot.
            _ckFloor = CheskaOrder(Add("FrozenCourt", VfxShapes.Prism(36, .012f, 1f), new Color(.72f, .89f, .99f, .5f), .22f), -6);
            _ckPlates = CheskaOrder(Add("FrostPlates", VfxShapes.Wedges(8, .14f, 9, .05f, .25f, 33), new Color(.78f, .95f, 1, .8f), .5f), -5);
            for (int i = 0; i < CkCourtFern.Length; i++)
                // A step under white, as the court is: from high up the ferns were most of what was blown out (film e3).
                _ckCourtFerns.Add(CheskaOrder(Add("CourtFern" + i, CheskaFrostFern(), new Color(.80f, .93f, 1f, .82f), .38f), -4));
            _ckStompRing = CheskaOrder(Add("StompFrostRing", VfxShapes.Collar(40, .03f, .9f), new Color(.95f, 1f, 1f, .9f), .8f), -4);
            _ckStompGlow = AddGlow("StompCold", new Color(.62f, .92f, 1f, 1f), falloff: 1.8f, core: .3f);
            for (int i = 0; i < 6; i++) _ckShards.Add(CheskaOrder(Add("SnapShard" + i, YasminGatheredCrystal(), CheskaIce, .9f), 3));
            for (int i = 0; i < 2; i++)
            {
                _ckSnapGlints.Add(CheskaOrder(Add("SnapGlint" + i, VfxShapes.TwoSided(VfxShapes.Star(4, .2f, 0)), Color.white, 1f, plain: true), 3));
                RagoUnlitBackdrop(_ckSnapGlints[i]);
            }

            // Everyone else, where they stand, and the can.
            StageOthers(3.0f, 9.5f);
            foreach (var other in _others) _ckStatues.Add(CheskaStatue(other, other.Home, 1.75f));
            var seen = new List<CkStatue>(_ckStatues);
            seen.Sort((a, b) => Mathf.Atan2(a.Home.x, a.Home.z).CompareTo(Mathf.Atan2(b.Home.x, b.Home.z)));
            var can = CheskaStatue(null, _hasCan ? _canAt : new Vector3(0, 0, 4.5f), .62f);
            _ckStatues.Add(can);
            // Three looks. With fewer than three other players the can takes one, then they go round again.
            if (seen.Count < CkLooks.Length) seen.Add(can);
            for (int look = 0; look < CkLooks.Length; look++)
            {
                var caught = seen[look % seen.Count];
                _ckPanels.Add(caught);
                caught.FrozenAt = Mathf.Min(caught.FrozenAt, CkLooks[look] + CkPointIn[look] + .04f);
            }
            // Anyone no snap reached is taken by the stomp.
            foreach (var statue in _ckStatues) if (statue.FrozenAt > CkStomp) statue.FrozenAt = CkStomp + .08f;

            // A panel is ONE flat colour, so its pieces are unlit (the ghost material glows them to white): a sheet behind
            // the player, the ground under them, and two stars turning against each other between.
            _ckBackdrop = Add("PanelBackdrop", VfxShapes.TwoSided(GlowQuad()), Color.white, 1f, plain: true);
            _ckBackFloor = Add("PanelGround", VfxShapes.Prism(4, .01f, 1f), Color.white, 1f, plain: true);
            _ckBurstBack = Add("SnapBurstBack", VfxShapes.TwoSided(VfxShapes.Star(9, .42f, 8)), Color.white, 1f, plain: true);
            _ckBurst = Add("SnapBurst", VfxShapes.TwoSided(VfxShapes.Star(14, .34f, 3)), Color.white, 1f, plain: true);
            // Still flat and still graphic, and crisper: a line round the front star, a flat shadow that sits the block on the
            // panel's ground, and four four-pointed glints that pop one after another, two of them her pink.
            _ckBurstLine = Add("SnapBurstLine", VfxShapes.TwoSided(VfxShapes.StarOutline(14, .34f, 3)), Color.white, 1f, plain: true);
            _ckPanelShadow = CheskaOrder(Add("PanelShadow", VfxShapes.Prism(20, .008f, 1f), Color.white, 1f, plain: true), 1);
            for (int i = 0; i < CkTwinkle.Length; i++)
                _ckTwinkles.Add(Add("PanelGlint" + i, VfxShapes.TwoSided(VfxShapes.Star(4, .2f, 0)), Color.white, 1f, plain: true));
            RagoUnlitBackdrop(_ckBackdrop); RagoUnlitBackdrop(_ckBackFloor); RagoUnlitBackdrop(_ckBurstBack); RagoUnlitBackdrop(_ckBurst);
            RagoUnlitBackdrop(_ckBurstLine); RagoUnlitBackdrop(_ckPanelShadow);
            foreach (int glint in _ckTwinkles) RagoUnlitBackdrop(glint);
            _ckWideSide = CheskaClearSide(CkWideShot); _ckSkateSide = CheskaClearSide(0);
            _ckHeartSize = CheskaHeartSize();

            _ckTrail = CheskaLine("SkateRibbon", 56);
            _ckRing = CheskaLine("SkateHeart", 360);
            // Her spray: shavings off the blade while she glides, a soft cold light under it, and the fan she throws up stopping.
            for (int i = 0; i < CkSprayGo.Length; i++) _ckSpray.Add(CheskaOrder(Add("SkateShaving" + i, YasminGatheredCrystal(), new Color(.95f, 1f, 1f, .95f), .9f), 3));
            _ckFan = CheskaOrder(Add("StopSprayFan", VfxShapes.TwoSided(CheskaSprayFan()), new Color(.93f, .99f, 1f, .86f), .8f), 3);
            _ckSkateGlow = AddGlow("SkateCold", new Color(.6f, .92f, 1f, 1f), billboard: false, falloff: 2.2f, core: .35f);
            if (_bodyRoot != null) { _ckBodyAt = _bodyRoot.transform.position; _ckBodyTurn = _bodyRoot.transform.rotation; }
            BuildCheskaHand();

            // The glacier: the crown behind her (a model, each peak its own joint), then the rings, each spike off any body
            // or can it would stand on. What it sheds as it stops and what it breaks into are here too.
            BuildCheskaGlacier();
            // No model: the crown is the plain spires it was staged with.
            if (_ckGlacier == null) foreach (var row in CkCrown)
                _ckSpires.Add(new CkSpire { At = new Vector3(row.x, 0, row.y), Height = row.z, Width = .5f + row.z * .11f, Lean = row.w, Turn = row.x * 31f, From = CkStomp + .04f + Mathf.Abs(row.x) * .012f });
            for (int ring = 0; ring < CkRings.Length; ring++)
            {
                int count = (int)CkRings[ring].y;
                for (int i = 0; i < count; i++)
                {
                    // Uneven round the ring: every spire a little off its place, its own height and lean.
                    float angle = (i + .5f * (ring % 2) + Mathf.Sin(i * 2.7f + ring) * .22f) * Mathf.PI * 2 / count;
                    float reach = CkRings[ring].x + Mathf.Sin(i * 1.9f + ring * 2) * .55f;
                    var at = new Vector3(Mathf.Sin(angle) * reach, 0, Mathf.Cos(angle) * reach);
                    // Not in front of her face, and not through anyone.
                    if (at.z > 0 && Mathf.Abs(at.x) < 1.6f && ring == 0) continue;
                    bool clear = true;
                    foreach (var statue in _ckStatues) if ((statue.Home - at).magnitude < 1.35f) clear = false;
                    if (!clear) continue;
                    float tall = CkRings[ring].z * (.7f + .6f * Mathf.Abs(Mathf.Sin(i * 3.3f + ring)));
                    _ckSpires.Add(new CkSpire { At = at, Height = tall, Width = .3f + tall * .1f, Lean = Mathf.Sin(i * 4.1f) * 11f, Turn = i * 47f, From = CkStomp + .10f + ring * .13f + (i % 3) * .02f });
                }
            }
            // Three spikes are modelled (one leaning, twins, a low cluster) and go round in turn. A spike with no model is the
            // plain spire it was.
            string[] kinds = { "icespike_a", "icespike_b", "icespike_c" };
            for (int i = 0; i < _ckSpires.Count; i++)
            {
                var spire = _ckSpires[i];
                bool crown = _ckGlacier == null && i < CkCrown.Length;
                if (!crown) spire.Model = CheskaSolid(kinds[i % kinds.Length]);
                if (spire.Model == null) spire.Piece = Add("GlacierSpire" + i, VfxShapes.Spire(6, .1f, .2f, 140 + i), new Color(.66f, .92f, 1, .86f), .5f);
            }

            for (int i = 0; i < CkMotes.Length; i++)
                _ckMoteGlows.Add(AddGlow("HangingMote" + i, new Color(.9f, .97f, 1, 1), falloff: 2.4f, core: .7f));
        }

        private int CheskaOrder(int index, int order) { _pieces[index].Renderer.sortingOrder = order; return index; }

        /// <summary>One of her solid ice models (or a pine), dressed as the cast is, under the stage and hidden; null if it is missing.</summary>
        private Transform CheskaSolid(string name)
        {
            var go = UltimateProp.Spawn(name, _root.transform, UltimateProp.IcePalette);
            if (go == null) return null;
            go.SetActive(false);
            return go.transform;
        }

        private static void CheskaSet(Transform model, Vector3 at, Vector3 scale, Quaternion turn, bool shown)
        {
            if (model == null) return;
            if (model.gameObject.activeSelf != shown) model.gameObject.SetActive(shown);
            if (!shown) return;
            model.localPosition = at; model.localScale = scale; model.localRotation = turn;
        }

        private static float CheskaBump(float x, float at, float width) => Mathf.Clamp01(1 - Mathf.Abs(x - at) / width);
        private static float CheskaOut(float u) { u = Mathf.Clamp01(u); return 1 - (1 - u) * (1 - u); }

        /// <summary>
        /// How far up a peak is, <paramref name="age"/> seconds after it starts: it shoots up in three frames, goes a fifth
        /// past its height, drops back under it and settles. That is the weight of it; a peak that only arrives is a prop.
        /// </summary>
        private static float CheskaRise(float age)
        {
            if (age <= 0) return 0;
            if (age < .1f) { float a = age / .1f; return (1 - (1 - a) * (1 - a) * (1 - a)) * (1 + .2f * a * a); }
            float after = age - .1f;
            return 1 + .2f * Mathf.Exp(-after * 9f) * Mathf.Cos(after * 24f);
        }

        private CkStatue CheskaStatue(Other body, Vector3 home, float height)
        {
            var statue = new CkStatue { Body = body, Home = home, Height = height };
            // Its cracked face is turned to her, a little off square.
            statue.Facing = Mathf.Atan2(-home.x, -home.z) * Mathf.Rad2Deg + 9f;
            var block = UltimateProp.SpawnIceBlock(body != null ? "iceblock" : "icecube", _root.transform, out statue.Look);
            if (block != null)
            {
                statue.Ice = block.transform; statue.Rim = PaeteProp.Find(block, "rim");
                foreach (var surface in block.GetComponentsInChildren<Renderer>(true)) surface.sortingOrder = 1;
                block.SetActive(false);
            }
            else statue.Cube = Add("IceBlock", VfxShapes.Prism(4, 1f, .94f, 0, 0, 40 + _ckStatues.Count), new Color(.7f, .93f, 1, .5f), .55f);
            // ⚠️ THE LIVE ICE IS ALREADY UNDER THE BLOCK. Play resumes with them frozen, and what the live Frozen status draws
            // is five shards round the feet and the frost coat (`CheskaIceVisuals.BuildRestraint`, `_FrostAmount`). The same
            // five stand here inside the block from the moment it lands, so that when the cutscene hands back, the ice at
            // their feet is the ice that was there. A missing ice mesh throws in that builder; the cutscene goes on without.
            if (body != null)
            {
                statue.Held = new GameObject("LiveRestraint");
                statue.Held.transform.SetParent(_root.transform, false);
                statue.Held.transform.localPosition = home;
                statue.Held.transform.localRotation = Quaternion.Euler(0, body.Yaw, 0);
                try { CheskaIceVisuals.BuildRestraint(statue.Held.transform); }
                catch (System.InvalidOperationException) { ObjectDestroy(statue.Held); statue.Held = null; }
                if (statue.Held != null) statue.Held.SetActive(false);
            }
            statue.Fern = CheskaOrder(Add("BlockFrostFern", CheskaFrostFern(), new Color(.96f, 1f, 1f, .85f), .7f), 2);
            statue.Ring = CheskaOrder(Add("BlockLandingRing", VfxShapes.Collar(28, .03f, .86f), new Color(.95f, 1f, 1f, .9f), .8f), 2);
            statue.Glow = AddGlow("BlockCold", new Color(.55f, .9f, 1f, 1f), falloff: 2f, core: .3f);
            for (int i = 0; i < statue.Chips.Length; i++)
                statue.Chips[i] = CheskaOrder(Add("BlockChip" + i, YasminGatheredCrystal(), new Color(.9f, .98f, 1f, .95f), .9f), 3);
            for (int k = 0; k < statue.Shed.Length; k++)
            {
                statue.Shed[k] = UltimateFx.Wisp(_root.transform, "spiral", new Color(.56f, .84f, 1f, 1f), new Color(.93f, 1f, 1f, 1f), k == 0 ? UltimateFx.Smoke : UltimateFx.Streak, 3)
                    .With("_Scale", new Vector4(2.0f, 1.5f, 0, 0)).With("_Scroll", new Vector4(2.4f, 0, 0, 0)).With("_Sweep", 1.0f)
                    .With("_Shape", .7f).With("_Band", .36f).With("_Lead", .66f).With("_Glow", .5f);
                statue.Shed[k].Seed = 11f + _ckStatues.Count * 3.1f + k * 1.7f;
            }
            return statue;
        }

        /// <summary>The glacier's model and its joints, the ice its peaks shed as they stop, and what it breaks into.</summary>
        private void BuildCheskaGlacier()
        {
            _ckGlacier = CheskaSolid("glacier");
            if (_ckGlacier != null)
                foreach (string joint in CkPeakName) _ckPeaks.Add(PaeteProp.Find(_ckGlacier.gameObject, joint));
            _ckChunkRoot = CheskaSolid("icechunks");
            if (_ckChunkRoot != null)
                for (int i = 0; i < _ckChunks.Length; i++) _ckChunks[i] = PaeteProp.Find(_ckChunkRoot.gameObject, "chunk-" + i);
            for (int i = 0; i < CkShedFrom.Length; i++)
                _ckShed.Add(CheskaOrder(Add("ShedIce" + i, YasminGatheredCrystal(), new Color(.88f, .98f, 1f, .95f), .9f), 3));
            _ckBreakRing = CheskaOrder(Add("BreakFrostRing", VfxShapes.Collar(44, .03f, .9f), new Color(.95f, 1f, 1f, .9f), .8f), -4);
            _ckBreakGlow = AddGlow("BreakCold", new Color(.7f, .95f, 1f, 1f), falloff: 2.6f, core: .2f);
        }

        /// <summary>
        /// Her world, which arrives with the glacier: a cold sky that is pale on the horizon and deep overhead, two ridges of
        /// mountains one behind the other with snow on their peaks, snowed-on pines in front of them, and two bands of light
        /// in the sky, one of them pink. The mist walls and plain dark spires it was staged with had no depth: one flat grey
        /// ring. The sky and the ridges are flat colour on purpose (they are a painted backdrop); the pines are models.
        /// </summary>
        private void BuildCheskaWorld()
        {
            _ckSky = CheskaOrder(Add("ColdSky", CheskaSky(), Color.white, 1f, plain: true), -20);
            _ckFarRidge = CheskaOrder(Add("FarRidge", CheskaRidge(CkFarRidge, new Color(.36f, .60f, .84f, 1f), new Color(.66f, .85f, .96f, 1f)), Color.white, 1f, plain: true), -19);
            _ckFarSnow = CheskaOrder(Add("FarRidgeSnow", CheskaRidgeSnow(CkFarRidge), Color.white, 1f, plain: true), -18);
            _ckNearRidge = CheskaOrder(Add("NearRidge", CheskaRidge(CkNearRidge, new Color(.16f, .36f, .58f, 1f), new Color(.30f, .56f, .78f, 1f)), Color.white, 1f, plain: true), -17);
            _ckNearSnow = CheskaOrder(Add("NearRidgeSnow", CheskaRidgeSnow(CkNearRidge), Color.white, 1f, plain: true), -16);
            RagoUnlitBackdrop(_ckSky); RagoUnlitBackdrop(_ckFarRidge); RagoUnlitBackdrop(_ckFarSnow); RagoUnlitBackdrop(_ckNearRidge); RagoUnlitBackdrop(_ckNearSnow);
            for (int i = 0; i < CkPineAt.Length; i++)
            {
                var pine = CheskaSolid("frostpine");
                if (pine != null) { _ckPineModels.Add(pine); _ckPines.Add(-1); }
                else { _ckPineModels.Add(null); _ckPines.Add(AddSolid("RidgePine" + i, VfxShapes.Spire(6, .08f, .25f, 120 + i), new Color(.18f, .40f, .52f, 1))); }
            }
            _ckAuroraPink = AddGlow("SkyLightPink", CkPink, CheskaAurora(128f, 236f, CkAuroraPinkLow, 3.0f), billboard: false, band: true, falloff: 1.5f, core: .15f);
            _ckAuroraIce = AddGlow("SkyLightIce", new Color(.55f, .95f, 1f, 1f), CheskaAurora(96f, 204f, CkAuroraIceLow, 2.6f), billboard: false, band: true, falloff: 1.5f, core: .15f);
            foreach (int light in new[] { _ckAuroraPink, _ckAuroraIce })
            {
                var material = _pieces[light].Renderer.sharedMaterial;
                if (material != null && material.HasProperty("_Tips")) material.SetFloat("_Tips", 1.3f);
            }
        }

        /// <summary>
        /// What leaves her fingertip when she blows: a crystal of frost sitting on it, then three curls of cold unwinding
        /// from one point, each with a soft light under it, seven frost ferns turning as they are carried and a little
        /// diamond dust. The first cut was one flat splat. He asked for this beat to end smoothly ("its ending too fast"),
        /// so nothing here pops: it all eases out and thins away.
        /// </summary>
        private void BuildCheskaBlow()
        {
            _ckTipFrost = CheskaOrder(Add("FingertipFrost", YasminGatheredCrystal(), new Color(.92f, .99f, 1f, .95f), .9f), 3);
            _ckTipGlow = AddGlow("FingertipCold", new Color(.7f, .95f, 1f, 1f), falloff: 2.2f, core: .5f);
            for (int i = 0; i < CkCurlLine.Length; i++)
            {
                _ckCurlGlows.Add(AddGlow("ColdCurlLight" + i, new Color(.5f, .86f, 1f, 1f), CheskaCurl(i, 2.1f), billboard: false, band: true, falloff: 1.4f, core: .1f));
                _ckCurls.Add(CheskaOrder(Add("ColdCurl" + i, VfxShapes.TwoSided(CheskaCurl(i, 1f)), new Color(.96f, 1f, 1f, .92f), .9f), 3));
                // With the effects kit the curl is the kit's: the same ribbon, a blue body with a pale leading edge, and it does
                // not thin to nothing, it is torn away from her fingertip outward until only its wound end hangs there.
                if (UltimateFx.Ready)
                {
                    _ckCurlFx[i] = CheskaWear(CheskaCurl(i, 1f), new Color(.44f, .76f, .98f, 1f), new Color(.93f, 1f, 1f, 1f), i == 1 ? UltimateFx.Streak : UltimateFx.Smoke, 3)
                        .With("_Scale", new Vector4(2.4f, 1.2f, 0, 0)).With("_Scroll", new Vector4(1.3f, 0, 0, 0)).With("_Sweep", 1.1f)
                        .With("_Shape", .55f).With("_Band", .34f).With("_Lead", .62f).With("_Hot", new Color(.6f, .94f, 1f, 1f)).With("_Glow", .45f);
                    _ckCurlFx[i].Seed = 2f + i * 2.9f;
                }
            }
            for (int i = 0; i < CkFlakeGo.Length; i++)
                _ckFlakes.Add(CheskaOrder(Add("BlownFern" + i, CheskaFrostFern(), new Color(1f, 1f, 1f, .95f), .9f), 3));
            for (int i = 0; i < 6; i++) _ckDust.Add(AddGlow("DiamondDust" + i, new Color(.9f, .98f, 1f, 1f), falloff: 2.6f, core: .8f));
        }

        // ------------------------------------------------------------------ the shapes of her effects

        /// <summary>
        /// A frost fern lying flat, a unit across its longest arm: six arms from a small plate, each tapering to a point and
        /// carrying three pairs of barbs that shorten toward its tip (`CkFernArm`, `CkFernBarb`). On the court it is the
        /// frost that creeps out from a block; small and turning, it is a flake on her breath.
        /// </summary>
        private static Mesh CheskaFrostFern()
        {
            var vertices = new List<Vector3>(240); var triangles = new List<int>(360);
            void Bar(Vector2 from, Vector2 to, float wide, float narrow)
            {
                var along = (to - from).normalized; var across = new Vector2(-along.y, along.x);
                int n = vertices.Count;
                Vector2 a = from + across * wide, b = from - across * wide, c = to - across * narrow, d = to + across * narrow;
                vertices.Add(new Vector3(a.x, 0, a.y)); vertices.Add(new Vector3(b.x, 0, b.y)); vertices.Add(new Vector3(c.x, 0, c.y)); vertices.Add(new Vector3(d.x, 0, d.y));
                triangles.AddRange(new[] { n, n + 3, n + 2, n, n + 2, n + 1 });
            }
            Vector2 Way(float degrees) => new Vector2(Mathf.Sin(degrees * Mathf.Deg2Rad), Mathf.Cos(degrees * Mathf.Deg2Rad));
            for (int arm = 0; arm < CkFernArm.Length; arm++)
            {
                var way = Way(CkFernArm[arm].x); float reach = CkFernArm[arm].y;
                Bar(way * .05f, way * reach, .042f, .008f);
                foreach (var barb in CkFernBarb)
                {
                    var root = way * (reach * barb.x);
                    Bar(root, root + Way(CkFernArm[arm].x + 56f) * (reach * barb.y), .026f, .005f);
                    Bar(root, root + Way(CkFernArm[arm].x - 56f) * (reach * barb.y * .9f), .026f, .005f);
                }
                // The plate in the middle, one wedge of it per arm.
                var next = Way(CkFernArm[(arm + 1) % CkFernArm.Length].x);
                int m = vertices.Count;
                vertices.Add(Vector3.zero); vertices.Add(new Vector3(way.x, 0, way.y) * .11f); vertices.Add(new Vector3(next.x, 0, next.y) * .11f);
                triangles.AddRange(new[] { m, m + 1, m + 2 });
            }
            var mesh = new Mesh { name = "Cheska frost fern" };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return VfxShapes.TwoSided(mesh);
        }

        /// <summary>
        /// The fan of shaved ice a skate throws up stopping: seven blades leaving one point along +z, each a thin sliver with
        /// a ridge down it that rises as it goes out, the middle ones the longest and highest (`CkFanBlade`). About 1.3 m
        /// to its longest tip. A silhouette, not a cloud.
        /// </summary>
        private static Mesh CheskaSprayFan()
        {
            var vertices = new List<Vector3>(28); var triangles = new List<int>(63);
            foreach (var blade in CkFanBlade)
            {
                float turn = blade.x * Mathf.Deg2Rad;
                var way = new Vector3(Mathf.Sin(turn), 0, Mathf.Cos(turn)); var across = new Vector3(way.z, 0, -way.x);
                int n = vertices.Count;
                vertices.Add(way * .08f + across * blade.w * .5f); vertices.Add(way * .08f - across * blade.w * .5f);
                vertices.Add(way * (blade.y * .5f) + Vector3.up * (blade.z * .8f + .05f));
                vertices.Add(way * blade.y + Vector3.up * blade.z);
                triangles.AddRange(new[] { n, n + 2, n + 3, n + 2, n + 1, n + 3, n, n + 1, n + 2 });
            }
            var mesh = new Mesh { name = "Cheska stop spray" };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// One curl of cold: a flat ribbon in its own plane (x across, y up, facing z) along its typed line, a smooth curve laid
        /// through the points, as wide at each as `CkCurlWide` says times <paramref name="swell"/> (the light under a curl is the
        /// same curl, fatter). It leaves the origin thin, fattens, and winds in on itself to a point.
        /// </summary>
        private static Mesh CheskaCurl(int which, float swell)
        {
            var line = CkCurlLine[which]; var wide = CkCurlWide[which];
            const int per = 6;
            int spans = line.Length - 1, count = spans * per + 1;
            var vertices = new Vector3[count * 2]; var uv = new Vector2[count * 2]; var triangles = new int[(count - 1) * 6];
            Vector2 At(float along)
            {
                int i = Mathf.Min(spans - 1, (int)along); float u = along - i;
                Vector2 p0 = line[Mathf.Max(0, i - 1)], p1 = line[i], p2 = line[i + 1], p3 = line[Mathf.Min(spans, i + 2)];
                return .5f * (2f * p1 + (p2 - p0) * u + (2f * p0 - 5f * p1 + 4f * p2 - p3) * u * u + (3f * p1 - p0 - 3f * p2 + p3) * u * u * u);
            }
            for (int k = 0; k < count; k++)
            {
                float along = k / (float)per;
                int i = Mathf.Min(spans - 1, (int)along);
                var at = At(along); var ahead = (At(Mathf.Min(spans, along + .05f)) - At(Mathf.Max(0, along - .05f))).normalized;
                var across = new Vector2(-ahead.y, ahead.x) * Mathf.Lerp(wide[i], wide[i + 1], along - i) * swell * .5f;
                vertices[k * 2] = at + across; vertices[k * 2 + 1] = at - across;
                uv[k * 2] = new Vector2(k / (float)(count - 1), 0); uv[k * 2 + 1] = new Vector2(k / (float)(count - 1), 1);
                if (k == count - 1) continue;
                int n = k * 2, t = k * 6;
                triangles[t] = n; triangles[t + 1] = n + 2; triangles[t + 2] = n + 1; triangles[t + 3] = n + 2; triangles[t + 4] = n + 3; triangles[t + 5] = n + 1;
            }
            var mesh = new Mesh { name = "Cheska cold curl", vertices = vertices, uv = uv, triangles = triangles };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Her sky: a wall of unit radius seen from inside, closed over the top, coloured row by row from `CkSkyHigh` and
        /// `CkSkyColour`. One flat grey wall had no distance in it; a sky that is pale where it meets the land and deep
        /// above does.
        /// </summary>
        private static Mesh CheskaSky()
        {
            const int sides = 28; int rows = CkSkyHigh.Length;
            var vertices = new List<Vector3>((sides + 1) * rows + 1); var colours = new List<Color>(vertices.Capacity); var triangles = new List<int>(sides * rows * 6);
            for (int i = 0; i <= sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides;
                for (int row = 0; row < rows; row++)
                {
                    vertices.Add(new Vector3(Mathf.Sin(a), CkSkyHigh[row], Mathf.Cos(a))); colours.Add(CkSkyColour[row]);
                    if (i == sides || row == rows - 1) continue;
                    int n = i * rows + row, w = n + rows;
                    triangles.AddRange(new[] { n, n + 1, w, w, n + 1, w + 1 });
                }
            }
            int top = vertices.Count; vertices.Add(Vector3.up * CkSkyHigh[rows - 1]); colours.Add(CkSkyColour[rows - 1]);
            for (int i = 0; i < sides; i++) triangles.AddRange(new[] { top, i * rows + rows - 1, (i + 1) * rows + rows - 1 });
            var mesh = new Mesh { name = "Cheska cold sky" };
            mesh.SetVertices(vertices); mesh.SetColors(colours); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// A ridge of mountains round the whole horizon, of unit radius: a wall whose top edge goes valley, peak, valley, peak
        /// by <paramref name="heights"/> (metres), darker at its foot than along its top so it stands out of the haze.
        /// </summary>
        private static Mesh CheskaRidge(float[] heights, Color foot, Color top)
        {
            int n = heights.Length;
            var vertices = new Vector3[(n + 1) * 2]; var colours = new Color[vertices.Length]; var triangles = new int[n * 6];
            for (int i = 0; i <= n; i++)
            {
                float a = i * Mathf.PI * 2 / n;
                var rim = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                vertices[i * 2] = rim; vertices[i * 2 + 1] = rim + Vector3.up * heights[i % n];
                colours[i * 2] = foot; colours[i * 2 + 1] = top;
                if (i == n) continue;
                int v = i * 2, t = i * 6;
                triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 2; triangles[t + 3] = v + 2; triangles[t + 4] = v + 1; triangles[t + 5] = v + 3;
            }
            var mesh = new Mesh { name = "Cheska ridge", vertices = vertices, colors = colours, triangles = triangles };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>The snow on that ridge: on every peak a white kite from the tip two fifths of the way down each side, its lower point straight under the tip.</summary>
        private static Mesh CheskaRidgeSnow(float[] heights)
        {
            int n = heights.Length;
            var vertices = new List<Vector3>(n * 2); var colours = new List<Color>(n * 2); var triangles = new List<int>(n * 3);
            Vector3 Top(int i) { float a = i * Mathf.PI * 2 / n; return new Vector3(Mathf.Sin(a), heights[(i % n + n) % n], Mathf.Cos(a)); }
            Color white = Color.white, shade = new Color(.84f, .93f, 1f, 1f);
            for (int i = 1; i < n; i += 2)
            {
                Vector3 peak = Top(i), left = Vector3.Lerp(peak, Top(i - 1), .4f), right = Vector3.Lerp(peak, Top(i + 1), .4f), under = peak - Vector3.up * heights[i] * .2f;
                int v = vertices.Count;
                vertices.Add(peak); vertices.Add(left); vertices.Add(under); vertices.Add(right);
                colours.Add(white); colours.Add(shade); colours.Add(white); colours.Add(shade);
                triangles.AddRange(new[] { v, v + 1, v + 2, v, v + 2, v + 3 });
            }
            var mesh = new Mesh { name = "Cheska ridge snow" };
            mesh.SetVertices(vertices); mesh.SetColors(colours); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// A band of light in the sky: part of a wall of unit radius from <paramref name="from"/> to <paramref name="to"/> degrees
        /// round her, its lower edge at the typed heights <paramref name="low"/> (so it waves) and <paramref name="tall"/>
        /// metres of it above that. Its uv runs along it and up it, for the glow to fade at both ends and both edges.
        /// </summary>
        private static Mesh CheskaAurora(float from, float to, float[] low, float tall)
        {
            int n = low.Length;
            var vertices = new Vector3[n * 2]; var uv = new Vector2[n * 2]; var triangles = new int[(n - 1) * 6];
            for (int i = 0; i < n; i++)
            {
                float u = i / (float)(n - 1), a = Mathf.Lerp(from, to, u) * Mathf.Deg2Rad;
                var rim = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                vertices[i * 2] = rim + Vector3.up * low[i]; vertices[i * 2 + 1] = rim + Vector3.up * (low[i] + tall);
                uv[i * 2] = new Vector2(u, 0); uv[i * 2 + 1] = new Vector2(u, 1);
                if (i == n - 1) continue;
                int v = i * 2, t = i * 6;
                triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 2; triangles[t + 3] = v + 2; triangles[t + 4] = v + 1; triangles[t + 5] = v + 3;
            }
            var mesh = new Mesh { name = "Cheska sky light", vertices = vertices, uv = uv, triangles = triangles };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        private CkRibbon CheskaLine(string name, int points)
        {
            var ribbon = new CkRibbon { Points = new Vector3[points] };
            // Widest and faintest first: the frost either side, the ice itself, the blade's cut down the middle of it.
            ribbon.Bloom = Line(name + "Frost", points, .64f, new Color(.66f, .92f, 1, .2f));
            ribbon.Core = Line(name, points, .27f, new Color(.80f, .95f, 1, .72f));
            ribbon.Cut = Line(name + "Cut", points, .065f, new Color(1, 1, 1, .95f));
            // And the newest of it, just behind her skate: brighter and wider, thinning back into the ribbon.
            ribbon.Fresh = Line(name + "Fresh", 12, .4f, new Color(1, 1, 1, .92f));
            ribbon.Fresh.widthCurve = new AnimationCurve(new Keyframe(0, .12f), new Keyframe(1, 1f));
            ribbon.Bloom.sortingOrder = -3; ribbon.Core.sortingOrder = -2; ribbon.Cut.sortingOrder = -1; ribbon.Fresh.sortingOrder = 0;
            return ribbon;
        }

        /// <summary>
        /// <summary>
        /// <summary>
        /// The copy of her that her view holds, and the arm on it. A rig with no elbow under the arm has nothing to bend, and
        /// she then raises no arm.
        /// </summary>
        private void BuildCheskaHand()
        {
            if (_bodyRoot == null) return;
            _ckSelf = MiniatureOf(_bodyRoot, _root.transform, out var surfaces);
            _ckSelf.name = "FirstPersonSelf";
            // How her body is turned in the scene as it is handed over (it faces the scene's own forward), and its size.
            _ckSelfTurn = Quaternion.Inverse(_root.transform.rotation) * _bodyRoot.transform.rotation;
            _ckSelfScale = _bodyRoot.transform.lossyScale;
            foreach (var surface in surfaces)
                if (surface is SkinnedMeshRenderer skin) { skin.forceMatrixRecalculationPerRender = true; skin.updateWhenOffscreen = true; }
            string side = _heldItem != null ? "left" : "right";
            foreach (var bone in _ckSelf.GetComponentsInChildren<Transform>(true))
            {
                // Through her own eyes she has no head to look past.
                if (bone.name == "head") bone.localScale = Vector3.zero;
                if (bone.name == "arm-" + side) _ckUpper = bone;
                if (bone.name == "forearm-" + side) _ckFore = bone;
            }
            if (_ckUpper == null || _ckFore == null || _ckFore.parent != _ckUpper) { _ckUpper = null; _ckFore = null; _ckSelf.SetActive(false); return; }
            // Shoulder to elbow, in the upper arm's own frame, is along +X or -X (the importer mirrors it; `HeroAbilityClips`, THE ELBOWS).
            _ckArmAlong = _ckFore.localPosition.x < 0 ? -1f : 1f;
            _ckUpperRest = _ckUpper.localRotation;
            // ⚠️ ONLY THE ARM (2026-10-09, measured): see `SampleCheskaArm`. Her model is not touched; this is the copy.
            var trunk = _ckUpper.parent;
            foreach (var bone in _ckSelf.GetComponentsInChildren<Transform>(true))
            {
                if (bone == _ckSelf.transform || bone == trunk || bone.IsChildOf(_ckUpper) || trunk.IsChildOf(bone)) continue;
                if (bone.GetComponent<Renderer>() != null) continue;
                bone.localScale = Vector3.zero;
            }
            trunk.localScale *= .001f; _ckUpper.localScale *= 1000f;
            _ckSelf.SetActive(false);
        }

        // A held crystal needs volume. VfxShapes.Crystal is a flat ground fan,
        // which disappeared edge-on in the low gather shot.
        private static Mesh YasminGatheredCrystal()
        {
            var top = Vector3.up;
            var bottom = Vector3.down * .65f;
            var front = Vector3.forward;
            var right = Vector3.right;
            var back = Vector3.back;
            var left = Vector3.left;
            var mesh = new Mesh { name = "Yasmin gathered ice crystal" };
            mesh.vertices = new[] {
                top, front, right, top, right, back,
                top, back, left, top, left, front,
                bottom, right, front, bottom, back, right,
                bottom, left, back, bottom, front, left
            };
            mesh.triangles = new[] { 0,1,2,3,4,5,6,7,8,9,10,11,
                12,13,14,15,16,17,18,19,20,21,22,23 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        private Vector3 CheskaGather => (_heldItem != null ? FreePalm : BothPalms) + new Vector3(0, .1f, .12f);

        /// <summary>
        /// The wide shot stands low in front of her, to one side. The others stand wherever they stood, so the side is chosen:
        /// whichever keeps every body furthest from the lens and from its line to her (film c5 had one a metre from the lens
        /// and the glacier went up behind a block of ice). +1 is the authored side.
        /// </summary>
        private float CheskaClearSide(int index)
        {
            if (_performance == null || _performance.Shots.Count <= index) return 1f;
            var shot = _performance.Shots[index];
            float best = -1f, side = 1f;
            foreach (float tried in new[] { 1f, -1f })
            {
                float clear = float.MaxValue;
                foreach (var eye in new[] { shot.EyeFrom, shot.EyeTo })
                {
                    var lens = new Vector3(eye.x * tried, 0, eye.z);
                    foreach (var statue in _ckStatues)
                    {
                        // Distance from this body to the line from the lens to her.
                        float along = Mathf.Clamp01(Vector3.Dot(statue.Home - lens, -lens) / Mathf.Max(.01f, lens.sqrMagnitude));
                        clear = Mathf.Min(clear, (statue.Home - (lens - lens * along)).magnitude);
                    }
                }
                if (clear > best) { best = clear; side = tried; }
            }
            return side;
        }

        /// <summary>
        /// <summary>
        /// ⚠️ THE HEART IS A ONE-LINE HEART, the kind drawn without lifting the pen. How it got here, all 2026-10-08: he drew a
        /// route in red over the film twice ("the heart is wayy too small, should be larger like 2-3x large"; "follow the route
        /// i gave, and make the heart more natural - the loop looks like an exact circle and breaks the smooth curve"); his
        /// points traced gave every wobble of a mouse line ("now it looks like a child drew it"); a clean mirrored one was
        /// wrong too ("no not symmetric at all"); and then he sent two pictures, "something like these": a single-line heart
        /// on a long waving stroke, and a brush heart with one lobe larger than the other.
        /// So, after the first of those: the line comes IN from the left over a low wave, dips, and rises through the heart's
        /// point; up the right side; over the right lobe, which is the taller and narrower; down into the cleft and round a
        /// TINY teardrop there; over the left lobe, which is the rounder and reaches further out; down the left side; back
        /// through the point, nearly level, crossing itself; on to the right, up over a crest, and OUT rising (the small curl the picture has in its
        /// tail was in and is out again, on his word). The heart leans a little to the left, as theirs do.
        /// Each row is a place on the line and the way and speed it is going through it, as (across, up the frame) in HEART
        /// WIDTHS from the point where the heart's two sides cross; a smooth curve is laid between each pair, so there is no
        /// corner anywhere and no arc that is a formula's. ⚠️ The tails are shorter than the picture's (it is nearly four
        /// hearts wide): the whole line here is 2.7 heart widths across, and that is already most of a court.
        /// </summary>
        private static readonly Vector2[,] CkRoute =
        {
            { new Vector2(-1.25f, .20f), new Vector2(.55f, -.10f) },    // in, off the back of a low wave
            { new Vector2(-.42f, -.10f), new Vector2(.50f, .00f) },     // the dip
            { new Vector2(0f, 0f), new Vector2(.42f, .30f) },           // through the point, rising
            { new Vector2(.40f, .56f), new Vector2(.00f, .50f) },       // the right side, at its widest
            { new Vector2(.18f, .92f), new Vector2(-.38f, .02f) },      // the top of the right lobe
            // ⚠️ The teardrop was 0.08 of a heart across and she whipped round it ("turn on the small heart loop is still too
            // sharp/fast in velocity"). It is twice that now and a quarter of a heart deep, and its bottom is a wide sweep.
            { new Vector2(-.10f, .68f), new Vector2(-.12f, -.34f) },    // down into the cleft
            { new Vector2(-.105f, .40f), new Vector2(.24f, .00f) },     // the bottom of the teardrop
            { new Vector2(-.02f, .58f), new Vector2(-.10f, .30f) },     // and back up across itself
            { new Vector2(-.32f, .84f), new Vector2(-.34f, .06f) },     // the top of the left lobe
            { new Vector2(-.60f, .56f), new Vector2(.00f, -.42f) },     // the left side, at its widest
            { new Vector2(-.34f, .14f), new Vector2(.36f, -.24f) },     // sweeping in to the point
            { new Vector2(0f, 0f), new Vector2(.40f, -.02f) },          // through the point again, nearly level
            { new Vector2(.62f, .20f), new Vector2(.38f, .30f) },       // rising
            // ⚠️ The tail had a small curl in it here, as the picture's does. He: "you can remove the tail curl at the end".
            { new Vector2(1.00f, .44f), new Vector2(.36f, .16f) },      // easing off the rise
            { new Vector2(1.36f, .58f), new Vector2(.30f, .10f) },      // out, where she stands
        };
        private const int CkRouteSteps = 20;
        // WHEN she is at each step of each curve (0 to 1 of the heart's time). ⚠️ NOT by length: at one even speed the line was
        // "too linear.. its not flowwy at all.. speed should slow down and vary, especially on short loops or turns" (owner,
        // 2026-10-08). A skater carries speed down a straight and gives it up to turn, so her speed at each step comes from
        // how tightly the line bends there (`CheskaHeart` builds this once).
        private static float[] _ckRouteClock;

        /// <summary>One curve of the line: from row <paramref name="curve"/> to the next, leaving and arriving the way each row says.</summary>
        private static Vector2 CheskaRoutePoint(int curve, float u)
        {
            Vector2 from = CkRoute[curve, 0], to = CkRoute[curve + 1, 0], leave = from + CkRoute[curve, 1] / 3f, arrive = to - CkRoute[curve + 1, 1] / 3f;
            float v = 1 - u;
            return v * v * v * from + 3 * v * v * u * leave + 3 * v * u * u * arrive + u * u * u * to;
        }

        /// <summary>
        /// Where she is on the route, <paramref name="along"/> 0 to 1 of its TIME (she is slow in its turns), for a heart <paramref name="width"/> metres
        /// wide (scene space; up his frame is in front of her). ⚠️ THE ROUTE ENDS WHERE SHE STANDS, not at the heart: play
        /// resumes from her own place, so the end of the last curve is laid on it and the heart falls to her left. She is
        /// first seen on it at its far left end; the shot before is through her own eyes, so nobody sees her go.
        /// </summary>
        private static Vector3 CheskaHeart(float along, float width)
        {
            int curves = CkRoute.GetLength(0) - 1, count = curves * CkRouteSteps + 1;
            if (_ckRouteClock == null)
            {
                // Every step's place, then how sharply the line turns at it: the angle it turns through over the distance it
                // takes to do it (in heart widths, so 1 is a gentle bend, 4 is round a lobe, and the teardrop is over 15).
                var places = new Vector2[count];
                for (int i = 0; i < count; i++)
                    places[i] = i == 0 ? CheskaRoutePoint(0, 0) : CheskaRoutePoint(Mathf.Min(curves - 1, (i - 1) / CkRouteSteps), ((i - 1) % CkRouteSteps + 1) / (float)CkRouteSteps);
                var pace = new float[count];
                for (int i = 0; i < count; i++)
                {
                    Vector2 back = places[i] - places[Mathf.Max(0, i - 1)], next = places[Mathf.Min(count - 1, i + 1)] - places[i];
                    float span = (back.magnitude + next.magnitude) * .5f;
                    float bend = i == 0 || i == count - 1 || span < 1e-5f ? 0 : Vector2.Angle(back, next) * Mathf.Deg2Rad / span;
                    // Her speed there: all of it down a straight, about a third round a lobe, a tenth in the teardrop and the curl.
                    pace[i] = Mathf.Max(.09f, 1f / (1f + .5f * Mathf.Pow(bend, 1.05f)));
                }
                // She does not brake in one step or leave a turn in one: her speed is eased over the steps either side. Twice.
                for (int pass = 0; pass < 2; pass++)
                {
                    var eased = new float[count];
                    for (int i = 0; i < count; i++)
                    {
                        float sum = 0; int taken = 0;
                        for (int k = -7; k <= 7; k++) if (i + k >= 0 && i + k < count) { sum += pace[i + k]; taken++; }
                        eased[i] = sum / taken;
                    }
                    pace = eased;
                }
                // And she glides to her stop over the last of it.
                for (int i = 0; i < count; i++) pace[i] *= Mathf.Lerp(.3f, 1f, Mathf.Clamp01((count - 1 - i) / (count * .07f)));
                var clock = new float[count];
                for (int i = 1; i < count; i++) clock[i] = clock[i - 1] + (places[i] - places[i - 1]).magnitude / ((pace[i] + pace[i - 1]) * .5f);
                for (int i = 1; i < count; i++) clock[i] /= clock[count - 1];
                _ckRouteClock = clock;
            }
            along = Mathf.Clamp01(along);
            int step = 1;
            while (step < count - 1 && _ckRouteClock[step] < along) step++;
            float within = Mathf.InverseLerp(_ckRouteClock[step - 1], _ckRouteClock[step], along);
            int curve = Mathf.Min(curves - 1, (step - 1) / CkRouteSteps);
            var on = CheskaRoutePoint(curve, ((step - 1) % CkRouteSteps + within) / CkRouteSteps) - CkRoute[curves, 0];
            return new Vector3(on.x, 0, on.y) * width;
        }

        /// <summary>
        /// How wide the heart is. He asked for two to three times the first (2.6 m), twice ("wayy too small"). ⚠️ IT IS NOW
        /// ALWAYS 4.4 m (1.7 times; the whole line is then 12 m across), WHOEVER IS IN THE WAY: `CkHeartLeast` equals `CkHeartMost`, so the search below never runs. With
        /// a smaller least it shrank the heart to keep her clear of the others, and with three people and a can on a court
        /// that was nearly always, back to the size he had refused (film c19). So she may pass through somebody's block of
        /// ice. Lower `CkHeartLeast` to have the clearance back.
        /// </summary>
        private float CheskaHeartSize()
        {
            for (float width = CkHeartMost; width > CkHeartLeast + .01f; width -= .2f)
            {
                bool clear = true;
                for (int i = 0; i <= 120 && clear; i++)
                {
                    var at = CheskaHeart(i / 120f, width);
                    foreach (var statue in _ckStatues)
                        if ((statue.Home - at).magnitude < (statue.Body != null ? .95f : .5f)) { clear = false; break; }
                }
                if (clear) return width;
            }
            return CkHeartLeast;
        }

        /// <summary>Where she is at this moment (scene space, her feet): on the loop she opens with, on the heart, or where she stands.</summary>
        private Vector3 CheskaSkate(float t)
        {
            if (t > CkCircleFrom && t < CkCircleTo)
            {
                float u = Mathf.InverseLerp(CkCircleFrom, CkCircleTo, t);
                // She comes in already moving and bleeds her speed only at the very end, where she stands.
                // Her pace along it is the line's own (`_ckRouteClock`): quick down its sweeps, slow round its turns.
                return CheskaHeart(u, _ckHeartSize);
            }
            int last = CkWayAt.Length - 1;
            if (t <= CkWayAt[0] || t >= CkWayAt[last]) return Vector3.zero;
            int i = 0;
            while (i < last - 1 && t >= CkWayAt[i + 1]) i++;
            float along = Mathf.InverseLerp(CkWayAt[i], CkWayAt[i + 1], t);
            // She gathers speed out of the push and bleeds it coming home.
            if (i == 0) along *= along; else if (i == last - 1) along = 1 - (1 - along) * (1 - along);
            Vector3 p0 = CkWay[Mathf.Max(0, i - 1)], p1 = CkWay[i], p2 = CkWay[i + 1], p3 = CkWay[Mathf.Min(last, i + 2)];
            // Catmull-Rom through the points: a loop, never a corner.
            return .5f * (2f * p1 + (p2 - p0) * along + (2f * p0 - 5f * p1 + 4f * p2 - p3) * along * along + (3f * p1 - p0 - 3f * p2 + p3) * along * along * along);
        }

        private Vector3 CheskaHeading(float t)
        {
            var way = CheskaSkate(t + .04f) - CheskaSkate(t - .04f); way.y = 0;
            return way.sqrMagnitude > 1e-5f ? way.normalized : Vector3.forward;
        }

        /// <summary>Which of the three she is looking at now, or -1.</summary>
        private static int CheskaLookAt(float t)
        {
            for (int look = CkLooks.Length - 1; look >= 0; look--) if (t >= CkLooks[look]) return t < CkLooks[look] + CkLookFor[look] ? look : -1;
            return -1;
        }

        private Vector3 CheskaHead => _head != null ? _root.transform.InverseTransformPoint(_head.position) : new Vector3(0, 1.3f, 0);

        /// <summary>The middle of whoever the look is on.</summary>
        private Vector3 CheskaTarget(int which) => _ckPanels[which].Home + Vector3.up * _ckPanels[which].Height * .56f;

        /// <summary>How far the turn to this look has gone (0 to 1, and a hair past for the whips), by its own clock.</summary>
        private static float CheskaTurned(int which, float since)
        {
            float turn = which == 0 ? .34f : .17f;
            float turned = Mathf.Clamp01(since / turn);
            turned = which == 0 ? turned * turned * (3 - 2 * turned) : 1 - (1 - turned) * (1 - turned) * (1 - turned);
            float past = which == 0 ? 0 : .05f * Mathf.Sin(Mathf.Clamp01(since / (turn * 1.7f)) * Mathf.PI) * (since > turn * .9f ? 1 : 0);
            return turned + past;
        }

        /// <summary>
        /// Shots 1 to 3 are THROUGH HER EYES: the view turns from the last one she looked at to the next, then zooms in until
        /// they fill the frame, and kicks as she points. The first is unhurried; the others are quick. The wide shot is taken
        /// from the side nobody stands on.
        /// </summary>
        private void CheskaFrame(int index, float t, ref Vector3 eye, ref Vector3 look, ref float fov)
        {
            if (index == CkWideShot) { eye.x *= _ckWideSide; look.x *= _ckWideSide; return; }
            // The skate is watched from in front and to one side too, and the same choice is made for it (film c11 watched
            // the whole loop from behind somebody's hat).
            if (index == 0) { eye.x *= _ckSkateSide; look.x *= _ckSkateSide; return; }
            // His route is shot from straight above the middle of all of it (the heart and both tails), from as high as holds
            // it, the frame square to her so the heart stands upright in it.
            if (index == CkWideShot - 1)
            {
                float width = _ckHeartSize, drift = Ease(CkCircleFrom, CkCircleTo, t);
                var middle = new Vector3(-1.305f * width, 0, -.17f * width);
                look = middle;
                // ⚠️ Nearly straight down, leaning a fixed tenth toward her side of the court and NEVER sideways. Looking exactly
                // down, the frame has no up of its own and whichever way the lens drifted became "up": the heart came out
                // lying on its side and turning (film c20). The lean keeps the lobes up the frame, as he drew them.
                float high = width * Mathf.Lerp(2.3f, 2.2f, drift) + 1f;
                eye = middle + new Vector3(0, high, -.1f * high);
                fov = 46f;
                return;
            }
            int which = index - CkFirstLook;
            if (which < 0 || which >= _ckPanels.Count) return;
            var caught = _ckPanels[which];
            float since = t - CkLooks[which];
            var at = CheskaTarget(which);
            var from = which == 0 ? CheskaHead + Vector3.forward * 6f : CheskaTarget(which - 1);
            // The turn: a slow, easy one to the first of them, a whip to the others that lands a hair past and comes back.
            float turn = which == 0 ? .34f : .17f, pointed = CkPointIn[which];
            look = Vector3.LerpUnclamped(from, at, CheskaTurned(which, since));
            // The lens is her eyes. Her body is not drawn while it is (her own goggles and hat would stand across the view).
            eye = CheskaHead + new Vector3(0, .1f, .12f);
            // The zoom: until they are about 3.4 m of frame tall, wherever they are; done just before she points.
            float tight = Mathf.Clamp(2f * Mathf.Atan(1.7f / Mathf.Max(1f, (at - eye).magnitude)) * Mathf.Rad2Deg, 12f, 42f);
            fov = Mathf.Lerp(64f, tight, Ease(turn, pointed - .14f, since)) * (1 - .1f * Flash(since, pointed + .05f, .09f));
        }

        /// <summary>Each block of ice landing, the stomp, and the glacier bursting.</summary>
        private Vector3 CheskaShake(float t)
        {
            float size = DanteKick(t, CkStomp, .15f, .55f) + DanteKick(t, CkSnap, .06f, .25f);
            // Small ones: the lens is zoomed in when these land, and a shake is that much bigger through it.
            for (int look = 0; look < CkLooks.Length; look++) size += DanteKick(t, CkLooks[look] + CkPointIn[look] + .1f, .02f, .18f);
            return new Vector3(Mathf.Sin(t * 67f), Mathf.Sin(t * 89f + 1.1f), 0) * size;
        }

        private void SampleCheska(float t)
        {
            float leave = 1 - Ease(Seconds - .45f, Seconds - .05f, t);
            SampleCheskaSkate(t, leave);
            int looking = CheskaLookAt(t);
            // Through her own eyes she does not see her own head. `enabled`, not `forceRenderingOff`: the phase sets that one
            // itself on every capture.
            foreach (var surface in _bodyRenderers) if (surface != null) surface.enabled = looking < 0;
            if (_heldRenderers != null) foreach (var surface in _heldRenderers) if (surface != null) surface.enabled = looking < 0;
            if (looking < 0 && _ckSelf != null) _ckSelf.SetActive(false);

            SampleCheskaBlow(t, leave);

            // Her world arrives with the glacier: the cold sky, the ridges and their pines, the frozen ground.
            float world = Ease(CkStomp, CkStomp + .22f, t) * leave;
            SampleCheskaWorld(t, world);
            float sinceStomp = t - CkStomp;
            Place(_ckFloor, Vector3.up * .035f, Vector3.one * 13.5f * Ease(CkStomp, CkStomp + .4f, t) + Vector3.up, Quaternion.identity, world);
            Place(_ckPlates, Vector3.up * .05f, Vector3.one * Mathf.Lerp(.4f, 2.6f, Ease(CkStomp, CkStomp + .18f, t)), Quaternion.Euler(0, 15, 0), world);
            // The frost goes out from her foot as a ring, and the ferns come up behind it as it passes each of them: the
            // one under her foot first, a little past its size and back.
            float ringOut = CheskaOut(sinceStomp / .5f);
            Place(_ckStompRing, Vector3.up * .08f, new Vector3(Mathf.Lerp(.6f, 13f, ringOut), 1, Mathf.Lerp(.6f, 13f, ringOut)), Quaternion.identity, sinceStomp > 0 ? (1 - ringOut) * world : 0);
            PlaceGlow(_ckStompGlow, Vector3.up * .3f, Vector3.one * 5.5f, Quaternion.identity, .7f * Flash(t, CkStomp + .05f, .16f));
            for (int i = 0; i < _ckCourtFerns.Count; i++)
            {
                var row = CkCourtFern[i];
                float since = sinceStomp - new Vector2(row.x, row.y).magnitude * .04f;
                float grown = CheskaOut(since / .3f) * (1 + .12f * CheskaBump(since, .3f, .14f)) * row.z;
                Place(_ckCourtFerns[i], new Vector3(row.x, .06f, row.y), new Vector3(grown, 1, grown), Quaternion.Euler(0, row.w, 0), since > 0 ? world : 0);
            }

            // Which of them the frame has gone graphic behind, if it has: the panel is one flat colour, and the frost and
            // the cold light at anybody else's feet would be drawn across it.
            CkStatue onPanel = null;
            if (looking >= 0 && t - CkLooks[looking] - CkPointIn[looking] >= 0) onPanel = _ckPanels[looking];

            // Everyone else: loose until their snap, then a block of ice comes down over them and they are done.
            foreach (var statue in _ckStatues)
            {
                float age = t - statue.FrozenAt; bool caught = age >= 0;
                if (statue.Body != null)
                {
                    float sway = caught || _reducedEffects ? 0 : Mathf.Sin(t * 3.1f + statue.Body.Reach) * 3.5f;
                    statue.Body.Holder.transform.localRotation = Quaternion.Euler(0, statue.Body.Yaw + sway, sway * .5f);
                    // ⚠️ NOT FADED AT THE END: play resumes with them frozen, so they are frozen on the last frame.
                    FrostOther(statue.Body.Renderers, caught ? 1 : 0);
                }
                // Down in two frames, faster as it falls; flat where it lands; then it springs a little too tall and settles.
                float fallen = Mathf.Clamp01(age / .07f), stood = Mathf.Max(0, age - .07f);
                float tall = caught ? 1 - .28f * Mathf.Exp(-stood * 13f) * Mathf.Cos(stood * 24f) : 1, wide = 1 + (1 - tall) * .7f;
                var at = statue.Home + Vector3.up * (1 - fallen * fallen) * 3.2f;
                if (statue.Ice != null)
                {
                    CheskaSet(statue.Ice, at, new Vector3(wide, tall, wide), Quaternion.Euler(0, statue.Facing, 0), caught);
                    // The shards round its foot are thrown up by the landing: a frame after it, past their size and back.
                    if (statue.Rim != null) statue.Rim.localScale = Vector3.one * Mathf.Max(.001f, CheskaOut((age - .06f) / .09f) * (1 + .3f * CheskaBump(age, .17f, .09f)));
                }
                else
                    Place(statue.Cube, at, new Vector3(.74f * wide, statue.Height * 1.14f * tall, .74f * wide), Quaternion.Euler(0, 45 + statue.Home.x * 17, 0), caught ? 1 : 0);
                // The block leaves as frost in the last frames (`CkShellFrom`): it thins, and two ribbons unwind up off it.
                float shell = Ease(CkShellFrom, CkShellTo, t);
                if (statue.Look != null) statue.Look.color = new Color(1, 1, 1, caught ? 1 - shell : 1);
                for (int k = 0; k < statue.Shed.Length; k++)
                {
                    float off = t - CkShellFrom - k * .03f, gone = off / (CkShellTo - CkShellFrom + .02f);
                    float girth = (statue.Body != null ? 1.05f : .8f) * (1 + .35f * CheskaOut(gone)), lifted = statue.Height * 1.05f * (.5f + .6f * CheskaOut(gone / .6f));
                    UltimateFx.Place(statue.Shed[k], statue.Home + Vector3.up * .05f, Quaternion.Euler(0, statue.Facing + k * 180f + off * 160f, 0), new Vector3(girth, lifted, girth));
                    UltimateFx.Set(statue.Shed[k], caught ? off : -1f, Mathf.Lerp(.12f, 1f, Ease(.25f, 1f, gone)));
                }
                if (statue.Held != null && statue.Held.activeSelf != age > .09f) statue.Held.SetActive(age > .09f);

                // Where it landed: a ring of frost knocked out, chips kicked up, a fern of frost that keeps creeping out
                // from under it for as long as it stands, and the cold coming off it.
                float landed = age - .07f; bool drawn = landed >= 0 && (onPanel == null || onPanel == statue);
                float knocked = CheskaOut(landed / .34f), reach = Mathf.Lerp(.5f, 2.1f, knocked);
                Place(statue.Ring, statue.Home + Vector3.up * .1f, new Vector3(reach, 1, reach), Quaternion.identity, drawn ? 1 - knocked : 0);
                float crept = (.55f * CheskaOut(landed / .18f) + .45f * Mathf.Clamp01(landed / 3.5f)) * 2.0f;
                Place(statue.Fern, statue.Home + Vector3.up * .095f, new Vector3(crept, 1, crept), Quaternion.Euler(0, statue.Home.x * 40f, 0), drawn ? 1 : 0);
                PlaceGlow(statue.Glow, statue.Home + Vector3.up * .3f, Vector3.one * (2.4f + 1.6f * Flash(t, statue.FrozenAt + .1f, .14f)), Quaternion.identity,
                    drawn ? .2f + .55f * Flash(t, statue.FrozenAt + .1f, .14f) : 0);
                for (int i = 0; i < statue.Chips.Length; i++)
                {
                    float u = Mathf.Clamp01(landed / .4f);
                    var go = Quaternion.Euler(0, statue.Facing, 0) * CkChipGo[i];
                    var chip = statue.Home + new Vector3(go.x, 0, go.z) * (.6f + 1.1f * u) + Vector3.up * (.15f + go.y * 4 * u * (1 - u));
                    Place(statue.Chips[i], chip, Vector3.one * .1f * (1 - u * .6f), Quaternion.Euler(u * 300f + i * 70, i * 50, u * 220f), drawn && u < 1 ? 1 - u * u : 0);
                }
            }

            // Through her eyes: the hand, and on the snap the frame goes graphic behind whoever she is looking at.
            bool graphic = false;
            if (looking >= 0 && _performance != null && ShotCount > CkFirstLook + looking)
            {
                var caught = _ckPanels[looking];
                _performance.Shot(CkFirstLook + looking, t, out var eye, out var look, out float fov);
                CheskaFrame(CkFirstLook + looking, t, ref eye, ref look, ref fov);
                float since = t - CkLooks[looking], snapped = since - CkPointIn[looking];
                SampleCheskaArm(eye, look, fov, since, looking);
                if (snapped >= 0)
                {
                    graphic = true;
                    var middle = caught.Home + Vector3.up * caught.Height * .56f;
                    var forward = (middle - eye).normalized; var facing = Quaternion.LookRotation(forward, Vector3.up);
                    var across = facing * Vector3.right; var above = facing * Vector3.up;
                    float pop = Mathf.Clamp01(snapped / .07f) * (1 + .35f * Mathf.Clamp01(1 - Mathf.Abs(snapped - .09f) / .06f));
                    // On the pale panel the front star is the dark one, and everything laid on it swaps with it.
                    bool pale = looking == 1;
                    Color dark = new Color(.16f, .40f, .62f, 1), front = pale ? dark : Color.white;
                    Place(_ckBackdrop, middle + forward * 2.8f, new Vector3(40f, 24f, 1), facing, 1); Tint(_ckBackdrop, 1, CkPanelColour[looking]);
                    Place(_ckBackFloor, caught.Home + Vector3.up * .07f, new Vector3(30f, 1, 30f), Quaternion.identity, 1); Tint(_ckBackFloor, 1, CkPanelColour[looking]);
                    Place(_ckBurstBack, middle + forward * 1.3f, Vector3.one * 3.5f * pop, facing * Quaternion.Euler(-90, -snapped * 80f, 0), 1); Tint(_ckBurstBack, 1, CkPanelStar[looking]);
                    Place(_ckBurst, middle + forward * 1.0f, Vector3.one * 2.4f * pop, facing * Quaternion.Euler(-90, snapped * 100f, 0), 1);
                    Tint(_ckBurst, 1, front);
                    // The line round the front star, a hand outside it and turning with it.
                    Place(_ckBurstLine, middle + forward * .97f, Vector3.one * 2.4f * 1.13f * pop, facing * Quaternion.Euler(-90, snapped * 100f, 0), 1);
                    Tint(_ckBurstLine, 1, pale ? CkPanelStar[looking] : dark);
                    // The block's flat shadow on the panel's ground: the panel's own colour, a third darker.
                    float under = caught.Body != null ? 1.05f : .95f;
                    Place(_ckPanelShadow, caught.Home + Vector3.up * .078f, new Vector3(under, 1, under) * CheskaOut(snapped / .09f), Quaternion.identity, 1);
                    Tint(_ckPanelShadow, 1, Color.Lerp(CkPanelColour[looking], new Color(.05f, .16f, .32f, 1), .32f));
                    for (int i = 0; i < _ckTwinkles.Count; i++)
                    {
                        var row = CkTwinkle[i];
                        float lit = snapped - row.w;
                        float size = row.z * CheskaOut(lit / .07f) * (1 + .6f * CheskaBump(lit, .08f, .06f)) * (1 + (_reducedEffects ? 0 : .12f * Mathf.Sin(lit * 13f + i * 2f)));
                        Place(_ckTwinkles[i], middle + forward * .9f + across * row.x + above * row.y, Vector3.one * size,
                            facing * Quaternion.Euler(-90, 45f + lit * (i % 2 == 0 ? 60f : -80f), 0), lit > 0 ? 1 : 0);
                        Tint(_ckTwinkles[i], lit > 0 ? 1 : 0, i % 2 == 0 ? CkPink : front);
                    }
                }
            }
            if (!graphic)
            {
                Tint(_ckBackdrop, 0); Tint(_ckBackFloor, 0); Tint(_ckBurstBack, 0); Tint(_ckBurst, 0); Tint(_ckBurstLine, 0); Tint(_ckPanelShadow, 0);
                foreach (int glint in _ckTwinkles) Tint(glint, 0);
            }

            SampleCheskaGlacier(t, leave, world);
            SampleCheskaLotus(t, leave, world);

            // Shards from her hands as they snap apart, and one pink glint at each hand: the cheek of it.
            Vector3 gather = CheskaGather;
            for (int i = 0; i < _ckShards.Count; i++)
            {
                float age = t - CkSnap, u = Mathf.Clamp01(age / .3f);
                float angle = i * Mathf.PI * 2 / _ckShards.Count;
                var at = gather + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) * .7f, .3f) * u * .9f;
                Place(_ckShards[i], at, Vector3.one * .06f * (1 - u * .5f), Quaternion.Euler(angle * 57, 0, 90), age >= 0 ? (1 - u) * leave : 0);
            }
            for (int i = 0; i < _ckSnapGlints.Count; i++)
            {
                float age = t - CkSnap, u = Mathf.Clamp01(age / .24f);
                float size = .34f * CheskaOut(age / .06f) * (1 - u * u);
                Place(_ckSnapGlints[i], gather + new Vector3(i == 0 ? -.42f : .42f, .12f, .1f) * CheskaOut(age / .1f), Vector3.one * size, Quaternion.Euler(-90, 45f + age * 200f, 0), age >= 0 && u < 1 ? 1 : 0);
                Tint(_ckSnapGlints[i], age >= 0 && u < 1 ? 1 : 0, i == 0 ? CkPink : Color.white);
            }
        }

        /// <summary>Her sky, her ridges and their pines, the lights in the sky and the dust that hangs in her air.</summary>
        private void SampleCheskaWorld(float t, float world)
        {
            Place(_ckSky, Vector3.zero, new Vector3(14.6f, 1, 14.6f), Quaternion.identity, world);
            Place(_ckFarRidge, Vector3.zero, new Vector3(14.4f, 1, 14.4f), Quaternion.identity, world);
            Place(_ckFarSnow, Vector3.zero, new Vector3(14.38f, 1, 14.38f), Quaternion.identity, world);
            Place(_ckNearRidge, Vector3.zero, new Vector3(13.9f, 1, 13.9f), Quaternion.Euler(0, 23, 0), world);
            Place(_ckNearSnow, Vector3.zero, new Vector3(13.88f, 1, 13.88f), Quaternion.Euler(0, 23, 0), world);
            for (int i = 0; i < CkPineAt.Length; i++)
            {
                float angle = CkPineAt[i].x * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * CkPineAt[i].y;
                if (_ckPineModels[i] != null) CheskaSet(_ckPineModels[i], at, Vector3.one * CkPineAt[i].z, Quaternion.Euler(0, i * 53f, 0), world > .01f);
                else Place(_ckPines[i], at, new Vector3(.9f, 3.4f + (i * 41 % 5) * .5f, .9f), Quaternion.identity, world > .01f ? 1 : 0);
            }
            // The lights sway a few degrees, slowly, and breathe; held still for reduced effects.
            float sway = _reducedEffects ? 0 : t;
            PlaceGlow(_ckAuroraPink, Vector3.zero, new Vector3(13.6f, 1, 13.6f), Quaternion.Euler(0, Mathf.Sin(sway * .9f) * 4f, 0), world * (.5f + .12f * Mathf.Sin(sway * 2.1f)));
            PlaceGlow(_ckAuroraIce, Vector3.zero, new Vector3(13.7f, 1, 13.7f), Quaternion.Euler(0, Mathf.Sin(sway * .7f + 1f) * -5f, 0), world * (.42f + .1f * Mathf.Sin(sway * 1.7f + 2f)));
            for (int i = 0; i < _ckMoteGlows.Count; i++)
                PlaceGlow(_ckMoteGlows[i], CkMotes[i] + Vector3.up * (Flash(t, CkSnap + .2f, .5f) * .5f + (_reducedEffects ? 0 : .08f * Mathf.Sin(t * 1.3f + i))), Vector3.one * (.09f + (i % 3) * .03f) * (1 + 2.5f * Flash(t, CkSnap + .12f, .3f)),
                    Quaternion.identity, world * .9f);
        }

        /// <summary>
        /// The glacier: every peak up on its own beat with a kick past its height and a settle (`CheskaRise`), fat at the
        /// foot as it comes and lean as it overshoots; ice shed from the caps as they stop; standing; and on her last snap
        /// it does not sink, it BREAKS: the peaks go to nothing in three frames while ten chunks are thrown out of where
        /// they stood, tumbling, and a ring of frost runs out along the ground.
        /// </summary>
        private void SampleCheskaGlacier(float t, float leave, float world)
        {
            float burst = Ease(CkSnap, CkSnap + .14f, t), broken = t - CkSnap;
            // Before it breaks it draws in, as her hands do before they snap apart; on the snap it swells for two frames and
            // is gone, and the chunks are what is left of it.
            float drawn = Ease(CkSnap - .16f, CkSnap - .02f, t), swell = CheskaOut(broken / .05f);
            if (_ckGlacier != null)
            {
                bool standing = t >= CkStomp + .02f && broken < .05f && leave > .01f;
                CheskaSet(_ckGlacier, CkGlacierAt, Vector3.one * (1 - .035f * drawn + .1f * swell), Quaternion.identity, standing);
                if (standing)
                    for (int i = 0; i < _ckPeaks.Count; i++)
                    {
                        if (_ckPeaks[i] == null) continue;
                        float age = t - CkStomp - CkPeakFrom[i], up = CheskaRise(age);
                        // Fat at the foot as it comes, lean as it overshoots; and it rocks as it settles, each peak its own way.
                        float thick = Mathf.Lerp(.5f, 1f, CheskaOut(age / .1f)) * (1.06f - .3f * (Mathf.Clamp(up, .9f, 1.2f) - 1f));
                        float rock = _reducedEffects ? 0 : 3.2f * Mathf.Exp(-Mathf.Max(0, age - .1f) * 6f) * Mathf.Sin(Mathf.Max(0, age - .1f) * 19f + i);
                        _ckPeaks[i].localScale = new Vector3(thick, Mathf.Max(.001f, up), thick);
                        _ckPeaks[i].localRotation = Quaternion.Euler(rock * .5f, 0, i % 2 == 0 ? rock : -rock);
                    }
            }
            foreach (var spire in _ckSpires)
            {
                float age = t - spire.From, up = CheskaRise(age);
                var size = new Vector3(spire.Width * (1 + .25f * burst), Mathf.Max(.01f, spire.Height * up * (1 - burst)), spire.Width * (1 + .25f * burst));
                var turn = Quaternion.Euler(spire.Lean * .4f, spire.Turn, spire.Lean);
                if (spire.Model != null) CheskaSet(spire.Model, spire.At, size, turn, age >= 0 && burst < .99f && leave > .01f);
                else Place(spire.Piece, spire.At, size, turn, age >= 0 && burst < .99f ? leave : 0);
            }

            // Shed as the peaks stop: a shard knocked off near each cap, falling as it turns.
            for (int i = 0; i < _ckShed.Count; i++)
            {
                float age = t - (CkStomp + .17f + i * .025f), u = Mathf.Clamp01(age / .75f);
                var at = CkShedFrom[i] + CkShedGo[i] * age + Vector3.down * 7f * age * age;
                at.y = Mathf.Max(.12f, at.y);
                Place(_ckShed[i], at, Vector3.one * (.2f - .07f * u), Quaternion.Euler(age * 420f + i * 40, i * 61, age * 260f), age >= 0 && u < 1 && _ckGlacier != null ? (1 - u * u) * world : 0);
            }

            // Broken: the chunks. Thrown out and up from where the peaks stood, toward the court, tumbling, each on its own arc.
            if (_ckChunkRoot != null)
            {
                bool flying = broken >= 0 && _ckGlacier != null && t < Seconds - .02f;
                CheskaSet(_ckChunkRoot, Vector3.zero, Vector3.one, Quaternion.identity, flying);
                // They are gone with the stage, in its last frames: play resumes on a court with no glacier on it.
                float last = 1 - Ease(Seconds - .14f, Seconds - .02f, t);
                if (flying)
                    for (int i = 0; i < _ckChunks.Length; i++)
                    {
                        if (_ckChunks[i] == null) continue;
                        _ckChunks[i].localPosition = CkChunkFrom[i] + CkChunkGo[i] * broken + Vector3.down * 11f * broken * broken;
                        _ckChunks[i].localRotation = Quaternion.AngleAxis(broken * 520f + i * 37f, CkChunkSpin[i].normalized);
                        _ckChunks[i].localScale = Vector3.one * Mathf.Max(.001f, CkChunkSize[i] * CheskaOut(broken / .05f) * last);
                    }
            }
            float ring = CheskaOut(broken / .3f), wide = Mathf.Lerp(2f, 15f, ring);
            Place(_ckBreakRing, new Vector3(0, .1f, -2.4f), new Vector3(wide, 1, wide), Quaternion.identity, broken >= 0 ? (1 - ring) * .9f : 0);
            PlaceGlow(_ckBreakGlow, new Vector3(0, 4f, -3.2f), Vector3.one * 13f, Quaternion.identity, .32f * Flash(t, CkSnap + .05f, .12f));
        }

        /// <summary>
        /// The blow. A crystal of frost forms on her fingertip as she brings it up; she blows, and it leaves as three curls of
        /// cold unwinding from that point, turning slowly, with ferns of frost carried on them and a little dust. It drifts and
        /// thins away over a second and a quarter: nothing in it is quick.
        /// </summary>
        private void SampleCheskaBlow(float t, float leave)
        {
            float since = t - CkBlow;
            // Where the breath starts: off the raised hand. If the hand is not where a blown fingertip would be (a rig posed
            // some other way: behind her, at her hip, over her head), it starts in front of her chin.
            Vector3 head = HeadPoint, tip = _heldItem != null ? FreePalm : RightPalm;
            if (tip.z < .02f || tip.y < head.y - 1.1f || tip.y > head.y) tip = head + new Vector3(0, -.75f, .32f);
            // Out from her and off to the side the lens is NOT on, rising a little. Blown straight out it came at the lens
            // and unwound across her face (film p2); this way it crosses the open side of the frame, beside her.
            float lensSide = 1f; var lens = new Vector3(.8f, 1.2f, 2.8f);
            if (_performance != null && ShotCount > CkWideShot + 1)
            {
                _performance.Shot(CkWideShot + 1, Mathf.Clamp(t, CkBlow, CkBlow + 1f), out lens, out _, out _);
                lensSide = lens.x < 0 ? -1f : 1f;
            }
            var way = (Vector3.forward * .8f + Vector3.left * lensSide * .56f + Vector3.up * .12f).normalized;
            var across = Vector3.Cross(Vector3.up, way).normalized; var above = Vector3.Cross(way, across);
            var start = tip + Vector3.up * .07f + way * .05f;

            // The frost on her fingertip, and the cold light of it: it gathers before she blows and is gone with the breath.
            float sat = Ease(CkBlow - .32f, CkBlow - .06f, t) * (1 - Ease(CkBlow, CkBlow + .2f, t));
            Place(_ckTipFrost, start, Vector3.one * .1f * sat, Quaternion.Euler(t * 90f, t * 60f, 20f), sat * leave);
            PlaceGlow(_ckTipGlow, start, Vector3.one * (.42f + .4f * Flash(t, CkBlow + .04f, .12f)), Quaternion.identity, (.5f * sat + .7f * Flash(t, CkBlow + .04f, .12f)) * leave);

            float thin = (1 - Ease(.5f, 1.28f, since)) * leave, carried = CheskaOut(since / 1.3f);
            var middle = start + way * (.12f + carried * .8f) + above * carried * .12f;
            // The curls and the ferns are flat: they are turned to the lens that watches this beat, or, going sideways
            // across its view, they are seen edge on and are nothing (film p3).
            var seen = (lens - middle).normalized;
            var facing = Quaternion.LookRotation(-seen, Vector3.up);
            for (int i = 0; i < _ckCurls.Count; i++)
            {
                var row = CkCurlGo[i];
                float grown = CheskaOut((since - row.z) / .55f);
                float turned = row.x + (_reducedEffects ? 0 : row.w * since);
                var turn = facing * Quaternion.Euler(0, 0, turned);
                float size = row.y * grown * (1 + .3f * carried);
                bool on = since > row.z && thin > .002f;
                // The mesh is flat in x and y; flat things lie in x and z everywhere else here, so its y is given a size of its own.
                bool worn = _ckCurlFx[i] != null && _ckCurlFx[i].Renderer != null;
                Place(_ckCurls[i], middle, new Vector3(size, size, size), turn, on && !worn ? thin : 0);
                if (worn)
                {
                    UltimateFx.Place(_ckCurlFx[i], middle, turn, new Vector3(size, size, size));
                    UltimateFx.Set(_ckCurlFx[i], on ? since - row.z : -1f, Mathf.Lerp(.1f, 1f, Ease(.45f, 1.3f, since)), leave);
                }
                PlaceGlow(_ckCurlGlows[i], middle - seen * .02f, new Vector3(size, size, size), turn, on ? thin * .5f : 0);
            }
            for (int i = 0; i < _ckFlakes.Count; i++)
            {
                var row = CkFlakeGo[i];
                float late = since - .06f - i * .035f, gone = CheskaOut(late / 1.25f);
                var at = start + way * (.08f + gone * row.z * .8f) + across * row.x * (.3f + gone) + above * (row.y * (.3f + gone) + (_reducedEffects ? 0 : .02f * Mathf.Sin(late * 3f + i)));
                // A fern lies flat in x and z: its flat is stood to face the lens, and it turns in that flat.
                var turn = Quaternion.LookRotation(Vector3.up, seen) * Quaternion.Euler(_reducedEffects ? 0 : 18f * Mathf.Sin(late * 2.2f + i), CkFlakeSpin[i] * late, 0);
                Place(_ckFlakes[i], at, Vector3.one * row.w * CheskaOut(late / .25f), turn, late > 0 ? thin : 0);
            }
            for (int i = 0; i < _ckDust.Count; i++)
            {
                float late = since - .1f - i * .05f, gone = CheskaOut(late / 1.2f);
                float round = i * 1.05f + 0.4f;
                var at = start + way * (.1f + gone * (.5f + .08f * i)) + (across * Mathf.Cos(round) + above * Mathf.Sin(round)) * (.08f + .22f * gone);
                PlaceGlow(_ckDust[i], at, Vector3.one * (.05f + .02f * (i % 3)), Quaternion.identity, late > 0 ? thin * (.6f + (_reducedEffects ? 0 : .4f * Mathf.Sin(late * 9f + i * 2f))) : 0);
            }
        }

        // ------------------------------------------------------------------ the lotus

        /// <summary>One piece of lit ice (a petal, a splinter, the seed): its model, its surfaces, and its own moment for the shader.</summary>
        private sealed class CkIce
        {
            public Transform Model; public Renderer[] Surfaces;
            public readonly MaterialPropertyBlock Block = new MaterialPropertyBlock();
        }

        private static readonly int CkIceAge = Shader.PropertyToID("_Age"), CkIceLit = Shader.PropertyToID("_Lit"), CkIceFlash = Shader.PropertyToID("_Flash"),
                                    CkIceErode = Shader.PropertyToID("_Erode"), CkIceSeed = Shader.PropertyToID("_Seed"),
                                    CkIceFrom = Shader.PropertyToID("_From"), CkIceTo = Shader.PropertyToID("_To");

        /// <summary>
        /// A model of the lotus in its own lit shader, hidden; null if the model is missing. <paramref name="from"/> and
        /// <paramref name="to"/> are where its root and its tip are along its own length (a petal stands on its root, a
        /// splinter is about its middle), which is what the shader runs its glint and its erosion along.
        /// </summary>
        private CkIce CheskaLitIce(string name, float seed, float from, float to)
        {
            var go = UltimateProp.SpawnLitIce(name, _root.transform, ref _ckLotusLook);
            if (go == null) return null;
            go.SetActive(false);
            var ice = new CkIce { Model = go.transform, Surfaces = go.GetComponentsInChildren<Renderer>(true) };
            ice.Block.SetFloat(CkIceSeed, seed); ice.Block.SetFloat(CkIceFrom, from); ice.Block.SetFloat(CkIceTo, to);
            return ice;
        }

        private static void CheskaLit(CkIce ice, Vector3 at, Vector3 scale, Quaternion turn, bool shown, float age, float lit, float flash, float eaten)
        {
            if (ice == null || ice.Model == null) return;
            if (ice.Model.gameObject.activeSelf != shown) ice.Model.gameObject.SetActive(shown);
            if (!shown) return;
            ice.Model.localPosition = at; ice.Model.localScale = scale; ice.Model.localRotation = turn;
            ice.Block.SetFloat(CkIceAge, age); ice.Block.SetFloat(CkIceLit, Mathf.Clamp(lit, 0f, 2f));
            ice.Block.SetFloat(CkIceFlash, Mathf.Clamp01(flash)); ice.Block.SetFloat(CkIceErode, Mathf.Clamp01(eaten));
            foreach (var surface in ice.Surfaces) if (surface != null) surface.SetPropertyBlock(ice.Block);
        }

        /// <summary>One of this stage's own ribbons worn by a piece of the effects kit: its uv already runs along it and across it.</summary>
        private UltimateFx.Piece CheskaWear(Mesh mesh, Color core, Color edge, int look, int order)
        {
            var piece = UltimateFx.Wisp(_root.transform, "arc", core, edge, look, order);
            var filter = piece.Transform != null ? piece.Transform.GetComponent<MeshFilter>() : null;
            if (filter != null) filter.sharedMesh = mesh;
            return piece;
        }

        /// <summary>
        /// How long a petal is, <paramref name="age"/> seconds after it starts: out in four frames, a sixth past its length,
        /// back under it and settled. Softer than a peak of the glacier (`CheskaRise`): a petal is thin, and it rings less.
        /// </summary>
        private static float CheskaBloom(float age)
        {
            if (age <= 0) return 0;
            const float rise = .14f;
            if (age < rise) { float a = age / rise; return (1 - (1 - a) * (1 - a) * (1 - a)) * (1 + .16f * a * a); }
            float after = age - rise;
            return 1 + .16f * Mathf.Exp(-after * 8f) * Mathf.Cos(after * 19f);
        }

        /// <summary>How far a petal has opened: it comes up closed, falls open past where it rests, and comes back.</summary>
        private static float CheskaOpen(float age)
            => age <= 0 ? 0 : 1 - Mathf.Exp(-age * 7.5f) * Mathf.Cos(age * 12f);

        /// <summary>
        /// The lotus and everything round it (the header of `CkPetals` has the beats). The petals, the splinters and the seed
        /// are models in their own lit shader; the frost drawn in, thrown out, circling and torn off is the effects kit's
        /// ribbons, which are eaten away and never faded; the light on the court and on her is glow. With no kit, or no
        /// models, each of those is simply not there and the rest plays.
        /// </summary>
        private void BuildCheskaLotus()
        {
            // Her cold in three steps and its frost. The frost is a little under white, so a frame full of it is not a white frame.
            Color body = new Color(.40f, .72f, .97f, 1f), light = new Color(.56f, .88f, 1f, 1f), cyan = new Color(.55f, .93f, 1f, 1f), frost = new Color(.90f, .98f, 1f, 1f);
            for (int i = 0; i < CkPetals.Length; i++) _ckPetalIce.Add(CheskaLitIce(CkPetalModel[CkPetals[i].Kind], 1f + i * 1.7f, 0f, 1f));
            for (int i = 0; i < CkSplinterFrom.Length; i++) _ckSplinters.Add(CheskaLitIce("lotusshard", 3f + i * 2.3f, -.5f, .5f));
            _ckSeed = CheskaLitIce("lotusshard", .5f, -.5f, .5f);
            _ckSeedGlow = AddGlow("LotusSeedCold", cyan, falloff: 2f, core: .5f);
            _ckSeedRingA = CheskaOrder(Add("LotusSeedRingA", VfxShapes.Collar(36, .02f, .9f), new Color(.92f, 1f, 1f, .95f), .9f), 3);
            _ckSeedRingB = CheskaOrder(Add("LotusSeedRingB", VfxShapes.Collar(36, .02f, .92f), new Color(.70f, .93f, 1f, .9f), .9f), 3);
            _ckLotusLight = AddGlow("LotusCourtLight", cyan, billboard: false, falloff: 2f, core: .25f);
            _ckLotusHer = AddGlow("LotusLightOnHer", cyan, falloff: 2.4f, core: .15f);
            for (int i = 0; i < CkGlitterFrom.Length; i++)
                _ckGlitter.Add(AddGlow("LotusGlitter" + i, i % 4 == 1 ? CkPink : new Color(.92f, .99f, 1f, 1f), falloff: 2.6f, core: .85f));

            // THE FROST ON THE COURT: a disc that creeps out from under the flower, solid in its middle and streaks at its
            // rim, and on the shatter is eaten from its middle outward along those streaks. ⚠️ DEEP BLUE, the darkest thing of
            // hers: the petals are pale, and on a pale court from above the flower was a white star on white (film e1).
            _ckFrostDisc = UltimateFx.Disc(_root.transform, new Color(.13f, .47f, .82f, 1f), frost, UltimateFx.Streak, -3).With("_Glow", .35f).With("_Band", .3f)
                .With("_Scale", new Vector4(1.0f, 8f, 0, 0)).With("_Spread", 1.2f);
            // THE FROST DRAWN IN to the seed: each ribbon's root is at the seed and its tip is out on the court; it is
            // eaten tip first while its grain runs toward the root and it winds round, so it is seen to be pulled in.
            for (int i = 0; i < _ckDrawn.Length; i++)
            {
                _ckDrawn[i] = UltimateFx.Wisp(_root.transform, CkDrawnShape[i], body, frost, i % 2 == 0 ? UltimateFx.Smoke : UltimateFx.Streak, 3)
                    .With("_Scale", new Vector4(2.0f, 1.5f, 0, 0)).With("_Scroll", new Vector4(-2.6f, 0, 0, 0)).With("_Sweep", -1.2f)
                    .With("_Shape", .75f).With("_Band", .32f).With("_Lead", .68f).With("_Hot", cyan).With("_Glow", .55f);
                _ckDrawn[i].Seed = 1f + i * 1.9f;
            }
            // THE FROST THROWN OUT by the bloom: a crown of curled lobes, wide and low, and one ribbon winding up round her.
            _ckBloomCrown = UltimateFx.Wisp(_root.transform, "crown", body, frost, UltimateFx.Smoke, 3)
                .With("_Scale", new Vector4(1.8f, 1.5f, 0, 0)).With("_Scroll", new Vector4(2.4f, .2f, 0, 0)).With("_Sweep", 1.1f)
                .With("_Shape", .8f).With("_Band", .34f).With("_Lead", .68f).With("_Hot", cyan).With("_Glow", .5f);
            _ckBloomCrown.Seed = 2f;
            _ckBloomSpiral = UltimateFx.Wisp(_root.transform, "spiral", light, frost, UltimateFx.Streak, 4)
                .With("_Scale", new Vector4(2.2f, 1.6f, 0, 0)).With("_Scroll", new Vector4(2.8f, 0, 0, 0)).With("_Sweep", 1.0f)
                .With("_Shape", .7f).With("_Band", .34f).With("_Lead", .66f).With("_Hot", cyan).With("_Glow", .5f);
            _ckBloomSpiral.Seed = 4f;
            // THE MIST that circles it while it stands: low curls, a pale leading edge on a blue body, each tearing away
            // from its root as the next is thrown.
            for (int i = 0; i < _ckMistFx.Length; i++)
            {
                _ckMistFx[i] = UltimateFx.Wisp(_root.transform, CkMistShape[i], body, frost, i % 2 == 0 ? UltimateFx.Smoke : UltimateFx.Streak, 3)
                    .With("_Scale", new Vector4(2.0f, 1.4f, 0, 0)).With("_Scroll", new Vector4(1.8f, .15f, 0, 0)).With("_Sweep", 1.0f)
                    .With("_Shape", .8f).With("_Band", .36f).With("_Lead", .66f).With("_Hot", cyan).With("_Glow", .4f);
                _ckMistFx[i].Seed = 6f + i * 2.3f;
            }
            // THE CROWN OF FROST torn off upward as it shatters.
            _ckBreakCrown = UltimateFx.Wisp(_root.transform, "crown", light, frost, UltimateFx.Streak, 4)
                .With("_Scale", new Vector4(1.6f, 1.8f, 0, 0)).With("_Scroll", new Vector4(3.4f, .2f, 0, 0)).With("_Sweep", 1.2f)
                .With("_Shape", .8f).With("_Band", .36f).With("_Lead", .66f).With("_Hot", cyan).With("_Glow", .6f);
            _ckBreakCrown.Seed = 9f;
        }

        private void SampleCheskaLotus(float t, float leave, float world)
        {
            float since = t - CkStomp, broken = t - CkSnap;
            // Reduced effects holds everything that only shimmers.
            float calm = _reducedEffects ? 0f : 1f, clock = _reducedEffects ? 0f : t;
            // What the shatter throws is gone with the stage, in its last frames, as the glacier's chunks are.
            float last = 1 - Ease(Seconds - .14f, Seconds - .02f, t);

            // ---- THE CHARGE. The seed turns in its ball of cold, the two rings close on it, the frost is drawn in; and
            // as her foot comes down it drops to the court under it and is the flower's middle.
            float charge = t - (CkStomp - CkChargeFor);
            float gathered = Ease(0f, CkChargeFor - .1f, charge), sunk = Ease(CkStomp - .07f, CkStomp + .02f, t);
            bool charging = charge > 0 && since < .03f;
            var seedAt = Vector3.Lerp(CkSeedAt, new Vector3(0, .08f, 0), sunk);
            float seed = gathered * (1 - sunk) * (1 + .06f * calm * Mathf.Sin(t * 29f));
            CheskaLit(_ckSeed, seedAt, Vector3.one * .36f * seed, Quaternion.Euler(clock * 210f, clock * 330f, 25f), charging && seed > .02f, t, 1.6f, 0f, 0f);
            float struck = Flash(t, CkStomp + .03f, .09f);
            PlaceGlow(_ckSeedGlow, seedAt, Vector3.one * (.6f + 1.0f * gathered + 2.4f * struck), Quaternion.identity, (charging ? .75f * gathered : 0f) + .6f * struck);
            float ringA = Mathf.Lerp(1.5f, .52f, gathered), ringB = Mathf.Lerp(1.9f, .72f, gathered);
            Place(_ckSeedRingA, seedAt, new Vector3(ringA, 1, ringA), Quaternion.Euler(0, clock * 300f, 0) * Quaternion.Euler(62f, 0, 0), charging ? gathered * (1 - sunk) : 0);
            Place(_ckSeedRingB, seedAt, new Vector3(ringB, 1, ringB), Quaternion.Euler(0, 70f - clock * 240f, 0) * Quaternion.Euler(-48f, 0, 25f), charging ? gathered * (1 - sunk) : 0);
            for (int i = 0; i < _ckDrawn.Length; i++)
            {
                var row = CkDrawnIn[i];
                // Not before the cold has begun to gather: a ribbon with nothing to be drawn to is a stray blade on the court (film e3).
                float u = (charge - .1f - row.y) / .42f;
                bool on = charging && u > 0 && u < 1;
                // It comes in slowly and then all at once.
                float reach = Mathf.Lerp(row.z, .6f, u * u), wound = row.x + row.w * Ease(0f, 1f, u);
                UltimateFx.Place(_ckDrawn[i], new Vector3(CkSeedAt.x, .06f, CkSeedAt.z), Quaternion.Euler(0, wound, 0), new Vector3(reach * .4f, reach * .3f, reach));
                UltimateFx.Set(_ckDrawn[i], on ? charge - .1f - row.y : -1f, Mathf.Lerp(.38f, 1f, Ease(.3f, 1f, u)));
            }

            // ---- THE BLOOM, AND THE FLOWER STANDING. Before the shatter it draws in and brightens, as the glacier does.
            float drawnIn = Ease(CkSnap - CkCrackFor, CkSnap - .02f, t);
            for (int i = 0; i < _ckPetalIce.Count; i++)
            {
                var row = CkPetals[i];
                float age = since - row.Late;
                float up = CheskaBloom(age), open = CheskaOpen(age - .04f);
                // Fat at the root as it comes, lean as it overshoots.
                float thick = Mathf.Lerp(.5f, 1f, CheskaOut(age / .14f)) * (1.08f - .5f * (Mathf.Clamp(up, .9f, 1.2f) - 1f));
                float tip = row.Tip * Mathf.LerpUnclamped(.25f, 1f, open) * (1 - .14f * drawnIn);
                float swell = broken > 0 ? 1 + .1f * CheskaOut(broken / .05f) : 1;
                // Eaten from its tip in four frames, not all on the same one.
                float eaten = broken > 0 ? CheskaOut((broken - .012f * (i % 4)) / .13f) : 0;
                float round = row.Turn * Mathf.Deg2Rad;
                var root = new Vector3(Mathf.Sin(round), 0, Mathf.Cos(round)) * row.Out + Vector3.down * .04f;
                var turn = Quaternion.Euler(0, row.Turn, 0) * Quaternion.Euler(tip, 0, 0) * Quaternion.Euler(0, row.Roll, 0);
                float across = row.Long * CkPetalWide * thick * swell;
                // The light inside: bright as it forms, then breathing, the breath going out through the rings; up again before the shatter.
                float lit = .62f + .22f * calm * Mathf.Sin(t * 2.6f - row.Kind * 1.1f) + Mathf.Exp(-Mathf.Max(0, age) * 5f) + .9f * drawnIn;
                float flash = .55f * Flash(t, CkSnap + .01f, .07f) + .3f * Flash(t, CkStomp + row.Late + .05f, .07f);
                CheskaLit(_ckPetalIce[i], root, new Vector3(across, Mathf.Max(.001f, row.Long * up * swell), across), turn, age > 0 && eaten < .999f, clock, lit, flash, eaten);
            }

            // The light of it on the court and on her.
            float stands = Ease(CkStomp + .02f, CkStomp + .3f, t) * (1 - Ease(CkSnap, CkSnap + .12f, t));
            float breath = calm * Mathf.Sin(t * 2.6f);
            // Before the flower the same light lies under the seed, small: from straight above, where the heart is watched
            // from, a ball of cold a hand above the court is nothing without the pool of light it throws on it.
            bool pooled = charging && since < 0;
            PlaceGlow(_ckLotusLight, pooled ? new Vector3(CkSeedAt.x, .11f, CkSeedAt.z) : Vector3.up * .11f, Vector3.one * (pooled ? 1.2f + 2.2f * gathered : 6.2f + .5f * breath), Quaternion.Euler(90, 0, 0),
                (pooled ? .55f * gathered : stands * (.2f + .06f * breath + .3f * drawnIn)) + .6f * Flash(t, CkStomp + .06f, .12f));
            PlaceGlow(_ckLotusHer, new Vector3(0, .5f, 0), Vector3.one * 2.3f, Quaternion.identity, stands * (.2f + .15f * drawnIn));

            // The frost creeping out on the court under it: fast, then slowly for as long as the flower stands.
            float reachOut = Mathf.Lerp(.5f, 4.4f, CheskaOut(since / .45f)) + .5f * Mathf.Clamp01(since / 2.2f);
            UltimateFx.Place(_ckFrostDisc, Vector3.up * .085f, Quaternion.Euler(0, 20f, 0), new Vector3(reachOut, 1f, reachOut));
            UltimateFx.Set(_ckFrostDisc, since > 0 ? since : -1f, broken > 0 ? Mathf.Lerp(.05f, 1f, Ease(0f, .26f, broken)) : .05f, .8f * Ease(CkStomp, CkStomp + .1f, t) * last);

            // The frost thrown out by the bloom.
            float thrownWide = Mathf.Lerp(1.4f, 5.2f, CheskaOut(since / .4f)), thrownHigh = 1.3f * CheskaOut(since / .12f) * (1 - .4f * Ease(.3f, .7f, since));
            UltimateFx.Place(_ckBloomCrown, Vector3.up * .08f, Quaternion.Euler(0, 40f, 0), new Vector3(thrownWide, Mathf.Max(.05f, thrownHigh), thrownWide));
            UltimateFx.Set(_ckBloomCrown, since > 0 ? since : -1f, Mathf.Lerp(.3f, 1f, Ease(.08f, .7f, since)));
            float coil = 1.5f * (1 + .3f * Mathf.Clamp01(since / .6f));
            UltimateFx.Place(_ckBloomSpiral, Vector3.up * .05f, Quaternion.Euler(0, 120f + clock * 90f, 0), new Vector3(coil, 2.0f * CheskaOut(since / .2f) + .05f, coil));
            UltimateFx.Set(_ckBloomSpiral, since > 0 ? since : -1f, Mathf.Lerp(.2f, 1f, Ease(.1f, .62f, since)));

            // The mist circling it. Each curl is thrown, grows and is torn away, and is thrown again further round.
            for (int i = 0; i < _ckMistFx.Length; i++)
            {
                var row = CkMist[i];
                float cycle = (since - .3f) / 1.25f + row.w, u = cycle - Mathf.Floor(cycle);
                float round = row.x + 22f * since, grown = Mathf.Lerp(1.2f, row.z, CheskaOut(u / .6f));
                var away = new Vector3(Mathf.Sin(round * Mathf.Deg2Rad), 0, Mathf.Cos(round * Mathf.Deg2Rad));
                UltimateFx.Place(_ckMistFx[i], away * row.y + Vector3.up * .05f, Quaternion.Euler(0, round + 98f, 0), new Vector3(grown * .55f, grown * .26f, grown));
                UltimateFx.Set(_ckMistFx[i], since > .3f ? u * 1.25f : -1f, Mathf.Max(Mathf.Lerp(.22f, 1f, Ease(.25f, 1f, u)), Ease(0f, .2f, broken)), Ease(.3f, .55f, since));
            }

            // ---- THE SHATTER: splinters on arcs, glitter that is carried out and hangs, and the crown torn off upward.
            bool flying = broken >= 0 && t < Seconds - .02f;
            for (int i = 0; i < _ckSplinters.Count; i++)
            {
                var at = CkSplinterFrom[i] + CkSplinterGo[i] * broken + Vector3.down * 11f * broken * broken;
                at.y = Mathf.Max(.1f, at.y);
                CheskaLit(_ckSplinters[i], at, Vector3.one * Mathf.Max(.001f, CkSplinterLong[i] * CheskaOut(broken / .04f) * last),
                    Quaternion.AngleAxis(broken * 640f + i * 47f, CkSplinterSpin[i].normalized), flying, clock, 1.3f, .4f * Flash(t, CkSnap + .02f, .06f), 0f);
            }
            for (int i = 0; i < _ckGlitter.Count; i++)
            {
                float carried = 1 - Mathf.Exp(-Mathf.Max(0, broken) * 9f), twinkle = .55f + .45f * calm * Mathf.Sin(broken * 46f + i * 2.4f);
                PlaceGlow(_ckGlitter[i], CkGlitterFrom[i] + CkGlitterGo[i] * carried + Vector3.down * .4f * Mathf.Max(0, broken), Vector3.one * CkGlitterSize[i] * (1 + .5f * twinkle),
                    Quaternion.identity, flying ? last * (.5f + .6f * twinkle) : 0);
            }
            float tornWide = Mathf.Lerp(1.6f, 5.0f, CheskaOut(broken / .22f)), tornHigh = 2.6f * CheskaOut(broken / .1f);
            UltimateFx.Place(_ckBreakCrown, Vector3.up * .1f, Quaternion.Euler(0, 200f, 0), new Vector3(tornWide, Mathf.Max(.05f, tornHigh), tornWide));
            UltimateFx.Set(_ckBreakCrown, broken >= 0 ? broken : -1f, Mathf.Lerp(.22f, 1f, Ease(.04f, .3f, broken)));
        }

        /// <summary>Her body on the loop and on the circle, and the ice each leaves.</summary>
        private void SampleCheskaSkate(float t, float leave)
        {
            var feet = CheskaSkate(t); var way = CheskaHeading(t);
            // Turned the way she goes while she glides, one slow turn on the spot as the loop ends, and exactly as she began
            // whenever she is standing.
            float gliding = Ease(CkWayAt[0], CkWayAt[0] + .2f, t) * (1 - Ease(CkSpinFrom - .2f, CkSpinFrom, t))
                          + Ease(CkCircleFrom, CkCircleFrom + .15f, t) * (1 - Ease(CkCircleTo - .18f, CkCircleTo, t));
            if (_bodyRoot != null)
            {
                float yaw = Mathf.Atan2(way.x, way.z) * Mathf.Rad2Deg * gliding + 360f * Ease(CkSpinFrom, CkSpinTo, t);
                var ahead = CheskaHeading(t + .12f);
                float bank = Mathf.Clamp(Vector3.SignedAngle(way, ahead, Vector3.up) * .75f, -28f, 28f) * gliding;
                _bodyRoot.transform.SetPositionAndRotation(_ckBodyAt + _facing * feet,
                    _facing * Quaternion.Euler(0, yaw, -bank) * Quaternion.Inverse(_facing) * _ckBodyTurn);
            }
            // The loop's ribbon melts as she turns to the first of them; the ring stays for the glacier to come up through.
            CheskaRibbon(_ckTrail, CkWayAt[0], CkWayAt[CkWayAt.Length - 1], t, t < CkLooks[0] + .2f && leave > .02f);
            CheskaRibbon(_ckRing, CkCircleFrom, CkCircleTo, t, t < CkStomp + .35f && leave > .02f);

            // Her spray. While she glides, shavings leave the blade behind her, each on its own small arc, more of them the
            // faster she goes; a cold light lies under the skate.
            float pace = Mathf.Clamp01((CheskaSkate(t + .04f) - CheskaSkate(t - .04f)).magnitude / .08f / 3.2f) * gliding;
            var side = new Vector3(way.z, 0, -way.x);
            for (int i = 0; i < _ckSpray.Count; i++)
            {
                var row = CkSprayGo[i];
                float u = Mathf.Repeat(t * 2.8f + row.w, 1f);
                var at = feet - way * (.12f + row.z * u) + side * row.x * u + Vector3.up * (.06f + row.y * 4 * u * (1 - u) * .5f);
                Place(_ckSpray[i], at, Vector3.one * .075f * (1 - u * .7f), Quaternion.Euler(u * 400f + i * 60, i * 45, u * 300f), pace > .05f ? (1 - u) * pace : 0);
            }
            PlaceGlow(_ckSkateGlow, feet + Vector3.up * .07f, Vector3.one * 1.5f, Quaternion.Euler(90, 0, 0), pace * .45f * leave);

            // And where she stops, the fan: thrown ahead of her along the line she came in on, up fast and a little too far,
            // then falling back and thinning. Once as the loop ends and once, smaller, as the heart does.
            float loopStop = CkWayAt[CkWayAt.Length - 1] - .07f, heartStop = CkCircleTo - .1f;
            bool second = t > CkLooks[0];
            float stopped = t - (second ? heartStop : loopStop);
            var thrown = CheskaHeading((second ? heartStop : loopStop) - .12f);
            float up = CheskaOut(stopped / .1f) * (1 + .25f * CheskaBump(stopped, .12f, .08f)), fall = Ease(.22f, second ? .34f : .5f, stopped);
            float big = second ? .8f : 1.25f;
            Place(_ckFan, feet + thrown * .15f + Vector3.up * .03f, new Vector3(big * up, big * up * (1 - .55f * fall), big * up * (1 + .2f * fall)),
                Quaternion.LookRotation(thrown, Vector3.up), stopped > 0 ? (1 - fall) * leave : 0);
        }

        /// <summary>
        /// One ribbon up to this moment, on the line her feet take and nowhere else: the same points for its frost, its ice
        /// and the blade's cut, and the newest third of a second of it again, bright, for the edge the blade has just made.
        /// </summary>
        private void CheskaRibbon(CkRibbon ribbon, float from, float to, float t, bool shown)
        {
            if (ribbon == null || ribbon.Core == null) return;
            float upTo = Mathf.Clamp(t, from, to - .001f);
            int count = ribbon.Points.Length;
            for (int i = 0; i < count; i++)
                ribbon.Points[i] = CheskaSkate(Mathf.Lerp(from + .001f, upTo, i / (float)(count - 1))) + Vector3.up * .05f;
            bool drawn = shown && t > from + .05f;
            foreach (var line in new[] { ribbon.Bloom, ribbon.Core, ribbon.Cut })
            {
                if (line == null) continue;
                line.SetPositions(ribbon.Points);
                line.enabled = drawn;
            }
            if (ribbon.Fresh == null) return;
            for (int i = 0; i < ribbon.Fresh.positionCount; i++)
                ribbon.Fresh.SetPosition(i, CheskaSkate(Mathf.Lerp(Mathf.Max(from + .001f, upTo - .34f), upTo, i / (float)(ribbon.Fresh.positionCount - 1))) + Vector3.up * .055f);
            // Only while the blade is still cutting it.
            ribbon.Fresh.enabled = drawn && t < to + .05f;
        }

        /// <summary>
        /// Her arm through her eyes: the copy of her is held to the FRAME (its shoulder at the bottom corner, the same size on
        /// screen however far the lens has zoomed), and on it the arm comes up with the elbow closed, the fist by her cheek,
        /// then swings to their line and opens until it is straight at them, a little past and back. The elbow is a hinge
        /// about the forearm's own Y, as every clip folds it.
        /// </summary>
        /// <summary>
        /// ⚠️ THE ARM, MEASURED (2026-10-09). Three placements of this arm were tried without seeing them and none showed.
        /// The editor's cutscene film sets this, and every frame the arm is up writes where its shoulder, elbow and every
        /// surface of the copy really are IN THE LENS'S OWN SPACE (right, up, forward in metres, then where that falls on
        /// the frame, -1 to 1 each way), to `Logs/paete-ability-film/arm_<tag>.txt`. Null in a match, and unread.
        /// </summary>
        public static System.Text.StringBuilder CheskaArmLog;

        private void SampleCheskaArm(Vector3 eye, Vector3 look, float fov, float since, int which)
        {
            if (_ckSelf == null || _ckUpper == null) return;
            float pointed = CkPointIn[which], length = CkLookFor[which];
            float raised = Ease(pointed - .54f, pointed - .26f, since) * (1 - Ease(length - .14f, length, since));
            _ckSelf.SetActive(raised > .01f);
            if (raised <= .01f) return;
            var forward = (look - eye).normalized; var right = Vector3.Cross(Vector3.up, forward).normalized; var above = Vector3.Cross(forward, right);
            float half = Mathf.Tan(fov * .5f * Mathf.Deg2Rad), zoom = half / Mathf.Tan(32f * Mathf.Deg2Rad);
            float side = _heldItem != null ? -1 : 1;
            // The thrust: fast, a little too far, and back.
            float thrust = Mathf.Clamp01((since - (pointed - .1f)) / .1f); thrust = 1 - (1 - thrust) * (1 - thrust);
            float over = .1f * Mathf.Sin(Mathf.Clamp01((since - pointed) / .2f) * Mathf.PI);

            // The copy faces the way she looks, the size the zoom leaves it.
            _ckSelf.transform.localRotation = Quaternion.LookRotation(forward, above) * _ckSelfTurn;
            // ⚠️ Her true size. Half as big again, with the shoulder at the lens, her arm and chest were the whole picture (film c16).
            _ckSelf.transform.localScale = _ckSelfScale * zoom;

            // Her arm, in the scene's space. Bent: the upper arm forward and down from the shoulder, a little out, so the
            // elbow leads. Straight: along her line of sight, turned a little in to the middle of the frame, where they are.
            var to = (forward * .9f + above * .35f - right * .25f * side).normalized;
            var upper = Vector3.Slerp((forward * .8f - above * .3f + right * .12f * side).normalized, to, thrust + over * .3f);
            // The elbow closes toward her own face (up and in), which is where the fist is before it goes.
            var close = (above - right * .25f * side + forward * .2f).normalized;
            var front = (close - upper * Vector3.Dot(close, upper)).normalized;
            var aimed = _root.transform.rotation * Quaternion.LookRotation(front, Vector3.Cross(front, upper * _ckArmAlong));
            _ckUpper.localRotation = _ckUpperRest;
            _ckUpper.rotation = Quaternion.Slerp(_ckUpper.rotation, aimed, raised);
            _ckFore.localRotation = Quaternion.Euler(0, -_ckArmAlong * Mathf.Lerp(96f, 0f, thrust) * raised, 0);

            // Then the whole copy is slid so that shoulder sits on the frame where a first-person arm's would: low and to
            // her side, just behind the lens, coming up from under the edge and in as she points.
            // ⚠️ MEASURED 2026-10-09: 0.2 m IN FRONT of the lens at no zoom (it was 0.25 behind, and nothing of her showed).
            // A fixed 0.75 m, and not a distance that shrinks with the zoom: her copy already shrinks with it, so a seat that came
            // nearer as well made the arm grow on every tighter look until it was the whole picture (films c31, c32).
            float away = .75f;
            // ⚠️ A quarter of a metre BEHIND the lens: her chest is then behind it too, and only the arm comes forward into view.
            float across = Mathf.Lerp(.7f, .6f, thrust) * side, high = Mathf.Lerp(-2.4f, Mathf.Lerp(-1.2f, -1.1f, thrust), raised);
            var seat = eye + forward * away + right * across * half * away * (16f / 9f) + above * high * half * away;
            _ckSelf.transform.position += _root.transform.TransformPoint(seat) - _ckUpper.position;
            if (CheskaArmLog == null) return;
            var lensAt = _root.transform.TransformPoint(eye);
            var lensTurn = Quaternion.Inverse(_root.transform.rotation * Quaternion.LookRotation(forward, above));
            string Seen(Vector3 world)
            {
                var v = lensTurn * (world - lensAt);
                string frame = v.z > .01f ? (v.x / (v.z * half * 16f / 9f)).ToString("0.00") + ", " + (v.y / (v.z * half)).ToString("0.00") : "BEHIND THE LENS";
                return "(" + v.x.ToString("0.00") + ", " + v.y.ToString("0.00") + ", " + v.z.ToString("0.00") + ") frame " + frame;
            }
            CheskaArmLog.AppendLine("look " + which + " since " + since.ToString("0.00") + " raised " + raised.ToString("0.00") + " thrust " + thrust.ToString("0.00")
                + " fov " + fov.ToString("0") + " zoom " + zoom.ToString("0.00") + " scale " + _ckSelf.transform.lossyScale.x.ToString("0.00"));
            CheskaArmLog.AppendLine("  shoulder " + Seen(_ckUpper.position));
            CheskaArmLog.AppendLine("  elbow    " + Seen(_ckFore.position));
            foreach (Transform child in _ckFore) CheskaArmLog.AppendLine("  under the forearm, " + child.name + " " + Seen(child.position));
            foreach (var surface in _ckSelf.GetComponentsInChildren<Renderer>(true))
                CheskaArmLog.AppendLine("  surface " + surface.name + " enabled " + surface.enabled + " active " + surface.gameObject.activeInHierarchy
                    + " forcedOff " + surface.forceRenderingOff + " middle " + Seen(surface.bounds.center) + " size " + surface.bounds.size.ToString("0.00"));
        }
    }
}
