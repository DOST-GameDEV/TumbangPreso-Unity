using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ THE TAYA'S TAG IS A REACH TO TOUCH, NOT A KICK OR A HAMMER.
    ///
    /// 🧑 2026-09-24: *"make tagging better too"*. Measured on the shared rig, the dash tag (`lunge`) played
    /// `attack-kick-right` (the right leg swinging 104 degrees) and the quick tag (`punch`) played
    /// `attack-melee-right` (the arm up to 169 degrees and chopped down). Neither is a tag. A tag is a touch:
    /// what it must say to the attacker being chased is "that hand is coming for you, now it has reached you"
    /// (`docs/reports/gameplay-animation-2026-09-24/research-and-analysis.md`).
    ///
    ///   * `punch`, the quick tag: step in and lean, the strong arm straight out at chest height toward the
    ///     target, the off arm back for balance; snapped home. The first-person `PunchClip`'s timing: reach by
    ///     0.12 s, a visible touch through0.22 s, home by0.34 s.
    ///   * `lunge`, the dash tag: a whole-body dive, deep lean, the reaching arm fully out and low, the legs
    ///     split. It HOLDS the reach while the sweep can still land (`LungeClip`'s own note: an arm still out
    ///     tells the truth about a tag that can still land) and recovers by 0.62 s. The body stretches as it
    ///     launches.
    ///
    /// Posed by where each limb points in the character's frame, over whatever clip is underneath, restored
    /// every frame. Presentation only: the tag's reach, timing and resolution are `CombatVerbs`'.
    /// </summary>
    public sealed partial class CharacterAnimator
    {
        public const float TagPunchSeconds = .34f, TagLungeSeconds = .62f;

        private string _tagAction;
        private float _tagTime = -1;
        private Transform _tgRoot, _tgTorso, _tgHead, _tgArmR, _tgArmL, _tgLegR, _tgLegL;
        private Vector3 _tgAlongR = Vector3.right, _tgAlongL = Vector3.left;
        private bool _tgResolved, _tgApplied;
        private float _tagRenderedWeight;
        private int _tagContactSubject = -1;
        // History reads the pose that was actually displayed, including a peak
        // preserved across a slow frame. This never drives the live gesture.
        internal float RecordedTagContactWeight => _tgApplied && _tagContactValid ? _tagRenderedWeight : 0;
        internal int RecordedTagContactSubject => _tgApplied && _tagContactValid ? _tagContactSubject : -1;
        private Vector3 _tgRootPositionRest;
        private Quaternion _tgRootRotationRest;
        private Vector3 _tgPalmR, _tgArmScaleRest, _tagContact;
        private bool _tagContactValid, _tagContactPending;
        private float _tagContactUntil, _tagStartedAt = -100f;
        private Quaternion _tgTorsoRest, _tgHeadRest, _tgArmRRest, _tgArmLRest, _tgLegRRest, _tgLegLRest;

        private void NoteTag(string action)
        {
            if (action != "punch" && action != "lunge") return;
            _tagAction = action; _tagTime = 0;
            _tagContactValid = _tagContactPending && Time.unscaledTime <= _tagContactUntil;
            _tagContactPending = false; _tagStartedAt = Time.unscaledTime;
            if (action == "lunge")
                (GetComponentInParent<CharacterSquashStretch>() ?? GetComponentInChildren<CharacterSquashStretch>())?.DashStretch(transform.forward, .2f);
        }

        private void RestoreTagBody()
        {
            if (!_tgApplied) return;
            _tgTorso.localRotation = _tgTorsoRest; _tgArmR.localRotation = _tgArmRRest; _tgArmL.localRotation = _tgArmLRest;
            _tgArmR.localScale = _tgArmScaleRest;
            if (_tgRoot != null) { _tgRoot.localPosition = _tgRootPositionRest; _tgRoot.localRotation = _tgRootRotationRest; }
            if (_tgHead != null) _tgHead.localRotation = _tgHeadRest;
            if (_tgLegR != null) _tgLegR.localRotation = _tgLegRRest;
            if (_tgLegL != null) _tgLegL.localRotation = _tgLegLRest;
            _tgApplied = false;
        }

        private void ClearTagBody() { RestoreTagBody(); _tgRoot = _tgTorso = _tgHead = _tgArmR = _tgArmL = _tgLegR = _tgLegL = null; _tgResolved = false; _tagTime = -1; _tagContactValid = _tagContactPending = false; _tagStartedAt = -100f; }

        // The already accepted, replicated tag supplies the contact. Misses keep
        // the ordinary reach; this cannot award a hit or move either live motor.
        public void PresentTagContact(CharacterMotor victim, Vector3 at)
        {
            if (victim == null || _motor == null || !_motor.IsDefender || !Core.CompanionSeats.IsPlayer(_motor.PlayerSlot)
                || float.IsNaN(at.x) || float.IsNaN(at.y) || float.IsNaN(at.z)
                || float.IsInfinity(at.x) || float.IsInfinity(at.y) || float.IsInfinity(at.z)) return;
            var capsule = victim.GetComponent<CharacterController>();
            _tagContactSubject = victim.PlayerSlot;
            var torso = victim.GetComponent<CharacterVisual>()?.TorsoBone;
            float height = torso != null ? torso.position.y - victim.transform.position.y + .12f
                : capsule != null ? capsule.center.y : .8f;
            Vector3 toward = transform.position - at; toward.y = 0;
            float radius = capsule != null ? capsule.radius * .70f : .28f;
            _tagContact = at + Vector3.up * Mathf.Clamp(height, .3f, 1.2f)
                + (toward.sqrMagnitude > .001f ? toward.normalized * radius : Vector3.zero);
            _tagContactUntil = Time.unscaledTime + .35f;
            _tagContactValid = _tagTime >= 0;
            // An old receipt after a completed gesture cannot aim the next miss.
            // A genuinely preceding event may still pair with its upcoming action.
            float cooldown = _tagAction == "lunge" ? Core.Balance.LungeCooldown : Core.Balance.PunchCooldown;
            _tagContactPending = !_tagContactValid && Time.unscaledTime - _tagStartedAt >= cooldown;
        }

        private void ReachAcceptedContact(float weight)
        {
            if (_tgPalmR.sqrMagnitude < .00001f) return;
            Vector3 desired = _tagContact - _tgArmR.position;
            Vector3 current = _tgArmR.TransformPoint(_tgPalmR) - _tgArmR.position;
            if (desired.sqrMagnitude < .0001f || desired.sqrMagnitude > 9f || current.sqrMagnitude < .0001f) return;
            float extension = Mathf.Clamp(desired.magnitude / current.magnitude, .90f, 1.10f);
            Vector3 scale = _tgArmScaleRest;
            Vector3 axis = new Vector3(Mathf.Abs(_tgPalmR.x), Mathf.Abs(_tgPalmR.y), Mathf.Abs(_tgPalmR.z));
            int index = axis.x >= axis.y && axis.x >= axis.z ? 0 : axis.y >= axis.z ? 1 : 2;
            scale[index] *= extension;
            _tgArmR.localScale = Vector3.Lerp(_tgArmScaleRest, scale, weight);
            current = _tgArmR.TransformPoint(_tgPalmR) - _tgArmR.position;
            var aimed = Quaternion.FromToRotation(current, desired) * _tgArmR.rotation;
            _tgArmR.rotation = Quaternion.Slerp(_tgArmR.rotation, aimed, weight);
        }

        /// <summary>0 to 1 to 0: rises by `reach`, holds to `hold`, returns by `end`.</summary>
        private static float Envelope(float t, float reach, float hold, float end)
        {
            if (t < reach) { float u = t / reach; return 1 - (1 - u) * (1 - u) * (1 - u); }
            if (t < hold) return 1;
            return 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(hold, end, t));
        }

        private void ApplyTagBody()
        {
            if (_tagTime < 0 || _animator == null) return;
            float previousTime = _tagTime;
            _tagTime += Time.deltaTime;
            bool lunge = _tagAction == "lunge";
            float end = lunge ? TagLungeSeconds : TagPunchSeconds;
            float reachTime = lunge ? .10f : .12f;
            float holdTime = lunge ? Core.Balance.LungeActiveTime : .22f;
            // A slow frame must not skip the only visible touch. Show its peak
            // once if this step crossed the entire contact window; the real
            // gesture clock still advances and recovers on the following frame.
            bool skippedTouch = previousTime < reachTime && _tagTime > holdTime;
            if ((_tagTime >= end && !skippedTouch) || (_motor != null && (_motor.IsStunned || _motor.IsTripped))) { _tagTime = -1; return; }
            if (!_tgResolved)
            {
                foreach (var skin in _animator.GetComponentsInChildren<SkinnedMeshRenderer>(false))
                {
                    if (!skin.enabled || skin.bones == null) continue;
                    var binds = skin.sharedMesh != null ? skin.sharedMesh.bindposes : null;
                    for (int i = 0; i < skin.bones.Length; i++)
                    {
                        var b = skin.bones[i];
                        if (b == null) continue;
                        switch (b.name)
                        {
                            case "torso": if (_tgTorso == null) _tgTorso = b; break;
                            case "head": if (_tgHead == null) _tgHead = b; break;
                            case "arm-right": if (_tgArmR == null) { _tgArmR = b; _tgAlongR = AlongArm(binds, i, _tgAlongR); } break;
                            case "arm-left": if (_tgArmL == null) { _tgArmL = b; _tgAlongL = AlongArm(binds, i, _tgAlongL); } break;
                            case "leg-right": if (_tgLegR == null) _tgLegR = b; break;
                            case "leg-left": if (_tgLegL == null) _tgLegL = b; break;
                        }
                    }
                }
                _tgResolved = true;
            }
            if (_tgTorso == null || _tgArmR == null || _tgArmL == null) return;
            var hand = GetComponent<CharacterVisual>()?.HandAnchor;
            _tgPalmR = hand != null && hand.parent == _tgArmR ? hand.localPosition : _tgAlongR * .28f;

            // Keep the touch legible, then recover on the existing clip clock.
            // The lunge holds for its actual live sweep, including late contacts.
            float w = Envelope(skippedTouch ? reachTime : _tagTime, reachTime, holdTime, end);
            if (w <= .001f) return;
            // The shoulder travels because the body commits to the reach. Do not
            // inherit the old melee clip's backwards wind-up under this pose.
            float commitment = 1f;
            if (_tagContactValid)
            {
                Vector3 toContact = _tagContact - transform.position; toContact.y = 0;
                commitment = Mathf.InverseLerp(.65f, 1.35f, toContact.magnitude);
            }
            float lean = (lunge ? 44f : Mathf.Lerp(22f, 28f, commitment)) * w;
            float twist = (lunge ? -28f : -42f) * w;
            float side = transform.InverseTransformPoint(_tgArmR.position).x >= 0 ? 1f : -1f;

            _tgTorsoRest = _tgTorso.localRotation; _tgArmRRest = _tgArmR.localRotation; _tgArmLRest = _tgArmL.localRotation;
            _tgArmScaleRest = _tgArmR.localScale;
            if (!_swingBonesResolved) ResolveSwingBones();
            _tgRoot = _swingRoot;
            if (_tgRoot != null) { _tgRootPositionRest = _tgRoot.localPosition; _tgRootRotationRest = _tgRoot.localRotation; }
            if (_tgHead != null) _tgHeadRest = _tgHead.localRotation;
            if (_tgLegR != null) _tgLegRRest = _tgLegR.localRotation;
            if (_tgLegL != null) _tgLegLRest = _tgLegL.localRotation;
            _tgApplied = true;
            _tagRenderedWeight = w;

            ToBind(_tgRoot, w, true);
            ToBind(_tgTorso, w, false); ToBind(_tgHead, w, false);
            ToBind(_tgArmR, w, false); ToBind(_tgArmL, w, false);
            ToBind(_tgLegR, w, false); ToBind(_tgLegL, w, false);

            var up = transform.up; var right = transform.right;
            // Weight moves over the forward foot. The rear sole stays planted:
            // with this rigid seven-bone rig, lowering the hips by the cosine
            // loss keeps the split stance on the court without stretching legs.
            float strideAngle = (lunge ? 42f : Mathf.Lerp(18f, 38f, commitment)) * w;
            if (_tgRoot != null && _legReachWorld > 0f && _motor != null && _motor.IsGrounded)
            {
                float angle = strideAngle * Mathf.Deg2Rad;
                _tgRoot.position += transform.forward * (_legReachWorld * Mathf.Sin(angle))
                    - up * (_legReachWorld * (1f - Mathf.Cos(angle)));
            }
            _tgTorso.rotation = Quaternion.AngleAxis(twist * side, up) * Quaternion.AngleAxis(lean, right) * _tgTorso.rotation;
            // Eyes on the target: the head does not follow the chest down.
            if (_tgHead != null) _tgHead.rotation = Quaternion.AngleAxis(-twist * side * .6f, up) * Quaternion.AngleAxis(-lean * .7f, right) * _tgHead.rotation;
            // The reaching hand: straight out at the target, chest height for the jab, low and long for the dive.
            var reach = lunge ? new Vector3(.08f, -.25f, .97f) : new Vector3(.05f, .02f, 1f);
            if (_tagContactValid)
            {
                ReachAcceptedContact(w);
                if (_tgRoot != null && _motor != null && _motor.IsGrounded)
                {
                    // Shorter authored arms need the body to finish the accepted step,
                    // rather than a longer limb. This render-only offset restores next frame.
                    Vector3 shortfall = Vector3.ProjectOnPlane(_tagContact - _tgArmR.TransformPoint(_tgPalmR), up);
                    if (Vector3.Dot(shortfall, transform.forward) > 0)
                        _tgRoot.position += Vector3.ClampMagnitude(shortfall, .5f) * w;
                    ReachAcceptedContact(w);
                }
            }
            else PointArm(_tgArmR, _tgAlongR, new Vector3(reach.x * side, reach.y, reach.z), w, 0);
            // The off arm swings back for balance.
            PointArm(_tgArmL, _tgAlongL, new Vector3(-.35f * side, -.75f, -.55f), w * .9f, 0);
            // The free foot steps ahead as the opposite foot pushes off. The
            // reaching-side shoulder rolls forward while the off arm balances.
            if (_tgLegR != null && _tgLegL != null)
            {
                float sideL = SideOf(_tgLegL, -1f), sideR = SideOf(_tgLegR, 1f);
                PoseLimb(_tgLegL, _legAxisL, sideL, sideL * side < 0 ? strideAngle : -strideAngle, 0, w);
                PoseLimb(_tgLegR, _legAxisR, sideR, sideR * side < 0 ? strideAngle : -strideAngle, 0, w);
            }
        }
    }
}
