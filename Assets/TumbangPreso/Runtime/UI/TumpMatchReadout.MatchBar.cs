using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The match bar: four player chips around the clock (TODO VISUAL-1.4).
    ///
    /// ⚠️⚠️ WHY THE SCOREBOARD MOVED. Four 530 x 320 slabs held the top left for the whole
    /// match, most of the time reading 0 0 0 0, and the can's state was written twice in two
    /// corners while nothing on screen showed whose turn it was to defend. The bar puts the
    /// match in one glance at the top centre, the way Splatoon's player icons and Valorant's
    /// top bar do: who is here, who is the taya, who is leading, where the can is, how much
    /// time is left and whose round comes next. Permanent HUD in ordinary play dropped from
    /// roughly a sixth of the frame to under a twentieth.
    ///
    /// ⚠️ SEAT ORDER IS FIXED. The old rows re-sorted by score, so a knockdown moved four rows
    /// at once in the corner of the eye. Chips stay where they are and a crown marks the
    /// unique leader instead. `ScoreFeedbackTests` asserts the emphasis follows the seat.
    ///
    /// ⚠️ NAMES ARE THE NAMEPLATES' JOB IN PLAY. A chip carries the portrait, the seat tag and
    /// the score; the full name is drawn under it for spectators, who follow four people
    /// rather than their own hands. Object names (`MatchScores`, `ScoreRow{i}`,
    /// `PlayerName`, `Score`, `RoundClock`, `TimeLeft`, `RoundLabel`) are kept because the
    /// reading layout, the contrast pass and the HUD probes address groups by name.
    /// </summary>
    public sealed partial class TumpMatchReadout
    {
        private readonly HudCard[] _chipCards = new HudCard[4];
        private readonly HudCard[] _chipSwatches = new HudCard[4];
        private readonly HudBadge[] _roleBadges = new HudBadge[4];
        private readonly HudBadge[] _stateBadges = new HudBadge[4];
        private readonly HudBadge[] _crowns = new HudBadge[4];
        private readonly Text[] _seatTags = new Text[4];
        private HudBadge _canGlyph;
        private bool? _canShownUpright;
        private float _canFlipAt = -1;
        private const float CanFlipSeconds = .34f;
        private HudRing _canRing;
        private HudPips _pips;
        private RectTransform _roundTrack;
        private HudRing _staminaArc;
        private CanvasGroup _staminaGroup;
        private float _staminaAlpha;
        private RectTransform _staminaRoot, _drainPins;
        private float _drainPinsAge = -1.0f;
        private HudCard _promptPlate;
        /// <summary>The action prompt's words: ivory on the glass strip (they were brown on cream).</summary>
        private static readonly Color PromptInk = HudDraw.Ivory;

        /// <summary>Fits the prompt pill to the words it carries; hidden when there are none.</summary>
        private void SizePromptPlate()
        {
            if (_promptPlate == null || _prompt == null) return;
            bool show = _promptRoot.gameObject.activeInHierarchy && _prompt.enabled && !string.IsNullOrEmpty(_prompt.text);
            _promptPlate.enabled = show;
            bool glyph = _bindingGlyph != null && _bindingGlyph.enabled;
            bool progress = _progress != null && _progress.transform.parent.gameObject.activeSelf;
            // The owner's larger Xelu glyph (Feedback 2026-09-30) beside one line of words on a
            // cream toy tile; progress is an orange fill in a groove inside the same tile.
            const float band = 70, key = 58, slack = 12;
            float glyphWidth = glyph ? InputGlyphs.PromptWidth(_bindingGlyph.sprite, key) : 0;
            var mode = _prompt.horizontalOverflow; _prompt.horizontalOverflow = HorizontalWrapMode.Overflow;
            float measured = Mathf.Ceil(_prompt.preferredWidth) + slack; _prompt.horizontalOverflow = HorizontalWrapMode.Wrap;
            float lead = glyph ? 14 + glyphWidth + 14 : 28;
            float width = Mathf.Min(_statusPromptMaxWidth, Mathf.Max(220, lead + measured + 24));
            float left = (1100 - width) * .5f;
            OwnerUiLayout.Place(_promptPlate.rectTransform, left, 0, width, progress ? band + 16 : band);
            if (glyph) OwnerUiLayout.Place(_bindingGlyph.rectTransform, left + 14, (band - key) * .5f, glyphWidth, key);
            OwnerUiLayout.Place(_prompt.rectTransform, left + lead, 0, width - lead - 20, band);
            float textHeight = Mathf.Max(band, _prompt.preferredHeight + 12);
            _prompt.rectTransform.sizeDelta = new Vector2(_prompt.rectTransform.sizeDelta.x, textHeight);
            _promptPlate.rectTransform.sizeDelta = new Vector2(width, textHeight + (progress ? 16 : 0));
            if(_progress != null)
                OwnerUiLayout.Place((RectTransform)_progress.transform.parent, left + 18, textHeight + 2, width - 36, 8);
            OwnerUiLayout.Place(_context.rectTransform, left - 100, textHeight + (progress ? 30 : 14), width + 200, 66);
        }

        // ⚠️⚠️ IN-GAME UI OVERHAUL 2026-10-20: THE TEAM BAR, OVERWATCH'S SHAPE IN THE GAME'S COLOURS.
        // The request: "make it similar to overwatch's simple ingame hud". Overwatch 2 puts every
        // player's hero icon in one slim row at the top centre, split either side of the objective
        // and the clock, and draws nothing else up there. These chips replace the 250 x 68 cream
        // toy cards that sat on extruded sides with a "You" tile hanging under one of them:
        //
        // - A chip is one smoked-glass parallelogram (`HudCard.Glass`) holding the portrait on a
        //   leaning tile in the seat colour, the score as a large italic numeral, and the role's
        //   own object (the can or the slipper) at its right end, whole or faded. That is every
        //   fact the old card carried.
        // - YOU is a honey bar under your chip and your score in honey, the way Overwatch marks
        //   your own portrait, instead of a second tile with a word on it.
        // - The full name is the world nameplate's job in play (the earlier VISUAL-1.4 note says the
        //   same) and is drawn under each chip only while spectating, where four people are being
        //   followed rather than one pair of hands.
        //
        // ⚠️ THE SIZES ARE MEASURED AGAINST THE WIDEST SCORE THE CHIP MUST HOLD, NOT TASTE.
        // `LargeScoreChipsStayInsideTheBarAtNormalAndAccessibleSizes` stages "-888888" at the
        // 28-unit floor: seven DIN Bold glyphs at about 15.5 units each is 108, so the score box is
        // 112. The chip is 200 so a four-digit score at full size (about 90 units) ends before the
        // state glyph at 172 (at 190 the first renders drew "1120" through the slipper), and no
        // wider, because `HeroPowerDetailsAndSpectatorCleanFeedKeepTheirLiveContracts` holds the
        // permanent Hero Strike HUD under 8 per cent of the frame and a 210 chip measured 8.0006.
        // The bar is 1060 wide against the old 1306, whose HUD measured 10.8 on the same check.
        private const float ChipWidth = 200, ChipHeight = 58, ClockWidth = 200, ClockHeight = 58, BarTop = 22;
        private const float ScoreLeft = 70, ScoreWidth = 112;
        private const int ScoreFont = 40, ScoreFontFloor = 28;
        private static readonly float[] ChipX = { 0, 212, 648, 860 };
        private const float BarWidth = 1060;
        private readonly HudCard[] _portraitTiles = new HudCard[4];
        private readonly HudCard[] _localMarks = new HudCard[4], _roleChips = new HudCard[4];
        private readonly string[] _barNames = new string[4], _barNameLabels = new string[4];

        private void BuildMatchBar()
        {
            _scoreRoot = OwnerUiLayout.Rect(_root, "MatchScores");
            Pin(_scoreRoot, new Vector2(.5f, 1), new Vector2(0, -(BarTop + ChipHeight * .5f)), new Vector2(BarWidth, ChipHeight));
            for (int i = 0; i < 4; i++) BuildChip(i);

            // The clock is the bar's centre: the same glass, denser, because its numerals are the
            // one thing on the bar read at a glance under every background.
            _clockRoot = OwnerUiLayout.Rect(_root, "RoundClock");
            Pin(_clockRoot, new Vector2(.5f, 1), new Vector2(0, -(BarTop + 50)), new Vector2(ClockWidth, 100));
            var plate = OwnerUiLayout.Rect(_clockRoot, "ClockFace").gameObject.AddComponent<HudCard>();
            OwnerUiLayout.Place(plate.rectTransform, 0, 0, ClockWidth, ClockHeight);
            plate.Glass(HudDraw.GlassDeep).raycastTarget = false;

            _canRing = OwnerUiLayout.Rect(_clockRoot, "CanProtection").gameObject.AddComponent<HudRing>();
            OwnerUiLayout.Place(_canRing.rectTransform, 12, 8, 42, 42);
            _canRing.Thickness = 3; _canRing.Track = Color.clear; _canRing.color = HudDraw.Honey;
            _canRing.Fill = 0; _canRing.raycastTarget = false;
            _canGlyph = OwnerUiLayout.Rect(_clockRoot, "CanStateIcon").gameObject.AddComponent<HudBadge>();
            OwnerUiLayout.Place(_canGlyph.rectTransform, 17, 13, 32, 32);
            _canGlyph.Detail = HudDraw.GlassDeep; _canGlyph.raycastTarget = false;

            // ⚠️ THE CLOCK KEEPS DARUMADROP: `docs/FONT_USAGE.md`, October 6, names `TimeLeft` as the
            // one in-game label that does not take DIN. Only its colour and plate changed here.
            _clock = OwnerUiLayout.Text(_clockRoot, "TimeLeft", "", 46, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_clock.rectTransform, 56, 1, 134, 56);
            _clock.alignment = TextAnchor.MiddleCenter; _clock.verticalOverflow = VerticalWrapMode.Overflow;
            _clock.horizontalOverflow = HorizontalWrapMode.Overflow;
            _clock.color = HudDraw.Ivory;

            // The round track is a slim glass tray of leaning ticks under the clock, one per round in
            // that round's taya colour (`HudPips` keeps its meaning; only its shape changed).
            var track = OwnerUiLayout.Rect(_clockRoot, "RoundTrack").gameObject.AddComponent<HudCard>();
            OwnerUiLayout.Place(track.rectTransform, (ClockWidth - 214) * .5f, ClockHeight + 8, 214, 22);
            _roundTrack = track.rectTransform;
            track.Glass(HudDraw.Glass, HudDraw.Lean, 2, .2f).raycastTarget = false; track.BorderWidth = 0;
            _pips = OwnerUiLayout.Rect(_clockRoot, "RoundPips").gameObject.AddComponent<HudPips>();
            OwnerUiLayout.Place(_pips.rectTransform, -40, ClockHeight + 10, ClockWidth + 80, 18); _pips.raycastTarget = false;
            _pips.Diameter = 12; _pips.Gap = 10;
            _round = Ink(_clockRoot, "RoundLabel", "", 28, false);
            OwnerUiLayout.Place(_round.rectTransform, -134, ClockHeight + 38, ClockWidth + 268, 36);
        }

        private void BuildChip(int i)
        {
            var row = OwnerUiLayout.Rect(_scoreRoot, "ScoreRow" + i);
            OwnerUiLayout.Place(row, ChipX[i], 0, ChipWidth, ChipHeight); _scoreRows[i] = row;
            var card = row.gameObject.AddComponent<HudCard>();
            card.Glass(HudDraw.Glass).raycastTarget = false; _chipCards[i] = card;

            // The portrait stands on a leaning tile in the seat colour: Overwatch's hero icon, and the
            // colour that ties this chip to its round tick, its feed rows and its nameplate.
            var swatch = OwnerUiLayout.Rect(row, "SeatSwatch").gameObject.AddComponent<HudCard>();
            OwnerUiLayout.Place(swatch.rectTransform, 6, 4, 58, ChipHeight - 8);
            swatch.Glass(PlayerIdentity.Colour(i), HudDraw.Lean, 2, .3f); swatch.BorderWidth = 0; swatch.FollowContrast = false;
            swatch.raycastTarget = false; _chipSwatches[i] = swatch; _portraitTiles[i] = swatch;
            _portraits[i] = OwnerPortraitArt.Create(row, "PlayerPortrait", "");
            OwnerUiLayout.Place(_portraits[i].rectTransform, 11, 5, 48, 48);

            // Kept for the probes; the role lives on the state icon at the chip's right end.
            _roleBadges[i] = OwnerUiLayout.Rect(row, "RoleBadge").gameObject.AddComponent<HudBadge>();
            OwnerUiLayout.Place(_roleBadges[i].rectTransform, 50, 34, 24, 24);
            _roleBadges[i].Detail = UiTheme.Defense; _roleBadges[i].raycastTarget = false;
            _crowns[i] = OwnerUiLayout.Rect(row, "LeaderCrown").gameObject.AddComponent<HudBadge>();
            OwnerUiLayout.Place(_crowns[i].rectTransform, 22, -22, 30, 24); _crowns[i].raycastTarget = false;

            // The seat tag is a small dark tab on the portrait's lower corner, in the seat colour.
            var tagPlate = OwnerUiLayout.Rect(row, "SeatTagPlate").gameObject.AddComponent<HudCard>();
            OwnerUiLayout.Place(tagPlate.rectTransform, 0, ChipHeight - 24, 38, 26);
            // Near-opaque: at glass strength the tag's seat colour sank into the portrait under it.
            tagPlate.Glass(new Color32(22, 12, 7, 225), HudDraw.Lean, 2, 0).raycastTarget = false; tagPlate.BorderWidth = 0;
            _seatTags[i] = OwnerUiLayout.Text(row, "SeatTag", "", 28, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_seatTags[i].rectTransform, 0, ChipHeight - 25, 38, 26);
            _seatTags[i].alignment = TextAnchor.MiddleCenter; _seatTags[i].color = PlayerIdentity.Colour(i);
            _seatTags[i].verticalOverflow = VerticalWrapMode.Overflow; _seatTags[i].horizontalOverflow = HorizontalWrapMode.Overflow;

            // The state well is retired as a surface (a second coloured tile per chip was the
            // clutter); its object stays for the reading layout and the glyph draws straight on glass.
            var well = _roleChips[i] = OwnerUiLayout.Rect(row, "StateWell").gameObject.AddComponent<HudCard>();
            OwnerUiLayout.Place(well.rectTransform, ChipWidth - 30, (ChipHeight - 40) * .5f, 26, 40);
            well.Glass(Color.clear, HudDraw.Lean, 2, 0).raycastTarget = false; well.BorderWidth = 0; well.enabled = false;
            well.FollowContrast = false;
            _stateBadges[i] = OwnerUiLayout.Rect(row, "StateBadge").gameObject.AddComponent<HudBadge>();
            OwnerUiLayout.Place(_stateBadges[i].rectTransform, ChipWidth - 28, (ChipHeight - 32) * .5f, 22, 32);
            _stateBadges[i].Detail = HudDraw.GlassDeep; _stateBadges[i].Rim = Color.clear; _stateBadges[i].raycastTarget = false;

            // Spectators only: the name sits under the chip, sized to it (`PaintCardName` ellipsises).
            _names[i] = OwnerUiLayout.Text(row, "PlayerName", "", 28, OwnerUiLayout.TypeRole.Reading);
            OwnerUiLayout.Place(_names[i].rectTransform, 0, ChipHeight + 8, ChipWidth, 32);
            _names[i].alignment = TextAnchor.MiddleCenter; _names[i].color = HudDraw.Ivory;
            _names[i].horizontalOverflow = HorizontalWrapMode.Overflow; _names[i].verticalOverflow = VerticalWrapMode.Overflow;
            var nameEdge = _names[i].gameObject.AddComponent<Outline>(); nameEdge.effectColor = UiTheme.InGameOutline;
            nameEdge.effectDistance = new Vector2(1.5f, -1.5f);
            _names[i].enabled = false;

            _scores[i] = OwnerUiLayout.Text(row, "Score", "", ScoreFont, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_scores[i].rectTransform, ScoreLeft, 2, ScoreWidth, ChipHeight - 4);
            _scores[i].alignment = TextAnchor.MiddleLeft; _scores[i].color = HudDraw.Ivory;
            _scores[i].fontStyle = FontStyle.Italic;
            _scores[i].horizontalOverflow = HorizontalWrapMode.Overflow;
            _scores[i].verticalOverflow = VerticalWrapMode.Overflow;
            var scoreEdge = _scores[i].gameObject.AddComponent<Outline>(); scoreEdge.effectColor = UiTheme.InGameOutline;
            scoreEdge.effectDistance = new Vector2(1.5f, -1.5f);
            BuildScoreGhost(i, row);
            BuildSpectatorCard(i, row);

            // YOU: a honey bar along the chip's foot. The label object is kept (and silent) because
            // the honey bar plus the honey score already say it without a word.
            var mark = _localMarks[i] = OwnerUiLayout.Rect(row, "LocalMark").gameObject.AddComponent<HudCard>();
            OwnerUiLayout.Place(mark.rectTransform, 8, ChipHeight + 2, ChipWidth - 16, 5);
            mark.Glass(HudDraw.Honey, HudDraw.Lean, 1, .3f).raycastTarget = false; mark.BorderWidth = 0; mark.FollowContrast = false;
            var you = OwnerUiLayout.Text(mark.transform, "LocalMarkLabel", "You", 28, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Fill(you.rectTransform); you.alignment = TextAnchor.MiddleCenter; you.color = HudDraw.Brown;
            you.horizontalOverflow = HorizontalWrapMode.Overflow; you.verticalOverflow = VerticalWrapMode.Overflow;
            you.enabled = false;
            mark.gameObject.SetActive(false);

            _roles[i] = Ink(row, "RoleState", "", 28, false);
            OwnerUiLayout.Place(_roles[i].rectTransform, -30, ChipHeight + 40, ChipWidth + 60, 34); _roles[i].enabled = false;
        }

        /// <summary>The card's name, once per change: the engine's seat suffix removed (the seat
        /// tag already says it) and an ellipsis only when the real name is wider than the card.</summary>
        private void PaintCardName(int i, int slot)
        {
            var who = GameServices.Round?.PlayerAt(slot);
            string name = SeatLabel.Raw(slot); string suffix = who != null ? who.LabelSuffix : "";
            if (!string.IsNullOrEmpty(suffix) && name.EndsWith(suffix, System.StringComparison.Ordinal))
                name = name.Substring(0, name.Length - suffix.Length).TrimEnd(' ', '·');
            if (_barNames[i] == name) { if (_names[i].text != _barNameLabels[i]) _names[i].text = _barNameLabels[i]; return; }
            _barNames[i] = name; _names[i].text = name;
            float room = _names[i].rectTransform.rect.width;
            if (_names[i].preferredWidth > room)
            {
                var starts = System.Globalization.StringInfo.ParseCombiningCharacters(name);
                int length = starts.Length;
                while (length > 1 && _names[i].preferredWidth > room)
                    _names[i].text = name.Substring(0, starts[--length]).TrimEnd() + "…";
            }
            _barNameLabels[i] = _names[i].text;
        }

        /// <summary>The compact chip never changes the exact score held by MatchDirector or shown at results.</summary>
        public static string ScoreTextForChip(int score)
        {
            long magnitude = score < 0 ? -(long)score : score;
            if (magnitude < 1000000) return score.ToString(System.Globalization.CultureInfo.InvariantCulture);

            long unit = magnitude >= 1000000000 ? 1000000000 : 1000000;
            string suffix = unit == 1000000000 ? "B" : "M";
            long whole = magnitude / unit;
            int places = whole < 10 ? 2 : whole < 100 ? 1 : 0;
            long scale = places == 2 ? 100 : places == 1 ? 10 : 1;
            long fraction = magnitude % unit * scale / unit;
            string shown = whole.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (fraction > 0)
                shown += "." + fraction.ToString("D" + places, System.Globalization.CultureInfo.InvariantCulture).TrimEnd('0');
            return (score < 0 ? "-" : "") + shown + suffix;
        }

        /// <summary>Fit only a changed score, leaving normal three-digit totals at their authored size.</summary>
        public static void PaintScoreValue(Text label, int value, int size = ScoreFont)
        {
            string shown = ScoreTextForChip(value);
            if (label.text == shown) return;
            label.text = shown;
            label.fontSize = size;
            while (label.fontSize > ScoreFontFloor && label.preferredWidth > label.rectTransform.rect.width)
                label.fontSize -= 2;
        }

        private void BuildStaminaArc()
        {
            var root = OwnerUiLayout.Rect(_root, "StaminaArc");
            Pin(root, new Vector2(.5f, .5f), Vector2.zero, new Vector2(128, 128));
            _staminaGroup = root.gameObject.AddComponent<CanvasGroup>();
            _staminaGroup.alpha = 0; _staminaGroup.blocksRaycasts = false; _staminaGroup.interactable = false;
            _staminaArc = OwnerUiLayout.Rect(root, "StaminaMeter").gameObject.AddComponent<HudRing>();
            OwnerUiLayout.Fill(_staminaArc.rectTransform);
            // The right-hand side of the reticle, the way a stamina wheel sits beside the
            // player in Breath of the Wild: close enough to read without looking away,
            // clear of the aim line, and gone the moment it is full.
            _staminaArc.StartDegrees = 38; _staminaArc.SpanDegrees = 76; _staminaArc.Thickness = 5;
            // ⚠️ Drains from the top: the fill is anchored at the arc's lower end (see `FillFromEnd`).
            _staminaArc.FillFromEnd = true;
            _staminaArc.Track = new Color(0, 0, 0, .42f); _staminaArc.color = CourtPresentationPalette.Gold;
            _staminaArc.raycastTarget = false;
            _staminaRoot = root;
            BuildDrainPins(root);
        }

        /// <summary>
        /// ⚠️ DRAINED, ON THE VICTIM'S OWN ARC (HERO-10 v3, plan 9.5: *"the bar pinches in the middle like a twisted cloth and
        /// empties at once; two crossed pins stamp over it"*, then *"the pinned bar; no sprint"* until *"the pins pop out and the
        /// bar starts refilling"*). Two pins, gold shafts and crimson heads, crossed over the middle of the arc beside the
        /// reticle, stamped in when DRAINED lands and popped out when it ends. Outlined black like every in-match mark.
        /// </summary>
        private void BuildDrainPins(RectTransform arc)
        {
            _drainPins = OwnerUiLayout.Rect(arc, "DrainedPins");
            Pin(_drainPins, new Vector2(.5f, .5f), new Vector2(60, 0), new Vector2(44, 44));
            DrainPin(_drainPins, "PinA", 38f);
            DrainPin(_drainPins, "PinB", -38f);
            _drainPins.gameObject.SetActive(false);
        }

        private static void DrainPin(RectTransform parent, string name, float degrees)
        {
            var pin = OwnerUiLayout.Rect(parent, name);
            Pin(pin, new Vector2(.5f, .5f), Vector2.zero, new Vector2(44, 44));
            pin.localRotation = Quaternion.Euler(0, 0, degrees);
            var shaft = OwnerUiLayout.Rect(pin, "Shaft").gameObject.AddComponent<Image>();
            Pin(shaft.rectTransform, new Vector2(.5f, .5f), new Vector2(0, -3), new Vector2(4, 36));
            shaft.color = CourtPresentationPalette.Gold; shaft.raycastTarget = false;
            shaft.gameObject.AddComponent<Outline>().effectColor = UiTheme.InGameOutline;
            var head = OwnerUiLayout.Rect(pin, "Head").gameObject.AddComponent<Image>();
            Pin(head.rectTransform, new Vector2(.5f, .5f), new Vector2(0, 17), new Vector2(10, 10));
            head.color = new Color(.86f, .14f, .24f, 1f); head.raycastTarget = false;
            head.gameObject.AddComponent<Outline>().effectColor = UiTheme.InGameOutline;
        }

        private void MatchBarClock(MatchDirector match, RoundDirector round, int time)
        {
            bool urgent = time <= 10 && round.RoundActive;
            _clock.color = urgent ? HudDraw.Honey : HudDraw.Ivory;
            float tick = urgent && !Settings.SettingsStore.Current.ReducedUiMotion ? 1 - Mathf.Repeat(round.TimeLeft, 1f) : 1;
            float pulse = 1 + .14f * (1 - Mathf.Clamp01(tick / .25f)) * (urgent ? 1 : 0);
            _clock.rectTransform.localScale = Vector3.one * pulse;
            _pips.Set(Mathf.Max(1, match.TotalRounds), match.IsWarmupBuffer ? 0 : match.RoundNumber);
            float gap = _pips.Count >= 4 && _pips.Count % 2 == 0 ? _pips.Gap : 0;
            float width = _pips.Count * _pips.Diameter + (_pips.Count - 1) * _pips.Gap + gap + 18;
            if(_roundTrack != null) OwnerUiLayout.Place(_roundTrack, (ClockWidth-width)*.5f, ClockHeight + 8, width, 22);
            OwnerUiLayout.Place(_pips.rectTransform, (ClockWidth-width)*.5f, ClockHeight + 10, width, 18);
            // Round pips and the ready prompt already cover this information.
            _round.enabled = false;
        }

        private void MatchBarCan(Lata lata)
        {
            if (lata == null) { _canGlyph.Show(HudBadge.Glyph.None, Color.clear, Color.clear); _canRing.Set(0); _canShownUpright = null; return; }
            // The icon FLIPS over when the can changes state (owner, 2026-10-22: everything animated): it turns edge-on,
            // swaps face at the halfway point, and lands a size too big before settling.
            if (_canShownUpright == null) _canShownUpright = lata.IsUpright;
            else if (_canShownUpright != lata.IsUpright && _canFlipAt < 0) _canFlipAt = Time.unscaledTime;
            float flip = _canFlipAt < 0 || Settings.SettingsStore.Current.ReducedUiMotion ? 1 : Mathf.Clamp01((Time.unscaledTime - _canFlipAt) / CanFlipSeconds);
            if (flip >= .5f && _canShownUpright != lata.IsUpright) _canShownUpright = lata.IsUpright;
            if (flip >= 1) _canFlipAt = -1;
            float edgeOn = flip < .5f ? Mathf.Cos(flip * Mathf.PI) : -Mathf.Cos(flip * Mathf.PI);
            float land = flip >= .5f && flip < 1 ? .3f * Mathf.Sin((flip - .5f) * 2 * Mathf.PI) : 0;
            _canGlyph.rectTransform.localScale = new Vector3(Mathf.Max(.04f, edgeOn) * (1 + land), 1 + land, 1);
            if (_canShownUpright == true) _canGlyph.Show(HudBadge.Glyph.Can, HudDraw.Ivory, Color.clear);
            else _canGlyph.Show(HudBadge.Glyph.CanDown, HudDraw.Alarm, Color.clear);
            _canRing.Set(lata.IsProtected ? lata.ProtectionLeft / Mathf.Max(.01f, Balance.ThrowRestoreCooldown) : 0);
        }

        private void MatchBarChip(int i, int slot, CharacterMotor actor, bool defender, bool mine, bool leader, bool spectating)
        {
            var seat = PlayerIdentity.Colour(slot);
            _seatTags[i].text = PlayerIdentity.Label(slot);
            if (_seatTags[i].color != seat) _seatTags[i].color = seat;
            // The name is always written (tests and the spectator read it) and drawn only for a
            // spectator: in play the world nameplate already says who stands where.
            _names[i].enabled = spectating; PaintCardName(i, slot);
            if (_localMarks[i] != null && _localMarks[i].gameObject.activeSelf != mine) _localMarks[i].gameObject.SetActive(mine);
            var scoreInk = mine ? HudDraw.Honey : HudDraw.Ivory;
            if (_scores[i].color != scoreInk) _scores[i].color = scoreInk;
            if (_chipSwatches[i].color != seat) _chipSwatches[i].color = seat;
            _roleBadges[i].Show(HudBadge.Glyph.None, Color.clear, Color.clear);
            PaintCrown(i, leader);
            NoteSpectatorCard(i, actor, defender, spectating);

            // Guide p.10: the thrower's slipper is whole in hand and half when away; the taya's
            // can is whole upright and half when down. A stun or a swim overrides both, because
            // a player who cannot act is the more urgent fact about that seat.
            var lata = GameServices.Round != null ? GameServices.Round.Lata : null;
            var ink = HudDraw.Ivory; var faded = ink; faded.a = .32f;
            if (actor.IsSwimming) _stateBadges[i].Show(HudBadge.Glyph.Wave, ink, Color.clear);
            else if (actor.IsTripped || actor.IsStunned) _stateBadges[i].Show(HudBadge.Glyph.Star, HudDraw.Honey, Color.clear);
            else if (defender) _stateBadges[i].Show(lata == null || lata.IsUpright ? HudBadge.Glyph.Can : HudBadge.Glyph.CanDown,
                lata == null || lata.IsUpright ? ink : faded, Color.clear);
            else _stateBadges[i].Show(HudBadge.Glyph.Slipper, actor.HoldingSlipper ? ink : faded, Color.clear);
        }

        private void StaminaArc(CharacterMotor local)
        {
            if (_staminaArc == null) return;
            bool want = false, drained = false, wringing = false;
            if (local != null && local.Stamina != null)
            {
                float ratio = Mathf.Clamp01(local.Stamina.Ratio);
                bool fatigued = local.Stamina.IsFatigued;
                drained = local.IsDrained;
                // HERO-10 v3: Phaister's DRAIN mark is on them and wringing (plan 9.5: *"their stamina bar shakes ... spend it now"*).
                wringing = local.VoodooMark == VoodooMarkKind.Drain;
                // ⚠️ 2026-10-20: THE ARC IS STAMINA'S ONLY READOUT AGAIN. A bottom-left stamina bar was
                // tried in the glass HUD pass and removed at the owner's word ("remove healthbar in
                // bottom left corner"), so the arc keeps its whole VISUAL-1.4 job on every device.
                want = ratio < .995f || fatigued || drained || wringing;
                _staminaArc.Set(ratio);
                var fill = drained ? new Color(.62f, .30f, .34f, 1f) : fatigued ? OwnerUiTheme.Current.Orange : CourtPresentationPalette.Gold;
                _staminaArc.Paint(fill, drained ? new Color(.30f, .04f, .08f, .55f) : new Color(0, 0, 0, .42f));
                // Zack's own energy takes this arc over (`TumpMatchReadout.Zack.cs`); every other body keeps it as it is.
                if (ZackMeter(local, ratio, fatigued, drained)) want = true;
            }
            else ZackMeter(null, 1, false, false);
            ZackVictimWarning(local);
            float dt = Time.unscaledDeltaTime;
            StepDrainPins(drained, wringing, local, dt);
            _staminaAlpha = Mathf.MoveTowards(_staminaAlpha, want ? 1 : 0, dt / (want ? .15f : .4f));
            _staminaGroup.alpha = _staminaAlpha;
        }

        /// <summary>The pins stamp in (big to size in 0.12 s) when DRAINED lands, pop out when it ends; the arc shakes while wrung.</summary>
        private void StepDrainPins(bool drained, bool wringing, CharacterMotor local, float dt)
        {
            if (_drainPins == null || _staminaRoot == null) return;
            if (drained && _drainPinsAge < 0.0f) _drainPinsAge = 0.0f;
            if (!drained && _drainPinsAge >= 0.0f) _drainPinsAge = -1.0f;
            bool show = _drainPinsAge >= 0.0f;
            if (_drainPins.gameObject.activeSelf != show) _drainPins.gameObject.SetActive(show);
            if (show)
            {
                _drainPinsAge += dt;
                float stamp = Mathf.Clamp01(_drainPinsAge / .12f);
                _drainPins.localScale = Vector3.one * Mathf.Lerp(1.8f, 1.0f, stamp * stamp);
            }
            // The shake speeds up as the wring tightens toward the drain.
            float tighten = wringing && local != null ? Mathf.Clamp01(local.VoodooMarkAge / VoodooRules.DrainDelaySeconds) : 0.0f;
            Vector2 shake = wringing ? new Vector2(Mathf.Sin(Time.unscaledTime * (40f + 50f * tighten)), Mathf.Sin(Time.unscaledTime * 53f))
                                       * (1.5f + 2.5f * tighten) : Vector2.zero;
            _staminaArc.rectTransform.anchoredPosition = shake;
        }
    }
}
