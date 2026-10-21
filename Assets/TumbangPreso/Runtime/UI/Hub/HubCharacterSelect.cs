using System.Collections.Generic;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// CHARACTER SELECT, for everyone after MATCH FOUND: who you are on the left, the hero standing
    /// in the chalk circle in the middle, the faces and SELECT on the right, the four seats along
    /// the bottom. The owner's reference is Valorant's agent select.
    ///
    /// ⚠️⚠️ THE PICK RULES ARE TODAY'S, UNCHANGED. A pick is `SelectLobbyPickServerRpc`, exactly what
    /// the old picker sent, so duplicates are allowed as they are now and a Classic pick is a cosmetic
    /// with neutral stats (`VISION.md` § 1). SELECT locks in with the lobby's own ready message
    /// (`DeclareReadyServerRpc`), whose tally every peer already receives, and the HOST starts when
    /// every human is locked or the clock runs out. No new message, no protocol bump.
    ///
    /// ⚠️⚠️ THE LAYOUT, AND WHY (2026-10-19 rework, `docs/reports/character-select-2026-10-19/`).
    /// The 2026-09 screen put the hero in the left 46 per cent, a dark ability box in the middle
    /// and the grid on the right, so the one big thing shared the screen with a box of the same
    /// weight and stood over a chalk ring drawn for somebody else's feet. Every reference read
    /// for this pass (Valorant, Overwatch, Apex, Marvel Rivals, League) does the opposite: the
    /// pick stands in the MIDDLE, what it does is one column of text beside it with no box, and
    /// the commit button sits directly under the choices. Read left to right it is one sentence:
    /// who this is and what they do, here they are, pick someone else or lock them in.
    ///
    /// THE FOUR ANSWERS:
    ///   the one thing   the hero you are choosing, standing in the chalk circle, centre
    ///   first press     a face in the grid (it picks at once, like the reference); SELECT locks in
    ///   not needed now  the loadout, which is its own screen from HOME
    ///   out             none while timed: a found match is not left from here. Untimed (from a
    ///                   custom lobby) BACK returns to the lobby with the pick kept.
    ///
    /// ⚠️ OUR OWN DNA, NOT A COPY. The stage is the street at night and the hero steps into the
    /// can's chalk circle; locking in puts your tsinelas down on your seat card, the way a player
    /// leaves a slipper on the line to say they are in; and your card says when you are TAYA,
    /// which `MatchRules.DefenderSlotFor` already decides from your seat before the match starts.
    /// </summary>
    public sealed class HubCharacterSelect : HubScreen
    {
        public override bool WearsBoard => true;
        public override float CourtShade => 0.78f;
        public override bool ShowsQueuePlate => false;

        /// <summary>A queue match: the clock runs and SELECT locks in. False from a custom lobby.</summary>
        public bool Timed;
        public const float Seconds = 30.0f;

        /// <summary>The left column's width: the name, the kit row and the ability's sentence.</summary>
        private const float ColumnWidth = 640;

        /// <summary>
        /// The role line's box, wider than the column. ⚠️ "//  THE FOREST OF MOUNT MAKILING" is about
        /// 670 units at Label and 770 with Larger text, and in a 640-unit box it wrapped to 104
        /// units inside 59 (`MatchArrivalFlowTests`, Paete, 960x540). Nothing stands to its right
        /// at the top of the screen until the clock, which only a timed screen draws, at the middle.
        /// </summary>
        private const float RoleWidth = ColumnWidth + 200;

        /// <summary>One grid cell and the gap between cells, in canvas units.</summary>
        private const float Cell = 160, Gap = 16;

        /// <summary>The seat strip's height, and the band the stage leaves above it for the chalked
        /// TAYA tag over your own card.</summary>
        private const float SeatHeight = 130, SeatGap = 58;

        private float _endsAt;
        private bool _locked, _started;
        private ModelPreview _preview;
        private RectTransform _stage, _light;
        private HubChalk _ring;
        private HubGlow _shadow;
        private Text _name, _role, _tagline, _clock, _hint;
        private HubButton _select;
        private readonly List<HubButton> _cells = new List<HubButton>();
        private readonly List<Image> _portraitFaces = new List<Image>();
        private readonly List<Color> _portraitColors = new List<Color>();
        private RectTransform _seats;
        private int _pick;
        private string _shownSeats = "";
        private HubButton[] _abilityButtons;
        private TumpAbilitySymbol[] _abilitySymbols;
        private Text _abilityName, _abilityMeta, _abilityCost, _abilitySummary;
        private RectTransform _abilityColumn;
        private HeroKit _shownKit;
        private HeroAbility[] _shownAbilities;
        private HeroKit.ScreenSlot[] _shownSlots;
        private string _shownHero;
        private string _framedHero;
        private int _inspectedAbility;

        private GameMode Mode => SceneFlow.SelectedMode;
        private IReadOnlyList<RosterEntry> People => Roster.GetPeople(Mode);

        /// <summary>A pad or keyboard starts ON the hero already picked, so the one ringed tile and
        /// the one persimmon tile are the same tile (the first capture ringed Dante under NEMU).</summary>
        public override Selectable FirstFocus => _pick >= 0 && _pick < _cells.Count ? _cells[_pick] : null;

        public override void Build()
        {
            // ⚠️ THE COURT AT NIGHT (`HubScenery`, 2026-09-24): the last screen before the match is
            // the street itself after dark, one warm light on the chalk circle the picked hero
            // steps into. It is the only screen on the road at night,
            // so the step from here into the match reads as walking onto the court.
            HubScenery.NightCourtGround(Root, 51, new Vector2(0.5f, 0.3f), new Vector2(1100, 600));
            _light = Root.Find("Light") as RectTransform;
            _pick = Mathf.Clamp(Settings.SettingsStore.Current.CharacterPick, 0, People.Count - 1);

            BuildStage();
            BuildIdentity();
            BuildClock();
            var grid = BuildGrid();

            _select = HubKit.Button(Root, "SelectButton", "SELECT", HubStyle.Chartreuse, Select, HubStyle.Display, 730);
            HubKit.Place((RectTransform)_select.transform, HubKit.BottomRight, new Vector2(-HubKit.Margin, HubKit.Margin), new Vector2(grid.sizeDelta.x, SeatHeight));
            _select.Shape.BandFraction = 0.12f;

            // The hint answers the button, so it sits on the grid it is about, right-aligned.
            _hint = HubKit.Text(Root, "Hint", "", HubStyle.Floor, false, HubStyle.HoneySoft, TextAnchor.LowerRight);
            HubKit.Place(_hint.rectTransform, HubKit.BottomRight,
                new Vector2(-HubKit.Margin, HubKit.Margin + SeatHeight + 24 + grid.sizeDelta.y + 12), new Vector2(grid.sizeDelta.x, 76));

            if (Mode == GameMode.HeroStrike) BuildAbilityReadout();

            // Everyone in the match along the bottom left, with their pick and whether they locked.
            _seats = HubKit.Place(HubKit.Rect(Root, "Seats"), HubKit.BottomLeft, new Vector2(HubKit.Margin, HubKit.Margin), new Vector2(4 * 300, SeatHeight));

            if (!Timed)
            {
                HubChrome.Back(Root, Hub);
                HubKit.Place(_role.rectTransform, HubKit.TopLeft, new Vector2(HubKit.Margin + HubChrome.BarHeight + 30, -HubKit.Margin), new Vector2(RoleWidth, 56));
                HubKit.Place(_name.rectTransform, HubKit.TopLeft, new Vector2(HubKit.Margin + HubChrome.BarHeight + 24, -HubKit.Margin - 44), new Vector2(ColumnWidth - HubChrome.BarHeight - 24, 150));
            }

            _endsAt = Time.unscaledTime + Seconds;
            Show();
            HubSlap.On(grid, 0.04f, 2);
            HubSlap.On(_select.transform, 0.1f, -2);
            if (_abilityColumn != null) HubSlap.On(_abilityColumn, 0.06f, -1);
        }

        /// <summary>
        /// The middle of the screen: the live model, its contact shadow and the chalk circle.
        ///
        /// ⚠️⚠️ THE RING AND THE SHADOW ARE PLACED WHERE THE FEET ARE DRAWN, EVERY FRAME, NOT AT A
        /// FIXED OFFSET. The 2026-09 screen placed both 40 units off the stage's bottom edge, which
        /// is only under the feet of a hero of the reference height at one window shape: Nemu stood
        /// a head above her own ring at every shape (`CharSelect-v1-*.png`). `StandOnFloor` puts
        /// every hero on one floor line, and `TryGroundViewport` reports where that floor is drawn.
        ///
        /// ⚠️ A SOFT INK POOL, NOT A PILL. The shadow used to be a 420 by 46 rounded sticker at 55
        /// per cent ink, which read as an empty text field inside the ring.
        /// </summary>
        private void BuildStage()
        {
            float right = HubKit.Margin + 3 * Cell + 2 * Gap + 24;
            _stage = HubKit.Span(HubKit.Rect(Root, "Stage"), Vector2.zero, Vector2.one,
                                 new Vector2(HubKit.Margin + ColumnWidth - 80, HubKit.Margin + SeatHeight + SeatGap),
                                 new Vector2(right, HubKit.Margin + 110));
            _shadow = HubKit.Rect(_stage, "ContactShadow").gameObject.AddComponent<HubGlow>();
            _shadow.color = new Color(HubStyle.Ink.r, HubStyle.Ink.g, HubStyle.Ink.b, 0.72f);
            _shadow.raycastTarget = false;
            _ring = HubScenery.ChalkRing(_stage, "ChalkCircle", 0.2f);
            _ring.Width = 7;
            foreach (var mark in new Graphic[] { _shadow, _ring })
                mark.rectTransform.anchorMin = mark.rectTransform.anchorMax = mark.rectTransform.pivot = Vector2.zero;
            PlaceFloor(new Vector2(0.5f, 0.18f));

            var model = HubKit.Stretch(HubKit.Rect(_stage, "Model"));
            _preview = model.gameObject.AddComponent<ModelPreview>();
            _preview.Attach(model);
            _preview.CentreSubject();
            _preview.StandOnFloor();
        }

        /// <summary>Put the ring, the shadow and the street light's pool at <paramref name="viewport"/>
        /// of the stage, sized from the stage's height so they scale with the hero.</summary>
        private void PlaceFloor(Vector2 viewport)
        {
            var size = _stage.rect.size;
            if (size.y < 1) return;
            float width = Mathf.Clamp(size.y * 0.55f, 260, 600), height = width * 0.3f;
            // The floor point is under the body's middle and the toes point at the camera, so the
            // ring is drawn forward of it: the feet stand inside the circle, not on its front edge.
            // Measured: Dante's toes are drawn 0.06 of the stage below his floor point and Nemu's
            // 0.04 (`CharSelect-v4-*.png`), and the earlier, smaller ring had Dante on the line.
            var at = new Vector2(size.x * viewport.x, size.y * viewport.y - height * 0.3f);
            Set(_ring.rectTransform, at - new Vector2(width, height) * 0.5f, new Vector2(width, height));
            Set(_shadow.rectTransform, at - new Vector2(width * 0.36f, height * 0.42f), new Vector2(width * 0.72f, height * 0.84f));
            if (_light != null)
            {
                // The street light's pool follows the circle across the root, not the other way round.
                Vector3 world = _stage.TransformPoint((Vector3)(at + _stage.rect.min));
                Vector2 local = Root.InverseTransformPoint(world);
                var anchor = new Vector2((local.x - Root.rect.xMin) / Mathf.Max(1, Root.rect.width),
                                         (local.y - Root.rect.yMin + size.y * 0.16f) / Mathf.Max(1, Root.rect.height));
                if ((_light.anchorMin - anchor).sqrMagnitude > 1e-6f) _light.anchorMin = _light.anchorMax = anchor;
            }
        }

        private static void Set(RectTransform rect, Vector2 position, Vector2 size)
        {
            if ((rect.anchoredPosition - position).sqrMagnitude > 0.25f) rect.anchoredPosition = position;
            if ((rect.sizeDelta - size).sqrMagnitude > 0.25f) rect.sizeDelta = size;
        }

        private void BuildIdentity()
        {
            _role = HubKit.Text(Root, "Role", "", HubStyle.Label, false, HubStyle.Golden, TextAnchor.MiddleLeft);
            HubKit.Place(_role.rectTransform, HubKit.TopLeft, new Vector2(HubKit.Margin + 6, -HubKit.Margin), new Vector2(RoleWidth, 56));
            _name = HubKit.Text(Root, "Heading", "", HubStyle.Hero, true, HubStyle.Honey, TextAnchor.MiddleLeft);
            HubKit.Place(_name.rectTransform, HubKit.TopLeft, new Vector2(HubKit.Margin, -HubKit.Margin - 44), new Vector2(ColumnWidth, 150));
            var shadow = _name.gameObject.AddComponent<Shadow>();
            shadow.effectColor = HubStyle.Ink; shadow.effectDistance = new Vector2(6, -7);
            // Classic has no kit to show, so its column carries the person's one-line story instead.
            _tagline = HubKit.Text(Root, "Tagline", "", HubStyle.Body, false, HubStyle.HoneySoft, TextAnchor.UpperLeft);
            HubKit.Place(_tagline.rectTransform, HubKit.TopLeft, new Vector2(HubKit.Margin + 4, -(HubKit.Margin + 206)), new Vector2(ColumnWidth - 40, 160));
        }

        private void BuildClock()
        {
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
        }

        /// <summary>
        /// The faces, right side, sitting directly on SELECT.
        ///
        /// ⚠️ ONE CELL PER PERSON AND NO MORE. The grid was a fixed 3 by 4, so Hero Strike's nine
        /// heroes left three hatched cells that looked like locked heroes and were nothing at all; a
        /// door that leads nowhere is § 6.3's dead end. The grid is as tall as the roster, and a
        /// thirteenth person widens it to four columns rather than falling off the bottom.
        ///
        /// ⚠️ ANCHORED TO THE BOTTOM, ON THE BUTTON IT FEEDS. The choices and the commit are one
        /// block in every reference (Valorant's LOCK IN sits under its grid), so the eye goes
        /// face, then SELECT, without crossing the screen.
        /// </summary>
        private RectTransform BuildGrid()
        {
            int count = People.Count;
            int columns = count > 12 ? 4 : 3;
            int rows = Mathf.Max(1, Mathf.CeilToInt(count / (float)columns));
            float cell = columns == 3 ? Cell : (3 * Cell + 2 * Gap - 3 * Gap) / 4f;
            float width = columns * cell + (columns - 1) * Gap, height = rows * cell + (rows - 1) * Gap;
            var grid = HubKit.Place(HubKit.Rect(Root, "Grid"), HubKit.BottomRight,
                new Vector2(-HubKit.Margin, HubKit.Margin + SeatHeight + 24), new Vector2(width, height));
            for (int i = 0; i < count; i++)
            {
                int index = i;
                var tile = HubKit.Button(grid, "Portrait" + i, null, HubStyle.Honey, () => Pick(index), 0, 710 + i);
                HubKit.Place((RectTransform)tile.transform, HubKit.TopLeft, new Vector2((i % columns) * (cell + Gap), -(i / columns) * (cell + Gap)), new Vector2(cell, cell));
                var face = HubKit.Picture(tile.Body, "Face", HubKit.Portrait(People[i].Id));
                HubKit.Stretch(face.rectTransform, 10);
                _portraitFaces.Add(face);
                _portraitColors.Add(face.color);
                _cells.Add(tile);
            }
            return grid;
        }

        private bool Usable(int index)
        {
            var person = Roster.At(People, index);
            if (person == null) return false;
            // Every route, offline included (`EconomyRules.UnlockedHeroes`, owner 2026-10-20).
            return HubOwnership.OwnsHeroInRoom(person.Id);
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
            // The focus ring goes with the pick, so the ringed tile and the persimmon tile are one
            // tile however the pick arrived (a pad's A, a click, or a test invoking the button).
            var events = UnityEngine.EventSystems.EventSystem.current;
            if (events != null && index < _cells.Count) events.SetSelectedGameObject(_cells[index].gameObject);
        }

        private void Select()
        {
            if (!Usable(_pick)) { MenuSfx.Error(); Hub.Toast("This hero is locked. Pick a hero without stripes."); return; }
            Hub.Host.PublishPicks();
            if (!Timed) { Close(); return; }
            _locked = true;
            Hub.Host.LockIn();
            MenuSfx.Valid();
            // The circle is chalked again, gold: this spot on the court is taken.
            _ring.color = HubStyle.Golden;
            _ring.DrawIn(0);
            Show();
            var events = UnityEngine.EventSystems.EventSystem.current;
            if (events != null && _pick < _cells.Count) events.SetSelectedGameObject(_cells[_pick].gameObject);
        }

        private void Show()
        {
            var person = Roster.At(People, _pick);
            if (person == null) return;
            string tagline = ConvertedCharacterSelect.TaglineFor(person.Id) ?? "";
            string[] parts = tagline.Split('\n');
            bool hero = Mode == GameMode.HeroStrike;
            _role.text = hero && parts.Length > 1 ? "//  " + parts[0] : "//  CLASSIC";
            _role.fontSize = HubStyle.Size(HubStyle.Label);
            HubKit.Fit(_role, RoleWidth);
            _name.text = person.Name;
            _name.fontSize = HubStyle.Size(HubStyle.Hero);
            HubKit.Fit(_name, _name.rectTransform.sizeDelta.x);
            _tagline.gameObject.SetActive(!hero);
            _tagline.text = hero ? "" : parts.Length > 1 ? parts[1] : tagline;
            _tagline.fontSize = HubStyle.Size(HubStyle.Body);
            _tagline.rectTransform.sizeDelta = new Vector2(ColumnWidth - 40, Mathf.Ceil(_tagline.preferredHeight) + 4);

            var art = RosterBook.Load().PersonArt(_pick, Mode);
            if (art != null) _preview.Show(art.Model, art.Clips, art.Palette, art.PetModel);
            // ⚠️ ABOUT 12 PER CENT CLOSER THAN THE 2026-09 SCREEN (0.95, Paete 0.89), with Paete's
            // ratio to the cast kept, so `HubFlowTests`' measured shares keep their order. The hero
            // is the one thing on this screen and a standard hero was drawn at 40 per cent of a
            // 16:9 screen's height; the stage now has the middle of the screen to fill.
            // Framed once per hero: LOCK IN and a re-pick of the same face redraw the screen but
            // must not move the camera under the player's eyes.
            if (_framedHero != person.Id) _preview.SetTileFraming(person.Id == "paete" ? 0.79f : 0.84f);
            _framedHero = person.Id;
            if (_abilityButtons != null)
            {
                _shownHero = person.Id;
                _shownKit = HeroAbilitySystem.CreateKitFor(person.Id);
                // A role kit shows four powers (signature, attacking, defending, ultimate); the
                // tiles keep their size and the row grows, inside the column's width.
                _shownSlots = _shownKit.ScreenSlots;
                _shownAbilities = System.Array.ConvertAll(_shownSlots, slot => slot.Ability);
                int count = _shownAbilities.Length;
                for (int i = 0; i < _abilityButtons.Length; i++)
                {
                    bool on = i < count;
                    _abilityButtons[i].gameObject.SetActive(on);
                    if (on) _abilitySymbols[i].Glyph = _shownAbilities[i].Glyph;
                }
                InspectAbility(Mathf.Min(_inspectedAbility, count - 1));
            }

            for (int i = 0; i < _cells.Count; i++)
            {
                bool usable = Usable(i);
                bool picked = i == _pick;
                HubKit.SetFill(_cells[i], picked ? HubStyle.Persimmon : HubStyle.Honey);
                _cells[i].Hatched = !usable;
                var original = _portraitColors[i];
                _portraitFaces[i].color = usable ? original
                    : new Color(original.r * 0.62f, original.g * 0.62f, original.b * 0.62f, original.a);
                // ⚠️ LOCKED IN, THE OTHER FACES STEP BACK AND YOURS STAYS LIT. Disabling every tile
                // hatched the whole grid, the picked one included, so the screen read as broken at
                // the one moment it should read as settled (`CharSelect-v1-locked-1600x680.png`).
                // The picked tile stays enabled and does nothing (`Pick` returns while locked).
                _cells[i].interactable = !_locked || picked;
                HubKit.Ensure<CanvasGroup>(_cells[i].gameObject).alpha = _locked && !picked ? 0.42f : 1.0f;
            }

            bool can = Usable(_pick);
            HubKit.SetLabel(_select, _locked ? "LOCKED IN" : Timed ? "SELECT" : "DONE");
            HubKit.LabelOf(_select).fontSize = HubStyle.Size(HubStyle.Display);
            _select.interactable = !_locked;
            HubKit.SetFill(_select, can ? HubStyle.Chartreuse : HubStyle.Honey);
            _hint.text = !can ? "This hero is locked."
                       : _locked ? "Waiting for everyone to lock in." : "";
            _shownSeats = "";
        }

        /// <summary>
        /// The hero's kit under the name: a row of ability tiles, then the inspected one's name,
        /// what kind of power it is, how it is cast and paid for, and its sentence.
        ///
        /// ⚠️ NO PLATE. The kit used to be a 380 by 610 dark box between the hero and the grid, as
        /// heavy as the hero and mostly empty below a three-line sentence. Valorant and Apex both
        /// set this text straight on the dark scene beside the pick; honey on the night road already
        /// clears every contrast floor.
        ///
        /// VISION 3's Learn layer stays on the timed selector. A popup would make TumpHub stop
        /// ticking this screen and could suspend the host's deadline.
        /// </summary>
        private void BuildAbilityReadout()
        {
            _abilityColumn = HubKit.Place(HubKit.Rect(Root, "SelectionAbilities"), HubKit.TopLeft,
                new Vector2(HubKit.Margin, -(HubKit.Margin + 214)), new Vector2(ColumnWidth, 620));
            _abilityButtons = new HubButton[4];
            _abilitySymbols = new TumpAbilitySymbol[4];
            for (int i = 0; i < 4; i++)
            {
                int slot = i;
                var button = HubKit.Button(_abilityColumn, "SelectionAbility" + i, null, HubStyle.Honey,
                    () => InspectAbility(slot), 0, 746 + i);
                HubKit.Place((RectTransform)button.transform, HubKit.TopLeft, new Vector2(4 + i * 120, 0), new Vector2(104, 104));
                var glyph = HubKit.Rect(button.Body, "Glyph").gameObject.AddComponent<TumpAbilitySymbol>();
                HubKit.Stretch(glyph.rectTransform, 12);
                glyph.color = HubStyle.Ink; glyph.raycastTarget = false;
                _abilityButtons[i] = button; _abilitySymbols[i] = glyph;
            }
            // ⚠️ NAME, THEN WHAT IT IS, THEN HOW IT IS CAST, THEN THE SENTENCE, STACKED BY MEASURED
            // HEIGHT (`StackAbilityText`). The kind and the cost are now two one-line labels: as one
            // wrapping label, "ATTACKING · PROJECTILE" plus "35 s cooldown" was the text that drew
            // over the sentence in the owner's screenshot (`CrispUiText` has the cause).
            _abilityName = HubKit.Text(_abilityColumn, "AbilityName", "", HubStyle.Title, true, HubStyle.Honey, TextAnchor.UpperLeft);
            _abilityMeta = HubKit.Text(_abilityColumn, "AbilityMeta", "", HubStyle.Floor, false, HubStyle.Golden, TextAnchor.UpperLeft);
            _abilityCost = HubKit.Text(_abilityColumn, "AbilityCost", "", HubStyle.Floor, false, HubStyle.HoneySoft, TextAnchor.UpperLeft);
            _abilitySummary = HubKit.Text(_abilityColumn, "AbilitySummary", "", HubStyle.Body, false, HubStyle.Honey, TextAnchor.UpperLeft);
        }

        private void InspectAbility(int slot)
        {
            if (_shownAbilities == null) return;
            _inspectedAbility = Mathf.Clamp(slot, 0, _shownAbilities.Length - 1);
            var ability = _shownAbilities[_inspectedAbility];
            var shown = _shownSlots[_inspectedAbility];
            AbilityVariant variant = null;
            if (HeroLoadoutRules.SidegradesOpen && shown.LoadoutSlot > 0)
            {
                var settings = Settings.SettingsStore.Current;
                var build = HeroBuildRules.RowFor(settings.HeroBuilds, _shownHero);
                variant = HeroBuildRules.Equipped(build, _shownHero, shown.LoadoutSlot, settings.AbilityChallenges);
            }
            bool alternate = variant != null && !variant.IsDefault;
            _abilityName.text = (alternate ? variant.Name : ability.Name).ToUpperInvariant();
            _abilitySummary.text = alternate ? variant.Description : ability.Summary;
            // A role kit says which role the power belongs to before what kind of power it is.
            string role = _shownKit.HasRoleAbilities && !shown.IsUltimate ? shown.Label + "  ·  " : "";
            _abilityMeta.text = role + AbilityIcons.LabelFor(ability.Glyph);
            string resource = shown.IsUltimate ? _shownKit.UltimateCost.ToString("0") + " charge"
                : ability.UsesCharges ? ability.MaxCharges + (ability.MaxCharges == 1 ? " use" : " uses")
                : ability.Cooldown.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " s cooldown";
            // ⚠️ THE KEY IS THE LIVE BINDING FOR THE DEVICE IN HAND (`Hud.KeyLabelFor`), never a
            // literal, and a phone is told no key at all (`docs/VISION.md` § 3).
            string key = InputLayer.LastInputDevice.Current == InputLayer.InputDeviceKind.Touch ? "" : Hud.KeyLabelFor(shown.Action);
            _abilityCost.text = string.IsNullOrEmpty(key) ? resource : "Press " + key + "  ·  " + resource;
            StackAbilityText();
            for (int i = 0; i < _abilityButtons.Length; i++)
                HubKit.SetFill(_abilityButtons[i], i == _inspectedAbility ? HubStyle.Persimmon
                    : i < _shownSlots.Length && _shownSlots[i].IsUltimate ? HubStyle.Golden : HubStyle.Honey);
        }

        /// <summary>Lay the name, the two meta lines and the sentence one under another at their real
        /// heights, so a one-line name does not leave a hole the size of a two-line one.</summary>
        private void StackAbilityText()
        {
            const float width = ColumnWidth - 40;
            _abilityName.fontSize = HubStyle.Size(HubStyle.Title);
            _abilityMeta.fontSize = _abilityCost.fontSize = HubStyle.Size(HubStyle.Floor);
            _abilitySummary.fontSize = HubStyle.Size(HubStyle.Body);
            float y = 104 + 26;
            foreach (var text in new[] { _abilityName, _abilityMeta, _abilityCost, _abilitySummary })
            {
                text.rectTransform.sizeDelta = new Vector2(width, text.rectTransform.sizeDelta.y);
                float height = Mathf.Ceil(text.preferredHeight) + 4;
                HubKit.Place(text.rectTransform, HubKit.TopLeft, new Vector2(4, -y), new Vector2(width, height));
                y += height + (text == _abilityName ? 6 : text == _abilityCost ? 18 : 4);
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
            if (_preview.TryGroundViewport(out var floor)) PlaceFloor(floor);
            // ⚠️ RE-STACK WHEN ANY LINE OUTGROWS ITS BOX. The stack is measured when an ability is
            // inspected, so turning Larger text on while this screen is open wrapped a one-line
            // name onto two lines inside a one-line box: `MatchArrivalFlowTests` measured 111 units
            // of name in 52 (red since 2026-09-23 23:53, before this date's scenery pass).
            if (_abilityName != null)
                foreach (var text in new[] { _abilityName, _abilityMeta, _abilityCost, _abilitySummary })
                    if (text.preferredHeight > text.rectTransform.rect.height + 2) { StackAbilityText(); break; }
            if (!Timed) return;

            float left = Net.MatchRpc.Instance != null && Net.MatchRpc.Instance.CharacterSelecting
                ? Net.MatchRpc.Instance.CharacterSelectSecondsLeft : Mathf.Max(0, _endsAt - Time.unscaledTime);
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
                // Completion belongs to this locked selection screen. Repeated START GAME
                // clicks in the lobby can only open the stage, never skip its lock-in.
                if (Net.MatchRpc.Instance != null && Net.MatchRpc.Instance.CharacterSelecting)
                    Net.MatchRpc.Instance.HostCompleteCharacterSelection();
                else Hub.Host.StartGame();
            }
        }

        /// <summary>The first round this seat is the taya in, or 0 when the match is too short.</summary>
        private static int FirstTayaRound(int slot)
        {
            int rounds = SceneFlow.SelectedRules != null ? SceneFlow.SelectedRules.Rounds : MatchRules.RoundCountFor(SceneFlow.SelectedMode);
            for (int round = 1; round <= rounds; round++)
                if (MatchRules.IsDefenderSlot(slot, round)) return round;
            return 0;
        }

        /// <summary>
        /// The four seats, as Valorant's team column shows them: whose seat, the face they are on,
        /// and whether they have committed to it.
        ///
        /// ⚠️ A TEAMMATE'S FACE STILL BEING CHOSEN IS DRAWN FAINT AND A LOCKED ONE SOLID, and your
        /// own card gets your tsinelas when you lock: the state reads without reading the word. A bot's hero is
        /// decided when the match installs (`MatchInstaller.AiCharacterIndex`), so a bot seat shows
        /// the bot mark rather than a face it might not play. An empty seat with no bot reads OPEN;
        /// it used to read BOT, which is what a host who left that seat empty did not choose.
        /// </summary>
        private void DrawSeats()
        {
            var seats = Hub.Host.Seats();
            string key = _locked + ":" + Timed + "|";
            foreach (var s in seats) key += s.Occupied + ":" + s.Bot + ":" + s.CharacterPick + ":" + s.Ready + ":" + s.Name + "|";
            if (key == _shownSeats) return;
            bool first = _shownSeats.Length == 0 && _seats.childCount == 0;
            _shownSeats = key;

            for (int i = _seats.childCount - 1; i >= 0; i--) Destroy(_seats.GetChild(i).gameObject);
            for (int i = 0; i < seats.Length; i++)
            {
                var seat = seats[i];
                bool open = !seat.Occupied && !seat.Bot;
                // ⚠️ ON A TIMED SCREEN YOUR OWN LOCK IS THIS SCREEN'S, NOT THE LOBBY'S READY FLAG. The
                // flag outlives the screen that set it, so a second pick screen in one session
                // opened with your card reading LOCKED beside a SELECT you had not pressed
                // (`CharSelect-v7-larger-1280x720.png`). `Tick`'s start test already reads `_locked`.
                bool locked = seat.Mine && Timed ? _locked : seat.Ready;
                var card = HubKit.Place(HubKit.Rect(_seats, "Seat" + i), HubKit.BottomLeft, new Vector2(i * 300, 0), new Vector2(284, SeatHeight));
                var plate = HubKit.Shape(card, "Plate", seat.Mine ? HubStyle.Persimmon : HubStyle.Night, false, 740 + i, 4, 18);
                HubKit.Stretch(plate.rectTransform);
                if (open) HubKit.Ensure<CanvasGroup>(card.gameObject).alpha = 0.55f;
                var person = seat.Occupied ? Roster.At(People, seat.CharacterPick) : null;
                var face = HubKit.Picture(card, "Face", HubKit.Portrait(person?.Id));
                HubKit.Place(face.rectTransform, HubKit.Left, new Vector2(6, 4), new Vector2(110, 110));
                // Your own face stays whole on your persimmon card; at half strength it read as a
                // smudge there. Everyone else's pick is faint until they lock it.
                if (face.sprite != null && Timed && !locked && !seat.Mine) face.color = new Color(1, 1, 1, 0.5f);
                if (face.sprite == null)
                {
                    var mark = HubKit.Glyph(card, "Bot", seat.Occupied ? HubGlyph.Mark.Person : seat.Bot ? HubGlyph.Mark.Bots : HubGlyph.Mark.Plus,
                                            seat.Mine ? HubStyle.Ink : HubStyle.HoneySoft);
                    HubKit.Place(mark.rectTransform, HubKit.Left, new Vector2(24, 0), new Vector2(72, 72));
                }
                Color ink = seat.Mine ? HubStyle.Ink : HubStyle.Honey;
                string title = seat.Mine ? "YOU" : seat.Occupied ? seat.Name : seat.Bot ? "BOT" : "OPEN";
                string state = seat.Bot ? (Timed ? "READY" : "")
                             : !seat.Occupied ? ""
                             : Timed ? (locked ? "LOCKED" : "PICKING")
                             : seat.Ready ? "READY" : "";
                var name = HubKit.Text(card, "Name", title, HubStyle.Floor, true, ink, TextAnchor.MiddleLeft);
                Text stateText = null;
                if (state.Length > 0) stateText = HubKit.Text(card, "State", state, HubStyle.Floor, false, ink, TextAnchor.MiddleLeft);
                // Stacked by measured height and centred on the card, so Larger text cannot push the
                // second line out of a box sized for the first one.
                float total = 0;
                foreach (var line in new[] { name, stateText })
                {
                    if (line == null) continue;
                    line.rectTransform.sizeDelta = new Vector2(150, 40);
                    HubKit.Fit(line, 150);
                    total += Mathf.Ceil(line.preferredHeight) + 2;
                }
                float y = (SeatHeight - total) * 0.5f;
                foreach (var line in new[] { name, stateText })
                {
                    if (line == null) continue;
                    float height = Mathf.Ceil(line.preferredHeight) + 2;
                    HubKit.Place(line.rectTransform, HubKit.TopLeft, new Vector2(122, -y), new Vector2(150, height));
                    y += height;
                }
                int taya = seat.Mine ? FirstTayaRound(seat.Slot) : 0;
                if (taya > 0)
                {
                    // ⚠️ THE GAME'S OWN WORD AND ITS OWN RULE, CHALKED ON THE ROAD OVER YOUR SEAT:
                    // seat N is the taya in round N + 1 (`MatchRules.DefenderSlotFor`), fixed before
                    // the match, and a role kit's defending power is the one you need first if this
                    // says round 1. It is outside the card because "TAYA IN ROUND 2" is about 240
                    // units at the 28-unit floor and a card's text column is 150.
                    var tag = HubKit.Text(_seats, "Taya", "TAYA IN ROUND " + taya, HubStyle.Floor, true, HubScenery.Chalk, TextAnchor.LowerLeft);
                    HubKit.Place(tag.rectTransform, HubKit.BottomLeft, new Vector2(i * 300 + 10, SeatHeight + 8), new Vector2(274, 44));
                    HubKit.Fit(tag, 274);
                }
                var slipper = seat.Mine && locked && Timed ? Resources.Load<Texture2D>("UI/brand/tsinelas_mark") : null;
                if (slipper != null)
                {
                    // ⚠️ YOUR TSINELAS, PUT DOWN ON THE LINE: THIS SEAT IS IN. It is the brand's own
                    // slipper (`Resources/UI/brand/tsinelas_mark.png`), drawn as delivered, and it
                    // is on YOUR card only: `BrandMarks.Mark` records that the tsinelas means "this
                    // one" and stops meaning anything the moment it repeats. A teammate's lock
                    // reads from their face turning solid and the word under their name. The first
                    // pass used the thin `HubGlyph` slipper, which read as a paperclip at 64 units.
                    // A RawImage, because the file imports as a texture and not a sprite (see
                    // `BrandMarks.Mark`'s remarks: a null sprite draws a white box).
                    // Over the portrait's outer corner, hanging off the card like a slapped-on
                    // sticker. Every other place collides with words: the portrait's inner corner
                    // covered the L of LOCKED (`CharSelect-v7-locked-1600x680.png`), the right edge
                    // meets LOCKED at Larger text, and above the card is the TAYA tag.
                    var holder = HubKit.Place(HubKit.Rect(card, "LockedSlipper"), HubKit.BottomLeft, new Vector2(-12, -10), new Vector2(54, 70));
                    holder.localRotation = Quaternion.Euler(0, 0, 14);
                    var stamp = HubKit.Stretch(HubKit.Rect(holder, "Slipper")).gameObject.AddComponent<RawImage>();
                    stamp.texture = slipper;
                    stamp.raycastTarget = false;
                    // The holder carries the tilt; `HubSlap` settles its own target back to upright.
                    HubSlap.On(stamp, 0, -18);
                }
                if (first) HubSlap.On(card, 0.05f + i * 0.04f, i % 2 == 0 ? 1.5f : -1.5f);
            }
        }
    }
}
