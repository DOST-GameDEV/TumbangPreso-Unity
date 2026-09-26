using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// ⚠️⚠️ THE ROSTER REWORK'S FOUR STATUSES (ABILITY-2, owner 2026-09-26): CONCUSSED, FEARED,
    /// DISORIENTED and VULNERABLE, with the owner's answers in `Core.StatusRules` and the plan in
    /// `docs/reports/ability-rework-2026-09-26/plan.md` section 2. Each is a timer that overlaps by
    /// `Max()` (`CLAUDE.md` § 4), refused by status immunity, and carried by `SyncUnit` so every peer
    /// shows it and the body's own peer obeys it.
    ///
    ///  * CONCUSSED: x0.7 speed (`StatusSpeedScale`), no sprint, the throw wobbles (`AimWobbleDegrees`).
    ///  * FEARED: *"Flee from kuro and drop slipper"*. The held slipper drops (host), and the peer that
    ///    simulates the body runs it AWAY from the source on its own (`FleeWish`); no act (`CanAct`).
    ///  * DISORIENTED: *"hallucinations but make it so that some of the shit they see are real"*. The
    ///    body is untouched; the victim's own screen draws phantoms (`DisorientedPhantoms`) and the aim
    ///    sways (`AimWobbleDegrees`).
    ///  * VULNERABLE: *"easier to tag and phaister can go out of box and tag them"*. Taggable anywhere,
    ///    tag reach x1.5 (`TagReachScale`), stuns x1.5, and a taya who owns a Vulnerable curse may
    ///    leave the box while one is running (`MayLeaveBoxToTag`).
    /// </summary>
    public sealed partial class CharacterMotor
    {
        private float _concussedLeft, _fearedLeft, _disorientedLeft, _vulnerableLeft;
        private Vector3 _fearFrom;

        public bool IsConcussed => _concussedLeft > 0.0f;
        public bool IsFeared => _fearedLeft > 0.0f;
        public bool IsDisoriented => _disorientedLeft > 0.0f;
        public bool IsVulnerable => _vulnerableLeft > 0.0f;
        public float ConcussedLeft => _concussedLeft;
        public float FearedLeft => _fearedLeft;
        public float DisorientedLeft => _disorientedLeft;
        public float VulnerableLeft => _vulnerableLeft;

        /// <summary>Where the fear came from (Kuro's haunt); the body runs directly away from it.</summary>
        public Vector3 FearSource => _fearFrom;

        /// <summary>True while any status except Tagged would be refused (Geo's Shield, Carapace).</summary>
        private bool StatusImmune => AbilitySystem != null && (AbilitySystem.IsImmuneToStuns || AbilitySystem.IsImmuneToStatuses);

        /// <summary>
        /// How far the next throw's aim is knocked off, degrees, sampled from a seeded sine of the
        /// body's own clock, so the owner (who throws) and the host (who checks) read the same wobble.
        /// </summary>
        public float AimWobbleDegrees
        {
            get
            {
                float t = Time.time + _playerSlot * 1.37f;
                float w = 0.0f;
                if (IsConcussed) w += StatusRules.ConcussedAimWobbleDegrees * Mathf.Sin(t * 5.3f) * Mathf.Cos(t * 2.1f);
                if (IsDisoriented) w += StatusRules.DisorientedAimSwayDegrees * Mathf.Sin(t * 1.7f + 0.8f);
                return w;
            }
        }

        /// <summary>Multiplier on the taya's tag reach against this body.</summary>
        public float TagReachScale => IsVulnerable ? StatusRules.VulnerableTagReachScale : 1.0f;

        public void ApplyConcussed(float seconds = StatusRules.ConcussedSeconds)
        {
            if (!MayMutateGameplayState() || StatusImmune) return;
            bool fresh = _concussedLeft <= 0.0f;
            _concussedLeft = StatusRules.Refresh(_concussedLeft, seconds);
            if (fresh)
            {
                Visual.DizzyStars.Attach(transform, seconds);
                RaiseStatus(StatusKind.Concussed);
            }
        }

        /// <summary>FEARED from <paramref name="source"/>: the slipper drops (host) and the body flees.</summary>
        public void ApplyFeared(Vector3 source, float seconds = StatusRules.FearedSeconds)
        {
            if (!MayMutateGameplayState() || StatusImmune) return;
            bool fresh = _fearedLeft <= 0.0f;
            _fearedLeft = StatusRules.Refresh(_fearedLeft, seconds);
            _fearFrom = source;
            ReleaseCommitment();
            if (NetAuthority.ShouldResolve())
            {
                var carrier = GetComponent<Carrier>();
                var held = carrier != null ? carrier.Held : null;
                if (held != null && held.HostDisarm())
                {
                    // Dropped in terror: it falls where they stood as they turn to run.
                    Vector3 away = transform.position - source; away.y = 0.0f;
                    held.HostScatter(-(away.sqrMagnitude > 0.01f ? away.normalized : transform.forward) * 0.4f);
                    Net.MatchRpc.Instance?.BroadcastSlipperState(held);
                }
            }
            if (fresh) RaiseStatus(StatusKind.Feared);
        }

        public void ApplyDisoriented(float seconds = StatusRules.DisorientedSeconds)
        {
            if (!MayMutateGameplayState() || StatusImmune) return;
            bool fresh = _disorientedLeft <= 0.0f;
            _disorientedLeft = StatusRules.Refresh(_disorientedLeft, seconds);
            if (fresh) RaiseStatus(StatusKind.Disoriented);
        }

        public void ApplyVulnerable(float seconds = StatusRules.VulnerableSeconds)
        {
            if (!MayMutateGameplayState() || StatusImmune) return;
            bool fresh = _vulnerableLeft <= 0.0f;
            _vulnerableLeft = StatusRules.Refresh(_vulnerableLeft, seconds);
            if (fresh) RaiseStatus(StatusKind.Vulnerable);
        }

        /// <summary>Geo's Shield on cast: every removable status ends (Tagged is not removable).</summary>
        public void CleanseStatuses()
        {
            if (!MayMutateGameplayState()) return;
            _whirledLeft = 0.0f; _chilledLeft = 0.0f;
            _concussedLeft = 0.0f; _fearedLeft = 0.0f; _disorientedLeft = 0.0f; _vulnerableLeft = 0.0f;
            if (IsFrozen) { _stunLeft = 0.0f; _stunTotal = 0.0f; }
            EndRooted();
        }

        /// <summary>The flee direction: straight away from the source, flat. Only the simulating peer uses it.</summary>
        private Vector3 FleeWish()
        {
            Vector3 away = transform.position - _fearFrom; away.y = 0.0f;
            if (away.sqrMagnitude < 0.0001f) away = -transform.forward;
            return away.normalized;
        }

        /// <summary>
        /// A taya may leave the box while an opponent is Vulnerable to THEIR curse: owner, *"phaister can
        /// go out of box and tag them"*. Read off the kit so only the curse's owner gets it.
        /// </summary>
        public bool MayLeaveBoxToTag
        {
            get
            {
                if (!_isDefender || AbilitySystem == null || !AbilitySystem.GrantsOutOfBoxTag) return false;
                var round = GameServices.Round;
                if (round == null) return false;
                foreach (var p in round.Players)
                    if (p != null && p != this && p.IsVulnerable) return true;
                return false;
            }
        }

        private void RaiseStatus(StatusKind kind)
        {
            StatusGained?.Invoke(this, kind);
            // The hallucinations live on the victim's own screen only (owner: *"some of the shit they see
            // are real"*): the local human's peer draws them, nobody else does.
            if (kind == StatusKind.Disoriented && IsLocalHuman) Visual.DisorientedHallucinations.Begin(this);
        }

        private void StepReworkStatuses(float dt)
        {
            if (_concussedLeft > 0.0f) _concussedLeft = Mathf.Max(0.0f, _concussedLeft - dt);
            if (_fearedLeft > 0.0f) _fearedLeft = Mathf.Max(0.0f, _fearedLeft - dt);
            if (_disorientedLeft > 0.0f) _disorientedLeft = Mathf.Max(0.0f, _disorientedLeft - dt);
            if (_vulnerableLeft > 0.0f) _vulnerableLeft = Mathf.Max(0.0f, _vulnerableLeft - dt);
        }

        private void ClearReworkStatuses()
        {
            _concussedLeft = 0.0f; _fearedLeft = 0.0f; _disorientedLeft = 0.0f; _vulnerableLeft = 0.0f;
        }

        /// <summary>The host's timers for the four, off the wire (`SyncUnit`), with the fear's source.</summary>
        public void ApplyNetworkReworkStatuses(float concussed, float feared, float disoriented, float vulnerable, Vector3 fearFrom)
        {
            bool c = _concussedLeft <= 0.0f && concussed > 0.0f;
            bool f = _fearedLeft <= 0.0f && feared > 0.0f;
            bool d = _disorientedLeft <= 0.0f && disoriented > 0.0f;
            bool v = _vulnerableLeft <= 0.0f && vulnerable > 0.0f;
            _concussedLeft = Mathf.Clamp(concussed, 0.0f, StatusRules.ConcussedSeconds + 0.01f);
            _fearedLeft = Mathf.Clamp(feared, 0.0f, StatusRules.FearedSeconds + 0.01f);
            _disorientedLeft = Mathf.Clamp(disoriented, 0.0f, StatusRules.DisorientedSeconds + 0.01f);
            _vulnerableLeft = Mathf.Clamp(vulnerable, 0.0f, StatusRules.VulnerableSeconds + 0.01f);
            if (Finite(fearFrom)) _fearFrom = fearFrom;
            if (c) { Visual.DizzyStars.Attach(transform, _concussedLeft); RaiseStatus(StatusKind.Concussed); }
            if (f) RaiseStatus(StatusKind.Feared);
            if (d) RaiseStatus(StatusKind.Disoriented);
            if (v) RaiseStatus(StatusKind.Vulnerable);
        }
    }
}
