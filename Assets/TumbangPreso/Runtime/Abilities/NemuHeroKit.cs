using System;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    public sealed class NemuHeroKit : HeroKit
    {
        public bool IsPhantomPhaseActive => Skill1 != null && Skill1.IsActive;

        public NemuHeroKit() : base("nemu", "NEMU")
        {
            Skill1 = new PhantomPhaseAbility();
            Skill2 = new GhostlyPoltergeistAbility();
            Ultimate = new NightmareSeanceVoidAbility();
        }

        private sealed class PhantomPhaseAbility : HeroAbility
        {
            public PhantomPhaseAbility()
                : base("nemu_skill1", "PHANTOM PHASE", "Phases into spirit realm for 2.5s, immune to tags and shoves.", 8.0f, 2.5f)
            {
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                GameServices.Audio?.PlayAt("ability_flick_dash", ctx.Position);
                // Mini forward slip
                ctx.Motor.ApplyImpulse(ctx.Forward * 5.0f);
            }

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                // Speed boost during phantom phase
                ctx.Motor.ApplyImpulse(ctx.Forward * 2.5f * dt);
            }
        }

        private sealed class GhostlyPoltergeistAbility : HeroAbility
        {
            public GhostlyPoltergeistAbility()
                : base("nemu_skill2", "GHOSTLY POLTERGEIST", "Sends ghost companion to haunt enemies and disrupt throws.", 7.5f, 0.0f)
            {
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                GameServices.Audio?.PlayAt("ability_shatter_trap", ctx.Position);
                HeroHazards.SpawnGhostPoltergeist(ctx.Position, ctx.Forward, ctx.Motor.PlayerSlot);
            }
        }

        private sealed class NightmareSeanceVoidAbility : HeroAbility
        {
            public NightmareSeanceVoidAbility()
                : base("nemu_ultimate", "NIGHTMARE SEANCE VOID", "Pulls dropped slippers inward and applies drowsy slow.", 0.0f, 0.0f)
            {
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                GameServices.Audio?.PlayAt("ability_spin_guard", ctx.Position);
                Vector3 voidPos = ctx.Position + ctx.Forward * 4.0f;
                HeroHazards.SpawnSeanceVoid(voidPos, 7.5f, 5.0f, ctx.Motor.PlayerSlot);
            }
        }
    }
}
