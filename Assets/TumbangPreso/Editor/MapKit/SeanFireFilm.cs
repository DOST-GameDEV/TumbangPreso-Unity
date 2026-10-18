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
    /// RAGO'S FIRE, FILMED. His STOKE STEP's new look (`FxStudySeanStoke`) on the bench's dark floor, with his real model for
    /// scale, from four eyes at once: a bystander at his front quarter, a bystander side on, his own eyes, and the eyes of
    /// a target standing 6 m in front of him. Nothing blooms: no game camera does.
    ///
    /// ⚠️ IT IS STAGED. His body is a still pose (nothing animates outside Play), slid along his aim by the rule's own sum
    /// (`FxStudySeanStoke.Gone`). No cast runs; the ash puffs are a film's own field of the approved cloud, spawned at the
    /// moments the effect names.
    ///
    /// Request: a line `bench <tag>` in `Temp/sean-fire-film.request` (its own file, so no other film's reader is touched),
    /// or `ping`, answered at once (a save asks first: no answer means the editor is in Play). Frames go to
    /// `Logs/paete-ability-film/fxbench_seanbench_<tag>_frames/`, named like a bench film so `fxvideo.py` and
    /// `fxstills.py` join them, then `done_fxbench_seanbench_<tag>.txt` with what it costs, or `failed_...`.
    /// </summary>
    [InitializeOnLoad]
    internal static class SeanFireFilmPoll
    {
        private const string Request = "Temp/sean-fire-film.request";
        private static double _next;
        static SeanFireFilmPoll() { EditorApplication.update += Poll; }

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 1.0;
            if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            AssetDatabase.Refresh();
            if (EditorApplication.isCompiling) return;
            string[] lines = File.ReadAllLines(Request);
            File.Delete(Request);
            foreach (string line in lines)
            {
                string[] words = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length == 0) continue;
                if (words[0] == "ping") { Directory.CreateDirectory("Logs/paete-ability-film"); File.WriteAllText("Logs/paete-ability-film/done_sean_ping.txt", DateTime.Now.ToString("HH:mm:ss")); continue; }
                PaeteAbilityFilm.RunSeanFire(words[0], words.Length > 1 ? words[1] : "v00", words.Length > 2 ? words[2] : "mix");
            }
        }
    }

    public static partial class PaeteAbilityFilm
    {
        private const int SeanCellWidth = 640, SeanCellHeight = 360;
        private const float SeanTargetAhead = 6.0f;

        internal static void RunSeanFire(string mode, string tag, string look = "mix")
        {
            // `court <tag>`: on a real map in daylight, old beside new (`SeanFireCourt.cs`).
            if (mode == "court") { RunSeanCourt(tag); return; }
            // `throw <tag>`: EMPOWERED THROW on the bench, four views (`SeanThrowFilm.cs`).
            if (mode == "throw") { RunSeanThrow(tag); return; }
            Directory.CreateDirectory(OutDir);
            tag = new string((tag ?? "v00").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray()).ToLowerInvariant();
            if (tag.Length == 0) tag = "v00";
            string named = "fxbench_seanbench_" + tag;
            string donePath = Path.Combine(OutDir, "done_" + named + ".txt"), failedPath = Path.Combine(OutDir, "failed_" + named + ".txt");
            if (File.Exists(donePath)) File.Delete(donePath);
            if (File.Exists(failedPath)) File.Delete(failedPath);
            if (EditorApplication.isPlayingOrWillChangePlaymode) { File.WriteAllText(failedPath, "The editor is in Play mode. The film only runs outside it."); return; }

            var scene = EditorSceneManager.NewPreviewScene();
            var owned = new List<Object>();
            ISeanStokeLook fx = null;
            CloudPuffField puffs = null;
            var report = new System.Text.StringBuilder();
            try
            {
                GameObject In(GameObject go) { SceneManager.MoveGameObjectToScene(go, scene); return go; }
                var cam = In(new GameObject("~SeanFilmCamera")).AddComponent<Camera>();
                cam.scene = scene; cam.enabled = false; cam.allowHDR = false;
                cam.nearClipPlane = 0.06f; cam.farClipPlane = 300f;
                cam.clearFlags = CameraClearFlags.SolidColor;

                // The bench's floor, as `RunFxBench` lays it.
                var floorShader = Shader.Find("TumbangPreso/FxStudyFloor");
                if (floorShader == null) throw new InvalidOperationException("The bench floor's shader is missing.");
                var floor = new Material(floorShader) { hideFlags = HideFlags.HideAndDontSave };
                owned.Add(floor);
                Color first = new Color(0.150f, 0.150f, 0.158f);
                floor.SetColor("_A", first); floor.SetColor("_B", new Color(0.27f, 0.27f, 0.28f));
                floor.SetColor("_LineColor", new Color(0.120f, 0.120f, 0.128f));
                floor.SetFloat("_Chequer", 0f); floor.SetFloat("_Cell", 1f);
                floor.SetVector("_Offset", Vector4.zero); floor.SetFloat("_LineWidth", 0.012f);
                floor.SetFloat("_Dim", 0.45f); floor.SetFloat("_DimAt", 9f);
                cam.backgroundColor = first * 0.45f;
                var ground = In(GameObject.CreatePrimitive(PrimitiveType.Plane));
                Object.DestroyImmediate(ground.GetComponent<Collider>());
                ground.transform.localScale = new Vector3(30f, 1f, 30f);
                ground.GetComponent<MeshRenderer>().sharedMaterial = floor;
                var sun = In(new GameObject("~SeanFilmSun")).AddComponent<Light>();
                sun.type = LightType.Directional; sun.intensity = 1.1f; sun.color = new Color(1f, 0.97f, 0.92f);
                sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
                Shader.SetGlobalFloat("_CharacterSmoothShade", ToonSkin.CharacterSmoothShade);

                Vector3 stand = Vector3.zero, forward = Vector3.forward, right = Vector3.right, targetAt = stand + forward * SeanTargetAhead;
                GameObject Person(string id, Vector3 at, float yaw, out GameObject model)
                {
                    model = null;
                    var holder = In(new GameObject("~SeanFilm-" + id));
                    holder.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
                    var art = RosterBook.Load()?.FindPersonArt(id);
                    if (art == null || art.Model == null) return holder;
                    model = (GameObject)Object.Instantiate(art.Model, holder.transform);
                    model.transform.localPosition = Vector3.zero;
                    model.transform.localRotation = Quaternion.Euler(0f, CharacterVisual.PersonModelYaw, 0f);
                    model.transform.localScale = Vector3.one * CharacterVisual.PersonScale;
                    ToonSkin.Apply(model, ToonSkin.PersonOutlineWidth, art.Palette);
                    foreach (var animator in model.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
                    return holder;
                }
                var sean = Person("sean", stand, 0f, out var seanModel);
                if (seanModel == null) report.AppendLine("Rago's model was not found: there is no body in this film.");
                var target = Person("dante", targetAt, 180f, out _);

                var stage = In(new GameObject("~SeanFilmStage"));
                stage.transform.SetPositionAndRotation(stand, Quaternion.LookRotation(forward));
                // Which look, by name: `s3` and `s4` and `s6` are the kept bench states, `mix` (the default) is the live one.
                bool s3 = look == "s3", curtain = s3 || look == "s4" || look == "curtain";
                if (s3) fx = new FxStudySeanStokeS3(stage.transform);
                else if (curtain || look == "s6" || look == "cel")
                {
                    var keepLook = FxStudySeanStokeS4.Look;
                    FxStudySeanStokeS4.Look = curtain ? FxStudySeanStokeS4.Style.Curtain : FxStudySeanStokeS4.Style.Cel;
                    fx = new FxStudySeanStokeS4(stage.transform);
                    FxStudySeanStokeS4.Look = keepLook;
                }
                else
                {
                    // `horn` is the live class with its wake drawn as the m4 horn instead of the tongues.
                    // `tongues` (m7) and `horn` (m4) are the live class with an earlier wake; anything else is the fireball.
                    var keepTongues = FxStudySeanStoke.Tongues; var keepComet = FxStudySeanStoke.Comet; var keepSoft = FxStudySeanStoke.Soft;
                    FxStudySeanStoke.Soft = look != "c1";
                    // `c2` is c2; `c3a` the smoke as cloud puffs, `c3b` as strands; anything else the live choice.
                    var keepC3 = FxStudySeanStoke.C3; var keepSmoke = FxStudySeanStoke.SmokeLook;
                    FxStudySeanStoke.C3 = look != "c2" && look != "c1";
                    if (look == "c3a") FxStudySeanStoke.SmokeLook = 1; else if (look == "c3b") FxStudySeanStoke.SmokeLook = 2;
                    FxStudySeanStoke.Tongues = look != "horn";
                    FxStudySeanStoke.Comet = look != "horn" && look != "tongues";
                    fx = new FxStudySeanStoke(stage.transform);
                    FxStudySeanStoke.Tongues = keepTongues; FxStudySeanStoke.Comet = keepComet; FxStudySeanStoke.Soft = keepSoft; FxStudySeanStoke.C3 = keepC3; FxStudySeanStoke.SmokeLook = keepSmoke;
                }
                report.AppendLine("look: " + look);
                puffs = new CloudPuffField(stage.transform, "Sean film puffs");
                if (!puffs.Valid) report.AppendLine("the cloud puff shader is missing: no ash in this film");

                var plain = new RenderTexture(SeanCellWidth, SeanCellHeight, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                owned.Add(plain);
                var cell = new Texture2D(SeanCellWidth, SeanCellHeight, TextureFormat.RGB24, false);
                var sheet = new Texture2D(SeanCellWidth * 2, SeanCellHeight * 2, TextureFormat.RGB24, false);
                owned.Add(cell); owned.Add(sheet);
                string frames = Path.Combine(OutDir, named + "_frames");
                if (Directory.Exists(frames)) foreach (string old in Directory.GetFiles(frames)) File.Delete(old);
                Directory.CreateDirectory(frames);

                float lead = 0.20f, seconds = lead + FxStudySeanStoke.Life;
                int count = Mathf.CeilToInt(seconds * BenchRate) + 1;
                bool kicked = false, planted = false;
                var clock = new System.Diagnostics.Stopwatch();
                double worstMs = 0, totalMs = 0, laterMs = 0; float worstMsAt = 0; long bytes = 0; int worstCalls = 0, worstTriangles = 0; float worstAt = 0;
                for (int f = 0; f < count; f++)
                {
                    float t = f / (float)BenchRate - lead;
                    float s = t - FxStudySeanStoke.Brace, gone = FxStudySeanStoke.Gone(s), stopped = s - FxStudySeanStoke.Travel;
                    Vector3 body = stand + forward * gone;
                    sean.transform.position = body;
                    if (curtain && !kicked && t >= FxStudySeanStoke.PushOffAt) { kicked = true; puffs.Spawn(s3 ? FxStudySeanStokeS3.KickPuff(stage.transform.TransformPoint(FxStudySeanStoke.PushOffPoint)) : FxStudySeanStokeS4.KickPuff(stage.transform.TransformPoint(FxStudySeanStoke.PushOffPoint)), t); }
                    if (curtain && !planted && stopped >= 0f) { planted = true; puffs.Spawn(s3 ? FxStudySeanStokeS3.PlantPuff(body) : FxStudySeanStokeS4.PlantPuff(body), t); }
                    puffs.Hold(t);

                    long before = GC.GetAllocatedBytesForCurrentThread();
                    clock.Restart();
                    fx.Pose(t, body + Vector3.up * 1.5f, gone, stopped);
                    clock.Stop();
                    bytes += GC.GetAllocatedBytesForCurrentThread() - before;
                    if (clock.Elapsed.TotalMilliseconds > worstMs) { worstMs = clock.Elapsed.TotalMilliseconds; worstMsAt = t; } totalMs += clock.Elapsed.TotalMilliseconds; if (f > 0) laterMs = Math.Max(laterMs, clock.Elapsed.TotalMilliseconds);
                    fx.Cost(out int calls, out int triangles);
                    if (curtain && puffs.Live) { calls += 2; triangles += 2 * CloudPuffField.Triangles; }
                    if (calls > worstCalls || (calls == worstCalls && triangles > worstTriangles)) { worstCalls = calls; worstTriangles = triangles; worstAt = t; }

                    for (int view = 0; view < 4; view++)
                    {
                        Vector3 eye, aim; float fov;
                        if (view == 0) { eye = stand + forward * 4.4f + right * 3.4f + Vector3.up * 2.0f; aim = stand + forward * 0.9f + Vector3.up * 0.8f; fov = 46f; }
                        else if (view == 1) { eye = stand + forward * 1.0f - right * 6.2f + Vector3.up * 1.3f; aim = stand + forward * 1.0f + Vector3.up * 0.75f; fov = 40f; }
                        else if (view == 2) { eye = body + Vector3.up * 1.50f + forward * 0.12f; aim = body + forward * 8f + Vector3.up * 1.0f; fov = 70f; }
                        else { eye = targetAt + Vector3.up * 1.50f - forward * 0.15f; aim = stand + forward * 1.0f + Vector3.up * 0.9f; fov = 70f; }
                        cam.fieldOfView = fov;
                        cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(aim - eye, Vector3.up));
                        if (seanModel != null) seanModel.SetActive(view != 2);
                        target.SetActive(view != 3);
                        cam.targetTexture = plain;
                        cam.Render();
                        var keep = RenderTexture.active; RenderTexture.active = plain;
                        cell.ReadPixels(new Rect(0, 0, SeanCellWidth, SeanCellHeight), 0, 0); cell.Apply();
                        RenderTexture.active = keep;
                        sheet.SetPixels((view % 2) * SeanCellWidth, (1 - view / 2) * SeanCellHeight, SeanCellWidth, SeanCellHeight, cell.GetPixels());
                    }
                    sheet.Apply();
                    File.WriteAllBytes(Path.Combine(frames, "f" + f.ToString("000") + ".png"), sheet.EncodeToPNG());
                }
                report.AppendLine(count + " frames at " + BenchRate + " a second, " + seconds.ToString("0.00") + " s (" + lead.ToString("0.00") + " s of nothing, the press, the launch " + FxStudySeanStoke.Brace.ToString("0.00") + " s later, planted " + FxStudySeanStoke.Travel.ToString("0.000") + " s after that), in " + frames.Replace('\\', '/'));
                report.AppendLine("each frame: top a bystander at his front quarter and one side on; bottom his own eyes and a target " + SeanTargetAhead.ToString("0") + " m in front of him. No bloom.");
                report.AppendLine("worst moment " + worstAt.ToString("0.00") + " s after the press: " + worstCalls + " draw calls, " + worstTriangles + " triangles (the ash field counted as its two draws and whole mesh)");
                report.AppendLine("posing (CPU, editor, outside Play): worst " + worstMs.ToString("0.000") + " ms at " + worstMsAt.ToString("0.00") + " s (worst after the first frame " + laterMs.ToString("0.000") + " ms), average " + (totalMs / count).ToString("0.000") + " ms a frame; " + bytes + " bytes allocated by Pose over " + count + " frames");
                File.WriteAllText(donePath, report.ToString());
                Debug.Log("[SeanFireFilm] " + named + " written to " + OutDir);
            }
            catch (Exception failure)
            {
                File.WriteAllText(failedPath, report + "\n" + failure);
                Debug.LogError("[SeanFireFilm] " + failure);
            }
            finally
            {
                try { fx?.Release(); } catch (Exception e) { Debug.LogWarning("[SeanFireFilm] release: " + e.Message); }
                try { puffs?.Dispose(); } catch (Exception) { }
                RenderTexture.active = null;
                foreach (var thing in owned)
                {
                    if (thing == null) continue;
                    if (thing is RenderTexture texture) texture.Release();
                    Object.DestroyImmediate(thing);
                }
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
