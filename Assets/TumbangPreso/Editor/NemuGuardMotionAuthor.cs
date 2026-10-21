using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    public static class NemuGuardMotionAuthor
    {
        [MenuItem("TUMP/Authoring/Wire Nemu Guard Clip")]
        public static void Wire()
        {
            const string model="Assets/TumbangPreso/Art/characters/persons/team-nemu.glb";
            AssetDatabase.ImportAsset(model,ImportAssetOptions.ForceSynchronousImport);
            var clip=AssetDatabase.LoadAllAssetsAtPath(model).OfType<AnimationClip>()
                .Single(c=>c.name=="hero-nemu-guard");
            if(clip.length<.7f)throw new InvalidOperationException("Nemu guard clip is incomplete.");
            var roster=AssetDatabase.LoadAssetAtPath<RosterEntryAsset>("Assets/TumbangPreso/Resources/Roster/person_nemu.asset");
            if(roster.Clips.Any(c=>c==clip))return;
            roster.Clips=roster.Clips.Concat(new[]{clip}).ToArray();
            EditorUtility.SetDirty(roster);AssetDatabase.SaveAssetIfDirty(roster);
            Debug.Log("Nemu guard authored clip added to the serialized roster.");
        }
    }
}
