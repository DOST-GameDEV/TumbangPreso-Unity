using System.Collections.Generic;
using TumbangPreso.UI;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Procedural particle systems and visual effect bursts for hero abilities and hazards.
    /// Manages lightweight particle emitters for Ice, Magma, Void, and Electric elements.
    /// </summary>
    public static class AbilityVfx
    {
        private static Material _particleMat;

        private static Material GetParticleMaterial()
        {
            if (_particleMat != null) return _particleMat;

            var shader = Shader.Find("Particles/Standard Unlit")
                         ?? Shader.Find("Mobile/Particles/Additive")
                         ?? Shader.Find("Sprites/Default")
                         ?? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                         ?? Shader.Find("Unlit/Color");

            _particleMat = new Material(shader);
            _particleMat.name = "AbilityVfx_ParticleMat";
            return _particleMat;
        }

        public static void Warmup()
        {
            GetParticleMaterial();
        }

        /// <summary>
        /// Spawns a radial blizzard ice crystal burst at the given position.
        /// </summary>
        public static GameObject SpawnIceBurst(Vector3 pos, float radius)
        {
            var go = new GameObject("Vfx_IceBurst");
            go.transform.position = pos;

            var ps = go.AddComponent<ParticleSystem>();
            var pRenderer = go.GetComponent<ParticleSystemRenderer>();
            pRenderer.material = GetParticleMaterial();

            var main = ps.main;
            main.duration = 0.8f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.75f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(radius * 1.5f, radius * 2.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.70f, 0.92f, 1.0f, 0.95f),
                new Color(0.35f, 0.80f, 1.0f, 0.90f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0.0f, (short)(radius * 14))
            });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius * 0.35f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(UiTheme.HeroIceBright, 1.0f) },
                new[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) });
            colorOverLifetime.color = grad;

            ps.Play();
            return go;
        }

        /// <summary>
        /// Spawns a volcanic magma eruption of embers and sparks at the given position.
        /// </summary>
        public static GameObject SpawnMagmaEruption(Vector3 pos, float radius)
        {
            var go = new GameObject("Vfx_MagmaEruption");
            go.transform.position = pos;

            var ps = go.AddComponent<ParticleSystem>();
            var pRenderer = go.GetComponent<ParticleSystemRenderer>();
            pRenderer.material = GetParticleMaterial();

            var main = ps.main;
            main.duration = 1.0f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.95f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(radius * 1.8f, radius * 3.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.32f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1.0f, 0.85f, 0.2f, 1.0f),
                new Color(1.0f, 0.35f, 0.05f, 0.95f));
            main.gravityModifier = 1.6f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0.0f, (short)(radius * 16))
            });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = radius * 0.4f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.yellow, 0.0f), new GradientColorKey(Color.red, 1.0f) },
                new[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) });
            col.color = grad;

            ps.Play();
            return go;
        }

        /// <summary>
        /// Spawns continuous swirling spirit void wisps for the duration of a zone.
        /// </summary>
        public static GameObject SpawnVoidWisps(Vector3 pos, float radius, float duration)
        {
            var go = new GameObject("Vfx_VoidWisps");
            go.transform.position = pos;

            var ps = go.AddComponent<ParticleSystem>();
            var pRenderer = go.GetComponent<ParticleSystemRenderer>();
            pRenderer.material = GetParticleMaterial();

            var main = ps.main;
            main.duration = duration;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.36f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.78f, 0.45f, 1.0f, 0.85f),
                new Color(0.45f, 0.15f, 0.85f, 0.75f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.rateOverTime = 22.0f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius * 0.85f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(UiTheme.HeroSpiritBright, 0.0f), new GradientColorKey(new Color(0.2f, 0.0f, 0.4f), 1.0f) },
                new[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(0.8f, 0.3f), new GradientAlphaKey(0.0f, 1.0f) });
            col.color = grad;

            ps.Play();
            return go;
        }

        /// <summary>
        /// Spawns bright electric sparks and lightning arcs at the given position.
        /// </summary>
        public static GameObject SpawnElectricArcs(Vector3 pos, float radius)
        {
            var go = new GameObject("Vfx_ElectricArcs");
            go.transform.position = pos;

            var ps = go.AddComponent<ParticleSystem>();
            var pRenderer = go.GetComponent<ParticleSystemRenderer>();
            pRenderer.material = GetParticleMaterial();

            var main = ps.main;
            main.duration = 0.6f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(radius * 2.0f, radius * 3.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.22f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1.0f, 1.0f, 0.4f, 1.0f),
                new Color(0.4f, 0.95f, 1.0f, 1.0f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0.0f, (short)(radius * 16)),
                new ParticleSystem.Burst(0.12f, (short)(radius * 10))
            });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius * 0.3f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(UiTheme.HeroElectricBright, 1.0f) },
                new[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) });
            col.color = grad;

            ps.Play();
            return go;
        }
    }
}
