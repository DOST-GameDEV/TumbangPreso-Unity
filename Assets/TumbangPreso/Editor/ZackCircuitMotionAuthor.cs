using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    public static class ZackCircuitMotionAuthor
    {
        [MenuItem("TUMP/Authoring/Wire Isagani Circuit Clip")]
        public static void Wire()
        {
            const string model = "Assets/TumbangPreso/Art/characters/persons/team-zack.glb";
            AssetDatabase.ImportAsset(model, ImportAssetOptions.ForceSynchronousImport);
            var clip = AssetDatabase.LoadAllAssetsAtPath(model).OfType<AnimationClip>()
                .Single(c => c.name == "hero-zack-circuit");
            if (clip.length < .6f || clip.length > .7f)
                throw new InvalidOperationException("Closed Circuit acquisition clip is incomplete.");
            var roster = AssetDatabase.LoadAssetAtPath<RosterEntryAsset>(
                "Assets/TumbangPreso/Resources/Roster/person_zack.asset");
            if (roster.Clips.Contains(clip)) return;
            roster.Clips = roster.Clips.Concat(new[] { clip }).ToArray();
            EditorUtility.SetDirty(roster);
            AssetDatabase.SaveAssetIfDirty(roster);
            Debug.Log("Closed Circuit acquisition clip wired to the shipping Isagani roster.");
        }
    }
}
