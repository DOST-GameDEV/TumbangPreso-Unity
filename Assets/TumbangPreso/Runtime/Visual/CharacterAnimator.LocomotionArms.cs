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
    /// ⚠️⚠️ AND THE SPREAD IS SOLVED PER BODY, BECAUSE A FIXED ONE LEFT EVERY HAND INSIDE THE HIPS. Owner, 2026-09-26: *"the
    /// walkingh animation looks so weird, hands close to body"* (ASKS-0926). The first version used one spread for every body
    /// (14 degrees walking), and `WalkArmsProbe`, which walks each body at the lens, measured every hanging fist INSIDE the
    /// body's front-on outline: -14 to -21 cm on the heroes, -36 cm on Phaister's robe (`Logs/cloud6/walk-arms/gaps_v1.csv`).
    /// The reason is in the meshes, not the numbers: every shoulder pivot sits INSIDE the torso (0.100 against a 0.158
    /// half-width on the shared body, 0.125 against 0.188 on Sean's), and the arm is a thick block (its lower face, which
    /// becomes the inner face when it hangs, is 6 to 10 cm below the pivot), so an arm swung about that pivot lays its fist
    /// against the belly. From the side, the one view the old probe photographed, none of that shows.
    ///
    /// So each arm is fitted once per model from its own bind-pose vertices (`FitArm`): the fist's inner face is solved to
    /// clear the hips at the height it hangs by `ArmClearance` of the arm's length, first by opening the spread from the
    /// base value (up to `MaxArmSpreadDegrees`), and for the part the spread cannot buy without a cowboy strut, by moving the
    /// shoulder out sideways (at most `MaxShoulderShift` of the arm's length; the arm's top still overlaps the torso, so no
    /// gap opens at the shoulder). The shift rides `_armSwingAmount`, so a hand-over to anything authored takes it away as
    /// smoothly as the swing.
    ///
    /// Handing over to anything authored (charge, throw, casts, emotes, recovery) eases out over
    /// `HandOverSeconds` rather than snapping: the charge pose is built on the clip's carry pose, so an
    /// instant hand-over would pop the lowered arm back up by 40 degrees on the first frame of a wind-up.
    /// </summary>
    public sealed partial class CharacterAnimator
    {
        /// <summary>Peak forward swing of a free arm, degrees from hanging. Back swing is 0.7 of it.</summary>
        public const float WalkArmSwingDegrees = 32f;
        public const float RunArmSwingDegrees = 55f;
        /// <summary>
        /// The spread a hanging arm STARTS from, degrees from vertical, before `FitArm` opens it for the body it is on (and the
        /// spread used as it is when a body's mesh cannot be read). Was 14 and 18 for every body.
        /// </summary>
        public const float WalkArmSpreadDegrees = 16f;
        public const float RunArmSpreadDegrees = 20f;
        /// <summary>The widest `FitArm` opens the spread before it stops and leaves the rest to the shoulder shift.</summary>
        public const float MaxArmSpreadDegrees = 30f;
        /// <summary>How far the hanging fist's inner face clears the hips, as a fraction of the arm's length (about 3.4 cm on the shared body).</summary>
        public const float ArmClearance = .05f;
        /// <summary>A shoulder shift up to this fraction of the arm's length is taken before opening the spread any further.</summary>
        public const float PreferredShoulderShift = .12f;
        /// <summary>The most the shoulder is ever moved out sideways, as a fraction of the arm's length (about 15 cm on the shared body).</summary>
        public const float MaxShoulderShift = .22f;
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
        private Vector3 _swingArmLPosRest, _swingArmRPosRest;

        /// <summary>
        /// One arm's clearance, solved once per model in the mesh's own units: the spread (degrees from vertical) and the
        /// sideways shoulder shift that put the hanging fist just outside the hips, walking and running.
        /// </summary>
        private struct ArmFit { public bool Valid; public float WalkSpread, WalkShift, RunSpread, RunShift; }
        private ArmFit _fitL, _fitR;
        /// <summary>World metres per mesh unit, from the two shoulders' distance.</summary>
        private float _armWorldScale;
        /// <summary>Read-only diagnostics for the probes: each arm's solved walking spread (degrees) and shift (metres).</summary>
        public Vector4 ArmFitDiagnostics => new Vector4(_fitL.WalkSpread, _fitL.WalkShift * _armWorldScale, _fitR.WalkSpread, _fitR.WalkShift * _armWorldScale);
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
            if (_swingArmL != null) { _swingArmL.localRotation = _swingArmLRest; _swingArmL.localPosition = _swingArmLPosRest; }
            if (_swingArmR != null) { _swingArmR.localRotation = _swingArmRRest; _swingArmR.localPosition = _swingArmRPosRest; }
            if (_swingTorso != null) _swingTorso.localRotation = _swingTorsoRest;
            _swingApplied = false;
        }

        private void ClearLocomotionArms()
        {
            RestoreLocomotionArms();
            _swingArmL = _swingArmR = _swingLegL = _swingLegR = _swingTorso = null;
            _swingBonesResolved = false;
            _fitL = _fitR = default;
            _armWorldScale = 0;
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
                SkinnedMeshRenderer armSkin = null, rightSkin = null;
                int armLIndex = -1, armRIndex = -1;
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
                            case "arm-left": if (_swingArmL == null) { _swingArmL = bone; _alongL = AlongArm(binds, i, _alongL); armSkin = skin; armLIndex = i; } break;
                            case "arm-right": if (_swingArmR == null) { _swingArmR = bone; _alongR = AlongArm(binds, i, _alongR); rightSkin = skin; armRIndex = i; } break;
                            case "leg-left": if (_swingLegL == null) _swingLegL = bone; break;
                            case "leg-right": if (_swingLegR == null) _swingLegR = bone; break;
                            case "torso": if (_swingTorso == null) _swingTorso = bone; break;
                        }
                    }
                }
                // Resolved once per graph (a model swap rebuilds the graph and clears this), so a prop with
                // no arms is not rescanned every frame.
                _swingBonesResolved = true;
                // Both arms must come from the one skin whose mesh is being read.
                FitArms(armSkin, armLIndex, rightSkin == armSkin ? armRIndex : -1);
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
            _swingArmLPosRest = _swingArmL.localPosition;
            _swingArmRPosRest = _swingArmR.localPosition;
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
            // Each arm's own fitted spread and shoulder shift (`FitArm`), or the base spread where the mesh could not be read.
            float spreadL = _fitL.Valid ? Mathf.Lerp(_fitL.WalkSpread, _fitL.RunSpread, _runWeight) : spread;
            float spreadR = _fitR.Valid ? Mathf.Lerp(_fitR.WalkSpread, _fitR.RunSpread, _runWeight) : spread;
            float shiftL = _fitL.Valid ? Mathf.Lerp(_fitL.WalkShift, _fitL.RunShift, _runWeight) * _armWorldScale : 0f;
            float shiftR = _fitR.Valid ? Mathf.Lerp(_fitR.WalkShift, _fitR.RunShift, _runWeight) * _armWorldScale : 0f;
            PoseArm(_swingArmL, _alongL, sideL, ArmSwing(sideL * stride, swing) + bias, spreadL);
            if (!carrying) PoseArm(_swingArmR, _alongR, sideR, ArmSwing(sideR * stride, swing) + bias, spreadR);
            else PoseArm(_swingArmR, _alongR, sideR,
                Mathf.Lerp(WalkCarryForwardDegrees, RunCarryForwardDegrees, _runWeight)
                + sideR * stride * Mathf.Lerp(WalkCarrySwingDegrees, RunCarrySwingDegrees, _runWeight), spreadR);
            // The shoulder moves out AFTER the rotation, in the character's frame, so the fist it carries clears the hip.
            _swingArmL.position += transform.right * (sideL * shiftL * _armSwingAmount);
            _swingArmR.position += transform.right * (sideR * shiftR * _armSwingAmount);
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

        /// <summary>
        /// Fits both arms of the visible skin (`ArmFit`). Mesh space is the bind pose, which is where the rig is T-posed and the
        /// arm's lower face (the one that turns inward when it hangs) can be read straight off Y. Each vertex belongs to its
        /// heaviest bone, which is exact for these rigidly skinned voxel bodies. A mesh that cannot be read keeps the base
        /// spread and no shift, which is what every body had before.
        /// </summary>
        private void FitArms(SkinnedMeshRenderer skin, int armL, int armR)
        {
            _fitL = _fitR = default;
            if (skin == null || armL < 0 || armR < 0 || skin.sharedMesh == null || !skin.sharedMesh.isReadable) return;
            var mesh = skin.sharedMesh;
            var vertices = mesh.vertices;
            var weights = mesh.boneWeights;
            var binds = mesh.bindposes;
            if (weights == null || weights.Length != vertices.Length || armL >= binds.Length || armR >= binds.Length) return;
            var dominant = new int[vertices.Length];
            for (int i = 0; i < vertices.Length; i++) dominant[i] = weights[i].boneIndex0;
            Vector3 pivotL = binds[armL].inverse.MultiplyPoint3x4(Vector3.zero), pivotR = binds[armR].inverse.MultiplyPoint3x4(Vector3.zero);
            float meshSpan = Mathf.Abs(pivotL.x - pivotR.x);
            if (meshSpan < 1e-4f) return;
            _armWorldScale = Vector3.Distance(_swingArmL.position, _swingArmR.position) / meshSpan;
            _fitL = FitArm(vertices, dominant, pivotL, armL, armR);
            _fitR = FitArm(vertices, dominant, pivotR, armR, armL);
        }

        /// <summary>
        /// One arm: the smallest spread from the base value at which a shoulder shift of at most `PreferredShoulderShift` clears
        /// the hips by `ArmClearance`; past `MaxArmSpreadDegrees` the shift is whatever is needed, capped at `MaxShoulderShift`.
        /// The hips are every vertex that is neither arm, in the band from just below the hanging fist to 40 per cent of an
        /// arm's length above it, on the fist's side.
        /// </summary>
        private static ArmFit FitArm(Vector3[] vertices, int[] dominant, Vector3 pivot, int arm, int otherArm)
        {
            var fit = new ArmFit();
            float side = pivot.x >= 0f ? 1f : -1f;
            float far = float.MinValue, low = float.MaxValue;
            for (int i = 0; i < vertices.Length; i++)
            {
                if (dominant[i] != arm) continue;
                far = Mathf.Max(far, vertices[i].x * side);
                low = Mathf.Min(low, vertices[i].y);
            }
            if (far == float.MinValue) return fit;
            float length = far - pivot.x * side, inner = pivot.y - low;
            if (length <= 1e-4f) return fit;
            Solve(WalkArmSpreadDegrees, out fit.WalkSpread, out fit.WalkShift);
            Solve(RunArmSpreadDegrees, out fit.RunSpread, out fit.RunShift);
            fit.Valid = true;
            return fit;

            void Solve(float from, out float spread, out float shift)
            {
                spread = from; shift = 0f;
                for (float degrees = from; degrees <= MaxArmSpreadDegrees + .01f; degrees += 1f)
                {
                    float radians = degrees * Mathf.Deg2Rad;
                    float fistY = pivot.y - length * Mathf.Cos(radians);
                    float hips = float.MinValue;
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        if (dominant[i] == arm || dominant[i] == otherArm) continue;
                        float y = vertices[i].y;
                        if (y < fistY - .1f * length || y > fistY + .4f * length) continue;
                        hips = Mathf.Max(hips, vertices[i].x * side);
                    }
                    spread = degrees;
                    if (hips == float.MinValue) { shift = 0f; return; }
                    float fistInner = pivot.x * side + length * Mathf.Sin(radians) - inner * Mathf.Cos(radians);
                    shift = Mathf.Max(0f, hips + ArmClearance * length - fistInner);
                    if (shift <= PreferredShoulderShift * length) return;
                }
                shift = Mathf.Min(shift, MaxShoulderShift * length);
            }
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
