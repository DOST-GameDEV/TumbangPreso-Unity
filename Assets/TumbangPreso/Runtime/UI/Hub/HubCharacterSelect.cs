using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// CHARACTER SELECT, for everyone after MATCH FOUND: the big NAME and model, a 3x4 portrait grid,
    /// SELECT. The owner's reference is Valorant's agent select.
    ///
    /// ⚠️⚠️ THE PICK RULES ARE TODAY'S, UNCHANGED. A pick is `SelectLobbyPickServerRpc`, exactly what
    /// the old picker sent, so duplicates are allowed as they are now and a Classic pick is a cosmetic
    /// with neutral stats (`VISION.md` § 1). SELECT locks in with the lobby's own ready message
    /// (`DeclareReadyServerRpc`), whose tally every peer already receives, and the HOST starts when
    /// every human is locked or the clock runs out. No new message, no protocol bump.
    ///
    /// THE FOUR ANSWERS:
    ///   the one thing   the fighter you are choosing, big, on the left
    ///   first press     a face in the grid (it picks at once, like the reference); SELECT locks in
    ///   not needed now  the loadout, which is its own screen from HOME
    ///   out             none while timed: a found match is not left from here. Untimed (from a
    ///                   custom lobby) BACK returns to the lobby with the pick kept.
    /// </summary>
    public sealed class HubCharacterSelect : HubScreen
    {
        public override float CourtShade => 0.78f;
        public override bool ShowsQueuePlate => false;

        /// <summary>A queue match: the clock runs and SELECT locks in. False from a custom lobby.</summary>
        public bool Timed;
        public const float Seconds = 30.0f;

        private float _endsAt;
        private bool _locked, _started;
        private ModelPreview _preview;
        private Text _name, _role, _clock, _hint;
        private HubButton _select;
        private readonly List<HubButton> _cells = new List<HubButton>();
        private RectTransform _seats;
        private int _pick;
        private string _shownSeats = "";

        private GameMode Mode => SceneFlow.SelectedMode;
        private IReadOnlyList<RosterEntry> People => Roster.GetPeople(Mode);

        public override void Build()
        {
            HubPattern.Ground(Root, HubStyle.Night, 51);
            _pick = Mathf.Clamp(Settings.SettingsStore.Current.CharacterPick, 0, People.Count - 1);

            // The stage and the name: the left 55 per cent.
            var stage = HubKit.Span(HubKit.Rect(Root, "Stage"), Vector2.zero, new Vector2(0.55f, 1),
                                    new Vector2(HubKit.Margin, 190), new Vector2(0, 150));
            var floor = HubKit.Shape(stage, "Floor", HubStyle.ArmyDeep, false, 701, 5, 34);
            HubKit.Place(floor.rectTransform, HubKit.Bottom, new Vector2(0, 0), new Vector2(760, 110));
            var model = HubKit.Stretch(HubKit.Rect(stage, "Model"));
            _preview = model.gameObject.AddComponent<ModelPreview>();
            _preview.Attach(model);
            _preview.CentreSubject();

            _role = HubKit.Text(Root, "Role", "", HubStyle.Label, false, HubStyle.Golden, TextAnchor.MiddleLeft);
            HubKit.Place(_role.rectTransform, HubKit.TopLeft, new Vector2(HubKit.Margin + 6, -HubKit.Margin), new Vector2(900, 44));
            _name = HubKit.Text(Root, "Heading", "", HubStyle.Hero, true, HubStyle.Honey, TextAnchor.MiddleLeft);
            HubKit.Place(_name.rectTransform, HubKit.TopLeft, new Vector2(HubKit.Margin, -HubKit.Margin - 30), new Vector2(980, 150));
            var shadow = _name.gameObject.AddComponent<Shadow>();
            shadow.effectColor = HubStyle.Ink; shadow.effectDistance = new Vector2(6, -7);

            _clock = HubKit.Text(Root, "Clock", "", HubStyle.Display, true, HubStyle.Honey, TextAnchor.MiddleCenter);
            HubKit.Place(_clock.rectTransform, HubKit.Top, new Vector2(0, -HubKit.Margin), new Vector2(240, 100));
            _clock.gameObject.SetActive(Timed);

            // The grid: 3 columns by 4 rows, right side.
            var grid = HubKit.Place(HubKit.Rect(Root, "Grid"), HubKit.TopRight, new Vector2(-HubKit.Margin, -(HubKit.Margin + 110)), new Vector2(3 * 160 + 2 * 16, 4 * 160 + 3 * 16));
            for (int i = 0; i < 12; i++)
            {
                int index = i;
                bool real = i < People.Count;
                var cell = HubKit.Button(grid, "Portrait" + i, null, HubStyle.Honey, real ? () => Pick(index) : (System.Action)null, 0, 710 + i);
                HubKit.Place((RectTransform)cell.transform, HubKit.TopLeft, new Vector2((i % 3) * 176, -(i / 3) * 176), new Vector2(160, 160));
                if (real)
                {
                    var face = HubKit.Picture(cell.Body, "Face", HubKit.Portrait(People[i].Id));
                    HubKit.Stretch(face.rectTransform, 10);
                }
                else
                {
                    cell.interactable = false;
                    HubKit.SetFill(cell, HubStyle.ArmyDeep);
                }
                _cells.Add(cell);
            }

            _select = HubKit.Button(Root, "SelectButton", "SELECT", HubStyle.Chartreuse, Select, HubStyle.Display, 730);
            HubKit.Place((RectTransform)_select.transform, HubKit.BottomRight, new Vector2(-HubKit.Margin, HubKit.Margin), new Vector2(3 * 160 + 2 * 16, 130));
            _select.Shape.BandFraction = 0.12f;
            _hint = HubKit.Text(Root, "Hint", "", HubStyle.Floor, false, HubStyle.HoneySoft, TextAnchor.MiddleRight);
            HubKit.Place(_hint.rectTransform, HubKit.BottomRight, new Vector2(-(HubKit.Margin + 512 + 30), HubKit.Margin + 140), new Vector2(700, 84));
            _hint.alignment = TextAnchor.LowerRight;

            // Everyone in the match along the bottom left, with their pick and whether they locked.
            _seats = HubKit.Place(HubKit.Rect(Root, "Seats"), HubKit.BottomLeft, new Vector2(HubKit.Margin, HubKit.Margin), new Vector2(4 * 300, 130));

            if (!Timed)
            {
                HubChrome.Back(Root, Hub);
                HubKit.Place(_role.rectTransform, HubKit.TopLeft, new Vector2(HubKit.Margin + HubChrome.BarHeight + 30, -HubKit.Margin), new Vector2(900, 44));
                HubKit.Place(_name.rectTransform, HubKit.TopLeft, new Vector2(HubKit.Margin + HubChrome.BarHeight + 24, -HubKit.Margin - 30), new Vector2(900, 150));
            }

            _endsAt = Time.unscaledTime + Seconds;
            Show();
            HubSlap.On(grid, 0.04f, 2);
            HubSlap.On(_select.transform, 0.1f, -2);
        }

        private bool Usable(int index)
        {
            var person = Roster.At(People, index);
            if (person == null) return false;
            bool online = Net.WalletStore.OnlineRoute;
            return !online || HubOwnership.OwnsHero(person.Id);
        }

        private void Pick(int index)
        {
            if (_locked) return;
            _pick = index;
            var settings = Settings.SettingsStore.Current;
            settings.CharacterPick = index;
            var person = Roster.At(People, index);
            if (person != null)
            {
                settings.SlipperPick = Settings.SettingsStore.SlipperPickFor(person.Id);
                settings.CanPick = Settings.SettingsStore.CanPickFor(person.Id);
            }
            Settings.SettingsStore.Save();
            if (Usable(index)) Hub.Host.PublishPicks();
            Show();
        }

        private void Select()
        {
            if (!Usable(_pick)) { MenuSfx.Error(); Hub.Toast("Unlock this hero in the SHOP to play it online. Try it in Practice."); return; }
            Hub.Host.PublishPicks();
            if (!Timed) { Close(); return; }
            _locked = true;
            Hub.Host.LockIn();
            MenuSfx.Valid();
            Show();
        }

        private void Show()
        {
            var person = Roster.At(People, _pick);
            if (person == null) return;
            string tagline = ConvertedCharacterSelect.TaglineFor(person.Id) ?? "";
            string[] parts = tagline.Split('\n');
            _role.text = Mode == GameMode.HeroStrike && parts.Length > 1 ? "//  " + parts[0] : "//  CLASSIC";
            _name.text = person.Name;
            _name.fontSize = HubStyle.Size(HubStyle.Hero);
            HubKit.Fit(_name, 900);

            var art = RosterBook.Load().PersonArt(_pick, Mode);
            if (art != null) _preview.Show(art.Model, art.Clips, art.Palette, art.PetModel);
            _preview.SetTileFraming(0.95f);

            for (int i = 0; i < _cells.Count && i < People.Count; i++)
            {
                bool usable = Usable(i);
                HubKit.SetFill(_cells[i], i == _pick ? HubStyle.Persimmon : HubStyle.Honey);
                _cells[i].Hatched = !usable;
                _cells[i].interactable = !_locked;
            }

            bool can = Usable(_pick);
            HubKit.SetLabel(_select, _locked ? "LOCKED IN" : Timed ? "SELECT" : "DONE");
            HubKit.LabelOf(_select).fontSize = HubStyle.Size(HubStyle.Display);
            _select.interactable = !_locked;
            HubKit.SetFill(_select, can ? HubStyle.Chartreuse : HubStyle.Honey);
            _hint.text = !can ? "Not unlocked for online play. Unlock it in the SHOP, or try it in Practice."
                       : _locked ? "Waiting for everyone to lock in." : "";
        }

        public override bool Back()
        {
            if (Timed) return true;   // a found match is not left from the pick screen
            Hub.Host.PublishPicks();
            return false;
        }

        public override void Tick()
        {
            DrawSeats();
            if (!Timed) return;

            float left = Mathf.Max(0, _endsAt - Time.unscaledTime);
            _clock.text = Mathf.CeilToInt(left).ToString();
            _clock.color = left < 6 ? HubStyle.Persimmon : HubStyle.Honey;

            if (left <= 0 && !_locked && Usable(_pick)) Select();
            if (_started || !Hub.Host.IsHost) return;

            // ⚠️ THE HOST STARTS, NOBODY ELSE CAN (`MatchRpc.HostStartMatch` is host-only). Everyone
            // locked, or the clock out: a pick left unlocked at zero is the pick the player is on.
            bool everyone = _locked;
            foreach (var seat in Hub.Host.Seats())
                if (seat.Occupied && !seat.Bot && !seat.Mine && !seat.Ready) everyone = false;
            if (everyone || left <= 0)
            {
                _started = true;
                Hub.Host.StartGame();
            }
        }

        private void DrawSeats()
        {
            var seats = Hub.Host.Seats();
            string key = "";
            foreach (var s in seats) key += s.Occupied + ":" + s.CharacterPick + ":" + s.Ready + ":" + s.Name + "|";
            if (key == _shownSeats) return;
            _shownSeats = key;

            for (int i = _seats.childCount - 1; i >= 0; i--) Destroy(_seats.GetChild(i).gameObject);
            for (int i = 0; i < seats.Length; i++)
            {
                var seat = seats[i];
                var card = HubKit.Place(HubKit.Rect(_seats, "Seat" + i), HubKit.BottomLeft, new Vector2(i * 300, 0), new Vector2(284, 130));
                var plate = HubKit.Shape(card, "Plate", seat.Mine ? HubStyle.Persimmon : HubStyle.ArmyDeep, false, 740 + i, 4, 18);
                HubKit.Stretch(plate.rectTransform);
                var person = seat.Occupied ? Roster.At(People, seat.CharacterPick) : null;
                var face = HubKit.Picture(card, "Face", HubKit.Portrait(person?.Id));
                HubKit.Place(face.rectTransform, HubKit.Left, new Vector2(6, 4), new Vector2(110, 110));
                if (face.sprite == null)
                {
                    var bot = HubKit.Glyph(card, "Bot", seat.Occupied ? HubGlyph.Mark.Person : HubGlyph.Mark.Bots, HubStyle.HoneySoft);
                    HubKit.Place(bot.rectTransform, HubKit.Left, new Vector2(24, 0), new Vector2(72, 72));
                }
                Color ink = seat.Mine ? HubStyle.Ink : HubStyle.Honey;
                var name = HubKit.Text(card, "Name", seat.Mine ? "YOU" : seat.Occupied ? seat.Name : "BOT", HubStyle.Floor, true, ink, TextAnchor.MiddleLeft);
                HubKit.Place(name.rectTransform, HubKit.TopLeft, new Vector2(118, -18), new Vector2(156, 40));
                HubKit.Fit(name, 156);
                var state = HubKit.Text(card, "State", seat.Ready || (seat.Mine && _locked) ? "LOCKED" : seat.Occupied ? "PICKING" : "", HubStyle.Floor, false, ink, TextAnchor.MiddleLeft);
                HubKit.Place(state.rectTransform, HubKit.BottomLeft, new Vector2(118, 18), new Vector2(156, 40));
                HubKit.Fit(state, 156);
            }
        }
    }
}
