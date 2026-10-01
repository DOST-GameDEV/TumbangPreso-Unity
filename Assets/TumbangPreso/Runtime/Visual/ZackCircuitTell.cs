using TumbangPreso.Abilities;
using TumbangPreso.Net;
using UnityEngine;

namespace TumbangPreso.Visual
{
    // A target-facing acquisition tell, never a hit or a gameplay collider.
    public sealed class ZackCircuitTell : MonoBehaviour
    {
        private CharacterMotor _caster;
        private ZackHeroKit _kit;
        private LineRenderer _line;
        private Material _material;
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
        }
        private void LateUpdate()
        {
            if(_caster==null || _caster.AbilitySystem?.Kit!=_kit){Destroy(gameObject);return;}
            var target=GameServices.Round?.PlayerAt(_kit.CircuitTarget);
            bool visible=_kit.CircuitStage==CircuitPhase.Acquiring && _kit.CircuitRemaining>0
                && target!=null && GameServices.Round?.RoundActive==true;
            _line.enabled=visible;if(!visible)return;
            Vector3 from=_caster.transform.position+Vector3.up*1.05f+_caster.transform.right*.14f;
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
