using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// Holding, charging, throwing, and the lata reset channel.
    ///
    /// ⚠️⚠️ E DOES THREE JOBS AND PICKS BY WHAT IS IN FRONT OF YOU. Rather than inventing two
    /// more keybinds for a game whose entire brief is "simpler", the press resolves against
    /// context, and THIS COMPONENT GETS FIRST REFUSAL. Only a press that neither a pickup nor
    /// a channel consumed falls through to the shove or the lunge.
    ///
    /// | press            | condition                                   | result           |
    /// |------------------|---------------------------------------------|------------------|
    /// | E tap            | Attacker, loose slipper within PickupRadius  | pick up          |
    /// | E tap            | Attacker, nothing grabbable                  | shove, instantly |
    /// | E hold           | Defender, in the lata's ring, lata down      | reset the lata   |
    /// | E hold 0.5 s     | Defender, anything else                      | lunge            |
    ///
    /// ⚠️ WHILE THE CHANNEL IS RUNNING THE LUNGE CHARGE IS CANCELLED, so resetting the can can
    /// never fire a lunge out of it. That is the one interaction between the two that a player
    /// would otherwise hit constantly, because both are E held as the taya.
    /// </summary>
    [RequireComponent(typeof(CharacterMotor))]
    public sealed class Carrier : MonoBehaviour
    {
        [SerializeField] private Transform _hand;

        /// <summary>
        /// Where a carried slipper rides.
        ///
        /// ⚠️⚠️ THE SERIALISED FIELD ABOVE WAS NEVER ASSIGNED BY ANYTHING, IN ANY BUILD.
        /// `MatchInstaller` installs this component with `AddComponent`, which cannot carry an
        /// inspector reference (rule 3), so `_hand` was null on every unit and the block that
        /// keeps a held slipper in the hand never ran once. A picked-up tsinelas simply stayed
        /// where the pickup left it and the carrier walked away from it, which is the
        /// third-person half of *"the slippers just float when you hold it, its completely
        /// unattached to person"*. The viewmodel fix addressed the local player's own view and
        /// left every OTHER player seeing exactly the reported bug.
        ///
        /// ⚠️ RE-ASKED WHILE IT IS NULL, NOT CACHED ONCE. `CharacterVisual` rebuilds the anchor
        /// on every model swap, and a Prop round swaps models mid-match.
        /// </summary>
        private Transform Hand()
        {
            if (_hand != null) return _hand;

            var visual = GetComponent<Visual.CharacterVisual>();
            return visual != null ? visual.HandAnchor : null;
        }

        private CharacterMotor _motor;

        private float _charge;
        private bool _charging;
        private float _throwLockLeft;
        private float _channel;

        public Slipper Held { get; private set; }
        public float ChargeRatio => ThrowRules.ChargeRatio(_charge);
        public float ChannelRatio { get; private set; }

        /// <summary>True while this unit is winding a throw up. Read by the aim arc and by the
        /// YOU card's charge meter.</summary>
        public bool IsCharging => _charging;

        /// <summary>
        /// ⚠️⚠️ THE WIND-UP EVERY OTHER PLAYER CAN SEE, and it is a SEPARATE value from
        /// <see cref="ChargeRatio"/> on purpose. `carrier.gd`'s header states it: the charge
        /// clock only ticks on the peer that controls the unit, so a third-person wind-up pose
        /// driven from it is invisible to the person being aimed at — which is the whole
        /// counterplay the 2.5 s charge exists to create.
        ///
        /// -1 when nobody is winding up.
        /// </summary>
        public float ObservedChargePower =>
            _observedCharge < 0.0f ? -1.0f : Mathf.Clamp01(_observedCharge / Balance.ChargeFullTime);

        private float _observedCharge = -1.0f;

        /// <summary>
        /// Seconds until this unit may throw again after a pickup.
        ///
        /// ⚠️ EXPOSED SO THE AI CAN SEE WHAT A PLAYER SEES. A bot that plants and charges
        /// during its own throw lock stands still doing nothing visible and reads as stuck.
        /// </summary>
        public float ThrowLockLeft => _throwLockLeft;

        public bool ThrowLocked => _throwLockLeft > 0.0f;
        public bool IsBusy => _channel > 0.0f || _charging;

        private void Awake() => _motor = GetComponent<CharacterMotor>();

        /// <summary>
        /// HOST-SIDE pickup, shared by the solo path and the networked request.
        ///
        /// ⚠️ THE HOST RE-CHECKS ELIGIBILITY EVEN THOUGH THE CLIENT ALREADY DID. A client one
        /// frame behind asking for a slipper somebody else just took is ordinary, not an
        /// error; refuse it quietly rather than trusting the request.
        /// </summary>
        public void HostPickUp(Slipper what)
        {
            if (!NetAuthority.ShouldResolve()) return;
            if (what == null || !what.CanBeGrabbedBy(_motor)) return;

            what.HostGrab(_motor);
            NotifyHolding(what);
        }

        /// <summary>
        /// HOST-SIDE throw from an explicit origin and aim point, so a networked throw leaves
        /// along the line the CLIENT was aiming rather than the one the host sees a frame later.
        /// </summary>
        public void HostThrowAt(Vector3 origin, Vector3 aimPoint, float charge)
        {
            if (!NetAuthority.ShouldResolve() || Held == null) return;

            Vector3 aim = aimPoint - origin;
            aim.y = 0.0f;
            if (aim.sqrMagnitude < 0.01f) aim = transform.forward;

            // The same 45 degree launch every range bound in the game is solved against.
            Vector3 dir = (aim.normalized + Vector3.up).normalized;

            GameServices.Audio?.PlayAt("throw_release", origin);
            GetComponentInChildren<Visual.CharacterAnimator>()?.PlayAction("throw");

            Held.HostThrow(_motor, origin, Held.LaunchVelocity(dir, Mathf.Clamp01(charge)));

            Held = null;
            _motor.HoldingSlipper = false;
            _charge = 0.0f;
        }

        /// <summary>
        /// ⚠️ THE LOCK IS SET AFTER A PICKUP THE PLAYER HAS ALREADY WALKED OVER AND MADE, so
        /// it covers the beat between HAVING the slipper and being able to throw it. It is
        /// emphatically not a "return" mechanic: nothing in this game hands a slipper back,
        /// and a label implying otherwise promised a mechanic that does not exist.
        /// </summary>
        public void NotifyHolding(Slipper what)
        {
            Held = what;
            GameServices.Audio?.PlayAt("pickup", transform.position);

            // Reaching down for a loose tsinelas — the literal clip for the job.
            GetComponentInChildren<Visual.CharacterAnimator>()?.PlayAction("grab");
            _motor.HoldingSlipper = what != null;

            if (what != null)
                _throwLockLeft = what.ThrowLock;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (_throwLockLeft > 0.0f) _throwLockLeft = Mathf.Max(0.0f, _throwLockLeft - dt);

            // The observed wind-up runs on every peer, including the ones that are not driving
            // this unit. See ObservedChargePower.
            if (_observedCharge >= 0.0f)
                _observedCharge = Mathf.Min(_observedCharge + dt, Balance.ChargeFullTime);

            if (!_motor.CanAct())
            {
                CancelAll();
                return;
            }

            if (_motor.IsDefender) StepDefender(dt);
            else StepAttacker(dt);
        }

        /// <summary>
        /// Rides the hand. Godot parents the tsinelas to a `BoneAttachment3D`, so it inherits the
        /// arm bone's transform for free and cannot come off it; Unity has no bone-parenting on a
        /// SkinnedMeshRenderer, so the carry is a per-frame follow instead. Same result, and the
        /// TPP object and the viewmodel one stay in sync because both are driven from the same
        /// measured anchor rather than from two hand-tuned offsets.
        ///
        /// ⚠️⚠️ IT HAS TO BE LateUpdate, AND IN Update IT DETACHED WHENEVER A CLIP PLAYED. Unity
        /// evaluates the Animator BETWEEN Update and LateUpdate, so a bone read during Update is
        /// the pose from the PREVIOUS frame. Standing still that is invisible, because last
        /// frame's hand and this frame's hand are the same place. The moment an arm actually
        /// moves — the pickup, the wind-up, the throw, a run cycle — the slipper trails the hand
        /// by exactly one frame of animation, which is the *"slippers deattach when animations
        /// play"* report and is worst during precisely the clips a player is looking at.
        ///
        /// ⚠️ AND IT IS NOT GATED ON CanAct. A stunned carrier still holds their tsinelas, and
        /// returning early above used to leave it frozen in the air where the stun started.
        /// </summary>
        private void LateUpdate()
        {
            if (Held == null) return;

            var hand = Hand();
            if (hand == null) return;

            // ⚠️ THE CARRY ROTATION IS PART OF THE POSE, not decoration. Without it the
            // slipper lies sideways across the palm.
            //
            // ⚠️ AND THE SOLE HANGS BELOW THE ORIGIN BY AN AMOUNT THAT DIFFERS PER SKIN, so the
            // last lift comes off the drawn bounds rather than from a constant. A clog and a
            // flip-flop cannot share one number, which `slipper.gd::_attach_to_hand()` records.
            Held.transform.SetPositionAndRotation(
                hand.position + hand.up * (Held.RestHeight * hand.lossyScale.y),
                hand.rotation * Slipper.CarryRotation);
        }

        // -------------------------------------------------------------------

        private void StepAttacker(float dt)
        {
            var intent = _motor.Intent;

            // First refusal: a tap with something grabbable at your feet is a pickup, and
            // nothing else gets to see that press.
            if (intent.JustPressed(Verb.Grab) && Held == null && TryPickup())
                return;

            if (Held == null)
            {
                CancelCharge();
                return;
            }

            bool canThrow = GameServices.Round != null
                            && GameServices.Round.CanThrow(_motor)
                            && _throwLockLeft <= 0.0f;

            // ⚠️⚠️ `Pressed`, NOT `JustPressed`, TO START A CHARGE, AND THAT IS MEASURED. The
            // .gd records it: `input_probe` drove a synthetic press that lasted one frame and
            // the just-pressed form charged to 0.000. A bot writes its intent for a window
            // rather than for an edge, so the edge form silently gave every AI seat a
            // zero-power throw.
            if (!_charging && intent.Pressed(Verb.SpecialAbility))
            {
                if (!canThrow) return;

                _charging = true;
                _charge = 0.0f;
                BroadcastCharge(true);

                // The wind-up is audible as well as visible. It is the taya's cue that a throw
                // is coming, and it is the only one that reaches a player who is looking away.
                GameServices.Audio?.PlayAt("throw_charge", transform.position);
                return;
            }

            if (!_charging) return;

            if (intent.Pressed(Verb.SpecialAbility))
            {
                _charge = Mathf.Min(_charge + dt, Balance.ChargeFullTime);

                // ⚠️ STEPPING OUT OF LEGALITY MID-CHARGE CANCELS IT RATHER THAN BANKING IT.
                // Walking into the box while charging must not launch on release: the crosshair
                // has already greyed out, and firing anyway is the rules disagreeing with the UI.
                if (!canThrow) CancelCharge();
                return;
            }

            // Released.
            float power = ChargeRatio;
            CancelCharge();

            if (canThrow) Release(power);
        }

        /// <summary>
        /// Ends a wind-up from one place — released, cancelled, the slipper knocked out of the
        /// hands, the round reset. An arc left on screen after the throw has gone is worse than
        /// one that never appeared, so the clear belongs with the cancel and not at each site.
        /// </summary>
        private void CancelCharge()
        {
            if (!_charging) return;

            _charging = false;
            _charge = 0.0f;
            BroadcastCharge(false);
        }

        /// <summary>
        /// ⚠️ IT TICKS ON EVERY PEER, which is what makes the wind-up counterplay work. In the
        /// local game this is simply the same clock; the shape is kept so the networked half
        /// has one place to become an RPC.
        /// </summary>
        private void BroadcastCharge(bool active) => _observedCharge = active ? 0.0f : -1.0f;

        private bool TryPickup()
        {
            var round = GameServices.Round;
            if (round == null) return false;

            Slipper best = null;
            float bestDist = float.MaxValue;

            foreach (var s in FindObjectsByType<Slipper>(FindObjectsSortMode.None))
            {
                if (!s.CanBeGrabbedBy(_motor)) continue;

                float d = Vector3.Distance(transform.position, s.transform.position);
                if (d >= bestDist) continue;

                bestDist = d;
                best = s;
            }

            if (best == null) return false;
            if (!best.HostGrab(_motor)) return false;

            NotifyHolding(best);
            return true;
        }

        /// <summary>
        /// Where the aim ray points. ⚠️ AN AI AIMS AT A POINT IT WAS TOLD, NOT DOWN A CAMERA.
        /// A non-mouse-aimed unit's camera follows its body and its body yaw is the direction it
        /// last WALKED, so the whole cast resolves to "wherever I was heading". Measured over 20
        /// AI rounds in the original, throws that reached the can: zero.
        /// </summary>
        public Vector3 AimPoint()
        {
            if (_motor.Intent.HasAimPoint) return _motor.Intent.AimPoint;

            var rig = UnityEngine.Camera.main != null
                ? UnityEngine.Camera.main.GetComponent<CameraSystem.CameraRig>()
                : null;

            if (rig != null && rig.IsFollowing(_motor)) return rig.AimPoint();

            return transform.position + transform.forward * CameraSystem.CameraRig.AimRayLength;
        }

        /// <summary>
        /// ⚠️⚠️ THE THROW LEAVES FROM THE SIGHT LINE, NOT THE HAND, and this was measured rather
        /// than chosen. From the hand a throw sags 0.38 to 0.43 m below the line the player is
        /// aiming along and peaks within 0.2 m of them, so the slipper drops out of the bottom of
        /// the screen the instant it is released. From the sight line the same throws sag 0.001
        /// to 0.043 m. The path was right; the starting height was not.
        /// </summary>
        public Vector3 ThrowOrigin()
        {
            var rig = UnityEngine.Camera.main != null
                ? UnityEngine.Camera.main.GetComponent<CameraSystem.CameraRig>()
                : null;

            // ⚠️ THE RIG'S OWN EYE WHEN THERE IS ONE, AND THE .gd's 0.9 FALLBACK WHEN THERE IS
            // NOT. A bot has no rig looking through it, and `throw_origin_for` uses exactly this
            // constant for that case rather than the FPP eye height, so a bot's throw and a
            // probe's throw leave from the same place.
            Vector3 eye = rig != null && rig.IsFollowing(_motor)
                ? rig.transform.position
                : transform.position + Vector3.up * 0.9f;

            Vector3 toAim = AimPoint() - eye;
            if (toAim.magnitude < 0.01f) return eye;

            return eye + toAim.normalized * Balance.MuzzleForward;
        }

        /// <summary>
        /// The velocity the throw WOULD leave with right now.
        ///
        /// ⚠️ THE AIM ARC ASKS THIS SAME FUNCTION, so the dotted line and the flight are one
        /// line by construction rather than by two implementations agreeing. A per-skin launch
        /// speed applied to only one of them would quietly land the preview where a neutral
        /// slipper lands and the real one five per cent away.
        /// </summary>
        public Vector3 LaunchVelocityNow()
        {
            if (Held == null) return Vector3.zero;

            Vector3 aim = AimPoint() - transform.position;
            aim.y = 0.0f;
            if (aim.sqrMagnitude < 0.01f) aim = transform.forward;

            // A 45 degree launch, which is the arc every range bound in the game is solved
            // against.
            Vector3 dir = (aim.normalized + Vector3.up).normalized;
            return Held.LaunchVelocity(dir, ChargeRatio);
        }

        private void Release(float power)
        {
            if (Held == null) return;

            Vector3 aim = AimPoint() - transform.position;
            aim.y = 0.0f;
            if (aim.sqrMagnitude < 0.01f) aim = transform.forward;

            Vector3 dir = (aim.normalized + Vector3.up).normalized;
            Vector3 origin = ThrowOrigin();

            GameServices.Audio?.PlayAt("throw_release", origin);
            GetComponentInChildren<Visual.CharacterAnimator>()?.PlayAction("throw");
            Held.HostThrow(_motor, origin, Held.LaunchVelocity(dir, power));

            Held = null;
            _motor.HoldingSlipper = false;
            _charge = 0.0f;
        }

        // -------------------------------------------------------------------

        private void StepDefender(float dt)
        {
            var round = GameServices.Round;
            var lata = round?.Lata;
            var intent = _motor.Intent;

            bool inRing = lata != null
                          && Vector3.Distance(
                                 new Vector3(transform.position.x, 0, transform.position.z),
                                 new Vector3(lata.transform.position.x, 0, lata.transform.position.z))
                             <= Balance.InteractionRadius;

            bool canChannel = lata != null && !lata.IsUpright && inRing;

            if (!canChannel || !intent.Pressed(Verb.Grab))
            {
                // ⚠️ LETTING GO ZEROES THE CHANNEL. It does not pause and it does not decay:
                // a partial reset that survives being interrupted would let the taya nibble
                // at it between throws, which is exactly the pressure the channel creates.
                _channel = 0.0f;
                ChannelRatio = 0.0f;
                return;
            }

            _channel += dt;
            ChannelRatio = Mathf.Clamp01(_channel / lata.ResetChannelTime);

            if (_channel >= lata.ResetChannelTime)
            {
                lata.HostRestore();
                _channel = 0.0f;
                ChannelRatio = 0.0f;
            }
        }

        private void CancelAll()
        {
            CancelCharge();
            _channel = 0.0f;
            ChannelRatio = 0.0f;
        }
    }
}
