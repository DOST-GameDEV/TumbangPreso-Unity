using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // AMIHAN, AIRBURST v7: THE BIRD AMIHAN, 5.6 s (`docs/reports/amihan-presentation-2026-10-02/cutscene-v4-plan.md`, "v7").
        // The owner on v6: *"it doesnt look as good as paete's or phaisters"*, *"their ults look really good and have really cool
        // beats and show off story of their character"*, *"i want amihan to have hher own ult that doesnt jsut copy someone elses
        // but it has to have the same feel or impact"*, *"make it look like wind was slowly gathering around her and in the
        // background"*, *"everything in the bg is just blank"*.
        //
        // WHAT THE TWO APPROVED ONES SHARE (filmed side by side, `Logs/ult-reference-1003`): the WORLD changes (Paete's court
        // sprouts and a tree rises; Phaister's sky turns and an eye opens); a GIANT manifestation from the hero's own myth
        // (Makiling and the tree with a face; the puppeteer's hands and the doll); a CLOSE-UP of its eyes as the climax; then
        // the victims. HERS IS HER NAME: in the Tagalog creation story Amihan is the first bird, the one that pecked open the
        // bamboo. The monsoon answers her whistle across the whole plaza, gathers into that bird made of wind, the bird swoops
        // round her and past the lens with its eyes alight, rises behind her as she winds up, and on her drive beats its wings
        // down the lane: the fan. Not a tree from the ground, not an eye in the sky: a creature of the air that flies.
        //
        //   STILL    0.00  bored, eyes closed, she floats (the authored lift); ONE LEAF falls straight down: no wind. Taps, shrug.
        //   CALL     1.78  the whistle. THE MONSOON ANSWERS: great wind sheets sweep round the plaza behind her, leaves torn into
        //                  a wide ring and court dust rolling, all circling her and closing, spiralling up into the sky.
        //   THE BIRD 2.75  it forms out of the vortex above her, swoops one circle round her and close past the lens, eyes
        //                  alight, leaves trailing it; she reaches up to it, grinning, then glances into the lens.
        //   WIND-UP  3.62  the bird behind and above her, wings raised, fed by more air, as she coils.
        //   DRIVE    4.55  her palms and its wings come down together. THE HANG: the story clock at about a fifth.
        //   FINISH   5.34  the bird gone down the lane; a single feather drifts down; hands on hips, a wink.
        //
        // ⚠️ THE STORY CLOCK (`AmStory`): before the release it is the scene clock; after it, it runs slow through the hang and
        // normal again for the finish, scaled so it has advanced exactly `AmihanStorm.CutsceneTail` at the hand-back. The live
        // fan and the thrown players are posed on it, so play resumes where the last frame drew.
        //
        // ⚠️ THE VFX LANGUAGE: wind drawn as bold brushed strokes (a dark ink line under a bright core, so they read on the light
        // tiles and the sky), sweeping translucent sheets, leaves and court dust. Every piece travels from a source to a
        // destination. The bird is drawn in the same strokes. Posed from the clock; nothing on `Update`. ⚠️ Reduced effects
        // keeps every shape, stills the spin and the flutter, and halves the light.
        // =========================================================================================
        private const float AmReadAt = .78f, AmFloatFrom = .55f, AmFloatTo = 1.46f, AmLeafTo = 1.74f, AmWhistleAt = 1.78f, AmArriveAt = 1.92f,
            AmGatherFrom = 1.95f, AmFormAt = 2.75f, AmSwoopAt = 2.95f, AmSwoopEnd = 3.55f, AmWindAt = 3.80f, AmBraceAt = 4.05f,
            // The cut in on its face (`AmihanFrame`).
            AmEyesFrom = 3.10f, AmEyesTo = 3.34f,
            // THE RELEASE. Play resumes after `AmRealTail` real seconds, `AmihanStorm.CutsceneTail` story seconds after it.
            AmReleaseAt = 4.55f, AmFinishAt = 5.30f;

        /// <summary>Real seconds from the release to the hand-back (5.9 - 4.55; v9 holds her finish longer).</summary>
        public const float AmRealTail = 1.35f;
        // THE HANG: from 0.10 to 0.62 real seconds after the release, eased over 0.06 at each end, the clock at 18 per cent.
        private const float AmHangFrom = .10f, AmHangTo = .62f, AmHangEdge = .06f, AmHangSpeed = .18f;

        // The shots (`tools/author_ultimate_intros.py` amihan()).
        private const int AmShotStill = 0, AmShotCall = 1, AmShotBird = 2, AmShotWindup = 3, AmShotHang = 4, AmShotHit = 5;

        // Where the vortex closes and the bird forms: above and behind her.
        private static readonly Vector3 AmBirdBorn = new Vector3(0f, 5.0f, -1.5f);
        // Where it hovers through the wind-up: above and ahead of her, its back to her, so it reads from behind her and its
        // wingbeat goes straight down the lane (v7 r1: hovering behind her it sat over the lens, out of frame).
        private static readonly Vector3 AmBirdHover = new Vector3(0f, 3.7f, 1.9f);
        // v7 r1: at a 7 m span drawn in hair-lines it read as a doodle in the sky; about 12 m, bolder, with wing masses.
        private const float AmBirdScale = 1.7f, AmSwoopRadius = 4.0f;

        // ------------------------------------------------------------------ pieces
        // v9 (owner: "this shit looks like lighting"): ten points round a curl drew zigzags; smooth now.
        private const int AmStrokeSamples = 28;
        private readonly Vector3[] _amStrokePoints = new Vector3[AmStrokeSamples];
        // THE FLOAT'S BREEZE (owner on v7.8: *"shes js floating randomly"*, *"add more vfx here make it look like wind is
        // gathering"*): curled streaks drifting in to her from all round, wisps spiralling up her, rings of air under her feet.
        private readonly LineRenderer[] _amFloatCore = new LineRenderer[6], _amFloatInk = new LineRenderer[6];
        private readonly LineRenderer[] _amFloatWisp = new LineRenderer[4];
        private readonly LineRenderer[] _amFloatRing = new LineRenderer[2];
        private const int AmWispSamples = 16, AmFloatRingSamples = 24;
        private readonly Vector3[] _amWispPoints = new Vector3[AmWispSamples];
        private readonly Vector3[] _amFloatRingPoints = new Vector3[AmFloatRingSamples];
        // v8 (owner: *"thoroughly think abt where to add good vfx make sure vfx is good"*): one effect per beat that had none,
        // each with a cause. THE CALL: her whistle as a bright curl leaving her lips for the sky upwind. THE BIRTH: light where
        // the vortex closes into the bird. THE WAKE: the court's dust kicked up under its swoop, then drawn up into it as it
        // charges over her wind-up. THE SLAM: light at its wings on the drive.
        private LineRenderer _amCallCore, _amCallInk;
        private int _amBirth = -1, _amSlam = -1;
        private WindVfx.Motif _amWake;
        // The arrival: streaks racing in from upwind toward where the vortex closes.
        private readonly LineRenderer[] _amArriveCore = new LineRenderer[8], _amArriveInk = new LineRenderer[8];
        // The gather: more air drawn into the bird through the wind-up.
        private readonly LineRenderer[] _amGatherCore = new LineRenderer[4], _amGatherInk = new LineRenderer[4];
        // THE BIRD's eyes and heart (the rest of it: `BuildAmihanBird`).
        private int _amEyeL = -1, _amEyeR = -1, _amHeart = -1;
        // THE MONSOON: great translucent wind sheets circling the plaza, closing on her and rising into the sky.
        private const int AmSheets = 12;
        private readonly List<WindVfx.Ribbon> _amSheet = new List<WindVfx.Ribbon>();
        private readonly Vector3[] _amSheetSeed = new Vector3[AmSheets];
        // The burst on the drive: arcs racing down the lane off her palms.
        private const int AmRingSamples = 18;
        private readonly LineRenderer[] _amRing = new LineRenderer[5];
        private readonly Vector3[] _amRingPoints = new Vector3[AmRingSamples];
        // THE LEAVES: 0 the lone leaf; 1 to 60 torn up by the monsoon; the feather at the end.
        private const int AmLeaves = 61;
        private readonly int[] _amLeaf = new int[AmLeaves];
        private readonly Vector3[] _amLeafSeed = new Vector3[AmLeaves];
        private int _amFeather = -1;
        private readonly List<WindVfx.Ribbon> _amSpeed = new List<WindVfx.Ribbon>();
        private WindVfx.Motif _amDust;
        private AmihanStormFan _amihanFan;
        private VoxelFace _amFace;
        private float _amCourt;

        // THE ARRIVAL: start (scene x, height, z), the side its curl turns to (+1 her right), curl radius, delay, width.
        private static readonly float[,] AmArriveRows =
        {
            { -9.0f, .30f, -6.2f,  1f, .30f, .00f, .080f }, { -8.2f, 1.10f, -4.4f, -1f, .36f, .06f, .090f },
            { -9.6f, .70f, -3.1f,  1f, .28f, .12f, .075f }, { -7.4f, 1.60f, -6.8f, -1f, .34f, .17f, .080f },
            {  8.8f, .40f, -4.2f, -1f, .32f, .22f, .085f }, {  9.9f, 1.30f, -2.0f,  1f, .38f, .27f, .075f },
            {  7.9f, .50f, -7.6f,  1f, .30f, .32f, .080f }, {  6.3f, 1.90f,  5.8f, -1f, .34f, .37f, .070f },
        };

        // THE SPEED STREAKS down the lane on the release: x, height, start z, end z, delay, width.
        private static readonly float[,] AmSpeedRows =
        {
            { -.45f, 1.85f, 1.0f,  9.5f, .00f, .06f }, {  .62f, 1.35f, 1.2f, 10.5f, .03f, .05f }, { -1.10f,  .70f, 1.6f,  8.5f, .05f, .05f },
            { 1.30f, 2.05f, 1.4f, 11.0f, .02f, .06f }, {  .15f,  .45f, 1.1f,  9.0f, .07f, .04f }, { -1.60f, 1.55f, 2.0f, 10.0f, .04f, .05f },
            {  .95f,  .85f, 1.8f,  8.0f, .06f, .04f }, { -.20f, 2.35f, 1.5f, 12.0f, .01f, .05f },
        };

        private static readonly Color AmSheetBody = new Color(0.78f, 0.96f, 0.72f, 1f);
        // v9: a dark ink line under a white core read as a lightning bolt; a soft pale teal edge reads as air.
        private static readonly Color AmInk = new Color(0.50f, 0.80f, 0.68f, .3f);
        private static readonly Color AmEye = new Color(1.0f, 0.93f, 0.62f, 1f);
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
                AmStrokePair("AmihanArrive" + i, i % 3 == 0 ? WindVfx.Core : AmSheetBody, taper, out _amArriveCore[i], out _amArriveInk[i]);
            for (int i = 0; i < _amGatherCore.Length; i++)
                AmStrokePair("AmihanGather" + i, i % 2 == 0 ? WindVfx.Core : AmSheetBody, taper, out _amGatherCore[i], out _amGatherInk[i]);

            for (int i = 0; i < _amFloatCore.Length; i++)
                AmStrokePair("AmihanFloatIn" + i, i % 2 == 0 ? WindVfx.Core : AmSheetBody, taper, out _amFloatCore[i], out _amFloatInk[i]);
            var wisp = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(.4f, 1f), new Keyframe(1f, .1f));
            for (int i = 0; i < _amFloatWisp.Length; i++)
            {
                _amFloatWisp[i] = Line("AmihanFloatWisp" + i, AmWispSamples, .05f, Color.white);
                _amFloatWisp[i].widthCurve = wisp; _amFloatWisp[i].numCapVertices = 0; _amFloatWisp[i].enabled = false;
            }
            for (int i = 0; i < _amFloatRing.Length; i++)
            {
                _amFloatRing[i] = Line("AmihanFloatRing" + i, AmFloatRingSamples, .04f, new Color(0.92f, 1.0f, 0.95f, .8f));
                _amFloatRing[i].widthCurve = wisp; _amFloatRing[i].numCapVertices = 0; _amFloatRing[i].enabled = false;
            }

            AmStrokePair("AmihanCall", Color.white, taper, out _amCallCore, out _amCallInk);
            _amBirth = AddGlow("AmihanBirdBirth", new Color(0.82f, 1.0f, 0.90f, 1f), falloff: 1.6f, core: .4f);
            _amSlam = AddGlow("AmihanBirdSlam", new Color(0.92f, 1.0f, 0.95f, 1f), falloff: 1.8f, core: .5f);
            _amWake = new WindVfx.Motif(AmHost("AmihanWake"), 22, 29.3f, .25f);

            BuildAmihanBird();

            // THE MONSOON SHEETS: each a standing arc of the wind ribbon, posed every frame round a moving centre.
            for (int k = 0; k < AmSheets; k++)
            {
                _amSheetSeed[k] = new Vector3(AmHash(k * 5 + 11), AmHash(k * 5 + 12), AmHash(k * 5 + 13));
                // v7 r2: 22 points scaled to 13 m drew jagged polylines, and the bright-rim look drew them hollow; smooth and filled.
                var arc = WindVfx.Arc(1f, 70f + 50f * _amSheetSeed[k].x, 48, 0f);
                var sheet = WindVfx.Build(_root.transform, "AmihanMonsoon" + k, arc, 1.3f + .8f * _amSheetSeed[k].y, WindVfx.Standing, 5f, .45f, 400f + k);
                sheet.Recolour(WindVfx.Core, AmSheetBody, WindVfx.Body);
                _amSheet.Add(sheet);
            }

            var burst = new AnimationCurve(new Keyframe(0f, .2f), new Keyframe(.3f, 1f), new Keyframe(.8f, 1f), new Keyframe(1f, .1f));
            for (int j = 0; j < _amRing.Length; j++)
            {
                _amRing[j] = Line("AmihanBurst" + j, AmRingSamples, .05f, j % 2 == 0 ? WindVfx.Core : AmSheetBody);
                _amRing[j].widthCurve = burst; _amRing[j].sortingOrder = 2; _amRing[j].enabled = false;
            }

            // THE LEAVES: thin lit slabs, each with its own seed for its path, its tumble and its colour.
            var leaf = VfxShapes.Prism(4, 1, 1);
            for (int i = 0; i < AmLeaves; i++)
            {
                _amLeafSeed[i] = new Vector3(AmHash(i * 3 + 1), AmHash(i * 3 + 2), AmHash(i * 3 + 3));
                _amLeaf[i] = AddSolid("AmihanLeaf" + i, leaf, AmLeafColours[i % AmLeafColours.Length]);
                Place(_amLeaf[i], Vector3.zero, Vector3.one * .001f, Quaternion.identity, 0f);
            }
            _amFeather = AddSolid("AmihanFeather", leaf, new Color(0.96f, 1.0f, 0.93f, 1f));
            Place(_amFeather, Vector3.zero, Vector3.one * .001f, Quaternion.identity, 0f);

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

            // Court dust rolling round her in the monsoon's ring (its own host: a Motif hands its tuft mesh to its parent's owner).
            _amDust = new WindVfx.Motif(AmHost("AmihanCourtDust"), 28, 17.1f, .3f);

            // The live fan itself, posed from the story clock and only from the release on: at the hand-back it is the live
            // fan `AmihanStorm.CutsceneTail` after its release, on the same court.
            _amihanFan = AmihanStormFan.Build(_root.transform, _root.transform.position, _root.transform.forward,
                Core.AmihanRules.StormSurgeGatherSeconds);
            _amihanFan.enabled = false;

            // Her cutscene faces (squint, whistle, grin, wink) on the copied body's own head.
            _amFace = VoxelFace.Attach(_bodyRenderers);

            BuildAmihanLane();
        }

        private void AmStrokePair(string name, Color core, AnimationCurve taper, out LineRenderer bright, out LineRenderer ink)
        {
            ink = Line(name + "Ink", AmStrokeSamples, .1f, AmInk);
            ink.widthCurve = taper; ink.sortingOrder = 0; ink.enabled = false; ink.numCornerVertices = 4;
            bright = Line(name, AmStrokeSamples, .06f, new Color(core.r, core.g, core.b, .85f));
            bright.widthCurve = taper; bright.sortingOrder = 1; bright.enabled = false; bright.numCornerVertices = 4;
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
            // v12 (owner on the teehee chevrons: "weird af expression"): her happy closed-eyed grin.
            if (t >= AmFinishAt - .04f) return VoxelFace.Look.Grin;
            if (t >= AmReleaseAt - .02f) return VoxelFace.Look.Grin;
            if (t >= AmWindAt - .05f) return VoxelFace.Look.Squint;
            if (t >= 2.84f) return VoxelFace.Look.Grin;
            if (t >= AmWhistleAt - .05f && t < 2.12f) return VoxelFace.Look.Whistle;
            if (t >= AmReadAt - .08f && t < 1.62f) return VoxelFace.Look.Squint;
            return VoxelFace.Look.Rest;
        }

        // ------------------------------------------------------------------ the sample

        private void SampleAmihan(float t)
        {
            bool calm = _reducedEffects;
            float story = AmStory(t);
            float light = calm ? .55f : 1f;
            float leave = 1 - Ease(Seconds - .30f, Seconds, t);
            float since = story - AmReleaseAt;
            _amFace?.Show(AmLook(t));

            AmBirdPose(story, calm, out var birdAt, out var birdTurn, out float flap, out float birdOn);
            SampleAmihanBird(story, calm, birdAt, birdTurn, flap, birdOn);
            SampleAmihanMonsoon(t, calm, light);
            SampleAmihanFloat(t, calm, light);
            SampleAmihanAccents(t, story, calm, light, birdAt, birdOn);

            // THE ARRIVAL: the wind answering the whistle from every side of the plaza, each streak curling once on its way to
            // where the vortex closes over her.
            for (int i = 0; i < _amArriveCore.Length; i++)
            {
                float s = t - AmArriveAt - AmArriveRows[i, 5];
                var from = new Vector3(AmArriveRows[i, 0], AmArriveRows[i, 1] + _amCourt, AmArriveRows[i, 2]);
                var to = AmBirdBorn + Vector3.up * _amCourt;
                var flat = to - from; flat.y = 0f;
                var side = Vector3.Cross(Vector3.up, flat.normalized) * AmArriveRows[i, 3];
                var bend = Vector3.Lerp(from, to, .5f) + side * 2.2f + Vector3.down * 1.2f;
                AmStroke(_amArriveCore[i], _amArriveInk[i], from, bend, to, side, AmArriveRows[i, 4],
                    Ease(0f, .55f, s), Ease(.22f, .72f, s), AmArriveRows[i, 6] * light);
            }

            // THE GATHER: more air drawn in from behind her into the bird through the wind-up.
            for (int i = 0; i < _amGatherCore.Length; i++)
            {
                float s = t - AmWindAt - .1f * i;
                float a = (130f + 33f * i) * Mathf.Deg2Rad;
                var from = new Vector3(Mathf.Sin(a) * 6f, .6f + .5f * i + _amCourt, Mathf.Cos(a) * 6f);
                var flat = birdAt - from; flat.y = 0f;
                var side = Vector3.Cross(Vector3.up, flat.normalized) * (i % 2 == 0 ? 1f : -1f);
                var bend = Vector3.Lerp(from, birdAt, .5f) + Vector3.up * .8f;
                AmStroke(_amGatherCore[i], _amGatherInk[i], from, bend, birdAt, side, .3f, Ease(0f, .36f, s), Ease(.16f, .5f, s), .07f * light);
            }

            SampleAmihanBurst(since, calm, 1f - Ease(AmReleaseAt + AmHangTo - .1f, AmReleaseAt + AmHangTo - .04f, t));
            SampleAmihanLeaves(t, story, calm);

            // THE SPEED STREAKS race down the lane on the release (story time: they hang with everything else). v15: gone before
            // the finish close-up, whose lens stands in their lane (they drew bands across her face).
            for (int i = 0; i < _amSpeed.Count; i++)
            {
                float s = since - AmSpeedRows[i, 4] * .4f;
                if (s < 0f || s > .5f) { _amSpeed[i].Set(0f, 0f); continue; }
                _amSpeed[i].Set(.75f * light * leave * (1f - Ease(AmReleaseAt + AmHangTo - .1f, AmReleaseAt + AmHangTo - .04f, t)), calm ? 0f : story * 9f, Ease(0f, .18f, s), Ease(.1f, .45f, s), Ease(.2f, .5f, s) * .7f);
            }

            // COURT DUST rolling round her in the monsoon's ring, closing with it.
            float dustFloor = _amCourt;
            float close = Ease(AmGatherFrom, AmFormAt, t);
            if (t < AmWhistleAt)
            {
                // THE FLOAT: court dust drawn in along the tiles toward her feet by her breeze, dying as she settles.
                float floatOn = Ease(AmFloatFrom + .1f, AmFloatFrom + .4f, t) * (1f - Ease(AmFloatTo - .15f, AmFloatTo + .1f, t));
                _amDust.Step(Mathf.Repeat(t * .7f, 1f), (calm ? .35f : .7f) * floatOn, (start, drift, u) =>
                {
                    float r = Mathf.Lerp(2.6f + 1.4f * start.z, .35f, u);
                    float a = start.x * 6.28f + u * 1.4f;
                    return new Vector3(Mathf.Sin(a) * r, dustFloor + .04f + .12f * Mathf.Sin(u * Mathf.PI) * (.5f + start.y), Mathf.Cos(a) * r);
                }, .08f);
            }
            else
            {
                float dustOn = Ease(AmGatherFrom, AmGatherFrom + .3f, t) * (1f - Ease(AmFormAt, AmSwoopAt + .2f, t));
                _amDust.Step(Mathf.Repeat(t * .6f, 1f), (calm ? .4f : .9f) * dustOn, (start, drift, u) =>
                {
                    float r = Mathf.Lerp(4f + 4f * start.z, 1.6f + start.z, close);
                    float a = start.x * 6.28f - (t - AmGatherFrom) * (1.2f + 2.4f * close) - u * 1.5f;
                    return new Vector3(Mathf.Sin(a) * r, dustFloor + .05f + u * (.6f + start.y) + drift.y * .2f, Mathf.Cos(a) * r);
                }, .1f);
            }

            SampleAmihanLane(t, story);

            // THE RELEASE: the live fan's own clock on the story clock, hidden until the drive and released ON it; at the
            // hand-back it is the live fan `AmihanStorm.CutsceneTail` after its release.
            float gather = Core.AmihanRules.StormSurgeGatherSeconds;
            _amihanFan.StepTo(t < AmReleaseAt ? -AmihanStormFan.DrawOnSeconds : gather + since);
        }

        /// <summary>
        /// A stroke's path at <paramref name="u"/>: a curve with ONE CURL in it (a full loop between 0.48 and 0.72 of the
        /// way, turning to <paramref name="side"/> and up), the classic drawn gust.
        /// </summary>
        private static Vector3 AmPath(Vector3 from, Vector3 bend, Vector3 to, Vector3 side, float curl, float u)
        {
            var p = AmBezier(from, bend, to, u);
            float k = Mathf.Clamp01((u - .48f) / .24f);
            if (k <= 0f || k >= 1f) return p;
            float a = k * k * (3f - 2f * k) * Mathf.PI * 2f;
            return p + (side * Mathf.Sin(a) + Vector3.up * (1f - Mathf.Cos(a))) * curl;
        }

        /// <summary>One curled stroke: its ink line and bright core along the stretch of its path between tail and head.</summary>
        private void AmStroke(LineRenderer core, LineRenderer ink, Vector3 from, Vector3 bend, Vector3 to, Vector3 side, float curl,
            float head, float tail, float width)
        {
            if (head - tail < .02f || width <= 0f) { core.enabled = false; ink.enabled = false; return; }
            for (int i = 0; i < AmStrokeSamples; i++)
                _amStrokePoints[i] = AmPath(from, bend, to, side, curl, Mathf.Lerp(tail, head, i / (AmStrokeSamples - 1f)));
            core.SetPositions(_amStrokePoints); ink.SetPositions(_amStrokePoints);
            core.widthMultiplier = width; ink.widthMultiplier = width * 2.4f;
            core.enabled = true; ink.enabled = true;
        }

        // ------------------------------------------------------------------ the float

        /// <summary>
        /// HER OWN BREEZE while she floats with her eyes closed: curled streaks drifting in to her from all round the plaza and
        /// winding into her, white wisps spiralling up her, rings of air pulsing out under her feet, the court's dust drawn in
        /// toward her. It gathers as she rises and dies as she settles: not enough. Then she whistles for the monsoon.
        /// </summary>
        private void SampleAmihanFloat(float t, bool calm, float light)
        {
            float lift = LiftAt(t);
            float gather = Ease(AmFloatFrom, AmFloatFrom + .45f, t) * (1f - Ease(AmFloatTo - .2f, AmFloatTo + .08f, t));
            var her = new Vector3(0f, _amCourt + .95f + lift, 0f);
            // In from all round: each streak starts out in the plaza, curls once, and winds into her.
            for (int i = 0; i < _amFloatCore.Length; i++)
            {
                float s = t - AmFloatFrom - .11f * i;
                float a = (i * 61f + 25f) * Mathf.Deg2Rad, reach = 3.6f + .9f * (i % 3);
                var from = new Vector3(Mathf.Sin(a) * reach, _amCourt + .3f + .35f * (i % 4), Mathf.Cos(a) * reach);
                var flat = her - from; flat.y = 0f;
                var side = Vector3.Cross(Vector3.up, flat.normalized) * (i % 2 == 0 ? 1f : -1f);
                var bend = Vector3.Lerp(from, her, .5f) + side * 1.1f + Vector3.up * .3f;
                AmStroke(_amFloatCore[i], _amFloatInk[i], from, bend, her, side, .2f, Ease(0f, .5f, s), Ease(.2f, .66f, s),
                    .05f * light * (t < AmFloatTo ? 1f : 0f));
            }
            // Up her: open wisps spiralling from her feet past her head, each running up its own length.
            for (int i = 0; i < _amFloatWisp.Length; i++)
            {
                var line = _amFloatWisp[i];
                if (gather <= .01f) { line.enabled = false; continue; }
                float run = calm ? .5f : Mathf.Repeat((t - AmFloatFrom) * .9f + i * .25f, 1f);
                for (int k = 0; k < AmWispSamples; k++)
                {
                    float u = Mathf.Lerp(Mathf.Max(0f, run - .45f), run, k / (AmWispSamples - 1f));
                    float ang = i * 1.57f + u * 7f + (calm ? 0f : t * 2.2f);
                    float r = .62f + .12f * Mathf.Sin(u * 5f + i);
                    _amWispPoints[k] = new Vector3(Mathf.Sin(ang) * r, _amCourt + lift - .05f + u * 2.1f, Mathf.Cos(ang) * r);
                }
                line.SetPositions(_amWispPoints);
                line.widthMultiplier = .045f * gather * light;
                line.enabled = true;
            }
            // Under her feet: rings of air pulsing outward, the cushion she floats on, for as long as she floats (v10: the whole
            // ult, owner: "she should stay floating too during her ult").
            float cushion = Mathf.Clamp01(lift / .2f);
            for (int i = 0; i < _amFloatRing.Length; i++)
            {
                var line = _amFloatRing[i];
                if (cushion <= .01f) { line.enabled = false; continue; }
                float pulse = calm ? .5f : Mathf.Repeat((t - AmFloatFrom) * 1.6f + i * .5f, 1f);
                float r = Mathf.Lerp(.3f, .85f, pulse);
                for (int k = 0; k < AmFloatRingSamples; k++)
                {
                    float ang = (k / (AmFloatRingSamples - 1f) * 300f + i * 150f + (calm ? 0f : t * 90f)) * Mathf.Deg2Rad;
                    _amFloatRingPoints[k] = new Vector3(Mathf.Sin(ang) * r, _amCourt + lift - .03f - .1f * pulse, Mathf.Cos(ang) * r);
                }
                line.SetPositions(_amFloatRingPoints);
                line.widthMultiplier = .05f * cushion * light * Mathf.Sin(pulse * Mathf.PI);
                line.enabled = true;
            }
        }

        // ------------------------------------------------------------------ the accents

        private void SampleAmihanAccents(float t, float story, bool calm, float light, Vector3 birdAt, float birdOn)
        {
            // THE CALL: her whistle leaves her lips as one bright curl, racing up over the roofs toward where the wind will come
            // from (her left, behind), a beat before the monsoon answers.
            {
                float s = t - AmWhistleAt - .03f;
                var mouth = HeadPoint + new Vector3(0f, -.5f, .25f);
                var sky = new Vector3(-6.5f, 6.5f + _amCourt, -7f);
                var side = Vector3.Cross(Vector3.up, (sky - mouth).normalized).normalized;
                var bend = Vector3.Lerp(mouth, sky, .45f) + Vector3.up * 1.2f + side * .8f;
                AmStroke(_amCallCore, _amCallInk, mouth, bend, sky, side, .35f, Ease(0f, .3f, s), Ease(.12f, .42f, s), .07f * light);
            }

            // THE BIRTH: light swells where the vortex closes and the bird opens out of it.
            {
                var at = AmBirdBorn + Vector3.up * _amCourt;
                float swell = Ease(AmFormAt - .12f, AmFormAt + .02f, t) * (1f - Ease(AmFormAt + .05f, AmFormAt + .3f, t));
                PlaceGlow(_amBirth, at, Vector3.one * Mathf.Lerp(2.5f, 5.5f, Ease(AmFormAt - .12f, AmFormAt + .3f, t)), Quaternion.identity,
                    1.4f * swell * (calm ? .5f : 1f));
            }

            // THE WAKE: dust and puffs kicked up off the court under its swoop as it passes low, then, through her wind-up, the
            // court's dust spiralling up into it as it charges over her.
            float sweep = Ease(AmSwoopAt + .05f, AmSwoopAt + .15f, story) * (1f - Ease(AmSwoopEnd - .05f, AmSwoopEnd + .1f, story));
            float charge = Ease(AmWindAt, AmWindAt + .2f, story) * (1f - Ease(AmReleaseAt - .03f, AmReleaseAt + .05f, story));
            float court = _amCourt;
            if (sweep > charge)
            {
                _amWake.Step(Mathf.Repeat(story * 1.4f, 1f), (calm ? .4f : .85f) * sweep * birdOn, (start, drift, u) =>
                {
                    AmBirdPose(story - .05f - .35f * start.x, calm, out var past, out _, out _, out _);
                    return new Vector3(past.x + (start.z - .5f) * 1.6f, court + .05f + u * (.5f + .8f * start.y), past.z + (drift.x - .5f) * 1.2f);
                }, .12f);
            }
            else
            {
                var below = new Vector3(birdAt.x, court, birdAt.z);
                _amWake.Step(Mathf.Repeat(story * 1.1f, 1f), (calm ? .4f : .85f) * charge, (start, drift, u) =>
                {
                    float a = start.x * 6.28f + u * 3.5f, rad = Mathf.Lerp(3.2f + 1.2f * start.z, .3f, u);
                    return Vector3.Lerp(below + new Vector3(Mathf.Sin(a) * rad, .05f, Mathf.Cos(a) * rad), birdAt, u * u);
                }, .1f);
            }

            // THE SLAM: light at its wings as they come down with her palms.
            {
                float s = story - AmReleaseAt;
                float flash = s < 0f ? 0f : Mathf.Clamp01(1f - s / .22f);
                PlaceGlow(_amSlam, birdAt + Vector3.down * .4f, Vector3.one * (4f + 6f * Mathf.Clamp01(s / .22f)), Quaternion.identity,
                    1.6f * flash * flash * (calm ? .5f : 1f));
            }
        }

        // ------------------------------------------------------------------ the monsoon

        /// <summary>Where the vortex turns about: on her, rising toward where the bird forms.</summary>
        private Vector3 AmVortexCentre(float t) => Vector3.Lerp(Vector3.up * _amCourt, AmBirdBorn + Vector3.up * _amCourt, Ease(2.35f, AmFormAt + .1f, t));

        /// <summary>
        /// THE MONSOON: twelve great sheets of wind sweeping round the plaza behind her from the whistle, slow at first and
        /// faster as they close, rising and narrowing into a funnel over her that becomes the bird.
        /// </summary>
        private void SampleAmihanMonsoon(float t, bool calm, float light)
        {
            var centre = AmVortexCentre(t);
            float on = Ease(AmGatherFrom, AmGatherFrom + .35f, t) * (1f - Ease(AmFormAt - .05f, AmFormAt + .2f, t));
            float close = Ease(AmGatherFrom + .1f, AmFormAt + .1f, t);
            float s = Mathf.Max(0f, t - AmGatherFrom);
            for (int k = 0; k < _amSheet.Count; k++)
            {
                var seed = _amSheetSeed[k];
                if (on <= .002f) { _amSheet[k].Set(0f, 0f); continue; }
                float r = Mathf.Lerp(4.5f + 5.5f * seed.x, .8f + .6f * seed.z, close * close);
                float h = Mathf.Lerp(.6f + 3.2f * seed.y, -.6f + 1.2f * seed.y, close);
                // Faster as they close: the angle is the integral of a speed that rises with the closing.
                float spin = calm ? 0f : -(s * 40f + s * s * 70f) * (.8f + .4f * seed.z);
                var tf = _amSheet[k].GameObject.transform;
                tf.localPosition = centre + Vector3.up * h;
                tf.localRotation = Quaternion.Euler(0f, seed.x * 360f + spin, (seed.y - .5f) * 14f);
                tf.localScale = new Vector3(r, 1f + 1.2f * (1f - close), r);
                float sweep = Mathf.Repeat(s * (.7f + .5f * seed.z) + seed.x, 1f);
                _amSheet[k].Set((.8f + .2f * seed.y) * on * light, calm ? 0f : -t * 6f, Mathf.Clamp01(sweep * 1.6f + .3f), Mathf.Clamp01(sweep * 1.6f - .5f), .2f);
            }
        }

        // ------------------------------------------------------------------ the bird

        // v7 r4 (owner: *"bird has to look better too"*, *"feels a bit clunky"*): the stroke-only bird read as a fish skeleton,
        // stick feathers with bead ends. Now a spirit of the monsoon with a SHAPE: glowing translucent wings with a scalloped
        // feather edge and a bright leading edge, a glowing body with a gold beak, long streamers in its tail and trails off its
        // wing tips that follow the path it really flew, so every swoop and wingbeat leaves a graceful arc.
        // v7 r5: near-white against the pale sky it faded to a haze; her teal-green, with the white kept for its edges.
        // v16 b (owner: "it doesnt look that good"): more colour in the wings so they hold a shape against the sky.
        private static readonly Color AmBirdFill = new Color(0.30f, 0.82f, 0.68f, .8f);
        private static readonly Color AmBirdBody = new Color(0.50f, 0.90f, 0.78f, .8f);
        private static readonly Color AmBeak = new Color(1.0f, 0.90f, 0.58f, .92f);
        // v16 (owner: "the bird has no head"): the eyes floated over the front of the body, the same teal as the wings. A
        // round head of its own, paler and brighter, on a neck raised off the breast.
        // v16: pure white edges and tip trails rendered salmon over the sky, the edge a pink zigzag like a bolt; her mint.
        private static readonly Color AmBirdEdge = new Color(0.70f, 1.0f, 0.86f, 1f);
        private static readonly Color AmBirdHead = new Color(0.80f, 1.0f, 0.92f, .9f);
        private static readonly Vector3 AmHeadAt = new Vector3(0f, .6f, 1.45f), AmHeadSize = new Vector3(.62f, .56f, .7f);
        private const int AmEdgeSamples = 21, AmTrailSamples = 12;
        private readonly int[] _amWing = new int[2];
        private readonly Mesh[] _amWingMesh = new Mesh[2];
        private readonly Vector3[] _amWingVerts = new Vector3[AmEdgeSamples * 2];
        private int _amBody = -1, _amHead = -1, _amBeakPiece = -1;
        private readonly LineRenderer[] _amWingEdge = new LineRenderer[2], _amWingTips = new LineRenderer[2], _amWingVein = new LineRenderer[6];
        private readonly LineRenderer[] _amTail = new LineRenderer[5], _amTipTrail = new LineRenderer[2], _amCrest = new LineRenderer[3];
        private readonly Vector3[] _amEdgePoints = new Vector3[AmEdgeSamples], _amTipPoints = new Vector3[AmEdgeSamples];
        private readonly Vector3[] _amTrailPoints = new Vector3[AmTrailSamples];

        private void BuildAmihanBird()
        {
            for (int w = 0; w < 2; w++)
            {
                var mesh = new Mesh { name = "AmihanBirdWing" + w };
                mesh.MarkDynamic();
                mesh.vertices = new Vector3[AmEdgeSamples * 2];
                // Both windings: the wing is seen from above and below.
                var tris = new List<int>((AmEdgeSamples - 1) * 12);
                for (int i = 0; i < AmEdgeSamples - 1; i++)
                {
                    int l0 = i, l1 = i + 1, t0 = AmEdgeSamples + i, t1 = AmEdgeSamples + i + 1;
                    tris.AddRange(new[] { l0, l1, t1, l0, t1, t0, l0, t1, l1, l0, t0, t1 });
                }
                mesh.triangles = tris.ToArray();
                _amWingMesh[w] = mesh;
                _amWing[w] = Add("AmihanBirdWing" + w, mesh, AmBirdFill, .55f, plain: true);
            }
            _amBody = Add("AmihanBirdBody", AmBodyMesh(), AmBirdBody, .55f, plain: true);
            _amHead = Add("AmihanBirdHead", AmHeadMesh(), AmBirdHead, .75f, plain: true);
            // v9 (owner on the close-up: "tf is that haha"): a solid orange pyramid read as a paper plane; a small round beak
            // glowing like the rest of its spirit body.
            _amBeakPiece = Add("AmihanBirdBeak", VfxShapes.Prism(10, 1f, 0f), AmBeak, .75f, plain: true);

            var edge = new AnimationCurve(new Keyframe(0f, .6f), new Keyframe(.5f, 1f), new Keyframe(1f, .15f));
            var trail = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(.6f, .55f), new Keyframe(1f, 0f));
            for (int w = 0; w < 2; w++)
            {
                _amWingEdge[w] = Line("AmihanBirdEdge" + w, AmEdgeSamples, .07f, AmBirdEdge);
                _amWingEdge[w].widthCurve = edge; _amWingEdge[w].sortingOrder = 4; _amWingEdge[w].numCapVertices = 0;
                // v16 c: the feather tips drawn in light, so the fingers and scallops make a crisp edge where the translucent
                // layers otherwise melt together in the wind-up.
                _amWingTips[w] = Line("AmihanBirdTips" + w, AmEdgeSamples, .035f, AmBirdEdge);
                _amWingTips[w].sortingOrder = 4; _amWingTips[w].numCapVertices = 0; _amWingTips[w].numCornerVertices = 2;
                _amTipTrail[w] = Line("AmihanBirdTipTrail" + w, AmTrailSamples, .12f, AmBirdEdge);
                _amTipTrail[w].widthCurve = trail; _amTipTrail[w].sortingOrder = 3; _amTipTrail[w].numCapVertices = 0;
            }
            for (int v = 0; v < _amWingVein.Length; v++)
            {
                _amWingVein[v] = Line("AmihanBirdVein" + v, AmStrokeSamples, .03f, AmSheetBody);
                _amWingVein[v].widthCurve = edge; _amWingVein[v].sortingOrder = 3; _amWingVein[v].numCapVertices = 0;
            }
            // v16 b: five plumes, each a quill that flares into a feather at its end, as a phoenix's (three thin streamers read as
            // strings trailing behind it).
            var plume = new AnimationCurve(new Keyframe(0f, .45f), new Keyframe(.5f, .22f), new Keyframe(.8f, 1f), new Keyframe(.93f, .8f), new Keyframe(1f, 0f));
            for (int k = 0; k < _amTail.Length; k++)
            {
                _amTail[k] = Line("AmihanBirdTail" + k, AmTrailSamples, .4f, k == 2 ? new Color(0.88f, 1.0f, 0.92f, .85f) : k % 2 == 0 ? new Color(0.62f, 0.95f, 0.80f, .75f) : new Color(0.42f, 0.86f, 0.72f, .75f));
                _amTail[k].widthCurve = plume; _amTail[k].sortingOrder = 2; _amTail[k].numCapVertices = 0;
            }
            for (int c = 0; c < _amCrest.Length; c++)
            {
                _amCrest[c] = Line("AmihanBirdCrest" + c, AmStrokeSamples, .09f, AmSheetBody);
                _amCrest[c].widthCurve = trail; _amCrest[c].sortingOrder = 4; _amCrest[c].numCapVertices = 0;
            }
            _amEyeL = AddGlow("AmihanBirdEyeL", AmEye, falloff: 2.2f, core: 1.0f);
            _amEyeR = AddGlow("AmihanBirdEyeR", AmEye, falloff: 2.2f, core: 1.0f);
            _amHeart = AddGlow("AmihanBirdHeart", new Color(0.80f, 1.0f, 0.86f, 1f), falloff: 1.4f, core: 0f);
        }

        /// <summary>
        /// Its body: a tube along a curved spine from the root of the tail (z -1.1) through the deep breast and up a neck into
        /// the back of the head (v16: the old spindle tapered to a point where the head should be). Stations are (z, y, radius).
        /// </summary>
        private static Mesh AmBodyMesh()
        {
            var stations = new[]
            {
                new Vector3(1.32f, .56f, .1f), new Vector3(1.12f, .46f, .2f), new Vector3(.88f, .32f, .25f), new Vector3(.5f, .16f, .4f),
                new Vector3(0f, .05f, .42f), new Vector3(-.6f, 0f, .3f), new Vector3(-1.1f, 0f, .05f),
            };
            const int rings = 25, sides = 12;
            var verts = new List<Vector3>(rings * sides);
            for (int r = 0; r < rings; r++)
            {
                float f = r / (rings - 1f) * (stations.Length - 1);
                int k = Mathf.Min((int)f, stations.Length - 2);
                var c = Vector3.Lerp(stations[k], stations[k + 1], Mathf.SmoothStep(0f, 1f, f - k));
                for (int s = 0; s < sides; s++)
                {
                    float a = s / (float)sides * Mathf.PI * 2f;
                    verts.Add(new Vector3(Mathf.Cos(a) * c.z, c.y + Mathf.Sin(a) * c.z * .85f, c.x));
                }
            }
            var tris = new List<int>();
            for (int r = 0; r < rings - 1; r++)
                for (int s = 0; s < sides; s++)
                {
                    int a = r * sides + s, b = r * sides + (s + 1) % sides, c = a + sides, d = b + sides;
                    tris.AddRange(new[] { a, c, b, b, c, d });
                }
            var mesh = new Mesh { name = "AmihanBirdBody" };
            mesh.SetVertices(verts); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Its head: a unit sphere, scaled by <see cref="AmHeadSize"/> where it is placed.</summary>
        private static Mesh AmHeadMesh()
        {
            const int rings = 10, sides = 14;
            var verts = new List<Vector3>();
            for (int r = 0; r <= rings; r++)
            {
                float v = Mathf.PI * r / rings;
                for (int s = 0; s < sides; s++)
                {
                    float a = s / (float)sides * Mathf.PI * 2f;
                    // v16 b: a teardrop drawn forward into the beak, not a ball stuck on the neck. The pole points forward.
                    float z = Mathf.Cos(v);
                    float pinch = z > 0f ? 1f - .55f * z * z : 1f;
                    verts.Add(new Vector3(Mathf.Sin(v) * Mathf.Cos(a) * pinch, Mathf.Sin(v) * Mathf.Sin(a) * pinch, z * (z > 0f ? 1.25f : 1f)) * .5f);
                }
            }
            var tris = new List<int>();
            for (int r = 0; r < rings; r++)
                for (int s = 0; s < sides; s++)
                {
                    int a = r * sides + s, b = r * sides + (s + 1) % sides, c = a + sides, d = b + sides;
                    tris.AddRange(new[] { a, b, c, b, d, c });
                }
            var mesh = new Mesh { name = "AmihanBirdHead" };
            mesh.SetVertices(verts); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// THE BIRD'S FLIGHT on the story clock, continuous from its birth to the lane (v7 r4: three separate branches jumped
        /// 2.5 m at the swoop and snapped between three flapping rhythms). Born where the vortex closes, half a circle down her
        /// left past the lens (widening from where it was born), settling above and ahead of her; one flapping rhythm whose
        /// depth and centre ease from strokes to a glide past the lens to the raised, trembling wings of the wind-up; then the
        /// wingbeat on her drive and away down the lane.
        /// </summary>
        private void AmBirdPose(float story, bool calm, out Vector3 at, out Quaternion turn, out float flap, out float on)
        {
            var court = Vector3.up * _amCourt;
            on = Ease(AmFormAt, AmFormAt + .2f, story);
            float since = story - AmReleaseAt;
            // One rhythm: its depth and centre eased between the phases.
            float glide = Ease(3.06f, 3.16f, story) * (1f - Ease(3.30f, 3.42f, story));
            float raise = Ease(AmWindAt, AmWindAt + .4f, story);
            float depth = Mathf.Lerp(Mathf.Lerp(42f, 8f, glide), 4f, raise) * (calm ? .3f : 1f);
            // v7 r6: raised to 78 degrees the wings were edge-on to the wind-up lens (a haze); a wide V, the bird rearing.
            float centre = Mathf.Lerp(Mathf.Lerp(22f, 8f, glide), 48f, raise);
            flap = centre + depth * Mathf.Sin(story * 10.5f);

            float u = Ease(AmSwoopAt, AmSwoopEnd, story);
            float a = (180f + 180f * u) * Mathf.Deg2Rad;
            float r = Mathf.Lerp(-AmBirdBorn.z, AmSwoopRadius, Ease(0f, .35f, u));
            float h = Mathf.Lerp(Mathf.Lerp(AmBirdBorn.y, 2.3f, Mathf.Sin(u * Mathf.PI * .8f)), AmBirdHover.y, u * u * u);
            var path = new Vector3(Mathf.Sin(a) * r, h, Mathf.Cos(a) * r * Mathf.Lerp(1f, .5f, u * u));
            var tangent = new Vector3(Mathf.Cos(a), 0f, -Mathf.Sin(a));
            float swooping = Ease(AmSwoopAt - .05f, AmSwoopAt + .1f, story);
            float settle = Ease(AmSwoopEnd - .1f, AmSwoopEnd + .25f, story);
            float bob = (calm ? 0f : .25f) * Mathf.Sin(story * 3f) * (1f - swooping) + (calm ? 0f : .1f) * Mathf.Sin(story * 10.5f + 1.2f) * settle * (1f - raise);
            at = Vector3.Lerp(Vector3.Lerp(AmBirdBorn, path, swooping), AmBirdHover + Vector3.up * (.35f * raise), settle) + court + Vector3.up * bob;
            var facing = Vector3.Slerp(Vector3.Slerp(Vector3.forward, tangent, swooping), Vector3.forward, settle);
            float bank = -34f * swooping * (1f - settle) * Mathf.Sin(Mathf.Clamp01(u) * Mathf.PI);
            turn = Quaternion.LookRotation(facing, Vector3.up) * Quaternion.Euler(-8f - 32f * raise, 0f, bank);

            if (since >= 0f)
            {
                // THE WINGBEAT on her drive, then on down the lane over the fan, dissolving.
                float down = Ease(0f, .12f, since);
                at += Vector3.down * (.9f * down) + Vector3.forward * (since * 14f);
                turn = Quaternion.Euler(-40f + 50f * down, 0f, 0f);
                flap = Mathf.Lerp(flap, -42f, down) + 10f * Ease(.2f, .5f, since);
                // v15: gone before the finish close-up (its trails swept across her face as the lens cut in).
                on *= 1f - Ease(.2f, .36f, since);
            }
        }

        /// <summary>A wing point: <paramref name="x"/> out along the wing, <paramref name="z"/> back, raised by the flap.</summary>
        private static Vector3 AmWing(float side, float raise, Vector3 shoulder, float x, float y, float z)
        {
            float r = raise * Mathf.Deg2Rad;
            return shoulder + new Vector3(side * x * Mathf.Cos(r) - side * y * Mathf.Sin(r), x * Mathf.Sin(r) + y * Mathf.Cos(r), z);
        }

        /// <summary>The leading edge at <paramref name="f"/> (0 the shoulder, 1 the tip), in two bones, the tip lagging the arm.</summary>
        private static Vector3 AmEdge(float side, float flap, float f)
        {
            var shoulder = new Vector3(side * .3f, .14f, .25f);
            if (f < .45f) return AmWing(side, flap, shoulder, f / .45f * 1.5f, 0f, .1f * f / .45f);
            var wrist = AmWing(side, flap, shoulder, 1.5f, 0f, .1f);
            float g = (f - .45f) / .55f;
            return AmWing(side, flap * 1.3f, wrist, g * 2.0f, 0f, -.6f * g * g);
        }

        /// <summary>The trailing edge, scalloped: a feather tip at every other sample, a notch between.</summary>
        private static Vector3 AmTrailing(float side, float flap, int i)
        {
            float f = i / (AmEdgeSamples - 1f);
            var root = AmEdge(side, flap, f);
            float raise = (f < .45f ? flap : flap * 1.3f) - 6f;
            float rake = Mathf.Lerp(0f, 58f, f) * Mathf.Deg2Rad;
            // v16 b: the outer half is the primaries, long fingers with deep gaps between (the mark of a bird's wing); the inner
            // half the secondaries, shallow scallops.
            bool primaries = f >= .5f;
            float notch = primaries ? .42f : .86f;
            float length = (primaries ? Mathf.Lerp(1.35f, 2.0f, (f - .5f) * 2f) * (1f - .3f * Mathf.Pow(f, 8f)) : Mathf.Lerp(1.05f, 1.3f, f * 2f))
                * (i % 2 == 0 ? 1f : notch);
            if (i == 0) length = .95f;
            return AmWing(side, raise, root, Mathf.Sin(rake) * length, -.06f, -Mathf.Cos(rake) * length);
        }

        private void SampleAmihanBird(float story, bool calm, Vector3 at, Quaternion turn, float flap, float on)
        {
            float scale = AmBirdScale;
            // It forms from the inside out: the body first, then the wings unfold from the shoulders, the tail streams out.
            float body = on * Ease(AmFormAt, AmFormAt + .14f, story);
            float unfold = Ease(AmFormAt + .04f, AmFormAt + .26f, story);
            float wingFlap = Mathf.Lerp(-60f, flap, unfold);
            for (int w = 0; w < 2; w++)
            {
                float side = w == 0 ? -1f : 1f;
                for (int i = 0; i < AmEdgeSamples; i++)
                {
                    float f = i / (AmEdgeSamples - 1f) * Mathf.Lerp(.3f, 1f, unfold);
                    _amWingVerts[i] = AmEdge(side, wingFlap, f);
                    _amEdgePoints[i] = at + turn * (_amWingVerts[i] * scale);
                    var trailing = AmTrailing(side, wingFlap, i);
                    _amWingVerts[AmEdgeSamples + i] = Vector3.Lerp(_amWingVerts[i], trailing, unfold);
                    _amTipPoints[i] = at + turn * (_amWingVerts[AmEdgeSamples + i] * scale);
                }
                _amWingTips[w].SetPositions(_amTipPoints);
                _amWingTips[w].widthMultiplier = .045f * scale * body * unfold;
                _amWingTips[w].enabled = body * unfold > .02f;
                _amWingMesh[w].vertices = _amWingVerts;
                _amWingMesh[w].RecalculateBounds();
                Place(_amWing[w], at, Vector3.one * scale, turn, body);
                _amWingEdge[w].SetPositions(_amEdgePoints);
                _amWingEdge[w].widthMultiplier = .11f * scale * body;
                _amWingEdge[w].enabled = body > .02f;
                // Three veins down each wing, the feathers' lines.
                for (int v = 0; v < 3; v++)
                {
                    var line = _amWingVein[w * 3 + v];
                    int idx = 4 + v * 6;
                    var root = _amWingVerts[idx]; var tip = _amWingVerts[AmEdgeSamples + idx];
                    for (int i = 0; i < AmStrokeSamples; i++) _amStrokePoints[i] = at + turn * (Vector3.Lerp(root, tip, i / (AmStrokeSamples - 1f) * .9f) * scale);
                    line.SetPositions(_amStrokePoints);
                    line.widthMultiplier = .04f * scale * body * unfold;
                    line.enabled = body * unfold > .02f;
                }
            }
            Place(_amBody, at, Vector3.one * scale, turn, body);
            Place(_amHead, at + turn * (AmHeadAt * scale), AmHeadSize * scale, turn, body);
            Place(_amBeakPiece, at + turn * (new Vector3(0f, .58f, 1.84f) * scale), new Vector3(.09f, .28f, .09f) * scale,
                turn * Quaternion.Euler(98f, 0f, 0f), body);

            // THE TAIL STREAMERS and THE WING-TIP TRAILS follow the path it really flew, a few hundredths of a second at a time.
            for (int k = 0; k < _amTail.Length; k++)
            {
                float offset = (k - 2) * .16f;
                for (int i = 0; i < AmTrailSamples; i++)
                {
                    float back = i * .04f;
                    AmBirdPose(story - back, calm, out var pAt, out var pTurn, out _, out _);
                    float wave = calm ? 0f : .12f * Mathf.Sin(story * 7f - i * .7f + k);
                    // v7 r7: hovering, a resting length of 4.5 m draped the streamers across the wind-up lens; the flight path gives the length.
                    // Fanned: the outer plumes spread and lie a little shorter.
                    _amTrailPoints[i] = pAt + pTurn * (new Vector3(offset * (1f + .35f * i), -.02f * i + wave, -1.0f - (.1f - .012f * Mathf.Abs(k - 2)) * i) * scale);
                }
                _amTail[k].SetPositions(_amTrailPoints);
                _amTail[k].widthMultiplier = (k == 2 ? .36f : .3f) * scale * body * Ease(AmFormAt + .08f, AmFormAt + .3f, story);
                _amTail[k].enabled = body > .02f;
            }
            for (int w = 0; w < 2; w++)
            {
                float side = w == 0 ? -1f : 1f;
                for (int i = 0; i < AmTrailSamples; i++)
                {
                    // v16: a third of a second drew long straight bars across the lens; a fifth is the arc of the stroke.
                    float back = i * .018f;
                    AmBirdPose(story - back, calm, out var pAt, out var pTurn, out float pFlap, out _);
                    _amTrailPoints[i] = pAt + pTurn * (AmEdge(side, pFlap, 1f) * scale);
                }
                _amTipTrail[w].SetPositions(_amTrailPoints);
                _amTipTrail[w].widthMultiplier = .1f * scale * body * unfold;
                _amTipTrail[w].enabled = body * unfold > .02f;
            }
            // THE CREST: three plumes swept back off its head, streaming.
            for (int c = 0; c < _amCrest.Length; c++)
            {
                for (int i = 0; i < AmStrokeSamples; i++)
                {
                    float u = i / (AmStrokeSamples - 1f);
                    float stream = calm ? 0f : .07f * Mathf.Sin(story * 11f + c - u * 3f) * u;
                    _amStrokePoints[i] = at + turn * (new Vector3((c - 1) * .1f, .84f + .3f * u * u + stream, 1.42f - .9f * u) * scale);
                }
                _amCrest[c].SetPositions(_amStrokePoints);
                _amCrest[c].widthMultiplier = .07f * scale * body;
                _amCrest[c].enabled = body > .02f;
            }

            // THE EYES, the climax as in Paete's tree and Phaister's doll: alight as it forms, blazing as it passes the lens and
            // as it raises its wings over her; its heart a soft glow inside the body.
            float blaze = 1f + 1.2f * Mathf.Max(Ease(AmEyesFrom - .05f, AmEyesFrom + .05f, story) * (1f - Ease(AmEyesTo, AmEyesTo + .15f, story)),
                .8f * Ease(AmWindAt, AmWindAt + .3f, story));
            float eyes = on * Ease(AmFormAt + .12f, AmFormAt + .3f, story) * blaze * (calm ? .6f : 1f);
            var eyeSize = Vector3.one * .17f * scale * Mathf.Min(blaze, 1.3f);
            PlaceGlow(_amEyeL, at + turn * (new Vector3(-.24f, .68f, 1.62f) * scale), eyeSize, Quaternion.identity, eyes);
            PlaceGlow(_amEyeR, at + turn * (new Vector3(.24f, .68f, 1.62f) * scale), eyeSize, Quaternion.identity, eyes);
            float charging = Ease(AmWindAt, AmReleaseAt - .05f, story) * (1f - Ease(AmReleaseAt, AmReleaseAt + .1f, story));
            float throb = calm ? 0f : .15f * Mathf.Sin(story * 18f) * charging;
            PlaceGlow(_amHeart, at + turn * (new Vector3(0f, .2f, .3f) * scale), Vector3.one * (2.0f + 1.4f * charging + throb) * scale, Quaternion.identity,
                (.55f + 1.1f * charging) * on * (calm ? .5f : 1f));
        }

        // ------------------------------------------------------------------ the burst

        /// <summary>On the drive: arcs racing down the lane off her palms (story time, so they hang in the slow motion).</summary>
        private void SampleAmihanBurst(float since, bool calm, float keep)
        {
            var palms = FreePalm;
            for (int j = 0; j < _amRing.Length; j++)
            {
                var line = _amRing[j];
                if (since < 0f || since >= .55f) { line.enabled = false; continue; }
                var centre = palms + Vector3.forward * (since * (9f + 2.5f * j)) + Vector3.up * (since * (.4f * j - .6f));
                float r = .3f + since * (4.5f + j);
                for (int i = 0; i < AmRingSamples; i++)
                {
                    float a = Mathf.Lerp(-55f, 55f, i / (AmRingSamples - 1f)) * Mathf.Deg2Rad;
                    _amRingPoints[i] = centre + new Vector3(Mathf.Sin(a) * r, 0f, Mathf.Cos(a) * r * .45f);
                }
                line.SetPositions(_amRingPoints);
                // v15: `keep` clears them before the finish close-up, whose lens stands in their path (bands across her face).
                line.widthMultiplier = .09f * (1f - Ease(.08f, .55f, since)) * (calm ? .7f : 1f) * keep;
                line.enabled = line.widthMultiplier > .001f;
            }
        }

        // ------------------------------------------------------------------ the leaves

        private void SampleAmihanLeaves(float t, float story, bool calm)
        {
            float since = story - AmReleaseAt;
            float tumble0 = calm ? 0f : story;
            // THE LEAF IN HER BREEZE: lifted off the court, it circles her lazily while she floats; when her breeze dies it drops
            // straight down, rocking, as a leaf falls in still air. Hers is not enough: she shrugs, and whistles for the monsoon.
            {
                float lift = LiftAt(t);
                bool on = t > AmFloatFrom && t < AmLeafTo;
                Vector3 at;
                Quaternion rot;
                if (t < AmFloatTo)
                {
                    float s = t - AmFloatFrom;
                    float a = 2.4f + s * 3.4f, radius = Mathf.Lerp(1.4f, .8f, Ease(0f, .6f, s));
                    at = new Vector3(Mathf.Sin(a) * radius, _amCourt + Mathf.Lerp(.05f, .9f + lift, Ease(0f, .5f, s)) + .12f * Mathf.Sin(s * 5f), Mathf.Cos(a) * radius);
                    rot = Quaternion.Euler(30f * Mathf.Sin(s * 4f), a * Mathf.Rad2Deg + 90f, 35f + 20f * Mathf.Cos(s * 5f));
                }
                else
                {
                    float a = 2.4f + (AmFloatTo - AmFloatFrom) * 3.4f, u = Mathf.InverseLerp(AmFloatTo, AmLeafTo, t);
                    var from = new Vector3(Mathf.Sin(a) * .8f, _amCourt + .9f + .12f * Mathf.Sin((AmFloatTo - AmFloatFrom) * 5f), Mathf.Cos(a) * .8f);
                    at = new Vector3(from.x + .08f * Mathf.Sin(t * 5.5f), Mathf.Lerp(from.y, _amCourt + .03f, u), from.z);
                    rot = Quaternion.Euler(18f * Mathf.Sin(t * 5.5f), 30f, 24f * Mathf.Cos(t * 5.5f));
                }
                Place(_amLeaf[0], at, new Vector3(.09f, .014f, .13f), rot, on ? 1f : 0f);
            }
            var centre = AmVortexCentre(t);
            float close = Ease(AmGatherFrom + .1f, AmFormAt + .1f, t);
            for (int i = 1; i < AmLeaves; i++)
            {
                var seed = _amLeafSeed[i];
                Vector3 at; bool on;
                float tumble = tumble0, keep = 1f;
                if (story < AmSwoopAt)
                {
                    // TORN UP BY THE MONSOON: circling the plaza in its ring, closing and rising into the vortex.
                    float s = Mathf.Max(0f, t - AmGatherFrom - .25f * seed.x);
                    float r = Mathf.Lerp(3.5f + 7f * seed.z, .6f + .8f * seed.y, close * close);
                    float a = seed.x * 6.28f - (calm ? 0f : s * 1.3f + s * s * 1.6f);
                    float h = Mathf.Lerp(.2f + 3.5f * seed.y, -.5f + 1.5f * seed.y, close);
                    at = centre + new Vector3(Mathf.Sin(a) * r, h, Mathf.Cos(a) * r);
                    on = t > AmGatherFrom + .25f * seed.x;
                    // v16: drawn into the swell of light they were dark green confetti round the newborn bird (owner, twice:
                    // "blocks"). They shrink into the light as the vortex closes, each a little before the next.
                    keep = 1f - Ease(AmFormAt - .3f + .15f * seed.y, AmFormAt + .02f, t);
                    on &= keep > .02f;
                }
                else if (since < 0f)
                {
                    // v11 (owner: "what are those blocks on the phoneix"): wheeling round the bird the leaves read as blocks stuck
                    // to it. From the swoop to the drive they are gone: the bird is clean.
                    at = Vector3.zero;
                    on = false;
                }
                else
                {
                    // FLUNG down the lane by the wingbeat, fanned across its 60 degrees, rising, then falling; slow in the hang.
                    float yaw = (seed.x - .5f) * 60f * Mathf.Deg2Rad;
                    var dir = new Vector3(Mathf.Sin(yaw), .1f + .3f * seed.y, Mathf.Cos(yaw));
                    float speed = 8f + 7f * seed.z;
                    // Blown out from before her palms down the lane, not off the bird.
                    var origin = new Vector3(0f, 1.1f + LiftAt(t) + _amCourt, 1.2f) + new Vector3(seed.y - .5f, (seed.z - .5f) * .8f, seed.x * .8f);
                    at = origin + dir * (speed * since) + Vector3.down * (3.5f * since * since);
                    // v15 (owner: "what are those blockss"): flung past the lens in the hang the leaves read as blocks. None in the
                    // throw: the streaks, the wingbeat and the thrown players carry it.
                    on = false;
                    tumble += since * 4f;
                }
                if (!on) { Place(_amLeaf[i], Vector3.zero, Vector3.one * .001f, Quaternion.identity, 0f); continue; }
                var spin = Quaternion.Euler(tumble * (190f + 120f * seed.x) + i * 37f, tumble * (260f + 90f * seed.y) + i * 53f, tumble * 140f + i * 11f);
                Place(_amLeaf[i], at, new Vector3(.1f, .016f, .15f) * ((.85f + .35f * seed.z) * keep), spin, keep);
            }

            // THE FEATHER: after the bird has gone down the lane, one feather drifts down past her, rocking.
            {
                bool on = t > AmReleaseAt + AmHangTo - .05f;
                float u = Mathf.InverseLerp(AmReleaseAt + AmHangTo - .05f, Seconds + .4f, t);
                var at = new Vector3(.55f + .18f * Mathf.Sin(t * 5f), Mathf.Lerp(2.6f, 1.2f, u) + _amCourt, .55f + .1f * Mathf.Cos(t * 4f));
                Place(_amFeather, at, new Vector3(.07f, .012f, .22f), Quaternion.Euler(20f * Mathf.Sin(t * 5f), 40f, 28f * Mathf.Cos(t * 5f)), on ? 1f : 0f);
            }
        }

        // ------------------------------------------------------------------ the lens

        /// <summary>THE COMPUTED SHOT: the cut in on the bird's face as it swoops past her.</summary>
        private void AmihanFrame(int index, float t, ref Vector3 eye, ref Vector3 look, ref float fov)
        {
            if (index == AmShotBird && t >= AmEyesFrom && t < AmEyesTo)
            {
                // THE CLIMAX: it sweeps across the sky right over her. v9 (owner on the tight face shot: "tf is that haha refione
                // that"): at that range its simple head fell apart. Now a low lens beside her at her left front, her in the
                // lower frame reaching up and the whole bird crossing above her, its eyes blazing: the silhouette is the moment.
                AmBirdPose(t, _reducedEffects, out var at, out _, out _, out _);
                var her = new Vector3(0f, 1.2f + _amCourt, 0f);
                eye = new Vector3(-1.5f, .55f + _amCourt, 2.4f);
                look = Vector3.Lerp(her, at, .62f);
                fov = 62f;
                return;
            }
            // v8 (owner: "close up of her"): the finish is the authored close-up row, her cute pose, not a computed wide.
        }

        /// <summary>A warm afternoon that turns bright and cool-vivid as the monsoon answers, flares as the bird passes and on the
        /// drive, and is richer through the hang, so the held moment feels held. Never darker.</summary>
        private void AmihanGrade(float t, out float brightness, out float saturation)
        {
            float monsoon = Ease(AmGatherFrom, AmFormAt, t) * (1f - Ease(AmReleaseAt + AmHangTo, Seconds, t));
            float beat = Mathf.Max(AmBeat(t, AmReleaseAt, .3f), Mathf.Max(.6f * AmBeat(t, AmWhistleAt, .25f), .5f * AmBeat(t, 3.18f, .3f)));
            float hang = Ease(AmReleaseAt + AmHangFrom - .05f, AmReleaseAt + AmHangFrom + .05f, t)
                * (1f - Ease(AmReleaseAt + AmHangTo, AmReleaseAt + AmHangTo + .1f, t));
            brightness = 1f + .05f * monsoon + .08f * beat + .03f * hang;
            saturation = 1f + .14f * monsoon + .12f * hang;
        }

        /// <summary>The lens takes the monsoon's arrival, the bird's pass, the wingbeat and the release, small and quickly gone.</summary>
        private static Vector3 AmihanShake(float t)
        {
            Vector3 Kick(float at, float size, float hz)
            {
                float s = t - at;
                if (s < 0f || s > .3f) return Vector3.zero;
                float fall = Mathf.Exp(-s * 14f) * size;
                return new Vector3(Mathf.Sin(s * hz) * fall, Mathf.Sin(s * hz * 1.3f + 1.1f) * fall * .6f, 0f);
            }
            return Kick(AmArriveAt + .3f, .02f, 64f) + Kick(3.2f, .035f, 52f) + Kick(AmReleaseAt, .09f, 46f);
        }
    }
}
