using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// ⚠️⚠️ POSSESSED, Nemu's PRUSISYON (owner 2026-10-11, `docs/reports/nemu-ultimate-2026-10-11/direction.md`
    /// section 2). A ghost rides the body for 15 s and ADDS its pull to the player's steering (`NemuRules.Contest`),
    /// the way a Left 4 Dead 2 survivor fights a Jockey. It is never a stun: every verb, the camera and the stamina
    /// rules stay the player's.
    ///
    ///  * The HOST owns the clock (`ApplyPossessed`); `SyncUnit` carries it (`PossessedLeft`) like every timed status.
    ///  * The body's own SIMULATING peer applies the contest in the motor step (`PossessedWish`), because that peer
    ///    moves the body. Bots are simulated by the host and press the same buttons, so they fight it the same way.
    ///  * Everyone else only DRAWS it: `PossessionPull` is a pure function of the seat, the replicated clock and
    ///    replicated positions, so every screen leans the rider the same way (`Visual.PossessedPresence`).
    ///  * Grip is the simulating peer's own, like the stick, and never decides an outcome on the host.
    /// </summary>
    public sealed partial class CharacterMotor
    {
        private float _possessedLeft, _possessionGrip = 1.0f;
        private bool _possessionResisting;

        public float PossessedLeft => _possessedLeft;
        public bool IsPossessed => _possessedLeft > 0.0f;

        /// <summary>This body is the person playing on this machine (the solo seat offline, the local seat online): whose
        /// own screen gets the possessed treatment and whose own ghost is hidden from their lens.</summary>
        public bool IsLocalViewer => IsLocalHuman;

        /// <summary>Seconds into the current possession (a refresh starts it again, like its rider).</summary>
        public float PossessionElapsed => IsPossessed ? NemuRules.PossessedSeconds - _possessedLeft : -1.0f;

        /// <summary>How hard the ghost is holding on, 0.45 to 1. Exact on the simulating peer; 1 elsewhere.</summary>
        public float PossessionGrip => _possessionGrip;

        /// <summary>True on the simulating peer while the player is pushing against the pull.</summary>
        public bool PossessionResisting => _possessionResisting;

        /// <summary>
        /// POSSESSED for the full 15 s (`Max`, never added). Refused by status immunity and only written where
        /// gameplay state may be (the host, or a local probe). The rider is attached for presentation on every peer
        /// from the status edge, so it is not attached here.
        /// </summary>
        public void ApplyPossessed(float seconds = NemuRules.PossessedSeconds)
        {
            if (!MayMutateGameplayState() || StatusImmune) return;
            bool fresh = _possessedLeft <= 0.0f;
            _possessedLeft = StatusRules.Refresh(_possessedLeft, Mathf.Min(seconds, NemuRules.PossessedSeconds));
            if (fresh) BeginPossession();
        }

        /// <summary>The host's replicated clock. Bounded and finite (the receiver has already checked).</summary>
        public void ApplyNetworkPossessed(float left)
        {
            bool fresh = _possessedLeft <= 0.0f && left > 0.0f;
            _possessedLeft = float.IsFinite(left) ? Mathf.Clamp(left, 0.0f, NemuRules.PossessedSeconds) : 0.0f;
            if (fresh) BeginPossession();
        }

        private void BeginPossession()
        {
            _possessionGrip = 1.0f;
            _possessionResisting = false;
            Visual.PossessedPresence.Attach(this);
            StatusGained?.Invoke(this, StatusKind.Possessed);
        }

        private void StepPossession(float dt)
        {
            if (_possessedLeft <= 0.0f) return;
            _possessedLeft = Mathf.Max(0.0f, _possessedLeft - dt);
            if (_possessedLeft <= 0.0f) { _possessionGrip = 1.0f; _possessionResisting = false; }
        }

        private void ClearPossession()
        {
            _possessedLeft = 0.0f;
            _possessionGrip = 1.0f;
            _possessionResisting = false;
        }

        /// <summary>
        /// Where the ghost wants this body, as a share of a full walk in world space, before grip. Zero when it is not
        /// possessed, when there is nowhere to pull it, or when a stronger status already owns its legs.
        /// </summary>
        public bool PossessionPull(out Vector3 pull)
        {
            pull = Vector3.zero;
            if (!IsPossessed || IsStunned || IsFeared || IsRooted) return false;
            var round = GameServices.Round;
            if (round == null) return false;
            CharacterMotor taya = null;
            var players = round.Players;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (p != null && p != this && p.IsDefender && p.gameObject.activeInHierarchy) { taya = p; break; }
            }
            Vector3 can = round.Lata != null ? round.Lata.transform.position : Vector3.zero;
            Vector3 at = transform.position;
            Vector3 tayaAt = taya != null ? taya.transform.position : Vector3.zero;
            if (!NemuRules.Lure(_playerSlot, at.x, at.z, IsDefender, taya != null, tayaAt.x, tayaAt.z,
                                can.x, can.z, out float lx, out float lz)) return false;
            NemuRules.Pull(PossessionElapsed, _playerSlot, lx, lz, out float px, out float pz);
            pull = new Vector3(px, 0.0f, pz);
            return pull.sqrMagnitude > 1e-6f;
        }

        /// <summary>True while the ghost is actually moving this body (for the stamina and animation `moving` flag).</summary>
        public bool IsPossessionPulling => PossessionPull(out _);

        /// <summary>
        /// THE CONTEST, on the simulating peer: the steered wish (world, at most 1) plus the gripped pull, clamped. Grip
        /// wears down while the player fights and recovers otherwise.
        /// </summary>
        private Vector3 PossessedWish(Vector3 wish, float dt)
        {
            if (!PossessionPull(out var pull)) { _possessionResisting = false; return wish; }
            _possessionResisting = NemuRules.Resisting(wish.x, wish.z, pull.x, pull.z);
            _possessionGrip = NemuRules.StepGrip(_possessionGrip, _possessionResisting, dt);
            NemuRules.Contest(wish.x, wish.z, pull.x, pull.z, _possessionGrip, out float wx, out float wz);
            return new Vector3(wx, 0.0f, wz);
        }
    }
}
