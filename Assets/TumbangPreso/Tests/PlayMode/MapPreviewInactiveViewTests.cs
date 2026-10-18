using System.Collections;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.Video;

namespace TumbangPreso.PlayTests
{
 public sealed class MapPreviewInactiveViewTests
 {
  [UnitySetUp]public IEnumerator Before(){yield return PlayModeWorld.Reset();}
  [UnityTearDown]public IEnumerator After(){yield return PlayModeWorld.Reset();}
  [UnityTest]public IEnumerator InactiveViewRetainsPosterAndDefersDecoderUntilShown()
  {
   var root=new GameObject("Prepared hidden map",typeof(RectTransform),typeof(RawImage));root.SetActive(false);
   var preview=root.AddComponent<MapPreviewVideo>();Assert.IsTrue(preview.Show(SceneFlow.Arena));
   Assert.IsNotNull(root.GetComponent<RawImage>().texture);Assert.IsNull(root.GetComponent<VideoPlayer>(),"An inactive screen must not open its native decoder.");
   root.SetActive(true);float until=Time.realtimeSinceStartup+40;
   while(!preview.HasFirstFrame&&Time.realtimeSinceStartup<until)yield return null;
   Assert.IsTrue(preview.HasFirstFrame,"Showing the prepared view must adopt its cached clip and decode.");
   Object.Destroy(root);yield return null;
  }
  [UnityTest]public IEnumerator DisabledPreviewComponentDefersDecoderUntilEnabled()
  {
   var root=new GameObject("Disabled map component",typeof(RectTransform),typeof(RawImage));
   var preview=root.AddComponent<MapPreviewVideo>();preview.enabled=false;
   Assert.IsTrue(preview.Show(SceneFlow.Eskinita));Assert.IsNotNull(root.GetComponent<RawImage>().texture);
   Assert.IsNull(root.GetComponent<VideoPlayer>(),"A disabled view component owns no active native decoder.");
   preview.enabled=true;float until=Time.realtimeSinceStartup+40;
   while(!preview.HasFirstFrame&&Time.realtimeSinceStartup<until)yield return null;
   Assert.IsTrue(preview.HasFirstFrame);Object.Destroy(root);yield return null;
  }
 }
}
