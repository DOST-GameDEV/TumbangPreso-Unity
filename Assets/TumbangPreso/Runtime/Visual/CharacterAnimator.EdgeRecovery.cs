using System.Linq;
using UnityEngine;
using UnityEngine.Playables;

namespace TumbangPreso.Visual
{
    public sealed partial class CharacterAnimator
    {
        private Transform[] _edgeBones;
        private readonly Vector3[] _edgeRestPositions=new Vector3[7];
        private readonly Quaternion[] _edgeRestRotations=new Quaternion[7];
        private Vector3 _edgeLeftPalm,_edgeRightPalm;
        private bool _edgePoseApplied,_edgePlaying;
        public float EdgeGripError {get;private set;}
        public bool EdgeRigReady=>_edgeBones!=null;

        private bool ResolveEdgeRig()
        {
            if(_edgeBones!=null&&_edgeBones[0]!=null)return true;
            if(_animator==null)return false;
            var names=new[]{"root","torso","head","arm-left","arm-right","leg-left","leg-right"};
            var all=_animator.GetComponentsInChildren<Transform>(true);
            var bones=names.Select(name=>all.FirstOrDefault(t=>t.name==name)).ToArray();
            if(bones.Any(t=>t==null))return false;
            bool left=false,right=false;
            foreach(var skin in _motor.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if(skin.sharedMesh==null||!skin.sharedMesh.isReadable)continue;
                for(int i=0;i<skin.bones.Length;i++)
                {
                    if(skin.bones[i]==bones[3]&&CharacterVisual.PalmCentre(skin,i,out var l)){_edgeLeftPalm=l;left=true;}
                    if(skin.bones[i]==bones[4]&&CharacterVisual.PalmCentre(skin,i,out var r)){_edgeRightPalm=r;right=true;}
                }
            }
            if(!left||!right)return false;
            _edgeBones=bones;return true;
        }

        private void RestoreEdgeRecoveryPose()
        {
            if(!_edgePoseApplied||_edgeBones==null)return;
            for(int i=0;i<_edgeBones.Length;i++)if(_edgeBones[i]!=null)
            {_edgeBones[i].localPosition=_edgeRestPositions[i];_edgeBones[i].localRotation=_edgeRestRotations[i];}
            _edgePoseApplied=false;
        }

        private bool StepEdgeRecoveryPose()
        {
            if(_motor==null||!_motor.IsEdgeRecovering)
            {
                if(_edgePlaying){var previous=Front();if(previous.IsValid())previous.SetSpeed(1);}
                _edgePlaying=false;return false;
            }
            _oneShotLeft=0;_holdAtEnd=false;_tripPhase=0;_chargePosing=false;_throwReleaseTime=-1;_throwCancelTime=-1;
            if(!_edgePlaying){CameraSystem.CameraRig.CancelViewmodelAction(_motor);Play(Idle,true,true);_edgePlaying=true;}
            var front=Front();if(front.IsValid()){front.SetSpeed(0);front.SetTime(0);}
            ResolveEdgeRig();Blend();return true;
        }

        private void ApplyEdgeRecoveryPose()
        {
            if(_motor==null||!_motor.IsEdgeRecovering||!ResolveEdgeRig())return;
            for(int i=0;i<7;i++){_edgeRestPositions[i]=_edgeBones[i].localPosition;_edgeRestRotations[i]=_edgeBones[i].localRotation;}
            _edgePoseApplied=true;
            float phase=_motor.EdgePhaseRatio;
            float reach=_motor.EdgePhase==0?Mathf.SmoothStep(0,1,phase):1;
            float pull=_motor.EdgePhase==2?phase:0;
            float release=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.58f,.96f,pull));
            float contact=reach*release;
            var root=_edgeBones[0];var torso=_edgeBones[1];var head=_edgeBones[2];var left=_edgeBones[3];var right=_edgeBones[4];
            var across=_motor.transform.right;
            float effort=_motor.EdgePhase==1?Mathf.Sin(Time.time*7)*.018f:0;
            float lean=Mathf.Lerp(10,44,Mathf.Sin(pull*Mathf.PI))*reach*release;
            torso.rotation=Quaternion.AngleAxis(lean,across)*torso.rotation;
            head.rotation=Quaternion.AngleAxis(-lean*.65f,across)*head.rotation;
            for(int i=0;i<2;i++)
            {
                float kick=_motor.EdgePhase==1?Mathf.Sin(Time.time*5+i*Mathf.PI)*7:0;
                float fold=Mathf.Lerp(10+kick,-72,Mathf.Sin(pull*Mathf.PI))*reach*release;
                _edgeBones[5+i].rotation=Quaternion.AngleAxis(fold,across)*_edgeBones[5+i].rotation;
            }
            var leftBase=left.rotation;var rightBase=right.rotation;var rootBase=root.position;
            float length=(left.TransformVector(_edgeLeftPalm).magnitude+right.TransformVector(_edgeRightPalm).magnitude)*.5f;
            float span=Vector3.Distance(left.position,right.position)*.5f+length*.40f;
            var tangent=Vector3.Dot(left.position-right.position,across)>=0?across:-across;
            var leftTarget=_motor.EdgeGrip+tangent*span;var rightTarget=_motor.EdgeGrip-tangent*span;
            var shoulderGoal=_motor.EdgeGrip+_motor.EdgeOutward*length*Mathf.Lerp(.50f,-.35f,Mathf.Clamp01(pull*1.7f))+
                Vector3.up*(length*Mathf.Lerp(-.78f,.92f,Mathf.Clamp01(pull*1.6f))+effort+_motor.EdgeMashRatio*.055f);
            root.position+=shoulderGoal-(left.position+right.position)*.5f;
            // Fit both measured palms to the actual lip. Rigid block arms retain
            // their lengths and shoulder joints; the body's weight shifts instead.
            for(int pass=0;pass<3;pass++)
            {
                AimPalm(left,_edgeLeftPalm,leftTarget);AimPalm(right,_edgeRightPalm,rightTarget);
                var midpoint=(left.TransformPoint(_edgeLeftPalm)+right.TransformPoint(_edgeRightPalm))*.5f;
                root.position+=_motor.EdgeGrip-midpoint;
            }
            AimPalm(left,_edgeLeftPalm,leftTarget);AimPalm(right,_edgeRightPalm,rightTarget);
            root.position=Vector3.Lerp(rootBase,root.position,contact);
            left.rotation=Quaternion.Slerp(leftBase,left.rotation,contact);right.rotation=Quaternion.Slerp(rightBase,right.rotation,contact);
            EdgeGripError=Mathf.Max(Vector3.Distance(left.TransformPoint(_edgeLeftPalm),leftTarget),Vector3.Distance(right.TransformPoint(_edgeRightPalm),rightTarget));
        }

        private static void AimPalm(Transform bone,Vector3 palm,Vector3 target)
        {
            var current=bone.TransformVector(palm);var desired=target-bone.position;
            if(current.sqrMagnitude>.00001f&&desired.sqrMagnitude>.00001f)bone.rotation=Quaternion.FromToRotation(current,desired)*bone.rotation;
        }
    }
}
