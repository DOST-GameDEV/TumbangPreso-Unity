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
    /// <summary>A real painted tire sign and contained tire-service workshop.</summary>
    public static class IlalimVulcanizingAuthor
    {
        public const string RootName="Vulcanizing tire sign and workshop";
        private const string Folder="Assets/TumbangPreso/Art/IlalimVulcanizing";
        private static readonly Color[] Colours={new Color(.74f,.73f,.66f),new Color(.105f,.115f,.12f),
            new Color(.46f,.50f,.49f),new Color(.82f,.79f,.66f),new Color(.26f,.39f,.46f),
            new Color(.59f,.62f,.59f),new Color(.51f,.24f,.19f),new Color(.60f,.56f,.43f)};
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/IlalimNgTulay.unity");var report=new StringBuilder();FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/ilalim-vulcanizing");File.WriteAllText("Logs/ilalim-vulcanizing/author.txt",report.ToString());Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            var map=GameObject.Find("IlalimNgTulay");var solids=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            var room=map.transform.Find("Dressing/PlaceRework/Frontage_Hardware");if(room==null)throw new InvalidOperationException("Missing vulcanizing frontage.");
            var prior=room.Find(RootName);if(prior!=null)Object.DestroyImmediate(prior.gameObject);
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            foreach(var r in room.GetComponentsInChildren<MeshRenderer>())
                if(new[]{"Boxed shop stock","Service counter body","Service counter top","Back shelf","Shopfront lower wall","Glazed private shop boundary","Window frame","Sign face Hardware","Shop sign backing Hardware"}.Contains(r.name))r.enabled=false;
            var g=new Geometry();
            // Contained workshop screen remains a visible private boundary, not an open route.
            foreach(float y in new[]{.30f,.62f,.94f})g.Box(new Vector3(0,y,.18f),new Vector3(4.08f,.048f,.045f),2);
            for(int i=0;i<9;i++)g.Box(new Vector3(-1.96f+i*.49f,.59f,.18f),new Vector3(.028f,.73f,.045f),2);
            foreach(float x in new[]{-1.94f,1.94f})g.Box(new Vector3(x,1.66f,.14f),new Vector3(.043f,1.37f,.052f),2);
            // Three flat stacked tires, actual toroidal openings and broad faceted shoulders.
            for(int i=0;i<3;i++)g.Tire(new Vector3(-1.30f,.165f+i*.20f,-1.28f),.43f,.22f,.20f,Quaternion.Euler(90,0,0));
            // Upright stock sits in a metal rack anchored to the rear wall/floor.
            foreach(float x in new[]{-1.74f,1.74f})g.Box(new Vector3(x,.95f,-3.19f),new Vector3(.058f,1.77f,.06f),2);
            foreach(float y in new[]{1.015f,1.86f})g.Box(new Vector3(0,y,-3.18f),new Vector3(3.51f,.05f,.075f),2);
            foreach(float x in new[]{-1.04f,0,1.04f})g.Tire(new Vector3(x,1.365f,-3.05f),.325f,.18f,.15f,Quaternion.identity);
            // A repair wheel is cradled on a low, supported stand, leaving the floor orderly.
            foreach(float x in new[]{.30f,1.05f})foreach(float z in new[]{-1.06f,-1.64f})g.Box(new Vector3(x,.23f,z),new Vector3(.075f,.33f,.075f),7);
            g.Box(new Vector3(.675f,.415f,-1.35f),new Vector3(.98f,.075f,.76f),7);
            g.Tire(new Vector3(.675f,.835f,-1.35f),.38f,.19f,.19f,Quaternion.identity);
            foreach(float x in new[]{.34f,1.01f})g.Box(new Vector3(x,.51f,-1.35f),new Vector3(.12f,.15f,.30f),2);
            // Compact compressor tank, motor, gauge and hose mounted inside this work bay.
            var compressor=new Vector3(.85f,.36f,-2.35f);
            g.Profile(compressor,new[]{0f,.16f,.21f,.21f,.16f,0f},new[]{-.51f,-.49f,-.38f,.38f,.49f,.51f},6,Quaternion.Euler(0,90,0));
            foreach(float x in new[]{-.32f,.32f})g.Box(compressor+new Vector3(x,-.245f,0),new Vector3(.12f,.17f,.31f),1);
            g.Box(compressor+new Vector3(0,.30f,0),new Vector3(.48f,.24f,.29f),2);
            for(int fin=0;fin<5;fin++)g.Box(compressor+new Vector3(-.18f+fin*.09f,.43f,0),new Vector3(.045f,.07f,.30f),1);
            g.Profile(compressor+new Vector3(.27f,.28f,.17f),new[]{0f,.082f,.082f,0f},new[]{0f,0f,.026f,.026f},0,Quaternion.identity);
            g.Beam(compressor+new Vector3(.27f,.28f,.201f),compressor+new Vector3(.295f,.32f,.201f),.008f,1);
            for(int i=0;i<16;i++)
            {
                float a=i*Mathf.PI/8,b=(i+1)*Mathf.PI/8;
                g.Beam(new Vector3(1.58f+Mathf.Cos(a)*.23f,1.60f+Mathf.Sin(a)*.23f,-3.06f),new Vector3(1.58f+Mathf.Cos(b)*.23f,1.60f+Mathf.Sin(b)*.23f,-3.06f),.028f,6);
            }
            g.Beam(compressor+new Vector3(.38f,.20f,0),new Vector3(1.69f,1.38f,-3.05f),.026f,6);
            // Real tire identifier, retained rectangular source board hidden behind this unit.
            var oldFace=room.Find("Sign face Hardware");var signAt=new Vector3(oldFace.localPosition.x,2.95f,.86f);
            g.Tire(signAt,.75f,.33f,.30f,Quaternion.identity);
            foreach(float x in new[]{-.25f,.25f})g.Beam(new Vector3(signAt.x+x,2.87f,.30f),new Vector3(signAt.x+x,3.47f,.70f),.058f,2);
            var go=new GameObject(RootName);go.transform.SetParent(room,false);go.isStatic=true;
            var mesh=g.Build();int vertices=mesh.vertexCount;go.AddComponent<MeshFilter>().sharedMesh=Save(mesh,Folder+"/Workshop.asset");
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=Palette();renderer.shadowCastingMode=ShadowCastingMode.Off;
            AirborneByDesign.Attach(go,"Tire stacks rest on original floor, rack on floor/rear wall, work stand supports tire, compressor stands on feet and hose mounts to rear rack; tire sign is braced into retained roof slab.");
            const string texturePath=Folder+"/Sidewall-v2.png";AssetDatabase.ImportAsset(texturePath);
            var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=1024;importer.alphaSource=TextureImporterAlphaSource.None;
            importer.wrapMode=TextureWrapMode.Clamp;importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            var paint=new Material(Shader.Find("Standard")){color=Color.white,mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath)};paint.SetFloat("_Glossiness",.03f);
            var painted=new GameObject("Painted tire sidewall");painted.transform.SetParent(go.transform,false);painted.isStatic=true;
            painted.AddComponent<MeshFilter>().sharedMesh=Save(PaintedAnnulus(signAt+Vector3.forward*.152f),Folder+"/PaintedSidewall.asset");
            var paintRenderer=painted.AddComponent<MeshRenderer>();paintRenderer.sharedMaterial=Save(paint,Folder+"/PaintedSidewall.mat");paintRenderer.shadowCastingMode=ShadowCastingMode.Off;
            if(solids.Count!=map.GetComponentsInChildren<Collider>(true).Length||solids.Any(p=>p.Key==null||p.Key.bounds!=p.Value))throw new InvalidOperationException("Vulcanizing changed private boundary/gameplay collision.");
            report.AppendLine("Vulcanizing only: real hollow tire/annular paint sign, supported tire stacks/rack/work stand, compact compressor/gauge/hose, visible workshop grille. "+vertices+" body vertices plus256paint-ring vertices; two renderers/materials, no new collider/shadow caster. Original room/collision preserved.");
        }
        private static Mesh PaintedAnnulus(Vector3 at)
        {
            var points=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
            for(int i=0;i<64;i++)
            {
                float a=i*Mathf.PI/32,b=(i+1)*Mathf.PI/32;
                var ring=new[]{new Vector2(Mathf.Cos(a),Mathf.Sin(a))*.351f,new Vector2(Mathf.Cos(a),Mathf.Sin(a))*.694f,new Vector2(Mathf.Cos(b),Mathf.Sin(b))*.694f,new Vector2(Mathf.Cos(b),Mathf.Sin(b))*.351f};
                int n=points.Count;foreach(var p in ring){points.Add(at+new Vector3(p.x,p.y,0));uv.Add(new Vector2(.5f-p.x/1.5f,.5f+p.y/1.5f));}
                tris.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
            }
            // The face looks along local +Z, matching the old sign quad rotated180deg:
            // viewer-right is local-X, so U is reversed while scale stays uniform.
            var mesh=new Mesh{name="Planar painted sidewall with real centre hole"};mesh.SetVertices(points);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        private static Material Palette()
        {
            var texture=new Texture2D(8,1,TextureFormat.RGBA32,false);texture.SetPixels(Colours);texture.Apply();string path=Folder+"/WorkshopPalette.png";File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
            var i=(TextureImporter)AssetImporter.GetAtPath(path);i.npotScale=TextureImporterNPOTScale.None;i.filterMode=FilterMode.Point;i.mipmapEnabled=false;i.wrapMode=TextureWrapMode.Clamp;i.textureCompression=TextureImporterCompression.Uncompressed;i.SaveAndReimport();
            var m=new Material(Shader.Find("Standard")){color=Color.white,mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path)};m.SetFloat("_Glossiness",.10f);return Save(m,Folder+"/Workshop.mat");
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
            public void Profile(Vector3 at,float[] radii,float[] depths,int colour,Quaternion q)
            {
                Vector3 P(int ring,int angle){float a=angle*Mathf.PI/12;return at+q*new Vector3(Mathf.Cos(a)*radii[ring],Mathf.Sin(a)*radii[ring],depths[ring]);}
                for(int ring=0;ring<radii.Length-1;ring++)for(int i=0;i<24;i++)Quad(P(ring,i),P(ring,i+1),P(ring+1,i+1),P(ring+1,i),colour);
            }
            public void Tire(Vector3 at,float outer,float inner,float width,Quaternion q)
            {
                Profile(at,new[]{inner,inner+.02f,outer-.06f,outer,outer,outer-.06f,inner+.02f,inner,inner},
                    new[]{-width*.26f,-width*.5f,-width*.5f,-width*.26f,width*.26f,width*.5f,width*.5f,width*.26f,-width*.26f},1,q);
                // Sparse broad tread blocks follow rubber circumference, no tiny noisy texture.
                for(int i=0;i<24;i++)
                {
                    float angle=i*15f*Mathf.Deg2Rad;var radial=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);
                    Box(at+q*radial*(outer+.003f),new Vector3(outer*.20f,.012f,width*.44f),1,q*Quaternion.Euler(0,0,i*15f-90));
                }
            }
            public Mesh Build(){var m=new Mesh{name="Supported tire-service workshop"};m.SetVertices(points);m.SetUVs(0,uv);m.SetTriangles(indices,0);m.RecalculateNormals();m.RecalculateBounds();return m;}
        }
    }
}
