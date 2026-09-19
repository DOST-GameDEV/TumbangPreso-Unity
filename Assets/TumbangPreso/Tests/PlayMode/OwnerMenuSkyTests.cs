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
    /// <summary>
    /// ⚠️⚠️ BOTH CASES HERE WERE DRIVING `OwnerMenuClouds`, WHICH NO SCREEN INSTALLS, AND THAT
    /// IS § 114.7 FOR THE THIRD TIME: a probe measuring a control the game no longer builds.
    /// `HomeCourtView.Build` adds **`OwnerMenuAir`** to the background plate, plus
    /// `OwnerRoadDust` and `OwnerMenuLeaves`; `OwnerMenuClouds` is the pass before it and is
    /// kept as a reference. So the clouds case failed on a null component, and the dust case
    /// threw a `NullReferenceException` on the same line before it ever reached the dust, which
    /// is why the DUST looked broken when nothing was wrong with it.
    ///
    /// ⚠️ THE CLAIMS ARE UNCHANGED AND THEY ARE 🧑'S: *"make sure all main menu effects are
    /// subtle"*. `OwnerMenuAir` carries the numbers (§ 153.3): the cast shadow sways 46 source
    /// pixels over 74 seconds, about 1.2 px a second at the fastest part of the cycle, the near
    /// cloud bank crosses at 4.2 units a second and the far one at 2.2, and reduced motion
    /// parks both at the x she drew them at, which reproduces 46.png exactly.
    /// </summary>
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
                var sky=canvas.GetComponentInChildren<OwnerMenuAir>();Assert.IsNotNull(sky,
                    "the title street has no OwnerMenuAir, so her clouds and her cast shadow are "+
                    "a still picture. HomeCourtView.Build is where it goes on.");
                var material=sky.GetComponent<RawImage>().material;
                Assert.AreEqual("TumbangPreso/UI/OwnerMenuAir",material.shader.name);Assert.True(material.shader.isSupported);
                Assert.IsNotNull(material.GetTexture("_Cloud"));Assert.IsNotNull(material.GetTexture("_Shadow"));
                TumbangPreso.Settings.SettingsStore.Current.ReducedUiMotion=false;
                var parked=material.GetVector("_CloudNear");
                yield return new WaitForSecondsRealtime(.4f);
                Assert.AreNotEqual(parked.x,material.GetVector("_CloudNear").x,
                    "the near cloud bank is not moving with reduced motion off");
                TumbangPreso.Settings.SettingsStore.Current.ReducedUiMotion=true;
                yield return null;yield return null;

                // ⚠️ REDUCED MOTION PARKS HER CLOUDS WHERE SHE DREW THEM RATHER THAN CLEARING
                // THE SKY: 1243 is the x in 46.png, and the shadow drift goes to zero. The
                // setting costs the art nothing, which is the whole of § 153.3's last bullet.
                Assert.AreEqual(1243f,material.GetVector("_CloudNear").x,.01f);
                Assert.AreEqual(0f,material.GetVector("_ShadowDrift").x,.01f);
                Assert.AreEqual(0f,material.GetVector("_ShadowDrift").y,.01f);
                sky.enabled=false;
                yield return TumpUiCapture.Capture("OwnerSky-matte-v11-rest",canvas,1920,1080,false);
                // ⚠️ THE BANK IS MOVED BY HAND RATHER THAN BY WAITING. The near bank crosses at
                // 4.2 units a second by design, so a test that waited for a visible difference
                // would sit on this screen for the better part of a minute; setting the position
                // the component would have reached is the same picture, sooner.
                material.SetVector("_CloudNear",new Vector4(1243-420,28,532,269));
                yield return TumpUiCapture.Capture("OwnerSky-matte-v11-drift",canvas,1920,1080,false);
                before=Read("rest");after=Read("drift");
                var a=before.GetPixels32();var b=after.GetPixels32();int skyChanges=0,outsideChanges=0;
                for(int y=0;y<1080;y++)for(int x=0;x<1920;x++)
                {
                    int at=(1079-y)*1920+x;int delta=Difference(a[at],b[at]);
                    if(delta<=3)continue;
                    if(InHerSky(x,y))skyChanges++;else outsideChanges++;
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
                material.SetVector("_CloudFade",new Vector4(0,0,0,0));
                yield return TumpUiCapture.Capture("OwnerSky-matte-v11-cleared",canvas,1920,1080,false);
                var cleared=Read("cleared");
                foreach(var point in new[]{new Vector2Int(1335,82),new Vector2Int(1490,200),new Vector2Int(1550,225)})
                    Assert.Less(cleared.GetPixel(point.x,1079-point.y).r,.65f,"An old yellow cloud remained at "+point);
                Object.DestroyImmediate(cleared);material.SetVector("_CloudFade",new Vector4(1f,.8f,0,0));
                material.SetVector("_CloudNear",new Vector4(1243+380,28,532,269));
                yield return TumpUiCapture.Capture("OwnerSky-matte-v11-reverse",canvas,1920,1080,false);
                Object.DestroyImmediate(after);after=Read("reverse");b=after.GetPixels32();
                int reverseOutside=0;
                for(int y=0;y<1080;y++)for(int x=0;x<1920;x++)
                    if(!InHerSky(x,y) && Difference(a[(1079-y)*1920+x],b[(1079-y)*1920+x])>3)reverseOutside++;
                Assert.AreEqual(0,reverseOutside,"Reverse drift moved the stationary scene");
            }
            finally
            {
                if(before!=null)Object.DestroyImmediate(before);if(after!=null)Object.DestroyImmediate(after);
                SceneFlow.BootedThroughSplash=boot;TumbangPreso.Settings.SettingsStore.Current.ReducedUiMotion=reduced;
            }
        }
        [UnityTest,Timeout(60000)]
        public IEnumerator DustCoversSandAndLeavesForegroundPropsClear()
        {
            bool boot=SceneFlow.BootedThroughSplash,reduced=TumbangPreso.Settings.SettingsStore.Current.ReducedUiMotion;
            Texture2D a=null,b=null;
            try
            {
                SceneFlow.BootedThroughSplash=false;yield return SceneManager.LoadSceneAsync(SceneFlow.MainMenu);yield return null;
                var canvas=GameObject.Find("OwnerHomeCanvas").GetComponent<Canvas>();EventSystem.current.SetSelectedGameObject(null);
                var sky=canvas.GetComponentInChildren<OwnerMenuAir>();if(sky!=null)sky.enabled=false;
                var dust=canvas.GetComponentInChildren<OwnerRoadDust>();dust.enabled=false;
                TumbangPreso.Settings.SettingsStore.Current.ReducedUiMotion=false;
                yield return new WaitForSecondsRealtime(.7f);
                yield return TumpUiCapture.Capture("OwnerDust-v8-base",canvas,1920,1080,false);
                dust.enabled=true;yield return null;
                Texture2D Load(string name){var t=new Texture2D(2,2);t.LoadImage(File.ReadAllBytes("Logs/shots-native-ui/OwnerDust-v8-"+name+".png"));return t;}
                a=Load("base");var before=a.GetPixels32();
                var changed=new System.Collections.Generic.HashSet<int>();
                var zones=new[]{new RectInt(1000,570,550,100),new RectInt(0,984,800,96),new RectInt(1760,700,155,365)};
                for(int phase=0;phase<4;phase++)
                {
                    yield return TumpUiCapture.Capture("OwnerDust-v8-active-"+phase,canvas,1920,1080,false);
                    b=Load("active-"+phase);var after=b.GetPixels32();
                    int Count(RectInt rect){int count=0;for(int y=rect.yMin;y<rect.yMax;y++)for(int x=rect.xMin;x<rect.xMax;x++)
                        if(Difference(before[(1079-y)*1920+x],after[(1079-y)*1920+x])>3){count++;changed.Add(y*1920+x);}return count;}
                    foreach(var zone in zones)Count(zone);
                    foreach(var solid in new[]{new RectInt(1590,695,130,205),new RectInt(1335,727,110,20),new RectInt(50,990,130,60)})
                        Assert.AreEqual(0,Count(solid),"Dust painted over a foreground object at "+solid);
                    Object.DestroyImmediate(b);b=null;yield return new WaitForSecondsRealtime(.8f);
                }
                foreach(var zone in zones)
                {
                    int pixels=0;foreach(int point in changed)if(zone.Contains(new Vector2Int(point%1920,point/1920)))pixels++;
                    Debug.Log("[OwnerDustCoverage] "+zone+" motion coverage pixels="+pixels);
                    Assert.Greater(pixels,30,"Wind must reach each sand region over the observed sequence");
                }
            }
            finally
            {
                if(a!=null)Object.DestroyImmediate(a);if(b!=null)Object.DestroyImmediate(b);
                SceneFlow.BootedThroughSplash=boot;TumbangPreso.Settings.SettingsStore.Current.ReducedUiMotion=reduced;
            }
        }
        /// <summary>
        /// Her sky opening at 1920x1080, as a rectangle.
        ///
        /// ⚠️⚠️ IT WAS `x 1170..1810, y &lt;= 350` AND THAT WAS THE PREVIOUS PAINTING'S SKY. On
        /// her 2026-09-18 street the opening reaches further left: moving the near bank changes
        /// **89 pixels between x1024 and x1165, at y28 to y48**, and every one of them samples
        /// as sky (about 135,150,101 against her measured `SkyColour` of 136,200,119 under the
        /// canopy) rather than as a roof or a pole. So the clouds were inside her sky and the
        /// rectangle was not.
        ///
        /// ⚠️ THE RECTANGLE IS THE WEAK HALF OF THIS CASE AND THE SAMPLES BELOW IT ARE THE REAL
        /// ONE: seven points on the actual utility pole, its crossarm, the foreground roofs and
        /// the tree, each asserted to move by at most one level. A rectangle can only say
        /// "somewhere in this box"; those say "not on this object".
        /// </summary>
        private static bool InHerSky(int x,int y)=>x>=1000 && x<=1820 && y<=360;

        private static int Difference(Color32 a,Color32 b)=>Mathf.Max(Mathf.Abs(a.r-b.r),Mathf.Abs(a.g-b.g),Mathf.Abs(a.b-b.b));
        private static Texture2D Read(string pose)
        {
            var texture=new Texture2D(2,2,TextureFormat.RGB24,false);
            texture.LoadImage(File.ReadAllBytes("Logs/shots-native-ui/OwnerSky-matte-v11-"+pose+".png"));return texture;
        }
    }
}
