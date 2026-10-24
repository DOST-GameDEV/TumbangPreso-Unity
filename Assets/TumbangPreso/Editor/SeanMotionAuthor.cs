using System;
using System.IO;
using System.Linq;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools
{
    // Rago's MAGMA FIST prayer (owner, 2026-10-24: "he js does a prayer pose tapos he long presses"), baked from his rig on every
    // roster build: the held prayer and its release (`HeroAbilityClips.Sean.cs`).
    //
    // ⚠️ BAKED, BECAUSE `AnimationClip.SetCurve` IS EDITOR-ONLY FOR THESE CLIPS: a clip built at runtime in a player is valid
    // and EMPTY (`GeneratedAnimationAuthor`'s header). `PhaisterMotionAuthor` is the precedent this copies.
    public static class SeanMotionAuthor
    {
        private const string Folder="Assets/TumbangPreso/Art/characters/sean-motion";

        public static AnimationClip[] Bake(GameObject model)
        {
            if(model==null)throw new ArgumentNullException(nameof(model));
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var copy=Object.Instantiate(model);copy.name="Rago motion authoring copy";
            copy.hideFlags=HideFlags.HideAndDontSave;
            AnimationClip[] generated=null;
            try
            {
                var animator=copy.GetComponentInChildren<Animator>(true);
                var root=animator!=null?animator.transform:copy.transform;
                generated=HeroAbilityClips.BuildSeanAuthored(root);
                var saved=new AnimationClip[generated.Length];
                for(int i=0;i<generated.Length;i++)
                {
                    var clip=generated[i];var bindings=AnimationUtility.GetCurveBindings(clip);
                    if(clip.length<=0||bindings.Length<21)throw new InvalidOperationException("Empty Rago action: "+clip.name);
                    foreach(var binding in bindings)
                        if(!string.IsNullOrEmpty(binding.path)&&root.Find(binding.path)==null)
                            throw new InvalidOperationException(clip.name+" does not bind to "+binding.path);
                    string path=Folder+"/"+clip.name+".anim";
                    saved[i]=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                    if(saved[i]==null){AssetDatabase.CreateAsset(clip,path);saved[i]=clip;}
                    else{EditorUtility.CopySerialized(clip,saved[i]);saved[i].name=clip.name;EditorUtility.SetDirty(saved[i]);}
                    // The held prayer loops for as long as the key is down; a clip that does not loop freezes on its last key.
                    if(clip.name.EndsWith("-aim"))
                    {
                        var settings=AnimationUtility.GetAnimationClipSettings(saved[i]);settings.loopTime=true;
                        AnimationUtility.SetAnimationClipSettings(saved[i],settings);EditorUtility.SetDirty(saved[i]);
                    }
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

        /// <summary>
        /// Bakes the prayer and binds it to Rago's roster entry without a whole roster rebuild (batch:
        /// `-executeMethod TumbangPreso.EditorTools.SeanMotionAuthor.BindToRoster -quit`). A clip of the same name is replaced.
        /// </summary>
        public static void BindToRoster()
        {
            var art=RosterBook.Load()?.FindPersonArt("sean");
            if(art==null||art.Model==null)throw new InvalidOperationException("Rago roster asset missing.");
            var baked=Bake(art.Model);
            var names=baked.Select(c=>c.name).ToArray();
            art.Clips=(art.Clips??Array.Empty<AnimationClip>()).Where(c=>c!=null&&!names.Contains(c.name)).Concat(baked).ToArray();
            EditorUtility.SetDirty(art);AssetDatabase.SaveAssets();
            Debug.Log("[SeanMotionAuthor] Bound "+string.Join(", ",names)+" to Rago.");
        }
    }
}
