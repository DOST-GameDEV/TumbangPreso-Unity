using System.Collections.Generic;
using TumbangPreso.Abilities;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // BASILIO, CONTINENTAL DRIFT: THE CASCADE. ONE idea anyone gets:
        //
        //      HE LEAPS, COMES DOWN FISTS FIRST, AND THE EARTH RISES AS A WAVE AT HIS ENEMIES AND THROWS THEM.
        //
        //   0.00 THE CROUCH     he sinks, stone packing onto his fists in three steps until they are BOULDERS, molten veins lit
        //   0.70 THE LEAP       he jumps high (a crack and dust where he left), against the sky, and turns fists down
        //   1.85 THE SLAM       he lands fists first: an impact frame, a molten crack racing forward down the lane
        //   2.25 THE CASCADE    five bands erupt down the real lane at play's own pace (0.3 s apart)
        //   3.75 THE GRIP       a cut to each enemy WHERE THEY REALLY STAND as the stone closes over their feet
        //   6.55 THE BURST      the whole lane heaves once more and throws everyone on it, framed wide to hold them all
        //   7.00 THE CUT        they are in the air; play lands them, Concussed (`DanteDriftWave`)
        // The owner, 2026-10-17: *"dannte's entire ult is js him hitting the ground annd 4 seconds of eart moving towards the
        // pppl"*, *"EVERYONE isnt always gonnna be in the same place"*; his picks: "The leap", "Fast cascade, per-enemy cuts".
        //
        // ⚠️ THE CASCADE IS PLAY'S OWN (owner, 2026-10-17: *"i js want actual game to match it"*, *"dapat cutscene transitions into
        // game well, hindi na dapat uulit"*). The bands are `DanteDriftLive`, built here on the live lane (`DanteDriftWave.Lane`:
        // from his feet, the court's full width) and posed from this clock; play builds the same thing and continues this clock,
        // so the last frame here is the frame play opens on, and nothing erupts twice. The enemies are the real players where
        // they stand; who is thrown is the live rule (`DanteDriftWave.InLane`).
        //
        // RULES (the method's traps): nothing here runs on `Update`, everything is posed from the scene clock; flat effects sit on
        // the court; light is cut INTO stone, never raised; the hottest colour is (1, .62, .18), never white.
        // =========================================================================================
        private const float DnLeap = DanteDriftLive.LeapAt, DnSlam = DanteDriftLive.SlamAt, DnFirstBand = DanteDriftLive.FirstBand,
                             DnGrip = DanteDriftLive.GripAt, DnBurst = DanteDriftLive.BurstAt, DnEnd = DanteDriftLive.SceneEnd;
        private static float DnBandAt(int k) => DanteDriftLive.BandAt(k);

        // The court's own surface over the stage root (`Slipper.GroundY`): Bayan Plaza's court stands above the root.
        private float _dnCourt;
        private float DnFloat => _dnCourt + .03f;

        // The live lane, in stage space (his feet at the origin, forward +z).
        private DanteDriftLive _dnLane;
        private float _dnHalfWidth = 8f, _dnReach = 16f;
        private Vector3 _dnTargets = new Vector3(0f, 0f, 8f);
        private float DnFront(float t) => _dnLane != null ? _dnLane.Front(t) : 0f;

        // His fists' stone: (which fist 0 left 1 right, offset from the palm, size, the step it packs on 0 to 2, seed).
        private static readonly (int fist, Vector3 offset, float size, int step, int seed)[] DnGauntletRows =
        {
            (0, new Vector3(-.06f, .02f, .05f), .17f, 0, 3), (0, new Vector3(.05f, .05f, .02f), .15f, 1, 5), (0, new Vector3(0f, -.06f, .06f), .14f, 2, 7), (0, new Vector3(-.03f, .08f, -.04f), .12f, 2, 9),
            (1, new Vector3(.06f, .02f, .05f), .17f, 0, 11), (1, new Vector3(-.05f, .05f, .02f), .15f, 1, 13), (1, new Vector3(0f, -.06f, .06f), .14f, 2, 17), (1, new Vector3(.03f, .08f, -.04f), .12f, 2, 19),
        };

        private readonly List<int> _dnGauntlet = new List<int>(8), _dnFistGlow = new List<int>(2);
        private readonly List<int> _dnLift = new List<int>(4);
        private int _dnSlamStar, _dnSlamRing, _dnLiftStar, _dnLiftRing;
        private Material _dnImpact;
        private Vector3? _dnFistAt;

        private void BuildDante()
        {
            float court = Slipper.GroundY(_root.transform.position + Vector3.up * .3f) - _root.transform.position.y;
            _dnCourt = float.IsInfinity(court) || float.IsNaN(court) ? 0f : Mathf.Clamp(court, 0f, .6f);

            // The live lane, measured exactly as the wave will measure it.
            var origin = _source != null ? _source.transform.position : _root.transform.position;
            DanteDriftWave.Lane(origin, _root.transform.forward, out _dnHalfWidth, out _dnReach);
            _dnLane = DanteDriftLive.BuildStage(_root.transform, _dnCourt, _dnHalfWidth, _dnReach);

            // The enemies where they stand; the shots frame the ones the lane catches.
            BuildDanteTake();
            Vector3 sum = Vector3.zero; int caught = 0;
            foreach (var v in _dnVictims) if (v.Caught) { sum += v.Spot; caught++; }
            if (caught == 0) foreach (var v in _dnVictims) { sum += v.Spot; caught++; }
            if (caught > 0) _dnTargets = sum / caught;
            _dnTargets.z = Mathf.Clamp(_dnTargets.z, 3f, Mathf.Max(3f, _dnReach - 1f));
            DnPlanGrips();

            var basalt = new[] { new Color(.29f, .24f, .2f, 1f), new Color(.34f, .29f, .25f, 1f), new Color(.24f, .2f, .17f, 1f) };
            // A flat hot spot under the fists (a custom mesh would need its own uv: the glow fades by uv, the crest lesson).
            _dnSlamStar = AddGlow("DanteSlamStar", new Color(1f, .45f, .09f), billboard: false, falloff: 1.4f, core: .6f);
            _dnSlamRing = AddGlow("DanteSlamRing", new Color(1f, .5f, .12f), DnRingMesh, billboard: false, band: true, falloff: 1.2f, core: .4f);
            // THE LEAP: a crack and a ring under his feet where he leaves the court, and dust kicked up round them.
            _dnLiftStar = AddGlow("DanteLiftStar", new Color(1f, .45f, .09f), billboard: false, falloff: 1.6f, core: .6f);
            _dnLiftRing = AddGlow("DanteLiftRing", new Color(1f, .5f, .12f), DnRingMesh, billboard: false, band: true, falloff: 1.2f, core: .4f);
            for (int i = 0; i < 4; i++)
                _dnLift.Add(Add("DanteLiftDust" + i, VfxShapes.TwoSided(VfxShapes.Splat(9, .48f, 401 + i * 7)), new Color(.5f, .37f, .24f, .72f), .3f, plain: true));
            // THE GRIP: three stones close over each caught enemy's feet.
            foreach (var v in _dnVictims)
                if (v.Caught)
                    for (int i = 0; i < 3; i++)
                        _dnGrips.Add(BasilioStone("DanteGrip" + _dnGrips.Count, VfxShapes.Prism(5, 1f, .7f, .3f, 0f, 500 + _dnGrips.Count), basalt[_dnGrips.Count % 3]));
            foreach (var g in DnGauntletRows)
                _dnGauntlet.Add(BasilioStone("DanteGauntlet" + g.seed, VfxShapes.Prism(5, 1f, .72f, .3f, 0f, g.seed), basalt[g.seed % 3]));
            for (int f = 0; f < 2; f++)
                _dnFistGlow.Add(AddGlow("DanteFistGlow" + f, new Color(1f, .5f, .12f), falloff: 2.2f, core: .8f));
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
        private Vector3 DnFistPoint(int fist) => fist == 0 ? FreePalm : RightPalm;
        private Vector3 DnFists => _dnFistAt ?? new Vector3(0f, DnFloat + .02f, .7f);

        private static float DanteKick(float t, float at, float size, float fade)
            => t < at ? 0 : size * Mathf.Clamp01(1 - (t - at) / fade);

        /// <summary>The lens feels it: the leap, the slam, each band (harder as they come), each grip, the burst.</summary>
        private Vector3 DanteShake(float t)
        {
            float size = DanteKick(t, DnLeap, .06f, .2f) + DanteKick(t, DnSlam, .18f, .25f) + DanteKick(t, DnBurst, .15f, .35f);
            for (int i = 0; i < _dnShown.Count; i++) size += DanteKick(t, DnGripAt(i), .05f, .2f);
            for (int k = 0; k < 5; k++) size += DanteKick(t, DnBandAt(k), .03f + .012f * k, .22f);
            return new Vector3(Mathf.Sin(t * 71f), Mathf.Sin(t * 93f + 1.3f), 0) * size;
        }

        /// <summary>The backdrop steps back a little from the slam on, so the molten light reads; back up by the cut, as play is.</summary>
        private void DanteGrade(float t, out float brightness, out float saturation)
        {
            float back = Ease(DnSlam, DnSlam + .25f, t) * (1f - Ease(DnBurst + .1f, DanteDriftLive.SceneEnd - .05f, t));
            brightness = Mathf.Lerp(1f, .82f, back);
            saturation = Mathf.Lerp(1f, .9f, back);
        }

        /// <summary>The two impact frames: two frames each, ink on molten orange, lines in to the hit.</summary>
        private void DantePostProcess(RenderTexture frame, Camera camera, float t)
        {
            if (_reducedEffects) return;
            float impact = 0f; Vector3 at = Vector3.zero;
            if (t >= DnSlam && t < DnSlam + .067f) { impact = 1f; at = DnFists; }
            else if (t >= DnBurst && t < DnBurst + .067f) { impact = .9f; at = _dnTargets + Vector3.up * (DnFloat + .8f); }
            if (impact <= 0f || camera == null) return;
            if (_dnImpact == null)
            {
                var shader = Resources.Load<Shader>("Shaders/DanteImpact");
                if (shader == null) return;
                _dnImpact = new Material(shader) { name = "DanteImpact" };
            }
            var vp = camera.WorldToViewportPoint(_root.transform.TransformPoint(at));
            _dnImpact.SetFloat("_Impact", impact);
            _dnImpact.SetVector("_Focus", new Vector4(Mathf.Clamp(vp.x, .1f, .9f), Mathf.Clamp(vp.y, .1f, .9f), 0f, 0f));
            _dnImpact.SetFloat("_Aspect", frame.height > 0 ? frame.width / (float)frame.height : 16f / 9f);
            _dnImpact.SetFloat("_Seed", t < 3f ? 1f : 3f);
            var tmp = RenderTexture.GetTemporary(frame.descriptor);
            Graphics.Blit(frame, tmp, _dnImpact);
            Graphics.Blit(tmp, frame);
            RenderTexture.ReleaseTemporary(tmp);
        }

        private void SampleDante(float t)
        {
            // Where the fists meet the court, caught the first time the stage is sampled at the slam (the phase samples forward).
            if (_dnFistAt == null && t >= DnSlam && t < DnSlam + .4f)
            {
                var mid = (DnFistPoint(0) + DnFistPoint(1)) * .5f;
                _dnFistAt = new Vector3(mid.x, DnFloat + .02f, Mathf.Max(.4f, mid.z));
            }

            // THE CROUCH: stone packs onto his fists in three steps as he sinks; molten veins lit; it stays on them.
            for (int i = 0; i < DnGauntletRows.Length; i++)
            {
                var g = DnGauntletRows[i];
                float step = .06f + g.step * .17f;
                float grow = Ease(step, step + .08f, t);
                var at = DnFistPoint(g.fist) + g.offset * 2.2f;
                Place(_dnGauntlet[i], at, Vector3.one * g.size * 2.3f * grow * (1f + .15f * Flash(t, step + .08f, .06f)),
                      Quaternion.Euler(g.seed * 23f, g.seed * 41f, g.seed * 13f), 1f);
            }
            for (int f = 0; f < 2; f++)
            {
                float veins = Ease(.1f, .6f, t) * (1f - .5f * Ease(DnSlam + .2f, DnSlam + .8f, t));
                PlaceGlow(_dnFistGlow[f], DnFistPoint(f), Vector3.one * 1.3f, Quaternion.identity, veins * (1f + .2f * Mathf.Sin(t * 24f + f)));
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

            // THE SLAM: a crack star and a ring under the fists.
            {
                float age = t - DnSlam;
                PlaceGlow(_dnSlamStar, DnFists + Vector3.up * .02f, Vector3.one * Mathf.Lerp(.6f, 1.6f, Mathf.Clamp01(age / .08f)),
                          Quaternion.Euler(90f, 0f, 0f), age < 0f ? 0f : 1.3f * (1f - .6f * Mathf.Clamp01(age / 1.2f)));
                float r = Mathf.Lerp(.3f, 3.2f, Mathf.Clamp01(age / .35f));
                PlaceGlow(_dnSlamRing, DnFists + Vector3.up * .03f, new Vector3(r, 1f, r), Quaternion.identity, age < 0f ? 0f : 1.2f * (1f - Mathf.Clamp01(age / .4f)));
            }

            // THE FUSE, THE CASCADE AND THE BURST: play's own build, on this clock.
            if (_dnLane != null) _dnLane.Pose(t);

            SampleDanteTake(t);
        }
    }
}
