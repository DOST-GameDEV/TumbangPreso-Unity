using UnityEngine;

namespace TumbangPreso.Abilities
{
    public sealed partial class HeroAbilitySystem
    {
        private long _pendingUltimateRequest;
        public bool UltimateRequestPending => _pendingUltimateRequest > 0;
        private HeroKit.CastOutcome SubmitSharedUltimate()
        {
            if (Kit == null || _motor == null) return HeroKit.CastOutcome.Missing;
            if (NetAuthority.ShouldResolve())
                return AcceptSharedUltimate(new UltimateCommit(_motor.PlayerSlot, 0, _context.Position,
                    _context.Forward, _context.AimPoint, Kit.Ultimate?.HeldSecondsOnCast ?? 0));
            var allowed = Kit.CheckUltimate(_context);
            if (allowed != HeroKit.CastOutcome.Cast) return allowed;
            if (_pendingUltimateRequest > 0) return HeroKit.CastOutcome.Cooling;
            var rpc = Net.MatchRpc.Instance;
            if (rpc == null) return HeroKit.CastOutcome.CannotAct;
            _pendingUltimateRequest = rpc.RequestSharedUltimate(_motor.PlayerSlot, _context.Position,
                _context.Forward, _context.AimPoint, Kit.Ultimate.HeldSecondsOnCast);
            // No prediction, cost, clip or world effect while host acceptance is pending.
            return _pendingUltimateRequest > 0 ? HeroKit.CastOutcome.Cast : HeroKit.CastOutcome.CannotAct;
        }
        internal HeroKit.CastOutcome AcceptSharedUltimate(UltimateCommit cast)
        {
            if (!NetAuthority.ShouldResolve() || Kit?.Ultimate == null || _motor == null) return HeroKit.CastOutcome.Missing;
            var phase = SharedUltimatePhase.Ensure();
            if (phase == null || !phase.CanAccept(_motor.PlayerSlot)) return HeroKit.CastOutcome.CannotAct;
            var context = new AbilityContext(_motor, _carrier, _verbs, cast.Position, cast.Forward, cast.Aim);
            var allowed = Kit.ReserveUltimate(context);
            if (allowed != HeroKit.CastOutcome.Cast) return allowed;
            Kit.Ultimate.HeldSecondsOnCast = cast.Held;
            phase.Accept(cast); return allowed;
        }
        internal void AdoptSharedUltimate(long request)
        {
            if (Kit?.Ultimate == null) return;
            Kit.AdoptUltimateReservation();
            if (_pendingUltimateRequest == request) _pendingUltimateRequest = 0;
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
            if (NetAuthority.IsNetworked)
            { using (NetCue.SuppressRelay()) Kit.Ultimate.BeginReservedActivation(context); }
            else Kit.Ultimate.BeginReservedActivation(context);
            PlayCastConfirm(Slot.Ultimate, context, afterIntroduction: true);
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
