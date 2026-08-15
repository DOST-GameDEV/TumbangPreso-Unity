using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    public enum SlipperState
    {
        Loose,      // on the ground, grabbable
        Held,       // in somebody's hand
        InFlight,   // thrown
    }

    /// <summary>
    /// The tsinelas. Ammunition, and the thing the whole game is actually about.
    ///
    /// ⚠️ THE GAME'S THESIS IS THE RETRIEVAL, NOT THE THROW. Throwing is safe and free;
    /// getting your slipper back is what costs you, because an Attacker becomes taggable the
    /// moment they pick one up inside the box. Anything that makes retrieval cheaper is a
    /// change to the core loop, not a convenience.
    /// </summary>
    public sealed class Slipper : MonoBehaviour
    {
        [SerializeField] private int _skinIndex = -1;
        [SerializeField] private int _ownerSlot = -1;

        public int SkinIndex { get => _skinIndex; set => _skinIndex = value; }

        /// <summary>
        /// ⚠️⚠️ OWNERSHIP IS A LABEL, NOT A LOCK. Any attacker may pick up any slipper. This
        /// was reversed twice in one day and BOTH instructions are worth knowing, because the
        /// second is not a correction of a mistake, it is a different call on the same
        /// trade-off. The strict version deletes the three-way rivalry: if any slipper serves
        /// any attacker the nearest is always correct and there is nothing to contest. The
        /// open version keeps the contest and moves it, because a slipper you can LOSE to a
        /// rival is more contested than one nobody may touch.
        ///
        /// `OwnerSlot` still exists and is still assigned at round start: it is what the foot
        /// arrow and the owner glow read, so "which one is mine" stays a well-defined
        /// question. It simply does not gate <see cref="CanBeGrabbedBy"/>.
        /// </summary>
        public int OwnerSlot { get => _ownerSlot; set => _ownerSlot = value; }

        public SlipperState State { get; private set; } = SlipperState.Loose;
        public CharacterMotor Holder { get; private set; }

        private Vector3 _velocity;
        private float _flightTime;
        private int _throwerSlot = -1;
        private float _throwerIgnoreLeft;

        public float FlightScale => Roster.SlipperFlightScale(_skinIndex);
        public float ThrowLock => ThrowRules.ThrowLockFor(_skinIndex);

        /// <summary>
        /// ⚠️ THE PREVIEW MUST CALL THIS SAME FUNCTION. The dotted aim arc and the real
        /// flight stay one line only while both integrate the velocity produced here. They
        /// were measured agreeing to 0.000 m on three of the four skins.
        ///
        /// ⚠️ AND THE THROW LEAVES FROM THE SIGHT LINE, NOT THE HAND. Measured: from the
        /// hand, the flight sags 0.38 to 0.43 m below the line the player is aiming along and
        /// peaks within 0.2 m of them, so the slipper drops out of the bottom of the screen
        /// the instant it is released. From the sight line it is 0.001 to 0.043 m. The path
        /// was always right; the starting height was not.
        /// </summary>
        public Vector3 LaunchVelocity(Vector3 aimDirection, float charge01)
        {
            float speed = ThrowRules.LaunchSpeedFor(_skinIndex, charge01);
            return aimDirection.normalized * speed;
        }

        public bool CanBeGrabbedBy(CharacterMotor who)
        {
            if (State != SlipperState.Loose || who == null) return false;
            if (who.IsDefender) return false;   // the taya has the tag, not the ammunition
            if (!who.CanAct()) return false;

            float d = Vector3.Distance(who.transform.position, transform.position);
            return d <= Balance.PickupRadius;
        }

        /// <summary>
        /// ⚠️ CONTESTED PICKUPS RESOLVE HOST-SIDE, and the ordering is what makes it safe: the
        /// first grab moves the slipper out of LOOSE, so a same-frame second grab fails on the
        /// first line of <see cref="CanBeGrabbedBy"/>. There is no window in which two
        /// attackers both succeed.
        /// </summary>
        public bool HostGrab(CharacterMotor who)
        {
            if (!CanBeGrabbedBy(who)) return false;

            State = SlipperState.Held;
            Holder = who;
            who.HoldingSlipper = true;
            _velocity = Vector3.zero;
            return true;
        }

        public void HostThrow(CharacterMotor thrower, Vector3 origin, Vector3 velocity)
        {
            State = SlipperState.InFlight;
            _throwerSlot = thrower != null ? thrower.PlayerSlot : -1;

            if (thrower != null) thrower.HoldingSlipper = false;
            Holder = null;

            transform.position = origin;
            _velocity = velocity;
            _flightTime = 0.0f;

            // You cannot block your own throw on release.
            _throwerIgnoreLeft = Balance.ThrowerIgnoreTime;
        }

        /// <summary>
        /// Host-side flight. ⚠️ EVERY CONTACT HERE IS A DISTANCE CHECK, deliberately: an
        /// overlap volume fires on whichever peer owns the body, and 16 of 36 were measured
        /// failing to land.
        /// </summary>
        private void FixedUpdate()
        {
            if (State != SlipperState.InFlight) return;

            float dt = Time.fixedDeltaTime;
            _flightTime += dt;
            if (_throwerIgnoreLeft > 0.0f) _throwerIgnoreLeft -= dt;

            _velocity.y -= Balance.Gravity * dt;
            transform.position += _velocity * dt;

            BounceOffBounds();
            SpinInFlight(dt);

            // ⚠️ LOST BELOW THE WORLD IS A REAL CASE, NOT A SAFETY NET. A slipper that
            // clears the arena falls forever and the round quietly loses a piece of its
            // ammunition — the attacker who owns it has nothing to fetch and simply stops
            // playing. Return it to its mark instead.
            if (transform.position.y < Balance.VoidY) { Land(); return; }

            var round = GameServices.Round;

            // The can first: it is the thing being aimed at.
            if (round?.Lata != null && round.Lata.IsUpright && round.Lata.Connects(transform.position))
            {
                round.Lata.HostKnockDown(_throwerSlot);
                Deflect(-_velocity.normalized * Balance.LataRecoilScale * _velocity.magnitude,
                        Balance.LataRecoilLiftScale);
                return;
            }

            // ⚠️ THEN ANY STANDING BODY, ATTACKERS INCLUDED. Three of them crowding one box
            // means friendly fire is part of the traffic, and a slipper that passed through
            // teammates would make the Defender's body block the only block in the game.
            if (round != null && _throwerIgnoreLeft <= 0.0f)
            {
                foreach (var p in round.Players)
                {
                    if (p == null || p.PlayerSlot == _throwerSlot) continue;

                    float d = Vector3.Distance(p.transform.position, transform.position);
                    if (d > Balance.SlipperHitRadius + p.GetComponent<CharacterController>().radius)
                        continue;

                    HostBlockedBy(p);
                    return;
                }
            }

            if (transform.position.y <= Balance.SlipperRestHeight
                || _flightTime >= Balance.MaxFlightTime)
                Land();
        }

        /// <summary>
        /// Keep a throw inside the arena.
        ///
        /// ⚠️ ENERGY IS LOST ON THE BOUNCE. A perfectly elastic wall returns a slipper at
        /// throw speed, which is a projectile nobody threw and which can still knock the lata
        /// down — a point scored by the wall. 0.45 carries it clear of the boundary without
        /// being a shot.
        ///
        /// ⚠️ THE FORM IS `-sign(position) * abs(velocity)`, NOT A PLAIN SIGN FLIP. A flip
        /// would send a slipper that is somehow ALREADY outside and travelling inward back
        /// out again — and being outside is exactly the state this exists to recover from, so
        /// it must not have a way to make it worse. This form always ends up pointing at the
        /// court.
        /// </summary>
        private void BounceOffBounds()
        {
            float limitX = AIController.PlayableHalfX - Balance.SlipperHitRadius;
            float limitZ = AIController.PlayableHalfZ - Balance.SlipperHitRadius;

            Vector3 p = transform.position;

            if (limitX > 0.0f && Mathf.Abs(p.x) > limitX)
            {
                p.x = Mathf.Sign(p.x) * limitX;
                _velocity.x = -Mathf.Sign(p.x) * Mathf.Abs(_velocity.x) * Balance.BounceRestitution;
            }

            if (limitZ > 0.0f && Mathf.Abs(p.z) > limitZ)
            {
                p.z = Mathf.Sign(p.z) * limitZ;
                _velocity.z = -Mathf.Sign(p.z) * Mathf.Abs(_velocity.z) * Balance.BounceRestitution;
            }

            transform.position = p;
        }

        /// <summary>
        /// ⚠️ SPIN AND TUMBLE AT ONCE. A real thrown slipper does both; doing only the spin is
        /// what made an earlier version read as "flying perfectly flat".
        ///
        /// ⚠️ AND IT ROTATES ABOUT THE MESH CENTRE. With the origin at the sole, a thrown
        /// slipper orbits its own underside instead of spinning in place.
        /// </summary>
        private void SpinInFlight(float dt)
        {
            transform.Rotate(Vector3.up, Balance.SlipperSpinSpeedDeg * dt, Space.Self);
            transform.Rotate(Vector3.right, Balance.SlipperTumbleSpeedDeg * dt, Space.Self);
        }

        /// <summary>
        /// ⚠️⚠️ A BLOCK COSTS THE TAYA POSITION, AND THAT IS THE POINT. Body-blocking is the
        /// taya's entire passive verb, and until it was fixed the only thing it produced was a
        /// sound at a world position: no flash, no recoil, nothing on the blocker's own
        /// screen. A verb with no feedback is a verb the player cannot tell they performed.
        ///
        /// ⚠️ A PUSH AND NOT A STUN, AND THAT WAS A DELIBERATE REVERSAL. A stagger was the
        /// obvious way to make blocking cost something and it is wrong: three attackers
        /// throwing at one box would chain stuns onto the defender, and Max() bounds the
        /// DURATION of one stun without bounding how often the next one starts. Knockback
        /// costs position, which is what a block is actually about, and cannot lock anybody
        /// out of the game.
        /// </summary>
        private void HostBlockedBy(CharacterMotor blocker)
        {
            float speed = Combat.BlockKnockbackSpeed(_skinIndex, blocker.CharacterIndex);
            Vector3 along = _velocity;
            along.y = 0.0f;
            blocker.ApplyImpulse(along.normalized * speed);

            // ⚠️ AWAY FROM THE BLOCKER, NOT MIRRORED. A true reflection sends it wherever the
            // incoming angle points, which is as often as not deeper into the box.
            Vector3 away = blocker.transform.position - transform.position;
            away.y = 0.0f;
            Deflect(-away.normalized * Balance.LaunchSpeed * Balance.DeflectSpeedScale, 1.0f);
        }

        private void Deflect(Vector3 horizontal, float liftScale)
        {
            _velocity = horizontal;
            _velocity.y = Balance.DeflectLift * liftScale;
            _flightTime = 0.0f;
            _throwerSlot = -1; // a deflected slipper credits nobody
        }

        private void Land()
        {
            State = SlipperState.Loose;
            _velocity = Vector3.zero;

            Vector3 p = transform.position;
            transform.position = new Vector3(p.x, 0.045f, p.z);

            // ⚠️ AND IT MAKES A SOUND. A throw that hit a body played one cue and a throw that
            // hit the can played another, but a throw that simply MISSED, 38 of 71 flights in
            // the baseline, landed in silence. The most common outcome was the one the game
            // said nothing about.
            GameServices.Audio?.PlayAt("slipper_land", transform.position);
        }
    }
}
