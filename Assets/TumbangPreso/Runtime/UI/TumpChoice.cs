using System;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed class TumpChoice : MonoBehaviour
    {
        private string[] _choices;
        private int _value;
        private Action<int> _changed;
        private GameObject _popup;
        private Text _label;
        public static TumpChoice OpenChoice { get; private set; }
        public bool IsOpen => _popup != null && _popup.activeSelf;
        public int Value => _value;
        public void Bind(string[] choices, int value, Action<int> changed)
        {
            _choices = choices; _changed = changed;
            _label = GetComponentInChildren<Text>();
            GetComponent<Button>().onClick.AddListener(Open);
            SetWithoutNotify(value);
        }
        public void SetWithoutNotify(int value)
        {
            _value = Mathf.Clamp(value, 0, Mathf.Max(0, _choices.Length - 1));
            if (_label != null) _label.text = _choices.Length > 0 ? _choices[_value] + "  ›" : "Unavailable";
        }
        private void Update()
        {
            if (!IsOpen || !InputLayer.MenuNav.CancelPressed) return;
            ScreenTakeover.ConsumeEscape(); Close();
        }
        private void OnDisable() => Close();
        private void OnDestroy() { if (_popup != null) Destroy(_popup); }
        private void Open()
        {
            if (IsOpen) { Close(); return; }
            if (OpenChoice != null) OpenChoice.Close();
            OpenChoice = this;
            if (_choices == null || _choices.Length == 0) return;
            if (_popup != null) Destroy(_popup);
            var canvas = GetComponentInParent<Canvas>();
            var overlay = TumpUiFactory.Rect(canvas.transform, "ChoiceOverlay"); TumpUiFactory.Stretch(overlay);
            _popup = overlay.gameObject;
            var catcher = TumpUiFactory.Ground(overlay, Color.clear);
            catcher.gameObject.AddComponent<Button>().onClick.AddListener(Close);
            catcher.GetComponent<Button>().navigation = new Navigation { mode = Navigation.Mode.None };
            var rect = (RectTransform)transform;
            var root = (RectTransform)canvas.transform;
            Vector2 center = root.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
            float width = Mathf.Max(440, rect.rect.width);
            float height = Mathf.Min(680, _choices.Length * 84 + 32);
            float x = Mathf.Clamp(center.x, root.rect.xMin + width * .5f + 24, root.rect.xMax - width * .5f - 24);
            float below = center.y - rect.rect.height * .5f - height * .5f - 8;
            float y = below - height * .5f < root.rect.yMin + 24
                ? center.y + rect.rect.height * .5f + height * .5f + 8 : below;
            y = Mathf.Clamp(y, root.rect.yMin + height * .5f + 24, root.rect.yMax - height * .5f - 24);
            var paper = TumpUiFactory.Surface(overlay, "Choices", TumpSurface.Form.Ticket, TumpUiTheme.Current.Cream);
            TumpUiFactory.Anchor(paper.rectTransform, new Vector2(.5f, .5f), new Vector2(x, y), new Vector2(width, height));
            var list = TumpUiFactory.Scroll(paper.transform, "OptionsScroll", out var scroll);
            TumpUiFactory.Stretch((RectTransform)scroll.transform, 14);
            list.GetComponent<VerticalLayoutGroup>().spacing = 4;
            for (int i = 0; i < _choices.Length; i++)
            {
                int value = i;
                var option = TumpUiFactory.Button(list, "Choice" + i, _choices[i], () =>
                { SetWithoutNotify(value); Close(); _changed?.Invoke(value); }, TumpSurface.Form.Link, TumpUiTheme.Current.Cream, 30);
                option.GetComponent<TumpSurface>().Selected = i == _value;
                TumpUiFactory.Height(option, 76);
            }
            InputLayer.ScreenFocus.Install(overlay.gameObject).Rebuild();
        }
        public void Close()
        {
            if (_popup != null) _popup.SetActive(false);
            if (OpenChoice == this) OpenChoice = null;
        }
    }
}
