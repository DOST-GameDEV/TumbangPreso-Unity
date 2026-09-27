using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // PHAISTER, OMEN, 4.0 s (HERO-10, 2026-09-27; plan.md 4.4; the body and shots are `tools/author_ultimate_intros.py`
        // `phaister()`). Replaces GRAND COVEN's laugh and the moon serpent, both rejected by the owner.
        //
        // "The omen pours out of her, and she throws it at them." Every moving part, and its direction:
        //  * THE NIGHT: the stage goes to her night at once (Castorice's domain): violet-black walls, a pale lilac moon rising
        //    behind her, a few stars.
        //  * THE RIBBONS: strips of her robe and hair whip UPWARD off her while the power surges (the rig has no cloth bones);
        //    owner: *"clothes are flying on her"*.
        //  * THE BUTTERFLIES (typed rows): out of her sleeves and hat, wheeling CLOCKWISE round her (SURGE); streaming LEFT TO
        //    RIGHT on screen into the space between her palms and vanishing into it (THE EYE); then round the opened eye in a
        //    clockwise maelstrom (OMEN). Wings slow, speed in the paths.
        //  * THE EYE (`CosmosEye.shader`): born between her palms TINY, GLITCHY and PULSING and growing (owner: *"glitchy and
        //    unstable as fuck when forming (make it pulsate?) it starts out small and gradually gets bigger"*); thrown forward
        //    and up, growing as it flies; it SNAPS OPEN into the window into space with an overshoot and steadies.
        //  * THE IMPACT: two frames of her dark over everything with a magenta butterfly silhouette (Seele's impact frame,
        //    Castorice's emblem), a shockwave racing out over the court, her sigils writing themselves clockwise round the ring.
        // Nothing here runs on Update: every piece is posed from `t` in `SamplePhaister`.
        // =========================================================================================

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

        private static readonly Vector3 OmenLands = new Vector3(0f, 2.3f, 4.0f);
        private static readonly Vector3 MoonAt = new Vector3(-4.2f, 5.3f, -5.4f);
        private const float MoonRadius = 1.55f, OmenOpenAt = 2.80f, OmenThrowAt = 2.55f, OmenEyeRadius = 1.2f;

        private int _nightGround, _nightSky, _moon, _moonHalo, _omenPalmGlow, _impactDark, _shock, _omenRing;
        private readonly List<int> _stars = new List<int>(8), _omenRibbons = new List<int>(10), _omenSigils = new List<int>(8);
        private readonly List<(Transform T, Transform[] W)> _omenFlies = new List<(Transform, Transform[])>();
        private Transform _omenEye, _omenFlash;
        private Material _omenCosmos;
        private readonly List<Material> _omenFlashInks = new List<Material>();

        private void BuildPhaister()
        {
            _nightGround = Wall("NightFallsGround", 0, 1.1f, new Color(.07f, .02f, .11f, .90f));
            _nightSky = Wall("NightFallsSky", 1.1f, 11, new Color(.14f, .05f, .24f, .88f), emission: .16f, cap: true);
            for (int i = 0; i < 7; i++)
                _stars.Add(Add("NightStar" + i, VfxShapes.TwoSided(VfxShapes.Star(4, .38f, 60 + i)), new Color(.86f, .78f, 1f, .9f), .6f));
            _moon = Add("RisingMoon", MoonDisc(0), new Color(.86f, .80f, .96f, .95f), .5f);
            _moonHalo = Add("MoonHalo", VfxShapes.TwoSided(VfxShapes.Collar(40, .02f, .8f)), new Color(.62f, .42f, .95f, .35f), .45f);
            var ribbonColours = new[] { new Color(.10f, .08f, .14f, .96f), new Color(.29f, .12f, .47f, .96f), new Color(.85f, .09f, .43f, .96f) };
            foreach (var r in OmenRibbons)
                _omenRibbons.Add(Add("OmenRibbon", VfxShapes.TwoSided(VfxShapes.Prism(4, 1f, 1f, 1)), ribbonColours[r.Kind], r.Kind == 2 ? .2f : .05f));
            _omenPalmGlow = AddGlow("OmenPalmGlow", new Color(.86f, .22f, .62f, 1f), falloff: 2.4f, core: .8f, lift: .1f);
            _impactDark = Add("OmenImpactDark", VfxShapes.TwoSided(VfxShapes.Splat(24, 0f, 3)), new Color(.05f, .01f, .09f, .9f), 0f);
            _shock = Add("OmenShock", VfxShapes.TwoSided(VfxShapes.Hollow(48, .86f, .1f, 4)), new Color(.80f, .36f, 1f, .85f), 1.6f);
            _omenRing = Add("OmenRing", VfxShapes.TwoSided(VfxShapes.Hollow(64, .94f, 0f, 9)), new Color(.74f, .40f, 1f, .8f), 1.2f);
            for (int i = 0; i < 8; i++)
                _omenSigils.Add(Add("OmenSigil" + i, VfxShapes.TwoSided(VfxShapes.Rune(41 + i, .12f)), new Color(.78f, .42f, 1f, .95f), 1.4f));

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

            // The emblem flash: a butterfly silhouette in her magenta, the impact frame's picture.
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

        /// <summary>Where the eye is at <paramref name="t"/>: between her palms while it forms, on a rising arc once thrown.</summary>
        private Vector3 OmenEyeAt(float t)
        {
            Vector3 palms = BothPalms + new Vector3(0f, .05f, .22f);
            if (t < OmenThrowAt) return palms;
            float u = Mathf.Clamp01((t - OmenThrowAt) / (OmenOpenAt - OmenThrowAt));
            return Vector3.Lerp(palms, OmenLands, u * (2f - u)) + Vector3.up * Mathf.Sin(u * Mathf.PI) * .6f;
        }

        private void SamplePhaister(float t)
        {
            float leave = 1 - Ease(Seconds - .45f, Seconds - .05f, t);
            float lift = LiftAt(t);

            // THE NIGHT, at once: the change of world is the first thing that happens.
            float night = Ease(0f, .4f, t) * leave;
            Tint(_nightGround, night); Tint(_nightSky, night);
            Quaternion facingCentre(Vector3 at) => Quaternion.LookRotation(-new Vector3(at.x, 0, at.z).normalized, Vector3.up) * Quaternion.Euler(90, 0, 0);
            for (int i = 0; i < _stars.Count; i++)
            {
                float angle = (-150 + i * 43) * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Sin(angle) * 7.6f, 3.4f + (i * 37 % 5) * .55f, Mathf.Cos(angle) * 7.6f - .2f);
                float twinkle = _reducedEffects ? .85f : .7f + .3f * Mathf.Sin(t * 5 + i * 1.7f);
                Place(_stars[i], at, Vector3.one * (.10f + (i % 3) * .04f), facingCentre(at), night * twinkle * Ease(.3f + i * .06f, .7f + i * .06f, t));
            }
            var moonFacing = Quaternion.LookRotation(-new Vector3(MoonAt.x - 1.2f, 0, MoonAt.z - 5.6f).normalized, Vector3.up) * Quaternion.Euler(90, 0, 0);
            float rise = Ease(.2f, .9f, t);
            Vector3 moonAt = MoonAt + Vector3.down * (1 - rise) * .9f;
            Place(_moon, moonAt, Vector3.one * MoonRadius * Mathf.Lerp(.7f, 1, rise), moonFacing, rise * leave);
            Place(_moonHalo, moonAt + moonFacing * Vector3.up * .01f, Vector3.one * MoonRadius * 1.25f, moonFacing, rise * leave * .8f);

            // THE RIBBONS whip UPWARD off her while the power surges (0.3 to 2.9 s), each on its own flutter.
            float surge = Ease(.3f, .9f, t) * (1 - Ease(2.8f, 3.2f, t)) * leave;
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

            // THE EYE: tiny, glitchy and pulsing between her palms (1.5 to 2.55), growing; thrown and growing (2.55 to 2.8);
            // snapping open with an overshoot (2.8) and steadying.
            Vector3 eyeAt = OmenEyeAt(t);
            float radius, glitch;
            if (t < 1.5f) { radius = 0f; glitch = 1f; }
            else if (t < OmenThrowAt)
            {
                float u = Mathf.Pow(Mathf.InverseLerp(1.5f, OmenThrowAt, t), 1.4f);
                float pulse = _reducedEffects ? 1f : 1f + .24f * Mathf.Sin(t * 23f) + .12f * Mathf.Sin(t * 37f + 1.3f);
                // v2: bigger while it forms (film v5: at 4 to 24 cm the cosmos did not show, only the glow).
                radius = Mathf.Lerp(.08f, .38f, u) * pulse; glitch = 1f - .25f * u;
            }
            else if (t < OmenOpenAt)
            {
                float u = Mathf.InverseLerp(OmenThrowAt, OmenOpenAt, t);
                float pulse = _reducedEffects ? 1f : 1f + .15f * Mathf.Sin(t * 29f);
                radius = Mathf.Lerp(.38f, .62f, u) * pulse; glitch = .8f;
            }
            else
            {
                float u = t - OmenOpenAt;
                radius = OmenEyeRadius * (u < .15f ? Mathf.Lerp(.46f, 1.12f, u / .15f) : u < .3f ? Mathf.Lerp(1.12f, 1f, (u - .15f) / .15f) : 1f);
                glitch = Mathf.Clamp01(1f - u / .3f) * .6f;
            }
            if (_reducedEffects) glitch *= .3f;
            _omenEye.localPosition = eyeAt;
            _omenEye.localScale = Vector3.one * Mathf.Max(.001f, radius * 2f / .78f) * leave;
            _omenEye.gameObject.SetActive(radius > .001f && leave > .01f);
            if (_omenCosmos != null) { _omenCosmos.SetFloat("_Open", .78f); _omenCosmos.SetFloat("_Time0", t); _omenCosmos.SetFloat("_Glitch", glitch); }
            // The glow of it in her hands, lighting her face from below in the close-up.
            // v2: a soft ring of light round the forming eye, not over it (film v5: the glow drowned the eye into a pink blob).
            PlaceGlow(_omenPalmGlow, eyeAt + Vector3.back * .05f, Vector3.one * (radius * 3.2f + .15f), Quaternion.identity, (t > 1.5f && t < OmenOpenAt ? .45f : 0f) * leave);

            // THE IMPACT: two frames of her dark with the emblem, then the shockwave and the ring on the court under the eye.
            bool impact = !_reducedEffects && t >= OmenOpenAt && t < OmenOpenAt + .07f;
            Place(_impactDark, eyeAt, Vector3.one * 30f, Quaternion.LookRotation(Vector3.back) * Quaternion.Euler(90, 0, 0), impact ? 1f : 0f);
            if (_omenFlash != null)
            {
                float f = t >= OmenOpenAt ? Mathf.Clamp01(1f - (t - OmenOpenAt) / .45f) : 0f;
                _omenFlash.localPosition = eyeAt + Vector3.back * .1f;
                _omenFlash.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                _omenFlash.localScale = Vector3.one * (6f + 4f * (1f - f));
                _omenFlash.gameObject.SetActive(f > .01f);
                foreach (var m in _omenFlashInks) PhaisterProp.SetAlpha(m, .85f * f * leave);
            }
            Vector3 ground = new Vector3(OmenLands.x, .03f, OmenLands.z);
            float shockU = Mathf.InverseLerp(OmenOpenAt, OmenOpenAt + .3f, t);
            Place(_shock, ground, new Vector3(1, 1, 1) * Mathf.Lerp(.5f, 6f, 1 - (1 - shockU) * (1 - shockU)), Quaternion.identity,
                (t >= OmenOpenAt ? 1f - shockU : 0f) * leave);
            float ringOn = Ease(OmenOpenAt, OmenOpenAt + .3f, t) * leave;
            Place(_omenRing, ground, Vector3.one * 3.6f, Quaternion.Euler(0, -8f * t, 0), ringOn);
            for (int i = 0; i < _omenSigils.Count; i++)
            {
                // Clockwise seen from above: the angle decreases as they write.
                float a = -i * 45f;
                float at = OmenOpenAt + .1f + i * .06f;
                var pos = ground + Quaternion.Euler(0, a, 0) * new Vector3(0, .005f, 4.1f);
                Place(_omenSigils[i], pos, Vector3.one * .9f * Ease(at, at + .08f, t), Quaternion.Euler(0, a, 0), Ease(at, at + .08f, t) * leave);
            }

            // THE BUTTERFLIES.
            Vector3 palms = BothPalms + new Vector3(0f, .05f, .22f);
            for (int i = 0; i < _omenFlies.Count; i++)
            {
                var (b, w) = _omenFlies[i];
                var row = OmenFlies[i];
                Vector3 p; Vector3 heading; float size = row.Size;
                float intoEye = 1.5f + i * .045f;                 // when this one leaves the orbit for her palms
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
                else if (t < OmenOpenAt)
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
                    // OMEN: round the opened eye, CLOCKWISE, tighter and faster in, from the rim of it outward.
                    float u = t - OmenOpenAt;
                    float r = Mathf.Lerp(1.4f, 1.4f + row.R * 1.4f, Mathf.Clamp01(u / .5f));
                    float a = (row.A - 360f * (row.Spin * 1.8f) * u) * Mathf.Deg2Rad;
                    p = eyeAt + new Vector3(Mathf.Sin(a) * r, (row.Y - 1.5f) * .6f, Mathf.Cos(a) * r);
                    heading = new Vector3(-Mathf.Cos(a), 0f, Mathf.Sin(a));
                    size *= Mathf.Clamp01(u / .2f) * 1.4f;
                }
                b.localPosition = p;
                if (heading.sqrMagnitude > .0001f) b.localRotation = Quaternion.LookRotation(heading.normalized, Vector3.up);
                b.localScale = Vector3.one * Mathf.Max(.0001f, size * 1.6f * leave);
                b.gameObject.SetActive(size > .01f && leave > .01f);
                float wingOpen = 10f + 60f * (.5f + .5f * Mathf.Sin(t * (3.2f + i % 4 * .5f) * Mathf.PI * 2f + row.A));
                if (w[0] != null) w[0].localRotation = Quaternion.AngleAxis(wingOpen, Vector3.forward);
                if (w[1] != null) w[1].localRotation = Quaternion.AngleAxis(-wingOpen, Vector3.forward);
            }
        }
    }
}
