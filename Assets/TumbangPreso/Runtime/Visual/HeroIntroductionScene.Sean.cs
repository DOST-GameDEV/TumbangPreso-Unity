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
        // ⚠️ BLOCKING STILL: every shape is a stock one. ⚠️ EVERY TIME IS ON THE TABLE'S CLOCK (`tools/author_ultimate_intros.py`,
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

        private int _duskLow, _duskGlow, _duskHigh, _parolFill, _parolHalo, _parolRim, _snLanding, _snLandingFill, _snStarTen, _snStarEight, _snTeeth, _snShock;
        private readonly List<int> _snRays = new List<int>(12), _snShaftPieces = new List<int>(6), _snRainGlows = new List<int>(16), _snShed = new List<int>(8);
        private LineRenderer _snTrail;
        private readonly List<int> _parolSticks = new List<int>(5), _lanterns = new List<int>(6), _lanternGlows = new List<int>(6),
            _footFlames = new List<int>(6), _sparks = new List<int>(8), _snLights = new List<int>(40), _snMarks = new List<int>(4);
        private Transform _snParol;
        // Which side the lens stands for THE RISE: the side nobody is standing on (film s1 went through somebody's hair).
        private float _snSide = 1f;

        private void BuildSean()
        {
            _duskLow = Wall("DuskGround", 0, 1.0f, new Color(.14f, .05f, .03f, .88f), radius: 15f);
            _duskGlow = Wall("DuskHorizon", 1.0f, 2.3f, new Color(.23f, .085f, .04f, 1f), radius: 15f, emission: .55f);
            _duskHigh = Wall("DuskSky", 2.3f, 40, new Color(.21f, .08f, .05f, .86f), radius: 15f, emission: .12f, cap: true);
            RagoUnlitBackdrop(_duskLow); RagoUnlitBackdrop(_duskGlow); RagoUnlitBackdrop(_duskHigh);

            // Where he will land, drawn on the court: the live blast's own reach (`telegraphRadius` 5.4) round where he stands.
            _snLandingFill = Add("LandingGlow", VfxShapes.Prism(40, .01f, 1f), new Color(1, .42f, .08f, .2f), .5f);
            _snLanding = Add("LandingRing", VfxShapes.Collar(56, .05f, .9f), new Color(1, .62f, .18f, .95f), 1f);
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
                _snMarks.Add(Add("InTheRing", VfxShapes.Collar(20, .05f, .55f), new Color(1, .3f, .06f, .95f), 1f));

            // The parol. Everything of it is built one unit across under one holder, so the lantern he holds and the giant
            // one over the court are the same object grown.
            _snParol = new GameObject("Parol").transform; _snParol.SetParent(_root.transform, false);
            _parolHalo = SeanPart(AddGlow("ParolWarmth", new Color(1, .38f, .05f, .22f), falloff: 2.6f, core: .12f));
            var stick = VfxShapes.Prism(4, 1, 1);
            for (int i = 0; i < 5; i++) _parolSticks.Add(SeanPart(Add("ParolStick" + i, stick, new Color(1, .88f, .48f, .96f), .85f)));
            _parolFill = SeanPart(Add("ParolFlame", VfxShapes.TwoSided(VfxShapes.Star(5, .42f, 3)), new Color(.96f, .22f, .04f, .72f), .7f));
            _parolRim = SeanPart(Add("ParolRim", VfxShapes.TwoSided(VfxShapes.Collar(48, .02f, .95f)), new Color(1, .78f, .3f, .95f), .9f));
            // The giant one's layers: a ten-point star and an eight-point one that turn against the parol, and a toothed rim.
            _snStarTen = SeanPart(Add("LanternStarTen", VfxShapes.TwoSided(VfxShapes.StarOutline(10, .62f, 71)), new Color(1, .56f, .14f, .9f), .9f));
            _snStarEight = SeanPart(Add("LanternStarEight", VfxShapes.TwoSided(VfxShapes.Star(8, .5f, 72)), new Color(.96f, .2f, .08f, .55f), .8f));
            _snTeeth = SeanPart(Add("LanternTeeth", VfxShapes.TwoSided(VfxShapes.Star(24, .86f, 73)), new Color(1, .8f, .3f, .5f), .8f));
            for (int i = 0; i < 12; i++) _snRays.Add(SeanPart(Add("LanternRay" + i, stick, new Color(1, .82f, .36f, .9f), 1f)));
            // What it throws when it ignites: a ring across the sky, a rain of sparks, and its light standing on the court.
            _snShock = Add("IgnitionRing", VfxShapes.TwoSided(VfxShapes.Collar(56, .02f, .93f)), new Color(1, .7f, .22f, .9f), 1f);
            for (int i = 0; i < SnShafts.Length; i++)
                _snShaftPieces.Add(Add("LanternShaft" + i, VfxShapes.Prism(8, 1f, .35f), new Color(1, .6f, .2f, .13f), .7f, plain: true));
            for (int i = 0; i < SnRain.Length; i++) _snRainGlows.Add(AddGlow("IgnitionSpark" + i, new Color(1, .7f, .25f, 1), falloff: 2.2f, core: .9f));
            for (int i = 0; i < 8; i++) _snShed.Add(AddGlow("ClimbSpark" + i, new Color(1, .6f, .18f, 1), falloff: 2.2f, core: .8f));
            _snTrail = Line("LanternTrail", 20, .16f, new Color(1, .55f, .14f, .8f));
            for (int ring = 0; ring < SnRings.Length; ring++)
                for (int i = 0; i < (int)SnRings[ring].y; i++)
                    _snLights.Add(SeanPart(AddGlow("ParolLight" + ring + "_" + i, SnLightColour[(int)SnRings[ring].z], falloff: 2.2f, core: .8f)));

            for (int i = 0; i < LanternSeats.Length; i++)
            {
                _lanternGlows.Add(AddGlow("LanternGlow" + i, new Color(1, .44f, .1f, .28f), falloff: 2.6f, core: .1f));
                _lanterns.Add(Add("PaperLantern" + i, VfxShapes.TwoSided(VfxShapes.StarOutline(5, .45f, 30 + i)), new Color(1, .78f, .3f, .95f), .7f));
            }
            for (int i = 0; i < 6; i++)
                _footFlames.Add(Add("CoilFlame" + i, VfxShapes.Tongue(5, .24f, .15f, .35f, .08f, 240 + i), new Color(1, i % 2 == 0 ? .32f : .58f, .04f, .7f), .6f));
            for (int i = 0; i < 8; i++)
                _sparks.Add(Add("RiseSpark" + i, VfxShapes.TwoSided(VfxShapes.Star(4, .4f, 50 + i)), new Color(1, .7f, .25f, .95f), .9f));
        }

        private int SeanPart(int index) { _pieces[index].Transform.SetParent(_snParol, false); return index; }

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
            var from = Vector3.Lerp(held, overhead, Ease(SnLift, SnRelease, t));
            float climb = Mathf.InverseLerp(SnRelease, SnTop, t); climb = 1 - (1 - climb) * (1 - climb);
            return t <= SnRelease ? from : Vector3.Lerp(overhead, new Vector3(0, SnHigh, .5f), climb);
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
            Tint(_duskLow, dusk); Tint(_duskHigh, dusk);
            var horizon = _pieces[_duskGlow].Color * (.85f + .25f * Ease(SnRelease, SnTop, t));
            horizon.a = 1;
            Tint(_duskGlow, dusk, horizon);

            // The parol: one holder, grown. It faces out of his chest, stands up to face the lens as it climbs, and lies over
            // flat above the court at the top.
            float size = SeanParolSize(t), giant = Mathf.InverseLerp(.6f, SnGiant, size), flat = Ease(SnFlatFrom, SnFlatTo, t);
            float turned = Mathf.Max(0, t - SnRelease) * 38f;
            _snParol.localPosition = SeanParolAt(t);
            _snParol.localRotation = Quaternion.Euler(90f * flat, 0, 0) * Quaternion.Euler(0, 0, turned);
            _snParol.localScale = Vector3.one * size;
            for (int i = 0; i < 5; i++)
            {
                // Stick i joins outer point i to outer point i + 2: the frame of a real parol.
                float a0 = (90 + i * 72) * Mathf.Deg2Rad, a1 = (90 + (i + 2) * 72) * Mathf.Deg2Rad;
                var p0 = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0);
                var p1 = new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0);
                float laid = Ease(.85f + i * .16f, .98f + i * .16f, t);
                // Each stick arrives from outside and settles into its seat: laid one by one.
                var mid = (p0 + p1) * .5f;
                var at = Vector3.Lerp(mid * 3.2f + Vector3.forward * .5f, mid, laid);
                var along = p1 - p0;
                var rotation = Quaternion.FromToRotation(Vector3.up, along.normalized);
                // Thinner for its size as it grows: a stick is a stick, not a beam.
                float thick = Mathf.Lerp(.053f, .012f, giant);
                Place(_parolSticks[i], at - rotation * Vector3.up * along.magnitude * .5f, new Vector3(thick, along.magnitude, thick), rotation, laid * leave);
            }
            float fill = Ease(SnFill, SnFill + .25f, t);
            Place(_parolFill, Vector3.back * .03f, Vector3.one * .95f, Quaternion.Euler(90, 0, 0) * Quaternion.Euler(0, 18, 0),
                fill * leave * Mathf.Lerp(1f, .5f, giant) * (1 + .5f * Flash(t, SnRelease, .1f)));
            PlaceGlow(_parolHalo, Vector3.zero, Vector3.one * 3f, Quaternion.identity, fill * leave * Mathf.Lerp(1f, .55f, giant));
            Place(_parolRim, Vector3.zero, Vector3.one * 1.08f, Quaternion.Euler(90, 0, 0), giant * leave);

            // Its layers, each on its own turn, and all of them only once it is big enough to have them.
            float burst = Ease(SnBurst, SnBurst + .3f, t);
            float layers = Ease(3.3f, 4.2f, t) * (1 - burst), flare = 1 + .6f * Flash(t, SnIgnite + .06f, .25f) + 1.2f * Flash(t, SnBurst + .03f, .2f);
            Place(_snStarTen, Vector3.back * .015f, Vector3.one * .78f, Quaternion.Euler(90, 0, 0) * Quaternion.Euler(0, -turned * 1.7f, 0), layers * flare);
            Place(_snStarEight, Vector3.back * .045f, Vector3.one * .46f, Quaternion.Euler(90, 0, 0) * Quaternion.Euler(0, turned * 2.6f, 0), layers * .8f * flare);
            Place(_snTeeth, Vector3.back * .06f, Vector3.one * 1.16f, Quaternion.Euler(90, 0, 0) * Quaternion.Euler(0, -turned * .6f, 0), layers * .7f);

            // Its lights: five rings. First a wave runs out from the middle, ring after ring, twice; then they chase round,
            // every third one bright and the bright one stepping on, each ring the other way from the last.
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
                    float bright = _reducedEffects ? .8f : ((i + step) % 3 + 3) % 3 == 0 ? 1.2f : .4f;
                    // On the burst every light flies out from the middle, the outer rings furthest.
                    PlaceGlow(_snLights[light], new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), -.02f) * SnRings[ring].x * (1 + burst * (1.2f + ring * .35f)), Vector3.one * (.11f + .05f * wave),
                        Quaternion.identity, lit * (bright + 1.6f * wave) * flare * leave);
                }
            }

            // Its rays, when it ignites and again as he rises: twelve, long and short by turns, out past its rim.
            float rayed = Mathf.Max(Ease(SnIgnite, SnIgnite + .12f, t) * (1 - Ease(SnIgnite + .5f, SnIgnite + 1.3f, t) * .65f), Ease(SnBurst - .04f, SnBurst + .08f, t) * 1.5f);
            for (int i = 0; i < _snRays.Count; i++)
            {
                float angle = (i * 30f + turned * .4f) * Mathf.Deg2Rad, reach = (i % 2 == 0 ? .62f : .34f) * rayed;
                var out_ = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                Place(_snRays[i], out_ * 1.2f, new Vector3(.022f, Mathf.Max(.001f, reach), .022f), Quaternion.FromToRotation(Vector3.up, out_), rayed * leave);
            }

            // The climb: a line of fire from where he let it go, and sparks shed behind it.
            var top = SeanParolAt(t); var let = HeadPoint + new Vector3(0, .7f, .2f);
            float climbing = Ease(SnRelease, SnRelease + .15f, t) * (1 - Ease(SnTop - .2f, SnTop + .5f, t)) * leave;
            for (int i = 0; i < _snTrail.positionCount; i++)
            {
                float u = i / (float)(_snTrail.positionCount - 1);
                _snTrail.SetPosition(i, Vector3.Lerp(Vector3.Lerp(let, top, .35f), top, u) + new Vector3(Mathf.Sin(u * 9f + t * 14f) * .12f * (1 - u), 0, 0));
            }
            _snTrail.enabled = climbing > .02f;
            _snTrail.widthMultiplier = .22f * climbing * Mathf.Lerp(1f, 2.4f, giant);
            for (int i = 0; i < _snShed.Count; i++)
            {
                float born = SnRelease + .12f + i * .2f, age = t - born;
                var from = SeanParolAt(born) + new Vector3(Mathf.Sin(i * 2.4f) * .3f, 0, Mathf.Cos(i * 1.7f) * .2f);
                PlaceGlow(_snShed[i], from + Vector3.down * age * age * 2.2f + new Vector3(Mathf.Sin(i * 3.1f) * .5f * age, 0, 0), Vector3.one * .16f * Mathf.Clamp01(1.2f - age),
                    Quaternion.identity, age >= 0 && age < 1.1f ? (1 - age / 1.1f) * leave : 0);
            }

            // The ignition, as it lies over: a ring thrown out across the sky from it, and a rain of sparks.
            // The same ring is thrown again, further, when it bursts round him.
            float ignited = t >= SnBurst ? t - SnBurst : t - SnIgnite, thrown = Mathf.Clamp01(ignited / (t >= SnBurst ? .4f : .55f));
            Place(_snShock, top, Vector3.one * Mathf.Lerp(SnGiant * .9f, SnGiant * 2.6f, 1 - (1 - thrown) * (1 - thrown)), Quaternion.identity,
                ignited >= 0 ? (1 - thrown) * leave : 0);
            for (int i = 0; i < _snRainGlows.Count; i++)
            {
                var row = SnRain[i]; float age = ignited - (i % 4) * .05f, a = row.x * Mathf.Deg2Rad;
                var from = top + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * row.y * SnGiant;
                PlaceGlow(_snRainGlows[i], from + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * age * 1.4f + Vector3.down * (row.z * age + 2.4f * age * age), Vector3.one * (.2f + .08f * (i % 3)),
                    Quaternion.identity, age >= 0 && age < 1.4f ? (1 - age / 1.4f) * leave : 0);
            }
            // Its light standing on the court in shafts, from the lantern down, once it is over it.
            float standing = Ease(SnIgnite + .05f, SnIgnite + .5f, t) * leave;
            for (int i = 0; i < _snShaftPieces.Count; i++)
            {
                var row = SnShafts[i];
                Place(_snShaftPieces[i], new Vector3(row.x, 0, row.y), new Vector3(row.z, top.y, row.z), Quaternion.Euler(0, i * 37, 0),
                    standing * (_reducedEffects ? .7f : .75f + .25f * Mathf.Sin(t * 3f + i * 1.9f)));
            }

            // Where he will land, and who is standing in it.
            float ringOn = Ease(SnRing, SnRing + .3f, t) * leave, pulse = _reducedEffects ? 1 : .8f + .2f * Mathf.Sin(t * 7f);
            Place(_snLandingFill, Vector3.up * .03f, new Vector3(SnLanding, 1, SnLanding), Quaternion.identity, ringOn);
            Place(_snLanding, Vector3.up * .04f, new Vector3(SnLanding, 1, SnLanding), Quaternion.Euler(0, t * 12f, 0), ringOn * pulse);
            for (int i = 0; i < _snMarks.Count && i < _others.Count; i++)
            {
                bool inside = _others[i].Reach <= SnLanding;
                Place(_snMarks[i], _others[i].Home + Vector3.up * .06f, Vector3.one * (1.15f + .12f * Mathf.Sin(t * 9f + i)), Quaternion.Euler(0, -t * 40f, 0), inside ? ringOn : 0);
            }

            // Paper-lantern stars drifting up behind him: his home, arriving with the dusk.
            for (int i = 0; i < _lanterns.Count; i++)
            {
                float born = .8f + i * .18f, age = Mathf.Max(0, t - born);
                var seat = LanternSeats[i];
                var at = seat + new Vector3(Mathf.Sin(age * .8f + i) * .15f, age * (.55f + (i % 3) * .12f), 0);
                var face = Quaternion.LookRotation(-new Vector3(at.x, 0, at.z - 3).normalized, Vector3.up) * Quaternion.Euler(90, 0, 0);
                float seen = Ease(born, born + .4f, t) * dusk;
                Place(_lanterns[i], at, Vector3.one * (.22f + (i % 2) * .06f), face * Quaternion.Euler(0, age * 20, 0), seen);
                PlaceGlow(_lanternGlows[i], at + face * Vector3.down * .02f, Vector3.one * (.78f + (i % 2) * .12f), Quaternion.identity, seen * (_reducedEffects ? .7f : .75f + .25f * Mathf.Sin(t * 4 + i)));
            }

            // The coil: flames lick up round his feet while he is low, and gather in on the rise.
            float coil = Ease(SnCoil, SnCoil + .3f, t) * (1 - Ease(SnRise, SnRise + .14f, t));
            for (int i = 0; i < _footFlames.Count; i++)
            {
                float a = i * Mathf.PI / 3 + t * 1.2f;
                float radius = Mathf.Lerp(.95f, .55f, Ease(SnRise - .4f, SnRise, t));
                Place(_footFlames[i], new Vector3(Mathf.Cos(a) * radius, .04f, Mathf.Sin(a) * radius),
                    new Vector3(.8f, Mathf.Lerp(.5f, 1.1f, coil), .8f), Quaternion.Euler(-14, -a * Mathf.Rad2Deg, 0), coil * leave);
            }

            // The rise: sparks race up ahead of him, toward the lantern.
            var chest = BothPalms;
            for (int i = 0; i < _sparks.Count; i++)
            {
                float age = t - (SnRise + .02f + i * .015f);
                bool alive = age >= 0;
                float u = Mathf.Clamp01(age / .36f);
                float angle = i * Mathf.PI * 2 / _sparks.Count;
                var at = chest + new Vector3(Mathf.Cos(angle) * .35f * u, u * 1.9f, Mathf.Sin(angle) * .25f * u);
                Place(_sparks[i], at, Vector3.one * .07f * (1 - u * .5f), Quaternion.Euler(-90, 0, angle * Mathf.Rad2Deg), alive ? (1 - u * u) * leave : 0);
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
