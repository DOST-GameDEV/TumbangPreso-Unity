using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>Main condo exterior finish, below the protected recreational roof.</summary>
    public static class RooftopCondoFinishAuthor
    {
        private const string Folder="Assets/TumbangPreso/Art/SaBubong/CondoFinish",RootName="Condo exterior finish";
        private const string SourceTag="TumpCondoBodySource",MeshTag="TUMP_CONDO_MESH_SOURCE:";
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene(SaBubongBuilder.ScenePath);var report=new StringBuilder();FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/rooftop-condo-finish");File.WriteAllText("Logs/rooftop-condo-finish/author.txt",report.ToString());
            Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            var map=GameObject.Find("SaBubong");var body=map.transform.Find("Dressing/Condo building");
            var solids=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            var old=body.Find(RootName);if(old!=null)Object.DestroyImmediate(old.gameObject);
            var filter=body.GetComponent<MeshFilter>();var renderer=body.GetComponent<MeshRenderer>();
            var original=filter.sharedMesh;string meshSource=AssetDatabase.GetAssetPath(original);
            var input=AssetImporter.GetAtPath(meshSource);
            if(input!=null&&input.userData.StartsWith(MeshTag))
            {meshSource=input.userData.Substring(MeshTag.Length);original=AssetDatabase.LoadAssetAtPath<Mesh>(meshSource);}
            var metres=new List<Vector2>();original.GetUVs(2,metres);
            if(metres.Count!=original.vertexCount)throw new InvalidOperationException("Condo surface lacks its authored metre coordinates.");
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var materialSlots=renderer.sharedMaterials;int wall=-1;
            for(int i=0;i<materialSlots.Length;i++)
            {
                string source=materialSlots[i].GetTag(SourceTag,false);
                if(!string.IsNullOrEmpty(source))materialSlots[i]=AssetDatabase.LoadAssetAtPath<Material>(source);
                if(MapSurfaceAuthor.SourceMaterialNameForAuthoring(materialSlots[i]).Replace(" ","").Equals("Weatheredparapet",StringComparison.OrdinalIgnoreCase))wall=i;
            }
            if(wall<0)throw new InvalidOperationException("Expected the retained condo body material, not the pool's separate renderers.");
            materialSlots[wall]=Wall(materialSlots[wall]);renderer.sharedMaterials=materialSlots;
            var derived=Object.Instantiate(original);derived.name="Condo exterior metre texture";derived.uv=metres.Select(p=>p/4).ToArray();
            if(!original.vertices.SequenceEqual(derived.vertices)||!original.triangles.SequenceEqual(derived.triangles))throw new InvalidOperationException("Condo topology changed.");
            var saved=Save(derived,Folder+"/CondoExterior.asset");var importer=AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(saved));
            importer.userData=MeshTag+meshSource;importer.SaveAndReimport();filter.sharedMesh=saved;
            var g=new RooftopNeighborsAuthor.Geometry();
            for(int storey=0;storey<7;storey++)
            {
                float floor=-2.4f-storey*3.35f;
                for(int bay=0;bay<5;bay++)foreach(float side in new[]{-1f,1f})
                {
                    float x=(bay-2)*4.9f,y=floor-1.55f;
                    foreach(float edge in new[]{-1f,1f})g.Box(new Vector3(x+edge*1.43f,y,side*21.88f),new Vector3(.12f,1.78f,.10f),3);
                    g.Box(new Vector3(x,y-.93f,side*21.97f),new Vector3(3.05f,.20f,.25f),3);
                    g.Box(new Vector3(x,y+.93f,side*21.94f),new Vector3(3.05f,.16f,.20f),3);
                    g.Box(new Vector3(x,y,side*21.865f),new Vector3(.10f,1.64f,.05f),5,8);
                }
                // Existing side sills are preserved. Only the wide casement's centre division is added.
                for(int bay=0;bay<7;bay++)foreach(float side in new[]{-1f,1f})
                    g.Box(new Vector3(side*18.915f,floor-1.65f,-14.4f+bay*4.8f),new Vector3(.05f,1.55f,.10f),5,8);
            }
            foreach(float x in new[]{-17.8f,17.8f})foreach(float side in new[]{-1f,1f})
            {
                g.Cylinder(new Vector3(x,-26.049f,side*21.90f),.11f,25.62f,9,8);
                g.Box(new Vector3(x,-.45f,side*21.70f),new Vector3(.24f,.16f,.52f),9);
                for(float y=-1.6f;y>-26;y-=3.35f)g.Box(new Vector3(x,y,side*21.8f),new Vector3(.36f,.12f,.34f),9);
            }
            var detail=g.Mesh("Condo fitted windows and downpipes");
            if(detail.bounds.max.y>-.30f||detail.bounds.min.y<-26.05f||detail.bounds.min.x<-19.5f||detail.bounds.max.x>19.5f||detail.bounds.min.z<-22.2f||detail.bounds.max.z>22.2f)
                throw new InvalidOperationException("Condo finish exceeds its exterior envelope or enters the playable deck.");
            var root=new GameObject(RootName);root.transform.SetParent(body,false);root.isStatic=true;
            root.AddComponent<MeshFilter>().sharedMesh=Save(detail,Folder+"/ExteriorDetails.asset");
            var r=root.AddComponent<MeshRenderer>();r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/TumbangPreso/Art/SaBubong/Neighbors/RoofNeighbors.mat");r.shadowCastingMode=ShadowCastingMode.Off;
            AirborneByDesign.Attach(root,"Fitted exterior window members and supported rainwater runs below the recreational deck.");
            if(solids.Count!=map.GetComponentsInChildren<Collider>(true).Length||solids.Any(p=>p.Key==null||p.Key.bounds!=p.Value))throw new InvalidOperationException("Condo finish changed collision.");
            report.AppendLine("Main condo: locally textured painted wall slot only; original floor-band/glass slots, pool and source materials retained. Original mesh positions/topology unchanged.70end windows fitted,98side casement divisions,four supported exterior downpipes. New finish remains below deck, no new collision/shadow caster.");
        }
        private static Material Wall(Material source)
        {
            const int size=128;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float u=x/(float)(size-1),v=y/(float)(size-1);
                float broad=RooftopApartmentFinishAuthor.Noise(u,v,5,211),brush=RooftopApartmentFinishAuthor.Noise(u*2,v,17,293);
                float value=.85f+(Mathf.SmoothStep(0,1,broad)-.5f)*.20f+(brush-.5f)*.055f;
                pixels[y*size+x]=new Color(value,value,value,1);
            }
            texture.SetPixels(pixels);texture.Apply();string texturePath=Folder+"/PaintedMineral.png";
            File.WriteAllBytes(texturePath,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(texturePath);
            var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Bilinear;
            importer.mipmapEnabled=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            var draft=new Material(source){name="Condo painted mineral",mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath)};
            var color=source.color;draft.color=new Color(color.r/.85f,color.g/.85f,color.b/.85f,color.a);
            draft.mainTextureScale=Vector2.one;draft.mainTextureOffset=Vector2.zero;draft.SetFloat("_SurfaceKind",1);draft.SetFloat("_SurfaceHasTexture",1);
            draft.SetOverrideTag(SourceTag,AssetDatabase.GetAssetPath(source));string path=Folder+"/PaintedMineral.mat";var saved=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(saved==null){AssetDatabase.CreateAsset(draft,path);saved=draft;}else{EditorUtility.CopySerialized(draft,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(draft);}return saved;
        }
        private static Mesh Save(Mesh mesh,string path)
        {var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}EditorUtility.CopySerialized(mesh,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(mesh);return saved;}
    }
}
