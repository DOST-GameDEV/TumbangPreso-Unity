using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    public static class RooftopTankFinishAuthor
    {
        private const string Folder="Assets/TumbangPreso/Art/SaBubong/TankFinish/",RootName="Resident tank finish",SourceTag="TumpRoofTankSource";
        private const string TankPath="SaBubong/Dressing/Laundry service corner/Resident water tank";
        private sealed class Geometry
        {
            public readonly List<Vector3> Points=new List<Vector3>();
            public readonly List<int>[] Indices={new List<int>(),new List<int>(),new List<int>()};
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,int material)
            {int n=Points.Count;Points.AddRange(new[]{a,b,c,d});Indices[material].AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
            public void Lathe(Vector3 center,float[] radii,float[] heights,int material)
            {
                const int segments=20;int start=Points.Count;
                for(int ring=0;ring<radii.Length;ring++)for(int i=0;i<segments;i++)
                {float a=i*Mathf.PI*2/segments;Points.Add(center+new Vector3(Mathf.Cos(a)*radii[ring],heights[ring],Mathf.Sin(a)*radii[ring]));}
                for(int ring=0;ring<radii.Length-1;ring++)for(int i=0;i<segments;i++)
                {int a=start+ring*segments+i,b=start+(ring+1)*segments+i,c=start+(ring+1)*segments+(i+1)%segments,d=start+ring*segments+(i+1)%segments;Indices[material].AddRange(new[]{a,b,c,a,c,d});}
            }
            public void Pipe(Vector3 a,Vector3 b,float radius,int material)
            {
                var direction=(b-a).normalized;var right=Vector3.Cross(direction,Vector3.forward).normalized;
                if(right.sqrMagnitude<.1f)right=Vector3.right;var across=Vector3.Cross(right,direction);
                for(int i=0;i<10;i++)
                {float x=i*Mathf.PI/5,y=(i+1)*Mathf.PI/5;var p=(right*Mathf.Cos(x)+across*Mathf.Sin(x))*radius;var q=(right*Mathf.Cos(y)+across*Mathf.Sin(y))*radius;Quad(a+p,b+p,b+q,a+q,material);}
            }
        }
        public static void ClearPrevious()
        {
            var old=GameObject.Find("SaBubong/Dressing/"+RootName);if(old!=null)Object.DestroyImmediate(old);
            var tank=GameObject.Find(TankPath);if(tank==null)return;
            var renderer=tank.GetComponent<MeshRenderer>();string source=renderer.sharedMaterial.GetTag(SourceTag,false);
            if(!string.IsNullOrEmpty(source))renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(source)??throw new InvalidOperationException("Missing original tank material.");
        }
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene(SaBubongBuilder.ScenePath);var report=new StringBuilder();FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/roof-tank-finish");File.WriteAllText("Logs/roof-tank-finish/author.txt",report.ToString());
            Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            ClearPrevious();var map=GameObject.Find("SaBubong");var solids=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            var tank=GameObject.Find(TankPath);if(tank==null)throw new InvalidOperationException("Missing retained water tank.");
            var renderer=tank.GetComponent<MeshRenderer>();var bounds=renderer.bounds;var center=bounds.center;
            if(Mathf.Abs(bounds.size.x-1.35f)>.02f||Mathf.Abs(bounds.max.y-1.81f)>.02f)throw new InvalidOperationException("Reassess changed tank proportions.");
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();var original=renderer.sharedMaterial;
            var draft=new Material(original);draft.SetOverrideTag(SourceTag,AssetDatabase.GetAssetPath(original));
            draft.SetFloat("_Metallic",.42f);draft.SetFloat("_Glossiness",.34f);
            string bodyPath=Folder+"DullTankSteel.mat";var steel=AssetDatabase.LoadAssetAtPath<Material>(bodyPath);
            if(steel==null){AssetDatabase.CreateAsset(draft,bodyPath);steel=draft;}else{EditorUtility.CopySerialized(draft,steel);EditorUtility.SetDirty(steel);Object.DestroyImmediate(draft);}
            renderer.sharedMaterial=steel;
            var dark=Mat("DullBandSteel",new Color(.36f,.42f,.41f),.25f,.30f);
            var pipe=Mat("ServicePipe",new Color(.22f,.39f,.42f),.08f,.22f);
            var g=new Geometry();var top=new Vector3(center.x,bounds.max.y+.002f,center.z);
            g.Lathe(top,new[]{.675f,.62f,.43f,.12f,0},new[]{0,.045f,.135f,.185f,.19f},0);
            g.Lathe(top,new[]{.115f,.115f,.09f,0},new[]{.183f,.225f,.24f,.24f},1);
            foreach(float y in new[]{.75f,1.40f})g.Lathe(new Vector3(center.x,y,center.z),new[]{.676f,.689f,.689f,.676f,.676f},new[]{-.022f,-.022f,.022f,.022f,-.022f},1);
            var outlet=new Vector3(center.x+.66f,1.1f,center.z);var elbow=new Vector3(center.x+.745f,1.1f,center.z);
            g.Pipe(outlet,elbow,.038f,2);g.Pipe(elbow,new Vector3(elbow.x,.432f,elbow.z),.038f,2);
            g.Pipe(new Vector3(elbow.x,1.06f,elbow.z),new Vector3(elbow.x,1.14f,elbow.z),.052f,1);
            g.Pipe(new Vector3(elbow.x,.95f,elbow.z-.065f),new Vector3(elbow.x,.95f,elbow.z+.065f),.018f,1);
            if(g.Points.Any(p=>Mathf.Abs(p.x-center.x)>.8f||Mathf.Abs(p.z-center.z)>.8f))throw new InvalidOperationException("Tank fitting leaves original plinth footprint.");
            var mesh=new Mesh{name="Roof tank fitted details"};mesh.SetVertices(g.Points);
            mesh.SetUVs(2,g.Points.Select(p=>new Vector2(p.x-center.x,p.y)).ToList());mesh.subMeshCount=3;
            for(int i=0;i<3;i++)mesh.SetTriangles(g.Indices[i],i);mesh.RecalculateNormals();mesh.RecalculateBounds();
            string path=Folder+"TankDetails.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}else{EditorUtility.CopySerialized(mesh,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(mesh);}
            var go=new GameObject(RootName);go.transform.SetParent(map.transform.Find("Dressing"),false);go.isStatic=true;
            go.AddComponent<MeshFilter>().sharedMesh=saved;go.AddComponent<MeshRenderer>().sharedMaterials=new[]{steel,dark,pipe};
            AirborneByDesign.Attach(go,"Lid, bands and service fittings attached to the retained water tank and plinth; not separate obstacles.");
            if(renderer.bounds!=bounds||solids.Count!=map.GetComponentsInChildren<Collider>(true).Length||solids.Any(p=>p.Key==null||p.Key.bounds!=p.Value))throw new InvalidOperationException("Tank finish changed retained body/collision.");
            AssetDatabase.SaveAssets();report.AppendLine($"Retained tank/plinth/collision; capped lid, service cap, two bands and contained outlet/valve. {g.Points.Count}detail vertices, one renderer, no new collision.");
        }
        private static Material Mat(string name,Color color,float metallic,float smooth)
        {
            string path=Folder+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
            m.color=color;m.SetFloat("_Metallic",metallic);m.SetFloat("_Glossiness",smooth);EditorUtility.SetDirty(m);return m;
        }
    }
}
