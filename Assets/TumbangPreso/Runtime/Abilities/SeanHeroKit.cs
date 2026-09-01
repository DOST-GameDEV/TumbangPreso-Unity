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
        /// ⚠️ 15 CHARGES, WHICH IS 15 LATA KNOCKDOWNS. Was 130 against a knockdown worth 25,
        /// which is 5.2. `docs/Hero_Strike_Balance.md` § 3.1.
        /// </summary>
        public override float UltimateCost => 15.0f;

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
                       50.0f, 0.6f, TumbangPreso.UI.AbilityGlyph.SeanRush,
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

                ctx.Motor.ApplyImpulse(forward.normalized * 17.0f
                                       * ctx.CostScale("sean.1.afterburn")
                                       + Vector3.up * 1.5f);
                _live.Clear();
                DropScorch(ctx);

                // ⚠️ 0.6 s, WHICH IS THE DASH ITSELF AND NOT A SECOND LONGER. The rush is the
                // shortest power in the game; an aura that outlived it would say Sean was still
                // charging when he had already stopped.
                Visual.AbilityVfx.AttachAura(ctx.Motor.transform,
                                             Visual.AbilityVfx.Aura.FireEmber, Duration);

                NetCue.Play("hero_sean_grunt", ctx.Position);
                NetCue.Play("sfx_fire_whoosh", ctx.Position);
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
                if (!NetAuthority.ShouldResolve()) return;
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
                            // ⚠️ 4, WHICH IS THE LIGHTEST HOLD IN THE GAME ON PURPOSE. This is
                            // a SKILL on a short cooldown, not an ultimate: it should interrupt
                            // a run and be shrugged off, and a burn nobody can shake is a
                            // different ability.
                            //
                            // ⚠️⚠️ THIS WAS THE WORST CASE OF § 83.14 AND THE NOTE HERE RECORDED
                            // THE SYMPTOM WITHOUT SEEING IT. It read *"the 1.5 s duration leaves
                            // only 0.3 s of mashable slack above `Balance.MinStunDown`, so four
                            // presses is already brisk"* — 0.3 s over four presses is 0.075 s
                            // each, and once they were spent `MashOutOfStun` refused every press
                            // for the remaining 1.1 s. Four presses at 10 Hz is 0.4 s of input,
                            // so the player spent nearly three times that hammering a key that
                            // did nothing: 🧑's *"only up to 2-3 button mash and nothing registers
                            // anymore"*. The floor is 0.60 now and the same four presses buy
                            // 0.225 s each, which is what "brisk" was supposed to mean.
                            p.ApplyStagger(1.5f, StunElement.Fire, 4);
                            // ⚠️ A DASH THROUGH THREE PLAYERS USED TO PRINT THREE "BAM!"s
                            // ON TOP OF ITS OWN "ROCKET!". The stars and the sound already
                            // confirm each hit.
                            NetCue.Play("bump", p.transform.position);

                            // ⚠️⚠️ THE STARS AND THE JOLT TRAVEL NOW, AND THE SOUND ALREADY DID.
                            // `OnTick` opens with `if (!NetAuthority.ShouldResolve()) return;`, so
                            // three players out of four watched a burning attacker with no crown
                            // over them and felt nothing when it was their own body. The accent
                            // and the weight are the same two this line chose by hand;
                            // `MatchFlair` looks the colour up off the caster's hero instead, so
                            // it cannot drift from the kit it belongs to.
                            Visual.MatchFlair.Announce(Visual.MatchFlair.Kind.HeroHit,
                                                       ctx.Motor != null ? ctx.Motor.PlayerSlot : -1,
                                                       p.PlayerSlot, p.transform.position, 1.5f);
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
                var disc = HeroHazards.SpawnFireTrail(ctx.Position, TrailRadius,
                                                      3.0f * ctx.GainScale("sean.1.afterburn"),
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
                NetCue.Play("sfx_fire_whoosh", ctx.Position);
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
                // ⚠️⚠️ THE CARD USED TO SAY *"Knocks the lata over on impact"* AND 🧑 READ IT
                // EXACTLY AS WRITTEN: *"this too it reads as unusable on defender"*. It was an
                // honest description of an attacker-only power. Both halves are fixed rather than
                // just the sentence: `HeroHazards.CreateExplosion` no longer knocks over the
                // CASTER'S OWN objective, and the crater below is what the ultimate leaves for
                // either role. The text now leads with what is true in both.
                //
                // ⚠️ DURATION 2.0 s TO 5.6 s, AND IT IS THE CRATER'S LIFE PLUS THE SLAM. The old
                // 2.0 covered the leap and the landing and nothing else, which is why
                // `SkyEvent.SecondsFor` gave it the bare floor. The ability now stays active for
                // as long as the ground is burning, which is what `IsActive` should mean.
                : base("sean_ultimate", "SUPERNOVA",
                       "Launches you up and slams you back down. The blast throws everyone near it clear and leaves the road burning behind you.",
                       0.0f, 5.6f, TumbangPreso.UI.AbilityGlyph.SeanSupernova,
                       summary: "Leap and crash down. Leaves burning ground where you land.",
                       telegraphRadius: 4.8f, telegraphRange: 0.0f,
                       castAction: "hero-sean-supernova",
                       viewmodelAction: "supernova-slam")
            {
                Windup = UltimateWindup;
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
                NetCue.Play("hero_sean_ult", ctx.Position);
                NetCue.Play("sfx_fire_whoosh", ctx.Position);
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

                    NetCue.Play("sfx_explosion_heavy", ctx.Position);

                    // ⚠️ THE BLAST IS BIGGER AND THE COMIC TEXT IS GONE. 4.8 to 5.4 m, knockback
                    // 16 to 22, hold 2.2 to 2.6: 🧑 asked for *"more of an impact"* and this is
                    // the half that is a number rather than a picture. The word went with every
                    // other cast callout in § 31.8; a blast this size does not need announcing.
                    HeroHazards.CreateExplosion(ctx.Position, 5.4f, 22.0f, 2.6f,
                        ctx.Motor.PlayerSlot, comicText: null,
                        style: HeroHazards.ExplosionStyle.Fire);

                    // ⚠️⚠️ AND THE GROUND STAYS ON FIRE, WHICH IS THE PART THAT WAS MISSING.
                    // `SpawnSupernovaCrater` carries why: Sean's was the only ultimate in the
                    // game that left nothing behind, which is 🧑's *"it just reads as a one time
                    // down on laata and knockback"* exactly. It is also what makes the power
                    // worth pressing as a taya, now that the blast no longer topples his own can.
                    HeroHazards.SpawnSupernovaCrater(ctx.Position, 5.4f, 5.0f,
                                                     ctx.Motor.PlayerSlot);

                    // ⚠️⚠️ THIS WAS `SpawnMagmaEruption`, WHICH IS DANTE'S. Sean's whole identity
                    // is `HeroFire` and Dante's is `HeroMagmaCore`, and the biggest moment in
                    // Sean's kit was throwing up Dante's orange rock. Two heroes reading as one
                    // is the most expensive version of *"they all look repetitive"*, because it
                    // costs a CHARACTER rather than an effect. `SpawnCastFlash` in Sean's own
                    // colour is now inside `CreateExplosion` via the Fire style, so the eruption
                    // here is removed rather than recoloured: one blast, one set of particles.
                }
            }
        }
    }
}
