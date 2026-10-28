using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The room's chat, rebuilt 2026-10-28 in the hub's own kit
    /// (`docs/reports/lobby-ui-2026-10-28/ux-research.md`). It replaces the pale two-line strip and
    /// its separate HISTORY sheet: CHAT opens one fixed-size column in the room (the owner's rule of
    /// 2026-08-28 still holds, the panel never grows), and that column IS the history, scrolled to
    /// the newest line, with the composer under it.
    ///
    /// ⚠️⚠️ LOBBY ONLY. The match path (`_inMatch`) is built by `ConstructNative` exactly as before;
    /// nothing here runs in a match.
    /// </summary>
    public sealed partial class LobbyChat
    {
        /// <summary>Lines any room chat has received in this process. `HubLobby` compares it with
        /// what it last showed to put the unread dot on CHAT.</summary>
        public static int RoomLineCount { get; private set; }

        private bool _lobbyColumn;
        private RectTransform _sheet;
        private CanvasGroup _sheetGroup;
        private float _sheetShown = 1;

        private void ConstructLobbyColumn()
        {
            _nativeChat = true; _lobbyColumn = true;
            _rect = gameObject.AddComponent<RectTransform>();
            gameObject.AddComponent<OwnerUiCanvas>();
            _rect.anchorMin = _rect.anchorMax = _rect.pivot = Vector2.zero;
            _rect.sizeDelta = new Vector2(HubLobby.RoomChatWidth, HubLobby.RoomColumnHeight);

            // The sheet is what moves; the root keeps its rect, so a press during the entrance
            // still lands where the panel will be.
            _sheet = HubKit.Stretch(HubKit.Rect(_rect, "ChatSheet"));
            _sheetGroup = _sheet.gameObject.AddComponent<CanvasGroup>();
            var paper = HubKit.Shape(_sheet, "ChatPaper", HubStyle.Night, false, 975, 6, 30);
            HubKit.Stretch(paper.rectTransform);
            paper.color = new Color(1, 1, 1, 0.95f);
            paper.raycastTarget = true;
            paper.Depth = 48;

            var title = HubKit.Text(_sheet, "ChatTitle", "ROOM CHAT", HubStyle.Title, true, HubStyle.Golden, TextAnchor.MiddleLeft);
            HubKit.Place(title.rectTransform, HubKit.TopLeft, new Vector2(36, -20), new Vector2(500, 60));
            var close = HubKit.IconButton(_sheet, "CloseChatButton", HubGlyph.Mark.Close, HubStyle.Honey, () => SetPresented(false), 976);
            HubKit.Place((RectTransform)close.transform, HubKit.TopRight, new Vector2(-26, -18), new Vector2(72, 72));

            // The conversation, in a well below the heading: newest line on the floor of the well.
            var log = HubKit.Span(HubKit.Rect(_sheet, "ChatHistoryScroll"), Vector2.zero, Vector2.one,
                                  new Vector2(30, 140), new Vector2(30, 104));
            var well = HubKit.Shape(log, "Well", new Color(0.2f, 0.12f, 0.06f), false, 977, 4, 18);
            HubKit.Stretch(well.rectTransform);
            well.raycastTarget = true;
            _historyScroll = log.gameObject.AddComponent<ScrollRect>();
            _historyScroll.horizontal = false; _historyScroll.vertical = true;
            _historyScroll.movementType = ScrollRect.MovementType.Clamped; _historyScroll.scrollSensitivity = 36;
            var viewport = HubKit.Stretch(HubKit.Rect(log, "Viewport"));
            viewport.offsetMin = new Vector2(22, 14); viewport.offsetMax = new Vector2(-34, -14);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = HubKit.Rect(viewport, "Content");
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1); content.sizeDelta = Vector2.zero;
            var column = content.gameObject.AddComponent<VerticalLayoutGroup>();
            column.childControlWidth = column.childControlHeight = true;
            column.childForceExpandWidth = true; column.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _historyText = HubKit.Text(content, "FullTranscript", EmptyLog, HubStyle.Body, false, HubStyle.HoneySoft, TextAnchor.UpperLeft);
            _historyText.lineSpacing = 1.12f;
            var track = HubKit.Rect(log, "Scrollbar");
            track.anchorMin = new Vector2(1, 0); track.anchorMax = Vector2.one; track.pivot = new Vector2(1, 0.5f);
            track.anchoredPosition = new Vector2(-12, 0); track.sizeDelta = new Vector2(10, -28);
            var rail = track.gameObject.AddComponent<Image>();
            rail.color = new Color(HubStyle.Honey.r, HubStyle.Honey.g, HubStyle.Honey.b, 0.14f);
            var bar = track.gameObject.AddComponent<Scrollbar>();
            bar.direction = Scrollbar.Direction.BottomToTop; bar.transition = Selectable.Transition.None;
            var handle = HubKit.Stretch(HubKit.Rect(HubKit.Stretch(HubKit.Rect(track, "SlidingArea")), "Handle")).gameObject.AddComponent<Image>();
            handle.color = new Color(HubStyle.Honey.r, HubStyle.Honey.g, HubStyle.Honey.b, 0.6f);
            bar.targetGraphic = handle; bar.handleRect = handle.rectTransform;
            _historyScroll.viewport = viewport; _historyScroll.content = content;
            _historyScroll.verticalScrollbar = bar;
            _historyScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            // The composer: HOST GAME's own field, read in the reading face, and a SEND sticker for
            // the pointer, the thumb and the pad. Enter sends too.
            var row = HubKit.Rect(_sheet, "ChatFieldRow");
            row.anchorMin = Vector2.zero; row.anchorMax = new Vector2(1, 0); row.pivot = new Vector2(0.5f, 0);
            row.anchoredPosition = new Vector2(0, 30); row.sizeDelta = new Vector2(-60, 92);
            _fieldRow = row.gameObject;
            _field = HubField.Build(row, "ChatInput", "", "Say something. Enter sends.", Net.MatchRpc.MaxChatLength, 978);
            var fieldRect = (RectTransform)_field.transform;
            fieldRect.anchorMin = Vector2.zero; fieldRect.anchorMax = Vector2.one; fieldRect.pivot = new Vector2(0.5f, 0.5f);
            fieldRect.offsetMin = Vector2.zero; fieldRect.offsetMax = new Vector2(-196, 0);
            _field.textComponent.font = HubStyle.ReadingFont;
            _field.textComponent.fontSize = HubStyle.Size(HubStyle.Body);
            _field.lineType = InputField.LineType.SingleLine;
            _field.onSubmit.AddListener(Submit);
            var send = HubKit.Button(row, "SendChat", "SEND", HubStyle.Honey, () => { if (_field != null) Submit(_field.text); },
                                     HubStyle.Label, 979, HubGlyph.Mark.Play);
            HubKit.Place((RectTransform)send.transform, HubKit.Right, Vector2.zero, new Vector2(180, 92));

            _historyPanel = null;
            SetNativeLines();
        }

        /// <summary>The column redraw: the whole log, the newest line resting on the well's floor.</summary>
        private void DrawLobbyColumn()
        {
            RefreshHistoryPanel();
            if (_historyText != null) _historyText.color = _history.Count == 0 ? HubStyle.HoneySoft : HubStyle.Honey;
            _snapPending = true;
        }

        /// <summary>CHAT slides the sheet in 48 units and fades it up over 0.18 s; Reduced UI motion
        /// shows it at once. Closing is immediate, so a pressed CLOSE never lingers.</summary>
        private void PresentLobbyColumn(bool shown)
        {
            if (!_lobbyColumn) return;
            _sheetShown = shown && !HubStyle.ReducedMotion ? 0 : 1;
            AnimateLobbyColumn();
            if (shown)
            {
                DrawLobbyColumn();
                if (isActiveAndEnabled)
                {
                    if (_refocus != null) StopCoroutine(_refocus);
                    _refocus = StartCoroutine(RefocusNextFrame());
                }
            }
        }

        private void AnimateLobbyColumn()
        {
            if (_sheet == null) return;
            if (_sheetShown < 1) _sheetShown = Mathf.MoveTowards(_sheetShown, 1, Time.unscaledDeltaTime / 0.18f);
            float ease = 1 - Mathf.Pow(1 - _sheetShown, 3);
            _sheet.anchoredPosition = new Vector2((1 - ease) * 48, 0);
            _sheetGroup.alpha = ease;
        }

        private static void CountRoomLine() => RoomLineCount++;
    }
}
