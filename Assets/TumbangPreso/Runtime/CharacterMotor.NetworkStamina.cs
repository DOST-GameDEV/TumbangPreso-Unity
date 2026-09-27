using UnityEngine;

namespace TumbangPreso
{
    public sealed partial class CharacterMotor
    {
        private bool _netMoveHeld, _netSprintHeld, _netInteractHeld;
        private float _netResourceIntentUntil;

        internal byte ResourceIntentFlags => IsLocallySimulated()
            ? (byte)((Intent.MoveAxis.sqrMagnitude > .0001f ? 2 : 0) | (Intent.Pressed(Verb.Sprint) ? 4 : 0)
                | (Intent.Pressed(Verb.Interact) ? 8 : 0))
            : (byte)0;

        // Remote holds use only accepted, leased input. Reported visual progress
        // is never proof that a gameplay hold completed on the host.
        public bool InteractionHeldForSimulation => IsLocallySimulated() ? Intent.Pressed(Verb.Interact)
            : NetAuthority.ShouldResolve() && _netInteractHeld && Time.unscaledTime <= _netResourceIntentUntil;

        public void ApplyNetworkResourceIntent(byte flags)
        {
            if (!NetAuthority.IsHost || IsLocallySimulated()) return;
            _netMoveHeld = (flags & 2) != 0;
            _netSprintHeld = (flags & 4) != 0;
            _netInteractHeld = (flags & 8) != 0;
            _netResourceIntentUntil = Time.unscaledTime + .5f;
        }

        private void StepRemoteStamina(float dt)
        {
            if (!NetAuthority.IsHost) return;
            // Only resource clocks are authoritative here. Remote movement remains
            // the accepted owner pose, never a second physics simulation.
            bool fresh = Time.unscaledTime <= _netResourceIntentUntil;
            bool canSteer = CanMove();
            bool moving = canSteer && (IsFeared || (fresh && _netMoveHeld));
            Stamina.StepFatigue(dt);
            Stamina.Step(dt, moving, canSteer && !IsConcussed && !IsFeared && fresh && _netSprintHeld);
        }

        private void ClearNetworkResourceIntent()
        {
            _netMoveHeld = _netSprintHeld = _netInteractHeld = false;
            _netResourceIntentUntil = 0;
        }
    }
}
