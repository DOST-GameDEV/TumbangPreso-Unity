using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>Preview a court before explicitly submitting a host-owned ballot.</summary>
    public sealed class HubMapVote : HubScreen
    {
        public override bool ShowsQueuePlate=>false;
        public override float CourtShade=>0;
        public override bool Back()=>true;
        public override Selectable FirstFocus=>_cards!=null&&_cards.Length>0?_cards[0]:null;
        HubButton[] _cards;
        Text[] _counts;
        Image[,] _faces;
        RawImage _background;
        MapPreviewVideo _media;
        HubButton _confirm;
        Text _clock,_heading,_instruction,_mapName,_choice;
        int _candidate=-1;
        string _drawn="";

        public override void Build()
        {
            _background=HubKit.Rect(Root,"SelectedCourtBackground").gameObject.AddComponent<RawImage>();
            HubKit.Stretch(_background.rectTransform);_background.raycastTarget=false;
            var fit=_background.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;fit.aspectRatio=16f/9;
            _media=_background.gameObject.AddComponent<MapPreviewVideo>();
            Scrim("TopReadability",new Vector2(0,.78f),Vector2.one,.48f);
            Scrim("BottomReadability",Vector2.zero,new Vector2(1,.42f),.76f);
            _heading=HubChrome.Title(Root,"CHOOSE THE COURT");
            _clock=HubKit.Text(Root,"VoteClock","12",HubStyle.Display,true,HubStyle.Golden,TextAnchor.MiddleRight);
            HubKit.Place(_clock.rectTransform,HubKit.TopRight,new Vector2(-HubKit.Margin,-HubKit.Margin),new Vector2(200,110));
            _instruction=HubKit.Text(Root,"VoteInstruction","Browse a court, then lock your vote.",HubStyle.Label,false,HubStyle.Paper,TextAnchor.MiddleLeft);
            HubKit.Place(_instruction.rectTransform,HubKit.TopLeft,new Vector2(HubKit.Margin,-178),new Vector2(1420,64));
            _mapName=HubKit.Text(Root,"SelectedCourtName","",HubStyle.Hero,true,HubStyle.Paper,TextAnchor.MiddleLeft);
            HubKit.Place(_mapName.rectTransform,HubKit.TopLeft,new Vector2(HubKit.Margin,-330),new Vector2(1320,160));
            _choice=HubKit.Text(Root,"BallotStatus","",HubStyle.Label,false,HubStyle.Paper,TextAnchor.MiddleLeft);
            HubKit.Place(_choice.rectTransform,HubKit.BottomLeft,new Vector2(HubKit.Margin,330),new Vector2(1100,62));
            int count=SceneFlow.MapRegistry.Length;_cards=new HubButton[count];_counts=new Text[count];_faces=new Image[count,Balance.PlayerCount];
            const float width=226,gap=16;float rowWidth=count*(width+gap)-gap;
            var row=HubKit.Place(HubKit.Rect(Root,"MapChoices"),HubKit.Bottom,new Vector2(0,112),new Vector2(rowWidth,200));
            if(rowWidth>1800)row.localScale=Vector3.one*(1800/rowWidth);
            for(int i=0;i<count;i++)
            {
                int index=i;var map=SceneFlow.MapRegistry[i];
                var card=HubKit.Button(row,"VoteMap"+i,null,HubStyle.Night,()=>Browse(index),0,1110+i);_cards[i]=card;
                card.Attention+=attended=>{if(attended)Browse(index);};
                HubKit.Place((RectTransform)card.transform,HubKit.TopLeft,new Vector2(i*(width+gap),0),new Vector2(width,200));
                var picture=HubKit.Rect(card.Body,"CourtImage").gameObject.AddComponent<RawImage>();
                picture.texture=MapPreviewVideo.PosterFor(SceneFlow.Maps[i])??Resources.Load<Texture2D>("UI/map-cards/"+map.Id);picture.raycastTarget=false;
                HubKit.Place(picture.rectTransform,HubKit.TopLeft,new Vector2(12,-12),new Vector2(202,113.625f));
                var name=HubKit.Text(card.Body,"CourtName",map.Name.ToUpperInvariant(),HubStyle.Floor,true,HubStyle.Paper,TextAnchor.MiddleLeft);
                HubKit.Place(name.rectTransform,HubKit.TopLeft,new Vector2(12,-128),new Vector2(202,36));HubKit.Fit(name);
                _counts[i]=HubKit.Text(card.Body,"Votes","",HubStyle.Floor,false,HubStyle.Honey,TextAnchor.MiddleLeft);
                HubKit.Place(_counts[i].rectTransform,HubKit.TopLeft,new Vector2(12,-162),new Vector2(202,38));
                for(int seat=0;seat<Balance.PlayerCount;seat++)
                {
                    var face=HubKit.Picture(card.Body,"Voter"+seat,null);_faces[i,seat]=face;
                    HubKit.Place(face.rectTransform,HubKit.BottomLeft,new Vector2(118+seat*24,80),new Vector2(22,22));face.gameObject.SetActive(false);
                }
            }
            _confirm=HubKit.Button(Root,"LockMapVote","LOCK VOTE",HubStyle.Golden,Confirm,HubStyle.Title,1140);
            HubKit.Place((RectTransform)_confirm.transform,HubKit.BottomRight,new Vector2(-HubKit.Margin,338),new Vector2(430,100));
            var tie=HubKit.Text(Root,"TieRule","Ties favour a different court from the last one.",HubStyle.Floor,false,HubStyle.Paper,TextAnchor.MiddleCenter);
            HubKit.Place(tie.rectTransform,HubKit.Bottom,new Vector2(0,52),new Vector2(1500,44));
            Browse(0);Draw();
        }
        void Scrim(string name,Vector2 min,Vector2 max,float alpha)
        {
            var image=HubKit.Rect(Root,name).gameObject.AddComponent<Image>();image.color=new Color(HubStyle.Ink.r,HubStyle.Ink.g,HubStyle.Ink.b,alpha);image.raycastTarget=false;
            HubKit.Span(image.rectTransform,min,max);
        }
        void Browse(int index)
        {
            if(index<0||index>=_cards.Length||index==_candidate||Hub.Host.MapVoteWinner>=0)return;
            _candidate=index;ShowBackground(index);_drawn="";Draw();
        }
        void ShowBackground(int index)
        {
            if(!_media.Show(SceneFlow.Maps[index])){_media.Stop();_background.texture=Resources.Load<Texture2D>("UI/map-cards/"+SceneFlow.MapRegistry[index].Id);}
            _mapName.text=SceneFlow.MapRegistry[index].Name.ToUpperInvariant();HubKit.Fit(_mapName);
        }
        void Confirm()
        {
            if(_candidate<0||Hub.Host.Spectating||Hub.Host.MapVoteWinner>=0)return;
            Hub.Host.VoteMap(_candidate);_drawn="";Draw();
        }
        public override void Tick(){if(!Hub.Host.InRoom){Hub.Home();return;}Draw();}
        void Draw()
        {
            int winner=Hub.Host.MapVoteWinner;
            _clock.text=winner>=0?"":Mathf.CeilToInt(Hub.Host.MapVoteSecondsLeft).ToString();
            _heading.text=winner>=0?"NEXT COURT":"CHOOSE THE COURT";
            _instruction.text=winner>=0?"The host has chosen the next court.":Hub.Host.Spectating?"The players are choosing. Browse any court to preview it.":"Browse a court, then lock your vote.";
            var seats=Hub.Host.Seats();int mine=-1;string key=winner+":"+Hub.Host.Spectating+":"+_candidate;
            foreach(var seat in seats){key+=":"+seat.Occupied+":"+seat.CharacterPick+":"+Hub.Host.MapVoteFor(seat.Slot);if(seat.Mine&&seat.Occupied)mine=Hub.Host.MapVoteFor(seat.Slot);}
            if(key==_drawn)return;_drawn=key;
            if(winner>=0&&winner<_cards.Length&&_candidate!=winner){_candidate=winner;ShowBackground(winner);}
            _choice.text=mine>=0?"YOUR VOTE · "+SceneFlow.MapRegistry[mine].Name.ToUpperInvariant():Hub.Host.Spectating?"SPECTATING":"No vote locked yet.";
            _confirm.interactable=winner<0&&!Hub.Host.Spectating&&_candidate>=0&&mine!=_candidate;
            HubKit.SetLabel(_confirm,mine>=0?mine==_candidate?"VOTE LOCKED":"CHANGE VOTE":"LOCK VOTE");
            for(int map=0;map<_cards.Length;map++)
            {
                int votes=0;
                foreach(var seat in seats)
                {
                    bool here=seat.Occupied&&Hub.Host.MapVoteFor(seat.Slot)==map;var face=_faces[map,seat.Slot];face.gameObject.SetActive(here);
                    if(!here)continue;votes++;face.sprite=HubKit.Portrait(Roster.At(Roster.GetPeople(SceneFlow.SelectedMode),Mathf.Max(0,seat.CharacterPick))?.Id);
                }
                _counts[map].text=votes+(votes==1?" VOTE":" VOTES");
                HubKit.SetFill(_cards[map],winner==map?HubStyle.ArmyDeep:_candidate==map?HubStyle.ArmyDeep:HubStyle.Night);
                _cards[map].interactable=winner<0;
            }
        }
    }
}
