using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
    /// ILYAS'S KIT, FILMED WHERE IT IS PLAYED: SKIM, WATER WALL and BAHA's live look, each OLD beside NEW at the same
    /// moments, on Eskinita opened as a PREVIEW scene (the owner's open scene is not touched and nothing is saved), in the
    /// map's own daylight, with no bloom (no game camera blooms), sixty pictures a second, real hero models for scale.
    ///
    /// Each frame is eight pictures: four rows (the caster's eyes, the victim's eyes, from the side, from above) and two
    /// columns (OLD: every switch false, today's look; NEW: the switches on).
    ///
    /// ⚠️ STAGED, AND SAID SO. No match runs; nothing animates the bodies. Each look is driven through its own code:
    ///   * WATER WALL and BAHA through `RafiWaterVisual.Build` and `StepTo` (the replay's path), once with the switch off
    ///     and once on, so both columns are the game's own pictures. These draw outside Play (they have no Update).
    ///   * SKIM's coats from their own builders by reflection, the kit stood in for (the old line is coloured as its
    ///     `Sample` colours it at full strength; the new coat is sampled with its clock set), and the new wake through
    ///     `RafiSkimWake.Pose` with the slide's numbers staged here. The shoe is carried by hand along a throw and a 2 m
    ///     slide at the rule's 5 m/s. The old look has no flight or slide picture, so its column shows the shoe alone then:
    ///     that IS today's look.
    /// ⚠️ THE OLD COLUMN IS CHECKED, NOT ASSUMED: the report counts the renderers it drew at its measured moment, so an
    /// empty column is a number and not a surprise.
    ///
    /// Request: lines in `Temp/rafi-kit-film.request`: `ping` (answered at once), or `<skim|wall|wallsplit|baha|all> <tag>`
    /// (`wallsplit` is the wall broken by a slipper 1.6 s in; `all` is skim, wall, baha; a comma list works). Frames go to `Logs/paete-ability-film/rafikit_<ability>_<tag>_frames/`
    /// (`f0000.png`, with `times.txt`), then `done_rafikit_<tag>.txt` (costs, measurements) or `failed_rafikit_<tag>.txt`.
    /// </summary>
    [InitializeOnLoad]
    internal static class RafiKitFilmPoll
    {
        private const string Request = "Temp/rafi-kit-film.request";
        private static double _next;
        static RafiKitFilmPoll() { EditorApplication.update += Poll; }

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
                if (words[0] == "ping") { Directory.CreateDirectory(RafiKitFilm.OutDir); File.WriteAllText(Path.Combine(RafiKitFilm.OutDir, "done_rafikit_ping.txt"), DateTime.Now.ToString("HH:mm:ss")); continue; }
                RafiKitFilm.Run(words[0], words.Length > 1 ? words[1] : "k1");
            }
        }
    }

    internal static class RafiKitFilm
    {
        internal const string OutDir = "Logs/paete-ability-film";
        private const string Map = "Assets/TumbangPreso/Scenes/Maps/Eskinita.unity";
        private const int CellWidth = 640, CellHeight = 360, Rate = 60, Rows = 4, Columns = 2;
        private const float Base = 100f;
        private static readonly string[] Abilities = { "skim", "wall", "baha" };
        private static readonly string[] RowNames = { "caster's eyes", "victim's eyes", "side", "above" };
        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private struct View
        {
            public Vector3 Eye, Aim; public float Fov; public GameObject Hide;
            public View(Vector3 eye, Vector3 aim, float fov, GameObject hide = null) { Eye = eye; Aim = aim; Fov = fov; Hide = hide; }
        }

        private sealed class Plan
        {
            public string Name;
            public float From, To, MeasureAt;
            public GameObject Old, New;
            /// <summary>Poses both columns for `t`, with the eye of the picture about to be taken.</summary>
            public Action<float, Vector3> Pose = (_, __) => { };
            public View[] Views;
            public readonly List<GameObject> Roots = new List<GameObject>();
            public Action Release = () => { };
            public string Notes = "";
        }

        private sealed class Stage
        {
            public Scene Scene;
            public PhysicsScene Physics;
            public System.Text.StringBuilder Report;
            public GameObject In(GameObject go, Plan plan) { SceneManager.MoveGameObjectToScene(go, Scene); plan.Roots.Add(go); return go; }

            public Vector3 Ground(float x, float z)
            {
                var at = new Vector3(x, 0f, z);
                if (Physics.IsValid() && Physics.Raycast(new Vector3(x, 30f, z), Vector3.down, out var hit, 80f, ~0, QueryTriggerInteraction.Ignore)) at.y = hit.point.y;
                return at;
            }

            public GameObject Person(Plan plan, string id, Vector3 at, float yaw)
            {
                var holder = In(new GameObject("~RafiKit-" + id), plan);
                holder.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
                var art = RosterBook.Load()?.FindPersonArt(id);
                if (art == null || art.Model == null) { Report.AppendLine("  " + id + "'s model was not found"); return holder; }
                var model = (GameObject)Object.Instantiate(art.Model, holder.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.Euler(0f, CharacterVisual.PersonModelYaw, 0f);
                model.transform.localScale = Vector3.one * CharacterVisual.PersonScale;
                ToonSkin.Apply(model, ToonSkin.PersonOutlineWidth, art.Palette);
                foreach (var animator in model.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
                foreach (var collider in model.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
                return holder;
            }

            // The real tsinelas under a holder, its sole's middle on the holder's origin and its length along the holder's z.
            public MeshFilter Shoe(Plan plan, Transform parent)
            {
                var art = RosterBook.Load()?.SlipperArt(0);
                if (art == null || art.Model == null) { Report.AppendLine("  the roster's first tsinelas was not found"); return null; }
                var model = (GameObject)Object.Instantiate(art.Model, parent);
                foreach (var collider in model.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
                ToonSkin.ApplySlipper(model, ToonSkin.PropOutlineWidth);
                var box = new Bounds(); bool any = false;
                foreach (var r in model.GetComponentsInChildren<Renderer>(true)) { if (any) box.Encapsulate(r.bounds); else { box = r.bounds; any = true; } }
                box.center = parent.InverseTransformPoint(box.center);
                if (box.size.x > box.size.z) { model.transform.localRotation = Quaternion.Euler(0f, 90f, 0f) * model.transform.localRotation; box = new Bounds(new Vector3(-box.center.z, box.center.y, box.center.x), new Vector3(box.size.z, box.size.y, box.size.x)); }
                model.transform.localPosition += new Vector3(-box.center.x, -box.min.y, -box.center.z);
                return model.GetComponentInChildren<MeshFilter>();
            }
        }

        private static FieldInfo Field(Type type, string name) => type.GetField(name, Any) ?? throw new MissingFieldException(type.Name, name);
        private static object Invoke(Type type, object on, string name, params object[] args)
            => (type.GetMethod(name, Any) ?? throw new MissingMethodException(type.Name, name)).Invoke(on, args);

        // `Camera.Render` writes the editor's own `_Time` as it sets up, so the film's clock goes in through command buffers
        // (as `QuickReworkFilm` does) for any shader that reads `_Time`. The water pictures read their own `_Age`.
        private static UnityEngine.Rendering.CommandBuffer _clockOpaque, _clockAlpha;
        private static void SetTime(float t)
        {
            float T = Base + t;
            var time = new Vector4(T / 20f, T, T * 2f, T * 3f);
            Shader.SetGlobalVector("_Time", time);
            foreach (var cb in new[] { _clockOpaque, _clockAlpha })
            {
                if (cb == null) continue;
                cb.Clear(); cb.SetGlobalVector("_Time", time);
                cb.SetGlobalVector("_SinTime", new Vector4(Mathf.Sin(T / 8f), Mathf.Sin(T / 4f), Mathf.Sin(T / 2f), Mathf.Sin(T)));
                cb.SetGlobalVector("_CosTime", new Vector4(Mathf.Cos(T / 8f), Mathf.Cos(T / 4f), Mathf.Cos(T / 2f), Mathf.Cos(T)));
                cb.SetGlobalVector("unity_DeltaTime", new Vector4(1f / Rate, Rate, 1f / Rate, Rate));
            }
        }

        // What a column costs as it stands: renderers drawn, triangles in them, line strips, distinct materials.
        private static string Cost(GameObject host)
        {
            int calls = 0, triangles = 0, lines = 0;
            var materials = new HashSet<Material>();
            foreach (var renderer in host.GetComponentsInChildren<Renderer>(false))
            {
                if (!renderer.enabled || renderer.forceRenderingOff) continue;
                if (renderer is LineRenderer) { lines++; calls++; if (renderer.sharedMaterial != null) materials.Add(renderer.sharedMaterial); continue; }
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;
                // The shoe is not the effect.
                if (Staged(renderer, host)) continue;
                int subs = filter.sharedMesh.subMeshCount;
                calls += Mathf.Max(1, Mathf.Min(subs, renderer.sharedMaterials.Length));
                for (int s = 0; s < subs; s++) triangles += (int)(filter.sharedMesh.GetIndexCount(s) / 3);
                foreach (var m in renderer.sharedMaterials) if (m != null) materials.Add(m);
            }
            return calls + " draw calls (" + lines + " line strips), " + triangles + " triangles, " + materials.Count + " materials";
        }

        internal static void Run(string ability, string tag)
        {
            Directory.CreateDirectory(OutDir);
            tag = new string((tag ?? "k1").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray()).ToLowerInvariant();
            if (tag.Length == 0) tag = "k1";
            string donePath = Path.Combine(OutDir, "done_rafikit_" + tag + ".txt"), failedPath = Path.Combine(OutDir, "failed_rafikit_" + tag + ".txt");
            if (File.Exists(donePath)) File.Delete(donePath);
            if (File.Exists(failedPath)) File.Delete(failedPath);
            if (EditorApplication.isPlayingOrWillChangePlaymode) { File.WriteAllText(failedPath, "The editor is in Play mode."); return; }
            string[] wanted = ability == "all" ? Abilities : ability.Split(',');

            var report = new System.Text.StringBuilder();
            var stage = new Stage { Report = report };
            var owned = new List<Object>();
            bool lit = false;
            var glowVariantBefore = RafiWaterVisual.WallGlowVariant;
            bool glowBefore = RafiWaterVisual.WallGlow, wallBefore = RafiWaterVisual.NewWall, bahaBefore = RafiWaterVisual.NewBaha, skimBefore = RafiSkimWater.Painted;
            var sprayBefore = RafiSkimWater.Spray;
            try
            {
                stage.Scene = EditorSceneManager.OpenPreviewScene(Map);
                if (!stage.Scene.IsValid()) throw new InvalidOperationException("No map to film on.");
                stage.Physics = stage.Scene.GetPhysicsScene();
                lit = Unsupported.SetOverrideLightingSettings(stage.Scene);
                Shader.SetGlobalFloat("_CharacterSmoothShade", ToonSkin.CharacterSmoothShade);
                Shader.SetGlobalFloat("_FxStudyBloom", 0f);
                RafiWallCurtain.StagedLight = -1f; RafiCurrentStream.StagedLight = -1f;
                // A tag starting "v11" films the wall glow's k11 look, "k10" the first approved one, any other the current default.
                RafiWaterVisual.WallGlowVariant = tag.StartsWith("v11") ? RafiWaterVisual.WallGlowLook.K11 : tag.StartsWith("k10") ? RafiWaterVisual.WallGlowLook.K10 : glowVariantBefore;
                report.AppendLine("map: " + Map + (lit ? ", lit by its own lighting settings" : ", its lighting could not be put in force") + "; no bloom; " + Rate + " pictures a second");
                report.AppendLine("each frame: rows " + string.Join(", ", RowNames) + "; columns OLD (switches off, today's look), NEW (switches on)");
                report.AppendLine("light laid on, new pieces: " + RafiWaterPaint.Day.Light.ToString("0.00") + " (the wall's and the current's match share)");

                var camGo = new GameObject("~RafiKitCamera");
                SceneManager.MoveGameObjectToScene(camGo, stage.Scene);
                var cam = camGo.AddComponent<Camera>();
                cam.scene = stage.Scene; cam.enabled = false; cam.allowHDR = false;
                cam.nearClipPlane = 0.03f; cam.farClipPlane = 400f;
                cam.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.55f, 0.70f, 0.88f);
                var plain = new RenderTexture(CellWidth, CellHeight, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                var cell = new Texture2D(CellWidth, CellHeight, TextureFormat.RGB24, false);
                owned.Add(plain); owned.Add(cell); owned.Add(camGo);
                _clockOpaque = new UnityEngine.Rendering.CommandBuffer { name = "film clock (opaque)" };
                _clockAlpha = new UnityEngine.Rendering.CommandBuffer { name = "film clock (transparent)" };
                cam.AddCommandBuffer(UnityEngine.Rendering.CameraEvent.BeforeForwardOpaque, _clockOpaque);
                cam.AddCommandBuffer(UnityEngine.Rendering.CameraEvent.BeforeForwardAlpha, _clockAlpha);

                Color[] Shoot(View v)
                {
                    if (v.Hide != null) v.Hide.SetActive(false);
                    cam.fieldOfView = v.Fov;
                    var dir = v.Aim - v.Eye;
                    var up = Mathf.Abs(dir.normalized.y) > 0.95f ? Vector3.forward : Vector3.up;
                    cam.transform.SetPositionAndRotation(v.Eye, Quaternion.LookRotation(dir, up));
                    cam.targetTexture = plain;
                    cam.Render();
                    var keep = RenderTexture.active; RenderTexture.active = plain;
                    cell.ReadPixels(new Rect(0, 0, CellWidth, CellHeight), 0, 0); cell.Apply();
                    RenderTexture.active = keep;
                    if (v.Hide != null) v.Hide.SetActive(true);
                    return cell.GetPixels();
                }

                foreach (string name in wanted)
                {
                    var plan = new Plan { Name = name };
                    report.AppendLine(name + ":");
                    try
                    {
                        Build(stage, plan);
                        string frames = Path.Combine(OutDir, "rafikit_" + name + "_" + tag + "_frames");
                        if (Directory.Exists(frames)) foreach (string old in Directory.GetFiles(frames)) File.Delete(old);
                        Directory.CreateDirectory(frames);
                        var sheet = new Texture2D(CellWidth * Columns, CellHeight * Rows, TextureFormat.RGB24, false);
                        var times = new System.Text.StringBuilder();
                        times.AppendLine("rows: " + string.Join("|", RowNames) + "; columns: OLD|NEW");
                        int count = Mathf.RoundToInt((plan.To - plan.From) * Rate) + 1, peakOld = 0, peakNew = 0;
                        float clipOld = 0f, clipNew = 0f, clipOldMax = 0f, clipNewMax = 0f; int cells = 0;
                        string measured = null;
                        for (int f = 0; f < count; f++)
                        {
                            float t = plan.From + f / (float)Rate;
                            for (int v = 0; v < Rows; v++)
                            {
                                var view = plan.Views[v];
                                SetTime(t);
                                plan.Pose(t, view.Eye);
                                for (int c = 0; c < Columns; c++)
                                {
                                    plan.Old.SetActive(c == 0); plan.New.SetActive(c == 1);
                                    var px = Shoot(view);
                                    sheet.SetPixels(c * CellWidth, (Rows - 1 - v) * CellHeight, CellWidth, CellHeight, px);
                                    float clip = Clipped(px);
                                    if (c == 1) { clipNew += clip; clipNewMax = Mathf.Max(clipNewMax, clip); } else { clipOld += clip; clipOldMax = Mathf.Max(clipOldMax, clip); }
                                    cells++;
                                }
                            }
                            plan.Old.SetActive(true); plan.New.SetActive(true);
                            peakOld = Mathf.Max(peakOld, Calls(plan.Old)); peakNew = Mathf.Max(peakNew, Calls(plan.New));
                            if (measured == null && t >= plan.MeasureAt)
                                measured = "  at " + t.ToString("0.00") + " s   OLD: " + Cost(plan.Old) + "\n               NEW: " + Cost(plan.New);
                            sheet.Apply();
                            File.WriteAllBytes(Path.Combine(frames, "f" + f.ToString("0000") + ".png"), sheet.EncodeToPNG());
                            times.AppendLine(t.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture));
                        }
                        Object.DestroyImmediate(sheet);
                        File.WriteAllText(Path.Combine(frames, "times.txt"), times.ToString());
                        report.Append(plan.Notes);
                        report.AppendLine("  " + count + " frames (" + plan.From.ToString("0.00") + " to " + plan.To.ToString("0.00") + " s) in " + frames.Replace('\\', '/'));
                        report.AppendLine(measured ?? "  cost not measured");
                        report.AppendLine("  most effect draw calls in any one frame: OLD " + peakOld + ", NEW " + peakNew);
                        int per = Mathf.Max(1, cells / 2);
                        report.AppendLine("  share of each picture clipped to white (all channels at or over 0.98), mean and worst: OLD " + (100f * clipOld / per).ToString("0.00") + "% / " + (100f * clipOldMax).ToString("0.00")
                            + "%, NEW " + (100f * clipNew / per).ToString("0.00") + "% / " + (100f * clipNewMax).ToString("0.00") + "% (the map's own sky and paint are in both)");
                        if (peakOld == 0) report.AppendLine("  ⚠️ THE OLD COLUMN DREW NOTHING OF THE EFFECT IN ANY FRAME");
                    }
                    catch (Exception failure)
                    {
                        // The whole exception with its stack, so a failure can be traced from the report alone.
                        report.AppendLine("  FAILED: " + failure);
                        Debug.LogError("[RafiKitFilm] " + name + ": " + failure);
                    }
                    finally
                    {
                        try { plan.Release(); } catch (Exception e) { report.AppendLine("  release: " + e.Message); }
                        foreach (var go in plan.Roots) if (go != null) Object.DestroyImmediate(go);
                    }
                }
                File.WriteAllText(donePath, report.ToString());
            }
            catch (Exception failure)
            {
                File.WriteAllText(failedPath, report + "\n" + failure);
                Debug.LogError("[RafiKitFilm] " + failure);
            }
            finally
            {
                RafiWaterVisual.NewWall = wallBefore; RafiWaterVisual.WallGlow = glowBefore; RafiWaterVisual.NewBaha = bahaBefore; RafiSkimWater.Painted = skimBefore;
                RafiSkimWater.Spray = sprayBefore;
                RafiWallCurtain.StagedEye = null; RafiBahaFlood.StagedEye = null; RafiSkimWater.StagedEye = null;
                RafiWaterVisual.WallGlowVariant = glowVariantBefore;
                _clockOpaque?.Release(); _clockAlpha?.Release(); _clockOpaque = _clockAlpha = null;
                if (lit) Unsupported.RestoreOverrideLightingSettings();
                RenderTexture.active = null;
                foreach (var thing in owned)
                {
                    if (thing == null) continue;
                    if (thing is RenderTexture texture) texture.Release();
                    Object.DestroyImmediate(thing);
                }
                if (stage.Scene.IsValid()) EditorSceneManager.ClosePreviewScene(stage.Scene);
            }
        }

        private static float Clipped(Color[] px)
        {
            int n = 0;
            foreach (var c in px) if (c.r >= 0.98f && c.g >= 0.98f && c.b >= 0.98f) n++;
            return px.Length > 0 ? n / (float)px.Length : 0f;
        }

        private static int Calls(GameObject host)
        {
            int n = 0;
            foreach (var renderer in host.GetComponentsInChildren<Renderer>(false))
                if (renderer.enabled && !renderer.forceRenderingOff && !Staged(renderer, host)
                    && (renderer is LineRenderer || renderer.GetComponent<MeshFilter>()?.sharedMesh != null)) n++;
            return n;
        }

        // A renderer the film stood in (the tsinelas under "~old-shoe" or "~new-shoe"), not one the effect made, unless it
        // is the effect's coat riding on it.
        private static bool Staged(Renderer renderer, GameObject host)
        {
            if (renderer.GetComponent<RafiSkimCoating>() != null || renderer.GetComponent<RafiSkimPainted>() != null) return false;
            for (var t = renderer.transform; t != null && t != host.transform; t = t.parent)
                if (t.name.StartsWith("~")) return true;
            return false;
        }

        private static void Build(Stage s, Plan p)
        {
            switch (p.Name)
            {
                case "skim": BuildSkim(s, p); return;
                case "wall": BuildWall(s, p, false); return;
                case "wallsplit": BuildWall(s, p, true); return;
                case "baha": BuildBaha(s, p); return;
            }
            throw new ArgumentException("No such ability: " + p.Name + " (skim, wall, wallsplit, baha)");
        }

        // ---------------------------------------------------------------- SKIM

        private const float SkimRelease = 0.6f, SkimFlight = 0.6f, SkimTouchAhead = 4.0f;

        private static void BuildSkim(Stage s, Plan p)
        {
            Vector3 fwd = Vector3.forward, right = Vector3.right, up = Vector3.up;
            Vector3 S = s.Ground(0f, -5f);
            var caster = s.Person(p, "rafi", S, 0f);
            Vector3 hand = S + right * 0.75f + up * 0.70f;
            Vector3 touch = s.Ground(S.x, S.z + SkimTouchAhead);
            float touchAt = SkimRelease + SkimFlight, slide = RafiRules.SkimDistance / RafiRules.SkimSpeed, restAt = touchAt + slide;
            var victim = s.Person(p, "dante", touch + fwd * (RafiRules.SkimDistance + 1.4f) + right * 0.5f, 180f);

            p.Old = s.In(new GameObject("~RafiKitOld"), p); p.New = s.In(new GameObject("~RafiKitNew"), p);
            var oldShoe = new GameObject("~old-shoe").transform; oldShoe.SetParent(p.Old.transform, false);
            var newShoe = new GameObject("~new-shoe").transform; newShoe.SetParent(p.New.transform, false);
            var oldFilter = s.Shoe(p, oldShoe); var newFilter = s.Shoe(p, newShoe);
            if (oldFilter == null || newFilter == null) throw new InvalidOperationException("No tsinelas mesh to coat.");

            // OLD: the shipped line coat (`RafiSkimCoating`), built by its own `Build`; `Sample` needs a live kit, so its
            // colour at full strength (what `Sample` writes once the load has risen) is written here instead.
            RafiSkimWater.Painted = false;
            var oldGo = new GameObject("SkimSoleMeniscus"); oldGo.layer = oldFilter.gameObject.layer;
            oldGo.transform.SetParent(oldFilter.transform, false);
            var oldCoat = oldGo.AddComponent<RafiSkimCoating>();
            var tc = typeof(RafiSkimCoating);
            Field(tc, "_mesh").SetValue(oldCoat, oldFilter.sharedMesh); Field(tc, "_target").SetValue(oldCoat, oldFilter);
            Invoke(tc, oldCoat, "Build");
            var line = (LineRenderer)Field(tc, "_edge").GetValue(oldCoat);
            var block = new MaterialPropertyBlock(); block.SetColor("_Color", new Color(.26f, .76f, .78f, .82f)); line.SetPropertyBlock(block);

            // NEW: the painted coat in the wall's colours (`RafiSkimPainted`), by its own `Build` and `Sample`.
            RafiSkimWater.Painted = true;
            var newGo = new GameObject("SkimPaintedCoat"); newGo.layer = newFilter.gameObject.layer;
            newGo.transform.SetParent(newFilter.transform, false);
            var newCoat = newGo.AddComponent<RafiSkimPainted>();
            var tp = typeof(RafiSkimPainted);
            Field(tp, "_target").SetValue(newCoat, newFilter); Field(tp, "_source").SetValue(newCoat, newFilter.sharedMesh);
            Field(tp, "_sourceRenderer").SetValue(newCoat, newFilter.GetComponent<Renderer>()); Field(tp, "_seed").SetValue(newCoat, 3.1f);
            Invoke(tp, newCoat, "Build");
            var coatAge = Field(tp, "_age"); var coatDying = Field(tp, "_dying");
            const float coatDie = 0.35f;

            // NEW: the wake at the touch point, and the spray into a field of puffs of the film's own (none runs outside Play).
            var wakeAt = new GameObject("wake-at").transform; wakeAt.SetParent(p.New.transform, false);
            wakeAt.SetPositionAndRotation(touch, Quaternion.LookRotation(fwd, up));
            var wake = new RafiSkimWake(wakeAt, 0.4f);
            var puffs = new CloudPuffField(p.New.transform, "film spray");
            bool sprayed = false;

            Vector3 ShoeAt(float t)
            {
                if (t < SkimRelease) return hand;
                if (t < touchAt)
                {
                    float k = (t - SkimRelease) / SkimFlight;
                    var flat = Vector3.Lerp(hand, touch, k); flat.y = Mathf.Lerp(hand.y, touch.y, k) + 4f * 1.2f * k * (1f - k);
                    return flat;
                }
                return touch + fwd * Mathf.Min(RafiRules.SkimDistance, (t - touchAt) * RafiRules.SkimSpeed);
            }
            Quaternion ShoeTurn(float t)
            {
                if (t < SkimRelease || t >= touchAt) return Quaternion.LookRotation(fwd, up);
                float k = (t - SkimRelease) / SkimFlight;
                return Quaternion.LookRotation(fwd, up) * Quaternion.Euler(-12f + 24f * k, 540f * k, 0f);
            }

            p.Pose = (t, eye) =>
            {
                var at = ShoeAt(t); var turn = ShoeTurn(t);
                oldShoe.SetPositionAndRotation(at, turn); newShoe.SetPositionAndRotation(at, turn);
                oldGo.SetActive(t < SkimRelease);
                float d = t < restAt ? -1f : t - restAt;
                bool on = d <= coatDie;
                newGo.SetActive(on);
                if (on) { coatAge.SetValue(newCoat, t + 0.5f); coatDying.SetValue(newCoat, d); Invoke(tp, newCoat, "Sample", 0f); }
                bool woke = t >= touchAt;
                wake.Root.gameObject.SetActive(woke);
                if (woke) wake.Pose(t - touchAt, Mathf.Min(RafiRules.SkimDistance, (t - touchAt) * RafiRules.SkimSpeed), slide, eye);
                if (!sprayed && woke) { puffs.Spawn(RafiSkimWater.TouchSpray(touch, 0.7f), Base + touchAt); sprayed = true; }
                puffs.Tick(Base + t);
            };
            p.Release = () => { wake.Dispose(); puffs.Dispose(); };
            p.From = -0.1f; p.To = restAt + RafiSkimWake.Tail + 0.25f; p.MeasureAt = touchAt + 0.2f;
            p.Notes = "  staged: loaded in the hand to " + SkimRelease + " s, a throw to the ground " + SkimTouchAhead + " m ahead at " + touchAt.ToString("0.00")
                + " s, a slide of " + RafiRules.SkimDistance + " m at " + RafiRules.SkimSpeed + " m/s to rest at " + restAt.ToString("0.00") + " s; the victim stands "
                + (RafiRules.SkimDistance + 1.4f).ToString("0.0") + " m past the touch\n";
            Vector3 mid = touch + fwd * 1f;
            p.Views = new[]
            {
                new View(S + up * 1.55f - fwd * 0.15f, touch + fwd * 0.6f, 62f, caster),
                new View(victim.transform.position + up * 1.5f, touch + up * 0.2f, 62f, victim),
                new View(mid + right * 3.6f + up * 0.9f, mid + up * 0.25f - right * 0.4f, 52f),
                new View(touch - fwd * 1.2f + up * 8.5f, touch - fwd * 1.2f, 62f),
            };
        }

        // ---------------------------------------------------------------- WATER WALL

        private static void BuildWall(Stage s, Plan p, bool split)
        {
            Vector3 fwd = Vector3.forward, right = Vector3.right, up = Vector3.up;
            Vector3 S = s.Ground(0f, -5f), wallAt = s.Ground(S.x, S.z + 3f);
            var caster = s.Person(p, "rafi", S, 0f);
            var victim = s.Person(p, "dante", wallAt + fwd * 4.5f, 180f);
            s.Person(p, "cheska", wallAt + right * (RafiRules.WallHalfWidth + 0.6f), -90f);
            const float splitAt = 1.6f;
            var state = new WorldEffectSnapshot.Field
            {
                Type = WorldEffectSnapshot.Kind.Waterwall, EventId = 1, Position = wallAt, Forward = fwd, Radius = RafiRules.WallHalfWidth,
                Duration = RafiRules.WallSeconds, Remaining = RafiRules.WallSeconds, Owner = 0, Path = Array.Empty<Vector3>(),
            };
            var broken = state; broken.Split = true; broken.FirstScale = splitAt;
            p.Old = s.In(new GameObject("~RafiKitOld"), p); p.New = s.In(new GameObject("~RafiKitNew"), p);
            RafiWaterVisual.NewWall = false;
            var oldWall = RafiWaterVisual.Build(p.Old.transform, state);
            RafiWaterVisual.NewWall = true;
            var newWall = RafiWaterVisual.Build(p.New.transform, state);
            oldWall.enabled = false; newWall.enabled = false;
            var curtain = newWall.Curtain ?? throw new InvalidOperationException("The new wall was not built.");
            float life = RafiRules.WallSeconds, letGo = split ? splitAt : life;
            p.Pose = (t, eye) =>
            {
                var now = split && t >= splitAt ? broken : state;
                // The old wall goes with its field at the end of its life; the new one runs its fall and splash after.
                oldWall.gameObject.SetActive(t >= 0f && t <= life);
                if (t >= 0f && t <= life) { oldWall.SetState(now); oldWall.StepTo(t); }
                RafiWallCurtain.StagedEye = eye;
                newWall.gameObject.SetActive(t >= 0f);
                if (t >= 0f) { if (t <= life) { newWall.SetState(now); newWall.StepTo(t); } else curtain.Pose(t, now); }
            };
            p.From = -0.1f; p.To = letGo + RafiWallCurtain.TailSeconds + 0.3f; p.MeasureAt = 1.2f;
            p.Notes = "  the wall is 3.0 m in front of the caster, the victim (a thrower) 4.5 m beyond it; " + (RafiRules.WallHalfWidth * 2f).ToString("0.0") + " m wide and "
                + RafiRules.WallHeight.ToString("0.0") + " m tall by the rule; " + (split ? "broken at " + splitAt + " s" : "nothing breaks it: the rule ends at " + life + " s") + "\n";
            Vector3 mid = wallAt + up * 0.9f;
            p.Views = new[]
            {
                new View(S + up * 1.55f - fwd * 0.15f, mid, 70f, caster),
                new View(victim.transform.position + up * 1.55f + fwd * 0.15f, mid, 70f, victim),
                new View(wallAt + right * 6.5f + up * 1.4f + fwd * 0.3f, mid - right * 0.5f, 55f),
                new View(wallAt + up * 9f - fwd * 0.5f, wallAt, 55f),
            };
        }

        // ---------------------------------------------------------------- BAHA

        private const float BahaRun = 12f, CrateAhead = 6.5f;

        private static void BuildBaha(Stage s, Plan p)
        {
            Vector3 fwd = Vector3.forward, right = Vector3.right, up = Vector3.up;
            Vector3 R = s.Ground(0f, -8f);
            var caster = s.Person(p, "rafi", R, 0f);
            var victim = s.Person(p, "zack", R + fwd * 8.5f - right * 0.8f, 180f);
            // A crate on the court that stops two of the nine lanes (as cover does in a match): the front must break there.
            var crate = s.In(GameObject.CreatePrimitive(PrimitiveType.Cube), p);
            crate.name = "~crate";
            foreach (var collider in crate.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
            crate.transform.position = R + fwd * CrateAhead + right * 1.875f + up * 0.5f;
            crate.transform.localScale = new Vector3(1.7f, 1f, 1f);

            var origin = R + up * 0.015f;
            var path = new Vector3[9];
            float distance = 0f;
            for (int i = 0; i < 9; i++)
            {
                float side = Mathf.Lerp(-RafiRules.BahaHalfWidth, RafiRules.BahaHalfWidth, i / 8f);
                // The rule's `ClearDistance`: lanes inside the crate's span stop 0.06 m short of its near face.
                bool blocked = side > 1.875f - 0.85f && side < 1.875f + 0.85f;
                float clear = blocked ? CrateAhead - 0.5f - 0.06f : BahaRun;
                path[i] = origin + right * side + fwd * clear; distance = Mathf.Max(distance, clear);
            }
            float duration = RafiRules.BahaDuration(distance);
            var state = new WorldEffectSnapshot.Field
            {
                Type = WorldEffectSnapshot.Kind.Baha, EventId = 7, Position = origin, Forward = fwd, Radius = RafiRules.BahaHalfWidth,
                FirstScale = RafiRules.BahaSpeed, SecondScale = distance, Duration = duration, Remaining = duration, Owner = 0, Path = path,
            };
            p.Old = s.In(new GameObject("~RafiKitOld"), p); p.New = s.In(new GameObject("~RafiKitNew"), p);
            RafiWaterVisual.NewBaha = false;
            var oldBaha = RafiWaterVisual.Build(p.Old.transform, state);
            RafiWaterVisual.NewBaha = true;
            var newBaha = RafiWaterVisual.Build(p.New.transform, state);
            oldBaha.enabled = false; newBaha.enabled = false;
            if (newBaha.Flood == null) throw new InvalidOperationException("The new Baha was not built.");
            // The lanes' spray goes into a field of puffs of the film's own (none runs outside Play).
            var puffs = new CloudPuffField(p.New.transform, "film spray");
            float now = 0f;
            RafiSkimWater.Spray = puff => puffs.Spawn(puff, Base + now);
            p.Release = () => puffs.Dispose();
            p.Pose = (t, eye) =>
            {
                bool on = t >= 0f && t <= duration;
                oldBaha.gameObject.SetActive(on); newBaha.gameObject.SetActive(t >= 0f);
                if (t < 0f) return;
                if (on) oldBaha.StepTo(t);
                RafiBahaFlood.StagedEye = eye;
                now = t;
                // Past the field's end the look still has its puddles: it is posed directly, since the visual's own clock stops.
                if (on) newBaha.StepTo(t); else newBaha.Flood.Pose(t, eye);
                puffs.Tick(Base + t);
            };
            p.From = -0.1f; p.To = duration + 5f; p.MeasureAt = RafiRules.BahaWarning + 1.0f;
            p.Notes = "  staged: the lane runs " + BahaRun + " m (court edge stand-in), two lanes stop at " + (CrateAhead - 0.56f).ToString("0.00") + " m on a crate; warning "
                + RafiRules.BahaWarning + " s, " + RafiRules.BahaSpeed + " m/s, duration " + duration.ToString("0.00") + " s; the victim stands 8.5 m out, in a free lane\n";
            Vector3 mid = R + fwd * 6f;
            p.Views = new[]
            {
                new View(R + up * 1.55f - fwd * 0.15f, R + fwd * 6f, 70f, caster),
                new View(victim.transform.position + up * 1.5f, R + fwd * 2f, 70f, victim),
                new View(mid + right * 8f + up * 1.6f, mid - right * 1f, 62f),
                new View(mid + up * 15f - fwd * 0.5f, mid, 62f),
            };
        }
    }
}
