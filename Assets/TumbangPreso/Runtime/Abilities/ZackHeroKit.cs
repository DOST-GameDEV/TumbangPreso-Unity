using System;
using System.Collections.Generic;
using System.Linq;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    public sealed class ZackHeroKit : HeroKit, ITimedKitReplication, IWorldEffectBinding
    {
        public bool IsOverchargeThrowActive { get; set; }
        public bool IsOverclocked { get; private set; }
        public override bool CanGainUltimateCharge => !IsOverclocked;
        private void RestoreOverclock()
        { IsOverclocked = true; UltimateCharge = 0; _joinThunderSettled = true; }
        public override void ResetForRound(AbilityContext context)
        {
            base.ResetForRound(context);
            IsOverchargeThrowActive = false; _joinMagnetSettled = false;
        }
        public override void ResetForMatch(AbilityContext context)
        { IsOverclocked = false; _joinThunderSettled = false; base.ResetForMatch(context); }
        public override void Reset()
        { IsOverclocked = false; IsOverchargeThrowActive = false; _joinMagnetSettled = _joinThunderSettled = false; base.Reset(); }
        public const float ObjectiveCooldownSeconds = 5;
        public override void OnObjectiveAwarded(float amount) => ApplyObjectiveCooldown(amount, true, true);
        public void ApplyObjectiveCooldown(float amount, bool signature, bool role)
        {
            if (!float.IsFinite(amount) || amount <= 0) return;
            float seconds = amount * ObjectiveCooldownSeconds;
            if (signature) Skill1?.ReduceCooldown(seconds);
            if (role)
            {
                AttackingSkill?.ReduceCooldown(seconds);
                DefendingSkill?.ReduceCooldown(seconds);
            }
        }
        private bool _joinMagnetSettled, _joinThunderSettled;

        public TimedKitSnapshot CaptureTimedKit()
            => new TimedKitSnapshot(AttackingSkill, IsOverchargeThrowActive ? AttackingSkill.DurationRemaining : 0,
                Ultimate, 0, Ultimate.IsWindingUp, IsOverclocked);

        public bool RestoreTimedKit(CharacterMotor motor, TimedKitSnapshot state)
        {
            if (motor == null) return false;
            bool restored = RestoreJoiningCharges(motor, state.PersonalRemaining, 0, state.UltimatePending);
            if (state.UltimatePermanent && !IsOverclocked) { RestoreOverclock(); restored = true; }
            return restored;
        }

        public void ConsumeMagnetCharge()
        {
            _joinMagnetSettled = true;
            IsOverchargeThrowActive = false;
        }

        public bool RestoreJoiningCharges(CharacterMotor motor, float magnetRemaining, float thunderRemaining, bool ultimatePending = false)
        {
            if (motor == null || !float.IsFinite(magnetRemaining) || magnetRemaining < 0 ||
                magnetRemaining > AttackingSkill.Duration || thunderRemaining != 0) return false;
            var context = new AbilityContext(motor, motor.GetComponent<Carrier>(), motor.GetComponent<CombatVerbs>());
            bool restored = false;
            if (!_joinMagnetSettled && !IsOverchargeThrowActive && !AttackingSkill.IsActive)
            {
                _joinMagnetSettled = true;
                ((BankShotAbility)AttackingSkill).RestoreCharge(context, magnetRemaining);
                restored = true;
            }
            return restored;
        }
        public bool IsThunderstrikeActive => Ultimate != null && Ultimate.IsActive && !IsOverclocked;
        public HeroMovementState CaptureMovementState()=>((StaticRailGrindAbility)Skill1).CaptureMovement();
        public bool RestoreJoiningMovement(CharacterMotor motor,HeroMovementState state,float age)
            => motor!=null && ((StaticRailGrindAbility)Skill1).RestoreMovement(
                new AbilityContext(motor,motor.GetComponent<Carrier>(),motor.GetComponent<CombatVerbs>()),state,age);
        public void AdoptMovementFields(int owner)=>((StaticRailGrindAbility)Skill1).AdoptFields(owner);
        public void RebindWorldEffects(CharacterMotor motor)
        { if (motor != null) AdoptMovementFields(motor.PlayerSlot); }
        public override float MovementSpeedScale => Skill1 != null && Skill1.IsActive
            ? Balance.ZackSprintSpeedScale : 1.0f;

        public ZackHeroKit() : base("zack", "ZACK")
        {
            Skill1 = new StaticRailGrindAbility(this);
            // ABILITY-2: the four-slot shape; the defending slot waits for the owner's Electro design.
            AttackingSkill = new BankShotAbility(this);
            DefendingSkill = new PlaceholderRoleAbility("zack_skill2d", "Zack", AbilityGlyph.ZackOvercharge);
            Ultimate = new ThunderstrikeOverdriveAbility(this);
        }

        // Current human Wiki anchor. New basic-mode migration remains separate.
        public override float UltimateCost => 15;

        private void RefreshChargeVisual(AbilityContext ctx)
        {
            var shoe = ctx.Carrier != null ? ctx.Carrier.Held : null;
            if (shoe != null) ZackMagnetCharge.Ensure(shoe.GetComponentInChildren<MeshFilter>(), shoe, this);
        }

        private sealed class StaticRailGrindAbility : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.Predicted;
            private readonly ZackHeroKit _kit;
            private float _trailDropTimer;
            private GameObject _sprintAura;

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

            private readonly Queue<Vector3?> _wake = new Queue<Vector3?>();
            private readonly Queue<GameObject> _live = new Queue<GameObject>();
            private bool _movementKnown, _movementRestored;

            public HeroMovementState CaptureMovement()
            {
                if(!IsActive) return HeroMovementState.Empty;
                var points=_wake.ToArray();
                var state=new HeroMovementState { Remaining=DurationRemaining,UntilNextEmission=Mathf.Max(0,_trailDropTimer),Wake=new Vector3[points.Length] };
                for(int i=0;i<points.Length;i++) if(points[i].HasValue)
                { state.Wake[i]=points[i].Value;state.KnownWake|=1u<<i; }
                return state;
            }

            public bool RestoreMovement(AbilityContext ctx,HeroMovementState state,float age)
            {
                int lagSamples=Mathf.Max(1,Mathf.RoundToInt(WakeLagSeconds/.30f));
                if(!state.Valid(Duration,.30f,age) || (state.Wake?.Length??0)>lagSamples
                    || (_movementKnown && (!_movementRestored || !IsActive))) return false;
                float remaining=Mathf.Max(0,state.Remaining-age);
                if(_movementKnown) remaining=Mathf.Min(remaining,DurationRemaining);
                bool first=!_movementRestored;
                _movementKnown=true; _movementRestored=remaining>0;
                if(remaining<=0) { EndEarly(ctx); return true; }
                RestoreLiveClock(remaining);
                _trailDropTimer=state.NextEmission(age,.30f,out int missed);
                _wake.Clear();
                var points=state.Wake??Array.Empty<Vector3>();
                for(int i=0;i<points.Length;i++) _wake.Enqueue((state.KnownWake&(1u<<i))!=0?points[i]:(Vector3?)null);
                // Preserve the missing time slots, not invented path coordinates.
                // Removing slots would make the last known point drop too late.
                for(int i=0;i<missed;i++)
                { _wake.Enqueue(null);if(_wake.Count>lagSamples)_wake.Dequeue(); }
                if(first)
                {
                    AdoptFields(ctx.Motor.PlayerSlot);
                    _sprintAura=AbilityVfx.AttachAura(ctx.Motor.transform,AbilityVfx.Aura.ElectricSpark,remaining);
                }
                return true;
            }

            public void AdoptFields(int owner)
            {
                if(!IsActive) return;
                _live.Clear();
                foreach(var field in UnityEngine.Object.FindObjectsByType<HeroHazards.ShockTrailComponent>(FindObjectsSortMode.None)
                    .Where(field=>field.OwnerSlot==owner).OrderBy(field=>field.Remaining)) _live.Enqueue(field.gameObject);
            }

            public StaticRailGrindAbility(ZackHeroKit kit)
                // ⚠️⚠️ 46 s, UP FROM 6.0, AND IT IS THE SHORTEST OF THE FOUR LONG COOLDOWNS ON
                // PURPOSE. Escape and chase is what Zack is FOR, so he gets the most of it.
                //
                // ⚠️ THIS PARAGRAPH SAID 30 s UNTIL 2026-09-04 WHILE THE CONSTRUCTOR PASSED 46.0f,
                // and the stale number had already been quoted in `SeanHeroKit` as this hero's
                // cooldown, which is how one wrong comment became two. A
                // comment that is wrong is worse than a missing one: it reads as measured, so the
                // next reader prices a new ability against it. `tools/audit_ability_stat_drift.py`
                // is why it cannot happen quietly again.
                //
                // At 6.0 s this cast 15 times a round. Four seats casting two skills each was
                // 44 to 56 casts per 90 s round, one every 1.8 seconds, and nothing at that
                // rate is a decision. 🧑 2026-08-25: *"game feels awkward when theres 20
                // abilities at once and i think the fix to this is making the abilities timers
                // longer? It forces users to think thoroughly abt how to use abilities"*.
                // Just under two casts a round is a plan.
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
                _movementKnown=true; _movementRestored=false;
                Vector3 forward = ctx.Forward;
                forward.y = 0.0f;

                var squash = ctx.Motor.GetComponent<CharacterSquashStretch>();
                if (squash != null) squash.DashStretch(forward, 0.05f);

                ctx.Motor.ApplyImpulse(forward.normalized * 12.0f);

                // ⚠️ `sfx_lightning_strike` OPENED ALL THREE OF HIS ABILITIES. See
                // `HeroAbility.CastCue`: the sprint is an accelerating impulse train, the magnet
                // is a rising pull into a slap, and the summon is ring modulation going tight.
                // The strike itself still sounds, from the payload, where it lands.
                NetCue.Play("hero_zack_grunt", ctx.Position);

                _wake.Clear();
                _wake.Enqueue(ctx.Position);
                _live.Clear();
                var first = HeroHazards.SpawnShockTrail(ctx.Position,
                    TrailRadius * ctx.CostScale("zack.1.arcline"), 3.0f,
                    ctx.Motor.PlayerSlot, ctx.GainScale("zack.1.arcline"), ctx.Forward);
                if (first != null) _live.Enqueue(first);
                _trailDropTimer = 0.25f;

                // ⚠️ THE SPARKS GO ON ZACK, NOT ON THE TRAIL DISCS. One dash drops up to thirty
                // of those, and thirty looping emitters is a different bug from the one this is
                // for. One aura on the body reads as speed and costs one system.
                _sprintAura = Visual.AbilityVfx.AttachAura(ctx.Motor.transform,
                                             Visual.AbilityVfx.Aura.ElectricSpark, Duration);
            }

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                // Sustained speed is applied to the motor's wish speed. A 4 m/s²
                // impulse was erased by the motor's 30 m/s² friction before movement.
                _trailDropTimer -= dt;
                if (_trailDropTimer > 0.0f) return;
                _trailDropTimer = 0.30f;

                // The wake is a fixed-length ring of recent positions, so the head of it is
                // where Zack was `WakeLagSeconds` ago at this drop interval. Two samples at
                // 0.30 s is 0.60 s of lag, which is the closest the drop rate can get to 0.5.
                _wake.Enqueue(ctx.Position);
                int lagSamples = Mathf.Max(1, Mathf.RoundToInt(WakeLagSeconds / 0.30f));

                if(_wake.Count<=lagSamples) return;
                var sample=_wake.Dequeue();
                if(!sample.HasValue) return;
                Vector3 drop=sample.Value;

                var disc = HeroHazards.SpawnShockTrail(drop,
                    TrailRadius * ctx.CostScale("zack.1.arcline"), 3.0f,
                    ctx.Motor.PlayerSlot, ctx.GainScale("zack.1.arcline"), ctx.Position - drop);
                if (disc == null) return;

                _live.Enqueue(disc);

                // ⚠️ THE OLDEST DISC IS DESTROYED RATHER THAN LEFT TO EXPIRE. Expiry is 3.0 s
                // and the cap has to hold at every instant, not on average. A null check first
                // because the disc destroys itself on its own timer and may already be gone.
                while (_live.Count > MaxLiveDiscs)
                {
                    var oldest = _live.Dequeue();
                    if (oldest != null)
                    {
                        oldest.SetActive(false);
                        UnityEngine.Object.Destroy(oldest);
                    }
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
                if(_sprintAura!=null) { _sprintAura.SetActive(false); UnityEngine.Object.Destroy(_sprintAura); }
                _sprintAura = null;
            }

            protected override void OnCancelled(AbilityContext ctx)
            {
                // Accepted trails may outlive a completed sprint. A refused or
                // reset cast must remove its own predicted wake immediately.
                foreach (var patch in _live)
                {
                    if (patch == null) continue;
                    patch.SetActive(false);
                    UnityEngine.Object.Destroy(patch);
                }
                OnEnd(ctx);
            }
        }

        public bool IsBankShotLoadedFor(Slipper shoe)
            => ((BankShotAbility)AttackingSkill).LoadedFor(shoe);
        public SlipperAffinity BankShotAffinityFor(Slipper shoe)
            => IsBankShotLoadedFor(shoe) ? (IsOverclocked ? SlipperAffinity.OverclockBank : SlipperAffinity.BankShot)
                : SlipperAffinity.Normal;

        private sealed class BankShotAbility : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.Predicted;
            private readonly ZackHeroKit _kit;
            private Slipper _loaded;
            private int _joiningSeat = -1;
            public BankShotAbility(ZackHeroKit kit)
                : base("zack_skill2", "BANK SHOT",
                       "Load your held slipper for eight seconds. Its next throw retains 85% speed on the first wall bank. Overclock permits two credited banks.",
                       35, 8, AbilityGlyph.ZackOvercharge,
                       summary: "Load one throw for a stronger bank; Overclock allows two.",
                       castAction: "hero-zack-charge", viewmodelAction: "overcharge", castCue: "sfx_cast_zack_magnet")
            { _kit = kit; }

            public override bool CanActivate(AbilityContext ctx)
                => base.CanActivate(ctx) && !ctx.Motor.IsDefender && ctx.Carrier?.Held != null;

            public bool LoadedFor(Slipper shoe)
            {
                if (!_kit.IsOverchargeThrowActive || DurationRemaining <= 0 || shoe == null) return false;
                return _loaded == shoe || (_loaded == null && _joiningSeat >= 0 && shoe.SeatOfOrigin == _joiningSeat);
            }
            protected override void OnActivate(AbilityContext ctx)
            {
                if (ctx?.Carrier?.Held == null) return;
                _loaded = ctx.Carrier.Held; _joiningSeat = -1;
                _kit._joinMagnetSettled = true; _kit.IsOverchargeThrowActive = true;
                _kit.RefreshChargeVisual(ctx);
            }
            public void RestoreCharge(AbilityContext ctx, float remaining)
            {
                if (remaining <= 0) { EndEarly(ctx); _kit.IsOverchargeThrowActive = false; return; }
                RestoreLiveClock(remaining);
                _loaded = ctx.Carrier?.Held;
                // Timed state can precede equipment during recovery. Bind only the
                // original dealt shoe, never an arbitrary later replacement.
                _joiningSeat = _loaded == null ? ctx.Motor.PlayerSlot : -1;
                _kit.IsOverchargeThrowActive = true; _kit.RefreshChargeVisual(ctx);
            }
            protected override void OnTick(AbilityContext ctx, float dt)
            {
                if (!_kit.IsOverchargeThrowActive) { EndEarly(ctx); return; }
                if (_loaded == null && _joiningSeat >= 0)
                {
                    var held = ctx.Carrier?.Held;
                    if (held == null) return;
                    if (held.SeatOfOrigin != _joiningSeat) { EndEarly(ctx); return; }
                    _loaded = held; _joiningSeat = -1;
                }
                if (_loaded == null || ctx.Carrier?.Held != _loaded) { EndEarly(ctx); return; }
                _kit.RefreshChargeVisual(ctx);
            }
            protected override void OnEnd(AbilityContext ctx)
            { _kit.IsOverchargeThrowActive = false; _loaded = null; _joiningSeat = -1; }
        }

        private sealed class ThunderstrikeOverdriveAbility : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.SharedUltimate;
            private readonly ZackHeroKit _kit;

            public override bool IsPersistentActive => _kit.IsOverclocked;
            public ThunderstrikeOverdriveAbility(ZackHeroKit kit)
                : base("zack_ultimate", "OVERCLOCK",
                       "Strike yourself with lightning, inflicting Zapped nearby and becoming Overclocked for the rest of the match.",
                       0, 0, AbilityGlyph.ZackThunderstrike,
                       summary: "Zap nearby rivals. Overclock lasts for the match.",
                       telegraphRadius: 4.5f, telegraphRange: 0,
                       castAction: "hero-zack-summon", viewmodelAction: "summon-lightning", castCue: "sfx_cast_zack_summon")
            {
                _kit = kit; Windup = UltimateWindup;
                SupportsPendingSnapshot = true; SupportsPermanentSnapshot = true;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                // ⚠️ THE STRIKE LANDS ON THE RING, THE GRUNT COMES FROM ZACK. They are different
                // places now, and `AudioDirector` parks a pooled voice at the point it is given:
                // a thunderclap fired at the caster while the lightning hits seven metres away
                // is the fault `LrtTrainFlyby` records about a moving train.
                _kit._joinThunderSettled = true;
                Vector3 at = ctx.Position;
                _kit.RestoreOverclock();

                NetCue.Play("hero_zack_ult", ctx.Position);
                NetCue.Play("sfx_lightning_strike", at);
                HeroHazards.CreateThunderstrike(at, 4.5f, ctx.Motor.PlayerSlot, applyGameplay: false);
                if (NetAuthority.ShouldResolve() && GameServices.Round != null)
                    foreach (var body in GameServices.Round.Bodies)
                    {
                        if (body == null || body == ctx.Motor || !body.gameObject.activeInHierarchy) continue;
                        var difference = body.transform.position - at; difference.y = 0;
                        if (difference.sqrMagnitude <= 4.5f * 4.5f) body.ApplyZapped();
                    }
                Visual.AbilityVfx.SpawnElectricArcs(at, 4.5f);
                _kit.RefreshChargeVisual(ctx);

                var squash = ctx.Motor.GetComponent<CharacterSquashStretch>();
                if (squash != null) squash.Stretch(0.05f);
            }

        }
    }
}
