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
        /// Stop a freshly added emitter dead before anything is written to it.
        ///
        /// ⚠️⚠️ `AddComponent&lt;ParticleSystem&gt;()` COMES BACK ALREADY PLAYING, AND WRITING
        /// `main.duration` TO A PLAYING SYSTEM IS AN ENGINE ASSERT: *"Setting the duration while
        /// system is still playing is not supported."* All four generators here set `duration`
        /// on the line after the component is added, so every hero ability that spawned
        /// particles threw an assert. It went unnoticed because it is a LOG assert rather than
        /// an exception: the effect still played, the game carried on, and only the PlayMode
        /// runner treats an unexpected log line as a failure. `AiDiagnosticProbe` and
        /// `BotBehaviourProbe` both went red on it the first time this branch compiled.
        ///
        /// ⚠️ `StopEmittingAndClear` RATHER THAN A PLAIN `Stop`. A plain stop leaves already
        /// emitted particles alive and the system counts as still playing until they expire,
        /// which is the same assert one frame later.
        /// </summary>
        private static void Quiesce(ParticleSystem ps)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
        }

        // -------------------------------------------------------------------
        // § AMBIENT AURAS: the particles that hang off a hero while a power is running.
        //
        // ⚠️⚠️ THEY GO ON THE BODY, AND ONLY ON THE FOUR POWERS THAT ARE ACTUALLY ON A BODY.
        // 🧑 2026-08-23: *"did u add vfx particles too, like enderman or blaze in minecraft,
        // try to implement it only for those that should have it"*. Right instinct and it is
        // also the readability rule: an aura is a STATUS, so it has to mean one thing. Nemu is
        // untaggable while the wisps are up; Dante is unstunnable while the embers are; Zack and
        // Sean are moving fast while their trails are. A player who learns those four reads them
        // from across the court without a nameplate icon.
        //
        // ⚠️⚠️ CHESKA GETS NONE, ON PURPOSE. Every one of her powers is placed on the GROUND, so
        // an aura on her body would say "something is happening to Cheska" when the thing that
        // is happening is three metres in front of her. The absence is the correct answer, not
        // an omission to be tidied up later.
        //
        // ⚠️⚠️ AND NOTHING GOES ON A TRAIL. `HeroHazards` records the measurement: a dashing hero
        // drops a trail disc every 0.10 s and each lives 3 s, so ONE dash leaves up to thirty
        // live objects. Thirty looping ParticleSystems is a different kind of bug from the one
        // this feature is for. Zone hazards are singular and get one each; trails get none.
        // -------------------------------------------------------------------

        /// <summary>What an aura is made of. One per hero state that has one.</summary>
        public enum Aura
        {
            /// <summary>Nemu phasing. Purple motes falling off the body, Enderman-style.</summary>
            VoidWisp,

            /// <summary>Dante armoured. Embers rising off hot rock, Blaze-style.</summary>
            MagmaEmber,

            /// <summary>Zack overcharged. Sparks thrown off sideways.</summary>
            ElectricSpark,

            /// <summary>Sean rushing. A short, hot smear of fire.</summary>
            FireEmber,

            /// <summary>A frost zone breathing. The only one that is not on a body.</summary>
            FrostMote,
        }

        /// <summary>
        /// Hang a looping emitter off a transform for a fixed time, then clean it up.
        ///
        /// ⚠️ IT PARENTS, AND SIMULATES IN WORLD SPACE. Parenting is what makes it follow the
        /// hero; world simulation is what makes the particles STAY BEHIND when the hero moves,
        /// which is the entire difference between an aura and a cloud of dots glued to a model.
        ///
        /// ⚠️ THE CALLER DOES NOT OWN THE OBJECT. Every one of these runs for a known duration
        /// (an ability's own), so it destroys itself rather than making five kits each remember
        /// to tear one down in `OnEnd`, which is where the last aura leak came from.
        /// </summary>
        public static GameObject AttachAura(Transform host, Aura aura, float duration)
        {
            if (host == null) return null;

            var go = new GameObject("Vfx_Aura_" + aura);
            go.transform.SetParent(host, false);
            go.transform.localPosition = new Vector3(0.0f, 0.9f, 0.0f);

            var ps = go.AddComponent<ParticleSystem>();
            Quiesce(ps);

            var pRenderer = go.GetComponent<ParticleSystemRenderer>();
            pRenderer.material = GetParticleMaterial();

            var main = ps.main;
            main.duration = Mathf.Max(0.2f, duration);
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            var shape = ps.shape;
            var col = ps.colorOverLifetime;
            col.enabled = true;

            var grad = new Gradient();

            switch (aura)
            {
                case Aura.VoidWisp:
                    // Falls rather than rises, and drifts slowly. Nemu is HERE AND NOT HERE, so
                    // the motes should look like they are coming off her rather than driving.
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 1.1f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.7f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
                    main.gravityModifier = 0.28f;
                    main.startColor = new ParticleSystem.MinMaxGradient(
                        UiTheme.HeroSpirit, UiTheme.HeroSpiritBright);
                    emission.rateOverTime = 26.0f;
                    shape.shapeType = ParticleSystemShapeType.Box;
                    shape.scale = new Vector3(0.7f, 1.5f, 0.7f);
                    grad.SetKeys(
                        new[] { new GradientColorKey(UiTheme.HeroSpiritBright, 0.0f),
                                new GradientColorKey(new Color(0.18f, 0.02f, 0.34f), 1.0f) },
                        new[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(0.85f, 0.25f),
                                new GradientAlphaKey(0.0f, 1.0f) });
                    break;

                case Aura.MagmaEmber:
                    // Rises, because heat rises. Negative gravity is the whole read.
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.0f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.4f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.13f);
                    main.gravityModifier = -0.35f;
                    main.startColor = new ParticleSystem.MinMaxGradient(
                        UiTheme.HeroMagmaCore, new Color(1.0f, 0.92f, 0.55f, 1.0f));
                    emission.rateOverTime = 32.0f;
                    shape.shapeType = ParticleSystemShapeType.Box;
                    shape.scale = new Vector3(0.85f, 1.4f, 0.85f);
                    grad.SetKeys(
                        new[] { new GradientColorKey(new Color(1.0f, 0.95f, 0.6f), 0.0f),
                                new GradientColorKey(new Color(0.75f, 0.14f, 0.02f), 1.0f) },
                        new[] { new GradientAlphaKey(0.95f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) });
                    break;

                case Aura.ElectricSpark:
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.14f, 0.34f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(1.6f, 3.4f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.11f);
                    main.gravityModifier = 0.1f;
                    main.startColor = new ParticleSystem.MinMaxGradient(
                        Color.white, UiTheme.HeroElectric);
                    emission.rateOverTime = 46.0f;
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    shape.radius = 0.45f;
                    grad.SetKeys(
                        new[] { new GradientColorKey(Color.white, 0.0f),
                                new GradientColorKey(UiTheme.HeroElectric, 1.0f) },
                        new[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) });
                    break;

                case Aura.FireEmber:
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.30f, 0.65f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.2f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.18f);
                    main.gravityModifier = -0.5f;
                    main.startColor = new ParticleSystem.MinMaxGradient(
                        new Color(1.0f, 0.86f, 0.35f, 1.0f), UiTheme.HeroFire);
                    emission.rateOverTime = 55.0f;
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    shape.radius = 0.5f;
                    grad.SetKeys(
                        new[] { new GradientColorKey(new Color(1.0f, 0.9f, 0.45f), 0.0f),
                                new GradientColorKey(UiTheme.HeroFire, 1.0f) },
                        new[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) });
                    break;

                default: // FrostMote
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.8f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.45f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
                    main.gravityModifier = -0.06f;
                    main.startColor = new ParticleSystem.MinMaxGradient(
                        UiTheme.HeroIceBright, UiTheme.HeroIce);
                    emission.rateOverTime = 18.0f;
                    shape.shapeType = ParticleSystemShapeType.Circle;
                    shape.radius = 1.0f;
                    grad.SetKeys(
                        new[] { new GradientColorKey(Color.white, 0.0f),
                                new GradientColorKey(UiTheme.HeroIceBright, 1.0f) },
                        new[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(0.7f, 0.3f),
                                new GradientAlphaKey(0.0f, 1.0f) });
                    break;
            }

            col.color = grad;

            // ⚠️⚠️ THE AURA THINS AS IT DIES, AND UNTIL NOW IT DID NOT.
            // `docs/Hero_Strike_Balance.md` § 8.5 item 2: *"a spent effect and a live one look
            // identical, so a player cannot tell whether a patch of ice is about to expire. That
            // is a real gameplay read and it is free."* The same fault was on the bodies. Dante's
            // carapace makes him immune to stuns, shoves and slips, and at 0.4 s left it emitted
            // exactly as hard as at 6.0 s: the three people deciding whether to commit had no way
            // to see the window closing, so the only counterplay was counting in your head.
            //
            // ⚠️ THE RATE FALLS, THE COLOUR DOES NOT. Fading the aura's colour would fight
            // `colorOverLifetime`, which is already spending alpha on each particle's OWN life,
            // and the two curves would multiply into something that vanishes far too early.
            // Emitting FEWER particles leaves every one of them as bright as it ever was and
            // simply makes them sparse, which reads as running out rather than as dimming.
            //
            // ⚠️ AND IT HOLDS NEAR FULL FOR THE FIRST TWO THIRDS. A rate decaying from the first
            // frame reads as a failing effect rather than as a timer: the drop has to arrive late
            // enough that it can only mean "nearly over".
            float peakRate = emission.rateOverTime.constant;
            var falloff = new AnimationCurve(
                new Keyframe(0.00f, 1.00f),
                new Keyframe(0.65f, 0.82f),
                new Keyframe(1.00f, 0.14f));
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(peakRate, falloff);

            ps.Play();
            return go;
        }

        /// <summary>
        /// Spawns a radial blizzard ice crystal burst at the given position.
        /// </summary>
        public static GameObject SpawnIceBurst(Vector3 pos, float radius)
        {
            var go = new GameObject("Vfx_IceBurst");
            go.transform.position = pos;

            var ps = go.AddComponent<ParticleSystem>();
            Quiesce(ps);

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
            Quiesce(ps);

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
            Quiesce(ps);

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
            Quiesce(ps);

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

        /// <summary>
        /// Spawns a crisp, punchy radial flash / shock ring at the cast position.
        /// </summary>
        public static GameObject SpawnCastFlash(Vector3 pos, Color color, float radius = 1.8f)
        {
            var go = new GameObject("Vfx_CastFlash");
            go.transform.position = pos + Vector3.up * 0.05f;

            var ps = go.AddComponent<ParticleSystem>();
            Quiesce(ps);

            var pRenderer = go.GetComponent<ParticleSystemRenderer>();
            pRenderer.material = GetParticleMaterial();

            var main = ps.main;
            main.duration = 0.35f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.30f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(radius * 3.0f, radius * 5.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.20f);
            main.startColor = new ParticleSystem.MinMaxGradient(Color.white, color);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0.0f, (short)(radius * 12))
            });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.2f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(color, 1.0f) },
                new[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) });
            col.color = grad;

            ps.Play();
            return go;
        }

        /// <summary>
        /// Attaches an elemental charging aura specifically to the character's right hand or arm bone
        /// for weapon/throw empowerment skills (e.g. Ignition Cannon, Static Charge).
        /// </summary>
        public static GameObject AttachHandVfx(Transform host, Aura aura, float duration)
        {
            if (host == null) return null;

            // Find right arm/hand bone if skinned mesh exists, otherwise use host
            Transform mount = null;
            var skinned = host.GetComponentInChildren<SkinnedMeshRenderer>();
            if (skinned != null && skinned.bones != null)
            {
                foreach (var bone in skinned.bones)
                {
                    if (bone != null && (bone.name == "arm-right" || bone.name.Contains("hand-right") || bone.name.Contains("arm_right")))
                    {
                        mount = bone;
                        break;
                    }
                }
            }

            if (mount == null) mount = host;

            var go = new GameObject("Vfx_HandAura_" + aura);
            go.transform.SetParent(mount, false);
            go.transform.localPosition = mount == host ? new Vector3(0.35f, 0.8f, 0.35f) : new Vector3(0.0f, 0.35f, 0.0f);

            var ps = go.AddComponent<ParticleSystem>();
            Quiesce(ps);

            var pRenderer = go.GetComponent<ParticleSystemRenderer>();
            pRenderer.material = GetParticleMaterial();

            var main = ps.main;
            main.duration = Mathf.Max(0.2f, duration);
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            var shape = ps.shape;
            var col = ps.colorOverLifetime;
            col.enabled = true;

            var grad = new Gradient();

            switch (aura)
            {
                case Aura.ElectricSpark:
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.2f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.10f);
                    main.startColor = new ParticleSystem.MinMaxGradient(Color.white, UiTheme.HeroElectricBright);
                    emission.rateOverTime = 38.0f;
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    shape.radius = 0.22f;
                    grad.SetKeys(
                        new[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(UiTheme.HeroElectricBright, 1.0f) },
                        new[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) });
                    break;

                case Aura.FireEmber:
                default:
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.20f, 0.45f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.6f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
                    main.gravityModifier = -0.4f;
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(1.0f, 0.9f, 0.4f), UiTheme.HeroFireBright);
                    emission.rateOverTime = 42.0f;
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    shape.radius = 0.25f;
                    grad.SetKeys(
                        new[] { new GradientColorKey(new Color(1.0f, 0.95f, 0.5f), 0.0f), new GradientColorKey(UiTheme.HeroFire, 1.0f) },
                        new[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) });
                    break;
            }

            col.color = grad;
            ps.Play();
            return go;
        }
    }
}
