using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
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
            private readonly HashSet<int> _hitSlots = new HashSet<int>();

            public RocketBurnDashAbility()
                : base("sean_skill1", "FLAME RUSH",
                       "Rushes you forward in a line of fire. Anyone you run through is knocked down, and the trail burns whoever follows.",
                       6.5f, 0.6f, TumbangPreso.UI.AbilityGlyph.Dash,
                       summary: "Rush forward. Knocks down who you hit, burns who follows.",
                       castAction: "hero-sean-dash",
                       viewmodelAction: "thrust-fire")
            {
            }

            private float _trailSpawnAccum;

            protected override void OnActivate(AbilityContext ctx)
            {
                _trailSpawnAccum = 0.0f;
                _hitSlots.Clear();
                Vector3 forward = ctx.Forward;
                forward.y = 0.0f;

                var squash = ctx.Motor.GetComponent<CharacterSquashStretch>();
                if (squash != null) squash.DashStretch(forward, 0.35f);

                ctx.Motor.ApplyImpulse(forward.normalized * 17.0f + Vector3.up * 1.5f);
                HeroHazards.SpawnFireTrail(ctx.Position, 1.6f, 3.0f, ctx.Motor.PlayerSlot);

                // ⚠️ 0.6 s, WHICH IS THE DASH ITSELF AND NOT A SECOND LONGER. The rush is the
                // shortest power in the game; an aura that outlived it would say Sean was still
                // charging when he had already stopped.
                Visual.AbilityVfx.AttachAura(ctx.Motor.transform,
                                             Visual.AbilityVfx.Aura.FireEmber, Duration);

                GameServices.Audio?.PlayAt("hero_sean_grunt", ctx.Position);
                GameServices.Audio?.PlayAt("sfx_fire_whoosh", ctx.Position);
                ComicPopup.Spawn(ctx.Position, "ROCKET!", UiTheme.HeroFireBright, 1.25f);
            }

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                _trailSpawnAccum += dt;
                if (_trailSpawnAccum >= 0.10f)
                {
                    _trailSpawnAccum = 0.0f;
                    HeroHazards.SpawnFireTrail(ctx.Position, 1.6f, 3.0f, ctx.Motor.PlayerSlot);
                }

                // Hit check during dash
                var round = ctx.Round;
                if (round != null)
                {
                    foreach (var p in round.Players)
                    {
                        if (p == null || p.PlayerSlot == ctx.Motor.PlayerSlot
                            || _hitSlots.Contains(p.PlayerSlot)) continue;

                        Vector3 diff = p.transform.position - ctx.Position;
                        diff.y = 0.0f;
                        if (diff.magnitude <= 2.2f)
                        {
                            _hitSlots.Add(p.PlayerSlot);
                            Vector3 hitForce = (diff.sqrMagnitude > 0.01f ? diff.normalized : ctx.Forward) * 15.0f;
                            hitForce.y = 4.5f;
                            p.ApplyImpulse(hitForce);
                            p.ApplyStagger(1.5f);
                            // ⚠️ A DASH THROUGH THREE PLAYERS USED TO PRINT THREE "BAM!"s
                            // ON TOP OF ITS OWN "ROCKET!". The stars and the sound already
                            // confirm each hit.
                            DizzyStars.Attach(p.transform, 1.5f, UiTheme.HeroFireBright);
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
                : base("sean_skill2", "IGNITION CANNON",
                       "Loads your next throw with fire. Wherever that tsinelas lands it goes off, so a near miss still counts.",
                       8.0f, 10.0f, TumbangPreso.UI.AbilityGlyph.Empower,
                       summary: "Your next throw explodes where it lands.",
                       castAction: "hero-sean-ignite",
                       viewmodelAction: "ignite")
            {
                _kit = kit;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                _kit.IsIgnitionCannonActive = true;
                Visual.AbilityVfx.AttachHandVfx(ctx.Motor.transform, Visual.AbilityVfx.Aura.FireEmber, Duration);
                GameServices.Audio?.PlayAt("sfx_fire_whoosh", ctx.Position);
                ComicPopup.Spawn(ctx.Position, "IGNITE!", UiTheme.HeroFireBright, 1.25f);
            }

            protected override void OnEnd(AbilityContext ctx)
            {
                _kit.IsIgnitionCannonActive = false;
            }
        }

        private sealed class SupernovaSmashdownAbility : HeroAbility
        {
            private float _airTimer;
            private float _impactTimeout;
            private bool _diving;
            private bool _hasLeftGround;
            private bool _smashed;

            public SupernovaSmashdownAbility()
                : base("sean_ultimate", "SUPERNOVA",
                       "Launches you up and slams you back down. The blast knocks the lata over and everyone near it away.",
                       0.0f, 2.0f, TumbangPreso.UI.AbilityGlyph.Slam,
                       summary: "Leap and crash down. Knocks the lata over on impact.",
                       telegraphRadius: 4.8f, telegraphRange: 0.0f,
                       castAction: "hero-sean-supernova",
                       viewmodelAction: "supernova-slam")
            {
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                _airTimer = 0.55f;
                _impactTimeout = 0.85f;
                _diving = false;
                _hasLeftGround = false;
                _smashed = false;

                var squash = ctx.Motor.GetComponent<CharacterSquashStretch>();
                if (squash != null) squash.Stretch(0.45f);

                // Launch upward
                ctx.Motor.ApplyImpulse(Vector3.up * 14.0f + ctx.Forward * 4.0f);
                GameServices.Audio?.PlayAt("hero_sean_ult", ctx.Position);
                GameServices.Audio?.PlayAt("sfx_fire_whoosh", ctx.Position);
                ComicPopup.Spawn(ctx.Position, "BLAST OFF!", UiTheme.HeroFireBright, 1.3f);
            }

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                if (!ctx.Motor.IsGrounded) _hasLeftGround = true;
                _airTimer -= dt;
                if (_airTimer <= 0.0f && !_diving)
                {
                    _diving = true;
                    // ⚠️ THE MIDDLE BEAT LOSES ITS CAPTION. "BLAST OFF!" on the launch and
                    // "SUPERNOVA!" on the landing are the two moments a player acts on; a third
                    // word 0.55 s into a 2 s ultimate is one the eye never finishes reading.
                    ctx.Motor.ApplyImpulse(Vector3.down * 28.0f);
                }

                if (!_diving || _smashed) return;

                _impactTimeout -= dt;
                if ((_hasLeftGround && ctx.Motor.IsGrounded) || _impactTimeout <= 0.0f)
                {
                    _smashed = true;

                    var squash = ctx.Motor.GetComponent<CharacterSquashStretch>();
                    if (squash != null) squash.Squash(0.4f);

                    GameServices.Audio?.PlayAt("sfx_explosion_heavy", ctx.Position);
                    HeroHazards.CreateExplosion(ctx.Position, 4.8f, 16.0f, 2.2f, ctx.Motor.PlayerSlot, "SUPERNOVA!");
                    Visual.AbilityVfx.SpawnMagmaEruption(ctx.Position, 4.8f);
                }
            }
        }
    }
}
