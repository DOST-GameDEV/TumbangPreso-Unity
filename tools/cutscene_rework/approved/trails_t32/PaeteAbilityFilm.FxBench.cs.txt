using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// ⚠️ THE EFFECTS BENCH (2026-10-10). The owner, of the first effects pass on the ultimate cutscenes: "none of the
    /// effects you added actually look like the references i gave you. can you actually look at the references and give me
    /// your best shot at recreating the ones u see". Those effects had only ever been looked at inside a hero's cutscene:
    /// a pale floor, a body in front, a camera that cuts. A reference is none of that. It is ONE effect, alone, on a dark
    /// neutral floor with a faint grid, from one fixed camera, and that is the only setting in which an effect of ours
    /// can honestly be laid beside it.
    ///
    /// So this films one effect of the study (`Runtime/Visual/FxStudy`, `FxStudyEffect`) exactly so: no character, no
    /// map, no light but the effect's own, the camera the effect itself names as its reference's (`FxStudyEffect.Shot`),
    /// played from its age float on a fixed clock of sixty frames a second.
    ///
    /// Request `fxbench <effect> <tag>` (`tools/cutscene_rework/fxbench.sh` does the whole cycle). Writes every frame to
    /// `Logs/paete-ability-film/fxbench_<effect>_<tag>_frames/` and then `done_fxbench_<effect>_<tag>.txt`, or
    /// `failed_fxbench_<effect>_<tag>.txt` with the exception. `tools/cutscene_rework/fxcompare.py` lays the frames beside
    /// the reference's at the same moments.
    ///
    /// ⚠️ NOTHING BLOOMS HERE, and nothing blooms in a cutscene's camera either, so a film from this bench shows an
    /// effect's glow exactly as far as the effect drew it itself. That is deliberate: a halo that only exists under a
    /// post effect would be a halo the game does not have.
    /// </summary>
    public static partial class PaeteAbilityFilm
    {
        private const int BenchWidth = 960, BenchHeight = 540, BenchRate = 60;

        // ⚠️ THE BENCH BLOOMS, THE GAME DOES NOT. The scene is drawn into a floating-point picture; what is over the
        // threshold is halved five times (each a 13-tap blur), doubled back up adding each level (9-tap tent), added to
        // the scene and rolled off to white: twelve full-screen passes, the biggest of them at half the picture's size.
        // Whether any game camera gets this is the owner's decision; nothing here touches one.
        private const int BloomLevels = 5;
        private const float BloomThreshold = 1.0f, BloomKnee = 0.30f, BloomIntensity = 0.42f, BloomWhite = 1.6f;

        private static void Bloom(RenderTexture scene, RenderTexture output, Material material, float intensity)
        {
            material.SetFloat("_Threshold", BloomThreshold); material.SetFloat("_Knee", BloomKnee);
            material.SetFloat("_Intensity", intensity); material.SetFloat("_White", BloomWhite);
            var levels = new RenderTexture[BloomLevels];
            int w = scene.width / 2, h = scene.height / 2;
            for (int i = 0; i < BloomLevels; i++)
            {
                levels[i] = RenderTexture.GetTemporary(Mathf.Max(2, w), Mathf.Max(2, h), 0, RenderTextureFormat.ARGBHalf);
                levels[i].filterMode = FilterMode.Bilinear;
                Graphics.Blit(i == 0 ? scene : levels[i - 1], levels[i], material, i == 0 ? 0 : 1);
                w /= 2; h /= 2;
            }
            for (int i = BloomLevels - 1; i > 0; i--) Graphics.Blit(levels[i], levels[i - 1], material, 2);
            material.SetTexture("_BloomTex", levels[0]);
            Graphics.Blit(scene, output, material, 3);
            foreach (var level in levels) RenderTexture.ReleaseTemporary(level);
        }

        private static void RunFxBench(string effect, string tag)
        {
            Directory.CreateDirectory(OutDir);
            // Both words end up in file names, and they came from a file anyone can write.
            string Clean(string word, string fallback)
            {
                word = new string((word ?? "").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray()).ToLowerInvariant();
                return word.Length == 0 ? fallback : word;
            }
            effect = Clean(effect, "none"); tag = Clean(tag, "v00");
            string named = "fxbench_" + effect + "_" + tag;
            string donePath = Path.Combine(OutDir, "done_" + named + ".txt"), failedPath = Path.Combine(OutDir, "failed_" + named + ".txt");
            if (File.Exists(donePath)) File.Delete(donePath);
            if (File.Exists(failedPath)) File.Delete(failedPath);
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                File.WriteAllText(failedPath, "The editor is in Play mode. The bench only films outside it.");
                return;
            }

            var scene = EditorSceneManager.NewPreviewScene();
            var owned = new List<Object>();
            RenderTexture rt = null;
            FxStudyEffect fx = null;
            try
            {
                GameObject In(GameObject go) { SceneManager.MoveGameObjectToScene(go, scene); return go; }

                var stage = In(new GameObject("~FxBenchStage"));
                fx = FxStudyEffect.Create(effect, stage.transform);
                if (fx == null) throw new ArgumentException("No effect called '" + effect + "'. One of: " + string.Join(", ", FxStudyEffect.Names));

                var cam = In(new GameObject("~FxBenchCamera")).AddComponent<Camera>();
                cam.scene = scene; cam.enabled = false;
                cam.nearClipPlane = 0.05f; cam.farClipPlane = 300f;
                // A tag that begins `nobloom` films with the bloom off: what the GAME's cameras would show today.
                bool bloom = !tag.StartsWith("nobloom");
                cam.allowHDR = bloom;
                Shader.SetGlobalFloat("_FxStudyBloom", bloom ? 1f : 0f);
                cam.clearFlags = CameraClearFlags.SolidColor;

                // The floor. Its far colour is the backdrop's, so no horizon line is drawn across the picture.
                var floorShader = Shader.Find("TumbangPreso/FxStudyFloor");
                if (floorShader == null) throw new InvalidOperationException("The bench floor's shader (TumbangPreso/FxStudyFloor) is missing.");
                var floor = new Material(floorShader) { hideFlags = HideFlags.HideAndDontSave };
                owned.Add(floor);
                bool chequered = fx.ChequeredFloor;
                Color first = chequered ? new Color(0.36f, 0.36f, 0.37f) : fx.FloorColour, second = new Color(0.27f, 0.27f, 0.28f);
                float dim = chequered ? 0.60f : fx.FloorDim;
                floor.SetColor("_A", first); floor.SetColor("_B", second);
                floor.SetColor("_LineColor", chequered ? new Color(0.13f, 0.13f, 0.14f) : fx.FloorLine);
                floor.SetFloat("_Chequer", chequered ? 1f : 0f); floor.SetFloat("_Cell", fx.FloorCell);
                floor.SetVector("_Offset", new Vector4(fx.FloorOffset.x, fx.FloorOffset.y, 0f, 0f));
                floor.SetFloat("_LineWidth", chequered ? 0.022f : 0.012f);
                floor.SetFloat("_Dim", dim); floor.SetFloat("_DimAt", chequered ? 26f : fx.FloorDimAt);
                cam.backgroundColor = (chequered ? Color.Lerp(first, second, 0.5f) : first) * dim;
                var ground = In(GameObject.CreatePrimitive(PrimitiveType.Plane));
                ground.name = "~FxBenchFloor";
                Object.DestroyImmediate(ground.GetComponent<Collider>());
                ground.transform.localScale = new Vector3(30f, 1f, 30f);
                ground.GetComponent<MeshRenderer>().sharedMaterial = floor;

                rt = new RenderTexture(BenchWidth, BenchHeight, 24, bloom ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                var bloomShader = Shader.Find("Hidden/TumbangPreso/FxStudyBloom");
                if (bloom && bloomShader == null) throw new InvalidOperationException("The bench bloom's shader (Hidden/TumbangPreso/FxStudyBloom) is missing.");
                Material bloomMaterial = bloom ? new Material(bloomShader) { hideFlags = HideFlags.HideAndDontSave } : null;
                if (bloomMaterial != null) owned.Add(bloomMaterial);
                var shown = new RenderTexture(BenchWidth, BenchHeight, 0, RenderTextureFormat.ARGB32);
                owned.Add(shown);
                var clock = new System.Diagnostics.Stopwatch();
                cam.targetTexture = rt;
                var shot = new Texture2D(BenchWidth, BenchHeight, TextureFormat.RGB24, false);
                owned.Add(shot);
                string frames = Path.Combine(OutDir, named + "_frames");
                if (Directory.Exists(frames)) foreach (string old in Directory.GetFiles(frames)) File.Delete(old);
                Directory.CreateDirectory(frames);

                float seconds = fx.Seconds;
                int count = Mathf.CeilToInt(seconds * BenchRate) + 1;
                for (int f = 0; f < count; f++)
                {
                    float age = Mathf.Min(seconds, f / (float)BenchRate);
                    fx.Shot(age, out var eye, out var focus, out float fov);
                    cam.fieldOfView = fov;
                    cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(focus - eye, Vector3.up));
                    fx.Sample(age, eye);
                    cam.Render();
                    if (bloom) { clock.Start(); Bloom(rt, shown, bloomMaterial, fx.BloomIntensity); clock.Stop(); }
                    var keep = RenderTexture.active; RenderTexture.active = bloom ? shown : rt;
                    shot.ReadPixels(new Rect(0, 0, BenchWidth, BenchHeight), 0, 0); shot.Apply();
                    RenderTexture.active = keep;
                    File.WriteAllBytes(Path.Combine(frames, "f" + f.ToString("000") + ".png"), shot.EncodeToPNG());
                }
                File.WriteAllText(donePath, count + " frames at " + BenchRate + " a second, " + seconds.ToString("0.00") + " s, in " + frames.Replace('\\', '/') + "\n"
                                            + (bloom ? "bloom on: " + (clock.Elapsed.TotalMilliseconds / count).ToString("0.000") + " ms a frame to SUBMIT its passes at " + BenchWidth + "x" + BenchHeight + " (not GPU time)\n" : "bloom off\n"));
                Debug.Log("[PaeteAbilityFilm] " + named + " written to " + OutDir);
            }
            catch (Exception failure)
            {
                File.WriteAllText(failedPath, failure.ToString());
                Debug.LogError("[PaeteAbilityFilm] " + failure);
            }
            finally
            {
                Shader.SetGlobalFloat("_FxStudyBloom", 0f);
                try { fx?.Dispose(); } catch (Exception e) { Debug.LogWarning("[PaeteAbilityFilm] bench dispose: " + e.Message); }
                if (rt != null) { RenderTexture.active = null; rt.Release(); Object.DestroyImmediate(rt); }
                foreach (var thing in owned) if (thing != null) Object.DestroyImmediate(thing);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
