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
            BuildCourtScores(); BuildCourtClock(); BuildCourtCan(); BuildCourtPersonal(); BuildCourtPrompts();
            MatchEventFeed.Create(_root);
            MatchMomentBanner.Create(_root);
            _powers = gameObject.AddComponent<TumpPowerReadout>(); _powers.Build(_root);
            _toast = Ink(_root, "MatchToast", "", 36, true);
            Pin(_toast.rectTransform, new Vector2(.5f, 1), new Vector2(0, -191), new Vector2(1080, 78)); _toast.enabled = false;
            _countdown = Ink(_root, "Countdown", "", 108, true);
            Pin(_countdown.rectTransform, new Vector2(.5f, .58f), Vector2.zero, new Vector2(740, 180)); _countdown.enabled = false;
            _crosshair = Ink(_root, "Reticle", "+", 34, false);
            Pin(_crosshair.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(76, 76));
            _hit = Ink(_root, "HitConfirmation", "\u00d7", 72, true);
            Pin(_hit.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(120, 120)); _hit.enabled = false;
            _spectator = Ink(_root, "SpectatorReadout", "", 28, false);
            Pin(_spectator.rectTransform, new Vector2(.5f, 0), new Vector2(0, 106), new Vector2(1460, 146));
            _sandbox = Ink(_root, "SandboxState", "", 28, false);
            Pin(_sandbox.rectTransform, new Vector2(1, 0), new Vector2(-286, 71), new Vector2(550, 54));
            var version = Ink(_root, "GameVersion", "", 28, false);
            Pin(version.rectTransform, new Vector2(1, 0), new Vector2(-230, 22), new Vector2(420, 40)); GameVersion.ApplyTo(version);
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
            _round = Ink(_clockRoot, "RoundLabel", "", 28, true); OwnerUiLayout.Place(_round.rectTransform, 0, 96, 620, 44);
        }
        private void BuildCourtCan()
        {
            _canRoot = OwnerUiLayout.Rect(_root, "CanReadout");
            Pin(_canRoot, new Vector2(1, 1), new Vector2(-253, -109), new Vector2(464, 170));
            var can = OwnerUiLayout.Rect(_canRoot, "CanStateIcon").gameObject.AddComponent<TumpSymbol>();
            can.Kind = TumpSymbol.Icon.Can; can.color = OwnerUiTheme.Current.Pale; can.raycastTarget = false;
            var edge = can.gameObject.AddComponent<Outline>(); edge.effectColor = OwnerUiTheme.Current.DeepInk; edge.effectDistance = new Vector2(2, -2);
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
            OwnerUiLayout.Place(track.rectTransform, 0, 136, 338, 12); track.color = new Color32(25, 40, 31, 230); track.raycastTarget = false;
            _stamina = OwnerUiLayout.Rect(track.transform, "StaminaFill").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(_stamina.rectTransform); _stamina.color = OwnerUiTheme.Current.Lime; _stamina.raycastTarget = false;
            var label = Ink(_personalRoot, "StaminaLabel", "Stamina", 28, false); OwnerUiLayout.Place(label.rectTransform, 353, 121, 142, 42);
            for (int i = 0; i < 4; i++)
            {
                _status[i] = Ink(_root, "TimedStatus" + i, "", 28, false); _status[i].alignment = TextAnchor.MiddleLeft;
                OwnerUiLayout.Place(_status[i].rectTransform, 38, 369 + i * 47, 500, 45);
            }
        }
        private void BuildCourtPrompts()
        {
            _promptRoot = OwnerUiLayout.Rect(_root, "ContextualAction");
            Pin(_promptRoot, new Vector2(.5f, .32f), Vector2.zero, new Vector2(1100, 174));
            _prompt = Ink(_promptRoot, "ActionPrompt", "", 36, true); OwnerUiLayout.Place(_prompt.rectTransform, 0, 0, 1100, 74);
            _context = Ink(_promptRoot, "ActionDetail", "", 28, false); OwnerUiLayout.Place(_context.rectTransform, 0, 77, 1100, 66);
            var track = OwnerUiLayout.Rect(_promptRoot, "RecoveryProgress").gameObject.AddComponent<Image>();
            OwnerUiLayout.Place(track.rectTransform, 320, 154, 460, 10); track.color = new Color32(25, 40, 31, 230); track.raycastTarget = false;
            _progress = OwnerUiLayout.Rect(track.transform, "ProgressFill").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(_progress.rectTransform); _progress.color = OwnerUiTheme.Current.Lime; _progress.raycastTarget = false;
            track.gameObject.SetActive(false);
        }
    }
}
