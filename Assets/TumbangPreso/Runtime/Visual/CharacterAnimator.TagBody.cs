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
        private Transform _tgRoot, _tgTorso, _tgHead, _tgArmR, _tgArmL, _tgForeR, _tgForeL, _tgLegR, _tgLegL;
        private Vector3 _tgAlongR = Vector3.right, _tgAlongL = Vector3.left;
        private bool _tgResolved, _tgApplied;
        private TagSoleVertex[] _tgSoleL, _tgSoleR;
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
        private bool _tagSkinContact;
        private CharacterMotor _tagSkinVictim;
        private Vector3 _tagSkinAt, _tagSkinBarycentric;
        private TagSoleVertex _tagSkinA, _tagSkinB, _tagSkinC;
        private float _tagContactUntil, _tagStartedAt = -100f;
        private Quaternion _tgTorsoRest, _tgHeadRest, _tgArmRRest, _tgArmLRest, _tgLegRRest, _tgLegLRest;
        private Quaternion _tgForeRRest, _tgForeLRest;

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
            if (_tgForeR != null) _tgForeR.localRotation = _tgForeRRest;
            if (_tgForeL != null) _tgForeL.localRotation = _tgForeLRest;
            _tgApplied = false;
        }

        private void ClearTagBody() { RestoreTagBody(); _tgRoot = _tgTorso = _tgHead = _tgArmR = _tgArmL = _tgForeR = _tgForeL = _tgLegR = _tgLegL = null; _tgSoleL = _tgSoleR = null; _tagSkinContact = false; _tagSkinVictim = null; _tagSkinA = _tagSkinB = _tagSkinC = default; _tgResolved = false; _tagTime = -1; _tagContactValid = _tagContactPending = false; _tagStartedAt = -100f; }

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
            SampleTagSkin(victim, at);
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
                            case "forearm-right": if (_tgForeR == null) _tgForeR = b; break;
                            case "forearm-left": if (_tgForeL == null) _tgForeL = b; break;
                            case "leg-right": if (_tgLegR == null) _tgLegR = b; break;
                            case "leg-left": if (_tgLegL == null) _tgLegL = b; break;
                        }
                    }
                }
                _tgResolved = true;
                _tgSoleL = TagSolePoints(_tgLegL);
                _tgSoleR = TagSolePoints(_tgLegR);
            }
            if (_tgTorso == null || _tgArmR == null || _tgArmL == null) return;
            RefreshTagSkinContact();
            var hand = GetComponent<CharacterVisual>()?.HandAnchor;

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
            if (_tgForeR != null) _tgForeRRest = _tgForeR.localRotation;
            if (_tgForeL != null) _tgForeLRest = _tgForeL.localRotation;
            _tgApplied = true;
            _tagRenderedWeight = w;

            ToBind(_tgRoot, w, true);
            ToBind(_tgTorso, w, false); ToBind(_tgHead, w, false);
            ToBind(_tgArmR, w, false); ToBind(_tgArmL, w, false);
            ToBind(_tgForeR, w, false); ToBind(_tgForeL, w, false);
            ToBind(_tgLegR, w, false); ToBind(_tgLegL, w, false);
            // Elbow rigs carry the palm below the forearm, not directly below
            // the upper arm. Aim the actual settled hand through the full chain.
            _tgPalmR = hand != null && hand.IsChildOf(_tgArmR)
                ? _tgArmR.InverseTransformPoint(hand.position) : _tgAlongR * .28f;

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
            if (_tgLegR != null && _tgLegL != null)
            {
                float sideL = SideOf(_tgLegL, -1f), sideR = SideOf(_tgLegR, 1f);
                PoseLimb(_tgLegL, _legAxisL, sideL, sideL * side < 0 ? strideAngle : -strideAngle, 0, w);
                PoseLimb(_tgLegR, _legAxisR, sideR, sideR * side < 0 ? strideAngle : -strideAngle, 0, w);
            }
            PlantTagSoles();
            // The reaching hand: straight out at the target, chest height for the jab, low and long for the dive.
            var reach = lunge ? new Vector3(.08f, -.25f, .97f) : new Vector3(.05f, .02f, 1f);
            if (_tagContactValid)
            {
                ReachAcceptedContact(w);
                if (_tgRoot != null && _motor != null && _motor.IsGrounded)
                {
                    // Solve the horizontal body step from the real arm length
                    // after grounding the feet. Short rigs cannot finish at an
                    // arbitrary half-metre cap; long arms may need a shorter step.
                    Vector3 desired = _tagContact - _tgArmR.position;
                    Vector3 planar = Vector3.ProjectOnPlane(desired, up);
                    float reachLength = Vector3.Distance(_tgArmR.position, _tgArmR.TransformPoint(_tgPalmR));
                    float height = Vector3.Dot(desired, up);
                    float horizontalReach = Mathf.Sqrt(Mathf.Max(0, reachLength * reachLength - height * height));
                    if (planar.sqrMagnitude > .0001f && desired.sqrMagnitude < 9f)
                        _tgRoot.position += planar.normalized * Mathf.Clamp(planar.magnitude - horizontalReach,
                            -Core.Balance.PunchRange, Core.Balance.PunchRange) * w;
                    ReachAcceptedContact(w);
                }
            }
            else PointArm(_tgArmR, _tgAlongR, new Vector3(reach.x * side, reach.y, reach.z), w, 0);
            // The off arm swings back for balance.
            PointArm(_tgArmL, _tgAlongL, new Vector3(-.35f * side, -.75f, -.55f), w * .9f, 0);
        }
        private readonly struct TagSoleVertex
        {
            private readonly Transform _b0, _b1, _b2, _b3;
            private readonly Vector3 _p0, _p1, _p2, _p3;
            private readonly BoneWeight _weight;
            public TagSoleVertex(Vector3 point, BoneWeight weight, Transform[] bones, Matrix4x4[] binds)
            {
                _weight = weight;
                _b0 = bones[weight.boneIndex0]; _b1 = bones[weight.boneIndex1];
                _b2 = bones[weight.boneIndex2]; _b3 = bones[weight.boneIndex3];
                _p0 = binds[weight.boneIndex0].MultiplyPoint3x4(point); _p1 = binds[weight.boneIndex1].MultiplyPoint3x4(point);
                _p2 = binds[weight.boneIndex2].MultiplyPoint3x4(point); _p3 = binds[weight.boneIndex3].MultiplyPoint3x4(point);
            }
            public Vector3 Position => _b0.TransformPoint(_p0) * _weight.weight0
                + (_weight.weight1 > 0 ? _b1.TransformPoint(_p1) * _weight.weight1 : Vector3.zero)
                + (_weight.weight2 > 0 ? _b2.TransformPoint(_p2) * _weight.weight2 : Vector3.zero)
                + (_weight.weight3 > 0 ? _b3.TransformPoint(_p3) * _weight.weight3 : Vector3.zero);
            public float Height => Position.y;
            public bool Valid => _b0 != null && (_weight.weight1 <= 0 || _b1 != null)
                && (_weight.weight2 <= 0 || _b2 != null) && (_weight.weight3 <= 0 || _b3 != null);
        }
        private void RefreshTagSkinContact()
        {
            if (!_tagContactValid || !_tagSkinContact) return;
            if (_tagSkinVictim == null || !_tagSkinA.Valid || !_tagSkinB.Valid || !_tagSkinC.Valid)
            { _tagSkinContact = false; return; }
            _tagContact = _tagSkinA.Position * _tagSkinBarycentric.x + _tagSkinB.Position * _tagSkinBarycentric.y
                + _tagSkinC.Position * _tagSkinBarycentric.z - _tagSkinVictim.transform.position + _tagSkinAt;
        }
        private void SampleTagSkin(CharacterMotor victim, Vector3 at)
        {
            _tagSkinContact = false; _tagSkinVictim = victim; _tagSkinAt = at;
            var receiver = victim.GetComponent<CharacterAnimator>();
            if (receiver == null || receiver._animator == null) return;
            Vector3 near = _tagContact + victim.transform.position - at;
            float best = float.PositiveInfinity;
            var baked = new Mesh();
            try
            {
                foreach (var skin in receiver._animator.GetComponentsInChildren<SkinnedMeshRenderer>(false))
                {
                    if (!skin.enabled || skin.sharedMesh == null || !skin.sharedMesh.isReadable) continue;
                    skin.BakeMesh(baked, true);
                    var posed = baked.vertices; var triangles = baked.triangles;
                    var vertices = skin.sharedMesh.vertices; var weights = skin.sharedMesh.boneWeights;
                    var binds = skin.sharedMesh.bindposes; var bones = skin.bones;
                    if (weights.Length != vertices.Length || posed.Length != vertices.Length) continue;
                    for (int i = 0; i < posed.Length; i++) posed[i] = skin.transform.TransformPoint(posed[i]);
                    for (int i = 0; i < triangles.Length; i += 3)
                    {
                        int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
                        Vector3 bary = TagTriangleBarycentric(near, posed[a], posed[b], posed[c]);
                        Vector3 point = posed[a] * bary.x + posed[b] * bary.y + posed[c] * bary.z;
                        float distance = (near - point).sqrMagnitude;
                        if (distance >= best) continue;
                        best = distance; _tagSkinBarycentric = bary;
                        _tagSkinA = new TagSoleVertex(vertices[a], weights[a], bones, binds);
                        _tagSkinB = new TagSoleVertex(vertices[b], weights[b], bones, binds);
                        _tagSkinC = new TagSoleVertex(vertices[c], weights[c], bones, binds);
                        _tagSkinContact = true;
                    }
                }
            }
            finally { Destroy(baked); }
        }
        private static Vector3 TagTriangleBarycentric(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 ab = b - a, ac = c - a, ap = p - a;
            float d1 = Vector3.Dot(ab, ap), d2 = Vector3.Dot(ac, ap);
            if (d1 <= 0 && d2 <= 0) return Vector3.right;
            Vector3 bp = p - b;
            float d3 = Vector3.Dot(ab, bp), d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0 && d4 <= d3) return Vector3.up;
            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0 && d1 >= 0 && d3 <= 0) { float v = d1 / (d1 - d3); return new Vector3(1 - v, v, 0); }
            Vector3 cp = p - c;
            float d5 = Vector3.Dot(ab, cp), d6 = Vector3.Dot(ac, cp);
            if (d6 >= 0 && d5 <= d6) return Vector3.forward;
            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0 && d2 >= 0 && d6 <= 0) { float w = d2 / (d2 - d6); return new Vector3(1 - w, 0, w); }
            float va = d3 * d6 - d5 * d4;
            if (va <= 0 && d4 - d3 >= 0 && d5 - d6 >= 0)
            { float w = (d4 - d3) / ((d4 - d3) + (d5 - d6)); return new Vector3(0, 1 - w, w); }
            float denominator = va + vb + vc;
            if (Mathf.Abs(denominator) < .0000001f) return Vector3.right;
            float vFace = vb / denominator, wFace = vc / denominator;
            return new Vector3(1 - vFace - wFace, vFace, wFace);
        }
        private TagSoleVertex[] TagSolePoints(Transform leg)
        {
            var result = new System.Collections.Generic.List<TagSoleVertex>();
            if (leg == null) return result.ToArray();
            foreach (var skin in _animator.GetComponentsInChildren<SkinnedMeshRenderer>(false))
            {
                if (!skin.enabled || skin.sharedMesh == null || !skin.sharedMesh.isReadable) continue;
                int index = System.Array.IndexOf(skin.bones, leg);
                var mesh = skin.sharedMesh;
                if (index < 0 || index >= mesh.bindposes.Length) continue;
                var vertices = mesh.vertices; var weights = mesh.boneWeights; var binds = mesh.bindposes; var bones = skin.bones;
                // Every posed sole vertex participates, including blended roots
                // and raised claws which can become the lowest point in a step.
                for (int i = 0; i < weights.Length; i++)
                    if (weights[i].boneIndex0 == index && weights[i].weight0 >= .5f)
                        result.Add(new TagSoleVertex(vertices[i], weights[i], bones, binds));
            }
            return result.ToArray();
        }
        private void PlantTagSoles()
        {
            if (_tgRoot == null || _motor == null || !_motor.IsGrounded) return;
            float lowest = float.PositiveInfinity;
            if (_tgSoleL != null) foreach (var point in _tgSoleL) lowest = Mathf.Min(lowest, point.Height);
            if (_tgSoleR != null) foreach (var point in _tgSoleR) lowest = Mathf.Min(lowest, point.Height);
            if (!float.IsFinite(lowest)) return;
            var capsule = _motor.GetComponent<CharacterController>();
            if (capsule == null) return;
            float scale = Mathf.Abs(_motor.transform.lossyScale.y);
            float floor = _motor.transform.position.y + (capsule.center.y - capsule.height * .5f - capsule.skinWidth) * scale;
            _tgRoot.position += Vector3.up * (floor + .005f - lowest);
        }
    }
}
