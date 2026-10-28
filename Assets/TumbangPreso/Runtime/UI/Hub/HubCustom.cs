using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// HOST GAME, the owner's "Custom Game Mode" sheet: lobby name; MAP, GAME MODE and VISIBILITY
    /// previous/next choices with a default map set; LAN or ONLINE; CREATE LOBBY. The selected map shows subtly
    /// behind the form and changes with the choice (the sheet's suggestion 2, "mababa lang opacity
    /// para lang alam nila ano yung possible look nung map").
    ///
    /// THE FOUR ANSWERS:
    ///   the one thing   CREATE LOBBY
    ///   first press     CREATE LOBBY works at once: the name is pre-filled and the map defaults to
    ///                   the last one played (the sheet's suggestion 1)
    ///   not needed now  match rules, which are the lobby's settings icon once the room exists
    ///   out             BACK or Escape to GAMEMODE SELECT
    /// </summary>
    public sealed class HubHost : HubScreen
    {
        public override bool WearsBoard => true;
        public override float CourtShade => 0.62f;

        private InputField _name;
        private HubArrowChoice _map, _mode, _visibility;
        private HubButton _lan, _online, _create;
        private bool _onlineChosen;
        private Text _status;
        private bool _busy;

        private static readonly string[] Visibility = { "PUBLIC", "FRIENDS ONLY", "PRIVATE" };

        public override void Build()
        {
            HubChrome.Back(Root, Hub);
            HubChrome.Title(Root, "HOST GAME");

            var panel = HubKit.Place(HubKit.Rect(Root, "Form"), HubKit.Left, new Vector2(HubKit.Margin, -30), new Vector2(820, 760));
            // ⚠️ THE ORGANISER'S CLIPBOARD (`HubScenery`, 2026-09-24): whoever calls the game holds
            // the list, so the form is a hardboard clipboard with its clip, held up over the court.
            var plate = HubKit.Shape(panel, "Plate", HubStyle.Lit(HubScenery.Plank, 0.35f), false, 901, 6, 22);
            HubKit.Stretch(plate.rectTransform);
            plate.ShadowOffset = new Vector2(10, -12);
            plate.ShadowColor = new Color(HubStyle.Ink.r, HubStyle.Ink.g, HubStyle.Ink.b, 0.6f);
            HubScenery.Clip(panel);
            panel.localRotation = Quaternion.Euler(0, 0, 0.8f);

            string me = GameServices.Account?.DisplayName;
            if (string.IsNullOrWhiteSpace(me)) me = Settings.SettingsStore.Current.PlayerName;
            if (string.IsNullOrWhiteSpace(me)) me = "PLAYER";

            HubField.Label(panel, "LOBBY NAME", new Vector2(44, -34));
            _name = HubField.Build(panel, "LobbyName", (me + "'s room").ToUpperInvariant(), "Name your room",
                                   Settings.GameSettings.RoomTitleMax, 902);
            HubKit.Place((RectTransform)_name.transform, HubKit.TopLeft, new Vector2(40, -76), new Vector2(740, 92));

            string[] maps = new string[SceneFlow.MapRegistry.Length];
            int mapIndex = 0;
            for (int i = 0; i < maps.Length; i++)
            {
                maps[i] = SceneFlow.MapRegistry[i].Name;
                if (SceneFlow.MapRegistry[i].Id == SceneFlow.SelectedMap) mapIndex = i;
            }
            HubField.Label(panel, "MAP", new Vector2(44, -190));
            _map = HubArrowChoice.Build(panel, "Map", maps, mapIndex, 903);
            HubKit.Place((RectTransform)_map.transform, HubKit.TopLeft, new Vector2(40, -232), new Vector2(740, 92));
            _map.Changed += i => Hub.Host.SelectMap(SceneFlow.MapRegistry[i].Id);

            HubField.Label(panel, "GAME MODE", new Vector2(44, -346), 360);
            _mode = HubArrowChoice.Build(panel, "Mode", new[] { "CLASSIC", "HERO STRIKE" },
                                      SceneFlow.SelectedMode == GameMode.Classic ? 0 : 1, 904);
            HubKit.Place((RectTransform)_mode.transform, HubKit.TopLeft, new Vector2(40, -388), new Vector2(360, 92));

            HubField.Label(panel, "VISIBILITY", new Vector2(424, -346), 360);
            _visibility = HubArrowChoice.Build(panel, "Visibility", Visibility, 0, 905);
            HubKit.Place((RectTransform)_visibility.transform, HubKit.TopLeft, new Vector2(420, -388), new Vector2(360, 92));

            HubField.Label(panel, "NETWORK", new Vector2(44, -502));
            _lan = HubKit.Button(panel, "LanChoice", "LAN", HubStyle.Honey, () => Network(false), HubStyle.Label, 906, HubGlyph.Mark.House);
            HubKit.Place((RectTransform)_lan.transform, HubKit.TopLeft, new Vector2(40, -544), new Vector2(360, 92));
            _online = HubKit.Button(panel, "OnlineChoice", "ONLINE", HubStyle.Honey, () => Network(true), HubStyle.Label, 907, HubGlyph.Mark.Globe);
            HubKit.Place((RectTransform)_online.transform, HubKit.TopLeft, new Vector2(420, -544), new Vector2(360, 92));

            _status = HubKit.Text(panel, "Status", "", HubStyle.Floor, false, HubStyle.HoneySoft, TextAnchor.MiddleLeft);
            HubKit.Place(_status.rectTransform, HubKit.BottomLeft, new Vector2(44, 26), new Vector2(740, 96));

            _create = HubKit.Button(Root, "CreateLobby", "CREATE LOBBY", HubStyle.Chartreuse, Create, HubStyle.Title, 908);
            HubKit.Place((RectTransform)_create.transform, HubKit.BottomRight, new Vector2(-HubKit.Margin, HubKit.Margin), new Vector2(520, 140));
            _create.Shape.BandFraction = 0.12f;

            Network(false);
            Hub.Host.SelectMap(SceneFlow.MapRegistry[mapIndex].Id);
            HubSlap.On(panel, 0, -1);
            HubSlap.On(_create.transform, 0.1f, 2);
        }

        private void Network(bool online)
        {
            _onlineChosen = online;
            HubKit.SetFill(_lan, online ? HubStyle.Honey : HubStyle.Persimmon);
            HubKit.SetFill(_online, online ? HubStyle.Persimmon : HubStyle.Honey);
            _status.text = online ? "Anyone with the code can join, anywhere. Needs the internet."
                                  : "Players on the same Wi-Fi or cable network can join. No internet needed.";
        }

        private async void Create()
        {
            if (_busy) return;
            _busy = true;
            _create.interactable = false;
            HubKit.SetLabel(_create, "OPENING...");
            _status.text = _onlineChosen ? "Opening an online room..." : "Opening a room on your network...";
            string title = string.IsNullOrWhiteSpace(_name.text) ? _name.placeholder.GetComponent<Text>().text : _name.text.Trim();
            string refusal = await Hub.Host.HostRoom(title, SceneFlow.MapRegistry[_map.Value].Id,
                                                     _mode.Value == 0 ? GameMode.Classic : GameMode.HeroStrike,
                                                     (RoomVisibility)_visibility.Value, _onlineChosen);
            if (this == null || !_busy) return;
            _busy = false;
            _create.interactable = true;
            HubKit.SetLabel(_create, "CREATE LOBBY");
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
    /// JOIN GAME: SOURCE on the left (Dedicated (Internet), Dedicated (LAN), Code); on the right one
    /// server list look for both networks with the header saying which, or the code field.
    /// "Same lang look if online or lan lalabas, mag change lang yung sa taas" (the sheet's own note).
    ///
    /// ⚠️ EVERY JOIN GOES THROUGH `LobbyJoinPanel.Connect`, the path the old panel pressed, so codes,
    /// addresses, LAN rows and online rows keep every guard they had.
    /// </summary>
    public sealed class HubJoin : HubScreen
    {
        public override bool WearsBoard => true;
        public override float CourtShade => 0.9f;
        public string JoinCodeOnOpen { get; set; }
        private int _source;     // 0 internet, 1 LAN, 2 code
        private HubButton[] _sources;
        private RectTransform _list, _code;
        private Text _header, _status, _empty;
        private RectTransform _emptyCan;
        private InputField _codeField;
        private HubButton _refresh;
        private float _refreshUntil = float.NegativeInfinity;
        private bool _refreshShown;
        private bool _busy;
        private float _nextDraw;

        /// <summary>
        /// How long REFRESH reads REFRESHING... and ignores presses. A LAN host answers the query within
        /// a frame or two and broadcasts every second; the online list asks UGS at once. The hold is what
        /// stops a held button or a mashed pad A from becoming a query per frame.
        /// </summary>
        public const float RefreshHoldSeconds = 1.5f;

        /// <summary>The LAN list while a REFRESH has emptied it and the answers are on their way.</summary>
        public const string LanRefreshingMessage = "Looking for rooms on your network...";

        /// <summary>The row's tag where JOIN would be, for a host on another protocol (beacon v3).</summary>
        public const string DifferentVersionLabel = "DIFFERENT\nVERSION";
        private const int VisibleRooms = 5;
        private readonly List<HubRoom> _drawnRooms = new List<HubRoom>(VisibleRooms);
        private string _drawnEmptyMessage;
        private int _drawnSource = -1;

        public override void Build()
        {
            // ⚠️ A HOLLOW-BLOCK WALL (`HubScenery`): the barangay wall where a game gets called, with
            // the two panels taped to it like notices, rather than the maroon every screen shared.
            HubScenery.BlockGround(Root, 61);
            HubChrome.Back(Root, Hub);
            HubScenery.SprayTitle(HubChrome.Title(Root, "JOIN GAME"));

            var left = HubKit.Place(HubKit.Rect(Root, "Sources"), HubKit.TopLeft, new Vector2(HubKit.Margin, -(HubKit.Margin + 150)), new Vector2(440, 520));
            var sourcePlate = HubKit.Shape(left, "Plate", HubStyle.Night, false, 911, 5, 26);
            HubKit.Stretch(sourcePlate.rectTransform);
            var sourceTitle = HubKit.Text(left, "SourceLabel", "SOURCE", HubStyle.Floor, false, HubStyle.Golden, TextAnchor.MiddleLeft);
            HubKit.Place(sourceTitle.rectTransform, HubKit.TopLeft, new Vector2(34, -24), new Vector2(360, 48));
            // Two lines, so the owner's words fit at full size instead of squeezing onto one.
            string[] names = { "DEDICATED\n(INTERNET)", "DEDICATED\n(LAN)", "CODE" };
            HubGlyph.Mark[] marks = { HubGlyph.Mark.Globe, HubGlyph.Mark.House, HubGlyph.Mark.Key };
            _sources = new HubButton[3];
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                _sources[i] = HubKit.Button(left, "Source" + i, names[i], HubStyle.Honey, () => Source(index), HubStyle.Body, 912 + i, marks[i]);
                HubKit.Place((RectTransform)_sources[i].transform, HubKit.TopLeft, new Vector2(30, -(80 + i * 130)), new Vector2(380, 110));
                HubKit.Fit(HubKit.LabelOf(_sources[i]), 270);
            }

            var right = HubKit.Span(HubKit.Rect(Root, "Servers"), Vector2.zero, Vector2.one,
                                    new Vector2(HubKit.Margin + 480, HubKit.Margin + 90), new Vector2(HubKit.Margin, HubKit.Margin + 150));
            var plate = HubKit.Shape(right, "Plate", HubStyle.Night, false, 920, 5, 26);
            HubKit.Stretch(plate.rectTransform);
            _header = HubKit.Text(right, "Heading", "", HubStyle.Title, true, HubStyle.Golden, TextAnchor.MiddleLeft);
            HubKit.Place(_header.rectTransform, HubKit.TopLeft, new Vector2(34, -20), new Vector2(900, 64));
            // ⚠️ REFRESH SITS ON THE LIST IT REFRESHES (owner 2026-10-20, "add a refresh button on server
            // list to refresh the servers"), at the heading's right, above the columns, so it reads as the
            // list's own control and not as a fourth SOURCE. It is a sticker button like every other one
            // here: the hub's focus path reaches it on a pad and a thumb presses it. Hidden for CODE.
            // ⚠️ PLACED FROM THE LEFT, OVER THE JOIN COLUMN, NOT IN THE PANEL'S CORNER. The rows are 1180
            // wide from the list's left (30), so JOIN's right edge is 1196 at every window shape; pinned
            // to the panel's corner, REFRESH sat under the corner tape at 16:9 and drifted 400 units
            // right of the rows on the owner's short wide window (first renders, 2026-10-20). Here it
            // spans 956 to 1196: clear of the heading (34 to 934) and of the tape, whose left end starts
            // at 1195 on the narrowest panel (1328 wide) and sits above y -12 there.
            _refresh = HubKit.Button(right, "RefreshServers", "REFRESH", HubStyle.Honey, RefreshServers, HubStyle.Label, 921);
            HubKit.Place((RectTransform)_refresh.transform, HubKit.TopLeft, new Vector2(956, -16), new Vector2(240, 72));

            _list = HubKit.Span(HubKit.Rect(right, "List"), Vector2.zero, Vector2.one, new Vector2(30, 30), new Vector2(30, 150));
            var columns = HubKit.Place(HubKit.Rect(right, "Columns"), HubKit.TopLeft, new Vector2(34, -96), new Vector2(1100, 40));
            Column(columns, "NAME", 0);
            Column(columns, "MAP", 470);
            Column(columns, "PLAYERS", 760);
            // ⚠️ ONE STATE LINE, IN THE READING FACE (2026-09-23 UI review). The empty list said
            // "Looking for ..." and "Nobody yet" at once, in the display face at body size: two
            // claims that contradict each other (still searching, already empty) in the face meant
            // for names and verbs.
            _empty = HubKit.Text(_list, "EmptyState", "", HubStyle.Label, false, HubStyle.HoneySoft, TextAnchor.MiddleCenter);
            HubKit.Stretch(_empty.rectTransform, 40);
            // ⚠️ AN EMPTY LIST IS A LATA STANDING ALONE IN ITS CIRCLE (2026-09-24): the game's own
            // picture of "nobody is playing yet", chalked above the sentence, gone once rooms appear.
            _emptyCan = HubKit.Rect(_empty.transform, "EmptyCan");
            HubKit.Place(_emptyCan, HubKit.Centre, new Vector2(0, 120), new Vector2(150, 150));
            var ring = HubScenery.ChalkRing(_emptyCan, "Circle", 0.2f);
            HubKit.Place(ring.rectTransform, HubKit.Bottom, new Vector2(0, -6), new Vector2(210, 60));
            ring.color = new Color(HubStyle.Honey.r, HubStyle.Honey.g, HubStyle.Honey.b, 0.35f);
            var can = HubKit.Glyph(_emptyCan, "Can", HubGlyph.Mark.Can, new Color(HubStyle.Honey.r, HubStyle.Honey.g, HubStyle.Honey.b, 0.45f), 0.07f);
            HubKit.Stretch(can.rectTransform, 12);

            _code = HubKit.Span(HubKit.Rect(right, "CodeEntry"), Vector2.zero, Vector2.one, new Vector2(30, 30), new Vector2(30, 110));
            HubField.Label(_code, "ENTER CODE", new Vector2(10, -40));
            _codeField = HubField.Build(_code, "CodeField", "", "ABCD", 21, 930);
            HubKit.Place((RectTransform)_codeField.transform, HubKit.TopLeft, new Vector2(6, -84), new Vector2(560, 110));
            _codeField.textComponent.fontSize = HubStyle.Size(HubStyle.Display);
            _codeField.onValidateInput += (text, index, c) => char.ToUpperInvariant(c);
            var join = HubKit.Button(_code, "JoinByCode", "JOIN", HubStyle.Chartreuse, () => Join(_codeField.text), HubStyle.Display, 931);
            HubKit.Place((RectTransform)join.transform, HubKit.TopLeft, new Vector2(600, -84), new Vector2(300, 110));
            var codeHint = HubKit.Text(_code, "CodeHint", "Four characters, from whoever is hosting. An address like 192.168.1.20 works too.",
                                       HubStyle.Floor, false, HubStyle.HoneySoft, TextAnchor.UpperLeft);
            HubKit.Place(codeHint.rectTransform, HubKit.TopLeft, new Vector2(10, -220), new Vector2(900, 90));

            _status = HubKit.Text(Root, "Status", "", HubStyle.Body, false, HubStyle.Honey, TextAnchor.MiddleLeft);
            HubKit.Place(_status.rectTransform, HubKit.BottomLeft, new Vector2(HubKit.Margin + 480, HubKit.Margin + 20), new Vector2(1300, 50));
            HubScenery.Tape(left, "Tape", new Vector2(0.5f, 1), new Vector2(0, -2), -5, 140);
            HubScenery.Tape(right, "TapeLeft", new Vector2(0, 1), new Vector2(60, -4), 12, 140);
            HubScenery.Tape(right, "TapeRight", new Vector2(1, 1), new Vector2(-60, -4), -9, 140);

            HubSlap.On(left, 0, -1);
            HubSlap.On(right, 0.06f, 1);
            if (!string.IsNullOrWhiteSpace(JoinCodeOnOpen))
            {
                string code = JoinCodeOnOpen;
                JoinCodeOnOpen = null;
                Source(2);
                _codeField.SetTextWithoutNotify(code);
                Join(code);
            }
            else
            {
                Source(0);
            }
        }

        private static void Column(RectTransform parent, string words, float x)
        {
            var t = HubKit.Text(parent, "Col_" + words, words, HubStyle.Floor, false, HubStyle.HoneySoft, TextAnchor.MiddleLeft);
            HubKit.Place(t.rectTransform, HubKit.TopLeft, new Vector2(x, 0), new Vector2(260, 48));
        }

        private void Source(int index)
        {
            _source = index;
            for (int i = 0; i < _sources.Length; i++) HubKit.SetFill(_sources[i], i == index ? HubStyle.Persimmon : HubStyle.Honey);
            bool code = index == 2;
            if (!code) Hub.Host.Browse();
            _code.gameObject.SetActive(code);
            _list.gameObject.SetActive(!code);
            _list.parent.Find("Columns").gameObject.SetActive(!code);
            _refresh.gameObject.SetActive(!code);
            _refreshUntil = float.NegativeInfinity;
            ShowRefresh();
            _header.text = code ? "JOIN BY CODE" : index == 0 ? "SERVERS (ONLINE)" : "SERVERS (LAN)";
            _drawnSource = -1;
            Draw();
            if (code) UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(_codeField.gameObject);
            Hub.RefreshFocus();
        }

        public override void Tick()
        {
            if (_busy) ShowJoining();
            if (_refreshShown != Refreshing) { ShowRefresh(); _nextDraw = 0; }
            if (Time.unscaledTime < _nextDraw) return;
            _nextDraw = Time.unscaledTime + 1.0f;
            if (_source == 1) Net.NetSession.Instance?.Beacon?.RequestDiscovery();
            Draw();
        }

        private bool Refreshing => Time.unscaledTime < _refreshUntil;

        /// <summary>REFRESH: re-ask the network for the list on screen. See <see cref="RefreshHoldSeconds"/>.</summary>
        private void RefreshServers()
        {
            if (_source == 2 || Refreshing) return;
            _refreshUntil = Time.unscaledTime + RefreshHoldSeconds;
            Hub.Host.RefreshRooms(_source == 1);
            ShowRefresh();
            _drawnSource = -1;
            Draw();
        }

        private void ShowRefresh()
        {
            _refreshShown = Refreshing;
            HubKit.SetLabel(_refresh, _refreshShown ? "REFRESHING..." : "REFRESH");
            HubKit.Fit(HubKit.LabelOf(_refresh), 204);
            _refresh.interactable = !_refreshShown;
        }

        private void Draw()
        {
            if (_source == 2) return;
            var rooms = Hub.Host.Rooms(_source == 1);
            string emptyMessage = _source == 1
                ? Refreshing ? LanRefreshingMessage : "No rooms on your network yet. Host one, or join with a code."
                : Hub.Host.OnlineBrowserMessage;
            int visible = Mathf.Min(rooms.Count, VisibleRooms);
            bool same = _drawnSource == _source && _drawnRooms.Count == visible
                && (visible > 0 || _drawnEmptyMessage == emptyMessage);
            for (int i = 0; same && i < visible; i++) same = _drawnRooms[i].SameListing(rooms[i]);
            if (same) return;
            _drawnSource = _source;
            _drawnEmptyMessage = emptyMessage;
            _drawnRooms.Clear();
            for (int i = 0; i < visible; i++) _drawnRooms.Add(rooms[i]);

            for (int i = _list.childCount - 1; i >= 0; i--)
                if (_list.GetChild(i) != _empty.transform) Destroy(_list.GetChild(i).gameObject);
            _empty.text = rooms.Count == 0 ? emptyMessage : "";
            _emptyCan.gameObject.SetActive(rooms.Count == 0);

            for (int i = 0; i < visible; i++)
            {
                var room = rooms[i];
                var row = HubKit.Place(HubKit.Rect(_list, "Room" + i), HubKit.TopLeft, new Vector2(0, -(i * 104)), new Vector2(1180, 92));
                var plate = HubKit.Shape(row, "Plate", HubStyle.Night, false, 940 + i, 4, 16);
                HubKit.Stretch(plate.rectTransform);
                var name = HubKit.Text(row, "Name", room.Name, HubStyle.Label, true, HubStyle.Honey, TextAnchor.MiddleLeft);
                HubKit.Place(name.rectTransform, HubKit.Left, new Vector2(20, 0), new Vector2(440, 70));
                HubKit.Fit(name, 440);
                var map = HubKit.Text(row, "Map", room.Map, HubStyle.Body, false, HubStyle.Honey, TextAnchor.MiddleLeft);
                HubKit.Place(map.rectTransform, HubKit.Left, new Vector2(474, 0), new Vector2(270, 60));
                HubKit.Fit(map, 270);
                var players = HubKit.Text(row, "Players", room.InProgress ? "IN A MATCH" : room.Players + " / " + room.Capacity, HubStyle.Body, false, HubStyle.Honey, TextAnchor.MiddleLeft);
                HubKit.Place(players.rectTransform, HubKit.Left, new Vector2(764, 0), new Vector2(200, 60));
                // ⚠️ A HOST ON ANOTHER PROTOCOL GETS NO JOIN AT ALL, NOT A GREYED ONE (LAN-VPN-JOIN-1020).
                // Pressing it could only be refused at approval after the wait; a disabled JOIN would still
                // say "this is a game you may join later", and a full room's does mean exactly that. The tag
                // says why in the place the press would have been, and the focus path skips it.
                if (room.DifferentVersion)
                {
                    // 84 tall, not JOIN's 72: two lines of the reading face at the floor size measure 76
                    // (`TumpUiCapture`'s clip check, 2026-10-20), and the row is 92.
                    var tag = HubKit.Text(row, "Version", DifferentVersionLabel, HubStyle.Floor, false, HubStyle.HoneySoft, TextAnchor.MiddleCenter);
                    HubKit.Place(tag.rectTransform, HubKit.Right, new Vector2(-14, 0), new Vector2(180, 84));
                    HubKit.Fit(tag, 180);
                    continue;
                }
                string key2 = room.Key;
                var join = HubKit.Button(row, "Join" + i, "JOIN", HubStyle.Chartreuse, () => Join(key2), HubStyle.Label, 950 + i);
                HubKit.Place((RectTransform)join.transform, HubKit.Right, new Vector2(-14, 0), new Vector2(180, 72));
                join.interactable = room.IsJoinable;
            }
            Hub.RefreshFocus();
        }

        // ⚠️ THE WAIT IS VISIBLE AND ALWAYS ENDS (2026-10-20, a LAN row over Hamachi: "it's stuck on
        // joining"). The line held "Joining <address>..." unchanged for the transport's ~24 s of retries,
        // which reads as a hang; it now counts the seconds and, after `JoinFailureRules.NoAnswerHintSeconds`,
        // names the usual cause. And this is `async void`: anything thrown before the attempt's own catch
        // (leaving the old room, naming the listed one) used to leave `_busy` set and the line frozen for good.
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
            _status.fontSize = HubStyle.Size(HubStyle.Body); HubKit.Fit(_status, 1300);
        }

        public override bool Back()
        {
            if (_busy) { _busy = false; Hub.Host.LeaveRoom(); }
            return false;
        }
    }

    /// <summary>
    /// GAME LOBBY, the custom room. Rebuilt 2026-10-28 from the room UX pass
    /// (`docs/reports/lobby-ui-2026-10-28/ux-research.md`): one room for every role, where the
    /// host gets controls in the places everyone else gets a summary.
    ///
    ///   host       players panel on the left, the MATCH card on the right (map previous/next,
    ///              mode, network, rules, the LAN firewall notice), START GAME
    ///   joined     the players panel alone, centred, under a one-line summary of the match
    ///              (owner, 2026-10-28: "summary nalang dapat pag joined"), READY
    ///   spectator  the joined view with no primary; the status line says what happens next
    ///
    /// CHAT opens in the right-hand column: in place of the host's MATCH card, or beside a joined
    /// player's panel, which slides over to make room. SPECTATE stays in the players' heading.
    ///
    /// THE FOUR ANSWERS:
    ///   the one thing   the players, n of 4
    ///   first press     START GAME for the host; READY for everyone else, in the same place
    ///   not needed now  rules and chat, one press away
    ///   out             BACK or Escape: a player leaves; the host is asked first, because
    ///                   leaving closes the room for everyone in it
    /// </summary>
    public sealed class HubLobby : HubScreen
    {
        public override bool WearsBoard => true;
        internal const float RoomContentWidth = 1808;
        internal const float RoomSeatWidth = 900;
        internal const float RoomColumnGap = 24;
        internal const float RoomChatWidth = RoomContentWidth - RoomSeatWidth - RoomColumnGap;
        internal const float RoomChatLeft = -RoomContentWidth * .5f + RoomSeatWidth + RoomColumnGap;
        /// <summary>Both columns stand on the same line above the primary, measured from the bottom,
        /// so a taller canvas (4:3) adds sky above the room instead of pulling the columns apart.</summary>
        internal const float RoomColumnBottom = 256;
        internal const float RoomColumnHeight = 560;
        internal const float RoomSeatsOffset = -(RoomContentWidth - RoomSeatWidth) * .5f;
        private const float RowPitch = 108, RowHeight = 96;
        public override float CourtShade => 0.3f;
        private Text _title, _code, _count, _status, _address, _summary, _mode, _network;
        private RectTransform _rows, _playersPanel, _matchCard, _summaryStrip;
        private HubButton _primary, _watch, _rules, _viewRules, _chatDoor, _codeChip;
        private HubArrowChoice _mapChoice;
        private GameObject _unread;
        private bool _chatShown;
        private bool _hostLayout;
        private int _seenLines;
        private float _panelX;

        // ⚠️⚠️ THE HOST'S FIREWALL LINE (LAN-VPN-JOIN-1020). A zip copy of the game hosting a LAN
        // room can be listed by every player on a VPN and answer none of them, because Windows'
        // first-run prompt allows it on Private networks only and files Hamachi and Radmin under
        // Public. `LanFirewall` reads, without admin, whether this program is reachable on the
        // active networks; when it is not, the room says so in one line, with ALLOW LAN PLAY, which
        // asks Windows (UAC) to add the installer's rule. An installed copy reads Open and shows
        // nothing, and so does an online room, a client and every non-Windows player.
        // ⚠️ IT LIVES IN THE MATCH CARD UNDER NETWORK, the fact it is about, and steps aside with
        // the card while the chat holds that column.
        internal const float FirewallWidth = 824;
        private RectTransform _firewall;
        private Text _firewallLine;
        private HubButton _firewallAllow;
        private string _firewallDrawn = "";
        private int _firewallAllowedSeen;

        public override Selectable FirstFocus => _primary != null && _primary.gameObject.activeInHierarchy ? _primary : (Selectable)_watch;
        private string _drawn = "";

        public override void Build()
        {
            // ⚠️ THE ROOM IS A GATHERING, SO IT GETS THE FIESTA'S BUNTING (`HubScenery`), strung
            // behind the panels and stirring on the same breeze as the title street.
            HubScenery.Bunting(Root, 7, 200, 0.1f);
            HubChrome.Back(Root, Hub);
            _title = HubChrome.Title(Root, "LOBBY");

            // The code is the one fact a host reads out loud, so it is the big chip; the LAN address
            // rides under it on the same sticker instead of floating over the scenery.
            _codeChip = HubKit.Button(Root, "CodeChip", null, HubStyle.Night, CopyCode, 0, 961);
            HubKit.Place((RectTransform)_codeChip.transform, HubKit.TopRight, new Vector2(-HubKit.Margin, -HubKit.Margin), new Vector2(460, 130));
            _code = HubKit.Text(_codeChip.Body, "Code", "", HubStyle.Title, true, HubStyle.Golden, TextAnchor.MiddleCenter);
            HubKit.Stretch(_code.rectTransform);
            _address = HubKit.Text(_codeChip.Body, "Address", "", HubStyle.Floor, false, HubStyle.HoneySoft, TextAnchor.MiddleCenter);
            HubKit.Place(_address.rectTransform, HubKit.Bottom, new Vector2(0, 14), new Vector2(420, 40));

            BuildPlayers();
            BuildMatchCard();
            BuildSummary();

            _primary = HubKit.Button(Root, "StartGame", "START GAME", HubStyle.Chartreuse, Primary, HubStyle.Title, 963);
            HubKit.Place((RectTransform)_primary.transform, HubKit.Bottom, new Vector2(0, HubKit.Margin), new Vector2(560, 130));
            _primary.Shape.BandFraction = 0.12f;
            _status = HubKit.Text(Root, "Status", "", HubStyle.Floor, false, HubStyle.Honey, TextAnchor.MiddleCenter);
            HubKit.Place(_status.rectTransform, HubKit.Bottom, new Vector2(0, HubKit.Margin + 140), new Vector2(1200, 48));
            _status.gameObject.AddComponent<Shadow>().effectColor = HubStyle.Ink;

            // Bottom right: the doors that leave the room's picture for a moment. CHAT carries a
            // persimmon dot while lines arrive unread; CHARACTER exists only where heroes are not
            // chosen together after START (offline rooms).
            _chatDoor = Door("ChatDoor", HubGlyph.Mark.Friends, "CHAT", 0, () => Hub.Host.ToggleChat());
            _unread = HubKit.Notice(_chatDoor.Body);
            _unread.SetActive(false);
            _seenLines = LobbyChat.RoomLineCount;
            _characterDoor = Door("CharacterDoor", HubGlyph.Mark.Person, "CHARACTER", 1, () => Hub.Push<HubCharacterSelect>(c => c.Timed = false));

            foreach (var words in new[] { _title, _count, _status, _firewallLine, _summary, _mode, _network })
                if (words != null) HubDepthText.On(words, TextDepth);

            Net.LanFirewall.Invalidate();
            _firewallAllowedSeen = Net.LanFirewall.AllowedCount;
            Draw(true);
            DrawFirewall();
            HubSlap.On(_playersPanel, 0, -1);
            HubSlap.On(_matchCard, 0.06f, 1);
        }

        private void BuildPlayers()
        {
            var panel = _playersPanel = HubKit.Place(HubKit.Rect(Root, "Players"), HubKit.Bottom,
                                                     new Vector2(0, RoomColumnBottom), new Vector2(RoomSeatWidth, RoomColumnHeight));
            var plate = HubKit.Shape(panel, "Plate", HubStyle.Night, false, 962, 6, 30);
            HubKit.Stretch(plate.rectTransform);
            plate.color = new Color(1, 1, 1, 0.93f);
            plate.Depth = PanelDepth;
            _count = HubKit.Text(panel, "Heading", "", HubStyle.Title, true, HubStyle.Golden, TextAnchor.MiddleLeft);
            HubKit.Place(_count.rectTransform, HubKit.TopLeft, new Vector2(36, -22), new Vector2(560, 64));
            _rows = HubKit.Span(HubKit.Rect(panel, "Rows"), Vector2.zero, Vector2.one, new Vector2(30, 24), new Vector2(30, 104));
            // ⚠️ SPECTATE STAYS IN THE PLAYERS' HEADING (owner 2026-10-28: "put spectate in the same
            // place", renamed from WATCH INSTEAD the same day). It is a seat choice, so it sits on the
            // list of seats.
            _watch = HubKit.Button(panel, "WatchToggle", "SPECTATE", HubStyle.Honey, () => { Hub.Host.ToggleSpectate(); _drawn = ""; },
                                   HubStyle.Floor, 968, HubGlyph.Mark.Eye);
            HubKit.Place((RectTransform)_watch.transform, HubKit.TopRight, new Vector2(-30, -20), new Vector2(300, 72));
        }

        /// <summary>The host's MATCH card: everything the host decides before START, in one place.</summary>
        private void BuildMatchCard()
        {
            var card = _matchCard = HubKit.Place(HubKit.Rect(Root, "MatchCard"), HubKit.Bottom,
                                                 new Vector2(RoomChatLeft + RoomChatWidth * .5f, RoomColumnBottom),
                                                 new Vector2(RoomChatWidth, RoomColumnHeight));
            var plate = HubKit.Shape(card, "Plate", HubStyle.Night, false, 972, 6, 30);
            HubKit.Stretch(plate.rectTransform);
            plate.color = new Color(1, 1, 1, 0.93f);
            plate.Depth = PanelDepth;
            var heading = HubKit.Text(card, "MatchHeading", "MATCH", HubStyle.Title, true, HubStyle.Golden, TextAnchor.MiddleLeft);
            HubKit.Place(heading.rectTransform, HubKit.TopLeft, new Vector2(36, -20), new Vector2(500, 60));
            HubDepthText.On(heading, TextDepth);

            // ⚠️ THE SAME PREVIOUS/NEXT SELECTOR AS HOST GAME (`HubArrowChoice`), on the same
            // `SelectMap` call the old CHANGE MAP popup made, so the room changes map in one press.
            string[] maps = new string[SceneFlow.MapRegistry.Length];
            for (int i = 0; i < maps.Length; i++) maps[i] = SceneFlow.MapRegistry[i].Name;
            HubField.Label(card, "MAP", new Vector2(40, -92));
            _mapChoice = HubArrowChoice.Build(card, "Map", maps, MapIndex(), 967);
            HubKit.Place((RectTransform)_mapChoice.transform, HubKit.TopLeft, new Vector2(30, -134), new Vector2(RoomChatWidth - 60, 92));
            _mapChoice.Changed += i => { Hub.Host.SelectMap(SceneFlow.MapRegistry[i].Id); _drawn = ""; };

            _mode = Fact(card, "Mode", "MODE", -244);
            _network = Fact(card, "Network", "NETWORK", -300);

            _rules = HubKit.Button(card, "SettingsDoor", "MATCH RULES", HubStyle.Honey, OpenRules, HubStyle.Label, 966, HubGlyph.Mark.Gear);
            HubKit.Place((RectTransform)_rules.transform, HubKit.TopLeft, new Vector2(30, -366), new Vector2(RoomChatWidth - 60, 84));

            _firewall = HubKit.Place(HubKit.Rect(card, "LanFirewall"), HubKit.TopLeft, new Vector2(30, -462), new Vector2(FirewallWidth, 84));
            _firewallLine = HubKit.Text(_firewall, "Line", "", HubStyle.Floor, false, HubStyle.Honey, TextAnchor.MiddleLeft);
            HubKit.Place(_firewallLine.rectTransform, HubKit.Left, new Vector2(6, 0), new Vector2(FirewallWidth - 340, 84));
            _firewallAllow = HubKit.Button(_firewall, "AllowLanPlay", LanFirewallRules.AllowLabel, HubStyle.Honey, AllowLanPlay,
                                           HubStyle.Floor, 969, HubGlyph.Mark.House);
            HubKit.Place((RectTransform)_firewallAllow.transform, HubKit.Right, Vector2.zero, new Vector2(320, 84));
            HubKit.Fit(HubKit.LabelOf(_firewallAllow), 220);
            _firewall.gameObject.SetActive(false);
        }

        /// <summary>A label on the left of a card line, its value on the right, both on one baseline.</summary>
        private static Text Fact(RectTransform card, string name, string label, float y)
        {
            var key = HubKit.Text(card, name + "Label", label, HubStyle.Floor, false, HubStyle.HoneySoft, TextAnchor.MiddleLeft);
            HubKit.Place(key.rectTransform, HubKit.TopLeft, new Vector2(40, y), new Vector2(260, 52));
            var value = HubKit.Text(card, name + "Value", "", HubStyle.Label, true, HubStyle.Honey, TextAnchor.MiddleRight);
            HubKit.Place(value.rectTransform, HubKit.TopRight, new Vector2(-40, y), new Vector2(520, 52));
            return value;
        }

        /// <summary>
        /// A joined player's or spectator's view of the match: one line above the seats, with the
        /// rules one press away and read-only (`CustomGameScreen.MayEdit`). It rides with the players'
        /// panel, so it moves with it when the chat opens.
        /// </summary>
        private void BuildSummary()
        {
            _summaryStrip = HubKit.Rect(_playersPanel, "MatchSummary");
            _summaryStrip.anchorMin = _summaryStrip.anchorMax = new Vector2(0.5f, 1);
            _summaryStrip.pivot = new Vector2(0.5f, 0);
            _summaryStrip.anchoredPosition = new Vector2(0, 16);
            _summaryStrip.sizeDelta = new Vector2(RoomSeatWidth, 64);
            var plate = HubKit.Shape(_summaryStrip, "Plate", HubStyle.Maroon, false, 973, 4, 18);
            HubKit.Stretch(plate.rectTransform);
            plate.Depth = RowDepth;
            var map = HubKit.Glyph(_summaryStrip, "Icon", HubGlyph.Mark.Globe, HubStyle.Golden);
            HubKit.Place(map.rectTransform, HubKit.Left, new Vector2(22, 0), new Vector2(36, 36));
            _summary = HubKit.Text(_summaryStrip, "Line", "", HubStyle.Label, true, HubStyle.Honey, TextAnchor.MiddleLeft);
            HubKit.Place(_summary.rectTransform, HubKit.Left, new Vector2(74, 0), new Vector2(RoomSeatWidth - 330, 56));
            _viewRules = HubKit.Button(_summaryStrip, "ViewRules", "VIEW RULES", HubStyle.Honey, OpenRules, HubStyle.Floor, 974, HubGlyph.Mark.Eye);
            HubKit.Place((RectTransform)_viewRules.transform, HubKit.Right, new Vector2(-6, 0), new Vector2(240, 56));
            HubKit.Fit(HubKit.LabelOf(_viewRules), 150);
        }

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

        // ⚠️⚠️ DEPTH ON EVERYTHING IN THE ROOM, owner 2026-10-17: *"increase depth for everything
        // such as using 4 shadows"*. Three heights, so the stack reads as layers rather than as one
        // blur everywhere: the panels float highest over the street, every button sits on them at
        // button height, and the seat rows sit lowest because they lie ON the panel.
        private const float PanelDepth = 48.0f, ButtonDepth = 28.0f, RowDepth = 16.0f, TextDepth = 10.0f;

        /// <summary>Give every surface in the room its height. Run after each draw, because the
        /// seat rows are rebuilt; a surface that already has one keeps it.
        /// ⚠️ A BOARD PIECE'S HATCH IS NOT A SURFACE (playtest 2026-10-23, "darked out lahat"). It is a
        /// clear shape over the piece's face (`HubBoardSkin.Dress`); given a depth, it stacked four ink
        /// shadows ON the face and dimmed every button and plate in the room to about half.</summary>
        private void Deepen()
        {
            foreach (var shape in Root.GetComponentsInChildren<HubShape>(true))
            {
                var piece = shape.transform.parent != null ? shape.transform.parent.GetComponent<HubShape>() : null;
                if (piece != null && piece.SkinHatch == shape) continue;
                if (shape.Depth <= 0.0f) shape.Depth = shape.Pressable ? ButtonDepth : RowDepth;
            }
        }

        private HubButton _characterDoor;

        private HubButton Door(string name, HubGlyph.Mark mark, string words, int index, System.Action action)
        {
            var door = HubKit.Button(Root, name, null, HubStyle.Honey, () =>
            {
                if (index != 0 && _chatShown) Hub.Host.ToggleChat();
                action();
            }, 0, 964 + index);
            // ⚠️ 176 WIDE, FOR "CHARACTER" AT THE 28 FLOOR, which overran a 150 door in the first capture.
            HubKit.Place((RectTransform)door.transform, HubKit.BottomRight, new Vector2(-(HubKit.Margin + index * 192), HubKit.Margin), new Vector2(176, 150));
            var glyph = HubKit.Glyph(door.Body, "Icon", mark, HubStyle.Ink, 0.1f);
            HubKit.Place(glyph.rectTransform, HubKit.Top, new Vector2(0, -12), new Vector2(80, 80));
            var label = HubKit.Text(door.Body, "Label", words, HubStyle.Floor, true, HubStyle.Ink, TextAnchor.MiddleCenter);
            HubKit.Place(label.rectTransform, HubKit.Bottom, new Vector2(0, 10), new Vector2(164, 40));
            HubKit.Fit(label, 160);
            return door;
        }

        /// <summary>
        /// The chat took or gave back the right-hand column. The host's MATCH card steps aside for
        /// it; a joined player's panel slides over (<see cref="Tick"/> eases it) so the seats never
        /// sit under the chat.
        /// </summary>
        public void SetChatPresented(bool shown)
        {
            if (_chatShown == shown) return;
            _chatShown = shown;
            if (shown) { _seenLines = LobbyChat.RoomLineCount; if (_unread != null) _unread.SetActive(false); }
            Layout(false);
            _firewallDrawn = "";
            DrawFirewall();
            Hub?.RefreshFocus();
        }

        private float PanelTarget => _hostLayout || _chatShown ? RoomSeatsOffset : 0;

        /// <summary>Place the columns for the role and the chat. Snaps on a rebuild, eases otherwise.</summary>
        private void Layout(bool snap)
        {
            if (_playersPanel == null) return;
            if (_matchCard != null) _matchCard.gameObject.SetActive(_hostLayout && !_chatShown);
            if (_summaryStrip != null) _summaryStrip.gameObject.SetActive(!_hostLayout);
            if (snap || HubStyle.ReducedMotion) _panelX = PanelTarget;
            _playersPanel.anchoredPosition = new Vector2(_panelX, RoomColumnBottom);
            if (_status != null)
            {
                bool column = _chatShown && !_hostLayout;
                _status.rectTransform.anchoredPosition = new Vector2(column ? RoomSeatsOffset : 0, HubKit.Margin + 140);
                _status.rectTransform.sizeDelta = new Vector2(column ? RoomSeatWidth : 1200, 48);
            }
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
            if (_playersPanel != null && !Mathf.Approximately(_panelX, PanelTarget))
            {
                // A short ease rather than a jump, so the eye follows the seats to their new place.
                _panelX = HubStyle.ReducedMotion ? PanelTarget
                    : Mathf.MoveTowards(Mathf.Lerp(_panelX, PanelTarget, 1 - Mathf.Exp(-Time.unscaledDeltaTime * 18)), PanelTarget, Time.unscaledDeltaTime * 400);
                _playersPanel.anchoredPosition = new Vector2(_panelX, RoomColumnBottom);
            }
            if (_unread != null)
            {
                if (_chatShown) _seenLines = LobbyChat.RoomLineCount;
                bool waiting = !_chatShown && LobbyChat.RoomLineCount > _seenLines;
                if (_unread.activeSelf != waiting) _unread.SetActive(waiting);
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
            _firewallLine.rectTransform.sizeDelta = new Vector2(offer ? FirewallWidth - 340 : FirewallWidth - 12, 84);
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
            string key = host.RoomTitle + host.RoomCode + host.IsHost + host.LocalReady + SceneFlow.SelectedMap + SceneFlow.SelectedMode + host.Spectating + host.MatchInProgress + host.SeatSwapOffer?.Id + host.SeatSwapResult + HubQueueWatch.QueueRoom + host.RoomOnline + host.RoomAddress;
            key += host.CanEditBots;
            key += Net.MatchRpc.Instance != null && (Net.MatchRpc.Instance.CharacterSelecting || Net.MatchRpc.Instance.QueueMapVoting);
            foreach (var s in seats) key += s.Occupied + ":" + s.Name + ":" + s.CharacterPick + ":" + s.Ready + ":" + s.Bot + ":" + s.Host + ":" + host.CanTakeSeat(s.Slot) + "|";
            if (!force && key == _drawn) return;
            _drawn = key;

            bool hostLayout = host.IsHost && !HubQueueWatch.QueueRoom;
            bool snap = force || hostLayout != _hostLayout;
            _hostLayout = hostLayout;
            Layout(snap);

            // Retitled with its plate, or the host's title runs off a plate sized for "LOBBY" (playtest 2026-10-23).
            HubChrome.Retitle(_title, string.IsNullOrWhiteSpace(host.RoomTitle) ? "LOBBY" : host.RoomTitle.ToUpperInvariant(), 760);
            _code.text = "CODE  " + (host.RoomCode ?? "");
            string address = host.RoomAddress;
            bool hasAddress = !string.IsNullOrEmpty(address);
            _address.text = hasAddress ? "LAN  " + address : "";
            _code.rectTransform.offsetMin = new Vector2(10, hasAddress ? 44 : 6);
            _code.rectTransform.offsetMax = new Vector2(-10, -6);

            HubKit.SetLabel(_watch, host.Spectating ? "TAKE A SEAT" : "SPECTATE");
            // The picture follows the words: an eye to go and watch, a person to come back and sit.
            var watchIcon = _watch.Body.Find("Icon")?.GetComponent<HubGlyph>();
            if (watchIcon != null && watchIcon.Kind != (host.Spectating ? HubGlyph.Mark.Person : HubGlyph.Mark.Eye))
            {
                watchIcon.Kind = host.Spectating ? HubGlyph.Mark.Person : HubGlyph.Mark.Eye;
                watchIcon.SetVerticesDirty();
            }
            HubKit.LabelOf(_watch).fontSize = HubStyle.Size(HubStyle.Floor);
            HubKit.Fit(HubKit.LabelOf(_watch), 210);

            string network = host.RoomOnline ? "ONLINE" : "LAN";
            if (_mapChoice != null && _mapChoice.Value != MapIndex()) _mapChoice.Set(MapIndex(), false);
            _mode.text = ModeName;
            _network.text = network;
            _summary.text = SceneFlow.PreviewFor(SceneFlow.SelectedMap).Name + "   ·   " + ModeName + "   ·   " + network;
            _summary.fontSize = HubStyle.Size(HubStyle.Label);
            HubKit.Fit(_summary, RoomSeatWidth - 330);

            int occupied = 0;
            foreach (var s in seats)
                if (s.Occupied) occupied++;
            _count.text = "PLAYERS  " + occupied + " / " + Balance.PlayerCount;

            DrawSeats(host, seats);

            if (host.IsHost)
            {
                // ⚠️ ALWAYS PRESSABLE FOR THE HOST. A custom room starts with whoever is in it,
                // and an OPEN seat is an empty chair (`ConvertedMatchSetup.StartGame`).
                HubKit.SetLabel(_primary, "START GAME");
                _primary.gameObject.SetActive(true);
                _primary.interactable = true;
                int open = 0;
                foreach (var s in seats) if (!s.Occupied && !s.Bot) open++;
                _status.text = open == 0 ? "" : open == 1 ? "One seat is open. It stays empty unless you add a bot."
                    : open + " seats are open. They stay empty unless you add bots.";
            }
            else if (host.Spectating)
            {
                // ⚠️ A SPECTATOR HAS NOTHING TO READY: the host refuses a spectator's READY
                // (`MatchRpc.HostDeclareReady`), so the button was a press that did nothing.
                _primary.gameObject.SetActive(false);
                _status.text = "You're spectating. The match plays out in front of you once the host starts it.";
            }
            else
            {
                HubKit.SetLabel(_primary, host.LocalReady ? "READY  ✓" : "READY");
                _primary.gameObject.SetActive(true);
                _primary.interactable = true;
                _status.text = host.LocalReady ? "You're ready. Waiting for the host to start." : "Press READY when you're set. The host starts the match.";
            }
            if (host.Spectating && Net.MatchRpc.Instance != null && Net.MatchRpc.Instance.CharacterSelecting)
                _status.text = "The players are choosing their heroes.";
            else if (host.Spectating && Net.MatchRpc.Instance != null && Net.MatchRpc.Instance.QueueMapVoting)
                _status.text = "The players are voting for the map.";
            // A spectator has no seat, so no hero to choose. In a networked room heroes are chosen together
            // in character select after START (owner, October 22: "baket u can pick character na agad?").
            if (_characterDoor != null)
                _characterDoor.gameObject.SetActive(!host.Spectating
                    && !(Net.NetSession.Instance != null && Net.NetSession.Instance.IsNetworked));
            if (host.SeatSwapOffer != null && !host.SeatSwapOffer.Incoming)
                _status.text = "Waiting for the other player to accept the seat switch.";
            else if (!string.IsNullOrEmpty(host.SeatSwapResult)) _status.text = host.SeatSwapResult;
            _status.fontSize = HubStyle.Size(HubStyle.Floor);
            Deepen();
            Hub.RefreshFocus();
        }

        private void DrawSeats(IHubHost host, HubSeat[] seats)
        {
            for (int i = _rows.childCount - 1; i >= 0; i--) Destroy(_rows.GetChild(i).gameObject);
            var people = Roster.GetPeople(SceneFlow.SelectedMode);
            for (int i = 0; i < seats.Length; i++)
            {
                var seat = seats[i];
                var row = HubKit.Place(HubKit.Rect(_rows, "Seat" + i), HubKit.TopLeft, new Vector2(0, -(i * RowPitch)), new Vector2(840, RowHeight));
                bool open = !seat.Occupied && !seat.Bot;
                Color fill = seat.Mine ? HubStyle.Persimmon : open ? new Color(0.2f, 0.12f, 0.06f) : HubStyle.Night;
                int destination = seat.Slot;
                bool editBots = host.CanEditBots && !seat.Occupied;

                // ⚠️⚠️ THE WHOLE ROW IS THE DOOR, owner 2026-10-17: *"in order to change your P
                // position, you could instead hover on other p positions and click anywhere on
                // their respective rectangle"*. A row the player can move to is a pressable sticker,
                // lifts under the pointer and says what a press does; a row they cannot move to
                // stays a rounded plate and does not pretend to be a button.
                // ⚠️ IT KEEPS THE NAME `TakeSeat{n}` AND THE SAME AUTHORITATIVE REQUEST, re-checked
                // on press because the host may have filled or started the room since this draw.
                Transform surface = row;
                float controls = editBots ? (seat.Bot ? 96 : 236) : 0;
                if (!seat.Mine && host.CanTakeSeat(destination))
                {
                    var take = HubKit.Button(row, "TakeSeat" + destination, null, fill, () =>
                    {
                        if (Hub.Host.CanTakeSeat(destination)) Hub.Host.TakeSeat(destination);
                    }, HubStyle.Label, 980 + destination);
                    HubKit.Stretch((RectTransform)take.transform);
                    take.Shape.OutlineWidth = 4;
                    take.Shape.Corner = 18;
                    take.Shape.Depth = RowDepth;
                    surface = take.Body;

                    // The hint names the press, so the row reads before it is clicked. Shown on
                    // hover and on pad focus; a thumb has no hover and simply taps.
                    var hint = HubKit.Text(take.Body, "Label", seat.Occupied ? "SWITCH" : "MOVE HERE",
                                           HubStyle.Floor, true, HubStyle.Golden, TextAnchor.MiddleRight);
                    HubKit.Place(hint.rectTransform, HubKit.Right, new Vector2(-(28 + controls), 0), new Vector2(200, 44));
                    HubKit.Fit(hint, 200);
                    HubDepthText.On(hint, 5.0f);
                    hint.enabled = false;
                    take.Attention += attended => { if (hint != null) hint.enabled = attended; };
                }
                else
                {
                    var plate = HubKit.Shape(row, "Plate", fill, false, 970 + i, 4, 18);
                    HubKit.Stretch(plate.rectTransform);
                    plate.Depth = RowDepth;
                }

                var person = seat.Occupied ? Roster.At(people, seat.CharacterPick) : null;
                var face = HubKit.Picture(surface, "Face", HubKit.Portrait(person?.Id));
                HubKit.Place(face.rectTransform, HubKit.Left, new Vector2(8, 2), new Vector2(86, 86));
                if (face.sprite == null)
                {
                    // An OPEN seat is an empty chair, a faint person outline, waiting; a bot wears the bot.
                    var mark = seat.Bot && !seat.Occupied ? HubGlyph.Mark.Bots : HubGlyph.Mark.Person;
                    Color tint = HubStyle.HoneySoft;
                    if (open) tint.a *= 0.4f;
                    var icon = HubKit.Glyph(surface, "Icon", mark, tint);
                    HubKit.Place(icon.rectTransform, HubKit.Left, new Vector2(22, 0), new Vector2(56, 56));
                }

                Color ink = seat.Mine ? HubStyle.Ink : HubStyle.Honey;
                if (open) ink = HubStyle.HoneySoft;
                string name = seat.Occupied ? (seat.Mine ? "YOU" : seat.Name) : seat.Bot ? "BOT" : "OPEN SEAT";
                var label = HubKit.Text(surface, "Name", "P" + (seat.Slot + 1) + "  " + name, HubStyle.Label, true, ink, TextAnchor.MiddleLeft);
                float nameWidth = 840 - 112 - 160 - controls;
                HubKit.Place(label.rectTransform, HubKit.Left, new Vector2(112, 14), new Vector2(nameWidth, 48));
                HubKit.Fit(label, nameWidth);
                if (seat.Host)
                {
                    // The crown sits just after the name it belongs to (playtest 2026-10-23).
                    var crown = HubKit.Glyph(surface, "HostMark", HubGlyph.Mark.Crown, HubStyle.Golden);
                    float after = 112 + Mathf.Min(label.preferredWidth, nameWidth) + 12;
                    HubKit.Place(crown.rectTransform, HubKit.Left, new Vector2(after, 14), new Vector2(34, 34));
                }

                // One state per row, in the reading face, under the name: who runs the room, who is
                // set, who is still getting ready, which chair is waiting.
                string state; Color stateInk;
                if (seat.Host) { state = "Host"; stateInk = seat.Mine ? HubStyle.Ink : HubStyle.Golden; }
                else if (seat.Occupied && seat.Ready) { state = "Ready"; stateInk = seat.Mine ? HubStyle.Ink : HubStyle.Chartreuse; }
                else if (seat.Occupied) { state = "Not ready yet"; stateInk = seat.Mine ? HubStyle.Ink : HubStyle.HoneySoft; }
                else if (seat.Bot) { state = "Plays this seat"; stateInk = HubStyle.HoneySoft; }
                else { state = "Waiting for a player"; stateInk = new Color(HubStyle.Honey.r, HubStyle.Honey.g, HubStyle.Honey.b, 0.55f); }
                var tag = HubKit.Text(surface, "State", state, HubStyle.Floor, false, stateInk, TextAnchor.MiddleLeft);
                HubKit.Place(tag.rectTransform, HubKit.Left, new Vector2(114, -24), new Vector2(nameWidth, 38));
                if (seat.Ready && !seat.Host && !seat.Mine)
                {
                    var tick = HubKit.Glyph(surface, "ReadyMark", HubGlyph.Mark.Check, HubStyle.Chartreuse);
                    HubKit.Place(tick.rectTransform, HubKit.Left, new Vector2(114 + tag.preferredWidth + 10, -24), new Vector2(28, 28));
                }

                // ⚠️ ADD BOT IN THE EMPTY CHAIR, A SMALL X ON A BOT (owner 2026-10-17 put the bot
                // controls per seat; the 2026-10-28 pass made them quiet, so three REMOVE BOT stickers
                // no longer out-shout START). Only the host of a custom room decides. ⚠️ A SIBLING
                // OF THE ROW'S BUTTON, NOT A CHILD OF ITS BODY: a child would light the row and show
                // "MOVE HERE" while the pointer is on the bot control, naming the wrong press.
                if (editBots)
                {
                    HubButton bot;
                    if (seat.Bot)
                    {
                        bot = HubKit.IconButton(row, "RemoveBot" + destination, HubGlyph.Mark.Close, HubStyle.Honey,
                                                () => Hub.Host.SetSeatBot(destination, false), 990 + destination);
                        HubKit.Place((RectTransform)bot.transform, HubKit.Right, new Vector2(-12, 0), new Vector2(72, 72));
                    }
                    else
                    {
                        bot = HubKit.Button(row, "AddBot" + destination, "ADD BOT", HubStyle.Honey,
                                            () => Hub.Host.SetSeatBot(destination, true), HubStyle.Floor, 990 + destination, HubGlyph.Mark.Plus);
                        HubKit.Place((RectTransform)bot.transform, HubKit.Right, new Vector2(-12, 0), new Vector2(212, 72));
                        HubKit.Fit(HubKit.LabelOf(bot), 120);
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
            var panel = HubCards.Panel(Root, this, "CLOSE ROOM?", "", new Vector2(820, 420));
            string who = Others <= 0 ? "Nobody else is in it yet."
                : Others == 1 ? "The other player in it goes back to the menu."
                : "The " + Others + " other players in it go back to the menu.";
            var message = HubKit.Text(panel, "Consequence", "Leaving closes this room. " + who,
                                      HubStyle.Body, false, HubStyle.Honey, TextAnchor.MiddleLeft);
            HubKit.Place(message.rectTransform, HubKit.TopLeft, new Vector2(52, -140), new Vector2(716, 110));
            _keep = HubKit.Button(panel, "KeepRoom", "KEEP ROOM", HubStyle.Honey, Close, HubStyle.Label, 995);
            HubKit.Place((RectTransform)_keep.transform, HubKit.BottomLeft, new Vector2(44, 40), new Vector2(340, 96));
            var close = HubKit.Button(panel, "CloseRoom", "CLOSE ROOM", HubStyle.DeepRed, CloseRoom, HubStyle.Label, 996, HubGlyph.Mark.Exit);
            HubKit.Place((RectTransform)close.transform, HubKit.BottomRight, new Vector2(-44, 40), new Vector2(340, 96));
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
