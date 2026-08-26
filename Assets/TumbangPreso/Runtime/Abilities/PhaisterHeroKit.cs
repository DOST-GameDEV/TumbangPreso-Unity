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

        public override float UltimateCost => 115.0f;

        /// <summary>
        /// Skill 1: KULAM HEX SIGIL (2 Charges per round)
        /// Places an occult hex circle on the ground. Enemies entering the circle are hexed and staggered.
        /// </summary>
        private sealed class KulamHexSigilAbility : HeroAbility
        {
            private const float HexRadius = 2.4f;
            private const float SigilLifetime = 6.0f;

            public KulamHexSigilAbility()
                : base("phaister_skill1", "KULAM HEX",
                       "Cast an occult hex sigil on the court. Enemies entering the circle are hexed and staggered by dark witchcraft.",
                       0.0f, SigilLifetime, AbilityGlyph.PhaisterHexSigil,
                       summary: "Ground rune that staggers and disorients enemies.",
                       telegraphRadius: HexRadius,
                       telegraphRange: 4.5f,
                       castAction: "hero-phaister-hex",
                       viewmodelAction: "cast-hex",
                       charges: 2,
                       rechargedBy: Recharge.Never)
            {
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                // ⚠️ HER OWN CAST VOICE. This ability had none at all, so the only thing marking
                // the cast was the sigil's own `sfx_hex_cast`; every other hero in the game
                // grunts when they spend a skill. See `tools/generate_hero_audio.py` for why she
                // had no voice to play until now.
                GameServices.Audio?.PlayAt("hero_phaister_grunt", ctx.Position);

                var forwardAim = ctx.Forward;
                Vector3 targetPos = ctx.Position + forwardAim * 4.5f;
                int slot = ctx.Motor != null ? ctx.Motor.PlayerSlot : -1;

                HeroHazards.SpawnKulamHexSigil(targetPos, HexRadius, SigilLifetime, slot);
                AbilityVfx.AttachAura(ctx.Motor.transform, AbilityVfx.Aura.WitchSigil, 1.5f);
            }
        }

        /// <summary>
        /// Skill 2: SHADOW PHASE BLINK (36.0s Long Cooldown)
        /// Instantaneous occult teleport forward with a purple flame burst shockwave.
        /// </summary>
        private sealed class ShadowPhaseBlinkAbility : HeroAbility
        {
            public ShadowPhaseBlinkAbility()
                : base("phaister_skill2", "SHADOW BLINK",
                       "Dissolve into purple witchfire and teleport forward, releasing a shockwave that staggers nearby opponents.",
                       36.0f, 0.4f, AbilityGlyph.PhaisterShadowBlink,
                       summary: "Witchfire teleport and knockback shockwave.",
                       castAction: "hero-phaister-blink",
                       viewmodelAction: "blink")
            {
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                // ⚠️ `sfx_ghost_teleport` IS NEMU'S AND IS KEPT DELIBERATELY. § 21.4 took the
                // borrowed cues off her sigil and her ultimate; this one stays because a blink
                // IS the same physical event Nemu's phase is, and two heroes who share an
                // element are allowed to share the sound of the one thing they both literally
                // do. What she needed was her own THROAT over it, which is the line below.
                GameServices.Audio?.PlayAt("hero_phaister_grunt", ctx.Position);
                GameServices.Audio?.PlayAt("sfx_ghost_teleport", ctx.Position);
                ComicPopup.Spawn(ctx.Position, "BLINK!", UiTheme.HeroWitchBright, 1.20f);

                Vector3 startPos = ctx.Position;
                Vector3 pushDir = ctx.Forward;
                Vector3 endPos = startPos + pushDir * 4.2f;

                HeroHazards.SpawnShadowBlinkBurst(startPos, endPos);
                AbilityVfx.AttachAura(ctx.Motor.transform, AbilityVfx.Aura.WitchSigil, 1.0f);
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
        /// Ultimate: GRAND COVEN ECLIPSE (115.0 Ultimate Cost, Earned via Play)
        /// Summons a mystical violet eclipse vortex across the arena.
        /// </summary>
        private sealed class GrandCovenEclipseAbility : HeroAbility
        {
            public GrandCovenEclipseAbility()
                : base("phaister_ultimate", "GRAND COVEN",
                       "Unleash a mystical eclipse vortex. Purple lightning strikes the court, all slippers ignite with witchfire, and the Taya is blinded by mystical shadow.",
                       0.0f, 5.0f, AbilityGlyph.PhaisterEclipse,
                       summary: "Arena-wide eclipse with lightning and witchfire.",
                       castAction: "hero-phaister-eclipse",
                       viewmodelAction: "coven-eclipse")
            {
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                // ⚠️⚠️ THIS LINE STILL CALLED `sfx_ghost_appear` UNTIL 2026-08-26, AND
                // `docs/TODO.md` § 21.4 RECORDS IT AS ALREADY FIXED. The cue was made
                // (`sfx_eclipse_toll`), given a mix level and registered, and the CALL SITE was
                // never changed, so the biggest moment in the newest kit went on logging
                // `no cue registered for 'sfx_ghost_appear'` and playing nothing. An entry
                // ticked off against the half of the work that is visible in a file listing.
                //
                // ⚠️ AND SHE HAS HER OWN VOICE NOW. She was playing `hero_nemu_grunt`, which
                // § 21.4 left open on the belief that the voice generator was present and
                // unseeded; it was missing from the repository entirely. See
                // `tools/generate_hero_audio.py`. Nemu is the one other spirit hero, so of every
                // borrow available this was the one that blurred two characters together.
                GameServices.Audio?.PlayAt("hero_phaister_ult", ctx.Position);
                GameServices.Audio?.PlayAt("sfx_eclipse_toll", ctx.Position);
                ComicPopup.Spawn(ctx.Position, "GRAND COVEN ECLIPSE!", UiTheme.HeroWitchBright, 2.0f);

                HeroHazards.SpawnGrandCovenEclipse(ctx.Position, 5.0f, Duration);
                AbilityVfx.AttachAura(ctx.Motor.transform, AbilityVfx.Aura.WitchSigil, Duration);

                var kit = ctx.Motor.AbilitySystem?.Kit as PhaisterHeroKit;
                if (kit != null) kit.IsWitchfireInfused = true;

                // Eclipse Curse: Stagger enemies on court and shroud in dark witchfire
                var round = GameServices.Round;
                if (round != null)
                {
                    int mySlot = ctx.Motor != null ? ctx.Motor.PlayerSlot : -1;
                    foreach (var p in round.Players)
                    {
                        if (p != null && p.PlayerSlot != mySlot)
                        {
                            p.ApplyStagger(0.50f);
                            AbilityVfx.AttachAura(p.transform, AbilityVfx.Aura.WitchSigil, 2.5f);
                            ComicPopup.Spawn(p.transform.position + Vector3.up * 1.3f, "CURSED!", UiTheme.HeroWitchBright, 1.2f);
                        }
                    }
                }
            }
        }
    }
}
