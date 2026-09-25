using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// ⚠️⚠️ THE STATUSES, THE CARRY AND FLIGHT (ability overhaul, owner 2026-09-25).
    ///
    /// The owner's status table is the one list: WHIRLED, CHILLED, FROZEN, TAGGED
    /// (`Core.StatusRules`). Frozen and Tagged are stuns and live in the stun stack the motor
    /// already has, so they are READ here rather than stored twice; Whirled and Chilled are not
    /// stuns (a Whirled body runs, a Chilled one walks) and get timers of their own. Every timer
    /// overlaps by `Max()` (`CLAUDE.md` § 4).
    ///
    /// The carry and flight are what Amihan's kit moves bodies with; both are written as distances
    /// and heights and solved in `Core.AmihanRules` / `Core.CarryRules`.
    /// </summary>
    public sealed partial class CharacterMotor
    {
        // ------------------------------------------------------------------ WHIRLED and CHILLED

        private float _whirledLeft, _chilledLeft;

        /// <summary>Seconds of Whirled left: no slipper retrieval while it runs.</summary>
        public float WhirledLeft => _whirledLeft;

        /// <summary>Seconds of Chilled left: half movement speed while it runs.</summary>
        public float ChilledLeft => _chilledLeft;

        public bool IsWhirled => _whirledLeft > 0.0f;
        public bool IsChilled => _chilledLeft > 0.0f;

        /// <summary>
        /// ⚠️ TAGGED IS READ OFF THE STUN, NOT STORED. The tag is the only stun that is 5 s long
        /// and carries no element (`StunElement.None` is "a rule, not a fight"), so the pair is
        /// the status exactly; `CatchReconstruction` already asks the same question the same way.
        /// </summary>
        public bool IsTagged => _stunLeft > 0.0f && _stunElement == StunElement.None
                                && _stunTotal >= StatusRules.TaggedSeconds - 0.05f;

        /// <summary>Frozen is an Ice-element hold (Glacial Nova, the barricade's freeze).</summary>
        public bool IsFrozen => _stunLeft > 0.0f && _stunElement == StunElement.Ice;

        /// <summary>Raised on every peer when this body gains a status, for the mark and the cue.</summary>
        public event System.Action<CharacterMotor, StatusKind> StatusGained;

        /// <summary>The seconds left on one status, 0 when it is not running.</summary>
        public float StatusLeft(StatusKind kind)
        {
            switch (kind)
            {
                case StatusKind.Whirled: return _whirledLeft;
                case StatusKind.Chilled: return _chilledLeft;
                case StatusKind.Frozen: return IsFrozen ? _stunLeft : 0.0f;
                case StatusKind.Tagged: return IsTagged ? _stunLeft : 0.0f;
                default: return 0.0f;
            }
        }

        /// <summary>What the status started at, for the draining ring.</summary>
        public float StatusTotal(StatusKind kind)
        {
            switch (kind)
            {
                case StatusKind.Frozen: return IsFrozen ? Mathf.Max(_stunTotal, 0.01f) : 1.0f;
                case StatusKind.Tagged: return IsTagged ? Mathf.Max(_stunTotal, 0.01f) : 1.0f;
                default: return StatusRules.For(kind)?.Seconds ?? 1.0f;
            }
        }

        /// <summary>The movement multiplier every live status puts on this body. 1 = none.</summary>
        public float StatusSpeedScale => IsChilled ? StatusRules.ChilledSpeedScale : 1.0f;

        /// <summary>
        /// WHIRLED: *"Drops slipper if currently in hand. Prevents slipper retrieval for 2.5
        /// seconds."* (owner's table).
        ///
        /// ⚠️⚠️ THE DROP IS A DECISION AND HAPPENS ON THE HOST ONLY. A peer predicting its own body
        /// may start its own timer (so its own pickup refuses at once), but only the host takes a
        /// slipper out of a hand, and `BroadcastSlipperState` tells everybody else.
        /// </summary>
        public void ApplyWhirled(float seconds = StatusRules.WhirledSeconds)
        {
            if (!MayMutateGameplayState()) return;
            if (AbilitySystem != null && AbilitySystem.IsImmuneToStuns) return;

            bool fresh = _whirledLeft <= 0.0f;
            _whirledLeft = StatusRules.Refresh(_whirledLeft, seconds);

            if (NetAuthority.ShouldResolve())
            {
                var carrier = GetComponent<Carrier>();
                var held = carrier != null ? carrier.Held : null;
                if (held != null && held.HostDisarm())
                {
                    // Knocked loose a little way out of the hand, on the ground, never under her.
                    held.HostScatter(transform.forward * -0.35f + transform.right * 0.45f);
                    Net.MatchRpc.Instance?.BroadcastSlipperState(held);
                }
            }

            if (fresh) StatusGained?.Invoke(this, StatusKind.Whirled);
        }

        /// <summary>CHILLED: *"Decreases movement speed by 50% for 5 seconds."*</summary>
        public void ApplyChilled(float seconds = StatusRules.ChilledSeconds)
        {
            if (!MayMutateGameplayState()) return;
            if (AbilitySystem != null && AbilitySystem.IsImmuneToStuns) return;
            bool fresh = _chilledLeft <= 0.0f;
            _chilledLeft = StatusRules.Refresh(_chilledLeft, seconds);
            if (fresh) StatusGained?.Invoke(this, StatusKind.Chilled);
        }

        /// <summary>
        /// TAGGED: *"Prevents movement or interaction for 5 seconds. Cannot be removed or be immune
        /// to."* (owner's table).
        ///
        /// ⚠️⚠️ IT DOES NOT ASK `IsImmuneToStuns`, AND THAT IS THE WHOLE DIFFERENCE FROM
        /// `ApplyStagger`. The tag went through `ApplyStagger`, which returns early under Demonic
        /// Carapace: an armoured attacker could be tagged, scored against, teleported, and then
        /// walk straight back in because the five seconds never landed. The element stays `None`,
        /// which is what already makes it unmashable (`MashOutOfStun` refuses `None`).
        /// </summary>
        public void ApplyTagged()
        {
            if (!MayMutateGameplayState()) return;
            AdvanceRecoveryEpisode();
            ReleaseCommitment();
            EndFlightImmediately();
            _carryLeft = 0.0f;
            _stunLeft = Combat.ApplyStagger(_stunLeft, StatusRules.TaggedSeconds);
            _stunTotal = Mathf.Max(_stunTotal, _stunLeft);
            _stunElement = StunElement.None;
            _stunBreakPresses = Balance.StunBreakPressesDefault;
            _stunMashPresses = 0;
            StatusGained?.Invoke(this, StatusKind.Tagged);
        }

        /// <summary>Ends Whirled and Chilled. For the round reset and the respawn only.</summary>
        public void ClearStatuses()
        {
            _whirledLeft = 0.0f;
            _chilledLeft = 0.0f;
            _carryLeft = 0.0f;
            EndFlightImmediately();
        }

        /// <summary>The host's status timers, off the wire (`SyncUnit`).</summary>
        public void ApplyNetworkStatuses(float whirledLeft, float chilledLeft)
        {
            bool whirled = _whirledLeft <= 0.0f && whirledLeft > 0.0f;
            bool chilled = _chilledLeft <= 0.0f && chilledLeft > 0.0f;
            _whirledLeft = Mathf.Clamp(whirledLeft, 0.0f, StatusRules.WhirledSeconds + 0.01f);
            _chilledLeft = Mathf.Clamp(chilledLeft, 0.0f, StatusRules.ChilledSeconds + 0.01f);
            if (whirled) StatusGained?.Invoke(this, StatusKind.Whirled);
            if (chilled) StatusGained?.Invoke(this, StatusKind.Chilled);
        }

        private void StepStatuses(float dt)
        {
            if (_whirledLeft > 0.0f) _whirledLeft = Mathf.Max(0.0f, _whirledLeft - dt);
            if (_chilledLeft > 0.0f) _chilledLeft = Mathf.Max(0.0f, _chilledLeft - dt);
        }

        // ------------------------------------------------------------------ THE CARRY

        private Vector3 _carryVelocity;
        private float _carryLeft;

        /// <summary>True while the wind (or a dash) is holding this body at a set speed.</summary>
        public bool IsCarried => _carryLeft > 0.0f;

        /// <summary>
        /// Hold this body at <paramref name="velocity"/> (horizontal) for <paramref name="seconds"/>,
        /// then let it slide out against `Friction`. See `Core.CarryRules` for why a carry exists
        /// and how its time is solved from the written distance. A vertical component is a one-off
        /// lift, capped like every other.
        ///
        /// ⚠️ THE OWNER OF THE BODY APPLIES IT, like `ApplyImpulse`: a host resolving a hit on a
        /// client-driven body uses <see cref="ApplyResolvedCarry"/>, which sends it there.
        /// </summary>
        public void BeginCarry(Vector3 velocity, float seconds)
        {
            if (!MayMutateGameplayState() || !IsLocallySimulated()) return;
            if (!Finite(velocity) || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            var flat = new Vector3(velocity.x, 0.0f, velocity.z);
            if (flat.magnitude > Balance.MaxKnockbackSpeed) flat = flat.normalized * Balance.MaxKnockbackSpeed;
            _carryVelocity = flat;
            _carryLeft = Mathf.Clamp(seconds, 0.0f, 3.0f);
            _externalVelocity = flat;
            if (velocity.y > 0.0f) _velocity.y = Mathf.Min(velocity.y, Balance.MaxKnockbackLift);
            // A carried body is not steering where it chose, and must not out-walk the wind.
            if (_carryLeft > 0.0f) Commit(_carryLeft);
        }

        /// <summary>The host's carry on any body: applied here if this peer simulates it, sent to
        /// the owner otherwise. The same split as <see cref="ApplyResolvedImpact"/>.</summary>
        public void ApplyResolvedCarry(Vector3 velocity, float seconds)
        {
            if (!NetAuthority.ShouldResolve()) return;
            if (AbilitySystem != null && AbilitySystem.IsImmuneToStuns) return;
            if (!IsLocallySimulated())
            {
                Net.MatchRpc.Instance?.BroadcastCarry(_playerSlot, velocity, seconds);
                return;
            }
            BeginCarry(velocity, seconds);
        }

        /// <summary>Called from the physics step: hold the carry, before `Friction` decays it.</summary>
        private void StepCarry(float dt)
        {
            if (_carryLeft <= 0.0f) return;
            _carryLeft = Mathf.Max(0.0f, _carryLeft - dt);
            _externalVelocity.x = _carryVelocity.x;
            _externalVelocity.z = _carryVelocity.z;
        }

        private static bool Finite(Vector3 v)
            => !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z)
                 || float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));

        // ------------------------------------------------------------------ FLIGHT

        private enum FlightMode : byte { Grounded, Aloft, Descending }
        private FlightMode _flight;
        private float _flightCeiling, _flightRiseSpeed, _flightDescent;

        /// <summary>
        /// True while held up in the air by a flight (Updraft). A body ALOFT cannot pick up a
        /// slipper and is out of the taya's reach; one DESCENDING is neither, because it is on its
        /// way to exactly the ground where both apply again. Owner, 2026-09-25: *"flies high and can
        /// throw slippers but cant pick up unless they choose to go down"*.
        /// </summary>
        public bool IsAloft => _flight == FlightMode.Aloft;

        /// <summary>Aloft or on the glide down. For the pose and the hover VFX.</summary>
        public bool IsFlying => _flight != FlightMode.Grounded;

        /// <summary>
        /// Lift this body to <paramref name="height"/> metres above where it stands, reaching it in
        /// <paramref name="riseSeconds"/>, and hold it there until <see cref="EndFlight"/>.
        ///
        /// ⚠️ SET ON EVERY PEER, SIMULATED ON THE OWNER. The ability runs on every peer (the cast is
        /// broadcast), so every peer knows the body is flying, for the pose and for the host's
        /// pickup and tag gates; only the peer that simulates the body moves it.
        /// </summary>
        public void BeginFlight(float height, float riseSeconds, float descentSpeed)
        {
            _flightCeiling = transform.position.y + Mathf.Max(0.0f, height);
            _flightRiseSpeed = Mathf.Max(0.5f, height / Mathf.Max(0.1f, riseSeconds));
            _flightDescent = Mathf.Max(0.5f, descentSpeed);
            _flight = FlightMode.Aloft;
            _grounded = false;
            ReleaseCommitment();
        }

        /// <summary>The glide down. The flight is over when the feet touch.</summary>
        public void EndFlight()
        {
            if (_flight == FlightMode.Aloft) _flight = FlightMode.Descending;
        }

        /// <summary>No glide: a tag or a round reset puts the body straight back under gravity.</summary>
        public void EndFlightImmediately() => _flight = FlightMode.Grounded;

        /// <summary>
        /// ⚠️ RETURNS TRUE WHEN FLIGHT OWNS THE VERTICAL THIS STEP, so `ApplyGravity` skips its own
        /// gravity and jump. A stun while aloft turns the hold into the glide down rather than a
        /// drop, so a Frozen flyer does not fall 2.8 m onto the chalk.
        /// </summary>
        private bool StepFlightVertical(float dt)
        {
            if (_flight == FlightMode.Grounded) return false;
            if (_flight == FlightMode.Aloft && IsStunned) _flight = FlightMode.Descending;

            if (_flight == FlightMode.Aloft)
            {
                float gap = _flightCeiling - transform.position.y;
                // Ease into the ceiling: full rise speed far below it, settling as it arrives, and
                // a gentle bob so a hovering body is never dead still.
                float bob = Mathf.Sin(Time.time * 2.6f) * 0.12f;
                _velocity.y = Mathf.Clamp(gap * 6.0f + bob, -_flightRiseSpeed, _flightRiseSpeed);
                return true;
            }

            // Descending: a steady glide, finished by the ground.
            if (_grounded && _velocity.y <= 0.0f)
            {
                _flight = FlightMode.Grounded;
                _velocity.y = GroundedRestVelocityY;
                return true;
            }
            _velocity.y = -_flightDescent;
            return true;
        }
    }
}
