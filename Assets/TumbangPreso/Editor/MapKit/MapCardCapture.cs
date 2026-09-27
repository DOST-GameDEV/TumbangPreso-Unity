using System;
using System.IO;
using System.Linq;
using TumbangPreso.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// THE MAP VOTE CARDS for Kanto and the Lagoon Cove: `Resources/UI/map-cards/&lt;Id&gt;.png`, the
    /// 960 x 540 picture `HubMapVote` shows for each registered map ("must show its actual court",
    /// MatchArrivalFlowTests). Written when both joined the map list (2026-09-27); the shipped
    /// maps' cards are not touched.
    ///
    /// ⚠️ CAPTURED IN PLAY, THROUGH THE MATCH'S OWN CAMERA. The look a player sees lives in
    /// components that only run in Play (`WorldLookPresentation`, `ColourGrade`, `WorldOutline`;
    /// none is marked to run in edit mode), so an editor-mode render is the authored scene, not the
    /// game (docs/KANTO_DESIGN_GUIDE.md § 12.1). This enters Play on the first map, waits for the
    /// match to install and settle, renders the real main camera from a card pose, loads the next
    /// map, and leaves Play. The viewmodel arms are switched off for the shot.
    ///
    /// Framing: high over the court and looking down across it, like the shipped cards, from the
    /// map's registry yaw. Menu: Tumbang Preso/Maps/Render Kanto and Lagoon Cove Cards.
    /// Batch: -executeMethod TumbangPreso.EditorTools.MapKit.MapCardCapture.Run (exits when done).
    /// </summary>
    [InitializeOnLoad]
    public static class MapCardCapture
    {
        private const int W = 960, H = 540;
        private const string QueueKey = "MapCardCapture.Queue", BatchKey = "MapCardCapture.Batch";
        private static int _startFrame = -1;
        private static double _startTime;

        static MapCardCapture()
        {
            EditorApplication.playModeStateChanged -= OnPlayMode;
            EditorApplication.playModeStateChanged += OnPlayMode;
            if (EditorApplication.isPlaying && !string.IsNullOrEmpty(SessionState.GetString(QueueKey, ""))) Begin();
        }

        [MenuItem("Tumbang Preso/Maps/Render Kanto and Lagoon Cove Cards")]
        public static void Menu() => Start(false);

        public static void Run() => Start(true);

        private static void Start(bool batch)
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("[MapCard] Stop Play first."); return; }
            if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string[] maps = { SceneFlow.LagoonCove, SceneFlow.Kanto };
            SessionState.SetString(QueueKey, string.Join(",", maps));
            SessionState.SetBool(BatchKey, batch);
            EditorSceneManager.OpenScene($"Assets/TumbangPreso/Scenes/Maps/{maps[0]}.unity", OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayMode(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && !string.IsNullOrEmpty(SessionState.GetString(QueueKey, ""))) Begin();
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(BatchKey, false) &&
                string.IsNullOrEmpty(SessionState.GetString(QueueKey, "")))
            {
                SessionState.EraseBool(BatchKey);
                AssetDatabase.Refresh();
                EditorApplication.Exit(0);
            }
        }

        private static void Begin() { _startFrame = -1; EditorApplication.update -= Poll; EditorApplication.update += Poll; }

        private static void Poll()
        {
            if (!EditorApplication.isPlaying) { EditorApplication.update -= Poll; return; }
            var queue = SessionState.GetString(QueueKey, "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (queue.Length == 0) { EditorApplication.update -= Poll; EditorApplication.ExitPlaymode(); return; }
            string map = queue[0];
            if (SceneManager.GetActiveScene().name != map) return;          // the next map is still loading
            if (_startFrame < 0) { _startFrame = Time.frameCount; _startTime = EditorApplication.timeSinceStartup; }
            bool ready = Object.FindFirstObjectByType<MatchInstaller>() != null && Camera.main != null;
            bool settled = Time.frameCount - _startFrame > 240 && EditorApplication.timeSinceStartup - _startTime > 5;
            bool timedOut = EditorApplication.timeSinceStartup - _startTime > 90;
            if (!(ready && settled) && !timedOut) return;
            try { if (ready) Shoot(map); else Debug.LogError("[MapCard] " + map + " never installed a match."); }
            catch (Exception e) { Debug.LogError("[MapCard] " + map + " failed: " + e); }
            var rest = queue.Skip(1).ToArray();
            SessionState.SetString(QueueKey, string.Join(",", rest));
            _startFrame = -1;
            if (rest.Length == 0) { EditorApplication.update -= Poll; SessionState.EraseString(QueueKey); EditorApplication.ExitPlaymode(); }
            else SceneManager.LoadScene(rest[0]);
        }

        private static void Shoot(string map)
        {
            var entry = SceneFlow.MapRegistry.First(e => e.Id == map);
            var cam = Camera.main;
            float floor = 0f;
            foreach (var hit in Physics.RaycastAll(new Vector3(0.3f, 80f, 0.3f), Vector3.down, 200f).OrderBy(h => h.distance))
            {
                if (hit.collider.isTrigger) continue;
                floor = hit.point.y; break;
            }
            var arms = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(b => b != null && b.enabled && b.GetType().Name == "ViewmodelArms").ToList();
            foreach (var a in arms) a.enabled = false;
            var pos = cam.transform.position; var rot = cam.transform.rotation; float fov = cam.fieldOfView; var target = cam.targetTexture;
            try
            {
                var dir = Quaternion.Euler(0, entry.Yaw, 0) * Vector3.back;
                var at = new Vector3(0, floor, 0) + dir * 24f + Vector3.up * 20f;
                cam.fieldOfView = Camera.HorizontalToVerticalFieldOfView(80f, 16f / 9f);
                cam.transform.SetPositionAndRotation(at, Quaternion.LookRotation(new Vector3(0, floor, 0) - dir * 3f - at));
                var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 4 };
                rt.Create(); cam.targetTexture = rt; cam.Render();
                var previous = RenderTexture.active;
                var display = RenderTexture.GetTemporary(W, H, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                bool write = GL.sRGBWrite; GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
                Graphics.Blit(rt, display); GL.sRGBWrite = write; RenderTexture.active = display;
                var image = new Texture2D(W, H, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, W, H), 0, 0); image.Apply();
                string path = $"Assets/TumbangPreso/Resources/UI/map-cards/{entry.Id}.png";
                File.WriteAllBytes(path, image.EncodeToPNG());
                RenderTexture.active = previous; cam.targetTexture = null;
                RenderTexture.ReleaseTemporary(display); rt.Release();
                Object.Destroy(rt); Object.Destroy(image);
                Debug.Log("[MapCard] " + entry.Id + " -> " + path);
            }
            finally
            {
                cam.transform.SetPositionAndRotation(pos, rot); cam.fieldOfView = fov; cam.targetTexture = target;
                foreach (var a in arms) if (a != null) a.enabled = true;
            }
        }
    }
}
