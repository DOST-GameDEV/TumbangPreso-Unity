using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TumbangPreso.Diagnostics
{
    /// <summary>
    /// THE PER-MAP FRAME BUDGET IN A LIVE BOT MATCH, IN THE BUILT PLAYER (owner, 2026-10-30: "i want it so optimized
    /// i'm able to run at 300+ fps" ... "on high graphics"). Off unless asked for:
    ///
    ///     TumbangPreso.exe -tp-framebudget out\dir [-tp-maps Payo,Kanto] [-tp-seconds 30]
    ///
    /// Each map loads with four bots in Hero Strike, the High tier, vsync off and no frame cap, warms 5 s, then records
    /// the live round. Main/render/GPU come from FrameTimingManager (the build turns frame timing stats on); markers read
    /// -1 in a release player, which only exposes counters, so their columns fill only in a development build.
    ///
    /// Owner, 2026-10-31 ("go ahead with 1 and 3, need to compare the cascades"), four more switches:
    ///   -tp-trace          writes MAP-frames.csv, every frame's time, main, GPU and GC, plus the markers in a development
    ///                      build, to find the uneven frames and the Eskinita hitch;
    ///   -tp-cascades N     runs at N shadow cascades instead of the tier's;
    ///   -tp-no-world-ao    keeps only the cast's creases, the screen pass's cost once a map's world AO is baked;
    ///   -tp-cascade-shots  freezes the round after the warm-up and shoots the same four eyes at 4, at 2 and at 4 cascades
    ///                      again (what differs between the two 4s moves by itself, so it says nothing about cascades);
    ///   -tp-no-replay      stops the replay's 10 Hz capture, -tp-no-rescan the outline's renderer scan: A/B for the uneven frames;
    ///   -tp-look-tests     the same frozen eyes with one suspect off at a time, for the floor's stripes and the plant outlines
    ///                      showing through walls (owner, 2026-10-31);
    ///   -tp-view-fps       holds the eye at the first player and times eight headings, a picture of each (owner, 2026-10-31:
    ///                      "im only getting 100 fps on payo when looking at the city");
    ///   -tp-size WxH       holds the window at that size; the saved settings otherwise replace the command line's.
    /// ⚠️ THE ARRIVAL FILM IS NOT THE ROUND: it held the screen for 8 to 24 s of the earlier runs, so the warm-up now starts
    /// once it has ended (`MatchArrivalPresentation.Active`), and every row states the size it really drew.
    /// </summary>
    public sealed class FrameBudgetProbe : MonoBehaviour
    {
        private static string _output;
        private static string[] _maps;
        private static float _seconds = 30f;
        private static bool _trace, _noWorldAO, _cascadeShots, _lookTests, _viewFps;
        private static int _cascades = -1, _width, _height;
        /// <summary>A benchmark run goes straight to the round, without the arrival film; the game itself never sets this.</summary>
        internal static bool SkipArrival;

        private static readonly (ProfilerCategory cat, string name)[] Markers =
        {
            (ProfilerCategory.Scripts, "PlayerLoop"),
            (ProfilerCategory.Scripts, "Update.ScriptRunBehaviourUpdate"),
            (ProfilerCategory.Scripts, "PreLateUpdate.ScriptRunBehaviourLateUpdate"),
            (ProfilerCategory.Physics, "FixedUpdate.PhysicsFixedUpdate"),
            (ProfilerCategory.Render, "Camera.Render"),
            (ProfilerCategory.Render, "Culling"),
            (ProfilerCategory.Render, "Shadows.RenderShadowMap"),
            (ProfilerCategory.Render, "Gfx.WaitForPresentOnGfxThread"),
            (ProfilerCategory.Gui, "UGUI.Rendering.UpdateBatches"),
            (ProfilerCategory.Gui, "Canvas.SendWillRenderCanvases"),
            (ProfilerCategory.Animation, "PreLateUpdate.DirectorUpdateAnimationEnd"),
            (ProfilerCategory.Scripts, "TP.WorldOutline.Rescan"),
            (ProfilerCategory.Scripts, "TP.PayoLife"),
            (ProfilerCategory.Scripts, "Update.ScriptRunDelayedDynamicFrameRate"),
            (ProfilerCategory.Scripts, "Update.ScriptRunDelayedTasks"),
            (ProfilerCategory.Internal, "EarlyUpdate.UpdatePreloading"),
            (ProfilerCategory.Internal, "PostLateUpdate.UpdateAllRenderers"),
            (ProfilerCategory.Internal, "PostLateUpdate.FinishFrameRendering"),
            (ProfilerCategory.Render, "Gfx.WaitForCommands"),
            (ProfilerCategory.Render, "Gfx.WaitForGfxCommandsFromMainThread"),
            (ProfilerCategory.Render, "Gfx.PresentFrame"),
            (ProfilerCategory.Internal, "GC.Collect"),
            (ProfilerCategory.Render, "Shader.CreateGPUProgram"),
            (ProfilerCategory.Loading, "Loading.ReadObject"),
            (ProfilerCategory.Loading, "Application.Integrate Assets in Background"),
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            var args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-tp-framebudget");
            if (at < 0 || at + 1 >= args.Length) return;
            _output = Path.GetFullPath(args[at + 1]);
            int maps = Array.IndexOf(args, "-tp-maps");
            _maps = maps >= 0 && maps + 1 < args.Length ? args[maps + 1].Split(',') : null;
            SkipArrival = true;
            _trace = Array.IndexOf(args, "-tp-trace") >= 0;
            _noWorldAO = Array.IndexOf(args, "-tp-no-world-ao") >= 0;
            _lookTests = Array.IndexOf(args, "-tp-look-tests") >= 0;
            _viewFps = Array.IndexOf(args, "-tp-view-fps") >= 0;
            _cascadeShots = _lookTests || Array.IndexOf(args, "-tp-cascade-shots") >= 0;
            int cascades = Array.IndexOf(args, "-tp-cascades");
            if (cascades >= 0 && cascades + 1 < args.Length) int.TryParse(args[cascades + 1], out _cascades);
            if (Array.IndexOf(args, "-tp-no-replay") >= 0) TumbangPreso.CameraSystem.SpectatorCamera.BenchNoReplayCapture = true;
            if (Array.IndexOf(args, "-tp-no-rescan") >= 0) TumbangPreso.Visual.WorldOutline.BenchNoRescan = true;
            int size = Array.IndexOf(args, "-tp-size");
            if (size >= 0 && size + 1 < args.Length)
            {
                var wh = args[size + 1].Split('x');
                if (wh.Length == 2) { int.TryParse(wh[0], out _width); int.TryParse(wh[1], out _height); }
            }
            int secs = Array.IndexOf(args, "-tp-seconds");
            if (secs >= 0 && secs + 1 < args.Length) float.TryParse(args[secs + 1], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out _seconds);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (_output == null) return;
            var go = new GameObject("~FrameBudgetProbe");
            DontDestroyOnLoad(go);
            go.AddComponent<FrameBudgetProbe>();
        }

        private static void Uncap()
        {
            if (GraphicsProfiles.Current != 2) GraphicsProfiles.Apply(2);
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            if (_cascades > 0) QualitySettings.shadowCascades = _cascades;
            if (_width > 0 && (Screen.width != _width || Screen.height != _height)) Screen.SetResolution(_width, _height, FullScreenMode.Windowed);
            if (_noWorldAO) TumbangPreso.Visual.WorldOutline.BakedWorldAO = true;
        }

        /// <summary>
        /// The prepass the ink reads, as a PNG (RG the normal, BA the depth), with the plants whose bounds lie on a ray through
        /// a sample pixel, to tell whether a plant's outline on a wall is the plant really being in that prepass.
        /// </summary>
        private IEnumerator DumpDepthNormals(string map, int heading, Action hide, MeshRenderer[] plants)
        {
            var cam = Camera.main;
            var rt = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
            var grab = new UnityEngine.Rendering.CommandBuffer { name = "FrameBudgetProbe prepass" };
            grab.Blit(UnityEngine.Rendering.BuiltinRenderTextureType.DepthNormals, rt);
            cam.AddCommandBuffer(UnityEngine.Rendering.CameraEvent.AfterForwardAlpha, grab);
            for (int f = 0; f < 4; f++) { hide(); yield return null; }
            cam.RemoveCommandBuffer(UnityEngine.Rendering.CameraEvent.AfterForwardAlpha, grab);
            grab.Release();
            var previous = RenderTexture.active; RenderTexture.active = rt;
            var read = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false, true);
            read.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); read.Apply();
            RenderTexture.active = previous;
            var o = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false, true);
            var src = read.GetPixels32(); var dst = new Color32[src.Length];
            for (int i = 0; i < src.Length; i++) { byte d = (byte)Mathf.Clamp(Mathf.RoundToInt((src[i].b / 255f + src[i].a / 65025f) * 255f * 8f), 0, 255); dst[i] = new Color32(src[i].r, src[i].g, d, 255); }
            o.SetPixels32(dst); o.Apply();
            File.WriteAllBytes(Path.Combine(_output, $"{map}_h{heading:000}_prepass.png"), o.EncodeToPNG());
            var log = new StringBuilder($"near {cam.nearClipPlane} far {cam.farClipPlane} eye {cam.transform.position} fwd {cam.transform.forward} prepass format {rt.width}x{rt.height}\n");
            foreach (var at in new[] { new Vector2(1170, 1000), new Vector2(1300, 1000), new Vector2(1100, 1040) })
            {
                var ray = cam.ScreenPointToRay(new Vector3(at.x, Screen.height - at.y, 0));
                var c = read.GetPixelBilinear(at.x / rt.width, 1f - at.y / rt.height);
                float eye = (c.b + c.a / 255f) * cam.farClipPlane;
                log.AppendLine($"pixel {at}: prepass eye depth {eye:F2} m; plants on the ray:");
                foreach (var r in plants)
                    if (r.bounds.IntersectRay(ray, out float dist))
                        log.AppendLine($"   {dist:F1} m {r.name} vis {r.isVisible} shadow {r.shadowCastingMode} static {r.isPartOfStaticBatch} lod {r.GetComponentInParent<LODGroup>() != null} mat {r.sharedMaterial.name} inst {r.sharedMaterial.enableInstancing} a {r.sharedMaterial.color.a:F2} cut {r.sharedMaterial.GetFloat("_Cutoff"):F2}");
            }
            File.WriteAllText(Path.Combine(_output, $"{map}_h{heading:000}_prepass.txt"), log.ToString());
            Destroy(read); Destroy(o); rt.Release(); Destroy(rt);
        }

        /// <summary>Each heading from the first player's eye: frame, main and GPU time held for four seconds, and its picture.</summary>
        private IEnumerator ViewFps(string map)
        {
            var round = GameServices.Round;
            var cam = Camera.main;
            if (round == null || round.Players.Count == 0 || cam == null) yield break;
            Vector3 from = round.Players[0].transform.position + Vector3.up * 1.7f;
            Quaternion look = Quaternion.identity;
            Camera.CameraCallback hold = c => { if (c == cam) cam.transform.SetPositionAndRotation(from, look); };
            Camera.onPreCull += hold;
            var lines = new StringBuilder($"{map} eye {from} {Screen.width}x{Screen.height} cascades {QualitySettings.shadowCascades}\nheading,fps,ms,p99,main,gpu\n");
            var timings = new FrameTiming[1];
            for (int heading = 0; heading < 360; heading += 45)
            {
                look = Quaternion.Euler(-4f, heading, 0f);
                for (float t = 0; t < 1f; t += Time.unscaledDeltaTime) { Uncap(); yield return null; }
                var ms = new List<float>(); double main = 0, gpu = 0; int n = 0;
                for (float t = 0; t < 4f; t += Time.unscaledDeltaTime)
                {
                    Uncap(); yield return null;
                    ms.Add(Time.unscaledDeltaTime * 1000f);
                    FrameTimingManager.CaptureFrameTimings();
                    if (FrameTimingManager.GetLatestTimings(1, timings) > 0 && timings[0].gpuFrameTime > 0) { main += timings[0].cpuMainThreadFrameTime; gpu += timings[0].gpuFrameTime; n++; }
                }
                float mean = ms.Average(); ms.Sort();
                var ci = System.Globalization.CultureInfo.InvariantCulture;
                lines.AppendLine(string.Format(ci, "{0},{1:F0},{2:F2},{3:F2},{4:F2},{5:F2}", heading, 1000f / mean, mean, Pct(ms, .99f), main / Mathf.Max(1, n), gpu / Mathf.Max(1, n)));
                ScreenCapture.CaptureScreenshot(Path.Combine(_output, $"{map}_view{heading:000}.png"));
                yield return null; yield return null;
            }
            Camera.onPreCull -= hold;
            File.WriteAllText(Path.Combine(_output, map + "-views.csv"), lines.ToString());
        }

        /// <summary>
        /// Names the renderers that put the plant outlines into the prepass over the floor (owner, 2026-10-31): every mesh
        /// renderer on the map is searched by halves in the frozen frame, against the prepass with all of them off.
        /// </summary>
        private IEnumerator FindGhost(string map, Action hide)
        {
            var cam = Camera.main;
            var all = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => r.enabled && r.gameObject.activeInHierarchy && r.sharedMaterials.Any(m => m != null && m.shader != null
                && (m.shader.name == "TumbangPreso/LagoonFoliage" || m.shader.name.Contains("Blade")))).ToArray();
            var rt = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
            var grab = new UnityEngine.Rendering.CommandBuffer { name = "FrameBudgetProbe search" };
            grab.Blit(UnityEngine.Rendering.BuiltinRenderTextureType.DepthNormals, rt);
            cam.AddCommandBuffer(UnityEngine.Rendering.CameraEvent.AfterForwardAlpha, grab);
            var read = new Texture2D(400, 200, TextureFormat.RGBA32, false, true);
            int x0 = 1000 * Screen.width / 2560, y0 = Screen.height - 1100 * Screen.height / 1440;
            Color32[] Snap()
            {
                var previous = RenderTexture.active; RenderTexture.active = rt;
                read.ReadPixels(new Rect(x0, y0, 400, 200), 0, 0); read.Apply();
                RenderTexture.active = previous;
                return read.GetPixels32();
            }
            var log = new StringBuilder($"search over {all.Length} renderers\n");
            var original = all.Select(r => r.enabled).ToArray();
            for (int f = 0; f < 4; f++) { hide(); yield return null; }
            var baseline = Snap();
            for (int f = 0; f < 3; f++) { hide(); yield return null; }
            int Noise(Color32[] a, Color32[] b)
            {
                int n = 0, lit = 0;
                for (int i = 0; i < a.Length; i++)
                {
                    if (a[i].r != 0 || a[i].g != 0) lit++;
                    if (Mathf.Abs(a[i].r - b[i].r) > 6 || Mathf.Abs(a[i].g - b[i].g) > 6) n++;
                }
                log.AppendLine($"self-check: {n} px differ between two untouched frames, {lit} of {a.Length} px hold a normal");
                return n;
            }
            Noise(baseline, Snap());
            int Count(Color32[] a, Color32[] b)
            {
                int n = 0;
                for (int i = 0; i < a.Length; i++)
                    if (Mathf.Abs(a[i].r - b[i].r) > 6 || Mathf.Abs(a[i].g - b[i].g) > 6) n++;
                return n;
            }
            // Turning a set OFF: what changes from the baseline is that set's mark on the floor's prepass.
            var stack = new Stack<MeshRenderer[]>(); stack.Push(all);
            int tests = 0;
            while (stack.Count > 0 && tests < 160)
            {
                var set = stack.Pop(); tests++;
                foreach (var r in set) r.enabled = false;
                for (int f = 0; f < 3; f++) { hide(); yield return null; }
                int changed = Count(baseline, Snap());
                foreach (var r in set) r.enabled = true;
                log.AppendLine($"  set of {set.Length} ({set[0].name}...): {changed} px");
                if (changed < 12) continue;
                if (set.Length == 1)
                {
                    var r = set[0];
                    log.AppendLine($"CULPRIT {changed} px: {r.name} parent {(r.transform.parent != null ? r.transform.parent.name : "-")} mesh {r.GetComponent<MeshFilter>()?.sharedMesh?.name} bounds {r.bounds.center} {r.bounds.size} dist {Vector3.Distance(cam.transform.position, r.bounds.center):F1} shadow {r.shadowCastingMode} mats {string.Join("+", r.sharedMaterials.Where(m => m != null).Select(m => m.name + "/" + m.shader.name + "/" + m.GetTag("RenderType", true) + "/q" + m.renderQueue + "/cut" + (m.HasProperty("_Cutoff") ? m.GetFloat("_Cutoff").ToString("F2") : "-") + "/tex" + (m.mainTexture != null ? m.mainTexture.name : "none")))}");
                    continue;
                }
                stack.Push(set.Take(set.Length / 2).ToArray());
                stack.Push(set.Skip(set.Length / 2).ToArray());
            }
            log.AppendLine($"{tests} tests");
            cam.RemoveCommandBuffer(UnityEngine.Rendering.CameraEvent.AfterForwardAlpha, grab);
            grab.Release(); rt.Release(); Destroy(rt); Destroy(read);
            for (int i = 0; i < all.Length; i++) all[i].enabled = original[i];
            File.WriteAllText(Path.Combine(_output, map + "-ghost-search.txt"), log.ToString());
        }

        /// <summary>
        /// The whole prepass with the plants on and off. A pixel the plants change without coming NEARER than what stands
        /// there without them cannot be a plant seen in front: it is the leak behind the outlines through walls. Writes a
        /// map of those pixels and the raw values of a sample of them (owner, 2026-10-31: "focus on the plant outlines").
        /// </summary>
        private IEnumerator LeakScan(string map, int heading, Action hide, MeshRenderer[] plants)
        {
            var cam = Camera.main;
            var rt = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
            var grab = new UnityEngine.Rendering.CommandBuffer { name = "FrameBudgetProbe leak" };
            grab.Blit(UnityEngine.Rendering.BuiltinRenderTextureType.DepthNormals, rt);
            cam.AddCommandBuffer(UnityEngine.Rendering.CameraEvent.AfterForwardAlpha, grab);
            var read = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false, true);
            Color32[] Snap()
            {
                var previous = RenderTexture.active; RenderTexture.active = rt;
                read.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); read.Apply();
                RenderTexture.active = previous;
                return read.GetPixels32();
            }
            for (int f = 0; f < 4; f++) { hide(); yield return null; }
            var on = Snap();
            var was = plants.Select(r => r.enabled).ToArray();
            foreach (var r in plants) r.enabled = false;
            for (int f = 0; f < 4; f++) { hide(); yield return null; }
            var off = Snap();
            for (int i = 0; i < plants.Length; i++) plants[i].enabled = was[i];
            float far = cam.farClipPlane;
            float Metres(Color32 c) => (c.b / 255f + c.a / 65025f) * far;
            int front = 0, leak = 0;
            var map2 = new Color32[on.Length];
            var log = new StringBuilder($"{map} {heading}, far {far}, x y | on r g metres | off r g metres\n");
            int every = 0;
            for (int i = 0; i < on.Length; i++)
            {
                map2[i] = new Color32(0, 0, 0, 255);
                if (Mathf.Abs(on[i].r - off[i].r) <= 3 && Mathf.Abs(on[i].g - off[i].g) <= 3 && on[i].b == off[i].b && on[i].a == off[i].a) continue;
                float a = Metres(on[i]), b = Metres(off[i]);
                if (a < b - .5f) { front++; map2[i] = new Color32(40, 90, 40, 255); continue; }
                leak++; map2[i] = new Color32(255, 255, 255, 255);
                if (every++ % 97 == 0 && every < 2500)
                    log.AppendLine($"{i % rt.width} {rt.height - 1 - i / rt.width} | {on[i].r} {on[i].g} {a:F2} | {off[i].r} {off[i].g} {b:F2}");
            }
            log.Insert(0, $"plant pixels in front {front}, leaked {leak}\n");
            File.WriteAllText(Path.Combine(_output, $"{map}_h{heading:000}_leak.txt"), log.ToString());
            var png = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false, true);
            png.SetPixels32(map2); png.Apply();
            File.WriteAllBytes(Path.Combine(_output, $"{map}_h{heading:000}_leak.png"), png.EncodeToPNG());
            cam.RemoveCommandBuffer(UnityEngine.Rendering.CameraEvent.AfterForwardAlpha, grab);
            grab.Release(); rt.Release(); Destroy(rt); Destroy(read); Destroy(png);
        }

        /// <summary>One eye, one suspect off per picture, each put back before the next.</summary>
        private IEnumerator LookTests(string map, int heading, Action hide)
        {
            Light sun = RenderSettings.sun;
            if (sun == null) sun = FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);
            float bias = sun != null ? sun.shadowBias : 0, normal = sun != null ? sun.shadowNormalBias : 0;
            var quality = QualitySettings.shadows;
            var ink = FindObjectsByType<TumbangPreso.Visual.WorldOutline>(FindObjectsSortMode.None);
            var plants = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => r.sharedMaterials.Any(m => m != null && m.shader != null
                && (m.shader.name == "TumbangPreso/LagoonFoliage" || m.shader.name.Contains("Blade")))).ToArray();
            var modes = plants.Select(r => r.shadowCastingMode).ToArray();
            Vector3 cam0 = Camera.main.transform.position;
            var foliageMaterials = plants.SelectMany(r => r.sharedMaterials).Where(m => m != null && m.shader.name == "TumbangPreso/LagoonFoliage").Distinct().ToArray();
            var plantMaterials = plants.SelectMany(r => r.sharedMaterials).Where(m => m != null && m.enableInstancing).Distinct().ToArray();
            var tests = new (string tag, Action off, Action on)[]
            {
                ("base", null, null),
                ("noshadow", () => QualitySettings.shadows = ShadowQuality.Disable, () => QualitySettings.shadows = quality),
                ("bias3", () => { if (sun != null) { sun.shadowBias = bias * 3; sun.shadowNormalBias = normal * 3; } }, () => { if (sun != null) { sun.shadowBias = bias; sun.shadowNormalBias = normal; } }),
                ("bias15", () => { if (sun != null) { sun.shadowBias = bias * 1.5f; sun.shadowNormalBias = normal * 1.5f; } }, () => { if (sun != null) { sun.shadowBias = bias; sun.shadowNormalBias = normal; } }),
                ("bias2", () => { if (sun != null) { sun.shadowBias = bias * 2f; sun.shadowNormalBias = normal * 2f; } }, () => { if (sun != null) { sun.shadowBias = bias; sun.shadowNormalBias = normal; } }),
                ("b2only", () => { if (sun != null) { sun.shadowBias = bias * 3f; } }, () => { if (sun != null) { sun.shadowBias = bias; } }),
                ("n3only", () => { if (sun != null) { sun.shadowNormalBias = normal * 3f; } }, () => { if (sun != null) { sun.shadowNormalBias = normal; } }),
                ("noink", () => { foreach (var o in ink) o.enabled = false; }, () => { foreach (var o in ink) o.enabled = true; }),
                ("noplants", () => { foreach (var r in plants) r.enabled = false; }, () => { foreach (var r in plants) r.enabled = true; }),
                ("noplantshadow", () => { foreach (var r in plants) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }, () => { for (int i = 0; i < plants.Length; i++) plants[i].shadowCastingMode = modes[i]; }),
                ("nofoliage", () => { foreach (var r in plants) if (r.sharedMaterials.Any(m => m != null && m.shader.name == "TumbangPreso/LagoonFoliage")) r.enabled = false; }, () => { foreach (var r in plants) r.enabled = true; }),
                ("nofarplants", () => { foreach (var r in plants) if (r.bounds.SqrDistance(cam0) > 100f * 100f) r.enabled = false; }, () => { foreach (var r in plants) r.enabled = true; }),
                ("nomerged", () => { foreach (var r in plants) if (r.name.StartsWith("plaza merged")) r.enabled = false; }, () => { foreach (var r in plants) r.enabled = true; }),
                ("noinstancing", () => { foreach (var m in plantMaterials) m.enableInstancing = false; }, () => { foreach (var m in plantMaterials) m.enableInstancing = true; }),
                ("noprepass", () => { foreach (var m in foliageMaterials) m.SetOverrideTag("RenderType", "PayoGrass"); }, () => { foreach (var m in foliageMaterials) m.SetOverrideTag("RenderType", ""); }),
                ("tol0", () => TumbangPreso.Visual.WorldOutline.BenchMaskTolerance = 0.00001f, () => TumbangPreso.Visual.WorldOutline.BenchMaskTolerance = -1f),
                ("nomasklist", () => TumbangPreso.Visual.WorldOutline.BenchNoExcluded = true, () => TumbangPreso.Visual.WorldOutline.BenchNoExcluded = false),
                ("baseb", null, null),
            };
            foreach (var (tag, off, on) in tests)
            {
                off?.Invoke();
                for (int f = 0; f < 8; f++) { hide(); yield return null; }
                hide();
                ScreenCapture.CaptureScreenshot(Path.Combine(_output, $"{map}_h{heading:000}_{tag}.png"));
                for (int f = 0; f < 8; f++) { hide(); yield return null; }
                on?.Invoke();
            }
            Debug.Log($"[FrameBudgetProbe] look tests {map} {heading}: {plants.Length} plants, sun {(sun != null ? sun.name : "none")}, bias {bias}/{normal}");
            yield return DumpDepthNormals(map, heading, hide, plants);
            if (map == "Payo" && heading == 270) yield return FindGhost(map, hide);
            yield return LeakScan(map, heading, hide, plants);
        }

        /// <summary>The same frozen frame at each cascade count, from four eyes around the first player, the overlay hidden.</summary>
        private IEnumerator CascadeShots(string map)
        {
            var round = GameServices.Round;
            var cam = Camera.main;
            if (round == null || round.Players.Count == 0 || cam == null) { Debug.Log("[FrameBudgetProbe] no eye for " + map); yield break; }
            Vector3 feet = round.Players[0].transform.position, from = feet;
            Time.timeScale = 0;
            // Pausing raises the spectator panel a frame later, so the overlay is hidden again on every frame of the shot.
            var canvases = new HashSet<Canvas>();
            void HideOverlay()
            {
                foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                    if (c.enabled) { c.enabled = false; canvases.Add(c); }
            }
            Quaternion look = Quaternion.identity;
            Camera.CameraCallback hold = c => { if (c == cam) cam.transform.SetPositionAndRotation(from, look); };
            Camera.onPreCull += hold;
            int before = QualitySettings.shadowCascades;
            for (int heading = 0; heading < 360; heading += 90)
            {
                look = Quaternion.Euler(12f, heading, 0f);
                from = feet + Vector3.up * 3f - look * Vector3.forward * 3f;   // behind and above the player, never inside a body
                if (_lookTests)
                {
                    yield return LookTests(map, heading, HideOverlay);
                    continue;
                }
                foreach (var (count, tag) in new[] { (4, "c4"), (2, "c2"), (4, "c4b") })
                {
                    QualitySettings.shadowCascades = count;
                    for (int f = 0; f < 8; f++) { HideOverlay(); yield return null; }
                    HideOverlay();
                    ScreenCapture.CaptureScreenshot(Path.Combine(_output, $"{map}_h{heading:000}_{tag}.png"));
                    for (int f = 0; f < 8; f++) { HideOverlay(); yield return null; }
                }
            }
            Camera.onPreCull -= hold;
            QualitySettings.shadowCascades = before;
            foreach (var c in canvases) if (c != null) c.enabled = true;
            Time.timeScale = 1;
        }

        private IEnumerator Start()
        {
            Directory.CreateDirectory(_output);
            var csv = new StringBuilder("map,width,height,frames,mean_ms,p95_ms,p99_ms,max_ms,spikes_over_6ms,main_mean,main_p99,render_mean,gpu_mean,gpu_p99,gc_bytes_frame,gc_frames_with_alloc,setpass,draws,"
                                        + string.Join(",", Markers.Select(m => m.name)) + "\n");
            File.WriteAllText(Path.Combine(_output, "scope.txt"),
                $"{SystemInfo.graphicsDeviceName} / {SystemInfo.graphicsDeviceType} / {SystemInfo.processorType}\n"
                + $"{Screen.width}x{Screen.height} High tier, vsync off, uncapped, 4 bots Hero Strike, {_seconds:F0} s live\n"
                + $"FrameTimingManager stats {FrameTimingManager.IsFeatureEnabled()}\n");

            foreach (string map in SceneFlow.Maps)
            {
                if (_maps != null && !_maps.Contains(map)) continue;
                SceneFlow.Networked = false;
                SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
                SceneFlow.SelectedMode = GameMode.HeroStrike;
                GameLaunch.AllBots = true; GameLaunch.Spectator = false;
                yield return SceneManager.LoadSceneAsync(map);

                float waited = 0;
                while (waited < 30f && (GameServices.Round == null || GameServices.Round.Players.Count < Balance.PlayerCount))
                { waited += Time.unscaledDeltaTime; yield return null; }
                var runner = FindFirstObjectByType<SliceRunner>();
                if (runner != null && !runner.Running) runner.Begin();
                Time.timeScale = 1;

                for (float t = 0; t < 2f || MatchArrivalPresentation.Active; t += Time.unscaledDeltaTime)
                {
                    Uncap();
                    if (t > 60f) { Debug.Log("[FrameBudgetProbe] arrival still on after 60 s on " + map); break; }
                    yield return null;
                }
                for (float t = 0; t < 5f; t += Time.unscaledDeltaTime) { Uncap(); yield return null; }

                if (_viewFps) { yield return ViewFps(map); continue; }
                if (_cascadeShots) { yield return CascadeShots(map); continue; }

                var row = new Row();
                yield return Record(row, map);
                string line = map + "," + Screen.width + "," + Screen.height + "," + row.Describe();
                csv.AppendLine(line);
                File.WriteAllText(Path.Combine(_output, "frame-budget.csv"), csv.ToString());
                Debug.Log("[FrameBudgetProbe] " + line);
            }
            Application.Quit(0);
        }

        private sealed class Row
        {
            public string Text;
            public string Describe() => Text;
        }

        private static float Pct(List<float> sorted, float p) => sorted.Count == 0 ? -1 : sorted[Mathf.Clamp((int)(sorted.Count * p), 0, sorted.Count - 1)];

        private static IEnumerator Record(Row into, string map)
        {
            var recorders = Markers.Select(m => ProfilerRecorder.StartNew(m.cat, m.name, 1)).ToArray();
            var gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
            var setpass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count", 1);
            var draws = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count", 1);
            var frame = new List<float>(20000); var main = new List<float>(20000); var gpu = new List<float>(20000);
            double render = 0; long gcBytes = 0; int gcFrames = 0; double sp = 0, dc = 0;
            var markerSum = new double[recorders.Length];
            var timings = new FrameTiming[1];
            var trace = _trace ? new List<float[]>(20000) : null;
            try
            {
                for (float t = 0; t < _seconds; t += Time.unscaledDeltaTime)
                {
                    Uncap();
                    yield return null;
                    frame.Add(Time.unscaledDeltaTime * 1000f);
                    float[] traced = null;
                    if (trace != null)
                    {
                        traced = new float[5 + recorders.Length];
                        traced[0] = t; traced[1] = Time.unscaledDeltaTime * 1000f; traced[4] = gc.Valid ? gc.LastValue : -1;
                        for (int i = 0; i < recorders.Length; i++) traced[5 + i] = recorders[i].Valid ? recorders[i].LastValue * 1e-6f : -1;
                        trace.Add(traced);
                    }
                    FrameTimingManager.CaptureFrameTimings();
                    if (FrameTimingManager.GetLatestTimings(1, timings) > 0)
                    {
                        main.Add((float)timings[0].cpuMainThreadFrameTime);
                        if (traced != null) { traced[2] = (float)timings[0].cpuMainThreadFrameTime; traced[3] = (float)timings[0].gpuFrameTime; }
                        render += timings[0].cpuRenderThreadFrameTime;
                        if (timings[0].gpuFrameTime > 0) gpu.Add((float)timings[0].gpuFrameTime);
                    }
                    if (gc.Valid) { gcBytes += gc.LastValue; if (gc.LastValue > 0) gcFrames++; }
                    if (setpass.Valid) sp += setpass.LastValue;
                    if (draws.Valid) dc += draws.LastValue;
                    for (int i = 0; i < recorders.Length; i++)
                        if (recorders[i].Valid) markerSum[i] += recorders[i].LastValue * 1e-6;
                }
            }
            finally
            {
                foreach (var r in recorders) r.Dispose();
                gc.Dispose(); setpass.Dispose(); draws.Dispose();
            }
            if (trace != null)
            {
                var ti = System.Globalization.CultureInfo.InvariantCulture;
                var lines = new StringBuilder("t,ms,main,gpu,gc," + string.Join(",", Markers.Select(m => m.name)) + "\n");
                foreach (var f in trace) lines.AppendLine(string.Join(",", f.Select(v => v.ToString("F3", ti))));
                File.WriteAllText(Path.Combine(_output, map + "-frames.csv"), lines.ToString());
            }
            int n = Mathf.Max(1, frame.Count);
            float mean = frame.Average(), max = frame.Max(); int spikes = frame.Count(f => f > 6f);
            frame.Sort(); main.Sort(); gpu.Sort();
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            string F(double v) => v.ToString("F3", ci);
            var s = new StringBuilder();
            s.Append(frame.Count).Append(',').Append(F(mean)).Append(',').Append(F(Pct(frame, .95f))).Append(',').Append(F(Pct(frame, .99f)))
             .Append(',').Append(F(max)).Append(',').Append(spikes)
             .Append(',').Append(F(main.Count > 0 ? main.Average() : -1)).Append(',').Append(F(Pct(main, .99f)))
             .Append(',').Append(F(main.Count > 0 ? render / main.Count : -1))
             .Append(',').Append(F(gpu.Count > 0 ? gpu.Average() : -1)).Append(',').Append(F(Pct(gpu, .99f)))
             .Append(',').Append(gc.Valid ? (gcBytes / n).ToString(ci) : "-1").Append(',').Append(gcFrames)
             .Append(',').Append(F(sp / n)).Append(',').Append(F(dc / n));
            for (int i = 0; i < recorders.Length; i++) s.Append(',').Append(F(markerSum[i] > 0 ? markerSum[i] / n : -1));
            into.Text = s.ToString();
        }
    }
}
