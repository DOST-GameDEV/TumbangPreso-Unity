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
    /// <summary>One print shop's legible working interior and fitted street awning.</summary>
    public static class IlalimPrintShopAuthor
    {
        public const string RootName="Print service equipment and awning";
        private const string Folder="Assets/TumbangPreso/Art/IlalimPrintShop";
        private static readonly Color[] Colours={new Color(.74f,.73f,.66f),new Color(.22f,.27f,.27f),
            new Color(.46f,.50f,.49f),new Color(.82f,.79f,.66f),new Color(.26f,.39f,.46f),
            new Color(.59f,.62f,.59f),new Color(.51f,.24f,.19f),new Color(.60f,.56f,.43f)};
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/IlalimNgTulay.unity");var report=new StringBuilder();FinishLoadedScene(report);IlalimShopSignAuthor.FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/ilalim-print-shop");File.WriteAllText("Logs/ilalim-print-shop/author.txt",report.ToString());Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            var map=GameObject.Find("IlalimNgTulay");var solids=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            var room=map.transform.Find("Dressing/PlaceRework/Frontage_Print");if(room==null)throw new InvalidOperationException("Missing Print frontage.");
            var prior=room.Find(RootName);if(prior!=null)Object.DestroyImmediate(prior.gameObject);
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            foreach(var r in room.GetComponentsInChildren<MeshRenderer>())
                if(new[]{"Photocopier","Copier lid","Paper ream","Boxed shop stock","Glazed private shop boundary","Window frame"}.Contains(r.name))r.enabled=false;
            var g=new Geometry();
            g.Box(new Vector3(0,.50f,.218f),new Vector3(5.55f,.70f,.024f),6);
            // Copier sits on the retained 1.115m counter top. Drawers, output throat,
            // scanner bed and controls form a recognizable machine at street scale.
            g.Box(new Vector3(-1.2f,1.385f,-.70f),new Vector3(1.20f,.54f,.78f),0);
            g.Box(new Vector3(-1.2f,1.68f,-.70f),new Vector3(1.28f,.07f,.84f),1);
            g.Box(new Vector3(-1.2f,1.745f,-.76f),new Vector3(1.24f,.08f,.70f),0);
            g.Box(new Vector3(-1.2f,1.81f,-.94f),new Vector3(.76f,.065f,.29f),2);
            g.Box(new Vector3(-1.39f,1.47f,-.298f),new Vector3(.66f,.14f,.025f),1);
            g.Box(new Vector3(-1.39f,1.38f,-.20f),new Vector3(.70f,.055f,.27f),2);
            g.Box(new Vector3(-1.39f,1.414f,-.16f),new Vector3(.46f,.015f,.25f),3);
            foreach(float y in new[]{1.22f,1.33f})
            {
                g.Box(new Vector3(-1.2f,y,-.298f),new Vector3(1.05f,.012f,.025f),2);
                g.Box(new Vector3(-1.18f,y+.042f,-.272f),new Vector3(.29f,.036f,.05f),1);
            }
            g.Box(new Vector3(-.70f,1.707f,-.30f),new Vector3(.28f,.08f,.20f),1);
            g.Box(new Vector3(-.73f,1.751f,-.31f),new Vector3(.14f,.012f,.115f),4);
            g.Box(new Vector3(-.60f,1.751f,-.27f),new Vector3(.055f,.012f,.06f),6);
            // Low laminator and emerging sheet, separate from stacked wrapped reams.
            g.Box(new Vector3(.32f,1.19f,-.74f),new Vector3(.69f,.15f,.35f),1);
            g.Box(new Vector3(.32f,1.18f,-.552f),new Vector3(.56f,.023f,.025f),2);
            g.Box(new Vector3(.32f,1.19f,-.41f),new Vector3(.40f,.013f,.29f),3);
            for(int i=0;i<4;i++)
            {
                g.Box(new Vector3(1.38f,1.165f+i*.095f,-.75f),new Vector3(.62f,.085f,.44f),3);
                g.Box(new Vector3(1.38f,1.165f+i*.095f,-.521f),new Vector3(.25f,.075f,.012f),i%2==0?4:6);
            }
            // Group paper stock by format rather than an identical multicolour food shelf.
            for(int tier=0;tier<2;tier++)for(int column=0;column<4;column++)
            {
                float x=-1.6f+column*1.02f,y=1.075f+tier*.62f;
                int stacks=column==2?3:2;
                for(int j=0;j<stacks;j++)g.Box(new Vector3(x,y+.06f+j*.12f,-3.26f),new Vector3(.65f,.11f,.36f),column==3?7:3);
                g.Box(new Vector3(x,y+.12f,-3.07f),new Vector3(.19f,.18f,.012f),column%2==0?4:6);
            }
            // Narrow side security sections leave the central serving opening readable.
            foreach(float side in new[]{-1f,1f})
            {
                g.Box(new Vector3(side*2.08f,1.76f,.13f),new Vector3(1.36f,.055f,.085f),4);
                foreach(float dx in new[]{-.50f,-.17f,.17f,.50f})g.Box(new Vector3(side*2.08f+dx,1.74f,.13f),new Vector3(.035f,1.39f,.045f),4);
                g.Box(new Vector3(side*1.35f,1.74f,.13f),new Vector3(.07f,1.48f,.085f),4);
            }
            // Awning sits below the raised sign; braces return into the existing piers.
            var tilt=Quaternion.Euler(9,0,0);
            g.Box(new Vector3(0,2.52f,.44f),new Vector3(5.90f,.045f,1.25f),5,tilt);
            for(int i=0;i<30;i++)g.Box(new Vector3(-2.85f+i*.197f,2.552f,.44f),new Vector3(.043f,.027f,1.25f),2,tilt);
            g.Box(new Vector3(0,2.407f,1.055f),new Vector3(5.94f,.075f,.065f),2);
            foreach(float x in new[]{-2.65f,2.65f})g.Beam(new Vector3(x,1.95f,.17f),new Vector3(x,2.43f,.99f),.065f,1);
            var face=room.Find("Sign face Print");var backing=room.Find("Shop sign backing Print");
            var pos=face.localPosition;pos.y=3.16f;face.localPosition=pos;pos=backing.localPosition;pos.y=3.16f;backing.localPosition=pos;
            foreach(float dx in new[]{-.36f,.36f})g.Box(new Vector3(face.localPosition.x+face.localScale.x*dx,3.15f,.50f),new Vector3(.075f,.98f,.075f),2);
            var go=new GameObject(RootName);go.transform.SetParent(room,false);go.isStatic=true;
            var mesh=g.Build();int vertices=mesh.vertexCount;go.AddComponent<MeshFilter>().sharedMesh=Save(mesh,Folder+"/PrintEquipment.asset");
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=Palette();renderer.shadowCastingMode=ShadowCastingMode.Off;
            AirborneByDesign.Attach(go,"Copier/laminator rest on retained counter; stock rests on shelves; grille and awning attach to retained piers; sign straps attach to slab.");
            if(solids.Count!=map.GetComponentsInChildren<Collider>(true).Length||solids.Any(p=>p.Key==null||p.Key.bounds!=p.Value))throw new InvalidOperationException("Print shop changed private boundary/gameplay collision.");
            report.AppendLine("Print only: legible copier/laminator/paper stocks, side grille, supported corrugated awning and raised sign straps. "+vertices+" vertices, one renderer/material, existing room/boundary/collision preserved. Other shops untouched.");
        }
        private static Material Palette()
        {
            var texture=new Texture2D(8,1,TextureFormat.RGBA32,false);texture.SetPixels(Colours);texture.Apply();string path=Folder+"/PrintPalette.png";File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
            var i=(TextureImporter)AssetImporter.GetAtPath(path);i.npotScale=TextureImporterNPOTScale.None;i.filterMode=FilterMode.Point;i.mipmapEnabled=false;i.wrapMode=TextureWrapMode.Clamp;i.textureCompression=TextureImporterCompression.Uncompressed;i.SaveAndReimport();
            var m=new Material(Shader.Find("Standard")){color=Color.white,mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path)};m.SetFloat("_Glossiness",.10f);return Save(m,Folder+"/PrintEquipment.mat");
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
            public Mesh Build(){var m=new Mesh{name="Print service equipment and supported awning"};m.SetVertices(points);m.SetUVs(0,uv);m.SetTriangles(indices,0);m.RecalculateNormals();m.RecalculateBounds();return m;}
        }
    }
}
