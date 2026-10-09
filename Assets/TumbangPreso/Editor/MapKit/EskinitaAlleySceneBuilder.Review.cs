using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>The review renders of <see cref="EskinitaAlleySceneBuilder"/>.</summary>
    public static partial class EskinitaAlleySceneBuilder
    {
        private static string NextReviewFolder()
        {
            // Versioned every time: chat clients cache images by filename.
            int v = 1; while (Directory.Exists("Logs/eskinita/unity/v" + v)) v++; return "Logs/eskinita/unity/v" + v;
        }

        /// <summary>
        /// Opens the built scene and renders each shot of the layout's "review" list (an aerial
        /// and a plan if it has none) at 1600 x 900 to `folder/eskinita_NAME.png`. The witness
        /// camera wears what a match camera wears: the scene's grade, the world outline, and the
        /// map's `WorldLookProfile` look installed the way the map preview installs it (the key
        /// light, the coloured shade, the haze and the blocky clouds). A shot's `fov` is
        /// HORIZONTAL, degrees. Returns one line saying what was written.
        /// </summary>
        public static string Review(string output)
        {
            if (!File.Exists(ScenePath)) throw new FileNotFoundException("The scene is not built yet: " + ScenePath);
            var layout = ReadLayout(out _);
            var can = new Vector3(layout.gameplay.can[0], layout.gameplay.can[1], layout.gameplay.can[2]);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Directory.CreateDirectory(output);

            var shots = new List<(string name, Vector3 at, Vector3 look, float fov)>();
            foreach (var s in layout.review ?? new ShotSpec[0])
            {
                if (s == null || s.at == null || s.at.Length < 3 || s.look == null || s.look.Length < 3) { Warn("a review shot has no 'at' or no 'look'"); continue; }
                string name = string.IsNullOrEmpty(s.name) ? "shot" + shots.Count : s.name;
                shots.Add((name, new Vector3(s.at[0], s.at[1], s.at[2]), new Vector3(s.look[0], s.look[1], s.look[2]), s.fov > 0 ? s.fov : 95f));
            }
            if (shots.Count == 0)
            {
                float hx = layout.gameplay.half_x, hz = layout.gameplay.half_z;
                shots.Add(("aerial", can + new Vector3(hx * 1.7f, 30f, -hz * 1.9f), can + Vector3.up * 3f, 55f));
                shots.Add(("plan", can + new Vector3(0f, 48f, -0.01f), can, 60f));
            }

            var camera = new GameObject("Eskinita Alley review witness").AddComponent<Camera>();
            camera.enabled = false; camera.nearClipPlane = 0.05f; camera.farClipPlane = 900;
            camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            camera.gameObject.AddComponent<WorldOutline>().PrototypeEnabled = true;
            WorldLookPresentation look = null;
            int written = 0;
            try
            {
                var sun = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);
                var sceneRoot = GameObject.Find(MapName);
                try
                {
                    camera.gameObject.AddComponent<WorldLookCamera>();
                    if (sceneRoot != null) look = WorldLookPresentation.InstallPreview(sceneRoot.transform, can.y, sun);
                    if (look == null) Warn("the review wears NO world look (no 'EskinitaAlley' row found): no blocky clouds, the scene's own light only");
                }
                catch (Exception e) { Warn("the world look preview failed: " + e.Message); }

                foreach (var s in shots)
                {
                    var forward = s.look - s.at;
                    if (forward.sqrMagnitude < 1e-6f) { Warn($"review shot '{s.name}' looks at its own position"); continue; }
                    // Straight down has no horizon to hold level: the top of the frame is +Z.
                    var up = Mathf.Abs(Vector3.Dot(forward.normalized, Vector3.up)) > 0.999f ? Vector3.forward : Vector3.up;
                    camera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(s.fov, 16f / 9f);
                    camera.transform.SetPositionAndRotation(s.at, Quaternion.LookRotation(forward, up));
                    var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 4 };
                    rt.Create(); camera.targetTexture = rt; camera.Render();
                    var previous = RenderTexture.active;
                    var display = RenderTexture.GetTemporary(1600, 900, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                    bool write = GL.sRGBWrite; GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
                    Graphics.Blit(rt, display); GL.sRGBWrite = write; RenderTexture.active = display;
                    var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                    image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
                    File.WriteAllBytes(Path.Combine(output, "eskinita_" + s.name + ".png"), image.EncodeToPNG());
                    RenderTexture.active = previous; camera.targetTexture = null;
                    RenderTexture.ReleaseTemporary(display); rt.Release();
                    Object.DestroyImmediate(rt); Object.DestroyImmediate(image);
                    written++;
                }
            }
            finally
            {
                if (look != null) Object.DestroyImmediate(look.gameObject);
                Object.DestroyImmediate(camera.gameObject);
            }
            string summary = $"{written} review renders in {output}";
            Debug.Log(Tag + summary);
            return summary;
        }

        /// <summary>Runs a request's words. Used by the watcher; throws on anything it cannot do.</summary>
        internal static string Run(string request)
        {
            var words = request.ToLowerInvariant().Split(new[] { ' ', '\t', '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries);
            bool build = words.Contains("build"), review = words.Contains("review");
            if (!build && !review) throw new InvalidOperationException("unknown request '" + request.Trim() + "' (write build, review, probe, or several of them)");
            Warnings.Clear();
            var said = new List<string>();
            var warnings = new List<string>();
            if (build) { said.Add(Build()); warnings.AddRange(Warnings); }
            if (review) { said.Add(Review(NextReviewFolder())); warnings.AddRange(Warnings.Where(w => !warnings.Contains(w))); }
            string result = string.Join("; ", said);
            if (warnings.Count > 0) result += "\n" + warnings.Count + " warning(s):\n- " + string.Join("\n- ", warnings);
            return result;
        }
    }

    /// <summary>
    /// HOW A SHELL DRIVES THE OPEN EDITOR. Write `build`, `review` or `build review` (and
    /// `probe`, the play smoke probe: see <see cref="EskinitaAlleyPlayProbe"/>) into
    /// `Temp/eskinita-alley.request` (project root). About once a second, when the editor is not
    /// in Play and not compiling or importing, this takes the request, refreshes the asset
    /// database (so new .glb, .png and .json files are imported first), runs it, and writes
    /// `Temp/eskinita-alley.done`: `OK what was done` (then any warnings, one per line) or
    /// `FAIL the exception`. The same text goes to the Console.
    ///
    /// ⚠️ IF THE REFRESH STARTS A SCRIPT COMPILE the request is left where it is and is taken
    /// after the reload, so it always runs on the code that is on disk.
    ///
    /// ⚠️ IT GIVES THE EDITOR BACK AS IT FOUND IT. A build or a review replaces the open scene,
    /// so the open scenes are remembered and reopened afterwards. If any open scene has unsaved
    /// changes NOTHING is run and the answer is `FAIL scene dirty`: the owner's unsaved work is
    /// never discarded and never saved for him.
    ///
    /// A stale `.done` is deleted when a request is taken, so a `.done` that exists is always
    /// the answer to the latest request.
    /// </summary>
    [InitializeOnLoad]
    internal static class EskinitaAlleyRequestWatcher
    {
        private const string Request = "Temp/eskinita-alley.request", Done = "Temp/eskinita-alley.done";
        private static double _nextPoll;

        static EskinitaAlleyRequestWatcher() { EditorApplication.update += Poll; }

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < _nextPoll) return;
            _nextPoll = EditorApplication.timeSinceStartup + 1.0;
            if (!File.Exists(Request)) return;
            if (EskinitaAlleyPlayProbe.Busy) return;   // a probe is in Play or handing the editor back: the request waits
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;

            string result;
            try
            {
                // An editor in the background does not look for changed files by itself.
                AssetDatabase.Refresh();
                if (EditorApplication.isCompiling) return;   // new code on disk: the request waits for the reload
                string text = File.ReadAllText(Request);
                File.Delete(Request);
                if (File.Exists(Done)) File.Delete(Done);
                // `probe` (see EskinitaAlleyPlayProbe): whatever else was asked runs first, then the
                // probe takes the editor into Play and writes the answer itself when it is back out.
                var words = text.ToLowerInvariant().Split(new[] { ' ', '\t', '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Contains("probe"))
                {
                    var rest = words.Where(w => w != "probe" && !int.TryParse(w, out _)).ToArray();
                    int budget = words.Select(w => int.TryParse(w, out int n) ? n : 0).FirstOrDefault(n => n > 0);
                    string before = rest.Length > 0 ? Guarded(string.Join(" ", rest)) : "";
                    EskinitaAlleyPlayProbe.Begin(before, budget > 0 ? budget : EskinitaAlleyPlayProbe.DefaultBudget);
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
            catch (Exception e) { Debug.LogError("[EskinitaAlley] could not write " + Done + ": " + e.Message); }
            if (result.StartsWith("OK")) Debug.Log("[EskinitaAlley] request: " + result);
            else Debug.LogError("[EskinitaAlley] request: " + result);
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
            try { return EskinitaAlleySceneBuilder.Run(text); }
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
            catch (Exception e) { Debug.LogWarning("[EskinitaAlley] could not reopen the scenes that were open: " + e.Message); }
        }
    }
}
