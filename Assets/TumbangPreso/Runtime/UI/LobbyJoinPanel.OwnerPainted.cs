using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class LobbyJoinPanel
    {
        private void ConstructPreviousPaintedJoin()
        {
            _nativeJoin=true;
            _nativeJoinCanvas=OwnerUiLayout.Canvas(transform,"OwnerJoinCanvas",830);
            OwnerUiBackdrop.Build(_nativeJoinCanvas.transform);
            var root=OwnerUiLayout.DesignArea(_nativeJoinCanvas.transform,"RoomBrowserComposition");
            OwnerTextAction.Create(root,"CloseJoinButton","BACK",Close,56,28,172,70,30);
            var title=OwnerUiLayout.Text(root,"JoinTitle","JOIN A ROOM",64,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(title.rectTransform,90,125,1650,100);
            var hint=OwnerUiLayout.Text(root,"JoinHint","Enter a friend's room code or address.",29);
            OwnerUiLayout.Place(hint.rectTransform,90,236,1650,54);hint.color=OwnerUiTheme.Current.EnteredInk;
            _entry=OwnerUiEntry.Create(root,"JoinCode","ROOM CODE OR ADDRESS",OwnerUiTheme.Piece.FirstField);
            _entry.characterLimit=128;_entry.onSubmit.AddListener(_=>Join());
            OwnerUiLayout.Place((RectTransform)_entry.transform,486,327,533,78);
            _nativeConnect=OwnerPaintedAction.Create(root,"ConnectToRoom","JOIN",Join,false,52);
            OwnerUiLayout.Place((RectTransform)_nativeConnect.transform,1069,321,413,91);
            _nearbyChip=OwnerTextAction.Create(root,"NearbyChip","NEARBY",()=>SetSource(false),125,440,300,70,32);
            _onlineChip=OwnerTextAction.Create(root,"OnlineChip","ONLINE",()=>SetSource(true),472,440,300,70,32);
            var sheet=OwnerUiLayout.Rect(root,"RoomsSheet").gameObject.AddComponent<OwnerUiPaper>();
            OwnerUiLayout.Place(sheet.rectTransform,96,545,1728,361);sheet.raycastTarget=false;
            _list=OwnerUiLayout.Rect(root,"RoomLists").gameObject;
            OwnerUiLayout.Place((RectTransform)_list.transform,134,574,1640,286);
            _nativeLanContent=OwnerScrollColumn.Build(_list.transform,"NearbyRooms",new Rect(0,0,1640,286),out var lanScroll);
            _nativeOnlineContent=OwnerScrollColumn.Build(_list.transform,"OnlineRooms",new Rect(0,0,1640,286),out var onlineScroll);
            _lanGroup=lanScroll.gameObject;_onlineGroup=onlineScroll.gameObject;
            _nativeJoinStatus=OwnerUiLayout.Text(root,"JoinStatus","",25,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_nativeJoinStatus.rectTransform,104,939,1110,93);_nativeJoinStatus.color=OwnerUiTheme.Current.HintInk;
            _leave=OwnerTextAction.Create(root,"LeaveGameButton","LEAVE CURRENT ROOM",Leave,1283,956,530,66,29);
            SetSource(false);RefreshNative();
            ScreenTakeover.Register(this,()=>_nativeJoinCanvas!=null && _nativeJoinCanvas.gameObject.activeInHierarchy);
        }
        private static void SelectPreviousPaintedSource(Button button,bool selected)
        {
            button.GetComponentInChildren<Text>().color=selected?OwnerUiTheme.Current.Green:OwnerUiTheme.Current.ActionInk;
            var mark=button.transform.Find("SelectedSource");
            if(mark==null)
            {
                var rule=OwnerUiLayout.Art(button.transform,"SelectedSource",OwnerUiTheme.Piece.LeftRule);
                OwnerUiLayout.Place(rule.rectTransform,29,59,242,6);mark=rule.transform;
            }
            mark.gameObject.SetActive(selected);
        }
        private void EnsurePreviousPaintedRows(RectTransform parent,List<Button> rows,List<Text> labels,int count,Action<int> selected)
        {
            while(rows.Count<Mathf.Max(1,count))
            {
                int index=rows.Count;var root=OwnerUiLayout.Rect(parent,"Room"+index);
                root.gameObject.AddComponent<LayoutElement>().preferredHeight=110;
                var hit=root.gameObject.AddComponent<Image>();hit.color=Color.clear;
                var button=root.gameObject.AddComponent<OwnerTextAction>();button.targetGraphic=hit;button.transition=Selectable.Transition.None;
                button.onClick.AddListener(()=>{MenuSfx.Click();selected(index);});
                var label=OwnerUiLayout.Text(root,"RoomTitle","",31,OwnerUiLayout.TypeRole.Display);
                OwnerUiLayout.Place(label.rectTransform,76,0,1420,58);
                var person=OwnerUiLayout.Art(root,"RoomIcon",OwnerUiTheme.Piece.Person);
                OwnerUiLayout.Place(person.rectTransform,18,19,28,29);
                var detail=OwnerUiLayout.Text(root,"RoomDetail","",26);
                OwnerUiLayout.Place(detail.rectTransform,77,58,1418,46);detail.color=OwnerUiTheme.Current.EnteredInk;
                rows.Add(button);labels.Add(label);
            }
        }
    }
}
