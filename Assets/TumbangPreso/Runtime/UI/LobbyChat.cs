using System.Collections.Generic;
using TumbangPreso.Net;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The chat log and its entry field, in the lobby and in a match.
    ///
    /// 🧑 2026-08-28: *"yea maybe add a chat to our game too that works in lobby and ingame"*.
    ///
    /// ⚠️⚠️ ONE COMPONENT FOR BOTH, WITH ONE SWITCH. The lobby and the arena want the same log,
    /// the same field and the same wire, and differ in exactly two things: whether the field is
    /// always open, and whether old lines fade. Building two of these would be two places for a
    /// wire change to be forgotten, which is the shape `docs/TODO.md` § 38.5 records costing
    /// three dead protocols and one verb that had never travelled at all.
    ///
    /// ⚠️⚠️ IN A MATCH IT MUST NOT SWALLOW MOVEMENT KEYS. A text field that eats WASD while the
    /// player thinks they are running is worse than no chat: they are standing still, being
    /// tagged, and the game looks frozen. <see cref="Typing"/> is what the input reader asks, and
    /// the field is CLOSED by default in a match and open by default in the lobby, where there is
    /// no movement to steal.
    ///
    /// ⚠️ THE LOG FADES IN A MATCH RATHER THAN ACCUMULATING. `VISION.md` § 2 rule 5: a screenshot
    /// taken mid-fight must still show the lata, the chalk and every player. A chat box that grows
    /// is a HUD element that grows, and `docs/TODO.md` § 46 is a section about two banners drawn
    /// on top of something. In the lobby there is nothing to obscure, so lines stay.
    ///
    /// ⚠️ EVERY LINE IS FITTED, and a chat line is the widest arbitrary string in the game: it is
    /// typed by another person on another machine. `MatchRpc.Clamp` bounds it to
    /// <see cref="MatchRpc.MaxChatLength"/> and strips newlines host-side, and `MenuKit.FitBlock`
    /// wraps what arrives. Neither alone is enough: the cap bounds characters and the wrap bounds
    /// height.
    /// </summary>
    public sealed class LobbyChat : MonoBehaviour, UnityEngine.EventSystems.IPointerClickHandler
    {
        /// <summary>
        /// ⚠️⚠️ THE LOBBY CHAT NEVER GROWS, AND CLICKING IT OPENS THE WHOLE LOG. 🧑 2026-08-28:
        /// *"i want u to not make the chat extend anymore bcz theres empty sapce, js keep it at
        /// tthe size i sent and u can see other chats by clicking it"*, after *"chat lowk buns, it
        /// justt extends to 3 chats and u cant see past that"*. Two rules come out of that and
        /// both are load-bearing:
        ///
        /// 1. The panel is a FIXED two-line box plus its field. A panel that grew a line at a time
        ///    walked up through the seat rail and still threw away everything past the sixth
        ///    message, so it was both in the way and lossy.
        /// 2. The panel is a DOOR. Clicking it opens a centred, scrollable log of the last
        ///    <see cref="MaxHistory"/> lines, which is the only place the older messages exist.
        ///
        /// ⚠️ AND NOTHING AUTO-OPENS THAT OVERLAY. The two compact rows are always drawn, so an
        /// arriving line is already on screen; popping a page-sized panel over the lobby for it
        /// would cover the cast to say something the rail underneath is already saying.
        /// </summary>
        public const int LobbyVisibleLines = 2;

        /// <summary>How many lines the scrollable lobby log keeps.</summary>
        public const int MaxHistory = 100;

        /// <summary>What the log says before anybody has said anything.</summary>
        public const string EmptyLog = "No messages yet.";

        /// <summary>
        /// ⚠️ CLICKING THE PANEL OPENS THE LOG **AND** FOCUSES THE FIELD. The plate eats clicks by
        /// design (see `Construct`), so without this a press anywhere on the chat that missed the
        /// field itself was swallowed and read as the control being dead. Focusing as well is what
        /// makes reading the log and answering it one press instead of two, and the overlay is
        /// centred while the field is bottom-left, so both are visible at once.
        /// </summary>
        public void OnPointerClick(UnityEngine.EventSystems.PointerEventData e)
        {
            if (_inMatch || _field == null) return;

            // ⚠⚠ THE SAME CLICK CLOSES IT. 🧑 2026-08-30: *"just make the chat extend when u click
            // it anywhere"*. It is one control that grows and shrinks, not a door to a window, and
            // the dimmed backdrop that used to catch a click-out went with the window
            // (`BuildHistoryPanel`). Without a toggle here the only ways back would be the CLOSE
            // button and Escape, and the press that opened it would do nothing the second time,
            // which reads as the control being stuck.
            if (_historyPanel != null && _historyPanel.activeSelf)
            {
                CloseHistory();
                return;
            }

            OpenHistory();
            _field.ActivateInputField();
        }

        /// <summary>How many lines the MATCH log keeps. The lobby draws
        /// <see cref="LobbyVisibleLines"/> of them and keeps the rest in the scrollable log.
        /// </summary>
        public const int MaxLines = 6;

        /// <summary>Seconds a line stays at full strength in a match before it fades.</summary>
        public const float MatchLineLife = 9.0f;
        public const float MatchFadeTime = 1.4f;

        private const float PanelWidth = 560.0f;
        public const float LineHeight = 26.0f;
        /// <summary>
        /// The entry field.
        ///
        /// ⚠️ IT GREW FROM 44 TO FILL THE PLATE RATHER THAN THE PLATE SHRINKING ONTO IT. 🧑
        /// 2026-08-28: *"extend say something to compensate for empty sapce"*. With the empty log
        /// rows deactivated (see `SetLines`) an idle chat panel is padding plus this and nothing
        /// else, so a short field left a wooden border with a thin slot in it. At 56 the field IS
        /// the panel when nobody has said anything, which is what the control actually is at that
        /// moment: an invitation to type.
        /// </summary>
        public const float FieldHeight = 56.0f;
        private const int LineSize = 19;

        private bool _inMatch;
        private RectTransform _rect;
        private InputField _field;
        private GameObject _fieldRow;
        private GameObject _historyPanel;
        private Text _historyText;
        private ScrollRect _historyScroll;
        private readonly List<Text> _lines = new List<Text>();
        private readonly List<float> _stamps = new List<float>();
        private readonly List<string> _history = new List<string>();

        /// <summary>
        /// True while the player is typing, so the gameplay input reader can stand down.
        ///
        /// ⚠️⚠️ ASK THIS BEFORE READING ANY KEY IN A MATCH. `CLAUDE.md` § 4's input rule is one
        /// control, one action, PER CONTEXT, and chat is a third context after gameplay and
        /// spectating. It is a narrowing of the same kind § 35.3 records for the spectator set: a
        /// player who is typing has no verbs, so a letter cannot be a movement key and a chat key
        /// at the same instant. Two actions inside one context sharing a key is still a defect.
        /// </summary>
        public bool Typing => _field != null && _field.isFocused;

        /// <summary>
        /// True while ANY chat field in the process has the keyboard.
        ///
        /// ⚠️⚠️ A STATIC, BECAUSE THE ASKER CANNOT REACH THE ANSWERER. `PlayerInputReader` is
        /// added by `MatchInstaller.BuildSeat` through `AddComponent`, so it can carry no
        /// inspector reference, and a `FindFirstObjectByType` every frame on every seat is the
        /// shape `docs/TODO.md` § 52.4 measured as a host re-scanning the scene 200 times a
        /// second. One bool written by one component when it gains or loses focus is the cheap
        /// version of the same question.
        ///
        /// ⚠️ IT IS CLEARED IN `OnDisable`, so a chat destroyed with its scene while focused
        /// cannot leave every future match unable to move. That failure would look exactly like
        /// the input being broken and nothing would point here.
        /// </summary>
        public static bool AnyTyping { get; private set; }

        public static LobbyChat Attach(Transform parent, bool inMatch)
        {
            if (parent == null) return null;

            var go = new GameObject("LobbyChat");
            go.transform.SetParent(parent, false);

            var chat = go.AddComponent<LobbyChat>();
            chat._inMatch = inMatch;
            chat.Construct();

            return chat;
        }

        private void Construct()
        {
            var rect = gameObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.0f, 0.0f);
            rect.anchorMax = new Vector2(0.0f, 0.0f);
            rect.pivot = new Vector2(0.0f, 0.0f);
            rect.anchoredPosition = new Vector2(48.0f, 48.0f);
            // ⚠️ THE ROW SPACING IS IN THIS SUM. `column.spacing` is 4 and the padding is 10 top
            // and bottom, so two rows cost (26 + 4) x 2, not 26 x 2, and leaving the spacing out
            // clipped the top row by exactly the gaps it forgot.
            rect.sizeDelta = new Vector2(PanelWidth,
                                         ((LineHeight + 4.0f) * (_inMatch ? MaxLines : LobbyVisibleLines)) +
                                         FieldHeight + 20.0f);

            _rect = rect;

            // ⚠️⚠️ THE PANEL IS AS TALL AS ITS CONTENT, NOT AS TALL AS ITS CAPACITY. Six reserved
            // line slots at a fixed height meant an empty lobby drew a 224 px wooden slab with one
            // input at the bottom and five rows of nothing above it, which is most of what
            // `Logs/shots-runtime/Lobby-v11.png` is. `SetLines` gives an empty line a height of
            // zero and this fitter collapses the panel onto whatever is left.
            //
            // ⚠️ THE PIVOT IS THE BOTTOM-LEFT (set above), so the panel GROWS UPWARD as lines
            // arrive and the entry field never moves under the player's cursor. With a centred
            // pivot the field would drift down half a line per message.
            if (_inMatch)
            {
                var fitter = gameObject.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            var column = gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = 4;
            column.padding = new RectOffset(12, 12, 10, 10);
            column.childControlWidth = true;

            // ⚠️⚠️ `childControlHeight` MUST BE TRUE OR EVERY ROW IS ZERO PIXELS TALL AND THE WHOLE
            // PANEL IS INVISIBLE. A `VerticalLayoutGroup` only reads a child's
            // `LayoutElement.preferredHeight` when it CONTROLS height; with control off it leaves
            // each child at whatever `sizeDelta.y` it already had, and a GameObject built from
            // code has 0. The six log lines and the entry field were all present, all wired, all
            // parented correctly and all 0 px tall, so `Logs/shots-runtime/Lobby-v7.png` shows bare
            // road where the chat is. Nothing logs, nothing throws, and the failure is
            // indistinguishable from the component never having been created.
            //
            // ⚠️ THE SAME TRAP IS WHY `LobbyJoinPanel`'S HEADINGS DRAW AND ITS BOXES DO NOT: a
            // legacy `Text` set to `Overflow` renders outside a zero-height rect, so a label looks
            // fine while the plate behind it is nothing. Anything with an `Image` disappears
            // completely and anything with only text survives, which makes the cause hard to see.
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            column.childAlignment = TextAnchor.LowerLeft;

            for (int i = 0; i < MaxLines; i++)
            {
                var line = MenuKit.Label(transform, "", LineSize, UiTheme.Cream,
                                         Vector2.zero, Vector2.zero, Vector2.zero,
                                         TextAnchor.LowerLeft);

                // ⚠️ IT EATS NO CLICKS. The log sits over the bottom-left of the screen, which in
                // the lobby is the START button and in a match is the ability deck.
                line.raycastTarget = false;
                line.horizontalOverflow = HorizontalWrapMode.Wrap;

                var element = line.gameObject.AddComponent<LayoutElement>();
                element.minHeight = LineHeight;
                element.preferredHeight = LineHeight;

                _lines.Add(line);
                _stamps.Add(-999.0f);
            }

            BuildField();

            // ⚠️⚠️ THE LOBBY GETS A PLATE AND THE MATCH DOES NOT, AND THAT IS `UiTheme.HeroPlate`'S
            // DISTINCTION APPLIED RATHER THAN IGNORED. Its note: a menu panel is FURNITURE, it is
            // the thing you are looking at, and it may be opaque; a combat overlay is a WINDOW
            // whose job is to disappear and let the court behind it read. This one component is
            // both screens, so it is both things, decided by the same flag that decides whether
            // the field is always open.
            //
            // ⚠️ IN THE LOBBY IT SITS IN THE BAND BELOW BOTH COLUMNS, which is the one horizontal
            // strip on that screen with nothing in it: the config column ends around y 945 and the
            // seat column around y 940, and everything under that was bare road. A floating input
            // with no plate there read as a stray control rather than as the chat.
            if (!_inMatch)
            {
                var plate = gameObject.AddComponent<Image>();
                plate.sprite = GodotTheme.WoodBox(UiTheme.WoodDeep, UiTheme.WoodEdge);
                plate.type = Image.Type.Sliced;
                plate.color = Color.white;

                // ⚠️ IT DOES EAT CLICKS, unlike everything else this component draws. A chat panel
                // you can click THROUGH into the seat rows behind it is a panel that steals half
                // its own presses and mis-seats somebody.
                plate.raycastTarget = true;

                // ⚠️ THE PRESS IS HANDLED BY `OnPointerClick` AND NOT BY A `Button` ON THIS SAME
                // OBJECT. Both would fire for one click, so the log would open and the field would
                // be focused twice; one handler that does both in order is the whole behaviour.
                BuildHistoryPanel();
            }

            SetLines();
        }

        private void BuildField()
        {
            _fieldRow = new GameObject("ChatField");
            _fieldRow.transform.SetParent(transform, false);

            var image = _fieldRow.AddComponent<Image>();
            image.sprite = GodotTheme.WoodBox(UiTheme.WoodDark, UiTheme.WoodEdge);
            image.type = Image.Type.Sliced;
            image.color = Color.white;

            var element = _fieldRow.AddComponent<LayoutElement>();
            element.minHeight = FieldHeight;
            element.preferredHeight = FieldHeight;

            var placeholder = MenuKit.Label(_fieldRow.transform,
                                            _inMatch ? "ENTER to talk" : "Say something",
                                            LineSize, UiTheme.CreamMuted,
                                            Vector2.zero, Vector2.zero, Vector2.zero,
                                            TextAnchor.MiddleLeft);
            placeholder.raycastTarget = false;
            Inset(placeholder.rectTransform);

            var typed = MenuKit.Label(_fieldRow.transform, "", LineSize, UiTheme.Cream,
                                      Vector2.zero, Vector2.zero, Vector2.zero,
                                      TextAnchor.MiddleLeft);
            typed.raycastTarget = false;
            typed.supportRichText = false;
            Inset(typed.rectTransform);

            _field = _fieldRow.AddComponent<InputField>();
            _field.textComponent = typed;
            _field.placeholder = placeholder;
            _field.targetGraphic = image;
            _field.lineType = InputField.LineType.SingleLine;

            // ⚠️ THE SAME CAP THE HOST ENFORCES, so the player is stopped by the field rather than
            // silently truncated by the relay. The host still clamps: this is a courtesy, not the
            // rule (see `MatchRpc.HostRelayChat`).
            _field.characterLimit = MatchRpc.MaxChatLength;

            _field.onSubmit.AddListener(Submit);

            // In a match the field is closed until ENTER opens it. See `Typing`.
            if (_inMatch) _fieldRow.SetActive(false);
        }

        /// <summary>
        /// Moves the panel's bottom-left corner, for a screen whose furniture is already in the
        /// default spot.
        ///
        /// ⚠️ THE LOBBY'S LEFT COLUMN OWNS THE BOTTOM-LEFT CORNER, so the chat sits to the right
        /// of it rather than on top of the START button. In a match nothing else is in that
        /// corner and the default is correct.
        /// </summary>
        public void PlaceAt(Vector2 corner, float width)
        {
            if (_rect == null) return;

            _rect.anchoredPosition = corner;
            _rect.sizeDelta = new Vector2(width, _rect.sizeDelta.y);
        }

        /// <summary>Places lobby chat directly below the raised lobby card. A top-right pivot
        /// makes new lines grow down the social rail instead of back upward over the card.</summary>
        public void PlaceBelowTopRight(float rightMargin, float top, float width)
        {
            if (_rect == null) return;

            _rect.anchorMin = Vector2.one;
            _rect.anchorMax = Vector2.one;
            _rect.pivot = Vector2.one;
            _rect.anchoredPosition = new Vector2(-rightMargin, -top);
            _rect.sizeDelta = new Vector2(width, _rect.sizeDelta.y);
        }

        /// <summary>Anchors the lobby field to the bottom-right social rail. New chat lines grow
        /// upward, keeping the entry field in a stable place and the cast's upper bodies clear.</summary>
        /// <summary>
        /// How tall the panel actually is right now.
        ///
        /// ⚠️⚠️ IT IS NOT `(LineHeight * MaxLines) + FieldHeight + 24`, WHICH IS WHAT THE FIRST
        /// ATTEMPT AT STACKING THE LOBBY DRAWER ABOVE IT ASSUMED. That expression is the CAPACITY;
        /// the `ContentSizeFitter` in `Construct` collapses the panel onto whatever lines are
        /// actually in it, so an empty chat is about 65 px and the capacity is 224.
        /// `Logs/shots-runtime/Lobby-v36.png` has the LOBBY & SERVERS pill floating in the middle
        /// of the frame over the fourth character, 160 px of nothing above a chat box, because it
        /// was positioned off the number rather than off the panel.
        ///
        /// ⚠️ AND IT CHANGES AS LINES ARRIVE, so a caller stacking against it has to re-read it
        /// rather than measure once. See `ConvertedMatchSetup.LateUpdate`.
        /// </summary>
        public float PanelHeight => _rect != null ? _rect.rect.height : 0.0f;

        public void PlaceBottomRight(float rightMargin, float bottom, float width)
        {
            if (_rect == null) return;

            _rect.anchorMin = new Vector2(1.0f, 0.0f);
            _rect.anchorMax = new Vector2(1.0f, 0.0f);
            _rect.pivot = new Vector2(1.0f, 0.0f);
            _rect.anchoredPosition = new Vector2(-rightMargin, bottom);
            _rect.sizeDelta = new Vector2(width, _rect.sizeDelta.y);
        }

        private static void Inset(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(14.0f, 0.0f);
            rt.offsetMax = new Vector2(-14.0f, 0.0f);
        }

        private void OnEnable() => MatchRpc.OnChatLine += Add;

        private void OnDestroy()
        {
            // ⚠️ THE OVERLAY IS NOT A CHILD OF THIS COMPONENT (see `BuildHistoryPanel`), so
            // destroying the chat does not destroy it. Left behind, it is a wooden log panel
            // floating over the next screen with nothing driving it.
            if (_historyPanel != null) Destroy(_historyPanel);
        }

        private void OnDisable()
        {
            MatchRpc.OnChatLine -= Add;
            CloseHistory();

            // See AnyTyping: a chat that goes away while focused must not leave the flag set.
            AnyTyping = false;
        }

        /// <summary>
        /// ⚠️ ENTER OPENS IT, ENTER SENDS IT, ESCAPE CANCELS. Read here rather than through the
        /// input asset because this is the ONE key that has to work while the gameplay map is
        /// suspended: routing the key that RESUMES the map through the map that is suspended is
        /// the deadlock version of this feature.
        ///
        /// ⚠️ AND ESCAPE IS CHECKED BEFORE THE SUBMIT. `ConvertedScreen.Update` also reads Escape
        /// and backs out of the screen; consuming it here while the field is open is what stops
        /// one press both cancelling a message and leaving the lobby.
        /// </summary>
        private void Update()
        {
            // ⚠️ WRITTEN EVERY FRAME BY THE ONE COMPONENT THAT KNOWS, INCLUDING IN THE LOBBY.
            // The lobby has no `PlayerInputReader` to protect, but the flag has to be FALSE there
            // rather than stale from a previous match, and an `InputField` can lose focus to a
            // click without telling anybody.
            AnyTyping = Typing;

            if (!_inMatch)
            {
                // ⚠️ ESCAPE CLOSES THE LOG BEFORE `ConvertedScreen` READS IT. That component
                // also reads Escape and backs out of the lobby, so one press has to mean the
                // innermost open thing: with the log up it closes the log, and only after that
                // does it leave the screen. `CLOSE` on the overlay is the same call.
                if (_historyPanel != null && _historyPanel.activeSelf &&
                    Input.GetKeyDown(KeyCode.Escape))
                {
                    _historyPanel.SetActive(false);
                }
                return;
            }

            if (Typing)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) Close();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Open();

            Fade();
        }

        private void Open()
        {
            if (_fieldRow == null || _field == null) return;

            _fieldRow.SetActive(true);
            _field.text = "";
            _field.ActivateInputField();
        }

        private void Close()
        {
            if (_field != null)
            {
                _field.text = "";
                _field.DeactivateInputField();
            }

            if (_inMatch && _fieldRow != null) _fieldRow.SetActive(false);
        }

        private void Submit(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                bool sent = MatchRpc.Instance != null && MatchRpc.Instance.SendChatServerRpc(text);

                // ⚠️ AN UNSENT LINE SAYS SO ON THE LOG RATHER THAN VANISHING. `IsListening` is
                // true from `StartClient` and not from approval, so a line typed during the join
                // window goes to a transport with nowhere to send it; `DeclareReadyServerRpc`
                // carries the same note and the same fix, because a press that silently did
                // nothing is what § 53.5 cost.
                if (!sent) AddLocal("Not connected. That line was not sent.");
            }

            if (_inMatch)
            {
                Close();
                return;
            }

            // In the lobby the field stays open and ready for the next line.
            if (_field == null) return;

            _field.text = "";
            _field.ActivateInputField();
        }

        /// <summary>
        /// ⚠️⚠️ THE RECEIPT IS LOGGED, AND WITHOUT IT THE TWO-PROCESS RUN COULD NOT ANSWER THE
        /// QUESTION IT EXISTS FOR. 🧑 2026-08-28: *"does say something even work? can u even chat
        /// with people?"*. The send side already prints `[LobbyAuto] chat ... sent=True`, which
        /// only proves a message reached the transport; the receiving end drew a label and said
        /// nothing, so a run where the host relayed nothing and a run where it relayed correctly
        /// produced identical logs on both machines. This is the same argument
        /// `ConvertedScreen.WireOne` makes for logging every menu press: in a shipped .exe a line
        /// in `Player.log` is the only way to tell "it never arrived" from "it arrived and the
        /// panel did not draw it".
        /// </summary>
        private void Add(string who, string what)
        {
            Debug.Log($"[Chat] received from '{who}': {what}");
            Push($"{who}:  {what}");
        }

        private void AddLocal(string note) => Push(note);

        /// <summary>
        /// ⚠️ THE LOG SCROLLS BY REWRITING SIX LABELS, NOT BY CREATING ONE PER LINE. Creating and
        /// destroying a `Text` per message allocates for the whole match and leaves the layout
        /// group rebuilding on every line, and `docs/TODO.md` § 52.3 measured what a HUD string
        /// rebuilt per frame cost the probe: an eighth of its frames.
        /// </summary>
        private void Push(string line)
        {
            _history.Add(line);
            if (_history.Count > MaxHistory) _history.RemoveAt(0);

            if (!_inMatch)
            {
                SetLines();
                RefreshHistoryPanel();

                // ⚠️ A LINE ARRIVING WHILE THE LOG IS OPEN MOVES THE BOTTOM OF IT, so the log
                // has to follow. Without this the panel stays on the line that was newest when
                // it was opened and every message after that arrives off screen, which is the
                // same complaint the snap was written for (*"u dont see most recent chats"*)
                // reached from the other direction.
                if (_historyPanel != null && _historyPanel.activeSelf) _snapPending = true;
                return;
            }

            for (int i = 0; i < _lines.Count - 1; i++)
            {
                _lines[i].text = _lines[i + 1].text;
                _stamps[i] = _stamps[i + 1];
            }

            _lines[_lines.Count - 1].text = line;
            _stamps[_stamps.Count - 1] = Time.unscaledTime;

            SetLines();
        }

        private void SetLines()
        {
            if (!_inMatch)
            {
                int first = Mathf.Max(0, _history.Count - LobbyVisibleLines);
                int target = MaxLines - Mathf.Min(LobbyVisibleLines, _history.Count);

                for (int i = 0; i < _lines.Count; i++)
                {
                    _lines[i].text = i < target ? "" : _history[first + i - target];
                }
            }

            foreach (var line in _lines)
            {
                var element = line.GetComponent<LayoutElement>();

                // ⚠️⚠️ AN EMPTY SLOT IS DEACTIVATED, NOT SET TO ZERO HEIGHT, AND THE DIFFERENCE IS
                // THE PANEL'S OWN SPACING. 🧑 2026-08-28, of the lobby chat: *"big empty sapce here
                // for lobby and say something"*. Zeroing the height was the obvious fix and it
                // only did half the job: a `VerticalLayoutGroup` puts its `spacing` between every
                // pair of ACTIVE children whatever their heights are, so six zero-height rows
                // still contributed six 4 px gaps, and with 20 px of padding the empty panel was
                // 44 px of nothing above a 44 px field. A layout group skips an inactive child
                // entirely, gap included.
                //
                // ⚠️ AND THE FIT RUNS AFTER THE ACTIVATION, not before. `MenuKit.FitBlock` measures
                // `preferredHeight` against the rect, and a rect on a deactivated object has not
                // been through a layout pass: fitting first measures a line that is not there yet
                // and leaves the type wherever it started.
                // ⚠️ A ROW IS DRAWN ONLY IF IT HAS SOMETHING TO SAY. In the lobby that is the
                // last two lines and nothing else (see `LobbyVisibleLines`); the older ones are
                // in the scrollable log behind the click.
                bool has = !string.IsNullOrEmpty(line.text);

                if (line.gameObject.activeSelf != has) line.gameObject.SetActive(has);

                if (!has) continue;

                line.color = UiTheme.Cream;

                if (element != null)
                {
                    element.minHeight = LineHeight;
                    element.preferredHeight = LineHeight;
                }

                // ⚠⚠ FITTED TO THE HEIGHT THE ROW ACTUALLY HAS, AND `LineHeight * 2.0f` HERE IS
                // WHY THE STRIP WENT BLANK. 🧑 2026-08-30: *"Chat does not appear on preview after
                // 1 chat"*. `docs/TODO.md` § 79.3 named this mechanism as the standing suspect and
                // could not reproduce it at four short messages; a longer one reproduces it every
                // time.
                //
                // The two lines above pin this row's `LayoutElement` to ONE `LineHeight`, 26 px.
                // This call then allowed the type to be sized against a TWO-line cap, 52 px, so a
                // message that wrapped was declared to fit at a size that draws 52 px of text into
                // a 26 px rect. **Legacy `Text` on `Truncate` clips by WHOLE LINES**, and a rect
                // that cannot hold one whole line at the chosen size paints nothing at all — while
                // remaining active, non-empty and correctly sized, which is why every check that
                // read `Text.text` passed against his screenshot.
                //
                // Fitting to `LineHeight` shrinks the type until ONE line genuinely fits the rect
                // it is clipped to, so the two numbers finally describe the same box.
                //
                // ⚠ A LINE TOO LONG TO FIT AT `MenuKit.MinReadableUnits` IS STILL CLIPPED, and that
                // is the design rather than a shortfall: this strip shows `LobbyVisibleLines` of
                // the log by intent, and the whole message is in the scrollable history behind the
                // click, on `Overflow`, inside a viewport. What changed is that it now clips to
                // ONE READABLE LINE instead of to nothing.
                MenuKit.FitBlock(line, LineHeight);

                // ⚠️⚠️ AND IT IS CLIPPED TO WHAT IT WAS JUST FITTED TO. 🧑 2026-08-29: *"nag
                // ooverflow yung text sa lobby chat"*. `MenuKit.FitBlock` sets
                // `verticalOverflow = Overflow` and then SHRINKS the type until the block fits
                // the cap it was given, which works right up until the font hits
                // `MenuKit.MinReadableUnits` 18 and cannot go lower. `MatchRpc.MaxChatLength` is
                // 120 characters, and 120 characters at 18 units in a 560 px panel is three
                // lines against a two-line slot: the `LayoutElement` then claims 52 px while the
                // label DRAWS 78, and legacy `Text` on Overflow happily paints the remainder
                // outside the plate, over the START button underneath it.
                //
                // ⚠️ CLIPPED RATHER THAN GROWN, because the plate is a fixed size by design and
                // the lobby deliberately shows only `LobbyVisibleLines` of the log. Nothing is
                // lost: the whole line is in the scrollable history behind the click, which is
                // on `Overflow` inside a viewport and is the right place for it.
                line.verticalOverflow = VerticalWrapMode.Truncate;

                // ⚠️⚠️ AND THE STRING IS CUT WHEN THE TYPE CANNOT SHRINK ANY FURTHER, WHICH IS THE
                // OTHER HALF AND THE ONE THE FIT ALONE COULD NEVER DO. `LobbyChatStripProbe`
                // measured it: at `MatchRpc.MaxChatLength` = 120 characters the fit bottoms out at
                // `MenuKit.MinReadableUnits` 18 and the block still wants **43 px in a 26 px row**.
                // Legacy `Text` on `Truncate` clips by whole lines, so the row paints NOTHING
                // while staying active and non-empty. Shrinking further is forbidden by that
                // constant and growing the row is forbidden by 🧑's *"i want u to not make the
                // chat extend anymore"*, so the only remaining lever is the text.
                Ellipsise(line, LineHeight);
            }
        }

        /// <summary>
        /// Cut a line down until it fits the box it is clipped to, and say so with an ellipsis.
        ///
        /// ⚠️ NOTHING IS LOST AND THAT IS WHAT MAKES THE CUT HONEST. The whole message is in the
        /// scrollable log behind the click, on `Overflow`, inside a viewport, which is where a
        /// 120-character sentence belongs. This strip's job is *"you have new messages, here is
        /// roughly what they say"*.
        ///
        /// ⚠️ A BINARY SEARCH, NOT A CHARACTER-AT-A-TIME LOOP. `preferredHeight` re-runs the text
        /// generator on every read, and this runs on every arriving message with two rows live;
        /// 7 measurements instead of up to 120 is the difference between free and noticeable.
        ///
        /// ⚠️ AND IT MEASURES THE SAME PROPERTY THE PROBE ASSERTS ON, deliberately. A fit checked
        /// against a different number from the one the test reads is how this went green while
        /// his screen was blank.
        /// </summary>
        private static void Ellipsise(Text line, float room)
        {
            if (line == null || string.IsNullOrEmpty(line.text)) return;
            if (line.preferredHeight <= room + 0.5f) return;

            string full = line.text;

            // Low is a length known too long, high a length known to fit. 4 is short enough that
            // any row can hold it, and it is the floor the search cannot go under.
            int low = 4, high = full.Length;

            while (high - low > 1)
            {
                int mid = (low + high) / 2;
                line.text = full.Substring(0, mid) + "…";

                if (line.preferredHeight <= room + 0.5f) low = mid;
                else high = mid;
            }

            line.text = full.Substring(0, low) + "…";
        }

        /// <summary>
        /// The scrollable log behind the click.
        ///
        /// ⚠️⚠️ IT HANGS OFF THE ROOT CANVAS, NOT OFF THE CHAT PANEL'S PARENT. The chat lives in
        /// whatever container the lobby put it in, and a 780 x 560 box centred inside a bottom-left
        /// strip is centred on the strip, not on the screen. Parenting to the root canvas and
        /// giving it its own sorting canvas is the same rule `ConvertedMatchSetup` applies to the
        /// character picker: a page-sized overlay owns its own render order rather than borrowing
        /// the hierarchy's.
        ///
        /// ⚠️ AND IT SORTS BELOW THE CHARACTER PICKER (100) ON PURPOSE. Both are lobby overlays,
        /// and the picker is the one that must never be drawn through.
        /// </summary>
        /// <summary>
        /// How tall the chat stands while it is open, in reference units.
        ///
        /// ⚠️ IT IS A HEIGHT AND NOT A LINE COUNT, because the log is one wrapped `Text` inside a
        /// `ScrollRect` rather than a stack of rows. `MaxHistory` still bounds what is IN it.
        /// </summary>
        private const float ExpandedHeight = 460.0f;

        private void BuildHistoryPanel()
        {
            if (_rect == null) return;

            // ⚠⚠⚠ IT IS PARENTED TO THE CHAT AND GROWS UPWARD OUT OF IT, AND IT USED TO BE A
            // CENTRED 780 x 560 WINDOW OVER A DIMMED LOBBY. 🧑 2026-08-30: *"pls overhaul how chat
            // works instead of making a separate window just make the chat extend when u click it
            // anywhere and make it so that it doesnt make the shit above it move"*, and, in the
            // same breath, granting the permission the old design was built to avoid: *"its the
            // one thing im giving authorization to cover shit"*.
            //
            // The centred window came from a real constraint — the previous note recorded that a
            // box centred inside a bottom-left strip is centred on the STRIP, not on the screen,
            // so it was moved to the root canvas to get the middle of the frame. That is still
            // true and it is no longer what is wanted: the ask is for the control to grow where
            // it already is.
            //
            // ⚠⚠ THE CHAT'S OWN RECT NEVER CHANGES SIZE, WHICH IS THE SECOND HALF OF THE ASK AND
            // THE EASIEST TO GET WRONG. *"make it so that it doesnt make the shit above it move"*.
            // `PanelHeight` reads `_rect.rect.height` and `LobbyChrome.StackRight` positions the
            // LOBBY & SERVERS pill off that number every frame, so growing `_rect` would walk the
            // whole right-hand rail up the screen. This is a SEPARATE rect that overlaps upward
            // and leaves `_rect` alone, so the stack above it cannot notice.
            //
            // ⚠ AND THE DIMMING BACKDROP IS GONE WITH THE WINDOW. It existed so a click on empty
            // road would close a modal (*"it clsoes when u click out"*, 2026-08-28); an in-place
            // extension is not a modal, it does not take the screen, and a full-screen raycast
            // target over a lobby somebody is trying to click is a control that eats presses. The
            // three ways out are the same click that opened it, the CLOSE button and Escape.
            _historyPanel = new GameObject("LobbyChatLog");
            _historyPanel.transform.SetParent(_rect, false);

            var layer = _historyPanel.AddComponent<RectTransform>();

            // ⚠⚠ IT SITS ON THE CHAT'S TOP EDGE, NOT ON ITS BOTTOM ONE, AND THAT IS THE WHOLE
            // DIFFERENCE BETWEEN EXTENDING AND COVERING. Anchored to the bottom it would have
            // grown up THROUGH the two visible lines and the "Say something" field, so opening the
            // log to read a message would hide the box you answer it in. Anchored to the top, the
            // collapsed chat stays exactly where it was and the scrollback is added above it —
            // which is what "extend" means, and it is why the field is still reachable while the
            // log is open (`OnPointerClick` focuses it on the same press).
            layer.anchorMin = new Vector2(0.0f, 1.0f);
            layer.anchorMax = new Vector2(1.0f, 1.0f);
            layer.pivot = new Vector2(0.5f, 0.0f);
            layer.offsetMin = Vector2.zero;
            layer.offsetMax = new Vector2(0.0f, ExpandedHeight);

            // ⚠ ITS OWN SORTING ORDER, KEPT FROM THE OLD DESIGN. A nested `Canvas` with
            // `overrideSorting` is what lets a child draw over its own ancestors, which is exactly
            // the authorization he gave. ⚠ 90 SORTS BELOW THE CHARACTER PICKER (100) ON PURPOSE:
            // both are lobby overlays and the picker is the one that must never be drawn through.
            var ownCanvas = _historyPanel.AddComponent<Canvas>();
            ownCanvas.overrideSorting = true;
            ownCanvas.sortingOrder = 90;
            _historyPanel.AddComponent<GraphicRaycaster>();

            var box = new GameObject("Panel");
            box.transform.SetParent(_historyPanel.transform, false);
            var plate = box.AddComponent<Image>();
            plate.sprite = GodotTheme.WoodBox(UiTheme.WoodDeep, UiTheme.WoodEdge);
            plate.type = Image.Type.Sliced;
            plate.color = Color.white;

            // ⚠ IT EATS CLICKS. The panel is opaque wood over a lobby full of controls, and a
            // press that fell through to a seat row underneath it would seat the player from a
            // click they aimed at a chat message.
            plate.raycastTarget = true;

            // The plate fills the extension exactly; the layer above is what positions it.
            var panelRect = plate.rectTransform;
            MenuKit.Stretch(panelRect, 0.0f);

            var title = MenuKit.Label(box.transform, "LOBBY CHAT", 30, UiTheme.Cream,
                                      Vector2.zero, Vector2.zero, Vector2.zero,
                                      TextAnchor.MiddleLeft);
            var titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0.0f, 1.0f);
            titleRect.anchorMax = new Vector2(1.0f, 1.0f);
            titleRect.pivot = new Vector2(0.5f, 1.0f);
            titleRect.offsetMin = new Vector2(24.0f, -66.0f);
            titleRect.offsetMax = new Vector2(-170.0f, -14.0f);
            title.raycastTarget = false;

            var close = MenuKit.WoodButton(box.transform, "CLOSE", Vector2.one,
                                           Vector2.zero, new Vector2(132.0f, 46.0f),
                                           CloseHistory);
            var closeRect = close.transform as RectTransform;
            closeRect.pivot = Vector2.one;
            closeRect.anchoredPosition = new Vector2(-18.0f, -14.0f);

            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(box.transform, false);
            var viewportImage = viewportGo.AddComponent<Image>();
            viewportImage.sprite = GodotTheme.WoodBox(UiTheme.WoodDark, UiTheme.WoodEdge);
            viewportImage.type = Image.Type.Sliced;
            viewportImage.color = Color.white;
            var viewportRect = viewportImage.rectTransform;
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(22.0f, 22.0f);
            viewportRect.offsetMax = new Vector2(-22.0f, -78.0f);

            // ⚠️ `RectMask2D` AND NOT `Mask`. `Mask` needs a stencil pass and a graphic that is
            // itself the mask, which would have thrown away the wooden inset this viewport draws.
            viewportGo.AddComponent<RectMask2D>();

            // ⚠️⚠️ THE TEXT IS ON THE CONTENT OBJECT ITSELF, NOT A CHILD OF IT. A
            // `ContentSizeFitter` measures the `ILayoutElement`s on ITS OWN object, and an empty
            // `RectTransform` with a `Text` child reports a preferred height of zero: the content
            // stays 0 px tall, the `ScrollRect` sees nothing to scroll, and the log opens showing
            // one screenful with the scroll wheel dead. One object carries both.
            var contentGo = new GameObject("ChatHistory");
            contentGo.transform.SetParent(viewportGo.transform, false);

            _historyText = contentGo.AddComponent<Text>();
            _historyText.font = MenuKit.Font;
            _historyText.fontSize = 22;
            _historyText.color = UiTheme.Cream;
            _historyText.alignment = TextAnchor.UpperLeft;
            _historyText.alignByGeometry = true;
            _historyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _historyText.verticalOverflow = VerticalWrapMode.Overflow;
            _historyText.raycastTarget = false;
            _historyText.text = EmptyLog;

            var contentRect = _historyText.rectTransform;
            contentRect.anchorMin = new Vector2(0.0f, 1.0f);
            contentRect.anchorMax = new Vector2(1.0f, 1.0f);
            contentRect.pivot = new Vector2(0.5f, 1.0f);
            contentRect.offsetMin = new Vector2(18.0f, 0.0f);
            contentRect.offsetMax = new Vector2(-18.0f, 0.0f);

            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _historyScroll = box.AddComponent<ScrollRect>();
            _historyScroll.viewport = viewportRect;
            _historyScroll.content = contentRect;
            _historyScroll.horizontal = false;
            _historyScroll.vertical = true;
            _historyScroll.movementType = ScrollRect.MovementType.Clamped;
            _historyScroll.scrollSensitivity = 28.0f;

            _historyPanel.SetActive(false);
        }

        private void CloseHistory()
        {
            if (_historyPanel != null) _historyPanel.SetActive(false);
        }

        /// <summary>
        /// ⚠️ IT OPENS ON THE NEWEST LINE, WHICH IS `verticalNormalizedPosition` 0. A hundred-line
        /// log that opens at the top opens on whatever was said when the lobby filled, and the
        /// player has to drag to reach the message they clicked it for.
        ///
        /// ⚠️ AND `ForceUpdateCanvases` RUNS BETWEEN THE FILL AND THE SCROLL. The fitter has not
        /// re-measured the new text yet at that point, so the scroll would be normalised against
        /// the PREVIOUS content height and land short.
        /// </summary>
        private void OpenHistory()
        {
            if (_historyPanel == null) return;

            RefreshHistoryPanel();
            _historyPanel.SetActive(true);
            _historyPanel.transform.SetAsLastSibling();

            Canvas.ForceUpdateCanvases();
            SnapHistoryToNewest();

            // ⚠️⚠️ AND AGAIN NEXT FRAME, WHICH IS THE HALF THAT ACTUALLY LANDS ON A COLD OPEN.
            // See `SnapHistoryToNewest`: the rebuild it does cannot run on a canvas that is
            // inactive this frame, and `LayoutRebuilder` says so outright. § 83.6 shipped the
            // same pairing for `ConvertedScreen` under the same constraint and § 80.2 before it.
            _snapPending = true;
        }

        /// <summary>
        /// ⚠️ ONE FRAME LATE IS STILL WRONG, SO THIS IS A SECOND CHANCE AND NOT THE PLAN.
        /// `OpenHistory` snaps immediately and sets this; if that snap measured a rect the layout
        /// had not produced yet, the same call one frame later measures a real one.
        /// </summary>
        private bool _snapPending;

        private void LateUpdate()
        {
            if (!_snapPending) return;

            _snapPending = false;
            SnapHistoryToNewest();
        }

        /// <summary>
        /// Puts the log on its newest line, and puts a SHORT log flush against the top of the box.
        ///
        /// ⚠️⚠️ `verticalNormalizedPosition = 0` IS MEANINGLESS WHEN THE CONTENT IS SHORTER THAN
        /// THE VIEWPORT, AND THAT IS THE REPORTED OVERFLOW. 🧑 2026-08-29 with a screenshot of the
        /// open log: ten lines sitting against the top of the panel with the FIRST ONE CLIPPED in
        /// half by the header, and a third of the box empty underneath them.
        ///
        /// A `ScrollRect` normalises the scroll against `content.height - viewport.height`. When
        /// the content is the shorter of the two that difference is zero or negative, the
        /// normalised value divides by nothing meaningful, and Unity leaves the content's
        /// `anchoredPosition` wherever it last was — which here is pushed UP by however tall the
        /// log was the last time it was long enough to scroll. So the panel looks correct after a
        /// hundred messages and broken after ten, which is the reverse of what an overflow bug
        /// normally does and is why reading `LobbyVisibleLines` never explained it.
        ///
        /// ⚠️ THE CONTENT IS TOP-ANCHORED AND TOP-PIVOTED (see `BuildHistoryPanel`), so y = 0 is
        /// flush under the header. Writing it directly is the only thing that reaches this case;
        /// there is no normalised position that expresses "shorter than the box".
        /// </summary>
        private void SnapHistoryToNewest()
        {
            if (_historyScroll == null) return;

            var content = _historyScroll.content;
            var viewport = _historyScroll.viewport;
            if (content == null || viewport == null) return;

            // ⚠️⚠️ THE HEIGHT IS REBUILT BEFORE IT IS READ, AND NOT REBUILDING IT IS WHY THE
            // FIRST FIX DID NOT TAKE. 🧑 2026-08-30 sent the log open on THREE messages with the
            // first one clipped in half by the header and two thirds of the box empty below it,
            // which is the same picture that produced this method — after this method shipped.
            //
            // `OpenHistory` called `Canvas.ForceUpdateCanvases()` and treated that as enough.
            // It is not: it flushes the canvas batches, it does not run the layout system, so
            // `ContentSizeFitter` had not yet resized `content` for the text written one line
            // earlier. `content.rect.height` was therefore the height of whatever the log held
            // the LAST time it was measured. Three short lines read as tall, the short-content
            // branch below was skipped, and the meaningless normalised write ran instead — which
            // leaves `anchoredPosition` exactly where the previous, longer log had pushed it.
            //
            // ⚠️ THIS IS `ConvertedScreen.ForceLayoutFor`'S FAULT CLASS, FOUR SURFACES OVER, and
            // `ModelPreview.EnsureTexture`, § 80.2, § 83.6 and § 79.6 are the others. A rect has
            // no size until a layout pass has run on it, and the frame a panel is switched on is
            // always before that pass.
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            float overflow = content.rect.height - viewport.rect.height;

            // ⚠️⚠️ THE POSITION IS WRITTEN DIRECTLY IN BOTH DIRECTIONS NOW, and the normalised
            // path is gone rather than kept for the tall case. A `ScrollRect` normalises against
            // `content.height - viewport.height`, so 0 means "the bottom" only while that
            // difference is positive; the moment it is not, Unity divides by nothing meaningful
            // and leaves the content wherever it last was. Keeping one branch on a value that is
            // undefined either side of a boundary is how this came back. The content is
            // top-anchored and top-pivoted (`BuildHistoryPanel`), so y is simply how far it has
            // been pushed up out of the box: 0 is flush under the header, and `overflow` is the
            // newest line resting on the bottom edge.
            content.anchoredPosition = new Vector2(content.anchoredPosition.x,
                                                   Mathf.Max(0.0f, overflow));
        }

        private void RefreshHistoryPanel()
        {
            if (_historyText == null) return;

            _historyText.text = _history.Count == 0 ? EmptyLog : string.Join("\n", _history);
        }

        /// <summary>
        /// ⚠️ IT FADES ON THE UNSCALED CLOCK. A hitstop writes `Time.timeScale` and the pause
        /// overlay writes it to zero; a chat line frozen on screen because somebody paused is a
        /// HUD element that outlives its own rule.
        /// </summary>
        private void Fade()
        {
            float now = Time.unscaledTime;

            for (int i = 0; i < _lines.Count; i++)
            {
                if (string.IsNullOrEmpty(_lines[i].text)) continue;

                float age = now - _stamps[i];
                if (age < MatchLineLife) continue;

                float t = Mathf.Clamp01((age - MatchLineLife) / MatchFadeTime);

                var colour = UiTheme.Cream;
                _lines[i].color = new Color(colour.r, colour.g, colour.b, 1.0f - t);

                if (t >= 1.0f) _lines[i].text = "";
            }
        }
    }
}
