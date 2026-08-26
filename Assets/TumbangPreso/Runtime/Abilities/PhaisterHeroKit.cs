using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    public sealed class PhaisterHeroKit : HeroKit
    {
        public bool IsWitchfireInfused { get; set; } = true;
        public bool IsEclipseActive => Ultimate != null && Ultimate.IsActive;

        public PhaisterHeroKit() : base("phaister", "PHAISTER")
        {
            Skill1 = new KulamHexSigilAbility();
            Skill2 = new ShadowPhaseBlinkAbility();
            Ultimate = new GrandCovenEclipseAbility();
        }

        public override float UltimateCost => 110.0f;

        /// <summary>
        /// Skill 1: KULAM HEX SIGIL
        /// Places an occult hex circle on the ground. Enemies entering the circle are hexed and staggered.
        /// </summary>
        private sealed class KulamHexSigilAbility : HeroAbility
        {
            private const float HexRadius = 2.4f;
            private const float SigilLifetime = 6.0f;

            public KulamHexSigilAbility()
                : base("phaister_skill1", "KULAM HEX",
                       "Cast an occult hex sigil on the court. Enemies entering the circle are hexed and staggered by dark witchcraft.",
                       28.0f, SigilLifetime, AbilityGlyph.PhaisterHexSigil,
                       summary: "Ground rune that staggers and disorients enemies.",
                       castAction: "hero-phaister-hex",
                       viewmodelAction: "cast-hex")
            {
                MaxCharges = 1;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                GameServices.Audio?.PlayAt("hero_nemu_grunt", ctx.Position);
                GameServices.Audio?.PlayAt("sfx_ghost_teleport", ctx.Position);
                ComicPopup.Spawn(ctx.Position, "KULAM!", UiTheme.HeroSpiritBright, 1.25f);

                var forwardAim = ctx.Forward;
                Vector3 targetPos = ctx.Position + forwardAim * 4.5f;
                SpawnHexSigil(targetPos);

                AbilityVfx.AttachAura(ctx.Motor.transform, AbilityVfx.Aura.VoidWisp, 1.5f);
            }

            private void SpawnHexSigil(Vector3 origin)
            {
                var sigilGo = new GameObject("~PhaisterHexSigil");
                sigilGo.transform.position = origin;
                var hazard = sigilGo.AddComponent<PhaisterHexHazard>();
                hazard.Initialize(origin, HexRadius, SigilLifetime);
            }
        }

        /// <summary>
        /// Skill 2: SHADOW PHASE BLINK
        /// Instantaneous occult teleport forward with a purple flame burst shockwave.
        /// </summary>
        private sealed class ShadowPhaseBlinkAbility : HeroAbility
        {
            public ShadowPhaseBlinkAbility()
                : base("phaister_skill2", "SHADOW BLINK",
                       "Dissolve into purple witchfire and teleport forward, releasing a shockwave that staggers nearby opponents.",
                       22.0f, 0.4f, AbilityGlyph.PhaisterShadowBlink,
                       summary: "Instantaneous witchfire teleport and knockback shockwave.",
                       castAction: "hero-phaister-blink",
                       viewmodelAction: "blink")
            {
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                GameServices.Audio?.PlayAt("sfx_ghost_teleport", ctx.Position);
                ComicPopup.Spawn(ctx.Position, "BLINK!", UiTheme.HeroSpiritBright, 1.20f);

                Vector3 startPos = ctx.Position;
                Vector3 pushDir = ctx.Forward;

                AbilityVfx.AttachAura(ctx.Motor.transform, AbilityVfx.Aura.VoidWisp, 1.0f);
                ctx.Motor.ApplyImpulse(pushDir * 12.0f);

                // Shockwave pulse around departure point
                var hits = Physics.OverlapSphere(startPos, 2.5f);
                foreach (var hit in hits)
                {
                    var otherMotor = hit.GetComponentInParent<CharacterMotor>();
                    if (otherMotor != null && otherMotor != ctx.Motor)
                    {
                        Vector3 repel = (otherMotor.transform.position - startPos).normalized;
                        if (repel.sqrMagnitude < 0.01f) repel = pushDir;
                        otherMotor.ApplyImpulse(repel * 6.0f + Vector3.up * 2.0f);
                        otherMotor.ApplyStagger(0.4f);
                    }
                }
            }
        }

        /// <summary>
        /// Ultimate: GRAND COVEN ECLIPSE
        /// Summons a mystical violet eclipse vortex across the arena.
        /// </summary>
        private sealed class GrandCovenEclipseAbility : HeroAbility
        {
            public GrandCovenEclipseAbility()
                : base("phaister_ultimate", "GRAND COVEN",
                       "Unleash a mystical eclipse vortex. Purple lightning strikes the court, all slippers ignite with witchfire, and the Taya is blinded by mystical shadow.",
                       0.0f, 5.0f, AbilityGlyph.PhaisterEclipse,
                       summary: "Arena-wide purple eclipse with lightning and witchfire infusion.",
                       castAction: "hero-phaister-eclipse",
                       viewmodelAction: "coven-eclipse")
            {
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                GameServices.Audio?.PlayAt("hero_nemu_grunt", ctx.Position);
                GameServices.Audio?.PlayAt("sfx_ghost_appear", ctx.Position);
                ComicPopup.Spawn(ctx.Position, "GRAND COVEN ECLIPSE!", UiTheme.HeroSpiritBright, 2.0f);

                AbilityVfx.AttachAura(ctx.Motor.transform, AbilityVfx.Aura.VoidWisp, Duration);

                var kit = ctx.Motor.AbilitySystem?.Kit as PhaisterHeroKit;
                if (kit != null) kit.IsWitchfireInfused = true;
            }
        }
    }

    public sealed class PhaisterHexHazard : MonoBehaviour
    {
        private Vector3 _center;
        private float _radius;
        private float _lifetime;
        private float _elapsed;
        private Light _pointLight;

        public void Initialize(Vector3 center, float radius, float lifetime)
        {
            _center = center;
            _radius = radius;
            _lifetime = lifetime;

            _pointLight = gameObject.AddComponent<Light>();
            _pointLight.type = LightType.Point;
            _pointLight.color = new Color(0.784f, 0.392f, 1.0f);
            _pointLight.range = radius * 2.0f;
            _pointLight.intensity = 2.5f;

            AbilityVfx.AttachAura(transform, AbilityVfx.Aura.VoidWisp, lifetime);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            if (_elapsed >= _lifetime)
            {
                Destroy(gameObject);
                return;
            }

            var colliders = Physics.OverlapSphere(_center, _radius);
            foreach (var col in colliders)
            {
                var motor = col.GetComponentInParent<CharacterMotor>();
                if (motor != null)
                {
                    motor.ApplyStagger(0.20f);
                }
            }
        }
    }
}
