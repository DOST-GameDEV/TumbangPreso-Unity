using System;
using System.Collections;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class LocalReplaySceneStateTests
    {
        [UnitySetUp]public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown]public IEnumerator After()=>PlayModeWorld.Reset();
        [UnityTest]public IEnumerator RecordedRoadPoseIsSeekableAndScopeRestoresPresentState()
        {
            var root=new GameObject("Replay road");root.SetActive(false);
            var road=root.AddComponent<KantoTraffic>();road.enabled=false;
            var car=GameObject.CreatePrimitive(PrimitiveType.Cube);car.transform.SetParent(root.transform,false);
            car.transform.position=new Vector3(1,0,2);
            road.Drivers=new[]{new KantoTraffic.Driver{Body=car.transform}};
            root.SetActive(true);
            var start=LocalReplaySceneState.Capture(10);
            car.transform.position=new Vector3(5,0,2);car.transform.rotation=Quaternion.Euler(0,30,0);
            var end=LocalReplaySceneState.Capture(11);
            car.transform.position=new Vector3(20,0,20);var present=car.transform.position;var rotation=car.transform.rotation;
            var segment=new LocalReplaySceneSegment();segment.Frames.Add(start);segment.Frames.Add(end);
            using(LocalReplaySceneState.Apply(segment,10.5f,SceneManager.GetActiveScene()))
                Assert.AreEqual(new Vector3(3,0,2),car.transform.position);
            Assert.AreEqual(present,car.transform.position);Assert.AreEqual(rotation,car.transform.rotation);
            try
            {
                using(LocalReplaySceneState.Apply(segment,10,SceneManager.GetActiveScene()))
                {Assert.AreEqual(start.Poses[0].Position,car.transform.position);throw new InvalidOperationException("render failed");}
            }
            catch(InvalidOperationException error){Assert.AreEqual("render failed",error.Message);}
            Assert.AreEqual(present,car.transform.position,"Render failure must not leave traffic in the past.");
            yield return null;
        }
        [UnityTest]public IEnumerator RecordedVisibilityNeverActivatesAnInactiveVehicle()
        {
            var root=new GameObject("Replay inactive road");root.SetActive(false);
            var road=root.AddComponent<KantoTraffic>();road.enabled=false;
            var car=GameObject.CreatePrimitive(PrimitiveType.Cube);car.transform.SetParent(root.transform,false);
            road.Drivers=new[]{new KantoTraffic.Driver{Body=car.transform}};root.SetActive(true);
            var frame=LocalReplaySceneState.Capture(10);frame.Poses[0].Active=false;
            var segment=new LocalReplaySceneSegment();segment.Frames.Add(frame);
            var renderer=car.GetComponent<Renderer>();bool before=renderer.forceRenderingOff;
            using(LocalReplaySceneState.Apply(segment,10,SceneManager.GetActiveScene()))
            {Assert.IsTrue(renderer.forceRenderingOff);Assert.IsTrue(car.activeSelf,"Recorded render visibility does not call GameObject.SetActive.");}
            Assert.AreEqual(before,renderer.forceRenderingOff);yield return null;
        }
        [UnityTest]public IEnumerator RoadWrapIsAnEdgeAndSignalSlotsRestoreTheirActualOverrides()
        {
            var root=new GameObject("Replay wrapped road");root.SetActive(false);var road=root.AddComponent<KantoTraffic>();road.enabled=false;
            var car=GameObject.CreatePrimitive(PrimitiveType.Cube);car.transform.SetParent(root.transform,false);
            var lamp=GameObject.CreatePrimitive(PrimitiveType.Cube);lamp.transform.SetParent(root.transform,false);var renderer=lamp.GetComponent<Renderer>();
            var material=new Material(Shader.Find("Standard"));renderer.sharedMaterial=material;
            road.Drivers=new[]{new KantoTraffic.Driver{Body=car.transform}};road.Signals=new[]{renderer};root.SetActive(true);
            int colour=Shader.PropertyToID("_Color"),emission=Shader.PropertyToID("_EmissionColor");var block=new MaterialPropertyBlock();
            car.transform.position=Vector3.zero;block.SetColor(colour,Color.red);block.SetColor(emission,Color.yellow);renderer.SetPropertyBlock(block,0);
            var first=LocalReplaySceneState.Capture(10);
            car.transform.position=new Vector3(40,0,0);block.SetColor(colour,Color.green);block.SetColor(emission,Color.white);renderer.SetPropertyBlock(block,0);
            var last=LocalReplaySceneState.Capture(11);var segment=new LocalReplaySceneSegment();segment.Frames.Add(first);segment.Frames.Add(last);
            car.transform.position=new Vector3(7,0,0);block.SetColor(colour,Color.blue);renderer.SetPropertyBlock(block,0);
            using(LocalReplaySceneState.Apply(segment,10.5f,SceneManager.GetActiveScene()))
            {Assert.AreEqual(Vector3.zero,car.transform.position);renderer.GetPropertyBlock(block,0);Assert.AreEqual(Color.red,block.GetColor(colour));}
            using(LocalReplaySceneState.Apply(segment,11,SceneManager.GetActiveScene()))
            {Assert.AreEqual(new Vector3(40,0,0),car.transform.position,"At the exact wrap edge use the destination, never interpolate across the court.");renderer.GetPropertyBlock(block,0);Assert.AreEqual(Color.green,block.GetColor(colour));}
            Assert.AreEqual(new Vector3(7,0,0),car.transform.position);renderer.GetPropertyBlock(block,0);Assert.AreEqual(Color.blue,block.GetColor(colour));
            Object.Destroy(material);yield return null;
        }
        [UnityTest]public IEnumerator EveryAutomaticallyRecordedSceneSegmentHasItsCompleteReadableWindow()
        {
            string folder=Environment.GetEnvironmentVariable("TUMP_REPLAY_AUTOSCENE");
            Assert.IsNotEmpty(folder,"Use a retained actual automatic match recording.");
            var entries=LocalReplayStore.List(System.IO.Path.GetDirectoryName(folder));var entry=entries.Find(e=>e.Directory==folder);
            Assert.IsNotNull(entry);Assert.IsTrue(entry.Manifest.Completed);Assert.IsTrue(entry.Manifest.Custom);
            for(int i=0;i<entry.Manifest.Segments.Count;i++)
            {
                var clip=LocalReplayStore.Read(entry,i);var scene=LocalReplayStore.ReadScene(entry,i);Assert.IsNotNull(scene);
                Assert.LessOrEqual(scene.Frames[0].Time,clip.Start+.001f);
                Assert.GreaterOrEqual(scene.Frames[scene.Frames.Count-1].Time,clip.End-.001f);
            }
            Assert.GreaterOrEqual(entry.Manifest.Segments.Count,9);yield return null;
        }
        [UnityTest]public IEnumerator WaterWakeAndCrowdUniformsSeekAndRestoreWithoutRunningMapEvents()
        {
            var water=GameObject.CreatePrimitive(PrimitiveType.Plane);water.name="Recorded wake surface";
            var material=new Material(Shader.Find("TumbangPreso/RoofPoolWater"));water.GetComponent<Renderer>().sharedMaterial=material;
            int swimmers=Shader.PropertyToID("_Swimmers"),wake=Shader.PropertyToID("_WakeStrength"),clock=Shader.PropertyToID("_ArenaCrowdClock");
            var renderer=water.GetComponent<Renderer>();var block=new MaterialPropertyBlock();
            block.SetFloat(wake,.7f);block.SetVectorArray(swimmers,new[]{new Vector4(1,2,3,1),Vector4.zero,Vector4.zero,Vector4.zero});renderer.SetPropertyBlock(block);
            float originalClock=Shader.GetGlobalFloat(clock);var first=LocalReplaySceneState.Capture(10);
            Assert.AreEqual(1,first.Water.Count);Assert.AreEqual(.7f,first.Water[0].WakeStrength);Assert.AreEqual(new Vector4(1,2,3,1),first.Water[0].Swimmers[0]);
            first.HasCrowd=true;first.CrowdClock=5;first.CrowdCheer=.2f;first.CrowdWave=new Vector4(.2f,.1f,.8f,0);
            block.SetFloat(wake,.1f);block.SetVectorArray(swimmers,new[]{new Vector4(9,8,2,1),Vector4.zero,Vector4.zero,Vector4.zero});renderer.SetPropertyBlock(block);
            var last=LocalReplaySceneState.Capture(11);last.HasCrowd=true;last.CrowdClock=6;last.CrowdCheer=.8f;last.CrowdWave=new Vector4(.3f,.1f,.9f,0);
            var segment=new LocalReplaySceneSegment();segment.Frames.Add(first);segment.Frames.Add(last);
            try
            {
                using(LocalReplaySceneState.Apply(segment,10.5f,SceneManager.GetActiveScene()))
                {renderer.GetPropertyBlock(block);Assert.AreEqual(.7f,block.GetFloat(wake));Assert.AreEqual(new Vector4(1,2,3,1),block.GetVectorArray(swimmers)[0]);Assert.AreEqual(5.5f,Shader.GetGlobalFloat(clock));}
                renderer.GetPropertyBlock(block);Assert.AreEqual(.1f,block.GetFloat(wake));Assert.AreEqual(originalClock,Shader.GetGlobalFloat(clock));
                try{using(LocalReplaySceneState.Apply(segment,10,SceneManager.GetActiveScene()))throw new InvalidOperationException("failed water render");}catch(InvalidOperationException){}
                renderer.GetPropertyBlock(block);Assert.AreEqual(.1f,block.GetFloat(wake));Assert.AreEqual(originalClock,Shader.GetGlobalFloat(clock));
            }
            finally{Shader.SetGlobalFloat(clock,originalClock);Object.Destroy(material);Object.Destroy(water);}
            yield return null;
        }
        [UnityTest]public IEnumerator ActualArenaCrowdAndRoofWaterExposeTheirSavedRenderState()
        {
            yield return MapRetrievalProbe.Load(TumbangPreso.UI.SceneFlow.Arena);yield return null;
            var arena=LocalReplaySceneState.Capture(10);Assert.IsTrue(arena.HasCrowd);Assert.Greater(arena.CrowdClock,0);
            float now=Shader.GetGlobalFloat("_ArenaCrowdClock");var segment=new LocalReplaySceneSegment();segment.Frames.Add(arena);
            arena.CrowdClock=now-1;
            using(LocalReplaySceneState.Apply(segment,10,SceneManager.GetActiveScene()))Assert.AreEqual(now-1,Shader.GetGlobalFloat("_ArenaCrowdClock"));
            Assert.AreEqual(now,Shader.GetGlobalFloat("_ArenaCrowdClock"));
            yield return MapRetrievalProbe.Load(TumbangPreso.UI.SceneFlow.SaBubong);yield return null;
            var roof=LocalReplaySceneState.Capture(20);Assert.Greater(roof.Water.Count,0,"The actual authored roof pool must be recorded.");
            foreach(var surface in roof.Water){Assert.AreEqual("TumbangPreso/RoofPoolWater",surface.Shader);Assert.AreEqual(4,surface.Swimmers.Length);}
            var waterSegment=new LocalReplaySceneSegment();waterSegment.Frames.Add(roof);
            using(LocalReplaySceneState.Apply(waterSegment,20,SceneManager.GetActiveScene())){}
        }
        [UnityTest]public IEnumerator ActualKantoRoadCanBeSavedReloadedSeekedAndRestored()
        {
            yield return MapRetrievalProbe.Load(TumbangPreso.UI.SceneFlow.Kanto);
            var road=Object.FindAnyObjectByType<KantoTraffic>();Assert.IsNotNull(road);Assert.Greater(road.Drivers.Length,0);
            var first=LocalReplaySceneState.Capture(10);
            var body=road.Drivers[0].Body;Assert.IsNotNull(body);Vector3 captured=body.position;
            yield return new WaitForSeconds(.15f);
            var last=LocalReplaySceneState.Capture(11);var present=body.position;
            var scene=new LocalReplaySceneSegment();scene.Frames.Add(first);scene.Frames.Add(last);
            var actor=GameServices.Round.PlayerAt(0);var model=actor.GetComponent<TumbangPreso.Visual.CharacterVisual>().Model;
            var history=new MatchPoseHistory.Track(actor,model);history.Record(10);history.Record(11);
            var canModel=MatchReplayArchive.PropModel(GameServices.Round.Lata.gameObject);
            var canHistory=new MatchPoseHistory.Track(actor,canModel);canHistory.Record(10);canHistory.Record(11);
            var clip=new RecordedMatchClip{MatchId=123,Id=1,Round=1,Actor=0,Subject=-1,Mode=actor.Mode,
                Map=TumbangPreso.UI.SceneFlow.Kanto,Start=10,End=11,Contact=10,Reason="SCENE CHECK",
                Objects=new[]{new RecordedObjectTrack{Kind=RecordedObjectKind.Player,Seat=0,Skin=actor.CharacterIndex,
                    Person=Core.Roster.PersonIdAt(actor.Mode,actor.CharacterIndex),VisualKey=MatchReplayArchive.VisualKey(model),Pose=history.Retain(10,11)},
                    new RecordedObjectTrack{Kind=RecordedObjectKind.Can,Seat=-1,Skin=GameServices.Round.Lata.SkinIndex,
                        VisualKey=MatchReplayArchive.VisualKey(canModel),Pose=canHistory.Retain(10,11)}}};
            string folder=System.IO.Path.GetFullPath("Logs/replay-scene-disk1007/"+Guid.NewGuid().ToString("N"));
            var writer=new LocalReplayStore.Writer(folder,new LocalReplayManifest{MatchId=123,Map=clip.Map,Mode=clip.Mode.ToString()});
            Assert.IsTrue(writer.Append(clip,scene));writer.Finish(true);while(!writer.Completion.IsCompleted)yield return null;Assert.IsNull(writer.Error);
            var entry=LocalReplayStore.List(folder)[0];var reloaded=LocalReplayStore.ReadScene(entry,0);Assert.AreEqual(2,reloaded.Frames.Count);
            present=body.position;
            using(LocalReplaySceneState.Apply(reloaded,10,UnityEngine.SceneManagement.SceneManager.GetActiveScene()))
                Assert.AreEqual(captured,body.position,"Actual map road must render its saved past pose.");
            Assert.AreEqual(present,body.position,"Actual road pose must restore immediately.");
            Assert.IsTrue(LocalReplayPlayback.Open(entry));var viewer=Object.FindAnyObjectByType<LocalReplayPlayback>();
            float until=Time.realtimeSinceStartup+15;
            while(viewer.Frame==null&&viewer.Error==null&&Time.realtimeSinceStartup<until)yield return null;
            Assert.IsNull(viewer.Error,viewer.Error);Assert.IsNotNull(viewer.Frame,"Saved traffic must bind after loading a fresh compatible map.");
            viewer.Seek(.5f);for(int i=0;i<10;i++)yield return null;Assert.IsNull(viewer.Error,viewer.Error);
            Object.Destroy(viewer.gameObject);yield return null;
            byte[] bytes=System.IO.File.ReadAllBytes(System.IO.Path.Combine(entry.Directory,entry.Manifest.Segments[0].SceneFile));
            bytes[0]^=1;System.IO.File.WriteAllBytes(System.IO.Path.Combine(entry.Directory,entry.Manifest.Segments[0].SceneFile),bytes);
            Assert.Throws<System.IO.InvalidDataException>(()=>LocalReplayStore.ReadScene(entry,0));
        }
    }
}
