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
    ///  * A LIGHT is a soft violet glow, round, or drawn along a world direction (a slanted eye, a claw, a mouth) as a short
    ///    chain of round glows, so the shape is a direction in the world and reads the same on every camera. (Film r2:
    ///    stretched billboards on a system that never simulates drew nothing at all.)
    /// No `Random` anywhere: the owner types every row.
    /// </summary>
    public sealed class NemuSmokeBatch : IDisposable
    {
        public static readonly Color Violet = new Color(.62f, .26f, 1f, 1f);

        private readonly GameObject _smokeGo, _lightGo, _ghostGo;
        private readonly ParticleSystem _smoke, _light, _ghostSystem;
        private readonly Material _smokeMaterial, _lightMaterial, _ghostMaterial;
        private readonly ParticleSystem.Particle[] _puffs, _lights, _ghosts;
        private int _puffCount, _lightCount, _ghostCount;

        public Renderer SmokeRenderer { get; }
        public Renderer LightRenderer { get; }
        public Renderer GhostRenderer { get; }

        public NemuSmokeBatch(Transform parent, string name, int maxPuffs, int maxLights, int maxGhosts = 1)
        {
            _ghosts = new ParticleSystem.Particle[Mathf.Max(1, maxGhosts)];
            _puffs = new ParticleSystem.Particle[Mathf.Max(1, maxPuffs)];
            _lights = new ParticleSystem.Particle[Mathf.Max(1, maxLights)];
            var shader = Resources.Load<Shader>("Shaders/NemuSmoke");
            _smokeMaterial = shader != null ? new Material(shader) { name = name + "Smoke" } : null;
            _lightMaterial = shader != null ? new Material(shader) { name = name + "Light" } : null;
            if (_smokeMaterial != null) { _smokeMaterial.SetFloat("_Mode", 0f); _smokeMaterial.renderQueue = (int)RenderQueue.Transparent + 10; }
            if (_lightMaterial != null) { _lightMaterial.SetFloat("_Mode", 1f); _lightMaterial.renderQueue = (int)RenderQueue.Transparent + 14; }
            _ghostMaterial = shader != null ? new Material(shader) { name = name + "Ghost" } : null;
            if (_ghostMaterial != null) { _ghostMaterial.SetFloat("_Mode", 2f); _ghostMaterial.renderQueue = (int)RenderQueue.Transparent + 11; }
            _smoke = Make(parent, name + "Smoke", _puffs.Length, _smokeMaterial, ParticleSystemRenderMode.Billboard, out _smokeGo);
            _light = Make(parent, name + "Light", _lights.Length, _lightMaterial, ParticleSystemRenderMode.Billboard, out _lightGo);
            SmokeRenderer = _smokeGo.GetComponent<ParticleSystemRenderer>();
            LightRenderer = _lightGo.GetComponent<ParticleSystemRenderer>();
            _ghostSystem = Make(parent, name + "Ghost", _ghosts.Length, _ghostMaterial, ParticleSystemRenderMode.Billboard, out _ghostGo);
            var ghostMain = _ghostSystem.main; ghostMain.startSize3D = true;
            GhostRenderer = _ghostGo.GetComponent<ParticleSystemRenderer>();
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

        public void Begin() { _puffCount = 0; _lightCount = 0; _ghostCount = 0; }

        /// <summary>
        /// One whole ghost (`Shaders/NemuSmoke` mode 2): <paramref name="height"/> metres tall, centred on <paramref name="at"/>,
        /// turned <paramref name="screenAngle"/> degrees on the lens (0 = head up), <paramref name="face"/> 0 to 1 how much of its
        /// burning face shows (0 seen from behind), <paramref name="arms"/> 0 low and out to 1 raised and clawing.
        /// </summary>
        public void Ghost(Vector3 at, float height, float stretch, float screenAngle, float density, float seed, float face, float arms)
        {
            if (_ghostCount >= _ghosts.Length || density <= .004f || height <= .01f) return;
            _ghosts[_ghostCount++] = new ParticleSystem.Particle
            {
                position = at,
                velocity = Vector3.zero,
                startSize3D = new Vector3(height * .8f, height * (1f + stretch), 1f),
                rotation = screenAngle,
                startColor = new Color32((byte)(Mathf.Clamp01(face) * 255f), (byte)(Mathf.Clamp01(arms) * 255f),
                                         (byte)(Mathf.Repeat(seed, 1f) * 255f), (byte)(Mathf.Clamp01(density) * 255f)),
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
        /// </summary>
        public void Light(Vector3 at, float size, float strength, Color colour, Vector3 along = default)
        {
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
                    startColor = n == 1 ? c : c * ((.75f + .25f * taper) * Mathf.Min(1f, 2.6f / n)),
                    remainingLifetime = 1000f,
                    startLifetime = 1000f,
                };
            }
        }

        /// <summary>Hand this frame's layout to the two systems; <paramref name="clock"/> drives the smoke's flow.</summary>
        public void End(float clock)
        {
            if (_smokeMaterial != null) _smokeMaterial.SetFloat("_Clock", clock);
            if (_lightMaterial != null) _lightMaterial.SetFloat("_Clock", clock);
            if (_ghostMaterial != null) _ghostMaterial.SetFloat("_Clock", clock);
            _ghostSystem.SetParticles(_ghosts, _ghostCount);
            _smoke.SetParticles(_puffs, _puffCount);
            _light.SetParticles(_lights, _lightCount);
        }

        public bool Visible
        {
            set { SmokeRenderer.enabled = value; LightRenderer.enabled = value; GhostRenderer.enabled = value; }
        }

        public void Dispose()
        {
            if (_smokeGo != null) Destroy(_smokeGo);
            if (_lightGo != null) Destroy(_lightGo);
            if (_ghostGo != null) Destroy(_ghostGo);
        }

        private static void Destroy(UnityEngine.Object o)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(o); else UnityEngine.Object.DestroyImmediate(o);
        }
    }
}
