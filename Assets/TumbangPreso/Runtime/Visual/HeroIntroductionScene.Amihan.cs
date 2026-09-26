using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // AMIHAN, STORM SURGE, 3.6 s. `docs/reports/amihan-kit-2026-09-25/direction.md` § 7 and
        // `research.md`: WHO, INTENT, GATHER, RELEASE, the grammar every good ultimate cutscene the
        // research watched shares (Venti's close-up and darkened sky, Kazuha's layered ribbons and
        // leaves, Feixiao's eye beat and finale vortex).
        //
        // The stage is hers: Calle Crisologo at dusk behind her (Vigan's stone-and-capiz houses, a
        // row of blocky silhouettes on the far wall, never a textured backdrop), under the amihan
        // sky; the court recedes behind it (Sepak U's rule, `research.md` of 2026-09-24). Every
        // piece of wind is a `WindVfx` ribbon, the same family her live effects use, so the
        // cutscene and the ability are one visual language:
        //
        //   WHO      the wind arrives: far sky streaks start crossing, the street row stands up.
        //   INTENT   the close-up. A cotton tuft sits in her palm; on the blow (1.10 s) it bursts
        //            into threads and cotton, and bright speed streaks cross the lens.
        //   GATHER   the kasikus diamonds bloom out of the road under her, a vortex of ribbons and
        //            cotton spirals up round her and lifts her.
        //   RELEASE  the storm is drawn back to her right palm (a dark eye in bright rings), then
        //            a wall of wind leaves her palms down the court with the ground streaks rushing
        //            ahead of it; she lands braced, the first frame of the live 2.5 s gather.
        // =========================================================================================
        private int _amihanSky, _amihanDusk, _amihanGround;
        private readonly List<int> _viganHouses = new List<int>(20);
        private readonly List<WindVfx.Ribbon> _skyStreaks = new List<WindVfx.Ribbon>();
        private readonly List<WindVfx.Ribbon> _vortex = new List<WindVfx.Ribbon>();
        private readonly List<WindVfx.Ribbon> _kasikus = new List<WindVfx.Ribbon>();
        private readonly List<WindVfx.Ribbon> _lensStreaks = new List<WindVfx.Ribbon>();
        private readonly List<WindVfx.Ribbon> _eyeRings = new List<WindVfx.Ribbon>();
        private readonly List<WindVfx.Ribbon> _wall = new List<WindVfx.Ribbon>();
        private readonly List<WindVfx.Ribbon> _groundRush = new List<WindVfx.Ribbon>();
        private int _cottonTuft, _eyeCore;
        private Transform _vortexPivot, _eyePivot;
        private WindVfx.Motif _blowMotif, _vortexMotif, _wallMotif;

        private void BuildAmihan()
        {
            // The sky: the Monsoon look, cool green-white over a dusk band; the ground dark, so she
            // and her wind are the brightest things on the stage.
            _amihanGround = Wall("AmihanGround", 0, 0.9f, new Color(0.10f, 0.15f, 0.11f, 0.9f), emission: 0.1f);
            _amihanDusk = Wall("AmihanDusk", 0.9f, 2.4f, new Color(0.46f, 0.52f, 0.42f, 0.7f), emission: 0.25f);
            _amihanSky = Wall("AmihanSky", 2.4f, 12, new Color(0.70f, 0.80f, 0.72f, 0.55f), emission: 0.3f, cap: true);

            // Calle Crisologo: a row of two-storey stone-and-capiz houses on the far wall. Blocky
            // silhouettes only (Art_Direction § 0): a lower stone storey, a wider timber upper
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

            // The cotton tuft in her palm for the close-up.
            _cottonTuft = Add("CottonTuft", VfxShapes.TwoSided(VfxShapes.Star(9, .6f, 3)), WindVfx.Cotton, .6f, plain: true);

            // Speed streaks across the lens on the blow: short straight ribbons in front of her face.
            for (int i = 0; i < 5; i++)
            {
                var line = new List<Vector3>();
                for (int k = 0; k < 10; k++) line.Add(new Vector3(-1.2f + k * 0.26f, 1.2f + i * 0.12f, 0.55f + (i % 2) * 0.12f));
                _lensStreaks.Add(WindVfx.Build(_root.transform, "LensStreak" + i, line, 0.035f, WindVfx.Standing, 6.0f, 0.4f, 70.0f + i));
            }

            // The vortex: four strands on a pivot that turns.
            _vortexPivot = new GameObject("AmihanVortex").transform; _vortexPivot.SetParent(_root.transform, false);
            for (int i = 0; i < 4; i++)
            {
                var spine = WindVfx.Helix(Vector3.up * 0.05f, Vector3.up * (2.6f + i * 0.2f), 1.25f - i * 0.12f, 1.4f + i * 0.2f, 34, i * 90.0f, 0.55f);
                _vortex.Add(WindVfx.Build(_vortexPivot, "VortexStrand" + i, spine, 0.2f - i * 0.03f,
                                          WindVfx.AroundAxis(spine, Vector3.up), 5.0f, 0.2f, 80.0f + i));
            }

            // The kasikus: graduated diamonds radiating from her feet, drawn as ribbons on the road.
            for (int i = 0; i < 5; i++)
            {
                float r = 0.7f + i * 0.55f;
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

            // The eye: the storm drawn into her right palm, a dark core in bright rings.
            _eyePivot = new GameObject("AmihanEye").transform; _eyePivot.SetParent(_root.transform, false);
            _eyeCore = Add("StormEyeCore", VfxShapes.TwoSided(VfxShapes.Splat(16, .08f, 5)), new Color(.06f, .12f, .08f, .95f), .05f, plain: true);
            for (int i = 0; i < 3; i++)
            {
                var ring = WindVfx.Arc(0.22f + i * 0.1f, 300.0f, 22, 0.0f, i * 40.0f);
                _eyeRings.Add(WindVfx.Build(_eyePivot, "EyeRing" + i, ring, 0.05f, WindVfx.Standing, 4.0f, 0.35f, 100.0f + i));
            }

            // The wall that leaves: three standing arcs thrown down the court (+z), and ground
            // streaks rushing ahead of it.
            for (int i = 0; i < 3; i++)
            {
                var arc = WindVfx.Arc(1.0f, 80.0f, 26, 0.0f);
                for (int k = 0; k < arc.Length; k++) arc[k].y = 0.3f + (2.0f - i * 0.5f) * (1.0f - Mathf.Pow(Mathf.Abs(k / (float)(arc.Length - 1) * 2 - 1), 2) * 0.4f);
                _wall.Add(WindVfx.Build(_root.transform, "ReleaseWall" + i, arc, 0.7f - i * 0.15f, WindVfx.Standing, 4.0f + i, 0.22f, 110.0f + i));
            }
            for (int i = 0; i < 7; i++)
            {
                float a = (-30 + i * 10) * Mathf.Deg2Rad;
                var lane = new List<Vector3>();
                for (int k = 0; k < 20; k++) lane.Add(new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * (0.8f + k * 0.55f) + Vector3.up * .03f);
                _groundRush.Add(WindVfx.Build(_root.transform, "GroundRush" + i, lane, 0.3f, WindVfx.Flat(lane), 8.0f, 0.2f, 120.0f + i));
            }

            _blowMotif = new WindVfx.Motif(_root.transform, 14, 3.3f, 0.45f);
            _vortexMotif = new WindVfx.Motif(_root.transform, 26, 7.1f, 0.35f);
            _wallMotif = new WindVfx.Motif(_root.transform, 20, 11.9f, 0.4f);
        }

        private void SampleAmihan(float t)
        {
            float leave = 1 - Ease(Seconds - .45f, Seconds - .05f, t);
            float phase = _reducedEffects ? 0.0f : t * 4.0f;
            float stage = StagePresence(t, .3f);

            // WHO: the stage stands up in the first 0.3 s.
            Tint(_amihanGround, stage); Tint(_amihanDusk, stage); Tint(_amihanSky, stage);
            for (int i = 0; i < 5; i++)
            {
                float angle = (180 - 58 + i * 29) * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Sin(angle) * 7.4f, 0, Mathf.Cos(angle) * 7.4f);
                var face = Quaternion.LookRotation(-at.normalized, Vector3.up);
                float width = 1.25f + (i % 2) * .25f;
                float rise = Ease(.05f + i * .04f, .35f + i * .04f, t);
                float on = stage > .01f ? 1 : 0;
                Place(_viganHouses[i * 4], at + Vector3.up * .0f, new Vector3(width, 1.2f * rise, .7f), face, on);
                Place(_viganHouses[i * 4 + 1], at + Vector3.up * 1.2f * rise, new Vector3(width * 1.12f, 1.0f * rise, .82f), face, on);
                Place(_viganHouses[i * 4 + 2], at + Vector3.up * 2.2f * rise, new Vector3(width * 1.3f, .55f * rise, 1.0f), face * Quaternion.Euler(0, 45, 0), on);
                Place(_viganHouses[i * 4 + 3], at + face * Vector3.forward * .43f + Vector3.up * 1.62f * rise, new Vector3(width * .9f, .22f * rise, .05f), face, rise * leave);
            }
            for (int i = 0; i < _skyStreaks.Count; i++)
            {
                float travel = Mathf.Repeat(t * (0.35f + i * 0.05f) + i * 0.17f, 1.4f);
                _skyStreaks[i].Set(stage * (0.45f + 0.1f * (i % 3)) * leave, phase * (1 + i * 0.1f),
                                   Mathf.Clamp01(travel), Mathf.Clamp01(travel - 0.45f), 0.2f);
            }

            // INTENT: the tuft sits in her palm from 0.66 s and bursts on the blow at 1.10 s.
            float tuftOn = Ease(.66f, .74f, t) * (1 - Ease(1.08f, 1.14f, t));
            Place(_cottonTuft, RightPalm + new Vector3(0, .05f, .06f), Vector3.one * .07f, Quaternion.Euler(80, t * 40, 0), tuftOn);
            var palm = RightPalm;
            _blowMotif.Step(Mathf.Clamp01((t - 1.1f) / .55f), leave * (t >= 1.1f ? 1 : 0), (start, drift, u) =>
                palm + new Vector3(start.x * .5f + u * (.6f + drift.x), .08f + start.y * .2f + u * drift.y * .5f, .1f + u * (1.4f + drift.z)), .05f);
            for (int i = 0; i < _lensStreaks.Count; i++)
            {
                float s = Mathf.Clamp01((t - 1.1f - i * .03f) / .28f);
                _lensStreaks[i].Set((s > 0 && s < 1 ? 1 : 0) * .9f, phase * 2, s, Mathf.Max(0, s - .45f), 0.1f);
            }

            // GATHER: the kasikus blooms out of the road, the vortex spirals up and lifts her.
            for (int i = 0; i < _kasikus.Count; i++)
            {
                float bloom = Ease(1.35f + i * .1f, 1.7f + i * .1f, t);
                float grow = Mathf.Lerp(.3f, 1.0f, bloom) + Mathf.Max(0, t - 2.9f) * (1.5f + i * .5f);
                _kasikus[i].GameObject.transform.localScale = new Vector3(grow, 1, grow);
                _kasikus[i].GameObject.transform.localRotation = Quaternion.Euler(0, 45 + (i % 2 == 0 ? 1 : -1) * t * 18, 0);
                _kasikus[i].Set(bloom * leave * (1 - i * .1f), phase, 1, 0, Ease(3.1f, 3.5f, t) * .8f);
            }
            float vortex = Ease(1.4f, 1.75f, t) * (1 - Ease(2.5f, 2.85f, t));
            _vortexPivot.localRotation = Quaternion.Euler(0, t * 320.0f, 0);
            _vortexPivot.localScale = new Vector3(1, 1, 1) * Mathf.Lerp(1.0f, .45f, Ease(2.46f, 2.85f, t));
            _vortexPivot.localPosition = Vector3.up * LiftAt(t) * .5f;
            for (int i = 0; i < _vortex.Count; i++)
                _vortex[i].Set(vortex * (1 - i * .15f), phase * (1 + i * .2f), Ease(1.4f, 1.9f, t), Ease(2.3f, 2.8f, t) * .6f, Ease(2.5f, 2.85f, t));
            float lift = LiftAt(t);
            _vortexMotif.Step(Mathf.Clamp01((t - 1.4f) / 1.3f), vortex, (start, drift, u) =>
            {
                float a = start.x * 12 + u * 7.5f;
                float r = 1.2f - u * .5f + start.z * .3f;
                return new Vector3(Mathf.Cos(a) * r, .2f + u * (2.2f + drift.y) + lift, Mathf.Sin(a) * r);
            }, .08f);

            // LOAD: the eye gathers at her right palm.
            float eye = Ease(2.5f, 2.78f, t) * (1 - Ease(3.08f, 3.16f, t));
            _eyePivot.localPosition = RightPalm + new Vector3(.12f, 0, .15f);
            _eyePivot.localRotation = Quaternion.Euler(0, t * 420.0f, 0);
            Place(_eyeCore, RightPalm + new Vector3(.12f, 0, .15f), Vector3.one * (.16f * eye + .001f), Quaternion.Euler(90, 0, 0), eye * .95f);
            for (int i = 0; i < _eyeRings.Count; i++)
            {
                _eyeRings[i].GameObject.transform.localRotation = Quaternion.Euler(i * 35, i * 50, 0);
                _eyeRings[i].Set(eye, phase * 2, 1, 0, .1f);
            }

            // RELEASE (3.1): the wall leaves down the court, the ground streaks rushing ahead.
            float go = Mathf.Clamp01((t - 3.1f) / .5f);
            float sweep = 1 - Mathf.Pow(1 - go, 2.2f);
            for (int i = 0; i < _wall.Count; i++)
            {
                float r = Mathf.Lerp(.6f, 11.0f, Mathf.Clamp01(sweep - i * .07f));
                _wall[i].GameObject.transform.localScale = new Vector3(r, 1 + go * .4f, r);
                _wall[i].Set((t >= 3.1f ? 1 : 0) * (1 - i * .2f) * (1 - Ease(3.45f, 3.6f, t)), phase * 1.5f, 1, 0, go * .7f);
            }
            for (int i = 0; i < _groundRush.Count; i++)
            {
                float head = Mathf.Clamp01((t - 3.06f - i * .01f) / .3f);
                _groundRush[i].Set((t >= 3.06f ? .8f : 0) * leave, phase * 2.5f, head, Mathf.Max(0, head - .5f), go * .5f);
            }
            _wallMotif.Step(go, t >= 3.1f ? leave : 0, (start, drift, u) =>
            {
                float a = (start.x * 1.2f) * .7f;
                float r = .6f + u * (6 + drift.z * 4);
                return new Vector3(Mathf.Sin(a) * r, .4f + start.y * 1.8f + drift.y * u, Mathf.Cos(a) * r);
            }, .1f);
        }
    }
}
