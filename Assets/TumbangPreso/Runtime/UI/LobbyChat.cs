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
    public sealed class LobbyChat : MonoBehaviour
    {
        /// <summary>How many lines the log keeps.</summary>
        public const int MaxLines = 6;

        /// <summary>Seconds a line stays at full strength in a match before it fades.</summary>
        public const float MatchLineLife = 9.0f;
        public const float MatchFadeTime = 1.4f;

        private const float PanelWidth = 560.0f;
        private const float LineHeight = 26.0f;
        private const float FieldHeight = 44.0f;
        private const int LineSize = 19;

        private bool _inMatch;
        private RectTransform _rect;
        private InputField _field;
        private GameObject _fieldRow;
        private readonly List<Text> _lines = new List<Text>();
        private readonly List<float> _stamps = new List<float>();

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
            rect.sizeDelta = new Vector2(PanelWidth,
                                         (LineHeight * MaxLines) + FieldHeight + 24.0f);

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
            var fitter = gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

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

        private static void Inset(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(14.0f, 0.0f);
            rt.offsetMax = new Vector2(-14.0f, 0.0f);
        }

        private void OnEnable() => MatchRpc.OnChatLine += Add;

        private void OnDisable()
        {
            MatchRpc.OnChatLine -= Add;

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

            if (!_inMatch) return;

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

        private void Add(string who, string what)
        {
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
            foreach (var line in _lines)
            {
                var element = line.GetComponent<LayoutElement>();

                // ⚠️ AN EMPTY SLOT TAKES NO ROOM. See the fitter in `Construct`: this is the half
                // that makes the panel collapse, and without it the fitter measures six reserved
                // rows and the box is full height with nothing in it.
                if (string.IsNullOrEmpty(line.text))
                {
                    if (element != null)
                    {
                        element.minHeight = 0.0f;
                        element.preferredHeight = 0.0f;
                    }

                    continue;
                }

                line.color = UiTheme.Cream;

                if (element != null) element.minHeight = LineHeight;

                MenuKit.FitBlock(line, LineHeight * 2.0f);
            }
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
