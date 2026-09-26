using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ THE WALK AND THE RUN ARE DRAWN HERE, BONE BY BONE, FROM THE BODY'S OWN `GaitStyle`, NOT BY THE CLIPS.
    ///
    /// History, kept because each step was a real failure:
    ///
    /// 1. 🧑 2026-09-24: *"my biggest issue is where the hands are and what they do when u run"*. The shipped `walk` and
    ///    `sprint` clips hold both arms ten degrees off vertical, pressed into the blocky torso, swinging +/-11.8 and +/-23.6
    ///    degrees while the legs swing +/-38 and +/-44; and while carrying, the whole upper body is the static
    ///    `holding-right` clip. So an arm layer was added after the graph evaluates.
    /// 2. 🧑 2026-09-26: *"the walkingh animation looks so weird, hands close to body"*. One fixed spread for every body left
    ///    every fist inside the hips, so a solver (`FitArm`) opened each body's spread from its mesh.
    /// 3. 🧑 same day: *"thihs walk still sucks"*: the solved spreads made every body a penguin, so the solver started sliding
    ///    each SHOULDER outward instead, up to 0.8 of an arm's length (24 to 54 world cm).
    /// 4. 🧑 2026-09-27, on that: *"look dude everyones arms are floating and not even attached right"*, *"walk is fucking ugly
    ///    hahaha do it one by oen dont generate the same one for all"*. ⚠️⚠️ DELETED: `FitArm`, the shoulder shift and every
    ///    cast-wide gait constant. The shoulder now never moves off the pivot the model was built with, so the arm's top
    ///    stays inside the torso and it is attached, and every angle of the walk and run (legs, arms, chest, head, hips) is
    ///    the character's own, written by hand in `GaitStyles` from their personality. This file is only plumbing: it reads
    ///    the gait's phase, asks the style for the pose, and puts it on the seven bones.
    ///
    /// ⚠️⚠️ THE LEGS ARE POSED HERE TOO. The clip legs were one swing for everybody (38 and 44 degrees), and a heavy stomper
    /// and a light skipper cannot share one. The cadence follows the style's own stride (`GaitStyle.CycleMetres`, read by
    /// `CalibrateGait`), so however long a character's step is, the planted foot does not slide.
    ///
    /// ⚠️ THE LOWER FOOT STAYS ON THE COURT (`ApplyFootPlant`). These legs have no knee, so a leg swung off vertical lifts its
    /// sole by reach * (1 - cos a); with the hips held still both feet would float at every contact. The root drops by the lift
    /// of the more vertical leg, which is also where a walker's bob comes from.
    ///
    /// ⚠️⚠️ THE CARRYING ARM IS LOWERED, NOT LEFT STRAIGHT OUT. `holding-right` raises the arm 60 degrees forward, so every
    /// attacker sprinted with the slipper held out at shoulder height like a sleepwalker. While moving it is carried low and a
    /// little forward with a SMALL swing. `Carrier` (execution order 0) rides the hand AFTER this component (-50) poses it.
    ///
    /// Handing over to anything authored (charge, throw, casts, emotes, recovery) eases out over `HandOverSeconds` rather than
    /// snapping: the charge pose is built on the clip's carry pose, so an instant hand-over pops the arm by 40 degrees.
    /// </summary>
    public sealed partial class CharacterAnimator
    {
        /// <summary>The carrying arm: forward of hanging, and its small swing, walking and running. An action pose, not a gait.</summary>
        public const float WalkCarryForwardDegrees = 26f, RunCarryForwardDegrees = 34f;
        public const float WalkCarrySwingDegrees = 7f, RunCarrySwingDegrees = 12f;
        /// <summary>How long the gait takes to hand over to an authored action.</summary>
        public const float HandOverSeconds = .08f;

        private GaitStyle _gaitStyle = GaitStyles.Custom;
        /// <summary>Set by `CharacterVisual` to the model asset's name before `Bind`, so the right body's gait is chosen.</summary>
        public string GaitSource { get; set; }
        /// <summary>Which character's gait this body is walking with (diagnostics and probes).</summary>
        public string GaitStyleName => _gaitStyle != null ? _gaitStyle.Name : "";
        public GaitStyle Style => _gaitStyle;

        private Transform _swingArmL, _swingArmR, _swingLegL, _swingLegR, _swingTorso, _swingHead, _swingRoot;
        /// <summary>Each arm's local axis that runs from the shoulder to the fist, measured off the bind pose.</summary>
        private Vector3 _alongL = Vector3.left, _alongR = Vector3.right;
        /// <summary>Each leg's local axis that runs from the hip to the sole, measured off the bind pose.</summary>
        private Vector3 _legAxisL = Vector3.down, _legAxisR = Vector3.down;
        public Vector3 LeftArmAlong => _alongL;
        public Vector3 RightArmAlong => _alongR;
        public Transform SwingArmLeft => _swingArmL;
        public Transform SwingArmRight => _swingArmR;
        private Quaternion _swingArmLRest, _swingArmRRest, _swingLegLRest, _swingLegRRest, _swingTorsoRest, _swingHeadRest;
        private Quaternion _swingRootRotRest;
        private Vector3 _swingRootPosRest;
        /// <summary>
        /// ⚠️⚠️ EVERY BONE'S BIND POSE, CAPTURED BEFORE THE GRAPH FIRST RUNS. The gait is drawn FROM these, not on top of the
        /// clip. The shipped `walk` and `sprint` were re-authored by `tools/author_grounded_gaits.py` with their own root and
        /// chest lean (2 and 4, 4 and 8 degrees) and a chest yaw on the stride, and layering a character's pose over that
        /// tipped every chest backwards in the first per-character film (Sean ran leaning away from where he was going).
        /// </summary>
        private readonly System.Collections.Generic.Dictionary<Transform, (Quaternion rotation, Vector3 position)> _bind =
            new System.Collections.Generic.Dictionary<Transform, (Quaternion, Vector3)>();
        /// <summary>Hip pivot to sole, in world metres, for the foot plant and every hip offset.</summary>
        private float _legReachWorld;
        /// <summary>-1 to 1: +1 while the body's LEFT leg bears the weight (mid-stance), -1 for the right.</summary>
        public float StanceSide { get; private set; }
        /// <summary>World metres the foot plant lowered the body this frame (diagnostics).</summary>
        public float FootPlantDrop { get; private set; }
        private bool _swingBonesResolved, _swingApplied;
        private float _armSwingAmount;

        /// <summary>0 to 1: how much of the drawn gait is on the body this frame.</summary>
        public float LocomotionArmAmount => _armSwingAmount;
        /// <summary>The gait's phase, 0 to 1, shared with the first-person arms.</summary>
        public float GaitPhase => _gaitPhase;
        /// <summary>0 walking, 1 running.</summary>
        public float GaitRunWeight => _runWeight;
        /// <summary>-1 to 1: +1 with the left leg fully forward.</summary>
        public float StrideSwing { get; private set; }
        /// <summary>Read-only diagnostics: the clip underneath, the posed bones, the amount and the style.</summary>
        public string ArmSwingDiagnostics => $"{_current}|{(_swingArmL != null ? _swingArmL.GetHashCode() : 0)}|{(_swingArmR != null ? _swingArmR.GetHashCode() : 0)}|{_armSwingAmount:F2}|{GaitStyleName}";

        /// <summary>Chooses the style for the model about to be bound, and remembers where its hips rest.</summary>
        private void ResolveGaitStyle(GameObject model)
        {
            _gaitStyle = GaitStyles.For(!string.IsNullOrEmpty(GaitSource) ? GaitSource : model != null ? model.name : null);
            _bind.Clear();
            if (model == null) return;
            // Captured before the graph first evaluates: the instance still stands in its bind pose here.
            foreach (var bone in model.GetComponentsInChildren<Transform>(true))
                switch (bone.name)
                {
                    case "root": case "torso": case "head": case "arm-left": case "arm-right": case "leg-left": case "leg-right":
                        _bind[bone] = (bone.localRotation, bone.localPosition); break;
                }
        }

        private void RestoreLocomotionArms()
        {
            if (!_swingApplied) return;
            if (_swingRoot != null) { _swingRoot.localPosition = _swingRootPosRest; _swingRoot.localRotation = _swingRootRotRest; }
            if (_swingLegL != null) _swingLegL.localRotation = _swingLegLRest;
            if (_swingLegR != null) _swingLegR.localRotation = _swingLegRRest;
            if (_swingArmL != null) _swingArmL.localRotation = _swingArmLRest;
            if (_swingArmR != null) _swingArmR.localRotation = _swingArmRRest;
            if (_swingHead != null) _swingHead.localRotation = _swingHeadRest;
            if (_swingTorso != null) _swingTorso.localRotation = _swingTorsoRest;
            _swingApplied = false;
        }

        private void ClearLocomotionArms()
        {
            RestoreLocomotionArms();
            _swingArmL = _swingArmR = _swingLegL = _swingLegR = _swingTorso = _swingHead = _swingRoot = null;
            _legReachWorld = 0; StanceSide = 0; FootPlantDrop = 0;
            _swingBonesResolved = false;
            _armSwingAmount = 0;
            StrideSwing = 0;
        }

        private void ApplyLocomotionArms()
        {
            if (_motor == null || _animator == null || !_graph.IsValid()) return;
            if (!_swingBonesResolved) ResolveSwingBones();
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

            float cycleRate = FlatSpeed / Mathf.Max(.1f, FootfallCycleMetres);
            var pose = _gaitStyle.Evaluate(_gaitPhase, _runWeight, Time.time, cycleRate);
            float amount = _armSwingAmount;
            StrideSwing = pose.Stride;
            StanceSide = pose.Stance * amount;
            bool carrying = _motor.HoldingSlipper;

            _swingArmLRest = _swingArmL.localRotation; _swingArmRRest = _swingArmR.localRotation;
            _swingLegLRest = _swingLegL.localRotation; _swingLegRRest = _swingLegR.localRotation;
            if (_swingTorso != null) _swingTorsoRest = _swingTorso.localRotation;
            if (_swingHead != null) _swingHeadRest = _swingHead.localRotation;
            if (_swingRoot != null) { _swingRootPosRest = _swingRoot.localPosition; _swingRootRotRest = _swingRoot.localRotation; }
            _swingApplied = true;

            // Back to the bind pose first, so nothing the shared clip keyed (its lean, its chest yaw, its root lift) survives
            // under the character's own gait. The body's answer to acceleration (`_locomotionLean`, the weight layer) is kept.
            ToBind(_swingRoot, amount, true);
            ToBind(_swingTorso, amount, false, Quaternion.Euler(_locomotionLean.x, 0, _locomotionLean.y));
            ToBind(_swingHead, amount, false, Quaternion.Euler(-_locomotionLean.x * .45f, 0, -_locomotionLean.y * .35f));
            ToBind(_swingLegL, amount, false); ToBind(_swingLegR, amount, false);
            ToBind(_swingArmL, amount, false);
            if (!carrying) ToBind(_swingArmR, amount, false);

            // Chest first (the arms and head hang off it), in the character's frame: pitch into the travel about its right
            // axis, roll the chest's top toward the stance leg (+ toward the character's left), turn about up.
            if (_swingTorso != null)
            {
                float calm = carrying ? .5f : 1f;
                _swingTorso.rotation = Quaternion.AngleAxis(pose.TorsoPitch * amount, transform.right)
                    * Quaternion.AngleAxis(pose.TorsoRoll * amount * calm, transform.forward)
                    * Quaternion.AngleAxis(pose.TorsoYaw * amount * calm, transform.up) * _swingTorso.rotation;
            }
            if (_swingHead != null)
                _swingHead.rotation = Quaternion.AngleAxis(pose.HeadPitch * amount, transform.right)
                    * Quaternion.AngleAxis(pose.HeadRoll * amount, transform.forward)
                    * Quaternion.AngleAxis(pose.HeadYaw * amount, transform.up) * _swingHead.rotation;

            // Sides by POSITION, not by bone name: the importer mirrors X. The left arm swings with the left pose value.
            float sideArmL = SideOf(_swingArmL, -1f), sideArmR = SideOf(_swingArmR, 1f);
            PoseLimb(_swingArmL, _alongL, sideArmL, sideArmL < 0 ? pose.ArmLeft : pose.ArmRight, sideArmL < 0 ? pose.SpreadLeft : pose.SpreadRight, amount);
            if (!carrying) PoseLimb(_swingArmR, _alongR, sideArmR, sideArmR < 0 ? pose.ArmLeft : pose.ArmRight, sideArmR < 0 ? pose.SpreadLeft : pose.SpreadRight, amount);
            else PoseLimb(_swingArmR, _alongR, sideArmR,
                Mathf.Lerp(WalkCarryForwardDegrees, RunCarryForwardDegrees, _runWeight)
                + pose.Stride * sideArmR * Mathf.Lerp(WalkCarrySwingDegrees, RunCarrySwingDegrees, _runWeight),
                sideArmR < 0 ? pose.SpreadLeft : pose.SpreadRight, amount);

            float sideLegL = SideOf(_swingLegL, -1f), sideLegR = SideOf(_swingLegR, 1f);
            PoseLimb(_swingLegL, _legAxisL, sideLegL, sideLegL < 0 ? pose.LegLeft : pose.LegRight, sideLegL < 0 ? pose.SplayLeft : pose.SplayRight, amount);
            PoseLimb(_swingLegR, _legAxisR, sideLegR, sideLegR < 0 ? pose.LegLeft : pose.LegRight, sideLegR < 0 ? pose.SplayLeft : pose.SplayRight, amount);

            ApplyFootPlant(pose, amount);
        }

        /// <summary>
        /// ⚠️⚠️ THE BONES THE VISIBLE SKIN IS BOUND TO, NOT A NAME SEARCH. A hero body also carries a hidden second rig, and
        /// searching every child by name posed THAT skeleton: the probe's carrying runs showed the arm untouched on screen and
        /// measured 0 degrees of swing twice. `CharacterVisual.HandAnchor`, which the carried slipper rides, is built the same way.
        /// </summary>
        private void ResolveSwingBones()
        {
            SkinnedMeshRenderer armSkin = null;
            Matrix4x4[] armBinds = null;
            int legIndex = -1;
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
                        case "arm-left": if (_swingArmL == null) { _swingArmL = bone; _alongL = AlongArm(binds, i, _alongL); armSkin = skin; armBinds = binds; } break;
                        case "arm-right": if (_swingArmR == null) { _swingArmR = bone; _alongR = AlongArm(binds, i, _alongR); } break;
                        case "leg-left": if (_swingLegL == null) { _swingLegL = bone; _legAxisL = DownLeg(binds, i); legIndex = i; } break;
                        case "leg-right": if (_swingLegR == null) { _swingLegR = bone; _legAxisR = DownLeg(binds, i); } break;
                        case "torso": if (_swingTorso == null) _swingTorso = bone; break;
                        case "head": if (_swingHead == null) _swingHead = bone; break;
                    }
                }
            }
            // The root that carries these bones, whichever skin it came from.
            if (_swingTorso != null && _swingTorso.parent != null && _swingTorso.parent.name == "root") _swingRoot = _swingTorso.parent;
            // Resolved once per graph (a model swap rebuilds the graph and clears this), so a prop with no arms is not
            // rescanned every frame.
            _swingBonesResolved = true;
            _legReachWorld = 0f;
            if (armSkin == null || armBinds == null || legIndex < 0 || legIndex >= armBinds.Length || !armSkin.sharedMesh.isReadable) return;
            // The leg's reach, hip pivot to sole, off the bind pose, in world metres (for the foot plant and hip offsets).
            var mesh = armSkin.sharedMesh;
            var vertices = mesh.vertices; var weights = mesh.boneWeights;
            if (weights == null || weights.Length != vertices.Length) return;
            float hip = armBinds[legIndex].inverse.MultiplyPoint3x4(Vector3.zero).y, sole = float.MaxValue;
            for (int i = 0; i < vertices.Length; i++) if (weights[i].boneIndex0 == legIndex) sole = Mathf.Min(sole, vertices[i].y);
            if (sole < hip) _legReachWorld = (hip - sole) * armSkin.transform.lossyScale.y;
        }

        /// <summary>
        /// ⚠️ MEASURED, NOT ASSUMED. The rig is T-posed along X, and which way along X is "toward the fist" depends on the
        /// importer: Unity mirrors glTF's X, so assuming the three.js convention pointed the arms' SHOULDER ends at the road.
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

        /// <summary>The leg bone's own axis that points at the model's floor in the bind pose.</summary>
        private static Vector3 DownLeg(Matrix4x4[] binds, int index)
        {
            if (binds == null || index >= binds.Length) return Vector3.down;
            var local = binds[index].MultiplyVector(Vector3.down);
            return local.sqrMagnitude > 1e-8f ? local.normalized : Vector3.down;
        }

        /// <summary>
        /// The hips: back to where the model rests (the clip's own root keys were authored for the old shared stride), then
        /// the style's rise and side shift, then down by the lift of the more vertical leg so that foot is on the court.
        /// </summary>
        private void ApplyFootPlant(in GaitPose pose, float amount)
        {
            FootPlantDrop = 0f;
            if (_swingRoot == null || _legReachWorld <= 0f) return;
            float lift = Mathf.Min(SoleLift(_swingLegL, _legAxisL), SoleLift(_swingLegR, _legAxisR));
            FootPlantDrop = lift;
            _swingRoot.position += transform.up * (pose.RootUp * _legReachWorld * amount - lift)
                + transform.right * (pose.RootRight * _legReachWorld * amount);
        }

        /// <summary>Eases a bone toward its bind rotation (and the root toward its bind position), with an optional extra turn.</summary>
        private void ToBind(Transform bone, float amount, bool position, Quaternion? extra = null)
        {
            if (bone == null || !_bind.TryGetValue(bone, out var rest)) return;
            var target = extra.HasValue ? rest.rotation * extra.Value : rest.rotation;
            bone.localRotation = Quaternion.Slerp(bone.localRotation, target, amount);
            if (position) bone.localPosition = Vector3.Lerp(bone.localPosition, rest.position, amount);
        }

        /// <summary>How far above the hip's rest height's floor this leg's sole sits, world metres, from the posed bone.</summary>
        private float SoleLift(Transform leg, Vector3 axis)
        {
            var down = transform.InverseTransformDirection(leg.TransformDirection(axis)).normalized;
            return _legReachWorld * (1f - Mathf.Clamp01(-down.y));
        }

        /// <summary>Which side of the body a bone is on, from where it actually sits (-1 left, +1 right).</summary>
        private float SideOf(Transform bone, float fallback)
        {
            float x = transform.InverseTransformPoint(bone.position).x;
            return Mathf.Abs(x) < 1e-3f ? fallback : Mathf.Sign(x);
        }

        /// <summary>
        /// Points a limb `out` degrees away from the body's centre line and `forward` degrees ahead of hanging, built in the
        /// character's frame, with the smallest rotation from where the clip left it so its twist stays. The pivot is never
        /// moved: an arm stays on its shoulder, a leg on its hip.
        /// </summary>
        private void PoseLimb(Transform limb, Vector3 localAxis, float side, float forward, float outward, float amount)
        {
            float o = outward * Mathf.Deg2Rad;
            var hang = new Vector3(side * Mathf.Sin(o), -Mathf.Cos(o), 0f);
            var desired = transform.TransformDirection(Quaternion.AngleAxis(-forward, Vector3.right) * hang);
            var current = limb.TransformDirection(localAxis);
            var target = Quaternion.FromToRotation(current, desired) * limb.rotation;
            limb.rotation = Quaternion.Slerp(limb.rotation, target, amount);
        }
    }
}
