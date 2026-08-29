using System.Collections.Generic;
using System.IO;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Bakes the mathematical dance curves into AnimationClip assets.
    ///
    /// ⚠️⚠️ THIS IS A PLAYER CORRECTNESS STEP, NOT AN ART EXPORT CONVENIENCE.
    /// `AnimationClip.SetCurve` works on these non-legacy clips in the editor and is editor-only
    /// for that clip type at runtime. Building them in `CharacterAnimator` therefore produced
    /// valid clips with no curves in a Windows player. The dance resolved successfully, beat its
    /// idle fallback, and held the rig's bind pose. Baking serialises the same curves before the
    /// player starts and lets the ordinary AnimationClipPlayable path consume them.
    /// </summary>
    public static class GeneratedAnimationAuthor
    {
        private const string OutputDirectory =
            "Assets/TumbangPreso/Resources/GeneratedAnimations";

        [MenuItem("Tumbang Preso/Bake Generated Animations")]
        public static void RunFromMenu() => Execute();

        public static void Run() => EditorApplication.Exit(Execute() ? 0 : 1);

        public static bool Execute()
        {
            Directory.CreateDirectory(OutputDirectory);
            var book = AssetDatabase.LoadAssetAtPath<RosterBook>(
                "Assets/TumbangPreso/Resources/RosterBook.asset");

            if (book == null)
            {
                Debug.LogError("[GeneratedAnimationAuthor] RosterBook.asset is missing.");
                return false;
            }

            var authored = new HashSet<string>();

            foreach (var entry in book.People)
            {
                if (entry == null || entry.Model == null) continue;

                var animator = entry.Model.GetComponentInChildren<Animator>();
                Transform root = animator != null ? animator.transform : entry.Model.transform;
                string resourceName = DanceClip.ResourceName(root);

                if (string.IsNullOrEmpty(resourceName))
                {
                    Debug.LogError($"[GeneratedAnimationAuthor] {entry.Id} has no rig resource name.");
                    return false;
                }

                if (!authored.Add(resourceName)) continue;

                var clip = DanceClip.Build(root);
                if (clip == null || !EveryBindingFits(root, entry.Id, new[] { clip })) return false;

                Save(resourceName, clip);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Debug.Log($"[GeneratedAnimationAuthor] baked the dance for each of {authored.Count} " +
                      $"rig hierarchies into {OutputDirectory}.");
            return true;
        }

        /// <summary>
        /// Replaces the sub-asset while preserving the set asset's GUID, so every rebuild is a
        /// content update rather than a new reference per rig.
        /// </summary>
        private static void Save(string resourceName, AnimationClip source)
        {
            string path = $"{OutputDirectory}/{resourceName}.asset";
            var set = AssetDatabase.LoadAssetAtPath<GeneratedAnimationSet>(path);

            if (set == null)
            {
                set = ScriptableObject.CreateInstance<GeneratedAnimationSet>();
                AssetDatabase.CreateAsset(set, path);
            }

            AnimationClip saved = null;

            foreach (Object old in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (old == set) continue;

                if (old is AnimationClip clip && saved == null)
                {
                    EditorUtility.CopySerialized(source, clip);
                    clip.name = source.name;
                    EditorUtility.SetDirty(clip);
                    saved = clip;
                    continue;
                }

                Object.DestroyImmediate(old, true);
            }

            if (saved == null)
            {
                saved = source;
                AssetDatabase.AddObjectToAsset(saved, set);
            }

            set.name = resourceName;
            set.Clips = new[] { saved };
            EditorUtility.SetDirty(set);
        }

        private static bool EveryBindingFits(Transform root, string id,
                                             IEnumerable<AnimationClip> clips)
        {
            foreach (var clip in clips)
            {
                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                {
                    if (root.Find(binding.path) != null) continue;

                    Debug.LogError($"[GeneratedAnimationAuthor] {clip.name} path " +
                                   $"'{binding.path}' is missing on {id}.");
                    return false;
                }
            }

            return true;
        }
    }
}
