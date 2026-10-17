using System.Collections.Generic;
using TumbangPreso.CameraSystem;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // BASILIO, CONTINENTAL DRIFT: THE CASCADE (rebuilt 2026-10-17). The owner on v3: "ang panget ng ult cutscen nya im gonna
        // be fr as well as communication of wtf happens i dont even get what js hapepend"; on the proposal: "lock inr efine it go".
        // v1 to v3 packed twelve beats into seven seconds and none landed. Now ONE idea anyone gets:
        //
        //      HE SLAMS THE GROUND AND THE EARTH RISES AS A WAVE AT HIS ENEMIES.
        //
        //   0.00 THE WIND-UP    planted, fists up; stone packs onto them in three steps until they are BOULDERS, molten veins lit
        //                       (film r17: his arms reach only head height under his hair, so a raise alone never read)
        //   1.38 THE SLAM       both fists into the court as one: an impact frame, a molten crack racing forward down the lane
        //   1.90 THE CASCADE    five bands of court erupt one after another toward them, 0.6 s apart: a row of basalt slabs
        //                       heaving up, molten light in the gaps, dust and rocks thrown. These are the five live blasts
        //                       that follow the cutscene (`DanteDriftWave`), so the scene previews exactly the hit.
        //   4.30 THE HIT        the fifth band, sized to land under them, bursts: they are thrown and land Concussed
        //   5.50 THE AFTERMATH  wide and high, the whole scarred lane; he still holds the slam (where play resumes)
        // The beast is cut (the owner's call on the proposal: it was the biggest source of confusion).
        //
        // RULES (the method's traps): nothing here runs on `Update`, everything is posed from the scene clock; flat effects sit on
        // the court; light is cut INTO stone (molten gaps between slabs), never raised; the hottest colour is (1, .62, .18), never
        // white; stone never fades: slabs stay heaved, rocks stay where they land.
        // =========================================================================================
        private const float DnSlam = 1.38f, DnFirstBand = 1.90f, DnBandGap = .60f, DnAfter = 5.50f;
        private static float DnBandAt(int k) => DnFirstBand + DnBandGap * k;
        private static float DnHit => DnBandAt(4);

        // The court's own surface over the stage root (`Slipper.GroundY`): Bayan Plaza's court stands above the root.
        private float _dnCourt;
        private float DnFloat => _dnCourt + .03f;

        // The lane: from just ahead of his fists out to the enemies, five bands deep, sized so the fifth lands under them.
        private const float DnLaneStart = 1.6f;
        private float _dnBandDepth = 1.6f, _dnHalfWidth = 4.5f;
        private Vector3 _dnTargets = new Vector3(0f, 0f, 8f);
        private float DnBandStart(int k) => DnLaneStart + k * _dnBandDepth;
        private float DnLaneEnd => DnBandStart(5);

        // Each band's slabs across the lane: (across share -1 to 1, width share of a sixth, depth share, height m, tilt x, tilt z,
        // delay s, seed). Typed per band, no two bands alike.
        private static readonly (float across, float width, float depth, float height, float tiltX, float tiltZ, float delay, int seed)[][] DnSlabRows =
        {
            new[] { (-.86f, .95f, .82f, .62f, -9f,  6f, .03f,  3), (-.52f, 1.05f, .90f, .78f, -12f, -4f, .00f,  5), (-.17f, .98f, .86f, .70f, -8f,  3f, .02f,  7),
                    ( .18f, 1.02f, .92f, .84f, -14f, -5f, .01f, 11), ( .53f, .96f, .84f, .66f, -10f,  7f, .04f, 13), ( .87f, 1.00f, .88f, .58f, -7f, -6f, .05f, 17) },
            new[] { (-.85f, 1.00f, .86f, .72f, -11f, -5f, .04f, 19), (-.50f, .97f, .92f, .90f, -15f,  4f, .01f, 23), (-.16f, 1.04f, .84f, .80f, -9f, -3f, .00f, 29),
                    ( .19f, .95f, .90f, .96f, -16f,  6f, .02f, 31), ( .54f, 1.03f, .86f, .74f, -12f, -7f, .03f, 37), ( .88f, .98f, .82f, .64f, -8f,  5f, .05f, 41) },
            new[] { (-.87f, .97f, .88f, .80f, -13f,  5f, .02f, 43), (-.51f, 1.02f, .84f, .98f, -17f, -6f, .03f, 47), (-.15f, .99f, .92f, 1.06f, -12f,  4f, .00f, 53),
                    ( .20f, 1.04f, .86f, .92f, -15f, -4f, .01f, 59), ( .55f, .96f, .90f, .84f, -13f,  7f, .04f, 61), ( .86f, 1.01f, .84f, .70f, -9f, -5f, .05f, 67) },
            new[] { (-.86f, 1.03f, .84f, .88f, -14f, -6f, .03f, 71), (-.52f, .96f, .90f, 1.10f, -18f,  5f, .02f, 73), (-.17f, 1.02f, .86f, 1.18f, -13f, -3f, .00f, 79),
                    ( .18f, .98f, .92f, 1.04f, -16f,  6f, .01f, 83), ( .53f, 1.04f, .84f, .94f, -14f, -7f, .04f, 89), ( .87f, .97f, .88f, .78f, -10f,  4f, .05f, 97) },
            new[] { (-.85f, .99f, .90f, 1.00f, -16f,  7f, .02f, 101), (-.51f, 1.04f, .86f, 1.26f, -20f, -5f, .01f, 103), (-.16f, .97f, .92f, 1.36f, -15f,  4f, .00f, 107),
                    ( .19f, 1.03f, .84f, 1.30f, -19f, -6f, .02f, 109), ( .54f, .98f, .90f, 1.12f, -16f,  6f, .03f, 113), ( .88f, 1.02f, .86f, .92f, -12f, -4f, .05f, 127) },
        };
        // Rocks each band throws: (across share, up speed, forward speed, size, seed). Five per band.
        private static readonly (float across, float up, float along, float size, int seed)[] DnRockRows =
        {
            (-.62f, 5.2f, 1.6f, .26f, 2), (-.20f, 6.4f, 2.2f, .32f, 4), (.14f, 5.8f, 1.2f, .22f, 6), (.48f, 6.8f, 2.6f, .28f, 8), (.80f, 4.9f, 1.8f, .24f, 10),
        };
        // His fists' stone: (which fist 0 left 1 right, offset from the palm, size, the step it packs on 0 to 2, seed).
        private static readonly (int fist, Vector3 offset, float size, int step, int seed)[] DnGauntletRows =
        {
            (0, new Vector3(-.06f, .02f, .05f), .17f, 0, 3), (0, new Vector3(.05f, .05f, .02f), .15f, 1, 5), (0, new Vector3(0f, -.06f, .06f), .14f, 2, 7), (0, new Vector3(-.03f, .08f, -.04f), .12f, 2, 9),
            (1, new Vector3(.06f, .02f, .05f), .17f, 0, 11), (1, new Vector3(-.05f, .05f, .02f), .15f, 1, 13), (1, new Vector3(0f, -.06f, .06f), .14f, 2, 17), (1, new Vector3(.03f, .08f, -.04f), .12f, 2, 19),
        };

        private readonly List<int> _dnSlabs = new List<int>(30), _dnRocks = new List<int>(25), _dnDust = new List<int>(20), _dnBandGlow = new List<int>(5);
        private readonly List<int> _dnGauntlet = new List<int>(8), _dnFistGlow = new List<int>(2), _dnPebbles = new List<int>(21);
        private int _dnFuse, _dnSlamStar, _dnSlamRing;
        private Material _dnImpact;
        private Vector3? _dnFistAt;

        private void BuildDante()
        {
            float court = Slipper.GroundY(_root.transform.position + Vector3.up * .3f) - _root.transform.position.y;
            _dnCourt = float.IsInfinity(court) || float.IsNaN(court) ? 0f : Mathf.Clamp(court, 0f, .6f);

            // The enemies first: the lane is sized to them.
            BuildDanteTake();
            if (_dnVictims.Count > 0)
            {
                Vector3 sum = Vector3.zero; float wide = 0f;
                foreach (var v in _dnVictims) { sum += v.Spot; wide = Mathf.Max(wide, Mathf.Abs(v.Spot.x)); }
                _dnTargets = sum / _dnVictims.Count;
                _dnHalfWidth = Mathf.Clamp(wide + 1.6f, 3.4f, 6f);
            }
            _dnTargets.z = Mathf.Clamp(_dnTargets.z, 5f, 12f);
            // The fifth band's middle on them.
            _dnBandDepth = Mathf.Clamp((_dnTargets.z - DnLaneStart) / 4.5f, 1.0f, 2.6f);
            // Every enemy stands in the fifth band.
            foreach (var v in _dnVictims)
                v.Spot = new Vector3(Mathf.Clamp(v.Spot.x, -_dnHalfWidth + .7f, _dnHalfWidth - .7f), 0f,
                                     Mathf.Clamp(v.Spot.z, DnBandStart(4) + .35f, DnLaneEnd - .25f));

            var basalt = new[] { new Color(.29f, .24f, .2f, 1f), new Color(.34f, .29f, .25f, 1f), new Color(.24f, .2f, .17f, 1f) };
            for (int k = 0; k < 5; k++)
            {
                // The molten light under the band, seen in the gaps as its slabs heave.
                _dnBandGlow.Add(AddGlow("DanteBandGlow" + k, new Color(1f, .42f, .08f), billboard: false, band: true, falloff: .8f, core: .7f));
                for (int i = 0; i < DnSlabRows[k].Length; i++)
                    _dnSlabs.Add(BasilioStone("DanteSlab" + k + "_" + i, VfxShapes.Prism(4, 1f, .84f, .16f, .785f, DnSlabRows[k][i].seed), basalt[(k + i) % 3]));
                foreach (var r in DnRockRows)
                    _dnRocks.Add(BasilioStone("DanteRock" + k + "_" + r.seed, VfxShapes.Prism(5, 1f, .7f, .3f, 0f, r.seed + k * 31), basalt[(k + r.seed) % 3]));
                for (int i = 0; i < 4; i++)
                    _dnDust.Add(Add("DanteDust" + k + "_" + i, VfxShapes.TwoSided(VfxShapes.Splat(9, .48f, 201 + k * 17 + i * 5)),
                                    new Color(.5f, .37f, .24f, .72f) * (i % 2 == 0 ? .9f : 1f), .3f, plain: true));
            }
            _dnFuse = AddGlow("DanteFuse", new Color(1f, .5f, .1f), billboard: false, band: true, falloff: 1.6f, core: .7f);
            // A flat hot spot under the fists (a custom mesh would need its own uv: the glow fades by uv, the crest lesson).
            _dnSlamStar = AddGlow("DanteSlamStar", new Color(1f, .45f, .09f), billboard: false, falloff: 1.4f, core: .6f);
            _dnSlamRing = AddGlow("DanteSlamRing", new Color(1f, .5f, .12f), DnRingMesh, billboard: false, band: true, falloff: 1.2f, core: .4f);
            foreach (var g in DnGauntletRows)
                _dnGauntlet.Add(BasilioStone("DanteGauntlet" + g.seed, VfxShapes.Prism(5, 1f, .72f, .3f, 0f, g.seed), basalt[g.seed % 3]));
            for (int f = 0; f < 2; f++)
                _dnFistGlow.Add(AddGlow("DanteFistGlow" + f, new Color(1f, .5f, .12f), falloff: 2.2f, core: .8f));
            // Concussed: three pebbles round each enemy's head once they land.
            for (int i = 0; i < _dnVictims.Count * 3; i++)
                _dnPebbles.Add(BasilioStone("DantePebble" + i, VfxShapes.Prism(5, 1f, .62f, .32f, 0f, 300 + i), basalt[i % 3]));
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

        /// <summary>How far the molten crack has run down the lane at t: from his fists at the slam, reaching each band as it goes.</summary>
        private float DnFront(float t)
        {
            if (t <= DnSlam) return DnLaneStart * .5f;
            if (t <= DnBandAt(0)) return Mathf.Lerp(DnLaneStart * .5f, DnBandStart(0), (t - DnSlam) / (DnBandAt(0) - DnSlam));
            for (int k = 0; k < 4; k++)
                if (t <= DnBandAt(k + 1)) return Mathf.Lerp(DnBandStart(k), DnBandStart(k + 1), (t - DnBandAt(k)) / DnBandGap);
            return Mathf.Lerp(DnBandStart(4), DnLaneEnd, Mathf.Clamp01((t - DnBandAt(4)) / .25f));
        }

        private static float DanteKick(float t, float at, float size, float fade)
            => t < at ? 0 : size * Mathf.Clamp01(1 - (t - at) / fade);

        /// <summary>The lens feels it: the slam, each band (harder as they grow), the hit.</summary>
        private Vector3 DanteShake(float t)
        {
            float size = DanteKick(t, DnSlam, .16f, .2f) + DanteKick(t, DnHit, .14f, .3f);
            for (int k = 0; k < 4; k++) size += DanteKick(t, DnBandAt(k), .03f + .01f * k, .22f);
            return new Vector3(Mathf.Sin(t * 71f), Mathf.Sin(t * 93f + 1.3f), 0) * size;
        }

        /// <summary>The backdrop steps back a little from the slam on, so the molten light reads; back up for the aftermath.</summary>
        private void DanteGrade(float t, out float brightness, out float saturation)
        {
            float back = Ease(DnSlam, DnSlam + .25f, t) * (1f - .5f * Ease(DnAfter, DnAfter + .6f, t));
            brightness = Mathf.Lerp(1f, .82f, back);
            saturation = Mathf.Lerp(1f, .9f, back);
        }

        /// <summary>The two impact frames: two frames each, ink on molten orange, lines in to the hit.</summary>
        private void DantePostProcess(RenderTexture frame, Camera camera, float t)
        {
            if (_reducedEffects) return;
            float impact = 0f; Vector3 at = Vector3.zero;
            if (t >= DnSlam && t < DnSlam + .067f) { impact = 1f; at = DnFists; }
            else if (t >= DnHit && t < DnHit + .067f) { impact = .9f; at = new Vector3(_dnTargets.x, DnFloat + .8f, (DnBandStart(4) + DnLaneEnd) * .5f); }
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

            // THE WIND-UP: stone packs onto his fists in three steps as they rise; molten veins lit; it stays on them.
            for (int i = 0; i < DnGauntletRows.Length; i++)
            {
                var g = DnGauntletRows[i];
                float step = .35f + g.step * .22f;
                float grow = Ease(step, step + .08f, t);
                var at = DnFistPoint(g.fist) + g.offset;
                at = DnFistPoint(g.fist) + g.offset * 2.2f;
                Place(_dnGauntlet[i], at, Vector3.one * g.size * 2.3f * grow * (1f + .15f * Flash(t, step + .08f, .06f)),
                      Quaternion.Euler(g.seed * 23f, g.seed * 41f, g.seed * 13f), 1f);
            }
            for (int f = 0; f < 2; f++)
            {
                float veins = Ease(.4f, 1.0f, t) * (1f - .5f * Ease(DnSlam + .2f, DnSlam + .8f, t));
                PlaceGlow(_dnFistGlow[f], DnFistPoint(f), Vector3.one * 1.3f, Quaternion.identity, veins * (1f + .2f * Mathf.Sin(t * 24f + f)));
            }

            // THE SLAM: a crack star and a ring under the fists.
            {
                float age = t - DnSlam;
                PlaceGlow(_dnSlamStar, DnFists + Vector3.up * .02f, Vector3.one * Mathf.Lerp(.6f, 1.6f, Mathf.Clamp01(age / .08f)),
                          Quaternion.Euler(90f, 0f, 0f), age < 0f ? 0f : 1.3f * (1f - .6f * Mathf.Clamp01(age / 1.2f)));
                float r = Mathf.Lerp(.3f, 3.2f, Mathf.Clamp01(age / .35f));
                PlaceGlow(_dnSlamRing, DnFists + Vector3.up * .03f, new Vector3(r, 1f, r), Quaternion.identity, age < 0f ? 0f : 1.2f * (1f - Mathf.Clamp01(age / .4f)));
            }

            // THE FUSE: the molten crack racing down the middle of the lane, just ahead of each band.
            {
                float front = DnFront(t), from = DnLaneStart * .5f;
                float len = Mathf.Max(.01f, front - from);
                float heat = t < DnSlam ? 0f : 1.4f * (1f - .55f * Ease(DnHit + .3f, DnAfter + .5f, t));
                // Lying on the court with its length down the lane (x to z) so the glow fades across it, not along it.
                PlaceGlow(_dnFuse, new Vector3(0f, DnFloat + .015f, from + len * .5f), new Vector3(len, .32f, 1f), Quaternion.Euler(90f, 90f, 0f), heat);
            }

            // THE CASCADE: five bands, each a row of slabs heaving up in a jolt, molten light in the gaps, dust and rocks thrown.
            for (int k = 0; k < 5; k++)
            {
                float at = DnBandAt(k), age = t - at;
                float z0 = DnBandStart(k), mid = z0 + _dnBandDepth * .5f;
                // The molten light under the band: a flare as it goes, then the glow in its cracks, cooling a little.
                float glow = age < 0f ? 0f : 2.3f * (1f - .4f * Mathf.Clamp01(age / .8f)) * (1f - .25f * Ease(DnAfter, 7f, t));
                PlaceGlow(_dnBandGlow[k], new Vector3(0f, DnFloat + .01f, mid), new Vector3(_dnHalfWidth * 1.9f, _dnBandDepth * 1.0f, 1f),
                          Quaternion.Euler(90f, 0f, 0f), glow);
                var rows = DnSlabRows[k];
                float sixth = _dnHalfWidth * 2f / 6f;
                for (int i = 0; i < rows.Length; i++)
                {
                    var s = rows[i];
                    float sa = age - s.delay;
                    float u = Mathf.Clamp01(sa / .1f);
                    // A jolt, a small overshoot, a hold; it stays heaved (the scar).
                    float up = sa < 0f ? 0f : u < 1f ? u * u : 1f + .12f * Mathf.Max(0f, 1f - (sa - .1f) / .12f);
                    // Peaks, then settles back to rubble, so the tallest ridge travels down the lane as a wave instead of building
                    // a wall in front of the lens (film r17); what is left behind is a broken, uneven lane, not tiles.
                    float settle = 1f - .5f * Ease(.25f, .7f, sa);
                    float h = s.height * .8f, w = sixth * s.width, d = _dnBandDepth * s.depth;
                    float jitter = ((s.seed % 5) - 2) * .12f * _dnBandDepth;
                    var centre = new Vector3(s.across * (_dnHalfWidth - sixth * .5f), DnFloat - h * .5f + h * .8f * up * settle, mid + jitter);
                    var rot = Quaternion.Euler(s.tiltX * 1.6f * Mathf.Min(1f, up), (s.seed % 7 - 3) * 5f, s.tiltZ * 1.8f * Mathf.Min(1f, up));
                    Place(_dnSlabs[k * 6 + i], centre, sa < 0f ? Vector3.zero : new Vector3(w, h, d), rot, 1f);
                }
                // Dust standing on the band's front edge, billowing up and out, thinning.
                for (int i = 0; i < 4; i++)
                {
                    float du = Mathf.Clamp01(age / .9f);
                    float across = (i / 3f * 2f - 1f) * (_dnHalfWidth - .8f);
                    float puff = Mathf.Lerp(1.0f, 2.6f, du) * (.8f + .1f * i);
                    var turn = Quaternion.LookRotation(Vector3.forward, Vector3.up) * Quaternion.Euler(90f, 0f, (i - 1.5f) * 9f);
                    Place(_dnDust[k * 4 + i], new Vector3(across, DnFloat + puff * .5f + du * .4f, z0 + _dnBandDepth + .1f),
                          age < 0f ? Vector3.zero : new Vector3(puff * 1.3f, 1f, puff), turn, age < 0f ? 0f : .85f * (1f - du * du));
                }
                // Rocks flung up and forward, landing where they land and staying.
                for (int i = 0; i < DnRockRows.Length; i++)
                {
                    var r = DnRockRows[i];
                    var start = new Vector3(r.across * _dnHalfWidth, DnFloat + .2f, mid);
                    if (age < 0f) { Place(_dnRocks[k * 5 + i], start, Vector3.zero, Quaternion.identity, 1f); continue; }
                    float land = 2f * r.up / 9.8f, ft = Mathf.Min(age, land);
                    var pos = start + Vector3.forward * (r.along * ft) + Vector3.up * (r.up * ft - 4.9f * ft * ft);
                    float spin = ft * (240f + r.seed * 9f);
                    Place(_dnRocks[k * 5 + i], pos, Vector3.one * r.size, Quaternion.Euler(spin, r.seed * 23f, spin * .6f), 1f);
                }
            }

            SampleDanteTake(t);
        }
    }
}
