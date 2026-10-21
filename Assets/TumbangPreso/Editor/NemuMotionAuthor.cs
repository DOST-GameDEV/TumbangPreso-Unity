using System;
using System.Linq;
using System.IO;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools
{
    // Nemu's authored cast clips, baked from the rig on every roster build (owner 2026-10-15: her ghost casts, `HeroAbilityClips.Nemu.cs`).
    //
    // ⚠️ BAKED, BECAUSE `AnimationClip.SetCurve` IS EDITOR-ONLY FOR THESE CLIPS: a clip built at
    // runtime in a player is valid and EMPTY, which is the bind pose (`GeneratedAnimationAuthor`'s
    // header). `PaeteMotionAuthor` is the precedent this copies. From the rig, not the mesh, so a
    // rebuilt glb re-bakes these from `HeroAbilityClips.Nemu.cs` with no change here.
    public static class NemuMotionAuthor
    {
        private const string Folder="Assets/TumbangPreso/Art/characters/nemu-motion";

        public static AnimationClip[] Bake(GameObject model)
        {
            if(model==null)throw new ArgumentNullException(nameof(model));
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var copy=Object.Instantiate(model);copy.name="Nemu motion authoring copy";
            copy.hideFlags=HideFlags.HideAndDontSave;
            AnimationClip[] generated=null;
            try
            {
                var animator=copy.GetComponentInChildren<Animator>(true);
                var root=animator!=null?animator.transform:copy.transform;
                generated=HeroAbilityClips.BuildNemuAuthored(root);
                var saved=new AnimationClip[generated.Length];
                for(int i=0;i<generated.Length;i++)
                {
                    var clip=generated[i];var bindings=AnimationUtility.GetCurveBindings(clip);
                    // 21 is the seven bones' rotations, the floor for any clip. A clip that keys an elbow carries six more
                    // curves per forearm (`HeroAbilityClips`, THE ELBOWS), written only on a rig that has the bone, so the
                    // path check below passes for them the same way; a clip that keys none carries none.
                    if(clip.length<=0||bindings.Length<21)throw new InvalidOperationException("Empty Nemu action: "+clip.name);
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

        /// <summary>Bake and put the clips on her roster entry without a whole roster build (batchmode: -executeMethod).</summary>
        [MenuItem("TUMP/Authoring/Bake Nemu Ghost Casts")]
        public static void Wire()
        {
            var roster=AssetDatabase.LoadAssetAtPath<RosterEntryAsset>("Assets/TumbangPreso/Resources/Roster/person_nemu.asset");
            if(roster==null||roster.Model==null)throw new InvalidOperationException("No Nemu roster entry or model.");
            var baked=Bake(roster.Model);
            var names=baked.Select(c=>c.name).ToArray();
            roster.Clips=roster.Clips.Where(c=>c!=null&&!names.Contains(c.name)).Concat(baked).ToArray();
            EditorUtility.SetDirty(roster);AssetDatabase.SaveAssets();
            Debug.Log("[NemuMotionAuthor] baked "+string.Join(", ",names));
        }
    }
}
