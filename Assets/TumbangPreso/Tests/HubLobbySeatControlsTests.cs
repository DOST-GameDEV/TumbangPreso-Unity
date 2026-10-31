using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace TumbangPreso.Tests
{
    public sealed class HubLobbySeatControlsTests
    {
        sealed class Host : IHubHost
        {
            public string OnlineBrowserMessage { get; set; } = "No public rooms yet. Host one, or join with a code.";
            public bool Allow=true; public int Requested=-1;
            public HubSeat[] Data={
                new HubSeat{Slot=0,Occupied=true,Mine=true,Host=true,Name="YOU",CharacterPick=-1},
                new HubSeat{Slot=1,Occupied=true,Name="GUEST",Ready=true,CharacterPick=-1},
                new HubSeat{Slot=2,Bot=true,CharacterPick=-1},new HubSeat{Slot=3,CharacterPick=-1}};
            public MapPreviewSurface Preview=>null; public bool OverlayOpen=>false;
            public bool InRoom=>true; public bool Hosting=true; public bool IsHost=>Hosting; public bool LocalReady=>false;
            public string RoomCode=>"TEST";public bool RoomOnline=>false;public string RoomTitle=>"SEAT CONTROLS";
            public bool MatchInProgress{get;set;}public bool Watching; public bool Spectating=>Watching;public string RoomAddress=>Hosting?"192.168.1.7:8910":"";
            public bool MapVoting=>false;public float MapVoteSecondsLeft=>0;public int MapVoteWinner=>0;
            public int MapVoteFor(int seat)=>-1;public HubSeat[] Seats()=>Data;
            public TumbangPreso.Net.LobbySeatSwapOffer SeatSwapOffer {get;set;}
            public string SeatSwapResult=>"";public long Replied;public bool Accepted;
            public void RespondToSeatSwap(long id,bool accept){Replied=id;Accepted=accept;SeatSwapOffer=null;}
            public bool CanTakeSeat(int seat)=>Allow&&!MatchInProgress&&SeatSwapOffer==null&&seat>=0&&seat<Data.Length&&!Data[seat].Mine;
            public void TakeSeat(int seat){Requested=seat;}
            public bool EditBots=true;public int BotSeat=-1;public bool BotWanted;
            public bool CanEditBots=>EditBots&&!MatchInProgress;
            public void SetSeatBot(int seat,bool bot){BotSeat=seat;BotWanted=bot;}
            public void OpenSettings(){}public void OpenProfile(){}public void OpenCareer(){}public void OpenParty(){}public void OpenCustomRules(){}
            public void StartPractice(){}public string StartQueue(GameMode mode,QueueStake stake)=>"";
            public void CancelQueue(){}public void AcceptBots(){}public void LeaveRoom(){}
            public Task<string> HostRoom(string title,string map,GameMode mode,RoomVisibility visibility,bool online)=>Task.FromResult("");
            public Task<string> Join(string codeOrAddress)=>Task.FromResult("");
            public void StartGame(){}public void ToggleReady(){}public void LockIn(){}public void VoteMap(int index){}
            public void PublishPicks(){}public string Map;public void SelectMap(string map){Map=map;}public void SelectMode(GameMode mode){}
            public List<HubRoom> LanRooms=new List<HubRoom>(),OnlineRooms=new List<HubRoom>();
            public void Browse(){}public void RefreshRooms(bool lan){}public List<HubRoom> Rooms(bool lan)=>lan?LanRooms:OnlineRooms;public void ToggleChat(){}public void ToggleSpectate(){}
        }
        GameObject _root,_hubRoot,_cameraRoot;Host _host;HubLobby _screen;Camera _camera;RenderTexture _target;
        [SetUp] public void Before()
        {
            _host=new Host();BuildForCurrentSeats();
        }
        private void BuildForCurrentSeats()
        {
            _hubRoot=new GameObject("Seat control host");var hub=_hubRoot.AddComponent<TumpHub>();hub.enabled=false;
            typeof(TumpHub).GetProperty("Host").SetValue(hub,_host);
            _root=new GameObject("Seat control canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
            typeof(TumpHub).GetProperty("Canvas").SetValue(hub,_root.GetComponent<Canvas>());
            _screen=_root.AddComponent<HubLobby>();_screen.enabled=false;
            typeof(HubScreen).GetProperty("Hub").SetValue(_screen,hub);
            typeof(HubScreen).GetProperty("Root").SetValue(_screen,_root.GetComponent<RectTransform>());
            _cameraRoot=new GameObject("Seat control UI camera",typeof(Camera));_camera=_cameraRoot.GetComponent<Camera>();
            _camera.enabled=false;_camera.cullingMask=1<<31;_camera.clearFlags=CameraClearFlags.SolidColor;_camera.backgroundColor=new Color(.1f,.1f,.1f);
            _target=new RenderTexture(1920,1080,24);_camera.targetTexture=_target;
            var canvas=_root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=_camera;canvas.planeDistance=1;
            var scaler=_root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);
            _screen.Build();foreach(var t in _root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
        }
        [TearDown] public void After()
        {
            if(_camera!=null)_camera.targetTexture=null;
            Object.DestroyImmediate(_root);Object.DestroyImmediate(_hubRoot);Object.DestroyImmediate(_cameraRoot);
            _target.Release();Object.DestroyImmediate(_target);
        }
        private void RebuildForCurrentSeats()
        {
            // Each new Editor case needs its initial roster, not a runtime deferred row redraw.
            // Dispose only this fixture's previous canvas/camera, then build the real lobby.
            After();BuildForCurrentSeats();
        }
        Button Button(string name)=>_root.GetComponentsInChildren<Button>(true).FirstOrDefault(b=>b.name==name);
        [Test] public void OpenAndBotSlotsExposeRealSeatActions()
        {
            var bot=Button("TakeSeat2");var open=Button("TakeSeat3");
            Assert.IsNotNull(bot,"The current lobby needs a seat action for a bot slot.");
            Assert.IsNotNull(open,"The current lobby needs a seat action for an open slot.");
            Assert.IsTrue(bot.interactable);Assert.IsTrue(open.interactable);
            bot.onClick.Invoke();Assert.AreEqual(2,_host.Requested);
            open.onClick.Invoke();Assert.AreEqual(3,_host.Requested);
        }
        /// <summary>
        /// The review picture of the room, written only when TUMP_LOBBY_CAPTURE names a file: the
        /// room at rest, and a second frame (`_hover` suffix) with P4's row in its hover state.
        /// ⚠️ THE ENTRANCE IS SETTLED FIRST. `HubSlap` animates the players' panel in from
        /// `Update`, which never runs inside an EditMode test, so the first capture of this screen
        /// was a room with no panel in it at all.
        /// </summary>
        [Test] public void RenderTheKnitScreensForReview()
        {
            string path=System.Environment.GetEnvironmentVariable("TUMP_LOBBY_CAPTURE");
            if(string.IsNullOrEmpty(path) || SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
            foreach(Transform child in _root.transform)child.gameObject.SetActive(false);
            _host.LanRooms.Add(new HubRoom{Name="NEIGHBOURHOOD",Map="TONDO",Players=2,Capacity=4,IsJoinable=true,Key="192.168.1.7:8910"});
            _host.LanRooms.Add(new HubRoom{Name="OLDER GAME",Map="ERMITA",Players=1,Capacity=4,DifferentVersion=true,Key="192.168.1.8:8910"});
            _host.OnlineRooms.Add(new HubRoom{Name="FRIENDS ONLINE",Map="SITANGKAI",Players=3,Capacity=4,IsJoinable=true,Key="ABCD"});
            foreach(var type in new[]{typeof(HubModeSelect),typeof(HubHost),typeof(HubJoin),typeof(HubPracticePopup),typeof(HubClassicPopup),typeof(HubCustomPopup),typeof(HubRoomClosePopup)})
            {
                var dock=new GameObject(type.Name,typeof(RectTransform));dock.transform.SetParent(_root.transform,false);
                var rect=(RectTransform)dock.transform;HubKit.Stretch(rect);
                var view=(HubScreen)dock.AddComponent(type);view.enabled=false;
                typeof(HubScreen).GetProperty("Hub").SetValue(view,_hubRoot.GetComponent<TumpHub>());
                typeof(HubScreen).GetProperty("Root").SetValue(view,rect);view.Build();
                foreach(var t in dock.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
                foreach(var slap in dock.GetComponentsInChildren<HubSlap>(true))
                {slap.enabled=false;slap.transform.localScale=Vector3.one;slap.transform.localRotation=Quaternion.identity;var group=slap.GetComponent<CanvasGroup>();if(group!=null)group.alpha=1;}
                Write(Path.Combine(Path.GetDirectoryName(path),type.Name+".png"));
                Object.DestroyImmediate(dock);
            }
        }
        [Test] public void RenderTheRoomForReview()
        {
            string path=System.Environment.GetEnvironmentVariable("TUMP_LOBBY_CAPTURE");
            if(string.IsNullOrEmpty(path) || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            // Real heroes on the cards, so the review shows the portraits popping out of them.
            for(int i=0;i<_host.Data.Length;i++) if(_host.Data[i].Occupied) _host.Data[i].CharacterPick=i;
            RebuildForCurrentSeats();
            // A warm street tone rather than flat grey: every shadow alpha is judged against what is behind it.
            _camera.backgroundColor=new Color(.47f,.45f,.38f);
            foreach(var slap in _root.GetComponentsInChildren<HubSlap>(true))
            {
                slap.enabled=false;slap.transform.localScale=Vector3.one;slap.transform.localRotation=Quaternion.identity;
                var group=slap.GetComponent<CanvasGroup>();if(group!=null)group.alpha=1;
            }
            Write(path);
            var row=(HubButton)Button("TakeSeat3");
            // The knit card's hover pose, as `HubStickerFeel` holds it: grown a little, its hint shown.
            row.Body.localScale=new Vector3(1.035f,1.035f,1);
            row.Body.Find("Label").GetComponent<Text>().enabled=true;
            Write(Path.Combine(Path.GetDirectoryName(path),Path.GetFileNameWithoutExtension(path)+"_hover.png"));
        }
        private void Write(string path)
        {
            Canvas.ForceUpdateCanvases();_camera.Render();var previous=RenderTexture.active;var image=new Texture2D(1920,1080,TextureFormat.RGBA32,false);
            try{RenderTexture.active=_target;image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
            finally{RenderTexture.active=previous;Object.DestroyImmediate(image);}
        }
        /// <summary>
        /// The joined and spectating rooms for review, written beside the host's picture when
        /// TUMP_LOBBY_CAPTURE names a file (`_joined`, `_spectator` suffixes).
        /// </summary>
        [Test] public void RenderTheOtherRolesForReview()
        {
            string path=System.Environment.GetEnvironmentVariable("TUMP_LOBBY_CAPTURE");
            if(string.IsNullOrEmpty(path) || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            foreach(var role in new[]{"joined","spectator"})
            {
                _host.Hosting=false;_host.EditBots=false;_host.Watching=role=="spectator";
                _host.Data[0]=new HubSeat{Slot=0,Occupied=true,Host=true,Name="PLAYER",CharacterPick=0};
                _host.Data[1]=role=="joined"?new HubSeat{Slot=1,Occupied=true,Mine=true,Name="YOU",CharacterPick=1}:new HubSeat{Slot=1,Occupied=true,Name="GUEST",Ready=true,CharacterPick=1};
                RebuildForCurrentSeats();
                _camera.backgroundColor=new Color(.47f,.45f,.38f);
                foreach(var slap in _root.GetComponentsInChildren<HubSlap>(true))
                {
                    slap.enabled=false;slap.transform.localScale=Vector3.one;slap.transform.localRotation=Quaternion.identity;
                    var group=slap.GetComponent<CanvasGroup>();if(group!=null)group.alpha=1;
                }
                Write(Path.Combine(Path.GetDirectoryName(path),Path.GetFileNameWithoutExtension(path)+"_"+role+".png"));
            }
        }
        [Test] public void TheHostChangesTheMapWithTheHostGameSelector()
        {
            var card=_root.transform.Find("MatchCard");Assert.IsNotNull(card,"The host's room has its MATCH card.");
            Assert.IsTrue(card.gameObject.activeSelf);
            var choice=card.GetComponentInChildren<HubArrowChoice>(true);
            Assert.IsNotNull(choice,"MAP is the same previous/next selector HOST GAME uses.");
            Assert.IsNull(Button("MapDoor"),"The CHANGE MAP popup door is gone.");
            choice.Next.onClick.Invoke();
            Assert.AreEqual(SceneFlow.MapRegistry[choice.Value].Id,_host.Map,"NEXT selects the following registered map.");
            Assert.IsFalse(_root.transform.Find("Players/MatchSummary").gameObject.activeSelf,"The host reads the card, not the summary.");
        }
        [Test] public void AJoinedPlayerSeesASummaryNotTheHostsControls()
        {
            _host.Hosting=false;_host.EditBots=false;RebuildForCurrentSeats();
            Assert.IsFalse(_root.transform.Find("MatchCard").gameObject.activeSelf,"A joined player gets no MATCH card.");
            var summary=_root.transform.Find("Players/MatchSummary");
            Assert.IsTrue(summary.gameObject.activeSelf,"A joined player reads the match in one line.");
            Assert.IsNotNull(summary.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name=="ViewRules"));
            Assert.AreEqual(0,((RectTransform)_root.transform.Find("Players")).anchoredPosition.x,.1f,"With the chat closed the seats are centred.");
            Assert.AreEqual("READY",HubKit.LabelOf((HubButton)Button("StartGame")).text);
            Assert.AreEqual("SPECTATE",HubKit.LabelOf((HubButton)Button("WatchToggle")).text);
        }
        [Test] public void EveryRoomControlIsHomeKnit()
        {
            // Owner 2026-10-28: the room is new UI in HOME's knit, not the old stickers rearranged.
            // Every control is a `HubKnit` piece moving with HOME's feel; BACK is HOME's own red piece.
            var buttons=_root.GetComponentsInChildren<HubButton>(true).Where(b=>b.name!="BackButton").ToList();
            Assert.IsNotEmpty(buttons);
            foreach(var button in buttons)
            {
                Assert.IsNull(button.Shape,"An old sticker in the room: "+button.name);
                Assert.IsNotNull(button.GetComponent<HubStickerFeel>(),"A room control without HOME's feel: "+button.name);
            }
        }
        [Test] public void TheWholeRowIsTheSeatDoorAndSitHereIsGone()
        {
            // Owner 2026-10-17: click anywhere on another P row to move there.
            var row=Button("TakeSeat3");Assert.IsNotNull(row);
            var rect=(RectTransform)row.transform;var seat=(RectTransform)rect.parent;
            Assert.AreEqual(seat.rect.size,rect.rect.size,"The seat door must cover the whole row.");
            Assert.IsFalse(_root.GetComponentsInChildren<Text>(true).Any(t=>t.text.Contains("SIT HERE")));
        }
        [Test] public void TheHostAddsAndRemovesBotsSeatBySeat()
        {
            var add=Button("AddBot3");var remove=Button("RemoveBot2");
            Assert.IsNotNull(add,"An open seat offers ADD BOT to the host.");
            Assert.IsNotNull(remove,"A bot seat offers REMOVE BOT to the host.");
            Assert.IsNull(Button("AddBot1"),"An occupied seat has no bot control.");
            Assert.IsNull(Button("AddBot0"));
            add.onClick.Invoke();Assert.AreEqual(3,_host.BotSeat);Assert.IsTrue(_host.BotWanted);
            remove.onClick.Invoke();Assert.AreEqual(2,_host.BotSeat);Assert.IsFalse(_host.BotWanted);
            Assert.AreEqual(-1,_host.Requested,"A bot control must not also move the player.");
        }
        [Test] public void OnlyTheHostSeesBotControls()
        {
            _host.EditBots=false;RebuildForCurrentSeats();
            Assert.IsNull(Button("AddBot3"));Assert.IsNull(Button("RemoveBot2"));
            Assert.IsNotNull(Button("TakeSeat3"),"Moving seats does not depend on editing bots.");
        }
        [Test] public void EmptySeatsWithNoBotsStillLetTheHostStart()
        {
            // Owner 2026-10-17: the host starts with nobody else in the room, and the old
            // "Bots are off" line under START is gone.
            foreach(int i in new[]{0,1,2,3})_host.Data[i]=new HubSeat{Slot=i,CharacterPick=-1};
            RebuildForCurrentSeats();Assert.IsTrue(Button("StartGame").interactable);
            var status=_root.GetComponentsInChildren<Text>(true).First(t=>t.name=="Status");
            StringAssert.DoesNotContain("Bots are off",status.text);
        }
        [Test] public void FillingVacanciesWithBotsKeepsStartAvailable()
        {
            foreach(int i in new[]{0,1,2,3})_host.Data[i]=new HubSeat{Slot=i,Bot=true,CharacterPick=-1};
            RebuildForCurrentSeats();Assert.IsTrue(Button("StartGame").interactable);
        }
        [Test] public void FourHumansKeepStartAvailableWithoutBots()
        {
            foreach(int i in new[]{0,1,2,3})_host.Data[i]=new HubSeat{Slot=i,Occupied=true,CharacterPick=-1,Name="PLAYER"};
            RebuildForCurrentSeats();Assert.IsTrue(Button("StartGame").interactable);
        }
        [Test] public void StaleRoomStateIsRecheckedBeforeSendingASeatRequest()
        {
            var button=Button("TakeSeat2");Assert.IsNotNull(button);
            _host.MatchInProgress=true;button.onClick.Invoke();Assert.AreEqual(-1,_host.Requested);
            _host.MatchInProgress=false;_host.Allow=false;button.onClick.Invoke();Assert.AreEqual(-1,_host.Requested);
            _host.Allow=true;button.onClick.Invoke();Assert.AreEqual(2,_host.Requested);
            Assert.AreNotEqual(Navigation.Mode.None,button.navigation.mode);
        }
        [Test] public void ANewHumanOccupantStillUsesTheAuthoritativeRequestRoute()
        {
            var button=Button("TakeSeat3");Assert.IsNotNull(button);
            _host.Data[3].Occupied=true;button.onClick.Invoke();Assert.AreEqual(3,_host.Requested);
            Assert.IsTrue(_host.Data[3].Occupied);
        }
        /// <summary>MATCH SETTINGS (owner 2026-10-28: the host's picks one press away, not printed across
        /// the room) lists what the host chose and closes on BACK. Written beside the room's picture as
        /// `_settings` when TUMP_LOBBY_CAPTURE names a file.</summary>
        [Test] public void OfflineChatCloseRestoresTheRoomDoors()
        {
            var character=Button("CharacterDoor");var chat=Button("ChatDoor");
            Assert.IsTrue(character.gameObject.activeSelf);
            _screen.SetChatPresented(true);
            Assert.IsFalse(character.gameObject.activeSelf);Assert.IsFalse(chat.gameObject.activeSelf);
            _screen.SetChatPresented(false);
            Assert.IsTrue(character.gameObject.activeSelf);Assert.IsTrue(chat.gameObject.activeSelf);
        }
        [Test] public void FullRulesUseTheKnitControls()
        {
            var rules=_hubRoot.AddComponent<CustomGameScreen>();rules.Open();
            var canvas=GameObject.Find("OwnerCustomGameCanvas").GetComponent<Canvas>();
            try
            {
                foreach(var name in new[]{"CustomRulesBack","MatchRulesTab","RoomRulesTab","RoundsNext","UseRulesButton"})
                    Assert.IsInstanceOf<HubButton>(canvas.GetComponentsInChildren<Button>(true).First(b=>b.name==name));
                string path=System.Environment.GetEnvironmentVariable("TUMP_LOBBY_CAPTURE");
                if(!string.IsNullOrEmpty(path) && SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null)
                {
                    canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=_camera;canvas.planeDistance=.5f;
                    foreach(var t in canvas.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
                    Write(Path.Combine(Path.GetDirectoryName(path),"full_rules_match.png"));
                    canvas.GetComponentsInChildren<Button>().First(b=>b.name=="RoomRulesTab").onClick.Invoke();
                    Write(Path.Combine(Path.GetDirectoryName(path),"full_rules_room.png"));
                }
            }
            finally{rules.Close();Object.DestroyImmediate(canvas.gameObject);Object.DestroyImmediate(rules);}
        }
        [Test] public void MatchSettingsCardListsTheHostsPicks()
        {
            _host.Hosting=false;_host.EditBots=false;RebuildForCurrentSeats();
            var popupRoot=new GameObject("Settings popup",typeof(RectTransform));
            popupRoot.transform.SetParent(_root.transform,false);
            var rect=(RectTransform)popupRoot.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
            var popup=popupRoot.AddComponent<HubMatchSettingsPopup>();
            typeof(HubScreen).GetProperty("Hub").SetValue(popup,_hubRoot.GetComponent<TumpHub>());
            typeof(HubScreen).GetProperty("Root").SetValue(popup,rect);popup.Build();
            var words=popupRoot.GetComponentsInChildren<Text>(true).Select(t=>t.text).ToList();
            Assert.Contains("MAP",words);Assert.Contains("MODE",words);Assert.Contains("ROUNDS",words);
            Assert.Contains(SceneFlow.SelectedRules.Rounds.ToString(),words);
            Assert.Contains("LAN",words,"A LAN room says so.");
            Assert.AreEqual("CloseSettings",popup.FirstFocus.name);
            Canvas.ForceUpdateCanvases();
            var last=popupRoot.GetComponentsInChildren<Text>(true).Where(t=>t.name.StartsWith("Value")).Last();
            var footer=popupRoot.GetComponentsInChildren<Button>(true).First(b=>b.name=="CloseSettings");
            var lastBounds=RectTransformUtility.CalculateRelativeRectTransformBounds(rect,last.rectTransform);
            var footerBounds=RectTransformUtility.CalculateRelativeRectTransformBounds(rect,footer.transform);
            Assert.Greater(lastBounds.min.y,footerBounds.max.y,"All facts must sit above the footer actions.");
            string path=System.Environment.GetEnvironmentVariable("TUMP_LOBBY_CAPTURE");
            if(!string.IsNullOrEmpty(path) && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                foreach(var t in popupRoot.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
                _camera.backgroundColor=new Color(.47f,.45f,.38f);
                foreach(var slap in _root.GetComponentsInChildren<HubSlap>(true))
                {
                    slap.enabled=false;slap.transform.localScale=Vector3.one;slap.transform.localRotation=Quaternion.identity;
                    var group=slap.GetComponent<CanvasGroup>();if(group!=null)group.alpha=1;
                }
                Write(Path.Combine(Path.GetDirectoryName(path),Path.GetFileNameWithoutExtension(path)+"_settings.png"));
            }
        }
        [TestCase(true),TestCase(false)] public void RecipientPopupSendsTheChosenAnswerExactlyOnce(bool accept)
        {
            var popupRoot=new GameObject("Consent popup",typeof(RectTransform));
            try
            {
                var offer=new TumbangPreso.Net.LobbySeatSwapOffer{Id=21,FromSeat=0,ToSeat=1,Incoming=true,RequesterName="Alice",ExpiresAt=double.MaxValue};
                _host.SeatSwapOffer=offer;var popup=popupRoot.AddComponent<HubSeatSwapPopup>();popup.Offer=offer;
                typeof(HubScreen).GetProperty("Hub").SetValue(popup,_hubRoot.GetComponent<TumpHub>());
                typeof(HubScreen).GetProperty("Root").SetValue(popup,popupRoot.GetComponent<RectTransform>());popup.Build();
                var yes=popupRoot.GetComponentsInChildren<Button>().First(b=>b.name=="AcceptSeatSwap");
                var no=popupRoot.GetComponentsInChildren<Button>().First(b=>b.name=="DeclineSeatSwap");
                Assert.AreSame(no,popup.FirstFocus);
                if(accept)yes.onClick.Invoke();else Assert.IsTrue(popup.Back());
                Assert.AreEqual(21,_host.Replied);Assert.AreEqual(accept,_host.Accepted);
                yes.onClick.Invoke();Assert.AreEqual(accept,_host.Accepted);
            }
            finally{Object.DestroyImmediate(popupRoot);}
        }
        [Test] public void HumanSlotOffersSwitchAndOwnSlotHasNoAction()
        {
            Assert.IsNull(Button("TakeSeat0")); Assert.IsNotNull(Button("TakeSeat1"));
            Assert.AreEqual("SWITCH",HubKit.LabelOf((HubButton)Button("TakeSeat1")).text);
            Assert.AreEqual(-1,_host.Requested);
        }
    }
}
