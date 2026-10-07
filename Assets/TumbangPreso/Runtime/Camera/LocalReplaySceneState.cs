using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TumbangPreso.CameraSystem
{
    [Serializable] public sealed class LocalReplayScenePose
    {
        public string Path,Name;
        public Vector3 Position,Scale;
        public Quaternion Rotation;
        public bool Active;
    }
    [Serializable] public sealed class LocalReplaySceneSurface
    {
        public string Path,Name;
        public int Slot;
        public Color Colour,Emission;
        public bool HasColour,HasEmission;
    }
    [Serializable] public sealed class LocalReplaySceneFrame
    {
        public float Time;
        public List<LocalReplayScenePose> Poses=new List<LocalReplayScenePose>();
        public List<LocalReplaySceneSurface> Surfaces=new List<LocalReplaySceneSurface>();
    }
    [Serializable] public sealed class LocalReplaySceneSegment
    {
        public int Version=1;
        public List<LocalReplaySceneFrame> Frames=new List<LocalReplaySceneFrame>();
    }
    // Rendering data only. Never call the road's clock, routes, hits or physics
    // methods. Stable sibling-index paths bind the same compatible map instance.
    public static class LocalReplaySceneState
    {
        private static readonly int ColourId=Shader.PropertyToID("_Color"),EmissionId=Shader.PropertyToID("_EmissionColor");
        private static string PathOf(Transform transform)
        {
            var parts=new List<string>();
            for(var current=transform;current!=null;current=current.parent)parts.Add(current.GetSiblingIndex().ToString());
            parts.Reverse();return string.Join("/",parts);
        }
        private static Transform Find(string path,Scene scene)
        {
            var parts=path.Split('/');var roots=scene.GetRootGameObjects();
            if(parts.Length==0||!int.TryParse(parts[0],out int root)||root<0||root>=roots.Length)return null;
            Transform current=roots[root].transform;
            for(int i=1;i<parts.Length;i++)
            {if(!int.TryParse(parts[i],out int child)||child<0||child>=current.childCount)return null;current=current.GetChild(child);}
            return current;
        }
        public static LocalReplaySceneFrame Capture(float time)
        {
            var frame=new LocalReplaySceneFrame{Time=time};var block=new MaterialPropertyBlock();
            foreach(var traffic in UnityEngine.Object.FindObjectsByType<KantoTraffic>())
            {
                foreach(var driver in traffic.Drivers)
                {
                    var body=driver?.Body;if(body==null)continue;
                    frame.Poses.Add(new LocalReplayScenePose{Path=PathOf(body),Name=body.name,Position=body.position,Rotation=body.rotation,
                        Scale=body.localScale,Active=body.gameObject.activeSelf});
                }
                foreach(var renderer in traffic.Signals)
                {
                    if(renderer==null)continue;var materials=renderer.sharedMaterials;
                    for(int slot=0;slot<materials.Length;slot++)
                    {
                        var material=materials[slot];if(material==null)continue;
                        bool colour=material.HasProperty(ColourId),emission=material.HasProperty(EmissionId);
                        if(!colour&&!emission)continue;
                        block.Clear();renderer.GetPropertyBlock(block,slot);
                        frame.Surfaces.Add(new LocalReplaySceneSurface{Path=PathOf(renderer.transform),Name=renderer.name,Slot=slot,
                            HasColour=colour,HasEmission=emission,
                            Colour=colour?(block.HasColor(ColourId)?block.GetColor(ColourId):material.GetColor(ColourId)):Color.white,
                            Emission=emission?(block.HasColor(EmissionId)?block.GetColor(EmissionId):material.GetColor(EmissionId)):Color.black});
                    }
                }
            }
            return frame;
        }
        public static IDisposable Apply(LocalReplaySceneSegment segment,float time,Scene scene)
        {
            if(segment==null||segment.Frames.Count==0)return null;
            var left=segment.Frames[0];var right=left;
            for(int i=1;i<segment.Frames.Count;i++){right=segment.Frames[i];if(right.Time>=time)break;left=right;}
            return new Restore(left,right,time,scene);
        }
        private sealed class Restore:IDisposable
        {
            private readonly List<(Transform target,Vector3 position,Quaternion rotation,Vector3 scale)> _poses=new List<(Transform,Vector3,Quaternion,Vector3)>();
            private readonly List<(Renderer target,bool hidden)> _visibility=new List<(Renderer,bool)>();
            private readonly List<(Renderer target,int slot,MaterialPropertyBlock block)> _surfaces=new List<(Renderer,int,MaterialPropertyBlock)>();
            public Restore(LocalReplaySceneFrame left,LocalReplaySceneFrame right,float time,Scene scene)
            {
                try
                {
                    float blend=right.Time>left.Time?Mathf.Clamp01((time-left.Time)/(right.Time-left.Time)):0;
                    foreach(var pose in left.Poses)
                    {
                        var target=Find(pose.Path,scene);if(target==null||target.name!=pose.Name)throw new InvalidOperationException("Recorded traffic hierarchy changed.");
                        _poses.Add((target,target.position,target.rotation,target.localScale));
                        var later=right.Poses.Find(p=>p.Path==pose.Path)??pose;
                        // A route wrap is an edge, not a vehicle driving through the court.
                        float t=(later.Position-pose.Position).sqrMagnitude>100&&blend<1?0:blend;
                        target.SetPositionAndRotation(Vector3.Lerp(pose.Position,later.Position,t),Quaternion.Slerp(pose.Rotation,later.Rotation,t));
                        target.localScale=Vector3.Lerp(pose.Scale,later.Scale,t);
                        bool visible=blend>=1?later.Active:pose.Active;
                        foreach(var renderer in target.GetComponentsInChildren<Renderer>(true))
                        {_visibility.Add((renderer,renderer.forceRenderingOff));renderer.forceRenderingOff=!visible;}
                    }
                    foreach(var surface in (blend>=1?right:left).Surfaces)
                    {
                        var target=Find(surface.Path,scene)?.GetComponent<Renderer>();
                        if(target==null||target.name!=surface.Name||surface.Slot<0||surface.Slot>=target.sharedMaterials.Length)throw new InvalidOperationException("Recorded traffic signal changed.");
                        var previous=new MaterialPropertyBlock();target.GetPropertyBlock(previous,surface.Slot);_surfaces.Add((target,surface.Slot,previous));
                        var block=new MaterialPropertyBlock();target.GetPropertyBlock(block,surface.Slot);
                        if(surface.HasColour)block.SetColor(ColourId,surface.Colour);
                        if(surface.HasEmission)block.SetColor(EmissionId,surface.Emission);
                        target.SetPropertyBlock(block,surface.Slot);
                    }
                }
                catch{Dispose();throw;}
            }
            public void Dispose()
            {
                foreach(var state in _surfaces)if(state.target!=null)state.target.SetPropertyBlock(state.block,state.slot);
                _surfaces.Clear();
                foreach(var state in _visibility)if(state.target!=null)state.target.forceRenderingOff=state.hidden;
                _visibility.Clear();
                foreach(var state in _poses)if(state.target!=null)
                {state.target.SetPositionAndRotation(state.position,state.rotation);state.target.localScale=state.scale;}
                _poses.Clear();
            }
        }
    }
}
