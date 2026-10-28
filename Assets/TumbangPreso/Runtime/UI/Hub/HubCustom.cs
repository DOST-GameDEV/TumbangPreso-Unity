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
        public override float CourtShade => 0.9f;

        private InputField _name;
        private HubButton _lan, _online, _create;
        private HubButton[] _visibility;
        private RectTransform _lanTick, _onlineTick;
        private int _visibilityValue;
        private bool _onlineChosen;
        private Text _status;
        private bool _busy;
        private const float Column = 1100;

        private static readonly string[] Visibility = { "PUBLIC", "FRIENDS ONLY", "PRIVATE" };

        public override Selectable FirstFocus => _create;

        public override void Build()
        {
            HubChrome.Back(Root, Hub);
            HubKnit.TitlePlate(Root, "CUSTOM", "HOST GAME", 520);

            string me = GameServices.Account?.DisplayName;
            if (string.IsNullOrWhiteSpace(me)) me = Settings.SettingsStore.Current.PlayerName;
            if (string.IsNullOrWhiteSpace(me)) me = "PLAYER";

            float left = -Column * .5f;
            Caption("NameCaption", "ROOM NAME", 392);
            _name = HubKnit.Field(Root, "LobbyName", (me + "'s room").ToUpperInvariant(), "Name your room",
                                  Settings.GameSettings.RoomTitleMax, HubStyle.Title);
            HubKit.Place((RectTransform)_name.transform, HubKit.Centre, new Vector2(0, 300), new Vector2(Column, 96));
            var pencil = HubKit.Glyph(_name.transform, "Pencil", HubGlyph.Mark.Pencil, HubKnit.GoldInk, 0.1f);
            HubKit.Place(pencil.rectTransform, HubKit.Right, new Vector2(-30, 0), new Vector2(38, 38));

            Caption("NetworkCaption", "WHERE ARE YOUR FRIENDS?", 214);
            _lan = Door("LanChoice", HubGlyph.Mark.House, "LAN", "Same Wi-Fi, no internet", left + 260, () => Network(false), out _lanTick);
            _online = Door("OnlineChoice", HubGlyph.Mark.Globe, "ONLINE", "Anywhere, by code", -left - 260, () => Network(true), out _onlineTick);

            Caption("VisibilityCaption", "WHO CAN JOIN", -178);
            var plate = HubKnit.Plate(Root, "Visibility", Knit.Maroon);
            HubKit.Place(plate, HubKit.Centre, new Vector2(0, -264), new Vector2(Column, 96));
            _visibility = new HubButton[Visibility.Length];
            float segment = (Column - 40) / Visibility.Length;
            for (int i = 0; i < Visibility.Length; i++)
            {
                int index = i;
                _visibility[i] = HubKnit.Button(HubKnit.FaceOf(plate), "Visibility" + i, Visibility[i], Knit.Gold, () => Visible(index), HubStyle.Label);
                HubKit.Place((RectTransform)_visibility[i].transform, HubKit.Left, new Vector2(20 + i * segment, 4), new Vector2(segment - 12, 70));
            }

            _status = HubKit.Text(Root, "Status", "", HubStyle.Floor, false, HubBoard.Colour("#FCD39F"), TextAnchor.MiddleCenter);
            HubKit.Place(_status.rectTransform, HubKit.Centre, new Vector2(0, -338), new Vector2(Column, 44));
            HubDepthText.On(_status, 4);

            _create = HubKnit.Button(Root, "CreateLobby", "CREATE ROOM", Knit.Gold, Create, 56, HubGlyph.Mark.None, primary: true);
            HubKit.Place((RectTransform)_create.transform, HubKit.Bottom, new Vector2(0, HubKit.Margin - 26), new Vector2(520, 124));

            Network(false);
            Visible(0);
        }

        private void Caption(string name, string words, float y)
        {
            var text = HubLobby.Caption(Root, name, words, HubKnit.Honey);
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(.5f, .5f);
            text.rectTransform.pivot = new Vector2(0, .5f);
            text.rectTransform.anchoredPosition = new Vector2(-Column * .5f + 6, y);
            text.rectTransform.sizeDelta = new Vector2(Column, 40);
            HubDepthText.On(text, 3);
        }

        /// <summary>A big knit door: its picture, its name, and what it means in one line.</summary>
        private HubButton Door(string name, HubGlyph.Mark mark, string title, string line, float x, System.Action pick, out RectTransform tick)
        {
            var door = HubKnit.Button(Root, name, null, Knit.Orange, pick);
            HubKit.Place((RectTransform)door.transform, HubKit.Centre, new Vector2(x, 28), new Vector2(520, 290));
            var glyph = HubKit.Glyph(door.Body, "Icon", mark, HubKnit.OrangeInk, 0.09f);
            HubKit.Place(glyph.rectTransform, HubKit.Top, new Vector2(0, -26), new Vector2(110, 110));
            var big = HubKit.Text(door.Body, "Label", title, HubStyle.Display, true, HubKnit.OrangeInk, TextAnchor.MiddleCenter);
            HubKit.Place(big.rectTransform, HubKit.Bottom, new Vector2(0, 68), new Vector2(480, 80));
            var small = HubKit.Text(door.Body, "Line", line, HubStyle.Body, false, HubKnit.OrangeInk, TextAnchor.MiddleCenter);
            HubKit.Place(small.rectTransform, HubKit.Bottom, new Vector2(0, 26), new Vector2(480, 40));
            door.GetComponent<HubStickerFeel>().Pictures = new[] { glyph.rectTransform };
            tick = HubKnit.Stamp(door.transform, "Chosen", "", Knit.Teal, 6, HubGlyph.Mark.Check);
            tick.sizeDelta = new Vector2(70, 62);
            tick.anchorMin = tick.anchorMax = tick.pivot = new Vector2(1, 1);
            tick.anchoredPosition = new Vector2(16, 18);
            return door;
        }

        private void Network(bool online)
        {
            _onlineChosen = online;
            // The chosen door is the gold tile; the other steps back onto a dark plate, so the choice
            // reads at a glance instead of the unchosen orange shouting louder than the chosen gold.
            HubKnit.Recolour(_lan.transform, online ? Knit.Maroon : Knit.Gold);
            HubKnit.Recolour(_online.transform, online ? Knit.Gold : Knit.Maroon);
            foreach (var door in new[] { _lan, _online })
            {
                var ink = door == _online == online ? HubKnit.GoldInk : new Color(HubKnit.Cream.r, HubKnit.Cream.g, HubKnit.Cream.b, 0.8f);
                foreach (var t in door.Body.GetComponentsInChildren<Text>(true)) t.color = ink;
                var icon = door.Body.Find("Icon")?.GetComponent<HubGlyph>();
                if (icon != null) icon.color = ink;
            }
            _lanTick.gameObject.SetActive(!online);
            _onlineTick.gameObject.SetActive(online);
            // The doors already say what each means; the line is kept for opening and refusals.
            _status.text = "";
        }

        private void Visible(int index)
        {
            _visibilityValue = index;
            for (int i = 0; i < _visibility.Length; i++)
                HubKnit.Bare(_visibility[i].transform, i != index, i == index ? HubKnit.GoldInk : new Color(HubKnit.Cream.r, HubKnit.Cream.g, HubKnit.Cream.b, 0.75f));
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
        public override float CourtShade => 0.9f;
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
        private const float Column = 1240, RowHeight = 108, RowPitch = 126;

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
        private const int VisibleRooms = 4;
        private readonly List<HubRoom> _drawnRooms = new List<HubRoom>(VisibleRooms);
        private readonly List<HubRoom> _rooms = new List<HubRoom>();
        private readonly List<bool> _roomLan = new List<bool>();
        private string _drawnEmptyMessage;
        private bool _drawnOnce;

        public override Selectable FirstFocus => _codeField;

        public override void Build()
        {
            HubChrome.Back(Root, Hub);
            HubKnit.TitlePlate(Root, "CUSTOM", "JOIN GAME", 520);
            float left = -Column * .5f;

            var codeCaption = HubLobby.Caption(Root, "CodeCaption", "GOT A CODE?", HubKnit.Honey);
            Centre(codeCaption.rectTransform, new Vector2(left + 6, 352), new Vector2(600, 40), 0);
            HubDepthText.On(codeCaption, 3);
            _codeField = HubKnit.Field(Root, "CodeField", "", "ABCD or 192.168.1.20", 21, HubStyle.Display);
            Centre((RectTransform)_codeField.transform, new Vector2(left, 262), new Vector2(Column - 330, 108), 0);
            _codeField.textComponent.font = HubBoard.FontFor("digits");
            _codeField.textComponent.fontSize = 56;
            _codeField.onValidateInput += (text, index, c) => char.ToUpperInvariant(c);
            _codeField.onSubmit.AddListener(code => { if (!string.IsNullOrWhiteSpace(code)) Join(code); });
            _joinByCode = HubKnit.Button(Root, "JoinByCode", "JOIN", Knit.Gold, () => Join(_codeField.text), 56, HubGlyph.Mark.None, primary: true);
            Centre((RectTransform)_joinByCode.transform, new Vector2(-left - 300, 262), new Vector2(300, 112), 0);

            // The list stands on HOME's wine plate, so rooms and the empty line read over the street.
            var board = HubKnit.Plate(Root, "ListBoard", Knit.Wine);
            board.anchorMin = board.anchorMax = new Vector2(.5f, .5f);
            board.pivot = new Vector2(.5f, 1);
            board.anchoredPosition = new Vector2(0, 198);
            board.sizeDelta = new Vector2(Column + 64, 198 + 430);
            var heading = HubKit.Text(Root, "Heading", "OPEN ROOMS", HubStyle.Title, true, HubBoard.Colour("#FCD39F"), TextAnchor.MiddleLeft);
            Centre(heading.rectTransform, new Vector2(left + 4, 156), new Vector2(600, 60), 0);
            HubDepthText.On(heading, 6);
            _refresh = HubKnit.Button(Root, "RefreshServers", "REFRESH", Knit.Orange, RefreshServers, HubStyle.Label);
            Centre((RectTransform)_refresh.transform, new Vector2(-left - 260, 156), new Vector2(260, 76), 0);

            _list = HubKit.Rect(Root, "List");
            _list.anchorMin = _list.anchorMax = new Vector2(.5f, .5f);
            _list.pivot = new Vector2(.5f, 1);
            _list.anchoredPosition = new Vector2(0, 100);
            _list.sizeDelta = new Vector2(Column, RowPitch * VisibleRooms);
            // ⚠️ ONE STATE LINE, IN THE READING FACE (2026-09-23 UI review): never "Looking for ..." and
            // "Nobody yet" at once.
            _empty = HubKit.Text(_list, "EmptyState", "", HubStyle.Label, false, HubBoard.Colour("#FCD39F"), TextAnchor.MiddleCenter);
            HubKit.Stretch(_empty.rectTransform, 40);
            HubDepthText.On(_empty, 4);
            // ⚠️ AN EMPTY LIST IS A LATA STANDING ALONE IN ITS CIRCLE (2026-09-24): the game's own
            // picture of "nobody is playing yet", above the sentence, gone once rooms appear.
            _emptyCan = HubKit.Rect(_empty.transform, "EmptyCan");
            HubKit.Place(_emptyCan, HubKit.Centre, new Vector2(0, 110), new Vector2(130, 130));
            var ring = HubScenery.ChalkRing(_emptyCan, "Circle", 0.2f);
            HubKit.Place(ring.rectTransform, HubKit.Bottom, new Vector2(0, -6), new Vector2(210, 60));
            ring.color = new Color(HubStyle.Honey.r, HubStyle.Honey.g, HubStyle.Honey.b, 0.35f);
            var can = HubKit.Glyph(_emptyCan, "Can", HubGlyph.Mark.Can, new Color(HubStyle.Honey.r, HubStyle.Honey.g, HubStyle.Honey.b, 0.45f), 0.07f);
            HubKit.Stretch(can.rectTransform, 12);

            _status = HubKit.Text(Root, "Status", "", HubStyle.Body, false, HubBoard.Colour("#FCD39F"), TextAnchor.MiddleCenter);
            HubKit.Place(_status.rectTransform, HubKit.Bottom, new Vector2(0, HubKit.Margin - 30), new Vector2(Column, 50));
            HubDepthText.On(_status, 4);

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

        private static void Centre(RectTransform rect, Vector2 leftTop, Vector2 size, float unused)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(0, .5f);
            rect.anchoredPosition = leftTop;
            rect.sizeDelta = size;
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
                var row = HubKnit.Plate(_list, "Room" + i, Knit.Orange);
                HubKit.Place(row, HubKit.TopLeft, new Vector2(0, -(i * RowPitch)), new Vector2(Column - 24, RowHeight));
                var face = HubKnit.FaceOf(row);
                var name = HubKit.Text(face, "Name", room.Name, HubStyle.Title, true, HubKnit.OrangeInk, TextAnchor.LowerLeft);
                HubKit.Place(name.rectTransform, HubKit.TopLeft, new Vector2(40, -10), new Vector2(560, 56));
                name.horizontalOverflow = HorizontalWrapMode.Overflow;
                HubKit.Fit(name, 560);
                var map = HubKit.Text(face, "Map", room.Map, HubStyle.Floor, false, HubKnit.WineInk, TextAnchor.UpperLeft);
                HubKit.Place(map.rectTransform, HubKit.BottomLeft, new Vector2(42, 8), new Vector2(560, 38));
                HubKit.Fit(map, 560);

                var tag = HubKnit.Stamp(face, "Network", _roomLan[i] ? "LAN" : "ONLINE", _roomLan[i] ? Knit.Teal : Knit.Red, 0);
                tag.anchorMin = tag.anchorMax = tag.pivot = new Vector2(1, .5f);
                tag.anchoredPosition = new Vector2(-470, 2);
                var who = HubKit.Glyph(face, "PlayersIcon", HubGlyph.Mark.Person, HubKnit.WineInk, 0.1f);
                HubKit.Place(who.rectTransform, HubKit.Right, new Vector2(-388, 2), new Vector2(34, 34));
                var players = HubKit.Text(face, "Players", room.InProgress ? "IN A MATCH" : room.Players + "/" + room.Capacity, HubStyle.Label, true, HubKnit.WineInk, TextAnchor.MiddleLeft);
                HubKit.Place(players.rectTransform, HubKit.Right, new Vector2(-232, 2), new Vector2(150, 50));
                players.font = room.InProgress ? HubStyle.DisplayFont : HubBoard.FontFor("digits");
                HubKit.Fit(players, 150);
                // ⚠️ A HOST ON ANOTHER PROTOCOL GETS NO JOIN AT ALL, NOT A GREYED ONE (LAN-VPN-JOIN-1020).
                // Pressing it could only be refused at approval after the wait; the tag says why in the
                // place the press would have been, and the focus path skips it.
                if (room.DifferentVersion)
                {
                    var version = HubKit.Text(face, "Version", DifferentVersionLabel, HubStyle.Floor, false, HubKnit.WineInk, TextAnchor.MiddleCenter);
                    HubKit.Place(version.rectTransform, HubKit.Right, new Vector2(-24, 0), new Vector2(190, 84));
                    HubKit.Fit(version, 190);
                    // A room nobody here can join steps back: its knit is faded, its words stay readable.
                    foreach (var image in new[] { row.Find("Slab")?.GetComponent<Image>(), face.GetComponent<Image>() })
                        if (image != null) image.color = new Color(1, 1, 1, 0.55f);
                    continue;
                }
                string key = room.Key;
                var join = HubKnit.Button(face, "Join" + i, "JOIN", Knit.Gold, () => Join(key), HubStyle.Label);
                HubKit.Place((RectTransform)join.transform, HubKit.Right, new Vector2(-34, 4), new Vector2(180, 76));
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
        public override float CourtShade => 0.55f;

        // The room's measures, canvas units, centred on the canvas (y up). A taller canvas (4:3)
        // adds air above and below the middle band instead of pulling the cards from the strip.
        public const float RoomContentWidth = 1808, CardGap = 36;
        internal const float CardsTop = 210, CardHeight = 400, PortraitSize = 360, PortraitRise = 104;
        internal const float StripTop = -226, StripHeight = 100;
        internal const float ChatWidth = 640, ChatGap = 40;
        /// <summary>The cards' column while the chat drawer is open, and how far it moves left.</summary>
        internal const float RoomSeatsNarrow = RoomContentWidth - ChatWidth - ChatGap;
        internal const float RoomSeatsOffset = -(RoomContentWidth - RoomSeatsNarrow) * .5f;
        /// <summary>The chat drawer's left edge, measured from the canvas centre.</summary>
        internal const float RoomChatLeft = RoomContentWidth * .5f - ChatWidth;
        internal const float RoomChatWidth = ChatWidth;

        private Text _code, _codeCaption, _address, _count, _status, _digest, _unreadCount;
        private RectTransform _seats, _playersPanel, _matchCard, _summaryStrip, _networkTag, _statusPlate, _watching;
        private HubButton _primary, _watch, _rules, _viewRules, _chatDoor, _codeChip, _modeClassic, _modeHero;
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
        internal const float FirewallWidth = 1060;
        private RectTransform _firewall;
        private Text _firewallLine;
        private HubButton _firewallAllow;
        private string _firewallDrawn = "";
        private int _firewallAllowedSeen;

        public override Selectable FirstFocus => _primary != null && _primary.gameObject.activeInHierarchy ? _primary : (Selectable)_watch;
        private string _drawn = "";

        public override void Build()
        {
            // The fiesta's bunting strung high, over the title and the ticket, clear of the players' heading.
            HubScenery.Bunting(Root, 7, 4, 0.1f);
            HubChrome.Back(Root, Hub);
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
        private RectTransform _titlePlate;

        /// <summary>The room's name on a maroon plate, with whose room it is above it.</summary>
        private void BuildTitle()
        {
            float x = HubKit.Margin + HubChrome.BarHeight + 24;
            _titlePlate = HubKnit.Plate(Root, "TitlePlate", Knit.Maroon);
            HubKit.Place(_titlePlate, HubKit.TopLeft, new Vector2(x, -HubKit.Margin + 18), new Vector2(640, 118));
            var face = HubKnit.FaceOf(_titlePlate);
            _titleCaption = Caption(face, "RoomOwner", "YOUR ROOM", HubKnit.Honey);
            HubKit.Place(_titleCaption.rectTransform, HubKit.TopLeft, new Vector2(34, -12), new Vector2(560, 34));
            _title = HubKit.Text(face, "ScreenTitle", "LOBBY", HubStyle.Title, true, HubKnit.Cream, TextAnchor.MiddleLeft);
            HubKit.Stretch(_title.rectTransform);
            _title.rectTransform.offsetMin = new Vector2(34, 14); _title.rectTransform.offsetMax = new Vector2(-26, -42);
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
        /// The code is the one fact a host reads out loud, so it is a gold ticket, top right: the code
        /// in HOME's digits, the network as a tag (a fact, not a setting), the LAN address under it.
        /// Pressing it copies the code.
        /// </summary>
        private void BuildTicket()
        {
            _codeChip = HubKnit.Button(Root, "CodeChip", null, Knit.Gold, CopyCode);
            HubKit.Place((RectTransform)_codeChip.transform, HubKit.TopRight, new Vector2(-HubKit.Margin - 24, -HubKit.Margin + 20), new Vector2(480, 128));
            var face = _codeChip.Body;
            _codeCaption = Caption(face, "CodeCaption", "ROOM CODE", HubKnit.GoldInk);
            HubKit.Place(_codeCaption.rectTransform, HubKit.TopLeft, new Vector2(34, -14), new Vector2(260, 32));
            _code = HubKit.Text(face, "Code", "", HubStyle.Display, true, HubKnit.WineInk, TextAnchor.MiddleLeft);
            _code.font = HubBoard.FontFor("digits");
            _code.fontSize = 58;
            HubKit.Place(_code.rectTransform, HubKit.BottomLeft, new Vector2(30, 12), new Vector2(250, 72));
            _networkTag = HubKnit.Stamp(face, "NetworkTag", "LAN", Knit.Teal, 0);
            HubKit.Place(_networkTag, HubKit.TopRight, new Vector2(-28, -16), _networkTag.sizeDelta);
            _networkWord = _networkTag.Find("Face/Label").GetComponent<Text>();
            _address = HubKit.Text(face, "Address", "", HubStyle.Floor, false, HubKnit.GoldInk, TextAnchor.MiddleRight);
            HubKit.Place(_address.rectTransform, HubKit.BottomRight, new Vector2(-28, 10), new Vector2(300, 50));
        }

        // ------------------------------------------------------------------ the cards

        private void BuildPlayers()
        {
            _count = HubKit.Text(Root, "PlayersCount", "", HubStyle.Title, true, HubBoard.Colour("#FCD39F"), TextAnchor.MiddleLeft);
            HubKit.Place(_count.rectTransform, HubKit.Centre, new Vector2(-RoomContentWidth * .5f + 200, CardsTop + 110), new Vector2(400, 64));
            HubDepthText.On(_count, 6);

            // ⚠️ SPECTATE STAYS AT THE TOP RIGHT OF THE PLAYERS, LABELLED SPECTATE (owner 2026-10-28).
            // It is a seat choice, so it sits on the line of seats; on, it turns gold with a tick.
            _watch = HubKnit.Button(Root, "WatchToggle", "SPECTATE", Knit.Orange, () => { Hub.Host.ToggleSpectate(); _drawn = ""; },
                                    HubStyle.Label, HubGlyph.Mark.Eye);
            HubKit.Place((RectTransform)_watch.transform, HubKit.Centre, new Vector2(RoomContentWidth * .5f - 140, CardsTop + 110), new Vector2(280, 76));

            _playersPanel = HubKit.Rect(Root, "Players");
            _playersPanel.anchorMin = _playersPanel.anchorMax = new Vector2(.5f, .5f);
            _playersPanel.pivot = new Vector2(.5f, 1);
            _playersPanel.anchoredPosition = new Vector2(0, CardsTop);
            _playersPanel.sizeDelta = new Vector2(RoomContentWidth, CardHeight);
            _seats = HubKit.Stretch(HubKit.Rect(_playersPanel, "Seats"));
        }

        /// <summary>The host's strip: MAP, MODE and RULES, under the cards.</summary>
        private void BuildMatchCard()
        {
            var card = _matchCard = HubKit.Rect(Root, "MatchCard");
            card.anchorMin = card.anchorMax = new Vector2(.5f, .5f);
            card.pivot = new Vector2(.5f, 1);
            card.anchoredPosition = new Vector2(0, StripTop);
            card.sizeDelta = new Vector2(RoomContentWidth, StripHeight);

            // ⚠️ THE SAME PREVIOUS/NEXT SELECTOR AS HOST GAME (`HubArrowChoice`), on its own knit
            // plate, on the same `SelectMap` call, so the room changes map in one press.
            var mapPlate = HubKnit.Plate(card, "MapPlate", Knit.Maroon);
            HubKit.Place(mapPlate, HubKit.TopLeft, Vector2.zero, new Vector2(780, StripHeight));
            var mapFace = HubKnit.FaceOf(mapPlate);
            var mapWord = Caption(mapFace, "MapCaption", "MAP", HubKnit.Honey);
            HubKit.Place(mapWord.rectTransform, HubKit.Left, new Vector2(34, 0), new Vector2(90, 40));
            string[] maps = new string[SceneFlow.MapRegistry.Length];
            for (int i = 0; i < maps.Length; i++) maps[i] = SceneFlow.MapRegistry[i].Name.ToUpperInvariant();
            _mapChoice = HubArrowChoice.BuildKnit(mapFace, "Map", maps, MapIndex());
            HubKit.Span((RectTransform)_mapChoice.transform, Vector2.zero, Vector2.one, new Vector2(120, 6), new Vector2(0, 6));
            _mapChoice.Changed += i => { Hub.Host.SelectMap(SceneFlow.MapRegistry[i].Id); _drawn = ""; };

            // MODE is chosen here and only here: two knit segments, the chosen one gold.
            var modePlate = HubKnit.Plate(card, "ModePlate", Knit.Maroon);
            HubKit.Place(modePlate, HubKit.TopLeft, new Vector2(816, 0), new Vector2(680, StripHeight));
            var modeFace = HubKnit.FaceOf(modePlate);
            var modeWord = Caption(modeFace, "ModeCaption", "MODE", HubKnit.Honey);
            HubKit.Place(modeWord.rectTransform, HubKit.Left, new Vector2(34, 0), new Vector2(100, 40));
            _modeClassic = HubKnit.Button(modeFace, "ModeClassic", "CLASSIC", Knit.Gold, () => PickMode(GameMode.Classic), HubStyle.Label);
            HubKit.Place((RectTransform)_modeClassic.transform, HubKit.Left, new Vector2(140, 4), new Vector2(230, 70));
            _modeHero = HubKnit.Button(modeFace, "ModeHeroStrike", "HERO STRIKE", Knit.Gold, () => PickMode(GameMode.HeroStrike), HubStyle.Label);
            HubKit.Place((RectTransform)_modeHero.transform, HubKit.Left, new Vector2(384, 4), new Vector2(270, 70));

            _rules = HubKnit.Button(card, "SettingsDoor", null, Knit.Orange, OpenRules, HubStyle.Label, HubGlyph.Mark.Book);
            HubKit.Place((RectTransform)_rules.transform, HubKit.TopRight, new Vector2(-24, 0), new Vector2(276, StripHeight - 4));
            var rulesIcon = (RectTransform)_rules.Body.Find("Icon");
            rulesIcon.anchorMin = rulesIcon.anchorMax = rulesIcon.pivot = new Vector2(0, .5f);
            rulesIcon.anchoredPosition = new Vector2(24, 0);
            var rulesWord = HubKit.Text(_rules.Body, "Label", "RULES", HubStyle.Label, true, HubKnit.OrangeInk, TextAnchor.LowerLeft);
            HubKit.Place(rulesWord.rectTransform, HubKit.TopLeft, new Vector2(80, -12), new Vector2(180, 42));
            _digest = HubKit.Text(_rules.Body, "Digest", "", HubStyle.Floor, false, HubKnit.WineInk, TextAnchor.UpperLeft);
            HubKit.Place(_digest.rectTransform, HubKit.BottomLeft, new Vector2(80, 8), new Vector2(186, 40));

            _firewall = HubKit.Rect(card, "LanFirewall");
            _firewall.anchorMin = _firewall.anchorMax = new Vector2(0, 0);
            _firewall.pivot = new Vector2(0, 1);
            _firewall.anchoredPosition = new Vector2(0, -14);
            _firewall.sizeDelta = new Vector2(FirewallWidth, 72);
            _firewallLine = HubKit.Text(_firewall, "Line", "", HubStyle.Floor, false, HubBoard.Colour("#FCD39F"), TextAnchor.MiddleLeft);
            HubKit.Place(_firewallLine.rectTransform, HubKit.Left, new Vector2(8, 0), new Vector2(FirewallWidth - 340, 72));
            HubDepthText.On(_firewallLine, 4);
            _firewallAllow = HubKnit.Button(_firewall, "AllowLanPlay", LanFirewallRules.AllowLabel, Knit.Teal, AllowLanPlay,
                                            HubStyle.Floor, HubGlyph.Mark.House);
            HubKit.Place((RectTransform)_firewallAllow.transform, HubKit.Right, Vector2.zero, new Vector2(320, 68));
            HubKnit.FitLabel(_firewallAllow);
            _firewall.gameObject.SetActive(false);
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
            _summaryStrip.anchoredPosition = new Vector2(-(280 + 20), 110);
            _summaryStrip.sizeDelta = new Vector2(196, 76);
            _viewRules = HubKnit.Button(_summaryStrip, "ViewRules", "RULES", Knit.Orange, OpenSettings, HubStyle.Label, HubGlyph.Mark.Book);
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
            _statusPlate = HubKnit.Plate(Root, "StatusPlate", Knit.Wine);
            HubKit.Place(_statusPlate, HubKit.BottomLeft, new Vector2(HubKit.Margin, HubKit.Margin - 16), new Vector2(600, 100));
            _status = HubKit.Text(HubKnit.FaceOf(_statusPlate), "Status", "", HubStyle.Floor, false, HubBoard.Colour("#F1E3C8"), TextAnchor.MiddleLeft);
            HubKit.Stretch(_status.rectTransform);
            _status.rectTransform.offsetMin = new Vector2(30, 6); _status.rectTransform.offsetMax = new Vector2(-24, -6);
            _status.horizontalOverflow = HorizontalWrapMode.Wrap;

            _primary = HubKnit.Button(Root, "StartGame", "START GAME", Knit.Gold, Primary, 56, HubGlyph.Mark.None, primary: true);
            HubKit.Place((RectTransform)_primary.transform, HubKit.Bottom, new Vector2(0, HubKit.Margin - 26), new Vector2(500, 128));

            // A spectator has nothing to press there, so the room's status takes the spot instead.
            _watching = HubKnit.Plate(Root, "WatchingPlate", Knit.Wine);
            HubKit.Place(_watching, HubKit.Bottom, new Vector2(0, HubKit.Margin - 26), new Vector2(720, 116));
            var watchingWord = HubKit.Text(HubKnit.FaceOf(_watching), "Word", "WATCHING", HubStyle.Title, true, HubKnit.Honey, TextAnchor.MiddleCenter);
            HubKit.Place(watchingWord.rectTransform, HubKit.Top, new Vector2(0, -10), new Vector2(680, 54));
            _watchingLine = HubKit.Text(HubKnit.FaceOf(_watching), "Line", "", HubStyle.Floor, false, HubBoard.Colour("#F1E3C8"), TextAnchor.MiddleCenter);
            HubKit.Place(_watchingLine.rectTransform, HubKit.Bottom, new Vector2(0, 10), new Vector2(680, 42));
            _watching.gameObject.SetActive(false);

            // CHAT carries the count of lines that arrived while it was shut; CHARACTER exists only
            // where heroes are not chosen together after START (offline rooms).
            _chatDoor = HubKnit.Button(Root, "ChatDoor", "CHAT", Knit.Teal, () => Hub.Host.ToggleChat(), HubStyle.Title, HubGlyph.Mark.Friends);
            HubKit.Place((RectTransform)_chatDoor.transform, HubKit.BottomRight, new Vector2(-HubKit.Margin - 24, HubKit.Margin - 14), new Vector2(270, 104));
            var badge = HubKnit.Plate(_chatDoor.Body, "Unread", Knit.Red);
            HubKit.Place(badge, HubKit.TopRight, new Vector2(18, 20), new Vector2(58, 54));
            _unreadCount = HubKit.Text(HubKnit.FaceOf(badge), "Count", "", HubStyle.Floor, true, HubKnit.Cream, TextAnchor.MiddleCenter);
            HubKit.Stretch(_unreadCount.rectTransform);
            _unread = badge.gameObject;
            _unread.SetActive(false);
            _seenLines = LobbyChat.RoomLineCount;
            _characterDoor = HubKnit.Button(Root, "CharacterDoor", "CHARACTER", Knit.Orange, () =>
            {
                if (_chatShown) Hub.Host.ToggleChat();
                Hub.Push<HubCharacterSelect>(c => c.Timed = false);
            }, HubStyle.Label, HubGlyph.Mark.Person);
            HubKit.Place((RectTransform)_characterDoor.transform, HubKit.BottomRight, new Vector2(-HubKit.Margin - 330, HubKit.Margin - 14), new Vector2(300, 104));
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
            {
                var w = (RectTransform)_watch.transform;
                w.anchoredPosition = new Vector2((_chatShown ? RoomSeatsOffset + RoomSeatsNarrow * .5f : RoomContentWidth * .5f) - 140, CardsTop + 110);
            }
            if (snap || HubStyle.ReducedMotion) { _panelX = PanelTarget; _panelWidth = WidthTarget; }
            ApplyPanel();
            if (_statusPlate != null)
                _statusPlate.sizeDelta = new Vector2(_chatShown ? 380 : 600, 100);
            if (_primary != null)
            {
                var p = (RectTransform)_primary.transform;
                p.anchoredPosition = new Vector2(_chatShown ? RoomSeatsOffset + 120 : 0, HubKit.Margin - 26);
            }
        }

        private void ApplyPanel()
        {
            _playersPanel.anchoredPosition = new Vector2(_panelX, CardsTop);
            _playersPanel.sizeDelta = new Vector2(_panelWidth, CardHeight);
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
            _title.fontSize = HubStyle.Size(HubStyle.Title);
            _title.horizontalOverflow = HorizontalWrapMode.Overflow;
            string owner = "";
            foreach (var s in seats) if (s.Host && !s.Mine && s.Occupied) owner = s.Name;
            _titleCaption.text = host.IsHost ? "YOUR ROOM" : string.IsNullOrEmpty(owner) ? "CUSTOM ROOM" : (owner + "'S ROOM").ToUpperInvariant();
            // The plate fits its words (a short name no longer sits on a long empty plate).
            float words = Mathf.Max(_title.preferredWidth, _titleCaption.preferredWidth);
            _titlePlate.sizeDelta = new Vector2(Mathf.Clamp(words + 80, 420, 760), _titlePlate.sizeDelta.y);
            HubKit.Fit(_title, _titlePlate.sizeDelta.x - 60);

            _code.text = host.RoomCode ?? "";
            string address = host.RoomAddress;
            _address.text = host.RoomOnline ? "tap to copy" : address ?? "";
            string network = host.RoomOnline ? "ONLINE" : "LAN";
            if (_networkWord.text != network)
            {
                _networkWord.text = network;
                HubKnit.Recolour(_networkTag, host.RoomOnline ? Knit.Red : Knit.Teal);
                _networkTag.sizeDelta = new Vector2(_networkWord.preferredWidth + 44, _networkTag.sizeDelta.y);
            }

            // SPECTATE keeps its word (owner 2026-10-28); on, it is gold with a tick.
            HubKnit.Recolour(_watch.transform, host.Spectating ? Knit.Gold : Knit.Orange);
            var watchIcon = _watch.Body.Find("Icon")?.GetComponent<HubGlyph>();
            if (watchIcon != null && watchIcon.Kind != (host.Spectating ? HubGlyph.Mark.Check : HubGlyph.Mark.Eye))
            {
                watchIcon.Kind = host.Spectating ? HubGlyph.Mark.Check : HubGlyph.Mark.Eye;
                watchIcon.SetVerticesDirty();
            }

            if (_mapChoice != null && _mapChoice.Value != MapIndex()) _mapChoice.Set(MapIndex(), false);
            bool classic = SceneFlow.SelectedMode == GameMode.Classic;
            Segment(_modeClassic, classic);
            Segment(_modeHero, !classic);
            _digest.text = rules != null ? rules.Rounds + " rounds · " + rules.RoundSeconds + " s" : "";
            HubKit.Fit(_digest, 186);

            int occupied = 0;
            foreach (var s in seats)
                if (s.Occupied) occupied++;
            _count.text = "PLAYERS  " + occupied + " / " + Balance.PlayerCount;

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

        /// <summary>A segment of a knit switch: the chosen one is a gold tile, the other bare words on the plate.</summary>
        private static void Segment(HubButton button, bool chosen)
        {
            if (button == null) return;
            HubKnit.Bare(button.transform, !chosen, chosen ? HubKnit.GoldInk : new Color(HubKnit.Cream.r, HubKnit.Cream.g, HubKnit.Cream.b, 0.75f));
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
                Knit kind = open ? Knit.Wine : seat.Mine ? Knit.Gold : Knit.Orange;
                int destination = seat.Slot;
                bool editBots = host.CanEditBots && !seat.Occupied;

                // ⚠️⚠️ THE WHOLE CARD IS THE DOOR (owner 2026-10-17: "click anywhere on their respective
                // rectangle"). A card the player can move to is a knit button that lifts and says what
                // a press does; one they cannot is a plate. It keeps the name `TakeSeat{n}` and the
                // same authoritative request, re-checked on press because the room may have changed.
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
                // Your own card stands a little proud of the row (the card, not its slot, so the
                // slot keeps the row's measure).
                if (seat.Mine) { card.offsetMin += new Vector2(0, 18); card.offsetMax += new Vector2(0, 18); }

                Color ink = HubKnit.InkOf(kind);
                // The name is the card's one fact: on your gold card it takes a deeper brown than HOME's small labels.
                Color nameInk = kind == Knit.Gold ? HubBoard.Colour("#7A4220") : ink;
                var person = seat.Occupied ? Roster.At(people, seat.CharacterPick) : null;
                // The hero pops out of the top of the card, as HOME's HERO icon does off its tile. The
                // portrait is cut flat at the chest, so it stands in a window whose lower edge fades
                // (a soft `RectMask2D`) and the cut never reads as a line across the card.
                var frame = HubKit.Rect(face, "Hero");
                frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(.5f, 1);
                frame.anchoredPosition = new Vector2(0, PortraitRise);
                frame.sizeDelta = new Vector2(PortraitSize, PortraitSize - 40);
                var fade = frame.gameObject.AddComponent<RectMask2D>();
                fade.softness = new Vector2Int(0, 46);
                var picture = HubKit.Picture(frame, "Face", HubKit.Portrait(person?.Id));
                picture.rectTransform.anchorMin = picture.rectTransform.anchorMax = picture.rectTransform.pivot = new Vector2(.5f, 1);
                picture.rectTransform.anchoredPosition = Vector2.zero;
                picture.rectTransform.sizeDelta = new Vector2(PortraitSize, PortraitSize);
                if (picture.sprite != null)
                {
                    var feel = card.GetComponent<HubStickerFeel>();
                    if (feel != null) { feel.Pictures = new[] { frame }; }
                }
                else
                {
                    // An open seat is an empty chair; a bot with no hero yet wears the bot; a person, a person.
                    var mark = open ? HubGlyph.Mark.Plus : seat.Bot && !seat.Occupied ? HubGlyph.Mark.Bots : HubGlyph.Mark.Person;
                    var glyph = HubKit.Glyph(face, "Icon", mark, new Color(ink.r, ink.g, ink.b, open ? 0.35f : 0.6f), 0.08f);
                    HubKit.Place(glyph.rectTransform, HubKit.Top, new Vector2(0, -50), new Vector2(150, 150));
                }

                var number = HubKnit.Stamp(face, "Number", "P" + (seat.Slot + 1), Knit.Maroon, 0);
                HubKit.Place(number, HubKit.TopLeft, new Vector2(18, -18), number.sizeDelta);

                string name = seat.Occupied ? (seat.Mine ? "YOU" : seat.Name) : seat.Bot ? "BOT" : "OPEN SEAT";
                var label = HubKit.Text(face, "Name", name.ToUpperInvariant(), HubStyle.Title, true, nameInk, TextAnchor.MiddleCenter);
                label.rectTransform.anchorMin = new Vector2(0, 0); label.rectTransform.anchorMax = new Vector2(1, 0);
                label.rectTransform.pivot = new Vector2(.5f, 0);
                label.rectTransform.offsetMin = new Vector2(20, open && editBots ? 140 : 92);
                label.rectTransform.offsetMax = new Vector2(-20, (open && editBots ? 140 : 92) + 56);
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.alignByGeometry = true;
                // The card's own width: with the chat open the row narrows to quarter columns of less.
                float cardWidth = (_chatShown ? RoomSeatsNarrow : RoomContentWidth) / n - CardGap - 30;
                HubKit.Fit(label, cardWidth);

                // An open seat just says OPEN SEAT (owner 2026-10-28: drop its sub-line); a press on it says MOVE HERE.
                string sub = person != null ? person.Name : seat.Bot ? "plays this seat" : "";
                // A narrow card keeps its name and drops the line under it, which has no room there.
                if (_chatShown && (open || seat.Bot)) sub = "";
                var under = HubKit.Text(face, "Hero", sub, HubStyle.Floor, false, new Color(ink.r, ink.g, ink.b, 0.85f), TextAnchor.MiddleCenter);
                under.rectTransform.anchorMin = new Vector2(0, 0); under.rectTransform.anchorMax = new Vector2(1, 0);
                under.rectTransform.pivot = new Vector2(.5f, 0);
                under.rectTransform.offsetMin = new Vector2(20, open && editBots ? 132 : 54);
                under.rectTransform.offsetMax = new Vector2(-20, (open && editBots ? 132 : 54) + 46);
                under.horizontalOverflow = HorizontalWrapMode.Overflow;
                HubKit.Fit(under, cardWidth);

                // One stamp per card, slapped on its lower right: who runs the room, who is set, who is not yet.
                if (seat.Occupied || seat.Bot)
                {
                    RectTransform stamp;
                    if (seat.Host) stamp = HubKnit.Stamp(slot, "Stamp", "HOST", Knit.Red, -6, HubGlyph.Mark.Crown);
                    else if (seat.Ready || seat.Bot) stamp = HubKnit.Stamp(slot, "Stamp", "READY", Knit.Teal, -6, HubGlyph.Mark.Check);
                    else stamp = HubKnit.Stamp(slot, "Stamp", "NOT READY", Knit.Maroon, -6, HubGlyph.Mark.Clock);
                    stamp.anchorMin = stamp.anchorMax = stamp.pivot = new Vector2(1, 0);
                    stamp.anchoredPosition = new Vector2(14, -14);
                }

                if (take != null)
                {
                    // The hint names the press, so the card reads before it is clicked. Shown on hover
                    // and on pad focus; a thumb has no hover and simply taps.
                    var hint = HubKit.Text(face, "Label", seat.Occupied || seat.Bot ? "SWITCH" : "MOVE HERE", HubStyle.Label, true, HubKnit.Honey, TextAnchor.MiddleCenter);
                    HubKit.Place(hint.rectTransform, HubKit.Centre, new Vector2(0, 30), new Vector2(320, 54));
                    HubDepthText.On(hint, 5.0f);
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
                        var remove = HubKnit.Button(slot, "RemoveBot" + destination, null, Knit.Red,
                                                    () => Hub.Host.SetSeatBot(destination, false), HubStyle.Label, HubGlyph.Mark.Close);
                        HubKit.Place((RectTransform)remove.transform, HubKit.TopRight, new Vector2(12, 14), new Vector2(66, 62));
                    }
                    else
                    {
                        var add = HubKnit.Button(slot, "AddBot" + destination, _chatShown ? "BOT" : "ADD BOT", Knit.Orange,
                                                 () => Hub.Host.SetSeatBot(destination, true), HubStyle.Label, HubGlyph.Mark.Plus);
                        var r = (RectTransform)add.transform;
                        r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(1, 0); r.pivot = new Vector2(.5f, 0);
                        r.offsetMin = new Vector2(36, 34); r.offsetMax = new Vector2(-36, 34 + 76);
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
            float rowH = 62, height = 230 + facts.Count * rowH;
            var panel = HubCards.KnitPanel(Root, this, "MATCH SETTINGS", "Set by the host.", new Vector2(760, height));
            for (int i = 0; i < facts.Count; i++)
            {
                float y = -170 - i * rowH;
                var key = HubLobby.Caption(panel, "Key" + i, facts[i].Key, HubKnit.Honey);
                HubKit.Place(key.rectTransform, HubKit.TopLeft, new Vector2(60, y), new Vector2(300, rowH));
                var value = HubKit.Text(panel, "Value" + i, facts[i].Value, HubStyle.Label, true, HubKnit.Cream, TextAnchor.MiddleRight);
                HubKit.Place(value.rectTransform, HubKit.TopRight, new Vector2(-60, y), new Vector2(340, rowH));
                HubKit.Fit(value, 340);
                if (i > 0)
                {
                    var rule = HubKit.Rect(panel, "Rule" + i).gameObject.AddComponent<Image>();
                    rule.color = new Color(HubKnit.Cream.r, HubKnit.Cream.g, HubKnit.Cream.b, 0.12f);
                    rule.raycastTarget = false;
                    HubKit.Place(rule.rectTransform, HubKit.TopLeft, new Vector2(60, y), new Vector2(640, 2));
                }
            }
            _close = HubKnit.Button(panel, "CloseSettings", "GOT IT", Knit.Gold, Close, HubStyle.Title, HubGlyph.Mark.None, primary: true);
            HubKit.Place((RectTransform)_close.transform, HubKit.BottomRight, new Vector2(-48, 36), new Vector2(260, 92));
            var full = HubKnit.Button(panel, "FullRules", "FULL RULES", Knit.Orange, () => { Close(); Hub.Host.OpenCustomRules(); }, HubStyle.Label, HubGlyph.Mark.Book);
            HubKit.Place((RectTransform)full.transform, HubKit.BottomLeft, new Vector2(48, 40), new Vector2(300, 84));
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
            // The room family's question card (owner 2026-10-28): HOME's wine plate, the question in
            // honey, the safe answer in PLAY's lime with focus, leaving in the menu red.
            var panel = HubKnit.Plate(Root, "Panel", Knit.Wine);
            HubKit.Place(panel, HubKit.Centre, Vector2.zero, new Vector2(860, 440));
            var face = HubKnit.FaceOf(panel);
            var title = HubKit.Text(face, "Heading", "CLOSE ROOM?", HubStyle.Display, true, HubKnit.Honey, TextAnchor.MiddleCenter);
            HubKit.Place(title.rectTransform, HubKit.Top, new Vector2(0, -30), new Vector2(780, 96));
            string who = Others <= 0 ? "Nobody else is in it yet."
                : Others == 1 ? "The other player in it goes back to the menu."
                : "The " + Others + " other players in it go back to the menu.";
            var message = HubKit.Text(face, "Consequence", "Leaving closes this room. " + who,
                                      HubStyle.Body, false, HubKnit.Cream, TextAnchor.MiddleCenter);
            HubKit.Place(message.rectTransform, HubKit.Top, new Vector2(0, -136), new Vector2(740, 100));
            _keep = HubKnit.Button(face, "KeepRoom", "KEEP ROOM", Knit.Gold, Close, HubStyle.Title, HubGlyph.Mark.None, primary: true);
            HubKit.Place((RectTransform)_keep.transform, HubKit.BottomLeft, new Vector2(50, 44), new Vector2(360, 112));
            var close = HubKnit.Button(face, "CloseRoom", "CLOSE ROOM", Knit.Red, CloseRoom, HubStyle.Title, HubGlyph.Mark.Exit);
            HubKit.Place((RectTransform)close.transform, HubKit.BottomRight, new Vector2(-50, 50), new Vector2(360, 104));
            HubKnit.FitLabel(close);
            HubSlap.On(panel, 0, -1.5f);
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
