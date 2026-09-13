using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools
{
    /// <summary>Contact-fitted recovery motion for the retained rigid, blocky rigs.</summary>
    public static class RecoveryAnimationAuthor
    {
        private const string Folder="Assets/TumbangPreso/Resources/RecoveryAnimations";
        private static readonly string[] Bones={"root","torso","head","arm-left","arm-right","leg-left","leg-right"};
        private struct Pose
        {
            public Vector3 Position;public Quaternion Rotation;
            public Pose(Transform bone){Position=bone.localPosition;Rotation=bone.localRotation;}
        }
        public static void Run()=>EditorApplication.Exit(Execute()?0:1);
        public static bool Execute()
        {
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var book=RosterBook.Load();if(book==null)return false;
            var authored=new HashSet<string>();
            foreach(var entry in book.People)
            {
                if(entry==null||entry.Model==null)continue;
                var sourceAnimator=entry.Model.GetComponentInChildren<Animator>();
                string id=DanceClip.ResourceName(sourceAnimator!=null?sourceAnimator.transform:entry.Model.transform);
                if(!authored.Add(id))continue;
                var fall=entry.Clips?.FirstOrDefault(c=>c!=null&&c.name=="die");
                if(fall==null)throw new InvalidOperationException(entry.Id+" has no retained landing source");
                var instance=Object.Instantiate(entry.Model);instance.hideFlags=HideFlags.HideAndDontSave;
                var scratch=new Mesh();
                try
                {
                    var animator=instance.GetComponentInChildren<Animator>();var root=animator!=null?animator.transform:instance.transform;
                    var all=root.GetComponentsInChildren<Transform>(true);
                    var bones=Bones.Select(name=>all.FirstOrDefault(t=>t.name==name)).ToArray();
                    if(bones.Any(t=>t==null))throw new InvalidOperationException(entry.Id+" misses a recovery bone");
                    var bind=bones.Select(t=>new Pose(t)).ToArray();
                    var palms=new Dictionary<Transform,Vector3>();
                    foreach(var skin in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                        for(int i=0;i<skin.bones.Length;i++)
                            if(skin.bones[i]!=null&&(skin.bones[i].name=="arm-left"||skin.bones[i].name=="arm-right")&&
                                CharacterVisual.PalmCentre(skin,i,out var palm))palms[skin.bones[i]]=palm;
                    fall.SampleAnimation(root.gameObject,Mathf.Max(0,fall.length-.0001f));
                    var prone=bones.Select(t=>new Pose(t)).ToArray();
                    var land=Object.Instantiate(fall);land.name=RecoveryMotion.Land;SetLoop(land,false);
                    FitLanding(land,fall,instance,root,bones,scratch);
                    var clips=new List<AnimationClip>{land};
                    foreach(bool standing in new[]{false,true})
                    {
                        float duration=standing?Balance.MinTripDown:1;
                        var clip=new AnimationClip{name=standing?RecoveryMotion.Stand:RecoveryMotion.Brace,frameRate=30};
                        var curves=new AnimationCurve[bones.Length,7];
                        for(int i=0;i<bones.Length;i++)for(int axis=0;axis<7;axis++)curves[i,axis]=new AnimationCurve();
                        for(int sample=0;sample<=40;sample++)
                        {
                            float t=sample/40f,p=standing?1:t;
                            ApplyBrace(bones,bind,prone,p);
                            foreach(var hand in palms)
                            {
                                float weight=Mathf.SmoothStep(0,1,Mathf.Clamp01((p-(hand.Key.name=="arm-right"?.06f:.20f))/.45f));
                                var current=hand.Key.TransformPoint(hand.Value)-hand.Key.position;
                                var down=instance.transform.TransformDirection(new Vector3(hand.Key.name=="arm-right"?-.10f:.10f,-1,-.10f));
                                var planted=Quaternion.FromToRotation(current,down)*hand.Key.rotation;
                                hand.Key.rotation=Quaternion.Slerp(hand.Key.rotation,planted,weight);
                            }
                            if(standing)
                            {
                                float rise=Mathf.SmoothStep(0,1,t);
                                for(int i=0;i<bones.Length;i++)
                                {
                                    bones[i].localRotation=Quaternion.Slerp(bones[i].localRotation,bind[i].Rotation,rise);
                                    bones[i].localPosition=Vector3.Lerp(bones[i].localPosition,bind[i].Position,rise);
                                }
                            }
                            FitContact(instance,bones[0],scratch);
                            for(int i=0;i<bones.Length;i++)
                            {
                                var position=bones[i].localPosition;var rotation=bones[i].localRotation;
                                for(int axis=0;axis<3;axis++)curves[i,axis].AddKey(t*duration,position[axis]);
                                for(int axis=0;axis<4;axis++)curves[i,axis+3].AddKey(t*duration,rotation[axis]);
                            }
                        }
                        for(int i=0;i<bones.Length;i++)
                        {
                            string path=AnimationUtility.CalculateTransformPath(bones[i],root);
                            for(int axis=0;axis<3;axis++)clip.SetCurve(path,typeof(Transform),"localPosition."+"xyz"[axis],curves[i,axis]);
                            for(int axis=0;axis<4;axis++)clip.SetCurve(path,typeof(Transform),"localRotation."+"xyzw"[axis],curves[i,axis+3]);
                        }
                        clip.EnsureQuaternionContinuity();SetLoop(clip,false);clips.Add(clip);
                    }
                    Save(id,clips);
                }
                finally{Object.DestroyImmediate(scratch);Object.DestroyImmediate(instance);}
            }
            AssetDatabase.SaveAssets();Debug.Log("[Recovery] baked landing, bracing and stand for "+authored.Count+" rig hierarchies");return true;
        }

        private static void ApplyBrace(Transform[] bones,Pose[] bind,Pose[] prone,float p)
        {
            for(int i=0;i<bones.Length;i++)
            {
                bones[i].localPosition=Vector3.Lerp(prone[i].Position,bind[i].Position,p);
                var target=bind[i].Rotation;
                switch(Bones[i])
                {
                    case "root":target=Quaternion.Slerp(prone[i].Rotation,bind[i].Rotation,.72f);break;
                    case "torso":target*=Quaternion.Euler(15,0,-8);break;
                    case "head":target*=Quaternion.Euler(-17,0,0);break;
                    case "arm-left":target*=Quaternion.Euler(-65,8,-18);break;
                    case "arm-right":target*=Quaternion.Euler(-38,-8,16);break;
                    case "leg-left":target*=Quaternion.Euler(-48,0,7);break;
                    case "leg-right":target*=Quaternion.Euler(42,0,-7);break;
                }
                bones[i].localRotation=Quaternion.Slerp(prone[i].Rotation,target,Mathf.SmoothStep(0,1,p));
            }
        }
        private static void FitLanding(AnimationClip land,AnimationClip source,GameObject instance,Transform root,Transform[] bones,Mesh scratch)
        {
            // The old fall rotated around its standing root and put the head
            // through the floor mid-clip. Preserve its gesture, fit its contact
            // through the arc, and leave the source/emote clip untouched.
            var curves=new AnimationCurve[bones.Length,7];
            for(int i=0;i<bones.Length;i++)for(int axis=0;axis<7;axis++)curves[i,axis]=new AnimationCurve();
            for(int sample=0;sample<=40;sample++)
            {
                float time=source.length*sample/40f;
                source.SampleAnimation(root.gameObject,Mathf.Min(time,source.length-.0001f));
                FitContact(instance,bones[0],scratch);
                for(int i=0;i<bones.Length;i++)
                {
                    var p=bones[i].localPosition;var q=bones[i].localRotation;
                    for(int axis=0;axis<3;axis++)curves[i,axis].AddKey(time,p[axis]);
                    for(int axis=0;axis<4;axis++)curves[i,axis+3].AddKey(time,q[axis]);
                }
            }
            for(int i=0;i<bones.Length;i++)
            {
                string path=AnimationUtility.CalculateTransformPath(bones[i],root);
                for(int axis=0;axis<3;axis++)land.SetCurve(path,typeof(Transform),"localPosition."+"xyz"[axis],curves[i,axis]);
                for(int axis=0;axis<4;axis++)land.SetCurve(path,typeof(Transform),"localRotation."+"xyzw"[axis],curves[i,axis+3]);
            }
            land.EnsureQuaternionContinuity();
        }
        private static void FitContact(GameObject instance,Transform rootBone,Mesh scratch)
        {
            float bottom=float.PositiveInfinity;
            foreach(var skin in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                skin.BakeMesh(scratch);
                foreach(var vertex in scratch.vertices)bottom=Mathf.Min(bottom,skin.transform.TransformPoint(vertex).y);
            }
            foreach(var filter in instance.GetComponentsInChildren<MeshFilter>(true))
                if(filter.sharedMesh!=null)foreach(var vertex in filter.sharedMesh.vertices)
                    bottom=Mathf.Min(bottom,filter.transform.TransformPoint(vertex).y);
            if(float.IsPositiveInfinity(bottom))throw new InvalidOperationException("Recovery rig has no measurable skin");
            rootBone.localPosition+=rootBone.parent.InverseTransformVector(Vector3.up*(instance.transform.position.y-bottom));
        }
        private static void SetLoop(AnimationClip clip,bool loop)
        {var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;AnimationUtility.SetAnimationClipSettings(clip,settings);}
        private static void Save(string id,List<AnimationClip> sources)
        {
            string path=Folder+"/"+id+".asset";var set=AssetDatabase.LoadAssetAtPath<GeneratedAnimationSet>(path);
            if(set==null){set=ScriptableObject.CreateInstance<GeneratedAnimationSet>();AssetDatabase.CreateAsset(set,path);}
            var old=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().ToArray();var clips=new List<AnimationClip>();
            foreach(var source in sources)
            {
                var saved=old.FirstOrDefault(c=>c.name==source.name);
                if(saved==null){AssetDatabase.AddObjectToAsset(source,set);saved=source;}
                else{EditorUtility.CopySerialized(source,saved);Object.DestroyImmediate(source);EditorUtility.SetDirty(saved);}
                clips.Add(saved);
            }
            set.name=id;set.Clips=clips.ToArray();EditorUtility.SetDirty(set);
        }
    }
}
