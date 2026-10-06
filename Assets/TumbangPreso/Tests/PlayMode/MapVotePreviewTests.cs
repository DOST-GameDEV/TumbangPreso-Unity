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
  [UnityTest,Timeout(120000)]public IEnumerator LiveHostBallotHasReadablePreviewsAndOneAttendedDecoder()
  {
   yield return HubFlowTests.OpenHome();
   var hosting=TumpHub.Current.Host.HostRoom("MAP PREVIEW CHECK",SceneFlow.Eskinita,TumbangPreso.Core.GameMode.HeroStrike,RoomVisibility.Private,false);
   while(!hosting.IsCompleted)yield return null;
   Assert.IsFalse(hosting.IsFaulted);Assert.IsTrue(string.IsNullOrEmpty(hosting.Result));
   MatchRpc.Instance.HostBeginQueueMapVote();
   float until=Time.realtimeSinceStartup+5;while(!(TumpHub.Current.Top is HubMapVote)&&Time.realtimeSinceStartup<until)yield return null;
   var vote=TumpHub.Current.Top as HubMapVote;Assert.IsNotNull(vote);Assert.IsTrue(TumpHub.Current.Host.MapVoting);
   var cards=vote.GetComponentsInChildren<HubButton>();Assert.AreEqual(SceneFlow.Maps.Length,cards.Length);
   foreach(var picture in vote.GetComponentsInChildren<RawImage>()){Assert.IsNotNull(picture.texture);Assert.Greater(picture.rectTransform.rect.width,350);}
   foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,720)})
   {
    yield return TumpUiCapture.Capture("MapVote-preview-"+size.x+"x"+size.y,TumpHub.Current.Canvas,size.x,size.y,false,checkActionBounds:true);
   }
   int arena=System.Array.IndexOf(SceneFlow.Maps,SceneFlow.Arena);var attended=cards.Single(c=>c.name=="VoteMap"+arena);
   ExecuteEvents.Execute(attended.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerEnterHandler);yield return null;
   Assert.AreEqual(1,vote.GetComponentsInChildren<VideoPlayer>().Count(p=>p.clip!=null));
   var next=cards.Single(c=>c.name=="VoteMap1");
   ExecuteEvents.Execute(next.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerEnterHandler);yield return null;
   Assert.AreEqual(0,vote.GetComponentsInChildren<VideoPlayer>().Count(p=>p.clip!=null));
   yield return HubFlowTests.Press("VoteMap1");
   Assert.AreEqual(1,TumpHub.Current.Host.MapVoteFor(NetAuthority.LocalSlot));
  }
 }
}

