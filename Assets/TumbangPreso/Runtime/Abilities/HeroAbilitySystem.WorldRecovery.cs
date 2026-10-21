using UnityEngine;

namespace TumbangPreso.Abilities
{
    public sealed partial class HeroAbilitySystem
    {
        public HeroAbility FindPreparedWorldAbility(string id)
        {
            if (Kit == null) return null;
            foreach (var ability in Kit.AllAbilities)
                if (ability?.Id == id && ability is IPreparedWorldReplication) return ability;
            return null;
        }

        public static bool RestorePreparedWorld(CharacterMotor motor, HeroAbility ability, Vector3 centre,
            float preparation, float remaining)
        {
            if (motor == null || NetAuthority.ShouldResolve() || !(ability is IPreparedWorldReplication recovery)
                || !float.IsFinite(preparation) || !float.IsFinite(remaining)
                || !float.IsFinite(centre.x) || !float.IsFinite(centre.y) || !float.IsFinite(centre.z)
                || motor.AbilitySystem?.AwaitingSkillEffect(ability) == true) return false;
            var context = new AbilityContext(motor, motor.GetComponent<Carrier>(), motor.GetComponent<CombatVerbs>(),
                motor.transform.position, motor.transform.forward, centre);
            using (NetCue.SuppressRelay())
            {
                bool wasOngoing = ability.IsWindingUp || ability.IsActive;
                bool restored = recovery.RestorePreparedWorld(context, centre, preparation, remaining);
                var animator = motor.GetComponentInChildren<Visual.CharacterAnimator>();
                if (restored && ability.IsWindingUp)
                    animator?.PlayActionAt(ability.CastAction, ability.ViewmodelAction,
                        ability.Windup - ability.WindupRemaining, onlyWhileClipRunning: true);
                else if (wasOngoing && !ability.ReservedForIntroduction && preparation <= 0 && remaining <= 0)
                    animator?.CancelHeroAction(ability.CastAction, ability.ViewmodelAction);
            }
            return true;
        }
    }
}
