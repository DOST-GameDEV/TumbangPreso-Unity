using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    public static class DanteBastionMotionAuthor
    {
        [MenuItem("TUMP/Authoring/Wire Basilio Bastion Clip")]
        public static void Wire()
        {
            const string model="Assets/TumbangPreso/Art/characters/persons/team-dante.glb";
            AssetDatabase.ImportAsset(model,ImportAssetOptions.ForceSynchronousImport);
            var clip=AssetDatabase.LoadAllAssetsAtPath(model).OfType<AnimationClip>()
                .Single(c=>c.name=="hero-dante-bastion");
            if(clip.length<.7f)throw new InvalidOperationException("Bastion clip is incomplete.");
            var roster=AssetDatabase.LoadAssetAtPath<RosterEntryAsset>("Assets/TumbangPreso/Resources/Roster/person_dante.asset");
            if(roster.Clips.Any(c=>c==clip))return;
            roster.Clips=roster.Clips.Concat(new[]{clip}).ToArray();
            EditorUtility.SetDirty(roster);AssetDatabase.SaveAssetIfDirty(roster);
            Debug.Log("Basilio Bastion authored clip added to the serialized roster.");
        }
    }
}
