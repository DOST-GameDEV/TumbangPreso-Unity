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
                // ⚠️ HER OWN ACCENT. See `HeroHazards.SpawnWitchSigil`: every mark, light and
                // popup in this kit reached for `HeroSpiritBright`, which belongs to Nemu, and
                // `UiTheme.HeroWitchBright` was sitting unused the whole time.
                ComicPopup.Spawn(ctx.Position, "KULAM!", UiTheme.HeroWitchBright, 1.25f);

                // ⚠️ THE GLYPH AT HER FEET IS THE CAST, THE SIGIL AHEAD IS THE SPELL. 🧑 asked
                // for *"glyphs effects during spells or abilities casting"*, and the distinction
                // matters for reading a fight: the small mark says WHO is casting and the big one
                // says WHERE it lands. They are deliberately different sizes so the two can never
                // be confused at a glance.
                HeroHazards.SpawnCastGlyph(ctx.Position);

                var forwardAim = ctx.Forward;
                Vector3 targetPos = ctx.Position + forwardAim * 4.5f;
                SpawnHexSigil(targetPos);

                AbilityVfx.AttachAura(ctx.Motor.transform, AbilityVfx.Aura.HexRune, 1.5f);
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
                ComicPopup.Spawn(ctx.Position, "BLINK!", UiTheme.HeroWitchBright, 1.20f);

                Vector3 startPos = ctx.Position;
                Vector3 pushDir = ctx.Forward;

                AbilityVfx.AttachAura(ctx.Motor.transform, AbilityVfx.Aura.HexRune, 1.0f);

                // ⚠️⚠️ A GLYPH AT BOTH ENDS, AND THE PAIR IS THE COUNTERPLAY. A blink that only
                // marks where she ARRIVED tells the three people chasing her nothing they did not
                // already work out by looking at her. The mark she leaves BEHIND is the one that
                // carries information: it says a blink happened here and which way it went, which
                // is the same argument `VfxShapes.Streak` makes for why a dash trail must point.
                //
                // ⚠️ THE DEPARTURE MARK IS ALSO WHERE THE SHOCKWAVE IS. The `OverlapSphere` below
                // is centred on `startPos`, so without this the knockback came from an unmarked
                // patch of road.
                HeroHazards.SpawnCastGlyph(startPos, 1.35f, 0.85f, seed: 5);
                HeroHazards.SpawnCastGlyph(ctx.Position, 1.05f, 0.65f, seed: 6);

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
                // ⚠️⚠️ THIS PLAYED `sfx_ghost_appear`, WHICH DOES NOT EXIST AND NEVER DID.
                // `AudioDirector.PlayAtVaried` warns `no cue registered` and returns, so the
                // sixth hero's ULTIMATE was silent apart from a borrowed grunt. It is a warning
                // rather than an exception, which is precisely how `LrtTrainFlyby` called
                // `ui_move` for two months with nobody noticing; `AudioCues` records that one.
                GameServices.Audio?.PlayAt("sfx_eclipse_toll", ctx.Position);
                ComicPopup.Spawn(ctx.Position, "GRAND COVEN ECLIPSE!", UiTheme.HeroWitchBright, 2.0f);

                // ⚠️⚠️ A HEPTAGRAM, NOT A PENTAGRAM, AND IT IS HOW HER THREE POWERS ARE TOLD
                // APART. Everything Phaister does is a drawn symbol, so the silhouette rule the
                // other five heroes follow cannot separate her own kit: instead the skills draw
                // a five-pointed star and the ultimate draws a seven-pointed one, at more than
                // double the radius. That is how occult diagrams actually escalate, and it means
                // a player learns one visual language rather than three.
                //
                // ⚠️ 3.4 m, WHICH IS AN ULTIMATE'S FOOTPRINT AND NOT A SKILL'S.
                // `docs/VISION.md` § 2 rule 2 allows an ultimate to be big, one at a time. It
                // costs far less than that number suggests because a sigil is STROKES: at this
                // bar width it paints about 8 per cent of its own circle and the road shows
                // through the rest.
                HeroHazards.SpawnWitchSigil(ctx.Position, 3.4f, Duration, 7, 3, 0.02f, 11);

                AbilityVfx.AttachAura(ctx.Motor.transform, AbilityVfx.Aura.HexRune, Duration);

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

        public void Initialize(Vector3 center, float radius, float lifetime)
        {
            _center = center;
            _radius = radius;
            _lifetime = lifetime;

            // ⚠️⚠️ THIS HAZARD HAD NO GEOMETRY. It added a `Light` and an aura and drew
            // nothing at all, so Phaister's signature power was an invisible purple glow with a
            // damage circle a player could not see. `HeroAbility.TelegraphRadius` exists to stop
            // a telegraph that LIES; this was a telegraph that was never drawn.
            //
            // ⚠️ THE 2.5 INTENSITY LIGHT IS GONE WITH IT, AND THAT IS NOT A STYLE CHANGE. Every
            // hazard light in `HeroHazards` came down by roughly two thirds on 2026-08-25 for
            // one measured reason: a hot source sitting on top of its own effect paints the
            // effect rather than the street, and the dark parts of the mark then render as the
            // light's own colour at full brightness. `SpawnWitchSigil` carries a 1.1 light at
            // 1.7 m for exactly that reason, so this one would only have fought it.
            HeroHazards.SpawnWitchSigil(center, radius, lifetime, 5, 2)
                       .transform.SetParent(transform, worldPositionStays: true);

            AbilityVfx.AttachAura(transform, AbilityVfx.Aura.HexRune, lifetime);
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
