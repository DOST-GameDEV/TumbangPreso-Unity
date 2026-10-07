using System.Collections;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.Video;

namespace TumbangPreso.PlayTests
{
 public sealed class MapPreviewMotionSettingsTests
 {
  bool _reduced;
  [UnitySetUp]public IEnumerator Before(){_reduced=Settings.SettingsStore.Current.ReducedUiMotion;Settings.SettingsStore.Current.ReducedUiMotion=false;yield return PlayModeWorld.Reset();}
  [UnityTearDown]public IEnumerator After(){Settings.SettingsStore.Current.ReducedUiMotion=_reduced;yield return PlayModeWorld.Reset();}
  [UnityTest]public IEnumerator VisiblePreviewAdoptsMotionSettingWithoutVisibilityRefresh()
  {
   var root=new GameObject("Live motion setting",typeof(RectTransform),typeof(RawImage));var media=root.AddComponent<MapPreviewVideo>();
   Assert.IsTrue(media.Show(SceneFlow.Arena));float until=Time.realtimeSinceStartup+40;
   while(!media.HasFirstFrame&&Time.realtimeSinceStartup<until)yield return null;Assert.IsTrue(media.HasFirstFrame);
   var player=root.GetComponent<VideoPlayer>();var image=root.GetComponent<RawImage>();Assert.IsTrue(player.isPlaying);
   // A setting change is independent of closing/reopening its owning view.
   Settings.SettingsStore.Current.ReducedUiMotion=true;yield return null;yield return null;
   Assert.IsFalse(player.isPlaying,"Reduced motion must stop the currently visible map movie.");
   Assert.AreSame(MapPreviewVideo.PosterFor(SceneFlow.Arena),image.texture);
   Settings.SettingsStore.Current.ReducedUiMotion=false;yield return null;yield return null;
   Assert.IsTrue(player.isPlaying,"Turning motion back on must resume without a map/visibility change.");
   Assert.IsInstanceOf<RenderTexture>(image.texture);Object.Destroy(root);yield return null;
  }
  [UnityTest]public IEnumerator MotionDisabledDuringPreparationKeepsPosterUntilReenabled()
  {
   var root=new GameObject("Motion change during prepare",typeof(RectTransform),typeof(RawImage));
   var media=root.AddComponent<MapPreviewVideo>();Assert.IsTrue(media.Show(SceneFlow.Arena));
   var player=root.GetComponent<VideoPlayer>();var image=root.GetComponent<RawImage>();
   Assert.IsNotNull(player);Assert.IsFalse(player.isPrepared,"This control must change motion before native preparation completes.");
   Settings.SettingsStore.Current.ReducedUiMotion=true;yield return null;yield return null;
   float until=Time.realtimeSinceStartup+40;
   while(!player.isPrepared&&Time.realtimeSinceStartup<until)yield return null;
   Assert.IsTrue(player.isPrepared,"Native preparation must actually finish under reduced motion.");
   yield return null;yield return null;
   Assert.IsFalse(player.isPlaying,"A late prepared callback must respect the current motion preference.");
   Assert.AreSame(MapPreviewVideo.PosterFor(SceneFlow.Arena),image.texture,"Late frames must not replace the reduced-motion poster.");
   Settings.SettingsStore.Current.ReducedUiMotion=false;
   // Preparation can deliver its first frame while paused. Give Update its
   // setting-change turn even when HasFirstFrame is already true.
   yield return null;yield return null;
   until=Time.realtimeSinceStartup+40;
   while(!media.HasFirstFrame&&Time.realtimeSinceStartup<until)yield return null;
   Assert.IsTrue(media.HasFirstFrame);Assert.IsTrue(player.isPlaying,"Re-enabling motion must resume the prepared movie.");
   Assert.IsInstanceOf<RenderTexture>(image.texture);Object.Destroy(root);yield return null;
  }
 }
}
