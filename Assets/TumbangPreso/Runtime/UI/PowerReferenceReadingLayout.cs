using TumbangPreso.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>Keep all three ability descriptions visible while increasing their type size.</summary>
    public sealed class PowerReferenceReadingLayout : MonoBehaviour
    {
        private RectTransform _rect;
        private Text[] _names, _timings, _bodies;
        private bool _large, _known;
        private readonly string[] _shownBodies = new string[3];
        public void Bind(Text[] names, Text[] timings, Text[] bodies)
        { _rect = (RectTransform)transform; _names = names; _timings = timings; _bodies = bodies; Refresh(); }
        private void OnEnable() => Refresh();
        private void LateUpdate() => Refresh();
        private void Refresh()
        {
            if (_rect == null) return;
            bool large = SettingsStore.Current.LargerText;
            bool changed = !_known || _large != large;
            for (int i = 0; i < 3; i++) changed |= _shownBodies[i] != _bodies[i].text;
            if (!changed) return;
            _known = true; _large = large;
            _rect.anchoredPosition = new Vector2(0, large ? 312 : 248);
            float bodyHeight = 96;
            for (int i = 0; i < 3; i++)
            {
                _shownBodies[i] = _bodies[i].text;
                _names[i].fontSize = large ? 41 : 34;
                _timings[i].fontSize = _bodies[i].fontSize = large ? 34 : 28;
                _timings[i].rectTransform.sizeDelta = new Vector2(530, large ? 84 : 57);
                var body = _bodies[i].rectTransform;
                body.anchoredPosition = new Vector2(body.anchoredPosition.x, large ? -218 : -195);
                bodyHeight = Mathf.Max(bodyHeight, _bodies[i].preferredHeight + 8);
            }
            // Fit the longest actual description, keeping short kits compact.
            for (int i = 0; i < 3; i++) _bodies[i].rectTransform.sizeDelta = new Vector2(530, large ? bodyHeight : 211);
            _rect.sizeDelta = new Vector2(1770, large ? 246 + bodyHeight : 430);
        }
    }
}
