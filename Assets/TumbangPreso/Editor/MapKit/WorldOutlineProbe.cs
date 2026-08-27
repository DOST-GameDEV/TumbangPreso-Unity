using System.IO;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// Photographs the world outline prototype OFF and ON from one angle, so the retry of the
    /// 2026-07-29 revert can be judged from a picture instead of from an argument.
    ///
    /// ⚠️⚠️ THE PAIR IS THE POINT. A single frame of an outlined street cannot answer "is this
    /// better", because the question is always "better than what". Both frames come from the same
    /// camera, the same grade and the same seed, and differ only in `PrototypeEnabled`, so any
    /// difference between them IS the feature.
    ///
    /// ⚠️ `EnvColourPass.Apply()` RUNS FIRST, and without it the shots are a lie. That pass gives
    /// every map its seeded facade palette and warm-neutral road, and it runs from `Start()`,
    /// which never happens in an edit-mode capture. `IlalimNgTulayShowcaseProbe` records four
    /// renders that were taken without it and showed raw `.mtl` colours.
    ///
    /// ⚠️ AND THE GRADE IS ADOPTED FROM THE SCENE, for the reason that probe records at length:
    /// a capture must grade the way the match grades or it is not evidence. This map's exposure
    /// has been wrong before, and fifteen frames a set were taken through the wrong one.
    ///
    /// ⚠️⚠️ BUMP `Version` ON EVERY CAPTURE. `CLAUDE.md` § 6.1: chat clients cache by filename, so
    /// overwriting a render leaves the previous image on screen and the whole review is conducted
    /// against a picture that is no longer on disk.
    /// </summary>
    public static class WorldOutlineProbe
    {
        private const string OutDir = "Logs/shots-world-outline";
        private const int ShotWidth = 1280;
        private const int ShotHeight = 720;

        /// <summary>Bump on every capture. See the class note.</summary>
        private const string Version = "v2";

        [MenuItem("Tumbang Preso/Capture World Outline A-B")]
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

            // The same two angles the showcase probe uses for the shots that matter: one wide
            // enough to show whether the street reads as drawn, and one at eye height down the
            // lane, which is where a player actually is.
            Shot("overview", new Vector3(6.2f, 7.0f, -15.2f), Quaternion.Euler(18.0f, -13.0f, 0.0f), 66.0f);
            Shot("eye_level", new Vector3(-2.0f, 1.65f, 5.5f), Quaternion.Euler(6.0f, -74.0f, 0.0f), 62.0f);

            Debug.Log($"[WorldOutlineProbe] wrote 4 frames to {OutDir}");
            return true;
        }

        private static void Shot(string name, Vector3 pos, Quaternion rot, float fov)
        {
            Capture(name, pos, rot, fov, outlineOn: false);
            Capture(name, pos, rot, fov, outlineOn: true);
        }

        private static void Capture(string name, Vector3 pos, Quaternion rot, float fov, bool outlineOn)
        {
            var camGo = new GameObject($"WorldOutlineProbeCam_{name}_{(outlineOn ? "on" : "off")}");
            camGo.transform.SetPositionAndRotation(pos, rot);

            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.05f;

            // ⚠️ A SHORTER FAR PLANE THAN THE MATCH CAMERA'S DEFAULT, ON PURPOSE. `WorldOutline`
            // reads `_CameraDepthNormalsTexture`, which packs depth into 16 bits spread evenly
            // over the frustum: at the 1000 m default that is a 15 mm step, and the edge test
            // speckles on near surfaces. 200 m divides the error by five and is still far past
            // anything on either arena. This is the same recommendation the pass carries in its
            // own comments, applied here so the capture shows the effect rather than the artefact.
            cam.farClipPlane = 200.0f;

            // ⚠️⚠️ REQUESTED HERE AS WELL AS INSIDE THE PASS, AND BOTH ARE NEEDED. `WorldOutline`
            // asks for `DepthNormals` from `LateUpdate`, which Unity does not tick outside play
            // mode, and from `OnPreRender`, which runs after Unity has already decided which
            // textures to generate for the frame it is about to draw. A camera that is built and
            // rendered in one go therefore receives no `_CameraDepthNormalsTexture` at all unless
            // the CALLER asks before `Render()`.
            //
            // ⚠️ THE v1 CAPTURE IS THE EVIDENCE. Off and on came back visually identical, differing
            // by roughly 30 bytes of PNG compression noise, because the edge test was sampling a
            // texture that was never generated and correctly found no edges in it.
            cam.depthTextureMode |= DepthTextureMode.DepthNormals;

            // ⚠️ THE OUTLINE GOES ON BEFORE THE GRADE, which is the order `CameraRig` builds and
            // the order the pass argues for: the hull's ink is geometry and is already tonemapped,
            // so the world's ink has to enter the same curve to match it. `WorldOutline` pins
            // itself to the opaque stage with `[ImageEffectOpaque]`, so this is belt and braces.
            var outline = camGo.AddComponent<WorldOutline>();
            outline.PrototypeEnabled = outlineOn;

            camGo.AddComponent<ColourGrade>().AdoptFromScene();

            // ⚠️ 4x, MATCHING EVERY OTHER PROBE IN THE REPO. Not because the game runs at 4x, but
            // because a capture that aliases cannot be read for a feature made ENTIRELY of thin
            // high-contrast lines. Judge the outline's shape here and its aliasing in the player.
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

            string path = Path.Combine(OutDir, $"outline_{name}_{(outlineOn ? "on" : "off")}_{Version}.png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Debug.Log($"[WorldOutlineProbe] {path} ({new FileInfo(path).Length / 1024} KB)");

            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(camGo);
        }
    }
}
