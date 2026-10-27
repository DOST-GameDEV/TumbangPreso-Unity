using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.PlayTests.Trailer
{
    /// <summary>
    /// A full-body pose for trailer acting, in the CHARACTER's frame (x toward the throwing hand, y up, z forward),
    /// the same language as the game's own throw (`CharacterAnimator.ThrowBody`): the chest turns, leans and rolls, the
    /// head nods, turns and tilts, and each upper arm and forearm POINTS somewhere. A zero limb vector leaves that limb
    /// to the clip underneath. Hop lifts the body, Sink lowers it (a crouch, a dip of anticipation).
    /// </summary>
    public struct Pose
    {
        public float Twist, Lean, Roll, Nod, Turn, Tilt, Hop, Sink;
        public Vector3 Arm, Fore, Off, OffFore;

        /// <summary>Unclamped, so an eased key can OVERSHOOT and settle (the whip of a follow-through).</summary>
        public static Pose Mix(in Pose a, in Pose b, float t) => new Pose
        {
            Twist = Mathf.LerpUnclamped(a.Twist, b.Twist, t), Lean = Mathf.LerpUnclamped(a.Lean, b.Lean, t), Roll = Mathf.LerpUnclamped(a.Roll, b.Roll, t),
            Nod = Mathf.LerpUnclamped(a.Nod, b.Nod, t), Turn = Mathf.LerpUnclamped(a.Turn, b.Turn, t), Tilt = Mathf.LerpUnclamped(a.Tilt, b.Tilt, t),
            Hop = Mathf.LerpUnclamped(a.Hop, b.Hop, t), Sink = Mathf.LerpUnclamped(a.Sink, b.Sink, t),
            Arm = Dir(a.Arm, b.Arm, t), Fore = Dir(a.Fore, b.Fore, t), Off = Dir(a.Off, b.Off, t), OffFore = Dir(a.OffFore, b.OffFore, t),
        };

        private static Vector3 Dir(Vector3 a, Vector3 b, float t)
        {
            if (a.sqrMagnitude < 1e-6f) return b;
            if (b.sqrMagnitude < 1e-6f) return a;
            return Vector3.SlerpUnclamped(a.normalized, b.normalized, t);
        }

        // ---- a vocabulary (the game's throw shapes, pushed for the camera)

        public static readonly Vector3 Down = new Vector3(0.16f, -1.0f, 0.05f), DownFore = new Vector3(0.1f, -1.0f, 0.2f);
        public static readonly Vector3 OffDown = new Vector3(-0.16f, -1.0f, 0.05f), OffDownFore = new Vector3(-0.1f, -1.0f, 0.2f);

        public static Pose Rest => new Pose { Arm = Down, Fore = DownFore, Off = OffDown, OffFore = OffDownFore };

        /// <summary>Loaded: chest turned away, throwing arm back and out with the forearm cocked, free arm at the target.</summary>
        public static Pose Charge => new Pose
        {
            Twist = 46, Lean = -9, Roll = 4, Nod = -4, Sink = 0.06f,
            Arm = new Vector3(0.7f, -0.02f, -0.72f), Fore = new Vector3(0.6f, 0.78f, -0.2f),
            Off = new Vector3(-0.3f, 0.26f, 0.92f), OffFore = new Vector3(-0.22f, 0.2f, 0.96f),
        };

        /// <summary>Let go: chest whipped past square, arm out in front, free fist tucked.</summary>
        public static Pose Contact => new Pose
        {
            Twist = -18, Lean = 14, Nod = 4,
            Arm = new Vector3(0.12f, 0.3f, 0.95f), Fore = new Vector3(0.06f, 0.16f, 0.98f),
            Off = new Vector3(-0.4f, -0.75f, -0.35f), OffFore = new Vector3(-0.2f, -0.5f, 0.84f),
        };

        /// <summary>Finished: the arm wrapped across and down along the flight, the chest turned past.</summary>
        public static Pose Follow => new Pose
        {
            Twist = -36, Lean = 20, Nod = 6, Sink = 0.03f,
            Arm = new Vector3(-0.45f, -0.55f, 0.7f), Fore = new Vector3(-0.72f, -0.62f, 0.3f),
            Off = new Vector3(-0.42f, -0.7f, -0.45f), OffFore = new Vector3(-0.2f, -0.55f, 0.8f),
        };

        /// <summary>Both arms flung up, back arched, chin up.</summary>
        public static Pose Cheer => new Pose
        {
            Lean = -12, Nod = -20,
            Arm = new Vector3(0.35f, 1.0f, 0.1f), Fore = new Vector3(0.15f, 1.0f, 0.05f),
            Off = new Vector3(-0.35f, 1.0f, 0.1f), OffFore = new Vector3(-0.15f, 1.0f, 0.05f),
        };

        /// <summary>Leaning in, hands clasped at the chest: nervous.</summary>
        public static Pose Nervous => new Pose
        {
            Lean = 12, Nod = -4,
            Arm = new Vector3(0.25f, -0.75f, 0.45f), Fore = new Vector3(-0.6f, 0.45f, 0.65f),
            Off = new Vector3(-0.25f, -0.75f, 0.45f), OffFore = new Vector3(0.6f, 0.45f, 0.65f),
        };

        /// <summary>Hands on knees, low and ready: the taya.</summary>
        public static Pose Ready => new Pose
        {
            Lean = 30, Nod = -30, Sink = 0.1f,
            Arm = new Vector3(0.3f, -0.8f, 0.5f), Fore = new Vector3(0.1f, -0.7f, 0.7f),
            Off = new Vector3(-0.3f, -0.8f, 0.5f), OffFore = new Vector3(-0.1f, -0.7f, 0.7f),
        };

        /// <summary>Both hands lifting something overhead.</summary>
        public static Pose Lift => new Pose
        {
            Lean = -10, Nod = -24,
            Arm = new Vector3(0.22f, 1.0f, 0.18f), Fore = new Vector3(-0.35f, 0.95f, 0.1f),
            Off = new Vector3(-0.22f, 1.0f, 0.18f), OffFore = new Vector3(0.35f, 0.95f, 0.1f),
        };
    }

    public enum Ease { Linear, Smooth, In, Out, Whip, Back, Step }

    /// <summary>Keyed poses over time. Each key says how the motion INTO it is eased.</summary>
    public sealed class Track
    {
        private readonly List<(float t, Pose pose, Ease ease)> _keys = new List<(float, Pose, Ease)>();

        public Track Key(float t, in Pose pose, Ease ease = Ease.Smooth) { _keys.Add((t, pose, ease)); return this; }

        public Pose At(float t)
        {
            if (_keys.Count == 0) return Pose.Rest;
            if (t <= _keys[0].t) return _keys[0].pose;
            for (int i = 1; i < _keys.Count; i++)
            {
                if (t >= _keys[i].t) continue;
                var a = _keys[i - 1]; var b = _keys[i];
                float u = Mathf.Clamp01((t - a.t) / Mathf.Max(1e-4f, b.t - a.t));
                return Pose.Mix(a.pose, b.pose, Shape(b.ease, u));
            }
            return _keys[_keys.Count - 1].pose;
        }

        public static float Shape(Ease ease, float u)
        {
            switch (ease)
            {
                case Ease.Linear: return u;
                case Ease.In: return u * u * u;
                case Ease.Out: return 1.0f - Mathf.Pow(1.0f - u, 3.0f);
                case Ease.Whip: { float c = 1.6f; return 1 + (c + 1) * Mathf.Pow(u - 1, 3) + c * Mathf.Pow(u - 1, 2); }
                case Ease.Back: { float c = 2.2f; float v = u - 1; return 1 + v * v * ((c + 1) * v + c); }
                case Ease.Step: return u < 1.0f ? 0.0f : 1.0f;
                default: return u * u * (3.0f - 2.0f * u);
            }
        }
    }

    /// <summary>
    /// The bones of one extra's VISIBLE skin, posed by <see cref="Pose"/> on top of whatever clip was sampled this frame.
    /// Call <see cref="Reset"/> BEFORE sampling the clip: a bone the clip does not key would otherwise keep last frame's
    /// pose and the layer would pile up.
    /// </summary>
    public sealed class TrailerBody
    {
        public readonly Transform Frame;
        private readonly Transform _torso, _head, _armR, _armL, _foreR, _foreL;
        private readonly Vector3 _alongR = Vector3.right, _alongL = Vector3.left, _foreAlongR = Vector3.right, _foreAlongL = Vector3.left;
        private readonly Dictionary<Transform, Quaternion> _rest = new Dictionary<Transform, Quaternion>();
        private readonly float _side = 1.0f, _foreLength = 0.2f;

        public TrailerBody(GameObject model)
        {
            Frame = model.transform;
            foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(false))
            {
                if (skin.bones == null) continue;
                var binds = skin.sharedMesh != null ? skin.sharedMesh.bindposes : null;
                for (int i = 0; i < skin.bones.Length; i++)
                {
                    var b = skin.bones[i];
                    if (b == null) continue;
                    if (b.name == "torso" && _torso == null) _torso = b;
                    else if (b.name == "head" && _head == null) _head = b;
                    else if (b.name == "arm-right" && _armR == null) { _armR = b; _alongR = Along(binds, i, _alongR); }
                    else if (b.name == "arm-left" && _armL == null) { _armL = b; _alongL = Along(binds, i, _alongL); }
                    else if (b.name == "forearm-right" && _foreR == null) { _foreR = b; _foreAlongR = Along(binds, i, _foreAlongR); }
                    else if (b.name == "forearm-left" && _foreL == null) { _foreL = b; _foreAlongL = Along(binds, i, _foreAlongL); }
                }
            }
            foreach (var b in new[] { _torso, _head, _armR, _armL, _foreR, _foreL }) if (b != null) _rest[b] = b.localRotation;
            if (_armR != null) _side = Frame.InverseTransformPoint(_armR.position).x >= 0 ? 1.0f : -1.0f;
            if (_armR != null && _foreR != null) _foreLength = Vector3.Distance(_armR.position, _foreR.position);
        }

        private static Vector3 Along(Matrix4x4[] binds, int index, Vector3 fallback)
        {
            if (binds == null || index >= binds.Length) return fallback;
            var boneToModel = binds[index].inverse;
            float side = boneToModel.MultiplyPoint3x4(Vector3.zero).x;
            float axis = boneToModel.MultiplyVector(Vector3.right).x;
            if (Mathf.Abs(side) < 1e-4f || Mathf.Abs(axis) < 1e-4f) return fallback;
            return Mathf.Sign(side) == Mathf.Sign(axis) ? Vector3.right : Vector3.left;
        }

        public void Reset() { foreach (var pair in _rest) if (pair.Key != null) pair.Key.localRotation = pair.Value; }

        public void Apply(in Pose p, Transform root)
        {
            Vector3 up = Frame.up, right = Frame.right, fwd = Frame.forward;
            if (root != null) root.position += Vector3.up * (p.Hop - p.Sink);
            if (_torso != null)
                _torso.rotation = Quaternion.AngleAxis(p.Twist * _side, up) * Quaternion.AngleAxis(p.Lean, right) * Quaternion.AngleAxis(-p.Roll * _side, fwd) * _torso.rotation;
            // The head keeps looking where the body means to go, then takes its own nod, turn and tilt.
            if (_head != null)
                _head.rotation = Quaternion.AngleAxis(-p.Twist * _side * 0.75f + p.Turn, up) * Quaternion.AngleAxis(-p.Lean * 0.5f + p.Nod, right) * Quaternion.AngleAxis(-p.Tilt, fwd) * _head.rotation;
            Point(_armR, _alongR, p.Arm, 1.0f);
            Point(_armL, _alongL, p.Off, -1.0f);
            Point(_foreR, _foreAlongR, p.Fore.sqrMagnitude > 1e-6f ? p.Fore : p.Arm, 1.0f);
            Point(_foreL, _foreAlongL, p.OffFore.sqrMagnitude > 1e-6f ? p.OffFore : p.Off, -1.0f);
        }

        private void Point(Transform bone, Vector3 along, Vector3 local, float hand)
        {
            if (bone == null || local.sqrMagnitude < 1e-6f) return;
            var desired = Frame.TransformDirection(new Vector3(local.x * _side, local.y, local.z).normalized);
            bone.rotation = Quaternion.FromToRotation(bone.TransformDirection(along), desired) * bone.rotation;
        }

        /// <summary>The throwing hand's fist, world space, and which way the forearm points.</summary>
        public Vector3 Hand(out Vector3 pointing)
        {
            var fore = _foreR != null ? _foreR : _armR;
            var along = _foreR != null ? _foreAlongR : _alongR;
            if (fore == null) { pointing = Frame.forward; return Frame.position + Vector3.up; }
            pointing = fore.TransformDirection(along);
            return fore.position + pointing * _foreLength * 0.95f;
        }

        public Vector3 OffHand()
        {
            var fore = _foreL != null ? _foreL : _armL;
            var along = _foreL != null ? _foreAlongL : _alongL;
            if (fore == null) return Frame.position + Vector3.up;
            return fore.position + fore.TransformDirection(along) * _foreLength * 0.95f;
        }

        /// <summary>Two-bone reach for a cinematic prop contact, keeping the elbow outside the torso.</summary>
        public float ReachRight(Vector3 target)
        {
            if (_armR == null || _foreR == null) return float.PositiveInfinity;
            Vector3 shoulder = _armR.position, delta = target - shoulder;
            float upper = Vector3.Distance(shoulder, _foreR.position), lower = _foreLength * .95f;
            float distance = Mathf.Clamp(delta.magnitude, Mathf.Abs(upper-lower)+.001f, upper+lower-.001f);
            Vector3 direction = delta.normalized;
            Vector3 pole = Vector3.ProjectOnPlane(Frame.right*_side-Frame.up*.1f,direction).normalized;
            if (pole.sqrMagnitude < .01f) pole = Vector3.Cross(direction,Frame.forward).normalized;
            float cosine = Mathf.Clamp((upper*upper+distance*distance-lower*lower)/(2*upper*distance),-1,1);
            Vector3 elbow = shoulder+direction*(upper*cosine)+pole*(upper*Mathf.Sqrt(Mathf.Max(0,1-cosine*cosine)));
            _armR.rotation = Quaternion.FromToRotation(_foreR.position-shoulder,elbow-shoulder)*_armR.rotation;
            Vector3 hand = Hand(out _);
            _foreR.rotation = Quaternion.FromToRotation(hand-_foreR.position,target-_foreR.position)*_foreR.rotation;
            return Vector3.Distance(Hand(out _),target);
        }
    }
}
