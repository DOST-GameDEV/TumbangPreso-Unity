using UnityEngine;

namespace TumbangPreso.Abilities
{
    public sealed partial class HeroAbilitySystem
    {
        private long _pendingUltimateRequest;
        private float _pendingUltimateUntil;
        public bool UltimateRequestPending => _pendingUltimateRequest > 0;
        internal bool DeliveringSharedIntroduction { get; private set; }
        private HeroKit.CastOutcome SubmitSharedUltimate()
        {
            if (Kit == null || _motor == null) return HeroKit.CastOutcome.Missing;
            var familiar = _motor.GetComponent<Visual.CharacterVisual>()?.Companion;
            if (NetAuthority.ShouldResolve())
                return AcceptSharedUltimate(new UltimateCommit(_motor.PlayerSlot, 0, _context.Position,
                    _context.Forward, SharedUltimateAim(), Kit.Ultimate?.HeldSecondsOnCast ?? 0, familiar != null, familiar != null ? familiar.transform.position : Vector3.zero));
            var allowed = Kit.CheckUltimate(_context);
            if (allowed != HeroKit.CastOutcome.Cast) return allowed;
            if (_pendingUltimateRequest > 0) return HeroKit.CastOutcome.Cooling;
            var rpc = Net.MatchRpc.Instance;
            if (rpc == null) return HeroKit.CastOutcome.CannotAct;
            _pendingUltimateUntil = Time.unscaledTime + 8;
            _pendingUltimateRequest = rpc.RequestSharedUltimate(_motor.PlayerSlot, _context.Position,
                _context.Forward, SharedUltimateAim(), Kit.Ultimate.HeldSecondsOnCast);
            // No prediction, cost, clip or world effect while host acceptance is pending.
            return _pendingUltimateRequest > 0 ? HeroKit.CastOutcome.Cast : HeroKit.CastOutcome.CannotAct;
        }
        /// <summary>
        /// The aim an ultimate is sent with. An ultimate placed where its caster looks (`HeroAbility.AimsWhereLooking`) sends the spot
        /// itself, read off THIS machine's camera, because no other peer has that camera; every other ultimate sends the context's aim.
        /// </summary>
        private Vector3 SharedUltimateAim()
            => Kit?.Ultimate != null && Kit.Ultimate.AimsWhereLooking ? AimDestination(Kit.Ultimate, _context) : _context.AimPoint;

        internal HeroKit.CastOutcome CheckSharedUltimate(UltimateCommit cast)
        {
            if (!NetAuthority.ShouldResolve() || Kit?.Ultimate == null || _motor == null) return HeroKit.CastOutcome.Missing;
            var phase = SharedUltimatePhase.Ensure();
            if (phase == null || !phase.CanAccept(_motor.PlayerSlot)) return HeroKit.CastOutcome.CannotAct;
            var context = new AbilityContext(_motor, _carrier, _verbs, cast.Position, cast.Forward, cast.Aim);
            return Kit.CheckUltimate(context);
        }
        internal HeroKit.CastOutcome AcceptSharedUltimate(UltimateCommit cast)
        {
            var check = CheckSharedUltimate(cast);
            if (check != HeroKit.CastOutcome.Cast) return check;
            var phase = SharedUltimatePhase.Instance;
            var context = new AbilityContext(_motor, _carrier, _verbs, cast.Position, cast.Forward, cast.Aim);
            var allowed = Kit.ReserveUltimate(context);
            if (allowed != HeroKit.CastOutcome.Cast) return allowed;
            Kit.Ultimate.HeldSecondsOnCast = cast.Held;
            phase.Accept(cast); return allowed;
        }
        internal void AcknowledgeSharedUltimate(long request)
        { if (_pendingUltimateRequest == request) _pendingUltimateRequest = 0; }
        internal void AdoptSharedUltimate(long request)
        {
            if (Kit?.Ultimate == null) return;
            Kit.AdoptUltimateReservation();
            AcknowledgeSharedUltimate(request);
        }
        internal void RefuseSharedUltimate(long request)
        {
            if (request <= 0 || request != _pendingUltimateRequest) return;
            _pendingUltimateRequest = 0; _answer[(int)Slot.Ultimate] = HeroKit.CastOutcome.CannotAct;
            _answeredAt[(int)Slot.Ultimate] = Time.time; PlayRefusal();
        }
        internal void ExecuteSharedUltimate(UltimateCommit cast, bool themePlayed)
        {
            if (Kit?.Ultimate == null || !Kit.Ultimate.ReservedForIntroduction) return;
            var context = new AbilityContext(_motor, _carrier, _verbs, cast.Position, cast.Forward, cast.Aim);
            Kit.Ultimate.HeldSecondsOnCast = cast.Held;
            if (cast.HasFamiliar) _motor.GetComponent<Visual.CharacterVisual>()?.Companion?.ApplyCastAnchor(cast.FamiliarPosition);
            if (NetAuthority.IsNetworked)
            { using (NetCue.SuppressRelay()) Kit.Ultimate.BeginReservedActivation(context); }
            else Kit.Ultimate.BeginReservedActivation(context);
            DeliveringSharedIntroduction = true;
            try { PlayCastConfirm(Slot.Ultimate, context, afterIntroduction: true); }
            finally { DeliveringSharedIntroduction = false; }
        }
        internal void CancelSharedUltimate()
        { Kit?.Ultimate?.CancelIntroductionReservation(); _pendingUltimateRequest = 0; }
        internal void ClearPresentationInput()
        {
            _skill1BufferedAt = _skill2BufferedAt = _ultimateBufferedAt = float.NegativeInfinity;
            for (int i = 0; i < _heldSince.Length; i++) _heldSince[i] = -1;
            _reticle?.Hide();
        }
    }
}
