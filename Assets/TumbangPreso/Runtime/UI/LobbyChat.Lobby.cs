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

        private RectTransform _bubbles;
        private int _bubblesDrawn = -1;
        internal const float DrawerHeight = 844, BubbleMax = 470;

        private void ConstructLobbyColumn()
        {
            _nativeChat = true; _lobbyColumn = true;
            _rect = gameObject.AddComponent<RectTransform>();
            gameObject.AddComponent<OwnerUiCanvas>();
            _rect.anchorMin = _rect.anchorMax = _rect.pivot = Vector2.zero;
            _rect.sizeDelta = new Vector2(HubLobby.RoomChatWidth, DrawerHeight);

            // The sheet is what moves; the root keeps its rect, so a press during the entrance
            // still lands where the drawer will be.
            _sheet = HubKit.Stretch(HubKit.Rect(_rect, "ChatSheet"));
            _sheetGroup = _sheet.gameObject.AddComponent<CanvasGroup>();
            // A large surface, so a cream panel; the bubbles are berry on it.
            var paper = HubKnit.Plate(_sheet, "ChatPaper", Knit.Panel);
            HubKit.Stretch(paper);
            HubKnit.FaceOf(paper).GetComponent<Image>().raycastTarget = true;

            var title = HubKnit.Display(_sheet, "ChatTitle", "ROOM CHAT", 48, HubKnit.KitMaroon);
            HubKit.Place(title.rectTransform, HubKit.TopLeft, new Vector2(50, -32), new Vector2(380, 60));
            var close = HubKnit.Button(_sheet, "CloseChatButton", null, Knit.Danger, () => SetPresented(false), HubStyle.Label, HubGlyph.Mark.Close);
            HubKit.Place((RectTransform)close.transform, HubKit.TopRight, new Vector2(-40, -22), new Vector2(90, 80));

            // The conversation: newest bubble on the floor, older ones scrolling up under the title.
            var log = HubKit.Span(HubKit.Rect(_sheet, "ChatHistoryScroll"), Vector2.zero, Vector2.one,
                                  new Vector2(24, 132), new Vector2(24, 110));
            var catcher = log.gameObject.AddComponent<Image>();
            catcher.color = Color.clear;
            _historyScroll = log.gameObject.AddComponent<ScrollRect>();
            _historyScroll.horizontal = false; _historyScroll.vertical = true;
            _historyScroll.movementType = ScrollRect.MovementType.Clamped; _historyScroll.scrollSensitivity = 36;
            var viewport = HubKit.Stretch(HubKit.Rect(log, "Viewport"));
            viewport.offsetMin = new Vector2(8, 4); viewport.offsetMax = new Vector2(-22, -4);
            viewport.gameObject.AddComponent<RectMask2D>();
            _bubbles = HubKit.Rect(viewport, "Content");
            _bubbles.anchorMin = new Vector2(0, 1); _bubbles.anchorMax = Vector2.one;
            _bubbles.pivot = new Vector2(0.5f, 1); _bubbles.sizeDelta = Vector2.zero;
            var track = HubKit.Rect(log, "Scrollbar");
            track.anchorMin = new Vector2(1, 0); track.anchorMax = Vector2.one; track.pivot = new Vector2(1, 0.5f);
            track.anchoredPosition = new Vector2(-4, 0); track.sizeDelta = new Vector2(10, -28);
            var rail = track.gameObject.AddComponent<Image>();
            rail.color = new Color(HubKnit.KitTan.r, HubKnit.KitTan.g, HubKnit.KitTan.b, 0.4f);
            var bar = track.gameObject.AddComponent<Scrollbar>();
            bar.direction = Scrollbar.Direction.BottomToTop; bar.transition = Selectable.Transition.None;
            var handle = HubKit.Stretch(HubKit.Rect(HubKit.Stretch(HubKit.Rect(track, "SlidingArea")), "Handle")).gameObject.AddComponent<Image>();
            handle.color = HubKnit.KitTanDark;
            bar.targetGraphic = handle; bar.handleRect = handle.rectTransform;
            _historyScroll.viewport = viewport; _historyScroll.content = _bubbles;
            _historyScroll.verticalScrollbar = bar;
            _historyScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            // The composer: a peach knit field and a gold SEND for the pointer, the thumb and the
            // pad. Enter sends too.
            var row = HubKit.Rect(_sheet, "ChatFieldRow");
            row.anchorMin = Vector2.zero; row.anchorMax = new Vector2(1, 0); row.pivot = new Vector2(0.5f, 0);
            row.anchoredPosition = new Vector2(0, 40); row.sizeDelta = new Vector2(-80, 84);
            _fieldRow = row.gameObject;
            _field = HubKnit.Field(row, "ChatInput", "", "Say something. Enter sends.", Net.MatchRpc.MaxChatLength, HubStyle.Body);
            var fieldRect = (RectTransform)_field.transform;
            fieldRect.anchorMin = Vector2.zero; fieldRect.anchorMax = Vector2.one; fieldRect.pivot = new Vector2(0.5f, 0.5f);
            fieldRect.offsetMin = Vector2.zero; fieldRect.offsetMax = new Vector2(-114, 0);
            _field.textComponent.font = HubKnit.SentenceFont(false);
            _field.textComponent.fontSize = HubStyle.Size(HubStyle.Floor);
            var hintWords = (Text)_field.placeholder;
            hintWords.fontSize = HubStyle.Size(HubStyle.Floor);
            hintWords.horizontalOverflow = HorizontalWrapMode.Overflow;
            _field.textComponent.rectTransform.offsetMin = hintWords.rectTransform.offsetMin = new Vector2(24, 0);
            _field.textComponent.rectTransform.offsetMax = hintWords.rectTransform.offsetMax = new Vector2(-16, 0);
            _field.lineType = InputField.LineType.SingleLine;
            _field.onSubmit.AddListener(Submit);
            var send = HubKnit.Button(row, "SendChat", null, Knit.Yellow, () => { if (_field != null) Submit(_field.text); },
                                      HubStyle.Label, HubGlyph.Mark.Right);
            HubKit.Place((RectTransform)send.transform, HubKit.Right, Vector2.zero, new Vector2(100, 80));

            _historyPanel = null;
            _historyText = null;
            SetNativeLines();
        }

        /// <summary>The name this player's own lines arrive under.</summary>
        private static string OwnName()
        {
            string me = GameServices.Account?.DisplayName;
            if (string.IsNullOrWhiteSpace(me)) me = Settings.SettingsStore.Current.PlayerName;
            return me ?? "";
        }

        /// <summary>
        /// The drawer redraw: one bubble per line, the newest on the floor. A line arrives as
        /// "who:  what" (`Add`); a line with no speaker is the room's own note and sits centred.
        ///   theirs  a dark bubble on the left, the speaker's name in honey
        ///   yours   a persimmon bubble on the right
        ///   room    centred, muted, no bubble
        /// </summary>
        private void DrawLobbyColumn()
        {
            if (_bubbles == null) return;
            if (_bubblesDrawn == RoomLineCount && _bubbles.childCount > 0) { _snapPending = true; return; }
            _bubblesDrawn = RoomLineCount;
            for (int i = _bubbles.childCount - 1; i >= 0; i--)
            {
                var old = _bubbles.GetChild(i).gameObject;
                if (Application.isPlaying) { old.transform.SetParent(null, false); Destroy(old); } else DestroyImmediate(old);
            }
            string me = OwnName();
            float width = HubLobby.RoomChatWidth - 24 * 2 - 30, y = -10;
            if (_history.Count == 0)
            {
                var empty = HubKnit.Words(_bubbles, "Empty", EmptyLog, HubStyle.Body, HubKnit.KitTanDark, TextAnchor.MiddleCenter);
                HubKit.Place(empty.rectTransform, HubKit.Top, new Vector2(0, -20), new Vector2(width, 60));
                y = -80;
            }
            for (int i = 0; i < _history.Count; i++)
            {
                string line = _history[i];
                int split = line.IndexOf(":  ", System.StringComparison.Ordinal);
                string who = split > 0 ? line.Substring(0, split) : null;
                string what = split > 0 ? line.Substring(split + 3) : line;
                bool mine = who != null && me.Length > 0 && string.Equals(who, me, System.StringComparison.OrdinalIgnoreCase);
                y -= Bubble(i, who, what, mine, y) + 16;
            }
            _bubbles.sizeDelta = new Vector2(0, -y);
            _snapPending = true;
        }

        private static Sprite _bubbleSprite;

        /// <summary>One bubble at <paramref name="top"/>; returns its height.</summary>
        private float Bubble(int index, string who, string what, bool mine, float top)
        {
            var row = HubKit.Rect(_bubbles, "Bubble" + index);
            row.anchorMin = new Vector2(0, 1); row.anchorMax = new Vector2(1, 1); row.pivot = new Vector2(0.5f, 1);
            row.anchoredPosition = new Vector2(0, top);
            if (who == null)
            {
                row.sizeDelta = new Vector2(0, 40);
                var note = HubKnit.Words(row, "Words", what, HubStyle.Floor, HubKnit.KitTanDark, TextAnchor.MiddleCenter);
                note.rectTransform.anchorMin = new Vector2(0, 1); note.rectTransform.anchorMax = new Vector2(1, 1); note.rectTransform.pivot = new Vector2(.5f, 1);
                note.rectTransform.offsetMin = new Vector2(20, -40); note.rectTransform.offsetMax = new Vector2(-20, 0);
                // Measured at the drawer's known inner width, less a margin: on a small window the font
                // rounds wider and a line that just fitted wraps, so the row is sized for that case.
                float noteWidth = (HubLobby.RoomChatWidth - 24 * 2 - 30 - 40) * .82f;
                var settings = note.GetGenerationSettings(new Vector2(noteWidth, 0));
                float h = Mathf.Max(40, note.cachedTextGeneratorForLayout.GetPreferredHeight(note.text, settings) / note.pixelsPerUnit + 6);
                note.rectTransform.offsetMin = new Vector2(20, -h);
                row.sizeDelta = new Vector2(0, h);
                return h;
            }
            if (_bubbleSprite == null) _bubbleSprite = HubKnit.Knitted("currency_field", new Vector4(40, 40, 40, 40));
            const float pad = 22;
            var shape = HubKit.Rect(row, "Shape").gameObject.AddComponent<Image>();
            shape.sprite = _bubbleSprite; shape.type = Image.Type.Sliced; shape.pixelsPerUnitMultiplier = 2f;
            shape.raycastTarget = false;
            // The peach well tinted: wine-dark for everyone else, persimmon for you.
            shape.color = mine ? HubKnit.KitLime : HubBoard.Colour("#8A2A3C");
            var words = HubKnit.Words(shape.transform, "Words", what, HubStyle.Body,
                                      mine ? HubKnit.GreenInk : HubKnit.KitCream, TextAnchor.UpperLeft, false);
            words.horizontalOverflow = HorizontalWrapMode.Overflow;
            // A little wider than the words measure, and measured narrower below, so a small window's
            // rounder glyphs never push a line onto a second one the bubble has no room for.
            float inner = Mathf.Min(BubbleMax - pad * 2, words.preferredWidth * 1.08f + 10);
            Text name = null;
            if (!mine)
            {
                name = HubKit.Text(shape.transform, "Who", who.ToUpperInvariant(), HubStyle.Floor, true, HubKnit.KitTan, TextAnchor.UpperLeft);
                name.horizontalOverflow = HorizontalWrapMode.Overflow;
                inner = Mathf.Max(inner, Mathf.Min(BubbleMax - pad * 2, name.preferredWidth + 2));
            }
            words.horizontalOverflow = HorizontalWrapMode.Wrap;
            words.rectTransform.anchorMin = words.rectTransform.anchorMax = words.rectTransform.pivot = new Vector2(0, 1);
            words.rectTransform.sizeDelta = new Vector2(inner, 10);
            float nameHeight = name != null ? 36 : 0;
            var measure = words.GetGenerationSettings(new Vector2(inner * .9f, 0));
            float wordsHeight = words.cachedTextGeneratorForLayout.GetPreferredHeight(what, measure) / words.pixelsPerUnit;
            words.rectTransform.sizeDelta = new Vector2(inner, wordsHeight + 2);
            words.rectTransform.anchoredPosition = new Vector2(pad, -(pad * .7f + nameHeight));
            if (name != null)
            {
                name.rectTransform.anchorMin = name.rectTransform.anchorMax = name.rectTransform.pivot = new Vector2(0, 1);
                name.rectTransform.anchoredPosition = new Vector2(pad, -pad * .6f);
                name.rectTransform.sizeDelta = new Vector2(inner, 34);
            }
            float height = wordsHeight + nameHeight + pad * 1.5f;
            var s = shape.rectTransform;
            s.anchorMin = s.anchorMax = s.pivot = mine ? new Vector2(1, 1) : new Vector2(0, 1);
            s.anchoredPosition = Vector2.zero;
            s.sizeDelta = new Vector2(inner + pad * 2, height);
            row.sizeDelta = new Vector2(0, height);
            return height;
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

        private void CountRoomLine() { RoomLineCount++; if (_lobbyColumn && IsPresented) DrawLobbyColumn(); }
    }
}
