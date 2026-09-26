using System;
using System.IO;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools
{
    // Amihan's authored cast clips, baked from the rig on every roster build (2026-09-25).
    //
    // ⚠️ BAKED, BECAUSE `AnimationClip.SetCurve` IS EDITOR-ONLY FOR THESE CLIPS: a clip built at
    // runtime in a player is valid and EMPTY, which is the bind pose (`GeneratedAnimationAuthor`'s
    // header). `RafiMotionAuthor` is the precedent. ⚠️ AND FROM THE RIG, NOT THE MESH: the owner is
    // still improving her model, so a rebuilt glb re-bakes these from `HeroAbilityClips.Amihan.cs`
    // with no change here.
    public static class AmihanMotionAuthor
    {
        private const string Folder="Assets/TumbangPreso/Art/characters/amihan-motion";

        public static AnimationClip[] Bake(GameObject model)
        {
            if(model==null)throw new ArgumentNullException(nameof(model));
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var copy=Object.Instantiate(model);copy.name="Amihan motion authoring copy";
            copy.hideFlags=HideFlags.HideAndDontSave;
            AnimationClip[] generated=null;
            try
            {
                var animator=copy.GetComponentInChildren<Animator>(true);
                var root=animator!=null?animator.transform:copy.transform;
                generated=HeroAbilityClips.BuildAmihanAuthored(root);
                var saved=new AnimationClip[generated.Length];
                for(int i=0;i<generated.Length;i++)
                {
                    var clip=generated[i];var bindings=AnimationUtility.GetCurveBindings(clip);
                    if(clip.length<=0||bindings.Length<21)throw new InvalidOperationException("Empty Amihan action: "+clip.name);
                    foreach(var binding in bindings)
                        if(!string.IsNullOrEmpty(binding.path)&&root.Find(binding.path)==null)
                            throw new InvalidOperationException(clip.name+" does not bind to "+binding.path);
                    string path=Folder+"/"+clip.name+".anim";
                    saved[i]=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                    if(saved[i]==null){AssetDatabase.CreateAsset(clip,path);saved[i]=clip;}
                    else{EditorUtility.CopySerialized(clip,saved[i]);saved[i].name=clip.name;EditorUtility.SetDirty(saved[i]);}
                }
                return saved;
            }
            finally
            {
                if(generated!=null)foreach(var clip in generated)
                    if(clip!=null&&!AssetDatabase.Contains(clip))Object.DestroyImmediate(clip);
                Object.DestroyImmediate(copy);
            }
        }
    }
}
