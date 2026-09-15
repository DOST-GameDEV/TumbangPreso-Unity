using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class CustomGameScreen
    {
        private readonly Dictionary<string,Text> _ownerValues=new Dictionary<string,Text>();
        private readonly Dictionary<string,GameObject> _ownerRuleRows=new Dictionary<string,GameObject>();
        private RectTransform _ownerMatchPage,_ownerRoomPage;
        private Button _ownerMatchTab,_ownerRoomTab,_ownerReset;
        private bool _ownerRules;
        public event Action RulesChanged;

        private void BuildPreviousPaintedRules()
        {
            _ownerRules=true;
            _canvas=OwnerUiLayout.Canvas(transform,"OwnerCustomGameCanvas",SortingOrder);
            _root=OwnerUiLayout.Rect(_canvas.transform,"CustomGameRoot").gameObject;
            OwnerUiLayout.Fill((RectTransform)_root.transform);OwnerUiBackdrop.Build(_root.transform);
            var design=OwnerUiLayout.DesignArea(_root.transform,"CustomRulesComposition");
            OwnerTextAction.Create(design,"CustomRulesBack","BACK",Close,57,24,170,70,30);
            var title=OwnerUiLayout.Text(design,"CustomRulesTitle","CUSTOM MATCH",60,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(title.rectTransform,96,107,1700,105);
            _headline=OwnerUiLayout.Text(design,"RulesHeadline","",30);
            OwnerUiLayout.Place(_headline.rectTransform,98,215,1725,74);_headline.color=OwnerUiTheme.Current.EnteredInk;
            _ownerMatchTab=OwnerTextAction.Create(design,"MatchRulesTab","THE MATCH",()=>ShowOwnerRulesPage(false),99,296,380,72,32);
            _ownerRoomTab=OwnerTextAction.Create(design,"RoomRulesTab","THE ROOM",()=>ShowOwnerRulesPage(true),522,296,380,72,32);
            foreach(var tab in new[]{_ownerMatchTab,_ownerRoomTab})
            {
                var stroke=OwnerUiLayout.Art(tab.transform,"SelectedTab",OwnerUiTheme.Piece.LeftRule);
                OwnerUiLayout.Place(stroke.rectTransform,68,64,242,6);
            }
            var paper=OwnerUiLayout.Rect(design,"RulesPaper").gameObject.AddComponent<OwnerUiPaper>();
            OwnerUiLayout.Place(paper.rectTransform,94,391,1732,487);paper.raycastTarget=false;
            _ownerMatchPage=OwnerUiLayout.Rect(design,"MatchRulesPage");OwnerUiLayout.Place(_ownerMatchPage,126,413,1670,436);
            _ownerRoomPage=OwnerUiLayout.Rect(design,"RoomRulesPage");OwnerUiLayout.Place(_ownerRoomPage,126,413,1670,436);
            Rule(_ownerMatchPage,"Format","FORMAT",0,d=>{_editing.Format=(MatchFormat)Cycle((int)_editing.Format,d,FormatCount);Apply();},
                "Choose how the round is played.");
            Rule(_ownerMatchPage,"Mode","MODE",72,d=>SetMode(_editing.Mode==GameMode.Classic?GameMode.HeroStrike:GameMode.Classic),
                "Classic street play or Hero Strike powers.");
            Rule(_ownerMatchPage,"Rounds","ROUNDS",144,d=>{_editing.Rounds=CustomGameRules.MinRounds+Cycle(_editing.Rounds-CustomGameRules.MinRounds,d,RoundOptionCount);Apply();},
                "How many rounds make up this match.");
            Rule(_ownerMatchPage,"Seconds","ROUND LENGTH",216,d=>{_editing.RoundSeconds=SecondsOptions[Cycle(SecondsIndex(_editing.RoundSeconds),d,SecondsOptions.Length)];Apply();},
                "Time allowed for each round.");
            Rule(_ownerMatchPage,"Target","SCORE TARGET",288,d=>{_editing.ScoreTarget=TargetOptions[Cycle(TargetIndex(_editing.ScoreTarget),d,TargetOptions.Length)];Apply();},
                "Reach this score to finish early. OFF plays every round.");
            Rule(_ownerMatchPage,"Stock","SLIPPERS EACH",360,d=>{_editing.Tsinelas=CustomGameRules.MinTsinelas+Cycle(_editing.Tsinelas-CustomGameRules.MinTsinelas,d,TsinelasOptionCount);Apply();},
                "Starting slippers in Last Tsinelas Standing.");
            Rule(_ownerRoomPage,"Bots","BOTS",0,d=>SetBots(Cycle(BotIndex(_editing),d,DifficultyCount+1)),
                "Fill empty seats, or choose NONE to play with people only.");
            Rule(_ownerRoomPage,"Private","PRIVATE ROOM",98,d=>{_editing.Private=!_editing.Private;Apply();},
                "Hide this room from the online room list.");
            _passwordRow=OwnerUiLayout.Rect(_ownerRoomPage,"PasswordRow").gameObject;
            OwnerUiLayout.Place((RectTransform)_passwordRow.transform,22,239,1600,142);
            _password=OwnerUiEntry.Create(_passwordRow.transform,"RoomPassword","ROOM PASSWORD",OwnerUiTheme.Piece.ThirdField,OwnerUiGlyph.Mark.Lock,true);
            OwnerUiLayout.Place((RectTransform)_password.transform,0,0,533,77);
            _password.characterLimit=CustomGameRules.MaxPasswordLength;
            _password.onValueChanged.AddListener(value=>{if(_editing==null || !MayEdit)return;_editing.Password=value??"";Apply();});
            var passwordHint=OwnerUiLayout.Text(_passwordRow.transform,"PasswordHelp","Optional. Use 4 to 16 characters.",27);
            OwnerUiLayout.Place(passwordHint.rectTransform,599,10,918,60);passwordHint.color=OwnerUiTheme.Current.EnteredInk;
            _ranked=OwnerUiLayout.Text(design,"RankedRulesNote","Custom rooms do not enter ranked matchmaking.",27);
            OwnerUiLayout.Place(_ranked.rectTransform,106,902,1070,48);_ranked.color=OwnerUiTheme.Current.EnteredInk;
            _refusal=OwnerUiLayout.Text(design,"RulesRefusal","",26,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_refusal.rectTransform,106,959,1118,60);_refusal.color=OwnerUiTheme.Current.HintInk;
            _ownerReset=OwnerTextAction.Create(design,"ResetRulesButton","RESET TO DEFAULTS",OnReset,102,1014,420,52,27);
            _use=OwnerPaintedAction.Create(design,"UseRulesButton","DONE",OnUse,false,52);
            OwnerUiLayout.Place((RectTransform)_use.transform,1376,951,413,91);
            _root.SetActive(false);
        }
        private void Rule(Transform parent,string id,string title,float y,Action<int> change,string hint)
        {
            var row=OwnerUiLayout.Rect(parent,id+"Rule");OwnerUiLayout.Place(row,0,y,1650,72);_ownerRuleRows[id]=row.gameObject;
            var caption=OwnerUiLayout.Text(row,id+"Label",title,27,OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place(caption.rectTransform,18,0,370,62);
            var value=OwnerUiLayout.Text(row,id+"Value","",29,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(value.rectTransform,455,0,407,62);value.alignment=TextAnchor.MiddleCenter;_ownerValues[id]=value;
            var detail=OwnerUiLayout.Text(row,id+"Help",hint,26);detail.color=OwnerUiTheme.Current.EnteredInk;
            OwnerUiLayout.Place(detail.rectTransform,961,0,662,62);
            foreach(int direction in new[]{-1,1})
            {
                int delta=direction;
                var button=OwnerTextAction.Create(row,id+(delta<0?"Previous":"Next"),"",()=>{if(MayEdit)change(delta);},delta<0?386:867,2,64,60);
                var arrow=OwnerUiGlyph.Create(button.transform,"Arrow",OwnerUiGlyph.Mark.Back,OwnerUiTheme.Current.ActionInk);
                OwnerUiLayout.Place(arrow.rectTransform,17,16,31,28);
                if(delta>0)arrow.rectTransform.localScale=new Vector3(-1,1,1);
            }
        }
        private static int Cycle(int value,int delta,int count)=>(value+delta+count)%count;
        private void ShowOwnerRulesPage(bool room){_roomOpen=room;RefreshOwnerRules();}
        private void RefreshOwnerRules()
        {
            if(!_ownerRules || _editing==null)return;
            _ownerMatchPage.gameObject.SetActive(!_roomOpen);_ownerRoomPage.gameObject.SetActive(_roomOpen);
            _ownerMatchTab.GetComponentInChildren<Text>().color=!_roomOpen?OwnerUiTheme.Current.Green:OwnerUiTheme.Current.ActionInk;
            _ownerRoomTab.GetComponentInChildren<Text>().color=_roomOpen?OwnerUiTheme.Current.Green:OwnerUiTheme.Current.ActionInk;
            _ownerMatchTab.transform.Find("SelectedTab").gameObject.SetActive(!_roomOpen);
            _ownerRoomTab.transform.Find("SelectedTab").gameObject.SetActive(_roomOpen);
            _ownerValues["Format"].text=CustomGameRules.FormatName(_editing.Format);
            _ownerValues["Mode"].text=_editing.Mode==GameMode.Classic?"CLASSIC":"HERO STRIKE";
            _ownerValues["Rounds"].text=_editing.Rounds.ToString();_ownerValues["Seconds"].text=_editing.RoundSeconds+"s";
            _ownerValues["Target"].text=ScoreTargetLabel(_editing.ScoreTarget);_ownerValues["Stock"].text=_editing.Tsinelas.ToString();
            _ownerRuleRows["Stock"].SetActive(_editing.Format==MatchFormat.LastTsinelas);
            _ownerValues["Bots"].text=BotLabel(_editing);_ownerValues["Private"].text=_editing.Private?"ON":"OFF";
            _passwordRow.SetActive(_editing.Private && MayEdit);
            _headline.text=(_editing.Mode==GameMode.Classic?"CLASSIC":"HERO STRIKE")+" · "+CustomGameRules.FormatName(_editing.Format)+
                " · "+_editing.Rounds+" rounds · "+_editing.RoundSeconds+"s"+(!MayEdit?"  (the host sets these)":"");
            foreach(var page in new[]{_ownerMatchPage,_ownerRoomPage})
                foreach(var selectable in page.GetComponentsInChildren<Selectable>(true))selectable.interactable=MayEdit;
            string refusal=CustomGameRules.Refusal(_editing);_refusal.text=refusal;
            _use.GetComponentInChildren<Text>().text=MayEdit?"DONE":"CLOSE";
            _use.interactable=!MayEdit || string.IsNullOrEmpty(refusal);_ownerReset.interactable=MayEdit;
            _canvas.GetComponent<InputLayer.ScreenFocus>().Rebuild();
        }
    }
}
