using UnityEngine;

namespace TumbangPreso
{
    public sealed partial class CharacterMotor
    {
        private bool _netMoveHeld, _netSprintHeld, _netInteractHeld;
        private float _netResourceIntentUntil;

        // ⚠️ BITS 16 AND 32: THE CROUCH AND THE SLIDE, FOR THE OTHER SCREENS (owner, 2026-10-09, of the slide's pose: "the
        // issue is it doesnt show up on other people. it shows to you but not to others"). The movement rework's
        // crouch and slide lived only on the peer that simulates the body, so everybody else saw a body walking at a
        // slide's speed. They ride the two spare bits of this byte, which already travels with every pose both ways:
        // the owner sets them, the host keeps the last it was sent (`ApplyNetworkResourceIntent`) and passes them on in
        // its own poses of that body, and a replica reads them (`ApplyNetworkMovePose`). PRESENTATION ONLY: nobody's
        // capsule, speed or stamina is decided by them, and a peer that does not know the bits ignores them, so the
        // protocol number does not move.
        private const byte CrouchedBit = 16, SlidingBit = 32;
        private bool _netCrouched, _netSliding;
        private float _netMovePoseUntil;

        internal byte ResourceIntentFlags => IsLocallySimulated()
            ? (byte)((Intent.MoveAxis.sqrMagnitude > .0001f ? 2 : 0) | (Intent.Pressed(Verb.Sprint) ? 4 : 0)
                | (Intent.Pressed(Verb.Interact) ? 8 : 0)
                | (_rwCrouched ? CrouchedBit : 0) | (_rwSliding ? SlidingBit : 0))
            // A body this peer does not simulate: the host passes on what the owner last said of its crouch and slide.
            : (byte)(Time.unscaledTime <= _netMovePoseUntil
                ? (_netCrouched ? CrouchedBit : 0) | (_netSliding ? SlidingBit : 0) : 0);

        /// <summary>A replica's crouch and slide, as its owner last reported them. Ignored for a body simulated here.</summary>
        public void ApplyNetworkMovePose(byte flags)
        {
            if (IsLocallySimulated()) return;
            _netCrouched = (flags & CrouchedBit) != 0;
            _netSliding = (flags & SlidingBit) != 0;
            _netMovePoseUntil = Time.unscaledTime + .5f;
        }

        private bool NetCrouched => _netCrouched && Time.unscaledTime <= _netMovePoseUntil;
        private bool NetSliding => _netSliding && Time.unscaledTime <= _netMovePoseUntil;

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
            ApplyNetworkMovePose(flags);
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
            _netCrouched = _netSliding = false; _netMovePoseUntil = 0;
        }
    }
}
