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
