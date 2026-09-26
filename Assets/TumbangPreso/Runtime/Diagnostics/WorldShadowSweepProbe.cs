using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TumbangPreso.Diagnostics
{
    /// <summary>
    /// Opt-in: `-tp-shadowsweep DIR [-tp-map ID]`. Walks the first-person camera round an arena in
    /// a real player and photographs the REAL BACK BUFFER at every pose, twice: once as shipped and
    /// once with the map's sun shadows off. Where the two frames match, the sun's shadows were not
    /// being drawn at that pose.
    ///
    /// ⚠️ IT EXISTS BECAUSE EVERY OFFSCREEN RENDER SAID THE SHADOWS WERE FINE. The owner reported
    /// Ilalim ng Tulay's lighting changing with the view angle and distance (2026-09-27), with
    /// frames showing the whole street losing its sun shadows. Rendering the same match camera
    /// into a render texture at 2,000 poses, across every graphics tier, both lighting styles and
    /// every MSAA count, drew the sun's shadows every single time. The one path not covered was
    /// the camera drawing straight to the screen, and `ScreenCapture` only works there: batch
    /// mode never reaches the end of a frame.
    /// </summary>
    public sealed class WorldShadowSweepProbe : MonoBehaviour
    {
        private static string _output, _map;
        private static bool _throughHub;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            var args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-tp-shadowsweep");
            if (at < 0 || at + 1 >= args.Length || args.Contains("-tp-tournament")) return;
            _output = Path.GetFullPath(args[at + 1]);
            int map = Array.IndexOf(args, "-tp-map");
            _map = map >= 0 && map + 1 < args.Length ? args[map + 1] : SceneFlow.IlalimNgTulay;
            // ⚠️ THE OWNER'S ROUTE: HOME, then the arena through the loading curtain, with the
            // hub's live map preview and portraits loaded and torn down first.
            _throughHub = args.Contains("-tp-shadowsweep-hub");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (_output == null) return;
            var go = new GameObject("~WorldShadowSweepProbe");
            DontDestroyOnLoad(go);
            go.AddComponent<WorldShadowSweepProbe>();
        }

        private IEnumerator Start()
        {
            Directory.CreateDirectory(_output);
            var log = new StringBuilder();
            bool failed = false;
            var run = Run(log);
            while (true)
            {
                bool more;
                try { more = run.MoveNext(); }
                catch (Exception error) { Debug.LogException(error); log.AppendLine("ERROR " + error); failed = true; more = false; }
                if (!more) break;
                yield return run.Current;
            }
            File.WriteAllText(Path.Combine(_output, "sweep.txt"), log.ToString());
            Application.Quit(failed ? 2 : 0);
        }

        private IEnumerator Run(StringBuilder log)
        {
            SceneFlow.Networked = false;
            SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            GameLaunch.AllBots = false; GameLaunch.Spectator = false; GameLaunch.SoloSeat = 1;
            if (_throughHub)
            {
                for (int warm = 0; warm < 30; warm++) yield return null;
                SceneFlow.GoHome();
                float until = Time.realtimeSinceStartup + 60;
                while (UI.Hub.TumpHub.Current == null && Time.realtimeSinceStartup < until) yield return null;
                for (int warm = 0; warm < 180; warm++) yield return null;
                SceneFlow.SelectedMode = GameMode.HeroStrike;
                SceneFlow.SelectedMap = _map;
                float asked = Time.realtimeSinceStartup;
                SceneFlow.StartMatch();
                while (SceneManager.GetActiveScene().name != _map && Time.realtimeSinceStartup < asked + 60) yield return null;
                while (UI.Hub.HubLoading.Visible && Time.realtimeSinceStartup < asked + 90) yield return null;
                log.AppendLine($"hub route: arena on screen {Time.realtimeSinceStartup - asked:F2} s after StartMatch");
            }
            else yield return SceneManager.LoadSceneAsync(_map);
            for (int warm = 0; warm < 60; warm++) yield return null;
            while (GameServices.Round == null || GameServices.Round.PlayerAt(1) == null) yield return null;
            foreach (var brain in FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
            foreach (var input in FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) input.enabled = false;
            var who = GameServices.Round.PlayerAt(1);
            var rig = FindFirstObjectByType<CameraRig>();
            rig.Follow(who); rig.SetAimSource(AimSource.Movement);

            var sun = RenderSettings.sun != null ? RenderSettings.sun
                : FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional && l.shadows != LightShadows.None);
            log.AppendLine($"map={_map} screen={Screen.width}x{Screen.height} device={SystemInfo.graphicsDeviceType} quality={QualitySettings.shadows}/{QualitySettings.shadowCascades}/{QualitySettings.shadowDistance} msaa={QualitySettings.antiAliasing} sun={(sun != null ? sun.name : "none")}");
            foreach (var camera in Camera.allCameras)
                log.AppendLine($"camera {camera.name} depth={camera.depth} target={(camera.targetTexture != null)} hdr={camera.allowHDR} msaa={camera.allowMSAA} depthMode={camera.depthTextureMode} near={camera.nearClipPlane}");
            if (sun == null) yield break;

            float floor = Visual.WorldLookPresentation.Current != null ? Visual.WorldLookPresentation.Current.Floor : 0f;
            int poses = 0, missing = 0, saved = 0;
            for (float x = -12; x <= 12.01f; x += 4)
            for (float z = -12; z <= 12.01f; z += 4)
            {
                var standing = new Vector3(x, floor + 1.5f, z);
                if (!Physics.Raycast(standing, Vector3.down, out var hit, 4)) continue;
                for (int yaw = 0; yaw < 360; yaw += 45)
                {
                    who.Teleport(hit.point + Vector3.up * .02f);
                    who.transform.rotation = Quaternion.Euler(0, yaw, 0);
                    for (int settle = 0; settle < 4; settle++) yield return null;
                    yield return new WaitForEndOfFrame();
                    var shipped = ScreenCapture.CaptureScreenshotAsTexture();
                    var mode = sun.shadows; sun.shadows = LightShadows.None;
                    yield return null;
                    yield return new WaitForEndOfFrame();
                    var unshadowed = ScreenCapture.CaptureScreenshotAsTexture();
                    sun.shadows = mode;
                    float a = Luminance(shipped), b = Luminance(unshadowed);
                    poses++;
                    bool none = Mathf.Abs(a - b) < .003f;
                    if (none) missing++;
                    log.AppendLine(FormattableString.Invariant($"{x},{z},{yaw},{a:F4},{b:F4},{a - b:F4}{(none ? ",NO-SUN-SHADOW" : "")}"));
                    if ((none && saved < 12) || poses % 25 == 1)
                    {
                        saved++;
                        File.WriteAllBytes(Path.Combine(_output, $"{(none ? "missing" : "ok")}_{x}_{z}_{yaw}.png"), shipped.EncodeToPNG());
                    }
                    Destroy(shipped); Destroy(unshadowed);
                }
            }
            log.Insert(0, $"poses={poses} noSunShadow={missing}\n");
        }

        private static float Luminance(Texture2D frame)
        {
            var pixels = frame.GetPixels32();
            double sum = 0; int count = 0;
            for (int i = 0; i < pixels.Length; i += 5)
            {
                var c = pixels[i];
                sum += .2126 * c.r + .7152 * c.g + .0722 * c.b; count++;
            }
            return (float)(sum / Math.Max(1, count) / 255.0);
        }
    }
}
