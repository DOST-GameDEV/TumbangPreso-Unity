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
    /// <summary>
    /// QUICK CIRCUIT, FILMED WHERE IT IS PLAYED. The bench is a dark floor with bloom; the game is a sunlit court and no
    /// camera that blooms. This films Isagani's cut on a real map opened as a PREVIEW scene (the scene the owner has open
    /// is not touched and nothing is saved), with his real model, the way `PaeteAbilityFilm.Gale.cs` films the Whirlwind.
    ///
    /// Each frame is twelve pictures. DOWN: third person from his front quarter, his own eyes, and the eyes of a target
    /// standing 8 m in front of him. ACROSS: a cut to his RIGHT (plain, then through the bench's bloom) and a cut to his
    /// LEFT (plain, then bloom). Both sides are filmed because the light on his chest and the leader must sit on his
    /// front whichever way he goes, and the target's row is where that is checked.
    ///
    /// There is no old look to stand beside it: before this the skill drew nothing in the world.
    ///
    /// ⚠️ HIS BODY IS A STILL POSE, SLID. Nothing animates outside Play, so he stands as his model stands and is moved
    /// along the cut by the rule's own sum (thrown at the root of 2 x friction 30 x 2 m, stopped by that friction).
    /// The afterimage is therefore a copy of that standing pose, not of his cast's lean.
    ///
    /// Request: a line `<tag> [map] [x z yaw]` in `Temp/zack-circuit-film.request` (its own file, so no other film's
    /// reader is touched). `map` is `eskinita` (the default), `ilalim` or `lagoon`; `x z yaw` say where he stands on it
    /// and which way he faces, when the map's own spot below is not wanted. Two more numbers may follow: one that is
    /// no longer read (it chose between looks that were tried and dropped) and the hundredths of each piece that are light laid on. A tag that begins `still`
    /// plays the whole cut but only draws and keeps four moments of it (0.10, 0.25, 0.33 and 0.43 s after the press). A tag that begins `scout` films nothing: it
    /// writes one picture of the map from straight above with a metre grid, for choosing that spot.
    /// Frames go to `Logs/paete-ability-film/fxbench_zackcourt_<tag>_frames/`, named like a bench film so the same tools
    /// join them, then `done_fxbench_zackcourt_<tag>.txt` with what it costs to draw, or `failed_...` with the exception.
    /// </summary>
    [InitializeOnLoad]
    internal static class ZackCircuitFilmPoll
    {
        private const string Request = "Temp/zack-circuit-film.request";
        private static double _next;
        static ZackCircuitFilmPoll() { EditorApplication.update += Poll; }

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 1.0;
            if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            // An editor in the background does not look for changed files by itself: look now, and if that starts a
            // compile the request waits for the reload.
            AssetDatabase.Refresh();
            if (EditorApplication.isCompiling) return;
            string[] lines = File.ReadAllLines(Request);
            File.Delete(Request);
            foreach (string line in lines)
            {
                string[] words = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length == 0) continue;
                // `maskbench <effect> <tag> <mask|-> [lift cutFrom cutTo [level]]`: the picture, the tune and the film in one step, so a
                // domain reload from another compile between two requests cannot film the plain trail under a candidate's name.
                if (words[0] == "maskbench" && words.Length > 3)
                {
                    FxStudyZackBank.StrandMask = words[3] != "-" ? words[3] : null;
                    FxStudyZackBank.StrandTune = words.Length > 6 ? new Vector4(float.Parse(words[4], System.Globalization.CultureInfo.InvariantCulture), float.Parse(words[5], System.Globalization.CultureInfo.InvariantCulture), float.Parse(words[6], System.Globalization.CultureInfo.InvariantCulture), words.Length > 7 ? float.Parse(words[7], System.Globalization.CultureInfo.InvariantCulture) : 1f) : (Vector4?)null;
                    try { PaeteAbilityFilm.ZackBench(words[1], words[2]); }
                    finally { FxStudyZackBank.StrandMask = null; FxStudyZackBank.StrandTune = null; }
                    continue;
                }
                // `mask <name> [lift cutFrom cutTo]` puts a candidate picture on Bank Shot's trail ribbons for the bench films that follow; `mask -` clears it.
                if (words[0] == "mask") { FxStudyZackBank.StrandMask = words.Length > 1 && words[1] != "-" ? words[1] : null; FxStudyZackBank.StrandTune = words.Length > 4 ? new Vector4(float.Parse(words[2], System.Globalization.CultureInfo.InvariantCulture), float.Parse(words[3], System.Globalization.CultureInfo.InvariantCulture), float.Parse(words[4], System.Globalization.CultureInfo.InvariantCulture), words.Length > 5 ? float.Parse(words[5], System.Globalization.CultureInfo.InvariantCulture) : 1f) : (Vector4?)null; Debug.Log("[ZackCircuitFilm] trail mask " + (words.Length > 1 ? words[1] : "-")); continue; }
                if (words[0].StartsWith("cost")) { PaeteAbilityFilm.CostOf(words[0], words.Length > 1 ? words[1] : "zackbank"); continue; }
                float[] spot = null;
                if (words.Length >= 5 && float.TryParse(words[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x)
                    && float.TryParse(words[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float z)
                    && float.TryParse(words[4], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float yaw)) spot = new[] { x, z, yaw };
                int look = -1, hundredths = -1;
                if (words.Length > 5) int.TryParse(words[5], out look);
                if (words.Length > 6) int.TryParse(words[6], out hundredths);
                PaeteAbilityFilm.RunZackCourt(words[0], words.Length > 1 ? words[1] : "eskinita", spot, look, hundredths);
            }
        }
    }

    public static partial class PaeteAbilityFilm
    {
        /// <summary>The bench film, reachable from Zack's own poller (the film runner itself is private to this class).</summary>
        internal static void ZackBench(string effect, string tag) => RunFxBench(effect, tag);

        /// <summary>
        /// What a bench effect costs to POSE, measured: the effect is built in a preview scene and posed at every frame of
        /// its life at sixty a second, three times over (the first pass is thrown away: it is the one that warms
        /// everything up), each `Sample` timed on its own. Also counted, at its busiest frame: how many renderers are on
        /// (each is a draw call; none of them batch) and the triangles they submit. Nothing is drawn: this is the CPU's
        /// share only. Request a line `cost<tag> <effect>` in `Temp/zack-circuit-film.request`; the answer is
        /// `done_fxbench_zackcourt_cost<tag>.txt`.
        /// </summary>
        internal static void CostOf(string tag, string effect)
        {
            Directory.CreateDirectory(OutDir);
            tag = new string(tag.Where(c => char.IsLetterOrDigit(c)).ToArray()).ToLowerInvariant();
            string donePath = Path.Combine(OutDir, "done_fxbench_zackcourt_" + tag + ".txt"), failedPath = Path.Combine(OutDir, "failed_fxbench_zackcourt_" + tag + ".txt");
            if (File.Exists(donePath)) File.Delete(donePath);
            if (File.Exists(failedPath)) File.Delete(failedPath);
            var scene = EditorSceneManager.NewPreviewScene();
            FxStudyEffect fx = null;
            try
            {
                var stage = new GameObject("~ZackCost"); SceneManager.MoveGameObjectToScene(stage, scene);
                fx = FxStudyEffect.Create(effect, stage.transform);
                if (fx == null) throw new ArgumentException("No effect called " + effect);
                int count = Mathf.CeilToInt(fx.Seconds * BenchRate) + 1;
                var clock = new System.Diagnostics.Stopwatch();
                double worst = 0, total = 0; int calls = 0, triangles = 0, materials = 0; float busiest = 0f;
                for (int pass = 0; pass < 3; pass++)
                    for (int f = 0; f < count; f++)
                    {
                        float age = f / (float)BenchRate;
                        fx.Shot(age, out var eye, out _, out _);
                        clock.Restart(); fx.Sample(age, eye); clock.Stop();
                        if (pass == 0) continue;
                        double ms = clock.Elapsed.TotalMilliseconds; total += ms; worst = Math.Max(worst, ms);
                        int on = 0, tris = 0;
                        var distinct = new HashSet<Material>();
                        foreach (var renderer in fx.Root.GetComponentsInChildren<MeshRenderer>(false))
                        {
                            if (!renderer.enabled) continue;
                            var filter = renderer.GetComponent<MeshFilter>();
                            if (filter == null || filter.sharedMesh == null) continue;
                            on++; tris += (int)(filter.sharedMesh.GetIndexCount(0) / 3);
                            if (renderer.sharedMaterial != null) distinct.Add(renderer.sharedMaterial);
                        }
                        if (on > calls) { calls = on; triangles = tris; busiest = age; materials = distinct.Count; }
                    }
                File.WriteAllText(donePath, effect + ": posing, measured over " + (2 * count) + " frames: mean " + (total / (2 * count)).ToString("0.0000") + " ms, worst " + worst.ToString("0.0000") + " ms a frame\n"
                    + "busiest frame " + busiest.ToString("0.00") + " s: " + calls + " renderers on (draw calls, none batched), " + triangles + " triangles, " + materials + " distinct materials\n"
                    + "(bench stand-ins, walls and shoe, are counted in that too)\n");
            }
            catch (Exception failure) { File.WriteAllText(failedPath, failure.ToString()); }
            finally
            {
                try { fx?.Dispose(); } catch (Exception) { }
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private const int ZackCellWidth = 480, ZackCellHeight = 270;
        private const float ZackTargetAhead = 8.0f;

        // Each map he can be filmed on: its scene, and where he stands and which way he faces (x, z, degrees), chosen off
        // that map's scout picture so there are 8 m of floor in front of him and more than the 2 m he is thrown either side.
        private static (string scene, Vector3 spot) ZackMapOf(string map)
        {
            switch ((map ?? "").ToLowerInvariant())
            {
                case "ilalim": return ("Assets/TumbangPreso/Scenes/Maps/IlalimNgTulay.unity", new Vector3(0f, 0f, 0f));
                case "lagoon": return ("Assets/TumbangPreso/Scenes/Maps/LagoonCove.unity", new Vector3(0f, 0f, 0f));
                // Eskinita: on the pale court, clear of the houses either side.
                default: return ("Assets/TumbangPreso/Scenes/Maps/Eskinita.unity", new Vector3(-2.5f, -6.0f, 0f));
            }
        }

        internal static void RunZackCourt(string tag, string map, float[] spot, int wanted, int hundredths)
        {
            var chosen = ZackMapOf(map);
            string ZackMap = chosen.scene;
            if (spot != null) chosen.spot = new Vector3(spot[0], spot[1], spot[2]);
            Vector3 ZackStand = new Vector3(chosen.spot.x, 0f, chosen.spot.y);
            float ZackYaw = chosen.spot.z;
            Directory.CreateDirectory(OutDir);
            tag = new string((tag ?? "v00").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray()).ToLowerInvariant();
            if (tag.Length == 0) tag = "v00";
            string named = "fxbench_zackcourt_" + tag;
            string donePath = Path.Combine(OutDir, "done_" + named + ".txt"), failedPath = Path.Combine(OutDir, "failed_" + named + ".txt");
            if (File.Exists(donePath)) File.Delete(donePath);
            if (File.Exists(failedPath)) File.Delete(failedPath);
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                File.WriteAllText(failedPath, "The editor is in Play mode. The film only runs outside it.");
                return;
            }

            Scene scene = default;
            bool opened = false, lit = false;
            var owned = new List<Object>();
            var flashes = new ZackCircuitFlash[2];
            var report = new System.Text.StringBuilder();
            try
            {
                try { scene = EditorSceneManager.OpenPreviewScene(ZackMap); opened = scene.IsValid(); }
                catch (Exception refused) { report.AppendLine("The map did not open as a preview scene (" + refused.Message + ")."); }
                if (!opened) throw new InvalidOperationException("No map to film on.");
                GameObject In(GameObject go) { SceneManager.MoveGameObjectToScene(go, scene); return go; }
                report.AppendLine("map: " + ZackMap);
                ZackCircuitFlash.StagedLight = hundredths >= 0 ? hundredths / 100f : -1f;
                report.AppendLine("light laid on " + (hundredths >= 0 ? (hundredths / 100f).ToString("0.00") : "the game's own"));
                bool stills = tag.StartsWith("still");

                lit = Unsupported.SetOverrideLightingSettings(scene);
                Shader.SetGlobalFloat("_CharacterSmoothShade", ToonSkin.CharacterSmoothShade);

                var cam = In(new GameObject("~ZackFilmCamera")).AddComponent<Camera>();
                cam.scene = scene; cam.enabled = false;
                cam.nearClipPlane = 0.06f; cam.farClipPlane = 400f;
                cam.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.55f, 0.70f, 0.88f);

                if (tag.StartsWith("scout")) { GaleScout(scene, cam, named, donePath, report, owned); return; }

                Vector3 forward = Quaternion.Euler(0f, ZackYaw, 0f) * Vector3.forward, right = Quaternion.Euler(0f, ZackYaw, 0f) * Vector3.right;
                float road = ZackStand.y;
                var physics = scene.GetPhysicsScene();
                if (physics.IsValid() && physics.Raycast(ZackStand + Vector3.up * 30f, Vector3.down, out var under, 80f, ~0, QueryTriggerInteraction.Ignore))
                { road = under.point.y; report.AppendLine("road found at y " + road.ToString("0.000") + " on " + under.collider.name); }
                else report.AppendLine("no road found in the preview scene's physics; standing at y " + road.ToString("0.000"));
                Vector3 stand = new Vector3(ZackStand.x, road, ZackStand.z), targetAt = stand + forward * ZackTargetAhead;

                // A person: a holder that faces the way he faces, and his model under it turned the way models are.
                GameObject Person(string id, Vector3 at, float yaw, out GameObject model)
                {
                    model = null;
                    var holder = In(new GameObject("~ZackFilm-" + id));
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
                report.AppendLine("he stands at (" + stand.x.ToString("0.0") + ", " + stand.z.ToString("0.0") + ") facing " + ZackYaw.ToString("0") + " degrees");
                var zack = Person("zack", stand, ZackYaw, out var zackModel);
                if (zackModel == null) report.AppendLine("Isagani's model was not found: there is no body, no cracks and no afterimage in this film.");
                var target = Person("dante", targetAt, ZackYaw + 180f, out _);
                Person("cheska", stand + forward * 3.5f - right * 4.2f, ZackYaw + 90f, out _);

                var plainTarget = new RenderTexture(ZackCellWidth, ZackCellHeight, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                var hdrTarget = new RenderTexture(ZackCellWidth, ZackCellHeight, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 4 };
                var shown = new RenderTexture(ZackCellWidth, ZackCellHeight, 0, RenderTextureFormat.ARGB32);
                owned.Add(plainTarget); owned.Add(hdrTarget); owned.Add(shown);
                var bloomShader = Shader.Find("Hidden/TumbangPreso/FxStudyBloom");
                Material bloomMaterial = bloomShader != null ? new Material(bloomShader) { hideFlags = HideFlags.HideAndDontSave } : null;
                if (bloomMaterial != null) owned.Add(bloomMaterial); else report.AppendLine("the bench bloom shader is missing: the bloom columns are the plain picture again");
                var cell = new Texture2D(ZackCellWidth, ZackCellHeight, TextureFormat.RGB24, false);
                var sheet = new Texture2D(ZackCellWidth * 4, ZackCellHeight * 3, TextureFormat.RGB24, false);
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
                    cell.ReadPixels(new Rect(0, 0, ZackCellWidth, ZackCellHeight), 0, 0); cell.Apply();
                    RenderTexture.active = keep;
                    Shader.SetGlobalFloat("_FxStudyBloom", 0f);
                    sheet.SetPixels(column * ZackCellWidth, (2 - row) * ZackCellHeight, ZackCellWidth, ZackCellHeight, cell.GetPixels());
                }

                int meshesBefore = Resources.FindObjectsOfTypeAll<Mesh>().Length, materialsBefore = Resources.FindObjectsOfTypeAll<Material>().Length;
                float tell = FxStudyZackCircuit.Tell, slide = FxStudyZackCircuit.Slide, lead = 0.20f, seconds = lead + tell + slide + 0.60f;
                int count = Mathf.CeilToInt(seconds * BenchRate) + 1;
                var thrown = new bool[2];
                var watch = new System.Diagnostics.Stopwatch();
                var tellMs = new List<double>(); var cutMs = new List<double>(); var poseMs = new List<double>();
                string cost = null;
                for (int f = 0; f < count; f++)
                {
                    float t = f / (float)BenchRate - lead;
                    // Where the rule has him: still through the tell, then the launch speed bled off by friction.
                    float s = Mathf.Clamp(t - tell, 0f, slide), gone = FxStudyZackCircuit.Launch * s - 0.5f * FxStudyZackCircuit.Friction * s * s;
                    for (int side = 0; side < 2; side++)
                    {
                        Vector3 dir = side == 0 ? right : -right, body = stand + dir * gone;
                        zack.transform.position = body;
                        if (zackModel != null) zackModel.SetActive(true);
                        for (int other = 0; other < 2; other++) if (flashes[other] != null) flashes[other].gameObject.SetActive(other == side);
                        if (t >= 0f)
                        {
                            // (Timed: what the press costs to make, what the cut costs, and what a frame costs to pose, with his
                            // body moving along the cut as it does in a match.)
                            if (flashes[side] == null) { watch.Restart(); flashes[side] = ZackCircuitFlash.Stage(zack.transform, zackModel, dir); watch.Stop(); tellMs.Add(watch.Elapsed.TotalMilliseconds); }
                            if (!thrown[side] && t >= tell) { watch.Restart(); flashes[side].StagedCut(); watch.Stop(); cutMs.Add(watch.Elapsed.TotalMilliseconds); thrown[side] = true; }
                            watch.Restart(); flashes[side].StepTo(t); watch.Stop(); poseMs.Add(watch.Elapsed.TotalMilliseconds);
                        }
                        for (int view = 0; view < 3; view++)
                        {
                            Vector3 eye, look; float fov;
                            if (view == 0) { eye = stand + forward * 5.4f + right * 3.6f + Vector3.up * 2.3f; look = stand + Vector3.up * 0.85f; fov = 46f; }
                            else if (view == 1) { eye = body + Vector3.up * 1.50f + forward * 0.12f; look = body + forward * 8f + Vector3.up * 1.0f; fov = 70f; }
                            else { eye = targetAt + Vector3.up * 1.50f - forward * 0.15f; look = stand + Vector3.up * 0.9f; fov = 70f; }
                            cam.fieldOfView = fov;
                            cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye, Vector3.up));
                            // Nobody sees their own body from behind their own eyes.
                            if (zackModel != null) zackModel.SetActive(view != 1);
                            target.SetActive(view != 2);
                            if (stills && f != 18 && f != 27 && f != 32 && f != 38) continue;
                            if (flashes[side] != null) flashes[side].StagedView(eye, view == 1);
                            Shoot(false, side * 2, view);
                            Shoot(true, side * 2 + 1, view);
                        }
                        // What it costs to draw, counted once in the middle of the cut: every renderer that is on is one
                        // draw call (nothing of it is batched), with the triangles it submits. Counted for the view of
                        // a target, which draws everything.
                        if (cost == null && side == 0 && t >= tell + 0.18f && flashes[0] != null)
                        {
                            int calls = 0, triangles = 0;
                            foreach (var renderer in flashes[0].GetComponentsInChildren<MeshRenderer>(false))
                            {
                                if (!renderer.enabled) continue;
                                var filter = renderer.GetComponent<MeshFilter>();
                                if (filter == null || filter.sharedMesh == null) continue;
                                calls++; triangles += (int)(filter.sharedMesh.GetIndexCount(0) / 3);
                            }
                            int bodyTriangles = 0;
                            if (zackModel != null) foreach (var skin in zackModel.GetComponentsInChildren<SkinnedMeshRenderer>(true)) if (skin.sharedMesh != null) bodyTriangles += (int)(skin.sharedMesh.GetIndexCount(0) / 3);
                            cost = "at " + (t - tell).ToString("0.00") + " s into the cut: " + calls + " draw calls, " + triangles + " triangles, all see-through (his own body is " + bodyTriangles + " triangles, and the afterimage is two copies of it)";
                        }
                    }
                    if (stills && f != 18 && f != 27 && f != 32 && f != 38) continue;
                    sheet.Apply();
                    File.WriteAllBytes(Path.Combine(frames, "f" + f.ToString("000") + ".png"), sheet.EncodeToPNG());
                }
                report.AppendLine(count + " frames at " + BenchRate + " a second (" + seconds.ToString("0.00") + " s: " + lead.ToString("0.00") + " s of nothing, the press, the cut " + tell.ToString("0.00") + " s later, stopped " + slide.ToString("0.000") + " s after that), in " + frames.Replace('\\', '/'));
                report.AppendLine("each frame: rows are third person, his own eyes, a target " + ZackTargetAhead.ToString("0.0") + " m in front of him; columns are RIGHT cut, RIGHT cut with the bench bloom, LEFT cut, LEFT cut with the bench bloom");
                report.AppendLine("of each lit piece, at most " + AmihanWhirlwind.DayLight.ToString("0.00") + " is light laid on (the bench's are 0.55 to 0.92)");
                report.AppendLine(cost ?? "cost not counted");
                report.AppendLine("MEASURED in the editor, his body moving along the cut: make at the press " + string.Join(", ", tellMs.Select(v => v.ToString("0.00"))) + " ms; make at the cut "
                    + string.Join(", ", cutMs.Select(v => v.ToString("0.00"))) + " ms; pose a frame mean " + (poseMs.Count > 0 ? poseMs.Average() : 0).ToString("0.000") + " ms, worst " + (poseMs.Count > 0 ? poseMs.Max() : 0).ToString("0.000") + " ms");
                // Six more casts one after another, each made, played through and ended the way a match ends one, so the
                // cost of a cast that is not the first is seen too.
                var again = new List<string>();
                for (int round = 0; round < 6; round++)
                {
                    Vector3 dir = round % 2 == 0 ? right : -right;
                    zack.transform.position = stand; if (zackModel != null) zackModel.SetActive(true);
                    watch.Restart();
                    var cast = ZackCircuitFlash.Stage(zack.transform, zackModel, dir);
                    double press = watch.Elapsed.TotalMilliseconds;
                    zack.transform.position = stand + dir * 0.05f;
                    watch.Restart(); cast.StagedCut(); double cutAt = watch.Elapsed.TotalMilliseconds;
                    for (int f = 0; f < 50; f++) { float t = tell + f / (float)BenchRate; float sl = Mathf.Clamp(t - tell, 0f, slide); zack.transform.position = stand + dir * (FxStudyZackCircuit.Launch * sl - 0.5f * FxStudyZackCircuit.Friction * sl * sl); cast.StepTo(t); }
                    watch.Restart(); cast.Free(); Object.DestroyImmediate(cast.gameObject); double end = watch.Elapsed.TotalMilliseconds;
                    again.Add(press.ToString("0.00") + "+" + cutAt.ToString("0.00") + "+" + end.ToString("0.00"));
                }
                report.AppendLine("six more casts in a row, press + cut + end (ms): " + string.Join("  ", again));
                for (int side = 0; side < 2; side++)
                    if (flashes[side] != null) { flashes[side].Free(); Object.DestroyImmediate(flashes[side].gameObject); flashes[side] = null; }
                // (Put-away effects are freed first: they are kept on purpose, and are not a leak.)
                ZackCircuitFlash.ClearSpares();
                report.AppendLine("after freeing both: " + (Resources.FindObjectsOfTypeAll<Mesh>().Length - meshesBefore) + " meshes and " + (Resources.FindObjectsOfTypeAll<Material>().Length - materialsBefore) + " materials more than before they were built");
                report.AppendLine(lit ? "lit by the map's own lighting settings" : "the map's lighting settings could not be put in force: lit by the open scene's ambient");
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
                for (int side = 0; side < 2; side++)
                    if (flashes[side] != null) { try { flashes[side].Free(); Object.DestroyImmediate(flashes[side].gameObject); } catch (Exception) { } }
                ZackCircuitFlash.ClearSpares();
                Shader.SetGlobalFloat("_FxStudyBloom", 0f);
                ZackCircuitFlash.StagedLight = -1f;
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
