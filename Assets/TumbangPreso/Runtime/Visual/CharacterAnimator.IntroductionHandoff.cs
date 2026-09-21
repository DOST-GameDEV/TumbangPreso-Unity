using System.Collections.Generic;
using TumbangPreso.Abilities;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class CharacterAnimator
    {
        private struct IntroductionBone
        {
            public Transform Bone;
            public Vector3 FromPosition, UnderPosition;
            public Quaternion FromRotation, UnderRotation;
        }
        private IntroductionBone[] _introductionBones;
        private HeroAbility _introductionAbility;
        private bool _introductionApplied;
        private float _introductionBlend;
        internal void StageIntroductionPose(GameObject recordedModel, HeroAbility ability)
        {
            ClearIntroductionPose();
            var visual=GetComponent<CharacterVisual>(); if(visual?.Model==null || recordedModel==null || ability==null)return;
            var source=new Dictionary<string,Transform>();
            foreach(var bone in recordedModel.GetComponentsInChildren<Transform>(true))
                if(IsIntroductionBone(bone.name))source[bone.name]=bone;
            var bones=new List<IntroductionBone>(7);
            foreach(var bone in visual.Model.GetComponentsInChildren<Transform>(true))
                if(source.TryGetValue(bone.name,out var from))
                    bones.Add(new IntroductionBone{Bone=bone,FromPosition=from.localPosition,FromRotation=from.localRotation});
            _introductionBones=bones.ToArray();_introductionAbility=ability;_introductionBlend=0;
        }
        private static bool IsIntroductionBone(string name) => name=="root" || name=="torso" || name=="head"
            || name=="arm-left" || name=="arm-right" || name=="leg-left" || name=="leg-right";
        private void RestoreIntroductionPose()
        {
            if(!_introductionApplied || _introductionBones==null)return;
            foreach(var pose in _introductionBones)
                if(pose.Bone!=null){pose.Bone.localPosition=pose.UnderPosition;pose.Bone.localRotation=pose.UnderRotation;}
            _introductionApplied=false;
        }
        private void ApplyIntroductionPose()
        {
            if(_introductionBones==null || _introductionAbility==null)return;
            if(_motor==null || _motor.IsStunned || _motor.IsTripped || !_motor.RoundActive)
            {ClearIntroductionPose();return;}
            if(!_introductionAbility.ReservedForIntroduction)_introductionBlend+=Time.deltaTime/.22f;
            if(_introductionBlend>=1){ClearIntroductionPose();return;}
            float blend=Mathf.SmoothStep(0,1,_introductionBlend);
            for(int i=0;i<_introductionBones.Length;i++)
            {
                var pose=_introductionBones[i];if(pose.Bone==null)continue;
                pose.UnderPosition=pose.Bone.localPosition;pose.UnderRotation=pose.Bone.localRotation;
                pose.Bone.localPosition=Vector3.Lerp(pose.FromPosition,pose.UnderPosition,blend);
                pose.Bone.localRotation=Quaternion.Slerp(pose.FromRotation,pose.UnderRotation,blend);
                _introductionBones[i]=pose;
            }
            _introductionApplied=true;
        }
        private void ClearIntroductionPose()
        {RestoreIntroductionPose();_introductionBones=null;_introductionAbility=null;_introductionBlend=0;}
    }
}
