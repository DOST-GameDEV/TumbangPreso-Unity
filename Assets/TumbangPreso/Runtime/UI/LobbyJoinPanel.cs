using System;
using System.Collections.Generic;
using System.Linq;
using TumbangPreso.Net;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The four ways into somebody else's game, on the lobby instead of on a screen before it.
    ///
    /// ⚠️⚠️ THIS IS `ConvertedMultiplayerSetup` LIFTED, NOT REDESIGNED, AND THE DIFFERENCE
    /// MATTERS. That screen carries four fixes that each cost a session to find, and every one of
    /// them is transcribed here rather than re-derived:
    ///
    ///   * `Reason()`. Every failed start used to throw away the reason it had just worked out,
    ///     so a dead join code, a rate-limited lookup, a refused port and a machine with no
    ///     internet were one sentence on screen. 🧑 2026-08-28: *"it sometimes says failed to join
    ///     online host via relay ... sometimes i get it to work"*.
    ///   * `NetSession.SplitHostPort`. `docs/TODO.md` § 59.1: nothing anywhere parsed `host:port`,
    ///     so two machines could discover each other and could not join.
    ///   * The code path asks `ResolveCodeAsync` which KIND of lobby it is and branches on
    ///     `IsLan`, so one four-character code reaches a LAN host or a Relay host and the player
    ///     never has to know which they are reading out.
    ///   * `LastDisconnectReason` is read and cleared by the lobby itself now
    ///     (`ConvertedMatchSetup.HandleClientDisconnected`), because the screen that can act on a
    ///     refusal is finally the screen the refusal arrives on.
    ///
    /// ⚠️⚠️ IT DOES NOT NAVIGATE. `ConvertedMultiplayerSetup` finished every successful join with
    /// `SceneFlow.Go(MatchSetup)`; the player is ALREADY on that scene here, and reloading it
    /// would tear down the map preview's cached arenas, both render textures and the whole cast.
    /// `SceneFlow.Go`'s one-load-per-frame latch does not cover this: it is scoped to a single
    /// frame on purpose. The panel raises <see cref="Joined"/> and the lobby redraws in place.
    ///
    /// ⚠️ EVERY LABEL IS FITTED. A LAN row carries a host NAME typed on another machine and an
    /// online row carries a map name and a count; both are arbitrary width, and legacy `Text`
    /// either wraps them out of the row or draws them past it. See `MenuKit.Fit`.
    /// </summary>
    public sealed class LobbyJoinPanel : MonoBehaviour
    {
        /// <summary>How many rows each browser draws. Four is what the old screen drew and it is
        /// the number that fits the card without scrolling.</summary>
        private const int RowCount = 2;

        private const float CardWidth = 940.0f;
        private const float CardHeight = 520.0f;
        private const float RowHeight = 48.0f;
        private const float Pad = 22.0f;

        /// <summary>Raised after a join has actually connected. The lobby redraws; it does not
        /// reload. See the header.</summary>
        public event Action Joined;

        /// <summary>Lets the owning lobby suspend furniture that lives on a later canvas layer.</summary>
        public event Action Opened;
        public event Action Closed;

        /// <summary>Raised with a line for the lobby's own status label, so this panel never
        /// owns a second place that reports network failures.</summary>
        public event Action<string> Status;

        private NetSession _net;

        private InputField _entry;
        private Text _lanTitle;
        private Text _onlineTitle;

        private readonly List<Button> _lanRows = new List<Button>();
        private readonly List<Text> _lanRowLabels = new List<Text>();
        private readonly List<string> _lanAddresses = new List<string>();

        private readonly List<Button> _onlineRows = new List<Button>();
        private readonly List<Text> _onlineRowLabels = new List<Text>();
        private readonly List<string> _onlineCodes = new List<string>();

        private bool _busy;

        public bool IsOpen => gameObject.activeSelf;

        /// <summary>
        /// Builds the card under <paramref name="parent"/> and returns it closed.
        ///
        /// ⚠️ IT IS BUILT ONCE AND HIDDEN, NEVER REBUILT ON EACH OPEN. Rebuilding would drop the
        /// text the player had half-typed and would re-subscribe to both browsers every time.
        /// </summary>
        public static LobbyJoinPanel Build(Transform parent, NetSession net)
        {
            var go = new GameObject("LobbyJoinPanel");
            go.transform.SetParent(parent, false);

            var panel = go.AddComponent<LobbyJoinPanel>();
            panel._net = net;
            panel.Construct();
            go.SetActive(false);

            return panel;
        }

        private void Construct()
        {
            var scrim = gameObject.AddComponent<Image>();
            scrim.color = new Color(UiTheme.Ink.r, UiTheme.Ink.g, UiTheme.Ink.b, 0.68f);
            scrim.raycastTarget = true;
            MenuKit.Stretch(scrim.rectTransform, 0.0f);

            var cardGo = new GameObject("JoinCard");
            cardGo.transform.SetParent(transform, false);
            var card = cardGo.AddComponent<Image>();
            card.sprite = GodotTheme.WoodBox(UiTheme.WoodDeep, UiTheme.WoodEdge);
            card.type = Image.Type.Sliced;
            card.color = Color.white;
            MenuKit.Place(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero,
                          new Vector2(CardWidth, CardHeight));

            var column = new GameObject("Column");
            column.transform.SetParent(cardGo.transform, false);

            var layout = column.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8;
            layout.padding = new RectOffset((int)Pad, (int)Pad, (int)Pad, (int)Pad);
            layout.childControlWidth = true;

            // ⚠️ TRUE, SO `LayoutElement.preferredHeight` IS ACTUALLY READ. A `VerticalLayoutGroup`
            // with height control OFF leaves each child at its own `sizeDelta.y`, which is 0 for a
            // GameObject built from code: the section headings here were 0 px tall and only drew
            // at all because a legacy `Text` set to `Overflow` renders outside its rect. See
            // `LobbyChat.Construct`, where the same setting made an entire panel invisible.
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperCenter;
            MenuKit.Stretch(column.GetComponent<RectTransform>(), 0);

            Heading(column.transform, "JOIN A GAME", 34, UiTheme.Amber, 40);

            var hint = Heading(column.transform,
                               "Type a four-character code or an address, or pick a game below.",
                               18, UiTheme.CreamMuted, 24);
            hint.alignment = TextAnchor.MiddleLeft;

            BuildEntryRow(column.transform);

            _lanTitle = Heading(column.transform, "ON YOUR NETWORK", 20, UiTheme.Cream, 26);
            _lanTitle.alignment = TextAnchor.MiddleLeft;
            BuildRows(column.transform, _lanRows, _lanRowLabels, OnLanRowClicked,
                      "SEARCHING FOR LAN GAMES…");

            _onlineTitle = Heading(column.transform, "ONLINE", 20, UiTheme.Cream, 26);
            _onlineTitle.alignment = TextAnchor.MiddleLeft;
            BuildRows(column.transform, _onlineRows, _onlineRowLabels, OnOnlineRowClicked,
                      "CHECKING ONLINE SERVERS…");

            BuildFooter(column.transform);
        }

        private static Text Heading(Transform parent, string text, int size, Color colour, float height)
        {
            var holder = new GameObject("Line");
            holder.transform.SetParent(parent, false);

            var element = holder.AddComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;
            element.flexibleHeight = 0.0f;

            var label = MenuKit.Label(holder.transform, text, size, colour,
                                      Vector2.zero, Vector2.zero, Vector2.zero,
                                      TextAnchor.MiddleCenter);
            label.raycastTarget = false;
            MenuKit.Stretch(label.rectTransform, 0);

            return label;
        }

        private void BuildEntryRow(Transform parent)
        {
            var row = new GameObject("EntryRow");
            row.transform.SetParent(parent, false);

            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var element = row.AddComponent<LayoutElement>();
            element.minHeight = RowHeight;
            element.preferredHeight = RowHeight;
            element.flexibleHeight = 0.0f;

            // ---- the field ------------------------------------------------------------
            var fieldGo = new GameObject("JoinAddressEdit");
            fieldGo.transform.SetParent(row.transform, false);

            var fieldImg = fieldGo.AddComponent<Image>();
            fieldImg.sprite = GodotTheme.WoodBox(UiTheme.WoodDark, UiTheme.WoodEdge);
            fieldImg.type = Image.Type.Sliced;
            fieldImg.color = Color.white;

            var fieldElement = fieldGo.AddComponent<LayoutElement>();
            fieldElement.flexibleWidth = 1;
            fieldElement.minHeight = RowHeight;
            fieldElement.preferredHeight = RowHeight;
            fieldElement.flexibleHeight = 0.0f;

            var placeholder = MenuKit.Label(fieldGo.transform, "JOIN CODE OR IP ADDRESS", 20,
                                            UiTheme.CreamMuted, Vector2.zero, Vector2.zero,
                                            Vector2.zero, TextAnchor.MiddleLeft);
            placeholder.raycastTarget = false;
            Inset(placeholder.rectTransform);

            var typed = MenuKit.Label(fieldGo.transform, "", 20, UiTheme.Cream,
                                      Vector2.zero, Vector2.zero, Vector2.zero,
                                      TextAnchor.MiddleLeft);
            typed.raycastTarget = false;
            typed.supportRichText = false;
            Inset(typed.rectTransform);

            _entry = fieldGo.AddComponent<InputField>();
            _entry.textComponent = typed;
            _entry.placeholder = placeholder;
            _entry.targetGraphic = fieldImg;

            // ⚠️ A JOIN CODE IS FOUR CHARACTERS AND AN IPv4 ADDRESS WITH A PORT IS TWENTY-ONE.
            // The cap is generous enough for a hostname and finite so a paste cannot run the
            // label off the card. `LobbySession.JoinCodeAlphabet` is uppercase, and the code path
            // uppercases what it gets, so a player typing lowercase is not punished for it.
            _entry.characterLimit = 64;
            _entry.lineType = InputField.LineType.SingleLine;

            _entry.onSubmit.AddListener(_ => Join());

            // ---- the button -----------------------------------------------------------
            var join = MenuKit.WoodButton(row.transform, "JOIN", Vector2.zero, Vector2.zero,
                                          new Vector2(140.0f, RowHeight), Join,
                                          "WoodAmberButton");
            join.name = "JoinButton";

            var joinElement = join.gameObject.AddComponent<LayoutElement>();
            joinElement.preferredWidth = 140.0f;
            joinElement.minWidth = 140.0f;
            joinElement.minHeight = RowHeight;
            joinElement.preferredHeight = RowHeight;
            joinElement.flexibleHeight = 0.0f;
        }

        private void BuildRows(Transform parent, List<Button> rows, List<Text> labels,
                               Action<int> onClick, string firstPlaceholder)
        {
            for (int i = 0; i < RowCount; i++)
            {
                int index = i;

                var button = MenuKit.WoodButton(parent, "", Vector2.zero, Vector2.zero,
                                                new Vector2(0.0f, RowHeight), () => onClick(index));
                button.name = $"BrowserRow{rows.Count}";

                var element = button.gameObject.AddComponent<LayoutElement>();
                element.minHeight = RowHeight;
                element.preferredHeight = RowHeight;
                element.flexibleHeight = 0.0f;

                var label = button.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.alignment = TextAnchor.MiddleLeft;
                    label.fontSize = 20;
                    label.text = i == 0 ? firstPlaceholder : "AVAILABLE GAMES APPEAR HERE";
                    label.color = UiTheme.CreamMuted;
                    Inset(label.rectTransform);
                }

                button.interactable = false;

                rows.Add(button);
                labels.Add(label);
            }
        }

        private void BuildFooter(Transform parent)
        {
            var spacer = new GameObject("FooterSpacer");
            spacer.transform.SetParent(parent, false);
            var spacerElement = spacer.AddComponent<LayoutElement>();
            spacerElement.flexibleHeight = 1.0f;

            var row = new GameObject("FooterRow");
            row.transform.SetParent(parent, false);

            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var element = row.AddComponent<LayoutElement>();
            element.minHeight = RowHeight;
            element.preferredHeight = RowHeight;
            element.flexibleHeight = 0.0f;

            var close = MenuKit.WoodButton(row.transform, "BACK TO LOBBY", Vector2.zero,
                                           Vector2.zero, new Vector2(0.0f, RowHeight), Close);
            close.name = "CloseJoinButton";
            var closeElement = close.gameObject.AddComponent<LayoutElement>();
            closeElement.minHeight = RowHeight;
            closeElement.preferredHeight = RowHeight;
            closeElement.flexibleHeight = 0.0f;

            // ⚠️⚠️ AND A WAY OUT OF SOMEBODY ELSE'S GAME THAT IS NOT JOINING A THIRD ONE.
            // 🧑 2026-08-29: *"make it possible to leave someones server too even tho u wont join
            // another one bcz rn the option for servere is Join and leave (u have to join a new
            // server to leave) (what if i want to host on my lan)?"*.
            //
            // This card offered JOIN and BACK TO LOBBY, and BACK only closes the card. The only
            // things that actually ended a session were BACK on the lobby screen, which also
            // leaves the screen, and joining somewhere else — **so the process stayed connected
            // to a lobby the player had finished with, and a process that is still a client
            // cannot become a host.** That is why he could not host on his own LAN without
            // restarting the game.
            //
            // ⚠️ IT IS HIDDEN WHEN THERE IS NOTHING TO LEAVE, not greyed. An offline player has
            // no session, and a dead control on a card with two live ones reads as a fault; the
            // three-line refusal this file gives for a greyed browser row is about a row that is
            // always there. `Refresh` sets it every time the card is drawn.
            _leave = MenuKit.WoodButton(row.transform, "LEAVE GAME", Vector2.zero,
                                        Vector2.zero, new Vector2(0.0f, RowHeight), Leave,
                                        "WoodDangerButton");
            _leave.name = "LeaveGameButton";
            var leaveElement = _leave.gameObject.AddComponent<LayoutElement>();
            leaveElement.minHeight = RowHeight;
            leaveElement.preferredHeight = RowHeight;
            leaveElement.flexibleHeight = 0.0f;
            _leave.gameObject.SetActive(false);
        }

        /// <summary>The LEAVE GAME control. Only on screen while there is a session to end.</summary>
        private Button _leave;

        /// <summary>
        /// End this peer's session and stay exactly where we are.
        ///
        /// ⚠️⚠️ IT DOES NOT NAVIGATE, AND THAT IS THE WHOLE DIFFERENCE FROM THE LOBBY'S BACK
        /// BUTTON. Back calls the same `Stop` and then leaves for the main menu; the ask was for
        /// the player to end up **in their own lobby, able to press HOST**, which is what this
        /// does. `NetSession.Stop` resets the peer table, the leader, `MatchInProgress` and the
        /// relay fields, so what is left is the same clean state a freshly opened lobby has.
        ///
        /// ⚠️ IT WORKS FOR A HOST TOO, AND ON A HOST IT IS THE ONLY ORDERLY WAY TO CLOSE A
        /// ROOM FROM THIS SCREEN. `Stop` now sends every peer a real `DisconnectClient` carrying
        /// `NetSession.HostLeftMessage`, so the other three are told rather than left to a
        /// silence timer.
        ///
        /// ⚠️ THE CARD CLOSES AFTERWARDS. Leaving is finished business, and a browser still
        /// listing the game you have just left is an invitation to walk straight back into it by
        /// accident.
        /// </summary>
        private void Leave()
        {
            if (_net == null || !_net.IsNetworked)
            {
                Report("You are not in anybody's game.");
                return;
            }

            bool wasHost = NetAuthority.IsHost;
            _net.Stop();

            Report(wasHost ? "Room closed. You can host again or join somebody."
                           : "Left the game. You can host now, or join somebody else.");

            Refresh();
            Close();

            // ⚠️ THE LOBBY IS TOLD, THROUGH THE EVENT IT ALREADY LISTENS TO. `Joined` is the
            // "something about this session changed, redraw yourself" signal
            // `ConvertedMatchSetup` subscribes to; raising it here is what turns the seats, the
            // nameplates and the START/READY button back into a lobby of one. Adding a second
            // event for the opposite direction would be a second thing to keep in step.
            Joined?.Invoke();
        }

        /// <summary>
        /// ⚠️ THE TEXT SITS INSIDE THE BOX RATHER THAN ON TOP OF IT. Stretching a label to its
        /// parent with no inset puts the first glyph hard against the wooden border, which reads
        /// as a rendering fault rather than as a margin somebody forgot.
        /// </summary>
        private static void Inset(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(18.0f, 0.0f);
            rt.offsetMax = new Vector2(-18.0f, 0.0f);
        }

        // ------------------------------------------------------------------------------

        public void Open()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            if (_net != null)
            {
                _net.BrowseLan();
                _net.Query?.StartBrowsing();
            }

            Refresh();
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            Opened?.Invoke();
            if (_net == null) return;

            if (_net.Beacon != null) _net.Beacon.EntriesChanged += Refresh;
            if (_net.Query != null) _net.Query.ServersChanged += Refresh;
        }

        private void OnDisable()
        {
            Closed?.Invoke();
            if (_net == null) return;

            if (_net.Beacon != null) _net.Beacon.EntriesChanged -= Refresh;
            if (_net.Query != null) _net.Query.ServersChanged -= Refresh;
        }

        /// <summary>
        /// ⚠️⚠️ THE ROWS ARE REDRAWN, NEVER REBUILT. `EntriesChanged` fires on every beacon
        /// packet, which on a busy network is several a second; destroying and recreating eight
        /// wood buttons at that rate would allocate through the whole time the panel is open, and
        /// `docs/TODO.md` § 52.3 is the entry about a HUD string doing exactly this.
        /// </summary>
        private void Refresh()
        {
            // ⚠️ THE LEAVE CONTROL APPEARS ONLY WHEN THERE IS SOMETHING TO LEAVE. See `Leave`:
            // an offline player has no session, and this card is opened from the lobby in both
            // states.
            if (_leave != null) _leave.gameObject.SetActive(_net != null && _net.IsNetworked);

            var lan = _net?.Beacon?.SortedEntries ?? new List<LanEntry>();

            _lanTitle.text = lan.Count > 0
                ? $"ON YOUR NETWORK  ·  {lan.Count}"
                : "ON YOUR NETWORK  ·  searching...";

            _lanAddresses.Clear();

            for (int i = 0; i < _lanRows.Count; i++)
            {
                if (i >= lan.Count)
                {
                    if (i == 0)
                        DrawEmpty(_lanRows[i], _lanRowLabels[i], "SEARCHING FOR LAN GAMES…");
                    else
                        _lanRows[i].gameObject.SetActive(false);
                    continue;
                }

                var entry = lan[i];
                string address = $"{entry.Address}:{entry.Port}";
                _lanAddresses.Add(address);

                string state = entry.InProgress ? "IN A MATCH" : "IN THE LOBBY";
                Draw(_lanRows[i], _lanRowLabels[i],
                     $"{entry.HostName}   ·   {entry.Players}/{entry.MaxPlayers}   ·   {state}   ·   {address}");
            }

            // ⚠️⚠️ OUR OWN LOBBY IS NOT A GAME WE CAN JOIN. 🧑 2026-08-29:
            // *"na kikita sarili sa lobby (join a game)"*. The LAN half is filtered at the source
            // by `LanBeacon.IsOurOwn`, which can compare a per-process id; the ONLINE half has no
            // such id on the wire, but it does not need one, because a relay lobby IS its join
            // code. If we are hosting and a listed code is the code we are advertising, that row
            // is this process.
            //
            // ⚠️ THE HOST GATE IS PART OF THE TEST, not a shortcut past it. A CLIENT holds the
            // host's join code in the same field once it has joined, and filtering on the code
            // alone would then hide the lobby that client is sitting in from its own browser
            // while it is still perfectly able to see everybody else's.
            string ownCode = _net != null && _net.IsNetworked && _net.IsHost
                ? (_net.Lobby?.JoinCode ?? "")
                : "";

            var online = (_net?.Query?.Servers ?? Enumerable.Empty<ServerQuery.Entry>())
                .Where(e => string.IsNullOrEmpty(ownCode)
                            || !string.Equals(e.JoinCode, ownCode, StringComparison.OrdinalIgnoreCase))
                .ToList();

            _onlineTitle.text = online.Count > 0
                ? $"ONLINE  ·  {online.Count}"
                : "ONLINE  ·  no answer yet";

            _onlineCodes.Clear();

            for (int i = 0; i < _onlineRows.Count; i++)
            {
                if (i >= online.Count)
                {
                    if (i == 0)
                        DrawEmpty(_onlineRows[i], _onlineRowLabels[i], "CHECKING ONLINE SERVERS…");
                    else
                        _onlineRows[i].gameObject.SetActive(false);
                    continue;
                }

                var entry = online[i];
                string code = !string.IsNullOrEmpty(entry.JoinCode) ? entry.JoinCode : entry.Name;
                _onlineCodes.Add(code);

                string state = entry.InProgress ? "IN A MATCH" : "IN THE LOBBY";
                string map = string.IsNullOrEmpty(entry.Name) ? "ESKINITA" : entry.Name.ToUpperInvariant();

                Draw(_onlineRows[i], _onlineRowLabels[i],
                     $"{code}   ·   {entry.Players}/{entry.Capacity}   ·   {map}   ·   {state}");
            }
        }

        /// <summary>
        /// ⚠️ THE ROW IS SHOWN BEFORE IT IS MEASURED. `preferredWidth` on a label inside a
        /// deactivated object is meaningless, and `MenuKit.Fit` would then leave a host name typed
        /// on another machine running off the side of the card.
        /// </summary>
        private static void Draw(Button row, Text label, string text)
        {
            row.gameObject.SetActive(true);
            row.interactable = true;

            if (label == null) return;

            label.text = text;
            label.color = UiTheme.Cream;
            label.fontSize = 20;

            float room = label.rectTransform.rect.width;
            if (room <= 1.0f) room = CardWidth - (Pad * 2.0f) - 36.0f;

            MenuKit.Fit(label, room);
        }

        private static void DrawEmpty(Button row, Text label, string text)
        {
            Draw(row, label, text);
            row.interactable = false;
            if (label != null) label.color = UiTheme.CreamMuted;
        }

        // ------------------------------------------------------------------------------

        private void OnLanRowClicked(int index)
        {
            if (index < 0 || index >= _lanAddresses.Count) return;

            MenuSfx.Click();
            if (_entry != null) _entry.text = _lanAddresses[index];
            Report($"Picked {_lanAddresses[index]}. Press JOIN.");
        }

        private void OnOnlineRowClicked(int index)
        {
            if (index < 0 || index >= _onlineCodes.Count) return;

            MenuSfx.Click();
            if (_entry != null) _entry.text = _onlineCodes[index];
            Report($"Picked code {_onlineCodes[index]}. Press JOIN.");
        }

        /// <summary>
        /// Pairs the headline the player understands with the detail the session already worked
        /// out. Transcribed from `ConvertedMultiplayerSetup.Reason`; its header has what throwing
        /// the detail away cost.
        /// </summary>
        private string Reason(string headline)
        {
            string detail = _net != null ? _net.Status : null;
            return string.IsNullOrWhiteSpace(detail) ? headline : $"{headline}  ({detail})";
        }

        private void Report(string message)
        {
            Status?.Invoke(message);
        }

        /// <summary>
        /// ⚠️⚠️ JOINING FROM HERE STOPS A HOST THAT IS ALREADY RUNNING, WHICH IS THE PATH
        /// `docs/TODO.md` § 65.1 WAS WRITTEN FOR. The lobby auto-hosts on arrival, so by the time
        /// anybody presses JOIN this process is listening, and `NetworkManager.Shutdown()` does
        /// not shut anything down: `CanStart` refuses outright while `IsListening` is still true.
        /// Every start path in `NetSession` opens with `EnsureStoppedAsync` for exactly this, and
        /// that is why nothing here calls `Stop()` first. Calling it would be a second shutdown
        /// racing the one the start is already waiting on.
        ///
        /// ⚠️ AND THE BUTTON IS LATCHED FOR THE DURATION. Two presses during a handshake are two
        /// transports being started over one another, and the second one wins in a way nothing
        /// downstream expects.
        /// </summary>
        private async void Join()
        {
            if (_busy) return;

            string typed = _entry == null || string.IsNullOrWhiteSpace(_entry.text)
                ? ""
                : _entry.text.Trim();

            if (string.IsNullOrEmpty(typed))
            {
                Report("Enter a four-character join code or an IP address.");
                return;
            }

            _busy = true;

            try
            {
                bool joined = await Connect(typed);
                if (this == null) return;

                if (!joined) return;

                Close();
                Joined?.Invoke();
            }
            finally
            {
                _busy = false;
            }
        }

        /// <summary>
        /// The join path, driven by a command line instead of by a finger. See
        /// `ConvertedMatchSetup.DriveAutomation`.
        ///
        /// ⚠️ IT GOES THROUGH `Connect` AND RAISES `Joined`, exactly as a press does, so an
        /// automated run and a human run take the same code. A test that reaches past the panel
        /// into `NetSession` would prove the transport works and say nothing about whether the
        /// button does.
        /// </summary>
        public async System.Threading.Tasks.Task<bool> AutomationJoin(string typed)
        {
            if (_entry != null) _entry.text = typed;

            bool joined = await Connect(typed);

            if (this == null) return false;

            if (!joined) return false;

            Close();
            Joined?.Invoke();
            return true;
        }

        private async System.Threading.Tasks.Task<bool> Connect(string typed)
        {
            // ⚠️ AN ADDRESS IS ANYTHING THAT LOOKS LIKE ONE. A join code is four characters out
            // of `LobbySession.JoinCodeAlphabet`, which has no dot and no colon in it, so this
            // test cannot swallow a code. Transcribed from the old screen.
            bool looksLikeAddress = typed.Contains(".") || typed.Contains(":") ||
                                    typed.Equals("localhost", StringComparison.OrdinalIgnoreCase);

            if (looksLikeAddress)
            {
                Report($"Connecting to {typed}...");

                if (await _net.StartClientAsync(typed)) return true;

                Report(Reason($"Could not reach {typed}."));
                return false;
            }

            if (typed.Length < LobbySession.JoinCodeLength)
            {
                Report($"A join code is {LobbySession.JoinCodeLength} characters.");
                return false;
            }

            string code = typed.ToUpperInvariant();
            Report($"Looking up {code}...");

            var resolved = await _net.Query.ResolveCodeAsync(code);

            if (this == null) return false;

            if (!resolved.HasValue || !resolved.Value.Found)
            {
                Report($"No game answered to '{code}'.");
                return false;
            }

            var match = resolved.Value;

            // ⚠️ THE CODE IS REMEMBERED BEFORE THE CONNECT, not after. A joiner that lands in the
            // lobby has to be able to read the same code back out to a third player, and the
            // lobby draws it from `LobbySession.JoinCode`.
            _net.Lobby.SetJoinCode(code);

            if (match.IsLan)
            {
                Report($"Joining {match.HostName} at {match.Address}:{match.Port}...");

                if (await _net.StartClientAsync(match.Address, match.Port)) return true;

                Report(Reason($"Could not reach {match.HostName} at {match.Address}:{match.Port}."));
                return false;
            }

            Report($"Joining {match.HostName} online...");

            if (await _net.StartRelayClient(match.RelayCode)) return true;

            // ⚠️ `docs/TODO.md` § 65.4 IS OPEN AND THIS IS WHERE IT SURFACES: the online browser
            // can offer a lobby whose Relay allocation is already gone. Moving the browser onto
            // the lobby does not fix that and must not hide it, so the reason reaches the player
            // rather than being swallowed into a generic failure.
            Report(Reason("Could not join that online game. Its session may have ended."));
            return false;
        }
    }
}
