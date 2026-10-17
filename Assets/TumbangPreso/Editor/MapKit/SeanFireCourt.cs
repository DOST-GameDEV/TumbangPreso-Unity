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
    /// RAGO'S STOKE STEP, FILMED WHERE IT IS PLAYED: Eskinita opened as a PREVIEW scene (the scene the owner has open is not
    /// touched and nothing is saved), lit by the map's own lighting, no bloom (no game camera blooms), his real model, a
    /// target 6 m in front of him. Request `court <tag>` in `Temp/sean-fire-film.request` (read by `SeanFireFilm.cs`).
    ///
    /// Each frame is six pictures. ACROSS: the OLD look (the shared fire aura, `AbilityVfx.AttachAura(... FireEmber ...)`,
    /// the very call the ability makes with its switch off, its particles stepped by hand) and the NEW (`FxStudySeanStoke`,
    /// the fireball, at the game's daylight share). DOWN: his own eyes, a bystander off his right shoulder, the target.
    ///
    /// ⚠️ STAGED, AND SAID SO: his body is a still pose (nothing animates outside Play) slid along his aim by the rule's own
    /// sum; the effect is the study object stood on the court and played from its own clock, not the match hook
    /// (`SeanStokeFire`), which only runs in Play; first-person arms are not drawn.
    /// Frames go to `Logs/paete-ability-film/fxbench_seancourt_<tag>_frames/`, then `done_fxbench_seancourt_<tag>.txt`.
    /// </summary>
    public static partial class PaeteAbilityFilm
    {
        private const int SeanCourtWidth = 480, SeanCourtHeight = 270;

        internal static void RunSeanCourt(string tag)
        {
            Directory.CreateDirectory(OutDir);
            tag = new string((tag ?? "v00").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray()).ToLowerInvariant();
            if (tag.Length == 0) tag = "v00";
            string named = "fxbench_seancourt_" + tag;
            string donePath = Path.Combine(OutDir, "done_" + named + ".txt"), failedPath = Path.Combine(OutDir, "failed_" + named + ".txt");
            if (File.Exists(donePath)) File.Delete(donePath);
            if (File.Exists(failedPath)) File.Delete(failedPath);
            if (EditorApplication.isPlayingOrWillChangePlaymode) { File.WriteAllText(failedPath, "The editor is in Play mode."); return; }

            const string map = "Assets/TumbangPreso/Scenes/Maps/Eskinita.unity";
            Scene scene = default; bool lit = false;
            var owned = new List<Object>();
            FxStudySeanStoke fx = null;
            GameObject aura = null;
            var report = new System.Text.StringBuilder();
            try
            {
                scene = EditorSceneManager.OpenPreviewScene(map);
                if (!scene.IsValid()) throw new InvalidOperationException("No map to film on.");
                GameObject In(GameObject go) { SceneManager.MoveGameObjectToScene(go, scene); return go; }
                lit = Unsupported.SetOverrideLightingSettings(scene);
                Shader.SetGlobalFloat("_CharacterSmoothShade", ToonSkin.CharacterSmoothShade);
                Shader.SetGlobalFloat("_FxStudyBloom", 0f);
                var cam = In(new GameObject("~SeanCourtCamera")).AddComponent<Camera>();
                cam.scene = scene; cam.enabled = false; cam.allowHDR = false;
                cam.nearClipPlane = 0.06f; cam.farClipPlane = 400f;
                cam.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.55f, 0.70f, 0.88f);

                // Isagani's court spot on this map (`ZackCircuitFilm`): pale court, 8 m clear ahead.
                Vector3 stand = new Vector3(-2.5f, 0f, -6.0f), forward = Vector3.forward, right = Vector3.right;
                var physics = scene.GetPhysicsScene();
                if (physics.IsValid() && physics.Raycast(stand + Vector3.up * 30f, Vector3.down, out var under, 80f, ~0, QueryTriggerInteraction.Ignore))
                { stand.y = under.point.y; report.AppendLine("road at y " + stand.y.ToString("0.000") + " on " + under.collider.name); }
                Vector3 targetAt = stand + forward * 6f;
                GameObject Person(string id, Vector3 at, float yaw, out GameObject model)
                {
                    model = null;
                    var holder = In(new GameObject("~SeanCourt-" + id));
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
                var target = Person("dante", targetAt, 180f, out _);
                Person("cheska", stand + forward * 3.5f - right * 4.2f, 90f, out _);

                var stage = In(new GameObject("~SeanCourtStage"));
                stage.transform.SetPositionAndRotation(stand, Quaternion.LookRotation(forward));
                fx = new FxStudySeanStoke(stage.transform, AmihanWhirlwind.DayLight);
                report.AppendLine("new look: the fireball (c2), " + AmihanWhirlwind.DayLight.ToString("0.00") + " of it light laid on, as the game will draw it");

                var plain = new RenderTexture(SeanCourtWidth, SeanCourtHeight, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                owned.Add(plain);
                var cell = new Texture2D(SeanCourtWidth, SeanCourtHeight, TextureFormat.RGB24, false);
                var sheet = new Texture2D(SeanCourtWidth * 2, SeanCourtHeight * 3, TextureFormat.RGB24, false);
                owned.Add(cell); owned.Add(sheet);
                string frames = Path.Combine(OutDir, named + "_frames");
                if (Directory.Exists(frames)) foreach (string old in Directory.GetFiles(frames)) File.Delete(old);
                Directory.CreateDirectory(frames);

                float lead = 0.20f, seconds = lead + FxStudySeanStoke.Life;
                int count = Mathf.CeilToInt(seconds * BenchRate) + 1;
                ParticleSystem ps = null;
                var clock = new System.Diagnostics.Stopwatch();
                double worstMs = 0, laterMs = 0; int worstCalls = 0, worstTriangles = 0; float worstAt = 0;
                for (int f = 0; f < count; f++)
                {
                    float t = f / (float)BenchRate - lead;
                    float s = t - FxStudySeanStoke.Brace, gone = FxStudySeanStoke.Gone(s), stopped = s - FxStudySeanStoke.Travel;
                    Vector3 body = stand + forward * gone;
                    sean.transform.position = body;
                    // The old look: the very call the ability makes at the launch, its particles stepped by hand.
                    if (aura == null && s >= 0f)
                    {
                        aura = AbilityVfx.AttachAura(sean.transform, AbilityVfx.Aura.FireEmber, FxStudySeanStoke.Travel);
                        ps = aura != null ? aura.GetComponent<ParticleSystem>() : null;
                        if (ps != null) { ps.Simulate(0f, true, true); ps.Play(true); }
                    }
                    if (ps != null)
                    {
                        // The aura is released when he stops, exactly as `StokeStepAbility.ReleaseAura` does.
                        if (stopped >= 0f && ps.isEmitting) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                        ps.Simulate(1f / BenchRate, true, false);
                    }
                    clock.Restart();
                    fx.Pose(t, body + Vector3.up * 1.5f, gone, stopped);
                    clock.Stop();
                    if (clock.Elapsed.TotalMilliseconds > worstMs) worstMs = clock.Elapsed.TotalMilliseconds;
                    if (f > 0) laterMs = Math.Max(laterMs, clock.Elapsed.TotalMilliseconds);
                    fx.Cost(out int calls, out int triangles);
                    if (calls > worstCalls || (calls == worstCalls && triangles > worstTriangles)) { worstCalls = calls; worstTriangles = triangles; worstAt = t; }

                    for (int look = 0; look < 2; look++)
                    {
                        // ⚠️ THE AURA IS NEVER SWITCHED OFF, ONLY ITS RENDERER: a particle system on an inactive object is not
                        // stepped, which is why the old column of film d1 showed no aura at all.
                        if (ps != null) ps.GetComponent<ParticleSystemRenderer>().enabled = look == 0;
                        fx.Root.gameObject.SetActive(look == 1);
                        for (int view = 0; view < 3; view++)
                        {
                            Vector3 eye, aim; float fov;
                            if (view == 0) { eye = body + Vector3.up * 1.50f + forward * 0.12f; aim = body + forward * 8f + Vector3.up * 1.0f; fov = 70f; }
                            else if (view == 1) { eye = stand - forward * 1.2f + right * 4.6f + Vector3.up * 2.0f; aim = stand + forward * 1.3f + Vector3.up * 0.8f; fov = 50f; }
                            else { eye = targetAt + Vector3.up * 1.50f - forward * 0.15f; aim = stand + forward * 1.0f + Vector3.up * 0.9f; fov = 70f; }
                            cam.fieldOfView = fov;
                            cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(aim - eye, Vector3.up));
                            if (seanModel != null) seanModel.SetActive(view != 0);
                            target.SetActive(view != 2);
                            cam.targetTexture = plain;
                            cam.Render();
                            var keep = RenderTexture.active; RenderTexture.active = plain;
                            cell.ReadPixels(new Rect(0, 0, SeanCourtWidth, SeanCourtHeight), 0, 0); cell.Apply();
                            RenderTexture.active = keep;
                            sheet.SetPixels(look * SeanCourtWidth, (2 - view) * SeanCourtHeight, SeanCourtWidth, SeanCourtHeight, cell.GetPixels());
                        }
                    }
                    fx.Root.gameObject.SetActive(true);
                    sheet.Apply();
                    File.WriteAllBytes(Path.Combine(frames, "f" + f.ToString("000") + ".png"), sheet.EncodeToPNG());
                }
                report.AppendLine("map: " + map + (lit ? ", lit by its own lighting settings" : ", its lighting could not be put in force"));
                report.AppendLine(count + " frames at " + BenchRate + " a second, " + seconds.ToString("0.00") + " s (press at 0.20 s), in " + frames.Replace('\\', '/'));
                report.AppendLine("columns: OLD (the shared fire aura), NEW (the fireball); rows: his own eyes, a bystander off his right shoulder, the target 6 m ahead. No bloom.");
                report.AppendLine("new look, worst moment " + worstAt.ToString("0.00") + " s after the press: " + worstCalls + " draw calls, " + worstTriangles + " triangles (a two-pass cloud field counts twice)");
                report.AppendLine("new look posing: worst " + worstMs.ToString("0.000") + " ms (first frame), " + laterMs.ToString("0.000") + " ms after it");
                File.WriteAllText(donePath, report.ToString());
            }
            catch (Exception failure)
            {
                File.WriteAllText(failedPath, report + "\n" + failure);
                Debug.LogError("[SeanFireCourt] " + failure);
            }
            finally
            {
                try { fx?.Release(); } catch (Exception) { }
                if (aura != null) Object.DestroyImmediate(aura);
                if (lit) Unsupported.RestoreOverrideLightingSettings();
                RenderTexture.active = null;
                foreach (var thing in owned)
                {
                    if (thing == null) continue;
                    if (thing is RenderTexture texture) texture.Release();
                    Object.DestroyImmediate(thing);
                }
                if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
