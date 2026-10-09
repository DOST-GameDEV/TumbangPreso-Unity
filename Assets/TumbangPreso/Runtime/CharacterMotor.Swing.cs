using TumbangPreso.Abilities;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// ⚠️⚠️ THE SWING: Paete's vines carrying this body along `Abilities.PaeteSwing`'s path and throwing it at the end
    /// (owner, 2026-10-07, LIANA LEAP: "swing through and fling"). A carry cannot do it (it is flat, with one capped
    /// push up) and a haul is the Arena's own shape (across, straight up, across).
    ///
    /// ⚠️ THE OWNER OF THE BODY APPLIES IT, like `BeginCarry` and `BeginHaul`, and nothing about the motion is sent:
    /// the host says where the vines caught and what kind of swing it is (`Net.PaeteVineState`), the owner plans the
    /// path from where it stands and walks it, and its poses travel as they always do. It never moves faster than
    /// <see cref="SwingMaxSpeed"/>, far under the host's move budget.
    ///
    /// It ends at the path's end (the throw), under anything overhead, when something holds the body
    /// <see cref="SwingLostMetres"/> behind the path, and on a stun, a root, a flight, a haul, a teleport or a round reset.
    /// </summary>
    public sealed partial class CharacterMotor
    {
        /// <summary>The fastest the body is moved to keep to the path, and how far behind it the body may fall before the vines let go.</summary>
        public const float SwingMaxSpeed = 20.0f, SwingLostMetres = 1.6f;

        /// <summary>
        /// ⚠️⚠️ THE SWING IS STEERED SIDE TO SIDE (owner, 2026-10-08: "can you also fix the leap swing. its not possible to
        /// swing side to side when the cluster is pulling u directly forward to it"). The path is one curve in the upright
        /// plane through his feet and the catch, and the body was walked along it on rails: whatever sideways speed he
        /// leapt with was thrown away, the strafe keys did nothing, and the throw went only straight at the catch. The
        /// path is still the backbone (`PaeteSwing.Plan` is untouched, and so is how long the vines last). What is new is
        /// how far he is ACROSS that plane, with a speed of its own: it starts as the sideways part of the speed he had
        /// when the vines took him, the move keys push it, and the throw carries it.
        ///
        /// ⚠️ NOTHING ABOUT IT IS SENT, like the rest of the swing: only the peer that simulates him steers it.
        ///
        ///   * `SwingSideSpeed` (7 m/s): the most the keys give. About his own run, and half the path's 13, so a full
        ///     steer bends his travel near 28 degrees and takes him about 4 m off the line over a swing at full range.
        ///   * `SwingSideAccel` (21 m/s a second): that top in a third of a second, because a swing is over in about one.
        ///   * `SwingSideCarryIn` (10 m/s): the most of his OWN sideways speed the vines take in. Over the keys' top on
        ///     purpose, so a fast entry (a slide, a hop chain) is not cut down to it. With the path's 13 that is 16.4 m/s
        ///     in all, under <see cref="SwingMaxSpeed"/> with room left to catch the path up, so in open air the cap
        ///     never eats the sideways part.
        ///   * `SwingSideHeldMetres` (0.6 m): the path alone runs up to 0.26 m ahead of the body each step (13 m/s, a
        ///     50th of a second), so over twice that means something solid has him. See `StepSwing`.
        ///
        /// ⚠️ NO PULL BACK TO THE PLANE, ON PURPOSE. A spring toward the catch would have him swinging back through the
        /// middle whenever he let the key go, and the throw would then go wherever that swing happened to be: he could
        /// not hold a side or count on a direction. With no keys the sideways speed is kept as it is, the same rule
        /// the movement rework gives a body in the air.
        /// </summary>
        public const float SwingSideSpeed = 7.0f, SwingSideAccel = 21.0f, SwingSideCarryIn = 10.0f, SwingSideHeldMetres = 0.6f;

        private float[] _swingX, _swingY;
        private Vector3 _swingAnchor, _swingFrom, _swingWay, _swingAcross;
        private VineSwing _swingKind;
        private float _swingStartsAt = -1.0f, _swingClock, _swingFlingX, _swingFlingY, _swingHold;
        // Across the plane: where the body was measured last step, the sideways speed, how far the last step asked it to
        // go sideways, how far behind the path it was held, and the side (-1, 0, +1) the keys are refused toward.
        private float _swingSide, _swingSideSpeed, _swingSideAsked, _swingHeld, _swingSideRefused;
        private int _swingCount;
        private bool _swinging;

        /// <summary>True from the vines catching until they let go.</summary>
        public bool IsSwinging => _swinging || _swingStartsAt >= 0.0f;

        /// <summary>
        /// Swing on vines caught at <paramref name="anchor"/>, starting <paramref name="delay"/> seconds from now (the
        /// time the vines are still on their way out). False anywhere but on the peer that simulates this body.
        /// </summary>
        public bool BeginSwing(Vector3 anchor, VineSwing kind, float delay)
        {
            if (!MayMutateGameplayState() || !IsLocallySimulated()) return false;
            if (kind == VineSwing.Reel || !Finite(anchor) || !float.IsFinite(delay)) return false;
            if (IsEdgeRecovering || IsStunned || IsRooted || IsFlying) return false;
            EndSwing();
            _swingAnchor = anchor;
            _swingKind = kind;
            _swingStartsAt = Time.time + Mathf.Clamp(delay, 0.0f, 1.0f);
            return true;
        }

        private void EndSwing()
        {
            _swinging = false;
            _swingStartsAt = -1.0f;
            _swingSide = _swingSideSpeed = _swingSideAsked = _swingHeld = _swingSideRefused = 0.0f;
        }

        private void StartSwing()
        {
            _swingStartsAt = -1.0f;
            if (IsEdgeRecovering || IsStunned || IsRooted || IsFlying || IsHauled) return;
            _swingX ??= new float[PaeteSwing.MaxSteps];
            _swingY ??= new float[PaeteSwing.MaxSteps];
            _swingFrom = transform.position;
            Vector3 to = _swingAnchor - _swingFrom;
            float up = to.y; to.y = 0.0f;
            float along = to.magnitude;
            // Straight overhead there is no "toward the catch": he goes the way he faces.
            Vector3 facing = transform.forward; facing.y = 0.0f;
            _swingWay = along > 0.05f ? to / along : facing.sqrMagnitude > 1e-4f ? facing.normalized : Vector3.forward;
            _swingCount = PaeteSwing.Plan(_swingKind, along, up, _swingX, _swingY, out _swingFlingX, out _swingFlingY, out _swingHold);
            if (_swingCount < 2) return;
            // ⚠️ THE SIDEWAYS SPEED HE HAD IS HIS TO KEEP (2026-10-08). Read here, on the step the vines take him and after
            // this step's steering, carry and friction have run: `_velocity` is his own travel (the walk, or the movement
            // rework's kept speed) and `_externalVelocity` is whatever was pushing him (a slide's impulse, a carry), and
            // the two together are what the body was about to move by. Only the part ACROSS the plane is taken: the part
            // along it is the path's to set. So a leap out of a strafe or a turn arcs him round, where it used to snap
            // him straight. Right of the way he is going is positive.
            _swingAcross = Vector3.Cross(Vector3.up, _swingWay);
            _swingSide = _swingSideAsked = _swingHeld = _swingSideRefused = 0.0f;
            _swingSideSpeed = Mathf.Clamp(Vector3.Dot(_velocity + _externalVelocity, _swingAcross), -SwingSideCarryIn, SwingSideCarryIn);
            _swingClock = 0.0f;
            _swinging = true;
            _carryLeft = 0.0f;
            _grounded = false;
        }

        /// <summary>
        /// The move keys as a push across the swing's plane, -1 (left of the way he is going) to +1 (right). Read the
        /// way `Steer` reads them (off the body's facing for a mouse-aimed body, which is the camera's yaw, and as a
        /// world heading for a bot or a stick), without `Steer`'s turning of the body. Only the part across the plane
        /// counts: forward and back are the path's.
        ///
        /// A diagonal counts as a full steer. The keys arrive normalised, so forward and strafe together (what a hand
        /// holds through a leap) is 0.71 each way, and it would otherwise steer at 0.71 of the top for no reason he
        /// could see.
        /// </summary>
        private float SwingSteer()
        {
            if (!CanMove() || IsFeared) return 0.0f;
            Vector2 axis = Intent.MoveAxis;
            Vector3 wish = new Vector3(axis.x, 0.0f, axis.y);
            if (wish.sqrMagnitude < 0.0001f) return 0.0f;
            if (MouseAimed) { wish = transform.TransformDirection(wish); wish.y = 0.0f; }
            return Mathf.Clamp(Vector3.Dot(wish, _swingAcross) * 1.4142f, -1.0f, 1.0f);
        }

        /// <summary>Called from the physics step after gravity: while a swing lasts, the velocity is the path's.</summary>
        private void StepSwing(float dt)
        {
            if (_swingStartsAt >= 0.0f && !_swinging && Time.time >= _swingStartsAt) StartSwing();
            if (!_swinging) return;
            if (IsStunned || IsRooted || IsFlying || IsHauled || IsEdgeRecovering) { EndSwing(); return; }

            _swingClock += dt;
            float at = _swingClock / PaeteSwing.Step;
            int i = (int)at;
            if (i >= _swingCount - 1) { LetGoOfSwing(dt); return; }
            float u = at - i;
            Vector3 onPath = _swingFrom
                             + _swingWay * Mathf.Lerp(_swingX[i], _swingX[i + 1], u)
                             + Vector3.up * Mathf.Lerp(_swingY[i], _swingY[i + 1], u);

            // ⚠️ WHERE HE IS ACROSS THE PLANE IS MEASURED, NEVER ASSUMED (2026-10-08). The body is the truth: the motor's
            // own collision moved it, so a wall, a ledge or the edge of the map has already had its say. `behind` is
            // then what is left in the plane, which is the old gap exactly, and the sideways part can never count as
            // being held behind the path. It also means nothing builds up: a step the speed cap or a wall cut short is
            // simply where he is now.
            Vector3 behind = onPath - transform.position;
            float across = -Vector3.Dot(behind, _swingAcross);
            behind += _swingAcross * across;
            float held = behind.magnitude;

            // STEERED INTO SOMETHING SOLID, the plain case (a wall beside him): the body made under half of the sideways
            // move the last step asked for, so the sideways speed stops there. It is not left pushing at the wall, and
            // the throw is not handed a speed he never had.
            float made = across - _swingSide;
            if (Mathf.Abs(_swingSideAsked) > 0.01f && made * _swingSideAsked < 0.5f * _swingSideAsked * _swingSideAsked
                && _swingSideSpeed * _swingSideAsked > 0.0f)
                _swingSideSpeed = 0.0f;
            _swingSide = across;

            // ⚠️ AND THE SLANTED CASE, WHICH IS THE ONE THAT WOULD TRIP A FALSE "LOST". A face he is rising beside that is
            // not square to his way: sliding along it sideways is allowed by the collision, but it pushes him back out
            // of the path, so the gap behind grows with every metre steered until the vines let go at
            // `SwingLostMetres` for something his own steering did. So once he is held `SwingSideHeldMetres` behind
            // and it is getting worse while he moves sideways, that sideways speed stops and the keys are refused
            // toward that side until he is back on the path. Steering AWAY is still his. Something square across the
            // path itself is not this: the gap grows whatever he does and the vines let go below, as they always have.
            if (held <= SwingSideHeldMetres) _swingSideRefused = 0.0f;
            else if (held > _swingHeld + 0.002f && Mathf.Abs(_swingSideSpeed) > 0.01f)
            {
                _swingSideRefused = Mathf.Sign(_swingSideSpeed);
                _swingSideSpeed = 0.0f;
            }
            _swingHeld = held;

            // The keys push toward their side up to `SwingSideSpeed`. They never take away speed he already has on
            // that side (a fast entry stays fast), and with no keys the speed is kept.
            float steer = SwingSteer();
            if (steer * _swingSideRefused > 0.0f) steer = 0.0f;
            float target = steer * SwingSideSpeed;
            if ((steer > 0.01f && _swingSideSpeed < target) || (steer < -0.01f && _swingSideSpeed > target))
                _swingSideSpeed = Mathf.MoveTowards(_swingSideSpeed, target, SwingSideAccel * dt);

            // The steered point: on the path, as far across as he is, plus this step's sideways travel.
            Vector3 gap = behind + _swingAcross * (_swingSideSpeed * dt);
            // Held behind the path by something solid: the vines let go rather than drag him through it.
            if (gap.magnitude > SwingLostMetres)
            {
                EndSwing();
                _velocity = Vector3.zero;
                _externalVelocity = Vector3.zero;
                return;
            }
            // ⚠️ THE CAP TAKES THE WHOLE VELOCITY, SIDEWAYS INCLUDED, and that is safe: in open air the path's 13 and the
            // most sideways (10) are 16.4 together, under it. It bites only while he catches up after being held, and
            // then what the step really asked for sideways is what is kept for the check above, so a capped step is
            // not mistaken for a wall.
            Vector3 velocity = gap / Mathf.Max(dt, 1e-4f);
            if (velocity.magnitude > SwingMaxSpeed) velocity = velocity.normalized * SwingMaxSpeed;
            _swingSideAsked = Vector3.Dot(velocity, _swingAcross) * dt;
            _velocity = velocity;
            _externalVelocity = Vector3.zero;
            _carryLeft = 0.0f;
        }

        /// <summary>
        /// The throw: up at once, forward held for a moment and then left to `Friction`, as a carry is.
        ///
        /// ⚠️ THE SIDEWAYS SPEED GOES WITH IT (2026-10-08): the throw is the path's forward speed PLUS the speed he had
        /// across it, held to `Balance.MaxKnockbackSpeed` in all, so the carry, the push and the speed the movement
        /// rework is handed at its end (`StepCarry`) all point the way he was really going, and a slide out of the
        /// leap goes that way too.
        /// </summary>
        private void LetGoOfSwing(float dt)
        {
            float side = _swingSideSpeed;
            EndSwing();
            Vector3 forward = Vector3.ClampMagnitude(_swingWay * _swingFlingX + _swingAcross * side, Balance.MaxKnockbackSpeed);
            _velocity = new Vector3(0.0f, _swingFlingY, 0.0f);
            _carryVelocity = forward;
            _carryLeft = Mathf.Clamp(_swingHold, 0.0f, 1.0f);
            // A throw with no hold (WALL: a hop off the face) had nothing to carry. With sideways speed it is held for
            // the one step it takes `StepCarry` to hand it to the body, or `Friction` would eat it in a quarter second.
            if (_carryLeft <= 0.0f && Mathf.Abs(side) > 1.0f) _carryLeft = Mathf.Max(dt, 0.0f);
            _externalVelocity = forward;
            _grounded = false;
            if (_carryLeft > 0.0f) Commit(_carryLeft);
            KeepCarryMomentumFor(_carryLeft + 0.2f);
        }
    }
}
