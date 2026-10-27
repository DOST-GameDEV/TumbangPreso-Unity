using System;
using UnityEngine;

namespace TumbangPreso.PlayTests.Trailer
{
    /// <summary>Grip the end of the actual model geometry, independent of its imported root pivot.</summary>
    public sealed class TrailerPropGrip
    {
        private readonly Transform _root;
        private readonly Vector3 _centre;
        private readonly bool _longX;
        private readonly float _length;
        public TrailerPropGrip(GameObject prop)
        {
            _root=prop.transform;var bounds=new Bounds();bool found=false;
            foreach(var filter in prop.GetComponentsInChildren<MeshFilter>(false))
            {
                if(filter.sharedMesh==null)continue;
                var box=filter.sharedMesh.bounds;var matrix=_root.worldToLocalMatrix*filter.transform.localToWorldMatrix;
                for(int i=0;i<8;i++)
                {
                    Vector3 corner=box.center+Vector3.Scale(box.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                    Vector3 point=matrix.MultiplyPoint3x4(corner);
                    if(!found){bounds=new Bounds(point,Vector3.zero);found=true;}else bounds.Encapsulate(point);
                }
            }
            if(!found)throw new InvalidOperationException("No prop geometry for a cinematic grip");
            _centre=bounds.center;_longX=bounds.size.x>bounds.size.z;
            _length=_longX?bounds.size.x*_root.lossyScale.x:bounds.size.z*_root.lossyScale.z;
            Debug.Log($"[TrailerPropGrip] {prop.name}: centre={_centre} longX={_longX} length={_length:F3}");
        }
        public void Pose(Vector3 hand,Vector3 fore,Vector3 face,out Vector3 position,out Quaternion turn)
        {
            Vector3 toe=(Vector3.up*.92f+fore*.35f).normalized;
            Vector3 normal=Vector3.ProjectOnPlane(face,toe).normalized;
            if(normal.sqrMagnitude<.1f)normal=Vector3.ProjectOnPlane(Vector3.forward,toe).normalized;
            turn=_longX?Quaternion.LookRotation(Vector3.Cross(toe,normal),normal):Quaternion.LookRotation(toe,normal);
            Vector3 centre=hand+fore*.07f+toe*Mathf.Max(.03f,_length*.5f-.03f);
            position=centre-turn*Vector3.Scale(_centre,_root.lossyScale);
        }
        public Vector3 CentreAt(Vector3 rootPosition,Quaternion turn)=>rootPosition+turn*Vector3.Scale(_centre,_root.lossyScale);
        public Vector3 RootAtCentre(Vector3 centre,Quaternion turn)=>centre-turn*Vector3.Scale(_centre,_root.lossyScale);
    }
}
