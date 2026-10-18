using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.Map;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    public static partial class PayoSceneBuilder
    {
        private static string NextReviewFolder()
        {
            int v = 1; while (Directory.Exists("Logs/payo/unity/v" + v)) v++; return "Logs/payo/unity/v" + v;
        }

        /// <summary>
        /// Opens the built scene and renders stills at 1600 x 900 to `folder/payo_LAYOUT_TIME_SHOT.png`: for each layout,
        /// by day and by night, from the air (`air`) and a three-quarter view of the stage (`stage`); for the first
        /// layout a player's eye from the court looking north, east, south and west (`eye_n` and so on), day and
        /// night; for the others the eye looking north by day.
        ///
        /// The witness camera wears what the game's camera wears (the scene's grade, the world outline with this map's
        /// ink distances, the "Payo" look installed the way the map preview installs it). ⚠️ EDIT MODE: a layout is
        /// shown by switching its solids on (no travel), the moving slabs stand where they start, the night is posed
        /// once (`PayoNight.Apply(1)`, `PayoNightLife.ReviewPose`: fireflies frozen mid-flight, two shooting stars
        /// stood in the sky, the bowls lit), and no body, can or chalk is in the picture (the match makes those).
        /// </summary>
        public static string Review(string output)
        {
            if (!File.Exists(ScenePath)) throw new FileNotFoundException("The scene is not built yet: " + ScenePath);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Directory.CreateDirectory(output);
            var sceneRoot = GameObject.Find(MapName);
            var stage = Object.FindFirstObjectByType<ArenaStage>();
            var night = Object.FindFirstObjectByType<PayoNight>();
            var life = Object.FindFirstObjectByType<PayoNightLife>();
            if (sceneRoot == null || stage == null) throw new InvalidOperationException("The built scene has no '" + MapName + "' root or no stage");

            var camera = new GameObject("Payo review witness").AddComponent<Camera>();
            camera.enabled = false; camera.nearClipPlane = 0.1f; camera.farClipPlane = FreeFar;
            camera.depthTextureMode |= DepthTextureMode.Depth;
            camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            var outline = camera.gameObject.AddComponent<WorldOutline>();
            outline.PrototypeEnabled = true;
            outline.SetFade(InkFadeStart, InkFadeEnd);
            WorldLookPresentation worn = null;
            int written = 0;
            var names = new List<string>();
            var counts = new System.Text.StringBuilder(); var cover = new System.Text.StringBuilder();
            counts.AppendLine("WHAT IS IN VIEW FROM THE CAN (1.7 m, 95 degrees wide, 16:9), counted from the scene in edit mode: renderers whose bounds meet the");
            counts.AppendLine("camera's frustum (no occlusion), the distinct materials they wear, and their triangles. NOT set-pass calls: those are the play probe's (the game view's own counter).");
            cover.AppendLine("EACH BLOCK'S ART AGAINST ITS COLLISION, per layout: at nine points of the block's top, the art's top minus the collider's top in metres");
            cover.AppendLine("(the art is placed 0.01 under; a stair whose flight ran the wrong way would read metres off at its ends).");
            try
            {
                var sun = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);
                try
                {
                    camera.gameObject.AddComponent<WorldLookCamera>();
                    worn = WorldLookPresentation.InstallPreview(sceneRoot.transform, 0f, sun);
                    if (worn == null) Warn("the review wears NO world look (no '" + MapName + "' row found): no blocky clouds, the scene's own light only");
                }
                catch (Exception e) { Warn("the world look preview failed: " + e.Message); }

                for (int l = 0; l < stage.LayoutCount; l++)
                {
                    Show(stage, l);
                    Physics.SyncTransforms();
                    string layout = stage.Layouts[l].Name;
                    var shots = new List<(string name, Vector3 at, Vector3 look, float fov)>
                    {
                        ("air", new Vector3(58f, 92f, -74f), new Vector3(0f, -4f, 4f), 62f),
                        ("stage", new Vector3(31f, 19f, -36f), new Vector3(0f, 0f, 3f), 66f),
                    };
                    cover.AppendLine(Cover(stage, l));
                    // A player's eye, 1.7 m over the stone that is really there: at the can (the court's middle), and on the four mid blocks.
                    var stands = new List<(string name, Vector3 at)>();
                    foreach (var stand in new[] { ("can", new Vector3(0f, 0f, 0f)), ("midn", new Vector3(0f, 0f, 8f)), ("mide", new Vector3(8f, 0f, 0f)), ("mids", new Vector3(0f, 0f, -8f)), ("midw", new Vector3(-8f, 0f, 0f)) })
                    {
                        Vector3 foot = stand.Item2;
                        if (Physics.Raycast(foot + Vector3.up * 20f, Vector3.down, out var hit, 40f, ~0, QueryTriggerInteraction.Ignore)) foot.y = hit.point.y;
                        stands.Add((stand.Item1, foot + Vector3.up * 1.7f));
                    }
                    for (int time = 0; time < 2; time++)
                    {
                        bool dark = time == 1;
                        var all = new List<(string name, Vector3 at, Vector3 look, float fov)>(dark ? shots.Take(1) : shots);
                        // THE TONE: stone the sun is on, the same stone's shade side, and the valley's distance, in one frame
                        // (for the owner's reference: cool blue light, grey stone).
                        if (l == 0 && !dark) all.Add(("tone", new Vector3(36f, 9f, 2f), new Vector3(-8f, 1f, 42f), 80f));
                        if (l == 0)
                            foreach (var stand in stands)
                            {
                                all.Add((stand.name + "_n", stand.at, stand.at + new Vector3(0f, 0.12f, 1f), 95f));
                                all.Add((stand.name + "_e", stand.at, stand.at + new Vector3(1f, 0.02f, 0f), 95f));
                                all.Add((stand.name + "_s", stand.at, stand.at + new Vector3(0f, 0.02f, -1f), 95f));
                                all.Add((stand.name + "_w", stand.at, stand.at + new Vector3(-1f, 0.02f, 0f), 95f));
                                if (stand.name != "can") continue;
                                all.Add((stand.name + "_ne", stand.at, stand.at + new Vector3(1f, 0.06f, 1f), 95f));
                                all.Add((stand.name + "_se", stand.at, stand.at + new Vector3(1f, 0.02f, -1f), 95f));
                                all.Add((stand.name + "_sw", stand.at, stand.at + new Vector3(-1f, 0.02f, -1f), 95f));
                                all.Add((stand.name + "_nw", stand.at, stand.at + new Vector3(-1f, 0.06f, 1f), 95f));
                            }
                        else if (!dark) all.Add(("can_n", stands[0].at, stands[0].at + new Vector3(0f, 0.12f, 1f), 95f));
                        if (night != null) night.Apply(dark ? 1f : 0f);
                        else if (dark) { Warn("the scene has no PayoNight: no night stills"); continue; }
                        foreach (var s in all)
                        {
                            camera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(s.fov, 16f / 9f);
                            camera.transform.SetPositionAndRotation(s.at, Quaternion.LookRotation(s.look - s.at, Vector3.up));
                            if (life != null)
                            {
                                try { life.ReviewPose(camera, dark ? 1f : 0f, 3.3f); }
                                catch (Exception e) { Warn("the night's life could not be posed: " + e.Message); life = null; }
                            }
                            string name = $"payo_{layout}_{(dark ? "night" : "day")}_{s.name}";
                            if (l == 0 && s.name.StartsWith("can_", StringComparison.Ordinal)) counts.AppendLine(InView(camera, name));
                            Render(camera, Path.Combine(output, name + ".png"));
                            names.Add(name); written++;
                        }
                    }
                }
            }
            finally
            {
                try { if (life != null) { life.ReviewPose(camera, 0f, 0f); life.Clear(); } } catch (Exception) { }
                try { if (night != null) night.Apply(0f); } catch (Exception) { }
                if (worn != null) Object.DestroyImmediate(worn.gameObject);
                Object.DestroyImmediate(camera.gameObject);
            }
            try
            {
                int all = 0, lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
                var distinct = new HashSet<Material>();
                foreach (var r in sceneRoot.GetComponentsInChildren<Renderer>(true)) { all++; foreach (var m in r.sharedMaterials) if (m != null) distinct.Add(m); }
                counts.AppendLine($"THE WHOLE SCENE: {all} renderers (shown or not, all five layouts' solids), {distinct.Count} distinct materials, {lights} lights");
                File.WriteAllText(Path.Combine(output, "stats.txt"), counts.ToString());
                File.WriteAllText(Path.Combine(output, "cover.txt"), cover.ToString());
            }
            catch (Exception e) { Warn("the counts could not be written: " + e.Message); }
            string compared;
            try { compared = Compare(output); }
            catch (Exception e) { compared = "the Lagoon Cove comparison still could not be rendered: " + e.Message; Warn(compared); }
            string summary = $"{written} review renders in {output} ({compared}): {string.Join(", ", names)}";
            Debug.Log(Tag + summary);
            return summary;
        }

        /// <summary>
        /// THE SAME WITNESS CAMERA IN LAGOON COVE, for a side by side of how the two maps are RENDERED (not of their art):
        /// `compare_lagooncove_land.png` and `compare_lagooncove_sea.png` from 1.7 m on that map's court, wearing its own
        /// look the same way. The owner, 2026-10-12: "have you actually taken a look at how we made the other maps?"
        /// </summary>
        private static string Compare(string output)
        {
            const string other = LagoonCoveSceneBuilder.ScenePath;
            if (!File.Exists(other)) return "no Lagoon Cove scene to compare with";
            EditorSceneManager.OpenScene(other, OpenSceneMode.Single);
            var root = GameObject.Find("LagoonCove");
            if (root == null) return "the Lagoon Cove scene has no root to compare with";
            var camera = new GameObject("Payo review witness (Lagoon Cove)").AddComponent<Camera>();
            camera.enabled = false; camera.nearClipPlane = 0.1f; camera.farClipPlane = FreeFar;
            camera.depthTextureMode |= DepthTextureMode.Depth;
            camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            camera.gameObject.AddComponent<WorldOutline>().PrototypeEnabled = true;
            WorldLookPresentation worn = null;
            try
            {
                var sun = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);
                camera.gameObject.AddComponent<WorldLookCamera>();
                worn = WorldLookPresentation.InstallPreview(root.transform, 0f, sun);
                camera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(95f, 16f / 9f);
                var eye = new Vector3(0f, 1.7f, 0f);
                var spawn = GameObject.Find("Spawn0");
                if (spawn != null) eye = spawn.transform.position + Vector3.up * 1.6f;
                camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(new Vector3(0.3f, 0.05f, -1f)));
                Render(camera, Path.Combine(output, "compare_lagooncove_land.png"));
                camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(new Vector3(-0.2f, 0.03f, 1f)));
                Render(camera, Path.Combine(output, "compare_lagooncove_sea.png"));
            }
            finally
            {
                if (worn != null) Object.DestroyImmediate(worn.gameObject);
                Object.DestroyImmediate(camera.gameObject);
            }
            return "and compare_lagooncove_land, compare_lagooncove_sea by the same witness camera";
        }

        /// <summary>One layout stood up in the editor: its solids, colliders and features on, every other's off. Nothing is saved.</summary>
        /// <summary>What a camera has in view: renderers (active, enabled) whose bounds meet its frustum, their distinct materials and triangles.</summary>
        private static string InView(Camera camera, string name)
        {
            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            int renderers = 0; long triangles = 0;
            var materials = new HashSet<Material>();
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy || !GeometryUtility.TestPlanesAABB(planes, r.bounds)) continue;
                Mesh mesh = r is SkinnedMeshRenderer skinned ? skinned.sharedMesh : r.GetComponent<MeshFilter>() != null ? r.GetComponent<MeshFilter>().sharedMesh : null;
                if (mesh == null) continue;
                renderers++;
                for (int sub = 0; sub < mesh.subMeshCount; sub++) triangles += mesh.GetIndexCount(sub) / 3;
                foreach (var m in r.sharedMaterials) if (m != null) materials.Add(m);
            }
            return $"  {name}: {renderers} renderers, {materials.Count} materials, {triangles} triangles";
        }

        /// <summary>
        /// THE ART UNDER THE FEET, MEASURED. For every block standing in a layout: nine points over its model's footprint,
        /// the model's top there (a ray down a mesh collider lent to the model for the moment) against the top of the
        /// layout's own colliders. One line a block: the worst gap and where. A stair placed with the wrong turn shows as
        /// metres at its ends; a block a centimetre under its collision reads -0.01.
        /// </summary>
        private static string Cover(ArenaStage stage, int layout)
        {
            var said = new System.Text.StringBuilder();
            said.AppendLine("layout " + layout + " (" + stage.Layouts[layout].Name + "):");
            var colliders = stage.Layouts[layout].Colliders != null ? stage.Layouts[layout].Colliders.transform : null;
            if (colliders == null) { said.AppendLine("  no colliders"); return said.ToString(); }
            Physics.SyncTransforms();
            int blocks = 0, off = 0;
            foreach (var piece in stage.Pieces)
            {
                if (piece == null || piece.Solids == null || layout >= piece.Solids.Length || piece.Solids[layout] == null) continue;
                var model = piece.Solids[layout].transform.Find("Model");
                if (model == null) continue;
                var filters = model.GetComponentsInChildren<MeshFilter>(false).Where(f => f.sharedMesh != null && f.GetComponent<Renderer>() != null).ToList();
                if (filters.Count == 0) continue;
                Bounds b = filters[0].GetComponent<Renderer>().bounds;
                foreach (var f in filters) b.Encapsulate(f.GetComponent<Renderer>().bounds);
                var lent = new List<MeshCollider>();
                foreach (var f in filters) { var c = f.gameObject.AddComponent<MeshCollider>(); c.sharedMesh = f.sharedMesh; lent.Add(c); }
                float worst = 0f; Vector3 where = b.center; int read = 0;
                for (int ix = -1; ix <= 1; ix++)
                    for (int iz = -1; iz <= 1; iz++)
                    {
                        var from = new Vector3(b.center.x + ix * b.extents.x * 0.7f, b.max.y + 30f, b.center.z + iz * b.extents.z * 0.7f);
                        float solid = float.NegativeInfinity, art = float.NegativeInfinity;
                        foreach (var hit in Physics.RaycastAll(from, Vector3.down, 120f, ~0, QueryTriggerInteraction.Ignore))
                            if (hit.collider.transform.IsChildOf(colliders) && hit.point.y > solid) solid = hit.point.y;
                        foreach (var c in lent)
                            if (c.Raycast(new Ray(from, Vector3.down), out var hit, 120f) && hit.point.y > art) art = hit.point.y;
                        if (float.IsNegativeInfinity(solid) || float.IsNegativeInfinity(art)) continue;
                        read++;
                        if (Mathf.Abs(art - solid) > Mathf.Abs(worst)) { worst = art - solid; where = from; }
                    }
                foreach (var c in lent) Object.DestroyImmediate(c);
                blocks++;
                bool bad = Mathf.Abs(worst) > 0.2f || read == 0;
                if (bad) off++;
                said.AppendLine($"  {(bad ? "OFF " : "ok  ")}{piece.Solids[layout].name}: worst {worst:+0.00;-0.00} m at ({where.x:0.0}, {where.z:0.0}), {read} of 9 points read, model turned {model.localEulerAngles.y:0}");
            }
            said.AppendLine($"  {blocks} blocks measured, {off} off by more than 0.2 m");
            return said.ToString();
        }

        private static void Show(ArenaStage stage, int layout)
        {
            foreach (var piece in stage.Pieces)
            {
                if (piece == null || piece.Solids == null) continue;
                for (int l = 0; l < piece.Solids.Length; l++)
                    if (piece.Solids[l] != null) { piece.Solids[l].SetActive(l == layout); piece.Solids[l].transform.localPosition = Vector3.zero; }
            }
            for (int l = 0; l < stage.LayoutCount; l++)
            {
                if (stage.Layouts[l].Colliders != null) stage.Layouts[l].Colliders.SetActive(l == layout);
                if (stage.Layouts[l].Features != null) stage.Layouts[l].Features.SetActive(l == layout);
            }
        }

        private static void Render(Camera camera, string path)
        {
            var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 4 };
            rt.Create(); camera.targetTexture = rt; camera.Render();
            var previous = RenderTexture.active;
            var display = RenderTexture.GetTemporary(1600, 900, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            bool write = GL.sRGBWrite; GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
            Graphics.Blit(rt, display); GL.sRGBWrite = write; RenderTexture.active = display;
            var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            RenderTexture.active = previous; camera.targetTexture = null;
            RenderTexture.ReleaseTemporary(display); rt.Release();
            Object.DestroyImmediate(rt); Object.DestroyImmediate(image);
        }

        /// <summary>Runs a request's words (not `probe`, which the watcher hands to the play probe). Throws on anything it cannot do.</summary>
        internal static string Run(string request)
        {
            var words = request.ToLowerInvariant().Split(new[] { ' ', '\t', '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries);
            bool build = words.Contains("build"), review = words.Contains("review");
            if (!build && !review) throw new InvalidOperationException("unknown request '" + request.Trim() + "' (write build, review, probe, or several of them)");
            var said = new List<string>();
            var warnings = new List<string>();
            if (build) { said.Add(Build()); warnings.AddRange(Warnings); }
            if (review) { Warnings.Clear(); said.Add(Review(NextReviewFolder())); warnings.AddRange(Warnings.Where(w => !warnings.Contains(w))); }
            string result = string.Join("; ", said);
            if (warnings.Count > 0) result += "\n" + warnings.Count + " warning(s):\n- " + string.Join("\n- ", warnings);
            return result;
        }
    }

    /// <summary>
    /// HOW A SHELL DRIVES THE OPEN EDITOR for the Payo map: write `build`, `review`, `probe` (or several; a number after
    /// `probe` is its cap in seconds, 30 to 300) into `Temp/payo.request`. About once a second, when the editor is not
    /// in Play and not compiling or importing, this takes the request, refreshes the asset database, runs it and
    /// writes `Temp/payo.done`: `OK what was done` or `FAIL why`. The other maps' watchers' rules, to the letter:
    ///  * if any open scene has unsaved changes NOTHING is run and the answer is `FAIL scene dirty`;
    ///  * the scenes that were open are reopened afterwards;
    ///  * a refresh that starts a compile leaves the request for after the reload;
    ///  * it never fights the Eskinita or Paete's run watchers: while one of their requests waits or one of their
    ///    probes runs, this one waits its turn.
    /// ⚠️ Unity empties `Temp` when it starts: a request written before the editor is up is lost.
    /// </summary>
    [InitializeOnLoad]
    internal static class PayoRequestWatcher
    {
        private const string Request = "Temp/payo.request", Done = "Temp/payo.done";
        private static readonly string[] Others = { "Temp/eskinita-alley.request", "Temp/paete-run.request" };
        private static double _nextPoll;

        static PayoRequestWatcher() { EditorApplication.update += Poll; }

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < _nextPoll) return;
            _nextPoll = EditorApplication.timeSinceStartup + 1.0;
            if (!File.Exists(Request)) return;
            if (PayoPlayProbe.Busy || PaeteRunPlayProbe.Busy || EskinitaAlleyPlayProbe.Busy || Others.Any(File.Exists)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;

            string result;
            try
            {
                AssetDatabase.Refresh();
                if (EditorApplication.isCompiling) return;
                string text = File.ReadAllText(Request);
                File.Delete(Request);
                if (File.Exists(Done)) File.Delete(Done);
                var words = text.ToLowerInvariant().Split(new[] { ' ', '\t', '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Contains("probe"))
                {
                    // `probe arena` checks the Arena's own scene; `rounds=N` sets the pinned match's length (6 unless said).
                    bool arena = words.Contains("arena");
                    int rounds = 6;
                    foreach (string w in words) if (w.StartsWith("rounds=", StringComparison.Ordinal) && int.TryParse(w.Substring(7), out int asked)) rounds = asked;
                    var rest = words.Where(w => w != "probe" && w != "arena" && !w.StartsWith("rounds=", StringComparison.Ordinal) && !int.TryParse(w, out _)).ToArray();
                    int budget = words.Select(w => int.TryParse(w, out int n) ? n : 0).FirstOrDefault(n => n > 0);
                    string before = rest.Length > 0 ? Guarded(string.Join(" ", rest)) : "";
                    PayoPlayProbe.Begin(before, budget > 0 ? budget : PayoPlayProbe.DefaultBudget, arena ? "arena" : "", rounds);
                    return;
                }
                result = "OK " + Guarded(text);
            }
            catch (Exception e)
            {
                try { if (File.Exists(Request)) File.Delete(Request); } catch { /* answered below either way */ }
                result = "FAIL " + e;
            }
            try { File.WriteAllText(Done, result); }
            catch (Exception e) { Debug.LogError("[Payo] could not write " + Done + ": " + e.Message); }
            if (result.StartsWith("OK")) Debug.Log("[Payo] request: " + result);
            else Debug.LogError("[Payo] request: " + result);
        }

        private static string Guarded(string text)
        {
            var open = new List<string>();
            string active = SceneManager.GetActiveScene().path;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isDirty) throw new InvalidOperationException("scene dirty (" + (string.IsNullOrEmpty(scene.name) ? "Untitled" : scene.name) + " has unsaved changes; nothing was run)");
                if (scene.isLoaded && !string.IsNullOrEmpty(scene.path)) open.Add(scene.path);
            }
            try { return PayoSceneBuilder.Run(text); }
            finally { Restore(open, active); }
        }

        internal static void Restore(List<string> open, string active)
        {
            try
            {
                open = open.Where(File.Exists).ToList();
                if (open.Count == 0) { EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single); return; }
                EditorSceneManager.OpenScene(open[0], OpenSceneMode.Single);
                for (int i = 1; i < open.Count; i++) EditorSceneManager.OpenScene(open[i], OpenSceneMode.Additive);
                var wanted = SceneManager.GetSceneByPath(active);
                if (wanted.IsValid() && wanted.isLoaded) SceneManager.SetActiveScene(wanted);
            }
            catch (Exception e) { Debug.LogWarning("[Payo] could not reopen the scenes that were open: " + e.Message); }
        }
    }
}
