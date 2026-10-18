using System.Collections;
using System.Linq;
using System.Reflection;
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
 public sealed class MapVoteBackgroundTests
 {
  [UnitySetUp]public IEnumerator Before(){yield return PlayModeWorld.Reset();}
  [UnityTearDown]public IEnumerator After(){NetSession.Instance?.Stop();yield return PlayModeWorld.Reset();}
  [UnityTest,Timeout(180000)]public IEnumerator BrowsingChangesTheBackgroundBeforeExplicitVoteConfirmation()
  {
   yield return HubFlowTests.OpenHome();
   var hosting=TumpHub.Current.Host.HostRoom("BACKGROUND VOTE",SceneFlow.Eskinita,TumbangPreso.Core.GameMode.HeroStrike,RoomVisibility.Private,false);
   while(!hosting.IsCompleted)yield return null;Assert.IsFalse(hosting.IsFaulted);Assert.IsTrue(string.IsNullOrEmpty(hosting.Result));
   MatchRpc.Instance.HostBeginQueueMapVote();
   // Extend only this controlled visual fixture's host window while three
   // viewport captures run. This does not qualify the usual12-second pacing.
   typeof(MatchRpc).GetField("_queueVoteEnds",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(MatchRpc.Instance,Time.unscaledTime+80);
   float until=Time.realtimeSinceStartup+5;while(!(TumpHub.Current.Top is HubMapVote)&&Time.realtimeSinceStartup<until)yield return null;
   var vote=TumpHub.Current.Top as HubMapVote;Assert.IsNotNull(vote);
   var background=vote.transform.Find("SelectedCourtBackground").GetComponent<RawImage>();Assert.IsNotNull(background.texture);
   int seat=NetAuthority.LocalSlot;Assert.AreEqual(-1,TumpHub.Current.Host.MapVoteFor(seat));
   int arena=System.Array.IndexOf(SceneFlow.Maps,SceneFlow.Arena);var cards=vote.GetComponentsInChildren<HubButton>();
   var attended=cards.Single(c=>c.name=="VoteMap"+arena);
   ExecuteEvents.Execute(attended.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerEnterHandler);yield return null;
   Assert.AreEqual(SceneFlow.Arena,background.GetComponent<MapPreviewVideo>().Map);Assert.AreEqual(-1,TumpHub.Current.Host.MapVoteFor(seat),"Hover must not cast a ballot.");
   foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,720),new Vector2Int(1600,680)})
    yield return TumpUiCapture.Capture("MapVote-background-"+size.x+"x"+size.y,TumpHub.Current.Canvas,size.x,size.y,false,checkActionBounds:true);
   yield return HubFlowTests.Press("VoteMap1");Assert.AreEqual(-1,TumpHub.Current.Host.MapVoteFor(seat),"Choosing a thumbnail must only preview it.");
   Assert.AreEqual(SceneFlow.BayanPlaza,background.GetComponent<MapPreviewVideo>().Map);
   yield return HubFlowTests.Press("LockMapVote");Assert.AreEqual(1,TumpHub.Current.Host.MapVoteFor(seat));
   var confirm=cards.Single(c=>c.name=="LockMapVote");Assert.IsFalse(confirm.interactable);
   yield return HubFlowTests.Press("VoteMap"+arena);Assert.AreEqual(1,TumpHub.Current.Host.MapVoteFor(seat),"Browsing after a vote must not silently change it.");
   Assert.IsTrue(confirm.interactable);yield return HubFlowTests.Press("LockMapVote");Assert.AreEqual(arena,TumpHub.Current.Host.MapVoteFor(seat));
   yield return null;Assert.AreEqual(1,vote.GetComponentsInChildren<VideoPlayer>().Count(p=>p.clip!=null),"Only the background owns a decoder.");
  }
  [UnityTest,Timeout(180000)]public IEnumerator EveryRecordedMapCanDecodeWithoutInstantiatingItsArena()
  {
   var root=new GameObject("All map recordings",typeof(RectTransform),typeof(RawImage));var media=root.AddComponent<MapPreviewVideo>();
   foreach(string map in SceneFlow.Maps)
   {
    Assert.IsTrue(media.Show(map),map+" poster missing");float until=Time.realtimeSinceStartup+40;
    while(!media.HasFirstFrame&&Time.realtimeSinceStartup<until)yield return null;
    Assert.IsTrue(media.HasFirstFrame,map+" failed to decode");
    Assert.IsFalse(UnityEngine.SceneManagement.SceneManager.GetSceneByName(map).isLoaded,map+" unnecessarily loaded live");
    var player=root.GetComponent<VideoPlayer>();Assert.AreEqual(1920,player.clip.width);Assert.AreEqual(1080,player.clip.height);
    Assert.AreEqual(780UL,player.clip.frameCount);Assert.That(player.clip.frameRate,Is.EqualTo(30).Within(.01));
    bool looped=false;VideoPlayer.EventHandler loop=p=>looped=true;player.loopPointReached+=loop;
    player.time=player.clip.length-.3;until=Time.realtimeSinceStartup+8;
    while(!looped&&Time.realtimeSinceStartup<until)yield return null;
    player.loopPointReached-=loop;Assert.IsTrue(looped,map+" failed to loop after seeking its final frames");
    Assert.IsNotNull(root.GetComponent<RawImage>().texture);
    media.Stop();yield return null;Assert.IsNull(root.GetComponent<VideoPlayer>());
   }
  }
 }
}
