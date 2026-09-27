using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public static class HeroPropAssets
    {
        private static readonly string[] Folders =
        {
            PaeteProp.ResourceFolder, ReworkProp.ResourceFolder, PhaisterProp.ResourceFolder
        };
        private static readonly Dictionary<string, GameObject[]> RetainedFolders = new Dictionary<string, GameObject[]>();
        private static readonly Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();

        public static GameObject Load(string folder, string name)
        {
            string path = folder + "/" + name;
            if (Prefabs.TryGetValue(path, out var prefab) && prefab != null) return prefab;
            prefab = Resources.Load<GameObject>(path);
            if (prefab != null) Prefabs[path] = prefab;
            return prefab;
        }

        public static IEnumerator Warmup(Action<float> progress = null)
        {
            for (int i = 0; i < Folders.Length; i++)
            {
                string folder = Folders[i];
                if (!RetainedFolders.TryGetValue(folder, out var prefabs) || !Intact(prefabs))
                {
                    // Folder discovery includes new authored props without a second filename
                    // list. Keep prefab roots alive: the general boot cache excludes GameObjects.
                    prefabs = Resources.LoadAll<GameObject>(folder);
                    RetainedFolders[folder] = prefabs;
                    if (prefabs.Length == 0) Debug.LogWarning("[HeroPropAssets] No props found in " + folder);
                }
                progress?.Invoke((i + 1f) / Folders.Length);
                yield return null;
            }
        }

        private static bool Intact(GameObject[] prefabs)
        {
            if (prefabs == null || prefabs.Length == 0) return false;
            foreach (var prefab in prefabs) if (prefab == null) return false;
            return true;
        }
    }
}
