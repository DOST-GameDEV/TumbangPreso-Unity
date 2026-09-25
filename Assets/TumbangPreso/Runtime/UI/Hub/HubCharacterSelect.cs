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
        private HeroKit.ScreenSlot[] _shownSlots;
        private string _shownHero;
        private int _inspectedAbility;

        private GameMode Mode => SceneFlow.SelectedMode;
        private IReadOnlyList<RosterEntry> People => Roster.GetPeople(Mode);

        public override void Build()
        {
            // ⚠️ THE COURT AT NIGHT (`HubScenery`, 2026-09-24): the last screen before the match is
            // the street itself after dark, one warm light on the chalk circle the picked hero
            // steps into. It is the only screen on the road at night,
            // so the step from here into the match reads as walking onto the court.
            bool wide = Mode == GameMode.HeroStrike;
            HubScenery.NightCourtGround(Root, 51, new Vector2(wide ? 0.23f : 0.27f, 0.3f), new Vector2(1150, 640));
            _pick = Mathf.Clamp(Settings.SettingsStore.Current.CharacterPick, 0, People.Count - 1);

            // The stage and the name: the left 55 per cent.
            var stage = HubKit.Span(HubKit.Rect(Root, "Stage"), Vector2.zero, new Vector2(Mode == GameMode.HeroStrike ? 0.46f : 0.55f, 1),
                                    new Vector2(HubKit.Margin, 190), new Vector2(0, 150));
            // ⚠️ A CONTACT SHADOW, NOT A PLINTH (2026-09-23 UI review). The model used to stand on
            // a 760 by 110 olive rounded rectangle with an ink outline, which read as an empty
            // text field under the hero's feet and was the largest flat shape on the screen. A
            // soft ink ellipse grounds the figure the way the HOME court's shadows do, and leaves
            // the hero as the one big thing on the left.
            var circle = HubScenery.ChalkRing(stage, "ChalkCircle", 0.2f);
            HubKit.Place(circle.rectTransform, HubKit.Bottom, new Vector2(0, 40), new Vector2(560, 92));
            var floor = HubKit.Shape(stage, "Floor", new Color(HubStyle.Ink.r, HubStyle.Ink.g, HubStyle.Ink.b, 0.55f), false, 701, 0, 23);
            HubKit.Place(floor.rectTransform, HubKit.Bottom, new Vector2(0, 62), new Vector2(420, 46));
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

            // ⚠️ THE CLOCK IS A NUMBER ON A PLATE WITH ITS JOB BESIDE IT. Valorant draws a big bare
            // number and Overwatch writes "ASSEMBLE YOUR TEAM: 19" (both inspected 2026-09-23,
            // `docs/reports/ui-hud-review-2026-09-23/research.md` finding 4), so the number stays a
            // number; an earlier plan's draining ring had no precedent in either. What it lacked was
            // a job: a bare "28" floating over the court could be a score. The plate and the PICK
            // caption say it is the time left to choose, and it still turns persimmon under six.
            var clockPlate = HubKit.Place(HubKit.Rect(Root, "ClockPlate"), HubKit.Top, new Vector2(0, -HubKit.Margin), new Vector2(260, 100));
            HubKit.Stretch(HubKit.Shape(clockPlate, "Plate", HubStyle.Night, false, 702, 4, 24).rectTransform);
            var clockCaption = HubKit.Text(clockPlate, "ClockCaption", "PICK", HubStyle.Floor, false, HubStyle.HoneySoft, TextAnchor.MiddleLeft);
            HubKit.Place(clockCaption.rectTransform, HubKit.Left, new Vector2(24, 0), new Vector2(90, 60));
            _clock = HubKit.Text(clockPlate, "Clock", "", HubStyle.Display, true, HubStyle.Honey, TextAnchor.MiddleRight);
            HubKit.Place(_clock.rectTransform, HubKit.Right, new Vector2(-24, 0), new Vector2(140, 100));
            clockPlate.gameObject.SetActive(Timed);

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
                    HubKit.SetFill(cell, HubStyle.Night);
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
                // A role kit shows four powers (signature, attacking, defending, ultimate); the
                // tiles narrow to fit the same 380-unit panel rather than the panel growing.
                _shownSlots = _shownKit.ScreenSlots;
                _shownAbilities = System.Array.ConvertAll(_shownSlots, slot => slot.Ability);
                int count = _shownAbilities.Length;
                float size = count == 4 ? 80 : 100, step = count == 4 ? 88 : 120;
                for (int i = 0; i < _abilityButtons.Length; i++)
                {
                    bool on = i < count;
                    _abilityButtons[i].gameObject.SetActive(on);
                    if (!on) continue;
                    HubKit.Place((RectTransform)_abilityButtons[i].transform, HubKit.TopLeft, new Vector2(20 + i * step, -22), new Vector2(size, size));
                    _abilitySymbols[i].Glyph = _shownAbilities[i].Glyph;
                }
                InspectAbility(Mathf.Min(_inspectedAbility, count - 1));
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
            _abilityButtons = new HubButton[4];
            _abilitySymbols = new TumpAbilitySymbol[4];
            for (int i = 0; i < 4; i++)
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
            // ⚠️ NAME, THEN META, THEN SENTENCE, STACKED BY MEASURED HEIGHT (`StackAbilityText`).
            // The three sat in fixed boxes sized for the worst case, which left a 70-unit dead gap
            // under every one-line name and drew the ability's name lighter than its own metadata.
            // The name is now the panel's one Title-size line.
            _abilityName = HubKit.Text(panel, "AbilityName", "", HubStyle.Title, true, HubStyle.Honey, TextAnchor.UpperLeft);
            HubKit.Place(_abilityName.rectTransform, HubKit.TopLeft, new Vector2(20, -145), new Vector2(340, 108));
            _abilityMeta = HubKit.Text(panel, "AbilityMeta", "", HubStyle.Floor, false, HubStyle.Golden, TextAnchor.UpperLeft);
            HubKit.Place(_abilityMeta.rectTransform, HubKit.TopLeft, new Vector2(20, -264), new Vector2(340, 92));
            _abilitySummary = HubKit.Text(panel, "AbilitySummary", "", HubStyle.Body, false, HubStyle.Honey, TextAnchor.UpperLeft);
            HubKit.Place(_abilitySummary.rectTransform, HubKit.TopLeft, new Vector2(20, -366), new Vector2(340, 220));
        }

        private void InspectAbility(int slot)
        {
            if (_shownAbilities == null) return;
            _inspectedAbility = Mathf.Clamp(slot, 0, _shownAbilities.Length - 1);
            var ability = _shownAbilities[_inspectedAbility];
            var shown = _shownSlots[_inspectedAbility];
            AbilityVariant variant = null;
            if (shown.LoadoutSlot > 0)
            {
                var settings = Settings.SettingsStore.Current;
                var build = HeroBuildRules.RowFor(settings.HeroBuilds, _shownHero);
                variant = HeroBuildRules.Equipped(build, _shownHero, shown.LoadoutSlot, settings.AbilityChallenges);
            }
            bool alternate = variant != null && !variant.IsDefault;
            _abilityName.text = (alternate ? variant.Name : ability.Name).ToUpperInvariant();
            _abilitySummary.text = alternate ? variant.Description : ability.Summary;
            string resource = shown.IsUltimate ? _shownKit.UltimateCost.ToString("0") + " CHARGE"
                : ability.UsesCharges ? ability.MaxCharges + (ability.MaxCharges == 1 ? " USE" : " USES")
                : ability.Cooldown.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " s cooldown";
            // A role kit says which role the power belongs to before what kind of power it is.
            string role = _shownKit.HasRoleAbilities && !shown.IsUltimate ? shown.Label + " · " : "";
            _abilityMeta.text = role + AbilityIcons.LabelFor(ability.Glyph) + "\n" + resource;
            StackAbilityText();
            for (int i = 0; i < _abilityButtons.Length; i++)
                HubKit.SetFill(_abilityButtons[i], i == _inspectedAbility ? HubStyle.Persimmon
                    : i < _shownSlots.Length && _shownSlots[i].IsUltimate ? HubStyle.Golden : HubStyle.Honey);
        }

        /// <summary>Lay the name, the meta lines and the sentence one under another at their real
        /// heights, so a one-line name does not leave a hole the size of a two-line one.</summary>
        private void StackAbilityText()
        {
            const float left = 20, width = 340, gap = 14, panelHeight = 610;
            _abilityName.fontSize = HubStyle.Size(HubStyle.Title);
            float y = 142;
            foreach (var text in new[] { _abilityName, _abilityMeta, _abilitySummary })
            {
                text.rectTransform.sizeDelta = new Vector2(width, text.rectTransform.sizeDelta.y);
                float height = Mathf.Ceil(text.preferredHeight) + 4;
                if (text == _abilitySummary) height = Mathf.Max(height, panelHeight - y - 20);
                HubKit.Place(text.rectTransform, HubKit.TopLeft, new Vector2(left, -y), new Vector2(width, height));
                y += height + gap;
            }
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
            // ⚠️ RE-STACK WHEN THE NAME OUTGROWS ITS BOX. The stack is measured once when an ability
            // is inspected, so turning Larger text on while this screen is open wrapped a one-line
            // name onto two lines inside a one-line box: `MatchArrivalFlowTests` measured 111 units
            // of name in 52 (red since 2026-09-23 23:53, before this date's scenery pass).
            if (_abilityName != null && _abilityName.preferredHeight > _abilityName.rectTransform.rect.height + 2)
                StackAbilityText();
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
                var plate = HubKit.Shape(card, "Plate", seat.Mine ? HubStyle.Persimmon : HubStyle.Night, false, 740 + i, 4, 18);
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
