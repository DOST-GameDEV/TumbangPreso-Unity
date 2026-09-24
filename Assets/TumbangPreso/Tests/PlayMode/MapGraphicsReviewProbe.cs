using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    /// <summary>Matched world-render evidence. Editor timings are not shipping FPS claims.</summary>
    [Category("WallClock")]
    public sealed class MapGraphicsReviewProbe
    {
        private int _quality,_sync,_target;
        private bool _bots,_spectator,_pinned,_networked;
        private int _seat;
        private CustomRules _rules;
        private static string Output=>Environment.GetEnvironmentVariable("TUMP_GRAPHICS_REVIEW")??"Logs/map-graphics-review-v1";
        [UnitySetUp] public IEnumerator Before()
        {
            _quality=GraphicsProfiles.Current;_sync=QualitySettings.vSyncCount;_target=Application.targetFrameRate;
            _bots=GameLaunch.AllBots;_spectator=GameLaunch.Spectator;_seat=GameLaunch.SoloSeat;
            _pinned=SceneFlow.RulesPinned;_networked=SceneFlow.Networked;_rules=SceneFlow.SelectedRules.Clone();
            yield return PlayModeWorld.Reset();Directory.CreateDirectory(Output);
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();GraphicsProfiles.Apply(_quality);
            QualitySettings.vSyncCount=_sync;Application.targetFrameRate=_target;
            GameLaunch.AllBots=_bots;GameLaunch.Spectator=_spectator;GameLaunch.SoloSeat=_seat;
            SceneFlow.Networked=_networked;SceneFlow.AdoptRemoteRules(_rules);
            if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();
        }
        [UnityTest,Timeout(240000)]
        public IEnumerator FourMapsReportMatchedWorldFramesAndQualityDifferences()
        {
            var report=new StringBuilder("map,quality,samples,mean_editor_frame_ms,p95_editor_frame_ms,mean_draw_calls,mean_triangles,mean_setpass,world_width,world_height\n");
            File.WriteAllText(Path.Combine(Output,"scope.txt"),
                "Editor world-render baseline, not a player FPS benchmark. Main world camera renders1920x1080 HDR; overlay UI is outside that target.\n"+
                "Same fixed eye position, no bot inputs. Ambient animals remain active, so small visit variation is expected.\n"+
                SystemInfo.graphicsDeviceName+" / "+SystemInfo.graphicsDeviceType+" / "+SystemInfo.processorType+"\n");
            try
            {
                foreach(string map in SceneFlow.Maps)
                {
                    yield return MapRetrievalProbe.Load(map);GameServices.Round.BeginRound();Time.timeScale=1;
                    var who=GameServices.Round.PlayerAt(1);who.Intent.Clear();who.Intent.Parked=true;
                    who.Teleport(new Vector3(0,map==SceneFlow.IlalimNgTulay?.212f:.1f,-10));
                    who.transform.rotation=Quaternion.identity;
                    var rig=Object.FindFirstObjectByType<CameraRig>();rig.Follow(who);rig.SetAimSource(AimSource.Movement);
                    var camera=rig.Camera;
                    var previous=camera.targetTexture;
                    var target=new RenderTexture(1920,1080,24,RenderTextureFormat.DefaultHDR,RenderTextureReadWrite.Linear)
                        {antiAliasing=Mathf.Max(1,QualitySettings.antiAliasing)};
                    target.Create();camera.targetTexture=target;
                    try
                    {
                        for(int quality=0;quality<GraphicsProfiles.All.Length;quality++)
                        {
                            GraphicsProfiles.Apply(quality);QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
                            for(int warm=0;warm<30;warm++)yield return null;
                            var times=new float[120];long draw=0,triangles=0,setpass=0;
                            for(int sample=0;sample<times.Length;sample++)
                            {
                                yield return null;times[sample]=Time.unscaledDeltaTime*1000;
                                draw+=UnityStats.drawCalls;triangles+=UnityStats.triangles;setpass+=UnityStats.setPassCalls;
                            }
                            Assert.Greater(draw,0,map+" produced no rendered-world counter samples");
                            float mean=times.Average();Array.Sort(times);float p95=times[(int)(times.Length*.95f)-1];
                            report.AppendLine(FormattableString.Invariant($"{map},{GraphicsProfiles.Of(quality).Label},{times.Length},{mean:F3},{p95:F3},{draw/(double)times.Length:F1},{triangles/(double)times.Length:F0},{setpass/(double)times.Length:F1},1920,1080"));
                            yield return GameplayShots.Render(camera,map+"-"+GraphicsProfiles.Of(quality).Label,false,Output);
                        }
                    }
                    finally{camera.targetTexture=previous;target.Release();Object.Destroy(target);}
                    yield return PlayModeWorld.Reset();
                }
            }
            finally{File.WriteAllText(Path.Combine(Output,"world-render.csv"),report.ToString());}
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator LagoonBoatFinishReview()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Lagoon);
            var pairs=Enumerable.Range(0,6).Select(id=>
            {
                var boat=GameObject.Find("Lagoon/"+(id<4?"Pointed working canoe ":"Sheltered houseboat ")+id);
                var filter=boat.GetComponent<MeshFilter>();var renderer=boat.GetComponent<MeshRenderer>();
                var importer=AssetImporter.GetAtPath("Assets/TumbangPreso/Art/LagoonBoatFinish/Boat"+id+".asset");Assert.IsNotNull(importer);
                Assert.IsTrue(importer.userData.StartsWith("TUMP_LAGOON_BOAT_MESH:"));
                var original=AssetDatabase.LoadAssetAtPath<Mesh>(importer.userData.Substring("TUMP_LAGOON_BOAT_MESH:".Length));Assert.IsNotNull(original);
                var after=renderer.sharedMaterials;var before=after.Select(m=>
                {string path=m.GetTag("TumpLagoonBoatSource",false);return string.IsNullOrEmpty(path)?m:AssetDatabase.LoadAssetAtPath<Material>(path);}).ToArray();
                Assert.IsFalse(before.Any(m=>m==null));Assert.IsNotNull(boat.GetComponent<MooredBoatMotion>());Assert.IsEmpty(boat.GetComponentsInChildren<Collider>());
                return new {filter,renderer,original,finished=filter.sharedMesh,before,after};
            }).ToArray();
            var rig=Object.FindFirstObjectByType<CameraRig>();rig.enabled=false;var camera=rig.Camera;
            foreach(var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None))arms.gameObject.SetActive(false);
            var clock=Object.FindFirstObjectByType<Visual.NeighbourhoodSkyMotion>();bool clockEnabled=clock!=null&&clock.enabled;
            if(clock!=null)clock.enabled=false;
            var eye=Vector3.zero;var rotation=Quaternion.identity;
            Camera.CameraCallback pin=cam=>{if(cam==camera){cam.transform.SetPositionAndRotation(eye,rotation);cam.fieldOfView=60;}};
            Camera.onPreCull+=pin;Time.timeScale=0;
            try
            {
                foreach(string view in new[]{"canoe0","houseboat4","shelter4"})
                {
                    eye=view=="canoe0"?new Vector3(-5,1,-33):view=="houseboat4"?new Vector3(-28,2.2f,17):new Vector3(-30,4,23);
                    var target=view=="canoe0"?new Vector3(-9,-1.0f,-29):new Vector3(-34,view=="shelter4"?.7f:-.25f,26);
                    rotation=Quaternion.LookRotation(target-eye);
                    foreach(string state in new[]{"before","after"})
                    {
                        bool on=state=="after";foreach(var p in pairs){p.filter.sharedMesh=on?p.finished:p.original;p.renderer.sharedMaterials=on?p.after:p.before;}
                        using(Visual.NeighbourhoodSkyMotion.At(20))
                            yield return GameplayShots.Render(camera,"Lagoon-boat-"+view+"-"+state,false,Output,width:1280,height:800);
                    }
                }
            }
            finally{Time.timeScale=1;Camera.onPreCull-=pin;if(clock!=null)clock.enabled=clockEnabled;}
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator LagoonWaterFinishReview()
        {
            Action<bool> ReadWater()
            {
                var water=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Single(t=>t.name=="Moving lagoon surface");
                var bed=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Single(t=>t.name=="Sandy lagoon bed");
                var waterMat=water.GetComponent<Renderer>().sharedMaterial;var bedMat=bed.GetComponent<Renderer>().sharedMaterial;
                Assert.AreEqual("TumbangPreso/LagoonWater",waterMat.shader.name);
                Assert.AreEqual("TumbangPreso/LagoonBed",bedMat.shader.name,"Runtime material conversion erased the authored seabed shader.");
                Assert.IsFalse(ShaderUtil.ShaderHasError(waterMat.shader));Assert.IsFalse(ShaderUtil.ShaderHasError(bedMat.shader));
                Assert.That(water.position.y,Is.EqualTo(LagoonWater.SurfaceY).Within(.0001f));
                var solid=bed.GetComponent<BoxCollider>();Assert.That(solid.bounds.size.x,Is.EqualTo(180).Within(.001f));
                Assert.That(solid.bounds.max.y,Is.EqualTo(LagoonWater.FloorY).Within(.0001f));
                var filter=bed.GetComponent<MeshFilter>();var wide=filter.sharedMesh;
                var original=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/TumbangPreso/Art/LagoonWaterFinish/OriginalBed.asset");Assert.IsNotNull(original);
                return on=>{waterMat.SetFloat("_Refinement",on?1:0);bedMat.SetFloat("_Refinement",on?1:0);filter.sharedMesh=on?wide:original;};
            }
            var canvas=new GameObject("Lagoon water preview",typeof(Canvas));
            var surface=new GameObject("Actual map preview",typeof(RectTransform),typeof(CanvasRenderer),typeof(UnityEngine.UI.RawImage));
            surface.transform.SetParent(canvas.transform,false);((RectTransform)surface.transform).sizeDelta=new Vector2(1920,1080);
            var preview=surface.AddComponent<MapPreviewSurface>();preview.Show(SceneFlow.Lagoon);
            float deadline=Time.realtimeSinceStartup+30;
            while(preview.Showing!=SceneFlow.Lagoon&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.AreEqual(SceneFlow.Lagoon,preview.Showing);preview.enabled=false;var change=ReadWater();Time.timeScale=0;
            try
            {
                foreach(string state in new[]{"before","after"})
                {change(state=="after");yield return GameplayShots.Render(preview.Camera,"Lagoon-water-preview-"+state,false,Output,width:1280,height:720);}
            }
            finally{change(true);Time.timeScale=1;}
            Object.Destroy(canvas);yield return PlayModeWorld.Reset();yield return MapRetrievalProbe.Load(SceneFlow.Lagoon);
            var rig=Object.FindFirstObjectByType<CameraRig>();rig.enabled=false;var camera=rig.Camera;
            foreach(var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None))arms.gameObject.SetActive(false);
            change=ReadWater();var eye=Vector3.zero;var rotation=Quaternion.identity;
            var clock=Object.FindFirstObjectByType<Visual.NeighbourhoodSkyMotion>();bool clockEnabled=clock!=null&&clock.enabled;
            if(clock!=null)clock.enabled=false;
            Camera.CameraCallback pin=cam=>{if(cam==camera){cam.transform.SetPositionAndRotation(eye,rotation);cam.fieldOfView=60;}};
            Camera.onPreCull+=pin;Time.timeScale=0;
            try
            {
                foreach(string view in new[]{"piles","boat","overlook"})
                {
                    eye=view=="piles"?new Vector3(-22,-.3f,3):view=="boat"?new Vector3(-28,2.2f,17):new Vector3(13,3,9);
                    var target=view=="piles"?new Vector3(-27,-1.05f,10):view=="boat"?new Vector3(-34,-.25f,26):new Vector3(18,-1.1f,10);
                    rotation=Quaternion.LookRotation(target-eye);
                    foreach(string state in new[]{"before","after"})
                    {
                        change(state=="after");using(Visual.NeighbourhoodSkyMotion.At(20))
                            yield return GameplayShots.Render(camera,"Lagoon-water-"+view+"-"+state,false,Output,width:1280,height:800);
                    }
                }
                using(Visual.NeighbourhoodSkyMotion.At(40))
                    yield return GameplayShots.Render(camera,"Lagoon-water-motion40",false,Output,width:1280,height:800);
                GraphicsProfiles.Apply(0);using(Visual.NeighbourhoodSkyMotion.At(20))
                    yield return GameplayShots.Render(camera,"Lagoon-water-low",false,Output,width:960,height:540);
            }
            finally{change(true);Time.timeScale=1;Camera.onPreCull-=pin;if(clock!=null)clock.enabled=clockEnabled;}
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator LagoonMetalFinishReview()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Lagoon);
            var homes=GameObject.Find("Lagoon/Supported homes").transform;
            var pairs=homes.Cast<Transform>().Where(t=>t.name.EndsWith("PatchedMetal",StringComparison.Ordinal)||t.name.EndsWith("RepairShelter",StringComparison.Ordinal)).Select(t=>
            {
                var words=t.name.Split(' ');int id=int.Parse(words[words.Length-2]);
                var filter=t.GetComponent<MeshFilter>();var renderer=t.GetComponent<MeshRenderer>();
                var importer=AssetImporter.GetAtPath("Assets/TumbangPreso/Art/LagoonMetalFinish/Metal"+id+".asset");Assert.IsNotNull(importer);
                Assert.IsTrue(importer.userData.StartsWith("TUMP_LAGOON_METAL_MESH:"));
                var original=AssetDatabase.LoadAssetAtPath<Mesh>(importer.userData.Substring("TUMP_LAGOON_METAL_MESH:".Length));Assert.IsNotNull(original);
                var after=renderer.sharedMaterials;var before=after.Select(m=>
                {string path=m.GetTag("TumpLagoonMetalSource",false);return string.IsNullOrEmpty(path)?m:AssetDatabase.LoadAssetAtPath<Material>(path);}).ToArray();
                Assert.IsFalse(before.Any(m=>m==null));
                return new {filter,renderer,original,finished=filter.sharedMesh,before,after};
            }).ToArray();Assert.AreEqual(6,pairs.Length);
            var rig=Object.FindFirstObjectByType<CameraRig>();rig.enabled=false;var camera=rig.Camera;
            foreach(var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None))arms.gameObject.SetActive(false);
            var eye=Vector3.zero;var rotation=Quaternion.identity;
            Camera.CameraCallback pin=cam=>{if(cam==camera){cam.transform.SetPositionAndRotation(eye,rotation);cam.fieldOfView=60;}};
            Camera.onPreCull+=pin;Time.timeScale=0;
            try
            {
                foreach(string view in new[]{"front2","roof2","repair1"})
                {
                    eye=view=="front2"?new Vector3(17,2.8f,-11.8f):view=="roof2"?new Vector3(18,6,-18):new Vector3(-20,6,4);
                    var target=view=="repair1"?new Vector3(-28,3,10):new Vector3(25,view=="front2"?2:3,-12);
                    rotation=Quaternion.LookRotation(target-eye);
                    foreach(string state in new[]{"before","after"})
                    {
                        bool on=state=="after";foreach(var p in pairs){p.filter.sharedMesh=on?p.finished:p.original;p.renderer.sharedMaterials=on?p.after:p.before;}
                        yield return GameplayShots.Render(camera,"Lagoon-metal-"+view+"-"+state,false,Output,width:1280,height:800);
                    }
                }
            }
            finally{Time.timeScale=1;Camera.onPreCull-=pin;}
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator LagoonVerandaFinishReview()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Lagoon);
            var homes=GameObject.Find("Lagoon/Supported homes").transform;
            var pairs=homes.Cast<Transform>().Where(t=>t.name.EndsWith("ScreenVeranda",StringComparison.Ordinal)||t.name.EndsWith("CommunityShade",StringComparison.Ordinal)).Select(t=>
            {
                var words=t.name.Split(' ');int id=int.Parse(words[words.Length-2]);
                var filter=t.GetComponent<MeshFilter>();var renderer=t.GetComponent<MeshRenderer>();
                var importer=AssetImporter.GetAtPath("Assets/TumbangPreso/Art/LagoonVerandaFinish/Veranda"+id+".asset");Assert.IsNotNull(importer);
                Assert.IsTrue(importer.userData.StartsWith("TUMP_LAGOON_VERANDA_MESH:"));
                var original=AssetDatabase.LoadAssetAtPath<Mesh>(importer.userData.Substring("TUMP_LAGOON_VERANDA_MESH:".Length));Assert.IsNotNull(original);
                var after=renderer.sharedMaterials;var before=after.Select(m=>
                {string path=m.GetTag("TumpLagoonVerandaSource",false);return string.IsNullOrEmpty(path)?m:AssetDatabase.LoadAssetAtPath<Material>(path);}).ToArray();
                Assert.IsFalse(before.Any(m=>m==null));var detail=t.Find("Veranda palm and weave").gameObject;Assert.IsEmpty(detail.GetComponentsInChildren<Collider>());
                return new {filter,renderer,original,finished=filter.sharedMesh,before,after,detail};
            }).ToArray();Assert.AreEqual(5,pairs.Length);
            var rig=Object.FindFirstObjectByType<CameraRig>();rig.enabled=false;var camera=rig.Camera;
            foreach(var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None))arms.gameObject.SetActive(false);
            var eye=Vector3.zero;var rotation=Quaternion.identity;
            Camera.CameraCallback pin=cam=>{if(cam==camera){cam.transform.SetPositionAndRotation(eye,rotation);cam.fieldOfView=60;}};
            Camera.onPreCull+=pin;Time.timeScale=0;
            try
            {
                foreach(string view in new[]{"screen3","roof3","shared5"})
                {
                    eye=view=="screen3"?new Vector3(23,3.2f,17):view=="roof3"?new Vector3(18,6,4):new Vector3(13,6,18);
                    var target=view=="shared5"?new Vector3(8.5f,3.1f,26):view=="screen3"?new Vector3(26,1.8f,10):new Vector3(27,3,9);
                    rotation=Quaternion.LookRotation(target-eye);
                    foreach(string state in new[]{"before","after"})
                    {
                        bool on=state=="after";foreach(var p in pairs){p.filter.sharedMesh=on?p.finished:p.original;p.renderer.sharedMaterials=on?p.after:p.before;p.detail.SetActive(on);}
                        yield return GameplayShots.Render(camera,"Lagoon-veranda-"+view+"-"+state,false,Output,width:1280,height:800);
                    }
                }
            }
            finally{Time.timeScale=1;Camera.onPreCull-=pin;}
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator LagoonGableFinishReview()
        {
            Action<bool> ReadFinish()
            {
                var homes=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Single(t=>t.name=="Supported homes");
                var pairs=homes.Cast<Transform>().Where(t=>t.name.EndsWith("ThatchGable",StringComparison.Ordinal)).Select(t=>
                {
                    string[] words=t.name.Split(' ');int id=int.Parse(words[words.Length-2]);
                    var filter=t.GetComponent<MeshFilter>();var renderer=t.GetComponent<MeshRenderer>();
                    var saved=AssetImporter.GetAtPath("Assets/TumbangPreso/Art/LagoonGableFinish/Gable"+id+".asset");
                    Assert.IsNotNull(saved);Assert.IsTrue(saved.userData.StartsWith("TUMP_LAGOON_GABLE_MESH:"));
                    var original=AssetDatabase.LoadAssetAtPath<Mesh>(saved.userData.Substring("TUMP_LAGOON_GABLE_MESH:".Length));Assert.IsNotNull(original);
                    var after=renderer.sharedMaterials;var before=after.Select(m=>
                    {string path=m.GetTag("TumpLagoonGableSource",false);return string.IsNullOrEmpty(path)?m:AssetDatabase.LoadAssetAtPath<Material>(path);}).ToArray();
                    Assert.IsFalse(before.Any(m=>m==null));var details=t.Find("Fitted gable thatch").gameObject;Assert.IsEmpty(details.GetComponentsInChildren<Collider>());
                    return new {filter,renderer,original,finished=filter.sharedMesh,before,after,details};
                }).ToArray();
                Assert.AreEqual(4,pairs.Length);
                return on=>{foreach(var p in pairs){p.filter.sharedMesh=on?p.finished:p.original;p.renderer.sharedMaterials=on?p.after:p.before;p.details.SetActive(on);}};
            }
            var canvas=new GameObject("Lagoon gable preview",typeof(Canvas));
            var surface=new GameObject("Actual map preview",typeof(RectTransform),typeof(CanvasRenderer),typeof(UnityEngine.UI.RawImage));
            surface.transform.SetParent(canvas.transform,false);((RectTransform)surface.transform).sizeDelta=new Vector2(1920,1080);
            var preview=surface.AddComponent<MapPreviewSurface>();preview.Show(SceneFlow.Lagoon);
            float deadline=Time.realtimeSinceStartup+30;
            while(preview.Showing!=SceneFlow.Lagoon&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.AreEqual(SceneFlow.Lagoon,preview.Showing);preview.enabled=false;var switchFinish=ReadFinish();Time.timeScale=0;
            try
            {
                foreach(string state in new[]{"before","after"})
                {switchFinish(state=="after");yield return GameplayShots.Render(preview.Camera,"Lagoon-gable-preview-"+state,false,Output,width:1280,height:720);}
            }
            finally{Time.timeScale=1;}
            Object.Destroy(canvas);yield return PlayModeWorld.Reset();yield return MapRetrievalProbe.Load(SceneFlow.Lagoon);
            var rig=Object.FindFirstObjectByType<CameraRig>();rig.enabled=false;var camera=rig.Camera;
            foreach(var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None))arms.gameObject.SetActive(false);
            switchFinish=ReadFinish();var eye=Vector3.zero;var rotation=Quaternion.identity;
            Camera.CameraCallback pin=cam=>{if(cam==camera){cam.transform.SetPositionAndRotation(eye,rotation);cam.fieldOfView=60;}};
            Camera.onPreCull+=pin;Time.timeScale=0;
            try
            {
                foreach(string view in new[]{"front0","roof0","detached8"})
                {
                    eye=view=="front0"?new Vector3(-17,2.8f,-10.5f):view=="roof0"?new Vector3(-18,6,-16):new Vector3(-34,6,19);
                    var target=view=="detached8"?new Vector3(-42,3.8f,23):new Vector3(-25,3.1f,-10.5f);rotation=Quaternion.LookRotation(target-eye);
                    foreach(string state in new[]{"before","after"})
                    {switchFinish(state=="after");yield return GameplayShots.Render(camera,"Lagoon-gable-"+view+"-"+state,false,Output,width:1280,height:800);}
                }
            }
            finally{Time.timeScale=1;Camera.onPreCull-=pin;}
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator LagoonArtBaselineReview()
        {
            var canvas=new GameObject("Lagoon art preview",typeof(Canvas));
            var surface=new GameObject("Actual map preview",typeof(RectTransform),typeof(CanvasRenderer),typeof(UnityEngine.UI.RawImage));
            surface.transform.SetParent(canvas.transform,false);((RectTransform)surface.transform).sizeDelta=new Vector2(1920,1080);
            var preview=surface.AddComponent<MapPreviewSurface>();preview.Show(SceneFlow.Lagoon);
            float deadline=Time.realtimeSinceStartup+30;
            while(preview.Showing!=SceneFlow.Lagoon&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.AreEqual(SceneFlow.Lagoon,preview.Showing);preview.enabled=false;
            yield return GameplayShots.Render(preview.Camera,"Lagoon-preview",false,Output,width:1280,height:720);
            Object.Destroy(canvas);yield return PlayModeWorld.Reset();yield return MapRetrievalProbe.Load(SceneFlow.Lagoon);
            var rig=Object.FindFirstObjectByType<CameraRig>();rig.enabled=false;
            foreach(var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None))arms.gameObject.SetActive(false);
            var camera=rig.Camera;camera.fieldOfView=60;GraphicsProfiles.Apply(2);
            var names=new[]{"gable-home0","repair-home1","metal-home2","screen-home3","hip-home4","shared-home5","piles","houseboat4","islands"};
            var eyes=new[]{new Vector3(-17,2.8f,-10.5f),new Vector3(-18,2.7f,10),new Vector3(17,2.8f,-11.8f),
                new Vector3(18,2.4f,9),new Vector3(-9,3,18),new Vector3(8.5f,2.7f,17),new Vector3(-18,-.45f,-5),
                new Vector3(-28,2.2f,17),new Vector3(0,2.1f,10)};
            var targets=new[]{new Vector3(-25,2,-10.5f),new Vector3(-27.4f,1.9f,10),new Vector3(25,2,-11.8f),
                new Vector3(26.5f,1.7f,8.9f),new Vector3(-9,2.2f,26.5f),new Vector3(8.6f,1.9f,25.5f),new Vector3(-25,-1.2f,-10),
                new Vector3(-34,-.25f,26),new Vector3(0,14,150)};
            var eye=eyes[0];var rotation=Quaternion.LookRotation(targets[0]-eye);
            Camera.CameraCallback pin=cam=>{if(cam==camera){cam.transform.SetPositionAndRotation(eye,rotation);cam.fieldOfView=60;}};
            Camera.onPreCull+=pin;
            try
            {
                for(int i=0;i<names.Length;i++)
                {eye=eyes[i];rotation=Quaternion.LookRotation(targets[i]-eye);yield return null;
                    yield return GameplayShots.Render(camera,"Lagoon-"+names[i],false,Output,width:1280,height:800);}
            }
            finally{Camera.onPreCull-=pin;}
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator SaBubongFinalArtReview()
        {
            var canvas=new GameObject("SaBubong final art preview",typeof(Canvas));
            var surface=new GameObject("Actual map preview",typeof(RectTransform),typeof(CanvasRenderer),typeof(UnityEngine.UI.RawImage));
            surface.transform.SetParent(canvas.transform,false);((RectTransform)surface.transform).sizeDelta=new Vector2(1920,1080);
            var preview=surface.AddComponent<MapPreviewSurface>();preview.Show(SceneFlow.SaBubong);
            float deadline=Time.realtimeSinceStartup+30;
            while(preview.Showing!=SceneFlow.SaBubong&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.AreEqual(SceneFlow.SaBubong,preview.Showing);preview.enabled=false;
            using(Visual.NeighbourhoodSkyMotion.At(20))
            {
                yield return GameplayShots.Render(preview.Camera,"SaBubong-preview",false,Output,width:1280,height:720);
                yield return GameplayShots.Render(preview.Camera,"SaBubong-card",false,Output,width:960,height:540);
            }
            Object.Destroy(canvas);yield return PlayModeWorld.Reset();yield return MapRetrievalProbe.Load(SceneFlow.SaBubong);
            var rig=Object.FindFirstObjectByType<CameraRig>();rig.enabled=false;
            // These are scenery-only art views. Normal cast/gameplay overlap remains
            // in the later integrated gate, rather than hiding it in a visual approval claim.
            foreach(var visual in Object.FindObjectsByType<Visual.CharacterVisual>(FindObjectsSortMode.None))
                foreach(var renderer in visual.GetComponentsInChildren<Renderer>())renderer.enabled=false;
            foreach(var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None))arms.gameObject.SetActive(false);
            var camera=rig.Camera;var eye=new Vector3(2,1.9f,3);
            var viewRotation=Quaternion.LookRotation(Vector3.forward*40+Vector3.up*.5f);
            var skyClock=Object.FindFirstObjectByType<Visual.NeighbourhoodSkyMotion>();
            bool skyWasEnabled=skyClock!=null&&skyClock.enabled;if(skyClock!=null)skyClock.enabled=false;
            // Render waits two layout frames. Hold only this fixture's sky clock and
            // camera during them, so labelled samples are not overwritten by live updates.
            Camera.CameraCallback pin=cam=>{if(cam==camera){cam.transform.SetPositionAndRotation(eye,viewRotation);cam.fieldOfView=70;}};
            Camera.onPreCull+=pin;
            try
            {
                var directions=new[]{Vector3.forward,Vector3.right,Vector3.back,Vector3.left};
                var names=new[]{"north","east","south","west"};GraphicsProfiles.Apply(2);
                for(int i=0;i<directions.Length;i++)
                {
                    viewRotation=Quaternion.LookRotation(directions[i]*40+Vector3.up*.5f);yield return null;
                    using(Visual.NeighbourhoodSkyMotion.At(20))
                        yield return GameplayShots.Render(camera,"SaBubong-final-"+names[i],false,Output,width:1280,height:800);
                }
                viewRotation=Quaternion.LookRotation(Vector3.forward*40+Vector3.up*.5f);
                using(Visual.NeighbourhoodSkyMotion.At(180))
                    yield return GameplayShots.Render(camera,"SaBubong-final-north-sky180",false,Output,width:1280,height:800);
                GraphicsProfiles.Apply(0);yield return null;
                using(Visual.NeighbourhoodSkyMotion.At(20))
                    yield return GameplayShots.Render(camera,"SaBubong-final-north-low",false,Output,width:960,height:540);
            }
            finally{Camera.onPreCull-=pin;if(skyClock!=null)skyClock.enabled=skyWasEnabled;}
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator SaBubongApartmentSurfaceReview()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.SaBubong);
            var rig=Object.FindFirstObjectByType<CameraRig>();rig.enabled=false;
            foreach(var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None))arms.gameObject.SetActive(false);
            var camera=rig.Camera;camera.fieldOfView=60;
            var root=GameObject.Find("SaBubong/Dressing/Metro rooftops/Inhabited neighbor roofs");Assert.AreEqual(6,root.transform.childCount);
            var pairs=root.transform.Cast<Transform>().Select((t,index)=>
            {
                var filter=t.GetComponent<MeshFilter>();var renderer=t.GetComponent<MeshRenderer>();
                // Provenance belongs to the saved derivative, not a runtime mesh instance.
                string path=AssetImporter.GetAtPath("Assets/TumbangPreso/Art/SaBubong/ApartmentFinish/Apartment"+index+".asset").userData;
                Assert.IsTrue(path.StartsWith("TUMP_ROOF_APARTMENT_SOURCE:"));
                var beforeMesh=AssetDatabase.LoadAssetAtPath<Mesh>(path.Substring("TUMP_ROOF_APARTMENT_SOURCE:".Length));
                var beforeMaterial=AssetDatabase.LoadAssetAtPath<Material>(renderer.sharedMaterial.GetTag("TumpApartmentSource",false));
                Assert.IsNotNull(beforeMesh);Assert.IsNotNull(beforeMaterial);
                return new {filter,renderer,beforeMesh,beforeMaterial,afterMesh=filter.sharedMesh,afterMaterial=renderer.sharedMaterial,details=t.Find("Apartment construction finish").gameObject};
            }).ToArray();
            Assert.IsEmpty(root.GetComponentsInChildren<Collider>());
            foreach(string view in new[]{"detail","court"})
            {
                camera.transform.position=view=="detail"?new Vector3(12,10,21):new Vector3(3,1.8f,8);
                camera.transform.LookAt(new Vector3(0,1.7f,39));
                foreach(string state in new[]{"before","after"})
                {
                    bool after=state=="after";
                    foreach(var p in pairs){p.filter.sharedMesh=after?p.afterMesh:p.beforeMesh;p.renderer.sharedMaterial=after?p.afterMaterial:p.beforeMaterial;p.details.SetActive(after);}
                    yield return null;
                    using(Visual.NeighbourhoodSkyMotion.At(20))
                        yield return GameplayShots.Render(camera,"SaBubong-apartment-"+view+"-"+state,false,Output,width:1280,height:800);
                }
            }
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator SaBubongNeighborRoofReview()
        {
            var canvas=new GameObject("SaBubong neighbor preview",typeof(Canvas));
            var surface=new GameObject("Actual map preview",typeof(RectTransform),typeof(CanvasRenderer),typeof(UnityEngine.UI.RawImage));
            surface.transform.SetParent(canvas.transform,false);((RectTransform)surface.transform).sizeDelta=new Vector2(1920,1080);
            var preview=surface.AddComponent<MapPreviewSurface>();preview.Show(SceneFlow.SaBubong);
            float deadline=Time.realtimeSinceStartup+30;
            while(preview.Showing!=SceneFlow.SaBubong&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.AreEqual(SceneFlow.SaBubong,preview.Showing);preview.enabled=false;
            var context=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Single(t=>t.name=="Inhabited neighbor roofs").gameObject;
            Assert.AreEqual(6,context.transform.childCount);Assert.IsEmpty(context.GetComponentsInChildren<Collider>());
            foreach(var r in context.GetComponentsInChildren<MeshRenderer>())
            {Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.Off,r.shadowCastingMode);Assert.AreEqual(1,r.sharedMaterial.GetFloat("_SurfaceVertexRoles"));}
            foreach(string state in new[]{"before","after"})
            {
                context.SetActive(state=="after");yield return null;
                using(Visual.NeighbourhoodSkyMotion.At(20))
                    yield return GameplayShots.Render(preview.Camera,"SaBubong-neighbors-preview-"+state,false,Output,width:1280,height:720);
            }
            Object.Destroy(canvas);yield return PlayModeWorld.Reset();yield return MapRetrievalProbe.Load(SceneFlow.SaBubong);
            var rig=Object.FindFirstObjectByType<CameraRig>();rig.enabled=false;
            foreach(var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None))arms.gameObject.SetActive(false);
            var camera=rig.Camera;camera.fieldOfView=60;
            context=GameObject.Find("SaBubong/Dressing/Metro rooftops/Inhabited neighbor roofs");
            foreach(string view in new[]{"north-player","west-player","roof-detail"})
            {
                camera.transform.position=view=="north-player"?new Vector3(3,1.8f,8):view=="west-player"?new Vector3(-8,1.8f,8):new Vector3(12,10,21);
                camera.transform.LookAt(view=="west-player"?new Vector3(-75,1,39):new Vector3(0,1.7f,39));
                foreach(string state in new[]{"before","after"})
                {
                    context.SetActive(state=="after");yield return null;
                    using(Visual.NeighbourhoodSkyMotion.At(20))
                        yield return GameplayShots.Render(camera,"SaBubong-neighbors-"+view+"-"+state,false,Output,width:1280,height:800);
                }
            }
            GraphicsProfiles.Apply(0);yield return null;
            camera.transform.position=new Vector3(3,1.8f,8);camera.transform.LookAt(new Vector3(0,1.7f,39));
            using(Visual.NeighbourhoodSkyMotion.At(20))
                yield return GameplayShots.Render(camera,"SaBubong-neighbors-low",false,Output,width:960,height:540);
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator SaBubongShadeFinishReview()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.SaBubong);
            var rig=Object.FindFirstObjectByType<CameraRig>();rig.enabled=false;
            foreach(var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None))arms.gameObject.SetActive(false);
            var camera=rig.Camera;camera.fieldOfView=55;
            var finish=GameObject.Find("SaBubong/Dressing/Resident shade finish");Assert.IsNotNull(finish);Assert.IsEmpty(finish.GetComponentsInChildren<Collider>());
            foreach(string view in new[]{"under","roof"})
            {
                camera.transform.position=view=="under"?new Vector3(-7,2,-12):new Vector3(-6,6.5f,-12);
                camera.transform.LookAt(new Vector3(-10.25f,view=="under"?2.8f:3.3f,-8.4f));
                foreach(string state in new[]{"before","after"})
                {
                    finish.SetActive(state=="after");yield return null;
                    using(Visual.NeighbourhoodSkyMotion.At(20))
                        yield return GameplayShots.Render(camera,"SaBubong-shade-"+view+"-"+state,false,Output,width:1280,height:800);
                }
            }
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator SaBubongStairheadFinishReview()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.SaBubong);
            var rig=Object.FindFirstObjectByType<CameraRig>();rig.enabled=false;
            foreach(var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None))arms.gameObject.SetActive(false);
            var camera=rig.Camera;camera.fieldOfView=55;
            var finish=GameObject.Find("SaBubong/Dressing/Resident stairhead finish");Assert.IsNotNull(finish);Assert.IsEmpty(finish.GetComponentsInChildren<Collider>());
            foreach(string view in new[]{"court","near"})
            {
                camera.transform.position=view=="court"?new Vector3(3,2.8f,12):new Vector3(7.9f,1.9f,12.4f);
                camera.transform.LookAt(new Vector3(8,1.55f,17.5f));
                foreach(string state in new[]{"before","after"})
                {
                    finish.SetActive(state=="after");yield return null;
                    using(Visual.NeighbourhoodSkyMotion.At(20))
                        yield return GameplayShots.Render(camera,"SaBubong-stairhead-"+view+"-"+state,false,Output,width:1280,height:800);
                }
            }
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator SaBubongTankFinishReview()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.SaBubong);
            var rig=Object.FindFirstObjectByType<CameraRig>();rig.enabled=false;
            foreach(var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None))arms.gameObject.SetActive(false);
            var camera=rig.Camera;camera.fieldOfView=55;
            var finish=GameObject.Find("SaBubong/Dressing/Resident tank finish");Assert.IsNotNull(finish);Assert.IsEmpty(finish.GetComponentsInChildren<Collider>());
            var tank=GameObject.Find("SaBubong/Dressing/Laundry service corner/Resident water tank").GetComponent<MeshRenderer>();
            var after=tank.sharedMaterial;var before=AssetDatabase.LoadAssetAtPath<Material>(after.GetTag("TumpRoofTankSource",false));Assert.IsNotNull(before);
            foreach(string view in new[]{"service","near"})
            {
                camera.transform.position=view=="service"?new Vector3(-3,2.4f,13):new Vector3(-.6f,2.2f,17.5f);
                camera.transform.LookAt(view=="service"?new Vector3(-5.2f,1.3f,19):new Vector3(-2.9f,1.15f,19.5f));
                foreach(string state in new[]{"before","after"})
                {
                    finish.SetActive(state=="after");tank.sharedMaterial=state=="after"?after:before;yield return null;
                    using(Visual.NeighbourhoodSkyMotion.At(20))
                        yield return GameplayShots.Render(camera,"SaBubong-tank-"+view+"-"+state,false,Output,width:1280,height:800);
                }
            }
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator SaBubongArtBaselineReview()
        {
            var canvas=new GameObject("SaBubong preview review",typeof(Canvas));
            var surface=new GameObject("Actual map preview",typeof(RectTransform),typeof(CanvasRenderer),typeof(UnityEngine.UI.RawImage));
            surface.transform.SetParent(canvas.transform,false);((RectTransform)surface.transform).sizeDelta=new Vector2(1920,1080);
            var preview=surface.AddComponent<MapPreviewSurface>();preview.Show(SceneFlow.SaBubong);
            float deadline=Time.realtimeSinceStartup+30;
            while(preview.Showing!=SceneFlow.SaBubong && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.AreEqual(SceneFlow.SaBubong,preview.Showing);preview.enabled=false;
            using(Visual.NeighbourhoodSkyMotion.At(20))
                yield return GameplayShots.Render(preview.Camera,"SaBubong-preview",false,Output,width:1280,height:720);
            Object.Destroy(canvas);yield return PlayModeWorld.Reset();yield return MapRetrievalProbe.Load(SceneFlow.SaBubong);
            var rig=Object.FindFirstObjectByType<CameraRig>();rig.enabled=false;
            foreach(var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None))arms.gameObject.SetActive(false);
            var camera=rig.Camera;camera.fieldOfView=60;GraphicsProfiles.Apply(2);yield return null;
            foreach(string view in new[]{"court","shade","laundry","stairhead","pool","neighbor-roofs"})
            {
                camera.transform.position=view=="court"?new Vector3(-3,1.7f,-10):view=="shade"?new Vector3(-6,2.4f,-11):
                    view=="laundry"?new Vector3(-3,2.4f,13):view=="stairhead"?new Vector3(3,2.8f,12):
                    view=="pool"?new Vector3(-3,2.5f,1):new Vector3(0,12,-10);
                camera.transform.LookAt(view=="court"?new Vector3(3,2.2f,16):view=="shade"?new Vector3(-10.6f,1,-7.2f):
                    view=="laundry"?new Vector3(-5.2f,1.3f,19):view=="stairhead"?new Vector3(8.4f,1.5f,18.3f):
                    view=="pool"?new Vector3(-10,0,5):new Vector3(32,-5,32));
                using(Visual.NeighbourhoodSkyMotion.At(20))
                    yield return GameplayShots.Render(camera,"SaBubong-"+view,false,Output,width:1280,height:800);
            }
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator IlalimFinalArtReview()
        {
            var canvas=new GameObject("Ilalim final preview",typeof(Canvas));
            var surface=new GameObject("Actual map preview",typeof(RectTransform),typeof(CanvasRenderer),typeof(UnityEngine.UI.RawImage));
            surface.transform.SetParent(canvas.transform,false);((RectTransform)surface.transform).sizeDelta=new Vector2(1920,1080);
            var preview=surface.AddComponent<MapPreviewSurface>();preview.Show(SceneFlow.IlalimNgTulay);
            float deadline=Time.realtimeSinceStartup+30;
            while(preview.Showing!=SceneFlow.IlalimNgTulay && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.AreEqual(SceneFlow.IlalimNgTulay,preview.Showing);preview.enabled=false;
            using(Visual.NeighbourhoodSkyMotion.At(20))
                yield return GameplayShots.Render(preview.Camera,"Ilalim-final-preview",false,Output,width:960,height:540);
            Object.Destroy(canvas);yield return PlayModeWorld.Reset();yield return MapRetrievalProbe.Load(SceneFlow.IlalimNgTulay);
            var rig=Object.FindFirstObjectByType<CameraRig>();rig.enabled=false;
            foreach(var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None))arms.gameObject.SetActive(false);
            var camera=rig.Camera;camera.fieldOfView=58;
            var ground=GameObject.Find("IlalimNgTulay/Dressing/Lupa/FarGroundPlate");Assert.IsEmpty(ground.GetComponentsInChildren<Collider>());
            var filter=ground.GetComponent<MeshFilter>();var mesh=filter.sharedMesh;var scale=ground.transform.localScale;
            var previous=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/TumbangPreso/Art/EnvironmentSurfaces/Meshes/Surface_0000000000000000e000000000000000_10202_240.0000_0.8000_240.0000.asset");Assert.IsNotNull(previous);
            camera.transform.position=new Vector3(25,25,-30);camera.transform.LookAt(new Vector3(45,9,15));
            foreach(string state in new[]{"before","after"})
            {
                ground.transform.localScale=state=="after"?scale:new Vector3(240,scale.y,240);filter.sharedMesh=state=="after"?mesh:previous;
                using(Visual.NeighbourhoodSkyMotion.At(20))
                    yield return GameplayShots.Render(camera,"Ilalim-ground-"+state,false,Output,width:1280,height:720);
            }
            foreach(string view in new[]{"west-north","west-south","east-north","east-south"})
            {
                bool west=view.StartsWith("west");float z=view.EndsWith("north")?9:-9;
                camera.transform.position=new Vector3(west?-3.7f:3.7f,2,z);
                camera.transform.LookAt(new Vector3(west?-11.5f:11.5f,1.7f,z+1.5f));
                using(Visual.NeighbourhoodSkyMotion.At(20))
                    yield return GameplayShots.Render(camera,"Ilalim-final-"+view,false,Output,width:1280,height:800);
            }
            camera.transform.position=new Vector3(-2.8f,1.8f,-10);camera.transform.LookAt(new Vector3(3,2.2f,20));
            GraphicsProfiles.Apply(0);yield return null;
            using(Visual.NeighbourhoodSkyMotion.At(20))
                yield return GameplayShots.Render(camera,"Ilalim-final-low",false,Output,width:960,height:540);
            GraphicsProfiles.Apply(2);camera.transform.position=new Vector3(-9,2,0);camera.transform.LookAt(new Vector3(-25,25,30));
            foreach(int seconds in new[]{20,180})
                using(Visual.NeighbourhoodSkyMotion.At(seconds))
                    yield return GameplayShots.Render(camera,"Ilalim-sky-"+seconds,false,Output,width:1280,height:720);
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator IlalimSkylineFinishReview()
        {
            var canvas=new GameObject("Ilalim skyline preview",typeof(Canvas));
            var surface=new GameObject("Actual map preview",typeof(RectTransform),typeof(CanvasRenderer),typeof(UnityEngine.UI.RawImage));
            surface.transform.SetParent(canvas.transform,false);((RectTransform)surface.transform).sizeDelta=new Vector2(1920,1080);
            var preview=surface.AddComponent<MapPreviewSurface>();preview.Show(SceneFlow.IlalimNgTulay);
            float deadline=Time.realtimeSinceStartup+30;
            while(preview.Showing!=SceneFlow.IlalimNgTulay && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.AreEqual(SceneFlow.IlalimNgTulay,preview.Showing);preview.enabled=false;
            var toggle=IlalimSkylineSwitch();
            foreach(string state in new[]{"before","after"})
            {
                toggle(state=="after");yield return null;
                using(Visual.NeighbourhoodSkyMotion.At(20))
                    yield return GameplayShots.Render(preview.Camera,"Ilalim-skyline-preview-"+state,false,Output,width:1280,height:720);
            }
            Object.Destroy(canvas);yield return PlayModeWorld.Reset();yield return MapRetrievalProbe.Load(SceneFlow.IlalimNgTulay);
            var rig=Object.FindFirstObjectByType<CameraRig>();rig.enabled=false;
            foreach(var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None))arms.gameObject.SetActive(false);
            var camera=rig.Camera;camera.fieldOfView=58;toggle=IlalimSkylineSwitch();
            foreach(string view in new[]{"street","district"})
            {
                camera.transform.position=view=="street"?new Vector3(-2.8f,1.8f,-10):new Vector3(25,25,-30);
                camera.transform.LookAt(view=="street"?new Vector3(3,5,30):new Vector3(45,9,15));
                foreach(string state in new[]{"before","after"})
                {
                    toggle(state=="after");yield return null;
                    using(Visual.NeighbourhoodSkyMotion.At(20))
                        yield return GameplayShots.Render(camera,"Ilalim-skyline-"+view+"-"+state,false,Output,width:1280,height:800);
                }
            }
        }
        private static Action<bool> IlalimSkylineSwitch()
        {
            const string tag="TumpIlalimSkylineSource";
            var root=GameObject.Find("IlalimNgTulay/Dressing/IlalimSkylineFinishes");Assert.IsNotNull(root);
            Assert.IsEmpty(root.GetComponentsInChildren<Collider>());
            var originals=new System.Collections.Generic.List<(MeshRenderer renderer,Material[] before,Material[] after)>();
            foreach(var renderer in GameObject.Find("IlalimNgTulay/Dressing/SkylineKit").GetComponentsInChildren<MeshRenderer>())
            {
                var after=renderer.sharedMaterials;if(!after.Any(m=>!string.IsNullOrEmpty(m.GetTag(tag,false))))continue;
                var before=after.Select(m=>string.IsNullOrEmpty(m.GetTag(tag,false))?m:AssetDatabase.LoadAssetAtPath<Material>(m.GetTag(tag,false))).ToArray();
                Assert.IsTrue(before.All(m=>m!=null));originals.Add((renderer,before,after));
            }
            Assert.IsNotEmpty(originals);
            return after=>{root.SetActive(after);foreach(var item in originals)item.renderer.sharedMaterials=after?item.after:item.before;};
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator IlalimSkylineMaterialStudy()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.IlalimNgTulay);
            var rig=Object.FindFirstObjectByType<CameraRig>();rig.enabled=false;var camera=rig.Camera;
            var targets=GameObject.Find("IlalimNgTulay/Dressing/SkylineKit").GetComponentsInChildren<MeshRenderer>();
            var all=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);var states=all.Select(r=>r.enabled).ToArray();
            bool fog=RenderSettings.fog;
            try
            {
                foreach(var renderer in all)renderer.enabled=false;
                RenderSettings.fog=false;camera.fieldOfView=35;
                foreach(char kind in "abc")
                {
                    string guid=AssetDatabase.AssetPathToGUID("Assets/TumbangPreso/Art/models/kits/commercial/low-detail-building-"+kind+".glb");
                    var target=targets.Where(r=>AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>().sharedMesh).Contains(guid))
                        .OrderBy(r=>r.bounds.center.sqrMagnitude).First();
                    target.enabled=true;var bounds=target.bounds;
                    camera.transform.position=bounds.center+new Vector3(.65f,.15f,-1).normalized*bounds.size.y*2;
                    camera.transform.LookAt(bounds.center);
                    using(Visual.NeighbourhoodSkyMotion.At(20))
                        yield return GameplayShots.Render(camera,"Ilalim-skyline-study-"+kind,false,Output,width:960,height:960);
                    target.enabled=false;
                }
            }
            finally
            {RenderSettings.fog=fog;for(int i=0;i<all.Length;i++)if(all[i]!=null)all[i].enabled=states[i];}
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator IlalimStructureFinishReview()
        {
            var canvas=new GameObject("Ilalim structure preview",typeof(Canvas));
            var surface=new GameObject("Actual map preview",typeof(RectTransform),typeof(CanvasRenderer),typeof(UnityEngine.UI.RawImage));
            surface.transform.SetParent(canvas.transform,false);((RectTransform)surface.transform).sizeDelta=new Vector2(1920,1080);
            var preview=surface.AddComponent<MapPreviewSurface>();preview.Show(SceneFlow.IlalimNgTulay);
            float deadline=Time.realtimeSinceStartup+30;
            while(preview.Showing!=SceneFlow.IlalimNgTulay && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.AreEqual(SceneFlow.IlalimNgTulay,preview.Showing);preview.enabled=false;
            var toggle=IlalimStructureSwitch();
            foreach(string state in new[]{"before","after"})
            {
                toggle(state=="after");yield return null;
                using(Visual.NeighbourhoodSkyMotion.At(20))
                    yield return GameplayShots.Render(preview.Camera,"Ilalim-structure-preview-"+state,false,Output,width:1280,height:720);
            }
            Object.Destroy(canvas);yield return PlayModeWorld.Reset();
            yield return MapRetrievalProbe.Load(SceneFlow.IlalimNgTulay);
            var rig=Object.FindFirstObjectByType<CameraRig>();rig.enabled=false;
            foreach(var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None))arms.gameObject.SetActive(false);
            var camera=rig.Camera;camera.fieldOfView=65;toggle=IlalimStructureSwitch();
            foreach(string view in new[]{"north","under-deck"})
            {
                camera.transform.position=view=="north"?new Vector3(-2.8f,1.8f,-10):new Vector3(0,2,-5);
                camera.transform.LookAt(view=="north"?new Vector3(3,2.2f,20):new Vector3(0,9,15));
                foreach(string state in new[]{"before","after"})
                {
                    toggle(state=="after");yield return null;
                    using(Visual.NeighbourhoodSkyMotion.At(20))
                        yield return GameplayShots.Render(camera,"Ilalim-structure-"+view+"-"+state,false,Output,width:1280,height:800);
                }
            }
        }
        private static Action<bool> IlalimStructureSwitch()
        {
            const string tag="TumpIlalimStructureSource",marker="TUMP_ILALIM_STRUCTURE_MESH_SOURCE:";
            var materials=new System.Collections.Generic.List<(MeshRenderer renderer,Material[] before,Material[] after)>();
            var meshes=new System.Collections.Generic.List<(MeshFilter filter,Mesh before,Mesh after)>();
            var root=GameObject.Find("IlalimNgTulay/Dressing/Tulay");Assert.IsNotNull(root);
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())
            {
                var after=renderer.sharedMaterials;if(!after.Any(m=>!string.IsNullOrEmpty(m.GetTag(tag,false))))continue;
                var before=after.Select(m=>string.IsNullOrEmpty(m.GetTag(tag,false))?m:AssetDatabase.LoadAssetAtPath<Material>(m.GetTag(tag,false))).ToArray();
                Assert.IsTrue(before.All(m=>m!=null));materials.Add((renderer,before,after));
                var filter=renderer.GetComponent<MeshFilter>();string path=AssetDatabase.GetAssetPath(filter.sharedMesh);
                var importer=AssetImporter.GetAtPath(path);string data=importer!=null?importer.userData:"";
                if(data.StartsWith(marker,StringComparison.Ordinal))
                {
                    var original=AssetDatabase.LoadAssetAtPath<Mesh>(data.Substring(marker.Length));Assert.IsNotNull(original);
                    meshes.Add((filter,original,filter.sharedMesh));
                }
            }
            Assert.IsNotEmpty(materials);Assert.IsNotEmpty(meshes);
            return after=>
            {
                foreach(var item in materials)item.renderer.sharedMaterials=after?item.after:item.before;
                foreach(var item in meshes)item.filter.sharedMesh=after?item.after:item.before;
            };
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator IlalimArtBaselineReview()
        {
            var canvas=new GameObject("Ilalim preview review",typeof(Canvas));
            var surface=new GameObject("Actual map preview",typeof(RectTransform),typeof(CanvasRenderer),typeof(UnityEngine.UI.RawImage));
            surface.transform.SetParent(canvas.transform,false);((RectTransform)surface.transform).sizeDelta=new Vector2(1920,1080);
            var preview=surface.AddComponent<MapPreviewSurface>();preview.Show(SceneFlow.IlalimNgTulay);
            float deadline=Time.realtimeSinceStartup+30;
            while(preview.Showing!=SceneFlow.IlalimNgTulay && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.AreEqual(SceneFlow.IlalimNgTulay,preview.Showing);preview.enabled=false;
            using(Visual.NeighbourhoodSkyMotion.At(20))
                yield return GameplayShots.Render(preview.Camera,"Ilalim-preview",false,Output,width:1280,height:720);
            Object.Destroy(canvas);yield return PlayModeWorld.Reset();
            yield return MapRetrievalProbe.Load(SceneFlow.IlalimNgTulay);
            var rig=Object.FindFirstObjectByType<CameraRig>();rig.enabled=false;
            foreach(var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None))arms.gameObject.SetActive(false);
            var camera=rig.Camera;camera.fieldOfView=65;GraphicsProfiles.Apply(2);yield return null;
            foreach(string view in new[]{"north","south","west-shops","east-shops","structure"})
            {
                camera.transform.position=view=="north"?new Vector3(-2.8f,1.8f,-10):view=="south"?new Vector3(2.8f,1.8f,10):
                    view=="west-shops"?new Vector3(-3.5f,2.6f,0):view=="east-shops"?new Vector3(3.5f,2.6f,-2):new Vector3(0,2,-5);
                camera.transform.LookAt(view=="north"?new Vector3(3,2.2f,20):view=="south"?new Vector3(-2,2.2f,-25):
                    view=="west-shops"?new Vector3(-11,2,0):view=="east-shops"?new Vector3(11,2,0):new Vector3(0,9,15));
                using(Visual.NeighbourhoodSkyMotion.At(20))
                    yield return GameplayShots.Render(camera,"Ilalim-"+view,false,Output,width:1280,height:800);
            }
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator BayanHouseFinishReview()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza);
            var rig=Object.FindFirstObjectByType<CameraRig>();rig.enabled=false;
            foreach(var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None))arms.gameObject.SetActive(false);
            var camera=rig.Camera;camera.fieldOfView=55;
            var finishes=GameObject.Find("BayanPlaza/Dressing/BayanHouseFinishes");Assert.IsNotNull(finishes);
            Assert.IsEmpty(finishes.GetComponentsInChildren<Collider>());
            foreach(int index in new[]{4,6,1,3})
            {
                var home=GameObject.Find("BayanPlaza/Dressing/Bahay/Bahay_Civic_"+index);var body=home.GetComponent<MeshRenderer>().bounds;
                camera.transform.position=index%2==0?new Vector3(body.center.x+11,3.8f,body.center.z+3.5f):
                    new Vector3(body.center.x+5.5f,6.2f,body.center.z+6.4f);
                camera.transform.LookAt(body.center);
                foreach(string state in new[]{"before","after"})
                {
                    finishes.SetActive(state=="after");yield return null;
                    using(Visual.NeighbourhoodSkyMotion.At(20))
                        yield return GameplayShots.Render(camera,"Bayan-finish-"+index+"-"+state,false,Output,width:1280,height:800);
                }
            }
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator BayanFinalArtReview()
        {
            var canvas=new GameObject("Bayan final preview",typeof(Canvas));
            var surface=new GameObject("Actual map preview",typeof(RectTransform),typeof(CanvasRenderer),typeof(UnityEngine.UI.RawImage));
            surface.transform.SetParent(canvas.transform,false);((RectTransform)surface.transform).sizeDelta=new Vector2(1920,1080);
            var preview=surface.AddComponent<MapPreviewSurface>();preview.Show(SceneFlow.BayanPlaza);
            float deadline=Time.realtimeSinceStartup+30;
            while(preview.Showing!=SceneFlow.BayanPlaza && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.AreEqual(SceneFlow.BayanPlaza,preview.Showing);preview.enabled=false;
            using(Visual.NeighbourhoodSkyMotion.At(20))
                yield return GameplayShots.Render(preview.Camera,"Bayan-final-preview",false,Output,width:960,height:540);
            Object.Destroy(canvas);yield return PlayModeWorld.Reset();
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza);
            var rig=Object.FindFirstObjectByType<CameraRig>();rig.enabled=false;
            foreach(var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None))arms.gameObject.SetActive(false);
            var camera=rig.Camera;camera.fieldOfView=55;GraphicsProfiles.Apply(2);yield return null;
            for(int index=0;index<4;index++)
            {
                var house=GameObject.Find("BayanPlaza/Dressing/Bahay/Bahay_Civic_"+index);Assert.IsNotNull(house);
                var bodies=house.GetComponentsInChildren<MeshRenderer>();var b=bodies[0].bounds;
                foreach(var body in bodies)b.Encapsulate(body.bounds);
                // Alternating source IDs are the back row. View those from their
                // service lane, not from inside the first row's roof.
                camera.transform.position=index%2==0?new Vector3(b.center.x+11,3.8f,b.center.z+3.5f):
                    new Vector3(b.center.x+4.4f,3.8f,b.center.z+8);
                camera.transform.LookAt(b.center);
                using(Visual.NeighbourhoodSkyMotion.At(20))
                    yield return GameplayShots.Render(camera,"Bayan-house-"+index,false,Output,width:1280,height:800);
            }
            foreach(string view in new[]{"church","hall","service-rear","south"})
            {
                camera.transform.position=view=="church"?new Vector3(-5.5f,3.6f,2.5f):view=="hall"?new Vector3(10,3.8f,3.5f):
                    view=="service-rear"?new Vector3(6,4.3f,30):new Vector3(-3,1.7f,10);
                camera.transform.LookAt(view=="church"?new Vector3(-4.2f,4.2f,15):view=="hall"?new Vector3(7.8f,3.5f,15):
                    view=="service-rear"?new Vector3(5,3,20):new Vector3(-3,2.3f,-40));
                using(Visual.NeighbourhoodSkyMotion.At(20))
                    yield return GameplayShots.Render(camera,"Bayan-final-"+view,false,Output,width:1280,height:800);
            }
            GraphicsProfiles.Apply(0);yield return null;
            using(Visual.NeighbourhoodSkyMotion.At(20))
                yield return GameplayShots.Render(camera,"Bayan-final-south-low",false,Output,width:960,height:540);
            GraphicsProfiles.Apply(2);camera.transform.position=new Vector3(0,2,0);camera.transform.LookAt(new Vector3(-8,15,40));
            foreach(int seconds in new[]{20,180})
                using(Visual.NeighbourhoodSkyMotion.At(seconds))
                    yield return GameplayShots.Render(camera,"Bayan-sky-"+seconds,false,Output,width:1280,height:720);
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator BayanPottedShrubReview()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza);
            var rig=Object.FindFirstObjectByType<CameraRig>();rig.enabled=false;
            foreach(var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None))arms.gameObject.SetActive(false);
            var camera=rig.Camera;camera.fieldOfView=58;
            var originals=new[]{"MonHedge_1","RimHedge_0","RimHedge_1","RimHedge_2"}
                .Select(name=>GameObject.Find("BayanPlaza/Dressing/Monument/"+name+"/default").GetComponent<MeshRenderer>()).ToArray();
            foreach(var original in originals)Assert.IsNotNull(original.transform.Find("PlantedShrub"));
            foreach(string view in new[]{"group","near"})
            {
                camera.transform.position=view=="group"?new Vector3(6.7f,3,-24):new Vector3(3.8f,1.65f,-16.1f);
                camera.transform.LookAt(view=="group"?new Vector3(6.7f,.78f,-13.5f):new Vector3(5.1f,.78f,-13.5f));
                foreach(string state in new[]{"before","after"})
                {
                    foreach(var original in originals)
                    {original.enabled=state=="before";original.transform.Find("PlantedShrub").gameObject.SetActive(state=="after");}
                    yield return null;
                    using(Visual.NeighbourhoodSkyMotion.At(20))
                        yield return GameplayShots.Render(camera,"Bayan-pots-"+view+"-"+state,false,Output,width:1280,height:800);
                }
            }
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator BayanTownContextReview()
        {
            var canvas=new GameObject("Bayan context preview",typeof(Canvas));
            var surface=new GameObject("Actual map preview",typeof(RectTransform),typeof(CanvasRenderer),typeof(UnityEngine.UI.RawImage));
            surface.transform.SetParent(canvas.transform,false);((RectTransform)surface.transform).sizeDelta=new Vector2(1920,1080);
            var preview=surface.AddComponent<MapPreviewSurface>();preview.Show(SceneFlow.BayanPlaza);
            float deadline=Time.realtimeSinceStartup+30;
            while(preview.Showing!=SceneFlow.BayanPlaza && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.AreEqual(SceneFlow.BayanPlaza,preview.Showing);preview.enabled=false;
            var context=GameObject.Find("BayanPlaza/Dressing/BayanTownContext");Assert.IsNotNull(context);
            Assert.IsEmpty(context.GetComponentsInChildren<Collider>());
            foreach(string state in new[]{"before","after"})
            {
                context.SetActive(state=="after");yield return null;
                using(Visual.NeighbourhoodSkyMotion.At(20))
                    yield return GameplayShots.Render(preview.Camera,"Bayan-context-preview-"+state,false,Output,width:1280,height:720);
            }
            Object.Destroy(canvas);yield return PlayModeWorld.Reset();
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza);
            var rig=Object.FindFirstObjectByType<CameraRig>();rig.enabled=false;
            foreach(var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None))arms.gameObject.SetActive(false);
            context=GameObject.Find("BayanPlaza/Dressing/BayanTownContext");var camera=rig.Camera;camera.fieldOfView=58;
            foreach(string view in new[]{"overview","north","south"})
            {
                camera.transform.position=view=="overview"?new Vector3(16,16,-22):new Vector3(0,1.7f,view=="north"?-10:10);
                camera.transform.LookAt(view=="overview"?new Vector3(0,1,1):new Vector3(0,2.3f,view=="north"?40:-40));
                foreach(string state in new[]{"before","after"})
                {
                    context.SetActive(state=="after");yield return null;
                    using(Visual.NeighbourhoodSkyMotion.At(20))
                        yield return GameplayShots.Render(camera,"Bayan-context-"+view+"-"+state,false,Output,width:1280,height:720);
                }
            }
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator BayanGardenMatchedReview()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza);
            var rig=Object.FindFirstObjectByType<CameraRig>();rig.enabled=false;
            foreach(var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None))arms.gameObject.SetActive(false);
            var camera=rig.Camera;camera.fieldOfView=55;GraphicsProfiles.Apply(2);
            var garden=GameObject.Find("BayanPlaza/Dressing/BayanGardenRefinement");Assert.IsNotNull(garden);
            Assert.IsEmpty(garden.GetComponentsInChildren<Collider>());
            var originals=new[]{"Ground/EdgeHedge_0","Ground/EdgeHedge_10","Ground/EdgeHedge_11","Ground/EdgeHedge_12",
                "Clutter/Clutter_0","Clutter/Clutter_1","Clutter/Clutter_2","Clutter/Clutter_3"}
                .Select(name=>GameObject.Find("BayanPlaza/Dressing/"+name).GetComponent<MeshRenderer>()).ToArray();
            foreach(string view in new[]{"overview","west","east"})
            {
                camera.transform.position=view=="overview"?new Vector3(16,16,-22):view=="west"?new Vector3(-10,2.2f,0):new Vector3(10,2.2f,7.6f);
                camera.transform.LookAt(view=="overview"?new Vector3(0,1,1):new Vector3(view=="west"?-13.1f:13.1f,.5f,3.6f));
                foreach(string state in new[]{"before","after"})
                {
                    garden.SetActive(state=="after");foreach(var original in originals)original.enabled=state=="before";
                    yield return null;
                    using(Visual.NeighbourhoodSkyMotion.At(20))
                        yield return GameplayShots.Render(camera,"Bayan-garden-"+view+"-"+state,false,Output,width:1280,height:800);
                }
            }
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator EskinitaStreetCompositionReview()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita);
            var rig=Object.FindFirstObjectByType<CameraRig>();rig.enabled=false;
            foreach(var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None))arms.gameObject.SetActive(false);
            var camera=rig.Camera;camera.fieldOfView=65;
            foreach(int quality in new[]{2,0})
            {
                GraphicsProfiles.Apply(quality);yield return null;
                foreach(string direction in new[]{"north","south","east","west"})
                {
                    Vector3 eye=direction=="north"?new Vector3(0,1.6f,-10):direction=="south"?new Vector3(0,1.6f,10):
                        direction=="east"?new Vector3(-5,1.6f,0):new Vector3(5,1.6f,0);
                    Vector3 aim=direction=="north"?new Vector3(0,1.6f,15):direction=="south"?new Vector3(0,1.6f,-15):
                        direction=="east"?new Vector3(10,1.6f,0):new Vector3(-10,1.6f,0);
                    camera.transform.position=eye;camera.transform.LookAt(aim);
                    using(Visual.NeighbourhoodSkyMotion.At(20))
                        yield return GameplayShots.Render(camera,"court-"+direction+"-quality"+quality,false,Output,width:960,height:540);
                }
            }
            GraphicsProfiles.Apply(2);camera.fieldOfView=55;yield return null;
            foreach(string lot in new[]{"W","E"})
            {
                var shop=GameObject.Find("Eskinita/Dressing/Kalat/SariSari_"+lot);Assert.IsNotNull(shop);
                var bodies=shop.GetComponentsInChildren<MeshRenderer>();var bounds=bodies[0].bounds;
                foreach(var body in bodies)bounds.Encapsulate(body.bounds);
                foreach(int side in new[]{-1,1})
                {
                    camera.transform.position=bounds.center+new Vector3(5,1.6f,side*6);
                    camera.transform.LookAt(bounds.center+Vector3.up*.2f);
                    using(Visual.NeighbourhoodSkyMotion.At(20))
                        yield return GameplayShots.Render(camera,"shop-"+lot+"-"+side,false,Output,width:1280,height:800);
                }
            }
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator EskinitaPrimaryHomesMaterialReview()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita);
            var rig = Object.FindFirstObjectByType<CameraRig>();
            rig.enabled = false;
            var camera = rig.Camera;
            camera.fieldOfView = 48;
            var houses = GameObject.Find("Eskinita/Dressing/Bahay").transform;
            string[] names = { "0_W", "1_W", "3_W", "4_W", "5_W", "1_E", "2_E", "3_E", "5_E", "6_E" };
            foreach (string name in names)
            {
                var home = houses.Find("Bahay_Rework_Bahay_" + name);
                Assert.IsNotNull(home, name);
                var renderers = home.GetComponentsInChildren<MeshRenderer>();
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                int side = name.EndsWith("W") ? 1 : -1;
                camera.transform.position = new Vector3(bounds.center.x + side * 12, 3.7f, bounds.center.z + 4.2f);
                camera.transform.LookAt(new Vector3(bounds.center.x, 2.2f, bounds.center.z));
                using (Visual.NeighbourhoodSkyMotion.At(20))
                    yield return GameplayShots.Render(camera, "home-" + name, false, Output, width: 1280, height: 800);
            }
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator EskinitaSteppedHomeKeepsOpeningsAndCollisionWithFittedMaterials()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita);
            var rig = Object.FindFirstObjectByType<CameraRig>();
            rig.enabled = false;
            // Compare the world finish without a held viewmodel covering the lower wall.
            foreach (var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None)) arms.gameObject.SetActive(false);
            var camera = rig.Camera; camera.fieldOfView = 48;
            var original = GameObject.Find("Eskinita/Dressing/Bahay/Bahay_Rework_Bahay_4_W");
            var finish = GameObject.Find("Eskinita/Dressing/EskinitaHouseRefinement/Bahay_4_W_FittedFinish");
            Assert.IsNotNull(original); Assert.IsNotNull(finish);
            Assert.IsEmpty(finish.GetComponentsInChildren<Collider>());
            var renderer = finish.GetComponent<MeshRenderer>();
            Assert.AreEqual("Bahay_4_W_materials", renderer.sharedMaterial.mainTexture.name);
            Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.Off, renderer.shadowCastingMode);
            var filter = finish.GetComponent<MeshFilter>();
            Assert.Greater(filter.sharedMesh.vertexCount, 100);
            var originals = original.GetComponentsInChildren<MeshRenderer>();
            var bounds = originals[0].bounds;
            foreach (var body in originals) bounds.Encapsulate(body.bounds);
            var allowed = bounds; allowed.Expand(.04f);
            Assert.IsTrue(allowed.Contains(renderer.bounds.min) && allowed.Contains(renderer.bounds.max), "Finish must remain fitted to the original solid.");
            var solids = original.GetComponentsInChildren<Collider>().Select(c => c.bounds).ToArray();
            foreach (string angle in new[] { "street", "frontage" })
            {
                camera.transform.position = angle == "street" ? new Vector3(bounds.center.x + 12, 3.7f, bounds.center.z + 4.2f) :
                    new Vector3(bounds.center.x + 8, 3.1f, bounds.center.z - 1.2f);
                camera.transform.LookAt(new Vector3(bounds.center.x, 2.3f, bounds.center.z));
                foreach (string state in new[] { "before", "after" })
                {
                    finish.SetActive(state == "after");
                    using (Visual.NeighbourhoodSkyMotion.At(20))
                        yield return GameplayShots.Render(camera, "stepped-" + angle + "-" + state, false, Output, width: 1280, height: 800);
                }
            }
            using (Visual.NeighbourhoodSkyMotion.At(20))
                yield return GameplayShots.Render(camera, "stepped-small", false, Output, width: 960, height: 540);
            CollectionAssert.AreEqual(solids, original.GetComponentsInChildren<Collider>().Select(c => c.bounds).ToArray());
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator EskinitaTerraceHomeTimberFollowsTheExistingCourses()
        { yield return ReviewTimberHomes(new[] { "5_W" }); }

        [UnityTest, Timeout(90000)]
        public IEnumerator EskinitaEastHomesTimberFollowsEachExistingFacade()
        { yield return ReviewTimberHomes(new[] { "2_E", "5_E", "6_E" }); }

        private IEnumerator ReviewTimberHomes(string[] lots)
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita);
            var rig = Object.FindFirstObjectByType<CameraRig>(); rig.enabled = false;
            foreach (var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None)) arms.gameObject.SetActive(false);
            var camera = rig.Camera; camera.fieldOfView = 45;
            foreach (string lot in lots)
            {
            var home = GameObject.Find("Eskinita/Dressing/Bahay/Bahay_Rework_Bahay_" + lot);
            var finish = GameObject.Find("Eskinita/Dressing/NeighborhoodRework/HouseFinish_Bahay_" + lot);
            Assert.IsNotNull(home); Assert.IsNotNull(finish);
            var renderers = finish.GetComponentsInChildren<MeshRenderer>();
            var after = renderers.Select(r => r.sharedMaterials).ToArray();
            int changed = 0;
            var before = after.Select(materials => materials.Select(m =>
            {
                string path = m.GetTag("TumpRefineOriginalMaterial", false);
                if (string.IsNullOrEmpty(path)) return m;
                changed++;
                var source = AssetDatabase.LoadAssetAtPath<Material>(path); Assert.IsNotNull(source);
                return source;
            }).ToArray()).ToArray();
            Assert.AreEqual(3, changed);
            var meshes = finish.GetComponentsInChildren<MeshFilter>().Select(f => f.sharedMesh).ToArray();
            var bodies = home.GetComponentsInChildren<MeshRenderer>(); var bounds = bodies[0].bounds;
            foreach (var body in bodies) bounds.Encapsulate(body.bounds);
            bool east = lot.EndsWith("E");
            foreach (string angle in new[] { east ? "lit-side" : "frontage", "street" })
            {
                camera.transform.position = angle == "lit-side" ? new Vector3(bounds.center.x + 5, 6.4f, bounds.center.z - 6) :
                    new Vector3(bounds.center.x + (east ? -1 : 1) * (angle == "frontage" ? 9 : 12), 4.6f,
                    bounds.center.z + (angle == "frontage" ? -3.8f : 4.2f));
                camera.transform.LookAt(new Vector3(bounds.center.x, 3.1f, bounds.center.z));
                foreach (string state in new[] { "before", "after" })
                {
                    for (int i = 0; i < renderers.Length; i++) renderers[i].sharedMaterials = state == "after" ? after[i] : before[i];
                    using (Visual.NeighbourhoodSkyMotion.At(20))
                        yield return GameplayShots.Render(camera, "timber" + lot.Replace("_", "").ToLowerInvariant() + "-" + angle + "-" + state, false, Output, width: 1280, height: 800);
                }
            }
            using (Visual.NeighbourhoodSkyMotion.At(20))
                yield return GameplayShots.Render(camera, "timber" + lot.Replace("_", "").ToLowerInvariant() + "-small", false, Output, width: 960, height: 540);
            CollectionAssert.AreEqual(meshes, finish.GetComponentsInChildren<MeshFilter>().Select(f => f.sharedMesh).ToArray());
            }
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator EskinitaDomesticFrontagePreservesNeighborsAndReauthoredFinishes()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita);
            var rig = Object.FindFirstObjectByType<CameraRig>(); rig.enabled = false;
            foreach (var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None)) arms.gameObject.SetActive(false);
            var camera = rig.Camera; camera.fieldOfView = 48;
            var added = GameObject.Find("Eskinita/Dressing/EskinitaDomestic3W");
            Assert.IsNotNull(added); Assert.IsEmpty(added.GetComponentsInChildren<Collider>());
            string originalPath = added.GetComponent<MeshRenderer>().sharedMaterial.GetTag("TumpRefineOriginalMesh", false);
            var original = AssetDatabase.LoadAssetAtPath<Mesh>(originalPath); Assert.IsNotNull(original);
            var detail = GameObject.Find("Eskinita/Dressing/NeighborhoodRework/HouseFinish_Bahay_3_W").GetComponentInChildren<MeshFilter>();
            var shop = GameObject.Find("Eskinita/Dressing/NeighborhoodRework/HouseFinish_Bahay_0_W").GetComponentInChildren<MeshFilter>();
            Assert.AreSame(original, shop.sharedMesh, "The neighboring shop's source must stay unchanged.");
            var after = detail.sharedMesh; var materials = detail.GetComponent<MeshRenderer>().sharedMaterials;
            int removed = 0;
            for (int sub = 0; sub < original.subMeshCount; sub++)
            {
                bool goods = materials[sub].name.StartsWith("Cream goods") || materials[sub].name.StartsWith("Oxblood goods");
                if (goods) { Assert.IsEmpty(after.GetTriangles(sub)); removed++; }
                else CollectionAssert.AreEqual(original.GetTriangles(sub), after.GetTriangles(sub), "Keep jalousies, sill, brackets and steps.");
            }
            Assert.AreEqual(2, removed);
            // This author run also fixes a real re-authoring serialization regression:
            // existing timber materials must retain the already accepted grain mode.
            int timber = 0;
            foreach (string lot in new[] { "5_W", "2_E", "5_E", "6_E" })
                foreach (var renderer in GameObject.Find("Eskinita/Dressing/NeighborhoodRework/HouseFinish_Bahay_" + lot).GetComponentsInChildren<MeshRenderer>())
                    foreach (var material in renderer.sharedMaterials)
                        if (!string.IsNullOrEmpty(material.GetTag("TumpRefineOriginalMaterial", false)))
                        { Assert.AreEqual(1, material.GetFloat("_DeckSurface"), lot + " reverted its fitted grain after re-authoring."); timber++; }
            Assert.AreEqual(12, timber);
            var home = GameObject.Find("Eskinita/Dressing/Bahay/Bahay_Rework_Bahay_3_W");
            var bodies = home.GetComponentsInChildren<MeshRenderer>(); var bounds = bodies[0].bounds;
            foreach (var body in bodies) bounds.Encapsulate(body.bounds);
            foreach (string angle in new[] { "frontage", "street" })
            {
                camera.transform.position = new Vector3(bounds.center.x + (angle == "frontage" ? 9 : 12), angle == "frontage" ? 2.7f : 3.7f, bounds.center.z + 3.8f);
                camera.transform.LookAt(new Vector3(bounds.center.x, 1.6f, bounds.center.z));
                foreach (string state in new[] { "before", "after" })
                {
                    detail.sharedMesh = state == "after" ? after : original;
                    added.SetActive(state == "after");
                    using (Visual.NeighbourhoodSkyMotion.At(20))
                        yield return GameplayShots.Render(camera, "domestic3w-" + angle + "-" + state, false, Output, width: 1280, height: 800);
                }
            }
            using (Visual.NeighbourhoodSkyMotion.At(20))
                yield return GameplayShots.Render(camera, "domestic3w-small", false, Output, width: 960, height: 540);
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator ExistingPassengerTricycleConstructionReview()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza);
            var rig = Object.FindFirstObjectByType<CameraRig>(); rig.enabled = false;
            foreach (var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None)) arms.gameObject.SetActive(false);
            var vehicle = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).FirstOrDefault(t => t.name == "TerminalTricycle_0");
            Assert.IsNotNull(vehicle, "Inspect the actual shipped passenger tricycle before making a new one.");
            var renderers = vehicle.GetComponentsInChildren<MeshRenderer>();
            var bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            var camera = rig.Camera; camera.fieldOfView = 40;
            var report = new StringBuilder("Read-only existing model review in its actual map. Not new placement or final acceptance.\n");
            report.AppendLine("bounds " + bounds);
            report.AppendLine("mesh vertices " + vehicle.GetComponentsInChildren<MeshFilter>().Sum(f => f.sharedMesh.vertexCount));
            report.AppendLine("materials " + string.Join(", ", renderers.SelectMany(r => r.sharedMaterials).Select(m => m.name)));
            File.WriteAllText(Path.Combine(Output, "existing-tricycle.txt"), report.ToString());
            for (int angle = 0; angle < 4; angle++)
            {
                float yaw = new[] { 0, 45, 90, 180 }[angle];
                Vector3 offset = Quaternion.Euler(0, yaw, 0) * new Vector3(0, 1.05f, 3.8f);
                camera.transform.position = bounds.center + vehicle.rotation * offset;
                camera.transform.LookAt(bounds.center);
                using (Visual.NeighbourhoodSkyMotion.At(20))
                    yield return GameplayShots.Render(camera, "existing-tricycle-" + new[] { "front", "quarter", "side", "back" }[angle], false, Output, width: 1200, height: 900);
            }
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator EskinitaPassengerTricycleFitsItsExistingHouseholdBay()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita);
            var rig=Object.FindFirstObjectByType<CameraRig>();rig.enabled=false;
            foreach(var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None))arms.gameObject.SetActive(false);
            var original=GameObject.Find("Eskinita/Dressing/Bahay/Sasakyan_Rework_Sasakyan_2_W");
            var vehicle=GameObject.Find("Eskinita/Dressing/EskinitaStreetLife/ParkedPassengerTricycle07");
            Assert.IsNotNull(original);Assert.IsNotNull(vehicle);Assert.IsEmpty(vehicle.GetComponentsInChildren<Collider>());
            var oldRenderers=original.GetComponentsInChildren<MeshRenderer>();
            Assert.IsTrue(oldRenderers.All(r=>!r.enabled));
            var oldBounds=oldRenderers[0].bounds;foreach(var renderer in oldRenderers)oldBounds.Encapsulate(renderer.bounds);
            var renderers=vehicle.GetComponentsInChildren<MeshRenderer>();var bounds=renderers[0].bounds;
            foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            Assert.That(bounds.min.y,Is.EqualTo(oldBounds.min.y).Within(.002f));
            Assert.GreaterOrEqual(bounds.min.x,oldBounds.min.x-.01f);Assert.LessOrEqual(bounds.max.x,oldBounds.max.x+.01f);
            Assert.GreaterOrEqual(bounds.min.z,oldBounds.min.z-.01f);Assert.LessOrEqual(bounds.max.z,oldBounds.max.z+.01f);
            Assert.IsTrue(renderers.SelectMany(r=>r.sharedMaterials).All(m=>m!=null&&m.shader.name=="TumbangPreso/NearFade"));
            var camera=rig.Camera;camera.fieldOfView=45;
            // Keep the retained bay as the look point across placement variants.
            camera.transform.position=new Vector3(0,2.1f,oldBounds.center.z+3.8f);
            camera.transform.LookAt(new Vector3(oldBounds.center.x,bounds.center.y+.2f,oldBounds.center.z));
            foreach(string state in new[]{"before","after"})
            {
                foreach(var renderer in oldRenderers)renderer.enabled=state=="before";
                vehicle.SetActive(state=="after");
                using(Visual.NeighbourhoodSkyMotion.At(20))
                    yield return GameplayShots.Render(camera,"tricycle-street-"+state,false,Output,width:1280,height:800);
            }
            using(Visual.NeighbourhoodSkyMotion.At(20))
                yield return GameplayShots.Render(camera,"tricycle-street-small",false,Output,width:960,height:540);
            camera.fieldOfView=40;
            for(int angle=0;angle<4;angle++)
            {
                float yaw=new[]{0,45,90,180}[angle];
                camera.transform.position=bounds.center+vehicle.transform.rotation*(Quaternion.Euler(0,yaw,0)*new Vector3(0,1.05f,3.8f));
                camera.transform.LookAt(bounds.center);
                using(Visual.NeighbourhoodSkyMotion.At(20))
                    yield return GameplayShots.Render(camera,"tricycle-"+new[]{"front","quarter","side","back"}[angle],false,Output,width:1200,height:900);
            }
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator EskinitaMasonryHomesKeepTheirSolidBodiesAndIndividualFinishes()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita);
            var rig=Object.FindFirstObjectByType<CameraRig>();rig.enabled=false;
            foreach(var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None))arms.gameObject.SetActive(false);
            var camera=rig.Camera;camera.fieldOfView=48;
            var root=GameObject.Find("Eskinita/Dressing/EskinitaMasonryFinishes");Assert.IsNotNull(root);
            Assert.AreEqual(5,root.transform.childCount);Assert.IsEmpty(root.GetComponentsInChildren<Collider>());
            Assert.AreEqual(5,root.GetComponentsInChildren<MeshRenderer>().Select(r=>r.sharedMaterial).Distinct().Count());
            foreach(string lot in new[]{"0_W","1_W","3_W","1_E","3_E"})
            {
                var original=GameObject.Find("Eskinita/Dressing/Bahay/Bahay_Rework_Bahay_"+lot);
                var finish=root.transform.Find("Finish_"+lot).gameObject;
                var bodies=original.GetComponentsInChildren<MeshRenderer>();var bounds=bodies[0].bounds;
                foreach(var body in bodies)bounds.Encapsulate(body.bounds);
                var renderer=finish.GetComponent<MeshRenderer>();var allowed=bounds;allowed.Expand(.04f);
                Assert.IsTrue(allowed.Contains(renderer.bounds.min)&&allowed.Contains(renderer.bounds.max),lot+" finish is not fitted.");
                Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.Off,renderer.shadowCastingMode);
                Assert.Greater(finish.GetComponent<MeshFilter>().sharedMesh.vertexCount,100);
                bool east=lot.EndsWith("E");int side=east?-1:1;
                foreach(string angle in new[]{"street","clear-side"})
                {
                    camera.transform.position=angle=="street"?new Vector3(bounds.center.x+side*12,3.7f,bounds.center.z+4.2f):
                        new Vector3(bounds.center.x+(east?7:9),east?6.0f:3.6f,bounds.center.z+(east?-8:-3.8f));
                    camera.transform.LookAt(new Vector3(bounds.center.x,2.1f,bounds.center.z));
                    foreach(string state in new[]{"before","after"})
                    {
                        finish.SetActive(state=="after");
                        using(Visual.NeighbourhoodSkyMotion.At(20))
                            yield return GameplayShots.Render(camera,"masonry-"+lot+"-"+angle+"-"+state,false,Output,width:1280,height:800);
                    }
                }
                using(Visual.NeighbourhoodSkyMotion.At(20))
                    yield return GameplayShots.Render(camera,"masonry-"+lot+"-small",false,Output,width:960,height:540);
            }
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator EskinitaOuterContextKeepsTheCourtAndUsesTheRealPreviewCamera()
        {
            var canvas = new GameObject("Context preview canvas", typeof(Canvas));
            var surface = new GameObject("Actual map preview", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.RawImage));
            surface.transform.SetParent(canvas.transform, false);
            ((RectTransform)surface.transform).sizeDelta = new Vector2(1920, 1080);
            var preview = surface.AddComponent<MapPreviewSurface>();
            preview.Show(SceneFlow.Eskinita);
            float deadline = Time.realtimeSinceStartup + 30;
            while (preview.Showing != SceneFlow.Eskinita && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.AreEqual(SceneFlow.Eskinita, preview.Showing);
            Assert.IsNotNull(preview.Camera);
            // Freeze this real preview pose for the paired geometry comparison. This component
            // has no OnDisable teardown; its camera and owned scene remain alive until destroy.
            preview.enabled = false;
            var context = GameObject.Find("Eskinita/Dressing/EskinitaContextRefinement");
            Assert.IsNotNull(context);
            var bodies = context.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("Bahay_Context_")).ToArray();
            Assert.AreEqual(26, bodies.Length);
            Assert.IsEmpty(context.GetComponentsInChildren<Collider>(true));
            var district = context.transform.Find("DistantDistrict");
            Assert.IsNotNull(district, "The owner's exposed background needs more than the first added row.");
            var blocks = district.GetComponentsInChildren<MeshRenderer>().Where(r => r.name.StartsWith("Block_")).ToArray();
            Assert.AreEqual(96, blocks.Length);
            Assert.AreEqual(1, blocks.Select(r => r.sharedMaterial).Distinct().Count(), "The distant blocks share a palette material.");
            foreach(var block in blocks)
            {
                Assert.Greater(block.GetComponent<MeshFilter>().sharedMesh.vertexCount, 100);
                Assert.AreEqual("EskinitaDistrictPalette", block.sharedMaterial.mainTexture.name);
                Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.Off, block.shadowCastingMode);
                var bounds = block.bounds;
                Assert.IsTrue(bounds.min.x >= 48 || bounds.max.x <= -48 || bounds.min.z >= 66 || bounds.max.z <= -66,
                    block.name + " enters the existing neighborhood.");
            }
            foreach (var body in bodies)
            {
                var renderers = body.GetComponentsInChildren<MeshRenderer>();
                Assert.IsNotEmpty(renderers);
                Assert.IsTrue(renderers.SelectMany(r => r.sharedMaterials).Any(m => m != null &&
                    m.mainTexture != null && m.mainTexture.name.StartsWith("colormap_roof_")),
                    body.name + " missed the existing neighborhood palette and kept the mint source roof.");
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                Assert.That(bounds.min.y, Is.EqualTo(.1f).Within(.01f), body.name + " floats.");
                Assert.IsTrue(bounds.min.x >= 28 || bounds.max.x <= -28 || bounds.min.z >= 47 || bounds.max.z <= -47,
                    body.name + " enters the retained neighborhood.");
            }
            Vector3 position = preview.Camera.transform.position;
            Quaternion rotation = preview.Camera.transform.rotation;
            foreach (string state in new[] { "before", "after" })
            {
                // Keep the initial row present in both images. The owner reported the empty
                // distance beyond it, so compare only the new, deeper neighborhood here.
                district.gameObject.SetActive(state == "after");
                yield return null;
                using (Visual.NeighbourhoodSkyMotion.At(20))
                    yield return GameplayShots.Render(preview.Camera, "Eskinita-preview-context-" + state,
                        false, Output, width: 1280, height: 720);
                Assert.AreEqual(position, preview.Camera.transform.position);
                Assert.AreEqual(rotation, preview.Camera.transform.rotation);
            }
            using (Visual.NeighbourhoodSkyMotion.At(20))
                yield return GameplayShots.Render(preview.Camera, "Eskinita-preview-context-small",
                    false, Output, width: 960, height: 540);
            Object.Destroy(canvas);
            yield return null;
        }
    }
}
