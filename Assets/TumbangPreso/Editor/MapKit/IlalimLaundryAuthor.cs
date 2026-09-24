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
    /// <summary>Laundry service visibility, supported folded stock and fitted sign mounting.</summary>
    public static class IlalimLaundryAuthor
    {
        public const string RootName="Laundry service fittings";
        private const string Folder="Assets/TumbangPreso/Art/IlalimLaundry";
        private static readonly Color[] Colours={new Color(.74f,.73f,.66f),new Color(.22f,.27f,.27f),
            new Color(.46f,.50f,.49f),new Color(.82f,.79f,.66f),new Color(.26f,.39f,.46f),
            new Color(.59f,.62f,.59f),new Color(.51f,.24f,.19f),new Color(.60f,.56f,.43f)};
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/IlalimNgTulay.unity");var report=new StringBuilder();FinishLoadedScene(report);IlalimShopSignAuthor.FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/ilalim-laundry");File.WriteAllText("Logs/ilalim-laundry/author.txt",report.ToString());Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            var map=GameObject.Find("IlalimNgTulay");var solids=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            var room=map.transform.Find("Dressing/PlaceRework/Frontage_Laundry");if(room==null)throw new InvalidOperationException("Missing Laundry frontage.");
            var prior=room.Find(RootName);if(prior!=null)Object.DestroyImmediate(prior.gameObject);
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            foreach(var r in room.GetComponentsInChildren<MeshRenderer>())
                if(new[]{"Folded laundry","Shopfront lower wall"}.Contains(r.name))r.enabled=false;
            var g=new Geometry();
            // Keep the physical private boundary. A clear lower panel reveals the retained
            // drum openings instead of hiding all three machines behind a retail wall.
            g.Box(new Vector3(0,.22f,.211f),new Vector3(5.68f,.30f,.024f),4);
            foreach(float y in new[]{.38f,.94f})g.Box(new Vector3(0,y,.21f),new Vector3(5.68f,.045f,.05f),5);
            foreach(float x in new[]{-2.8f,-.68f,.68f,2.8f})g.Box(new Vector3(x,.66f,.21f),new Vector3(.045f,.57f,.05f),5);
            // Folded cloth is layered, with a visible returned edge rather than one cube.
            for(int machine=0;machine<3;machine++)
            {
                float x=(machine-1)*1.25f;
                for(int layer=0;layer<4;layer++)
                {
                    float y=1.195f+layer*.058f;int colour=layer%2==0?3:machine==1?4:6;
                    g.Box(new Vector3(x+(layer%2==0?.014f:-.014f),y,-.86f),new Vector3(.65f,.054f,.43f),colour);
                    g.Box(new Vector3(x,y+.004f,-.638f),new Vector3(.52f,.014f,.011f),colour==3?7:3);
                }
                g.Box(new Vector3(x+.27f,.65f,-.396f),new Vector3(.055f,.14f,.057f),1);
            }
            // Separate collected parcels on the existing shelves, plus small detergent stock.
            foreach(float x in new[]{-1.55f,0,1.55f})
            {
                for(int layer=0;layer<3;layer++)g.Box(new Vector3(x,1.095f+layer*.065f,-3.28f),new Vector3(.71f,.06f,.38f),layer%2==0?3:4);
                g.Box(new Vector3(x,1.177f,-3.077f),new Vector3(.13f,.16f,.012f),0);
            }
            foreach(float x in new[]{-.66f,-.22f,.22f,.66f})
            {
                g.Box(new Vector3(x,1.855f,-3.28f),new Vector3(.26f,.35f,.29f),3);
                g.Box(new Vector3(x,2.047f,-3.28f),new Vector3(.115f,.035f,.115f),4);
                g.Box(new Vector3(x,1.85f,-3.127f),new Vector3(.17f,.19f,.009f),4);
            }
            var face=room.Find("Sign face Laundry");
            foreach(float dx in new[]{-.37f,.37f})g.Box(new Vector3(face.localPosition.x+face.localScale.x*dx,2.98f,.50f),new Vector3(.055f,.95f,.065f),2);
            var go=new GameObject(RootName);go.transform.SetParent(room,false);go.isStatic=true;
            var mesh=g.Build();int vertices=mesh.vertexCount;go.AddComponent<MeshFilter>().sharedMesh=Save(mesh,Folder+"/LaundryFittings.asset");
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=Palette();renderer.shadowCastingMode=ShadowCastingMode.Off;
            AirborneByDesign.Attach(go,"Folded fabric sits on retained washer tops, parcels on rear shelves; glazing and fascia supports attach to existing solid frontage.");
            var glass=GameObject.CreatePrimitive(PrimitiveType.Cube);glass.name="Laundry lower glazing";glass.transform.SetParent(go.transform,false);
            glass.transform.localPosition=new Vector3(0,.66f,.21f);glass.transform.localScale=new Vector3(5.64f,.51f,.008f);
            Object.DestroyImmediate(glass.GetComponent<Collider>());glass.isStatic=true;
            var glassRenderer=glass.GetComponent<MeshRenderer>();glassRenderer.sharedMaterial=room.Find("Glazed private shop boundary").GetComponent<MeshRenderer>().sharedMaterial;
            glassRenderer.shadowCastingMode=ShadowCastingMode.Off;
            if(solids.Count!=map.GetComponentsInChildren<Collider>(true).Length||solids.Any(p=>p.Key==null||p.Key.bounds!=p.Value))throw new InvalidOperationException("Laundry changed private boundary/gameplay collision.");
            report.AppendLine("Laundry only: revealed retained washer drums behind lower glazing, folded fabric layers, collected parcels and detergents on retained shelves, fitted fascia brackets. "+vertices+" solid vertices plus one glass panel; two renderers, no new collider/shadow caster. Original boundary unchanged.");
        }
        private static Material Palette()
        {
            var texture=new Texture2D(8,1,TextureFormat.RGBA32,false);texture.SetPixels(Colours);texture.Apply();string path=Folder+"/LaundryPalette.png";File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
            var i=(TextureImporter)AssetImporter.GetAtPath(path);i.npotScale=TextureImporterNPOTScale.None;i.filterMode=FilterMode.Point;i.mipmapEnabled=false;i.wrapMode=TextureWrapMode.Clamp;i.textureCompression=TextureImporterCompression.Uncompressed;i.SaveAndReimport();
            var m=new Material(Shader.Find("Standard")){color=Color.white,mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path)};m.SetFloat("_Glossiness",.10f);return Save(m,Folder+"/LaundryFittings.mat");
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
            public Mesh Build(){var m=new Mesh{name="Laundry folded stock and fitted glazing"};m.SetVertices(points);m.SetUVs(0,uv);m.SetTriangles(indices,0);m.RecalculateNormals();m.RecalculateBounds();return m;}
        }
    }
}
