using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    // Compare the loaded scene's meaning after saved authoring runs. Unity assigns
    // new instance/file IDs when a generated group is replaced; those IDs alone
    // cannot establish a geometry, placement, collider or material difference.
    public static class MapRepeatabilityCheck
    {

        public static void Run()
        {
            string output = Environment.GetEnvironmentVariable("TUMP_MAP_REPEATABILITY") ?? "Logs/map-repeatability-v1";
            Directory.CreateDirectory(output);
            var report = new StringBuilder();
            bool passed = true;
            foreach (string map in new[] { "Eskinita", "BayanPlaza", "IlalimNgTulay" })
            {
                string path = "Assets/TumbangPreso/Scenes/Maps/" + map + ".unity";
                File.Copy(path, Path.Combine(output, map + "-before.unity"), true);
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var before = Capture(scene);
                Write(output, map, "before", before);
                SortedDictionary<string, string> first = null;
                for (int run = 1; run <= 2; run++)
                {
                    NeighborhoodFinishAuthor.FinishLoadedScene(map, report);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    AssetDatabase.SaveAssets();
                    // Reopen to include saved prefab overrides and serialization,
                    // not just two observations of the same in-memory objects.
                    scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    var current = Capture(scene);
                    Write(output, map, "run" + run, current);
                    if (run == 1)
                    {
                        first = current;
                        report.AppendLine(map + " baseline-to-run1: " + Differences(before, current).Count + " changed rows");
                    }
                    else
                    {
                        var changes = Differences(first, current);
                        report.AppendLine(map + " run1-to-run2: " + changes.Count + " changed rows; " + current.Count + " compared rows");
                        File.WriteAllLines(Path.Combine(output, map + "-differences.txt"), changes);
                        passed &= changes.Count == 0;
                    }
                }
            }
            report.AppendLine(passed ? "SEMANTIC REPEATABILITY PASS" : "SEMANTIC REPEATABILITY FAIL");
            File.WriteAllText(Path.Combine(output, "report.txt"), report.ToString());
            Debug.Log(report.ToString());
            EditorApplication.Exit(passed ? 0 : 1);
        }

        private static List<string> Differences(SortedDictionary<string, string> a, SortedDictionary<string, string> b)
            => a.Keys.Union(b.Keys).OrderBy(k => k, StringComparer.Ordinal)
                .Where(k => !a.TryGetValue(k, out var av) || !b.TryGetValue(k, out var bv) || av != bv)
                .Select(k => k + "\n BEFORE " + (a.TryGetValue(k, out var av) ? av : "<absent>") +
                    "\n AFTER  " + (b.TryGetValue(k, out var bv) ? bv : "<absent>")).ToList();

        private static void Write(string output, string map, string run, SortedDictionary<string, string> rows)
            => File.WriteAllLines(Path.Combine(output, map + "-" + run + ".txt"), rows.Select(p => p.Key + "=" + p.Value));

        private static SortedDictionary<string, string> Capture(Scene scene)
        {
            var rows = new SortedDictionary<string, string>(StringComparer.Ordinal);
            var assets = new HashSet<Object>();
            foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                string key = PathOf(t);
                rows.Add(key + "/GameObject", Normalize(t.gameObject));
                var occurrences = new Dictionary<Type, int>();
                foreach (var c in t.GetComponents<Component>())
                {
                    if (c == null) throw new InvalidOperationException("Missing script on " + key);
                    var type = c.GetType();
                    int n = occurrences.TryGetValue(type, out var count) ? count : 0;
                    occurrences[type] = n + 1;
                    rows.Add(key + "/" + type.FullName + "[" + n + "]", Normalize(c));
                    if (c is MeshFilter filter && filter.sharedMesh != null) assets.Add(filter.sharedMesh);
                    if (c is MeshCollider collider && collider.sharedMesh != null) assets.Add(collider.sharedMesh);
                    if (c is Renderer renderer)
                        foreach (var material in renderer.sharedMaterials) if (material != null) assets.Add(material);
                }
            }
            foreach (var asset in assets)
                rows["Asset/" + Reference(asset)] = Hash(Normalize(asset));
            rows["RenderSettings"] = FormattableString.Invariant(
                $"{RenderSettings.ambientMode}|{RenderSettings.ambientIntensity:R}|{ColorText(RenderSettings.ambientSkyColor)}|{ColorText(RenderSettings.ambientEquatorColor)}|{ColorText(RenderSettings.ambientGroundColor)}|{RenderSettings.fog}|{ColorText(RenderSettings.fogColor)}|{RenderSettings.fogMode}|{RenderSettings.fogDensity:R}|{RenderSettings.fogStartDistance:R}|{RenderSettings.fogEndDistance:R}|{Reference(RenderSettings.skybox)}|{Reference(RenderSettings.sun)}|{RenderSettings.reflectionIntensity:R}");
            return rows;
        }

        private static string ColorText(Color c) => FormattableString.Invariant($"{c.r:R},{c.g:R},{c.b:R},{c.a:R}");
        private static string Hash(string data)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(data))).Replace("-", "");
        }

        private static string Normalize(Object o)
        {
            var rows = new List<string>();
            using (var serialized = new SerializedObject(o))
            {
                var p = serialized.GetIterator();
                bool enterChildren = true;
                while (p.Next(enterChildren))
                {
                    // Descend into structural containers only. Object references
                    // expose raw file/path IDs as children in Unity 6; visiting
                    // those would put the ignored identity back into the snapshot.
                    enterChildren = p.propertyType == SerializedPropertyType.Generic;
                    if (p.propertyPath == "m_CorrespondingSourceObject" || p.propertyPath == "m_PrefabInstance" ||
                        p.propertyPath == "m_PrefabAsset" || p.propertyPath == "m_RootOrder") continue;
                    string value;
                    switch (p.propertyType)
                    {
                        case SerializedPropertyType.Generic: continue;
                        case SerializedPropertyType.ObjectReference: value = Reference(p.objectReferenceValue); break;
                        case SerializedPropertyType.Integer:
                        case SerializedPropertyType.ArraySize:
                        case SerializedPropertyType.LayerMask:
                        case SerializedPropertyType.Enum: value = p.longValue.ToString(CultureInfo.InvariantCulture); break;
                        case SerializedPropertyType.Float: value = p.doubleValue.ToString("R", CultureInfo.InvariantCulture); break;
                        case SerializedPropertyType.Boolean: value = p.boolValue.ToString(); break;
                        case SerializedPropertyType.String: value = p.stringValue; break;
                        case SerializedPropertyType.AnimationCurve:
                            value = JsonUtility.ToJson(new CurveValue { curve = p.animationCurveValue }); break;
                        case SerializedPropertyType.Gradient:
                            value = JsonUtility.ToJson(new GradientValue { gradient = p.gradientValue }); break;
                        default: value = JsonUtility.ToJson(p.boxedValue); break;
                    }
                    rows.Add(p.propertyPath + ":" + value);
                }
            }
            return string.Join("|", rows);
        }

        [Serializable] private sealed class CurveValue { public AnimationCurve curve; }
        [Serializable] private sealed class GradientValue { public Gradient gradient; }

        private static string Reference(Object o)
        {
            if (o == null) return "null";
            if (EditorUtility.IsPersistent(o) && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o, out string guid, out long id))
                return guid + ":" + id;
            if (o is GameObject go) return PathOf(go.transform) + "/GameObject";
            if (o is Component c)
            {
                int index = Array.IndexOf(c.GetComponents(c.GetType()), c);
                return PathOf(c.transform) + "/" + c.GetType().FullName + "[" + index + "]";
            }
            // Converted maps retain embedded materials/meshes, which have no
            // external asset GUID. Their contents, not their save-time file ID,
            // identify them. This also detects a generator silently repainting one.
            if (o is Material || o is Mesh || o is Texture)
                return o.GetType().Name + ":" + o.name + ":" + Hash(Normalize(o));
            if (o is Shader) return "Shader:" + o.name;
            throw new InvalidOperationException("Unresolved scene reference: " + o.GetType().FullName + " / " + o.name);
        }

        private static string PathOf(Transform t)
        {
            var siblings = t.parent != null ? t.parent.Cast<Transform>() :
                t.gameObject.scene.GetRootGameObjects().Select(g => g.transform);
            int sameName = siblings.TakeWhile(s => s != t).Count(s => s.name == t.name);
            string part = t.name.Replace("\\", "\\\\").Replace("\"", "\\\"") + "[" + sameName + "]";
            return t.parent == null ? part : PathOf(t.parent) + "/" + part;
        }
    }
}
