using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // AMIHAN, AIRBURST v4, 4.4 s (`docs/reports/amihan-presentation-2026-10-02/cutscene-v4-plan.md`). The owner on v3.7:
        // *"thoroughly revise cutscene i want her personality to show in it as well as give it its own feel"*, *"it looks like
        // shapes are floating and not vfx"*. v3 was a solemn ritual (cupped hands, a woven emblem, a darkened world, glyphs in
        // the air). She is a bright, impatient street kid from Vigan who reads the wind off the abel hung out to air and commits
        // before the play is ready: *Reads the wind. Gets there first.*
        //
        // ONE SENTENCE: Amihan cannot wait: she whistles the monsoon down like calling a teammate, it arrives as her family's
        // abel cloth, and she lets it fly before anyone is ready. ONE COMPANION: the cloth off the line, which she catches,
        // whirls, swings back over her head and cracks down the lane, where the live fan's own cloth takes over.
        //
        //   OPEN   0.00  the abel on the line snapping in a warm afternoon street; a whip pan onto her running in; the skid.
        //   READ   0.50  a licked finger up (squint), the line goes slack; two foot taps, a shrug. Nothing comes.
        //   CALL   1.30  the two-finger whistle: the wind answers down the street, dust and cotton running at her; the cloth
        //                tears off the line (1.60) and flies to her free hand.
        //   CATCH  1.85  caught, whirled once round her with the street's wind wrapping it.
        //   CARD   2.50  AIRBURST (`HeroIntroductionScene.AmihanBurst.cs`): her point down the lane, a grin; blown away at 2.86.
        //   COMMIT 2.95  the real lane; wound up, the cloth swung back over her head, the drive at 3.85 (the release).
        //   HIT    3.85  from down the lane: the thrown players past the lens, her in the middle, hands on hips, a wink.
        //
        // ⚠️ The VFX language (the plan's rules): three materials only, abel cloth, air streaks that always travel, and cotton and
        // dust pushed by the air. No outline shapes, glints, rings, emblems or veils in the scene; her graphic language is the
        // card, in front of the lens. A warm grade, never darker. Every piece is posed from the scene clock; nothing on `Update`.
        // ⚠️ Reduced effects keeps every shape, stills the streak motion and halves the light.
        // =========================================================================================
        private const float AmSkidAt = .50f, AmReadAt = .74f, AmWhistleAt = 1.30f, AmArriveAt = 1.42f, AmTearAt = 1.60f,
            AmCatchAt = 1.85f, AmCardAt = 2.50f, AmCardGoneAt = 2.86f, AmWarpAt = 2.95f, AmWindAt = 3.10f, AmBraceAt = 3.20f,
            // THE RELEASE. Play resumes `AmihanStorm.CutsceneTail` (0.55 s) after it, on the hit, with no live delay.
            AmReleaseAt = 3.85f, AmFinishAt = 4.24f;

        // The shots (`tools/author_ultimate_intros.py` amihan()).
        private const int AmShotOpen = 0, AmShotCall = 2, AmShotCatch = 3, AmShotCard = 4, AmShotCommit = 5, AmShotHit = 6;

        private int _amihanSky, _amihanDusk, _amihanGround;
        private readonly List<int> _viganHouses = new List<int>(20);
        private int _amPostA, _amPostB, _amRope;
        private AbelCloth _amCloth;
        // THE GUST (v4 r5, owner on the woven bands: *"not a fan of the sash shit"*, *"refine her vfx in a diff way"*): what she
        // catches, whirls and cracks down the lane is the wind itself, three bright strands, not cloth.
        private readonly LineRenderer[] _amGust = new LineRenderer[3];
        private readonly Vector3[] _amGustPoints = new Vector3[AmClothSamples];
        private Vector3[] _amClothCentre, _amClothAcross, _amClothFrom, _amClothFromAcross;
        private const int AmClothSamples = 28;
        private readonly List<WindVfx.Ribbon> _amArrive = new List<WindVfx.Ribbon>();
        private readonly List<WindVfx.Ribbon> _amBreeze = new List<WindVfx.Ribbon>();
        private readonly List<WindVfx.Ribbon> _amWrap = new List<WindVfx.Ribbon>();
        private readonly List<WindVfx.Ribbon> _amGather = new List<WindVfx.Ribbon>();
        private readonly List<WindVfx.Ribbon> _amSpeed = new List<WindVfx.Ribbon>();
        private WindVfx.Motif _amihanNear, _amDust, _amLift;
        private AmihanStormFan _amihanFan;
        private VoxelFace _amFace;
        private float _amCourt;

        // The line in the street, to her left and behind her, upwind: the abel hung out to air.
        private static readonly Vector3 AmLineA = new Vector3(-2.5f, 2.15f, -.1f), AmLineB = new Vector3(-.9f, 2.15f, -1.45f);
        // Where the street's wind comes from (her left, behind), and the way it blows (past her, toward the lane).
        private static readonly Vector3 AmWindFrom = new Vector3(-8.5f, 1.0f, -5.5f);

        // THE ARRIVAL: streaks racing down the street at her on the whistle. Start (scene), the side they pass her on (+1 her
        // right), height at her, delay, width.
        private static readonly float[,] AmArriveRows =
        {
            { -9.0f, .30f, -6.2f,  .7f, .35f, .00f, .07f }, { -8.2f, 1.10f, -4.4f, -.6f, 1.20f, .04f, .09f },
            { -9.6f, .70f, -3.1f,  .9f, .80f, .08f, .06f }, { -7.4f, 1.60f, -6.8f, -.9f, 1.70f, .10f, .07f },
            { -8.8f, .20f, -2.2f, -.5f, .25f, .13f, .08f }, { -9.9f, 1.30f, -5.0f,  .6f, 1.45f, .16f, .06f },
            { -7.9f, .50f, -7.6f,  .8f, .55f, .19f, .07f }, { -9.3f, 1.90f, -3.8f, -.7f, 1.95f, .22f, .05f },
        };

        // THE SPEED STREAKS down the lane on the release: x, height, start z, end z, delay, width.
        private static readonly float[,] AmSpeedRows =
        {
            { -.45f, 1.85f, 1.0f,  9.5f, .00f, .06f }, {  .62f, 1.35f, 1.2f, 10.5f, .03f, .05f }, { -1.10f,  .70f, 1.6f,  8.5f, .05f, .05f },
            { 1.30f, 2.05f, 1.4f, 11.0f, .02f, .06f }, {  .15f,  .45f, 1.1f,  9.0f, .07f, .04f }, { -1.60f, 1.55f, 2.0f, 10.0f, .04f, .05f },
            {  .95f,  .85f, 1.8f,  8.0f, .06f, .04f }, { -.20f, 2.35f, 1.5f, 12.0f, .01f, .05f },
        };

        private static readonly Color AmSheetBody = new Color(0.78f, 0.96f, 0.72f, 1f);

        private void BuildAmihan()
        {
            // The stage: Calle Hangin on a warm afternoon. Warm cobbles, a golden band, a pale warm sky (never the dusk-dark of v3).
            _amihanGround = Wall("AmihanGround", 0, 0.9f, new Color(0.46f, 0.37f, 0.28f, 0.80f), emission: 0.20f);
            _amihanDusk = Wall("AmihanDusk", 0.9f, 2.4f, new Color(0.96f, 0.82f, 0.58f, 0.62f), emission: 0.40f);
            _amihanSky = Wall("AmihanSky", 2.4f, 12, new Color(0.80f, 0.90f, 0.96f, 0.55f), emission: 0.45f, cap: true);

            // Calle Crisologo: whitewashed lime plaster below, warm narra timber above, clay roofs, pearly capiz panes lit.
            var stone = new Color(0.86f, 0.80f, 0.68f, 1);
            var timber = new Color(0.44f, 0.29f, 0.18f, 1);
            var roof = new Color(0.66f, 0.33f, 0.22f, 1);
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

            // THE LINE: two narra posts and a rope, the abel hung over it.
            _amPostA = AddSolid("AmihanLinePostA", VfxShapes.Prism(4, 1, 1), timber);
            _amPostB = AddSolid("AmihanLinePostB", VfxShapes.Prism(4, 1, 1), timber);
            _amRope = AddSolid("AmihanLineRope", VfxShapes.Prism(4, 1, 1), new Color(0.78f, 0.70f, 0.55f, 1));
            _amCloth = new AbelCloth(_root.transform, "AmihanCompanionCloth", AmClothSamples, 1.5f);
            _amClothCentre = new Vector3[AmClothSamples]; _amClothAcross = new Vector3[AmClothSamples];
            _amClothFrom = new Vector3[AmClothSamples]; _amClothFromAcross = new Vector3[AmClothSamples];
            for (int k = 0; k < _amGust.Length; k++)
            {
                _amGust[k] = Line("AmihanGust" + k, AmClothSamples, .07f - .015f * k, k == 1 ? WindVfx.Core : AmSheetBody);
                _amGust[k].widthCurve = new AnimationCurve(new Keyframe(0f, .35f), new Keyframe(.25f, 1f), new Keyframe(.75f, .7f), new Keyframe(1f, 0f));
                _amGust[k].enabled = false;
            }

            // THE BREEZE before she calls: three thin streaks drifting across the line, so the cloth's flapping has a cause.
            for (int i = 0; i < 3; i++)
            {
                var from = new Vector3(-5.5f, 1.7f + .25f * i, -2.6f + .5f * i);
                var to = new Vector3(1.5f, 2.0f + .15f * i, 1.2f + .4f * i);
                var mid = Vector3.Lerp(from, to, .5f) + new Vector3(0, .35f - .2f * i, -.3f);
                var streak = WindVfx.Build(_root.transform, "AmihanBreeze" + i, AmCurve(from, mid, to, 16), .035f - .006f * i, WindVfx.Standing, 2.5f, .3f, 300f + i);
                AmBrightRim(streak, .6f);
                _amBreeze.Add(streak);
            }

            // THE ARRIVAL: each streak runs from the street, bends round her on its side, and leaves toward the lane.
            for (int i = 0; i < AmArriveRows.GetLength(0); i++)
            {
                var from = new Vector3(AmArriveRows[i, 0], AmArriveRows[i, 1] + _amCourt, AmArriveRows[i, 2]);
                float side = AmArriveRows[i, 3], h = AmArriveRows[i, 4] + _amCourt;
                var past = new Vector3(side * .75f, h, -.2f);
                var leave = new Vector3(side * 1.6f, h + .35f, 3.4f);
                var spine = new List<Vector3>(26);
                for (int k = 0; k < 26; k++)
                {
                    float u = k / 25f;
                    spine.Add(u < .62f ? AmBezier(from, Vector3.Lerp(from, past, .6f) + new Vector3(0, .2f, 0), past, u / .62f)
                                       : Vector3.Lerp(past, leave, (u - .62f) / .38f) + Vector3.up * .25f * Mathf.Sin((u - .62f) / .38f * Mathf.PI));
                }
                var streak = WindVfx.Build(_root.transform, "AmihanArrive" + i, spine, AmArriveRows[i, 6], WindVfx.Standing, 3.0f, .25f, 310f + i);
                AmBrightRim(streak, .58f);
                _amArrive.Add(streak);
            }

            // THE WRAP while she whirls the cloth: three open, rising strands round her (never a closed cage).
            for (int i = 0; i < 3; i++)
            {
                var spine = WindVfx.Helix(Vector3.up * (.3f + .25f * i + _amCourt), Vector3.up * (1.6f + .2f * i + _amCourt), .95f + .15f * i, .7f, 24, i * 120f, .8f);
                var strand = WindVfx.Build(_root.transform, "AmihanWrap" + i, spine, .06f - .01f * i, WindVfx.AroundAxis(spine, Vector3.up), 3.0f, .3f, 330f + i);
                AmBrightRim(strand, .6f);
                _amWrap.Add(strand);
            }

            // THE GATHER on the wind-up: four streaks drawn in from behind her into the cloth over her head.
            for (int i = 0; i < 4; i++)
            {
                float a = (150f + 22f * i) * Mathf.Deg2Rad;
                var from = new Vector3(Mathf.Sin(a) * 4.5f, .5f + .45f * i + _amCourt, Mathf.Cos(a) * 4.5f);
                var to = new Vector3(.2f, 2.0f + _amCourt, -.5f);
                var streak = WindVfx.Build(_root.transform, "AmihanGather" + i, AmCurve(from, Vector3.Lerp(from, to, .5f) + Vector3.up * .6f, to, 18),
                    .05f, WindVfx.Standing, 3.0f, .3f, 340f + i);
                AmBrightRim(streak, .6f);
                _amGather.Add(streak);
            }

            // THE SPEED STREAKS down the lane on the release.
            for (int i = 0; i < AmSpeedRows.GetLength(0); i++)
            {
                var spine = new List<Vector3>(12);
                for (int k = 0; k < 12; k++)
                    spine.Add(new Vector3(AmSpeedRows[i, 0], AmSpeedRows[i, 1], Mathf.Lerp(AmSpeedRows[i, 2], AmSpeedRows[i, 3], k / 11f)));
                var streak = WindVfx.Build(_root.transform, "AmihanSpeed" + i, spine, AmSpeedRows[i, 5], WindVfx.Standing, 2.0f, .3f, 210f + i);
                AmBrightRim(streak, .55f);
                _amSpeed.Add(streak);
            }

            // Dust and cotton: the street's dust running at her when the wind answers; cotton lifted round her as she whirls; and
            // the near layer at the lens. Each Motif gets its own host (a Motif hands its tuft mesh to its parent's single owner).
            _amDust = new WindVfx.Motif(AmHost("AmihanStreetDust"), 22, 17.1f, .3f);
            _amLift = new WindVfx.Motif(AmHost("AmihanLiftedCotton"), 14, 21.3f, .35f);
            _amihanNear = new WindVfx.Motif(AmHost("AmihanNearLayer"), 12, 13.7f, .5f);

            // The live fan itself, posed from this scene's clock (never its own Update) and only from the release on: at the
            // hand-back it is the live fan `AmihanStorm.CutsceneTail` after its release, on the same court.
            _amihanFan = AmihanStormFan.Build(_root.transform, _root.transform.position, _root.transform.forward,
                Core.AmihanRules.StormSurgeGatherSeconds);
            _amihanFan.enabled = false;

            // Her cutscene faces (squint, whistle, grin, wink) on the copied body's own head.
            _amFace = VoxelFace.Attach(_bodyRenderers);

            BuildAmihanBurst();
            BuildAmihanLane();
        }

        private Transform AmHost(string name)
        {
            var host = new GameObject(name).transform; host.SetParent(_root.transform, false);
            return host;
        }

        /// <summary>`WindRibbon`'s ink band turned to a bright rim: the swept-air look (bright edges, a middle you see through).</summary>
        private static void AmBrightRim(WindVfx.Ribbon ribbon, float rimFrom)
        {
            ribbon.Recolour(WindVfx.Body, AmSheetBody, WindVfx.Core);
            if (ribbon.Material != null) { ribbon.Material.SetFloat("_InkFrom", rimFrom); ribbon.Material.SetFloat("_InkAlpha", .9f); }
        }

        private static Vector3 AmBezier(Vector3 a, Vector3 b, Vector3 c, float u)
            => Vector3.Lerp(Vector3.Lerp(a, b, u), Vector3.Lerp(b, c, u), u);

        private static List<Vector3> AmCurve(Vector3 a, Vector3 b, Vector3 c, int points)
        {
            var spine = new List<Vector3>(points);
            for (int k = 0; k < points; k++) spine.Add(AmBezier(a, b, c, k / (points - 1f)));
            return spine;
        }

        /// <summary>A beat's swell: up at once, gone by <paramref name="width"/>.</summary>
        private static float AmBeat(float t, float at, float width = .22f) => t < at ? 0f : Mathf.Clamp01(1f - (t - at) / width);

        private VoxelFace.Look AmLook(float t)
        {
            if (t >= AmFinishAt - .04f) return VoxelFace.Look.Wink;
            if (t >= AmReleaseAt - .02f) return VoxelFace.Look.Grin;
            if (t >= AmWindAt - .1f) return VoxelFace.Look.Squint;
            if (t >= AmCardAt - .04f) return VoxelFace.Look.Grin;
            if (t >= AmCatchAt - .03f) return VoxelFace.Look.Grin;
            if (t >= AmWhistleAt - .05f && t < AmTearAt - .02f) return VoxelFace.Look.Whistle;
            if (t >= AmReadAt - .06f && t < AmWhistleAt - .05f) return VoxelFace.Look.Squint;
            return VoxelFace.Look.Rest;
        }

        private void SampleAmihan(float t)
        {
            bool calm = _reducedEffects;
            float phase = calm ? 0.0f : t * 3.0f;
            float light = calm ? .55f : 1f;
            float leave = 1 - Ease(Seconds - .30f, Seconds, t);
            _amFace?.Show(AmLook(t));

            // The street stands up at once and steps back to the real court as she commits to the lane.
            float stage = Ease(0, .25f, t) * (1 - Ease(AmWarpAt - .05f, AmWarpAt + .35f, t));
            Tint(_amihanGround, stage); Tint(_amihanDusk, stage); Tint(_amihanSky, stage);
            for (int i = 0; i < 5; i++)
            {
                float angle = (180 - 58 + i * 29) * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Sin(angle) * 7.4f, 0, Mathf.Cos(angle) * 7.4f);
                var face = Quaternion.LookRotation(-at.normalized, Vector3.up);
                float width = 1.25f + (i % 2) * .25f;
                float rise = Ease(.0f, .2f + i * .03f, t) * (1 - Ease(AmWarpAt - .05f + i * .03f, AmWarpAt + .35f + i * .03f, t));
                float on = rise > .01f ? 1 : 0;
                Place(_viganHouses[i * 4], at, new Vector3(width, 1.2f * rise, .7f), face, on);
                Place(_viganHouses[i * 4 + 1], at + Vector3.up * 1.2f * rise, new Vector3(width * 1.12f, 1.0f * rise, .82f), face, on);
                Place(_viganHouses[i * 4 + 2], at + Vector3.up * 2.2f * rise, new Vector3(width * 1.3f, .55f * rise, 1.0f), face * Quaternion.Euler(0, 45, 0), on);
                Place(_viganHouses[i * 4 + 3], at + face * Vector3.forward * .43f + Vector3.up * 1.62f * rise, new Vector3(width * .9f, .22f * rise, .05f), face, rise);
            }

            // THE LINE: posts and rope stand with the street.
            float lineOn = stage > .01f ? 1f : 0f;
            var court = Vector3.up * _amCourt;
            Place(_amPostA, new Vector3(AmLineA.x, 0, AmLineA.z) + court, new Vector3(.07f, AmLineA.y + .05f, .07f), Quaternion.identity, lineOn);
            Place(_amPostB, new Vector3(AmLineB.x, 0, AmLineB.z) + court, new Vector3(.07f, AmLineB.y + .05f, .07f), Quaternion.identity, lineOn);
            var rope = AmLineB - AmLineA;
            Place(_amRope, AmLineA + court, new Vector3(.015f, rope.magnitude, .015f), Quaternion.FromToRotation(Vector3.up, rope), lineOn);

            SampleAmihanCloth(t, calm);

            // THE BREEZE: drifting across the line before she calls, gone once she reads it (the wind drops).
            for (int i = 0; i < _amBreeze.Count; i++)
            {
                float s = (t + i * .17f) / .9f;
                float alpha = .28f * (1f - Ease(AmSkidAt + .1f, AmReadAt + .1f, t)) * light;
                _amBreeze[i].Set(alpha, -phase * 2f, Ease(0f, .5f, s), Ease(.35f, 1f, s), .4f);
            }

            // THE ARRIVAL: the street's wind racing at her on the whistle, each streak's head running ahead of its tail.
            for (int i = 0; i < _amArrive.Count; i++)
            {
                float s = t - AmArriveAt - AmArriveRows[i, 5];
                if (s < 0f || s > .9f) { _amArrive[i].Set(0f, 0f); continue; }
                _amArrive[i].Set((.62f + .08f * (i % 3)) * light * leave, -phase * 2.5f, Ease(0f, .38f, s), Ease(.22f, .85f, s), Ease(.45f, .9f, s) * .7f);
            }

            // THE WRAP: open strands rising round her as she whirls the cloth, turning with it.
            for (int i = 0; i < _amWrap.Count; i++)
            {
                float s = t - AmCatchAt - .05f * i;
                if (s < 0f || s > .75f) { _amWrap[i].Set(0f, 0f); continue; }
                _amWrap[i].GameObject.transform.localRotation = Quaternion.Euler(0f, calm ? 0f : -s * 420f, 0f);
                _amWrap[i].Set(.5f * light, -phase * 2f, Ease(0f, .3f, s), Ease(.25f, .75f, s), Ease(.4f, .75f, s) * .7f);
            }

            // THE GATHER: streaks drawn in from behind her into the cloth over her head through the wind-up.
            for (int i = 0; i < _amGather.Count; i++)
            {
                float s = t - AmWindAt - .08f * i;
                if (s < 0f || s > .75f) { _amGather[i].Set(0f, 0f); continue; }
                _amGather[i].Set(.5f * light, -phase * 2f, Ease(0f, .4f, s), Ease(.3f, .75f, s), Ease(.45f, .75f, s) * .7f);
            }

            // THE SPEED STREAKS race down the lane on the release.
            for (int i = 0; i < _amSpeed.Count; i++)
            {
                float s = t - AmReleaseAt - AmSpeedRows[i, 4];
                if (s < 0f || s > .5f) { _amSpeed[i].Set(0f, 0f); continue; }
                _amSpeed[i].Set(.75f * light * leave, phase * 3f, Ease(0f, .18f, s), Ease(.1f, .45f, s), Ease(.2f, .5f, s) * .7f);
            }

            // STREET DUST: skimming the cobbles at her when the wind answers, passing her and on toward the lane.
            float dustOn = Ease(AmArriveAt, AmArriveAt + .1f, t) * (1f - Ease(AmCatchAt + .2f, AmCardAt, t));
            float dustFloor = _amCourt;
            _amDust.Step(Mathf.Clamp01((t - AmArriveAt) / 1.1f) * 1.3f, (calm ? .4f : .9f) * dustOn, (start, drift, u) =>
            {
                var from = AmWindFrom + new Vector3(start.x * 3f, 0, start.z * 3f);
                from.y = dustFloor + .05f + start.y * .5f;
                var to = new Vector3(start.x * 2.5f, dustFloor + .15f + start.y * .9f + drift.y * .4f, 3.5f + start.z * 2f);
                return Vector3.Lerp(from, to, u) + Vector3.up * Mathf.Sin(u * Mathf.PI) * (.3f + drift.x * .4f);
            }, .08f);

            // COTTON lifted round her as she whirls the cloth.
            float liftOn = Ease(AmCatchAt, AmCatchAt + .1f, t) * (1f - Ease(AmCardAt - .1f, AmCardAt, t));
            _amLift.Step(Mathf.Clamp01((t - AmCatchAt) / .7f) * 1.4f, (calm ? .4f : .85f) * liftOn, (start, drift, u) =>
            {
                float a = start.x * 9f - u * 4f, r = .8f + start.z * .6f + u * .4f;
                return new Vector3(Mathf.Sin(a) * r, dustFloor + .3f + u * (1.6f + drift.y), Mathf.Cos(a) * r);
            }, .08f);

            // The near layer: cotton drifting across the lens on the wind's way, down the lane once she commits.
            AmLens(t, out int shot, out var eye, out var lensLook, out _);
            if (shot >= 0)
            {
                var nearEye = eye; var nearLook = lensLook;
                var toward = shot >= AmShotCommit ? new Vector3(0f, .8f, 9f) : new Vector3(1.5f, 1.2f, 2.5f);
                float nearOn = Ease(AmArriveAt - .1f, AmArriveAt + .15f, t) * (shot == AmShotCard ? 0f : 1f);
                _amihanNear.Step(Mathf.Repeat(t / 1.2f, 1.0f), (calm ? .4f : .8f) * nearOn * leave, (start, drift, u) =>
                    Vector3.Lerp(Vector3.Lerp(nearEye, nearLook, .3f + start.y * .2f) + new Vector3(start.x, start.z * .6f, 0) * 1.1f, toward, u * .6f), .035f);
            }

            SampleAmihanBurst(t, leave);
            SampleAmihanLane(t);

            // THE RELEASE: the live fan's own clock, hidden until the drive and released ON it; at 4.4 s it is the live fan
            // `AmihanStorm.CutsceneTail` after its release.
            float gather = Core.AmihanRules.StormSurgeGatherSeconds;
            _amihanFan.StepTo(t < AmReleaseAt ? -AmihanStormFan.DrawOnSeconds : gather + (t - AmReleaseAt));
        }

        // ------------------------------------------------------------------ the companion cloth

        /// <summary>
        /// THE COMPANION: the abel off the line, posed from the clock in one of six states (hanging, torn off and flying to her,
        /// whirled, held at the card, swung back over her head, cracked down the lane), each eased in from the one before over
        /// 0.14 s, so it never jumps. s 0 is the end in her hand (or the line), s 1 its free end.
        /// </summary>
        private void SampleAmihanCloth(float t, bool calm)
        {
            // THE CLOTH stays on the line, a prop of her street, snapping harder as her wind arrives and again on the release.
            AmClothState(0, t, calm, _amClothCentre, _amClothAcross);
            _amCloth.Pose(_amClothCentre, _amClothAcross, 0f);
            if (t >= AmWarpAt) _amCloth.Hide();

            // THE GUST: off the line to her hand, whirled, held, swung back and cracked down the lane, each state eased in
            // from the one before over 0.14 s.
            float[] starts = { 0f, AmTearAt, AmCatchAt, AmCardAt, AmWarpAt, AmReleaseAt };
            int state = 0;
            for (int i = starts.Length - 1; i >= 0; i--) if (t >= starts[i]) { state = i; break; }
            float fade = 1f - Ease(AmReleaseAt + .15f, AmReleaseAt + .45f, t);
            if (state == 0 || fade <= .01f) { foreach (var line in _amGust) line.enabled = false; return; }
            AmClothState(state, t, calm, _amClothCentre, _amClothAcross);
            float blend = state == 1 ? 1f : Ease(starts[state], starts[state] + .14f, t);
            if (blend < .999f)
            {
                AmClothState(state - 1, t, calm, _amClothFrom, _amClothFromAcross);
                for (int i = 0; i < AmClothSamples; i++)
                {
                    _amClothCentre[i] = Vector3.Lerp(_amClothFrom[i], _amClothCentre[i], blend);
                    _amClothAcross[i] = Vector3.Lerp(_amClothFromAcross[i], _amClothAcross[i], blend);
                }
            }
            float grow = state == 1 ? Ease(AmTearAt, AmTearAt + .12f, t) : 1f;
            for (int k = 0; k < _amGust.Length; k++)
            {
                var line = _amGust[k];
                for (int i = 0; i < AmClothSamples; i++)
                {
                    float s = i / (AmClothSamples - 1f);
                    // The three strands wind round the gust's spine, so it reads as turning air, not a flat band.
                    float turn = (calm ? 0f : t * 9f) + s * 7f + k * 2.1f;
                    var side = _amClothAcross[i] * (1.1f * Mathf.Cos(turn));
                    var lift = Vector3.Cross(_amClothAcross[i], (_amClothCentre[Mathf.Min(i + 1, AmClothSamples - 1)] - _amClothCentre[Mathf.Max(i - 1, 0)]).normalized) * Mathf.Sin(turn);
                    _amGustPoints[i] = _amClothCentre[i] + side + lift;
                }
                line.SetPositions(_amGustPoints);
                line.widthMultiplier = (.07f - .015f * k) * grow * fade * (calm ? .7f : 1f);
                line.enabled = true;
            }
        }

        private void AmClothState(int state, float t, bool calm, Vector3[] centre, Vector3[] across)
        {
            float wob = calm ? 0f : 1f;
            var palm = FreePalm;
            var wind = (new Vector3(0f, 0f, 0f) - AmWindFrom); wind.y = 0f; wind.Normalize();
            for (int i = 0; i < AmClothSamples; i++)
            {
                float s = i / (AmClothSamples - 1f);
                Vector3 c, a;
                switch (state)
                {
                    case 0:
                    {
                        // HANGING over the rope: snapping in the breeze, going slack as she reads it, lifting on the whistle.
                        var top = Vector3.Lerp(AmLineA, AmLineB, Mathf.Lerp(.18f, .82f, s)) + Vector3.up * _amCourt;
                        float breeze = 1f - Ease(AmSkidAt, AmReadAt + .15f, t);
                        float call = Ease(AmWhistleAt + .1f, AmTearAt, t) * (1f - .4f * Ease(AmCatchAt, AmCardAt, t));
                        float flap = wob * (breeze * .22f * Mathf.Sin(s * 9f - t * 15f) + call * .3f * Mathf.Sin(s * 11f - t * 24f));
                        var blow = wind * (.15f + .35f * breeze + .9f * call) + Vector3.up * (.35f * call);
                        var hang = Vector3.down * .5f + blow * .5f + wind * flap;
                        c = top + hang * .5f; a = hang * .5f;
                        break;
                    }
                    case 1:
                    {
                        // TORN OFF and flying to her: the near end races to her free hand, the rest streaming behind it downwind.
                        float u = Ease(AmTearAt, AmCatchAt, t);
                        var lead = Vector3.Lerp(Vector3.Lerp(AmLineA, AmLineB, .5f) + Vector3.up * _amCourt, palm, u) + Vector3.up * Mathf.Sin(u * Mathf.PI) * .6f;
                        c = lead - wind * (s * 1.6f) + Vector3.up * (.25f * Mathf.Sin(s * 5f - t * 18f) * wob);
                        a = Quaternion.AngleAxis((s * 140f + t * 300f) * wob, wind) * Vector3.up * .22f;
                        break;
                    }
                    case 2:
                    {
                        // WHIRLED round her once, rising off her hand in a widening spiral.
                        float spin = (t - AmCatchAt) / .62f * 360f * (calm ? .3f : 1f);
                        float r = .35f + 1.05f * s, ang = (spin - 165f * s) * Mathf.Deg2Rad;
                        var ring = new Vector3(Mathf.Sin(ang) * r, .2f * s + .12f * Mathf.Sin(s * 6f - t * 14f) * wob, Mathf.Cos(ang) * r);
                        var start = new Vector3(Mathf.Sin(spin * Mathf.Deg2Rad) * .35f, 0, Mathf.Cos(spin * Mathf.Deg2Rad) * .35f);
                        c = palm + ring - start;
                        a = Vector3.up * (.24f * (1f - .35f * s));
                        break;
                    }
                    case 3:
                    {
                        // AT THE CARD: hanging off her pointing hand, streaming back over her shoulder in the wind.
                        var back = new Vector3(.35f, .1f, -1f).normalized;
                        c = palm + back * (1.5f * s) + Vector3.down * (.35f * s * s) + Vector3.up * (.12f * Mathf.Sin(s * 8f - t * 16f) * wob);
                        a = Vector3.up * (.22f * (1f - .3f * s));
                        break;
                    }
                    case 4:
                    {
                        // SWUNG BACK over her head on the wind-up, rippling, held taut on the anticipation.
                        float up = Ease(AmWarpAt, AmWindAt + .2f, t);
                        var arc = Vector3.Lerp(new Vector3(.3f, .05f, -1f), new Vector3(.15f, .9f, -.55f), up).normalized;
                        c = palm + arc * (1.7f * s) + Vector3.down * (.5f * s * s * (1f - up)) + Vector3.right * (.15f * Mathf.Sin(s * 7f - t * 20f) * wob);
                        a = Vector3.Cross(arc, Vector3.right).normalized * (.24f * (1f - .3f * s));
                        break;
                    }
                    default:
                    {
                        // CRACKED down the lane: it unfurls forward off her palms, whipping, lifting, and races out.
                        float u = Ease(AmReleaseAt, AmReleaseAt + .3f, t);
                        float length = Mathf.Lerp(1.7f, 8.5f, u);
                        float crack = wob * Mathf.Sin(s * 10f - (t - AmReleaseAt) * 30f) * (.1f + .5f * s) * (1f - .5f * u);
                        c = palm + Vector3.forward * (length * s) + Vector3.right * crack + Vector3.up * (1.0f * Mathf.Sin(s * Mathf.PI * .8f) * u);
                        float twist = wob * (s * 3f - (t - AmReleaseAt) * 8f);
                        a = (Vector3.up * Mathf.Cos(twist) + Vector3.right * Mathf.Sin(twist)) * (.26f * (1f - .3f * s));
                        break;
                    }
                }
                centre[i] = c; across[i] = a;
            }
        }

        // ------------------------------------------------------------------ the lens

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
        /// THE COMPUTED SHOTS. OPEN whip-pans off the cloth onto her as she skids in. CATCH orbits low round her with the
        /// whirling cloth. HIT stands down the lane behind the nearest player she throws, looking back at her, so the thrown
        /// bodies pass the lens and she stands in the middle of it, hands on hips.
        /// </summary>
        private void AmihanFrame(int index, float t, ref Vector3 eye, ref Vector3 look, ref float fov)
        {
            if (index == AmShotOpen)
            {
                // The whip: held on the snapping cloth, then a fast pan (0.12 s) onto her as she arrives.
                var cloth = Vector3.Lerp(AmLineA, AmLineB, .5f) + new Vector3(0, -.3f + _amCourt, 0);
                var her = new Vector3(0, 1.15f + _amCourt, 0);
                look = Vector3.Lerp(cloth, her, Ease(.30f, .44f, t));
            }
            else if (index == AmShotCatch)
            {
                float u = Ease(AmCatchAt, AmCardAt, t);
                float a = Mathf.Lerp(55f, -25f, u) * Mathf.Deg2Rad, r = Mathf.Lerp(3.4f, 3.1f, u);
                eye = new Vector3(Mathf.Sin(a) * r, .62f + .25f * u + _amCourt, Mathf.Cos(a) * r);
                look = new Vector3(0f, 1.35f + _amCourt, 0f);
                fov = 52f;
            }
            else if (index == AmShotHit)
            {
                // v4 r1: from down the lane the nearest thrown player stood between the lens and her. In front of her right side
                // instead, her finish in the middle of the frame and the lane opening past her left; the bodies blow away
                // down it, and a slow push in lands on her wink.
                float u = Ease(AmReleaseAt + .1f, Seconds, t);
                // v4 r2: at eye height the fan's cloth swept through the lens and filled the frame; from 3 m up the cloth radiates
                // out under the lens round her instead.
                // v4 r3: at 40 degrees the fan's 50 degree sash flew through the lens; 33 lies between the sashes at 20 and 50.
                float a = 33f * Mathf.Deg2Rad, r = Mathf.Lerp(5.4f, 4.8f, u);
                eye = new Vector3(Mathf.Sin(a) * r, 3.3f - .25f * u + _amCourt, Mathf.Cos(a) * r);
                look = new Vector3(-.2f, .95f + _amCourt, .7f);
                fov = Mathf.Lerp(46f, 42f, u);
            }
        }

        /// <summary>A warm afternoon: the street lifts a little warmer and brighter while it stands; the release flares.</summary>
        private void AmihanGrade(float t, out float brightness, out float saturation)
        {
            float street = Ease(.1f, .4f, t) * (1f - Ease(AmWarpAt, AmWarpAt + .5f, t));
            float beat = Mathf.Max(AmBeat(t, AmReleaseAt, .35f), .6f * AmBeat(t, AmWhistleAt, .25f));
            brightness = 1f + .04f * street + .08f * beat;
            saturation = 1f + .10f * street;
        }

        /// <summary>The lens takes the skid, the whistle, the catch and the release, small and quickly gone.</summary>
        private static Vector3 AmihanShake(float t)
        {
            Vector3 Kick(float at, float size, float hz)
            {
                float s = t - at;
                if (s < 0f || s > .3f) return Vector3.zero;
                float fall = Mathf.Exp(-s * 14f) * size;
                return new Vector3(Mathf.Sin(s * hz) * fall, Mathf.Sin(s * hz * 1.3f + 1.1f) * fall * .6f, 0f);
            }
            return Kick(AmSkidAt, .02f, 70f) + Kick(AmArriveAt + .1f, .025f, 64f) + Kick(AmCatchAt, .02f, 64f) + Kick(AmReleaseAt, .08f, 46f);
        }
    }
}
