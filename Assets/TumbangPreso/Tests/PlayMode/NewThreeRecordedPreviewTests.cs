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
    public sealed class NewThreeRecordedPreviewTests
    {
        private GameSettings _settings;
        [UnitySetUp] public IEnumerator Before()
        {
            _settings=SettingsStore.Current;yield return PlayModeWorld.Reset();
            SettingsStore.OverrideForTests(new GameSettings{ReducedUiMotion=false});
        }
        [UnityTearDown] public IEnumerator After()
        {yield return PlayModeWorld.Reset();SettingsStore.OverrideForTests(_settings);}
        [UnityTest] public IEnumerator NewCourtFootageDecodesAtOriginalQualityWithoutLoadingAnArena()
        {
            Directory.CreateDirectory("Logs/new3-preview-decoded1007");
            foreach(string map in new[]{SceneFlow.IlalimNgTulay,SceneFlow.LagoonCove,SceneFlow.Kanto})
            {
                var root=new GameObject("New court preview "+map,typeof(RectTransform),typeof(RawImage));
                var view=root.AddComponent<MapPreviewSurface>();view.Show(map);
                Assert.AreSame(MapPreviewVideo.PosterFor(map),root.GetComponent<RawImage>().texture,"The first frame must already be a real poster.");
                Assert.IsNull(view.Camera);Assert.IsFalse(SceneManager.GetSceneByName(map).isLoaded);
                var media=root.GetComponent<MapPreviewVideo>();float until=Time.realtimeSinceStartup+30;
                while(!media.HasFirstFrame&&Time.realtimeSinceStartup<until)yield return null;
                Assert.IsTrue(media.HasFirstFrame,map+": actual native first frame missing.");
                var player=root.GetComponent<VideoPlayer>();Assert.IsTrue(player.isPrepared);Assert.IsTrue(player.isPlaying);
                Assert.AreEqual(map+"-loop",player.clip.name,"Selection must bind the requested clip, not a cached other map.");
                Assert.AreEqual(1920,player.clip.width);Assert.AreEqual(1080,player.clip.height);
                Assert.AreEqual(780,player.clip.frameCount);Assert.AreEqual(30,player.clip.frameRate,.001);
                Assert.IsTrue(player.isLooping);Assert.AreEqual(1920,player.targetTexture.width);Assert.AreEqual(1080,player.targetTexture.height);
                // Seek completion is a clock event, not proof the GPU target
                // contains the decoded picture. Observe an actual frame after
                // replaying from zero before comparing it to the source poster.
                bool sought=false,delivered=false;long deliveredFrame=-1;
                player.seekCompleted+=p=>sought=true;player.sendFrameReadyEvents=true;
                player.frameReady+=(p,frame)=>{delivered=true;deliveredFrame=frame;};
                player.Pause();player.frame=0;player.Play();
                until=Time.realtimeSinceStartup+5;while((!sought||!delivered)&&Time.realtimeSinceStartup<until)yield return null;
                Assert.IsTrue(sought&&delivered,map+": an actual post-seek frame did not arrive.");
                for(int settle=0;settle<6;settle++)yield return null;
                player.Pause();TestContext.WriteLine(map+": clip="+player.clip.name+" frame="+player.frame+" delivered="+deliveredFrame+" texture="+player.texture?.name);
                var previous=RenderTexture.active;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
                try{RenderTexture.active=player.targetTexture;image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
                    File.WriteAllBytes("Logs/new3-preview-decoded1007/"+map+"-native.png",image.EncodeToPNG());
                    var expected=RenderTexture.GetTemporary(1920,1080,0,RenderTextureFormat.ARGB32);
                    var poster=new Texture2D(1920,1080,TextureFormat.RGB24,false);
                    try{Graphics.Blit(MapPreviewVideo.PosterFor(map),expected);RenderTexture.active=expected;
                        poster.ReadPixels(new Rect(0,0,1920,1080),0,0);poster.Apply();
                        var actualPixels=image.GetPixels32();var expectedPixels=poster.GetPixels32();double difference=0;
                        for(int pixel=0;pixel<actualPixels.Length;pixel+=16){var a=actualPixels[pixel];var b=expectedPixels[pixel];
                            difference+=System.Math.Abs(a.r-b.r)+System.Math.Abs(a.g-b.g)+System.Math.Abs(a.b-b.b);}
                        difference/=System.Math.Ceiling(actualPixels.Length/16.0)*3;
                        TestContext.WriteLine(map+": decoded-vs-matching-poster meanRGB="+difference);
                        Assert.Less(difference,20,map+": metadata-only success displayed the wrong/unwritten movie frame.");}
                    finally{RenderTexture.ReleaseTemporary(expected);Object.Destroy(poster);}}
                finally{RenderTexture.active=previous;Object.Destroy(image);}
                bool looped=false;player.loopPointReached+=p=>looped=true;player.frame=778;player.Play();
                until=Time.realtimeSinceStartup+5;while(!looped&&Time.realtimeSinceStartup<until)yield return null;
                Assert.IsTrue(looped,map+": native end-to-start loop did not fire.");
                Assert.IsNull(view.Camera);Assert.IsFalse(SceneManager.GetSceneByName(map).isLoaded);
                Object.Destroy(root);yield return null;yield return null;
                Assert.IsTrue(player==null,"The retired view releases its decoder.");
            }
        }
    }
}
