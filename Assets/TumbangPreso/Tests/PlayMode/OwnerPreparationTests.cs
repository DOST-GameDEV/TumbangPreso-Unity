using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
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
            var settings = Settings.SettingsStore.Current;
            string saved = JsonUtility.ToJson(settings);
            var rules = SceneFlow.SelectedRules.Clone(); bool pinned = SceneFlow.RulesPinned;
            try
            {
                HubHome.Choice = 1;
                yield return HubFlowTests.OpenHome();
                Assert.AreEqual(GameMode.Classic, SceneFlow.SelectedMode);
                yield return HubFlowTests.Press("ModeCard");
                yield return HubFlowTests.Press("ClassicCard");
                yield return HubFlowTests.Press("HeroStrikeChoice");
                Assert.AreEqual(GameMode.HeroStrike, SceneFlow.SelectedMode);
                yield return HubFlowTests.Press("ModeCard");
                yield return HubFlowTests.Press("CustomCard");
                yield return HubFlowTests.Press("HostDoor");
                yield return HubFlowTests.Press("MapDropdown");
                yield return HubFlowTests.Press("Option1");
                Assert.AreEqual(SceneFlow.MapRegistry[1].Id, SceneFlow.SelectedMap);
                TumpHub.Current.Home();
                yield return HubFlowTests.Press("MenuButton");
                yield return HubFlowTests.Press("MenuMATCHRULES");
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

                Press("LoadoutButton"); yield return null;
                Assert.IsInstanceOf<HubLoadout>(TumpHub.Current.Top);
                Press("BackButton"); yield return null;
                Assert.IsInstanceOf<HubHome>(TumpHub.Current.Top);
            }
            finally
            {
                JsonUtility.FromJsonOverwrite(saved, settings);
                SceneFlow.AdoptRemoteRules(rules);
                if (pinned) SceneFlow.PinSelectedRules(rules); else SceneFlow.UnpinSelectedRules();
            }
        }
        [UnityTest,Timeout(90000)]
        public IEnumerator RankedPreparationKeepsRankedStakeAndAccountState()
        {
            int choice = Settings.SettingsStore.Current.HubQueueChoice;
            try
            {
                HubHome.Choice = 0;
                yield return HubFlowTests.OpenHome();
                Assert.AreEqual(QueueStake.Ranked, HubHome.ChoiceStake);
                Assert.AreEqual(GameMode.HeroStrike, SceneFlow.SelectedMode);
                Assert.IsTrue(TumpHub.Current.Top.GetComponentsInChildren<Text>().Any(t => t.text == "RANKED"));
                if (GameServices.Account == null || GameServices.Account.IsGuest)
                {
                    yield return HubFlowTests.Press("PlayButton");
                    Assert.IsFalse(HubQueueWatch.QueueRoom, "A guest must not enter ranked matchmaking.");
                    Assert.IsNotEmpty(TumbangPreso.Net.Matchmaker.Current.Refusal, "Ranked refusal must explain the account gate.");
                }
            }
            finally
            {
                TumpHub.Current?.Host.CancelQueue();
                Settings.SettingsStore.Current.HubQueueChoice = choice;
            }
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
                Assert.IsInstanceOf<HubLobby>(TumpHub.Current.Top);
                var canvas = TumpHub.Current.Canvas;
                Assert.IsTrue(canvas.GetComponentsInChildren<Button>().Any(b => b.name == "CodeChip"));
                Assert.IsTrue(canvas.GetComponentsInChildren<Button>().Any(b => b.name == "StartGame"));
                foreach (var size in TumpUiCapture.PcViewports)
                    yield return TumpUiCapture.Capture("CourtPreparation-friends-" + size.x + "x" + size.y,
                        canvas, size.x, size.y, false, checkActionBounds: true);
                var chat=Object.FindFirstObjectByType<LobbyChat>();
                Assert.IsNotNull(chat,"Hidden chat must stay subscribed to incoming room messages.");
                Assert.IsFalse(chat.IsPresented);
                var local=typeof(LobbyChat).GetMethod("AddLocal",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                local.Invoke(chat,new object[]{"Local test note while the panel is closed."});
                Press("ChatDoor");yield return null;
                Assert.True(chat.IsPresented);
                Assert.That(chat.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("Local test note")));
                foreach(var size in TumpUiCapture.PcViewports)
                    yield return TumpUiCapture.Capture("RoomChat-compact-"+size.x+"x"+size.y,canvas,size.x,size.y,false,checkActionBounds:true);
                Press("ChatHistoryButton");yield return null;
                Assert.IsTrue(TumpHub.Current.Canvas.enabled, "Nested chat history must not hide its parent canvas.");
                var transcript=chat.GetComponentsInChildren<Text>().First(t=>t.name=="FullTranscript");
                StringAssert.Contains("Local test note",transcript.text);
                foreach(var size in TumpUiCapture.PcViewports)
                    yield return TumpUiCapture.Capture("RoomChat-history-"+size.x+"x"+size.y,canvas,size.x,size.y,false,checkActionBounds:true);
                Press("ChatHistoryBack");yield return null;Press("CloseChatButton");yield return null;
                Assert.False(chat.IsPresented);Assert.True(chat.isActiveAndEnabled);
                local.Invoke(chat,new object[]{"Second local note survives hiding."});
                Press("CharacterDoor"); yield return null;
                Assert.IsInstanceOf<HubCharacterSelect>(TumpHub.Current.Top);
                TumpHub.Current.Back(); yield return null;
                Press("ChatDoor");yield return null;
                Assert.That(chat.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("Second local note")));
                Press("CloseChatButton");yield return null;
                Press("BackButton");yield return null;yield return null;
                Assert.IsInstanceOf<HubHome>(TumpHub.Current.Top);
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
