using TumbangPreso.Audio;
using TumbangPreso.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed class CalloutCaption : MonoBehaviour
    {
        private RectTransform _panel;
        private Text _words;
        private float _until;
        public string VisibleText => _panel != null && _panel.gameObject.activeSelf ? _words.text : "";
        public static CalloutCaption Create(RectTransform parent)
        {
            var caption = parent.gameObject.AddComponent<CalloutCaption>();
            caption._panel = OwnerUiLayout.Rect(parent, "CalloutCaption");
            var panel = caption._panel;
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(.5f, 1);
            panel.anchoredPosition = new Vector2(0, -250); panel.sizeDelta = new Vector2(500, 92);
            var backing = panel.gameObject.AddComponent<Image>(); backing.color = new Color(0,0,0,.9f); backing.raycastTarget = false;
            var speaker = OwnerUiLayout.Text(panel, "CaptionSpeaker", "ANNOUNCER", 20);
            OwnerUiLayout.Place(speaker.rectTransform, 16, 5, 468, 28); speaker.color = Color.white; speaker.alignment = TextAnchor.MiddleCenter;
            caption._words = OwnerUiLayout.Text(panel, "CaptionWords", "", 32);
            OwnerUiLayout.Place(caption._words.rectTransform, 16, 34, 468, 50);
            caption._words.alignment = TextAnchor.MiddleCenter; caption._words.color = Color.white;
            panel.gameObject.SetActive(false); return caption;
        }
        private void OnEnable() => VoiceDirector.Captioned += Show;
        private void OnDisable()
        {
            VoiceDirector.Captioned -= Show; _until = 0;
            if (_panel != null) _panel.gameObject.SetActive(false);
        }
        private void Show(string words, float seconds)
        {
            if (_panel == null || !SettingsStore.Current.CalloutCaptions) return;
            _words.text = words; _until = Time.unscaledTime + seconds; _panel.gameObject.SetActive(true);
        }
        private void Update()
        {
            if (_panel != null && (!SettingsStore.Current.CalloutCaptions || Time.unscaledTime >= _until))
                _panel.gameObject.SetActive(false);
        }
    }
}
