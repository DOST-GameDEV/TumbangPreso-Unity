using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>Six residential roof neighbours on measured vacant SaBubong plots.</summary>
    public static class RooftopNeighborsAuthor
    {
        private const string Folder="Assets/TumbangPreso/Art/SaBubong/Neighbors", RootName="Inhabited neighbor roofs";
        [Serializable] private sealed class Site { public string name,use;public float x,z,height; }
        [Serializable] private sealed class Plan { public float baseY,width,depth;public Site[] sites; }
        private static readonly Color32[] Colours={
            new Color32(179,167,139,255),new Color32(153,116,94,255),new Color32(143,155,148,255),
            new Color32(195,185,157,255),new Color32(116,112,99,255),new Color32(55,73,73,255),
            new Color32(111,144,147,255),new Color32(87,106,90,255),new Color32(96,78,56,255),
            new Color32(162,171,167,255),new Color32(85,117,127,255),new Color32(117,138,79,255),
            new Color32(65,92,52,255),new Color32(162,109,77,255),new Color32(227,213,179,255),
            new Color32(187,139,88,255)};
        private sealed class Geometry
        {
            private readonly List<Vector3> vertices=new List<Vector3>();
            private readonly List<Vector2> uv=new List<Vector2>();
            private readonly List<Color> roles=new List<Color>();
            private readonly List<int> triangles=new List<int>();
            private static readonly int[] SurfaceRoles={1,1,1,2,2,0,7,5,4,8,16,15,15,10,9,9};
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,int colour,int role=-1)
            {
                int n=vertices.Count;vertices.AddRange(new[]{a,b,c,d});
                for(int i=0;i<4;i++){uv.Add(new Vector2((colour+.5f)/16,.5f));roles.Add(new Color((role<0?SurfaceRoles[colour]:role)/32f,0,0,1));}
                triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
            }
            public void Box(Vector3 at,Vector3 size,int colour,int role=-1)
            {
                var a=at-size*.5f;var b=at+size*.5f;
                Quad(new Vector3(a.x,a.y,b.z),new Vector3(b.x,a.y,b.z),new Vector3(b.x,b.y,b.z),new Vector3(a.x,b.y,b.z),colour,role);
                Quad(new Vector3(b.x,a.y,a.z),new Vector3(a.x,a.y,a.z),new Vector3(a.x,b.y,a.z),new Vector3(b.x,b.y,a.z),colour,role);
                Quad(new Vector3(a.x,a.y,a.z),new Vector3(a.x,a.y,b.z),new Vector3(a.x,b.y,b.z),new Vector3(a.x,b.y,a.z),colour,role);
                Quad(new Vector3(b.x,a.y,b.z),new Vector3(b.x,a.y,a.z),new Vector3(b.x,b.y,a.z),new Vector3(b.x,b.y,b.z),colour,role);
                Quad(new Vector3(a.x,b.y,a.z),new Vector3(a.x,b.y,b.z),new Vector3(b.x,b.y,b.z),new Vector3(b.x,b.y,a.z),colour,role);
                Quad(new Vector3(a.x,a.y,b.z),new Vector3(a.x,a.y,a.z),new Vector3(b.x,a.y,a.z),new Vector3(b.x,a.y,b.z),colour,role);
            }
            public void Cylinder(Vector3 bottom,float radius,float height,int colour,int sides=12)
            {
                for(int i=0;i<sides;i++)
                {
                    float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;
                    var p=bottom+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);
                    var q=bottom+new Vector3(Mathf.Cos(b)*radius,0,Mathf.Sin(b)*radius);
                    var up=Vector3.up*height;
                    Quad(q,p,p+up,q+up,colour);Quad(bottom+up,q+up,p+up,bottom+up,colour);
                }
            }
            public Mesh Mesh(string name)
            {
                var m=new Mesh{name=name};m.SetVertices(vertices);m.SetUVs(0,uv);m.SetColors(roles);m.SetTriangles(triangles,0);
                m.RecalculateNormals();m.RecalculateBounds();return m;
            }
        }
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene(SaBubongBuilder.ScenePath);var report=new StringBuilder();FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/rooftop-neighbors");File.WriteAllText("Logs/rooftop-neighbors/author.txt",report.ToString());
            Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            var map=GameObject.Find("SaBubong");var city=map.transform.Find("Dressing/Metro rooftops");
            if(city==null)throw new InvalidOperationException("Missing retained city.");
            var solids=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            var old=city.Find(RootName);if(old!=null)Object.DestroyImmediate(old.gameObject);
            var occupied=new List<Bounds>{new Bounds(new Vector3(0,-13,0),new Vector3(39,26,45))};
            foreach(Transform child in city)
            {
                if(child.name.StartsWith("CityBlock_")||child.name.StartsWith("Residential skyline")||child.name.StartsWith("CityGarden_"))
                    occupied.Add(DrawnBounds(child));
                if(child.name=="Street frontages")foreach(Transform house in child)occupied.Add(DrawnBounds(house));
            }
            var plan=JsonUtility.FromJson<Plan>(File.ReadAllText("MapSource/environment/layouts/sabubong-neighbor-roofs-20260924.json"));
            if(plan.sites.Length!=6)throw new InvalidOperationException("Expected the six individually planned sites.");
            foreach(var site in plan.sites)
            {
                var b=new Bounds(new Vector3(site.x,plan.baseY+site.height*.5f,site.z),new Vector3(plan.width,site.height,plan.depth));
                if(new[]{-52f,-23.5f,23.5f,52f}.Any(x=>b.min.x<x+2.85f&&b.max.x>x-2.85f)||
                    new[]{-52f,-26.5f,26.5f,52f}.Any(z=>b.min.z<z+2.85f&&b.max.z>z-2.85f))
                    throw new InvalidOperationException(site.name+" crosses an existing road.");
                if(occupied.Any(o=>Overlaps(b,o,.7f)))throw new InvalidOperationException(site.name+" overlaps retained construction/planting.");
                occupied.Add(b);
            }
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();var material=Palette();
            var root=new GameObject(RootName).transform;root.SetParent(city,false);int vertices=0;
            for(int i=0;i<plan.sites.Length;i++)
            {
                var site=plan.sites[i];var g=new Geometry();float h=site.height;
                Building(g,plan.width,plan.depth,h,i%3);
                // Each roof keeps open circulation from the stairhead to its selected use.
                Access(g,h,i%3);
                switch(site.use)
                {
                    case "shade":Shade(g,new Vector3(-2,h,-.5f),i==0?7:13);break;
                    case "water-laundry":Tank(g,new Vector3(-3.7f,h,2.8f));Laundry(g,new Vector3(-.6f,h,-2));break;
                    case "garden":
                        foreach(var p in new[]{new Vector3(-3.7f,h,-3),new Vector3(-3.7f,h,.1f),new Vector3(-3.7f,h,3)})Plant(g,p);
                        Bench(g,new Vector3(-1,h,2));break;
                    case "laundry":Laundry(g,new Vector3(-2.7f,h,-1.3f));Laundry(g,new Vector3(.3f,h,-1.3f));break;
                    case "service":
                        g.Box(new Vector3(-3.4f,h+.25f,2.6f),new Vector3(1.3f,.5f,1.3f),3);
                        g.Box(new Vector3(-3.4f,h+.9f,2.6f),new Vector3(.8f,.8f,.8f),9);
                        g.Box(new Vector3(-3.4f,h+1.34f,2.6f),new Vector3(1.15f,.12f,1.15f),4);break;
                    default:throw new InvalidOperationException("Unplanned roof use "+site.use);
                }
                var mesh=g.Mesh(site.name);vertices+=mesh.vertexCount;
                if(mesh.bounds.min.y<-.01f||mesh.bounds.min.x<-plan.width*.5f||mesh.bounds.max.x>plan.width*.5f||
                    mesh.bounds.min.z<-plan.depth*.5f||mesh.bounds.max.z>plan.depth*.5f)
                    throw new InvalidOperationException(site.name+" exceeds its measured plot.");
                string path=Folder+"/RoofNeighbor"+i+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}else{EditorUtility.CopySerialized(mesh,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(mesh);}
                var go=new GameObject(site.name);go.transform.SetParent(root,false);go.transform.localPosition=new Vector3(site.x,plan.baseY,site.z);
                go.transform.localRotation=Quaternion.Euler(0,site.z<0?180:0,0);go.isStatic=true;
                go.AddComponent<MeshFilter>().sharedMesh=saved;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=material;
                r.shadowCastingMode=ShadowCastingMode.Off;AirborneByDesign.Attach(go,"Supported distant residential scenery outside the playable roof.");
                report.AppendLine(site.name+": "+site.use+", roofY="+(plan.baseY+h)+", vertices="+saved.vertexCount);
            }
            if(solids.Count!=map.GetComponentsInChildren<Collider>(true).Length||solids.Any(p=>p.Key==null||p.Key.bounds!=p.Value))
                throw new InvalidOperationException("Roof neighbors changed original collision.");
            report.AppendLine($"Six roofs, {vertices}vertices, six renderers/one palette, no new collision or shadow casters. Retained footprints and street clearance verified.");
        }
        private static void Building(Geometry g,float width,float depth,float height,int wall)
        {
            float w=width-.4f,d=depth-.4f;
            g.Box(new Vector3(0,height*.5f,0),new Vector3(w,height,d),wall);
            g.Box(new Vector3(0,.42f,0),new Vector3(w+.2f,.84f,d+.2f),4);
            g.Box(new Vector3(0,height-.12f,0),new Vector3(width,.24f,depth),3);
            g.Box(new Vector3(0,height+.015f,0),new Vector3(w-.4f,.03f,d-.4f),4,13);
            foreach(float side in new[]{-1f,1f})
            {
                g.Box(new Vector3(0,height+.46f,side*(depth*.5f-.17f)),new Vector3(width,.92f,.32f),wall);
                g.Box(new Vector3(side*(width*.5f-.17f),height+.46f,0),new Vector3(.32f,.92f,depth-.64f),wall);
                g.Box(new Vector3(0,height+.95f,side*(depth*.5f-.17f)),new Vector3(width,.10f,.34f),3);
                g.Box(new Vector3(side*(width*.5f-.17f),height+.95f,0),new Vector3(.34f,.10f,depth-.68f),3);
            }
            for(float y=3.3f;y<height-1.3f;y+=3.15f)
            {
                foreach(float side in new[]{-1f,1f})foreach(float x in new[]{-3.45f,0,3.45f})
                {
                    g.Box(new Vector3(x,y,side*(d*.5f+.012f)),new Vector3(2.25f,1.5f,.024f),5);
                    g.Box(new Vector3(x,y+.03f,side*(d*.5f+.030f)),new Vector3(1.98f,1.22f,.025f),6);
                    g.Box(new Vector3(x,y,side*(d*.5f+.045f)),new Vector3(.12f,1.5f,.025f),3);
                    g.Box(new Vector3(x,y+.88f,side*(d*.5f+.09f)),new Vector3(2.55f,.16f,.18f),3);
                }
                foreach(float side in new[]{-1f,1f})foreach(float z in new[]{-2.8f,2.8f})
                {
                    g.Box(new Vector3(side*(w*.5f+.015f),y,z),new Vector3(.03f,1.55f,2.2f),5);
                    g.Box(new Vector3(side*(w*.5f+.04f),y,z),new Vector3(.03f,1.28f,1.92f),6);
                    g.Box(new Vector3(side*(w*.5f+.09f),y+.86f,z),new Vector3(.18f,.16f,2.45f),3);
                }
            }
            g.Box(new Vector3(0,1.15f,-d*.5f-.025f),new Vector3(1.55f,2.3f,.05f),5);
            g.Box(new Vector3(0,2.52f,-d*.5f-.1f),new Vector3(2,.18f,.2f),3);
        }
        private static void Access(Geometry g,float h,int wall)
        {
            g.Box(new Vector3(2.8f,h+1.45f,2.55f),new Vector3(3.1f,2.9f,3),wall);
            g.Box(new Vector3(2.8f,h+2.97f,2.55f),new Vector3(3.4f,.18f,3.3f),3);
            g.Box(new Vector3(2.4f,h+1.08f,1.035f),new Vector3(1.1f,2.16f,.035f),8);
            g.Box(new Vector3(2.4f,h+.12f,.83f),new Vector3(1.5f,.24f,.4f),3);
            for(int i=0;i<3;i++)g.Box(new Vector3(3.7f,h+2.15f+i*.16f,1.025f),new Vector3(.7f,.09f,.07f),5);
        }
        private static void Shade(Geometry g,Vector3 p,int colour)
        {
            foreach(float x in new[]{-2f,2f})foreach(float z in new[]{-2f,2f})g.Box(p+new Vector3(x,1.4f,z),new Vector3(.18f,2.8f,.18f),8);
            foreach(float x in new[]{-2f,2f})g.Box(p+new Vector3(x,2.72f,0),new Vector3(.2f,.2f,4.35f),8);
            foreach(float z in new[]{-2f,-1f,0,1f,2f})g.Box(p+new Vector3(0,2.83f,z),new Vector3(4.45f,.12f,.12f),8);
            g.Box(p+new Vector3(0,2.96f,0),new Vector3(4.6f,.16f,4.6f),colour,5);
            g.Box(p+new Vector3(0,.8f,0),new Vector3(1.8f,.16f,1.05f),8);
            foreach(float x in new[]{-.65f,.65f})g.Box(p+new Vector3(x,.38f,0),new Vector3(.15f,.76f,.65f),8);
            Bench(g,p+new Vector3(0,0,-1.3f));
        }
        private static void Bench(Geometry g,Vector3 p)
        {
            g.Box(p+Vector3.up*.51f,new Vector3(1.9f,.14f,.5f),8);
            foreach(float x in new[]{-.65f,.65f})g.Box(p+new Vector3(x,.25f,0),new Vector3(.17f,.5f,.4f),3);
        }
        private static void Tank(Geometry g,Vector3 p)
        {
            g.Box(p+Vector3.up*.2f,new Vector3(1.8f,.4f,1.8f),3);
            g.Cylinder(p+Vector3.up*.4f,.72f,1.7f,10);
            g.Cylinder(p+Vector3.up*2.1f,.75f,.15f,10);g.Cylinder(p+Vector3.up*2.25f,.22f,.13f,9);
            foreach(float y in new[]{.75f,1.5f})g.Cylinder(p+Vector3.up*y,.74f,.07f,9);
            g.Box(p+new Vector3(.76f,.48f,0),new Vector3(.09f,.75f,.09f),9);
        }
        private static void Laundry(Geometry g,Vector3 p)
        {
            foreach(float z in new[]{-1.65f,1.65f})
            {g.Box(p+new Vector3(0,1.2f,z),new Vector3(.10f,2.4f,.10f),9);g.Box(p+new Vector3(0,2.35f,z),new Vector3(1.4f,.08f,.08f),9);}
            foreach(float x in new[]{-.48f,.48f})g.Box(p+new Vector3(x,2.32f,0),new Vector3(.022f,.022f,3.3f),5);
            for(int i=0;i<4;i++)g.Box(p+new Vector3(i%2==0?-.48f:.48f,1.86f,-1.05f+i*.7f),new Vector3(.045f,.92f,.55f),i%2==0?14:15);
            g.Cylinder(p+new Vector3(.85f,0,1.3f),.32f,.46f,10);
        }
        private static void Plant(Geometry g,Vector3 p)
        {
            g.Box(p+Vector3.up*.35f,new Vector3(1.25f,.7f,1.75f),13);
            g.Box(p+Vector3.up*.72f,new Vector3(1.08f,.06f,1.58f),8);
            g.Cylinder(p+Vector3.up*.75f,.07f,.8f,8,6);
            g.Cylinder(p+Vector3.up*1.3f,.73f,.7f,12,7);
            g.Cylinder(p+Vector3.up*1.92f,.53f,.3f,11,7);
        }
        private static bool Overlaps(Bounds a,Bounds b,float gap)=>a.min.x-gap<b.max.x&&a.max.x+gap>b.min.x&&a.min.z-gap<b.max.z&&a.max.z+gap>b.min.z;
        private static Bounds DrawnBounds(Transform root)
        {var rows=root.GetComponentsInChildren<Renderer>();if(rows.Length==0)throw new InvalidOperationException("Empty retained body "+root.name);var b=rows[0].bounds;foreach(var r in rows)b.Encapsulate(r.bounds);return b;}
        private static Material Palette()
        {
            string texPath=Folder+"/RoofNeighbors.png";var tex=new Texture2D(16,1,TextureFormat.RGBA32,false);tex.SetPixels32(Colours);tex.Apply();
            File.WriteAllBytes(texPath,tex.EncodeToPNG());Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(texPath);
            var importer=(TextureImporter)AssetImporter.GetAtPath(texPath);importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
            string path=Folder+"/RoofNeighbors.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            var source=new Material(Shader.Find("Standard")){color=Color.white,mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(texPath)};
            var draft=Visual.NearFade.CopySurfaceForAuthoring(source);Object.DestroyImmediate(source);
            // Author roles per part. These six roofs use mineral plaster, cast supports,
            // sheet roofing, timber, glass, cloth and plastic; no all-purpose grunge.
            draft.SetFloat("_SurfaceKind",1);draft.SetFloat("_SurfaceVertexRoles",1);draft.SetFloat("_SurfaceCoordinates",0);
            draft.SetFloat("_SurfaceScale",1);draft.SetFloat("_SurfaceStrength",1);draft.SetFloat("_SurfaceBaseY",-26.049f);
            draft.SetFloat("_DeckSurface",1);draft.SetFloat("_Glossiness",.12f);
            if(m==null){AssetDatabase.CreateAsset(draft,path);m=draft;}else{EditorUtility.CopySerialized(draft,m);Object.DestroyImmediate(draft);}
            EditorUtility.SetDirty(m);return m;
        }
    }
}
