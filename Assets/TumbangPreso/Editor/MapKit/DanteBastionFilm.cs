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
    /// BASTION, FILMED WHERE IT IS PLAYED. The match's own wall (`DanteBastionWall`, the component `DanteHeroKit`'s BASTION
    /// builds on every peer) stood under Basilio on a real map opened as a PREVIEW scene (the scene the owner has open is
    /// not touched and nothing is saved, never in Play), in daylight with no bloom, the way `DanteBoulderFilm` films BOULDER.
    ///
    /// Each frame is six pictures. ACROSS: the thrower's eyes 6 m in front of him, a three-quarter from his right, and over
    /// his shoulder from behind. DOWN: the NEW look (`DanteBastionWall.Approved`, the approved b8b wall) and the OLD look
    /// (ASTRA's slabs, heat field and stone rim), built and stepped the same way at the same moments.
    ///
    /// ⚠️ STAGED, AND SAID SO: the wall is the game's component, at the rule's real size and life (7.5 s, then its end),
    /// stepped from its own clock (`Advance`), because nothing updates outside Play. His body is a still pose. No slipper
    /// flies: the two strikes (at 2.0 s and 5.0 s) are told to the wall where a slipper would have turned back
    /// (`DanteBastionWall.Strike`), on the new wall only (the old one's strike throws rubble into the open scene). He steps
    /// 0.9 m to his left between 2.8 s and 4.2 s, so it is seen to follow him; at its end it is let go where it stood.
    ///
    /// Request: a line `<tag> [map]` in `Temp/dante-bastion-film.request` (its own file). `map` is `eskinita` (the
    /// default), `ilalim` or `lagoon`. Frames go to `Logs/paete-ability-film/fxbench_dantebastioncourt_<tag>_frames/`,
    /// sixty a second, so `fxvideo.py dantebastioncourt_<tag>` joins them; then `done_fxbench_dantebastioncourt_<tag>.txt`
    /// with what each look costs to draw, or `failed_...` with the exception.
    /// </summary>
    [InitializeOnLoad]
    internal static class DanteBastionFilmPoll
    {
        private const string Request = "Temp/dante-bastion-film.request";
        private static double _next;
        static DanteBastionFilmPoll() { EditorApplication.update += Poll; }

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
                PaeteAbilityFilm.RunDanteBastionCourt(words[0], words.Length > 1 ? words[1] : "eskinita");
            }
        }
    }

    public static partial class PaeteAbilityFilm
    {
        private const float BastionStepFrom = 2.8f, BastionStepTo = 4.2f, BastionStepBy = 0.9f, BastionTail = 1.1f;
        private static readonly (float at, float across, float up)[] BastionStrikes = { (2.0f, 0.55f, 1.45f), (5.0f, -0.85f, 2.05f) };

        internal static void RunDanteBastionCourt(string tag, string map)
        {
            var chosen = DanteMapOf(map);
            float yaw = chosen.spot.z;
            Directory.CreateDirectory(OutDir);
            tag = new string((tag ?? "v00").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray()).ToLowerInvariant();
            if (tag.Length == 0) tag = "v00";
            string named = "fxbench_dantebastioncourt_" + tag;
            string donePath = Path.Combine(OutDir, "done_" + named + ".txt"), failedPath = Path.Combine(OutDir, "failed_" + named + ".txt");
            if (File.Exists(donePath)) File.Delete(donePath);
            if (File.Exists(failedPath)) File.Delete(failedPath);
            if (EditorApplication.isPlayingOrWillChangePlaymode) { File.WriteAllText(failedPath, "The editor is in Play mode. The film only runs outside it."); return; }

            Scene scene = default;
            bool lit = false, approved = DanteBastionWall.Approved;
            var owned = new List<Object>();
            var report = new System.Text.StringBuilder();
            DanteBastionWall fresh = null, old = null;
            Vector3 benchKey = FxStudyDanteBoulder.KeyLight;
            try
            {
                try { scene = EditorSceneManager.OpenPreviewScene(chosen.scene); }
                catch (Exception refused) { report.AppendLine("The map did not open as a preview scene (" + refused.Message + ")."); }
                if (!scene.IsValid()) throw new InvalidOperationException("No map to film on.");
                GameObject In(GameObject go) { SceneManager.MoveGameObjectToScene(go, scene); return go; }
                report.AppendLine("map: " + chosen.scene);
                lit = Unsupported.SetOverrideLightingSettings(scene);
                Shader.SetGlobalFloat("_CharacterSmoothShade", ToonSkin.CharacterSmoothShade);

                var cam = In(new GameObject("~BastionFilmCamera")).AddComponent<Camera>();
                cam.scene = scene; cam.enabled = false; cam.allowHDR = false;
                cam.nearClipPlane = 0.06f; cam.farClipPlane = 400f;
                cam.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.55f, 0.70f, 0.88f);

                Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward, right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
                float road = 0f;
                var physics = scene.GetPhysicsScene();
                if (physics.IsValid() && physics.Raycast(new Vector3(chosen.spot.x, 30f, chosen.spot.y), Vector3.down, out var under, 80f, ~0, QueryTriggerInteraction.Ignore))
                { road = under.point.y; report.AppendLine("road found at y " + road.ToString("0.000") + " on " + under.collider.name); }
                else report.AppendLine("no road found in the preview scene's physics; standing at y 0");
                Vector3 stand = new Vector3(chosen.spot.x, road, chosen.spot.y), thrower = stand + forward * DanteTargetAhead;
                // ⚠️ The wall finds the court with the game's own ground probe, which looks in the OPEN scene's physics, not
                // this preview's: said here so a wall standing off the road is read as the film's fault, not the game's.
                report.AppendLine("the game's ground probe reads y " + Slipper.GroundY(stand).ToString("0.000") + " under him (it looks in the open scene, not this map)");

                Light sun = null;
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var light in root.GetComponentsInChildren<Light>(false))
                        if (sun == null && light.type == LightType.Directional && light.enabled) sun = light;
                // `DanteBastionWall` takes its stone's light from `RenderSettings.sun`; for the preview it is this map's.
                if (sun != null) { FxStudyDanteBoulder.KeyLight = -sun.transform.forward; report.AppendLine("the stone is lit from the map's sun (" + sun.name + ")"); }

                GameObject Person(string id, Vector3 at, float facing, out GameObject model)
                {
                    model = null;
                    var holder = In(new GameObject("~BastionFilm-" + id));
                    holder.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, facing, 0f));
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
                // Two of him, one for each look, at the same spot: only one is shown at a time.
                var him = Person("dante", stand, yaw, out var danteModel);
                var himOld = Person("dante", stand, yaw, out var danteOldModel);
                var target = Person("zack", thrower, yaw + 180f, out _);
                if (danteModel == null) report.AppendLine("Basilio's model was not found: the wall stands under an empty holder.");

                DanteBastionWall.Approved = true;
                fresh = DanteBastionWall.Build(him.transform, GeoRules.BarrierSeconds);
                DanteBastionWall.Approved = false;
                old = DanteBastionWall.Build(himOld.transform, GeoRules.BarrierSeconds);
                DanteBastionWall.Approved = approved;
                report.AppendLine("new wall: " + (fresh.DrawsApproved ? "the approved b8b wall" : "ASTRA's (the approved one could not be built)") + ", " + fresh.SlabCount + " slabs");

                var plain = new RenderTexture(DanteCellWidth, DanteCellHeight, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                owned.Add(plain);
                var cell = new Texture2D(DanteCellWidth, DanteCellHeight, TextureFormat.RGB24, false);
                var sheet = new Texture2D(DanteCellWidth * 3, DanteCellHeight * 2, TextureFormat.RGB24, false);
                owned.Add(cell); owned.Add(sheet);
                string frames = Path.Combine(OutDir, named + "_frames");
                if (Directory.Exists(frames)) foreach (string gone in Directory.GetFiles(frames)) File.Delete(gone);
                Directory.CreateDirectory(frames);

                void Shoot(int column, int row)
                {
                    cam.targetTexture = plain;
                    cam.Render();
                    var keep = RenderTexture.active; RenderTexture.active = plain;
                    cell.ReadPixels(new Rect(0, 0, DanteCellWidth, DanteCellHeight), 0, 0); cell.Apply();
                    RenderTexture.active = keep;
                    sheet.SetPixels(column * DanteCellWidth, (1 - row) * DanteCellHeight, DanteCellWidth, DanteCellHeight, cell.GetPixels());
                }

                float life = GeoRules.BarrierSeconds, seconds = life + BastionTail;
                int count = Mathf.CeilToInt(seconds * BenchRate) + 1, struck = 0;
                bool released = false;
                string cost = null;
                for (int f = 0; f < count; f++)
                {
                    float age = f / (float)BenchRate, dt = f == 0 ? 0f : 1f / BenchRate;
                    // He steps to his left once while it stands; it goes with him.
                    Vector3 at = stand - right * (BastionStepBy * FxStudyKit.Ease(FxStudyKit.Run(age, BastionStepFrom, BastionStepTo)));
                    him.transform.position = at; himOld.transform.position = at;
                    if (!released && age >= life) { released = true; if (fresh != null) fresh.Release(); if (old != null) old.Release(); }
                    if (fresh != null) fresh.Advance(dt);
                    if (old != null) old.Advance(dt);
                    while (struck < BastionStrikes.Length && age >= BastionStrikes[struck].at)
                    {
                        var s = BastionStrikes[struck++];
                        if (fresh != null) fresh.Strike(him.transform.TransformPoint(new Vector3(s.across, s.up, GeoRules.BarrierForward)));
                    }
                    Vector3 wallAt = at + forward * GeoRules.BarrierForward;
                    for (int view = 0; view < 3; view++)
                    {
                        Vector3 eye, look; float fov;
                        if (view == 0) { eye = thrower + Vector3.up * DanteEye - forward * 0.15f; look = wallAt + Vector3.up * 1.0f; fov = 60f; }
                        else if (view == 1) { look = wallAt + Vector3.up * 1.0f - forward * 0.4f; eye = look + right * 4.6f + forward * 2.6f + Vector3.up * 1.4f; fov = 50f; }
                        else { look = wallAt + Vector3.up * 1.0f; eye = at - forward * 3.2f + right * 0.9f + Vector3.up * 1.9f; fov = 55f; }
                        cam.fieldOfView = fov;
                        cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye, Vector3.up));
                        target.SetActive(view != 0);
                        for (int row = 0; row < 2; row++)
                        {
                            him.SetActive(row == 0); himOld.SetActive(row == 1);
                            if (fresh != null) fresh.gameObject.SetActive(row == 0);
                            if (old != null) old.gameObject.SetActive(row == 1);
                            Shoot(view, row);
                        }
                    }
                    // ⚠️ WHAT EACH LOOK COSTS, COUNTED at 2.1 s (standing, the first strike lit): every renderer that is on,
                    // times its material's passes, is the draw calls; nothing batches. The editor, outside Play.
                    if (cost == null && age >= 2.1f)
                    {
                        string Count(DanteBastionWall wall, string label)
                        {
                            if (wall == null) return label + ": not built\n";
                            wall.gameObject.SetActive(true);
                            int calls = 0, triangles = 0, renderers = 0;
                            var materials = new HashSet<Material>();
                            foreach (var renderer in wall.GetComponentsInChildren<MeshRenderer>(false))
                            {
                                if (!renderer.enabled || renderer.sharedMaterial == null) continue;
                                var filter = renderer.GetComponent<MeshFilter>();
                                if (filter == null || filter.sharedMesh == null) continue;
                                int passes = Mathf.Max(1, renderer.sharedMaterial.passCount), own = 0;
                                for (int s = 0; s < filter.sharedMesh.subMeshCount; s++) own += (int)(filter.sharedMesh.GetIndexCount(s) / 3);
                                renderers++; calls += passes; triangles += own * passes;
                                foreach (var m in renderer.sharedMaterials) if (m != null) materials.Add(m);
                            }
                            var watch = System.Diagnostics.Stopwatch.StartNew();
                            long before = GC.GetAllocatedBytesForCurrentThread();
                            for (int n = 0; n < 300; n++) wall.StepTo(2.1f);
                            watch.Stop();
                            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                            return label + ": " + renderers + " renderers on, " + calls + " draw calls, " + triangles + " triangles drawn, " + materials.Count + " materials; posing "
                                 + (watch.Elapsed.TotalMilliseconds / 300.0).ToString("0.0000") + " ms a frame, " + (allocated / 300.0).ToString("0") + " bytes a frame\n";
                        }
                        cost = Count(fresh, "new (approved b8b)") + Count(old, "old (ASTRA)");
                    }
                    sheet.Apply();
                    File.WriteAllBytes(Path.Combine(frames, "f" + f.ToString("000") + ".png"), sheet.EncodeToPNG());
                }
                report.AppendLine(count + " frames at " + BenchRate + " a second (" + seconds.ToString("0.00") + " s), in " + frames.Replace('\\', '/'));
                report.AppendLine("each frame: columns are the thrower's eyes, his right, behind him; rows are NEW (approved b8b) and OLD (ASTRA)");
                report.AppendLine(cost ?? "cost not counted");
                report.AppendLine(lit ? "lit by the map's own lighting settings" : "the map's lighting settings could not be put in force: lit by the open scene's ambient");
                report.AppendLine("STAGED: no slipper flies (the strikes are told to the new wall); his body is a still pose; no bloom");
                File.WriteAllText(donePath, report.ToString());
                Debug.Log("[DanteBastionFilm] " + named + " written to " + OutDir);
            }
            catch (Exception failure)
            {
                File.WriteAllText(failedPath, report + "\n" + failure);
                Debug.LogError("[DanteBastionFilm] " + failure);
            }
            finally
            {
                DanteBastionWall.Approved = approved;
                try { if (fresh != null) { fresh.FreeLook(); Object.DestroyImmediate(fresh.gameObject); } } catch (Exception) { }
                try { if (old != null) Object.DestroyImmediate(old.gameObject); } catch (Exception) { }
                FxStudyDanteBoulder.KeyLight = benchKey;
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
