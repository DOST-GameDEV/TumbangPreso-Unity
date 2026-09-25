using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// ⚠️⚠️ PAETE, THE NINTH HERO (owner, 2026-09-25): the forest on Mount Makiling, a plant (Dendro)
    /// kit on the signature plus role shape. The owner's table, with *"all skill names are to be made
    /// better but here are the concepts"*:
    ///
    /// | Slot | Name | Owner's table |
    /// |---|---|---|
    /// | Signature | KAPIT-BAGING (Vine Pull) | His arms extend in sync toward a direction and he flies to it, a web swing. *"i want it to be an escape"*. |
    /// | Attacking | PUNLANG TSINELAS (Throwing Slipper Plant) | A plant that grows wooden slippers and shoots them on command, 10 to 20 s apart. |
    /// | Defending | BAWI (Thorn Pull) | A construct that reaches every player and pulls their slippers to it, *"even the ones on the hands"*. |
    /// | Ultimate | YAKAP NG MAKILING (Nature's Wrath) | A sentry that pulls everyone within 9 m to it, Rooted, hold Interact 7 s to break free; they can still throw and cast. |
    ///
    /// Numbers: `Core.PaeteRules`. Design and the owner's answers:
    /// `docs/reports/paete-kit-2026-09-25/plan.md`; research: `research.md` beside it; the six-beat
    /// look, sound and motion follow `docs/reports/amihan-kit-2026-09-25/direction.md`.
    ///
    /// ⚠️ HOST AUTHORITY: every slipper moved, every carry on another body, every root and every
    /// score is behind `NetAuthority.ShouldResolve()` (`tools/audit_ability_authority.py`); a peer
    /// moves only its own body (the vine reel), as the other kits do.
    /// </summary>
    public sealed class PaeteHeroKit : HeroKit
    {
        public override float UltimateCost => PaeteRules.SentryCost;

        public PaeteHeroKit() : base("paete", "PAETE")
        {
            Skill1 = new KapitBaging();
            AttackingSkill = new PunlangTsinelas();
            DefendingSkill = new Bawi();
            Ultimate = new YakapNgMakiling();
        }

        // ================================================================== KAPIT-BAGING (signature)

        private sealed class KapitBaging : HeroAbility
        {
            private CharacterMotor _caster;
            private Vector3 _anchor;
            private float _elapsed;
            private bool _reeled;

            public KapitBaging()
                : base("paete_skill1", "KAPIT-BAGING",
                       "Both arms shoot vines where you aim and reel you to what they catch. Works with a slipper in hand.",
                       PaeteRules.VineCooldown, 0.0f, AbilityGlyph.PaeteVine,
                       summary: "Vines from both arms reel you where you aim.",
                       castAction: "hero-paete-vine", viewmodelAction: "vine-reach",
                       castCue: "sfx_cast_paete_vine")
            {
                // The tell: both arms draw back and the palms glow (research.md § 3). A short
                // wind-up is also the counterplay window for an escape tool.
                Windup = PaeteRules.VineTellSeconds;
                Duration = PaeteRules.VineReachSeconds + PaeteRules.VineHoldSeconds(PaeteRules.VineRange) + 0.35f;
            }

            public override void Activate(AbilityContext ctx)
            {
                base.Activate(ctx);
                if (IsWindingUp && ctx?.Motor != null) PaetePalmGlow.Attach(ctx.Motor, PaeteRules.VineTellSeconds);
            }

            public override bool CanActivate(AbilityContext ctx)
            {
                if (!base.CanActivate(ctx)) return false;
                // A rooted body cannot swing out of the sentry, and a flying one has nowhere to reel.
                // ⚠️ NO SLIPPER GUARD, UNLIKE UPDRAFT: the owner, 2026-09-25, *"i want it to be an
                // escape"*. The 30 s cooldown and the 0.12 s tell are its price.
                return !ctx.Motor.IsRooted && !ctx.Motor.IsFlying;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                if (ctx?.Motor == null) return;
                _caster = ctx.Motor;
                _elapsed = 0.0f;
                _reeled = false;
                _anchor = PaeteVine.FindAnchor(ctx.Position, ctx.Forward, ctx.AimPoint);
                float distance = Flat(_anchor - ctx.Position).magnitude;
                float reel = PaeteRules.VineHoldSeconds(distance) + PaeteRules.VineReelSpeed / (2f * Balance.Friction);
                PaeteVineReach.Build(_caster, _anchor, PaeteRules.VineReachSeconds, reel);
                _caster.GetComponentInChildren<CharacterSquashStretch>()?.Stretch(0.14f);
            }

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                if (_caster == null || _reeled) return;
                _elapsed += dt;
                if (_elapsed < PaeteRules.VineReachSeconds) return;
                _reeled = true;
                // His own body: the owner simulates it (BeginCarry refuses anywhere else).
                Vector3 d = Flat(_anchor - _caster.transform.position);
                if (d.magnitude <= PaeteRules.VineStopShort) return;
                _caster.BeginCarry(d.normalized * PaeteRules.VineReelSpeed + Vector3.up * PaeteRules.VineLift,
                                   PaeteRules.VineHoldSeconds(d.magnitude));
            }

            protected override void OnEnd(AbilityContext ctx) { _caster = null; }

            private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0.0f, v.z);
        }

        // ================================================================== PUNLANG TSINELAS (attacking)

        private sealed class PunlangTsinelas : HeroAbility
        {
            // The seedling is a world object: the owner waits for the host before planting it, as
            // Cheska's barricade and Amihan's gale do.
            public override bool DefersPredictedEffect => true;

            public PunlangTsinelas()
                : base("paete_skill2", "PUNLANG TSINELAS",
                       "Attacking. Plant a seedling that grows wooden slippers. Press again to throw one where you aim. Pullable after 15 s.",
                       PaeteRules.PlantCooldown, PaeteRules.PlantLifeSeconds, AbilityGlyph.PaeteSprout,
                       summary: "A seedling grows wooden slippers you throw at will.",
                       telegraphRadius: 0.6f, telegraphRange: PaeteRules.PlantThrowRange,
                       castAction: "hero-paete-sprout", viewmodelAction: "seed-toss",
                       castCue: "sfx_cast_paete_sprout") { }

            // The second press is the command to throw (owner: *"he can control when they shoot"*).
            public override bool CanReactivate => true;

            protected override void OnActivate(AbilityContext ctx)
            {
                if (ctx?.Motor == null) return;
                Vector3 at = PaeteVine.GroundTarget(ctx.Position, ctx.Forward, ctx.AimPoint, PaeteRules.PlantThrowRange);
                PaetePlant.Spawn(ctx.Position + Vector3.up * 1.2f, at, ctx.Motor.PlayerSlot);
            }

            public override void Reactivate(AbilityContext ctx)
            {
                // ⚠️ NOT `EndEarly`: a second press fires, it does not dig the plant up.
                var plant = ctx?.Motor != null ? PaetePlant.OwnedBy(ctx.Motor.PlayerSlot) : null;
                if (plant == null) { EndEarly(ctx); return; }
                plant.Fire(ctx.AimPoint);
            }

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                // The ability is live as long as its plant is: pulled or withered, it is over.
                if (ctx?.Motor != null && PaetePlant.OwnedBy(ctx.Motor.PlayerSlot) == null && DurationRemaining < PaeteRules.PlantLifeSeconds - 1f)
                    DurationRemaining = 0.0f;
            }

            public override void Reset()
            {
                foreach (var p in PaetePlant.Live.ToArray()) if (p != null) Object.Destroy(p.gameObject);
                base.Reset();
            }
        }

        // ================================================================== BAWI (defending)

        private sealed class Bawi : HeroAbility
        {
            public override bool DefersPredictedEffect => true;

            public Bawi()
                : base("paete_skill2d", "BAWI",
                       "Defending. Thorns burst from your feet and take every slipper within 7 m, even from hands, and drag them to you.",
                       PaeteRules.ThornCooldown, 0.0f, AbilityGlyph.PaeteThorn,
                       summary: "Thorns take every slipper nearby, even from hands.",
                       // The ring is the construct itself, not its 7 m reach: the reach is caught in one frame and
                       // leaves nothing on the ground for a bot to path round (`AiTuning.HazardAvoidMaxRadius`).
                       telegraphRadius: 0.8f, telegraphRange: 0.0f,
                       castAction: "hero-paete-thorns", viewmodelAction: "thorn-stamp",
                       castCue: "sfx_cast_paete_thorns") { }

            protected override void OnActivate(AbilityContext ctx)
            {
                if (ctx?.Motor == null) return;
                PaeteThorns.Spawn(ctx.Position, ctx.Motor.PlayerSlot);
                ctx.Motor.GetComponentInChildren<CharacterSquashStretch>()?.Squash(0.12f);
            }
        }

        // ================================================================== YAKAP NG MAKILING (ultimate)

        private sealed class YakapNgMakiling : HeroAbility
        {
            public YakapNgMakiling()
                : base("paete_ultimate", "YAKAP NG MAKILING",
                       "A seed bursts into a sentry that drags everyone within 9 m in and roots them. Throws still work. Hold Interact 7 s to escape.",
                       0.0f, 0.0f, AbilityGlyph.PaeteSentry,
                       summary: "A sentry drags everyone in 9 m to it and roots them.",
                       telegraphRadius: PaeteRules.SentryRadius, telegraphRange: PaeteRules.SentryThrowRange,
                       castAction: "hero-paete-sentry", viewmodelAction: "sentry-throw",
                       castCue: "sfx_cast_paete_sentry") { }

            protected override void OnActivate(AbilityContext ctx)
            {
                if (ctx?.Motor == null) return;
                Vector3 at = PaeteVine.GroundTarget(ctx.Position, ctx.Forward, ctx.AimPoint, PaeteRules.SentryThrowRange);
                PaeteSentry.Spawn(ctx.Position + Vector3.up * 1.4f, at, ctx.Motor.PlayerSlot);
            }

            public override void Reset()
            {
                foreach (var s in Object.FindObjectsByType<PaeteSentry>()) if (s != null) Object.Destroy(s.gameObject);
                base.Reset();
            }
        }
    }
}
