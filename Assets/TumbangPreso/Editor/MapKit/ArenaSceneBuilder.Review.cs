using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.Map;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// The Arena's review renders (ARENA-1.5): the built scene photographed from the places the
    /// art brief says every kit must be judged from, without entering Play. Versioned PNGs in
    /// Logs/arena/unity/vN (chat clients cache images by file name), each shot twice as the Ilalim
    /// review does: `look`, the match look installed as the map preview installs it (the look
    /// row, the grade and its bloom, the outline), and `authored`, the scene's own light alone.
    ///
    ///   eye_n .. eye_nw      a player's eye by the can, looking out along each of eight bearings
    ///   game_attacker, game_taya   about where the game's third-person camera sits behind each
    ///   shaft_down           over the stage's edge, down the shaft to the city floor
    ///   upper_stand_top_row  the top row of the south stand, looking at the stage
    ///   plaza, air, air_far, under   the hull's plaza, the whole thing from the air, from below
    ///   layout_NAME          each of the layouts from straight above, north up
    /// The first three kinds are taken through the GAME camera's range (`PlayFar`, the ink's own
    /// fade): what a player would see. The rest are free cameras with the whole map in range.
    ///
    /// ⚠️ WHAT A STILL OF AN EDIT-MODE SCENE CANNOT SHOW: the crowd's frames and the craft do not
    /// move, a jump pad draws only where its parts are stood by hand here (`JumpPad` builds and
    /// animates its look in Play), and nothing is static-batched, so the draw-call count of
    /// these renders is not the game's. Tests/PlayMode/ArenaPerfProbe.cs measures that in Play.
    ///
    /// Batch: TumbangPreso.EditorTools.MapKit.ArenaSceneBuilder.RunReview
    /// </summary>
    public static partial class ArenaSceneBuilder
    {
        public const string LogFolder = "Logs/arena/unity";
        private const float EyeHeight = 1.6f;

        [MenuItem("Tumbang Preso/Sample Map/Render Arena Review")]
        public static void ReviewFromMenu() { Review(NextReviewFolder()); Open(); }

        /// <summary>Build, then render the review. ⚠️ A batch run must EXIT with a failure code
        /// on an exception, or the caller reads a silent zero and a stale scene.</summary>
        public static void RunReview()
        {
            try { Build(); Review(NextReviewFolder()); }
            catch (Exception e) { Debug.LogError(Tag + "FAILED: " + e); EditorApplication.Exit(1); return; }
            EditorApplication.Exit(0);
        }

        private static string NextReviewFolder()
        {
            int v = 1;
            while (Directory.Exists($"{LogFolder}/v{v}")) v++;
            return $"{LogFolder}/v{v}";
        }

        private struct Shot
        {
            public string Name;
            public Vector3 At, Look, Up;
            /// <summary>Vertical, degrees.</summary>
            public float Fov;
            /// <summary>Through the game camera's range and ink, not a free camera's.</summary>
            public bool Game;
            /// <summary>The layout the stage stands in for it, or -1 for the one the scene was saved in.</summary>
            public int Layout;
        }

        private static Shot Free(string name, Vector3 at, Vector3 look, float fov) =>
            new Shot { Name = name, At = at, Look = look, Up = Vector3.up, Fov = fov, Layout = -1 };

        private static Shot Play(string name, Vector3 at, Vector3 look, float fov) =>
            new Shot { Name = name, At = at, Look = look, Up = Vector3.up, Fov = fov, Game = true, Layout = -1 };

        public static void Review(string output)
        {
            Directory.CreateDirectory(output);
            // The materials by name, for the jump pads stood in below (a menu run has none in memory).
            if (ArenaArtPlacer.Exists) ArenaArtPlacer.Prepare();

            foreach (bool withLook in new[] { true, false })
            {
                // Opened fresh for each pass, so the look's changes to the light and RenderSettings never carry over.
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var stage = Object.FindFirstObjectByType<ArenaStage>();
                if (stage == null) throw new InvalidOperationException("No ArenaStage in " + ScenePath);

                int saved = Array.FindIndex(stage.Layouts, l => l != null && l.Colliders != null && l.Colliders.activeSelf);
                if (saved < 0) saved = 0;
                StandIns();

                var camera = new GameObject("Arena review witness").AddComponent<Camera>();
                camera.enabled = false;
                camera.nearClipPlane = 0.05f;
                WorldLookPresentation look = null;
                WorldOutline outline = null;
                if (withLook)
                {
                    camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
                    outline = camera.gameObject.AddComponent<WorldOutline>();
                    outline.PrototypeEnabled = true;
                    try
                    {
                        camera.gameObject.AddComponent<WorldLookCamera>();
                        var sun = Object.FindObjectsByType<Light>().FirstOrDefault(l => l.type == LightType.Directional);
                        var sceneRoot = GameObject.Find(SceneName);
                        if (sceneRoot != null) look = WorldLookPresentation.InstallPreview(sceneRoot.transform, 0f, sun);
                        Debug.Log(Tag + (look != null ? "Review wears the look " + look.Look.Map : "No world look found for the review"));
                    }
                    catch (Exception e) { Debug.LogWarning(Tag + "World look preview failed: " + e.Message); }
                }

                int shown = saved;
                foreach (var s in Shots(stage, saved))
                {
                    int want = s.Layout >= 0 ? s.Layout : saved;
                    if (want != shown) { Show(stage, want); shown = want; }

                    const int w = 1600, h = 900;
                    camera.farClipPlane = s.Game ? PlayFar : FreeFar * 2.4f;
                    // The game camera's ink fades where `MapCameraRange` says; a free camera's keeps the fog's.
                    if (outline != null) outline.SetFade(s.Game ? InkFadeStart : -1.0f, s.Game ? InkFadeEnd : -1.0f);
                    camera.fieldOfView = s.Fov;
                    camera.transform.SetPositionAndRotation(s.At, Quaternion.LookRotation(s.Look - s.At, s.Up));
                    var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 4 };
                    rt.Create();
                    camera.targetTexture = rt;
                    var clock = System.Diagnostics.Stopwatch.StartNew();
                    camera.Render();
                    var previous = RenderTexture.active;
                    var display = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                    bool write = GL.sRGBWrite;
                    GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
                    Graphics.Blit(rt, display);
                    GL.sRGBWrite = write;
                    RenderTexture.active = display;
                    var image = new Texture2D(w, h, TextureFormat.RGB24, false);
                    image.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                    image.Apply();
                    Debug.Log($"{Tag}Shot {s.Name} ({(withLook ? "look" : "authored")}): {clock.Elapsed.TotalMilliseconds:F1} ms render and readback");
                    File.WriteAllBytes(Path.Combine(output, $"arena_{s.Name}_{(withLook ? "look" : "authored")}.png"), image.EncodeToPNG());
                    RenderTexture.active = previous;
                    camera.targetTexture = null;
                    RenderTexture.ReleaseTemporary(display);
                    rt.Release();
                    Object.DestroyImmediate(rt);
                    Object.DestroyImmediate(image);
                }

                if (look != null) Object.DestroyImmediate(look.gameObject);
                Object.DestroyImmediate(camera.gameObject);
            }

            // Reopened and never saved: the stand-ins and the layouts shown are gone with it.
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log(Tag + "Review renders written to " + output);
        }

        private static List<Shot> Shots(ArenaStage stage, int saved)
        {
            float can = stage.Layouts.Length > 0 && stage.Layouts[saved] != null ? stage.Layouts[saved].CanHeight : 0.0f;
            float eye = can + EyeHeight;
            var shots = new List<Shot>();

            // A player's eye by the can: two metres behind it, looking over it and out along the bearing.
            string[] compass = { "n", "ne", "e", "se", "s", "sw", "w", "nw" };
            for (int i = 0; i < compass.Length; i++)
            {
                Vector3 out_ = ArenaStageMesh.Direction(45.0f * i);
                shots.Add(Play("eye_" + compass[i], -out_ * 2.0f + Vector3.up * eye, out_ * 120.0f + Vector3.up * 28.0f, CameraSystem.CameraRig.FppFieldOfView));
            }

            // The third-person camera, about where the rig holds it: behind an attacker on the +z
            // marks looking at the can, and behind the taya at (0, 0, -2.5) looking at them.
            shots.Add(Play("game_attacker", new Vector3(0.0f, 2.6f, 13.2f), new Vector3(0.0f, can + 0.6f, 0.0f), CameraSystem.CameraRig.TppFieldOfView));
            shots.Add(Play("game_taya", new Vector3(0.0f, can + 2.6f, -6.4f), new Vector3(0.0f, 1.0f, 9.0f), CameraSystem.CameraRig.TppFieldOfView));
            shots.Add(Play("shaft_down", new Vector3(10.0f, 2.0f, -10.0f), new Vector3(2.0f, -70.0f, -2.0f), CameraSystem.CameraRig.FppFieldOfView));

            shots.Add(Free("upper_stand_top_row", new Vector3(0.0f, 59.6f, -174.0f), new Vector3(0.0f, 4.0f, 10.0f), 60.0f));
            shots.Add(Free("plaza", new Vector3(150.0f, 16.0f, -190.0f), new Vector3(60.0f, 20.0f, -120.0f), 60.0f));
            shots.Add(Free("air", new Vector3(330.0f, 300.0f, -470.0f), Vector3.zero, 50.0f));
            shots.Add(Free("air_far", new Vector3(900.0f, 420.0f, -1300.0f), new Vector3(0.0f, -40.0f, 0.0f), 42.0f));
            shots.Add(Free("under", new Vector3(220.0f, -200.0f, -320.0f), new Vector3(0.0f, -30.0f, 0.0f), 55.0f));

            // Each layout from straight above, north (+z) up the picture, the whole stage in frame.
            float height = Mathf.Max(40.0f, stage.Radius * 2.9f);
            for (int l = 0; l < stage.Layouts.Length; l++)
                shots.Add(new Shot
                {
                    Name = "layout_" + (stage.Layouts[l] != null ? stage.Layouts[l].Name : l.ToString()),
                    At = new Vector3(0.0f, height, 0.0f), Look = Vector3.zero, Up = Vector3.forward, Fov = 42.0f, Layout = l,
                });
            return shots;
        }

        /// <summary>Stand the stage in a layout, as `ArenaStage` does at rest: that layout's
        /// solids and features on, every hologram off. (The colliders are not touched: nothing
        /// here collides.)</summary>
        private static void Show(ArenaStage stage, int layout)
        {
            // Travel 1 of the way from a layout to itself: every piece stands where that layout has it.
            stage.PoseTravel(layout, layout, 1.0f, 0.0f);
            for (int l = 0; l < stage.Layouts.Length; l++)
            {
                var features = stage.Layouts[l] != null ? stage.Layouts[l].Features : null;
                if (features != null) features.SetActive(l == layout);
            }
        }

        /// <summary>
        /// What only exists in Play, stood in for a still: the crowd's seats (its meshes are
        /// built when the scene loads) and each jump pad's parts (the pad builds and animates
        /// them itself), placed at about the middle of their loop. Never saved.
        /// </summary>
        private static void StandIns()
        {
            var crowd = Object.FindFirstObjectByType<ArenaCrowd>();
            if (crowd != null) crowd.Rebuild();

            foreach (var pad in Object.FindObjectsByType<JumpPad>(FindObjectsInactive.Include))
            {
                if (pad.Model == null) continue;

                var model = (GameObject)PrefabUtility.InstantiatePrefab(pad.Model, pad.transform);
                float fit = pad.Radius / Mathf.Max(0.01f, pad.ModelHalfSize);
                model.transform.localScale = new Vector3(fit, fit, fit);
                ArenaArtPlacer.Dress(model, false);
                foreach (var part in model.GetComponentsInChildren<Transform>())
                {
                    if (part.name.StartsWith(pad.PartPrefix + "cushion", StringComparison.Ordinal)) part.localPosition = Vector3.up * 0.02f;
                    else if (part.name.StartsWith(pad.PartPrefix + "ring", StringComparison.Ordinal)) part.localPosition = Vector3.up * 0.5f;
                    else if (part.name.StartsWith(pad.PartPrefix + "chevron", StringComparison.Ordinal)) part.localPosition = Vector3.up * 1.05f;
                }
            }
        }
    }
}
