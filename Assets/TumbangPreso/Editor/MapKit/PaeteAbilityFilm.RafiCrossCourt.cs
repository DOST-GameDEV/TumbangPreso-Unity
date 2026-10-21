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
    /// ⚠️ THE CROSSCURRENT OF ILYAS, FILMED WHERE IT IS PLAYED. The water wall's court film (`PaeteAbilityFilm.RafiWall.cs`)
    /// done for his CROSSCURRENT, through the match's own path: a `WorldEffectSnapshot.Field` exactly as
    /// `RafiHeroKit.Crosscurrent` casts it, handed to `RafiWaterVisual.Build` once with `NewCurrent` off and once with it
    /// on, so what is filmed is what a match draws. The same map opened as a preview scene (the scene the owner has open
    /// is not touched and nothing is saved), the gale's spot on its court, daylight, no bloom, bodies for scale.
    ///
    /// Each frame is eight pictures: four views down (third person from the side, the caster's eyes, a thrower's eyes
    /// 7.5 m down the path looking back along it, and from above), and across them THE OLD CURRENT and THE NEW ONE at the
    /// same moments.
    ///
    /// Its own request file, `Temp/rafi-cross-court.request`, one line: the tag. A tag holding `spent` spends it 0.55 s
    /// in (the bench's spent age), as a slipper's touch would; one holding `tight` casts it as TIGHT CUT (the variant's
    /// narrower, faster column). Frames go to `Logs/paete-ability-film/fxbench_raficrosscourt_<tag>_frames/` (named like a
    /// bench film so the same tools join and cut them), then `done_fxbench_raficrosscourt_<tag>.txt`, or `failed_...`.
    /// </summary>
    public static partial class PaeteAbilityFilm
    {
        private const string RafiCrossCourtRequest = "Temp/rafi-cross-court.request";
        private const float RafiCrossSpentAt = 0.55f;
        private static double _nextRafiCrossCourtPoll;

        [InitializeOnLoadMethod]
        private static void HookRafiCrossCourt() { EditorApplication.update += PollRafiCrossCourt; }

        private static void PollRafiCrossCourt()
        {
            if (EditorApplication.timeSinceStartup < _nextRafiCrossCourtPoll) return;
            _nextRafiCrossCourtPoll = EditorApplication.timeSinceStartup + 1.0;
            if (!File.Exists(RafiCrossCourtRequest) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            AssetDatabase.Refresh();
            if (EditorApplication.isCompiling) return;
            string tag = File.ReadAllText(RafiCrossCourtRequest).Trim();
            File.Delete(RafiCrossCourtRequest);
            RunRafiCrossCourt(tag);
        }

        private static void RunRafiCrossCourt(string tag)
        {
            Directory.CreateDirectory(OutDir);
            tag = new string((tag ?? "v00").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray()).ToLowerInvariant();
            if (tag.Length == 0) tag = "v00";
            string named = "fxbench_raficrosscourt_" + tag;
            string donePath = Path.Combine(OutDir, "done_" + named + ".txt"), failedPath = Path.Combine(OutDir, "failed_" + named + ".txt");
            if (File.Exists(donePath)) File.Delete(donePath);
            if (File.Exists(failedPath)) File.Delete(failedPath);

            Scene scene = default;
            bool lit = false, switchBefore = RafiWaterVisual.NewCurrent;
            var owned = new List<Object>();
            var report = new System.Text.StringBuilder();
            RafiCurrentStream stream = null;
            try
            {
                scene = EditorSceneManager.OpenPreviewScene(GaleMap);
                if (!scene.IsValid()) throw new InvalidOperationException("The map did not open as a preview scene: " + GaleMap);
                GameObject In(GameObject go) { SceneManager.MoveGameObjectToScene(go, scene); return go; }
                report.AppendLine("map: " + GaleMap);
                lit = Unsupported.SetOverrideLightingSettings(scene);
                Shader.SetGlobalFloat("_CharacterSmoothShade", ToonSkin.CharacterSmoothShade);
                Shader.SetGlobalFloat("_FxStudyBloom", 0f);

                var cam = In(new GameObject("~RafiCrossFilmCamera")).AddComponent<Camera>();
                cam.scene = scene; cam.enabled = false;
                cam.nearClipPlane = 0.06f; cam.farClipPlane = 400f; cam.allowHDR = false;
                cam.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.55f, 0.70f, 0.88f);

                Quaternion facing = Quaternion.Euler(0f, GaleYaw, 0f);
                Vector3 forward = facing * Vector3.forward, right = facing * Vector3.right;
                float road = GaleStand.y;
                var physics = scene.GetPhysicsScene();
                if (physics.IsValid() && physics.Raycast(GaleStand + Vector3.up * 30f, Vector3.down, out var under, 80f, ~0, QueryTriggerInteraction.Ignore))
                { road = under.point.y; report.AppendLine("road found at y " + road.ToString("0.000") + " on " + under.collider.name); }
                Vector3 stand = new Vector3(GaleStand.x, road, GaleStand.z), throwerAt = stand + forward * 7.5f;

                GameObject Body(string id, Vector3 at, float yaw)
                {
                    var art = RosterBook.Load()?.FindPersonArt(id);
                    if (art == null || art.Model == null) return null;
                    var body = In((GameObject)Object.Instantiate(art.Model));
                    body.name = "~RafiCrossFilm-" + id;
                    body.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw + CharacterVisual.PersonModelYaw, 0f));
                    body.transform.localScale = Vector3.one * CharacterVisual.PersonScale;
                    ToonSkin.Apply(body, ToonSkin.PersonOutlineWidth, art.Palette);
                    foreach (var animator in body.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
                    return body;
                }
                var caster = Body("rafi", stand, GaleYaw);
                var thrower = Body("dante", throwerAt, GaleYaw + 180f);
                // One standing just off the path halfway down it, for its height and width against a body.
                Body("cheska", stand + forward * 3.5f + right * 1.4f, GaleYaw - 90f);

                // ⚠️ THE CAST, AS THE KIT MAKES IT (`RafiHeroKit.Crosscurrent.OnActivate`): TIGHT CUT scales the radius by the
                // variant's cost and the speed by its gain, and the life follows from the speed.
                bool tight = tag.Contains("tight"), spentRun = tag.Contains("spent");
                var variant = HeroLoadoutRules.VariantById("rafi.1.tightcut");
                float radius = RafiRules.CurrentRadius * (tight && variant != null ? 1f + variant.Cost : 1f);
                float speed = 8f * (tight && variant != null ? 1f + variant.Gain : 1f);
                float life = RafiRules.CurrentGather + RafiRules.CurrentRange / speed;
                var state = new WorldEffectSnapshot.Field
                {
                    Type = WorldEffectSnapshot.Kind.Current, EventId = 1, Position = stand, Forward = forward, Radius = radius,
                    FirstScale = speed, SecondScale = tight ? 1 : 0, Duration = life, Remaining = life, Owner = 0, Path = Array.Empty<Vector3>(),
                };
                var spent = state; spent.Split = true;

                var oldHost = In(new GameObject("~RafiCrossFilmOld")); var newHost = In(new GameObject("~RafiCrossFilmNew"));
                RafiWaterVisual.NewCurrent = false;
                var oldCurrent = RafiWaterVisual.Build(oldHost.transform, state);
                RafiWaterVisual.NewCurrent = true;
                var newCurrent = RafiWaterVisual.Build(newHost.transform, state);
                oldCurrent.enabled = false; newCurrent.enabled = false;
                stream = newCurrent.Stream;
                if (stream == null) throw new InvalidOperationException("The new current was not built: RafiWaterVisual.NewCurrent made no stream.");

                var target = new RenderTexture(GaleCellWidth, GaleCellHeight, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                var cell = new Texture2D(GaleCellWidth, GaleCellHeight, TextureFormat.RGB24, false);
                var sheet = new Texture2D(GaleCellWidth * 2, GaleCellHeight * 4, TextureFormat.RGB24, false);
                owned.Add(target); owned.Add(cell); owned.Add(sheet);
                cam.targetTexture = target;
                string frames = Path.Combine(OutDir, named + "_frames");
                if (Directory.Exists(frames)) foreach (string old in Directory.GetFiles(frames)) File.Delete(old);
                Directory.CreateDirectory(frames);

                void Shoot(int column, int row)
                {
                    cam.Render();
                    var keep = RenderTexture.active; RenderTexture.active = target;
                    cell.ReadPixels(new Rect(0, 0, GaleCellWidth, GaleCellHeight), 0, 0); cell.Apply();
                    RenderTexture.active = keep;
                    sheet.SetPixels(column * GaleCellWidth, (3 - row) * GaleCellHeight, GaleCellWidth, GaleCellHeight, cell.GetPixels());
                }

                // The new current is drawn past the rule's end: its drop, its splash and its drops.
                float letGo = spentRun ? Mathf.Min(RafiCrossSpentAt, life) : life;
                float seconds = letGo + RafiCurrentStream.TailSeconds + 0.2f;
                int count = Mathf.CeilToInt(seconds * BenchRate) + 1;
                string cost = null, truth = null;
                for (int f = 0; f < count; f++)
                {
                    float t = f / (float)BenchRate;
                    var now = spentRun && t >= RafiCrossSpentAt ? spent : state;
                    for (int view = 0; view < 4; view++)
                    {
                        Vector3 eye, look, up = Vector3.up; float fov;
                        if (view == 0) { eye = stand + forward * 3.2f + right * 6.8f + Vector3.up * 2.4f; look = stand + forward * 3.2f + Vector3.up * 0.85f; fov = 50f; }
                        else if (view == 1) { eye = stand + Vector3.up * 1.55f - forward * 0.15f; look = stand + forward * 6f + Vector3.up * 0.9f; fov = 70f; }
                        else if (view == 2) { eye = throwerAt + Vector3.up * 1.55f + forward * 0.15f; look = stand + Vector3.up * 0.9f; fov = 70f; }
                        else { eye = stand + forward * 3.2f + Vector3.up * 10f; look = stand + forward * 3.2f; fov = 50f; up = forward; }
                        cam.fieldOfView = fov;
                        cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye, up));
                        // Nobody sees their own body from behind their own eyes.
                        if (caster != null) caster.SetActive(view != 1);
                        if (thrower != null) thrower.SetActive(view != 2);

                        // The old current is gone, with its field, at the end of its life.
                        newHost.SetActive(false); oldHost.SetActive(t <= life);
                        oldCurrent.SetState(now); oldCurrent.StepTo(t);
                        Shoot(0, view);

                        oldHost.SetActive(false); newHost.SetActive(true);
                        RafiCurrentStream.StagedEye = eye;
                        // Past the field's life nothing steps the visual: the stream runs its own death, which is this call.
                        if (t <= life) { newCurrent.SetState(now); newCurrent.StepTo(t); } else stream.Pose(t, now);
                        Shoot(1, view);
                    }
                    sheet.Apply();
                    File.WriteAllBytes(Path.Combine(frames, "f" + f.ToString("000") + ".png"), sheet.EncodeToPNG());

                    // Once, mid run: what each costs, and whether the new picture is where and as wide as what hits.
                    float probeAt = Mathf.Min(0.60f, letGo - 0.02f);
                    if (cost == null && t >= probeAt)
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
                        cost = "in flight, at " + t.ToString("0.00") + " s   OLD: " + Count(oldHost, false) + "\n                        NEW: " + Count(newHost, true);
                        // The rule's column at this moment: its middle `travel` along the path at 0.85 m, `radius` round.
                        float travel = Mathf.Min(RafiRules.CurrentRange, Mathf.Max(0f, t - RafiRules.CurrentGather) * speed);
                        var drawn = new Bounds(); bool any = false;
                        foreach (var renderer in newHost.GetComponentsInChildren<MeshRenderer>(false))
                        {
                            if (!renderer.enabled) continue;
                            if (!any) { drawn = renderer.bounds; any = true; } else drawn.Encapsulate(renderer.bounds);
                        }
                        if (any)
                        {
                            float across = Vector3.Dot(drawn.size, new Vector3(Mathf.Abs(right.x), Mathf.Abs(right.y), Mathf.Abs(right.z)));
                            float front = Vector3.Dot(drawn.max - stand, forward), top = drawn.max.y - road, bottom = drawn.min.y - road;
                            truth = "at " + t.ToString("0.00") + " s the rule's column is " + (2f * radius).ToString("0.00") + " m wide, its middle " + travel.ToString("0.00")
                                    + " m out, reaching " + (0.85f - RafiRules.CurrentHalfHeight).ToString("0.00") + " to " + (0.85f + RafiRules.CurrentHalfHeight).ToString("0.00") + " m up"
                                    + "\nthe new current's drawn pieces (bounds, so a little over the true outline): " + across.ToString("0.00") + " m across, front at " + front.ToString("0.00")
                                    + " m out, " + bottom.ToString("0.00") + " to " + top.ToString("0.00") + " m up";
                        }
                    }
                }
                report.AppendLine(count + " frames at " + BenchRate + " a second (" + seconds.ToString("0.00") + " s), in " + frames.Replace('\\', '/'));
                report.AppendLine("each frame: rows are third person from the side, caster's eyes, a thrower's eyes looking back down the path, above; columns are OLD, NEW; no bloom");
                report.AppendLine((tight ? "TIGHT CUT" : "plain") + ": radius " + radius.ToString("0.000") + " m, " + speed.ToString("0.0") + " m/s, life " + life.ToString("0.000") + " s");
                report.AppendLine(spentRun ? "spent at " + RafiCrossSpentAt.ToString("0.00") + " s: the new current lets go then, the old one dims to 12 per cent then" : "nothing spends it: the rule ends at " + life.ToString("0.00") + " s, the old current is gone then, the new one lets go and splashes for " + RafiCurrentStream.TailSeconds.ToString("0.00") + " s after");
                report.AppendLine("of the new current, " + RafiWallCurtain.DayLight.ToString("0.00") + " is light laid on (the bench's is 0.45); drawn " + Mathf.Clamp(radius / FxStudyRafiCross.Radius, 0.1f, 1f).ToString("0.000") + " of its approved width");
                report.AppendLine(truth ?? "truth not measured");
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
                RafiWaterVisual.NewCurrent = switchBefore;
                RafiCurrentStream.StagedEye = null;
                // (Outside Play nothing calls a component's OnDestroy, so the film frees the stream by the same call that does.)
                if (stream != null) stream.Free();
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
    }
}
