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
    /// <summary>A window-served sari-sari shop with supported packets, tins and bottles.</summary>
    public static class IlalimTindahanAuthor
    {
        public const string RootName="Tindahan serving window and stock";
        private const string Folder="Assets/TumbangPreso/Art/IlalimTindahan";
        private static readonly Color[] Colours={new Color(.74f,.73f,.66f),new Color(.15f,.22f,.35f),
            new Color(.46f,.50f,.49f),new Color(.82f,.79f,.66f),new Color(.27f,.41f,.30f),
            new Color(.59f,.62f,.59f),new Color(.51f,.24f,.19f),new Color(.60f,.56f,.43f)};
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/IlalimNgTulay.unity");var report=new StringBuilder();FinishLoadedScene(report);IlalimShopSignAuthor.FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/ilalim-tindahan");File.WriteAllText("Logs/ilalim-tindahan/author.txt",report.ToString());Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            var map=GameObject.Find("IlalimNgTulay");var solids=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            var room=map.transform.Find("Dressing/PlaceRework/Frontage_Load");if(room==null)throw new InvalidOperationException("Missing Load frontage.");
            var prior=room.Find(RootName);if(prior!=null)Object.DestroyImmediate(prior.gameObject);
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            foreach(var r in room.GetComponentsInChildren<MeshRenderer>())
                if(new[]{"Boxed shop stock","Glazed private shop boundary","Window frame"}.Contains(r.name))r.enabled=false;
            var g=new Geometry();
            // Side grilles and hanging stock leave the centre free for serving customers.
            foreach(float side in new[]{-1f,1f})
            {
                g.Box(new Vector3(side*2.04f,1.77f,.125f),new Vector3(1.38f,.045f,.06f),4);
                foreach(float dx in new[]{-.58f,-.29f,0,.29f,.58f})g.Box(new Vector3(side*2.04f+dx,1.75f,.125f),new Vector3(.027f,1.42f,.04f),4);
                g.Box(new Vector3(side*2.01f,2.38f,.23f),new Vector3(1.47f,.035f,.045f),1);
                for(int strip=0;strip<4;strip++)
                {
                    float x=side*2.01f+(strip-1.5f)*.29f;int colour=strip%3==0?6:strip%3==1?3:4;
                    g.Box(new Vector3(x,1.925f,.255f),new Vector3(.042f,.87f,.018f),0);
                    for(int packet=0;packet<4;packet++)
                    {
                        float y=2.22f-packet*.21f;
                        g.Box(new Vector3(x,y,.284f),new Vector3(.215f,.19f,.033f),colour);
                        g.Box(new Vector3(x,y+.072f,.306f),new Vector3(.204f,.013f,.012f),0);
                        g.Box(new Vector3(x,y-.066f,.306f),new Vector3(.204f,.013f,.012f),0);
                        g.Box(new Vector3(x,y,.306f),new Vector3(.12f,.065f,.014f),colour==3?6:3);
                    }
                }
            }
            // A shallow crate on the retained counter contains small bottles, not loose props.
            var crate=new Vector3(-1.27f,1.115f,-1.09f);
            g.Box(crate+new Vector3(0,.03f,0),new Vector3(.93f,.055f,.52f),4);
            foreach(float x in new[]{-.45f,.45f})g.Box(crate+new Vector3(x,.145f,0),new Vector3(.04f,.25f,.52f),4);
            foreach(float z in new[]{-.24f,.24f})foreach(float y in new[]{.075f,.205f})g.Box(crate+new Vector3(0,y,z),new Vector3(.93f,.08f,.035f),4);
            for(int x=0;x<4;x++)for(int z=0;z<2;z++)g.Bottle(crate+new Vector3((x-1.5f)*.21f,.058f,(z-.5f)*.23f));
            g.Beam(new Vector3(1.08f,1.70f,.35f),new Vector3(1.34f,1.77f,.13f),.018f,1);
            // Three squat snack tins at the serving counter, with lids and simple paper bands.
            for(int i=0;i<3;i++)
            {
                var at=new Vector3(.56f+i*.49f,1.115f,-1.07f);
                g.Cylinder(at,.18f,.40f,7);g.Cylinder(at+Vector3.up*.10f,.183f,.18f,i==1?4:6);
                g.Cylinder(at+Vector3.up*.40f,.19f,.035f,2);
            }
            // Useful stock is grouped by form on the actual supported back shelves.
            for(int shelf=0;shelf<2;shelf++)
            {
                float y=1.065f+shelf*.62f;
                for(int i=0;i<6;i++)g.Bottle(new Vector3(-2.24f+i*.22f,y,-3.28f));
                for(int i=0;i<4;i++)
                {
                    var at=new Vector3(.05f+i*.53f,y,-3.28f);g.Cylinder(at,.17f,.35f,3);g.Cylinder(at+Vector3.up*.065f,.174f,.21f,i%2==0?6:4);
                    g.Cylinder(at+Vector3.up*.35f,.18f,.022f,2);
                }
            }
            // Owner-selected Aling Pasing reference: green grille, white ledge and a
            // supported navy/white cloth awning with a short triangular valance.
            g.Box(new Vector3(0,1.035f,.205f),new Vector3(5.78f,.115f,.45f),0);
            var awningTilt=Quaternion.Euler(8,0,0);
            g.Box(new Vector3(0,2.53f,.42f),new Vector3(5.88f,.035f,1.10f),0,awningTilt);
            for(int stripe=0;stripe<18;stripe++)
            {
                float x=-2.88f+stripe*.32f;
                if(stripe%2==0)g.Box(new Vector3(x+.13f,2.552f,.42f),new Vector3(.31f,.010f,1.10f),1,awningTilt);
                g.Triangle(new Vector3(x,2.457f,.966f),new Vector3(x+.16f,2.24f,.966f),new Vector3(x+.32f,2.457f,.966f),stripe%2==0?1:0);
            }
            foreach(float x in new[]{-2.64f,2.64f})g.Beam(new Vector3(x,2.00f,.15f),new Vector3(x,2.46f,.94f),.055f,4);
            var face=room.Find("Sign face Load");var board=room.Find("Shop sign backing Load");
            var signPosition=face.localPosition;signPosition.y=3.15f;face.localPosition=signPosition;signPosition=board.localPosition;signPosition.y=3.15f;board.localPosition=signPosition;
            foreach(float dx in new[]{-.36f,.36f})g.Box(new Vector3(face.localPosition.x+face.localScale.x*dx,3.13f,.50f),new Vector3(.055f,.95f,.065f),2);
            var go=new GameObject(RootName);go.transform.SetParent(room,false);go.isStatic=true;
            var mesh=g.Build();int vertices=mesh.vertexCount;go.AddComponent<MeshFilter>().sharedMesh=Save(mesh,Folder+"/TindahanStock.asset");
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=Palette();renderer.shadowCastingMode=ShadowCastingMode.Off;
            AirborneByDesign.Attach(go,"Hanging packets attach to supported grille rail; crate/tins rest on retained counter, bottle/tin groups on rear shelves; fascia supports attach to slab.");
            var card=GameObject.CreatePrimitive(PrimitiveType.Quad);card.name="Load service placard";card.transform.SetParent(go.transform,false);
            card.transform.localPosition=new Vector3(1.08f,1.44f,.36f);card.transform.localRotation=Quaternion.Euler(0,180,0);card.transform.localScale=new Vector3(.52f,.52f,1);
            Object.DestroyImmediate(card.GetComponent<Collider>());card.isStatic=true;
            const string placardPath="Assets/TumbangPreso/Art/IlalimShopSigns/Load-placard-v1.png";AssetDatabase.ImportAsset(placardPath);
            var importer=(TextureImporter)AssetImporter.GetAtPath(placardPath);importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=512;importer.wrapMode=TextureWrapMode.Clamp;importer.mipmapEnabled=true;importer.SaveAndReimport();
            var cardMat=new Material(Shader.Find("Standard")){color=Color.white,mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(placardPath)};cardMat.SetFloat("_Glossiness",.03f);
            var cardRenderer=card.GetComponent<MeshRenderer>();cardRenderer.sharedMaterial=Save(cardMat,Folder+"/LoadPlacard.mat");cardRenderer.shadowCastingMode=ShadowCastingMode.Off;
            if(solids.Count!=map.GetComponentsInChildren<Collider>(true).Length||solids.Any(p=>p.Key==null||p.Key.bounds!=p.Value))throw new InvalidOperationException("Tindahan changed private boundary/gameplay collision.");
            report.AppendLine("Tindahan only: side serving grilles with hanging packets, supported bottle crate/snack tins, stocked shelves and fascia supports. "+vertices+" vertices, plus separate small load placard, two renderers/materials, no new collider/shadow caster. Tara Laro paint, counter/private boundary and street cart preserved.");
        }
        private static Material Palette()
        {
            var texture=new Texture2D(8,1,TextureFormat.RGBA32,false);texture.SetPixels(Colours);texture.Apply();string path=Folder+"/TindahanPalette.png";File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
            var i=(TextureImporter)AssetImporter.GetAtPath(path);i.npotScale=TextureImporterNPOTScale.None;i.filterMode=FilterMode.Point;i.mipmapEnabled=false;i.wrapMode=TextureWrapMode.Clamp;i.textureCompression=TextureImporterCompression.Uncompressed;i.SaveAndReimport();
            var m=new Material(Shader.Find("Standard")){color=Color.white,mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path)};m.SetFloat("_Glossiness",.10f);return Save(m,Folder+"/TindahanStock.mat");
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
            public void Triangle(Vector3 a,Vector3 b,Vector3 c,int colour)
            {Quad(a,b,c,a,colour);Quad(c,b,a,c,colour);}
            public void Cylinder(Vector3 at,float radius,float height,int colour)
            {
                Vector3 P(int i,float y){float a=i*Mathf.PI/4;return at+new Vector3(Mathf.Cos(a)*radius,y,Mathf.Sin(a)*radius);}
                for(int i=0;i<8;i++)
                {Quad(P(i+1,0),P(i,0),P(i,height),P(i+1,height),colour);Quad(at+Vector3.up*height,P(i+1,height),P(i,height),at+Vector3.up*height,colour);}
            }
            public void Bottle(Vector3 at)
            {
                Cylinder(at,.070f,.28f,4);Cylinder(at+Vector3.up*.08f,.072f,.115f,3);
                Cylinder(at+Vector3.up*.28f,.040f,.115f,4);Cylinder(at+Vector3.up*.395f,.045f,.026f,6);
            }
            public Mesh Build(){var m=new Mesh{name="Sari-sari serving window and stock"};m.SetVertices(points);m.SetUVs(0,uv);m.SetTriangles(indices,0);m.RecalculateNormals();m.RecalculateBounds();return m;}
        }
    }
}
