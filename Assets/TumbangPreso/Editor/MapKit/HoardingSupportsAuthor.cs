using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    public static class HoardingSupportsAuthor
    {
        public static void RepairLoaded()
        {
            var skyline=GameObject.Find("IlalimNgTulay/Dressing/SkylineKit");
            if(skyline==null)return;
            foreach(Transform board in skyline.transform)
            {
                if(!board.name.StartsWith("Hoarding_",StringComparison.Ordinal))continue;
                var points=new List<Vector3>();
                foreach(var filter in board.GetComponentsInChildren<MeshFilter>())
                    points.AddRange(filter.sharedMesh.vertices.Select(v=>filter.transform.TransformPoint(v)));
                float bottom=points.Min(p=>p.y);
                var feet=points.Where(p=>p.y<bottom+.012f).ToArray();
                bool alongX=feet.Max(p=>p.x)-feet.Min(p=>p.x)>feet.Max(p=>p.z)-feet.Min(p=>p.z);
                float middle=alongX?(feet.Min(p=>p.x)+feet.Max(p=>p.x))*.5f:(feet.Min(p=>p.z)+feet.Max(p=>p.z))*.5f;
                string prefix="HoardingMast_"+board.name.Substring("Hoarding_".Length)+"_";
                var masts=skyline.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith(prefix,StringComparison.Ordinal)).OrderBy(t=>t.name,StringComparer.Ordinal).ToArray();
                if(masts.Length!=2)throw new InvalidOperationException("Expected two billboard supports for "+board.name);
                for(int side=0;side<2;side++)
                {
                    var foot=feet.Where(p=>((alongX?p.x:p.z)<middle)==(side==0)).ToArray();
                    if(foot.Length==0)throw new InvalidOperationException("Missing billboard foot vertices");
                    float x=(foot.Min(p=>p.x)+foot.Max(p=>p.x))*.5f;
                    float z=(foot.Min(p=>p.z)+foot.Max(p=>p.z))*.5f;
                    var mast=masts[side];float ground=mast.GetComponent<Renderer>().bounds.min.y;
                    mast.SetPositionAndRotation(new Vector3(x,(ground+bottom)*.5f,z),Quaternion.identity);
                    mast.localScale=new Vector3(.42f,bottom-ground,.42f);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(mast);
                }
            }
        }


    }
}
