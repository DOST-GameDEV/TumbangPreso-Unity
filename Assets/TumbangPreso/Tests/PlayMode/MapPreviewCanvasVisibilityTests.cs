using System.Collections;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.Video;

namespace TumbangPreso.PlayTests
{
 public sealed class MapPreviewCanvasVisibilityTests
 {
  bool _reduced;
  [UnitySetUp]public IEnumerator Before(){_reduced=Settings.SettingsStore.Current.ReducedUiMotion;Settings.SettingsStore.Current.ReducedUiMotion=false;yield return PlayModeWorld.Reset();}
  [UnityTearDown]public IEnumerator After(){Settings.SettingsStore.Current.ReducedUiMotion=_reduced;yield return PlayModeWorld.Reset();}
  static MapPreviewVideo View(Canvas canvas)
  {
   var image=new GameObject("Recorded court",typeof(RectTransform),typeof(RawImage));
   image.transform.SetParent(canvas.transform,false);
   return image.AddComponent<MapPreviewVideo>();
  }
  [UnityTest]public IEnumerator HiddenCanvasKeepsPosterAndDefersItsDecoder()
  {
   var root=new GameObject("Hidden canvas",typeof(RectTransform),typeof(Canvas));
   var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.enabled=false;
   var media=View(canvas);Assert.IsTrue(media.Show(SceneFlow.Arena));
   // Graphic.canvas skips disabled canvases; inspect the actual owning parent.
   Assert.AreSame(canvas,media.GetComponentInParent<Canvas>(true));
   Assert.IsNotNull(media.GetComponent<RawImage>().texture);
   Assert.IsNull(media.GetComponent<VideoPlayer>(),"A non-drawing Canvas must not start the preview decoder.");
   canvas.enabled=true;float until=Time.realtimeSinceStartup+40;
   while(!media.HasFirstFrame&&Time.realtimeSinceStartup<until)yield return null;
   Assert.IsTrue(media.HasFirstFrame,"Showing the Canvas must adopt the cached clip.");
   Object.Destroy(root);yield return null;
  }
  [UnityTest]public IEnumerator CanvasOverlayPausesAndResumesTheOwnedMovie()
  {
   var root=new GameObject("Overlay canvas",typeof(RectTransform),typeof(Canvas));
   var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
   var media=View(canvas);Assert.IsTrue(media.Show(SceneFlow.Eskinita));float until=Time.realtimeSinceStartup+40;
   while(!media.HasFirstFrame&&Time.realtimeSinceStartup<until)yield return null;
   Assert.IsTrue(media.HasFirstFrame);var player=media.GetComponent<VideoPlayer>();Assert.IsTrue(player.isPlaying);
   // TumpHub hides its Canvas for an older overlay without disabling its children.
   canvas.enabled=false;yield return null;yield return null;
   Assert.IsTrue(media.isActiveAndEnabled);Assert.IsFalse(canvas.isActiveAndEnabled);
   Assert.IsFalse(player.isPlaying,"The hidden Canvas must not keep decoding its background movie.");
   canvas.enabled=true;yield return null;yield return null;
   Assert.IsTrue(player.isPlaying,"Closing the overlay must resume the same owned decoder.");
   Assert.AreSame(player,media.GetComponent<VideoPlayer>());
   Assert.IsInstanceOf<RenderTexture>(media.GetComponent<RawImage>().texture);
   Object.Destroy(root);yield return null;
  }
 }
}
