using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ OMEN, SEEN IN PLAY (HERO-10, plan 4.4, every row of its moving-parts table). The pull itself is gameplay
    /// (`HeroHazards.SeanceVoidComponent` on `Abilities.VoodooBlackHole`); this is everything a player sees, on every peer,
    /// posed from one clock (`StepTo`), so a rejoiner or a replay can be shown any moment of it.
    ///
    /// | Row | Part | Time |
    /// |---|---|---|
    /// | 1 | her sigils write themselves CLOCKWISE round the 7.5 m ring on the court, then the ring turns slowly | the cast, then the pull |
    /// | 2 | cloth ribbons off her waist and hat whip UPWARD while the power surges through her (the rig has no cloth) | the cast |
    /// | 3 | black butterflies pour out of her sleeves and hat and stream in a low arc to the spot | the cast |
    /// | 4 | the black eye opens at 1.1 m over the spot, overshooting | the end of the cast |
    /// | 5 | the eye is a window into the cosmos (`CosmosEye.shader`), two accretion rings turning round it | the pull |
    /// | 6 | one shockwave ring races out to the boundary; a butterfly-shaped flash on the court (her emblem) | the landing |
    /// | 7 | the MAELSTROM: butterflies orbit CLOCKWISE, faster toward the middle, each spiralling IN and diving into the core, then coming round again from the rim | the pull |
    /// | 8 | thin violet streaks slide INWARD along the court toward the eye | the pull |
    /// | 11 | every butterfly bursts UP into the sky at once, the eye pops, three stay and flutter off one by one | the end |
    /// | 14 | the court inside the ring dims to her violet | the pull |
    ///
    /// Rows 9, 10 (bodies and slippers dragged) are the gameplay pull; 12 and 13 (the screen veils) are owed.
    /// Direction rules (plan 4.4): everything of hers turns clockwise seen from above; wings stay slow, speed lives in paths.
    /// </summary>
    public sealed class PhaisterOmen : MonoBehaviour, IVfxTimeline
    {
        // ⚠️ v3 (owner, of the first eye, a black ball in a spiky ring: *"needs serious refinement"*, *"make it look bigger and make
        // the actual blackwhole prettier like ur peeking into the cosmos"*): the eye is a WINDOW INTO SPACE now, `CosmosEye.shader`
        // on a camera-facing disc, 2.4 m across, with two thin accretion rings turning round it. `CosmosOpen` is how much of the
        // quad the hole fills at full open, so its outer glow still fits inside the quad.
        public const float EyeSize = 2.4f, EndSeconds = 1.2f, CosmosOpen = 0.78f;
        private float EyeHeight = VoodooRules.HigopMinHeight;

        /// <summary>
        /// ⚠️ v2 (film v3): at the rows' own sizes the maelstrom read as scattered black specks from the court camera 13 m away,
        /// crows rather than a storm. Every butterfly is drawn this much larger (a 0.34 m prop at about 0.8 m).
        /// </summary>
        public const float FlyScale = 1.9f;

        // Row 7, typed by hand: (start radius, height, speed factor, phase deg, size, wing Hz, inward metres a second).
        private static readonly (float R, float Y, float K, float Phase, float Size, float Hz, float In)[] Orbit =
        {
            (7.0f, 1.9f, 1.00f,   0f, 1.30f, 3.2f, 1.1f), (6.6f, 1.5f, 0.94f,  22f, 1.20f, 3.8f, 0.9f), (6.2f, 2.2f, 1.06f,  41f, 1.40f, 3.0f, 1.3f),
            (5.8f, 1.2f, 0.90f,  67f, 1.10f, 4.2f, 1.0f), (5.5f, 1.7f, 1.10f,  83f, 1.25f, 3.5f, 1.2f), (5.1f, 0.9f, 0.98f, 104f, 1.15f, 4.6f, 0.8f),
            (4.8f, 2.0f, 1.02f, 126f, 1.35f, 3.3f, 1.4f), (4.4f, 1.4f, 0.92f, 147f, 1.05f, 4.0f, 0.9f), (4.1f, 1.8f, 1.08f, 162f, 1.30f, 3.6f, 1.1f),
            (3.7f, 1.1f, 0.96f, 188f, 1.00f, 4.8f, 0.7f), (3.4f, 1.6f, 1.04f, 205f, 1.20f, 3.9f, 1.0f), (3.0f, 1.3f, 0.99f, 229f, 1.10f, 4.4f, 0.8f),
            (2.7f, 1.0f, 1.12f, 246f, 0.95f, 5.0f, 0.9f), (2.3f, 1.2f, 0.95f, 270f, 1.05f, 4.1f, 0.6f), (2.0f, 0.9f, 1.05f, 291f, 0.90f, 5.2f, 0.7f),
            (1.6f, 1.1f, 1.00f, 314f, 0.95f, 4.7f, 0.5f), (7.2f, 1.1f, 0.97f, 333f, 1.25f, 3.4f, 1.2f), (6.8f, 2.4f, 1.03f, 352f, 1.35f, 3.1f, 1.0f),
            (6.4f, 0.8f, 0.91f,  12f, 1.15f, 4.3f, 1.1f), (6.0f, 1.9f, 1.09f,  31f, 1.30f, 3.7f, 1.2f), (5.6f, 2.3f, 0.93f,  55f, 1.40f, 3.2f, 0.9f),
            (5.3f, 1.0f, 1.01f,  76f, 1.10f, 4.5f, 1.3f), (4.9f, 1.6f, 1.07f,  96f, 1.20f, 3.8f, 1.0f), (4.6f, 2.1f, 0.95f, 117f, 1.30f, 3.4f, 1.1f),
            (4.2f, 0.8f, 1.04f, 139f, 1.00f, 4.9f, 0.8f), (3.9f, 1.5f, 0.98f, 156f, 1.15f, 4.0f, 1.2f), (3.5f, 2.0f, 1.10f, 178f, 1.25f, 3.6f, 0.9f),
            (3.2f, 0.9f, 0.94f, 197f, 0.95f, 5.1f, 0.7f), (2.9f, 1.7f, 1.06f, 219f, 1.10f, 4.2f, 1.0f), (2.5f, 1.4f, 1.00f, 238f, 1.00f, 4.6f, 0.8f),
            (2.2f, 0.7f, 1.08f, 259f, 0.90f, 5.4f, 0.6f), (1.8f, 1.3f, 0.97f, 281f, 0.95f, 4.8f, 0.6f), (1.4f, 1.0f, 1.03f, 302f, 0.85f, 5.6f, 0.5f),
            (7.4f, 1.6f, 0.99f, 322f, 1.30f, 3.3f, 1.3f), (7.0f, 0.9f, 1.05f, 341f, 1.20f, 4.0f, 1.0f), (6.5f, 2.1f, 0.92f,   5f, 1.35f, 3.0f, 1.2f),
        };

        // Row 2, typed: ribbons off her body during the cast (angle round her deg, height m, length m, colour: 0 robe, 1 trim, 2 hair).
        private static readonly (float A, float Y, float L, int Colour)[] Ribbons =
        {
            (20f, 0.42f, 0.55f, 0), (75f, 0.40f, 0.62f, 1), (130f, 0.44f, 0.50f, 0), (185f, 0.41f, 0.66f, 0), (240f, 0.43f, 0.58f, 1),
            (300f, 0.40f, 0.60f, 0), (160f, 1.30f, 0.46f, 2), (205f, 1.34f, 0.52f, 2), (250f, 1.28f, 0.42f, 2),
        };

        // Row 8, typed: pull streaks (angle deg, phase 0..1, length m).
        private static readonly (float A, float Phase, float L)[] Streaks =
        {
            (10f, 0.00f, 0.9f), (47f, 0.35f, 0.7f), (86f, 0.70f, 1.0f), (121f, 0.15f, 0.8f), (163f, 0.55f, 0.9f), (199f, 0.85f, 0.7f),
            (238f, 0.25f, 1.0f), (276f, 0.60f, 0.8f), (311f, 0.05f, 0.9f), (348f, 0.45f, 0.7f),
        };

        private static readonly Color Violet = new Color(0.60f, 0.30f, 0.92f), Magenta = new Color(0.88f, 0.16f, 0.50f);
        private static readonly Color[] RibbonColours = { new Color(0.10f, 0.08f, 0.14f), new Color(0.29f, 0.12f, 0.47f), new Color(0.85f, 0.09f, 0.43f) };

        private float _cast, _pull, _t;
        private Transform _her;
        private Transform _eye, _ring, _dim, _shock, _flash, _accretionA, _accretionB;
        private Material _cosmos, _ringInk, _dimInk, _shockInk, _flashInk, _accretionInkA, _accretionInkB;
        private readonly List<(Transform T, Material M, float At)> _sigils = new List<(Transform, Material, float)>();
        private readonly List<(Transform T, Transform[] W)> _flies = new List<(Transform, Transform[])>();
        private readonly List<Transform> _ribbons = new List<Transform>();
        private readonly List<(Transform T, Material M)> _streaks = new List<(Transform, Material)>();

        public float LifeSeconds => _cast + _pull + EndSeconds;

        /// <summary>
        /// Plays OMEN at <paramref name="at"/> (a ground point): a <paramref name="castSeconds"/> cast from <paramref name="her"/>,
        /// then <paramref name="pullSeconds"/> of the eye, then the end. <paramref name="startAt"/> starts part way (a rejoiner).
        /// </summary>
        public static PhaisterOmen Play(Vector3 at, Transform her, float castSeconds, float pullSeconds, float startAt = 0f, float eyeHeight = VoodooRules.HigopMinHeight)
        {
            var go = new GameObject("PhaisterOmen");
            go.transform.position = VfxShapes.GroundPoint(at);
            var fx = go.AddComponent<PhaisterOmen>();
            fx._cast = castSeconds; fx._pull = pullSeconds; fx._her = her; fx.EyeHeight = eyeHeight;
            fx.Build();
            fx.StepTo(startAt);
            return fx;
        }

        private void Build()
        {
            float R = VoodooRules.HigopRadius;
            // Row 14: the dim, a flat disc of her dark over the court inside the ring.
            var dim = VfxShapes.Lay(transform, "OmenDim", VfxShapes.Splat(40, 0.0f, 5), R, 0.02f);
            VfxMaterial.Ghost(dim.GetComponent<Renderer>(), new Color(0.12f, 0.04f, 0.20f, 0f), 0f);
            VfxShapes.DrapeToGround(dim, 0.02f);
            _dim = dim.transform; _dimInk = dim.GetComponent<Renderer>().sharedMaterial;
            // Row 1: the ring line and twelve sigils round it.
            var ring = VfxShapes.Lay(transform, "OmenRing", VfxShapes.Hollow(64, 0.94f, 0.0f, 9), R, 0.03f);
            VfxMaterial.Ghost(ring.GetComponent<Renderer>(), new Color(0.74f, 0.40f, 1.0f, 0f), 1.2f);
            VfxShapes.DrapeToGround(ring, 0.03f);
            _ring = ring.transform; _ringInk = ring.GetComponent<Renderer>().sharedMaterial;
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30f + (i % 3) * 4f;
                var s = VfxShapes.Lay(transform, "OmenSigil", VfxShapes.Rune(31 + i, 0.12f), 1.30f + 0.15f * (i % 2), 0.035f, -a);
                s.transform.localPosition = Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0.035f, R + 0.55f);
                VfxMaterial.Ghost(s.GetComponent<Renderer>(), new Color(0.78f, 0.42f, 1.0f, 0f), 1.4f);
                VfxShapes.DrapeToGround(s, 0.035f);
                // Clockwise seen from above: the angle decreases, so the writing order runs 0, 330, 300 ...
                _sigils.Add((s.transform, s.GetComponent<Renderer>().sharedMaterial, (12 - i) % 12 / 12f));
            }
            // Row 4 and 5: the eye, a window into the cosmos (`Resources/Shaders/CosmosEye`), and its accretion rings.
            var eye = GameObject.CreatePrimitive(PrimitiveType.Quad);
            eye.name = "OmenEye"; VfxMaterial.StripCollider(eye);
            eye.transform.SetParent(transform, false); eye.transform.localPosition = Vector3.up * EyeHeight;
            var shader = Resources.Load<Shader>("Shaders/CosmosEye");
            var r0 = eye.GetComponent<Renderer>();
            if (shader != null) { _cosmos = new Material(shader); r0.sharedMaterial = _cosmos; VfxRenderTag.Own(eye, _cosmos); }
            else { Debug.LogWarning("[PhaisterOmen] Shaders/CosmosEye is missing; the eye falls back to a dark disc."); VfxMaterial.Solid(r0, new Color(0.05f, 0.01f, 0.09f), 0f); }
            r0.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r0.receiveShadows = false;
            _eye = eye.transform;
            GameObject Accretion(string name, int seed, Color colour, out Material ink)
            {
                var g = new GameObject(name);
                g.transform.SetParent(transform, false); g.transform.localPosition = Vector3.up * EyeHeight;
                var m = VfxShapes.TwoSided(VfxShapes.Hollow(72, 0.955f, 0.0f, seed));
                g.AddComponent<MeshFilter>().sharedMesh = m; g.AddComponent<MeshRenderer>();
                VfxShapes.Own(g, m);
                VfxMaterial.Ghost(g.GetComponent<Renderer>(), colour, 2.0f);
                ink = g.GetComponent<Renderer>().sharedMaterial;
                return g;
            }
            _accretionA = Accretion("OmenAccretionA", 21, new Color(Magenta.r, Magenta.g, Magenta.b, 0.85f), out _accretionInkA).transform;
            _accretionB = Accretion("OmenAccretionB", 22, new Color(0.70f, 0.46f, 1.0f, 0.6f), out _accretionInkB).transform;
            // Row 6: the shockwave and the emblem flash.
            var shock = VfxShapes.Lay(transform, "OmenShock", VfxShapes.Hollow(48, 0.86f, 0.1f, 4), 1f, 0.04f);
            VfxMaterial.Ghost(shock.GetComponent<Renderer>(), new Color(0.80f, 0.36f, 1.0f, 0f), 1.6f);
            _shock = shock.transform; _shockInk = shock.GetComponent<Renderer>().sharedMaterial;
            var flash = PhaisterProp.Spawn("butterfly", transform, null, PhaisterProp.InsectOutlineWidth);
            if (flash != null)
            {
                flash.transform.localPosition = Vector3.up * 0.05f;
                flash.transform.localScale = new Vector3(13f, 0.05f, 13f);
                Material shared = null;
                foreach (var r in flash.GetComponentsInChildren<Renderer>())
                {
                    VfxMaterial.Ghost(r, new Color(Magenta.r, Magenta.g, Magenta.b, 0f), 1.8f);
                    shared ??= r.sharedMaterial;
                }
                _flash = flash.transform; _flashInk = shared;
            }
            // Row 7 and 3: the butterflies.
            foreach (var row in Orbit)
            {
                var b = PhaisterProp.Spawn("butterfly", transform, null, PhaisterProp.InsectOutlineWidth);
                if (b == null) continue;
                _flies.Add((b.transform, new[] { PhaisterProp.Find(b, "wing-l"), PhaisterProp.Find(b, "wing-r") }));
            }
            // Row 2: ribbons.
            foreach (var rb in Ribbons)
            {
                var r = GameObject.CreatePrimitive(PrimitiveType.Cube);
                r.name = "OmenRibbon"; VfxMaterial.StripCollider(r);
                VfxMaterial.Solid(r.GetComponent<Renderer>(), RibbonColours[rb.Colour], rb.Colour == 2 ? 0.1f : 0f);
                r.transform.SetParent(transform, false);
                _ribbons.Add(r.transform);
            }
            // Row 8: streaks.
            foreach (var st in Streaks)
            {
                var s = VfxShapes.Lay(transform, "OmenStreak", VfxShapes.Streak(0.62f, 12, 5), 1f, 0.03f);
                VfxMaterial.Ghost(s.GetComponent<Renderer>(), new Color(0.70f, 0.40f, 1.0f, 0f), 1.0f);
                _streaks.Add((s.transform, s.GetComponent<Renderer>().sharedMaterial));
            }
        }

        private void Update() { StepTo(_t + Time.deltaTime); if (_t >= LifeSeconds) Destroy(gameObject); }

        private static void Alpha(Material m, float a) => PhaisterProp.SetAlpha(m, a);

        public void StepTo(float seconds)
        {
            _t = seconds;
            float R = VoodooRules.HigopRadius;
            float cast = Mathf.Clamp01(seconds / Mathf.Max(0.01f, _cast));
            float pullT = seconds - _cast;                             // < 0 while casting
            float endT = seconds - _cast - _pull;                      // >= 0 at the end
            bool pulling = pullT >= 0f && endT < 0f, ending = endT >= 0f;
            float fadeOut = ending ? Mathf.Clamp01(1f - endT / 0.4f) : 1f;

            // Row 1: the sigils write themselves clockwise through the cast; the ring turns slowly once it is whole.
            foreach (var (t, m, at) in _sigils)
            {
                float u = (cast - at) / 0.12f;
                t.gameObject.SetActive(u > 0f && fadeOut > 0f);
                Alpha(m, Mathf.Clamp01(u) * 0.95f * fadeOut);
            }
            _ring.gameObject.SetActive(fadeOut > 0f);
            Alpha(_ringInk, Mathf.Clamp01(cast * 1.5f) * 0.8f * fadeOut);
            transform.rotation = Quaternion.Euler(0f, pullT > 0f ? -6f * pullT : 0f, 0f);

            // Row 14: the dim.
            Alpha(_dimInk, (pulling ? Mathf.Clamp01(pullT / 0.4f) : 0f) * 0.30f * fadeOut + (ending ? 0f : 0f));

            // Row 4 and 5. ⚠️ v4 (owner: *"glitchy and unstable as fuck when forming (make it pulsate?) it starts out small and
            // gradually gets bigger"*): the eye is there from the first moment of the cast, SMALL, pulsing hard on two beats at
            // once and glitching (`CosmosEye._Glitch`), growing to half size over the 2.2 s; when the pull begins it snaps open
            // with an overshoot and steadies (the glitch dies over 0.3 s); it breathes while open and shuts to a point at the end.
            float glitch = 0f;
            float open;
            if (pullT < 0f)
            {
                float grow = Mathf.Pow(cast, 1.4f);
                float pulse = 1f + 0.22f * Mathf.Sin(seconds * 23f) + 0.12f * Mathf.Sin(seconds * 37f + 1.3f);
                // Continues from the cutscene's end state (it ends on the eye thrown, glitchy, about half grown): never shown twice.
                open = Mathf.Lerp(0.40f, 0.60f, grow) * pulse;
                glitch = 1f - 0.35f * grow;
            }
            else open = pullT < 0.18f ? Mathf.Lerp(0.52f, 1.12f, pullT / 0.18f) : pullT < 0.34f ? Mathf.Lerp(1.12f, 1f, (pullT - 0.18f) / 0.16f) : 1f;
            if (pullT >= 0f) glitch = Mathf.Clamp01(1f - pullT / 0.3f) * 0.6f;
            if (ending) open = endT < 0.10f ? Mathf.Lerp(1f, 1.2f, endT / 0.10f) : endT < 0.25f ? Mathf.Lerp(1.2f, 0f, (endT - 0.10f) / 0.15f) : 0f;
            float breathe = 1f + 0.035f * Mathf.Sin(seconds * Mathf.PI * 1.6f);
            _eye.localScale = Vector3.one * (EyeSize / CosmosOpen) * Mathf.Max(0.001f, open) * breathe;
            _eye.gameObject.SetActive(open > 0.01f);
            if (_cosmos != null) { _cosmos.SetFloat("_Open", CosmosOpen); _cosmos.SetFloat("_Time0", seconds); _cosmos.SetFloat("_Glitch", ending ? 0.5f : glitch); }
            // Two thin rings turning CLOCKWISE round the eye at different tilts, the accretion of everything it is drinking.
            float ringR = EyeSize * 0.5f * 1.28f * open;
            _accretionA.localScale = new Vector3(ringR, 1f, ringR);
            _accretionB.localScale = new Vector3(ringR * 1.16f, 1f, ringR * 1.16f);
            _accretionA.localRotation = Quaternion.Euler(72f, 0f, 0f) * Quaternion.Euler(0f, -90f * seconds, 0f);
            _accretionB.localRotation = Quaternion.Euler(-62f, 35f, 0f) * Quaternion.Euler(0f, -60f * seconds, 0f);
            // The rings only form once it is open: while it is unstable there is nothing steady to orbit.
            _accretionA.gameObject.SetActive(open > 0.01f && pullT > 0.05f);
            _accretionB.gameObject.SetActive(open > 0.01f && pullT > 0.12f);

            // Row 6: the landing, one shockwave out to the boundary and the emblem flash.
            float land = pullT;
            _shock.gameObject.SetActive(land >= 0f && land < 0.6f);
            if (land >= 0f)
            {
                float r = Mathf.Lerp(0.5f, R, 1f - Mathf.Pow(1f - Mathf.Clamp01(land / 0.25f), 2f));
                _shock.localScale = new Vector3(r, 1f, r);
                Alpha(_shockInk, 0.85f * Mathf.Clamp01(1f - land / 0.6f));
            }
            if (_flash != null)
            {
                _flash.gameObject.SetActive(land >= 0f && land < 0.45f);
                Alpha(_flashInk, 0.8f * Mathf.Clamp01(1f - land / 0.45f));
            }

            // Row 8: streaks sliding in along the court.
            for (int i = 0; i < _streaks.Count; i++)
            {
                var (st, sm) = _streaks[i];
                var row = Streaks[i];
                st.gameObject.SetActive(pulling);
                if (!pulling) continue;
                float u = Mathf.Repeat(pullT * 3f / R + row.Phase, 1f);
                float r = Mathf.Lerp(R, 0.8f, u);
                st.localPosition = Quaternion.Euler(0f, row.A, 0f) * new Vector3(0f, 0.035f, r);
                st.localRotation = Quaternion.Euler(0f, row.A, 0f);
                st.localScale = new Vector3(0.08f, 1f, row.L);
                Alpha(sm, 0.6f * Mathf.Sin(u * Mathf.PI));
            }

            // Row 2: ribbons whip upward off her through the cast.
            Vector3 herAt = _her != null ? _her.position : transform.position;
            for (int i = 0; i < _ribbons.Count; i++)
            {
                var rb = Ribbons[i];
                bool on = pullT < 0.2f && seconds > 0.15f && _her != null;
                _ribbons[i].gameObject.SetActive(on);
                if (!on) continue;
                float surge = Mathf.Clamp01((seconds - 0.15f) / 0.5f);
                float wave = Mathf.Sin(seconds * 9f + i * 1.7f);
                float lift = Mathf.Clamp01(_her.position.y - Slipper.GroundY(_her.position) + 0.2f);
                var dir = Quaternion.Euler(0f, rb.A + _her.eulerAngles.y, 0f) * Vector3.forward;
                Vector3 root = herAt + Vector3.up * rb.Y + dir * (rb.Colour == 2 ? 0.30f : 0.36f);
                Vector3 up = (Vector3.up * (0.6f + 0.4f * surge) + dir * (0.35f + 0.1f * wave)).normalized;
                float len = rb.L * surge * (0.8f + 0.2f * lift);
                _ribbons[i].position = root + up * len * 0.5f;
                _ribbons[i].rotation = Quaternion.LookRotation(up, dir) * Quaternion.Euler(0f, 0f, 20f * wave);
                _ribbons[i].localScale = new Vector3(0.12f, 0.02f, Mathf.Max(0.01f, len));
            }

            // Row 3, 7 and 11: the butterflies.
            for (int i = 0; i < _flies.Count; i++)
            {
                var (b, w) = _flies[i];
                var row = Orbit[i];
                Vector3 p; Vector3 heading; float scale = row.Size;
                float stagger = (i % 12) / 12f * 0.9f;
                if (pullT < 0f)
                {
                    // Row 3: out of her sleeves and hat, in a low arc to the spot, arriving on their own orbit slot.
                    float u = Mathf.Clamp01((seconds - 0.5f - stagger) / 0.9f);
                    Vector3 src = herAt + Vector3.up * (i % 2 == 0 ? 0.75f : 1.55f);
                    float a0 = row.Phase * Mathf.Deg2Rad;
                    Vector3 slot = transform.position + new Vector3(Mathf.Sin(a0), 0f, Mathf.Cos(a0)) * row.R + Vector3.up * row.Y;
                    p = Vector3.Lerp(src, slot, u * u * (3f - 2f * u)) + Vector3.up * Mathf.Sin(u * Mathf.PI) * 0.8f;
                    heading = slot - src;
                    scale *= Mathf.Clamp01(u * 4f);
                }
                else if (!ending)
                {
                    // Row 7: clockwise, faster toward the middle, spiralling in; at the core it dives and comes round from the rim.
                    float r = Mathf.Repeat(row.R - 1.4f - row.In * pullT, R - 1.4f) + 1.4f;
                    float omega = 2.8f / Mathf.Max(0.9f, r) * row.K;             // turns a second
                    float a = (row.Phase - 360f * omega * pullT) * Mathf.Deg2Rad;
                    float lift = EyeHeight - VoodooRules.HigopMinHeight;
                    float y = Mathf.Lerp(EyeHeight, row.Y + lift, Mathf.Clamp01((r - 0.7f) / 3f));
                    p = transform.position + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * r + Vector3.up * y;
                    heading = new Vector3(-Mathf.Cos(a), 0f, Mathf.Sin(a));
                    scale *= Mathf.Clamp01((r - 1.4f) / 0.6f);
                }
                else
                {
                    // Row 11: everything bursts UP at once; three stay (rows 0, 11, 23) and flutter off late.
                    bool late = i == 0 || i == 11 || i == 23;
                    float r = Mathf.Repeat(row.R - 1.4f - row.In * _pull, R - 1.4f) + 1.4f;
                    float a = (row.Phase - 360f * (2.8f / Mathf.Max(0.9f, r) * row.K) * _pull) * Mathf.Deg2Rad;
                    Vector3 from = transform.position + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * r + Vector3.up * (row.Y + EyeHeight - VoodooRules.HigopMinHeight);
                    float u = late ? Mathf.Clamp01((endT - 0.4f) / 0.8f) : Mathf.Clamp01(endT / 0.6f);
                    Vector3 outward = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                    p = from + (Vector3.up * (late ? 3f : 9f) + outward * (late ? 2f : 3f)) * (u * (2f - u));
                    heading = Vector3.up + outward;
                    scale *= late ? 1f : Mathf.Clamp01(1f - (u - 0.6f) / 0.4f);
                }
                b.position = p;
                if (heading.sqrMagnitude > 0.0001f) b.rotation = Quaternion.LookRotation(heading.normalized, Vector3.up);
                b.localScale = Vector3.one * Mathf.Max(0.0001f, scale * FlyScale);
                b.gameObject.SetActive(scale > 0.01f);
                float wingOpen = 10f + 60f * (0.5f + 0.5f * Mathf.Sin(seconds * row.Hz * Mathf.PI * 2f + row.Phase));
                if (w[0] != null) w[0].localRotation = Quaternion.AngleAxis(wingOpen, Vector3.forward);
                if (w[1] != null) w[1].localRotation = Quaternion.AngleAxis(-wingOpen, Vector3.forward);
            }
        }
    }
}
