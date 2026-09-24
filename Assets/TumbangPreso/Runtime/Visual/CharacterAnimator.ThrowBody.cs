using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ THE THIRD-PERSON THROW AND PEKTUS, AS SHAPES THE COURT CAN READ.
    ///
    /// 🧑 2026-09-24: *"refine other gameplay animations too for both FPP and TPP view ... throwing of
    /// slippers or pektus"*, *"analyze how it should look to communicate that that action is happening"*.
    /// The analysis is `docs/reports/gameplay-animation-2026-09-24/research-and-analysis.md`. Filmed before
    /// this pass (`GameplayActionShots`, `Logs/gameplay-actions/before`), the straight throw and both pektus
    /// were the SAME few degrees of shoulder: the shoe drifted up by the cheek and back, and from where the
    /// taya stands there was no wind-up, no release and no direction.
    ///
    /// So the throw is posed by where each limb POINTS, in the character's frame, the way the locomotion
    /// arms are (`LocomotionArms`), rather than by Euler offsets on a mirrored rig:
    ///
    ///   * CHARGE: the chest turns away from the target, the throwing arm goes back and OUT (never up behind
    ///     the head: the shoe cut through the hair, `body-throw.md`), and the free arm comes up and points at
    ///     the target. Scaled by the charge, so a tap is small and a full hold is the whole shape.
    ///   * CONTACT (the first `ContactSeconds`): the chest whips round past square and the arm is out in front.
    ///   * FOLLOW-THROUGH (held to `HoldSeconds`): the arm finishes along the flight, the chest turned past.
    ///   * RECOVERY (to `ReleaseSeconds`): back to whatever the legs and locomotion are doing.
    ///
    /// ⚠️ PEKTUS IS A DIFFERENT THROW, NOT A ROTATED ONE. It is a SIDEARM throw (the arm low and flat at the
    /// waist), the wrist visibly rolled while charging (palm up for right spin, palm down for left), and it
    /// finishes OPEN out to the right for a right curve and CLOSED across the body for a left one. Three
    /// cues a defender reads from across the court: the arm plane, the slipper's roll, the finish.
    ///
    /// ⚠️ TIMING IS UNTOUCHED. Release time, projectile origin and trajectory are the game's; this only
    /// draws the body around them. The taya's lunge preparation keeps its own pose.
    /// </summary>
    public sealed partial class CharacterAnimator
    {
        /// <summary>The follow-through pose is held until here before recovering.</summary>
        public const float ThrowHoldSeconds = .26f;

        private struct ThrowShape
        {
            public float Twist, Lean, Roll;   // chest: + turns right, + bends forward, + tips toward the throwing side
            public Vector3 Arm, Off;          // where the throwing and free arms point, character frame
            public float Wrist;               // the throwing arm's roll about itself (pektus)
        }

        private Transform _tbTorso, _tbHead, _tbArmR, _tbArmL;
        private Vector3 _tbAlongR = Vector3.right, _tbAlongL = Vector3.left;
        private bool _tbResolved;
        private ThrowShape _tbFrom;
        private float _tbFromWeight;
        private bool _tbReleasing, _tbApplied;
        private Quaternion _tbTorsoRest, _tbHeadRest, _tbArmRRest, _tbArmLRest;

        /// <summary>
        /// ⚠️ THIS LAYER RESTORES ITS OWN BONES. The charge code restores the bones IT resolved, by a name
        /// search that can land on a body's hidden second rig; these are the visible skin's, so relying on
        /// that restore would let these rotations pile up frame on frame.
        /// </summary>
        private void RestoreThrowBody()
        {
            if (!_tbApplied) return;
            _tbTorso.localRotation = _tbTorsoRest; _tbArmR.localRotation = _tbArmRRest; _tbArmL.localRotation = _tbArmLRest;
            if (_tbHead != null) _tbHead.localRotation = _tbHeadRest;
            _tbApplied = false;
        }

        private static ThrowShape Charge(float spin)
        {
            float side = Mathf.Abs(spin);
            // Overhand: back and out at shoulder height. Sidearm: out wide and low at the waist.
            // ⚠️ Arm just under level (y -0.08), well back: on these chunky bodies the shoulder is at the jaw, and y 0.32 put
            // the shoe beside the head in the taya's view (the first front-on film). The free arm points out
            // and forward rather than up, so it no longer crosses the face.
            var over = new ThrowShape { Twist = 34, Lean = -6, Roll = 0, Arm = new Vector3(.7f, -.08f, -.72f), Off = new Vector3(-.34f, .18f, .92f), Wrist = 0 };
            var side_ = new ThrowShape { Twist = 28, Lean = 4, Roll = 12, Arm = new Vector3(.9f, -.12f, -.42f), Off = new Vector3(-.34f, .14f, .93f), Wrist = 55 * Mathf.Sign(spin) };
            return Mix(over, side_, side);
        }

        private static ThrowShape Contact(float spin)
        {
            float side = Mathf.Abs(spin);
            var over = new ThrowShape { Twist = -14, Lean = 10, Roll = 0, Arm = new Vector3(.12f, .3f, .95f), Off = new Vector3(-.4f, -.75f, -.35f), Wrist = 0 };
            var side_ = new ThrowShape { Twist = -10, Lean = 8, Roll = 8, Arm = new Vector3(.45f, -.05f, .9f), Off = new Vector3(-.45f, -.7f, -.3f), Wrist = 30 * Mathf.Sign(spin) };
            return Mix(over, side_, side);
        }

        private static ThrowShape Follow(float spin)
        {
            var over = new ThrowShape { Twist = -30, Lean = 16, Roll = 0, Arm = new Vector3(-.45f, -.55f, .7f), Off = new Vector3(-.4f, -.72f, -.45f), Wrist = 0 };
            if (spin > .05f) // right curve: finishes OPEN, out to the right
                return Mix(over, new ThrowShape { Twist = -8, Lean = 10, Roll = 10, Arm = new Vector3(.75f, -.2f, .62f), Off = new Vector3(-.45f, -.6f, -.4f), Wrist = 70 }, spin);
            if (spin < -.05f) // left curve: finishes CLOSED, across the body to the left
                return Mix(over, new ThrowShape { Twist = -38, Lean = 12, Roll = 6, Arm = new Vector3(-.7f, -.3f, .65f), Off = new Vector3(-.35f, -.7f, -.5f), Wrist = -70 }, -spin);
            return over;
        }

        // Unclamped, so a follow-through can OVERSHOOT its pose and settle back: the whip.
        private static ThrowShape Mix(ThrowShape a, ThrowShape b, float t) => new ThrowShape
        {
            Twist = Mathf.LerpUnclamped(a.Twist, b.Twist, t), Lean = Mathf.LerpUnclamped(a.Lean, b.Lean, t), Roll = Mathf.LerpUnclamped(a.Roll, b.Roll, t),
            Arm = Vector3.SlerpUnclamped(a.Arm.normalized, b.Arm.normalized, t), Off = Vector3.SlerpUnclamped(a.Off.normalized, b.Off.normalized, t),
            Wrist = Mathf.LerpUnclamped(a.Wrist, b.Wrist, t),
        };

        /// <summary>Ease out with an overshoot of about 12 per cent, then settle.</summary>
        private static float Whip(float u) { u = Mathf.Clamp01(u); float c = 1.4f; return 1 + (c + 1) * Mathf.Pow(u - 1, 3) + c * Mathf.Pow(u - 1, 2); }

        private void ResolveThrowBones()
        {
            if (_tbResolved || _animator == null) return;
            foreach (var skin in _animator.GetComponentsInChildren<SkinnedMeshRenderer>(false))
            {
                if (!skin.enabled || skin.bones == null) continue;
                var binds = skin.sharedMesh != null ? skin.sharedMesh.bindposes : null;
                for (int i = 0; i < skin.bones.Length; i++)
                {
                    var b = skin.bones[i];
                    if (b == null) continue;
                    if (b.name == "torso" && _tbTorso == null) _tbTorso = b;
                    else if (b.name == "head" && _tbHead == null) _tbHead = b;
                    else if (b.name == "arm-right" && _tbArmR == null) { _tbArmR = b; _tbAlongR = AlongArm(binds, i, _tbAlongR); }
                    else if (b.name == "arm-left" && _tbArmL == null) { _tbArmL = b; _tbAlongL = AlongArm(binds, i, _tbAlongL); }
                }
            }
            _tbResolved = true;
        }

        private void ClearThrowBody() { RestoreThrowBody(); _tbTorso = _tbHead = _tbArmR = _tbArmL = null; _tbResolved = false; _tbReleasing = false; _tbFromWeight = 0; }

        /// <summary>
        /// Draws the throw over whatever the charge code left. Runs inside the charge-offset window, so the
        /// bones it writes are restored with the others next frame.
        /// </summary>
        private void ApplyThrowBody(bool throwing)
        {
            if (_motor == null) return;
            ResolveThrowBones();
            if (_tbTorso == null || _tbArmR == null || _tbArmL == null) return;

            ThrowShape shape; float weight;
            if (_throwReleaseTime >= 0)
            {
                if (!_tbReleasing)
                {
                    _tbReleasing = true;
                    // ⚠️ SATISFYING, NOT JUST READABLE (🧑 2026-09-24: *"make ALL animations and actions ... look
                    // satisfying"*): the whole body snaps forward on the frame it lets go.
                    var squash = GetComponentInParent<CharacterSquashStretch>() ?? GetComponentInChildren<CharacterSquashStretch>();
                    squash?.DashStretch(transform.forward, .16f);
                }
                float t = _throwReleaseTime, spin = _throwReleaseSpin;
                var start = _tbFromWeight > 0 ? _tbFrom : Charge(spin);
                float startWeight = Mathf.Max(_tbFromWeight, .35f);
                if (t < ThrowGesture.ContactSeconds)
                { float u = t / ThrowGesture.ContactSeconds; shape = Mix(start, Contact(spin), u * u); weight = Mathf.Lerp(startWeight, 1, u); }
                else if (t < ThrowGesture.FollowSeconds)
                { shape = Mix(Contact(spin), Follow(spin), Whip(Mathf.InverseLerp(ThrowGesture.ContactSeconds, ThrowGesture.FollowSeconds, t))); weight = 1; }
                else
                { shape = Follow(spin); weight = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(ThrowHoldSeconds, ThrowGesture.ReleaseSeconds, t)); }
            }
            else if (throwing && _chargePosing)
            {
                _tbReleasing = false;
                float p = ObservedCharge();
                // Quick to commit, then filling out: a tap still shows the turn, a hold shows everything.
                p = (1 - Mathf.Exp(-4.5f * Mathf.Clamp01(p))) / (1 - Mathf.Exp(-4.5f));
                float spin = _carrier != null ? Mathf.Clamp(_carrier.ObservedPektusSpin, -1, 1) : 0;
                shape = Charge(spin); weight = p;
                // Held at full, the arm trembles very slightly: the tension of a loaded throw.
                float full = Mathf.InverseLerp(.93f, 1f, p);
                if (full > 0) shape.Arm = (shape.Arm.normalized + .03f * full * new Vector3(Mathf.Sin(Time.time * 83f), Mathf.Sin(Time.time * 71f + 1.3f), 0)).normalized;
                _tbFrom = shape; _tbFromWeight = p;
            }
            else if (_throwCancelTime >= 0 && _tbFromWeight > 0)
            {
                shape = _tbFrom; weight = _tbFromWeight * (1 - Mathf.SmoothStep(0, 1, _throwCancelTime / ThrowGesture.CancelSeconds));
            }
            else { _tbReleasing = false; _tbFromWeight = 0; return; }

            if (weight <= .001f) return;
            _tbTorsoRest = _tbTorso.localRotation; _tbArmRRest = _tbArmR.localRotation; _tbArmLRest = _tbArmL.localRotation;
            if (_tbHead != null) _tbHeadRest = _tbHead.localRotation;
            _tbApplied = true;
            var up = transform.up; var right = transform.right; var fwd = transform.forward;
            // The throwing side, from where the throwing shoulder sits (the importer mirrors X).
            float side = transform.InverseTransformPoint(_tbArmR.position).x >= 0 ? 1f : -1f;
            float twist = shape.Twist * side * weight, lean = shape.Lean * weight, roll = shape.Roll * side * weight;
            var chest = Quaternion.AngleAxis(twist, up) * Quaternion.AngleAxis(lean, right) * Quaternion.AngleAxis(-roll, fwd);
            _tbTorso.rotation = chest * _tbTorso.rotation;
            // The head keeps looking where the slipper is going.
            if (_tbHead != null) _tbHead.rotation = Quaternion.AngleAxis(-twist * .75f, up) * Quaternion.AngleAxis(-lean * .5f, right) * _tbHead.rotation;
            PointArm(_tbArmR, _tbAlongR, new Vector3(shape.Arm.x * side, shape.Arm.y, shape.Arm.z), weight, shape.Wrist * weight);
            PointArm(_tbArmL, _tbAlongL, new Vector3(shape.Off.x * side, shape.Off.y, shape.Off.z), weight, 0);
        }

        private void PointArm(Transform arm, Vector3 along, Vector3 local, float weight, float wrist)
        {
            var desired = transform.TransformDirection(local.normalized);
            var target = Quaternion.FromToRotation(arm.TransformDirection(along), desired) * arm.rotation;
            if (Mathf.Abs(wrist) > .01f) target = Quaternion.AngleAxis(wrist, desired) * target;
            arm.rotation = Quaternion.Slerp(arm.rotation, target, weight);
        }
    }
}
