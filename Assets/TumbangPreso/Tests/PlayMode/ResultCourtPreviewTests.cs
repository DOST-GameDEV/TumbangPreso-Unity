using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.InputLayer;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.Video;

namespace TumbangPreso.PlayTests
{
 public sealed class ResultCourtPreviewTests
 {
  [UnitySetUp]public IEnumerator Before(){yield return PlayModeWorld.Reset();}
  [UnityTearDown]public IEnumerator After(){yield return PlayModeWorld.Reset();}
  static int[] Votes(MatchResult result)=>(int[])typeof(MatchResult).GetField("_mapVotes",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(result);
  static Button Button(string name)=>Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b=>b.name==name&&b.gameObject.activeInHierarchy);
  static void Press(string name){var button=Button(name);Assert.IsTrue(button.interactable,name);ExecuteEvents.Execute(button.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);}
  static IEnumerator OpenResults()
  {
   SceneFlow.Networked=false;SceneFlow.SelectedMap=SceneFlow.Eskinita;GameLaunch.SoloSeat=1;GameLaunch.Spectator=false;
   SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
   yield return SceneManager.LoadSceneAsync(SceneFlow.Eskinita);yield return new WaitForSecondsRealtime(.4f);
   var gate=Object.FindFirstObjectByType<ReadyGate>();Assert.IsNotNull(gate);gate.StartLocalCountdown();
   float until=Time.realtimeSinceStartup+8;
   while((gate.CountingDown||PresentationClock.Held||!GameServices.Round.RoundActive)&&Time.realtimeSinceStartup<until)yield return null;
   Assert.IsFalse(PresentationClock.Held,"The staged result must follow actual ready countdown release.");
   Assert.IsTrue(GameServices.Round.RoundActive);
   var result=Object.FindFirstObjectByType<MatchResult>();Assert.IsNotNull(result);result.OnMatchWon(-1);yield return null;
  }
  [UnityTest,Timeout(180000)]public IEnumerator ResultPreviewNeedsConfirmationAndPlaysWhileTheMatchIsPaused()
  {
   yield return OpenResults();var result=Object.FindFirstObjectByType<MatchResult>();
   Assert.IsTrue(Votes(result).All(v=>v==-1));int scenes=SceneManager.sceneCount;
   Press("ResultNextMap");yield return null;yield return null;
   var picker=Object.FindFirstObjectByType<CourtPreviewPicker>();Assert.IsNotNull(picker);
   var focus=picker.GetComponent<ScreenFocus>();focus.Rebuild();Assert.AreEqual(SceneFlow.Maps.Length+2,focus.Order.Count);
   Assert.IsTrue(EventSystem.current.currentSelectedGameObject.transform.IsChildOf(picker.transform));
   var image=picker.transform.Find("SelectedCourtBackground").GetComponent<RawImage>();Assert.IsNotNull(image.texture);
   int arena=System.Array.IndexOf(SceneFlow.Maps,SceneFlow.Arena);
   ExecuteEvents.Execute(Button("PreviewCourt"+arena).gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerEnterHandler);yield return null;
   Assert.IsTrue(Votes(result).All(v=>v==-1),"Hover must not submit a result ballot.");
   Press("PreviewCourt"+arena);yield return null;Assert.IsTrue(Votes(result).All(v=>v==-1));
   var media=image.GetComponent<MapPreviewVideo>();Assert.AreEqual(SceneFlow.Arena,media.Map);
   float until=Time.realtimeSinceStartup+40;while(!media.HasFirstFrame&&Time.realtimeSinceStartup<until)yield return null;
   Assert.IsTrue(media.HasFirstFrame);Assert.That(Time.timeScale,Is.EqualTo(0).Within(.001f));
   var player=image.GetComponent<VideoPlayer>();long frame=player.frame;yield return new WaitForSecondsRealtime(.5f);Assert.Greater(player.frame,frame);
   Assert.AreEqual(scenes,SceneManager.sceneCount,"Selecting a recording must not load another live arena.");
   var canvas=picker.GetComponentInParent<Canvas>();
   foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,720)})
    yield return TumpUiCapture.Capture("ResultCourtPicker-"+size.x+"x"+size.y,canvas,size.x,size.y,false,checkActionBounds:true);
   // Submit opened/browsed the picker; pointer confirmation uses the same control.
   ExecuteEvents.Execute(Button("ConfirmCourtVote").gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
   yield return null;
   Assert.AreEqual(arena,Votes(result)[1]);Assert.AreEqual(-1,Votes(result)[0],"Offline authority seat zero must not receive the human seat one's ballot.");
   Assert.IsNull(Object.FindFirstObjectByType<CourtPreviewPicker>());
   Assert.IsTrue(result.IsVisible);Assert.AreSame(Button("ResultNextMap").gameObject,EventSystem.current.currentSelectedGameObject);
   Assert.AreEqual(0,Object.FindObjectsByType<VideoPlayer>(FindObjectsSortMode.None).Count(p=>p.name=="SelectedCourtBackground"));
  }
  [UnityTest,Timeout(120000)]public IEnumerator CancelAndRetirementPreserveTheExistingBallotAndReleaseThePreview()
  {
   yield return OpenResults();var result=Object.FindFirstObjectByType<MatchResult>();result.HostReceiveMapVote(1,1);
   Press("ResultNextMap");yield return null;Press("PreviewCourt6");yield return null;Press("CancelCourtPreview");yield return null;
   Assert.AreEqual(1,Votes(result)[1]);Assert.IsTrue(result.IsVisible);Assert.IsNull(Object.FindFirstObjectByType<CourtPreviewPicker>());
   Press("ResultNextMap");yield return null;result.enabled=false;yield return null;
   Assert.IsNull(Object.FindFirstObjectByType<CourtPreviewPicker>());Assert.That(Time.timeScale,Is.EqualTo(1).Within(.001f));
   Assert.AreEqual(1,Votes(result)[1]);
   result.enabled=true;result.IsSpectator=true;result.OnMatchWon(-1);yield return null;
   Assert.IsFalse(Button("ResultNextMap").interactable);Assert.IsTrue(Votes(result).All(v=>v==-1));
   Assert.IsNull(Object.FindFirstObjectByType<CourtPreviewPicker>());
  }
 }
}
