using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// ⚠️ THE WATER WALL OF ILYAS, FILMED WHERE IT IS PLAYED. The gale's court film (`PaeteAbilityFilm.Gale.cs`) done for
    /// his WATER WALL: the same map opened as a preview scene (the scene the owner has open is not touched and nothing
    /// is saved), the same spot on its court, daylight, bodies for scale, from the three places the wall is seen from:
    ///
    ///   * a third person three-quarter view;
    ///   * the caster's own eyes, the wall 3 m in front of him;
    ///   * a thrower's eyes, facing the wall from 4.5 m the other side: the view a throw is made from.
    ///
    /// Each frame is nine pictures: those three views down, and across them THE OLD WALL, THE NEW WALL, and the new one
    /// through the bench's bloom, so the two are judged at the same moments and what a bloom would buy is seen beside
    /// what ships without one.
    ///
    /// It has a request file of its own, `Temp/rafi-wall-film.request` (one line: the tag), and is hooked to the editor
    /// from here, so the shared film file is not touched. A tag that begins `split` breaks the wall 1.6 s in, as a
    /// slipper would. Frames go to `Logs/paete-ability-film/fxbench_rafiwallcourt_<tag>_frames/` (named like a bench film
    /// so the same tools join and cut them), then `done_fxbench_rafiwallcourt_<tag>.txt`, or `failed_...`.
    /// </summary>
    public static partial class PaeteAbilityFilm
    {
        private const string RafiWallRequest = "Temp/rafi-wall-film.request";
        private const float RafiWallAhead = 3.0f, RafiThrowerAhead = 7.5f, RafiSplitAt = 1.6f;
        private static double _nextRafiWallPoll;

        [InitializeOnLoadMethod]
        private static void HookRafiWall() { EditorApplication.update += PollRafiWall; }

        private static void PollRafiWall()
        {
            if (EditorApplication.timeSinceStartup < _nextRafiWallPoll) return;
            _nextRafiWallPoll = EditorApplication.timeSinceStartup + 1.0;
            if (!File.Exists(RafiWallRequest) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            AssetDatabase.Refresh();
            if (EditorApplication.isCompiling) return;
            string tag = File.ReadAllText(RafiWallRequest).Trim();
            File.Delete(RafiWallRequest);
            if (tag.StartsWith("measurecurrent")) { MeasureRafiCurrent(tag); return; }
            if (tag.StartsWith("streamviews")) { RunRafiStreamViews(tag); return; }
            if (tag.StartsWith("measurestream")) { MeasureRafiStream(tag); return; }
            RunRafiWall(tag);
        }

        private static void RunRafiWall(string tag)
        {
            Directory.CreateDirectory(OutDir);
            tag = new string((tag ?? "v00").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray()).ToLowerInvariant();
            if (tag.Length == 0) tag = "v00";
            string named = "fxbench_rafiwallcourt_" + tag;
            string donePath = Path.Combine(OutDir, "done_" + named + ".txt"), failedPath = Path.Combine(OutDir, "failed_" + named + ".txt");
            if (File.Exists(donePath)) File.Delete(donePath);
            if (File.Exists(failedPath)) File.Delete(failedPath);

            Scene scene = default;
            bool lit = false, switchBefore = RafiWaterVisual.NewWall;
            var owned = new List<Object>();
            var report = new System.Text.StringBuilder();
            RafiWallCurtain curtain = null;
            try
            {
                scene = EditorSceneManager.OpenPreviewScene(GaleMap);
                if (!scene.IsValid()) throw new InvalidOperationException("The map did not open as a preview scene: " + GaleMap);
                GameObject In(GameObject go) { SceneManager.MoveGameObjectToScene(go, scene); return go; }
                report.AppendLine("map: " + GaleMap);
                lit = Unsupported.SetOverrideLightingSettings(scene);
                Shader.SetGlobalFloat("_CharacterSmoothShade", ToonSkin.CharacterSmoothShade);

                var cam = In(new GameObject("~RafiWallFilmCamera")).AddComponent<Camera>();
                cam.scene = scene; cam.enabled = false;
                cam.nearClipPlane = 0.06f; cam.farClipPlane = 400f;
                cam.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.55f, 0.70f, 0.88f);

                Quaternion facing = Quaternion.Euler(0f, GaleYaw, 0f);
                Vector3 forward = facing * Vector3.forward, right = facing * Vector3.right;
                float road = GaleStand.y;
                var physics = scene.GetPhysicsScene();
                if (physics.IsValid() && physics.Raycast(GaleStand + Vector3.up * 30f, Vector3.down, out var under, 80f, ~0, QueryTriggerInteraction.Ignore))
                { road = under.point.y; report.AppendLine("road found at y " + road.ToString("0.000") + " on " + under.collider.name); }
                Vector3 stand = new Vector3(GaleStand.x, road, GaleStand.z), wallAt = stand + forward * RafiWallAhead, throwerAt = stand + forward * RafiThrowerAhead;

                GameObject Body(string id, Vector3 at, float yaw)
                {
                    var art = RosterBook.Load()?.FindPersonArt(id);
                    if (art == null || art.Model == null) return null;
                    var body = In((GameObject)Object.Instantiate(art.Model));
                    body.name = "~RafiWallFilm-" + id;
                    body.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw + CharacterVisual.PersonModelYaw, 0f));
                    body.transform.localScale = Vector3.one * CharacterVisual.PersonScale;
                    ToonSkin.Apply(body, ToonSkin.PersonOutlineWidth, art.Palette);
                    foreach (var animator in body.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
                    return body;
                }
                var caster = Body("rafi", stand, GaleYaw);
                var thrower = Body("dante", throwerAt, GaleYaw + 180f);
                // One standing right at the wall's end, for its height against a body.
                Body("cheska", wallAt + right * (RafiRules.WallHalfWidth + 0.6f), GaleYaw - 90f);

                bool split = tag.StartsWith("split");
                var state = new WorldEffectSnapshot.Field
                {
                    Type = WorldEffectSnapshot.Kind.Waterwall, EventId = 1, Position = wallAt, Forward = forward, Radius = RafiRules.WallHalfWidth,
                    Duration = RafiRules.WallSeconds, Remaining = RafiRules.WallSeconds, Owner = 0, Path = Array.Empty<Vector3>(),
                };
                var broken = state; broken.Split = true; broken.FirstScale = RafiSplitAt;

                // One of each wall, at the same place, each built with the switch its own way.
                var oldHost = In(new GameObject("~RafiWallFilmOld")); var newHost = In(new GameObject("~RafiWallFilmNew"));
                RafiWaterVisual.NewWall = false;
                var oldWall = RafiWaterVisual.Build(oldHost.transform, state);
                RafiWaterVisual.NewWall = true;
                RafiWallCurtain.StagedLight = -1f;
                if (tag.Contains("light") && int.TryParse(new string(tag.Substring(tag.IndexOf("light") + 5).TakeWhile(char.IsDigit).ToArray()), out int hundredths)) RafiWallCurtain.StagedLight = hundredths / 100f;
                var newWall = RafiWaterVisual.Build(newHost.transform, state);
                oldWall.enabled = false; newWall.enabled = false;
                curtain = newWall.Curtain;
                if (curtain == null) throw new InvalidOperationException("The new wall was not built: RafiWaterVisual.NewWall made no curtain.");

                var plainTarget = new RenderTexture(GaleCellWidth, GaleCellHeight, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                var hdrTarget = new RenderTexture(GaleCellWidth, GaleCellHeight, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 4 };
                var shown = new RenderTexture(GaleCellWidth, GaleCellHeight, 0, RenderTextureFormat.ARGB32);
                owned.Add(plainTarget); owned.Add(hdrTarget); owned.Add(shown);
                var bloomShader = Shader.Find("Hidden/TumbangPreso/FxStudyBloom");
                Material bloomMaterial = bloomShader != null ? new Material(bloomShader) { hideFlags = HideFlags.HideAndDontSave } : null;
                if (bloomMaterial != null) owned.Add(bloomMaterial); else report.AppendLine("the bench bloom shader is missing: the third column is the plain picture again");
                var cell = new Texture2D(GaleCellWidth, GaleCellHeight, TextureFormat.RGB24, false);
                var sheet = new Texture2D(GaleCellWidth * 3, GaleCellHeight * 3, TextureFormat.RGB24, false);
                owned.Add(cell); owned.Add(sheet);
                string frames = Path.Combine(OutDir, named + "_frames");
                if (Directory.Exists(frames)) foreach (string old in Directory.GetFiles(frames)) File.Delete(old);
                Directory.CreateDirectory(frames);

                void Shoot(bool bloom, int column, int row)
                {
                    cam.allowHDR = bloom && bloomMaterial != null;
                    cam.targetTexture = cam.allowHDR ? hdrTarget : plainTarget;
                    Shader.SetGlobalFloat("_FxStudyBloom", cam.allowHDR ? 1f : 0f);
                    cam.Render();
                    if (cam.allowHDR) Bloom(hdrTarget, shown, bloomMaterial, BloomIntensity);
                    var keep = RenderTexture.active; RenderTexture.active = cam.allowHDR ? shown : plainTarget;
                    cell.ReadPixels(new Rect(0, 0, GaleCellWidth, GaleCellHeight), 0, 0); cell.Apply();
                    RenderTexture.active = keep;
                    Shader.SetGlobalFloat("_FxStudyBloom", 0f);
                    sheet.SetPixels(column * GaleCellWidth, (2 - row) * GaleCellHeight, GaleCellWidth, GaleCellHeight, cell.GetPixels());
                }

                // The new wall is drawn past the rule's end: its fall and its splash.
                float life = RafiRules.WallSeconds, letGo = split ? RafiSplitAt : life;
                float seconds = split ? letGo + RafiWallCurtain.TailSeconds + 0.4f : life + RafiWallCurtain.TailSeconds + 0.15f;
                int count = Mathf.CeilToInt(seconds * BenchRate) + 1;
                string cost = null;
                for (int f = 0; f < count; f++)
                {
                    float t = f / (float)BenchRate;
                    var now = split && t >= RafiSplitAt ? broken : state;
                    for (int view = 0; view < 3; view++)
                    {
                        Vector3 eye, look; float fov;
                        if (view == 0) { eye = wallAt + right * 5.2f + Vector3.up * 2.7f - forward * 4.4f; look = wallAt + Vector3.up * 0.8f; fov = 46f; }
                        else if (view == 1) { eye = stand + Vector3.up * 1.55f - forward * 0.15f; look = wallAt + Vector3.up * 0.9f; fov = 70f; }
                        else { eye = throwerAt + Vector3.up * 1.55f + forward * 0.15f; look = wallAt + Vector3.up * 0.9f; fov = 70f; }
                        cam.fieldOfView = fov;
                        cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye, Vector3.up));
                        // Nobody sees their own body from behind their own eyes.
                        if (caster != null) caster.SetActive(view != 1);
                        if (thrower != null) thrower.SetActive(view != 2);

                        // The old wall is gone, with its field, at the end of its life.
                        newHost.SetActive(false); oldHost.SetActive(t <= life);
                        oldWall.SetState(now); oldWall.StepTo(t);
                        Shoot(false, 0, view);

                        oldHost.SetActive(false); newHost.SetActive(true);
                        RafiWallCurtain.StagedEye = eye;
                        // Past the field's life nothing steps the visual: the wall runs its own tail, which is this call.
                        if (t <= life) { newWall.SetState(now); newWall.StepTo(t); } else curtain.Pose(t, now);
                        Shoot(false, 1, view);
                        Shoot(true, 2, view);
                    }
                    sheet.Apply();
                    File.WriteAllBytes(Path.Combine(frames, "f" + f.ToString("000") + ".png"), sheet.EncodeToPNG());

                    if (cost == null && t >= 1.2f)
                    {
                        string Count(GameObject host, bool on)
                        {
                            host.SetActive(true);
                            int calls = 0, triangles = 0;
                            foreach (var renderer in host.GetComponentsInChildren<MeshRenderer>(false))
                            {
                                if (!renderer.enabled) continue;
                                var filter = renderer.GetComponent<MeshFilter>();
                                if (filter == null || filter.sharedMesh == null) continue;
                                calls++; triangles += (int)(filter.sharedMesh.GetIndexCount(0) / 3);
                            }
                            int lines = host.GetComponentsInChildren<LineRenderer>(false).Length;
                            host.SetActive(on);
                            return calls + lines + " draw calls (" + lines + " of them line strips), " + triangles + " triangles in meshes";
                        }
                        cost = "standing, at 1.2 s   OLD: " + Count(oldHost, false) + "\n                     NEW: " + Count(newHost, true);
                    }
                }
                report.AppendLine(count + " frames at " + BenchRate + " a second (" + seconds.ToString("0.00") + " s), in " + frames.Replace('\\', '/'));
                report.AppendLine("each frame: rows are third person, caster's eyes, a thrower's eyes; columns are OLD, NEW, NEW with the bench bloom");
                report.AppendLine("the wall is " + RafiWallAhead.ToString("0.0") + " m in front of the caster and the thrower " + (RafiThrowerAhead - RafiWallAhead).ToString("0.0") + " m beyond it; it is "
                                  + (RafiRules.WallHalfWidth * 2f).ToString("0.0") + " m wide and " + RafiRules.WallHeight.ToString("0.0") + " m tall by the rule");
                report.AppendLine(split ? "broken at " + RafiSplitAt.ToString("0.00") + " s: the new wall falls then, the old one drains then" : "nothing breaks it: the rule ends at " + life.ToString("0.00") + " s, the old wall is gone then, the new one falls and splashes for " + RafiWallCurtain.TailSeconds.ToString("0.00") + " s after");
                report.AppendLine("of the new wall, " + (RafiWallCurtain.StagedLight >= 0f ? RafiWallCurtain.StagedLight : RafiWallCurtain.DayLight).ToString("0.00") + " is light laid on (the bench's is 0.45)");
                float Top(GameObject thing)
                {
                    float top = road; bool was = thing.activeSelf; thing.SetActive(true);
                    foreach (var renderer in thing.GetComponentsInChildren<Renderer>(false)) if (renderer.enabled) top = Mathf.Max(top, renderer.bounds.max.y);
                    thing.SetActive(was); return top - road;
                }
                newWall.SetState(state); newWall.StepTo(1.2f);
                if (caster != null) { caster.SetActive(true); report.AppendLine("a character stands " + Top(caster).ToString("0.00") + " m; the new wall's drawn pieces reach " + Top(newHost).ToString("0.00") + " m"); }
                report.AppendLine(cost ?? "cost not counted");
                report.AppendLine(lit ? "lit by the map's own lighting settings" : "the map's lighting settings could not be put in force: lit by the open scene's ambient");
                File.WriteAllText(donePath, report.ToString());
                Debug.Log("[PaeteAbilityFilm] " + named + " written to " + OutDir);
            }
            catch (Exception failure)
            {
                File.WriteAllText(failedPath, report + "\n" + failure);
                Debug.LogError("[PaeteAbilityFilm] " + failure);
            }
            finally
            {
                RafiWaterVisual.NewWall = switchBefore;
                RafiWallCurtain.StagedEye = null; RafiWallCurtain.StagedLight = -1f;
                // (Outside Play nothing calls a component's OnDestroy, so the film frees the wall by the same call that does.)
                if (curtain != null) curtain.Free();
                Shader.SetGlobalFloat("_FxStudyBloom", 0f);
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

        /// <summary>
        /// ⚠️ WHAT THE CROSSCURRENT COSTS, MEASURED AND NOT WORKED OUT FROM MESH SIZES. Eight currents at once (more than a
        /// match ever has: one Ilyas, one current each) are built in a preview scene and each is posed 2,000 times across
        /// its life, timed by a stopwatch, with the managed memory allocated while posing counted. Then one is drawn and
        /// its renderers counted. Posing is all the CPU work it does in play; the rest is the GPU's. `measurecurrent <tag>`
        /// in `Temp/rafi-wall-film.request` writes `done_measurecurrent_<tag>.txt`.
        /// </summary>
        private static void MeasureRafiCurrent(string request)
        {
            Directory.CreateDirectory(OutDir);
            string tag = new string(request.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
            string path = Path.Combine(OutDir, "done_" + tag + ".txt");
            var scene = EditorSceneManager.NewPreviewScene();
            var report = new System.Text.StringBuilder();
            var made = new List<FxStudyRafiCurrent>();
            try
            {
                var stage = new GameObject("~CurrentMeasure"); SceneManager.MoveGameObjectToScene(stage, scene);
                var look = FxStudyRafiWall.Look.Bench; look.Light = RafiWallCurtain.DayLight;
                long builtBefore = GC.GetAllocatedBytesForCurrentThread();
                var build = System.Diagnostics.Stopwatch.StartNew();
                for (int i = 0; i < 8; i++) made.Add(new FxStudyRafiCurrent(stage.transform, look, -1f, i * 0.13f));
                build.Stop();
                long builtBytes = GC.GetAllocatedBytesForCurrentThread() - builtBefore;
                // A warm-up pass, so the first property block set is not counted.
                foreach (var current in made) current.Pose(0.5f, -1f);
                const int poses = 2000;
                long before = GC.GetAllocatedBytesForCurrentThread();
                var clock = System.Diagnostics.Stopwatch.StartNew();
                for (int k = 0; k < poses; k++)
                {
                    float t = (k % 120) / 120f * (FxStudyRafiCurrent.Life + FxStudyRafiCurrent.Burst);
                    foreach (var current in made) current.Pose(t, -1f);
                }
                clock.Stop();
                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                double each = clock.Elapsed.TotalMilliseconds / (poses * (double)made.Count);
                report.AppendLine("posing: " + (each * 1000.0).ToString("0.00") + " microseconds per current per frame (" + made.Count + " currents x " + poses + " poses, " + clock.Elapsed.TotalMilliseconds.ToString("0.0") + " ms in all), measured in the editor outside Play");
                report.AppendLine("managed memory allocated while posing: " + allocated + " bytes over " + (poses * made.Count) + " poses");
                report.AppendLine("building: " + (build.Elapsed.TotalMilliseconds / made.Count).ToString("0.00") + " ms per current, " + (builtBytes / made.Count) + " bytes each (meshes and materials are made once, by the first)");
                made[0].Pose(0.5f, -1f);
                int calls = 0, triangles = 0;
                var materials = new HashSet<Material>(); var meshes = new HashSet<Mesh>();
                foreach (var current in made)
                    foreach (var renderer in current.Root.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        var filter = renderer.GetComponent<MeshFilter>();
                        if (renderer.sharedMaterial != null) materials.Add(renderer.sharedMaterial);
                        if (filter != null && filter.sharedMesh != null) meshes.Add(filter.sharedMesh);
                        if (current != made[0] || !renderer.enabled || filter == null || filter.sharedMesh == null) continue;
                        calls++; triangles += (int)(filter.sharedMesh.GetIndexCount(0) / 3);
                    }
                report.AppendLine("one current in flight: " + calls + " draw calls, " + triangles + " triangles");
                report.AppendLine("all eight share " + materials.Count + " materials and " + meshes.Count + " meshes");
            }
            catch (Exception failure) { report.AppendLine(failure.ToString()); }
            finally
            {
                foreach (var current in made) current.Dispose();
                EditorSceneManager.ClosePreviewScene(scene);
                File.WriteAllText(path, report.ToString());
            }
        }

        /// <summary>
        /// ⚠️ THE STREAM FROM FOUR SIDES AT ONCE. The owner rejected passes elsewhere that looked right from the one camera
        /// they were filmed from and were a sliver or a card from another. So every frame of this is four pictures: the
        /// side, behind the caster, in front looking back down its path (a thrower's view), and from above, on the
        /// bench's dark floor, lit, with the caster and a thrower standing for scale and no bloom (as the game draws).
        /// `streamviews<tag>` films it whole; `streamviewsspent<tag>` spends it 0.55 s after the cast. Frames go to
        /// `Logs/paete-ability-film/fxbench_<request>_frames/`, as a bench film's do.
        /// </summary>
        private static void RunRafiStreamViews(string request)
        {
            Directory.CreateDirectory(OutDir);
            string tag = new string(request.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
            string named = "fxbench_" + tag;
            string donePath = Path.Combine(OutDir, "done_" + named + ".txt"), failedPath = Path.Combine(OutDir, "failed_" + named + ".txt");
            if (File.Exists(donePath)) File.Delete(donePath);
            if (File.Exists(failedPath)) File.Delete(failedPath);
            var scene = EditorSceneManager.NewPreviewScene();
            var owned = new List<Object>();
            FxStudyRafiStream fx = null;
            try
            {
                GameObject In(GameObject go) { SceneManager.MoveGameObjectToScene(go, scene); return go; }
                var stage = In(new GameObject("~StreamStage"));
                fx = new FxStudyRafiStream(stage.transform, FxStudyRafiWall.Look.Bench, tag.Contains("spent") ? 0.55f : -1f, 0f, 0);
                var floorShader = Shader.Find("TumbangPreso/FxStudyFloor");
                var floor = new Material(floorShader) { hideFlags = HideFlags.HideAndDontSave }; owned.Add(floor);
                floor.SetColor("_A", fx.FloorColour); floor.SetColor("_B", fx.FloorColour); floor.SetColor("_LineColor", fx.FloorLine);
                floor.SetFloat("_Chequer", 0f); floor.SetFloat("_Cell", 1f); floor.SetFloat("_LineWidth", 0.012f); floor.SetFloat("_Dim", 0.45f); floor.SetFloat("_DimAt", 14f);
                var ground = In(GameObject.CreatePrimitive(PrimitiveType.Plane)); Object.DestroyImmediate(ground.GetComponent<Collider>());
                ground.transform.localScale = new Vector3(30f, 1f, 30f); ground.GetComponent<MeshRenderer>().sharedMaterial = floor;
                var sunGo = In(new GameObject("~StreamSun")); var sun = sunGo.AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.0f;
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
                var cam = In(new GameObject("~StreamCamera")).AddComponent<Camera>();
                cam.scene = scene; cam.enabled = false; cam.nearClipPlane = 0.05f; cam.farClipPlane = 200f;
                cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = fx.FloorColour * 0.45f; cam.allowHDR = false;
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
            catch (Exception failure) { File.WriteAllText(failedPath, failure.ToString()); Debug.LogError("[PaeteAbilityFilm] " + failure); }
            finally
            {
                fx?.Dispose();
                RenderTexture.active = null;
                foreach (var thing in owned) { if (thing == null) continue; if (thing is RenderTexture texture) texture.Release(); Object.DestroyImmediate(thing); }
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        /// <summary>The stream's cost, measured as the current's is (`MeasureRafiCurrent`): eight at once, each posed 2,000
        /// times by a stopwatch, managed memory counted, one counted for draw calls and triangles.</summary>
        private static void MeasureRafiStream(string request)
        {
            Directory.CreateDirectory(OutDir);
            string tag = new string(request.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
            string path = Path.Combine(OutDir, "done_" + tag + ".txt");
            var scene = EditorSceneManager.NewPreviewScene();
            var report = new System.Text.StringBuilder();
            var made = new List<FxStudyRafiStream>();
            try
            {
                var stage = new GameObject("~StreamMeasure"); SceneManager.MoveGameObjectToScene(stage, scene);
                var look = FxStudyRafiWall.Look.Bench; look.Light = RafiWallCurtain.DayLight;
                for (int i = 0; i < 8; i++) made.Add(new FxStudyRafiStream(stage.transform, look, -1f, i * 0.13f, 0));
                foreach (var stream in made) stream.Pose(0.5f, -1f);
                const int poses = 2000;
                long before = GC.GetAllocatedBytesForCurrentThread();
                var clock = System.Diagnostics.Stopwatch.StartNew();
                for (int k = 0; k < poses; k++)
                {
                    float t = (k % 120) / 120f * (FxStudyRafiStream.Life + FxStudyRafiStream.Die);
                    foreach (var stream in made) stream.Pose(t, -1f);
                }
                clock.Stop();
                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                report.AppendLine("posing: " + (clock.Elapsed.TotalMilliseconds * 1000.0 / (poses * (double)made.Count)).ToString("0.00") + " microseconds per stream per frame (" + made.Count + " streams x " + poses + " poses, " + clock.Elapsed.TotalMilliseconds.ToString("0.0") + " ms in all), measured in the editor outside Play");
                report.AppendLine("managed memory allocated while posing: " + allocated + " bytes over " + (poses * made.Count) + " poses");
                made[0].Pose(0.6f, -1f);
                int calls = 0, triangles = 0;
                var materials = new HashSet<Material>(); var meshes = new HashSet<Mesh>();
                foreach (var stream in made)
                    foreach (var renderer in stream.Root.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        var filter = renderer.GetComponent<MeshFilter>();
                        if (renderer.sharedMaterial != null) materials.Add(renderer.sharedMaterial);
                        if (filter != null && filter.sharedMesh != null) meshes.Add(filter.sharedMesh);
                        if (stream != made[0] || !renderer.enabled || filter == null || filter.sharedMesh == null) continue;
                        calls++; triangles += (int)(filter.sharedMesh.GetIndexCount(0) / 3);
                    }
                report.AppendLine("one stream in flight: " + calls + " draw calls, " + triangles + " triangles");
                report.AppendLine("all eight share " + materials.Count + " materials and " + meshes.Count + " meshes");
            }
            catch (Exception failure) { report.AppendLine(failure.ToString()); }
            finally
            {
                foreach (var stream in made) stream.Dispose();
                EditorSceneManager.ClosePreviewScene(scene);
                File.WriteAllText(path, report.ToString());
            }
        }
    }
}
