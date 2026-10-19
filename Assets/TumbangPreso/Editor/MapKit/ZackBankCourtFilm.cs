using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    public static partial class PaeteAbilityFilm
    {
        /// <summary>
        /// BANK SHOT ON THE COURT: the match driver (`FxStudyZackBank.Live`, the same object `ZackBankFlight` feeds in Play)
        /// fed a staged loaded throw on a real map, opened as a preview scene (nothing the owner has open is touched).
        ///
        /// The throw is the rule's: launched from his hand at 16.6 m/s (inside `Balance.LaunchSpeed`), lobbed so it is in
        /// the air about 1.3 s, falling at `Balance.Gravity`, banking off two stand-in walls 2.5 m either side of the line
        /// he faces and keeping 85 per cent of its speed at each, until it lands. Overclock's two banks (at about 0.28 s and
        /// 0.94 s), or one with a tag that begins `single`. The shoe is the real `tsinelas_classic` model.
        ///
        /// ⚠️ THE WALLS ARE COLLIDERS WITH ONLY A KERB AND TWO POSTS DRAWN. Solid stand-in walls stood between every outside
        /// camera and the flight and hid it (c1). The colliders are still 6 m tall, so the star finds them as it finds a
        /// real wall; what is drawn is a low kerb along each wall's foot and a thin post at each end, so the wall's line is
        /// readable and nothing covers the trail. Painted with the built-in `Standard` shader: this project draws with the
        /// built-in pipeline, and the URP shader c1 reached for rendered magenta.
        ///
        /// Each frame is four pictures at 60 a second, no bloom (no game camera has it): from the side, from behind his
        /// shoulder, from the eyes of a target down the court looking back up the flight, and from above. It runs 0.9 s
        /// past the landing, so the trail is seen dissipating where it lies.
        ///
        /// Request: a line `bankcourt <tag> [map]` in `Temp/zack-circuit-film.request`. Frames go to
        /// `Logs/paete-ability-film/fxbench_zackbankcourt_<tag>_frames/`, then `done_fxbench_zackbankcourt_<tag>.txt` with
        /// what it costs, or `failed_...`.
        /// </summary>
        internal static void RunZackBankCourt(string tag, string map)
        {
            var chosen = ZackMapOf(map);
            Directory.CreateDirectory(OutDir);
            tag = new string((tag ?? "c1").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray()).ToLowerInvariant();
            if (tag.Length == 0) tag = "c1";
            string named = "fxbench_zackbankcourt_" + tag;
            string donePath = Path.Combine(OutDir, "done_" + named + ".txt"), failedPath = Path.Combine(OutDir, "failed_" + named + ".txt");
            if (File.Exists(donePath)) File.Delete(donePath);
            if (File.Exists(failedPath)) File.Delete(failedPath);
            if (EditorApplication.isPlayingOrWillChangePlaymode) { File.WriteAllText(failedPath, "The editor is in Play mode. The film only runs outside it."); return; }

            Scene scene = default;
            bool opened = false, lit = false;
            var owned = new List<Object>();
            FxStudyZackBank.Live fx = null;
            var report = new System.Text.StringBuilder();
            try
            {
                try { scene = EditorSceneManager.OpenPreviewScene(chosen.scene); opened = scene.IsValid(); }
                catch (Exception refused) { report.AppendLine("The map did not open as a preview scene (" + refused.Message + ")."); }
                if (!opened) throw new InvalidOperationException("No map to film on.");
                GameObject In(GameObject go) { SceneManager.MoveGameObjectToScene(go, scene); return go; }
                report.AppendLine("map: " + chosen.scene);
                lit = Unsupported.SetOverrideLightingSettings(scene);
                Shader.SetGlobalFloat("_CharacterSmoothShade", ToonSkin.CharacterSmoothShade);

                var cam = In(new GameObject("~BankFilmCamera")).AddComponent<Camera>();
                cam.scene = scene; cam.enabled = false; cam.nearClipPlane = 0.06f; cam.farClipPlane = 400f;
                cam.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.55f, 0.70f, 0.88f);

                float yaw = chosen.spot.z;
                Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward, right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
                Vector3 stand = new Vector3(chosen.spot.x, 0f, chosen.spot.y);
                var physics = scene.GetPhysicsScene();
                if (physics.IsValid() && physics.Raycast(stand + Vector3.up * 30f, Vector3.down, out var under, 80f, ~0, QueryTriggerInteraction.Ignore)) stand.y = under.point.y;
                float road = stand.y;

                GameObject Person(string id, Vector3 at, float facing)
                {
                    var holder = In(new GameObject("~BankFilm-" + id));
                    holder.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, facing, 0f));
                    var art = RosterBook.Load()?.FindPersonArt(id);
                    if (art == null || art.Model == null) return holder;
                    var model = (GameObject)Object.Instantiate(art.Model, holder.transform);
                    model.transform.localPosition = Vector3.zero;
                    model.transform.localRotation = Quaternion.Euler(0f, CharacterVisual.PersonModelYaw, 0f);
                    model.transform.localScale = Vector3.one * CharacterVisual.PersonScale;
                    ToonSkin.Apply(model, ToonSkin.PersonOutlineWidth, art.Palette);
                    foreach (var animator in model.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
                    return holder;
                }
                Person("zack", stand, yaw);
                Vector3 targetAt = stand + forward * 12f;
                var target = Person("dante", targetAt, yaw + 180f);

                // The two stand-in walls, 2.5 m either side of the line he faces: a collider each, and a kerb and two posts drawn.
                const float Half = 2.5f, WallFrom = 0.5f, WallTo = 11.5f, WallTall = 6f;
                var paint = new Material(Shader.Find("Standard")) { color = new Color(0.70f, 0.68f, 0.64f), hideFlags = HideFlags.HideAndDontSave };
                owned.Add(paint);
                GameObject Block(string name, Vector3 at, Vector3 size, bool drawn)
                {
                    var block = In(GameObject.CreatePrimitive(PrimitiveType.Cube));
                    block.name = name;
                    block.transform.SetPositionAndRotation(at, Quaternion.LookRotation(forward));
                    block.transform.localScale = size;
                    var renderer = block.GetComponent<MeshRenderer>();
                    renderer.sharedMaterial = paint;
                    renderer.enabled = drawn;
                    // The tall collider is the wall the star looks for; a drawn kerb or post must not stand in for it.
                    if (drawn) { var box = block.GetComponent<BoxCollider>(); if (box != null) Object.DestroyImmediate(box); }
                    return block;
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 foot = stand + right * (side * (Half + 0.12f)) + forward * ((WallFrom + WallTo) * 0.5f);
                    Block("~BankFilmWall", foot + Vector3.up * (WallTall * 0.5f), new Vector3(0.2f, WallTall, WallTo - WallFrom), false);
                    Block("~BankFilmKerb", foot + Vector3.up * 0.06f, new Vector3(0.22f, 0.12f, WallTo - WallFrom), true);
                    for (int end = 0; end < 2; end++)
                        Block("~BankFilmPost", stand + right * (side * (Half + 0.12f)) + forward * (end == 0 ? WallFrom : WallTo) + Vector3.up * (WallTall * 0.5f),
                              new Vector3(0.06f, WallTall, 0.06f), true);
                }
                // (A preview scene's physics knows a new collider only once it is told.)
                Physics.SyncTransforms();

                // The shoe.
                var shoe = In(new GameObject("~BankFilmShoe"));
                var source = Resources.Load<GameObject>("Models/tsinelas_classic");
                if (source != null)
                {
                    var model = (GameObject)Object.Instantiate(source, shoe.transform);
                    var filter = model.GetComponentInChildren<MeshFilter>();
                    if (filter != null && filter.sharedMesh != null)
                    {
                        var b = filter.sharedMesh.bounds;
                        model.transform.localScale = Vector3.one * (0.46f / Mathf.Max(0.01f, Mathf.Max(b.size.x, b.size.z)));
                        model.transform.localRotation = b.size.x > b.size.z ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
                        model.transform.localPosition = -(model.transform.localRotation * (b.center * model.transform.localScale.x));
                    }
                }
                else report.AppendLine("tsinelas_classic was not found: no shoe is drawn, only its effect");

                var stage = In(new GameObject("~BankFilmStage"));
                fx = new FxStudyZackBank.Live(stage.transform, scene, physics);

                int charges = tag.StartsWith("single") ? 1 : 2;
                // The flight, stepped finely and looked at sixty times a second, as a frame of a match looks at it.
                // Lobbed across the lane: 8 m/s toward the right wall, 8 m/s down the court, 12 m/s up (16.6 m/s in all).
                Vector3 p = stand + Vector3.up * 1.25f + forward * 0.35f;
                Vector3 v = right * 8f + forward * 8f + Vector3.up * 12f;
                int left = charges; bool inFlight = true;
                const float Lead = 0.25f, After = 0.9f;
                var plainTarget = new RenderTexture(ZackCellWidth, ZackCellHeight, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                owned.Add(plainTarget);
                var cell = new Texture2D(ZackCellWidth, ZackCellHeight, TextureFormat.RGB24, false);
                var sheet = new Texture2D(ZackCellWidth * 2, ZackCellHeight * 2, TextureFormat.RGB24, false);
                owned.Add(cell); owned.Add(sheet);
                string frames = Path.Combine(OutDir, named + "_frames");
                if (Directory.Exists(frames)) foreach (string old in Directory.GetFiles(frames)) File.Delete(old);
                Directory.CreateDirectory(frames);
                var watch = new System.Diagnostics.Stopwatch(); var ms = new List<double>();
                int calls = 0, triangles = 0, banks = 0; float landedAt = -1f, busiest = 0f;
                float clock = 0f;
                for (int f = 0; ; f++)
                {
                    float t = f / (float)BenchRate - Lead;
                    if (t > 6f || (landedAt >= 0f && t > landedAt + After)) break;
                    if (t > 0f && inFlight)
                    {
                        const int Steps = 4;
                        float dt = 1f / (BenchRate * Steps);
                        for (int k = 0; k < Steps && inFlight; k++)
                        {
                            v.y -= Balance.Gravity * dt;
                            p += v * dt;
                            float lateral = Vector3.Dot(p - stand, right);
                            if (Mathf.Abs(lateral) > Half - Balance.SlipperHitRadius)
                            {
                                p -= right * (lateral - Mathf.Sign(lateral) * (Half - Balance.SlipperHitRadius));
                                float across = Vector3.Dot(v, right);
                                v -= right * (2f * across);
                                if (left > 0) { v = new Vector3(v.x * 0.85f, v.y, v.z * 0.85f); left--; banks++; }
                            }
                            if (p.y <= road + 0.05f) { p.y = road + 0.05f; inFlight = false; landedAt = t; }
                        }
                    }
                    shoe.transform.SetPositionAndRotation(p, Quaternion.LookRotation(new Vector3(v.x, 0f, v.z).sqrMagnitude > 1e-4f ? new Vector3(v.x, 0f, v.z) : forward));
                    clock = t;

                    // The flight spans about 9 m of court and climbs to about 4.9 m. No wall is drawn above its kerb, so no
                    // view looks through one.
                    Vector3 mid = stand + forward * 4.5f + Vector3.up * 2.6f;
                    for (int view = 0; view < 4; view++)
                    {
                        Vector3 eye, look; float fov;
                        if (view == 0) { eye = mid - right * 12f + Vector3.up * 0.8f; look = mid; fov = 50f; }
                        else if (view == 1) { eye = stand - forward * 3.2f - right * 0.6f + Vector3.up * 2.2f; look = mid; fov = 70f; }
                        else if (view == 2) { eye = targetAt + Vector3.up * 1.5f; look = mid; fov = 70f; }
                        else { eye = stand + forward * 3.0f + Vector3.up * 15f; look = stand + forward * 4.5f; fov = 55f; }
                        if (t > 0f)
                        {
                            watch.Restart();
                            if (view == 0) fx.Observe(clock, p, inFlight ? v : Vector3.zero, left, inFlight);
                            fx.Pose(clock, eye);
                            watch.Stop(); if (view == 0) ms.Add(watch.Elapsed.TotalMilliseconds);
                        }
                        target.SetActive(view != 2);
                        cam.fieldOfView = fov;
                        cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye, Vector3.up));
                        cam.targetTexture = plainTarget; cam.allowHDR = false;
                        cam.Render();
                        var keep = RenderTexture.active; RenderTexture.active = plainTarget;
                        cell.ReadPixels(new Rect(0, 0, ZackCellWidth, ZackCellHeight), 0, 0); cell.Apply();
                        RenderTexture.active = keep;
                        sheet.SetPixels((view % 2) * ZackCellWidth, (1 - view / 2) * ZackCellHeight, ZackCellWidth, ZackCellHeight, cell.GetPixels());
                    }
                    int on = 0, tris = 0;
                    foreach (var renderer in stage.GetComponentsInChildren<MeshRenderer>(false))
                    {
                        if (!renderer.enabled) continue;
                        var filter = renderer.GetComponent<MeshFilter>();
                        if (filter == null || filter.sharedMesh == null) continue;
                        on++; tris += (int)(filter.sharedMesh.GetIndexCount(0) / 3);
                    }
                    if (on > calls) { calls = on; triangles = tris; busiest = t; }
                    sheet.Apply();
                    File.WriteAllBytes(Path.Combine(frames, "f" + f.ToString("000") + ".png"), sheet.EncodeToPNG());
                }
                report.AppendLine((charges > 1 ? "Overclock, two banks" : "one bank") + "; banked " + banks + " times; landed at " + landedAt.ToString("0.00") + " s");
                report.AppendLine("frames at " + BenchRate + " a second in " + frames.Replace('\\', '/') + "; each frame: side, behind him, the target's eyes, above (no bloom)");
                report.AppendLine("busiest frame " + busiest.ToString("0.00") + " s: " + calls + " renderers on (draw calls, none batched), " + triangles + " triangles");
                report.AppendLine("MEASURED in the editor: observe and pose a frame mean " + (ms.Count > 0 ? ms.Average() : 0).ToString("0.000") + " ms, worst " + (ms.Count > 0 ? ms.Max() : 0).ToString("0.000") + " ms");
                report.AppendLine(lit ? "lit by the map's own lighting settings" : "the map's lighting settings could not be put in force");
                File.WriteAllText(donePath, report.ToString());
                Debug.Log("[ZackCircuitFilm] " + named + " written to " + OutDir);
            }
            catch (Exception failure)
            {
                File.WriteAllText(failedPath, report + "\n" + failure);
                Debug.LogError("[ZackCircuitFilm] " + failure);
            }
            finally
            {
                try { fx?.Dispose(); } catch (Exception) { }
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
