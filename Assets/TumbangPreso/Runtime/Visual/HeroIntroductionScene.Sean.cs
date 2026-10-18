using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // RAGO, SUPERNOVA, 7.0 s (was 3.4 s; plan.md § 1 is the first one).
        //
        // ⚠️ RESTAGED 2026-10-08. The owner, of three staging ideas: "sean i like 1 and 2", agreed to them joined at about
        // 7 s: "he builds the small parol, lights it and lets it go. The camera follows it up as it grows into the giant
        // turning festival lantern. He launches into its centre, it bursts, and we end looking down from his height at the
        // players inside the landing ring."
        //   1. BUILD (0 to 2.2): the craftsman, as before: five sticks laid one at a time between his cupped hands, the star
        //      filled with flame, him watching it.
        //   2. RELEASE (2.2 to 2.9): he lifts it over his head and lets it go.
        //   3. THE RISE (2.9 to 4.9): the lens follows it up as it climbs to a little over six metres and grows to eleven across,
        //      turning, its three rings of lights coming on and chasing round it as San Fernando's giant lanterns do, and at
        //      the top it lies over flat above the court like a ceiling of lights.
        //   4. FROM ABOVE (4.9 to 6.0): down through its frame: the court in its light, the ring where he will land drawn
        //      on the ground round him, and every other player standing inside that ring marked.
        //   5. THE COIL AND THE RISE (6.0 to 7.0), low in front of him: flames round his feet, the lantern filling the sky
        //      over him, and he drives up into the first frame of the live leap.
        // ⚠️ HE LEAPS, AND THE CUTSCENE ENDS AT THE TOP OF IT. A first cut kept him on the ground (play's ability IS the leap,
        // and a cutscene that showed it would have had play show it again). His answer, 2026-10-09: "he should leap but then
        // at the apex of the leap, the person playing sean should have their cam tween to their fpv. and the rest of the
        // players cut to their fpv". So: he goes up at 6.2 into the lantern, which bursts round him at 6.72, and at 7.0 he is
        // at the top (`SeanHeroKit.SupernovaApex`, 4.675 m: where play's own launch has him when its dive begins). Play
        // resumes WITH HIM THERE (`SeanHeroKit`, `IntroductionIsTheWindup`) and he dives. On his own screen the lens leaves
        // the last shot at 6.52 and arrives in his eyes (`SeanHandBack`); everyone else's ends on the ordinary quick dissolve.
        // The lantern hangs at 6.2 m now, not 8.5: his head is in its plane at the top.
        // ⚠️ NOTHING OF THE STAGE FADES AT THE END (as Dante's does not): the burst is still in the air on the last frame.
        //
        // Kept of his first cutscene, each asked for or approved in September: the parol is a real frame of five sticks,
        // each joining a point to the point two along; his fire is BUILT, not summoned; the dusk and the paper lanterns of
        // home drifting up behind him; orange and gold on ember brown; the coil with flames at his feet; the last pose.
        //
        // ⚠️ THE DENSITY PASS, the next day. Of the first cut he said only "it looks really basic", and it was, three ways:
        // the giant lantern was a five-point outline where a San Fernando lantern is a kaleidoscope; nothing happened when it
        // reached the top; and the air round all of it was empty (`docs/HERO_KIT_METHOD.md` § 6: "in every Genshin burst the
        // AIR ITSELF is full"). So: the lantern has LAYERS (the parol, a ten-point star and an eight-point one turning
        // against it, a toothed rim), five rings of lights that run in waves out from its middle and then chase; a trail
        // of fire and shed sparks as it climbs; an IGNITION when it lies over (a ring thrown across the sky, twelve rays,
        // a rain of sparks, shafts of its light standing on the court); and it flares again as he rises.
        // ⚠️ MODELLED AND DRESSED, 2026-10-10 (it was blocking: every shape was a stock one). The owner, once the staging stood:
        // "continue polishing all the ultimates. i dont want to see anything bare in textures, anything looking unpolished, no
        // vfx etc." The parol is a typed model now (`BuildSeanParol`): bamboo sticks, paper points in two halves on a ridge,
        // a faceted heart with a capiz boss, a hoop, a second star behind, a crown of ten scallops, cords with bulbs on them
        // and a tail at every point. Its build is the model's own sticks and panels arriving; its burst is its own panels and
        // scallops thrown. The fire has shape (a tapered ribbon with a core and tongues, tumbling embers), the rays have width,
        // the rain is little stars and paper diamonds, the shafts are soft and pool on the court, the landing mark is his own
        // star laid on the ground and counting, and the dusk has a town in front of it: roofs, lit windows, strings of flags.
        // ⚠️ NO TIME, POSE OR SHOT MOVED IN THAT PASS. ⚠️ EVERY TIME IS ON THE TABLE'S CLOCK (`tools/author_ultimate_intros.py`,
        // `_sean`). The last pose is `rise`.
        // =========================================================================================
        // ⚠️ THE LANTERN IS IN THE SKY, 16 m UP AND 24 m ACROSS (owner 2026-10-09, of film s8: "sean jumps up and clips his head on
        // the magic star effect . the effect should look like its reaching from the sky and large"). At 6.2 m up and 11 m
        // across his leap, which tops out with his head near 6.5 m, went through it. Everything of it hangs off these two
        // numbers: its rings, rays, rain, and the shafts of light, which now stand the whole way down from it to the court.
        private const float SnFill = 1.75f, SnLift = 2.25f, SnRelease = 2.70f, SnTop = 4.60f, SnFlatFrom = 4.15f, SnFlatTo = 4.85f,
                            SnRing = 4.75f, SnCoil = 5.20f, SnRise = 6.20f, SnBurst = 7.02f, SnHome = 8.02f, SnBehind = 7.15f, SnBehindBy = 7.85f, SnIgnite = 4.62f, SnHigh = 16f, SnGiant = 12f, SnLanding = 5.4f;
        private static readonly Vector3[] LanternSeats =
        {
            new Vector3(-2.6f, .4f, -4.2f), new Vector3(1.9f, .2f, -5.3f), new Vector3(-4.6f, .7f, -2.2f),
            new Vector3(3.9f, .5f, -2.9f), new Vector3(-.4f, .1f, -6.2f), new Vector3(5.2f, .3f, .6f),
        };
        // The giant lantern's rings of lights: (how far out as a share of its radius, how many, which colour).
        private static readonly Vector3[] SnRings =
        { new Vector3(.22f, 6, 0), new Vector3(.40f, 10, 1), new Vector3(.58f, 14, 2), new Vector3(.76f, 18, 0), new Vector3(.94f, 24, 1) };
        // The shafts of its light on the court: (x, z, how wide), typed.
        private static readonly Vector3[] SnShafts =
        { new Vector3(-2.6f, 1.4f, .7f), new Vector3(1.9f, 2.8f, .9f), new Vector3(.2f, -2.4f, .8f), new Vector3(3.3f, -.6f, .6f), new Vector3(-1.2f, 4.0f, .65f), new Vector3(-3.6f, -1.8f, .55f) };
        // The sparks it rains when it ignites: (which way out, how far out, how fast down), typed.
        private static readonly Vector3[] SnRain =
        {
            new Vector3(12f, .5f, 2.6f), new Vector3(47f, .9f, 3.4f), new Vector3(80f, .3f, 2.2f), new Vector3(118f, .75f, 3.0f), new Vector3(151f, 1.0f, 3.8f),
            new Vector3(189f, .45f, 2.4f), new Vector3(224f, .85f, 3.2f), new Vector3(256f, .6f, 2.8f), new Vector3(291f, .95f, 3.6f), new Vector3(327f, .35f, 2.0f),
            new Vector3(30f, .7f, 4.0f), new Vector3(98f, .55f, 3.5f), new Vector3(170f, .65f, 2.9f), new Vector3(240f, .4f, 3.7f), new Vector3(308f, .8f, 2.5f), new Vector3(355f, .6f, 3.1f),
        };
        private static readonly Color[] SnLightColour = { new Color(1f, .86f, .36f, 1), new Color(1f, .5f, .12f, 1), new Color(.96f, .2f, .08f, 1) };

        // The flags of the far town that carry a light: (how far round from the quarter's middle in degrees, how high), typed
        // to match the strings in `tools/build_ultimate_props_sean.py`.
        private static readonly Vector2[] SnFiesta =
        { new Vector2(-22.5f, 3.33f), new Vector2(-15.5f, 2.90f), new Vector2(-8.5f, 3.10f), new Vector2(2.6f, 3.80f), new Vector2(8.0f, 3.10f), new Vector2(13.4f, 3.66f) };
        // The first stars of the evening: (which way round, how high, how big), typed.
        private static readonly Vector3[] SnEvening =
        {
            new Vector3(18f, 9.5f, .34f), new Vector3(61f, 13f, .22f), new Vector3(104f, 7.5f, .28f), new Vector3(139f, 11.5f, .2f), new Vector3(176f, 8.5f, .3f),
            new Vector3(212f, 12.5f, .24f), new Vector3(249f, 7f, .22f), new Vector3(287f, 10.5f, .32f), new Vector3(321f, 14f, .2f), new Vector3(349f, 6.5f, .26f),
        };

        private int _duskLow, _duskGlow, _duskHigh, _duskSun, _duskMid, _duskNight, _duskSunGlow, _parolHalo, _parolFace, _parolHeartGlow, _snFlash, _snMark, _snCast,
                    _snShock, _snShockSoft, _snShockGlow, _snCoilGlow, _snLaunch, _snJetGlow;
        private readonly List<int> _snRays = new List<int>(12), _snShaftPieces = new List<int>(18), _snShaftPools = new List<int>(6), _snRainGlows = new List<int>(16), _snRainPieces = new List<int>(16), _snShed = new List<int>(8), _snShedPieces = new List<int>(8), _snTrailFlames = new List<int>(5),
            _snLandTips = new List<int>(5), _snFiestaGlows = new List<int>(24), _snEveningGlows = new List<int>(10),
            _snCoilCores = new List<int>(6), _snJets = new List<int>(3);
        private LineRenderer _snTrail, _snTrailOuter;
        private readonly List<int> _lanternGlows = new List<int>(6),
            _footFlames = new List<int>(6), _sparks = new List<int>(8), _snLights = new List<int>(72), _snMarks = new List<int>(4);
        private Transform _snParol;
        // The modelled parol's joints (`Models/UltimateProps/parol`), and where each one sits when the lantern is whole.
        private readonly Transform[] _snStick = new Transform[5], _snPoint = new Transform[5], _snTail = new Transform[5], _snTailEnd = new Transform[5], _snPetal = new Transform[10];
        private readonly Vector3[] _snStickHome = new Vector3[5], _snPointHome = new Vector3[5], _snPetalHome = new Vector3[10];
        private Transform _snHeart, _snHoop, _snBack, _snCrown, _snStrings, _snBulbSource;
        private readonly List<Transform> _snBulbs = new List<Transform>(72);
        // The court under him, in the scene's own height: flat marks lie on what players stand on (`PhaisterProp.OnCourt`).
        private float _snFloor;
        // ⚠️ THE SHARED EFFECTS KIT (`UltimateFx`, `Shaders/UltimateErode`), 2026-10-10: shapes that are EATEN AWAY by a running
        // mask with a burning rim instead of fading. Here: a dome of capiz panes that rises off the landing mark, the two
        // trails as torn ribbons, wisps thrown by the leap and by the burst, and a wreath of light on the frame the burst
        // leaves bare. No comic pop on him. False when the shader is not there: the cutscene then plays as it did.
        private bool _snKit;
        private int _snDome, _snDomeRims, _snDomeSheen, _snDomeHoop, _snDomeGlow, _snDomePool, _snWreathHoop, _snWreathHoopIn;
        private readonly List<int> _snOrbitTails = new List<int>(12);
        private readonly List<int> _snClimbRibbons = new List<int>(4), _snLeapRibbons = new List<int>(4), _snWreathSticks = new List<int>(5),
            _snOrbit = new List<int>(12), _snOrbitGlows = new List<int>(12);
        private readonly UltimateFx.Piece[] _snLeapWisps = new UltimateFx.Piece[SnLeapThrown.Length], _snBurstWisps = new UltimateFx.Piece[SnBurstThrown.Length];
        // The wisps the leap throws off the court: (which way round, how long in metres, how long it lasts), each its own shape.
        private static readonly string[] SnLeapShapes = { "whip", "hook", "arc", "whip", "hook", "arc", "whip", "hook" };
        // ⚠️ ALL OF THEM BEHIND HIM (0 is toward the lens): thrown across him they hid him (film p7), and one thrown to his side
        // came past the lens and filled a corner of the frame with white (film p9).
        private static readonly Vector3[] SnLeapThrown =
        {
            new Vector3(108f, 2.5f, .74f), new Vector3(127f, 2.0f, .62f), new Vector3(147f, 2.8f, .82f), new Vector3(168f, 2.2f, .66f),
            new Vector3(190f, 2.6f, .78f), new Vector3(211f, 2.1f, .64f), new Vector3(232f, 2.8f, .84f), new Vector3(252f, 2.3f, .70f),
        };
        // The wisps the burst throws from the lantern, curling down and out from its rim: the same three numbers.
        private static readonly string[] SnBurstShapes = { "whip", "hook", "arc", "whip", "hook", "s" };
        private static readonly Vector3[] SnBurstThrown =
        { new Vector3(12f, 10.5f, 1.5f), new Vector3(74f, 8.5f, 1.3f), new Vector3(133f, 11.5f, 1.6f), new Vector3(191f, 9f, 1.4f), new Vector3(252f, 10f, 1.5f), new Vector3(312f, 8f, 1.3f) };
        private static readonly int SnErodeId = Shader.PropertyToID("_Erode"), SnOpacityId = Shader.PropertyToID("_Opacity"), SnSweepId = Shader.PropertyToID("_Sweep");
        private readonly List<Renderer> _snBulbSkins = new List<Renderer>(144);
        private readonly MaterialPropertyBlock _snBulbBlock = new MaterialPropertyBlock();
        private readonly List<int> _snTipGlows = new List<int>(5), _snTrailGlows = new List<int>(3), _snClouds = new List<int>(4),
            _snMarkEmbers = new List<int>(12), _snMarkEmberGlows = new List<int>(12), _snCoilEmbers = new List<int>(8), _snLeapSparks = new List<int>(10),
            _snShards = new List<int>(12), _snFlakes = new List<int>(18), _snFlakeGlints = new List<int>(18);
        private static readonly int SnModeId = Shader.PropertyToID("_Mode"), SnAgeId = Shader.PropertyToID("_Age"), SnPresenceId = Shader.PropertyToID("_Presence"),
            SnLitId = Shader.PropertyToID("_Lit"), SnHeatId = Shader.PropertyToID("_Heat"), SnTurnId = Shader.PropertyToID("_Turn"), SnGlowId = Shader.PropertyToID("_Glow"),
            SnFlashColourId = Shader.PropertyToID("_FlashColor"), SnFlashAmountId = Shader.PropertyToID("_FlashAmount");
        // Long thin clouds lying on the dusk, lit from under: (which way round, how high, how long), typed.
        private static readonly Vector3[] SnCloudRows = { new Vector3(200f, 7.2f, 9f), new Vector3(150f, 9.4f, 7f), new Vector3(20f, 8.0f, 8f), new Vector3(300f, 10.6f, 6.5f) };
        private readonly List<Transform> _snSmall = new List<Transform>(6), _snSmallTails = new List<Transform>(6), _snTown = new List<Transform>(4);
        // Which side the lens stands for THE RISE: the side nobody is standing on (film s1 went through somebody's hair).
        private float _snSide = 1f;

        private void BuildSean()
        {
            _duskLow = Wall("DuskGround", 0, 1.0f, new Color(.14f, .05f, .03f, .88f), radius: 15f);
            _duskGlow = Wall("DuskHorizon", 1.0f, 2.3f, new Color(.23f, .085f, .04f, 1f), radius: 15f, emission: .55f);
            _duskHigh = Wall("DuskSky", 2.3f, 40, new Color(.21f, .08f, .05f, .86f), radius: 15f, emission: .12f, cap: true);
            // The horizon has depth now: the last of the sun as a thin hot band on it, and the sky above it stepping down from
            // ember to night instead of being one flat wall. A hair inside the others, so they never fight for the same pixels.
            _duskSun = Wall("DuskSunBand", 1.0f, 1.34f, new Color(.95f, .46f, .1f, .9f), radius: 14.96f, emission: .6f);
            _duskMid = Wall("DuskEmberSky", 2.3f, 6.2f, new Color(.42f, .15f, .06f, .5f), radius: 14.93f, emission: .2f);
            RagoUnlitBackdrop(_duskLow); RagoUnlitBackdrop(_duskGlow); RagoUnlitBackdrop(_duskHigh); RagoUnlitBackdrop(_duskSun); RagoUnlitBackdrop(_duskMid);
            // And higher, the ember going out into night, with long clouds lying on it lit from under by the last of the sun:
            // the sky was one flat brown wall over the town.
            _duskNight = Wall("DuskNightSky", 6.2f, 13f, new Color(.30f, .10f, .08f, .38f), radius: 14.9f, emission: .1f);
            RagoUnlitBackdrop(_duskNight);
            for (int i = 0; i < SnCloudRows.Length; i++)
            {
                int cloud = AddGlow("DuskCloud" + i, new Color(1f, .42f, .14f, 1), billboard: false, band: true, falloff: 1.4f, core: .15f);
                _pieces[cloud].Renderer.sharedMaterial.SetFloat("_Tips", 1.2f);
                _snClouds.Add(cloud);
            }
            var court = PhaisterProp.OnCourt(_root.transform.position + Vector3.up * .2f);
            float under = _root.transform.InverseTransformPoint(court).y;
            _snFloor = Mathf.Abs(under) < .6f ? Mathf.Max(0, under) : 0;
            _duskSunGlow = AddGlow("DuskSun", new Color(1f, .5f, .12f, 1), falloff: 1.7f, core: .25f);
            for (int i = 0; i < SnEvening.Length; i++) _snEveningGlows.Add(AddGlow("EveningStar" + i, new Color(1f, .86f, .6f, 1), falloff: 2.6f, core: 1.2f));
            // The far roofs of home, four quarters of one typed town, and the lights on their strings of flags.
            for (int i = 0; i < 4; i++)
            {
                var town = UltimateProp.Spawn("duskline", _root.transform, UltimateProp.ParolPalette);
                if (town == null) break;
                _snTown.Add(town.transform);
                for (int k = 0; k < SnFiesta.Length; k++)
                    _snFiestaGlows.Add(AddGlow("FiestaLight" + i + "_" + k, SnLightColour[(i + k) % 3], falloff: 2.2f, core: .8f));
            }

            // Where he will land, drawn on the court: the live blast's own reach (`telegraphRadius` 5.4) round where he stands.
            // ⚠️ IT IS HIS MARK NOW, NOT A PLAIN RING: a parol laid on the ground inside it, a ring of ticks that steps round like
            // a clock, and the star's five points lighting one after another until he leaves the ground. The bright outer ring
            // is still the first thing read: this is where he comes down.
            // ⚠️ AND IT IS LIGHT, NOT TINTED MESHES (owner, of film p3: "the ground effects/magic circles are lacking proper
            // shaders and effects"). One quad drawn by `Shaders/ParolGround`: the ring with light running round it, the ticks,
            // the parol's underside as the lantern throws it down, each point igniting, all of it hotter as the leap nears.
            // Under it, court-wide and faint, the lantern's own light on the ground (`_snCast`).
            _snCast = SnGround("LanternCast", 1f, new Color(1, .5f, .14f, .05f));
            _snMark = SnGround("LandingMark", 0f, new Color(1, .56f, .16f, .3f));
            for (int i = 0; i < 5; i++) _snLandTips.Add(AddGlow("LandingPoint" + i, new Color(1, .8f, .36f, 1), falloff: 2f, core: .9f));
            StageOthers(2.2f, 9.5f);
            if (_performance != null && _performance.Shots.Count > 2)
            {
                // The others stand wherever they stood; the lens takes whichever side keeps them furthest from where it goes.
                var shot = _performance.Shots[2];
                float best = -1f;
                foreach (float tried in new[] { 1f, -1f })
                {
                    float clear = float.MaxValue;
                    foreach (var other in _others)
                        for (int step = 0; step <= 4; step++)
                        {
                            var lens = Vector3.Lerp(shot.EyeFrom, shot.EyeTo, step / 4f); lens.x *= tried; lens.y = 0;
                            clear = Mathf.Min(clear, (other.Home - lens).magnitude);
                        }
                    if (clear > best) { best = clear; _snSide = tried; }
                }
            }
            foreach (var other in _others)
            {
                _snMarks.Add(SnGround("InTheRing", 2f, new Color(1, .3f, .06f, .5f)));
            }

            // The parol. Everything of it is built one unit across under one holder, so the lantern he holds and the giant
            // one over the court are the same object grown.
            _snParol = new GameObject("Parol").transform; _snParol.SetParent(_root.transform, false);
            BuildSeanParol();
            _parolHalo = SeanPart(AddGlow("ParolWarmth", new Color(1, .38f, .05f, .22f), falloff: 2.6f, core: .12f));
            // Paper is lit from inside: a soft light lying on its face, which is what keeps the underside from going dull
            // when it lies over the court with the sun on its back.
            _parolFace = SeanPart(AddGlow("ParolLitFace", new Color(1, .5f, .14f, 1), billboard: false, falloff: 1.3f, core: .2f));
            // Bright things read as LIGHTS against the dusk: a hot heart, a glow on each point of the frame (which is what is
            // left burning after the burst), and light carried up the trail.
            _parolHeartGlow = SeanPart(AddGlow("ParolHeartLight", new Color(1, .74f, .3f, 1), falloff: 2f, core: .9f));
            for (int i = 0; i < 5; i++) _snTipGlows.Add(SeanPart(AddGlow("ParolPointLight" + i, new Color(1, .7f, .26f, 1), falloff: 2f, core: .9f)));
            for (int i = 0; i < 3; i++) _snTrailGlows.Add(AddGlow("LanternTrailLight" + i, new Color(1, .66f, .22f, 1), falloff: 1.9f, core: .7f));
            // Its rays: twelve tapered spikes of light, long and short by turns, out past its crown.
            var spike = SnSpindle(4, 0f, .14f, 1f);
            for (int i = 0; i < 12; i++) _snRays.Add(SeanPart(Add("LanternRay" + i, spike, i % 2 == 0 ? new Color(1, .84f, .4f, .92f) : new Color(1, .56f, .16f, .85f), 1f)));
            // What it throws when it ignites: a ring across the sky with a softer one behind it, a rain of little lit stars and
            // paper diamonds, and its light standing on the court.
            _snShock = Add("IgnitionRing", VfxShapes.TwoSided(VfxShapes.Collar(56, .02f, .93f)), new Color(1, .7f, .22f, .9f), 1f);
            _snShockSoft = Add("IgnitionRingSoft", VfxShapes.TwoSided(VfxShapes.Collar(56, .02f, .78f)), new Color(1, .4f, .1f, .4f), .8f);
            _snShockGlow = AddGlow("IgnitionSheet", new Color(1, .55f, .16f, 1), billboard: false, falloff: 1.2f, core: .1f);
            _snFlash = AddGlow("LanternFlash", new Color(1, .7f, .3f, 1), falloff: 1.6f, core: .5f);
            // ⚠️ THE SHAFTS ARE LIGHT, NOT PILLARS: each is three sheets of glow crossed on one line, soft to nothing at their
            // edges and at both ends, with a pool of light and a thin bright rim where it meets the court. The plain tall
            // prisms they were had hard sides.
            for (int i = 0; i < SnShafts.Length; i++)
            {
                for (int sheet = 0; sheet < 3; sheet++)
                {
                    int index = AddGlow("LanternShaft" + i + "_" + sheet, new Color(1, .62f, .22f, 1), billboard: false, band: true, falloff: 1.5f, core: .3f);
                    _pieces[index].Renderer.sharedMaterial.SetFloat("_Tips", .45f);
                    _snShaftPieces.Add(index);
                }
                _snShaftPools.Add(SnGround("LanternShaftPool" + i, 1f, new Color(1, .66f, .26f, .2f)));
            }
            var star = SnPuffedStar(5, .42f, .26f); var diamond = SnPuffedStar(4, .5f, .2f);
            for (int i = 0; i < SnRain.Length; i++)
            {
                _snRainPieces.Add(Add("IgnitionStar" + i, i % 2 == 0 ? star : diamond, i % 3 == 0 ? new Color(1, .93f, .7f, 1) : i % 3 == 1 ? new Color(1, .74f, .24f, 1) : new Color(1, .44f, .12f, 1), 1.1f));
                _snRainGlows.Add(AddGlow("IgnitionSpark" + i, new Color(1, .7f, .25f, 1), falloff: 2.2f, core: .9f));
            }
            for (int i = 0; i < 8; i++)
            {
                _snShedPieces.Add(Add("ClimbEmber" + i, diamond, i % 2 == 0 ? new Color(1, .8f, .3f, 1) : new Color(1, .42f, .1f, 1), 1.1f));
                _snShed.Add(AddGlow("ClimbSpark" + i, new Color(1, .6f, .18f, 1), falloff: 2.2f, core: .8f));
            }
            // The climb's fire: a wide soft ribbon with a hot core down its middle, both tapering to nothing behind it, and five
            // tongues licking back off it.
            _snTrailOuter = Line("LanternTrailFlame", 20, .16f, new Color(1, .36f, .06f, .55f));
            _snTrail = Line("LanternTrail", 20, .16f, new Color(1, .92f, .6f, .96f));
            var taper = new AnimationCurve(new Keyframe(0, 0), new Keyframe(.55f, .42f), new Keyframe(.9f, 1f), new Keyframe(1, .7f));
            _snTrail.widthCurve = taper; _snTrailOuter.widthCurve = taper;
            for (int i = 0; i < 5; i++)
                _snTrailFlames.Add(Add("ClimbFlame" + i, VfxShapes.Tongue(5, .3f, .22f, .5f, .1f, 310 + i), new Color(1, i % 2 == 0 ? .42f : .7f, i % 2 == 0 ? .05f : .16f, .8f), .9f));
            // Its lights: a bulb on the cord (the model's own, copied round), and the glow of it on top.
            for (int ring = 0; ring < SnRings.Length; ring++)
                for (int i = 0; i < (int)SnRings[ring].y; i++)
                {
                    _snBulbs.Add(_snBulbSource != null ? Object.Instantiate(_snBulbSource.gameObject, _snParol, false).transform : null);
                    _snBulbSkins.Add(_snBulbs[_snBulbs.Count - 1] != null ? _snBulbs[_snBulbs.Count - 1].GetComponentInChildren<Renderer>(true) : null);
                    _snLights.Add(SeanPart(AddGlow("ParolLight" + ring + "_" + i, SnLightColour[(int)SnRings[ring].z], falloff: 2.2f, core: .8f)));
                }
            if (_snBulbSource != null) _snBulbSource.gameObject.SetActive(false);

            // The paper lanterns of home: small parols with their tails, each with its own warm light.
            for (int i = 0; i < LanternSeats.Length; i++)
            {
                _lanternGlows.Add(AddGlow("LanternGlow" + i, new Color(1, .44f, .1f, .28f), falloff: 2.6f, core: .1f));
                var small = UltimateProp.Spawn("parol_small", _root.transform, UltimateProp.ParolPalette, .03f);
                _snSmall.Add(small != null ? small.transform : null);
                _snSmallTails.Add(small != null ? PaeteProp.Find(small, "tails") : null);
            }
            // The coil: a ring of flames, each with a hotter one inside it, their light on the ground, and what the leap
            // leaves: a ring thrown out along the court and three jets under his feet.
            _snCoilGlow = SnGround("CoilGroundFire", 4f, new Color(1, .46f, .1f, .25f));
            for (int i = 0; i < 6; i++)
            {
                _footFlames.Add(Add("CoilFlame" + i, VfxShapes.Tongue(5, .24f, .15f, .35f, .08f, 240 + i), new Color(1, i % 2 == 0 ? .32f : .5f, .04f, .72f), .6f));
                _snCoilCores.Add(Add("CoilFlameCore" + i, VfxShapes.Tongue(4, .2f, .1f, .5f, .06f, 260 + i), new Color(1, .86f, .38f, .9f), 1f));
            }
            _snLaunch = SnGround("LeapRing", 3f, new Color(1, .7f, .26f, .3f));
            _snJetGlow = AddGlow("LeapJetLight", new Color(1, .62f, .2f, 1), falloff: 1.8f, core: .7f);
            BuildSeanKit(star, diamond);
            // ⚠️ PARTICLES WITH A SHAPE AND A PATH EACH (owner: "be more creative with particle effects"): embers that rise and
            // curl off the mark and off the coil, spikes thrown along the court by the leap, shards thrown from the lantern's
            // rim when it ignites and bursts, and capiz flakes that flutter down from the burst and glint as they turn.
            for (int i = 0; i < 12; i++)
            {
                _snMarkEmbers.Add(Add("MarkEmber" + i, i % 3 == 0 ? star : diamond, i % 2 == 0 ? new Color(1, .84f, .4f, 1) : new Color(1, .5f, .14f, 1), 1.4f));
                _snMarkEmberGlows.Add(AddGlow("MarkEmberLight" + i, new Color(1, .6f, .2f, 1), falloff: 2.2f, core: .9f));
            }
            for (int i = 0; i < 8; i++) _snCoilEmbers.Add(Add("CoilEmber" + i, diamond, i % 2 == 0 ? new Color(1, .9f, .5f, 1) : new Color(1, .46f, .1f, 1), 1.4f));
            for (int i = 0; i < 10; i++) _snLeapSparks.Add(Add("LeapSpark" + i, spike, i % 2 == 0 ? new Color(1, .9f, .56f, 1) : new Color(1, .56f, .16f, 1), 1.4f));
            for (int i = 0; i < 12; i++) _snShards.Add(Add("IgnitionShard" + i, spike, i % 3 == 0 ? new Color(1, .95f, .74f, 1) : new Color(1, .68f, .22f, 1), 1.4f));
            var flake = SnPuffedStar(4, .62f, .08f);
            for (int i = 0; i < 18; i++)
            {
                _snFlakes.Add(Add("CapizFlake" + i, i % 4 == 0 ? star : flake, i % 3 == 0 ? new Color(1, .82f, .4f, 1) : new Color(1, .96f, .84f, 1), 1.2f));
                _snFlakeGlints.Add(AddGlow("CapizGlint" + i, new Color(1, .9f, .66f, 1), falloff: 2.4f, core: 1.2f));
            }
            for (int i = 0; i < 3; i++)
                _snJets.Add(Add("LeapJet" + i, VfxShapes.Tongue(5, .26f, .1f, .4f, .08f, 280 + i), new Color(1, i == 1 ? .84f : .46f, i == 1 ? .36f : .08f, .85f), 1f));
            for (int i = 0; i < 8; i++)
                _sparks.Add(Add("RiseSpark" + i, i % 2 == 0 ? star : diamond, new Color(1, .76f, .3f, .95f), 1f));
        }

        /// <summary>
        /// ⚠️ THE PAROL IS MODELLED AND PAINTED (2026-10-10; it was five stick prisms, a flat splat, a collar and two flat stars
        /// while the staging was judged): `Models/UltimateProps/parol`, built in `tools/build_ultimate_props_sean.py` the way
        /// the hand companions and Paete's plants are (round forms, smooth normals, a painted atlas beside it) and dressed
        /// as the cast is. A first model of flat palette facets was replaced the same day: the owner, of a prop made that
        /// way, "doesn't fit the hand painted illustrated stylistic art design of our characters". One unit from its middle to a point, its face on +z: the side the lens sees in his hands and
        /// the underside the court sees when it lies over. Every joint posed here is a node of its own, and each one's seat is
        /// read from the model, so the build (sticks, then panels) and the burst (panels and scallops thrown) are the model's
        /// own parts moving. A missing model is a warning in the log and empty joints here: the cutscene runs without it.
        /// </summary>
        private void BuildSeanParol()
        {
            // Its ink is thinner than a person's for its size: at 24 m across, a person's line would be a beam.
            var model = UltimateProp.Spawn("parol", _snParol, UltimateProp.ParolPalette, .006f);
            if (model == null) return;
            for (int i = 0; i < 5; i++)
            {
                _snStick[i] = PaeteProp.Find(model, "stick-" + i); _snPoint[i] = PaeteProp.Find(model, "point-" + i);
                _snTail[i] = PaeteProp.Find(model, "tail-" + i); _snTailEnd[i] = PaeteProp.Find(model, "tail-" + i + "-end");
                if (_snStick[i] != null) _snStickHome[i] = _snStick[i].localPosition;
                if (_snPoint[i] != null) _snPointHome[i] = _snPoint[i].localPosition;
            }
            for (int i = 0; i < 10; i++)
            {
                _snPetal[i] = PaeteProp.Find(model, "petal-" + i);
                if (_snPetal[i] != null) _snPetalHome[i] = _snPetal[i].localPosition;
            }
            _snHeart = PaeteProp.Find(model, "heart"); _snHoop = PaeteProp.Find(model, "ring"); _snBack = PaeteProp.Find(model, "back");
            _snCrown = PaeteProp.Find(model, "crown"); _snStrings = PaeteProp.Find(model, "strings"); _snBulbSource = PaeteProp.Find(model, "bulb");
        }

        /// <summary>
        /// What the shared effects kit draws for him (see `_snKit`). His colours: fire gold, ember, capiz cream.
        /// </summary>
        private void BuildSeanKit(Mesh star, Mesh diamond)
        {
            _snKit = UltimateFx.Ready;
            Color gold = new Color(1f, .74f, .22f, 1), ember = new Color(.95f, .32f, .06f, 1), cream = new Color(1f, .95f, .78f, 1), deep = new Color(.8f, .17f, .05f, 1);
            // The dome: a lattice of capiz panes and ten thin ribs over the landing mark. It is drawn rim first and apex
            // last as it rises, and the other way when the leap takes it.
            // ⚠️ ONE STRUCTURE OF LIGHT, IN THREE LAYERS (it was every pane a flat opaque diamond, which from the low shot read as
            // confetti over the town, film p8): its ribs and the hoops its panes sit on, which carry the curve; each pane's
            // bright rim, lit by a wave that runs round the rings so only some are on at once; and each pane's body, a
            // faint capiz sheen you see through. The side away from the lens is thinner, for depth, so it does not turn.
            _snDome = SnTorn("LandingDome", SnDomeMesh(0), gold, cream, UltimateFx.Streak, new Vector2(2.4f, 1f), new Vector2(.5f, 0), emit: .7f, sweep: -1.4f, fill: .6f);
            _snDomeRims = SnTorn("LandingDomePaneRims", SnDomeMesh(1), cream, gold, UltimateFx.Smoke, new Vector2(1.2f, 5f), new Vector2(.1f, .55f), emit: .8f, sweep: -1.4f, fill: .1f);
            _snDomeSheen = SnTorn("LandingDomePaneSheen", SnDomeMesh(2), cream, gold, UltimateFx.Smoke, new Vector2(1.2f, 5f), new Vector2(.1f, .55f), emit: .3f, sweep: -1.4f, fill: .35f);
            foreach (int part in new[] { _snDome, _snDomeRims, _snDomeSheen }) _pieces[part].Renderer.sharedMaterial.SetFloat("_Shape", 0f);
            // What gives it height from straight above: a hoop of light that climbs it (so it is seen to shrink as it
            // rises), a soft glow carried up inside that hoop, and the lantern's cast on the court under it, which grows
            // as the dome does.
            _snDomeHoop = SnTorn("LandingDomeRisingHoop", SnBandMesh(1f, .028f), cream, gold, UltimateFx.Streak, new Vector2(8f, 1f), new Vector2(.6f, 0), emit: 1.1f, fill: .5f);
            _snDomeGlow = AddGlow("LandingDomeInnerLight", new Color(1, .72f, .3f, 1), billboard: false, falloff: 1.4f, core: .2f);
            _snDomePool = SnGround("LandingDomePool", 1f, new Color(1, .6f, .2f, .1f));
            for (int i = 0; i < 12; i++)
            {
                _snOrbit.Add(Add("DomeSpark" + i, i % 2 == 0 ? star : diamond, i % 3 == 0 ? new Color(1, .95f, .74f, 1) : new Color(1, .7f, .24f, 1), 1.4f));
                _snOrbitGlows.Add(AddGlow("DomeSparkLight" + i, new Color(1, .68f, .24f, 1), falloff: 2.2f, core: .9f));
                // Each spark draws a short torn tail behind it, so the two tilted rings they ride are read from above.
                _snOrbitTails.Add(SnTorn("DomeSparkTail" + i, SnTrailMesh(), cream, gold, UltimateFx.Streak, new Vector2(1.4f, 1f), new Vector2(3f, 0), emit: .9f, fill: .35f));
            }
            // The trails: two crossed ribbons of ember fire with two of gold down their middle, for the lantern's climb and
            // again under him as he leaps. The mask runs back along them, so they tear into streaks behind. Bright down
            // the middle and ember where they thin and tear: fire has a hot core, and a flat yellow blade is a leaf (film p7).
            var ribbon = SnTrailMesh();
            for (int i = 0; i < 4; i++)
            {
                bool core = i >= 2;
                _snClimbRibbons.Add(SnTorn("ClimbRibbon" + i, ribbon, core ? cream : gold, core ? gold : deep, core ? UltimateFx.Streak : UltimateFx.Smoke,
                    core ? new Vector2(1.5f, 1f) : new Vector2(2.2f, 1.3f), new Vector2(core ? 3.4f : 2.2f, 0), emit: core ? 1.1f : .45f, fill: core ? .4f : .12f));
                _snLeapRibbons.Add(SnTorn("LeapRibbon" + i, ribbon, core ? cream : gold, core ? gold : deep, core ? UltimateFx.Streak : UltimateFx.Smoke,
                    core ? new Vector2(1.5f, 1f) : new Vector2(2.2f, 1.3f), new Vector2(core ? 4.2f : 2.8f, 0), emit: core ? 1.1f : .45f, fill: core ? .4f : .12f));
            }
            // The leap: a crown of fire thrown up off the court round where he stood, and four wisps each its own way.
            for (int i = 0; i < _snLeapWisps.Length; i++)
                // Fire gold in the body, ember orange where it thins and tears. No red: two red hooks at his feet were ribbons (film p8).
                _snLeapWisps[i] = UltimateFx.Wisp(_root.transform, SnLeapShapes[i], i % 3 == 0 ? cream : gold, new Color(1f, .48f, .1f, 1), i % 2 == 0 ? UltimateFx.Smoke : UltimateFx.Streak, 4)
                    .With("_Band", .5f).With("_Shape", .9f).With("_Emit", .7f).With("_Glow", 1.1f).With("_Scroll", new Vector4(2.4f, 0, 0, 0));
            // The burst: six wisps curling down and out from the lantern's rim, big, slow, still tearing on the last frame.
            for (int i = 0; i < _snBurstWisps.Length; i++)
                _snBurstWisps[i] = UltimateFx.Wisp(_root.transform, SnBurstShapes[i], i % 3 == 0 ? ember : gold, deep, i % 2 == 0 ? UltimateFx.Smoke : UltimateFx.Streak, 8)
                    .With("_Band", .5f).With("_Shape", .9f).With("_Emit", .55f).With("_Glow", 1f).With("_Scroll", new Vector4(1.3f, 0, 0, 0));
            // The wreath: when the paper has gone, the hoop and the five sticks each wear a band of slowly tearing light,
            // so what is left is a burning frame and not thin sticks and a hoop.
            _snWreathHoop = SeanPart(SnTorn("FrameWreathHoop", SnBandMesh(1.03f, .085f), gold, cream, UltimateFx.Smoke, new Vector2(9f, 1.2f), new Vector2(.5f, 0), emit: .8f, fill: .1f));
            _snWreathHoopIn = SeanPart(SnTorn("FrameWreathHoopInner", SnBandMesh(.97f, .16f), ember, gold, UltimateFx.Cells, new Vector2(7f, 1.4f), new Vector2(-.35f, .1f), emit: .4f));
            var straight = SnStickBandMesh();
            for (int i = 0; i < 5; i++)
            {
                int band = SeanPart(SnTorn("FrameWreathStick" + i, straight, i % 2 == 0 ? gold : ember, i % 2 == 0 ? cream : gold, UltimateFx.Streak, new Vector2(3f, 1f), new Vector2(i % 2 == 0 ? .7f : -.6f, 0), emit: .8f, fill: .15f));
                float a0 = (90f + i * 72f) * Mathf.Deg2Rad, a1 = (90f + (i + 2) * 72f) * Mathf.Deg2Rad;
                Vector3 p0 = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0), p1 = new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0);
                var band_ = _pieces[band].Transform;
                band_.localPosition = (p0 + p1) * .5f + Vector3.forward * .03f; band_.localRotation = Quaternion.FromToRotation(Vector3.up, (p1 - p0).normalized);
                band_.localScale = new Vector3(.13f, (p1 - p0).magnitude, 1f);
                _snWreathSticks.Add(band);
            }
            _pieces[_snWreathHoop].Transform.localPosition = Vector3.forward * .03f; _pieces[_snWreathHoopIn].Transform.localPosition = Vector3.forward * .02f;
        }

        /// <summary>
        /// A piece of <paramref name="mesh"/> drawn by the kit's shader (`Shaders/UltimateErode`: uv.x along it, uv.y across,
        /// a vertex's alpha its own share of erosion), for a shape the kit does not have. Hidden until <see cref="SnTear"/>.
        /// </summary>
        private int SnTorn(string name, Mesh mesh, Color core, Color edge, int look, Vector2 grain, Vector2 scroll, float emit = .6f, float sweep = 0f, float fill = 0f)
        {
            int index = Add(name, mesh, core, .8f);
            var piece = _pieces[index]; piece.Renderer.enabled = false;
            var shader = Resources.Load<Shader>("Shaders/UltimateErode");
            if (shader == null || !shader.isSupported) return index;
            var material = new Material(shader) { name = name };
            material.SetColor("_Core", core); material.SetColor("_Edge", edge); material.SetColor("_Hot", Color.Lerp(core, edge, .5f));
            material.SetFloat("_Look", look); material.SetVector("_Scale", new Vector4(grain.x, grain.y, 0, 0)); material.SetVector("_Scroll", new Vector4(scroll.x, scroll.y, 0, 0));
            material.SetFloat("_Emit", emit); material.SetFloat("_Sweep", sweep); material.SetFloat("_Fill", fill); material.SetFloat("_Glow", 1f);
            // A wide band of the edge colour and edges that thin: bright in its body, ember where it tears.
            material.SetFloat("_Band", .45f); material.SetFloat("_Shape", .85f);
            material.SetFloat("_Seed", index * .37f);
            piece.Renderer.sharedMaterial = material;
            VfxRenderTag.Own(piece.Renderer.gameObject, material);
            return index;
        }

        /// <summary>A torn piece's moment: its mask's clock, how much of it is gone (0 fresh, 1 gone), how strongly it is drawn.</summary>
        private void SnTear(int index, float age, float erode, float opacity = 1f)
        {
            var p = _pieces[index];
            bool seen = _snKit && age >= 0f && erode < .999f && opacity > .002f;
            p.Renderer.enabled = seen;
            if (!seen) return;
            p.Block.SetFloat(SnAgeId, age); p.Block.SetFloat(SnErodeId, Mathf.Clamp01(erode)); p.Block.SetFloat(SnOpacityId, Mathf.Clamp01(opacity));
            p.Renderer.SetPropertyBlock(p.Block);
        }

        /// <summary>
        /// The dome over the landing mark, one unit out and one high, in the three layers it is drawn in. Six rings of small
        /// diamond panes, fewer toward the top, each ring half a pane round from the last: the lattice of a capiz window,
        /// bent over. <paramref name="layer"/> 0 is what holds it: ten ribs from rim to apex, a thin hoop under each ring of
        /// panes, a band at its foot. 1 is every pane's rim, 2 every pane's body inside its rim. uv.x is height (0 at the
        /// rim), so the shader draws it rim first; a pane's uv.y is how far round it is, so a wave can run round the rings.
        /// A vertex's alpha is its share of erosion, and the side away from the lens (-z) is given less: thinner, further.
        /// </summary>
        private static Mesh SnDomeMesh(int layer)
        {
            var at = new List<Vector3>(); var uv = new List<Vector2>(); var paint = new List<Color>(); var triangles = new List<int>();
            Vector3 On(float lift, float round) => new Vector3(Mathf.Cos(lift) * Mathf.Cos(round), Mathf.Sin(lift), Mathf.Cos(lift) * Mathf.Sin(round));
            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 e, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ue)
            {
                int first = at.Count;
                at.Add(a); at.Add(b); at.Add(c); at.Add(e); uv.Add(ua); uv.Add(ub); uv.Add(uc); uv.Add(ue);
                foreach (var corner in new[] { a, b, c, e }) paint.Add(new Color(1, 1, 1, Mathf.Lerp(.55f, 1f, (corner.z + 1f) * .5f)));
                triangles.AddRange(new[] { first, first + 1, first + 2, first, first + 2, first + 3 });
            }
            // The rings of panes: (how high in degrees, how many, how big a pane is in degrees of height).
            var rings = new[] { new Vector3(9f, 30, 4f), new Vector3(22f, 28, 4f), new Vector3(35f, 24, 4f), new Vector3(48f, 20, 3.8f), new Vector3(61f, 14, 3.6f), new Vector3(74f, 8, 3.3f) };
            for (int ring = 0; ring < rings.Length; ring++)
            {
                int count = (int)rings[ring].y; float lift = rings[ring].x * Mathf.Deg2Rad, tall = rings[ring].z * Mathf.Deg2Rad;
                float wide = Mathf.PI / count * .5f, high = lift / (Mathf.PI * .5f);
                if (layer == 0)
                {
                    // The hoop this ring of panes sits on.
                    for (int i = 0; i < 48; i++)
                    {
                        float r0 = i * Mathf.PI * 2 / 48, r1 = (i + 1) * Mathf.PI * 2 / 48, thin = .006f;
                        Quad(On(lift - thin, r0), On(lift - thin, r1), On(lift + thin, r1), On(lift + thin, r0),
                            new Vector2(high, .5f), new Vector2(high, .5f), new Vector2(high, .5f), new Vector2(high, .5f));
                    }
                    continue;
                }
                for (int i = 0; i < count; i++)
                {
                    float round = (i + .5f * (ring % 2)) * Mathf.PI * 2 / count;
                    Vector3 top = On(lift + tall, round), right = On(lift, round + wide), bottom = On(lift - tall, round), left = On(lift, round - wide);
                    var middle = (top + right + bottom + left) * .25f;
                    Vector3 topIn = Vector3.Lerp(top, middle, .3f), rightIn = Vector3.Lerp(right, middle, .3f), bottomIn = Vector3.Lerp(bottom, middle, .3f), leftIn = Vector3.Lerp(left, middle, .3f);
                    var where = new Vector2(high, round / (Mathf.PI * 2));
                    if (layer == 2) { Quad(topIn, rightIn, bottomIn, leftIn, where, where, where, where); continue; }
                    Quad(top, right, rightIn, topIn, where, where, where, where); Quad(right, bottom, bottomIn, rightIn, where, where, where, where);
                    Quad(bottom, left, leftIn, bottomIn, where, where, where, where); Quad(left, top, topIn, leftIn, where, where, where, where);
                }
            }
            if (layer == 0)
            {
                for (int rib = 0; rib < 10; rib++)
                {
                    float round = (rib * 36f + 18f) * Mathf.Deg2Rad, half = .009f;
                    for (int step = 0; step < 10; step++)
                    {
                        float l0 = step * 8.6f * Mathf.Deg2Rad, l1 = (step + 1) * 8.6f * Mathf.Deg2Rad;
                        float h0 = half / Mathf.Max(.2f, Mathf.Cos(l0)), h1 = half / Mathf.Max(.2f, Mathf.Cos(l1));
                        Quad(On(l0, round - h0), On(l0, round + h0), On(l1, round + h1), On(l1, round - h1),
                            new Vector2(step / 10f, .5f), new Vector2(step / 10f, .5f), new Vector2((step + 1) / 10f, .5f), new Vector2((step + 1) / 10f, .5f));
                    }
                }
                for (int i = 0; i < 48; i++)
                {
                    float r0 = i * Mathf.PI * 2 / 48, r1 = (i + 1) * Mathf.PI * 2 / 48;
                    Quad(On(0, r0), On(0, r1), On(.03f, r1), On(.03f, r0), new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(.02f, .5f), new Vector2(.02f, .5f));
                }
            }
            var mesh = new Mesh { name = "Rago capiz dome " + layer };
            mesh.SetVertices(at); mesh.SetUVs(0, uv); mesh.SetColors(paint); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// A trail: a ribbon hanging from its head at the origin one unit down -y, one wide at its fullest, swelling just
        /// behind the head and narrowing along a slow wave to a thin tail. Its tail's alpha goes to nothing, so the shader
        /// tears it there. uv.x runs head to tail.
        /// </summary>
        private static Mesh SnTrailMesh()
        {
            float[] wide = { .34f, .78f, 1f, .94f, .82f, .7f, .58f, .47f, .37f, .28f, .2f, .13f, .07f, .02f };
            float[] sway = { 0f, .01f, .03f, .05f, .05f, .02f, -.03f, -.07f, -.08f, -.05f, .01f, .06f, .09f, .07f };
            var at = new List<Vector3>(); var uv = new List<Vector2>(); var paint = new List<Color>(); var triangles = new List<int>();
            for (int i = 0; i < wide.Length; i++)
            {
                float u = i / (float)(wide.Length - 1), there = i == 0 ? .7f : 1 - Mathf.Clamp01((u - .45f) / .55f);
                at.Add(new Vector3(sway[i] - wide[i] * .5f, -u, 0)); at.Add(new Vector3(sway[i] + wide[i] * .5f, -u, 0));
                uv.Add(new Vector2(u, 0)); uv.Add(new Vector2(u, 1)); paint.Add(new Color(1, 1, 1, there)); paint.Add(new Color(1, 1, 1, there));
                if (i == 0) continue;
                int n = i * 2;
                triangles.AddRange(new[] { n - 2, n - 1, n, n, n - 1, n + 1 });
            }
            var mesh = new Mesh { name = "Rago trail ribbon" };
            mesh.SetVertices(at); mesh.SetUVs(0, uv); mesh.SetColors(paint); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>A band lying in the lantern's plane round a circle, its width breathing six times round it. uv.x goes round.</summary>
        private static Mesh SnBandMesh(float radius, float half)
        {
            const int steps = 60;
            var at = new List<Vector3>(); var uv = new List<Vector2>(); var paint = new List<Color>(); var triangles = new List<int>();
            for (int i = 0; i <= steps; i++)
            {
                float a = i * Mathf.PI * 2 / steps, wide = half * (1f + .35f * Mathf.Sin(a * 6f));
                var out_ = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                at.Add(out_ * (radius - wide)); at.Add(out_ * (radius + wide));
                uv.Add(new Vector2(i / (float)steps, 0)); uv.Add(new Vector2(i / (float)steps, 1)); paint.Add(Color.white); paint.Add(Color.white);
                if (i == 0) continue;
                int n = i * 2;
                triangles.AddRange(new[] { n - 2, n - 1, n, n, n - 1, n + 1 });
            }
            var mesh = new Mesh { name = "Rago wreath band" };
            mesh.SetVertices(at); mesh.SetUVs(0, uv); mesh.SetColors(paint); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>A band along a stick: one long (y, about its middle) and one wide, torn at both ends.</summary>
        private static Mesh SnStickBandMesh()
        {
            float[] wide = { .1f, .7f, 1f, .8f, 1f, .75f, 1f, .7f, .1f };
            var at = new List<Vector3>(); var uv = new List<Vector2>(); var paint = new List<Color>(); var triangles = new List<int>();
            for (int i = 0; i < wide.Length; i++)
            {
                float u = i / (float)(wide.Length - 1), there = Mathf.Clamp01(Mathf.Min(u, 1 - u) * 6f);
                at.Add(new Vector3(-wide[i] * .5f, u - .5f, 0)); at.Add(new Vector3(wide[i] * .5f, u - .5f, 0));
                uv.Add(new Vector2(u, 0)); uv.Add(new Vector2(u, 1)); paint.Add(new Color(1, 1, 1, there)); paint.Add(new Color(1, 1, 1, there));
                if (i == 0) continue;
                int n = i * 2;
                triangles.AddRange(new[] { n - 2, n - 1, n, n, n - 1, n + 1 });
            }
            var mesh = new Mesh { name = "Rago wreath stick" };
            mesh.SetVertices(at); mesh.SetUVs(0, uv); mesh.SetColors(paint); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        private int SeanPart(int index) { _pieces[index].Transform.SetParent(_snParol, false); return index; }

        /// <summary>
        /// A flat mark of LIGHT lying on the court, drawn by `Shaders/ParolGround` (its header says what each mode is). One
        /// quad, a unit from its middle to its edge. If the shader is missing or will not run here it is a plain disc in
        /// <paramref name="plain"/>, so the cutscene still shows where he lands.
        /// </summary>
        private int SnGround(string name, float mode, Color plain)
        {
            var shader = Resources.Load<Shader>("Shaders/ParolGround");
            bool drawn = shader != null && shader.isSupported;
            int index = Add(name, drawn ? SnGroundQuad() : VfxShapes.Prism(40, .01f, 1f), plain, .6f);
            if (!drawn) return index;
            var material = new Material(shader) { name = name };
            material.SetFloat(SnModeId, mode);
            _pieces[index].Renderer.sharedMaterial = material;
            VfxRenderTag.Own(_pieces[index].Renderer.gameObject, material);
            return index;
        }

        private static Mesh SnGroundQuad()
        {
            var mesh = new Mesh { name = "Rago ground light" };
            mesh.vertices = new[] { new Vector3(-1, 0, -1), new Vector3(1, 0, -1), new Vector3(1, 0, 1), new Vector3(-1, 0, 1) };
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Lay a ground light at <paramref name="at"/> (its height is above the court), <paramref name="radius"/> metres out, and
        /// hand its shader the scene's own clock: nothing of it runs on shader time, the match is paused.
        /// </summary>
        private void SnLight(int index, Vector3 at, float radius, float presence, float age, float lit = 0, float heat = 0, float turn = 0, float glow = 1)
        {
            var p = _pieces[index];
            p.Transform.localPosition = at + Vector3.up * _snFloor; p.Transform.localRotation = Quaternion.identity;
            p.Transform.localScale = new Vector3(radius, 1, radius);
            var plain = p.Color; plain.a *= Mathf.Clamp01(presence);
            p.Block.SetColor("_Color", plain); p.Block.SetColor("_BaseColor", plain);
            p.Block.SetFloat(SnAgeId, age); p.Block.SetFloat(SnPresenceId, Mathf.Clamp01(presence)); p.Block.SetFloat(SnLitId, lit);
            p.Block.SetFloat(SnHeatId, heat); p.Block.SetFloat(SnTurnId, turn); p.Block.SetFloat(SnGlowId, glow);
            p.Renderer.SetPropertyBlock(p.Block);
            p.Renderer.enabled = presence > .002f;
        }

        /// <summary>Put a joint of a model somewhere, at a size; nothing of it is drawn at no size. A missing joint is skipped.</summary>
        private static void SnPose(Transform joint, Vector3 at, Quaternion turn, float size)
        {
            if (joint == null) return;
            bool there = size > .002f;
            if (joint.gameObject.activeSelf != there) joint.gameObject.SetActive(there);
            if (!there) return;
            joint.localPosition = at; joint.localRotation = turn; joint.localScale = Vector3.one * size;
        }

        /// <summary>Arriving with weight: past its size and back to it (0 before, 1 after).</summary>
        private static float SnSettle(float u)
        {
            if (u <= 0) return 0; if (u >= 1) return 1;
            float v = u - 1; return 1 + 2.70158f * v * v * v + 1.70158f * v * v;
        }

        /// <summary>Flat facets from loose triangles, each turned to face away from <paramref name="inside"/>.</summary>
        private static Mesh SnFacets(string name, List<Vector3> corners, Vector3 inside)
        {
            var triangles = new int[corners.Count];
            for (int i = 0; i < corners.Count; i += 3)
            {
                var normal = Vector3.Cross(corners[i + 1] - corners[i], corners[i + 2] - corners[i]);
                var outward = (corners[i] + corners[i + 1] + corners[i + 2]) / 3f - inside;
                if (Vector3.Dot(normal, outward) < 0) { var swap = corners[i + 1]; corners[i + 1] = corners[i + 2]; corners[i + 2] = swap; }
                triangles[i] = i; triangles[i + 1] = i + 1; triangles[i + 2] = i + 2;
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(corners); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// A little star with body: its points in the XY plane, one unit out, raised to a peak on both faces, so every point
        /// is two facets and it catches the light as it tumbles. What his fire sheds and rains, in place of flat stars and
        /// round blobs. Four points is a paper diamond.
        /// </summary>
        private static Mesh SnPuffedStar(int points, float inner, float puff)
        {
            var corners = new List<Vector3>(points * 12);
            Vector3 front = new Vector3(0, 0, puff), back = new Vector3(0, 0, -puff);
            for (int i = 0; i < points * 2; i++)
            {
                float a0 = Mathf.PI * .5f + i * Mathf.PI / points, a1 = Mathf.PI * .5f + (i + 1) * Mathf.PI / points;
                float r0 = i % 2 == 0 ? 1f : inner, r1 = i % 2 == 0 ? inner : 1f;
                var p0 = new Vector3(Mathf.Cos(a0) * r0, Mathf.Sin(a0) * r0, 0); var p1 = new Vector3(Mathf.Cos(a1) * r1, Mathf.Sin(a1) * r1, 0);
                corners.Add(front); corners.Add(p0); corners.Add(p1);
                corners.Add(back); corners.Add(p1); corners.Add(p0);
            }
            return SnFacets("Rago puffed star", corners, Vector3.zero);
        }

        /// <summary>
        /// Two pyramids base to base along y: a point at <paramref name="foot"/>, one unit wide at <paramref name="waist"/>, a
        /// point at <paramref name="tip"/>: a ray of light with width and a taper.
        /// </summary>
        private static Mesh SnSpindle(int sides, float foot, float waist, float tip)
        {
            var corners = new List<Vector3>(sides * 6);
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2 / sides, a1 = (i + 1) * Mathf.PI * 2 / sides;
                var p0 = new Vector3(Mathf.Cos(a0), waist, Mathf.Sin(a0)); var p1 = new Vector3(Mathf.Cos(a1), waist, Mathf.Sin(a1));
                corners.Add(new Vector3(0, tip, 0)); corners.Add(p0); corners.Add(p1);
                corners.Add(new Vector3(0, foot, 0)); corners.Add(p1); corners.Add(p0);
            }
            return SnFacets("Rago spindle", corners, new Vector3(0, waist, 0));
        }

        private void RagoUnlitBackdrop(int index)
        {
            var shader = Shader.Find("Sprites/Default");
            if(shader == null) return;
            var piece = _pieces[index];
            var material = new Material(shader) { name="Rago dusk backdrop", color=piece.Color };
            piece.Renderer.sharedMaterial=material;
            VfxRenderTag.Own(piece.Renderer.gameObject,material);
        }

        private Vector3 RagoParolCentre => (_heldItem != null ? FreePalm : BothPalms) + new Vector3(0,.12f,.30f);

        /// <summary>Where the parol is: between his palms, lifted over his head, then up over the court.</summary>
        private Vector3 SeanParolAt(float t)
        {
            var held = RagoParolCentre; var overhead = HeadPoint + new Vector3(0, .7f, .2f);
            // ⚠️ NOTHING OF IT MOVES ON A STRAIGHT LINE (owner, 2026-10-10, of another cutscene's prop: "tweened so poorly").
            // He carries it up on an arc out from his chest. Let go, it sinks a hand's width before it takes, climbs on a
            // slow S that dies out as it rises, goes a little past its height and settles back to it. The times are the same.
            float lift = Ease(SnLift, SnRelease, t);
            var from = Vector3.Lerp(held, overhead, lift) + new Vector3(0, 0, .22f) * Mathf.Sin(lift * Mathf.PI);
            if (t <= SnRelease) return from;
            float u = Mathf.InverseLerp(SnRelease, SnTop, t), climb = 1 - (1 - u) * (1 - u);
            float sink = -.12f * Mathf.Sin(Mathf.Clamp01(u / .12f) * Mathf.PI);
            float past = .55f * Mathf.Sin(Mathf.Clamp01((t - (SnTop - .45f)) / .8f) * Mathf.PI);
            var sway = new Vector3(Mathf.Sin(climb * Mathf.PI * 2) * .5f * (1 - climb), 0, 0);
            return Vector3.Lerp(overhead, new Vector3(0, SnHigh, .5f), climb) + Vector3.up * (sink + past) + sway;
        }

        /// <summary>How big it is, in metres from its middle to a point: a hand's width in his hands, the court's width at the top.</summary>
        private float SeanParolSize(float t)
        {
            float small = _heldItem != null ? .26f : .30f;
            float grown = Mathf.InverseLerp(SnRelease + .1f, SnTop, t);
            return Mathf.Lerp(small, SnGiant, grown * grown * (3 - 2 * grown) * (.4f + .6f * grown));
        }

        /// <summary>THE RISE is shot looking at the lantern wherever it has got to (shot 2).</summary>
        private void SeanFrame(int index, float t, ref Vector3 eye, ref Vector3 look, ref float fov)
        {
            // The last shot is low and wide, so the lens is kept well off anyone standing near it: at 2.8 m a player was still a
            // third of the frame (film s3). The close shots are left where they are authored (pushing them moved them behind
            // someone, film s4), and THE RISE is shot from above head height so it looks over whoever is there.
            if (index == 4)
            {
                eye = KeepLensOffOthers(eye, 4.4f);
                // And it goes up with him: the lens rises a third as far as he does, and looks at him all the way.
                float up = LiftAt(t);
                eye.y += up * .34f; look = new Vector3(0, 1.1f + up, 0);
                // ⚠️ THEN ROUND BEHIND HIM (owner 2026-10-09: "it should also tween to the back of sean in a tpv type of way before
                // going to fpv"). With him hanging at the top, the same shot swings round him to a third-person place behind and
                // over his shoulder, looking where he looks, and holds there; only then does his own screen go into his eyes
                // (`SeanHandBack`). It goes round the side the lens was already on, so it never crosses his face.
                float behind = Ease(SnBehind, SnBehindBy, t);
                if (behind > 0)
                {
                    var centre = new Vector3(0, 1.1f + up, 0);
                    float from = Mathf.Atan2(eye.x, eye.z), to = (eye.x >= 0 ? 1 : -1) * Mathf.PI * .97f;
                    float angle = Mathf.Lerp(from, to, behind), reach = Mathf.Lerp(new Vector2(eye.x, eye.z).magnitude, 3.4f, behind);
                    eye = new Vector3(Mathf.Sin(angle) * reach, Mathf.Lerp(eye.y, centre.y + 1.25f, behind), Mathf.Cos(angle) * reach);
                    look = Vector3.Lerp(look, centre + new Vector3(0, -.9f, 7f), behind);
                    fov = Mathf.Lerp(fov, 62f, behind);
                }
            }
            if (index == 2) { eye.x *= _snSide; look.x *= _snSide; }
            if (index != 2) return;
            // On him as he lets go, on the lantern as it leaves.
            look = Vector3.Lerp(look, SeanParolAt(t), Ease(SnRelease - .1f, SnRelease + .5f, t));
        }

        private void SampleSean(float t)
        {
            const float leave = 1f;
            // Dusk falls as the lantern starts to build, not at the first frame: the plant and
            // the shoulder roll happen on the real court.
            float dusk = Ease(.6f, 1.3f, t) * leave;
            Tint(_duskLow, dusk); Tint(_duskHigh, dusk); Tint(_duskSun, dusk); Tint(_duskMid, dusk); Tint(_duskNight, dusk);
            for (int i = 0; i < _snClouds.Count; i++)
            {
                // Each lies along the sky's wall and drifts round it, slowly, its underside lit.
                var row = SnCloudRows[i]; float round = (row.x + t * (.9f + i * .3f)) * Mathf.Deg2Rad;
                var seat = new Vector3(Mathf.Sin(round) * 14.4f, row.y, Mathf.Cos(round) * 14.4f);
                PlaceGlow(_snClouds[i], seat, new Vector3(row.z, 1.15f, 1f), Quaternion.LookRotation(new Vector3(-seat.x, 0, -seat.z), Vector3.up), dusk * (.18f + .04f * i));
            }
            var horizon = _pieces[_duskGlow].Color * (.85f + .25f * Ease(SnRelease, SnTop, t));
            horizon.a = 1;
            Tint(_duskGlow, dusk, horizon);
            // The sun is down behind him and to one side, half under the horizon; the first stars are out over it.
            PlaceGlow(_duskSunGlow, new Vector3(-5.5f, 1.25f, -13.6f), new Vector3(15f, 6.5f, 1f), Quaternion.identity, dusk * .42f);
            for (int i = 0; i < _snEveningGlows.Count; i++)
            {
                var row = SnEvening[i]; float a = row.x * Mathf.Deg2Rad;
                PlaceGlow(_snEveningGlows[i], new Vector3(Mathf.Sin(a) * 14.2f, row.y, Mathf.Cos(a) * 14.2f), Vector3.one * row.z * 1.7f, Quaternion.identity,
                    dusk * (_reducedEffects ? .9f : .75f + .4f * Mathf.Sin(t * 2.3f + i * 2.1f)));
            }
            // A built thing does not fade in: each quarter of the town stands up out of the ground as the dusk arrives, one
            // after another round him, a little past its height and back.
            for (int i = 0; i < _snTown.Count; i++)
            {
                float up = SnSettle(Mathf.InverseLerp(.88f + i * .07f, 1.4f + i * .07f, t)) * leave;
                var town = _snTown[i]; var turn = Quaternion.Euler(0, 180f + i * 90f, 0);
                bool there = up > .01f;
                if (town.gameObject.activeSelf != there) town.gameObject.SetActive(there);
                town.localPosition = Vector3.zero; town.localRotation = turn; town.localScale = new Vector3(1, Mathf.Max(.01f, up), 1);
                for (int k = 0; k < SnFiesta.Length; k++)
                {
                    // The import mirrors x, so a flag typed at +c degrees hangs at -c here.
                    float c = -SnFiesta[k].x * Mathf.Deg2Rad;
                    var hung = turn * new Vector3(Mathf.Sin(c) * 13.0f, (SnFiesta[k].y - .2f) * up, Mathf.Cos(c) * 13.0f);
                    int step = Mathf.FloorToInt(t * 4f);
                    float bright = _reducedEffects ? .7f : ((k + i + step) % 3 + 3) % 3 == 0 ? 1.1f : .45f;
                    PlaceGlow(_snFiestaGlows[i * SnFiesta.Length + k], hung, Vector3.one * .5f, Quaternion.identity, Mathf.Clamp01(up) * dusk * bright);
                }
            }

            // The parol: one holder, grown. It faces out of his chest, stands up to face the lens as it climbs, and lies over
            // flat above the court at the top.
            float size = SeanParolSize(t), giant = Mathf.InverseLerp(.6f, SnGiant, size), flat = Ease(SnFlatFrom, SnFlatTo, t);
            float turned = Mathf.Max(0, t - SnRelease) * 38f;
            var top = SeanParolAt(t); var let = HeadPoint + new Vector3(0, .7f, .2f);
            float climbing = Ease(SnRelease, SnRelease + .15f, t) * (1 - Ease(SnTop - .2f, SnTop + .5f, t)) * leave;
            // Since it burst: its panels and scallops are thrown, quick and then slowing, and start to fall.
            float since = Mathf.Max(0, t - SnBurst), flung = 1 - Mathf.Exp(-2.6f * since);
            // Its facing follows its path: it leans its face down into the climb (which is also the way it will lie over),
            // and banks into the slow S it climbs on. Before it bursts it draws in on itself for a breath.
            var ahead = SeanParolAt(t + .06f) - top;
            float lean = _reducedEffects ? 0 : 20f * climbing * (1 - flat), bank = _reducedEffects ? 0 : Mathf.Clamp(-ahead.x * 70f, -13f, 13f) * (1 - flat);
            float gather = t < SnBurst ? Ease(SnBurst - .24f, SnBurst - .03f, t) : 0;
            _snParol.localPosition = top;
            _snParol.localRotation = Quaternion.Euler(90f * flat + lean, 0, bank) * Quaternion.Euler(0, 0, turned);
            _snParol.localScale = Vector3.one * size * (1 - .045f * gather + .05f * Flash(t, SnBurst + .05f, .12f));
            for (int i = 0; i < 5; i++)
            {
                // Stick i joins a point to the point two along: the frame of a real parol. Each arrives from outside,
                // turning, and settles into its seat: laid one by one.
                // ⚠️ ON AN ARC, PAST ITS SEAT AND BACK: it swings in round the frame, not down a line, turning as it comes.
                float coming = Mathf.InverseLerp(.85f + i * .16f, 1.03f + i * .16f, t), laid = SnSettle(coming);
                var seat = _snStickHome[i]; var round = new Vector3(-seat.y, seat.x, 0);
                SnPose(_snStick[i], Vector3.LerpUnclamped(seat * 3.2f + Vector3.forward * .5f, seat, laid) + round * 1.4f * Mathf.Sin(coming * Mathf.PI) * (1 - coming),
                    Quaternion.Euler(0, 0, (1 - laid) * (i % 2 == 0 ? 60f : -60f)), Mathf.Clamp01(coming * 5f) * leave);
            }
            // Then the paper: the five points fill in one after another round the frame, each past its size and back, and
            // the heart last. On the burst each point is thrown out along its own line, tumbling, and falls.
            for (int i = 0; i < 5; i++)
            {
                float filled = SnSettle(Mathf.InverseLerp(SnFill + i * .045f, SnFill + .2f + i * .045f, t));
                var seat = _snPointHome[i]; var out_ = seat.sqrMagnitude > 0 ? seat.normalized : Vector3.up;
                SnPose(_snPoint[i], seat * (1 - .07f * gather) + out_ * flung * (.95f + .12f * i) + Vector3.forward * .3f * since * since,
                    Quaternion.AngleAxis(since * (150f + 35f * i), new Vector3(out_.y, -out_.x, .4f)), filled * (1 - .06f * gather) * Mathf.Lerp(1f, .72f, Mathf.Clamp01(since)) * leave);
            }
            float fill = Ease(SnFill, SnFill + .25f, t);
            SnPose(_snHeart, Vector3.forward * .5f * flung, Quaternion.Euler(0, 0, since * 220f),
                SnSettle(Mathf.InverseLerp(SnFill + .22f, SnFill + .42f, t)) * (1 + .12f * Flash(t, SnRelease, .1f)) * (1 - Ease(SnBurst + .1f, SnBurst + .5f, t)) * leave);
            SnPose(_snHoop, Vector3.zero, Quaternion.identity, SnSettle(Mathf.InverseLerp(SnFill + .3f, SnFill + .52f, t)) * (1 + .05f * Flash(t, SnBurst + .04f, .16f)) * leave);
            // Its tails hang straight down whichever way it turns: the two low ones from the moment it is whole, the other
            // three when it lies over the court. They sway, stream as it climbs, swing out as it turns, and are whipped
            // outward by the burst; the cut paper under each bell swings late.
            for (int i = 0; i < 5; i++)
            {
                var tail = _snTail[i];
                if (tail == null) continue;
                float shown = i == 2 || i == 3 ? SnSettle(Mathf.InverseLerp(SnFill + .34f, SnFill + .62f, t))
                    : SnSettle(Mathf.InverseLerp(SnFlatFrom + .25f + i * .07f, SnFlatFrom + .7f + i * .07f, t));
                bool there = shown * leave > .002f;
                if (tail.gameObject.activeSelf != there) tail.gameObject.SetActive(there);
                if (!there) continue;
                var hung = _root.transform.InverseTransformPoint(tail.position);
                var away = new Vector3(hung.x - top.x, 0, hung.z - top.z);
                if (away.sqrMagnitude > .0001f) away.Normalize();
                var sway = new Vector3(Mathf.Sin(t * 2.3f + i) * .10f, 0, Mathf.Cos(t * 1.9f + i * 1.7f) * .07f) * (1 + 2.2f * climbing);
                var down = Vector3.down + (_reducedEffects ? Vector3.zero : sway) + away * (.14f * flat + (t >= SnBurst ? .9f * Mathf.Exp(-1.5f * since) : 0));
                tail.rotation = _root.transform.rotation * Quaternion.FromToRotation(Vector3.down, down.normalized);
                tail.localScale = new Vector3(1, shown * (1 + .14f * climbing), 1);
                if (_snTailEnd[i] != null)
                    _snTailEnd[i].localRotation = _reducedEffects ? Quaternion.identity
                        : Quaternion.Euler(Mathf.Sin(t * 3.1f + i * 1.3f) * 7f, 0, Mathf.Sin(t * 2.6f + i * 2f) * 11f * (1 + climbing));
            }
            PlaceGlow(_parolHalo, Vector3.zero, Vector3.one * 3f, Quaternion.identity, fill * leave * Mathf.Lerp(1f, .4f, giant));

            // Its layers, each on its own turn, and all of them only once it is big enough to have them: a second star behind
            // the first, turning against it, the cords its lights hang on, and a crown of ten scallops that OPENS when it
            // ignites and is thrown off when it bursts.
            float burst = Ease(SnBurst, SnBurst + .3f, t);
            float flare = 1 + .6f * Flash(t, SnIgnite + .06f, .25f) + 1.2f * Flash(t, SnBurst + .03f, .2f);
            SnPose(_snBack, Vector3.zero, Quaternion.Euler(0, 0, -turned * 1.5f), SnSettle(Mathf.InverseLerp(3.3f, 3.9f, t)) * (1 - Ease(SnBurst, SnBurst + .22f, t)) * leave);
            SnPose(_snStrings, Vector3.zero, Quaternion.identity, SnSettle(Mathf.InverseLerp(3.0f, 3.25f, t)) * (t >= SnBurst + .04f ? 0 : 1) * leave);
            SnPose(_snCrown, Vector3.zero, Quaternion.Euler(0, 0, -turned * .6f), Ease(SnIgnite - .2f, SnIgnite - .1f, t) * leave);
            for (int i = 0; i < 10; i++)
            {
                float open = SnSettle(Mathf.InverseLerp(SnIgnite - .1f + i * .022f, SnIgnite + .14f + i * .022f, t));
                var seat = _snPetalHome[i]; var out_ = seat.sqrMagnitude > 0 ? seat.normalized : Vector3.up;
                SnPose(_snPetal[i], seat * (1 - .05f * gather) + out_ * flung * (1.35f + .22f * (i % 3)) + Vector3.forward * .36f * since * since,
                    Quaternion.AngleAxis(since * (190f + 45f * (i % 4)), new Vector3(out_.y, -out_.x, .3f)), open * Mathf.Lerp(1f, .8f, Mathf.Clamp01(since)) * leave);
            }
            PlaceGlow(_parolFace, Vector3.forward * .22f, Vector3.one * 2.5f, Quaternion.identity, fill * leave * Mathf.Min(.42f, (.14f + .1f * giant) * flare) * (1 - .6f * burst));
            // Its heart is the brightest thing on it, and beats; each point of its frame carries a light, which is what is
            // left burning when the paper has gone.
            float beat = _reducedEffects ? .85f : .8f + .2f * Mathf.Sin(t * 6f);
            PlaceGlow(_parolHeartGlow, Vector3.forward * .24f, Vector3.one * Mathf.Lerp(.9f, .46f, giant), Quaternion.identity,
                fill * beat * Mathf.Min(.9f, .42f * flare) * (1 - .75f * Ease(SnBurst + .1f, SnBurst + .5f, t)) * leave);
            for (int i = 0; i < _snTipGlows.Count; i++)
            {
                float a = (90f + i * 72f) * Mathf.Deg2Rad, burning = Ease(SnBurst, SnBurst + .2f, t);
                float flick = _reducedEffects ? 1f : .8f + .2f * Mathf.Sin(t * 11f + i * 2.4f);
                PlaceGlow(_snTipGlows[i], new Vector3(Mathf.Cos(a), Mathf.Sin(a), .06f), Vector3.one * (.3f + .14f * burning), Quaternion.identity,
                    Ease(SnIgnite, SnIgnite + .2f, t) * (.55f + .75f * burning) * flick * flare * leave);
            }

            // Its lights: five rings of bulbs on their cords. First a wave runs out from the middle, ring after ring, twice;
            // then they chase round, every third one bright and the bright one stepping on, each ring the other way from the last.
            int light = 0;
            for (int ring = 0; ring < SnRings.Length; ring++)
            {
                int count = (int)SnRings[ring].y;
                float lit = Ease(3.1f + ring * .16f, 3.3f + ring * .16f, t);
                float wave = Mathf.Max(Flash(t, 3.9f + ring * .09f, .16f), Flash(t, SnIgnite + ring * .07f, .16f));
                for (int i = 0; i < count; i++, light++)
                {
                    float angle = (i + .5f * (ring % 2)) * Mathf.PI * 2 / count;
                    int step = Mathf.FloorToInt(t * 10f) * (ring % 2 == 0 ? 1 : -1);
                    bool on = ((i + step) % 3 + 3) % 3 == 0;
                    float bright = _reducedEffects ? .8f : on ? 1.75f : .45f;
                    // On the burst every light flies out from the middle, the outer rings furthest. Only the light goes: a
                    // bulb thrown clear of the lantern was a dark bead with a glow round it (film p2).
                    var seat = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * SnRings[ring].x * (1 + burst * (1.2f + ring * .35f));
                    SnPose(_snBulbs[light], seat + Vector3.forward * .13f, Quaternion.identity, SnSettle(lit) * (on && !_reducedEffects ? 1.25f : 1f) * (1 + .5f * wave) * (1 - burst) * leave);
                    // A lit bulb is lit: the glass takes its own light, not the dusk's shade.
                    if (light < _snBulbSkins.Count && _snBulbSkins[light] != null)
                    {
                        _snBulbBlock.SetColor(SnFlashColourId, Color.Lerp(SnLightColour[(int)SnRings[ring].z], new Color(1, .98f, .86f, 1), .6f));
                        _snBulbBlock.SetFloat(SnFlashAmountId, lit * (on || _reducedEffects ? .9f : .3f));
                        _snBulbSkins[light].SetPropertyBlock(_snBulbBlock);
                    }
                    PlaceGlow(_snLights[light], seat + Vector3.forward * .15f, Vector3.one * (Mathf.Lerp(.11f, .085f, giant) + .05f * wave),
                        Quaternion.identity, lit * (bright + 1.6f * wave) * flare * leave);
                }
            }

            // Its rays, when it ignites and again as it bursts: twelve tapered spikes, long and short by turns, out past its
            // crown. They shoot past their length and come back to it.
            float struck = Mathf.InverseLerp(SnIgnite, SnIgnite + .3f, t);
            float rayed = Mathf.Max(SnSettle(struck) * (1 - Ease(SnIgnite + .5f, SnIgnite + 1.3f, t) * .65f), Ease(SnBurst - .04f, SnBurst + .08f, t) * 1.5f);
            for (int i = 0; i < _snRays.Count; i++)
            {
                float angle = (i * 30f + turned * .4f) * Mathf.Deg2Rad, length = (i % 2 == 0 ? .78f : .42f) * rayed, wide = (i % 2 == 0 ? .06f : .04f) * Mathf.Sqrt(Mathf.Max(0, rayed));
                var out_ = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                Place(_snRays[i], out_ * 1.42f, new Vector3(wide, Mathf.Max(.001f, length), wide), Quaternion.FromToRotation(Vector3.up, out_), Mathf.Clamp01(rayed) * leave);
            }

            // The climb: a ribbon of fire from where he let it go, a hot core down its middle, tongues licking back off it,
            // and embers shed behind it that tumble as they fall.
            for (int i = 0; i < _snTrail.positionCount; i++)
            {
                float u = i / (float)(_snTrail.positionCount - 1);
                var on = Vector3.Lerp(Vector3.Lerp(let, top, .35f), top, u);
                _snTrail.SetPosition(i, on + new Vector3(Mathf.Sin(u * 9f + t * 14f) * .12f * (1 - u), 0, 0));
                _snTrailOuter.SetPosition(i, on + new Vector3(Mathf.Sin(u * 6f - t * 11f) * .26f * (1 - u) * (1 + giant), 0, Mathf.Cos(u * 5f + t * 9f) * .1f * (1 - u)));
            }
            _snTrail.enabled = _snTrailOuter.enabled = climbing > .02f;
            _snTrail.widthMultiplier = .2f * climbing * Mathf.Lerp(1f, 2.6f, giant);
            _snTrailOuter.widthMultiplier = .62f * climbing * Mathf.Lerp(1f, 3.4f, giant);
            if (_snKit)
            {
                // ⚠️ THE CLIMB'S FIRE IS A TORN RIBBON NOW, not a line that fades: two crossed ribbons of ember with gold down
                // their middle, hung from the lantern back to where he let it go, the mask running back along them. As the
                // lantern reaches its height they are eaten away into streaks.
                _snTrail.enabled = _snTrailOuter.enabled = false;
                var back = Vector3.Lerp(let, top, .35f) - top; float longness = Mathf.Max(.05f, back.magnitude);
                var aim = Quaternion.FromToRotation(Vector3.down, back / longness);
                float broad = Mathf.Lerp(.6f, 4.4f, giant), eaten = Mathf.Lerp(.3f, 1f, Ease(SnTop - .4f, SnTop + .45f, t));
                for (int i = 0; i < _snClimbRibbons.Count; i++)
                {
                    var piece = _pieces[_snClimbRibbons[i]].Transform; bool core = i >= 2;
                    piece.localPosition = top; piece.localRotation = aim * Quaternion.Euler(0, (i % 2) * 90f + 25f, 0);
                    piece.localScale = new Vector3(broad * (core ? .4f : 1f), longness, 1f);
                    SnTear(_snClimbRibbons[i], t - SnRelease, eaten, Ease(SnRelease, SnRelease + .15f, t) * leave);
                }
            }
            for (int i = 0; i < _snTrailGlows.Count; i++)
            {
                // Light carried up the fire: brightest under the lantern, dimmer down the trail.
                float u = 1f - i * .22f;
                PlaceGlow(_snTrailGlows[i], Vector3.Lerp(Vector3.Lerp(let, top, .35f), top, u), Vector3.one * Mathf.Lerp(.9f, 5.5f, giant) * (1 - i * .22f), Quaternion.identity,
                    climbing * (.9f - i * .25f) * (_reducedEffects ? 1f : .85f + .15f * Mathf.Sin(t * 17f + i)));
            }
            for (int i = 0; i < _snTrailFlames.Count; i++)
            {
                float u = .97f - i * .15f, lick = _reducedEffects ? 1f : 1 + .28f * Mathf.Sin(t * 19f + i * 2.3f);
                var on = Vector3.Lerp(Vector3.Lerp(let, top, .35f), top, u) + new Vector3(Mathf.Sin(u * 6f - t * 11f) * .2f * (1 - u), 0, 0);
                float body = Mathf.Lerp(.42f, 2.8f, giant) * (1 - i * .13f);
                // Each hangs point down off the ribbon and leans out of it, turned a fifth of the way round from the last.
                Place(_snTrailFlames[i], on, new Vector3(body, body * 1.9f * lick, body), Quaternion.Euler(180f + (i % 2 == 0 ? 16f : -16f), i * 72f + t * 60f, 0), climbing * (.85f - i * .1f));
            }
            for (int i = 0; i < _snShed.Count; i++)
            {
                float born = SnRelease + .12f + i * .2f, age = t - born;
                var from = SeanParolAt(born) + new Vector3(Mathf.Sin(i * 2.4f) * .3f, 0, Mathf.Cos(i * 1.7f) * .2f);
                var at = from + Vector3.down * age * age * 2.2f + new Vector3(Mathf.Sin(i * 3.1f) * .5f * age, 0, 0);
                float alive = age >= 0 && age < 1.1f ? (1 - age / 1.1f) * leave : 0;
                Place(_snShedPieces[i], at, Vector3.one * (.07f + .09f * Mathf.InverseLerp(SnRelease, SnTop, born)) * Mathf.Clamp01(1.3f - age),
                    Quaternion.Euler(age * 260f + i * 40f, age * 190f, i * 53f), alive > 0 ? 1 : 0);
                PlaceGlow(_snShed[i], at, Vector3.one * .2f * Mathf.Clamp01(1.2f - age), Quaternion.identity, alive * .7f);
            }

            // The ignition, as it lies over: a ring thrown out across the sky from it with a softer one behind, a sheet of
            // light under it, and a rain of little lit stars and paper diamonds. All of it is thrown again, further, when
            // it bursts round him.
            float ignited = t >= SnBurst ? t - SnBurst : t - SnIgnite, thrown = Mathf.Clamp01(ignited / (t >= SnBurst ? .4f : .55f));
            float late = Mathf.Clamp01((ignited - .07f) / (t >= SnBurst ? .5f : .65f));
            Place(_snShock, top, Vector3.one * Mathf.Lerp(SnGiant * .9f, SnGiant * 2.6f, 1 - (1 - thrown) * (1 - thrown)), Quaternion.identity,
                ignited >= 0 ? (1 - thrown) * leave : 0);
            Place(_snShockSoft, top + Vector3.down * .2f, Vector3.one * Mathf.Lerp(SnGiant * .8f, SnGiant * 2.3f, 1 - (1 - late) * (1 - late)), Quaternion.identity,
                ignited >= .07f ? (1 - late) * leave : 0);
            PlaceGlow(_snShockGlow, top + Vector3.down * .4f, Vector3.one * Mathf.Lerp(SnGiant * 1.6f, SnGiant * 3.6f, thrown), Quaternion.Euler(90, 0, 0),
                ignited >= 0 ? (1 - thrown) * .5f * leave : 0);
            PlaceGlow(_snFlash, top, Vector3.one * SnGiant * 2.2f, Quaternion.identity, (.22f * Flash(t, SnIgnite + .05f, .2f) + .55f * Flash(t, SnBurst + .04f, .22f)) * leave);
            for (int i = 0; i < _snRainGlows.Count; i++)
            {
                var row = SnRain[i]; float age = ignited - (i % 4) * .05f, a = row.x * Mathf.Deg2Rad;
                var from = top + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * row.y * SnGiant;
                var at = from + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * age * 1.4f + Vector3.down * (row.z * age + 2.4f * age * age);
                float alive = age >= 0 && age < 1.4f ? (1 - age / 1.4f) * leave : 0;
                Place(_snRainPieces[i], at, Vector3.one * (.3f + .1f * (i % 3)) * Mathf.Clamp01(.4f + alive * 1.5f), Quaternion.Euler(age * 230f + i * 40f, age * 170f + i * 25f, i * 33f), alive > 0 ? 1 : 0);
                PlaceGlow(_snRainGlows[i], at, Vector3.one * (.5f + .12f * (i % 3)), Quaternion.identity, alive * .6f);
            }
            // Shards thrown from its rim when it ignites, and again when it bursts: twelve spikes of light, each out and a
            // little up and then over and down, pointing along its own fall.
            for (int i = 0; i < _snShards.Count; i++)
            {
                float age = ignited - (i % 3) * .04f, a = (i * 30f + 11f) * Mathf.Deg2Rad;
                var out_ = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                float fast = 6.5f + 2.5f * (i % 3), lift = 2.2f + 1.4f * (i % 2);
                var velocity = out_ * fast * Mathf.Exp(-1.1f * Mathf.Max(0, age)) + Vector3.up * (lift - 7f * age);
                var at = top + out_ * (SnGiant * 1.05f + fast * (1 - Mathf.Exp(-1.1f * Mathf.Max(0, age))) / 1.1f) + Vector3.up * (lift * age - 3.5f * age * age);
                bool alive = age >= 0 && age < 1.2f;
                Place(_snShards[i], at, new Vector3(.16f, 1.5f * Mathf.Clamp01(1.25f - age), .16f), Quaternion.FromToRotation(Vector3.up, velocity.normalized), alive ? Mathf.Clamp01(1.6f - age * 1.4f) * leave : 0);
            }
            // The burst's paper: capiz flakes let go across the whole lantern, fluttering down each on its own drift, and
            // catching the light as they turn. Still falling on the last frame.
            for (int i = 0; i < _snFlakes.Count; i++)
            {
                float age = t - SnBurst - .05f - i * .025f, a = i * 137.5f * Mathf.Deg2Rad, far = Mathf.Sqrt((i + .5f) / _snFlakes.Count) * SnGiant * .92f;
                var from = new Vector3(0, SnHigh, .5f) + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * far;
                var at = from + new Vector3(Mathf.Sin(age * 2.6f + i) * .7f + Mathf.Cos(a) * age * 1.2f, -(1.6f + .5f * (i % 3)) * age - .7f * age * age, Mathf.Cos(age * 2.1f + i * 2f) * .6f + Mathf.Sin(a) * age * 1.2f);
                float spin = age * (150f + 40f * (i % 4)) + i * 50f, glint = Mathf.Pow(Mathf.Abs(Mathf.Sin(spin * Mathf.Deg2Rad)), 6f);
                bool alive = age >= 0;
                Place(_snFlakes[i], at, Vector3.one * (.34f + .1f * (i % 3)) * SnSettle(Mathf.Clamp01(age * 4f)), Quaternion.Euler(spin, i * 40f + age * 60f, spin * .6f), alive ? leave : 0);
                PlaceGlow(_snFlakeGlints[i], at, Vector3.one * (.5f + 1.1f * glint), Quaternion.identity, alive ? (.25f + 1.3f * (_reducedEffects ? .3f : glint)) * leave : 0);
            }

            // The burst's wisps: six ribbons of fire curling down and out from the lantern's rim, each its own shape and
            // length, out past their size and back, torn slowly: still tearing on the last frame. And the wreath: the
            // hoop and the five sticks wearing bands of slow tearing light, so the frame the paper leaves is a burning
            // one. It comes on as the paper goes and breathes there.
            for (int i = 0; i < _snBurstWisps.Length; i++)
            {
                var row = SnBurstThrown[i]; float age = t - SnBurst - i * .03f, u = Mathf.Clamp01(age / row.z);
                float grown = row.y * .62f * SnSettle(Mathf.Clamp01(age / .3f)), round = (row.x + turned) * Mathf.Deg2Rad;
                var from = new Vector3(0, SnHigh, .5f) + new Vector3(Mathf.Sin(round), 0, Mathf.Cos(round)) * SnGiant * .5f;
                UltimateFx.Place(_snBurstWisps[i], from, Quaternion.Euler(0, row.x + turned, 0) * Quaternion.Euler(0, 0, 180f), new Vector3(grown * .7f, grown * (.55f + .2f * u), grown));
                UltimateFx.Set(_snBurstWisps[i], age, Mathf.Lerp(.38f, .85f, u * u * (3 - 2 * u)), leave);
            }
            float wreathed = Ease(SnBurst + .05f, SnBurst + .45f, t), flicker = _reducedEffects ? .5f : .5f + .5f * Mathf.Sin(t * 2.4f);
            SnTear(_snWreathHoop, t, Mathf.Lerp(1f, .36f + .1f * flicker, wreathed), leave);
            SnTear(_snWreathHoopIn, t, Mathf.Lerp(1f, .5f + .12f * (1 - flicker), wreathed), .8f * leave);
            for (int i = 0; i < _snWreathSticks.Count; i++)
                SnTear(_snWreathSticks[i], t + i * .7f, Mathf.Lerp(1f, .34f + .14f * Mathf.Abs(Mathf.Sin(t * 1.7f + i * 1.3f)), wreathed), leave);

            // ⚠️ THE LANTERN IS A LIGHT 16 m UP, AND THE COURT SHOWS IT. Under the whole court, faint, its light lies on the
            // ground with the star's shape in it, turning as the lantern turns, its rings of bulbs stepping with the chase;
            // it comes up as the lantern grows, jumps when it ignites and when it bursts, and is dimmer after.
            float turnedBy = turned * Mathf.Deg2Rad, struckLight = Flash(t, SnIgnite + .06f, .25f) + 1.6f * Flash(t, SnBurst + .04f, .22f);
            SnLight(_snCast, Vector3.up * .03f, SnGiant * .98f, Ease(3.5f, SnIgnite + .2f, t) * leave, t, 0, struckLight, turnedBy,
                .24f * (1 - .45f * Ease(SnBurst + .2f, SnBurst + .9f, t)));
            // Its light standing on the court in shafts, from the lantern down, once it is over it: soft sheets, and where
            // each lands a pool of the same thrown light, small, breathing.
            float standing = Ease(SnIgnite + .05f, SnIgnite + .5f, t) * leave;
            for (int i = 0; i < SnShafts.Length; i++)
            {
                var row = SnShafts[i];
                float breath = _reducedEffects ? .85f : .75f + .25f * Mathf.Sin(t * 3f + i * 1.9f);
                for (int sheet = 0; sheet < 3; sheet++)
                    PlaceGlow(_snShaftPieces[i * 3 + sheet], new Vector3(row.x, top.y * .5f, row.y), new Vector3(top.y, row.z * 2.3f, 1f),
                        Quaternion.Euler(0, i * 37f + sheet * 60f, 90f), standing * breath * .2f);
                SnLight(_snShaftPools[i], new Vector3(row.x, .045f, row.y), row.z * 2.1f * (.94f + .08f * breath), standing, t + i * 1.7f, 0, struckLight, turnedBy + i, .5f * breath);
            }

            // Where he will land, and who is standing in it: one mark of light (`Shaders/ParolGround`). It arrives past its
            // size and back, the parol turning into place inside it; its ticks step; the star's five points ignite one after
            // another, the fifth as he leaves the ground; and it burns hotter all through the coil. It stays lit after
            // he has gone up: that is where he comes down.
            float ringOn = Ease(SnRing, SnRing + .3f, t) * leave;
            float drawn = SnSettle(Mathf.InverseLerp(SnRing, SnRing + .42f, t)) * (1 + .05f * Flash(t, SnRise + .05f, .18f));
            float markTurn = (1 - Ease(SnRing, SnRing + .6f, t)) * 1.2f;
            float points = Mathf.InverseLerp(SnRing + .35f, SnRise, t) * 5f;
            float heat = Mathf.Max(Ease(SnCoil, SnRise, t) * (1 - .35f * Ease(SnRise + .1f, SnRise + .8f, t)), Flash(t, SnBurst + .05f, .25f));
            float markOut = SnLanding / .93f * Mathf.Max(.01f, drawn);
            SnLight(_snMark, Vector3.up * .05f, markOut, ringOn, t, points, heat, markTurn, .85f);
            for (int i = 0; i < _snLandTips.Count; i++)
            {
                // A flare of real light standing over each point as it ignites, where the shader draws it (.72 of the way out).
                float a = Mathf.PI * .5f + markTurn + i * Mathf.PI * .4f, since_ = points - i;
                float kick = since_ > 0 ? Mathf.Exp(-3.2f * Mathf.Max(0, since_ - .25f)) : 0;
                PlaceGlow(_snLandTips[i], new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * markOut * .72f + Vector3.up * (_snFloor + .14f), Vector3.one * (.7f + 1.3f * kick),
                    Quaternion.identity, ringOn * Mathf.Clamp01(since_ * 6f) * (.7f + 1.2f * kick));
            }
            for (int i = 0; i < _snMarks.Count && i < _others.Count; i++)
            {
                bool inside = _others[i].Reach <= SnLanding;
                SnLight(_snMarks[i], _others[i].Home + Vector3.up * .06f, .95f * SnSettle(Mathf.InverseLerp(SnRing + .1f + i * .08f, SnRing + .5f + i * .08f, t)), inside ? ringOn : 0, t + i * .9f);
            }
            // ⚠️ THE DOME. The ring does not stay a mark on the floor: a dome of capiz panes rises off it, rim first, the
            // lattice of the lantern's own windows bent over the place he will land. It breathes, jumps a little as each
            // point of the star ignites, and its panes wink as the mask runs over them. Twelve sparks ride round it on
            // two tilted rings, quicker as he coils. At the leap it is taken upward, rim first, and is gone in a third of
            // a second, and the sparks close in and follow him up. Half there at most: he and whoever stands in it are
            // seen through it.
            float domeUp = Ease(SnRing + .2f, SnRing + .8f, t), domeGone = Mathf.Clamp01((t - SnRise) / .36f);
            float ignition = points > 0 && points < 5 ? Mathf.Exp(-5f * (points - Mathf.Floor(points))) : 0;
            float breathing = _reducedEffects ? .5f : .5f + .5f * Mathf.Sin(t * 5f);
            float domeEaten = t < SnRise ? Mathf.Lerp(1f, .24f + .08f * breathing - .1f * heat - .06f * ignition, domeUp) : Mathf.Lerp(.22f, 1f, domeGone * domeGone);
            float domeOut = SnLanding * .985f * Mathf.Max(.01f, drawn) * (1 - .12f * domeGone), domeHigh = (3.1f + .2f * ignition + .12f * breathing) * (1 + 1.3f * domeGone);
            var domeAt = Vector3.up * (_snFloor + .06f + 2.4f * domeGone * domeGone);
            // Its three layers on one place: the structure strongest, the panes' rims lit by the wave that runs round the
            // rings (so most are dark at any moment), the panes' bodies a faint sheen. None of it as bright as him.
            for (int layer = 0; layer < 3; layer++)
            {
                int part = layer == 0 ? _snDome : layer == 1 ? _snDomeRims : _snDomeSheen;
                var piece = _pieces[part];
                piece.Transform.localPosition = domeAt; piece.Transform.localRotation = Quaternion.identity; piece.Transform.localScale = new Vector3(domeOut, domeHigh, domeOut);
                piece.Block.SetFloat(SnSweepId, t < SnRise ? -1.4f : 1.5f);
                SnTear(part, t - SnRing, Mathf.Clamp01(domeEaten + (layer == 0 ? -.1f : layer == 1 ? .2f : .1f)), (layer == 0 ? .5f : layer == 1 ? .5f : .13f) * ringOn);
            }
            // A hoop of light climbs it: up with the dome as it rises, then again and again while he coils, quicker toward
            // the leap. From straight above it is a ring that closes, which is what says this thing has height. A soft
            // light rides inside it, and the cast on the court under the dome grows with it.
            float climbs = domeUp < 1 ? domeUp * .8f : Mathf.Repeat((t - SnRing - .8f) * (.75f + .5f * heat), 1f);
            float hoopLift = climbs * 80f * Mathf.Deg2Rad, hoopSeen = (domeUp < 1 ? 1f : Mathf.Sin(climbs * Mathf.PI)) * domeUp * (1 - domeGone) * ringOn;
            var hoop = _pieces[_snDomeHoop].Transform;
            hoop.localPosition = domeAt + Vector3.up * domeHigh * Mathf.Sin(hoopLift); hoop.localRotation = Quaternion.Euler(90, 0, 0);
            hoop.localScale = Vector3.one * domeOut * Mathf.Cos(hoopLift);
            SnTear(_snDomeHoop, t, .18f, hoopSeen);
            PlaceGlow(_snDomeGlow, hoop.localPosition, Vector3.one * domeOut * Mathf.Cos(hoopLift) * 2.1f, Quaternion.Euler(90, 0, 0), hoopSeen * .22f);
            SnLight(_snDomePool, Vector3.up * .04f, SnLanding * (.45f + .5f * domeUp), domeUp * (1 - domeGone) * ringOn, t, 0, heat * .5f, markTurn, .42f);
            float follow = Ease(SnRise, SnRise + .55f, t), lifting = LiftAt(t), sparksOut = 1 - Ease(SnBurst - .25f, SnBurst + .1f, t);
            for (int i = 0; i < _snOrbit.Count; i++)
            {
                bool second = i % 2 == 1;
                float a = i * Mathf.PI / 6f + (t - SnRing) * (1.5f + 1.4f * heat) * (second ? -1 : 1) + follow * 5f * (second ? -1 : 1);
                // Inside the dome, not on its rim: on the rim one of them came past the low lens and its tail filled a corner of the
                // frame with white (films p9, p10).
                float far = Mathf.Lerp(SnLanding * .86f, .95f + .2f * (i % 3), follow * follow);
                var ride = Quaternion.Euler(second ? -11f : 11f, 0, second ? 7f : -7f) * new Vector3(Mathf.Cos(a) * far, 0, Mathf.Sin(a) * far);
                var at = ride + Vector3.up * (_snFloor + Mathf.Lerp(1.5f, lifting + .4f + .5f * Mathf.Sin(a * 2f), follow));
                float shown = domeUp * ringOn * sparksOut;
                Place(_snOrbit[i], at, Vector3.one * (.13f + .04f * (i % 3)) * shown, Quaternion.Euler(t * 300f + i * 40f, a * Mathf.Rad2Deg, t * 220f), shown > .02f ? 1 : 0);
                PlaceGlow(_snOrbitGlows[i], at, Vector3.one * (.55f + .2f * follow), Quaternion.identity, shown * (.8f + .5f * heat));
                // Its tail lies back along the ring it rides, flat to the sky and to the side at once so both shots see it.
                var onward = Quaternion.Euler(second ? -11f : 11f, 0, second ? 7f : -7f) * new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a)) * (second ? -1 : 1) + Vector3.up * follow * .8f;
                var behindIt = -onward.normalized; var flatTo = Vector3.ProjectOnPlane(Vector3.up + new Vector3(ride.x, 0, ride.z).normalized, behindIt);
                var tail = _pieces[_snOrbitTails[i]].Transform;
                tail.localPosition = at; tail.localRotation = Quaternion.LookRotation(flatTo.sqrMagnitude > .001f ? flatTo.normalized : Vector3.forward, -behindIt);
                tail.localScale = new Vector3(.13f, (1.3f + .9f * heat) * (1 - .55f * follow), 1f);
                SnTear(_snOrbitTails[i], t + i * .3f, .34f, shown * .8f);
            }

            // Embers rise off the mark and curl: each born somewhere on it, climbing a turn of a slow spiral as it goes,
            // swelling and dying, more of them and faster as the leap nears.
            for (int i = 0; i < _snMarkEmbers.Count; i++)
            {
                float life = 1.5f + .3f * (i % 3), phase = Mathf.Repeat((t - SnRing) / life + i * .381f, 1f);
                float a = i * 137.5f * Mathf.Deg2Rad + phase * 2.4f * (i % 2 == 0 ? 1 : -1), far = SnLanding * (.25f + .65f * Mathf.Repeat(i * .618f, 1f)) * (1 - .25f * phase);
                var at = new Vector3(Mathf.Cos(a) * far, _snFloor + .1f + phase * phase * (1.8f + .6f * (i % 3)) * (.6f + .6f * heat), Mathf.Sin(a) * far);
                float swell = Mathf.Sin(phase * Mathf.PI), shown = ringOn * (i < 5 ? 1 : Mathf.Clamp01(heat * 12f - (i - 5)));
                Place(_snMarkEmbers[i], at, Vector3.one * .1f * swell, Quaternion.Euler(phase * 300f + i * 40f, phase * 220f, i * 53f), shown * swell > .02f ? 1 : 0);
                PlaceGlow(_snMarkEmberGlows[i], at, Vector3.one * .3f * swell, Quaternion.identity, shown * swell * .8f);
            }

            // Paper lanterns drifting up behind him: his home, arriving with the dusk. Small parols now, each turning a little
            // on its string of air with its tails swinging under it.
            for (int i = 0; i < _lanternGlows.Count; i++)
            {
                float born = .8f + i * .18f, age = Mathf.Max(0, t - born);
                var seat = LanternSeats[i];
                var at = seat + new Vector3(Mathf.Sin(age * .8f + i) * .15f, age * (.55f + (i % 3) * .12f), 0);
                var toward = new Vector3(-at.x, 0, 3 - at.z).normalized;
                float seen = Ease(born, born + .4f, t) * dusk;
                var small = _snSmall[i];
                if (small != null)
                {
                    bool there = seen > .01f;
                    if (small.gameObject.activeSelf != there) small.gameObject.SetActive(there);
                    small.localPosition = at;
                    small.localRotation = Quaternion.LookRotation(toward, Vector3.up) * Quaternion.Euler(0, Mathf.Sin(age * .9f + i * 1.4f) * 28f, Mathf.Sin(age * 1.3f + i) * 7f);
                    small.localScale = Vector3.one * (.22f + (i % 2) * .06f) * Mathf.Max(.01f, SnSettle(Mathf.InverseLerp(born, born + .45f, t)));
                    if (_snSmallTails[i] != null) _snSmallTails[i].localRotation = Quaternion.Euler(Mathf.Sin(age * 2.1f + i) * 9f, 0, Mathf.Sin(age * 1.7f + i * 2f) * 12f);
                }
                PlaceGlow(_lanternGlows[i], at - toward * .05f, Vector3.one * (.78f + (i % 2) * .12f), Quaternion.identity, seen * (_reducedEffects ? .7f : .75f + .25f * Mathf.Sin(t * 4 + i)));
            }

            // The coil: fire on the ground winding in toward his feet (`Shaders/ParolGround`), flames licking up round them
            // while he is low, each with a hotter one inside it, embers rising in a tightening spiral, and all of it
            // gathering in on the rise.
            float coil = Ease(SnCoil, SnCoil + .3f, t) * (1 - Ease(SnRise, SnRise + .14f, t));
            SnLight(_snCoilGlow, Vector3.up * .07f, Mathf.Lerp(2.6f, 1.7f, Ease(SnRise - .4f, SnRise, t)), coil * leave, t, 0, 0, 0, .5f);
            for (int i = 0; i < _footFlames.Count; i++)
            {
                float a = i * Mathf.PI / 3 + t * 1.2f;
                float radius = Mathf.Lerp(.95f, .55f, Ease(SnRise - .4f, SnRise, t));
                float lick = _reducedEffects ? 1f : 1 + .22f * Mathf.Sin(t * 17f + i * 2.1f);
                var foot = new Vector3(Mathf.Cos(a) * radius, _snFloor + .04f, Mathf.Sin(a) * radius);
                // Knee high and then some: at half this size they were lost in their own light on the ground (film p2).
                Place(_footFlames[i], foot, new Vector3(1.25f, Mathf.Lerp(.7f, 1.75f, coil) * lick, 1.25f), Quaternion.Euler(-14, -a * Mathf.Rad2Deg, 0), coil * leave);
                Place(_snCoilCores[i], new Vector3(foot.x * .93f, foot.y, foot.z * .93f), new Vector3(.85f, Mathf.Lerp(.4f, 1.1f, coil) * (2 - lick), .85f), Quaternion.Euler(-10, -a * Mathf.Rad2Deg + 40f, 0), coil * leave);
            }
            for (int i = 0; i < _snCoilEmbers.Count; i++)
            {
                float life = .8f + .12f * (i % 3), phase = Mathf.Repeat((t - SnCoil) / life + i * .273f, 1f);
                float a = i * 137.5f * Mathf.Deg2Rad - phase * 4.2f, far = Mathf.Lerp(1.15f, .2f, phase * phase);
                var at = new Vector3(Mathf.Cos(a) * far, _snFloor + .1f + phase * 2.3f, Mathf.Sin(a) * far);
                Place(_snCoilEmbers[i], at, Vector3.one * .09f * Mathf.Sin(phase * Mathf.PI), Quaternion.Euler(phase * 400f, i * 60f, phase * 280f), coil > .05f ? 1 : 0);
            }

            // The leap: a ring of fire thrown out along the court from where he stood, spikes of it kicked up and out that
            // arc and fall, three jets under his feet that shorten as he slows with their light under him, and little
            // stars racing up ahead of him toward the lantern.
            float leapt = t - SnRise, ringOut = Mathf.Clamp01(leapt / .6f);
            SnLight(_snLaunch, Vector3.up * .08f, 5.2f, leapt >= 0 && leapt < .6f ? leave : 0, t, ringOut, 0, 0, 1.2f);
            for (int i = 0; i < _snLeapSparks.Count; i++)
            {
                float age = leapt - (i % 2) * .03f, a = (i * 36f + 14f) * Mathf.Deg2Rad;
                var out_ = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                float fast = 3.4f + 1.3f * (i % 3), lift = 4.4f + 1.5f * (i % 2);
                var velocity = out_ * fast + Vector3.up * (lift - 12f * age);
                var at = out_ * (.5f + fast * age) + Vector3.up * (_snFloor + .1f + lift * age - 6f * age * age);
                bool alive = age >= 0 && at.y > _snFloor;
                Place(_snLeapSparks[i], at, new Vector3(.07f, .55f * Mathf.Clamp01(1.2f - age), .07f), Quaternion.FromToRotation(Vector3.up, velocity.normalized), alive ? leave : 0);
            }
            float lifted = LiftAt(t), jet = leapt >= 0 ? Mathf.Clamp01(1 - leapt / 1.15f) : 0;
            for (int i = 0; i < _snJets.Count; i++)
            {
                float lick = _reducedEffects ? 1f : 1 + .2f * Mathf.Sin(t * 23f + i * 2.4f);
                Place(_snJets[i], new Vector3((i - 1) * .2f, lifted + .12f, (i == 1 ? .06f : -.06f)), new Vector3(1.15f, (i == 1 ? 1.7f : 1.2f) * jet * lick, 1.15f),
                    Quaternion.Euler(180f, i * 120f + t * 200f, 0), jet * leave);
            }
            PlaceGlow(_snJetGlow, new Vector3(0, lifted - .25f, 0), Vector3.one * (1.2f + 1.6f * jet), Quaternion.identity, jet * 1.1f * leave);
            // And what the kit adds to the leap: eight wisps of fire gold thrown outward along the court behind him and to his
            // sides, each its own shape, all torn into streaks within the second; and under him a ribbon of fire from his feet back down
            // toward the court, which is eaten into streaks as he slows at the top.
            for (int i = 0; i < _snLeapWisps.Length; i++)
            {
                var row = SnLeapThrown[i]; float age = leapt - i * .025f, u = Mathf.Clamp01(age / row.z);
                float grown = row.y * SnSettle(Mathf.Clamp01(age / .2f));
                // Low and long along the court and then up: thrown outward, not standing over him.
                UltimateFx.Place(_snLeapWisps[i], Vector3.up * (_snFloor + .1f), Quaternion.Euler(0, row.x, i % 2 == 0 ? 10f : -8f), new Vector3(grown * .65f, grown * (.62f - .12f * u), grown * (.95f + .3f * u)));
                UltimateFx.Set(_snLeapWisps[i], age, Mathf.Lerp(.28f, 1f, u * u * (3 - 2 * u)), leave);
            }
            float trailLong = Mathf.Min(lifted + .1f, 4.4f), trailEaten = Mathf.Lerp(.34f, 1f, Ease(SnRise + .6f, SnRise + 1.5f, t));
            for (int i = 0; i < _snLeapRibbons.Count; i++)
            {
                var piece = _pieces[_snLeapRibbons[i]].Transform; bool core = i >= 2;
                piece.localPosition = new Vector3(0, lifted + .15f, 0); piece.localRotation = Quaternion.Euler(0, (i % 2) * 90f + 40f, 0);
                piece.localScale = new Vector3(core ? .4f : .95f, Mathf.Max(.05f, trailLong), 1f);
                SnTear(_snLeapRibbons[i], leapt, trailEaten, leapt >= 0 ? leave : 0);
            }
            var chest = BothPalms;
            for (int i = 0; i < _sparks.Count; i++)
            {
                float age = t - (SnRise + .02f + i * .015f);
                bool alive = age >= 0;
                float u = Mathf.Clamp01(age / .36f);
                float angle = i * Mathf.PI * 2 / _sparks.Count;
                // Each goes up on a turn of a spiral, quick and then slowing: none of them on a line.
                float risen = 1 - (1 - u) * (1 - u); angle += risen * 2.2f;
                var at = chest + new Vector3(Mathf.Cos(angle) * .38f * Mathf.Sin(u * Mathf.PI * .8f), risen * 1.9f, Mathf.Sin(angle) * .28f * Mathf.Sin(u * Mathf.PI * .8f));
                Place(_sparks[i], at, Vector3.one * .08f * (1 - u * .5f), Quaternion.Euler(age * 300f, angle * Mathf.Rad2Deg, age * 240f), alive ? (1 - u * u) * leave : 0);
            }
        }

        /// <summary>
        /// The world steps back while his fire is on screen (`docs/HERO_KIT_METHOD.md` § 6, "the backdrop steps back"): dimmer
        /// and a little greyer from the dusk on, darkest as the lantern ignites, and back to itself for the rise.
        /// </summary>
        private void SeanGrade(float t, out float brightness, out float saturation)
        {
            float on = Ease(.8f, 1.6f, t) * (1 - Ease(SnBurst - .1f, SnBurst + .2f, t));
            brightness = Mathf.Lerp(1f, .68f, on) * (1 + .12f * Flash(t, SnIgnite + .05f, .2f));
            saturation = Mathf.Lerp(1f, .8f, on);
        }

        /// <summary>
        /// ⚠️ HIS OWN SCREEN COMES HOME TO HIS OWN EYES. From `SnHome` the lens leaves the last shot and travels to where his
        /// first-person camera will be the moment play resumes: the live camera's own place and aim (it has not moved, the
        /// world is paused), raised by exactly what the ability will raise him (`SeanHeroKit.SupernovaApexRise`). It arrives
        /// before the end, so the hand-back is a picture that does not change. His own copy is not drawn once the lens is
        /// nearly in it. World positions.
        /// </summary>
        private void SeanHandBack(float t, Camera live, ref Vector3 eye, ref Vector3 target, ref float fov)
        {
            float home = Ease(SnHome, Seconds - .06f, t);
            if (home <= 0) return;
            float rise = _source != null ? Abilities.SeanHeroKit.SupernovaApexRise(_source.transform.position) : Abilities.SeanHeroKit.SupernovaApex;
            var eyes = live.transform.position + Vector3.up * rise;
            eye = Vector3.Lerp(eye, eyes, home);
            target = Vector3.Lerp(target, eyes + live.transform.forward * 8f, home);
            fov = Mathf.Lerp(fov, live.fieldOfView, home);
            if (home > .7f) foreach (var surface in _bodyRenderers) if (surface != null) surface.enabled = false;
        }
    }
}
