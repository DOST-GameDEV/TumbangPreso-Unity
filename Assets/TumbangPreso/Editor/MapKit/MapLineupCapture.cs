using System.IO;
using System.Linq;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// THE MAP LINEUP: every map in the shipped map select (GameLaunch.Maps), plus the Kanto
    /// sample, photographed the SAME way so they can be compared side by side before the map
    /// revamp (owner, 2026-09-26: "render out the current implemented map selections and i'll
    /// select a new one").
    ///
    /// Five shots per map, the Kanto review's set: an aerial over the court, and the game's eye
    /// (1.25 m above the floor, 95 degrees horizontal) from the court looking out to each side.
    /// The floor is found by a raycast down through the court's centre, since the maps do not
    /// share a floor height (Sa Bubong is a roof). Same camera stack as the Kanto review
    /// (the scene's ColourGrade, WorldOutline), so no map is flattered by a different look.
    ///
    /// Writes Logs/map-lineup-vN/<map>_<shot>.png (a new N every run: chat clients cache images
    /// by filename). Menu: Tumbang Preso/Maps/Render Map Lineup. Batch: .Run.
    /// ⚠️ It opens each scene in turn and reopens the scene that was open, without saving any.
    /// </summary>
    public static class MapLineupCapture
    {
        [MenuItem("Tumbang Preso/Maps/Render Map Lineup")]
        public static void Menu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string back = EditorSceneManager.GetActiveScene().path;
            string dir = Capture();
            if (!string.IsNullOrEmpty(back)) EditorSceneManager.OpenScene(back, OpenSceneMode.Single);
            EditorUtility.RevealInFinder(dir);
        }

        public static void Run() { Capture(); EditorApplication.Exit(0); }

        private static string Capture()
        {
            int v = 1; while (Directory.Exists("Logs/map-lineup-v" + v)) v++;
            string dir = "Logs/map-lineup-v" + v;
            Directory.CreateDirectory(dir);
            var maps = GameLaunch.Maps.Select(m => (m.Id, m.Scene)).ToList();
            maps.Add(("kanto", "Kanto"));
            foreach (var (id, scene) in maps)
            {
                string path = AssetDatabase.FindAssets("t:Scene " + scene).Select(AssetDatabase.GUIDToAssetPath)
                    .FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == scene);
                if (path == null) { Debug.LogWarning("[MapLineup] no scene for " + id); continue; }
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                Shoot(id, dir);
            }
            Debug.Log("[MapLineup] written to " + Path.GetFullPath(dir));
            return dir;
        }

        private static void Shoot(string id, string dir)
        {
            Physics.SyncTransforms();
            float floor = 0f;
            foreach (var hit in Physics.RaycastAll(new Vector3(0.3f, 80f, 0.3f), Vector3.down, 200f).OrderBy(h => h.distance))
            {
                if (hit.collider.isTrigger) continue;
                floor = hit.point.y; break;
            }
            float eye = floor + 1.25f;
            var cam = new GameObject("Map lineup witness").AddComponent<Camera>();
            cam.enabled = false; cam.nearClipPlane = 0.05f; cam.farClipPlane = 1500;
            cam.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            cam.gameObject.AddComponent<WorldOutline>().PrototypeEnabled = true;
            var shots = new (string name, Vector3 at, Vector3 look, float fov)[]
            {
                ("aerial", new Vector3(34, floor + 30, -38), new Vector3(0, floor + 2, 0), 60),
                ("eye_north", new Vector3(0, eye, -9), new Vector3(0, eye + 3, 45), 95),
                ("eye_east", new Vector3(-9, eye, 0), new Vector3(45, eye + 3, 0), 95),
                ("eye_south", new Vector3(0, eye, 9), new Vector3(0, eye + 3, -45), 95),
                ("eye_west", new Vector3(9, eye, 0), new Vector3(-45, eye + 3, 0), 95),
            };
            foreach (var s in shots)
            {
                cam.fieldOfView = Camera.HorizontalToVerticalFieldOfView(s.fov, 16f / 9f);
                cam.transform.SetPositionAndRotation(s.at, Quaternion.LookRotation(s.look - s.at));
                var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 4 };
                rt.Create(); cam.targetTexture = rt; cam.Render();
                var previous = RenderTexture.active;
                var display = RenderTexture.GetTemporary(1600, 900, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                bool write = GL.sRGBWrite; GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
                Graphics.Blit(rt, display); GL.sRGBWrite = write; RenderTexture.active = display;
                var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
                File.WriteAllBytes(Path.Combine(dir, id + "_" + s.name + ".png"), image.EncodeToPNG());
                RenderTexture.active = previous; cam.targetTexture = null;
                RenderTexture.ReleaseTemporary(display); rt.Release();
                Object.DestroyImmediate(rt); Object.DestroyImmediate(image);
            }
            Object.DestroyImmediate(cam.gameObject);
        }
    }
}
