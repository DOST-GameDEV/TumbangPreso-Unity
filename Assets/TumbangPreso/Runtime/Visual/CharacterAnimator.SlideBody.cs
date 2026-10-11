using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ THE SLIDE HAS A POSE OF ITS OWN (owner, 2026-10-09: "sliding needs its own pose/animation"). The movement
    /// rework's slide (`CharacterMotor.ReworkSliding`) played the out-of-breath clip, a body bent over on its feet,
    /// while it shot along the ground.
    ///
    /// Posed in code over whatever clip is underneath, the way the tag and the throw are (`TagBody`, `ThrowBody`), so
    /// every rig in the cast has it without thirty-odd authored clips: down on one hip with the hips nearly on the
    /// ground, the lead leg long ahead and the other tucked, the chest laid back, the head held up to look where it is
    /// going, the trailing hand down behind on the ground and the other arm out ahead for balance. It eases in over
    /// `SlideInSeconds` and out over `SlideOutSeconds`, rides a small shudder while it runs, and restores its bones
    /// every frame.
    ///
    /// Presentation only, and only on the peer that simulates the body: the slide is not replicated, so other
    /// screens do not see it (`docs/SKILL_NETWORK_CONTRACT.md`, protocol157).
    /// </summary>
    public sealed partial class CharacterAnimator
    {
        public const float SlideInSeconds = .10f, SlideOutSeconds = .18f;

        private float _slideWeight;
        private bool _slideApplied;
        private Vector3 _slRootPos;
        private Quaternion _slRootRot, _slTorso, _slHead, _slArmL, _slArmR, _slLegL, _slLegR, _slForeL, _slForeR;

        private void RestoreSlideBody()
        {
            if (!_slideApplied) return;
            if (_swingRoot != null) { _swingRoot.localPosition = _slRootPos; _swingRoot.localRotation = _slRootRot; }
            if (_swingTorso != null) _swingTorso.localRotation = _slTorso;
            if (_swingHead != null) _swingHead.localRotation = _slHead;
            if (_swingArmL != null) _swingArmL.localRotation = _slArmL;
            if (_swingArmR != null) _swingArmR.localRotation = _slArmR;
            if (_swingLegL != null) _swingLegL.localRotation = _slLegL;
            if (_swingLegR != null) _swingLegR.localRotation = _slLegR;
            if (_swingForeL != null) _swingForeL.localRotation = _slForeL;
            if (_swingForeR != null) _swingForeR.localRotation = _slForeR;
            _slideApplied = false;
        }

        private void ClearSlideBody() { RestoreSlideBody(); _slideWeight = 0f; }

        private void ApplySlideBody()
        {
            if (_motor == null || _animator == null) return;
            bool sliding = _motor.ReworkSliding && !_motor.IsStunned && !_motor.IsTripped;
            _slideWeight = Mathf.MoveTowards(_slideWeight, sliding ? 1f : 0f,
                                             Time.deltaTime / (sliding ? SlideInSeconds : SlideOutSeconds));
            if (_slideWeight <= .001f) return;
            if (!_swingBonesResolved) ResolveSwingBones();
            if (_swingRoot == null || _swingTorso == null || _swingArmL == null || _swingArmR == null
                || _swingLegL == null || _swingLegR == null) return;

            // Eased, so the drop onto the hip has a start and an end.
            float w = Mathf.SmoothStep(0f, 1f, _slideWeight);

            _slRootPos = _swingRoot.localPosition; _slRootRot = _swingRoot.localRotation;
            _slTorso = _swingTorso.localRotation;
            if (_swingHead != null) _slHead = _swingHead.localRotation;
            _slArmL = _swingArmL.localRotation; _slArmR = _swingArmR.localRotation;
            _slLegL = _swingLegL.localRotation; _slLegR = _swingLegR.localRotation;
            if (_swingForeL != null) _slForeL = _swingForeL.localRotation;
            if (_swingForeR != null) _slForeR = _swingForeR.localRotation;
            _slideApplied = true;

            // From the bind pose, so nothing the clip underneath keyed shows through.
            ToBind(_swingRoot, w, true); ToBind(_swingTorso, w, false); ToBind(_swingHead, w, false);
            ToBind(_swingArmL, w, false); ToBind(_swingArmR, w, false);
            ToBind(_swingLegL, w, false); ToBind(_swingLegR, w, false);
            ToBind(_swingForeL, w, false); ToBind(_swingForeR, w, false);

            var up = transform.up; var right = transform.right; var fwd = transform.forward;
            float shudder = Mathf.Sin(Time.time * 41f) * .006f * w;

            // The hips: nearly on the ground (the legs lie forward, so the body comes down by most of their length),
            // a little behind the feet, rolled onto the trailing hip.
            float reach = _legReachWorld > 0f ? _legReachWorld : .45f;
            _swingRoot.position += (-up * (reach * .80f) - fwd * (reach * .18f) + up * shudder) * w;
            _swingRoot.rotation = Quaternion.AngleAxis(-12f * w, fwd) * _swingRoot.rotation;

            // The legs, by POSITION as the gait does (the importer mirrors X): the left long ahead, the right tucked.
            float sideL = SideOf(_swingLegL, -1f), sideR = SideOf(_swingLegR, 1f);
            Transform lead = sideL < 0 ? _swingLegL : _swingLegR, tuck = sideL < 0 ? _swingLegR : _swingLegL;
            Vector3 leadAxis = sideL < 0 ? _legAxisL : _legAxisR, tuckAxis = sideL < 0 ? _legAxisR : _legAxisL;
            PoseLimb(lead, leadAxis, -1f, 84f, 6f, w);
            PoseLimb(tuck, tuckAxis, 1f, 62f, 16f, w);

            // The chest laid back and turned a little toward the trailing hand; the head up, looking down the slide.
            _swingTorso.rotation = Quaternion.AngleAxis(-30f * w, right) * Quaternion.AngleAxis(14f * w, up) * _swingTorso.rotation;
            if (_swingHead != null)
                _swingHead.rotation = Quaternion.AngleAxis(24f * w, right) * Quaternion.AngleAxis(-12f * w, up) * _swingHead.rotation;

            // The arms, by position too. The right hand is down behind on the ground; the left is out ahead.
            float armSideL = SideOf(_swingArmL, -1f);
            Transform back = armSideL < 0 ? _swingArmR : _swingArmL, front = armSideL < 0 ? _swingArmL : _swingArmR;
            Vector3 backAlong = armSideL < 0 ? _alongR : _alongL, frontAlong = armSideL < 0 ? _alongL : _alongR;
            Transform backFore = armSideL < 0 ? _swingForeR : _swingForeL, frontFore = armSideL < 0 ? _swingForeL : _swingForeR;
            Vector3 backForeAlong = armSideL < 0 ? _foreAlongR : _foreAlongL, frontForeAlong = armSideL < 0 ? _foreAlongL : _foreAlongR;
            PointArm(back, backAlong, new Vector3(.55f, -.55f, -.62f), w, 0f);
            if (backFore != null) PointArm(backFore, backForeAlong, new Vector3(.30f, -.90f, -.30f), w, 0f);
            PointArm(front, frontAlong, new Vector3(-.50f, .05f, .86f), w, 0f);
            if (frontFore != null) PointArm(frontFore, frontForeAlong, new Vector3(-.30f, .40f, .86f), w, 0f);
        }
    }
}
