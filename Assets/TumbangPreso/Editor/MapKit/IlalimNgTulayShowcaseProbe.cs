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
        private const string Version = "v24";

        [MenuItem("Tumbang Preso/Capture Ilalim Ng Tulay Showcase")]
        public static void RunFromMenu() => Execute();

        public static void Run() => EditorApplication.Exit(Execute() ? 0 : 1);

        /// <summary>When set, only the frames that judge the PC Express fascia are rendered.
        /// ⚠ IT IS A LOOKING TOOL, NOT A SIGN-OFF. A three-frame set cannot show a floating
        /// prop or a repeated sign, which is the whole reason the full set has fifteen.</summary>
        internal static bool FasciaOnly;

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
            Shot("overview", new Vector3(6.2f, 7.0f, -15.2f), Quaternion.Euler(18.0f, -13.0f, 0.0f), 66.0f);

            // 2. The taya's problem. Standing on the can, looking down the south lane at where
            // the attackers throw from.
            Shot("taya_view", new Vector3(0.0f, 1.65f, 0.4f), Quaternion.Euler(4.0f, 180.0f, 0.0f), 72.0f);

            // 3. The attacker's problem, from the spawn ring looking north at the can. This is
            // the shot that shows whether the chalk, the throwing line and the can all read.
            Shot("thrower_view", new Vector3(-1.4f, 1.65f, -ring), Quaternion.Euler(5.0f, 6.0f, 0.0f), 72.0f);

            // 4. PC Express from the carriageway, which is where a player sees it from.
            Shot("pcexpress", new Vector3(-2.0f, 2.1f, 5.5f), Quaternion.Euler(6.0f, -74.0f, 0.0f), 62.0f);

            if (FasciaOnly)
            {
                Shot("pcexpress_close", new Vector3(-6.2f, 3.30f, 5.5f),
                     Quaternion.Euler(-5.0f, -90.0f, 0.0f), 42.0f);
                Shot("pcexpress_angle", new Vector3(-4.4f, 2.60f, 1.4f),
                     Quaternion.Euler(-6.0f, -58.0f, 0.0f), 55.0f);
                Debug.Log($"[IlalimNgTulayShowcaseProbe] fascia-only set ({Version}).");
                return true;
            }

            // 5. The east pavement: pisonet, pares cart, clutter, and the kerb step that tells a
            // player where the box ends.
            Shot("street_life", new Vector3(2.0f, 2.0f, -1.5f), Quaternion.Euler(6.0f, 68.0f, 0.0f), 70.0f);

            // 6. The hoop, from the line a player would try the shot from.
            Shot("hoop", new Vector3(-3.0f, 1.7f, -5.5f), Quaternion.Euler(-11.0f, -40.0f, 0.0f), 68.0f);

            // 7. Straight down the corridor at chest height, to check that the street closes at
            // both ends instead of opening into sky.
            Shot("corridor", new Vector3(0.0f, 1.6f, -14.0f), Quaternion.Euler(2.0f, 0.0f, 0.0f), 74.0f);

            // 8. Above the parapet, because wheel-to-rail fit and dual-track width cannot be
            // judged from the street even though the whole structure reads from down there.
            Shot("guideway", new Vector3(13.0f, 13.0f, -19.0f),
                 Quaternion.Euler(23.0f, -22.0f, 0.0f), 66.0f);

            // ------------------------------------------------------------------
            // 9 to 15 were added for the composition pass.
            //
            // ⚠⚠ THE FIRST EIGHT ALL LOOK AT SOMETHING THAT WAS BUILT ON PURPOSE, AND THAT
            // IS EXACTLY WHY THEY MISSED WHAT WAS WRONG. Every one of them frames the guideway,
            // a shop or the chalk, so the v14 set contains no picture of the ground between the
            // near row and the far belt, no picture of the strip read ALONG a pavement (which is
            // how sign repetition shows), and only one direction of the lane a thrower stands
            // in. The shots below are chosen to expose faults rather than to present features.
            // ------------------------------------------------------------------

            // 9. The fascia at reading distance. The mark is traced from the official artwork
            // and the only way to judge a trace is to fill the frame with it.
            Shot("pcexpress_close", new Vector3(-6.2f, 3.30f, 5.5f),
                 Quaternion.Euler(-5.0f, -90.0f, 0.0f), 42.0f);

            // 10. The other throwing lane. A map with a front and a back is a map that was
            // composed from one camera, and the north approach had never been rendered.
            Shot("thrower_view_north", new Vector3(1.4f, 1.65f, ring),
                 Quaternion.Euler(5.0f, 186.0f, 0.0f), 72.0f);

            // 11 and 12. Along each pavement, at eye height, looking down the row. This is the
            // frame that shows whether two neighbouring businesses share a sign silhouette,
            // because it is the only one that puts several of them side by side.
            Shot("pavement_west", new Vector3(-9.2f, 1.65f, -12.5f),
                 Quaternion.Euler(2.0f, 12.0f, 0.0f), 74.0f);
            Shot("pavement_east", new Vector3(9.2f, 1.65f, 12.5f),
                 Quaternion.Euler(2.0f, 192.0f, 0.0f), 74.0f);

            // 13. High enough to hold all three depths of § 9.1 at once: the quiet box, the
            // shop strip, and the district behind it.
            Shot("depth_overview", new Vector3(-26.0f, 26.0f, -30.0f),
                 Quaternion.Euler(30.0f, 40.0f, 0.0f), 60.0f);

            // 14 and 15. The background on its own terms, from inside the walls, which is where
            // a player actually sees it from. Repetition and bare ground both show here first.
            Shot("background_north", new Vector3(-4.0f, 2.4f, 14.0f),
                 Quaternion.Euler(-2.0f, 24.0f, 0.0f), 76.0f);
            Shot("background_south", new Vector3(5.0f, 2.4f, -14.0f),
                 Quaternion.Euler(-2.0f, 200.0f, 0.0f), 76.0f);

            Debug.Log($"[IlalimNgTulayShowcaseProbe] captured 15 shots ({Version}) into {OutDir}. " +
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

            // ⚠️⚠️ IT ADOPTS THE MAP'S OWN GRADE AND IT USED TO HARD-CODE ONE, WHICH IS THE
            // WHOLE REASON THIS PROBE SIGNED OFF A MAP THAT SHIPPED BLACK.
            //
            // The line here read `Set(1.05f, 1.10f, 1.15f, 0.92f, 1.85f)`. The map's `MapGrade`
            // carried an exposure of **0.15**, so every showcase render from v15 to v22 was
            // taken through Eskinita's 0.92 while the GAME rendered the street through 0.15,
            // where every linear value under 0.59 clips to pure black. Fifteen frames per set,
            // eight sets, all of them lying about the one property that made the map unplayable.
            //
            // ⚠️ A CAPTURE MUST GRADE THE WAY THE MATCH GRADES OR IT IS NOT EVIDENCE.
            // `ColourGrade.AdoptFromScene` is the same call `CameraRig` makes when a match
            // starts, so the probe now sees exactly what a player sees, including a wrong grade.
            // `MapGradeSanityTests` catches the value; this makes the picture honest as well.
            camGo.AddComponent<ColourGrade>().AdoptFromScene();

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
