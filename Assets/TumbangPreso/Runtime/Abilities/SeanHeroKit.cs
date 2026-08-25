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

        /// <summary>
        /// ⚠️ PRICED ABOVE DANTE AND BELOW CHESKA BECAUSE IT PAYS A POINT DIRECTLY. Supernova's
        /// blast knocks the lata over, so unlike every other ultimate in the game it converts
        /// into score without needing a follow-up. It has to be aimed and it can be walked out
        /// of, which is what keeps it under Thunderstrike's 150.
        ///
        /// 130 is 5.2 lata knockdowns. `docs/Hero_Strike_Balance.md` § 3.1.
        /// </summary>
        public override float UltimateCost => 130.0f;

        private sealed class RocketBurnDashAbility : HeroAbility
        {
            private readonly HashSet<int> _hitSlots = new HashSet<int>();

            /// <summary>
            /// ⚠️⚠️ 1.0 m, DOWN FROM 1.6, FOR THE REASON WRITTEN UP ON ZACK'S `TrailRadius`: the
            /// per-disc number was never what the player was looking at. `OnTick` drops one
            /// every 0.10 s for the whole dash and each lives 3.0 s, so the whole run is live at
            /// once.
            ///
            /// The dash carries `17² / (2 · Balance.Friction) = 4.82 m`, so the corridor was
            /// `2 · 1.6 · 4.82 + π · 1.6² = 23.5 m² = 12.0 per cent of the box` for a skill,
            /// against a budget of 3 to 8 per cent. At 1.0 m it is 7.5 per cent and the lane is
            /// 2.0 m across, which is one body plus margin.
            /// </summary>
            private const float TrailRadius = 1.0f;

            /// <summary>See Zack's `MaxLiveDiscs`. Six is the whole dash at the new 0.15 s drop
            /// rate, so for Sean this is a ceiling rather than a window: his rush is short
            /// enough that the cap should never bind, and it is here so a future change to the
            /// dash distance cannot quietly reintroduce the corridor.</summary>
            private const int MaxLiveDiscs = 6;

            private readonly Queue<GameObject> _live = new Queue<GameObject>();

            public RocketBurnDashAbility()
                // ⚠️⚠️ 34 s, UP FROM 6.5. Longer than Zack's 30 because this dash also KNOCKS
                // DOWN everyone it passes through, so it is a mobility skill and an opener at
                // once. 2.6 casts a round. See Zack's `StaticRailGrindAbility` for the reasoning
                // behind the whole retune and `docs/Hero_Strike_Balance.md` § 3.1 for the table.
                : base("sean_skill1", "FLAME RUSH",
                       "Rushes you forward in a line of fire. Anyone you run through is knocked down, and the trail burns whoever follows.",
                       34.0f, 0.6f, TumbangPreso.UI.AbilityGlyph.SeanRush,
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
                _live.Clear();
                DropScorch(ctx);

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
                // ⚠️ 0.15 s RATHER THAN 0.10. At 1.0 m the discs no longer need to overlap
                // heavily to read as a continuous line, and a third fewer of them is a third
                // fewer translucent primitives in a frame `docs/VISION.md` § 2 rule 4 is about.
                _trailSpawnAccum += dt;
                if (_trailSpawnAccum >= 0.15f)
                {
                    _trailSpawnAccum = 0.0f;
                    DropScorch(ctx);
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
                            Visual.HitFeel.Land(p, Visual.HitFeel.Weight.Knockdown,
                                                UiTheme.HeroFireBright);
                        }
                    }
                }
            }

            private void DropScorch(AbilityContext ctx)
            {
                // ⚠️ THE FACING IS PASSED SO THE MARK POINTS. `SpawnFireTrail` lays a STREAK
                // rather than a disc now, and a streak with no direction is just a disc that
                // took more triangles to draw. A player who finds one has to be able to tell
                // which way Sean went, which is most of what surviving Sean is.
                var disc = HeroHazards.SpawnFireTrail(ctx.Position, TrailRadius, 3.0f,
                                                      ctx.Motor.PlayerSlot, ctx.Forward);
                if (disc == null) return;

                _live.Enqueue(disc);

                while (_live.Count > MaxLiveDiscs)
                {
                    var oldest = _live.Dequeue();
                    if (oldest != null) UnityEngine.Object.Destroy(oldest);
                }
            }

            /// <summary>See Zack's. The queue is cleared and the discs are left to their own
            /// 3.0 s life, which they are meant to outlive the dash by.</summary>
            protected override void OnEnd(AbilityContext ctx) => _live.Clear();
        }

        private sealed class IgnitionCannonAbility : HeroAbility
        {
            private readonly SeanHeroKit _kit;

            public IgnitionCannonAbility(SeanHeroKit kit)
                // ⚠️⚠️ TWO CHARGES A ROUND, BACK ONE PER LATA KNOCKDOWN. It leaves an effect on
                // the court, so it takes charges rather than a cooldown (`HeroAbility.MaxCharges`
                // has the rule), and paying it off the objective closes the skill's own loop:
                // charge the throw, land it, get the charge back.
                //
                // ⚠️ THE EXPLOSION STAYS HERE AND LEAVES ZACK'S KIT. Sean and Zack shipped as
                // the same kit in three matching slots, and this is the slot where they split:
                // Sean is the one whose near miss still counts. `docs/Hero_Strike_Balance.md`
                // § 4.4.
                : base("sean_skill2", "IGNITION CANNON",
                       "Loads your next throw with fire. Wherever that tsinelas lands it goes off, so a near miss still counts.",
                       0.0f, 10.0f, TumbangPreso.UI.AbilityGlyph.SeanIgnite,
                       summary: "Your next throw explodes where it lands.",
                       castAction: "hero-sean-ignite",
                       viewmodelAction: "ignite",
                       charges: 2,
                       rechargedBy: Recharge.LataKnocked)
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
                       0.0f, 2.0f, TumbangPreso.UI.AbilityGlyph.SeanSupernova,
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
