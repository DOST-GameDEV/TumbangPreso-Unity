using TumbangPreso.Abilities;
using TumbangPreso.Net;
using TumbangPreso.CameraSystem;
using UnityEngine;

namespace TumbangPreso.Visual
{
    // A target-facing acquisition tell, never a hit or a gameplay collider.
    [DefaultExecutionOrder(1100)]
    public sealed class ZackCircuitTell : MonoBehaviour
    {
        private CharacterMotor _caster;
        private ZackHeroKit _kit;
        private LineRenderer _line;
        private Material _material;
        private Transform _leftArm;
        private Vector3 _leftPalm;
        private ViewmodelArms _ownerArms;
        public static void Ensure(CharacterMotor caster,ZackHeroKit kit)
        {
            if(caster==null || kit==null)return;
            var tell=caster.GetComponentInChildren<ZackCircuitTell>();
            if(tell==null)
            {
                var go=new GameObject("Closed Circuit acquisition tell");go.transform.SetParent(caster.transform,false);
                tell=go.AddComponent<ZackCircuitTell>();tell._line=go.AddComponent<LineRenderer>();
                tell._line.positionCount=4;tell._line.useWorldSpace=true;
                tell._line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;tell._line.receiveShadows=false;
                tell._line.textureMode=LineTextureMode.Stretch;tell._line.numCapVertices=0;tell._line.numCornerVertices=0;
                tell._material=new Material(Shader.Find("TumbangPreso/WorldClock")){name="Closed Circuit tell"};
                tell._material.SetColor("_Face",new Color(1,.9f,.22f));tell._material.SetFloat("_Fill",1);
                tell._line.sharedMaterial=tell._material;tell._line.enabled=false;
            }
            tell._caster=caster;tell._kit=kit;
            tell.BindCastingHand();
        }
        private void BindCastingHand()
        {
            _leftArm=null;_ownerArms=null;
            var visual=_caster.GetComponent<CharacterVisual>();
            var skin=visual!=null && visual.Model!=null
                ? visual.Model.GetComponentInChildren<SkinnedMeshRenderer>() : null;
            if(skin!=null)
                for(int i=0;i<skin.bones.Length;i++)
                    if(skin.bones[i]!=null && skin.bones[i].name=="arm-left"
                        && CharacterVisual.PalmCentre(skin,i,out var palm))
                    { _leftArm=skin.bones[i];_leftPalm=palm;break; }
            // Resolve once per acquisition, never a scene search in LateUpdate.
            if(ViewmodelArms.IsFirstPersonFor(_caster))
                foreach(var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None))
                    if(arms.BoundCharacter==_caster){_ownerArms=arms;break;}
        }
        private Vector3 CastingHand()
        {
            if(_ownerArms!=null && ViewmodelArms.IsFirstPersonFor(_caster))
            {
                var hand=_ownerArms.LeftHandForProps();
                if(hand!=null)return hand.TransformPoint(_ownerArms.LeftPalmOffset());
            }
            return _leftArm!=null ? _leftArm.TransformPoint(_leftPalm)
                : _caster.transform.position+Vector3.up*1.05f+_caster.transform.right*.14f;
        }
        private void LateUpdate()
        {
            if(_caster==null || _caster.AbilitySystem?.Kit!=_kit){Destroy(gameObject);return;}
            var target=GameServices.Round?.PlayerAt(_kit.CircuitTarget);
            bool visible=_kit.CircuitStage==CircuitPhase.Acquiring && _kit.CircuitRemaining>0
                && target!=null && GameServices.Round?.RoundActive==true;
            _line.enabled=visible;if(!visible)return;
            Vector3 from=CastingHand();
            var capsule=target.GetComponent<CharacterController>();
            Vector3 to=target.transform.position+(capsule!=null?capsule.center:Vector3.up*.8f);
            Vector3 side=Vector3.Cross((to-from).normalized,Vector3.up);
            float progress=1-Mathf.Clamp01(_kit.CircuitRemaining/.4f);
            _line.startWidth=.018f;_line.endWidth=.026f;
            _line.SetPosition(0,from);_line.SetPosition(1,Vector3.Lerp(from,to,.36f)+side*.035f);
            _line.SetPosition(2,Vector3.Lerp(from,to,.68f)-side*.025f);_line.SetPosition(3,to);
            _material.SetFloat("_Weight",.45f+progress*.45f);
        }
        private void OnDestroy(){if(_material!=null)Destroy(_material);}
    }
}
