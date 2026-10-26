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
        private float _scale = -1;
        private RectTransform[] _phoneCards;
        private ScrollRect _phoneScroll;
        private Vector2 _phoneParentSize;
        private readonly string[] _shownBodies = new string[3];
        public void Bind(Text[] names, Text[] timings, Text[] bodies)
        { _rect = (RectTransform)transform; _names = names; _timings = timings; _bodies = bodies; Refresh(); }
        public void BindPhone(Text[] names, Text[] timings, Text[] bodies, RectTransform[] cards, ScrollRect scroll)
        { _phoneCards = cards; _phoneScroll = scroll; Bind(names, timings, bodies); }
        private void OnEnable() { if (_phoneScroll != null) _phoneScroll.verticalNormalizedPosition = 1; Refresh(); }
        private void LateUpdate() => Refresh();
        private void Refresh()
        {
            if (_rect == null) return;
            bool large = SettingsStore.Current.LargerText;
            // Wrapping depends on the screen's own scale, so a resized window re-measures too.
            var canvas = GetComponentInParent<Canvas>(); float scale = canvas != null ? canvas.rootCanvas.scaleFactor : 1;
            bool changed = !_known || _large != large || !Mathf.Approximately(scale, _scale);
            Vector2 parentSize = _rect.parent is RectTransform parent ? parent.rect.size : new Vector2(1920, 1080);
            if (_phoneScroll != null) changed |= _phoneParentSize != parentSize;
            for (int i = 0; i < 3; i++) changed |= _shownBodies[i] != _bodies[i].text;
            if (!changed) return;
            _known = true; _large = large; _scale = scale;
            if (_phoneScroll != null) { RefreshPhone(parentSize, large); return; }
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
                bodyHeight = Mathf.Max(bodyHeight, _bodies[i].preferredHeight * 1.06f + 12);
            }
            // Fit the longest actual description at every size. Normal size used to keep a fixed
            // 211-unit box, so a role kit's middle column (its ability and the other role's)
            // spilled out of the sheet at 960x540 while Larger Text grew correctly.
            bodyHeight = Mathf.Max(bodyHeight, 211);
            for (int i = 0; i < 3; i++) _bodies[i].rectTransform.sizeDelta = new Vector2(530, bodyHeight);
            _rect.sizeDelta = new Vector2(1770, (large ? 218 : 195) + bodyHeight + 34);
        }
        private void RefreshPhone(Vector2 parentSize, bool large)
        {
            _phoneParentSize = parentSize;
            float width = Mathf.Min(1770, Mathf.Max(600, parentSize.x - 72));
            float height = Mathf.Max(300, parentSize.y - 360);
            _rect.anchoredPosition = Vector2.zero; _rect.sizeDelta = new Vector2(width, height);
            OwnerUiLayout.Place((RectTransform)_phoneScroll.transform, 24, 88, width - 48, height - 112);
            float usable = width - 80;
            for (int i = 0; i < 3; i++)
            {
                _shownBodies[i] = _bodies[i].text;
                _names[i].fontSize = large ? 41 : 34;
                _timings[i].fontSize = _bodies[i].fontSize = large ? 34 : 28;
                OwnerUiLayout.Place(_names[i].rectTransform, 140, 20, usable - 164, 92);
                float nameHeight = Mathf.Max(92, _names[i].preferredHeight + 8);
                _names[i].rectTransform.sizeDelta = new Vector2(usable - 164, nameHeight);
                float timingY = 32 + nameHeight;
                OwnerUiLayout.Place(_timings[i].rectTransform, 24, timingY, usable - 48, 60);
                float timingHeight = Mathf.Max(44, _timings[i].preferredHeight + 8);
                _timings[i].rectTransform.sizeDelta = new Vector2(usable - 48, timingHeight);
                float bodyY = timingY + timingHeight + 12;
                OwnerUiLayout.Place(_bodies[i].rectTransform, 24, bodyY, usable - 48, 64);
                float bodyHeight = Mathf.Max(64, _bodies[i].preferredHeight * 1.06f + 12);
                _bodies[i].rectTransform.sizeDelta = new Vector2(usable - 48, bodyHeight);
                _phoneCards[i].GetComponent<LayoutElement>().preferredHeight = bodyY + bodyHeight + 24;
            }
            LayoutRebuilder.MarkLayoutForRebuild(_phoneScroll.content);
        }
    }
}
