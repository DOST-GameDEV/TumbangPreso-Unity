using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
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
            Skill2 = new MagnetRecallAbility(this);
            Ultimate = new ThunderstrikeOverdriveAbility(this);
        }

        /// <summary>
        /// ⚠️ THE MOST EXPENSIVE ULTIMATE IN THE GAME, AND IT IS PRICED ON RELIABILITY RATHER
        /// THAN ON DAMAGE. Thunderstrike stuns everyone within 4.5 m of Zack's own feet. It
        /// needs no aim, cannot miss, and there is nothing the victims can read in advance and
        /// act on, so it converts into value in every situation a player chooses to press it.
        /// Compare Titan Fissure at 12, which whiffs completely if the court scatters.
        ///
        /// ⚠️ 20 CHARGES, THE CEILING OF THE RANGE, AND IT IS TWENTY LATA KNOCKDOWNS. Was 150
        /// against a knockdown worth 25, which is six. 🧑 asked for 10 to 20 *"depending on
        /// impact"* and this is the highest-impact power in the game.
        /// `docs/Hero_Strike_Balance.md` § 3.1.
        /// </summary>
        public override float UltimateCost => 20.0f;

        private sealed class StaticRailGrindAbility : HeroAbility
        {
            private readonly ZackHeroKit _kit;
            private float _trailDropTimer;

            /// <summary>
            /// ⚠️⚠️ 1.0 m, DOWN FROM 1.8, AND THE PER-DISC NUMBER WAS NEVER THE PROBLEM.
            /// `docs/VISION.md` § 2 names this trail as the reference the whole readability
            /// budget is set from, on the grounds that one disc at 1.8 m is 5.19 per cent of the
            /// box. **This ability does not place one disc.** It drops one every 0.30 s for the
            /// whole 2.5 s duration, each living 3.0 s, so every disc of the run is live at
            /// once and what is on the floor is the swept corridor.
            ///
            /// Measured: a player holding forward covers roughly 12 m in 2.5 s, so the corridor
            /// was `2 · 1.8 · 12 + π · 1.8² = 53.4 m²`, which is **27.2 per cent of the box off
            /// a 6.0 s cooldown, more floor than any ultimate in the game.** It was invisible to
            /// every previous pass because the trails were always measured one disc at a time.
            ///
            /// At 1.0 m with <see cref="MaxLiveDiscs"/> the corridor cannot exceed about 8 per
            /// cent, and 2.0 m across is one body plus margin: you have to actually step on it.
            /// `docs/Hero_Strike_Balance.md` § 1.1 and § 3.2.
            /// </summary>
            private const float TrailRadius = 1.0f;

            /// <summary>
            /// ⚠️⚠️ THE HARD BOUND ON THE CORRIDOR, AND IT IS WHAT THE RADIUS ALONE CANNOT DO.
            /// Without a cap the trail's length is however far Zack ran, so a shrunken disc just
            /// paints a longer thin stripe. Six live discs means the wake is always about 6 m of
            /// recent path and never the whole run, whatever his speed, and that is also what
            /// makes it READ as a speed trail: a thing just behind him rather than a map of
            /// where he has been.
            /// </summary>
            private const int MaxLiveDiscs = 6;

            /// <summary>
            /// ⚠️⚠️ THE TRAIL IS LAID WHERE ZACK WAS, NOT WHERE HE IS, AND THE ABILITY'S OWN
            /// DESCRIPTION IS WHY. It says the trail *"shocks anyone chasing you"*, and dropping
            /// discs on his CURRENT position put them in front of a chaser, which is a chaser's
            /// problem only by accident. Half a second of lag puts each disc squarely in the
            /// path of someone following, which is the ability the text has always described.
            ///
            /// It also splits Zack from Sean, whose rush is a line committed FORWARD.
            /// `docs/Hero_Strike_Balance.md` § 4.4.
            /// </summary>
            private const float WakeLagSeconds = 0.5f;

            private readonly Queue<Vector3> _wake = new Queue<Vector3>();
            private readonly Queue<GameObject> _live = new Queue<GameObject>();

            public StaticRailGrindAbility(ZackHeroKit kit)
                // ⚠️⚠️ 30 s, UP FROM 6.0, AND IT IS THE SHORTEST OF THE FOUR LONG COOLDOWNS ON
                // PURPOSE. Escape and chase is what Zack is FOR, so he gets the most of it.
                //
                // At 6.0 s this cast 15 times a round. Four seats casting two skills each was
                // 44 to 56 casts per 90 s round, one every 1.8 seconds, and nothing at that
                // rate is a decision. 🧑 2026-08-25: *"game feels awkward when theres 20
                // abilities at once and i think the fix to this is making the abilities timers
                // longer? It forces users to think thoroughly abt how to use abilities"*.
                // Three casts a round is a plan.
                //
                // ⚠️ A COOLDOWN AND NOT CHARGES, and the rule is written up on
                // `HeroAbility.MaxCharges`: this moves your own body, and a player holding
                // their last escape charge does not escape, they hoard. `docs/VISION.md` § 4
                // forbids anything that rewards waiting.
                : base("zack_skill1", "BOLT SPRINT",
                       "Overcharges your skates. You move faster, and the trail you leave behind shocks anyone chasing you.",
                       46.0f, 2.5f, TumbangPreso.UI.AbilityGlyph.ZackSprint,
                       summary: "Move faster, and shock whoever chases your trail.",
                       castAction: "hero-zack-sprint",
                       viewmodelAction: "sprint-electric",
                       castCue: "sfx_cast_zack_sprint")
            {
                _kit = kit;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                Vector3 forward = ctx.Forward;
                forward.y = 0.0f;

                var squash = ctx.Motor.GetComponent<CharacterSquashStretch>();
                if (squash != null) squash.DashStretch(forward, 0.3f);

                ctx.Motor.ApplyImpulse(forward.normalized * 12.0f);

                // ⚠️ `sfx_lightning_strike` OPENED ALL THREE OF HIS ABILITIES. See
                // `HeroAbility.CastCue`: the sprint is an accelerating impulse train, the magnet
                // is a rising pull into a slap, and the summon is ring modulation going tight.
                // The strike itself still sounds, from the payload, where it lands.
                NetCue.Play("hero_zack_grunt", ctx.Position);

                _wake.Clear();
                _wake.Enqueue(ctx.Position);
                HeroHazards.SpawnShockTrail(ctx.Position,
                    TrailRadius * ctx.CostScale("zack.1.arcline"), 3.0f,
                    ctx.Motor.PlayerSlot, ctx.GainScale("zack.1.arcline"));
                _trailDropTimer = 0.25f;

                // ⚠️ THE SPARKS GO ON ZACK, NOT ON THE TRAIL DISCS. One dash drops up to thirty
                // of those, and thirty looping emitters is a different bug from the one this is
                // for. One aura on the body reads as speed and costs one system.
                Visual.AbilityVfx.AttachAura(ctx.Motor.transform,
                                             Visual.AbilityVfx.Aura.ElectricSpark, Duration);
            }

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                // Speed boost during rail grind
                ctx.Motor.ApplyImpulse(ctx.Forward * 4.0f * dt);

                _trailDropTimer -= dt;
                if (_trailDropTimer > 0.0f) return;
                _trailDropTimer = 0.30f;

                // The wake is a fixed-length ring of recent positions, so the head of it is
                // where Zack was `WakeLagSeconds` ago at this drop interval. Two samples at
                // 0.30 s is 0.60 s of lag, which is the closest the drop rate can get to 0.5.
                _wake.Enqueue(ctx.Position);
                int lagSamples = Mathf.Max(1, Mathf.RoundToInt(WakeLagSeconds / 0.30f));

                Vector3 drop;
                if (_wake.Count > lagSamples) drop = _wake.Dequeue();
                else return;   // still inside the first half second: nothing behind him yet

                var disc = HeroHazards.SpawnShockTrail(drop,
                    TrailRadius * ctx.CostScale("zack.1.arcline"), 3.0f,
                    ctx.Motor.PlayerSlot, ctx.GainScale("zack.1.arcline"));
                if (disc == null) return;

                _live.Enqueue(disc);

                // ⚠️ THE OLDEST DISC IS DESTROYED RATHER THAN LEFT TO EXPIRE. Expiry is 3.0 s
                // and the cap has to hold at every instant, not on average. A null check first
                // because the disc destroys itself on its own timer and may already be gone.
                while (_live.Count > MaxLiveDiscs)
                {
                    var oldest = _live.Dequeue();
                    if (oldest != null) UnityEngine.Object.Destroy(oldest);
                }
            }

            /// <summary>
            /// ⚠️ THE WAKE IS CLEARED WHEN THE SPRINT ENDS, NOT LEFT FOR THE NEXT CAST. Thirty
            /// seconds later the queued positions are somewhere else entirely, and reusing them
            /// would lay the first two discs of a new sprint across the arena. The DISCS are
            /// left alone: they own their own 3.0 s life and are meant to outlive the dash.
            /// </summary>
            protected override void OnEnd(AbilityContext ctx)
            {
                _wake.Clear();
                _live.Clear();
            }
        }

        /// <summary>
        /// Skill 2: MAGNET. Your own tsinelas snaps back into your hand from anywhere.
        ///
        /// ⚠️⚠️ IT REPLACES STATIC CHARGE, WHICH WAS SEAN'S IGNITION CANNON WITH A DIFFERENT
        /// ELEMENT ON IT. 🧑 2026-09-02: *"the kit of zack and sean are the exact fricking
        /// same"*, *"bcaz its js speed up and upgraded attack"*, *"it feels like theyre the exact
        /// same character js diff color based on kits"*. He is right and this file already said
        /// so: `docs/Hero_Strike_Balance.md` § 4.4 is titled *"Sean and Zack shipped as the same
        /// kit in three matching slots"*, and the fix attempted there moved NUMBERS — Sean got
        /// the blast, Zack got the speed. Two throw buffs tuned apart are still two throw buffs,
        /// and slot two was the loudest of the three matches.
        ///
        /// ⚠️⚠️ SO THE NICHE IS THE ONE THING THIS GAME IS ACTUALLY ABOUT. `docs/VISION.md` § 0:
        /// *"The tension is the retrieval, not the throw."* Every attacker's round is throw, walk
        /// back in, get caught or do not; the taya's only scoring verb exists to punish that
        /// walk. **Nothing in the game touched that loop until now.** Zack is the hero who can
        /// skip the walk, which makes him the one attacker a taya cannot plan around by standing
        /// between somebody and their tsinelas, and it is a job no other kit is doing.
        ///
        /// ⚠️⚠️ THE COST IS THAT IT IS PAID FOR BY HITTING. It keeps `Recharge.LataKnocked` from
        /// the ability it replaces, and it keeps ONE charge instead of two: recall, throw, land
        /// it, recall again. A Zack who hits never walks; a Zack who misses walks exactly like
        /// everybody else, from wherever the miss went. That is a skill loop rather than a
        /// cooldown, and it is self-limiting without a single new number.
        ///
        /// ⚠️ AND IT REFUSES RATHER THAN WASTING THE CHARGE when there is nothing to pull:
        /// already holding one, somebody else picked it up, or it is still in the air. See
        /// <see cref="CanActivate"/>. `HeroKit.CastOutcome.CannotAct` is buffered and retried for
        /// `InputBufferWindow`, so a press made a fraction of a second before the tsinelas lands
        /// still fires when it does.
        ///
        /// ⚠️ THE GLYPH IS KEPT. `AbilityGlyph.ZackOvercharge` is a charge orb with things
        /// orbiting it, which reads as a magnet as readily as it read as static, and
        /// `EveryAbilityAcrossAllHeroesHasAUniqueBespokeGlyph` only asks that no two abilities
        /// share one. `VISION.md` § 3's rule is that an icon says what the power does to the
        /// WORLD; pulling and charging are both "this thing attracts", and inventing an
        /// eighteenth bespoke glyph to say so again would be art spent on a distinction the
        /// player never has to make.
        /// </summary>
        private sealed class MagnetRecallAbility : HeroAbility
        {
            /// <summary>
            /// How long the tsinelas takes to reach him, in seconds.
            ///
            /// ⚠️⚠️ IT IS NOT INSTANT AND THE DELAY IS THE COUNTERPLAY. An arc that crosses the
            /// court is the loudest thing on screen and it points straight at Zack, so the taya
            /// is TOLD that he is about to be armed and where he is standing. A recall with no
            /// flight would be a hero who rearms with no tell at all, which is the one shape
            /// `docs/VISION.md` § 1.1 rules out: every power has to be readable by the people it
            /// is used against.
            ///
            /// ⚠️ 0.45 s IS UNDER `Balance.TagStunTime` AND THAT IS DELIBERATE. It has to be
            /// short enough that recalling mid-chase is a real option; it is long enough that a
            /// taya standing next to him gets a beat to close.
            /// </summary>
            private const float FlightSeconds = 0.45f;

            /// <summary>
            /// How long the returned tsinelas stays live in his hand, in seconds.
            ///
            /// ⚠️⚠️ THE CHARGE IS KEPT AND FOLDED INTO THE RECALL RATHER THAN DELETED WITH THE
            /// ABILITY IT CAME FROM. `SlipperAffinity.ElectricStun`, `Carrier.HostThrowAt`,
            /// `StatusStack` and `Slipper.TriggerAffinityImpact` are all built around
            /// <see cref="ZackHeroKit.IsOverchargeThrowActive"/>; dropping the only thing that
            /// sets it would have left four files of shipped, tuned behaviour that nothing in the
            /// game could ever reach again.
            ///
            /// ⚠️ AND IT IS THE BETTER ABILITY FOR IT. The shoe comes back LIVE, so the recall
            /// and the charged throw are one loop instead of two presses that happen to sit on
            /// the same hero: pull it in, throw it hard, knock the lata over, get the charge
            /// back. 10 s is the window STATIC CHARGE carried, unchanged.
            /// </summary>
            private const float ChargeSeconds = 10.0f;

            private readonly ZackHeroKit _kit;

            public MagnetRecallAbility(ZackHeroKit kit)
                : base("zack_skill2", "MAGNET",
                       "Snaps your own tsinelas back into your hand from anywhere, still live. That throw flies faster and jolts where it lands.",
                       0.0f, ChargeSeconds, TumbangPreso.UI.AbilityGlyph.ZackOvercharge,
                       summary: "Pulls your tsinelas back, charged. No walk back in.",
                       castAction: "hero-zack-charge",
                       viewmodelAction: "overcharge",
                       castCue: "sfx_cast_zack_magnet",
                       charges: 1,
                       rechargedBy: Recharge.LataKnocked)
            {
                _kit = kit;
            }

            /// <summary>
            /// ⚠️ EVERY REFUSAL IS A STATE THE PLAYER CAN SEE ON THE COURT. He is holding one, it
            /// is in the air, or somebody else has it. None of the three is a bug and none of
            /// them should cost the charge, which is why this is a `CanActivate` and not a check
            /// inside `OnActivate`.
            /// </summary>
            public override bool CanActivate(AbilityContext ctx)
            {
                if (!base.CanActivate(ctx)) return false;
                if (ctx.Motor == null || ctx.Motor.IsDefender) return false;
                // ⚠️⚠️ ALREADY HOLDING ONE IS A REFUSAL AND NOT A FREE CHARGE-UP, AND THAT IS
                // WHAT KEEPS THIS FROM BEING SEAN'S SKILL WITH EXTRA STEPS. If it charged a
                // tsinelas he is already carrying, the ability would be IGNITION CANNON and the
                // recall would be a rider on it rather than the point of it. He has to have
                // thrown, which is the whole loop.
                if (ctx.Carrier != null && ctx.Carrier.Held != null) return false;

                return FindOwn(ctx) != null;
            }

            /// <summary>
            /// His own tsinelas, if it is lying in the street.
            ///
            /// ⚠️ `OwnerSlot`, WHICH IS DEALT PER ROUND BY `SliceRunner.EquipOwnedSlippers` AND
            /// IS A LABEL RATHER THAN A LOCK (`Slipper.OwnerSlot`, `docs/TODO.md` § 79.9). That
            /// is the right key anyway: this power is about the walk back to YOUR shoe, and a
            /// version that could yank somebody else's out from under them would be a different,
            /// much nastier ability wearing this one's description.
            ///
            /// ⚠️ `Loose` ONLY. In flight it has not landed yet and held means somebody beat him
            /// to it, which is exactly the moment the taya's positioning is supposed to have paid
            /// off.
            /// </summary>
            private static Slipper FindOwn(AbilityContext ctx)
            {
                if (ctx == null || ctx.Motor == null) return null;

                foreach (var s in UnityEngine.Object.FindObjectsByType<Slipper>(
                             FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    if (s == null || s.OwnerSlot != ctx.Motor.PlayerSlot) continue;
                    if (s.State != SlipperState.Loose) continue;
                    return s;
                }

                return null;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                var mine = FindOwn(ctx);
                if (mine == null) return;

                Vector3 from = mine.transform.position;

                // ⚠️ THE CAST CUE SOUNDS AT ZACK, CENTRALLY. What plays HERE is the far end:
                // the tsinelas leaving the road, up to a court away, which is the same reasoning
                // `sfx_blink_arrive` records about the far end of a teleport.
                NetCue.Play("hero_zack_grunt", ctx.Position);
                NetCue.Play("slipper_bounce", from);

                // ⚠️⚠️ THE ARC IS DRAWN FOR EVERYBODY AND THE EQUIP IS DECIDED BY THE HOST, WHICH
                // IS `CLAUDE.md` § 4 IN ONE METHOD. `SpawnCircuitArcs` is presentation and runs on
                // every peer; `Slipper.HostForceEquip` opens with `NetAuthority.ShouldResolve()`
                // and is a no-op anywhere else, so a client predicts the effect and the host
                // decides the state. A client that could put a tsinelas in its own hand is a
                // client that can arm itself.
                HeroHazards.SpawnCircuitArcs(from, Mathf.Max(2.0f, Vector3.Distance(from, ctx.Position)),
                                             ctx.Motor.PlayerSlot);
                Visual.AbilityVfx.AttachHandVfx(ctx.Motor.transform,
                                                Visual.AbilityVfx.Aura.ElectricSpark, ChargeSeconds);

                mine.HostForceEquip(ctx.Motor);

                // ⚠️ THE SHOE ARRIVES LIVE. See `ChargeSeconds`: this flag is what
                // `Carrier.HostThrowAt` reads to stamp `SlipperAffinity.ElectricStun` onto the
                // launch, and `Carrier` clears it on the throw, so one recall charges one throw.
                _kit.IsOverchargeThrowActive = true;
            }

            protected override void OnEnd(AbilityContext ctx)
            {
                _kit.IsOverchargeThrowActive = false;
            }
        }

        private sealed class ThunderstrikeOverdriveAbility : HeroAbility
        {
            private readonly ZackHeroKit _kit;

            /// <summary>Closest he can call it. Under this it is on his own head.</summary>
            private const float MinRange = 1.5f;

            /// <summary>
            /// Furthest, at a full hold.
            ///
            /// ⚠️ 7.0 m IS HALF THE 14 m BOX, AND IT IS THE LONGEST AIM BAND IN THE GAME BY
            /// DESIGN. It costs 20 charges, the ceiling of the range, and the price note above
            /// says why it was that expensive: *"it needs no aim, cannot miss, and there is
            /// nothing the victims can read in advance and act on"*. Two of those three are no
            /// longer true, so the reach is what the aim buys back.
            /// </summary>
            private const float MaxRange = 7.0f;

            public ThunderstrikeOverdriveAbility(ZackHeroKit kit)
                : base("zack_ultimate", "THUNDERSTRIKE",
                       "Hold to pick a spot, let go and the sky opens on it. Everyone caught underneath is stunned where they stand.",
                       0.0f, 7.0f, TumbangPreso.UI.AbilityGlyph.ZackThunderstrike,
                       summary: "Hold to aim, release. Stuns everyone under the strike.",
                       telegraphRadius: 4.5f, telegraphRange: MaxRange,
                       castAction: "hero-zack-summon",
                       viewmodelAction: "summon-lightning",
                       castCue: "sfx_cast_zack_summon")
            {
                // ⚠️⚠️ IT IS AIMED NOW, AND THAT IS THE THIRD OF THE THREE MATCHING SLOTS. 🧑
                // 2026-09-02: *"the kit of zack and sean are the exact fricking same"*.
                // Thunderstrike put a 4.5 m stun circle on Zack's own feet; Supernova puts a
                // 4.8 m knockback circle on Sean's. Two ultimates that go off under the caster
                // and cannot miss are one ultimate with two particle systems, whatever the
                // payload does afterwards.
                //
                // ⚠️ SEAN'S STAYS SELF-CENTRED AND MUST. He leaps and lands on it, so the circle
                // IS where his body arrives; making both of them aimed would fix the sameness by
                // deleting the one thing that was already his. Sean commits his body, Zack
                // commits the sky.
                //
                // ⚠️ AND IT COSTS ZACK THE ONE THING THE PRICE WAS PAYING FOR: a hold-to-aim
                // ultimate can be read by everybody in the room, because the wind-up is now a
                // decision made in the open rather than a press with no tell. `UltimateCost`
                // stays at 20 for one release and is the first number to revisit if Zack comes
                // back weak; the reach is the compensation offered first because it is the one
                // that adds a decision rather than removing a cost.
                AimByHolding(MinRange, MaxRange, rampSeconds: 0.55f, maxHoldSeconds: 0.0f);
                TelegraphStyle = Visual.GroundReticle.Style.Storm;
                _kit = kit;
                Windup = UltimateWindup;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                // ⚠️ THE STRIKE LANDS ON THE RING, THE GRUNT COMES FROM ZACK. They are different
                // places now, and `AudioDirector` parks a pooled voice at the point it is given:
                // a thunderclap fired at the caster while the lightning hits seven metres away
                // is the fault `LrtTrainFlyby` records about a moving train.
                Vector3 at = AimedDestination(ctx);

                NetCue.Play("hero_zack_ult", ctx.Position);
                NetCue.Play("sfx_lightning_strike", at);
                HeroHazards.CreateThunderstrike(at, 4.5f, ctx.Motor.PlayerSlot);
                Visual.AbilityVfx.SpawnElectricArcs(at, 4.5f);

                var squash = ctx.Motor.GetComponent<CharacterSquashStretch>();
                if (squash != null) squash.Stretch(0.4f);
            }

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                ctx.Motor.ApplyImpulse(ctx.Forward * 5.5f * dt);
            }
        }
    }
}
