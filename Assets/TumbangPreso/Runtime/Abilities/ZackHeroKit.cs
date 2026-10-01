using System;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    public sealed partial class ZackHeroKit : HeroKit, ITimedKitReplication, IWorldEffectBinding
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
                if (!CircuitSequenceActive) DefendingSkill?.ReduceCooldown(seconds);
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
        public HeroMovementState CaptureMovementState()=>((QuickCircuitAbility)Skill1).CaptureMovement();
        public bool RestoreJoiningMovement(CharacterMotor motor,HeroMovementState state,float age)
            => motor!=null && ((QuickCircuitAbility)Skill1).RestoreMovement(
                new AbilityContext(motor,motor.GetComponent<Carrier>(),motor.GetComponent<CombatVerbs>()),state,age);
        public void AdoptMovementFields(int owner) { }
        public void RebindWorldEffects(CharacterMotor motor)
        { if (motor != null) AdoptMovementFields(motor.PlayerSlot); }

        public ZackHeroKit() : base("zack", "ZACK")
        {
            Skill1 = new QuickCircuitAbility(this);
            // Role-specific authored Electro jobs.
            AttackingSkill = new BankShotAbility(this);
            DefendingSkill = new ClosedCircuitAbility(this);
            Ultimate = new ThunderstrikeOverdriveAbility(this);
        }

        // Current human Wiki anchor. New basic-mode migration remains separate.
        public override float UltimateCost => 15;

        private void RefreshChargeVisual(AbilityContext ctx)
        {
            var shoe = ctx.Carrier != null ? ctx.Carrier.Held : null;
            if (shoe != null) ZackMagnetCharge.Ensure(shoe.GetComponentInChildren<MeshFilter>(), shoe, this);
        }

        private sealed class QuickCircuitAbility : HeroAbility
        {
            public const float CutDistance = 2, TellSeconds = .15f, RecoverySeconds = .2f, FollowupSeconds = 1;
            private static readonly float CutSpeed = Mathf.Sqrt(2 * Balance.Friction * CutDistance);
            private static readonly float RecoverWindow = CutSpeed / Balance.Friction + RecoverySeconds;
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.Predicted;
            // Capability stays true on an observer whose first window expired;
            // approved follow-up events still play, while local readiness is gated.
            public override bool CanReactivate => true;
            public override bool ReactivateReady => _followup && !IsWindingUp && DurationRemaining <= FollowupSeconds - RecoverWindow;
            public override float ReactivateReadyIn => _followup ? Mathf.Max(0, DurationRemaining - (FollowupSeconds - RecoverWindow)) : 0;
            private readonly ZackHeroKit _kit;
            private bool _followup, _secondCut, _movementKnown, _movementRestored;
            public QuickCircuitAbility(ZackHeroKit kit)
                : base("zack_skill1", "QUICK CIRCUIT",
                       "Cut two metres sideways after a brief tell. Overclock allows one optional second cut within one second. No stun trail or invulnerability.",
                       30, FollowupSeconds, AbilityGlyph.ZackSprint,
                       summary: "Cut sideways; Overclock offers one second cut.",
                       castAction: "hero-zack-sprint", viewmodelAction: "sprint-electric", castCue: "sfx_cast_zack_sprint")
            { _kit = kit; Windup = TellSeconds; SupportsPendingSnapshot = true; }
            public override AbilityContext CaptureLocalCastContext(AbilityContext ctx)
            {
                if (ctx?.Motor == null) return ctx;
                var right = Vector3.Cross(Vector3.up, ctx.Forward).normalized;
                float side = ctx.Motor.Intent.MoveAxis.x < -.1f ? -1 : 1;
                return new AbilityContext(ctx.Motor, ctx.Carrier, ctx.Verbs, ctx.Position, ctx.Forward, ctx.Position + right * side * CutDistance);
            }
            public override bool CanActivate(AbilityContext ctx) => base.CanActivate(ctx) && !IsActive && !ctx.Motor.IsFlying;
            public override void Activate(AbilityContext ctx)
            { _secondCut = false; _followup = false; _movementKnown = true; _movementRestored = false; base.Activate(ctx); }
            public override void Reactivate(AbilityContext ctx)
            {
                if (ctx?.Motor == null || (!ctx.IsApprovedReplay && !ReactivateReady)) return;
                float cooldown = CooldownRemaining;
                _secondCut = true; _followup = false; _movementKnown = true; _movementRestored = false;
                base.Activate(ctx); CooldownRemaining = cooldown;
            }
            protected override void OnActivate(AbilityContext ctx)
            {
                if (ctx?.Motor == null) return;
                var right = Vector3.Cross(Vector3.up, ctx.Forward).normalized;
                float side = Vector3.Dot(ctx.AimPoint - ctx.Position, right) < 0 ? -1 : 1;
                ctx.Motor.ApplyImpulse(right * side * CutSpeed);
                ctx.Motor.Commit(RecoverWindow);
                _followup = !_secondCut && _kit.IsOverclocked;
                DurationRemaining = _followup ? FollowupSeconds : RecoverWindow;
            }
            public HeroMovementState CaptureMovement()
                => new HeroMovementState { Remaining = DurationRemaining, Wake = Array.Empty<Vector3>(),
                    QuickFollowup = _followup, QuickSecondCut = _secondCut };
            public bool RestoreMovement(AbilityContext ctx, HeroMovementState state, float age)
            {
                if (!state.Valid(Duration, .30f, age) || state.UntilNextEmission != 0 || (state.Wake?.Length ?? 0) != 0
                    || (state.QuickFollowup && (state.Remaining <= 0 || state.QuickSecondCut))
                    || (_movementKnown && (!_movementRestored || !IsActive))) return false;
                float remaining = Mathf.Max(0, state.Remaining - age);
                if (_movementKnown) remaining = Mathf.Min(remaining, DurationRemaining);
                _movementKnown = true; _movementRestored = remaining > 0;
                _secondCut = state.QuickSecondCut;
                _followup = state.QuickFollowup && remaining > 0;
                if (remaining <= 0) { EndEarly(ctx); return true; }
                RestoreLiveClock(remaining);
                // Motion already belongs to the pose snapshot. Never relaunch.
                return true;
            }
            protected override void OnEnd(AbilityContext ctx) { _followup = false; _secondCut = false; }
            protected override void OnCancelled(AbilityContext ctx) => OnEnd(ctx);
            public override void Reset()
            { base.Reset(); _followup = _secondCut = _movementKnown = _movementRestored = false; }
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
