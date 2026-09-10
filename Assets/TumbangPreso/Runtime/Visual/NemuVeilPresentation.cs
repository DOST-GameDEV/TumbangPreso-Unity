using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>A readable spectral edge and movement wisps without a body-washing light.</summary>
    public sealed class NemuVeilPresentation : MonoBehaviour, IVfxTimeline
    {
        private readonly List<Renderer> _bodies=new List<Renderer>();
        private readonly List<float> _rimRest=new List<float>();
        private readonly List<Color> _colorRest=new List<Color>();
        private readonly List<TrailRenderer> _trails=new List<TrailRenderer>();
        private MaterialPropertyBlock _block;
        private float _duration,_age;
        private bool _released;
        public float LifeSeconds=>_duration+.34f;

        public static NemuVeilPresentation Attach(CharacterMotor motor,float duration)
        {
            var root=new GameObject("NemuVeil");root.transform.SetParent(motor.transform,false);
            var effect=root.AddComponent<NemuVeilPresentation>();effect._duration=duration;
            effect._block=new MaterialPropertyBlock();
            var bounds=new Bounds(motor.transform.position+Vector3.up*.55f,new Vector3(.65f,1,.4f));
            bool found=false;
            foreach(var body in motor.GetComponentsInChildren<Renderer>())
            {
                var material=body.sharedMaterial;
                if(material==null || !material.HasProperty("_RimStrength"))continue;
                effect._bodies.Add(body);effect._rimRest.Add(material.GetFloat("_RimStrength"));
                effect._colorRest.Add(material.GetColor("_RimColor"));
                if(!found){bounds=body.bounds;found=true;}else bounds.Encapsulate(body.bounds);
            }
            // The local player sees the same spectral edge on their hands. Body
            // bounds above remain the source of trail placement, not the camera rig.
            var rig=Camera.main!=null?Camera.main.GetComponent<CameraSystem.CameraRig>():null;
            if(rig!=null && rig.IsFollowing(motor))
            {
                var arms=rig.GetComponentInChildren<CameraSystem.ViewmodelArms>();
                if(arms!=null)
                    foreach(var renderer in arms.GetComponentsInChildren<Renderer>())
                    {
                        var material=renderer.sharedMaterial;
                        if(material==null || !material.HasProperty("_RimStrength") || effect._bodies.Contains(renderer))continue;
                        effect._bodies.Add(renderer);effect._rimRest.Add(material.GetFloat("_RimStrength"));
                        effect._colorRest.Add(material.GetColor("_RimColor"));
                    }
            }
            for(int i=0;i<2;i++)
            {
                var anchor=new GameObject("VeilWisp");anchor.transform.SetParent(root.transform,false);
                anchor.transform.position=bounds.center+motor.transform.right*((i==0?-1:1)*bounds.size.x*.36f);
                var trail=anchor.AddComponent<TrailRenderer>();trail.time=.32f;trail.minVertexDistance=.06f;
                trail.widthMultiplier=.016f;
                trail.widthCurve=new AnimationCurve(new Keyframe(0,1),new Keyframe(1,0));
                VfxMaterial.Ghost(trail,new Color(.68f,.62f,.80f,.48f),.04f);
                effect._trails.Add(trail);
            }
            effect.StepTo(0);return effect;
        }
        private void Update()=>StepTo(_age+Time.deltaTime);
        public void StepTo(float seconds)
        {
            _age=seconds;if(_released)return;
            if(seconds>=_duration){Release();return;}
            for(int i=0;i<_bodies.Count;i++)
            {
                var body=_bodies[i];if(body==null)continue;
                body.GetPropertyBlock(_block);
                _block.SetFloat("_RimStrength",.32f+.045f*Mathf.Sin(seconds*7));
                _block.SetColor("_RimColor",new Color(.65f,.56f,.79f));
                body.SetPropertyBlock(_block);
            }
        }
        public void Release()
        {
            if(_released)return;_released=true;
            RestoreRims();
            foreach(var trail in _trails)if(trail!=null)trail.emitting=false;
            transform.SetParent(null,true);
            if(Application.isPlaying)Destroy(gameObject,.34f);else DestroyImmediate(gameObject);
        }
        private void RestoreRims()
        {
            for(int i=0;i<_bodies.Count;i++)
            {
                var body=_bodies[i];if(body==null)continue;
                body.GetPropertyBlock(_block);_block.SetFloat("_RimStrength",_rimRest[i]);
                _block.SetColor("_RimColor",_colorRest[i]);body.SetPropertyBlock(_block);
            }
            _bodies.Clear();
        }
        private void OnDestroy()=>RestoreRims();
    }
}
