using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>Keep a scouting familiar and its recall destination inside traversable space.</summary>
    public static class GhostPetMotion
    {
        public static Vector3 ClampToCourt(CharacterMotor owner,Vector3 position)
        {
            float x=owner!=null && owner.IsDefender?Core.Balance.ConfinementRadius:AIController.PlayableHalfX;
            float z=owner!=null && owner.IsDefender?Core.Balance.ConfinementRadius:AIController.PlayableHalfZ;
            position.x=Mathf.Clamp(position.x,-x,x);position.z=Mathf.Clamp(position.z,-z,z);
            return position;
        }
        private static void Capsule(CharacterMotor owner,Vector3 ground,bool allowSteps,out Vector3 low,out Vector3 high,out float radius)
        {
            var cc=owner!=null?owner.GetComponent<CharacterController>():null;
            radius=cc!=null?cc.radius:.32f;
            float height=cc!=null?cc.height:1.7f;
            float step=allowSteps && cc!=null?cc.stepOffset:0;
            low=ground+Vector3.up*(radius+step+.025f);
            high=ground+Vector3.up*Mathf.Max(radius+step+.025f,height-radius+.025f);
        }
        private static bool Ignore(Collider collider,CharacterMotor owner,bool actors)
        {
            if(collider.GetComponentInParent<Slipper>()!=null)return true;
            var motor=collider.GetComponentInParent<CharacterMotor>();
            return motor!=null && (!actors || motor==owner);
        }
        public static bool CanLand(CharacterMotor owner,Vector3 ground,bool actors=true)
        {
            Capsule(owner,ground,false,out var low,out var high,out float radius);
            foreach(var collider in Physics.OverlapCapsule(low,high,radius,~0,QueryTriggerInteraction.Ignore))
                if(!Ignore(collider,owner,actors))return false;
            return true;
        }
        public static Vector3 Move(CharacterMotor owner,Vector3 from,Vector3 displacement)
        {
            var position=ClampToCourt(owner,from);
            var wanted=ClampToCourt(owner,position+displacement);
            Vector3 remaining=wanted-position;remaining.y=0;
            // Two swept segments permit sliding along a wall without tunnelling.
            for(int pass=0;pass<2 && remaining.sqrMagnitude>.000001f;pass++)
            {
                var ground=VfxShapes.GroundPoint(position);
                Capsule(owner,ground,true,out var low,out var high,out float radius);
                float distance=remaining.magnitude;Vector3 direction=remaining/distance;
                float travel=distance;Vector3 normal=Vector3.zero;
                foreach(var hit in Physics.CapsuleCastAll(low,high,radius,direction,distance,~0,QueryTriggerInteraction.Ignore))
                {
                    if(Ignore(hit.collider,owner,false) || hit.normal.y>.65f)continue;
                    if(hit.distance<travel){travel=Mathf.Max(0,hit.distance-.025f);normal=hit.normal;}
                }
                position+=direction*travel;
                remaining=normal==Vector3.zero?Vector3.zero:Vector3.ProjectOnPlane(direction*(distance-travel),normal);
                remaining.y=0;
            }
            return ClampToCourt(owner,position);
        }
        public static Vector3 Recall(CharacterMotor owner,Vector3 at,Vector3 lastSafe)
        {
            var centre=VfxShapes.GroundPoint(ClampToCourt(owner,at));
            if(CanLand(owner,centre))return centre;
            for(int ring=1;ring<=2;ring++)
                for(int i=0;i<8;i++)
                {
                    float angle=i*Mathf.PI*.25f;
                    var candidate=VfxShapes.GroundPoint(ClampToCourt(owner,centre+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*(ring*.5f)));
                    if(CanLand(owner,candidate))return candidate;
                }
            var fallback=VfxShapes.GroundPoint(ClampToCourt(owner,lastSafe));
            return CanLand(owner,fallback)?fallback:owner.transform.position;
        }
    }
}
