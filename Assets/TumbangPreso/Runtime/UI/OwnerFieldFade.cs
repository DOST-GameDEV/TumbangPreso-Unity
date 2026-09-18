using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Fades a field's fault line in and out as its text comes and goes.
    ///
    /// ⚠️ IT WATCHES THE STRING RATHER THAN TAKING A CALL, so every writer gets
    /// the same behaviour for free and none of them has to remember. The login
    /// has four of them now (live validation, the submit path, the server's own
    /// message and the mode switch clearing everything), and a fade that three
    /// of the four remembered would look like a bug in the fourth.
    ///
    /// ⚠️ THE LINE ALSO RISES A COUPLE OF UNITS AS IT ARRIVES. Two units, once,
    /// under a field the player is looking at: enough that the eye catches the
    /// change while typing, small enough that nothing appears to move.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public sealed class OwnerFieldFade : MonoBehaviour
    {
        private Text _text;
        private RectTransform _rect;
        private Vector2 _rest;
        private string _last = "";
        private float _shown;

        private void Awake()
        {
            _text = GetComponent<Text>();
            _rect = (RectTransform)transform;
            _rest = _rect.anchoredPosition;
        }

        private void LateUpdate()
        {
            string now = _text.text ?? "";
            if (now != _last)
            {
                _last = now;
                if (!string.IsNullOrEmpty(now)) _shown = 0;
            }
            bool present = !string.IsNullOrEmpty(now);
            bool reduced = Settings.SettingsStore.Current.ReducedUiMotion;
            if (reduced) { _shown = 1; SetAlpha(present ? 1 : 0); _rect.anchoredPosition = _rest; return; }

            _shown = Mathf.MoveTowards(_shown, 1, Time.unscaledDeltaTime / .22f);
            float ease = 1 - Mathf.Pow(1 - _shown, 3);
            SetAlpha(present ? ease : 0);
            _rect.anchoredPosition = _rest + Vector2.down * (present ? (1 - ease) * 2f : 0);
        }

        private void SetAlpha(float alpha)
        {
            var colour = _text.color;
            if (Mathf.Approximately(colour.a, alpha)) return;
            colour.a = alpha;
            _text.color = colour;
        }
    }
}
