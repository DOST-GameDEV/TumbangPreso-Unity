using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    // Authoring-only raycasts against the retained building itself. Whole-building
    // bounds include parapets and stepped wings; their highest point is not a roof.
    public sealed class RoofSurfaceSupport : IDisposable
    {
        private readonly List<MeshCollider> _surfaces=new List<MeshCollider>();
        private readonly List<MeshCollider> _temporary=new List<MeshCollider>();
        private readonly Bounds _bounds;

        public RoofSurfaceSupport(Transform building)
        {
            var filters=building.GetComponentsInChildren<MeshFilter>();
            bool first=true;
            foreach(var filter in filters)
            {
                if(filter.sharedMesh==null)continue;
                var renderer=filter.GetComponent<Renderer>();
                if(renderer==null)continue;
                if(first){_bounds=renderer.bounds;first=false;}else _bounds.Encapsulate(renderer.bounds);
                var collider=filter.GetComponent<MeshCollider>();
                if(collider==null){collider=filter.gameObject.AddComponent<MeshCollider>();_temporary.Add(collider);}
                collider.sharedMesh=filter.sharedMesh;
                _surfaces.Add(collider);
            }
            Physics.SyncTransforms();
        }

        internal bool Height(Vector3 point,out float height)
        {
            height=float.NegativeInfinity;
            var ray=new Ray(new Vector3(point.x,_bounds.max.y+1,point.z),Vector3.down);
            foreach(var surface in _surfaces)
                if(surface.Raycast(ray,out var hit,_bounds.size.y+1)&&hit.normal.y>.65f)
                    height=Mathf.Max(height,hit.point.y);
            return height>_bounds.min.y+.8f;
        }

        private bool Footprint(Vector3 centre,Vector3 extent,out float height)
        {
            height=float.NegativeInfinity;float low=float.PositiveInfinity;
            foreach(float x in new[]{-1f,0f,1f})
            foreach(float z in new[]{-1f,0f,1f})
            {
                if(!Height(centre+new Vector3(x*extent.x,0,z*extent.z),out float y))return false;
                height=Mathf.Max(height,y);low=Mathf.Min(low,y);
            }
            return height-low<.045f;
        }

        public bool Seat(Transform attachment)
        {
            var own=attachment.GetComponent<Renderer>();
            var renderers=attachment.GetComponentsInChildren<Renderer>();
            if(renderers.Length==0)return false;
            // Aerial crossarms are carried by the pole, not directly by the roof.
            var bounds=own!=null?own.bounds:renderers[0].bounds;
            if(own==null)foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            var extent=new Vector3(Mathf.Max(.025f,bounds.extents.x*.96f),0,Mathf.Max(.025f,bounds.extents.z*.96f));
            var candidates=new List<Vector3>{bounds.center};
            for(int x=1;x<16;x++)for(int z=1;z<16;z++)
                candidates.Add(new Vector3(Mathf.Lerp(_bounds.min.x,_bounds.max.x,x/16f),0,
                                           Mathf.Lerp(_bounds.min.z,_bounds.max.z,z/16f)));
            foreach(var candidate in candidates.OrderBy(p=>new Vector2(p.x-bounds.center.x,p.z-bounds.center.z).sqrMagnitude))
            {
                if(!Footprint(candidate,extent,out float y))continue;
                attachment.position+=new Vector3(candidate.x-bounds.center.x,y-bounds.min.y,candidate.z-bounds.center.z);
                return true;
            }
            Debug.Log("[Roof support] Omitted "+attachment.name+": no flat supported footprint on "+_bounds);
            return false;
        }

        public void Dispose(){foreach(var collider in _temporary)Object.DestroyImmediate(collider);}
    }
}
