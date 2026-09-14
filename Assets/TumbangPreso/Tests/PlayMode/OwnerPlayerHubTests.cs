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
    public sealed class OwnerPlayerHubTests
    {
        [UnitySetUp]public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown]public IEnumerator After()=>PlayModeWorld.Reset();
        [UnityTest,Timeout(90000)]
        public IEnumerator HubPagesKeepProfileDraftsAcrossNavigationAndServiceRefresh()
        {
            SceneFlow.Networked=false;SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            PlaySelectionScreen.RequestedLobbyMode=LobbyMode.Practice;
            yield return SceneManager.LoadSceneAsync(SceneFlow.MatchSetup);yield return null;
            Press(Find("ProfileButton"));yield return null;
            var hub=Object.FindFirstObjectByType<PlayerHub>();Assert.IsTrue(hub.IsOpen);
            var canvas=GameObject.Find("OwnerPlayerHubCanvas").GetComponent<Canvas>();
            Assert.IsEmpty(canvas.GetComponentsInChildren<GodotButton>(true));Assert.IsEmpty(canvas.GetComponentsInChildren<TumpSurface>(true));
            var name=canvas.GetComponentsInChildren<InputField>().First(f=>f.name=="PlayerNameEdit");
            string accountName=GameServices.Account?.DisplayName,localName=TumbangPreso.Settings.SettingsStore.Current.PlayerName;
            name.text="Draft_Player";hub.SendMessage("OnDataChanged");yield return null;
            Assert.AreEqual("Draft_Player",canvas.GetComponentsInChildren<InputField>().First(f=>f.name=="PlayerNameEdit").text);
            yield return TumpUiCapture.Capture("OwnerHub-profile-v1",canvas,1920,1080,false);
            foreach(string tab in new[]{"Friends","Career","Matches","Account"})
            {
                Press(Find("HubTab"+tab));yield return null;
                Assert.IsNotEmpty(canvas.GetComponentsInChildren<Text>().First(t=>t.name=="HubPageTitle").text);
                yield return TumpUiCapture.Capture("OwnerHub-"+tab+"-v1",canvas,1280,800,false);
            }
            Press(Find("HubTabProfile"));yield return null;
            Assert.AreEqual("Draft_Player",canvas.GetComponentsInChildren<InputField>().First(f=>f.name=="PlayerNameEdit").text);
            var scroll=canvas.GetComponentsInChildren<ScrollRect>().First(s=>s.name=="HubRows");
            scroll.verticalNormalizedPosition=0;yield return null;
            Press(canvas.GetComponentsInChildren<Button>().First(b=>b.transform.parent.name=="Group_Optional details"));yield return null;
            var bio=canvas.GetComponentsInChildren<InputField>().First(f=>f.name=="ProfileBio");bio.text="A profile draft kept while browsing.";
            hub.SendMessage("OnDataChanged");yield return null;
            Assert.AreEqual("A profile draft kept while browsing.",canvas.GetComponentsInChildren<InputField>().First(f=>f.name=="ProfileBio").text);
            scroll.verticalNormalizedPosition=.7f;yield return null;
            Press(canvas.GetComponentsInChildren<Button>().First(b=>b.transform.parent.name=="Group_Optional details"));yield return null;
            Assert.IsFalse(canvas.GetComponentsInChildren<InputField>().Any(f=>f.name=="ProfileBio"));
            var draft=typeof(PlayerHub).GetMethod("OwnerDraftValue",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            Assert.AreEqual("A profile draft kept while browsing.",draft.Invoke(hub,new object[]{"ProfileBio",""}));
            Assert.AreEqual(accountName,GameServices.Account?.DisplayName,"Typing must not submit account changes.");
            Assert.AreEqual(localName,TumbangPreso.Settings.SettingsStore.Current.PlayerName);
            Press(Find("ClosePlayerHub"));yield return null;Assert.IsFalse(hub.IsOpen);
            Assert.IsTrue(GameObject.Find("OwnerPreparationCanvas").activeInHierarchy);
        }
        [UnityTest,Timeout(90000)]
        public IEnumerator PopulatedCareerAndHistoryRenderRealColumnsWithoutSavingFixtureData()
        {
            SceneFlow.Networked=false;SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            PlaySelectionScreen.RequestedLobbyMode=LobbyMode.Practice;
            yield return SceneManager.LoadSceneAsync(SceneFlow.MatchSetup);yield return null;
            var profile=GameServices.Career?.Profile;Assert.IsNotNull(profile);
            string before=JsonUtility.ToJson(profile);
            try
            {
                profile.Xp=2500;profile.Rank.MatchesThisSeason=9;profile.Rank.Rating=1800;profile.Rank.Deviation=50;
                var totals=ProfileRules.ModeFor(profile,"Classic").Totals;totals.Matches=12;totals.Wins=4;
                totals.Placements[0]=4;totals.Placements[1]=3;totals.Placements[2]=3;totals.Placements[3]=2;
                Press(Find("ProfileButton"));yield return null;var hub=Object.FindFirstObjectByType<PlayerHub>();
                var canvas=GameObject.Find("OwnerPlayerHubCanvas").GetComponent<Canvas>();
                Press(Find("HubTabCareer"));yield return null;
                Assert.That(canvas.GetComponentsInChildren<Text>().Any(t=>t.text.Contains(RatingRules.TierName(RatingRules.TierFor(1800)))));
                yield return TumpUiCapture.Capture("OwnerHub-career-populated-v1",canvas,1920,1080,false);
                var record=new MatchRecord{MatchId="ui-fixture-only",Mode="Classic",MapId=SceneFlow.Eskinita,Rounds=4,
                    DurationSeconds=360,PlayedUtc="2026-09-15T12:00:00Z",WinningSlot=0,DefenderByRound=new[]{0,1,2,3},Players=new PlayerMatchStats[4]};
                for(int i=0;i<4;i++)record.Players[i]=new PlayerMatchStats{Slot=i,PlayerId=i==0?TumbangPreso.Net.CareerStore.LocalPlayerId:"ui-only-"+i,
                    Handle="Review Player "+(i+1),Placement=i+1,Score=2400-i*350,Throws=12+i,Knockdowns=3+i,Retrievals=8+i,Tags=2,Sabotages=1,DefenceTicks=60};
                typeof(PlayerHub).GetField("_shown",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
                    .SetValue(hub,new List<MatchRecord>{record});
                Press(Find("HubTabMatches"));yield return null;
                Press(Find("OpenMatchDetail"));yield return null;
                Assert.AreEqual(4,canvas.GetComponentsInChildren<RectTransform>().Count(r=>r.name=="PlayerStats"));
                Assert.IsTrue(canvas.GetComponentsInChildren<Text>().Any(t=>t.name=="DefendersByRound" && t.text.Contains("R4: P4")));
                yield return TumpUiCapture.Capture("OwnerHub-scorecard-v1",canvas,1920,1080,false);
                Press(Find("CloseMatchDetail"));yield return null;
                Assert.IsFalse(canvas.GetComponentsInChildren<RectTransform>().Any(r=>r.name=="PlayerStats"));
                Press(Find("ClosePlayerHub"));yield return null;
            }
            finally{JsonUtility.FromJsonOverwrite(before,profile);}
        }
        private static Button Find(string name)=>Object.FindObjectsByType<Button>().First(b=>b.name==name && b.isActiveAndEnabled);
        private static void Press(Button button)
        {
            Canvas.ForceUpdateCanvases();var rect=(RectTransform)button.transform;
            var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,
                position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center))};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            Assert.IsNotEmpty(hits,button.name);Assert.AreEqual(button,hits[0].gameObject.GetComponentInParent<Button>(),button.name+" is covered");
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        }
    }
}
