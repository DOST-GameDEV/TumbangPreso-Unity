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
    /// ⚠️⚠️ SECOND PASS, SAME DAY: THE ARMS HANG STRAIGHT DOWN BESIDE THE TORSO, AND THE BODY TAKES ITS WEIGHT. The owner on
    /// the first fit's frames: *"thihs walk still sucks pls imrpove still on all"*. The fit had bought its clearance mostly
    /// with SPREAD (19 to 30 degrees), so every body walked with its arms held out in an A like a penguin, and nothing else
    /// moved: no bob, no weight shift. A blocky figure's arm reads as an arm when it hangs nearly vertical and FLUSH against
    /// the side of the torso block and swings in a big arc fore and aft, so the fit now starts from 5 degrees (8 running), puts
    /// the arm's inner face flush with the torso at the shoulder by moving the shoulder out (`TopClearance`), and opens the
    /// spread only if the fist would still land inside the hips (a robe). And `ApplyFootPlant` keeps the LOWER foot on the
    /// ground: these legs have no knee, so with the root fixed both feet lifted 21 per cent of the leg's length off the court
    /// at every contact (1 - cos 38 degrees) and the body floated through the walk. Dropping the root by exactly that puts the
    /// weight down on each step, which is also the bob. The lean and the side-to-side weight shift are in
    /// `CharacterAnimator.LocomotionWeight.cs`.
    ///
    /// Handing over to anything authored (charge, throw, casts, emotes, recovery) eases out over
    /// `HandOverSeconds` rather than snapping: the charge pose is built on the clip's carry pose, so an
    /// instant hand-over would pop the lowered arm back up by 40 degrees on the first frame of a wind-up.
    /// </summary>
    public sealed partial class CharacterAnimator
    {
        /// <summary>Peak forward swing of a free arm, degrees from hanging. Back swing is 0.7 of it.</summary>
        public const float WalkArmSwingDegrees = 36f;
        public const float RunArmSwingDegrees = 58f;
        /// <summary>
        /// The spread a hanging arm STARTS from, degrees from vertical, before `FitArm` opens it for the body it is on (and the
        /// spread used as it is when a body's mesh cannot be read). Was 14 and 18 for every body, then 16 and 20 opening to 30
        /// (the penguin; see the second-pass note above).
        /// </summary>
        public const float WalkArmSpreadDegrees = 5f;
        public const float RunArmSpreadDegrees = 8f;
        /// <summary>The widest `FitArm` opens the spread, and only for a fist that would otherwise land inside the hips.</summary>
        public const float MaxArmSpreadDegrees = 14f;
        /// <summary>The arm's inner face at the shoulder against the torso's side: 0 is flush, touching, never a gap.</summary>
        public const float TopClearance = 0f;
        /// <summary>How far the hanging fist's inner face clears the hips, as a fraction of the arm's length.</summary>
        public const float ArmClearance = .03f;
        /// <summary>The most the shoulder is ever moved out sideways, as a fraction of the arm's length (Phaister's robe needs 0.79).</summary>
        public const float MaxShoulderShift = .8f;
        /// <summary>How much of the knee-less legs' contact lift the root gives back (1 keeps the lower foot exactly on the court).</summary>
        public const float FootPlantShare = 1f;
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
        private Vector3 _swingArmLPosRest, _swingArmRPosRest, _swingRootPosRest;
        private Transform _swingRoot;
        /// <summary>Hip pivot to sole, in world metres, for the foot plant.</summary>
        private float _legReachWorld;
        private float _lastStride, _strideRate, _lastGaitPhase, _gaitCycleRate;
        /// <summary>-1 to 1: +1 while the body's LEFT leg bears the weight (mid-stance), -1 for the right. Read by the weight shift.</summary>
        public float StanceSide { get; private set; }
        /// <summary>World metres the foot plant lowered the body this frame (diagnostics).</summary>
        public float FootPlantDrop { get; private set; }

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
            if (_swingRoot != null) _swingRoot.localPosition = _swingRootPosRest;
            if (_swingTorso != null) _swingTorso.localRotation = _swingTorsoRest;
            _swingApplied = false;
        }

        private void ClearLocomotionArms()
        {
            RestoreLocomotionArms();
            _swingArmL = _swingArmR = _swingLegL = _swingLegR = _swingTorso = _swingRoot = null;
            _legReachWorld = 0; StanceSide = 0; FootPlantDrop = 0;
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
                            case "root": if (_swingRoot == null && skin == armSkin) _swingRoot = bone; break;
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
            if (_armSwingAmount <= .001f) { StrideSwing = 0; StanceSide = 0; FootPlantDrop = 0; return; }

            float maxLeg = Mathf.Lerp(WalkLegSwingDegrees, RunLegSwingDegrees, _runWeight) * 2f;
            // Sides by POSITION, not by bone name: the importer mirrors X, so a name says nothing about
            // which side of the body a limb is on. +1 = the leg on the body's left is forward.
            bool swapped = SideOf(_swingLegL, -1f) > 0;
            var leftLeg = swapped ? _swingLegR : _swingLegL;
            var rightLeg = swapped ? _swingLegL : _swingLegR;
            float legLeft = LegForwardDegrees(leftLeg), legRight = LegForwardDegrees(rightLeg);
            float stride = Mathf.Clamp((legLeft - legRight) / maxLeg, -1f, 1f);
            StrideSwing = stride;
            // Which leg bears the weight: the stance leg is the one travelling BACK under the body, so the left leg is in
            // stance while the stride (+1 left forward) is falling. Normalised by the cycle's own angular rate, a sine's
            // derivative over its frequency, so it reads -1 to 1 at any cadence and peaks at mid-stance.
            if (dt > 0f)
            {
                float cycles = Mathf.Abs(Mathf.Repeat(_gaitPhase - _lastGaitPhase + .5f, 1f) - .5f) / dt;
                _gaitCycleRate = Mathf.Lerp(_gaitCycleRate, cycles, 1 - Mathf.Exp(-10f * dt));
                _strideRate = Mathf.Lerp(_strideRate, (stride - _lastStride) / dt, 1 - Mathf.Exp(-20f * dt));
            }
            _lastStride = stride; _lastGaitPhase = _gaitPhase;
            float omega = 2f * Mathf.PI * Mathf.Max(.2f, _gaitCycleRate);
            StanceSide = Mathf.Clamp(-_strideRate / omega, -1f, 1f) * _armSwingAmount;
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
            // The shoulder moves out AFTER the rotation, in the character's frame, so the arm hangs flush beside the torso.
            _swingArmL.position += transform.right * (sideL * shiftL * _armSwingAmount);
            _swingArmR.position += transform.right * (sideR * shiftR * _armSwingAmount);
            ApplyFootPlant(legLeft, legRight);
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
        /// ⚠️ THE LOWER FOOT STAYS ON THE COURT. With no knee, a leg swung `a` degrees off vertical lifts its sole by
        /// reach * (1 - cos a) with the hip where it is, and the walk clip swings both legs to 38 degrees at contact, so both
        /// soles hung 21 per cent of a leg's length (about 9 cm on the shared body) above the ground at every step and the body
        /// hovered. The root drops by the lift of the MORE vertical leg, so that sole touches down; the result is also the gait's
        /// bob, lowest at contact and highest at passing, twice a cycle, which is where a walker's weight actually goes.
        /// </summary>
        private void ApplyFootPlant(float legLeft, float legRight)
        {
            FootPlantDrop = 0f;
            if (_swingRoot == null || _legReachWorld <= 0f) return;
            _swingRootPosRest = _swingRoot.localPosition;
            float lower = Mathf.Min(Mathf.Abs(legLeft), Mathf.Abs(legRight)) * Mathf.Deg2Rad;
            FootPlantDrop = _legReachWorld * (1f - Mathf.Cos(lower)) * FootPlantShare * _armSwingAmount;
            _swingRoot.position -= transform.up * FootPlantDrop;
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
            int head = -1, leg = -1;
            for (int i = 0; i < skin.bones.Length; i++)
            {
                if (skin.bones[i] == null) continue;
                if (skin.bones[i].name == "head") head = i;
                else if (skin.bones[i].name == "leg-left" && leg < 0) leg = i;
            }
            _fitL = FitArm(vertices, dominant, pivotL, armL, armR, head);
            _fitR = FitArm(vertices, dominant, pivotR, armR, armL, head);
            // The leg's reach, hip pivot to sole, off the same bind pose (`ApplyFootPlant`).
            if (leg >= 0 && leg < binds.Length)
            {
                float hip = binds[leg].inverse.MultiplyPoint3x4(Vector3.zero).y, sole = float.MaxValue;
                for (int i = 0; i < vertices.Length; i++) if (dominant[i] == leg) sole = Mathf.Min(sole, vertices[i].y);
                if (sole < hip) _legReachWorld = (hip - sole) * _armWorldScale;
            }
        }

        /// <summary>
        /// One arm. First the shoulder: moved out until the hanging arm's inner face is flush with the torso's side
        /// (`TopClearance`), the torso being every vertex that is neither arm nor head from 60 per cent of an arm's length
        /// below the pivot to just above it. Then the fist: the spread opens a degree at a time from the base value, up to
        /// `MaxArmSpreadDegrees`, only while the fist's inner face would still sit inside the hips (the band from just below the
        /// hanging fist to 40 per cent of an arm above it) by less than `ArmClearance`; past the cap the shoulder moves out the
        /// rest of the way, at most `MaxShoulderShift` of the arm's length.
        /// </summary>
        private static ArmFit FitArm(Vector3[] vertices, int[] dominant, Vector3 pivot, int arm, int otherArm, int head)
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
            float torso = Widest(pivot.y - .6f * length, pivot.y + .05f * length);
            Solve(WalkArmSpreadDegrees, out fit.WalkSpread, out fit.WalkShift);
            Solve(RunArmSpreadDegrees, out fit.RunSpread, out fit.RunShift);
            fit.Valid = true;
            return fit;

            float Widest(float from, float to)
            {
                float widest = float.MinValue;
                for (int i = 0; i < vertices.Length; i++)
                {
                    int b = dominant[i];
                    if (b == arm || b == otherArm || b == head) continue;
                    float y = vertices[i].y;
                    if (y < from || y > to) continue;
                    widest = Mathf.Max(widest, vertices[i].x * side);
                }
                return widest;
            }

            void Solve(float from, out float spread, out float shift)
            {
                spread = from; shift = 0f;
                float need = 0f;
                for (float degrees = from; degrees <= MaxArmSpreadDegrees + .01f; degrees += 1f)
                {
                    float radians = degrees * Mathf.Deg2Rad, cos = Mathf.Cos(radians);
                    spread = degrees;
                    shift = torso == float.MinValue ? 0f : Mathf.Max(0f, torso + TopClearance * length - (pivot.x * side - inner * cos));
                    float fistY = pivot.y - length * cos;
                    float hips = Widest(fistY - .1f * length, fistY + .4f * length);
                    if (hips == float.MinValue) return;
                    float fistInner = pivot.x * side + shift + length * Mathf.Sin(radians) - inner * cos;
                    need = hips + ArmClearance * length - fistInner;
                    if (need <= 0f) return;
                }
                shift = Mathf.Min(shift + need, MaxShoulderShift * length);
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
