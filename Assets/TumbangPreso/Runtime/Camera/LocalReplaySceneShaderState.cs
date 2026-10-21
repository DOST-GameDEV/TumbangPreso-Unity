using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TumbangPreso.CameraSystem
{
    // Read render uniforms only. Never run crowd events, swimming, or map clocks.
    internal static class LocalReplaySceneShaderState
    {
        private static readonly int Clock=Shader.PropertyToID("_ArenaCrowdClock"),Cheer=Shader.PropertyToID("_ArenaCrowdExcitement"),
            Groan=Shader.PropertyToID("_ArenaCrowdGroan"),Wave=Shader.PropertyToID("_ArenaCrowdWave"),
            Swimmers=Shader.PropertyToID("_Swimmers"),Wake=Shader.PropertyToID("_WakeStrength");
        private static Scene _scene;
        private static string _sceneName;
        private static readonly List<Renderer> _water=new List<Renderer>(2);
        private static bool IsWater(Shader shader)=>shader!=null&&shader.name=="TumbangPreso/RoofPoolWater";
        public static void Capture(LocalReplaySceneFrame frame)
        {
            var scene=SceneManager.GetActiveScene();
            var crowd=UnityEngine.Object.FindAnyObjectByType<Map.ArenaCrowd>();
            frame.HasCrowd=crowd!=null&&crowd.gameObject.scene==scene;
            if(frame.HasCrowd)
            {frame.CrowdClock=Shader.GetGlobalFloat(Clock);frame.CrowdCheer=Shader.GetGlobalFloat(Cheer);frame.CrowdGroan=Shader.GetGlobalFloat(Groan);frame.CrowdWave=Shader.GetGlobalVector(Wave);}
            // Authored water surfaces are stable for a loaded court. Avoid walking
            // the entire renderer hierarchy and allocating material arrays at20Hz.
            if(_scene!=scene||_sceneName!=scene.name||_water.Exists(renderer=>renderer==null))
            {
                _scene=scene;_sceneName=scene.name;_water.Clear();
                foreach(var renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
                    if(renderer.gameObject.scene==scene&&IsWater(renderer.sharedMaterial?.shader))_water.Add(renderer);
            }
            var block=new MaterialPropertyBlock();
            foreach(var renderer in _water)
            {
                if(renderer==null)continue;var material=renderer.sharedMaterial;if(!IsWater(material?.shader))continue;
                block.Clear();renderer.GetPropertyBlock(block);var values=block.GetVectorArray(Swimmers);
                frame.Water.Add(new LocalReplayWaterSurface{Path=LocalReplaySceneState.PathOf(renderer.transform),Name=renderer.name,Shader=material.shader.name,
                    WakeStrength=block.HasFloat(Wake)?block.GetFloat(Wake):material.GetFloat(Wake),
                    Swimmers=values!=null&&values.Length==4?values:new Vector4[4]});
            }
        }
        public static IDisposable Apply(LocalReplaySceneFrame left,LocalReplaySceneFrame right,float blend,Scene scene)=>new Scope(left,right,blend,scene);
        private sealed class Scope:IDisposable
        {
            private readonly bool _crowd;
            private readonly float _clock,_cheer,_groan;
            private readonly Vector4 _wave;
            private readonly List<(Renderer target,MaterialPropertyBlock block)> _water=new List<(Renderer,MaterialPropertyBlock)>();
            public Scope(LocalReplaySceneFrame left,LocalReplaySceneFrame right,float blend,Scene scene)
            {
                _crowd=left.HasCrowd;
                _clock=Shader.GetGlobalFloat(Clock);_cheer=Shader.GetGlobalFloat(Cheer);_groan=Shader.GetGlobalFloat(Groan);_wave=Shader.GetGlobalVector(Wave);
                try
                {
                    if(_crowd)
                    {
                        float t=right.HasCrowd?blend:0;
                        Shader.SetGlobalFloat(Clock,Mathf.Lerp(left.CrowdClock,right.CrowdClock,t));
                        Shader.SetGlobalFloat(Cheer,Mathf.Lerp(left.CrowdCheer,right.CrowdCheer,t));
                        Shader.SetGlobalFloat(Groan,Mathf.Lerp(left.CrowdGroan,right.CrowdGroan,t));
                        // A wave phase wrap is discontinuous, not a reverse wave.
                        Shader.SetGlobalVector(Wave,Mathf.Abs(right.CrowdWave.x-left.CrowdWave.x)>.5f?(blend>=1?right:left).CrowdWave:Vector4.Lerp(left.CrowdWave,right.CrowdWave,t));
                    }
                    var surfaces=(blend>=1?right:left).Water;
                    if(surfaces==null)return; // optional older sidecars
                    foreach(var surface in surfaces)
                    {
                        var renderer=LocalReplaySceneState.Find(surface.Path,scene)?.GetComponent<Renderer>();
                        if(renderer==null||renderer.name!=surface.Name||renderer.sharedMaterial?.shader?.name!=surface.Shader)
                            throw new InvalidOperationException("Recorded water surface changed.");
                        var previous=new MaterialPropertyBlock();renderer.GetPropertyBlock(previous);_water.Add((renderer,previous));
                        var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);block.SetFloat(Wake,surface.WakeStrength);block.SetVectorArray(Swimmers,surface.Swimmers);
                        renderer.SetPropertyBlock(block);
                    }
                }
                catch{Dispose();throw;}
            }
            public void Dispose()
            {
                foreach(var saved in _water)if(saved.target!=null)saved.target.SetPropertyBlock(saved.block);_water.Clear();
                if(_crowd){Shader.SetGlobalFloat(Clock,_clock);Shader.SetGlobalFloat(Cheer,_cheer);Shader.SetGlobalFloat(Groan,_groan);Shader.SetGlobalVector(Wave,_wave);}
            }
        }
    }
}
