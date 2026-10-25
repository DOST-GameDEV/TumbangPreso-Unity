using System;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// ZACK, THE SPEEDSTER (owner's V3 skill sheet on the Wiki, approved 2026-10-25; `docs/reports/zack-kit-2026-10-25/plan.md`).
    ///
    /// | Slot | Name | Owner's table |
    /// |---|---|---|
    /// | Passive | MOMENTUM | The longer Zack keeps running, the faster he gets, up to 20% faster after 3 seconds. Stopping, throwing or being hit resets it. |
    /// | Signature | BACKTRACK | Snap back to where you were 3 seconds ago, keeping whatever you are holding. 30 s. |
    /// | Attacking | AFTERIMAGE | Send an electric copy of yourself running straight ahead for 3 seconds. It vanishes if the taya touches it. 30 s. |
    /// | Defending | LIVE WIRE | Hold to aim, release to string a sparking wire between two points up to 6 metres apart. The first attacker to cross it is Zapped. Lasts 10 seconds. 30 s. |
    /// | Ultimate | OVERCLOCK | Zack becomes Overclocked for 25 seconds. 15 points. |
    ///
    /// Every number is in `Core.ZackRules`. Stable ability ids are the old kit's (`zack_skill1`, `zack_skill2`, `zack_skill2d`,
    /// `zack_ultimate`) so saves, mastery, loadouts and the HUD's slots hold; what they do is new.
    ///
    /// ⚠️ HOST AUTHORITY. Only the host Zaps (`ZackLiveWire`), pops a copy (`ZackAfterimage`) or accepts a Backtrack spot. The
    /// owner moves only its own body (the Backtrack snap, on the existing predicted-teleport route, and Momentum's speed).
    /// OVERCLOCKED runs one 25 s clock on every peer from the accepted ultimate, as Dante's MOUNTAIN FORM does; a joiner gets
    /// the time left in the timed-kit snapshot's passive fields.
    /// </summary>
    public sealed class ZackHeroKit : HeroKit, ITimedKitReplication, IWorldEffectBinding
    {
        public ZackHeroKit() : base("zack", "ZACK")
        {
            Skill1 = new BacktrackAbility(this);
            AttackingSkill = new AfterimageAbility(this);
            DefendingSkill = new LiveWireAbility(this);
            Ultimate = new OverclockAbility(this);
        }

        public override float UltimateCost => ZackRules.OverclockCost;

        // ================================================================== OVERCLOCKED (the ultimate's state)

        /// <summary>Seconds of OVERCLOCKED left, 0 when he is not.</summary>
        public float OverclockLeft { get; private set; }
        public bool IsOverclocked => OverclockLeft > 0.0f;

        /// <summary>The meter does not charge while he is Overclocked; it charges normally again once it ends (owner's sheet).</summary>
        public override bool CanGainUltimateCharge => !IsOverclocked;

        /// <summary>The ultimate tile counts the 25 s down (`TumpPowerReadout.OwnerDeck`).</summary>
        public override bool UltimateAftermath(out float left, out float total)
        { left = OverclockLeft; total = ZackRules.OverclockSeconds; return IsOverclocked; }

        private ZackOverclockLook _overclockLook;

        /// <summary>Overclock begins: from the end of his cutscene (the bolt is already in him), or from nothing in a test.</summary>
        public void BeginOverclock(CharacterMotor motor, float left = ZackRules.OverclockSeconds)
        {
            OverclockLeft = Mathf.Clamp(left, 0.0f, ZackRules.OverclockSeconds);
            UltimateCharge = 0.0f;
            // "Backtrack recharges in 3 seconds": a Backtrack already cooling is pulled down to the Overclocked length at once.
            ((BacktrackAbility)Skill1).ClampToOverclock();
            DressOverclock(motor);
        }

        private void DressOverclock(CharacterMotor motor)
        {
            if (motor == null) return;
            if (_overclockLook == null || !_overclockLook.BelongsTo(motor)) _overclockLook = ZackOverclockLook.Attach(motor, this);
        }

        private void EndOverclock()
        {
            OverclockLeft = 0.0f;
            if (_overclockLook != null) _overclockLook.Release();
            _overclockLook = null;
        }

        // ================================================================== MOMENTUM (passive)

        private float _momentum, _stoppedFor;
        private int _seenImpact = -1, _seenEpoch = -1;
        private Slipper _seenHeld;
        private CharacterMotor _statusBody;
        private ZackMomentumLook _momentumLook;
        private ZackBacktrackEcho _backtrackEcho;

        /// <summary>Seconds of uninterrupted running Momentum has counted (0 to 3). Real on the peer that moves him.</summary>
        public float MomentumSeconds => _momentum;

        /// <summary>Momentum times Overclocked, multiplied into the motor's speed (`CharacterMotor` reads it every step).</summary>
        public override float MovementSpeedScale => ZackRules.SpeedScale(_momentum, IsOverclocked);

        /// <summary>A stop, a throw, a hit or a snap: Momentum starts again from nothing.</summary>
        public void ResetMomentum() { _momentum = 0.0f; _stoppedFor = 0.0f; }

        private void OnStatusGained(CharacterMotor body, StatusKind kind)
        {
            // Being hit resets it: every negative status lands from somebody else's power or the taya's tag.
            if (body == _statusBody) ResetMomentum();
        }

        /// <summary>
        /// The passive, on the peer that simulates his body only (his own screen, the host for a bot). Running is grounded, steering,
        /// at least `MomentumRunSpeed` on the court; anything that stops him or lands on him starts it over.
        /// </summary>
        private void StepMomentum(CharacterMotor motor, float dt)
        {
            if (motor == null || !motor.IsLocallySimulated() || motor.Mode != GameMode.HeroStrike
                || GameServices.Round?.RoundActive != true || PresentationClock.BlocksInput)
            { if (motor == null || GameServices.Round?.RoundActive != true) ResetMomentum(); return; }
            if (_statusBody != motor)
            {
                if (_statusBody != null) _statusBody.StatusGained -= OnStatusGained;
                _statusBody = motor; _statusBody.StatusGained += OnStatusGained;
                _seenImpact = motor.ImpactSerial; _seenEpoch = motor.MovementEpoch;
            }
            bool hit = motor.ImpactSerial != _seenImpact;
            _seenImpact = motor.ImpactSerial;
            bool snapped = motor.MovementEpoch != _seenEpoch;
            _seenEpoch = motor.MovementEpoch;
            // A throw resets it: the slipper he held is now flying, thrown by him.
            var held = motor.GetComponent<Carrier>()?.Held;
            bool threw = _seenHeld != null && held == null && _seenHeld.State == SlipperState.InFlight
                         && _seenHeld.ThrowerSlot == motor.PlayerSlot;
            _seenHeld = held;
            if (hit || snapped || threw || motor.IsStunned || motor.IsTripped || motor.IsRooted || motor.IsCarryingCan
                || motor.IsSwimming || motor.IsFlying)
            { ResetMomentum(); return; }
            var flat = motor.Velocity; flat.y = 0.0f;
            bool running = motor.IsGrounded && motor.Intent.MoveAxis.sqrMagnitude > 0.01f
                           && flat.magnitude >= ZackRules.MomentumRunSpeed;
            _momentum = ZackRules.StepMomentum(_momentum, ref _stoppedFor, running, dt);
        }

        // ================================================================== THE 3 s PATH (Backtrack's history)

        private readonly Vector3[] _pathPoints = new Vector3[Mathf.CeilToInt(ZackRules.BacktrackHistoryKeep / ZackRules.BacktrackSampleInterval) + 2];
        private readonly float[] _pathTimes = new float[Mathf.CeilToInt(ZackRules.BacktrackHistoryKeep / ZackRules.BacktrackSampleInterval) + 2];
        private int _pathCount, _pathHead;
        private float _pathClock, _pathNextSample;

        /// <summary>This peer's own clock for the ring: it runs with the kit's tick, so a paused world adds nothing to it.</summary>
        public float PathClock => _pathClock;

        private void SamplePath(CharacterMotor motor, float dt)
        {
            if (motor == null || GameServices.Round?.RoundActive != true) { ClearPath(); return; }
            if (!(dt > 0.0f)) return;
            _pathClock += dt;
            if (_pathClock < _pathNextSample) return;
            _pathNextSample = _pathClock + ZackRules.BacktrackSampleInterval;
            _pathHead = (_pathHead + 1) % _pathPoints.Length;
            _pathPoints[_pathHead] = motor.transform.position;
            _pathTimes[_pathHead] = _pathClock;
            _pathCount = Mathf.Min(_pathCount + 1, _pathPoints.Length);
        }

        private void ClearPath() { _pathCount = 0; _pathHead = 0; _pathClock = 0.0f; _pathNextSample = 0.0f; }

        /// <summary>How many seconds of path this peer holds.</summary>
        public float PathSeconds => _pathCount < 2 ? 0.0f : _pathClock - _pathTimes[Oldest];

        private int Oldest => (_pathHead - _pathCount + 1 + _pathPoints.Length) % _pathPoints.Length;

        /// <summary>Where the path was <paramref name="secondsAgo"/> ago, interpolated between samples.</summary>
        public bool PathPointAgo(float secondsAgo, out Vector3 point)
        {
            point = Vector3.zero;
            if (_pathCount < 1) return false;
            float target = _pathClock - secondsAgo;
            int index = _pathHead;
            for (int n = 0; n < _pathCount; n++)
            {
                int i = (_pathHead - n + _pathPoints.Length) % _pathPoints.Length;
                if (_pathTimes[i] <= target)
                {
                    int newer = (i + 1) % _pathPoints.Length;
                    if (n == 0) { point = _pathPoints[i]; return true; }
                    float span = _pathTimes[newer] - _pathTimes[i];
                    float u = span > 1e-5f ? Mathf.Clamp01((target - _pathTimes[i]) / span) : 0.0f;
                    point = Vector3.Lerp(_pathPoints[i], _pathPoints[newer], u);
                    return true;
                }
                index = i;
            }
            point = _pathPoints[index];
            return true;
        }

        /// <summary>The path from <paramref name="secondsAgo"/> to now, oldest first, for the rewind's drawing.</summary>
        public Vector3[] PathSince(float secondsAgo)
        {
            if (_pathCount < 1) return Array.Empty<Vector3>();
            float from = _pathClock - secondsAgo;
            var list = new System.Collections.Generic.List<Vector3>(_pathCount);
            for (int n = _pathCount - 1; n >= 0; n--)
            {
                int i = (_pathHead - n + _pathPoints.Length) % _pathPoints.Length;
                if (_pathTimes[i] >= from) list.Add(_pathPoints[i]);
            }
            return list.ToArray();
        }

        /// <summary>
        /// The spot Backtrack goes to on this peer: 3 s ago on its own path, or the round's oldest point when there is less (at
        /// least 0.5 s). False when there is not enough history or the spot is too close to bother.
        /// </summary>
        public bool TryBacktrackPoint(CharacterMotor motor, out Vector3 point)
        {
            point = Vector3.zero;
            float back = ZackRules.BacktrackLookBack(PathSeconds);
            if (motor == null || back < 0.0f || !PathPointAgo(back, out point)) return false;
            var flat = point - motor.transform.position; flat.y = 0.0f;
            return flat.magnitude >= ZackRules.BacktrackMinDistance;
        }

        /// <summary>
        /// ⚠️ THE HOST'S CHECK OF A SPOT THE OWNER CHOSE: within `BacktrackTolerance` of where THIS peer saw the body anywhere from
        /// 2.5 to 3.5 s ago (or from its oldest point if it holds less). The owner's own ring passes it trivially; a remote
        /// claim passes it only if this peer saw the body near there.
        /// </summary>
        public bool AcceptsBacktrackPoint(CharacterMotor motor, Vector3 point)
        {
            if (motor == null || !float.IsFinite(point.x) || !float.IsFinite(point.y) || !float.IsFinite(point.z)) return false;
            if (_pathCount < 1 || PathSeconds < ZackRules.BacktrackMinHistory) return false;
            if (point.x < AIController.PlayableMinX || point.x > AIController.PlayableMaxX
                || point.z < AIController.PlayableMinZ || point.z > AIController.PlayableMaxZ) return false;
            float floor = Slipper.FindGroundY(point, 0.5f);
            if (Mathf.Abs(point.y - floor) > 0.3f) return false;
            float newest = ZackRules.BacktrackWindowEarly, oldest = ZackRules.BacktrackWindowLate;
            float held = PathSeconds;
            if (held < oldest) { oldest = held; newest = Mathf.Min(newest, Mathf.Max(0.0f, held - 1.0f)); }
            float best = float.PositiveInfinity;
            for (int n = 0; n < _pathCount; n++)
            {
                int i = (_pathHead - n + _pathPoints.Length) % _pathPoints.Length;
                float age = _pathClock - _pathTimes[i];
                if (age < newest - .05f || age > oldest + .05f) continue;
                var d = _pathPoints[i] - point; d.y = 0.0f;
                if (d.sqrMagnitude > ZackRules.BacktrackTolerance * ZackRules.BacktrackTolerance
                    || Mathf.Abs(_pathPoints[i].y - point.y) > 0.6f) continue;
                float distance = d.magnitude;
                if (distance > 0.02f && RafiWaterField.ClearDistance(point + Vector3.up * 0.9f,
                    d / distance, distance) < distance - 0.02f) continue;
                best = Mathf.Min(best, d.sqrMagnitude);
            }
            var away = point - motor.transform.position; away.y = 0.0f;
            return best <= ZackRules.BacktrackTolerance * ZackRules.BacktrackTolerance
                   && away.magnitude >= ZackRules.BacktrackMinDistance - .05f;
        }

        // ================================================================== the kit's clock

        public override void Tick(AbilityContext ctx, float dt)
        {
            base.Tick(ctx, dt);
            var motor = ctx?.Motor;
            if (OverclockLeft > 0.0f)
            {
                if (float.IsFinite(dt) && dt > 0.0f) OverclockLeft = Mathf.Max(0.0f, OverclockLeft - dt);
                if (OverclockLeft <= 0.0f) EndOverclock();
                else DressOverclock(motor);
            }
            SamplePath(motor, dt);
            StepMomentum(motor, dt);
            if (motor != null && (_momentumLook == null || !_momentumLook.BelongsTo(motor)))
                _momentumLook = ZackMomentumLook.Attach(motor, this);
            // Only his own screen draws the echo (`ZackBacktrackEcho` checks); attaching it everywhere keeps one rule.
            if (motor != null && (_backtrackEcho == null || !_backtrackEcho.BelongsTo(motor)))
                _backtrackEcho = ZackBacktrackEcho.Attach(motor, this);
        }

        public override void ResetForRound(AbilityContext ctx)
        {
            if (_statusBody != null) _statusBody.StatusGained -= OnStatusGained;
            _statusBody = null;
            EndOverclock();
            ResetMomentum();
            ClearPath();
            _seenHeld = null;
            ZackLiveWire.RetireOwnedBy(ctx?.Motor != null ? ctx.Motor.PlayerSlot : -1);
            base.ResetForRound(ctx);
        }

        public override void Reset()
        {
            if (_statusBody != null) _statusBody.StatusGained -= OnStatusGained;
            _statusBody = null;
            EndOverclock();
            ResetMomentum();
            ClearPath();
            _seenHeld = null;
            base.Reset();
        }

        // ================================================================== recovery

        public TimedKitSnapshot CaptureTimedKit()
            => new TimedKitSnapshot(null, 0, Ultimate, 0, Ultimate.IsWindingUp,
                passiveRemaining: OverclockLeft, passiveCapacity: ZackRules.OverclockSeconds);

        public bool RestoreTimedKit(CharacterMotor motor, TimedKitSnapshot state)
        {
            if (motor == null) return false;
            float left = Mathf.Clamp(state.PassiveRemaining, 0.0f, ZackRules.OverclockSeconds);
            if (Mathf.Abs(left - OverclockLeft) < .05f) return false;
            if (left <= 0.0f) { EndOverclock(); return true; }
            // A joiner sees him Overclocked as he is now; the meter stays empty while it runs.
            OverclockLeft = left; UltimateCharge = 0.0f;
            DressOverclock(motor);
            return true;
        }

        /// <summary>The wire and the copies are host-owned fields: a world snapshot replaces them without the kit's help.</summary>
        public void RebindWorldEffects(CharacterMotor motor) { }

        // ================================================================== BACKTRACK (signature)

        private sealed class BacktrackAbility : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.Predicted;
            private readonly ZackHeroKit _kit;

            public BacktrackAbility(ZackHeroKit kit)
                : base("zack_skill1", "BACKTRACK",
                       "Snap back to where you were 3 seconds ago, keeping whatever you are holding.",
                       ZackRules.BacktrackCooldown, 0.0f, AbilityGlyph.ZackBacktrack,
                       summary: "Snap back 3 seconds, slipper and all.",
                       castAction: "hero-zack-backtrack", viewmodelAction: "backtrack-snap",
                       castCue: "sfx_cast_zack_backtrack")
            { _kit = kit; }

            /// <summary>The owner picks the spot from its own path; the accepted cast carries it to the host and every peer.</summary>
            public override AbilityContext CaptureLocalCastContext(AbilityContext ctx)
            {
                if (ctx?.Motor == null || !_kit.TryBacktrackPoint(ctx.Motor, out var point)) return ctx;
                return new AbilityContext(ctx.Motor, ctx.Carrier, ctx.Verbs, ctx.Position, ctx.Forward, point);
            }

            public override bool CanActivate(AbilityContext ctx)
            {
                if (!base.CanActivate(ctx)) return false;
                var m = ctx.Motor;
                if (m.IsFlying || m.IsRooted || m.IsCarryingCan || m.IsSwimming || m.IsEdgeRecovering) return false;
                return _kit.AcceptsBacktrackPoint(m, ctx.AimPoint);
            }

            public override void Activate(AbilityContext ctx)
            {
                base.Activate(ctx);
                ClampToOverclock();
            }

            public void ClampToOverclock()
            {
                if (_kit.IsOverclocked && CooldownRemaining > ZackRules.BacktrackOverclockedCooldown)
                    CooldownRemaining = ZackRules.BacktrackOverclockedCooldown;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                if (ctx?.Motor == null) return;
                var motor = ctx.Motor;
                Vector3 from = ctx.Position, to = ctx.AimPoint;
                // The rewind is drawn along the path this peer saw him run, played backwards; it decides nothing.
                var path = _kit.PathSince(ZackRules.BacktrackSeconds + .1f);
                var pull = ZackBacktrackLook.Play(motor, from, to, path);
                // His own body: the owner predicts it and the host replays it (`CharacterMotor.Teleport` refuses elsewhere). He
                // keeps the slipper in hand, his statuses and his facing.
                motor.Teleport(to, keepStatuses: true);
                pull?.SealTeleport();
                _kit.ResetMomentum();
                NetCue.Play("sfx_zack_backtrack_land", to);
            }
        }

        // ================================================================== AFTERIMAGE (attacking)

        private sealed class AfterimageAbility : HeroAbility
        {
            // ⚠️ THE COPIES ARE WORLD OBJECTS, SO THE OWNER WAITS FOR THE HOST before they exist (Whirlwind's and Water Wall's
            // reason): a refused cast must not leave a Zack running across one screen that exists nowhere else.
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.HostConfirmed;
            private readonly ZackHeroKit _kit;

            public AfterimageAbility(ZackHeroKit kit)
                : base("zack_skill2", "AFTERIMAGE",
                       "Attacking. Send an electric copy of yourself running straight ahead for 3 seconds. It vanishes if the taya touches it.",
                       ZackRules.AfterimageCooldown, 0.0f, AbilityGlyph.ZackAfterimage,
                       summary: "A copy of you runs straight ahead. The taya pops it on touch.",
                       castAction: "hero-zack-afterimage", viewmodelAction: "afterimage-split",
                       castCue: "sfx_cast_zack_afterimage")
            { _kit = kit; }

            public override bool CanActivate(AbilityContext ctx)
                => base.CanActivate(ctx) && !ctx.Motor.IsDefender && !ctx.Motor.IsCarryingCan && !ctx.Motor.IsSwimming;

            protected override void OnActivate(AbilityContext ctx)
            {
                if (ctx?.Motor == null) return;
                // Along his flat aim (plan: "facing his flat aim"), falling back to his facing with no aim or an aim at his feet.
                var run = ctx.HasAimPoint ? ctx.AimPoint - ctx.Position : ctx.Forward;
                run.y = 0.0f;
                if (run.sqrMagnitude < 0.25f) run = ctx.Forward;
                if (NetAuthority.ShouldResolve())
                    ZackAfterimage.Cast(ctx.Motor, ctx.Position, run,
                        ZackRules.AfterimageCopies(_kit.IsOverclocked), ctx.Carrier?.Held != null);
            }
        }

        // ================================================================== LIVE WIRE (defending)

        private sealed class LiveWireAbility : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.HostConfirmed;
            private readonly ZackHeroKit _kit;

            public LiveWireAbility(ZackHeroKit kit)
                : base("zack_skill2d", "LIVE WIRE",
                       "Defending. Hold to aim, release to string a sparking wire between two points up to 6 metres apart. The first attacker to cross it is Zapped. Lasts 10 seconds.",
                       ZackRules.LiveWireCooldown, 0.0f, AbilityGlyph.ZackLiveWire,
                       summary: "String a live wire. The first attacker across it is Zapped.",
                       telegraphRadius: .35f, telegraphRange: ZackRules.LiveWireMaxLength,
                       castAction: "hero-zack-wire", viewmodelAction: "livewire-throw",
                       castCue: "sfx_cast_zack_livewire")
            {
                _kit = kit;
                // Release only: the reach grows to 6 m over 0.55 s and stops; holding longer buys nothing.
                AimByHolding(ZackRules.LiveWireMinLength, ZackRules.LiveWireMaxLength, rampSeconds: .55f, maxHoldSeconds: 0.0f,
                             whereLooking: true);
                AimPoseAction = "hero-zack-wire-aim";
            }

            public override bool DrawsOwnAim => true;
            private ZackWireAim _aim;

            public override void PresentAim(CharacterMotor caster, Vector3 at, float heldSeconds)
            {
                if (_aim == null) _aim = ZackWireAim.Create();
                _aim.Show(caster, at, ZackLiveWire.CanPlace(caster, caster.transform.position, at), _kit.IsOverclocked);
            }

            public override void EndAim()
            {
                if (_aim != null) _aim.Release();
                _aim = null;
            }

            public override bool CanActivate(AbilityContext ctx)
                => base.CanActivate(ctx) && ctx.Motor.IsDefender
                   && ZackLiveWire.CanPlace(ctx.Motor, ctx.Position, AimedDestination(ctx));

            protected override void OnActivate(AbilityContext ctx)
            {
                if (ctx?.Motor == null) return;
                if (NetAuthority.ShouldResolve())
                    ZackLiveWire.Cast(ctx.Motor, ctx.Position, AimedDestination(ctx), _kit.IsOverclocked);
            }
        }

        // ================================================================== OVERCLOCK (ultimate)

        private sealed class OverclockAbility : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.SharedUltimate;
            private readonly ZackHeroKit _kit;

            public OverclockAbility(ZackHeroKit kit)
                : base("zack_ultimate", "OVERCLOCK",
                       "Zack becomes Overclocked for 25 seconds.",
                       0.0f, 0.0f, AbilityGlyph.ZackOverclock,
                       summary: "25 s faster, and every skill overclocked.",
                       castAction: "hero-zack-overclock", viewmodelAction: "overclock-take", castCue: "")
            {
                _kit = kit;
                Windup = UltimateWindup;
                SupportsPendingSnapshot = true;
            }

            // The cutscene IS the charge: the bolt goes into him and play hands back with him already Overclocked.
            protected override bool IntroductionIsTheWindup => true;

            public override bool CanActivate(AbilityContext ctx) => base.CanActivate(ctx) && !_kit.IsOverclocked;

            protected override void OnActivate(AbilityContext ctx)
            {
                if (ctx?.Motor == null) return;
                NetCue.Play("hero_zack_ult", ctx.Position);
                _kit.BeginOverclock(ctx.Motor);
                ctx.Motor.GetComponent<CharacterSquashStretch>()?.Stretch(0.08f);
            }
        }
    }
}
