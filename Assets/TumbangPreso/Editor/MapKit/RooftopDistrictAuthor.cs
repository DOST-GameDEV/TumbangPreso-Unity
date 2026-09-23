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
    /// <summary>Map-local outer residential district, beyond all retained SaBubong scenery.</summary>
    public static class RooftopDistrictAuthor
    {
        private const string Folder="Assets/TumbangPreso/Art/SaBubong/OuterDistrict",RootName="Outer residential district";
        [Serializable] private sealed class Block {public float x,z;public bool garden;}
        [Serializable] private sealed class Plan {public float retainedHalfExtent,groundHalfExtent,baseY,blockPitch,blockHalfExtent;public Block[] blocks;}
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene(SaBubongBuilder.ScenePath);var report=new StringBuilder();FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/rooftop-district");File.WriteAllText("Logs/rooftop-district/author.txt",report.ToString());
            Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            var map=GameObject.Find("SaBubong");var city=map.transform.Find("Dressing/Metro rooftops");
            var solids=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            var old=city.Find(RootName);if(old!=null)Object.DestroyImmediate(old.gameObject);
            var plan=JsonUtility.FromJson<Plan>(File.ReadAllText("MapSource/environment/layouts/sabubong-outer-district-20260924.json"));
            var ground=city.Find("Distant city ground");var groundRenderer=ground.GetComponent<MeshRenderer>();
            if(ground.GetComponentsInChildren<Collider>().Length!=0)throw new InvalidOperationException("Visual ground gained collision.");
            // Inspect the actual retained bodies as well as the documented central reserve.
            var occupied=city.GetComponentsInChildren<MeshRenderer>().Where(r=>r!=groundRenderer&&r.transform.name!="Connected city roads").Select(r=>r.bounds).ToArray();
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();var material=RooftopNeighborsAuthor.Palette();
            var root=new GameObject(RootName).transform;root.SetParent(city,false);int homes=0,gardens=0,vertices=0,index=0;
            foreach(var block in plan.blocks)
            {
                if(Mathf.Abs(block.x)-plan.blockHalfExtent<plan.retainedHalfExtent&&Mathf.Abs(block.z)-plan.blockHalfExtent<plan.retainedHalfExtent)
                    throw new InvalidOperationException("Outer block enters retained district.");
                var g=new RooftopNeighborsAuthor.Geometry();
                for(int slot=0;slot<4;slot++)
                {
                    float x=slot%2==0?-11:11,z=slot<2?-11:11;int variant=index*5+slot*3;
                    var p=new Vector3(x,0,z);
                    if(block.garden&&slot==2)
                    {
                        g.Box(p+Vector3.up*.06f,new Vector3(16,.12f,17),11,15);
                        foreach(var t in new[]{new Vector3(-4,0,-3),new Vector3(3,0,4)})
                        {g.Cylinder(p+t,.3f,4.5f,8,6);g.Cylinder(p+t+Vector3.up*3.8f,2.8f,2.6f,12,7);g.Cylinder(p+t+Vector3.up*6.2f,1.6f,.9f,11,7);}
                        gardens++;continue;
                    }
                    float w=14+(variant%3),d=15+(variant%2),h=11+(variant%6)*2.4f;
                    var footprint=new Bounds(new Vector3(block.x+x,plan.baseY+h*.5f,block.z+z),new Vector3(w+.6f,h,d+.6f));
                    if(occupied.Any(o=>Overlap(footprint,o,.7f)))throw new InvalidOperationException("Outer home overlaps retained scenery at "+footprint.center);
                    if(Mathf.Abs(x)+w*.5f+.3f>plan.blockHalfExtent||Mathf.Abs(z)+d*.5f+.3f>plan.blockHalfExtent)
                        throw new InvalidOperationException("Outer home crosses its block street.");
                    Home(g,p,w,d,h,variant);homes++;
                }
                vertices+=Save(root,"Block"+index,g,material,new Vector3(block.x,plan.baseY,block.z));index++;
            }
            var streets=new RooftopNeighborsAuthor.Geometry();var roadRects=new List<Rect>();
            void Road(float x,float z,float w,float d)
            {streets.Box(new Vector3(x,-26.021f,z),new Vector3(w,.045f,d),4,12);roadRects.Add(Rect.MinMaxRect(x-w*.5f,z-d*.5f,x+w*.5f,z+d*.5f));}
            // Continuous peripheral streets. Middle sections remain open for the retained town.
            foreach(float edge in new[]{-286f,-234f,-182f,182f,234f,286f})
            {Road(edge,0,7,579);Road(0,edge,579,7);}
            foreach(float line in new[]{-130f,-78f,-26f,26f,78f,130f})foreach(float side in new[]{-1f,1f})
            {Road(line,side*234,7,111);Road(side*234,line,111,7);}
            foreach(float line in new[]{-52f,-23.5f,23.5f,52f})foreach(float side in new[]{-1f,1f})Road(line,side*176.5f,5.7f,13);
            foreach(float line in new[]{-52f,-26.5f,26.5f,52f})foreach(float side in new[]{-1f,1f})Road(side*176.5f,line,13,5.7f);
            foreach(var block in plan.blocks)
            {
                var plot=Rect.MinMaxRect(block.x-plan.blockHalfExtent,block.z-plan.blockHalfExtent,block.x+plan.blockHalfExtent,block.z+plan.blockHalfExtent);
                if(roadRects.Any(r=>r.Overlaps(plot)))throw new InvalidOperationException("District street crosses an occupied block.");
            }
            vertices+=Save(root,"Connected outer streets",streets,material,Vector3.zero);
            float top=groundRenderer.bounds.max.y;var scale=ground.localScale;var source=ground.GetComponent<MeshFilter>().sharedMesh;
            scale.x=2*plan.groundHalfExtent/source.bounds.size.x/ground.parent.lossyScale.x;
            scale.z=2*plan.groundHalfExtent/source.bounds.size.z/ground.parent.lossyScale.z;ground.localScale=scale;
            if(Mathf.Abs(groundRenderer.bounds.max.y-top)>.0001f)throw new InvalidOperationException("Visual ground height changed.");
            MapSurfaceAuthor.FinishLoadedScene("SaBubong",report,ground);
            if(solids.Count!=map.GetComponentsInChildren<Collider>(true).Length||solids.Any(p=>p.Key==null||p.Key.bounds!=p.Value))
                throw new InvalidOperationException("Outer district changed collision.");
            report.AppendLine($"{plan.blocks.Length}blocks, {homes}homes, {gardens}planted courts; {vertices}vertices/{index+1}renderers, one existing palette. Retained180m central scenery untouched, streets connected, ground3000m/no new collision or shadow casters.");
        }
        private static void Home(RooftopNeighborsAuthor.Geometry g,Vector3 p,float w,float d,float h,int variant)
        {
            int wall=variant%3;g.Box(p+Vector3.up*(h*.5f),new Vector3(w,h,d),wall);
            g.Box(p+Vector3.up*.35f,new Vector3(w+.2f,.7f,d+.2f),4);
            // Window groups stay broad enough to survive the roof-level distance.
            for(float y=3;y<h-1.5f;y+=3.2f)foreach(float side in new[]{-1f,1f})
            {
                foreach(float x in new[]{-w*.27f,w*.27f})g.Box(p+new Vector3(x,y,side*(d*.5f+.025f)),new Vector3(w*.32f,1.5f,.05f),5);
                foreach(float z in new[]{-d*.27f,d*.27f})g.Box(p+new Vector3(side*(w*.5f+.025f),y,z),new Vector3(.05f,1.5f,d*.3f),6);
            }
            if(variant%3==0)
            {
                var a=p+new Vector3(-w*.5f-.25f,h,-d*.5f-.25f);var b=p+new Vector3(w*.5f+.25f,h,-d*.5f-.25f);
                var c=p+new Vector3(w*.5f+.25f,h,d*.5f+.25f);var e=p+new Vector3(-w*.5f-.25f,h,d*.5f+.25f);
                var r0=p+new Vector3(0,h+2.3f,-d*.5f-.25f);var r1=p+new Vector3(0,h+2.3f,d*.5f+.25f);
                g.Quad(a,e,r1,r0,7,5);g.Quad(r0,r1,c,b,13,5);
                g.Quad(b,a,r0,r0,wall);g.Quad(e,c,r1,r1,wall);
            }
            else
            {
                g.Box(p+Vector3.up*(h+.14f),new Vector3(w+.4f,.28f,d+.4f),3);
                foreach(float side in new[]{-1f,1f})
                {g.Box(p+new Vector3(0,h+.5f,side*(d*.5f-.12f)),new Vector3(w,.75f,.24f),wall);
                    g.Box(p+new Vector3(side*(w*.5f-.12f),h+.5f,0),new Vector3(.24f,.75f,d),wall);}
                g.Box(p+new Vector3(w*.22f,h+1.4f,d*.2f),new Vector3(3.5f,2.8f,4),wall);
                g.Box(p+new Vector3(w*.22f,h+2.9f,d*.2f),new Vector3(3.8f,.2f,4.3f),3);
                if(variant%4==0)g.Cylinder(p+new Vector3(-w*.27f,h+.28f,d*.25f),.75f,1.6f,10);
            }
        }
        private static int Save(Transform root,string name,RooftopNeighborsAuthor.Geometry g,Material material,Vector3 position)
        {
            var mesh=g.Mesh(name);string path=Folder+"/"+name.Replace(" ","")+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}else{EditorUtility.CopySerialized(mesh,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(mesh);}
            var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.localPosition=position;go.isStatic=true;
            go.AddComponent<MeshFilter>().sharedMesh=saved;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;
            AirborneByDesign.Attach(go,"Distant city scenery supported on retained street-level ground.");return saved.vertexCount;
        }
        private static bool Overlap(Bounds a,Bounds b,float gap)=>a.min.x-gap<b.max.x&&a.max.x+gap>b.min.x&&a.min.z-gap<b.max.z&&a.max.z+gap>b.min.z;
    }
}
