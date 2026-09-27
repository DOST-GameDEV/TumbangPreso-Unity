using System;
using System.IO;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools
{
    // Phaister's authored cast clips (HERO-10, 2026-09-27), baked from the rig on every roster build: VANISHING ACT,
    // MANIKA MISCHIEF, SPOTLIGHT PIN and OMEN, one clip each (`HeroAbilityClips.Phaister.cs`).
    //
    // ⚠️ BAKED, BECAUSE `AnimationClip.SetCurve` IS EDITOR-ONLY FOR THESE CLIPS: a clip built at runtime in a player is valid
    // and EMPTY (`GeneratedAnimationAuthor`'s header). `PaeteMotionAuthor` is the precedent this copies.
    // ⚠️ HER GLB IS NEVER REBUILT FROM ITS BUILDER (it drops her appended clips and shortened arms; `tools/add_phaister_details.py`),
    // so these bake from the shipped rig.
    public static class PhaisterMotionAuthor
    {
        private const string Folder="Assets/TumbangPreso/Art/characters/phaister-motion";

        public static AnimationClip[] Bake(GameObject model)
        {
            if(model==null)throw new ArgumentNullException(nameof(model));
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var copy=Object.Instantiate(model);copy.name="Phaister motion authoring copy";
            copy.hideFlags=HideFlags.HideAndDontSave;
            AnimationClip[] generated=null;
            try
            {
                var animator=copy.GetComponentInChildren<Animator>(true);
                var root=animator!=null?animator.transform:copy.transform;
                generated=HeroAbilityClips.BuildPhaisterAuthored(root);
                var saved=new AnimationClip[generated.Length];
                for(int i=0;i<generated.Length;i++)
                {
                    var clip=generated[i];var bindings=AnimationUtility.GetCurveBindings(clip);
                    if(clip.length<=0||bindings.Length<21)throw new InvalidOperationException("Empty Phaister action: "+clip.name);
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
