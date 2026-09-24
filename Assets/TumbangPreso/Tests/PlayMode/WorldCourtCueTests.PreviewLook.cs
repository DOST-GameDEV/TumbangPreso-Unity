using System.Collections;
using NUnit.Framework;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed partial class WorldCourtCueTests
    {
        // ⚠️⚠️ LIGHT-1.8: THE MAP SELECT SHOWS THE LOOK THE MATCH PLAYS IN, AND GIVES IT BACK.
        // Driven through the real MapPreviewSurface on all five maps rather than a staged
        // install. Per map: the look is live for the shown map, the preview camera is graded and
        // the menu's main camera is not, the scene settings and the sun carry the look, a lobby
        // refresh reuses the live install, and the map shown before it has its authored sun back.
        // Then a cached map is shown again, and destroying the surface hands its sun back too.
        // Each map's own preview target is written to TUMP_WORLD_CUE_OUT as preview-<map>.png.
        [UnityTest,Timeout(240000)] public IEnumerator MapPreviewShowsTheBrightLookAndHandsEachMapItsLightingBack()
        {
            StageWeights(1);
            var menu=new GameObject("Menu UI camera stand-in"){tag="MainCamera"}.AddComponent<Camera>();
            menu.cullingMask=0;menu.clearFlags=CameraClearFlags.Depth;
            var portrait=new GameObject("Unrelated portrait key").AddComponent<Light>();
            portrait.type=LightType.Directional;portrait.transform.rotation=Quaternion.Euler(12,147,0);portrait.intensity=0;
            var root=new GameObject("Bright look preview",typeof(RectTransform),typeof(RawImage));
            var preview=root.AddComponent<MapPreviewSurface>();
            string shown=null;preview.MapShown+=map=>shown=map;
            var report=new System.Text.StringBuilder("map,floor,pivot_y,fog_end,sun_intensity,bloom_live,target_format\n");
            var maps=new[]{SceneFlow.BayanPlaza,SceneFlow.Eskinita,SceneFlow.IlalimNgTulay,SceneFlow.SaBubong,SceneFlow.Lagoon,SceneFlow.BayanPlaza};
            var authored=new System.Collections.Generic.Dictionary<string,(Light sun,Color colour,float intensity,Vector3 forward)>();
            string previous=null;
            Scene handbackScene=default,priorActive=default;
            try
            {
                foreach(string map in maps)
                {
                    shown=null;preview.Show(map);
                    float until=Time.realtimeSinceStartup+40;
                    while(shown!=map && Time.realtimeSinceStartup<until)yield return null;
                    Assert.AreEqual(map,shown,"The preview never finished showing "+map);
                    yield return null;yield return null;
                    var look=WorldLookPresentation.Current;
                    Assert.IsNotNull(look,map+": the preview installed no bright look");
                    Assert.AreEqual(map,look.gameObject.scene.name,"The live look belongs to another map");
                    Assert.IsTrue(WorldLookPresentation.HandlesCamera(preview.Camera),map+": the preview camera is not graded");
                    var main=Camera.main;Assert.IsNotNull(main);Assert.AreNotSame(preview.Camera,main);
                    Assert.IsFalse(WorldLookPresentation.HandlesCamera(main),map+": the menu's main camera was graded");
                    var sun=PreviewSun(map);Assert.IsNotNull(sun,map+" has no shadow-casting sun");
                    Assert.AreSame(sun,look.KeyLight,map+": selected look used another scene's key");
                    if(!authored.ContainsKey(map))
                    {
                        // Weight 0 is the scene's own lighting, so this is the authored sun.
                        StageWeights(0);yield return null;
                        authored[map]=(sun,sun.color,sun.intensity,sun.transform.forward);
                        StageWeights(1);yield return null;
                    }
                    Assert.AreEqual(AmbientMode.Trilight,RenderSettings.ambientMode,map);
                    Assert.AreEqual(look.Look.FogEnd,RenderSettings.fogEndDistance,.01f,map+" fog");
                    Assert.Less(Vector4.Distance(look.Look.Sun,sun.color),.002f,map+" sun colour");
                    Assert.AreEqual(look.Look.SunIntensity,sun.intensity,.002f,map+" sun intensity");
                    preview.ReapplyEnvironment();yield return null;
                    Assert.IsTrue(ReferenceEquals(look,WorldLookPresentation.Current),map+": a lobby refresh rebuilt the look");
                    Assert.AreEqual(look.Look.FogEnd,RenderSettings.fogEndDistance,.01f,map+": a lobby refresh left the authored fog");
                    if(previous!=null && previous!=map)
                    {
                        var before=authored[previous];
                        Assert.Less(Vector4.Distance(before.colour,before.sun.color),.002f,previous+" kept the look's sun colour after it was parked");
                        Assert.AreEqual(before.intensity,before.sun.intensity,.002f,previous+" kept the look's sun intensity");
                        Assert.Less(Vector3.Angle(before.forward,before.sun.transform.forward),.05f,previous+" kept the lifted sun");
                    }
                    var grade=preview.Camera.GetComponent<ColourGrade>();
                    if(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.DefaultHDR))
                        Assert.AreNotEqual(RenderTextureFormat.ARGB32,preview.Camera.targetTexture.format,"Preview highlights were clipped to an LDR target");
                    SavePreview(preview.Camera,"preview-"+map+"-before-LDR",true);
                    var outline=preview.Camera.GetComponent<WorldOutline>();Assert.IsNotNull(outline);
                    var previousSun=RenderSettings.sun;
                    try
                    {
                        RenderSettings.sun=portrait;outline.LegacyKeyForReview=true;
                        SavePreview(preview.Camera,"preview-"+map+"-before-key");
                    }
                    finally{RenderSettings.sun=previousSun;outline.LegacyKeyForReview=false;}
                    SavePreview(preview.Camera,"preview-"+map);
                    var material=(Material)typeof(WorldOutline).GetField("_material",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(outline);
                    Assert.IsNotNull(material,"Native edge material was not rendered");
                    var expected=preview.Camera.worldToCameraMatrix.MultiplyVector(-sun.transform.forward).normalized;
                    Assert.Less(Vector3.Distance(expected,(Vector3)material.GetVector("_PeakSunView")),.001f,map+": edges used the portrait or a stale map light");
                    report.AppendLine(System.FormattableString.Invariant(
                        $"{map},{look.Floor:F3},{preview.Pivot.y:F3},{RenderSettings.fogEndDistance:F1},{sun.intensity:F3},{grade!=null && grade.BloomLive},{preview.Camera.targetTexture.format}"));
                    previous=map;
                }
                // START can activate the next scene before deferred preview
                // destruction. Its settings must not receive the menu snapshot.
                priorActive=SceneManager.GetActiveScene();handbackScene=SceneManager.CreateScene("Preview lighting handback witness");
                SceneManager.SetActiveScene(handbackScene);
                var nextSky=new Color(.21f,.32f,.43f);RenderSettings.ambientSkyColor=nextSky;
                RenderSettings.fogEndDistance=87;RenderSettings.skybox=null;
                Object.Destroy(root);yield return null;yield return null;
                Assert.IsNull(WorldLookPresentation.Current,"The look outlived the preview surface");
                Assert.Less(Vector4.Distance(nextSky,RenderSettings.ambientSkyColor),.002f,"Old preview overwrote the new scene ambient");
                Assert.AreEqual(87,RenderSettings.fogEndDistance,.01f,"Old preview overwrote the new scene fog");
                var last=authored[previous];
                Assert.Less(Vector4.Distance(last.colour,last.sun.color),.002f,"Destroying the preview kept the look's sun");
                SceneManager.SetActiveScene(priorActive);yield return SceneManager.UnloadSceneAsync(handbackScene);
            }
            finally
            {
                if(root!=null)Object.Destroy(root);Object.Destroy(menu.gameObject);Object.Destroy(portrait.gameObject);
                if(handbackScene.IsValid()&&handbackScene.isLoaded)
                {if(priorActive.IsValid()&&priorActive.isLoaded)SceneManager.SetActiveScene(priorActive);SceneManager.UnloadSceneAsync(handbackScene);}
                System.IO.Directory.CreateDirectory(Output);
                System.IO.File.WriteAllText(System.IO.Path.Combine(Output,"preview-look.csv"),report.ToString());
            }
        }

        // The same pick as MapPreviewSurface.ApplyPreviewLook: the first shadow-casting directional
        // light under a live root that is not the stripped match root.
        private static Light PreviewSun(string map)
        {
            foreach(var root in SceneManager.GetSceneByName(map).GetRootGameObjects())
            {
                if(!root.activeInHierarchy || root.GetComponent<MatchInstaller>()!=null)continue;
                foreach(var light in root.GetComponentsInChildren<Light>())
                    if(light.type==LightType.Directional && light.shadows!=LightShadows.None &&
                       light.GetComponentInParent<MatchInstaller>(true)==null)return light;
            }
            return null;
        }

        // The preview's own target, as the lobby's RawImage shows it, resolved and sRGB-encoded.
        private static void SavePreview(Camera camera,string name,bool legacyLdr=false)
        {
            var original=camera.targetTexture;
            var target=legacyLdr?new RenderTexture(original.width,original.height,24,RenderTextureFormat.ARGB32){antiAliasing=original.antiAliasing}:original;
            camera.targetTexture=target;camera.Render();camera.targetTexture=original;
            var flat=RenderTexture.GetTemporary(target.width,target.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            Graphics.Blit(target,flat);
            var active=RenderTexture.active;RenderTexture.active=flat;
            var pixels=new Texture2D(flat.width,flat.height,TextureFormat.RGB24,false);
            pixels.ReadPixels(new Rect(0,0,flat.width,flat.height),0,0);pixels.Apply();
            RenderTexture.active=active;RenderTexture.ReleaseTemporary(flat);
            System.IO.Directory.CreateDirectory(Output);
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(Output,name+".png"),pixels.EncodeToPNG());
            Object.Destroy(pixels);
            if(legacyLdr){target.Release();Object.Destroy(target);}
        }
    }
}
