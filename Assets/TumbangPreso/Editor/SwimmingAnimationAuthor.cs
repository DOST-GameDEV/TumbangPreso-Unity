using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools
{
    /// <summary>Bake real player-readable swim clips without changing the approved models.</summary>
    public static class SwimmingAnimationAuthor
    {
        private const string Folder="Assets/TumbangPreso/Resources/SwimmingAnimations";
        public static void Run()=>EditorApplication.Exit(Execute()?0:1);
        public static bool Execute()
        {
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var book=RosterBook.Load();if(book==null)return false;
            var authored=new HashSet<string>();
            foreach(var entry in book.People)
            {
                if(entry==null||entry.Model==null)continue;
                var animator=entry.Model.GetComponentInChildren<Animator>();
                var root=animator!=null?animator.transform:entry.Model.transform;
                string id=DanceClip.ResourceName(root);
                if(string.IsNullOrEmpty(id))throw new InvalidOperationException("Missing swim rig identity: "+entry.Id);
                if(!authored.Add(id))continue;
                var bones=root.GetComponentsInChildren<Transform>(true);
                var hold=entry.Clips?.FirstOrDefault(c=>c!=null&&c.name=="holding-right");
                var clips=new List<AnimationClip>();
                foreach(bool moving in new[]{true,false})foreach(bool held in new[]{false,true})
                {
                    float duration=SwimmingMotion.Duration(moving);
                    var clip=new AnimationClip{name=SwimmingMotion.Clip(moving,held),frameRate=30,wrapMode=WrapMode.Loop};
                    foreach(string name in new[]{"torso","head","arm-left","arm-right","leg-left","leg-right"})
                    {
                        var bone=bones.FirstOrDefault(t=>t.name==name);
                        if(bone==null)throw new InvalidOperationException(entry.Id+" misses swim bone "+name);
                        string path=AnimationUtility.CalculateTransformPath(bone,root);
                        var grip=held&&name=="arm-right"&&hold!=null?HoldRotation(hold,path,bone.localRotation):bone.localRotation;
                        Vector3 palm=Vector3.zero;
                        bool arm=name=="arm-left"||name=="arm-right";
                        if(arm)
                            foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                            {
                                int index=Array.IndexOf(skin.bones,bone);
                                if(index>=0&&CharacterVisual.PalmCentre(skin,index,out palm))break;
                            }
                        var curves=new[]{new AnimationCurve(),new AnimationCurve(),new AnimationCurve(),new AnimationCurve()};
                        for(int sample=0;sample<=32;sample++)
                        {
                            float fraction=sample/32f;
                            float phase=fraction*Mathf.PI*2;
                            var rotation=bone.localRotation*Quaternion.Euler(SwimmingMotion.Rotation(name,phase,moving,held));
                            if(arm&&palm.sqrMagnitude>.00001f)
                            {
                                // Both arms share the breaststroke reach/sweep/
                                // recovery cycle. Aim the actual palm, retaining
                                // the approved grip/hand bindings when carrying.
                                var parent=bone.parent.rotation*Quaternion.Euler(SwimmingMotion.Rotation("torso",phase,moving,held));
                                var direction=parent*(grip*palm);
                                var desired=root.TransformDirection(SwimmingMotion.HandDirection(name=="arm-left"?-1:1,phase,moving,held));
                                rotation=Quaternion.Inverse(parent)*Quaternion.FromToRotation(direction,desired)*parent*grip;
                            }
                            for(int axis=0;axis<4;axis++)curves[axis].AddKey(fraction*duration,rotation[axis]);
                        }
                        for(int axis=0;axis<4;axis++)clip.SetCurve(path,typeof(Transform),"localRotation."+"xyzw"[axis],curves[axis]);
                        // Preserve the character's approved grip instead of inventing
                        // a second hand-to-slipper attachment pose in water.
                        if(held&&name=="arm-right"&&hold!=null)
                        {
                            foreach(var binding in AnimationUtility.GetCurveBindings(hold).Where(b=>b.path==path||b.path.StartsWith(path+"/",StringComparison.Ordinal)))
                            {
                                var curve=AnimationUtility.GetEditorCurve(hold,binding);if(curve==null)continue;
                                // The shoulder follows the stroke; its hand/grip
                                // and descendant bindings retain the authored pose.
                                if(binding.path==path&&IsRotation(binding.propertyName))continue;
                                float value=curve.Evaluate(0);
                                AnimationUtility.SetEditorCurve(clip,binding,AnimationCurve.Constant(0,duration,value));
                            }
                        }
                    }
                    var rootBone=bones.FirstOrDefault(t=>t.name=="root");
                    if(rootBone==null)throw new InvalidOperationException(entry.Id+" misses its swim root bone");
                    var lift=new AnimationCurve();
                    for(int sample=0;sample<=32;sample++)
                    {
                        float fraction=sample/32f;
                        lift.AddKey(fraction*duration,rootBone.localPosition.y+SwimmingMotion.VisualLift(fraction*Mathf.PI*2,moving)/CharacterVisual.PersonScale);
                    }
                    clip.SetCurve(AnimationUtility.CalculateTransformPath(rootBone,root),typeof(Transform),"localPosition.y",lift);
                    clip.EnsureQuaternionContinuity();
                    var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;
                    AnimationUtility.SetAnimationClipSettings(clip,settings);clips.Add(clip);
                }
                Save(id,clips);
            }
            AssetDatabase.SaveAssets();Debug.Log("[Swimming] baked4loop variants for "+authored.Count+" rig hierarchies.");return true;
        }

        private static bool IsRotation(string property)=>property.IndexOf("Rotation",StringComparison.OrdinalIgnoreCase)>=0||
            property.IndexOf("Euler",StringComparison.OrdinalIgnoreCase)>=0;

        private static Quaternion HoldRotation(AnimationClip clip,string path,Quaternion fallback)
        {
            var q=fallback;var euler=Vector3.zero;bool hasEuler=false;
            foreach(var binding in AnimationUtility.GetCurveBindings(clip).Where(b=>b.path==path&&IsRotation(b.propertyName)))
            {
                int axis="xyzw".IndexOf(binding.propertyName[binding.propertyName.Length-1]);if(axis<0)continue;
                float value=AnimationUtility.GetEditorCurve(clip,binding).Evaluate(0);
                if(binding.propertyName.IndexOf("Euler",StringComparison.OrdinalIgnoreCase)>=0)
                {if(axis<3){euler[axis]=value;hasEuler=true;}}
                else q[axis]=value;
            }
            return hasEuler?Quaternion.Euler(euler):q.normalized;
        }

        private static void Save(string id,List<AnimationClip> sources)
        {
            string path=Folder+"/"+id+".asset";
            var set=AssetDatabase.LoadAssetAtPath<GeneratedAnimationSet>(path);
            if(set==null){set=ScriptableObject.CreateInstance<GeneratedAnimationSet>();AssetDatabase.CreateAsset(set,path);}
            var existing=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().ToArray();
            var clips=new List<AnimationClip>();
            foreach(var source in sources)
            {
                var saved=existing.FirstOrDefault(c=>c.name==source.name);
                if(saved==null){AssetDatabase.AddObjectToAsset(source,set);saved=source;}
                else{EditorUtility.CopySerialized(source,saved);Object.DestroyImmediate(source);EditorUtility.SetDirty(saved);}
                clips.Add(saved);
            }
            set.name=id;set.Clips=clips.ToArray();EditorUtility.SetDirty(set);
        }
    }
}
