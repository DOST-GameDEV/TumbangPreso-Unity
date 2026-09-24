using UnityEngine;

namespace TumbangPreso
{
    public enum EdgeRecoveryKind : byte { None, Rooftop, Lagoon }

    /// <summary>A real grip and supported landing, never a round spawn marker.</summary>
    public readonly struct MapEdgeAnchor
    {
        public readonly EdgeRecoveryKind Kind;
        public readonly Vector3 Grip, Landing, Outward;
        public MapEdgeAnchor(EdgeRecoveryKind kind,Vector3 grip,Vector3 landing,Vector3 outward)
        {Kind=kind;Grip=grip;Landing=landing;Outward=outward;}
    }

    public static class MapEdgeGeometry
    {
        private static readonly Collider[] LandingHits=new Collider[24];
        private static Transform _roofRoot,_waterRoot;
        private static BoxCollider[] _roofBoxes,_waterBoxes;

        public static bool TryRooftop(CharacterMotor actor,out MapEdgeAnchor anchor,bool emergency=false)
        {
            anchor=default;var roof=RooftopRecovery.Instance;
            if(roof==null||actor==null||!RooftopRecovery.OutsideDeck(actor.transform.position))return false;
            var p=actor.transform.position;float best=float.PositiveInfinity;
            if(_roofRoot!=roof.transform||_roofBoxes==null){_roofRoot=roof.transform;_roofBoxes=roof.GetComponentsInChildren<BoxCollider>();}
            foreach(var rail in _roofBoxes)
            {
                if(rail==null||rail.name!="Rail collision"||!rail.enabled)continue;
                var bounds=rail.bounds;bool alongZ=bounds.size.z>bounds.size.x;
                var outward=alongZ?Vector3.right*Mathf.Sign(bounds.center.x):Vector3.forward*Mathf.Sign(bounds.center.z);
                var grip=bounds.center;grip.y=bounds.max.y;
                if(alongZ){grip.x+=outward.x*bounds.extents.x;grip.z=Mathf.Clamp(p.z,bounds.min.z+.6f,bounds.max.z-.6f);}
                else{grip.z+=outward.z*bounds.extents.z;grip.x=Mathf.Clamp(p.x,bounds.min.x+.6f,bounds.max.x-.6f);}
                float distance=Vector3.ProjectOnPlane(p-grip,Vector3.up).sqrMagnitude;
                if((!emergency&&distance>1.7f*1.7f)||distance>=best)continue;
                var landing=grip-outward*.95f;landing.y=RooftopRecovery.RoofY+.02f;
                if(!ClearLanding(actor,landing))continue;
                best=distance;anchor=new MapEdgeAnchor(EdgeRecoveryKind.Rooftop,grip,landing,outward);
            }
            return anchor.Kind!=EdgeRecoveryKind.None;
        }

        public static bool TryLagoon(CharacterMotor actor,out MapEdgeAnchor anchor)
        {
            anchor=default;var water=LagoonWater.Instance;
            if(water==null||!water.Active||actor==null||!actor.IsSwimming)return false;
            var root=water.transform;var floor=root.Find("Floor");var paths=root.Find("Village circulation");
            if(_waterRoot!=root||_waterBoxes==null){_waterRoot=root;_waterBoxes=root.GetComponentsInChildren<BoxCollider>();}
            var colliders=_waterBoxes;var p=actor.transform.position;float best=float.PositiveInfinity;
            foreach(var deck in colliders)
            {
                if(deck==null||!deck.enabled||deck.name!="Continuous deck collision")continue;
                bool publicDeck=(floor!=null&&deck.transform.IsChildOf(floor))||(paths!=null&&deck.transform.IsChildOf(paths))||deck.transform.parent.name=="Boat landing";
                if(!publicDeck)continue;
                var local=deck.transform.InverseTransformPoint(p)-deck.center;var half=deck.size*.5f;
                float outsideX=Mathf.Abs(local.x)-half.x,outsideZ=Mathf.Abs(local.z)-half.z;
                // Reach the outside edge first. A jump under the middle of a solid
                // deck must not pull a body through its underside.
                if(outsideX<-.03f&&outsideZ<-.03f)continue;
                bool xSide=outsideX>outsideZ;
                var normal=xSide?Vector3.right*Mathf.Sign(local.x):Vector3.forward*Mathf.Sign(local.z);
                var edge=new Vector3(Mathf.Clamp(local.x,-half.x+.50f,half.x-.50f),half.y,Mathf.Clamp(local.z,-half.z+.50f,half.z-.50f));
                if(xSide)edge.x=normal.x*half.x;else edge.z=normal.z*half.z;
                var grip=deck.transform.TransformPoint(deck.center+edge);
                var outward=deck.transform.TransformDirection(normal).normalized;
                float distance=Vector3.ProjectOnPlane(p-grip,Vector3.up).sqrMagnitude;
                if(distance>1.30f*1.30f||distance>=best||grip.y-p.y>2.65f)continue;
                var landing=grip-outward*.80f;landing.y+=.02f;
                if(!ClearLanding(actor,landing))continue;
                // Existing supported rails sit at the deck lip. Use their actual
                // top where present, rather than reaching through a rail to a plank.
                foreach(var rail in colliders)
                {
                    if(rail==null||!rail.enabled||rail.name!="Supported handrail")continue;
                    var b=rail.bounds;var q=b.ClosestPoint(new Vector3(grip.x,b.center.y,grip.z));
                    if(Vector3.ProjectOnPlane(q-grip,Vector3.up).sqrMagnitude>.22f*.22f||b.max.y<grip.y||b.max.y>grip.y+1.2f)continue;
                    grip=new Vector3(q.x,b.max.y,q.z);break;
                }
                best=distance;anchor=new MapEdgeAnchor(EdgeRecoveryKind.Lagoon,grip,landing,outward);
            }
            return anchor.Kind!=EdgeRecoveryKind.None;
        }

        private static bool ClearLanding(CharacterMotor actor,Vector3 feet)
        {
            var cc=actor.GetComponent<CharacterController>();if(cc==null)return false;
            float radius=Mathf.Max(.1f,cc.radius-.025f),half=Mathf.Max(0,cc.height*.5f-radius);
            var centre=feet+cc.center;int count=Physics.OverlapCapsuleNonAlloc(centre-Vector3.up*half,centre+Vector3.up*half,radius,LandingHits,~0,QueryTriggerInteraction.Ignore);
            if(count==LandingHits.Length)return false;
            for(int i=0;i<count;i++)
            {
                var hit=LandingHits[i];
                if(hit.GetComponentInParent<CharacterMotor>()!=null)continue;
                // Supported feet may touch their landing plane within the controller skin.
                if(hit.bounds.max.y<=feet.y+.04f)continue;
                return false;
            }
            return true;
        }
    }
}
