using System.Collections;
using System.IO;
using NUnit.Framework;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.Video;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
 public sealed class RecordedMapPreviewTests
 {
  GameSettings settings;bool preview;string map;
  [UnitySetUp]public IEnumerator Before(){settings=SettingsStore.Current;preview=MatchInstaller.PreviewOnly;map=SceneFlow.SelectedMap;yield return PlayModeWorld.Reset();GameServices.Ensure();SettingsStore.OverrideForTests(new GameSettings{ReducedUiMotion=false});SceneFlow.SelectedMap=SceneFlow.Arena;}
  [UnityTearDown]public IEnumerator After(){yield return PlayModeWorld.Reset();SettingsStore.OverrideForTests(settings);MatchInstaller.PreviewOnly=preview;SceneFlow.SelectedMap=map;}
  [UnityTest,Timeout(120000)]public IEnumerator RecordedArenaPlaysWithoutALiveMapAndHandsOffToLobby()
  {
   var root=new GameObject("Recorded map preview",typeof(RectTransform),typeof(RawImage));var view=root.AddComponent<MapPreviewSurface>();view.Show(SceneFlow.Arena);
   Assert.AreEqual(SceneFlow.Arena,view.Showing);Assert.IsNull(view.Camera);Assert.IsFalse(SceneManager.GetSceneByName(SceneFlow.Arena).isLoaded);
   var media=root.GetComponent<MapPreviewVideo>();Assert.IsNotNull(media);Assert.IsNotNull(root.GetComponent<RawImage>().texture);
   float until=Time.realtimeSinceStartup+40;while(!media.HasFirstFrame&&Time.realtimeSinceStartup<until)yield return null;
   Assert.IsTrue(media.HasFirstFrame,"Unity did not decode the recorded map.");
   var player=root.GetComponent<VideoPlayer>();Assert.IsNotNull(player);Assert.IsTrue(player.isPlaying);Assert.IsTrue(player.isLooping);
   player.Pause();player.frame=0;yield return null;yield return null;
   var target=player.targetTexture;var previous=RenderTexture.active;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
   RenderTexture.active=target;image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();RenderTexture.active=previous;
   Directory.CreateDirectory("Logs/map-video-playback");File.WriteAllBytes("Logs/map-video-playback/Arena-decoded-frame0.png",image.EncodeToPNG());Object.Destroy(image);
   view.SetRenderingEnabled(false);Assert.IsFalse(player.isPlaying);view.SetRenderingEnabled(true);yield return null;Assert.IsTrue(player.isPlaying);Assert.IsNull(view.Camera);
   root.SetActive(false);yield return null;Assert.IsFalse(player.isPlaying);root.SetActive(true);until=Time.realtimeSinceStartup+10;while(!player.isPlaying&&Time.realtimeSinceStartup<until)yield return null;Assert.IsTrue(player.isPlaying,"Reopening the surface must prepare and resume its decoder.");
   view.LobbyShot=true;until=Time.realtimeSinceStartup+45;while(view.Camera==null&&Time.realtimeSinceStartup<until)yield return null;
   Assert.IsNotNull(view.Camera);Assert.IsTrue(SceneManager.GetSceneByName(SceneFlow.Arena).isLoaded);Assert.IsNull(root.GetComponent<VideoPlayer>());
   view.LobbyShot=false;view.Show(SceneFlow.Arena);Assert.IsNull(view.Camera);Assert.AreEqual(SceneFlow.Arena,view.Showing);
  }
  [UnityTest]public IEnumerator ReducedMotionUsesTheMatchingPosterWithoutADecoder()
  {
   SettingsStore.OverrideForTests(new GameSettings{ReducedUiMotion=true});
   var root=new GameObject("Recorded reduced-motion map",typeof(RectTransform),typeof(RawImage));var view=root.AddComponent<MapPreviewSurface>();view.Show(SceneFlow.Arena);yield return null;
   Assert.AreSame(MapPreviewVideo.PosterFor(SceneFlow.Arena),root.GetComponent<RawImage>().texture);
   Assert.IsNull(root.GetComponent<VideoPlayer>());Assert.IsNull(view.Camera);Assert.IsFalse(SceneManager.GetSceneByName(SceneFlow.Arena).isLoaded);
  }
 }
}

