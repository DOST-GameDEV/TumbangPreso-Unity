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
        private HudRing _canRing;
        private HudPips _pips;
        private HudRing _staminaArc;
        private CanvasGroup _staminaGroup;
        private float _staminaAlpha;
        private HudCard _promptPlate;

        /// <summary>Fits the prompt pill to the words it carries; hidden when there are none.</summary>
        private void SizePromptPlate()
        {
            if (_promptPlate == null || _prompt == null) return;
            bool show = _promptRoot.gameObject.activeInHierarchy && _prompt.enabled && !string.IsNullOrEmpty(_prompt.text);
            _promptPlate.enabled = show;
            if (!show) return;
            float width = Mathf.Min(1100, _prompt.preferredWidth + 56);
            OwnerUiLayout.Place(_promptPlate.rectTransform, (1100 - width) * .5f, 11, width, 52);
        }

        private const float ChipWidth = 180, ChipHeight = 64, ClockWidth = 232, BarTop = 12;
        private static readonly float[] ChipX = { 2, 192, 628, 818 };

        private void BuildMatchBar()
        {
            _scoreRoot = OwnerUiLayout.Rect(_root, "MatchScores");
            Pin(_scoreRoot, new Vector2(.5f, 1), new Vector2(0, -(BarTop + ChipHeight * .5f)), new Vector2(1000, ChipHeight));
            for (int i = 0; i < 4; i++) BuildChip(i);

            _clockRoot = OwnerUiLayout.Rect(_root, "RoundClock");
            Pin(_clockRoot, new Vector2(.5f, 1), new Vector2(0, -(BarTop + 48)), new Vector2(ClockWidth, 96));
            var plate = OwnerUiLayout.Rect(_clockRoot, "ClockFace").gameObject.AddComponent<HudCard>();
            OwnerUiLayout.Place(plate.rectTransform, 0, 0, ClockWidth, ChipHeight);
            plate.color = HudDraw.Plate; plate.Radius = 16; plate.raycastTarget = false;

            _canRing = OwnerUiLayout.Rect(_clockRoot, "CanProtection").gameObject.AddComponent<HudRing>();
            OwnerUiLayout.Place(_canRing.rectTransform, 9, 9, 46, 46);
            _canRing.Thickness = 3; _canRing.Track = Color.clear; _canRing.color = CourtPresentationPalette.Paper;
            _canRing.Fill = 0; _canRing.raycastTarget = false;
            _canGlyph = OwnerUiLayout.Rect(_clockRoot, "CanStateIcon").gameObject.AddComponent<HudBadge>();
            OwnerUiLayout.Place(_canGlyph.rectTransform, 14, 14, 36, 36);
            _canGlyph.Detail = HudDraw.Plate; _canGlyph.raycastTarget = false;

            _clock = OwnerUiLayout.Text(_clockRoot, "TimeLeft", "", 46, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_clock.rectTransform, 58, 2, 166, 60);
            _clock.alignment = TextAnchor.MiddleCenter; _clock.verticalOverflow = VerticalWrapMode.Overflow;
            _clock.color = CourtPresentationPalette.Paper;

            // A quiet plate under the pips so they read against bright sky as well as dark roofs.
            var track = OwnerUiLayout.Rect(_clockRoot, "RoundTrack").gameObject.AddComponent<HudCard>();
            OwnerUiLayout.Place(track.rectTransform, (ClockWidth - 214) * .5f, 69, 214, 24);
            track.color = new Color(HudDraw.Plate.r, HudDraw.Plate.g, HudDraw.Plate.b, .72f); track.Radius = 12; track.ShadowAlpha = 0; track.raycastTarget = false;
            _pips = OwnerUiLayout.Rect(_clockRoot, "RoundPips").gameObject.AddComponent<HudPips>();
            OwnerUiLayout.Place(_pips.rectTransform, -40, 72, ClockWidth + 80, 18); _pips.raycastTarget = false;
            _pips.Diameter = 12; _pips.Gap = 10;
            _round = Ink(_clockRoot, "RoundLabel", "", 28, false);
            OwnerUiLayout.Place(_round.rectTransform, -134, 96, ClockWidth + 268, 36);
        }

        private void BuildChip(int i)
        {
            var row = OwnerUiLayout.Rect(_scoreRoot, "ScoreRow" + i);
            OwnerUiLayout.Place(row, ChipX[i], 0, ChipWidth, ChipHeight); _scoreRows[i] = row;
            var card = row.gameObject.AddComponent<HudCard>();
            card.color = CourtPresentationPalette.Paper; card.Radius = 14; card.raycastTarget = false; _chipCards[i] = card;

            var swatch = OwnerUiLayout.Rect(row, "SeatSwatch").gameObject.AddComponent<HudCard>();
            OwnerUiLayout.Place(swatch.rectTransform, 6, 6, 52, 52);
            swatch.Radius = 10; swatch.ShadowAlpha = 0; swatch.FollowContrast = false;
            swatch.color = PlayerIdentity.Colour(i); swatch.raycastTarget = false; _chipSwatches[i] = swatch;
            _portraits[i] = OwnerPortraitArt.Create(row, "PlayerPortrait", "");
            OwnerUiLayout.Place(_portraits[i].rectTransform, 8, 8, 48, 48);

            _roleBadges[i] = OwnerUiLayout.Rect(row, "RoleBadge").gameObject.AddComponent<HudBadge>();
            OwnerUiLayout.Place(_roleBadges[i].rectTransform, 38, 36, 28, 28);
            _roleBadges[i].Detail = UiTheme.Defense; _roleBadges[i].raycastTarget = false;
            _crowns[i] = OwnerUiLayout.Rect(row, "LeaderCrown").gameObject.AddComponent<HudBadge>();
            OwnerUiLayout.Place(_crowns[i].rectTransform, 18, -15, 28, 22); _crowns[i].raycastTarget = false;

            _seatTags[i] = OwnerUiLayout.Text(row, "SeatTag", "", 28, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_seatTags[i].rectTransform, 64, 2, 56, 32);
            _seatTags[i].color = HudDraw.CardInk; _seatTags[i].verticalOverflow = VerticalWrapMode.Overflow;
            _stateBadges[i] = OwnerUiLayout.Rect(row, "StateBadge").gameObject.AddComponent<HudBadge>();
            OwnerUiLayout.Place(_stateBadges[i].rectTransform, 64, 32, 30, 30);
            _stateBadges[i].Detail = CourtPresentationPalette.Paper; _stateBadges[i].raycastTarget = false;

            _scores[i] = OwnerUiLayout.Text(row, "Score", "", 36, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_scores[i].rectTransform, 90, 2, 82, 60);
            _scores[i].alignment = TextAnchor.MiddleRight; _scores[i].color = HudDraw.CardInk;
            _scores[i].verticalOverflow = VerticalWrapMode.Overflow;

            _names[i] = Ink(row, "PlayerName", "", 28, false);
            OwnerUiLayout.Place(_names[i].rectTransform, -30, 68, ChipWidth + 60, 34);
            _roles[i] = Ink(row, "RoleState", "", 28, false);
            OwnerUiLayout.Place(_roles[i].rectTransform, -30, 102, ChipWidth + 60, 34); _roles[i].enabled = false;
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
        }

        private void MatchBarClock(MatchDirector match, RoundDirector round, int time)
        {
            _clock.color = time <= 10 && round.RoundActive ? OwnerUiTheme.Current.Orange : CourtPresentationPalette.Paper;
            _pips.Set(Mathf.Max(1, match.TotalRounds), match.IsWarmupBuffer ? 0 : match.RoundNumber);
            // The round is the pips' job now. The line only speaks in the warm-up, where there
            // is a real sentence to say and no round to point at.
            _round.enabled = match.IsWarmupBuffer;
        }

        private void MatchBarCan(Lata lata)
        {
            if (lata == null) { _canGlyph.Show(HudBadge.Glyph.None, Color.clear, Color.clear); _canRing.Set(0); return; }
            if (lata.IsUpright) _canGlyph.Show(HudBadge.Glyph.Can, CourtPresentationPalette.Paper, Color.clear);
            else _canGlyph.Show(HudBadge.Glyph.CanDown, OwnerUiTheme.Current.Orange, Color.clear);
            _canRing.Set(lata.IsProtected ? lata.ProtectionLeft / Mathf.Max(.01f, Balance.ThrowRestoreCooldown) : 0);
        }

        private void MatchBarChip(int i, int slot, CharacterMotor actor, bool defender, bool mine, bool leader, bool spectating)
        {
            var seat = PlayerIdentity.Colour(slot);
            _seatTags[i].text = PlayerIdentity.Label(slot);
            _names[i].enabled = spectating; _names[i].color = seat;
            _chipCards[i].Style(CourtPresentationPalette.Paper, defender ? UiTheme.Defense : Color.clear, defender ? 3.5f : 0,
                mine ? CourtPresentationPalette.Gold : Color.clear, mine ? 4 : 0);
            if (_chipSwatches[i].color != seat) _chipSwatches[i].color = seat;
            if (defender) _roleBadges[i].Show(HudBadge.Glyph.Can, CourtPresentationPalette.Paper, UiTheme.Defense);
            else _roleBadges[i].Show(HudBadge.Glyph.None, Color.clear, Color.clear);
            _crowns[i].Show(leader ? HudBadge.Glyph.Crown : HudBadge.Glyph.None, CourtPresentationPalette.Gold, Color.clear);

            // ⚠️ HIGH CONTRAST TURNS THE CARD BLACK (`HudCard.FollowContrast`), so the badge ink
            // turns white with it; dark ink on the contrast plate was invisible (look-1.6-v1).
            var ink = Settings.SettingsStore.Current.HighContrastHud ? Color.white : HudDraw.CardInk;
            var faded = ink; faded.a = .32f;
            if (actor.IsSwimming) _stateBadges[i].Show(HudBadge.Glyph.Wave, ink, Color.clear);
            else if (actor.IsTripped || actor.IsStunned) _stateBadges[i].Show(HudBadge.Glyph.Star, OwnerUiTheme.Current.Orange, Color.clear);
            else if (defender) _stateBadges[i].Show(HudBadge.Glyph.None, Color.clear, Color.clear);
            else if (actor.HoldingSlipper) _stateBadges[i].Show(HudBadge.Glyph.Slipper, ink, Color.clear);
            else _stateBadges[i].Show(HudBadge.Glyph.Slipper, faded, Color.clear);
        }

        private void StaminaArc(CharacterMotor local)
        {
            if (_staminaArc == null) return;
            bool want = false;
            if (local != null && local.Stamina != null)
            {
                float ratio = Mathf.Clamp01(local.Stamina.Ratio);
                bool fatigued = local.Stamina.IsFatigued;
                want = ratio < .995f || fatigued;
                _staminaArc.Set(ratio);
                _staminaArc.Paint(fatigued ? OwnerUiTheme.Current.Orange : CourtPresentationPalette.Gold, new Color(0, 0, 0, .42f));
            }
            float dt = Time.unscaledDeltaTime;
            _staminaAlpha = Mathf.MoveTowards(_staminaAlpha, want ? 1 : 0, dt / (want ? .15f : .4f));
            _staminaGroup.alpha = _staminaAlpha;
        }
    }
}
