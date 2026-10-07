using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class LocalReplaySceneShaderTests
    {
        [UnitySetUp]public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown]public IEnumerator After()=>PlayModeWorld.Reset();
        private static Color32[] Pixels(Camera camera,RenderTexture target,string label)
        {
            using(RecordedShaderClock.At(10))camera.Render();
            var before=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);
            image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();var pixels=image.GetPixels32();
            string folder=Environment.GetEnvironmentVariable("TUMP_SHADER_CAPTURES");
            if(!string.IsNullOrEmpty(folder)){Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,label+".png"),image.EncodeToPNG());}
            RenderTexture.active=before;Object.Destroy(image);return pixels;
        }
        private static float Difference(Color32[] a,Color32[] b)
        {long sum=0;for(int i=0;i<a.Length;i++)sum+=Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b);return sum/(a.Length*3f);}
        private static string Hash(byte[] bytes)
        {using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        private static RecordedMatchClip Clip()
        {
            RecordedPoseTrack.Sample Pose(float time)=>new RecordedPoseTrack.Sample{Time=time,Positions=new[]{Vector3.zero},Rotations=new[]{Quaternion.identity},Scales=new[]{Vector3.one},Active=new[]{true}};
            return new RecordedMatchClip{MatchId=123,Id=1,Round=1,Actor=0,Subject=-1,Start=10,End=11,Contact=10,
                Map=TumbangPreso.UI.SceneFlow.SaBubong,Mode=Core.GameMode.Classic,Reason="RENDER STATE",
                Objects=new[]{new RecordedObjectTrack{Kind=RecordedObjectKind.Player,Seat=0,Skin=0,Person="boy",VisualKey="fixture",
                    Pose=new RecordedPoseTrack(new[]{""},new[]{Pose(10),Pose(11)})}}};
        }
        [UnityTest]public IEnumerator ActualArenaCrowdSavedUniformsRestoreTheOriginalRenderedFrame()
        {
            yield return MapRetrievalProbe.Load(TumbangPreso.UI.SceneFlow.Arena);yield return null;
            var crowd=Object.FindAnyObjectByType<TumbangPreso.Map.ArenaCrowd>();Assert.IsNotNull(crowd);
            var surfaces=crowd.GetComponentsInChildren<Renderer>();Assert.Greater(surfaces.Length,0);
            var surface=surfaces[0];int layer=surface.gameObject.layer;surface.gameObject.layer=31;
            var camera=new GameObject("Recorded crowd comparison").AddComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<31;
            var bounds=surface.bounds;float distance=Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z)*1.5f+2;
            camera.transform.position=bounds.center+new Vector3(0,distance*.3f,-distance);camera.transform.LookAt(bounds.center);
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.farClipPlane=200;
            var target=new RenderTexture(512,512,24,RenderTextureFormat.ARGB32);target.Create();camera.targetTexture=target;
            int clock=Shader.PropertyToID("_ArenaCrowdClock"),cheer=Shader.PropertyToID("_ArenaCrowdExcitement"),groan=Shader.PropertyToID("_ArenaCrowdGroan"),wave=Shader.PropertyToID("_ArenaCrowdWave");
            float oldClock=Shader.GetGlobalFloat(clock),oldCheer=Shader.GetGlobalFloat(cheer),oldGroan=Shader.GetGlobalFloat(groan);var oldWave=Shader.GetGlobalVector(wave);
            try
            {
                Shader.SetGlobalFloat(clock,8);Shader.SetGlobalFloat(cheer,1);Shader.SetGlobalFloat(groan,0);Shader.SetGlobalVector(wave,new Vector4(.35f,.08f,1,0));
                var first=LocalReplaySceneState.Capture(10);var original=Pixels(camera,target,"crowd-live-original");
                Shader.SetGlobalFloat(clock,80);Shader.SetGlobalFloat(cheer,0);Shader.SetGlobalFloat(groan,1);Shader.SetGlobalVector(wave,Vector4.zero);
                var last=LocalReplaySceneState.Capture(11);var current=Pixels(camera,target,"crowd-live-later");
                Assert.Greater(Difference(original,current),.1f,"The actual crowd control must show different animation/response pixels.");
                var segment=new LocalReplaySceneSegment();segment.Frames.Add(first);segment.Frames.Add(last);
                var clip=Clip();clip.Map=TumbangPreso.UI.SceneFlow.Arena;
                string folder=Path.GetFullPath("Logs/replay-crowd-disk1007/"+Guid.NewGuid().ToString("N"));
                var writer=new LocalReplayStore.Writer(folder,new LocalReplayManifest{MatchId=123,Map=clip.Map,Mode="Classic"});
                Assert.IsTrue(writer.Append(clip,segment));writer.Finish(true);while(!writer.Completion.IsCompleted)yield return null;Assert.IsNull(writer.Error);
                var entry=LocalReplayStore.List(folder)[0];var saved=LocalReplayStore.ReadScene(entry,0);Assert.IsTrue(saved.Frames[0].HasCrowd);
                // Restore the later comparison values after the disk worker's yielded frames.
                Shader.SetGlobalFloat(clock,80);Shader.SetGlobalFloat(cheer,0);Shader.SetGlobalFloat(groan,1);Shader.SetGlobalVector(wave,Vector4.zero);
                Color32[] replay;using(LocalReplaySceneState.Apply(saved,10,SceneManager.GetActiveScene()))replay=Pixels(camera,target,"crowd-replay-original");
                float delta=Difference(original,replay);Debug.Log("[ReplayCrowdPixels] control="+Difference(original,current)+" rewind="+delta);Assert.Less(delta,.08f);
                Assert.Less(Difference(current,Pixels(camera,target,"crowd-live-restored")),.08f);
                var timer=System.Diagnostics.Stopwatch.StartNew();for(int n=0;n<100;n++)LocalReplaySceneState.Capture(20+n*.05f);timer.Stop();
                Debug.Log("[ReplayCrowdCaptureCost] averageMs="+timer.Elapsed.TotalMilliseconds/100);
            }
            finally
            {
                Shader.SetGlobalFloat(clock,oldClock);Shader.SetGlobalFloat(cheer,oldCheer);Shader.SetGlobalFloat(groan,oldGroan);Shader.SetGlobalVector(wave,oldWave);
                if(surface!=null)surface.gameObject.layer=layer;camera.targetTexture=null;target.Release();Object.Destroy(target);Object.Destroy(camera.gameObject);
            }
        }
        [UnityTest]public IEnumerator SavedWakeProducesTheOriginalPixelsAndLegacySidecarsStillRead()
        {
            var water=GameObject.CreatePrimitive(PrimitiveType.Plane);water.name="Saved wake pixel surface";water.transform.position=new Vector3(1000,0,1000);
            var material=new Material(Shader.Find("TumbangPreso/RoofPoolWater"));var renderer=water.GetComponent<Renderer>();renderer.sharedMaterial=material;
            var light=new GameObject("Wake pixel sun").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(35,20,0);
            var camera=new GameObject("Wake pixel comparison").AddComponent<Camera>();camera.enabled=false;camera.transform.position=water.transform.position+new Vector3(0,4,-4);camera.transform.LookAt(water.transform.position);
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.farClipPlane=20;
            var target=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32);target.Create();camera.targetTexture=target;
            int wake=Shader.PropertyToID("_WakeStrength"),swimmers=Shader.PropertyToID("_Swimmers");var block=new MaterialPropertyBlock();
            try
            {
                block.SetFloat(wake,1);block.SetVectorArray(swimmers,new[]{new Vector4(1000,1000,3,1),new Vector4(1001,1002,2,1),Vector4.zero,Vector4.zero});renderer.SetPropertyBlock(block);
                yield return null;var first=LocalReplaySceneState.Capture(10);var original=Pixels(camera,target,"wake-live-original");
                block.SetFloat(wake,0);block.SetVectorArray(swimmers,new Vector4[4]);renderer.SetPropertyBlock(block);
                var last=LocalReplaySceneState.Capture(11);var current=Pixels(camera,target,"wake-live-later");
                Assert.Greater(Difference(original,current),.1f,"The live wake control must actually affect rendered pixels.");
                var scene=new LocalReplaySceneSegment();scene.Frames.Add(first);scene.Frames.Add(last);
                string folder=Path.GetFullPath("Logs/replay-uniform-disk1007/"+Guid.NewGuid().ToString("N"));
                var writer=new LocalReplayStore.Writer(folder,new LocalReplayManifest{MatchId=123,Map=TumbangPreso.UI.SceneFlow.SaBubong,Mode="Classic"});
                Assert.IsTrue(writer.Append(Clip(),scene));writer.Finish(true);while(!writer.Completion.IsCompleted)yield return null;Assert.IsNull(writer.Error);
                var entry=LocalReplayStore.List(folder)[0];var saved=LocalReplayStore.ReadScene(entry,0);Assert.AreEqual(4,saved.Frames[0].Water[0].Swimmers.Length);
                Color32[] replay;using(LocalReplaySceneState.Apply(saved,10,SceneManager.GetActiveScene()))replay=Pixels(camera,target,"wake-replay-original");
                float delta=Difference(original,replay);Debug.Log("[ReplayWakePixels] control="+Difference(original,current)+" rewind="+delta);Assert.Less(delta,.08f);
                Assert.Less(Difference(current,Pixels(camera,target,"wake-restored")),.08f,"Replay must restore the actual current rendered wake.");
                string path=Path.Combine(entry.Directory,entry.Manifest.Segments[0].SceneFile);
                byte[] bytes=File.ReadAllBytes(path);
                var malformed=JsonUtility.FromJson<LocalReplaySceneSegment>(System.Text.Encoding.UTF8.GetString(bytes));malformed.Frames[0].Water[0].Swimmers=new Vector4[3];
                byte[] bad=System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(malformed));File.WriteAllBytes(path,bad);entry.Manifest.Segments[0].SceneSha256=Hash(bad);
                Assert.Throws<InvalidDataException>(()=>LocalReplayStore.ReadScene(entry,0),"Even correctly hashed malformed arrays must be rejected.");
                string legacy="{\"Version\":1,\"Frames\":[{\"Time\":10,\"Poses\":[],\"Surfaces\":[]},{\"Time\":11,\"Poses\":[],\"Surfaces\":[]}]}";
                byte[] old=System.Text.Encoding.UTF8.GetBytes(legacy);File.WriteAllBytes(path,old);entry.Manifest.Segments[0].SceneSha256=Hash(old);
                var previous=LocalReplayStore.ReadScene(entry,0);Assert.IsFalse(previous.Frames[0].HasCrowd);
                using(LocalReplaySceneState.Apply(previous,10,SceneManager.GetActiveScene())){}
            }
            finally{camera.targetTexture=null;target.Release();Object.Destroy(target);Object.Destroy(camera.gameObject);Object.Destroy(light.gameObject);Object.Destroy(water);Object.Destroy(material);}
        }
    }
}
