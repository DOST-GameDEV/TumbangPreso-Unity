using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class LobbyChat
    {
        private bool _nativeChat;
        private const float NativeLineHeight = 46;
        private void ConstructPreviousNative()
        {
            _nativeChat = true;
            _rect = gameObject.AddComponent<RectTransform>();
            _rect.anchorMin = _rect.anchorMax = Vector2.zero; _rect.pivot = Vector2.zero;
            _rect.anchoredPosition = new Vector2(38, 232); _rect.sizeDelta = new Vector2(560, 172);
            var column = gameObject.AddComponent<VerticalLayoutGroup>();
            column.childControlHeight = column.childControlWidth = true;
            column.childForceExpandHeight = false; column.childForceExpandWidth = true;
            column.childAlignment = TextAnchor.LowerLeft; column.spacing = 6;
            var fitter = gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            for (int i = 0; i < MaxLines; i++)
            {
                var line = TumpUiFactory.Text(transform, "ChatLine" + i, "", 25);
                line.color = _inMatch ? TumpUiTheme.Current.Cream : TumpUiTheme.Current.DeepOlive;
                TumpUiFactory.Height(line, NativeLineHeight);
                if (_inMatch)
                {
                    var outline = line.gameObject.AddComponent<Outline>(); outline.effectColor = TumpUiTheme.Current.DeepOlive;
                    outline.effectDistance = new Vector2(2, -2);
                }
                _lines.Add(line); _stamps.Add(-999);
            }
            _field = TumpUiFactory.Field(transform, "ChatInput", _inMatch ? "Enter to talk" : "Say something");
            _field.characterLimit = Net.MatchRpc.MaxChatLength; _field.onSubmit.AddListener(Submit);
            _fieldRow = _field.gameObject; TumpUiFactory.Height(_field, 76);
            if (_inMatch) _fieldRow.SetActive(false);
            else
            {
                var hit = gameObject.AddComponent<Image>(); hit.color = Color.clear;
                BuildNativeHistory();
            }
            SetNativeLines();
        }
        private void SetPreviousNativeLines()
        {
            if (!_inMatch)
            {
                int first = Mathf.Max(0, _history.Count - LobbyVisibleLines);
                int target = MaxLines - Mathf.Min(LobbyVisibleLines, _history.Count);
                for (int i = 0; i < _lines.Count; i++) _lines[i].text = i < target ? "" : _history[first + i - target];
                if (_history.Count == 0) _lines[_lines.Count - 1].text = EmptyLog;
            }
            foreach (var line in _lines)
            {
                bool has = !string.IsNullOrEmpty(line.text); line.gameObject.SetActive(has); if (!has) continue;
                line.color = _inMatch ? TumpUiTheme.Current.Cream : TumpUiTheme.Current.DeepOlive;
                // Keep the transcript complete in history; compact preview lines ellipsise,
                // never shrink into unreadable microtype.
                line.fontSize = 25; TumpUiFactory.Height(line, NativeLineHeight);
                Ellipsise(line, NativeLineHeight);
            }
        }
        private void BuildPreviousNativeHistory()
        {
            var rect = TumpUiFactory.Rect(_rect, "NativeChatHistory"); _historyPanel = rect.gameObject;
            rect.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one; rect.pivot = new Vector2(.5f, 0);
            rect.offsetMin = Vector2.zero; rect.offsetMax = new Vector2(0, 510);
            var canvas = rect.gameObject.AddComponent<Canvas>(); canvas.overrideSorting = true;
            var parent = _rect.GetComponentInParent<Canvas>(); canvas.sortingOrder = parent != null ? parent.sortingOrder + 10 : 710;
            rect.gameObject.AddComponent<GraphicRaycaster>();
            TumpUiFactory.Ground(rect, TumpUiTheme.Current.Cream);
            var title = TumpUiFactory.Text(rect, "ChatTitle", "Conversation", 34, true);
            TumpUiFactory.Place(title.rectTransform, 22, 16, 360, 70);
            var back = TumpUiFactory.BackButton(rect, "ChatHistoryBack", CloseHistory);
            TumpUiFactory.Anchor((RectTransform)back.transform, new Vector2(1, 1), new Vector2(-95, -48), new Vector2(158, 76));
            var content = TumpUiFactory.Scroll(rect, "ChatHistoryScroll", out _historyScroll);
            var sr = (RectTransform)_historyScroll.transform;
            sr.anchorMin = Vector2.zero; sr.anchorMax = Vector2.one;
            sr.offsetMin = new Vector2(24, 24); sr.offsetMax = new Vector2(-24, -108);
            _historyText = TumpUiFactory.Text(content, "FullTranscript", EmptyLog, 25);
            _historyText.alignment = TextAnchor.UpperLeft;
            InputLayer.ScreenFocus.Install(rect.gameObject);
            _historyPanel.SetActive(false);
        }
    }
}
