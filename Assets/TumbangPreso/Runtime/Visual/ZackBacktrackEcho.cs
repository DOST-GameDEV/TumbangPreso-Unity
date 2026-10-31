using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// BACKTRACK's private half (owner, 2026-10-25: "Zack always"): while it is ready, the spot it would take him to is shown
    /// ON HIS SCREEN ONLY, at the kit's own chosen point (`ZackHeroKit.TryBacktrackPoint`). Everyone sees the rewind itself on
    /// the cast (`ZackBacktrackLook`), as before.
    ///
    /// ⚠️ THE SWITCH, `MarkInsteadOfBody`. The owner, playing: *"i get this weird ghost thing walking around"*. Off, this draws
    /// what it drew before: his recorded pose from 3 s ago, a flickering see-through copy of his body walking his route at the
    /// spot. On (his pick, "b"), it draws a small mark on the court there instead (`ZackBacktrackMark`) and no body at all.
    /// Read every frame. TO GO BACK, MAKE THE LINE BELOW `false`.
    /// </summary>
    public sealed class ZackBacktrackEcho : MonoBehaviour
    {
        public static bool MarkInsteadOfBody = true;

        /// <summary>
        /// In his own first person the mark is held back when it would sit under his own feet: closer than this to his eye
        /// across the court it is inside the frame's bottom edge and his body, not a spot to go to.
        /// </summary>
        private const float FirstPersonClear = 0.9f;

        private CharacterMotor _motor;
        private Abilities.ZackHeroKit _kit;
        private ZackFx.Echo _echo;
        private ZackBacktrackMark _mark;
        private float _nextTry;
        private bool _staged, _wasRound;

        public static ZackBacktrackEcho Attach(CharacterMotor motor, Abilities.ZackHeroKit kit)
        {
            if (motor == null) return null;
            var go = new GameObject("ZackBacktrackEcho-P" + (motor.PlayerSlot + 1));
            var echo = go.AddComponent<ZackBacktrackEcho>();
            echo._motor = motor; echo._kit = kit;
            return echo;
        }

        /// <summary>
        /// FOR A FILM MADE OUTSIDE PLAY: the mark alone, with no motor and no kit, told the spot by `StagedStep`. Nothing frees
        /// itself; destroy its object when done.
        /// </summary>
        public static ZackBacktrackEcho Stage(Transform parent)
        {
            var go = new GameObject("ZackBacktrackEcho-staged") { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(parent, false);
            var echo = go.AddComponent<ZackBacktrackEcho>();
            echo._staged = true;
            echo._mark = new ZackBacktrackMark(go.transform);
            return echo;
        }

        /// <summary>A staged mark at `seconds` on the film's clock; `show` is Backtrack ready with a spot, `eye` the camera.</summary>
        public void StagedStep(float seconds, bool show, Vector3 spot, Vector3 eye, bool eyes)
        {
            if (!_staged || _mark == null) return;
            _mark.Step(seconds, show, spot, !TooClose(spot, eye, eyes));
        }

        public bool BelongsTo(CharacterMotor motor) => this != null && _motor == motor;

        private static bool TooClose(Vector3 spot, Vector3 eye, bool eyes)
        {
            if (!eyes) return false;
            var across = spot - eye; across.y = 0f;
            return across.magnitude < FirstPersonClear;
        }

        private void LateUpdate()
        {
            if (_staged) return;
            if (_motor == null || _kit == null || _motor.AbilitySystem?.Kit != _kit) { Destroy(gameObject); return; }
            bool round = GameServices.Round?.RoundActive == true;
            bool show = ZackFx.IsOwnBody(_motor) && _kit.Skill1 != null && _kit.Skill1.IsReady && round;
            Vector3 point = default;
            show = show && _kit.TryBacktrackPoint(_motor, out point);

            if (MarkInsteadOfBody)
            {
                if (_echo != null) { _echo.Destroy(); _echo = null; }
                if (_mark == null) _mark = new ZackBacktrackMark(transform);
                // Between rounds nothing is left standing: the round's end clears it at once rather than eating it.
                if (!round && _wasRound) _mark.Clear();
                _wasRound = round;
                var cam = UnityEngine.Camera.main;
                bool visible = !show || cam == null || !TooClose(point, cam.transform.position, ZackFx.InOwnFirstPerson(_motor));
                _mark.Step(Time.time, show, point, visible);
                return;
            }
            if (_mark != null) { _mark.Destroy(); _mark = null; }

            if (show && _echo == null && Time.time >= _nextTry)
            {
                _echo = ZackFx.Echo.Of(_motor.PlayerSlot, transform);
                if (_echo == null) _nextTry = Time.time + 1.0f;
                else _echo.Past(.65f, .35f);
            }
            if (_echo == null) return;
            if (show)
            {
                _echo.PoseAt(_echo.Track.Newest - ZackRules.BacktrackLookBack(_kit.PathSeconds));
                var root = _echo.Copy.Bones[0];
                root.position = new Vector3(point.x, root.position.y, point.z);
                // Faint: it flickers in and out rather than standing there like another Zack.
                _echo.Visible(Mathf.Repeat(Time.time * 3.0f, 1.0f) < 0.55f);
            }
            else _echo.Visible(false);
        }

        private void OnDestroy()
        {
            _echo?.Destroy();
            _mark?.Destroy();
        }
    }
}
