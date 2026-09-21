using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class CharacterAnimator
    {
        private struct ContactFoot { public Transform Bone; public Vector3[] Points; }
        private ContactFoot[] _contactFeet;
        private Transform _contactRoot;
        private CharacterVisual _contactVisual;
        private Vector3 _contactRest;
        private float _contactLift;
        private bool _contactApplied;

        private void RestoreFootContact()
        {
            if(!_contactApplied)return;
            if(_contactRoot!=null)_contactRoot.localPosition=_contactRest;
            _contactApplied=false;
        }
        private void ClearFootContact()
        {
            RestoreFootContact();_contactFeet=null;_contactRoot=null;_contactVisual=null;_contactLift=0;
        }
        private void CacheFootContact()
        {
            _contactVisual=GetComponent<CharacterVisual>();
            foreach(var bone in _animator.GetComponentsInChildren<Transform>(true))
                if(bone.name=="root"){_contactRoot=bone;break;}
            var points=new Dictionary<Transform,HashSet<Vector3>>();
            foreach(var skin in _animator.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if(skin.sharedMesh==null)continue;
                var vertices=skin.sharedMesh.vertices;var weights=skin.sharedMesh.boneWeights;
                var binds=skin.sharedMesh.bindposes;var bones=skin.bones;
                if(weights.Length!=vertices.Length)continue;
                for(int i=0;i<vertices.Length;i++)
                {
                    // The approved block rigs use rigid limbs. Cache their actual
                    // vertices once, not importer bounds or an assumed leg length.
                    var w=weights[i];int index=w.boneIndex0;
                    if(w.weight0<.999f||index<0||index>=bones.Length||index>=binds.Length)continue;
                    var bone=bones[index];if(bone==null||(bone.name!="leg-left"&&bone.name!="leg-right"))continue;
                    if(!points.TryGetValue(bone,out var list)){list=new HashSet<Vector3>();points.Add(bone,list);}
                    list.Add(binds[index].MultiplyPoint3x4(vertices[i]));
                }
            }
            var feet=new List<ContactFoot>();
            foreach(var pair in points)
            {var values=new Vector3[pair.Value.Count];pair.Value.CopyTo(values);feet.Add(new ContactFoot{Bone=pair.Key,Points=values});}
            _contactFeet=feet.ToArray();
        }
        private void ApplyFootContact()
        {
            if(_motor==null||_animator==null||!_graph.IsValid())return;
            if(_contactFeet==null)CacheFootContact();
            bool allowed=_motor.IsGrounded&&!_motor.IsSwimming&&!_motor.IsTripped&&!_motor.IsStunned
                &&(_oneShotLeft<=0||_throwReleaseTime>=0)&&_introductionBones==null
                &&(_emote==null||!_emote.IsEmoting);
            if(!allowed||_contactRoot==null||_contactFeet.Length==0||_contactVisual==null
                ||!_contactVisual.TryGetGroundSupport(out float support))
            { _contactLift=0;return; }
            float lowest=float.PositiveInfinity;
            foreach(var foot in _contactFeet)
            {
                if(foot.Bone==null)continue;var matrix=foot.Bone.localToWorldMatrix;
                foreach(var point in foot.Points)lowest=Mathf.Min(lowest,matrix.MultiplyPoint3x4(point).y);
            }
            if(float.IsInfinity(lowest))return;
            // Only the rendered skeleton lifts. Physics, aiming and the independent
            // remote root correction keep their existing owners. Airborne/cast poses
            // bypass this entirely, and the offset is removed before every evaluation.
            float target=Mathf.Clamp(support-lowest,-.24f,.32f);
            _contactLift=Mathf.MoveTowards(_contactLift,target,Time.deltaTime*3);
            _contactRest=_contactRoot.localPosition;
            _contactRoot.position+=Vector3.up*_contactLift;_contactApplied=true;
        }
    }
}
