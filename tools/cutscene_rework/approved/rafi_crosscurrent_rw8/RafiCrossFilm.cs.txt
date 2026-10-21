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
    /// The rework of Ilyas's CROSSCURRENT in the wall's water (`FxStudyRafiCross`), filmed from the stream's four views at
    /// once, as the stream was (`RunRafiStreamViews`). Its own request file, `Temp/rafi-cross-film.request`, one line:
    /// `crossviews<tag>` films it whole, `crossviewsspent<tag>` spends it 0.55 s in, `measurecross<tag>` measures its cost.
    /// Frames go to `Logs/paete-ability-film/fxbench_<request>_frames/`, then `done_fxbench_<request>.txt` or `failed_...`.
    /// </summary>
    [InitializeOnLoad]
    internal static class RafiCrossFilmPoll
    {
        private const string Request = "Temp/rafi-cross-film.request";
        private static double _next;
        static RafiCrossFilmPoll() { EditorApplication.update += Poll; }

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 1.0;
            if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            AssetDatabase.Refresh();
            if (EditorApplication.isCompiling) return;
            string line = File.ReadAllText(Request).Trim();
            File.Delete(Request);
            if (line.StartsWith("measurecross")) PaeteAbilityFilm.MeasureRafiCross(line);
            else PaeteAbilityFilm.RunRafiCrossViews(line);
        }
    }

    public static partial class PaeteAbilityFilm
    {
        internal static void RunRafiCrossViews(string request)
        {
            Directory.CreateDirectory(OutDir);
            string tag = new string(request.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
            string named = "fxbench_" + tag;
            string donePath = Path.Combine(OutDir, "done_" + named + ".txt"), failedPath = Path.Combine(OutDir, "failed_" + named + ".txt");
            if (File.Exists(donePath)) File.Delete(donePath);
            if (File.Exists(failedPath)) File.Delete(failedPath);
            var scene = EditorSceneManager.NewPreviewScene();
            var owned = new List<Object>();
            FxStudyRafiCross fx = null;
            try
            {
                GameObject In(GameObject go) { SceneManager.MoveGameObjectToScene(go, scene); return go; }
                var stage = In(new GameObject("~CrossStage"));
                // A tag with "day" in it stands it on a light warm floor in daylight, with the match's light share (no bloom either way).
                bool day = tag.Contains("day");
                var look = FxStudyRafiWall.Look.Bench; if (day) look.Light = RafiWallCurtain.DayLight;
                fx = new FxStudyRafiCross(stage.transform, look, tag.Contains("spent") ? 0.55f : -1f, 0f, 0);
                Color floorA = day ? new Color(0.62f, 0.56f, 0.48f) : fx.FloorColour, floorLine = day ? new Color(0.55f, 0.49f, 0.42f) : fx.FloorLine;
                var floorShader = Shader.Find("TumbangPreso/FxStudyFloor");
                var floor = new Material(floorShader) { hideFlags = HideFlags.HideAndDontSave }; owned.Add(floor);
                floor.SetColor("_A", floorA); floor.SetColor("_B", floorA); floor.SetColor("_LineColor", floorLine);
                floor.SetFloat("_Chequer", 0f); floor.SetFloat("_Cell", 1f); floor.SetFloat("_LineWidth", 0.012f); floor.SetFloat("_Dim", day ? 0.10f : 0.45f); floor.SetFloat("_DimAt", 14f);
                var ground = In(GameObject.CreatePrimitive(PrimitiveType.Plane)); Object.DestroyImmediate(ground.GetComponent<Collider>());
                ground.transform.localScale = new Vector3(30f, 1f, 30f); ground.GetComponent<MeshRenderer>().sharedMaterial = floor;
                var sunGo = In(new GameObject("~CrossSun")); var sun = sunGo.AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = day ? 1.3f : 1.0f;
                sunGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                GameObject Body(string id, Vector3 at, float yaw)
                {
                    var art = RosterBook.Load()?.FindPersonArt(id);
                    if (art == null || art.Model == null) return null;
                    var body = In((GameObject)Object.Instantiate(art.Model));
                    body.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw + CharacterVisual.PersonModelYaw, 0f));
                    body.transform.localScale = Vector3.one * CharacterVisual.PersonScale;
                    ToonSkin.Apply(body, ToonSkin.PersonOutlineWidth, art.Palette);
                    foreach (var animator in body.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
                    return body;
                }
                var caster = Body("rafi", new Vector3(0f, 0f, -0.35f), 0f);
                var thrower = Body("dante", new Vector3(0.4f, 0f, 8.0f), 180f);
                var cam = In(new GameObject("~CrossCamera")).AddComponent<Camera>();
                cam.scene = scene; cam.enabled = false; cam.nearClipPlane = 0.05f; cam.farClipPlane = 200f;
                cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = day ? new Color(0.86f, 0.80f, 0.70f) : fx.FloorColour * 0.45f; cam.allowHDR = false;
                Shader.SetGlobalFloat("_FxStudyBloom", 0f);
                var target = new RenderTexture(GaleCellWidth, GaleCellHeight, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 }; owned.Add(target);
                var cell = new Texture2D(GaleCellWidth, GaleCellHeight, TextureFormat.RGB24, false); owned.Add(cell);
                var sheet = new Texture2D(GaleCellWidth * 2, GaleCellHeight * 2, TextureFormat.RGB24, false); owned.Add(sheet);
                cam.targetTexture = target;
                string frames = Path.Combine(OutDir, named + "_frames");
                if (Directory.Exists(frames)) foreach (string old in Directory.GetFiles(frames)) File.Delete(old);
                Directory.CreateDirectory(frames);
                int count = Mathf.CeilToInt(fx.Seconds * BenchRate) + 1;
                for (int f = 0; f < count; f++)
                {
                    float age = f / (float)BenchRate;
                    for (int view = 0; view < 4; view++)
                    {
                        FxStudyRafiStream.View(view, out var eye, out var focus, out float fov);
                        cam.fieldOfView = fov; cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(focus - eye, view == 3 ? Vector3.forward : Vector3.up));
                        if (caster != null) caster.SetActive(view != 1);
                        if (thrower != null) thrower.SetActive(view != 2);
                        fx.Sample(age, eye);
                        cam.Render();
                        var keep = RenderTexture.active; RenderTexture.active = target;
                        cell.ReadPixels(new Rect(0, 0, GaleCellWidth, GaleCellHeight), 0, 0); cell.Apply();
                        RenderTexture.active = keep;
                        sheet.SetPixels((view % 2) * GaleCellWidth, (1 - view / 2) * GaleCellHeight, GaleCellWidth, GaleCellHeight, cell.GetPixels());
                    }
                    sheet.Apply();
                    File.WriteAllBytes(Path.Combine(frames, "f" + f.ToString("000") + ".png"), sheet.EncodeToPNG());
                }
                File.WriteAllText(donePath, count + " frames at " + BenchRate + " a second, " + fx.Seconds.ToString("0.00") + " s\nviews: side (top left), behind the caster (top right), a thrower's looking back down its path (bottom left), above (bottom right); no bloom\n");
            }
            catch (Exception failure) { File.WriteAllText(failedPath, failure.ToString()); Debug.LogError("[RafiCrossFilm] " + failure); }
            finally
            {
                fx?.Dispose();
                RenderTexture.active = null;
                foreach (var thing in owned) { if (thing == null) continue; if (thing is RenderTexture texture) texture.Release(); Object.DestroyImmediate(thing); }
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        /// <summary>Its cost, as the stream's was measured: eight at once posed 2,000 times each, managed memory counted, one
        /// counted for draw calls and triangles.</summary>
        internal static void MeasureRafiCross(string request)
        {
            Directory.CreateDirectory(OutDir);
            string tag = new string(request.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
            string path = Path.Combine(OutDir, "done_" + tag + ".txt");
            var scene = EditorSceneManager.NewPreviewScene();
            var report = new System.Text.StringBuilder();
            var made = new List<FxStudyRafiCross>();
            try
            {
                var stage = new GameObject("~CrossMeasure"); SceneManager.MoveGameObjectToScene(stage, scene);
                var look = FxStudyRafiWall.Look.Bench; look.Light = RafiWallCurtain.DayLight;
                for (int i = 0; i < 8; i++) made.Add(new FxStudyRafiCross(stage.transform, look, -1f, i * 0.13f, 0));
                var eye = new Vector3(6f, 2f, -2f);
                foreach (var c in made) c.Pose(0.5f, -1f, eye);
                const int poses = 2000;
                long before = GC.GetAllocatedBytesForCurrentThread();
                var clock = System.Diagnostics.Stopwatch.StartNew();
                for (int k = 0; k < poses; k++)
                {
                    float t = (k % 120) / 120f * (FxStudyRafiCross.Life + FxStudyRafiCross.Die);
                    foreach (var c in made) c.Pose(t, -1f, eye);
                }
                clock.Stop();
                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                report.AppendLine("posing: " + (clock.Elapsed.TotalMilliseconds * 1000.0 / (poses * (double)made.Count)).ToString("0.00") + " microseconds per cast per frame, measured in the editor outside Play");
                report.AppendLine("managed memory allocated while posing: " + allocated + " bytes over " + (poses * made.Count) + " poses");
                foreach (float at in new[] { 0.6f, FxStudyRafiCross.Life + 0.2f })
                {
                    made[0].Pose(at, -1f, eye);
                    int calls = 0, triangles = 0;
                    foreach (var renderer in made[0].Root.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        var filter = renderer.GetComponent<MeshFilter>();
                        if (!renderer.enabled || filter == null || filter.sharedMesh == null) continue;
                        calls++; triangles += (int)(filter.sharedMesh.GetIndexCount(0) / 3);
                    }
                    report.AppendLine("one cast at " + at.ToString("0.00") + " s: " + calls + " draw calls, " + triangles + " triangles");
                }
                var materials = new HashSet<Material>(); var meshes = new HashSet<Mesh>();
                foreach (var c in made)
                    foreach (var renderer in c.Root.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        if (renderer.sharedMaterial != null) materials.Add(renderer.sharedMaterial);
                        var filter = renderer.GetComponent<MeshFilter>();
                        if (filter != null && filter.sharedMesh != null) meshes.Add(filter.sharedMesh);
                    }
                report.AppendLine("all eight share " + materials.Count + " materials and " + meshes.Count + " meshes");
            }
            catch (Exception failure) { report.AppendLine(failure.ToString()); }
            finally
            {
                foreach (var c in made) c.Dispose();
                EditorSceneManager.ClosePreviewScene(scene);
                File.WriteAllText(path, report.ToString());
            }
        }
    }
}
