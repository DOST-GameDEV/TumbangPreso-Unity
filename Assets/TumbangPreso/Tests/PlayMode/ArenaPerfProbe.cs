using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Map;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// WHAT THE ARENA COSTS TO DRAW, MEASURED IN REAL PLAY (docs/ARENA_MAP_BRIEF.md: "a
    /// performance probe from the first build, not after"; the pattern is IlalimPerfProbe). One
    /// batch run enters Play on the Arena scene and writes Logs/arena/unity/perf.txt and a picture
    /// of every view (Logs/arena/unity/perf/*.png):
    ///   * the whole scene: renderers, materials, triangles by group, lights, who casts a shadow;
    ///   * from a player's eye by the can, looking out along each of EIGHT BEARINGS, through the
    ///     game camera itself (its lens, its far plane, its whole effect chain): triangles,
    ///     vertices, batches, draw calls, set-pass calls, shadow casters;
    ///   * the same counts from THE BREAK CAMERA at the start of its orbit, the widest view of
    ///     the stage the game ever takes.
    /// COUNTS are the measure (the same on every machine); the milliseconds are batch-mode CPU
    /// time of Camera.Render on this PC, not a player's GPU.
    ///
    /// WHAT TO READ IT AGAINST. The art is about 301,000 triangles (tools/export_arena_unity.py's
    /// budget), one stage layout 10,000 to 16,000, the crowd about 38,000 as drawn from the stage.
    /// A view cannot hold all of it: the three ring-shaped meshes are cut into sectors so the
    /// part behind the player is culled. So a view well over 250,000 triangles means the culling
    /// is not working (a ring that was not cut, static batching off). Shadow casters should be
    /// the stage's pieces and the bodies and nothing else: a stand or a tower in that column is
    /// `ArenaArtPlacer.Dress` not having been applied. The art has 53 materials, so a set-pass
    /// count far past 100 means the materials are not shared (an importer's copies still worn).
    /// </summary>
    [Category("WallClock")]
    public sealed class ArenaPerfProbe
    {
        private const string Scene = "Assets/TumbangPreso/Scenes/Maps/Arena.unity";
        private const string Folder = "Logs/arena/unity";
        private const int Width = 1920, Height = 1080, Renders = 4;
        private const float Eye = 1.6f;

        private static readonly string[] Compass = { "n", "ne", "e", "se", "s", "sw", "w", "nw" };

        private static readonly string[] Counters =
        {
            "Triangles Count", "Vertices Count", "Batches Count", "Draw Calls Count", "SetPass Calls Count", "Shadow Casters Count",
        };

        [UnityTest, Timeout(600000)]
        public IEnumerator CountWhatEachViewOfTheArenaDraws()
        {
#if UNITY_EDITOR
            LogAssert.ignoreFailingMessages = true;
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            Time.timeScale = 1f;
            float waited = 0f;
            while (waited < 40f && (Camera.main == null || ArenaStage.Instance == null || Object.FindObjectsByType<CharacterMotor>().Length < 2)) { waited += Time.unscaledDeltaTime; yield return null; }
            for (float t = 0f; t < 6f; t += Time.unscaledDeltaTime) yield return null;
            // ARENA-INTRO: the match's opening hides the stage until it is built; this measures the map standing whole.
            ArenaIntro.Stop();
            yield return null; yield return null;

            var cam = Camera.main;
            var stage = ArenaStage.Instance;
            Assert.IsNotNull(cam, "No main camera in Play on " + Scene);
            Assert.IsNotNull(stage, "No ArenaStage in Play on " + Scene);
            string shots = Folder + "/perf";
            Directory.CreateDirectory(shots);

            // The default tier, whatever the profile saved; put back at the end.
            int savedTier = Settings.SettingsStore.Current.GraphicsQuality;
            Settings.SettingsStore.Current.GraphicsQuality = Settings.GraphicsProfiles.Default;
            Settings.GraphicsProfiles.Apply(Settings.GraphicsProfiles.Default);

            Time.timeScale = 0f;
            yield return null; yield return null;

            var report = new StringBuilder();
            report.AppendLine($"ARENA PERFORMANCE PROBE, {Scene}");
            report.AppendLine("COUNTS are the measure (the same on every machine). The milliseconds are batch-mode CPU time of Camera.Render on this PC, not a player's GPU.");
            var outline = cam.GetComponent<Visual.WorldOutline>();
            var range = Object.FindFirstObjectByType<Visual.MapCameraRange>();
            report.AppendLine($"camera {cam.name}: fov {CameraSystem.CameraRig.FppFieldOfView:F0} vertical (the first-person lens; the rig's was {cam.fieldOfView:F0} when measured), near {cam.nearClipPlane}, far {cam.farClipPlane} " +
                              $"(MapCameraRange: {(range != null ? $"play {range.PlayFar}, free {range.FreeFar}, ink {range.InkFadeStart}..{range.InkFadeEnd}" : "ABSENT: the city is clipped at the rig's own far plane")}), " +
                              $"depth mode {cam.depthTextureMode}, path {cam.actualRenderingPath}, WorldOutline {(outline != null ? "enabled " + outline.enabled : "absent")}, " +
                              $"effects [{string.Join(", ", cam.GetComponents<MonoBehaviour>().Where(m => m != null && m.enabled).Select(m => m.GetType().Name))}]");
            report.AppendLine($"quality: tier {Settings.GraphicsProfiles.Of(Settings.GraphicsProfiles.Current).Label}, shadows {QualitySettings.shadows} {QualitySettings.shadowResolution}, " +
                              $"distance {QualitySettings.shadowDistance} m, cascades {QualitySettings.shadowCascades}, pixel lights {QualitySettings.pixelLightCount}, " +
                              $"lodBias {QualitySettings.lodBias}, msaa {QualitySettings.antiAliasing}, fog {RenderSettings.fogStartDistance}..{RenderSettings.fogEndDistance} m, " +
                              $"sky {(RenderSettings.skybox != null ? RenderSettings.skybox.shader.name : "none")}, device {SystemInfo.graphicsDeviceType}, {SystemInfo.graphicsDeviceName}");
            report.AppendLine($"stage: layout '{(stage.Applied >= 0 ? stage.Layouts[stage.Applied].Name : "none")}', can at {stage.CanHeight} m, radius {stage.Radius} m");
            report.AppendLine();
            WholeScene(report);
            report.AppendLine();

            var recorders = Counters.Select(c => ProfilerRecorder.StartNew(ProfilerCategory.Render, c)).ToArray();
            yield return null; yield return null;
            var savedPos = cam.transform.position; var savedRot = cam.transform.rotation; float savedFov = cam.fieldOfView;
            float eye = stage.CanHeight + Eye;
            report.AppendLine($"PER VIEW ({Width}x{Height}, mean of {Renders} renders)");
            report.AppendLine($"{"view",-22} {"triangles",10} {"vertices",10} {"batched",8} {"draws",8} {"setpass",8} {"casters",8} {"ms",7}   (UnityStats: tris draws setpass casters)");
            long sumTris = 0, sumBatches = 0, sumSetPass = 0, sumCasters = 0; int measured = 0;

            // Nine views: eight from the can through the game camera, one through the break camera.
            var breakCamera = Object.FindFirstObjectByType<ArenaBreakCamera>(FindObjectsInactive.Include);
            for (int view = 0; view < Compass.Length + 1; view++)
            {
                bool isBreak = view == Compass.Length;
                if (isBreak && breakCamera == null) { report.AppendLine("break_camera           (no ArenaBreakCamera in the scene)"); break; }

                var eyeCam = isBreak ? breakCamera.GetComponent<Camera>() : cam;
                string name;
                Vector3 at, look;
                if (isBreak)
                {
                    // Where `ArenaBreakCamera` stands as a break begins: the start of its orbit.
                    name = "break_camera";
                    float scale = breakCamera.AuthoredRadius > 0f ? Mathf.Max(1f, stage.Radius / breakCamera.AuthoredRadius) : 1f;
                    float yaw = breakCamera.StartYaw * Mathf.Deg2Rad;
                    at = new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw)) * (breakCamera.OrbitRadiusFrom * scale) + Vector3.up * (breakCamera.OrbitHeightFrom * scale);
                    look = Vector3.up * 0.5f;
                }
                else
                {
                    name = "can_" + Compass[view];
                    Vector3 away = ArenaStageMesh.Direction(45f * view);
                    at = -away * 2f + Vector3.up * eye;
                    look = away * 120f + Vector3.up * 28f;
                }

                Vector3 keptPos = eyeCam.transform.position; Quaternion keptRot = eyeCam.transform.rotation; float keptFov = eyeCam.fieldOfView;
                var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                rt.Create();
                // A first render nobody counts: shaders, culling and uploads settle in it.
                Pose(eyeCam, at, look, isBreak);
                eyeCam.targetTexture = rt; eyeCam.Render(); eyeCam.targetTexture = null;
                yield return null;
                yield return null;          // the frame the floor is read from: no render of ours in it
                var floor = recorders.Select(r => r.LastValue).ToArray();
                Pose(eyeCam, at, look, isBreak);
                eyeCam.targetTexture = rt;
                var clock = Stopwatch.StartNew();
                for (int k = 0; k < Renders; k++) eyeCam.Render();
                double ms = clock.Elapsed.TotalMilliseconds / Renders;
                eyeCam.targetTexture = null;
                // The editor's own statistics for the frame so far, as a second witness, and for the draw calls and the batches.
                string stats = $"{UnityEditor.UnityStats.triangles} {UnityEditor.UnityStats.drawCalls} {UnityEditor.UnityStats.setPassCalls} {UnityEditor.UnityStats.shadowCasters}";
                long batches = UnityEditor.UnityStats.staticBatches + UnityEditor.UnityStats.dynamicBatches + UnityEditor.UnityStats.instancedBatches;
                long draws = UnityEditor.UnityStats.drawCalls, statsTris = UnityEditor.UnityStats.triangles;
                var previous = RenderTexture.active; RenderTexture.active = rt;
                var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); image.Apply();
                RenderTexture.active = previous;
                File.WriteAllBytes($"{shots}/{name}.png", image.EncodeToPNG());
                Object.Destroy(image);
                yield return null;
                var got = new long[Counters.Length];
                for (int c = 0; c < Counters.Length; c++) got[c] = Math.Max(0, recorders[c].LastValue - floor[c]) / Renders;
                // ⚠️ The profiler's batch and draw-call counters read 0 in this editor (IlalimPerfProbe);
                // the editor's own statistics carry them.
                if (got[2] == 0) got[2] = batches;
                if (got[3] == 0) got[3] = draws;
                if (got[0] == 0) got[0] = statsTris;
                rt.Release(); Object.Destroy(rt);
                eyeCam.transform.SetPositionAndRotation(keptPos, keptRot); eyeCam.fieldOfView = keptFov;
                report.AppendLine($"{name,-22} {got[0],10} {got[1],10} {got[2],8} {got[3],8} {got[4],8} {got[5],8} {ms,7:F1}   ({stats})");
                if (!isBreak) { sumTris += got[0]; sumBatches += got[2]; sumSetPass += got[4]; sumCasters += got[5]; measured++; }
            }

            if (measured > 0)
                report.AppendLine($"{"MEAN of the can's views",-22} {sumTris / measured,9} {"",10} {sumBatches / measured,8} {"",8} {sumSetPass / measured,8} {sumCasters / measured,8}");
            foreach (var r in recorders) r.Dispose();
            cam.transform.SetPositionAndRotation(savedPos, savedRot); cam.fieldOfView = savedFov;
            Settings.SettingsStore.Current.GraphicsQuality = savedTier;
            Settings.GraphicsProfiles.Apply(savedTier);
            Time.timeScale = 1f;
            File.WriteAllText($"{Folder}/perf.txt", report.ToString());
            Debug.Log("[ArenaPerfProbe]\n" + report);
            Assert.Greater(sumTris, 0, "The render counters read nothing: no view was measured.\n" + report);
#else
            Assert.Ignore("Editor only: loads the scene by path and reads the editor's render statistics.");
            yield break;
#endif
        }

        private static void Pose(Camera cam, Vector3 at, Vector3 look, bool keepLens)
        {
            cam.transform.SetPositionAndRotation(at, Quaternion.LookRotation(look - at));
            if (!keepLens) cam.fieldOfView = CameraSystem.CameraRig.FppFieldOfView;
        }

#if UNITY_EDITOR
        /// <summary>What is in the scene, whatever a camera sees of it: by the top group each
        /// renderer stands under (the map root's children and the `Dressing` group's).</summary>
        private static void WholeScene(StringBuilder report)
        {
            var renderers = Object.FindObjectsByType<Renderer>();
            var groups = new System.Collections.Generic.SortedDictionary<string, (int renderers, long triangles, int casters)>();
            var materials = new System.Collections.Generic.HashSet<Material>();
            var shaders = new System.Collections.Generic.SortedDictionary<string, int>();
            long total = 0; int casters = 0;
            foreach (var r in renderers)
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy) continue;

                long tris = 0;
                Mesh mesh = r is SkinnedMeshRenderer skinned ? skinned.sharedMesh : r.GetComponent<MeshFilter>() != null ? r.GetComponent<MeshFilter>().sharedMesh : null;
                if (mesh != null) for (int k = 0; k < mesh.subMeshCount; k++) tris += (long)mesh.GetIndexCount(k) / 3;
                bool casts = r.shadowCastingMode != ShadowCastingMode.Off;
                string group = GroupOf(r.transform);
                groups.TryGetValue(group, out var g);
                groups[group] = (g.renderers + 1, g.triangles + tris, g.casters + (casts ? 1 : 0));
                total += tris; if (casts) casters++;
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null || !materials.Add(m)) continue;
                    string shader = m.shader != null ? m.shader.name : "(none)";
                    shaders.TryGetValue(shader, out int n); shaders[shader] = n + 1;
                }
            }

            var lights = Object.FindObjectsByType<Light>();
            report.AppendLine($"WHOLE SCENE: {renderers.Length} renderers, {total} triangles, {materials.Count} materials, {casters} renderers casting a shadow, " +
                              $"{lights.Length} lights ({lights.Count(l => l.shadows != LightShadows.None)} with shadows, {lights.Count(l => l.renderMode == LightRenderMode.ForceVertex)} per vertex)");
            report.AppendLine($"{"group",-28} {"renderers",9} {"triangles",10} {"casters",8}");
            foreach (var kv in groups.OrderByDescending(k => k.Value.triangles))
                report.AppendLine($"{kv.Key,-28} {kv.Value.renderers,9} {kv.Value.triangles,10} {kv.Value.casters,8}");
            report.AppendLine("materials by shader: " + string.Join(", ", shaders.Select(kv => $"{kv.Key} {kv.Value}")));
        }

        private static string GroupOf(Transform t)
        {
            // Root > group, or root > Dressing > group; anything outside the map's root is the game's own.
            var chain = new System.Collections.Generic.List<Transform>();
            for (; t != null; t = t.parent) chain.Add(t);
            chain.Reverse();
            if (chain.Count == 0 || chain[0].name != "Arena") return "(outside the map: " + (chain.Count > 0 ? chain[0].name : "?") + ")";
            if (chain.Count < 2) return "Arena";
            if (chain[1].name == "Dressing" && chain.Count > 2) return "Dressing/" + chain[2].name;
            return chain[1].name;
        }
#endif
    }
}
