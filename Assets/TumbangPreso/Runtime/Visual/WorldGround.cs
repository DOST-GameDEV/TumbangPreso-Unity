using UnityEngine;

namespace TumbangPreso.Visual
{
    // Shared visual support queries. No collider or gameplay position is changed.
    public static class WorldGround
    {
        private static readonly RaycastHit[] Hits=new RaycastHit[64];
        public static float UnderFeet(Vector3 feet) =>
            TryBelow(feet, .25f, 12f, out float floor) ? floor : feet.y;
        public static bool TryBelow(Vector3 point,float rise,float reach,out float height)
        {
            var hits = Hits;
            int count=Physics.RaycastNonAlloc(point+Vector3.up*rise,Vector3.down,hits,reach,~0,QueryTriggerInteraction.Ignore);
            if (count == hits.Length)
            { hits = Physics.RaycastAll(point+Vector3.up*rise,Vector3.down,reach,~0,QueryTriggerInteraction.Ignore); count = hits.Length; }
            height=float.NegativeInfinity;
            for(int i=0;i<count;i++)
            {
                var collider=hits[i].collider;
                if(collider==null || hits[i].normal.y<.65f || collider.GetComponentInParent<CharacterMotor>()!=null ||
                    collider.GetComponentInParent<Slipper>()!=null || collider.GetComponentInParent<Lata>()!=null)continue;
                height=Mathf.Max(height,hits[i].point.y);
            }
            return !float.IsNegativeInfinity(height);
        }
    }
}
