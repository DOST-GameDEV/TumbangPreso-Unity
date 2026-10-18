using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.Map;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// THE PERFORMANCE PASS'S STILLS AND SHEET (2026-10-18). Request words, through `PayoRequestWatcher`:
    ///  * `stills LABEL`: renders, by day in the first layout, with the review's own witness camera (the scene's grade, the
    ///    world outline at this map's ink distances, the "Payo" look), to `Logs/payo/unity/perf/LABEL_SHOT.png`:
    ///      can_s       the court eye (1.7 m over the can) looking south to the town, 95 degrees wide (the review's can_s)
    ///      can_n       the same looking north down the valley (the review's can_n)
    ///      can_sun     the same looking toward the sun (the backlit side)
    ///      grass_close a player's eye 1.5 m over the densest grass near the court (`PayoBlades.GrassSample`), toward the sun
    ///      grass_lip   from the court's edge down onto that grass
    ///      paddy_close 1.5 m over the densest rice near the court (`PayoBlades.RiceSample`)
    ///  * `sheet`: `Logs/payo/unity/perf/compare.png` from `sheet.txt` beside it, a row a line: `label|left.png|right.png`.
    /// </summary>
    public static partial class PayoSceneBuilder
    {
        private const string PerfFolder = "Logs/payo/unity/perf";
        /// <summary>The close-ups' spots, fixed so a before and an after look at the same ground: the lawn under the court's
        /// south side and a paddy west of it (the first bake's densest patches, 2026-10-18).</summary>
        private static readonly Vector3 GrassSpot = new Vector3(9.13f, -6.19f, -66.71f), RiceSpot = new Vector3(-56.97f, -5.00f, 14.49f);

        internal static string Stills(string label)
        {
            if (!File.Exists(ScenePath)) throw new FileNotFoundException("The scene is not built yet: " + ScenePath);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Directory.CreateDirectory(PerfFolder);
            var sceneRoot = GameObject.Find(MapName);
            var stage = Object.FindFirstObjectByType<ArenaStage>();
            var night = Object.FindFirstObjectByType<PayoNight>();
            var blades = Object.FindFirstObjectByType<PayoBlades>();
            if (sceneRoot == null || stage == null) throw new InvalidOperationException("The built scene has no '" + MapName + "' root or no stage");

            var camera = new GameObject("Payo perf witness").AddComponent<Camera>();
            camera.enabled = false; camera.nearClipPlane = 0.1f; camera.farClipPlane = FreeFar;
            camera.depthTextureMode |= DepthTextureMode.Depth;
            camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            var outline = camera.gameObject.AddComponent<WorldOutline>();
            outline.PrototypeEnabled = true;
            outline.SetFade(InkFadeStart, InkFadeEnd);
            WorldLookPresentation worn = null;
            var written = new List<string>();
            try
            {
                var sun = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);
                try
                {
                    camera.gameObject.AddComponent<WorldLookCamera>();
                    worn = WorldLookPresentation.InstallPreview(sceneRoot.transform, 0f, sun);
                }
                catch (Exception e) { Warn("the world look preview failed: " + e.Message); }
                Show(stage, 0);
                Physics.SyncTransforms();
                // A label with "plain" in it: the far land wears its own materials again for these stills (a check of what
                // leaving the ink's prepass changes). Not saved: the scene is reopened after.
                PayoFlora.EveryCaster = label.Contains("allcast");
                if (label.Contains("plain"))
                    foreach (var r in sceneRoot.GetComponentsInChildren<Renderer>(true))
                    {
                        var mats = r.sharedMaterials; bool swapped = false;
                        for (int i = 0; i < mats.Length; i++)
                        {
                            if (mats[i] == null || !mats[i].name.EndsWith("_far", StringComparison.Ordinal)) continue;
                            var own = AssetDatabase.LoadAssetAtPath<Material>($"{Root}/Materials/{mats[i].name.Substring(0, mats[i].name.Length - 4)}.mat");
                            if (own != null) { mats[i] = own; swapped = true; }
                        }
                        if (swapped) r.sharedMaterials = mats;
                    }
                if (night != null) night.Apply(0f);
                if (blades != null) blades.Push();

                Vector3 foot = Vector3.zero;
                if (Physics.Raycast(Vector3.up * 20f, Vector3.down, out var hit, 40f, ~0, QueryTriggerInteraction.Ignore)) foot.y = hit.point.y;
                Vector3 eye = foot + Vector3.up * 1.7f;
                // Toward the sun, on the flat: the light shines along its forward, so the sun is behind -forward.
                Vector3 toSun = sun != null ? -sun.transform.forward : new Vector3(0f, 0.5f, 1f);
                Vector3 sunFlat = new Vector3(toSun.x, 0f, toSun.z).sqrMagnitude > 1e-4f ? new Vector3(toSun.x, 0f, toSun.z).normalized : Vector3.forward;

                var shots = new List<(string name, Vector3 at, Vector3 look, float fov)>
                {
                    ("can_s", eye, eye + new Vector3(0f, 0.02f, -1f), 95f),
                    ("can_n", eye, eye + new Vector3(0f, 0.12f, 1f), 95f),
                    ("can_sun", eye, eye + sunFlat + Vector3.up * 0.05f, 95f),
                    ("air", new Vector3(58f, 92f, -74f), new Vector3(0f, -4f, 4f), 62f),
                };
                {
                    Vector3 g = GrassSpot;
                    Vector3 fromCourt = new Vector3(g.x, 0f, g.z).normalized;
                    // Stand 3 m on the shadow side of the patch, looking across it toward the sun: the backlit view.
                    Vector3 stand = g - sunFlat * 3.0f;
                    stand.y = LandHeight(stand, g.y) + 1.5f;
                    shots.Add(("grass_close", stand, g + sunFlat * 4.0f + Vector3.up * 0.2f, 80f));
                    Vector3 lip = fromCourt * 30.0f; lip.y = foot.y + 1.7f;
                    shots.Add(("grass_lip", lip, g, 70f));
                    // A raised eye (a block's top, the break camera) 25 m off across the patch.
                    Vector3 high = g - fromCourt * 25.0f + Vector3.up * 9.0f;
                    shots.Add(("grass_mid", high, g + fromCourt * 10.0f, 75f));
                }
                {
                    Vector3 r = RiceSpot;
                    Vector3 fromCourt = new Vector3(r.x, 0f, r.z).normalized;
                    Vector3 stand = r - fromCourt * 3.0f;
                    stand.y = LandHeight(stand, r.y) + 1.5f;
                    shots.Add(("paddy_close", stand, r + fromCourt * 5.0f, 80f));
                }
                foreach (var s in shots)
                {
                    camera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(s.fov, 16f / 9f);
                    camera.transform.SetPositionAndRotation(s.at, Quaternion.LookRotation(s.look - s.at, Vector3.up));
                    string path = Path.Combine(PerfFolder, label + "_" + s.name + ".png");
                    Render(camera, path);
                    written.Add(s.name);
                }
            }
            finally
            {
                PayoFlora.EveryCaster = false;
                try { if (night != null) night.Apply(0f); } catch (Exception) { }
                if (worn != null) Object.DestroyImmediate(worn.gameObject);
                Object.DestroyImmediate(camera.gameObject);
            }
            string flora = "";
            var instanced = Object.FindFirstObjectByType<PayoFlora>();
            if (instanced != null) flora = "; " + instanced.Report();
            return $"{written.Count} stills in {PerfFolder}/{label}_*.png: {string.Join(", ", written)}" +
                   (blades != null ? $"; grass sample {blades.GrassSample}, rice sample {blades.RiceSample}" : "; NO PayoBlades") + flora;
        }

        /// <summary>The land's height under a point (the land has no colliders: the highest renderer bounds hit is not enough,
        /// so a ray against lent mesh colliders on the land pieces near it), or the fallback.</summary>
        private static float LandHeight(Vector3 at, float fallback)
        {
            var lent = new List<MeshCollider>();
            var all = new List<Collider>();
            try
            {
                foreach (var filter in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
                {
                    if (filter.sharedMesh == null) continue;
                    bool land = false;
                    for (var t = filter.transform; t != null && !land; t = t.parent) land = IsLand(t.name) || t.name == "Terraces";
                    if (!land) continue;
                    var r = filter.GetComponent<Renderer>();
                    if (r == null) continue;
                    var b = r.bounds; b.Expand(new Vector3(2f, 2000f, 2f));
                    if (!b.Contains(at)) continue;
                    var own = filter.GetComponent<Collider>();
                    if (own != null) { all.Add(own); continue; }
                    var c = filter.gameObject.AddComponent<MeshCollider>(); c.sharedMesh = filter.sharedMesh; lent.Add(c); all.Add(c);
                }
                Physics.SyncTransforms();
                float best = float.NegativeInfinity;
                foreach (var c in all)
                    if (c.Raycast(new Ray(new Vector3(at.x, 1000f, at.z), Vector3.down), out var hit, 2000f) && hit.point.y > best) best = hit.point.y;
                return float.IsNegativeInfinity(best) ? fallback : best;
            }
            finally { foreach (var c in lent) if (c != null) Object.DestroyImmediate(c); }
        }

        internal static string PerfSheet()
        {
            string list = Path.Combine(PerfFolder, "sheet.txt");
            if (!File.Exists(list)) throw new FileNotFoundException("no " + list);
            var rows = File.ReadAllLines(list).Select(l => l.Split('|')).Where(p => p.Length >= 3).ToList();
            const int w = 800, h = 450, gap = 6;
            var sheet = new Texture2D(w * 2 + gap * 3, (h + gap) * rows.Count + gap, TextureFormat.RGB24, false);
            var fill = new Color32[sheet.width * sheet.height];
            for (int i = 0; i < fill.Length; i++) fill[i] = new Color32(24, 24, 24, 255);
            sheet.SetPixels32(fill);
            for (int row = 0; row < rows.Count; row++)
            {
                int y = sheet.height - (row + 1) * (h + gap);
                Blit(sheet, rows[row][1].Trim(), gap, y, w, h);
                Blit(sheet, rows[row][2].Trim(), gap * 2 + w, y, w, h);
            }
            sheet.Apply();
            string path = Path.Combine(PerfFolder, "compare.png");
            File.WriteAllBytes(path, sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
            File.WriteAllText(Path.Combine(PerfFolder, "compare.txt"), "compare.png rows, top to bottom (left BEFORE, right AFTER):\n" +
                string.Join("\n", rows.Select(r => $"{r[0]}: {r[1]} | {r[2]}")));
            return $"{path}: {rows.Count} rows";
        }
    }
}
