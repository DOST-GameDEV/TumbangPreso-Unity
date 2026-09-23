using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// HOST GAME, the owner's "Custom Game Mode" sheet: lobby name; MAP, GAME MODE and VISIBILITY
    /// dropdowns with a default map set; LAN or ONLINE; CREATE LOBBY. The selected map shows subtly
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
        public override float CourtShade => 0.62f;

        private InputField _name;
        private HubDropdown _map, _mode, _visibility;
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
            var plate = HubKit.Shape(panel, "Plate", HubStyle.Night, false, 901, 6, 30);
            HubKit.Stretch(plate.rectTransform);
            plate.color = new Color(1, 1, 1, 0.94f);

            string me = GameServices.Account?.DisplayName;
            if (string.IsNullOrWhiteSpace(me)) me = Settings.SettingsStore.Current.PlayerName;
            if (string.IsNullOrWhiteSpace(me)) me = "PLAYER";

            HubField.Label(panel, "LOBBY NAME", new Vector2(44, -34));
            _name = HubField.Build(panel, "LobbyName", (me + "'s room").ToUpperInvariant(), "Name your room", 24, 902);
            HubKit.Place((RectTransform)_name.transform, HubKit.TopLeft, new Vector2(40, -76), new Vector2(740, 92));

            string[] maps = new string[SceneFlow.MapRegistry.Length];
            int mapIndex = 0;
            for (int i = 0; i < maps.Length; i++)
            {
                maps[i] = SceneFlow.MapRegistry[i].Name;
                if (SceneFlow.MapRegistry[i].Id == SceneFlow.SelectedMap) mapIndex = i;
            }
            HubField.Label(panel, "MAP", new Vector2(44, -190));
            _map = HubDropdown.Build(panel, Hub, "MapDropdown", "MAP", maps, mapIndex, 903);
            HubKit.Place((RectTransform)_map.transform, HubKit.TopLeft, new Vector2(40, -232), new Vector2(740, 92));
            _map.Changed += i => Hub.Host.SelectMap(SceneFlow.MapRegistry[i].Id);

            HubField.Label(panel, "GAME MODE", new Vector2(44, -346), 360);
            _mode = HubDropdown.Build(panel, Hub, "ModeDropdown", "GAME MODE", new[] { "CLASSIC", "HERO STRIKE" },
                                      SceneFlow.SelectedMode == GameMode.Classic ? 0 : 1, 904);
            HubKit.Place((RectTransform)_mode.transform, HubKit.TopLeft, new Vector2(40, -388), new Vector2(360, 92));

            HubField.Label(panel, "VISIBILITY", new Vector2(424, -346), 360);
            _visibility = HubDropdown.Build(panel, Hub, "VisibilityDropdown", "VISIBILITY", Visibility, 0, 905);
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
            if (this == null) return;
            _busy = false;
            _create.interactable = true;
            HubKit.SetLabel(_create, "CREATE LOBBY");
            if (!string.IsNullOrEmpty(refusal)) { _status.text = refusal; MenuSfx.Error(); return; }
            Hub.ShowLobby();
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
        public override float CourtShade => 0.9f;
        private int _source;     // 0 internet, 1 LAN, 2 code
        private HubButton[] _sources;
        private RectTransform _list, _code;
        private Text _header, _status, _empty;
        private InputField _codeField;
        private bool _busy;
        private float _nextDraw;
        private string _drawn = "";

        public override void Build()
        {
            HubPattern.Ground(Root, HubStyle.ArmyDeep, 61);
            HubChrome.Back(Root, Hub);
            HubChrome.Title(Root, "JOIN GAME");

            var left = HubKit.Place(HubKit.Rect(Root, "Sources"), HubKit.TopLeft, new Vector2(HubKit.Margin, -(HubKit.Margin + 150)), new Vector2(440, 520));
            var sourcePlate = HubKit.Shape(left, "Plate", HubStyle.Night, false, 911, 5, 26);
            HubKit.Stretch(sourcePlate.rectTransform);
            var sourceTitle = HubKit.Text(left, "SourceLabel", "SOURCE", HubStyle.Floor, false, HubStyle.Golden, TextAnchor.MiddleLeft);
            HubKit.Place(sourceTitle.rectTransform, HubKit.TopLeft, new Vector2(34, -24), new Vector2(360, 48));
            string[] names = { "DEDICATED (INTERNET)", "DEDICATED (LAN)", "CODE" };
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

            _list = HubKit.Span(HubKit.Rect(right, "List"), Vector2.zero, Vector2.one, new Vector2(30, 30), new Vector2(30, 150));
            var columns = HubKit.Place(HubKit.Rect(right, "Columns"), HubKit.TopLeft, new Vector2(34, -96), new Vector2(1100, 40));
            Column(columns, "NAME", 0);
            Column(columns, "MAP", 470);
            Column(columns, "PLAYERS", 760);
            _empty = HubKit.Text(_list, "EmptyState", "", HubStyle.Label, true, HubStyle.HoneySoft, TextAnchor.MiddleCenter);
            HubKit.Stretch(_empty.rectTransform, 40);

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

            Hub.Host.Browse();
            Source(0);
            HubSlap.On(left, 0, -1);
            HubSlap.On(right, 0.06f, 1);
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
            _code.gameObject.SetActive(code);
            _list.gameObject.SetActive(!code);
            _list.parent.Find("Columns").gameObject.SetActive(!code);
            _header.text = code ? "JOIN BY CODE" : index == 0 ? "SERVERS (ONLINE)" : "SERVERS (LAN)";
            _drawn = "";
            Draw();
            if (code) UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(_codeField.gameObject);
            Hub.RefreshFocus();
        }

        public override void Tick()
        {
            if (Time.unscaledTime < _nextDraw) return;
            _nextDraw = Time.unscaledTime + 1.0f;
            Draw();
        }

        private void Draw()
        {
            if (_source == 2) return;
            var rooms = Hub.Host.Rooms(_source == 1);
            string key = _source + ":";
            foreach (var r in rooms) key += r.Key + r.Players + r.InProgress + ";";
            if (key == _drawn) return;
            _drawn = key;

            for (int i = _list.childCount - 1; i >= 0; i--)
                if (_list.GetChild(i) != _empty.transform) Destroy(_list.GetChild(i).gameObject);
            _empty.text = rooms.Count == 0
                ? (_source == 1 ? "Looking for rooms on your network...\nNobody yet. Host one, or ask for a code."
                                : "Looking for public rooms online...\nNobody yet. Host one, or ask for a code.")
                : "";

            for (int i = 0; i < Mathf.Min(rooms.Count, 5); i++)
            {
                var room = rooms[i];
                var row = HubKit.Place(HubKit.Rect(_list, "Room" + i), HubKit.TopLeft, new Vector2(0, -(i * 104)), new Vector2(1180, 92));
                var plate = HubKit.Shape(row, "Plate", HubStyle.ArmyDeep, false, 940 + i, 4, 16);
                HubKit.Stretch(plate.rectTransform);
                var name = HubKit.Text(row, "Name", room.Name, HubStyle.Label, true, HubStyle.Honey, TextAnchor.MiddleLeft);
                HubKit.Place(name.rectTransform, HubKit.Left, new Vector2(20, 0), new Vector2(440, 70));
                HubKit.Fit(name, 440);
                var map = HubKit.Text(row, "Map", room.Map, HubStyle.Body, false, HubStyle.Honey, TextAnchor.MiddleLeft);
                HubKit.Place(map.rectTransform, HubKit.Left, new Vector2(474, 0), new Vector2(270, 60));
                HubKit.Fit(map, 270);
                var players = HubKit.Text(row, "Players", room.InProgress ? "IN A MATCH" : room.Players + " / " + room.Capacity, HubStyle.Body, false, HubStyle.Honey, TextAnchor.MiddleLeft);
                HubKit.Place(players.rectTransform, HubKit.Left, new Vector2(764, 0), new Vector2(200, 60));
                string key2 = room.Key;
                var join = HubKit.Button(row, "Join" + i, "JOIN", HubStyle.Chartreuse, () => Join(key2), HubStyle.Label, 950 + i);
                HubKit.Place((RectTransform)join.transform, HubKit.Right, new Vector2(-14, 0), new Vector2(180, 72));
                join.interactable = !room.InProgress && room.Players < room.Capacity;
            }
            Hub.RefreshFocus();
        }

        private async void Join(string key)
        {
            if (_busy || string.IsNullOrWhiteSpace(key)) return;
            _busy = true;
            _status.text = "Joining " + key.Trim().ToUpperInvariant() + "...";
            string refusal = await Hub.Host.Join(key.Trim());
            if (this == null) return;
            _busy = false;
            if (!string.IsNullOrEmpty(refusal)) { _status.text = refusal; MenuSfx.Error(); return; }
            Hub.ShowLobby();
        }
    }

    /// <summary>
    /// GAME LOBBY: BACK, the lobby's name, the map behind everything, the players n/4 with their
    /// portraits and the host marked, START GAME, and the character and settings icons bottom right.
    ///
    /// THE FOUR ANSWERS:
    ///   the one thing   the players, n of 4
    ///   first press     START GAME for the host; READY for everyone else, in the same place
    ///   not needed now  the match rules, behind the settings icon
    ///   out             BACK or Escape leaves the room (the host's leaving closes it)
    /// </summary>
    public sealed class HubLobby : HubScreen
    {
        public override float CourtShade => 0.3f;
        private Text _title, _code, _count, _mapLine, _status, _address;
        private RectTransform _rows;
        private HubButton _primary, _map, _watch;

        public override Selectable FirstFocus => _primary;
        private string _drawn = "";

        public override void Build()
        {
            HubChrome.Back(Root, Hub);
            _title = HubChrome.Title(Root, "LOBBY");

            var codeChip = HubKit.Button(Root, "CodeChip", null, HubStyle.Night, CopyCode, 0, 961);
            HubKit.Place((RectTransform)codeChip.transform, HubKit.TopRight, new Vector2(-HubKit.Margin, -HubKit.Margin), new Vector2(420, 110));
            _code = HubKit.Text(codeChip.Body, "Code", "", HubStyle.Title, true, HubStyle.Golden, TextAnchor.MiddleCenter);
            HubKit.Stretch(_code.rectTransform, 10);
            _address = HubKit.Text(Root, "Address", "", HubStyle.Floor, false, HubStyle.Honey, TextAnchor.MiddleRight);
            HubKit.Place(_address.rectTransform, HubKit.TopRight, new Vector2(-HubKit.Margin, -(HubKit.Margin + 120)), new Vector2(600, 48));
            _address.gameObject.AddComponent<Shadow>().effectColor = HubStyle.Ink;

            _mapLine = HubKit.Text(Root, "MapLine", "", HubStyle.Label, true, HubStyle.Honey, TextAnchor.MiddleLeft);
            HubKit.Place(_mapLine.rectTransform, HubKit.TopLeft, new Vector2(HubKit.Margin + HubChrome.BarHeight + 34, -HubKit.Margin - 100), new Vector2(900, 50));

            var panel = HubKit.Place(HubKit.Rect(Root, "Players"), HubKit.Centre, new Vector2(0, 10), new Vector2(900, 620));
            var plate = HubKit.Shape(panel, "Plate", HubStyle.Night, false, 962, 6, 30);
            HubKit.Stretch(plate.rectTransform);
            plate.color = new Color(1, 1, 1, 0.93f);
            _count = HubKit.Text(panel, "Heading", "", HubStyle.Title, true, HubStyle.Golden, TextAnchor.MiddleLeft);
            HubKit.Place(_count.rectTransform, HubKit.TopLeft, new Vector2(36, -22), new Vector2(600, 64));
            _rows = HubKit.Span(HubKit.Rect(panel, "Rows"), Vector2.zero, Vector2.one, new Vector2(30, 30), new Vector2(30, 104));
            _watch = HubKit.Button(panel, "WatchToggle", "WATCH INSTEAD", HubStyle.Honey, () => { Hub.Host.ToggleSpectate(); _drawn = ""; },
                                   HubStyle.Floor, 968, HubGlyph.Mark.Eye);
            HubKit.Place((RectTransform)_watch.transform, HubKit.TopRight, new Vector2(-30, -20), new Vector2(300, 72));

            _primary = HubKit.Button(Root, "StartGame", "START GAME", HubStyle.Chartreuse, Primary, HubStyle.Title, 963);
            HubKit.Place((RectTransform)_primary.transform, HubKit.Bottom, new Vector2(0, HubKit.Margin), new Vector2(560, 130));
            _primary.Shape.BandFraction = 0.12f;
            _status = HubKit.Text(Root, "Status", "", HubStyle.Floor, false, HubStyle.Honey, TextAnchor.MiddleCenter);
            HubKit.Place(_status.rectTransform, HubKit.Bottom, new Vector2(0, HubKit.Margin + 140), new Vector2(1200, 48));

            // Bottom right: character and loadout, settings, chat. Each a square sticker with its name.
            Door("CharacterDoor", HubGlyph.Mark.Person, "CHARACTER", 0, () => Hub.Push<HubCharacterSelect>(c => c.Timed = false));
            Door("SettingsDoor", HubGlyph.Mark.Gear, "RULES", 1, () => Hub.Host.OpenCustomRules());
            Door("ChatDoor", HubGlyph.Mark.Friends, "CHAT", 2, () => Hub.Host.ToggleChat());

            _map = HubKit.Button(Root, "MapDoor", null, HubStyle.Honey, ChangeMap, 0, 967, HubGlyph.Mark.House);
            HubKit.Place((RectTransform)_map.transform, HubKit.BottomLeft, new Vector2(HubKit.Margin, HubKit.Margin), new Vector2(420, 110));
            var mapLabel = HubKit.Text(_map.Body, "Label", "CHANGE MAP", HubStyle.Label, true, HubStyle.Ink, TextAnchor.MiddleCenter);
            HubKit.Stretch(mapLabel.rectTransform);
            mapLabel.rectTransform.offsetMin = new Vector2(72, 6);

            Draw(true);
            HubSlap.On(panel, 0, -1);
        }

        private void Door(string name, HubGlyph.Mark mark, string words, int index, System.Action action)
        {
            var door = HubKit.Button(Root, name, null, HubStyle.Honey, action, 0, 964 + index);
            // ⚠️ 176 WIDE, FOR "CHARACTER" AT THE 28 FLOOR, which overran a 150 door in the first capture.
            HubKit.Place((RectTransform)door.transform, HubKit.BottomRight, new Vector2(-(HubKit.Margin + index * 192), HubKit.Margin), new Vector2(176, 150));
            var glyph = HubKit.Glyph(door.Body, "Icon", mark, HubStyle.Ink, 0.1f);
            HubKit.Place(glyph.rectTransform, HubKit.Top, new Vector2(0, -12), new Vector2(80, 80));
            var label = HubKit.Text(door.Body, "Label", words, HubStyle.Floor, true, HubStyle.Ink, TextAnchor.MiddleCenter);
            HubKit.Place(label.rectTransform, HubKit.Bottom, new Vector2(0, 10), new Vector2(164, 40));
            HubKit.Fit(label, 160);
        }

        private void ChangeMap()
        {
            string[] maps = new string[SceneFlow.MapRegistry.Length];
            int current = 0;
            for (int i = 0; i < maps.Length; i++)
            {
                maps[i] = SceneFlow.MapRegistry[i].Name;
                if (SceneFlow.MapRegistry[i].Id == SceneFlow.SelectedMap) current = i;
            }
            Hub.Push<HubChoicePopup>(p =>
            {
                p.Title = "MAP";
                p.Options = maps;
                p.Current = current;
                p.Chosen = i => { Hub.Host.SelectMap(SceneFlow.MapRegistry[i].Id); _drawn = ""; };
            });
        }

        private void CopyCode()
        {
            GUIUtility.systemCopyBuffer = Hub.Host.RoomCode ?? "";
            Hub.Toast("Code copied: " + Hub.Host.RoomCode);
        }

        public override void Tick() => Draw(false);

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
            string key = host.RoomTitle + host.RoomCode + host.IsHost + host.LocalReady + SceneFlow.SelectedMap + SceneFlow.SelectedMode + host.Spectating;
            foreach (var s in seats) key += s.Occupied + ":" + s.Name + ":" + s.CharacterPick + ":" + s.Ready + ":" + s.Bot + "|";
            if (!force && key == _drawn) return;
            _drawn = key;

            _title.text = string.IsNullOrWhiteSpace(host.RoomTitle) ? "LOBBY" : host.RoomTitle.ToUpperInvariant();
            _title.fontSize = HubStyle.Size(HubStyle.Display);
            HubKit.Fit(_title, 900);
            _code.text = "CODE  " + (host.RoomCode ?? "");
            string address = host.RoomAddress;
            _address.text = string.IsNullOrEmpty(address) ? "" : "or join by address  " + address;
            HubKit.SetLabel(_watch, host.Spectating ? "TAKE A SEAT" : "WATCH INSTEAD");
            HubKit.LabelOf(_watch).fontSize = HubStyle.Size(HubStyle.Floor);
            HubKit.Fit(HubKit.LabelOf(_watch), 210);
            _mapLine.text = SceneFlow.PreviewFor(SceneFlow.SelectedMap).Name + "   ·   " +
                            (SceneFlow.SelectedMode == GameMode.Classic ? "CLASSIC" : "HERO STRIKE") + "   ·   " +
                            (host.RoomOnline ? "ONLINE" : "LAN");

            int occupied = 0;
            foreach (var s in seats) if (s.Occupied) occupied++;
            _count.text = "PLAYERS  " + occupied + " / " + Balance.PlayerCount;

            for (int i = _rows.childCount - 1; i >= 0; i--) Destroy(_rows.GetChild(i).gameObject);
            var people = Roster.GetPeople(SceneFlow.SelectedMode);
            for (int i = 0; i < seats.Length; i++)
            {
                var seat = seats[i];
                var row = HubKit.Place(HubKit.Rect(_rows, "Seat" + i), HubKit.TopLeft, new Vector2(0, -(i * 118)), new Vector2(840, 106));
                var plate = HubKit.Shape(row, "Plate", seat.Mine ? HubStyle.Persimmon : seat.Occupied ? HubStyle.ArmyDeep : new Color(0.2f, 0.12f, 0.06f), false, 970 + i, 4, 18);
                HubKit.Stretch(plate.rectTransform);
                var person = seat.Occupied ? Roster.At(people, seat.CharacterPick) : null;
                var face = HubKit.Picture(row, "Face", HubKit.Portrait(person?.Id));
                HubKit.Place(face.rectTransform, HubKit.Left, new Vector2(8, 2), new Vector2(96, 96));
                if (face.sprite == null)
                {
                    var icon = HubKit.Glyph(row, "Icon", seat.Occupied ? HubGlyph.Mark.Person : HubGlyph.Mark.Bots, HubStyle.HoneySoft);
                    HubKit.Place(icon.rectTransform, HubKit.Left, new Vector2(26, 0), new Vector2(60, 60));
                }
                Color ink = seat.Mine ? HubStyle.Ink : HubStyle.Honey;
                string name = seat.Occupied ? (seat.Mine ? "YOU" : seat.Name) : seat.Bot ? "BOT" : "OPEN";
                var label = HubKit.Text(row, "Name", name, HubStyle.Label, true, ink, TextAnchor.MiddleLeft);
                HubKit.Place(label.rectTransform, HubKit.Left, new Vector2(122, 0), new Vector2(420, 70));
                HubKit.Fit(label, 420);
                if (seat.Host)
                {
                    var crown = HubKit.Glyph(row, "HostMark", HubGlyph.Mark.Crown, HubStyle.Golden);
                    HubKit.Place(crown.rectTransform, HubKit.Right, new Vector2(-150, 0), new Vector2(54, 54));
                }
                string state = seat.Host ? "HOST" : seat.Ready ? "READY" : seat.Occupied ? "" : "";
                var tag = HubKit.Text(row, "State", state, HubStyle.Floor, true, seat.Ready ? HubStyle.Chartreuse : ink, TextAnchor.MiddleRight);
                HubKit.Place(tag.rectTransform, HubKit.Right, new Vector2(-24, 0), new Vector2(120, 50));
            }

            if (host.IsHost)
            {
                HubKit.SetLabel(_primary, "START GAME");
                _primary.interactable = true;
                _status.text = occupied < Balance.PlayerCount ? "Empty seats are filled by bots." : "Everyone is here.";
            }
            else
            {
                HubKit.SetLabel(_primary, host.LocalReady ? "READY  ✓" : "READY");
                _primary.interactable = true;
                _status.text = host.LocalReady ? "Waiting for the host." : "The host starts the match.";
            }
            _map.gameObject.SetActive(host.IsHost);
            Hub.RefreshFocus();
        }

        private void Primary()
        {
            if (Hub.Host.IsHost) Hub.Host.StartGame();
            else Hub.Host.ToggleReady();
            _drawn = "";
        }

        public override bool Back()
        {
            Hub.Host.LeaveRoom();
            Hub.Home();
            return true;
        }
    }
}
