using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>Ability-owned channels for scoped TimedKitState recovery.</summary>
    public interface ITimedKitReplication
    {
        TimedKitSnapshot CaptureTimedKit();
        bool RestoreTimedKit(CharacterMotor motor, TimedKitSnapshot state);
    }

    public readonly struct TimedKitSnapshot
    {
        public readonly HeroAbility PersonalAbility, UltimateAbility;
        public readonly float PersonalRemaining, UltimateRemaining;
        public readonly bool UltimatePending;
        public readonly bool UltimatePermanent;

        public TimedKitSnapshot(HeroAbility personal, float remaining,
            HeroAbility ultimate = null, float ultimateRemaining = 0, bool ultimatePending = false, bool ultimatePermanent = false)
        {
            PersonalAbility = personal;
            PersonalRemaining = remaining;
            UltimateAbility = ultimate;
            UltimateRemaining = ultimateRemaining;
            UltimatePending = ultimatePending;
            UltimatePermanent = ultimatePermanent;
        }

        public bool TryAge(float personal, float ultimate, bool pending, float age,
            out TimedKitSnapshot state, bool permanent = false)
        {
            state = default;
            if (!Finite(age) || age < 0 || !ValidRemaining(PersonalAbility, personal)
                || !ValidRemaining(UltimateAbility, ultimate)
                || (pending && (UltimateAbility == null || !UltimateAbility.SupportsPendingSnapshot))
                || (permanent && (pending || ultimate != 0 || UltimateAbility?.SupportsPermanentSnapshot != true)))
                return false;
            state = new TimedKitSnapshot(PersonalAbility,
                Mathf.Clamp(personal - age, 0, PersonalAbility?.Duration ?? 0),
                UltimateAbility, Mathf.Clamp(ultimate - age, 0, UltimateAbility?.Duration ?? 0), pending, permanent);
            return true;
        }

        private static bool ValidRemaining(HeroAbility ability, float remaining)
            => Finite(remaining) && remaining >= 0 && (ability == null ? remaining == 0
                : Finite(ability.Duration) && ability.Duration >= 0 && remaining <= ability.Duration + .1f);

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
