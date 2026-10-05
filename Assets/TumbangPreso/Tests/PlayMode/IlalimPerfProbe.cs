using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
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
    /// WHAT ILALIM COSTS TO DRAW, COUNTED (owner, 2026-10-04: the game is "primarily not laggy for
    /// the server host but it is for players joining too", then "add these optimization fixes").
    /// The map is about 6.4 million triangles and the players who join are on weaker PCs, so every
    /// optimisation of it is measured here before and after. This enters Play on Ilalim, waits for
    /// the match's bodies, freezes the clock, puts the traffic on a fixed seed and a fixed age (so
    /// two runs draw the same street), and renders the GAME'S OWN CAMERA (its whole effect chain,
    /// as `CharacterAoProbe` does) at 1920x1080 from a fixed list of viewpoints in the game frame
    /// (the can is the origin, Taft's centre line x = 23, the Padre Faura junction about z = 17.5).
    /// Per viewpoint it records triangles, vertices, batches, draw calls, set-pass calls and shadow
    /// casters, and the CPU time of `Camera.Render`, and for the scene: renderers, triangles,
    /// colliders by type, lights and materials. It writes Logs/ilalim-unity/perf_LABEL.txt and one
    /// PNG per viewpoint to Logs/ilalim-unity/perf_LABEL/, where LABEL is the one line in
    /// Logs/ilalim-unity/perf_label.txt (or "run").
    ///
    /// ⚠️ THE COUNTS ARE THE MEASURE, NOT THE MILLISECONDS. Batch mode on the development PC is
    /// not a joining player's GPU: the render times are printed for a rough before/after on one
    /// machine only. A count is the same on every machine.
    /// ⚠️ HOW A VIEW IS COUNTED: the render statistics are whole-frame numbers, so each view takes a
    /// frame with no render of ours (the floor: whatever else draws that frame), then a frame
    /// with `Renders` renders of ours; the view's numbers are the difference over `Renders`.
    /// The tier is forced to the default (Balanced) so a profile's saved setting cannot move them.
    /// After the views it also reports: whether the occlusion bake hides anything that shows
    /// (`OcclusionTruth`, the one thing it asserts besides having measured), how far the cheaper
    /// ambient occlusion of the tiers under High moves the picture (`AmbientOcclusionTiers`), and
    /// what a step of the live road and of the sidewalk people costs on this PC (`Simulation`).
    /// </summary>
    [Category("WallClock")]
    public sealed class IlalimPerfProbe
    {
        private const string Scene = "Assets/TumbangPreso/Scenes/Maps/IlalimNgTulay.unity";
        private const string Folder = "Logs/ilalim-unity";
        private const string OcclusionData = "Assets/TumbangPreso/Scenes/Maps/IlalimNgTulay/OcclusionCullingData.asset";
        private const int Width = 1920, Height = 1080, Renders = 4;
        // The game frame: Taft's centre line, its asphalt, the Padre Faura junction's centre.
        private const float RoadX = 23f, RoadY = -0.24f, JunctionZ = 17.5f, Eye = 1.6f;

        private static readonly (string name, Vector3 at, Vector3 look)[] Views =
        {
            ("spawn_to_can_and_bridge", new Vector3(0f, Eye, -9f), new Vector3(9f, 3f, 4f)),
            ("can_east_to_road", new Vector3(0f, Eye, 0f), new Vector3(RoadX, 2.5f, 0f)),
            ("lot_edge_north_to_junction", new Vector3(10.5f, Eye, 2f), new Vector3(RoadX - 4f, 2.5f, JunctionZ + 6f)),
            ("taft_north", new Vector3(RoadX, RoadY + Eye, 0f), new Vector3(RoadX, 3f, 80f)),
            ("taft_south", new Vector3(RoadX, RoadY + Eye, 0f), new Vector3(RoadX, 3f, -80f)),
            ("junction_west", new Vector3(RoadX, RoadY + Eye, JunctionZ), new Vector3(-60f, 3f, JunctionZ + 1.5f)),
            ("junction_east", new Vector3(RoadX, RoadY + Eye, JunctionZ), new Vector3(100f, 3f, JunctionZ - 4f)),
            ("jump_apex_over_deck", new Vector3(31.6f, 14f, -6.2f), new Vector3(0f, 2f, 2f)),
        };

        private static readonly string[] Counters =
        {
            "Triangles Count", "Vertices Count", "Batches Count", "Draw Calls Count", "SetPass Calls Count", "Shadow Casters Count",
        };

        [UnityTest, Timeout(600000)]
        public IEnumerator CountWhatEachViewOfIlalimDraws()
        {
#if UNITY_EDITOR
            LogAssert.ignoreFailingMessages = true;
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            Time.timeScale = 1f;
            float waited = 0f;
            while (waited < 40f && (Camera.main == null || Object.FindObjectsByType<CharacterMotor>().Length < 2)) { waited += Time.unscaledDeltaTime; yield return null; }
            for (float t = 0f; t < 6f; t += Time.unscaledDeltaTime) yield return null;

            var cam = Camera.main;
            Assert.IsNotNull(cam, "No main camera in Play on " + Scene);
            string label = "run";
            try { if (File.Exists(Folder + "/perf_label.txt")) label = File.ReadAllText(Folder + "/perf_label.txt").Trim(); } catch (IOException) { }
            if (string.IsNullOrEmpty(label)) label = "run";
            string shots = $"{Folder}/perf_{label}";
            Directory.CreateDirectory(shots);

            // The default tier, whatever the profile saved; put back at the end.
            int savedTier = Settings.SettingsStore.Current.GraphicsQuality;
            Settings.SettingsStore.Current.GraphicsQuality = Settings.GraphicsProfiles.Default;
            Settings.GraphicsProfiles.Apply(Settings.GraphicsProfiles.Default);

            Time.timeScale = 0f;
            yield return null;
            string street = FixTheStreet();
            yield return null; yield return null;

            var report = new StringBuilder();
            report.AppendLine($"ILALIM PERFORMANCE PROBE, label '{label}', {Scene}");
            report.AppendLine("COUNTS are the measure (the same on every machine). The milliseconds are batch-mode CPU time of Camera.Render on this PC, not a player's GPU.");
            var outline = cam.GetComponent<Visual.WorldOutline>();
            // The play lens: a few seconds into a match the rig may still be on an arrival shot's.
            report.AppendLine($"camera {cam.name}: fov {CameraSystem.CameraRig.FppFieldOfView:F0} vertical (the first-person lens; the rig's was {cam.fieldOfView:F0} when measured), near {cam.nearClipPlane}, far {cam.farClipPlane}, occlusion culling {(cam.useOcclusionCulling ? "on" : "off")}, " +
                              $"depth mode {cam.depthTextureMode}, path {cam.actualRenderingPath}, WorldOutline {(outline != null ? "enabled " + outline.enabled : "absent")}, " +
                              $"effects [{string.Join(", ", cam.GetComponents<MonoBehaviour>().Where(m => m != null && m.enabled).Select(m => m.GetType().Name))}]");
            report.AppendLine($"quality: tier {Settings.GraphicsProfiles.Of(Settings.GraphicsProfiles.Current).Label}, shadows {QualitySettings.shadows} {QualitySettings.shadowResolution}, " +
                              $"distance {QualitySettings.shadowDistance} m, cascades {QualitySettings.shadowCascades}, pixel lights {QualitySettings.pixelLightCount}, " +
                              $"lodBias {QualitySettings.lodBias}, maximumLODLevel {QualitySettings.maximumLODLevel}, msaa {QualitySettings.antiAliasing}, " +
                              $"fog {RenderSettings.fogStartDistance}..{RenderSettings.fogEndDistance} m, device {SystemInfo.graphicsDeviceType}, {SystemInfo.graphicsDeviceName}");
            report.AppendLine($"occlusion data beside the scene: {(File.Exists(OcclusionData) ? new FileInfo(OcclusionData).Length + " bytes" : "none")}; street: {street}");
            report.AppendLine();
            WholeScene(report);
            report.AppendLine();

            var recorders = Counters.Select(c => ProfilerRecorder.StartNew(ProfilerCategory.Render, c)).ToArray();
            yield return null; yield return null;
            var savedPos = cam.transform.position; var savedRot = cam.transform.rotation; float savedFov = cam.fieldOfView;
            report.AppendLine($"PER VIEW ({Width}x{Height}, the game camera's own lens and effect chain, mean of {Renders} renders)");
            report.AppendLine($"{"view",-28} {"triangles",10} {"vertices",10} {"batched",8} {"draws",8} {"setpass",8} {"casters",8} {"ms",7}   (UnityStats: tris draws setpass casters)");
            long sumTris = 0, sumBatches = 0, sumSetPass = 0, sumCasters = 0; int measured = 0;
            foreach (var v in Views)
            {
                var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                rt.Create();
                // A first render nobody counts: shaders, LOD choices and uploads settle in it.
                Pose(cam, v.at, v.look);
                cam.fieldOfView = CameraSystem.CameraRig.FppFieldOfView;
                cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
                yield return null;
                yield return null;          // the frame the floor is read from: no render of ours in it
                var floor = recorders.Select(r => r.LastValue).ToArray();
                Pose(cam, v.at, v.look);
                cam.fieldOfView = CameraSystem.CameraRig.FppFieldOfView;
                cam.targetTexture = rt;
                var clock = Stopwatch.StartNew();
                for (int k = 0; k < Renders; k++) cam.Render();
                double ms = clock.Elapsed.TotalMilliseconds / Renders;
                cam.targetTexture = null;
                // The editor's own statistics for the frame so far, as a second witness, and for the draw calls and the batches (static, dynamic and instanced; the "batched" column).
                string stats = $"{UnityEditor.UnityStats.triangles} {UnityEditor.UnityStats.drawCalls} {UnityEditor.UnityStats.setPassCalls} {UnityEditor.UnityStats.shadowCasters}";
                long batches = UnityEditor.UnityStats.staticBatches + UnityEditor.UnityStats.dynamicBatches + UnityEditor.UnityStats.instancedBatches;
                long draws = UnityEditor.UnityStats.drawCalls, statsTris = UnityEditor.UnityStats.triangles;
                var previous = RenderTexture.active; RenderTexture.active = rt;
                var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); image.Apply();
                RenderTexture.active = previous;
                File.WriteAllBytes($"{shots}/{v.name}.png", image.EncodeToPNG());
                Object.Destroy(image);
                yield return null;
                var got = new long[Counters.Length];
                for (int c = 0; c < Counters.Length; c++) got[c] = Math.Max(0, recorders[c].LastValue - floor[c]) / Renders;
                // ⚠️ The profiler's batch and draw-call counters read 0 in this editor; the editor's own
                // statistics carry them (and agree with the profiler on triangles, set-pass calls and casters).
                if (got[2] == 0) got[2] = batches;
                if (got[3] == 0) got[3] = draws;
                if (got[0] == 0) got[0] = statsTris;
                rt.Release(); Object.Destroy(rt);
                report.AppendLine($"{v.name,-28} {got[0],10} {got[1],10} {got[2],8} {got[3],8} {got[4],8} {got[5],8} {ms,7:F1}   ({stats})");
                sumTris += got[0]; sumBatches += got[2]; sumSetPass += got[4]; sumCasters += got[5]; measured++;
            }
            report.AppendLine($"{"MEAN",-28} {sumTris / measured,10} {"",10} {sumBatches / measured,8} {"",8} {sumSetPass / measured,8} {sumCasters / measured,8}");
            foreach (var r in recorders) r.Dispose();
            int culledWrongly = 0;
            if (File.Exists(OcclusionData))
            {
                report.AppendLine();
                culledWrongly = OcclusionTruth(cam, report, shots);
            }
            report.AppendLine();
            AmbientOcclusionTiers(cam, report, shots);
            cam.transform.SetPositionAndRotation(savedPos, savedRot); cam.fieldOfView = savedFov;
            report.AppendLine();
            Simulation(report);
            Settings.SettingsStore.Current.GraphicsQuality = savedTier;
            Settings.GraphicsProfiles.Apply(savedTier);
            Time.timeScale = 1f;
            File.WriteAllText($"{Folder}/perf_{label}.txt", report.ToString());
            Debug.Log("[IlalimPerfProbe]\n" + report);
            Assert.Greater(sumTris, 0, "The render counters read nothing: no view was measured.\n" + report);
            Assert.AreEqual(0, culledWrongly, "Occlusion culling removed something the camera can see (see the occlusion section):\n" + report);
#else
            Assert.Ignore("Editor only: loads the scene by path and reads the editor's render statistics.");
            yield break;
#endif
        }

        private static void Pose(Camera cam, Vector3 at, Vector3 look)
            => cam.transform.SetPositionAndRotation(at, Quaternion.LookRotation(look - at));

#if UNITY_EDITOR
        /// <summary>The live road draws a different street every run (offline its seed is a new
        /// Guid). For a count that compares run to run it is put on one seed and one age, through
        /// the traffic's own `Reseed` and its own catch-up, so the vehicles stand where its
        /// arithmetic puts them 20 s into that street.</summary>
        private static string FixTheStreet()
        {
            var traffic = Object.FindFirstObjectByType<KantoTraffic>();
            if (traffic == null) return "no KantoTraffic";
            const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var reseed = typeof(KantoTraffic).GetMethod("Reseed", F);
            var time = typeof(KantoTraffic).GetField("_lockTime", F);
            if (reseed == null || time == null) return "KantoTraffic's Reseed or _lockTime not found: the street is this run's own";
            reseed.Invoke(traffic, new object[] { 20261004 });
            time.SetValue(traffic, 20.0);
            return $"{traffic.DriverCount} vehicles, seed 20261004, 20 s old";
        }

        /// <summary>
        /// ⚠️ DOES THE OCCLUSION BAKE HIDE ANYTHING THAT SHOWS? The one honest test is the picture:
        /// with the clock frozen, the same camera renders the same frame twice, occlusion culling
        /// on and off, and every pixel must agree. A pixel that differs is an object the bake
        /// culled although the camera can see it (a thin or see-through thing wrongly made an
        /// occluder, a cell too coarse). It is asked from every viewpoint above and from a grid
        /// over the whole play rectangle (every 5.5 m, eye height, four ways round), and from the
        /// air over the jump pads and the can. "floor" is the same comparison of two renders both
        /// culled, so a picture that is not repeatable by itself is not blamed on the bake.
        /// Returns the views where more than `Allowed` pixels differ.
        /// </summary>
        private static int OcclusionTruth(UnityEngine.Camera cam, StringBuilder report, string shots)
        {
            const int w = 960, h = 540, Allowed = 12;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            rt.Create();
            var image = new Texture2D(w, h, TextureFormat.RGB24, false);
            Color32[] Shoot(bool occlusion)
            {
                cam.useOcclusionCulling = occlusion;
                cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
                var previous = RenderTexture.active; RenderTexture.active = rt;
                image.ReadPixels(new Rect(0, 0, w, h), 0, 0); image.Apply();
                RenderTexture.active = previous;
                return image.GetPixels32();
            }
            int Differ(Color32[] a, Color32[] b, Color32[] mark)
            {
                int n = 0;
                for (int i = 0; i < a.Length; i++)
                {
                    int d = Mathf.Max(Mathf.Abs(a[i].r - b[i].r), Mathf.Max(Mathf.Abs(a[i].g - b[i].g), Mathf.Abs(a[i].b - b[i].b)));
                    if (d > 6) n++;
                    if (mark != null) { byte v = (byte)Mathf.Min(255, d * 8); mark[i] = new Color32(v, v, v, 255); }
                }
                return n;
            }
            var poses = new List<(string name, Vector3 at, Quaternion rot)>();
            foreach (var v in Views) poses.Add((v.name, v.at, Quaternion.LookRotation(v.look - v.at)));
            // The play rectangle (Editor/MapKit/IlalimFrame.cs), a metre inside its walls, standing on whatever is there.
            for (float x = -11f; x <= 33.01f; x += 5.5f)
                for (float z = -9.8f; z <= 24.01f; z += 5.63f)
                {
                    if (!Physics.Raycast(new Vector3(x, 6f, z), Vector3.down, out var hit, 8f, ~0, QueryTriggerInteraction.Ignore)) continue;
                    var eye = hit.point + Vector3.up * Eye;
                    for (int yaw = 0; yaw < 360; yaw += 90) poses.Add(($"grid_{x:F0}_{z:F0}_{yaw}", eye, Quaternion.Euler(4f, yaw, 0f)));
                }
            foreach (var over in new[] { new Vector3(31.6f, 0f, -6.2f), new Vector3(31.6f, 0f, 5.8f), new Vector3(14.4f, 0f, -2.2f), new Vector3(10f, 0f, 6.8f), new Vector3(0f, 0f, 0f), new Vector3(23f, 0f, 17.5f) })
                foreach (float height in new[] { 7f, 14f })
                    for (int yaw = 0; yaw < 360; yaw += 90) poses.Add(($"air_{over.x:F0}_{over.z:F0}_{height:F0}_{yaw}", new Vector3(over.x, height, over.z), Quaternion.Euler(25f, yaw, 0f)));

            bool saved = cam.useOcclusionCulling; float savedFov = cam.fieldOfView;
            cam.fieldOfView = CameraSystem.CameraRig.FppFieldOfView;
            var mark = new Color32[w * h];
            int bad = 0, worst = 0, floorWorst = 0; string worstName = "";
            var lines = new List<string>();
            foreach (var pose in poses)
            {
                cam.transform.SetPositionAndRotation(pose.at, pose.rot);
                var on = Shoot(true); var again = Shoot(true); var off = Shoot(false);
                int floor = Differ(on, again, null), differ = Differ(on, off, mark);
                floorWorst = Mathf.Max(floorWorst, floor);
                if (differ > worst) { worst = differ; worstName = pose.name; }
                if (differ <= Allowed + floor) continue;
                bad++;
                lines.Add($"  {pose.name,-30} at {pose.at} yaw {pose.rot.eulerAngles.y:F0}: {differ} pixels differ ({100f * differ / on.Length:F2}%), floor {floor}");
                if (bad > 24) continue;
                image.SetPixels32(off); image.Apply(); File.WriteAllBytes($"{shots}/occlusion_{pose.name}_off.png", image.EncodeToPNG());
                image.SetPixels32(on); image.Apply(); File.WriteAllBytes($"{shots}/occlusion_{pose.name}_on.png", image.EncodeToPNG());
                image.SetPixels32(mark); image.Apply(); File.WriteAllBytes($"{shots}/occlusion_{pose.name}_diff_x8.png", image.EncodeToPNG());
            }
            cam.useOcclusionCulling = saved; cam.fieldOfView = savedFov;
            Object.Destroy(image); rt.Release(); Object.Destroy(rt);
            report.AppendLine($"OCCLUSION TRUTH ({w}x{h}; the same frame with occlusion culling on and off, pixels differing by more than 6/255)");
            report.AppendLine($"{poses.Count} views: {bad} with more than {Allowed} differing pixels over their floor; the worst is {worst} pixels ({worstName}); the worst floor (on against on) is {floorWorst}.");
            foreach (var line in lines) report.AppendLine(line);
            return bad;
        }

        /// <summary>
        /// The same frame with the occlusion pass at sixteen probes a pixel (the High tier) and at
        /// eight (Balanced, the default, since 2026-10-04; `WorldOutline.AmbientOcclusionLiteKeyword`):
        /// how many pixels move and by how much, with both pictures and their difference times
        /// eight written for the first view. A GPU cost cannot be counted here; the pass's
        /// texture reads halve by construction.
        /// </summary>
        private static void AmbientOcclusionTiers(UnityEngine.Camera cam, StringBuilder report, string shots)
        {
            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            rt.Create();
            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            int saved = Visual.WorldOutline.AmbientOcclusionSamplesTest; float savedFov = cam.fieldOfView;
            cam.fieldOfView = CameraSystem.CameraRig.FppFieldOfView;
            Color32[] Shoot(int probes, string file)
            {
                Visual.WorldOutline.AmbientOcclusionSamplesTest = probes;
                cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
                var previous = RenderTexture.active; RenderTexture.active = rt;
                image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); image.Apply();
                RenderTexture.active = previous;
                if (file != null) File.WriteAllBytes(file, image.EncodeToPNG());
                return image.GetPixels32();
            }
            report.AppendLine($"AMBIENT OCCLUSION BY TIER ({Width}x{Height}; the same frame with 16 probes a pixel, the High tier, and with 8, Balanced)");
            for (int v = 0; v < 4 && v < Views.Length; v++)
            {
                Pose(cam, Views[v].at, Views[v].look);
                var full = Shoot(16, v == 0 ? $"{shots}/ao_16_{Views[v].name}.png" : null);
                var lite = Shoot(8, v == 0 ? $"{shots}/ao_08_{Views[v].name}.png" : null);
                int over2 = 0, over6 = 0, biggest = 0; long sum = 0;
                var mark = new Color32[full.Length];
                for (int i = 0; i < full.Length; i++)
                {
                    int d = Mathf.Max(Mathf.Abs(full[i].r - lite[i].r), Mathf.Max(Mathf.Abs(full[i].g - lite[i].g), Mathf.Abs(full[i].b - lite[i].b)));
                    if (d > 2) over2++;
                    if (d > 6) over6++;
                    if (d > biggest) biggest = d;
                    sum += d;
                    byte m = (byte)Mathf.Min(255, d * 8); mark[i] = new Color32(m, m, m, 255);
                }
                if (v == 0) { image.SetPixels32(mark); image.Apply(); File.WriteAllBytes($"{shots}/ao_diff_x8_{Views[v].name}.png", image.EncodeToPNG()); }
                report.AppendLine($"{Views[v].name,-28} pixels moved by more than 2/255: {100f * over2 / full.Length:F2}%, by more than 6/255: {100f * over6 / full.Length:F2}%, largest {biggest}/255, mean {(float)sum / full.Length:F2}/255");
            }
            Visual.WorldOutline.AmbientOcclusionSamplesTest = saved; cam.fieldOfView = savedFov;
            Object.Destroy(image); rt.Release(); Object.Destroy(rt);
        }

        /// <summary>
        /// What the map's own per-frame code costs on this PC, through the components' own
        /// methods: a fixed step of the live road (`KantoTraffic.UpdateRoutes`, every vehicle
        /// against every other; the match-start catch-up runs thousands of them), and a step of
        /// the sidewalk people with everybody posed and with nobody posed
        /// (`SidewalkLife.PoseUnseen`; in batch mode no camera draws, so "nobody seen" is what
        /// the life does by itself here). ⚠️ This runs LAST: it moves the street and the people on.
        /// </summary>
        private static void Simulation(StringBuilder report)
        {
            const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            report.AppendLine("SIMULATION (CPU time on this PC, the editor's Mono; a weak laptop is several times slower)");
            var traffic = Object.FindFirstObjectByType<KantoTraffic>();
            var routes = typeof(KantoTraffic).GetMethod("UpdateRoutes", F);
            var quiet = typeof(KantoTraffic).GetField("_quiet", F);
            if (traffic != null && routes != null && quiet != null)
            {
                const int steps = 3000;
                var step = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), traffic, routes);
                quiet.SetValue(traffic, true);
                var clock = Stopwatch.StartNew();
                for (int i = 0; i < steps; i++) step(1f / 30f);
                double micro = clock.Elapsed.TotalMilliseconds * 1000.0 / steps;
                quiet.SetValue(traffic, false);
                report.AppendLine($"live road: {micro:F1} microseconds a fixed step ({traffic.DriverCount} vehicles). The catch-up was 600 steps a frame = {micro * 600 / 1000:F1} ms here; " +
                                  $"it is now boxed at 4 ms a frame (20 ms behind the loading curtain), about {4000 / micro:F0} steps here. A full 20 minute epoch is 36000 steps = {micro * 36000 / 1e6:F2} s of stepping.");
            }
            else report.AppendLine("live road: no KantoTraffic, or its UpdateRoutes was not found");
            var life = Object.FindFirstObjectByType<SidewalkLife>();
            var lifeStep = typeof(SidewalkLife).GetMethod("Step", F);
            if (life != null && lifeStep != null)
            {
                var step = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), life, lifeStep);
                double Cost(bool pose)
                {
                    life.PoseUnseen = pose;
                    for (int i = 0; i < 30; i++) step(1f / 60f);       // past the quarter second a body is posed after it was last seen
                    var clock = Stopwatch.StartNew();
                    for (int i = 0; i < 300; i++) step(1f / 60f);
                    return clock.Elapsed.TotalMilliseconds / 300;
                }
                bool saved = life.PoseUnseen;
                // The people come out over the first minutes: the story is run on until half of them are out.
                int shown = 0;
                for (int warm = 0; warm < 9000 && shown * 2 < life.PeopleCount; warm++)
                {
                    step(1f / 30f);
                    shown = 0; for (int i = 0; i < life.PeopleCount; i++) if (life.PersonShown(i)) shown++;
                }
                double posed = Cost(true), walked = Cost(false);
                life.PoseUnseen = saved;
                report.AppendLine($"sidewalk people: {posed * 1000:F0} microseconds a frame with all {shown} of the {life.PeopleCount} people who are out posed, {walked * 1000:F0} with none seen (walked, not posed).");
            }
            else report.AppendLine("sidewalk people: no SidewalkLife, or its Step was not found");
        }

        /// <summary>Everything in the loaded scene, whatever the camera sees.</summary>
        private static void WholeScene(StringBuilder report)
        {
            var renderers = Object.FindObjectsByType<Renderer>();
            long tris = 0; int enabled = 0, casting = 0, batched = 0, skinned = 0;
            var materials = new HashSet<Material>();
            var byGroup = new SortedDictionary<string, (int renderers, long tris, int casters)>();
            foreach (var r in renderers)
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                enabled++;
                bool casts = r.shadowCastingMode != ShadowCastingMode.Off;
                if (casts) casting++;
                if (r.isPartOfStaticBatch) batched++;
                foreach (var m in r.sharedMaterials) if (m != null) materials.Add(m);
                long t = Triangles(r);
                if (r is SkinnedMeshRenderer) skinned++;
                tris += t;
                string g = GroupOf(r.transform);
                byGroup.TryGetValue(g, out var row);
                byGroup[g] = (row.renderers + 1, row.tris + t, row.casters + (casts ? 1 : 0));
            }
            report.AppendLine("WHOLE SCENE (in Play, with the match's bodies)");
            report.AppendLine($"renderers {enabled} enabled of {renderers.Length} ({batched} in static batches, {skinned} skinned, {casting} casting shadows), triangles {tris}, materials in use {materials.Count}, " +
                              $"LODGroups {Object.FindObjectsByType<LODGroup>().Length}");
            var colliders = Object.FindObjectsByType<Collider>();
            report.AppendLine("colliders " + colliders.Length + ": " + string.Join(", ", colliders.GroupBy(c => c.GetType().Name).OrderByDescending(g => g.Count()).Select(g => $"{g.Key} {g.Count()}")) +
                              $" (triggers {colliders.Count(c => c.isTrigger)}, rigidbodies {Object.FindObjectsByType<Rigidbody>().Length})");
            var lights = Object.FindObjectsByType<Light>();
            report.AppendLine("lights " + lights.Length + ": " + string.Join(", ", lights.GroupBy(l => l.type + (l.shadows != LightShadows.None ? " shadowed" : "")).Select(g => $"{g.Key} {g.Count()}")));
            report.AppendLine($"{"group",-22} {"renderers",9} {"triangles",10} {"casters",8}");
            foreach (var kv in byGroup.OrderByDescending(k => k.Value.tris))
                report.AppendLine($"{kv.Key,-22} {kv.Value.renderers,9} {kv.Value.tris,10} {kv.Value.casters,8}");
        }

        /// <summary>A renderer's triangles. ⚠️ In Play a statically batched renderer's MeshFilter
        /// holds the whole combined mesh; its own share is the sub-meshes from `subMeshStartIndex`,
        /// one per material.</summary>
        private static long Triangles(Renderer r)
        {
            Mesh mesh = null; int first = 0, count = 0;
            if (r is SkinnedMeshRenderer s) { mesh = s.sharedMesh; count = mesh != null ? mesh.subMeshCount : 0; }
            else if (r is MeshRenderer mr)
            {
                var f = r.GetComponent<MeshFilter>();
                mesh = f != null ? f.sharedMesh : null;
                if (mesh != null) { first = r.isPartOfStaticBatch ? mr.subMeshStartIndex : 0; count = r.isPartOfStaticBatch ? r.sharedMaterials.Length : mesh.subMeshCount; }
            }
            if (mesh == null) return 0;
            long t = 0;
            for (int k = first; k < first + count && k < mesh.subMeshCount; k++) t += (long)mesh.GetIndexCount(k) / 3;
            return t;
        }

        /// <summary>The builder's group a renderer belongs to: the child of "Dressing" above it,
        /// or the scene root object's name for everything else (the match's bodies, the life).</summary>
        private static string GroupOf(Transform t)
        {
            Transform top = t;
            while (top.parent != null)
            {
                if (top.parent.name == "Dressing") return top.name;
                top = top.parent;
            }
            return "(" + top.name + ")";
        }
#endif
    }
}
