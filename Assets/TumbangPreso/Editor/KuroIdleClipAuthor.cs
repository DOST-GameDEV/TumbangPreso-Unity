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
    /// <summary>Bake the live idle sampler to named clips for future Timeline/trailer shots.</summary>
    public static class KuroIdleClipAuthor
    {
        public const string Folder="Assets/TumbangPreso/Art/animations/kuro-idles";
        public static void Build()
        {
            var entry=Resources.Load<RosterBook>("RosterBook").People.First(p=>p.Id=="nemu");
            if(entry.PetModel==null)throw new InvalidOperationException("Refresh the actual Kuro roster reference first.");
            Directory.CreateDirectory(Folder);
            var target=new GameObject("Kuro clip origin");
            var actor=Object.Instantiate(entry.PetModel);
            try
            {
                var companion=actor.AddComponent<GhostPetCompanion>();companion.Bind(target.transform,Vector3.zero,1);
                var nodes=actor.GetComponentsInChildren<Transform>();
                foreach(GhostPetCompanion.FidgetState gesture in Enum.GetValues(typeof(GhostPetCompanion.FidgetState)))
                {
                    float duration=GhostPetCompanion.IdleGestureDuration(gesture);
                    var curves=new Dictionary<(string,string),AnimationCurve>();
                    int frames=Mathf.CeilToInt(duration*60);
                    for(int f=0;f<=frames;f++)
                    {
                        float time=Mathf.Min(duration,f/60f);
                        if(!companion.SampleIdleForCapture(gesture,time))throw new InvalidOperationException("Idle sampler refused an idle preview.");
                        foreach(var node in nodes)
                        {
                            string path=AnimationUtility.CalculateTransformPath(node,actor.transform);
                            var p=node.localPosition;var q=node.localRotation;var scale=node.localScale;
                            float[] values={p.x,p.y,p.z,q.x,q.y,q.z,q.w,scale.x,scale.y,scale.z};
                            string[] names={"m_LocalPosition.x","m_LocalPosition.y","m_LocalPosition.z","m_LocalRotation.x","m_LocalRotation.y","m_LocalRotation.z","m_LocalRotation.w","m_LocalScale.x","m_LocalScale.y","m_LocalScale.z"};
                            for(int i=0;i<names.Length;i++)
                            {
                                var key=(path,names[i]);if(!curves.TryGetValue(key,out var curve))curves[key]=curve=new AnimationCurve();
                                curve.AddKey(time,values[i]);
                            }
                        }
                    }
                    string name=gesture==GhostPetCompanion.FidgetState.None?"Hover":gesture.ToString();
                    var clip=new AnimationClip {name="kuro-"+name,frameRate=60};
                    foreach(var pair in curves)
                    {
                        var keys=pair.Value.keys;
                        // Constant channels need only their identical endpoints. Keep
                        // every animated sample; this changes no authored motion.
                        var curve=keys.All(k=>k.value==keys[0].value)
                            ? new AnimationCurve(new Keyframe(0,keys[0].value),new Keyframe(duration,keys[0].value)) : pair.Value;
                        AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(pair.Key.Item1,typeof(Transform),pair.Key.Item2),curve);
                    }
                    clip.EnsureQuaternionContinuity();
                    var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=gesture==GhostPetCompanion.FidgetState.None;AnimationUtility.SetAnimationClipSettings(clip,settings);
                    string asset=Folder+"/"+clip.name+".anim";
                    var previous=AssetDatabase.LoadAssetAtPath<AnimationClip>(asset);
                    if(previous==null)AssetDatabase.CreateAsset(clip,asset);
                    else {EditorUtility.CopySerialized(clip,previous);EditorUtility.SetDirty(previous);Object.DestroyImmediate(clip);}
                    Debug.Log("[Kuro idle] "+name+" "+duration+" seconds, "+curves.Count+" transform curves");
                }
                AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            }
            finally {Object.DestroyImmediate(actor);Object.DestroyImmediate(target);}
            EditorApplication.Exit(0);
        }
    }
}
