using System;
using System.Linq;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    public static class ResidentLaundryAuthor
    {
        public static Transform Place(Transform parent,string model,Vector3 start,float yaw,float ground,bool freestanding=true)
        {
            string path="Assets/TumbangPreso/Art/models/resident-laundry/"+model+".glb";
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(asset==null)throw new InvalidOperationException("Missing original laundry source: "+path);
            var group=new GameObject("Resident laundry "+model).transform;group.SetParent(parent,false);
            group.SetPositionAndRotation(start,Quaternion.Euler(0,yaw,0));
            var line=(GameObject)PrefabUtility.InstantiatePrefab(asset);line.transform.SetParent(group,false);
            var rope=line.GetComponentsInChildren<MeshFilter>().First(f=>f.name=="Fixed clothesline");
            Vector3[] Points()=>rope.sharedMesh.vertices.Select(v=>group.InverseTransformPoint(rope.transform.TransformPoint(v))).ToArray();
            var points=Points();
            // glTF's handedness conversion reverses this source's horizontal
            // direction. Orient from measured geometry,not an assumed export axis.
            if(points.Average(p=>p.x)<0){line.transform.localRotation*=Quaternion.Euler(0,180,0);points=Points();}
            line.AddComponent<LaundryMotion>();
            AirborneByDesign.Attach(line,freestanding?"Clothes hang from the fixed sagging line between its two supported end posts.":
                "The measured rope ends tie to the existing utility trunks below the electrical conductors.");
            float span=model=="alley-line"?15.5f:4.4f;
            var steel=AssetDatabase.LoadAssetAtPath<Material>("Assets/TumbangPreso/Art/NeighborhoodFinish/painted_steel.mat");
            float min=points.Min(p=>p.x),max=points.Max(p=>p.x);
            if(Mathf.Abs((max-min)-span)>.05f)throw new InvalidOperationException("Unexpected imported laundry span");
            if(!freestanding)return group;
            var anchors=new[]{points.Where(p=>p.x<min+.02f).ToArray(),points.Where(p=>p.x>max-.02f).ToArray()}
                .Select(end=>end.Aggregate(Vector3.zero,(sum,p)=>sum+p)/end.Length).ToArray();
            foreach(var anchor in anchors)
            {
                float height=start.y+anchor.y-ground+.035f;
                var post=GameObject.CreatePrimitive(PrimitiveType.Cube);post.name="Clothesline end post";
                post.transform.SetParent(group,false);post.transform.localPosition=anchor+new Vector3(0,-height*.5f+.035f,0);
                post.transform.localScale=new Vector3(.13f,height,.13f);post.GetComponent<Renderer>().sharedMaterial=steel;
                post.isStatic=true;
                var foot=GameObject.CreatePrimitive(PrimitiveType.Cube);foot.name="Post foot";foot.transform.SetParent(group,false);
                foot.transform.localPosition=anchor+new Vector3(0,-height+.155f,0);foot.transform.localScale=new Vector3(.28f,.24f,.28f);
                foot.GetComponent<Renderer>().sharedMaterial=steel;foot.isStatic=true;
            }
            return group;
        }

        public static void AddToEskinita(Transform root)
        {
            var west=root.Find("PosteRework_Poste_2").GetComponent<BoxCollider>().bounds;
            var east=root.Find("PosteRework_Poste_8").GetComponent<BoxCollider>().bounds;
            var start=new Vector3(west.max.x+.006f,4.45f,west.center.z);
            var end=new Vector3(east.min.x-.006f,4.45f,east.center.z);
            var group=Place(root,"alley-line",start,0,.1f,false);
            group.localScale=new Vector3((end.x-start.x)/15.5f,1,1);
        }
    }
}
