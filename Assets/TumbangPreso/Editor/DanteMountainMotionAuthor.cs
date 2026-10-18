using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Puts Dante's MOUNTAIN FORM clips (`mountain-*`, written into dante-redesign.glb by
    /// `tools/author_dante_mountain_clips.py`) on his roster entry without a whole roster build, so they ship
    /// (`RosterEntryAsset.Clips` is the only reference a player keeps). Batchmode: -executeMethod
    /// TumbangPreso.EditorTools.DanteMountainMotionAuthor.Wire.
    /// </summary>
    public static class DanteMountainMotionAuthor
    {
        public const string Model = "Assets/TumbangPreso/Art/CharacterRedesign/dante/dante-redesign.glb";
        public static readonly string[] Names =
            { "mountain-idle", "mountain-walk", "mountain-sprint", "mountain-jump", "mountain-fall", "mountain-throw" };

        [MenuItem("TUMP/Authoring/Wire Dante Mountain Form Clips")]
        public static void Wire()
        {
            AssetDatabase.ImportAsset(Model, ImportAssetOptions.ForceSynchronousImport);
            var all = AssetDatabase.LoadAllAssetsAtPath(Model).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToArray();
            var clips = Names.Select(n => all.SingleOrDefault(c => c.name == n)
                                          ?? throw new InvalidOperationException(n + " is not in " + Model + "; run tools/author_dante_mountain_clips.py")).ToArray();
            foreach (var clip in clips)
                if (clip.length <= 0f || AnimationUtility.GetCurveBindings(clip).Length < 20)
                    throw new InvalidOperationException("Empty Mountain clip: " + clip.name);
            var roster = AssetDatabase.LoadAssetAtPath<RosterEntryAsset>("Assets/TumbangPreso/Resources/Roster/person_dante.asset");
            if (roster == null) throw new InvalidOperationException("No Dante roster entry.");
            roster.Clips = roster.Clips.Where(c => c != null && !Names.Contains(c.name)).Concat(clips).ToArray();
            EditorUtility.SetDirty(roster); AssetDatabase.SaveAssetIfDirty(roster);
            Debug.Log("[DanteMountainMotionAuthor] wired " + string.Join(", ", clips.Select(c => c.name + " " + c.length.ToString("F2") + "s")));
        }
    }
}
