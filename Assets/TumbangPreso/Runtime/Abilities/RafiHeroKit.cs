using TumbangPreso.Net;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    public sealed class RafiHeroKit : HeroKit, ITimedKitReplication
    {
        private Slipper _loadedSlipper;
        private bool _joiningSkimSettled;
        public bool IsSkimLoaded => _loadedSlipper != null && AttackingSkill.IsActive
            && _loadedSlipper.State == SlipperState.Held && _loadedSlipper.Holder != null
            && !_loadedSlipper.Holder.IsDefender && _loadedSlipper.Holder.AbilitySystem?.Kit == this;

        public bool ConsumeSkim(Slipper slipper)
        {
            if (!IsSkimLoaded || _loadedSlipper != slipper) return false;
            _loadedSlipper = null;
            return true;
        }

        public TimedKitSnapshot CaptureTimedKit()
            => new TimedKitSnapshot(AttackingSkill, IsSkimLoaded ? AttackingSkill.DurationRemaining : 0);

        public bool RestoreTimedKit(CharacterMotor motor, TimedKitSnapshot state)
        {
            if (motor == null || _joiningSkimSettled || AttackingSkill.IsActive
                || !float.IsFinite(state.PersonalRemaining) || state.PersonalRemaining < 0
                || state.PersonalRemaining > RafiRules.SkimLoadSeconds) return false;
            _joiningSkimSettled = true;
            var held = motor.GetComponent<Carrier>()?.Held;
            if (state.PersonalRemaining <= 0 || held == null || motor.IsDefender) return false;
            _loadedSlipper = held;
            ((Skim)AttackingSkill).RestoreLoad(state.PersonalRemaining);
            return true;
        }

        public override void Reset()
        {
            _loadedSlipper = null; _joiningSkimSettled = false;
            base.Reset();
        }

        public override void ResetForRound(AbilityContext ctx)
        {
            base.ResetForRound(ctx);
            _loadedSlipper = null; _joiningSkimSettled = false;
        }

        public override float UltimateCost => 16;

        public RafiHeroKit() : base("rafi", "RAFI")
        {
            Skill1 = new Crosscurrent();
            // ABILITY-2: the four-slot shape; the defending slot waits for the owner's Hydro design.
            AttackingSkill = new Skim(this);
            DefendingSkill = new PlaceholderRoleAbility("rafi_skill2d", "Rafi", AbilityGlyph.RafiMirrorwake);
            Ultimate = new Breakwater();
        }

        private sealed class Crosscurrent : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.HostConfirmed;
            public Crosscurrent() : base("rafi_skill1", "CROSSCURRENT",
                "Aim a narrow current to bend one flying slipper. Its thrower keeps the credit; later throws pass through.",
                RafiRules.CurrentCooldown, glyph: AbilityGlyph.RafiCrosscurrent,
                summary: "Bend one flying slipper. Its thrower keeps the credit.",
                telegraphRadius: RafiRules.CurrentRadius, telegraphRange: RafiRules.CurrentRange,
                castAction: "hero-rafi-cut", viewmodelAction: "current-cut", castCue: "sfx_cast_rafi_current") { }
            protected override void OnActivate(AbilityContext ctx)
            {
                if (!NetAuthority.ShouldResolve()) return;
                bool tight = ctx.HasVariant("rafi.1.tightcut");
                float speed = 8 * ctx.GainScale("rafi.1.tightcut");
                RafiWaterField.Cast(ctx, WorldEffectSnapshot.Kind.Current, RafiRules.CurrentRadius * ctx.CostScale("rafi.1.tightcut"),
                    speed, RafiRules.CurrentGather + RafiRules.CurrentRange / speed, tight);
            }
        }

        private sealed class Skim : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.Predicted;
            private readonly RafiHeroKit _kit;
            public Skim(RafiHeroKit kit) : base("rafi_skill2", "SKIM",
                "Coat your held slipper for 8 seconds. Its next throw skims up to 2 metres after first ground contact, then rests for normal retrieval.",
                RafiRules.SkimCooldown, RafiRules.SkimLoadSeconds, glyph: AbilityGlyph.RafiMirrorwake,
                summary: "Your next throw skims on landing. Bodies and the can consume it normally.",
                castAction: "hero-rafi-feint", viewmodelAction: "mirror-feint", castCue: "sfx_cast_rafi_mirror")
            { _kit = kit; }
            public override bool CanActivate(AbilityContext ctx)
                => base.CanActivate(ctx) && !ctx.Motor.IsDefender && ctx.Carrier?.Held != null;
            public void RestoreLoad(float remaining) => RestoreLiveClock(remaining);
            protected override void OnActivate(AbilityContext ctx)
            { _kit._joiningSkimSettled = true; _kit._loadedSlipper = ctx.Carrier?.Held; }
            protected override void OnTick(AbilityContext ctx, float dt)
            {
                if (_kit._loadedSlipper == null || ctx.Carrier?.Held != _kit._loadedSlipper
                    || _kit._loadedSlipper.State != SlipperState.Held || ctx.Motor.IsDefender)
                    DurationRemaining = 0;
            }
            protected override void OnEnd(AbilityContext ctx) => _kit._loadedSlipper = null;
            protected override void OnCancelled(AbilityContext ctx) => _kit._loadedSlipper = null;
        }

        private sealed class Breakwater : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.SharedUltimate;
            public Breakwater() : base("rafi_ultimate", "BREAKWATER",
                "Release a low wave that nudges each rival once and carries loose slippers. Jump, sidestep or use cover.",
                0, glyph: AbilityGlyph.RafiBreakwater,
                summary: "Send a low wave. Rivals can jump, sidestep or use cover.",
                telegraphRadius: 3, telegraphRange: 8,
                castAction: "hero-rafi-breakwater", viewmodelAction: "breakwater-release", castCue: "sfx_cast_rafi_breakwater") { }
            protected override void OnActivate(AbilityContext ctx)
            {
                if (!NetAuthority.ShouldResolve()) return;
                RafiWaterField.Cast(ctx, WorldEffectSnapshot.Kind.Breakwater, 3, 5, 2.15f, false);
            }
        }
    }
}
