using System.Collections.Generic;
using TumbangPreso.Abilities;
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
        private HubButton[] _abilityButtons;
        private TumpAbilitySymbol[] _abilitySymbols;
        private Text _abilityName, _abilityMeta, _abilitySummary;
        private HeroKit _shownKit;
        private HeroAbility[] _shownAbilities;
        private string _shownHero;
        private int _inspectedAbility;

        private GameMode Mode => SceneFlow.SelectedMode;
        private IReadOnlyList<RosterEntry> People => Roster.GetPeople(Mode);

        public override void Build()
        {
            HubPattern.Ground(Root, HubStyle.Night, 51);
            _pick = Mathf.Clamp(Settings.SettingsStore.Current.CharacterPick, 0, People.Count - 1);

            // The stage and the name: the left 55 per cent.
            var stage = HubKit.Span(HubKit.Rect(Root, "Stage"), Vector2.zero, new Vector2(Mode == GameMode.HeroStrike ? 0.46f : 0.55f, 1),
                                    new Vector2(HubKit.Margin, 190), new Vector2(0, 150));
            var floor = HubKit.Shape(stage, "Floor", HubStyle.ArmyDeep, false, 701, 5, 34);
            HubKit.Place(floor.rectTransform, HubKit.Bottom, new Vector2(0, 0), new Vector2(760, 110));
            var model = HubKit.Stretch(HubKit.Rect(stage, "Model"));
            _preview = model.gameObject.AddComponent<ModelPreview>();
            _preview.Attach(model);
            _preview.CentreSubject();

            _role = HubKit.Text(Root, "Role", "", HubStyle.Label, false, HubStyle.Golden, TextAnchor.MiddleLeft);
            HubKit.Place(_role.rectTransform, HubKit.TopLeft, new Vector2(HubKit.Margin + 6, -HubKit.Margin), new Vector2(900, 56));
            _name = HubKit.Text(Root, "Heading", "", HubStyle.Hero, true, HubStyle.Honey, TextAnchor.MiddleLeft);
            HubKit.Place(_name.rectTransform, HubKit.TopLeft, new Vector2(HubKit.Margin, -HubKit.Margin - 44), new Vector2(980, 150));
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
            HubKit.Place(_hint.rectTransform, HubKit.BottomRight, new Vector2(-(HubKit.Margin + 512 + 30), HubKit.Margin + 140), new Vector2(700, 96));
            _hint.alignment = TextAnchor.LowerRight;
            if (Mode == GameMode.HeroStrike)
            {
                HubKit.Place(_hint.rectTransform, HubKit.TopRight, new Vector2(-HubKit.Margin, -HubKit.Margin), new Vector2(512, 96));
                _hint.alignment = TextAnchor.UpperRight;
                BuildAbilityReadout();
            }

            // Everyone in the match along the bottom left, with their pick and whether they locked.
            _seats = HubKit.Place(HubKit.Rect(Root, "Seats"), HubKit.BottomLeft, new Vector2(HubKit.Margin, HubKit.Margin), new Vector2(4 * 300, 130));

            if (!Timed)
            {
                HubChrome.Back(Root, Hub);
                HubKit.Place(_role.rectTransform, HubKit.TopLeft, new Vector2(HubKit.Margin + HubChrome.BarHeight + 30, -HubKit.Margin), new Vector2(900, 56));
                HubKit.Place(_name.rectTransform, HubKit.TopLeft, new Vector2(HubKit.Margin + HubChrome.BarHeight + 24, -HubKit.Margin - 44), new Vector2(900, 150));
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
            if (_abilityButtons != null)
            {
                _shownHero = person.Id;
                _shownKit = HeroAbilitySystem.CreateKitFor(person.Id);
                _shownAbilities = new[] { _shownKit.Skill1, _shownKit.Skill2, _shownKit.Ultimate };
                for (int i = 0; i < _shownAbilities.Length; i++) _abilitySymbols[i].Glyph = _shownAbilities[i].Glyph;
                InspectAbility(_inspectedAbility);
            }

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
            _hint.text = !can ? "Unlock in the SHOP, or try it in Practice."
                       : _locked ? "Waiting for everyone to lock in." : "";
        }

        private void BuildAbilityReadout()
        {
            // VISION 3's Learn layer stays on the timed selector. A popup would make
            // TumpHub stop ticking this screen and could suspend the host's deadline.
            var panel = HubKit.Place(HubKit.Rect(Root, "SelectionAbilities"), HubKit.TopRight,
                new Vector2(-(HubKit.Margin + 512 + 32), -(HubKit.Margin + 210)), new Vector2(380, 610));
            HubKit.Stretch(HubKit.Shape(panel, "Plate", HubStyle.Night, false, 745, 5, 24).rectTransform);
            _abilityButtons = new HubButton[3];
            _abilitySymbols = new TumpAbilitySymbol[3];
            for (int i = 0; i < 3; i++)
            {
                int slot = i;
                var button = HubKit.Button(panel, "SelectionAbility" + i, null, HubStyle.Honey,
                    () => InspectAbility(slot), 0, 746 + i);
                HubKit.Place((RectTransform)button.transform, HubKit.TopLeft, new Vector2(20 + i * 120, -22), new Vector2(100, 100));
                var glyph = HubKit.Rect(button.Body, "Glyph").gameObject.AddComponent<TumpAbilitySymbol>();
                HubKit.Stretch(glyph.rectTransform, 12);
                glyph.color = HubStyle.Ink; glyph.raycastTarget = false;
                _abilityButtons[i] = button; _abilitySymbols[i] = glyph;
            }
            _abilityName = HubKit.Text(panel, "AbilityName", "", HubStyle.Label, true, HubStyle.Honey, TextAnchor.UpperLeft);
            HubKit.Place(_abilityName.rectTransform, HubKit.TopLeft, new Vector2(20, -145), new Vector2(340, 108));
            _abilityMeta = HubKit.Text(panel, "AbilityMeta", "", HubStyle.Floor, false, HubStyle.Golden, TextAnchor.UpperLeft);
            HubKit.Place(_abilityMeta.rectTransform, HubKit.TopLeft, new Vector2(20, -264), new Vector2(340, 92));
            _abilitySummary = HubKit.Text(panel, "AbilitySummary", "", HubStyle.Body, false, HubStyle.Honey, TextAnchor.UpperLeft);
            HubKit.Place(_abilitySummary.rectTransform, HubKit.TopLeft, new Vector2(20, -366), new Vector2(340, 220));
        }

        private void InspectAbility(int slot)
        {
            if (_shownAbilities == null) return;
            _inspectedAbility = Mathf.Clamp(slot, 0, 2);
            var ability = _shownAbilities[_inspectedAbility];
            AbilityVariant variant = null;
            if (_inspectedAbility < 2)
            {
                var settings = Settings.SettingsStore.Current;
                var build = HeroBuildRules.RowFor(settings.HeroBuilds, _shownHero);
                variant = HeroBuildRules.Equipped(build, _shownHero, _inspectedAbility + 1, settings.AbilityChallenges);
            }
            bool alternate = variant != null && !variant.IsDefault;
            _abilityName.text = (alternate ? variant.Name : ability.Name).ToUpperInvariant();
            _abilitySummary.text = alternate ? variant.Description : ability.Summary;
            string resource = _inspectedAbility == 2 ? _shownKit.UltimateCost.ToString("0") + " CHARGE"
                : ability.UsesCharges ? ability.MaxCharges + (ability.MaxCharges == 1 ? " USE" : " USES")
                : ability.Cooldown.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " s cooldown";
            _abilityMeta.text = AbilityIcons.LabelFor(ability.Glyph) + "\n" + resource;
            for (int i = 0; i < _abilityButtons.Length; i++)
                HubKit.SetFill(_abilityButtons[i], i == _inspectedAbility ? HubStyle.Persimmon : i == 2 ? HubStyle.Golden : HubStyle.Honey);
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
                HubKit.Place(state.rectTransform, HubKit.BottomLeft, new Vector2(118, 14), new Vector2(156, 48));
                HubKit.Fit(state, 156);
            }
        }
    }
}
