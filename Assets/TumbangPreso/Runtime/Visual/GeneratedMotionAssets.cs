using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    // Retain authored data only. Loading never creates an actor or an animation graph.
    public static class GeneratedMotionAssets
    {
        private static readonly Dictionary<string, GeneratedAnimationSet> Cache = new Dictionary<string, GeneratedAnimationSet>();
        private static readonly string[] Folders =
            { DanceClip.ResourceFolder, "CarryMotion", SwimmingMotion.Folder, RecoveryMotion.Folder, RootedMotion.Folder };

        public static GeneratedAnimationSet For(string folder, string rig)
        {
            if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(rig)) return null;
            string path = folder + "/" + rig;
            if (Cache.TryGetValue(path, out var cached) && cached != null) return cached;
            var asset = Resources.Load<GeneratedAnimationSet>(path);
            if (asset != null) Cache[path] = asset;
            return asset;
        }

        public static IEnumerator Warmup(GameObject model)
        {
            if (model == null) yield break;
            var animator = model.GetComponentInChildren<Animator>();
            string rig = DanceClip.ResourceName(animator != null ? animator.transform : model.transform);
            if (string.IsNullOrEmpty(rig)) yield break;
            foreach (string folder in Folders)
            {
                string path = folder + "/" + rig;
                if (Cache.TryGetValue(path, out var cached) && cached != null) continue;
                var request = Resources.LoadAsync<GeneratedAnimationSet>(path);
                yield return request;
                var asset = request.asset as GeneratedAnimationSet;
                if (asset != null) Cache[path] = asset;
                yield return null;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Cache.Clear();
    }
}
