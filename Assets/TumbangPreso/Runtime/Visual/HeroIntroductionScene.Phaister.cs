using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // PHAISTER, GRAND COVEN, 4.2 s. plan.md § 2.
        //
        // She sets the stage and enjoys the setup more than winning, so the stage is HER joke
        // landing: night falls behind her the moment the laugh breaks out, the moon rises over her
        // shoulder, and the moon-swallowing serpent from the Capul story she grew up with (her own
        // fictional image of it, CHARACTER_ORIGINS.md) coils round it until only a violet rim is
        // left. Every "ha" of `hero_phaister_ult` puffs two small stars off her, a shadow on the
        // road shows how high the laugh carried her, and when she points down the ritual ring
        // claims the ground the live Grand Coven then draws.
        //
        // ⚠️ NOTHING HERE IS A GENERIC RING OR BURST. The owner's brief: "Phaister laughing and
        // levitating ... make her act like herself", and "instead of giving each hero another
        // generic ring or explosion". Her one ground ring is her own WardCircle, and it arrives
        // only as the consequence of a gesture.
        // =========================================================================================
        private static readonly float[] PhaisterSyllables = { 1.15f, 1.41f, 1.68f, 1.91f, 2.11f };
        private int _nightGround, _nightSky, _moon, _eclipse, _corona, _halo, _shadow, _shadowRim, _ward, _serpentHead;
        private readonly List<int> _stars = new List<int>(8), _laughStars = new List<int>(10);
        private Mesh _serpent;
        private int[] _serpentTriangles;
        private readonly List<int> _serpentSubset = new List<int>(512);
        private Transform _serpentBody;
        private Renderer _serpentRenderer;
        private MaterialPropertyBlock _serpentBlock;
        private const int SerpentSegments = 48;
        // Behind her from shot C (the eclipse shot): along its line of sight, just inside the wall.
        private static readonly Vector3 MoonAt = new Vector3(-1.3f, 4.3f, -7.1f);
        private const float MoonRadius = 1.55f;

        private void BuildPhaister()
        {
            _nightGround = Wall("NightFallsGround", 0, 1.1f, new Color(.06f, .02f, .09f, .88f));
            _nightSky = Wall("NightFallsSky", 1.1f, 11, new Color(.16f, .06f, .25f, .84f), emission: .18f);
            for (int i = 0; i < 7; i++)
                _stars.Add(Add("NightStar" + i, VfxShapes.TwoSided(VfxShapes.Star(4, .38f, 60 + i)), new Color(1, .93f, .78f, .9f), .6f));
            _moon = Add("RisingMoon", MoonDisc(0), new Color(.95f, .91f, .8f, .95f), .5f);
            _halo = Add("MoonHalo", VfxShapes.TwoSided(VfxShapes.Collar(40, .02f, .8f)), new Color(.72f, .62f, .95f, .35f), .45f);
            BuildSerpent();
            _eclipse = Add("EclipseBody", MoonDisc(1), new Color(.035f, .012f, .075f, .97f), .05f);
            _corona = Add("EclipseCorona", VfxShapes.TwoSided(VfxShapes.Corona(26, .74f, .4f, 7)), new Color(.72f, .28f, .95f, .85f), .7f);
            for (int i = 0; i < 10; i++)
                _laughStars.Add(Add("LaughStar" + i, VfxShapes.TwoSided(VfxShapes.Star(5, .45f, 80 + i)), new Color(.82f, .5f, 1, .95f), .7f));
            _shadow = Add("LevitationShadow", VfxShapes.Splat(16, .08f, 5), new Color(.08f, .02f, .12f, .6f), 0);
            _shadowRim = Add("LevitationRim", VfxShapes.Collar(24, .015f, .88f), new Color(.63f, .23f, .86f, .7f), .5f);
            _ward = Add("ClaimedGround", VfxShapes.WardCircle(12, 4, .03f, 11), new Color(.63f, .23f, .86f, .9f), .55f);
        }

        private static Mesh MoonDisc(int seed)
        {
            var mesh = VfxShapes.TwoSided(VfxShapes.Splat(40, 0, 7 + seed));
            var normals = new Vector3[mesh.vertexCount];
            for (int i = 0; i < normals.Length; i++) normals[i] = Vector3.up;
            mesh.normals = normals; return mesh;
        }

        /// <summary>
        /// The serpent is a thick band round the moon that is revealed segment by segment, so it
        /// COILS rather than fades in. A triangle subset per frame is cheap at 48 segments.
        /// </summary>
        private void BuildSerpent()
        {
            _serpent = new Mesh { name = "Phaister moon serpent" };
            var vertices = new Vector3[(SerpentSegments + 1) * 2];
            _serpentTriangles = new int[SerpentSegments * 12];
            for (int i = 0; i <= SerpentSegments; i++)
            {
                float a = i * Mathf.PI * 2 / SerpentSegments;
                // The body thickens toward the head and thins at the tail, so it reads as a creature.
                float girth = Mathf.Lerp(.07f, .2f, i / (float)SerpentSegments);
                var dir = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                vertices[i * 2] = dir * (1.02f - girth * .4f); vertices[i * 2 + 1] = dir * (1.02f + girth);
                if (i == SerpentSegments) continue;
                int n = i * 2, t = i * 12;
                int[] faces = { n, n + 1, n + 2, n + 2, n + 1, n + 3, n, n + 2, n + 1, n + 2, n + 3, n + 1 };
                for (int k = 0; k < 12; k++) _serpentTriangles[t + k] = faces[k];
            }
            _serpent.vertices = vertices; _serpent.triangles = _serpentTriangles;
            var normals = new Vector3[vertices.Length];
            for (int i = 0; i < normals.Length; i++) normals[i] = Vector3.up;
            _serpent.normals = normals; _serpent.RecalculateBounds();
            var go = VfxShapes.Stand(_root.transform, "MoonSerpent", _serpent, 1);
            _serpentRenderer = go.GetComponent<Renderer>();
            _serpentRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            VfxMaterial.Ghost(_serpentRenderer, new Color(.1f, .04f, .16f, .97f), .08f);
            _serpentBody = go.transform; _serpentBlock = new MaterialPropertyBlock();
            _serpentHead = Add("SerpentHead", VfxShapes.TwoSided(VfxShapes.Star(3, .55f, 13)), new Color(.1f, .04f, .16f, .97f), .08f);
        }

        private void SamplePhaister(float t)
        {
            float leave = 1 - Ease(Seconds - .45f, Seconds - .05f, t);
            // Night falls with the laugh (1.0 to 1.6 s), not before: the sly beat plays on the
            // real court so the change of world IS the punchline.
            float night = Ease(1.0f, 1.6f, t) * leave;
            Tint(_nightGround, night); Tint(_nightSky, night);

            Quaternion facingCentre(Vector3 at) => Quaternion.LookRotation(-new Vector3(at.x, 0, at.z).normalized, Vector3.up) * Quaternion.Euler(90, 0, 0);
            for (int i = 0; i < _stars.Count; i++)
            {
                float angle = (-150 + i * 43) * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Sin(angle) * 7.6f, 3.4f + (i * 37 % 5) * .55f, Mathf.Cos(angle) * 7.6f - .2f);
                float twinkle = _reducedEffects ? .85f : .7f + .3f * Mathf.Sin(t * 5 + i * 1.7f);
                Place(_stars[i], at, Vector3.one * (.10f + (i % 3) * .04f), facingCentre(at), night * twinkle * Ease(1.3f + i * .06f, 1.7f + i * .06f, t));
            }

            // The moon rises over her shoulder as she reaches the top of the laugh.
            var moonFacing = Quaternion.LookRotation(-new Vector3(MoonAt.x - .6f, 0, MoonAt.z - 5).normalized, Vector3.up) * Quaternion.Euler(90, 0, 0);
            float rise = Ease(2.2f, 2.75f, t);
            Vector3 moonAt = MoonAt + Vector3.down * (1 - rise) * .9f;
            Place(_moon, moonAt, Vector3.one * MoonRadius * Mathf.Lerp(.7f, 1, rise), moonFacing, rise * leave);
            Place(_halo, moonAt + moonFacing * Vector3.up * .01f, Vector3.one * MoonRadius * 1.25f, moonFacing, rise * (1 - Ease(3.1f, 3.4f, t)) * leave);

            // The serpent coils round it (2.6 to 3.35 s), head first, then the dark swallows the
            // moon and only the rim is left burning.
            float coil = Ease(2.6f, 3.35f, t);
            int shown = Mathf.Clamp(Mathf.RoundToInt(coil * SerpentSegments), 0, SerpentSegments);
            _serpentSubset.Clear();
            for (int i = 0; i < shown * 12; i++) _serpentSubset.Add(_serpentTriangles[i]);
            _serpent.SetTriangles(_serpentSubset, 0);
            float spin = -coil * 40;
            _serpentBody.localPosition = moonAt + moonFacing * Vector3.up * .02f;
            _serpentBody.localRotation = moonFacing * Quaternion.Euler(0, spin, 0);
            _serpentBody.localScale = Vector3.one * MoonRadius;
            float swallow = Ease(3.3f, 3.6f, t);
            var body = new Color(.1f, .04f, .16f, .97f); body.a *= (shown > 0 ? 1 : 0) * leave * (1 - swallow * .5f);
            _serpentBlock.SetColor("_Color", body); _serpentBlock.SetColor("_BaseColor", body);
            _serpentRenderer.SetPropertyBlock(_serpentBlock); _serpentRenderer.enabled = shown > 0 && leave > .01f;
            float headAngle = (shown / (float)SerpentSegments) * Mathf.PI * 2;
            var headLocal = new Vector3(Mathf.Sin(headAngle), 0, Mathf.Cos(headAngle)) * 1.14f;
            Place(_serpentHead, moonAt + moonFacing * (Quaternion.Euler(0, spin, 0) * headLocal * MoonRadius) + moonFacing * Vector3.up * .03f,
                Vector3.one * .42f, moonFacing * Quaternion.Euler(0, spin - headAngle * Mathf.Rad2Deg + 90, 0), (shown > 0 && shown < SerpentSegments ? 1 : 0) * leave);

            Place(_eclipse, moonAt + moonFacing * Vector3.up * .04f, Vector3.one * MoonRadius * Mathf.Lerp(.2f, .97f, swallow), moonFacing, swallow * leave);
            float burn = Ease(3.4f, 3.7f, t);
            Place(_corona, moonAt + moonFacing * Vector3.up * .05f, Vector3.one * MoonRadius * Mathf.Lerp(1.05f, 1.3f, burn),
                moonFacing * Quaternion.Euler(0, t * 12, 0), burn * leave);

            // Every "ha" throws two small stars off her, left and right, and they drift away.
            for (int i = 0; i < _laughStars.Count; i++)
            {
                float start = PhaisterSyllables[i / 2], age = t - start;
                bool alive = age >= 0 && age < .45f;
                float side = i % 2 == 0 ? 1 : -1;
                float u = Mathf.Clamp01(age / .45f);
                var from = HeadPoint + Vector3.down * .15f + Vector3.forward * .25f;
                var at = from + new Vector3(side * (.25f + u * .55f), .15f + u * .5f - u * u * .25f, .1f + u * .2f);
                Place(_laughStars[i], at, Vector3.one * .09f * (1 - u * .4f), Quaternion.Euler(-90 + side * 15, 0, side * u * 90), alive ? (1 - u) * leave : 0);
            }

            // The shadow on the road says how high the laugh carried her.
            float lift = LiftAt(t);
            float airborne = Ease(.02f, .2f, lift);
            Place(_shadow, Vector3.up * .02f, new Vector3(.75f, 1, .75f) * Mathf.Lerp(1, .7f, Mathf.Clamp01(lift / .6f)), Quaternion.identity, airborne * leave * .9f);
            Place(_shadowRim, Vector3.up * .025f, new Vector3(.95f, 1, .95f) * Mathf.Lerp(1, .8f, Mathf.Clamp01(lift / .6f)), Quaternion.Euler(0, t * 30, 0), airborne * leave * .7f);

            // The pointed hand claims the ground: the ring snaps out from under her (3.45 s).
            float claim = Ease(3.42f, 3.62f, t);
            Place(_ward, Vector3.up * .03f, new Vector3(1, 1, 1) * Mathf.Lerp(.4f, 2.3f, claim), Quaternion.Euler(0, 40 + t * 18, 0), claim * leave);
        }
    }
}
