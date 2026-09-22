using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class TumpMatchReadout
    {
        public void Build(Transform owner)
        {
            Canvas = OwnerUiLayout.Canvas(owner, "OwnerMatchCanvas", 100);
            Canvas.GetComponent<InputLayer.ScreenFocus>().enabled = false; _root = (RectTransform)Canvas.transform;
            _effects = gameObject.AddComponent<TumpHudEffects>(); _effects.Build(_root);
            // VISUAL-1.4: the match bar replaces the corner score slabs and the clock box.
            // `BuildCourtScores` and `BuildCourtClock` below are the previous layout, kept for
            // the record and no longer called.
            BuildMatchBar(); BuildCourtCan(); BuildCourtPersonal(); BuildStaminaArc(); BuildCourtPrompts();
            MatchEventFeed.Create(_root);
            MatchMomentBanner.Create(_root);
            _powers = gameObject.AddComponent<TumpPowerReadout>(); _powers.Build(_root);
            // VISUAL-1.4: the toast sits under the bar on the halftime banner's brush shape in
            // the clock's plate colour, so a rare call-out is one family with the bar and the
            // popup instead of loose outlined words over the sky.
            _toastPlate = OwnerUiLayout.Rect(_root, "MatchToastPlate").gameObject.AddComponent<CourtPopupGraphic>();
            _toastPlate.Brush = true; _toastPlate.color = HudDraw.Plate; _toastPlate.raycastTarget = false; _toastPlate.enabled = false;
            Pin(_toastPlate.rectTransform, new Vector2(.5f, 1), new Vector2(0, -176), new Vector2(400, 64));
            _toast = Ink(_root, "MatchToast", "", 36, true);
            Pin(_toast.rectTransform, new Vector2(.5f, 1), new Vector2(0, -176), new Vector2(1080, 64)); _toast.enabled = false;
            BuildScorePops();
            _countdown = Ink(_root, "Countdown", "", 108, true);
            Pin(_countdown.rectTransform, new Vector2(.5f, .58f), Vector2.zero, new Vector2(740, 180)); _countdown.enabled = false;
            // VISUAL-1.6: a drawn reticle that carries charge, pektus, cooldown, refusal and
            // (for the taya) reach, instead of a "+" in the display face.
            _reticle = OwnerUiLayout.Rect(_root, "Reticle").gameObject.AddComponent<HudReticle>();
            _reticle.color = CourtPresentationPalette.Paper; _reticle.raycastTarget = false; _crosshair = _reticle;
            Pin(_reticle.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(96, 96));
            // VISUAL-1.4: a drawn hit mark (four ticks on black keels) instead of a "x" in the
            // display face. Same name, so the reading layout still scales it.
            _hitMark = OwnerUiLayout.Rect(_root, "HitConfirmation").gameObject.AddComponent<HudBadge>();
            Pin(_hitMark.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(64, 64));
            _hitMark.Kind = HudBadge.Glyph.Hit; _hitMark.RimWidth = 2; _hitMark.raycastTarget = false; _hitMark.enabled = false;
            _spectator = Ink(_root, "SpectatorReadout", "", 28, false);
            Pin(_spectator.rectTransform, new Vector2(.5f, 0), new Vector2(0, 106), new Vector2(1460, 146));
            _sandbox = Ink(_root, "SandboxState", "", 28, false);
            Pin(_sandbox.rectTransform, new Vector2(1, 0), new Vector2(-286, 71), new Vector2(550, 54));
            var version = Ink(_root, "GameVersion", "", 28, false);
            Pin(version.rectTransform, new Vector2(1, 0), new Vector2(-230, 22), new Vector2(420, 40)); GameVersion.ApplyTo(version);
            CalloutCaption.Create(_root);
            HudReadingLayout.Install(_root);
            HudContrast.Install(_root);
        }
        private void BuildCourtScores()
        {
            _scoreRoot = OwnerUiLayout.Rect(_root, "MatchScores");
            Pin(_scoreRoot, new Vector2(0, 1), new Vector2(288, -186), new Vector2(530, 320));
            for (int i = 0; i < 4; i++)
            {
                var row = OwnerUiLayout.Rect(_scoreRoot, "ScoreRow" + i); OwnerUiLayout.Place(row, 0, i * 80, 530, 78); _scoreRows[i] = row;
                _scoreAccents[i] = row.gameObject.AddComponent<OwnerScoreStrip>();
                _scoreAccents[i].raycastTarget = false;
                _portraits[i] = OwnerPortraitArt.Create(row, "PlayerPortrait", ""); OwnerUiLayout.Place(_portraits[i].rectTransform, 8, 5, 65, 65);
                _names[i] = Ink(row, "PlayerName", "", 28, true); _names[i].alignment = TextAnchor.MiddleLeft;
                OwnerUiLayout.Place(_names[i].rectTransform, 83, 1, 318, 43);
                _scores[i] = Ink(row, "Score", "", 31, true); _scores[i].alignment = TextAnchor.MiddleRight;
                OwnerUiLayout.Place(_scores[i].rectTransform, 418, 7, 96, 54);
                _roles[i] = Ink(row, "RoleState", "", 28, false); _roles[i].alignment = TextAnchor.MiddleLeft;
                OwnerUiLayout.Place(_roles[i].rectTransform, 84, 41, 430, 36);
            }
        }
        private void BuildCourtClock()
        {
            _clockRoot = OwnerUiLayout.Rect(_root, "RoundClock");
            Pin(_clockRoot, new Vector2(.5f, 1), new Vector2(0, -82), new Vector2(620, 140));
            var face = OwnerUiLayout.Rect(_clockRoot, "ClockFace").gameObject.AddComponent<Image>();
            OwnerUiLayout.Place(face.rectTransform, 157, 0, 306, 92); face.color = new Color32(35, 29, 33, 218); face.raycastTarget = false;
            _clock = Ink(_clockRoot, "TimeLeft", "", 60, true); OwnerUiLayout.Place(_clock.rectTransform, 170, 0, 280, 89);
            _round = Ink(_clockRoot, "RoundLabel", "", 28, true); OwnerUiLayout.Place(_round.rectTransform, 70, 96, 480, 44);
        }
        private void BuildCourtCan()
        {
            _canRoot = OwnerUiLayout.Rect(_root, "CanReadout");
            Pin(_canRoot, new Vector2(1, 1), new Vector2(-253, -109), new Vector2(464, 170));
            var can = OwnerUiLayout.Rect(_canRoot, "CanStateIcon").gameObject.AddComponent<TumpSymbol>();
            can.Kind = TumpSymbol.Icon.Can; can.color = OwnerUiTheme.Current.Pale; can.raycastTarget = false;
            var edge = can.gameObject.AddComponent<Outline>(); edge.effectColor = UiTheme.InGameOutline; edge.effectDistance = new Vector2(2, -2);
            OwnerUiLayout.Place(can.rectTransform, 12, 5, 68, 84);
            _canState = Ink(_canRoot, "CanState", "", 32, true); _canState.alignment = TextAnchor.MiddleLeft;
            OwnerUiLayout.Place(_canState.rectTransform, 101, 7, 352, 75);
            _canHint = Ink(_canRoot, "CanHint", "", 28, false); _canHint.alignment = TextAnchor.UpperRight;
            OwnerUiLayout.Place(_canHint.rectTransform, 0, 103, 452, 67);
        }
        private void BuildCourtPersonal()
        {
            _personalRoot = OwnerUiLayout.Rect(_root, "LocalState");
            Pin(_personalRoot, new Vector2(0, 0), new Vector2(285, 122), new Vector2(500, 184));
            _role = Ink(_personalRoot, "LocalRole", "", 34, true); _role.alignment = TextAnchor.MiddleLeft;
            OwnerUiLayout.Place(_role.rectTransform, 0, 0, 500, 58);
            _stock = Ink(_personalRoot, "SlipperState", "", 28, false); _stock.alignment = TextAnchor.MiddleLeft;
            OwnerUiLayout.Place(_stock.rectTransform, 0, 62, 500, 54);
            var track = OwnerUiLayout.Rect(_personalRoot, "StaminaTrack").gameObject.AddComponent<Image>();
            OwnerUiLayout.Place(track.rectTransform, 0, 136, 338, 12); track.color = new Color32(35, 29, 33, 230); track.raycastTarget = false;
            _stamina = OwnerUiLayout.Rect(track.transform, "StaminaFill").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(_stamina.rectTransform); _stamina.color = CourtPresentationPalette.Gold; _stamina.raycastTarget = false;
            var label = Ink(_personalRoot, "StaminaLabel", "Stamina", 28, false); OwnerUiLayout.Place(label.rectTransform, 353, 121, 142, 42);
            // VISUAL-1.4: a stun the player cannot time is a stun they cannot play around
            // (`Design.md` § 11), so these stay; they sit under the reticle, where the eye
            // already is, instead of floating in the left margin under a scoreboard that moved.
            for (int i = 0; i < 4; i++)
            {
                _status[i] = Ink(_root, "TimedStatus" + i, "", 28, false); _status[i].alignment = TextAnchor.MiddleCenter;
                Pin(_status[i].rectTransform, new Vector2(.5f, .5f), new Vector2(0, -104 - i * 38), new Vector2(560, 38));
            }
        }
        private void BuildCourtPrompts()
        {
            _promptRoot = OwnerUiLayout.Rect(_root, "ContextualAction");
            Pin(_promptRoot, new Vector2(.5f, .32f), Vector2.zero, new Vector2(1100, 174));
            // VISUAL-1.4: the verb sits on a dark pill sized to its own words, the same plate as
            // the clock, so it reads over sky, chalk and asphalt alike.
            _promptPlate = OwnerUiLayout.Rect(_promptRoot, "PromptPlate").gameObject.AddComponent<HudCard>();
            _promptPlate.color = HudDraw.Plate; _promptPlate.Radius = 22; _promptPlate.raycastTarget = false;
            _promptPlate.enabled = false;
            _prompt = Ink(_promptRoot, "ActionPrompt", "", 32, true); OwnerUiLayout.Place(_prompt.rectTransform, 0, 0, 1100, 74);
            _context = Ink(_promptRoot, "ActionDetail", "", 28, false); OwnerUiLayout.Place(_context.rectTransform, 0, 77, 1100, 66);
            var track = OwnerUiLayout.Rect(_promptRoot, "RecoveryProgress").gameObject.AddComponent<Image>();
            OwnerUiLayout.Place(track.rectTransform, 320, 154, 460, 10); track.color = new Color32(35, 29, 33, 230); track.raycastTarget = false;
            _progress = OwnerUiLayout.Rect(track.transform, "ProgressFill").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(_progress.rectTransform); _progress.color = CourtPresentationPalette.Gold; _progress.raycastTarget = false;
            track.gameObject.SetActive(false);
        }
    }
}
