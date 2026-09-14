using System;
using TumbangPreso.Core;

namespace TumbangPreso.UI
{
    public sealed partial class PlayerHub
    {
        private void BuildFriendsTab()
        {
            var social=GameServices.Social;var list=social?.List;var now=DateTime.UtcNow;
            if(!SocialRules.IsAddressable(GameServices.Account?.PlayerId))
            {
                HubNote("Sign in from ACCOUNT to keep a friends list across devices.");SetFooter("","");return;
            }
            BuildRequestRows(social,list);BuildFindFriendRows(social);BuildFriendRows(social,list,now);BuildBlockedRows(social,list);
            SetFooter("REFRESH","Presence updates about once a minute while the game is open.");
        }
        private void BuildRequestRows(Net.SocialStore social,SocialList list)
        {
            int pending=list?.Incoming?.Count??0;if(pending==0)return;
            if(!Group("Friend requests",pending+" waiting for your response."))return;
            foreach(var request in list.Incoming)
            {
                string name=string.IsNullOrEmpty(request.Handle)?Shorten(request.PlayerId):request.Handle,id=request.PlayerId;
                HubAction("AcceptFriend",name,"ACCEPT",()=>social?.Accept(id));
                HubAction("DeclineFriend","","DECLINE",()=>social?.Decline(id));
            }
        }
        private void BuildFindFriendRows(Net.SocialStore social)
        {
            if(!Group("Find a friend","Enter their exact NAME#TAG.",false))return;
            var field=OwnerUiEntry.Create(OwnerSettingsRows.Row(_list,"FriendSearchRow","Name and tag"),"FriendSearch","NAME#TAG",OwnerUiTheme.Piece.FirstField);
            field.characterLimit=AccountRules.HandleMax;field.SetTextWithoutNotify(_ownerFriendSearch);field.onValueChanged.AddListener(value=>_ownerFriendSearch=value);
            HubAction("SendFriendRequest","","SEND REQUEST",()=>social?.RequestHandle(_ownerFriendSearch),"They must accept before joining your friends list.");
            if(!string.IsNullOrEmpty(social?.SearchStatus))HubNote(social.SearchStatus);
        }
        private void BuildFriendRows(Net.SocialStore social,SocialList list,DateTime now)
        {
            var friends=SocialRules.Sorted(list?.Friends,now);
            if(friends.Count==0){HubNote("No friends yet. Add someone by their tag or from the results after a match.");return;}
            int online=0;foreach(var friend in friends)if(SocialRules.EffectivePresence(friend,now)!=PresenceState.Offline)online++;
            if(!Group("Friends",online+" of "+friends.Count+" online."))return;
            foreach(var friend in friends)
            {
                var state=SocialRules.EffectivePresence(friend,now);
                string name=string.IsNullOrEmpty(friend.Handle)?Shorten(friend.PlayerId):friend.Handle,id=friend.PlayerId,code=friend.JoinCode;
                if(SocialRules.IsJoinable(friend,now))HubAction("JoinFriend",name,"JOIN",()=>JoinFriend(code),SocialRules.PresenceLabel(state));
                else HubValue(name,SocialRules.PresenceLabel(state));
                HubAction("RemoveFriend","","REMOVE",()=>social?.Remove(id),"Removes each of you from the other's friends list.");
            }
        }
        private void BuildBlockedRows(Net.SocialStore social,SocialList list)
        {
            int blocked=list?.Blocked?.Count??0;if(blocked==0 || !Group("Blocked",blocked+" cannot join games you host.",false))return;
            foreach(string id in list.Blocked)
            {
                string subject=id;HubAction("UnblockPlayer",Shorten(subject),"UNBLOCK",()=>social?.Unblock(subject),"Lets them join again without adding them as a friend.");
            }
        }
    }
}
