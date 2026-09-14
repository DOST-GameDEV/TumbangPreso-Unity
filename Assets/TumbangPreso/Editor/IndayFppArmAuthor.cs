using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.CameraSystem;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools
{
    public static class IndayFppArmAuthor
    {
        public static void Run()
        {
            try
            {
                var entry=RosterBook.Load().FindPersonArt("inday");
                foreach(string side in new[]{"left","right"})
                {
                    var mesh=Extract(entry.Model,side);
                    string path="Assets/TumbangPreso/Resources/Models/FppDetails/inday_"+side+"_arm.asset";
                    var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if(existing==null)AssetDatabase.CreateAsset(mesh,path);
                    else{EditorUtility.CopySerialized(mesh,existing);EditorUtility.SetDirty(existing);Object.DestroyImmediate(mesh);}
                }
                AssetDatabase.SaveAssets();EditorApplication.Exit(0);
            }
            catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
        }

        public static Mesh Extract(GameObject model,string side)
        {
            var renderer=model.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r=>r.name=="body-mesh");
            var source=renderer.sharedMesh;var vertices=source.vertices;var uv=source.uv;
            var weights=source.boneWeights;var normals=source.normals;
            int bone=Array.FindIndex(renderer.bones,b=>b.name=="arm-"+side);
            var bind=source.bindposes[bone];var local=vertices.Select(bind.MultiplyPoint3x4).ToArray();
            bool Owned(int i)
            {
                var w=weights[i];return (w.boneIndex0==bone?w.weight0:0)+(w.boneIndex1==bone?w.weight1:0)
                    +(w.boneIndex2==bone?w.weight2:0)+(w.boneIndex3==bone?w.weight3:0)>.99f;
            }
            int Slot(int i)=>Mathf.FloorToInt(uv[i].x*16)/2+(Mathf.FloorToInt(uv[i].y*16)<=3?8:0);
            var hand=Enumerable.Range(0,vertices.Length).Where(i=>Owned(i)&&Slot(i)==14).ToArray();
            if(hand.Length<8)throw new InvalidOperationException("Restored Inday hand is missing.");
            float sign=Mathf.Sign(hand.Average(i=>local[i].x));
            float handEnd=hand.Max(i=>local[i].x*sign);
            var parent=Enumerable.Range(0,vertices.Length).ToArray();
            int Find(int i){while(parent[i]!=i){parent[i]=parent[parent[i]];i=parent[i];}return i;}
            void Join(int a,int b){parent[Find(b)]=Find(a);}
            var welded=new Dictionary<Vector3Int,int>();var triangles=new List<int[]>();
            var indices=source.triangles;
            for(int i=0;i<indices.Length;i+=3)
            {
                var triangle=new[]{indices[i],indices[i+1],indices[i+2]};
                if(!triangle.All(Owned))continue;
                foreach(int v in triangle)
                {
                    var p=local[v];var key=new Vector3Int(Mathf.RoundToInt(p.x*100000),Mathf.RoundToInt(p.y*100000),Mathf.RoundToInt(p.z*100000));
                    if(welded.TryGetValue(key,out int other))Join(v,other);else welded[key]=v;
                }
                Join(triangle[0],triangle[1]);Join(triangle[0],triangle[2]);triangles.Add(triangle);
            }
            // The old mesh also carries a long pink object. Preserve every actual
            // sleeve/hand/guard component, but do not duplicate that held object.
            var retained=triangles.GroupBy(t=>Find(t[0])).Where(group=>
                group.SelectMany(t=>t).Max(i=>local[i].x*sign)<=handEnd+.025f).SelectMany(group=>group).ToArray();
            var used=retained.SelectMany(t=>t).Distinct().ToArray();
            var lo=used.Select(i=>local[i]).Aggregate(Vector3.Min);var hi=used.Select(i=>local[i]).Aggregate(Vector3.Max);
            // Rigid rotation, translation and ONE uniform scale. No clamping,
            // reconstructed hand, stretched axes, substitute colours or clothing.
            float shoulder=used.Min(i=>local[i].x*sign);
            float scale=ViewmodelArms.ArmLength/(handEnd-shoulder);
            var points=new List<Vector3>();var outNormals=new List<Vector3>();var tex=new List<Vector2>();var output=new List<int>();
            var remap=new Dictionary<int,int>();
            foreach(var triangle in retained)foreach(int i in triangle)
            {
                if(!remap.TryGetValue(i,out int mapped))
                {
                    mapped=points.Count;remap.Add(i,mapped);var p=local[i];var n=bind.MultiplyVector(normals[i]).normalized;
                    points.Add(new Vector3(-sign*p.z*scale,ViewmodelArms.ArmLength+(sign*p.x-handEnd)*scale,-p.y*scale));
                    outNormals.Add(new Vector3(-sign*n.z,sign*n.x,-n.y));tex.Add(uv[i]);
                }
                output.Add(mapped);
            }
            var mesh=new Mesh{name="IndayRestored_"+side+"_ExactArm"};
            mesh.SetVertices(points);mesh.SetNormals(outNormals);mesh.SetUVs(0,tex);mesh.SetTriangles(output,0);mesh.RecalculateBounds();
            Debug.Log("[IndayFpp] Exact restored "+side+" arm: "+points.Count+" vertices, "+output.Count/3+" source triangles; uniform scale "+scale);
            return mesh;
        }
    }
}
