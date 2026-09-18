using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The little disc at the right-hand end of a field that says whether what
    /// is in it can be used.
    ///
    /// ⚠️⚠️ THE REFUSED DISC IS HERS AND THE ACCEPTED ONE IS DRAWN, AND THAT IS
    /// NOT A STYLE DECISION. She drew both: the red cross is in her element
    /// sheet at full size and is lifted whole (`login3-invalid`), and the green
    /// tick only exists in 44.png, which is a three-up contact sheet at about a
    /// third scale. There is nothing there to cut, so the green one is built to
    /// the same 29x30 box out of `OwnerUiGlyph.Disc` plus `OwnerUiGlyph.Check`
    /// in her own lime and white. Replace it the moment a full-size export of
    /// hers exists.
    ///
    /// ⚠️ NEITHER STATE MOVES THE FIELD'S RIGHT EDGE. Both marks are centred in
    /// the same 45x34 seat the password fields give their eye, so a field that
    /// gains, changes or loses a mark never reflows, and the three fields keep
    /// one optical right margin whatever state they are in.
    ///
    /// ⚠️ IT FADES AND SETTLES RATHER THAN APPEARING. A mark that pops in while
    /// somebody is mid-word reads as an alarm; over `StateSeconds` it reads as
    /// the form keeping up. Reduced motion gets the end state immediately.
    /// </summary>
    public sealed class OwnerFieldMark : MonoBehaviour
    {
        public enum State { None, Refused, Accepted }

        private Image _refused;
        private CanvasGroup _refusedGroup, _acceptedGroup;
        private RectTransform _accepted;
        private State _state = State.None;
        private float _goal;

        public static OwnerFieldMark Create(Transform field, string name)
        {
            var seat = OwnerLoginLayout.Get("login3-eye-open");
            var rect = OwnerUiLayout.Rect(field, name);
            OwnerUiLayout.Place(rect, 480, 22, seat.width, seat.height);
            var mark = rect.gameObject.AddComponent<OwnerFieldMark>();

            var refused = OwnerMenuArt.Image(rect, "Refused", "login3-invalid");
            var refusedBox = OwnerLoginLayout.Get("login3-invalid");
            OwnerUiLayout.Place(refused.rectTransform,
                (seat.width - refusedBox.width) * .5f, (seat.height - refusedBox.height) * .5f,
                refusedBox.width, refusedBox.height);
            mark._refused = refused;
            mark._refusedGroup = refused.gameObject.AddComponent<CanvasGroup>();

            var accepted = OwnerUiLayout.Rect(rect, "Accepted");
            OwnerUiLayout.Place(accepted,
                (seat.width - refusedBox.width) * .5f, (seat.height - refusedBox.height) * .5f,
                refusedBox.width, refusedBox.height);
            var disc = OwnerUiGlyph.Create(accepted, "Disc", OwnerUiGlyph.Mark.Disc, OwnerUiTheme.Current.Lime);
            OwnerUiLayout.Fill(disc.rectTransform);
            disc.raycastTarget = false;
            var tick = OwnerUiGlyph.Create(accepted, "Tick", OwnerUiGlyph.Mark.Check, Color.white);
            tick.raycastTarget = false;
            OwnerUiLayout.Place(tick.rectTransform, refusedBox.width * .26f, refusedBox.height * .30f,
                refusedBox.width * .48f, refusedBox.height * .40f);
            mark._accepted = accepted;
            mark._acceptedGroup = accepted.gameObject.AddComponent<CanvasGroup>();

            mark._refusedGroup.alpha = 0;
            mark._acceptedGroup.alpha = 0;
            mark._refused.raycastTarget = false;
            return mark;
        }

        public void Show(State state)
        {
            if (_state == state) return;
            _state = state;
            _goal = state == State.None ? 0 : 1;
            if (Settings.SettingsStore.Current.ReducedUiMotion) Settle();
        }

        private void Settle()
        {
            _refusedGroup.alpha = _state == State.Refused ? 1 : 0;
            _acceptedGroup.alpha = _state == State.Accepted ? 1 : 0;
        }

        private void LateUpdate()
        {
            if (Settings.SettingsStore.Current.ReducedUiMotion) { Settle(); return; }
            float step = 1 - Mathf.Exp(-Time.unscaledDeltaTime / Mathf.Max(.03f, OwnerUiTheme.Current.StateSeconds));
            _refusedGroup.alpha = Mathf.Lerp(_refusedGroup.alpha, _state == State.Refused ? _goal : 0, step);
            _acceptedGroup.alpha = Mathf.Lerp(_acceptedGroup.alpha, _state == State.Accepted ? _goal : 0, step);
        }
    }
}
