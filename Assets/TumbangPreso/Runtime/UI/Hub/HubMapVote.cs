using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// Five real courts, one vote per playing seat. Character lock-in has already finished.
    /// The map image and name are the target; the four portraits show the actual shared vote.
    /// No confirm button or local winner calculation: the host owns the decision and arena load.
    /// </summary>
    public sealed class HubMapVote : HubScreen
    {
        public override bool ShowsQueuePlate => false;
        public override float CourtShade => .9f;
        public override bool Back() => true;
        public override Selectable FirstFocus => _cards != null && _cards.Length > 0 ? _cards[0] : null;
        private HubButton[] _cards;
        private Text[] _counts, _names;
        private Image[,] _faces;
        private Text _clock, _heading, _instruction;
        private string _drawn = "";

        public override void Build()
        {
            HubPattern.Ground(Root, HubStyle.Night, 1101);
            _heading = HubChrome.Title(Root, "CHOOSE THE COURT");
            _clock = HubKit.Text(Root, "VoteClock", "12", HubStyle.Display, true, HubStyle.Golden, TextAnchor.MiddleRight);
            HubKit.Place(_clock.rectTransform, HubKit.TopRight, new Vector2(-HubKit.Margin, -HubKit.Margin), new Vector2(200, 110));
            _instruction = HubKit.Text(Root, "VoteInstruction", "Highest vote count wins.", HubStyle.Label, false, HubStyle.Honey, TextAnchor.MiddleLeft);
            HubKit.Place(_instruction.rectTransform, HubKit.TopLeft, new Vector2(HubKit.Margin, -180), new Vector2(1500, 62));

            int count = SceneFlow.MapRegistry.Length;
            _cards = new HubButton[count]; _counts = new Text[count]; _names = new Text[count]; _faces = new Image[count, Balance.PlayerCount];
            var row = HubKit.Place(HubKit.Rect(Root, "MapChoices"), HubKit.Centre, new Vector2(0, -15), new Vector2(count * 340 - 24, 480));
            for (int i = 0; i < count; i++)
            {
                int index = i;
                var map = SceneFlow.MapRegistry[i];
                var card = HubKit.Button(row, "VoteMap" + i, null, HubStyle.ArmyDeep, () => Hub.Host.VoteMap(index), 0, 1110 + i);
                _cards[i] = card;
                HubKit.Place((RectTransform)card.transform, HubKit.TopLeft, new Vector2(i * 340, 0), new Vector2(316, 480));
                var picture = HubKit.Rect(card.Body, "CourtImage").gameObject.AddComponent<RawImage>();
                picture.texture = Resources.Load<Texture2D>("UI/map-cards/" + map.Id);
                picture.raycastTarget = false;
                HubKit.Place(picture.rectTransform, HubKit.TopLeft, new Vector2(16, -16), new Vector2(284, 160));
                var name = HubKit.Text(card.Body, "CourtName", map.Name.ToUpperInvariant(), HubStyle.Title, true, HubStyle.Honey, TextAnchor.MiddleLeft);
                _names[i] = name;
                HubKit.Place(name.rectTransform, HubKit.TopLeft, new Vector2(22, -186), new Vector2(272, 132));
                _counts[i] = HubKit.Text(card.Body, "Votes", "", HubStyle.Floor, false, HubStyle.Honey, TextAnchor.MiddleLeft);
                HubKit.Place(_counts[i].rectTransform, HubKit.TopLeft, new Vector2(24, -330), new Vector2(268, 52));
                for (int seat = 0; seat < Balance.PlayerCount; seat++)
                {
                    var face = HubKit.Picture(card.Body, "Voter" + seat, null);
                    HubKit.Place(face.rectTransform, HubKit.BottomLeft, new Vector2(24 + seat * 64, 34), new Vector2(58, 58));
                    face.gameObject.SetActive(false);
                    _faces[i, seat] = face;
                }
                HubSlap.On(card.transform, i * .055f, i % 2 == 0 ? -1 : 1);
            }
            var tie = HubKit.Text(Root, "TieRule", "A tied vote favours a different court from the last one.", HubStyle.Floor, false, HubStyle.HoneySoft, TextAnchor.MiddleCenter);
            HubKit.Place(tie.rectTransform, HubKit.Bottom, new Vector2(0, HubKit.Margin), new Vector2(1600, 60));
            Draw();
        }

        public override void Tick()
        {
            if (!Hub.Host.InRoom) { Hub.Home(); return; }
            Draw();
        }

        private void Draw()
        {
            int winner = Hub.Host.MapVoteWinner;
            _clock.text = winner >= 0 ? "" : Mathf.CeilToInt(Hub.Host.MapVoteSecondsLeft).ToString();
            _heading.text = winner >= 0 ? "NEXT COURT" : "CHOOSE THE COURT";
            _instruction.text = winner >= 0 ? SceneFlow.MapRegistry[winner].Name.ToUpperInvariant()
                : Hub.Host.Spectating ? "The players are choosing the next court." : "Highest vote count wins.";
            var seats = Hub.Host.Seats();
            string key = winner + ":" + Hub.Host.Spectating;
            foreach (var seat in seats) key += ":" + seat.Occupied + ":" + seat.CharacterPick + ":" + Hub.Host.MapVoteFor(seat.Slot);
            if (key == _drawn) return;
            _drawn = key;
            for (int map = 0; map < _cards.Length; map++)
            {
                int votes = 0; bool mine = false;
                foreach (var seat in seats)
                {
                    bool here = seat.Occupied && Hub.Host.MapVoteFor(seat.Slot) == map;
                    var face = _faces[map, seat.Slot];
                    face.gameObject.SetActive(here);
                    if (!here) continue;
                    votes++; mine |= seat.Mine;
                    face.sprite = HubKit.Portrait(Roster.At(Roster.GetPeople(SceneFlow.SelectedMode), Mathf.Max(0, seat.CharacterPick))?.Id);
                }
                _counts[map].text = winner == map ? "NEXT UP" : mine ? "YOUR VOTE" : votes + (votes == 1 ? " VOTE" : " VOTES");
                HubKit.SetFill(_cards[map], winner == map ? HubStyle.Persimmon : mine ? HubStyle.Army : HubStyle.ArmyDeep);
                _counts[map].color = winner == map || mine ? HubStyle.Ink : HubStyle.Honey;
                _names[map].color = _counts[map].color;
                _cards[map].interactable = winner < 0 && !Hub.Host.Spectating;
            }
        }
    }
}
