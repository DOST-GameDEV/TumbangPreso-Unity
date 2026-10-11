using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    /// <summary>
    /// ⚠️⚠️ SOMETHING LIVES ON EACH HERO'S HANDS.
    ///
    /// Owner, 2026-10-06: *"i wanted to make unique vfx for the characters' hands as theyre doing different things like
    /// falling or idling etc. but the issue is the vfx and animations that the session is giving me is just bland 2d
    /// vfx particles. i want you to apply the same mindset you did when you transformed the plain drone to a cutesy
    /// robot, and work on the fpv hands in the same way"*.
    ///
    /// So a hero's hands do not THROW effects, they CARRY a companion: a small modelled thing in the game's own toon
    /// dress and ink line, with a body, a face or a temper, that is there the whole match and acts on everything the
    /// player does (stands, walks, jumps, falls, lands, picks up a slipper, winds up, throws, is tagged, casts). One
    /// class a hero (`HandCompanion`), nothing shared between two heroes but the mood below and the springs.
    ///
    /// ⚠️ IT IS STEPPED LAST, after every layer has posed the arms, so it sits on the hands where they ARE this frame.
    /// ⚠️ IT LIVES UNDER THE ARMS' OWN ROOT, so `Framing` frames it with the arms and hides it from every other camera:
    /// only the player whose hands they are sees it. Nothing here is sent or read by anyone else.
    /// </summary>
    public sealed partial class ViewmodelArms
    {
        /// <summary>What the player is doing this frame, as a hand companion needs to know it.</summary>
        public struct HandMood
        {
            /// <summary>0 standing to 1 moving on foot, and of that how much is a sprint.</summary>
            public float Walk, Run;
            /// <summary>The body's stride, in cycles (two footfalls a cycle).</summary>
            public float GaitPhase;
            public bool Grounded;
            /// <summary>Metres a second, up positive.</summary>
            public float VerticalSpeed;
            /// <summary>A slipper is in the right hand; the throw's wind-up, 0 to 1, or below 0 when not winding up.</summary>
            public bool Carrying; public float Charge;
            /// <summary>Tagged, stunned or tripped: the body is not the player's own for a moment.</summary>
            public bool Tagged;
            /// <summary>An ability's own hand clip is playing, and which (the first-person action's name, as `PlayAction` takes it).</summary>
            public bool Casting; public string Action;
            /// <summary>Nothing authored owns the hands (the same test the air spring makes).</summary>
            public bool Free;
        }

        /// <summary>One hero's hand companion. Built once for the hero, stepped every frame, destroyed with them.</summary>
        public abstract class HandCompanion
        {
            /// <summary>False when the hero's art is missing: the hands then play without it.</summary>
            public abstract bool Build(ViewmodelArms arms);
            /// <summary>Undo anything put on the ARMS last frame (a sleeve swollen, say). Called before any layer poses them.</summary>
            public virtual void Restore(ViewmodelArms arms) { }
            public abstract void Step(ViewmodelArms arms, in HandMood mood, float dt);
            public abstract void Destroy();
        }

        /// <summary>A probe plays the hands without a body: it sets this and the companion (and the air spring) follow it.</summary>
        public HandMood? ProbeMood { get; set; }

        private HandCompanion _handCompanion;
        private string _handCompanionHero;

        /// <summary>
        /// ⚠️ A PET ON THE HAND IS NEMU'S ALONE. Owner, 2026-10-06, of all nine companions seen in play: *"i like it but we were
        /// trying to reserve the pet idea only for nemu ... we might keep this as an unlockable though"*. So Kuro is always
        /// on, and the other eight (the ember, the stones, the battery-bean, the snow bunny, the fish, the maya bird, the
        /// moth, the sampaguita bud) are KEPT, whole and working, behind this switch for a possible unlock. It is off, and
        /// nothing in the game turns it on yet: an unlock would set it for the local player before the hero is chosen.
        /// </summary>
        public static bool UnlockedHandPets;

        private static HandCompanion CompanionFor(string heroId)
        {
            if (heroId == "nemu") return new NemuKuroHand();
            // Without a pet a hero's hands are still their own: what the bare arms do (one class a hero, as they are made).
            if (!UnlockedHandPets) return heroId switch { "paete" => new PaeteVineHands(), "phaister" => new SorayaStageHands(), "amihan" => new AmihanWindHands(), "cheska" => new CheskaRimeHands(), "dante" => new DanteGauntletHands(), "sean" => new SeanFlameHands(), "rafi" => new RafiWaterHands(), "zack" => new ZackMagnetHands(), _ => null };
            return heroId switch
            {
                "sean" => new SeanEmberHand(),
                "dante" => new DanteStoneHand(),
                "rafi" => new RafiTideHand(),
                "cheska" => new CheskaFrostHand(),
                "zack" => new ZackSparkHand(),
                "amihan" => new AmihanPinwheelHand(),
                "phaister" => new SorayaMothHand(),
                "paete" => new PaeteSproutHand(),
                _ => null,
            };
        }

        // ------------------------------------------------------------------ carried by the arm's swing, never run through by it
        // ⚠️⚠️ WHAT RIDES A HAND IS CARRIED BY THAT HAND'S STRIDE AS ONE PIECE WITH IT. Owner, 2026-10-06: *"sometimes the hand
        // additions clip through the hands because of the walking animation, happens for many heros.. need you to fix
        // this"*. The stride (`RunSway`) pumps each forearm through as much as thirty degrees and several centimetres,
        // twice a second, and the things on the hands were chasing that on springs of their own: a spring is always a
        // little behind, so on every footfall the arm swung INTO whatever was meant to be sitting on it.
        //
        // So the stride is hidden from them. Just before a companion is stepped, what the stride put on the two pivots
        // is taken off; the companion places everything on arms that are not walking, where its springs have nothing
        // to fall behind; the stride is put back; and every drawn piece is then moved by exactly what the stride did to
        // the arm it is on (a rigid move, the arm's own), so it stays where it was put ON that arm. A piece between the
        // hands (a thing leaping across) takes a share of each arm's move by how near it is to each.
        // ⚠️ The pieces' own places are put back at the start of the next frame (`RestoreHandLife`), or a piece that its
        // companion does not place every frame would be moved again from where it was moved to, and creep away.
        // What a companion does about walking is still its own (it is told the stride in `HandMood`): only the arm's
        // swing is kept from it.
        // ⚠️ THE SAME FOR A JUMP AND A FALL (owner, the same day: *"also fix the clipping when jumping and falling"*). The
        // air spring throws the hands up and out, tips them back, and the hero's own flail carries each fist round a
        // loop (`AirMotion`): all of that is taken off and put back with the stride, and the pieces ride it rigidly.
        private readonly System.Collections.Generic.List<Renderer> _carried = new System.Collections.Generic.List<Renderer>();
        private readonly System.Collections.Generic.List<Transform> _carriedPiece = new System.Collections.Generic.List<Transform>();
        private readonly System.Collections.Generic.List<Vector3> _carriedPosition = new System.Collections.Generic.List<Vector3>();
        private readonly System.Collections.Generic.List<Quaternion> _carriedRotation = new System.Collections.Generic.List<Quaternion>();

        private void RestoreHandLife()
        {
            for (int i = 0; i < _carriedPiece.Count; i++)
            {
                var piece = _carriedPiece[i];
                if (piece == null) continue;
                piece.localPosition = _carriedPosition[i]; piece.localRotation = _carriedRotation[i];
            }
            _carriedPiece.Clear(); _carriedPosition.Clear(); _carriedRotation.Clear();
            _handCompanion?.Restore(this);
        }

        private void StepHandLife(float dt)
        {
            if (_handCompanionHero != _currentHeroId)
            {
                _handCompanion?.Destroy();
                _handCompanionHero = _currentHeroId;
                _handCompanion = string.IsNullOrEmpty(_currentHeroId) || !NaturalArms ? null : CompanionFor(_currentHeroId);
                if (_handCompanion != null && !_handCompanion.Build(this)) { _handCompanion.Destroy(); _handCompanion = null; }
            }
            if (_handCompanion == null) return;

            bool arms = _leftPivot != null && _rightPivot != null && _leftArm != null && _rightArm != null;
            bool walking = arms && _swayApplied, flying = arms && _airApplied;
            bool striding = walking || flying;
            Matrix4x4 leftWalking = default, rightWalking = default;
            Vector3 leftAt = default, rightAt = default; Quaternion leftTurn = default, rightTurn = default;
            if (striding)
            {
                leftWalking = _leftArm.localToWorldMatrix; rightWalking = _rightArm.localToWorldMatrix;
                leftAt = _leftPivot.localPosition; leftTurn = _leftPivot.localRotation;
                rightAt = _rightPivot.localPosition; rightTurn = _rightPivot.localRotation;
                Vector3 leftStillAt = leftAt, rightStillAt = rightAt; Quaternion leftStillTurn = leftTurn, rightStillTurn = rightTurn;
                if (flying)
                {
                    // The air's turns were put on in FRONT of what was there (`AngleAxis * rotation`): taken off the same side.
                    leftStillAt -= _airLeftOffset; rightStillAt -= _airRight;
                    leftStillTurn = Quaternion.Inverse(_airLeftAfter * Quaternion.Inverse(_airLeftBase)) * leftStillTurn;
                    rightStillTurn = Quaternion.Inverse(_airRightAfter * Quaternion.Inverse(_airRightBase)) * rightStillTurn;
                }
                if (walking)
                {
                    // The stride's turn was put on BEHIND (`rotation *= turn`): taken off that side.
                    leftStillAt -= _swayLeftOffset; rightStillAt -= _swayRightOffset;
                    leftStillTurn = leftStillTurn * Quaternion.Inverse(_swayLeftTurn); rightStillTurn = rightStillTurn * Quaternion.Inverse(_swayRightTurn);
                }
                _leftPivot.localPosition = leftStillAt; _leftPivot.localRotation = leftStillTurn;
                _rightPivot.localPosition = rightStillAt; _rightPivot.localRotation = rightStillTurn;
            }

            _handCompanion.Step(this, ProbeMood ?? ReadHandMood(), Mathf.Clamp(dt, 0f, .05f));

            if (!striding) return;
            Matrix4x4 leftStill = _leftArm.localToWorldMatrix, rightStill = _rightArm.localToWorldMatrix;
            _leftPivot.localPosition = leftAt; _leftPivot.localRotation = leftTurn;
            _rightPivot.localPosition = rightAt; _rightPivot.localRotation = rightTurn;
            CarryWithTheStride(leftWalking * leftStill.inverse, rightWalking * rightStill.inverse, leftStill, rightStill);
        }

        /// <summary>Moves every drawn piece of the companion by what the stride did to the arm it is on.</summary>
        private void CarryWithTheStride(Matrix4x4 leftMove, Matrix4x4 rightMove, Matrix4x4 leftStill, Matrix4x4 rightStill)
        {
            Quaternion leftSpin = leftMove.rotation, rightSpin = rightMove.rotation;
            // Each arm as a line from its elbow to its tip, in the world, as the companion saw it.
            Vector3 l0 = leftStill.MultiplyPoint3x4(Vector3.zero), l1 = leftStill.MultiplyPoint3x4(Vector3.up * ArmLength);
            Vector3 r0 = rightStill.MultiplyPoint3x4(Vector3.zero), r1 = rightStill.MultiplyPoint3x4(Vector3.up * ArmLength);
            for (int c = 0; c < transform.childCount; c++)
            {
                var root = transform.GetChild(c);
                if (!root.name.StartsWith("~HandCompanion")) continue;
                root.GetComponentsInChildren(true, _carried);
                // Parents come before their children in this list, so a child is placed after its parent has moved.
                int first = _carriedPiece.Count;
                for (int i = 0; i < _carried.Count; i++)
                {
                    var piece = _carried[i].transform;
                    _carriedPiece.Add(piece); _carriedPosition.Add(piece.localPosition); _carriedRotation.Add(piece.localRotation);
                }
                // Where each stands now, before any is moved (moving a parent would move the ones under it).
                for (int i = 0; i < _carried.Count; i++) { var piece = _carried[i].transform; _carriedWorld.Add(piece.position); _carriedSpin.Add(piece.rotation); }
                for (int i = 0; i < _carried.Count; i++)
                {
                    var piece = _carriedPiece[first + i];
                    Vector3 at = _carriedWorld[i]; Quaternion turn = _carriedSpin[i];
                    float toLeft = DistanceToLine(at, l0, l1), toRight = DistanceToLine(at, r0, r1);
                    // Wholly one arm's when it is much nearer that one; shared smoothly in between.
                    float share = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.3f, .7f, toLeft / Mathf.Max(1e-5f, toLeft + toRight)));
                    piece.SetPositionAndRotation(Vector3.Lerp(leftMove.MultiplyPoint3x4(at), rightMove.MultiplyPoint3x4(at), share),
                        Quaternion.Slerp(leftSpin * turn, rightSpin * turn, share));
                }
                _carriedWorld.Clear(); _carriedSpin.Clear();
            }
        }
        private readonly System.Collections.Generic.List<Vector3> _carriedWorld = new System.Collections.Generic.List<Vector3>();
        private readonly System.Collections.Generic.List<Quaternion> _carriedSpin = new System.Collections.Generic.List<Quaternion>();

        private static float DistanceToLine(Vector3 point, Vector3 from, Vector3 to)
        {
            Vector3 run = to - from;
            float along = Mathf.Clamp01(Vector3.Dot(point - from, run) / Mathf.Max(1e-6f, run.sqrMagnitude));
            return Vector3.Distance(point, from + run * along);
        }

        private HandMood ReadHandMood()
        {
            var mood = new HandMood { Grounded = true, Carrying = _carrying, Charge = _charge, Casting = _clip != null && _heroAction, Free = true };
            mood.Action = _clip != null ? _actionName : null;
            if (_characterMotor == null) return mood;
            if (_gaitAnimator == null) _gaitAnimator = _characterMotor.GetComponentInChildren<Visual.CharacterAnimator>();
            if (_gaitAnimator != null)
            {
                mood.Walk = _gaitAnimator.LocomotionArmAmount; mood.Run = _gaitAnimator.GaitRunWeight; mood.GaitPhase = _gaitAnimator.GaitPhase;
            }
            mood.Grounded = _characterMotor.IsGrounded || _characterMotor.IsSwimming;
            mood.VerticalSpeed = _characterMotor.Velocity.y;
            mood.Tagged = _characterMotor.IsStunned;
            mood.Free = _charge < 0 && _clip == null && _actionReturnLeft <= 0 && string.IsNullOrEmpty(_aimPreview)
                && !_characterMotor.IsSwimming && !_characterMotor.IsEdgeRecovering && !_characterMotor.IsFlying && !_characterMotor.IsStunned;
            return mood;
        }

        /// <summary>A sprung value: chases `Target`, overshoots and settles. The hand companions' squash and bounce.</summary>
        public struct Spring
        {
            public float Value, Speed, Target;
            public void Step(float stiffness, float damping, float dt)
            {
                // Two small steps keep a stiff spring stable at a low frame rate.
                for (int i = 0; i < 2; i++)
                {
                    float h = dt * .5f;
                    Speed += ((Target - Value) * stiffness - Speed * damping) * h;
                    Value += Speed * h;
                }
            }
            public void Snap(float to) { Value = Target = to; Speed = 0f; }
        }
    }
}
