using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class ReplayLibraryPointerTests
    {
        private byte[] _location;
        private string Location=>Path.Combine(ProfilePaths.Root,"replay-folder.txt");
        [UnitySetUp]public IEnumerator Before(){yield return PlayModeWorld.Reset();_location=File.Exists(Location)?File.ReadAllBytes(Location):null;}
        [UnityTearDown]public IEnumerator After()
        {
            var playback=Object.FindAnyObjectByType<LocalReplayPlayback>();if(playback!=null)Object.Destroy(playback.gameObject);
            yield return PlayModeWorld.Reset();
            if(_location!=null)File.WriteAllBytes(Location,_location);else if(File.Exists(Location))File.Delete(Location);
        }
        private static List<RaycastResult> Hits(Button button,out PointerEventData pointer)
        {
            var canvas=button.GetComponentInParent<Canvas>();var rect=(RectTransform)button.transform;
            pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,
                position=RectTransformUtility.WorldToScreenPoint(canvas.worldCamera,rect.TransformPoint(rect.rect.center))};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            TestContext.WriteLine("row="+button.name+" point="+pointer.position+" rect="+rect.rect+" mode="+canvas.renderMode+
                " camera="+canvas.worldCamera+" enabled="+canvas.enabled+" hits="+hits.Count);
            return hits;
        }
        [UnityTest]public IEnumerator ActualAsynchronousReplayRowReceivesMaturePointerClick()
        {
            string recording=Environment.GetEnvironmentVariable("TUMP_REPLAY_EXISTING");
            if(string.IsNullOrEmpty(recording))Assert.Ignore("Set TUMP_REPLAY_EXISTING to an actual retained recording.");
            Assert.IsTrue(LocalReplayStore.SetFolder(Path.GetDirectoryName(recording),out string error),error);
            yield return SceneManager.LoadSceneAsync(SceneFlow.MatchSetup);float until=Time.realtimeSinceStartup+15;
            while(TumpHub.Current==null&&Time.realtimeSinceStartup<until)yield return null;
            Assert.IsNotNull(TumpHub.Current);TumpHub.Current.Push<HubReplays>();
            until=Time.realtimeSinceStartup+10;GameObject row=null;
            while(row==null&&Time.realtimeSinceStartup<until){row=GameObject.Find("Replay0");yield return null;}
            Assert.IsNotNull(row);var button=row.GetComponent<Button>();
            Canvas.ForceUpdateCanvases();Hits(button,out _);
            yield return new WaitForSecondsRealtime(.2f);Canvas.ForceUpdateCanvases();
            var hits=Hits(button,out var pointer);Assert.Greater(hits.Count,0,"The mature replay row must be raycastable.");
            var target=ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
            Assert.AreSame(button.gameObject,target,"The mature row must not be covered by another UI element.");
            ExecuteEvents.Execute(target,pointer,ExecuteEvents.pointerClickHandler);
            until=Time.realtimeSinceStartup+15;
            while(Object.FindAnyObjectByType<LocalReplayPlayback>()?.Frame==null&&Time.realtimeSinceStartup<until)yield return null;
            var viewer=Object.FindAnyObjectByType<LocalReplayPlayback>();Assert.IsNotNull(viewer);Assert.IsNull(viewer.Error,viewer.Error);Assert.IsNotNull(viewer.Frame);
        }
    }
}
