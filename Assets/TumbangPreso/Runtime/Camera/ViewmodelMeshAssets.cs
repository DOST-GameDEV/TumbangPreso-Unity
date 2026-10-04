using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    public static class ViewmodelMeshAssets
    {
        private static readonly Dictionary<string, Mesh> Cache = new Dictionary<string, Mesh>();
        private static readonly string[] SharedPaths =
        {
            "Models/viewmodel_arm", "Models/tsinelas_classic",
            "Models/FppDetails/inday_left_arm", "Models/FppDetails/inday_right_arm",
        };

        public static Mesh Load(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (Cache.TryGetValue(path, out var mesh) && mesh != null) return mesh;
            return RetainWorkingCopy(path, Resources.Load<Mesh>(path));
        }

        public static IEnumerator Warmup(RosterBook book, Action<float> progress = null)
        {
            var paths = new List<string>(SharedPaths);
            var seen = new HashSet<string>(SharedPaths);
            if (book?.People != null)
                foreach (var person in book.People)
                {
                    if (person == null || string.IsNullOrEmpty(person.Id)) continue;
                    string left = "Models/RosterArms/" + person.Id + "_left";
                    string right = "Models/RosterArms/" + person.Id + "_right";
                    if (seen.Add(left)) paths.Add(left);
                    if (seen.Add(right)) paths.Add(right);
                }
            for (int index = 0; index < paths.Count; index++)
            {
                string path = paths[index];
                if (!Cache.TryGetValue(path, out var mesh) || mesh == null)
                {
                    var request = Resources.LoadAsync<Mesh>(path);
                    yield return request;
                    if (request.asset is Mesh loaded) RetainWorkingCopy(path, loaded);
                }
                progress?.Invoke((index + 1f) / paths.Count);
            }
        }

        private static Mesh RetainWorkingCopy(string path, Mesh source)
        {
            if (source == null) return null;
            // A synchronous caller may have populated the cache while Warmup yielded.
            if (Cache.TryGetValue(path, out var retained) && retained != null) return retained;
            // Outline welding writes tangents. Serialized .asset meshes must remain source data.
            var copy = UnityEngine.Object.Instantiate(source);
            copy.name = source.name + " (viewmodel)";
            copy.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            Cache[path] = copy;
            return copy;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            foreach (var mesh in Cache.Values)
            {
                if (mesh == null) continue;
                Visual.OutlineNormals.Forget(mesh);
                if (Application.isPlaying) UnityEngine.Object.Destroy(mesh);
                else UnityEngine.Object.DestroyImmediate(mesh);
            }
            Cache.Clear();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void HookEditorCleanup()
        {
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }
        private static void OnPlayModeChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.EnteredEditMode) Reset();
        }
#endif
    }
}
