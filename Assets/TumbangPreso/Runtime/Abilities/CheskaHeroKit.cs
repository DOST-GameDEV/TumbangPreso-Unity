using System;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    public sealed class CheskaHeroKit : HeroKit
    {
        public CheskaHeroKit() : base("cheska", "CHESKA")
        {
            Skill1 = new PermafrostSheetAbility();
            Skill2 = new IceBarricadeAbility();
            Ultimate = new GlacialShatterBurstAbility();
        }

        private sealed class PermafrostSheetAbility : HeroAbility
        {
            public PermafrostSheetAbility()
                : base("cheska_skill1", "PERMAFROST SHEET", "Coats the ground in slippery ice, disrupting enemy footing.", 7.0f, 0.0f)
            {
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                Vector3 target = ctx.Position + ctx.Forward * 3.0f;
                HeroHazards.SpawnIceSheet(target, 5.0f, 6.0f, ctx.Motor.PlayerSlot);
                GameServices.Audio?.PlayAt("ability_shatter_trap", target);
            }
        }

        private sealed class IceBarricadeAbility : HeroAbility
        {
            public IceBarricadeAbility()
                : base("cheska_skill2", "ICE BARRICADE", "Deploys a crystalline ice wall to block attacks and routes.", 9.0f, 0.0f)
            {
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                Vector3 wallPos = ctx.Position + ctx.Forward * 2.2f;
                HeroHazards.SpawnIceBarricade(wallPos, ctx.Forward, 6.5f);
                GameServices.Audio?.PlayAt("ability_spin_guard", wallPos);
            }
        }

        private sealed class GlacialShatterBurstAbility : HeroAbility
        {
            public GlacialShatterBurstAbility()
                : base("cheska_ultimate", "GLACIAL SHATTER BURST", "Freezes nearby enemies and violently deflects slippers.", 0.0f, 0.0f)
            {
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                GameServices.Audio?.PlayAt("ability_bakya_bash", ctx.Position);

                var round = ctx.Round;
                if (round != null)
                {
                    // Freeze all nearby opponents
                    foreach (var p in round.Players)
                    {
                        if (p == null || p.PlayerSlot == ctx.Motor.PlayerSlot) continue;

                        Vector3 diff = p.transform.position - ctx.Position;
                        diff.y = 0.0f;
                        if (diff.magnitude <= 7.0f)
                        {
                            p.ApplyStagger(2.5f);
                            p.ApplyImpulse(diff.normalized * 8.0f + Vector3.up * 2.0f);
                        }
                    }

                    // Deflect slippers away
                    foreach (var s in UnityEngine.Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None))
                    {
                        if (s != null)
                        {
                            Vector3 away = s.transform.position - ctx.Position;
                            away.y = 0.0f;
                            if (away.magnitude <= 8.0f)
                            {
                                s.Deflect(away.normalized * 18.0f + Vector3.up * 4.0f, 1.0f);
                            }
                        }
                    }
                }

                // Spawn residual permafrost ring
                HeroHazards.SpawnIceSheet(ctx.Position, 6.5f, 5.0f, ctx.Motor.PlayerSlot);
            }
        }
    }
}
