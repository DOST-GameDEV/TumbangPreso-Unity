using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // PHAISTER, OMEN, 5.0 s (HERO-10 v8, 2026-09-27; plan.md 4.4, direction.md section 3; the body and the storyboard shots are
        // `tools/author_ultimate_intros.py` `phaister()`). v7 (4.0 s) ended on the eye and its maelstrom with nobody in it, landed
        // the eye at a fixed spot 4 m ahead whatever she aimed at, formed it over her mouth under a brim that hid her eyes, and
        // faked the impact with a dark disc. This version:
        //
        //   0.00-1.30  SURGE     her night falls; she rises; butterflies pour out and wheel CLOCKWISE; the camera orbits clockwise
        //   1.30-2.40  THE EYE   from below her chin: her eyes light, the butterflies stream LEFT TO RIGHT into her palms, low in
        //                        front of her chest, and crush into a tiny glitching eye that pulses and grows; her smirk
        //   2.40-3.20  THE THROW she hurls it; it streaks LEFT TO RIGHT across the frame to WHERE SHE AIMED (the commit's aim, its
        //                        height included) and lands at 3.00: the impact frame, the butterfly burst, the ring on the court
        //   3.20-5.00  THE MARK  the maelstrom starts round the unstable eye and one black butterfly goes to every REAL player in
        //                        its reach (`HeroIntroductionScene.PhaisterMark.cs`); one crane round the eye ends with them all in frame
        //
        // ⚠️ NEVER SHOWN TWICE. OMEN's rules keep a 2.2 s cast after the hand-back (the owner: *"those can stay"*), so this does
        // NOT show the pull: it ends where play begins, the eye still unstable over the spot (the size the live cast starts at),
        // the marks already on the players and the maelstrom already turning (`PhaisterOmen` picks all three up from there).
        // Nothing here runs on Update: every piece is posed from `t` in `SamplePhaister`.
        // =========================================================================================

        private const float PhEyeAt = 1.30f, PhThrowAt = 2.47f, PhLandAt = 3.00f, PhMarkAt = 3.20f;

        // Butterflies, typed: (start angle deg, orbit radius m, height m, turns a second, size, emerge delay s, from hat).
        private static readonly (float A, float R, float Y, float Spin, float Size, float Delay, bool Hat)[] OmenFlies =
        {
            (  0f, 1.10f, 1.30f, .32f, 1.25f, .15f, false), ( 38f, 1.45f, 1.80f, .28f, 1.10f, .22f, true ), ( 71f, 0.95f, 0.95f, .36f, 1.30f, .18f, false),
            (112f, 1.60f, 2.10f, .25f, 1.00f, .30f, true ), (147f, 1.25f, 1.15f, .30f, 1.20f, .26f, false), (186f, 1.70f, 1.60f, .24f, 1.15f, .34f, true ),
            (221f, 1.05f, 0.80f, .35f, 1.35f, .20f, false), (259f, 1.50f, 2.25f, .27f, 0.95f, .38f, true ), (298f, 1.30f, 1.45f, .31f, 1.20f, .24f, false),
            (331f, 1.80f, 1.95f, .23f, 1.05f, .42f, true ), ( 18f, 1.35f, 0.70f, .33f, 1.25f, .46f, false), ( 95f, 1.90f, 1.35f, .22f, 1.10f, .50f, true ),
            (166f, 1.15f, 2.00f, .34f, 1.00f, .54f, false), (240f, 1.65f, 1.05f, .26f, 1.30f, .58f, true ), (312f, 1.20f, 1.75f, .30f, 1.15f, .62f, false),
            ( 57f, 1.75f, 1.55f, .25f, 1.20f, .66f, true ), (203f, 1.40f, 0.90f, .29f, 1.25f, .70f, false), (277f, 1.55f, 2.30f, .24f, 1.05f, .74f, true ),
        };

        // Ribbons off her body, typed: (angle round her deg, height m, length m, 0 robe / 1 trim / 2 hair).
        // v2 (film v5: wide black slabs, one rising across her face): thin, in her trim violet and hair magenta, and only at her
        // sides and back (90 to 270 degrees), never between the lens and her face.
        private static readonly (float A, float Y, float L, int Kind)[] OmenRibbons =
        {
            ( 95f, .55f, .70f, 1), (130f, .52f, .82f, 0), (165f, .56f, .66f, 1), (200f, .53f, .86f, 0), (235f, .55f, .74f, 1),
            (265f, .52f, .78f, 1), (150f, 1.25f, .58f, 2), (205f, 1.30f, .66f, 2), (250f, 1.22f, .52f, 2),
        };

        /// <summary>Where the eye lands when the scene has no aim (a preview): 4 m ahead, 2.3 m up.</summary>
        private static readonly Vector3 OmenLands = new Vector3(0f, 2.3f, 4.0f);
        private static readonly Vector3 MoonAt = new Vector3(-4.2f, 5.3f, -5.4f);
        /// <summary>
        /// ⚠️ THE EYE ENDS THE CUTSCENE AT THE SIZE THE LIVE CAST STARTS AT (`PhaisterOmen`: 0.40 of 2.4 m across, glitching), so the
        /// hand-back does not jump. v7 opened it fully here and play then showed it small again.
        /// </summary>
        private const float MoonRadius = 1.55f, OmenLandedRadius = 0.50f;
        // ⚠️ The night walls stand wider than the other heroes' 8 m stage: THE MARK's crane circles an eye up to 8 m from her and
        // backs out until every marked player fits (film v10: at 17 m the crane crossed the wall and filmed through it).
        private const float OmenStageRadius = 30f;

        private int _nightGround, _nightSky, _moon, _moonHalo, _omenPalmGlow, _shock, _omenRing;
        private readonly List<int> _stars = new List<int>(8), _omenRibbons = new List<int>(10), _omenSigils = new List<int>(8);
        private readonly List<(Transform T, Transform[] W)> _omenFlies = new List<(Transform, Transform[])>();
        private Transform _omenEye, _omenFlash;
        private Material _omenCosmos;
        private readonly List<Material> _omenFlashInks = new List<Material>();
        /// <summary>Where the eye lands, in the scene's space (her feet, her facing): the commit's aim, its height kept.</summary>
        private Vector3 _phLand, _phGround;

        private void BuildPhaister()
        {
            // Where she aimed. The commit's aim is a world point whose height is the eye's (`HeroAbility.AimsInTheAir`).
            _phLand = OmenLands; _phGround = new Vector3(OmenLands.x, 0f, OmenLands.z);
            if (_aim != Vector3.zero)
            {
                var local = _root.transform.InverseTransformPoint(_aim);
                var ground = _root.transform.InverseTransformPoint(VfxShapes.GroundPoint(_aim + Vector3.up * 0.5f));
                float flat = new Vector2(local.x, local.z).magnitude;
                if (flat > 1.5f && flat < 14f)
                {
                    _phGround = new Vector3(local.x, ground.y, local.z);
                    _phLand = new Vector3(local.x, ground.y + Mathf.Clamp(local.y - ground.y, Core.VoodooRules.HigopMinHeight, Core.VoodooRules.HigopMaxHeight), local.z);
                }
            }

            _nightGround = Wall("NightFallsGround", 0, 1.1f, new Color(.07f, .02f, .11f, .90f), OmenStageRadius);
            _nightSky = Wall("NightFallsSky", 1.1f, 13, new Color(.14f, .05f, .24f, .88f), OmenStageRadius, emission: .16f, cap: true);
            for (int i = 0; i < 7; i++)
                _stars.Add(Add("NightStar" + i, VfxShapes.TwoSided(VfxShapes.Star(4, .38f, 60 + i)), new Color(.86f, .78f, 1f, .9f), .6f));
            _moon = Add("RisingMoon", MoonDisc(0), new Color(.86f, .80f, .96f, .95f), .5f);
            _moonHalo = Add("MoonHalo", VfxShapes.TwoSided(VfxShapes.Collar(40, .02f, .8f)), new Color(.62f, .42f, .95f, .35f), .45f);
            var ribbonColours = new[] { new Color(.10f, .08f, .14f, .96f), new Color(.29f, .12f, .47f, .96f), new Color(.85f, .09f, .43f, .96f) };
            foreach (var r in OmenRibbons)
                _omenRibbons.Add(Add("OmenRibbon", VfxShapes.TwoSided(VfxShapes.Prism(4, 1f, 1f, 1)), ribbonColours[r.Kind], r.Kind == 2 ? .2f : .05f));
            _omenPalmGlow = AddGlow("OmenPalmGlow", new Color(.86f, .22f, .62f, 1f), falloff: 2.4f, core: .8f, lift: .1f);
            _shock = Add("OmenShock", VfxShapes.Hollow(48, .86f, .1f, 4), new Color(.80f, .36f, 1f, .85f), 1.6f);
            _omenRing = Add("OmenRing", VfxShapes.Hollow(64, .94f, 0f, 9), new Color(.74f, .40f, 1f, .8f), 1.2f);
            for (int i = 0; i < 8; i++)
                _omenSigils.Add(Add("OmenSigil" + i, PhaisterSpellGeometry.FlatRune(41 + i, .15f), new Color(.78f, .42f, 1f, .95f), 1.4f));

            // The eye: a camera-facing window into space (`CosmosEye.shader`) on the glow quad, so its bounds never cull it.
            var eye = new GameObject("OmenEye");
            eye.transform.SetParent(_root.transform, false);
            eye.layer = _root.layer;
            var eyeMesh = GlowQuad();
            eye.AddComponent<MeshFilter>().sharedMesh = eyeMesh;
            var eyeRenderer = eye.AddComponent<MeshRenderer>();
            VfxShapes.Own(eye, eyeMesh);
            eyeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; eyeRenderer.receiveShadows = false;
            var shader = Resources.Load<Shader>("Shaders/CosmosEye");
            if (shader != null) { _omenCosmos = new Material(shader); eyeRenderer.sharedMaterial = _omenCosmos; VfxRenderTag.Own(eye, _omenCosmos); }
            else VfxMaterial.Solid(eyeRenderer, new Color(.05f, .01f, .09f), 0f);
            _omenEye = eye.transform;

            // The emblem: a butterfly silhouette in her magenta, the burst's shape as it lands (Castorice 27 s).
            var flash = PhaisterProp.Spawn("butterfly", _root.transform, null, PhaisterProp.InsectOutlineWidth);
            if (flash != null)
            {
                foreach (var r in flash.GetComponentsInChildren<Renderer>())
                {
                    VfxMaterial.Ghost(r, new Color(.88f, .16f, .50f, 0f), 2f);
                    _omenFlashInks.Add(r.sharedMaterial);
                }
                SetLayer(flash, _root.layer);
                _omenFlash = flash.transform;
            }
            foreach (var row in OmenFlies)
            {
                var b = PhaisterProp.Spawn("butterfly", _root.transform, null, PhaisterProp.InsectOutlineWidth);
                if (b == null) continue;
                SetLayer(b, _root.layer);
                _omenFlies.Add((b.transform, new[] { PhaisterProp.Find(b, "wing-l"), PhaisterProp.Find(b, "wing-r") }));
            }
            BuildPhaisterBurst();
            BuildPhaisterMark();
        }

        private static void SetLayer(GameObject go, int layer)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        }

        private static Mesh MoonDisc(int seed)
        {
            var mesh = VfxShapes.TwoSided(VfxShapes.Splat(40, 0, 7 + seed));
            var normals = new Vector3[mesh.vertexCount];
            for (int i = 0; i < normals.Length; i++) normals[i] = Vector3.up;
            mesh.normals = normals; return mesh;
        }

        /// <summary>
        /// Where the forming eye is held: between her palms, pushed forward and down in front of her chest (v2: v7 held it at palm
        /// height, which on this chibi body is in front of her mouth, and the close-up lost her face behind it).
        /// </summary>
        private Vector3 OmenPalms => BothPalms + new Vector3(0f, -.08f, .40f);

        /// <summary>Where the eye is at <paramref name="t"/>: in front of her while it forms, on a rising arc once thrown.</summary>
        private Vector3 OmenEyeAt(float t)
        {
            if (t < PhThrowAt) return OmenPalms;
            float u = Mathf.Clamp01((t - PhThrowAt) / (PhLandAt - PhThrowAt));
            float lift = .5f + .12f * Vector3.Distance(OmenPalms, _phLand);
            return Vector3.Lerp(OmenPalms, _phLand, u * (2f - u)) + Vector3.up * Mathf.Sin(u * Mathf.PI) * lift;
        }

        private void SamplePhaister(float t)
        {
            float leave = 1 - Ease(Seconds - .45f, Seconds - .05f, t);
            float lift = LiftAt(t);

            // THE NIGHT, at once: the change of world is the first thing that happens.
            float night = Ease(0f, .3f, t) * leave;
            Tint(_nightGround, night); Tint(_nightSky, night);
            Quaternion facingCentre(Vector3 at) => Quaternion.LookRotation(-new Vector3(at.x, 0, at.z).normalized, Vector3.up) * Quaternion.Euler(90, 0, 0);
            for (int i = 0; i < _stars.Count; i++)
            {
                float angle = (-150 + i * 43) * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Sin(angle) * 14f, 5.4f + (i * 37 % 5) * .9f, Mathf.Cos(angle) * 14f - .2f);
                float twinkle = _reducedEffects ? .85f : .7f + .3f * Mathf.Sin(t * 5 + i * 1.7f);
                Place(_stars[i], at, Vector3.one * (.22f + (i % 3) * .08f), facingCentre(at), night * twinkle * Ease(.2f + i * .05f, .6f + i * .05f, t));
            }
            var moonFacing = Quaternion.LookRotation(-new Vector3(MoonAt.x - 1.2f, 0, MoonAt.z - 5.6f).normalized, Vector3.up) * Quaternion.Euler(90, 0, 0);
            float rise = Ease(.15f, .8f, t);
            Vector3 moonAt = MoonAt + Vector3.down * (1 - rise) * .9f;
            Place(_moon, moonAt, Vector3.one * MoonRadius * Mathf.Lerp(.7f, 1, rise), moonFacing, rise * leave);
            Place(_moonHalo, moonAt + moonFacing * Vector3.up * .01f, Vector3.one * MoonRadius * 1.25f, moonFacing, rise * leave * .8f);

            // THE RIBBONS whip UPWARD off her while the power surges (0.3 to 2.6 s), each on its own flutter.
            float surge = Ease(.3f, .9f, t) * (1 - Ease(2.5f, 2.9f, t)) * leave;
            for (int i = 0; i < _omenRibbons.Count; i++)
            {
                var r = OmenRibbons[i];
                float wave = Mathf.Sin(t * 9f + i * 1.7f);
                var dir = Quaternion.Euler(0f, r.A, 0f) * Vector3.forward;
                Vector3 root = dir * (r.Kind == 2 ? .30f : .36f) + Vector3.up * (r.Y + lift);
                Vector3 up = (Vector3.up * (.6f + .4f * surge) + dir * (.35f + .1f * wave)).normalized;
                float len = r.L * surge;
                Place(_omenRibbons[i], root + up * len * .5f, new Vector3(.07f, Mathf.Max(.001f, len), .015f),
                    Quaternion.FromToRotation(Vector3.up, up) * Quaternion.Euler(0f, r.A + 20f * wave, 0f), surge > .02f ? 1f : 0f);
            }

            // THE EYE: tiny, glitchy and pulsing in front of her (1.45 to 2.47), growing; thrown and growing on its way; landed, still
            // unstable, at the size the live cast starts at (see `OmenLandedRadius`).
            Vector3 eyeAt = OmenEyeAt(t);
            float radius, glitch;
            if (t < 1.45f) { radius = 0f; glitch = 1f; }
            else if (t < PhThrowAt)
            {
                float u = Mathf.Pow(Mathf.InverseLerp(1.45f, PhThrowAt, t), 1.4f);
                float pulse = _reducedEffects ? 1f : 1f + .24f * Mathf.Sin(t * 23f) + .12f * Mathf.Sin(t * 37f + 1.3f);
                radius = Mathf.Lerp(.08f, .30f, u) * pulse; glitch = 1f - .25f * u;
            }
            else if (t < PhLandAt)
            {
                float u = Mathf.InverseLerp(PhThrowAt, PhLandAt, t);
                float pulse = _reducedEffects ? 1f : 1f + .15f * Mathf.Sin(t * 29f);
                radius = Mathf.Lerp(.30f, .44f, u) * pulse; glitch = .8f;
            }
            else
            {
                // Landed: a jolt as it hits, then it hangs there, pulsing on two beats and glitching (it is not open yet).
                float u = t - PhLandAt;
                float jolt = u < .12f ? Mathf.Lerp(1.35f, 1f, u / .12f) : 1f;
                float pulse = _reducedEffects ? 1f : 1f + .12f * Mathf.Sin(t * 21f) + .06f * Mathf.Sin(t * 33f + .7f);
                radius = OmenLandedRadius * jolt * pulse; glitch = .8f;
            }
            if (_reducedEffects) glitch *= .3f;
            _omenEye.localPosition = eyeAt;
            _omenEye.localScale = Vector3.one * Mathf.Max(.001f, radius * 2f / .78f) * leave;
            _omenEye.gameObject.SetActive(radius > .001f && leave > .01f);
            if (_omenCosmos != null) { _omenCosmos.SetFloat("_Open", .78f); _omenCosmos.SetFloat("_Time0", t); _omenCosmos.SetFloat("_Glitch", glitch); }
            // The glow of it in her hands, lighting her face from below in the close-up: a soft ring round the forming eye.
            PlaceGlow(_omenPalmGlow, eyeAt + Vector3.back * .05f, Vector3.one * (radius * 3.2f + .15f), Quaternion.identity, (t > 1.45f && t < PhLandAt ? .45f : 0f) * leave);

            // THE LANDING: the emblem burst (the butterfly silhouette inside the flash), the shockwave, the ring and her sigils. The
            // two frames of the inverted picture are `PostProcess`.
            if (_omenFlash != null)
            {
                float f = t >= PhLandAt ? Mathf.Clamp01(1f - (t - PhLandAt) / .45f) : 0f;
                _omenFlash.localPosition = eyeAt + (PhLens(t, out var lensAt) ? (lensAt - eyeAt).normalized * .15f : Vector3.back * .1f);
                _omenFlash.localRotation = PhLens(t, out lensAt) ? Quaternion.LookRotation(eyeAt - lensAt, Vector3.up) * Quaternion.Euler(-90f, 0f, 0f) : Quaternion.Euler(-90f, 0f, 0f);
                _omenFlash.localScale = Vector3.one * (5f + 5f * (1f - f));
                _omenFlash.gameObject.SetActive(f > .01f);
                foreach (var m in _omenFlashInks) PhaisterProp.SetAlpha(m, .85f * f * leave);
            }
            Vector3 ground = _phGround + Vector3.up * .03f;
            float shockU = Mathf.InverseLerp(PhLandAt, PhLandAt + .3f, t);
            Place(_shock, ground, new Vector3(1, 1, 1) * Mathf.Lerp(.5f, 7.5f, 1 - (1 - shockU) * (1 - shockU)), Quaternion.identity,
                (t >= PhLandAt ? 1f - shockU : 0f) * leave);
            float ringOn = Ease(PhLandAt, PhLandAt + .3f, t) * leave;
            Place(_omenRing, ground, Vector3.one * 7.5f, Quaternion.Euler(0, -8f * t, 0), ringOn * .8f);
            for (int i = 0; i < _omenSigils.Count; i++)
            {
                // Clockwise seen from above: the angle decreases as they write.
                float a = -i * 45f;
                float at = PhLandAt + .1f + i * .06f;
                var pos = ground + Quaternion.Euler(0, a, 0) * new Vector3(0, .005f, 8.0f);
                Place(_omenSigils[i], pos, Vector3.one * 1.3f * Ease(at, at + .08f, t), Quaternion.Euler(0, a, 0), Ease(at, at + .08f, t) * leave);
            }

            // THE BUTTERFLIES.
            Vector3 palms = OmenPalms;
            for (int i = 0; i < _omenFlies.Count; i++)
            {
                var (b, w) = _omenFlies[i];
                var row = OmenFlies[i];
                Vector3 p; Vector3 heading; float size = row.Size;
                float intoEye = 1.45f + i * .04f;                 // when this one leaves the orbit for her palms
                if (t < intoEye)
                {
                    // SURGE: out of her sleeve or hat, wheeling CLOCKWISE round her, the orbit opening out.
                    float u = Mathf.Clamp01((t - row.Delay) / .5f);
                    float a = (row.A - 360f * row.Spin * Mathf.Max(0f, t - row.Delay)) * Mathf.Deg2Rad;
                    Vector3 from = new Vector3(row.Hat ? .15f : (i % 2 == 0 ? .5f : -.5f), row.Hat ? 1.7f : .9f, .1f) + Vector3.up * lift;
                    Vector3 orbit = new Vector3(Mathf.Sin(a) * row.R, row.Y + lift, Mathf.Cos(a) * row.R);
                    p = Vector3.Lerp(from, orbit, u * u * (3f - 2f * u));
                    heading = new Vector3(-Mathf.Cos(a), 0f, Mathf.Sin(a));
                    size *= Mathf.Clamp01(u * 3f) * (t >= row.Delay ? 1f : 0f);
                }
                else if (t < PhLandAt)
                {
                    // THE EYE: into the palms, LEFT TO RIGHT on the close-up's screen (from her +x side), shrinking to nothing.
                    float u = Mathf.Clamp01((t - intoEye) / .45f);
                    float a = (row.A - 360f * row.Spin * (intoEye - row.Delay)) * Mathf.Deg2Rad;
                    Vector3 orbit = new Vector3(Mathf.Sin(a) * row.R, row.Y + lift, Mathf.Cos(a) * row.R);
                    Vector3 side = palms + new Vector3(1.1f, .25f, .4f);
                    p = Vector3.Lerp(Vector3.Lerp(orbit, side, u), Vector3.Lerp(side, palms, u), u);
                    heading = palms - side;
                    size *= 1f - u * u;
                }
                else
                {
                    // THE MARK: out of the eye as it lands and round it CLOCKWISE, the maelstrom starting, wider as it gathers.
                    float u = t - PhLandAt;
                    float r = Mathf.Lerp(.6f, 1.2f + row.R * 1.2f, Mathf.Clamp01(u / .6f));
                    float a = (row.A - 360f * (row.Spin * 1.6f) * u) * Mathf.Deg2Rad;
                    p = eyeAt + new Vector3(Mathf.Sin(a) * r, (row.Y - 1.5f) * .6f, Mathf.Cos(a) * r);
                    heading = new Vector3(-Mathf.Cos(a), 0f, Mathf.Sin(a));
                    size *= Mathf.Clamp01(u / .25f) * 1.4f;
                }
                b.localPosition = p;
                if (heading.sqrMagnitude > .0001f) b.localRotation = Quaternion.LookRotation(heading.normalized, Vector3.up);
                b.localScale = Vector3.one * Mathf.Max(.0001f, size * 1.6f * leave);
                b.gameObject.SetActive(size > .01f && leave > .01f);
                float wingOpen = 10f + 60f * (.5f + .5f * Mathf.Sin(t * (3.2f + i % 4 * .5f) * Mathf.PI * 2f + row.A));
                if (w[0] != null) w[0].localRotation = Quaternion.AngleAxis(wingOpen, Vector3.forward);
                if (w[1] != null) w[1].localRotation = Quaternion.AngleAxis(-wingOpen, Vector3.forward);
            }

            SamplePhaisterBurst(t, leave, lift);
            SamplePhaisterMark(t, leave);
        }
    }
}
