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
    /// RAGO'S EMPOWERED THROW on the bench's dark floor (`FxStudySeanThrow`), with his real model, the real tsinelas flying
    /// the bench's stand-in arc, and a target 7.2 m ahead. FOUR views, because a flat card seen edge-on is invisible from
    /// one of them: side on, behind the thrower, from in front looking back down the path, and from above. No bloom.
    /// Request `throw <tag>` in `Temp/sean-fire-film.request`. Frames to `fxbench_seanthrow_<tag>_frames/`.
    /// ⚠️ STAGED: his body is a still pose; nothing of the match runs; the shoe flies an arc chosen for the bench.
    /// </summary>
    public static partial class PaeteAbilityFilm
    {
        internal static void RunSeanThrow(string tag)
        {
            Directory.CreateDirectory(OutDir);
            tag = new string((tag ?? "v00").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray()).ToLowerInvariant();
            if (tag.Length == 0) tag = "v00";
            string named = "fxbench_seanthrow_" + tag;
            string donePath = Path.Combine(OutDir, "done_" + named + ".txt"), failedPath = Path.Combine(OutDir, "failed_" + named + ".txt");
            if (File.Exists(donePath)) File.Delete(donePath);
            if (File.Exists(failedPath)) File.Delete(failedPath);
            if (EditorApplication.isPlayingOrWillChangePlaymode) { File.WriteAllText(failedPath, "The editor is in Play mode."); return; }

            var scene = EditorSceneManager.NewPreviewScene();
            var owned = new List<Object>();
            FxStudySeanThrow fx = null;
            var report = new System.Text.StringBuilder();
            try
            {
                GameObject In(GameObject go) { SceneManager.MoveGameObjectToScene(go, scene); return go; }
                var cam = In(new GameObject("~SeanThrowCamera")).AddComponent<Camera>();
                cam.scene = scene; cam.enabled = false; cam.allowHDR = false;
                cam.nearClipPlane = 0.06f; cam.farClipPlane = 300f; cam.clearFlags = CameraClearFlags.SolidColor;
                Shader.SetGlobalFloat("_FxStudyBloom", 0f);
                var floorShader = Shader.Find("TumbangPreso/FxStudyFloor");
                var floor = new Material(floorShader) { hideFlags = HideFlags.HideAndDontSave };
                owned.Add(floor);
                Color first = new Color(0.150f, 0.150f, 0.158f);
                floor.SetColor("_A", first); floor.SetColor("_B", new Color(0.27f, 0.27f, 0.28f));
                floor.SetColor("_LineColor", new Color(0.120f, 0.120f, 0.128f));
                floor.SetFloat("_Chequer", 0f); floor.SetFloat("_Cell", 1f); floor.SetVector("_Offset", Vector4.zero);
                floor.SetFloat("_LineWidth", 0.012f); floor.SetFloat("_Dim", 0.45f); floor.SetFloat("_DimAt", 14f);
                cam.backgroundColor = first * 0.45f;
                var ground = In(GameObject.CreatePrimitive(PrimitiveType.Plane));
                Object.DestroyImmediate(ground.GetComponent<Collider>());
                ground.transform.localScale = new Vector3(30f, 1f, 30f);
                ground.GetComponent<MeshRenderer>().sharedMaterial = floor;
                var sun = In(new GameObject("~SeanThrowSun")).AddComponent<Light>();
                sun.type = LightType.Directional; sun.intensity = 1.1f; sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
                Shader.SetGlobalFloat("_CharacterSmoothShade", ToonSkin.CharacterSmoothShade);

                GameObject Person(string id, Vector3 at, float yaw)
                {
                    var holder = In(new GameObject("~SeanThrow-" + id));
                    holder.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
                    var art = RosterBook.Load()?.FindPersonArt(id);
                    if (art == null || art.Model == null) return holder;
                    var model = (GameObject)Object.Instantiate(art.Model, holder.transform);
                    model.transform.localRotation = Quaternion.Euler(0f, CharacterVisual.PersonModelYaw, 0f);
                    model.transform.localScale = Vector3.one * CharacterVisual.PersonScale;
                    ToonSkin.Apply(model, ToonSkin.PersonOutlineWidth, art.Palette);
                    foreach (var animator in model.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
                    return holder;
                }
                Person("sean", Vector3.zero, 0f);
                Person("dante", new Vector3(0f, 0f, 7.2f), 180f);
                // The real tsinelas, flying the bench's arc.
                var shoe = In(new GameObject("~SeanThrow-tsinelas"));
                var shoeArt = RosterBook.Load()?.SlipperArt(0);
                if (shoeArt != null && shoeArt.Model != null)
                {
                    var model = (GameObject)Object.Instantiate(shoeArt.Model, shoe.transform);
                    foreach (var collider in model.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
                    ToonSkin.ApplySlipper(model, ToonSkin.PropOutlineWidth);
                }
                else report.AppendLine("the tsinelas model was not found: no shoe in this film");

                var stage = In(new GameObject("~SeanThrowStage"));
                fx = new FxStudySeanThrow(stage.transform);
                // The held shoe's fire, riding the world shoe, and a second shoe held up into his eyes' view as a stand-in for
                // the first-person one (the first-person arms are not drawn outside Play).
                var heldLook = FxStudySeanThrow.HeldMaterial(AmihanWhirlwind.DayLight); owned.Add(heldLook);
                GameObject HeldFire(Transform on)
                {
                    var go = new GameObject("~SeanHeldFire"); go.transform.SetParent(on, false);
                    go.AddComponent<MeshFilter>().sharedMesh = FxStudySeanThrow.HeldMesh();
                    var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = heldLook;
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.localBounds = new Bounds(Vector3.zero, Vector3.one * 4f);
                    return go;
                }
                var worldFire = HeldFire(shoe.transform);
                var eyeShoe = In(new GameObject("~SeanThrow-firstperson-shoe"));
                if (shoeArt != null && shoeArt.Model != null)
                {
                    var model = (GameObject)Object.Instantiate(shoeArt.Model, eyeShoe.transform);
                    foreach (var collider in model.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
                    ToonSkin.ApplySlipper(model, ToonSkin.PropOutlineWidth);
                }
                HeldFire(eyeShoe.transform);
                var sheEye = new Vector3(0f, 1.62f, 0.12f);
                eyeShoe.transform.SetPositionAndRotation(sheEye + new Vector3(0.26f, -0.32f, 0.55f), Quaternion.Euler(10f, -20f, 0f));

                const int W = 640, H = 360;
                var plain = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                owned.Add(plain);
                var cell = new Texture2D(W, H, TextureFormat.RGB24, false);
                var sheet = new Texture2D(W * 3, H * 2, TextureFormat.RGB24, false);
                owned.Add(cell); owned.Add(sheet);
                string frames = Path.Combine(OutDir, named + "_frames");
                if (Directory.Exists(frames)) foreach (string old in Directory.GetFiles(frames)) File.Delete(old);
                Directory.CreateDirectory(frames);

                int count = Mathf.CeilToInt(fx.Seconds * BenchRate) + 1;
                var clock = new System.Diagnostics.Stopwatch();
                double worstMs = 0, laterMs = 0; int worstCalls = 0, worstTriangles = 0; float worstAt = 0;
                for (int f = 0; f < count; f++)
                {
                    float age = f / (float)BenchRate;
                    float thrown = FxStudySeanThrow.ThrownAt;
                    var at = FxStudySeanThrow.Shoe(age - thrown, out bool flying);
                    shoe.transform.position = at;
                    shoe.SetActive(age >= thrown ? flying : true);
                    // Held at his right hip until the throw; its fire rides it and goes out as it leaves his hand.
                    if (age < thrown) { shoe.transform.position = new Vector3(0.38f, 1.0f, 0.15f); shoe.transform.rotation = Quaternion.Euler(0f, 0f, 70f); }
                    else shoe.transform.rotation = Quaternion.LookRotation(Vector3.forward);
                    heldLook.SetFloat("_Age", age); heldLook.SetFloat("_Opacity", age < thrown ? 1f : 0f);
                    clock.Restart(); fx.Sample(age, at); clock.Stop();
                    worstMs = Math.Max(worstMs, clock.Elapsed.TotalMilliseconds); if (f > 0) laterMs = Math.Max(laterMs, clock.Elapsed.TotalMilliseconds);
                    fx.Cost(out int calls, out int triangles);
                    if (calls > worstCalls) { worstCalls = calls; worstTriangles = triangles; worstAt = age; }
                    for (int view = 0; view < 6; view++)
                    {
                        Vector3 eye, aim; float fov = 42f;
                        if (view == 0) { eye = new Vector3(8.0f, 1.6f, 3.6f); aim = new Vector3(0f, 1.0f, 3.6f); }
                        else if (view == 1) { eye = new Vector3(1.6f, 2.6f, -3.2f); aim = new Vector3(0f, 1.0f, 4.0f); fov = 50f; }
                        else if (view == 2) { eye = new Vector3(2.2f, 1.7f, 10.5f); aim = new Vector3(0f, 1.0f, 3.0f); fov = 50f; }
                        else if (view == 3) { eye = new Vector3(0.01f, 12f, 3.6f); aim = new Vector3(0f, 0f, 3.6f); fov = 45f; }
                        // His own eyes (the first-person shoe held up into view while it is loaded), and a close look at the
                        // world shoe at his hip while held.
                        else if (view == 4) { eye = sheEye; aim = sheEye + new Vector3(0.12f, -0.25f, 1f); fov = 70f; }
                        else { eye = new Vector3(1.5f, 1.15f, -0.6f); aim = new Vector3(0.38f, 1.05f, 0.15f); fov = 40f; }
                        eyeShoe.SetActive(view == 4 && age < thrown);
                        worldFire.SetActive(view != 4);
                        cam.fieldOfView = fov;
                        cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(aim - eye, view == 3 ? Vector3.forward : Vector3.up));
                        cam.targetTexture = plain; cam.Render();
                        var keep = RenderTexture.active; RenderTexture.active = plain;
                        cell.ReadPixels(new Rect(0, 0, W, H), 0, 0); cell.Apply();
                        RenderTexture.active = keep;
                        sheet.SetPixels((view % 3) * W, (1 - view / 3) * H, W, H, cell.GetPixels());
                    }
                    sheet.Apply();
                    File.WriteAllBytes(Path.Combine(frames, "f" + f.ToString("000") + ".png"), sheet.EncodeToPNG());
                }
                report.AppendLine(count + " frames at " + BenchRate + " a second; thrown at 0.15 s, lands at " + (0.15f + 0.48f).ToString("0.00") + " s or so; in " + frames.Replace('\\', '/'));
                report.AppendLine("views: top row side on, behind the thrower, from in front looking back down the path; bottom row from above, his own eyes (a stand-in first-person shoe held up into view while loaded), a close look at the held world shoe. No bloom.");
                report.AppendLine("worst moment " + worstAt.ToString("0.00") + " s: " + worstCalls + " draw calls, " + worstTriangles + " triangles (a two-pass cloud field counts twice)");
                report.AppendLine("posing: worst " + worstMs.ToString("0.000") + " ms, after the first frame " + laterMs.ToString("0.000") + " ms");
                File.WriteAllText(donePath, report.ToString());
            }
            catch (Exception failure)
            {
                File.WriteAllText(failedPath, report + "\n" + failure);
                Debug.LogError("[SeanThrowFilm] " + failure);
            }
            finally
            {
                try { fx?.Release(); } catch (Exception) { }
                RenderTexture.active = null;
                foreach (var thing in owned) { if (thing == null) continue; if (thing is RenderTexture t) t.Release(); Object.DestroyImmediate(thing); }
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
