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

        /// <summary>
        /// Where this slipper is going, for anything that has to predict it.
        ///
        /// ⚠️ READ BY THE TAYA'S INTERCEPT, which walks this forward under gravity to find the
        /// part of the arc a body can actually stand in. Without it the AI could only ask
        /// "is anything in flight", which is not a point to run to.
        /// </summary>
        public Vector3 Velocity => _velocity;
        private float _flightTime;
        private int _throwerSlot = -1;
        private float _throwerIgnoreLeft;

        public float FlightScale => Roster.SlipperFlightScale(_skinIndex);
        public float ThrowLock => ThrowRules.ThrowLockFor(_skinIndex);

        /// <summary>
        /// How a carried tsinelas is turned in the hand.
        ///
        /// ⚠️ CONVERTED FROM THE .gd's BAKED `CARRY_BASIS`, which is X=(0,0,1), Y=(0,1,0),
        /// Z=(-1,0,0) — a quarter turn about Y. Under the handedness flip that becomes
        /// Z=(1,0,0) forward with Y up, i.e. +90° about Y here. Without it the slipper lies
        /// across the palm sideways, which reads as a bug in the grab rather than in a
        /// rotation nobody applied.
        /// </summary>
        public static readonly Quaternion CarryRotation = Quaternion.Euler(0.0f, 90.0f, 0.0f);

        /// <summary>
        /// Light the slipper that belongs to the local player.
        ///
        /// ⚠️ PER-PEER, DELIBERATELY NOT REPLICATED. "Yours" is a different slipper on every
        /// machine, so this is computed locally and never sent — a networked glow would light
        /// one slipper for everybody.
        /// </summary>
        public void SetOwnerGlow(bool mine)
        {
            if (_glowOn == mine) return;
            _glowOn = mine;

            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                var block = new MaterialPropertyBlock();
                r.GetPropertyBlock(block);

                float rim = mine ? Balance.OwnerRimStrength : 0.0f;
                block.SetFloat("_RimStrength", rim);

                // Most built-in shaders have no rim term, so also lift the emission — which
                // every standard shader does honour — or the glow is invisible on the
                // placeholder materials and reads as "the feature was never built".
                block.SetColor("_EmissionColor",
                    mine ? UI.UiTheme.Highlight * Balance.OwnerRimStrength : Color.black);

                r.SetPropertyBlock(block);
            }
        }

        private bool _glowOn;

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

        /// <summary>
        /// The velocity that puts this slipper THROUGH a point, at the speed its charge buys.
        ///
        /// ⚠️⚠️ THE PORT THREW AT A FIXED 45 DEGREES AND THIS IS THE FUNCTION THAT WAS MISSING.
        /// `slipper.gd::_solve_arc` solves the launch ANGLE for the target, and its header notes
        /// that `trajectory_preview.gd` calls the same function precisely so the dotted line and
        /// the flight cannot drift apart. Unity had the "cannot drift apart" half — the preview
        /// integrates whatever `Carrier` hands it — around the wrong velocity.
        ///
        /// What a fixed 45 does to the game: the throw ignores where the player is aiming
        /// vertically and lobs at maximum-range angle whatever the distance, so a can eight
        /// metres away and a can two metres away get the same towering arc, and the aim line
        /// leaves the frame near-vertically instead of pointing at the mark. Reported against a
        /// cropped frame of exactly that: *"THIS charge outline is so ugly, it doesnt behave
        /// naturally"*.
        ///
        /// ⚠️ THE LOW ROOT, NOT THE HIGH ONE. `(v² - root)` is the flatter of the two solutions:
        /// less airtime, harder to body-block, and the one a person throwing a slipper at a can
        /// actually produces. The .gd drops the lob root for the same reason.
        ///
        /// ⚠️ AND OUT OF RANGE THROWS ALONG THE PLAYER'S OWN LINE rather than falling back to
        /// 45. The .gd is explicit about why: aiming at a distant wall would otherwise fire a
        /// lob straight up, which is a far stranger thing to have happen than a throw that
        /// visibly does not get there.
        /// </summary>
        public Vector3 LaunchVelocityTo(Vector3 origin, Vector3 target, float charge01)
        {
            float speed = ThrowRules.LaunchSpeedFor(_skinIndex, charge01);
            return SolveArc(origin, target, speed) * speed;
        }

        /// <summary>The unit launch direction from origin to target at a given speed.</summary>
        public static Vector3 SolveArc(Vector3 origin, Vector3 target, float speed)
        {
            Vector3 toTarget = target - origin;
            Vector3 flat = new Vector3(toTarget.x, 0.0f, toTarget.z);
            float distance = flat.magnitude;

            // Straight up, straight down, or on top of us: no arc to solve, throw along the
            // line. Also guards the division below.
            if (distance < 0.05f || speed < 0.01f)
                return toTarget.magnitude > 0.01f ? toTarget.normalized : Vector3.forward;

            float g = Balance.Gravity;
            float v2 = speed * speed;

            float discriminant = v2 * v2 - g * (g * distance * distance + 2.0f * toTarget.y * v2);

            if (discriminant < 0.0f) return toTarget.normalized;

            float root = Mathf.Sqrt(discriminant);
            float tangent = (v2 - root) / (g * distance);

            return (flat.normalized + Vector3.up * tangent).normalized;
        }

        /// <summary>
        /// The HELD skin's own resting origin height, measured off the mesh rather than assumed.
        ///
        /// ⚠️ THE FOURTH SKIN MISSES BY 0.263 m AND THE ARC IS NOT THE BUG. A crocs RESTS
        /// 0.161 m off the ground against the other three at 0.034 to 0.056, while a preview
        /// that stops at a fixed floor epsilon draws its line to the ground. The line is right;
        /// the tall skin simply stops higher.
        /// </summary>
        public float RestHeight
        {
            get
            {
                var r = GetComponentInChildren<Renderer>();
                return r != null ? Mathf.Max(Balance.SlipperRestHeight, r.bounds.extents.y)
                                 : Balance.SlipperRestHeight;
            }
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
        /// <remarks>
        /// ⚠️⚠️ IT TELLS THE CARRIER ITSELF, RATHER THAN LEAVING THAT TO THE CALLER.
        ///
        /// **This was not a live bug**, and saying so matters: all three shipped callers
        /// (`Carrier.HostPickUp`, `Carrier.TryGrab`, and the networked grab through `MatchRpc`)
        /// already called `NotifyHolding` on the very next line. It was a TRAP, found by a test
        /// that called this directly and got a slipper that believed it was held, a motor that
        /// reported holding one, and a `Carrier` with nothing in hand — after which nothing
        /// moved the slipper (the carry reads `Held`) and the local player's viewmodel stayed
        /// empty (the rig reads `Held` too). That is *"the slippers just float when you hold
        /// it"* one careless call site away, in a codebase where the pickup already has three.
        ///
        /// `slipper.gd` owns this relationship for exactly that reason and says so: *"Written
        /// only by `slipper.gd` through `notify_holding()`, so there is one owner of the
        /// relationship. Two writers of the same relationship is how it ends up half-cleared."*
        /// The port had two. The existing calls stay and are harmless — `NotifyHolding` is
        /// idempotent now — but nothing depends on them any more.
        /// </remarks>
        public bool HostGrab(CharacterMotor who)
        {
            if (!CanBeGrabbedBy(who)) return false;

            State = SlipperState.Held;
            Holder = who;
            who.HoldingSlipper = true;
            _velocity = Vector3.zero;

            who.GetComponent<Carrier>()?.NotifyHolding(this);
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
                    if (!HitsBody(p)) continue;

                    HostBlockedBy(p);
                    return;
                }
            }

            // ⚠️ AGAINST THE GROUND UNDER IT, NOT AGAINST WORLD ZERO. See GroundY: a flight that
            // ends at an absolute height either stops in mid-air over a raised slab or carries on
            // through a lowered one, and both were reachable on the shipped maps.
            if (transform.position.y <= GroundY(transform.position) + Balance.SlipperRestHeight
                || _flightTime >= Balance.MaxFlightTime)
                Land();
        }

        /// <summary>
        /// Does the slipper's current position sit inside this body's capsule?
        ///
        /// ⚠️⚠️ A CAPSULE, NOT A SPHERE, AND THE PORT ASKED FOR A SPHERE AROUND THE FEET. 🧑 on
        /// this build: *"the players dont block the slipper thats thrown, theyre supposed to"*.
        /// The old test was
        ///
        ///     Vector3.Distance(p.transform.position, transform.position) &gt; HitRadius + radius
        ///
        /// and `p.transform.position` on these seats is the SOLE OF THE FOOT — `BuildSeat` gives
        /// the controller `center = (0, 0.8, 0)` precisely because the origin is at the feet. So
        /// the test asked whether the slipper was within 0.58 m OF THE GROUND AT THEIR TOES. A
        /// throw travels at chest height: measured against a 1.6 m seat, a slipper passing dead
        /// centre through the torso is about 1.2 m from that origin, more than double the
        /// window. The block could only ever fire on a slipper already rolling along the floor,
        /// which is to say never during a throw. The taya's entire passive verb was unreachable,
        /// and so was the friendly fire that keeps three attackers honest around one box.
        ///
        /// `slipper.gd::_first_body_hit` splits it in two on purpose, and its own comment says
        /// why: *"Capsule, not sphere: a slipper passing over a crouched head and one passing
        /// through a chest are different events and a sphere conflates them."* Flat distance
        /// decides whether the throw is on the body's LINE; the height band decides whether it
        /// is at a height the body occupies.
        ///
        /// ⚠️ THE BAND IS BUILT FROM THE CONTROLLER'S OWN CENTRE, NOT FROM THE TRANSFORM. The
        /// .gd's `dy` is measured against a CENTRED body, so it can write ±height/2 directly.
        /// Copying that expression literally against a feet-origin transform reintroduces the
        /// same off-by-a-body error one layer down, and it would look like a faithful port.
        /// </summary>
        private bool HitsBody(CharacterMotor who)
        {
            var cc = who.GetComponent<CharacterController>();

            // ⚠️ GUARDED. The old line dereferenced this straight off a `GetComponent` and a
            // seat mid-teardown at a round boundary is a null here, not a bug worth throwing on.
            float radius = cc != null ? cc.radius : 0.4f;
            float height = cc != null ? cc.height : 1.6f;

            Vector3 centre = who.transform.position + (cc != null ? cc.center : new Vector3(0.0f, height * 0.5f, 0.0f));

            Vector3 flat = new Vector3(transform.position.x - centre.x, 0.0f,
                                       transform.position.z - centre.z);

            if (flat.magnitude > Balance.SlipperHitRadius + radius) return false;

            float dy = transform.position.y - centre.y;
            return dy >= -height * 0.5f && dy <= height * 0.5f;
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
            // A body block is a hit too — it is the taya's entire passive verb.
            blocker.GetComponentInChildren<Visual.CharacterVisual>()?.FlashHit();
            Visual.ImpactBurst.SpawnAt(blocker.transform.position);

            // ⚠️⚠️ AND IT MAKES A SOUND, WHICH IT DID NOT. `slipper.gd:1170` plays `hit_body` on
            // exactly this path. This function's own header says a verb with no feedback is a
            // verb the player cannot tell they performed, and it then gave the block a flash and
            // a burst and left it silent — so the one thing a taya can do without pressing
            // anything was the one thing they could not hear.
            GameServices.Audio?.PlayAt("hit_body", transform.position);

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

        /// <summary>
        /// The height of the floor under a point, or 0 when nothing is there.
        ///
        /// ⚠️⚠️ NEITHER ARENA'S FLOOR IS AT y = 0, AND EVERY PLACE THIS PORT ASSUMED IT WAS PUT A
        /// SLIPPER IN THE AIR. Eskinita's asphalt is a slab with its own thickness and Bayan
        /// Plaza's paving is another; a rest height added to world zero is only correct by
        /// accident. Cast from well above and take the first hit, which is what "lying on the
        /// ground" means whatever the map is built out of.
        ///
        /// ⚠️ TRIGGERS ARE IGNORED. The confinement chalk, the hazard discs and the kill plane
        /// are all colliders a downward ray would otherwise stop on, and a slipper resting on
        /// the chalk's trigger volume floats by exactly its thickness.
        ///
        /// ⚠️ AND A MISS RETURNS ZERO RATHER THAN LEAVING THE SLIPPER WHERE IT WAS. Off the edge
        /// of the map there is no floor to rest on, and `VoidY` already owns that case: a
        /// slipper below the world is recovered to its spawn, not balanced on nothing.
        /// </summary>
        public static float GroundY(Vector3 at)
        {
            var from = new Vector3(at.x, at.y + 6.0f, at.z);

            var hits = Physics.RaycastAll(from, Vector3.down, 40.0f, ~0,
                                          QueryTriggerInteraction.Ignore);

            float best = float.NegativeInfinity;

            foreach (var hit in hits)
            {
                // ⚠️⚠️ A BODY IS NOT THE GROUND, AND SKIPPING THIS PUT SLIPPERS ON PEOPLE'S
                // HEADS. Every slipper starts at its owner's FEET, so the first thing a downward
                // cast from above that mark meets is the owner's own capsule — and the slipper
                // was then placed 2 m up, out of its own pickup radius. `AnyAttackerCanPickUpAny
                // Slipper` caught it immediately: the attacker was standing on a grabbable
                // tsinelas that was actually floating over their head.
                if (hit.collider.GetComponentInParent<CharacterMotor>() != null) continue;
                if (hit.collider.GetComponentInParent<Slipper>() != null) continue;
                if (hit.collider.GetComponentInParent<Lata>() != null) continue;

                if (hit.point.y > best) best = hit.point.y;
            }

            return float.IsNegativeInfinity(best) ? 0.0f : best;
        }

        private void Land()
        {
            State = SlipperState.Loose;
            _velocity = Vector3.zero;

            // ⚠️ THE TUMBLE IS CLEARED ON LANDING. `_apply_landed` sets `rotation = Vector3.ZERO`
            // on both the body and its Visual, and skipping it leaves the slipper resting at
            // whatever angle the spin happened to stop at — standing on its edge, or on its toe,
            // or half inside the road. It is also the pose the pickup prompt is judged against.
            transform.rotation = Quaternion.identity;

            var visual = transform.Find("Visual");
            if (visual != null) visual.localRotation = Quaternion.identity;

            // ⚠️ THE SKIN'S OWN REST HEIGHT, NOT A LITERAL. The four skins rest between 0.034
            // and 0.161 off the ground, so one number leaves the tall one buried.
            //
            // ⚠️⚠️ AND IT IS MEASURED FROM THE GROUND UNDERNEATH, NOT FROM WORLD ZERO. This read
            // `Mathf.Max(p.y, RestHeight)`, which is only the ground when the ground happens to
            // be at y = 0 — and neither arena's floor is. 🧑 2026-08-16, on a Bayan Plaza
            // capture: *"also ur slippers are floating"*. The .gd's own note is about the mesh
            // ORIGIN sitting at the volume centroid, so "resting on the floor is half a slipper
            // up" — half a slipper up FROM THE FLOOR, which is what has to be found first.
            Vector3 p = transform.position;
            transform.position = new Vector3(p.x, GroundY(p) + RestHeight, p.z);

            // ⚠️ AND IT MAKES A SOUND. A throw that hit a body played one cue and a throw that
            // hit the can played another, but a throw that simply MISSED, 38 of 71 flights in
            // the baseline, landed in silence. The most common outcome was the one the game
            // said nothing about.
            GameServices.Audio?.PlayAt("slipper_land", transform.position);
        }
    }
}
