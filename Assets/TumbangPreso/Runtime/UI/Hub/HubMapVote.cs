using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>Preview a court before explicitly submitting a host-owned ballot.</summary>
    public sealed class HubMapVote : HubScreen
    {
        public override bool WearsBoard => true;
        public override bool ShowsQueuePlate=>false;
        public override float CourtShade=>0;
        public override bool Back()=>true;
        public override Selectable FirstFocus=>_cards!=null&&_cards.Length>0?_cards[0]:null;
        HubButton[] _cards;
        Text[] _counts,_names;
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
            var dim=HubKit.Stretch(HubKit.Rect(Root,"Dim")).gameObject.AddComponent<Image>();
            dim.color=new Color(.157f,.031f,.055f,.27f);dim.raycastTarget=false;
            _heading=HubKnit.Display(Root,"ScreenTitle","CHOOSE THE COURT",86,HubKnit.KitWhite,TextAnchor.MiddleLeft,7);
            HubKnit.At(_heading.rectTransform,60,35,1500,100);
            _instruction=HubKnit.Words(Root,"VoteInstruction","Browse a court, then lock your vote.",34,HubKnit.KitWhite,TextAnchor.MiddleLeft,true,3);
            HubKnit.At(_instruction.rectTransform,60,140,1420,60);
            var clockCard=HubKnit.At(HubKnit.Plate(Root,"VoteClockCard",Knit.Card),1680,30,180,144);
            _clock=HubKnit.Display(HubKnit.FaceOf(clockCard),"VoteClock","12",84,HubKnit.KitCream,TextAnchor.MiddleCenter);
            HubKnit.In(_clock.rectTransform,0,12,180,106);
            _mapName=HubKnit.Display(Root,"SelectedCourtName","",170,HubKnit.KitWhite,TextAnchor.MiddleLeft,9);
            HubKnit.At(_mapName.rectTransform,56,270,1320,210);
            _choice=HubKnit.Words(Root,"BallotStatus","",34,HubKnit.KitWhite,TextAnchor.MiddleLeft,true,3);
            HubKnit.At(_choice.rectTransform,62,526,1100,56);
            int count=SceneFlow.MapRegistry.Length;_cards=new HubButton[count];_counts=new Text[count];_names=new Text[count];_faces=new Image[count,Balance.PlayerCount];
            const float width=290,gap=12;float rowWidth=count*(width+gap)-gap;
            var row=HubKnit.At(HubKit.Rect(Root,"MapChoices"),960-rowWidth*.5f,640,rowWidth,264);
            if(rowWidth>1800){row.pivot=new Vector2(.5f,1);row.anchoredPosition=new Vector2(0,-100);row.localScale=Vector3.one*(1800/rowWidth);}
            for(int i=0;i<count;i++)
            {
                int index=i;var map=SceneFlow.MapRegistry[i];
                var card=HubKnit.Button(row,"VoteMap"+i,null,Knit.Card,()=>Browse(index));_cards[i]=card;
                card.Attention+=attended=>{if(attended)Browse(index);};
                HubKnit.In((RectTransform)card.transform,i*(width+gap),0,width,264);
                var picture=HubKit.Rect(card.Body,"CourtImage").gameObject.AddComponent<KitCutPicture>();
                picture.texture=MapPreviewVideo.PosterFor(SceneFlow.Maps[i])??Resources.Load<Texture2D>("UI/map-cards/"+map.Id);picture.raycastTarget=false;
                HubKnit.In(picture.rectTransform,20,18,250,140);
                _names[i]=HubKnit.Display(card.Body,"CourtName",map.Name.ToUpperInvariant(),30,HubKnit.KitCream,TextAnchor.MiddleCenter);
                HubKnit.In(_names[i].rectTransform,14,166,262,44);HubKit.Fit(_names[i],262);
                _counts[i]=HubKnit.Words(card.Body,"Votes","",HubStyle.Floor,HubKnit.KitTan,TextAnchor.MiddleCenter);
                HubKnit.In(_counts[i].rectTransform,14,204,262,36);
                for(int seat=0;seat<Balance.PlayerCount;seat++)
                {
                    var face=HubKit.Picture(card.Body,"Voter"+seat,null);_faces[i,seat]=face;
                    HubKnit.In(face.rectTransform,26+seat*34,124,32,32);face.gameObject.SetActive(false);
                }
            }
            _confirm=HubKnit.Button(Root,"LockMapVote","LOCK VOTE",Knit.Go,Confirm,54,HubGlyph.Mark.None,primary:true);
            HubKnit.At((RectTransform)_confirm.transform,1400,470,460,124);
            var tie=HubKnit.Words(Root,"TieRule","Ties favour a different court from the last one.",32,HubKnit.KitWhite,TextAnchor.MiddleCenter,true,3);
            HubKnit.At(tie.rectTransform,210,944,1500,52);
            Browse(0);Draw();
        }
        void Browse(int index)
        {
            if(index<0||index>=_cards.Length||index==_candidate||Hub.Host.MapVoteWinner>=0)return;
            _candidate=index;ShowBackground(index);_drawn="";Draw();
        }
        void ShowBackground(int index)
        {
            if(!_media.Show(SceneFlow.Maps[index])){_media.Stop();_background.texture=Resources.Load<Texture2D>("UI/map-cards/"+SceneFlow.MapRegistry[index].Id);}
            _mapName.text=SceneFlow.MapRegistry[index].Name.ToUpperInvariant();HubKit.Fit(_mapName,1320);
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
            HubKnit.Say(_confirm,mine>=0?mine==_candidate?"VOTE LOCKED":"CHANGE VOTE":"LOCK VOTE");
            for(int map=0;map<_cards.Length;map++)
            {
                int votes=0;
                foreach(var seat in seats)
                {
                    bool here=seat.Occupied&&Hub.Host.MapVoteFor(seat.Slot)==map;var face=_faces[map,seat.Slot];face.gameObject.SetActive(here);
                    if(!here)continue;votes++;face.sprite=HubKit.Portrait(Roster.At(Roster.GetPeople(SceneFlow.SelectedMode),Mathf.Max(0,seat.CharacterPick))?.Id);
                }
                _counts[map].text=votes+(votes==1?" VOTE":" VOTES");
                bool lit=winner==map||_candidate==map;
                HubKnit.Recolour(_cards[map].transform,lit?Knit.CardOn:Knit.Card);
                _names[map].color=lit?HubKnit.KitLime:HubKnit.KitCream;
                _cards[map].interactable=winner<0;
            }
        }
    }
}
