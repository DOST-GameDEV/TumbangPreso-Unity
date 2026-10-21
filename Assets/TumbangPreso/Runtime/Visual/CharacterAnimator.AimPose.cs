using TumbangPreso.Abilities;

namespace TumbangPreso.Visual
{
    public sealed partial class CharacterAnimator
    {
        /// <summary>
        /// ⚠️⚠️ A HERO'S TELL WHILE THEY AIM (HERO-10, Phaister, film v7, 2026-09-27). Holding VANISHING ACT or MANIKA MISCHIEF left
        /// her standing in the shared idle for as long as the key was down, so nobody could read that anything was coming, and the
        /// doll's prick happened AFTER she let go (the cast clip began on the release). Plan 4.1 and 4.2 put the tell in the hold:
        /// wrists crossed at her chest with moths crawling from her cuffs; the doll up at her chin with a pin going in.
        ///
        /// An ability names the pose (`HeroAbility.AimPoseAction`) and the rig must carry a clip of that name; otherwise nothing
        /// changes, so no other hero is touched. It loops for as long as the aim lasts, and the release clip starts from it.
        /// `IsAiming` reads local input or the shared replicated body-aim state. Clip names remain local presentation data;
        /// remote holds never enter gameplay input or cause a release-cast on this peer.
        /// </summary>
        private string AimPose()
        {
            var hero = _motor != null ? _motor.AbilitySystem : null;
            if (hero == null || hero.Kit == null || !_motor.CanAct()) return null;
            string reach = ReachPose();
            if (reach != null) return reach;
            return Pose(hero, HeroAbilitySystem.Slot.Skill1) ?? Pose(hero, HeroAbilitySystem.Slot.Skill2) ?? Pose(hero, HeroAbilitySystem.Slot.Ultimate);
        }

        /// <summary>
        /// ⚠️ PHAISTER'S REACH (HERO-10 v3, plan 9.5): her arm held out at the one she is cursing for the whole 2 s, looped for as
        /// long as the BODY is reaching. It reads the replicated reach, not a held key (the curse is a tap), so the owner, the host
        /// and every observer draw the same arm, and a rejoiner sees a reach already running. DRAIN hauls at the chest, HEX reaches
        /// high with the doll at her cheek; a rig without the clip changes nothing.
        /// </summary>
        private string ReachPose()
        {
            if (!_motor.IsVoodooReaching) return null;
            string clip = _motor.VoodooReachKind == VoodooMarkKind.Drain ? "hero-phaister-drain-aim" : "hero-phaister-hexreach-aim";
            return _clips.ContainsKey(clip) ? clip : null;
        }

        private string Pose(HeroAbilitySystem hero, HeroAbilitySystem.Slot slot)
        {
            var ability = hero.AbilityFor(slot);
            if (ability == null || string.IsNullOrEmpty(ability.AimPoseAction) || !hero.IsAiming(slot)) return null;
            return _clips.ContainsKey(ability.AimPoseAction) ? ability.AimPoseAction : null;
        }
    }
}
