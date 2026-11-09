using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ THE SLIDE HAS A POSE OF ITS OWN (owner, 2026-10-09: "sliding needs its own pose/animation"). The movement
    /// rework's slide (`CharacterMotor.ReworkSliding`) played the out-of-breath clip, a body bent over on its feet,
    /// while it shot along the ground.
    ///
    /// Drop onto the trailing hip, extend into the glide, then bring the lead foot back under
    /// the body as speed falls. Local and observed bodies drive it from the same slide flag.
    /// </summary>
    public sealed partial class CharacterAnimator
    {
        public const float SlideInSeconds = .075f, SlideOutSeconds = .22f;

        private float _slideWeight, _slideAge, _slideExtension;
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

        private void ClearSlideBody()
        {
            RestoreSlideBody();
            _slideWeight = _slideAge = _slideExtension = 0f;
        }

        private void ApplySlideBody()
        {
            if (_motor == null || _animator == null) return;
            if (_motor.IsStunned || _motor.IsTripped) { ClearSlideBody(); return; }
            bool sliding = _motor.ReworkSliding;
            float dt = Time.deltaTime;
            _slideWeight = Mathf.MoveTowards(_slideWeight, sliding ? 1f : 0f,
                                             dt / (sliding ? SlideInSeconds : SlideOutSeconds));
            if (_slideWeight <= .001f) { _slideAge = _slideExtension = 0f; return; }
            if (sliding) _slideAge += dt;
            float speedShare = Mathf.InverseLerp(1.2f, _motor.IsDefender ? 5.2f : 3.8f, _motor.ReworkFlatSpeed);
            float extension = sliding ? Mathf.SmoothStep(0, 1, _slideAge / .16f) * speedShare : 0f;
            _slideExtension = Mathf.MoveTowards(_slideExtension, extension, dt * 8f);
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
            float glide = _slideExtension;
            float reach = _legReachWorld > 0f ? _legReachWorld : .45f;
            _swingRoot.position += (-up * (reach * Mathf.Lerp(.51f, .80f, glide))
                - fwd * (reach * Mathf.Lerp(.02f, .22f, glide))) * w;
            _swingRoot.rotation = Quaternion.AngleAxis(Mathf.Lerp(-5f, -14f, glide) * w, fwd) * _swingRoot.rotation;

            // The legs, by POSITION as the gait does (the importer mirrors X): the left long ahead, the right tucked.
            float sideL = SideOf(_swingLegL, -1f), sideR = SideOf(_swingLegR, 1f);
            Transform lead = sideL < 0 ? _swingLegL : _swingLegR, tuck = sideL < 0 ? _swingLegR : _swingLegL;
            Vector3 leadAxis = sideL < 0 ? _legAxisL : _legAxisR, tuckAxis = sideL < 0 ? _legAxisR : _legAxisL;
            PoseLimb(lead, leadAxis, -1f, Mathf.Lerp(43f, 86f, glide), 7f, w);
            PoseLimb(tuck, tuckAxis, 1f, Mathf.Lerp(25f, 57f, glide), Mathf.Lerp(6f, 20f, glide), w);

            // The chest laid back and turned a little toward the trailing hand; the head up, looking down the slide.
            _swingTorso.rotation = Quaternion.AngleAxis(Mathf.Lerp(8f, -26f, glide) * w, right)
                * Quaternion.AngleAxis(12f * glide * w, up) * _swingTorso.rotation;
            if (_swingHead != null)
                _swingHead.rotation = Quaternion.AngleAxis(Mathf.Lerp(-5f, 19f, glide) * w, right)
                    * Quaternion.AngleAxis(-10f * glide * w, up) * _swingHead.rotation;

            // The arms, by position too. The right hand is down behind on the ground; the left is out ahead.
            float armSideL = SideOf(_swingArmL, -1f);
            Transform back = armSideL < 0 ? _swingArmR : _swingArmL, front = armSideL < 0 ? _swingArmL : _swingArmR;
            Vector3 backAlong = armSideL < 0 ? _alongR : _alongL, frontAlong = armSideL < 0 ? _alongL : _alongR;
            Transform backFore = armSideL < 0 ? _swingForeR : _swingForeL, frontFore = armSideL < 0 ? _swingForeL : _swingForeR;
            Vector3 backForeAlong = armSideL < 0 ? _foreAlongR : _foreAlongL, frontForeAlong = armSideL < 0 ? _foreAlongL : _foreAlongR;
            PointArm(back, backAlong, Vector3.Lerp(new Vector3(.42f, -.65f, .25f), new Vector3(.55f, -.55f, -.62f), glide), w, 0f);
            if (backFore != null) PointArm(backFore, backForeAlong, new Vector3(.30f, -.90f, -.30f), w, 0f);
            bool holding = _motor.HoldingSlipper;
            PointArm(front, frontAlong, holding ? new Vector3(-.18f, -.58f, .77f) : new Vector3(-.50f, -.04f, .86f), w, 0f);
            if (frontFore != null) PointArm(frontFore, frontForeAlong,
                holding ? new Vector3(.08f, .54f, .84f) : new Vector3(-.30f, .40f, .86f), w, 0f);
        }
    }
}
