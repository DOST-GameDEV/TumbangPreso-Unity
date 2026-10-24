using TumbangPreso.Abilities;
using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    public sealed partial class ViewmodelArms
    {
        // ⚠️ RAGO'S PRAYER IN HIS OWN EYES (MAGMA FIST, owner 2026-10-24: "he js does a prayer pose tapos he long presses or smth to
        // decide where it gets cast"). The body holds `hero-sean-pray-aim` for as long as the key is down; first person played
        // nothing while he aimed and then the old Cinder Gate draw on the release. While he holds it his two hands come together
        // at the bottom centre of the frame, palms pressed, breathing slowly, so his own screen shows the same tell everyone else
        // sees; the release (`magma-palms`, `CastPaths`) drives them down at the court from there.
        private const string PrayerAimPose = "hero-sean-pray-aim";
        private Vector3 _prayerLeft, _prayerRight;
        private float _prayer;
        private bool _prayerApplied;

        private void RestorePrayer()
        {
            if (!_prayerApplied) return;
            if (_leftPivot != null) _leftPivot.localPosition -= _prayerLeft;
            if (_rightPivot != null) _rightPivot.localPosition -= _prayerRight;
            _prayerApplied = false;
        }

        private bool Praying()
        {
            var hero = _characterMotor != null ? _characterMotor.AbilitySystem : null;
            if (hero == null || hero.Kit == null || hero.Kit.HeroId != "sean" || !_characterMotor.CanAct()) return false;
            foreach (var slot in new[] { HeroAbilitySystem.Slot.Skill1, HeroAbilitySystem.Slot.Skill2, HeroAbilitySystem.Slot.Ultimate })
            {
                var ability = hero.AbilityFor(slot);
                if (ability != null && ability.AimPoseAction == PrayerAimPose && hero.IsAiming(slot)) return true;
            }
            return false;
        }

        private void ApplyPrayer(float dt)
        {
            _prayer = Mathf.MoveTowards(_prayer, Praying() ? 1f : 0f, dt / .2f);
            if (_prayer <= 0f || _leftPivot == null || _rightPivot == null) return;
            float e = _prayer * _prayer * (3f - 2f * _prayer);
            float breath = Mathf.Sin(Time.time * 2.2f) * .008f;
            // Palms meet just below the centre of the frame; the slipper hand keeps its grip if it has one.
            _prayerLeft = new Vector3(.17f, .31f + breath, .07f) * e;
            _prayerRight = _carrying ? Vector3.zero : new Vector3(-.12f, .07f + breath, .07f) * e;
            _leftPivot.localPosition += _prayerLeft;
            _rightPivot.localPosition += _prayerRight;
            _prayerApplied = true;
        }
    }
}
