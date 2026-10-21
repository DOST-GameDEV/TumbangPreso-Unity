using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// MEASURES KANTO'S LOOK TWO WAYS, SAME VIEWS, BEFORE ANYTHING IS CHANGED (owner, 2026-09-27:
    /// the lighting "when in game ... [is] different compared to when in the editor";
    /// docs/KANTO_DESIGN_GUIDE.md § 12.1: "Measure before changing anything ... Show the owner the
    /// two frames side by side; the owner decides which one is right").
    ///
    ///   1. EDITOR: the saved scene, rendered exactly as `KantoSceneBuilder.Review` does (a bare
    ///      camera with `ColourGrade.AdoptFromScene` and `WorldOutline`), from the review's own
    ///      eye-level poses.
    ///   2. PLAY: the editor enters Play on the same scene, waits until the match has installed
    ///      (MatchInstaller, the camera rig, and `WorldLookPresentation` if the map has a look),
    ///      then renders THE REAL MAIN CAMERA, with every component and hook the match gave it,
    ///      from the same poses, and leaves Play by itself.
    /// Both sides render into the same 1280 x 720 half-float target with 4x MSAA, so the only
    /// differences are the ones the game makes. Each side logs RenderSettings, every light,
    /// QualitySettings, the camera, its ColourGrade's fields, the render style and graphics
    /// profile, and the `_World*` shader weights. The report marks every line that differs.
    ///
    /// Output: Logs/kanto-look-vN/ editor_&lt;shot&gt;.png, play_&lt;shot&gt;.png, side_&lt;shot&gt;.png
    /// (editor left, Play right) and look.txt. Menu: Tumbang Preso/Sample Map/Measure Kanto Look.
    ///
    /// ⚠️ The viewmodel arms are switched off for the Play captures (they draw from the main
    /// camera's own pre-cull hook and would cover the view); nothing else about the camera is
    /// touched, and its pose, field of view and target are restored after each shot.
    /// </summary>
    [InitializeOnLoad]
    public static class KantoLookMeasure
    {
        private const string FolderKey = "KantoLookMeasure.Folder";
        private const string EnvKey = "KantoLookMeasure.EditorEnv";
        private const int W = 1280, H = 720;
        private static int _startFrame = -1;
        private static double _startTime;

        private static readonly (string name, Vector3 at, Vector3 look, float fov)[] Shots =
        {
            ("court-to-brick", new Vector3(6, 1.4f, 6), new Vector3(-30, 9, -30), 95),
            ("court-to-deco", new Vector3(-6, 1.4f, 6), new Vector3(30, 7, -30), 95),
            ("court-to-tower", new Vector3(6, 1.4f, -6), new Vector3(-30, 14, 30), 95),
            ("park-edge", new Vector3(0, 1.4f, -12), new Vector3(0, 3, 10), 95),
            ("aerial", new Vector3(62, 55, 70), new Vector3(0, 4, 0), 50),
        };

        static KantoLookMeasure()
        {
            // Play mode reloads the domain; the pending run survives in SessionState.
            EditorApplication.playModeStateChanged -= OnPlayMode;
            EditorApplication.playModeStateChanged += OnPlayMode;
            if (EditorApplication.isPlaying && !string.IsNullOrEmpty(SessionState.GetString(FolderKey, "")))
                BeginWaiting();
        }

        [MenuItem("Tumbang Preso/Sample Map/Measure Kanto Look (editor vs Play)")]
        public static void Measure()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("[KantoLook] Stop Play first."); return; }
            // Opening Kanto replaces the open scene: never discard the owner's unsaved edits.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            int v = 1; while (Directory.Exists("Logs/kanto-look-v" + v)) v++;
            string folder = "Logs/kanto-look-v" + v;
            Directory.CreateDirectory(folder);
            EditorSceneManager.OpenScene(KantoSceneBuilder.ScenePath, OpenSceneMode.Single);

            var camera = new GameObject("Kanto look witness").AddComponent<Camera>();
            camera.enabled = false; camera.nearClipPlane = 0.05f; camera.farClipPlane = 900;
            camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            camera.gameObject.AddComponent<WorldOutline>().PrototypeEnabled = true;
            string env = Environment("EDITOR (KantoSceneBuilder.Review's camera on the saved scene)", camera);
            foreach (var s in Shots) Capture(camera, s, Path.Combine(folder, "editor_" + s.name + ".png"));
            Object.DestroyImmediate(camera.gameObject);

            SessionState.SetString(FolderKey, folder);
            SessionState.SetString(EnvKey, env);
            Debug.Log("[KantoLook] Editor side written; entering Play for the match side.");
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayMode(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && !string.IsNullOrEmpty(SessionState.GetString(FolderKey, "")))
                BeginWaiting();
        }

        private static void BeginWaiting()
        {
            _startFrame = -1;
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
        }

        // Waits for the match to install and settle: MatchInstaller and a main camera, then
        // 240 frames and at least 4 s, so the world look, clouds and grade have all applied.
        private static void Poll()
        {
            if (!EditorApplication.isPlaying) { EditorApplication.update -= Poll; return; }
            if (_startFrame < 0) { _startFrame = Time.frameCount; _startTime = EditorApplication.timeSinceStartup; }
            bool installed = Object.FindFirstObjectByType<MatchInstaller>() != null && Camera.main != null;
            bool settled = Time.frameCount - _startFrame > 240 && EditorApplication.timeSinceStartup - _startTime > 4;
            bool timedOut = EditorApplication.timeSinceStartup - _startTime > 60;
            if (!(installed && settled) && !timedOut) return;
            EditorApplication.update -= Poll;
            string folder = SessionState.GetString(FolderKey, "");
            string editorEnv = SessionState.GetString(EnvKey, "");
            SessionState.EraseString(FolderKey); SessionState.EraseString(EnvKey);
            try { CapturePlay(folder, editorEnv, installed); }
            catch (Exception e) { Debug.LogError("[KantoLook] Play capture failed: " + e); }
            EditorApplication.ExitPlaymode();
        }

        private static void CapturePlay(string folder, string editorEnv, bool installed)
        {
            var camera = Camera.main;
            if (camera == null) { Debug.LogError("[KantoLook] No main camera in Play."); return; }
            string playEnv = Environment($"PLAY (the match's main camera; MatchInstaller found: {installed})", camera);
            var arms = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(b => b != null && b.enabled && b.GetType().Name == "ViewmodelArms").ToList();
            foreach (var a in arms) a.enabled = false;
            var pos = camera.transform.position; var rot = camera.transform.rotation;
            float fov = camera.fieldOfView; var target = camera.targetTexture;
            try { foreach (var s in Shots) Capture(camera, s, Path.Combine(folder, "play_" + s.name + ".png")); }
            finally
            {
                camera.transform.SetPositionAndRotation(pos, rot); camera.fieldOfView = fov; camera.targetTexture = target;
                foreach (var a in arms) if (a != null) a.enabled = true;
            }
            foreach (var s in Shots) SideBySide(folder, s.name);
            File.WriteAllText(Path.Combine(folder, "look.txt"), Report(editorEnv, playEnv));
            Debug.Log("[KantoLook] Written " + folder + " (editor_*, play_*, side_*, look.txt).");
        }

        private static void Capture(Camera camera, (string name, Vector3 at, Vector3 look, float fov) s, string path)
        {
            camera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(s.fov, 16f / 9f);
            camera.transform.SetPositionAndRotation(s.at, Quaternion.LookRotation(s.look - s.at));
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 4 };
            rt.Create(); camera.targetTexture = rt; camera.Render();
            var previous = RenderTexture.active;
            var display = RenderTexture.GetTemporary(W, H, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            bool write = GL.sRGBWrite; GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
            Graphics.Blit(rt, display); GL.sRGBWrite = write; RenderTexture.active = display;
            var image = new Texture2D(W, H, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, W, H), 0, 0); image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            RenderTexture.active = previous; camera.targetTexture = null;
            RenderTexture.ReleaseTemporary(display); rt.Release();
            Object.DestroyImmediate(rt); Object.DestroyImmediate(image);
        }

        private static void SideBySide(string folder, string shot)
        {
            var a = Load(Path.Combine(folder, "editor_" + shot + ".png"));
            var b = Load(Path.Combine(folder, "play_" + shot + ".png"));
            if (a == null || b == null) return;
            var side = new Texture2D(W * 2 + 8, H, TextureFormat.RGB24, false);
            var gap = Enumerable.Repeat(new Color32(20, 12, 6, 255), 8 * H).ToArray();
            side.SetPixels32(0, 0, W, H, a.GetPixels32());
            side.SetPixels32(W, 0, 8, H, gap);
            side.SetPixels32(W + 8, 0, W, H, b.GetPixels32());
            side.Apply();
            File.WriteAllBytes(Path.Combine(folder, "side_" + shot + ".png"), side.EncodeToPNG());
            Object.DestroyImmediate(a); Object.DestroyImmediate(b); Object.DestroyImmediate(side);
        }

        private static Texture2D Load(string path)
        {
            if (!File.Exists(path)) return null;
            var t = new Texture2D(2, 2, TextureFormat.RGB24, false);
            t.LoadImage(File.ReadAllBytes(path));
            return t.width == W && t.height == H ? t : null;
        }

        // ------------------------------------------------------------------ the environment

        private static string Environment(string title, Camera camera)
        {
            var sb = new StringBuilder();
            void Line(string key, object value) => sb.Append(key).Append(" = ").Append(Fmt(value)).Append('\n');
            sb.Append("# ").Append(title).Append('\n');
            Line("scene", UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            Line("render.ambientMode", RenderSettings.ambientMode);
            Line("render.ambientSky", RenderSettings.ambientSkyColor);
            Line("render.ambientEquator", RenderSettings.ambientEquatorColor);
            Line("render.ambientGround", RenderSettings.ambientGroundColor);
            Line("render.ambientLight", RenderSettings.ambientLight);
            Line("render.ambientIntensity", RenderSettings.ambientIntensity);
            Line("render.fog", RenderSettings.fog);
            Line("render.fogMode", RenderSettings.fogMode);
            Line("render.fogColor", RenderSettings.fogColor);
            Line("render.fogStart", RenderSettings.fogStartDistance);
            Line("render.fogEnd", RenderSettings.fogEndDistance);
            Line("render.fogDensity", RenderSettings.fogDensity);
            Line("render.skybox", RenderSettings.skybox != null ? RenderSettings.skybox.name + " / " + RenderSettings.skybox.shader.name : "none");
            Line("render.sun", RenderSettings.sun != null ? RenderSettings.sun.name : "none");
            Line("render.reflectionIntensity", RenderSettings.reflectionIntensity);
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.InstanceID))
            {
                string k = "light[" + l.name + "]";
                Line(k + ".enabled", l.enabled && l.gameObject.activeInHierarchy);
                Line(k + ".type", l.type);
                Line(k + ".color", l.color);
                Line(k + ".intensity", l.intensity);
                Line(k + ".euler", l.transform.eulerAngles);
                Line(k + ".shadows", l.shadows);
                Line(k + ".shadowStrength", l.shadowStrength);
            }
            Line("quality.level", QualitySettings.names[QualitySettings.GetQualityLevel()]);
            Line("quality.shadows", QualitySettings.shadows);
            Line("quality.shadowDistance", QualitySettings.shadowDistance);
            Line("quality.shadowCascades", QualitySettings.shadowCascades);
            Line("quality.shadowResolution", QualitySettings.shadowResolution);
            Line("quality.pixelLightCount", QualitySettings.pixelLightCount);
            Line("quality.antiAliasing", QualitySettings.antiAliasing);
            Line("quality.lodBias", QualitySettings.lodBias);
            Line("camera.allowHDR", camera.allowHDR);
            Line("camera.allowMSAA", camera.allowMSAA);
            Line("camera.renderingPath", camera.actualRenderingPath);
            Line("camera.depthTextureMode", camera.depthTextureMode);
            Line("camera.clearFlags", camera.clearFlags);
            Line("camera.components", string.Join(", ", camera.GetComponents<Component>().Select(c => c.GetType().Name)));
            var grade = camera.GetComponent<ColourGrade>();
            if (grade != null) DumpFields(sb, "grade", grade);
            DumpStatics(sb, "renderStyle", typeof(Settings.RenderStyles));
            DumpStatics(sb, "graphicsProfile", typeof(Settings.GraphicsProfiles));
            Line("worldLook.installed", WorldLookPresentation.Current != null);
            Line("worldLook.profileHasKanto", WorldLookProfile.Current != null && WorldLookProfile.Current.Find("Kanto") != null);
            foreach (var g in new[] { "_WorldLookWeight", "_WorldLookShape", "_WorldArchitecture" })
                Line("global." + g, Shader.GetGlobalFloat(g));
            return sb.ToString();
        }

        private static void DumpFields(StringBuilder sb, string prefix, object o)
        {
            foreach (var f in o.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var t = f.FieldType;
                if (!(t.IsPrimitive || t.IsEnum || t == typeof(Color) || t == typeof(Vector4) || t == typeof(Vector3))) continue;
                sb.Append(prefix).Append('.').Append(f.Name).Append(" = ").Append(Fmt(f.GetValue(o))).Append('\n');
            }
        }

        private static void DumpStatics(StringBuilder sb, string prefix, Type type)
        {
            foreach (var p in type.GetProperties(BindingFlags.Static | BindingFlags.Public))
            {
                var t = p.PropertyType;
                if (!(t.IsPrimitive || t.IsEnum) || p.GetIndexParameters().Length > 0) continue;
                object value; try { value = p.GetValue(null); } catch { continue; }
                sb.Append(prefix).Append('.').Append(p.Name).Append(" = ").Append(Fmt(value)).Append('\n');
            }
        }

        private static string Fmt(object v)
        {
            switch (v)
            {
                case float f: return f.ToString("0.###");
                case Color c: return $"({c.r:0.###}, {c.g:0.###}, {c.b:0.###}, {c.a:0.###})";
                case Vector3 x: return $"({x.x:0.##}, {x.y:0.##}, {x.z:0.##})";
                case Vector4 x: return $"({x.x:0.###}, {x.y:0.###}, {x.z:0.###}, {x.w:0.###})";
                default: return v?.ToString() ?? "null";
            }
        }

        private static string Report(string editorEnv, string playEnv)
        {
            Dictionary<string, string> Parse(string text) => text.Split('\n')
                .Where(l => l.Contains(" = ")).Select(l => l.Split(new[] { " = " }, 2, StringSplitOptions.None))
                .GroupBy(p => p[0]).ToDictionary(g => g.Key, g => g.First()[1]);
            var e = Parse(editorEnv); var p2 = Parse(playEnv);
            var sb = new StringBuilder("# Kanto look: editor vs Play. Lines marked * differ.\n\n");
            foreach (var key in e.Keys.Union(p2.Keys))
            {
                e.TryGetValue(key, out var a); p2.TryGetValue(key, out var b);
                sb.Append(a == b ? "  " : "* ").Append(key).Append("\n      editor: ").Append(a ?? "(absent)")
                  .Append("\n      play:   ").Append(b ?? "(absent)").Append('\n');
            }
            sb.Append("\n\n").Append(editorEnv).Append("\n").Append(playEnv);
            return sb.ToString();
        }
    }
}
