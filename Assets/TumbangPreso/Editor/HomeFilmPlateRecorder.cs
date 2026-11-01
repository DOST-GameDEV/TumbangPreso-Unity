using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using TumbangPreso.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Records the real map through a HOME film's camera, frame by frame, as the plates the film's
    /// characters and effects are composited over (ArtSource/home-scene/README.md).
    ///
    /// It looks through the same MapPreviewSurface as the recorded map previews, so the plates have the
    /// map's lighting, grade, ink outline and running life (traffic, signals, pigeons), and only the
    /// camera differs. Run through tools/run_unity_guarded.py; ArtSource/home-scene/scripts/plates.mjs
    /// writes the camera file and starts it.
    /// </summary>
    [InitializeOnLoad]
    public static class HomeFilmPlateRecorder
    {
        const string CamKey = "HomeFilmPlateRecorder.Cam", OutKey = "HomeFilmPlateRecorder.Out";

        /// <summary>One picture. `dt` is how far the street then runs; 0 takes another picture of the same moment.</summary>
        [Serializable] class Shot { public string clip; public int i; public float x, y, z, yaw, pitch, roll, f, dt; public Vector3[] subjects; }
        [Serializable] class Track { public string map; public int warm; public float green; public Shot[] frames; }

        static HomeFilmPlateRecorder()
        {
            EditorApplication.playModeStateChanged -= Changed;
            EditorApplication.playModeStateChanged += Changed;
        }

        public static void Record()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Batch only: see ArtSource/home-scene/scripts/plates.mjs.");
            SessionState.SetString(CamKey, Argument("-tp-plate-cam") ?? throw new ArgumentException("-tp-plate-cam"));
            SessionState.SetString(OutKey, Argument("-tp-plate-out") ?? throw new ArgumentException("-tp-plate-out"));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        static string Argument(string key)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == key) return args[i + 1];
            return null;
        }

        static void Changed(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || string.IsNullOrEmpty(SessionState.GetString(CamKey, ""))) return;
            var go = new GameObject("~HomeFilmPlates");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<MapPreviewRecordingHost>().StartCoroutine(Guard(Run()));
        }

        static IEnumerator Guard(IEnumerator work)
        {
            while (true)
            {
                bool moved;
                try { moved = work.MoveNext(); }
                catch (Exception error) { Debug.LogException(error); SessionState.EraseString(CamKey); EditorApplication.Exit(1); yield break; }
                if (!moved) break;
                yield return work.Current;
            }
            SessionState.EraseString(CamKey);
            Debug.Log("[HomeFilmPlates] done");
            EditorApplication.Exit(0);
        }

        static IEnumerator Run()
        {
            var track = JsonUtility.FromJson<Track>(File.ReadAllText(SessionState.GetString(CamKey, "")));
            string output = SessionState.GetString(OutKey, "");
            foreach (var shot in track.frames) Directory.CreateDirectory(Path.Combine(output, shot.clip));
            GameServices.Ensure();

            var root = new GameObject("Plates " + track.map, typeof(RectTransform), typeof(RawImage));
            var view = root.AddComponent<MapPreviewSurface>();
            view.PreferRecordedPreview = false;
            view.Show(track.map);
            while (view.Showing != track.map || view.Camera == null) yield return null;
            yield return null; yield return null; // The world look lifts the ground in its first Update (MAP_PREVIEW_RECORDING.md).

            const int width = 1920, height = 1080;
            var camera = view.Camera;
            var source = camera.targetTexture;
            var descriptor = source.descriptor; descriptor.width = width; descriptor.height = height;
            var target = new RenderTexture(descriptor) { name = source.name, filterMode = source.filterMode };
            target.Create();
            typeof(MapPreviewSurface).GetField("_target", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(view, target);
            camera.targetTexture = target;
            root.GetComponent<RawImage>().texture = target;
            source.Release(); Object.Destroy(source);
            camera.aspect = 16f / 9f;
            camera.nearClipPlane = 0.1f;
            camera.enabled = false;
            view.enabled = false; // Its Update would put the sway back on the camera.

            var traffic = Object.FindFirstObjectByType<KantoTraffic>();
            if (traffic != null)
                foreach (var signal in traffic.Signals)
                    if (signal != null) Debug.Log("[HomeFilmPlates] signal " + signal.name + " at " + signal.bounds.center.ToString("F2") + " faces " + signal.transform.forward.ToString("F2"));
            var scenery = new List<Renderer>();
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                if (r.gameObject.scene.name == track.map && r.bounds.size.magnitude < MaxSpan) scenery.Add(r);
            var hidden = new List<(Renderer r, ShadowCastingMode was)>();
            Debug.Log("[HomeFilmPlates] " + track.map + " traffic=" + (traffic != null ? traffic.Drivers.Length.ToString() : "none") + " look weight=" + TumbangPreso.Visual.WorldCueProfile.LightingWeight);

            float capture = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;
            for (int i = 0; i < track.warm; i++) yield return null; // Let the street spread out and the flock settle.

            // The film's light turns green on its own beat, so the street's cycle is set to agree with it.
            if (traffic != null && track.green >= 0f) SetClockForGreen(traffic, track.green);

            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            var display = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previous = RenderTexture.active;
            bool srgb = GL.sRGBWrite;
            var clock = Stopwatch.StartNew();
            try
            {
                for (int n = 0; n < track.frames.Length; n++)
                {
                    var s = track.frames[n];
                    Time.captureDeltaTime = s.dt > 0f ? s.dt : 1f / 60f;
                    camera.transform.SetPositionAndRotation(new Vector3(s.x, s.y, s.z), Quaternion.Euler(s.pitch, s.yaw, s.roll));
                    camera.fieldOfView = 2f * Mathf.Atan(540f / s.f) * Mathf.Rad2Deg;
                    Clear(scenery, hidden, camera.transform.position, s.subjects);
                    camera.Render();
                    GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
                    Graphics.Blit(target, display);
                    RenderTexture.active = display;
                    image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    image.Apply();
                    File.WriteAllBytes(Path.Combine(output, s.clip, s.i.ToString("D5") + ".jpg"), image.EncodeToJPG(96));
                    RenderTexture.active = previous;
                    GL.sRGBWrite = srgb;
                    if (n % 120 == 0) Debug.Log("[HomeFilmPlates] " + n + "/" + track.frames.Length + " " + clock.Elapsed.TotalSeconds.ToString("F0") + "s");
                    if (s.dt > 0f) yield return null;
                }
            }
            finally
            {
                Time.captureDeltaTime = capture;
                RenderTexture.active = previous;
                GL.sRGBWrite = srgb;
                Object.Destroy(image);
                RenderTexture.ReleaseTemporary(display);
            }
        }

        /// <summary>Bigger than this is the street itself (MapPreviewSurface.MaxOccluderSpan).</summary>
        const float MaxSpan = 14f;
        /// <summary>A prop this close to the lens is a blur across the frame, not a part of the shot.</summary>
        const float LensClearance = 2.2f;

        /// <summary>
        /// The cast is composited over the plate, so nothing in the map may stand in front of them. Whatever
        /// would is drawn for its shadow only, for just the frames it is in the way.
        /// </summary>
        static void Clear(List<Renderer> scenery, List<(Renderer r, ShadowCastingMode was)> hidden, Vector3 eye, Vector3[] subjects)
        {
            foreach (var h in hidden) if (h.r != null) h.r.shadowCastingMode = h.was;
            hidden.Clear();
            foreach (var r in scenery)
            {
                if (r == null || !r.enabled) continue;
                var b = r.bounds;
                bool away = (b.ClosestPoint(eye) - eye).sqrMagnitude > LensClearance * LensClearance;
                if (away && (subjects == null || !Blocks(eye, b, subjects))) continue;
                hidden.Add((r, r.shadowCastingMode));
                r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
        }

        static bool Blocks(Vector3 eye, Bounds b, Vector3[] subjects)
        {
            foreach (var at in subjects)
            {
                Vector3 to = at - eye;
                float far = to.magnitude;
                if (far < 0.5f) continue;
                if (b.IntersectRay(new Ray(eye, to / far), out float hit) && hit < far - 0.6f) return true;
            }
            return false;
        }

        /// <summary>Winds the signals so the Z road's green opens this many seconds after the first plate.</summary>
        static void SetClockForGreen(KantoTraffic traffic, float seconds)
        {
            float cycle = 2f * (traffic.GreenSeconds + traffic.AmberSeconds + traffic.AllRedSeconds);
            float zGreenStarts = cycle / 2f;
            typeof(KantoTraffic).GetField("_clock", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(traffic, Mathf.Repeat(zGreenStarts - seconds, cycle));
        }
    }
}
