using System;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// ZACK, THE SPEEDSTER (owner's V3 skill sheet on the Wiki, approved 2026-10-25, refined from the owner's 2026-10-26
    /// feedback; `docs/reports/zack-kit-2026-10-25/plan.md` and the V4 note on the Wiki's Zack Kit Rework tab).
    ///
    /// | Slot | Name | Wiki text |
    /// |---|---|---|
    /// | Passive | MOMENTUM | Zack runs 8% faster than other heroes and sprints on Charge, his own energy bar. The longer he keeps running, the faster he gets: up to 25% faster after 2 seconds, in three visible stages. |
    /// | Signature | BACKTRACK | Snap back to where you were 3 seconds ago, keeping whatever you are holding. Your past moments flash up and merge into you. 30 s. |
    /// | Attacking | FULL THROTTLE | Instantly full Momentum and full Charge; for 3 seconds Momentum cannot be lost. A hit, stun, trip or root still stops you. 24 s (Overclocked: 8 s). |
    /// | Defending | LIVE WIRE | String a wire from your feet to where you aim. It Zaps a crosser, goes dark 5 s, stays until the round ends; look at it and hold to reel it in. 20 s. |
    /// | Ultimate | OVERCLOCK | Overclocked on 25 s of fuel; hold the ultimate to zap a rival with both hands, burning fuel three times as fast. 15 points. |
    ///
    /// Every number is in `Core.ZackRules`. Stable ability ids are the old kit's (`zack_skill1`, `zack_skill2`, `zack_skill2d`,
    /// `zack_ultimate`) so saves, mastery, loadouts and the HUD's slots hold; what they do is new.
    ///
    /// FULL THROTTLE replaced AFTERIMAGE on 2026-10-26 (owner: *"i approve full throttle"*); `ZackAfterimage` remains only to
    /// restore and draw copies in older replay clips.
    ///
    /// ⚠️ HOST AUTHORITY. Only the host Zaps (`ZackLiveWire`) or accepts a Backtrack spot. The
    /// owner moves only its own body (the Backtrack snap, on the existing predicted-teleport route, and Momentum's speed).
    /// OVERCLOCKED runs one 25 s clock on every peer from the accepted ultimate, as Dante's MOUNTAIN FORM does; a joiner gets
    /// the time left in the timed-kit snapshot's passive fields.
    /// </summary>
    public sealed class ZackHeroKit : HeroKit, ITimedKitReplication, IWorldEffectBinding
    {
        public ZackHeroKit() : base("zack", "ZACK")
        {
            Skill1 = new BacktrackAbility(this);
            AttackingSkill = new FullThrottleAbility(this);
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

        /// <summary>His Charge: a longer sprint and a quicker refill than the shared bar, free while Overclocked.</summary>
        public override StaminaProfile StaminaProfile => ZackRules.ChargeProfile(IsOverclocked);

        /// <summary>He walks while he channels the zap.</summary>
        public override bool BlocksSprint => Zapping;

        private ZackOverclockLook _overclockLook;

        /// <summary>Overclock begins: from the end of his cutscene (the bolt is already in him), or from nothing in a test.</summary>
        public void BeginOverclock(CharacterMotor motor, float left = ZackRules.OverclockSeconds)
        {
            OverclockLeft = Mathf.Clamp(left, 0.0f, ZackRules.OverclockSeconds);
            UltimateCharge = 0.0f;
            // The bolt fills him: Charge is full (and any fatigue gone) the moment the fuel takes over the bar.
            motor?.Stamina?.RefillAndClearFatigue();
            // "Backtrack recharges in 3 seconds": a Backtrack already cooling is pulled down to the Overclocked length at once.
            ((BacktrackAbility)Skill1).ClampToOverclock();
            ((FullThrottleAbility)AttackingSkill).ClampToOverclock();
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
            StopZap();
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
        public override float MovementSpeedScale
            => ZackRules.SpeedScale(_momentum, IsOverclocked) * (Zapping ? ZackRules.ZapMoveScale : 1.0f);

        /// <summary>A stop, a throw, a hit or a snap: Momentum starts again from nothing.</summary>
        public void ResetMomentum() { _momentum = 0.0f; _stoppedFor = 0.0f; }

        /// <summary>A stop, a throw or a snap: it costs Momentum unless Full Throttle has locked it.</summary>
        private void SoftResetMomentum() { if (!Throttled) ResetMomentum(); }

        /// <summary>A hit, a stun, a trip, a root or a tag: Momentum goes and Full Throttle's lock breaks with it.</summary>
        private void HardResetMomentum(CharacterMotor motor)
        {
            if (Throttled) AttackingSkill.EndEarly(null);
            ResetMomentum();
        }

        private void OnStatusGained(CharacterMotor body, StatusKind kind)
        {
            // Being hit resets it: every negative status lands from somebody else's power or the taya's tag.
            if (body == _statusBody) HardResetMomentum(body);
        }

        // ================================================================== FULL THROTTLE's lock

        private ZackThrottleLook _throttleLook;

        /// <summary>Full Throttle's lock is running: Momentum is full and nothing short of a hit takes it.</summary>
        public bool Throttled => AttackingSkill != null && AttackingSkill.IsActive;

        /// <summary>Seconds of the lock left (0 when not throttled), for the countdown ring and the HUD.</summary>
        public float ThrottleLeft => Throttled ? AttackingSkill.DurationRemaining : 0.0f;

        /// <summary>The cast, on every peer that accepts it: full Momentum, full Charge, and the surge on screen.</summary>
        private void BeginThrottle(CharacterMotor motor)
        {
            _momentum = ZackRules.MomentumRampSeconds; _stoppedFor = 0.0f;
            motor?.Stamina?.RefillAndClearFatigue();
            if (motor == null) return;
            if (_throttleLook == null || !_throttleLook.BelongsTo(motor)) _throttleLook = ZackThrottleLook.Attach(motor, this);
            _throttleLook?.Surge();
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
            if (hit || motor.IsStunned || motor.IsTripped || motor.IsRooted || motor.IsTagged || motor.IsCarryingCan
                || motor.IsSwimming || motor.IsFlying)
            { HardResetMomentum(motor); return; }
            // Full Throttle carries Momentum through a throw and a Backtrack; a stop it carries in `StepMomentum`.
            if ((snapped || threw) && !Throttled) { ResetMomentum(); return; }
            var flat = motor.Velocity; flat.y = 0.0f;
            bool running = motor.IsGrounded && motor.Intent.MoveAxis.sqrMagnitude > 0.01f
                           && flat.magnitude >= ZackRules.MomentumRunSpeed;
            _momentum = ZackRules.StepMomentum(_momentum, ref _stoppedFor, running, dt, Throttled);
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
            if (motor != null && NetAuthority.ShouldResolve()) HostStepZap(motor, dt);
            if (OverclockLeft > 0.0f)
            {
                // Overclock is fuel: it burns 1 per second on its own and faster while he holds the zap.
                if (float.IsFinite(dt) && dt > 0.0f) OverclockLeft = Mathf.Max(0.0f, OverclockLeft - ZackRules.FuelBurn(Zapping, ZapConnected) * dt);
                if (OverclockLeft <= 0.0f) { GameServices.Audio?.PlayAt("sfx_zack_overclock_end", motor != null ? motor.transform.position : Vector3.zero); EndOverclock(); }
                else DressOverclock(motor);
            }
            SamplePath(motor, dt);
            StepMomentum(motor, dt);
            if (motor != null && (_momentumLook == null || !_momentumLook.BelongsTo(motor)))
                _momentumLook = ZackMomentumLook.Attach(motor, this);
            // Only his own screen draws the echo (`ZackBacktrackEcho` checks); attaching it everywhere keeps one rule.
            if (motor != null && (_backtrackEcho == null || !_backtrackEcho.BelongsTo(motor)))
                _backtrackEcho = ZackBacktrackEcho.Attach(motor, this);
            if (motor != null && (_zapLook == null || !_zapLook.BelongsTo(motor)))
                _zapLook = ZackZapLook.Attach(motor, this);
            if (motor != null && (_throttleLook == null || !_throttleLook.BelongsTo(motor)))
                _throttleLook = ZackThrottleLook.Attach(motor, this);
            // His own screen marks the wire he could reel in right now (the highlight and the prompt read this).
            ReelAimedEvent = motor != null && ZackFx.IsOwnBody(motor) && motor.CanAct() ? (WireInReelReach(ctx)?.EventId ?? 0) : 0;
        }

        public override void ResetForRound(AbilityContext ctx)
        {
            if (_statusBody != null) _statusBody.StatusGained -= OnStatusGained;
            _statusBody = null;
            EndOverclock();
            ResetMomentum();
            ClearPath();
            _seenHeld = null;
            _reelEvent = 0;
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
            => new TimedKitSnapshot(AttackingSkill, ThrottleLeft, Ultimate, 0, Ultimate.IsWindingUp,
                passiveRemaining: OverclockLeft, passiveCapacity: ZackRules.OverclockSeconds);

        public bool RestoreTimedKit(CharacterMotor motor, TimedKitSnapshot state)
        {
            if (motor == null) return false;
            // A joiner sees Full Throttle's lock run out on the same clock (presentation: the owner's own peer counts Momentum).
            bool throttle = ((FullThrottleAbility)AttackingSkill).RestoreLock(state.PersonalRemaining);
            float left = Mathf.Clamp(state.PassiveRemaining, 0.0f, ZackRules.OverclockSeconds);
            if (Mathf.Abs(left - OverclockLeft) < .05f) return throttle;
            if (left <= 0.0f) { EndOverclock(); return true; }
            // A joiner sees him Overclocked as he is now; the meter stays empty while it runs.
            OverclockLeft = left; UltimateCharge = 0.0f;
            DressOverclock(motor);
            return true;
        }

        /// <summary>The wire and the copies are host-owned fields: a world snapshot replaces them without the kit's help.</summary>
        public void RebindWorldEffects(CharacterMotor motor) { }

        // ================================================================== THE OVERCLOCK ZAP (hold the ultimate while Overclocked)

        /// <summary>One frame of the channel as the host runs it (or a client last heard it).</summary>
        public struct ZapState
        {
            public bool Active;
            public int Target;
            public float Lock;
            public bool Grounded;
            public float Fuel;
            public bool SameChannel(in ZapState o) => Active == o.Active && Target == o.Target && Grounded == o.Grounded;
        }

        private ZackZapLook _zapLook;
        // The owner's own command (its prediction), the host's channel, and what a client last heard from the host.
        private bool _ownerZapping;
        private int _ownerTarget = -1;
        private float _ownerSentAt = -1.0f;
        private bool _ownerSentActive;
        private int _ownerSentTarget = -1;
        private bool _hostCmdActive;
        private int _hostCmdTarget = -1;
        private float _hostHeardAt = -1.0f, _hostBroken, _hostBroadcastAt = -1.0f;
        private ZapState _host, _heard, _hostSent;
        private readonly System.Collections.Generic.Dictionary<int, float> _zapGroundedUntil =
            new System.Collections.Generic.Dictionary<int, float>();

        /// <summary>A remote owner that stops answering for this long has let go (a lost release never leaves a beam on).</summary>
        private const float ZapCommandTimeout = 0.6f;
        private const float ZapKeepAlive = 0.2f;

        private bool Resolves => NetAuthority.ShouldResolve();
        private bool OwnerPredicts => !Resolves && _statusBody != null && _statusBody.IsLocallySimulated();

        /// <summary>He is channelling the zap (host channel; the owner's own press on his client; the host's word elsewhere).</summary>
        public bool Zapping => IsOverclocked && (Resolves ? _host.Active : OwnerPredicts ? _ownerZapping : _heard.Active);

        /// <summary>The rival in the beam, or -1 while it searches.</summary>
        public int ZapTarget => !Zapping ? -1 : Resolves ? _host.Target : OwnerPredicts ? _ownerTarget : _heard.Target;

        public bool ZapConnected => ZapTarget >= 0;

        /// <summary>0 to 1 toward the Zap on the current rival (the host's count, drawn by every peer).</summary>
        public float ZapLock01 => !ZapConnected ? 0.0f : Mathf.Clamp01((Resolves ? _host.Lock : _heard.Lock) / ZackRules.ZapLockSeconds);

        /// <summary>The rival in the beam was just Zapped by it and is grounded: the beam shows but cannot Zap them again yet.</summary>
        public bool ZapGrounded => ZapConnected && (Resolves ? _host.Grounded : _heard.Grounded);

        /// <summary>
        /// The rival a zap from <paramref name="motor"/> along <paramref name="aim"/> may hold: a rival body (attackers for the taya,
        /// the taya for an attacker), not tagged, within reach, inside <paramref name="degrees"/> of the aim and in plain sight of
        /// his chest. Owner (to pick) and host (to check) both ask; the host passes its wider cone and leeway.
        /// </summary>
        public static bool ZapCanHold(CharacterMotor motor, CharacterMotor rival, Vector3 aim, float degrees, float reach)
        {
            if (motor == null || rival == null || rival == motor || rival.IsDefender == motor.IsDefender || rival.IsTagged) return false;
            if (!rival.gameObject.activeInHierarchy) return false;
            Vector3 from = motor.transform.position + Vector3.up * 1.0f, to = rival.transform.position + Vector3.up * 0.9f;
            var flat = to - from; flat.y = 0.0f;
            float distance = flat.magnitude;
            if (distance > reach || distance < 0.05f) return false;
            if (ZackRules.FlatAngle(aim.x, aim.z, flat.x, flat.z) > degrees) return false;
            var line = to - from;
            float length = line.magnitude;
            return RafiWaterField.ClearDistance(from, line / length, length) >= length - 0.1f;
        }

        private static Vector3 AimOf(AbilityContext ctx)
        {
            var motor = ctx?.Motor;
            if (motor == null) return Vector3.forward;
            var aim = ctx.HasAimPoint ? ctx.AimPoint - motor.transform.position : motor.transform.forward;
            aim.y = 0.0f;
            return aim.sqrMagnitude > 0.01f ? aim.normalized : motor.transform.forward;
        }

        /// <summary>
        /// THE OWNER'S PRESS, every frame it is held (`OverclockAbility.StepHeldCommand`). It keeps the rival it has while that
        /// rival stays inside the wider hold cone, otherwise takes the one nearest the crosshair inside the narrow one, and tells
        /// the host on every change and as a keep-alive.
        /// </summary>
        public void OwnerZap(AbilityContext ctx, bool down)
        {
            var motor = ctx?.Motor;
            if (motor == null) return;
            bool active = down && IsOverclocked && CanChannel(motor);
            int target = -1;
            if (active)
            {
                var aim = AimOf(ctx);
                var round = GameServices.Round;
                var kept = _ownerTarget >= 0 ? round?.BodyAt(_ownerTarget) : null;
                if (kept != null && ZapCanHold(motor, kept, aim, ZackRules.ZapHoldDegrees, ZackRules.ZapRange)) target = _ownerTarget;
                else if (round != null)
                {
                    float best = float.PositiveInfinity;
                    foreach (var rival in round.Players)
                    {
                        if (!ZapCanHold(motor, rival, aim, ZackRules.ZapAcquireDegrees, ZackRules.ZapRange)) continue;
                        var to = rival.transform.position - motor.transform.position; to.y = 0.0f;
                        float angle = ZackRules.FlatAngle(aim.x, aim.z, to.x, to.z);
                        if (angle < best) { best = angle; target = rival.PlayerSlot; }
                    }
                }
            }
            _ownerZapping = active; _ownerTarget = target;
            if (Resolves) { HostZapCommand(motor, active, target); return; }
            if (active != _ownerSentActive || target != _ownerSentTarget || (active && Time.time - _ownerSentAt >= ZapKeepAlive))
            {
                _ownerSentActive = active; _ownerSentTarget = target; _ownerSentAt = Time.time;
                MatchRpc.Instance?.RequestZackZap(motor.PlayerSlot, active, target);
            }
        }

        private static bool CanChannel(CharacterMotor motor)
            => motor.CanAct() && !motor.IsCarryingCan && !motor.IsSwimming && GameServices.Round?.RoundActive == true
               && !PresentationClock.BlocksInput;

        /// <summary>⚠️ HOST ONLY. The owner's latest command (its own press here, or the request on the wire).</summary>
        public void HostZapCommand(CharacterMotor motor, bool active, int target)
        {
            if (!Resolves || motor == null || motor.AbilitySystem?.Kit != this) return;
            _hostCmdActive = active; _hostCmdTarget = active ? target : -1; _hostHeardAt = Time.time;
        }

        /// <summary>
        /// ⚠️ HOST ONLY, every frame: validates the commanded rival (range with leeway, sight, his facing within the host cone),
        /// runs the lock, Zaps at one second, grounds that rival, and broadcasts the channel and the fuel on change and at 5 Hz.
        /// </summary>
        private void HostStepZap(CharacterMotor motor, float dt)
        {
            bool fresh = motor.IsLocallySimulated() || Time.time - _hostHeardAt <= ZapCommandTimeout;
            bool active = _hostCmdActive && fresh && IsOverclocked && CanChannel(motor);
            int target = -1;
            if (active && _hostCmdTarget >= 0)
            {
                var rival = GameServices.Round?.BodyAt(_hostCmdTarget);
                var facing = motor.IsLocallySimulated() && motor.Intent != null && motor.Intent.HasAimPoint
                    ? motor.Intent.AimPoint - motor.transform.position : motor.transform.forward;
                facing.y = 0.0f;
                if (ZapCanHold(motor, rival, facing, ZackRules.ZapHostDegrees, ZackRules.ZapRange + ZackRules.ZapHostRangeLeeway))
                    target = _hostCmdTarget;
            }
            bool grounded = target >= 0 && _zapGroundedUntil.TryGetValue(target, out float until) && Time.time < until;
            float lockBefore = _host.Lock;
            float lockNow = target != _host.Target && _host.Target >= 0 && target >= 0 ? 0.0f : lockBefore;
            lockNow = ZackRules.StepZapLock(lockNow, ref _hostBroken, target >= 0 && !grounded, float.IsFinite(dt) ? dt : 0.0f);
            if (target >= 0 && !grounded && lockNow >= ZackRules.ZapLockSeconds - 1e-4f)
            {
                var victim = GameServices.Round.BodyAt(target);
                victim.ApplyZapped();
                _zapGroundedUntil[target] = Time.time + ZackRules.ZapImmunitySeconds;
                grounded = true; lockNow = 0.0f;
                var at = victim.transform.position + Vector3.up * 0.9f;
                NetCue.Play("sfx_zack_livewire_zap", at);
                Visual.MatchFlair.Announce(Visual.MatchFlair.Kind.HeroHit, motor.PlayerSlot, target, at, 1.0f);
            }
            _host = new ZapState { Active = active, Target = target, Lock = target >= 0 ? lockNow : 0.0f, Grounded = grounded, Fuel = OverclockLeft };
            if (!NetAuthority.IsNetworked || !NetAuthority.IsHost) return;
            bool changed = !_host.SameChannel(_hostSent);
            if (changed || (active && Time.time - _hostBroadcastAt >= ZapKeepAlive))
            {
                _hostSent = _host; _hostBroadcastAt = Time.time;
                MatchRpc.Instance?.BroadcastZackZap(motor.PlayerSlot, _host);
            }
        }

        /// <summary>A client: the host's word on the channel, and the fuel it counts.</summary>
        public void ApplyHostZap(CharacterMotor motor, ZapState state)
        {
            if (Resolves || motor == null || motor.AbilitySystem?.Kit != this) return;
            _heard = state;
            if (IsOverclocked && Mathf.Abs(state.Fuel - OverclockLeft) > 0.15f)
            {
                if (state.Fuel <= 0.0f) EndOverclock();
                else OverclockLeft = Mathf.Min(state.Fuel, ZackRules.OverclockSeconds);
            }
        }

        private void StopZap()
        {
            _ownerZapping = false; _ownerTarget = -1;
            _hostCmdActive = false; _hostCmdTarget = -1; _hostBroken = 0.0f;
            _host = default; _host.Target = -1; _heard = default; _heard.Target = -1;
            if (_ownerSentActive && _statusBody != null && !Resolves)
                MatchRpc.Instance?.RequestZackZap(_statusBody.PlayerSlot, false, -1);
            _ownerSentActive = false; _ownerSentTarget = -1;
            _zapGroundedUntil.Clear();
        }

        // ================================================================== LIVE WIRE reel-in (look at your own wire and hold)

        private int _reelEvent;
        private bool _reelDone;

        /// <summary>His own strung wire, if he is looking at it within reach right now (owner side).</summary>
        public ZackLiveWire WireInReelReach(AbilityContext ctx)
        {
            var motor = ctx?.Motor;
            if (motor == null || !motor.IsDefender) return null;
            var wire = ZackLiveWire.OwnedBy(motor.PlayerSlot);
            if (wire == null) return null;
            var at = motor.transform.position;
            if (wire.FlatDistanceTo(at) > ZackRules.LiveWireReelRange) return null;
            var aim = AimOf(ctx);
            float off = ZackRules.LookToWire(at.x, at.z, aim.x, aim.z, ZackRules.LiveWireReelRange + 0.5f,
                                             wire.PostA.x, wire.PostA.z, wire.PostB.x, wire.PostB.z);
            return off <= ZackRules.LiveWireReelAimTolerance ? wire : null;
        }

        /// <summary>The event id of his own wire he is looking at within reel reach on his own screen, 0 when none.</summary>
        public int ReelAimedEvent { get; private set; }

        /// <summary>0 to 1 of the reel hold on his screen (for the prompt and his hands), -1 when not reeling.</summary>
        public float ReelProgress { get; private set; } = -1.0f;

        public void OwnerReel(AbilityContext ctx, bool down, float held)
        {
            var motor = ctx?.Motor;
            if (motor == null) return;
            var rpc = MatchRpc.Instance;
            if (held <= 0.0f && down)
            {
                var wire = WireInReelReach(ctx);
                _reelEvent = wire != null ? wire.EventId : 0; _reelDone = false;
                if (_reelEvent > 0) Request(motor.PlayerSlot, MatchRpc.ZackReelPhase.Start);
                ReelProgress = _reelEvent > 0 ? 0.0f : -1.0f;
                return;
            }
            if (_reelEvent <= 0 || _reelDone) { if (!down) ReelProgress = -1.0f; return; }
            if (!down)
            {
                Request(motor.PlayerSlot, MatchRpc.ZackReelPhase.Cancel);
                _reelEvent = 0; ReelProgress = -1.0f;
                return;
            }
            ReelProgress = Mathf.Clamp01(held / ZackRules.LiveWireReelHoldSeconds);
            if (held >= ZackRules.LiveWireReelHoldSeconds)
            {
                _reelDone = true; ReelProgress = -1.0f;
                Request(motor.PlayerSlot, MatchRpc.ZackReelPhase.Complete);
            }
            void Request(int slot, MatchRpc.ZackReelPhase phase)
            {
                if (rpc != null) rpc.RequestZackReel(slot, _reelEvent, phase);
                else if (phase == MatchRpc.ZackReelPhase.Complete && NetAuthority.ShouldResolve())
                    ZackLiveWire.HostReelable(motor, _reelEvent)?.HostReelIn();
            }
        }

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
                _kit.SoftResetMomentum();
                // The accepted pullback look plays the catch when the body arrives.
            }
        }

        // ================================================================== FULL THROTTLE (attacking)

        private sealed class FullThrottleAbility : HeroAbility
        {
            // His own body and his own bar only, like Backtrack: the owner predicts it, the host and every peer replay the
            // accepted cast (the refill on the host's stamina clock, the surge on every screen).
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.Predicted;
            private readonly ZackHeroKit _kit;

            public FullThrottleAbility(ZackHeroKit kit)
                : base("zack_skill2", "FULL THROTTLE",
                       "Attacking. Instantly reach full Momentum and full Charge. For 3 seconds only a hit can slow you down.",
                       ZackRules.FullThrottleCooldown, ZackRules.FullThrottleSeconds, AbilityGlyph.ZackFullThrottle,
                       summary: "Full Momentum and full Charge at once, locked in for 3 s.",
                       castAction: "hero-zack-throttle", viewmodelAction: "throttle-rev",
                       // Codex owns audio: the old kit's registered cast sound stands in until a dedicated one lands.
                       castCue: "sfx_cast_zack_afterimage")
            { _kit = kit; }

            public override bool CanActivate(AbilityContext ctx)
                => base.CanActivate(ctx) && !ctx.Motor.IsDefender && !ctx.Motor.IsCarryingCan && !ctx.Motor.IsSwimming
                   && !ctx.Motor.IsTripped && !ctx.Motor.IsRooted;

            public override void Activate(AbilityContext ctx)
            {
                base.Activate(ctx);
                ClampToOverclock();
            }

            public void ClampToOverclock()
            {
                if (_kit.IsOverclocked && CooldownRemaining > ZackRules.FullThrottleOverclockedCooldown)
                    CooldownRemaining = ZackRules.FullThrottleOverclockedCooldown;
            }

            public bool RestoreLock(float remaining)
            {
                if (!float.IsFinite(remaining) || remaining <= 0.0f || IsActive) return false;
                RestoreLiveClock(Mathf.Min(remaining, ZackRules.FullThrottleSeconds));
                return true;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                if (ctx?.Motor == null) return;
                _kit.BeginThrottle(ctx.Motor);
                ctx.Motor.GetComponent<CharacterSquashStretch>()?.Stretch(0.06f);
            }

            // A refused prediction takes the speed back: the owner's free Momentum must not outlive the host's no.
            protected override void OnCancelled(AbilityContext ctx) => _kit.ResetMomentum();
        }

        // ================================================================== LIVE WIRE (defending)

        private sealed class LiveWireAbility : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.HostConfirmed;
            private readonly ZackHeroKit _kit;

            public LiveWireAbility(ZackHeroKit kit)
                : base("zack_skill2d", "LIVE WIRE",
                       "Defending. String a wire up to 6 m. A crosser is Zapped; it goes dark 5 s, then rearms. Look at it and hold to reel it in.",
                       ZackRules.LiveWireCooldown, 0.0f, AbilityGlyph.ZackLiveWire,
                       summary: "A wire that stays. Look at it and hold to reel it in.",
                       telegraphRadius: .35f, telegraphRange: ZackRules.LiveWireMaxLength,
                       castAction: "hero-zack-wire", viewmodelAction: "livewire-throw",
                       castCue: "sfx_cast_zack_livewire")
            {
                _kit = kit;
                // Owner, 2026-10-26: a TAP strings it where he looks. The reach is the full 6 m at once (no ramp), so a tap and a
                // held aim land on the same spot; holding only shows the preview first.
                AimByHolding(ZackRules.LiveWireMinLength, ZackRules.LiveWireMaxLength, rampSeconds: 0.0f, maxHoldSeconds: 0.0f,
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

            /// <summary>
            /// Looking at his own strung wire within reach when the key goes down makes the press a reel-in, whatever the cooldown:
            /// the hold is never a cast, and letting go early does nothing. Anywhere else the press is the ordinary aim and cast.
            /// </summary>
            public override bool TakesHeldCommand(AbilityContext ctx) => _kit.WireInReelReach(ctx) != null;

            public override void StepHeldCommand(AbilityContext ctx, bool down, float heldSeconds)
                => _kit.OwnerReel(ctx, down, heldSeconds);

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
                       "Overclocked on 25 s of fuel. Hold the ultimate to zap a rival within 9 m: 1 s held Zaps them. Zapping burns fuel 3x.",
                       0.0f, 0.0f, AbilityGlyph.ZackOverclock,
                       summary: "25 s of fuel: faster, free sprint. Hold to zap a rival.",
                       castAction: "hero-zack-overclock", viewmodelAction: "overclock-take", castCue: "")
            {
                _kit = kit;
                Windup = UltimateWindup;
                SupportsPendingSnapshot = true;
            }

            // The cutscene IS the charge: the bolt goes into him and play hands back with him already Overclocked.
            protected override bool IntroductionIsTheWindup => true;

            public override bool CanActivate(AbilityContext ctx) => base.CanActivate(ctx) && !_kit.IsOverclocked;

            /// <summary>While Overclocked the ultimate key is the zap: held, never a second cast.</summary>
            public override bool TakesHeldCommand(AbilityContext ctx) => _kit.IsOverclocked;

            public override void StepHeldCommand(AbilityContext ctx, bool down, float heldSeconds) => _kit.OwnerZap(ctx, down);

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
