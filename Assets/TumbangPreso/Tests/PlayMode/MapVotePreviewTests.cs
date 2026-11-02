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
  bool _reduced;
  // Reduced UI motion keeps the poster and creates no decoder, so this fixture states its setting.
  [UnitySetUp]public IEnumerator Before(){_reduced=Settings.SettingsStore.Current.ReducedUiMotion;Settings.SettingsStore.Current.ReducedUiMotion=false;yield return PlayModeWorld.Reset();}
  [UnityTearDown]public IEnumerator After(){NetSession.Instance?.Stop();yield return PlayModeWorld.Reset();Settings.SettingsStore.Current.ReducedUiMotion=_reduced;}
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
   Canvas.ForceUpdateCanvases();
   foreach(var label in vote.GetComponentsInChildren<Text>().Where(text=>text.name=="CourtName"))
    Assert.LessOrEqual(label.preferredWidth,label.rectTransform.rect.width+1,label.text+" exceeds its court card.");
   var status=vote.GetComponentsInChildren<Text>().Single(text=>text.name=="BallotStatus").rectTransform;
   var row=vote.GetComponentsInChildren<RectTransform>().Single(rect=>rect.name=="MapChoices");
   var statusCorners=new Vector3[4];var rowCorners=new Vector3[4];status.GetWorldCorners(statusCorners);row.GetWorldCorners(rowCorners);
   Assert.GreaterOrEqual(statusCorners[0].y,rowCorners[1].y,"The court cards cover the ballot status.");
   int arena=System.Array.IndexOf(SceneFlow.Maps,SceneFlow.Arena);var attended=cards.Single(c=>c.name=="VoteMap"+arena);
   ExecuteEvents.Execute(attended.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerEnterHandler);yield return null;
   Assert.AreEqual(1,vote.GetComponentsInChildren<VideoPlayer>().Count(p=>p.clip!=null));
   var next=cards.Single(c=>c.name=="VoteMap1");
   ExecuteEvents.Execute(next.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerEnterHandler);yield return null;
   Assert.AreEqual(1,vote.GetComponentsInChildren<VideoPlayer>().Count(p=>p.clip!=null));
   yield return HubFlowTests.Press("VoteMap1");
   Assert.AreEqual(-1,TumpHub.Current.Host.MapVoteFor(NetAuthority.LocalSlot),"Browsing must not cast a vote.");
   foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,720)})
    yield return TumpUiCapture.Capture("FiveMapVote-"+size.x+"x"+size.y,TumpHub.Current.Canvas,size.x,size.y,false,checkActionBounds:true);
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
   // The ballot redraws on its next hub tick. Confirm and the host both refuse a vote after the winner.
   yield return null;
   Assert.IsFalse(vote.GetComponentsInChildren<HubButton>().Single(c=>c.name=="LockMapVote").interactable);
  }
 }
}

