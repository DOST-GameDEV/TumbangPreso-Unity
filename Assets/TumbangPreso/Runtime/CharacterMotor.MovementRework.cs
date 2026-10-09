using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// The body's side of the movement rework prototype (`MovementRework`, a debug switch, offline only). Everything
    /// here is reached only for the LOCAL HUMAN'S body while the switch is on; with it off `FixedUpdate` runs the one
    /// velocity write it always has.
    ///
    ///   * GROUND: friction, then acceleration toward the wish (`MovementRework.GroundFriction`, `GroundAccel`), so a
    ///     start, a stop and a turn each take a moment and speed above the wish bleeds off instead of vanishing.
    ///   * AIR: the horizontal velocity is KEPT with no keys held. With keys, they steer it: the direction of travel is
    ///     pushed round toward the wish while the speed stays what it was, with a small gain for steering across the
    ///     travel, up to the cap.
    ///   * JUMP: buffered a moment before landing, forgiven a moment after a ledge, and repeated while held. The step
    ///     that jumps skips the ground friction, so speed carried into a landing is carried out of it: the bunny hop.
    ///   * CROUCH: the capsule is shortened from the top (the feet stay put), the walk is slower, sprint is refused, and
    ///     standing waits for headroom.
    ///   * SLIDE: crouch at a sprint. A boost along the way it was going, little friction, a little steering; it ends
    ///     when it has slowed to a crouch walk, when crouch is let go, or into a jump that keeps its speed.
    /// </summary>
    public sealed partial class CharacterMotor
    {
        private bool _rwCrouched, _rwSliding, _rwCrouchHeldPrev, _rwJumpQueued, _rwCapsuleKept;
        private float _rwSlideLeft, _rwSlideCooldown, _rwJumpBuffer, _rwSinceGrounded, _rwYawPrev;
        private float _rwStandHeight, _rwLeapHandedOverUntil;
        private Vector3 _rwStandCentre;
        private static readonly Collider[] RwOverlap = new Collider[16];

        private bool ReworkDrivesThisBody => MovementRework.Active && IsLocalHuman && !IsSwimming && !IsFlying;

        /// <summary>True while the rework has this body crouched or sliding (the animator and the camera read these).</summary>
        public bool ReworkCrouched => _rwCrouched;
        public bool ReworkSliding => _rwSliding;
        /// <summary>How far the first-person eye sits below standing, metres; the camera eases toward it.</summary>
        public float ReworkEyeDropTarget => _rwSliding ? MovementRework.SlideEyeDrop : _rwCrouched ? MovementRework.CrouchEyeDrop : 0f;
        /// <summary>Flat speed, metres a second, for the prototype map's readout.</summary>
        public float ReworkFlatSpeed => new Vector2(_velocity.x, _velocity.z).magnitude;

        /// <summary>Read by `ApplyGravity`: the rework wants the grounded jump to fire this step.</summary>
        private bool ReworkJumpQueued => _rwJumpQueued;
        /// <summary>Read by `ApplyGravity`: how much of the jump a tired body gets (1 with the switch off).</summary>
        private float ReworkJumpScale => _rwJumpScale;
        private float _rwJumpScale = 1f, _rwJumpFatigue, _rwFloorTime, _rwRecoveryBlockedUntil;
        /// <summary>True while hopping keeps the stamina bar from refilling (`CharacterMotor.Voodoo.cs`, where recovery is gated).</summary>
        public bool ReworkBlocksRecovery => MovementRework.Active && Time.time < _rwRecoveryBlockedUntil;
        /// <summary>Which side this airtime's strafe is earning on, and which side the airtime before earned on (0 none).</summary>
        private int _rwGainSide, _rwLastGainSide;
        /// <summary>0 rested to 1 spent, for the prototype map's readout.</summary>
        public float ReworkJumpFatigue => _rwJumpFatigue;

        /// <summary>
        /// The horizontal velocity for this step. Returns false when the rework does not drive this body, and the caller
        /// writes the velocity the way it always has.
        /// </summary>
        private bool StepReworkVelocity(Vector3 wish, float speed, float dt)
        {
            _rwJumpQueued = false; _rwJumpScale = 1f;
            if (!ReworkDrivesThisBody) { _rwJumpFatigue = 0f; StandUpFromRework(force: true); return false; }

            // The jump: buffered before the landing, repeated while held.
            _rwJumpBuffer = Intent.JustPressed(Verb.Jump) ? MovementRework.JumpBufferSeconds : Mathf.Max(0f, _rwJumpBuffer - dt);
            _rwSinceGrounded = _grounded ? 0f : _rwSinceGrounded + dt;
            bool wantsJump = _rwJumpBuffer > 0f || (MovementRework.AutoHop && Intent.Pressed(Verb.Jump));
            bool groundJump = wantsJump && _grounded && _velocity.y <= 0f;
            // A step off a ledge is still a jump for a moment, if it was a step and not a jump.
            bool coyoteJump = !groundJump && _rwJumpBuffer > 0f && !_grounded && _velocity.y <= 0f
                              && _rwSinceGrounded <= MovementRework.CoyoteSeconds;
            if (groundJump) { _rwJumpQueued = true; _rwJumpBuffer = 0f; }
            if (coyoteJump)
            {
                _rwJumpBuffer = 0f; _rwSinceGrounded = MovementRework.CoyoteSeconds + 1f;
                _velocity.y = Balance.JumpVelocity;
                NetCue.PlayVaried("jump", transform.position, 0.96f, 1.08f, 0.9f);
                GetComponentInChildren<Visual.CharacterSquashStretch>()?.Stretch(0.20f);
            }

            var v = new Vector2(_velocity.x, _velocity.z);
            var w = new Vector2(wish.x, wish.z);
            if (w.sqrMagnitude > 1e-6f) w.Normalize(); else w = Vector2.zero;
            float flat = v.magnitude;
            float run = Stamina.MovementSpeed(_isDefender, true);
            // THE CAP: a share above this body's own run speed, whatever chain of slides and hops reached for more. A
            // speed pad's boost is in `speed` and is not cut by it.
            float cap = Mathf.Min(MovementRework.MaxSpeed, Mathf.Max(speed, run * MovementRework.MaxSpeedScale));

            // How well the strafe keys are in time with the view this step, 0 to 1: a strafe key held on the side the
            // view is turning toward, and the view turning steadily (between `StrafeTurnMin` and `StrafeTurnMax`
            // degrees a second, best in the middle band).
            float yaw = transform.eulerAngles.y;
            float yawRate = dt > 0f ? Mathf.DeltaAngle(_rwYawPrev, yaw) / dt : 0f;
            _rwYawPrev = yaw;
            float strafe = Intent.MoveAxis.x;
            float strafeSync = 0f;
            if (Mathf.Abs(strafe) > 0.3f && strafe * yawRate > 0f)
            {
                float rate = Mathf.Abs(yawRate);
                strafeSync = Mathf.InverseLerp(MovementRework.StrafeTurnMin, MovementRework.StrafeTurnMin * 3f, rate)
                             * (1f - Mathf.InverseLerp(MovementRework.StrafeTurnMax * 0.6f, MovementRework.StrafeTurnMax, rate));
            }
            // ⚠️⚠️ A HOP CHAIN HAS TO BE PLAYED, AND IT IS PAID FOR (owner, 2026-10-09: "bhopping is too easy and gives an unfair
            // advantage to the attackers. right now all you have to do is hold jump then smoothly glide your mouse. and
            // even while bhopping, you can recharge your stamina even as you jump"). Three things, with the jump no
            // longer repeating while held (`MovementRework.AutoHop`) and its press window short:
            //   1. THE STRAFE EARNS SPEED ONLY WITH THE FORWARD KEY UP, and ONLY ON THE OTHER SIDE FROM THE HOP BEFORE.
            //      One long glide of the mouse to one side earns on one hop and never again; a chain is left, right,
            //      left, each with its own key and its own turn of the view, as the real technique is.
            //   2. SPEED ABOVE A WALK IN THE AIR IS SPRINTING, AND COSTS WHAT SPRINTING COSTS. It drains the stamina bar
            //      at the sprint's own rate (which also stops the bar refilling), and a body that cannot pay comes
            //      down to its walk. So a chain buys no more distance than the same bar spent running.
            //   3. THE CAP IS LOWER (`MaxSpeedScale`), so an attacker's best chain stays well under the taya's run.
            // ⚠️ THE SIDE IS HANDED ON AT EVERY LANDING, THE HOPPING ONE TOO (owner, 2026-10-09: "it seems like the air
            // strafing checks the first strafe of the player, if its right, continuously gliding left loses you momentum
            // but continuously gliding right gains you momentum"). It was handed on only on a step spent standing on
            // the ground, and with jump held there is none: the body lands and jumps in the same step. So the first
            // side ever strafed stayed the earning side for the whole chain, which is the opposite of the rule.
            if (_grounded && _rwGainSide != 0) { _rwLastGainSide = _rwGainSide; _rwGainSide = 0; }
            bool onFloor = _grounded && !groundJump;
            if (onFloor)
            {
                _rwFloorTime += dt;
                // A rest on the ground ends the chain: the next hop may earn on either side.
                if (_rwFloorTime > MovementRework.ChainResetSeconds) _rwLastGainSide = 0;
            }
            else _rwFloorTime = 0f;
            // ⚠️ RETUNED THE SAME DAY (owner: "yeah now i cant bhop at all"). That cut stacked four costs and the chain
            // could not be held by anybody: (a) the forward key had to be up, which nobody holding W knew; (b) EVERY
            // turn of the travel out of time was charged against all speed above a WALK, and changing sides is a moment
            // out of time by its nature, so each change of side emptied the chain; (c) a tired hop took up to 0.6 of the
            // speed; (d) the stamina. Now: the forward key is free; a turn is charged only when it is a WEIRD one
            // (`weirdTurn`, below: steering with no strafe key, or a strafe key against a view plainly turning the
            // other way), never while the mouse is coming round between sides; and a tired hop takes less
            // (`JumpFatigueSpeedLoss`). Changing sides and the stamina stay: they are the skill and the price.
            bool weirdTurn = Mathf.Abs(strafe) <= 0.3f
                             || (strafe * yawRate < 0f && Mathf.Abs(yawRate) > MovementRework.StrafeTurnMin * 3f);
            float gainSync = 0f;
            // (In the air only: a strafe made walking on the ground is not part of any chain and must not use up a side.)
            if (strafeSync > 0f && !_grounded)
            {
                int side = strafe > 0f ? 1 : -1;
                if (_rwGainSide == 0 && side != _rwLastGainSide) _rwGainSide = side;
                // (The left-right-left rule is a switch, and off: owner, 2026-10-09, "lets try removing the forced
                // left-right left right". Off, any strafe in time with the view earns, on either side, hop after hop.)
                if (_rwGainSide == side || !MovementRework.StrafeMustAlternate) gainSync = strafeSync;
            }
            // ⚠️⚠️ THE HOP IS WHAT A BODY WITH NO STAMINA HAS LEFT (owner, 2026-10-09: "the point of the bhopping was for another
            // way players can move when the stamina is gone.. how can we adjust for that?"). So it no longer COSTS the bar
            // (the cut before this drained it at the sprint's rate, which made the hop useless exactly when it was
            // wanted). What keeps it fair instead:
            //   * IT IS SLOWER THAN A SPRINT. A strafe can build speed only up to `HopSpeedScale` of the body's run
            //     (`hopCap`): quicker than the walk a spent body is left with, never as quick as running on a full bar.
            //   * IT DOES NOT REST YOU. The bar does not refill in the air, nor for `HopRecoveryDelay` after a hop
            //     (`ReworkBlocksRecovery`, read where the bar's recovery is gated). To get the sprint back, stop hopping.
            float hopCap = Mathf.Max(speed, run * MovementRework.HopSpeedScale);
            if (!_grounded || groundJump || coyoteJump) _rwRecoveryBlockedUntil = Time.time + MovementRework.HopRecoveryDelay;

            bool held = Intent.Crouch;
            // ⚠️ CROUCH HELD THROUGH A LEAP IS A SLIDE OUT OF IT (owner, 2026-10-07: "im still unable to slide while leaping").
            // A slide starts on the PRESS, and a press made during Paete's leap found a body with no speed of its own
            // (the vines were carrying it) and was spent. For a moment after the leap hands its speed over
            // (`StepCarry`), crouch being DOWN counts as the press.
            bool pressed = held && (!_rwCrouchHeldPrev || Time.time < _rwLeapHandedOverUntil);
            _rwCrouchHeldPrev = held;
            _rwSlideCooldown = Mathf.Max(0f, _rwSlideCooldown - dt);

            // A slide starts on the press, on the ground, at a sprint.
            if (pressed && _grounded && !groundJump && !_rwSliding && flat >= run * MovementRework.SlideEntryScale)
            {
                _rwSliding = true; _rwSlideLeft = MovementRework.SlideMaxSeconds;
                if (_rwSlideCooldown <= 0f)
                {
                    v = v.normalized * Mathf.Min(cap, Mathf.Max(flat, run * MovementRework.SlideBoostScale));
                    _rwSlideCooldown = MovementRework.SlideBoostCooldown;
                    GetComponentInChildren<Visual.CharacterSquashStretch>()?.Squash(0.18f);
                }
                flat = v.magnitude;
            }
            SetReworkCrouch(held || _rwSliding);

            bool onGround = _grounded && !groundJump;
            if (_rwSliding)
            {
                _rwSlideLeft -= dt;
                if (onGround)
                {
                    // ⚠️ THE GROUND TAKES THE SLIDE, NOT A CLOCK (owner, 2026-10-07, of a slide that lost a share of its
                    // speed a second and then stopped when its time ran out: "too linear, doesnt slowly slow down,
                    // only stops when the slide time is done ... it doesnt feel like theres friction to it"). A real
                    // drag in metres a second each second, light as it starts and biting harder the longer it runs,
                    // so it glides, then drags, then is down to a crouch walk and is over.
                    float age = MovementRework.SlideMaxSeconds - _rwSlideLeft;
                    float drag = Mathf.Lerp(MovementRework.SlideDragStart, MovementRework.SlideDragEnd,
                                            Mathf.Clamp01(age / Mathf.Max(0.01f, MovementRework.SlideDragRamp)));
                    float now = v.magnitude;
                    if (now > 1e-4f) v *= Mathf.Max(0f, now - drag * dt) / now;
                    flat = v.magnitude;
                    v = Accelerate(v, w, v.magnitude, MovementRework.SlideSteer, dt);
                    // Steering turns a slide; it does not speed it up.
                    if (v.magnitude > flat) v = v.normalized * flat;
                }
                bool slow = v.magnitude < Mathf.Max(0.5f, speed * MovementRework.CrouchSpeedScale * 1.15f);
                // Into a jump the slide ends and its speed goes with the body.
                if (!held || slow || _rwSlideLeft <= 0f || groundJump || !_grounded) _rwSliding = false;
            }
            else if (onGround)
            {
                float wishSpeed = speed * (_rwCrouched ? MovementRework.CrouchSpeedScale : 1f);
                float friction = IsOnIce ? MovementRework.IceFriction : MovementRework.GroundFriction;
                float accel = IsOnIce ? MovementRework.IceAccel : MovementRework.GroundAccel;
                if (flat > 1e-4f)
                {
                    float drop = Mathf.Max(flat, MovementRework.StopSpeed) * friction * dt;
                    v *= Mathf.Max(0f, flat - drop) / flat;
                }
                v = Accelerate(v, w, wishSpeed, accel, dt);
            }
            else
            {
                // ⚠️ IN THE AIR THE KEYS STEER, AND SPEED IS NOT FREE (owner, 2026-10-07, of the first cut, a Quake strafe:
                // "spinning in a circle just ramps up ur speed too easily, airstrafing barely moves your character - but
                // it does increase speed. i need to actually be able to move directions when airstrafing"). So the wish
                // pushes the velocity round at `AirAccel`, and the speed it comes out with is held to what it went in
                // with (or the ordinary move speed, for a standing jump), plus a small gain for steering across the
                // travel, never past the cap. No keys: the velocity is kept as it is.
                if (w != Vector2.zero)
                {
                    Vector2 heading = flat > 1e-3f ? v / flat : w;
                    float across = 1f - Mathf.Abs(Vector2.Dot(heading, w));
                    v += w * (MovementRework.AirAccel * dt);
                    // ⚠️ SPEED IS EARNED BY A STRAFE IN TIME WITH THE VIEW, AND ONLY BY THAT (owner, 2026-10-07, of a cut with no
                    // gain at all: "wait, now i cant gain any momentum.."; and of the one before, where any turn kept
                    // it: "even a weird turn can still keep it"). `strafeSync` is 1 while a strafe key is held on the
                    // side the view is turning toward, at a steady turn (not a flick, not a standstill): then the
                    // travel follows the view for free and gains `AirStrafeGain`. Any other turn of the travel is
                    // paid for out of the extra speed, below.
                    //
                    // ⚠️ AND THE FORWARD KEY ADDS NOTHING IN THE AIR (owner, 2026-10-07: "im still able to spam jump and
                    // move but at walking speed, even though im not air strafing"). The ceiling here was the body's
                    // ordinary move speed, so holding forward through a chain of tired hops topped the speed back up
                    // in the air after every landing had cut it, and jump fatigue cost nothing. The ceiling is now the
                    // speed the body HAS, with only `AirBaseSpeed` for a jump from a standstill (as Source gives a
                    // standing jump a crawl and no more).
                    // That standstill allowance is for RESTED legs: it shrinks with the jump debt, or mashed hops settle at
                    // it and never get slower (owner: "still reaching a minimum jump fatigue speed of only 1.2m/s").
                    float airBase = MovementRework.AirBaseSpeed * (1f - _rwJumpFatigue);
                    float allowed = Mathf.Min(Mathf.Max(flat, airBase) + MovementRework.AirStrafeGain * gainSync * dt, Mathf.Max(hopCap, flat));
                    float mag = Mathf.Min(Mathf.Max(v.magnitude, flat + MovementRework.AirStrafeGain * gainSync * dt), allowed);
                    // Whatever the body carries above its ordinary move speed is lost in proportion to how far this
                    // step turned its travel out of time: a straight line keeps it all, a sharp turn nearly none.
                    if (flat > 1e-3f && mag > 1e-3f && strafeSync < 1f && weirdTurn)
                    {
                        float turned = Vector2.Angle(heading, v) * Mathf.Deg2Rad;
                        mag -= Mathf.Max(0f, mag - speed) * Mathf.Clamp01(MovementRework.AirTurnLoss * turned * (1f - strafeSync));
                    }
                    v = mag > 1e-4f ? v.normalized * mag : Vector2.zero;
                }
            }

            // ⚠️ JUMP FATIGUE (owner, 2026-10-07: "there should be jump fatigue ... its the reason why airstrafe movement is a
            // skill and the only way to keep ur momentum (or gain) while bhopping"). Every jump tires the legs, and
            // only time on the ground rests them. A jump taken tired is lower and takes a share of the body's speed,
            // so hopping on and on bleeds it away; the speed a strafe in time with the view earns in the air is what
            // pays that back.
            //
            // ⚠️ MODELLED ON COUNTER-STRIKE'S STAMINA (owner, same day, of a first version that cost a flat quarter at
            // most: "jump fatigue also affects horizontal speed.. at some point you're barely moving off the ground if
            // you keep spamming jump without strafing. need you to look into source style movement or valorant").
            // There a jump adds to a stamina debt that drains away with time; the debt scales the next jump's launch
            // down and cuts the speed a body lands with. So here: the debt eases off ALL the time, in the air too, by
            // a share of itself (a long jump rests more than a short one), every jump adds to it, and a tired jump is
            // lower and slower by the debt. A lower jump is a shorter rest, so jumps mashed one after another sink
            // into small slow hops; the same chain with a strafe in time earns back in the air what each landing takes.
            _rwJumpFatigue *= Mathf.Exp(-dt / Mathf.Max(0.05f, MovementRework.JumpFatigueRecoverSeconds));
            if (_rwJumpFatigue < 0.005f) _rwJumpFatigue = 0f;
            if (groundJump || coyoteJump)
            {
                float tired = _rwJumpFatigue;
                v *= 1f - MovementRework.JumpFatigueSpeedLoss * tired;
                float scale = 1f - MovementRework.JumpFatigueHeightLoss * tired;
                if (coyoteJump) _velocity.y *= scale; else _rwJumpScale = scale;
                _rwJumpFatigue = Mathf.Min(1f, tired + MovementRework.JumpFatiguePerJump);
                // And the smaller flat share every hop takes of the speed above the ordinary (`HopLoss`).
                float mag = v.magnitude;
                if (mag > speed) v = v.normalized * (speed + (mag - speed) * (1f - MovementRework.HopLoss));
            }

            if (v.magnitude > cap) v = v.normalized * cap;
            _velocity.x = v.x; _velocity.z = v.y;
            return true;
        }

        /// <summary>Speed toward `wishSpeed` along `wish`, never past it: the part of the velocity along the wish is what counts.</summary>
        private static Vector2 Accelerate(Vector2 velocity, Vector2 wish, float wishSpeed, float accel, float dt)
        {
            if (wish == Vector2.zero) return velocity;
            float add = wishSpeed - Vector2.Dot(velocity, wish);
            if (add <= 0f) return velocity;
            return velocity + wish * Mathf.Min(accel * wishSpeed * dt, add);
        }

        /// <summary>Called where the body cannot steer (a stun, a root): no crouch, no slide, nothing queued.</summary>
        private void StepReworkIdle()
        {
            _rwJumpQueued = false; _rwJumpScale = 1f; _rwJumpBuffer = 0f; _rwSliding = false; _rwCrouchHeldPrev = false;
            StandUpFromRework(force: true);
        }

        private void SetReworkCrouch(bool want)
        {
            if (want == _rwCrouched) return;
            if (!want) { StandUpFromRework(force: false); return; }
            if (!_rwCapsuleKept) { _rwStandHeight = _cc.height; _rwStandCentre = _cc.center; _rwCapsuleKept = true; }
            // Shortened from the top: the feet stay where they are.
            float height = Mathf.Min(_rwStandHeight, Mathf.Max(MovementRework.CrouchHeight, _cc.radius * 2f));
            float feet = _rwStandCentre.y - _rwStandHeight * 0.5f;
            _cc.height = height;
            _cc.center = new Vector3(_rwStandCentre.x, feet + height * 0.5f, _rwStandCentre.z);
            _rwCrouched = true;
        }

        private void StandUpFromRework(bool force)
        {
            if (!_rwCrouched) return;
            if (!force && !HeadroomToStand()) return;
            _cc.height = _rwStandHeight; _cc.center = _rwStandCentre;
            _rwCrouched = false; _rwSliding = false;
        }

        /// <summary>Whether the standing capsule's upper part is clear of the world (other bodies do not count).</summary>
        private bool HeadroomToStand()
        {
            float radius = _cc.radius * 0.95f;
            Vector3 feet = transform.position + Vector3.up * (_rwStandCentre.y - _rwStandHeight * 0.5f);
            Vector3 low = feet + Vector3.up * (_cc.height - radius);
            Vector3 high = feet + Vector3.up * (_rwStandHeight - radius);
            int count = Physics.OverlapCapsuleNonAlloc(low, high, radius, RwOverlap, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var other = RwOverlap[i];
                if (other == null || other.transform.IsChildOf(transform)) continue;
                if (other.GetComponentInParent<CharacterMotor>() != null) continue;
                return false;
            }
            return true;
        }
    }
}
