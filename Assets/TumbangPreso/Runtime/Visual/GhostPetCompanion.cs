using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Autonomous dynamic companion pet for Nemu (Sleepy Ghost Girl).
    /// Provides smooth spring-lag following, organic floating physics, breathing pulses,
    /// and cute playful idle AI behaviors (spins, hops, curious peeks, playful orbits)
    /// whenever Nemu is standing still.
    /// </summary>
    public sealed class GhostPetCompanion : MonoBehaviour
    {
        private enum FidgetState
        {
            None,
            TwirlSpin,
            HappyHop,
            CuriousPeek,
            OrbitArc,
            SleepySnooze,
            CheekyGiggle,
            HeartbeatPulse
        }

        [Header("Follow Target & Offset")]
        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _localOffset = new Vector3(-0.52f, 0.58f, -0.05f);
        [SerializeField] private float _smoothTime = 0.14f;

        [Header("Floating Bobbing & Drift")]
        [SerializeField] private float _bobSpeed = 2.8f;
        [SerializeField] private float _bobHeight = 0.045f;
        [SerializeField] private float _driftSpeed = 1.4f;
        [SerializeField] private float _driftAmount = 0.025f;

        [Header("Breathing Pulse & Tilt")]
        [SerializeField] private float _pulseSpeed = 3.2f;
        [SerializeField] private float _pulseAmount = 0.035f;
        [SerializeField] private float _maxTiltAngle = 20.0f;
        [SerializeField] private float _tiltSmoothTime = 0.10f;

        private Vector3 _currentVelocity;
        private Vector3 _baseScale = Vector3.one;
        private float _timeOffset;
        private float _tiltVelocity;
        private float _currentBank;
        private Vector3 _lastTargetPos;
        private bool _hasLastPos;
        private float _lastSpeed;

        // Inertia overshoot
        private Vector3 _overshootVelocity;
        private Vector3 _currentOvershoot;

        // Idle AI Behavior state
        private float _stillTime;
        private float _nextFidgetTimer;
        private FidgetState _currentFidget = FidgetState.None;
        private float _fidgetProgress;
        private float _fidgetDuration = 1.0f;
        private Vector3 _fidgetOffset;
        private float _fidgetExtraYaw;
        private float _fidgetExtraPitch;
        private float _fidgetExtraRoll;
        private Vector3 _fidgetScaleMul = Vector3.one;

        public const string PetName = "Kuro";
        public string CompanionName => PetName;

        // Possession state
        public bool IsPossessed { get; private set; }
        private CharacterMotor _nemuMotor;
        private AIController _temporaryAi;
        private GameObject _possessLightGo;
        private Vector2 _playerInput;

        public void SetPlayerInput(Vector2 input) => _playerInput = input;

        private void Awake()
        {
            _baseScale = transform.localScale;
            _timeOffset = Random.Range(0.0f, 100.0f);
            ResetFidgetTimer();
        }

        private void ResetFidgetTimer()
        {
            _nextFidgetTimer = Random.Range(2.8f, 4.8f);
        }

        public void Bind(Transform target, Vector3? customOffset = null, float scaleMultiplier = 1.0f)
        {
            _target = target;
            if (customOffset.HasValue)
                _localOffset = customOffset.Value;

            _baseScale = Vector3.one * scaleMultiplier;
            transform.localScale = _baseScale;

            // In gameplay, unparent to world root so the companion is its own independent entity
            if (transform.parent != null && !transform.parent.name.Contains("PreviewStage"))
            {
                transform.SetParent(null, true);
            }

            if (_target != null)
            {
                transform.position = _target.TransformPoint(_localOffset);
                transform.rotation = _target.rotation;
                _lastTargetPos = _target.position;
                _hasLastPos = true;
            }
        }

        public void BeginPossession(CharacterMotor nemuMotor)
        {
            _nemuMotor = nemuMotor;
            IsPossessed = true;
            _playerInput = Vector2.zero;

            if (_possessLightGo == null)
            {
                _possessLightGo = new GameObject("GhostPossessLight");
                _possessLightGo.transform.SetParent(transform, false);
                var l = _possessLightGo.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(0.85f, 0.35f, 1.0f);
                l.range = 7.0f;
                l.intensity = 4.0f;
            }

            if (_nemuMotor != null && _nemuMotor.GetComponent<AIController>() == null)
            {
                _temporaryAi = _nemuMotor.gameObject.AddComponent<AIController>();
            }

            GameServices.Audio?.PlayAt("ability_flick_dash", transform.position);
            ComicPopup.Spawn(transform.position, "KURO POSSESSED!", UI.UiTheme.HeroSpiritBright, 1.25f);
        }

        public void EndPossession(bool teleportNemu)
        {
            if (!IsPossessed) return;

            if (teleportNemu && _nemuMotor != null)
            {
                _nemuMotor.Teleport(transform.position);
                GameServices.Audio?.PlayAt("respawn", transform.position);
                Abilities.HeroHazards.SpawnShockTrail(transform.position, 2.5f, 2.0f, _nemuMotor.PlayerSlot);
            }

            if (_temporaryAi != null)
            {
                Destroy(_temporaryAi);
                _temporaryAi = null;
            }

            if (_possessLightGo != null)
            {
                Destroy(_possessLightGo);
                _possessLightGo = null;
            }

            _playerInput = Vector2.zero;
            IsPossessed = false;
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime > 0.0f ? Time.deltaTime : Time.unscaledDeltaTime;
            if (dt <= 0.0f) dt = 0.016f;
            dt = Mathf.Min(dt, 0.10f);

            float time = (Application.isPlaying ? Time.time : Time.unscaledTime) + _timeOffset;

            if (IsPossessed)
            {
                UpdatePossession(dt, time);
                return;
            }

            if (_target == null)
            {
                Destroy(gameObject);
                return;
            }

            // Measure movement velocity
            Vector3 moveVel = Vector3.zero;
            if (_hasLastPos)
            {
                moveVel = (_target.position - _lastTargetPos) / dt;
            }
            _lastTargetPos = _target.position;
            _hasLastPos = true;

            float speed = moveVel.magnitude;

            // Inertia spring overshoot on sudden stop
            if (_lastSpeed > 1.0f && speed <= 0.15f)
            {
                Vector3 fwd = _target.forward * Mathf.Clamp01(_lastSpeed / 4.0f) * 0.14f;
                _currentOvershoot = fwd;
            }
            _lastSpeed = speed;
            _currentOvershoot = Vector3.SmoothDamp(_currentOvershoot, Vector3.zero, ref _overshootVelocity, 0.22f, Mathf.Infinity, dt);

            // Update Idle AI Fidget state
            UpdateFidgetAI(dt, speed, time);

            // Compute ideal anchor point in world space
            Vector3 anchor = _target.TransformPoint(_localOffset + _fidgetOffset) + _currentOvershoot;

            // Compute floating oscillations (sine bobbing + figure-8 sway)
            float bobY = Mathf.Sin(time * _bobSpeed) * _bobHeight;
            float driftX = Mathf.Cos(time * _driftSpeed) * _driftAmount;
            float driftZ = Mathf.Sin(time * _driftSpeed * 0.7f) * (_driftAmount * 0.8f);

            Vector3 floatOffset = _target.rotation * new Vector3(driftX, bobY, driftZ);
            Vector3 desiredPos = anchor + floatOffset;

            // Smooth position lag / trailing
            if (!IsFinite(transform.position) || !IsFinite(_currentVelocity))
            {
                transform.position = desiredPos;
                _currentVelocity = Vector3.zero;
            }
            else
            {
                Vector3 next = Vector3.SmoothDamp(transform.position, desiredPos,
                    ref _currentVelocity, _smoothTime, 30.0f, dt);
                transform.position = IsFinite(next) ? next : desiredPos;
            }

            // Velocity-based banking tilt & forward pitch into run
            float targetBank = 0.0f;
            float targetRunPitch = 0.0f;
            if (speed > 0.1f)
            {
                Vector3 localVel = _target.InverseTransformDirection(moveVel);
                targetBank = Mathf.Clamp(-localVel.x * 4.5f, -_maxTiltAngle, _maxTiltAngle);
                targetRunPitch = Mathf.Clamp(localVel.z * 3.5f, -12.0f, 15.0f);
            }

            _currentBank = Mathf.SmoothDamp(_currentBank, targetBank, ref _tiltVelocity, _tiltSmoothTime, Mathf.Infinity, dt);

            // Floating wobble angles + Fidget angles
            float idleRoll = Mathf.Sin(time * 2.0f) * 3.5f + _fidgetExtraRoll;
            float idlePitch = Mathf.Cos(time * 1.8f) * 3.0f + _fidgetExtraPitch + targetRunPitch;
            float idleYaw = _fidgetExtraYaw;

            Quaternion baseRot = _target.rotation;
            Quaternion tiltRot = Quaternion.Euler(idlePitch, idleYaw, _currentBank + idleRoll);
            transform.rotation = Quaternion.Slerp(transform.rotation, baseRot * tiltRot, dt * 14.0f);

            // Cute breathing scale pulse with speed stretch
            float pulse = 1.0f + Mathf.Sin(time * _pulseSpeed) * _pulseAmount;
            float speedStretchZ = Mathf.Clamp(speed * 0.04f, 0.0f, 0.15f);
            float speedSquashX = speedStretchZ * 0.5f;

            Vector3 finalScale = new Vector3(_baseScale.x * _fidgetScaleMul.x * (pulse - speedSquashX),
                                             _baseScale.y * _fidgetScaleMul.y * (pulse - speedSquashX),
                                             _baseScale.z * _fidgetScaleMul.z * (pulse + speedStretchZ));
            transform.localScale = finalScale;
        }

        private static bool IsFinite(Vector3 value)
            => !float.IsNaN(value.x) && !float.IsInfinity(value.x)
               && !float.IsNaN(value.y) && !float.IsInfinity(value.y)
               && !float.IsNaN(value.z) && !float.IsInfinity(value.z);

        private void UpdateFidgetAI(float dt, float speed, float time)
        {
            var ownerEmotes = _target != null ? _target.GetComponentInParent<Social.EmotePlayer>() : null;
            if (ownerEmotes != null && ownerEmotes.IsEmoting)
            {
                // Dance and bounce happily alongside the player during emotes
                _fidgetExtraYaw = (_fidgetExtraYaw + dt * 280.0f) % 360.0f;
                _fidgetExtraPitch = Mathf.Sin(time * 6.0f) * 8.0f;
                _fidgetExtraRoll = Mathf.Cos(time * 6.0f) * 6.0f;
                _fidgetOffset = new Vector3(0.0f, Mathf.Sin(time * 7.0f) * 0.10f, 0.0f);
                float squash = 1.0f + Mathf.Sin(time * 7.0f) * 0.14f;
                _fidgetScaleMul = new Vector3(1.0f / Mathf.Sqrt(squash), squash, 1.0f / Mathf.Sqrt(squash));
                return;
            }

            if (speed > 0.15f)
            {
                // Active movement cancels idle fidgets smoothly
                _stillTime = 0.0f;
                _currentFidget = FidgetState.None;
                _fidgetOffset = Vector3.Lerp(_fidgetOffset, Vector3.zero, dt * 8.0f);
                _fidgetExtraYaw = Mathf.Lerp(_fidgetExtraYaw, 0.0f, dt * 8.0f);
                _fidgetExtraPitch = Mathf.Lerp(_fidgetExtraPitch, 0.0f, dt * 8.0f);
                _fidgetExtraRoll = Mathf.Lerp(_fidgetExtraRoll, 0.0f, dt * 8.0f);
                _fidgetScaleMul = Vector3.Lerp(_fidgetScaleMul, Vector3.one, dt * 8.0f);
                return;
            }

            _stillTime += dt;

            if (_currentFidget == FidgetState.None)
            {
                _fidgetOffset = Vector3.Lerp(_fidgetOffset, Vector3.zero, dt * 4.0f);
                _fidgetExtraYaw = Mathf.Lerp(_fidgetExtraYaw, 0.0f, dt * 4.0f);
                _fidgetExtraPitch = Mathf.Lerp(_fidgetExtraPitch, 0.0f, dt * 4.0f);
                _fidgetExtraRoll = Mathf.Lerp(_fidgetExtraRoll, 0.0f, dt * 4.0f);
                _fidgetScaleMul = Vector3.Lerp(_fidgetScaleMul, Vector3.one, dt * 4.0f);

                if (_stillTime > 1.2f)
                {
                    _nextFidgetTimer -= dt;
                    if (_nextFidgetTimer <= 0.0f)
                    {
                        // Trigger a random cute idle behavior (1 to 7)
                        int pick = Random.Range(1, 8);
                        _currentFidget = (FidgetState)pick;
                        _fidgetProgress = 0.0f;

                        switch (_currentFidget)
                        {
                            case FidgetState.TwirlSpin:
                                _fidgetDuration = 0.85f;
                                break;
                            case FidgetState.HappyHop:
                                _fidgetDuration = 1.1f;
                                break;
                            case FidgetState.CuriousPeek:
                                _fidgetDuration = 1.6f;
                                break;
                            case FidgetState.OrbitArc:
                                _fidgetDuration = 2.2f;
                                break;
                            case FidgetState.SleepySnooze:
                                _fidgetDuration = 1.8f;
                                break;
                            case FidgetState.CheekyGiggle:
                                _fidgetDuration = 1.0f;
                                break;
                            case FidgetState.HeartbeatPulse:
                                _fidgetDuration = 1.3f;
                                break;
                        }
                    }
                }
            }
            else
            {
                _fidgetProgress += dt / _fidgetDuration;
                float p = Mathf.Clamp01(_fidgetProgress);

                switch (_currentFidget)
                {
                    case FidgetState.TwirlSpin:
                        // 360 degree celebratory pirouette with slight upward bounce
                        float spin = Mathf.SmoothStep(0.0f, 360.0f, p);
                        _fidgetExtraYaw = spin;
                        float jump = Mathf.Sin(p * Mathf.PI) * 0.09f;
                        _fidgetOffset = new Vector3(0.0f, jump, 0.0f);
                        _fidgetScaleMul = new Vector3(1.0f - jump * 1.5f, 1.0f + jump * 2.0f, 1.0f - jump * 1.5f);
                        break;

                    case FidgetState.HappyHop:
                        // Two cute little excited double-hops with squish and stretch
                        float hopSin = Mathf.Abs(Mathf.Sin(p * Mathf.PI * 2.0f));
                        float hopY = hopSin * 0.095f;
                        _fidgetOffset = new Vector3(0.0f, hopY, 0.0f);
                        _fidgetExtraPitch = -hopSin * 12.0f;
                        _fidgetScaleMul = new Vector3(1.0f - hopY * 1.3f, 1.0f + hopY * 1.9f, 1.0f - hopY * 1.3f);
                        break;

                    case FidgetState.CuriousPeek:
                        // Floats forward slightly, tilts inquisitively left and right
                        float peekT = Mathf.Sin(p * Mathf.PI);
                        _fidgetOffset = new Vector3(0.06f * peekT, 0.02f * peekT, 0.14f * peekT);
                        _fidgetExtraYaw = Mathf.Sin(p * Mathf.PI * 2.0f) * 24.0f;
                        _fidgetExtraRoll = Mathf.Cos(p * Mathf.PI * 2.0f) * 16.0f;
                        _fidgetExtraPitch = -9.0f * peekT;
                        _fidgetScaleMul = Vector3.one;
                        break;

                    case FidgetState.OrbitArc:
                        // Drifts in a gentle semi-circle around Nemu and floats back
                        float arcAngle = Mathf.Sin(p * Mathf.PI) * 0.65f;
                        float arcX = Mathf.Sin(arcAngle) * 0.20f;
                        float arcZ = (Mathf.Cos(arcAngle) - 1.0f) * 0.20f;
                        _fidgetOffset = new Vector3(arcX, 0.035f * Mathf.Sin(p * Mathf.PI), arcZ);
                        _fidgetExtraYaw = arcAngle * 38.0f;
                        _fidgetExtraRoll = -arcAngle * 16.0f;
                        _fidgetScaleMul = Vector3.one;
                        break;

                    case FidgetState.SleepySnooze:
                        // Gentle sleepy sink downward, soft sleepy droop nod, then a perky float back up
                        float dip = Mathf.Sin(p * Mathf.PI) * 0.06f;
                        _fidgetOffset = new Vector3(0.0f, -dip, 0.025f * Mathf.Sin(p * Mathf.PI));
                        _fidgetExtraPitch = Mathf.Sin(p * Mathf.PI) * 18.0f;
                        _fidgetExtraRoll = Mathf.Sin(p * Mathf.PI * 2.0f) * 6.0f;
                        float squishY = 1.0f - dip * 1.8f;
                        _fidgetScaleMul = new Vector3(1.0f + dip * 0.9f, squishY, 1.0f + dip * 0.9f);
                        break;

                    case FidgetState.CheekyGiggle:
                        // Playful rapid shimmy giggle vibration with a bouncy upward pop
                        float shimmy = Mathf.Sin(p * Mathf.PI * 6.0f) * (1.0f - p);
                        float giggleY = Mathf.Sin(p * Mathf.PI) * 0.065f;
                        _fidgetOffset = new Vector3(shimmy * 0.03f, giggleY, 0.0f);
                        _fidgetExtraRoll = shimmy * 18.0f;
                        _fidgetExtraPitch = -giggleY * 15.0f;
                        _fidgetScaleMul = new Vector3(1.0f + shimmy * 0.08f, 1.0f - shimmy * 0.08f, 1.0f);
                        break;

                    case FidgetState.HeartbeatPulse:
                        // Three rhythmic squash-and-stretch pulses with a gentle forward lean
                        float pulseSin = Mathf.Clamp01(Mathf.Sin(p * Mathf.PI * 3.0f));
                        float pulseScale = pulseSin * 0.16f;
                        _fidgetOffset = new Vector3(0.0f, pulseSin * 0.02f, pulseSin * 0.04f);
                        _fidgetExtraPitch = -pulseSin * 10.0f;
                        _fidgetScaleMul = new Vector3(1.0f + pulseScale, 1.0f - pulseScale * 0.5f, 1.0f + pulseScale);
                        break;
                }

                if (_fidgetProgress >= 1.0f)
                {
                    _currentFidget = FidgetState.None;
                    ResetFidgetTimer();
                }
            }
        }

        private void UpdatePossession(float dt, float time)
        {
            Vector2 move = _playerInput.sqrMagnitude > 0.001f
                ? _playerInput
                : (_nemuMotor != null && _nemuMotor.Intent != null ? _nemuMotor.Intent.MoveAxis : Vector2.zero);

            Vector3 camFwd = Camera.main != null ? Camera.main.transform.forward : (_nemuMotor != null ? _nemuMotor.transform.forward : transform.forward);
            Vector3 camRight = Camera.main != null ? Camera.main.transform.right : (_nemuMotor != null ? _nemuMotor.transform.right : transform.right);
            camFwd.y = 0.0f;
            camRight.y = 0.0f;

            Vector3 moveDir = (camFwd.normalized * move.y + camRight.normalized * move.x);
            float flySpeed = 12.5f;

            if (moveDir.sqrMagnitude > 0.01f)
            {
                transform.position += moveDir.normalized * flySpeed * dt;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), dt * 14.0f);
            }

            // Floating bob & tilt
            transform.position += Vector3.up * Mathf.Sin(time * 6.0f) * 0.02f;

            // Height clamp to hover over street
            if (Physics.Raycast(transform.position + Vector3.up * 1.5f, Vector3.down, out RaycastHit hit, 5.0f, ~0, QueryTriggerInteraction.Ignore))
            {
                float targetY = hit.point.y + 0.9f;
                Vector3 p = transform.position;
                p.y = Mathf.Lerp(p.y, targetY, dt * 8.0f);
                transform.position = p;
            }

            // Haunt and chill opponents touched by ghost
            var round = GameServices.Round;
            if (round != null && _nemuMotor != null)
            {
                foreach (var p in round.Players)
                {
                    if (p == null || p.PlayerSlot == _nemuMotor.PlayerSlot) continue;
                    Vector3 diff = p.transform.position - transform.position;
                    diff.y = 0.0f;
                    if (diff.magnitude < 1.6f)
                    {
                        p.ApplyStagger(0.35f);
                        p.ApplyImpulse(diff.normalized * 3.0f * dt);
                    }
                }
            }
        }
    }
}
