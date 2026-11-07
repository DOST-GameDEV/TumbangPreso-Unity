using UnityEngine;

namespace TumbangPreso.Visual
{
    // Shared visual support queries. No collider or gameplay position is changed.
    public static class WorldGround
    {
        // ⚠️ STEP DOWN THE NEAREST HITS; DO NOT COLLECT EVERY HIT (Banaue, 2026-11-05). The highest valid support is the
        // first valid surface going down, so one closest-hit cast normally answers. Collecting all hits filled a 64-slot
        // buffer under Banaue's stacked terraces and then fell back to RaycastAll, which allocated on every call. Those
        // calls ran every frame for each contact shadow, the dust and the 192-point throw ring: GC spikes. Backface
        // queries are off and a cast that starts inside a collider does not report it, so continuing just under a
        // rejected hit gives the same answer as the old maximum over all hits.
        private const int MaxSteps = 64;
        private const float StepPast = .001f;
        public static float UnderFeet(Vector3 feet) =>
            TryBelow(feet, .25f, 12f, out float floor) ? floor : feet.y;
        public static bool TryBelow(Vector3 point,float rise,float reach,out float height)
        {
            height=float.NegativeInfinity;
            var from=point+Vector3.up*rise;
            float left=reach;
            for(int step=0;step<MaxSteps && left>0;step++)
            {
                if(!Physics.Raycast(from,Vector3.down,out var hit,left,~0,QueryTriggerInteraction.Ignore))return false;
                var collider=hit.collider;
                if(collider!=null && hit.normal.y>=.65f && collider.GetComponentInParent<CharacterMotor>()==null &&
                    collider.GetComponentInParent<Slipper>()==null && collider.GetComponentInParent<Lata>()==null)
                { height=hit.point.y; return true; }
                float advance=hit.distance+StepPast;
                from+=Vector3.down*advance; left-=advance;
            }
            return false;
        }
    }
}
