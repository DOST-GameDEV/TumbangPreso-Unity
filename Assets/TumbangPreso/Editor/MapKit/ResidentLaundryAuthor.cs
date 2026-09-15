using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
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
            float span=model.StartsWith("alley-",StringComparison.Ordinal)?15.5f:4.4f;
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
            AcrossStreet(root,"alley-line",2,8,4.45f);
            AcrossStreet(root,"alley-colour-line",4,10,4.65f);
            InYard(root,"courtyard-family-line","Bahay_4_W",-1,2.70f);
            InYard(root,"courtyard-sheets-line","Bahay_2_E",1,2.85f);
        }

        private static void AcrossStreet(Transform root,string model,int westId,int eastId,float height)
        {
            Physics.SyncTransforms();
            var west=root.Find("PosteRework_Poste_"+westId).GetComponent<BoxCollider>().bounds;
            var east=root.Find("PosteRework_Poste_"+eastId).GetComponent<BoxCollider>().bounds;
            var start=new Vector3(west.max.x+.006f,height,west.center.z);
            var end=new Vector3(east.min.x-.006f,height,east.center.z);
            var delta=end-start;
            var group=Place(root,model,start,-Mathf.Atan2(delta.z,delta.x)*Mathf.Rad2Deg,.1f,false);
            group.localScale=new Vector3(delta.magnitude/15.5f,1,1);
            Tie(group,"west",west,start,1);
            Tie(group,"east",east,end,-1);
        }

        private static void InYard(Transform root,string model,string lot,int side,float height)
        {
            var plot=root.Find("Plot_"+lot);
            if(plot==null)throw new InvalidOperationException("Missing laundry yard: "+lot);
            var paving=plot.Find("Private yard").GetComponent<Renderer>().bounds;
            // Parallel to the frontage, between the boundary and retained facade.
            // Both end posts sit outside the central gate opening.
            var start=new Vector3(plot.position.x+side*.88f,height,plot.position.z-2.2f);
            var group=Place(root,model,start,-90,paving.max.y);
            Physics.SyncTransforms();
            int index=0;
            foreach(var post in group.GetComponentsInChildren<Transform>().Where(t=>t.name=="Clothesline end post").ToArray())
            {
                var bounds=post.GetComponent<Renderer>().bounds;
                Tie(group,"yard-"+index++,bounds,new Vector3(bounds.center.x,height,bounds.center.z),side);
            }
        }

        private static void Tie(Transform parent,string id,Bounds trunk,Vector3 anchor,int inward)
        {
            // Two tight turns, a visible crossed hitch and a short loose tail.
            // The wrap follows the measured square trunk, with no floating ring.
            var vertices=new List<Vector3>();var triangles=new List<int>();
            float x=trunk.center.x,z=trunk.center.z,rx=trunk.extents.x+.011f,rz=trunk.extents.z+.011f;
            var wrap=new List<Vector3>();
            var corners=new[]{new Vector2(1,0),new Vector2(1,1),new Vector2(-1,1),new Vector2(-1,-1),new Vector2(1,-1),new Vector2(1,0)};
            for(int turn=0;turn<2;turn++)for(int j=0;j<corners.Length;j++)
            {
                float y=anchor.y-.024f+(turn+j/(float)(corners.Length-1))*.028f;
                wrap.Add(new Vector3(x+inward*corners[j].x*rx,y,z+corners[j].y*rz));
            }
            Tube(parent,wrap,.014f,vertices,triangles);
            var knot=new Vector3(x+inward*(rx+.018f),anchor.y,z);
            Tube(parent,new[]{wrap[wrap.Count-1],knot+new Vector3(0,.024f,.031f),
                knot+new Vector3(inward*.026f,-.018f,-.032f),knot+new Vector3(0,-.025f,.029f),
                knot+new Vector3(inward*.026f,.024f,-.027f),anchor},.014f,vertices,triangles);
            Tube(parent,new[]{knot,knot+new Vector3(inward*.035f,-.12f,.03f),knot+new Vector3(inward*.065f,-.20f,.016f)},.012f,vertices,triangles);
            var mesh=new Mesh{name=parent.name+" "+id+" rope hitch"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);
            mesh.RecalculateNormals();mesh.RecalculateBounds();
            const string folder="Assets/TumbangPreso/Art/models/resident-laundry";
            string path=folder+"/"+parent.name.Replace("Resident laundry ","")+"-"+id+"-hitch.asset";
            var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved==null)AssetDatabase.CreateAsset(mesh,path);
            else{EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);mesh=saved;EditorUtility.SetDirty(mesh);}
            var go=new GameObject("Supported rope hitch "+id);go.transform.SetParent(parent,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var rope=parent.GetComponentsInChildren<MeshRenderer>().First(r=>r.name=="Fixed clothesline");
            go.AddComponent<MeshRenderer>().sharedMaterial=rope.sharedMaterial;go.isStatic=true;
            AirborneByDesign.Attach(go,"Two rope turns hug the measured support; the crossed hitch joins the actual clothesline end.");
        }

        private static void Tube(Transform parent,IReadOnlyList<Vector3> points,float radius,List<Vector3> vertices,List<int> triangles)
        {
            const int sides=6;int first=vertices.Count;
            for(int i=0;i<points.Count;i++)
            {
                var along=(points[Mathf.Min(i+1,points.Count-1)]-points[Mathf.Max(0,i-1)]).normalized;
                var across=Vector3.Cross(along,Mathf.Abs(along.y)>.9f?Vector3.forward:Vector3.up).normalized;
                var up=Vector3.Cross(along,across).normalized;
                for(int s=0;s<sides;s++)
                {float angle=s*Mathf.PI*2/sides;vertices.Add(parent.InverseTransformPoint(points[i]+radius*(across*Mathf.Cos(angle)+up*Mathf.Sin(angle))));}
            }
            for(int i=0;i<points.Count-1;i++)for(int s=0;s<sides;s++)
            {int a=first+i*sides+s,b=first+i*sides+(s+1)%sides;triangles.AddRange(new[]{a,b,b+sides,a,b+sides,a+sides});}
        }

        public static void RefreshEskinita()
        {
            const string path="Assets/TumbangPreso/Scenes/Maps/Eskinita.unity";
            var scene=EditorSceneManager.OpenScene(path);
            var root=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="NeighborhoodRework");
            foreach(Transform child in root.Cast<Transform>().Where(t=>t.name.StartsWith("Resident laundry ",StringComparison.Ordinal)).ToArray())
                Object.DestroyImmediate(child.gameObject);
            AddToEskinita(root);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory("Logs/laundry-review");
            var geometry=root.Cast<Transform>().Where(t=>t.name.StartsWith("Resident laundry ",StringComparison.Ordinal))
                .SelectMany(t=>t.GetComponentsInChildren<MeshFilter>()).Select(f=>f.name+"|"+f.sharedMesh.vertexCount+"|"+
                    string.Join(";",f.sharedMesh.vertices.Select(v=>f.transform.TransformPoint(v).ToString("F4"))))
                .OrderBy(s=>s,StringComparer.Ordinal);
            File.WriteAllLines("Logs/laundry-review/semantic.txt",geometry);
            Debug.Log("[LaundryAuthor] Saved four distinct supported laundry lines in Eskinita; unrelated map groups preserved.");
        }
    }
}
