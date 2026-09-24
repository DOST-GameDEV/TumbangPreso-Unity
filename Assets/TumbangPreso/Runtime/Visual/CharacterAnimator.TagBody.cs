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
    ///     0.16 s, home by 0.34 s.
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
        private Transform _tgTorso, _tgHead, _tgArmR, _tgArmL, _tgLegR, _tgLegL;
        private Vector3 _tgAlongR = Vector3.right, _tgAlongL = Vector3.left;
        private bool _tgResolved, _tgApplied;
        private Quaternion _tgTorsoRest, _tgHeadRest, _tgArmRRest, _tgArmLRest, _tgLegRRest, _tgLegLRest;

        private void NoteTag(string action)
        {
            if (action != "punch" && action != "lunge") return;
            _tagAction = action; _tagTime = 0;
            if (action == "lunge")
                (GetComponentInParent<CharacterSquashStretch>() ?? GetComponentInChildren<CharacterSquashStretch>())?.DashStretch(transform.forward, .2f);
        }

        private void RestoreTagBody()
        {
            if (!_tgApplied) return;
            _tgTorso.localRotation = _tgTorsoRest; _tgArmR.localRotation = _tgArmRRest; _tgArmL.localRotation = _tgArmLRest;
            if (_tgHead != null) _tgHead.localRotation = _tgHeadRest;
            if (_tgLegR != null) _tgLegR.localRotation = _tgLegRRest;
            if (_tgLegL != null) _tgLegL.localRotation = _tgLegLRest;
            _tgApplied = false;
        }

        private void ClearTagBody() { RestoreTagBody(); _tgTorso = _tgHead = _tgArmR = _tgArmL = _tgLegR = _tgLegL = null; _tgResolved = false; _tagTime = -1; }

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
            _tagTime += Time.deltaTime;
            bool lunge = _tagAction == "lunge";
            float end = lunge ? TagLungeSeconds : TagPunchSeconds;
            if (_tagTime >= end || (_motor != null && (_motor.IsStunned || _motor.IsTripped))) { _tagTime = -1; return; }
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

            // Punch: reach by 0.16 s, snap home by 0.34. Lunge: reach by 0.10, hold the live sweep to 0.38, home by 0.62.
            float w = lunge ? Envelope(_tagTime, .10f, .38f, end) : Envelope(_tagTime, .12f, .16f, end);
            if (w <= .001f) return;
            float lean = (lunge ? 34f : 16f) * w;
            float twist = (lunge ? -10f : -16f) * w;
            float side = transform.InverseTransformPoint(_tgArmR.position).x >= 0 ? 1f : -1f;

            _tgTorsoRest = _tgTorso.localRotation; _tgArmRRest = _tgArmR.localRotation; _tgArmLRest = _tgArmL.localRotation;
            if (_tgHead != null) _tgHeadRest = _tgHead.localRotation;
            if (_tgLegR != null) _tgLegRRest = _tgLegR.localRotation;
            if (_tgLegL != null) _tgLegLRest = _tgLegL.localRotation;
            _tgApplied = true;

            var up = transform.up; var right = transform.right;
            _tgTorso.rotation = Quaternion.AngleAxis(twist * side, up) * Quaternion.AngleAxis(lean, right) * _tgTorso.rotation;
            // Eyes on the target: the head does not follow the chest down.
            if (_tgHead != null) _tgHead.rotation = Quaternion.AngleAxis(-twist * side * .6f, up) * Quaternion.AngleAxis(-lean * .7f, right) * _tgHead.rotation;
            // The reaching hand: straight out at the target, chest height for the jab, low and long for the dive.
            var reach = lunge ? new Vector3(.08f, -.25f, .97f) : new Vector3(.05f, .02f, 1f);
            PointArm(_tgArmR, _tgAlongR, new Vector3(reach.x * side, reach.y, reach.z), w, 0);
            // The off arm swings back for balance.
            PointArm(_tgArmL, _tgAlongL, new Vector3(-.35f * side, -.75f, -.55f), w * .9f, 0);
            // The legs split into the dive (the off side's leg forward); a small step into the jab.
            if (_tgLegR != null && _tgLegL != null)
            {
                float split = (lunge ? 34f : 12f) * w;
                var front = side > 0 ? _tgLegL : _tgLegR; var back = side > 0 ? _tgLegR : _tgLegL;
                front.rotation = Quaternion.AngleAxis(-split, right) * front.rotation;
                back.rotation = Quaternion.AngleAxis(split * .8f, right) * back.rotation;
            }
        }
    }
}
