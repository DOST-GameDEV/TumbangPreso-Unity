using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using TumbangPreso.Map;
using TumbangPreso.Visual;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// PAYO IN UNITY AGAINST THE APPROVED BLENDER SCENE (2026-10-18). The cameras of tools/payo_dress.py and
    /// tools/payo_land_blend.py, moved into Unity (Blender (x, y, z) is Unity (x, z, y); a Blender lens over its 36 mm
    /// sensor is the horizontal field of view), rendered by the review's witness camera in the Payo day look, and set
    /// beside the newest Blender render of the same camera (`Logs/payo/terrain/dressed_dNN_VIEW.png`) in one sheet.
    /// Then the court eye MEASURED: renderers, triangles and draw calls the LODs actually leave in view, the engine's
    /// own counters for one render, and the time of a timed render loop.
    ///
    /// BATCH: `Unity -batchmode -projectPath . -executeMethod TumbangPreso.EditorTools.MapKit.PayoSceneBuilder.BatchBuildCompare`
    /// builds the scene, renders and measures into `Logs/payo/unity/v1` (or the folder after `-payoOut`) and exits (code 1 on a failure).
    /// `BatchCompare` does the same without building.
    /// </summary>
    public static partial class PayoSceneBuilder
    {
        private const int CompareW = 1280, CompareH = 720;

        /// <summary>A Blender camera: where it is, what it looks at (Blender metres), its lens (mm, 36 mm sensor) or ortho width.</summary>
        private static readonly (string name, Vector3 at, Vector3 to, float lens, float ortho)[] BlenderCameras =
        {
            ("eye_court_looking_south", new Vector3(0, 12, 1.7f), new Vector3(0, -80, 10), 20, 0),
            ("over_the_court", new Vector3(0, -60, 34), new Vector3(0, 400, -70), 22, 0),
            ("terraces_close", new Vector3(-60, 150, -12), new Vector3(40, 330, -46), 24, 0),
            ("town_from_the_east", new Vector3(170, 70, 170), new Vector3(-20, -100, 20), 26, 0),
            ("corner_from_valley", new Vector3(-90, 110, 5), new Vector3(-30, 25, -20), 24, 0),
            ("valley_to_court", new Vector3(0, 260, 40), new Vector3(0, 0, -10), 28, 0),
            ("top_court", new Vector3(0, -40, 600), new Vector3(0, -40, 0), 0, 440),
        };

        /// <summary>Blender (x east, y north, z up) to Unity (x east, y up, z north): checked on the head fall (Blender (0, -208), Unity z -208).</summary>
        internal static Vector3 FromBlender(Vector3 b) => new Vector3(b.x, b.z, b.y);

        public static void BatchBuildCompare() => Batch(true);
        public static void BatchCompare() => Batch(false);

        private static void Batch(bool build)
        {
            int code = 0;
            try
            {
                if (build) Debug.Log(Tag + Build());
                var args = Environment.GetCommandLineArgs();
                int at = Array.IndexOf(args, "-payoOut");
                Debug.Log(Tag + BlenderCompare(at >= 0 && at + 1 < args.Length ? args[at + 1] : "Logs/payo/unity/v1"));
                if (Warnings.Count > 0) Debug.Log(Tag + Warnings.Count + " warning(s):\n- " + string.Join("\n- ", Warnings));
            }
            catch (Exception e) { Debug.LogException(e); code = 1; }
            EditorApplication.Exit(code);
        }

        public static string BlenderCompare(string output)
        {
            if (!File.Exists(ScenePath)) throw new FileNotFoundException("The scene is not built yet: " + ScenePath);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Directory.CreateDirectory(output);
            var sceneRoot = GameObject.Find(MapName);
            var stage = Object.FindFirstObjectByType<ArenaStage>();
            var night = Object.FindFirstObjectByType<PayoNight>();
            if (sceneRoot == null || stage == null) throw new InvalidOperationException("The built scene has no '" + MapName + "' root or no stage");
            Show(stage, 0);
            if (night != null) night.Apply(0f);
            var blades = Object.FindFirstObjectByType<PayoBlades>();
            if (blades != null) blades.Push();

            var camera = new GameObject("Payo compare witness").AddComponent<Camera>();
            camera.enabled = false; camera.nearClipPlane = 0.3f; camera.farClipPlane = FreeFar;
            camera.depthTextureMode |= DepthTextureMode.Depth;
            camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            var outline = camera.gameObject.AddComponent<WorldOutline>();
            outline.PrototypeEnabled = true;
            outline.SetFade(InkFadeStart, InkFadeEnd);
            WorldLookPresentation worn = null;
            var report = new StringBuilder();
            var pairs = new List<(string name, string unity, string blender)>();
            try
            {
                var sun = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);
                try
                {
                    camera.gameObject.AddComponent<WorldLookCamera>();
                    worn = WorldLookPresentation.InstallPreview(sceneRoot.transform, 0f, sun);
                    if (worn == null) Warn("the compare wears NO world look (no '" + MapName + "' row found)");
                }
                catch (Exception e) { Warn("the world look preview failed: " + e.Message); }

                foreach (var view in BlenderCameras)
                {
                    Pose(camera, view.at, view.to, view.lens, view.ortho);
                    string unity = Path.Combine(output, "unity_" + view.name + ".png");
                    Render(camera, unity, CompareW, CompareH);
                    pairs.Add((view.name, unity, NewestBlender(view.name)));
                    report.AppendLine(Measure(camera, view.name, false));
                }
                camera.orthographic = false;

                // THE COURT EYE, measured: the player's eye over the can looking north over the valley (the busiest view),
                // and the four other ways.
                report.AppendLine();
                report.AppendLine("THE COURT EYE (1.7 m over the can, 60 degrees vertical, 16:9), LOD-aware counts (each LODGroup's LOD for this camera, renderers whose bounds meet the frustum):");
                foreach (var (name, look) in new[] { ("north", new Vector3(0, -0.05f, 1)), ("east", new Vector3(1, -0.05f, 0)), ("south", new Vector3(0, 0.05f, -1)), ("west", new Vector3(-1, -0.05f, 0)), ("northeast", new Vector3(1, -0.05f, 1)) })
                {
                    camera.orthographic = false;
                    camera.fieldOfView = 60f;
                    camera.aspect = 16f / 9f;
                    camera.transform.SetPositionAndRotation(new Vector3(0f, 1.7f, 0f), Quaternion.LookRotation(look, Vector3.up));
                    report.AppendLine(Measure(camera, "court_eye_" + name, true));
                }
                report.AppendLine();
                report.AppendLine(Timed(camera));
            }
            finally
            {
                if (worn != null) Object.DestroyImmediate(worn.gameObject);
                Object.DestroyImmediate(camera.gameObject);
            }
            string sheet = Path.Combine(output, "compare.png");
            Sheet(pairs, sheet);
            File.WriteAllText(Path.Combine(output, "measure.txt"), report.ToString());
            string summary = $"{pairs.Count} views rendered against Blender into {output}; sheet {sheet}; measurements in measure.txt:\n{report}";
            return summary;
        }

        private static void Pose(Camera camera, Vector3 at, Vector3 to, float lens, float ortho)
        {
            camera.aspect = (float)CompareW / CompareH;
            Vector3 eye = FromBlender(at), target = FromBlender(to);
            if (ortho > 0f)
            {
                // Blender's default camera turn looks down -Z with +Y up the picture: north is up.
                camera.orthographic = true;
                camera.orthographicSize = ortho * 0.5f * CompareH / CompareW;
                camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(Vector3.down, Vector3.forward));
                return;
            }
            camera.orthographic = false;
            float horizontal = 2f * Mathf.Atan(18f / lens) * Mathf.Rad2Deg;     // sensor 36 mm wide, fit to the wider side
            camera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(horizontal, camera.aspect);
            camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(target - eye, Vector3.up));
        }

        /// <summary>The newest Blender render of a view (dressed_d53 first, then older passes), or null.</summary>
        private static string NewestBlender(string view)
        {
            for (int d = 60; d >= 30; d--)
            {
                string path = $"Logs/payo/terrain/dressed_d{d}_{view}.png";
                if (File.Exists(path)) return path;
            }
            return null;
        }

        private static void Render(Camera camera, string path, int w, int h)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 4 };
            rt.Create(); camera.targetTexture = rt; camera.Render();
            var previous = RenderTexture.active;
            var display = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            bool write = GL.sRGBWrite; GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
            Graphics.Blit(rt, display); GL.sRGBWrite = write; RenderTexture.active = display;
            var image = new Texture2D(w, h, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, w, h), 0, 0); image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            RenderTexture.active = previous; camera.targetTexture = null;
            RenderTexture.ReleaseTemporary(display); rt.Release();
            Object.DestroyImmediate(rt); Object.DestroyImmediate(image);
        }

        /// <summary>
        /// What a camera draws, counted from the scene: each LODGroup's current LOD for this camera (Unity's rule: the group's
        /// world size over its distance, against 2 tan(fov / 2), times lodBias), renderers enabled and active whose bounds
        /// meet the frustum. Draw calls are ESTIMATED: a renderer submesh each, except that renderers sharing one mesh,
        /// submesh and instanced material are one call per 500 (GPU instancing); shadow passes are not counted.
        /// </summary>
        private static string Measure(Camera camera, string name, bool detail)
        {
            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            var hidden = new HashSet<Renderer>();
            foreach (var group in Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None))
            {
                if (!group.enabled || !group.gameObject.activeInHierarchy) continue;
                var lods = group.GetLODs();
                Vector3 s = group.transform.lossyScale;
                float size = group.size * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
                Vector3 centre = group.transform.TransformPoint(group.localReferencePoint);
                float relative = camera.orthographic ? size / (2f * camera.orthographicSize)
                    : size / (2f * Vector3.Distance(camera.transform.position, centre) * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad));
                relative *= QualitySettings.lodBias;
                int chosen = lods.Length;
                for (int i = 0; i < lods.Length; i++) if (relative >= lods[i].screenRelativeTransitionHeight) { chosen = i; break; }
                for (int i = 0; i < lods.Length; i++)
                    if (i != chosen) foreach (var r in lods[i].renderers) if (r != null) hidden.Add(r);
            }
            int renderers = 0; long triangles = 0; int calls = 0;
            var instanced = new Dictionary<(Mesh, int, Material), int>();
            var byGroup = new Dictionary<string, long>();
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy || hidden.Contains(r) || !GeometryUtility.TestPlanesAABB(planes, r.bounds)) continue;
                if (r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer) continue;
                Mesh mesh = r is SkinnedMeshRenderer skinned ? skinned.sharedMesh : r.GetComponent<MeshFilter>() != null ? r.GetComponent<MeshFilter>().sharedMesh : null;
                if (mesh == null) continue;
                renderers++;
                long t = 0;
                var mats = r.sharedMaterials;
                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    t += mesh.GetIndexCount(sub) / 3;
                    var m = sub < mats.Length ? mats[sub] : null;
                    if (m != null && m.enableInstancing && !GameObjectUtility.GetStaticEditorFlags(r.gameObject).HasFlag(StaticEditorFlags.BatchingStatic))
                    {
                        var key = (mesh, sub, m);
                        instanced.TryGetValue(key, out int n); instanced[key] = n + 1;
                    }
                    else calls++;
                }
                triangles += t;
                string top = Top(r.transform);
                byGroup.TryGetValue(top, out long g); byGroup[top] = g + t;
            }
            foreach (var n in instanced.Values) calls += (n + 499) / 500;
            var line = new StringBuilder($"  {name}: {renderers} renderers, {triangles:N0} triangles, about {calls} draw calls (before shadows)");
            if (detail)
                line.Append("\n      by part: " + string.Join(", ", byGroup.OrderByDescending(p => p.Value).Take(12).Select(p => $"{p.Key} {p.Value:N0}")));
            return line.ToString();
        }

        /// <summary>The part of the map a renderer belongs to, for the counts: the child of Dressing it stands under (by model), or the root's child.</summary>
        private static string Top(Transform t)
        {
            Transform last = t, before = null;
            for (; t != null; t = t.parent)
            {
                if (t.parent != null && t.parent.name == "Dressing")
                {
                    string n = t.name;
                    if (n.StartsWith("payo_land")) return "land";
                    if (n.StartsWith("tree_") || n.StartsWith("payo_plant_palm")) return "trees";
                    if (n.StartsWith("plant_") || n.StartsWith("cover_") || n.StartsWith("payo_plant")) return "flora";
                    return "dressing";
                }
                before = last; last = t;
            }
            return before != null ? before.name : last.name;
        }

        /// <summary>A timed render loop at the court eye: the engine's counters for one render (where it fills them in edit
        /// mode), then 60 renders to a 1920 x 1080 target, each waited for by a one-pixel read back.</summary>
        private static string Timed(Camera camera)
        {
            camera.orthographic = false; camera.fieldOfView = 60f; camera.aspect = 16f / 9f;
            camera.transform.SetPositionAndRotation(new Vector3(0f, 1.7f, 0f), Quaternion.LookRotation(new Vector3(0, -0.05f, 1), Vector3.up));
            var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 4 };
            rt.Create(); camera.targetTexture = rt;
            var probe = new Texture2D(1, 1, TextureFormat.RGBAHalf, false);
            var said = new StringBuilder();
            void Wait() { var prev = RenderTexture.active; RenderTexture.active = rt; probe.ReadPixels(new Rect(0, 0, 1, 1), 0, 0); probe.Apply(); RenderTexture.active = prev; }
            using (var tris = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count"))
            using (var batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count"))
            using (var draws = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count"))
            using (var setpass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count"))
            {
                for (int i = 0; i < 3; i++) { camera.Render(); Wait(); }
                said.AppendLine($"ENGINE COUNTERS after a court-eye render (edit mode; may read 0 where the editor does not fill them outside a player frame): " +
                                $"ProfilerRecorder triangles {tris.LastValue:N0}, batches {batches.LastValue:N0}, draw calls {draws.LastValue:N0}, set-pass {setpass.LastValue:N0}; " +
                                $"UnityStats triangles {UnityStats.triangles:N0}, draw calls {UnityStats.drawCalls:N0}, set-pass {UnityStats.setPassCalls:N0}, shadow casters {UnityStats.shadowCasters:N0}");
            }
            var times = new List<double>();
            var clock = new Stopwatch();
            for (int i = 0; i < 60; i++)
            {
                clock.Restart(); camera.Render(); Wait(); clock.Stop();
                times.Add(clock.Elapsed.TotalMilliseconds);
            }
            times.Sort();
            said.Append($"TIMED RENDER LOOP at the court eye looking north, 1920 x 1080, MSAA 4, shadows and the outline on, 60 renders each waited for (CPU culling and submission plus GPU, in the editor, {SystemInfo.graphicsDeviceName}, {SystemInfo.graphicsDeviceType}): " +
                        $"median {times[30]:0.0} ms, 10th {times[6]:0.0} ms, 90th {times[54]:0.0} ms, worst {times[59]:0.0} ms");
            camera.targetTexture = null; rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(probe);
            return said.ToString();
        }

        /// <summary>The sheet: per view a row, Blender on the left and Unity on the right, each at half size, labelled by file.</summary>
        private static void Sheet(List<(string name, string unity, string blender)> pairs, string path)
        {
            const int w = CompareW / 2, h = CompareH / 2, gap = 6;
            var sheet = new Texture2D(w * 2 + gap * 3, (h + gap) * pairs.Count + gap, TextureFormat.RGB24, false);
            var fill = new Color32[sheet.width * sheet.height];
            for (int i = 0; i < fill.Length; i++) fill[i] = new Color32(24, 24, 24, 255);
            sheet.SetPixels32(fill);
            for (int row = 0; row < pairs.Count; row++)
            {
                int y = sheet.height - (row + 1) * (h + gap);
                Blit(sheet, pairs[row].blender, gap, y, w, h);
                Blit(sheet, pairs[row].unity, gap * 2 + w, y, w, h);
            }
            sheet.Apply();
            File.WriteAllBytes(path, sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
            File.WriteAllText(Path.ChangeExtension(path, ".txt"), "compare.png rows, top to bottom (left Blender, right Unity):\n" +
                string.Join("\n", pairs.Select(p => $"{p.name}: {(p.blender ?? "NO BLENDER RENDER")} | {p.unity}")));
        }

        private static void Blit(Texture2D sheet, string file, int x, int y, int w, int h)
        {
            if (file == null || !File.Exists(file)) return;
            var source = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (!source.LoadImage(File.ReadAllBytes(file))) { Object.DestroyImmediate(source); return; }
            var pixels = new Color32[w * h];
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                    pixels[j * w + i] = source.GetPixelBilinear((i + 0.5f) / w, (j + 0.5f) / h);
            sheet.SetPixels32(x, y, w, h, pixels);
            Object.DestroyImmediate(source);
        }
    }
}
