using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
 public sealed class MapPreviewPendingFrameTests
 {
  const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
  bool preview;string map;
  [UnitySetUp]public IEnumerator Before(){preview=MatchInstaller.PreviewOnly;map=SceneFlow.SelectedMap;yield return PlayModeWorld.Reset();SceneFlow.SelectedMap=SceneFlow.Eskinita;}
  [UnityTearDown]public IEnumerator After(){yield return PlayModeWorld.Reset();MatchInstaller.PreviewOnly=preview;SceneFlow.SelectedMap=map;}
  [UnityTest,Timeout(120000)]public IEnumerator AColdMapSwapKeepsTheCompletedFrameUntilTheReplacementIsReady()
  {
   var root=new GameObject("Pending map frame",typeof(RectTransform),typeof(RawImage));var surface=root.GetComponent<RawImage>();var view=root.AddComponent<MapPreviewSurface>();
   view.Show(SceneFlow.Eskinita);float until=Time.realtimeSinceStartup+45;
   while(view.Showing!=SceneFlow.Eskinita&&Time.realtimeSinceStartup<until)yield return null;
   Assert.AreEqual(SceneFlow.Eskinita,view.Showing);yield return null;yield return null;
   Assert.IsNotNull(surface.texture);Assert.IsNotNull(view.Camera);Assert.IsTrue(view.Camera.enabled);
   var previous=surface.texture;view.Show(SceneFlow.Kanto);
   Assert.IsTrue((bool)typeof(MapPreviewSurface).GetField("_busy",Hidden).GetValue(view),"The replacement must be a real pending cold load.");
   view.SetRenderingEnabled(true);
   bool drewDuringPending=view.Camera.enabled;
   yield return null;yield return null;
   if((bool)typeof(MapPreviewSurface).GetField("_busy",Hidden).GetValue(view))drewDuringPending|=view.Camera.enabled;
   Assert.AreSame(previous,surface.texture,"Pending replacement discarded the completed texture.");
   until=Time.realtimeSinceStartup+45;
   while(view.Showing!=SceneFlow.Kanto&&Time.realtimeSinceStartup<until)yield return null;
   Assert.AreEqual(SceneFlow.Kanto,view.Showing);Assert.IsTrue(view.Camera.enabled);
   Assert.IsFalse(drewDuringPending,"The preview camera overwrites its visible texture while the old map is parked and the new map is still loading.");
   Object.Destroy(root);yield return null;
  }
 }
}
