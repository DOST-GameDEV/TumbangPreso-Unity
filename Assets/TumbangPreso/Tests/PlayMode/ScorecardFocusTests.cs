using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class ScorecardFocusTests
    {
        private const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        private PlayerAccount _account;
        private CareerStore _career;
        private SocialStore _social;
        private GameObject _root,_detail;
        private PlayerHub _hub;
        private Canvas _canvas;
        private Button _close;
        [UnitySetUp]
        public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _account=GameServices.Account;_career=GameServices.Career;_social=GameServices.Social;
            Service("Account",null);Service("Career",null);Service("Social",null);
            _root=new GameObject("Current scorecard focus scope");
            _hub=_root.AddComponent<PlayerHub>();_hub.Install();_hub.OpenTab(PlayerHub.Door.Profile);
            _canvas=Read<Canvas>("_canvas");
            var record=new MatchRecord{MatchId="focus-only-scorecard",Mode="Classic",MapId=SceneFlow.Eskinita,Rounds=4,
                PlayedUtc="2026-10-02T00:00:00Z",Players=new PlayerMatchStats[4]};
            for(int i=0;i<4;i++)record.Players[i]=new PlayerMatchStats{Slot=i,PlayerId="record-only-"+i,Handle="Recorded"+i,Placement=i+1,Score=400-i*50};
            typeof(PlayerHub).GetField("_shown",Hidden).SetValue(_hub,new List<MatchRecord>{record});
            var tab=System.Enum.Parse(typeof(PlayerHub).GetNestedType("Tab",BindingFlags.NonPublic),"Matches");
            typeof(PlayerHub).GetMethod("Show",Hidden).Invoke(_hub,new[]{tab});
            yield return null;
            _canvas.GetComponentsInChildren<Button>().Single(button=>button.name=="OpenMatchDetail").onClick.Invoke();
            _detail=Read<GameObject>("_detail");
            _close=_detail.GetComponentsInChildren<Button>().Single(button=>button.name=="CloseMatchDetail");
            yield return null;
            Assert.IsTrue(_detail.activeInHierarchy);Assert.IsTrue(_hub.IsOpen);
        }
        [UnityTearDown]
        public IEnumerator After()
        {
            if(_root!=null)Object.Destroy(_root);
            yield return null;
            Service("Account",_account);Service("Career",_career);Service("Social",_social);
            yield return PlayModeWorld.Reset();
        }
        private static void Service(string name,object value)=>typeof(GameServices).GetProperty(name).SetValue(null,value);
        private T Read<T>(string name)=>(T)typeof(PlayerHub).GetField(name,Hidden).GetValue(_hub);
        [UnityTest]
        public IEnumerator ClosingTheSelectedScorecardControlReturnsUsableHubFocus()
        {
            _close.Select();Assert.AreSame(_close.gameObject,EventSystem.current.currentSelectedGameObject);
            _close.onClick.Invoke();Assert.IsFalse(_detail.activeSelf);Assert.IsTrue(_hub.IsOpen);
            yield return null;
            var selected=EventSystem.current.currentSelectedGameObject;
            Assert.IsNotNull(selected);Assert.IsTrue(selected.activeInHierarchy,
                "Closing the scorecard left navigation inside the inactive modal.");
            Assert.IsFalse(selected.transform.IsChildOf(_detail.transform));
            Assert.IsTrue(selected.transform.IsChildOf(_canvas.transform));
        }
        [UnityTest]
        public IEnumerator ClosingDoesNotStealSelectionFromAnotherLiveScreen()
        {
            var owner=new GameObject("Other live screen");owner.transform.SetParent(_root.transform);
            var other=OwnerUiLayout.Canvas(owner.transform,"Other live canvas",600);
            var button=OwnerTextAction.Create(other.transform,"OtherLiveAction","OTHER",()=>{},0,0,200,80,28);
            button.Select();_close.onClick.Invoke();yield return null;
            Assert.IsFalse(_detail.activeSelf);Assert.AreSame(button.gameObject,EventSystem.current.currentSelectedGameObject);
        }
        [UnityTest]
        public IEnumerator ClosingAgainKeepsTheCurrentHistoryPageAndSelection()
        {
            var baseButton=_canvas.GetComponentsInChildren<Button>().First(button=>button.name=="HubTabMatches");
            baseButton.Select();_close.onClick.Invoke();var selected=EventSystem.current.currentSelectedGameObject;
            _close.onClick.Invoke();yield return null;
            Assert.IsTrue(_hub.IsOpen);Assert.IsFalse(_detail.activeSelf);
            Assert.AreEqual(1,Read<List<MatchRecord>>("_shown").Count);Assert.AreEqual(0,Read<int>("_page"));
            Assert.AreSame(selected,EventSystem.current.currentSelectedGameObject);
        }
    }
}
