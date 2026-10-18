using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Net;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.Video;

namespace TumbangPreso.PlayTests
{
 public sealed class MapVotePreviewTests
 {
  [UnitySetUp]public IEnumerator Before(){yield return PlayModeWorld.Reset();}
  [UnityTearDown]public IEnumerator After(){NetSession.Instance?.Stop();yield return PlayModeWorld.Reset();}
  [UnityTest,Timeout(120000)]public IEnumerator NormalHostBallotSupportsPreviewThenLock()
  {
   yield return HubFlowTests.OpenHome();
   var hosting=TumpHub.Current.Host.HostRoom("MAP PREVIEW CHECK",SceneFlow.Eskinita,TumbangPreso.Core.GameMode.HeroStrike,RoomVisibility.Private,false);
   while(!hosting.IsCompleted)yield return null;
   Assert.IsFalse(hosting.IsFaulted);Assert.IsTrue(string.IsNullOrEmpty(hosting.Result));
   MatchRpc.Instance.HostBeginQueueMapVote();
   float until=Time.realtimeSinceStartup+5;while(!(TumpHub.Current.Top is HubMapVote)&&Time.realtimeSinceStartup<until)yield return null;
   var vote=TumpHub.Current.Top as HubMapVote;Assert.IsNotNull(vote);Assert.IsTrue(TumpHub.Current.Host.MapVoting);
   var cards=vote.GetComponentsInChildren<HubButton>();Assert.AreEqual(SceneFlow.Maps.Length+1,cards.Length);
   Assert.That(TumpHub.Current.Host.MapVoteSecondsLeft,Is.GreaterThan(0).And.LessThanOrEqualTo(12));
   foreach(var picture in vote.GetComponentsInChildren<RawImage>())Assert.IsNotNull(picture.texture);
   int arena=System.Array.IndexOf(SceneFlow.Maps,SceneFlow.Arena);var attended=cards.Single(c=>c.name=="VoteMap"+arena);
   ExecuteEvents.Execute(attended.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerEnterHandler);yield return null;
   Assert.AreEqual(1,vote.GetComponentsInChildren<VideoPlayer>().Count(p=>p.clip!=null));
   var next=cards.Single(c=>c.name=="VoteMap1");
   ExecuteEvents.Execute(next.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerEnterHandler);yield return null;
   Assert.AreEqual(1,vote.GetComponentsInChildren<VideoPlayer>().Count(p=>p.clip!=null));
   yield return HubFlowTests.Press("VoteMap1");
   Assert.AreEqual(-1,TumpHub.Current.Host.MapVoteFor(NetAuthority.LocalSlot),"Browsing must not cast a vote.");
   yield return HubFlowTests.Press("LockMapVote");
   Assert.AreEqual(1,TumpHub.Current.Host.MapVoteFor(NetAuthority.LocalSlot));
  }
  [UnityTest,Timeout(120000)]public IEnumerator UnansweredBallotResolvesAtTheNormalDeadline()
  {
   yield return HubFlowTests.OpenHome();
   var hosting=TumpHub.Current.Host.HostRoom("MAP DEADLINE CHECK",SceneFlow.Eskinita,TumbangPreso.Core.GameMode.HeroStrike,RoomVisibility.Private,false);
   while(!hosting.IsCompleted)yield return null;
   Assert.IsFalse(hosting.IsFaulted);Assert.IsTrue(string.IsNullOrEmpty(hosting.Result));
   float opened=Time.realtimeSinceStartup;MatchRpc.Instance.HostBeginQueueMapVote();
   float until=opened+15;
   while(TumpHub.Current.Host.MapVoteWinner<0&&Time.realtimeSinceStartup<until)yield return null;
   Assert.AreEqual(0,TumpHub.Current.Host.MapVoteWinner,"No votes retain the current court.");
   Assert.That(Time.realtimeSinceStartup-opened,Is.GreaterThanOrEqualTo(11).And.LessThan(15));
   Assert.AreEqual(-1,TumpHub.Current.Host.MapVoteFor(NetAuthority.LocalSlot));
   var vote=TumpHub.Current.Top as HubMapVote;Assert.IsNotNull(vote);
   Assert.IsFalse(vote.GetComponentsInChildren<HubButton>().Single(c=>c.name=="LockMapVote").interactable);
  }
 }
}

