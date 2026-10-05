using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// The drone that carries a fallen body back onto the Arena's stage. With the stage kit's
    /// model (a TEMPLATE the scene builder leaves in the scene, already wearing the map's
    /// materials: parts `drone_body`, `drone_rotor_0..3` and `drone_beam`, its nose along +z) it
    /// is a copy of that; without one it is the grey-box: a flat box with four rotors and a beam
    /// under it, built from primitives.
    ///
    /// ⚠️ IT DECIDES NOTHING. It hovers over a body for as long as that body's replicated
    /// `EdgeKind` is `Drone` and flies off when it is not, so every peer shows the same carry
    /// from the fields `SyncUnit` already brings. The host's `CharacterMotor.StepDroneCarry`
    /// owns where the body goes.
    /// </summary>
    public sealed class ArenaDrone : MonoBehaviour
    {
        public const float Hover=2.6f,ArriveFrom=4.5f,LeaveSeconds=.7f,LeaveSpeed=9;
        /// <summary>The model's beam is a cone this long, hung from the drone's belly at scale 1.</summary>
        private const float ModelBeam=2.5f;
        private bool _modelled;
        private readonly Transform[] _rotors=new Transform[4];
        private CharacterMotor _body;
        private Transform _beam;
        private float _leaving;

        public static ArenaDrone Build(Transform parent,GameObject template=null)
        {
            if(template!=null)
            {
                var copy=Instantiate(template,parent,false);copy.name="Arena drone";copy.SetActive(false);
                var modelled=copy.GetComponent<ArenaDrone>();if(modelled==null)modelled=copy.AddComponent<ArenaDrone>();
                int found=0;
                foreach(var part in copy.GetComponentsInChildren<Transform>(true))
                {
                    if(part.name.StartsWith("drone_rotor",System.StringComparison.Ordinal)&&found<modelled._rotors.Length)modelled._rotors[found++]=part;
                    else if(part.name.StartsWith("drone_beam",System.StringComparison.Ordinal))modelled._beam=part;
                }
                // A template with no beam or with rotors missing is not the kit's drone: the grey-box is drawn.
                if(modelled._beam!=null&&found==modelled._rotors.Length){modelled._modelled=true;return modelled;}
                Destroy(copy);
            }
            var root=new GameObject("Arena drone");root.transform.SetParent(parent,false);
            var drone=root.AddComponent<ArenaDrone>();
            Part(PrimitiveType.Cube,"Body",root.transform,Vector3.zero,new Vector3(1.1f,.26f,1.1f),new Color(.42f,.45f,.50f),false);
            for(int i=0;i<4;i++)
            {
                var at=new Vector3(i%2==0?-.62f:.62f,.16f,i<2?-.62f:.62f);
                drone._rotors[i]=Part(PrimitiveType.Cylinder,"Rotor",root.transform,at,new Vector3(.52f,.02f,.52f),new Color(.20f,.22f,.26f),false);
            }
            // A Unity cylinder is 2 m tall at scale 1: the beam is scaled and hung in LateUpdate.
            drone._beam=Part(PrimitiveType.Cylinder,"Beam",root.transform,Vector3.zero,Vector3.one,new Color(.55f,.95f,1.0f,.35f),true);
            root.SetActive(false);
            return drone;
        }

        private static Transform Part(PrimitiveType shape,string name,Transform parent,Vector3 at,Vector3 scale,Color colour,bool beam)
        {
            var go=GameObject.CreatePrimitive(shape);go.name=name;
            // Decoration only. A primitive's collider would be a floor for the body's own probes.
            Visual.VfxMaterial.StripCollider(go);
            go.transform.SetParent(parent,false);go.transform.localPosition=at;go.transform.localScale=scale;
            var renderer=go.GetComponent<Renderer>();
            if(beam)Visual.VfxMaterial.Ghost(renderer,colour,.8f);else Visual.VfxMaterial.Solid(renderer,colour);
            renderer.shadowCastingMode=beam?UnityEngine.Rendering.ShadowCastingMode.Off:UnityEngine.Rendering.ShadowCastingMode.On;
            return go.transform;
        }

        public void Attend(CharacterMotor body)
        {
            if(_body==body&&_leaving<=0&&gameObject.activeSelf)return;
            _body=body;_leaving=0;gameObject.SetActive(true);Place();
        }

        private bool Carrying=>_body!=null&&_body.gameObject.activeInHierarchy&&_body.EdgeKind==EdgeRecoveryKind.Drone;

        private void LateUpdate()
        {
            if(Carrying){_leaving=0;Place();}
            else
            {
                // Set down, or the round reset under it: let go and climb away.
                _leaving+=Time.deltaTime;_beam.gameObject.SetActive(false);
                transform.position+=Vector3.up*LeaveSpeed*Time.deltaTime;
                if(_leaving>=LeaveSeconds){_body=null;gameObject.SetActive(false);}
            }
            for(int i=0;i<_rotors.Length;i++)_rotors[i].Rotate(0,(i%2==0?1400:-1400)*Time.deltaTime,0,Space.Self);
        }

        private void Place()
        {
            // It drops onto the falling body over the catch and holds its height after.
            float arrive=_body.EdgePhase==0?Mathf.SmoothStep(0,1,_body.EdgePhaseRatio):1;
            var feet=_body.transform.position;
            transform.SetPositionAndRotation(feet+Vector3.up*(Hover+ArriveFrom*(1-arrive)),Quaternion.Euler(0,_body.transform.eulerAngles.y,0));
            // The beam reaches from the drone's belly to the shoulders, and only once it has the body.
            float length=(Hover-1.25f)*arrive;
            _beam.gameObject.SetActive(length>.05f);
            if(_modelled){_beam.localPosition=Vector3.zero;_beam.localScale=new Vector3(1,length/ModelBeam,1);return;}
            _beam.localPosition=Vector3.down*(.13f+length*.5f);
            _beam.localScale=new Vector3(.55f,length*.5f,.55f);
        }
    }
}
