using TumbangPreso.Net;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    public sealed class RafiHeroKit : HeroKit, ITimedKitReplication
    {
        public const float BackwashSeconds=1.5f, BackwashScale=1.2f;
        private float _backwashRemaining;
        public float BackwashRemaining => _backwashRemaining;
        public override float PassiveDuration => BackwashSeconds;
        public override float MovementSpeedScale => !IsDefending && _backwashRemaining>0 ? BackwashScale : 1f;
        public override void OnManualOwnThrowRetrieved(AbilityContext context)
        {
            if (!NetAuthority.ShouldResolve() || context?.Motor == null || context.Motor.IsDefender
                || context.Motor.Mode != GameMode.HeroStrike || GameServices.Round?.RoundActive != true) return;
            _backwashRemaining=BackwashSeconds;
            MatchRpc.Instance?.BroadcastTimedKitState(context.Motor.PlayerSlot);
        }
        public override void Tick(AbilityContext context,float dt)
        {
            base.Tick(context,dt);
            if (context?.Motor == null || context.Motor.IsDefender || GameServices.Round?.RoundActive != true)
                _backwashRemaining=0;
            else if (float.IsFinite(dt) && dt>0) _backwashRemaining=Mathf.Max(0,_backwashRemaining-dt);
        }

        private Slipper _loadedSlipper;
        private bool _joiningSkimSettled;
        public bool IsSkimLoaded => _loadedSlipper != null && AttackingSkill.IsActive
            && _loadedSlipper.State == SlipperState.Held && _loadedSlipper.Holder != null
            && !_loadedSlipper.Holder.IsDefender && _loadedSlipper.Holder.AbilitySystem?.Kit == this;

        public bool IsSkimLoadedFor(Slipper slipper) => IsSkimLoaded && _loadedSlipper == slipper;

        public bool ConsumeSkim(Slipper slipper)
        {
            if (!IsSkimLoaded || _loadedSlipper != slipper) return false;
            _loadedSlipper = null;
            return true;
        }

        public TimedKitSnapshot CaptureTimedKit()
            => new TimedKitSnapshot(AttackingSkill, IsSkimLoaded ? AttackingSkill.DurationRemaining : 0,
                passiveRemaining:_backwashRemaining,passiveCapacity:PassiveDuration);

        public bool RestoreTimedKit(CharacterMotor motor, TimedKitSnapshot state)
        {
            if (motor == null) return false;
            bool changed=RestoreJoiningSkim(motor,state.PersonalRemaining);
            float remaining=motor.IsDefender?0:Mathf.Clamp(state.PassiveRemaining,0,BackwashSeconds);
            changed |= remaining != _backwashRemaining;_backwashRemaining=remaining;
            return changed;
        }

        private bool RestoreJoiningSkim(CharacterMotor motor,float personalRemaining)
        {
            if (motor == null || _joiningSkimSettled || AttackingSkill.IsActive
                || !float.IsFinite(personalRemaining) || personalRemaining < 0
                || personalRemaining > RafiRules.SkimLoadSeconds) return false;
            _joiningSkimSettled = true;
            var held = motor.GetComponent<Carrier>()?.Held;
            if (personalRemaining <= 0 || held == null || motor.IsDefender) return false;
            _loadedSlipper = held;
            ((Skim)AttackingSkill).RestoreLoad(personalRemaining);
            Visual.RafiSkimCoating.Ensure(held.GetComponentInChildren<MeshFilter>(),held,this);
            return true;
        }

        public override void Reset()
        {
            _loadedSlipper = null; _joiningSkimSettled = false; _backwashRemaining=0;
            base.Reset();
        }

        public override void ResetForRound(AbilityContext ctx)
        {
            base.ResetForRound(ctx);
            _loadedSlipper = null; _joiningSkimSettled = false; _backwashRemaining=0;
        }

        public override float UltimateCost => RafiRules.BahaCost;

        public RafiHeroKit() : base("rafi", "ILYAS")
        {
            Skill1 = new Crosscurrent();
            AttackingSkill = new Skim(this);
            DefendingSkill = new Waterwall();
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
                RafiRules.SkimCooldown, RafiRules.SkimLoadSeconds, glyph: AbilityGlyph.RafiSkim,
                summary: "Your next throw skims on landing. Bodies and the can consume it normally.",
                castAction: "hero-rafi-skim", viewmodelAction: "skim-coat", castCue: "sfx_cast_rafi_mirror")
            { _kit = kit; }
            public override bool CanActivate(AbilityContext ctx)
                => base.CanActivate(ctx) && !ctx.Motor.IsDefender && ctx.Carrier?.Held != null;
            public void RestoreLoad(float remaining) => RestoreLiveClock(remaining);
            protected override void OnActivate(AbilityContext ctx)
            {
                _kit._joiningSkimSettled = true; _kit._loadedSlipper = ctx.Carrier?.Held;
                var shoe=_kit._loadedSlipper;
                if(shoe!=null)Visual.RafiSkimCoating.Ensure(shoe.GetComponentInChildren<MeshFilter>(),shoe,_kit);
            }
            protected override void OnTick(AbilityContext ctx, float dt)
            {
                if (_kit._loadedSlipper == null || ctx.Carrier?.Held != _kit._loadedSlipper
                    || _kit._loadedSlipper.State != SlipperState.Held || ctx.Motor.IsDefender)
                    DurationRemaining = 0;
            }
            protected override void OnEnd(AbilityContext ctx) => _kit._loadedSlipper = null;
            protected override void OnCancelled(AbilityContext ctx) => _kit._loadedSlipper = null;
        }

        private sealed class Waterwall : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.HostConfirmed;
            public Waterwall() : base("rafi_skill2d", "WATER WALL",
                "Raise a thin 4-metre water curtain for 4 seconds. People pass through; its first flying slipper drops on the approach side and breaks it.",
                RafiRules.WallCooldown, glyph: AbilityGlyph.RafiWaterwall,
                summary: "Place a single-use curtain. People pass through.",
                telegraphRadius: RafiRules.WallHalfWidth, telegraphRange: RafiRules.WallRange,
                castAction: "hero-rafi-wall", viewmodelAction: "waterwall-lift", castCue: "sfx_cast_rafi_current")
            { AimByHolding(.75f,RafiRules.WallRange,.4f,0,whereLooking:true); }
            public override bool CanActivate(AbilityContext ctx)
                => base.CanActivate(ctx) && ctx.Motor.IsDefender
                    && RafiWaterField.CanPlaceWall(ctx,AimedDestination(ctx));
            protected override void OnActivate(AbilityContext ctx)
            {
                if (NetAuthority.ShouldResolve()) RafiWaterField.CastWall(ctx,AimedDestination(ctx));
            }
        }

        private sealed class Breakwater : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.SharedUltimate;
            public Breakwater() : base("rafi_ultimate", "BAHA",
                "After a 0.8-second warning, send a low flood front across the court. It carries loose slippers up to 3 metres and nudges each grounded rival once. Jump or use cover; held slippers and the can are unaffected.",
                0, glyph: AbilityGlyph.RafiBreakwater,
                summary: "Send a low wave. Rivals can jump, sidestep or use cover.",
                telegraphRadius: 3, telegraphRange: 8,
                castAction: "hero-rafi-breakwater", viewmodelAction: "breakwater-release", castCue: "sfx_cast_rafi_breakwater") { }
            protected override void OnActivate(AbilityContext ctx)
            {
                if (!NetAuthority.ShouldResolve()) return;
                RafiWaterField.CastBaha(ctx);
            }
        }
    }
}
