using System;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>One new defender portrait and the actual cumulative standings between rounds.</summary>
    public sealed partial class TumpRoundSwapView : MonoBehaviour
    {
        public Canvas Canvas { get; private set; }
        private Text _round, _name, _buffer, _continueLabel;
        private Image _portrait;
        private readonly Text[] _names = new Text[4], _scores = new Text[4];
        private readonly Image[] _portraits = new Image[4];
        private void BuildPrevious(Transform owner, Action dismiss)
        {
            var f = TumpUiTheme.Current;
            Canvas = TumpUiFactory.Canvas(owner, "TumpRoundSwapCanvas", 220);
            var hud = Hud.Instance;
            if (hud != null)
            {
                Canvas.transform.SetParent(hud.CleanFeedRoot, false);
                TumpUiFactory.Stretch((RectTransform)Canvas.transform);
            }
            var root = (RectTransform)Canvas.transform;
            TumpUiFactory.Ground(root, f.Cream);
            _round = TumpUiFactory.Text(root, "RoundHeadline", "", 64, true);
            _round.color = f.Brick; TumpUiFactory.Place(_round.rectTransform, 96, 54, 1700, 106);
            var next = TumpUiFactory.Text(root, "NextRole", "Next defender", 42, true);
            next.color = f.Brick; TumpUiFactory.Place(next.rectTransform, 120, 204, 700, 76);
            var stage = TumpUiFactory.Surface(root, "DefenderSpotlight", TumpSurface.Form.Disc, f.Yellow, false);
            TumpUiFactory.Place(stage.rectTransform, 92, 330, 646, 476);
            var rim = TumpUiFactory.Surface(root, "DefenderSpotlightInner", TumpSurface.Form.Disc, f.Apricot, false);
            TumpUiFactory.Place(rim.rectTransform, 142, 380, 546, 376);
            _portrait = TumpUiFactory.Art(root, "NextDefenderPortrait", null);
            TumpUiFactory.Place(_portrait.rectTransform, 124, 296, 540, 480);
            var floor = TumpUiFactory.Rect(root, "DefenderUnderline").gameObject.AddComponent<Image>();
            floor.color = f.HotOrange; floor.raycastTarget = false;
            TumpUiFactory.Place(floor.rectTransform, 126, 798, 620, 12);
            _name = TumpUiFactory.Text(root, "NextDefenderName", "", 44, true);
            TumpUiFactory.Place(_name.rectTransform, 126, 822, 650, 100);
            var standing = TumpUiFactory.Text(root, "StandingsTitle", "Standings", 46, true);
            standing.color = f.Brick; TumpUiFactory.Place(standing.rectTransform, 922, 214, 780, 78);
            for (int i = 0; i < 4; i++)
            {
                float y = 330 + i * 124;
                var place = TumpUiFactory.Text(root, "Place" + i, (i + 1).ToString(), 46, true);
                place.color = f.Brick; TumpUiFactory.Place(place.rectTransform, 900, y, 80, 84);
                _portraits[i] = TumpUiFactory.Art(root, "StandingPortrait" + i, null);
                TumpUiFactory.Place(_portraits[i].rectTransform, 990, y - 6, 96, 96);
                _names[i] = TumpUiFactory.Text(root, "StandingName" + i, "", 36, true);
                TumpUiFactory.Place(_names[i].rectTransform, 1110, y, 478, 90);
                _scores[i] = TumpUiFactory.Text(root, "StandingScore" + i, "", 46, true);
                _scores[i].alignment = TextAnchor.MiddleRight;
                TumpUiFactory.Place(_scores[i].rectTransform, 1608, y, 190, 90);
                var rule = TumpUiFactory.Rect(root, "StandingRule" + i).gameObject.AddComponent<Image>();
                rule.color = new Color(f.OliveSand.r, f.OliveSand.g, f.OliveSand.b, .55f); rule.raycastTarget = false;
                TumpUiFactory.Place(rule.rectTransform, 922, y + 104, 872, 2);
            }
            var go = TumpUiFactory.Button(root, "ContinueWarmup", "Continue warming up", dismiss, TumpSurface.Form.Pebble, f.Lime, 34);
            TumpUiFactory.Anchor((RectTransform)go.transform, new Vector2(1, 0), new Vector2(-380, 90), new Vector2(580, 90));
            _buffer = TumpUiFactory.Text(root, "WarmupTime", "", 26);
            TumpUiFactory.Place(_buffer.rectTransform, 104, 960, 710, 66);
        }
        public void Show(int nextRound, int defender)
        {
            _round.text = "Round " + Mathf.Max(1, nextRound - 1) + " complete";
            var who = GameServices.Round?.PlayerAt(defender);
            _name.text = who != null ? who.DisplayName() : "P" + (defender + 1);
            _portrait.sprite = Portrait(who); _portrait.enabled = _portrait.sprite != null;
            var match = GameServices.Match;
            if (match != null)
            {
                var order = match.Ranking();
                for (int i = 0; i < 4; i++)
                {
                    var actor = GameServices.Round?.PlayerAt(order[i]);
                    _names[i].text = actor != null ? actor.DisplayName() : "P" + (order[i] + 1);
                    _scores[i].text = match.ScoreFor(order[i]).ToString();
                    _portraits[i].sprite = Portrait(actor); _portraits[i].enabled = _portraits[i].sprite != null;
                }
            }
            Canvas.gameObject.SetActive(true); Canvas.GetComponent<InputLayer.ScreenFocus>().Rebuild();
        }
        private bool _halftime;private string _fallback;
        public void SetBreakContext(bool halftime,string fallback)
        {_halftime=halftime;_fallback=fallback;if(halftime)_round.text="HALFTIME  /  "+_round.text;
            if(_continueLabel!=null)_continueLabel.text=halftime?"HIDE STANDINGS":"KEEP WARMING UP";}
        public void Remaining(float seconds) => _buffer.text = (_fallback!=null?_fallback+" · ":_halftime?"Back to the court · ":"Next round · ") + Mathf.CeilToInt(seconds) + "s";
        private static Sprite Portrait(CharacterMotor actor)
        {
            if (actor == null) return null;
            var people = Roster.GetPeople(actor.Mode);
            return actor.CharacterIndex >= 0 && actor.CharacterIndex < people.Count
                ? OwnerPortraitArt.Get("UI/portraits/" + people[actor.CharacterIndex].Id) : null;
        }
    }
}
