using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class OwnerPreparationTests
    {
        [UnitySetUp]public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown]public IEnumerator After()=>PlayModeWorld.Reset();
        [UnityTest,Timeout(90000)]
        public IEnumerator OfflinePreparationKeepsMapModeLoadoutAndBackRoutes()
        {
            bool networked=SceneFlow.Networked;var rules=SceneFlow.SelectedRules.Clone();bool pinned=SceneFlow.RulesPinned;
            int seat=GameLaunch.SoloSeat;bool spectator=GameLaunch.Spectator;
            try
            {
                SceneFlow.Networked=false;SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
                GameLaunch.SoloSeat=1;GameLaunch.Spectator=false;PlaySelectionScreen.RequestedLobbyMode=LobbyMode.Practice;
                yield return SceneManager.LoadSceneAsync(SceneFlow.MatchSetup);yield return null;
                var canvas=GameObject.Find("OwnerPreparationCanvas").GetComponent<Canvas>();
                Assert.IsEmpty(canvas.GetComponentsInChildren<TumpSurface>());
                Assert.AreEqual("CLASSIC",canvas.GetComponentsInChildren<Text>().First(t=>t.name=="ModeValue").text);
                Assert.IsFalse(canvas.GetComponentsInChildren<Button>(true).First(b=>b.name=="JoinRoomButton").gameObject.activeInHierarchy);
                var preview=canvas.GetComponentInChildren<MapPreviewSurface>();
                float until=Time.realtimeSinceStartup+15;
                while(preview.GetComponent<RawImage>().texture==null && Time.realtimeSinceStartup<until)yield return null;
                Assert.IsNotNull(preview.GetComponent<RawImage>().texture,"Selected map never reached its actual preview.");
                var previewRect=((RectTransform)preview.transform).rect;
                Assert.That(previewRect.width/previewRect.height,Is.EqualTo(16f/9f).Within(.001f),"The real map must keep its render aspect.");
                foreach (var size in TumpUiCapture.PcViewports)
                    yield return TumpUiCapture.Capture("CourtPreparation-classic-" + size.x + "x" + size.y,
                        canvas, size.x, size.y, false, checkActionBounds: true);
                Press("ModeNextButton");yield return new WaitForSecondsRealtime(.2f);
                Assert.AreEqual(GameMode.HeroStrike,SceneFlow.SelectedMode);
                foreach (var size in TumpUiCapture.PcViewports)
                    yield return TumpUiCapture.Capture("CourtPreparation-hero-" + size.x + "x" + size.y,
                        canvas, size.x, size.y, false, checkActionBounds: true);
                string beforeMap=SceneFlow.SelectedMap;
                Press("MapNextButton");yield return new WaitForSecondsRealtime(.2f);
                Assert.AreNotEqual(beforeMap,SceneFlow.SelectedMap);
                Assert.That(canvas.GetComponentsInChildren<Text>().First(t=>t.name=="MapName").text,
                    Is.EqualTo(SceneFlow.PreviewFor(SceneFlow.SelectedMap).Name));
                Press("CustomGameButton");yield return null;
                var custom=GameObject.Find("OwnerCustomGameCanvas").GetComponent<Canvas>();
                Assert.IsEmpty(custom.GetComponentsInChildren<TumpSurface>());
                int rounds=SceneFlow.SelectedRules.Rounds;
                Press("RoundsNext");yield return null;
                Assert.That(SceneFlow.SelectedRules.Rounds,Is.EqualTo(rounds+1));
                Assert.That(custom.GetComponentsInChildren<Text>().First(t=>t.name=="RoundsValue").text,Is.EqualTo((rounds+1).ToString()));
                for (int format = 0; format < System.Enum.GetValues(typeof(MatchFormat)).Length; format++)
                {
                    foreach (var size in TumpUiCapture.PcViewports)
                        yield return TumpUiCapture.Capture("MatchSlate-format" + format + "-" + size.x + "x" + size.y,
                            custom, size.x, size.y, false, checkActionBounds: true);
                    Press("FormatNext"); yield return null;
                }
                Press("RoomRulesTab");yield return null;
                Press("PrivateNext");yield return null;
                var password=custom.GetComponentsInChildren<InputField>().First(i=>i.name=="RoomPassword");
                password.text="testroom";yield return null;
                Assert.AreEqual("testroom",SceneFlow.SelectedRules.Password);
                Press("BotsPrevious");yield return null;
                int botTier=TumbangPreso.Settings.SettingsStore.Current.AiDifficulty;
                foreach (var size in TumpUiCapture.PcViewports)
                    yield return TumpUiCapture.Capture("MatchSlate-room-" + size.x + "x" + size.y,
                        custom, size.x, size.y, false, checkActionBounds: true);
                Press("UseRulesButton");yield return new WaitForSecondsRealtime(.65f);
                Assert.IsFalse(Object.FindFirstObjectByType<CustomGameScreen>().IsOpen);
                Assert.AreEqual(botTier,TumbangPreso.Settings.SettingsStore.Current.AiDifficulty,"Preparation must not overwrite the chosen custom bot setting.");
                Press("LoadoutButton");yield return null;yield return null;
                Assert.IsNotNull(Object.FindFirstObjectByType<TumpPickerView>());
                // The existing picker owns its close callback; it must return to preparation.
                var picker=Object.FindFirstObjectByType<ConvertedCharacterSelect>();
                typeof(ConvertedCharacterSelect).GetMethod("Dismiss",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
                    .Invoke(picker,null);
                yield return null;
                Assert.True(canvas.gameObject.activeInHierarchy);
                Press("BackButton");yield return null;yield return null;
                Assert.IsNotNull(GameObject.Find("OwnerPlayCanvas"));
            }
            finally
            {
                SceneFlow.Networked=networked;SceneFlow.AdoptRemoteRules(rules);
                if(pinned)SceneFlow.PinSelectedRules(rules);else SceneFlow.UnpinSelectedRules();
                GameLaunch.SoloSeat=seat;GameLaunch.Spectator=spectator;PlaySelectionScreen.RequestedLobbyMode=null;
            }
        }
        [UnityTest,Timeout(90000)]
        public IEnumerator RankedPreparationKeepsRankedStakeAndAccountState()
        {
            SceneFlow.Networked=true;SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            PlaySelectionScreen.RequestedLobbyMode=LobbyMode.Ranked;
            yield return SceneManager.LoadSceneAsync(SceneFlow.MatchSetup);yield return null;
            var canvas=GameObject.Find("OwnerPreparationCanvas").GetComponent<Canvas>();
            Assert.AreEqual(QueueStake.Ranked,Object.FindFirstObjectByType<QueueCard>(FindObjectsInactive.Include).Stake);
            var view=Object.FindFirstObjectByType<OwnerPreparationView>();
            Assert.IsFalse(view.MatchChoices.activeInHierarchy);Assert.IsTrue(view.RankedSummary.activeInHierarchy);
            Assert.IsFalse(view.Primary.gameObject.activeInHierarchy);
            Assert.IsTrue(canvas.transform.Find("PreparationComposition/RankedRoute/SelectedRoute").gameObject.activeInHierarchy);
            if(GameServices.Account==null || GameServices.Account.IsGuest)
            {
                Assert.False(view.StartMatch.interactable);StringAssert.Contains("SIGN IN",view.RankedTitle.text);
                StringAssert.Contains("profile",view.RankedDetail.text);
            }
            float until=Time.realtimeSinceStartup+20;
            while(view.Preview.GetComponent<RawImage>().texture==null && Time.realtimeSinceStartup<until)yield return null;
            Assert.IsNotNull(view.Preview.GetComponent<RawImage>().texture);
            Assert.IsNotEmpty(view.RankedTitle.text);Assert.IsNotEmpty(view.RankedDetail.text);
            foreach (var size in TumpUiCapture.PcViewports)
                yield return TumpUiCapture.Capture("CourtPreparation-ranked-" + size.x + "x" + size.y,
                    canvas, size.x, size.y, false, checkActionBounds: true);
            Press("BackButton");yield return null;yield return null;
            Assert.IsNotNull(GameObject.Find("OwnerPlayCanvas"));
            SceneFlow.Networked=false;
        }
        [UnityTest]
        public IEnumerator ReadOnlyCustomRulesKeepTabsAndCloseWithoutChangingRules()
        {
            var previous=NetAuthority.Provider;NetAuthority.Provider=new ReadingClient();
            try
            {
            SceneFlow.Networked=true;SceneFlow.AdoptRemoteRules(CustomGameRules.Defaults(GameMode.Classic));
            var screen=CustomGameScreen.Ensure();screen.Open();yield return null;
            var canvas=GameObject.Find("OwnerCustomGameCanvas").GetComponent<Canvas>();
            var buttons=canvas.GetComponentsInChildren<Button>();
            Assert.IsFalse(buttons.First(b=>b.name=="RoundsNext").interactable);
            Assert.IsFalse(buttons.First(b=>b.name=="ResetRulesButton").interactable);
            int rounds=SceneFlow.SelectedRules.Rounds;
            foreach (var size in TumpUiCapture.PcViewports)
                yield return TumpUiCapture.Capture("MatchSlate-readonly-" + size.x + "x" + size.y,
                    canvas, size.x, size.y, false, checkActionBounds: true);
            Press("RoomRulesTab");yield return null;
            Assert.IsTrue(canvas.GetComponentsInChildren<Text>().Any(t=>t.name=="BotsValue"));
            Press("UseRulesButton");yield return null;
            Assert.IsFalse(screen.IsOpen);Assert.AreEqual(rounds,SceneFlow.SelectedRules.Rounds);
            SceneFlow.Networked=false;
            }
            finally{NetAuthority.Provider=previous;SceneFlow.Networked=false;}
        }
        // Exercise the view's existing authority boundary without making an external connection.
        private sealed class ReadingClient : INetProvider
        {
            public bool IsHost=>false;public bool IsNetworked=>true;public int LocalSlot=>1;
            public int LocalPeerId=>1;public bool IsSeatlessReferee=>false;
        }
        [UnityTest,Timeout(90000)]
        public IEnumerator FriendsHostKeepsChatHistoryWhileHiddenAndRoomControlsReachable()
        {
            var net=TumbangPreso.Net.NetSession.Ensure();
            var hosting=net.StartHostAsync(18658);
            while(!hosting.IsCompleted)yield return null;
            Assert.IsTrue(hosting.Result,net.Status);
            try
            {
                SceneFlow.Networked=true;PlaySelectionScreen.RequestedLobbyMode=LobbyMode.Custom;
                yield return SceneManager.LoadSceneAsync(SceneFlow.MatchSetup);yield return null;
                var view=Object.FindFirstObjectByType<OwnerPreparationView>();
                Assert.True(view.CopyCode.gameObject.activeInHierarchy);Assert.True(view.StartMatch.gameObject.activeInHierarchy);
                foreach (var size in TumpUiCapture.PcViewports)
                    yield return TumpUiCapture.Capture("CourtPreparation-friends-" + size.x + "x" + size.y,
                        view.Canvas, size.x, size.y, false, checkActionBounds: true);
                var chat=Object.FindFirstObjectByType<LobbyChat>();
                Assert.IsNotNull(chat,"Hidden chat must stay subscribed to incoming room messages.");
                Assert.IsFalse(chat.IsPresented);
                var local=typeof(LobbyChat).GetMethod("AddLocal",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                local.Invoke(chat,new object[]{"Local test note while the panel is closed."});
                Press("ChatButton");yield return null;
                Assert.True(chat.IsPresented);
                Assert.That(chat.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("Local test note")));
                foreach(var size in TumpUiCapture.PcViewports)
                    yield return TumpUiCapture.Capture("RoomChat-compact-"+size.x+"x"+size.y,view.Canvas,size.x,size.y,false,checkActionBounds:true);
                Press("ChatHistoryButton");yield return null;
                var transcript=chat.GetComponentsInChildren<Text>().First(t=>t.name=="FullTranscript");
                StringAssert.Contains("Local test note",transcript.text);
                foreach(var size in TumpUiCapture.PcViewports)
                    yield return TumpUiCapture.Capture("RoomChat-history-"+size.x+"x"+size.y,view.Canvas,size.x,size.y,false,checkActionBounds:true);
                Press("ChatHistoryBack");yield return null;Press("CloseChatButton");yield return null;
                Assert.False(chat.IsPresented);Assert.True(chat.isActiveAndEnabled);
                local.Invoke(chat,new object[]{"Second local note survives hiding."});
                Press("JoinRoomButton");yield return null;
                Assert.IsNotNull(GameObject.Find("OwnerJoinCanvas"));Press("CloseJoinButton");yield return null;
                Press("ChatButton");yield return null;
                Assert.That(chat.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("Second local note")));
                Press("CloseChatButton");yield return null;
                Press("BackButton");yield return null;yield return null;
                Assert.IsNotNull(GameObject.Find("OwnerPlayCanvas"));
            }
            finally{net.Stop();SceneFlow.Networked=false;PlaySelectionScreen.RequestedLobbyMode=null;}
        }
        private static void Press(string name)
        {
            var button=Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.name==name && b.isActiveAndEnabled);
            Canvas.ForceUpdateCanvases();var rect=(RectTransform)button.transform;var canvas=button.GetComponentInParent<Canvas>();
            var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,
                position=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,
                    rect.TransformPoint(rect.rect.center))};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            Assert.IsNotEmpty(hits,name);Assert.AreEqual(button,hits[0].gameObject.GetComponentInParent<Button>(),name+" is covered");
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        }
    }
}
