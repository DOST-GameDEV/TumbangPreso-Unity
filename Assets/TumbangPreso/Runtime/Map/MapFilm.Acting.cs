using System.Collections.Generic;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Map
{
    public abstract partial class MapFilm
    {
        // ------------------------------------------------------------------ acting
        //
        // ⚠️ EMOTION AND READABILITY ARE A TOP BAR FOR EVERY FILM (owner, 2026-10-23: "make everyone as expressive as
        // possible and show emotions excited sad etc and laughing at taya"; "make showing emotion and expressiveness
        // and readability one of the main bars"). The cast has flat faces, so a feeling has to be in the BODY and read
        // from the far side of a wide shot: a lean, a head, two arms, a hop. Each is pushed past what feels enough in a
        // still and given an anticipation, a peak and a settle in time.
        //
        // HOW IT IS LAID ON: after this film's `Animate` has run the held animator for the frame (the idle or the walk
        // clip), `Act` turns the torso and head about the MODEL's own axes and aims each arm in WORLD space (`AimArm`:
        // the arm's own length axis, measured once, turned onto a direction), so the same feeling reads the same on
        // every rig without knowing its bone axes. Nothing accumulates: a bone the clip does not key is put back to
        // what it was before this layer wrote it, every frame. Nothing outlives the film: the models are released by
        // `End` and the animator writes its own pose again.

        /// <summary>One frame of a body's acting. Degrees; arm directions in world space (zero leaves the arm alone).</summary>
        protected struct Feel
        {
            /// <summary>Torso forward (+) or back (-), turned about the hips (+ to the character's left), rocked sideways (+ right shoulder down).</summary>
            public float Lean, Twist, Rock;
            /// <summary>Head down (+) or up (-), turned (+ left), tilted (+ right ear down).</summary>
            public float Nod, Turn, Tilt;
            public Vector3 Left, Right;
            /// <summary>How fully the arms take their directions, 0 to 1.</summary>
            public float Arms;
            /// <summary>Lift of the whole body over the floor, metres (a hop).</summary>
            public float Hop;

            public static Feel Lerp(in Feel a, in Feel b, float t)
            {
                t = Mathf.Clamp01(t);
                return new Feel
                {
                    Lean = Mathf.Lerp(a.Lean, b.Lean, t), Twist = Mathf.Lerp(a.Twist, b.Twist, t), Rock = Mathf.Lerp(a.Rock, b.Rock, t),
                    Nod = Mathf.Lerp(a.Nod, b.Nod, t), Turn = Mathf.Lerp(a.Turn, b.Turn, t), Tilt = Mathf.Lerp(a.Tilt, b.Tilt, t),
                    Left = Slerp(a.Left, b.Left, t), Right = Slerp(a.Right, b.Right, t),
                    Arms = Mathf.Lerp(a.Arms, b.Arms, t), Hop = Mathf.Lerp(a.Hop, b.Hop, t),
                };
            }

            private static Vector3 Slerp(Vector3 a, Vector3 b, float t)
            {
                if (a.sqrMagnitude < 1e-4f) return b;
                if (b.sqrMagnitude < 1e-4f) return a;
                return Vector3.Slerp(a.normalized, b.normalized, t);
            }
        }

        private readonly Transform[] _torso = new Transform[Seats], _head = new Transform[Seats], _armL = new Transform[Seats], _armR = new Transform[Seats];
        private readonly Vector3[] _axisL = new Vector3[Seats], _axisR = new Vector3[Seats];
        private readonly Dictionary<Transform, Quaternion> _actBase = new Dictionary<Transform, Quaternion>(), _actWritten = new Dictionary<Transform, Quaternion>();

        private void FindActingBones(int seat, GameObject model)
        {
            _torso[seat] = _head[seat] = _armL[seat] = _armR[seat] = null;
            if (model == null) return;
            foreach (var bone in model.GetComponentsInChildren<Transform>(true))
            {
                if (bone.name == "torso") _torso[seat] = bone;
                else if (bone.name == "head") _head[seat] = bone;
                else if (bone.name == "arm-left") _armL[seat] = bone;
                else if (bone.name == "arm-right") _armR[seat] = bone;
            }
            _axisL[seat] = ArmAxis(_armL[seat]); _axisR[seat] = ArmAxis(_armR[seat]);
        }

        /// <summary>The arm's own length, shoulder to hand, in its local frame: to its first child when it has one, else
        /// else straight down as the rig stands now.</summary>
        private static Vector3 ArmAxis(Transform arm)
        {
            if (arm == null) return Vector3.down;
            Vector3 to = Vector3.zero;
            if (arm.childCount > 0) to = arm.GetChild(0).position - arm.position;
            if (to.sqrMagnitude < 1e-4f) to = Vector3.down * 0.3f;
            return (Quaternion.Inverse(arm.rotation) * to).normalized;
        }

        /// <summary>Lays `feel` over this frame's pose of `seat` (call after `Animate`).</summary>
        protected void Act(int seat, in Feel feel)
        {
            if (!Posable(seat)) return;
            var root = _root[seat];
            Vector3 right = root.right, up = Vector3.up, forward = Vector3.ProjectOnPlane(root.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.5f) forward = Vector3.forward;
            right = Vector3.Cross(up, forward);
            if (feel.Hop > 0.0f) root.position += Vector3.up * feel.Hop;

            var torso = Fresh(_torso[seat]);
            if (torso != null)
                torso.rotation = Quaternion.AngleAxis(feel.Lean, right) * Quaternion.AngleAxis(-feel.Twist, up) * Quaternion.AngleAxis(-feel.Rock, forward) * torso.rotation;
            var head = Fresh(_head[seat]);
            if (head != null)
                head.rotation = Quaternion.AngleAxis(feel.Nod, right) * Quaternion.AngleAxis(-feel.Turn, up) * Quaternion.AngleAxis(-feel.Tilt, forward) * head.rotation;
            AimArm(Fresh(_armL[seat]), _axisL[seat], feel.Left, feel.Arms);
            AimArm(Fresh(_armR[seat]), _axisR[seat], feel.Right, feel.Arms);
            Written(_torso[seat]); Written(_head[seat]); Written(_armL[seat]); Written(_armR[seat]);
        }

        /// <summary>The bone as the clip left it this frame: one the clip does not key still holds what this layer wrote
        /// last frame, so it is put back to its base first.</summary>
        private Transform Fresh(Transform bone)
        {
            if (bone == null) return null;
            if (_actWritten.TryGetValue(bone, out var written) && bone.localRotation == written && _actBase.TryGetValue(bone, out var rest)) bone.localRotation = rest;
            _actBase[bone] = bone.localRotation;
            return bone;
        }

        private void Written(Transform bone) { if (bone != null) _actWritten[bone] = bone.localRotation; }

        private static void AimArm(Transform arm, Vector3 axis, Vector3 direction, float amount)
        {
            if (arm == null || direction.sqrMagnitude < 1e-4f || amount <= 0.0f) return;
            Vector3 now = arm.rotation * axis;
            arm.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(now, direction.normalized), Mathf.Clamp01(amount)) * arm.rotation;
        }

        // ------------------------------------------------------------------ the vocabulary
        //
        // Every feeling is built in the seat's own frame (its posed model's facing), so "point at the taya" or "hands
        // into the ring" aim at the real thing wherever the body stands.

        protected Vector3 Facing(int seat)
        {
            if (!Posable(seat)) return Vector3.forward;
            Vector3 f = Vector3.ProjectOnPlane(_root[seat].forward, Vector3.up);
            return f.sqrMagnitude > 0.01f ? f.normalized : Vector3.forward;
        }

        protected Vector3 Side(int seat) => Vector3.Cross(Vector3.up, Facing(seat));

        /// <summary>Where a seat's chest is this frame.</summary>
        protected Vector3 Chest(int seat) { var b = Drawn(seat); return b.center + Vector3.up * b.extents.y * 0.15f; }

        /// <summary>Head turn (degrees) that looks from `seat` toward a point.</summary>
        protected float LookTurn(int seat, Vector3 at)
        {
            Vector3 to = Vector3.ProjectOnPlane(at - Chest(seat), Vector3.up);
            return to.sqrMagnitude < 0.01f ? 0.0f : Mathf.Clamp(-Vector3.SignedAngle(Facing(seat), to, Vector3.up), -70.0f, 70.0f);
        }

        /// <summary>Leaning into a huddle, both hands into its middle.</summary>
        protected Feel HandsIn(int seat, Vector3 middle, float tension)
        {
            Vector3 to = (middle + Vector3.up * 0.9f - Chest(seat)).normalized;
            return new Feel { Lean = 14.0f + 8.0f * tension, Nod = 12.0f, Left = to - Side(seat) * 0.15f, Right = to + Side(seat) * 0.15f, Arms = 0.85f };
        }

        /// <summary>"Not me!": a jerk back, both hands up in front, head turned away. `t` from the moment, seconds.</summary>
        protected Feel Flinch(int seat, float t)
        {
            float k = Mathf.Exp(-t * 5.0f) * Mathf.Clamp01(t / 0.06f);
            Vector3 up = Vector3.up, f = Facing(seat), s = Side(seat);
            return new Feel { Lean = -16.0f * k, Nod = -6.0f * k, Turn = 35.0f * k, Tilt = 10.0f * k, Left = f * 0.8f + up * 0.9f - s * 0.3f, Right = f * 0.8f + up * 0.9f + s * 0.3f, Arms = 0.9f * k };
        }

        /// <summary>Struck: thrown back, arms flung out, head up.</summary>
        protected Feel Shock(int seat)
        {
            Vector3 s = Side(seat);
            return new Feel { Lean = -18.0f, Nod = -14.0f, Left = -s + Vector3.up * 0.55f, Right = s + Vector3.up * 0.55f, Arms = 1.0f };
        }

        /// <summary>Crushed: head hung, shoulders forward, arms dangling.</summary>
        protected Feel Slump(int seat, float sway)
        {
            Vector3 f = Facing(seat), s = Side(seat);
            return new Feel { Lean = 24.0f, Nod = 34.0f, Tilt = 8.0f * sway, Left = Vector3.down + f * 0.25f + s * 0.05f, Right = Vector3.down + f * 0.25f - s * 0.05f, Arms = 0.9f };
        }

        /// <summary>Cheering: both arms straight up, back arched, a hop on each beat of `t`.</summary>
        protected Feel Cheer(int seat, float t, float hopEvery = 0.42f)
        {
            float phase = Mathf.Repeat(t, hopEvery) / hopEvery;
            float hop = Mathf.Sin(phase * Mathf.PI) * 0.28f;
            Vector3 s = Side(seat);
            float wave = Mathf.Sin(t * 13.0f) * 0.25f;
            return new Feel { Lean = -12.0f, Nod = -18.0f, Left = Vector3.up - s * (0.3f + wave), Right = Vector3.up + s * (0.3f - wave), Arms = 1.0f, Hop = hop };
        }

        /// <summary>Laughing AT someone: one arm pointing at them, the other clutching the belly, the body rocking back
        /// with each "ha".</summary>
        protected Feel LaughAt(int seat, Vector3 target, float t, bool pointRight = true)
        {
            float ha = Mathf.Abs(Mathf.Sin(t * 10.5f));
            Vector3 aim = (target - Chest(seat)).normalized, belly = (Facing(seat) * 0.6f + Vector3.down * 0.9f).normalized;
            return new Feel
            {
                Lean = -8.0f - 12.0f * ha, Nod = -16.0f - 10.0f * ha, Turn = LookTurn(seat, target) * 0.6f, Tilt = 10.0f * Mathf.Sin(t * 5.2f), Rock = 6.0f * Mathf.Sin(t * 5.2f),
                Right = pointRight ? aim : belly, Left = pointRight ? belly : aim, Arms = 1.0f, Hop = 0.04f * ha,
            };
        }

        /// <summary>Doubled over laughing: bent forward, both hands on the belly, shaking.</summary>
        protected Feel DoubledOver(int seat, float t)
        {
            float ha = Mathf.Abs(Mathf.Sin(t * 11.0f));
            Vector3 belly = (Facing(seat) * 0.7f + Vector3.down * 0.8f).normalized;
            return new Feel { Lean = 30.0f + 8.0f * ha, Nod = 6.0f, Rock = 8.0f * Mathf.Sin(t * 5.5f), Left = belly, Right = belly, Arms = 0.95f, Hop = 0.03f * ha };
        }

        /// <summary>A fist pumped up on each beat, the other arm low and back, a little dance in the hips.</summary>
        protected Feel FistPump(int seat, float t, float every = 0.36f)
        {
            float phase = Mathf.Repeat(t, every) / every;
            float up = Mathf.Sin(phase * Mathf.PI);
            Vector3 s = Side(seat), f = Facing(seat);
            return new Feel { Lean = -6.0f, Twist = 14.0f * Mathf.Sin(t * Mathf.PI * 2.0f / (every * 2.0f)), Nod = -10.0f * up,
                              Right = Vector3.Lerp(f * 0.8f + Vector3.up * 0.2f, Vector3.up + s * 0.1f, up), Left = Vector3.down - f * 0.3f - s * 0.2f, Arms = 1.0f, Hop = 0.1f * up };
        }

        /// <summary>Squared up: a fist and a finger at `target`, chin down, weight forward.</summary>
        protected Feel Defiant(int seat, Vector3 target)
        {
            Vector3 aim = (target - Chest(seat)).normalized, s = Side(seat);
            return new Feel { Lean = 10.0f, Nod = 6.0f, Turn = LookTurn(seat, target), Right = aim, Left = Facing(seat) * 0.6f + Vector3.down * 0.5f - s * 0.4f, Arms = 1.0f };
        }

        /// <summary>A shake of the head, no: `t` seconds in.</summary>
        protected Feel HeadShake(int seat, float t)
        {
            return new Feel { Turn = 28.0f * Mathf.Sin(t * 17.0f) * Mathf.Exp(-t * 1.8f), Nod = 10.0f, Lean = 6.0f };
        }

        /// <summary>Sizing someone up: chin down, eyes on them, arms loose and ready, a slow sway.</summary>
        protected Feel Glare(int seat, Vector3 target, float t)
        {
            Vector3 s = Side(seat), f = Facing(seat);
            return new Feel { Lean = 8.0f, Nod = 10.0f, Turn = LookTurn(seat, target), Rock = 3.0f * Mathf.Sin(t * 2.2f), Left = Vector3.down + f * 0.35f - s * 0.35f, Right = Vector3.down + f * 0.35f + s * 0.35f, Arms = 0.7f };
        }

        /// <summary>Arms crossed tight, weight back, chin up: unimpressed.</summary>
        protected Feel ArmsCrossed(int seat, Vector3 lookAt)
        {
            Vector3 f = Facing(seat), s = Side(seat);
            return new Feel { Lean = -6.0f, Nod = -8.0f, Turn = LookTurn(seat, lookAt) * 0.7f, Left = (f * 0.8f + s * 0.9f + Vector3.down * 0.2f), Right = (f * 0.8f - s * 0.9f + Vector3.down * 0.1f), Arms = 0.95f };
        }

        /// <summary>A pulse (0 to 1) shaped like a hit: a fast attack at `t` = 0 and a decay.</summary>
        protected static float Hit(float t, float attack = 0.06f, float decay = 5.0f) => t < 0.0f ? 0.0f : Mathf.Clamp01(t / attack) * Mathf.Exp(-Mathf.Max(0.0f, t - attack) * decay);
    }
}
