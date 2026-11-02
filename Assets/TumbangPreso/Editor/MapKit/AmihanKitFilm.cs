using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TumbangPreso.Abilities;
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
    /// AMIHAN'S KIT, FILMED OLD BESIDE NEW: FEATHERFALL (`AmihanFeatherfallLook`) and AIRBURST's live phase
    /// (`AmihanAirburstLook`; the cutscene is not filmed), on Eskinita opened as a PREVIEW scene (the owner's open scene is
    /// not touched and nothing is saved), in the map's own daylight, no bloom (no game camera blooms), 60 pictures a
    /// second, with real hero models for scale. DRIFT and WHIRLWIND are final and are not filmed here.
    ///
    /// Every picture is a grid: four rows (the caster's eyes, a target's eyes, side, above) by two columns, OLD on the
    /// left and NEW on the right, the same moment and the same camera.
    ///
    /// ⚠️ STAGED, AND SAID SO. No match runs and nothing animates outside Play, so bodies are still poses moved by the
    /// rule's own numbers, and every look is stepped on the film's clock:
    ///   * featherfall: she takes off at 0 s, is held the full 5 s and comes down at 3.5 m/s, landing at 5.8 s (the rule's
    ///     numbers, `AmihanRules`). OLD is `AmihanUpdraftLaunch` stepped by its own `StepTo`. ⚠️ THE OLD HOLD
    ///     (`AmihanHoverRing`: the streak column, gust rings and dust) IS NOT IN THE OLD COLUMN: it is driven by a live
    ///     `CharacterMotor` in `LateUpdate` and cannot be drawn outside Play. NEW is the look a match draws
    ///     (`FxStudyFeatherfall` at `AmihanFeatherfallLook.Light`), posed from the same flight numbers, and its cloud puffs.
    ///   * airburst: 0 s is the cutscene handing back, which is when `StormSurge.OnActivate` runs. OLD is the cutscene's
    ///     fan (`AmihanStormFan`) from the age the hand-back spawns it at (`AmihanStorm.CutsceneTail` after the release),
    ///     stepped by its own `StepTo`. NEW is `AmihanAirburstPicture` (with a puff at her feet). Three others: one 6 m in
    ///     front and one 6.4 m behind her (both caught, flying outward along `AmihanStorm.BlowDirection` from where the
    ///     cutscene's last frame left them, at the throw's own speed and lift), and one 10.3 m behind (safe, standing).
    ///
    /// Request: lines in `Temp/amihan-kit-film.request`: `ping` (answered at once), or `&lt;featherfall|airburst|all&gt; &lt;tag&gt;`.
    /// Frames: `Logs/paete-ability-film/amihankit_&lt;ability&gt;_&lt;tag&gt;_frames/` with `times.txt`, then
    /// `done_amihankit_&lt;tag&gt;.txt` (with draw calls, triangles and materials, old and new) or `failed_amihankit_&lt;tag&gt;.txt`.
    /// </summary>
    [InitializeOnLoad]
    internal static class AmihanKitFilmPoll
    {
        private const string Request = "Temp/amihan-kit-film.request";
        private static double _next;
        static AmihanKitFilmPoll() { EditorApplication.update += Poll; }

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
                if (words[0] == "ping") { Directory.CreateDirectory(AmihanKitFilm.OutDir); File.WriteAllText(Path.Combine(AmihanKitFilm.OutDir, "done_amihankit_ping.txt"), DateTime.Now.ToString("HH:mm:ss")); continue; }
                AmihanKitFilm.Run(words[0], words.Length > 1 ? words[1] : "a1");
            }
        }
    }

    internal static class AmihanKitFilm
    {
        internal const string OutDir = "Logs/paete-ability-film";
        private const string Map = "Assets/TumbangPreso/Scenes/Maps/Eskinita.unity";
        private const int CellWidth = 640, CellHeight = 360, Rate = 60;
        private const float Base = 100f;
        private static readonly string[] Abilities = { "featherfall", "airburst" };
        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        // A view's eye and aim are asked for after the moment is posed, so a camera can ride a body that moves.
        private struct View
        {
            public string Name; public Func<Vector3> Eye, Aim; public float Fov, Near; public GameObject Hide;
            public View(string name, Func<Vector3> eye, Func<Vector3> aim, float fov, GameObject hide = null, float near = 0.03f) { Name = name; Eye = eye; Aim = aim; Fov = fov; Hide = hide; Near = near; }
            public View(string name, Vector3 eye, Vector3 aim, float fov, GameObject hide = null, float near = 0.03f) : this(name, () => eye, () => aim, fov, hide, near) { }
        }

        private sealed class Plan
        {
            public string Name;
            public readonly List<(float from, float to)> Segments = new List<(float, float)>();
            public Action<float> Pose = _ => { };
            /// <summary>Turns whatever faces the lens to this view's eye, after `Pose`, before the picture.</summary>
            public Action<float, Vector3> Face = (_, __) => { };
            public View[] Views;
            public readonly List<GameObject> Roots = new List<GameObject>();
            public readonly List<GameObject> Old = new List<GameObject>(), New = new List<GameObject>();
            public float[] CostAt = { 0f };
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
                var holder = In(new GameObject("~AmihanKit-" + id), plan);
                holder.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
                var art = RosterBook.Load()?.FindPersonArt(id);
                if (art == null || art.Model == null) { Report.AppendLine("  " + id + "'s model was not found"); return holder; }
                var model = (GameObject)Object.Instantiate(art.Model, holder.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.Euler(0f, CharacterVisual.PersonModelYaw, 0f);
                model.transform.localScale = Vector3.one * CharacterVisual.PersonScale;
                ToonSkin.Apply(model, ToonSkin.PersonOutlineWidth, art.Palette);
                foreach (var animator in model.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
                return holder;
            }

            // Runs a game call and moves whatever it made at the root of the owner's scene into this one.
            public List<GameObject> Capture(Plan plan, Action make)
            {
                var active = SceneManager.GetActiveScene();
                var before = new HashSet<GameObject>(active.isLoaded ? active.GetRootGameObjects() : new GameObject[0]);
                make();
                var made = new List<GameObject>();
                if (active.isLoaded)
                    foreach (var go in active.GetRootGameObjects())
                        if (!before.Contains(go) && (go.hideFlags & HideFlags.DontSave) == 0) { SceneManager.MoveGameObjectToScene(go, Scene); plan.Roots.Add(go); made.Add(go); }
                return made;
            }
        }

        private static UnityEngine.Rendering.CommandBuffer _clockOpaque, _clockAlpha;

        // As `QuickReworkFilm.SetTime`: the film's clock reaches `_Time` through command buffers inside the render.
        private static void SetTime(float t)
        {
            float T = Base + t;
            var time = new Vector4(T / 20f, T, T * 2f, T * 3f);
            var sin = new Vector4(Mathf.Sin(T / 8f), Mathf.Sin(T / 4f), Mathf.Sin(T / 2f), Mathf.Sin(T));
            var cos = new Vector4(Mathf.Cos(T / 8f), Mathf.Cos(T / 4f), Mathf.Cos(T / 2f), Mathf.Cos(T));
            var delta = new Vector4(1f / Rate, Rate, 1f / Rate, Rate);
            Shader.SetGlobalVector("_Time", time);
            foreach (var cb in new[] { _clockOpaque, _clockAlpha })
            {
                if (cb == null) continue;
                cb.Clear();
                cb.SetGlobalVector("_Time", time); cb.SetGlobalVector("_SinTime", sin);
                cb.SetGlobalVector("_CosTime", cos); cb.SetGlobalVector("unity_DeltaTime", delta);
            }
        }

        internal static void Run(string ability, string tag)
        {
            Directory.CreateDirectory(OutDir);
            tag = new string((tag ?? "a1").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray()).ToLowerInvariant();
            if (tag.Length == 0) tag = "a1";
            string donePath = Path.Combine(OutDir, "done_amihankit_" + tag + ".txt"), failedPath = Path.Combine(OutDir, "failed_amihankit_" + tag + ".txt");
            if (File.Exists(donePath)) File.Delete(donePath);
            if (File.Exists(failedPath)) File.Delete(failedPath);
            if (EditorApplication.isPlayingOrWillChangePlaymode) { File.WriteAllText(failedPath, "The editor is in Play mode."); return; }
            string[] wanted = ability == "all" ? Abilities : ability.Split(',');

            var report = new System.Text.StringBuilder();
            var stage = new Stage { Report = report };
            var owned = new List<Object>();
            var active = SceneManager.GetActiveScene();
            bool wasDirty = active.isLoaded && active.isDirty;
            bool lit = false;
            try
            {
                stage.Scene = EditorSceneManager.OpenPreviewScene(Map);
                if (!stage.Scene.IsValid()) throw new InvalidOperationException("No map to film on.");
                stage.Physics = stage.Scene.GetPhysicsScene();
                lit = Unsupported.SetOverrideLightingSettings(stage.Scene);
                Shader.SetGlobalFloat("_CharacterSmoothShade", ToonSkin.CharacterSmoothShade);
                Shader.SetGlobalFloat("_FxStudyBloom", 0f);
                report.AppendLine("map: " + Map + (lit ? ", lit by its own lighting settings" : ", its lighting could not be put in force") + "; no bloom; " + Rate + " pictures a second; columns OLD | NEW");

                var camGo = new GameObject("~AmihanKitCamera");
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

                Color[] Shoot(View v, Vector3 eye)
                {
                    bool hid = v.Hide != null && v.Hide.activeSelf;
                    if (hid) v.Hide.SetActive(false);
                    cam.fieldOfView = v.Fov;
                    // A high camera clips what stands between it and the court (an awning, a roof), so the row shows the effect.
                    cam.nearClipPlane = Mathf.Max(0.03f, v.Near);
                    var dir = v.Aim() - eye;
                    var up = Mathf.Abs(dir.normalized.y) > 0.95f ? Vector3.forward : Vector3.up;
                    cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(dir, up));
                    cam.targetTexture = plain;
                    cam.Render();
                    var keep = RenderTexture.active; RenderTexture.active = plain;
                    cell.ReadPixels(new Rect(0, 0, CellWidth, CellHeight), 0, 0); cell.Apply();
                    RenderTexture.active = keep;
                    if (hid) v.Hide.SetActive(true);
                    return cell.GetPixels();
                }

                foreach (string name in wanted)
                {
                    var plan = new Plan { Name = name };
                    report.AppendLine(name + ":");
                    try
                    {
                        Build(stage, plan);
                        foreach (float at in plan.CostAt)
                        {
                            plan.Pose(at);
                            plan.Face(at, plan.Views[2].Eye());
                            report.AppendLine("  cost at " + at.ToString("0.00") + " s, before batching: OLD " + Cost(plan.Old) + "; NEW " + Cost(plan.New));
                        }

                        string frames = Path.Combine(OutDir, "amihankit_" + name + "_" + tag + "_frames");
                        if (Directory.Exists(frames)) foreach (string old in Directory.GetFiles(frames)) File.Delete(old);
                        Directory.CreateDirectory(frames);
                        int rows = plan.Views.Length;
                        var sheet = new Texture2D(CellWidth * 2, CellHeight * rows, TextureFormat.RGB24, false);
                        var times = new System.Text.StringBuilder();
                        times.AppendLine("rows top to bottom: " + string.Join("|", plan.Views.Select(v => v.Name)) + "; columns: OLD|NEW");
                        int f = 0;
                        foreach (var (from, to) in plan.Segments)
                        {
                            int count = Mathf.RoundToInt((to - from) * Rate) + 1;
                            for (int i = 0; i < count; i++, f++)
                            {
                                float t = from + i / (float)Rate;
                                SetTime(t);
                                plan.Pose(t);
                                for (int column = 0; column < 2; column++)
                                {
                                    var hide = column == 0 ? plan.New : plan.Old;
                                    var was = hide.Where(g => g != null && g.activeSelf).ToList();
                                    foreach (var g in was) g.SetActive(false);
                                    for (int v = 0; v < rows; v++)
                                    {
                                        SetTime(t);
                                        Vector3 eye = plan.Views[v].Eye();
                                        plan.Face(t, eye);
                                        // Row 0 at the top of the picture: textures count rows from the bottom.
                                        sheet.SetPixels(column * CellWidth, (rows - 1 - v) * CellHeight, CellWidth, CellHeight, Shoot(plan.Views[v], eye));
                                    }
                                    foreach (var g in was) g.SetActive(true);
                                }
                                sheet.Apply();
                                File.WriteAllBytes(Path.Combine(frames, "f" + f.ToString("0000") + ".png"), sheet.EncodeToPNG());
                                times.AppendLine(t.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture));
                            }
                        }
                        Object.DestroyImmediate(sheet);
                        File.WriteAllText(Path.Combine(frames, "times.txt"), times.ToString());
                        report.AppendLine("  " + f + " frames in " + frames.Replace('\\', '/') + "; rows: " + string.Join(", ", plan.Views.Select(v => v.Name)));
                    }
                    catch (Exception failure)
                    {
                        report.AppendLine("  FAILED: " + failure.GetType().Name + ": " + failure.Message);
                        Debug.LogError("[AmihanKitFilm] " + name + ": " + failure);
                    }
                    finally
                    {
                        foreach (var go in plan.Roots) if (go != null) Object.DestroyImmediate(go);
                    }
                }
                File.WriteAllText(donePath, report.ToString());
            }
            catch (Exception failure)
            {
                File.WriteAllText(failedPath, report + "\n" + failure);
                Debug.LogError("[AmihanKitFilm] " + failure);
            }
            finally
            {
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
                if (active.isLoaded && active.isDirty && !wasDirty)
                {
                    var clear = typeof(EditorSceneManager).GetMethod("ClearSceneDirtiness", Any);
                    if (clear != null) { clear.Invoke(null, new object[] { active }); File.AppendAllText(File.Exists(donePath) ? donePath : failedPath, "the owner's open scene had been marked changed by a bake and was cleared\n"); }
                    else File.AppendAllText(File.Exists(donePath) ? donePath : failedPath, "the owner's open scene was marked changed (nothing of the film's is left in it)\n");
                }
            }
        }

        // Draw calls as renderers times their sub-meshes, triangles as their meshes hold, distinct materials: only what is
        // active, enabled and not forced off at that moment.
        private static string Cost(IEnumerable<GameObject> roots)
        {
            int draws = 0; long tris = 0; var mats = new HashSet<Material>();
            foreach (var root in roots)
            {
                if (root == null || !root.activeInHierarchy) continue;
                foreach (var r in root.GetComponentsInChildren<Renderer>(false))
                {
                    if (!r.enabled || r.forceRenderingOff) continue;
                    var mesh = r.GetComponent<MeshFilter>()?.sharedMesh;
                    if (mesh == null) continue;
                    if (r.GetComponent<VfxRenderTag>() == null && r.sharedMaterial != null && r.sharedMaterial.shader.name.Contains("Toon")) continue;
                    int subs = Mathf.Max(1, Mathf.Min(mesh.subMeshCount, r.sharedMaterials.Length));
                    draws += subs;
                    for (int s = 0; s < subs; s++) tris += mesh.GetIndexCount(s) / 3;
                    foreach (var m in r.sharedMaterials) if (m != null) mats.Add(m);
                }
            }
            return draws + " draws, " + tris + " triangles, " + mats.Count + " materials";
        }

        private static void Build(Stage s, Plan p)
        {
            switch (p.Name)
            {
                case "featherfall": BuildFeatherfall(s, p); return;
                case "airburst": BuildAirburst(s, p); return;
            }
            throw new ArgumentException("No such ability: " + p.Name + " (featherfall, airburst or all)");
        }

        // ---------------------------------------------------------------- FEATHERFALL

        private static void BuildFeatherfall(Stage s, Plan p)
        {
            Vector3 fwd = Vector3.forward, right = Vector3.right, up = Vector3.up;
            const float High = AmihanRules.UpdraftHeight, Rise = AmihanRules.UpdraftRiseSeconds, Hold = AmihanRules.UpdraftSeconds, Down = AmihanRules.UpdraftDescentSpeed;
            float landAt = Hold + High / Down;
            Vector3 S = s.Ground(0f, -2f);
            var her = s.Person(p, "amihan", S, 0f);
            var taya = s.Person(p, "dante", S + fwd * 2.2f + right * 0.6f, 195f);
            float Gap(float t) => t < 0f ? 0f : t < Rise ? High * FxStudyKit.EaseOut(t / Rise) : t < Hold ? High : Mathf.Max(0f, High - Down * (t - Hold));
            // She moves while held, as a match lets her: 1.8 m to her right between 1.0 and 2.3 s, so the trail and the lean
            // of the updraft have something to follow.
            Vector3 Across(float t) => right * (1.8f * FxStudyKit.Ease(FxStudyKit.Run(t, 1.0f, 2.3f)));
            Vector3 Feet(float t) => S + Across(t) + up * Gap(t);

            // OLD: the sample the owner rejected (a1's NEW), riding under her.
            var oldHolder = s.In(new GameObject("~AmihanKit-featherfall-old"), p);
            oldHolder.transform.position = S;
            var fx = new FxStudyFeatherfall(oldHolder.transform, AmihanFeatherfallLook.Light, false);
            fx.LaunchAt(S); fx.LandAt(S + Across(landAt));
            var puffs = new CloudPuffField(oldHolder.transform);
            puffs.Spawn(FxStudyFeatherfall.TakeOffPuff(S), 0.0f);
            puffs.Spawn(FxStudyFeatherfall.LandingPuff(S + Across(landAt)), landAt);
            p.Old.Add(oldHolder);

            var newHolder = s.In(new GameObject("~AmihanKit-featherfall-new"), p);
            var painted = new AmihanFeatherfallPainted(newHolder.transform);
            p.New.Add(newHolder);
            s.Report.AppendLine("  STAGED: takes off at 0 s, " + High + " m in " + Rise + " s, moves 1.8 m right from 1.0 to 2.3 s, held the full " + Hold + " s, down at " + Down
                + " m/s, lands at " + landAt.ToString("0.00") + " s. OLD is a1's look (FxStudyFeatherfall, rejected); NEW is AmihanFeatherfallPainted.");

            p.Pose = t =>
            {
                float gap = Gap(t);
                her.transform.position = Feet(t);
                oldHolder.transform.position = S + Across(t);
                float left = t >= 0f && t < Hold ? 1f - t / Hold : -1f;
                fx.Pose(t, gap, left, t, t - landAt);
                puffs.Hold(t);
                Vector3 g = S + Across(t);
                painted.Pose(new AmihanFeatherfallPainted.Moment
                {
                    Age = t, Gap = t >= landAt ? 0f : gap, Ground = g, Left = left,
                    ReleaseAge = Hold, ReleaseGap = High, ReleaseGround = S + Across(Hold),
                    SinceLand = t - landAt, LandGround = S + Across(landAt), LaunchGround = S, Feet = Feet,
                });
            };
            p.CostAt = new[] { 0.3f, 2.5f, 5.5f };
            p.Segments.Add((-0.15f, 2.7f)); p.Segments.Add((4.3f, landAt + AmihanFeatherfallPainted.FallLife));
            p.Views = new[]
            {
                // Her own eyes, looking ahead and down, where the time left lies under her soles.
                new View("caster's eyes", () => her.transform.position + up * 1.45f + fwd * 0.1f, () => her.transform.position + fwd * 3.2f - up * 0.6f, 75f, her),
                new View("taya's eyes, under her", () => taya.transform.position + up * 1.4f + (S - taya.transform.position).normalized * 0.15f, () => her.transform.position + up * 0.8f, 70f, taya),
                new View("side", S + right * 6.5f + up * 2.0f - fwd * 1.5f, S + right * 0.9f + up * 1.8f, 55f),
                new View("above", S + up * 10f - fwd * 0.5f + right * 0.9f, S + right * 0.9f, 45f, null, 3f),
            };
        }

        // ---------------------------------------------------------------- AIRBURST, live phase

        private static void BuildAirburst(Stage s, Plan p)
        {
            Vector3 fwd = Vector3.forward, right = Vector3.right, up = Vector3.up;
            Vector3 S = s.Ground(0f, -2f);
            var her = s.Person(p, "amihan", S, 0f);
            var front = s.Person(p, "dante", s.Ground(S.x + 1.5f, S.z + 6f), 190f);
            var back = s.Person(p, "cheska", s.Ground(S.x - 4f, S.z - 5f), 30f);
            s.Person(p, "zack", s.Ground(S.x + 6.5f, S.z - 8f), -30f);
            var thrown = new[] { front, back };
            var starts = thrown.Select(g => g.transform.position).ToArray();
            float hold = AmihanRules.StormSurgeHoldSeconds;
            // Where the throw has each one at a moment: the cutscene left them `CutsceneTail` less 0.01 s a metre into it
            // (`AmihanStorm.Release`), and the throw flies on at its own speed and lift under the game's gravity.
            Vector3 Flown(int i, float t)
            {
                Vector3 start = starts[i], flat = start - S; flat.y = 0f;
                float at = Mathf.Clamp(AmihanStorm.CutsceneTail - flat.magnitude * AmihanStorm.CutsceneHitPerMetre + t, 0f, hold);
                Vector3 dir = AmihanStorm.BlowDirection(S, fwd, start);
                float rise = Mathf.Max(0f, AmihanRules.StormSurgeLift * at - 0.5f * Balance.CharacterGravity * at * at);
                return start + dir * (AmihanRules.StormSurgeSpeed * at) + up * rise;
            }

            // OLD: the picture the owner rejected (a1's NEW).
            var oldHolder = s.In(new GameObject("~AmihanKit-airburst-old"), p);
            var picture = new AmihanAirburstPicture(oldHolder.transform, S, fwd, AmihanAirburstPicture.CourtBox, AmihanAirburstLook.Light);
            var puffs = new CloudPuffField(oldHolder.transform);
            var puff = FxStudyFeatherfall.LandingPuff(S); puff.Scale = 2.2f; puff.Reach = 0.9f;
            puffs.Spawn(puff, 0f);
            p.Old.Add(oldHolder);

            var newHolder = s.In(new GameObject("~AmihanKit-airburst-new"), p);
            var paths = new List<Func<float, Vector3>>();
            for (int i = 0; i < thrown.Length; i++) { int k = i; paths.Add(t => Flown(k, t) + up * 0.8f); }
            var painted = new AmihanAirburstPainted(newHolder.transform, S, fwd, AmihanAirburstPicture.CourtBox, paths);
            p.New.Add(newHolder);
            s.Report.AppendLine("  LIVE PHASE ONLY: 0 s is the hand-back. Caught: one 6.2 m in front, one 6.4 m behind, flying from where the cutscene left them; safe: one 10.3 m behind. Hold "
                + hold.ToString("0.00") + " s. OLD is a1's look (AmihanAirburstPicture, rejected); NEW is AmihanAirburstPainted, " + painted.Seconds.ToString("0.00") + " s.");

            p.Pose = t =>
            {
                for (int i = 0; i < thrown.Length; i++) thrown[i].transform.position = Flown(i, t);
                oldHolder.SetActive(t >= 0f && t <= picture.Seconds);
                puffs.Hold(Mathf.Max(0f, t));
                painted.Pose(t);
            };
            p.Face = (t, eye) => picture.Pose(t, eye);
            p.CostAt = new[] { 0.15f, 0.6f };
            p.Segments.Add((-0.15f, painted.Seconds + 0.1f));
            Vector3 mid = S + fwd * 3f;
            p.Views = new[]
            {
                new View("caster's eyes", S - fwd * 0.2f + up * 1.45f, S + fwd * 8f + up * 0.2f, 75f, her),
                new View("victim's eyes (thrown, 6 m in front)", () => front.transform.position + up * 1.3f, () => S + up * 0.8f, 75f, front),
                new View("side", S + right * 7.6f + up * 4.5f - fwd * 2f, S + fwd * 6f, 80f, null, 0.5f),
                new View("above", S + up * 17f + fwd * 2.5f, S + fwd * 2.5f, 78f, null, 7f),
            };
        }
    }
}
