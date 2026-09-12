using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    public static class EskinitaUtilityAuthor
    {
        private const string Folder="Assets/TumbangPreso/Art/EskinitaNeighborhood";
        private static readonly string[] Anchors={"WireAnchor_high0","WireAnchor_high1","WireAnchor_high2","WireAnchor_mid0","WireAnchor_mid1"};

        public static void Finish(Transform dressing,Transform root,StringBuilder report)
        {
            var group=dressing.Find("Kable");if(group==null)throw new InvalidOperationException("Missing retained utility row.");
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TumbangPreso/Art/models/urban-utilities/retained-utility-pole.glb");
            if(asset==null)throw new InvalidOperationException("Missing derived pole hardware source.");
            var rows=new[]{new List<Transform>(),new List<Transform>()};
            foreach(Transform original in group)
            {
                if(!original.name.StartsWith("Poste_"))continue;
                var pole=(GameObject)PrefabUtility.InstantiatePrefab(asset);pole.name="PosteRework_"+original.name;
                pole.transform.SetParent(root,false);pole.transform.SetPositionAndRotation(original.position,Quaternion.identity);
                var renderers=pole.GetComponentsInChildren<MeshRenderer>();
                var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                if(Mathf.Abs(bounds.size.y-7.2f)>.03f)throw new InvalidOperationException("Unexpected pole import scale: "+bounds);
                pole.transform.position+=Vector3.up*(.1f-bounds.min.y);
                var points=pole.GetComponentsInChildren<MeshFilter>().SelectMany(f=>f.sharedMesh.vertices.Select(f.transform.TransformPoint))
                    .Where(v=>v.y<.15f).ToArray();
                if(points.Length<4)throw new InvalidOperationException("Missing pole trunk base vertices.");
                var trunk=new Bounds(points[0],Vector3.zero);foreach(var point in points)trunk.Encapsulate(point);
                var collider=pole.AddComponent<BoxCollider>();
                collider.center=pole.transform.InverseTransformPoint(new Vector3(trunk.center.x,3.7f,trunk.center.z));
                collider.size=new Vector3(trunk.size.x,7.2f,trunk.size.z);
                pole.isStatic=true;original.gameObject.SetActive(false);
                rows[original.position.x<0?0:1].Add(pole.transform);
            }
            if(rows.Any(r=>r.Count!=6))throw new InvalidOperationException("Expected the retained six posts on each side.");
            var vertices=new List<Vector3>();var indices=new List<int>();int count=0;
            foreach(var row in rows)
            {
                row.Sort((a,b)=>a.position.z.CompareTo(b.position.z));
                for(int i=0;i+1<row.Count;i++)foreach(string name in Anchors)
                {
                    var a=row[i].GetComponentsInChildren<Transform>().SingleOrDefault(t=>t.name==name);
                    var b=row[i+1].GetComponentsInChildren<Transform>().SingleOrDefault(t=>t.name==name);
                    if(a==null||b==null)throw new InvalidOperationException("Missing authored wire anchor: "+name);
                    Span(vertices,indices,root.InverseTransformPoint(a.position),root.InverseTransformPoint(b.position),name.Contains("high")?.34f:.45f);
                    count++;
                }
            }
            var mesh=new Mesh{name="EskinitaConnectedWires"};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            string meshPath=Folder+"/EskinitaConnectedWires.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(saved==null)AssetDatabase.CreateAsset(mesh,meshPath);else{EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);mesh=saved;EditorUtility.SetDirty(mesh);}
            string materialPath=Folder+"/utility_wire.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null){material=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(material,materialPath);}
            material.color=new Color(.05f,.055f,.05f);material.SetFloat("_Glossiness",.08f);EditorUtility.SetDirty(material);
            var wire=new GameObject("Connected neighborhood conductors");wire.transform.SetParent(root,false);
            wire.AddComponent<MeshFilter>().sharedMesh=mesh;wire.AddComponent<MeshRenderer>().sharedMaterial=material;wire.isStatic=true;
            AirborneByDesign.Attach(wire,"Five separated conductors connect the actual retained pole insulators; circular cross-sections remain visible from oblique views.");
            report.AppendLine("Eskinita utilities:12 retained hardware posts with measured trunk collision;"+count+" connected44mm conductors on authored insulator anchors; original baked span meshes inactive.");
        }

        private static void Span(List<Vector3> vertices,List<int> indices,Vector3 a,Vector3 b,float sag)
        {
            const int steps=16,sides=6;int start=vertices.Count;
            var along=(b-a).normalized;var across=Vector3.Cross(Vector3.up,along).normalized;
            var up=Vector3.Cross(along,across).normalized;
            for(int i=0;i<=steps;i++)
            {
                float t=i/(float)steps;var center=Vector3.Lerp(a,b,t)-Vector3.up*(4*t*(1-t)*sag);
                for(int s=0;s<sides;s++)
                {float angle=s*Mathf.PI*2/sides;vertices.Add(center+(across*Mathf.Cos(angle)+up*Mathf.Sin(angle))*.022f);}
            }
            for(int i=0;i<steps;i++)for(int s=0;s<sides;s++)
            {
                int n=start+i*sides+s,next=start+i*sides+(s+1)%sides;
                indices.AddRange(new[]{n,next,next+sides,n,next+sides,n+sides});
            }
        }
    }
}
