using System;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    public sealed class ZackHeroKit : HeroKit
    {
        public bool IsOverchargeThrowActive { get; set; }
        public bool IsThunderstrikeActive => Ultimate != null && Ultimate.IsActive;

        public ZackHeroKit() : base("zack", "ZACK")
        {
            Skill1 = new StaticRailGrindAbility(this);
            Skill2 = new OverchargeThrowAbility(this);
            Ultimate = new ThunderstrikeOverdriveAbility(this);
        }

        private sealed class StaticRailGrindAbility : HeroAbility
        {
            private readonly ZackHeroKit _kit;
            private float _trailDropTimer;

            public StaticRailGrindAbility(ZackHeroKit kit)
                : base("zack_skill1", "STATIC RAIL-GRIND", "High-speed electric dash leaving a shock trail.", 6.0f, 2.5f)
            {
                _kit = kit;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                Vector3 forward = ctx.Forward;
                forward.y = 0.0f;
                ctx.Motor.ApplyImpulse(forward.normalized * 11.0f);

                GameServices.Audio?.PlayAt("dash", ctx.Position);
                HeroHazards.SpawnShockTrail(ctx.Position, 2.2f, 3.5f, ctx.Motor.PlayerSlot);
                _trailDropTimer = 0.3f;
            }

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                // Speed boost during rail grind
                ctx.Motor.ApplyImpulse(ctx.Forward * 3.5f * dt);

                _trailDropTimer -= dt;
                if (_trailDropTimer <= 0.0f)
                {
                    _trailDropTimer = 0.35f;
                    HeroHazards.SpawnShockTrail(ctx.Position, 2.0f, 3.0f, ctx.Motor.PlayerSlot);
                }
            }
        }

        private sealed class OverchargeThrowAbility : HeroAbility
        {
            private readonly ZackHeroKit _kit;

            public OverchargeThrowAbility(ZackHeroKit kit)
                : base("zack_skill2", "OVERCHARGE THROW", "Electrifies next throw for high velocity & stun blast.", 8.0f, 10.0f)
            {
                _kit = kit;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                _kit.IsOverchargeThrowActive = true;
                GameServices.Audio?.PlayAt("throw_charge", ctx.Position);
            }

            protected override void OnEnd(AbilityContext ctx)
            {
                _kit.IsOverchargeThrowActive = false;
            }
        }

        private sealed class ThunderstrikeOverdriveAbility : HeroAbility
        {
            private readonly ZackHeroKit _kit;

            public ThunderstrikeOverdriveAbility(ZackHeroKit kit)
                : base("zack_ultimate", "THUNDERSTRIKE OVERDRIVE", "Overvoltage state (+40% speed, instant throw, chain lightning).", 0.0f, 7.0f)
            {
                _kit = kit;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                GameServices.Audio?.PlayAt("ability_flick_dash", ctx.Position);
                HeroHazards.CreateExplosion(ctx.Position, 5.0f, 8.0f, 1.2f, ctx.Motor.PlayerSlot);
            }

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                ctx.Motor.ApplyImpulse(ctx.Forward * 5.0f * dt);
            }
        }
    }
}
