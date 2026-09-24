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
    /// <summary>Two fitted barber workstations, with retained chairs facing their mirrors.</summary>
    public static class IlalimBarberAuthor
    {
        public const string RootName="Barber chair fittings and workstations";
        private const string Folder="Assets/TumbangPreso/Art/IlalimBarber";
        private static readonly Color[] Colours={new Color(.74f,.73f,.66f),new Color(.22f,.27f,.27f),
            new Color(.46f,.50f,.49f),new Color(.82f,.79f,.66f),new Color(.26f,.39f,.46f),
            new Color(.59f,.62f,.59f),new Color(.47f,.23f,.24f),new Color(.60f,.56f,.43f)};
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/IlalimNgTulay.unity");var report=new StringBuilder();FinishLoadedScene(report);IlalimShopSignAuthor.FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/ilalim-barber");File.WriteAllText("Logs/ilalim-barber/author.txt",report.ToString());Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            var map=GameObject.Find("IlalimNgTulay");var solids=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            var room=map.transform.Find("Dressing/PlaceRework/Frontage_Barber");if(room==null)throw new InvalidOperationException("Missing Barber frontage.");
            var prior=room.Find(RootName);if(prior!=null)Object.DestroyImmediate(prior.gameObject);
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            foreach(var r in room.GetComponentsInChildren<MeshRenderer>())
                if(new[]{"Shopfront lower wall","Back shelf"}.Contains(r.name))r.enabled=false;
            foreach(Transform chair in room)if(chair.name=="Barber chair")
            {
                if(chair.GetComponentsInChildren<Collider>().Length!=0)throw new InvalidOperationException("Barber chair unexpectedly has gameplay collision.");
                chair.localRotation=Quaternion.Euler(0,180,0);
            }
            var g=new Geometry();
            // Glazed lower frontage reveals furniture function while retaining the solid boundary.
            g.Box(new Vector3(0,.16f,.209f),new Vector3(4.88f,.19f,.022f),0);
            foreach(float y in new[]{.28f,.95f})g.Box(new Vector3(0,y,.21f),new Vector3(4.88f,.034f,.045f),2);
            foreach(float x in new[]{-2.40f,0,2.40f})g.Box(new Vector3(x,.60f,.21f),new Vector3(.035f,.65f,.045f),2);
            foreach(float x in new[]{-1.2f,1.2f})
            {
                // Retained red seats now face the actual rear mirrors.
                g.Cylinder(new Vector3(x,.067f,-1.60f),.43f,.045f,2);
                foreach(float side in new[]{-1f,1f})
                {
                    g.Beam(new Vector3(x+side*.44f,.79f,-1.28f),new Vector3(x+side*.44f,1.065f,-1.34f),.046f,2);
                    g.Beam(new Vector3(x+side*.44f,.80f,-1.84f),new Vector3(x+side*.44f,1.065f,-1.90f),.046f,2);
                    g.Box(new Vector3(x+side*.44f,1.095f,-1.63f),new Vector3(.14f,.07f,.63f),0);
                    g.Beam(new Vector3(x+side*.29f,.68f,-1.86f),new Vector3(x+side*.29f,.36f,-2.15f),.044f,2);
                }
                g.Box(new Vector3(x,1.61f,-1.28f),new Vector3(.11f,.18f,.055f),2);
                g.Box(new Vector3(x,1.72f,-1.28f),new Vector3(.45f,.19f,.13f),6);
                g.Box(new Vector3(x,.36f,-2.18f),new Vector3(.69f,.045f,.30f),2);
                for(int stripe=0;stripe<6;stripe++)g.Box(new Vector3(x-.26f+stripe*.105f,.391f,-2.18f),new Vector3(.045f,.015f,.25f),1);
                // Work ledge anchored beneath each original mirror, with tools and towels.
                g.Box(new Vector3(x,1.08f,-3.07f),new Vector3(1.24f,.10f,.45f),3);
                g.Box(new Vector3(x,.83f,-3.12f),new Vector3(1.08f,.41f,.32f),7);
                g.Box(new Vector3(x,.91f,-2.947f),new Vector3(.28f,.03f,.026f),2);
                for(int i=0;i<3;i++)g.Box(new Vector3(x+.35f,1.155f+i*.04f,-3.08f),new Vector3(.37f,.036f,.30f),0);
                foreach(float dx in new[]{-.41f,-.23f})
                {
                    g.Cylinder(new Vector3(x+dx,1.132f,-3.09f),.062f,.22f,4);
                    g.Box(new Vector3(x+dx,1.377f,-3.09f),new Vector3(.037f,.07f,.039f),1);
                    g.Box(new Vector3(x+dx+.025f,1.408f,-3.09f),new Vector3(.085f,.027f,.040f),0);
                }
                g.Box(new Vector3(x,1.148f,-2.925f),new Vector3(.11f,.035f,.19f),1);
                g.Box(new Vector3(x,1.161f,-2.827f),new Vector3(.105f,.021f,.041f),2);
                // Small stylized glints distinguish mirror from a blank painted inset.
                g.Beam(new Vector3(x-.31f,2.10f,-3.23f),new Vector3(x-.12f,2.33f,-3.23f),.025f,0);
                g.Beam(new Vector3(x-.21f,1.97f,-3.23f),new Vector3(x+.01f,2.24f,-3.23f),.017f,0);
            }
            // Restrained interior tile rhythm; it never spills onto the public pavement.
            for(int x=0;x<9;x++)for(int z=0;z<6;z++)
                g.Box(new Vector3(-2.04f+x*.51f,.068f,-.52f-z*.50f),new Vector3(.502f,.005f,.492f),(x+z)%2==0?0:1);
            var face=room.Find("Sign face Barber");
            foreach(float dx in new[]{-.36f,.36f})g.Box(new Vector3(face.localPosition.x+face.localScale.x*dx,2.96f,.50f),new Vector3(.055f,.95f,.065f),2);
            var go=new GameObject(RootName);go.transform.SetParent(room,false);go.isStatic=true;
            var mesh=g.Build();int vertices=mesh.vertexCount;go.AddComponent<MeshFilter>().sharedMesh=Save(mesh,Folder+"/BarberFittings.asset");
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=Palette();renderer.shadowCastingMode=ShadowCastingMode.Off;
            AirborneByDesign.Attach(go,"Chair accessories attach to retained seats/bases; mirror ledges anchor to rear wall; tools/towels rest on ledges; glazing and fascia supports attach to existing walls.");
            var glass=GameObject.CreatePrimitive(PrimitiveType.Cube);glass.name="Barber lower glazing";glass.transform.SetParent(go.transform,false);
            glass.transform.localPosition=new Vector3(0,.61f,.21f);glass.transform.localScale=new Vector3(4.82f,.61f,.008f);
            Object.DestroyImmediate(glass.GetComponent<Collider>());glass.isStatic=true;
            var glassRenderer=glass.GetComponent<MeshRenderer>();glassRenderer.sharedMaterial=room.Find("Glazed private shop boundary").GetComponent<MeshRenderer>().sharedMaterial;glassRenderer.shadowCastingMode=ShadowCastingMode.Off;
            if(solids.Count!=map.GetComponentsInChildren<Collider>(true).Length||solids.Any(p=>p.Key==null||p.Key.bounds!=p.Value))throw new InvalidOperationException("Barber changed private boundary/gameplay collision.");
            report.AppendLine("Barber only: retained chairs face mirrors, supported head/arm/footrests, grounded bases, work ledges/tools/towels, restrained interior tiles and lower glazing. "+vertices+" solid vertices plus glass panel; two renderers, no new collider/shadow caster. All original collision preserved.");
        }
        private static Material Palette()
        {
            var texture=new Texture2D(8,1,TextureFormat.RGBA32,false);texture.SetPixels(Colours);texture.Apply();string path=Folder+"/BarberPalette.png";File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
            var i=(TextureImporter)AssetImporter.GetAtPath(path);i.npotScale=TextureImporterNPOTScale.None;i.filterMode=FilterMode.Point;i.mipmapEnabled=false;i.wrapMode=TextureWrapMode.Clamp;i.textureCompression=TextureImporterCompression.Uncompressed;i.SaveAndReimport();
            var m=new Material(Shader.Find("Standard")){color=Color.white,mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path)};m.SetFloat("_Glossiness",.10f);return Save(m,Folder+"/BarberFittings.mat");
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
            public void Cylinder(Vector3 at,float radius,float height,int colour)
            {
                Vector3 P(int i,float y){float a=i*Mathf.PI/4;return at+new Vector3(Mathf.Cos(a)*radius,y,Mathf.Sin(a)*radius);}
                for(int i=0;i<8;i++)
                {Quad(P(i+1,0),P(i,0),P(i,height),P(i+1,height),colour);Quad(at+Vector3.up*height,P(i+1,height),P(i,height),at+Vector3.up*height,colour);}
            }
            public Mesh Build(){var m=new Mesh{name="Fitted barber chair and mirror workstations"};m.SetVertices(points);m.SetUVs(0,uv);m.SetTriangles(indices,0);m.RecalculateNormals();m.RecalculateBounds();return m;}
        }
    }
}
