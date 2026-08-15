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

        private CharacterMotor _motor;

        private float _charge;
        private bool _charging;
        private float _throwLockLeft;
        private float _channel;

        public Slipper Held { get; private set; }
        public float ChargeRatio => ThrowRules.ChargeRatio(_charge);
        public float ChannelRatio { get; private set; }

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

            if (!_motor.CanAct())
            {
                CancelAll();
                return;
            }

            if (_motor.IsDefender) StepDefender(dt);
            else StepAttacker(dt);

            if (Held != null && _hand != null)
                // ⚠️ THE CARRY ROTATION IS PART OF THE POSE, not decoration. Without it the
                // slipper lies sideways across the palm.
                Held.transform.SetPositionAndRotation(
                    _hand.position, _hand.rotation * Slipper.CarryRotation);
        }

        // -------------------------------------------------------------------

        private void StepAttacker(float dt)
        {
            var intent = _motor.Intent;

            // First refusal: a tap with something grabbable at your feet is a pickup, and
            // nothing else gets to see that press.
            if (intent.JustPressed(Verb.Grab) && Held == null && TryPickup())
                return;

            if (Held == null) return;

            bool canThrow = GameServices.Round != null
                            && GameServices.Round.CanThrow(_motor)
                            && _throwLockLeft <= 0.0f;

            if (intent.JustPressed(Verb.SpecialAbility) && canThrow)
            {
                _charging = true;
                _charge = 0.0f;
            }

            if (_charging)
            {
                _charge += dt;

                // ⚠️ A THROW THAT BECOMES ILLEGAL MID-CHARGE IS CANCELLED, NOT FIRED. Walking
                // into the box while charging must not launch on release: the crosshair has
                // already greyed out, and firing anyway is the rules disagreeing with the UI.
                if (!canThrow) { _charging = false; _charge = 0.0f; return; }

                if (intent.JustReleased(Verb.SpecialAbility))
                {
                    Release();
                    _charging = false;
                }
            }
        }

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

        private void Release()
        {
            if (Held == null) return;

            Vector3 aim = _motor.Intent.AimPoint - transform.position;
            aim.y = 0.0f;
            if (aim.sqrMagnitude < 0.01f) aim = transform.forward;

            // A 45 degree launch, which is the arc every range bound in the game is solved
            // against. The preview integrates this same velocity.
            Vector3 dir = (aim.normalized + Vector3.up).normalized;

            Vector3 origin = _hand != null ? _hand.position : transform.position + Vector3.up * 1.25f;
            origin += aim.normalized * Balance.MuzzleForward;

            GameServices.Audio?.PlayAt("throw_release", origin);
            GetComponentInChildren<Visual.CharacterAnimator>()?.PlayAction("throw");
            Held.HostThrow(_motor, origin, Held.LaunchVelocity(dir, ThrowRules.ChargeRatio(_charge)));

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
            _charging = false;
            _charge = 0.0f;
            _channel = 0.0f;
            ChannelRatio = 0.0f;
        }
    }
}
