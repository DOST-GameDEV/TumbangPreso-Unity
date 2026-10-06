using TumbangPreso.InputLayer;
using UnityEngine.EventSystems;

namespace TumbangPreso.UI
{
    public sealed partial class MatchResult
    {
        CourtPreviewPicker _courtPicker;
        void OpenCourtPicker()
        {
            if(_courtPicker!=null||IsSpectator||!IsVisible)return;
            int seat=LocalResultSeat();
            if(seat<0||seat>=_mapVotes.Length)return;
            int initial=_mapVotes[seat]>=0?_mapVotes[seat]:ProjectedNextMap();
            _courtPicker=CourtPreviewPicker.Open(_canvas.transform,initial,ConfirmCourtVote,CloseCourtPicker);
        }
        void ConfirmCourtVote(int choice)
        {
            if(IsSpectator||!IsVisible||choice<0||choice>=SceneFlow.Maps.Length)return;
            int seat=LocalResultSeat();if(seat<0||seat>=_mapVotes.Length)return;
            _mapVotes[seat]=choice;RefreshMapVote();
            if(NetAuthority.IsNetworked)Net.MatchRpc.Instance?.SelectMapVoteServerRpc(choice);
            CloseCourtPicker();
        }
        void CloseCourtPicker()
        {
            if(_courtPicker==null)return;
            _courtPicker.Close();_courtPicker=null;ScreenTakeover.ConsumeEscape();
            if(_mapVote!=null&&_mapVote.gameObject.activeInHierarchy&&EventSystem.current!=null)
                EventSystem.current.SetSelectedGameObject(_mapVote.gameObject);
        }
    }
}
