using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools
{
    // Preserve Inday's actual coral guard shapes around the retained clean hand.
    // The long model-held props are not clothing and never enter the FPP mesh.
    public static class FppGuardAuthor
    {
        public static void Run()
        {
            try
            {
                var entry=RosterBook.Load().FindPersonArt("inday");
                foreach(string side in new[]{"left","right"})Bake(entry,side);
                AssetDatabase.SaveAssets();EditorApplication.Exit(0);
            }
            catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
        }

        private static void Bake(RosterEntryAsset entry,string side)
        {
            var renderer=entry.Model.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r=>r.name=="body-mesh");
            var source=renderer.sharedMesh;var vertices=source.vertices;var uv=source.uv;var weights=source.boneWeights;
            int bone=Array.FindIndex(renderer.bones,b=>b.name=="arm-"+side);var bind=source.bindposes[bone];
            var local=vertices.Select(bind.MultiplyPoint3x4).ToArray();
            int Slot(int i)=>Mathf.FloorToInt(uv[i].x*16)/2+(Mathf.FloorToInt(uv[i].y*16)<=3?8:0);
            bool Owned(int i)
            {
                var w=weights[i];return (w.boneIndex0==bone?w.weight0:0)+(w.boneIndex1==bone?w.weight1:0)
                    +(w.boneIndex2==bone?w.weight2:0)+(w.boneIndex3==bone?w.weight3:0)>.99f;
            }
            var hand=Enumerable.Range(0,vertices.Length).Where(i=>Owned(i)&&Slot(i)==14).Select(i=>local[i]).ToArray();
            if(hand.Length<8)throw new InvalidOperationException("Restored Inday has no skin hand region.");
            var min=hand.Aggregate(Vector3.Min);var max=hand.Aggregate(Vector3.Max);var centre=(min+max)*.5f;
            float sign=Mathf.Sign(centre.x),near=Mathf.Min(min.x*sign,max.x*sign),far=Mathf.Max(min.x*sign,max.x*sign);
            var parent=Enumerable.Range(0,vertices.Length).ToArray();
            int Find(int i){while(parent[i]!=i){parent[i]=parent[parent[i]];i=parent[i];}return i;}
            void Join(int a,int b){parent[Find(b)]=Find(a);}
            var welded=new Dictionary<Vector3Int,int>();var triangles=new List<int[]>();var raw=source.triangles;
            for(int i=0;i<raw.Length;i+=3)
            {
                var triangle=new[]{raw[i],raw[i+1],raw[i+2]};
                if(!triangle.All(v=>Owned(v)&&Slot(v)==5))continue;
                foreach(int v in triangle)
                {
                    var p=local[v];var key=new Vector3Int(Mathf.RoundToInt(p.x*100000),Mathf.RoundToInt(p.y*100000),Mathf.RoundToInt(p.z*100000));
                    if(welded.TryGetValue(key,out int other))Join(v,other);else welded[key]=v;
                }
                Join(triangle[0],triangle[1]);Join(triangle[0],triangle[2]);triangles.Add(triangle);
            }
            var groups=triangles.GroupBy(t=>Find(t[0])).ToArray();
            var points=new List<Vector3>();var tex=new List<Vector2>();var indices=new List<int>();
            foreach(var group in groups)
            {
                var members=group.SelectMany(t=>t).Distinct().ToArray();
                if(members.Max(i=>local[i].x*sign)>far+.025f)continue;
                foreach(var triangle in group)
                {
                    var mapped=new Vector3[3];
                    for(int j=0;j<3;j++)
                    {
                        var p=local[triangle[j]];
                        // Preserve volume outside the skin. Clamping every outer
                        // vertex to one plane flattened the real curved guards.
                        mapped[j]=new Vector3(-sign*FitSection((p.z-centre.z)*2/(max.z-min.z),.158f),
                            .62f+(p.x*sign-near)*.22f/(far-near),
                            -FitSection((p.y-centre.y)*2/(max.y-min.y),.15f));
                    }
                    if(Vector3.Cross(mapped[1]-mapped[0],mapped[2]-mapped[0]).sqrMagnitude<1e-12f)continue;
                    for(int j=0;j<3;j++){indices.Add(points.Count);points.Add(mapped[j]);tex.Add(uv[triangle[j]]);}
                }
            }
            if(points.Count<12)throw new InvalidOperationException("No real guard geometry survived the hand fit.");
            var mesh=new Mesh{name="inday_"+side+"_coral_guard"};mesh.SetVertices(points);mesh.SetUVs(0,tex);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            const string folder="Assets/TumbangPreso/Resources/Models/FppDetails";Directory.CreateDirectory(folder);
            string path=folder+"/inday_"+side+"_guard.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing==null)AssetDatabase.CreateAsset(mesh,path);
            else{EditorUtility.CopySerialized(mesh,existing);EditorUtility.SetDirty(existing);Object.DestroyImmediate(mesh);}
            Debug.Log("[FppGuard] Mapped restored Inday's actual coral guard, excluding held props: "+side+", "+points.Count+" vertices");
        }

        private static float FitSection(float normalized,float handHalfWidth)
        {
            float size=Mathf.Abs(normalized);
            return Mathf.Sign(normalized)*(Mathf.Min(size,1)*handHalfWidth+Mathf.Max(0,size-1)*.035f);
        }
    }
}
