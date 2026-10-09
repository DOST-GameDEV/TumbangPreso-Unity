using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    /// <summary>
    /// ⚠️⚠️ THE FIRST-PERSON HANDS REACT TO A JUMP, A FALL AND A LANDING.
    ///
    /// Owner, 2026-10-06, listing the reworks after the arms themselves: *"hand animations, like for when the player
    /// jumps/falls or walks and the hands react"*, in the game's *"cartoony and poppy/lively"* style. Until this the
    /// hands held their rest pose through the whole of a jump: the body below threw its arms up in a V and the view
    /// showed two still fists.
    ///
    /// What they do, the same beats the body's own `jump` and `fall` clips play:
    ///   TAKE-OFF  a quick dip (the anticipation), then they are thrown UP and OUT as the body rises;
    ///   FALLING   they float higher and wider the faster she drops, tipped back, with a small flutter;
    ///   LANDING   they slam down past rest with the impact and spring back over it, harder from a long fall.
    ///
    /// ⚠️ IT IS A SPRING, NOT A POSE PER STATE. Each hand's lift and spread chase a target on an under-damped spring,
    /// and take-off and landing KICK the spring's speed. That is where the overshoot and the settle come from, and why
    /// a short hop and a long drop read differently without a clip for each.
    ///
    /// ⚠️ THE CARRYING HAND ONLY TRANSLATES, AND LESS (the same rule as `RunSway`: turning it swings the tsinelas in
    /// the grip). Anything authored (a throw, a cast, the charge, an aim preview, swimming, a climb, Amihan's flight)
    /// owns the hands and the spring is steered back to rest under it.
    /// </summary>
    public sealed partial class ViewmodelArms
    {
        /// <summary>How far the hands are thrown up while rising, and at the fastest fall. Pivot units (the rig's own).</summary>
        public const float AirRiseLift = .11f, AirFallLift = .19f;
        /// <summary>How far each hand opens outward, rising and at the fastest fall.</summary>
        public const float AirRiseSpread = .05f, AirFallSpread = .10f;
        /// <summary>Degrees the hands tip back (fists up) at full lift.</summary>
        public const float AirTipDegrees = 16f;
        /// <summary>The fall speed, metres a second, at which the hands are fully up.</summary>
        public const float AirFullFallSpeed = 9f;
        /// <summary>The spring: how hard it pulls and how quickly it settles. Under-damped on purpose.</summary>
        public const float AirStiffness = 150f, AirDamping = 11f;
        /// <summary>The kicks: down at take-off, and down at landing for each metre a second of impact.</summary>
        public const float AirTakeOffKick = 1.1f, AirLandKickPerSpeed = .34f, AirLandKickMax = 3.2f;
        /// <summary>The carrying hand's share of the lift.</summary>
        public const float AirCarryShare = .45f;

        private float _airLift, _airLiftSpeed, _airSpread, _airSpreadSpeed, _airFallSpeed;
        private bool _airGrounded = true, _airApplied;
        private Vector3 _airLeftOffset, _airRight;
        private Vector3 _airLeftScale = Vector3.one, _airRightScale = Vector3.one;
        private bool _airScaled;
        private Quaternion _airLeftBase, _airRightBase;
        /// <summary>Each pivot's turn once the jump, the fall and the flail are on it (`HandLife` takes that turn off and puts it back).</summary>
        private Quaternion _airLeftAfter, _airRightAfter;
        /// <summary>The hands' lift this frame, in pivot units (diagnostics and probes).</summary>
        public float AirLift => _airLift;

        private void RestoreAirMotion()
        {
            if (_airScaled && !_airApplied)
            {
                if (_leftArm != null) _leftArm.localScale = _airLeftScale;
                if (_rightArm != null) _rightArm.localScale = _airRightScale;
                _airScaled = false;
            }
            if (!_airApplied) return;
            if (_leftPivot != null) { _leftPivot.localPosition -= _airLeftOffset; _leftPivot.localRotation = _airLeftBase; }
            if (_rightPivot != null) { _rightPivot.localPosition -= _airRight; _rightPivot.localRotation = _airRightBase; }
            if (_airScaled)
            {
                if (_leftArm != null) _leftArm.localScale = _airLeftScale;
                if (_rightArm != null) _rightArm.localScale = _airRightScale;
                _airScaled = false;
            }
            _airApplied = false;
        }

        private void ApplyAirMotion(float dt)
        {
            if (_characterMotor == null)
            {
                // No body (a probe): the same spring, fed the mood the probe plays (`HandLife`).
                if (!ProbeMood.HasValue) return;
                StepAirSpring(ProbeMood.Value.Grounded, ProbeMood.Value.VerticalSpeed, ProbeMood.Value.Free, dt);
                PoseAirMotion();
                return;
            }
            bool free = _charge < 0 && _clip == null && _actionReturnLeft <= 0 && string.IsNullOrEmpty(_aimPreview)
                && !_characterMotor.IsSwimming && !_characterMotor.IsEdgeRecovering && !_characterMotor.IsFlying
                && !_characterMotor.IsStunned;
            StepAirSpring(_characterMotor.IsGrounded, _characterMotor.Velocity.y, free, dt);
            PoseAirMotion();
        }

        /// <summary>
        /// One step of the spring from the body's state. Public so `FpvNaturalArmProbe` can play a jump through it
        /// without a match: the probe and the game then run the same motion, not two copies of it.
        /// </summary>
        public void StepAirSpring(bool grounded, float verticalSpeed, bool free, float dt)
        {
            dt = Mathf.Clamp(dt, 0f, .05f);
            if (grounded != _airGrounded)
            {
                // Leaving the ground: a dip before the throw. Arriving: the impact, by how fast she was falling.
                if (free) _airLiftSpeed -= grounded ? Mathf.Min(AirLandKickMax, AirLandKickPerSpeed * _airFallSpeed) : AirTakeOffKick;
                _airGrounded = grounded;
            }
            _airFallSpeed = grounded ? 0f : Mathf.Max(0f, -verticalSpeed);

            float lift = 0f, spread = 0f;
            if (free && !grounded)
            {
                float fall = Mathf.Clamp01(-verticalSpeed / AirFullFallSpeed);
                var style = CurrentHandStyle;
                lift = (verticalSpeed > 0f ? AirRiseLift : Mathf.Lerp(AirRiseLift, AirFallLift, fall)) * style.Lift;
                spread = (verticalSpeed > 0f ? AirRiseSpread : Mathf.Lerp(AirRiseSpread, AirFallSpread, fall)) * style.Spread;
            }
            // Two small steps a frame keep a stiff spring stable at a low frame rate.
            for (int i = 0; i < 2; i++)
            {
                float h = dt * .5f;
                _airLiftSpeed += ((lift - _airLift) * AirStiffness - _airLiftSpeed * AirDamping) * h;
                _airLift += _airLiftSpeed * h;
                _airSpreadSpeed += ((spread - _airSpread) * AirStiffness - _airSpreadSpeed * AirDamping) * h;
                _airSpread += _airSpreadSpeed * h;
            }
        }

        /// <summary>Puts the spring's lift and spread on the two pivots. Public for the probe, as `StepAirSpring`.</summary>
        public void PoseAirMotion()
        {
            if (_leftPivot == null || _rightPivot == null) return;
            if (Mathf.Abs(_airLift) < .0005f && Mathf.Abs(_airSpread) < .0005f && Mathf.Abs(_airLiftSpeed) < .001f) return;
            // The flutter of a hand in the wind of a fall: small, quick, and only while it is up.
            float flutter = Mathf.Sin(_phase * 23f) * .006f * Mathf.Clamp01(_airSpread / AirFallSpread) * (_airGrounded ? 0f : 1f);
            float tip = AirTipDegrees * Mathf.Clamp(_airLift / AirFallLift, -1f, 1f);

            _airLeftBase = _leftPivot.localRotation; _airRightBase = _rightPivot.localRotation;
            _airLeftOffset = new Vector3(-_airSpread, _airLift + flutter, 0f);
            _airRight = new Vector3(_airSpread, _airLift - flutter, 0f) * (_carrying ? AirCarryShare : 1f);
            _leftPivot.localPosition += _airLeftOffset;
            _rightPivot.localPosition += _airRight;
            // About the view's own right axis, so "fists up" means up on the screen whichever way a pivot is turned.
            _leftPivot.localRotation = Quaternion.AngleAxis(-tip, Vector3.right) * _leftPivot.localRotation;
            if (!_carrying) _rightPivot.localRotation = Quaternion.AngleAxis(-tip, Vector3.right) * _rightPivot.localRotation;
            // THE FLAIL, this hero's own (HandStyle): only while falling, growing with the fall, each arm on its beat.
            // ⚠️ A CIRCLE, NOT A ROCK. Pitch and yaw run a quarter turn apart, so the fist is carried round a loop;
            // on one axis it only swung front and back (owner, 2026-10-06: "most arms falling animation just swings
            // front and back, not flailing in circles").
            var style = CurrentHandStyle;
            float falling = _airGrounded ? 0f : Mathf.Clamp01(_airFallSpeed / (AirFullFallSpeed * .45f));
            if (style.Flail > 0f && falling > 0f)
            {
                float beat = _phase * style.FlailHz * Mathf.PI * 2f, other = beat + style.FlailOffset * Mathf.PI * 2f;
                float reach = style.Flail * falling;
                // Mirrored, so both fists go round outward over the top.
                _leftPivot.localRotation = Quaternion.Euler(Mathf.Sin(beat) * reach, -Mathf.Cos(beat) * reach * style.Circle, 0f) * _leftPivot.localRotation;
                if (!_carrying) _rightPivot.localRotation = Quaternion.Euler(Mathf.Sin(other) * reach, Mathf.Cos(other) * reach * style.Circle, 0f) * _rightPivot.localRotation;
            }
            // THE BILLOW (Nemu): her sleeves are one piece with her arms, so swinging them fast read as her arms
            // thrashing ("it looks like her arms are just swinging really fast"). They swell and slacken across
            // instead, the two a little out of step, and drift on a slow sway.
            if (style.Billow > 0f && falling > 0f && _leftArm != null && _rightArm != null)
            {
                float swell = _phase * style.FlailHz * Mathf.PI * 2f;
                _airLeftScale = _leftArm.localScale; _airRightScale = _rightArm.localScale; _airScaled = true;
                float l = 1f + style.Billow * falling * (.5f + .5f * Mathf.Sin(swell)), r = 1f + style.Billow * falling * (.5f + .5f * Mathf.Sin(swell + 1.9f));
                _leftArm.localScale = Vector3.Scale(_airLeftScale, new Vector3(l, 1f - (l - 1f) * .35f, l));
                if (!_carrying) _rightArm.localScale = Vector3.Scale(_airRightScale, new Vector3(r, 1f - (r - 1f) * .35f, r));
                float sway = Mathf.Sin(_phase * 1.7f) * 5f * falling;
                _leftPivot.localRotation = Quaternion.Euler(0f, 0f, sway) * _leftPivot.localRotation;
                if (!_carrying) _rightPivot.localRotation = Quaternion.Euler(0f, 0f, sway) * _rightPivot.localRotation;
            }
            _airLeftAfter = _leftPivot.localRotation; _airRightAfter = _rightPivot.localRotation;
            _airApplied = true;
        }
    }
}
