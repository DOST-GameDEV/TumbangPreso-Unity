using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
 public sealed class MapPreviewRecoveryTests
 {
  const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
  bool preview;string map;
  [UnitySetUp]public IEnumerator Before(){preview=MatchInstaller.PreviewOnly;map=SceneFlow.SelectedMap;yield return PlayModeWorld.Reset();SceneFlow.SelectedMap=SceneFlow.Eskinita;}
  [UnityTearDown]public IEnumerator After(){yield return PlayModeWorld.Reset();MatchInstaller.PreviewOnly=preview;SceneFlow.SelectedMap=map;}
  static IEnumerator Ready(MapPreviewSurface view,string map)
  {
   view.Show(map);float until=Time.realtimeSinceStartup+45;
   while((view.Showing!=map||(bool)typeof(MapPreviewSurface).GetField("_busy",Hidden).GetValue(view))&&Time.realtimeSinceStartup<until)yield return null;
   Assert.AreEqual(map,view.Showing);Assert.IsFalse((bool)typeof(MapPreviewSurface).GetField("_busy",Hidden).GetValue(view));
  }
  static uint Pixels(RenderTexture source,out int colours)
  {
   var before=RenderTexture.active;var small=RenderTexture.GetTemporary(64,36,0,RenderTextureFormat.ARGB32);
   var image=new Texture2D(64,36,TextureFormat.RGBA32,false);uint hash=2166136261;var distinct=new HashSet<uint>();
   try
   {
    Graphics.Blit(source,small);RenderTexture.active=small;image.ReadPixels(new Rect(0,0,64,36),0,0);image.Apply();
    foreach(var p in image.GetPixels32())
    {
     uint rgb=(uint)(p.r|(p.g<<8)|(p.b<<16));distinct.Add(rgb);
     unchecked{hash=(hash^rgb)*16777619;}
    }
    colours=distinct.Count;return hash;
   }
   finally{RenderTexture.active=before;RenderTexture.ReleaseTemporary(small);Object.Destroy(image);}
  }
  [UnityTest,Timeout(120000)]public IEnumerator InvalidDestinationKeepsTheUsableCurrentMap()
  {
   var root=new GameObject("Invalid preview recovery",typeof(RectTransform),typeof(RawImage));var view=root.AddComponent<MapPreviewSurface>();
   yield return Ready(view,SceneFlow.Eskinita);Assert.IsTrue(view.Camera.enabled);
   var texture=root.GetComponent<RawImage>().texture;
   LogAssert.Expect(LogType.Warning,"[MapPreview] 'NotAnIncludedMap' is not in the build settings; the setup screen keeps its backdrop.");
   view.Show("NotAnIncludedMap");yield return null;
   Assert.AreEqual(SceneFlow.Eskinita,view.Showing);Assert.AreSame(texture,root.GetComponent<RawImage>().texture);
   Assert.IsTrue(view.Camera.enabled,"Invalid selection parked the valid map and stranded its camera.");
   view.Show(SceneFlow.Eskinita);yield return null;Assert.IsTrue(view.Camera.enabled);
  }
  [UnityTest,Timeout(120000)]public IEnumerator PendingColdSwapPreservesActualPixelsAndCachedReturnIsReady()
  {
   var root=new GameObject("Pixel retention preview",typeof(RectTransform),typeof(RawImage));var view=root.AddComponent<MapPreviewSurface>();
   yield return Ready(view,SceneFlow.Eskinita);view.Camera.Render();
   uint original=Pixels(view.Camera.targetTexture,out int colours);Assert.Greater(colours,12,"Initial map target is blank.");
   float start=Time.realtimeSinceStartup;view.Show(SceneFlow.Kanto);
   int checkedFrames=0;
   while((bool)typeof(MapPreviewSurface).GetField("_busy",Hidden).GetValue(view)&&Time.realtimeSinceStartup-start<45)
   {
    Assert.IsFalse(view.Camera.enabled);
    Assert.AreEqual(original,Pixels(view.Camera.targetTexture,out colours),"Pending map load replaced its completed image.");
    checkedFrames++;yield return null;
   }
   Assert.Greater(checkedFrames,0);Assert.AreEqual(SceneFlow.Kanto,view.Showing);
   float cold=Time.realtimeSinceStartup-start;Pixels(view.Camera.targetTexture,out colours);Assert.Greater(colours,12,"Ready replacement target is blank.");
   start=Time.realtimeSinceStartup;yield return Ready(view,SceneFlow.Eskinita);
   float cached=Time.realtimeSinceStartup-start;Pixels(view.Camera.targetTexture,out colours);Assert.Greater(colours,12);
   Directory.CreateDirectory("Logs/preview-recovery");File.WriteAllText("Logs/preview-recovery/timings.txt",System.FormattableString.Invariant($"cold_kanto_seconds={cold:F4}\ncached_eskinita_seconds={cached:F4}\npending_pixel_checks={checkedFrames}\n"));
  }
 }
}
