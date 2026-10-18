using System.Collections;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class RecordedShaderClockTests
    {
#if UNITY_EDITOR
        [UnityTest]public IEnumerator EveryUpdatedAuthoredShaderKeepsItsOrdinaryRender()
        {
            string[] paths={"Assets/TumbangPreso/Resources/Shaders/MoonShaft.shader","Assets/TumbangPreso/Resources/Shaders/OmenVeil.shader","Assets/TumbangPreso/Resources/Shaders/RoofPoolWater.shader","Assets/TumbangPreso/Resources/Shaders/SoulGlow.shader","Assets/TumbangPreso/Resources/Shaders/SoulSpill.shader","Assets/TumbangPreso/Resources/Shaders/SpiritGhost.shader","Assets/TumbangPreso/Resources/Shaders/VoodooGhost.shader","Assets/TumbangPreso/Resources/Shaders/VoodooThread.shader","Assets/TumbangPreso/Resources/Shaders/VoodooWisp.shader","Assets/TumbangPreso/Shaders/ArenaHologram.shader","Assets/TumbangPreso/Shaders/LagoonCoveWater.shader","Assets/TumbangPreso/Shaders/SlipperBeam.shader","Assets/TumbangPreso/Shaders/Toon.shader","Assets/TumbangPreso/Shaders/VolcanicRock.shader"};
            var surface=GameObject.CreatePrimitive(PrimitiveType.Plane);surface.transform.position=new Vector3(1000,0,1000);
            var camera=new GameObject("Shader equivalence camera").AddComponent<Camera>();camera.enabled=false;
            camera.transform.position=surface.transform.position+new Vector3(0,4,-4);camera.transform.LookAt(surface.transform.position);
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.farClipPlane=20;
            var target=new RenderTexture(128,128,24,RenderTextureFormat.ARGB32);target.Create();camera.targetTexture=target;
            int clock=Shader.PropertyToID("_TumpRecordedClock");var before=Shader.GetGlobalVector(clock);Shader.SetGlobalVector(clock,Vector4.zero);
            try
            {
                foreach(string path in paths)
                {
                    var candidate=UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(path);Assert.IsNotNull(candidate,path);Assert.IsTrue(candidate.isSupported,path);
                    string source=System.IO.File.ReadAllText(path).Replace("\r\n","\n");
                    source=string.Join("\n",System.Linq.Enumerable.Where(source.Split('\n'),line=>!line.Contains("RecordedShaderTime.cginc"))).Replace("TumpShaderTime()","_Time.y");
                    source=source.Replace("Shader \""+candidate.name+"\"","Shader \"ReplayOriginal/"+candidate.name+"\"");
                    var original=UnityEditor.ShaderUtil.CreateShaderAsset(source,true);Assert.IsNotNull(original,path);
                    var a=new Material(original);var b=new Material(candidate);
                    try
                    {
                        surface.GetComponent<Renderer>().sharedMaterial=a;var live=Pixels(camera,target);
                        surface.GetComponent<Renderer>().sharedMaterial=b;var current=Pixels(camera,target);
                        Assert.IsFalse(UnityEditor.ShaderUtil.ShaderHasError(original),"Original: "+path);
                        Assert.IsFalse(UnityEditor.ShaderUtil.ShaderHasError(candidate),"Candidate: "+path);
                        Assert.Less(Difference(live,current),.08f,"Ordinary render changed: "+path);
                        Debug.Log("[ShaderLiveEquivalence] "+path+" delta="+Difference(live,current));
                    }
                    finally{surface.GetComponent<Renderer>().sharedMaterial=null;Object.Destroy(a);Object.Destroy(b);Object.Destroy(original);}
                }
            }
            finally{Shader.SetGlobalVector(clock,before);camera.targetTexture=null;target.Release();Object.Destroy(target);Object.Destroy(camera.gameObject);Object.Destroy(surface);}
            yield return null;
        }
#endif

        [UnitySetUp]public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown]public IEnumerator After()=>PlayModeWorld.Reset();
        private static Color32[] Pixels(Camera camera,RenderTexture target,string label=null)
        {
            camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;
            var image=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);
            image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
            var pixels=image.GetPixels32();
            string folder=System.Environment.GetEnvironmentVariable("TUMP_SHADER_CAPTURES");
            if(label!=null&&!string.IsNullOrEmpty(folder)){System.IO.Directory.CreateDirectory(folder);System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder,label+".png"),image.EncodeToPNG());}
            RenderTexture.active=previous;Object.Destroy(image);return pixels;
        }
#if UNITY_EDITOR
        private static float LiveShaderSeconds(Camera camera,Renderer surface)
        {
            var shader=UnityEditor.ShaderUtil.CreateShaderAsset("Shader \"ReplayClockProbe\" { SubShader { Pass { CGPROGRAM\n#pragma vertex vert\n#pragma fragment frag\n#include \"UnityCG.cginc\"\nfloat4 vert(float4 vertex:POSITION):SV_POSITION{return UnityObjectToClipPos(vertex);}\nfloat4 frag():SV_Target{return _Time;}\nENDCG } } }",true);
            var material=new Material(shader);var before=surface.sharedMaterial;var oldTarget=camera.targetTexture;
            var target=new RenderTexture(16,16,24,RenderTextureFormat.ARGBFloat);target.Create();var active=RenderTexture.active;
            var image=new Texture2D(16,16,TextureFormat.RGBAFloat,false,true);
            try
            {
                surface.sharedMaterial=material;camera.targetTexture=target;camera.Render();RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,16,16),0,0);image.Apply();var gpu=image.GetPixel(8,8);
                Debug.Log("[ShaderClockGPU] cpu="+Time.time+" level="+Time.timeSinceLevelLoad+" real="+Time.realtimeSinceStartup+" editor="+UnityEditor.EditorApplication.timeSinceStartup+" global="+Shader.GetGlobalVector("_Time")+" gpu="+gpu);
                return gpu.g;
            }
            finally{surface.sharedMaterial=before;camera.targetTexture=oldTarget;RenderTexture.active=active;target.Release();Object.Destroy(target);Object.Destroy(image);Object.Destroy(material);Object.Destroy(shader);}
        }
#endif
        private static float Difference(Color32[] a,Color32[] b)
        {
            long delta=0;for(int i=0;i<a.Length;i++)delta+=System.Math.Abs(a[i].r-b[i].r)+System.Math.Abs(a[i].g-b[i].g)+System.Math.Abs(a[i].b-b[i].b);
            return delta/(a.Length*3f);
        }
        [UnityTest]public IEnumerator AfterTwoMapLoadsTheSavedSceneClockMatchesGpuAndRewindPixels()
        {
            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(TumbangPreso.UI.SceneFlow.Eskinita);yield return null;
            yield return new WaitForSeconds(.3f);
            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(TumbangPreso.UI.SceneFlow.Kanto);yield return new WaitForSeconds(.3f);
            Assert.Greater(Time.time-Time.timeSinceLevelLoad,.1f,"This control must distinguish the application and level clocks.");
            Assert.Greater(Time.timeSinceLevelLoad,.1f,"The regression must test an advancing clock, not only the map-load zero.");
            yield return AuthoredWaterRewindsItsShaderClockAndRestoresLiveTime();
        }
        [UnityTest]public IEnumerator AuthoredWaterRewindsItsShaderClockAndRestoresLiveTime()
        {
            var shader=Shader.Find("TumbangPreso/RoofPoolWater");Assert.IsNotNull(shader);Assert.IsTrue(shader.isSupported);
            var water=GameObject.CreatePrimitive(PrimitiveType.Plane);water.transform.position=new Vector3(1000,0,1000);
            var material=new Material(shader);material.SetFloat("_WakeStrength",0);material.color=new Color(.14f,.48f,.51f,1);
            water.GetComponent<Renderer>().sharedMaterial=material;
            var lamp=new GameObject("Recorded shader test sun").AddComponent<Light>();lamp.type=LightType.Directional;lamp.transform.rotation=Quaternion.Euler(35,20,0);
            var camera=new GameObject("Recorded shader comparison").AddComponent<Camera>();camera.enabled=false;
            camera.transform.position=water.transform.position+new Vector3(0,4,-4);camera.transform.LookAt(water.transform.position);
            camera.backgroundColor=Color.black;camera.clearFlags=CameraClearFlags.SolidColor;camera.farClipPlane=20;
            var target=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32);target.Create();camera.targetTexture=target;
            int clock=Shader.PropertyToID("_TumpRecordedClock");var original=Shader.GetGlobalVector(clock);Shader.SetGlobalVector(clock,Vector4.zero);
            try
            {
                yield return null;
#if UNITY_EDITOR
                float gpu=LiveShaderSeconds(camera,water.GetComponent<Renderer>());
                var captured=LocalReplaySceneState.Capture(Time.time);var segment=new LocalReplaySceneSegment();segment.Frames.Add(captured);
                float recorded=LocalReplaySceneState.ShaderTimeAt(segment,captured.Time);
                Assert.AreEqual(gpu,recorded,.0001f,"The separately recorded scene clock must match the actual GPU clock.");
#else
                float recorded=Time.timeSinceLevelLoad;
#endif
                var live=Pixels(camera,target,"live-original");
                yield return new WaitForSeconds(.7f);var present=Pixels(camera,target,"live-later");float animation=Difference(live,present);
                Assert.Greater(animation,.2f,"The control must actually show water animation.");
                Color32[] replay;using(RecordedShaderClock.At(recorded))replay=Pixels(camera,target,"replay-original-time");
                float rewind=Difference(live,replay);Debug.Log("[RecordedShaderClock] animation="+animation+" rewind="+rewind);
                Assert.Less(rewind,.08f,"A recorded water frame must reproduce the same camera/time pixels.");
                using(RecordedShaderClock.At(recorded+.4f))Assert.Greater(Difference(live,Pixels(camera,target)),.1f,"Forward seeking must animate the authored shader.");
                using(RecordedShaderClock.At(recorded))Assert.Less(Difference(live,Pixels(camera,target)),.08f,"Backward seeking must return to the original frame.");
                using(RecordedShaderClock.At(recorded))
                {
                    var paused=Pixels(camera,target);yield return new WaitForSeconds(.3f);
                    Assert.Less(Difference(paused,Pixels(camera,target)),.08f,"A paused replay shader must hold while live time advances.");
                }
                present=Pixels(camera,target);
                Assert.AreEqual(Vector4.zero,Shader.GetGlobalVector(clock),"Live shader globals restore after render.");
                Assert.Less(Difference(present,Pixels(camera,target)),.08f,"Returning from replay preserves the live rendered frame.");
            }
            finally
            {
                Shader.SetGlobalVector(clock,original);camera.targetTexture=null;target.Release();Object.Destroy(target);
                Object.Destroy(camera.gameObject);Object.Destroy(lamp.gameObject);Object.Destroy(water);Object.Destroy(material);
            }
        }
    }
}
