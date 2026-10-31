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
    /// YASMIN'S KIT, FILMED OLD BESIDE NEW: COLD FEET (`CheskaColdFeetLook`), FROSTBITE (`CheskaFrostbiteCoating.Crust` and
    /// the Frozen casing `CheskaFrozenLook`) and ABSOLUTE ZERO's live phase (`CheskaAbsoluteZeroLook`), on Eskinita opened
    /// as a PREVIEW scene (the owner's open scene is not touched and nothing is saved), in the map's own daylight, no
    /// bloom (no game camera blooms), 60 pictures a second, with real hero models for scale.
    ///
    /// Every picture is a grid: four rows (the caster's eyes, the victim's eyes, side, above) by two columns, OLD (the
    /// look's switch off) on the left and NEW (on) on the right, the same moment and the same camera.
    ///
    /// ⚠️ STAGED, AND SAID SO. No match runs and nothing animates outside Play, so bodies are still poses and every look
    /// is stepped by its own `StepTo` on the film's clock:
    ///   * coldfeet: both fields are built by `FrostSurfacePresentation.Build` as the cast builds them, the switch flipped
    ///     for each; filmed over its first 1.8 s and its last 1.65 s (the middle is the same picture).
    ///   * frostbite: the coat is assembled by reflection exactly as `CheskaFrostbiteCoating.Ensure` assembles it (that
    ///     needs a live kit), and its strength is the coating's own rule written out below for this timeline: loaded in
    ///     her hand at 0 s, thrown at 1.0 s, hits the victim 6 m away at 1.55 s. The casings are made by the game's own
    ///     `HeroHazards.SpawnIceCubePrison` (a pose has no `CharacterMotor`, so it takes the same builder a body's status
    ///     mark does) and released at 1.55 + 2.5 s, as the end of Frozen releases them. The old casing's thaw fragments
    ///     fly only in Play and are not filmed; it simply goes at the release.
    ///   * abszero: the LIVE PHASE ONLY. 0 s is the cutscene handing back, which is when `AbsoluteZero.OnActivate` runs;
    ///     nothing before it is drawn (that is the cutscene, unchanged and not filmed here). Three victims at 5, 9 and 12 m
    ///     are cased at 0 s and released at 2.5 s.
    ///
    /// Request: lines in `Temp/cheska-kit-film.request`: `ping` (answered at once), `&lt;coldfeet|frostbite|abszero|all&gt; &lt;tag&gt;`,
    /// or `floorboard &lt;tag&gt;`: the floor MATERIAL BOARD (c3), the three slab variants side by side at Cold Feet's size,
    /// fully formed, from three angles as the camera swings 40 degrees round them so the gloss and the depth move.
    /// Board frames: `cheskakit_floorboard_&lt;tag&gt;_frames/`, three columns (A, B, C) by three rows (low, three-quarter, above).
    /// Frames: `Logs/paete-ability-film/cheskakit_&lt;ability&gt;_&lt;tag&gt;_frames/` with `times.txt`, then
    /// `done_cheskakit_&lt;tag&gt;.txt` (with draw calls, triangles and materials, old and new) or `failed_cheskakit_&lt;tag&gt;.txt`.
    /// </summary>
    [InitializeOnLoad]
    internal static class CheskaKitFilmPoll
    {
        private const string Request = "Temp/cheska-kit-film.request";
        private static double _next;
        static CheskaKitFilmPoll() { EditorApplication.update += Poll; }

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
                if (words[0] == "matchpath") { CheskaKitFilm.MatchPath(words.Length > 1 ? words[1] : "c1"); continue; }
                if (words[0] == "ping") { Directory.CreateDirectory(CheskaKitFilm.OutDir); File.WriteAllText(Path.Combine(CheskaKitFilm.OutDir, "done_cheskakit_ping.txt"), DateTime.Now.ToString("HH:mm:ss")); continue; }
                CheskaKitFilm.CastHigh = words.Length > 3 && words[3] == "high";
                // `<case> <tag> <map> look`: the match's ambient occlusion on the film's camera. Without it this film's
                // camera has no `WorldOutline`, so no contact shade appears in it at all.
                CheskaKitFilm.GameLook = words.Length > 3 && words[3] == "look";
                CheskaKitFilm.Run(words[0], words.Length > 1 ? words[1] : "c1", words.Length > 2 ? words[2] : null);
            }
        }
    }

    internal static class CheskaKitFilm
    {
        internal const string OutDir = "Logs/paete-ability-film";
        private const string MapFolder = "Assets/TumbangPreso/Scenes/Maps/";
        private static string Map = MapFolder + "Eskinita.unity";
        private const int CellWidth = 640, CellHeight = 360, Rate = 60;
        private const float Base = 100f;
        private static readonly string[] Abilities = { "coldfeet", "frostbite", "abszero" };
        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private struct View
        {
            public string Name; public Vector3 Eye, Aim; public float Fov; public GameObject Hide; public Renderer[] HideToo; public Vector3 Sway;
            public View(string name, Vector3 eye, Vector3 aim, float fov, GameObject hide = null) { Name = name; Eye = eye; Aim = aim; Fov = fov; Hide = hide; HideToo = null; Sway = Vector3.zero; }
        }

        private sealed class Plan
        {
            public string Name;
            public readonly List<(float from, float to)> Segments = new List<(float, float)>();
            public Action<float> Pose = _ => { };
            public View[] Views;
            public readonly List<GameObject> Roots = new List<GameObject>();
            public readonly List<GameObject> Old = new List<GameObject>(), New = new List<GameObject>();
            public float[] CostAt = { 0f };
            // Called before each column is shot (0 OLD, 1 NEW): what differs between them that is not an object, a body's coat.
            public Action<int> Column;
        }

        // The body coat for one frozen body on the film's clock, as `CharacterVisual.ProcessFrost` drives it on a motor.
        // `age` counts from the freeze, `wait` is how long an Absolute Zero front takes to arrive, `release` is the freeze's end.
        // OLD is the shipped wash, tapering over the last `FrostThawTime`; NEW holds while the crystals stand and thaws with them.
        private static void BodyCoat(Renderer[] skin, MaterialPropertyBlock block, float age, float wait, float release, bool reworked)
        {
            float level;
            if (reworked)
            {
                float rise = Mathf.Clamp01((age - wait) / CharacterVisual.FrostRampIn);
                level = age < release ? rise : Mathf.Clamp01(1f - (age - release) / CharacterVisual.FrostRampOut);
            }
            else level = age < 0f || age >= release ? 0f : Mathf.Min(age / CharacterVisual.FrostRampIn, Mathf.Clamp01((release - age) / CharacterVisual.FrostThawTime));
            bool keep = StunCoat.SolidIce;
            StunCoat.SolidIce = reworked;
            foreach (var r in skin)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetFloat("_FrostAmount", level);
                StunCoat.WriteBody(block, StunElement.Ice);
                r.SetPropertyBlock(block);
            }
            StunCoat.SolidIce = keep;
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
                var holder = In(new GameObject("~CheskaKit-" + id), plan);
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

            // The real tsinelas, its sole's middle on the holder and its length along the holder's z (as `QuickReworkFilm`).
            public MeshFilter Shoe(Plan plan, out GameObject holder)
            {
                holder = In(new GameObject("~CheskaKit-tsinelas"), plan);
                var art = RosterBook.Load()?.SlipperArt(0);
                if (art == null || art.Model == null) { Report.AppendLine("  the roster's first tsinelas was not found"); return null; }
                var model = (GameObject)Object.Instantiate(art.Model, holder.transform);
                foreach (var collider in model.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
                ToonSkin.ApplySlipper(model, ToonSkin.PropOutlineWidth);
                var box = new Bounds(); bool any = false;
                foreach (var r in model.GetComponentsInChildren<Renderer>(true)) { if (any) box.Encapsulate(r.bounds); else { box = r.bounds; any = true; } }
                box.center = holder.transform.InverseTransformPoint(box.center);
                if (box.size.x > box.size.z) { model.transform.localRotation = Quaternion.Euler(0f, 90f, 0f) * model.transform.localRotation; box = new Bounds(new Vector3(-box.center.z, box.center.y, box.center.x), new Vector3(box.size.z, box.size.y, box.size.x)); }
                model.transform.localPosition += new Vector3(-box.center.x, -box.min.y, -box.center.z);
                return model.GetComponentInChildren<MeshFilter>();
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

        private static FieldInfo Field(Type type, string name) => type.GetField(name, Any) ?? throw new MissingFieldException(type.Name, name);

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

        /// <summary>Stand her on the highest place the map's routes know, not in the middle of the court.</summary>
        internal static bool CastHigh;
        internal static bool GameLook;
        private static float _filmTime;

        internal static void Run(string ability, string tag, string map = null)
        {
            // A third word names another map (`abszerolotus c7 Payo`); the look measures that map as a match does.
            Map = MapFolder + (string.IsNullOrEmpty(map) ? "Eskinita" : map) + ".unity";
            Directory.CreateDirectory(OutDir);
            tag = new string((tag ?? "c1").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray()).ToLowerInvariant();
            if (tag.Length == 0) tag = "c1";
            string donePath = Path.Combine(OutDir, "done_cheskakit_" + tag + ".txt"), failedPath = Path.Combine(OutDir, "failed_cheskakit_" + tag + ".txt");
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
            WorldLookPresentation gameLook = null;
            bool keepFeet = CheskaColdFeetLook.Enabled, keepCrust = CheskaFrostbiteCoating.Crust, keepFrozen = CheskaFrozenLook.Enabled, keepZero = CheskaAbsoluteZeroLook.Enabled;
            try
            {
                stage.Scene = EditorSceneManager.OpenPreviewScene(Map);
                if (!stage.Scene.IsValid()) throw new InvalidOperationException("No map to film on.");
                stage.Physics = stage.Scene.GetPhysicsScene();
                // Every ground query of the game's looks asks this scene, so the film proves the game's own ground rules.
                VfxShapes.GroundScene = stage.Physics;
                lit = Unsupported.SetOverrideLightingSettings(stage.Scene);
                Shader.SetGlobalFloat("_CharacterSmoothShade", ToonSkin.CharacterSmoothShade);
                Shader.SetGlobalFloat("_FxStudyBloom", 0f);
                report.AppendLine("map: " + Map + (lit ? ", lit by its own lighting settings" : ", its lighting could not be put in force") + "; no bloom; " + Rate + " pictures a second; columns OLD | NEW");

                var camGo = new GameObject("~CheskaKitCamera");
                SceneManager.MoveGameObjectToScene(camGo, stage.Scene);
                var cam = camGo.AddComponent<Camera>();
                cam.scene = stage.Scene; cam.enabled = false; cam.allowHDR = false;
                cam.nearClipPlane = 0.03f; cam.farClipPlane = 400f;
                cam.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.55f, 0.70f, 0.88f);
                var plain = new RenderTexture(CellWidth, CellHeight, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                var cell = new Texture2D(CellWidth, CellHeight, TextureFormat.RGB24, false);
                owned.Add(plain); owned.Add(cell); owned.Add(camGo);
                if (GameLook)
                {
                    // As the map builders' review cameras: the look's preview owns this camera and draws its occlusion.
                    var outline = camGo.AddComponent<WorldOutline>();
                    outline.PrototypeEnabled = true;
                    // Awake does not run on an added component outside Play, and with no camera the pass never asks for its texture.
                    Field(typeof(WorldOutline), "_camera").SetValue(outline, cam);
                    camGo.AddComponent<WorldLookCamera>();
                    var lookRoot = new GameObject("~CheskaKitLook");
                    SceneManager.MoveGameObjectToScene(lookRoot, stage.Scene);
                    owned.Add(lookRoot);
                    Light sun = null;
                    foreach (var root in stage.Scene.GetRootGameObjects())
                        foreach (var l in root.GetComponentsInChildren<Light>(true))
                            if (sun == null && l.type == LightType.Directional) sun = l;
                    // A preview scene does not carry the map's name, and the look finds its row by that name.
                    var row = WorldLookProfile.Current.Find(Path.GetFileNameWithoutExtension(Map));
                    string rowName = row != null ? row.Map : null;
                    if (row != null) row.Map = stage.Scene.name;
                    try { gameLook = WorldLookPresentation.InstallPreview(lookRoot.transform, stage.Ground(0f, 2f).y, sun); }
                    finally { if (row != null) row.Map = rowName; }
                    if (gameLook != null && WorldLookPresentation.Current != gameLook) typeof(WorldLookPresentation).GetMethod("OnEnable", Any)?.Invoke(gameLook, null);
                    report.AppendLine("look current: " + (WorldLookPresentation.Current != null) + "; handles camera: " + WorldLookPresentation.HandlesCamera(cam));
                    report.AppendLine("game look: " + (gameLook != null ? "on" : "NOT FOUND") + "; tier " + TumbangPreso.Settings.GraphicsProfiles.Current
                        + "; lighting weight " + WorldCueProfile.LightingWeight + "; occlusion " + WorldLookProfile.Current.AmbientOcclusion);
                }
                _clockOpaque = new UnityEngine.Rendering.CommandBuffer { name = "film clock (opaque)" };
                _clockAlpha = new UnityEngine.Rendering.CommandBuffer { name = "film clock (transparent)" };
                cam.AddCommandBuffer(UnityEngine.Rendering.CameraEvent.BeforeForwardOpaque, _clockOpaque);
                cam.AddCommandBuffer(UnityEngine.Rendering.CameraEvent.BeforeForwardAlpha, _clockAlpha);

                Color[] Shoot(View v)
                {
                    bool hid = v.Hide != null && v.Hide.activeSelf;
                    if (hid) v.Hide.SetActive(false);
                    if (v.HideToo != null) foreach (var r in v.HideToo) if (r != null) r.enabled = false;
                    cam.fieldOfView = v.Fov;
                    var eye = v.Eye + v.Sway * Mathf.Sin(_filmTime * 1.6f);
                    var dir = v.Aim - eye;
                    var up = Mathf.Abs(dir.normalized.y) > 0.95f ? Vector3.forward : Vector3.up;
                    cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(dir, up));
                    cam.targetTexture = plain;
                    cam.Render();
                    var keep = RenderTexture.active; RenderTexture.active = plain;
                    cell.ReadPixels(new Rect(0, 0, CellWidth, CellHeight), 0, 0); cell.Apply();
                    RenderTexture.active = keep;
                    if (hid) v.Hide.SetActive(true);
                    if (v.HideToo != null) foreach (var r in v.HideToo) if (r != null) r.enabled = true;
                    return cell.GetPixels();
                }

                foreach (string name in wanted)
                {
                    if (name == "floorboard")
                    {
                        report.AppendLine("floorboard:");
                        try { FloorBoard(stage, tag, Shoot, report); }
                        catch (Exception failure) { report.AppendLine("  FAILED: " + failure.GetType().Name + ": " + failure.Message); Debug.LogError("[CheskaKitFilm] floorboard: " + failure); }
                        continue;
                    }
                    var plan = new Plan { Name = name };
                    report.AppendLine(name + ":");
                    try
                    {
                        Build(stage, plan);
                        // The cost of each look, measured on what is actually drawn at a moment when all of it is up.
                        // (Every moment here is before any release, which latches once and would spoil the film after it.)
                        foreach (float at in plan.CostAt)
                        {
                            plan.Pose(at);
                            report.AppendLine("  cost at " + at.ToString("0.00") + " s, before batching: OLD " + Cost(plan.Old) + "; NEW " + Cost(plan.New));
                        }

                        string frames = Path.Combine(OutDir, "cheskakit_" + name + "_" + tag + "_frames");
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
                                _filmTime = t;
                                plan.Pose(t);
                                for (int column = 0; column < 2; column++)
                                {
                                    var hide = column == 0 ? plan.New : plan.Old;
                                    var was = hide.Where(g => g != null && g.activeSelf).ToList();
                                    foreach (var g in was) g.SetActive(false);
                                    plan.Column?.Invoke(column);
                                    for (int v = 0; v < rows; v++)
                                    {
                                        SetTime(t);
                                        // Row 0 at the top of the picture: textures count rows from the bottom.
                                        sheet.SetPixels(column * CellWidth, (rows - 1 - v) * CellHeight, CellWidth, CellHeight, Shoot(plan.Views[v]));
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
                        Debug.LogError("[CheskaKitFilm] " + name + ": " + failure);
                    }
                    finally
                    {
                        CheskaColdFeetLook.Enabled = keepFeet; CheskaFrostbiteCoating.Crust = keepCrust;
                        CheskaFrozenLook.Enabled = keepFrozen; CheskaAbsoluteZeroLook.Enabled = keepZero;
                        foreach (var go in plan.Roots) if (go != null) Object.DestroyImmediate(go);
                    }
                }
                File.WriteAllText(donePath, report.ToString());
            }
            catch (Exception failure)
            {
                File.WriteAllText(failedPath, report + "\n" + failure);
                Debug.LogError("[CheskaKitFilm] " + failure);
            }
            finally
            {
                CheskaColdFeetLook.Enabled = keepFeet; CheskaFrostbiteCoating.Crust = keepCrust;
                CheskaFrozenLook.Enabled = keepFrozen; CheskaAbsoluteZeroLook.Enabled = keepZero;
                VfxShapes.GroundScene = null;
                _clockOpaque?.Release(); _clockAlpha?.Release(); _clockOpaque = _clockAlpha = null;
                if (gameLook != null) Object.DestroyImmediate(gameLook.gameObject);
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
                    else File.AppendAllText(File.Exists(donePath) ? donePath : failedPath, "the owner's open scene was marked changed by a bake (nothing of the film's is left in it)\n");
                }
            }
        }

        // Draw calls as renderers times their sub-meshes, triangles as their meshes hold, distinct materials: only what is
        // active and enabled at that moment.
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
                    // The look's own pieces only: the people and the shoe model are the same in both columns.
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
            Vector3 fwd = Vector3.forward, right = Vector3.right, up = Vector3.up;
            switch (p.Name)
            {
                case "coldfeet": BuildColdFeet(s, p, fwd, right, up); return;
                // The spot on this map where the most of the disc crosses a change of height: slopes, steps and platform edges.
                case "coldfeetedge": BuildColdFeet(s, p, fwd, right, up, EdgeSpot(s)); return;
                // The whole 7.65 s with no cut, to judge continuity. `coldfeetbare` hides the bodies, so a frame difference is the flake alone.
                case "coldfeetuncut": BuildColdFeet(s, p, fwd, right, up, null, false, true); return;
                case "coldfeetbare": BuildColdFeet(s, p, fwd, right, up, null, false, true, true); return;
                // A Frozen body standing on it, the formed look beside the casing.
                case "coldfeetfrozen": BuildColdFeet(s, p, fwd, right, up, null, true); return;
                case "frostbite": BuildFrostbite(s, p, fwd, right, up); return;
                case "frozen": BuildFrozen(s, p, fwd, right, up); return;
                case "abszero": BuildAbsoluteZero(s, p, fwd, right, up); return;
                // The live look's two floors side by side: c3's slab (OLD) and the ice lotus's (NEW, `LotusFloor`).
                case "abszerolotus": BuildAbsoluteZero(s, p, fwd, right, up, true); return;
                // The same with one real Glacial Wall standing on the frozen floor, held at 1 s, for the colour comparison.
                case "abszerowall": BuildAbsoluteZero(s, p, fwd, right, up, true); AddWallToCompare(s, p, fwd, right, up); return;
                // The same at the same low angle with the camera drifting sideways through the hold, to see the reflections glide.
                case "abszerosheen":
                    BuildAbsoluteZero(s, p, fwd, right, up, true); AddWallToCompare(s, p, fwd, right, up);
                    p.Segments.Clear(); p.Segments.Add((0.55f, 1.95f));
                    for (int i = 0; i < p.Views.Length; i++) p.Views[i].Sway = right * 0.9f;
                    return;
            }
            throw new ArgumentException("No such ability: " + p.Name + " (coldfeet, frostbite, abszero or all)");
        }

        // ---------------------------------------------------------------- COLD FEET

        // Rings of columns round each point of the court, scored by how many sit a step or a drop from the middle.
        private static Vector3 EdgeSpot(Stage s)
        {
            var found = new List<VfxShapes.Surface>();
            float Top(Vector3 from)
            {
                found.Clear();
                VfxShapes.WalkableSurfaces(from, 8f, found);
                float best = float.NaN;
                foreach (var f in found) if (float.IsNaN(best) || f.Y > best) best = f.Y;
                return best;
            }
            float court = s.Ground(0f, 0f).y, score = -1f;
            Vector3 spot = s.Ground(0f, 0f);
            for (float z = -14f; z <= 14f; z += 1f)
                for (float x = -12f; x <= 12f; x += 1f)
                {
                    float y = Top(new Vector3(x, court + 4f, z));
                    if (float.IsNaN(y)) continue;
                    float hits = 0f, none = 0f;
                    for (int k = 0; k < 12; k++)
                    {
                        float a = k * Mathf.PI / 6f;
                        float h = Top(new Vector3(x + Mathf.Cos(a) * 1.7f, y + 0.9f, z + Mathf.Sin(a) * 1.7f));
                        if (float.IsNaN(h)) { none++; continue; }
                        float d = Mathf.Abs(h - y);
                        if (d > 0.2f && d < 1.2f) hits++;
                    }
                    float sum = hits - none * 0.5f;
                    if (hits >= 3f && hits <= 8f && sum > score) { score = sum; spot = new Vector3(x, y, z); }
                }
            s.Report.AppendLine("  edge spot " + spot + ", score " + score);
            return spot;
        }

        private static void BuildColdFeet(Stage s, Plan p, Vector3 fwd, Vector3 right, Vector3 up, Vector3? at = null, bool frozen = false, bool uncut = false, bool bare = false)
        {
            Vector3 S = at.HasValue ? at.Value - fwd * 3.2f : s.Ground(0f, -4f);
            var her = s.Person(p, "cheska", S, 0f);
            Vector3 C = at ?? s.Ground(S.x, S.z + 3.2f);
            var victim = s.Person(p, "dante", C + right * 0.8f, 200f);
            FrostSurfacePresentation Field(bool reworked, List<GameObject> into)
            {
                var holder = s.In(new GameObject(reworked ? "IceSheetZone-new" : "IceSheetZone-old"), p);
                holder.transform.position = C;
                into.Add(holder);
                CheskaColdFeetLook.Enabled = reworked;
                return FrostSurfacePresentation.Build(holder.transform, CryoRules.ColdFeetRadius, CryoRules.ColdFeetSeconds);
            }
            var old = Field(false, p.Old);
            var neu = Field(true, p.New);
            CheskaFrozenLook casing = null;
            if (frozen)
            {
                CheskaFrozenLook.Enabled = true;
                var made = s.Capture(p, () => HeroHazards.SpawnIceCubePrison(victim.transform, StatusRules.FrozenSeconds));
                p.New.AddRange(made);
                casing = made.Select(g => g.GetComponentInChildren<CheskaFrozenLook>(true)).FirstOrDefault(l => l != null);
                p.Segments.Add((-0.15f, 1.8f));
            }
            s.Report.AppendLine("  field " + CryoRules.ColdFeetRadius + " m radius, " + CryoRules.ColdFeetSeconds + " s, aimed 3.2 m ahead of her; a victim stands 0.8 m off its middle. New rim bake: " + CheskaColdFeetLook.BakeReport);
            p.Pose = t =>
            {
                bool on = t >= 0f && t <= CryoRules.ColdFeetSeconds;
                foreach (var g in p.Old.Concat(p.New)) g.SetActive(on);
                if (!on) return;
                old.StepTo(t); neu.StepTo(t);
                casing?.StepTo(t);
            };
            p.CostAt = new[] { 1.0f };
            if (uncut) p.Segments.Add((-0.15f, 7.65f));
            else if (!frozen) { p.Segments.Add((-0.15f, 1.8f)); p.Segments.Add((6.0f, 7.65f)); }
            if (bare) { her.SetActive(false); victim.SetActive(false); }
            Vector3 eyeV = C + right * 0.8f + up * 1.1f + (S - C).normalized * 0.2f;
            p.Views = new[]
            {
                new View("caster's eyes", S - fwd * 0.3f + up * 1.25f, C + up * 0.1f, 65f, her),
                new View("victim's eyes", eyeV, S + up * 0.45f, 70f, victim),
                new View("side", C + right * 6.3f + up * 1.0f, C + up * 0.3f - right * 0.5f, 45f),
                new View("above", C + up * 9f - fwd * 0.3f, C, 42f),
            };
        }

        // ---------------------------------------------------------------- FROSTBITE

        // FROZEN alone, close on the victim: the casing and the body coat. The coat is `CharacterVisual`'s own level for this timeline
        // (in over `FrostRampIn`, down over the last `FrostThawTime` of the stun), written onto the pose because a pose has no motor.
        private static void BuildFrozen(Stage s, Plan p, Vector3 fwd, Vector3 right, Vector3 up)
        {
            Vector3 V = s.Ground(0f, 2f);
            var victim = s.Person(p, "dante", V, 180f);
            var skin = victim.GetComponentsInChildren<Renderer>(true);
            var block = new MaterialPropertyBlock();
            List<GameObject> Case(bool reworked) { CheskaFrozenLook.Enabled = reworked; return s.Capture(p, () => HeroHazards.SpawnIceCubePrison(victim.transform, StatusRules.FrozenSeconds)); }
            var oldCase = Case(false); var newCase = Case(true);
            p.Old.AddRange(oldCase); p.New.AddRange(newCase);
            var casing = newCase.Select(g => g.GetComponentInChildren<CheskaFrozenLook>(true)).FirstOrDefault(l => l != null);
            if (casing == null) throw new InvalidOperationException("The new casing was not built.");
            float release = StatusRules.FrozenSeconds, thaw = CharacterVisual.FrostThawTime, filmNow = 0f;
            s.Report.AppendLine("  frozen at 0 s, released at " + release + " s; coat ramps in over " + CharacterVisual.FrostRampIn + " s, thaws over the last " + thaw + " s; still pose, as a frozen body is");
            p.Pose = t =>
            {
                filmNow = t;
                foreach (var g in oldCase) g.SetActive(t >= 0f && t < release);
                bool on = t >= 0f && t < release + CheskaFrozenLook.CrackSeconds + CheskaFrozenLook.SinkSeconds + 0.1f;
                foreach (var g in newCase) g.SetActive(on);
                if (on) { if (t >= release) casing.BeginRelease(); casing.StepTo(t); }
            };
            p.Column = column => BodyCoat(skin, block, filmNow, 0f, release, column == 1);
            p.CostAt = new[] { 1.0f };
            p.Segments.Add((-0.35f, release + 0.75f));
            Vector3 chest = V + up * 0.8f;
            p.Views = new[]
            {
                new View("front", V - fwd * 2.6f + right * 0.5f + up * 1.2f, chest, 45f),
                new View("three-quarter back", V + fwd * 2.3f + right * 1.5f + up * 1.0f, chest - up * 0.2f, 45f),
                new View("above", V + up * 3.4f - fwd * 0.6f, V, 50f),
                new View("his own eyes, looking down", V - fwd * 0.18f + up * 1.15f, V - fwd * 1.1f, 70f, victim),
            };
        }

        private const float ThrowAt = 1.0f, HitAt = 1.55f, LandAt = 1.85f;

        private static void BuildFrostbite(Stage s, Plan p, Vector3 fwd, Vector3 right, Vector3 up)
        {
            Vector3 S = s.Ground(0f, -4f);
            var her = s.Person(p, "cheska", S, 0f);
            Vector3 V = s.Ground(S.x, S.z + 6f);
            var victim = s.Person(p, "dante", V, 180f);
            Vector3 hand = S + right * 0.55f + fwd * 0.45f + up * 0.85f, chest = V + up * 0.9f, rest = s.Ground(V.x + 0.25f, V.z - 0.55f);

            var oldCoat = Coat(s, p, false, out var oldShoe, out var oldBlock, out var oldR);
            var newCoat = Coat(s, p, true, out var newShoe, out var newBlock, out var newR);
            p.Old.Add(oldShoe); p.New.Add(newShoe);

            // The casings, from the game's own call on a posed body.
            List<GameObject> Case(bool reworked)
            {
                CheskaFrozenLook.Enabled = reworked;
                return s.Capture(p, () => HeroHazards.SpawnIceCubePrison(victim.transform, StatusRules.FrozenSeconds));
            }
            var oldCase = Case(false); var newCase = Case(true);
            p.Old.AddRange(oldCase); p.New.AddRange(newCase);
            var casing = newCase.Select(g => g.GetComponentInChildren<CheskaFrozenLook>(true)).FirstOrDefault(l => l != null);
            if (casing == null) throw new InvalidOperationException("The new casing was not built (its crystal bake may have failed).");
            s.Report.AppendLine("  loaded in her hand at 0 s, thrown at " + ThrowAt + " s, hits a victim 6 m away at " + HitAt + " s, Frozen " + StatusRules.FrozenSeconds + " s; casing bake: " + CheskaFrozenLook.BakeReport);
            s.Report.AppendLine("  coat strength is each look's own rule for this timeline: OLD smoothstep(age / 0.18) while held, 0 once thrown; NEW closes in "
                + CheskaFrostbiteCoating.GrowSeconds + " s, keeps its crust in flight, melts in " + CheskaFrostbiteCoating.MeltSeconds + " s from the hit");

            Vector3 Path(float t)
            {
                if (t < ThrowAt) return hand;
                if (t < HitAt) { float u = (t - ThrowAt) / (HitAt - ThrowAt); return Vector3.Lerp(hand, chest, u) + up * (4f * u * (1f - u) * 0.6f); }
                float w = Mathf.Clamp01((t - HitAt) / (LandAt - HitAt));
                return Vector3.Lerp(chest, rest, w) + up * (0.25f * Mathf.Sin(w * Mathf.PI));
            }
            Quaternion Facing(float t)
            {
                if (t < ThrowAt) return Quaternion.LookRotation(fwd);
                var d = Path(t + 0.01f) - Path(t);
                return d.sqrMagnitude > 1e-6f && t < HitAt ? Quaternion.LookRotation(d) : Quaternion.LookRotation(fwd) * Quaternion.Euler(0f, 35f * Mathf.Clamp01((t - HitAt) / 0.3f), 0f);
            }
            float release = HitAt + StatusRules.FrozenSeconds, filmNow = 0f;
            var skin = victim.GetComponentsInChildren<Renderer>(true);
            var coatBlock = new MaterialPropertyBlock();
            p.Column = column => BodyCoat(skin, coatBlock, filmNow - HitAt, 0f, StatusRules.FrozenSeconds, column == 1);
            p.Pose = t =>
            {
                filmNow = t;
                bool shoeOn = t >= 0f;
                oldShoe.SetActive(shoeOn); newShoe.SetActive(shoeOn);
                oldShoe.transform.SetPositionAndRotation(Path(Mathf.Max(0f, t)), Facing(Mathf.Max(0f, t)));
                newShoe.transform.SetPositionAndRotation(oldShoe.transform.position, oldShoe.transform.rotation);
                float oldStrength = t < 0f || t >= ThrowAt ? 0f : Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / .18f));
                float newStrength = t < 0f ? 0f : t < HitAt ? Mathf.Clamp01(t / CheskaFrostbiteCoating.GrowSeconds) : Mathf.Clamp01(1f - (t - HitAt) / CheskaFrostbiteCoating.MeltSeconds);
                oldBlock.SetFloat("_Strength", oldStrength); oldR.SetPropertyBlock(oldBlock); oldR.enabled = oldStrength > 0f;
                newBlock.SetFloat("_Strength", newStrength); newR.SetPropertyBlock(newBlock); newR.enabled = newStrength > 0f;
                foreach (var g in oldCase) g.SetActive(t >= HitAt && t < release);
                bool caseOn = t >= HitAt && t < release + CheskaFrozenLook.CrackSeconds + CheskaFrozenLook.SinkSeconds + 0.1f;
                foreach (var g in newCase) g.SetActive(caseOn);
                if (caseOn)
                {
                    // A segment never runs backwards, so the release is set once, at its moment, on the casing's own clock.
                    if (t >= release) casing.BeginRelease();
                    casing.StepTo(t - HitAt);
                }
            };
            // Cost twice: with the shoe in flight (only the new look draws a coat then), and with the casing up.
            p.CostAt = new[] { 0.6f, 2.5f };
            p.Segments.Add((-0.15f, release + 0.6f));
            Vector3 mid = S + fwd * 3f;
            p.Views = new[]
            {
                new View("caster's eyes", S - fwd * 0.3f + up * 1.25f, S + fwd * 4f + up * 0.6f, 70f, her),
                new View("victim's eyes", V - fwd * 0.18f + up * 1.1f, S + up * 0.55f, 70f, victim),
                new View("side", mid + right * 7f + up * 1.2f, mid + up * 0.7f, 50f),
                new View("above", mid + up * 10f - fwd * 0.4f, mid, 50f),
            };
        }

        // A tsinelas and its coat, assembled as `CheskaFrostbiteCoating.Ensure` assembles it (that needs a live kit).
        private static CheskaFrostbiteCoating Coat(Stage s, Plan p, bool reworked, out GameObject shoe, out MaterialPropertyBlock block, out MeshRenderer renderer)
        {
            var filter = s.Shoe(p, out shoe);
            if (filter == null) throw new InvalidOperationException("No tsinelas mesh to coat.");
            var go = new GameObject("FrostbiteSoleRime"); go.layer = filter.gameObject.layer;
            SceneManager.MoveGameObjectToScene(go, s.Scene);
            go.transform.SetParent(filter.transform, false);
            var fx = go.AddComponent<CheskaFrostbiteCoating>();
            var t = typeof(CheskaFrostbiteCoating);
            var copy = go.AddComponent<MeshFilter>();
            renderer = go.AddComponent<MeshRenderer>(); renderer.receiveShadows = false;
            // The coating's own choice of shader: the crust when `Crust` is on, else the shipped lines (q1 stays off).
            var shader = reworked ? Resources.Load<Shader>("Shaders/CheskaFrostbiteCrust") : CheskaPaintedFrostLook.Coat(Resources.Load<Shader>("Shaders/FrostbiteLoad"));
            if (shader == null) throw new InvalidOperationException("Frostbite shader is missing.");
            var material = new Material(shader) { name = "Frostbite surface rime" };
            block = new MaterialPropertyBlock();
            Field(t, "_target").SetValue(fx, filter); Field(t, "_source").SetValue(fx, filter.GetComponent<Renderer>());
            Field(t, "_copy").SetValue(fx, copy); Field(t, "_renderer").SetValue(fx, renderer);
            Field(t, "_material").SetValue(fx, material); Field(t, "_block").SetValue(fx, block);
            Field(t, "_crust").SetValue(fx, reworked);
            t.GetMethod("RefreshMesh", Any).Invoke(fx, null);
            s.Report.AppendLine("  " + (reworked ? "NEW" : "OLD") + " coat shader " + shader.name);
            return fx;
        }

        // ---------------------------------------------------------------- ABSOLUTE ZERO, live phase

        private static void BuildAbsoluteZero(Stage s, Plan p, Vector3 fwd, Vector3 right, Vector3 up, bool slabOld = false)
        {
            // ⚠️ THE BOX IS THIS MAP'S, MEASURED AS A MATCH MEASURES IT (`MatchInstaller.MeasureWalls` on its "Bounds" boxes). The
            // look reads `AIController.Playable*`, statics that the last match or probe in this editor left behind: c5c was
            // filmed on another map's box. It is built inside this call, so the statics are put back right after.
            float keepX0 = AIController.PlayableMinX, keepX1 = AIController.PlayableMaxX, keepZ0 = AIController.PlayableMinZ, keepZ1 = AIController.PlayableMaxZ;
            var boxes = new List<Bounds>();
            foreach (var root in s.Scene.GetRootGameObjects())
                foreach (var node in root.GetComponentsInChildren<Transform>(true))
                    if (node.name == "Bounds") boxes.AddRange(node.GetComponentsInChildren<BoxCollider>().Select(c => c.bounds));
            TumbangPreso.MatchInstaller.MeasureWalls(boxes, out float x0, out float x1, out float z0, out float z1, out float ceiling);
            AIController.PlayableMinX = x0; AIController.PlayableMaxX = x1; AIController.PlayableMinZ = z0; AIController.PlayableMaxZ = z1;
            float keepCeiling = AIController.PlayableCeilingY; AIController.PlayableCeilingY = ceiling;
            // The map's own standing points, as a match has them through `MapRoutes.Current`.
            var routes = s.Scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MapRoutes>(true)).FirstOrDefault();
            CheskaAbsoluteZeroLook.StandingOverride = routes != null ? routes.Nodes : new Vector3[0];
            CheskaIceFloorMap.ForgetBake();
            s.Report.AppendLine("  routes on this map: " + (routes != null ? routes.Nodes.Length + " standing points" : "none") + "; ceiling " + ceiling.ToString("0.0"));
            s.Report.AppendLine("  map bounds (" + boxes.Count + " boxes): x " + x0.ToString("0.0") + " to " + x1.ToString("0.0") + ", z " + z0.ToString("0.0") + " to " + z1.ToString("0.0"));
            try { BuildAbsoluteZeroOnBox(s, p, fwd, right, up, slabOld); }
            finally
            {
                AIController.PlayableMinX = keepX0; AIController.PlayableMaxX = keepX1; AIController.PlayableMinZ = keepZ0; AIController.PlayableMaxZ = keepZ1;
                AIController.PlayableCeilingY = keepCeiling; CheskaAbsoluteZeroLook.StandingOverride = null;
            }
        }

        private static void BuildAbsoluteZeroOnBox(Stage s, Plan p, Vector3 fwd, Vector3 right, Vector3 up, bool slabOld)
        {
            float bx0 = AIController.PlayableMinX, bx1 = AIController.PlayableMaxX, bz0 = AIController.PlayableMinZ, bz1 = AIController.PlayableMaxZ;
            float hx = (bx1 - bx0) * 0.5f, hz = (bz1 - bz0) * 0.5f, mx = (bx0 + bx1) * 0.5f, mz = (bz0 + bz1) * 0.5f;
            // By the game's ground rules, so a viaduct or a roof overhead is not the floor; on Eskinita these are where they were.
            // ⚠️ A SPOT WITH NO GROUND (Payo's blocks) IS NOT A SPOT: a failed probe gives minus infinity, and a camera there
            // renders nothing, so the row kept the previous row's picture. That was Payo's two identical rows, c7. Walk
            // out from the spot to the nearest ground, by the same rule.
            Vector3 On(float x, float z)
            {
                for (float r = 0f; r < 12f; r += 0.75f)
                    for (int k = 0; k < (r == 0f ? 1 : 12); k++)
                    {
                        float a = k * Mathf.PI / 6f, px = x + Mathf.Sin(a) * r, pz = z + Mathf.Cos(a) * r;
                        if (VfxShapes.TryGroundUnder(new Vector3(px, 0f, pz), 60f, 60f, out float y)) return new Vector3(px, y, pz);
                    }
                return new Vector3(x, 0f, z);
            }
            Vector3 S = On(mx, mz - 0.15f * hz);
            var her = s.Person(p, "cheska", S, 0f);
            if (CastHigh)
            {
                // The highest court or stage surface well inside the box.
                float best = float.MinValue;
                var column = new List<VfxShapes.Surface>();
                for (float x = bx0 + 5f; x < bx1 - 5f; x += 1.5f)
                    for (float z = bz0 + 5f; z < bz1 - 5f; z += 1.5f)
                    {
                        column.Clear();
                        VfxShapes.WalkableSurfaces(new Vector3(x, 40f, z), 80f, column);
                        foreach (var c in column) if (c.Court && c.Y > best) { best = c.Y; S = new Vector3(x, c.Y, z); }
                    }
                her.transform.position = S;
            }
            var victims = new[]
            {
                s.Person(p, "dante", On(S.x + 0.35f * hx, S.z + 0.31f * hz), 200f),
                s.Person(p, "zack", On(S.x - 0.58f * hx, S.z + 0.58f * hz), 150f),
                s.Person(p, "amihan", On(S.x + 0.46f * hx, S.z + 0.87f * hz), 200f),
            };
            bool keepLotus = CheskaAbsoluteZeroLook.LotusFloor;
            CheskaAbsoluteZeroLook slab = null;
            List<GameObject> oldMade;
            if (slabOld)
            {
                CheskaAbsoluteZeroLook.Enabled = true; CheskaAbsoluteZeroLook.LotusFloor = false;
                oldMade = s.Capture(p, () => slab = CheskaAbsoluteZeroLook.Play(S, renderOnly: true));
                CheskaAbsoluteZeroLook.LotusFloor = true;
            }
            else
            {
                CheskaAbsoluteZeroLook.Enabled = false;
                oldMade = s.Capture(p, () => FrostSurfacePresentation.Nova(S, 4.6f, renderOnly: true));
            }
            var nova = oldMade.Select(g => g.GetComponent<FrostSurfacePresentation>()).FirstOrDefault(n => n != null);
            if (nova != null) nova.enabled = false;
            CheskaAbsoluteZeroLook live = null;
            List<GameObject> newMade;
            try { newMade = s.Capture(p, () => live = CheskaAbsoluteZeroLook.Play(S, renderOnly: true)); }
            finally { CheskaAbsoluteZeroLook.LotusFloor = keepLotus; }
            if (live == null) throw new InvalidOperationException("The live look was not built.");
            p.Old.AddRange(oldMade); p.New.AddRange(newMade);
            var casings = new List<(CheskaFrozenLook look, float delay)>();
            foreach (var v in victims)
            {
                CheskaFrozenLook.Enabled = slabOld;
                var oldCase = s.Capture(p, () => HeroHazards.SpawnIceCubePrison(v.transform, StatusRules.FrozenSeconds));
                p.Old.AddRange(oldCase);
                CheskaFrozenLook.Enabled = true;
                var made = s.Capture(p, () => HeroHazards.SpawnIceCubePrison(v.transform, StatusRules.FrozenSeconds));
                p.New.AddRange(made);
                // Each casing waits for the front to reach its body, as `CheskaAbsoluteZeroLook.PendingDelay` makes it in a match.
                var flat = new Vector2(v.transform.position.x - S.x, v.transform.position.z - S.z);
                float delay = CheskaAbsoluteZeroLook.Arrival(flat.magnitude, live.Far);
                casings.AddRange(made.Select(g => g.GetComponentInChildren<CheskaFrozenLook>(true)).Where(l => l != null).Select(l => (l, delay)));
                if (slabOld) casings.AddRange(oldCase.Select(g => g.GetComponentInChildren<CheskaFrozenLook>(true)).Where(l => l != null).Select(l => (l, delay)));
            }
            s.Report.AppendLine("  LIVE PHASE ONLY: 0 s is the cutscene handing back (the cutscene is not filmed); victims at 5.0, 9.0 and 12.0 m, Frozen "
                + StatusRules.FrozenSeconds + " s from 0 s. OLD is the shipped nova (4.6 m) and shards; NEW the wave (front " + CheskaAbsoluteZeroLook.RunSeconds + " s to the far corner, " + live.Far.ToString("0.0") + " m), the slab it leaves, her burst and the casings, each raised as the front reaches it. "
                + CheskaAbsoluteZeroLook.BakeReport);
            s.Report.AppendLine("  floor: " + CheskaAbsoluteZeroLook.FrostReport + "; her feet at " + S.ToString("0.00") + ", victims at "
                + string.Join(" ", victims.Select(v => v.transform.position.ToString("0.0"))));
            float release = StatusRules.FrozenSeconds, filmNow = 0f;
            var bodies = victims.Select(v =>
            {
                var flat = new Vector2(v.transform.position.x - S.x, v.transform.position.z - S.z);
                return (skin: v.GetComponentsInChildren<Renderer>(true), wait: CheskaAbsoluteZeroLook.Arrival(flat.magnitude, live.Far));
            }).ToList();
            var coatBlock = new MaterialPropertyBlock();
            p.Column = column => { foreach (var b in bodies) BodyCoat(b.skin, coatBlock, filmNow, b.wait, release, column == 1); };
            p.Pose = t =>
            {
                filmNow = t;
                bool on = t >= 0f;
                foreach (var g in oldMade) g.SetActive(on && t <= (slab != null ? slab.LifeSeconds : 0.7f));
                foreach (var g in newMade) g.SetActive(on && t <= live.LifeSeconds);
                foreach (var g in p.Old.Except(oldMade)) g.SetActive(slabOld ? on && t < release + CheskaFrozenLook.CrackSeconds + CheskaFrozenLook.SinkSeconds + 0.1f : on && t < release);
                bool caseOn = on && t < release + CheskaFrozenLook.CrackSeconds + CheskaFrozenLook.SinkSeconds + 0.1f;
                foreach (var g in p.New.Except(newMade)) g.SetActive(caseOn);
                if (!on) return;
                if (nova != null) nova.StepTo(t);
                if (slab != null) slab.StepTo(t);
                live.StepTo(t);
                foreach (var (c, delay) in casings) { if (t >= release) c.BeginRelease(); c.StepTo(t - delay); }
            };
            p.CostAt = new[] { 0.3f, 1.5f };
            p.Segments.Add((-0.15f, 3.3f));
            Vector3 D = victims[0].transform.position, mid = S + fwd * 5f, centre = On(mx, mz);
            float wide = Mathf.Max(hx, hz);
            p.Views = new[]
            {
                new View("caster's eyes", S - fwd * 0.3f + up * 1.3f, S + fwd * 8f + up * 0.3f, 70f, her) { HideToo = live.LotusParts },
                new View("victim's eyes (5 m)", D + (S - D).normalized * 0.18f + up * 1.1f, S + up * 0.6f, 70f, victims[0]),
                // Inside the box, against its wall and well up, so no building is ever in the way, looking across the whole floor.
                new View("side", SideEye(s, bx1, mx, mz, centre.y + 2.2f + 0.28f * hx, centre), new Vector3(mx - 0.5f * hx, centre.y, mz), 78f),
                new View("above", centre + up * (wide * 1.9f), centre, 60f),
                // Down along one wall from a corner: where the floor meets the edge of the map.
                new View("edge", new Vector3(bx1 - 2.2f, On(bx1 - 2.2f, bz0 + 1.2f).y + 1.9f, bz0 + 1.2f), new Vector3(bx1 - 0.2f, centre.y, mz), 70f),
            };
        }

        // The camera nearest the box's side with a clear line to the middle of the floor: a building never blocks it.
        private static Vector3 SideEye(Stage s, float side, float mx, float mz, float y, Vector3 centre)
        {
            for (float x = side - 0.4f; x > mx + 1f; x -= 0.8f)
            {
                var eye = new Vector3(x, y, mz);
                if (!s.Physics.Raycast(eye, (centre + Vector3.up * 0.5f - eye).normalized, out _, (centre - eye).magnitude, ~0, QueryTriggerInteraction.Ignore)) return eye;
            }
            return new Vector3(mx + 1f, y, mz);
        }

        // ⚠️ THE MATCH'S PATH, NOT PLAY. `VfxShapes.GroundScene` is null here, so the look's ground rule asks the default physics
        // scene exactly as in a match. Outside Play there is no map in it to ask, so two boxes named as a court stand 5 km from
        // anything of the owner's for the length of this call: a floor and, on one half of it, a step 0.4 m up. It is the
        // ability's look call and the ground rule, not a played cast: no cutscene, no network, no statuses.
        internal static void MatchPath(string tag)
        {
            Directory.CreateDirectory(OutDir);
            string done = Path.Combine(OutDir, "done_cheskakit_" + tag + ".txt");
            var active = SceneManager.GetActiveScene(); bool wasDirty = active.isLoaded && active.isDirty;
            var text = new System.Text.StringBuilder();
            float keepX0 = AIController.PlayableMinX, keepX1 = AIController.PlayableMaxX, keepZ0 = AIController.PlayableMinZ, keepZ1 = AIController.PlayableMaxZ;
            var ground = new GameObject("Ground") { hideFlags = HideFlags.HideAndDontSave };
            CheskaAbsoluteZeroLook live = null;
            try
            {
                var at = new Vector3(5000f, 0f, 5000f);
                ground.transform.position = at;
                var floor = ground.AddComponent<BoxCollider>(); floor.center = new Vector3(0f, -0.5f, 0f); floor.size = new Vector3(60f, 1f, 60f);
                var step = ground.AddComponent<BoxCollider>(); step.center = new Vector3(0f, -0.1f, 15f); step.size = new Vector3(60f, 1f, 30f);
                Physics.SyncTransforms();
                AIController.PlayableMinX = 4990f; AIController.PlayableMaxX = 5010f; AIController.PlayableMinZ = 4988f; AIController.PlayableMaxZ = 5016f;
                VfxShapes.GroundScene = null;
                text.AppendLine("GroundScene null: " + (VfxShapes.GroundScene == null) + "; box x -10 to 10, z -12 to 16 from her; step 0.4 m high at z above 0");
                CheskaIceFloorMap.ForgetBake();
                live = CheskaAbsoluteZeroLook.Play(at + Vector3.up * 0.9f, renderOnly: false, ownEyes: true);
                text.AppendLine("floor: " + CheskaAbsoluteZeroLook.FrostReport);
                text.AppendLine("root y: " + live.transform.position.y.ToString("0.000") + "; spikes for her own eyes: " + live.LotusParts.Length);
                live.StepTo(0.3f); live.StepTo(1.5f); live.StepTo(2.9f);
                text.AppendLine("stepped through 0.3, 1.5, 2.9 s without an exception");
            }
            catch (Exception failure) { text.AppendLine("FAILED: " + failure); }
            finally
            {
                if (live != null) { typeof(CheskaAbsoluteZeroLook).GetMethod("OnDestroy", Any)?.Invoke(live, null); Object.DestroyImmediate(live.gameObject); }
                Object.DestroyImmediate(ground);
                Physics.SyncTransforms();
                AIController.PlayableMinX = keepX0; AIController.PlayableMaxX = keepX1; AIController.PlayableMinZ = keepZ0; AIController.PlayableMaxZ = keepZ1;
                if (active.isLoaded && active.isDirty && !wasDirty)
                    typeof(EditorSceneManager).GetMethod("ClearSceneDirtiness", Any)?.Invoke(null, new object[] { active });
                File.WriteAllText(done, text.ToString());
            }
        }

        // One real Glacial Wall on the frozen floor, formed, in the new column only, seen from one more camera; the plan is cut
        // to the hold, because the point is the two surfaces at one moment under one light.
        private static void AddWallToCompare(Stage s, Plan p, Vector3 fwd, Vector3 right, Vector3 up)
        {
            var pose = p.Pose;
            float x = (AIController.PlayableMinX + AIController.PlayableMaxX) * 0.5f, z = (AIController.PlayableMinZ + AIController.PlayableMaxZ) * 0.5f;
            VfxShapes.TryGroundUnder(new Vector3(x, 0f, z), 60f, 60f, out float y);
            var at = new Vector3(x - 2.5f, y, z + 1f);
            var holder = s.In(new GameObject("~CheskaKit-compareWall"), p);
            holder.transform.position = at;
            p.New.Add(holder);
            CheskaGlacialWallLook.Enabled = true;
            var wall = CheskaGlacialWallLook.Build(holder.transform, CryoRules.GlacialWallArcLength, CryoRules.GlacialWallArcRadius);
            p.Pose = t => { pose(t); holder.SetActive(true); CheskaGlacialWallLook.Pose(wall, 1.0f, 10f, 0f); };
            p.Segments.Clear(); p.Segments.Add((1.0f, 1.05f));
            var aim = at + fwd * CryoRules.GlacialWallArcRadius;
            p.Views = new[]
            {
                new View("compare", aim - fwd * 5.5f + right * 1.5f + up * 1.6f, aim + up * 0.2f, 42f),
                new View("compare from the side", aim + right * 5f + up * 1.3f - fwd * 0.8f, aim + up * 0.2f, 42f),
            };
        }

        // ---------------------------------------------------------------- the floor MATERIAL BOARD (c3)

        // The three slab variants (`CheskaFrostKit.SlabVariant` 0, 1, 2) as full Cold Feet fields, formed, cracked, with
        // their crowns, on the court in daylight. Each column is one variant; each row an angle; over 1.5 s the camera
        // swings 40 degrees round each slab, so the sheen, the highlight bands and the depth under the top can be judged
        // in motion and not from one still.
        private static void FloorBoard(Stage s, string tag, Func<View, Color[]> shoot, System.Text.StringBuilder report)
        {
            var plan = new Plan { Name = "floorboard" };
            try
            {
                float[] xs = { -5.6f, 0f, 5.6f };
                var centres = new Vector3[3];
                for (int k = 0; k < 3; k++)
                {
                    centres[k] = s.Ground(xs[k], 2f);
                    var holder = s.In(new GameObject("~CheskaKit-board-" + (char)('A' + k)), plan);
                    holder.transform.position = centres[k];
                    CheskaColdFeetLook.Build(holder.transform, CryoRules.ColdFeetRadius, k).Pose(2.0f, CryoRules.ColdFeetSeconds);
                }
                s.Person(plan, "dante", centres[1] + Vector3.right * 0.9f, 200f);
                report.AppendLine("  three slabs of " + CryoRules.ColdFeetRadius + " m, posed 2.0 s into their life; crown bake: " + CheskaColdFeetLook.BakeReport);
                string frames = Path.Combine(OutDir, "cheskakit_floorboard_" + tag + "_frames");
                if (Directory.Exists(frames)) foreach (string old in Directory.GetFiles(frames)) File.Delete(old);
                Directory.CreateDirectory(frames);
                var sheet = new Texture2D(CellWidth * 3, CellHeight * 3, TextureFormat.RGB24, false);
                int f = 0;
                for (; f <= Mathf.RoundToInt(1.5f * Rate); f++)
                {
                    float t = f / (float)Rate;
                    SetTime(2.0f + t);
                    float swing = Mathf.Lerp(-20f, 20f, t / 1.5f);
                    for (int k = 0; k < 3; k++)
                    {
                        var c = centres[k];
                        var rot = Quaternion.Euler(0f, 150f + swing, 0f);
                        var rows = new[]
                        {
                            new View("low", c + rot * new Vector3(0f, 0f, 4.6f) + Vector3.up * 0.9f, c + Vector3.up * 0.1f, 45f),
                            new View("three-quarter", c + rot * new Vector3(0f, 0f, 5.0f) + Vector3.up * 3.2f, c, 45f),
                            new View("above", c + rot * new Vector3(0f, 0f, 0.8f) + Vector3.up * 7.5f, c, 50f),
                        };
                        for (int r = 0; r < 3; r++) sheet.SetPixels(k * CellWidth, (2 - r) * CellHeight, CellWidth, CellHeight, shoot(rows[r]));
                    }
                    sheet.Apply();
                    File.WriteAllBytes(Path.Combine(frames, "f" + f.ToString("0000") + ".png"), sheet.EncodeToPNG());
                }
                Object.DestroyImmediate(sheet);
                File.WriteAllText(Path.Combine(frames, "times.txt"), "columns: A|B|C (CheskaFrostKit.SlabVariant 0, 1, 2); rows: low|three-quarter|above; camera swings -20 to +20 degrees over the frames");
                report.AppendLine("  " + f + " frames in " + frames.Replace('\\', '/') + "; columns A, B, C; rows low, three-quarter, above; cost of one: "
                    + Cost(new[] { plan.Roots[0] }));
            }
            finally { foreach (var go in plan.Roots) if (go != null) Object.DestroyImmediate(go); }
        }
    }
}
