using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // BASILIO, CONTINENTAL DRIFT: THE CHARGE-UP (v6, 2026-10-17). The owner on v5: *"where does the rock hands come from , he
        // js has them"*, *"why the fk are ppl knocked back already annd they js stay them"*, then *"after nna malaunch ung ult, end
        // nna dapat cutscene tapos ppl see the rock going towards them and maknoknnock away sila and concuss"*, *"so focus on
        // directing his charge up and vfx related to it brahh"*. So the cutscene is HIM, gathering the earth, and it ends as he
        // launches it; the cascade (the bands, the knock-away, Concussed) is play's alone (`DanteDriftLive`, `DanteDriftWave`).
        //
        //   0.30 THE PLANT        he drives both fists into the court: a crack star under each, the court shudders
        //   0.35 THE CRACKS       molten cracks spider out from his fists across the court round him
        //   0.55 THE EARTH ANSWERS stone tears up out of the far end of each crack and flies onto his fists, three waves, each
        //                         landing with a jolt; loose pebbles lift off the court and hang, then are pulled in. Where the
        //                         rock hands come from is on screen.
        //   1.65 THE RISE         he stands with the boulder fists overhead, molten light in their seams, the cracks pulsing
        //   2.09 THE LEAP         he jumps high against the sky (a crack, a ring and dust where he left)
        //   3.25 THE SLAM         seen high from behind him, down the lane at the real players where they stand: he lands fists
        //                         first, the stone on his fists is driven back into the court (that earth is what erupts, and why
        //                         his hands are bare in play), dust bursts out, the crack leaps forward
        //   3.33 THE GROUND RISES the first band heaves at his fists, the crack racing on at them; the cut comes as it rises, before
        //                         it reaches anyone, and play's cascade carries on from that very frame (`DanteDriftLive` is
        //                         built here on the live lane and posed with play's own clock; the wave starts
        //                         `GeoRules.DriftCutsceneTail` in). The owner on v6: *"dante's ult dont make sense wwtf"*, *"why
        //                         does the earthquake or ground up come after 10000 years dapat it happens right after he goes
        //                         down"*, *"dont force it to 7 seconds"*, *"sa end ng ult ni dante dapat nakikita na ground is
        //                         rising ito tapos it resumes nnalang"*, *"end the cutscene when the ground is rsiing towards the
        //                         players"*
        //
        // RULES (the method's traps): nothing here runs on `Update`, everything is posed from the scene clock; flat effects sit on
        // the court; light is cut INTO stone, never raised; the hottest colour is (1, .62, .18), never white; stone never fades:
        // it is torn up, flies, and stays on his fists (the pebbles are pulled into them).
        // =========================================================================================
        private const float DnPlant = .30f, DnCracks = .35f, DnRise = 1.65f, DnLeap = 2.09f, DnSlam = 3.25f, DnBand0 = 3.33f, DnEnd = DnBand0 + Core.GeoRules.DriftCutsceneTail;
        /// <summary>The three waves of stone off the court onto his fists (the gauntlet rows' steps).</summary>
        private static readonly float[] DnWaves = { .58f, .88f, 1.18f };
        private const float DnFly = .26f;

        // The court's own surface over the stage root (`Slipper.GroundY`): Bayan Plaza's court stands above the root.
        private float _dnCourt;
        private float DnFloat => _dnCourt + .03f;

        // His fists' stone: (which fist 0 left 1 right, offset from the palm, size, the wave it comes on 0 to 2, seed).
        private static readonly (int fist, Vector3 offset, float size, int step, int seed)[] DnGauntletRows =
        {
            (0, new Vector3(-.06f, .02f, .05f), .17f, 0, 3), (0, new Vector3(.05f, .05f, .02f), .15f, 1, 5), (0, new Vector3(0f, -.06f, .06f), .14f, 2, 7), (0, new Vector3(-.03f, .08f, -.04f), .12f, 2, 9),
            (1, new Vector3(.06f, .02f, .05f), .17f, 0, 11), (1, new Vector3(-.05f, .05f, .02f), .15f, 1, 13), (1, new Vector3(0f, -.06f, .06f), .14f, 2, 17), (1, new Vector3(.03f, .08f, -.04f), .12f, 2, 19),
        };
        // The cracks round him, one per gauntlet stone: (angle round him, degrees from forward, length m). The stone tears up at
        // the far end; the left fist's stones come from his left, the right's from his right.
        // Each crack runs in three jagged legs, bent by its own typed angles (film r35: one straight leg read as orange tape).
        private static readonly float[][] DnCrackBends = { new[] { 0f, 22f, -16f }, new[] { 0f, -18f, 24f }, new[] { 0f, 15f, -25f }, new[] { 0f, -24f, 12f } };
        private static readonly (float angle, float length)[] DnCrackRows =
        {
            (-62f, 2.4f), (-118f, 2.1f), (-25f, 2.7f), (-160f, 1.9f),
            ( 58f, 2.5f), ( 122f, 2.0f), ( 22f, 2.8f), ( 165f, 1.8f),
        };
        // Pebbles lifted off the court as the earth answers: (angle, radius, hover height, lift time, seed).
        private static readonly (float angle, float radius, float height, float lift, int seed)[] DnPebbleRows =
        {
            (-40f, 1.3f, .55f, .62f, 31), (35f, 1.5f, .8f, .70f, 33), (-95f, 1.8f, .4f, .78f, 35), (100f, 1.6f, .95f, .66f, 37),
            (-145f, 1.2f, .7f, .84f, 39), (150f, 1.4f, .5f, .74f, 41), (-10f, 2.1f, 1.1f, .90f, 43), (15f, 1.9f, .35f, .96f, 45),
            (-70f, 2.4f, .9f, 1.02f, 47), (75f, 2.2f, .65f, 1.08f, 49), (-125f, 2.0f, 1.0f, .94f, 51), (130f, 2.3f, .45f, 1.0f, 53),
        };

        // EMBERS (owner on v8: *"kinda boring to look at compared to other characrers"*): sparks lift off the cracks and stream up
        // into his fists while the earth answers, and spray off the slam down the lane. (crack they rise from, seconds per trip,
        // phase, arc height, size; at the slam: degrees off forward, out speed, up speed.)
        private static readonly (int crack, float period, float phase, float arc, float size, float spray, float out_, float up)[] DnEmberRows =
        {
            (0, .82f, .05f, .55f, .10f, -38f, 4.2f, 4.6f), (4, .78f, .55f, .65f, .09f,  34f, 4.6f, 5.2f), (2, .90f, .30f, .40f, .11f, -12f, 5.4f, 3.8f),
            (6, .86f, .80f, .50f, .08f,  15f, 5.1f, 4.4f), (1, .74f, .15f, .70f, .09f, -62f, 3.6f, 5.6f), (5, .94f, .65f, .45f, .10f,  58f, 3.9f, 5.0f),
            (3, .80f, .40f, .60f, .08f, -24f, 4.9f, 6.0f), (7, .88f, .95f, .35f, .11f,  26f, 4.4f, 3.6f), (0, .96f, .70f, .75f, .07f,  -4f, 6.0f, 4.9f),
            (4, .84f, .20f, .50f, .09f,   6f, 5.7f, 5.5f), (2, .76f, .85f, .60f, .08f, -48f, 4.0f, 4.2f), (6, .92f, .45f, .40f, .10f,  45f, 4.3f, 4.7f),
            (1, .88f, .10f, .55f, .07f, -78f, 3.2f, 5.3f), (5, .80f, .60f, .70f, .08f,  80f, 3.3f, 4.1f), (3, .86f, .35f, .45f, .09f, -30f, 5.9f, 3.9f),
            (7, .90f, .75f, .50f, .08f,  20f, 6.2f, 5.8f),
        };

        private readonly List<int> _dnGauntlet = new List<int>(8), _dnFistGlow = new List<int>(2), _dnCracks = new List<int>(8),
                                   _dnTearDust = new List<int>(8), _dnPebbles = new List<int>(12), _dnLift = new List<int>(4),
                                   _dnPlantStars = new List<int>(2), _dnSlamDust = new List<int>(6), _dnEmbers = new List<int>(16);
        private int _dnSlamStar, _dnSlamRing, _dnSlamRingOuter, _dnLiftStar, _dnLiftRing, _dnGroundGlow;
        private DanteDriftLive _dnLane;
        private Material _dnImpact;
        private Vector3? _dnFistAt;
        private readonly Vector3[] _dnPlantAt = new Vector3[2];
        private bool _dnPlanted;

        private void BuildDante()
        {
            float court = Slipper.GroundY(_root.transform.position + Vector3.up * .3f) - _root.transform.position.y;
            _dnCourt = float.IsInfinity(court) || float.IsNaN(court) ? 0f : Mathf.Clamp(court, 0f, .6f);

            var basalt = new[] { new Color(.29f, .24f, .2f, 1f), new Color(.34f, .29f, .25f, 1f), new Color(.24f, .2f, .17f, 1f) };
            var molten = new Color(1f, .45f, .09f);
            // The light under him that grows as the earth answers.
            _dnGroundGlow = AddGlow("DanteGroundGlow", new Color(1f, .42f, .08f), billboard: false, falloff: 1.5f, core: .5f);
            for (int f = 0; f < 2; f++) _dnPlantStars.Add(AddGlow("DantePlantStar" + f, molten, billboard: false, falloff: 1.6f, core: .6f));
            for (int i = 0; i < DnCrackRows.Length * 3; i++) _dnCracks.Add(AddGlow("DanteCrack" + i, new Color(1f, .5f, .1f), billboard: false, band: true, falloff: 1.7f, core: .7f));
            for (int i = 0; i < DnCrackRows.Length; i++)
                _dnTearDust.Add(Add("DanteTearDust" + i, VfxShapes.TwoSided(VfxShapes.Splat(9, .48f, 301 + i * 7)), new Color(.5f, .37f, .24f, .72f), .3f, plain: true));
            foreach (var g in DnGauntletRows)
                _dnGauntlet.Add(BasilioStone("DanteGauntlet" + g.seed, VfxShapes.Prism(5, 1f, .72f, .3f, 0f, g.seed), basalt[g.seed % 3]));
            foreach (var p in DnPebbleRows)
                _dnPebbles.Add(BasilioStone("DantePebble" + p.seed, VfxShapes.Prism(5, 1f, .62f, .32f, 0f, p.seed), basalt[p.seed % 3]));
            for (int f = 0; f < 2; f++)
                _dnFistGlow.Add(AddGlow("DanteFistGlow" + f, new Color(1f, .5f, .12f), falloff: 2.2f, core: .8f));
            // THE LEAP: where he leaves the court.
            _dnLiftStar = AddGlow("DanteLiftStar", molten, billboard: false, falloff: 1.6f, core: .6f);
            _dnLiftRing = AddGlow("DanteLiftRing", new Color(1f, .5f, .12f), DnRingMesh, billboard: false, band: true, falloff: 1.2f, core: .4f);
            for (int i = 0; i < 4; i++)
                _dnLift.Add(Add("DanteLiftDust" + i, VfxShapes.TwoSided(VfxShapes.Splat(9, .48f, 401 + i * 7)), new Color(.5f, .37f, .24f, .72f), .3f, plain: true));
            // THE SLAM: a crack star and a ring under the fists, and the crack that starts down the lane (play's fuse goes on).
            _dnSlamStar = AddGlow("DanteSlamStar", molten, billboard: false, falloff: 1.4f, core: .6f);
            _dnSlamRing = AddGlow("DanteSlamRing", new Color(1f, .5f, .12f), DnRingMesh, billboard: false, band: true, falloff: 1.2f, core: .4f);
            _dnSlamRingOuter = AddGlow("DanteSlamRingOuter", new Color(1f, .42f, .08f), DnRingMesh, billboard: false, band: true, falloff: 1.4f, core: .3f);
            for (int i = 0; i < DnEmberRows.Length; i++) _dnEmbers.Add(AddGlow("DanteEmber" + i, new Color(1f, .58f, .16f), falloff: 2.4f, core: .7f));
            // Play's own cascade, on the live lane, measured exactly as the wave will measure it.
            var origin = _source != null ? _source.transform.position : _root.transform.position;
            Abilities.DanteDriftWave.Lane(origin, _root.transform.forward, out float halfWidth, out float reach);
            _dnLane = DanteDriftLive.BuildStage(_root.transform, _dnCourt, halfWidth, reach);
            _dnLane.gameObject.SetActive(false);
            for (int i = 0; i < 6; i++)
                _dnSlamDust.Add(Add("DanteSlamDust" + i, VfxShapes.TwoSided(VfxShapes.Splat(9, .48f, 501 + i * 7)), new Color(.5f, .37f, .24f, .72f), .3f, plain: true));
            // The real players, where they stand (unclamped): the last shot looks down the lane at them.
            StageOthers(0f, 1000f);
        }

        /// <summary>An opaque lit stone with a small local fill, so the dusk stage cannot turn it into a black cutout.</summary>
        private int BasilioStone(string name, Mesh mesh, Color colour)
        {
            int index = AddSolid(name, mesh, colour);
            var material = _pieces[index].Renderer.sharedMaterial;
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", colour * .45f);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            Place(index, Vector3.zero, Vector3.zero, Quaternion.identity, 1f);
            return index;
        }

        private static Mesh _dnRingMesh;
        /// <summary>A flat ring in XZ, unit outer radius, its band thin, v across it.</summary>
        private static Mesh DnRingMesh
        {
            get
            {
                if (_dnRingMesh != null) return _dnRingMesh;
                const int Sides = 64;
                var v = new Vector3[(Sides + 1) * 2]; var uv = new Vector2[v.Length]; var tris = new int[Sides * 6];
                for (int i = 0; i <= Sides; i++)
                {
                    float a = i * Mathf.PI * 2f / Sides;
                    var d = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                    v[i * 2] = d * .9f; v[i * 2 + 1] = d;
                    uv[i * 2] = new Vector2(i / (float)Sides, 0f); uv[i * 2 + 1] = new Vector2(i / (float)Sides, 1f);
                    if (i == Sides) continue;
                    int b = i * 6, k = i * 2;
                    tris[b] = k; tris[b + 1] = k + 2; tris[b + 2] = k + 1; tris[b + 3] = k + 2; tris[b + 4] = k + 3; tris[b + 5] = k + 1;
                }
                _dnRingMesh = new Mesh { name = "BasilioRing", hideFlags = HideFlags.DontSave, vertices = v, uv = uv, triangles = tris };
                _dnRingMesh = VfxShapes.TwoSided(_dnRingMesh);
                return _dnRingMesh;
            }
        }

        // His fists: the left, and the right (or where the right's slipper is held).
        private Vector3 DnFistPoint(int fist) => fist == 0 ? DnLeftFist : RightPalm;

        // ⚠️ HIS LEFT FIST IS THE RIGHT ONE MIRRORED ON HIS OWN ARM (owner on v7: *"what happened baket one arm nalanh huhu"*). The
        // stage's free palm measured 0.11 m left of his middle while his right palm was 0.46 m right of it (film r43): the left
        // boulder stood inside his chest and only one fist ever showed. The right hand's offset from its own arm bone, mirrored,
        // and laid on the left arm bone follows the real left arm through every pose.
        private Transform _dnArmRight;
        private bool _dnArmSearched;
        private Vector3 DnLeftFist
        {
            get
            {
                if (!_dnArmSearched && _bodyRoot != null)
                {
                    _dnArmSearched = true;
                    foreach (var bone in _bodyRoot.GetComponentsInChildren<Transform>(true))
                        if (bone.name == "arm-right") { _dnArmRight = bone; break; }
                }
                if (_dnArmRight == null || _leftArm == null || _rightHand == null) return FreePalm;
                var local = _dnArmRight.InverseTransformPoint(_rightHand.position);
                local.x = -local.x;
                return _root.transform.InverseTransformPoint(_leftArm.TransformPoint(local));
            }
        }
        private Vector3 DnFistMid => (DnFistPoint(0) + DnFistPoint(1)) * .5f;
        private Vector3 DnFists => _dnFistAt ?? new Vector3(0f, DnFloat + .02f, .7f);

        /// <summary>Leg <paramref name="leg"/> (0 to 2) of crack <paramref name="i"/>: from his planted fist out to where its stone
        /// tears up, in three jagged legs.</summary>
        private void DnCrackLeg(int i, int leg, out Vector3 from, out Vector3 to)
        {
            var c = DnCrackRows[i];
            var start = _dnPlanted ? _dnPlantAt[DnGauntletRows[i].fist] : new Vector3(DnGauntletRows[i].fist == 0 ? -.3f : .3f, 0f, .55f);
            from = new Vector3(start.x, DnFloat + .012f, start.z);
            var bends = DnCrackBends[i % DnCrackBends.Length];
            to = from;
            for (int j = 0; j <= leg; j++)
            {
                from = to;
                float a = (c.angle + bends[j]) * Mathf.Deg2Rad;
                to = from + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * (c.length * (j == 0 ? .4f : .3f));
            }
        }

        /// <summary>Where crack <paramref name="i"/> ends: where its stone tears up.</summary>
        private Vector3 DnCrackEnd(int i) { DnCrackLeg(i, 2, out _, out var end); return end; }

        private static float DanteKick(float t, float at, float size, float fade)
            => t < at ? 0 : size * Mathf.Clamp01(1 - (t - at) / fade);

        /// <summary>The lens feels it: the plant, every wave of stone landing on his fists, the leap, the slam.</summary>
        private Vector3 DanteShake(float t)
        {
            float size = DanteKick(t, DnPlant, .09f, .2f) + DanteKick(t, DnLeap, .06f, .2f) + DanteKick(t, DnSlam, .18f, .3f);
            foreach (var w in DnWaves) size += DanteKick(t, w + DnFly, .045f, .16f);
            // A low tremble while the earth answers.
            size += .012f * Ease(DnCracks, DnWaves[0], t) * (1f - Ease(DnRise, DnLeap, t));
            return new Vector3(Mathf.Sin(t * 71f), Mathf.Sin(t * 93f + 1.3f), 0) * size;
        }

        /// <summary>The backdrop steps back as the earth answers, so the molten light reads; back up by the cut, as play is. Every
        /// hit flares it (the plant, each wave of stone landing, hardest the slam), so the beats land on the whole frame.</summary>
        private void DanteGrade(float t, out float brightness, out float saturation)
        {
            float back = Ease(DnCracks, DnWaves[0] + .2f, t) * (1f - Ease(DnSlam + .1f, DnEnd - .05f, t));
            float beat = Mathf.Max(DnBeat(t, DnSlam, .3f), .4f * DnBeat(t, DnPlant, .25f));
            foreach (var w in DnWaves) beat = Mathf.Max(beat, .3f * DnBeat(t, w + DnFly, .2f));
            brightness = Mathf.Lerp(1f, .8f, back) + .24f * beat;
            saturation = Mathf.Lerp(1f, .9f, back) + .14f * beat;
        }

        private static float DnBeat(float t, float at, float length) => t < at ? 0f : Mathf.Exp(-3f * (t - at) / length);

        /// <summary>The impact frame: two frames, ink on molten orange, lines in to the slam.</summary>
        private void DantePostProcess(RenderTexture frame, Camera camera, float t)
        {
            if (_reducedEffects || camera == null || t < DnSlam || t >= DnSlam + .067f) return;
            if (_dnImpact == null)
            {
                var shader = Resources.Load<Shader>("Shaders/DanteImpact");
                if (shader == null) return;
                _dnImpact = new Material(shader) { name = "DanteImpact" };
            }
            var vp = camera.WorldToViewportPoint(_root.transform.TransformPoint(DnFists));
            _dnImpact.SetFloat("_Impact", 1f);
            _dnImpact.SetVector("_Focus", new Vector4(Mathf.Clamp(vp.x, .1f, .9f), Mathf.Clamp(vp.y, .1f, .9f), 0f, 0f));
            _dnImpact.SetFloat("_Aspect", frame.height > 0 ? frame.width / (float)frame.height : 16f / 9f);
            _dnImpact.SetFloat("_Seed", 1f);
            var tmp = RenderTexture.GetTemporary(frame.descriptor);
            Graphics.Blit(frame, tmp, _dnImpact);
            Graphics.Blit(tmp, frame);
            RenderTexture.ReleaseTemporary(tmp);
        }

        private void SampleDante(float t)
        {
            // Where his fists meet the court, caught the first time the stage is sampled at the plant and at the slam.
            if (!_dnPlanted && t >= DnPlant && t < DnPlant + .4f)
            {
                for (int f = 0; f < 2; f++) { var p = DnFistPoint(f); _dnPlantAt[f] = new Vector3(p.x, DnFloat, Mathf.Max(.35f, p.z)); }
                _dnPlanted = true;
            }
            if (_dnFistAt == null && t >= DnSlam && t < DnSlam + .4f)
            {
                var mid = DnFistMid;
                _dnFistAt = new Vector3(mid.x, DnFloat + .02f, Mathf.Max(.4f, mid.z));
            }
            float charged = Ease(DnWaves[0], DnRise, t);
            float leftCourt = Ease(DnLeap, DnLeap + .5f, t);

            // THE PLANT: a crack star under each fist as they go in.
            for (int f = 0; f < 2; f++)
            {
                float age = t - DnPlant;
                var at = _dnPlanted ? _dnPlantAt[f] : new Vector3(f == 0 ? -.3f : .3f, DnFloat, .55f);
                PlaceGlow(_dnPlantStars[f], at + Vector3.up * .02f, Vector3.one * Mathf.Lerp(.4f, 1.1f, Mathf.Clamp01(age / .08f)),
                          Quaternion.Euler(90f, 0f, 0f), age < 0f ? 0f : 1.3f * (1f - .5f * Ease(DnRise, DnLeap + .4f, t)));
            }
            // The light under him, growing as the earth answers, gone as he leaves.
            PlaceGlow(_dnGroundGlow, new Vector3(0f, DnFloat + .01f, .3f), Vector3.one * Mathf.Lerp(1.5f, 4.5f, charged), Quaternion.Euler(90f, 0f, 0f),
                      Ease(DnCracks, DnWaves[0] + .3f, t) * (.9f + .25f * Mathf.Sin(t * 13f)) * (1f - leftCourt));

            // THE CRACKS spider out from his fists; each pulses as its stone tears up; they cool once he has leapt.
            for (int i = 0; i < DnCrackRows.Length; i++)
                for (int leg = 0; leg < 3; leg++)
                {
                    DnCrackLeg(i, leg, out var from, out var to);
                    // Each leg runs on from the last, so the crack is seen racing out and forking.
                    float start = DnCracks + .03f * (i % 4) + .1f * leg;
                    float grow = Ease(start, start + .14f, t);
                    var dir = to - from; float len = Mathf.Max(.01f, dir.magnitude * grow);
                    float tear = DnWaves[DnGauntletRows[i].step] + .02f * i;
                    float heat = grow <= 0f ? 0f : (1.1f + .9f * Mathf.Clamp01(1f - Mathf.Abs(t - tear) / .2f)) * (1f - Ease(DnLeap, DnSlam, t));
                    float yaw = 90f - Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                    // Thinner as it runs out.
                    float width = (.2f - .05f * leg) * (1f + .2f * ((i + leg) % 3 - 1));
                    PlaceGlow(_dnCracks[i * 3 + leg], from + dir.normalized * (len * .5f), new Vector3(len + .3f * grow, width, 1f), Quaternion.Euler(90f, yaw, 0f), heat);
                }

            // THE EARTH ANSWERS: each stone lies in the court at its crack's end, glowing under it; on its wave it tears up, flies
            // in an arc and lands on his fist with a jolt; it stays there.
            for (int i = 0; i < DnGauntletRows.Length; i++)
            {
                var g = DnGauntletRows[i];
                var bed = DnCrackEnd(i);
                float tear = DnWaves[g.step] + .02f * i;
                float fly = Mathf.Clamp01((t - tear) / DnFly), arrive = Ease(0f, 1f, fly);
                var fist = DnFistPoint(g.fist) + g.offset * 2.2f;
                var lying = bed + Vector3.up * (g.size * .6f * Ease(DnCracks + .3f, tear, t) - .08f);
                var at = Vector3.Lerp(lying, fist, arrive) + Vector3.up * (Mathf.Sin(fly * Mathf.PI) * .9f);
                float size = g.size * 2.3f * Mathf.Lerp(.75f, 1f, arrive) * (1f + .18f * Flash(t, tear + DnFly, .06f));
                // THE SLAM drives it back into the court: off his fist, into the ground round the impact, crumbling into it.
                if (t > DnSlam)
                {
                    float a = (g.seed * 47f) * Mathf.Deg2Rad;
                    var bury = DnFists + new Vector3(Mathf.Sin(a) * .5f, -g.size * .9f, .3f + Mathf.Cos(a) * .45f);
                    at = Vector3.Lerp(at, bury, Ease(DnSlam, DnSlam + .07f, t));
                    float crumble = Ease(DnSlam + .07f, DnSlam + .12f, t) * .2f + Ease(DnSlam + .13f, DnEnd, t) * .25f;
                    size *= 1f - crumble;
                }
                bool shown = t >= DnCracks + .3f;
                Place(_dnGauntlet[i], at, shown ? Vector3.one * size : Vector3.zero,
                      Quaternion.Euler(g.seed * 23f + 400f * fly, g.seed * 41f, g.seed * 13f + 260f * fly), 1f);
                // Dust where it tore out.
                float du = Mathf.Clamp01((t - tear) / .7f);
                float puff = Mathf.Lerp(.3f, 1.1f, du);
                Place(_dnTearDust[i], bed + Vector3.up * (puff * .4f), t < tear ? Vector3.zero : new Vector3(puff * 1.2f, 1f, puff),
                      Quaternion.Euler(0f, i * 45f, 0f) * Quaternion.Euler(90f, 0f, 0f), t < tear ? 0f : .75f * (1f - du * du));
            }
            // Loose pebbles lift off the court and hang, trembling, then are pulled into his fists as he rises.
            for (int i = 0; i < DnPebbleRows.Length; i++)
            {
                var p = DnPebbleRows[i];
                float a = p.angle * Mathf.Deg2Rad;
                var court = new Vector3(Mathf.Sin(a) * p.radius, DnFloat + .04f, .3f + Mathf.Cos(a) * p.radius);
                float up = Ease(p.lift, p.lift + .35f, t);
                var hang = court + Vector3.up * (p.height * up + .04f * Mathf.Sin(t * 17f + i));
                float pull = Ease(DnRise - .1f + .02f * i, DnRise + .2f + .02f * i, t);
                var fist = DnFistPoint(i % 2);
                var at = Vector3.Lerp(hang, fist, pull);
                Place(_dnPebbles[i], at, t < p.lift ? Vector3.zero : Vector3.one * .11f * (1f - pull),
                      Quaternion.Euler(t * 90f + i * 30f, i * 50f, t * 60f), 1f);
            }
            // The molten seams in the boulder fists: lit as the stone lands, bright as he rises, still burning in the air.
            for (int f = 0; f < 2; f++)
            {
                float veins = Ease(DnWaves[0] + DnFly, DnRise, t) * (1f + .6f * Ease(DnRise, DnRise + .2f, t)) * (1f - .5f * Ease(DnSlam + .1f, DnEnd, t));
                PlaceGlow(_dnFistGlow[f], DnFistPoint(f), Vector3.one * 1.4f, Quaternion.identity, veins * (1f + .2f * Mathf.Sin(t * 24f + f)));
            }

            // THE LEAP: where he left the court.
            {
                float age = t - DnLeap;
                PlaceGlow(_dnLiftStar, new Vector3(0f, DnFloat + .02f, 0f), Vector3.one * Mathf.Lerp(.5f, 1.3f, Mathf.Clamp01(age / .08f)),
                          Quaternion.Euler(90f, 0f, 0f), age < 0f ? 0f : 1.1f * (1f - Mathf.Clamp01(age / .9f)));
                float r = Mathf.Lerp(.3f, 2.4f, Mathf.Clamp01(age / .3f));
                PlaceGlow(_dnLiftRing, new Vector3(0f, DnFloat + .03f, 0f), new Vector3(r, 1f, r), Quaternion.identity, age < 0f ? 0f : 1f * (1f - Mathf.Clamp01(age / .35f)));
                for (int i = 0; i < _dnLift.Count; i++)
                {
                    float du = Mathf.Clamp01(age / .8f), a = i * 1.571f + .4f;
                    float puff = Mathf.Lerp(.5f, 1.5f, du);
                    var at = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * (.5f + 1.4f * du) + Vector3.up * (DnFloat + puff * .45f);
                    Place(_dnLift[i], at, age < 0f ? Vector3.zero : new Vector3(puff * 1.2f, 1f, puff), Quaternion.Euler(0f, a * Mathf.Rad2Deg, 0f) * Quaternion.Euler(90f, 0f, 0f),
                          age < 0f ? 0f : .8f * (1f - du * du));
                }
            }

            // THE SLAM: a crack star and a ring under the fists, and the crack that starts racing down the lane.
            {
                float age = t - DnSlam;
                PlaceGlow(_dnSlamStar, DnFists + Vector3.up * .02f, Vector3.one * Mathf.Lerp(.6f, 1.8f, Mathf.Clamp01(age / .08f)),
                          Quaternion.Euler(90f, 0f, 0f), age < 0f ? 0f : 1.4f * (1f - .5f * Mathf.Clamp01(age / .8f)));
                float r = Mathf.Lerp(.3f, 3.6f, Mathf.Clamp01(age / .35f));
                PlaceGlow(_dnSlamRing, DnFists + Vector3.up * .03f, new Vector3(r, 1f, r), Quaternion.identity, age < 0f ? 0f : 1.2f * (1f - Mathf.Clamp01(age / .4f)));
                // A second, wider ring a beat behind the first: the shock carrying on out across the court.
                float outer = age - .06f, ro = Mathf.Lerp(.5f, 6.5f, Ease(0f, .45f, outer));
                PlaceGlow(_dnSlamRingOuter, DnFists + Vector3.up * .025f, new Vector3(ro, 1f, ro), Quaternion.identity, outer < 0f ? 0f : .9f * (1f - Mathf.Clamp01(outer / .5f)));
                // Dust bursting out round the impact.
                for (int i = 0; i < _dnSlamDust.Count; i++)
                {
                    float du = Mathf.Clamp01(age / .5f), a = i * 1.047f + .3f;
                    float puff = Mathf.Lerp(.5f, 1.6f, du);
                    var at = DnFists + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * (.4f + 1.8f * du) + Vector3.up * (puff * .4f);
                    Place(_dnSlamDust[i], at, age < 0f ? Vector3.zero : new Vector3(puff * 1.2f, 1f, puff), Quaternion.Euler(0f, a * Mathf.Rad2Deg, 0f) * Quaternion.Euler(90f, 0f, 0f),
                          age < 0f ? 0f : .85f * (1f - du * du));
                }
            }

            // EMBERS: off the cracks into his fists while the earth answers; then sprayed off the slam, falling and dying out.
            for (int i = 0; i < _dnEmbers.Count; i++)
            {
                var e = DnEmberRows[i];
                if (t < DnSlam)
                {
                    float life = Ease(DnCracks + .15f, DnWaves[0] + .1f, t) * (1f - Ease(DnLeap - .1f, DnLeap + .15f, t));
                    float c = Mathf.Repeat((t - DnCracks) / e.period + e.phase, 1f);
                    var from = DnCrackEnd(e.crack) + Vector3.up * .05f;
                    var to = DnFistPoint(DnGauntletRows[e.crack].fist);
                    var at = Vector3.Lerp(from, to, c * c) + Vector3.up * (Mathf.Sin(c * Mathf.PI) * e.arc);
                    PlaceGlow(_dnEmbers[i], at, Vector3.one * e.size * (1.2f - .6f * c), Quaternion.identity, life * 1.6f * Mathf.Sin(c * Mathf.PI));
                }
                else
                {
                    float a = t - DnSlam - .01f * i, way = e.spray * Mathf.Deg2Rad;
                    var at = DnFists + new Vector3(Mathf.Sin(way) * e.out_, 0f, Mathf.Cos(way) * e.out_) * Mathf.Max(0f, a)
                             + Vector3.up * (.1f + e.up * Mathf.Max(0f, a) - 4.9f * a * a);
                    at.y = Mathf.Max(DnFloat + .04f, at.y);
                    PlaceGlow(_dnEmbers[i], at, Vector3.one * e.size * 1.3f, Quaternion.identity, a < 0f ? 0f : 1.9f * (1f - Mathf.Clamp01(a / .75f)));
                }
            }

            // THE GROUND RISES: play's cascade, on play's clock, from the first band on.
            if (_dnLane != null)
            {
                bool on = t >= DnBand0;
                if (_dnLane.gameObject.activeSelf != on) _dnLane.gameObject.SetActive(on);
                if (on) _dnLane.StepTo(t - DnBand0);
            }
        }
    }
}
