using TumbangPreso.UI;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// The particle burst on a landed hit, converted from
    /// `character_visual.gd::_spawn_impact_particles`.
    ///
    /// ⚠️ NO ART ASSET, ON PURPOSE. A handful of small unshaded points in the IMPACT colour,
    /// built in code — the moodboard's "impact effect (particle burst)" without anything to
    /// author or keep in sync with twelve rigs.
    ///
    /// ⚠️ AND IT FREES ITSELF. The original notes this explicitly: a burst per hit that never
    /// cleans up leaks one particle system per tag for the rest of the match, and a 90-second
    /// round with four players produces a lot of tags.
    /// </summary>
    public static class ImpactBurst
    {
        public const int ParticleCount = 16;
        public const float Lifetime = 0.4f;

        /// <summary>Roughly chest height, so the burst reads as happening to the UNIT rather
        /// than at its feet.</summary>
        public const float Height = 1.0f;

        public const float SpeedMin = 1.5f;
        public const float SpeedMax = 3.5f;
        public const float PointRadius = 0.04f;

        /// <summary>How long the hit flash on the body lasts. Deliberately NOT shared with the
        /// YouCard's ready flash — one is a hit landing on a 3D mesh, the other a 2D meter
        /// refilling, and there is no reason the two move together.</summary>
        public const float FlashDuration = 0.15f;

        public static void SpawnAt(Vector3 worldPosition)
        {
            var go = new GameObject("~ImpactBurst");
            go.transform.position = worldPosition + Vector3.up * Height;

            var ps = go.AddComponent<ParticleSystem>();

            // ⚠️⚠️ IT IS ALREADY PLAYING BY THE TIME WE GET IT, AND `main.duration` IS ILLEGAL
            // WHILE IT IS. `AddComponent<ParticleSystem>` returns a system that is live on the
            // frame it is added, so the very next line logged
            // *"Setting the duration while system is still playing is not supported"* — every
            // single time a tag landed. In a player that is silent log spam; in a PlayMode test
            // an unexpected assert FAILS THE RUN, which is how the first bot punch this port
            // ever landed came back as a red test rather than as the fix it was.
            //
            // Stopping with `StopEmittingAndClear` is the form the message itself asks for.
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // ⚠️ CONFIGURE BEFORE THE FIRST PLAY. A burst configured after it starts emits one
            // frame of defaults first, which looks like a white puff nobody asked for.
            var main = ps.main;
            main.duration = Lifetime;
            main.loop = false;
            main.startLifetime = Lifetime;
            main.startSpeed = new ParticleSystem.MinMaxCurve(SpeedMin, SpeedMax);
            main.startSize = PointRadius * 2.0f;
            main.startColor = UiTheme.Impact;
            main.gravityModifier = 1.0f;
            main.playOnAwake = false;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.rateOverTime = 0.0f;

            // A burst, not a stream — everything at once on the frame of the hit.
            emission.SetBursts(new[] { new ParticleSystem.Burst(0.0f, ParticleCount) });

            // Spherical emission, matching the original's 180° spread.
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            // ⚠️⚠️ A RENDERER CREATED IN CODE HAS NO MATERIAL AND UNITY DRAWS THAT IN MAGENTA.
            // This is the same rule the port ledger opens with, and a particle system is the
            // easiest place in the project to miss it: the burst still emits, still moves and
            // still dies on schedule, so everything about it is correct except that the hit
            // feedback is a spray of bright pink error quads.
            renderer.sharedMaterial = BurstMaterial;

            ps.Play();

            // Belt and braces alongside stopAction: if the system is stopped by anything else,
            // the object still goes rather than sitting in the scene forever.
            Object.Destroy(go, Lifetime * 2.0f);
        }

        private static Material _burstMaterial;

        /// <summary>
        /// One shared material for every burst. ⚠️ THE SHADER IS RESOLVED RATHER THAN NAMED
        /// ONCE: this project renders on the built-in pipeline with the URP package present, so
        /// a single hard-coded name is a magenta spray the day either changes.
        ///
        /// ⚠️ VERTEX-COLOURED AND UNTEXTURED ON PURPOSE. The Godot original draws small flat
        /// points, so the particle's own start colour IS the look, and a default particle
        /// texture would put a soft glow on a game with no other soft glows in it.
        /// </summary>
        private static Material BurstMaterial
        {
            get
            {
                if (_burstMaterial != null) return _burstMaterial;

                var shader = Shader.Find("Particles/Standard Unlit")
                             ?? Shader.Find("Sprites/Default")
                             ?? Shader.Find("Unlit/Color");

                _burstMaterial = new Material(shader) { name = "ImpactBurst" };
                return _burstMaterial;
            }
        }
    }
}
