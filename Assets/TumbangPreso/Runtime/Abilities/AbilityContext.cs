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

        public Transform Transform => Motor != null ? Motor.transform : null;
        public Vector3 Position => Motor != null ? Motor.transform.position : Vector3.zero;
        public Vector3 Forward => Motor != null ? Motor.transform.forward : Vector3.forward;

        public Vector3 AimPoint
        {
            get
            {
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
    }
}
