using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>Small cloth billows below fixed pegs; never moves the line or supports.</summary>
    public sealed class LaundryMotion : MonoBehaviour
    {
        private sealed class Cloth
        { public MeshFilter Filter;public Mesh Source,Drawn;public Vector3[] Rest,Points;public float Phase; }
        private readonly List<Cloth> _cloths=new List<Cloth>();
        private void Start()
        {
            foreach(var filter in GetComponentsInChildren<MeshFilter>())
            {
                if(!filter.name.StartsWith("Cloth_")||filter.sharedMesh==null||!filter.sharedMesh.isReadable)continue;
                var source=filter.sharedMesh;var mesh=Instantiate(source);mesh.MarkDynamic();
                var rest=mesh.vertices;
                _cloths.Add(new Cloth{Filter=filter,Source=source,Drawn=mesh,Rest=rest,Points=new Vector3[rest.Length],Phase=_cloths.Count*1.73f});
                filter.sharedMesh=mesh;
            }
        }
        private void LateUpdate()
        {
            foreach(var cloth in _cloths)
            {
                for(int i=0;i<cloth.Rest.Length;i++)
                {
                    var p=cloth.Rest[i];float free=Mathf.SmoothStep(0,1,Mathf.Clamp01(-p.y/.65f));
                    p.z+=Mathf.Sin(Time.time*1.6f+cloth.Phase+p.x*3-p.y*2)*.035f*free;
                    cloth.Points[i]=p;
                }
                cloth.Drawn.vertices=cloth.Points;cloth.Drawn.RecalculateBounds();
            }
        }
        private void OnDestroy()
        {
            foreach(var cloth in _cloths)
            {
                if(cloth.Filter!=null)cloth.Filter.sharedMesh=cloth.Source;
                if(cloth.Drawn!=null)Destroy(cloth.Drawn);
            }
        }
    }
}
