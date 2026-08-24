using System.IO;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// In-engine capture of Ilalim ng Tulay, from the angles a decision actually gets made from.
    ///
    /// ⚠️⚠️ THE FILENAMES CARRY A VERSION AND `Version` MUST BE BUMPED EVERY TIME. `CLAUDE.md`
    /// § 6.1: chat clients cache images by name, so overwriting a render leaves the previous one
    /// on screen and the whole review is conducted against a picture that is no longer on disk.
    ///
    /// ⚠️⚠️ IT RUNS `EnvColourPass.Apply()` BEFORE RENDERING, AND WITHOUT THAT THE SHOTS ARE A
    /// LIE. That pass is what gives every map its seeded Manila facade palette, its roof
    /// atlases and its warm-neutral road, and it runs from `Start()`, which never happens in an
    /// edit-mode capture. The first four renders of this map were taken without it, so they
    /// showed raw `.mtl` colours and the map looked like it belonged to a different game than
    /// Eskinita. Half of that complaint was the capture, not the map.
    ///
    /// ⚠️ SHOTS 3 AND 4 ARE EYE HEIGHT AT THE TWO POSITIONS THE MATCH IS PLAYED FROM, and they
    /// are the two that matter. The old set had an overview, a shop interior and two angles
    /// chosen for the props; none of the four looked along the throwing lane, and none of them
    /// looked at a pavement edge, which is how a map with both pavements floating over open air
    /// got signed off from its own showcase.
    /// </summary>
    public static class IlalimNgTulayShowcaseProbe
    {
        private const string OutDir = "Logs/shots-ilalim";
        private const int ShotWidth = 1280;
        private const int ShotHeight = 720;

        /// <summary>Bump on every capture. See the class note.</summary>
        private const string Version = "v6";

        [MenuItem("Tumbang Preso/Capture Ilalim Ng Tulay Showcase")]
        public static void RunFromMenu() => Execute();

        public static void Run() => EditorApplication.Exit(Execute() ? 0 : 1);

        public static bool Execute()
        {
            Directory.CreateDirectory(OutDir);
            EditorSceneManager.OpenScene(IlalimNgTulayBuilder.ScenePath, OpenSceneMode.Single);

            foreach (var pass in Object.FindObjectsByType<EnvColourPass>(
                         FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                pass.Apply();
            }

            float r = Balance.ConfinementRadius;
            float line = Confinement.ThrowingLine();
            float ring = Confinement.AttackerSpawnRing();

            // 1. The street, from above the south east pavement. Chalk, kerb line, guideway,
            // both column rows and the shopfront line all in one frame.
            Shot("overview", new Vector3(9.5f, 6.6f, -19.5f), Quaternion.Euler(17.0f, -19.0f, 0.0f), 66.0f);

            // 2. The taya's problem. Standing on the can, looking down the south lane at where
            // the attackers throw from.
            Shot("taya_view", new Vector3(0.0f, 1.65f, 0.4f), Quaternion.Euler(4.0f, 180.0f, 0.0f), 72.0f);

            // 3. The attacker's problem, from the spawn ring looking north at the can. This is
            // the shot that shows whether the chalk, the throwing line and the can all read.
            Shot("thrower_view", new Vector3(-1.4f, 1.65f, -ring), Quaternion.Euler(5.0f, 6.0f, 0.0f), 72.0f);

            // 4. PC Express from the carriageway, which is where a player sees it from.
            Shot("pcexpress", new Vector3(-2.0f, 2.1f, 5.5f), Quaternion.Euler(6.0f, -74.0f, 0.0f), 62.0f);

            // 5. The east pavement: pisonet, pares cart, clutter, and the kerb step that tells a
            // player where the box ends.
            Shot("street_life", new Vector3(2.0f, 2.0f, -1.5f), Quaternion.Euler(6.0f, 68.0f, 0.0f), 70.0f);

            // 6. The hoop, from the line a player would try the shot from.
            Shot("hoop", new Vector3(-3.0f, 1.7f, -5.5f), Quaternion.Euler(-11.0f, -40.0f, 0.0f), 68.0f);

            // 7. Straight down the corridor at chest height, to check that the street closes at
            // both ends instead of opening into sky.
            Shot("corridor", new Vector3(0.0f, 1.6f, -14.0f), Quaternion.Euler(2.0f, 0.0f, 0.0f), 74.0f);

            Debug.Log($"[IlalimNgTulayShowcaseProbe] captured 7 shots ({Version}) into {OutDir}. " +
                      $"chalk r={r:F2}, throwing line={line:F2}, spawn ring={ring:F2}");
            return true;
        }

        private static void Shot(string name, Vector3 pos, Quaternion rot, float fov)
        {
            var camGo = new GameObject("~ShowcaseCam");
            camGo.transform.position = pos;
            camGo.transform.rotation = rot;

            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 260.0f;
            cam.clearFlags = CameraClearFlags.Skybox;

            camGo.AddComponent<ColourGrade>().Set(1.05f, 1.10f, 1.15f, 0.92f, 1.85f);

            var rt = new RenderTexture(ShotWidth, ShotHeight, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 4,
            };

            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(ShotWidth, ShotHeight, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, ShotWidth, ShotHeight), 0, 0);
            tex.Apply();

            RenderTexture.active = null;
            cam.targetTexture = null;

            string path = Path.Combine(OutDir, $"ilalim_{name}_{Version}.png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Debug.Log($"[IlalimNgTulayShowcaseProbe] {path} ({new FileInfo(path).Length / 1024} KB)");

            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(camGo);
        }
    }
}
