using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class ArenaFxRecordedTests
    {
        [UnitySetUp]public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown]public IEnumerator After()=>PlayModeWorld.Reset();
        private static Color32[] Pixels(Camera camera,RenderTexture target)
        {
            camera.Render();var before=RenderTexture.active;RenderTexture.active=target;
            var image=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
            var pixels=image.GetPixels32();RenderTexture.active=before;Object.Destroy(image);return pixels;
        }
        private static float Difference(Color32[] a,Color32[] b)
        {long sum=0;for(int i=0;i<a.Length;i++)sum+=System.Math.Abs(a[i].r-b[i].r)+System.Math.Abs(a[i].g-b[i].g)+System.Math.Abs(a[i].b-b[i].b);return sum/(a.Length*3f);}
        [UnityTest]public IEnumerator OwnedReplayMeshUsesTheActualMaterialAndCameraFacingGeometry()
        {
            yield return MapRetrievalProbe.Load(TumbangPreso.UI.SceneFlow.Arena);
            var fx=TumbangPreso.Map.ArenaFx.Instance;var live=fx.GetComponentInChildren<MeshRenderer>();Assert.IsNotNull(live);
            fx.Flash(new Vector3(0,3,0),3,5,Color.green,1,5);yield return null;
            var quads=fx.CaptureRecordedQuads();Assert.Greater(quads.Length,0);
            int oldLayer=live.gameObject.layer;live.gameObject.layer=30;
            var owner=new GameObject("Recorded effect test owner");var camera=new GameObject("Recorded effect test camera").AddComponent<Camera>();
            camera.CopyFrom(TumbangPreso.Map.ArenaFx.View);camera.transform.SetPositionAndRotation(TumbangPreso.Map.ArenaFx.View.transform.position,TumbangPreso.Map.ArenaFx.View.transform.rotation);camera.enabled=false;camera.cullingMask=1<<30;
            var target=new RenderTexture(512,512,24,RenderTextureFormat.ARGB32);target.Create();camera.targetTexture=target;
            using(var view=new TumbangPreso.CameraSystem.RecordedArenaEffects(owner.transform,fx.RecordedMaterial))
            try
            {
                var original=Pixels(camera,target);Assert.Greater(System.Array.FindAll(original,pixel=>pixel.r+pixel.g+pixel.b>0).Length,10,"The actual effect control must be visible.");live.forceRenderingOff=true;view.Draw(quads,camera);
                var copy=owner.GetComponentInChildren<MeshRenderer>();copy.gameObject.layer=30;
                var replay=Pixels(camera,target);float delta=Difference(original,replay);Debug.Log("[RecordedArenaFxPixels] sameCamera="+delta);Assert.Less(delta,.08f);
                var q=quads[0];q.Facing=TumbangPreso.Map.ArenaFx.RecordedFacing.Billboard;q.Right=Vector3.right;q.Up=Vector3.up;q.Eye=new Vector3(0,0,-10);
                q.A=new Vector3(-1,-1,0);q.B=new Vector3(1,-1,0);q.C=new Vector3(1,1,0);q.D=new Vector3(-1,1,0);
                camera.transform.SetPositionAndRotation(new Vector3(10,0,0),Quaternion.Euler(0,-90,0));
                TumbangPreso.CameraSystem.RecordedArenaEffects.Corners(q,camera.transform,out var a,out var b,out var c,out var d);
                Assert.Greater(System.Math.Abs(Vector3.Dot(Vector3.Cross(b-a,d-a).normalized,camera.transform.forward)),.999f);
                Assert.AreEqual(2,Vector3.Distance(a,b),.0001f);Assert.AreEqual(2,Vector3.Distance(a,d),.0001f);
            }
            finally{live.gameObject.layer=oldLayer;live.forceRenderingOff=false;camera.targetTexture=null;target.Release();Object.Destroy(target);Object.Destroy(camera.gameObject);Object.Destroy(owner);}
        }
        [UnityTest]public IEnumerator NaturalArenaMatchWritesRenderFrameEffectsAndActualViewerSeeksThem()
        {
            var rules=TumbangPreso.UI.SceneFlow.SelectedRules.Clone();bool pinned=TumbangPreso.UI.SceneFlow.RulesPinned,bots=GameLaunch.AllBots;
            string preference=System.IO.Path.Combine(ProfilePaths.Root,"replay-folder.txt");byte[] old=System.IO.File.Exists(preference)?System.IO.File.ReadAllBytes(preference):null;
            string folder=System.IO.Path.GetFullPath("Logs/replay-arena-full1007/"+System.Guid.NewGuid().ToString("N"));
            try
            {
                Assert.IsTrue(TumbangPreso.CameraSystem.LocalReplayStore.SetFolder(folder,out string error),error);
                var custom=Core.CustomGameRules.Defaults(Core.GameMode.Classic);custom.Rounds=1;custom.RoundSeconds=30;
                TumbangPreso.UI.SceneFlow.PinSelectedRules(custom);GameLaunch.AllBots=true;
                yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(TumbangPreso.UI.SceneFlow.Arena);yield return null;
                int observed=0;var liveFx=TumbangPreso.Map.ArenaFx.Instance;System.Action<float> counted=time=>{if(GameServices.Round?.RoundActive==true)observed++;};liveFx.FrameRendered+=counted;
                float until=Time.realtimeSinceStartup+60;while(!GameServices.Match.HasCompleted&&Time.realtimeSinceStartup<until)yield return null;liveFx.FrameRendered-=counted;
                Assert.IsTrue(GameServices.Match.HasCompleted);long identity=GameServices.Match.PresentationMatchId;
                TumbangPreso.CameraSystem.LocalReplayEntry entry=null;until=Time.realtimeSinceStartup+8;
                while(entry==null&&Time.realtimeSinceStartup<until){entry=TumbangPreso.CameraSystem.LocalReplayStore.List(folder).Find(e=>e.Manifest.MatchId==identity&&e.Manifest.Completed);yield return null;}
                Assert.IsNotNull(entry);int frames=0,quads=0;
                for(int n=0;n<entry.Manifest.Segments.Count;n++)
                {
                    var effects=TumbangPreso.CameraSystem.LocalReplayStore.ReadEffects(entry,n);Assert.IsNotNull(effects,"Each actual Arena gameplay segment needs its effect timeline.");frames+=effects.Frames.Count;
                    foreach(var frame in effects.Frames)quads+=frame.Quads.Length;
                }
                Assert.Greater(observed,0);Assert.GreaterOrEqual(frames,observed-3,"Actual render frames must survive regardless of this machine's frame rate.");Assert.Greater(quads,0,"Natural Arena effects must reach disk.");
                Assert.IsTrue(TumbangPreso.CameraSystem.LocalReplayPlayback.Open(entry));var viewer=Object.FindAnyObjectByType<TumbangPreso.CameraSystem.LocalReplayPlayback>();until=Time.realtimeSinceStartup+20;
                while(viewer.Frame==null&&viewer.Error==null&&Time.realtimeSinceStartup<until)yield return null;Assert.IsNull(viewer.Error,viewer.Error);Assert.IsNotNull(viewer.Frame);
                viewer.Seek(17);for(int n=0;n<20;n++)yield return null;Assert.IsNull(viewer.Error,viewer.Error);
                viewer.Seek(2);for(int n=0;n<20;n++)yield return null;Assert.IsNull(viewer.Error,viewer.Error);
                Object.Destroy(viewer.gameObject);yield return null;Debug.Log("[NaturalArenaReplayEffects] frames="+frames+" totalQuads="+quads+" segments="+entry.Manifest.Segments.Count);
            }
            finally
            {
                var active=Object.FindAnyObjectByType<TumbangPreso.CameraSystem.LocalReplayPlayback>();if(active!=null)Object.DestroyImmediate(active.gameObject);
                GameLaunch.AllBots=bots;TumbangPreso.UI.SceneFlow.AdoptRemoteRules(rules);if(pinned)TumbangPreso.UI.SceneFlow.PinSelectedRules(rules);else TumbangPreso.UI.SceneFlow.UnpinSelectedRules();
                if(old!=null)System.IO.File.WriteAllBytes(preference,old);else if(System.IO.File.Exists(preference))System.IO.File.Delete(preference);
            }
        }
        [Test]public void FullPoolThreeSecondStreamIsLosslessAndMeasured()
        {
            const int frames=180,count=TumbangPreso.Map.ArenaFx.MaxParticles+TumbangPreso.Map.ArenaFx.MaxImmediate;
            var source=new TumbangPreso.CameraSystem.LocalReplayFxSegment();
            for(int f=0;f<frames;f++)
            {
                var quads=new TumbangPreso.Map.ArenaFx.RecordedQuad[count];
                for(int q=0;q<count;q++)
                {
                    float x=q*.1f+f*.017f;
                    quads[q]=new TumbangPreso.Map.ArenaFx.RecordedQuad{A=new Vector3(x,2,3),B=new Vector3(x+1,2,3),C=new Vector3(x+1,3,3),D=new Vector3(x,3,3),
                        Eye=new Vector3(0,2,-10),Right=Vector3.right,Up=Vector3.up,Cell=(byte)(q%16),Colour=0xff12aa77,Facing=TumbangPreso.Map.ArenaFx.RecordedFacing.Billboard};
                }
                source.Frames.Add(new TumbangPreso.CameraSystem.LocalReplayFxFrame{Time=10+f/60f,Quads=quads});
            }
            var timer=System.Diagnostics.Stopwatch.StartNew();var encoded=TumbangPreso.CameraSystem.LocalReplayEffectsCodec.Encode(source);timer.Stop();double write=timer.Elapsed.TotalMilliseconds;
            timer.Restart();var decoded=TumbangPreso.CameraSystem.LocalReplayEffectsCodec.Decode(encoded,10,13);timer.Stop();
            Assert.AreEqual(frames,decoded.Frames.Count);Assert.AreEqual(count,decoded.Frames[179].Quads.Length);
            for(int f=0;f<frames;f++)for(int q=0;q<count;q++)
            {Assert.AreEqual(source.Frames[f].Quads[q].A,decoded.Frames[f].Quads[q].A);Assert.AreEqual(source.Frames[f].Quads[q].Colour,decoded.Frames[f].Quads[q].Colour);}
            Debug.Log("[FullPoolFxCodec] frames="+frames+" quadsPerFrame="+count+" bytes="+encoded.Length+" encodeMs="+write+" decodeMs="+timer.Elapsed.TotalMilliseconds);
        }
        [Test]public void CompressedTimelineKeepsSingleFrameFlashesAndRejectsDamage()
        {
            var q=new TumbangPreso.Map.ArenaFx.RecordedQuad{A=Vector3.zero,B=Vector3.right,C=Vector3.one,D=Vector3.up,
                Facing=TumbangPreso.Map.ArenaFx.RecordedFacing.Billboard,Cell=7,Colour=0xff123456,Eye=Vector3.back*10,Right=Vector3.right,Up=Vector3.up};
            var source=new TumbangPreso.CameraSystem.LocalReplayFxSegment();
            source.Frames.Add(new TumbangPreso.CameraSystem.LocalReplayFxFrame{Time=10,Quads=System.Array.Empty<TumbangPreso.Map.ArenaFx.RecordedQuad>()});
            source.Frames.Add(new TumbangPreso.CameraSystem.LocalReplayFxFrame{Time=10.012f,Quads=new[]{q}});
            source.Frames.Add(new TumbangPreso.CameraSystem.LocalReplayFxFrame{Time=10.027f,Quads=System.Array.Empty<TumbangPreso.Map.ArenaFx.RecordedQuad>()});
            var encoded=TumbangPreso.CameraSystem.LocalReplayEffectsCodec.Encode(source);
            var saved=TumbangPreso.CameraSystem.LocalReplayEffectsCodec.Decode(encoded,10,11);
            Assert.AreEqual(0,TumbangPreso.CameraSystem.LocalReplayEffectsCodec.At(saved,10.01f).Length);
            Assert.AreEqual(1,TumbangPreso.CameraSystem.LocalReplayEffectsCodec.At(saved,10.015f).Length,"A flash shorter than the pose sample interval must survive.");
            Assert.AreEqual(0,TumbangPreso.CameraSystem.LocalReplayEffectsCodec.At(saved,10.03f).Length);
            Assert.AreEqual(q.Colour,saved.Frames[1].Quads[0].Colour);Assert.AreEqual(q.C,saved.Frames[1].Quads[0].C);
            var truncated=new byte[encoded.Length/2];System.Array.Copy(encoded,truncated,truncated.Length);
            Assert.Catch(()=>TumbangPreso.CameraSystem.LocalReplayEffectsCodec.Decode(truncated,10,11));
        }
        [UnityTest]public IEnumerator SnapshotContainsTheActualPoolGeometryWithoutSteppingIt()
        {
            yield return MapRetrievalProbe.Load(TumbangPreso.UI.SceneFlow.Arena);
            var fx=TumbangPreso.Map.ArenaFx.Instance;Assert.IsNotNull(fx);
            fx.Emit(TumbangPreso.Map.ArenaFx.Cell.Star5,TumbangPreso.Map.ArenaFx.Mode.Billboard,new Vector3(0,4,0),Vector3.zero,Color.green,1,5,2,3);
            fx.DrawBeam(Vector3.zero,Vector3.up*5,1,2,Color.red,1);
            fx.DrawFlat(TumbangPreso.Map.ArenaFx.Cell.Band,Vector3.zero,3,4,30,Color.blue,1);
            yield return null;
            var first=fx.CaptureRecordedQuads();var second=fx.CaptureRecordedQuads();Assert.Greater(first.Length,0);Assert.AreEqual(first.Length,second.Length);
            for(int i=0;i<first.Length;i++)
            {Assert.AreEqual(first[i].A,second[i].A);Assert.AreEqual(first[i].D,second[i].D);Assert.AreEqual(first[i].Colour,second[i].Colour);Assert.Less(first[i].Cell,16);}
            Assert.IsNotNull(fx.RecordedMaterial);Assert.AreEqual("TumbangPreso/ArenaGlow",fx.RecordedMaterial.shader.name);
            var renderer=fx.GetComponentInChildren<MeshRenderer>();Assert.IsNotNull(renderer);
            var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices;
            Assert.AreEqual(vertices[0],first[0].A);Assert.AreEqual(vertices[3],first[0].D);
        }
    }
}
