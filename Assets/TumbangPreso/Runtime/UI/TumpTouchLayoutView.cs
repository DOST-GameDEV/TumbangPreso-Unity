using System;
using System.Collections.Generic;
using TumbangPreso.InputLayer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>Edits the real native touch controls through existing layout-store APIs.</summary>
    public sealed partial class TumpTouchLayoutView : MonoBehaviour
    {
        private Canvas _canvas;
        private TouchHud _hud;
        private Transform _previousParent;
        private bool _createdHud, _forceBefore, _stickBefore, _lookBefore, _focusBefore;
        private int _sortBefore;
        private float _opacityBefore, _scaleBefore;
        private readonly Dictionary<Verb, TouchTweak> _before = new Dictionary<Verb, TouchTweak>();
        private TumpTouchStickDrag _drag;
        private Action _back;
        private bool _open;
        private Slider _opacitySlider, _sizeSlider;
        public bool IsOpen => _open;

        public void Open(Transform owner, Action back)
        {
            _back = back; _opacityBefore = TouchLayoutStore.Opacity; _scaleBefore = TouchLayoutStore.Scale;
            _before.Clear(); foreach (var entry in InputCatalogue.All) _before[entry.Verb] = TouchLayoutStore.TweakFor(entry.Verb);
            _forceBefore = TouchHud.ForceVisible; _createdHud = TouchHud.Instance == null;
            TouchHud.ForceVisible = true; _hud = TouchHud.Install();
            TouchButton.Customising = true;
            if (_canvas == null) _canvas = OwnerUiLayout.Canvas(owner, "OwnerTouchLayoutCanvas", 820);
            _canvas.gameObject.SetActive(true);
            foreach (Transform child in _canvas.transform) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            OwnerUiBackdrop.Build(_canvas.transform);
            _previousParent = _hud.Canvas.transform.parent; _sortBefore = _hud.Canvas.sortingOrder;
            _hud.Canvas.transform.SetParent(_canvas.transform, false); _hud.Canvas.sortingOrder = 830;
            OwnerUiLayout.Fill((RectTransform)_hud.Canvas.transform);
            Canvas.ForceUpdateCanvases();
            var focus = _hud.Canvas.GetComponent<ScreenFocus>(); _focusBefore = focus != null && focus.enabled;
            if (focus != null) focus.enabled = false;
            _stickBefore = _hud.Stick.enabled; _hud.Stick.enabled = false;
            var look = _hud.Canvas.GetComponentInChildren<TouchLookArea>(true);
            _lookBefore = look != null && look.enabled; if (look != null) look.enabled = false;
            _drag = _hud.Stick.gameObject.AddComponent<TumpTouchStickDrag>();
            BuildToolbar(); _hud.ApplyLayout(); _open = true;
        }
        private void BuildPreviousToolbar()
        {
            var f = TumpUiTheme.Current;
            var toolbar = TumpUiFactory.Surface(_canvas.transform, "LayoutToolbar", TumpSurface.Form.WavePanel, f.DeepOlive, false);
            TumpUiFactory.Place(toolbar.rectTransform, 32, 24, 1380, 256);
            var top = toolbar.gameObject.AddComponent<Canvas>(); top.overrideSorting = true; top.sortingOrder = 850;
            toolbar.gameObject.AddComponent<GraphicRaycaster>();
            var title = TumpUiFactory.Text(toolbar.transform, "Heading", "Make room for your thumbs", 42, true);
            title.color = f.Cream; TumpUiFactory.Place(title.rectTransform, 28, 18, 770, 70);
            var note = TumpUiFactory.Text(toolbar.transform, "Hint", "Drag controls to move them. Save the layout or cancel your changes.", 26);
            note.color = f.Cream; TumpUiFactory.Place(note.rectTransform, 34, 90, 1260, 54);
            Tool(toolbar.transform, "SaveTouchLayout", "Save", 860, true, () => Close(true));
            Tool(toolbar.transform, "CancelTouchLayout", "Cancel", 1030, false, () => Close(false));
            Tool(toolbar.transform, "ResetTouchLayout", "Reset", 1190, false, Reset);
            var opacity = TumpUiFactory.Text(toolbar.transform, "Opacity", "Opacity", 30, true);
            opacity.color = f.Cream; TumpUiFactory.Place(opacity.rectTransform, 34, 162, 144, 68);
            var opacitySlot = TumpUiFactory.Rect(toolbar.transform, "OpacityControl");
            TumpUiFactory.Place(opacitySlot, 182, 155, 480, 84);
            _opacitySlider = TumpFormWidgets.Slider(opacitySlot, "TouchOpacity", TouchLayoutStore.Opacity, TouchLayoutStore.MinOpacity, TouchLayoutStore.MaxOpacity,
                v => TouchLayoutStore.Opacity = v, v => Mathf.RoundToInt(v * 100) + "%");
            var size = TumpUiFactory.Text(toolbar.transform, "Size", "Size", 30, true);
            size.color = f.Cream; TumpUiFactory.Place(size.rectTransform, 718, 162, 112, 68);
            var sizeSlot = TumpUiFactory.Rect(toolbar.transform, "SizeControl");
            TumpUiFactory.Place(sizeSlot, 830, 155, 480, 84);
            _sizeSlider = TumpFormWidgets.Slider(sizeSlot, "TouchSize", TouchLayoutStore.Scale, TouchLayoutStore.MinScale, TouchLayoutStore.MaxScale,
                v => TouchLayoutStore.Scale = v, v => Mathf.RoundToInt(v * 100) + "%");
            foreach (var value in toolbar.GetComponentsInChildren<Text>())
                if (value.name == "Value") value.color = f.Cream;
            ScreenFocus.Install(toolbar.gameObject).Rebuild();
        }
        private static void Tool(Transform root, string name, string label, float x, bool primary, Action action)
        {
            var b = TumpUiFactory.Button(root, name, label, action, primary ? TumpSurface.Form.Pebble : TumpSurface.Form.Link,
                TumpUiTheme.Current.Lime, 30);
            TumpUiFactory.Place((RectTransform)b.transform, x, 22, primary ? 150 : 140, 70);
            if (!primary) { b.GetComponentInChildren<Text>().color = TumpUiTheme.Current.Cream; b.GetComponent<TumpSurface>().LightInk = true; }
        }
        private void Reset()
        {
            // Reset known controls without discarding unknown future-version entries.
            foreach (var entry in InputCatalogue.All)
                TouchLayoutStore.SetTweak(new TouchTweak { Verb = entry.Verb.ToString(), Scale = 1 });
            TouchLayoutStore.Opacity = TouchLayoutStore.DefaultOpacity; TouchLayoutStore.Scale = 1;
            _opacitySlider.SetValueWithoutNotify(TouchLayoutStore.DefaultOpacity);
            _opacitySlider.GetComponentInChildren<Text>().text = Mathf.RoundToInt(TouchLayoutStore.DefaultOpacity * 100) + "%";
            _sizeSlider.SetValueWithoutNotify(1); _sizeSlider.GetComponentInChildren<Text>().text = "100%";
            _hud.ApplyLayout();
        }
        private void Update()
        {
            if (!_open || !MenuNav.CancelPressed || ScreenTakeover.EscapeIsSpoken) return;
            ScreenTakeover.ConsumeEscape(); Close(false);
        }
        private void OnDisable() { if (_open) Close(false); }
        private void Close(bool save)
        {
            if (!_open) return; _open = false;
            if (!save)
            {
                foreach (var tweak in _before.Values) TouchLayoutStore.SetTweak(tweak);
                TouchLayoutStore.Opacity = _opacityBefore; TouchLayoutStore.Scale = _scaleBefore;
            }
            TouchButton.Customising = false;
            if (_drag != null) Destroy(_drag);
            _hud.Stick.enabled = _stickBefore;
            var look = _hud.Canvas.GetComponentInChildren<TouchLookArea>(true); if (look != null) look.enabled = _lookBefore;
            var focus = _hud.Canvas.GetComponent<ScreenFocus>(); if (focus != null) focus.enabled = _focusBefore;
            _hud.Canvas.transform.SetParent(_previousParent, false); _hud.Canvas.sortingOrder = _sortBefore;
            TouchHud.ForceVisible = _forceBefore;
            _hud.ApplyLayout();
            if (_createdHud) { _hud.Canvas.gameObject.SetActive(false); Destroy(_hud.gameObject); }
            _canvas.gameObject.SetActive(false); if (gameObject.activeInHierarchy) _back?.Invoke();
        }
    }

    public sealed class TumpTouchStickDrag : MonoBehaviour, IDragHandler
    {
        public void OnDrag(PointerEventData eventData)
        {
            if (!TouchButton.Customising) return;
            var zone = InputCatalogue.InZone(TouchZone.MoveStick);
            if (zone.Count == 0) return;
            var canvas = GetComponentInParent<Canvas>();
            float scale = canvas != null ? Mathf.Max(.001f, canvas.scaleFactor) : 1;
            var tweak = TouchLayoutStore.TweakFor(zone[0].Verb);
            tweak.OffsetX += eventData.delta.x / scale; tweak.OffsetY += eventData.delta.y / scale;
            TouchLayoutStore.SetTweak(tweak); TouchHud.Instance?.ApplyLayout();
        }
    }
}
