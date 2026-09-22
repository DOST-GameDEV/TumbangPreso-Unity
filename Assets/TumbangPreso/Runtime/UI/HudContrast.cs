using System.Collections.Generic;
using TumbangPreso.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    [DefaultExecutionOrder(2100)]
    public sealed class HudContrast : MonoBehaviour
    {
        private struct Plate { public RectTransform Target; public Image Image; public Text[] Labels; }
        private readonly List<Plate> _plates = new List<Plate>();
        private Text[] _texts;
        private Color[] _colours;
        private OwnerScoreStrip[] _scores;
        private bool _contrast;
        public static void Install(RectTransform root)
        {
            var contrast = root.gameObject.AddComponent<HudContrast>();
            contrast._texts = root.GetComponentsInChildren<Text>(true); contrast._colours = new Color[contrast._texts.Length];
            for (int i = 0; i < contrast._texts.Length; i++) contrast._colours[i] = contrast._texts[i].color;
            contrast._scores = root.GetComponentsInChildren<OwnerScoreStrip>(true);
            foreach (string name in new[] { "CanReadout", "LocalState", "ContextualAction", "PowerSeals",
                "SpectatorReadout", "MatchToast", "RoundClock/TimeLeft", "RoundClock/RoundLabel", "TimedStatus0", "TimedStatus1", "TimedStatus2", "TimedStatus3" })
            {
                var target = root.Find(name) as RectTransform; if (target == null) continue;
                var image = OwnerUiLayout.Rect(target.parent, target.name + "ContrastBacking").gameObject.AddComponent<Image>();
                image.transform.SetSiblingIndex(target.GetSiblingIndex()); image.raycastTarget = false;
                image.color = new Color(0,0,0,.94f); image.enabled = false;
                contrast._plates.Add(new Plate { Target = target, Image = image, Labels = target.GetComponentsInChildren<Text>(true) });
            }
        }
        private void LateUpdate()
        {
            bool contrast = SettingsStore.Current.HighContrastHud;
            if (contrast != _contrast) foreach (var strip in _scores) if (strip != null) strip.SetVerticesDirty();
            for (int i = 0; i < _texts.Length; i++)
            {
                var text = _texts[i]; if (text == null) continue;
                if (!_contrast || text.color != Color.white) _colours[i] = text.color;
                // Invisible binding text stays invisible underneath controller glyphs.
                if (contrast && text.color.a > .01f) text.color = Color.white;
                else if (_contrast && !contrast) text.color = _colours[i];
            }
            _contrast = contrast;
            foreach (var plate in _plates)
            {
                if (plate.Target == null) continue;
                bool visible = false;
                if (contrast && plate.Target.gameObject.activeInHierarchy)
                    foreach (var text in plate.Labels) if (text != null && text.isActiveAndEnabled && !string.IsNullOrEmpty(text.text)) { visible = true; break; }
                plate.Image.enabled = visible; if (!visible) continue;
                var target = plate.Target; var rect = plate.Image.rectTransform;
                rect.anchorMin = target.anchorMin; rect.anchorMax = target.anchorMax; rect.pivot = target.pivot;
                rect.anchoredPosition = target.anchoredPosition; rect.sizeDelta = target.sizeDelta + new Vector2(16,8);
                rect.localScale = target.localScale; rect.localRotation = target.localRotation;
            }
        }
    }
}
