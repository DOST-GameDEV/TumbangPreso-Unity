using System;
using TumbangPreso.Core;
using TumbangPreso.Net;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class ConvertedMatchSetup
    {
        private OwnerPreparationView _ownerPreparation;
        private LobbyMode _ownerRoute;
        private float _ownerRefreshAt;
        private CustomGameScreen _ownerCustomRules;
        private LobbyMode ActivePreparationRoute=>_ownerPreparation!=null?_ownerRoute:
            _chrome!=null?_chrome.Mode:IsLobby?LobbyMode.Custom:LobbyMode.Practice;

        private void WireOwnerPreparation()
        {
            for(int i=0;i<_replicatedPicks.Length;i++)_replicatedPicks[i]=-1;
            GameServices.Music?.Play("menu",GameServices.MenuTrack);
            _ownerRoute=PlaySelectionScreen.RequestedLobbyMode??(IsLobby?LobbyMode.Custom:LobbyMode.Practice);
            PlaySelectionScreen.RequestedLobbyMode=null;
            if(_ownerRoute==LobbyMode.Ranked)SceneFlow.SelectedMode=GameMode.HeroStrike;
            var net=IsLobby?NetSession.Ensure():NetSession.Instance;
            if(net!=null)
            {
                net.Lobby.JoinCodeChanged+=HandleJoinCodeChanged;net.SeatingChanged+=HandleSeatingChanged;
                NetSession.ClientDisconnected+=HandleClientDisconnected;
                if(NetAuthority.IsHost && net.Lobby.MatchInProgress)
                {net.Lobby.ReturnToLobby();MatchRpc.Instance?.BroadcastLobbyPicks();}
            }
            _map=Mathf.Max(0,Array.IndexOf(SceneFlow.Maps,SceneFlow.SelectedMap));
            var settings=Settings.SettingsStore.Current;
            _difficulty=Mathf.Clamp(settings.AiDifficulty,0,DifficultyOptionCount-1);
            AIController.ApplyDifficulty(_difficulty);
            if((!SceneFlow.Networked || NetAuthority.IsHost) && !SceneFlow.RulesPinned)
                SceneFlow.SetSelectedRules(CustomGameRules.Parse(settings.CustomRulesWire,SceneFlow.SelectedMode));
            _format=Mathf.Clamp((int)SceneFlow.SelectedFormat,0,FormatOptionCount-1);
            _characterPanel=Node("CharacterSelectPanel");EnsureCharacterOverlayIsolation();
            foreach(Transform child in transform)child.gameObject.SetActive(false);
            _ownerPreparation=gameObject.AddComponent<OwnerPreparationView>();
            _ownerPreparation.Build(transform,OwnerPreparationBack,OnPrimaryPressed,OnStartPressed,
                OpenJoinPanel,ToggleOnline,ToggleSpectate,OpenOwnerCustomRules,OpenLoadout,OpenPlayerHub,OpenGameSettings,
                OnMapCycle,OnModeCycle,OnDifficultyCycle,OwnerSeatPressed,OnCodeCopyPressed,OnAddressCopyPressed,SelectOwnerRoute,ToggleOwnerChat);
            _preview=_ownerPreparation.Preview;
            _codeCopyBtnText=_ownerPreparation.CopyCode.GetComponentInChildren<Text>();
            _addressCopyBtnText=_ownerPreparation.CopyAddress.GetComponentInChildren<Text>();
            _joinPanel=LobbyJoinPanel.Build(transform,net);_joinPanel.Status+=SetStatus;_joinPanel.Joined+=HandleJoinedInPlace;
            _queueCard=QueueCard.Dock(_ownerPreparation.QueueHost,null);
            _queueCard.Stake=QueueStake.Ranked;
            OwnerUiLayout.Place((RectTransform)_queueCard.transform,0,0,620,440);
            _queueCard.Status+=SetStatus;_queueCard.Joined+=HandleJoinedInPlace;_queueCard.StartWithBots+=StartAgainstBots;
            _chat=LobbyChat.Attach(_ownerPreparation.Canvas.transform,inMatch:false);
            if(_chat!=null){_chat.PlaceBottomRight(770,220,1060);_chat.SetPresented(false);}
            _hub=GetComponent<PlayerHub>();if(_hub==null)_hub=gameObject.AddComponent<PlayerHub>();_hub.Install();
            MatchRpc.OnMapChanged+=HandleMapSynced;MatchRpc.OnDifficultyChanged+=HandleDifficultySynced;
            MatchRpc.OnFormatChanged+=HandleFormatSynced;MatchRpc.OnRulesChanged+=HandleRulesSynced;
            MatchRpc.OnLobbyPicksSynced+=HandleLobbyPicksSynced;MatchRpc.OnLobbyRosterSynced+=HandleLobbyRosterSynced;
            MatchRpc.OnLobbyReadyChanged+=HandleLobbyReadyChanged;MatchRpc.OnModeChanged+=HandleModeSynced;
            MatchRpc.OnMatchStarted+=HandleMatchStarted;
            var people=Roster.GetPeople(SceneFlow.SelectedMode);
            if(settings.CharacterPick<0 || settings.CharacterPick>=people.Count)settings.CharacterPick=0;
            if(settings.CanPick<0 || settings.CanPick>=Roster.Cans.Count)settings.CanPick=0;
            if(settings.SlipperPick<0 || settings.SlipperPick>=Roster.Slippers.Count)settings.SlipperPick=0;
            Settings.SettingsStore.Save();
            if(net!=null && net.IsNetworked)
                MatchRpc.Instance?.SelectLobbyPickServerRpc(settings.CharacterPick,settings.CanPick,settings.SlipperPick);
            RefreshOwnerPreparation();RejoinRunningMatch();AutoHost();
        }
        private void OwnerPreparationBack()
        {
            if(_chat!=null && _chat.IsPresented){_chat.SetPresented(false);return;}
            if(_joinPanel!=null && _joinPanel.IsOpen){_joinPanel.Close();return;}
            if(_queueCard!=null && _queueCard.IsQueueing){_queueCard.CancelSearch();return;}
            var net=NetSession.Instance;if(net!=null && net.IsNetworked)net.Stop();
            SceneFlow.Go(SceneFlow.ModeSelect);
        }
        private void ToggleOwnerChat()
        {
            if(_chat!=null)_chat.SetPresented(!_chat.IsPresented && IsLobby);
        }
        private void OpenOwnerCustomRules()
        {
            if(_ownerCustomRules==null)
            {
                _ownerCustomRules=CustomGameScreen.Ensure();
                _ownerCustomRules.RulesChanged+=OwnerRulesChanged;
            }
            _ownerCustomRules.Open();
        }
        private void OwnerRulesChanged()
        {
            _difficulty=Mathf.Clamp(Settings.SettingsStore.Current.AiDifficulty,0,DifficultyOptionCount-1);
            _format=Mathf.Clamp((int)SceneFlow.SelectedFormat,0,FormatOptionCount-1);
            RefreshOwnerPreparation();
        }
        private void SelectOwnerRoute(LobbyMode mode)
        {
            if(mode==_ownerRoute)return;
            if(_queueCard!=null && _queueCard.IsQueueing)_queueCard.CancelSearch();
            var net=NetSession.Instance;
            if(mode==LobbyMode.Practice && net!=null && net.IsNetworked)net.Stop();
            SceneFlow.Networked=mode!=LobbyMode.Practice;PlaySelectionScreen.RequestedLobbyMode=mode;
            SceneFlow.Go(SceneFlow.MatchSetup);
        }
        private void OwnerSeatPressed(int seat)
        {
            int mine=IsLive?NetAuthority.LocalSlot:GameLaunch.SoloSeat;
            if(seat==mine && !GameLaunch.Spectator)OpenLoadout();else TakeSeat(seat);
        }
        private void RefreshOwnerPreparation()
        {
            if(_ownerPreparation==null)return;
            var view=_ownerPreparation;var net=NetSession.Instance;bool live=IsLive;
            bool host=!SceneFlow.Networked || NetAuthority.IsHost;
            bool ranked=_ownerRoute==LobbyMode.Ranked;
            view.SelectRoute(_ownerRoute);
            SceneFlow.SelectedMap=SceneFlow.Maps[Mathf.Clamp(_map,0,SceneFlow.Maps.Length-1)];
            var map=SceneFlow.PreviewFor(SceneFlow.SelectedMap);
            view.Heading.text=ranked?"RANKED MATCH":IsLobby?"FRIENDS LOBBY":"YOUR MATCH";
            view.MapName.text=map.Name;view.ModeName.text=SceneFlow.SelectedMode==GameMode.Classic?"CLASSIC":"HERO STRIKE";
            view.BotsName.text=Difficulties[_difficulty];
            if(view.Preview.Showing!=SceneFlow.SelectedMap)view.Preview.Show(SceneFlow.SelectedMap);
            var settings=Settings.SettingsStore.Current;
            var rules=SceneFlow.SelectedRules;
            view.RulesSummary.text=CustomGameRules.FormatName(rules.Format)+" · "+rules.Rounds+" rounds · "+rules.RoundSeconds+"s";
            var people=Roster.GetPeople(SceneFlow.SelectedMode);
            string person=Roster.At(people,settings.CharacterPick)?.Name??"";
            string can=Roster.At(Roster.Cans,settings.CanPick)?.Name??"";
            string shoe=Roster.At(Roster.Slippers,settings.SlipperPick)?.Name??"";
            view.LoadoutName.text=person+" · "+can+" · "+shoe;
            view.ProfileName.text=GameServices.Account?.DisplayName??"YOUR PROFILE";
            view.ShowRoom(IsLobby && live && !ranked);
            view.RoomCode.text="CODE  "+(net?.Lobby?.JoinCode??"");view.RoomAddress.text=live?HostAddress():"";
            view.JoinRoom.gameObject.SetActive(IsLobby && !ranked);
            view.Chat.gameObject.SetActive(IsLobby && live);
            view.Online.gameObject.SetActive(IsLobby && live && NetAuthority.IsHost && !ranked);
            view.Online.GetComponentInChildren<Text>().text=net!=null && net.IsRelay?"USE LOCAL NETWORK":"GO ONLINE";
            foreach(var control in new[]{view.MapPrevious,view.MapNext,view.ModePrevious,view.ModeNext,view.BotsPrevious,view.BotsNext})
                control.interactable=host && !ranked;
            view.CustomRules.interactable=!ranked;
            view.Spectate.interactable=!ranked;
            view.Spectate.GetComponentInChildren<Text>().text=GameLaunch.Spectator?"TAKE A SEAT":"WATCH INSTEAD";
            view.QueueHost.gameObject.SetActive(ranked);
            if(ranked)RefreshOwnerRank();
            RefreshOwnerActions();RefreshOwnerSeats();view.RebuildFocus();
        }
        private void RefreshOwnerActions()
        {
            if(_ownerPreparation==null)return;var view=_ownerPreparation;
            bool ranked=_ownerRoute==LobbyMode.Ranked;
            bool host=IsLobby && IsLive && NetAuthority.IsHost;
            bool searching=ranked && _queueCard!=null && _queueCard.IsQueueing;
            view.StartMatch.gameObject.SetActive((ranked || host) && !searching);view.Primary.gameObject.SetActive(!ranked && !host);
            view.LoadoutName.gameObject.SetActive(!searching);
            if(ranked)
            {
                bool queueing=_queueCard!=null && _queueCard.IsQueueing;
                view.StartMatch.GetComponentInChildren<Text>().text=queueing?"SEARCHING...":"FIND A MATCH";
                view.StartMatch.interactable=GameServices.Account!=null && !GameServices.Account.IsGuest && !queueing;
            }
            else if(host)
            {
                bool full=AIController.BotsEnabled || NetSession.Instance.Lobby.OccupiedSeatCount()>=Balance.PlayerCount;
                view.StartMatch.GetComponentInChildren<Text>().text=full?"START MATCH":"WAITING FOR PLAYERS";
                view.StartMatch.interactable=full;
            }
            else
            {
                view.Primary.GetComponentInChildren<Text>().text=!IsLobby?"START MATCH":!IsLive?"CONNECTING...":
                    GameLaunch.Spectator?"WATCHING":_localReady?"READY ✓":"READY";
                view.Primary.interactable=!IsLobby || (IsLive && !GameLaunch.Spectator);
            }
        }
        private void RefreshOwnerRank()
        {
            var view=_ownerPreparation;var account=GameServices.Account;
            if(account==null || account.IsGuest)
            {
                view.RankedTitle.text="SIGN IN TO RANK";
                view.RankedDetail.text="Open your profile above to sign in. Offline play and friends' rooms support guests.";
                return;
            }
            var rank=GameServices.Career?.Profile?.Rank;
            if(rank==null || rank.MatchesThisSeason==0)
            {
                view.RankedTitle.text="UNRANKED";view.RankedDetail.text="Play a ladder match to get placed. Solo, or a party of up to three.";
            }
            else
            {
                view.RankedTitle.text=RatingRules.TierName(RatingRules.TierFor(rank.Rating));
                view.RankedDetail.text=(rank.Deviation>RatingRules.SettledDeviation?"Still placing":"Rank established")+
                    $" · {rank.MatchesThisSeason} this season. Solo, or a party of up to three.";
            }
        }
        private void RefreshOwnerSeats()
        {
            if(_ownerPreparation==null)return;bool live=IsLive;var people=Roster.GetPeople(SceneFlow.SelectedMode);
            int local=live?NetAuthority.LocalSlot:GameLaunch.SoloSeat;
            for(int i=0;i<Balance.PlayerCount;i++)
            {
                var info=live?MatchRpc.Instance?.GetSeatInfo(i):null;
                bool mine=!GameLaunch.Spectator && i==local;bool occupied=mine || (info!=null && info.Occupied);
                int pick=mine?Settings.SettingsStore.Current.CharacterPick:info?.CharacterPick??-1;
                if(occupied && pick>=0 && pick<people.Count)_ownerPreparation.SetPortrait(i,"UI/portraits/"+people[pick].Id);
                else
                {
                    _ownerPreparation.SetEmptySeat(i,_ownerRoute!=LobbyMode.Ranked && AIController.BotsEnabled);
                }
                string label=mine?"YOU":occupied?(info.Name??"PLAYER "+(i+1)):
                    _ownerRoute!=LobbyMode.Ranked && AIController.BotsEnabled?"BOT":"OPEN";
                bool ready=mine?_localReady:info!=null && info.Ready;
                _ownerPreparation.SeatLabels[i].text=label+(ready?" · READY":"");
                _ownerPreparation.Seats[i].interactable=mine || !live || (!occupied && !NetSession.Instance.Lobby.MatchInProgress);
            }
        }
    }
}
