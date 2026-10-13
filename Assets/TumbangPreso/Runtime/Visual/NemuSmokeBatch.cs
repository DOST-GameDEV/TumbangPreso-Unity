using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ NEMU'S SMOKE AND LIGHT, LAID OUT BY HAND (LET THEM IN, 2026-10-13, `docs/reports/nemu-ultimate-2026-10-11/
    /// direction.md` section 11.5). Every frame the owner calls <see cref="Begin"/>, adds every puff and light it wants at
    /// this moment (posed from ITS clock: the scene clock in the cutscene, the status clock in play), then <see cref="End"/>.
    /// Two particle systems that never simulate (the way `VoodooSoulDraw` lays out its motes): one draw for all the smoke,
    /// one for all the light, both `Shaders/NemuSmoke`. Lights draw after smoke, so an eye is never put out by its own head.
    ///
    ///  * A PUFF darkens what is behind it and glows violet at its frayed rim. Its seed gives it its own rim; its spin
    ///    turns it so a column of them is never one stamp repeated.
    ///  * A WISP is a puff drawn long and thin along a world direction (owner on v3: the round puffs looked *"chonky"*), so
    ///    smoke reads as torn strands and flame tongues rather than balls. It needs the lens (<see cref="SetView"/>, or the
    ///    main camera) to turn the strand to its direction on the screen.
    ///  * A LIGHT is a soft violet glow, round, or drawn along a world direction (a slanted eye, a claw, a mouth) as a short
    ///    chain of round glows, so the shape is a direction in the world and reads the same on every camera. (Film r2:
    ///    stretched billboards on a system that never simulates drew nothing at all.)
    ///  * A MARK is smoke drawn AFTER the lights: the hollow black eyes and open mouth of a soul whose head is a glow
    ///    (SOUL STORM, section 16). Same shader and rows as a wisp, a third draw.
    /// No `Random` anywhere: the owner types every row. (The cutscene's dead and their hands were cards here until preview v4;
    /// they are 3D now, `NemuDeadFigure`.)
    /// </summary>
    public sealed class NemuSmokeBatch : IDisposable
    {
        public static readonly Color Violet = new Color(.62f, .26f, 1f, 1f);

        private readonly GameObject _smokeGo, _lightGo, _markGo;
        private readonly ParticleSystem _smoke, _light, _mark;
        private readonly Material _smokeMaterial, _lightMaterial, _markMaterial;
        private readonly ParticleSystem.Particle[] _puffs, _lights, _marks;
        private int _puffCount, _lightCount, _markCount;
        private Vector3 _viewEye, _viewLook;
        private bool _hasView;

        public Renderer SmokeRenderer { get; }
        public Renderer LightRenderer { get; }

        public NemuSmokeBatch(Transform parent, string name, int maxPuffs, int maxLights, int maxMarks = 1)
        {
            _puffs = new ParticleSystem.Particle[Mathf.Max(1, maxPuffs)];
            _lights = new ParticleSystem.Particle[Mathf.Max(1, maxLights)];
            _marks = new ParticleSystem.Particle[Mathf.Max(1, maxMarks)];
            var shader = Resources.Load<Shader>("Shaders/NemuSmoke");
            _smokeMaterial = shader != null ? new Material(shader) { name = name + "Smoke" } : null;
            _lightMaterial = shader != null ? new Material(shader) { name = name + "Light" } : null;
            if (_smokeMaterial != null) { _smokeMaterial.SetFloat("_Mode", 0f); _smokeMaterial.renderQueue = (int)RenderQueue.Transparent + 10; }
            if (_lightMaterial != null) { _lightMaterial.SetFloat("_Mode", 1f); _lightMaterial.renderQueue = (int)RenderQueue.Transparent + 14; }
            _markMaterial = shader != null ? new Material(shader) { name = name + "Mark" } : null;
            if (_markMaterial != null) { _markMaterial.SetFloat("_Mode", 0f); _markMaterial.renderQueue = (int)RenderQueue.Transparent + 18; }
            _smoke = Make(parent, name + "Smoke", _puffs.Length, _smokeMaterial, ParticleSystemRenderMode.Billboard, out _smokeGo);
            _light = Make(parent, name + "Light", _lights.Length, _lightMaterial, ParticleSystemRenderMode.Billboard, out _lightGo);
            _mark = Make(parent, name + "Mark", _marks.Length, _markMaterial, ParticleSystemRenderMode.Billboard, out _markGo);
            var markMain = _mark.main; markMain.startSize3D = true;
            SmokeRenderer = _smokeGo.GetComponent<ParticleSystemRenderer>();
            LightRenderer = _lightGo.GetComponent<ParticleSystemRenderer>();
            var smokeMain = _smoke.main; smokeMain.startSize3D = true;
        }

        private static ParticleSystem Make(Transform parent, string name, int max, Material material, ParticleSystemRenderMode mode, out GameObject go)
        {
            go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.maxParticles = max;
            main.startSpeed = 0f;
            main.gravityModifier = 0f;
            // Never simulated: every particle is placed by hand each frame.
            main.simulationSpeed = 0f;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            var emission = ps.emission; emission.enabled = false;
            var shape = ps.shape; shape.enabled = false;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = mode;
            r.sortMode = ParticleSystemSortMode.Distance;
            r.sharedMaterial = material;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.minParticleSize = 0f;
            r.maxParticleSize = 4f;
            r.alignment = ParticleSystemRenderSpace.View;
            if (material != null) VfxRenderTag.Own(go, material);
            else VfxRenderTag.Attach(go);
            ps.Play();
            return ps;
        }

        public void Begin() { _puffCount = 0; _lightCount = 0; _markCount = 0; }

        /// <summary>The lens this frame's wisps are turned for (world). Without it the main camera is used.</summary>
        public void SetView(Vector3 eye, Vector3 look) { _viewEye = eye; _viewLook = look; _hasView = true; }

        /// <summary>The angle on the lens (degrees, 0 = up) of a world direction.</summary>
        public float ScreenAngle(Vector3 dir)
        {
            Vector3 eye = _viewEye, look = _viewLook;
            if (!_hasView)
            {
                var cam = Camera.main;
                if (cam == null) return 0f;
                eye = cam.transform.position; look = eye + cam.transform.forward;
            }
            Vector3 f = look - eye; f = f.sqrMagnitude > 1e-6f ? f.normalized : Vector3.forward;
            Vector3 r = Vector3.Cross(Vector3.up, f); r = r.sqrMagnitude > 1e-6f ? r.normalized : Vector3.right;
            Vector3 u = Vector3.Cross(f, r);
            return Mathf.Atan2(Vector3.Dot(dir, r), Vector3.Dot(dir, u)) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// A strand of smoke: <paramref name="width"/> across, <paramref name="length"/> long along the world direction
        /// <paramref name="along"/>, centred on <paramref name="at"/>. The same smoke as <see cref="Puff"/>, drawn thin.
        /// </summary>
        public void Wisp(Vector3 at, Vector3 along, float width, float length, float density, float seed, float fray = 0f, float rim = .8f)
        {
            if (_puffCount >= _puffs.Length || density <= .004f || width <= .001f) return;
            _puffs[_puffCount++] = new ParticleSystem.Particle
            {
                position = at,
                velocity = Vector3.zero,
                startSize3D = new Vector3(width, Mathf.Max(width, length), 1f),
                rotation = ScreenAngle(along.sqrMagnitude > 1e-6f ? along : Vector3.up),
                startColor = new Color32((byte)(Mathf.Clamp01(rim) * 255f), (byte)(Mathf.Clamp01(fray) * 255f),
                                         (byte)(Mathf.Repeat(seed, 1f) * 255f), (byte)(Mathf.Clamp01(density) * 255f)),
                remainingLifetime = 1000f,
                startLifetime = 1000f,
            };
        }

        /// <summary>A strand of black drawn over the lights (a soul's hollow eye or open mouth); no rim, so it reads as a hole.</summary>
        public void Mark(Vector3 at, Vector3 along, float width, float length, float density, float seed)
        {
            if (_markCount >= _marks.Length || density <= .004f || width <= .001f) return;
            _marks[_markCount++] = new ParticleSystem.Particle
            {
                position = at,
                velocity = Vector3.zero,
                startSize3D = new Vector3(width, Mathf.Max(width, length), 1f),
                rotation = ScreenAngle(along.sqrMagnitude > 1e-6f ? along : Vector3.up),
                startColor = new Color32(0, 0, (byte)(Mathf.Repeat(seed, 1f) * 255f), (byte)(Mathf.Clamp01(density) * 255f)),
                remainingLifetime = 1000f,
                startLifetime = 1000f,
            };
        }

        /// <summary>A smoke puff at <paramref name="at"/> (world), <paramref name="size"/> metres across.</summary>
        /// <param name="density">0 to 1: how black its middle is.</param>
        /// <param name="seed">0 to 1: its own rim.</param>
        /// <param name="fray">0 whole, 1 gone: holes open as it dies.</param>
        /// <param name="rim">0 to 1: how much its edge glows violet.</param>
        /// <param name="spin">Degrees: turns the puff on the lens.</param>
        public void Puff(Vector3 at, float size, float density, float seed, float fray = 0f, float rim = .8f, float spin = 0f)
        {
            if (_puffCount >= _puffs.Length || density <= .004f || size <= .001f) return;
            _puffs[_puffCount++] = new ParticleSystem.Particle
            {
                position = at,
                velocity = Vector3.zero,
                startSize = size,
                rotation = spin,
                startColor = new Color32((byte)(Mathf.Clamp01(rim) * 255f), (byte)(Mathf.Clamp01(fray) * 255f),
                                         (byte)(Mathf.Repeat(seed, 1f) * 255f), (byte)(Mathf.Clamp01(density) * 255f)),
                remainingLifetime = 1000f,
                startLifetime = 1000f,
            };
        }

        /// <summary>
        /// A light at <paramref name="at"/> (world), <paramref name="size"/> wide, <paramref name="strength"/> 0 to about 1.5.
        /// Give <paramref name="along"/> to stretch it that way (world direction; its length is the extra length in metres).
        /// <paramref name="pull"/> (metres, up to 2) draws it that much nearer the lens without moving it on screen, so a glow
        /// lying on the court is not cut in half by it (film r30: Nemu's cracks were sliced into dashes).
        /// </summary>
        public void Light(Vector3 at, float size, float strength, Color colour, Vector3 along = default, float pull = 0f)
        {
            float a = 1f - Mathf.Clamp(pull, 0f, 2f) * .5f;
            if (strength <= .004f || size <= .001f) return;
            float s = Mathf.Clamp(strength, 0f, 2.5f) / 2.5f;
            var c = new Color(colour.r * s, colour.g * s, colour.b * s, 1f);
            float len = along.magnitude;
            int n = len > size * .3f ? Mathf.Clamp(Mathf.CeilToInt(len / (size * .22f)), 2, 10) : 1;
            for (int i = 0; i < n && _lightCount < _lights.Length; i++)
            {
                float u = n == 1 ? 0f : i / (float)(n - 1) - .5f;
                // Thinner toward the ends of a stretched light, so a chain reads as one tapered stroke.
                float taper = n == 1 ? 1f : 1f - Mathf.Abs(u) * .7f;
                _lights[_lightCount++] = new ParticleSystem.Particle
                {
                    position = at + along * u,
                    velocity = Vector3.zero,
                    startSize = size * taper,
                    startColor = WithAlpha(n == 1 ? c : c * ((.75f + .25f * taper) * Mathf.Min(1f, 2.6f / n)), a),
                    remainingLifetime = 1000f,
                    startLifetime = 1000f,
                };
            }
        }

        // A light's alpha is not drawn (it only adds); it carries the pull toward the lens.
        private static Color WithAlpha(Color c, float a) { c.a = a; return c; }

        /// <summary>Hand this frame's layout to the two systems; <paramref name="clock"/> drives the smoke's flow.</summary>
        public void End(float clock)
        {
            if (_smokeMaterial != null) _smokeMaterial.SetFloat("_Clock", clock);
            if (_lightMaterial != null) _lightMaterial.SetFloat("_Clock", clock);
            if (_markMaterial != null) _markMaterial.SetFloat("_Clock", clock);
            _smoke.SetParticles(_puffs, _puffCount);
            _light.SetParticles(_lights, _lightCount);
            _mark.SetParticles(_marks, _markCount);
        }

        public bool Visible
        {
            set { SmokeRenderer.enabled = value; LightRenderer.enabled = value; if (_markGo != null) _markGo.GetComponent<ParticleSystemRenderer>().enabled = value; }
        }

        public void Dispose()
        {
            if (_smokeGo != null) Destroy(_smokeGo);
            if (_lightGo != null) Destroy(_lightGo);
            if (_markGo != null) Destroy(_markGo);
        }

        private static void Destroy(UnityEngine.Object o)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(o); else UnityEngine.Object.DestroyImmediate(o);
        }
    }
}
