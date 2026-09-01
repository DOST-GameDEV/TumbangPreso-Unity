using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// Execution context passed to hero abilities during activation and tick.
    /// </summary>
    public sealed class AbilityContext
    {
        public CharacterMotor Motor { get; }
        public Carrier Carrier { get; }
        public CombatVerbs Verbs { get; }
        public RoundDirector Round => GameServices.Round;
        public MatchDirector Match => GameServices.Match;

        public bool HasVariant(string variantId)
            => Motor != null && Motor.AbilitySystem != null
               && Motor.AbilitySystem.HasVariant(variantId);

        /// <summary>1 plus the authored gain for this body when the named variant is equipped.</summary>
        public float GainScale(string variantId)
        {
            if (!HasVariant(variantId)) return 1.0f;
            var variant = HeroLoadoutRules.VariantById(variantId);
            return variant == null ? 1.0f : 1.0f + variant.Gain;
        }

        /// <summary>1 plus the authored negative cost for this body.</summary>
        public float CostScale(string variantId)
        {
            if (!HasVariant(variantId)) return 1.0f;
            var variant = HeroLoadoutRules.VariantById(variantId);
            return variant == null ? 1.0f : 1.0f + variant.Cost;
        }

        public Transform Transform => Motor != null ? Motor.transform : null;
        public Vector3 Position => _hasPose ? _position
            : Motor != null ? Motor.transform.position : Vector3.zero;
        public Vector3 Forward => _hasPose ? _forward
            : Motor != null ? Motor.transform.forward : Vector3.forward;

        private readonly bool _hasPose;
        private readonly Vector3 _position;
        private readonly Vector3 _forward;
        private readonly bool _hasAimPoint;
        private readonly Vector3 _aimPoint;

        public Vector3 AimPoint
        {
            get
            {
                if (_hasAimPoint) return _aimPoint;
                if (Motor != null && Motor.Intent != null && Motor.Intent.HasAimPoint)
                    return Motor.Intent.AimPoint;
                return Position + Forward * 10.0f;
            }
        }

        public AbilityContext(CharacterMotor motor, Carrier carrier, CombatVerbs verbs)
        {
            Motor = motor;
            Carrier = carrier;
            Verbs = verbs;
        }

        /// <summary>
        /// Host-side context for a network intent. The client sends where it stood and faced
        /// when it pressed, and the host still decides every victim. Keeping the pose in the
        /// context avoids teleporting the host's copy merely to judge one cast.
        /// </summary>
        public AbilityContext(CharacterMotor motor, Carrier carrier, CombatVerbs verbs,
                              Vector3 position, Vector3 forward, Vector3 aimPoint)
            : this(motor, carrier, verbs)
        {
            forward.y = 0.0f;
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;

            _hasPose = true;
            _position = position;
            _forward = forward.normalized;
            _hasAimPoint = true;
            _aimPoint = aimPoint;
        }
    }
}
