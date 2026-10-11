using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>What a voodoo mark on a body will do when it goes off (HERO-10 v3).</summary>
    public enum VoodooMarkKind : byte
    {
        None = 0,
        /// <summary>CURSE: DRAIN. Goes off by itself `VoodooRules.DrainDelaySeconds` after it lands.</summary>
        Drain = 1,
        /// <summary>CURSE: HEX. Armed after `VoodooRules.HexArmSeconds`; goes off when she recasts.</summary>
        Hex = 2,
    }

    /// <summary>
    /// ⚠️⚠️ PHAISTER'S VOODOO ON A BODY (HERO-10 v3, the owner's table of 2026-09-27; `docs/reports/phaister-kit-2026-09-27/plan.md`
    /// section 9). Three pieces of state, all owned by the HOST, all stepped on every peer so the pictures move smoothly between
    /// snapshots, and all carried by `SyncUnit` so a joining peer needs no replayed cast:
    ///
    ///  * DRAINED and HEXED, two statuses from his table: *"Depletes stamina to 0. Prevents stamina recovery for 2.5 seconds."*
    ///    and *"Hallucinations of slippers randomly appear on your screen for 7.5 seconds."* Timers overlap by `Max()`.
    ///  * THE MARK, on the one she cursed: which curse, whose, and how old. His answer: *"only affects marked"*.
    ///  * THE REACH, on HER: whom she is reaching for, with which curse, and for how long. His words: *"i want her to just hold
    ///    her hand out towards someone for like 2 seconds or smth and thats marked and whiels he's holding towards them it
    ///    shows like an eerie vfx connecitng the two"*. The host breaks it if they leave `VoodooRules.ReachBreakRange`, her aim
    ///    or her sight, or if she is stunned; at `VoodooRules.ReachSeconds` it becomes a mark on them.
    ///
    /// THE PASSIVE, VOODOO: *"Whenever Phaister marks someone she takes 10% of their speed and they slow down by 10%"*, for as
    /// long as the mark lives. It is read off the marks, so it needs no state of its own (`VoodooSpeedScale`).
    /// </summary>
    public sealed partial class CharacterMotor
    {
        private float _drainedLeft, _hexedLeft;
        private VoodooMarkKind _markKind;
        private int _markSource = -1;
        private float _markAge;
        private VoodooMarkKind _reachKind;
        private int _reachTarget = -1;
        private float _reachElapsed;
        private bool _reachSucceeded;

        public bool IsDrained => _drainedLeft > 0.0f;
        public bool IsHexed => _hexedLeft > 0.0f;
        public float DrainedLeft => _drainedLeft;
        public float HexedLeft => _hexedLeft;

        /// <summary>The curse on this body, or None.</summary>
        public VoodooMarkKind VoodooMark => _markKind;
        /// <summary>The seat that marked this body, or -1.</summary>
        public int VoodooMarkSource => _markKind == VoodooMarkKind.None ? -1 : _markSource;
        /// <summary>Seconds since the mark landed.</summary>
        public float VoodooMarkAge => _markAge;
        /// <summary>A hex mark old enough for its caster to set it off.</summary>
        public bool VoodooHexArmed => _markKind == VoodooMarkKind.Hex && VoodooRules.HexArmed(_markAge);

        /// <summary>True while this body is reaching for someone with a curse.</summary>
        public bool IsVoodooReaching => _reachTarget >= 0 && _reachKind != VoodooMarkKind.None;
        public int VoodooReachTarget => IsVoodooReaching ? _reachTarget : -1;
        public VoodooMarkKind VoodooReachKind => IsVoodooReaching ? _reachKind : VoodooMarkKind.None;
        public float VoodooReachElapsed => _reachElapsed;
        public bool VoodooReachSucceeded => _reachSucceeded;
        /// <summary>0 to 1 through the reach.</summary>
        public float VoodooReachProgress => IsVoodooReaching ? Mathf.Clamp01(_reachElapsed / VoodooRules.ReachSeconds) : 0.0f;

        /// <summary>
        /// ⚠️ HER SLIPPER GOES TO HER BELT WHILE A CURSE NEEDS HER HANDS (plan 9.4, owner: *"think abt where slipper goes when u
        /// use skill and make it so that u can use right hand when doing skills"*). True through the reach and a moment after it
        /// (the tug), and for as long as a curse's gesture asked (`HoldSlipperAtBelt`: DRAIN's wring, HEX's stab). Presentation
        /// only: the slipper is still carried, and a throw charge takes it straight back to her hand (`Carrier`). Every peer
        /// computes it from the replicated reach and the same cast events, so every screen agrees.
        /// </summary>
        public bool StowsCarriedSlipper => IsVoodooReaching || _stowLeft > 0.0f;

        /// <summary>Keep the carried slipper at her belt for this long (a curse's two-handed gesture).</summary>
        public void HoldSlipperAtBelt(float seconds) => _stowLeft = Mathf.Max(_stowLeft, seconds);

        /// <summary>How long the slipper stays at the belt after a reach ends: the tug back to her chest.</summary>
        private const float StowAfterReach = 0.3f;
        private float _stowLeft;

        /// <summary>
        /// True while no curse can land on this body (Geo's Shield): `HostVoodooMark` would refuse it, so her reach picks
        /// someone else rather than spending 2 s on a mark that cannot happen.
        /// </summary>
        public bool RefusesVoodoo => StatusImmune;

        /// <summary>
        /// This body's own share of a player's speed: 1 for every person, `VoodooRules.DollSpeedScale` for Phaister's voodoo doll
        /// (her ultimate's body, slow on the owner's word). Set by whoever makes the body; it is part of what the body is.
        /// </summary>
        public float BodySpeedScale { get; set; } = 1.0f;

        /// <summary>Raised on every peer when a reach ends: true when it became a mark, false when it snapped.</summary>
        public event System.Action<CharacterMotor, bool> VoodooReachEnded;

        // ------------------------------------------------------------------ statuses

        public void ApplyDrained(float seconds = StatusRules.DrainedSeconds)
        {
            if (!MayMutateGameplayState() || StatusImmune) return;
            bool fresh = _drainedLeft <= 0.0f;
            _drainedLeft = StatusRules.Refresh(_drainedLeft, seconds);
            Stamina.RecoveryBlocked = IsDrained;
            Stamina.Deplete();
            if (fresh) RaiseStatus(StatusKind.Drained);
        }

        public void ApplyHexed(float seconds = StatusRules.HexedSeconds)
        {
            if (!MayMutateGameplayState() || StatusImmune) return;
            bool fresh = _hexedLeft <= 0.0f;
            _hexedLeft = StatusRules.Refresh(_hexedLeft, seconds);
            if (fresh) RaiseStatus(StatusKind.Hexed);
        }

        // ------------------------------------------------------------------ the reach and the mark (host)

        /// <summary>The host starts her reach. Refused unless the target could be reached now.</summary>
        public bool HostBeginVoodooReach(VoodooMarkKind kind, CharacterMotor target)
        {
            if (!NetAuthority.ShouldResolve() || kind == VoodooMarkKind.None || target == null || target == this) return false;
            if (!VoodooReachIsValid(target, starting: true)) return false;
            _reachKind = kind;
            _reachTarget = target.PlayerSlot;
            _reachElapsed = 0.0f;
            _reachSucceeded = false;
            return true;
        }

        /// <summary>Is <paramref name="target"/> within her reach, her aim and her sight?</summary>
        public bool VoodooReachIsValid(CharacterMotor target, bool starting)
        {
            if (target == null || !target.isActiveAndEnabled) return false;
            Vector3 from = transform.position + Vector3.up * 1.2f;
            Vector3 to = target.transform.position + Vector3.up * 1.2f;
            Vector3 flat = to - from; flat.y = 0.0f;
            float distance = flat.magnitude;
            Vector3 facing = transform.forward; facing.y = 0.0f;
            float off = distance < 0.05f ? 0.0f : Vector3.Angle(facing, flat);
            bool sight = VoodooLineOfSight(from, to);
            return starting ? VoodooRules.ReachCanStart(distance, off, sight) : VoodooRules.ReachHolds(distance, off, sight);
        }

        private static bool VoodooLineOfSight(Vector3 from, Vector3 to)
        {
            Vector3 toward = to - from;
            float distance = toward.magnitude;
            if (distance < 0.05f) return true;
            if (!Physics.Raycast(from, toward / distance, out var hit, distance, ~0, QueryTriggerInteraction.Ignore)) return true;
            // Bodies and slippers are not walls; a jeepney is.
            return hit.collider == null || hit.collider.GetComponentInParent<CharacterMotor>() != null
                   || hit.collider.GetComponentInParent<Slipper>() != null;
        }

        /// <summary>The host marks this body with <paramref name="kind"/> from <paramref name="source"/>.</summary>
        public void HostVoodooMark(VoodooMarkKind kind, int source)
        {
            if (!NetAuthority.ShouldResolve()) return;
            if (StatusImmune) return;
            _markKind = kind;
            _markSource = source;
            _markAge = 0.0f;
        }

        /// <summary>The host sets off an armed hex on this body (her recast). False when there is none to set off.</summary>
        public bool HostDetonateHex(int source)
        {
            if (!NetAuthority.ShouldResolve() || !VoodooHexArmed || _markSource != source) return false;
            ClearVoodooMark();
            ApplyHexed();
            return true;
        }

        private void ClearVoodooMark()
        {
            _markKind = VoodooMarkKind.None;
            _markSource = -1;
            _markAge = 0.0f;
        }

        private void EndVoodooReach(bool marked)
        {
            if (!IsVoodooReaching) return;
            _reachSucceeded = marked;
            _reachKind = VoodooMarkKind.None;
            _reachTarget = -1;
            _reachElapsed = 0.0f;
            VoodooReachEnded?.Invoke(this, marked);
        }

        /// <summary>Every peer, every frame: the clocks run; only the host decides what they mean.</summary>
        private void StepVoodoo(float dt)
        {
            if (_drainedLeft > 0.0f) _drainedLeft = Mathf.Max(0.0f, _drainedLeft - dt);
            if (_hexedLeft > 0.0f) _hexedLeft = Mathf.Max(0.0f, _hexedLeft - dt);
            // (And while the movement rework prototype's hop chain is running, a debug switch that is false with it off:
            // hopping does not rest the bar. `CharacterMotor.MovementRework.cs`.)
            Stamina.RecoveryBlocked = IsDrained || ReworkBlocksRecovery;
            if (_markKind != VoodooMarkKind.None) _markAge += dt;
            if (IsVoodooReaching) { _reachElapsed += dt; _stowLeft = Mathf.Max(_stowLeft, StowAfterReach); }
            else if (_stowLeft > 0.0f) _stowLeft = Mathf.Max(0.0f, _stowLeft - dt);

            if (!NetAuthority.ShouldResolve()) return;

            if (_markKind == VoodooMarkKind.Drain && _markAge >= VoodooRules.DrainDelaySeconds)
            {
                ClearVoodooMark();
                ApplyDrained();
            }
            else if (_markKind == VoodooMarkKind.Hex && VoodooRules.HexExpired(_markAge))
                ClearVoodooMark();

            if (!IsVoodooReaching) return;
            var round = GameServices.Round;
            var target = round != null ? round.PlayerAt(_reachTarget) : null;
            if (target == null || !CanAct() || !VoodooReachIsValid(target, starting: false))
            {
                EndVoodooReach(marked: false);
                return;
            }
            if (_reachElapsed < VoodooRules.ReachSeconds) return;
            var kind = _reachKind;
            EndVoodooReach(marked: true);
            target.HostVoodooMark(kind, _playerSlot);
        }

        /// <summary>
        /// THE PASSIVE: *"she takes 10% of their speed and they slow down by 10%"*, while her mark lives. A body her mark is on
        /// runs slower; the body whose mark it is runs faster. Read off the replicated marks, so every peer agrees.
        /// </summary>
        public float VoodooSpeedScale
        {
            get
            {
                float scale = _markKind != VoodooMarkKind.None ? VoodooRules.PassiveSpeedScale(isTheCaster: false) : 1.0f;
                var round = GameServices.Round;
                if (round == null) return scale;
                foreach (var p in round.Players)
                    if (p != null && p != this && p._markKind != VoodooMarkKind.None && p._markSource == _playerSlot)
                        return scale * VoodooRules.PassiveSpeedScale(isTheCaster: true);
                return scale;
            }
        }

        /// <summary>Round reset, respawn and cleanse: the curses and the reach end.</summary>
        private void ClearVoodoo()
        {
            _drainedLeft = 0.0f;
            _hexedLeft = 0.0f;
            Stamina.RecoveryBlocked = false;
            ClearVoodooMark();
            EndVoodooReach(marked: false);
            _reachSucceeded = false;
            _stowLeft = 0.0f;
        }

        /// <summary>
        /// A cleanse (Geo's Shield): DRAINED, HEXED and any curse waiting on this body end. Her own reach, if this body is hers,
        /// goes on: a cleanse lifts what was done TO a body.
        /// </summary>
        private void CleanseVoodoo()
        {
            _drainedLeft = 0.0f;
            _hexedLeft = 0.0f;
            Stamina.RecoveryBlocked = false;
            ClearVoodooMark();
        }

        /// <summary>The host's voodoo state for this body, off the wire (`SyncUnit`).</summary>
        public void ApplyNetworkVoodoo(float drainedLeft, float hexedLeft, byte markKind, int markSource, float markAge,
                                       byte reachKind, int reachTarget, float reachElapsed, bool? reachSucceeded = null)
        {
            bool drained = _drainedLeft <= 0.0f && drainedLeft > 0.0f;
            bool hexed = _hexedLeft <= 0.0f && hexedLeft > 0.0f;
            _drainedLeft = Mathf.Clamp(drainedLeft, 0.0f, StatusRules.DrainedSeconds + 0.01f);
            _hexedLeft = Mathf.Clamp(hexedLeft, 0.0f, StatusRules.HexedSeconds + 0.01f);
            Stamina.RecoveryBlocked = IsDrained;
            if (drained) { Stamina.Deplete(); RaiseStatus(StatusKind.Drained); }
            if (hexed) RaiseStatus(StatusKind.Hexed);

            _markKind = markKind <= (byte)VoodooMarkKind.Hex ? (VoodooMarkKind)markKind : VoodooMarkKind.None;
            _markSource = _markKind == VoodooMarkKind.None ? -1 : markSource;
            _markAge = Mathf.Clamp(markAge, 0.0f, VoodooRules.HexMarkLifeSeconds);

            var kind = reachKind <= (byte)VoodooMarkKind.Hex ? (VoodooMarkKind)reachKind : VoodooMarkKind.None;
            if (kind == VoodooMarkKind.None || reachTarget < 0)
            {
                // Network receipts carry the host's outcome. Another body's mark
                // snapshot may arrive later, so only legacy direct callers infer it.
                bool marked = reachSucceeded ?? false;
                if (!reachSucceeded.HasValue)
                {
                    var was = GameServices.Round != null && _reachTarget >= 0 ? GameServices.Round.PlayerAt(_reachTarget) : null;
                    marked = was != null && was._markSource == _playerSlot && was._markKind != VoodooMarkKind.None;
                }
                EndVoodooReach(marked);
                _reachSucceeded = marked;
            }
            else
            {
                _reachKind = kind;
                _reachTarget = reachTarget;
                _reachElapsed = Mathf.Clamp(reachElapsed, 0.0f, VoodooRules.ReachSeconds);
                _reachSucceeded = false;
            }
        }
    }
}
