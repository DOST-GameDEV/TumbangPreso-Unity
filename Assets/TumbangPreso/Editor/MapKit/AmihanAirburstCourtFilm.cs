using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// AIRBURST ON A REAL MAP (`AmihanAirburstLook.BuildBench`, the call a match makes), as `AmihanKitFilm` films the kit: the map
    /// opened as a PREVIEW scene, its own daylight, no bloom, 60 pictures a second, real hero models. One picture is four views
    /// in a square: the caster's eyes, a thrown player's eyes, the side, above.
    ///
    /// ⚠️ STAGED, AND SAID SO. No match runs outside Play: bodies are still poses moved by the rule's own numbers, and the sample
    /// is stepped on the film's clock. It proves the picture and where it lies, not the cast.
    ///
    /// Request: lines in `Temp/amihan-airburst-court.request`: `&lt;map scene name&gt; &lt;tag&gt; [lean]`. `lean` films the phone and Low tier
    /// version (the dome's LOD 100, nothing bent). Frames: `Logs/paete-ability-film/airburstcourt_&lt;map&gt;_&lt;tag&gt;_frames/`, then
    /// `done_airburstcourt_&lt;map&gt;_&lt;tag&gt;.txt` with the floor's measured heights and the cost, or `failed_...`.
    /// </summary>
    [InitializeOnLoad]
    internal static class AmihanAirburstCourtFilm
    {
        private const string Request = "Temp/amihan-airburst-court.request", OutDir = "Logs/paete-ability-film";
        private const int CellWidth = 960, CellHeight = 540, Rate = 60;
        private static double _next;
        private static UnityEngine.Rendering.CommandBuffer _clockOpaque, _clockAlpha;

        static AmihanAirburstCourtFilm() { EditorApplication.update += Poll; }

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 1.0;
            if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            AssetDatabase.Refresh();
            if (EditorApplication.isCompiling) return;
            string[] lines = File.ReadAllLines(Request);
            File.Delete(Request);
            foreach (string line in lines)
            {
                string[] words = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length >= 2) Run(words[0], words[1], words.Length > 2 && words[2] == "lean");
            }
        }

        private struct View
        {
            public string Name; public Func<Vector3> Eye, Aim; public float Fov, Near; public GameObject Hide;
            public View(string name, Func<Vector3> eye, Func<Vector3> aim, float fov, GameObject hide, float near) { Name = name; Eye = eye; Aim = aim; Fov = fov; Hide = hide; Near = near; }
        }

        // As `AmihanKitFilm.SetTime`: the film's clock reaches `_Time` through command buffers inside the render.
        private static void SetTime(float t)
        {
            float T = 100f + t;
            var time = new Vector4(T / 20f, T, T * 2f, T * 3f);
            var sin = new Vector4(Mathf.Sin(T / 8f), Mathf.Sin(T / 4f), Mathf.Sin(T / 2f), Mathf.Sin(T));
            var cos = new Vector4(Mathf.Cos(T / 8f), Mathf.Cos(T / 4f), Mathf.Cos(T / 2f), Mathf.Cos(T));
            var delta = new Vector4(1f / Rate, Rate, 1f / Rate, Rate);
            Shader.SetGlobalVector("_Time", time);
            foreach (var cb in new[] { _clockOpaque, _clockAlpha })
            {
                if (cb == null) continue;
                cb.Clear();
                cb.SetGlobalVector("_Time", time); cb.SetGlobalVector("_SinTime", sin);
                cb.SetGlobalVector("_CosTime", cos); cb.SetGlobalVector("unity_DeltaTime", delta);
            }
        }

        private static GameObject Person(Scene scene, List<GameObject> roots, System.Text.StringBuilder report, string id, Vector3 at, float yaw)
        {
            var holder = new GameObject("~AirburstCourt-" + id);
            SceneManager.MoveGameObjectToScene(holder, scene); roots.Add(holder);
            holder.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
            var art = RosterBook.Load()?.FindPersonArt(id);
            if (art == null || art.Model == null) { report.AppendLine("  " + id + "'s model was not found"); return holder; }
            var model = (GameObject)Object.Instantiate(art.Model, holder.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(0f, CharacterVisual.PersonModelYaw, 0f);
            model.transform.localScale = Vector3.one * CharacterVisual.PersonScale;
            ToonSkin.Apply(model, ToonSkin.PersonOutlineWidth, art.Palette);
            foreach (var animator in model.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            return holder;
        }

        private static string Cost(GameObject root)
        {
            int draws = 0; long tris = 0; var mats = new HashSet<Material>();
            foreach (var r in root.GetComponentsInChildren<Renderer>(false))
            {
                var mesh = r.enabled ? r.GetComponent<MeshFilter>()?.sharedMesh : null;
                if (mesh == null || mesh.vertexCount == 0 || r.sharedMaterial == null) continue;
                draws++; tris += mesh.GetIndexCount(0) / 3; mats.Add(r.sharedMaterial);
            }
            return draws + " draws, " + tris + " triangles, " + mats.Count + " materials";
        }

        private static void Run(string map, string tag, bool lean)
        {
            Directory.CreateDirectory(OutDir);
            string name = "airburstcourt_" + map.ToLowerInvariant() + "_" + tag;
            string donePath = Path.Combine(OutDir, "done_" + name + ".txt"), failedPath = Path.Combine(OutDir, "failed_" + name + ".txt");
            if (File.Exists(donePath)) File.Delete(donePath);
            if (File.Exists(failedPath)) File.Delete(failedPath);
            var report = new System.Text.StringBuilder();
            var owned = new List<Object>(); var roots = new List<GameObject>();
            Scene scene = default;
            bool lit = false, keepBend = FxStudyAirburst.Bend;
            FxStudyAirburst fx = null;
            try
            {
                scene = EditorSceneManager.OpenPreviewScene("Assets/TumbangPreso/Scenes/Maps/" + map + ".unity");
                if (!scene.IsValid()) throw new InvalidOperationException("No map to film on.");
                VfxShapes.GroundScene = scene.GetPhysicsScene();
                lit = Unsupported.SetOverrideLightingSettings(scene);
                Shader.SetGlobalFloat("_CharacterSmoothShade", ToonSkin.CharacterSmoothShade);
                Shader.SetGlobalFloat("_FxStudyBloom", 0f);
                FxStudyAirburst.Bend = !lean;
                report.AppendLine("map: " + map + (lit ? ", lit by its own lighting settings" : ", its lighting could not be put in force") + "; no bloom; " + Rate + " pictures a second; " + (lean ? "LEAN: dome LOD 100, nothing bent" : "dome LOD 300, bending"));

                // Her spot: the map's own standing point nearest the middle of its walls.
                var boxes = new List<Bounds>();
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var node in root.GetComponentsInChildren<Transform>(true))
                        if (node.name == "Bounds") boxes.AddRange(node.GetComponentsInChildren<BoxCollider>().Select(c => c.bounds));
                MatchInstaller.MeasureWalls(boxes, out float x0, out float x1, out float z0, out float z1, out float ceiling);
                var middle = new Vector3((x0 + x1) * 0.5f, 0f, (z0 + z1) * 0.5f);
                var routes = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MapRoutes>(true)).FirstOrDefault();
                Vector3 spot = middle;
                if (routes != null && routes.Nodes.Length > 0) spot = routes.Nodes.OrderBy(n => (new Vector3(n.x, 0f, n.z) - middle).sqrMagnitude).First();
                Vector3 On(float x, float z, float y)
                {
                    return VfxShapes.TryGroundUnder(new Vector3(x, y + 2f, z), 4f, 12f, out float h) ? new Vector3(x, h, z) : new Vector3(x, y, z);
                }
                Vector3 S = On(spot.x, spot.z, spot.y);
                Vector3 fwd = Vector3.forward, right = Vector3.right, up = Vector3.up;
                report.AppendLine("  walls x " + x0.ToString("0.0") + " to " + x1.ToString("0.0") + ", z " + z0.ToString("0.0") + " to " + z1.ToString("0.0") + "; she stands at " + S.ToString("0.00"));

                // The real floor against the plane the sample lies on, by the game's ground rule, out to the rule's radius.
                float R = AmihanRules.StormSurgeAroundRadius;
                var heights = new System.Text.StringBuilder();
                int none = 0, count = 0;
                for (float r = 2.5f; r <= R + 0.01f; r += 2.5f)
                {
                    float lo = float.MaxValue, hi = float.MinValue;
                    for (int k = 0; k < 24; k++)
                    {
                        float a = k * Mathf.PI / 12f; count++;
                        if (VfxShapes.TryGroundUnder(S + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r, 2f, 8f, out float h)) { lo = Mathf.Min(lo, h - S.y); hi = Mathf.Max(hi, h - S.y); }
                        else none++;
                    }
                    heights.Append("  ").Append(r.ToString("0.0")).Append(" m: ").Append(lo.ToString("0.00")).Append(" to ").Append(hi.ToString("0.00"));
                }
                report.AppendLine("  floor against her feet, by ring:" + heights + "; " + none + " of " + count + " probes had no floor");

                var her = Person(scene, roots, report, "amihan", S, 0f);
                var front = Person(scene, roots, report, "dante", On(S.x + 1.5f, S.z + 6f, S.y), 190f);
                var back = Person(scene, roots, report, "cheska", On(S.x - 4f, S.z - 5f, S.y), 30f);
                Person(scene, roots, report, "zack", On(S.x, S.z - (R + 0.3f), S.y), 0f);
                var thrown = new[] { front, back };
                var starts = thrown.Select(g => g.transform.position).ToArray();
                float hold = AmihanRules.StormSurgeHoldSeconds;
                // As `AmihanKitFilm`: the cutscene left them part way into the throw, and it flies on by its own numbers.
                Vector3 Flown(int i, float t)
                {
                    Vector3 start = starts[i], flat = start - S; flat.y = 0f;
                    float at = Mathf.Clamp(AmihanStorm.CutsceneTail - flat.magnitude * AmihanStorm.CutsceneHitPerMetre + t, 0f, hold);
                    Vector3 dir = AmihanStorm.BlowDirection(S, fwd, start);
                    float rise = Mathf.Max(0f, AmihanRules.StormSurgeLift * at - 0.5f * Balance.CharacterGravity * at * at);
                    return start + dir * (AmihanRules.StormSurgeSpeed * at) + up * rise;
                }

                var holder = new GameObject("~AirburstCourt-look");
                SceneManager.MoveGameObjectToScene(holder, scene); roots.Add(holder);
                var clock = System.Diagnostics.Stopwatch.StartNew();
                fx = AmihanAirburstLook.BuildBench(holder.transform, S + up * 0.9f, fwd);
                report.AppendLine("  built in " + clock.Elapsed.TotalMilliseconds.ToString("0.0") + " ms on this machine, once a cast; lies at y " + holder.transform.position.y.ToString("0.00"));

                var camGo = new GameObject("~AirburstCourtCamera");
                SceneManager.MoveGameObjectToScene(camGo, scene);
                var cam = camGo.AddComponent<Camera>();
                cam.scene = scene; cam.enabled = false; cam.allowHDR = false;
                cam.farClipPlane = 400f;
                cam.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.55f, 0.70f, 0.88f);
                var plain = new RenderTexture(CellWidth, CellHeight, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                var cell = new Texture2D(CellWidth, CellHeight, TextureFormat.RGB24, false);
                owned.Add(plain); owned.Add(cell); owned.Add(camGo);
                _clockOpaque = new UnityEngine.Rendering.CommandBuffer { name = "film clock (opaque)" };
                _clockAlpha = new UnityEngine.Rendering.CommandBuffer { name = "film clock (transparent)" };
                cam.AddCommandBuffer(UnityEngine.Rendering.CameraEvent.BeforeForwardOpaque, _clockOpaque);
                cam.AddCommandBuffer(UnityEngine.Rendering.CameraEvent.BeforeForwardAlpha, _clockAlpha);

                var views = new[]
                {
                    new View("caster's eyes", () => S - fwd * 0.2f + up * 1.45f, () => S + fwd * 8f + up * 0.2f, 75f, her, 0.03f),
                    new View("thrown player's eyes", () => front.transform.position + up * 1.3f, () => S + up * 0.8f, 75f, front, 0.03f),
                    new View("side", () => S + right * 7.6f + up * 4.5f - fwd * 2f, () => S + fwd * 1f, 80f, null, 0.5f),
                    new View("above", () => S + up * 22f, () => S, 62f, null, 8f),
                };

                string frames = Path.Combine(OutDir, name + "_frames");
                if (Directory.Exists(frames)) foreach (string old in Directory.GetFiles(frames)) File.Delete(old);
                Directory.CreateDirectory(frames);
                var sheet = new Texture2D(CellWidth * 2, CellHeight * 2, TextureFormat.RGB24, false);
                owned.Add(sheet);
                var times = new System.Text.StringBuilder("views, reading order: " + string.Join("|", views.Select(v => v.Name)) + "\n");
                int total = Mathf.RoundToInt((fx.Seconds + 0.25f) * Rate);
                double sampleMs = 0d; int samples = 0;
                for (int f = 0; f < total; f++)
                {
                    float t = -0.15f + f / (float)Rate;
                    for (int i = 0; i < thrown.Length; i++) thrown[i].transform.position = Flown(i, t);
                    for (int v = 0; v < views.Length; v++)
                    {
                        SetTime(t);
                        Vector3 eye = views[v].Eye();
                        clock.Restart();
                        fx.Sample(Mathf.Max(0f, t), eye);
                        sampleMs += clock.Elapsed.TotalMilliseconds; samples++;
                        bool hid = views[v].Hide != null;
                        if (hid) views[v].Hide.SetActive(false);
                        cam.fieldOfView = views[v].Fov; cam.nearClipPlane = views[v].Near;
                        var dir = views[v].Aim() - eye;
                        cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(dir, Mathf.Abs(dir.normalized.y) > 0.95f ? Vector3.forward : Vector3.up));
                        cam.targetTexture = plain;
                        cam.Render();
                        var keep = RenderTexture.active; RenderTexture.active = plain;
                        cell.ReadPixels(new Rect(0, 0, CellWidth, CellHeight), 0, 0); cell.Apply();
                        RenderTexture.active = keep;
                        if (hid) views[v].Hide.SetActive(true);
                        sheet.SetPixels((v % 2) * CellWidth, (1 - v / 2) * CellHeight, CellWidth, CellHeight, cell.GetPixels());
                    }
                    sheet.Apply();
                    File.WriteAllBytes(Path.Combine(frames, "f" + f.ToString("0000") + ".png"), sheet.EncodeToPNG());
                    times.AppendLine(t.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture));
                    if (Mathf.Abs(t - 0.15f) < 0.5f / Rate || Mathf.Abs(t - 0.6f) < 0.5f / Rate) report.AppendLine("  cost at " + t.ToString("0.00") + " s: " + Cost(holder));
                }
                File.WriteAllText(Path.Combine(frames, "times.txt"), times.ToString());
                report.AppendLine("  posing it: " + (sampleMs / Mathf.Max(1, samples)).ToString("0.000") + " ms a frame on this machine (editor, flecks' mesh included)");
                report.AppendLine("  " + total + " frames in " + frames.Replace('\\', '/'));
                File.WriteAllText(donePath, report.ToString());
            }
            catch (Exception failure)
            {
                File.WriteAllText(failedPath, report + "\n" + failure);
                Debug.LogError("[AmihanAirburstCourtFilm] " + failure);
            }
            finally
            {
                fx?.Dispose();
                foreach (var go in roots) if (go != null) Object.DestroyImmediate(go);
                FxStudyAirburst.Bend = keepBend;
                var dome = Shader.Find(FxStudyAirburst.DomeShader);
                if (dome != null) dome.maximumLOD = 300;
                VfxShapes.GroundScene = null;
                _clockOpaque?.Release(); _clockAlpha?.Release(); _clockOpaque = _clockAlpha = null;
                if (lit) Unsupported.RestoreOverrideLightingSettings();
                RenderTexture.active = null;
                foreach (var thing in owned)
                {
                    if (thing == null) continue;
                    if (thing is RenderTexture texture) texture.Release();
                    Object.DestroyImmediate(thing);
                }
                if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
