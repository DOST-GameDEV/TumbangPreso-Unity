using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>One connected outer residential block, using the retained Eskinita bodies.</summary>
    public static class EskinitaContextAuthor
    {
        public const string RootName = "EskinitaContextRefinement";
        [Serializable] private sealed class SourceLot { public string id, retainedFamily; }
        [Serializable] private sealed class SourcePlan { public SourceLot[] slots; }

        public static void ClearPrevious(string map)
        {
            if (map != "Eskinita") return;
            var previous = GameObject.Find("Eskinita/Dressing/" + RootName);
            if (previous != null) Undo.DestroyObjectImmediate(previous);
        }

        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/Eskinita.unity", OpenSceneMode.Single);
            var report = new StringBuilder();
            FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory("Logs/eskinita-context");
            File.WriteAllText("Logs/eskinita-context/author.txt", report.ToString());
            MapFinalInventory.WriteLoadedScene("Eskinita", "Logs/eskinita-context");
            Debug.Log(report.ToString());
            EditorApplication.Exit(0);
        }

        public static void FinishLoadedScene(StringBuilder report)
        {
            var map = GameObject.Find("Eskinita");
            if (map == null) throw new InvalidOperationException("Eskinita must be the loaded authoring target.");
            var dressing = map.transform.Find("Dressing");
            var houses = dressing != null ? dressing.Find("Bahay") : null;
            var retained = dressing != null ? dressing.Find("NeighborhoodRework") : null;
            if (houses == null || retained == null) throw new InvalidOperationException("Retained Eskinita neighborhood is missing.");
            var sourcePlan = JsonUtility.FromJson<SourcePlan>(File.ReadAllText("MapSource/environment/layouts/eskinita-neighborhood-plan-v1.json"));
            var sources = new Dictionary<char, Transform>();
            foreach (var lot in sourcePlan.slots.Where(s => s.id.StartsWith("Bahay_")))
            {
                char family = lot.retainedFamily[0];
                if (sources.ContainsKey(family)) continue;
                var source = houses.Find("Bahay_Rework_" + lot.id);
                if (source == null || !source.gameObject.activeInHierarchy)
                    throw new InvalidOperationException("Missing active retained source: " + lot.id);
                sources.Add(family, source);
            }
            var road = retained.Find("Neighborhood cross-street");
            var path = retained.Find("Cross-street footpath");
            if (road == null || path == null) throw new InvalidOperationException("Retained street materials/geometry are missing.");

            // Replace only this author's owned group. All existing lots, source assets and
            // gameplay collision stay in place. The full map author calls this after finishes.
            var previous = dressing.Find(RootName);
            if (previous != null) Undo.DestroyObjectImmediate(previous.gameObject);
            var originalSolids = map.GetComponentsInChildren<Collider>(true)
                .ToDictionary(c => c, c => c.bounds);
            var root = new GameObject(RootName).transform;
            root.SetParent(dressing, false);
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Refine Eskinita context");
            var bodies = new List<Bounds>();
            var names = new List<string>();

            // Deliberate street lots, not random scatter. These roads connect to the existing
            // cross-streets at z +/-29.5 and continue behind the retained second house row.
            AddRow(root, sources, bodies, names, "West", "baceoc", true, -37f, -22f, 8.8f);
            AddRow(root, sources, bodies, names, "East", "coabea", true, 37f, -22f, 8.8f);
            AddRow(root, sources, bodies, names, "North", "caoeacb", false, 55.2f, -26.4f, 8.8f);
            AddRow(root, sources, bodies, names, "South", "ecaobae", false, -55.2f, -26.4f, 8.8f);

            foreach (float side in new[] { -1f, 1f })
            {
                Surface(root, road, "Side street " + side, new Vector3(side * 32.2f, .089f, 0), new Vector3(6, .02f, 106));
                // Keep crossings open: footpaths stop short of both existing and new junctions.
                foreach (var span in new[] { new Vector2(-25.7f, 25.7f), new Vector2(33.3f, 46.2f), new Vector2(-46.2f, -33.3f) })
                    foreach (float edge in new[] { -1f, 1f })
                        Surface(root, path, "Side footpath", new Vector3(side * 32.2f + edge * 3.9f, .095f, (span.x + span.y) * .5f),
                            new Vector3(1.8f, .03f, span.y - span.x));
                Surface(root, road, "Outer cross-street " + side, new Vector3(0, .089f, side * 50), new Vector3(88, .02f, 6));
                foreach (float edge in new[] { -1f, 1f })
                    foreach (var span in new[] { new Vector2(-28.4f, 28.4f), new Vector2(-44f, -36f), new Vector2(36f, 44f) })
                        Surface(root, path, "Outer footpath", new Vector3((span.x + span.y) * .5f, .095f, side * 50 + edge * 3.9f),
                            new Vector3(span.y - span.x, .03f, 1.8f));
            }

            for (int i = 0; i < bodies.Count; i++)
            {
                var a = bodies[i];
                if (Mathf.Abs(a.min.y - .1f) > .01f) throw new InvalidOperationException(names[i] + " is not grounded.");
                if (a.min.x < 28 && a.max.x > -28 && a.min.z < 47 && a.max.z > -47)
                    throw new InvalidOperationException(names[i] + " enters the retained neighborhood.");
                for (int j = 0; j < i; j++)
                {
                    var b = bodies[j];
                    float x = Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x);
                    float z = Mathf.Min(a.max.z, b.max.z) - Mathf.Max(a.min.z, b.min.z);
                    if (x > .02f && z > .02f) throw new InvalidOperationException(names[i] + " overlaps " + names[j]);
                }
                report.AppendLine(names[i] + " " + a);
            }
            if (root.GetComponentsInChildren<Collider>(true).Length != 0)
                throw new InvalidOperationException("Visual neighborhood context must not add gameplay collision.");
            if (map.GetComponentsInChildren<Collider>(true).Length != originalSolids.Count ||
                originalSolids.Any(pair => pair.Key == null || pair.Key.bounds != pair.Value))
                throw new InvalidOperationException("Existing gameplay collision changed during context authoring.");
            report.AppendLine("Eskinita only:26 retained-family background bodies,4 connected road segments; no new gameplay colliders or source material edits.");
        }

        private static void AddRow(Transform root, Dictionary<char, Transform> sources, List<Bounds> bounds,
            List<string> names, string label, string families, bool xEdge, float edge, float first, float step)
        {
            for (int i = 0; i < families.Length; i++)
            {
                // Reuse the scene's already-finished mesh/material references. Original GLBs and
                // shared finishes are not modified. Fine near-house additions are intentionally
                // omitted at this distance; the retained body includes real roofs/window frames.
                var source = sources[families[i]];
                var go = Object.Instantiate(source.gameObject, root);
                // EnvColourPass recognises the retained generator's Bahay_ building contract.
                go.name = "Bahay_Context_" + label + "_" + (i + 1).ToString("00") + "_" + families[i];
                RemoveCollision(go);
                float yaw = xEdge ? (edge < 0 ? 90 : 270) : (edge < 0 ? 0 : 180);
                go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0, yaw, 0));
                var b = BoundsOf(go);
                float front = xEdge ? (edge < 0 ? b.max.x : b.min.x) : (edge < 0 ? b.max.z : b.min.z);
                float along = first + i * step;
                go.transform.position = xEdge ? new Vector3(edge - front, .1f - b.min.y, along - b.center.z)
                    : new Vector3(along - b.center.x, .1f - b.min.y, edge - front);
                foreach (Transform child in go.GetComponentsInChildren<Transform>(true)) child.gameObject.isStatic = true;
                bounds.Add(BoundsOf(go)); names.Add(go.name);
            }
        }

        private static void Surface(Transform root, Transform source, string name, Vector3 at, Vector3 size)
        {
            var go = Object.Instantiate(source.gameObject, root);
            go.name = name;
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            go.transform.localScale = Vector3.one;
            RemoveCollision(go);
            var unit = BoundsOf(go);
            go.transform.localScale = new Vector3(size.x / unit.size.x, size.y / unit.size.y, size.z / unit.size.z);
            var b = BoundsOf(go);
            go.transform.position = at - b.center;
            go.isStatic = true;
        }

        private static void RemoveCollision(GameObject go)
        { foreach (var collider in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider); }

        private static Bounds BoundsOf(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("No mesh in " + go.name);
            var result = renderers[0].bounds;
            foreach (var renderer in renderers) result.Encapsulate(renderer.bounds);
            return result;
        }
    }
}
