using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // AMIHAN, AIRBURST v6, 5.6 s (`docs/reports/amihan-presentation-2026-10-02/cutscene-v4-plan.md`, "v6"). The owner on
        // v4: *"i like the expression she makes and her relaxing feel but the vfx and the shit thhat show up in the back dont
        // make sense"*, *"i also dont like the ranodm blue background"*; then *"thoroughly rethhink how to execute her
        // cutscenes as well as throoughly figure out hwo to make her vfx there better"*, *"give her time to breathe bcz
        // cutscene feels too fast"*, *"the part where she like lies down or smth should be where it slows down for a brief
        // moment"*.
        //
        // THE IDEA: she does not chase the wind. She waits for it, gets bored, whistles, and it comes to her. Twelve quick
        // actions in 4.4 s became six beats in 5.6 s, on the real court she cast from, among the real players.
        //
        //   BORED   0.00  easy, hip cocked; her arm out to feel the wind (squint): ONE LEAF falls straight down past her
        //                 palm. No wind. Two foot taps, a shrug.
        //   CALL    1.78  the two-finger whistle. The wind answers: curled streaks race in from upwind carrying leaves and
        //                 court dust, and wind into A LITTLE WHIRLWIND on her palm, which she holds up and admires (grin).
        //   LOOK    3.30  over her shoulder at the lens, grin, the whirlwind on her hand.
        //   WIND-UP 3.85  swung back over her head; more air and leaves drawn in from behind; it grows.
        //   DRIVE   4.55  the release. THE HANG: the scene clock slows to about a fifth while she hangs in the low lunge,
        //                 the whirlwind burst into arcs racing down the lane, its leaves and the thrown players suspended.
        //   FINISH  5.34  hands on hips, a wink. Hand-back at 5.6.
        //
        // ⚠️ THE STORY CLOCK (`AmStory`): before the release it is the scene clock; after it, it runs slow through the hang
        // and normal again for the finish, scaled so it has advanced exactly `AmihanStorm.CutsceneTail` at the hand-back.
        // Everything the game continues (the live fan, the thrown players) is posed on it, so play resumes where the
        // last frame drew. The body keys are authored on the scene clock (the hang is a held drive).
        //
        // ⚠️ THE VFX LANGUAGE: wind is shown by what it carries and by curled streaks that always travel from a source to a
        // destination: upwind to her palm, her palm down the lane. Three materials: air (curled streaks with a dark ink line
        // under a bright core, so they read on the light tiles), leaves (torn off the plaza's trees), court dust. Nothing
        // floats without a cause: no cards, emblems, outline shapes or scenery. A warm grade, never darker. Every piece is
        // posed from the clock; nothing on `Update`. ⚠️ Reduced effects keeps every shape, stills the spin and the curls'
        // motion, and halves the light.
        // =========================================================================================
        private const float AmReadAt = .78f, AmLeafFrom = .62f, AmLeafTo = 1.72f, AmWhistleAt = 1.78f, AmArriveAt = 1.92f,
            AmFormAt = 2.10f, AmCatchAt = 2.42f, AmLookAt = 3.30f, AmWindAt = 3.80f, AmBraceAt = 4.05f,
            // THE RELEASE. Play resumes after `AmRealTail` real seconds, `AmihanStorm.CutsceneTail` story seconds after it.
            AmReleaseAt = 4.55f, AmFinishAt = 5.34f;

        /// <summary>Real seconds from the release to the hand-back (5.6 - 4.55).</summary>
        public const float AmRealTail = 1.05f;
        // THE HANG: from 0.10 to 0.62 real seconds after the release, eased over 0.06 at each end, the clock at 18 per cent.
        private const float AmHangFrom = .10f, AmHangTo = .62f, AmHangEdge = .06f, AmHangSpeed = .18f;

        // The shots (`tools/author_ultimate_intros.py` amihan()).
        private const int AmShotBored = 0, AmShotCall = 1, AmShotCommit = 2, AmShotHang = 3, AmShotHit = 4;

        // THE AIR: each streak an ink line under a bright core, posed every frame along its curled path.
        private const int AmStreakSamples = 18;
        private readonly LineRenderer[] _amArriveCore = new LineRenderer[8], _amArriveInk = new LineRenderer[8];
        private readonly LineRenderer[] _amGatherCore = new LineRenderer[4], _amGatherInk = new LineRenderer[4];
        private readonly Vector3[] _amStreakPoints = new Vector3[AmStreakSamples];
        // THE WHIRLWIND: five open rings, narrow at her palm and wide at the top, and two strands spiralling up it.
        private const int AmRingSamples = 18, AmHelixSamples = 22;
        private readonly LineRenderer[] _amRing = new LineRenderer[5];
        private readonly LineRenderer[] _amHelix = new LineRenderer[2];
        private readonly Vector3[] _amRingPoints = new Vector3[AmRingSamples];
        private readonly Vector3[] _amHelixPoints = new Vector3[AmHelixSamples];
        // THE LEAVES: 0 the lone leaf; 1 to 14 carried in on the arrival; 15 to 22 drawn in on the wind-up.
        private const int AmLeaves = 23;
        private readonly int[] _amLeaf = new int[AmLeaves];
        private readonly Vector3[] _amLeafSeed = new Vector3[AmLeaves];
        private readonly List<WindVfx.Ribbon> _amSpeed = new List<WindVfx.Ribbon>();
        private WindVfx.Motif _amDust, _amSpin;
        private AmihanStormFan _amihanFan;
        private VoxelFace _amFace;
        private float _amCourt;

        // Where the wind comes from: her left, behind.
        private static readonly Vector3 AmWindFrom = new Vector3(-8.5f, 1.0f, -5.5f);

        // THE ARRIVAL: start (scene x, height, z), the side its curl turns to (+1 her right), curl radius, delay, width.
        private static readonly float[,] AmArriveRows =
        {
            { -9.0f, .30f, -6.2f,  1f, .22f, .00f, .060f }, { -8.2f, 1.10f, -4.4f, -1f, .28f, .05f, .070f },
            { -9.6f, .70f, -3.1f,  1f, .20f, .10f, .055f }, { -7.4f, 1.60f, -6.8f, -1f, .26f, .13f, .060f },
            { -8.8f, .20f, -2.2f, -1f, .24f, .17f, .065f }, { -9.9f, 1.30f, -5.0f,  1f, .30f, .21f, .055f },
            { -7.9f, .50f, -7.6f,  1f, .22f, .25f, .060f }, { -9.3f, 1.90f, -3.8f, -1f, .26f, .29f, .050f },
        };

        // THE SPEED STREAKS down the lane on the release: x, height, start z, end z, delay, width.
        private static readonly float[,] AmSpeedRows =
        {
            { -.45f, 1.85f, 1.0f,  9.5f, .00f, .06f }, {  .62f, 1.35f, 1.2f, 10.5f, .03f, .05f }, { -1.10f,  .70f, 1.6f,  8.5f, .05f, .05f },
            { 1.30f, 2.05f, 1.4f, 11.0f, .02f, .06f }, {  .15f,  .45f, 1.1f,  9.0f, .07f, .04f }, { -1.60f, 1.55f, 2.0f, 10.0f, .04f, .05f },
            {  .95f,  .85f, 1.8f,  8.0f, .06f, .04f }, { -.20f, 2.35f, 1.5f, 12.0f, .01f, .05f },
        };

        private static readonly Color AmSheetBody = new Color(0.78f, 0.96f, 0.72f, 1f);
        private static readonly Color AmInk = new Color(0.16f, 0.36f, 0.20f, .55f);
        private static readonly Color[] AmLeafColours =
        {
            new Color(0.33f, 0.58f, 0.24f, 1f), new Color(0.47f, 0.68f, 0.26f, 1f), new Color(0.26f, 0.48f, 0.22f, 1f),
            new Color(0.80f, 0.70f, 0.30f, 1f),
        };

        private void BuildAmihan()
        {
            // The court she stands on, not the classed ground under it (Bayan Plaza's tiles sit above it).
            _amCourt = Mathf.Clamp(AmihanStormFan.CourtUnder(_root.transform.position).y - _root.transform.position.y, 0f, .4f);

            var taper = new AnimationCurve(new Keyframe(0f, .1f), new Keyframe(.5f, 1f), new Keyframe(1f, .35f));
            for (int i = 0; i < _amArriveCore.Length; i++)
                AmStreakPair("AmihanArrive" + i, i % 3 == 0 ? WindVfx.Core : AmSheetBody, taper, out _amArriveCore[i], out _amArriveInk[i]);
            for (int i = 0; i < _amGatherCore.Length; i++)
                AmStreakPair("AmihanGather" + i, i % 2 == 0 ? WindVfx.Core : AmSheetBody, taper, out _amGatherCore[i], out _amGatherInk[i]);

            var ring = new AnimationCurve(new Keyframe(0f, .2f), new Keyframe(.3f, 1f), new Keyframe(.8f, 1f), new Keyframe(1f, .1f));
            for (int j = 0; j < _amRing.Length; j++)
            {
                _amRing[j] = Line("AmihanWhirlRing" + j, AmRingSamples, .02f, j % 2 == 0 ? WindVfx.Core : AmSheetBody);
                _amRing[j].widthCurve = ring; _amRing[j].sortingOrder = 2; _amRing[j].enabled = false;
            }
            for (int k = 0; k < _amHelix.Length; k++)
            {
                _amHelix[k] = Line("AmihanWhirlStrand" + k, AmHelixSamples, .02f, WindVfx.Core);
                _amHelix[k].widthCurve = taper; _amHelix[k].sortingOrder = 2; _amHelix[k].enabled = false;
            }

            // THE LEAVES: thin lit slabs, each with its own seed for its path, its tumble and its colour.
            var leaf = VfxShapes.Prism(4, 1, 1);
            for (int i = 0; i < AmLeaves; i++)
            {
                _amLeafSeed[i] = new Vector3(AmHash(i * 3 + 1), AmHash(i * 3 + 2), AmHash(i * 3 + 3));
                _amLeaf[i] = AddSolid("AmihanLeaf" + i, leaf, AmLeafColours[i % AmLeafColours.Length]);
                Place(_amLeaf[i], Vector3.zero, Vector3.one * .001f, Quaternion.identity, 0f);
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

            // Court dust running at her when the wind answers, and dust spun up inside the whirlwind. Each Motif gets its own
            // host (a Motif hands its tuft mesh to its parent's single owner).
            _amDust = new WindVfx.Motif(AmHost("AmihanCourtDust"), 22, 17.1f, .3f);
            _amSpin = new WindVfx.Motif(AmHost("AmihanWhirlDust"), 12, 21.3f, .2f);

            // The live fan itself, posed from the story clock (never its own Update) and only from the release on: at the
            // hand-back it is the live fan `AmihanStorm.CutsceneTail` after its release, on the same court.
            _amihanFan = AmihanStormFan.Build(_root.transform, _root.transform.position, _root.transform.forward,
                Core.AmihanRules.StormSurgeGatherSeconds);
            _amihanFan.enabled = false;

            // Her cutscene faces (squint, whistle, grin, wink) on the copied body's own head.
            _amFace = VoxelFace.Attach(_bodyRenderers);

            BuildAmihanLane();
        }

        private void AmStreakPair(string name, Color core, AnimationCurve taper, out LineRenderer bright, out LineRenderer ink)
        {
            ink = Line(name + "Ink", AmStreakSamples, .1f, AmInk);
            ink.widthCurve = taper; ink.sortingOrder = 0; ink.enabled = false;
            bright = Line(name, AmStreakSamples, .06f, core);
            bright.widthCurve = taper; bright.sortingOrder = 1; bright.enabled = false;
        }

        private Transform AmHost(string name)
        {
            var host = new GameObject(name).transform; host.SetParent(_root.transform, false);
            return host;
        }

        private static float AmHash(int n) => Mathf.Repeat(Mathf.Sin(n * 12.9898f + 4.1f) * 43758.5453f, 1f);

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

        // ------------------------------------------------------------------ the story clock

        private static float AmHangRaw(float real)
        {
            // The clock's speed, integrated: 1 everywhere but the hang, AmHangSpeed inside it, eased at its edges.
            const int steps = 48;
            float s = Mathf.Clamp(real, 0f, AmRealTail), h = s / steps, sum = 0f;
            for (int i = 0; i < steps; i++)
            {
                float x = (i + .5f) * h;
                float inside = Ease(AmHangFrom - AmHangEdge, AmHangFrom, x) * (1f - Ease(AmHangTo, AmHangTo + AmHangEdge, x));
                sum += (1f - (1f - AmHangSpeed) * inside) * h;
            }
            return sum + Mathf.Max(0f, real - AmRealTail);
        }

        /// <summary>
        /// Story seconds after the release for <paramref name="real"/> scene seconds after it: slowed through the hang, and
        /// scaled so the hand-back (<see cref="AmRealTail"/>) lands exactly on <c>AmihanStorm.CutsceneTail</c>.
        /// </summary>
        public static float AmihanStoryAfterRelease(float real)
            => real <= 0f ? real : AmHangRaw(real) * (Abilities.AmihanStorm.CutsceneTail / AmHangRaw(AmRealTail));

        private static float AmStory(float t) => t <= AmReleaseAt ? t : AmReleaseAt + AmihanStoryAfterRelease(t - AmReleaseAt);

        private VoxelFace.Look AmLook(float t)
        {
            if (t >= AmFinishAt - .04f) return VoxelFace.Look.Wink;
            if (t >= AmReleaseAt - .02f) return VoxelFace.Look.Grin;
            if (t >= AmWindAt - .05f) return VoxelFace.Look.Squint;
            if (t >= AmCatchAt - .2f) return VoxelFace.Look.Grin;
            if (t >= AmWhistleAt - .05f) return VoxelFace.Look.Whistle;
            if (t >= AmReadAt - .08f && t < 1.62f) return VoxelFace.Look.Squint;
            return VoxelFace.Look.Rest;
        }

        // ------------------------------------------------------------------ the whirlwind's shape

        /// <summary>Where the whirlwind stands: on her free palm.</summary>
        private Vector3 AmBallAt => FreePalm + new Vector3(0f, .03f, .02f);

        /// <summary>Its size: nothing, wound up as the air arrives, fed bigger on the wind-up.</summary>
        private static float AmWhirlSize(float t)
            => Ease(AmFormAt - .1f, AmCatchAt, t) + .55f * Ease(AmWindAt, AmReleaseAt - .05f, t);

        // v6 r1: at 0.46 m with 2 cm lines it was lost against her coat; taller, wider and drawn bolder.
        private static float AmWhirlHeight(float size) => .62f * size;

        /// <summary>Its radius at height fraction <paramref name="v"/> (0 at her palm, 1 at the top).</summary>
        private static float AmWhirlRadius(float size, float v) => (.045f + .27f * Mathf.Pow(Mathf.Clamp01(v), 1.3f)) * size;

        /// <summary>A point inside the whirlwind: height fraction, angle, how far out (1 its wall).</summary>
        private Vector3 AmWhirlPoint(Vector3 ball, float size, float v, float angle, float out_, float t, bool calm)
        {
            float r = AmWhirlRadius(size, v) * out_;
            // The top wanders: a dust devil is never a rigid cone.
            float sway = calm ? 0f : .03f * size * v;
            var lean = new Vector3(Mathf.Sin(t * 7f) * sway, 0f, Mathf.Cos(t * 6f) * sway);
            return ball + lean + new Vector3(Mathf.Cos(angle) * r, v * AmWhirlHeight(size), Mathf.Sin(angle) * r);
        }

        // ------------------------------------------------------------------ the sample

        private void SampleAmihan(float t)
        {
            bool calm = _reducedEffects;
            float story = AmStory(t);
            float light = calm ? .55f : 1f;
            float leave = 1 - Ease(Seconds - .30f, Seconds, t);
            _amFace?.Show(AmLook(t));
            var ball = AmBallAt;
            float size = AmWhirlSize(t);
            float sinceRelease = story - AmReleaseAt;

            SampleAmihanWhirl(t, story, calm, ball, size);

            // THE ARRIVAL: the wind answering the whistle, each streak curling once on its way and into her palm.
            for (int i = 0; i < _amArriveCore.Length; i++)
            {
                float s = t - AmArriveAt - AmArriveRows[i, 5];
                AmArrivePath(i, ball, out var from, out var bend, out var side);
                AmStreak(_amArriveCore[i], _amArriveInk[i], from, bend, ball, side, AmArriveRows[i, 4],
                    Ease(0f, .42f, s), Ease(.16f, .56f, s), AmArriveRows[i, 6] * light);
            }

            // THE GATHER: more air drawn in from behind her into the whirlwind over her head through the wind-up.
            for (int i = 0; i < _amGatherCore.Length; i++)
            {
                float s = t - AmWindAt - .1f * i;
                AmGatherPath(i, ball, out var from, out var bend, out var side);
                AmStreak(_amGatherCore[i], _amGatherInk[i], from, bend, ball, side, .22f, Ease(0f, .36f, s), Ease(.16f, .5f, s), .055f * light);
            }

            SampleAmihanLeaves(t, story, calm, ball, size);

            // THE SPEED STREAKS race down the lane on the release (story time: they hang with everything else).
            for (int i = 0; i < _amSpeed.Count; i++)
            {
                float s = sinceRelease - AmSpeedRows[i, 4] * .4f;
                if (s < 0f || s > .5f) { _amSpeed[i].Set(0f, 0f); continue; }
                _amSpeed[i].Set(.75f * light * leave, calm ? 0f : story * 9f, Ease(0f, .18f, s), Ease(.1f, .45f, s), Ease(.2f, .5f, s) * .7f);
            }

            // COURT DUST: skimming the tiles at her when the wind answers, passing her and on toward the lane.
            float dustOn = Ease(AmArriveAt, AmArriveAt + .1f, t) * (1f - Ease(AmCatchAt, AmCatchAt + .4f, t));
            float dustFloor = _amCourt;
            _amDust.Step(Mathf.Clamp01((t - AmArriveAt) / 1.2f) * 1.3f, (calm ? .4f : .9f) * dustOn, (start, drift, u) =>
            {
                var from = AmWindFrom + new Vector3(start.x * 3f, 0, start.z * 3f);
                from.y = dustFloor + .05f + start.y * .5f;
                var to = new Vector3(start.x * 2.5f, dustFloor + .15f + start.y * .9f + drift.y * .4f, 3.5f + start.z * 2f);
                return Vector3.Lerp(from, to, u) + Vector3.up * Mathf.Sin(u * Mathf.PI) * (.3f + drift.x * .4f);
            }, .08f);

            // DUST spun up inside the whirlwind while it stands on her palm.
            float spinOn = Ease(AmFormAt, AmCatchAt, t) * (1f - Ease(AmReleaseAt - .02f, AmReleaseAt + .05f, t));
            _amSpin.Step(Mathf.Repeat(t * .9f, 1f), (calm ? .4f : .8f) * spinOn, (start, drift, u) =>
                AmWhirlPoint(ball, size, u, start.x * 9f + t * (calm ? 0f : 11f), .7f + .3f * start.z, t, calm), .035f * Mathf.Max(.6f, size));

            SampleAmihanLane(t, story);

            // THE RELEASE: the live fan's own clock on the story clock, hidden until the drive and released ON it; at the
            // hand-back it is the live fan `AmihanStorm.CutsceneTail` after its release.
            float gather = Core.AmihanRules.StormSurgeGatherSeconds;
            _amihanFan.StepTo(t < AmReleaseAt ? -AmihanStormFan.DrawOnSeconds : gather + sinceRelease);
        }

        private void AmArrivePath(int i, Vector3 ball, out Vector3 from, out Vector3 bend, out Vector3 side)
        {
            from = new Vector3(AmArriveRows[i, 0], AmArriveRows[i, 1] + _amCourt, AmArriveRows[i, 2]);
            var toHer = ball - from; toHer.y = 0f;
            side = Vector3.Cross(Vector3.up, toHer.normalized) * AmArriveRows[i, 3];
            bend = Vector3.Lerp(from, ball, .55f) + side * 1.1f + Vector3.up * .35f;
        }

        private void AmGatherPath(int i, Vector3 ball, out Vector3 from, out Vector3 bend, out Vector3 side)
        {
            float a = (150f + 22f * i) * Mathf.Deg2Rad;
            from = new Vector3(Mathf.Sin(a) * 4.5f, .5f + .45f * i + _amCourt, Mathf.Cos(a) * 4.5f);
            var toHer = ball - from; toHer.y = 0f;
            side = Vector3.Cross(Vector3.up, toHer.normalized) * (i % 2 == 0 ? 1f : -1f);
            bend = Vector3.Lerp(from, ball, .5f) + Vector3.up * .6f;
        }

        /// <summary>
        /// A streak's path at <paramref name="u"/>: a curve from its source to her palm with ONE CURL in it (a full loop
        /// between 0.48 and 0.72 of the way, turning to <paramref name="side"/> and up), the classic drawn gust.
        /// </summary>
        private static Vector3 AmPath(Vector3 from, Vector3 bend, Vector3 to, Vector3 side, float curl, float u)
        {
            var p = AmBezier(from, bend, to, u);
            float k = Mathf.Clamp01((u - .48f) / .24f);
            if (k <= 0f || k >= 1f) return p;
            float a = k * k * (3f - 2f * k) * Mathf.PI * 2f;
            return p + (side * Mathf.Sin(a) + Vector3.up * (1f - Mathf.Cos(a))) * curl;
        }

        /// <summary>One streak: its ink line and bright core along the stretch of its path between tail and head.</summary>
        private void AmStreak(LineRenderer core, LineRenderer ink, Vector3 from, Vector3 bend, Vector3 to, Vector3 side, float curl,
            float head, float tail, float width)
        {
            if (head - tail < .02f || width <= 0f) { core.enabled = false; ink.enabled = false; return; }
            for (int i = 0; i < AmStreakSamples; i++)
                _amStreakPoints[i] = AmPath(from, bend, to, side, curl, Mathf.Lerp(tail, head, i / (AmStreakSamples - 1f)));
            core.SetPositions(_amStreakPoints); ink.SetPositions(_amStreakPoints);
            core.widthMultiplier = width; ink.widthMultiplier = width * 1.9f;
            core.enabled = true; ink.enabled = true;
        }

        // ------------------------------------------------------------------ the whirlwind

        /// <summary>
        /// THE WHIRLWIND on her palm from the moment the air reaches it until the drive; on the drive its rings burst into
        /// arcs racing down the lane (story time, so they hang in the slow-motion) while the live fan takes over.
        /// </summary>
        private void SampleAmihanWhirl(float t, float story, bool calm, Vector3 ball, float size)
        {
            float since = story - AmReleaseAt;
            bool standing = t >= AmFormAt - .1f && t < AmReleaseAt && size > .02f;
            for (int k = 0; k < _amHelix.Length; k++)
            {
                if (!standing) { _amHelix[k].enabled = false; continue; }
                float spin = calm ? 0f : t * 13f;
                for (int i = 0; i < AmHelixSamples; i++)
                {
                    float v = i / (AmHelixSamples - 1f);
                    _amHelixPoints[i] = AmWhirlPoint(ball, size, v, spin + k * Mathf.PI + v * Mathf.PI * 4f, 1.02f, t, calm);
                }
                _amHelix[k].SetPositions(_amHelixPoints);
                _amHelix[k].widthMultiplier = .036f * Mathf.Min(1.3f, size) * (calm ? .7f : 1f);
                _amHelix[k].enabled = true;
            }
            for (int j = 0; j < _amRing.Length; j++)
            {
                var line = _amRing[j];
                float v = j / (_amRing.Length - 1f);
                if (standing)
                {
                    float spin = (calm ? 0f : t * (15f + 3f * j)) + j * 1.3f;
                    for (int i = 0; i < AmRingSamples; i++)
                        _amRingPoints[i] = AmWhirlPoint(ball, size, v, spin + i / (AmRingSamples - 1f) * 5.2f, 1f, t, calm);
                    line.widthMultiplier = (.03f + .016f * v) * Mathf.Min(1.4f, size + .2f) * (calm ? .7f : 1f);
                }
                else if (since >= 0f && since < .55f)
                {
                    // THE BURST: each ring thrown forward down the lane as a widening arc, the upper ones faster and higher.
                    var centre = ball + Vector3.forward * (since * (9f + 2.5f * j)) + Vector3.up * (since * (.4f * j - .6f));
                    float r = .3f + since * (4.5f + j);
                    for (int i = 0; i < AmRingSamples; i++)
                    {
                        float a = Mathf.Lerp(-55f, 55f, i / (AmRingSamples - 1f)) * Mathf.Deg2Rad;
                        _amRingPoints[i] = centre + new Vector3(Mathf.Sin(a) * r, 0f, Mathf.Cos(a) * r * .45f);
                    }
                    line.widthMultiplier = .09f * (1f - Ease(.08f, .55f, since)) * (calm ? .7f : 1f);
                }
                else { line.enabled = false; continue; }
                line.SetPositions(_amRingPoints);
                line.enabled = line.widthMultiplier > .001f;
            }
        }

        // ------------------------------------------------------------------ the leaves

        private void SampleAmihanLeaves(float t, float story, bool calm, Vector3 ball, float size)
        {
            float since = story - AmReleaseAt;
            for (int i = 0; i < AmLeaves; i++)
            {
                var seed = _amLeafSeed[i];
                Vector3 at; bool on = true;
                float tumble = calm ? 0f : story;
                if (i == 0)
                {
                    // THE LONE LEAF: falling straight down past her open palm. No wind.
                    float u = Mathf.InverseLerp(AmLeafFrom, AmLeafTo, t);
                    on = t > AmLeafFrom && t < AmLeafTo;
                    at = new Vector3(-.78f + .1f * Mathf.Sin(t * 5.5f), Mathf.Lerp(2.5f, .03f, u) + _amCourt, .42f + .06f * Mathf.Cos(t * 4.2f));
                    // It rocks side to side as a leaf falls in still air, rather than tumbling.
                    Place(_amLeaf[i], at, new Vector3(.075f, .012f, .11f), Quaternion.Euler(18f * Mathf.Sin(t * 5.5f), 30f, 24f * Mathf.Cos(t * 5.5f)), on ? 1f : 0f);
                    continue;
                }
                bool arrival = i <= 14;
                // Carried in: riding just behind the head of its streak.
                float joined;
                if (arrival)
                {
                    int s = (i - 1) % _amArriveCore.Length;
                    float start = AmArriveAt + AmArriveRows[s, 5] + .03f * ((i - 1) / _amArriveCore.Length);
                    float u = Ease(0f, .5f, t - start) - .05f - .04f * seed.x;
                    joined = start + .5f;
                    AmArrivePath(s, ball, out var from, out var bend, out var side);
                    at = AmPath(from, bend, ball, side, AmArriveRows[s, 4], Mathf.Clamp01(u));
                    on = u > 0f;
                }
                else
                {
                    int s = (i - 15) % _amGatherCore.Length;
                    float start = AmWindAt + .1f * s + .04f * ((i - 15) / _amGatherCore.Length);
                    float u = Ease(0f, .42f, t - start) - .05f - .04f * seed.x;
                    joined = start + .42f;
                    AmGatherPath(s, ball, out var from, out var bend, out var side);
                    at = AmPath(from, bend, ball, side, .22f, Mathf.Clamp01(u));
                    on = u > 0f;
                }
                // Then round and round inside the whirlwind.
                float circle = Ease(joined - .08f, joined + .12f, t);
                if (circle > 0f && t < AmReleaseAt)
                {
                    var inside = AmWhirlPoint(ball, Mathf.Max(size, .3f), .2f + .7f * seed.y, seed.z * 6.28f + t * (calm ? 0f : 10f + 3f * seed.x), 1.15f, t, calm);
                    at = Vector3.Lerp(at, inside, circle);
                }
                if (since >= 0f)
                {
                    // FLUNG down the lane with the burst, fanned across its 60 degrees, rising, then falling; slow in the hang.
                    float yaw = (seed.x - .5f) * 60f * Mathf.Deg2Rad;
                    var dir = new Vector3(Mathf.Sin(yaw), .25f + .35f * seed.y, Mathf.Cos(yaw));
                    float speed = 6f + 6f * seed.z;
                    var origin = AmWhirlPoint(ball, Mathf.Max(size, .3f), .2f + .7f * seed.y, seed.z * 6.28f, 1.15f, AmReleaseAt, calm);
                    at = origin + dir * (speed * since) + Vector3.down * (3.5f * since * since);
                    on = since < 1.4f && at.y > _amCourt - .05f;
                    tumble += since * 4f;
                }
                if (!on) { Place(_amLeaf[i], Vector3.zero, Vector3.one * .001f, Quaternion.identity, 0f); continue; }
                var spinRot = Quaternion.Euler(tumble * (190f + 120f * seed.x) + i * 37f, tumble * (260f + 90f * seed.y) + i * 53f, tumble * 140f + i * 11f);
                Place(_amLeaf[i], at, new Vector3(.07f, .012f, .1f) * (.85f + .3f * seed.z), spinRot, 1f);
            }
        }

        // ------------------------------------------------------------------ the lens

        /// <summary>THE COMPUTED SHOT: FINISH stands high in front of her right side, her wink in the middle, the thrown
        /// bodies blowing away down the lane past her left (v4 r1 to r3), with a slow push in.</summary>
        private void AmihanFrame(int index, float t, ref Vector3 eye, ref Vector3 look, ref float fov)
        {
            if (index != AmShotHit) return;
            float u = Ease(AmReleaseAt + AmHangTo, Seconds, t);
            float a = 33f * Mathf.Deg2Rad, r = Mathf.Lerp(5.4f, 4.8f, u);
            eye = new Vector3(Mathf.Sin(a) * r, 3.3f - .25f * u + _amCourt, Mathf.Cos(a) * r);
            look = new Vector3(-.2f, .95f + _amCourt, .7f);
            fov = Mathf.Lerp(46f, 42f, u);
        }

        /// <summary>A warm afternoon: the light lifts a little while she plays with the wind; the release flares and the hang
        /// is a touch richer, so the held moment feels held.</summary>
        private void AmihanGrade(float t, out float brightness, out float saturation)
        {
            float warm = Ease(.1f, .5f, t) * (1f - Ease(AmWindAt, AmWindAt + .5f, t));
            float beat = Mathf.Max(AmBeat(t, AmReleaseAt, .3f), .6f * AmBeat(t, AmWhistleAt, .25f));
            float hang = Ease(AmReleaseAt + AmHangFrom - .05f, AmReleaseAt + AmHangFrom + .05f, t)
                * (1f - Ease(AmReleaseAt + AmHangTo, AmReleaseAt + AmHangTo + .1f, t));
            brightness = 1f + .04f * warm + .08f * beat + .03f * hang;
            saturation = 1f + .10f * warm + .14f * hang;
        }

        /// <summary>The lens takes the whistle's answer, the catch and the release, small and quickly gone.</summary>
        private static Vector3 AmihanShake(float t)
        {
            Vector3 Kick(float at, float size, float hz)
            {
                float s = t - at;
                if (s < 0f || s > .3f) return Vector3.zero;
                float fall = Mathf.Exp(-s * 14f) * size;
                return new Vector3(Mathf.Sin(s * hz) * fall, Mathf.Sin(s * hz * 1.3f + 1.1f) * fall * .6f, 0f);
            }
            return Kick(AmArriveAt + .1f, .02f, 64f) + Kick(AmReleaseAt, .08f, 46f);
        }
    }
}
