using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed class PhaisterArrivalSeal : MonoBehaviour,IVfxTimeline
    {
        private float _age;
        private Material _ink;
        public float LifeSeconds=>.65f;
        public static GameObject Create(Vector3 at,Vector3 direction)
        {
            direction.y=0;if(direction.sqrMagnitude<.001f)direction=Vector3.forward;
            var root=new GameObject("ShadowArrival");root.transform.SetPositionAndRotation(VfxShapes.GroundPoint(at),Quaternion.LookRotation(direction));
            var fold=VfxShapes.Lay(root.transform,"ArrivalFold",PhaisterSpellGeometry.Fold(),1,0);
            VfxMaterial.Ghost(fold.GetComponent<Renderer>(),new Color(.83f,.67f,.95f,.88f),.35f);
            VfxShapes.DrapeToGround(fold,.027f);
            var effect=root.AddComponent<PhaisterArrivalSeal>();effect._ink=fold.GetComponent<Renderer>().sharedMaterial;
            effect.StepTo(0);return root;
        }
        private void Update(){StepTo(_age+Time.deltaTime);if(_age>=LifeSeconds)Destroy(gameObject);}
        public void StepTo(float seconds)
        {
            _age=seconds;if(_ink==null)return;
            var color=_ink.color;float remaining=Mathf.Clamp01(1-seconds/LifeSeconds);
            color.a=.88f*remaining*remaining;_ink.color=color;
        }
    }
}
