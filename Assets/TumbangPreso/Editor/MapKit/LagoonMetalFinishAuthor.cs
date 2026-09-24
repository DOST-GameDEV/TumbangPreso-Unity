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
    public static class LagoonMetalFinishAuthor
    {
        private const string Folder="Assets/TumbangPreso/Art/LagoonMetalFinish",SourceTag="TumpLagoonMetalSource",MeshTag="TUMP_LAGOON_METAL_MESH:";
        private static readonly int[] Ids={1,2,7,9,13,15};
        private static readonly float[] Depths={5.3f,4.8f,4.5f,3.8f,4.5f,4.3f};
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene(LagoonBuilder.ScenePath);var report=new StringBuilder();FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/lagoon-metal-finish");File.WriteAllText("Logs/lagoon-metal-finish/author.txt",report.ToString());
            Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            var map=GameObject.Find("Lagoon");var homes=map.transform.Find("Supported homes");
            var solids=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            for(int i=0;i<Ids.Length;i++)
            {
                int id=Ids[i];string kind=id==1||id==15?"RepairShelter":"PatchedMetal";
                var home=homes.Find((id<8?"Neighbourhood home ":"Detached stilt home ")+id+" "+kind);
                if(home==null)throw new InvalidOperationException("Changed metal-roof family: "+id);
                var filter=home.GetComponent<MeshFilter>();var renderer=home.GetComponent<MeshRenderer>();
                var original=filter.sharedMesh;string meshSource=AssetDatabase.GetAssetPath(original);var sourceImporter=AssetImporter.GetAtPath(meshSource);
                if(sourceImporter!=null&&sourceImporter.userData.StartsWith(MeshTag))
                {meshSource=sourceImporter.userData.Substring(MeshTag.Length);original=AssetDatabase.LoadAssetAtPath<Mesh>(meshSource);}
                var slots=renderer.sharedMaterials;int roof=-1,wall=-1;
                for(int m=0;m<slots.Length;m++)
                {
                    string source=slots[m].GetTag(SourceTag,false);if(!string.IsNullOrEmpty(source))slots[m]=AssetDatabase.LoadAssetAtPath<Material>(source);
                    string name=MapSurfaceAuthor.SourceMaterialNameForAuthoring(slots[m]).Replace(" ","");
                    if(name.StartsWith("Weatheredcorrugatedroof",StringComparison.OrdinalIgnoreCase))roof=m;
                    if(id==2&&name.Equals("Householdtimberplanks2",StringComparison.OrdinalIgnoreCase))wall=m;
                }
                if(roof<0||(id==2&&wall<0))throw new InvalidOperationException("Missing metal/paint target material.");
                var mesh=Object.Instantiate(original);mesh.name="Fitted metal roof coordinates "+id;
                var detail=new List<Vector2>();mesh.GetUVs(2,detail);if(detail.Count!=mesh.vertexCount)throw new InvalidOperationException("Missing original metre coordinates.");
                var beforeDetail=detail.ToArray();var uv=mesh.uv;var beforeUv=(Vector2[])uv.Clone();var points=mesh.vertices;var normals=mesh.normals;
                float depth=Depths[i]+.9f,pitch=depth/Mathf.CeilToInt(depth/.26f),scale=.17f/pitch,first=-depth*.5f+pitch*.5f;
                foreach(int v in mesh.GetTriangles(roof).Distinct())
                {
                    var down=Vector3.ProjectOnPlane(Vector3.down,normals[v]).normalized;
                    if(down.sqrMagnitude<.1f)down=Vector3.right;
                    // U crosses the actual Z-spaced ribs; V follows the roof slope.
                    // Compensate V so changing rib pitch does not stretch sheet lengths.
                    detail[v]=new Vector2(points[v].z-first,Vector3.Dot(points[v],down)/scale);
                }
                slots[roof]=Roof(slots[roof],id,scale);
                if(wall>=0)
                {
                    float height=2.15f+(id%3)*.12f;
                    foreach(int v in mesh.GetTriangles(wall).Distinct())
                    {
                        uv[v]=new Vector2(beforeUv[v].x,points[v].y/height);
                        detail[v]=new Vector2(beforeDetail[v].y,beforeDetail[v].x);
                    }
                    slots[wall]=Paint(slots[wall],height);
                }
                for(int m=0;m<mesh.subMeshCount;m++)if(m!=roof&&m!=wall&&mesh.GetTriangles(m).Any(v=>uv[v]!=beforeUv[v]||detail[v]!=beforeDetail[v]))
                    throw new InvalidOperationException("Preserved material coordinates changed.");
                mesh.uv=uv;mesh.SetUVs(2,detail);
                if(!original.vertices.SequenceEqual(mesh.vertices)||!original.triangles.SequenceEqual(mesh.triangles))throw new InvalidOperationException("Metal-family geometry changed.");
                string path=Folder+"/Metal"+id+".asset";var saved=Save(mesh,path);var importer=AssetImporter.GetAtPath(path);
                importer.userData=MeshTag+meshSource;importer.SaveAndReimport();filter.sharedMesh=saved;renderer.sharedMaterials=slots;
                report.AppendLine(id+" "+kind+": shader rib pitch="+pitch.ToString("F4")+"m and slope direction match existing geometry"+(wall>=0?"; lower painted wall band only onhome2":"")+".");
            }
            if(solids.Count!=map.GetComponentsInChildren<Collider>(true).Length||solids.Any(p=>p.Key==null||p.Key.bounds!=p.Value))throw new InvalidOperationException("Metal finish changed collision.");
            report.AppendLine("Six metal-roof homes, one painted household; original positions/topology/other slots/collision unchanged. No new geometry, global shader or gameplay change.");
        }
        private static Material Roof(Material source,int id,float scale)
        {
            var draft=new Material(source){name="Fitted corrugated roof "+id};draft.SetFloat("_SurfaceScale",scale);
            draft.SetOverrideTag(SourceTag,AssetDatabase.GetAssetPath(source));return SaveMaterial(draft,Folder+"/Roof"+id+".mat");
        }
        private static Material Paint(Material source,float height)
        {
            const int w=32,h=128;var texture=new Texture2D(w,h,TextureFormat.RGBA32,false);var pixels=new Color[w*h];
            var blue=new Color(.19f,.36f,.39f,1);var wood=source.color;
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
            {
                float u=x/(float)(w-1),level=y/(float)(h-1)*height,edge=Mathf.Sin(u*17)*.007f;
                float bare=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.94f+edge,.97f+edge,level));
                var color=Color.Lerp(blue,wood,bare);color.a=1;pixels[y*w+x]=color;
            }
            texture.SetPixels(pixels);texture.Apply();string texturePath=Folder+"/House2PaintedBoards.png";
            File.WriteAllBytes(texturePath,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(texturePath);
            var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
            importer.mipmapEnabled=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            var draft=new Material(source){name="House2 painted lower boards",color=Color.white,mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath)};
            draft.mainTextureScale=Vector2.one;draft.mainTextureOffset=Vector2.zero;draft.SetFloat("_DeckSurface",1);
            draft.SetOverrideTag(SourceTag,AssetDatabase.GetAssetPath(source));return SaveMaterial(draft,Folder+"/House2PaintedBoards.mat");
        }
        private static Material SaveMaterial(Material draft,string path)
        {var saved=AssetDatabase.LoadAssetAtPath<Material>(path);if(saved==null){AssetDatabase.CreateAsset(draft,path);return draft;}EditorUtility.CopySerialized(draft,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(draft);return saved;}
        private static Mesh Save(Mesh mesh,string path)
        {var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}EditorUtility.CopySerialized(mesh,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(mesh);return saved;}
    }
}
