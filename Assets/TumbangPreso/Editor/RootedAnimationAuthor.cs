using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Bakes the three clips every rig needs for Paete's kit (HERO-9): the struggle against his roots,
    /// the heave that pulls his seedling out and the break-out when the roots let go, keyed in `HeroAbilityClips.Paete.cs`, one set per
    /// rig hierarchy into `Resources/RootedAnimations` (the `RecoveryAnimationAuthor` pattern).
    ///
    /// ⚠️ A PLAYER CANNOT BUILD THESE ITSELF: `AnimationClip.SetCurve` is editor-only for these clips
    /// and a runtime-built one is valid and EMPTY (`GeneratedAnimationAuthor`'s header). Run it after
    /// any rig changes:
    ///   python tools/run_unity_guarded.py -batchmode -executeMethod TumbangPreso.EditorTools.RootedAnimationAuthor.Run -logFile Logs/rooted.log
    /// </summary>
    public static class RootedAnimationAuthor
    {
        private const string Folder="Assets/TumbangPreso/Resources/"+RootedMotion.Folder;

        public static void Run()=>EditorApplication.Exit(Execute()?0:1);

        public static bool Execute()
        {
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var book=RosterBook.Load();if(book==null)return false;
            var authored=new HashSet<string>();
            foreach(var entry in book.People)
            {
                if(entry==null||entry.Model==null)continue;
                var instance=Object.Instantiate(entry.Model);instance.hideFlags=HideFlags.HideAndDontSave;
                try
                {
                    var animator=instance.GetComponentInChildren<Animator>();var root=animator!=null?animator.transform:instance.transform;
                    string id=DanceClip.ResourceName(root);
                    if(string.IsNullOrEmpty(id)||!authored.Add(id))continue;
                    var clips=HeroAbilityClips.BuildRootedShared(root);
                    if(clips==null){Debug.LogError("[Rooted] "+entry.Id+" has no complete rig");return false;}
                    foreach(var clip in clips)
                    {
                        if(AnimationUtility.GetCurveBindings(clip).Length<21){Debug.LogError("[Rooted] empty "+clip.name+" on "+entry.Id);return false;}
                        var settings=AnimationUtility.GetAnimationClipSettings(clip);
                        settings.loopTime=clip.name==RootedMotion.Struggle||clip.name==RootedMotion.Feared;
                        AnimationUtility.SetAnimationClipSettings(clip,settings);
                    }
                    Save(id,clips);
                }
                finally{Object.DestroyImmediate(instance);}
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Rooted] baked struggle, heave and break-out for "+authored.Count+" rig hierarchies");
            return true;
        }

        private static void Save(string id,IEnumerable<AnimationClip> sources)
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
