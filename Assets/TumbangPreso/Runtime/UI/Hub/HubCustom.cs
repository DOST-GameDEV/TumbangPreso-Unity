using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// HOST GAME, in the room's knit (owner 2026-10-28). It asks only what cannot change once the
    /// room exists: its name, LAN or ONLINE, and who can join. MAP and MODE are chosen in the room,
    /// with the room's own selector; the room opens on the last ones played (`SceneFlow`).
    ///
    /// THE FOUR ANSWERS:
    ///   the one thing   CREATE ROOM
    ///   first press     CREATE ROOM works at once: the name is pre-filled, LAN and PUBLIC chosen
    ///   not needed now  map, mode and rules, which belong to the room
    ///   out             BACK or Escape to GAMEMODE SELECT
    /// </summary>
    public sealed class HubHost : HubScreen
    {
        public override bool WearsBoard => true;
        public override float CourtShade => 1f;

        private InputField _name;
        private HubButton _lan, _online, _create;
        private HubButton[] _visibility;
        private int _visibilityValue;
        private bool _onlineChosen;
        private Text _status;
        private bool _busy;

        private static readonly string[] Visibility = { "PUBLIC", "FRIENDS ONLY", "PRIVATE" };

        public override Selectable FirstFocus => _create;

        public override void Build()
        {
            HubKnit.Back(Root, Hub);
            HubKnit.Title(Root, "HOST GAME", "CUSTOM", out _);

            string me = GameServices.Account?.DisplayName;
            if (string.IsNullOrWhiteSpace(me)) me = Settings.SettingsStore.Current.PlayerName;
            if (string.IsNullOrWhiteSpace(me)) me = "PLAYER";

            var plate = HubKnit.At(HubKnit.Plate(Root, "HostPanel", Knit.Panel), 360, 200, 1200, 674);
            var panel = HubKnit.FaceOf(plate);
            HubKnit.Head(panel, "NameCaption", "ROOM NAME", 60, 52);
            _name = HubKnit.Field(panel, "LobbyName", (me + "'s room").ToUpperInvariant(), "Name your room",
                                  Settings.GameSettings.RoomTitleMax, 40);
            HubKnit.In((RectTransform)_name.transform, 60, 80, 1080, 96);

            HubKnit.Head(panel, "NetworkCaption", "WHERE ARE YOUR FRIENDS?", 60, 222);
            _lan = Door(panel, "LanChoice", "LAN", "Same Wi-Fi, no internet", 60, () => Network(false));
            _online = Door(panel, "OnlineChoice", "ONLINE", "Anywhere, by code", 620, () => Network(true));

            HubKnit.Head(panel, "VisibilityCaption", "WHO CAN JOIN", 60, 502);
            _visibility = HubKnit.Tabs(panel, "Visibility", 60, 530, 1080, 104,
                                       new[] { "Visibility0", "Visibility1", "Visibility2" }, Visibility, Visible);

            _status = HubKnit.Words(Root, "Status", "", HubStyle.Floor, HubKnit.KitWhite, TextAnchor.MiddleCenter, true, 3);
            HubKnit.At(_status.rectTransform, 360, 874, 1200, 36);

            _create = HubKnit.Button(Root, "CreateLobby", "CREATE ROOM", Knit.Go, Create, 56, HubGlyph.Mark.None, primary: true);
            HubKnit.At((RectTransform)_create.transform, 690, 908, 540, 128);

            Network(false);
            Visible(0);
        }

        /// <summary>A big berry door: its name, and what it means in one line.</summary>
        private HubButton Door(RectTransform panel, string name, string title, string line, float x, System.Action pick)
        {
            var door = HubKnit.Button(panel, name, null, Knit.Card, pick);
            HubKnit.In((RectTransform)door.transform, x, 250, 520, 204);
            var big = HubKnit.Display(door.Body, "Label", title, 70, HubKnit.KitCream, TextAnchor.MiddleCenter);
            HubKnit.In(big.rectTransform, 0, 26, 520, 88);
            var small = HubKnit.Words(door.Body, "Line", line, 30, HubKnit.KitTan, TextAnchor.MiddleCenter);
            HubKnit.In(small.rectTransform, 0, 118, 520, 44);
            return door;
        }

        private void Network(bool online)
        {
            _onlineChosen = online;
            // Lime means chosen, on the rim and on the door's name.
            HubKnit.Recolour(_lan.transform, online ? Knit.Card : Knit.CardOn);
            HubKnit.Recolour(_online.transform, online ? Knit.CardOn : Knit.Card);
            HubKit.LabelOf(_lan).color = online ? HubKnit.KitCream : HubKnit.KitLime;
            HubKit.LabelOf(_online).color = online ? HubKnit.KitLime : HubKnit.KitCream;
            // The doors already say what each means; the line is kept for opening and refusals.
            _status.text = "";
        }

        private void Visible(int index)
        {
            _visibilityValue = index;
            HubKnit.ChooseTab(_visibility, index);
        }

        private async void Create()
        {
            if (_busy) return;
            _busy = true;
            _create.interactable = false;
            HubKnit.Say(_create, "OPENING...");
            _status.text = _onlineChosen ? "Opening an online room..." : "Opening a room on your network...";
            string title = string.IsNullOrWhiteSpace(_name.text) ? _name.placeholder.GetComponent<Text>().text : _name.text.Trim();
            // ⚠️ MAP AND MODE ARE THE ROOM'S, so the room opens on the last ones chosen and the host
            // changes them there (owner 2026-10-28: no choosing the same thing on two screens).
            string refusal = await Hub.Host.HostRoom(title, SceneFlow.SelectedMap, SceneFlow.SelectedMode,
                                                     (RoomVisibility)_visibilityValue, _onlineChosen);
            if (this == null || !_busy) return;
            _busy = false;
            _create.interactable = true;
            HubKnit.Say(_create, "CREATE ROOM");
            if (!string.IsNullOrEmpty(refusal)) { _status.text = refusal; MenuSfx.Error(); return; }
            Hub.ShowLobby();
        }

        public override bool Back()
        {
            if (_busy) { _busy = false; Hub.Host.LeaveRoom(); }
            return false;
        }
    }

    /// <summary>
    /// JOIN GAME, in the room's knit (owner 2026-10-28: "merge the online and LAN sources into one
    /// list, with the code entry in that same place"). The code and JOIN sit on top; under them one
    /// list of every room this player can see, LAN and online together, each tagged with where it
    /// is. No SOURCE to choose first.
    ///
    /// ⚠️ EVERY JOIN GOES THROUGH `Hub.Host.Join`, the path the old panel pressed, so codes,
    /// addresses, LAN rows and online rows keep every guard they had.
    /// </summary>
    public sealed class HubJoin : HubScreen
    {
        public override bool WearsBoard => true;
        public override float CourtShade => 1f;
        public string JoinCodeOnOpen { get; set; }
        private RectTransform _list;
        private Text _status, _empty;
        private RectTransform _emptyCan;
        private InputField _codeField;
        private HubButton _refresh, _joinByCode;
        private float _refreshUntil = float.NegativeInfinity;
        private bool _refreshShown;
        private bool _busy;
        private float _nextDraw;
        private const float Column = 1200, RowHeight = 142, RowPitch = 150;

        /// <summary>
        /// How long REFRESH reads as busy and ignores presses. A LAN host answers the query within a
        /// frame or two and broadcasts every second; the online list asks UGS at once. The hold is
        /// what stops a held button or a mashed pad A from becoming a query per frame.
        /// </summary>
        public const float RefreshHoldSeconds = 1.5f;

        /// <summary>The list while a REFRESH has emptied it and the answers are on their way.</summary>
        public const string LanRefreshingMessage = "Looking for rooms on your network...";

        /// <summary>The row's tag where JOIN would be, for a host on another protocol (beacon v3).</summary>
        public const string DifferentVersionLabel = "DIFFERENT\nVERSION";
        private const int VisibleRooms = 3;
        private readonly List<HubRoom> _drawnRooms = new List<HubRoom>(VisibleRooms);
        private readonly List<HubRoom> _rooms = new List<HubRoom>();
        private readonly List<bool> _roomLan = new List<bool>();
        private string _drawnEmptyMessage;
        private bool _drawnOnce;

        public override Selectable FirstFocus => _codeField;

        public override void Build()
        {
            HubKnit.Back(Root, Hub);
            HubKnit.Title(Root, "JOIN GAME", "CUSTOM", out _);
            var plate = HubKnit.At(HubKnit.Plate(Root, "ListBoard", Knit.Panel), 360, 200, 1200, 774);
            var panel = HubKnit.FaceOf(plate);

            HubKnit.Head(panel, "CodeCaption", "GOT A CODE?", 60, 52);
            _codeField = HubKnit.Field(panel, "CodeField", "", "ABCD or 192.168.1.20", 21, 44);
            HubKnit.In((RectTransform)_codeField.transform, 60, 80, 790, 100);
            _codeField.onValidateInput += (text, index, c) => char.ToUpperInvariant(c);
            _codeField.onSubmit.AddListener(code => { if (!string.IsNullOrWhiteSpace(code)) Join(code); });
            _joinByCode = HubKnit.Button(panel, "JoinByCode", "JOIN", Knit.Go, () => Join(_codeField.text), 50, HubGlyph.Mark.None, primary: true);
            HubKnit.In((RectTransform)_joinByCode.transform, 880, 80, 260, 100);

            HubKnit.Head(panel, "Heading", "OPEN ROOMS", 60, 252);
            _refresh = HubKnit.Button(panel, "RefreshServers", "REFRESH", Knit.Plain, RefreshServers, 32);
            HubKnit.In((RectTransform)_refresh.transform, 920, 214, 220, 76);

            _list = HubKit.Rect(panel, "List");
            HubKnit.In(_list, 60, 306, 1080, RowPitch * VisibleRooms);
            // ⚠️ ONE STATE LINE, IN THE READING FACE (2026-09-23 UI review): never "Looking for ..." and
            // "Nobody yet" at once.
            _empty = HubKnit.Words(_list, "EmptyState", "", 34, HubKnit.KitInk, TextAnchor.MiddleCenter);
            HubKit.Stretch(_empty.rectTransform, 40);
            // ⚠️ AN EMPTY LIST IS A LATA STANDING ALONE IN ITS CIRCLE (2026-09-24): the game's own
            // picture of "nobody is playing yet", above the sentence, gone once rooms appear.
            _emptyCan = HubKit.Rect(_empty.transform, "EmptyCan");
            HubKit.Place(_emptyCan, HubKit.Centre, new Vector2(0, 110), new Vector2(130, 130));
            var ring = HubScenery.ChalkRing(_emptyCan, "Circle", 0.2f);
            HubKit.Place(ring.rectTransform, HubKit.Bottom, new Vector2(0, -6), new Vector2(210, 60));
            ring.color = new Color(HubKnit.KitTanDark.r, HubKnit.KitTanDark.g, HubKnit.KitTanDark.b, 0.6f);
            var can = HubKit.Glyph(_emptyCan, "Can", HubGlyph.Mark.Can, new Color(HubKnit.KitTanDark.r, HubKnit.KitTanDark.g, HubKnit.KitTanDark.b, 0.8f), 0.07f);
            HubKit.Stretch(can.rectTransform, 12);

            _status = HubKnit.Words(Root, "Status", "", HubStyle.Body, HubKnit.KitWhite, TextAnchor.MiddleCenter, true, 3);
            HubKnit.At(_status.rectTransform, 360, 990, 1200, 50);

            HubSlap.On(_list, 0.04f, 1);
            // Both lists, from the start: LAN and online rooms arrive in the same list.
            Hub.Host.Browse();
            ShowRefresh();
            if (!string.IsNullOrWhiteSpace(JoinCodeOnOpen))
            {
                string code = JoinCodeOnOpen;
                JoinCodeOnOpen = null;
                _codeField.SetTextWithoutNotify(code);
                Join(code);
            }
            Draw();
        }

        public override void Tick()
        {
            if (_busy) ShowJoining();
            if (_refreshShown != Refreshing) { ShowRefresh(); _nextDraw = 0; }
            if (Time.unscaledTime < _nextDraw) return;
            _nextDraw = Time.unscaledTime + 1.0f;
            // ⚠️ THE LAN QUESTION IS ASKED EVERY SECOND, WHATEVER IS ON SCREEN: there is no LAN tab any more.
            Net.NetSession.Instance?.Beacon?.RequestDiscovery();
            Draw();
        }

        private bool Refreshing => Time.unscaledTime < _refreshUntil;

        /// <summary>REFRESH: re-ask both networks for their rooms. See <see cref="RefreshHoldSeconds"/>.</summary>
        private void RefreshServers()
        {
            if (Refreshing) return;
            _refreshUntil = Time.unscaledTime + RefreshHoldSeconds;
            Hub.Host.RefreshRooms(true);
            Hub.Host.RefreshRooms(false);
            ShowRefresh();
            _drawnOnce = false;
            Draw();
        }

        private void ShowRefresh()
        {
            _refreshShown = Refreshing;
            HubKnit.Say(_refresh, _refreshShown ? "REFRESHING..." : "REFRESH");
            HubKnit.FitLabel(_refresh);
            _refresh.interactable = !_refreshShown;
        }

        /// <summary>LAN rooms first (they answer at once and need no internet), then online ones not already listed.</summary>
        private void Gather()
        {
            _rooms.Clear(); _roomLan.Clear();
            var seen = new HashSet<string>();
            foreach (bool lan in new[] { true, false })
                foreach (var room in Hub.Host.Rooms(lan))
                {
                    if (!seen.Add(room.Key ?? room.Name ?? "")) continue;
                    _rooms.Add(room); _roomLan.Add(lan);
                }
        }

        private void Draw()
        {
            Gather();
            string online = Hub.Host.OnlineBrowserMessage;
            string emptyMessage = Refreshing ? LanRefreshingMessage
                : "No rooms yet. Host one, or join with a code." + (string.IsNullOrEmpty(online) ? "" : "\n" + online);
            int visible = Mathf.Min(_rooms.Count, VisibleRooms);
            bool same = _drawnOnce && _drawnRooms.Count == visible && (visible > 0 || _drawnEmptyMessage == emptyMessage);
            for (int i = 0; same && i < visible; i++) same = _drawnRooms[i].SameListing(_rooms[i]);
            if (same) return;
            _drawnOnce = true;
            _drawnEmptyMessage = emptyMessage;
            _drawnRooms.Clear();
            for (int i = 0; i < visible; i++) _drawnRooms.Add(_rooms[i]);

            for (int i = _list.childCount - 1; i >= 0; i--)
                if (_list.GetChild(i) != _empty.transform) Destroy(_list.GetChild(i).gameObject);
            _empty.text = _rooms.Count == 0 ? emptyMessage : "";
            _emptyCan.gameObject.SetActive(_rooms.Count == 0);

            for (int i = 0; i < visible; i++)
            {
                var room = _rooms[i];
                var row = HubKnit.Plate(_list, "Room" + i, Knit.Card);
                HubKnit.In(row, 0, i * RowPitch, 1080, RowHeight);
                var face = HubKnit.FaceOf(row);
                var name = HubKnit.Display(face, "Name", room.Name, 40, HubKnit.KitCream);
                HubKnit.In(name.rectTransform, 42, 12, 440, 58);
                HubKit.Fit(name, 440);
                var map = HubKnit.Words(face, "Map", room.Map, HubStyle.Floor, HubKnit.KitTan);
                HubKnit.In(map.rectTransform, 44, 70, 440, 40);
                HubKit.Fit(map, 440);

                var tag = HubKnit.Tag(face, "Network", _roomLan[i] ? "LAN" : "ONLINE", _roomLan[i] ? Knit.PillGo : Knit.PillYellow);
                HubKnit.In(tag, 490, 38, tag.sizeDelta.x, tag.sizeDelta.y);
                var players = HubKnit.Display(face, "Players", room.InProgress ? "IN A MATCH" : room.Players + "/" + room.Capacity, room.InProgress ? 28 : 40, HubKnit.KitCream);
                HubKnit.In(players.rectTransform, 640, 36, 210, 60);
                HubKit.Fit(players, 210);
                // ⚠️ A HOST ON ANOTHER PROTOCOL GETS NO JOIN AT ALL, NOT A GREYED ONE (LAN-VPN-JOIN-1020).
                // Pressing it could only be refused at approval after the wait; the tag says why in the
                // place the press would have been, and the focus path skips it.
                if (room.DifferentVersion)
                {
                    var version = HubKnit.Words(face, "Version", DifferentVersionLabel, HubStyle.Floor, HubKnit.KitTan, TextAnchor.MiddleCenter);
                    HubKnit.In(version.rectTransform, 860, 26, 190, 84);
                    HubKit.Fit(version, 190);
                    // A room nobody here can join steps back: its card is faded, its words stay readable.
                    face.GetComponent<Image>().color = new Color(1, 1, 1, 0.55f);
                    continue;
                }
                string key = room.Key;
                var join = HubKnit.Button(face, "Join" + i, "JOIN", Knit.Yellow, () => Join(key), 38);
                HubKnit.In((RectTransform)join.transform, 870, 28, 180, 80);
                join.interactable = room.IsJoinable;
            }
            Hub.RefreshFocus();
        }

        // ⚠️ THE WAIT IS VISIBLE AND ALWAYS ENDS (2026-10-20, a LAN row over Hamachi: "it's stuck on
        // joining"). The line counts the seconds and, after `JoinFailureRules.NoAnswerHintSeconds`,
        // names the usual cause. And this is `async void`: anything thrown before the attempt's own
        // catch used to leave `_busy` set and the line frozen for good.
        private string _joinTarget;
        private float _joinStarted;
        private int _joinShownSecond = -1;

        private async void Join(string key)
        {
            if (_busy || string.IsNullOrWhiteSpace(key)) return;
            _busy = true;
            _joinTarget = key.Trim().ToUpperInvariant(); _joinStarted = Time.unscaledTime; _joinShownSecond = -1;
            ShowJoining();
            string refusal;
            try { refusal = await Hub.Host.Join(key.Trim()); }
            catch (System.Exception error)
            {
                Debug.LogException(error);
                refusal = "Could not join: " + error.Message;
            }
            if (this == null || !_busy) return;
            _busy = false;
            if (!string.IsNullOrEmpty(refusal)) { SetStatus(refusal); MenuSfx.Error(); return; }
            Hub.ShowLobby();
        }

        private void ShowJoining()
        {
            float waited = Time.unscaledTime - _joinStarted;
            int second = (int)waited;
            if (second == _joinShownSecond) return;
            _joinShownSecond = second;
            SetStatus(JoinFailureRules.WaitingLine(_joinTarget, waited));
        }

        private void SetStatus(string line)
        {
            _status.text = line;
            _status.fontSize = HubStyle.Size(HubStyle.Body); HubKit.Fit(_status, Column);
        }

        public override bool Back()
        {
            if (_busy) { _busy = false; Hub.Host.LeaveRoom(); }
            return false;
        }
    }

    /// <summary>
    /// GAME LOBBY, the custom room, in HOME's knit (owner 2026-10-28: "completely new UI", "same
    /// family as HOME", approved from `Logs/lobby-ui-20261028/concept/index.html`). Every piece is
    /// `HubKnit`: HOME's own tiles, plates and PLAY, with HOME's springs.
    ///
    ///   cards      four player cards in a row, the hero popping out of the top like HOME's HERO
    ///              tile; yours gold and raised, the others orange, an open seat a wine "empty
    ///              chair". The whole card is the seat switch (`TakeSeat{n}`). A stamp says who:
    ///              HOST (red, crown), READY (teal, tick), NOT READY (peach).
    ///   host       the strip under the cards (`MatchCard`): MAP previous/next, the CLASSIC /
    ///              HERO STRIKE switch, RULES carrying the rules in brief
    ///   joined     THE MATCH line in the strip's place (`Players/MatchSummary`): map, mode and
    ///              the key rules for everyone, ALL RULES for the full read-only list (owner,
    ///              2026-10-28: "where will ppl see the summary of the match")
    ///   spectator  the same line; WATCHING takes the primary's place
    ///   bottom     the status sentence, START GAME or READY breathing in lime, CHAT in teal with
    ///              its unread count, CHARACTER only offline
    ///
    /// CHAT opens a wine drawer on the right (`LobbyChat`); the cards narrow to the left, as
    /// quarter columns, and the host strip steps aside.
    ///
    /// THE FOUR ANSWERS:
    ///   the one thing   the players, n of 4
    ///   first press     START GAME for the host; READY for everyone else, in the same place
    ///   not needed now  the full rules and chat, one press away
    ///   out             BACK or Escape: a player leaves; the host is asked first, because
    ///                   leaving closes the room for everyone in it
    /// </summary>
    public sealed class HubLobby : HubScreen
    {
        public override bool WearsBoard => true;
        public override float CourtShade => 1f;

        // The room's measures, canvas units, centred on the canvas (y up). A taller canvas (4:3)
        // adds air above and below the middle band instead of pulling the cards from the strip.
        public const float RoomContentWidth = 1800, CardGap = 40;
        internal const float CardsTop = 230, HostSeatHeight = 454, GuestSeatHeight = 534, ChatSeatHeight = 554;
        internal const float StripTop = -236, StripHeight = 154;
        internal const float ChatWidth = 570, ChatGap = 50;
        /// <summary>The cards' column while the chat drawer is open, and how far it moves left.</summary>
        internal const float RoomSeatsNarrow = RoomContentWidth - ChatWidth - ChatGap;
        internal const float RoomSeatsOffset = -(RoomContentWidth - RoomSeatsNarrow) * .5f;
        /// <summary>The chat drawer's left edge, measured from the canvas centre.</summary>
        internal const float RoomChatLeft = RoomContentWidth * .5f - ChatWidth;
        internal const float RoomChatWidth = ChatWidth;

        private Text _code, _codeCaption, _address, _count, _status, _unreadCount;
        private RectTransform _seats, _playersPanel, _matchCard, _summaryStrip, _networkTag, _statusPlate, _watching;
        private HubButton _primary, _watch, _rules, _viewRules, _chatDoor, _codeChip, _modeClassic, _modeHero;
        private HubButton[] _modeTabs;
        private Text _networkWord, _watchingLine;
        private HubArrowChoice _mapChoice;
        private GameObject _unread;
        private bool _chatShown, _hostLayout;
        private int _seenLines;
        private float _panelWidth = RoomContentWidth, _panelX;

        // ⚠️⚠️ THE HOST'S FIREWALL LINE (LAN-VPN-JOIN-1020). A zip copy of the game hosting a LAN
        // room can be listed by every player on a VPN and answer none of them, because Windows'
        // first-run prompt allows it on Private networks only and files Hamachi and Radmin under
        // Public. `LanFirewall` reads, without admin, whether this program is reachable on the
        // active networks; when it is not, the room says so in one line, with ALLOW LAN PLAY, which
        // asks Windows (UAC) to add the installer's rule. An installed copy reads Open and shows
        // nothing, and so does an online room, a client and every non-Windows player.
        // ⚠️ IT HANGS UNDER THE HOST'S STRIP (`MatchCard`), the room's network facts, and steps
        // aside with the strip while the chat is open.
        internal const float FirewallWidth = 610;
        private RectTransform _firewall;
        private Text _firewallLine;
        private HubButton _firewallAllow;
        private string _firewallDrawn = "";
        private int _firewallAllowedSeen;

        public override Selectable FirstFocus => _primary != null && _primary.gameObject.activeInHierarchy ? _primary : (Selectable)_watch;
        private string _drawn = "";

        public override void Build()
        {
            HubKnit.Back(Root, Hub);
            BuildTitle();
            BuildTicket();
            BuildPlayers();
            BuildMatchCard();
            BuildSummary();
            BuildBottom();

            Net.LanFirewall.Invalidate();
            _firewallAllowedSeen = Net.LanFirewall.AllowedCount;
            Draw(true);
            DrawFirewall();
            HubSlap.On(_playersPanel, 0, -1);
            HubSlap.On(_matchCard, 0.06f, 1);
        }

        // ------------------------------------------------------------------ top: name and code

        private Text _title, _titleCaption;

        /// <summary>The room's name beside BACK, with whose room it is under it.</summary>
        private void BuildTitle()
        {
            _title = HubKnit.Title(Root, "LOBBY", "YOUR ROOM", out _titleCaption);
        }

        /// <summary>A small letter-spaced caption in HOME's tag face (the `#1234` lettering).</summary>
        internal static Text Caption(Transform parent, string name, string words, Color colour)
        {
            var text = HubKit.Text(parent, name, words, HubStyle.Floor, false, colour, TextAnchor.MiddleLeft);
            text.font = HubBoard.FontFor("tag");
            text.fontSize = HubStyle.Size(HubStyle.Floor) - 4 < HubStyle.Floor ? HubStyle.Floor : HubStyle.Size(HubStyle.Floor) - 4;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            return text;
        }

        /// <summary>
        /// The code is the one fact a host reads out loud, so it is a berry card, top right: the code
        /// large, the network as a tag (a fact, not a setting), the LAN address beside it.
        /// Pressing it copies the code.
        /// </summary>
        private void BuildTicket()
        {
            _codeChip = HubKnit.Button(Root, "CodeChip", null, Knit.Card, CopyCode);
            HubKit.Place((RectTransform)_codeChip.transform, HubKit.TopRight, new Vector2(-60, -30), new Vector2(460, 184));
            var face = _codeChip.Body;
            _codeCaption = HubKnit.Display(face, "CodeCaption", "ROOM CODE", 28, HubKnit.KitTan);
            HubKnit.In(_codeCaption.rectTransform, 40, 26, 260, 40);
            _code = HubKnit.Display(face, "Code", "", 60, HubKnit.KitCream);
            HubKnit.In(_code.rectTransform, 40, 70, 170, 80);
            _networkTag = HubKnit.Tag(face, "NetworkTag", "LAN", Knit.PillGo, 56);
            HubKit.Place(_networkTag, HubKit.TopRight, new Vector2(-30, -20), _networkTag.sizeDelta);
            _networkWord = _networkTag.Find("Face/Label").GetComponent<Text>();
            _address = HubKnit.Words(face, "Address", "", HubStyle.Floor, HubKnit.KitTan, TextAnchor.MiddleRight);
            HubKit.Place(_address.rectTransform, HubKit.TopRight, new Vector2(-30, -92), new Vector2(240, 60));
            _address.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        // ------------------------------------------------------------------ the cards

        private void BuildPlayers()
        {
            _count = HubKnit.Display(Root, "PlayersCount", "", 46, HubKnit.KitWhite, TextAnchor.MiddleLeft, 5);
            HubKnit.At(_count.rectTransform, 60, 216, 500, 70);

            // ⚠️ SPECTATE STAYS AT THE TOP RIGHT OF THE PLAYERS (owner 2026-10-28). It is a seat choice,
            // so it sits on the line of seats; on, it is lime and says so.
            _watch = HubKnit.Button(Root, "WatchToggle", "SPECTATE", Knit.Plain, () => { Hub.Host.ToggleSpectate(); _drawn = ""; }, 34);

            _playersPanel = HubKit.Rect(Root, "Players");
            _playersPanel.anchorMin = _playersPanel.anchorMax = new Vector2(.5f, .5f);
            _playersPanel.pivot = new Vector2(.5f, 1);
            _playersPanel.anchoredPosition = new Vector2(0, CardsTop);
            _playersPanel.sizeDelta = new Vector2(RoomContentWidth, HostSeatHeight);
            _seats = HubKit.Stretch(HubKit.Rect(_playersPanel, "Seats"));
        }

        /// <summary>The host's MAP, MODE and RULES, under the seats: each under its own white label, no strip.</summary>
        private void BuildMatchCard()
        {
            var card = _matchCard = HubKit.Rect(Root, "MatchCard");
            card.anchorMin = card.anchorMax = new Vector2(.5f, .5f);
            card.pivot = new Vector2(.5f, 1);
            card.anchoredPosition = new Vector2(0, StripTop);
            card.sizeDelta = new Vector2(RoomContentWidth, StripHeight);

            Label("MapCaption", "MAP", 0);
            // ⚠️ THE SAME `SelectMap` call as ever, so the room changes map in one press.
            string[] maps = new string[SceneFlow.MapRegistry.Length];
            for (int i = 0; i < maps.Length; i++) maps[i] = SceneFlow.MapRegistry[i].Name.ToUpperInvariant();
            _mapChoice = HubArrowChoice.BuildKnit(card, "Map", maps, MapIndex());
            HubKnit.In((RectTransform)_mapChoice.transform, 0, 50, 580, 92);
            _mapChoice.Changed += i => { Hub.Host.SelectMap(SceneFlow.MapRegistry[i].Id); _drawn = ""; };

            // MODE is chosen here and only here: the tab track, its pill lime.
            Label("ModeCaption", "MODE", 660);
            var modes = HubKnit.Tabs(card, "ModePlate", 660, 42, 600, 104, new[] { "ModeClassic", "ModeHeroStrike" },
                                     new[] { "CLASSIC", "HERO STRIKE" }, i => PickMode(i == 0 ? GameMode.Classic : GameMode.HeroStrike));
            _modeTabs = modes; _modeClassic = modes[0]; _modeHero = modes[1];

            Label("RulesCaption", "RULES", 1320);
            _rules = HubKnit.Button(card, "SettingsDoor", "RULES", Knit.Plain, OpenRules, 36);
            HubKnit.In((RectTransform)_rules.transform, 1320, 50, 480, 92);

            // The host's firewall line sits in the free corner beside START GAME.
            _firewall = HubKit.Rect(card, "LanFirewall");
            HubKnit.In(_firewall, 0, 160, FirewallWidth, 130);
            _firewallLine = HubKnit.Words(_firewall, "Line", "", HubStyle.Floor, HubKnit.KitWhite, TextAnchor.UpperLeft, true, 3);
            HubKnit.In(_firewallLine.rectTransform, 0, 0, FirewallWidth, 70);
            _firewallAllow = HubKnit.Button(_firewall, "AllowLanPlay", LanFirewallRules.AllowLabel, Knit.Yellow, AllowLanPlay, HubStyle.Floor);
            HubKnit.In((RectTransform)_firewallAllow.transform, 0, 72, 320, 60);
            HubKnit.FitLabel(_firewallAllow);
            _firewall.gameObject.SetActive(false);

            void Label(string name, string words, float x)
            {
                var label = HubKnit.Display(card, name, words, 34, HubKnit.KitWhite, TextAnchor.MiddleLeft, 4);
                HubKnit.In(label.rectTransform, x, 0, 300, 44);
            }
        }

        private void PickMode(GameMode mode)
        {
            if (SceneFlow.SelectedMode != mode) Hub.Host.SelectMode(mode);
            _drawn = "";
        }

        /// <summary>
        /// THE MATCH, for everyone who is not the host: one line in the host strip's place, inside the
        /// players' column (`Players/MatchSummary`) so it narrows with the cards when chat opens.
        /// </summary>
        private void BuildSummary()
        {
            // ⚠️ A BUTTON, NOT A STRIP (owner 2026-10-28: "add like a button ppl can click that show the
            // configs ... kaysa sa ikalit dyan"). Lobbies such as Among Us and Fortnite's keep the host's
            // settings one press away rather than printed across the room; this opens them on a card
            // (`HubMatchSettingsPopup`). It rides in the players' column (`Players/MatchSummary`), so it
            // stays under the cards when the chat narrows them.
            // ⚠️ SMALL, AT THE SIDE, ONE WORD (owner 2026-10-28: "does NOT belong to the middle ... very
            // small button ... a short one word"). RULES, the same word as the host's button, beside
            // SPECTATE on the players' line; it rides the players' column so it follows the chat.
            _summaryStrip = HubKit.Rect(_playersPanel, "MatchSummary");
            _summaryStrip.anchorMin = _summaryStrip.anchorMax = new Vector2(1, 1);
            _summaryStrip.pivot = new Vector2(1, .5f);
            _summaryStrip.anchoredPosition = new Vector2(-(280 + 20), 58);
            _summaryStrip.sizeDelta = new Vector2(220, 80);
            _viewRules = HubKnit.Button(_summaryStrip, "ViewRules", "RULES", Knit.Plain, OpenSettings, 34);
            HubKit.Stretch((RectTransform)_viewRules.transform);
            HubKnit.FitLabel(_viewRules);
        }

        /// <summary>The host's settings on a card, read-only, for everyone who is not the host.</summary>
        private void OpenSettings()
        {
            if (_chatShown) Hub.Host.ToggleChat();
            Hub.Push<HubMatchSettingsPopup>();
        }

        private void BuildBottom()
        {
            _statusPlate = HubKnit.Plate(Root, "StatusPlate", Knit.Card);
            HubKnit.At(_statusPlate, 60, 946, 600, 104);
            _status = HubKnit.Words(HubKnit.FaceOf(_statusPlate), "Status", "", HubStyle.Floor, HubKnit.KitCream, TextAnchor.MiddleLeft);
            HubKit.Stretch(_status.rectTransform);
            _status.rectTransform.offsetMin = new Vector2(30, 20); _status.rectTransform.offsetMax = new Vector2(-24, -8);
            _status.horizontalOverflow = HorizontalWrapMode.Wrap;

            _primary = HubKnit.Button(Root, "StartGame", "START GAME", Knit.Go, Primary, 54, HubGlyph.Mark.None, primary: true);
            HubKnit.At((RectTransform)_primary.transform, 690, 936, 540, 124);

            // A spectator has nothing to press there, so the room's status takes the spot instead.
            _watching = HubKnit.Plate(Root, "WatchingPlate", Knit.Card);
            HubKnit.At(_watching, 560, 930, 800, 134);
            var watchingWord = HubKnit.Display(HubKnit.FaceOf(_watching), "Word", "WATCHING", 44, HubKnit.KitCream, TextAnchor.MiddleCenter);
            HubKnit.In(watchingWord.rectTransform, 0, 12, 800, 56);
            _watchingLine = HubKnit.Words(HubKnit.FaceOf(_watching), "Line", "", HubStyle.Floor, HubKnit.KitTan, TextAnchor.MiddleCenter);
            HubKnit.In(_watchingLine.rectTransform, 20, 68, 760, 44);
            _watching.gameObject.SetActive(false);

            // CHAT carries the count of lines that arrived while it was shut; CHARACTER exists only
            // where heroes are not chosen together after START (offline rooms).
            _chatDoor = HubKnit.Button(Root, "ChatDoor", "CHAT", Knit.Yellow, () => Hub.Host.ToggleChat(), 44);
            HubKnit.At((RectTransform)_chatDoor.transform, 1560, 946, 300, 104);
            var badge = HubKnit.Plate(_chatDoor.Body, "Unread", Knit.PillDanger);
            HubKit.Place(badge, HubKit.TopRight, new Vector2(18, 20), new Vector2(70, 56));
            _unreadCount = HubKit.Text(HubKnit.FaceOf(badge), "Count", "", HubStyle.Floor, true, HubKnit.KitCream, TextAnchor.MiddleCenter);
            HubKit.Stretch(_unreadCount.rectTransform);
            _unreadCount.rectTransform.anchorMin = new Vector2(0, .06f);
            _unread = badge.gameObject;
            _unread.SetActive(false);
            _seenLines = LobbyChat.RoomLineCount;
            _characterDoor = HubKnit.Button(Root, "CharacterDoor", "CHARACTER", Knit.Yellow, () =>
            {
                if (_chatShown) Hub.Host.ToggleChat();
                Hub.Push<HubCharacterSelect>(c => c.Timed = false);
            }, 36);
            HubKnit.At((RectTransform)_characterDoor.transform, 1240, 946, 300, 104);
            HubKnit.FitLabel(_characterDoor);
        }

        private HubButton _characterDoor;

        private static int MapIndex()
        {
            for (int i = 0; i < SceneFlow.MapRegistry.Length; i++)
                if (SceneFlow.MapRegistry[i].Id == SceneFlow.SelectedMap) return i;
            return 0;
        }

        private void OpenRules()
        {
            if (_chatShown) Hub.Host.ToggleChat();
            Hub.Host.OpenCustomRules();
        }

        /// <summary>
        /// The chat took or gave back the right-hand side. The host's strip steps aside for it and
        /// the cards narrow to the left as quarter columns (<see cref="Tick"/> eases them).
        /// </summary>
        public void SetChatPresented(bool shown)
        {
            if (_chatShown == shown) return;
            _chatShown = shown;
            if (shown) { _seenLines = LobbyChat.RoomLineCount; if (_unread != null) _unread.SetActive(false); }
            // Opening eases the cards aside; closing puts them back at once, as the drawer itself
            // closes at once (`LobbyChat.PresentLobbyColumn`), so a pressed CLOSE never lingers.
            Layout(!shown);
            _drawn = "";
            _firewallDrawn = "";
            DrawFirewall();
            Hub?.RefreshFocus();
        }

        private float WidthTarget => _chatShown ? RoomSeatsNarrow : RoomContentWidth;
        private float PanelTarget => _chatShown ? RoomSeatsOffset : 0;
        private float SeatHeight => _chatShown ? ChatSeatHeight : _hostLayout ? HostSeatHeight : GuestSeatHeight;

        /// <summary>Place the pieces for the role and the chat. Snaps on a rebuild, eases otherwise.</summary>
        private void Layout(bool snap)
        {
            if (_playersPanel == null) return;
            if (_matchCard != null) _matchCard.gameObject.SetActive(_hostLayout && !_chatShown);
            if (_summaryStrip != null) _summaryStrip.gameObject.SetActive(!_hostLayout);
            // The drawer takes the right-hand side, doors included; they come back when it closes.
            if (_chatDoor != null) _chatDoor.gameObject.SetActive(!_chatShown);
            if (_characterDoor != null)
                _characterDoor.gameObject.SetActive(!_chatShown && Hub != null && !Hub.Host.Spectating
                    && !(Net.NetSession.Instance != null && Net.NetSession.Instance.IsNetworked));
            if (_watch != null)
                HubKnit.At((RectTransform)_watch.transform, (_chatShown ? 1240 : 1860) - (_hostLayout ? 260 : 280), 212, _hostLayout ? 260 : 280, 80);
            if (snap || HubStyle.ReducedMotion) { _panelX = PanelTarget; _panelWidth = WidthTarget; }
            ApplyPanel();
            if (_statusPlate != null)
                _statusPlate.sizeDelta = new Vector2(_chatShown ? 300 : 600, 104);
            if (_primary != null)
            {
                var p = (RectTransform)_primary.transform;
                p.anchoredPosition = new Vector2((_chatShown ? 650 : 960) - 270 - 960, 540 - 936);
            }
        }

        private void ApplyPanel()
        {
            _playersPanel.anchoredPosition = new Vector2(_panelX, CardsTop);
            _playersPanel.sizeDelta = new Vector2(_panelWidth, SeatHeight);
        }

        private void OnDisable()
        {
            if (_chatShown && Hub != null) Hub.Host.ToggleChat();
        }

        private void CopyCode()
        {
            GUIUtility.systemCopyBuffer = Hub.Host.RoomCode ?? "";
            Hub.Toast("Code copied: " + Hub.Host.RoomCode);
        }

        public override void Tick()
        {
            Draw(false);
            DrawFirewall();
            if (_playersPanel != null && (!Mathf.Approximately(_panelX, PanelTarget) || !Mathf.Approximately(_panelWidth, WidthTarget)))
            {
                // A short ease rather than a jump, so the eye follows the cards to their new place.
                float k = 1 - Mathf.Exp(-Time.unscaledDeltaTime * 16);
                _panelX = HubStyle.ReducedMotion ? PanelTarget : Mathf.MoveTowards(Mathf.Lerp(_panelX, PanelTarget, k), PanelTarget, Time.unscaledDeltaTime * 300);
                _panelWidth = HubStyle.ReducedMotion ? WidthTarget : Mathf.MoveTowards(Mathf.Lerp(_panelWidth, WidthTarget, k), WidthTarget, Time.unscaledDeltaTime * 600);
                ApplyPanel();
            }
            if (_unread != null)
            {
                if (_chatShown) _seenLines = LobbyChat.RoomLineCount;
                int waiting = _chatShown ? 0 : LobbyChat.RoomLineCount - _seenLines;
                if (_unread.activeSelf != waiting > 0) _unread.SetActive(waiting > 0);
                if (waiting > 0) _unreadCount.text = waiting > 9 ? "9+" : waiting.ToString();
            }
        }

        /// <summary>
        /// The host's firewall line and its control, redrawn only when what they say changes.
        /// ⚠️ A CONTROL THAT HIDES WHILE FOCUSED HANDS FOCUS TO THE PRIMARY, so a pad that just
        /// pressed ALLOW LAN PLAY is never left holding nothing.
        /// </summary>
        private void DrawFirewall()
        {
            if (_firewall == null) return;
            var host = Hub.Host;
            bool lanHost = host != null && host.InRoom && host.IsHost && !host.RoomOnline && Net.LanFirewall.Supported;
            if (lanHost)
            {
                var beacon = Net.NetSession.Instance != null ? Net.NetSession.Instance.Beacon : null;
                Net.LanFirewall.Watch(beacon != null ? beacon.Port : Net.LobbySession.DefaultPort);
            }
            if (Net.LanFirewall.AllowedCount != _firewallAllowedSeen)
            {
                _firewallAllowedSeen = Net.LanFirewall.AllowedCount;
                if (lanHost) Hub.Toast(LanFirewallRules.AllowedToast);
            }

            string line = lanHost ? Net.LanFirewall.Line : "";
            bool shown = line.Length > 0 && !_chatShown;
            bool offer = shown && Net.LanFirewall.CanRequest;
            string key = shown + "|" + offer + "|" + line;
            if (key == _firewallDrawn) return;
            _firewallDrawn = key;

            var events = UnityEngine.EventSystems.EventSystem.current;
            bool focused = events != null && events.currentSelectedGameObject == _firewallAllow.gameObject;
            _firewall.gameObject.SetActive(shown);
            _firewallLine.text = line;
            _firewallLine.rectTransform.sizeDelta = new Vector2(offer ? FirewallWidth - 340 : FirewallWidth - 12, 72);
            _firewallAllow.gameObject.SetActive(offer);
            Hub.RefreshFocus();
            if (focused && !offer && _primary != null) events.SetSelectedGameObject(_primary.gameObject);
        }

        private void AllowLanPlay()
        {
            Net.LanFirewall.RequestAllow();
            DrawFirewall();
        }

        private static string ModeName => SceneFlow.SelectedMode == GameMode.Classic ? "CLASSIC" : "HERO STRIKE";

        /// <summary>The rules in brief, as the RULES button and THE MATCH line both show them.</summary>
        internal static string RulesInBrief(CustomRules rules, string between)
        {
            if (rules == null) return "";
            string brief = rules.Rounds + " rounds" + between + rules.RoundSeconds + " s each";
            if (rules.ScoreTarget > 0) brief += between + "first to " + rules.ScoreTarget;
            return brief;
        }

        private void Draw(bool force)
        {
            var host = Hub.Host;
            if (!host.InRoom)
            {
                // The room ended under us: say so once and go HOME. The controller's disconnect
                // handler is the one that knows why; the hub shows its sentence as a toast.
                if (!force) { Hub.Home(); return; }
            }

            var seats = host.Seats();
            var rules = SceneFlow.SelectedRules;
            string key = host.RoomTitle + host.RoomCode + host.IsHost + host.LocalReady + SceneFlow.SelectedMap + SceneFlow.SelectedMode + host.Spectating + host.MatchInProgress + host.SeatSwapOffer?.Id + host.SeatSwapResult + HubQueueWatch.QueueRoom + host.RoomOnline + host.RoomAddress;
            key += host.CanEditBots + RulesInBrief(rules, ",");
            key += Net.MatchRpc.Instance != null && (Net.MatchRpc.Instance.CharacterSelecting || Net.MatchRpc.Instance.QueueMapVoting);
            foreach (var s in seats) key += s.Occupied + ":" + s.Name + ":" + s.CharacterPick + ":" + s.Ready + ":" + s.Bot + ":" + s.Host + ":" + s.Mine + ":" + host.CanTakeSeat(s.Slot) + "|";
            if (!force && key == _drawn) return;
            _drawn = key;

            bool hostLayout = host.IsHost && !HubQueueWatch.QueueRoom;
            bool snap = force || hostLayout != _hostLayout;
            _hostLayout = hostLayout;
            Layout(snap);

            string title = string.IsNullOrWhiteSpace(host.RoomTitle) ? "LOBBY" : host.RoomTitle.ToUpperInvariant();
            _title.text = title;
            _title.fontSize = 86;
            HubKit.Fit(_title, 1120);
            string owner = "";
            foreach (var s in seats) if (s.Host && !s.Mine && s.Occupied) owner = s.Name;
            _titleCaption.text = host.IsHost ? "YOUR ROOM" : string.IsNullOrEmpty(owner) ? "CUSTOM ROOM" : ("HOSTED BY " + owner).ToUpperInvariant();

            _code.text = host.RoomCode ?? "";
            string address = host.RoomAddress;
            _address.text = host.RoomOnline ? "tap to copy" : address ?? "";
            string network = host.RoomOnline ? "ONLINE" : "LAN";
            if (_networkWord.text != network)
            {
                _networkWord.text = network;
                HubKnit.Recolour(_networkTag, host.RoomOnline ? Knit.PillYellow : Knit.PillGo);
                _networkTag.sizeDelta = new Vector2(_networkWord.preferredWidth + 56, _networkTag.sizeDelta.y);
            }

            // Lime means on.
            HubKnit.Recolour(_watch.transform, host.Spectating ? Knit.Go : Knit.Plain);
            HubKnit.Say(_watch, host.Spectating ? "SPECTATING" : "SPECTATE");
            HubKnit.FitLabel(_watch);

            if (_mapChoice != null && _mapChoice.Value != MapIndex()) _mapChoice.Set(MapIndex(), false);
            bool classic = SceneFlow.SelectedMode == GameMode.Classic;
            HubKnit.ChooseTab(_modeTabs, classic ? 0 : 1);
            HubKnit.Say(_rules, rules != null ? rules.Rounds + " ROUNDS · " + rules.RoundSeconds + " S" : "RULES");
            HubKnit.FitLabel(_rules);

            int occupied = 0;
            foreach (var s in seats)
                if (s.Occupied) occupied++;
            _count.text = "PLAYERS " + occupied + " / " + Balance.PlayerCount;

            DrawSeats(host, seats);

            _watching.gameObject.SetActive(false);
            if (host.IsHost)
            {
                // ⚠️ ALWAYS PRESSABLE FOR THE HOST. A custom room starts with whoever is in it,
                // and an OPEN seat is an empty chair (`ConvertedMatchSetup.StartGame`).
                HubKnit.Say(_primary, "START GAME");
                _primary.gameObject.SetActive(true);
                _primary.interactable = true;
                int open = 0;
                foreach (var s in seats) if (!s.Occupied && !s.Bot) open++;
                // ⚠️ NO RUNNING COMMENTARY (owner 2026-10-28, of "3 seats are open": "why do they need to
                // know that, intuitive na un"). The cards already show the open seats.
                _status.text = "";
            }
            else if (host.Spectating)
            {
                // ⚠️ A SPECTATOR HAS NOTHING TO READY: the host refuses a spectator's READY
                // (`MatchRpc.HostDeclareReady`), so the room's status takes the primary's place.
                _primary.gameObject.SetActive(false);
                _watching.gameObject.SetActive(true);
                int ready = 0, players = 0;
                foreach (var s in seats) if (s.Occupied && !s.Host) { players++; if (s.Ready) ready++; }
                _watchingLine.text = players == 0 ? "Waiting for the host to start" : ready + " of " + players + " ready · waiting for the host to start";
                _status.text = "";
            }
            else
            {
                HubKnit.Say(_primary, host.LocalReady ? "READY  ✓" : "READY");
                _primary.gameObject.SetActive(true);
                _primary.interactable = true;
                _status.text = "";
            }
            if (host.Spectating && Net.MatchRpc.Instance != null && Net.MatchRpc.Instance.CharacterSelecting)
                _watchingLine.text = "The players are choosing their heroes.";
            else if (host.Spectating && Net.MatchRpc.Instance != null && Net.MatchRpc.Instance.QueueMapVoting)
                _watchingLine.text = "The players are voting for the map.";
            // A spectator has no seat, so no hero to choose. In a networked room heroes are chosen together
            // in character select after START (owner, October 22: "baket u can pick character na agad?").
            if (_characterDoor != null)
                _characterDoor.gameObject.SetActive(!host.Spectating && !_chatShown
                    && !(Net.NetSession.Instance != null && Net.NetSession.Instance.IsNetworked));
            if (host.SeatSwapOffer != null && !host.SeatSwapOffer.Incoming)
                _status.text = "Waiting for the other player to accept the seat switch.";
            else if (!string.IsNullOrEmpty(host.SeatSwapResult)) _status.text = host.SeatSwapResult;
            _status.fontSize = HubStyle.Size(HubStyle.Floor);
            // The plate is there only while it has something to say (a seat switch waiting, its answer).
            _statusPlate.gameObject.SetActive(!string.IsNullOrEmpty(_status.text));
            Hub.RefreshFocus();
        }

        private void DrawSeats(IHubHost host, HubSeat[] seats)
        {
            // Immediate outside play mode (EditMode tests and captures redraw too); deferred in play.
            for (int i = _seats.childCount - 1; i >= 0; i--)
            {
                var old = _seats.GetChild(i).gameObject;
                if (Application.isPlaying) { old.transform.SetParent(null, false); Destroy(old); } else DestroyImmediate(old);
            }
            var people = Roster.GetPeople(SceneFlow.SelectedMode);
            int n = Mathf.Max(1, seats.Length);
            for (int i = 0; i < seats.Length; i++)
            {
                var seat = seats[i];
                // ⚠️ QUARTER COLUMNS, ANCHORED, NOT PLACED: the cards reflow when the chat narrows the row.
                var slot = HubKit.Rect(_seats, "Seat" + i);
                slot.anchorMin = new Vector2((float)i / n, 0); slot.anchorMax = new Vector2((float)(i + 1) / n, 1);
                slot.offsetMin = new Vector2(i == 0 ? 0 : CardGap * .5f, 0);
                slot.offsetMax = new Vector2(i == n - 1 ? 0 : -CardGap * .5f, 0);
                bool open = !seat.Occupied && !seat.Bot;
                Knit kind = open ? Knit.Well : seat.Mine ? Knit.CardOn : Knit.Card;
                int destination = seat.Slot;
                bool editBots = host.CanEditBots && !seat.Occupied;
                float s = (_chatShown ? 280 : 420) / 420f;
                float cardW = 420 * s, cardH = SeatHeight - 14;

                // ⚠️⚠️ THE WHOLE CARD IS THE DOOR (owner 2026-10-17: "click anywhere on their respective
                // rectangle"). A card the player can move to is a button that lifts and says what a press
                // does; one they cannot is a plate. It keeps the name `TakeSeat{n}` and the same
                // authoritative request, re-checked on press because the room may have changed.
                RectTransform card, face;
                HubButton take = null;
                if (!seat.Mine && host.CanTakeSeat(destination))
                {
                    take = HubKnit.Button(slot, "TakeSeat" + destination, null, kind, () =>
                    {
                        if (Hub.Host.CanTakeSeat(destination)) Hub.Host.TakeSeat(destination);
                    });
                    card = HubKit.Stretch((RectTransform)take.transform);
                    face = take.Body;
                }
                else
                {
                    card = HubKit.Stretch(HubKnit.Plate(slot, "Card", kind));
                    face = HubKnit.FaceOf(card);
                }

                var person = seat.Occupied ? Roster.At(people, seat.CharacterPick) : null;
                var number = HubKnit.Display(face, "Number", "P" + (seat.Slot + 1), Mathf.RoundToInt(30 * s), open ? HubKnit.KitSoftRed : HubKnit.KitTan);
                HubKnit.In(number.rectTransform, 30 * s, 30 * s, 80, 40 * s);

                if (open)
                {
                    var plus = HubKit.Glyph(face, "Icon", HubGlyph.Mark.Plus, HubKnit.KitSoftRed, 0.12f);
                    HubKnit.In(plus.rectTransform, (cardW - 70 * s) * .5f, cardH * .36f - 35 * s, 70 * s, 70 * s);
                    var openWord = HubKnit.Display(face, "Name", "OPEN SEAT", Mathf.RoundToInt(44 * s), HubKnit.KitCream, TextAnchor.MiddleCenter);
                    HubKnit.In(openWord.rectTransform, 0, cardH * .6f - 30, cardW, 60);
                }
                else
                {
                    // The portrait is cut flat at the chest, so its window fades at the lower edge.
                    float size = Mathf.Min(cardW - 60 * s, cardH * .56f);
                    var frame = HubKit.Rect(face, "Hero");
                    HubKnit.In(frame, (cardW - size) * .5f, 26 * s, size, size * .92f);
                    frame.gameObject.AddComponent<RectMask2D>().softness = new Vector2Int(0, 30);
                    var picture = HubKit.Picture(frame, "Face", HubKit.Portrait(person?.Id));
                    HubKnit.In(picture.rectTransform, 0, 0, size, size);
                    if (picture.sprite != null)
                    {
                        var feel = card.GetComponent<HubStickerFeel>();
                        if (feel != null) feel.Pictures = new[] { frame };
                    }
                    else
                    {
                        var glyph = HubKit.Glyph(face, "Icon", seat.Bot && !seat.Occupied ? HubGlyph.Mark.Bots : HubGlyph.Mark.Person, HubKnit.KitTan, 0.08f);
                        HubKnit.In(glyph.rectTransform, (cardW - 150 * s) * .5f, 70 * s, 150 * s, 150 * s);
                    }

                    string name = seat.Occupied ? (seat.Mine ? "YOU" : seat.Name) : "BOT";
                    var label = HubKnit.Display(face, "Name", name.ToUpperInvariant(), Mathf.RoundToInt(52 * s), seat.Mine ? HubKnit.KitLime : HubKnit.KitCream, TextAnchor.MiddleCenter);
                    HubKnit.In(label.rectTransform, 16, cardH - 150 * s - 30, cardW - 32, 60);
                    HubKit.Fit(label, cardW - 40);
                    string sub = person != null ? person.Name.ToUpperInvariant() : seat.Bot ? "PLAYS THIS SEAT" : "";
                    if (_chatShown && seat.Bot && person == null) sub = "";
                    var under = HubKnit.Words(face, "HeroName", sub, HubStyle.Floor, HubKnit.KitTan, TextAnchor.MiddleCenter);
                    HubKnit.In(under.rectTransform, 16, cardH - 104 * s - 20, cardW - 32, 40);
                    under.horizontalOverflow = HorizontalWrapMode.Overflow;
                    HubKit.Fit(under, cardW - 40);

                    // One pill per card: who runs the room, who is set, who is not yet.
                    RectTransform stamp;
                    if (seat.Host) stamp = HubKnit.Tag(face, "Stamp", "HOST", Knit.PillYellow, 52);
                    else if (seat.Ready || seat.Bot) stamp = HubKnit.Tag(face, "Stamp", "READY", Knit.PillGo, 52);
                    else stamp = HubKnit.Tag(face, "Stamp", "NOT READY", Knit.PillPlain, 52);
                    HubKnit.In(stamp, (cardW - stamp.sizeDelta.x) * .5f, cardH - 76 * s, stamp.sizeDelta.x, 52);
                }

                if (take != null)
                {
                    // The hint names the press, so the card reads before it is clicked. Shown on hover
                    // and on pad focus; a thumb has no hover and simply taps.
                    var hint = HubKnit.Display(face, "Label", seat.Occupied || seat.Bot ? "SWITCH" : "MOVE HERE", 34, HubKnit.KitWhite, TextAnchor.MiddleCenter, 4);
                    HubKnit.In(hint.rectTransform, 0, cardH * .22f, cardW, 54);
                    hint.enabled = false;
                    take.Attention += attended => { if (hint != null) hint.enabled = attended; };
                }

                // ⚠️ ADD BOT IN THE EMPTY CHAIR, A SMALL X ON A BOT'S CORNER, for the host of a custom
                // room only. ⚠️ SIBLINGS OF THE CARD'S BUTTON, NOT CHILDREN OF ITS FACE: a child would
                // lift the card and show SWITCH while the pointer is on the bot control.
                if (editBots)
                {
                    if (seat.Bot)
                    {
                        var remove = HubKnit.Button(slot, "RemoveBot" + destination, null, Knit.Danger,
                                                    () => Hub.Host.SetSeatBot(destination, false), HubStyle.Label, HubGlyph.Mark.Close);
                        HubKit.Place((RectTransform)remove.transform, HubKit.TopRight, new Vector2(10, 14), new Vector2(84, 72));
                    }
                    else
                    {
                        var add = HubKnit.Button(slot, "AddBot" + destination, _chatShown ? "+ BOT" : "+ ADD BOT", Knit.Yellow,
                                                 () => Hub.Host.SetSeatBot(destination, true), Mathf.RoundToInt(34 * s));
                        var r = (RectTransform)add.transform;
                        r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(1, 0); r.pivot = new Vector2(.5f, 0);
                        r.offsetMin = new Vector2(35 * s, 14 + 46 * s); r.offsetMax = new Vector2(-35 * s, 14 + 130 * s);
                        HubKnit.FitLabel(add);
                    }
                }
            }
        }

        private void Primary()
        {
            if (Hub.Host.IsHost) Hub.Host.StartGame();
            else Hub.Host.ToggleReady();
            _drawn = "";
        }

        /// <summary>
        /// BACK and Escape. A player or spectator leaves at once: the code brings them back, so a
        /// question would only slow a routine exit. The host is asked first, because the room and
        /// everyone in it go with them (`HubRoomClosePopup`).
        /// </summary>
        public override bool Back()
        {
            var host = Hub.Host;
            if (host.InRoom && host.IsHost && !HubQueueWatch.QueueRoom && !host.MatchInProgress)
            {
                int others = 0;
                foreach (var s in host.Seats()) if (s.Occupied && !s.Mine) others++;
                if (_chatShown) host.ToggleChat();
                Hub.Push<HubRoomClosePopup>(p => p.Others = others);
                return true;
            }
            host.LeaveRoom();
            Hub.Home();
            return true;
        }
    }

    /// <summary>
    /// MATCH SETTINGS: what the host picked, on a knit card, for a player or spectator to read. The
    /// facts a player plans around first (map, mode, rounds, time), then the room's. FULL RULES opens
    /// the complete read-only sheet (`OpenCustomRules`).
    /// </summary>
    public sealed class HubMatchSettingsPopup : HubScreen
    {
        public override bool WearsBoard => true;
        public override bool IsPopup => true;
        private HubButton _close;
        public override Selectable FirstFocus => _close;

        public override void Build()
        {
            var host = Hub.Host;
            var rules = SceneFlow.SelectedRules;
            bool classic = SceneFlow.SelectedMode == GameMode.Classic;
            var facts = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("MAP", SceneFlow.PreviewFor(SceneFlow.SelectedMap).Name.ToUpperInvariant()),
                new KeyValuePair<string, string>("MODE", classic ? "CLASSIC" : "HERO STRIKE"),
            };
            if (rules != null)
            {
                facts.Add(new KeyValuePair<string, string>("ROUNDS", rules.Rounds.ToString()));
                facts.Add(new KeyValuePair<string, string>("ROUND LENGTH", rules.RoundSeconds + " S"));
                if (rules.ScoreTarget > 0) facts.Add(new KeyValuePair<string, string>("SCORE TARGET", rules.ScoreTarget.ToString()));
                if (classic) facts.Add(new KeyValuePair<string, string>("SLIPPERS EACH", rules.Tsinelas.ToString()));
            }
            facts.Add(new KeyValuePair<string, string>("NETWORK", host.RoomOnline ? "ONLINE" : "LAN"));
            float rowH = 78, height = 370 + facts.Count * rowH;
            var panel = HubKnit.Modal(Root, "MATCH SETTINGS", 860, height, 84);
            var set = HubKnit.Words(panel, "Line", "Set by the host.", 30, HubKnit.KitInk, TextAnchor.MiddleCenter, false);
            HubKnit.In(set.rectTransform, 0, 124, 860, 40);
            for (int i = 0; i < facts.Count; i++)
            {
                float y = 214 + i * rowH;
                var key = HubKnit.Display(panel, "Key" + i, facts[i].Key, 34, HubKnit.KitTanDark);
                HubKnit.In(key.rectTransform, 70, y - 30, 360, 60);
                var value = HubKnit.Display(panel, "Value" + i, facts[i].Value, 38, HubKnit.KitMaroon, TextAnchor.MiddleRight);
                HubKnit.In(value.rectTransform, 430, y - 30, 360, 60);
                HubKit.Fit(value, 360);
                if (i < facts.Count - 1)
                {
                    var rule = HubKit.Rect(panel, "Rule" + i).gameObject.AddComponent<Image>();
                    rule.color = HubKnit.KitTan;
                    rule.raycastTarget = false;
                    HubKnit.In(rule.rectTransform, 60, y + 37, 740, 4);
                }
            }
            _close = HubKnit.Button(panel, "CloseSettings", "GOT IT", Knit.Go, Close, 48, HubGlyph.Mark.None, primary: true);
            HubKit.Place((RectTransform)_close.transform, HubKit.BottomRight, new Vector2(-70, 46), new Vector2(340, 108));
            var full = HubKnit.Button(panel, "FullRules", "FULL RULES", Knit.Yellow, () => { Close(); Hub.Host.OpenCustomRules(); }, 42);
            HubKit.Place((RectTransform)full.transform, HubKit.BottomLeft, new Vector2(70, 46), new Vector2(340, 108));
            HubKnit.FitLabel(full);
        }

        public override bool Back() { Close(); return true; }

        public override void Tick()
        {
            if (!Hub.Host.InRoom || Hub.Host.MatchInProgress) Close();
        }
    }

    /// <summary>
    /// The host's "close this room?" question. Named answers, never YES/NO, and the safe one has
    /// focus, so an Escape or a mashed A keeps the room (NN/g's confirmation rules, quoted in
    /// `docs/reports/lobby-ui-2026-10-28/ux-research.md`). Back is KEEP ROOM.
    /// </summary>
    public sealed class HubRoomClosePopup : HubScreen
    {
        public override bool WearsBoard => true;
        public override bool IsPopup => true;
        public int Others;
        private HubButton _keep;
        private bool _closing;
        public override Selectable FirstFocus => _keep;

        public override void Build()
        {
            // The question on a cream popup: the safe answer in lime with focus, leaving in red.
            var panel = HubKnit.Modal(Root, "CLOSE ROOM?", 860, 400);
            string who = Others <= 0 ? "Nobody else is in it yet."
                : Others == 1 ? "The other player in it goes back to the menu."
                : "The " + Others + " other players in it go back to the menu.";
            var message = HubKnit.Words(panel, "Consequence", "Leaving closes this room. " + who, 30, HubKnit.KitInk, TextAnchor.MiddleCenter, false);
            HubKnit.In(message.rectTransform, 50, 136, 760, 80);
            _keep = HubKnit.Button(panel, "KeepRoom", "KEEP ROOM", Knit.Go, Close, 46, HubGlyph.Mark.None, primary: true);
            HubKnit.In((RectTransform)_keep.transform, 70, 240, 340, 112);
            var close = HubKnit.Button(panel, "CloseRoom", "CLOSE ROOM", Knit.Danger, CloseRoom, 46);
            HubKnit.In((RectTransform)close.transform, 450, 240, 340, 112);
            HubKnit.FitLabel(close);
        }

        private void CloseRoom()
        {
            if (_closing) return;
            _closing = true;
            Hub.Host.LeaveRoom();
            Hub.Home();
        }

        public override bool Back() { Close(); return true; }

        public override void Tick()
        {
            if (!_closing && (!Hub.Host.InRoom || Hub.Host.MatchInProgress)) Close();
        }
    }
}
