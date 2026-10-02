using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // AMIHAN, AIRBURST, 3.6 s (`docs/reports/amihan-presentation-2026-10-02/beat-sheet.md`). The length
        // is shared phase timing on every peer and stays 3.6 s. This cutscene plays BEFORE the live 1.5 s
        // windup, which is everyone else's dodge window, so it NEVER shows the release (the previous version
        // sent a wall of wind down the court here and then the game released it again): one sentence, "she
        // pulls the street's wind into her cupped hands, packs it tight and aims it down one lane".
        //
        // The travelling thing is wind thread moving INWARD to her hands; the release (live) reverses it.
        // The charge lives in her hands, after the Miks reference: a cotton boll packed between the palms,
        // pulsing on the same two beats the live windup repeats. The kasikus her family weaves blooms and
        // tightens under her. In the last shot the stage steps back to the real court and the live fan
        // (`AmihanStormFan`, driven here at negative age) draws its edges out from her feet, so its final
        // frame IS the live fan at age 0 and every player sees which lane before play resumes.
        //
        // Rejected from the old cutscene: the ribbon vortex that lifted her (a Venti cage; she stays
        // grounded), the dark storm eye, the lens streaks, and the pre-release wall and ground rush.
        // =========================================================================================
        private int _amihanSky, _amihanDusk, _amihanGround;
        private readonly List<int> _viganHouses = new List<int>(20);
        private readonly List<WindVfx.Ribbon> _skyStreaks = new List<WindVfx.Ribbon>();
        private readonly List<WindVfx.Ribbon> _kasikus = new List<WindVfx.Ribbon>();
        private readonly List<WindVfx.Ribbon> _amihanThreads = new List<WindVfx.Ribbon>();
        private readonly List<float> _amihanThreadDelay = new List<float>();
        private int _cottonTuft, _amihanBollGlow;
        private WindVfx.Motif _amihanNear;
        private AmihanStormFan _amihanFan;

        /// <summary>Where her palms meet cupped at the right hip (the table's `cup` pose, measured on the glb).</summary>
        private static readonly Vector3 AmihanCup = new Vector3(.35f, .62f, .12f);

        private void BuildAmihan()
        {
            // The sky: the Monsoon look, cool green-white over a dusk band; the ground dark, so she
            // and her wind are the brightest things on the stage.
            _amihanGround = Wall("AmihanGround", 0, 0.9f, new Color(0.10f, 0.15f, 0.11f, 0.9f), emission: 0.1f);
            _amihanDusk = Wall("AmihanDusk", 0.9f, 2.4f, new Color(0.46f, 0.52f, 0.42f, 0.7f), emission: 0.25f);
            _amihanSky = Wall("AmihanSky", 2.4f, 12, new Color(0.70f, 0.80f, 0.72f, 0.55f), emission: 0.3f, cap: true);

            // Calle Crisologo: a row of two-storey stone-and-capiz houses on the far wall. Blocky
            // silhouettes only (Art_Direction section 0): a lower stone storey, a wider timber upper
            // storey with a strip of lit capiz windows, a hipped roof.
            var stone = new Color(0.16f, 0.14f, 0.12f, 1);
            var timber = new Color(0.12f, 0.10f, 0.09f, 1);
            var capiz = new Color(0.98f, 0.86f, 0.58f, 0.85f);
            for (int i = 0; i < 5; i++)
            {
                _viganHouses.Add(AddSolid("ViganStone" + i, VfxShapes.Prism(4, 1, 1), stone));
                _viganHouses.Add(AddSolid("ViganUpper" + i, VfxShapes.Prism(4, 1, 1), timber));
                _viganHouses.Add(AddSolid("ViganRoof" + i, VfxShapes.Prism(4, 1, .05f), timber));
                _viganHouses.Add(Add("ViganCapiz" + i, VfxShapes.Prism(4, 1, 1), capiz, 0.6f, plain: true));
            }

            // Far sky streaks: long thin ribbons on the stage wall's inside, crossing at three heights.
            for (int i = 0; i < 6; i++)
            {
                float h = 3.2f + i * 0.9f;
                var arc = WindVfx.Arc(7.2f - i * 0.15f, 120.0f, 30, h, 120.0f + i * 12.0f);
                _skyStreaks.Add(WindVfx.Build(_root.transform, "SkyStreak" + i, arc, 0.35f + (i % 2) * 0.15f,
                                              WindVfx.Standing, 3.0f, 0.18f, 60.0f + i));
            }

            // Wind threads pulled in from down the lane (+z) to her cupped hands: open curves with space
            // between them, never a closed vortex. Typed one by one: where each starts, how it bends.
            AmihanThread("ThreadHighLeft", new Vector3(-1.9f, 2.1f, 7.6f), new Vector3(-1.0f, .5f, 0), .00f, .055f);
            AmihanThread("ThreadLowRight", new Vector3(1.6f, .9f, 6.8f), new Vector3(.8f, -.2f, 0), .10f, .045f);
            AmihanThread("ThreadMidLeft", new Vector3(-.8f, 1.5f, 8.8f), new Vector3(-.6f, .2f, 0), .22f, .05f);
            AmihanThread("ThreadFarRight", new Vector3(2.4f, 1.8f, 9.2f), new Vector3(.9f, .4f, 0), .30f, .04f);
            AmihanThread("ThreadLowLeft", new Vector3(-2.2f, .7f, 5.6f), new Vector3(-.7f, -.1f, 0), .40f, .04f);
            AmihanThread("ThreadHighRight", new Vector3(1.1f, 2.4f, 7.0f), new Vector3(.5f, .7f, 0), .52f, .045f);
            AmihanThread("ThreadCentre", new Vector3(.2f, 1.2f, 10.0f), new Vector3(-.3f, .3f, 0), .62f, .05f);

            // The cotton boll packed between her palms, and the light inside it (her cream and wind green).
            _cottonTuft = Add("CottonBoll", VfxShapes.TwoSided(VfxShapes.Star(9, .6f, 3)), WindVfx.Cotton, .6f, plain: true);
            _amihanBollGlow = AddGlow("CottonBollGlow", new Color(WindVfx.Body.r, WindVfx.Body.g, WindVfx.Body.b, 1), falloff: 2.4f, core: .5f);

            // The kasikus: graduated diamonds radiating from her feet, drawn as ribbons on the road.
            for (int i = 0; i < 5; i++)
            {
                float r = 0.7f + i * 0.45f;
                var diamond = new List<Vector3>();
                for (int k = 0; k < 4; k++)
                    for (int j = 0; j < 6; j++)
                    {
                        float a0 = k * 90 * Mathf.Deg2Rad, a1 = (k + 1) * 90 * Mathf.Deg2Rad;
                        diamond.Add(Vector3.Lerp(new Vector3(Mathf.Sin(a0), 0, Mathf.Cos(a0)), new Vector3(Mathf.Sin(a1), 0, Mathf.Cos(a1)), j / 6.0f) * r + Vector3.up * .03f);
                    }
                diamond.Add(diamond[0]);
                _kasikus.Add(WindVfx.Build(_root.transform, "Kasikus" + i, diamond, 0.12f, WindVfx.Flat(diamond), 3.0f, 0.4f, 90.0f + i));
            }
            // The court she stands on, not the classed ground under it (Bayan Plaza's tiles sit above it).
            float court = AmihanStormFan.CourtUnder(_root.transform.position).y - _root.transform.position.y;
            foreach (var ring in _kasikus) ring.GameObject.transform.localPosition = Vector3.up * Mathf.Clamp(court, 0f, .4f);

            // A few cotton tufts and threads near the lens, drifting toward her: the near layer.
            // Its own host: a Motif gives its shared tuft mesh to its parent's single GeneratedMeshOwner, and
            // the old cutscene threw on every cast by putting three of them on the scene root.
            var nearHost = new GameObject("AmihanNearLayer").transform; nearHost.SetParent(_root.transform, false);
            _amihanNear = new WindVfx.Motif(nearHost, 8, 13.7f, 0.5f);

            // The live fan itself, posed from this scene's clock (never its own Update) so the handback
            // frame is the live fan's first frame on the same court.
            _amihanFan = AmihanStormFan.Build(_root.transform, _root.transform.position, _root.transform.forward,
                Core.AmihanRules.StormSurgeGatherSeconds);
            _amihanFan.enabled = false;
        }

        /// <summary>One wind thread from <paramref name="from"/> (down the lane) curving into her cupped hands.</summary>
        private void AmihanThread(string name, Vector3 from, Vector3 bend, float delay, float width)
        {
            var spine = new List<Vector3>(24);
            Vector3 control = Vector3.Lerp(from, AmihanCup, .5f) + bend;
            for (int k = 0; k < 24; k++)
            {
                float u = k / 23.0f;
                spine.Add(Vector3.Lerp(Vector3.Lerp(from, control, u), Vector3.Lerp(control, AmihanCup, u), u));
            }
            _amihanThreads.Add(WindVfx.Build(_root.transform, name, spine, width, WindVfx.Standing, 5.0f, 0.3f, 70.0f + _amihanThreads.Count));
            _amihanThreadDelay.Add(delay);
        }

        private void SampleAmihan(float t)
        {
            float phase = _reducedEffects ? 0.0f : t * 3.0f;
            // The stage stands up at once and steps back to the real court through the AIM shot.
            float stage = Ease(0, .3f, t) * (1 - Ease(2.55f, 3.05f, t));
            Tint(_amihanGround, stage); Tint(_amihanDusk, stage); Tint(_amihanSky, stage);
            for (int i = 0; i < 5; i++)
            {
                float angle = (180 - 58 + i * 29) * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Sin(angle) * 7.4f, 0, Mathf.Cos(angle) * 7.4f);
                var face = Quaternion.LookRotation(-at.normalized, Vector3.up);
                float width = 1.25f + (i % 2) * .25f;
                float rise = Ease(.05f + i * .04f, .35f + i * .04f, t) * (1 - Ease(2.55f + i * .04f, 3.0f + i * .03f, t));
                float on = rise > .01f ? 1 : 0;
                Place(_viganHouses[i * 4], at, new Vector3(width, 1.2f * rise, .7f), face, on);
                Place(_viganHouses[i * 4 + 1], at + Vector3.up * 1.2f * rise, new Vector3(width * 1.12f, 1.0f * rise, .82f), face, on);
                Place(_viganHouses[i * 4 + 2], at + Vector3.up * 2.2f * rise, new Vector3(width * 1.3f, .55f * rise, 1.0f), face * Quaternion.Euler(0, 45, 0), on);
                Place(_viganHouses[i * 4 + 3], at + face * Vector3.forward * .43f + Vector3.up * 1.62f * rise, new Vector3(width * .9f, .22f * rise, .05f), face, rise);
            }
            for (int i = 0; i < _skyStreaks.Count; i++)
            {
                float travel = Mathf.Repeat(t * (0.35f + i * 0.05f) + i * 0.17f, 1.4f);
                _skyStreaks[i].Set(stage * (0.35f + 0.1f * (i % 3)), phase * (1 + i * 0.1f),
                                   Mathf.Clamp01(travel), Mathf.Clamp01(travel - 0.45f), 0.2f);
            }

            // CALL to GATHER: the threads answer her call at 0.48, stream in and are packed into her hands
            // by the second pack beat; nothing is left of them once she aims.
            for (int i = 0; i < _amihanThreads.Count; i++)
            {
                float d = _amihanThreadDelay[i];
                float head = Ease(.42f + d, 1.45f + d * .8f, t);
                float tail = Ease(1.0f + d * .9f, 2.2f + d * .3f, t);
                float alpha = (.55f + .1f * (i % 3)) * (1 - Ease(2.15f, 2.4f, t));
                _amihanThreads[i].Set(alpha, -phase * 1.6f, head, tail * .98f, Ease(1.9f, 2.35f, t) * .7f);
            }

            // The boll: forms as the palms meet (1.42), pulses on the pack beats (1.72, 2.06), held small while
            // she aims and spent into the lane as the fan draws on.
            var cup = BothPalms;
            float boll = Ease(1.28f, 1.46f, t) * (1 - Ease(2.9f, 3.3f, t));
            float beat = Mathf.Clamp01(1 - Mathf.Abs(t - 1.72f) / .12f) + Mathf.Clamp01(1 - Mathf.Abs(t - 2.06f) / .12f);
            float size = .11f * boll * (1 + .35f * beat) * Mathf.Lerp(1, .7f, Ease(2.3f, 2.6f, t));
            Place(_cottonTuft, cup + new Vector3(0, .02f, .04f), Vector3.one * (size + .001f), Quaternion.Euler(70, t * 60, 0), boll);
            PlaceGlow(_amihanBollGlow, cup + new Vector3(0, .02f, .04f), Vector3.one * (.42f * boll * (1 + .5f * beat) + .001f),
                      Quaternion.identity, boll * (_reducedEffects ? .5f : .85f + .5f * beat));

            // The kasikus blooms out of the road as the palms meet and tightens on each pack beat; it gives
            // way to the live fan's own diamonds at her feet in the AIM shot.
            float pack = .1f * Ease(1.72f, 1.84f, t) + .1f * Ease(2.06f, 2.18f, t);
            for (int i = 0; i < _kasikus.Count; i++)
            {
                float bloom = Ease(1.36f + i * .05f, 1.62f + i * .05f, t);
                float grow = Mathf.Lerp(.4f, 1.0f, bloom) * (1 - pack);
                _kasikus[i].GameObject.transform.localScale = new Vector3(grow, 1, grow);
                _kasikus[i].GameObject.transform.localRotation = Quaternion.Euler(0, 45 + (i % 2 == 0 ? 1 : -1) * t * 14, 0);
                _kasikus[i].Set(bloom * (1 - i * .12f) * (1 - Ease(2.55f, 3.0f, t)), phase, 1, 0, Ease(2.6f, 3.0f, t) * .8f);
            }

            // The near layer: a few pieces drifting from the lens toward her, on the current shot's line.
            int shot = ShotIndexAt(t);
            if (shot >= 0)
            {
                _performance.Shot(shot, t, out var eye, out var look, out _);
                var toward = cup;
                _amihanNear.Step(Mathf.Repeat(t / 1.8f, 1.0f), (_reducedEffects ? .4f : .8f) * Ease(0, .25f, t) * (1 - Ease(3.2f, 3.55f, t)),
                    (start, drift, u) => Vector3.Lerp(Vector3.Lerp(eye, look, .22f + start.y * .2f) + new Vector3(start.x, start.z * .6f, 0) * .9f,
                                                      toward, u * .55f), .07f);
            }

            // AIM: the live fan draws on; at 3.6 s it is exactly the live fan at age 0.
            _amihanFan.StepTo(t - Seconds);
        }
    }
}
