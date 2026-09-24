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
    /// <summary>The bakery's stepped bread showcase, separate from neighbouring services.</summary>
    public static class IlalimBakeryAuthor
    {
        public const string RootName="Bakery bread showcase";
        private const string Folder="Assets/TumbangPreso/Art/IlalimBakery";
        private static readonly Color[] Colours={new Color(.77f,.76f,.67f),new Color(.28f,.31f,.29f),
            new Color(.58f,.61f,.59f),new Color(.80f,.54f,.23f),new Color(.92f,.69f,.34f),
            new Color(.60f,.32f,.13f),new Color(.55f,.22f,.15f),new Color(.92f,.80f,.54f)};
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/IlalimNgTulay.unity");var report=new StringBuilder();
            FinishLoadedScene(report);IlalimShopSignAuthor.FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/ilalim-bakery");File.WriteAllText("Logs/ilalim-bakery/author.txt",report.ToString());Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            var map=GameObject.Find("IlalimNgTulay");var solids=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            var room=map.transform.Find("Dressing/PlaceRework/Frontage_Bakery");if(room==null)throw new InvalidOperationException("Missing Bakery frontage.");
            var prior=room.Find(RootName);if(prior!=null)Object.DestroyImmediate(prior.gameObject);
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            foreach(var r in room.GetComponentsInChildren<MeshRenderer>())
                if(new[]{"Bread loaf","Food display tray","Boxed shop stock","Glazed private shop boundary","Window frame"}.Contains(r.name))r.enabled=false;
            var g=new Geometry();var glass=new Geometry();
            // Fresh painted lower wall and inset panels remain part of the existing solid boundary.
            g.Box(new Vector3(0,.51f,.209f),new Vector3(4.89f,.81f,.017f),6);
            foreach(float x in new[]{-1.63f,0,1.63f})
            {
                g.Box(new Vector3(x,.52f,.221f),new Vector3(1.48f,.63f,.018f),0);
                g.Box(new Vector3(x,.52f,.234f),new Vector3(1.34f,.49f,.01f),6);
            }
            // Three shallow stepped tiers, borne by the retained counter and slim side posts.
            foreach(float x in new[]{-1.97f,1.97f})
            {
                foreach(float z in new[]{-.52f,-1.46f})g.Box(new Vector3(x,1.68f,z),new Vector3(.045f,1.13f,.045f),0);
                glass.Box(new Vector3(x,1.69f,-.99f),new Vector3(.008f,1.07f,.92f),0);
            }
            foreach(float y in new[]{1.14f,2.23f})foreach(float z in new[]{-.52f,-1.46f})
                g.Box(new Vector3(0,y,z),new Vector3(3.98f,.035f,.035f),0);
            glass.Box(new Vector3(0,1.69f,-.512f),new Vector3(3.90f,1.05f,.007f),0);
            glass.Box(new Vector3(0,2.231f,-.99f),new Vector3(3.9f,.007f,.94f),0);
            for(int tier=0;tier<3;tier++)
            {
                float y=1.16f+tier*.35f,z=-.80f-tier*.19f;
                foreach(float x in new[]{-1.28f,0,1.28f})
                {
                    g.Box(new Vector3(x,y,z),new Vector3(1.18f,.027f,.46f),2);
                    foreach(float side in new[]{-1f,1f})g.Box(new Vector3(x+side*.588f,y+.025f,z),new Vector3(.02f,.05f,.46f),2);
                    g.Box(new Vector3(x,y+.025f,z+.225f),new Vector3(1.18f,.05f,.018f),2);
                    // Round pandesal below; oval rolls in the middle; wider split loaves above.
                    int count=tier==2?2:3;
                    for(int i=0;i<count;i++)
                    {
                        float bx=x+(i-(count-1)*.5f)*(tier==2?.51f:.35f);
                        var at=new Vector3(bx,y+.03f,z);var size=tier==2?new Vector3(.42f,.24f,.33f):new Vector3(.29f,.19f,.31f);
                        g.Bread(at,size,tier==1?4:3);
                        if(tier>0)g.Bread(at+new Vector3(0,size.y*.94f,0),new Vector3(size.x*.67f,.015f,.047f),7);
                        if(tier==0)g.Bread(at+new Vector3(.035f,size.y*.90f,.04f),new Vector3(.11f,.017f,.095f),4);
                    }
                }
                foreach(float x in new[]{-1.91f,1.91f})g.Box(new Vector3(x,y-.025f,z),new Vector3(.055f,.04f,.51f),1);
            }
            // Bread stacks and a few bagged loaves replace the generic multicolour boxes.
            for(int tier=0;tier<2;tier++)for(int col=0;col<4;col++)
            {
                float x=(col-1.5f)*1.02f,y=1.065f+tier*.62f;
                g.Box(new Vector3(x,y+.014f,-3.28f),new Vector3(.78f,.027f,.35f),2);
                for(int row=0;row<2;row++)g.Bread(new Vector3(x+(row-.5f)*.32f,y+.03f,-3.28f),new Vector3(.29f,.20f,.30f),3+col%2);
                if(tier==1)
                {
                    glass.Box(new Vector3(x,y+.20f,-3.28f),new Vector3(.75f,.36f,.34f),0);
                    g.Box(new Vector3(x,y+.39f,-3.28f),new Vector3(.13f,.035f,.09f),6);
                }
            }
            // The fascia retains its real timber backing; two brackets visibly support it.
            var face=room.Find("Sign face Bakery");
            foreach(float dx in new[]{-.35f,.35f})g.Box(new Vector3(face.localPosition.x+face.localScale.x*dx,2.96f,.505f),new Vector3(.06f,.94f,.065f),1);
            var root=new GameObject(RootName);root.transform.SetParent(room,false);root.isStatic=true;
            Add(root.transform,"Tiered metal showcase and bread",g,Palette(),"BreadDisplay.asset");
            var glaze=new Material(Shader.Find("Standard")){color=new Color(.76f,.86f,.85f,.045f)};
            glaze.SetFloat("_Glossiness",.28f);glaze.SetFloat("_Mode",2);glaze.SetOverrideTag("RenderType","Transparent");
            glaze.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);glaze.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);glaze.SetInt("_ZWrite",0);glaze.EnableKeyword("_ALPHABLEND_ON");glaze.renderQueue=3000;
            Add(root.transform,"Clear showcase glazing and bread bags",glass,Save(glaze,Folder+"/Glazing.mat"),"DisplayGlazing.asset");
            AirborneByDesign.Attach(root,"Showcase posts/trays stand on retained service counter; bagged bread sits on retained back shelves; facade panels/brackets attach to retained wall and slab.");
            if(solids.Count!=map.GetComponentsInChildren<Collider>(true).Length||solids.Any(p=>p.Key==null||p.Key.bounds!=p.Value))throw new InvalidOperationException("Bakery changed private boundary/gameplay collision.");
            report.AppendLine("Bakery only: three bread tiers, round/oval/split loaves, back-shelf bread bags, slim glazed case and fitted painted panels. "+g.Count+" opaque and "+glass.Count+" glass vertices, two renderers/materials, no new collision or shadow casters.");
        }
        private static void Add(Transform root,string name,Geometry geometry,Material material,string file)
        {var go=new GameObject(name);go.transform.SetParent(root,false);go.isStatic=true;go.AddComponent<MeshFilter>().sharedMesh=Save(geometry.Build(),Folder+"/"+file);var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;}
        private static Material Palette()
        {
            var texture=new Texture2D(8,1,TextureFormat.RGBA32,false);texture.SetPixels(Colours);texture.Apply();string path=Folder+"/BreadPalette.png";File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
            var i=(TextureImporter)AssetImporter.GetAtPath(path);i.npotScale=TextureImporterNPOTScale.None;i.filterMode=FilterMode.Point;i.mipmapEnabled=false;i.wrapMode=TextureWrapMode.Clamp;i.textureCompression=TextureImporterCompression.Uncompressed;i.SaveAndReimport();
            var m=new Material(Shader.Find("Standard")){color=Color.white,mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path)};m.SetFloat("_Glossiness",.08f);return Save(m,Folder+"/BreadDisplay.mat");
        }
        private static T Save<T>(T draft,string path) where T:Object
        {var saved=AssetDatabase.LoadAssetAtPath<T>(path);if(saved==null){AssetDatabase.CreateAsset(draft,path);return draft;}EditorUtility.CopySerialized(draft,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(draft);return saved;}
        private sealed class Geometry
        {
            private readonly List<Vector3> points=new List<Vector3>();private readonly List<Vector2> uv=new List<Vector2>();private readonly List<int> indices=new List<int>();
            public int Count=>points.Count;
            private void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,int colour)
            {int n=points.Count;points.AddRange(new[]{a,b,c,d});indices.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});for(int i=0;i<4;i++)uv.Add(new Vector2((colour+.5f)/8,.5f));}
            public void Box(Vector3 at,Vector3 size,int colour)
            {
                var h=size*.5f;Vector3 P(float x,float y,float z)=>at+Vector3.Scale(h,new Vector3(x,y,z));
                Quad(P(-1,-1,1),P(1,-1,1),P(1,1,1),P(-1,1,1),colour);Quad(P(1,-1,-1),P(-1,-1,-1),P(-1,1,-1),P(1,1,-1),colour);
                Quad(P(-1,-1,-1),P(-1,-1,1),P(-1,1,1),P(-1,1,-1),colour);Quad(P(1,-1,1),P(1,-1,-1),P(1,1,-1),P(1,1,1),colour);
                Quad(P(-1,1,-1),P(-1,1,1),P(1,1,1),P(1,1,-1),colour);Quad(P(-1,-1,1),P(-1,-1,-1),P(1,-1,-1),P(1,-1,1),colour);
            }
            public void Bread(Vector3 at,Vector3 size,int colour)
            {
                // Flattened, slightly bulging loaf, using broad facets instead of noisy crumbs.
                float[] radii={.77f,1,.88f,.49f,0};float[] heights={0,.24f,.60f,.89f,1};
                Vector3 P(int ring,int angle){float a=angle*Mathf.PI/5;return at+new Vector3(Mathf.Cos(a)*radii[ring]*size.x*.5f,heights[ring]*size.y,Mathf.Sin(a)*radii[ring]*size.z*.5f);}
                for(int ring=0;ring<4;ring++)for(int i=0;i<10;i++)Quad(P(ring,i+1),P(ring,i),P(ring+1,i),P(ring+1,i+1),ring==0?5:colour);
            }
            public Mesh Build(){var m=new Mesh{name="Bakery fitted showcase"};m.SetVertices(points);m.SetUVs(0,uv);m.SetTriangles(indices,0);m.RecalculateNormals();m.RecalculateBounds();return m;}
        }
    }
}
