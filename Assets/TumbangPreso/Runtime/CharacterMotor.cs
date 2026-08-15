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

        /// <summary>The roster pick's name, falling back to the seat number.
        /// CharacterIndex is -1 until a pick arrives.</summary>
        private string CharacterName()
        {
            // ⚠️ THE CORE ROSTER, NOT RosterBook. RosterBook maps an index to a model; the
            // NAME is balance-layer data and lives in the engine-free package, so a headless
            // test can assert a legend without loading a single asset.
            if (_characterIndex < 0 || _characterIndex >= Roster.People.Count)
                return $"P{_playerSlot + 1}";
            return Roster.People[_characterIndex].Name;
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

        public void Teleport(Vector3 position)
        {
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

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            if (_spawnSettle > 0)
            {
                _spawnSettle--;
                transform.position = _spawnSettleAt;
                return;
            }

            Stamina.StepFatigue(dt);

            Vector2 axis = Intent.MoveAxis;
            bool moving = axis.sqrMagnitude > 0.0001f;

            // The sprint multiplier. Fatigue is NOT in this value: it rides the speed-zone
            // stack so it composes with a hazard zone rather than one silently winning.
            float sprint = Stamina.Step(dt, moving, Intent.Pressed(Verb.Sprint));

            float speed = Balance.Speed
                          * Stamina.RoleSpeedScale(_isDefender)
                          * Roster.PersonSpeedScale(_characterIndex)
                          * sprint
                          * Stamina.SpeedZones.Value;

            Vector3 wish = new Vector3(axis.x, 0.0f, axis.y);
            if (wish.sqrMagnitude > 1.0f) wish.Normalize();

            _velocity.x = wish.x * speed;
            _velocity.z = wish.z * speed;

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
            _grounded = (flags & CollisionFlags.Below) != 0 || _cc.isGrounded;

            ShedCharacterPerch();
            Confine();
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
                    _velocity.y = Balance.JumpVelocity;
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
            if (!Confinement.IsConfined(RoundActive, _isDefender)) return;

            Vector3 p = transform.position;
            float x = p.x, z = p.z;
            Confinement.ClampToBox(ref x, ref z);

            if (x != p.x || z != p.z)
            {
                _cc.enabled = false;
                transform.position = new Vector3(x, p.y, z);
                _cc.enabled = true;
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
        /// </summary>
        public bool IsTaggable()
        {
            if (_isDefender || !CanAct()) return false;
            if (!HoldingSlipper) return false;
            return IsInsideBox();
        }

        private float _stunLeft;

        /// <summary>⚠️ Max(), NEVER additive. That is the entire bound on a stun chain in a
        /// 1-vs-3 game.</summary>
        public void ApplyStagger(float duration)
        {
            _stunLeft = Combat.ApplyStagger(_stunLeft, duration);
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
            if (_stunLeft > 0.0f) _stunLeft = Mathf.Max(0.0f, _stunLeft - Time.deltaTime);
        }
    }
}
