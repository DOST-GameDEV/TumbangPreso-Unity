using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.InputLayer
{
    public sealed partial class TouchHud
    {
        private void BuildPreviousNative()
        {
            var f = TumpUiTheme.Current;
            _canvas = TumpUiFactory.Canvas(transform, "TumpTouchCanvas", 300);
            var root = (RectTransform)_canvas.transform;
            _lookArea = TumpUiFactory.Rect(root, "LookArea");
            _lookArea.anchorMin = new Vector2(.45f, 0); _lookArea.anchorMax = Vector2.one;
            _lookArea.offsetMin = _lookArea.offsetMax = Vector2.zero;
            var look = _lookArea.gameObject.AddComponent<Image>(); look.color = Color.clear;
            _lookArea.gameObject.AddComponent<TouchLookArea>();
            var baseFace = TumpUiFactory.Surface(root, "MoveStick", TumpSurface.Form.Disc, f.DeepOlive);
            var stickRect = baseFace.rectTransform;
            TumpUiFactory.Anchor(stickRect, Vector2.zero, new Vector2(StickCentreX, StickCentreY), new Vector2(StickRadius * 2, StickRadius * 2));
            baseFace.raycastTarget = true;
            var group = stickRect.gameObject.AddComponent<CanvasGroup>();
            var knob = TumpUiFactory.Surface(stickRect, "Knob", TumpSurface.Form.Disc, f.Cream);
            TumpUiFactory.Anchor(knob.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(StickRadius, StickRadius));
            _stick = stickRect.gameObject.AddComponent<TouchStick>();
            _stick.Bind(stickRect, knob.rectTransform, StickRadius, group);
            foreach (var entry in InputCatalogue.All)
            {
                float size = TouchMetrics.UnitsFor(entry.Size);
                var face = TumpUiFactory.Surface(root, "Touch_" + entry.Verb, TumpSurface.Form.Pebble, Primary(entry) ? f.Lime : f.Cream);
                var rect = face.rectTransform; rect.pivot = new Vector2(.5f, .5f);
                rect.sizeDelta = new Vector2(size, size);
                // Anchor arithmetic interprets existing saved offsets; no legacy
                // hierarchy, texture skin, label or builder is reused.
                Place(rect, entry); face.raycastTarget = true;
                var opacity = face.gameObject.AddComponent<CanvasGroup>();
                var verb = TumpUiFactory.Rect(rect, "VerbIcon").gameObject.AddComponent<TumpVerbSymbol>();
                verb.Verb = entry.Verb; verb.color = f.Brick; verb.raycastTarget = false;
                TumpUiFactory.Stretch(verb.rectTransform, size * .20f);
                var ability = TumpUiFactory.Rect(rect, "AbilityIcon").gameObject.AddComponent<TumpAbilitySymbol>();
                ability.color = f.Brick; ability.raycastTarget = false;
                TumpUiFactory.Stretch(ability.rectTransform, size * .20f);
                ability.gameObject.SetActive(false);
                var button = face.gameObject.AddComponent<TouchButton>();
                button.BindNative(entry, opacity, face, verb, ability);
                if (entry.Zone == TouchZone.SkillRail)
                {
                    var label = TumpUiFactory.Text(rect, "SkillSlot", entry.Verb == Verb.Ultimate ? "ULT" : (entry.Slot + 1).ToString(), 28, true);
                    label.color = f.Brick; label.alignment = TextAnchor.MiddleCenter;
                    TumpUiFactory.Anchor(label.rectTransform, new Vector2(.5f, 0), new Vector2(0, 28), new Vector2(90, 52));
                }
                _buttons.Add(button);
            }
            var sandbox = TumpUiFactory.Button(root, "SandboxToggle", SandboxOffText, () => { PracticeSandbox.Toggle(); RefreshSandbox(); },
                TumpSurface.Form.Ticket, f.Cream, 26);
            TumpUiFactory.Place((RectTransform)sandbox.transform, SandboxMargin, SandboxTopInset, SandboxWidth, SandboxHeight);
            _sandboxRoot = sandbox.gameObject; _sandboxLabel = sandbox.GetComponentInChildren<Text>();
            ApplyModeVisibility(); ApplyLayout(); RefreshSandbox();
        }
    }
}
