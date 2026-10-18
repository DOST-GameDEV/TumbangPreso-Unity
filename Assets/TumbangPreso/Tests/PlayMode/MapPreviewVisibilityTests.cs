using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class MapPreviewVisibilityTests
    {
        [UnitySetUp]public IEnumerator Before(){yield return PlayModeWorld.Reset();}
        [UnityTearDown]public IEnumerator After(){yield return PlayModeWorld.Reset();}

        [UnityTest]public IEnumerator HiddenPreparationRetainsItsPosterAndResumesDecoding()
        {
            var root=new GameObject("Interrupted map preparation",typeof(RectTransform),typeof(RawImage));
            var preview=root.AddComponent<MapPreviewVideo>();
            Assert.IsTrue(preview.Show(SceneFlow.Arena));
            var poster=root.GetComponent<RawImage>().texture;
            Assert.IsNotNull(poster,"The map must be visible before decoding.");
            preview.SetVisible(false);
            // Simulate time spent on another screen without waiting thirty wall
            // seconds. The real Update and native player handle the interruption.
            typeof(MapPreviewVideo).GetField("_prepareStarted",BindingFlags.Instance|BindingFlags.NonPublic)
                .SetValue(preview,Time.realtimeSinceStartup-35f);
            yield return null;yield return null;
            Assert.IsFalse((bool)typeof(MapPreviewVideo).GetField("_failed",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(preview),
                "Time spent hidden must not exhaust the visible first-frame budget.");
            Assert.AreSame(poster,root.GetComponent<RawImage>().texture);
            Assert.IsFalse(UnityEngine.SceneManagement.SceneManager.GetSceneByName(SceneFlow.Arena).isLoaded);
            Time.timeScale=0;
            preview.SetVisible(true);
            float until=Time.realtimeSinceStartup+40;
            while(!preview.HasFirstFrame&&Time.realtimeSinceStartup<until)yield return null;
            Assert.IsTrue(preview.HasFirstFrame,"Returning to the screen must resume the native decoder.");
            var player=root.GetComponent<UnityEngine.Video.VideoPlayer>();long first=player.frame;
            yield return new WaitForSecondsRealtime(.5f);
            Assert.Greater(player.frame,first,"A result-board pause must not freeze its map preview.");
            Time.timeScale=1;
            Object.Destroy(root);yield return null;
        }
    }
}
