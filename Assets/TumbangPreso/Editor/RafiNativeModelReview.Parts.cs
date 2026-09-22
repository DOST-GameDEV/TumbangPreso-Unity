using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools
{
    public static partial class RafiNativeModelReview
    {
        private static void RenderPartStudies(string directory)
        {
            foreach(var part in new[]{("head-hair-face",new[]{"head"}),
                ("torso-cloth-accessories",new[]{"torso"}),("left-arm-cord",new[]{"arm-left"}),
                ("right-arm-hand",new[]{"arm-right"}),("legs-sandals",new[]{"leg-left","leg-right"})})
                RenderPart(directory,part.Item1,part.Item2);
        }

        // Isolate the real, posed source triangles by their bone ownership.
        // No source asset or original hero is edited and no geometry is invented.
        private static void RenderPart(string directory,string label,string[] names)
        {
            string output=Path.Combine(directory,"part-"+label+".png");
            if(File.Exists(output))throw new IOException("Use a fresh part-study path: "+output);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var light=new GameObject("Native part key").AddComponent<Light>();light.type=LightType.Directional;
            light.intensity=.85f;light.color=new Color(1,.97f,.90f);light.transform.rotation=Quaternion.Euler(38,-40,0);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight=new Color(.62f,.58f,.52f)*.78f;RenderSettings.fog=false;
            var entry=RosterBook.Load().FindPersonArt("rafi");var source=Object.Instantiate(entry.Model);
            entry.Clips.FirstOrDefault(c=>c!=null&&c.name=="idle")?.SampleAnimation(source,0);
            var root=new GameObject(label);var owned=new List<Mesh>();
            foreach(var skin in source.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var ids=skin.bones.Select((bone,index)=>(bone,index)).Where(p=>p.bone!=null&&names.Contains(p.bone.name)).Select(p=>p.index).ToArray();
                var weights=skin.sharedMesh.boneWeights;if(ids.Length==0)continue;
                bool Selected(int i)=>ids.Contains(weights[i].boneIndex0)&&weights[i].weight0>.99f;
                var indices=skin.sharedMesh.triangles;var chosen=new List<int>();
                for(int i=0;i<indices.Length;i+=3)
                    if(Selected(indices[i])&&Selected(indices[i+1])&&Selected(indices[i+2]))chosen.AddRange(new[]{indices[i],indices[i+1],indices[i+2]});
                if(chosen.Count==0)continue;
                var pose=new Mesh();skin.BakeMesh(pose);
                var vertices=pose.vertices;var normals=pose.normals;var uv=skin.sharedMesh.uv;
                var selected=chosen.Distinct().ToArray();var remap=selected.Select((index,next)=>(index,next)).ToDictionary(p=>p.index,p=>p.next);
                var matrix=source.transform.worldToLocalMatrix*skin.transform.localToWorldMatrix;
                var mesh=new Mesh{name="Native Rafi "+label};
                mesh.vertices=selected.Select(i=>matrix.MultiplyPoint3x4(vertices[i])).ToArray();
                mesh.normals=selected.Select(i=>matrix.MultiplyVector(normals[i]).normalized).ToArray();
                mesh.uv=selected.Select(i=>uv[i]).ToArray();mesh.triangles=chosen.Select(i=>remap[i]).ToArray();mesh.RecalculateBounds();
                Object.DestroyImmediate(pose);owned.Add(mesh);
                var go=new GameObject(skin.name);go.transform.SetParent(root.transform,false);
                go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=skin.sharedMaterial;
            }
            Object.DestroyImmediate(source);
            if(owned.Count==0)throw new InvalidOperationException("No real source geometry for "+label);
            root.transform.localScale=Vector3.one*2.38f;ToonSkin.Apply(root,ToonSkin.PersonOutlineWidth,entry.Palette);
            float[] angles={180,220,270,0};string[] labels={"FRONT","THREE QUARTER","SIDE","BACK"};
            var renderers=root.GetComponentsInChildren<Renderer>();var bounds=new Bounds[4];float span=0;
            for(int i=0;i<4;i++)
            {
                root.transform.rotation=Quaternion.Euler(0,CharacterVisual.PersonModelYaw+angles[i],0);
                bounds[i]=renderers[0].bounds;foreach(var renderer in renderers.Skip(1))bounds[i].Encapsulate(renderer.bounds);
                span=Mathf.Max(span,Mathf.Max(bounds[i].size.x,bounds[i].size.y));
            }
            var camera=new GameObject("Four angle native detail camera").AddComponent<Camera>();camera.enabled=false;
            camera.orthographic=true;camera.orthographicSize=span*.62f;camera.aspect=1;
            camera.nearClipPlane=.1f;camera.farClipPlane=15;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.153f,.161f,.208f);
            var target=new RenderTexture(2400,600,24,RenderTextureFormat.ARGB32){antiAliasing=4};target.Create();camera.targetTexture=target;
            var caption=new GameObject("Detail angle label").AddComponent<TextMesh>();caption.fontSize=36;caption.anchor=TextAnchor.MiddleCenter;
            caption.color=new Color(.86f,.90f,.94f);caption.transform.localScale=Vector3.one*(span*.018f);
            for(int i=0;i<4;i++)
            {
                root.transform.rotation=Quaternion.Euler(0,CharacterVisual.PersonModelYaw+angles[i],0);
                camera.transform.position=bounds[i].center+Vector3.back*6;camera.rect=new Rect(i*.25f,0,.25f,1);
                caption.text=labels[i];caption.transform.position=bounds[i].center+Vector3.down*span*.55f+Vector3.back*.7f;
                camera.Render();
            }
            var old=RenderTexture.active;RenderTexture.active=target;
            var image=new Texture2D(2400,600,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,2400,600),0,0);image.Apply();
            File.WriteAllBytes(output,image.EncodeToPNG());
            File.WriteAllText(Path.ChangeExtension(output,"txt"),"Rafi HERO source only; idle pose; native ToonSkin and fixed2.38 scale. Same camera magnification across all four angles. Isolated bones: "+string.Join(",",names)+"; meshes="+owned.Count+"; framingSpan="+span+"m.\n");
            RenderTexture.active=old;camera.targetTexture=null;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);
            foreach(var mesh in owned)Object.DestroyImmediate(mesh);EditorSceneManager.CloseScene(scene,true);
        }
    }
}
