using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    public static class CheskaFrostbiteMotionAuthor
    {
        [MenuItem("TUMP/Authoring/Wire Yasmin Frostbite Clip")]
        public static void Wire()
        {
            const string model="Assets/TumbangPreso/Art/characters/persons/team-cheska.glb";
            AssetDatabase.ImportAsset(model,ImportAssetOptions.ForceSynchronousImport);
            var clip=AssetDatabase.LoadAllAssetsAtPath(model).OfType<AnimationClip>()
                .Single(c=>c.name=="hero-cheska-frostbite");
            if(clip.length<.7f)throw new InvalidOperationException("Frostbite clip is incomplete.");
            var roster=AssetDatabase.LoadAssetAtPath<RosterEntryAsset>("Assets/TumbangPreso/Resources/Roster/person_cheska.asset");
            if(roster.Clips.Any(c=>c==clip))return;
            roster.Clips=roster.Clips.Concat(new[]{clip}).ToArray();
            EditorUtility.SetDirty(roster);AssetDatabase.SaveAssetIfDirty(roster);
            Debug.Log("Yasmin Frostbite authored clip added to her serialized roster.");
        }
    }
}
