using UnityEngine;

namespace TumbangPreso.Visual
{
    // Shared visual support queries. No collider or gameplay position is changed.
    public static class WorldGround
    {
        private static readonly RaycastHit[] Hits=new RaycastHit[32];
        public static bool TryBelow(Vector3 point,float rise,float reach,out float height)
        {
            int count=Physics.RaycastNonAlloc(point+Vector3.up*rise,Vector3.down,Hits,reach,~0,QueryTriggerInteraction.Ignore);
            height=float.NegativeInfinity;
            for(int i=0;i<count;i++)
            {
                var collider=Hits[i].collider;
                if(collider==null || Hits[i].normal.y<.65f || collider.GetComponentInParent<CharacterMotor>()!=null ||
                    collider.GetComponentInParent<Slipper>()!=null || collider.GetComponentInParent<Lata>()!=null)continue;
                height=Mathf.Max(height,Hits[i].point.y);
            }
            return !float.IsNegativeInfinity(height);
        }
    }
}
