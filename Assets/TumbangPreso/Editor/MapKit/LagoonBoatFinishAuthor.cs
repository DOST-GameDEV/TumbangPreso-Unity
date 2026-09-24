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
    public static class LagoonBoatFinishAuthor
    {
        private const string Folder="Assets/TumbangPreso/Art/LagoonBoatFinish",SourceTag="TumpLagoonBoatSource",MeshTag="TUMP_LAGOON_BOAT_MESH:";
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene(LagoonBuilder.ScenePath);var report=new StringBuilder();FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/lagoon-boat-finish");File.WriteAllText("Logs/lagoon-boat-finish/author.txt",report.ToString());
            Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            var map=GameObject.Find("Lagoon");var solids=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();var hullTexture=HullTexture();
            var palm=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TumbangPreso/Art/LagoonGableFinish/PalmFibres.png");
            if(palm==null)throw new InvalidOperationException("Missing authored palm-fibre input.");
            for(int id=0;id<6;id++)
            {
                var boat=map.transform.Find((id<4?"Pointed working canoe ":"Sheltered houseboat ")+id);
                if(boat==null)throw new InvalidOperationException("Missing retained watercraft "+id);
                var motion=boat.GetComponent<MooredBoatMotion>();if(motion==null)throw new InvalidOperationException("Boat lost its retained mooring motion.");
                var tie=motion.DockTie;var bow=motion.BowLocal;var position=boat.localPosition;var rotation=boat.localRotation;
                var filter=boat.GetComponent<MeshFilter>();var renderer=boat.GetComponent<MeshRenderer>();
                var original=filter.sharedMesh;string sourcePath=AssetDatabase.GetAssetPath(original);var input=AssetImporter.GetAtPath(sourcePath);
                if(input!=null&&input.userData.StartsWith(MeshTag)){sourcePath=input.userData.Substring(MeshTag.Length);original=AssetDatabase.LoadAssetAtPath<Mesh>(sourcePath);}
                var slots=renderer.sharedMaterials;int hull=-1,roof=-1;
                for(int m=0;m<slots.Length;m++)
                {
                    string source=slots[m].GetTag(SourceTag,false);if(!string.IsNullOrEmpty(source))slots[m]=AssetDatabase.LoadAssetAtPath<Material>(source);
                    string name=MapSurfaceAuthor.SourceMaterialNameForAuthoring(slots[m]).Replace(" ","");
                    if(name.Equals("Workingboatpaint"+id,StringComparison.OrdinalIgnoreCase))hull=m;
                    if(name.Equals("Houseboatpalmshelter",StringComparison.OrdinalIgnoreCase))roof=m;
                }
                if(hull<0||(id>=4&&roof<0))throw new InvalidOperationException("Missing original hull/shelter material on "+id);
                var mesh=Object.Instantiate(original);mesh.name="Watercraft material coordinates "+id;
                var uv=mesh.uv;var oldUv=(Vector2[])uv.Clone();var points=mesh.vertices;var normals=mesh.normals;
                foreach(int v in mesh.GetTriangles(hull).Distinct())uv[v]=new Vector2(oldUv[v].x/2,(oldUv[v].y+.35f)/.70f);
                if(roof>=0)foreach(int v in mesh.GetTriangles(roof).Distinct())uv[v]=LagoonGableFinishAuthor.RoofUv(points[v],normals[v])+new Vector2(id*.13f,id*.23f);
                for(int m=0;m<mesh.subMeshCount;m++)if(m!=hull&&m!=roof&&mesh.GetTriangles(m).Any(v=>uv[v]!=oldUv[v]))throw new InvalidOperationException("Preserved boat part UV changed.");
                mesh.uv=uv;
                if(!original.vertices.SequenceEqual(mesh.vertices)||!original.triangles.SequenceEqual(mesh.triangles))throw new InvalidOperationException("Boat shape changed.");
                string path=Folder+"/Boat"+id+".asset";var saved=Save(mesh,path);var importer=AssetImporter.GetAtPath(path);importer.userData=MeshTag+sourcePath;importer.SaveAndReimport();filter.sharedMesh=saved;
                slots[hull]=Material(slots[hull],hullTexture,"Hull"+id,.85f,.19f);
                if(roof>=0)slots[roof]=Material(slots[roof],palm,"Shelter"+id,.85f,.08f);
                renderer.sharedMaterials=slots;
                if(motion.DockTie!=tie||motion.BowLocal!=bow||boat.localPosition!=position||boat.localRotation!=rotation)throw new InvalidOperationException("Boat motion/placement changed.");
                report.AppendLine("Boat"+id+": curved hull material"+(roof>=0?" and palm shelter fibres":"")+", source shape/other parts/mooring retained.");
            }
            if(solids.Count!=map.GetComponentsInChildren<Collider>(true).Length||solids.Any(p=>p.Key==null||p.Key.bounds!=p.Value))throw new InvalidOperationException("Boat finish changed collision.");
            report.AppendLine("Six watercraft, no added geometry/renderers/colliders or gameplay/motion changes.");
        }
        private static Texture2D HullTexture()
        {
            const int size=128;var tex=new Texture2D(size,size,TextureFormat.RGBA32,false);var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float u=x/(float)size,v=y/(float)(size-1),height=v*.70f-.35f;
                float seam=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.004f,.015f,Mathf.Abs(height+.15f)));
                float grain=Mathf.Sin(v*80+Mathf.Sin(u*Mathf.PI*2)*.35f);
                float paint=Mathf.Sin(u*Mathf.PI*2)*Mathf.Sin(v*Mathf.PI*4);
                float value=.85f+grain*.018f+paint*.028f-seam*.11f;
                pixels[y*size+x]=new Color(value,value,value,1);
            }
            tex.SetPixels(pixels);tex.Apply();string path=Folder+"/PaintedPlankedHull.png";
            File.WriteAllBytes(path,tex.EncodeToPNG());Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Bilinear;
            importer.mipmapEnabled=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        private static Material Material(Material source,Texture2D texture,string name,float grey,float smoothness)
        {
            var draft=Visual.NearFade.CopySurfaceForAuthoring(source);var c=source.color;
            draft.name=name+" fitted material";draft.color=new Color(c.r/grey,c.g/grey,c.b/grey,c.a);draft.mainTexture=texture;
            draft.mainTextureScale=Vector2.one;draft.mainTextureOffset=Vector2.zero;draft.SetFloat("_SurfaceKind",0);draft.SetFloat("_SurfaceVertexRoles",0);draft.SetFloat("_Glossiness",smoothness);
            draft.SetOverrideTag(SourceTag,AssetDatabase.GetAssetPath(source));string path=Folder+"/"+name+".mat";var saved=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(saved==null){AssetDatabase.CreateAsset(draft,path);return draft;}EditorUtility.CopySerialized(draft,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(draft);return saved;
        }
        private static Mesh Save(Mesh mesh,string path)
        {var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}EditorUtility.CopySerialized(mesh,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(mesh);return saved;}
    }
}
