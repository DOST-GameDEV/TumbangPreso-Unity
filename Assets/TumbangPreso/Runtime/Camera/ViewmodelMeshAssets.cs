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
            mesh = Resources.Load<Mesh>(path);
            if (mesh != null) Cache[path] = mesh;
            return mesh;
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
                    if (request.asset is Mesh loaded) Cache[path] = loaded;
                }
                progress?.Invoke((index + 1f) / paths.Count);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Cache.Clear();
    }
}
