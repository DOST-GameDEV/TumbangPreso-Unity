using System;
using System.Collections.Generic;
using System.Linq;
using TumbangPreso.Map;
using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    public enum LocalReplaySceneryKind:byte { Animal,Drone,DroneMark }
    [Serializable]public sealed class LocalReplayScenerySurface
    {
        public string Path;public int Slot=-1;public bool Enabled,HasColour,HasUv;
        public Color Colour;public Vector4 Uv;
    }
    [Serializable]public sealed class LocalReplaySceneryLine
    {
        public string Path;public bool Enabled,WorldSpace;public Vector3[] Points;
        public float StartWidth,EndWidth;public Color StartColour,EndColour;
    }
    [Serializable]public sealed class LocalReplaySceneryRoot
    {
        public string Id,OwnerPath,OwnerName,Model,Art;
        public LocalReplaySceneryKind Kind;
        public int Generation;
        public string[] Paths;public Vector3[] Positions,Scales;public Quaternion[] Rotations;public bool[] Active;
        public List<LocalReplayScenerySurface> Surfaces=new List<LocalReplayScenerySurface>();
        public List<LocalReplaySceneryLine> Lines=new List<LocalReplaySceneryLine>();
    }
    public static class LocalReplaySceneryState
    {
        private static readonly Dictionary<GameObject,string> _art=new Dictionary<GameObject,string>();
        private static readonly Dictionary<GameObject,int> _generations=new Dictionary<GameObject,int>();
        private static int _generation;
        private static int GenerationOf(GameObject root)
        {
            if(!_generations.TryGetValue(root,out int value))_generations[root]=value=++_generation;
            return value;
        }
        public static string ArtKey(GameObject root)
        {
            foreach(var dead in _art.Keys.Where(k=>k==null).ToArray()){_art.Remove(dead);_generations.Remove(dead);}
            if(_art.TryGetValue(root,out var known))return known;
            using var bytes=new System.IO.MemoryStream();using(var writer=new System.IO.BinaryWriter(bytes,System.Text.Encoding.UTF8,true))
            foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if(renderer is LineRenderer)continue;
                writer.Write(Relative(root.transform,renderer.transform));
                var mesh=renderer is SkinnedMeshRenderer skin?skin.sharedMesh:renderer.GetComponent<MeshFilter>()?.sharedMesh;
                if(mesh!=null)
                {
                    writer.Write(mesh.name);writer.Write(mesh.vertexCount);writer.Write(mesh.subMeshCount);
                    using var acquired=Mesh.AcquireReadOnlyMeshData(mesh);var data=acquired[0];
                    for(int stream=0;stream<mesh.vertexBufferCount;stream++)writer.Write(data.GetVertexData<byte>(stream).ToArray());
                    writer.Write(data.GetIndexData<byte>().ToArray());
                    foreach(var bind in mesh.bindposes)for(int n=0;n<16;n++)writer.Write(bind[n]);
                }
                foreach(var material in renderer.sharedMaterials)
                {
                    if(material==null){writer.Write(0u);continue;}writer.Write(material.ComputeCRC());writer.Write(material.shader.name);
                    foreach(string property in material.GetTexturePropertyNames())
                    {writer.Write(property);var texture=material.GetTexture(property);writer.Write(SceneryTextureIdentities.Resolve(texture));}
                }
            }
            using var sha=System.Security.Cryptography.SHA256.Create();known=BitConverter.ToString(sha.ComputeHash(bytes.ToArray())).Replace("-","");_art[root]=known;return known;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]private static void Reset(){_art.Clear();_generations.Clear();_generation=0;}
        internal static string Relative(Transform root,Transform node)
        {
            var parts=new List<string>();for(var current=node;current!=root;current=current.parent)
            {if(current==null)throw new InvalidOperationException("Scenery hierarchy changed.");parts.Add(current.name);}
            parts.Reverse();return string.Join("/",parts);
        }
        public static List<LocalReplaySceneryRoot> Capture()
        {
            var roots=new List<LocalReplaySceneryRoot>();
            foreach(var owner in UnityEngine.Object.FindObjectsByType<AmbientLife>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            foreach(var animal in owner.Animals)
            {
                var root=owner.transform.Find("Ambient "+animal.Id);if(root==null)continue;
                roots.Add(CaptureRoot(root,LocalReplaySceneryKind.Animal,LocalReplaySceneState.PathOf(owner.transform),owner.name,animal.Id,animal.Model?.name));
            }
            foreach(var drone in UnityEngine.Object.FindObjectsByType<ArenaDrone>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                string id=LocalReplaySceneState.PathOf(drone.transform);
                roots.Add(CaptureRoot(drone.transform,LocalReplaySceneryKind.Drone,"","",id,""));
                var mark=drone.RecordedLandingMark;
                if(mark!=null)roots.Add(CaptureRoot(mark,LocalReplaySceneryKind.DroneMark,"","",id,""));
            }
            return roots;
        }
        public static LocalReplaySceneryRoot CaptureRoot(Transform root,LocalReplaySceneryKind kind,string ownerPath,string ownerName,string id,string model)
        {
            // Line geometry is separate; a lazily created line must not alter
            // the body's skeletal topology or require a live graph during replay.
            var bones=root.GetComponentsInChildren<Transform>(true).Where(t=>t.GetComponent<LineRenderer>()==null).ToArray();
            var result=new LocalReplaySceneryRoot{Kind=kind,OwnerPath=ownerPath,OwnerName=ownerName,Id=id,Model=model,Generation=GenerationOf(root.gameObject),Art=ArtKey(root.gameObject),
                Paths=new string[bones.Length],Positions=new Vector3[bones.Length],Rotations=new Quaternion[bones.Length],Scales=new Vector3[bones.Length],Active=new bool[bones.Length]};
            for(int n=0;n<bones.Length;n++)
            {
                var bone=bones[n];result.Paths[n]=Relative(root,bone);
                result.Positions[n]=n==0?bone.position:bone.localPosition;result.Rotations[n]=n==0?bone.rotation:bone.localRotation;
                result.Scales[n]=n==0?bone.lossyScale:bone.localScale;result.Active[n]=n==0?root.gameObject.activeInHierarchy:bone.gameObject.activeSelf;
            }
            var block=new MaterialPropertyBlock();
            foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                string path=Relative(root,renderer.transform);
                if(renderer is LineRenderer line)
                {
                    var points=new Vector3[line.positionCount];line.GetPositions(points);
                    result.Lines.Add(new LocalReplaySceneryLine{Path=path,Enabled=line.enabled&&line.gameObject.activeInHierarchy,WorldSpace=line.useWorldSpace,
                        Points=points,StartWidth=line.startWidth,EndWidth=line.endWidth,StartColour=line.startColor,EndColour=line.endColor});continue;
                }
                for(int slot=-1;slot<renderer.sharedMaterials.Length;slot++)
                {
                    block.Clear();if(slot<0)renderer.GetPropertyBlock(block);else renderer.GetPropertyBlock(block,slot);
                    if(slot>=0&&block.isEmpty)continue;
                    result.Surfaces.Add(new LocalReplayScenerySurface{Path=path,Slot=slot,Enabled=renderer.enabled,
                        HasColour=block.HasColor("_Color"),Colour=block.GetColor("_Color"),HasUv=block.HasVector("_MainTex_ST"),Uv=block.GetVector("_MainTex_ST")});
                }
            }
            return result;
        }
    }
}
