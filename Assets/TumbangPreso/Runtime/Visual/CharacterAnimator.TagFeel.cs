using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ THE TAG HAS TO FEEL LIKE A CATCH (owner, 2026-10-22: *"taya animation needs to be wayyy better"*,
    /// *"most of the time getting taya doesnt even look and feel like ur getting taya"*). The reach itself
    /// (`CharacterAnimator.TagBody`) now overshoots and follows through; this file adds the two beats that were
    /// missing after an ACCEPTED catch:
    ///   * GOT YOU, on the taya: once the hand has landed, a fist pump high, the chest up and a small hop.
    ///   * CAUGHT, on the victim: a whiplash recoil away from the hand with both arms flung, then a dizzy
    ///     head-circle wobble for the rest of the stun, fading out as they recover.
    /// Both are presentation only, layered over whatever clip plays, restored every frame, on every peer from
    /// the replicated tag (`MatchFlair.PlayTag`). Nothing here moves a motor or decides anything.
    /// </summary>
    public sealed partial class CharacterAnimator
    {
        // The touch is held before either beat starts, so the tag replay's settled frame shows the hand on the
        // victim rather than a fist in the air and a body already flung away (`CatchReconstruction`).
        public const float TagWinDelay = .4f, TagWinSeconds = .7f, CaughtRecoilSeconds = .6f, CaughtHoldSeconds = .2f;

        // ---------------------------------------------------------------- GOT YOU (taya)
        private float _tagWinAt = -100f;
        private bool _twApplied;
        private Transform _twTorso, _twHead, _twArmR, _twArmL, _twRoot;
        private Quaternion _twTorsoRest, _twHeadRest, _twArmRRest, _twArmLRest;
        private Vector3 _twRootRest;

        private void NoteTagWin() => _tagWinAt = Time.unscaledTime + TagWinDelay;

        private void RestoreTagWin()
        {
            if (!_twApplied) return;
            if (_twTorso != null) _twTorso.localRotation = _twTorsoRest;
            if (_twHead != null) _twHead.localRotation = _twHeadRest;
            if (_twArmR != null) _twArmR.localRotation = _twArmRRest;
            if (_twArmL != null) _twArmL.localRotation = _twArmLRest;
            if (_twRoot != null) _twRoot.localPosition = _twRootRest;
            _twApplied = false;
        }

        private void ApplyTagWin()
        {
            float t = Time.unscaledTime - _tagWinAt;
            if (t < 0 || t > TagWinSeconds || _motor == null || _motor.IsStunned || _motor.IsTripped) return;
            if (!_swingBonesResolved) ResolveSwingBones();
            _twTorso = _swingTorso; _twHead = _swingHead; _twArmR = _swingArmR; _twArmL = _swingArmL; _twRoot = _swingRoot;
            if (_twTorso == null || _twArmR == null) return;
            float e = Envelope(t, .12f, .42f, TagWinSeconds);
            if (e <= .001f) return;
            _twTorsoRest = _twTorso.localRotation; _twArmRRest = _twArmR.localRotation;
            if (_twHead != null) _twHeadRest = _twHead.localRotation;
            if (_twArmL != null) _twArmLRest = _twArmL.localRotation;
            if (_twRoot != null) _twRootRest = _twRoot.localPosition;
            _twApplied = true;

            var right = transform.right; var up = transform.up;
            float side = transform.InverseTransformPoint(_twArmR.position).x >= 0 ? 1f : -1f;
            // Chest up and a touch back, eyes up with it.
            _twTorso.rotation = Quaternion.AngleAxis(-10f * e, right) * _twTorso.rotation;
            if (_twHead != null) _twHead.rotation = Quaternion.AngleAxis(-8f * e, right) * _twHead.rotation;
            // The fist goes up: a short punch at the sky with a pulse in it.
            float pulse = 1f + .12f * Mathf.Sin(Mathf.Clamp01(t / .3f) * Mathf.PI);
            PointArm(_twArmR, _alongR, new Vector3(.22f * side, .96f * pulse, .12f), e, 0);
            if (_twArmL != null) PointArm(_twArmL, _alongL, new Vector3(-.55f * side, -.35f, .25f), e * .7f, 0);
            // The hop: up and down once inside the first third of a second, only off the ground's support.
            if (_twRoot != null && _motor.IsGrounded)
            {
                float hop = Mathf.Sin(Mathf.PI * Mathf.Clamp01((t - .04f) / .32f));
                _twRoot.position += up * (.075f * hop * e);
            }
        }

        // ---------------------------------------------------------------- CAUGHT (victim)
        private float _caughtAt = -100f;
        private Vector3 _caughtFrom = Vector3.forward;
        private bool _cbApplied;
        private Transform _cbTorso, _cbHead, _cbArmR, _cbArmL;
        private Quaternion _cbTorsoRest, _cbHeadRest, _cbArmRRest, _cbArmLRest;

        /// <summary>The accepted tag landed on this body, pushed along <paramref name="hitDirection"/> (taya to victim).</summary>
        public void PresentCaught(Vector3 hitDirection)
        {
            hitDirection.y = 0;
            if (!float.IsFinite(hitDirection.x) || !float.IsFinite(hitDirection.z)) return;
            _caughtFrom = hitDirection.sqrMagnitude > .0001f ? hitDirection.normalized : -transform.forward;
            _caughtAt = Time.unscaledTime + CaughtHoldSeconds;
        }

        private void RestoreCaught()
        {
            if (!_cbApplied) return;
            if (_cbTorso != null) _cbTorso.localRotation = _cbTorsoRest;
            if (_cbHead != null) _cbHead.localRotation = _cbHeadRest;
            if (_cbArmR != null) _cbArmR.localRotation = _cbArmRRest;
            if (_cbArmL != null) _cbArmL.localRotation = _cbArmLRest;
            _cbApplied = false;
        }

        private void ApplyCaught()
        {
            float t = Time.unscaledTime - _caughtAt;
            if (t < 0 || _motor == null) return;
            float stun = _motor.StunLeft;
            float recoil = t < CaughtRecoilSeconds ? Envelope(t, .07f, .16f, CaughtRecoilSeconds) : 0;
            // The daze takes over from the recoil and lasts the stun, gone in its last third of a second.
            float daze = Mathf.Clamp01((t - .25f) / .4f) * Mathf.Clamp01(stun / .35f);
            if (recoil <= .001f && daze <= .001f) { if (t > CaughtRecoilSeconds) _caughtAt = stun > 0 ? _caughtAt : -100f; return; }
            if (!_swingBonesResolved) ResolveSwingBones();
            _cbTorso = _swingTorso; _cbHead = _swingHead; _cbArmR = _swingArmR; _cbArmL = _swingArmL;
            if (_cbTorso == null) return;
            _cbTorsoRest = _cbTorso.localRotation;
            if (_cbHead != null) _cbHeadRest = _cbHead.localRotation;
            if (_cbArmR != null) _cbArmRRest = _cbArmR.localRotation;
            if (_cbArmL != null) _cbArmLRest = _cbArmL.localRotation;
            _cbApplied = true;

            var up = transform.up;
            // Tipped along the push: the axis is level and square to it.
            Vector3 axis = Vector3.Cross(up, _caughtFrom);
            if (axis.sqrMagnitude < .0001f) axis = transform.right;
            axis.Normalize();
            _cbTorso.rotation = Quaternion.AngleAxis(24f * recoil, axis) * _cbTorso.rotation;
            // The head whips further and a beat late.
            float whip = t < CaughtRecoilSeconds ? Envelope(Mathf.Max(0, t - .03f), .08f, .18f, CaughtRecoilSeconds) : 0;
            if (_cbHead != null) _cbHead.rotation = Quaternion.AngleAxis(18f * whip, axis) * _cbHead.rotation;
            // Arms thrown up and out by the shove.
            if (_cbArmR != null) PointArm(_cbArmR, _alongR, new Vector3(.8f, .5f, -.25f), recoil * .85f, 0);
            if (_cbArmL != null) PointArm(_cbArmL, _alongL, new Vector3(-.8f, .5f, -.25f), recoil * .85f, 0);
            if (daze > .001f)
            {
                // Seeing stars: the head rolls a slow circle and the body sways off its feet a little.
                float c = t * 7.2f;
                if (_cbHead != null)
                    _cbHead.rotation = Quaternion.AngleAxis(9f * daze * Mathf.Sin(c), transform.forward)
                        * Quaternion.AngleAxis(6f * daze * Mathf.Cos(c), transform.right) * _cbHead.rotation;
                _cbTorso.rotation = Quaternion.AngleAxis(4f * daze * Mathf.Sin(t * 3.6f), transform.forward)
                    * Quaternion.AngleAxis(7f * daze, transform.right) * _cbTorso.rotation;
            }
        }

        private void ClearTagFeel()
        {
            RestoreCaught(); RestoreTagWin();
            _twTorso = _twHead = _twArmR = _twArmL = _twRoot = null;
            _cbTorso = _cbHead = _cbArmR = _cbArmL = null;
            _tagWinAt = _caughtAt = -100f;
        }
    }
}
