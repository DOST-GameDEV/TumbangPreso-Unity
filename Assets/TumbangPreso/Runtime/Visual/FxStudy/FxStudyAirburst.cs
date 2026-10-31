using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;
using Knot = TumbangPreso.Visual.FxStudyKit.Knot;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// STUDY: AIRBURST, recreated from the owner's reference, Nicolas Petrimaux's Dishonored 2 wind vortex (`ref2` in
    /// `Logs/coordinator/refs/airburst/`), LYING ON THE GROUND round her.
    ///
    /// The owner's notes, in order: of the court version, "it uses the same cartoony sticker vfx that we're working so hard
    /// to get rid of"; of the process, "need you to try and recreate the references I attached ... same process you did for
    /// the benchmark vfx"; of the first bench sample (r1, standing up in the air and winding inward), "the airburst is
    /// supposed to be laying flat on the ground ... airburst is a radius remember?" and "it reads as a sucking in type of
    /// thing. the reference clearly shows the air blasting outwards"; of r2 (one thin spiral with an open tail), "whats with
    /// this cowlick at the top left? get rid of that" and "it looks so empty now. the one u showed that was in the wrong
    /// orientation was close to my goal, it was just in the wrong direction".
    ///
    /// ⚠️ SO THIS IS r1's BUILD, KEPT WHOLE, WITH ONLY FOUR CHANGES: laid flat on the ground at the rule's radius with its
    /// leading edges rolled up a little; blasting OUTWARD (unwinding and growing from her feet, most of the way in the
    /// first fifth of a second, nothing travelling inward); no tail; no hard outer ring. r1's density stays: five wide arms
    /// in the bench's painted smoke mask (`trail_smoke`), overlapping, wide at the outside and a point at her feet.
    ///
    /// THE RULE IT DRAWS (`AmihanRules`): everyone within `StormSurgeAroundRadius` (10 m) of her is thrown outward, and the
    /// half court in front of her. The bench piece is that 10 m circle, centred on her (a grey post her height).
    ///
    /// THE READING OF THE REFERENCE, kept from r1: wide translucent sheets of air spiralling, each with a hard lit leading
    /// edge (`_Lead`) and a soft trailing side (`_Shade`), torn by erosion, never outlined; long thin dark speed lines,
    /// here radiating out across the floor; debris, light chips and dark dots, flung outward.
    /// </summary>
    public sealed class FxStudyAirburst : FxStudyEffect
    {
        private const float Lead = 0.05f, Life = 2.3f;
        private static float Radius => AmihanRules.StormSurgeAroundRadius;

        private readonly bool _above, _day;
        public override Color FloorColour => _day ? new Color(0.62f, 0.56f, 0.48f) : base.FloorColour;
        public override Color FloorLine => _day ? new Color(0.56f, 0.50f, 0.43f) : base.FloorLine;
        // On the pale floor the cells are chequered (as a court has painted lines and grain): a plain floor bends to itself
        // and the dome's light-bending cannot be seen on it.
        public override bool ChequeredFloor => _day;
        private readonly Transform _burst, _spin;
        private readonly List<FxPiece> _sheets = new List<FxPiece>();
        private readonly List<float> _sheetDelay = new List<float>();
        private readonly FxFlecks _flecks;
        private readonly FxPiece _kick, _dome;
        private readonly Transform _shell;
        private readonly List<FxPiece> _gusts = new List<FxPiece>();
        private readonly List<float> _gustAt = new List<float>();
        public const string DomeShader = "TumbangPreso/FxStudyAirDome";
        /// <summary>
        /// ⚠️ THE DOME BENDS THE LIGHT ONLY WHERE A FULL-SCREEN GRAB IS AFFORDABLE. Its shader grabs the scene once per camera
        /// while it is drawn (`_AirburstGrab`). On a lean graphics tier (`Settings.GraphicsProfiles`, Low) and on phones the
        /// shader's LOD 100 version is drawn instead: the same dome, nothing bent, no grab. True leaves it to that test.
        /// </summary>
        public static bool Bend = true;

        // ⚠️ HER COLOURS, NOT GREYS. The owner, of r10: "should probably color it the same as amihan's other abilities". So
        // every colour here is the approved WHIRLWIND's own (`FxStudyWhirlwind.Look.Picture`, read, never changed): its silk
        // ramp on the sheets, its hue wandering green to blue (`HueDrift`), and its ground greens on the dark motes.
        private static readonly FxStudyWhirlwind.Look Her = FxStudyWhirlwind.Look.Picture;
        private static readonly Color Deep = Her.SilkDeep, Body = Her.Silk, Hot = Her.SilkHot, Halo = Her.SilkHalo, Ink = Her.GroundDeep;

        public override float Seconds => Lead + Life;
        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov)
        {
            // A game-like look down at about 50 degrees, or straight down.
            if (_above) { focus = Vector3.zero; eye = new Vector3(0f, 25f, -0.01f); fov = 52f; return; }
            focus = new Vector3(0f, 0f, 0.5f); eye = focus + new Vector3(0f, 16.5f, -14.5f); fov = 60f;
        }

        public FxStudyAirburst(Transform parent) : this(parent, false) { }

        /// <summary>`day` films it on a pale warm floor, a stand-in for the sunlit court (as `dantebastionb6day`).</summary>
        public FxStudyAirburst(Transform parent, bool above, bool day = false) : base("airburst", parent)
        {
            _above = above; _day = day;
            float R = Radius;
            _burst = Group("burst");
            _spin = new GameObject("spin") { hideFlags = HideFlags.DontSave }.transform;
            _spin.SetParent(_burst, false);

            // Her, for scale: a grey post her height.
            var plain = Shader.Find("Unlit/Color");
            if (plain != null)
            {
                var grey = new Material(plain) { hideFlags = HideFlags.HideAndDontSave, color = new Color(0.36f, 0.37f, 0.40f) };
                var post = Add("stand-in", FxStudyKit.Square(false), grey, 0, Group("her"));
                grey.renderQueue = 1950;
                post.At.localPosition = new Vector3(0f, 0.85f, 0f); post.At.localScale = new Vector3(0.5f, 1.7f, 1f); post.At.localRotation = Quaternion.Euler(0f, -20f, 0f);
                post.Frame(0f, 1f);
            }

            // THE SHEETS: r1's five arms of a spiral that tightens toward her (r = R e^(-k(1-w))), laid on the ground, now
            // run from her (w 0) out to the rim (w 1) so they are drawn OUTWARD; wide at the outside, a point at her feet;
            // each rolled about its own length so its leading edge lifts a little (knee height at most).
            float scale = R / 2.15f;
            // ⚠️ NOT A PINWHEEL. r8 kept r1's five evenly spaced, equal arms, which read as a five-way symmetric pinwheel; the
            // reference's spiral is asymmetric. So each arm has its own start, turn and reach (start degrees, turn, reach share).
            float[,] arms = { { 0f, 330f, 1.00f }, { 61f, 265f, 0.80f }, { 150f, 305f, 0.94f }, { 208f, 375f, 0.70f }, { 287f, 280f, 0.88f } };
            for (int i = 0; i < 5; i++)
            {
                var knots = new List<Knot>();
                float start = arms[i, 0], turn = arms[i, 1], outer = 2.15f * arms[i, 2] * scale;
                for (int k = 0; k <= 10; k++)
                {
                    float w = k / 10f, a = (start + turn * w) * Mathf.Deg2Rad;
                    float r = outer * Mathf.Exp(-1.55f * (1f - w));
                    float lift = 0.05f + 0.04f * i + 0.10f * w;
                    knots.Add(new Knot(Mathf.Cos(a) * r, lift, Mathf.Sin(a) * r,
                        Mathf.Lerp(0.05f, 0.95f, Mathf.Pow(w, 0.8f)) * (0.85f + 0.1f * (i % 2)) * scale, 16f * w));
                }
                var sheet = Add("sheet-" + i, FxStudyKit.Sweep(knots, Vector3.up, perKnot: 8, across: 6, cup: 0.12f), FxStudyKit.Ribbon("trail_smoke", "erode_streak"), 10 + i, _spin);
                Dress(sheet, i * 0.37f).Set("_Lead", 0.80f).Set("_Shade", 0.45f);
                _sheets.Add(sheet); _sheetDelay.Add(0.02f * i);
            }

            // THE KICK: the bench wisp's dish streaks (`FxStudyWisp.DressDishStreaks`) thrown out from under her on the very
            // first frames, so the burst has a release frame before the arms have grown.
            var dish = new[] { new Vector2(0.15f, 0.03f), new Vector2(0.45f, 0.03f), new Vector2(0.80f, 0.03f), new Vector2(1.0f, 0.03f) };
            _kick = Add("kick", FxStudyKit.Revolve(dish, 48), FxStudyKit.Ribbon("erode_streak", "erode_cloud"), 20, Group("kick"));
            FxStudyWisp.DressDishStreaks(_kick).Colours(Deep, Body, Hot, Halo).Set("_Add", 0.3f).Set("_Ends", 0f, 0f, 0.32f).Set("_Hue", Her.HueDrift, 4f, 0.3f, 1f);

            // THE DOME: a shockwave of air blown out over the 10 m from her feet (the owner: "add a sphere (dome) that expands
            // outwards ... organic like wind shockwave ... a distort effect that it bends the light"). Seen by its rim, wind
            // streaks swept round it along the spiral's turn, torn open from the crown, faded before the court so there is no
            // ring at its foot, and bending what is behind it (`FxStudyAirDome.shader`).
            var domeShader = Shader.Find(DomeShader);
            if (domeShader != null)
            {
                // The tier test is a match's: a film outside Play (the bench) always draws the bending version, whatever the
                // editor's build target or the tier last chosen in it (r17 and r18 were filmed bending nothing because of that).
                bool lean = !Bend || (Application.isPlaying && (Application.isMobilePlatform || Settings.GraphicsProfiles.Of(Settings.GraphicsProfiles.Current).Lean));
                domeShader.maximumLOD = lean ? 100 : 300;
                var domeMat = new Material(domeShader) { hideFlags = HideFlags.HideAndDontSave };
                var streaks = FxStudyKit.Mask("erode_streak"); if (streaks != null) domeMat.SetTexture("_MaskTex", streaks);
                var tear = FxStudyKit.Mask("erode_cloud"); if (tear != null) domeMat.SetTexture("_ErodeTex", tear);
                _dome = Add("dome", FxStudyKit.Ball(40, 14, 0f, 1f), domeMat, 25, Group("dome"));
                _dome.Set("_Deep", Deep).Set("_Body", Body).Set("_Hot", Hot);
            }

            // ⚠️ THE DOME'S RIM IS WIND, NOT GLASS. r15's rim was the shader's fresnel, a thin even line round a clean ball. Now
            // the visible dome is GUSTS of the arms' own silk (`Dress`, `trail_smoke`) laid on the unit dome and swept round it
            // in the spiral's turn, each dropping toward the edge as it goes round, separate, some of the circle missing; they
            // grow with the dome, are brightest at their leading ends, and the crown ones tear first.
            _shell = Group("shell");
            // azimuth start (degrees), sweep (degrees), latitude from the crown at its start and end (degrees), width (unit), seed
            float[,] gusts =
            {
                { 0f, 110f, 48f, 72f, 0.34f, 0.2f }, { 95f, 80f, 58f, 80f, 0.28f, 0.9f }, { 150f, 120f, 40f, 66f, 0.30f, 1.6f },
                { 250f, 70f, 62f, 84f, 0.26f, 2.3f }, { 300f, 95f, 50f, 76f, 0.32f, 3.1f }, { 40f, 60f, 70f, 86f, 0.22f, 3.8f },
                { 200f, 65f, 66f, 86f, 0.24f, 4.6f },
            };
            for (int g = 0; g < gusts.GetLength(0); g++)
            {
                var knots = new List<Knot>();
                for (int k = 0; k <= 10; k++)
                {
                    float f = k / 10f, az = (gusts[g, 0] + gusts[g, 1] * f) * Mathf.Deg2Rad, lat = Mathf.Lerp(gusts[g, 2], gusts[g, 3], f) * Mathf.Deg2Rad;
                    var p = new Vector3(Mathf.Sin(lat) * Mathf.Cos(az), Mathf.Cos(lat), Mathf.Sin(lat) * Mathf.Sin(az)) * 1.015f;
                    float shape = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Pow(f, 0.6f))), 1.1f);
                    knots.Add(new Knot(p, gusts[g, 4] * (0.1f + 0.9f * shape)));
                }
                // Lying on the dome: its flat side faces out from the dome's middle.
                var gust = Add("gust-" + g, FxStudyKit.Sweep(knots, Vector3.up, perKnot: 6, across: 4, cup: 0.15f), FxStudyKit.Ribbon("trail_smoke", "erode_streak"), 26 + g, _shell);
                Dress(gust, gusts[g, 5]).Set("_Lead", 0.78f).Set("_Shade", 0.4f).Set("_Add", 0.7f);
                _gusts.Add(gust); _gustAt.Add(gusts[g, 2] / 90f);
            }

            // (No speed lines. The owner, of r10: "get rid of the black and white lines". The outward blast reads through the
            // sheets growing and unwinding and the debris thrown out ahead of them.)

            _flecks = AddFlecks("flecks", 40, "fleck_sheet");
            _flecks.Grid = 4;
            _flecks.Piece.Colours(Deep, Body, Hot, Halo).Set("_Add", 0f).Set("_Halo", 0f).Set("_Stops", 0.2f, 0.5f, 0.9f);
        }

        // r1's dressing, unchanged but for the erosion now eating the rim first.
        private static FxPiece Dress(FxPiece piece, float seed)
            => piece.Colours(Deep, Body, Hot, Halo)
                .Set("_Tile", 1.4f, 1f, seed, 0f).Set("_Scroll", 0.9f, 0f)
                .Set("_Cut", 0.05f, 0.50f).Set("_Curve", 1.7f).Set("_Stops", 0.30f, 0.58f, 0.86f)
                .Set("_Ends", 0.03f, 0.20f, 0.10f).Set("_ErodeTile", 1.5f, 1f, seed, 0f).Set("_ErodeScroll", 0.6f, 0f)
                // ⚠️ AIRIER (r12): r11 covered what was behind it (light share 0.25) and read heavy beside the Whirlwind, whose
                // silk is mostly light (`Look.Light` 0.85). Raised toward it, with the Whirlwind's halo round the bright parts and
                // its hot silk edge: the leading band and the mask's peaks lifted to near white.
                .Set("_Sweep", -1.2f).Set("_Soft", 0.25f).Set("_Halo", 0.45f).Set("_KeepHalo", 1f).Set("_Add", 0.62f)
                .Set("_LeadColor", Hot).Set("_Hue", Her.HueDrift, 6f, seed, 1f).WindowSoft(0.06f);

        /// <summary>The burst's reach as a share of its full size: most of it in the first fifth of a second.</summary>
        private static float Reach(float t) => Mathf.Lerp(0.30f, 1f, 1f - Mathf.Exp(-15f * Mathf.Max(0f, t)))
            // and it keeps drifting outward a little as it dies, so the rim tears away outward, not in place.
            * (1f + 0.12f * FxStudyKit.Ease(FxStudyKit.Run(t, 0.7f, 2.2f)));

        public override void Sample(float age, Vector3 eye)
        {
            float t = age - Lead;
            bool on = t >= 0f && t < Life;
            float reach = Reach(t);

            // Everything grows outward from her, and the spiral turns the way that UNWINDS it: its arms run outward.
            _burst.localScale = new Vector3(reach, 1f, reach);
            _spin.localRotation = Quaternion.Euler(0f, 80f * (1f - Mathf.Exp(-6f * Mathf.Max(0f, t))) + 10f * Mathf.Max(0f, t), 0f);

            // The dome: most of its growth in the first fifth of a second, a moment held, then torn open from the crown.
            if (_dome != null)
            {
                float grown = 1f - Mathf.Exp(-13f * Mathf.Max(0f, t));
                float r = Radius * Mathf.Lerp(0.08f, 1.02f, grown);
                _dome.At.localScale = new Vector3(r, r * Mathf.Lerp(0.75f, 0.42f, grown), r);
                _dome.At.localRotation = Quaternion.Euler(0f, t * 40f, 0f);
                _dome.Frame(age, on && t < 0.9f ? 1f : 0f, FxStudyKit.Ease(FxStudyKit.Run(t, 0.12f, 0.80f)));
                _shell.localScale = _dome.At.localScale;
                _shell.localRotation = _dome.At.localRotation;
            }
            for (int g = 0; g < _gusts.Count; g++)
            {
                // Each gust drawn round the dome in a tenth of a second, then torn away, the crown ones first.
                _gusts[g].Open(-0.05f, FxStudyKit.EaseOut(FxStudyKit.Run(t, 0.01f, 0.12f)) * 1.1f);
                _gusts[g].Frame(age, on && t < 0.9f ? 1f : 0f, FxStudyKit.Ease(FxStudyKit.Run(t, 0.08f + 0.35f * _gustAt[g], 0.85f)));
            }

            float thrown = FxStudyKit.EaseOut(FxStudyKit.Run(t, 0f, 0.22f));
            _kick.At.localScale = new Vector3(Mathf.Lerp(0.8f, 5.5f, thrown), 1f, Mathf.Lerp(0.8f, 5.5f, thrown));
            _kick.At.localRotation = Quaternion.Euler(0f, t * 120f, 0f);
            _kick.Frame(age, on && t < 0.35f ? 1f : 0f, Mathf.Max(0.25f, FxStudyKit.Run(t, 0.04f, 0.35f)));

            for (int i = 0; i < _sheets.Count; i++)
            {
                float u = t - _sheetDelay[i];
                // Drawn from her outward, then let go of from her outward, and torn away at the rim first (`_Sweep` < 0).
                float head = FxStudyKit.EaseOut(FxStudyKit.Run(u, 0f, 0.22f)) * 1.1f;
                float tail = FxStudyKit.Ease(FxStudyKit.Run(u, 0.6f, 2.0f)) * 0.8f - 0.1f;
                _sheets[i].Open(tail, head);
                _sheets[i].Frame(age, on && u >= 0f ? 0.85f : 0f, FxStudyKit.Ease(FxStudyKit.Run(u, 0.9f, 2.2f)));
            }

            // Debris thrown outward along the ground: light chips and dark dots, slowing as they go.
            _flecks.Begin(eye);
            for (int i = 0; i < 46; i++)
            {
                float u = t - 0.01f * (i % 7);
                if (u < 0f || u > 1.9f) continue;
                float a = FxStudyKit.Hash(i, 3) * Mathf.PI * 2f;
                float r = Radius * (0.15f + (0.75f + 0.45f * FxStudyKit.Hash(i, 7)) * (1f - Mathf.Exp(-4f * u)));
                Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 p = dir * r + Vector3.up * (0.15f + 0.9f * FxStudyKit.Hash(i, 9) * Mathf.Sin(Mathf.PI * Mathf.Clamp01(u / 1.2f)));
                bool dark = i % 3 != 0;
                float size = dark ? 0.12f : 0.2f + 0.12f * FxStudyKit.Hash(i, 13);
                float speed = Mathf.Exp(-4f * u);
                _flecks.Add(p, dir, size * (1.5f + 3f * speed), size, 1f - FxStudyKit.Run(u, 1.3f, 1.9f), i, dark ? Ink : Hot);
            }
            // And debris skimming the dome's outer surface, pushed out and down it as it grows.
            if (_dome != null && t >= 0f && t < 0.9f)
            {
                Vector3 sc = _dome.At.localScale;
                for (int i = 0; i < 24; i++)
                {
                    float az = FxStudyKit.Hash(i, 21) * Mathf.PI * 2f + t * 0.7f;
                    float lat = Mathf.Lerp(0.5f, 1.45f, Mathf.Clamp01(FxStudyKit.Hash(i, 23) * 0.6f + t * 1.1f));
                    Vector3 n = new Vector3(Mathf.Sin(lat) * Mathf.Cos(az), Mathf.Cos(lat), Mathf.Sin(lat) * Mathf.Sin(az));
                    Vector3 p = Vector3.Scale(n * 1.04f, sc);
                    Vector3 going = new Vector3(Mathf.Cos(lat) * Mathf.Cos(az), -Mathf.Sin(lat), Mathf.Cos(lat) * Mathf.Sin(az));
                    bool dark = i % 3 == 0;
                    _flecks.Add(p, going, 0.45f, 0.14f, 1f - FxStudyKit.Run(t, 0.5f, 0.9f), i + 7, dark ? Ink : Hot);
                }
            }
            _flecks.End(age);
        }
    }
}
