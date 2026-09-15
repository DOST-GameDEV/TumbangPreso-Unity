using System;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class OwnerPreparationView : MonoBehaviour
    {
        public Canvas Canvas { get; private set; }
        public MapPreviewSurface Preview { get; private set; }
        public Button Primary, StartMatch, JoinRoom, Online, Spectate, CustomRules,Chat,CopyCode,CopyAddress;
        public Button MapPrevious,MapNext,ModePrevious,ModeNext,BotsPrevious,BotsNext;
        public Text Heading,MapName,ModeName,BotsName,Status,RoomCode,RoomAddress,ProfileName,LoadoutName;
        public Text RankedTitle,RankedDetail,RulesSummary;
        private Text _previewStatus;
        public GameObject MatchChoices,RankedSummary;
        private readonly Button[] _routes=new Button[3];
        public readonly Button[] Seats=new Button[4];
        public readonly Image[] Portraits=new Image[4];
        public readonly Text[] SeatLabels=new Text[4];
        public RectTransform QueueHost;
        private void BuildPreviousPainted(Transform owner,Action back,Action primary,Action start,Action join,Action online,
            Action spectate,Action custom,Action loadout,Action profile,Action settings,
            Action<int> map,Action<int> mode,Action<int> bots,Action<int> seat,Action copyCode,Action copyAddress,Action<LobbyMode> route,Action chat)
        {
            Canvas=OwnerUiLayout.Canvas(owner,"OwnerPreparationCanvas",100);OwnerUiBackdrop.Build(Canvas.transform);
            var root=OwnerUiLayout.DesignArea(Canvas.transform,"PreparationComposition");
            OwnerTextAction.Create(root,"BackButton","BACK",back,52,25,170,70,30);
            Chat=OwnerTextAction.Create(root,"ChatButton","CHAT",chat,316,25,195,70,28);
            Heading=OwnerUiLayout.Text(root,"PreparationHeading","YOUR MATCH",58,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(Heading.rectTransform,88,111,1180,100);
            _routes[0]=OwnerTextAction.Create(root,"PracticeRoute","OFFLINE",()=>route(LobbyMode.Practice),714,118,236,59,25);
            _routes[1]=OwnerTextAction.Create(root,"FriendsRoute","FRIENDS",()=>route(LobbyMode.Custom),971,118,236,59,25);
            _routes[2]=OwnerTextAction.Create(root,"RankedRoute","RANKED",()=>route(LobbyMode.Ranked),1228,118,236,59,25);
            foreach(var tab in _routes)
            {
                var rule=OwnerUiLayout.Art(tab.transform,"SelectedRoute",OwnerUiTheme.Piece.LeftRule);
                OwnerUiLayout.Place(rule.rectTransform,26,54,184,5);
            }
            var account=OwnerTextAction.Create(root,"ProfileButton","YOUR PROFILE",profile,1453,29,376,62,27);
            ProfileName=account.GetComponentInChildren<Text>();
            OwnerTextAction.Create(root,"SettingsButton","SETTINGS",settings,1554,109,274,64,29);

            var mapSheet=OwnerUiLayout.Rect(root,"ArenaSheet").gameObject.AddComponent<OwnerUiPaper>();
            OwnerUiLayout.Place(mapSheet.rectTransform,83,236,1068,666);mapSheet.raycastTarget=false;
            MapName=OwnerUiLayout.Text(root,"MapName","",36,OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place(MapName.rectTransform,176,252,882,69);MapName.alignment=TextAnchor.MiddleCenter;
            MapPrevious=Arrow(root,"MapPrevButton",map,-1,104,259);
            MapNext=Arrow(root,"MapNextButton",map,1,1067,259);
            var preview=OwnerUiLayout.Rect(root,"MapPreview").gameObject.AddComponent<RawImage>();
            // MapPreviewSurface renders 16:9. Match its geometry instead of stretching the scene.
            OwnerUiLayout.Place(preview.rectTransform,103,313,1028,578.25f);preview.raycastTarget=false;
            Preview=preview.gameObject.AddComponent<MapPreviewSurface>();
            _previewStatus=OwnerUiLayout.Text(root,"MapLoading","GETTING THE ARENA READY...",30,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_previewStatus.rectTransform,141,520,939,108);_previewStatus.alignment=TextAnchor.MiddleCenter;

            var choices=OwnerUiLayout.Rect(root,"MatchChoices");OwnerUiLayout.Fill(choices);MatchChoices=choices.gameObject;
            ModeName=SettingRow(choices,"Mode","MODE",321,out ModePrevious,out ModeNext,mode);
            BotsName=SettingRow(choices,"Bots","BOTS",465,out BotsPrevious,out BotsNext,bots);
            CustomRules=OwnerTextAction.Create(choices,"CustomGameButton","CUSTOM SETTINGS",custom,1240,583,544,62,28);
            RulesSummary=OwnerUiLayout.Text(choices,"RulesSummary","",27);
            OwnerUiLayout.Place(RulesSummary.rectTransform,1233,648,552,72);RulesSummary.color=OwnerUiTheme.Current.EnteredInk;
            RulesSummary.alignment=TextAnchor.UpperCenter;
            Spectate=OwnerTextAction.Create(choices,"SpectateButton","WATCH INSTEAD",spectate,1240,753,544,62,28);
            var ranked=OwnerUiLayout.Rect(root,"RankedSummary");OwnerUiLayout.Fill(ranked);RankedSummary=ranked.gameObject;
            RankedTitle=OwnerUiLayout.Text(ranked,"RankedTitle","",44,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(RankedTitle.rectTransform,1238,289,550,84);
            RankedDetail=OwnerUiLayout.Text(ranked,"RankedDetail","",29);
            OwnerUiLayout.Place(RankedDetail.rectTransform,1240,379,540,115);RankedDetail.color=OwnerUiTheme.Current.EnteredInk;

            for(int i=0;i<4;i++)
            {
                int index=i;float x=102+i*254;
                var box=OwnerUiLayout.Rect(root,"SeatButton"+i);OwnerUiLayout.Place(box,x,911,238,100);
                var hit=box.gameObject.AddComponent<Image>();hit.color=Color.clear;
                var button=box.gameObject.AddComponent<OwnerTextAction>();button.targetGraphic=hit;button.transition=Selectable.Transition.None;
                button.onClick.AddListener(()=>seat(index));Seats[i]=button;
                Portraits[i]=OwnerPortraitArt.Create(box,"SeatPortrait","UI/portraits/bayan");
                OwnerUiLayout.Place(Portraits[i].rectTransform,25,2,72,74);
                SeatLabels[i]=OwnerUiLayout.Text(box,"SeatLabel","",23,OwnerUiLayout.TypeRole.Display);
                OwnerUiLayout.Place(SeatLabels[i].rectTransform,106,0,130,88);SeatLabels[i].alignment=TextAnchor.MiddleLeft;
            }
            OwnerTextAction.Create(root,"LoadoutButton","CHANGE LOADOUT",loadout,111,1008,320,54,28);
            LoadoutName=OwnerUiLayout.Text(root,"LoadoutSummary","",26);
            OwnerUiLayout.Place(LoadoutName.rectTransform,1238,849,548,54);LoadoutName.color=OwnerUiTheme.Current.EnteredInk;
            LoadoutName.alignment=TextAnchor.MiddleCenter;
            Primary=OwnerPaintedAction.Create(root,"PrimaryButton","START MATCH",primary,false,44);
            OwnerUiLayout.Place((RectTransform)Primary.transform,1304,931,413,91);
            StartMatch=OwnerPaintedAction.Create(root,"StartButton","START MATCH",start,false,44);
            OwnerUiLayout.Place((RectTransform)StartMatch.transform,1304,931,413,91);
            JoinRoom=OwnerTextAction.Create(root,"JoinRoomButton","JOIN A ROOM",join,458,1008,320,54,29);
            Online=OwnerTextAction.Create(root,"OnlineRoomButton","GO ONLINE",online,805,1008,310,54,29);
            RoomCode=OwnerUiLayout.Text(root,"RoomCode","",26,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(RoomCode.rectTransform,109,193,542,44);
            CopyCode=OwnerTextAction.Create(root,"CopyRoomCode","COPY CODE",copyCode,696,192,218,44,23);
            RoomAddress=OwnerUiLayout.Text(root,"RoomAddress","",24);
            OwnerUiLayout.Place(RoomAddress.rectTransform,937,193,444,44);
            CopyAddress=OwnerTextAction.Create(root,"CopyRoomAddress","COPY ADDRESS",copyAddress,1462,192,352,44,23);
            Status=OwnerUiLayout.Text(root,"PreparationStatus","",23,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(Status.rectTransform,1238,812,548,45);Status.color=OwnerUiTheme.Current.HintInk;
            QueueHost=OwnerUiLayout.Rect(root,"RankedQueueDock");OwnerUiLayout.Place(QueueHost,1197,497,620,440);
            Canvas.GetComponent<InputLayer.ScreenFocus>().Rebuild();
        }
        private static Button Arrow(Transform parent,string name,Action<int> action,int direction,float x,float y)
        {
            var button=OwnerTextAction.Create(parent,name,"",()=>action(direction),x,y,64,64,28);
            var glyph=OwnerUiGlyph.Create(button.transform,"Arrow",OwnerUiGlyph.Mark.Back,OwnerUiTheme.Current.ActionInk);
            OwnerUiLayout.Place(glyph.rectTransform,13,15,37,34);
            if(direction>0)glyph.rectTransform.localScale=new Vector3(-1,1,1);return button;
        }
        private static Text SettingRow(Transform root,string name,string title,float y,out Button previous,out Button next,Action<int> change)
        {
            var frame=OwnerUiLayout.Art(root,name+"Field",name=="Mode"?OwnerUiTheme.Piece.FirstField:OwnerUiTheme.Piece.SecondField);
            OwnerUiLayout.Place(frame.rectTransform,1240,y-5,533,78);
            var caption=OwnerUiLayout.Text(root,name+"Caption",title,24,OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place(caption.rectTransform,1230,y-43,564,40);caption.alignment=TextAnchor.MiddleCenter;
            previous=Arrow(root,name+"PrevButton",change,-1,1246,y);
            next=Arrow(root,name+"NextButton",change,1,1701,y);
            var value=OwnerUiLayout.Text(root,name+"Value","",33,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(value.rectTransform,1290,y,432,66);value.alignment=TextAnchor.MiddleCenter;return value;
        }
        public void ShowRoom(bool shown)
        {
            RoomCode.gameObject.SetActive(shown);RoomAddress.gameObject.SetActive(shown);
            RoomCode.transform.parent.Find("CopyRoomCode").gameObject.SetActive(shown);
            RoomAddress.transform.parent.Find("CopyRoomAddress").gameObject.SetActive(shown);
        }
        public void SetPortrait(int seat,string resource)
        {
            var image=Portraits[seat];
            image.sprite=OwnerPortraitArt.Get(resource);image.enabled=image.sprite!=null;
            if(_emptySeatMarks[seat]!=null)_emptySeatMarks[seat].gameObject.SetActive(false);
        }
        public void SelectRoute(LobbyMode selected)
        {
            var kinds=new[]{LobbyMode.Practice,LobbyMode.Custom,LobbyMode.Ranked};
            for(int i=0;i<_routes.Length;i++)
            {
                bool active=kinds[i]==selected;
                _routes[i].GetComponentInChildren<Text>().color=active?OwnerUiTheme.Current.Lime:OwnerUiTheme.Current.Pale;
                _routes[i].transform.Find("SelectedRoute").gameObject.SetActive(active);
            }
            MatchChoices.SetActive(selected!=LobbyMode.Ranked);RankedSummary.SetActive(selected==LobbyMode.Ranked);
            MapPrevious.gameObject.SetActive(selected!=LobbyMode.Ranked);MapNext.gameObject.SetActive(selected!=LobbyMode.Ranked);
        }
        private void Update()
        {
            if(_previewStatus!=null)_previewStatus.gameObject.SetActive(Preview!=null && Preview.GetComponent<RawImage>().texture==null);
        }
        public void RebuildFocus()=>Canvas.GetComponent<InputLayer.ScreenFocus>().Rebuild();
        private void OnDisable(){if(Canvas!=null)Canvas.gameObject.SetActive(false);}
    }
}
