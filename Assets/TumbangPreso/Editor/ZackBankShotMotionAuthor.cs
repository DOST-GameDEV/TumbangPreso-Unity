using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    public static class ZackBankShotMotionAuthor
    {
        [MenuItem("TUMP/Authoring/Wire Zack Bank Shot Clip")]
        public static void Wire()
        {
            const string model = "Assets/TumbangPreso/Art/characters/persons/team-zack.glb";
            AssetDatabase.ImportAsset(model, ImportAssetOptions.ForceSynchronousImport);
            var clip = AssetDatabase.LoadAllAssetsAtPath(model).OfType<AnimationClip>()
                .Single(c => c.name == "hero-zack-bankshot");
            if (clip.length < .6f || clip.length > .7f)
                throw new InvalidOperationException("Bank Shot held-shoe load clip is incomplete.");
            var roster = AssetDatabase.LoadAssetAtPath<RosterEntryAsset>(
                "Assets/TumbangPreso/Resources/Roster/person_zack.asset");
            if (roster.Clips.Contains(clip)) return;
            roster.Clips = roster.Clips.Concat(new[] { clip }).ToArray();
            EditorUtility.SetDirty(roster);
            AssetDatabase.SaveAssetIfDirty(roster);
            Debug.Log("Bank Shot held-shoe load clip wired to the shipping Zack roster.");
        }
    }
}
