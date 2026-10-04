using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// A pavement jump pad: it throws whoever steps on it straight up, high enough to see over
    /// the LRT deck (owner, 2026-10-04: "can you add jump pads on the sidewalk so we can jump up to
    /// see the train"). The deck hides the consist from everywhere a player can stand, so this is
    /// the one way to watch it pass.
    ///
    /// Each peer launches only the motors it simulates (`CharacterMotor.LaunchUp` refuses the
    /// rest), the same ownership rule as every other movement.
    ///
    /// THE LOOK is the owner's sketch (2026-10-04: "can you fix up a better model for the jump
    /// pads something like this with animations"): a square pad inside a white outline, white
    /// outline frames that lift off it, and two chevrons climbing over its middle. It was first
    /// built here from a few unlit quads, which the owner rejected (2026-10-04: "it looks flat,
    /// doesnt have shading, texture or any of that stylized character to it"). It is now a
    /// modelled, painted PROP (tools/author_jump_pad.py, its atlas tools/author_jump_pad_textures.py)
    /// loaded from Resources, lit by the street's own shader, and animated here. The scene still
    /// carries nothing but this component.
    ///
    /// ⚠️ IT GLOWS (owner, 2026-10-04: "jump pad needs to be glowy btw"), four ways, because no
    /// one of them reads everywhere: the painted parts are EMISSIVE in their own colours (so the
    /// drawing is its own mask: cream chevrons and amber burn, charcoal rubber stays matte); the
    /// floating chevrons and frames are light, brightest as they leave the pad; a small warm
    /// POINT LIGHT lights the pavement and a player's feet in the bridge's shade; and a soft
    /// HALO on the ground carries it in daylight, where a point light barely shows. `ColourGrade`
    /// blooms only what passes 2.2 (its knee opens at 1.76, at 0.05 strength, and not at all on
    /// the Low tier), so the idle glow sits under that and only a lift-off and a launch cross it.
    ///
    /// ⚠️ IF THE PROP IS MISSING (no .glb, no texture) the old quads are built instead and one
    /// warning is logged; if only the painted shader is missing the prop wears `Standard`.
    /// </summary>
    public sealed class JumpPad : MonoBehaviour
    {
        /// <summary>Half the plate's side, metres: standing inside the square is standing on it.</summary>
        public float Radius = 0.85f;

        /// <summary>Upward speed in m/s. At `Balance.Gravity` 20 the apex is v*v/40: 24 gives
        /// 14.4 m, above the consist's roof at about 12.7 m.</summary>
        public float LaunchSpeed = 24.0f;

        private CharacterMotor[] _motors = System.Array.Empty<CharacterMotor>();
        private float _rescan;
        private float _kick;        // 1 on a launch, eased to 0: the pad's answer to throwing somebody
        private float _phase;       // the loop's own clock, which a launch hurries
        private float _sinceLaunch = 99.0f;   // seconds since the last throw: the cushion's spring runs on it

        // ⚠️ AMBER, NOT THE SKETCH'S ORANGE. The sketch's plate is close to the attackers' role hue
        // (#f87020), and nothing on a map may read as a team colour. Amber keeps the warmth and
        // the chevrons go a lighter yellow so they still separate from the plate. The light and
        // the halo are the same gold; nothing here is near the taya's #0080e8 either.
        private static readonly Color Plate = new Color(0.96f, 0.66f, 0.05f);
        private static readonly Color Chevron = new Color(1.0f, 0.86f, 0.22f);
        private static readonly Color Line = new Color(1.0f, 0.99f, 0.95f);
        private static readonly Color Glow = new Color(1.0f, 0.78f, 0.34f);

        private const string ModelPath = "Map/JumpPad/jump_pad";
        private const string PaintPath = "Map/JumpPad/jump_pad_paint";
        private const string PaintedShader = "TumbangPreso/IlalimPainted";
        /// <summary>How solid a rising ring is at its most solid: see-through, so the street shows behind it.</summary>
        private const float RingAlpha = 0.62f;
        private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");
        private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
        private bool _ghostFades;

        // The prop's own measures (tools/author_jump_pad.py): it is modelled 1.70 m square, the
        // cushion sits on the well floor, and a frame starts where the painted outline is.
        private const float ModelHalf = 0.85f, CushionSeat = 0.02f, FrameStart = 0.09f;

        private const int Ghosts = 3;
        private const float GhostRise = 1.5f, LineWidth = 0.07f;
        private const float ChevronLow = 0.45f, ChevronClimb = 0.9f;

        // Emission, as multiples of the painted colour, in linear light. See the summary for why
        // the idle values stay low and the launch ones do not.
        private const float BaseGlow = 0.22f, CushionGlow = 0.55f, CushionFlash = 3.0f;
        private const float FloatBright = 1.6f, GhostDim = 0.35f, ChevronDim = 0.6f, FloatFlash = 1.6f;

        // The pad's light, by the numbers IlalimSceneBuilder.PointLight gives the map's other small
        // lights (the overclock pad 3.5 m at 1.5, the food warmer 3.8 m at 1.0): no shadows.
        private const float LightRange = 3.6f, LightIdle = 1.25f, LightSwing = 0.3f, LightFlash = 2.4f;
        private const float HaloReach = 1.15f, HaloHeight = 0.02f, HaloIdle = 0.34f, HaloFlash = 0.5f;

        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static bool _warned;

        private bool _modelled;
        private Transform _look, _cushion, _halo;
        private Material _baseMaterial, _cushionMaterial, _haloMaterial;
        private Light _light;
        private Mesh _haloMesh;

        // The flat fallback's parts, and (shared by both looks) the floating frames and chevrons.
        private Material _plateMaterial, _lineMaterial;
        private Transform _plate;
        private readonly Transform[] _ghost = new Transform[Ghosts];
        private readonly Material[] _ghostMaterial = new Material[Ghosts];
        private readonly Transform[] _chevron = new Transform[2];
        private readonly Material[] _chevronMaterial = new Material[2];
        private Mesh _plateMesh, _frameMesh, _chevronMesh;
        /// <summary>Every renderer of the look, for `Seen`.</summary>
        private Renderer[] _drawn = System.Array.Empty<Renderer>();

        private void Start()
        {
            // Everything hangs off one root scaled to `Radius`, so a pad made wider in the
            // Inspector still fills its own trigger square.
            _look = new GameObject("Look").transform;
            _look.SetParent(transform, false);
            float fit = Radius / ModelHalf;
            _look.localScale = new Vector3(fit, fit, fit);

            _modelled = BuildModel();
            if (!_modelled) BuildFlat();
            BuildGlow();
            _drawn = _look.GetComponentsInChildren<Renderer>(true);
        }

        /// <summary>
        /// True when a camera drew any part of the pad last frame. ⚠️ A PAD NOBODY IS LOOKING AT
        /// IS NOT ANIMATED (owner, 2026-10-04: the joining players' frame rate, "add these
        /// optimization fixes"): the loop writes a dozen transforms, seven materials and a light
        /// every frame on each of the map's four pads. The launch, the loop's clock and the kick
        /// run on regardless, so a pad that comes into view is mid-beat, not restarting.
        /// </summary>
        private bool Seen()
        {
            foreach (var r in _drawn)
                if (r != null && r.isVisible) return true;
            return false;
        }

        // ------------------------------------------------------------------ the modelled prop

        /// <summary>
        /// The four meshes of jump_pad.glb (pad_base, pad_cushion, pad_chevron, pad_ring), each on a
        /// runtime material of the street's painted shader reading the one atlas. Only the MESHES
        /// are taken: the kit is exported unrotated and unscaled, so they carry the whole shape,
        /// and the parts are placed here. False if the prop cannot be built.
        /// </summary>
        private bool BuildModel()
        {
            var prefab = Resources.Load<GameObject>(ModelPath);
            var paint = Resources.Load<Texture2D>(PaintPath);
            Mesh baseMesh = null, cushionMesh = null, chevronMesh = null, ringMesh = null;
            if (prefab != null)
            {
                foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter.sharedMesh == null) continue;
                    if (Named(filter, "pad_base")) baseMesh = filter.sharedMesh;
                    else if (Named(filter, "pad_cushion")) cushionMesh = filter.sharedMesh;
                    else if (Named(filter, "pad_chevron")) chevronMesh = filter.sharedMesh;
                    else if (Named(filter, "pad_ring")) ringMesh = filter.sharedMesh;
                }
            }
            if (paint == null || baseMesh == null || cushionMesh == null || chevronMesh == null || ringMesh == null)
            {
                Warn("[JumpPad] The prop is incomplete (Resources/" + ModelPath + " or " + PaintPath +
                     "); drawing the flat pad instead.");
                return false;
            }

            var shader = Shader.Find(PaintedShader);
            bool painted = shader != null;
            if (!painted)
            {
                Warn("[JumpPad] Shader " + PaintedShader + " is missing; the pad wears Standard.");
                shader = Shader.Find("Standard");
                if (shader == null) return false;
            }

            _baseMaterial = Painted(shader, paint, painted, BaseGlow);
            Part("Base", baseMesh, _baseMaterial, true);
            _cushionMaterial = Painted(shader, paint, painted, CushionGlow);
            _cushion = Part("Cushion", cushionMesh, _cushionMaterial, true);
            _cushion.localPosition = new Vector3(0.0f, CushionSeat, 0.0f);
            for (int i = 0; i < Ghosts; i++)
            {
                // ⚠️ THE RINGS ARE SEE-THROUGH AND FADE OUT (owner, 2026-10-04, first look in game:
                // "can the white stuff be semi-transparent, fading out at the end"). The painted
                // shader is opaque, so they used to shrink to nothing instead. They wear the
                // game's own blended toon shader now: still lit and banded like everything else,
                // with the alpha in `_Color` and the lift-off glow in its flash.
                var fade = Shader.Find("TumbangPreso/ToonTransparent");
                _ghostFades = fade != null;
                if (_ghostFades)
                {
                    _ghostMaterial[i] = new Material(fade) { mainTexture = paint };
                    _ghostMaterial[i].SetColor(FlashColorId, new Color(1.0f, 0.96f, 0.82f, 1.0f));
                }
                else _ghostMaterial[i] = Painted(shader, paint, painted, GhostDim);
                _ghost[i] = Part("Rising frame " + i, ringMesh, _ghostMaterial[i], false);
            }
            for (int i = 0; i < _chevron.Length; i++)
            {
                _chevronMaterial[i] = Painted(shader, paint, painted, ChevronDim);
                _chevron[i] = Part("Chevron " + i, chevronMesh, _chevronMaterial[i], false);
            }
            return true;
        }

        private static bool Named(MeshFilter filter, string key)
        {
            return filter.name.StartsWith(key, System.StringComparison.Ordinal) ||
                   filter.sharedMesh.name.StartsWith(key, System.StringComparison.Ordinal);
        }

        private static void Warn(string message)
        {
            if (_warned) return;
            _warned = true;
            Debug.LogWarning(message);
        }

        /// <summary>
        /// A material on TumbangPreso/IlalimPainted, with every property the shader reads set as
        /// `IlalimSceneBuilder.Painted` sets it for a plain surface: the atlas on UV0 unscaled and
        /// unrotated, no anti-tiling resample, all three overlays off, no alpha cut, back faces
        /// culled, and the emission multiplied by the painted colour. On `Standard` (the shader
        /// fallback) the atlas is the emission map, which gives the same "glow in its own colour".
        /// </summary>
        private static Material Painted(Shader shader, Texture2D paint, bool painted, float glow)
        {
            var m = new Material(shader) { name = "JumpPad paint", mainTexture = paint };
            m.SetColor(ColorId, Color.white);
            m.SetFloat("_Glossiness", 0.1f);
            if (painted)
            {
                m.SetColor("_PostTint", Color.white);
                m.SetTextureScale("_MainTex", Vector2.one);
                m.SetTextureOffset("_MainTex", Vector2.zero);
                m.SetFloat("_MainRot", 0.0f);
                m.SetFloat("_Saturation", 1.0f);
                m.SetFloat("_BumpScale", 1.0f);
                m.SetFloat("_Cutoff", 0.0f);
                m.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Back);
                m.SetFloat("_EmissionFromAlbedo", 1.0f);
                m.SetFloat("_AntiTileCount", 0.0f);
                m.SetVector("_Ov0", new Vector4(1, 0, 0, 0));
                m.SetVector("_Ov1", new Vector4(2, 0, 0, 0));
                m.SetVector("_Ov2", new Vector4(3, 0, 0, 0));
            }
            else
            {
                m.SetFloat("_Metallic", 0.0f);
                m.SetTexture("_EmissionMap", paint);
                m.EnableKeyword("_EMISSION");
            }
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            m.SetColor(EmissionId, Lit(glow));
            return m;
        }

        /// <summary>An emission of `linear` times the painted colour. The project is in linear
        /// colour space and `SetColor` decodes what it is given as sRGB, so the value is encoded
        /// first (the Kanto convention, as the layout's `emission` is).</summary>
        private static Color Lit(float linear)
        {
            float v = QualitySettings.activeColorSpace == ColorSpace.Linear ? Mathf.LinearToGammaSpace(linear) : linear;
            return new Color(v, v, v, 1.0f);
        }

        // ------------------------------------------------------------------ the flat fallback

        /// <summary>The first look, kept for a build that lost the prop: unlit quads.</summary>
        private void BuildFlat()
        {
            // Sprites/Default: unlit, alpha blended, two-sided, and always in a build.
            var shader = Shader.Find("Sprites/Default");
            _plateMesh = Quad(ModelHalf - LineWidth * 0.5f);
            _frameMesh = Frame(ModelHalf, LineWidth);
            _chevronMesh = ChevronMesh(0.34f, 0.17f, 0.085f);

            _plateMaterial = new Material(shader) { color = Plate };
            _plate = Part("Plate", _plateMesh, _plateMaterial, false);
            _plate.localPosition = new Vector3(0.0f, 0.02f, 0.0f);

            _lineMaterial = new Material(shader) { color = Line };
            Part("Outline", _frameMesh, _lineMaterial, false).localPosition = new Vector3(0.0f, 0.03f, 0.0f);

            for (int i = 0; i < Ghosts; i++)
            {
                _ghostMaterial[i] = new Material(shader) { color = Line };
                _ghost[i] = Part("Rising frame " + i, _frameMesh, _ghostMaterial[i], false);
            }
            for (int i = 0; i < _chevron.Length; i++)
            {
                _chevronMaterial[i] = new Material(shader) { color = Chevron };
                _chevron[i] = Part("Chevron " + i, _chevronMesh, _chevronMaterial[i], false);
            }
        }

        private Transform Part(string name, Mesh mesh, Material material, bool shadows)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_look, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            // The frame and the cushion are street furniture and shade like it; the floating
            // markers are light and cast nothing.
            renderer.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = shadows;
            return go.transform;
        }

        // ------------------------------------------------------------------ the glow

        /// <summary>
        /// The light and the ground halo (owner, 2026-10-04: "jump pad needs to be glowy btw").
        /// THE HALO is a flat skirt round the pad, 2 cm above the pavement so it never fights it,
        /// starting under the frame's chamfer and reaching `HaloReach` beyond the edge. It has no
        /// texture: its softness is VERTEX ALPHA falling off ring by ring, and its outline
        /// wanders (two slow waves) so it reads as a painted wash, not a ruled disc.
        /// </summary>
        private void BuildGlow()
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                _haloMesh = HaloMesh(ModelHalf - 0.04f, 0.16f, HaloReach);
                _haloMaterial = new Material(shader) { name = "JumpPad halo", color = Glow };
                _halo = Part("Halo", _haloMesh, _haloMaterial, false);
                _halo.localPosition = new Vector3(0.0f, HaloHeight, 0.0f);
            }

            var go = new GameObject("PadLight");
            go.transform.SetParent(_look, false);
            go.transform.localPosition = new Vector3(0.0f, 0.45f, 0.0f);
            _light = go.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.color = Glow;
            _light.range = LightRange;
            _light.intensity = LightIdle;
            _light.shadows = LightShadows.None;
        }

        private void OnDestroy()
        {
            if (_baseMaterial != null) Destroy(_baseMaterial);
            if (_cushionMaterial != null) Destroy(_cushionMaterial);
            if (_haloMaterial != null) Destroy(_haloMaterial);
            if (_plateMaterial != null) Destroy(_plateMaterial);
            if (_lineMaterial != null) Destroy(_lineMaterial);
            foreach (var m in _ghostMaterial) if (m != null) Destroy(m);
            foreach (var m in _chevronMaterial) if (m != null) Destroy(m);
            if (_haloMesh != null) Destroy(_haloMesh);
            if (_plateMesh != null) Destroy(_plateMesh);
            if (_frameMesh != null) Destroy(_frameMesh);
            if (_chevronMesh != null) Destroy(_chevronMesh);
        }

        private void Update()
        {
            _rescan -= Time.deltaTime;
            if (_rescan <= 0.0f)
            {
                _rescan = 1.0f;
                _motors = FindObjectsByType<CharacterMotor>();
            }

            Vector3 here = transform.position;
            foreach (var motor in _motors)
            {
                if (motor == null || !motor.IsGrounded) continue;
                Vector3 d = motor.transform.position - here;
                if (Mathf.Abs(d.y) > 0.8f || Mathf.Abs(d.x) > Radius || Mathf.Abs(d.z) > Radius) continue;
                if (motor.LaunchUp(LaunchSpeed)) { _kick = 1.0f; _sinceLaunch = 0.0f; }
            }

            if (_look == null) return;
            _kick = Mathf.MoveTowards(_kick, 0.0f, Time.deltaTime * 1.6f);
            _phase += Time.deltaTime * (0.55f + 2.6f * _kick);
            _sinceLaunch += Time.deltaTime;
            if (!Seen()) return;
            if (_modelled) Animate(); else AnimateFlat();
            AnimateGlow();
        }

        // ------------------------------------------------------------------ the curves
        // Nothing here moves on a straight ramp (the owner has turned down animation on this
        // project as too linear and too unlively): things leave with a pop, overshoot, and settle.

        /// <summary>0 to 1, shooting about 10 per cent past 1 on the way and settling back.</summary>
        private static float OutBack(float t)
        {
            t = Mathf.Clamp01(t) - 1.0f;
            return 1.0f + 2.70158f * t * t * t + 1.70158f * t * t;
        }

        /// <summary>0 to 1, first dipping about 10 per cent below 0: a wind-up before it goes.</summary>
        private static float InBack(float t)
        {
            t = Mathf.Clamp01(t);
            return 2.70158f * t * t * t - 1.70158f * t * t;
        }

        private static float OutCubic(float t)
        {
            t = 1.0f - Mathf.Clamp01(t);
            return 1.0f - t * t * t;
        }

        /// <summary>
        /// The cushion's height scale after a throw: slammed to 0.35 in 60 ms, then a damped
        /// spring back that overshoots to about 1.23 at 0.19 s and has settled by 0.7 s.
        /// </summary>
        private static float Spring(float since)
        {
            if (since < 0.06f) return Mathf.Lerp(1.0f, 0.35f, OutCubic(since / 0.06f));
            float t = since - 0.06f;
            if (t > 1.2f) return 1.0f;
            return 1.0f - 0.65f * Mathf.Exp(-8.0f * t) * Mathf.Cos(24.0f * t);
        }

        /// <summary>
        /// The loop. THE CUSHION breathes: a small swell each time a frame leaves it, and on a
        /// throw it is squashed flat and springs up past rest (`Spring`), bulging as it flattens.
        /// EACH FRAME starts as the painted outline itself, swells out of it, rises `GhostRise`
        /// (quick off the pad, slowing) and widens; over its last third it puffs a touch and then
        /// SHRINKS TO NOTHING, in plan and in thickness together. The painted shader is opaque and
        /// cannot fade, and a frame that vanished at full size would pop. THE TWO CHEVRONS climb
        /// half a beat apart, turned about the vertical to face the camera: each pops in past
        /// full size, is stretched tall while it is moving fast, overshoots its climb and winds
        /// up before it pops out. A throw hurries the whole loop (`_phase`) and flings every
        /// floating part higher for a moment.
        /// </summary>
        private void Animate()
        {
            float burst = _kick * _kick;

            float beat = Mathf.Repeat(_phase * Ghosts, 1.0f);
            float breath = 1.0f + 0.05f * (1.0f - beat) * (1.0f - beat) * Mathf.Sin(Mathf.PI * Mathf.Min(1.0f, beat * 3.0f));
            float tall = breath * Spring(_sinceLaunch);
            float wide = Mathf.Clamp(1.0f + (1.0f - tall) * 0.025f, 0.99f, 1.016f);   // the well is 1 cm away
            _cushion.localScale = new Vector3(wide, tall, wide);

            for (int i = 0; i < Ghosts; i++)
            {
                float u = Mathf.Repeat(_phase + i / (float)Ghosts, 1.0f);
                float outQuad = 1.0f - (1.0f - u) * (1.0f - u);
                float rise = Mathf.Lerp(u, outQuad, 0.6f);
                float emerge = OutBack(u / 0.15f);
                float shrink = Mathf.Max(0.0f, 1.0f - InBack((u - 0.68f) / 0.32f));
                float plan = (1.0f + 0.10f * u + 0.25f * burst * u) * shrink;
                float thick = Mathf.Max(0.0f, (0.15f + 0.85f * emerge) * shrink);
                _ghost[i].localPosition = new Vector3(0.0f, FrameStart + GhostRise * rise * (1.0f + 0.6f * burst), 0.0f);
                if (_ghostFades)
                {
                    // Full size all the way up: it thins into the air instead of shrinking. In from
                    // nothing over the first tenth, RingAlpha through the middle, gone by the top.
                    float grow = 1.0f + 0.10f * u + 0.25f * burst * u;
                    _ghost[i].localScale = new Vector3(grow, Mathf.Max(0.0f, 0.15f + 0.85f * emerge), grow);
                    float away = Mathf.Clamp01((u - 0.35f) / 0.65f);
                    float alpha = RingAlpha * Mathf.Clamp01(u / 0.10f) * (1.0f - away * away * (3.0f - 2.0f * away));
                    _ghostMaterial[i].SetColor(ColorId, new Color(1.0f, 1.0f, 1.0f, Mathf.Clamp01(alpha + 0.25f * burst * (1.0f - away))));
                    // The glow: flashed toward warm white as it leaves the pad, dimming as it rises.
                    _ghostMaterial[i].SetFloat(FlashAmountId, Mathf.Clamp01(0.55f * (1.0f - u) * (1.0f - u) + 0.4f * burst));
                    continue;
                }
                _ghost[i].localScale = new Vector3(plan, thick, plan);
                // Light: brightest as it leaves the pad, dimming as it rises.
                _ghostMaterial[i].SetColor(EmissionId, Lit(GhostDim + (FloatBright - GhostDim) * (1.0f - u) * (1.0f - u) + FloatFlash * burst));
            }

            float yaw = CameraYaw();
            for (int i = 0; i < _chevron.Length; i++)
            {
                float u = Mathf.Repeat(_phase * 1.5f + i * 0.5f, 1.0f);
                float a = Mathf.Min(1.0f, u / 0.7f);
                float climb = Mathf.Lerp(u, OutBack(a), 0.5f);
                float size = Mathf.Max(0.0f, OutBack(u / 0.22f) * (1.0f - InBack((u - 0.78f) / 0.22f)));
                float hurry = (1.0f - a) * (1.0f - a);
                _chevron[i].localPosition = new Vector3(0.0f, ChevronLow + ChevronClimb * climb * (1.0f + 0.6f * burst), 0.0f);
                _chevron[i].localRotation = Quaternion.Euler(0.0f, yaw, 0.0f);
                _chevron[i].localScale = new Vector3(size * (1.0f - 0.12f * hurry), size * (1.0f + 0.25f * hurry), size);
                _chevronMaterial[i].SetColor(EmissionId, Lit(ChevronDim + (FloatBright - ChevronDim) * (1.0f - u) * (1.0f - u) + FloatFlash * burst));
            }
        }

        /// <summary>
        /// The glow's own loop, for both looks. Idle: the cushion, the light and the halo swell
        /// together on the same beat a frame leaves on, a quick rise and a slow fall. A throw
        /// flashes all three (the cushion far past the bloom threshold) and pops the halo wide,
        /// and they ease back as `_kick` does, squared so the flash is sharp and its tail long.
        /// </summary>
        private void AnimateGlow()
        {
            float burst = _kick * _kick;
            float beat = Mathf.Repeat(_phase * Ghosts, 1.0f);
            float pulse = (1.0f - beat) * (1.0f - beat) * OutCubic(beat * 5.0f);   // 0..1, peaking early in the beat

            if (_cushionMaterial != null)
                _cushionMaterial.SetColor(EmissionId, Lit(CushionGlow * (0.75f + 0.6f * pulse) + CushionFlash * burst));
            if (_baseMaterial != null)
                _baseMaterial.SetColor(EmissionId, Lit(BaseGlow * (0.8f + 0.4f * pulse) + 0.5f * burst));
            if (_light != null)
                _light.intensity = LightIdle + LightSwing * (pulse * 2.0f - 0.6f) + LightFlash * burst;
            if (_halo != null)
            {
                float pop = 1.0f + 0.05f * pulse + 0.30f * burst;
                _halo.localScale = new Vector3(pop, 1.0f, pop);
                var c = Glow; c.a = Mathf.Min(1.0f, HaloIdle * (0.8f + 0.35f * pulse) + HaloFlash * burst);
                _haloMaterial.color = c;
            }
        }

        /// <summary>The turn about the vertical, in the pad's own frame, that faces the camera.</summary>
        private float CameraYaw()
        {
            var eye = UnityEngine.Camera.main;
            if (eye == null) return 0.0f;
            Vector3 to = eye.transform.position - transform.position;
            return to.x * to.x + to.z * to.z > 1e-4f ? Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg - transform.eulerAngles.y : 0.0f;
        }

        /// <summary>
        /// The flat fallback's loop: each white frame lifts off the plate, widens a little and fades
        /// out by `GhostRise`, one after another; the two chevrons climb over the middle, half a beat
        /// apart, turned to face the camera. A launch hurries the whole loop and flashes the plate.
        /// </summary>
        private void AnimateFlat()
        {
            _plateMaterial.color = Color.Lerp(Plate * (0.9f + 0.1f * Mathf.Sin(_phase * 6.0f)), Color.white, _kick * 0.8f);

            for (int i = 0; i < Ghosts; i++)
            {
                float u = Mathf.Repeat(_phase + i / (float)Ghosts, 1.0f);
                float grow = 1.0f + 0.10f * u + 0.25f * _kick * u;
                _ghost[i].localPosition = new Vector3(0.0f, 0.04f + GhostRise * u * (1.0f + _kick), 0.0f);
                _ghost[i].localScale = new Vector3(grow, 1.0f, grow);
                var c = Line; c.a = 0.75f * (1.0f - u) * (1.0f - u);
                _ghostMaterial[i].color = c;
            }

            float yaw = CameraYaw();
            for (int i = 0; i < _chevron.Length; i++)
            {
                float u = Mathf.Repeat(_phase * 1.5f + i * 0.5f, 1.0f);
                _chevron[i].localPosition = new Vector3(0.0f, 0.35f + 0.75f * u * (1.0f + _kick), 0.0f);
                _chevron[i].localRotation = Quaternion.Euler(0.0f, yaw, 0.0f);
                float size = 0.85f + 0.3f * Mathf.Sin(Mathf.PI * u);
                _chevron[i].localScale = new Vector3(size, size, size);
                var c = Chevron; c.a = Mathf.Sin(Mathf.PI * u);
                _chevronMaterial[i].color = c;
            }
        }

        // ------------------------------------------------------------------ the built meshes

        /// <summary>
        /// The halo: `Rings` rounded-square loops, the first hugging the pad (`half`, corner
        /// `corner`) at full alpha, each next one further out and fainter, the last `reach` away
        /// and clear. The outer loops wander by up to a fifth so the edge is not a ruled line.
        /// </summary>
        private static Mesh HaloMesh(float half, float corner, float reach)
        {
            const int Rings = 7, Around = 48;
            var vertices = new Vector3[Rings * Around];
            var colours = new Color[Rings * Around];
            var tris = new int[(Rings - 1) * Around * 6];
            float inner = half - corner;
            for (int r = 0; r < Rings; r++)
            {
                float t = r / (float)(Rings - 1);
                float fade = (1.0f - t) * (1.0f - t) * (1.0f - 0.35f * t);
                for (int k = 0; k < Around; k++)
                {
                    float angle = k / (float)Around * Mathf.PI * 2.0f;
                    float wander = 1.0f + 0.14f * Mathf.Sin(3.0f * angle + 1.3f) + 0.07f * Mathf.Sin(5.0f * angle + 0.4f);
                    // A point on the rounded square grown by the ring's reach: clamp a far point
                    // on this bearing to the inner square, then step out from there.
                    var far = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (half * 1.6f);
                    var core = new Vector2(Mathf.Clamp(far.x, -inner, inner), Mathf.Clamp(far.y, -inner, inner));
                    var p = core + (far - core).normalized * (corner + reach * t * wander);
                    vertices[r * Around + k] = new Vector3(p.x, 0.0f, p.y);
                    colours[r * Around + k] = new Color(1.0f, 1.0f, 1.0f, fade);
                }
            }
            int n = 0;
            for (int r = 0; r < Rings - 1; r++)
            {
                for (int k = 0; k < Around; k++)
                {
                    int a = r * Around + k, b = r * Around + (k + 1) % Around, c = b + Around, d = a + Around;
                    tris[n++] = a; tris[n++] = c; tris[n++] = b;
                    tris[n++] = a; tris[n++] = d; tris[n++] = c;
                }
            }
            var mesh = new Mesh { name = "JumpPad halo" };
            mesh.vertices = vertices;
            mesh.colors = colours;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>A flat square on the ground, side 2 * half.</summary>
        private static Mesh Quad(float half)
        {
            var mesh = new Mesh { name = "JumpPad plate" };
            mesh.vertices = new[] { new Vector3(-half, 0, -half), new Vector3(-half, 0, half), new Vector3(half, 0, half), new Vector3(half, 0, -half) };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>A flat square outline on the ground: outer side 2 * half, the line `width` wide.</summary>
        private static Mesh Frame(float half, float width)
        {
            float o = half, i = half - width;
            var mesh = new Mesh { name = "JumpPad frame" };
            mesh.vertices = new[]
            {
                new Vector3(-o, 0, -o), new Vector3(-o, 0, o), new Vector3(o, 0, o), new Vector3(o, 0, -o),
                new Vector3(-i, 0, -i), new Vector3(-i, 0, i), new Vector3(i, 0, i), new Vector3(i, 0, -i),
            };
            var tris = new int[24];
            for (int k = 0; k < 4; k++)
            {
                int a = k, b = (k + 1) % 4, c = 4 + (k + 1) % 4, d = 4 + k;
                tris[k * 6] = a; tris[k * 6 + 1] = b; tris[k * 6 + 2] = c;
                tris[k * 6 + 3] = a; tris[k * 6 + 4] = c; tris[k * 6 + 5] = d;
            }
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>A chunky "^" standing upright in its own XY plane: two arms of `thick`, meeting at the top.</summary>
        private static Mesh ChevronMesh(float halfWidth, float rise, float thick)
        {
            var mesh = new Mesh { name = "JumpPad chevron" };
            mesh.vertices = new[]
            {
                new Vector3(-halfWidth, 0, 0), new Vector3(0, rise, 0), new Vector3(0, rise + thick, 0), new Vector3(-halfWidth, thick, 0),
                new Vector3(halfWidth, 0, 0), new Vector3(halfWidth, thick, 0),
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3, 4, 2, 1, 4, 5, 2 };
            mesh.RecalculateBounds();
            return mesh;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Plate;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.05f, new Vector3(Radius * 2.0f, 0.1f, Radius * 2.0f));
        }
    }
}
