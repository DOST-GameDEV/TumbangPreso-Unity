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
    /// <summary>Pares cooking and serving equipment on the retained shop counter.</summary>
    public static class IlalimParesAuthor
    {
        public const string RootName="Pares cooking and serving counter";
        private const string Folder="Assets/TumbangPreso/Art/IlalimPares";
        private static readonly Color[] Colours={new Color(.74f,.73f,.66f),new Color(.22f,.27f,.27f),
            new Color(.46f,.50f,.49f),new Color(.82f,.79f,.66f),new Color(.26f,.39f,.46f),
            new Color(.59f,.62f,.59f),new Color(.51f,.24f,.19f),new Color(.60f,.56f,.43f)};
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/IlalimNgTulay.unity");var report=new StringBuilder();FinishLoadedScene(report);IlalimShopSignAuthor.FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/ilalim-pares");File.WriteAllText("Logs/ilalim-pares/author.txt",report.ToString());Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            var map=GameObject.Find("IlalimNgTulay");var solids=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            var room=map.transform.Find("Dressing/PlaceRework/Frontage_Pares");if(room==null)throw new InvalidOperationException("Missing Pares frontage.");
            var prior=room.Find(RootName);if(prior!=null)Object.DestroyImmediate(prior.gameObject);
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            foreach(var r in room.GetComponentsInChildren<MeshRenderer>())
                if(new[]{"Covered serving dish","Food display tray","Boxed shop stock","Glazed private shop boundary","Window frame"}.Contains(r.name))r.enabled=false;
            var g=new Geometry();
            // A large cooking vessel sits to one side, visible beside the retained street cart.
            var pot=new Vector3(-1.50f,1.115f,-1.07f);
            g.Profile(pot,new[]{.30f,.38f,.40f,.40f,.365f,.35f,.30f,0f},new[]{0f,.04f,.54f,.62f,.62f,.08f,.045f,.045f},2);
            foreach(float side in new[]{-1f,1f})
            {
                g.Box(pot+new Vector3(side*.46f,.46f,0),new Vector3(.22f,.04f,.045f),1);
                g.Box(pot+new Vector3(side*.55f,.42f,0),new Vector3(.04f,.09f,.045f),1);
            }
            g.Cylinder(pot+Vector3.up*.40f,.348f,.015f,7);
            g.Beam(pot+new Vector3(.03f,.42f,0),pot+new Vector3(.34f,.85f,-.11f),.027f,2);
            g.Cylinder(pot+new Vector3(.03f,.405f,0),.062f,.026f,2);
            // Second covered soup vessel behind the serving area; different height/diameter.
            var soup=new Vector3(-.90f,1.68f,-3.27f);
            g.Cylinder(soup,.27f,.40f,2);g.Profile(soup+Vector3.up*.40f,new[]{.29f,.22f,.055f,0f},new[]{0f,.045f,.07f,.07f},0);
            g.Box(soup+new Vector3(0,.50f,0),new Vector3(.13f,.055f,.05f),1);
            // Stacks of enamel bowls, clearly curved rather than identical food cubes.
            foreach(float x in new[]{.65f,1.30f})for(int level=0;level<4;level++)
                g.Bowl(new Vector3(x,1.115f+level*.078f,-1.05f),.225f,.16f);
            g.Bowl(new Vector3(.08f,1.115f,-.86f),.225f,.16f);
            g.Cylinder(new Vector3(.08f,1.235f,-.86f),.198f,.012f,7);
            foreach(float dx in new[]{-.075f,0,.075f})g.Beam(new Vector3(.08f+dx,1.25f,-.98f),new Vector3(.08f+dx+.045f,1.25f,-.74f),.022f,3);
            // Condiments are grouped in a shallow tray on the other side of service.
            g.Box(new Vector3(1.86f,1.14f,-1.03f),new Vector3(.39f,.05f,.53f),1);
            foreach(float z in new[]{-.88f,-1.15f})
            {
                g.Cylinder(new Vector3(1.86f,1.168f,z),.078f,.26f,6);
                g.Cylinder(new Vector3(1.86f,1.27f,z),.08f,.11f,3);
                g.Cylinder(new Vector3(1.86f,1.428f,z),.044f,.08f,0);
            }
            // Supported covered pans, dish stacks and utensils occupy existing rear shelves.
            foreach(float x in new[]{-1.7f,-.85f})
            {
                g.Box(new Vector3(x,1.15f,-3.29f),new Vector3(.68f,.18f,.35f),2);
                g.Box(new Vector3(x,1.251f,-3.29f),new Vector3(.70f,.035f,.37f),0);
                g.Box(new Vector3(x,1.286f,-3.29f),new Vector3(.15f,.035f,.06f),1);
            }
            foreach(float x in new[]{.10f,.76f,1.42f})for(int level=0;level<3;level++)
                g.Bowl(new Vector3(x,1.065f+level*.075f,-3.27f),.21f,.15f);
            g.Cylinder(new Vector3(.40f,1.68f,-3.28f),.10f,.22f,2);
            for(int i=0;i<4;i++)g.Beam(new Vector3(.34f+i*.042f,1.78f,-3.28f),new Vector3(.32f+i*.056f,2.10f,-3.28f),.018f,0);
            var face=room.Find("Sign face Pares");
            foreach(float dx in new[]{-.36f,.36f})g.Box(new Vector3(face.localPosition.x+face.localScale.x*dx,2.96f,.50f),new Vector3(.055f,.95f,.065f),2);
            var go=new GameObject(RootName);go.transform.SetParent(room,false);go.isStatic=true;
            var mesh=g.Build();int vertices=mesh.vertexCount;go.AddComponent<MeshFilter>().sharedMesh=Save(mesh,Folder+"/ParesCounter.asset");
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=Palette();renderer.shadowCastingMode=ShadowCastingMode.Off;
            AirborneByDesign.Attach(go,"Cooking/serving equipment rests on retained counter and rear shelves; ladle is seated in pot, utensil holder supports handles; fascia supports attach to slab.");
            if(solids.Count!=map.GetComponentsInChildren<Collider>(true).Length||solids.Any(p=>p.Key==null||p.Key.bounds!=p.Value))throw new InvalidOperationException("Pares shop changed private boundary/gameplay collision.");
            report.AppendLine("Pares only: shaped stockpot/ladle, covered soup vessel, enamel bowl stacks/one serving, condiment tray, covered pans and utensils on supported shelves. "+vertices+" vertices, one renderer/material, no new collider/shadow caster. Street cart/private boundary retained.");
        }
        private static Material Palette()
        {
            var texture=new Texture2D(8,1,TextureFormat.RGBA32,false);texture.SetPixels(Colours);texture.Apply();string path=Folder+"/ParesPalette.png";File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
            var i=(TextureImporter)AssetImporter.GetAtPath(path);i.npotScale=TextureImporterNPOTScale.None;i.filterMode=FilterMode.Point;i.mipmapEnabled=false;i.wrapMode=TextureWrapMode.Clamp;i.textureCompression=TextureImporterCompression.Uncompressed;i.SaveAndReimport();
            var m=new Material(Shader.Find("Standard")){color=Color.white,mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path)};m.SetFloat("_Glossiness",.10f);return Save(m,Folder+"/ParesCounter.mat");
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
            public void Profile(Vector3 at,float[] radii,float[] heights,int colour)
            {
                Vector3 P(int ring,int angle){float a=angle*Mathf.PI/6;return at+new Vector3(Mathf.Cos(a)*radii[ring],heights[ring],Mathf.Sin(a)*radii[ring]);}
                for(int ring=0;ring<radii.Length-1;ring++)for(int i=0;i<12;i++)
                    Quad(P(ring,i+1),P(ring,i),P(ring+1,i),P(ring+1,i+1),colour);
            }
            public void Bowl(Vector3 at,float radius,float height)
            {
                Profile(at,new[]{radius*.40f,radius*.45f,radius*.88f,radius,radius*.88f,radius*.37f,0f},
                    new[]{0f,height*.10f,height*.72f,height,height*.91f,height*.24f,height*.24f},0);
                Profile(at,new[]{radius*.985f,radius,radius*.88f},new[]{height*.94f,height,height*.91f},6);
            }
            public Mesh Build(){var m=new Mesh{name="Pares stockpot and serving equipment"};m.SetVertices(points);m.SetUVs(0,uv);m.SetTriangles(indices,0);m.RecalculateNormals();m.RecalculateBounds();return m;}
        }
    }
}
