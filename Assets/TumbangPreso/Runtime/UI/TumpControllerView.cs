using System;
using System.Collections.Generic;
using System.Linq;
using TumbangPreso.InputLayer;
using TumbangPreso.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>New controller-map presentation over the existing binding and diagram data.</summary>
    public sealed class TumpControllerView : MonoBehaviour
    {
        private Canvas _canvas;
        private TumpSettingsSession _session;
        private RectTransform _list, _diagram, _marker;
        private Text _status;
        private Action _back;
        private int _group;
        private readonly Dictionary<string, Button> _rows = new Dictionary<string, Button>();
        public bool IsOpen => _canvas != null && _canvas.gameObject.activeSelf;
        public void Open(Transform owner, TumpSettingsSession session, Action back)
        {
            if (_session != null) _session.Changed -= Changed;
            _session = session; _back = back; _session.Changed += Changed;
            if (_canvas == null) Build(owner);
            _canvas.gameObject.SetActive(true); BuildRows();
        }
        private void OnDisable() { if (_canvas != null) _canvas.gameObject.SetActive(false); }
        private void OnDestroy() { if (_session != null) _session.Changed -= Changed; }
        private void Update()
        {
            if (!IsOpen || !MenuNav.CancelPressed || ScreenTakeover.EscapeIsSpoken) return;
            ScreenTakeover.ConsumeEscape();
            if (_session.Listening) _session.CancelRebind();
            else if (TumpChoice.OpenChoice != null) TumpChoice.OpenChoice.Close();
            else Back();
        }
        private void Back() { _canvas.gameObject.SetActive(false); _back?.Invoke(); }
        private void Build(Transform owner)
        {
            var f = TumpUiTheme.Current;
            _canvas = TumpUiFactory.Canvas(owner, "TumpControllerCanvas", 850);
            var root = (RectTransform)_canvas.transform; TumpUiFactory.Ground(root, f.Cream);
            var back = TumpUiFactory.BackButton(root, "TumpControllerBack", Back);
            TumpUiFactory.Place((RectTransform)back.transform, 54, 24, 168, 76);
            var title = TumpUiFactory.Text(root, "Heading", "Your controller", 62, true);
            title.color = f.Brick; TumpUiFactory.Place(title.rectTransform, 64, 120, 1220, 94);
            var image = TumpUiFactory.Art(root, "ControllerDiagram", PadDiagram.Art);
            _diagram = image.rectTransform; TumpUiFactory.Place(_diagram, 50, 298, 830, 560);
            var marker = TumpUiFactory.Surface(_diagram, "SelectedControl", TumpSurface.Form.Pebble, f.Lime);
            _marker = marker.rectTransform; _marker.sizeDelta = new Vector2(56, 56); _marker.gameObject.SetActive(false);
            var note = TumpUiFactory.Text(root, "Instructions", "Choose an action, then press its new control.\nReturn to Settings to save or discard changes.", 28);
            TumpUiFactory.Place(note.rectTransform, 74, 862, 784, 122);
            var groupRow = TumpUiFactory.Rect(root, "GroupChoice");
            TumpUiFactory.Place(groupRow, 970, 224, 864, 90);
            TumpFormWidgets.Choice(groupRow, "ControllerGroup", Rebinding.Groups.Select(g => g.Title).ToArray(), _group, v => { _group = v; BuildRows(); });
            _list = TumpUiFactory.Scroll(root, "ControllerActions", out var scroll);
            TumpUiFactory.Place((RectTransform)scroll.transform, 968, 350, 870, 576);
            _status = TumpUiFactory.Text(root, "ControllerStatus", "", 26);
            TumpUiFactory.Place(_status.rectTransform, 970, 936, 862, 96);
        }
        private void BuildRows()
        {
            foreach (Transform child in _list) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            _rows.Clear();
            foreach (string action in Rebinding.Groups[_group].Actions)
            {
                if (Rebinding.IsMovePart(action)) continue;
                string id = action;
                var button = TumpUiFactory.Button(_list, "Controller_" + action, Rebinding.LabelFor(action), () =>
                { Mark(id); _session.BeginRebind(id, InputDeviceKind.Gamepad); }, TumpSurface.Form.Ticket, TumpUiTheme.Current.Apricot, 32);
                TumpUiFactory.Height(button, 100);
                var text = button.GetComponentInChildren<Text>(); text.alignment = TextAnchor.MiddleLeft;
                text.rectTransform.offsetMin = new Vector2(26, 5); text.rectTransform.offsetMax = new Vector2(-190, -5);
                var cap = TumpUiFactory.Text(button.transform, "Binding", Binding(action), 28, true);
                cap.rectTransform.anchorMin = new Vector2(1, 0); cap.rectTransform.anchorMax = Vector2.one;
                cap.rectTransform.offsetMin = new Vector2(-174, 4); cap.rectTransform.offsetMax = new Vector2(-18, -4);
                cap.alignment = TextAnchor.MiddleCenter;
                var glyph = TumpUiFactory.Art(button.transform, "BindingGlyph", InputGlyphs.For(Binding(action).ToUpperInvariant(), false));
                TumpUiFactory.Anchor(glyph.rectTransform, new Vector2(1, .5f), new Vector2(-92, 0), new Vector2(112, 76));
                cap.enabled = glyph.sprite == null;
                button.interactable = Rebinding.HasBindingFor(_session.Actions, action, InputDeviceKind.Gamepad);
                _rows[action] = button;
            }
            if (_group == 0)
            {
                var fixedAxes = TumpUiFactory.Text(_list, "FixedAxes", "Move: " + Rebinding.DisplayNameFor(_session.Actions, "Move", InputDeviceKind.Gamepad)
                    + "\nLook: " + Rebinding.DisplayNameFor(_session.Actions, "Look", InputDeviceKind.Gamepad), 30, true);
                TumpUiFactory.Height(fixedAxes, 132);
            }
            if (ControllerWatch.HasUnrecognised)
            {
                var row = TumpFormWidgets.Row(_list, "GenericController", "Unrecognised controller");
                TumpFormWidgets.Toggle(row, "GenericControllerSupport", GenericPadBridge.Enabled, v =>
                { GenericPadBridge.Enabled = v; _status.text = ControllerWatch.StatusLine(); });
            }
            Changed(ControllerWatch.HasUnrecognised ? ControllerWatch.StatusLine() : "");
            _canvas.GetComponent<ScreenFocus>().Rebuild();
        }
        private string Binding(string action) => Rebinding.DisplayNameFor(_session.Actions, action, InputDeviceKind.Gamepad);
        private void Mark(string action)
        {
            string path = Rebinding.PlainPathFor(_session.Actions, action, InputDeviceKind.Gamepad);
            string control = string.IsNullOrEmpty(path) ? "" : path.Substring(path.LastIndexOf('/') + 1);
            if (!PadDiagram.TryAnchor(control, out var anchor)) { _marker.gameObject.SetActive(false); return; }
            float width = Mathf.Min(_diagram.rect.width, _diagram.rect.height * PadDiagram.Aspect);
            float height = width / PadDiagram.Aspect;
            TumpUiFactory.Anchor(_marker, new Vector2(.5f, .5f), new Vector2((anchor.x - .5f) * width, (.5f - anchor.y) * height), new Vector2(56, 56));
            _marker.gameObject.SetActive(true);
        }
        private void Changed(string text)
        {
            if (_status != null) _status.text = text;
            foreach (var pair in _rows)
            {
                if (pair.Value == null) continue;
                var label = pair.Value.transform.Find("Binding").GetComponent<Text>(); label.text = Binding(pair.Key);
                var glyph = pair.Value.transform.Find("BindingGlyph").GetComponent<Image>();
                glyph.sprite = InputGlyphs.For(label.text.ToUpperInvariant(), false);
                glyph.enabled = glyph.sprite != null; label.enabled = glyph.sprite == null;
            }
        }
    }
}
