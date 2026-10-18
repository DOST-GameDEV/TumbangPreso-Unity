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
    /// GLACIAL WALL, FILMED WHERE IT IS PLAYED: Yasmin's reworked wall (`CheskaGlacialWallLook`) on a real map opened
    /// as a PREVIEW scene (the open scene is not touched, nothing is saved, never in Play), in daylight with no bloom,
    /// with her model and a target's for scale, the way `ZackCircuitFilm` films Quick Circuit.
    ///
    /// Each frame is six pictures. TOP: the target's eyes 7 m in front of her (the convex face), her own eyes from
    /// behind it, and the side. BOTTOM: from above, a close three-quarter, and the OLD look (`CheskaIceVisuals.BuildArc`)
    /// from the target's eyes, so old and new are seen at the same moments.
    ///
    /// The wall's life is shortened for the film (default 3 s against the rule's 10 s) so the rise, two slipper hits
    /// and the sink all fit; the rise and the sink are at their real speed. Hits land at 1.2 s and 1.8 s.
    ///
    /// Request: a line `<tag> [life seconds]` in `Temp/cheska-wall-film.request` (its own file). Frames go to
    /// `Logs/paete-ability-film/fxbench_cheskawall_<tag>_frames/`, named like a bench film so `fxvideo.py` joins them,
    /// then `done_fxbench_cheskawall_<tag>.txt` with what it costs, or `failed_...` with the exception.
    /// </summary>
    [InitializeOnLoad]
    internal static class CheskaWallFilmPoll
    {
        private const string Request = "Temp/cheska-wall-film.request";
        private static double _next;
        static CheskaWallFilmPoll() { EditorApplication.update += Poll; }

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
                if (words[0].StartsWith("board")) { PaeteAbilityFilm.RunCheskaBoard(words[0]); continue; }
                if (words[0].StartsWith("pair") && words.Length > 2) { float.TryParse(words[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float pairLife); PaeteAbilityFilm.RunCheskaPair(words[0], Mathf.Clamp(pairLife, 1.5f, 12f), words[2]); continue; }
                float life = 3f;
                if (words.Length > 1) float.TryParse(words[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out life);
                PaeteAbilityFilm.RunCheskaWall(words[0], Mathf.Clamp(life, 1.5f, 12f));
            }
        }
    }

    public static partial class PaeteAbilityFilm
    {
        private const int WallCellW = 480, WallCellH = 270;

        /// <summary>
        /// A STYLE BOARD, not a film: every <see cref="CheskaGlacialWallLook.Style"/> at full height from the target's
        /// eyes and from a close three-quarter, four consecutive frames of its rise from the three-quarter (for an
        /// onion), and one still of the game's own models on the same court in the same light, to judge the wall's
        /// stylisation against. Request a line `board<tag>` in `Temp/cheska-wall-film.request`; pictures go to
        /// `Logs/paete-ability-film/cheskaboard_<tag>/`, then `done_cheskaboard_<tag>.txt`.
        /// </summary>
        internal static void RunCheskaBoard(string tag)
        {
            const string map = "Assets/TumbangPreso/Scenes/Maps/Eskinita.unity";
            Vector3 standAt = new Vector3(-2.5f, 0f, -6.0f);
            tag = new string((tag ?? "board").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray()).ToLowerInvariant();
            string outDir = Path.Combine(OutDir, "cheskaboard_" + tag);
            string donePath = Path.Combine(OutDir, "done_cheskaboard_" + tag + ".txt"), failedPath = Path.Combine(OutDir, "failed_cheskaboard_" + tag + ".txt");
            if (File.Exists(donePath)) File.Delete(donePath);
            if (File.Exists(failedPath)) File.Delete(failedPath);
            if (EditorApplication.isPlayingOrWillChangePlaymode) { File.WriteAllText(failedPath, "The editor is in Play mode."); return; }
            Scene scene = default; bool lit = false;
            var owned = new List<Object>();
            var report = new System.Text.StringBuilder();
            try
            {
                Directory.CreateDirectory(outDir);
                foreach (string old in Directory.GetFiles(outDir)) File.Delete(old);
                scene = EditorSceneManager.OpenPreviewScene(map);
                GameObject In(GameObject go) { SceneManager.MoveGameObjectToScene(go, scene); return go; }
                lit = Unsupported.SetOverrideLightingSettings(scene);
                Shader.SetGlobalFloat("_CharacterSmoothShade", ToonSkin.CharacterSmoothShade);
                Shader.SetGlobalFloat("_FxStudyBloom", 0f);
                var cam = In(new GameObject("~BoardCamera")).AddComponent<Camera>();
                cam.scene = scene; cam.enabled = false; cam.allowHDR = false; cam.nearClipPlane = 0.06f; cam.farClipPlane = 400f;
                cam.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.55f, 0.70f, 0.88f);
                Vector3 forward = Vector3.forward, right = Vector3.right;
                Vector3 stand = standAt, wallAt = stand + forward * 3f, targetAt = stand + forward * 7f;
                GameObject Person(string id, Vector3 at, float faces)
                {
                    var holder = In(new GameObject("~Board-" + id));
                    holder.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, faces, 0f));
                    var art = RosterBook.Load()?.FindPersonArt(id);
                    if (art == null || art.Model == null) return holder;
                    var model = (GameObject)Object.Instantiate(art.Model, holder.transform);
                    model.transform.localRotation = Quaternion.Euler(0f, CharacterVisual.PersonModelYaw, 0f);
                    model.transform.localScale = Vector3.one * CharacterVisual.PersonScale;
                    ToonSkin.Apply(model, ToonSkin.PersonOutlineWidth, art.Palette);
                    foreach (var animator in model.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
                    return holder;
                }
                var her = Person("cheska", stand, 0f);
                var target = Person("dante", targetAt, 180f);
                var rt = new RenderTexture(WallCellW * 2, WallCellH * 2, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                owned.Add(rt);
                var shot = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                owned.Add(shot);
                cam.targetTexture = rt;
                void Shoot(string name, Vector3 eye, Vector3 look, float fov)
                {
                    cam.fieldOfView = fov;
                    cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye, Vector3.up));
                    cam.Render();
                    var keep = RenderTexture.active; RenderTexture.active = rt;
                    shot.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); shot.Apply();
                    RenderTexture.active = keep;
                    File.WriteAllBytes(Path.Combine(outDir, name + ".png"), shot.EncodeToPNG());
                }
                Vector3 mid = wallAt + Vector3.up * 0.95f;
                Vector3 targetEye = targetAt + Vector3.up * 1.5f - forward * 0.15f;
                Vector3 quarter = wallAt + forward * 3.4f + right * 2.6f + Vector3.up * 1.5f;
                // The game's own models, close, in the same light: the stylisation the wall has to match.
                Shoot("models", stand + forward * 2.4f + right * 2.2f + Vector3.up * 1.3f, stand + Vector3.up * 0.8f, 45f);
                Shoot("models_target", targetAt - forward * 2.4f + right * 2.2f + Vector3.up * 1.3f, targetAt + Vector3.up * 0.8f, 45f);
                foreach (CheskaGlacialWallLook.Style style in Enum.GetValues(typeof(CheskaGlacialWallLook.Style)))
                {
                    var root = In(new GameObject("~BoardWall-" + style));
                    root.transform.SetPositionAndRotation(wallAt, Quaternion.LookRotation(forward));
                    var wall = CheskaGlacialWallLook.Build(root.transform, Core.CryoRules.GlacialWallArcLength, Core.CryoRules.GlacialWallArcRadius, style);
                    int tris = 0, calls = 0;
                    foreach (var r in wall.Root.GetComponentsInChildren<MeshFilter>(true)) { calls++; if (r.sharedMesh != null) tris += (int)(r.sharedMesh.GetIndexCount(0) / 3); }
                    report.AppendLine(style + ": " + calls + " draw calls, " + tris + " triangles");
                    CheskaGlacialWallLook.Pose(wall, 1.3f, 10f, 0f);
                    target.SetActive(false);
                    Shoot(style + "_target", targetEye, mid, 70f);
                    target.SetActive(true);
                    Shoot(style + "_quarter", quarter, mid, 50f);
                    Shoot(style + "_side", wallAt + right * 7.5f + Vector3.up * 1.7f + forward * 0.3f, mid, 45f);
                    for (int f = 0; f < 4; f++)
                    {
                        CheskaGlacialWallLook.Pose(wall, 0.12f + f / 60f, 10f, 0f);
                        Shoot(style + "_rise" + f, quarter, mid, 50f);
                    }
                    // A close still of every style at one framing, to set the in-between surfaces beside Crystal.
                    CheskaGlacialWallLook.Pose(wall, 2.0f, 10f, 0f);
                    Shoot(style + "_close", wallAt + forward * 2.2f + right * 1.2f + Vector3.up * 1.3f, mid, 40f);
                    // The in-between styles live while they stand: four moments 0.1 s apart, close, to see the shimmer and sparkle move.
                    if (style >= CheskaGlacialWallLook.Style.BetweenA)
                        for (int f = 0; f < 4; f++)
                        {
                            CheskaGlacialWallLook.Pose(wall, 2.0f + f * 0.1f, 10f, 0f);
                            Shoot(style + "_live" + f, wallAt + forward * 2.2f + right * 1.2f + Vector3.up * 1.3f, mid, 40f);
                        }
                    Object.DestroyImmediate(root);
                }
                report.AppendLine("bake: " + CheskaGlacialWallLook.BakeReport);
                File.WriteAllText(donePath, report.ToString());
                Debug.Log("[CheskaWallFilm] board " + tag + " written to " + outDir);
            }
            catch (Exception failure) { File.WriteAllText(failedPath, report + "\n" + failure); Debug.LogError("[CheskaWallFilm] " + failure); }
            finally
            {
                if (lit) Unsupported.RestoreOverrideLightingSettings();
                RenderTexture.active = null;
                foreach (var thing in owned) { if (thing == null) continue; if (thing is RenderTexture texture) texture.Release(); Object.DestroyImmediate(thing); }
                if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        /// <summary>
        /// STYLES SIDE BY SIDE, IN MOTION: the whole life of the wall (rise, standing, two slipper hits, sink) at 60 a
        /// second, for each style named in <paramref name="styleList"/> (comma separated), one column a style. Top row the
        /// target's eyes, bottom row the close three-quarter. Same map, light and models as the board. Request a line
        /// `pair<tag> <life> <Style,Style,...>` in `Temp/cheska-wall-film.request`; frames go to
        /// `Logs/paete-ability-film/fxbench_cheskapair_<tag>_frames/`, then `done_fxbench_cheskapair_<tag>.txt`.
        /// </summary>
        internal static void RunCheskaPair(string tag, float life, string styleList)
        {
            const string map = "Assets/TumbangPreso/Scenes/Maps/Eskinita.unity";
            Vector3 standAt = new Vector3(-2.5f, 0f, -6.0f);
            tag = new string((tag ?? "pair").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray()).ToLowerInvariant();
            string named = "fxbench_cheskapair_" + tag;
            string donePath = Path.Combine(OutDir, "done_" + named + ".txt"), failedPath = Path.Combine(OutDir, "failed_" + named + ".txt");
            if (File.Exists(donePath)) File.Delete(donePath);
            if (File.Exists(failedPath)) File.Delete(failedPath);
            if (EditorApplication.isPlayingOrWillChangePlaymode) { File.WriteAllText(failedPath, "The editor is in Play mode."); return; }
            Scene scene = default; bool lit = false;
            var owned = new List<Object>();
            var report = new System.Text.StringBuilder();
            try
            {
                var styles = styleList.Split(',').Select(n => (CheskaGlacialWallLook.Style)Enum.Parse(typeof(CheskaGlacialWallLook.Style), n.Trim())).ToArray();
                int cw = styles.Length <= 2 ? 640 : 480, ch = cw * 9 / 16;
                scene = EditorSceneManager.OpenPreviewScene(map);
                GameObject In(GameObject go) { SceneManager.MoveGameObjectToScene(go, scene); return go; }
                lit = Unsupported.SetOverrideLightingSettings(scene);
                Shader.SetGlobalFloat("_CharacterSmoothShade", ToonSkin.CharacterSmoothShade);
                Shader.SetGlobalFloat("_FxStudyBloom", 0f);
                var cam = In(new GameObject("~PairCamera")).AddComponent<Camera>();
                cam.scene = scene; cam.enabled = false; cam.allowHDR = false; cam.nearClipPlane = 0.06f; cam.farClipPlane = 400f;
                cam.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.55f, 0.70f, 0.88f);
                Vector3 forward = Vector3.forward, right = Vector3.right;
                Vector3 stand = standAt, wallAt = stand + forward * 3f, targetAt = stand + forward * 7f;
                GameObject Person(string id, Vector3 at, float faces)
                {
                    var holder = In(new GameObject("~Pair-" + id));
                    holder.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, faces, 0f));
                    var art = RosterBook.Load()?.FindPersonArt(id);
                    if (art == null || art.Model == null) return holder;
                    var model = (GameObject)Object.Instantiate(art.Model, holder.transform);
                    model.transform.localRotation = Quaternion.Euler(0f, CharacterVisual.PersonModelYaw, 0f);
                    model.transform.localScale = Vector3.one * CharacterVisual.PersonScale;
                    ToonSkin.Apply(model, ToonSkin.PersonOutlineWidth, art.Palette);
                    foreach (var animator in model.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
                    return holder;
                }
                Person("cheska", stand, 0f);
                var target = Person("dante", targetAt, 180f);
                var walls = new CheskaGlacialWallLook.Wall[styles.Length];
                for (int s = 0; s < styles.Length; s++)
                {
                    var root = In(new GameObject("~PairWall-" + styles[s]));
                    root.transform.SetPositionAndRotation(wallAt, Quaternion.LookRotation(forward));
                    walls[s] = CheskaGlacialWallLook.Build(root.transform, Core.CryoRules.GlacialWallArcLength, Core.CryoRules.GlacialWallArcRadius, styles[s]);
                    int tris = 0, calls = 0;
                    foreach (var r in walls[s].Root.GetComponentsInChildren<MeshFilter>(true)) { calls++; if (r.sharedMesh != null) tris += (int)(r.sharedMesh.GetIndexCount(0) / 3); }
                    report.AppendLine(styles[s] + ": " + calls + " draw calls, " + tris + " triangles");
                    walls[s].Root.SetActive(false);
                }
                var rt = new RenderTexture(cw, ch, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                owned.Add(rt);
                var cell = new Texture2D(cw, ch, TextureFormat.RGB24, false);
                var sheet = new Texture2D(cw * styles.Length, ch * 2, TextureFormat.RGB24, false);
                owned.Add(cell); owned.Add(sheet);
                string frames = Path.Combine(OutDir, named + "_frames");
                if (Directory.Exists(frames)) foreach (string old in Directory.GetFiles(frames)) File.Delete(old);
                Directory.CreateDirectory(frames);
                cam.targetTexture = rt;
                void Shoot(Vector3 eye, Vector3 look, float fov, int column, int row)
                {
                    cam.fieldOfView = fov;
                    cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye, Vector3.up));
                    cam.Render();
                    var keep = RenderTexture.active; RenderTexture.active = rt;
                    cell.ReadPixels(new Rect(0, 0, cw, ch), 0, 0); cell.Apply();
                    RenderTexture.active = keep;
                    sheet.SetPixels(column * cw, (1 - row) * ch, cw, ch, cell.GetPixels());
                }
                const float lead = 0.15f;
                float[] hits = { 1.2f, 1.8f };
                int count = Mathf.CeilToInt((lead + life + 0.25f) * BenchRate) + 1;
                Vector3 mid = wallAt + Vector3.up * 0.95f;
                Vector3 targetEye = targetAt + Vector3.up * 1.5f - forward * 0.15f;
                Vector3 quarter = wallAt + forward * 3.4f + right * 2.6f + Vector3.up * 1.5f;
                for (int f = 0; f < count; f++)
                {
                    float age = f / (float)BenchRate - lead;
                    float flash = 0f;
                    foreach (float h in hits) if (age >= h) flash = Mathf.Max(flash, 1f - (age - h) / 0.28f);
                    flash = Mathf.Clamp01(flash);
                    for (int s = 0; s < styles.Length; s++)
                    {
                        walls[s].Root.SetActive(age >= 0f);
                        CheskaGlacialWallLook.Pose(walls[s], Mathf.Max(0f, age), life, flash);
                        target.SetActive(false);
                        Shoot(targetEye, mid, 70f, s, 0);
                        target.SetActive(true);
                        Shoot(quarter, mid, 50f, s, 1);
                        walls[s].Root.SetActive(false);
                    }
                    sheet.Apply();
                    File.WriteAllBytes(Path.Combine(frames, "f" + f.ToString("000") + ".png"), sheet.EncodeToPNG());
                }
                report.AppendLine(count + " frames at " + BenchRate + " a second; life " + life.ToString("0.0") + " s (shortened for the film), hits at 1.2 and 1.8 s; columns " + styleList + "; top the target's eyes, bottom the close three-quarter; no bloom");
                File.WriteAllText(donePath, report.ToString());
            }
            catch (Exception failure) { File.WriteAllText(failedPath, report + "\n" + failure); Debug.LogError("[CheskaWallFilm] " + failure); }
            finally
            {
                if (lit) Unsupported.RestoreOverrideLightingSettings();
                RenderTexture.active = null;
                foreach (var thing in owned) { if (thing == null) continue; if (thing is RenderTexture texture) texture.Release(); Object.DestroyImmediate(thing); }
                if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        internal static void RunCheskaWall(string tag, float life)
        {
            const string map = "Assets/TumbangPreso/Scenes/Maps/Eskinita.unity";
            Vector3 standAt = new Vector3(-2.5f, 0f, -6.0f);
            const float yaw = 0f, aimAhead = 3.0f, targetAhead = 7.0f;
            Directory.CreateDirectory(OutDir);
            tag = new string((tag ?? "v00").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray()).ToLowerInvariant();
            if (tag.Length == 0) tag = "v00";
            string named = "fxbench_cheskawall_" + tag;
            string donePath = Path.Combine(OutDir, "done_" + named + ".txt"), failedPath = Path.Combine(OutDir, "failed_" + named + ".txt");
            if (File.Exists(donePath)) File.Delete(donePath);
            if (File.Exists(failedPath)) File.Delete(failedPath);
            if (EditorApplication.isPlayingOrWillChangePlaymode) { File.WriteAllText(failedPath, "The editor is in Play mode. The film only runs outside it."); return; }

            Scene scene = default;
            bool lit = false;
            var owned = new List<Object>();
            var report = new System.Text.StringBuilder();
            try
            {
                scene = EditorSceneManager.OpenPreviewScene(map);
                if (!scene.IsValid()) throw new InvalidOperationException("No map to film on.");
                GameObject In(GameObject go) { SceneManager.MoveGameObjectToScene(go, scene); return go; }
                lit = Unsupported.SetOverrideLightingSettings(scene);
                Shader.SetGlobalFloat("_CharacterSmoothShade", ToonSkin.CharacterSmoothShade);
                Shader.SetGlobalFloat("_FxStudyBloom", 0f);

                var cam = In(new GameObject("~WallFilmCamera")).AddComponent<Camera>();
                cam.scene = scene; cam.enabled = false; cam.allowHDR = false;
                cam.nearClipPlane = 0.06f; cam.farClipPlane = 400f;
                cam.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.55f, 0.70f, 0.88f);

                Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward, right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
                float Road(Vector3 at)
                {
                    var physics = scene.GetPhysicsScene();
                    return physics.IsValid() && physics.Raycast(at + Vector3.up * 30f, Vector3.down, out var hit, 80f, ~0, QueryTriggerInteraction.Ignore) ? hit.point.y : 0f;
                }
                Vector3 stand = new Vector3(standAt.x, 0f, standAt.z); stand.y = Road(stand);
                Vector3 wallAt = stand + forward * aimAhead; wallAt.y = Road(wallAt);
                Vector3 targetAt = stand + forward * targetAhead; targetAt.y = Road(targetAt);
                report.AppendLine("map " + map + "; she stands at (" + stand.x.ToString("0.0") + ", " + stand.y.ToString("0.00") + ", " + stand.z.ToString("0.0") + "), the wall's aimed point " + aimAhead + " m ahead at road height " + wallAt.y.ToString("0.00"));

                GameObject Person(string id, Vector3 at, float faces, out GameObject model)
                {
                    model = null;
                    var holder = In(new GameObject("~WallFilm-" + id));
                    holder.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, faces, 0f));
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
                Person("cheska", stand, yaw, out var herModel);
                var target = Person("dante", targetAt, yaw + 180f, out _);

                var newRoot = In(new GameObject("~GlacialWallNew"));
                newRoot.transform.SetPositionAndRotation(wallAt, Quaternion.LookRotation(forward));
                var oldRoot = In(new GameObject("~GlacialWallOld"));
                oldRoot.transform.SetPositionAndRotation(wallAt, Quaternion.LookRotation(forward));
                CheskaIceVisuals.BuildArc(oldRoot.transform, Core.CryoRules.GlacialWallArcLength, Core.CryoRules.GlacialWallArcRadius, renderOnly: true);

                var watch = new System.Diagnostics.Stopwatch();
                int meshesBefore = Resources.FindObjectsOfTypeAll<Mesh>().Length, materialsBefore = Resources.FindObjectsOfTypeAll<Material>().Length;
                watch.Restart();
                var wall = CheskaGlacialWallLook.Build(newRoot.transform, Core.CryoRules.GlacialWallArcLength, Core.CryoRules.GlacialWallArcRadius);
                double firstBuild = watch.Elapsed.TotalMilliseconds;
                report.AppendLine("bake: " + CheskaGlacialWallLook.BakeReport);
                var again = new List<string>();
                for (int i = 0; i < 6; i++)
                {
                    watch.Restart();
                    var spare = CheskaGlacialWallLook.Build(newRoot.transform, Core.CryoRules.GlacialWallArcLength, Core.CryoRules.GlacialWallArcRadius);
                    double made = watch.Elapsed.TotalMilliseconds;
                    watch.Restart(); Object.DestroyImmediate(spare.Root); double gone = watch.Elapsed.TotalMilliseconds;
                    again.Add(made.ToString("0.000") + "+" + gone.ToString("0.000"));
                }
                int calls = 0, triangles = 0; var mats = new HashSet<Material>();
                foreach (var r in wall.Root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    calls++; mats.Add(r.sharedMaterial);
                    var mf = r.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null) triangles += (int)(mf.sharedMesh.GetIndexCount(0) / 3);
                }
                int oldCalls = 0, oldTris = 0;
                foreach (var r in oldRoot.GetComponentsInChildren<Renderer>(true))
                {
                    oldCalls++;
                    var mf = r.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null) oldTris += (int)(mf.sharedMesh.GetIndexCount(0) / 3);
                }
                report.AppendLine("MEASURED in the editor: first cast (bakes the shared meshes) " + firstBuild.ToString("0.00") + " ms; six more casts, make+free (ms): " + string.Join("  ", again));
                report.AppendLine("draw calls " + calls + " (" + mats.Count + " materials, shared by every wall), triangles " + triangles + "; the old look: " + oldCalls + " renderers (5 slabs + 10 line renderers), " + oldTris + " mesh triangles");

                var rt = new RenderTexture(WallCellW, WallCellH, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                owned.Add(rt);
                var cell = new Texture2D(WallCellW, WallCellH, TextureFormat.RGB24, false);
                var sheet = new Texture2D(WallCellW * 3, WallCellH * 2, TextureFormat.RGB24, false);
                owned.Add(cell); owned.Add(sheet);
                string frames = Path.Combine(OutDir, named + "_frames");
                if (Directory.Exists(frames)) foreach (string old in Directory.GetFiles(frames)) File.Delete(old);
                Directory.CreateDirectory(frames);
                cam.targetTexture = rt;

                void Shoot(Vector3 eye, Vector3 look, float fov, int column, int row)
                {
                    cam.fieldOfView = fov;
                    cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye, Vector3.up));
                    cam.Render();
                    var keep = RenderTexture.active; RenderTexture.active = rt;
                    cell.ReadPixels(new Rect(0, 0, WallCellW, WallCellH), 0, 0); cell.Apply();
                    RenderTexture.active = keep;
                    sheet.SetPixels(column * WallCellW, (1 - row) * WallCellH, WallCellW, WallCellH, cell.GetPixels());
                }

                const float lead = 0.15f;
                float[] hits = { 1.2f, 1.8f };
                float seconds = lead + life + 0.25f;
                int count = Mathf.CeilToInt(seconds * BenchRate) + 1;
                var poseMs = new List<double>();
                Vector3 mid = wallAt + Vector3.up * 0.95f;
                for (int f = 0; f < count; f++)
                {
                    float age = f / (float)BenchRate - lead;
                    float flash = 0f;
                    foreach (float h in hits) if (age >= h) flash = Mathf.Max(flash, 1f - (age - h) / 0.28f);
                    flash = Mathf.Clamp01(flash);
                    bool newOn = age >= 0f, oldOn = age >= 0f && age < life;
                    watch.Restart();
                    CheskaGlacialWallLook.Pose(wall, Mathf.Max(0f, age), life, flash);
                    poseMs.Add(watch.Elapsed.TotalMilliseconds);
                    newRoot.SetActive(newOn); oldRoot.SetActive(false);
                    Vector3 targetEye = targetAt + Vector3.up * 1.5f - forward * 0.15f;
                    if (herModel != null) herModel.SetActive(true);
                    // Nobody sees their own body from behind their own eyes (g1 filmed the inside of his head).
                    target.SetActive(false);
                    Shoot(targetEye, mid, 70f, 0, 0);
                    target.SetActive(true);
                    if (herModel != null) herModel.SetActive(false);
                    Shoot(stand + Vector3.up * 1.5f + forward * 0.12f, mid - Vector3.up * 0.1f, 70f, 1, 0);
                    if (herModel != null) herModel.SetActive(true);
                    Shoot(wallAt + right * 7.5f + Vector3.up * 1.7f + forward * 0.3f, mid, 45f, 2, 0);
                    Shoot(wallAt + Vector3.up * 9.5f - forward * 2.6f, wallAt, 50f, 0, 1);
                    Shoot(wallAt + forward * 3.4f + right * 2.6f + Vector3.up * 1.5f, mid, 50f, 1, 1);
                    newRoot.SetActive(false); oldRoot.SetActive(oldOn);
                    target.SetActive(false);
                    Shoot(targetEye, mid, 70f, 2, 1);
                    target.SetActive(true);
                    oldRoot.SetActive(false);
                    sheet.Apply();
                    File.WriteAllBytes(Path.Combine(frames, "f" + f.ToString("000") + ".png"), sheet.EncodeToPNG());
                }
                report.AppendLine(count + " frames at " + BenchRate + " a second: " + lead + " s before the cast, a life of " + life.ToString("0.0") + " s (the rule's is 10 s; shortened for the film), hits at " + string.Join(", ", hits) + " s");
                report.AppendLine("pose a frame (C#, five property blocks): mean " + poseMs.Average().ToString("0.0000") + " ms, worst " + poseMs.Max().ToString("0.0000") + " ms");
                report.AppendLine("cells: top = target's eyes, her eyes, side; bottom = above, close three-quarter, OLD look from the target's eyes. No bloom.");
                Object.DestroyImmediate(wall.Root);
                report.AppendLine("after freeing: " + (Resources.FindObjectsOfTypeAll<Mesh>().Length - meshesBefore) + " meshes and " + (Resources.FindObjectsOfTypeAll<Material>().Length - materialsBefore) + " materials more than before (the shared bake, kept on purpose, and the old look's)");
                report.AppendLine(lit ? "lit by the map's own lighting settings" : "the map's lighting settings could not be put in force");
                File.WriteAllText(donePath, report.ToString());
                Debug.Log("[CheskaWallFilm] " + named + " written to " + OutDir);
            }
            catch (Exception failure)
            {
                File.WriteAllText(failedPath, report + "\n" + failure);
                Debug.LogError("[CheskaWallFilm] " + failure);
            }
            finally
            {
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
