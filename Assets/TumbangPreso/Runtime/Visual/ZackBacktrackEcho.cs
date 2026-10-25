using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// BACKTRACK's private half (owner, 2026-10-25: "Zack always"): while it is ready, his own faint gold echo stands at the
    /// spot it would take him to, ON HIS SCREEN ONLY. It is his recorded pose from 3 s ago at the kit's own chosen point.
    /// </summary>
    public sealed class ZackBacktrackEcho : MonoBehaviour
    {
        private CharacterMotor _motor;
        private Abilities.ZackHeroKit _kit;
        private ZackFx.Echo _echo;
        private float _nextTry;

        public static ZackBacktrackEcho Attach(CharacterMotor motor, Abilities.ZackHeroKit kit)
        {
            if (motor == null) return null;
            var go = new GameObject("ZackBacktrackEcho-P" + (motor.PlayerSlot + 1));
            var echo = go.AddComponent<ZackBacktrackEcho>();
            echo._motor = motor; echo._kit = kit;
            return echo;
        }

        public bool BelongsTo(CharacterMotor motor) => this != null && _motor == motor;

        private void LateUpdate()
        {
            if (_motor == null || _kit == null || _motor.AbilitySystem?.Kit != _kit) { Destroy(gameObject); return; }
            bool show = ZackFx.IsOwnBody(_motor) && _kit.Skill1 != null && _kit.Skill1.IsReady
                        && GameServices.Round?.RoundActive == true && _kit.TryBacktrackPoint(_motor, out _);
            if (show && _echo == null && Time.time >= _nextTry)
            {
                _echo = ZackFx.Echo.Of(_motor.PlayerSlot, transform);
                if (_echo == null) _nextTry = Time.time + 1.0f;
                else _echo.Coat(1.0f);
            }
            if (_echo == null) return;
            if (show && _kit.TryBacktrackPoint(_motor, out var point))
            {
                _echo.PoseAt(_echo.Track.Newest - ZackRules.BacktrackLookBack(_kit.PathSeconds));
                var root = _echo.Copy.Bones[0];
                root.position = new Vector3(point.x, root.position.y, point.z);
                // Faint: it flickers in and out rather than standing there like another Zack.
                _echo.Visible(Mathf.Repeat(Time.time * 3.0f, 1.0f) < 0.55f);
            }
            else _echo.Visible(false);
        }

        private void OnDestroy() => _echo?.Destroy();
    }
}
