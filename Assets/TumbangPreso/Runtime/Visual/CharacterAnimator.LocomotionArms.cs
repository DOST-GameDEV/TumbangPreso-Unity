using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ THE ARMS WHILE WALKING AND RUNNING ARE POSED HERE, NOT BY THE CLIPS.
    ///
    /// 🧑 2026-09-24: *"refine actual walking/running animation"*, *"my biggest issue is where the hands
    /// are and what they do when u run"*, after REFINE-2 recorded *"Sean's walking arms sticking to his
    /// body"*. Measured on `team-sean.glb` (the rig every person shares): `walk` and `sprint` hold both
    /// arms at 80 degrees down from the T, ten degrees off vertical and pressed into the blocky torso,
    /// and swing them only +/-11.8 (walk) and +/-23.6 (sprint) while the legs swing +/-38 and +/-44. And
    /// while carrying, the whole upper body is the static `holding-right` clip, so the off hand stands out
    /// at 45 degrees and never moves however fast the legs run.
    ///
    /// So after the graph evaluates, each free arm is pointed at a pose built in the CHARACTER's frame:
    /// hanging out from the torso by a spread that clears the body, swung against the opposite leg by an
    /// amplitude that matches the gait, further forward than back. The stride is read off the leg bones
    /// the graph just posed, not off a clock, so it follows the gait exactly, including backpedalling.
    ///
    /// ⚠️⚠️ THE CARRYING ARM IS LOWERED, NOT LEFT STRAIGHT OUT. `holding-right` raises the arm 60 degrees
    /// forward, so every attacker sprinted with the slipper held out at shoulder height like a sleepwalker
    /// (`LocomotionArmsProbe`'s first pictures). While moving it is carried low and a little forward at her
    /// side with a SMALL swing. The tsinelas cannot drift off it: `Carrier` (execution order 0) rides the
    /// hand AFTER this component (-50) poses it, every frame, so slipper and hand move as one. The
    /// first-person arm is a separate fixed pose (`ViewmodelArms`) and is not driven by this bone.
    ///
    /// Handing over to anything authored (charge, throw, casts, emotes, recovery) eases out over
    /// `HandOverSeconds` rather than snapping: the charge pose is built on the clip's carry pose, so an
    /// instant hand-over would pop the lowered arm back up by 40 degrees on the first frame of a wind-up.
    /// </summary>
    public sealed partial class CharacterAnimator
    {
        /// <summary>Peak forward swing of a free arm, degrees from hanging. Back swing is 0.7 of it.</summary>
        public const float WalkArmSwingDegrees = 30f;
        public const float RunArmSwingDegrees = 55f;
        /// <summary>How far a hanging arm stands out from the torso, degrees from vertical.</summary>
        public const float WalkArmSpreadDegrees = 14f;
        public const float RunArmSpreadDegrees = 18f;
        /// <summary>A runner carries the arms a little forward of hanging.</summary>
        public const float RunArmForwardDegrees = 10f;
        /// <summary>Shoulders counter-rotate against the hips.</summary>
        public const float WalkShoulderTwistDegrees = 5f;
        public const float RunShoulderTwistDegrees = 8f;
        /// <summary>The carrying arm: forward of hanging, and its small swing, walking and running.</summary>
        public const float WalkCarryForwardDegrees = 26f, RunCarryForwardDegrees = 34f;
        public const float WalkCarrySwingDegrees = 7f, RunCarrySwingDegrees = 12f;
        /// <summary>How long the arms take to hand over to an authored action.</summary>
        public const float HandOverSeconds = .08f;

        private Transform _swingArmL, _swingArmR, _swingLegL, _swingLegR, _swingTorso;
        /// <summary>Each arm's local axis that runs from the shoulder to the fist, measured off the bind pose.</summary>
        private Vector3 _alongL = Vector3.left, _alongR = Vector3.right;
        public Vector3 LeftArmAlong => _alongL;
        public Vector3 RightArmAlong => _alongR;
        public Transform SwingArmLeft => _swingArmL;
        public Transform SwingArmRight => _swingArmR;
        private Quaternion _swingArmLRest, _swingArmRRest, _swingTorsoRest;
        private bool _swingBonesResolved, _swingApplied;
        private float _armSwingAmount, _armStride;

        /// <summary>0 to 1: how much the locomotion arm swing is drawn this frame.</summary>
        public float LocomotionArmAmount => _armSwingAmount;
        /// <summary>The gait's phase, 0 to 1, shared with the first-person arms.</summary>
        public float GaitPhase => _gaitPhase;
        /// <summary>0 walking, 1 running.</summary>
        public float GaitRunWeight => _runWeight;
        /// <summary>-1 to 1: +1 with the left leg fully forward. Read off the posed legs.</summary>
        public float StrideSwing { get; private set; }
        /// <summary>Read-only diagnostics: the clip the upper body is on and the bone the layer poses.</summary>
        public string ArmSwingDiagnostics => $"{_current}|{(_swingArmL != null ? _swingArmL.GetHashCode() : 0)}|{(_swingArmR != null ? _swingArmR.GetHashCode() : 0)}|{_armSwingAmount:F2}";

        private void RestoreLocomotionArms()
        {
            if (!_swingApplied) return;
            if (_swingArmL != null) _swingArmL.localRotation = _swingArmLRest;
            if (_swingArmR != null) _swingArmR.localRotation = _swingArmRRest;
            if (_swingTorso != null) _swingTorso.localRotation = _swingTorsoRest;
            _swingApplied = false;
        }

        private void ClearLocomotionArms()
        {
            RestoreLocomotionArms();
            _swingArmL = _swingArmR = _swingLegL = _swingLegR = _swingTorso = null;
            _swingBonesResolved = false;
            _armSwingAmount = 0;
            StrideSwing = 0;
        }

        private void ApplyLocomotionArms()
        {
            if (_motor == null || _animator == null || !_graph.IsValid()) return;
            if (!_swingBonesResolved)
            {
                // ⚠️⚠️ THE BONES THE VISIBLE SKIN IS BOUND TO, NOT A NAME SEARCH. A hero body also carries a
                // hidden second rig, and searching every child by name posed THAT skeleton: the probe's
                // carrying runs showed the arm untouched on screen and measured 0 degrees of swing twice.
                // `CharacterVisual.HandAnchor`, which the carried slipper rides, is built the same way.
                foreach (var skin in _animator.GetComponentsInChildren<SkinnedMeshRenderer>(false))
                {
                    if (!skin.enabled || skin.bones == null) continue;
                    var binds = skin.sharedMesh != null ? skin.sharedMesh.bindposes : null;
                    for (int i = 0; i < skin.bones.Length; i++)
                    {
                        var bone = skin.bones[i];
                        if (bone == null) continue;
                        switch (bone.name)
                        {
                            case "arm-left": if (_swingArmL == null) { _swingArmL = bone; _alongL = AlongArm(binds, i, _alongL); } break;
                            case "arm-right": if (_swingArmR == null) { _swingArmR = bone; _alongR = AlongArm(binds, i, _alongR); } break;
                            case "leg-left": if (_swingLegL == null) _swingLegL = bone; break;
                            case "leg-right": if (_swingLegR == null) _swingLegR = bone; break;
                            case "torso": if (_swingTorso == null) _swingTorso = bone; break;
                        }
                    }
                }
                // Resolved once per graph (a model swap rebuilds the graph and clears this), so a prop with
                // no arms is not rescanned every frame.
                _swingBonesResolved = true;
            }
            if (_swingArmL == null || _swingArmR == null || _swingLegL == null || _swingLegR == null) return;

            float dt = Time.deltaTime;
            bool gaitClip = _current == Walk || _current == Sprint || _current == HoldingRight;
            bool ordinary = gaitClip && _motor.IsGrounded && !_motor.IsSwimming && !_motor.IsStunned
                && !_motor.IsTripped && !_motor.Stamina.IsFatigued && _oneShotLeft <= 0
                && !_chargePosing && _throwReleaseTime < 0 && _throwCancelTime < 0
                && _introductionBones == null && (_emote == null || !_emote.IsEmoting)
                && (_carrier == null || _carrier.ChannelRatio <= 0) && !_motor.IsEdgeRecovering;
            float target = ordinary ? Mathf.Clamp01((FlatSpeed - WalkSpeedThreshold) / .8f) : 0f;
            // Handing over to an authored action is quick; easing back in takes a moment.
            _armSwingAmount = Mathf.MoveTowards(_armSwingAmount, target, dt / (ordinary ? .15f : HandOverSeconds));
            if (_armSwingAmount <= .001f) { StrideSwing = 0; return; }

            float maxLeg = Mathf.Lerp(WalkLegSwingDegrees, RunLegSwingDegrees, _runWeight) * 2f;
            // Sides by POSITION, not by bone name: the importer mirrors X, so a name says nothing about
            // which side of the body a limb is on. +1 = the leg on the body's left is forward.
            bool swapped = SideOf(_swingLegL, -1f) > 0;
            var leftLeg = swapped ? _swingLegR : _swingLegL;
            var rightLeg = swapped ? _swingLegL : _swingLegR;
            float stride = Mathf.Clamp((LegForwardDegrees(leftLeg) - LegForwardDegrees(rightLeg)) / maxLeg, -1f, 1f);
            StrideSwing = stride;
            // The arms trail the legs by a few hundredths of a second, as loose arms do; locked to the stride
            // they read as a wind-up toy. Secondary motion, the cheapest "alive" there is.
            // ⚠️ A FAST filter with its loss given back. At 16/s it trailed nicely but also cut the sprint's swing
            // from 93 to 50 degrees (a lag filter damps its own frequency; `LocomotionArmsProbe` caught it). At
            // 30/s the lag is about 33 ms and the loss about 8 per cent at sprint cadence, restored here.
            _armStride = Mathf.Lerp(_armStride, stride, 1 - Mathf.Exp(-30f * Mathf.Max(0, dt)));
            stride = Mathf.Clamp(_armStride * 1.08f, -1f, 1f);
            bool carrying = _motor.HoldingSlipper;

            _swingArmLRest = _swingArmL.localRotation;
            _swingArmRRest = _swingArmR.localRotation;
            if (_swingTorso != null)
            {
                _swingTorsoRest = _swingTorso.localRotation;
                // Right shoulder forward with the left leg: the chest turns toward the left.
                float twist = -stride * Mathf.Lerp(WalkShoulderTwistDegrees, RunShoulderTwistDegrees, _runWeight)
                    * (carrying ? .35f : 1f) * _armSwingAmount;
                _swingTorso.rotation = Quaternion.AngleAxis(twist, transform.up) * _swingTorso.rotation;
            }

            float swing = Mathf.Lerp(WalkArmSwingDegrees, RunArmSwingDegrees, _runWeight);
            float spread = Mathf.Lerp(WalkArmSpreadDegrees, RunArmSpreadDegrees, _runWeight);
            float bias = RunArmForwardDegrees * _runWeight;
            // Each arm goes WITH the opposite leg.
            // An arm goes forward with the leg on the OTHER side: the body's right arm with its left leg.
            float sideL = SideOf(_swingArmL, -1f), sideR = SideOf(_swingArmR, 1f);
            PoseArm(_swingArmL, _alongL, sideL, ArmSwing(sideL * stride, swing) + bias, spread);
            if (!carrying) PoseArm(_swingArmR, _alongR, sideR, ArmSwing(sideR * stride, swing) + bias, spread);
            else PoseArm(_swingArmR, _alongR, sideR,
                Mathf.Lerp(WalkCarryForwardDegrees, RunCarryForwardDegrees, _runWeight)
                + sideR * stride * Mathf.Lerp(WalkCarrySwingDegrees, RunCarrySwingDegrees, _runWeight), spread);
            _swingApplied = true;
        }

        /// <summary>
        /// ⚠️ MEASURED, NOT ASSUMED. The rig is T-posed along X, and which way along X is "toward the fist"
        /// depends on the importer: Unity mirrors glTF's X, so the three.js loop's convention (arm-right -X)
        /// is backwards here, and assuming it pointed the arms' SHOULDER ends at the road. In the bind pose
        /// the arm points straight out from the body, so the axis whose direction agrees with the bone's
        /// sideways offset is the one that runs to the fist.
        /// </summary>
        private static Vector3 AlongArm(Matrix4x4[] binds, int index, Vector3 fallback)
        {
            if (binds == null || index >= binds.Length) return fallback;
            var boneToModel = binds[index].inverse;
            float side = boneToModel.MultiplyPoint3x4(Vector3.zero).x;
            float axis = boneToModel.MultiplyVector(Vector3.right).x;
            if (Mathf.Abs(side) < 1e-4f || Mathf.Abs(axis) < 1e-4f) return fallback;
            return Mathf.Sign(side) == Mathf.Sign(axis) ? Vector3.right : Vector3.left;
        }

        /// <summary>Which side of the body a shoulder is on, from where it actually sits (-1 left, +1 right).</summary>
        private float SideOf(Transform arm, float fallback)
        {
            float x = transform.InverseTransformPoint(arm.position).x;
            return Mathf.Abs(x) < 1e-3f ? fallback : Mathf.Sign(x);
        }

        /// <summary>Forward of hanging is the full swing; behind is 0.7 of it, as a real arm does.</summary>
        private static float ArmSwing(float s, float amplitude) => s * amplitude * (s >= 0 ? 1f : .7f);

        /// <summary>How far a leg points forward of straight down, degrees, in the character's frame.</summary>
        private float LegForwardDegrees(Transform leg)
        {
            var down = transform.InverseTransformDirection(leg.TransformDirection(Vector3.down));
            return Mathf.Atan2(down.z, -down.y) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Points an arm hanging `spread` degrees out to its side and `swing` degrees forward, built in the
        /// character's frame, with the smallest rotation from where the clip left it so its twist stays.
        /// </summary>
        private void PoseArm(Transform arm, Vector3 localAlong, float side, float swing, float spread)
        {
            float sp = spread * Mathf.Deg2Rad;
            var hang = new Vector3(side * Mathf.Sin(sp), -Mathf.Cos(sp), 0f);
            var local = Quaternion.AngleAxis(-swing, Vector3.right) * hang;
            var desired = transform.TransformDirection(local);
            var current = arm.TransformDirection(localAlong);
            var target = Quaternion.FromToRotation(current, desired) * arm.rotation;
            arm.rotation = Quaternion.Slerp(arm.rotation, target, _armSwingAmount);
        }
    }
}
