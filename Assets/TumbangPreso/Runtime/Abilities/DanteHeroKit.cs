using System;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    public sealed class DanteHeroKit : HeroKit
    {
        public bool IsDemonicCarapaceActive => Skill2 != null && Skill2.IsActive;

        public DanteHeroKit() : base("dante", "DANTE")
        {
            Skill1 = new SeismicStompAbility();
            Skill2 = new DemonicCarapaceAbility();
            Ultimate = new DemonTitanFissureAbility();
        }

        private sealed class SeismicStompAbility : HeroAbility
        {
            public SeismicStompAbility()
                : base("dante_skill1", "SEISMIC STOMP", "Heavy ground slam knocking back foes and repelling slippers.", 6.5f, 0.0f)
            {
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                HeroHazards.CreateExplosion(ctx.Position, 5.5f, 10.0f, 1.4f, ctx.Motor.PlayerSlot);

                var round = ctx.Round;
                if (round != null)
                {
                    // Repel slippers with extra force
                    foreach (var s in UnityEngine.Object.FindObjectsByType<Slipper>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    {
                        if (s != null)
                        {
                            Vector3 sDiff = s.transform.position - ctx.Position;
                            sDiff.y = 0.0f;
                            if (sDiff.magnitude <= 6.5f)
                            {
                                s.Deflect(sDiff.normalized * 16.0f + Vector3.up * 4.0f, 1.0f);
                            }
                        }
                    }
                }
            }
        }

        private sealed class DemonicCarapaceAbility : HeroAbility
        {
            public DemonicCarapaceAbility()
                : base("dante_skill2", "DEMONIC CARAPACE", "Iron armor granting 4s complete immunity to stuns and shoves.", 9.0f, 4.0f)
            {
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                GameServices.Audio?.PlayAt("guard_block", ctx.Position);
                // Clear any existing stuns immediately
                ctx.Motor.ClearStun();
            }

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                // Constantly purge stuns while active
                ctx.Motor.ClearStun();
            }
        }

        private sealed class DemonTitanFissureAbility : HeroAbility
        {
            public DemonTitanFissureAbility()
                : base("dante_ultimate", "DEMON TITAN FISSURE", "Rips open earthen fissures in a cone, launching foes and raising rock pillars.", 0.0f, 0.0f)
            {
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                GameServices.Audio?.PlayAt("ability_bagsak_bomb", ctx.Position);

                Vector3 forward = ctx.Forward;
                forward.y = 0.0f;
                forward.Normalize();

                var round = ctx.Round;
                if (round != null)
                {
                    foreach (var p in round.Players)
                    {
                        if (p == null || p.PlayerSlot == ctx.Motor.PlayerSlot) continue;

                        Vector3 diff = p.transform.position - ctx.Position;
                        diff.y = 0.0f;
                        float d = diff.magnitude;

                        if (d <= 10.0f)
                        {
                            float angle = Vector3.Angle(forward, diff.normalized);
                            if (angle <= 40.0f)
                            {
                                Vector3 launch = forward * 8.0f + Vector3.up * 10.0f;
                                p.ApplyImpulse(launch);
                                p.ApplyStagger(2.0f);
                            }
                        }
                    }
                }

                HeroHazards.CreateExplosion(ctx.Position + forward * 3.0f, 6.5f, 15.0f, 2.0f, ctx.Motor.PlayerSlot);

                // Spawn 6 basalt earth pillars in forward arc / circle
                for (int i = -2; i <= 3; i++)
                {
                    float angle = i * 35.0f;
                    Vector3 offset = Quaternion.Euler(0, angle, 0) * forward * (i % 2 == 0 ? 5.5f : 4.0f);
                    HeroHazards.SpawnEarthPillar(ctx.Position + offset, 6.0f);
                }
            }
        }
    }
}
