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
    /// <summary>A working computer repair bench rather than a duplicate retail display.</summary>
    public static class IlalimRepairShopAuthor
    {
        public const string RootName="Computer repair workbench";
        private const string Folder="Assets/TumbangPreso/Art/IlalimRepairShop";
        private static readonly Color[] Colours={new Color(.74f,.73f,.66f),new Color(.22f,.27f,.27f),
            new Color(.46f,.50f,.49f),new Color(.82f,.79f,.66f),new Color(.26f,.39f,.46f),
            new Color(.59f,.62f,.59f),new Color(.51f,.24f,.19f),new Color(.60f,.56f,.43f)};
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/IlalimNgTulay.unity");var report=new StringBuilder();FinishLoadedScene(report);IlalimShopSignAuthor.FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/ilalim-repair-shop");File.WriteAllText("Logs/ilalim-repair-shop/author.txt",report.ToString());Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            var map=GameObject.Find("IlalimNgTulay");var solids=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            var room=map.transform.Find("Dressing/PlaceRework/Frontage_Repair");if(room==null)throw new InvalidOperationException("Missing Repair frontage.");
            var prior=room.Find(RootName);if(prior!=null)Object.DestroyImmediate(prior.gameObject);
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            foreach(var r in room.GetComponentsInChildren<MeshRenderer>())
                if(new[]{"Display monitor","Boxed shop stock","Glazed private shop boundary","Window frame"}.Contains(r.name))r.enabled=false;
            var g=new Geometry();
            // Open laptop, with keyboard and a matte diagnostic screen, sits on the counter.
            g.Box(new Vector3(-1.18f,1.15f,-1.08f),new Vector3(1.12f,.07f,.72f),2);
            g.Box(new Vector3(-1.18f,1.193f,-1.16f),new Vector3(.87f,.013f,.38f),1);
            for(int row=0;row<4;row++)for(int key=0;key<9;key++)
                g.Box(new Vector3(-1.55f+key*.093f,1.206f,-1.30f+row*.088f),new Vector3(.075f,.012f,.061f),0);
            g.Box(new Vector3(-1.18f,1.192f,-.84f),new Vector3(.32f,.014f,.17f),0);
            var hinge=new Vector3(-1.18f,1.20f,-1.40f);var tilt=Quaternion.Euler(-12,0,0);
            g.Box(hinge+tilt*new Vector3(0,.35f,0),new Vector3(1.12f,.70f,.06f),1,tilt);
            g.Box(hinge+tilt*new Vector3(0,.35f,.035f),new Vector3(.98f,.57f,.013f),4,tilt);
            // A few broad diagnostic lines read as a powered service screen, not a new UI.
            for(int i=0;i<3;i++)g.Box(hinge+tilt*new Vector3(-.16f,.49f-i*.09f,.044f),new Vector3(.53f-i*.08f,.018f,.009f),0,tilt);
            // Open side of a desktop case faces the street; the other side carries its board.
            var tower=new Vector3(1.25f,1.115f,-1.12f);
            g.Box(tower+new Vector3(0,.51f,-.30f),new Vector3(.98f,1.02f,.045f),2);
            foreach(float y in new[]{.035f,.995f})g.Box(tower+new Vector3(0,y,0),new Vector3(.98f,.065f,.64f),1);
            foreach(float x in new[]{-.465f,.465f})
            {
                g.Box(tower+new Vector3(x,.51f,-.03f),new Vector3(.055f,.94f,.60f),2);
                g.Box(tower+new Vector3(x,.51f,.295f),new Vector3(.035f,.95f,.028f),1);
            }
            g.Box(tower+new Vector3(-.085f,.59f,-.265f),new Vector3(.70f,.65f,.02f),4);
            g.Box(tower+new Vector3(-.12f,.67f,-.22f),new Vector3(.29f,.27f,.075f),2);
            for(int i=0;i<6;i++)g.Box(tower+new Vector3(-.235f+i*.046f,.67f,-.167f),new Vector3(.022f,.26f,.035f),0);
            foreach(float x in new[]{.13f,.235f})
            {g.Box(tower+new Vector3(x,.61f,-.23f),new Vector3(.05f,.47f,.04f),1);g.Box(tower+new Vector3(x,.385f,-.205f),new Vector3(.035f,.028f,.018f),7);}
            g.Box(tower+new Vector3(0,.22f,-.12f),new Vector3(.77f,.20f,.34f),1);
            g.Box(tower+new Vector3(.20f,.25f,.062f),new Vector3(.19f,.10f,.022f),0);
            // Service lead follows the inside edge; loose side panel rests flat on the bench.
            g.Beam(tower+new Vector3(.33f,.82f,-.15f),tower+new Vector3(.36f,.36f,-.12f),.025f,6);
            g.Box(new Vector3(.03f,1.14f,-1.08f),new Vector3(.77f,.05f,.63f),2);
            for(int i=0;i<5;i++)g.Box(new Vector3(-.17f+i*.074f,1.172f,-1.08f),new Vector3(.027f,.016f,.29f),1);
            g.Box(new Vector3(.10f,1.185f,-.755f),new Vector3(.25f,.023f,.035f),2);
            g.Box(new Vector3(-.08f,1.187f,-.755f),new Vector3(.17f,.042f,.063f),6);
            // Distinct computer packaging and spare drive cases on retained rear shelves.
            for(int shelf=0;shelf<2;shelf++)for(int column=0;column<4;column++)
            {
                float x=(column-1.5f)*1.07f,y=1.06f+shelf*.62f;
                var size=column%2==0?new Vector3(.77f,.31f,.35f):new Vector3(.40f,.45f,.35f);
                g.Box(new Vector3(x,y+size.y*.5f,-3.28f),size,column%2==0?1:0);
                g.Box(new Vector3(x,y+size.y*.60f,-3.098f),new Vector3(size.x*.72f,size.y*.32f,.012f),column%2==0?7:4);
            }
            var face=room.Find("Sign face Repair");
            foreach(float dx in new[]{-.36f,.36f})g.Box(new Vector3(face.localPosition.x+face.localScale.x*dx,2.96f,.50f),new Vector3(.055f,.95f,.065f),2);
            var go=new GameObject(RootName);go.transform.SetParent(room,false);go.isStatic=true;
            var mesh=g.Build();int vertices=mesh.vertexCount;go.AddComponent<MeshFilter>().sharedMesh=Save(mesh,Folder+"/RepairBench.asset");
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=Palette();renderer.shadowCastingMode=ShadowCastingMode.Off;
            AirborneByDesign.Attach(go,"Open laptop/case/panel/tools rest on retained counter; parts packaging rests on retained shelves; sign supports attach to fascia.");
            if(solids.Count!=map.GetComponentsInChildren<Collider>(true).Length||solids.Any(p=>p.Key==null||p.Key.bounds!=p.Value))throw new InvalidOperationException("Repair shop changed private boundary/gameplay collision.");
            report.AppendLine("Repair only: open laptop, exposed desktop case and broad components, supported removed panel/tool, distinct spare parts on shelves. "+vertices+" vertices, one renderer/material, no new collider/shadow caster; retained counter/private boundary unchanged.");
        }
        private static Material Palette()
        {
            var texture=new Texture2D(8,1,TextureFormat.RGBA32,false);texture.SetPixels(Colours);texture.Apply();string path=Folder+"/RepairPalette.png";File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
            var i=(TextureImporter)AssetImporter.GetAtPath(path);i.npotScale=TextureImporterNPOTScale.None;i.filterMode=FilterMode.Point;i.mipmapEnabled=false;i.wrapMode=TextureWrapMode.Clamp;i.textureCompression=TextureImporterCompression.Uncompressed;i.SaveAndReimport();
            var m=new Material(Shader.Find("Standard")){color=Color.white,mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path)};m.SetFloat("_Glossiness",.10f);return Save(m,Folder+"/RepairBench.mat");
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
            public Mesh Build(){var m=new Mesh{name="Repair bench and supported computer parts"};m.SetVertices(points);m.SetUVs(0,uv);m.SetTriangles(indices,0);m.RecalculateNormals();m.RecalculateBounds();return m;}
        }
    }
}
