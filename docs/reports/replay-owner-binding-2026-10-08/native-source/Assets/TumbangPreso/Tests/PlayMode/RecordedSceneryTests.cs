using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Map;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class RecordedSceneryTests
    {
        [DefaultExecutionOrder(9000)]private sealed class Observer:MonoBehaviour
        {
            public readonly HashSet<float> Times=new HashSet<float>();
            private void LateUpdate(){if(GameServices.Round?.RoundActive==true)Times.Add(Time.time);}
        }
        private Core.CustomRules _rules;private bool _pinned,_bots;
        [UnitySetUp]public IEnumerator Before(){yield return PlayModeWorld.Reset();_rules=SceneFlow.SelectedRules.Clone();_pinned=SceneFlow.RulesPinned;_bots=GameLaunch.AllBots;}
        [UnityTearDown]public IEnumerator After(){yield return PlayModeWorld.Reset();GameLaunch.AllBots=_bots;SceneFlow.AdoptRemoteRules(_rules);if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();}
        private static IEnumerator Load(string map)
        {
            GameLaunch.AllBots=true;yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(map);yield return null;
            float until=Time.realtimeSinceStartup+45;
            while((GameServices.Round?.RoundActive!=true||PresentationClock.Held)&&Time.realtimeSinceStartup<until)yield return null;
            Assert.IsTrue(GameServices.Round.RoundActive);
        }
        private static Color32[] Pixels(Camera camera,RenderTexture target)
        {
            camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;
            var texture=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);texture.Apply();
            var pixels=texture.GetPixels32();RenderTexture.active=previous;Object.Destroy(texture);return pixels;
        }
        private static float Difference(Color32[] a,Color32[] b)
        {long sum=0;for(int n=0;n<a.Length;n++)sum+=System.Math.Abs(a[n].r-b[n].r)+System.Math.Abs(a[n].g-b[n].g)+System.Math.Abs(a[n].b-b[n].b);return sum/(a.Length*3f);}
        [UnityTest]public IEnumerator ActualDroneAndDetachedMarkReplayUseTheSameRenderedGeometryAndMaterials()
        {
            yield return Load(SceneFlow.Arena);var sourceParent=new GameObject("Owned scenery control source");var copyParent=new GameObject("Owned scenery replay");
            var camera=new GameObject("Owned scenery comparison camera").AddComponent<Camera>();camera.CopyFrom(Camera.main);camera.enabled=false;camera.cullingMask=1<<30;
            camera.transform.SetPositionAndRotation(new Vector3(8,5,-8),Quaternion.LookRotation(new Vector3(-8,-3,8)));camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            var target=new RenderTexture(512,512,24,RenderTextureFormat.ARGB32);target.Create();camera.targetTexture=target;
            try
            {
                var drone=ArenaDrone.Build(sourceParent.transform,Object.FindAnyObjectByType<ArenaFallRecovery>().DroneTemplate);
                drone.Hold(new Vector3(0,3,0),ArenaDrone.Act.SetDown,.5f,2.5f,Vector3.zero,Vector3.zero,.016f,false);
                foreach(var node in sourceParent.GetComponentsInChildren<Transform>(true))node.gameObject.layer=30;
                var frame=new LocalReplaySceneryFrame{Time=10};
                frame.Roots.Add(LocalReplaySceneryState.CaptureRoot(drone.transform,LocalReplaySceneryKind.Drone,"","","test-drone",""));
                frame.Roots.Add(LocalReplaySceneryState.CaptureRoot(drone.RecordedLandingMark,LocalReplaySceneryKind.DroneMark,"","","test-drone",""));
                var segment=new LocalReplayScenerySegment();segment.Frames.Add(frame);
                var decoded=LocalReplaySceneryCodec.Decode(LocalReplaySceneryCodec.Encode(segment),10,11);
                var original=Pixels(camera,target);Assert.Greater(System.Array.FindAll(original,p=>p.r+p.g+p.b>0).Length,30,"Actual source must be visible.");
                foreach(var renderer in sourceParent.GetComponentsInChildren<Renderer>(true))renderer.forceRenderingOff=true;
                using(var view=new RecordedSceneryView(copyParent.transform))
                {
                    view.Draw(decoded.Frames[0]);foreach(var node in copyParent.GetComponentsInChildren<Transform>(true))node.gameObject.layer=30;view.Visible(true);
                var replay=Pixels(camera,target);float delta=Difference(original,replay);Debug.Log("[RecordedSceneryPixels] droneAndMark="+delta);Assert.Less(delta,.08f);
                    Assert.AreEqual(0,copyParent.GetComponentsInChildren<MonoBehaviour>(true).Length,"Copies must have rendering data only.");
                    view.Visible(false);Assert.Greater(Difference(replay,Pixels(camera,target)),.1f,"Visible replay control must matter.");
                }
            }
            finally{camera.targetTexture=null;target.Release();Object.Destroy(target);Object.Destroy(camera.gameObject);Object.Destroy(sourceParent);Object.Destroy(copyParent);}
        }
        [Test]public void RenderFrameTimelineKeepsShortVisibilityEdgesAndRejectsInvalidData()
        {
            LocalReplaySceneryRoot Root(bool active)=>new LocalReplaySceneryRoot{Kind=LocalReplaySceneryKind.Animal,Id="test",OwnerPath="0",OwnerName="test",Model="test",Art=new string('a',64),
                Paths=new[]{""},Positions=new[]{Vector3.zero},Rotations=new[]{Quaternion.identity},Scales=new[]{Vector3.one},Active=new[]{active}};
            var segment=new LocalReplayScenerySegment();
            segment.Frames.Add(new LocalReplaySceneryFrame{Time=10,Roots=new System.Collections.Generic.List<LocalReplaySceneryRoot>{Root(false)}});
            segment.Frames.Add(new LocalReplaySceneryFrame{Time=10.012f,Roots=new System.Collections.Generic.List<LocalReplaySceneryRoot>{Root(true)}});
            segment.Frames.Add(new LocalReplaySceneryFrame{Time=10.027f,Roots=new System.Collections.Generic.List<LocalReplaySceneryRoot>{Root(false)}});
            var bytes=LocalReplaySceneryCodec.Encode(segment);var saved=LocalReplaySceneryCodec.Decode(bytes,10,11);
            Assert.IsFalse(LocalReplaySceneryCodec.At(saved,10.01f).Roots[0].Active[0]);Assert.IsTrue(LocalReplaySceneryCodec.At(saved,10.015f).Roots[0].Active[0]);Assert.IsFalse(LocalReplaySceneryCodec.At(saved,10.03f).Roots[0].Active[0]);
            segment.Version=2;Assert.Catch(()=>LocalReplaySceneryCodec.Encode(segment));segment.Version=1;
            segment.Frames[1].Roots[0].Kind=(LocalReplaySceneryKind)9;Assert.Catch(()=>LocalReplaySceneryCodec.Encode(segment));segment.Frames[1].Roots[0].Kind=LocalReplaySceneryKind.Animal;
            segment.Frames[1].Roots[0].Generation=17;var replaced=LocalReplaySceneryCodec.Decode(LocalReplaySceneryCodec.Encode(segment),10,11);
            Assert.AreEqual(17,replaced.Frames[1].Roots[0].Generation);Assert.AreEqual(0,replaced.Frames[0].Roots[0].Generation,"Absence in the original saved schema remains a distinct legacy generation.");
            segment.Frames[1].Roots[0].Positions[0]=new Vector3(float.NaN,0,0);Assert.Catch(()=>LocalReplaySceneryCodec.Encode(segment));
            var cut=new byte[bytes.Length/2];System.Array.Copy(bytes,cut,cut.Length);Assert.Catch(()=>LocalReplaySceneryCodec.Decode(cut,10,11));
        }
        [UnityTest]public IEnumerator SavedAnimalAndNewLineStayVisibleWhenTheirLiveRootIsInactive()
        {
            yield return Load(SceneFlow.Eskinita);
            var life=Object.FindAnyObjectByType<AmbientLife>();Assert.IsNotNull(life);
            var animal=life.Animals[0];var source=life.transform.Find("Ambient "+animal.Id);Assert.IsNotNull(source);
            var owner=new GameObject("Owned recorded animal");var camera=new GameObject("Owned animal camera").AddComponent<Camera>();camera.CopyFrom(Camera.main);camera.enabled=false;camera.cullingMask=1<<30;
            Vector3 centre=source.position+Vector3.up*.3f;camera.transform.position=centre+new Vector3(2,1.5f,-2);camera.transform.LookAt(centre);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            var target=new RenderTexture(512,512,24,RenderTextureFormat.ARGB32);target.Create();camera.targetTexture=target;Material lineMaterial=null;
            var layers=new System.Collections.Generic.Dictionary<GameObject,int>();
            try
            {
                var before=LocalReplaySceneryState.Capture().Find(r=>r.Kind==LocalReplaySceneryKind.Animal&&r.Id==animal.Id);
                var line=new GameObject("Brief surface arc").AddComponent<LineRenderer>();line.transform.SetParent(source,false);
                lineMaterial=new Material(Shader.Find("Sprites/Default"));line.sharedMaterial=lineMaterial;line.positionCount=5;line.useWorldSpace=true;line.startWidth=.005f;line.endWidth=.003f;line.startColor=line.endColor=new Color(.64f,.61f,.4f,.28f);
                for(int n=0;n<5;n++)line.SetPosition(n,source.position+new Vector3(.2f+n*.05f,.2f+Mathf.Sin(n*Mathf.PI/4)*.07f,.1f));
                foreach(var node in source.GetComponentsInChildren<Transform>(true)){layers[node.gameObject]=node.gameObject.layer;node.gameObject.layer=30;}
                var after=LocalReplaySceneryState.Capture().Find(r=>r.Kind==LocalReplaySceneryKind.Animal&&r.Id==animal.Id);
                var stream=new LocalReplayScenerySegment();stream.Frames.Add(new LocalReplaySceneryFrame{Time=10,Roots=new System.Collections.Generic.List<LocalReplaySceneryRoot>{before}});stream.Frames.Add(new LocalReplaySceneryFrame{Time=10.012f,Roots=new System.Collections.Generic.List<LocalReplaySceneryRoot>{after}});
                var saved=LocalReplaySceneryCodec.Decode(LocalReplaySceneryCodec.Encode(stream),10,11);var original=Pixels(camera,target);
                Assert.Greater(System.Array.FindAll(original,p=>p.r+p.g+p.b>0).Length,30);
                source.gameObject.SetActive(false);
                using(var view=new RecordedSceneryView(owner.transform))
                {
                    view.Draw(saved.Frames[0]);foreach(var node in owner.GetComponentsInChildren<Transform>(true))node.gameObject.layer=30;
                    view.Draw(saved.Frames[1]);foreach(var node in owner.GetComponentsInChildren<Transform>(true))node.gameObject.layer=30;view.Visible(true);
                    float delta=Difference(original,Pixels(camera,target));Debug.Log("[RecordedSceneryPixels] animalAndLine="+delta);Assert.Less(delta,.08f);
                    Assert.IsFalse(source.gameObject.activeSelf,"Playback must never activate the live animal root.");
                    var copiedLine=owner.GetComponentInChildren<LineRenderer>(true);Assert.IsNotNull(copiedLine);Assert.IsTrue(copiedLine.enabled);Assert.AreEqual(5,copiedLine.positionCount);
                    view.Draw(saved.Frames[0]);Assert.IsFalse(copiedLine.enabled,"Rewinding before line birth must hide it.");
                    Assert.AreEqual(0,owner.GetComponentsInChildren<MonoBehaviour>(true).Length);
                }
            }
            finally
            {
                source.gameObject.SetActive(true);foreach(var pair in layers)if(pair.Key!=null)pair.Key.layer=pair.Value;
                if(lineMaterial!=null)Object.Destroy(lineMaterial);camera.targetTexture=null;target.Release();Object.Destroy(target);Object.Destroy(camera.gameObject);Object.Destroy(owner);
            }
        }
        [UnityTest]public IEnumerator SavedAnimalSurvivesSiblingInsertionAndRejectsAmbiguousOrChangedArt()
        {
            GameLaunch.AllBots=true;
            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(SceneFlow.Eskinita);
            yield return null;
            var life=Object.FindObjectsByType<AmbientLife>(FindObjectsSortMode.None).First(l=>l.Animals.Length>0);
            var frame=LocalReplaySceneryState.Capture().Single(r=>r.Kind==LocalReplaySceneryKind.Animal
                &&r.OwnerName==life.name&&r.Id==life.Animals[0].Id);
            var saved=new LocalReplaySceneryFrame{Time=1,Roots=new List<LocalReplaySceneryRoot>{frame}};
            var insertion=new GameObject("Owned runtime sibling insertion");
            var replay=new GameObject("Owned shifted scenery replay");
            GameObject duplicate=null;
            try
            {
                insertion.transform.SetParent(life.transform.parent,false);
                insertion.transform.SetSiblingIndex(life.transform.GetSiblingIndex());
                Assert.AreNotEqual(frame.OwnerPath,LocalReplaySceneryState.Capture().Single(r=>r.Kind==LocalReplaySceneryKind.Animal
                    &&r.OwnerName==life.name&&r.Id==life.Animals[0].Id).OwnerPath);
                using(var view=new RecordedSceneryView(replay.transform))
                {
                    Assert.DoesNotThrow(()=>view.Draw(saved));
                    Assert.Greater(replay.GetComponentsInChildren<Renderer>(true).Length,0);
                    Assert.AreEqual(0,replay.GetComponentsInChildren<AmbientLife>(true).Length);
                }
                duplicate=new GameObject(life.name);duplicate.transform.SetParent(life.transform.parent,false);
                duplicate.AddComponent<AmbientLife>();
                using(var view=new RecordedSceneryView(replay.transform))
                    Assert.Throws<System.InvalidOperationException>(()=>view.Draw(saved),"A name collision must never select an arbitrary owner.");
                Object.DestroyImmediate(duplicate);duplicate=null;
                frame.Art=new string('0',64);
                using(var view=new RecordedSceneryView(replay.transform))
                    Assert.Throws<System.InvalidOperationException>(()=>view.Draw(saved),"A shifted path must not bypass actual art provenance.");
            }
            finally
            {
                if(duplicate!=null)Object.DestroyImmediate(duplicate);
                Object.DestroyImmediate(insertion);Object.DestroyImmediate(replay);
            }
        }
        [UnityTest]public IEnumerator SavedSceneryRenderingErrorPausesWithoutRepeatedExceptions()
        {
            string path=System.Environment.GetEnvironmentVariable("TUMP_SCENERY_INCOMPATIBLE");
            if(string.IsNullOrEmpty(path))Assert.Ignore("Set TUMP_SCENERY_INCOMPATIBLE to the retained incompatible recording.");
            var entry=LocalReplayStore.List(Path.GetDirectoryName(path)).Single(e=>e.Directory==path);
            LogAssert.Expect(LogType.Warning,"[LocalReplay] Recorded scenery art content changed.");
            int warnings=0;
            Application.LogCallback log=(message,stack,type)=>
            {if(type==LogType.Warning&&message=="[LocalReplay] Recorded scenery art content changed.")warnings++;};
            Application.logMessageReceived+=log;
            LocalReplayPlayback viewer=null;
            try
            {
                Assert.IsTrue(LocalReplayPlayback.Open(entry));viewer=Object.FindAnyObjectByType<LocalReplayPlayback>();
                float until=Time.realtimeSinceStartup+25;
                while(viewer.Error==null&&Time.realtimeSinceStartup<until)yield return null;
                Assert.AreEqual("Recorded scenery art content changed.",viewer.Error);
                Assert.IsTrue(viewer.Paused);float stoppedAt=viewer.Position;
                for(int n=0;n<10;n++)yield return null;
                Assert.AreEqual(stoppedAt,viewer.Position);
                Assert.AreEqual(1,warnings,"The failed render must report once and stop drawing.");
            }
            finally{Application.logMessageReceived-=log;if(viewer!=null)Object.DestroyImmediate(viewer.gameObject);}
        }
        [UnityTest]public IEnumerator NaturalCustomMatchSavesEverySceneryRenderFrameAndReopensIt()
        {
            string preference=Path.Combine(ProfilePaths.Root,"replay-folder.txt");byte[] before=File.Exists(preference)?File.ReadAllBytes(preference):null;
            string folder=Path.GetFullPath("Logs/replay-scenery-natural1007/"+System.Guid.NewGuid().ToString("N"));
            try
            {
                Assert.IsTrue(LocalReplayStore.SetFolder(folder,out string error),error);
                var rules=Core.CustomGameRules.Defaults(Core.GameMode.Classic);rules.Rounds=1;rules.RoundSeconds=30;SceneFlow.PinSelectedRules(rules);GameLaunch.AllBots=true;
                yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(SceneFlow.Eskinita);yield return null;
                var observer=new GameObject("Owned actual scenery render observer").AddComponent<Observer>();
                float until=Time.realtimeSinceStartup+70;while(!GameServices.Match.HasCompleted&&Time.realtimeSinceStartup<until)yield return null;
                Assert.IsTrue(GameServices.Match.HasCompleted);long identity=GameServices.Match.PresentationMatchId;
                LocalReplayEntry entry=null;until=Time.realtimeSinceStartup+20;
                while(entry==null&&Time.realtimeSinceStartup<until){entry=LocalReplayStore.List(folder).Find(e=>e.Manifest.MatchId==identity&&e.Manifest.Completed);yield return null;}
                Assert.IsNotNull(entry);Assert.IsTrue(string.IsNullOrEmpty(entry.Manifest.Warning),entry.Manifest.Warning);
                var savedTimes=new HashSet<float>();long bytes=0;int roots=0,poses=0;
                for(int n=0;n<entry.Manifest.Segments.Count;n++)
                {
                    var scenery=LocalReplayStore.ReadScenery(entry,n);Assert.IsNotNull(scenery);
                    bytes+=new FileInfo(Path.Combine(entry.Directory,entry.Manifest.Segments[n].SceneryFile)).Length;
                    foreach(var frame in scenery.Frames){savedTimes.Add(frame.Time);roots+=frame.Roots.Count;foreach(var root in frame.Roots)poses+=root.Paths.Length;}
                }
                float first=entry.Manifest.Segments[0].Start,last=entry.Manifest.Segments.Last().End;
                foreach(float time in observer.Times)if(time>=first&&time<=last)Assert.IsTrue(savedTimes.Contains(time),"Missing actual scenery render time "+time);
                Debug.Log("[NaturalSceneryReplay] observed="+observer.Times.Count+" savedUnique="+savedTimes.Count+" roots="+roots+" poses="+poses+" bytes="+bytes+" segments="+entry.Manifest.Segments.Count);
                Object.Destroy(observer.gameObject);
                Assert.IsTrue(LocalReplayPlayback.Open(entry));var viewer=Object.FindAnyObjectByType<LocalReplayPlayback>();until=Time.realtimeSinceStartup+25;
                while(viewer.Frame==null&&viewer.Error==null&&Time.realtimeSinceStartup<until)yield return null;
                Assert.IsNull(viewer.Error,viewer.Error);Assert.IsNotNull(viewer.Frame);
                viewer.Seek(17);for(int n=0;n<30;n++)yield return null;Assert.IsNull(viewer.Error,viewer.Error);
                viewer.Seek(2);for(int n=0;n<30;n++)yield return null;Assert.IsNull(viewer.Error,viewer.Error);
                Object.Destroy(viewer.gameObject);yield return null;
            }
            finally
            {
                var viewer=Object.FindAnyObjectByType<LocalReplayPlayback>();if(viewer!=null)Object.DestroyImmediate(viewer.gameObject);
                if(before!=null)File.WriteAllBytes(preference,before);else if(File.Exists(preference))File.Delete(preference);
            }
        }
        [UnityTest]public IEnumerator ExistingSavedSceneryReopensWithValidSkinnedBindingsAndSeeks()
        {
            string path=System.Environment.GetEnvironmentVariable("TUMP_SCENERY_EXISTING");if(string.IsNullOrEmpty(path))Assert.Ignore("Set TUMP_SCENERY_EXISTING to the retained actual scenery recording.");
            var entry=LocalReplayStore.List(Path.GetDirectoryName(path)).Single(e=>e.Directory==path);
            Assert.IsTrue(LocalReplayPlayback.Open(entry));var viewer=Object.FindAnyObjectByType<LocalReplayPlayback>();
            try
            {
                float until=Time.realtimeSinceStartup+25;while(viewer.Frame==null&&viewer.Error==null&&Time.realtimeSinceStartup<until)yield return null;
                Assert.IsNull(viewer.Error,viewer.Error);Assert.IsNotNull(viewer.Frame);
                foreach(float time in new[]{17f,2f,entry.Manifest.Duration-.02f})
                {
                    viewer.Seek(time);until=Time.realtimeSinceStartup+15;
                    var field=typeof(LocalReplayPlayback).GetField("_loading",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                    do{yield return null;}while(field.GetValue(viewer)!=null&&viewer.Error==null&&Time.realtimeSinceStartup<until);
                    for(int n=0;n<5;n++)yield return null;
                    Assert.IsNull(viewer.Error,viewer.Error);Assert.AreEqual(time,viewer.Position,.001f);
                }
                Assert.Greater(Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None).Count(r=>r.transform.root.name=="~RecordedWorld"),0);
            }
            finally{if(viewer!=null)Object.DestroyImmediate(viewer.gameObject);}
        }
        [UnityTest]public IEnumerator ActualShowAndRecoveryPopulationHasMeasuredBoundedPayloadAndGenerationIdentity()
        {
            yield return Load(SceneFlow.Arena);var owner=new GameObject("Owned twelve drone capacity population");
            try
            {
                var template=Object.FindAnyObjectByType<ArenaFallRecovery>().DroneTemplate;
                // The show creates its eight models lazily, not at every round
                // start. Use twelve actual authored factory models for the
                // documented eight-show plus four-recovery capacity scenario.
                for(int n=0;n<12;n++)
                {var drone=ArenaDrone.Build(owner.transform,template);drone.Hold(new Vector3(n*2,3,0),ArenaDrone.Act.Across,.5f,2.5f,Vector3.zero,Vector3.zero,.016f,false);}
                var drones=Object.FindObjectsByType<ArenaDrone>(FindObjectsInactive.Include,FindObjectsSortMode.None);
                Assert.GreaterOrEqual(drones.Length,12,"The authored twelve-model capacity scenario must be included.");
                var timer=System.Diagnostics.Stopwatch.StartNew();var roots=LocalReplaySceneryState.Capture();timer.Stop();double cold=timer.Elapsed.TotalMilliseconds;
                timer.Restart();for(int n=0;n<30;n++)roots=LocalReplaySceneryState.Capture();timer.Stop();double capture=timer.Elapsed.TotalMilliseconds/30;
                Assert.GreaterOrEqual(roots.Count(r=>r.Kind==LocalReplaySceneryKind.Drone),12);
                Assert.AreEqual(roots.Count(r=>r.Kind==LocalReplaySceneryKind.Drone),roots.Count(r=>r.Kind==LocalReplaySceneryKind.DroneMark));
                var segment=new LocalReplayScenerySegment();for(int n=0;n<180;n++)segment.Frames.Add(new LocalReplaySceneryFrame{Time=20+n/60f,Roots=roots});
                timer.Restart();var encoded=LocalReplaySceneryCodec.Encode(segment);timer.Stop();double encode=timer.Elapsed.TotalMilliseconds;
                timer.Restart();var decoded=LocalReplaySceneryCodec.Decode(encoded,20,23);timer.Stop();
                Assert.AreEqual(180,decoded.Frames.Count);Assert.AreEqual(roots.Count,decoded.Frames[179].Roots.Count);
                Assert.AreEqual(roots[0].Generation,decoded.Frames[179].Roots[0].Generation);
                Assert.AreEqual(roots[0].Positions[0],decoded.Frames[179].Roots[0].Positions[0]);
                Debug.Log("[SceneryPopulation] drones="+drones.Length+" roots="+roots.Count+" coldCaptureMs="+cold+" warmCaptureMs="+capture+" encodedBytes="+encoded.Length+" encodeMs="+encode+" decodeMs="+timer.Elapsed.TotalMilliseconds);
            }
            finally{Object.Destroy(owner);}
        }
    }
}
