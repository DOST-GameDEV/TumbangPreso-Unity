using System;
using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // AMIHAN, AIRBURST v3, 5.6 s (`docs/reports/amihan-presentation-2026-10-02/airburst-v3.md`). The owner on the 3.6 s
        // version: *"throoughly revamp her cutscene what the fuck is that it sucsk"*, *"theres legit no vfx and shit"*, *"it
        // doesnt feel the same as paete and phaisters"*. That version was one close-up of a woman cupping air in front of a wall.
        //
        // ONE SENTENCE: Amihan calls the monsoon down into her hands, weaves it into her family's kasikus, and strings the whole
        // lane like a loom; everyone standing in it feels it coming. ONE TRAVELLING THING: thread. Wind arrives as broad sheets
        // (the Genshin wind language: bright rims, clear middles), becomes thread pouring into her palms, is woven into the
        // kasikus between them, and leaves as the WARP strung down the lane, which is the live fan every player then reads.
        //
        //   CALL  0.00 to 1.70  front right. The call out (0.25), the call in (0.55): the monsoon answers, six sheets arrive and
        //                       curl round her (open on the camera side, never a cage), a curtain of wind streaks rises behind.
        //   WEAVE 1.70 to 3.70  low right. The sheets pour into her palms as thread; the cup (2.15) draws the kasikus between them
        //                       and blooms it on the court; two pack beats (2.50, 2.95); the draw (3.30). The veil is deepest here.
        //   WARP  3.70 to 5.60  over the shoulder, rising down the lane. A flick (3.78) unravels the kasikus into the warp; the
        //                       live fan draws on along it; the REAL players in the lane brace (`HeroIntroductionScene.AmihanLane.cs`).
        //
        // v3.2: it SHOWS the release (5.05) and the real players in the fan thrown; play resumes on the hit, the live fan taking
        // up at the age this cutscene's last frame drew (`AmihanStorm.CutsceneTail`).
        // The density layer (glints, kasikus rings, flashes, the veil) is `HeroIntroductionScene.AmihanBurst.cs`.
        // ⚠️ Nothing here runs on `Update`; every piece is posed from the scene clock. Every row is typed, each its own place,
        // time, size and colour. Her colours only (wind green, cream, abel teal and rust, brooch gold), never white.
        // ⚠️ Reduced effects keeps every shape, stills the streak motion and halves the light.
        // =========================================================================================
        private const float AmCallOutAt = .25f, AmAnswerAt = .55f, AmWeaveAt = 1.70f, AmCupAt = 2.15f, AmPack1At = 2.50f,
            AmPack2At = 2.95f, AmDrawAt = 3.30f, AmWarpAt = 3.70f, AmFlickAt = 3.78f, AmBraceAt = 4.10f,
            // v3.2 (owner: *"show the ult actually hitting and knocking abck ppl already in the cutscene"*): THE BEATER. She drives
            // both palms, the fan releases, and the real players in it are thrown; play resumes on the hit 0.55 s later
            // (`AmihanStorm.CutsceneTail`, `AmihanRules.StormSurgeDelaySeconds` 0).
            AmReleaseAt = 5.05f;

        private int _amihanSky, _amihanDusk, _amihanGround;
        private readonly List<int> _viganHouses = new List<int>(20);
        private readonly List<WindVfx.Ribbon> _amSheets = new List<WindVfx.Ribbon>();
        private readonly List<Transform> _amSheetHosts = new List<Transform>();
        private readonly List<WindVfx.Ribbon> _amCurtain = new List<WindVfx.Ribbon>();
        private readonly List<WindVfx.Ribbon> _amihanThreads = new List<WindVfx.Ribbon>();
        private readonly List<WindVfx.Ribbon> _amEmblem = new List<WindVfx.Ribbon>();
        private readonly List<WindVfx.Ribbon> _amFloor = new List<WindVfx.Ribbon>();
        private readonly List<WindVfx.Ribbon> _amColumn = new List<WindVfx.Ribbon>();
        private readonly List<WindVfx.Ribbon> _amSpeed = new List<WindVfx.Ribbon>();
        private Transform _amEmblemHost, _amFloorHost, _amColumnHost;
        private int _amEmblemGlow = -1;
        private WindVfx.Motif _amihanNear, _amMotes;
        private AmihanStormFan _amihanFan;
        private float _amCourt;

        // THE SHEETS: arrival time, radius, start and end height, start angle and sweep (degrees round her, 0 is her front),
        // width, tilt (degrees from upright toward her, so each leans like a swept crescent), orbit speed (degrees per second).
        // The first is already crossing at the cut (the element is in the air before anything happens).
        private static readonly float[,] AmSheetRows =
        {
            { -.25f, 2.45f, .25f, 1.65f,  58f, 168f, .72f,  38f,  72f },
            {  .50f, 2.05f, 1.45f, .40f, 196f, 188f, .86f, -30f, -86f },
            {  .56f, 1.72f, .50f, 1.95f, 298f, 158f, .62f,  24f,  96f },
            {  .62f, 2.80f, 2.20f, .80f, 118f, 204f, .96f,  34f, -62f },
            {  .71f, 1.40f, .92f, 1.32f,  22f, 222f, .52f,  12f, 118f },
            {  .80f, 2.22f, .30f, 2.40f, 238f, 148f, .76f, -18f,  82f },
        };

        // THE CURTAIN: thin wind streaks rising behind her as the monsoon answers and again on the cup and packs. Angle round her
        // (degrees, behind her from the CALL lens), radius, start time, top height, width.
        private static readonly float[,] AmCurtainRows =
        {
            { 152f, 2.6f, .58f, 2.9f, .07f }, { 171f, 3.1f, .63f, 3.4f, .09f }, { 188f, 2.3f, .60f, 2.6f, .06f }, { 203f, 3.4f, .70f, 3.8f, .10f },
            { 219f, 2.8f, .66f, 3.1f, .07f }, { 236f, 3.2f, .75f, 3.6f, .08f }, { 250f, 2.5f, .72f, 2.7f, .06f }, { 141f, 3.6f, .80f, 3.9f, .09f },
            { 163f, 2.2f, 2.16f, 2.5f, .06f }, { 196f, 2.9f, 2.20f, 3.3f, .08f }, { 228f, 2.4f, 2.18f, 2.8f, .07f }, { 245f, 3.3f, 2.26f, 3.5f, .09f },
            { 177f, 2.7f, 2.52f, 3.0f, .07f }, { 212f, 3.0f, 2.55f, 3.4f, .08f }, { 158f, 3.4f, 2.97f, 3.7f, .09f }, { 233f, 2.6f, 3.00f, 2.9f, .07f },
        };

        // THE THREADS pouring in from down the lane: start point (scene space), the bend, delay, width, abel colour (index into `WindVfx.Threads`).
        private static readonly float[,] AmThreadRows =
        {
            { -1.9f, 2.1f,  7.6f, -1.0f,  .5f, 0f, .00f, .055f, 0 }, {  1.6f,  .9f,  6.8f,  .8f, -.2f, 0f, .08f, .045f, 1 },
            {  -.8f, 1.5f,  8.8f,  -.6f,  .2f, 0f, .16f, .050f, 2 }, {  2.4f, 1.8f,  9.2f,  .9f,  .4f, 0f, .22f, .040f, 3 },
            { -2.2f,  .7f,  5.6f,  -.7f, -.1f, 0f, .30f, .040f, 1 }, {  1.1f, 2.4f,  7.0f,  .5f,  .7f, 0f, .38f, .045f, 0 },
            {   .2f, 1.2f, 10.0f,  -.3f,  .3f, 0f, .44f, .050f, 2 }, { -2.8f, 1.6f,  4.8f, -1.1f,  .3f, 0f, .50f, .040f, 3 },
            {  2.9f, 1.1f,  5.2f,  1.2f, -.3f, 0f, .56f, .045f, 0 }, { -1.3f, 2.7f,  9.6f,  -.4f,  .9f, 0f, .62f, .040f, 1 },
            {  1.7f, 2.9f,  8.4f,   .7f, 1.0f, 0f, .68f, .040f, 2 }, {  -.4f,  .5f,  6.2f,  -.2f, -.2f, 0f, .74f, .050f, 0 },
        };

        // THE COLUMN of thread winding up round her while she packs: radius, turns, phase (degrees), width, abel colour.
        private static readonly float[,] AmColumnRows = { { .62f, 1.45f, 0f, .046f, 0 }, { .71f, 1.70f, 120f, .040f, 1 }, { .79f, 1.25f, 240f, .042f, 3 } };

        // THE SPEED STREAKS down the lane on the flick: x, height, start z, end z, delay, width.
        private static readonly float[,] AmSpeedRows =
        {
            { -.45f, 1.85f, 1.0f,  9.5f, .00f, .06f }, {  .62f, 1.35f, 1.2f, 10.5f, .03f, .05f }, { -1.10f,  .70f, 1.6f,  8.5f, .05f, .05f },
            { 1.30f, 2.05f, 1.4f, 11.0f, .02f, .06f }, {  .15f,  .45f, 1.1f,  9.0f, .07f, .04f }, { -1.60f, 1.55f, 2.0f, 10.0f, .04f, .05f },
            {  .95f,  .85f, 1.8f,  8.0f, .06f, .04f }, { -.20f, 2.35f, 1.5f, 12.0f, .01f, .05f },
        };

        // Her sheet colours: a mint middle and a near-cream rim (`WindRibbon`'s ink band turned bright), so each reads as swept
        // wind: bright edges, a middle you can see through.
        private static readonly Color AmSheetBody = new Color(0.78f, 0.96f, 0.72f, 1f);
        private static readonly Color AmFloorBody = new Color(0.46f, 0.76f, 0.36f, 1f), AmFloorInk = new Color(0.12f, 0.30f, 0.11f, 1f);

        private void BuildAmihan()
        {
            // The stage: the Monsoon look, cool green-white over a dusk band.
            _amihanGround = Wall("AmihanGround", 0, 0.9f, new Color(0.18f, 0.26f, 0.20f, 0.85f), emission: 0.15f);
            _amihanDusk = Wall("AmihanDusk", 0.9f, 2.4f, new Color(0.50f, 0.60f, 0.50f, 0.66f), emission: 0.28f);
            _amihanSky = Wall("AmihanSky", 2.4f, 12, new Color(0.72f, 0.84f, 0.76f, 0.55f), emission: 0.32f, cap: true);

            // Calle Crisologo: whitewashed lime plaster below, warm narra timber above, clay roofs, pearly capiz panes lit.
            var stone = new Color(0.80f, 0.74f, 0.62f, 1);
            var timber = new Color(0.40f, 0.26f, 0.17f, 1);
            var roof = new Color(0.60f, 0.31f, 0.21f, 1);
            var capiz = new Color(1.0f, 0.93f, 0.76f, 0.9f);
            for (int i = 0; i < 5; i++)
            {
                _viganHouses.Add(AddSolid("ViganStone" + i, VfxShapes.Prism(4, 1, 1), stone));
                _viganHouses.Add(AddSolid("ViganUpper" + i, VfxShapes.Prism(4, 1, 1), timber));
                _viganHouses.Add(AddSolid("ViganRoof" + i, VfxShapes.Prism(4, 1, .05f), roof));
                _viganHouses.Add(Add("ViganCapiz" + i, VfxShapes.Prism(4, 1, 1), capiz, 0.85f, plain: true));
            }

            // The court she stands on, not the classed ground under it (Bayan Plaza's tiles sit above it).
            _amCourt = Mathf.Clamp(AmihanStormFan.CourtUnder(_root.transform.position).y - _root.transform.position.y, 0f, .4f);
            var cup = new Vector3(.35f, .62f, .12f);

            // THE SHEETS, each on its own host so it can orbit, contract into her hands and thin away.
            for (int i = 0; i < AmSheetRows.GetLength(0); i++)
            {
                var host = new GameObject("AmihanSheetHost" + i).transform; host.SetParent(_root.transform, false);
                float r = AmSheetRows[i, 1], h0 = AmSheetRows[i, 2], h1 = AmSheetRows[i, 3], a0 = AmSheetRows[i, 4], sweep = AmSheetRows[i, 5];
                float tilt = AmSheetRows[i, 7] * Mathf.Deg2Rad;
                var spine = new List<Vector3>(30); var radial = new List<Vector3>(30);
                for (int k = 0; k < 30; k++)
                {
                    float u = k / 29f, a = (a0 + sweep * u) * Mathf.Deg2Rad, rr = r * (1f - .22f * u);
                    var outward = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                    spine.Add(outward * rr + Vector3.up * (h0 + (h1 - h0) * u)); radial.Add(outward);
                }
                float cos = Mathf.Cos(tilt), sin = Mathf.Sin(tilt);
                var sheet = WindVfx.Build(host, "AmihanSheet" + i, spine, AmSheetRows[i, 6],
                    k => Vector3.up * cos - radial[k] * sin, 3.0f, .16f, 140f + i);
                AmBrightRim(sheet, .58f);
                _amSheets.Add(sheet); _amSheetHosts.Add(host);
            }

            // THE CURTAIN.
            for (int i = 0; i < AmCurtainRows.GetLength(0); i++)
            {
                float a = AmCurtainRows[i, 0] * Mathf.Deg2Rad, r = AmCurtainRows[i, 1];
                var at = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * r;
                var side = new Vector3(Mathf.Cos(a), 0, -Mathf.Sin(a));
                var spine = new[]
                {
                    at + Vector3.up * (.05f + _amCourt), at + Vector3.up * (AmCurtainRows[i, 3] * .5f + _amCourt),
                    at + Vector3.up * (AmCurtainRows[i, 3] + _amCourt),
                };
                var streak = WindVfx.Build(_root.transform, "AmihanCurtain" + i, spine, AmCurtainRows[i, 4], _ => side, 2.0f, .3f, 160f + i);
                AmBrightRim(streak, .6f);
                _amCurtain.Add(streak);
            }

            // THE THREADS: open curves from down the lane into her cupped hands, each in an abel colour.
            for (int i = 0; i < AmThreadRows.GetLength(0); i++)
            {
                var from = new Vector3(AmThreadRows[i, 0], AmThreadRows[i, 1], AmThreadRows[i, 2]);
                var control = Vector3.Lerp(from, cup, .5f) + new Vector3(AmThreadRows[i, 3], AmThreadRows[i, 4], AmThreadRows[i, 5]);
                var spine = new List<Vector3>(24);
                for (int k = 0; k < 24; k++)
                {
                    float u = k / 23f;
                    spine.Add(Vector3.Lerp(Vector3.Lerp(from, control, u), Vector3.Lerp(control, cup, u), u));
                }
                var thread = WindVfx.Build(_root.transform, "AmihanThread" + i, spine, AmThreadRows[i, 7], WindVfx.Standing, 5.0f, .3f, 70f + i);
                var colour = WindVfx.Threads[(int)AmThreadRows[i, 8] % WindVfx.Threads.Length];
                thread.Recolour(Color.Lerp(colour, WindVfx.Core, .45f), colour, colour * .55f);
                _amihanThreads.Add(thread);
            }

            // THE EMBLEM: her kasikus, three graduated woven diamonds and a light inside, drawn between her palms.
            _amEmblemHost = new GameObject("AmihanKasikusEmblem").transform; _amEmblemHost.SetParent(_root.transform, false);
            float[] er = { .11f, .19f, .28f };
            Color[] ec = { WindVfx.Gold, WindVfx.Cotton, WindVfx.Body };
            for (int i = 0; i < er.Length; i++)
            {
                var spine = AmDiamond(er[i], false);
                var ring = WindVfx.Build(_amEmblemHost, "AmihanEmblem" + i, spine, i == 0 ? .04f : .032f, AmPlanar(spine), 4.0f, .35f, 120f + i);
                ring.Recolour(Color.Lerp(ec[i], WindVfx.Core, .5f), ec[i], WindVfx.Ink);
                _amEmblem.Add(ring);
            }
            _amEmblemGlow = AddGlow("AmihanEmblemLight", new Color(0.70f, 0.95f, 0.58f, 1f), falloff: 2.2f, core: .6f, lift: .12f);

            // THE FLOOR KASIKUS: five graduated diamonds and the two threads that cross them, woven into the court.
            _amFloorHost = new GameObject("AmihanFloorKasikus").transform; _amFloorHost.SetParent(_root.transform, false);
            _amFloorHost.localPosition = Vector3.up * (_amCourt + .02f);
            float[] fr = { .80f, 1.30f, 1.80f, 2.30f, 2.85f };
            for (int i = 0; i < fr.Length; i++)
            {
                var spine = AmDiamond(fr[i], true);
                var d = WindVfx.Build(_amFloorHost, "AmihanFloorDiamond" + i, spine, .13f - i * .01f, WindVfx.Flat(spine), 3.0f, .3f, 180f + i);
                AmFloorStroke(d);
                _amFloor.Add(d);
            }
            for (int i = 0; i < 2; i++)
            {
                var axis = i == 0 ? Vector3.right : Vector3.forward;
                var spine = new List<Vector3>(16);
                for (int k = 0; k < 16; k++) spine.Add(axis * Mathf.Lerp(-3.0f, 3.0f, k / 15f) + Vector3.up * .01f);
                var cross = WindVfx.Build(_amFloorHost, "AmihanFloorWeft" + i, spine, .05f, WindVfx.Flat(spine), 6.0f, .3f, 190f + i);
                AmFloorStroke(cross);
                _amFloor.Add(cross);
            }

            // THE COLUMN of thread winding up round her while she packs: open strands with space between them, never a cage.
            _amColumnHost = new GameObject("AmihanThreadColumn").transform; _amColumnHost.SetParent(_root.transform, false);
            for (int i = 0; i < AmColumnRows.GetLength(0); i++)
            {
                var spine = WindVfx.Helix(Vector3.up * (.1f + _amCourt), Vector3.up * (2.35f + _amCourt), AmColumnRows[i, 0], AmColumnRows[i, 1], 32, AmColumnRows[i, 2], .85f);
                var strand = WindVfx.Build(_amColumnHost, "AmihanColumn" + i, spine, AmColumnRows[i, 3], WindVfx.AroundAxis(spine, Vector3.up), 5.0f, .3f, 200f + i);
                var colour = WindVfx.Threads[(int)AmColumnRows[i, 4] % WindVfx.Threads.Length];
                strand.Recolour(Color.Lerp(colour, WindVfx.Core, .45f), colour, colour * .55f);
                _amColumn.Add(strand);
            }

            // THE SPEED STREAKS down the lane on the flick.
            for (int i = 0; i < AmSpeedRows.GetLength(0); i++)
            {
                var spine = new List<Vector3>(12);
                for (int k = 0; k < 12; k++)
                    spine.Add(new Vector3(AmSpeedRows[i, 0], AmSpeedRows[i, 1], Mathf.Lerp(AmSpeedRows[i, 2], AmSpeedRows[i, 3], k / 11f)));
                var streak = WindVfx.Build(_root.transform, "AmihanSpeed" + i, spine, AmSpeedRows[i, 5], WindVfx.Standing, 2.0f, .3f, 210f + i);
                AmBrightRim(streak, .55f);
                _amSpeed.Add(streak);
            }

            // Cotton rising round her while she weaves, and cotton and thread at the lens in every shot. Each Motif gets its own
            // host: a Motif hands its shared tuft mesh to its parent's single GeneratedMeshOwner, and two on one object throw.
            var motes = new GameObject("AmihanRisingCotton").transform; motes.SetParent(_root.transform, false);
            _amMotes = new WindVfx.Motif(motes, 18, 21.3f, .25f);
            var nearHost = new GameObject("AmihanNearLayer").transform; nearHost.SetParent(_root.transform, false);
            _amihanNear = new WindVfx.Motif(nearHost, 14, 13.7f, .5f);

            // The live fan itself, posed from this scene's clock (never its own Update): its warp is drawn on along the flick, so
            // the handback frame is the live fan's first frame on the same court.
            _amihanFan = AmihanStormFan.Build(_root.transform, _root.transform.position, _root.transform.forward,
                Core.AmihanRules.StormSurgeGatherSeconds);
            _amihanFan.enabled = false;

            BuildAmihanBurst();
            BuildAmihanLane();
        }

        /// <summary>`WindRibbon`'s ink band turned to a bright rim: the swept-sheet look (bright edges, a middle you see through).</summary>
        private static void AmBrightRim(WindVfx.Ribbon ribbon, float rimFrom)
        {
            ribbon.Recolour(WindVfx.Body, AmSheetBody, WindVfx.Core);
            if (ribbon.Material != null) { ribbon.Material.SetFloat("_InkFrom", rimFrom); ribbon.Material.SetFloat("_InkAlpha", .9f); }
        }

        /// <summary>A stroke woven into the court: her darker greens and a solid ink rim (the live fan's floor language).</summary>
        private static void AmFloorStroke(WindVfx.Ribbon ribbon)
        {
            ribbon.Recolour(WindVfx.Body, AmFloorBody, AmFloorInk);
            if (ribbon.Material != null) { ribbon.Material.SetFloat("_InkFrom", .45f); ribbon.Material.SetFloat("_InkAlpha", .95f); }
        }

        /// <summary>A kasikus diamond of radius <paramref name="r"/>, corners on the axes, flat on the court or upright in XY.</summary>
        private static List<Vector3> AmDiamond(float r, bool flat)
        {
            var spine = new List<Vector3>(25);
            for (int k = 0; k < 4; k++)
                for (int j = 0; j < 6; j++)
                {
                    float a0 = k * 90f * Mathf.Deg2Rad, a1 = (k + 1) * 90f * Mathf.Deg2Rad, u = j / 6f;
                    float x = Mathf.Lerp(Mathf.Sin(a0), Mathf.Sin(a1), u) * r, y = Mathf.Lerp(Mathf.Cos(a0), Mathf.Cos(a1), u) * r;
                    spine.Add(flat ? new Vector3(x, 0, y) : new Vector3(x, y, 0));
                }
            spine.Add(spine[0]);
            return spine;
        }

        /// <summary>A side vector that lays a ribbon in the XY plane, across its own direction.</summary>
        private static Func<int, Vector3> AmPlanar(IList<Vector3> spine)
            => i =>
            {
                var along = spine[Mathf.Min(i + 1, spine.Count - 1)] - spine[Mathf.Max(i - 1, 0)];
                along.z = 0f;
                return along.sqrMagnitude > 1e-6f ? Vector3.Cross(Vector3.forward, along) : Vector3.up;
            };

        /// <summary>A beat's swell: up at once, gone by <paramref name="width"/>.</summary>
        private static float AmBeat(float t, float at, float width = .22f) => t < at ? 0f : Mathf.Clamp01(1f - (t - at) / width);

        private void SampleAmihan(float t)
        {
            bool calm = _reducedEffects;
            float phase = calm ? 0.0f : t * 3.0f;
            float light = calm ? .55f : 1f;
            float leave = 1 - Ease(Seconds - .30f, Seconds, t);

            // The stage stands up at once and steps back to the real court through the WARP shot.
            float stage = Ease(0, .3f, t) * (1 - Ease(3.75f, 4.35f, t));
            Tint(_amihanGround, stage); Tint(_amihanDusk, stage); Tint(_amihanSky, stage);
            for (int i = 0; i < 5; i++)
            {
                float angle = (180 - 58 + i * 29) * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Sin(angle) * 7.4f, 0, Mathf.Cos(angle) * 7.4f);
                var face = Quaternion.LookRotation(-at.normalized, Vector3.up);
                float width = 1.25f + (i % 2) * .25f;
                float rise = Ease(.05f + i * .04f, .35f + i * .04f, t) * (1 - Ease(3.75f + i * .04f, 4.25f + i * .03f, t));
                float on = rise > .01f ? 1 : 0;
                Place(_viganHouses[i * 4], at, new Vector3(width, 1.2f * rise, .7f), face, on);
                Place(_viganHouses[i * 4 + 1], at + Vector3.up * 1.2f * rise, new Vector3(width * 1.12f, 1.0f * rise, .82f), face, on);
                Place(_viganHouses[i * 4 + 2], at + Vector3.up * 2.2f * rise, new Vector3(width * 1.3f, .55f * rise, 1.0f), face * Quaternion.Euler(0, 45, 0), on);
                Place(_viganHouses[i * 4 + 3], at + face * Vector3.forward * .43f + Vector3.up * 1.62f * rise, new Vector3(width * .9f, .22f * rise, .05f), face, rise);
            }

            var cup = BothPalms;
            float packs = AmBeat(t, AmPack1At) + AmBeat(t, AmPack2At) + .8f * AmBeat(t, AmDrawAt, .3f);

            // THE SHEETS: each sweeps in (its head racing round her), orbits, then pours into her palms as the weave begins.
            for (int i = 0; i < _amSheets.Count; i++)
            {
                float arrive = AmSheetRows[i, 0];
                float s = t - arrive;
                float head = Ease(0f, .38f, s);
                float tail = Ease(.22f, .75f, s) * .55f;
                float pour = Ease(AmWeaveAt, AmCupAt + .05f, t);
                var host = _amSheetHosts[i];
                host.localRotation = Quaternion.Euler(0f, AmSheetRows[i, 8] * Mathf.Max(0f, s) * (1f + 1.5f * pour), 0f);
                host.localPosition = Vector3.Lerp(Vector3.zero, cup - Vector3.up * .5f, pour);
                host.localScale = Vector3.one * Mathf.Lerp(1f, .18f, pour);
                // // SUBTLE (owner, on v3.6: *"make her vfx in ult as well a bit more subtle"*, *"it looks like shapes are floating"*). 
                float alpha = (.42f + .08f * (i % 3)) * Ease(-.05f, .12f, s) * (1f - Ease(AmCupAt - .1f, AmCupAt + .1f, t)) * light;
                _amSheets[i].Set(alpha * leave, phase * (1.2f + .15f * i), head, tail, Ease(AmWeaveAt + .1f, AmCupAt, t) * .8f);
            }

            // THE CURTAIN: each streak rises, its tail following, a first wave on the answer and a second on the cup and packs.
            for (int i = 0; i < _amCurtain.Count; i++)
            {
                float s = t - AmCurtainRows[i, 2];
                if (s < 0f || s > 1.2f) { _amCurtain[i].Set(0f, 0f); continue; }
                float stageLight = 1f - Ease(3.4f, 3.75f, t);
                _amCurtain[i].Set(.32f * light * stageLight, -phase * 2f, Ease(0f, .5f, s), Ease(.3f, 1.1f, s), Ease(.6f, 1.2f, s) * .7f);
            }

            // THE THREADS: answering the call, streaming in and packed into her hands by the second beat; nothing left once she aims.
            for (int i = 0; i < _amihanThreads.Count; i++)
            {
                float d = AmThreadRows[i, 6];
                float head = Ease(AmAnswerAt + d, AmWeaveAt + .1f + d * .6f, t);
                float tail = Ease(AmWeaveAt + d * .5f, AmPack2At + d * .3f, t);
                float alpha = (.62f + .1f * (i % 3)) * (1f - Ease(AmPack2At + .1f, AmDrawAt, t));
                _amihanThreads[i].Set(alpha * leave, -phase * 1.6f, head, tail * .98f, Ease(AmPack2At - .2f, AmDrawAt, t) * .7f);
            }

            // THE EMBLEM: drawn on at the cup, facing the lens, turning slowly; it swells on each beat and unravels into the warp.
            AmLens(t, out int shot, out var eye, out var lensLook, out _);
            var emblemAt = cup + new Vector3(0f, .03f, .05f);
            _amEmblemHost.localPosition = emblemAt;
            var toEye = eye - emblemAt; toEye.y *= .3f;
            _amEmblemHost.localRotation = Quaternion.LookRotation(toEye.sqrMagnitude > 1e-4f ? -toEye.normalized : Vector3.back)
                                           * Quaternion.Euler(0f, 0f, calm ? 0f : t * 24f);
            float unravel = Ease(AmFlickAt, AmFlickAt + .2f, t);
            float emblemScale = (1f + .28f * packs) * Mathf.Lerp(1f, 2.4f, unravel) * GrowthVfx.Pop(Mathf.Clamp01((t - AmCupAt) / .22f));
            _amEmblemHost.localScale = Vector3.one * Mathf.Max(.001f, emblemScale);
            for (int i = 0; i < _amEmblem.Count; i++)
            {
                float on = Ease(AmCupAt + i * .05f, AmCupAt + .18f + i * .05f, t);
                float alpha = Ease(AmCupAt - .02f, AmCupAt + .05f, t) * (1f - unravel) * (.85f + .15f * packs);
                _amEmblem[i].Set(.6f * alpha * leave, phase * (i % 2 == 0 ? 1f : -1f), on, 0f, unravel * .8f);
            }
            float glow = t < AmCupAt ? 0f : (.6f + .5f * packs + .7f * AmBeat(t, AmCupAt, .25f)) * (1f - unravel) * light;
            PlaceGlow(_amEmblemGlow, emblemAt, Vector3.one * (.42f + .18f * packs), Quaternion.identity, glow * leave);

            // THE FLOOR KASIKUS: blooms out of the court at the cup, steps inward on each beat, and gives way to the live fan's own
            // diamonds at her feet as the warp is strung.
            float bloom = t < AmCupAt ? 0f : GrowthVfx.Pop(Mathf.Clamp01((t - AmCupAt) / .3f));
            float step = .07f * Ease(AmPack1At, AmPack1At + .12f, t) + .07f * Ease(AmPack2At, AmPack2At + .12f, t) + .06f * Ease(AmDrawAt, AmDrawAt + .2f, t);
            float floorAlpha = Ease(AmCupAt - .02f, AmCupAt + .08f, t) * (1f - Ease(AmWarpAt + .1f, AmBraceAt + .2f, t));
            for (int i = 0; i < _amFloor.Count; i++)
            {
                bool diamond = i < 5;
                float grow = Mathf.Lerp(.3f, 1f, bloom) * (1f - step) * (diamond ? 1f : Mathf.Lerp(.6f, 1f, bloom));
                _amFloor[i].GameObject.transform.localScale = new Vector3(Mathf.Max(.001f, grow), 1f, Mathf.Max(.001f, grow));
                _amFloor[i].GameObject.transform.localRotation = Quaternion.Euler(0f, diamond ? (i % 2 == 0 ? 1f : -1f) * (calm ? 0f : t * 9f) : 45f, 0f);
                float a = floorAlpha * (diamond ? .9f - i * .08f : .55f) * (1f + .3f * packs) * .4f;
                _amFloor[i].Set(Mathf.Clamp01(a) * leave, phase * .4f, 1f, 0f, Ease(AmWarpAt, AmBraceAt + .2f, t) * .7f);
            }

            // THE COLUMN: winds up round her from the cup, quickening on each beat, and is flung out with the flick.
            float columnOn = Ease(AmCupAt, AmCupAt + .4f, t);
            _amColumnHost.localScale = Vector3.one * Mathf.Lerp(1f, 1.9f, unravel);
            _amColumnHost.localRotation = Quaternion.Euler(0f, calm ? 0f : t * 70f + 160f * (Ease(AmPack1At, AmPack1At + .2f, t) + Ease(AmPack2At, AmPack2At + .2f, t)), 0f);
            for (int i = 0; i < _amColumn.Count; i++)
            {
                float alpha = .7f * columnOn * (1f - Ease(AmFlickAt, AmFlickAt + .25f, t)) * light;
                _amColumn[i].Set(alpha * leave, -phase * (1.4f + .3f * i) - packs, columnOn, 0f, .2f + unravel * .7f);
            }

            // THE SPEED STREAKS race down the lane on the flick, past the lens.
            for (int i = 0; i < _amSpeed.Count; i++)
            {
                float s = t - AmFlickAt - AmSpeedRows[i, 4];
                if (s < 0f || s > .5f) { _amSpeed[i].Set(0f, 0f); continue; }
                _amSpeed[i].Set(.75f * light * leave, phase * 3f, Ease(0f, .18f, s), Ease(.1f, .45f, s), Ease(.2f, .5f, s) * .7f);
            }

            // Cotton rising round her while she weaves.
            float rising = Ease(AmCupAt - .1f, AmCupAt + .2f, t) * (1f - Ease(AmWarpAt - .1f, AmWarpAt + .15f, t));
            float court = _amCourt;
            _amMotes.Step(Mathf.Repeat((t - AmCupAt) / 1.6f, 1f), (calm ? .4f : .85f) * rising, (start, drift, u) =>
            {
                float a = start.x * 11f + u * 2.4f, r = .5f + start.z * .9f + u * .25f;
                return new Vector3(Mathf.Sin(a) * r, court + .2f + u * (2.2f + drift.y), Mathf.Cos(a) * r);
            }, .085f);

            // The near layer: cotton and thread drifting from the lens toward her (in the WARP, down the lane), on every shot.
            if (shot >= 0)
            {
                var nearEye = eye; var nearLook = lensLook;
                var toward = shot >= AmShotRide ? new Vector3(0f, .8f, 9f) : cup;
                _amihanNear.Step(Mathf.Repeat(t / 1.4f, 1.0f), (calm ? .4f : .85f) * Ease(0, .2f, t) * leave, (start, drift, u) =>
                    Vector3.Lerp(Vector3.Lerp(nearEye, nearLook, .2f + start.y * .2f) + new Vector3(start.x, start.z * .6f, 0) * 1.1f, toward, u * .6f), .07f);
            }

            SampleAmihanBurst(t, leave);
            SampleAmihanLane(t);

            // WARP and RELEASE: the live fan's own clock, strung over the flick (its draw-on and gather pressed into 3.78 to 5.05)
            // and released ON the beater; at 5.6 s it is the live fan `AmihanStorm.CutsceneTail` after its release.
            float gather = Core.AmihanRules.StormSurgeGatherSeconds;
            float fanAge = t < AmFlickAt ? -AmihanStormFan.DrawOnSeconds - 1f
                : t < AmReleaseAt ? Mathf.Lerp(-AmihanStormFan.DrawOnSeconds, gather, (t - AmFlickAt) / (AmReleaseAt - AmFlickAt))
                : gather + (t - AmReleaseAt);
            _amihanFan.StepTo(fanAge);
        }

        // The seven shots (`tools/author_ultimate_intros.py` amihan()): CALL, ANSWER, WEAVE, CUP, PACK, RIDE, REVEAL.
        private const int AmShotAnswer = 1, AmShotCup = 3, AmShotRide = 5;

        /// <summary>The lens at this moment in scene space, computed shots included (no shake).</summary>
        private bool AmLens(float t, out int shot, out Vector3 eye, out Vector3 look, out float fov)
        {
            shot = ShotIndexAt(t); eye = new Vector3(2.5f, 1f, 1.5f); look = Vector3.up; fov = 50f;
            if (shot < 0) return false;
            _performance.Shot(shot, t, out eye, out look, out fov);
            AmihanFrame(shot, t, ref eye, ref look, ref fov);
            return true;
        }

        /// <summary>
        /// ⚠️ v3.1, THE COMPUTED SHOTS (owner, 2026-10-03: *"thoroughly think abt how to improve vfx as well as cutscene direciton
        /// bcz it looks so bad compared to paete and phasiter"*). What Paete's and Phaister's cutscenes have that hers did not is a
        /// camera that ANSWERS the action: Paete's rides his limb out to the players it takes, Phaister's pushes in on the doll's
        /// face as it turns. Hers held three slow pushes. Now:
        ///  * ANSWER: the lens orbits round her right side, rising, as the six sheets curl round her, so the wind reads as 3D
        ///    sweeping arcs and not flat lines across the frame (Jean's wide).
        ///  * CUP: in close on her REAL palms (the rig's, not a typed point), her face kept in the top of the frame (the owner's
        ///    feel-pass note), punching in on the cup and the first pack (Venti's draw: the charge lives in the hands).
        ///  * RIDE: the lens chases the warp threads down the lane to the nearest player standing in it, arriving beside them
        ///    as they brace (`HeroIntroductionScene.AmihanLane.cs`); nobody in it, it rides to the middle of the lane.
        /// </summary>
        private void AmihanFrame(int index, float t, ref Vector3 eye, ref Vector3 look, ref float fov)
        {
            if (index == AmShotAnswer)
            {
                float u = Ease(AmAnswerAt, AmWeaveAt, t);
                // To her right side and no further: film r1's 105 degrees ended on the back of her head.
                float a = Mathf.Lerp(30f, 80f, u) * Mathf.Deg2Rad, r = Mathf.Lerp(3.9f, 4.2f, u);
                eye = new Vector3(Mathf.Sin(a) * r, Mathf.Lerp(.8f, 1.7f, u) + _amCourt, Mathf.Cos(a) * r);
                look = new Vector3(0f, 1.2f + .1f * u + _amCourt, 0f);
                fov = Mathf.Lerp(52f, 48f, u);
            }
            else if (index == AmShotCup)
            {
                var palms = BothPalms;
                float u = Ease(AmCupAt, AmPack2At, t);
                // Film r1 at 1.5 m and 34 degrees cropped her eyes off: her head is large. Her whole face over her hands.
                look = Vector3.Lerp(palms, HeadPoint, .5f);
                var dir = new Vector3(1.0f, .12f, .7f).normalized;
                eye = look + dir * Mathf.Lerp(2.5f, 2.25f, u);
                fov = 40f - 4f * Decay(t - AmCupAt, .18f) - 2.5f * Decay(t - AmPack1At, .15f);
            }
            else if (index == AmShotRide)
            {
                var lead = AlLead();
                var flat = new Vector3(lead.x, 0f, lead.z);
                var dir = flat.sqrMagnitude > 1e-3f ? flat.normalized : Vector3.forward;
                var side = Vector3.Cross(Vector3.up, dir);
                var from = new Vector3(.85f, 1.15f + _amCourt, .35f);
                // Film r1 ended 2.3 m off them on a face filling the frame: back and up, their whole body, the lane and the warp.
                var to = lead - dir * 3.8f + side * 1.6f + Vector3.up * .9f;
                float chase = Ease(AmWarpAt, AmBraceAt + .2f, t);
                eye = Vector3.Lerp(from, to, Mathf.Pow(chase, .8f)) + Vector3.up * .25f * Mathf.Sin(Mathf.PI * chase);
                // Creep in on them as they brace.
                eye += dir * .25f * Ease(AmBraceAt + .2f, AmBraceAt + .35f + .3f, t);
                look = Vector3.Lerp(new Vector3(0f, .8f + _amCourt, 3f), lead - Vector3.up * .35f, Ease(AmWarpAt, AmBraceAt, t));
                fov = Mathf.Lerp(56f, 50f, chase);
            }
        }

        /// <summary>The world steps back while the power is on screen and returns for the real lane (Paete's grade, her numbers).</summary>
        private void AmihanGrade(float t, out float brightness, out float saturation)
        {
            float away = Ease(.1f, .55f, t) * (1f - Ease(AmWarpAt, AmWarpAt + .7f, t));
            float deep = Ease(AmWeaveAt, AmCupAt, t) * (1f - Ease(AmDrawAt + .2f, AmWarpAt, t));
            float beat = Mathf.Max(Mathf.Max(AmBeat(t, AmCupAt, .3f), AmBeat(t, AmReleaseAt, .35f)), Mathf.Max(AmBeat(t, AmPack1At), AmBeat(t, AmPack2At)));
            brightness = 1f - .24f * away - .06f * deep + .08f * beat;
            saturation = 1f - .16f * away - .06f * deep;
        }

        /// <summary>The lens takes the call in, the cup and the flick, small and quickly gone.</summary>
        private static Vector3 AmihanShake(float t)
        {
            Vector3 Kick(float at, float size, float hz)
            {
                float s = t - at;
                if (s < 0f || s > .3f) return Vector3.zero;
                float fall = Mathf.Exp(-s * 14f) * size;
                return new Vector3(Mathf.Sin(s * hz) * fall, Mathf.Sin(s * hz * 1.3f + 1.1f) * fall * .6f, 0f);
            }
            return Kick(AmAnswerAt, .025f, 70f) + Kick(AmCupAt, .03f, 64f) + Kick(AmFlickAt, .045f, 58f) + Kick(AmReleaseAt, .08f, 46f);
        }
    }
}
