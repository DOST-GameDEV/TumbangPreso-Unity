using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // AMIHAN, AIRBURST v5, 4.4 s (`docs/reports/amihan-presentation-2026-10-02/cutscene-v4-plan.md`, "v5"). The owner on v4
        // (2026-10-03): *"i like the expression she makes and her relaxing feel but the vfx and the shit thhat show up in the
        // back dont make sense"*, *"i also dont like the ranodm blue background saying the name of her skill"*.
        //
        // KEPT: her body, timing and faces (the run-in and skid, the read with the squint, the foot taps and shrug, the whistle,
        // the grin, the hands-on-hips wink). GONE: the stage that rose behind her (lime walls, timber houses, a clothesline with
        // a cloth on it), the AIRBURST card and its letterbox, and the gust that wound round the old cloth path. She performs on
        // the real court she cast from, among the real players, so everything behind her is the match.
        //
        // ONE CAUSE AND EFFECT FOR THE WIND: she whistles, it answers. Streaks race in from upwind and feed into her free palm,
        // where they wind into a small spinning ball of air. She keeps it on her hand (caught high, whirled over her head, held
        // as she points down the lane, swung back behind her head while more air is drawn in) and on the drive it unrolls into
        // the jet down the lane that the live fan takes over.
        //
        //   OPEN   0.00  a pan across the court onto her running in; the skid.
        //   READ   0.50  her free arm out feeling for the wind (squint); two foot taps, a shrug. Nothing comes.
        //   CALL   1.30  the two-finger whistle: the wind answers, streaks and court dust racing in at her.
        //   CATCH  1.85  the air wound into a ball on her palm and whirled over her head.
        //   POINT  2.50  down the lane, a grin, the ball on her hand.
        //   COMMIT 2.95  from behind her: wound up, the ball swung back over her head and fed, the drive at 3.85 (the release).
        //   HIT    3.85  the thrown players blown away, her in the middle, hands on hips, a wink.
        //
        // ⚠️ Every effect has a source and a direction: air travels from upwind to her hand, and from her hand down the lane.
        // No outline shapes, glints, rings, emblems, veils, cards or scenery. A warm grade, never darker. Every piece is posed
        // from the scene clock; nothing on `Update`. ⚠️ Reduced effects keeps every shape, stills the spin and halves the light.
        // =========================================================================================
        private const float AmSkidAt = .50f, AmReadAt = .74f, AmWhistleAt = 1.30f, AmArriveAt = 1.42f, AmFormAt = 1.57f,
            AmCatchAt = 1.85f, AmPointAt = 2.50f, AmWarpAt = 2.95f, AmWindAt = 3.10f, AmBraceAt = 3.20f,
            // THE RELEASE. Play resumes `AmihanStorm.CutsceneTail` (0.55 s) after it, on the hit, with no live delay.
            AmReleaseAt = 3.85f, AmFinishAt = 4.24f;

        // The shots (`tools/author_ultimate_intros.py` amihan()).
        private const int AmShotOpen = 0, AmShotCall = 2, AmShotCatch = 3, AmShotCommit = 5, AmShotHit = 6;

        // THE BALL: three open strands of air spun round her palm, which unroll into the jet on the release.
        private readonly LineRenderer[] _amGust = new LineRenderer[3];
        private readonly Vector3[] _amGustPoints = new Vector3[AmGustSamples];
        private readonly Vector3[] _amGustFrom = new Vector3[AmGustSamples];
        private Vector3[] _amJetCentre, _amJetAcross;
        private const int AmGustSamples = 28;
        // The air feeding it: streaks drawn each frame from upwind into her moving hand.
        private readonly LineRenderer[] _amArrive = new LineRenderer[8];
        private readonly LineRenderer[] _amGather = new LineRenderer[4];
        private readonly Vector3[] _amFeedPoints = new Vector3[AmFeedSamples];
        private const int AmFeedSamples = 14;
        private readonly List<WindVfx.Ribbon> _amSpeed = new List<WindVfx.Ribbon>();
        private WindVfx.Motif _amihanNear, _amDust, _amLift;
        private AmihanStormFan _amihanFan;
        private VoxelFace _amFace;
        private float _amCourt;

        // Where the wind comes from (her left, behind), and where the open pans across the court from.
        private static readonly Vector3 AmWindFrom = new Vector3(-8.5f, 1.0f, -5.5f);
        private static readonly Vector3 AmOpenFrom = new Vector3(-1.7f, 1.4f, -.8f);

        // THE ARRIVAL: streaks racing in at her hand on the whistle. Start (scene x, height, z), the side they swing round on
        // (+1 her right), unused, delay, width.
        private static readonly float[,] AmArriveRows =
        {
            { -9.0f, .30f, -6.2f,  .7f, .35f, .00f, .050f }, { -8.2f, 1.10f, -4.4f, -.6f, 1.20f, .04f, .060f },
            { -9.6f, .70f, -3.1f,  .9f, .80f, .08f, .045f }, { -7.4f, 1.60f, -6.8f, -.9f, 1.70f, .10f, .050f },
            { -8.8f, .20f, -2.2f, -.5f, .25f, .13f, .055f }, { -9.9f, 1.30f, -5.0f,  .6f, 1.45f, .16f, .045f },
            { -7.9f, .50f, -7.6f,  .8f, .55f, .19f, .050f }, { -9.3f, 1.90f, -3.8f, -.7f, 1.95f, .22f, .040f },
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
            // The court she stands on, not the classed ground under it (Bayan Plaza's tiles sit above it).
            _amCourt = Mathf.Clamp(AmihanStormFan.CourtUnder(_root.transform.position).y - _root.transform.position.y, 0f, .4f);

            _amJetCentre = new Vector3[AmGustSamples]; _amJetAcross = new Vector3[AmGustSamples];
            for (int k = 0; k < _amGust.Length; k++)
            {
                _amGust[k] = Line("AmihanGust" + k, AmGustSamples, .05f, k == 1 ? WindVfx.Core : AmSheetBody);
                _amGust[k].widthCurve = new AnimationCurve(new Keyframe(0f, .35f), new Keyframe(.25f, 1f), new Keyframe(.75f, .7f), new Keyframe(1f, 0f));
                _amGust[k].enabled = false;
            }
            // The feeding streaks: a thin tail, a full middle, a fine head where the air arrives.
            var taper = new AnimationCurve(new Keyframe(0f, .1f), new Keyframe(.55f, 1f), new Keyframe(1f, .3f));
            for (int i = 0; i < _amArrive.Length; i++)
            {
                _amArrive[i] = Line("AmihanArrive" + i, AmFeedSamples, AmArriveRows[i, 6], i % 3 == 0 ? WindVfx.Core : AmSheetBody);
                _amArrive[i].widthCurve = taper; _amArrive[i].enabled = false;
            }
            for (int i = 0; i < _amGather.Length; i++)
            {
                _amGather[i] = Line("AmihanGather" + i, AmFeedSamples, .05f, i % 2 == 0 ? WindVfx.Core : AmSheetBody);
                _amGather[i].widthCurve = taper; _amGather[i].enabled = false;
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

            // Dust and cotton: the court's dust running at her when the wind answers; cotton sucked into the ball as it forms;
            // and the near layer at the lens. Each Motif gets its own host (a Motif hands its tuft mesh to its parent's owner).
            _amDust = new WindVfx.Motif(AmHost("AmihanCourtDust"), 22, 17.1f, .3f);
            _amLift = new WindVfx.Motif(AmHost("AmihanDrawnCotton"), 14, 21.3f, .35f);
            _amihanNear = new WindVfx.Motif(AmHost("AmihanNearLayer"), 12, 13.7f, .5f);

            // The live fan itself, posed from this scene's clock (never its own Update) and only from the release on: at the
            // hand-back it is the live fan `AmihanStorm.CutsceneTail` after its release, on the same court.
            _amihanFan = AmihanStormFan.Build(_root.transform, _root.transform.position, _root.transform.forward,
                Core.AmihanRules.StormSurgeGatherSeconds);
            _amihanFan.enabled = false;

            // Her cutscene faces (squint, whistle, grin, wink) on the copied body's own head.
            _amFace = VoxelFace.Attach(_bodyRenderers);

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

        /// <summary>A beat's swell: up at once, gone by <paramref name="width"/>.</summary>
        private static float AmBeat(float t, float at, float width = .22f) => t < at ? 0f : Mathf.Clamp01(1f - (t - at) / width);

        private VoxelFace.Look AmLook(float t)
        {
            if (t >= AmFinishAt - .04f) return VoxelFace.Look.Wink;
            if (t >= AmReleaseAt - .02f) return VoxelFace.Look.Grin;
            if (t >= AmWindAt - .1f) return VoxelFace.Look.Squint;
            if (t >= AmCatchAt - .03f) return VoxelFace.Look.Grin;
            if (t >= AmWhistleAt - .05f && t < AmFormAt + .01f) return VoxelFace.Look.Whistle;
            if (t >= AmReadAt - .06f && t < AmWhistleAt - .05f) return VoxelFace.Look.Squint;
            return VoxelFace.Look.Rest;
        }

        /// <summary>Where the ball sits: just off her free palm.</summary>
        private Vector3 AmBallAt => FreePalm + new Vector3(0f, .06f, .04f);

        /// <summary>The ball's radius: nothing, wound up on the catch, fed bigger on the wind-up.</summary>
        private static float AmBallRadius(float t, bool calm)
        {
            float r = .2f * Ease(AmFormAt, AmCatchAt, t) + .14f * Ease(AmWindAt, AmReleaseAt - .05f, t);
            return r * (1f + (calm ? 0f : .06f * Mathf.Sin(t * 23f)));
        }

        private void SampleAmihan(float t)
        {
            bool calm = _reducedEffects;
            float phase = calm ? 0.0f : t * 3.0f;
            float light = calm ? .55f : 1f;
            float leave = 1 - Ease(Seconds - .30f, Seconds, t);
            _amFace?.Show(AmLook(t));
            var ball = AmBallAt;

            SampleAmihanGust(t, calm, ball);

            // THE ARRIVAL: the wind racing at her on the whistle, each streak swinging round on its side and into her hand.
            for (int i = 0; i < _amArrive.Length; i++)
            {
                float s = t - AmArriveAt - AmArriveRows[i, 5];
                var from = new Vector3(AmArriveRows[i, 0], AmArriveRows[i, 1] + _amCourt, AmArriveRows[i, 2]);
                var toHer = ball - from; toHer.y = 0f;
                var bend = Vector3.Lerp(from, ball, .6f) + Vector3.Cross(Vector3.up, toHer.normalized) * (AmArriveRows[i, 3] * 1.1f) + Vector3.up * .3f;
                AmFeed(_amArrive[i], from, bend, ball, Ease(0f, .32f, s), Ease(.14f, .46f, s), AmArriveRows[i, 6] * light);
            }

            // THE GATHER: more air drawn in from behind her into the ball over her head through the wind-up.
            for (int i = 0; i < _amGather.Length; i++)
            {
                float s = t - AmWindAt - .1f * i;
                float a = (150f + 22f * i) * Mathf.Deg2Rad;
                var from = new Vector3(Mathf.Sin(a) * 4.5f, .5f + .45f * i + _amCourt, Mathf.Cos(a) * 4.5f);
                var bend = Vector3.Lerp(from, ball, .5f) + Vector3.up * .6f;
                AmFeed(_amGather[i], from, bend, ball, Ease(0f, .3f, s), Ease(.16f, .5f, s), .05f * light);
            }

            // THE SPEED STREAKS race down the lane on the release.
            for (int i = 0; i < _amSpeed.Count; i++)
            {
                float s = t - AmReleaseAt - AmSpeedRows[i, 4];
                if (s < 0f || s > .5f) { _amSpeed[i].Set(0f, 0f); continue; }
                _amSpeed[i].Set(.75f * light * leave, phase * 3f, Ease(0f, .18f, s), Ease(.1f, .45f, s), Ease(.2f, .5f, s) * .7f);
            }

            // COURT DUST: skimming the tiles at her when the wind answers, passing her and on toward the lane.
            float dustOn = Ease(AmArriveAt, AmArriveAt + .1f, t) * (1f - Ease(AmCatchAt + .2f, AmPointAt, t));
            float dustFloor = _amCourt;
            _amDust.Step(Mathf.Clamp01((t - AmArriveAt) / 1.1f) * 1.3f, (calm ? .4f : .9f) * dustOn, (start, drift, u) =>
            {
                var from = AmWindFrom + new Vector3(start.x * 3f, 0, start.z * 3f);
                from.y = dustFloor + .05f + start.y * .5f;
                var to = new Vector3(start.x * 2.5f, dustFloor + .15f + start.y * .9f + drift.y * .4f, 3.5f + start.z * 2f);
                return Vector3.Lerp(from, to, u) + Vector3.up * Mathf.Sin(u * Mathf.PI) * (.3f + drift.x * .4f);
            }, .08f);

            // COTTON sucked into the ball as it winds up, spiralling in round her hand.
            float liftOn = Ease(AmFormAt, AmFormAt + .1f, t) * (1f - Ease(AmPointAt - .2f, AmPointAt, t));
            _amLift.Step(Mathf.Clamp01((t - AmFormAt) / .8f) * 1.4f, (calm ? .4f : .85f) * liftOn, (start, drift, u) =>
            {
                float a = start.x * 9f + u * 6f, r = Mathf.Lerp(.9f + start.z * .4f, .12f, u);
                return ball + new Vector3(Mathf.Sin(a) * r, (start.y - .5f) * .6f * (1f - u) + drift.y * .1f, Mathf.Cos(a) * r);
            }, .07f);

            // The near layer: cotton drifting across the lens on the wind's way, down the lane once she commits.
            AmLens(t, out int shot, out var eye, out var lensLook, out _);
            if (shot >= 0)
            {
                var nearEye = eye; var nearLook = lensLook;
                var toward = shot >= AmShotCommit ? new Vector3(0f, .8f, 9f) : new Vector3(1.5f, 1.2f, 2.5f);
                float nearOn = Ease(AmArriveAt - .1f, AmArriveAt + .15f, t);
                _amihanNear.Step(Mathf.Repeat(t / 1.2f, 1.0f), (calm ? .4f : .8f) * nearOn * leave, (start, drift, u) =>
                    Vector3.Lerp(Vector3.Lerp(nearEye, nearLook, .3f + start.y * .2f) + new Vector3(start.x, start.z * .6f, 0) * 1.1f, toward, u * .6f), .035f);
            }

            SampleAmihanLane(t);

            // THE RELEASE: the live fan's own clock, hidden until the drive and released ON it; at 4.4 s it is the live fan
            // `AmihanStorm.CutsceneTail` after its release.
            float gather = Core.AmihanRules.StormSurgeGatherSeconds;
            _amihanFan.StepTo(t < AmReleaseAt ? -AmihanStormFan.DrawOnSeconds : gather + (t - AmReleaseAt));
        }

        /// <summary>One feeding streak: the stretch of its curve between <paramref name="tail"/> and <paramref name="head"/>.</summary>
        private void AmFeed(LineRenderer line, Vector3 from, Vector3 bend, Vector3 to, float head, float tail, float width)
        {
            if (head - tail < .02f || width <= 0f) { line.enabled = false; return; }
            for (int i = 0; i < AmFeedSamples; i++)
                _amFeedPoints[i] = AmBezier(from, bend, to, Mathf.Lerp(tail, head, i / (AmFeedSamples - 1f)));
            line.SetPositions(_amFeedPoints);
            line.widthMultiplier = width;
            line.enabled = true;
        }

        // ------------------------------------------------------------------ the ball and the jet

        /// <summary>
        /// THE BALL on her palm from the moment the air reaches it, three open strands spinning round it in tilted planes and
        /// spiralling in; on the release it unrolls over 0.14 s into the jet cracked down the lane, and fades as the fan takes over.
        /// </summary>
        private void SampleAmihanGust(float t, bool calm, Vector3 ball)
        {
            float radius = AmBallRadius(t, calm);
            float fade = 1f - Ease(AmReleaseAt + .15f, AmReleaseAt + .45f, t);
            if (t < AmFormAt || fade <= .01f) { foreach (var line in _amGust) line.enabled = false; return; }
            bool released = t >= AmReleaseAt;
            if (released) AmJet(t, calm, ball);
            float unroll = released ? Ease(AmReleaseAt, AmReleaseAt + .14f, t) : 0f;
            float spin = calm ? 0f : t * 14f;
            for (int k = 0; k < _amGust.Length; k++)
            {
                var line = _amGust[k];
                var plane = Quaternion.Euler(-55f + 55f * k, 35f * k, 20f);
                for (int i = 0; i < AmGustSamples; i++)
                {
                    float s = i / (AmGustSamples - 1f);
                    float a = spin * (1f + .2f * k) + k * 2.1f + s * 5.0f;
                    float r = radius * (1f - .35f * s);
                    _amGustFrom[i] = ball + plane * new Vector3(Mathf.Cos(a) * r, (s - .5f) * radius * .3f, Mathf.Sin(a) * r);
                }
                if (released)
                {
                    for (int i = 0; i < AmGustSamples; i++)
                    {
                        float s = i / (AmGustSamples - 1f);
                        // The three strands wind round the jet's spine, so it reads as turning air, not a flat band.
                        float turn = spin * .65f + s * 7f + k * 2.1f;
                        var side = _amJetAcross[i] * (1.1f * Mathf.Cos(turn));
                        var lift = Vector3.Cross(_amJetAcross[i], (_amJetCentre[Mathf.Min(i + 1, AmGustSamples - 1)] - _amJetCentre[Mathf.Max(i - 1, 0)]).normalized) * Mathf.Sin(turn);
                        _amGustPoints[i] = Vector3.Lerp(_amGustFrom[i], _amJetCentre[i] + side + lift, unroll);
                    }
                    line.SetPositions(_amGustPoints);
                }
                else line.SetPositions(_amGustFrom);
                float grow = Ease(AmFormAt, AmFormAt + .12f, t);
                line.widthMultiplier = Mathf.Lerp(.05f - .01f * k, .07f - .015f * k, unroll) * grow * fade * (calm ? .7f : 1f);
                line.enabled = true;
            }
        }

        /// <summary>The jet: it unfurls forward off her palms, whipping, lifting, and races out down the lane.</summary>
        private void AmJet(float t, bool calm, Vector3 palm)
        {
            float wob = calm ? 0f : 1f;
            float u = Ease(AmReleaseAt, AmReleaseAt + .3f, t);
            float length = Mathf.Lerp(1.7f, 8.5f, u);
            for (int i = 0; i < AmGustSamples; i++)
            {
                float s = i / (AmGustSamples - 1f);
                float crack = wob * Mathf.Sin(s * 10f - (t - AmReleaseAt) * 30f) * (.1f + .5f * s) * (1f - .5f * u);
                _amJetCentre[i] = palm + Vector3.forward * (length * s) + Vector3.right * crack + Vector3.up * (1.0f * Mathf.Sin(s * Mathf.PI * .8f) * u);
                float twist = wob * (s * 3f - (t - AmReleaseAt) * 8f);
                _amJetAcross[i] = (Vector3.up * Mathf.Cos(twist) + Vector3.right * Mathf.Sin(twist)) * (.26f * (1f - .3f * s));
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
        /// THE COMPUTED SHOTS. OPEN pans fast across the court onto her as she skids in. CATCH orbits low round her as the ball
        /// winds up on her hand. HIT stands high in front of her right side, her finish in the middle, the thrown bodies blowing
        /// away down the lane past her left.
        /// </summary>
        private void AmihanFrame(int index, float t, ref Vector3 eye, ref Vector3 look, ref float fov)
        {
            if (index == AmShotOpen)
            {
                // The pan: held on the court upwind of her, then fast (0.14 s) onto her as she arrives.
                var her = new Vector3(0, 1.15f + _amCourt, 0);
                look = Vector3.Lerp(AmOpenFrom + Vector3.up * _amCourt, her, Ease(.30f, .44f, t));
            }
            else if (index == AmShotCatch)
            {
                float u = Ease(AmCatchAt, AmPointAt, t);
                float a = Mathf.Lerp(55f, -25f, u) * Mathf.Deg2Rad, r = Mathf.Lerp(3.4f, 3.1f, u);
                eye = new Vector3(Mathf.Sin(a) * r, .62f + .25f * u + _amCourt, Mathf.Cos(a) * r);
                look = new Vector3(0f, 1.35f + _amCourt, 0f);
                fov = 52f;
            }
            else if (index == AmShotHit)
            {
                // v4 r1 to r3: in front of her right side, 3 m up, 33 degrees round, so the thrown bodies blow away past her
                // and the fan radiates out under the lens; a slow push in lands on her wink.
                float u = Ease(AmReleaseAt + .1f, Seconds, t);
                float a = 33f * Mathf.Deg2Rad, r = Mathf.Lerp(5.4f, 4.8f, u);
                eye = new Vector3(Mathf.Sin(a) * r, 3.3f - .25f * u + _amCourt, Mathf.Cos(a) * r);
                look = new Vector3(-.2f, .95f + _amCourt, .7f);
                fov = Mathf.Lerp(46f, 42f, u);
            }
        }

        /// <summary>A warm afternoon: the light lifts a little warmer while she plays with the wind; the release flares.</summary>
        private void AmihanGrade(float t, out float brightness, out float saturation)
        {
            float warm = Ease(.1f, .4f, t) * (1f - Ease(AmWarpAt, AmWarpAt + .5f, t));
            float beat = Mathf.Max(AmBeat(t, AmReleaseAt, .35f), .6f * AmBeat(t, AmWhistleAt, .25f));
            brightness = 1f + .04f * warm + .08f * beat;
            saturation = 1f + .10f * warm;
        }

        /// <summary>The lens takes the skid, the wind's arrival, the catch and the release, small and quickly gone.</summary>
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
