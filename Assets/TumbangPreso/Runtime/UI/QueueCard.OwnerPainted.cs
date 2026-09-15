using UnityEngine;
using UnityEngine.UI;
using TumbangPreso.Core;
using TumbangPreso.Net;

namespace TumbangPreso.UI
{
    public sealed partial class QueueCard
    {
        private void ConstructNativeQueue()
        {
            _nativeQueue=true;_queue=Matchmaker.Ensure();
            _queue.Changed+=Refresh;_queue.Joined+=OnQueueJoined;
            if(!_docked)
            {
                _open=OwnerTextAction.Create(transform,"QuickMatchButton","QUICK MATCH",OnQuickMatchPressed,0,0,413,91,39);
                var rt=(RectTransform)_open.transform;rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(.5f,0);
                rt.anchoredPosition=new Vector2(0,40);rt.sizeDelta=new Vector2(413,91);
            }
            else if(_open!=null)_open.onClick.AddListener(OnQuickMatchPressed);
            var ticket=OwnerUiLayout.Rect(transform,"NativeQueueState").gameObject.AddComponent<OwnerQueueTicket>();
            ticket.raycastTarget=true;_card=ticket.gameObject;
            if(_docked)
            {
                _nativeQueueBounds=(RectTransform)transform;_nativeQueueBounds.sizeDelta=new Vector2(620,440);
                OwnerUiLayout.Fill(ticket.rectTransform);
            }
            else
            {
                _nativeQueueBounds=ticket.rectTransform;
                _nativeQueueBounds.anchorMin=_nativeQueueBounds.anchorMax=_nativeQueueBounds.pivot=new Vector2(.5f,0);
                _nativeQueueBounds.anchoredPosition=new Vector2(0,180);_nativeQueueBounds.sizeDelta=new Vector2(620,440);
            }
            var root=ticket.rectTransform;var theme=OwnerUiTheme.Current;
            _headline=OwnerUiLayout.Text(root,"QueueHeadline","Finding a match",36,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_headline.rectTransform,28,20,564,66);_headline.color=theme.GuestInk;
            _band=OwnerUiLayout.Text(root,"SearchBand","",30);OwnerUiLayout.Place(_band.rectTransform,30,104,560,86);_band.color=theme.Paper;
            _elapsed=OwnerUiLayout.Text(root,"QueueElapsed","",30,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_elapsed.rectTransform,30,200,560,59);_elapsed.color=theme.Lime;
            var track=OwnerUiLayout.Rect(root,"SearchRangeTrack").gameObject.AddComponent<Image>();
            OwnerUiLayout.Place(track.rectTransform,30,274,556,8);track.color=new Color(1,1,1,.2f);track.raycastTarget=false;
            _barFill=OwnerUiLayout.Rect(track.transform,"SearchRangeFill").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(_barFill.rectTransform);_barFill.color=theme.Lime;_barFill.raycastTarget=false;
            _promise=OwnerUiLayout.Text(root,"RotationPromise",MatchmakingRules.TayaRotationPromise,30);
            OwnerUiLayout.Place(_promise.rectTransform,30,300,560,100);_promise.color=theme.Paper;
            _fill=OwnerTextAction.Create(root,"StartWithBotsButton","",OnFillPressed,26,413,350,80,30);
            _fillLabel=_fill.GetComponentInChildren<Text>();
            _fillLabel.color=theme.Lime;
            _fillCaveat=OwnerUiLayout.Text(root,"BotFillCaveat","",30);
            OwnerUiLayout.Place(_fillCaveat.rectTransform,30,515,556,190);_fillCaveat.color=theme.Paper;
            _cancel=OwnerTextAction.Create(root,"CancelQueueButton","CANCEL",OnCancelPressed,402,413,192,80,30);
            _cancel.GetComponentInChildren<Text>().color=theme.GuestInk;
            RefreshNativeQueue();
        }
    }
}
