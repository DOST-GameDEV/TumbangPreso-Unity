using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// Movement, stamina and confinement for every unit, human or bot.
    ///
    /// ⚠️⚠️ THIS IS THE HIGHEST-RISK FILE IN THE PORT AND THE REASON PHASE 3 EXISTS.
    /// Godot's `CharacterBody3D.move_and_slide()` and Unity's `CharacterController.Move()`
    /// do not resolve collisions the same way, so movement FEEL does not survive the port
    /// for free. It has to be measured against the Godot build on one player, in an empty
    /// scene, before any netcode is built on top of it: the alternative is discovering it
    /// in Phase 6 and re-tuning with the whole game already standing on it.
    ///
    /// The model is kept as close to the original as the two engines allow: an explicit
    /// `velocity` field, gravity integrated by hand, and one Move per physics step. Godot
    /// works the same way, which is why this is the closest available mapping rather than
    /// a rewrite around Rigidbody forces.
    /// </summary>
    /// <remarks>
    /// ⚠️ -100 SO THE CAPSULE HAS ALREADY MOVED WHEN ANYTHING ELSE RUNS ITS FixedUpdate. The
    /// carry in <see cref="Carrier.FixedUpdate"/> is the reader that needs it: a held tsinelas
    /// placed on the hand BEFORE the body moves is a step of walking behind it for the rest of
    /// the frame. See the execution-order note on `Carrier` for the full ordering.
    ///
    /// ⚠️ THE INTENT SNAPSHOT IS UNAFFECTED. `CommitFrame` still runs at the end of this step
    /// and every consumer of a press EDGE runs in Update, which is a later phase entirely — so
    /// moving this earlier within FixedUpdate cannot shorten the window those readers see.
    /// </remarks>
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(CharacterController))]
    public sealed class CharacterMotor : MonoBehaviour
    {
        [Header("Role")]
        [SerializeField] private bool _isDefender;
        [SerializeField] private int _playerSlot;
        [SerializeField] private int _characterIndex = -1;

        /// ⚠️ KEPT ONLY SO CharacterVisual AND CharacterNameplate KEEP THEIR SIGNATURES.
        /// Every unit is a Person now; nothing sets these to anything else. They are the
        /// last two lines of the objects-are-players thesis (`character_base.gd:386`).
        /// The spectator's POV eye height reads IsPerson, so it cannot be folded away
        /// without giving SpectatorCamera another source for the same question.
        [SerializeField] private bool _isPerson = true;
        [SerializeField] private bool _isCan;

        /// ⚠️ RENAMED FROM `team` IN THE ORIGINAL. There are no teams — this is the
        /// player's seat, the index into MatchManager.scores, and the rotation position
        /// that decides when they defend. A spectator legend that says "TEAM A" is
        /// describing a deleted game (`spectator_camera.gd:416`).
        [SerializeField] private string _playerName = "";
        [SerializeField] private bool _isBot;

        public bool IsDefender { get => _isDefender; set => _isDefender = value; }
        public int PlayerSlot { get => _playerSlot; set => _playerSlot = value; }
        public int CharacterIndex { get => _characterIndex; set => _characterIndex = value; }
        public bool IsAttacker => !_isDefender;
        public bool IsPerson { get => _isPerson; set => _isPerson = value; }
        public bool IsCan { get => _isCan; set => _isCan = value; }
        public bool IsBot { get => _isBot; set => _isBot = value; }

        /// <summary>Empty is a real value: it means "never set one", and every reader
        /// falls back to <see cref="DisplayName"/> rather than printing a blank row.</summary>
        public string PlayerName { get => _playerName; set => _playerName = value; }

        /// <summary>
        /// What to draw over this unit, from `character_base.gd:496`.
        ///
        /// ⚠️ IT IS A DISPLAY TRANSFORM, NOT A WRITE. PlayerName keeps the case the player
        /// typed, so the settings field still shows them their own name as they entered it.
        /// Case is cosmetic and length is structural, which is why the length limit lives
        /// on the data (GameSettings.SanitiseName) and the casing lives here.
        /// </summary>
        public string DisplayName()
        {
            if (_isBot) return CharacterName().ToUpperInvariant();
            if (_playerName != "") return _playerName.ToUpperInvariant();
            return $"P{_playerSlot + 1}";
        }

        /// <summary>Active game mode for trait lookups and ability kits.</summary>
        public GameMode Mode { get; set; } = GameMode.HeroStrike;

        private Abilities.HeroAbilitySystem _abilitySystem;
        public Abilities.HeroAbilitySystem AbilitySystem =>
            _abilitySystem != null ? _abilitySystem : (_abilitySystem = GetComponent<Abilities.HeroAbilitySystem>());

        /// <summary>The roster pick's name, falling back to the seat number.
        /// CharacterIndex is -1 until a pick arrives.</summary>
        private string CharacterName()
        {
            // ⚠️ THE CORE ROSTER, NOT RosterBook. RosterBook maps an index to a model; the
            // NAME is balance-layer data and lives in the engine-free package, so a headless
            // test can assert a legend without loading a single asset.
            var list = Roster.GetPeople(Mode);
            if (_characterIndex < 0 || _characterIndex >= list.Count)
                return $"P{_playerSlot + 1}";
            return list[_characterIndex].Name;
        }

        public InputIntent Intent { get; } = new InputIntent();
        public Stamina Stamina { get; private set; }

        public Vector3 Velocity => _velocity;
        public bool IsGrounded => _grounded;

        /// <summary>Set false while the round is not live. Confinement and most verbs read it.</summary>
        public bool RoundActive { get; set; } = true;

        private CharacterController _cc;
        private Vector3 _velocity;
        private bool _grounded;
        private float _fallSpeed;
        private int _spawnSettle;
        private Vector3 _spawnSettleAt;

        /// <summary>The push a shove, a block or a tag applied, decaying against Friction.
        /// Kept separate from walk velocity so a knockback cannot be walked out of.</summary>
        private Vector3 _externalVelocity;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            Stamina = new Stamina();
        }

        /// ⚠️ THE SPECTATABLE REGISTRY IS POPULATED HERE, NOT AT THE SPAWN SITE. Godot's
        /// `main.gd` added each unit to the `spectatable` group as it spawned it; that file
        /// is 3,595 lines and unported, and hanging the spectator's follow cycle on it would
        /// mean Tab does nothing until the very last phase of the port. A unit registering
        /// itself is also the stricter version of the same rule: a unit that exists is
        /// followable, with no second place to remember.
        ///
        /// Registration is not a gameplay write and the spectator never writes back, so this
        /// does not weaken the "spectator touches nothing" contract.
        private void OnEnable() => CameraSystem.SpectatorCamera.Register(this);

        private void OnDisable() => CameraSystem.SpectatorCamera.Unregister(this);

        /// <summary>
        /// ⚠️ DO NOT REMOVE ON THE ASSUMPTION UNITY DOES NOT NEED THIS.
        /// In Godot, writing `position` updates the scene tree immediately and the physics
        /// broadphase only at the next step, so when roles rotate and two players trade
        /// marks, each stands on the other's stale collider for one frame. Measured there:
        /// three contacts with normal (0,1,0), a 1.60 shove upward, then 9.89 units into a
        /// wall on the next frame. That was expensively diagnosed and role rotation is
        /// exactly what triggers it.
        ///
        /// Unity has the same shape of problem: teleporting a CharacterController does not
        /// re-run the broadphase until the next physics step, and `Physics.SyncTransforms`
        /// only syncs transforms, it does not resolve the overlap. Three frames is 50 ms
        /// and is invisible. Let a probe decide whether it can go, not an assumption.
        /// </summary>
        public void BeginSpawnSettle()
        {
            _spawnSettle = Balance.SpawnSettleFrames;
            _spawnSettleAt = transform.position;
            _velocity = Vector3.zero;
            _externalVelocity = Vector3.zero;
        }

        /// <summary>
        /// Stop dead. `main.gd::_on_match_won_freeze_physics` runs this over every character the
        /// moment the match is won.
        ///
        /// ⚠️ THE INTENT IS PARKED TOO, NOT JUST THE VELOCITY. Zeroing the velocity alone leaves
        /// whatever was held still held, so a player who was sprinting when the last point landed
        /// is moving again on the next physics step and the freeze lasts one frame. `Parked` is
        /// the flag `InputIntent` already has for exactly this, and it covers a bot as well as a
        /// human because both go through the same table.
        /// </summary>
        public void FreezeForMatchEnd()
        {
            _velocity = Vector3.zero;
            _externalVelocity = Vector3.zero;
            Intent.Clear();
            Intent.CommitFrame();
            Intent.Parked = true;
        }

        public void Teleport(Vector3 position)
        {
            // ⚠️⚠️ THE ARENA WALL IS ENFORCED HERE TOO, AND THIS IS THE PATH THAT ACTUALLY
            // BROKE IT. `Confine` holds a body that WALKS or is PUSHED at the edge, and a
            // teleport skips the whole movement step, so a caller handing this an arbitrary
            // point put a player outside the world with nothing to pull them back. Nemu owns
            // both such callers: PHANTOM PHASE ends by blinking to wherever the projected
            // ghost drifted to, and the pet's `EndPossession` teleports Nemu onto the pet.
            // Neither destination is bounded by anything.
            //
            // ⚠️ MEASURED 2026-08-23, and it read as an AI fault rather than an ability one.
            // A whole Hero Strike match reported a seat 45.8 m out on X against a half width of
            // 8.6, holding its tsinelas the entire way; it then threw from out there and spent
            // the rest of the round unable to fetch, because a bot clamps its GOAL to the
            // playable rectangle and so cannot follow itself out. Clamping at the one function
            // every teleport already goes through fixes both callers and every future one.
            //
            // ⚠️ THE SPAWN MARKS AND THE TAG SAFE ZONE ARE ALL WELL INSIDE THIS, so nothing that
            // was already correct moves by a millimetre.
            position.x = Mathf.Clamp(position.x, -AIController.PlayableHalfX, AIController.PlayableHalfX);
            position.z = Mathf.Clamp(position.z, -AIController.PlayableHalfZ, AIController.PlayableHalfZ);

            _cc.enabled = false;      // CharacterController fights direct transform writes
            transform.position = position;
            _cc.enabled = true;
            BeginSpawnSettle();
        }

        /// <summary>Where this unit returns to when it falls off the world. Written at
        /// spawn; the kill plane is the only thing that reads it today.</summary>
        public Vector3 SpawnPosition { get; set; }

        // -------------------------------------------------------------------
        // SPEED ZONES — hazard slows, from character_base.gd:1556.
        //
        // ⚠️ ONE STACK, IN Stamina. Fatigue already rides `Stamina.SpeedZones`, and the
        // movement step already reads it. A second list here would be two copies of the same
        // state and the copy nobody reads is the one that drifts.
        // -------------------------------------------------------------------

        public void EnterSpeedZone(float multiplier) => Stamina.SpeedZones.Enter(multiplier);

        public void ExitSpeedZone(float multiplier) => Stamina.SpeedZones.Exit(multiplier);

        /// <summary>The slow currently applied to this unit, 1.0 when clear.</summary>
        public float SpeedMultiplier => Stamina.SpeedZones.Value;

        /// <summary>
        /// Put this unit back on its own spawn with no velocity, from
        /// `character_base.gd:1939`.
        ///
        /// ⚠️ NOT DESPAWNED AND NOT DAMAGED. The GDD's rule is stun-only, no permanent
        /// elimination, so falling off the map costs position and nothing else. Anything
        /// that makes this destructive is changing the design, not fixing a bug.
        /// </summary>
        public void Respawn()
        {
            Teleport(SpawnPosition);
            // Godot reached the autoload directly (`AudioManager.play_at`). GameServices is
            // this port's stand-in for the nine autoloads, and it is null in a bare test
            // scene, so the call is guarded rather than assumed.
            GameServices.Audio?.PlayAt("respawn", transform.position);
        }

        /// <summary>
        /// The move axis as a world direction.
        ///
        /// ⚠️⚠️ MOUSE-AIMED MOVEMENT IS RELATIVE TO THE BODY AND THIS WAS THE "controls are
        /// inverted and most dont work" REPORT IN FULL. `character_base.gd:912` reads
        ///
        ///     direction = transform.basis * Vector3(input.x, 0, input.y)
        ///
        /// for a mouse-aimed unit, and a bare world-space `Vector3(x, 0, y)` only for one that
        /// steers by movement. This file had the world-space form for BOTH, so W walked the
        /// player toward world +Z no matter which way they were facing: pointing south made W
        /// reverse, pointing east made it strafe, and only one of the four cardinal headings
        /// behaved. Nothing about it reads as a movement bug from inside the game.
        ///
        /// ⚠️ AND A MOVEMENT-AIMED UNIT TURNS TO FACE ITS DIRECTION, which is `look_at` on the
        /// same line. Every bot steers this way, and without it a bot slides sideways while its
        /// punch, its lunge and its shove all fire along a forward vector that never moved. The
        /// three verbs all derive their direction from the body (`-basis.z` in the .gd), so this
        /// is combat correctness rather than an animation nicety.
        /// </summary>
        private Vector3 Steer(Vector2 axis)
        {
            Vector3 wish = new Vector3(axis.x, 0.0f, axis.y);
            if (wish.sqrMagnitude > 1.0f) wish.Normalize();

            if (wish.sqrMagnitude < 0.0001f) return Vector3.zero;

            if (MouseAimed)
            {
                wish = transform.TransformDirection(wish);
                wish.y = 0.0f;
                return wish.normalized;
            }

            wish = wish.normalized;
            transform.rotation = Quaternion.LookRotation(wish, Vector3.up);
            return wish;
        }

        /// <summary>
        /// True when a local player is steering this unit with the mouse, from
        /// `character_base.gd::_is_mouse_aimed()`.
        ///
        /// ⚠️ ASKED OF THE RIG, NOT STORED HERE. One flag on the motor would have to be kept in
        /// step with every camera handover: spectating, the debug player switcher, and a peer
        /// that stops being the authority for a body. The rig is the thing that actually knows.
        /// </summary>
        private bool MouseAimed
        {
            get
            {
                if (_rig == null || !_rig.IsFollowing(this)) return false;
                return _rig.Aim == CameraSystem.AimSource.Mouse;
            }
        }

        private CameraSystem.CameraRig _rig;

        /// <summary>
        /// ⚠️ RE-RESOLVED WHILE IT IS NULL, NOT ONCE IN Awake. `MatchInstaller` builds the rig
        /// after the seats, so a unit that cached the answer at Awake would cache "no rig" and
        /// the human seat would steer like a bot for the whole match.
        /// </summary>
        private void ResolveRig()
        {
            if (_rig != null) return;
            _rig = FindFirstObjectByType<CameraSystem.CameraRig>();
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            ResolveRig();

            if (_spawnSettle > 0)
            {
                _spawnSettle--;
                transform.position = _spawnSettleAt;
                return;
            }

            Stamina.StepFatigue(dt);

            // ⚠️⚠️ A STUNNED UNIT DOES NOT STEER, AND ITS ABSENCE WAS THE REPORTED
            // *"i can still move while stunned"*. `character_base.gd:932` is explicit:
            //
            //     if state != State.NORMAL:
            //         velocity.x = move_toward(velocity.x, 0, FRICTION * delta)
            //         velocity.z = move_toward(velocity.z, 0, FRICTION * delta)
            //         _move_and_confine()
            //         return
            //
            // and this file had no equivalent gate at all, so `Intent.MoveAxis` was read and
            // `_velocity` written on every frame of a stun. A 5 s tag penalty that the victim
            // can simply walk out of is not a penalty, and it is the half of the stun the HUD
            // was already counting down for them.
            //
            // ⚠️ THE DECAY IS `Friction`, NOT AN INSTANT ZERO. The walk velocity a shove
            // interrupted has to bleed off at the same rate every other impulse in the game
            // does, or a stunned unit stops dead in the air and the knockback reads as a wall.
            //
            // ⚠️ AND GRAVITY, THE EXTERNAL IMPULSE AND THE CONFINEMENT ALL STILL RUN BELOW.
            // Being stunned stops you ACTING; it does not exempt you from the world. A shove
            // that could not push a stunned body is a shove that cannot combo into a tag,
            // which is the interaction `IsTaggable`'s own header exists to protect.
            bool canSteer = CanAct();

            Vector2 axis = canSteer ? Intent.MoveAxis : Vector2.zero;
            bool moving = axis.sqrMagnitude > 0.0001f;

            // The sprint multiplier. Fatigue is NOT in this value: it rides the speed-zone
            // stack so it composes with a hazard zone rather than one silently winning.
            float sprint = Stamina.Step(dt, moving, canSteer && Intent.Pressed(Verb.Sprint));

            // ⚠️⚠️ THE FATIGUE CUE, WHICH SHIPPED REGISTERED AND WAS NEVER FIRED ONCE.
            // `character_base.gd::_enter_fatigue` plays it on the frame the bar bottoms out, and
            // that moment is worth a sound for the same reason the HUD row exists: emptying the
            // bar costs a 0.75 speed lockout the player did not choose and cannot see coming.
            //
            // ⚠️ THE EDGE IS DETECTED HERE BECAUSE `Stamina` CANNOT MAKE A SOUND. It lives in the
            // engine-free core package (rule 3), so it has no way to reach the audio director and
            // must not acquire one. The motor is the nearest thing that has both the state and a
            // UnityEngine reference, which is exactly the split the package boundary is for.
            //
            // ⚠️ AN EDGE, NOT A STATE. Fatigue lasts seconds; playing on the state would retrigger
            // every frame, which is the buzzsaw case `AudioCues.HeadroomDb` exists to keep out.
            bool fatigued = Stamina.IsFatigued;

            if (fatigued && !_wasFatigued)
                GameServices.Audio?.PlayAt("stamina_empty", transform.position);

            _wasFatigued = fatigued;

            float speed = Balance.Speed
                          * Stamina.RoleSpeedScale(_isDefender)
                          * Roster.PersonSpeedScale(_characterIndex, Mode)
                          * sprint
                          * Stamina.SpeedZones.Value;

            if (canSteer)
            {
                Vector3 wish = Steer(axis);

                _velocity.x = wish.x * speed;
                _velocity.z = wish.z * speed;
            }
            else
            {
                _velocity.x = Mathf.MoveTowards(_velocity.x, 0.0f, Balance.Friction * dt);
                _velocity.z = Mathf.MoveTowards(_velocity.z, 0.0f, Balance.Friction * dt);
            }

            // ⚠️ EXTERNAL IMPULSES DECAY AGAINST Friction, WHICH IS WHAT MAKES v²/60 TRUE.
            // Every published knockback distance in the game is that solve, so this
            // deceleration is not a feel parameter: changing it invalidates SHOVE_SPEED,
            // LUNGE_SPEED and BLOCK_KNOCKBACK_SPEED all at once.
            if (_externalVelocity.sqrMagnitude > 0.0001f)
            {
                float mag = _externalVelocity.magnitude;
                mag = Mathf.Max(0.0f, mag - Balance.Friction * dt);
                _externalVelocity = mag <= 0.0f ? Vector3.zero : _externalVelocity.normalized * mag;
            }

            ApplyGravity(dt);

            Vector3 total = _velocity + _externalVelocity;
            CollisionFlags flags = _cc.Move(total * dt);

            // ⚠️ `isGrounded` ALONE IS NOT TRUSTWORTHY. It reflects only the last Move and
            // goes false on slopes, on steps and on the frame an impulse lifts the capsule.
            // The collision flag is what the controller actually resolved this step.
            bool wasAirborne = !_grounded;
            _grounded = (flags & CollisionFlags.Below) != 0 || _cc.isGrounded;

            // ⚠️ THE LANDING SOUND HAS A SPEED FLOOR. Below LandSfxMinSpeed a landing is
            // silent, or a unit stepping off a kerb thumps like one that fell off a roof —
            // and on uneven ground the grounded flag flickers, so every step would thud.
            if (_grounded && wasAirborne && _fallSpeed > Balance.LandSfxMinSpeed)
            {
                float weight = Mathf.InverseLerp(Balance.LandSfxMinSpeed,
                                                 Balance.MaxFallSpeed, _fallSpeed);
                GameServices.Audio?.PlayAtVaried("land", transform.position,
                                                 0.86f, 1.04f,
                                                 Mathf.Lerp(0.65f, 1.0f, weight));
                GetComponentInChildren<Visual.CharacterSquashStretch>()?
                    .Squash(Mathf.Lerp(0.12f, 0.30f, weight));
            }

            // Tracked on the way down, because by the time the capsule is grounded the
            // vertical velocity has already been zeroed.
            _fallSpeed = _grounded ? 0.0f : Mathf.Max(_fallSpeed, -_velocity.y);

            ShedCharacterPerch();
            Confine();

            // ⚠⚠ THE MASH IS READ HERE, BEFORE `CommitFrame`, AND IT IS THE JUMP KEY ON
            // PURPOSE. Jump is the one verb that is meaningless while a body is face down on the
            // tarmac, so nothing is taken away by giving it a second job in that state, and
            // "hammer the jump key to get up" needs no teaching. It follows the pattern `Grab`
            // already uses: one control, one action, resolved by context. No new binding is
            // added, so `InputMapAndAbilityTests`' one-control-one-action rule is untouched.
            if (_tripLeft > 0.0f && Intent.JustPressed(Verb.Jump)) MashRecover();

            // ⚠️⚠️ THE INTENT SNAPSHOT IS TAKEN HERE, AT THE END OF THE AUTHORITATIVE STEP, AND
            // NOWHERE ELSE. `JustPressed` and `JustReleased` are a diff against it, so whoever
            // takes it decides which readers can still see a press edge. Both producers used to
            // take their own at the end of their Update, which meant the edge was gone before
            // this step ran: jump, the shove and the lunge all read as never pressed, for a bot
            // and for a human alike.
            //
            // Taken last, after every verb resolved in this step has been read, so an edge
            // written by an Update survives into the next physics step exactly once. Consumers
            // that still run in Update (`Carrier`, for the pickup) then see it until this line
            // runs, which is what stops a press being dropped entirely on a frame that happens to
            // carry no physics step at all.
            Intent.CommitFrame();
        }

        private void ApplyGravity(float dt)
        {
            if (_grounded && _velocity.y <= 0.0f)
            {
                // ⚠️ A SMALL CONSTANT DOWNWARD BIAS RATHER THAN ZERO. A CharacterController
                // resting at exactly 0 vertical velocity reports `isGrounded` false every
                // other frame, which reads as a unit that cannot jump reliably and cannot
                // be told apart from an input bug.
                _velocity.y = -2.0f;

                if (Intent.JustPressed(Verb.Jump) && CanAct())
                {
                    _velocity.y = Balance.JumpVelocity;
                    GameServices.Audio?.PlayAtVaried("jump", transform.position,
                                                     0.96f, 1.08f, 0.9f);
                    GetComponentInChildren<Visual.CharacterSquashStretch>()?.Stretch(0.20f);
                }
            }
            else
            {
                _velocity.y -= Balance.Gravity * dt;
                if (_velocity.y < -Balance.MaxFallSpeed) _velocity.y = -Balance.MaxFallSpeed;
            }
        }

        /// <summary>
        /// ⚠️⚠️ YOU CANNOT STAND ON SOMEBODY'S HEAD. Every unit collides with every other,
        /// so one capsule resting on another is a perfectly legal floor as far as the
        /// controller is concerned: grounded goes true in mid-air, gravity is never
        /// applied, and the player hovers with full walking control. This came from live
        /// play and is MORE likely here, with three attackers converging on one box.
        ///
        /// ⚠️ THE FIX IS A NUDGE, NOT A COLLISION-LAYER CHANGE. Turning character-vs-
        /// character collision off would take the BODY BLOCK with it, and the taya standing
        /// in the throwing lane is the whole defensive mechanic. So only a contact steep
        /// enough to BE a perch is answered.
        /// </summary>
        private void ShedCharacterPerch()
        {
            if (!_perchedThisStep) return;
            _perchedThisStep = false;

            Vector3 away = transform.position - _perchContact;
            away.y = 0.0f;
            if (away.sqrMagnitude < 0.0001f) away = transform.forward; // exactly stacked
            _externalVelocity += away.normalized * PerchShedSpeed;
        }

        private const float PerchNormalMin = 0.7f;
        private const float PerchShedSpeed = 2.5f;
        private bool _perchedThisStep;
        private Vector3 _perchContact;

        /// <summary>Last frame's fatigue state, so the cue fires on the edge. See FixedUpdate.</summary>
        private bool _wasFatigued;

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.normal.y < PerchNormalMin) return;
            if (hit.collider.GetComponent<CharacterMotor>() == null) return;

            _perchedThisStep = true;
            _perchContact = hit.point;
        }

        /// <summary>
        /// ⚠️⚠️ A SQUARE, NOT A CIRCLE, and X and Z are clamped INDEPENDENTLY. The chalk the
        /// map draws and this clamp are the same expression; if either becomes radial the
        /// throwing line and the chalk stop agreeing and nobody will be able to see why.
        ///
        /// ⚠️ ONLY THE DEFENDER IS CONFINED. Attackers move freely everywhere and the box
        /// is merely dangerous to them, never closed.
        /// </summary>
        private void Confine()
        {
            Vector3 p = transform.position;
            float x = p.x, z = p.z;

            if (Confinement.IsConfined(RoundActive, _isDefender))
                Confinement.ClampToBox(ref x, ref z);

            // ⚠️⚠️ AND NOBODY LEAVES THE ARENA AT ALL, ROLE OR NO ROLE. The chalk box above is a
            // RULE and applies to the taya only; this is the WALL and applies to everybody. The
            // port had the wall for the tsinelas and not for the people: `Slipper.BounceOffBounds`
            // has bounced off `PlayableHalfX/Z` since it was written, while a body could walk or
            // be launched straight through the same line into empty space.
            //
            // ⚠️ MEASURED, AND IT IS NOT A CORNER CASE. `AiDiagnosticProbe` on 2026-08-23 caught
            // seat 3 at z = 18.73 against a half depth of 13.0, eight seconds into a Hero Strike
            // round, still holding its tsinelas. It threw from out there, the slipper landed at
            // (62.7, 38.4), and the owner then spent the rest of the round in FETCH walking into
            // the edge of the world at a goal it clamps but a body it did not. The whole-match
            // probe reported that as 121 unretrieved-slipper penalties and a seat travelling
            // 2,359 m: the AI looked broken and was in fact the only thing behaving.
            //
            // ⚠️ HERO STRIKE IS WHERE IT SURFACES BUT IT IS NOT A HERO BUG. Its kits apply far
            // more knockback than Classic's do, so they find the missing wall first. A human
            // shoved off the same edge in Classic has always had the same hole to fall into.
            x = Mathf.Clamp(x, -AIController.PlayableHalfX, AIController.PlayableHalfX);
            z = Mathf.Clamp(z, -AIController.PlayableHalfZ, AIController.PlayableHalfZ);

            if (x == p.x && z == p.z) return;

            _cc.enabled = false;
            transform.position = new Vector3(x, p.y, z);
            _cc.enabled = true;

            // ⚠️ THE PUSH THAT REACHED THE WALL IS SPENT AT THE WALL. Without this the body is
            // clamped back every step while the impulse still points outwards, so a knockback
            // into the edge reads as being pinned there for its whole duration instead of
            // stopping against it. Only the component INTO the wall is removed: a knockback
            // along the edge still slides.
            if (x != p.x)
            {
                _externalVelocity.x = 0.0f;
                _velocity.x = 0.0f;
            }

            if (z != p.z)
            {
                _externalVelocity.z = 0.0f;
                _velocity.z = 0.0f;
            }
        }

        // -------------------------------------------------------------------
        // The queries every other system asks in. Each is ONE function on purpose:
        // the HUD and the rule that acts must never be able to disagree.
        // -------------------------------------------------------------------

        public bool CanAct() => RoundActive && !IsStunned;
        public bool IsStunned => _stunLeft > 0.0f;
        public bool HoldingSlipper { get; set; }

        public bool IsInsideBox() =>
            Confinement.IsInsideBox(transform.position.x, transform.position.z);

        /// <summary>
        /// The ENTIRE vulnerability rule, in one function. An Attacker inside the box is
        /// 100% safe until they pick a slipper up. The HUD's VULNERABLE row reads this same
        /// function, so the warning a player sees cannot disagree with the rule that tags
        /// them.
        ///
        /// ⚠️⚠️ IT ASKS `RoundActive`, NOT `CanAct()`, AND THAT ONE WORD IS A SHIPPED BUG FIX.
        /// 🧑 2026-08-06, on the Godot build: *"a player that has been sabotaged by a player
        /// cannot be tagged by the defender. when the attacker is in a frozen state, it cannot
        /// be tagged."*
        ///
        /// `CanAct()` is `RoundActive &amp;&amp; !IsStunned`. The second half is a rule about whether
        /// this player can DO something; being tagged is something done TO them, and the two
        /// are not the same question. Reading it here made a stunned attacker IMMUNE, which is
        /// exactly backwards: standing in the box, holding a slipper, unable to move is the most
        /// vulnerable a player is ever going to be.
        ///
        /// ⚠️ AND IT MADE THE SABOTAGE SCORE UNREACHABLE, which is the proof it was never
        /// intended. `MatchRules` pays `ScoreSabotage` (50) to whoever shoved the victim inside
        /// `SabotageWindow`, and only a connecting shove records that credit. But a shove
        /// staggers, so the very act that earns the credit put the victim into the state that
        /// made this function refuse the tag that would have paid it. Shove into tag into
        /// sabotage is a designed combo whose middle step could not happen: a whole scoring
        /// event dead behind one word, in this port exactly as in the original.
        ///
        /// ⚠️ IT DOES NOT OPEN A CHAIN-TAG, AND THE GUARD IS POSITIONAL RATHER THAN THIS ONE.
        /// The obvious worry is that a tag itself applies a 5 s stun, so allowing a stunned
        /// attacker to be tagged lets the taya re-lunge on a 1.5 s cooldown and cash the same
        /// victim twice. It cannot: the tag penalty teleports the victim to their safe spot, so
        /// `IsInsideBox()` below is already false for the whole stun. The victim is protected by
        /// where they ARE, which is the check that was doing the work all along.
        ///
        /// ⚠️ `RoundActive` IS KEPT, because it is the half of `CanAct()` that genuinely
        /// belongs. Nobody is taggable between rounds, and dropping it would let a lunge left
        /// over from the last frame of a round score into the intermission.
        /// </summary>
        public bool IsTaggable()
        {
            if (_isDefender || !RoundActive) return false;
            if (AbilitySystem != null && AbilitySystem.IsImmuneToTags) return false;
            if (!HoldingSlipper) return false;
            return IsInsideBox();
        }

        private float _stunLeft;
        private float _stunTotal;
        private float _tripLeft;
        private float _tripTotal;

        /// <summary>Seconds of stun left, so the HUD can print the number the player needs.</summary>
        public float StunLeft => _stunLeft;

        /// <summary>What the current stun started at, so a bar can draw a ratio rather than a
        /// raw number. Reset with the stun, never accumulated.</summary>
        public float StunTotal => _stunTotal;

        /// <summary>True while the character has tripped and is grounded on the floor.</summary>
        public bool IsTripped => _tripLeft > 0.0f;
        public float TripLeft => _tripLeft;
        public float TripTotal => _tripTotal;

        private float _lastMashTime = -99.0f;
        private int _mashPresses;

        /// <summary>Accepted presses in the current fall, so the HUD can show it filling.</summary>
        public int MashPresses => _mashPresses;

        /// <summary>
        /// True while the player should be told to mash.
        ///
        /// ⚠️ IT GOES FALSE AT THE FLOOR RATHER THAN AT THE END OF THE TRIP. Once
        /// `Balance.MinTripDown` is reached nothing further can be bought, and a prompt that
        /// keeps asking for presses it will not honour teaches the player that mashing does not
        /// work, which is the opposite of the intent.
        /// </summary>
        public bool CanMashUp => _tripLeft > Balance.MinTripDown;

        public void ClearStun()
        {
            _stunLeft = 0.0f;
            _stunTotal = 0.0f;
        }

        public void ClearTrip()
        {
            _tripLeft = 0.0f;
            _tripTotal = 0.0f;
            _mashPresses = 0;
        }

        /// <summary>
        /// Trips the character, making them tumble flat onto the ground for a duration (e.g. 2.5s)
        /// before rising back up.
        /// </summary>
        public void ApplyTrip(float duration = 2.5f)
        {
            if (AbilitySystem != null && AbilitySystem.IsImmuneToStuns) return;

            _tripLeft = Mathf.Max(_tripLeft, duration);
            _tripTotal = Mathf.Max(_tripTotal, _tripLeft);
            ApplyStagger(duration);
            _velocity.x = 0.0f;
            _velocity.z = 0.0f;

            // A new fall is a new mash. Carrying the count over would let a second trip start
            // with its prompt already full.
            _mashPresses = 0;
            _lastMashTime = -99.0f;
        }

        /// <summary>
        /// One mash press against the current fall.
        ///
        /// 🧑, 2026-08-25: *"then fall down animation plays and u have to spam a button to
        /// get back up"*.
        ///
        /// ⚠⚠️ THE STUN COMES DOWN WITH THE TRIP, AND FORGETTING THAT IS THE WHOLE BUG
        /// WAITING TO HAPPEN HERE. `ApplyTrip` sets BOTH `_tripLeft` and, through
        /// `ApplyStagger`, `_stunLeft` to the same duration. Shortening only the trip stands the
        /// body up on schedule and leaves it unable to move, sprint, throw or grab for the rest
        /// of the original 2.5 s: the player mashes, watches themselves get up, and then watches
        /// themselves stand there, which reads as the mash having broken the character.
        ///
        /// ⚠️ THE RATE CAP LIVES IN `Combat.MashRecover`, NOT HERE. A bot presses the same
        /// buttons a human does, so both reach the cap through the same function rather than
        /// through an input-layer check only one of them passes through.
        /// </summary>
        public bool MashRecover()
        {
            if (_tripLeft <= 0.0f) return false;

            float since = Time.time - _lastMashTime;
            float before = _tripLeft;
            float after = Combat.MashRecover(_tripLeft, since, out bool accepted);
            if (!accepted) return false;

            _lastMashTime = Time.time;
            _mashPresses++;

            float removed = before - after;
            _tripLeft = after;
            _stunLeft = Mathf.Max(0.0f, _stunLeft - removed);

            return removed > 0.0f;
        }

        /// <summary>⚠️ Max(), NEVER additive. That is the entire bound on a stun chain in a
        /// 1-vs-3 game.</summary>
        public void ApplyStagger(float duration)
        {
            if (AbilitySystem != null && AbilitySystem.IsImmuneToStuns) return;

            _stunLeft = Combat.ApplyStagger(_stunLeft, duration);

            // The bar's denominator follows the same Max: a short stun landing inside a longer
            // one must not rescale the bar and make the remaining time look like it grew.
            _stunTotal = Mathf.Max(_stunTotal, _stunLeft);
        }

        public void ApplyImpulse(Vector3 impulse)
        {
            _externalVelocity += impulse;

            float mag = _externalVelocity.magnitude;
            if (mag > Balance.MaxKnockbackSpeed)
                _externalVelocity = _externalVelocity.normalized * Balance.MaxKnockbackSpeed;

            if (impulse.y > 0.0f)
                _velocity.y = Mathf.Min(impulse.y, Balance.MaxKnockbackLift);
        }

        private void Update()
        {
            if (_tripLeft > 0.0f)
            {
                _tripLeft = Mathf.Max(0.0f, _tripLeft - Time.deltaTime);
                if (_tripLeft <= 0.0f) _tripTotal = 0.0f;
            }

            if (_stunLeft <= 0.0f) return;

            _stunLeft = Mathf.Max(0.0f, _stunLeft - Time.deltaTime);
            if (_stunLeft <= 0.0f) _stunTotal = 0.0f;
        }
    }
}
