using System;
using System.Collections.Generic;
using TumbangPreso.CameraSystem;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // NEMU AND KURO, LIGHTS OUT, 7.2 s (was DEVOURING SEANCE, 3.8 s; plan.md § 4 is the first one).
        //
        // ⚠️ RESTAGED 2026-10-09, UNPITCHED. The owner was away and asked for "something for the rest of the heroes ...
        // surprise me with something really good", so this one was not chosen from options as Dante's, Cheska's and
        // Sean's were. It is BLOCKING until he says the idea stays. The first cutscene is whole in git history and in
        // `tools/cutscene_rework/old/`.
        //
        // What is kept, because it is the part that is hers: "Looks distracted. Already knows your next move." She gazes
        // at nothing, Kuro nudges her, she offers a hand and he nuzzles it, then she looks straight down the lens
        // (0 to 2.45, unchanged). What is new is everything the look sets off, and it now shows what the ultimate DOES
        // ("Kuro becomes a monster and chases every player he sees"):
        //   1. THE DIVE (2.45 to 3.0), from high over the court: the ink runs out from her feet across the whole floor
        //      and the walls go up. Little Kuro hops once and plunges into it like a fish.
        //   2. THE RISE (3.0 to 4.0), same shot: far too big, he comes up out of the ink behind her, taller than the
        //      court is wide. She does not turn round.
        //   3. HE SEES YOU (4.0 to 4.95): his head turns to each other player where they really stand, one at a time,
        //      and a pair of eyes opens in the floor under each of them.
        //   4. THE SILHOUETTE (4.95 to 6.0), low and close: she is small and calm with her hands folded, he is the whole
        //      sky behind her. This is the plan's signature picture, finally at its real scale.
        //   5. THE SEND (6.0 to 7.2): she points, he rears and comes over her head at the lens mouth first, and the
        //      picture is swallowed. It ENDS BLACK, so every screen returns to its own eyes out of the dark with him
        //      already loose.
        //
        // ⚠️ NOTHING FADES AT THE END. The stage is whole to the last frame and the last frames are black: the going
        // dark is the hand-back. A stage that left early would show the court for a moment before he arrives.
        //
        // ⚠️ THE RETAINED KURO RENDER COPY AND HIS CALM/RAGE FORMS ARE UNCHANGED. He is the same copy the whole way: calm
        // until he dives, the rage form from the rise, at `GhostPetCompanion.DevourScale` times `NmGiant`.
        //
        // ⚠️ EVERY TIME BELOW IS ON THE TABLE'S CLOCK (`tools/author_ultimate_intros.py`, `nemu`): her look, her folded
        // hands and her point are typed there.
        //
        // ⚠️ THE LOOK PASS, 2026-10-10 (owner: "i dont want to see anything bare in textures, anything looking unpolished,
        // no vfx etc"). The staging, the clock, the shots and Kuro are as they were. Everything round him was a stock
        // splat or a plain cylinder, and three things were wrong with all of it at once:
        //   * THE INK WAS SUNLIT. It was drawn with the effects' lit material, so the court's key light shaded it and it
        //     filmed as a grey floor inside a slate wall (`sheet_nemu_p0.jpg`). Ink does not take the sun. Every piece of
        //     it here is UNLIT and carries its own colour per vertex (`AddInk`): near-black violet, a purple sheen, a
        //     lavender lip where it ends.
        //   * IT WAS SCALED, NOT MOVING. A disc that grows reads as a decal. The pool, the curtain, the crowns, the ropes
        //     and the eyes are meshes rewritten every frame from typed tables (`NmFingers`, `NmLeads`, the peaks), so the
        //     edge creeps in fingers, the wall climbs in uneven columns and a lid really closes over an iris.
        //   * BEATS HAD NOTHING IN THEM. The dive was one thin ring and so was the rise. Each beat now has an arrival, an
        //     impact and a trace: a crown thrown up and falling back, ropes that stretch off him and snap, drips still
        //     falling while he looks round, and a jaw of pale teeth closing over the lens as the black arrives.
        // The palette is ink, lavender and lilac only. Her pink is hers and is on nothing here.
        //
        // ⚠️ AND IT IS DRAWN THE WAY KURO IS DRAWN. Ink that has a body (the crowns, the ropes, the drops, what streams off
        // him) wears his own tail's shader (`Shaders/KuroTail`, `AddDrawn`): stepped painted tones off one ramp, an ink
        // line, and where it thins it breaks into halftone dots instead of going see-through, exactly as his tail does.
        // It comes off him, so it is made of what he is made of. Flat ink (the floor, the wall) cannot wear that shader,
        // so its edges break into rows of shrinking dots laid as geometry.
        //
        // ⚠️ HE IS THE SOURCE OF THE DARK (owner, 2026-10-10, of the film with all of the above in it: "needs more
        // vfx/particles", and for every hero "be more creative with particle effects and do more with emmissives"). So the
        // light in this night is HIS and nothing else's, and every moving mote has a job at a beat:
        //   * embers in his eyes and his throat, and their light lying on the ink under him;
        //   * comets of spirit light that stream out of him and are drawn back in, each on its own typed orbit;
        //   * drops of ink that lift off the floor as he rises and hang in the air round him, tails down;
        //   * sparks circling Nemu, slow, because she is the still middle of it;
        //   * a shock of motes and a ring under each player the moment he sees them;
        //   * streaks tearing past the lens as he comes.
        // Every eye on the stage also has a lit core now where its iris is. None of it is a cloud of the same dot: each
        // table below is typed, and each thing in it is a different size on a different clock.
        //
        // ⚠️ AND HIS GIANT FORM WAS REMODELLED FOR THIS (`tools/kubo_kuro_rage.py`): one painted skin. Its tail is built
        // to fade as calm Kuro's does, and `NmDressGiantTail` is what dresses it so here. The live game does not yet.
        //
        // ⚠️⚠️ THE LENS IS WORKED OUT FROM HIS BODY, NOT TYPED (owner, 2026-10-10, of this cutscene in a match with the
        // ability's banner across the top of it: "still too high, if anything you should raise the camera so the full
        // ghost is always visible"). The high shot was typed when he was a different shape and stood him with his ears on
        // the very top edge of the picture. A match then lays the banner over the top of the screen, and may slide the lens
        // in along its line when the map puts something behind it (`UltimatePhaseView.ChooseShot`): his brows, eyes and
        // ears were gone, and the film, which has no banner and no map, showed him whole. So from the moment he is up until
        // he goes for the lens, `NmFitGiant` takes each typed shot as a STARTING view and moves it: it aims so that he and
        // Nemu sit between the bottom of the picture and the underside of the banner, then raises and draws back the lens
        // along its own line until every corner of him is inside, at whatever size, lean and screen shape this is. Where
        // the lens cannot go back (the stage's own wall, or the map's push-in, which it measures from the frame before) it
        // widens instead. It is a function of where he is, so it moves as he does and never snaps.
        // ⚠️ AND HE STANDS IN THE INK, NOT OVER IT: `NmGiantSink` lets him down until his tail goes into the pool it rose from.
        //
        // ⚠️ AND WHAT LEAVES HIM TEARS, IT DOES NOT FADE (owner, 2026-10-10, of a feed of effects: "see if you can pull off
        // any of those"; then, of the kit that came of it, "give me the next effects"). `UltimateFx` draws a ribbon with a
        // mask running along it and eats it away by a threshold, with a burning rim where it tears. In his language that
        // is ink smoke: the comets' tails and the streaks of the lunge are torn ribbons now, slow wisps peel off his
        // shoulders, his back and the foot of his tail and are eaten as they climb, a crown of them is thrown up where he
        // breaks the pool and a wash runs out from it, wisps are ripped back off him as he goes, a ring spins round the
        // light in his throat before he roars, and the picture is EATEN to black by ink closing on its middle. No comic
        // pop: nothing of his is a bang. Every one of them is behind or beside his head, never across his face.
        //
        // ⚠️ HE ACTS, ON HIS RIG (owner, 2026-10-10: "it feels too stiff for me"; from 4.1 to 6.4 s he held one pose, arms
        // out flat). `KuroRagePresentation.Live` gives him breath, a weave, a running tail, arms that reach in turn and
        // claws that work, in play and here alike. This stage adds what the beats ask for through `KuroLife`: his head
        // whips to each player and carries on past them before it settles, he roars as he arrives, he coils before the
        // pounce (arms in, head back, tail bunched) and is thrown out of it with his jaw wide.
        //
        // ⚠️ NOTHING WITH WEIGHT MOVES ON A STRAIGHT LINE. Little Kuro crouches before his hop and tips forward into the
        // ink; the giant stretches as he comes up; the send is one move, a deep rear back and a pounce in an arc over
        // her head. The times are the table's and are untouched: only the curves between them were reshaped.
        // =========================================================================================
        private const float NmDark = 2.3f, NmDive = 2.5f, NmRise = 3.0f, NmRisen = 4.0f, NmLean = 4.9f, NmRear = 5.95f, NmLunge = 6.2f, NmGulp = 6.95f;
        // How many times his devouring size he is here, where he stands, and where the last shot's lens is authored.
        private const float NmGiant = 2.4f;
        private static readonly Vector3 NmGiantAt = new Vector3(0, 0, -7.5f), NmLastLens = new Vector3(0, 1.3f, 6.5f);
        // Where little Kuro goes under, and the moment his body crosses the surface (the plunge is a square, so it is late).
        private static readonly Vector3 NmDiveAt = new Vector3(-.75f, 0, -.35f);
        private const float NmSplash = NmDive + .24f, NmStageRadius = 16f, NmStageTop = 18f;

        private static readonly Color NmInkDeep = new Color(.024f, .008f, .05f, 1f), NmInkBody = new Color(.06f, .022f, .11f, 1f),
            NmInkSheen = new Color(.30f, .15f, .52f, 1f), NmInkLip = new Color(.60f, .44f, .92f, 1f), NmInkEdge = new Color(.10f, .04f, .19f, 1f),
            NmHaze = new Color(.115f, .05f, .2f, 1f), NmLilac = new Color(.86f, .78f, 1f, 1f), NmWhite = new Color(.97f, .94f, 1f, 1f);

        // ⚠️ IT STARTS ABOVE THE FLOOR'S OWN DARK. Painted from the floor's near-black, a crown standing on that floor was a
        // black shape on black and the dive read as nothing (film p2). Ink that has left the pool catches the light.
        // The one ramp every drawn piece of ink is painted from, darkest first. A piece asks for a tone and the shader
        // steps between these, so the shading is in flat painted bands and never a smooth computer gradient.
        private static readonly Color[] NmTones =
        {
            new Color(.045f, .018f, .09f, 1f), new Color(.08f, .034f, .155f, 1f), new Color(.125f, .055f, .235f, 1f), new Color(.18f, .085f, .33f, 1f),
            new Color(.255f, .13f, .45f, 1f), new Color(.35f, .2f, .59f, 1f), new Color(.5f, .34f, .78f, 1f), new Color(.68f, .53f, .96f, 1f),
        };

        /// <summary>⚠️ A FILM OUTSIDE PLAY HAS NO KURO: its caster is a bare motor with no `CharacterVisual`. The editor's cutscene
        /// film stands one beside her and names it here; in a match this is null and unread (as `FilmStandIns`).</summary>
        public static GhostPetCompanion FilmCompanion;
        private MatchPoseHistory.Copy _kuro;
        private KuroRagePresentation _rage;
        private Vector3 _kuroScale, _nmLens;
        private Quaternion _kuroFront;
        private Renderer[] _kuroRenderers;
        private int _inkWall, _inkSpill, _nmDiveRing, _nmRiseRing, _nmShroud;
        private int _nmCeiling, _nmRidgeFar, _nmRidgeNear, _nmHang, _nmCrownPiece, _nmTearPiece, _nmSwell, _nmJet, _nmPuddle;
        private int _nmDiveRing2, _nmRiseRing2, _nmDiveGlow, _nmRiseGlow, _nmShadow, _nmNuzzleGlow, _nmJawUp, _nmJawDown, _nmJawGlowUp, _nmJawGlowDown;
        private NmInk _nmPool, _nmCurtain, _nmCrown, _nmTear;
        private readonly List<NmEye> _inkEyes = new List<NmEye>(8), _nmSeen = new List<NmEye>(8);
        private readonly List<int> _nmBlobs = new List<int>(12), _nmGlints = new List<int>(3), _nmDrops = new List<int>(7), _nmWisps = new List<int>(6),
            _nmMotes = new List<int>(8), _nmBeads = new List<int>(7), _nmTearBeads = new List<int>(8), _nmRopePieces = new List<int>(8),
            _nmDrips = new List<int>(10), _nmDripRings = new List<int>(10), _nmStreams = new List<int>(8), _nmStars = new List<int>(3);
        private readonly List<NmInk> _nmRopes = new List<NmInk>(8);
        private readonly float[] _nmSnapAt = new float[8];
        private int _nmThroat, _nmThroatCast, _nmEmberLeft, _nmEmberRight;
        private readonly List<UltimateFx.Piece> _nmTrails = new List<UltimateFx.Piece>(12), _nmRushTrails = new List<UltimateFx.Piece>(12),
            _nmSmoke = new List<UltimateFx.Piece>(8), _nmTorn = new List<UltimateFx.Piece>(6);
        private UltimateFx.Piece _nmRiseWisps, _nmRiseWash, _nmEaten, _nmEatenAll;
        private readonly UltimateFx.Piece[] _nmOrbRing = new UltimateFx.Piece[2];
        // The smoke that peels off him: which ribbon, where it leaves him (from his mouth, m; the last two from where he
        // stands, for the foot of his tail), and how long one takes to climb and be eaten (s).
        private static readonly string[] NmSmokeShape = { "s", "hook", "arc", "whip", "s", "arc", "spiral", "s" };
        private static readonly Vector4[] NmSmokeAt =
        {
            new Vector4(-4.6f, .2f, -4.2f, 2.3f), new Vector4(4.4f, -.2f, -4.4f, 2.9f), new Vector4(-2.2f, 1.6f, -5.6f, 2.6f), new Vector4(2.5f, 1.3f, -5.8f, 3.3f),
            new Vector4(.2f, .4f, -6.4f, 2.1f), new Vector4(-5.4f, -1.8f, -4.6f, 3.6f), new Vector4(.7f, .1f, .9f, 1.9f), new Vector4(-.9f, .1f, .3f, 2.5f),
        };
        // The wisps torn back off him as he goes: bearing round his mouth as the lens sees it (degrees), which ribbon.
        private static readonly float[] NmTornAt = { 28, 82, 143, 201, 262, 318 };
        private static readonly string[] NmTornShape = { "whip", "hook", "whip", "arc", "whip", "hook" };
        private Renderer _nmGiantEyeLeft, _nmGiantEyeRight;
        // The box the remodelled giant stands in, in his model's own unit (`tools/kubo_kuro_rage.py` prints it): ear tips
        // to tail tip, claw to claw. And how far he is let down into the ink, in metres.
        private static readonly Vector3 NmBodyLow = new Vector3(-.154f, .006f, -.125f), NmBodyHigh = new Vector3(.154f, .288f, .032f);
        private const float NmGiantSink = 1.3f;
        // How much of the picture's height the banner leaves him (of the half height, from the middle), and the margins.
        private const float NmFrameTop = .58f, NmFrameBottom = .9f, NmFrameSide = .9f;
        private readonly Vector3[] _nmFit = new Vector3[10];
        private readonly float[] _nmPull = { 1, 1, 1, 1, 1, 1, 1, 1 };
        private Vector3 _nmAskedEye, _nmAskedLook;
        private float _nmAskedAt = -9;
        private int _nmAskedShot = -1;
        private readonly List<int> _nmComets = new List<int>(12), _nmLifted = new List<int>(12), _nmOrbit = new List<int>(5),
            _nmOrbitLights = new List<int>(5), _nmShock = new List<int>(24), _nmShockRings = new List<int>(4);
        private static readonly Vector3 KuroSeat = new Vector3(-.95f, .65f, .15f);

        // ------------------------------------------------------------------ the typed tables
        // ⚠️ EVERY SHAPE BELOW IS TYPED, NOT ROLLED. A seeded jitter gives eleven fingers of one character; typing them gives
        // a long one beside a stub beside a fat slow one, which is what makes an edge look as if it is deciding where to go.

        // The pool's fingers: where on the rim (degrees), half its width (degrees), how far ahead of the rim it reaches
        // (of the radius), and where in its own pulse it starts.
        private static readonly Vector4[] NmFingers =
        {
            new Vector4(8, 16, .34f, .0f), new Vector4(41, 9, .22f, .5f), new Vector4(67, 22, .16f, .2f), new Vector4(103, 11, .38f, .7f),
            new Vector4(139, 18, .20f, .35f), new Vector4(171, 8, .30f, .9f), new Vector4(198, 24, .14f, .15f), new Vector4(236, 12, .36f, .6f),
            new Vector4(262, 9, .20f, .05f), new Vector4(291, 20, .26f, .45f), new Vector4(327, 13, .32f, .8f),
        };
        // Beads of ink that land ahead of the rim and are overrun by it: bearing (degrees), how far out (m), size (m).
        private static readonly Vector3[] NmSpatter =
        {
            new Vector3(24, 2.2f, .22f), new Vector3(88, 3.4f, .30f), new Vector3(154, 4.8f, .26f), new Vector3(218, 3.0f, .20f),
            new Vector3(276, 5.6f, .34f), new Vector3(52, 8.2f, .50f), new Vector3(122, 10.5f, .60f), new Vector3(184, 9.0f, .55f),
            new Vector3(250, 12.0f, .70f), new Vector3(310, 11.0f, .60f), new Vector3(346, 14.0f, .80f), new Vector3(214, 13.5f, .80f),
        };
        // How far ahead of the wall each of its columns climbs, round the stage.
        private static readonly float[] NmLeads =
            { 0f, .35f, .1f, .8f, .2f, .05f, .5f, .15f, .9f, .3f, 0f, .45f, .1f, .7f, .25f, .05f, .6f, .2f, 1f, .1f, .4f, 0f, .75f, .3f };
        // The far shapes of the night: a low near ridge, a tall far one, and what hangs from the top (metres, round the stage).
        private static readonly float[] NmRidgeNear = { 1.4f, 2.6f, 1.1f, .7f, 2.2f, 2.9f, 1.3f, .8f, 1.9f, 2.4f, .9f, 1.6f, 2.8f, 1.2f, .6f, 2.1f };
        private static readonly float[] NmRidgeFar = { 4.2f, 6.4f, 3.3f, 5.1f, 7.2f, 4.0f, 2.9f, 5.8f, 6.6f, 3.6f, 4.8f, 7.0f, 3.1f, 5.4f, 6.1f, 3.8f };
        private static readonly float[] NmHangs = { 1.2f, 3.6f, .8f, 2.4f, 4.4f, 1.0f, 2.9f, .7f, 3.9f, 1.6f, 2.2f, 4.8f, .9f, 3.1f, 1.4f, 2.6f };
        // Drops gathering under the top: bearing (degrees), how far out (m), how long it hangs (m), how thick (m).
        private static readonly Vector4[] NmCeilingDrops =
        {
            new Vector4(150, 10.5f, 5.2f, .7f), new Vector4(212, 11.5f, 3.4f, .55f), new Vector4(181, 12.6f, 6.4f, .85f), new Vector4(118, 12.0f, 2.8f, .5f),
            new Vector4(242, 9.6f, 4.3f, .6f), new Vector4(74, 12.8f, 3.6f, .6f), new Vector4(292, 12.4f, 4.9f, .7f),
        };
        // Wisps adrift in the dark: bearing (degrees), how far out (m), height (m), length (m).
        private static readonly Vector4[] NmWispAt =
        {
            new Vector4(140, 13.0f, 5.5f, 9f), new Vector4(205, 12.2f, 9.0f, 7f), new Vector4(250, 13.6f, 4.0f, 8f),
            new Vector4(100, 12.6f, 10.5f, 6f), new Vector4(172, 13.8f, 12.5f, 10f), new Vector4(300, 13.0f, 7.0f, 7f),
        };
        // Little lights far off: bearing (degrees), how far out (m), height (m), size (m).
        private static readonly Vector4[] NmMoteAt =
        {
            new Vector4(128, 11.0f, 8.2f, .22f), new Vector4(160, 13.2f, 11.0f, .16f), new Vector4(196, 12.0f, 3.1f, .2f), new Vector4(226, 13.6f, 9.4f, .26f),
            new Vector4(262, 11.6f, 5.6f, .18f), new Vector4(96, 13.0f, 4.4f, .2f), new Vector4(178, 10.2f, 13.4f, .24f), new Vector4(236, 9.4f, 12.0f, .16f),
        };
        // The crown where little Kuro goes in: where each point stands on the rim (degrees) and how tall it is.
        private static readonly float[] NmDivePeaks = { 0, 48, 101, 158, 205, 259, 311 }, NmDiveGains = { 1f, .7f, .9f, .6f, 1.05f, .75f, .85f };
        // The crown he tears through: heavier, wider and fewer.
        private static readonly float[] NmTearPeaks = { 12, 62, 121, 170, 214, 266, 303, 344 }, NmTearGains = { .8f, 1f, .65f, .5f, .95f, .7f, 1.05f, .6f };
        // The ropes of ink he pulls up with him: where each holds him (from his root, m), where it is rooted in the floor
        // (from where he stands, m), how high the hold is when it snaps (m), and how thick it is (m).
        private static readonly Vector3[] NmRopeHold =
        {
            new Vector3(-4.6f, 6.6f, .5f), new Vector3(4.6f, 6.6f, .5f), new Vector3(-2.9f, 6.2f, .4f), new Vector3(2.9f, 6.2f, .4f),
            new Vector3(-1.5f, 5.4f, 1.1f), new Vector3(1.5f, 5.4f, 1.1f), new Vector3(-.4f, 3.0f, .5f), new Vector3(.5f, 2.2f, .5f),
        };
        private static readonly Vector3[] NmRopeRoot =
        {
            new Vector3(-6.0f, 0, 1.2f), new Vector3(6.2f, 0, .8f), new Vector3(-3.4f, 0, 2.0f), new Vector3(3.1f, 0, 2.4f),
            new Vector3(-1.9f, 0, 3.0f), new Vector3(2.2f, 0, 2.7f), new Vector3(-1.2f, 0, 1.6f), new Vector3(1.4f, 0, 1.3f),
        };
        private static readonly Vector2[] NmRopeSnap =
        {
            new Vector2(4.6f, .5f), new Vector2(5.2f, .5f), new Vector2(3.6f, .42f), new Vector2(4.1f, .42f),
            new Vector2(2.8f, .55f), new Vector2(3.3f, .55f), new Vector2(2.2f, .6f), new Vector2(1.7f, .6f),
        };
        // What still falls off him while he looks round: where it lets go (from his root, m) and when.
        private static readonly Vector4[] NmDripAt =
        {
            new Vector4(-4.9f, 6.2f, .6f, 3.62f), new Vector4(4.4f, 6.0f, .7f, 3.78f), new Vector4(-1.4f, 5.2f, 1.3f, 3.9f), new Vector4(2.6f, 5.9f, .5f, 4.05f),
            new Vector4(-3.2f, 5.8f, .5f, 4.18f), new Vector4(1.2f, 5.0f, 1.3f, 4.3f), new Vector4(5.0f, 6.3f, .5f, 4.42f), new Vector4(-.3f, 2.6f, .6f, 4.5f),
            new Vector4(-4.2f, 6.4f, .6f, 4.62f), new Vector4(3.4f, 6.1f, .6f, 4.74f),
        };
        // What streams back off him as he comes: bearing round his mouth as the lens sees it (degrees), how far out from the
        // mouth (m), how long it grows (of the others), how thick (m).
        private static readonly Vector4[] NmStreamAt =
        {
            new Vector4(18, 2.9f, 1f, .5f), new Vector4(62, 2.6f, .7f, .38f), new Vector4(108, 2.7f, 1.15f, .46f), new Vector4(158, 3.0f, .8f, .42f),
            new Vector4(202, 2.9f, 1.05f, .5f), new Vector4(248, 2.5f, .6f, .36f), new Vector4(292, 2.6f, .9f, .44f), new Vector4(338, 3.0f, 1.2f, .4f),
        };
        // His teeth as the lens sees them close: where along the jaw (frame half-heights, the left half; the right is its
        // mirror), how wide and how long. The upper row has no middle tooth and the lower row has one, so they mesh.
        private static readonly Vector3[] NmTeethUp = { new Vector3(-2.02f, .34f, .5f), new Vector3(-1.5f, .36f, .66f), new Vector3(-1.02f, .27f, .38f), new Vector3(-.6f, .24f, .3f), new Vector3(-.2f, .22f, .25f) };
        private static readonly Vector3[] NmTeethDown = { new Vector3(-1.76f, .3f, .42f), new Vector3(-1.26f, .32f, .52f), new Vector3(-.8f, .25f, .33f), new Vector3(-.4f, .23f, .27f), new Vector3(0, .22f, .24f) };

        // The comets that stream off him and are drawn back in: how far out its orbit reaches (m), how high above his mouth
        // it rides (m), how fast it goes round (radians a second; the sign is the way), and where on its clock it starts.
        private static readonly Vector4[] NmCometAt =
        {
            new Vector4(6.5f, 2.6f, 1.3f, .0f), new Vector4(4.8f, -1.2f, -1.7f, 1.1f), new Vector4(8.2f, .6f, .9f, 2.3f), new Vector4(5.6f, 3.6f, -1.1f, 3.6f),
            new Vector4(7.4f, -2.8f, 1.5f, 4.4f), new Vector4(4.2f, 1.4f, 2.1f, 5.2f), new Vector4(9.0f, 2.0f, -.8f, .7f), new Vector4(6.0f, -3.6f, -1.4f, 2.9f),
            new Vector4(5.2f, 4.4f, 1.0f, 4.0f), new Vector4(7.8f, -.4f, -1.9f, 5.8f), new Vector4(3.8f, -2.0f, 1.8f, 1.7f), new Vector4(8.6f, 3.2f, 1.2f, 3.2f),
        };
        // ⚠️ NONE OF THEM HANGS BETWEEN THE LOW LENS AND HIS FACE. Two did (film p12): from 5.07 to 6.43 s one sat on his cheek
        // beside his fangs like a hole in him. Each is typed out to a side of the line from Nemu to him.
        // The drops that lift off the floor as he rises: where from (from where he stands, m), how high each hangs (m), how late it lets go (s).
        private static readonly Vector4[] NmLiftAt =
        {
            new Vector4(-5.2f, 1.6f, 3.4f, .0f), new Vector4(4.6f, 2.4f, 5.6f, .12f), new Vector4(-2.4f, 4.2f, 2.2f, .22f), new Vector4(6.8f, -.6f, 7.4f, .05f),
            new Vector4(-7.4f, -1.0f, 6.2f, .3f), new Vector4(4.4f, 5.0f, 1.6f, .4f), new Vector4(-4.0f, -3.2f, 8.6f, .18f), new Vector4(3.4f, -3.8f, 4.4f, .5f),
            new Vector4(8.6f, 2.8f, 3.0f, .34f), new Vector4(-8.8f, 2.4f, 4.8f, .46f), new Vector4(-6.4f, 5.6f, 3.6f, .6f), new Vector4(5.8f, 5.2f, 6.6f, .26f),
        };
        // The sparks that circle Nemu: how far out (m), how high (m), where on the circle each starts.
        private static readonly Vector3[] NmOrbitAt =
            { new Vector3(.78f, 1.28f, .0f), new Vector3(.92f, .52f, 1.4f), new Vector3(.7f, .92f, 2.7f), new Vector3(1.02f, 1.56f, 3.9f), new Vector3(.84f, .24f, 5.2f) };
        // The shock under a player he has just seen: the way each of its six motes is thrown.
        private static readonly Vector3[] NmShockWay =
        {
            new Vector3(1f, .5f, .2f), new Vector3(.4f, .9f, .9f), new Vector3(-.7f, .6f, .7f), new Vector3(-1f, .4f, -.2f), new Vector3(-.3f, 1.1f, -.9f), new Vector3(.7f, .7f, -.8f),
        };
        // The streaks that tear past the lens as he comes: bearing round the line between them (degrees), how far off that
        // line (m), how long (m), and where along it each starts.
        private static readonly Vector4[] NmRushAt =
        {
            new Vector4(12, 1.6f, 5.5f, .0f), new Vector4(48, 2.8f, 7.5f, .42f), new Vector4(81, 1.3f, 4.5f, .77f), new Vector4(118, 3.2f, 8.5f, .2f),
            new Vector4(152, 1.9f, 6f, .61f), new Vector4(187, 2.5f, 5f, .9f), new Vector4(221, 1.4f, 7f, .33f), new Vector4(254, 3.0f, 6.5f, .7f),
            new Vector4(289, 1.7f, 4.8f, .1f), new Vector4(322, 2.6f, 8f, .52f), new Vector4(345, 3.6f, 5.5f, .84f), new Vector4(66, 3.8f, 6.8f, .27f),
        };
        // The giant's tail, tier by tier from under his body to its tip: the colours `tools/kubo_kuro_rage.py` paints its
        // flat cells in, in that tool's order, so the fading shader shows the same tail the painted one is.
        private static readonly Color[] NmGiantTail =
        {
            new Color(.231f, .125f, .412f, 1f), new Color(.333f, .188f, .604f, 1f), new Color(.478f, .322f, .776f, 1f), new Color(.643f, .514f, .902f, 1f),
            new Color(.804f, .722f, .969f, 1f), new Color(.957f, .933f, 1f, 1f), new Color(.957f, .933f, 1f, 1f), new Color(.725f, .549f, 1f, 1f),
        };

        // ------------------------------------------------------------------ ink, as a mesh that is rewritten

        /// <summary>A mesh with a colour on every vertex, cleared and refilled by whoever owns it: once for a shape that only
        /// moves, every frame for one that creeps.</summary>
        private sealed class NmInk
        {
            public readonly Mesh Mesh;
            private readonly List<Vector3> _points = new List<Vector3>(256);
            private readonly List<Color> _colours = new List<Color>(256);
            private readonly List<Vector2> _tones = new List<Vector2>(256);
            private readonly List<int> _triangles = new List<int>(1024);
            public NmInk(string name) { Mesh = new Mesh { name = name }; Mesh.MarkDynamic(); }
            public int Count => _points.Count;
            public void Begin() { _points.Clear(); _colours.Clear(); _tones.Clear(); _triangles.Clear(); }
            /// <summary>Flat ink, in the colour as it should be SEEN. ⚠️ A mesh's colours are not converted as a material's are:
            /// the first film stored them as typed and every near-black came out a mid purple.</summary>
            public int V(Vector3 point, Color colour) { _points.Add(point); _colours.Add(colour.linear); _tones.Add(new Vector2(0, .6f)); return _points.Count - 1; }
            /// <summary>Drawn ink: a tone off the ramp (0 darkest to 1) and how much of it is left here (1 solid, 0 gone to dots).</summary>
            public int P(Vector3 point, float tone, float left)
            {
                tone = Mathf.Clamp01(tone);
                float f = tone * (NmTones.Length - 1); int i = Mathf.Min((int)f, NmTones.Length - 2);
                var colour = Color.Lerp(NmTones[i], NmTones[i + 1], f - i).linear; colour.a = Mathf.Clamp01(left);
                // The cell the shader reads the tone from: the ramp is the top row of its palette, one slot every two cells.
                _points.Add(point); _colours.Add(colour); _tones.Add(new Vector2(tone * .999f, .6f));
                return _points.Count - 1;
            }
            public void Tri(int a, int b, int c) { _triangles.Add(a); _triangles.Add(b); _triangles.Add(c); }
            /// <summary>Join `rows` rows of `columns` vertices, written row by row from `first`. `outward` winds a shape turned
            /// round +y so its faces look out, which a drawn piece needs for its ink line.</summary>
            public void Grid(int first, int rows, int columns, bool wrap, bool outward = false)
            {
                int across = wrap ? columns : columns - 1;
                for (int r = 0; r < rows - 1; r++)
                    for (int c = 0; c < across; c++)
                    {
                        int a = first + r * columns + c, b = first + r * columns + (c + 1) % columns, d = a + columns, e = b + columns;
                        if (outward) { Tri(a, b, d); Tri(b, e, d); } else { Tri(a, d, b); Tri(b, d, e); }
                    }
            }
            /// <summary>Join every vertex from `first` to here to the one at `centre`, as a closed fan.</summary>
            public void Fan(int centre, int first)
            {
                int n = _points.Count - first;
                for (int i = 0; i < n; i++) Tri(centre, first + i, first + (i + 1) % n);
            }
            /// <summary>A small flat six-sided dot, for an edge that breaks up.</summary>
            public void Dot(Vector3 at, Vector3 across, Vector3 along, float radius, Color colour)
            {
                int centre = V(at, colour), first = Count;
                for (int s = 0; s < 6; s++) V(at + (across * Mathf.Cos(s * Mathf.PI / 3) + along * Mathf.Sin(s * Mathf.PI / 3)) * radius, colour);
                Fan(centre, first);
            }
            /// <summary>`reach` above zero fixes the bounds, for a mesh that moves inside them and must never be culled.
            /// `drawn` gives it the smooth normals the ink line and the soft light are worked from.</summary>
            public void End(float reach = 0, bool drawn = false)
            {
                Mesh.Clear(); Mesh.SetVertices(_points); Mesh.SetColors(_colours); Mesh.SetUVs(0, _tones); Mesh.SetTriangles(_triangles, 0);
                if (drawn) Mesh.RecalculateNormals();
                if (reach > 0) Mesh.bounds = new Bounds(Vector3.zero, Vector3.one * (reach * 2));
            }
        }

        private sealed class NmEye
        {
            public NmInk Ink;
            public int Piece, Glow, Core;
        }

        /// <summary>
        /// ⚠️ INK IS UNLIT. The effects' own material is the lit Standard shader, and under the court's sun a near-black
        /// floor filmed as a mid grey one. This swaps the piece onto the sprite shader, which takes the mesh's own colours
        /// and nothing from the scene, and gives it a place in the draw order: these pieces write no depth, so the far
        /// wall must be told it is behind the floor and the floor that it is under the eyes.
        /// </summary>
        private void NmUnlit(int index, int order, bool ownColour)
        {
            var piece = _pieces[index];
            piece.Renderer.sortingOrder = order;
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) return;
            if (!ownColour) piece.Color = Color.white;
            var material = new Material(shader) { name = "Nemu ink", color = piece.Color };
            piece.Renderer.sharedMaterial = material;
            VfxRenderTag.Own(piece.Renderer.gameObject, material);
        }

        /// <summary>An ink piece. `fallback` is what it is painted if the unlit shader is missing: still dark, never white.</summary>
        private int AddInk(string name, Mesh mesh, int order, Color? fallback = null)
        {
            int index = Add(name, mesh, fallback ?? NmInkDeep, .02f, plain: true);
            NmUnlit(index, order, false);
            return index;
        }

        /// <summary>
        /// ⚠️ INK WITH A BODY IS DRAWN AS KURO'S TAIL IS (`Shaders/KuroTail`): flat painted tones stepped off `NmTones`, an
        /// ink line `line` wide (in the mesh's own units), and halftone dots where a vertex says little of it is left.
        /// Solid and depth-writing like him, so it sorts against him and the players with no help. If the shader is
        /// missing the piece falls back to unlit ink and the cutscene still runs.
        /// </summary>
        private int AddDrawn(string name, Mesh mesh, float line)
        {
            int index = Add(name, mesh, NmInkDeep, .02f, plain: true);
            var shader = Resources.Load<Shader>("Shaders/KuroTail");
            if (shader == null) { NmUnlit(index, 6, false); return index; }
            var piece = _pieces[index]; piece.Color = Color.white;
            var material = new Material(shader) { name = "Nemu drawn ink" };
            var palette = new Vector4[16];
            for (int i = 0; i < 16; i++) palette[i] = NmTones[i % NmTones.Length].linear;
            material.SetVectorArray("_Palette", palette);
            material.SetColor("_Color", Color.white); material.SetColor("_OutlineColor", new Color(.02f, .006f, .04f, 1f));
            material.SetFloat("_OutlineWidth", line); material.SetFloat("_Shade", .7f);
            piece.Renderer.sharedMaterial = material;
            VfxRenderTag.Own(piece.Renderer.gameObject, material);
            return index;
        }

        /// <summary>
        /// ⚠️ THE GIANT'S TAIL FADES AS CALM KURO'S DOES. The remodelled rage form (`tools/kubo_kuro_rage.py`) carries how
        /// much of the tail is left down its length in its vertex alpha and paints it in flat cells, one a tier, which is
        /// exactly what `Shaders/KuroTail` reads. The live game dresses only the calm tail so far
        /// (`GhostPetCompanion.DressTail`), so this stage dresses its own copy. A model with no alpha down its tail (the
        /// old rage form, should it be restored) is left as it is.
        /// </summary>
        private void NmDressGiantTail(Transform rage)
        {
            var shader = Resources.Load<Shader>("Shaders/KuroTail");
            if (shader == null) return;
            foreach (var renderer in rage.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.name != "RageLowerSpirit_part0") continue;
                var mesh = renderer is SkinnedMeshRenderer skinned ? skinned.sharedMesh : renderer.GetComponent<MeshFilter>()?.sharedMesh;
                if (mesh == null || mesh.colors32 == null || mesh.colors32.Length == 0) continue;
                var material = new Material(shader) { name = "Kuro giant tail (fade)" };
                var palette = new Vector4[16];
                for (int i = 0; i < 16; i++) palette[i] = NmGiantTail[i % NmGiantTail.Length].linear;
                material.SetVectorArray("_Palette", palette);
                material.SetColor("_Color", Color.white); material.SetColor("_OutlineColor", new Color(.02f, .006f, .04f, 1f));
                // In the model's own unit: the form is a hundredth the size it is drawn at.
                material.SetFloat("_OutlineWidth", .0012f); material.SetFloat("_DotSize", 9f); material.SetFloat("_Shade", .72f);
                renderer.sharedMaterial = material;
                VfxRenderTag.Own(renderer.gameObject, material);
            }
        }

        /// <summary>Where one of the giant's eyes is, in the scene's space; his mouth if the model has no such eye.</summary>
        private Vector3 NmGiantEye(Renderer eye, Vector3 fallback)
            => eye != null ? _root.transform.InverseTransformPoint(eye.bounds.center) : fallback;

        /// <summary>A thing with weight arriving: past where it is going, then back.</summary>
        private static float NmSettle(float u)
        { u = Mathf.Clamp01(u) - 1; return 1 + 2.2f * u * u * u + 1.2f * u * u; }

        /// <summary>One of the kit's flat ribbons (it lies along its own +y from its root) laid from `from` along `way`, its face to `lens`.</summary>
        private static void NmPlaceRibbon(UltimateFx.Piece piece, Vector3 from, Vector3 way, Vector3 lens, float length, float width)
        {
            var side = Vector3.Cross(way, lens - from);
            if (side.sqrMagnitude < 1e-8f) { UltimateFx.Place(piece, from, Quaternion.identity, Vector3.zero); return; }
            UltimateFx.Place(piece, from, Quaternion.LookRotation(Vector3.Cross(side, way), way), new Vector3(width, length, 1));
        }

        /// <summary>A streak of light lying along `way`, turned so its face is toward `lens`.</summary>
        private void NmPlaceStreak(int index, Vector3 at, Vector3 way, Vector3 lens, float length, float width, float strength)
        {
            var toLens = lens - at;
            var up = Vector3.Cross(toLens, way);
            if (up.sqrMagnitude < 1e-6f || toLens.sqrMagnitude < 1e-6f) { PlaceGlow(index, at, Vector3.one, Quaternion.identity, 0); return; }
            var face = Vector3.Cross(way, up);
            PlaceGlow(index, at, new Vector3(length, width, 1), Quaternion.LookRotation(face, up), strength);
        }

        private static float NmBump(float x) { if (x <= -1 || x >= 1) return 0; float k = 1 - x * x; return k * k; }

        /// <summary>A typed table read round a circle, eased from one entry to the next.</summary>
        private static float NmTyped(float[] table, float degrees)
        {
            float f = Mathf.Repeat(degrees, 360f) / 360f * table.Length;
            int i = (int)f % table.Length; float u = f - Mathf.Floor(f); u = u * u * (3 - 2 * u);
            return Mathf.Lerp(table[i], table[(i + 1) % table.Length], u);
        }

        /// <summary>How far the pool's rim stands out at this bearing, of its radius: the fingers, each on its own pulse.</summary>
        private static float NmRim(float degrees, float t, float live)
        {
            float reach = 0;
            for (int i = 0; i < NmFingers.Length; i++)
            {
                var f = NmFingers[i];
                float d = Mathf.DeltaAngle(degrees, f.x) / f.y;
                if (d > -1 && d < 1) reach += f.z * (.6f + .4f * live * Mathf.Sin(t * 5.2f + f.w * 6.283f)) * NmBump(d);
            }
            return 1 + reach + .03f * Mathf.Sin(degrees * Mathf.Deg2Rad * 5 + t * 2.1f);
        }

        /// <summary>The colour of the ink's surface here: deep and less deep, slow rings crossing it, and one long shine lying across it.</summary>
        private static Color NmSurface(float x, float z, float r, float t)
        {
            float crest = Mathf.Sin(r * 1.15f - t * 1.6f + .5f * Mathf.Sin(x * .31f + z * .23f));
            crest = crest > 0 ? crest * crest * crest : 0;
            float across = x * .42f + z * .91f - 2f + Mathf.Sin(t * .35f) * 3f + Mathf.Sin(z * .5f + t * .8f) * .6f;
            float shine = Mathf.Exp(-across * across / 9f);
            float deep = .5f + .5f * Mathf.Sin(x * .45f + 1.3f) * Mathf.Sin(z * .38f + .4f + t * .2f);
            return Color.Lerp(Color.Lerp(NmInkDeep, NmInkBody, deep), NmInkSheen, Mathf.Clamp01(.5f * shine + .4f * crest));
        }

        /// <summary>The pool, written in the scene's own space so its surface does not slide as its middle moves.</summary>
        private void NmFillPool(float t, float spread)
        {
            const int Around = 144, Rings = 20;
            float size = Mathf.Lerp(.3f, 17.5f, spread), lip = Mathf.Min(.16f, size * .12f), live = 1 - spread * .5f;
            var middle = Vector3.Lerp(new Vector3(-1.2f, 0, -.2f), Vector3.zero, spread);
            _nmPool.Begin();
            for (int k = 0; k <= Rings; k++)
                for (int a = 0; a < Around; a++)
                {
                    float degrees = a * 360f / Around, radians = degrees * Mathf.Deg2Rad;
                    float rim = size * NmRim(degrees, t, live), inner = rim - lip * 3;
                    float r = k == Rings ? rim : k == Rings - 1 ? rim - lip : inner * k / (Rings - 2f);
                    float x = middle.x + Mathf.Sin(radians) * r, z = middle.z + Mathf.Cos(radians) * r;
                    // A dark line, a bright lip just inside it, then the surface: the edge of something with a height.
                    Color c = k == Rings ? NmInkEdge : k == Rings - 1 ? NmInkLip : NmSurface(x, z, r, t);
                    if (k == Rings - 2) c = Color.Lerp(c, NmInkSheen, .55f);
                    _nmPool.V(new Vector3(x, 0, z), c);
                }
            _nmPool.Grid(0, Rings + 1, Around, true);
            // Where it thins at its edge it breaks into dots, as his tail does: three rows, each smaller and further out.
            const int Dots = 150;
            for (int row = 0; row < 3 && spread < 1; row++)
                for (int a = 0; a < Dots; a++)
                {
                    float degrees = (a + (row % 2) * .5f) * 360f / Dots, radians = degrees * Mathf.Deg2Rad;
                    float rim = size * NmRim(degrees, t, live), pitch = Mathf.Min(.5f, rim * 6.283f / Dots);
                    float r = rim + pitch * (.4f + row * .85f);
                    _nmPool.Dot(new Vector3(middle.x + Mathf.Sin(radians) * r, 0, middle.z + Mathf.Cos(radians) * r), Vector3.right, Vector3.forward,
                        pitch * (row == 0 ? .4f : row == 1 ? .27f : .15f), NmInkEdge);
                }
            _nmPool.End(40);
        }

        /// <summary>The wall, climbing. Each column runs ahead of the rest by its own typed lead and all of them arrive together.</summary>
        private void NmFillCurtain(float t, float climb, float settled)
        {
            const int Around = 96;
            float live = 1 - settled;
            _nmCurtain.Begin();
            for (int row = 0; row < 6; row++)
                for (int a = 0; a < Around; a++)
                {
                    float degrees = a * 360f / Around, radians = degrees * Mathf.Deg2Rad;
                    float h = NmStageTop * Mathf.Clamp01(climb * (1 + NmTyped(NmLeads, degrees) * 1.4f));
                    h = Mathf.Max(.01f, h + .3f * Mathf.Sin(degrees * .31f + t * 6f) * live * climb);
                    float low = Mathf.Min(h, 2.5f), mid = Mathf.Min(h, 7f), under = Mathf.Max(mid, h - 1.1f), lipAt = Mathf.Max(under, h - .22f);
                    float breath = .85f + .15f * Mathf.Sin(t * .7f + degrees * .05f);
                    float y; Color c;
                    switch (row)
                    {
                        // A haze low down, so the far end of the night is paler than the near: that is all the depth a dark needs.
                        case 0: y = 0; c = NmHaze * breath; break;
                        case 1: y = low; c = Color.Lerp(NmHaze, NmInkDeep, .6f); break;
                        case 2: y = mid; c = NmInkDeep; break;
                        case 3: y = under; c = Color.Lerp(NmInkDeep, NmInkBody, live); break;
                        case 4: y = lipAt; c = Color.Lerp(NmInkDeep, NmInkLip, live); break;
                        default: y = h; c = Color.Lerp(NmInkDeep, NmInkSheen, live); break;
                    }
                    c.a = 1;
                    _nmCurtain.V(new Vector3(Mathf.Sin(radians) * NmStageRadius, y, Mathf.Cos(radians) * NmStageRadius), c);
                }
            _nmCurtain.Grid(0, 6, Around, true);
            // Its climbing edge breaks into dots too, running ahead of it, and they are gone once it has arrived.
            for (int row = 0; row < 3 && live > .02f; row++)
                for (int a = 0; a < Around; a++)
                {
                    float degrees = (a + (row % 2) * .5f) * 360f / Around, radians = degrees * Mathf.Deg2Rad;
                    float h = NmStageTop * Mathf.Clamp01(climb * (1 + NmTyped(NmLeads, degrees) * 1.4f));
                    h = Mathf.Max(.01f, h + .3f * Mathf.Sin(degrees * .31f + t * 6f) * live * climb);
                    _nmCurtain.Dot(new Vector3(Mathf.Sin(radians) * (NmStageRadius - .05f), h + .22f + row * .4f, Mathf.Cos(radians) * (NmStageRadius - .05f)),
                        new Vector3(Mathf.Cos(radians), 0, -Mathf.Sin(radians)), Vector3.up, (row == 0 ? .17f : row == 1 ? .11f : .06f) * live, NmInkEdge);
                }
            _nmCurtain.End(40);
        }

        /// <summary>
        /// A crown of ink thrown up round a point: a flared cup whose rim stands in typed points. `height` and `radius` are
        /// driven by the caller, so the same sheet is the little splash where he dives and the heavy one he tears through.
        /// `left` is how much of it there still is: as it falls back it thins from its points down and goes to dots.
        /// </summary>
        private static void NmFillCrown(NmInk ink, float[] peaks, float[] gains, float width, float radius, float height, float flare, float lip, float left)
        {
            const int Sides = 56;
            ink.Begin();
            // ⚠️ TWO SKINS. The high shot looks down INTO the cup, and a single skin shows its back there, which this shader
            // draws as the ink line: the whole crown filmed as a black shape (film p3). The inner skin is the same sheet a
            // little smaller and a step darker, facing in.
            for (int skin = 0; skin < 2; skin++)
            {
                int first = ink.Count;
                for (int row = 0; row < 4; row++)
                    for (int s = 0; s < Sides; s++)
                    {
                        float degrees = s * 360f / Sides, radians = degrees * Mathf.Deg2Rad;
                        float point = 0;
                        for (int i = 0; i < peaks.Length; i++) point += gains[i] * NmBump(Mathf.DeltaAngle(degrees, peaks[i]) / width);
                        float h = height * (.34f + .66f * point);
                        // The inner skin is lit on its far side, as the inside of a cup is.
                        float light = Mathf.Clamp01(.5f + .5f * Mathf.Cos(radians - .9f + skin * Mathf.PI)); light *= light;
                        float r, y, tone, keep;
                        switch (row)
                        {
                            case 0: r = radius * .8f; y = 0; tone = .18f; keep = 1; break;
                            case 1: r = radius * .93f; y = h * .42f; tone = .3f + light * .3f; keep = Mathf.Clamp01(left * 1.6f); break;
                            case 2: r = radius * (1 + flare * .55f); y = h * .82f; tone = .45f + light * .3f; keep = left; break;
                            default: r = radius * (1 + flare * (1 + point * .3f)); y = h; tone = .62f + .36f * lip * (.35f + .65f * point); keep = left * (.8f - .3f * point); break;
                        }
                        if (skin == 1) { r -= radius * (row == 3 ? .01f : .06f); tone -= .16f; }
                        ink.P(new Vector3(Mathf.Sin(radians) * r, y, Mathf.Cos(radians) * r), tone, keep);
                    }
                ink.Grid(first, 4, Sides, true, skin == 0);
            }
            ink.End(14, true);
        }

        /// <summary>
        /// A rope of ink from the floor to where it holds him. It sags while it is short, thins as it is pulled (and goes to
        /// dots where it is thinnest), and once `gap` opens it is two stubs with a bead on each, drawing back to their ends.
        /// </summary>
        private static void NmFillRope(NmInk ink, Vector3 from, Vector3 to, float thick, float gap)
        {
            const int Segments = 12, Sides = 6;
            ink.Begin();
            var along = to - from; float length = along.magnitude;
            if (length < .05f) { ink.End(40, true); return; }
            var tangent = along / length;
            var side = Vector3.Cross(tangent, Vector3.up); side = side.sqrMagnitude < .001f ? Vector3.right : side.normalized;
            var other = Vector3.Cross(tangent, side);
            float sag = Mathf.Max(0, 1.4f - length * .16f), thin = Mathf.Clamp(2.2f / (length + .6f), .25f, 1f);
            var sun = new Vector3(-.4f, .8f, .45f).normalized;
            for (int s = 0; s <= Segments; s++)
            {
                float u = s / (float)Segments, off = Mathf.Abs(2 * u - 1);
                var p = Vector3.Lerp(from, to, u) + Vector3.down * (sag * 4 * u * (1 - u));
                // Fat where it is rooted and where it holds, thin between.
                float r = thick * thin * .7f * (1 + 2f * off * off * off);
                float open = Mathf.Abs(u - .5f) - gap * .5f;
                if (gap > 0) r = open <= 0 ? 0 : r * Mathf.Min(1, open / .05f) * (1 + 1.4f * Mathf.Exp(-open * open / .006f));
                float keep = Mathf.Lerp(Mathf.Clamp01(thin * 1.3f), 1, off) * (gap > 0 ? Mathf.Clamp01(open / .14f) : 1);
                for (int k = 0; k < Sides; k++)
                {
                    float a = k * Mathf.PI * 2 / Sides;
                    var normal = side * Mathf.Cos(a) + other * Mathf.Sin(a);
                    float light = Mathf.Clamp01(Vector3.Dot(normal, sun) * .6f + .4f);
                    ink.P(p + normal * r, .3f + light * .5f + (1 - off) * .12f, keep);
                }
            }
            ink.Grid(0, Segments + 1, Sides, true, true);
            ink.End(40, true);
        }

        /// <summary>
        /// An eye: a pale almond, an iris with a bright ring, a slit pupil with a glint, and two lids. The lids are the
        /// point: they are ink, they cover whatever of the iris the opening does not show, and so the iris can sit off
        /// centre and be cut by them, which is what makes an eye LOOK at something instead of staring.
        /// `open` 0 is shut and may pass 1 for the snap; `look` moves the iris inside the white; `slit` is the pupil's half width.
        /// </summary>
        private static void NmFillEye(NmInk ink, float open, Vector2 look, float slit)
        {
            const int N = 12;
            const float High = .52f;
            ink.Begin();
            int first = ink.Count;
            for (int row = 0; row < 3; row++)
                for (int j = 0; j <= N; j++)
                {
                    float angle = Mathf.PI * (1 - j / (float)N), x = Mathf.Cos(angle), y = open * High * Mathf.Sin(angle) * Mathf.Sin(angle);
                    ink.V(new Vector3(x, row == 0 ? y : row == 1 ? 0 : -y * .85f, 0), row == 1 ? NmWhite : new Color(.78f, .68f, .97f, 1f));
                }
            ink.Grid(first, 3, N + 1, false);

            // The iris: dark middle, a bright ring, a dark line round it.
            var at = new Vector3(look.x, look.y, 0);
            int middle = ink.V(at, new Color(.26f, .12f, .56f, 1f));
            first = ink.Count;
            for (int ring = 0; ring < 3; ring++)
                for (int s = 0; s < 16; s++)
                {
                    float a = s * Mathf.PI / 8, r = ring == 0 ? .17f : ring == 1 ? .31f : .4f;
                    ink.V(at + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0),
                        ring == 0 ? new Color(.40f, .20f, .78f, 1f) : ring == 1 ? new Color(.84f, .68f, 1f, 1f) : new Color(.13f, .05f, .27f, 1f));
                }
            ink.Grid(first, 3, 16, true);
            for (int s = 0; s < 16; s++) ink.Tri(middle, first + s, first + (s + 1) % 16);

            // The slit, and two glints: a creature's eye, not a hole.
            middle = ink.V(at, NmInkDeep); first = ink.Count;
            for (int s = 0; s < 12; s++)
            {
                float a = s * Mathf.PI / 6;
                ink.V(at + new Vector3(Mathf.Cos(a) * slit * (.55f + .45f * Mathf.Abs(Mathf.Cos(a))), Mathf.Sin(a) * .34f, 0), NmInkDeep);
            }
            ink.Fan(middle, first);
            middle = ink.V(at + new Vector3(-.13f, .15f, 0), Color.white); first = ink.Count;
            for (int s = 0; s < 6; s++) ink.V(at + new Vector3(-.13f + Mathf.Cos(s * Mathf.PI / 3) * .075f, .15f + Mathf.Sin(s * Mathf.PI / 3) * .075f, 0), NmWhite);
            ink.Fan(middle, first);
            middle = ink.V(at + new Vector3(.11f, -.13f, 0), NmWhite); first = ink.Count;
            for (int s = 0; s < 6; s++) ink.V(at + new Vector3(.11f + Mathf.Cos(s * Mathf.PI / 3) * .035f, -.13f + Mathf.Sin(s * Mathf.PI / 3) * .035f, 0), NmLilac);
            ink.Fan(middle, first);

            // The lids: solid out to where the iris could ever reach, then gone into the dark round them.
            var lid = new Color(.085f, .034f, .15f, 1f); var gone = NmInkDeep; gone.a = 0;
            for (int half = 0; half < 2; half++)
            {
                float sign = half == 0 ? 1 : -.85f; first = ink.Count;
                for (int row = 0; row < 3; row++)
                    for (int j = 0; j <= N; j++)
                    {
                        float angle = Mathf.PI * (1 - j / (float)N), cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
                        float edge = open * High * sin * sin * Mathf.Abs(sign);
                        float y = row == 0 ? edge : row == 1 ? Mathf.Max(.64f * sin, edge + .02f) : Mathf.Max(.88f * sin, edge + .04f);
                        float x = row == 0 ? cos : row == 1 ? cos * 1.14f : cos * 1.36f;
                        ink.V(new Vector3(x, y * Mathf.Sign(sign), 0), row == 0 ? lid : row == 1 ? NmInkDeep : gone);
                    }
                ink.Grid(first, 3, N + 1, false);
                // The lash line: a bright edge on the upper lid and a dimmer one below, so the almond reads from across the court.
                first = ink.Count;
                var lash = half == 0 ? NmLilac : new Color(.56f, .44f, .88f, 1f);
                for (int row = 0; row < 2; row++)
                    for (int j = 0; j <= N; j++)
                    {
                        float angle = Mathf.PI * (1 - j / (float)N), cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
                        float edge = open * High * sin * sin * Mathf.Abs(sign) + (row == 0 ? 0 : (half == 0 ? .06f : .035f) * Mathf.Sqrt(sin));
                        ink.V(new Vector3(cos, edge * Mathf.Sign(sign), 0), lash);
                    }
                ink.Grid(first, 2, N + 1, false);
            }
            ink.End(3);
        }

        // ------------------------------------------------------------------ ink, as shapes that only move

        /// <summary>A rounded shape turned round its own axis, drawn as Kuro is. Each profile point is a radius, a height, its tone
        /// on the ramp, and how much of it is left there. A little of the tone is painted in from one side, as a brush would.</summary>
        private static Mesh NmLathe(string name, Vector4[] profile, int sides)
        {
            var ink = new NmInk(name);
            ink.Begin();
            for (int r = 0; r < profile.Length; r++)
                for (int s = 0; s < sides; s++)
                {
                    float a = s * Mathf.PI * 2 / sides, light = Mathf.Clamp01(.5f + .5f * Mathf.Cos(a - .9f)); light *= light;
                    ink.P(new Vector3(Mathf.Sin(a) * profile[r].x, profile[r].y, Mathf.Cos(a) * profile[r].x), profile[r].z + (light - .35f) * .4f, profile[r].w);
                }
            ink.Grid(0, profile.Length, sides, true, true);
            ink.End(0, true);
            return ink.Mesh;
        }

        /// <summary>A drop in the air: round and heavy below, drawn out to a tail above that thins away.</summary>
        private static Mesh NmDropMesh() => NmLathe("Nemu ink drop", new[]
        {
            new Vector4(0, -.5f, .2f, 1), new Vector4(.19f, -.47f, .22f, 1), new Vector4(.36f, -.38f, .26f, 1), new Vector4(.47f, -.2f, .36f, 1), new Vector4(.5f, 0, .5f, 1),
            new Vector4(.45f, .2f, .46f, 1), new Vector4(.33f, .42f, .38f, .9f), new Vector4(.2f, .66f, .32f, .7f), new Vector4(.09f, .95f, .3f, .4f), new Vector4(0, 1.25f, .3f, 0),
        }, 12);

        /// <summary>Something hanging or standing: a flared root, a neck, and a bead on the end. Unit long, along +y.</summary>
        private static Mesh NmHangMesh() => NmLathe("Nemu ink hang", new[]
        {
            new Vector4(1.5f, 0, .12f, 1), new Vector4(1.02f, .03f, .16f, 1), new Vector4(.66f, .1f, .22f, 1), new Vector4(.44f, .24f, .28f, 1), new Vector4(.34f, .46f, .32f, 1),
            new Vector4(.36f, .64f, .38f, 1), new Vector4(.5f, .76f, .5f, 1), new Vector4(.6f, .85f, .58f, 1), new Vector4(.54f, .93f, .5f, 1), new Vector4(.32f, .98f, .42f, 1), new Vector4(0, 1f, .4f, 1),
        }, 12);

        /// <summary>The floor swelling over him before he breaks it.</summary>
        private static Mesh NmSwellMesh() => NmLathe("Nemu ink swell", new[]
        {
            new Vector4(1.14f, 0, .08f, 1), new Vector4(1.04f, .06f, .3f, 1), new Vector4(.94f, .26f, .26f, 1), new Vector4(.8f, .5f, .32f, 1), new Vector4(.62f, .72f, .4f, 1),
            new Vector4(.42f, .88f, .5f, 1), new Vector4(.2f, .97f, .58f, .85f), new Vector4(0, 1f, .6f, .7f),
        }, 32);

        /// <summary>What streams back off him: thick where it leaves him, a long taper going to dots, a bead at the end. Unit long, along +y.</summary>
        private static Mesh NmStreamMesh() => NmLathe("Nemu ink stream", new[]
        {
            new Vector4(1f, 0, .28f, 1), new Vector4(.8f, .05f, .36f, 1), new Vector4(.56f, .16f, .38f, 1), new Vector4(.36f, .4f, .34f, .9f), new Vector4(.24f, .66f, .3f, .7f),
            new Vector4(.2f, .8f, .32f, .6f), new Vector4(.3f, .89f, .5f, .7f), new Vector4(.26f, .96f, .44f, .5f), new Vector4(0, 1f, .36f, .2f),
        }, 10);

        /// <summary>A flat blob with a typed outline and the pool's own edge: surface, bright lip, dark line.</summary>
        private static Mesh NmBlobMesh(string name, float[] outline, float lip, Color centre, Color surface, Color lipColour, Color edge)
        {
            int sides = outline.Length * 4;
            var ink = new NmInk(name);
            ink.Begin();
            for (int row = 0; row < 5; row++)
                for (int s = 0; s < sides; s++)
                {
                    float degrees = s * 360f / sides, radians = degrees * Mathf.Deg2Rad, rim = NmTyped(outline, degrees);
                    float r = row == 0 ? 0 : row == 1 ? rim * .55f : row == 2 ? rim - lip * 3 : row == 3 ? rim - lip : rim;
                    ink.V(new Vector3(Mathf.Sin(radians) * r, 0, Mathf.Cos(radians) * r),
                        row == 0 ? centre : row == 1 ? Color.Lerp(centre, surface, .5f) : row == 2 ? surface : row == 3 ? lipColour : edge);
                }
            ink.Grid(0, 5, sides, true);
            ink.End();
            return ink.Mesh;
        }

        /// <summary>A ripple: a raised bright crest with the dark falling away on both sides of it, unit radius.</summary>
        private static Mesh NmRippleMesh()
        {
            var ink = new NmInk("Nemu ink ripple");
            ink.Begin();
            var none = NmInkSheen; none.a = 0; var out0 = NmInkEdge; out0.a = 0;
            for (int row = 0; row < 5; row++)
                for (int s = 0; s < 48; s++)
                {
                    float a = s * Mathf.PI / 24, r = row == 0 ? .7f : row == 1 ? .86f : row == 2 ? .93f : row == 3 ? .97f : 1f;
                    ink.V(new Vector3(Mathf.Sin(a) * r, 0, Mathf.Cos(a) * r), row == 0 ? none : row == 1 ? NmInkSheen : row == 2 ? NmInkLip : row == 3 ? NmInkEdge : out0);
                }
            ink.Grid(0, 5, 48, true);
            ink.End();
            return ink.Mesh;
        }

        /// <summary>A ring of far shapes round the stage: a typed skyline, standing up from the floor or hanging down from the top.</summary>
        private static Mesh NmRidgeMesh(string name, float radius, float[] heights, bool hanging, Color body, Color line)
        {
            const int Around = 128;
            var ink = new NmInk(name);
            ink.Begin();
            for (int row = 0; row < 3; row++)
                for (int a = 0; a < Around; a++)
                {
                    float degrees = a * 360f / Around, radians = degrees * Mathf.Deg2Rad;
                    float h = NmTyped(heights, degrees) * (.86f + .14f * Mathf.Sin(radians * 11));
                    float y = row == 0 ? 0 : row == 1 ? h - Mathf.Min(.35f, h * .3f) : h;
                    ink.V(new Vector3(Mathf.Sin(radians) * radius, hanging ? -y : y, Mathf.Cos(radians) * radius), row == 2 ? line : row == 1 ? body : Color.Lerp(body, NmInkDeep, .6f));
                }
            ink.Grid(0, 3, Around, true);
            ink.End();
            return ink.Mesh;
        }

        /// <summary>A four-pointed sparkle, white at its heart.</summary>
        private static Mesh NmStarMesh()
        {
            var ink = new NmInk("Nemu sparkle");
            ink.Begin();
            int middle = ink.V(Vector3.zero, Color.white);
            for (int s = 0; s < 8; s++)
            {
                float a = s * Mathf.PI / 4, r = s % 2 == 0 ? 1f : .22f;
                ink.V(new Vector3(Mathf.Sin(a) * r, Mathf.Cos(a) * r, 0), s % 2 == 0 ? new Color(.8f, .68f, 1f, 1f) : NmWhite);
            }
            ink.Fan(middle, 1);
            ink.End();
            return ink.Mesh;
        }

        /// <summary>One jaw as the lens sees it from inside his mouth: the gum is ink and fills everything behind the teeth,
        /// so as the jaws meet the picture is already dark where they have been. Teeth hang down from y = 0; the left half is
        /// typed and the right is its mirror.</summary>
        private static Mesh NmJawMesh(string name, Vector3[] teeth)
        {
            var ink = new NmInk(name);
            ink.Begin();
            var gumLine = new Color(.24f, .11f, .4f, 1f);
            int a = ink.V(new Vector3(-4, 0, 0), gumLine), b = ink.V(new Vector3(4, 0, 0), gumLine), c = ink.V(new Vector3(4, .3f, 0), NmInkDeep),
                d = ink.V(new Vector3(-4, .3f, 0), NmInkDeep), e = ink.V(new Vector3(4, 4, 0), NmInkDeep), f = ink.V(new Vector3(-4, 4, 0), NmInkDeep);
            ink.Tri(a, b, c); ink.Tri(a, c, d); ink.Tri(d, c, e); ink.Tri(d, e, f);
            var root = new Color(.42f, .3f, .72f, 1f); var side = new Color(.8f, .72f, .98f, 1f);
            foreach (var tooth in teeth)
                for (int mirror = 0; mirror < (tooth.x < -.001f ? 2 : 1); mirror++)
                {
                    float x = mirror == 0 ? tooth.x : -tooth.x, w = tooth.y, l = tooth.z;
                    int heart = ink.V(new Vector3(x, -l * .5f, 0), NmWhite); int first = ink.Count;
                    ink.V(new Vector3(x - w * .5f, .04f, 0), root); ink.V(new Vector3(x + w * .5f, .04f, 0), root);
                    ink.V(new Vector3(x + w * .4f, -l * .5f, 0), side); ink.V(new Vector3(x + w * .2f, -l * .88f, 0), NmWhite);
                    ink.V(new Vector3(x, -l, 0), NmWhite);
                    ink.V(new Vector3(x - w * .2f, -l * .88f, 0), NmWhite); ink.V(new Vector3(x - w * .4f, -l * .5f, 0), side);
                    ink.Fan(heart, first);
                }
            ink.End(12);
            return ink.Mesh;
        }

        // ------------------------------------------------------------------ build

        private void BuildNemu(CharacterMotor source)
        {
            var companion = source.GetComponent<CharacterVisual>()?.Companion ?? FilmCompanion;
            if (companion == null) throw new InvalidOperationException("Nemu's introduction requires retained Kuro.");
            var track = new MatchPoseHistory.Track(source, companion.gameObject);
            track.Record(0); track.Record(.05f); _kuro = track.Clone(_root.transform);
            if (_kuro == null) throw new InvalidOperationException("Kuro exceeded the render-copy contract.");
            track.Apply(_kuro, .05f); _kuro.Root.SetActive(true);
            _kuro.Root.transform.localPosition = KuroSeat;
            _kuro.Root.transform.localRotation = Quaternion.Euler(0, -18, 0);
            // The live pet's idle fidget can stretch its current scale.
            // Use the same canonical base as the actual devour, not that transient pose.
            _kuroScale = companion.RestScale;
            Vector3 face = companion.MouthPosition - companion.transform.position; face.y = 0;
            Vector3 localFace = companion.transform.InverseTransformDirection(face.normalized);
            _kuroFront = localFace.sqrMagnitude > .01f ? Quaternion.FromToRotation(localFace, Vector3.forward) : Quaternion.identity;
            var calm = GhostPetCompanion.FindForm(_kuro.Root.transform, "CalmForm");
            var rage = GhostPetCompanion.FindForm(_kuro.Root.transform, "RageForm");
            if (calm == null || rage == null) throw new InvalidOperationException("Retained Kuro calm/rage forms are missing.");
            // The live helper has already made six inactive eye wisps.
            // Copies of those have no timeline; the private helper below
            // owns fresh ones. Do not grow dormant source spheres too.
            foreach (var bone in _kuro.Bones)
                if (bone.name == "KuroEyeWisp")
                { bone.gameObject.SetActive(false); bone.SetParent(_root.transform, false); ObjectDestroy(bone.gameObject); }
            _rage = new KuroRagePresentation(_kuro.Root, calm, rage);
            _kuroRenderers = _kuro.Root.GetComponentsInChildren<Renderer>(true);
            NmDressGiantTail(rage);
            foreach (var renderer in _kuroRenderers)
            {
                if (renderer.name == "RageEyeLeft") _nmGiantEyeLeft = renderer;
                if (renderer.name == "RageEyeRight") _nmGiantEyeRight = renderer;
            }

            // ---- the night: the wall, its top, and what stands and hangs far off inside it. Drawn first, in this order.
            _inkWall = AddInk("InkRising", (_nmCurtain = new NmInk("Nemu ink curtain")).Mesh, -12);
            _nmCeiling = AddInk("InkTop", NmBlobMesh("Nemu ink top", new[] { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f }, .1f, NmInkDeep, NmInkDeep, NmInkDeep, NmInkDeep), -12);
            _nmRidgeFar = AddInk("NightFar", NmRidgeMesh("Nemu far ridge", 15.6f, NmRidgeFar, false, new Color(.07f, .03f, .125f, 1f), new Color(.19f, .09f, .32f, 1f)), -11);
            _nmHang = AddInk("NightHanging", NmRidgeMesh("Nemu hanging ink", 15.4f, NmHangs, true, new Color(.04f, .015f, .075f, 1f), new Color(.2f, .1f, .34f, 1f)), -11);
            for (int i = 0; i < 8; i++)
            {
                var eye = new NmEye { Ink = new NmInk("Nemu wall eye") };
                eye.Piece = AddInk("InkEye" + i, eye.Ink.Mesh, -9, NmLilac);
                eye.Glow = AddGlow("InkEyeGlow" + i, new Color(.62f, .46f, 1f, 1f), billboard: false, falloff: 2.4f, core: .2f);
                eye.Core = AddGlow("InkEyeCore" + i, new Color(.9f, .78f, 1f, 1f), billboard: false, falloff: 3.2f, core: 1.2f);
                _inkEyes.Add(eye);
            }
            _nmRidgeNear = AddInk("NightNear", NmRidgeMesh("Nemu near ridge", 14.4f, NmRidgeNear, false, new Color(.034f, .013f, .064f, 1f), new Color(.15f, .07f, .26f, 1f)), -7);
            for (int i = 0; i < NmCeilingDrops.Length; i++) _nmDrops.Add(AddDrawn("TopDrop" + i, NmHangMesh(), .07f));
            for (int i = 0; i < NmWispAt.Length; i++)
            {
                int wisp = AddGlow("NightWisp" + i, new Color(.56f, .42f, .95f, 1f), billboard: false, band: true, falloff: 1.6f, core: .15f);
                // Soft at both ends as well as both edges: a wisp has no end.
                _pieces[wisp].Renderer.sharedMaterial.SetFloat("_Tips", 1.4f);
                _nmWisps.Add(wisp);
            }
            for (int i = 0; i < NmMoteAt.Length; i++) _nmMotes.Add(AddGlow("NightMote" + i, new Color(.74f, .6f, 1f, 1f), falloff: 2.6f, core: .7f));

            // ---- the floor: the pool, what spatters ahead of it, and the slow shine lying on it.
            _inkSpill = AddInk("InkSpill", (_nmPool = new NmInk("Nemu ink pool")).Mesh, 0);
            for (int i = 0; i < NmSpatter.Length; i++)
                _nmBlobs.Add(AddInk("InkSpatter" + i, NmBlobMesh("Nemu ink spatter", new[] { 1f, .9f, 1.07f, .96f, .86f, 1.05f, 1f, .9f, 1.08f, .94f }, .14f, NmInkDeep, NmInkBody, NmInkLip, NmInkEdge), 1));
            for (int i = 0; i < 3; i++) _nmGlints.Add(AddGlow("InkShine" + i, new Color(.5f, .3f, .9f, 1f), billboard: false, falloff: 1.8f, core: .1f));
            var soft = NmInkDeep; soft.a = .62f; var faded = NmInkDeep; faded.a = 0;
            _nmShadow = AddInk("KuroInkShadow", NmBlobMesh("Nemu Kuro shadow", new[] { 1f, .93f, 1.05f, .96f, 1.02f, .9f, 1.04f, .95f }, .2f, soft, Color.Lerp(soft, faded, .35f), Color.Lerp(soft, faded, .75f), faded), 1);

            // ---- the dive.
            _nmDiveRing = AddInk("InkDiveRing", NmRippleMesh(), 2);
            _nmDiveRing2 = AddInk("InkDiveRingAfter", NmRippleMesh(), 2);
            _nmCrownPiece = AddDrawn("InkDiveCrown", (_nmCrown = new NmInk("Nemu dive crown")).Mesh, .012f);
            _nmJet = AddDrawn("InkDiveJet", NmHangMesh(), .12f);
            for (int i = 0; i < NmDivePeaks.Length; i++) _nmBeads.Add(AddDrawn("InkDiveBead" + i, NmDropMesh(), .15f));
            _nmDiveGlow = AddGlow("InkDiveLight", new Color(.66f, .5f, 1f, 1f), falloff: 2.2f, core: .4f);

            // ---- the rise.
            _nmPuddle = AddInk("InkRisePool", NmBlobMesh("Nemu rise pool", new[] { 1f, .84f, 1.12f, .9f, .78f, 1.06f, .94f, 1.16f, .86f, 1.02f, .8f, 1.1f, .92f, .82f, 1.08f, .88f }, .035f,
                NmInkDeep, Color.Lerp(NmInkBody, NmInkSheen, .35f), Color.Lerp(NmInkSheen, NmInkLip, .45f), NmInkEdge), 1);
            _nmRiseRing = AddInk("InkRiseRing", NmRippleMesh(), 2);
            _nmRiseRing2 = AddInk("InkRiseRingAfter", NmRippleMesh(), 2);
            _nmSwell = AddDrawn("InkSwell", NmSwellMesh(), .016f);
            _nmTearPiece = AddDrawn("InkTornCrown", (_nmTear = new NmInk("Nemu torn crown")).Mesh, .025f);
            for (int i = 0; i < NmTearPeaks.Length; i++) _nmTearBeads.Add(AddDrawn("InkTornBead" + i, NmDropMesh(), .13f));
            for (int i = 0; i < NmRopeHold.Length; i++)
            {
                var rope = new NmInk("Nemu ink rope"); _nmRopes.Add(rope);
                _nmRopePieces.Add(AddDrawn("InkRope" + i, rope.Mesh, .018f));
                // When this rope lets go: the first moment its hold is higher than it can stretch.
                _nmSnapAt[i] = NmRisen + .4f;
                for (float at = NmRise; at < NmRisen + .4f; at += .01f)
                    if (NmRopeHold[i].y + NmGiantLift(at) > NmRopeSnap[i].x) { _nmSnapAt[i] = at; break; }
            }
            for (int i = 0; i < NmDripAt.Length; i++)
            {
                _nmDripRings.Add(AddInk("InkDripRing" + i, NmRippleMesh(), 2));
                _nmDrips.Add(AddDrawn("InkDrip" + i, NmDropMesh(), .11f));
            }
            _nmRiseGlow = AddGlow("InkRiseLight", new Color(.56f, .4f, 1f, 1f), falloff: 2f, core: .25f);

            // Everyone else, where they stand, brought near enough for the high shot to hold them. Each gets a pair of
            // eyes in the floor under them.
            StageOthers(3f, 11f);
            for (int i = 0; i < _others.Count && i < 4; i++)
                for (int k = 0; k < 2; k++)
                {
                    var eye = new NmEye { Ink = new NmInk("Nemu floor eye") };
                    eye.Piece = AddInk("SeenEye" + i + "-" + k, eye.Ink.Mesh, 4, NmLilac);
                    eye.Glow = AddGlow("SeenEyeGlow" + i + "-" + k, new Color(.62f, .46f, 1f, 1f), billboard: false, falloff: 2.4f, core: .2f);
                    eye.Core = AddGlow("SeenEyeCore" + i + "-" + k, new Color(.9f, .78f, 1f, 1f), billboard: false, falloff: 3.2f, core: 1.2f);
                    _nmSeen.Add(eye);
                }
            // The last shot looks along the court from 6.5 m out: keep it off whoever stands there, once, so it does not slide.
            _nmLens = KeepLensOffOthers(NmLastLens, 1.8f);

            // ---- the send.
            for (int i = 0; i < NmStreamAt.Length; i++) _nmStreams.Add(AddDrawn("InkStream" + i, NmStreamMesh(), .08f));
            for (int i = 0; i < 3; i++) _nmStars.Add(AddInk("NuzzleSparkle" + i, NmStarMesh(), 9, NmLilac));
            _nmNuzzleGlow = AddGlow("NuzzleLight", new Color(.8f, .68f, 1f, 1f), falloff: 2.4f, core: .5f);

            // ---- his light, and everything that moves in it.
            _nmThroat = AddGlow("KuroThroatEmber", new Color(.82f, .48f, 1f, 1f), falloff: 2f, core: .7f);
            _nmThroatCast = AddGlow("KuroThroatLightOnInk", new Color(.6f, .36f, 1f, 1f), billboard: false, falloff: 1.7f, core: .15f);
            _nmEmberLeft = AddGlow("KuroEyeEmberLeft", new Color(.86f, .74f, 1f, 1f), falloff: 2.6f, core: 1f);
            _nmEmberRight = AddGlow("KuroEyeEmberRight", new Color(.86f, .74f, 1f, 1f), falloff: 2.6f, core: 1f);
            for (int i = 0; i < NmCometAt.Length; i++)
            {
                _nmComets.Add(AddGlow("KuroComet" + i, new Color(.8f, .66f, 1f, 1f), falloff: 2.4f, core: .9f));
                // Its tail is a torn ribbon: a pale core, violet where it thins, eaten from its far end.
                // ⚠️ INK, NOT PAPER. With a pale body these were hard lilac slivers beside his head (film p14). The body is dark
                // violet, the only light in it is the thin line where it tears, and it is narrow and long.
                _nmTrails.Add(UltimateFx.Wisp(_root.transform, "streak", new Color(.31f, .15f, .62f, 1f), new Color(.16f, .07f, .34f, 1f), i % 2 == 0 ? UltimateFx.Smoke : UltimateFx.Ink, 10)
                    .With("_Scale", new Vector4(2.6f + .3f * (i % 3), 1.6f, 0, 0)).With("_Scroll", new Vector4(2.2f, .2f, 0, 0)).With("_Sweep", -1.2f).With("_Shape", .9f).With("_Warp", 1.1f)
                    .With("_Band", .2f).With("_Hot", new Color(.7f, .54f, 1f, 1f)).With("_Glow", .8f).With("_Emit", .25f).With("_Soft", .08f));
            }
            for (int i = 0; i < NmLiftAt.Length; i++) _nmLifted.Add(AddDrawn("InkLifted" + i, NmDropMesh(), .11f));
            for (int i = 0; i < NmOrbitAt.Length; i++)
            {
                _nmOrbit.Add(AddInk("NemuSpark" + i, NmStarMesh(), 9, NmLilac));
                _nmOrbitLights.Add(AddGlow("NemuSparkLight" + i, new Color(.78f, .64f, 1f, 1f), falloff: 2.6f, core: .6f));
            }
            for (int i = 0; i < _others.Count && i < 4; i++)
            {
                _nmShockRings.Add(AddInk("SeenShockRing" + i, NmRippleMesh(), 3));
                for (int k = 0; k < NmShockWay.Length; k++) _nmShock.Add(AddGlow("SeenShockMote" + i + "-" + k, new Color(.84f, .7f, 1f, 1f), falloff: 2.4f, core: .9f));
            }
            for (int i = 0; i < NmRushAt.Length; i++)
            {
                _nmRushTrails.Add(UltimateFx.Wisp(_root.transform, "streak", new Color(.28f, .13f, .56f, 1f), new Color(.14f, .06f, .3f, 1f), i % 3 == 0 ? UltimateFx.Ink : UltimateFx.Smoke, 11)
                    .With("_Scale", new Vector4(2.0f, 1.6f, 0, 0)).With("_Scroll", new Vector4(4.2f, .2f, 0, 0)).With("_Sweep", -1f).With("_Shape", .9f).With("_Warp", 1.1f)
                    .With("_Band", .2f).With("_Hot", new Color(.68f, .52f, 1f, 1f)).With("_Glow", .8f).With("_Emit", .25f).With("_Soft", .08f));
            }

            // ---- ink smoke: dark in its body, violet where it tears.
            // ⚠️ DARK, WITH ONLY ITS TORN EDGE LIT. Given a bright edge band these read as shards of lilac paper stuck round his head
            // (film p13); smoke is a dark thing and the violet is the thin line where it is being eaten.
            var smokeBody = new Color(.17f, .075f, .34f, 1f); var smokeEdge = new Color(.38f, .21f, .74f, 1f); var smokeHot = new Color(.64f, .46f, 1f, 1f);
            for (int i = 0; i < NmSmokeAt.Length; i++)
                _nmSmoke.Add(UltimateFx.Wisp(_root.transform, NmSmokeShape[i], smokeBody, smokeEdge, i % 3 == 1 ? UltimateFx.Ink : UltimateFx.Smoke, 5)
                    .With("_Scale", new Vector4(1.8f + .25f * (i % 4), 1.3f, 0, 0)).With("_Scroll", new Vector4(.9f, .1f, 0, 0)).With("_Sweep", 1f).With("_Shape", .65f)
                    .With("_Band", .22f).With("_Hot", smokeHot).With("_Glow", .45f));
            _nmRiseWisps = UltimateFx.Wisp(_root.transform, "crown", smokeBody, smokeEdge, UltimateFx.Ink, 5)
                .With("_Scale", new Vector4(1.7f, 1.6f, 0, 0)).With("_Scroll", new Vector4(2.2f, .2f, 0, 0)).With("_Sweep", .9f).With("_Shape", .7f).With("_Band", .3f)
                .With("_Hot", smokeHot).With("_Glow", .7f);
            _nmRiseWash = UltimateFx.Disc(_root.transform, new Color(.07f, .03f, .15f, 1f), new Color(.26f, .13f, .52f, 1f), UltimateFx.Ink, 3)
                .With("_Hot", smokeHot).With("_Glow", .3f).With("_Band", .2f).With("_Scale", new Vector4(1.1f, 8f, 0, 0)).With("_Spread", 1.3f);
            for (int i = 0; i < NmTornAt.Length; i++)
                _nmTorn.Add(UltimateFx.Wisp(_root.transform, NmTornShape[i], smokeBody, smokeEdge, i % 2 == 0 ? UltimateFx.Ink : UltimateFx.Smoke, 9)
                    .With("_Scale", new Vector4(2.1f, 1.5f, 0, 0)).With("_Scroll", new Vector4(3.4f, 0, 0, 0)).With("_Sweep", -1.1f).With("_Shape", .7f).With("_Band", .32f)
                    .With("_Hot", smokeHot).With("_Glow", .6f));
            for (int i = 0; i < 2; i++)
                _nmOrbRing[i] = UltimateFx.Wisp(_root.transform, "arc", new Color(.9f, .8f, 1f, 1f), new Color(.5f, .3f, .95f, 1f), UltimateFx.Streak, 12)
                    .With("_Scale", new Vector4(1.4f, 1.2f, 0, 0)).With("_Scroll", new Vector4(3f, 0, 0, 0)).With("_Shape", .8f).With("_Glow", .9f).With("_Emit", .7f);
            // What eats the picture at the end: ink closing on the middle of the lens, burning violet where it closes.
            // ⚠️ BLOTS, NOT RAYS. Eaten from its middle out along a streaked mask, this was a clean burst of straight black rays:
            // the comic language the owner turned down for Rafi (film p14). Ink does not travel in straight lines. Two sheets
            // of soft warped cells, torn by a plain threshold: blots open all over the picture, spread and run together, the
            // first thickest at its edges and the second a moment later from everywhere at once, his throat included.
            _nmEaten = UltimateFx.Wisp(_root.transform, "card", new Color(.012f, 0f, .02f, 1f), new Color(.2f, .08f, .42f, 1f), UltimateFx.Cells, 14)
                .With("_Radial", 0f).With("_Shape", 0f).With("_Sweep", 0f).With("_Scale", new Vector4(3.4f, 3.0f, 0, 0)).With("_Scroll", new Vector4(.25f, .18f, 0, 0)).With("_Warp", 1.6f)
                .With("_Band", .16f).With("_Hot", new Color(.66f, .46f, 1f, 1f)).With("_Glow", .9f).With("_Soft", .05f);
            _nmEatenAll = UltimateFx.Wisp(_root.transform, "card", new Color(.012f, 0f, .02f, 1f), new Color(.2f, .08f, .42f, 1f), UltimateFx.Cells, 14)
                .With("_Radial", 0f).With("_Shape", 0f).With("_Sweep", 0f).With("_Scale", new Vector4(2.1f, 2.4f, 0, 0)).With("_Scroll", new Vector4(-.2f, .25f, 0, 0)).With("_Warp", 1.8f)
                .With("_Band", .16f).With("_Hot", new Color(.66f, .46f, 1f, 1f)).With("_Glow", .9f).With("_Soft", .05f);

            // The swallow: black all round the last lens. Unlit, so it is black and not a dark grey surface in the sun.
            _nmShroud = Add("Swallowed", VfxShapes.Prism(12, 1, 1), new Color(.02f, 0, .03f, 1f), 0f, plain: true);
            NmUnlit(_nmShroud, 15, true);
            // His teeth close over the lens inside the black, so they are drawn after it.
            _nmJawUp = AddInk("SwallowTeethUpper", NmJawMesh("Nemu upper teeth", NmTeethUp), 20, NmLilac);
            _nmJawDown = AddInk("SwallowTeethLower", NmJawMesh("Nemu lower teeth", NmTeethDown), 20, NmLilac);
            _nmJawGlowUp = AddGlow("SwallowTeethLightUpper", new Color(.88f, .82f, 1f, 1f), billboard: false, band: true, falloff: 1.5f, core: .3f);
            _nmJawGlowDown = AddGlow("SwallowTeethLightLower", new Color(.88f, .82f, 1f, 1f), billboard: false, band: true, falloff: 1.5f, core: .3f);
        }

        /// <summary>When he looks at the i-th other player, and their eyes in the floor open.</summary>
        private static float NemuSeenAt(int i) => NmRisen + .02f + i * .3f;

        /// <summary>How high the giant's root is. He comes up fast and settles: a little too far, then back.</summary>
        private static float NmGiantLift(float t)
        {
            float rise = Ease(NmRise, NmRisen, t);
            float over = Mathf.Sin(Mathf.Clamp01((t - (NmRisen - .25f)) / .7f) * Mathf.PI) * .45f;
            return Mathf.Lerp(-9.5f, -NmGiantSink, rise) + over + Mathf.Sin(t * 1.3f) * .12f * rise;
        }

        /// <summary>The last lens trembles as he comes at it. One place, because what is fixed to the lens (his teeth) must shake with it.</summary>
        private Vector3 NemuTremble(float t)
        {
            if (_reducedEffects) return Vector3.zero;
            float go = Mathf.Clamp01((t - NmLunge) / (NmGulp - NmLunge));
            return new Vector3(Mathf.Sin(t * 71f), Mathf.Sin(t * 83f + 1.3f), 0) * (.035f * go * go);
        }

        /// <summary>Where the lens is and what it looks at, in the scene's space: the eyes on the wall watch it.</summary>
        private void NmLensAt(float t, out Vector3 eye, out Vector3 look, out float fov)
        {
            eye = NmLastLens; look = new Vector3(0, 3, -3); fov = 52;
            if (_performance == null || _performance.Shots.Count == 0) return;
            int index = _performance.ShotIndexAt(t);
            _performance.Shot(index, t, out eye, out look, out fov);
            if (index == 4)
            {
                var tremble = NemuTremble(t);
                eye = _nmLens + tremble; look += tremble;
            }
            NmFitGiant(index, t, ref eye, ref look, ref fov);
        }

        /// <summary>
        /// ⚠️ MOVE A TYPED SHOT UNTIL THE WHOLE OF HIM IS IN IT, under the banner, with Nemu. Everything is in the scene's own
        /// space. The eight corners of his box are taken through his real transform, so his size, his lean and his rearing
        /// back are all in it; what is under the floor is not counted, so the lens follows him up as he rises.
        /// </summary>
        private void NmFitGiant(int index, float t, ref Vector3 eye, ref Vector3 look, ref float fov)
        {
            float weight = Ease(NmRise + .05f, NmRise + .7f, t) * (1 - Ease(NmLunge, NmLunge + .4f, t));
            if (weight <= 0 || _kuro == null || _kuro.Root == null || index < 0 || index >= _nmPull.Length) return;
            var body = _kuro.Root.transform;
            int count = 0;
            for (int corner = 0; corner < 8; corner++)
            {
                var p = _root.transform.InverseTransformPoint(body.TransformPoint(new Vector3((corner & 1) == 0 ? NmBodyLow.x : NmBodyHigh.x,
                    (corner & 2) == 0 ? NmBodyLow.y : NmBodyHigh.y, (corner & 4) == 0 ? NmBodyLow.z : NmBodyHigh.z)));
                p.y = Mathf.Max(0, p.y);
                _nmFit[count++] = p;
            }
            _nmFit[count++] = Vector3.zero; _nmFit[count++] = new Vector3(0, 1.9f, 0);

            float pull = _nmPull[index], reach = (eye - look).magnitude;
            if (reach < .01f) return;
            // ⚠️ THE LOW SHOTS STAY ON NEMU'S SIDE OF THE OTHERS. Drawn back as far as it took to hold him, the lens went out
            // past where the other players stand and looked at him between them: one filled the left of the picture and one
            // the right (film p10). From the floor the lens may go no further from her than the nearest of them does, less a
            // stride, and a wider lens holds what that leaves out: low, close and wide is the shot this always was.
            float furthest = NmStageRadius - 1f;
            if (index >= 3)
            {
                float nearest = 12f;
                foreach (var other in _others) nearest = Mathf.Min(nearest, other.Reach);
                furthest = Mathf.Max(new Vector2(eye.x, eye.z).magnitude, Mathf.Min(nearest - 1.2f, 7f));
            }
            var from = look + (eye - look) * pull;
            float tanV = Mathf.Tan(fov * .5f * Mathf.Deg2Rad);
            float aspect = Mathf.Clamp(Screen.height > 0 ? Screen.width / (float)Screen.height : 16f / 9, 1.2f, 2.4f);
            Vector3 at = from, aim = look;
            for (int pass = 0; pass < 5; pass++)
            {
                var forward = (aim - at).normalized;
                var right = Vector3.Cross(Vector3.up, forward);
                if (right.sqrMagnitude < 1e-5f) break;
                right.Normalize();
                var up = Vector3.Cross(forward, right);
                // Aim: as much room over his ears, under the banner, as there is under Nemu's feet.
                float high = float.NegativeInfinity, low = float.PositiveInfinity;
                for (int i = 0; i < count; i++)
                {
                    var v = _nmFit[i] - at; float depth = Mathf.Max(.5f, Vector3.Dot(v, forward)), slope = Vector3.Dot(v, up) / depth;
                    high = Mathf.Max(high, slope); low = Mathf.Min(low, slope);
                }
                float tilt = Mathf.Atan(.5f * (high + low - (NmFrameTop - NmFrameBottom) * tanV));
                forward = (forward * Mathf.Cos(tilt) + up * Mathf.Sin(tilt)).normalized;
                up = Vector3.Cross(forward, right);
                aim = at + forward * Mathf.Max(4f, (aim - at).magnitude);
                // Back: far enough that every corner is inside. Never nearer than the typed shot.
                float back = 0, wide = 1;
                for (int i = 0; i < count; i++)
                {
                    var v = _nmFit[i] - at; float depth = Vector3.Dot(v, forward), x = Mathf.Abs(Vector3.Dot(v, right)), y = Vector3.Dot(v, up);
                    back = Mathf.Max(back, Mathf.Max(y / (NmFrameTop * tanV), Mathf.Max(-y / (NmFrameBottom * tanV), x / (NmFrameSide * tanV * aspect))) - depth);
                    float near = Mathf.Max(.5f, depth);
                    wide = Mathf.Max(wide, Mathf.Max(y / near / (NmFrameTop * tanV), Mathf.Max(-y / near / (NmFrameBottom * tanV), x / near / (NmFrameSide * tanV * aspect))));
                }
                if (pull >= .98f && back > 0 && pass < 3)
                {
                    at -= forward * back;
                    // Inside the stage: over the floor, under the top, within the wall.
                    at.y = Mathf.Clamp(at.y, .4f, NmStageTop - 2f);
                    var flat = new Vector2(at.x, at.z);
                    if (flat.magnitude > furthest) { flat = flat.normalized * furthest; at.x = flat.x; at.z = flat.y; }
                    // And never inside one of the others: drawn back this far, the low shots reached where they stand (film p9 had a
                    // player's head filling the right of the picture).
                    at = KeepLensOffOthers(at, 2.2f);
                }
                else if (wide > 1.001f)
                    // Nowhere further back to stand: a wider lens, as far as one still reads as a lens.
                    tanV = Mathf.Min(tanV * wide, Mathf.Tan(52f * Mathf.Deg2Rad));
            }
            at = Vector3.Lerp(from, at, weight); look = Vector3.Lerp(look, aim, weight);
            fov = Mathf.Lerp(fov, 2 * Mathf.Atan(tanV) * Mathf.Rad2Deg, weight);
            // Hand it back as the typed shot is handed over: before the map's push-in, which is put on after this.
            eye = look + (at - look) / Mathf.Max(.2f, pull);
        }

        /// <summary>Where an eye's iris sits when it looks at `target`: pushed toward it inside the white, further the more side-on it is.</summary>
        private static Vector2 NmLook(Vector3 at, Quaternion facing, Vector3 target)
        {
            var local = Quaternion.Inverse(facing) * (target - at);
            var flat = new Vector2(local.x, local.y); float size = flat.magnitude;
            if (size < .001f) return Vector2.zero;
            float amount = Mathf.Clamp01(size / (Mathf.Abs(local.z) + .001f) * 1.5f);
            return new Vector2(flat.x / size * .36f * amount, flat.y / size * .13f * amount);
        }

        private void NmPlaceEye(NmEye eye, Vector3 at, Quaternion facing, float size, float open, Vector2 look, float slit, float light)
        {
            bool shown = open > .004f;
            if (shown) NmFillEye(eye.Ink, open, look, slit);
            Place(eye.Piece, at, Vector3.one * size, facing, shown ? 1 : 0);
            PlaceGlow(eye.Glow, at, new Vector3(size * 3.6f, size * 2.3f, 1), facing, shown ? light * Mathf.Clamp01(open) : 0);
            // The iris is lit from inside, and the light goes where it looks.
            PlaceGlow(eye.Core, at + facing * new Vector3(look.x * size, look.y * size, 0), new Vector3(size * 1.15f, size * 1.15f * Mathf.Clamp01(open), 1), facing,
                shown ? (.55f + light) * Mathf.Clamp01(open * 1.4f - .4f) : 0);
        }

        private void SampleNemu(float t)
        {
            // ------------------------------------------------------------- little Kuro, up to the dive
            float bob = Mathf.Sin(t * 2.4f) * .06f;
            Vector3 atHand = FreePalm + new Vector3(-.2f, .12f, .12f);
            Vector3 nudge = new Vector3(-.55f, .95f, .25f);
            Vector3 behind = new Vector3(-.75f, .8f, -.35f);
            Vector3 at = KuroSeat + Vector3.up * bob;
            at = Vector3.Lerp(at, nudge, Ease(.52f, .7f, t) * (1 - Ease(.78f, .98f, t)));
            at = Vector3.Lerp(at, atHand + Vector3.up * bob * .5f, Ease(1.2f, 1.45f, t) * (1 - Ease(1.72f, 1.95f, t)));
            at = Vector3.Lerp(at, behind + Vector3.up * bob, Ease(1.8f, 2.1f, t));
            // One hop, then down into the ink. The plunge accelerates: he falls, he is not lowered.
            float hop = Mathf.Sin(Mathf.Clamp01((t - (NmDive - .24f)) / .34f) * Mathf.PI);
            float dive = Mathf.Clamp01((t - (NmDive + .06f)) / .3f); dive *= dive;
            // ⚠️ THE HOP IS AN ARC WITH A CROUCH BEFORE IT, not a rise and a drop: he gathers, goes up and a little forward,
            // tips his nose down at the top and falls in along the curve.
            float crouch = Ease(NmDive - .33f, NmDive - .25f, t) * (1 - Ease(NmDive - .24f, NmDive - .14f, t));
            float tip = Mathf.Lerp(-12f * hop, 40f, Ease(NmDive, NmDive + .3f, t));
            at += Vector3.up * (hop * .3f - crouch * .07f) + Vector3.forward * (hop * .16f);
            at = Vector3.Lerp(at, new Vector3(-.75f, -1.3f, -.35f), dive);
            float turn = Mathf.Lerp(-18, 20, Ease(.5f, .7f, t) * (1 - Ease(.8f, 1f, t)));
            float nuzzle = Ease(1.42f, 1.5f, t) * (1 - Ease(1.55f, 1.7f, t));
            var squash = new Vector3(1 + nuzzle * .12f - dive * .3f + crouch * .1f, 1 - nuzzle * .1f + dive * .55f - crouch * .16f, 1 + nuzzle * .12f - dive * .3f + crouch * .1f);

            NmLensAt(t, out var lens, out var lensLook, out float lensFov);
            Vector3 maw = NmGiantAt;
            float go = 0;
            bool giant = t >= NmRise - .04f;
            if (!giant)
            {
                _kuro.Root.transform.localPosition = at;
                _kuro.Root.transform.localRotation = Quaternion.Euler(0, turn, 0) * Quaternion.Euler(tip, 0, 0) * _kuroFront;
                _kuro.Root.transform.localScale = Vector3.Scale(_kuroScale, squash);
                _rage.Sample(0, t, .12f);
            }
            else
            {
                // --------------------------------------------------------- the giant
                var pos = NmGiantAt + Vector3.up * NmGiantLift(t);
                // His head goes to each of them in turn, then comes back to her.
                float yaw = 0;
                for (int i = 0; i < _others.Count && i < 4; i++)
                {
                    float s = NemuSeenAt(i);
                    float want = Mathf.Clamp(Mathf.Atan2(_others[i].Home.x - NmGiantAt.x, _others[i].Home.z - NmGiantAt.z) * Mathf.Rad2Deg, -70f, 70f);
                    yaw = Mathf.Lerp(yaw, want, Ease(s - .12f, s + .04f, t));
                }
                yaw = Mathf.Lerp(yaw, 0, Ease(NmLean - .25f, NmLean + .1f, t));
                // He leans over her for the silhouette, rears back, and goes.
                float lean = Ease(NmLean, NmLean + .7f, t) * 12f;
                float rear = Ease(NmRear, NmLunge, t);
                // ⚠️ ONE MOVE: he gathers back and up, and the pounce leaves from the bottom of that gather.
                pos += new Vector3(0, 1.0f, -1.7f) * rear;
                lean -= rear * 20f;
                go = Mathf.Clamp01((t - NmLunge) / (NmGulp - NmLunge)); go = go * go * (.4f + .6f * go);
                lean += go * 26f;
                // He is soft: long while he climbs, short and wide while he gathers, long again as he goes.
                float stretch = 1 + .09f * Mathf.Sin(Mathf.PI * Ease(NmRise, NmRisen, t)) - .08f * rear * (1 - Ease(NmLunge, NmLunge + .15f, t)) + .1f * Ease(NmLunge, NmLunge + .3f, t);
                float narrow = 1 / Mathf.Sqrt(stretch);
                var root = _kuro.Root.transform;
                root.localRotation = Quaternion.Euler(lean, yaw, 0) * _kuroFront;
                root.localScale = Vector3.Scale(_kuroScale, new Vector3(narrow, stretch, narrow)) * (GhostPetCompanion.DevourScale * NmGiant * (1 + go * .25f));
                root.localPosition = pos;
                // ---- what he is doing, for his rig to act out.
                var life = new KuroLife();
                float before = 0;
                for (int i = 0; i < _others.Count && i < 4; i++)
                {
                    float s = NemuSeenAt(i), age = t - (s - .1f);
                    float want = Mathf.Clamp(Mathf.Atan2(_others[i].Home.x - NmGiantAt.x, _others[i].Home.z - NmGiantAt.z) * Mathf.Rad2Deg, -70f, 70f);
                    // A whip: his head gets there before his body, goes past, and comes back in two swings.
                    if (age > 0) { float way = Mathf.Sign(want - before + .01f); life.HeadYaw += way * 24f * Mathf.Exp(-5.5f * age) * Mathf.Cos(age * 14f); life.HeadRoll -= way * 11f * Mathf.Exp(-4f * age) * Mathf.Sin(age * 9f); }
                    before = want;
                }
                // He looks down at them while he finds them, and at her while he leans over her.
                life.HeadPitch = 9f * Ease(NmRisen - .2f, NmRisen + .2f, t) * (1 - rear) + 6f * Ease(NmLean, NmLean + .5f, t) * (1 - rear);
                // The roar as he arrives: the jaw drops fast and closes slowly.
                float roar = Mathf.Clamp01((t - (NmRisen - .22f)) / .14f) * (1 - Ease(NmRisen + .1f, NmRisen + .75f, t));
                life.Jaw = 20f * roar - 4f * Ease(NmLean, NmLean + .4f, t) * (1 - rear);
                life.Coil = rear * (1 - Ease(NmLunge - .02f, NmLunge + .14f, t));
                life.Throw = Ease(NmLunge - .02f, NmLunge + .22f, t);
                // Up out of the ink long, and a jiggle as he settles.
                float settled = t - NmRisen;
                life.Swell = .07f * Mathf.Sin(Mathf.PI * Ease(NmRise, NmRisen, t)) + (settled > 0 ? .06f * Mathf.Exp(-4.5f * settled) * Mathf.Sin(settled * 21f) : 0);
                _rage.Sample(1, t, 0, life);
                if (go > 0)
                {
                    // His MOUTH goes to the lens, wherever the mouth is on him at this size and lean, and on through it.
                    Vector3 mawFrom = _root.transform.InverseTransformPoint(_rage.MawPosition) - pos;
                    Vector3 through = (_nmLens - (pos + mawFrom)).normalized * 2.2f;
                    // Over her head in an arc and down onto the lens, not along a ruler laid between the two.
                    root.localPosition = Vector3.Lerp(pos, _nmLens - mawFrom + through, go) + Vector3.up * (.9f * Mathf.Sin(Mathf.PI * go) * (1 - go));
                }
                maw = _root.transform.InverseTransformPoint(_rage.MawPosition);
            }

            // ------------------------------------------------------------- the calm part's two small things
            // His shadow is ink already: it is what the pool grows out of. It shrinks as he lifts and goes down with him.
            float shadowSize = Mathf.Clamp(.44f - (at.y - .65f) * .22f, .16f, .5f);
            Place(_nmShadow, new Vector3(at.x, .028f, at.z), new Vector3(shadowSize, 1, shadowSize), Quaternion.Euler(0, t * 14f, 0),
                giant ? 0 : Ease(0, .3f, t) * (1 - Ease(NmDive + .1f, NmDive + .3f, t)));
            // He nuzzles her hand and it shows: three sparkles, one after another, turned to the lens.
            for (int i = 0; i < _nmStars.Count; i++)
            {
                float age = (t - (1.44f + i * .07f)) / .34f;
                float pop = age > 0 && age < 1 ? Mathf.Sin(Mathf.PI * Mathf.Pow(age, .6f)) : 0;
                var spot = atHand + (i == 0 ? new Vector3(.02f, .24f, .05f) : i == 1 ? new Vector3(-.17f, .13f, 0) : new Vector3(.17f, .1f, .02f)) + Vector3.up * age * .06f;
                var toLens = lens - spot;
                Place(_nmStars[i], spot, Vector3.one * ((i == 0 ? .13f : .09f) * pop),
                    (toLens.sqrMagnitude > .01f ? Quaternion.LookRotation(toLens, Vector3.up) : Quaternion.identity) * Quaternion.Euler(0, 0, age * 70f + i * 20f), pop > 0 ? 1 : 0);
            }
            PlaceGlow(_nmNuzzleGlow, atHand + Vector3.up * .14f, Vector3.one * .55f, Quaternion.identity, nuzzle * .3f);

            // ------------------------------------------------------------- the ink
            // It pools under Kuro the moment she looks at the viewer, then runs out over the whole court.
            float spread = Ease(1.88f, NmRise, t), wet = Ease(1.88f, 1.98f, t);
            if (wet > 0) NmFillPool(t, spread);
            // ⚠️ JUST ABOVE THE COURT, AND EVERYTHING FLAT STACKS FROM HERE: a flat effect at the floor's own height fights it.
            Place(_inkSpill, Vector3.up * .03f, Vector3.one, Quaternion.identity, wet);
            float size = Mathf.Lerp(.3f, 17.5f, spread);
            var poolMiddle = Vector3.Lerp(new Vector3(-1.2f, 0, -.2f), Vector3.zero, spread);
            for (int i = 0; i < _nmBlobs.Count; i++)
            {
                var b = NmSpatter[i];
                // It lands a little ahead of the rim, swells, and is gone the moment the rim runs over it.
                float ahead = 1.2f + b.y * .16f, lands = Mathf.Clamp01((size - (b.y - ahead)) / (ahead * .45f)), over = Mathf.Clamp01((size - b.y) / (b.z * 1.5f + .2f));
                float swell = lands * (1 + .3f * Mathf.Sin(lands * Mathf.PI));
                Place(_nmBlobs[i], poolMiddle + Quaternion.Euler(0, b.x, 0) * Vector3.forward * b.y + Vector3.up * .033f, new Vector3(b.z * swell, 1, b.z * swell),
                    Quaternion.Euler(0, b.x * 3, 0), wet > 0 && lands > 0 ? 1 - over : 0);
            }
            for (int i = 0; i < _nmGlints.Count; i++)
            {
                // A slow shine lying on the surface: light that falls away softly is the half of "glossy" a painted colour cannot do.
                var drift = new Vector3(Mathf.Sin(t * .31f + i * 2.1f) * 4.5f + (i - 1) * 5f, .06f, Mathf.Cos(t * .27f + i * 1.7f) * 3f - 2f - i * 2.5f);
                PlaceGlow(_nmGlints[i], drift, new Vector3(9f + i * 2, 6f, 1), Quaternion.Euler(90, 25 + i * 40, 0), .035f * Ease(NmDark, NmRise, t));
            }

            // ⚠️⚠️ THE STAGE ENCLOSES THE CAMERA. At 8 m the old fitted shot left the wall and filmed its outside (solid black
            // frames, `Logs/review-v3/nemu-introduction-scene`, 2026-09-24). At 16 m every lens here, the high one at 12.5 m
            // out included, is inside it.
            float climb = Ease(NmDark - .2f, NmRise, t), night = Ease(NmRise - .3f, NmRise + .4f, t);
            if (climb > 0) NmFillCurtain(t, climb, Ease(NmRise - .05f, NmRise + .3f, t));
            Place(_inkWall, Vector3.zero, Vector3.one, Quaternion.identity, Ease(NmDark - .2f, NmDark - .1f, t));
            Place(_nmCeiling, Vector3.up * NmStageTop, Vector3.one * (NmStageRadius + .3f), Quaternion.identity, Ease(NmDark + .2f, NmRise - .05f, t));
            // The far shapes come up with the wall and settle a little late, the near ones first.
            float near = Ease(NmDark + .2f, NmRise + .1f, t), far = Ease(NmDark + .3f, NmRise + .25f, t);
            Place(_nmRidgeNear, Vector3.zero, new Vector3(1, near * (1 + .08f * Mathf.Sin(near * Mathf.PI)), 1), Quaternion.identity, near > 0 ? 1 : 0);
            Place(_nmRidgeFar, Vector3.zero, new Vector3(1, far * (1 + .06f * Mathf.Sin(far * Mathf.PI)), 1), Quaternion.Euler(0, 11, 0), far > 0 ? 1 : 0);
            Place(_nmHang, Vector3.up * NmStageTop, new Vector3(1, Mathf.Max(.01f, night), 1), Quaternion.Euler(0, 23, 0), night > 0 ? 1 : 0);
            for (int i = 0; i < _nmDrops.Count; i++)
            {
                var d = NmCeilingDrops[i];
                // Each gathers, hangs and stretches a little on its own slow breath.
                float grown = Ease(NmRise - .1f + i * .09f, NmRise + .8f + i * .09f, t), length = d.z * grown * (1 + .07f * Mathf.Sin(t * 1.1f + i * 1.9f));
                Place(_nmDrops[i], Quaternion.Euler(0, d.x, 0) * Vector3.forward * d.y + Vector3.up * NmStageTop, new Vector3(d.w, Mathf.Max(.01f, length), d.w),
                    Quaternion.Euler(180, 0, 0), grown > 0 ? 1 : 0);
            }
            for (int i = 0; i < _nmWisps.Count; i++)
            {
                var w = NmWispAt[i];
                float bearing = w.x + t * (i % 2 == 0 ? 2.2f : -1.7f);
                var out3 = Quaternion.Euler(0, bearing, 0) * Vector3.forward;
                PlaceGlow(_nmWisps[i], out3 * w.y + Vector3.up * (w.z + Mathf.Sin(t * .5f + i * 1.3f) * .5f), new Vector3(w.w, 1.5f + .3f * Mathf.Sin(t * .8f + i), 1),
                    Quaternion.LookRotation(out3, Vector3.up) * Quaternion.Euler(0, 0, 8f * Mathf.Sin(t * .4f + i * 2f)), .05f * night);
            }
            for (int i = 0; i < _nmMotes.Count; i++)
            {
                var m = NmMoteAt[i];
                var spot = Quaternion.Euler(0, m.x + Mathf.Sin(t * .3f + i) * 3f, 0) * Vector3.forward * m.y + Vector3.up * (m.z + Mathf.Sin(t * .6f + i * 2.3f) * .4f);
                PlaceGlow(_nmMotes[i], spot, Vector3.one * m.w * 2.2f, Quaternion.identity, night * (.3f + .18f * Mathf.Sin(t * 1.7f + i * 1.1f)));
            }

            // ------------------------------------------------------------- the dive
            // He goes in and the ink answers: a crown thrown up fast that hangs and falls back, a bead off each of its points,
            // the column that jumps up out of the hole after it, and two rings going out.
            float splash = t - NmSplash, crown = Mathf.Clamp01(splash / .7f);
            bool crowned = splash > 0 && crown < 1;
            float crownRadius = .3f + .75f * (1 - (1 - crown) * (1 - crown));
            if (crowned) NmFillCrown(_nmCrown, NmDivePeaks, NmDiveGains, 20f, crownRadius, 1.25f * Mathf.Sin(Mathf.PI * Mathf.Pow(crown, .55f)), .35f + crown * .5f, 1 - crown, Mathf.Clamp01((1 - crown) * 2.2f));
            Place(_nmCrownPiece, NmDiveAt + Vector3.up * .04f, Vector3.one, Quaternion.identity, crowned ? 1 : 0);
            for (int i = 0; i < _nmBeads.Count; i++)
            {
                float age = splash - .1f, gain = NmDiveGains[i];
                var way = Quaternion.Euler(0, NmDivePeaks[i], 0) * Vector3.forward;
                float y = .8f * gain + 4.2f * gain * age - 6f * age * age, speed = 4.2f * gain - 12f * age;
                Place(_nmBeads[i], NmDiveAt + way * (.75f + 1.6f * gain * age) + Vector3.up * y, new Vector3(.12f, .12f + Mathf.Abs(speed) * .012f, .12f) * (.7f + .5f * gain),
                    Quaternion.identity, age > 0 && y > 0 ? 1 : 0);
            }
            float jet = Mathf.Clamp01((splash - .3f) / .62f);
            Place(_nmJet, NmDiveAt + Vector3.up * .04f, new Vector3(.17f, Mathf.Max(.01f, .95f * Mathf.Sin(Mathf.PI * Mathf.Pow(jet, .7f))), .17f), Quaternion.identity, jet > 0 && jet < 1 ? 1 : 0);
            for (int k = 0; k < 2; k++)
            {
                float age = splash - k * .22f, u = Mathf.Clamp01(age / .9f), wide = Mathf.Lerp(.4f, 3.4f, 1 - (1 - u) * (1 - u));
                Place(k == 0 ? _nmDiveRing : _nmDiveRing2, NmDiveAt + Vector3.up * .04f, new Vector3(wide, 1, wide), Quaternion.identity, age >= 0 ? (1 - u) * .95f : 0);
            }

            // ------------------------------------------------------------- the rise
            // The floor swells over him first, so the eye is already there when he breaks it.
            float swelling = Ease(NmRise - .24f, NmRise + .05f, t) * (1 - Ease(NmRise + .14f, NmRise + .24f, t));
            Place(_nmSwell, NmGiantAt + Vector3.up * .035f, new Vector3(3.3f * (.75f + .25f * swelling), 1.7f * swelling * (1 + .06f * Mathf.Sin(t * 38f)), 3.3f * (.75f + .25f * swelling)),
                Quaternion.identity, swelling > 0 ? 1 : 0);
            // Then it tears: a heavy crown he comes up through, slower to rise and much slower to fall than the little one.
            float torn = t - (NmRise + .14f), tear = Mathf.Clamp01(torn / 1.05f);
            bool tearing = torn > 0 && tear < 1;
            float tearRadius = 3f + 1.7f * (1 - (1 - tear) * (1 - tear));
            if (tearing) NmFillCrown(_nmTear, NmTearPeaks, NmTearGains, 19f, tearRadius, 3.5f * Mathf.Sin(Mathf.PI * Mathf.Pow(tear, .5f)), .25f + tear * .45f, 1 - tear * .6f, Mathf.Clamp01((1 - tear) * 2f));
            Place(_nmTearPiece, NmGiantAt + Vector3.up * .04f, Vector3.one, Quaternion.identity, tearing ? 1 : 0);
            for (int i = 0; i < _nmTearBeads.Count; i++)
            {
                float age = torn - .14f, gain = NmTearGains[i];
                var way = Quaternion.Euler(0, NmTearPeaks[i], 0) * Vector3.forward;
                float y = 2.2f * gain + 7.5f * gain * age - 7f * age * age, speed = 7.5f * gain - 14f * age;
                Place(_nmTearBeads[i], NmGiantAt + way * (3.6f + 2.4f * gain * age) + Vector3.up * y, new Vector3(.34f, .34f + Mathf.Abs(speed) * .02f, .34f) * (.6f + .5f * gain),
                    Quaternion.identity, age > 0 && y > 0 ? 1 : 0);
            }
            // Ropes of it hold him to the floor, stretch, and let go one at a time, the low ones first.
            for (int i = 0; i < _nmRopes.Count; i++)
            {
                var hold = NmGiantAt + NmRopeHold[i] + Vector3.up * NmGiantLift(t);
                float since = t - _nmSnapAt[i], gap = since > 0 ? Mathf.Clamp01(since / .42f) : 0;
                bool held = t >= NmRise && hold.y > .3f && gap < 1;
                if (held) NmFillRope(_nmRopes[i], NmGiantAt + NmRopeRoot[i] + Vector3.up * .03f, hold, NmRopeSnap[i].y, gap * gap * (3 - 2 * gap));
                Place(_nmRopePieces[i], Vector3.zero, Vector3.one, Quaternion.identity, held ? 1 : 0);
            }
            // And it keeps running off him while he looks round: each drop falls, lands and leaves a ring.
            for (int i = 0; i < _nmDrips.Count; i++)
            {
                var d = NmDripAt[i];
                float age = t - d.w, from = d.y + NmGiantLift(d.w), y = from - 7f * age * age, landed = Mathf.Sqrt(Mathf.Max(.01f, from) / 7f);
                var spot = NmGiantAt + new Vector3(d.x, 0, d.z);
                // It gathers for a moment where it hangs before it lets go.
                float gather = Mathf.Clamp01((age + .2f) / .2f);
                Place(_nmDrips[i], spot + Vector3.up * Mathf.Max(0, age > 0 ? y : from), new Vector3(.4f, .4f + Mathf.Max(0, age) * .5f, .4f) * (age > 0 ? 1 : gather),
                    Quaternion.identity, age > -.2f && age < landed && t < NmRear ? 1 : 0);
                float ring = Mathf.Clamp01((age - landed) / .7f);
                Place(_nmDripRings[i], spot + Vector3.up * .042f, Vector3.one * Mathf.Lerp(.3f, 1.9f, 1 - (1 - ring) * (1 - ring)), Quaternion.identity, age > landed ? (1 - ring) * .9f : 0);
            }
            // What came off him gathers round where he stands, with an edge, and breathes.
            float pooled = Ease(NmRise + .1f, NmRisen + .15f, t);
            float poolSize = 4.6f * pooled * (1 + .1f * Mathf.Sin(pooled * Mathf.PI)) * (1 + .02f * Mathf.Sin(t * 1.6f));
            Place(_nmPuddle, NmGiantAt + Vector3.up * .036f, new Vector3(poolSize, 1, poolSize), Quaternion.Euler(0, t * 3f, 0), pooled > 0 ? 1 : 0);
            for (int k = 0; k < 2; k++)
            {
                float age = t - (NmRise + .2f + k * .55f), u = Mathf.Clamp01(age / 1.5f), wide = Mathf.Lerp(2.6f, 10.5f, 1 - (1 - u) * (1 - u));
                Place(k == 0 ? _nmRiseRing : _nmRiseRing2, NmGiantAt + Vector3.up * .04f, new Vector3(wide, 1, wide), Quaternion.identity, age >= 0 ? (1 - u) * .95f : 0);
            }
            PlaceGlow(_nmRiseGlow, NmGiantAt + Vector3.up * 1.6f, Vector3.one * 9f, Quaternion.identity, Flash(t, NmRise + .22f, .3f) * .18f);

            // ------------------------------------------------------------- the eyes
            // When he goes for the lens every eye on the stage goes with him: they turn to it and their pupils open.
            float hunt = Ease(NmLunge - .12f, NmLunge + .12f, t);
            // His eyes open in the dark behind him, in pairs, and blink. They watch the lens.
            for (int i = 0; i < _inkEyes.Count; i++)
            {
                int pair = i / 2; float side = i % 2 == 0 ? -1 : 1;
                float big = pair == 0 ? 1.1f : pair == 1 ? .78f : pair == 2 ? .95f : .7f;
                float angle = (pair < 2 ? 112 + pair * 30 : 218 + (pair - 2) * 30) * Mathf.Deg2Rad;
                float apart = side * big * 1.3f;
                var eye = new Vector3(Mathf.Sin(angle) * 15.2f + apart * Mathf.Cos(angle), 3.4f + (pair % 2) * 2.6f, Mathf.Cos(angle) * 15.2f - apart * Mathf.Sin(angle));
                var facing = Quaternion.LookRotation(new Vector3(eye.x, 0, eye.z).normalized, Vector3.up);
                float since = t - (NmRise + .25f + pair * .14f);
                float open = Mathf.Clamp01(since / .09f) * (1 + .3f * Mathf.Sin(Mathf.Clamp01(since / .3f) * Mathf.PI));
                float blink = _reducedEffects ? 1 : 1 - .96f * Mathf.Clamp01(1 - Mathf.Abs(Mathf.Repeat(t + pair * .37f, 1.3f) - .65f) / .06f);
                blink = Mathf.Lerp(blink, 1.12f, hunt);
                NmPlaceEye(_inkEyes[i], eye, facing, big, open * blink, NmLook(eye, facing, lens), Mathf.Lerp(.085f, .17f, hunt), .16f + .1f * hunt);
            }

            // The eyes in the floor under each player he has seen. They open with a snap, find their player and stay on them.
            for (int i = 0; i < _others.Count && i < 4; i++)
            {
                float since = t - NemuSeenAt(i);
                float open = Mathf.Clamp01(since / .08f) * (1 + .35f * Mathf.Sin(Mathf.Clamp01(since / .3f) * Mathf.PI));
                float blink = _reducedEffects ? 1 : 1 - .96f * Mathf.Clamp01(1 - Mathf.Abs(Mathf.Repeat(t + i * .53f, 1.9f) - 1.2f) / .06f);
                blink = Mathf.Lerp(blink, 1.12f, hunt);
                var home = _others[i].Home;
                // Flat on the floor with the upper lid away from her, so from every lens here they are the right way up.
                var facing = Quaternion.Euler(-90, 0, 0);
                for (int k = 0; k < 2; k++)
                {
                    var spot = home + new Vector3(k == 0 ? -1.1f : 1.1f, .05f, .05f);
                    // A startled dart the wrong way first, then onto the body standing over them.
                    var look = Vector2.Lerp(new Vector2(k == 0 ? -.3f : .3f, .08f), NmLook(spot, facing, home + Vector3.up * 1.1f), Ease(.1f, .26f, since));
                    look = Vector2.Lerp(look, NmLook(spot, facing, lens), hunt);
                    NmPlaceEye(_nmSeen[i * 2 + k], spot, facing, .86f, open * blink, look, Mathf.Lerp(Mathf.Lerp(.2f, .085f, Ease(.02f, .28f, since)), .18f, hunt), .18f + .1f * hunt);
                }
            }

            // ------------------------------------------------------------- the send
            // As he comes the ink cannot keep up with him: it streams back off his head in ropes, longer the faster he is.
            var lensWay = (lensLook - lens).normalized;
            var lensRight = Vector3.Cross(Vector3.up, lensWay); lensRight = lensRight.sqrMagnitude < .001f ? Vector3.right : lensRight.normalized;
            var lensUp = Vector3.Cross(lensWay, lensRight);
            float streaming = Mathf.Clamp01(go * 14f);
            var back = (NmGiantAt + Vector3.up * 6.5f - _nmLens).normalized;
            for (int i = 0; i < _nmStreams.Count; i++)
            {
                var s = NmStreamAt[i];
                float bearing = s.x * Mathf.Deg2Rad;
                var radial = lensRight * Mathf.Cos(bearing) + lensUp * Mathf.Sin(bearing);
                var way = (back + radial * .3f + lensUp * .12f * Mathf.Sin(t * 19f + i * 1.7f)).normalized;
                float length = s.z * (.6f + 12f * go) * (1 + .12f * Mathf.Sin(t * 23f + i * 2.4f));
                Place(_nmStreams[i], maw + radial * s.y * (1 + go * .25f) + back * .6f, new Vector3(s.w, length * streaming, s.w), Quaternion.FromToRotation(Vector3.up, way),
                    giant && streaming > 0 ? 1 : 0);
            }

            // ------------------------------------------------------------- his light, and what moves in it
            float risen = giant ? Ease(NmRise + .2f, NmRisen, t) : 0;
            float flicker = _reducedEffects ? 0 : .5f * Mathf.Sin(t * 13f) + .3f * Mathf.Sin(t * 29f + 1.3f);
            // The embers: his throat, hotter as he comes, and both eyes, hottest the moment he finds someone.
            float found = 0;
            for (int i = 0; i < _others.Count && i < 4; i++) found = Mathf.Max(found, Flash(t, NemuSeenAt(i) + .03f, .22f));
            var toLensFromMaw = (lens - maw).normalized;
            PlaceGlow(_nmThroat, maw + toLensFromMaw * .4f, Vector3.one * (3.6f + .4f * flicker + 1.5f * go), Quaternion.identity, risen * (.2f + .05f * flicker + .12f * go) * (1 - Ease(NmGulp - .3f, NmGulp - .12f, t)));
            var leftEye = NmGiantEye(_nmGiantEyeLeft, maw + new Vector3(-1.2f, 2.6f, 0)); var rightEye = NmGiantEye(_nmGiantEyeRight, maw + new Vector3(1.2f, 2.6f, 0));
            PlaceGlow(_nmEmberLeft, leftEye + (lens - leftEye).normalized * .5f, Vector3.one * (1.5f + .15f * flicker + 1.5f * found), Quaternion.identity, risen * (.2f + .05f * flicker + .55f * found + .15f * hunt));
            PlaceGlow(_nmEmberRight, rightEye + (lens - rightEye).normalized * .5f, Vector3.one * (1.5f - .15f * flicker + 1.5f * found), Quaternion.identity, risen * (.2f - .05f * flicker + .55f * found + .15f * hunt));
            // Their light lying on the ink in front of him, pulled long toward whoever stands there.
            PlaceGlow(_nmThroatCast, new Vector3(maw.x, .07f, maw.z + 3.2f), new Vector3(8.5f + .6f * flicker, 12f, 1), Quaternion.Euler(90, 0, 0), risen * (.11f + .03f * flicker + .12f * found) * (1 - go));

            // Comets: out of him on a long loop and drawn back in. Each has a bright head and a tail lying along the way it
            // came, and each is on its own orbit and its own clock.
            // Behind his face, always: they come round his sides and over his back, never across his eyes.
            var heart = maw + new Vector3(0, .2f, -4.8f);
            for (int i = 0; i < _nmComets.Count; i++)
            {
                var c = NmCometAt[i];
                Vector3 CometAt(float when)
                {
                    float angle = c.w + when * c.z, breath = .5f + .5f * Mathf.Sin(when * (1.1f + .13f * i) + c.w * 2f);
                    float reach = c.x * (.22f + .78f * breath * breath);
                    return heart + new Vector3(Mathf.Cos(angle) * reach, c.y * (.3f + .5f * breath) + Mathf.Sin(angle * .7f + c.w) * 1.1f - 1f, Mathf.Sin(angle) * reach * .3f);
                }
                Vector3 here = CometAt(t), before = CometAt(t - .05f), way = here - before;
                float speed = way.magnitude / .05f, big = .5f + .09f * (i % 5);
                // As he goes for the lens they are all pulled in behind him.
                float shown = risen * (1 - Ease(NmLunge + .2f, NmGulp - .15f, t));
                PlaceGlow(_nmComets[i], here, Vector3.one * big * 1.5f * (1 + .2f * flicker), Quaternion.identity, shown * .9f);
                // The tail lies back along the way it came, longer the faster it goes, and tears more as it is drawn back in.
                float frayed = .16f + .34f * (.5f - .5f * Mathf.Sin(t * (1.1f + .13f * i) + c.w * 2f));
                NmPlaceRibbon(_nmTrails[i], here, -way, lens, 2.4f + speed * .65f, big * .8f);
                UltimateFx.Set(_nmTrails[i], way.sqrMagnitude > 1e-6f ? t + i * .37f : -1f, frayed + .02f, shown);
            }

            // Ink that goes UP: drops bead on the floor as he rises, stretch, let go, and hang in the air round him with
            // their tails down. When he goes, they go after him.
            for (int i = 0; i < _nmLifted.Count; i++)
            {
                var d = NmLiftAt[i];
                float age = t - (NmRise - .05f + d.w), up = NmSettle(age / 1.25f), bead = Mathf.Clamp01((age + .25f) / .25f);
                var spot = NmGiantAt + new Vector3(d.x, 0, d.y) + Vector3.up * (age > 0 ? d.z * up + .22f * Mathf.Sin(t * 1.4f + i * 1.7f) * Mathf.Clamp01(age - .8f) : .1f);
                float pulled = Ease(NmLunge - .05f, NmLunge + .5f, t); pulled *= pulled;
                spot = Vector3.Lerp(spot, maw, pulled);
                float big = (.3f + .05f * (i % 4)) * bead * (1 - pulled), stretched = 1 + 1.4f * Mathf.Clamp01(1 - age / .5f) * Mathf.Clamp01(age * 6f);
                Place(_nmLifted[i], spot, new Vector3(big, big * stretched, big), Quaternion.Euler(180, i * 47f, 0), giant && age > -.25f && pulled < .98f ? 1 : 0);
            }

            // Sparks round Nemu: slow, small, never crossing her face. She is the still middle of all this.
            float still = Ease(NmDark + .3f, NmRise + .3f, t) * (1 - Ease(NmGulp - .35f, NmGulp - .1f, t));
            for (int i = 0; i < _nmOrbit.Count; i++)
            {
                var o = NmOrbitAt[i];
                float angle = o.z + t * (i % 2 == 0 ? 1.5f : -1.15f), twinkle = .55f + .45f * Mathf.Abs(Mathf.Sin(t * (3.1f + i * .7f) + o.z));
                var spot = new Vector3(Mathf.Cos(angle) * o.x, o.y + .1f * Mathf.Sin(t * 1.3f + o.z * 2f), Mathf.Sin(angle) * o.x);
                var toLens = lens - spot;
                Place(_nmOrbit[i], spot, Vector3.one * (.075f * twinkle * still),
                    (toLens.sqrMagnitude > .01f ? Quaternion.LookRotation(toLens, Vector3.up) : Quaternion.identity) * Quaternion.Euler(0, 0, t * 60f + i * 30f), still > .01f ? 1 : 0);
                PlaceGlow(_nmOrbitLights[i], spot, Vector3.one * .42f * twinkle, Quaternion.identity, still * .4f * twinkle);
            }

            // He sees you: a shock under that player. A ring runs out over the ink and six motes are thrown up off it, fast
            // and then slowing, each its own way.
            for (int i = 0; i < _nmShockRings.Count && i < _others.Count; i++)
            {
                float age = t - NemuSeenAt(i), u = Mathf.Clamp01(age / .6f), thrown = 1 - (1 - u) * (1 - u) * (1 - u);
                var home = _others[i].Home;
                Place(_nmShockRings[i], home + Vector3.up * .055f, Vector3.one * Mathf.Lerp(.5f, 3.4f, thrown), Quaternion.identity, age >= 0 ? (1 - u) * .95f : 0);
                for (int k = 0; k < NmShockWay.Length; k++)
                {
                    var w = NmShockWay[k];
                    var spot = home + Vector3.up * .25f + new Vector3(w.x, 0, w.z) * (1.9f * thrown) + Vector3.up * (w.y * (2.4f * thrown - 1.1f * u * u));
                    PlaceGlow(_nmShock[i * NmShockWay.Length + k], spot, Vector3.one * (.5f - .22f * u) * (.8f + .1f * k), Quaternion.identity, age >= 0 && u < 1 ? (1 - u) * .85f : 0);
                }
            }

            // The rush: streaks of his light tearing past the lens, more and longer the faster he comes.
            for (int i = 0; i < _nmRushTrails.Count; i++)
            {
                var r = NmRushAt[i];
                float bearing = r.x * Mathf.Deg2Rad, along = Mathf.Repeat(r.w + go * 2.6f + t * .9f, 1);
                var radial = lensRight * Mathf.Cos(bearing) + lensUp * Mathf.Sin(bearing);
                var spot = lens + lensWay * Mathf.Lerp(17f, -3f, along) + radial * (r.y * 1.5f + 1f);
                NmPlaceRibbon(_nmRushTrails[i], spot, lensWay, lens + lensWay * 4f, r.z * (.8f + 1.8f * go), .24f + .08f * (i % 3));
                UltimateFx.Set(_nmRushTrails[i], go > .01f ? t * 1.4f + i : -1f, Mathf.Lerp(.75f, .26f, Mathf.Sin(along * Mathf.PI)), Mathf.Clamp01(go * 5f) * .95f);
            }

            // ------------------------------------------------------------- ink smoke
            // It peels off his shoulders, his back and the foot of his tail, climbs, and is eaten from its root as it goes.
            // Each on its own clock; all of it behind or beside his head.
            float smoking = risen * (1 - Ease(NmLunge + .05f, NmLunge + .35f, t));
            for (int i = 0; i < _nmSmoke.Count; i++)
            {
                var s = NmSmokeAt[i];
                float u = Mathf.Repeat((t + i * .41f) / s.w, 1), climbed = u * u * (3 - 2 * u);
                var from = i < 6 ? maw + new Vector3(s.x, s.y, s.z) : NmGiantAt + new Vector3(s.x, s.y, s.z);
                float big = (i < 6 ? 3.3f : 1.8f) * (.7f + .7f * climbed);
                UltimateFx.Place(_nmSmoke[i], from + Vector3.up * ((i < 6 ? 2.4f : 1.3f) * climbed), Quaternion.Euler(0, i * 67f + 50f * u, (i % 2 == 0 ? 9f : -11f) * u), new Vector3(big, big * (1 + .5f * climbed), big));
                UltimateFx.Set(_nmSmoke[i], giant ? t + i * 1.3f : -1f, Mathf.Lerp(.14f, 1f, climbed * climbed), smoking * Mathf.Clamp01(u * 8f));
            }
            // The rise: a crown of it thrown up where he breaks the pool, falling back eaten, and a wash running out over the ink.
            float broke = t - (NmRise + .12f), thrown2 = Mathf.Clamp01(broke / 1.25f), lofted = Mathf.Sin(Mathf.PI * Mathf.Pow(thrown2, .5f));
            UltimateFx.Place(_nmRiseWisps, NmGiantAt + Vector3.up * .1f, Quaternion.Euler(0, 34f, 0), new Vector3(6.4f + 6f * thrown2, Mathf.Max(.05f, 3.8f * lofted), 6.4f + 6f * thrown2));
            UltimateFx.Set(_nmRiseWisps, broke, Mathf.Lerp(.24f, 1f, Ease(.12f, .7f, thrown2)));
            float washed = Mathf.Clamp01((t - (NmRise + .18f)) / 1.7f), ran = 1 - (1 - washed) * (1 - washed);
            UltimateFx.Place(_nmRiseWash, NmGiantAt + Vector3.up * .062f, Quaternion.Euler(0, 20f, 0), new Vector3(Mathf.Lerp(5f, 15f, ran), 1, Mathf.Lerp(5f, 15f, ran)));
            UltimateFx.Set(_nmRiseWash, t - (NmRise + .18f), Mathf.Lerp(.3f, 1f, Ease(.1f, 1f, washed)));
            // Before the roar the light in his throat gathers, and a ring of it spins round it and is gone as his jaw drops.
            float charged = giant ? Ease(NmRisen - .8f, NmRisen - .35f, t) * (1 - Ease(NmRisen - .24f, NmRisen - .12f, t)) : 0;
            for (int i = 0; i < 2; i++)
            {
                UltimateFx.Place(_nmOrbRing[i], maw + toLensFromMaw * .5f, Quaternion.Euler(24f, 0, 14f) * Quaternion.Euler(0, t * 560f + i * 180f, 0), Vector3.one * (2.3f - .7f * charged));
                UltimateFx.Set(_nmOrbRing[i], charged > .01f ? t * 2f + i : -1f, Mathf.Lerp(.7f, .25f, charged), charged);
            }
            PlaceGlow(_nmDiveGlow, charged > 0 ? maw + toLensFromMaw * .5f : NmDiveAt + Vector3.up * .3f, Vector3.one * (charged > 0 ? 1.2f + 1.6f * charged : 2.4f), Quaternion.identity,
                charged > 0 ? .5f * charged : Flash(t, NmSplash + .04f, .2f) * .4f);
            // The lunge: he outruns his own smoke. It is ripped back off his head along the way he came, longer the faster he goes.
            for (int i = 0; i < _nmTorn.Count; i++)
            {
                float bearing = NmTornAt[i] * Mathf.Deg2Rad;
                var radial = lensRight * Mathf.Cos(bearing) + lensUp * Mathf.Sin(bearing);
                float long_ = (2.6f + 8f * go) * (1 + .1f * Mathf.Sin(t * 21f + i * 2.1f));
                UltimateFx.Place(_nmTorn[i], maw + radial * 3.3f * (1 + go * .25f) + back * 3.4f, Quaternion.LookRotation((back + radial * .25f).normalized, radial), Vector3.one * long_);
                UltimateFx.Set(_nmTorn[i], go > .015f ? t * 1.6f + i * .7f : -1f, .2f + .3f * (.5f + .5f * Mathf.Sin(t * 8f + i * 1.9f)), Mathf.Clamp01(go * 9f) * (1 - Ease(NmGulp - .22f, NmGulp - .1f, t)));
            }
            // And the end: the picture is eaten. Ink closes on the middle of the lens from all round, torn and burning at its
            // edge, a breath before the black that was always the last frame. It is behind his teeth, which close over it.
            // ⚠️ SLOW TO START, LATE TO CLOSE. Closed on a plain ease the picture was black by 6.80 s and the blots were on screen
            // for four frames nobody saw (film p15). Each sheet now stays mostly open for the first two thirds of its time,
            // so the blots are seen opening, spreading and running together, and the black is whole on the frame it always was.
            float eaten = Mathf.Pow(Mathf.Clamp01((t - (NmGulp - .32f)) / .3f), 2.1f), eatenAll = Mathf.Pow(Mathf.Clamp01((t - (NmGulp - .26f)) / .26f), 2.4f);
            UltimateFx.Place(_nmEaten, lens + lensWay * .25f, Quaternion.LookRotation(lensWay, lensUp), new Vector3(1.3f, 1.3f, 1f));
            UltimateFx.Set(_nmEaten, eaten > 0 ? t : -1f, Mathf.Lerp(.9f, 0f, eaten));
            UltimateFx.Place(_nmEatenAll, lens + lensWay * .245f, Quaternion.LookRotation(lensWay, lensUp) * Quaternion.Euler(0, 0, 70f), new Vector3(1.3f, 1.3f, 1f));
            UltimateFx.Set(_nmEatenAll, eatenAll > 0 ? t + 3f : -1f, Mathf.Lerp(.94f, 0f, eatenAll));

            // His teeth, as the lens sees them from inside his mouth: two pale rows closing in from the top and bottom of the
            // picture, slowly and then all at once, with the dark of the gum behind them. They are a hand's breadth from the
            // lens and shake with it, and they are gone into the black by the time it is whole.
            float bite = Mathf.Clamp01((t - (NmGulp - .19f)) / .14f); bite *= bite;
            float teethShown = bite > 0 ? 1 - Ease(NmGulp, NmGulp + .04f, t) : 0;
            const float TeethOut = .2f;
            float frame = TeethOut * Mathf.Tan(lensFov * .5f * Mathf.Deg2Rad), jawAt = Mathf.Lerp(1.78f, .8f, bite);
            var teethMiddle = lens + lensWay * TeethOut;
            var teethFacing = Quaternion.LookRotation(lensWay, lensUp);
            Place(_nmJawUp, teethMiddle + lensUp * (frame * jawAt), Vector3.one * frame, teethFacing, teethShown);
            Place(_nmJawDown, teethMiddle - lensUp * (frame * jawAt), Vector3.one * frame, teethFacing * Quaternion.Euler(0, 0, 180), teethShown);
            PlaceGlow(_nmJawGlowUp, teethMiddle + lensUp * (frame * (jawAt - .25f)) - lensWay * .002f, new Vector3(frame * 6, frame * 1.5f, 1), teethFacing, teethShown * (.02f + .03f * bite));
            PlaceGlow(_nmJawGlowDown, teethMiddle - lensUp * (frame * (jawAt - .25f)) - lensWay * .002f, new Vector3(frame * 6, frame * 1.5f, 1), teethFacing, teethShown * (.02f + .03f * bite));

            // Swallowed: black all round the lens as his mouth closes over it, and it stays black.
            float gone = Ease(NmGulp - .09f, NmGulp + .02f, t);
            // A shell a hand's breadth from the lens: at 6 m he was inside it, and the last frames were the lit inside of his
            // throat instead of black (film n2).
            Place(_nmShroud, _nmLens - Vector3.up * .3f, new Vector3(.3f, .6f, .3f), Quaternion.identity, gone);
        }

        /// <summary>The last shot's lens is the one the build kept off the others, and it trembles as he comes.</summary>
        private void NemuFrame(int index, float t, ref Vector3 eye, ref Vector3 look, ref float fov)
        {
            if (index == 4)
            {
                eye = _nmLens;
                var tremble = NemuTremble(t);
                eye += tremble; look += tremble;
            }
            // ⚠️ HOW FAR THE MATCH PUSHED THE LAST FRAME'S LENS IN. `UltimatePhaseView` slides a blocked shot in along its own
            // line AFTER this is asked, and says where the lens ended up (`LastEye`). One number a shot, the same all through
            // it, so it is measured once from the frame before and kept; the film has no map and measures nothing.
            if (Application.isPlaying && index >= 0 && index < _nmPull.Length && _nmAskedShot == index && UltimatePhaseView.LastShot == index
                && Mathf.Abs(UltimatePhaseView.LastAge - _nmAskedAt) < .0005f)
            {
                float asked = (_nmAskedEye - _nmAskedLook).magnitude;
                float got = (_root.transform.InverseTransformPoint(UltimatePhaseView.LastEye) - _nmAskedLook).magnitude;
                if (asked > .1f) _nmPull[index] = Mathf.Clamp(got / asked, .2f, 1f);
            }
            NmFitGiant(index, t, ref eye, ref look, ref fov);
            _nmAskedEye = eye; _nmAskedLook = look; _nmAskedAt = t; _nmAskedShot = index;
        }

        /// <summary>The lights go down with the ink, and out when he swallows the picture.</summary>
        private void NemuGrade(float t, out float brightness, out float saturation)
        {
            float dark = Ease(NmDark, NmRise, t);
            brightness = Mathf.Lerp(1f, .8f, dark) * (1 - Ease(NmGulp - .09f, NmGulp + .02f, t));
            saturation = Mathf.Lerp(1f, .78f, dark);
        }
    }
}
