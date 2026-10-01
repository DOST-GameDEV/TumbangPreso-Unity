using System.Collections.Generic;
using System.Linq;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>Read-only prerequisite check. Building never reauthors retained clips.</summary>
    public static class AuthoredAnimationBuildCheck
    {
        // Explicit missing-input repair, never called by a regular build. Existing
        // sets are skipped, including hand-adjusted curves and retained GUIDs.
        public static void RepairMissing()
        {
            bool ok = SwimmingAnimationAuthor.EnsureMissing() && RecoveryAnimationAuthor.EnsureMissing() && Execute();
            EditorApplication.Exit(ok ? 0 : 1);
        }

        public static bool Execute()
        {
            if (!Validate(out int rigs, out string error))
            { Debug.LogError("[Build] " + error + " Author the missing set explicitly before building."); return false; }
            Debug.Log("[Build] retained swim/recovery clips validated for " + rigs + " rig hierarchies.");
            return true;
        }

        public static bool Validate(out int rigs, out string error)
        {
            rigs = 0; error = "";
            var book = RosterBook.Load();
            if (book == null) { error = "Roster book is unavailable."; return false; }
            var seen = new HashSet<string>();
            foreach (var entry in book.People)
            {
                if (entry == null || entry.Model == null) continue;
                var animator = entry.Model.GetComponentInChildren<Animator>();
                string id = DanceClip.ResourceName(animator != null ? animator.transform : entry.Model.transform);
                if (string.IsNullOrEmpty(id)) { error = "Missing rig identity: " + entry.Id; return false; }
                if (!seen.Add(id)) continue;
                string swim = "Assets/TumbangPreso/Resources/" + SwimmingMotion.Folder + "/" + id + ".asset";
                string recovery = "Assets/TumbangPreso/Resources/" + RecoveryMotion.Folder + "/" + id + ".asset";
                if (!ValidateSet(AssetDatabase.LoadAssetAtPath<GeneratedAnimationSet>(swim), SwimmingMotion.Names, out error))
                { error = swim + ": " + error; return false; }
                if (!ValidateSet(AssetDatabase.LoadAssetAtPath<GeneratedAnimationSet>(recovery),
                    new[] { RecoveryMotion.Land, RecoveryMotion.Brace, RecoveryMotion.Stand }, out error))
                { error = recovery + ": " + error; return false; }
                rigs++;
            }
            if (rigs == 0) { error = "No retained rig animation sets were found."; return false; }
            return true;
        }

        public static bool ValidateSet(GeneratedAnimationSet set, IEnumerable<string> names, out string error)
        {
            foreach (string name in names)
            {
                var matching = set?.Clips?.Where(c => c != null && c.name == name).ToArray();
                if (matching == null || matching.Length != 1)
                { error = "Expected one retained clip: " + name; return false; }
                var clip = matching[0];
                if (clip.length <= 0 || float.IsNaN(clip.length) || float.IsInfinity(clip.length)
                    || AnimationUtility.GetCurveBindings(clip).Length == 0)
                { error = "Empty retained clip: " + name; return false; }
            }
            error = ""; return true;
        }
    }
}
