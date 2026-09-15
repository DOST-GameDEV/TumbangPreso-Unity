using System.Collections;
using System.IO;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class OwnerMenuSkyTests
    {
        [UnitySetUp]public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown]public IEnumerator After()=>PlayModeWorld.Reset();
        [UnityTest,Timeout(60000)]
        public IEnumerator CloudsMoveBehindStationaryStreetAndOccluders()
        {
            bool boot=SceneFlow.BootedThroughSplash;
            bool reduced=TumbangPreso.Settings.SettingsStore.Current.ReducedUiMotion;
            Texture2D before=null,after=null;
            try
            {
                SceneFlow.BootedThroughSplash=false;
                yield return SceneManager.LoadSceneAsync(SceneFlow.MainMenu);yield return null;
                var canvas=GameObject.Find("OwnerHomeCanvas").GetComponent<Canvas>();
                EventSystem.current.SetSelectedGameObject(null);
                var sky=canvas.GetComponentInChildren<OwnerMenuClouds>();Assert.IsNotNull(sky);
                var material=sky.GetComponent<RawImage>().material;
                Assert.AreEqual("TumbangPreso/UI/OwnerMenuSky",material.shader.name);Assert.True(material.shader.isSupported);
                TumbangPreso.Settings.SettingsStore.Current.ReducedUiMotion=false;
                yield return new WaitForSecondsRealtime(.2f);
                Assert.Greater(material.GetVector("_CloudDrift").x,0);
                TumbangPreso.Settings.SettingsStore.Current.ReducedUiMotion=true;
                yield return null;yield return null;
                Assert.AreEqual(Vector4.zero,material.GetVector("_CloudDrift"));
                sky.enabled=false;
                yield return TumpUiCapture.Capture("OwnerSky-v4-rest",canvas,1920,1080,false);
                material.SetVector("_CloudDrift",new Vector4(24,1,0,0));
                yield return TumpUiCapture.Capture("OwnerSky-v4-drift",canvas,1920,1080,false);
                before=Read("rest");after=Read("drift");
                var a=before.GetPixels32();var b=after.GetPixels32();int skyChanges=0,outsideChanges=0;
                for(int y=0;y<1080;y++)for(int x=0;x<1920;x++)
                {
                    int at=(1079-y)*1920+x;int delta=Difference(a[at],b[at]);
                    if(delta<=3)continue;
                    if(x>=1220 && x<=1750 && y<=326)skyChanges++;else outsideChanges++;
                }
                Debug.Log("[OwnerSky] changed sky pixels="+skyChanges+" unchanged-region violations="+outsideChanges);
                Assert.Greater(skyChanges,800,"Cloud drift must be visible, not just a changing material value");
                Assert.AreEqual(0,outsideChanges,"The wall, street, foreground props and controls must stay fixed");
                // Samples on the actual utility pole/crossarm and the foreground
                // roof/tree silhouettes, not a synthetic sky mask copied from code.
                foreach(var point in new[]{new Vector2Int(1380,201),new Vector2Int(1381,160),
                    new Vector2Int(1360,156),new Vector2Int(1398,152),new Vector2Int(1541,30),
                    new Vector2Int(1593,111),new Vector2Int(1240,227)})
                {
                    int at=(1079-point.y)*1920+point.x;
                    Assert.LessOrEqual(Difference(a[at],b[at]),1,"Clouds moved a foreground sample at "+point);
                }
                material.SetVector("_CloudDrift",new Vector4(-36,-3,0,0));
                yield return TumpUiCapture.Capture("OwnerSky-v4-reverse",canvas,1920,1080,false);
                Object.DestroyImmediate(after);after=Read("reverse");b=after.GetPixels32();
                int reverseOutside=0;
                for(int y=0;y<1080;y++)for(int x=0;x<1920;x++)
                    if((x<1220 || x>1750 || y>326) && Difference(a[(1079-y)*1920+x],b[(1079-y)*1920+x])>3)reverseOutside++;
                Assert.AreEqual(0,reverseOutside,"Reverse drift moved the stationary scene");
            }
            finally
            {
                if(before!=null)Object.DestroyImmediate(before);if(after!=null)Object.DestroyImmediate(after);
                SceneFlow.BootedThroughSplash=boot;TumbangPreso.Settings.SettingsStore.Current.ReducedUiMotion=reduced;
            }
        }
        private static int Difference(Color32 a,Color32 b)=>Mathf.Max(Mathf.Abs(a.r-b.r),Mathf.Abs(a.g-b.g),Mathf.Abs(a.b-b.b));
        private static Texture2D Read(string pose)
        {
            var texture=new Texture2D(2,2,TextureFormat.RGB24,false);
            texture.LoadImage(File.ReadAllBytes("Logs/shots-native-ui/OwnerSky-v4-"+pose+".png"));return texture;
        }
    }
}
