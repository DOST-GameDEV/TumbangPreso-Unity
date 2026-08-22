using System;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    public sealed class SeanHeroKit : HeroKit
    {
        public bool IsIgnitionCannonActive { get; set; }

        public SeanHeroKit() : base("sean", "SEAN")
        {
            Skill1 = new RocketBurnDashAbility();
            Skill2 = new IgnitionCannonAbility(this);
            Ultimate = new SupernovaSmashdownAbility();
        }

        private sealed class RocketBurnDashAbility : HeroAbility
        {
            public RocketBurnDashAbility()
                : base("sean_skill1", "ROCKET BURN DASH", "Fiery shoulder charge delivering heavy knockback.", 6.5f, 0.6f)
            {
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                Vector3 forward = ctx.Forward;
                forward.y = 0.0f;
                ctx.Motor.ApplyImpulse(forward.normalized * 14.0f + Vector3.up * 1.5f);
                GameServices.Audio?.PlayAt("ability_flick_dash", ctx.Position);
            }

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                // Hit check during dash
                var round = ctx.Round;
                if (round != null)
                {
                    foreach (var p in round.Players)
                    {
                        if (p == null || p.PlayerSlot == ctx.Motor.PlayerSlot) continue;

                        Vector3 diff = p.transform.position - ctx.Position;
                        diff.y = 0.0f;
                        if (diff.magnitude <= 1.8f)
                        {
                            Vector3 hitForce = (diff.sqrMagnitude > 0.01f ? diff.normalized : ctx.Forward) * 12.0f;
                            hitForce.y = 4.0f;
                            p.ApplyImpulse(hitForce);
                            p.ApplyStagger(1.4f);
                            GameServices.Audio?.PlayAt("bump", p.transform.position);
                        }
                    }
                }
            }
        }

        private sealed class IgnitionCannonAbility : HeroAbility
        {
            private readonly SeanHeroKit _kit;

            public IgnitionCannonAbility(SeanHeroKit kit)
                : base("sean_skill2", "IGNITION CANNON", "Ignites next throw to trigger explosive force on impact.", 8.0f, 10.0f)
            {
                _kit = kit;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                _kit.IsIgnitionCannonActive = true;
                GameServices.Audio?.PlayAt("throw_charge", ctx.Position);
            }

            protected override void OnEnd(AbilityContext ctx)
            {
                _kit.IsIgnitionCannonActive = false;
            }
        }

        private sealed class SupernovaSmashdownAbility : HeroAbility
        {
            private float _airTimer;
            private bool _smashed;

            public SupernovaSmashdownAbility()
                : base("sean_ultimate", "SUPERNOVA SMASHDOWN", "Leaps high and crashes down with a massive crater explosion.", 0.0f, 1.2f)
            {
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                _airTimer = 0.55f;
                _smashed = false;

                // Launch upward
                ctx.Motor.ApplyImpulse(Vector3.up * 13.0f + ctx.Forward * 4.0f);
                GameServices.Audio?.PlayAt("jump", ctx.Position);
            }

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                _airTimer -= dt;
                if (_airTimer <= 0.0f && !_smashed)
                {
                    _smashed = true;
                    // Slam downward
                    ctx.Motor.ApplyImpulse(Vector3.down * 22.0f);
                    HeroHazards.CreateExplosion(ctx.Position, 7.0f, 16.0f, 2.2f, ctx.Motor.PlayerSlot);
                }
            }
        }
    }
}
