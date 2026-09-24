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
    /// <summary>Owner-referenced ukay shop with paired footwear and hanging garments.</summary>
    public static class IlalimUkayAuthor
    {
        public const string RootName="Ciao Grazey shoe and garment display";
        private const string Folder="Assets/TumbangPreso/Art/IlalimUkay";
        private static readonly Color[] Colours={new Color(.74f,.73f,.66f),new Color(.22f,.27f,.27f),
            new Color(.46f,.50f,.49f),new Color(.82f,.79f,.66f),new Color(.26f,.39f,.46f),
            new Color(.59f,.62f,.59f),new Color(.51f,.24f,.19f),new Color(.60f,.56f,.43f)};
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/IlalimNgTulay.unity");var report=new StringBuilder();FinishLoadedScene(report);IlalimShopSignAuthor.FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/ilalim-ukay");File.WriteAllText("Logs/ilalim-ukay/author.txt",report.ToString());Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            var map=GameObject.Find("IlalimNgTulay");var solids=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            var vendor=map.transform.Find("Dressing/PlaceRework/Vendor_ClothesAndCases");
            if(vendor==null)throw new InvalidOperationException("Missing retained street clothes rack.");
            var vendorBefore=vendor.position;var vendorAfter=vendorBefore;vendorAfter.z=-19.4f;vendor.position=vendorAfter;
            var vendorShift=vendorAfter-vendorBefore;Physics.SyncTransforms();
            var room=map.transform.Find("Dressing/PlaceRework/Frontage_Clothing");if(room==null)throw new InvalidOperationException("Missing Clothing frontage.");
            var prior=room.Find(RootName);if(prior!=null)Object.DestroyImmediate(prior.gameObject);
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            foreach(var r in room.GetComponentsInChildren<MeshRenderer>())
                if(new[]{"Folded stock","Back shelf","Window frame"}.Contains(r.name))r.enabled=false;
            var g=new Geometry();
            // Open shoe shelving sits on the existing shop counter, with three paired groups.
            foreach(float x in new[]{-1.99f,1.99f})foreach(float z in new[]{-.71f,-1.48f})
                g.Box(new Vector3(x,1.64f,z),new Vector3(.035f,1.05f,.035f),2);
            for(int tier=0;tier<3;tier++)
            {
                float y=1.15f+tier*.34f;
                g.Box(new Vector3(0,y,-1.09f),new Vector3(4.02f,.03f,.80f),0);
                g.Box(new Vector3(0,y+.018f,-.68f),new Vector3(4.02f,.035f,.02f),2);
                for(int pair=0;pair<3;pair++)foreach(float side in new[]{-1f,1f})
                    g.Shoe(new Vector3((pair-1)*1.29f+side*.18f,y+.015f,-1.08f),Quaternion.Euler(0,-side*12,0),
                        pair==0?1:pair==1?4:6,tier==1&&pair!=1);
            }
            // Rear clothes rail: solid posts, actual hangers and garment silhouettes.
            foreach(float x in new[]{-2.22f,2.22f})g.Box(new Vector3(x,1.21f,-3.24f),new Vector3(.055f,2.30f,.055f),2);
            g.Beam(new Vector3(-2.22f,2.37f,-3.24f),new Vector3(2.22f,2.37f,-3.24f),.047f,2);
            for(int i=0;i<6;i++)
            {
                var at=new Vector3(-1.87f+i*.75f,2.13f,-3.19f);int colour=i%3==0?4:i%3==1?6:3;
                g.Beam(at+new Vector3(-.24f,-.05f,0),at+new Vector3(0,.13f,0),.018f,1);
                g.Beam(at+new Vector3(0,.13f,0),at+new Vector3(.24f,-.05f,0),.018f,1);
                g.Beam(at+new Vector3(-.24f,-.05f,0),at+new Vector3(.24f,-.05f,0),.018f,1);
                g.Beam(at+new Vector3(0,.13f,0),at+new Vector3(0,.24f,-.05f),.019f,1);
                if(i==2||i==5)
                {
                    g.Box(at+new Vector3(0,-.13f,.035f),new Vector3(.45f,.16f,.06f),colour);
                    foreach(float side in new[]{-1f,1f})g.Box(at+new Vector3(side*.118f,-.46f,.035f),new Vector3(.195f,.61f,.06f),colour);
                }
                else g.Shirt(at,colour,i==0||i==4);
            }
            var face=room.Find("Sign face Clothing");
            foreach(float dx in new[]{-.36f,.36f})g.Box(new Vector3(face.localPosition.x+face.localScale.x*dx,2.96f,.50f),new Vector3(.055f,.95f,.065f),2);
            var go=new GameObject(RootName);go.transform.SetParent(room,false);go.isStatic=true;
            var mesh=g.Build();int vertices=mesh.vertexCount;go.AddComponent<MeshFilter>().sharedMesh=Save(mesh,Folder+"/UkayDisplay.asset");
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=Palette();renderer.shadowCastingMode=ShadowCastingMode.Off;
            AirborneByDesign.Attach(go,"Shoe shelving stands on retained counter; clothes hang on supported rail/hangers; sign fixings attach to fascia.");
            if(solids.Count!=map.GetComponentsInChildren<Collider>(true).Length)throw new InvalidOperationException("Ukay changed collider count.");
            foreach(var pair in solids)
            {
                if(pair.Key==null)throw new InvalidOperationException("Ukay removed an original collider.");
                var expected=pair.Value;if(pair.Key.transform.IsChildOf(vendor))expected.center+=vendorShift;
                if((pair.Key.bounds.center-expected.center).sqrMagnitude>.00001f||(pair.Key.bounds.size-expected.size).sqrMagnitude>.00001f)
                    throw new InvalidOperationException("Ukay changed collision beyond the explicit retained rack translation.");
            }
            report.AppendLine("Clothing only: paired footwear with soles/uppers/collars on three supported shelves, shirts/jackets/trousers on rear hanger rail. "+vertices+" vertices, one renderer/material, no new collider/shadow caster; retained counter/window/private boundary unchanged; existing street clothes rack shifted along same pavement to z-19.4, beyond shop bay, retaining model/height/scale/colliders.");
        }
        private static Material Palette()
        {
            var texture=new Texture2D(8,1,TextureFormat.RGBA32,false);texture.SetPixels(Colours);texture.Apply();string path=Folder+"/UkayPalette.png";File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
            var i=(TextureImporter)AssetImporter.GetAtPath(path);i.npotScale=TextureImporterNPOTScale.None;i.filterMode=FilterMode.Point;i.mipmapEnabled=false;i.wrapMode=TextureWrapMode.Clamp;i.textureCompression=TextureImporterCompression.Uncompressed;i.SaveAndReimport();
            var m=new Material(Shader.Find("Standard")){color=Color.white,mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path)};m.SetFloat("_Glossiness",.10f);return Save(m,Folder+"/UkayDisplay.mat");
        }
        private static T Save<T>(T draft,string path) where T:Object
        {var saved=AssetDatabase.LoadAssetAtPath<T>(path);if(saved==null){AssetDatabase.CreateAsset(draft,path);return draft;}EditorUtility.CopySerialized(draft,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(draft);return saved;}
        private sealed class Geometry
        {
            private readonly List<Vector3> points=new List<Vector3>();private readonly List<Vector2> uv=new List<Vector2>();private readonly List<int> indices=new List<int>();
            private void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,int colour)
            {int n=points.Count;points.AddRange(new[]{a,b,c,d});indices.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});for(int i=0;i<4;i++)uv.Add(new Vector2((colour+.5f)/8,.5f));}
            public void Box(Vector3 at,Vector3 size,int colour,Quaternion? rotation=null)
            {
                var h=size*.5f;var q=rotation??Quaternion.identity;Vector3 P(float x,float y,float z)=>at+q*Vector3.Scale(h,new Vector3(x,y,z));
                Quad(P(-1,-1,1),P(1,-1,1),P(1,1,1),P(-1,1,1),colour);Quad(P(1,-1,-1),P(-1,-1,-1),P(-1,1,-1),P(1,1,-1),colour);
                Quad(P(-1,-1,-1),P(-1,-1,1),P(-1,1,1),P(-1,1,-1),colour);Quad(P(1,-1,1),P(1,-1,-1),P(1,1,-1),P(1,1,1),colour);
                Quad(P(-1,1,-1),P(-1,1,1),P(1,1,1),P(1,1,-1),colour);Quad(P(-1,-1,1),P(-1,-1,-1),P(1,-1,-1),P(1,-1,1),colour);
            }
            public void Beam(Vector3 a,Vector3 b,float thickness,int colour)=>Box((a+b)*.5f,new Vector3(thickness,Vector3.Distance(a,b),thickness),colour,Quaternion.FromToRotation(Vector3.up,b-a));
            public void Shoe(Vector3 at,Quaternion q,int colour,bool high)
            {
                Vector3 P(float x,float y,float z)=>at+q*new Vector3(x,y,z);
                Box(P(0,.025f,0),new Vector3(.235f,.05f,.51f),0,q);
                float ankle=high?.28f:.195f;
                var profile=new[]{new Vector2(.026f,.25f),new Vector2(.085f,.27f),new Vector2(.14f,.18f),new Vector2(.16f,.01f),new Vector2(ankle,-.055f),new Vector2(ankle,-.20f),new Vector2(.08f,-.24f),new Vector2(.026f,-.22f)};
                for(int i=0;i<profile.Length;i++)
                {
                    var a=profile[i];var b=profile[(i+1)%profile.Length];
                    Quad(P(-.10f,a.x,a.y),P(.10f,a.x,a.y),P(.10f,b.x,b.y),P(-.10f,b.x,b.y),colour);
                    Quad(P(.10f,.09f,0),P(.10f,b.x,b.y),P(.10f,a.x,a.y),P(.10f,.09f,0),colour);
                    Quad(P(-.10f,.09f,0),P(-.10f,a.x,a.y),P(-.10f,b.x,b.y),P(-.10f,.09f,0),colour);
                }
                Box(P(0,ankle+.006f,-.13f),new Vector3(.14f,.018f,.104f),1,q);
                for(int i=0;i<3;i++)Box(P(0,.174f,.055f-i*.035f),new Vector3(.132f,.012f,.013f),0,q);
            }
            public void Shirt(Vector3 at,int colour,bool jacket)
            {
                Box(at+new Vector3(0,-.36f,.035f),new Vector3(.47f,.57f,.08f),colour);
                foreach(float side in new[]{-1f,1f})
                {
                    Box(at+new Vector3(side*.30f,-.16f,.035f),new Vector3(.20f,.30f,.08f),colour,Quaternion.Euler(0,0,side*31));
                    if(jacket)Box(at+new Vector3(side*.37f,-.34f,.035f),new Vector3(.17f,.30f,.08f),colour);
                }
                Box(at+new Vector3(0,-.073f,.07f),new Vector3(.14f,.065f,.06f),1);
                if(jacket)Box(at+new Vector3(0,-.35f,.081f),new Vector3(.022f,.51f,.012f),2);
                Box(at+new Vector3(0,-.642f,.073f),new Vector3(.43f,.02f,.02f),colour==3?7:1);
            }
            public Mesh Build(){var m=new Mesh{name="Paired shoes and hanging garments"};m.SetVertices(points);m.SetUVs(0,uv);m.SetTriangles(indices,0);m.RecalculateNormals();m.RecalculateBounds();return m;}
        }
    }
}
