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
    /// THE CLOUD ON THE COURT, IN THE MAP'S OWN LIGHT. It stands the bench cloud's two columns (`FxStudyCloud`) on the real
    /// map beside a character and films them two ways side by side:
    ///
    ///   the cloud without its line  |  the cloud with its line
    ///
    /// over two rows: the columns whole, and close on their lobes.
    ///
    /// IT DOES NOT SHOW THE GAME'S AMBIENT OCCLUSION, AND NO COLUMN OF IT MAY BE CALLED THAT. That pass (`WorldOutline`, on
    /// a match camera) was put on this camera three times and measured each time against the same camera without it:
    /// the street differed by 0.00, 0.00 and 0.01 of 255. It draws in Play and not in a film made outside it. The owner
    /// then saw the cloud under it in Play and chose a line of the cloud's own instead ("i feel like an outline shader
    /// would work better instead of ao"), and the cloud now keeps out of that pass altogether.
    ///
    /// `Temp/paete-ability-film.request` line: `cloudcourt <tag>`. Frames go to `fxbench_cloudcourt_<tag>_frames/`.
    /// </summary>
    public static partial class PaeteAbilityFilm
    {
        /// <summary>
        /// WHAT THE CLOUD COSTS A FRAME, MEASURED. The owner, of the cloud stood in a match: "the cloud is really laggy". The
        /// bench films are not real time, so nothing had ever timed it. `cloudcourt cost<anything>` builds the two columns
        /// twice (without their line and with it), poses each three hundred times at sixty frames a second with a stopwatch
        /// round the posing alone, and writes the milliseconds a frame for ONE column, the managed memory the posing asked
        /// for, and the triangles in its meshes. Nothing is rendered: this is the processor's share and not the card's.
        /// </summary>
        private static void RunCloudCost(string tag)
        {
            string named = "fxbench_cloudcourt_" + tag;
            string donePath = Path.Combine(OutDir, "done_" + named + ".txt"), failedPath = Path.Combine(OutDir, "failed_" + named + ".txt");
            Directory.CreateDirectory(OutDir);
            if (File.Exists(donePath)) File.Delete(donePath);
            if (File.Exists(failedPath)) File.Delete(failedPath);
            var report = new System.Text.StringBuilder();
            var host = new GameObject("~CloudCost") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                const int Frames = 300;
                foreach (bool line in new[] { false, true })
                {
                    var cloud = new FxStudyCloud(host.transform, false, line ? FxStudyCloud.Line : 0f);
                    try
                    {
                        Vector3 eye = new Vector3(0f, 2f, -9f);
                        for (int f = 0; f < 40; f++) cloud.Sample(f / 60f, eye);
                        long before = GC.GetAllocatedBytesForCurrentThread(); int collections = GC.CollectionCount(0);
                        var watch = System.Diagnostics.Stopwatch.StartNew();
                        for (int f = 0; f < Frames; f++) cloud.Sample(40f / 60f + f / 60f, eye);
                        watch.Stop();
                        long asked = GC.GetAllocatedBytesForCurrentThread() - before; collections = GC.CollectionCount(0) - collections;
                        long triangles = 0; int meshes = 0;
                        foreach (var filter in cloud.Root.GetComponentsInChildren<MeshFilter>(true))
                            if (filter.sharedMesh != null && filter.sharedMesh.subMeshCount > 0) { triangles += filter.sharedMesh.GetIndexCount(0) / 3; meshes++; }
                        double each = watch.Elapsed.TotalMilliseconds / Frames;
                        report.AppendLine((line ? "WITH ITS LINE (as F12 stands it)" : "WITHOUT ITS LINE")
                            + ": " + (each / 2.0).ToString("0.000") + " ms a frame for one column (" + each.ToString("0.000") + " ms for the two), over " + Frames + " frames; "
                            + (asked / (double)Frames).ToString("0") + " bytes of managed memory asked for a frame, " + collections + " collections; "
                            + triangles + " triangles in " + meshes + " meshes (each drawn " + (line ? "twice: bodies, then the line" : "once") + ")");
                    }
                    finally { cloud.Dispose(); }
                }
                // What it costs to MAKE one: the bench's two columns of 130 balls, each with a mesh of its own; and a
                // small cloud from `FxCloud.Build`, whose mesh is made once and shared by every later one.
                var make = System.Diagnostics.Stopwatch.StartNew();
                for (int i = 0; i < 20; i++) new FxStudyCloud(host.transform, false, FxStudyCloud.Line).Dispose();
                make.Stop();
                report.AppendLine("TO MAKE the two columns of 130 balls, each its own mesh: " + (make.Elapsed.TotalMilliseconds / 20.0).ToString("0.000") + " ms (so one column " + (make.Elapsed.TotalMilliseconds / 40.0).ToString("0.000") + " ms), mean of 20");
                foreach (int balls in new[] { 16, 48 })
                {
                    var first = System.Diagnostics.Stopwatch.StartNew();
                    var one = FxCloud.Build(host.transform, "puff", 900 + balls, balls, Color.white, Color.grey, Color.black, FxStudyCloud.Line);
                    first.Stop(); int triangles = one.Triangles; one.Dispose();
                    long before = GC.GetAllocatedBytesForCurrentThread();
                    var later = System.Diagnostics.Stopwatch.StartNew();
                    for (int i = 0; i < 200; i++) FxCloud.Build(host.transform, "puff", 900 + balls, balls, Color.white, Color.grey, Color.black, FxStudyCloud.Line).Dispose();
                    later.Stop();
                    long asked = GC.GetAllocatedBytesForCurrentThread() - before;
                    report.AppendLine("TO MAKE a cloud of " + balls + " balls with FxCloud.Build (" + triangles + " triangles): the first, which makes the shared mesh, " + first.Elapsed.TotalMilliseconds.ToString("0.000")
                        + " ms; each later one " + (later.Elapsed.TotalMilliseconds / 200.0).ToString("0.0000") + " ms and " + (asked / 200) + " bytes, mean of 200 (made and freed)");
                }
                File.WriteAllText(donePath, report.ToString());
            }
            catch (Exception failure) { File.WriteAllText(failedPath, report + "\n" + failure); }
            finally { Object.DestroyImmediate(host); }
        }

        /// <summary>
        /// WHAT A LANDING'S DUST COSTS, MEASURED: the flat dust (`CourtContactDust`: an object and a particle system built
        /// on every landing) beside the cloud puffs (`CloudPuffField`: a row written). `cloudcourt puffcost<anything>`.
        /// A frame here is EIGHT landings at once, two hundred frames. Only the processor's share of asking for the
        /// dust is timed; what the particle system then costs the engine each frame cannot be timed from here.
        /// </summary>
        private static void RunPuffCost(string tag)
        {
            string named = "fxbench_cloudcourt_" + tag;
            string donePath = Path.Combine(OutDir, "done_" + named + ".txt"), failedPath = Path.Combine(OutDir, "failed_" + named + ".txt");
            Directory.CreateDirectory(OutDir);
            if (File.Exists(donePath)) File.Delete(donePath);
            if (File.Exists(failedPath)) File.Delete(failedPath);
            var report = new System.Text.StringBuilder();
            var host = new GameObject("~PuffCost") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                const int Frames = 200, Each = 8;
                var field = new CloudPuffField(host.transform);
                for (int i = 0; i < 30; i++) field.Spawn(CloudPuffField.Puff.Landing(new Vector3(i, 0f, 0f)), i * 0.01f);
                long held = GC.GetTotalMemory(false); int collections = GC.CollectionCount(0);
                var watch = System.Diagnostics.Stopwatch.StartNew();
                for (int f = 0; f < Frames; f++)
                {
                    float now = 1f + f / 60f;
                    for (int k = 0; k < Each; k++) field.Spawn(CloudPuffField.Puff.Landing(new Vector3(k, 0f, f)), now);
                    field.Tick(now);
                }
                watch.Stop();
                long grew = GC.GetTotalMemory(false) - held; collections = GC.CollectionCount(0) - collections;
                var idle = System.Diagnostics.Stopwatch.StartNew();
                for (int f = 0; f < 2000; f++) field.Tick(5f + (f % 10) * 0.01f);
                idle.Stop();
                report.AppendLine("CLOUD PUFFS: " + (watch.Elapsed.TotalMilliseconds / Frames).ToString("0.0000") + " ms a frame with " + Each + " landings in it ("
                    + (watch.Elapsed.TotalMilliseconds / Frames / Each * 1000.0).ToString("0.00") + " microseconds a landing); the managed heap grew " + grew + " bytes over " + (Frames * Each)
                    + " landings, " + collections + " collections; a frame with dust in the air and no landing " + (idle.Elapsed.TotalMilliseconds / 2000.0 * 1000.0).ToString("0.000")
                    + " microseconds; one mesh of " + CloudPuffField.Triangles + " triangles and one material for all " + CloudPuffField.Slots + " puffs, drawn twice (balls, line): 2 draw calls however many are live");
                field.Dispose();

                // The flat dust needs ground under it: look for some in whatever scene is open.
                Vector3 ground = Vector3.zero; bool found = false;
                foreach (var probe in new[] { Vector3.zero, new Vector3(3f, 0f, 3f), new Vector3(-4f, 0f, 2f), new Vector3(0f, 0f, 8f), new Vector3(10f, 0f, 0f) })
                    if (Physics.Raycast(probe + Vector3.up * 40f, Vector3.down, out var hit, 80f, ~0, QueryTriggerInteraction.Ignore) && hit.normal.y > 0.65f) { ground = hit.point; found = true; break; }
                if (!found) report.AppendLine("FLAT DUST: no ground found under five points of the open scene, so it could not be asked for here.");
                else
                {
                    var made = new List<CourtContactDust>();
                    var first = CourtContactDust.Play("land", ground + Vector3.up * 0.1f, host.transform);
                    if (first == null) report.AppendLine("FLAT DUST: the call returned nothing at " + ground + " (its own gates refused), so it could not be timed.");
                    else
                    {
                        Object.DestroyImmediate(first.gameObject);
                        held = GC.GetTotalMemory(false); collections = GC.CollectionCount(0);
                        var old = new System.Diagnostics.Stopwatch();
                        for (int f = 0; f < Frames; f++)
                        {
                            old.Start();
                            for (int k = 0; k < Each; k++) made.Add(CourtContactDust.Play("land", ground + Vector3.up * 0.1f, host.transform));
                            old.Stop();
                            if (f % 4 == 3) { foreach (var dust in made) if (dust != null) Object.DestroyImmediate(dust.gameObject); made.Clear(); }
                        }
                        grew = GC.GetTotalMemory(false) - held; collections = GC.CollectionCount(0) - collections;
                        report.AppendLine("FLAT DUST (as a replay asks for it; a match also starts the particle system): " + (old.Elapsed.TotalMilliseconds / Frames).ToString("0.0000") + " ms a frame with " + Each
                            + " landings in it (" + (old.Elapsed.TotalMilliseconds / Frames / Each * 1000.0).ToString("0.00") + " microseconds a landing); the managed heap grew " + grew + " bytes over "
                            + (Frames * Each) + " landings, " + collections + " collections; one object, one particle system and one draw call for each puff live (up to 8 flat squares each)");
                    }
                }
                File.WriteAllText(donePath, report.ToString());
            }
            catch (Exception failure) { File.WriteAllText(failedPath, report + "\n" + failure); }
            finally { Object.DestroyImmediate(host); }
        }

        /// <summary>
        /// WHAT FEATHERFALL COSTS A FRAME, MEASURED (`cloudcourt feathercost<anything>`): the effect as a match would stand
        /// it, posed three hundred times through a whole flight with a stopwatch round the posing; what it costs to make;
        /// and how many pieces are drawn and how many triangles they are at the hold.
        /// </summary>
        private static void RunFeatherCost(string tag)
        {
            string named = "fxbench_cloudcourt_" + tag;
            string donePath = Path.Combine(OutDir, "done_" + named + ".txt"), failedPath = Path.Combine(OutDir, "failed_" + named + ".txt");
            Directory.CreateDirectory(OutDir);
            if (File.Exists(donePath)) File.Delete(donePath);
            if (File.Exists(failedPath)) File.Delete(failedPath);
            var report = new System.Text.StringBuilder();
            var host = new GameObject("~FeatherCost") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                var make = System.Diagnostics.Stopwatch.StartNew();
                for (int i = 0; i < 10; i++) new FxStudyFeatherfall(host.transform, 0.5f, false).Dispose();
                make.Stop();
                var fx = new FxStudyFeatherfall(host.transform, 0.5f, false);
                const int Frames = 300;
                for (int f = 0; f < 30; f++) fx.Pose(f / 60f, 2.8f, 0.5f, f / 60f, -1f);
                long held = GC.GetTotalMemory(false); int collections = GC.CollectionCount(0);
                var watch = System.Diagnostics.Stopwatch.StartNew();
                for (int f = 0; f < Frames; f++) { float t = 1f + f / 60f; fx.Pose(t, 2.8f, 1f - t / 6f, t, -1f); }
                watch.Stop();
                long grew = GC.GetTotalMemory(false) - held; collections = GC.CollectionCount(0) - collections;
                int drawn = 0; long triangles = 0;
                foreach (var r in fx.Root.GetComponentsInChildren<MeshRenderer>(false))
                {
                    if (!r.enabled) continue; var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
                    drawn++; triangles += mf.sharedMesh.GetIndexCount(0) / 3;
                }
                report.AppendLine("FEATHERFALL at the hold: " + (watch.Elapsed.TotalMilliseconds / Frames).ToString("0.0000") + " ms a frame to pose, over " + Frames + " frames; managed heap grew " + grew + " bytes, "
                    + collections + " collections; " + drawn + " pieces drawn (one draw call each), " + triangles + " triangles; to make one: " + (make.Elapsed.TotalMilliseconds / 10.0).ToString("0.00") + " ms (mean of 10)");
                fx.Dispose();
                File.WriteAllText(donePath, report.ToString());
            }
            catch (Exception failure) { File.WriteAllText(failedPath, report + "\n" + failure); }
            finally { Object.DestroyImmediate(host); }
        }

        private static void RunCloudCourt(string tag)
        {
            if ((tag ?? "").StartsWith("audit", StringComparison.OrdinalIgnoreCase)) { RunAudit(tag); return; }
            if ((tag ?? "").StartsWith("feathercost", StringComparison.OrdinalIgnoreCase)) { RunFeatherCost(tag); return; }
            if ((tag ?? "").StartsWith("puffcost", StringComparison.OrdinalIgnoreCase)) { RunPuffCost(tag); return; }
            if ((tag ?? "").StartsWith("cost", StringComparison.OrdinalIgnoreCase)) { RunCloudCost(tag); return; }
            string named = "fxbench_cloudcourt_" + tag;
            string donePath = Path.Combine(OutDir, "done_" + named + ".txt"), failedPath = Path.Combine(OutDir, "failed_" + named + ".txt");
            Directory.CreateDirectory(OutDir);
            if (File.Exists(donePath)) File.Delete(donePath);
            if (File.Exists(failedPath)) File.Delete(failedPath);
            if (EditorApplication.isPlayingOrWillChangePlaymode) { File.WriteAllText(failedPath, "The editor is in Play mode. The film only runs outside it."); return; }

            Scene scene = default; bool lit = false;
            var owned = new List<Object>();
            var report = new System.Text.StringBuilder();
            WorldLookPresentation look = null;
            FxStudyEffect plain = null, creased = null;
            try
            {
                scene = EditorSceneManager.OpenPreviewScene(GaleMap);
                if (!scene.IsValid()) throw new InvalidOperationException("the map did not open as a preview scene: " + GaleMap);
                GameObject In(GameObject go) { SceneManager.MoveGameObjectToScene(go, scene); return go; }
                lit = Unsupported.SetOverrideLightingSettings(scene);
                Shader.SetGlobalFloat("_CharacterSmoothShade", ToonSkin.CharacterSmoothShade);

                var cam = In(new GameObject("~CloudCourtCamera")).AddComponent<Camera>();
                cam.scene = scene; cam.enabled = false; cam.nearClipPlane = 0.06f; cam.farClipPlane = 400f;
                cam.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.55f, 0.70f, 0.88f);

                // The map's grade and look, as the map review cameras wear them. Not `WorldOutline`: see the note above.
                try { cam.gameObject.AddComponent<ColourGrade>().AdoptFromScene(); } catch (Exception e) { report.AppendLine("no colour grade on the camera: " + e.Message); }
                try { cam.gameObject.AddComponent<WorldLookCamera>(); } catch (Exception e) { report.AppendLine("no WorldLookCamera on the camera: " + e.Message); }
                var roots = scene.GetRootGameObjects();
                var sun = roots.SelectMany(r => r.GetComponentsInChildren<Light>(true)).FirstOrDefault(l => l.type == LightType.Directional);
                var sceneRoot = roots.OrderByDescending(r => r.GetComponentsInChildren<Renderer>(true).Length).FirstOrDefault();
                try { if (sceneRoot != null) look = WorldLookPresentation.InstallPreview(sceneRoot.transform, 0f, sun); }
                catch (Exception e) { report.AppendLine("the map's look could not be installed for a preview: " + e.Message); }
                report.AppendLine("map: " + GaleMap + "; the map's look " + (look != null ? "is installed" : "is NOT installed") + "; the game's AO pass is not on this camera (it does not draw outside Play)");

                Vector3 stand = GaleStand, forward = Quaternion.Euler(0f, GaleYaw, 0f) * Vector3.forward, right = Vector3.Cross(Vector3.up, forward);
                float road = stand.y;
                if (scene.GetPhysicsScene().Raycast(stand + Vector3.up * 30f, Vector3.down, out var hit, 80f, ~0, QueryTriggerInteraction.Ignore)) road = hit.point.y;
                Vector3 at = new Vector3(stand.x, road, stand.z) + forward * 5.0f;

                // A character beside the columns, for scale.
                try
                {
                    var art = RosterBook.Load()?.FindPersonArt("dante");
                    if (art != null && art.Model != null)
                    {
                        var body = In((GameObject)Object.Instantiate(art.Model));
                        body.transform.SetPositionAndRotation(at + right * 3.1f - forward * 0.6f, Quaternion.Euler(0f, GaleYaw - 70f + CharacterVisual.PersonModelYaw, 0f));
                        body.transform.localScale = Vector3.one * CharacterVisual.PersonScale;
                        ToonSkin.Apply(body, ToonSkin.PersonOutlineWidth, art.Palette);
                        foreach (var animator in body.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
                    }
                }
                catch (Exception e) { report.AppendLine("no character for scale: " + e.Message); }

                // The same two columns twice: without the line, and with it. Never both at once.
                var hostPlain = In(new GameObject("~CloudCourtPlain")); var hostCreased = In(new GameObject("~CloudCourtCreased"));
                hostPlain.transform.position = at; hostCreased.transform.position = at;
                plain = new FxStudyCloud(hostPlain.transform, false, 0f);
                creased = new FxStudyCloud(hostCreased.transform, false, FxStudyCloud.Line);

                var target = new RenderTexture(GaleCellWidth, GaleCellHeight, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 4 };
                var shown = new RenderTexture(GaleCellWidth, GaleCellHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                var cell = new Texture2D(GaleCellWidth, GaleCellHeight, TextureFormat.RGB24, false);
                var sheet = new Texture2D(GaleCellWidth * 2, GaleCellHeight * 2, TextureFormat.RGB24, false);
                owned.Add(target); owned.Add(shown); owned.Add(cell); owned.Add(sheet);
                string frames = Path.Combine(OutDir, named + "_frames");
                if (Directory.Exists(frames)) foreach (string old in Directory.GetFiles(frames)) File.Delete(old);
                Directory.CreateDirectory(frames);

                void Shoot(int column, int row)
                {
                    cam.allowHDR = true; cam.targetTexture = target;
                    cam.Render();
                    bool write = GL.sRGBWrite; GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
                    Graphics.Blit(target, shown); GL.sRGBWrite = write;
                    var keep = RenderTexture.active; RenderTexture.active = shown;
                    cell.ReadPixels(new Rect(0, 0, GaleCellWidth, GaleCellHeight), 0, 0); cell.Apply();
                    RenderTexture.active = keep;
                    sheet.SetPixels(column * GaleCellWidth, (1 - row) * GaleCellHeight, GaleCellWidth, GaleCellHeight, cell.GetPixels());
                }

                float seconds = plain.Seconds;
                int count = Mathf.CeilToInt(seconds * 30f) + 1;
                for (int f = 0; f < count; f++)
                {
                    float t = f / 30f;
                    for (int view = 0; view < 2; view++)
                    {
                        Vector3 eye, lookAt; float fov;
                        if (view == 0) { eye = at + right * 6.8f - forward * 6.6f + Vector3.up * 2.7f; lookAt = at + right * 0.5f + Vector3.up * 1.8f; fov = 44f; }
                        else { eye = at + right * 3.3f - forward * 3.2f + Vector3.up * 2.4f; lookAt = at + right * 1.4f + Vector3.up * 2.3f; fov = 34f; }
                        cam.fieldOfView = fov;
                        cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(lookAt - eye, Vector3.up));
                        plain.Sample(t, eye); creased.Sample(t, eye);

                        hostCreased.SetActive(false); hostPlain.SetActive(true);
                        Shoot(0, view);
                        hostPlain.SetActive(false); hostCreased.SetActive(true);
                        Shoot(1, view);
                    }
                    sheet.Apply();
                    File.WriteAllBytes(Path.Combine(frames, "f" + f.ToString("000") + ".png"), sheet.EncodeToPNG());
                }
                report.AppendLine(count + " frames at 30 a second (" + seconds.ToString("0.00") + " s, one loop), in " + frames.Replace('\\', '/'));
                report.AppendLine("each frame: rows are the columns whole, and close on their lobes; columns are the cloud without its line | with it. Neither shows the game's AO.");
                report.AppendLine(lit ? "lit by the map's own lighting settings" : "the map's lighting settings could not be put in force");
                File.WriteAllText(donePath, report.ToString());
            }
            catch (Exception failure)
            {
                File.WriteAllText(failedPath, report + "\n" + failure);
                Debug.LogError("[PaeteAbilityFilm] " + failure);
            }
            finally
            {
                plain?.Dispose(); creased?.Dispose();
                if (look != null) Object.DestroyImmediate(look.gameObject);
                if (lit) Unsupported.RestoreOverrideLightingSettings();
                RenderTexture.active = null;
                foreach (var thing in owned) if (thing != null) Object.DestroyImmediate(thing);
                if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
