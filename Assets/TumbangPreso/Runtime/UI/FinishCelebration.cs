using UnityEngine;

namespace TumbangPreso.UI
{
    // The original winner rig on the existing isolated preview stage. This never
    // animates a live player or creates a gameplay emote, reward or network event.
    [DefaultExecutionOrder(1100)]
    public sealed class FinishCelebration : MonoBehaviour
    {
        private ModelPreview _preview;
        private Transform _torso,_head,_left,_right;
        private Quaternion _torsoRest,_headRest,_leftRest,_rightRest;
        private float _began;
        private bool _applied;
        public void Bind(ModelPreview preview)
        {
            Restore();_preview=preview;_torso=_head=_left=_right=null;_began=-1;
            if(preview.Subject==null)return;
            foreach(var bone in preview.Subject.GetComponentsInChildren<Transform>(true))
            {
                if(bone.name=="torso")_torso=bone;
                else if(bone.name=="head")_head=bone;
                else if(bone.name=="arm-left")_left=bone;
                else if(bone.name=="arm-right")_right=bone;
            }
        }
        private void Restore()
        {
            if(!_applied)return;
            if(_torso!=null)_torso.localRotation=_torsoRest;
            if(_head!=null)_head.localRotation=_headRest;
            if(_left!=null)_left.localRotation=_leftRest;
            if(_right!=null)_right.localRotation=_rightRest;
            _applied=false;
        }
        private void Update()=>Restore();
        private void OnDisable()=>Restore();
        private void LateUpdate()
        {
            if(_preview==null||!_preview.isActiveAndEnabled||_torso==null)return;
            if(_began<0)_began=Time.unscaledTime;
            bool reduced=Settings.SettingsStore.Current.ReducedUiMotion;
            _preview.AnimateSubject=!reduced;
            float age=reduced?1.2f:Time.unscaledTime-_began;
            float rise=Mathf.SmoothStep(0,1,Mathf.Clamp01(age/.32f));
            float settle=Mathf.SmoothStep(0,1,Mathf.Clamp01((age-.58f)/.62f));
            float accent=rise*(1-settle);
            _torsoRest=_torso.localRotation;_torso.localRotation=_torsoRest*Quaternion.Euler(-3*accent,-6*accent,-3*accent);
            if(_head!=null){_headRest=_head.localRotation;_head.localRotation=_headRest*Quaternion.Euler(-6*accent,5*accent,0);}
            if(_left!=null){_leftRest=_left.localRotation;_left.localRotation=_leftRest*Quaternion.Euler(-18*rise,0,12*rise);}
            if(_right!=null){_rightRest=_right.localRotation;_right.localRotation=_rightRest*Quaternion.Euler(-50*rise-45*accent,0,-22*rise-28*accent);}
            _applied=true;
        }
    }
}
