using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    public static class BayanHallFinishAuthor
    {
        private const string Folder="Assets/TumbangPreso/Art/BayanHallFinish",MeshTag="TUMP_BAYAN_HALL_MESH:",SourceTag="TumpBayanHallSource";
        public static void ClearPrevious(string map)
        {
            if(map!="BayanPlaza")return;var root=GameObject.Find(map);if(root==null)return;
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var importer=AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(filter.sharedMesh));
                if(importer==null||!importer.userData.StartsWith(MeshTag))continue;
                filter.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(importer.userData.Substring(MeshTag.Length));
                var renderer=filter.GetComponent<MeshRenderer>();renderer.sharedMaterials=renderer.sharedMaterials.Where(m=>string.IsNullOrEmpty(m.GetTag(SourceTag,false))).ToArray();
            }
        }
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/BayanPlaza.unity");var report=new StringBuilder();FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/bayan-hall-finish");File.WriteAllText("Logs/bayan-hall-finish/author.txt",report.ToString());Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            var map=GameObject.Find("BayanPlaza");var solids=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();int count=0;
            foreach(var filter in map.GetComponentsInChildren<MeshFilter>())
            {
                var original=filter.sharedMesh;if(original==null)continue;string path=AssetDatabase.GetAssetPath(original);var importer=AssetImporter.GetAtPath(path);
                bool repeat=importer!=null&&importer.userData.StartsWith(MeshTag);
                if(repeat){path=importer.userData.Substring(MeshTag.Length);original=AssetDatabase.LoadAssetAtPath<Mesh>(path);}
                var source=MapSurfaceAuthor.SourceMeshForAuthoring(original);
                if(!AssetDatabase.GetAssetPath(source).Contains("env_municipal_hall"))continue;
                count++;var renderer=filter.GetComponent<MeshRenderer>();var slots=renderer.sharedMaterials.ToList();
                if(repeat)slots.RemoveAll(m=>!string.IsNullOrEmpty(m.GetTag(SourceTag,false)));
                int body=slots.FindIndex(m=>MapSurfaceAuthor.SourceMaterialNameForAuthoring(m).EndsWith("municipal_hall_body",StringComparison.OrdinalIgnoreCase));
                if(body<0)throw new InvalidOperationException("Missing original hall body material.");
                var mesh=Object.Instantiate(original);var p=mesh.vertices;var normals=mesh.normals;var uv=mesh.uv;var keep=new List<int>();var selected=new List<int>();
                var triangles=mesh.GetTriangles(body);
                for(int i=0;i<triangles.Length;i+=3)
                {
                    int a=triangles[i],b=triangles[i+1],c=triangles[i+2];var n=Vector3.Cross(p[b]-p[a],p[c]-p[a]);
                    bool wall=Mathf.Abs(n.normalized.y)<.02f&&n.magnitude*.5f>4;
                    (wall?selected:keep).AddRange(new[]{a,b,c});
                }
                if(selected.Count!=24)throw new InvalidOperationException("Expected eight large vertical hall wall triangles, found "+selected.Count/3);
                var chosen=new HashSet<int>(selected);
                if(keep.Any(chosen.Contains))throw new InvalidOperationException("Hall small details share selected wall coordinates.");
                foreach(int index in chosen)
                {
                    var v=p[index];float horizontal=Mathf.Abs(normals[index].x)>.5f?v.z:v.x;
                    uv[index]=new Vector2(horizontal/8,v.y/8);
                }
                mesh.uv=uv;mesh.SetTriangles(keep,body);mesh.subMeshCount=original.subMeshCount+1;mesh.SetTriangles(selected,original.subMeshCount);
                if(!mesh.vertices.SequenceEqual(original.vertices)||!mesh.triangles.OrderBy(v=>v).SequenceEqual(original.triangles.OrderBy(v=>v)))throw new InvalidOperationException("Hall source topology changed.");
                var material=Visual.NearFade.CopySurfaceForAuthoring(slots[body]);var colour=slots[body].color;
                material.color=new Color(colour.r/.86f,colour.g/.86f,colour.b/.86f,colour.a);material.mainTexture=Wash();material.mainTextureScale=Vector2.one;material.mainTextureOffset=Vector2.zero;
                material.SetFloat("_SurfaceKind",0);material.SetFloat("_SurfaceVertexRoles",0);material.SetFloat("_Glossiness",.06f);material.SetOverrideTag(SourceTag,AssetDatabase.GetAssetPath(slots[body]));
                slots.Add(Save(material,Folder+"/PaintedPlaster.mat"));renderer.sharedMaterials=slots.ToArray();
                string output=Folder+"/HallWalls.asset";mesh.name="Bayan hall fitted plaster coordinates";filter.sharedMesh=Save(mesh,output);
                importer=AssetImporter.GetAtPath(output);importer.userData=MeshTag+path;importer.SaveAndReimport();
                report.AppendLine("Hall: eight large wall triangles only; source geometry, small body parts, trim, roof, windows and collision retained. One extra material slot, no new renderer/collider.");
            }
            if(count!=1)throw new InvalidOperationException("Expected one hall body, found "+count);
            if(solids.Count!=map.GetComponentsInChildren<Collider>(true).Length||solids.Any(p=>p.Key==null||p.Key.bounds!=p.Value))throw new InvalidOperationException("Hall finish changed collision.");
        }
        private static Texture2D Wash()
        {
            const int size=256;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float u=x/(float)size,v=y/(float)size;
                float broad=Mathf.Sin(u*Mathf.PI*4+Mathf.Cos(v*Mathf.PI*2)*.6f)*Mathf.Sin(v*Mathf.PI*6+.4f);
                float mineral=Mathf.PerlinNoise(u*31+4,v*31+7)-.5f;
                float baseWear=Mathf.Exp(-v*8*2.2f)*(.035f+.014f*Mathf.Sin(u*Mathf.PI*8));
                float value=.86f+broad*.035f+mineral*.018f-baseWear;
                pixels[y*size+x]=new Color(value,value,value,1);
            }
            texture.SetPixels(pixels);texture.Apply();string path=Folder+"/CivicPlaster.png";File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Bilinear;importer.mipmapEnabled=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        private static T Save<T>(T draft,string path) where T:Object
        {var saved=AssetDatabase.LoadAssetAtPath<T>(path);if(saved==null){AssetDatabase.CreateAsset(draft,path);return draft;}EditorUtility.CopySerialized(draft,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(draft);return saved;}
    }
}
