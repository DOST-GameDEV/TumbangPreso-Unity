using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TumbangPreso.Abilities;
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
    /// THE QUICK REWORK LOOKS, FILMED. Eight new looks sit behind switches that default to false, so nobody has seen one
    /// run. This films each of them on Eskinita opened as a PREVIEW scene (the owner's open scene is not touched and
    /// nothing is saved), in the map's own daylight, with no bloom (no game camera blooms), real hero models for scale,
    /// from three views at once.
    ///
    /// ⚠️ STAGED, AND SAID SO. No match runs. Every look is driven through its own code outside Play:
    ///   * a look whose public entry point needs only a place (Supernova, the four ultimates, the refusal puff, the frost
    ///     field) is called as the game calls it, with its switch turned on in memory for the call and turned back off;
    ///   * a look whose entry point needs a live kit or slipper (Skim, Frostbite, Closed Circuit) is built from its own
    ///     private builders by reflection, and only the few lines that read the kit are stood in for here, each one
    ///     written out below beside the line of the look it replaces;
    ///   * the Cinder Gate is the game's own gate (`SeanCinderGate.Restore`) with its age stepped by hand.
    /// Bodies are still poses (nothing animates outside Play). Shader time is the film's clock, written to `_Time` before
    /// every picture, so a shader that reads `_Time` plays at the film's rate; the report says whether that took.
    ///
    /// Request: lines in `Temp/quick-rework-film.request`: `ping` (answered at once), or `<look|all> <tag>`.
    /// Frames go to `Logs/paete-ability-film/quickrework_<look>_<tag>_frames/` (one picture per sixtieth of a second,
    /// three views side by side) with `times.txt`, then `done_quickrework_<tag>.txt` or `failed_quickrework_<tag>.txt`.
    /// </summary>
    [InitializeOnLoad]
    internal static class QuickReworkFilmPoll
    {
        private const string Request = "Temp/quick-rework-film.request";
        private static double _next;
        static QuickReworkFilmPoll() { EditorApplication.update += Poll; }

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
                if (words[0] == "ping") { Directory.CreateDirectory(QuickReworkFilm.OutDir); File.WriteAllText(Path.Combine(QuickReworkFilm.OutDir, "done_quickrework_ping.txt"), DateTime.Now.ToString("HH:mm:ss")); continue; }
                QuickReworkFilm.Run(words[0], words.Length > 1 ? words[1] : "q1");
            }
        }
    }

    internal static class QuickReworkFilm
    {
        internal const string OutDir = "Logs/paete-ability-film";
        private const string Map = "Assets/TumbangPreso/Scenes/Maps/Eskinita.unity";
        private const int CellWidth = 640, CellHeight = 360, Rate = 60;
        // The film's clock is written to `_Time` as this plus the look's own time, so `_Born` and `_Time` agree.
        private const float Base = 100f;

        internal static readonly string[] Looks =
        {
            "rafi_skim", "cheska_coldfeet", "cheska_frostbite", "dante_refusal", "zack_circuit",
            "sean_cinder", "sean_supernova", "ult_amihan", "ult_cheska", "ult_rafi", "ult_zack",
        };

        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private struct View
        {
            public string Name; public Vector3 Eye, Aim; public float Fov; public GameObject Hide;
            public View(string name, Vector3 eye, Vector3 aim, float fov, GameObject hide = null) { Name = name; Eye = eye; Aim = aim; Fov = fov; Hide = hide; }
        }

        private sealed class Plan
        {
            public string Name;
            public readonly List<(float from, float to)> Segments = new List<(float, float)>();
            public Action<float> Pose = _ => { };
            public View[] Views;
            public readonly List<GameObject> Roots = new List<GameObject>();
            public Action Release = () => { };
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

            // A person: a holder that faces `yaw`, and the roster's model under it as a match dresses it.
            public GameObject Person(Plan plan, string id, Vector3 at, float yaw, out GameObject model)
            {
                model = null;
                var holder = In(new GameObject("~QuickRework-" + id), plan);
                holder.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
                var art = RosterBook.Load()?.FindPersonArt(id);
                if (art == null || art.Model == null) { Report.AppendLine("  " + id + "'s model was not found"); return holder; }
                model = (GameObject)Object.Instantiate(art.Model, holder.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.Euler(0f, CharacterVisual.PersonModelYaw, 0f);
                model.transform.localScale = Vector3.one * CharacterVisual.PersonScale;
                ToonSkin.Apply(model, ToonSkin.PersonOutlineWidth, art.Palette);
                foreach (var animator in model.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
                return holder;
            }

            // The real tsinelas, its sole's middle on the holder and its length along the holder's z (as `DanteBoulderFilm`).
            public MeshFilter Shoe(Plan plan, Vector3 at, Quaternion facing)
            {
                var holder = In(new GameObject("~QuickRework-tsinelas"), plan);
                holder.transform.SetPositionAndRotation(at, facing);
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
                Report.AppendLine("  tsinelas " + box.size.z.ToString("0.000") + " m long, " + box.size.x.ToString("0.000") + " m wide");
                return model.GetComponentInChildren<MeshFilter>();
            }

            // Runs a look's own spawn call and moves whatever it made at the root of the owner's scene into this one.
            public List<GameObject> Capture(Plan plan, Action make)
            {
                var active = SceneManager.GetActiveScene();
                var before = new HashSet<GameObject>(active.isLoaded ? active.GetRootGameObjects() : new GameObject[0]);
                make();
                var made = new List<GameObject>();
                if (active.isLoaded)
                    foreach (var go in active.GetRootGameObjects())
                        if (!before.Contains(go)) { SceneManager.MoveGameObjectToScene(go, Scene); plan.Roots.Add(go); made.Add(go); }
                return made;
            }
        }

        private static FieldInfo Field(Type type, string name) => type.GetField(name, Any) ?? throw new MissingFieldException(type.Name, name);
        private static object Invoke(Type type, object on, string name, params object[] args)
            => (type.GetMethod(name, Any) ?? throw new MissingMethodException(type.Name, name)).Invoke(on, args);

        // ⚠️ `Camera.Render` writes the editor's own `_Time` as it sets the camera up, so a global written before it is
        // overwritten (the first film's check measured exactly that). The film's clock goes in through command buffers
        // that run inside the render, after that setup and before the opaque and the transparent passes.
        private static UnityEngine.Rendering.CommandBuffer _clockOpaque, _clockAlpha;

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

        private static void Rebirth(IEnumerable<GameObject> made, float bornAt)
        {
            var block = new MaterialPropertyBlock();
            foreach (var go in made)
                foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
                {
                    block.Clear(); r.GetPropertyBlock(block);
                    block.SetFloat("_Born", Base + bornAt);
                    r.SetPropertyBlock(block);
                }
        }

        private static void Show(IEnumerable<GameObject> made, bool on) { foreach (var go in made) if (go != null && go.activeSelf != on) go.SetActive(on); }

        internal static void Run(string look, string tag)
        {
            Directory.CreateDirectory(OutDir);
            tag = new string((tag ?? "q1").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray()).ToLowerInvariant();
            if (tag.Length == 0) tag = "q1";
            string donePath = Path.Combine(OutDir, "done_quickrework_" + tag + ".txt"), failedPath = Path.Combine(OutDir, "failed_quickrework_" + tag + ".txt");
            if (File.Exists(donePath)) File.Delete(donePath);
            if (File.Exists(failedPath)) File.Delete(failedPath);
            if (EditorApplication.isPlayingOrWillChangePlaymode) { File.WriteAllText(failedPath, "The editor is in Play mode."); return; }
            string[] wanted = look == "all" ? Looks : look.Split(',');

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
                report.AppendLine("map: " + Map + (lit ? ", lit by its own lighting settings" : ", its lighting could not be put in force") + "; no bloom; " + Rate + " pictures a second of look time");

                var camGo = new GameObject("~QuickReworkCamera");
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

                if (wanted.Contains("scout"))
                {
                    // One picture straight down, orthographic, 44 m square centred on the court's middle: for placing views.
                    var big = new RenderTexture(1320, 1320, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                    var pic = new Texture2D(1320, 1320, TextureFormat.RGB24, false);
                    owned.Add(big); owned.Add(pic);
                    cam.orthographic = true; cam.orthographicSize = 22f;
                    cam.transform.SetPositionAndRotation(new Vector3(0f, 80f, 0f), Quaternion.LookRotation(Vector3.down, Vector3.forward));
                    cam.targetTexture = big; cam.Render(); cam.orthographic = false;
                    RenderTexture.active = big; pic.ReadPixels(new Rect(0, 0, 1320, 1320), 0, 0); pic.Apply(); RenderTexture.active = null;
                    File.WriteAllBytes(Path.Combine(OutDir, "quickrework_scout_" + tag + ".png"), pic.EncodeToPNG());
                    report.AppendLine("scout: 1320 px across 44 m (x -22..22 left to right, z -22..22 bottom to top), 30 px a metre");
                    wanted = wanted.Where(w => w != "scout").ToArray();
                }

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
                        if (name == "zack_circuit") TimeCheck(plan, Shoot, report);
                        string frames = Path.Combine(OutDir, "quickrework_" + name + "_" + tag + "_frames");
                        if (Directory.Exists(frames)) foreach (string old in Directory.GetFiles(frames)) File.Delete(old);
                        Directory.CreateDirectory(frames);
                        var sheet = new Texture2D(CellWidth * plan.Views.Length, CellHeight, TextureFormat.RGB24, false);
                        var times = new System.Text.StringBuilder();
                        times.AppendLine(string.Join("|", plan.Views.Select(v => v.Name)));
                        int f = 0;
                        foreach (var (from, to) in plan.Segments)
                        {
                            int count = Mathf.RoundToInt((to - from) * Rate) + 1;
                            for (int i = 0; i < count; i++, f++)
                            {
                                float t = from + i / (float)Rate;
                                SetTime(t);
                                plan.Pose(t);
                                for (int v = 0; v < plan.Views.Length; v++)
                                {
                                    SetTime(t);
                                    sheet.SetPixels(v * CellWidth, 0, CellWidth, CellHeight, Shoot(plan.Views[v]));
                                }
                                sheet.Apply();
                                File.WriteAllBytes(Path.Combine(frames, "f" + f.ToString("0000") + ".png"), sheet.EncodeToPNG());
                                times.AppendLine(t.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture));
                            }
                        }
                        Object.DestroyImmediate(sheet);
                        File.WriteAllText(Path.Combine(frames, "times.txt"), times.ToString());
                        report.AppendLine("  " + f + " frames in " + frames.Replace('\\', '/') + "; views: " + string.Join(", ", plan.Views.Select(v => v.Name)));
                    }
                    catch (Exception failure)
                    {
                        report.AppendLine("  FAILED: " + failure.GetType().Name + ": " + failure.Message);
                        Debug.LogError("[QuickReworkFilm] " + name + ": " + failure);
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
                Debug.LogError("[QuickReworkFilm] " + failure);
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
                // Anything a look made at the root of the owner's scene was moved out at once; if that still left his
                // scene marked changed, the mark is taken off again (nothing of ours is in it any more).
                if (active.isLoaded && active.isDirty && !wasDirty)
                {
                    var clear = typeof(EditorSceneManager).GetMethod("ClearSceneDirtiness", Any);
                    if (clear != null) { clear.Invoke(null, new object[] { active }); File.AppendAllText(File.Exists(donePath) ? donePath : failedPath, "the owner's open scene had been marked changed by a spawn and was cleared\n"); }
                    else File.AppendAllText(File.Exists(donePath) ? donePath : failedPath, "the owner's open scene was marked changed by a spawn (nothing of the film's is left in it)\n");
                }
            }
        }

        // Is the film's clock what the shaders read? The arc flickers by `_Time`: the same moment twice must match, and a
        // later `_Time` must differ.
        private static void TimeCheck(Plan plan, Func<View, Color[]> shoot, System.Text.StringBuilder report)
        {
            plan.Pose(0.3f);
            SetTime(0.3f); var a = shoot(plan.Views[1]);
            SetTime(0.3f); var b = shoot(plan.Views[1]);
            SetTime(0.67f); var c = shoot(plan.Views[1]);
            double same = 0, later = 0;
            for (int i = 0; i < a.Length; i++)
            {
                same += Mathf.Abs(a[i].r - b[i].r) + Mathf.Abs(a[i].g - b[i].g) + Mathf.Abs(a[i].b - b[i].b);
                later += Mathf.Abs(a[i].r - c[i].r) + Mathf.Abs(a[i].g - c[i].g) + Mathf.Abs(a[i].b - c[i].b);
            }
            report.AppendLine("  shader clock check (arc held still, `_Time` written by the film): same moment twice differs by " + (same / a.Length).ToString("0.000000")
                + ", 0.37 s later differs by " + (later / a.Length).ToString("0.000000") + (later > same * 4 + 1e-6 ? ": the film's clock is what the shaders read" : ": THE FILM'S CLOCK DID NOT REACH `_Time`"));
        }

        private static void Build(Stage s, Plan p)
        {
            Vector3 fwd = Vector3.forward, right = Vector3.right, up = Vector3.up;
            // Eskinita's open plaza runs x -7..7, z -8..7 (scout picture), with alleys north and south along z.
            Vector3 S = s.Ground(0f, -4f);
            switch (p.Name)
            {
                case "rafi_skim": BuildSkim(s, p, S, fwd, right); return;
                case "cheska_frostbite": BuildFrostbite(s, p, S, fwd, right); return;
                case "cheska_coldfeet":
                {
                    s.Person(p, "cheska", S, 0f, out _);
                    Vector3 C = s.Ground(S.x, S.z + 3.2f);
                    s.Person(p, "dante", C + right * 0.7f, 200f, out _);
                    var holder = s.In(new GameObject("IceSheetZone"), p);
                    holder.transform.position = C;
                    bool keep = CheskaPaintedFrostLook.PaintedColdFeet;
                    CheskaPaintedFrostLook.PaintedColdFeet = true;
                    FrostSurfacePresentation fx;
                    try { fx = FrostSurfacePresentation.Build(holder.transform, CryoRules.ColdFeetRadius, CryoRules.ColdFeetSeconds); }
                    finally { CheskaPaintedFrostLook.PaintedColdFeet = keep; }
                    var shaders = holder.GetComponentsInChildren<Renderer>(true).Select(r => r.sharedMaterial != null ? r.sharedMaterial.shader.name : "none").Distinct();
                    s.Report.AppendLine("  field " + CryoRules.ColdFeetRadius + " m radius, " + CryoRules.ColdFeetSeconds + " s, 3.2 m in front of her, drawn with " + string.Join(", ", shaders) + "; filmed 0 to 1.8 s and 6.0 to 7.65 s (the middle is cut)");
                    p.Pose = t => { holder.SetActive(t >= 0f); fx.StepTo(Mathf.Max(0f, t)); };
                    p.Segments.Add((-0.15f, 1.8f)); p.Segments.Add((6.0f, 7.65f));
                    p.Views = new[]
                    {
                        new View("bystander three-quarter", C + right * 5.0f - fwd * 3.5f + up * 2.6f, C + up * 0.2f, 45f),
                        new View("side", C + right * 6.3f + up * 1.0f, C + up * 0.3f - right * 0.5f, 45f),
                        new View("from above", C + up * 9f - fwd * 0.3f, C, 42f),
                    };
                    return;
                }
                case "dante_refusal": BuildRefusal(s, p, S, fwd, right); return;
                case "zack_circuit": BuildCircuit(s, p, S, fwd, right); return;
                case "sean_cinder": BuildCinder(s, p, S, fwd, right); return;
                case "sean_supernova":
                {
                    Vector3 P = s.Ground(0f, 0f);
                    s.Person(p, "sean", P, 0f, out _);
                    s.Person(p, "dante", P + right * 3f, 270f, out _);
                    s.Person(p, "zack", s.Ground(P.x - 5.2f, P.z + 1f), 90f, out _);
                    s.Person(p, "cheska", s.Ground(P.x + 1f, P.z + 6.8f), 180f, out _);
                    List<GameObject> made;
                    bool keep = SeanSupernovaLook.Enabled;
                    SeanSupernovaLook.Enabled = true;
                    try { made = s.Capture(p, () => SeanSupernovaLook.Spawn(P, 5.4f, 5.0f)); }
                    finally { SeanSupernovaLook.Enabled = keep; }
                    var look = made.Select(g => g.GetComponent<SeanSupernovaLook>()).FirstOrDefault(l => l != null);
                    if (look == null) throw new InvalidOperationException("SeanSupernovaLook.Spawn made nothing (its shader may be missing).");
                    s.Report.AppendLine("  landing at the game's call: radius 5.4 m, life 5.0 s; bystanders at 3 m (inside), 5.2 m (on the ring) and 7 m (outside)");
                    var age = Field(typeof(SeanSupernovaLook), "_age");
                    p.Pose = t =>
                    {
                        look.gameObject.SetActive(t >= 0f && t <= 5.6f);
                        age.SetValue(look, Mathf.Max(0f, t));
                        Invoke(typeof(SeanSupernovaLook), look, "Push");
                    };
                    p.Segments.Add((-0.15f, 5.7f));
                    p.Views = new[]
                    {
                        new View("bystander three-quarter (south alley)", P - fwd * 12f + right * 3.5f + up * 7f, P + up * 0.5f, 55f),
                        new View("side, low (north alley)", P + fwd * 15f + up * 1.6f, P + up * 0.8f, 45f),
                        new View("from above", P + up * 18f, P, 50f),
                    };
                    return;
                }
                case "ult_amihan":
                {
                    Vector3 A = s.Ground(0f, -7f);
                    s.Person(p, "amihan", A, 0f, out _);
                    s.Person(p, "zack", s.Ground(A.x + 2f, A.z + 6f), 180f, out _);
                    float range = AmihanRules.StormSurgeRangeForCourt(AIController.PlayableMinX, AIController.PlayableMaxX, AIController.PlayableMinZ, AIController.PlayableMaxZ);
                    var made = s.Capture(p, () => UltimateLiveLook.Amihan(A, fwd, range, AmihanRules.StormSurgeAroundRadius));
                    Rebirth(made, 0f);
                    s.Report.AppendLine("  Airburst at the game's call: range " + range.ToString("0.0") + " m (the default court's diagonal), ring " + AmihanRules.StormSurgeAroundRadius + " m; " + made.Count + " pieces");
                    p.Pose = t => Show(made, t >= 0f);
                    p.Segments.Add((-0.15f, 2.6f));
                    p.Views = new[]
                    {
                        new View("bystander three-quarter (south alley)", A - fwd * 5f + right * 3f + up * 6f, A + fwd * 8f, 60f),
                        new View("side", A + fwd * 6f + right * 6.3f + up * 2f, A + fwd * 6f - right * 2f, 72f),
                        new View("from above", A + fwd * 8f + up * 32f, A + fwd * 8f, 62f),
                    };
                    return;
                }
                case "ult_cheska":
                {
                    S = s.Ground(0f, -2f);
                    s.Person(p, "cheska", S, 0f, out _);
                    s.Person(p, "dante", s.Ground(S.x + 3f, S.z + 4f), 200f, out _);
                    var made = s.Capture(p, () => UltimateLiveLook.Cheska(S));
                    Rebirth(made, 0f);
                    s.Report.AppendLine("  Absolute Zero at the game's call (map front 26 m, rime wall 4.6 m wide, 2.4 m tall); " + made.Count + " pieces");
                    p.Pose = t => Show(made, t >= 0f);
                    p.Segments.Add((-0.15f, 3.4f));
                    p.Views = new[]
                    {
                        new View("bystander three-quarter (south alley)", S - fwd * 11f + right * 3f + up * 6f, S + up * 1f, 55f),
                        new View("side", S + right * 6.3f + up * 1.5f, S + up * 1.2f - right * 1f, 62f),
                        new View("from above", S + up * 36f, S, 70f),
                    };
                    return;
                }
                case "ult_rafi":
                {
                    Vector3 R = s.Ground(0f, -8f);
                    const float distance = 15f;
                    float duration = RafiRules.BahaDuration(distance);
                    s.Person(p, "rafi", R, 0f, out _);
                    s.Person(p, "zack", s.Ground(R.x + 1f, R.z + 8f), 180f, out _);
                    var holder = s.In(new GameObject("~QuickRework-Baha"), p);
                    var made = new List<GameObject>(s.Capture(p, () => UltimateLiveLook.Rafi(holder.transform, R, fwd, RafiRules.BahaHalfWidth, distance, RafiRules.BahaWarning, RafiRules.BahaSpeed, duration, 0f)));
                    made.Add(holder);
                    Rebirth(new[] { holder }, 0f);
                    s.Report.AppendLine("  Baha at the game's call: lane half width " + RafiRules.BahaHalfWidth + " m, stop distance " + distance + " m (staged), duration " + duration.ToString("0.00") + " s");
                    p.Pose = t => Show(made, t >= 0f);
                    p.Segments.Add((-0.15f, duration + 0.3f));
                    p.Views = new[]
                    {
                        new View("bystander three-quarter (south alley)", R - fwd * 5f + right * 3f + up * 5f, R + fwd * 7f, 60f),
                        new View("side", R + fwd * 7.5f + right * 6.3f + up * 2f, R + fwd * 7.5f - right * 1f, 75f),
                        new View("from above", R + fwd * 7.5f + up * 24f, R + fwd * 7.5f, 62f),
                    };
                    return;
                }
                case "ult_zack":
                {
                    Vector3 Z = s.Ground(S.x, S.z + 3f);
                    s.Person(p, "zack", S, 0f, out _);
                    s.Person(p, "dante", Z + right * 2f, 270f, out _);
                    var made = s.Capture(p, () => UltimateLiveLook.Zack(Z, 4.5f));
                    Rebirth(made, 0f);
                    s.Report.AppendLine("  Overclock at the game's call: strike 3 m ahead, ring 4.5 m; " + made.Count + " pieces");
                    p.Pose = t => Show(made, t >= 0f);
                    p.Segments.Add((-0.15f, 1.35f));
                    p.Views = new[]
                    {
                        new View("bystander three-quarter", Z - fwd * 7.5f + right * 3.5f + up * 3.5f, Z + up * 1.5f, 60f),
                        new View("side", Z + right * 6.3f + up * 1.5f, Z + up * 2.5f - right * 1f, 70f),
                        new View("from above", Z + up * 14f, Z, 50f),
                    };
                    return;
                }
            }
            throw new ArgumentException("No such look: " + p.Name);
        }

        // ---------------------------------------------------------------- a held slipper: Skim and Frostbite

        // ⚠️ Held out at arm's length beside his right hand, clear of his body: at the hip the 0.43 m tsinelas sat inside
        // his torso and the first film could not see the coat. Nothing holds it (a still pose).
        private static Vector3 Hip(Vector3 stand, Vector3 right) => stand + right * 0.75f + Vector3.up * 0.70f;

        private static View[] ShoeViews(Vector3 stand, Vector3 shoe, Vector3 fwd, Vector3 right) => new[]
        {
            new View("close three-quarter, low", shoe + fwd * 0.6f + right * 0.75f + Vector3.up * 0.10f, shoe, 40f),
            new View("close, side on", shoe + right * 0.95f + Vector3.up * 0.25f, shoe, 38f),
            new View("bystander, game distance", stand + fwd * 3.2f + right * 2.4f + Vector3.up * 1.4f, stand + Vector3.up * 0.7f, 45f),
        };

        private static void BuildSkim(Stage s, Plan p, Vector3 S, Vector3 fwd, Vector3 right)
        {
            s.Person(p, "rafi", S, 0f, out _);
            var shoeAt = Hip(S, right);
            var filter = s.Shoe(p, shoeAt, Quaternion.LookRotation(fwd));
            if (filter == null) throw new InvalidOperationException("No tsinelas mesh to coat.");
            // `RafiSkimPainted.Ensure` needs a live kit with Skim loaded; its body is this, by reflection.
            var go = new GameObject("SkimPaintedCoat"); go.layer = filter.gameObject.layer;
            SceneManager.MoveGameObjectToScene(go, s.Scene);
            go.transform.SetParent(filter.transform, false);
            var fx = go.AddComponent<RafiSkimPainted>();
            var t = typeof(RafiSkimPainted);
            Field(t, "_target").SetValue(fx, filter); Field(t, "_source").SetValue(fx, filter.sharedMesh);
            Field(t, "_sourceRenderer").SetValue(fx, filter.GetComponent<Renderer>()); Field(t, "_seed").SetValue(fx, 3.1f);
            Invoke(t, fx, "Build");
            var age = Field(t, "_age"); var dying = Field(t, "_dying");
            const float dieAt = 1.4f, dieSeconds = 0.35f;
            s.Report.AppendLine("  Skim loaded at 0 s, the load ends at " + dieAt + " s (its own " + dieSeconds + " s death follows); the coat is the look's own mesh and Sample, with no kit");
            p.Pose = time =>
            {
                float d = time < dieAt ? -1f : time - dieAt;
                bool on = time >= 0f && d <= dieSeconds;
                go.SetActive(on);
                if (!on) return;
                // Sample(0) with no kit starts the death at 0, whose tail is 0: identical to a live coat. Past the end
                // of the load the death time is set here and Sample draws it.
                age.SetValue(fx, time); dying.SetValue(fx, d);
                Invoke(t, fx, "Sample", 0f);
            };
            p.Segments.Add((-0.15f, 1.9f));
            p.Views = ShoeViews(S, shoeAt, fwd, right);
        }

        private static void BuildFrostbite(Stage s, Plan p, Vector3 S, Vector3 fwd, Vector3 right)
        {
            s.Person(p, "cheska", S, 0f, out _);
            var shoeAt = Hip(S, right);
            var filter = s.Shoe(p, shoeAt, Quaternion.LookRotation(fwd));
            if (filter == null) throw new InvalidOperationException("No tsinelas mesh to coat.");
            // `CheskaFrostbiteCoating.Ensure` needs a live kit with Frostbite loaded; its body is this, by reflection.
            var go = new GameObject("FrostbiteSoleRime"); go.layer = filter.gameObject.layer;
            SceneManager.MoveGameObjectToScene(go, s.Scene);
            go.transform.SetParent(filter.transform, false);
            var fx = go.AddComponent<CheskaFrostbiteCoating>();
            var t = typeof(CheskaFrostbiteCoating);
            var copy = go.AddComponent<MeshFilter>();
            var renderer = go.AddComponent<MeshRenderer>(); renderer.receiveShadows = false;
            bool keep = CheskaPaintedFrostLook.PaintedFrostbite;
            CheskaPaintedFrostLook.PaintedFrostbite = true;
            Shader shader;
            try { shader = CheskaPaintedFrostLook.Coat(Resources.Load<Shader>("Shaders/FrostbiteLoad")); }
            finally { CheskaPaintedFrostLook.PaintedFrostbite = keep; }
            if (shader == null) throw new InvalidOperationException("Frostbite shader is missing.");
            var material = new Material(shader) { name = "Frostbite surface rime" };
            var block = new MaterialPropertyBlock();
            Field(t, "_target").SetValue(fx, filter); Field(t, "_source").SetValue(fx, filter.GetComponent<Renderer>());
            Field(t, "_copy").SetValue(fx, copy); Field(t, "_renderer").SetValue(fx, renderer);
            Field(t, "_material").SetValue(fx, material); Field(t, "_block").SetValue(fx, block);
            Invoke(t, fx, "RefreshMesh");
            const float holdEnds = 1.6f;
            s.Report.AppendLine("  coat shader " + shader.name + "; loaded at 0 s, the 15 s hold is cut to " + holdEnds + " s, then its last 0.35 s; strength is the look's own sum (rise over 0.18 s, fade over the last 0.35 s)");
            p.Pose = time =>
            {
                // CheskaFrostbiteCoating.Sample: strength = SmoothStep(age / .18) * Clamp01(remaining / .35).
                float remaining = time < holdEnds ? 10f : 0.35f - (time - holdEnds);
                float strength = time < 0f ? 0f : Mathf.SmoothStep(0, 1, Mathf.Clamp01(time / .18f)) * Mathf.Clamp01(remaining / .35f);
                block.SetFloat("_Strength", strength); renderer.SetPropertyBlock(block);
                renderer.enabled = strength > 0f;
            };
            p.Segments.Add((-0.15f, 2.15f));
            p.Views = ShoeViews(S, shoeAt, fwd, right);
        }

        // ---------------------------------------------------------------- Unstoppable's refusal

        private static void BuildRefusal(Stage s, Plan p, Vector3 S, Vector3 fwd, Vector3 right)
        {
            s.Person(p, "dante", S, 0f, out _);
            s.Person(p, "zack", S + fwd * 4f, 180f, out var attacker);
            var hits = new (float at, StatusKind kind, Vector3 where)[]
            {
                (0.0f, StatusKind.Chilled, S + Vector3.up * 0.95f + fwd * 0.22f - right * 0.10f),
                (0.8f, StatusKind.Zapped, S + Vector3.up * 1.05f + fwd * 0.22f + right * 0.12f),
                (1.6f, StatusKind.Rooted, S + Vector3.up * 0.80f + fwd * 0.24f),
            };
            var puffType = typeof(CloudPuffs);
            var shatters = new List<(DanteStatusShatter fx, float born)>();
            int next = 0;
            CloudPuffs owner = null;
            CloudPuffField field = null;
            var nowField = Field(puffType, "_now");
            s.Capture(p, CloudPuffs.Prepare);
            owner = (CloudPuffs)Field(puffType, "_current").GetValue(null);
            if (owner == null) throw new InvalidOperationException("The match's cloud puff field did not make itself.");
            field = (CloudPuffField)Field(puffType, "_field").GetValue(owner);
            s.Report.AppendLine("  three refusals off his front plate, as the game calls them: Chilled at 0 s, Zapped at 0.8 s, Rooted at 1.6 s; the match's puff field and the rubble are stepped by the film's clock");
            p.Pose = t =>
            {
                while (next < hits.Length && t >= hits[next].at)
                {
                    var h = hits[next++];
                    nowField.SetValue(owner, h.at);
                    var made = s.Capture(p, () => DanteRefusalPuff.Spawn(h.where, fwd, h.kind));
                    foreach (var go in made) { var fx = go.GetComponent<DanteStatusShatter>(); if (fx != null) shatters.Add((fx, h.at)); }
                }
                nowField.SetValue(owner, Mathf.Max(0f, t));
                field.Hold(Mathf.Max(0f, t));
                foreach (var (fx, born) in shatters)
                {
                    if (fx == null) continue;
                    float age = t - born;
                    bool on = age >= 0f && age <= 1.15f;
                    fx.gameObject.SetActive(on);
                    if (on) fx.StepTo(age);
                }
            };
            p.Segments.Add((-0.15f, 2.9f));
            p.Views = new[]
            {
                new View("bystander three-quarter", S + fwd * 2.4f + right * 1.8f + Vector3.up * 1.4f, S + Vector3.up * 0.8f, 45f),
                new View("side", S + right * 3.2f + fwd * 0.3f + Vector3.up * 1.0f, S + Vector3.up * 0.7f + fwd * 0.3f, 40f),
                new View("attacker's eyes, 4 m", S + fwd * 4f + Vector3.up * 1.15f, S + Vector3.up * 0.8f, 60f, attacker),
            };
        }

        // ---------------------------------------------------------------- Closed Circuit

        private static void BuildCircuit(Stage s, Plan p, Vector3 S, Vector3 fwd, Vector3 right)
        {
            var caster = s.Person(p, "zack", S, 0f, out var zack);
            var first = s.Person(p, "dante", S + fwd * 5f, 180f, out _);
            var second = s.Person(p, "cheska", s.Ground(S.x - 3f, S.z + 3.5f), 140f, out _);
            var t = typeof(ZackCircuitArc);
            var go = s.In(new GameObject("Closed Circuit arc"), p);
            go.AddComponent<MeshFilter>().sharedMesh = (Mesh)Invoke(t, null, "SharedMesh");
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = (Material)Invoke(t, null, "SharedMaterial");
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
            var block = new MaterialPropertyBlock();
            // ZackCircuitArc.BindCastingHand/CastingHand: the left palm of his third-person model, else its fallback.
            Transform arm = null; Vector3 palm = Vector3.zero;
            var skin = zack != null ? zack.GetComponentInChildren<SkinnedMeshRenderer>() : null;
            if (skin != null && CharacterVisual.HandBone(skin, "left", out int hand, out palm)) arm = skin.bones[hand];
            s.Report.AppendLine("  two locks of 0.4 s (the lock's length) with the look's 0.16 s fade: on a target 5 m ahead at 0 s, then one 4.6 m off to his left at 1.0 s; strands from " + (arm != null ? "his left palm" : "the look's fallback point (no hand bone found)") + " to the target's middle (the look's 0.8 m fallback; there is no capsule here)");
            var locks = new (float at, GameObject target)[] { (0f, first), (1.0f, second) };
            const float lockSeconds = .4f, fadeSeconds = .16f;
            p.Pose = time =>
            {
                renderer.enabled = false;
                foreach (var (at, target) in locks)
                {
                    float local = time - at;
                    if (local < 0f || local > lockSeconds + fadeSeconds) continue;
                    // ZackCircuitArc.LateUpdate, with the kit's remaining lock read from the film's clock.
                    bool live = local <= lockSeconds;
                    float progress = Mathf.Clamp01(local / lockSeconds);
                    float fade = live ? fadeSeconds : fadeSeconds - (local - lockSeconds);
                    Vector3 from = arm != null ? arm.TransformPoint(palm) : caster.transform.position + Vector3.up * 1.05f + caster.transform.right * .14f;
                    Vector3 to = target.transform.position + Vector3.up * .8f, delta = to - from;
                    float length = delta.magnitude;
                    caster.transform.rotation = Quaternion.LookRotation(new Vector3(delta.x, 0f, delta.z), Vector3.up);
                    from = arm != null ? arm.TransformPoint(palm) : from; delta = to - from; length = delta.magnitude;
                    go.transform.SetPositionAndRotation(from, Quaternion.LookRotation(delta / length, Vector3.up));
                    go.transform.localScale = new Vector3(1, 1, length);
                    block.SetFloat("_Reach", live ? Mathf.Clamp01(.15f + progress * 1.45f) : 1);
                    block.SetFloat("_Erode", live ? 0 : 1 - Mathf.Clamp01(fade / fadeSeconds));
                    block.SetFloat("_Length", length);
                    renderer.SetPropertyBlock(block);
                    renderer.enabled = true;
                }
            };
            p.Segments.Add((-0.15f, 1.75f));
            Vector3 mid = S + fwd * 2.5f;
            p.Views = new[]
            {
                new View("caster's eyes (no first-person arms)", S + Vector3.up * 1.15f + fwd * 0.12f, S + fwd * 6f + Vector3.up * 0.8f - right * 1.2f, 75f, zack),
                new View("bystander three-quarter", S - fwd * 2.5f + right * 4f + Vector3.up * 2f, mid + Vector3.up * 0.8f - right * 1f, 55f),
                new View("side", mid + right * 6.3f + Vector3.up * 1.1f, mid + Vector3.up * 0.8f - right * 1.5f, 55f),
            };
        }

        // ---------------------------------------------------------------- Cinder Gate

        private static void BuildCinder(Stage s, Plan p, Vector3 S, Vector3 fwd, Vector3 right)
        {
            s.Person(p, "sean", S, 0f, out _);
            Vector3 G = s.Ground(S.x, S.z + 3f);
            var runner = s.Person(p, "zack", G + fwd * 2f, 180f, out _);
            var state = new WorldEffectSnapshot.Field
            {
                Type = WorldEffectSnapshot.Kind.CinderGate, EventId = 990001, Owner = 0, Position = G, Forward = fwd,
                Radius = SeanGateRules.HalfWidth, Duration = SeanGateRules.TotalSeconds, Remaining = SeanGateRules.TotalSeconds,
                Path = Array.Empty<Vector3>(),
            };
            SeanCinderGate gate = null;
            s.Capture(p, () => gate = SeanCinderGate.Restore(state, 0f));
            if (gate == null) throw new InvalidOperationException("SeanCinderGate.Restore refused the staged gate.");
            s.Capture(p, () => SeanCinderGateLook.Attach(gate));
            var look = gate.GetComponent<SeanCinderGateLook>();
            if (look == null) throw new InvalidOperationException("SeanCinderGateLook.Attach drew nothing (its shader may be missing).");
            var t = typeof(SeanCinderGate);
            var age = Field(t, "_age"); var st = Field(t, "_state");
            const float crossAt = 2.0f;
            s.Report.AppendLine("  the game's own gate (" + (SeanGateRules.HalfWidth * 2) + " m line, " + SeanGateRules.WarningSeconds + " s warning, " + SeanGateRules.TotalSeconds + " s life) 3 m in front of him; a rival crosses it at " + crossAt + " s and splits it");
            p.Pose = time =>
            {
                gate.gameObject.SetActive(time >= 0f);
                float a = Mathf.Clamp(time, 0f, SeanGateRules.TotalSeconds);
                age.SetValue(gate, a);
                var field = (WorldEffectSnapshot.Field)st.GetValue(gate);
                bool split = time >= crossAt;
                if (field.Split != split) { field.Split = split; field.FirstScale = split ? crossAt : 0f; field.SecondScale = split ? -1f : 0f; st.SetValue(gate, field); }
                // The rival runs through the line, crossing it at `crossAt`.
                runner.transform.position = G + fwd * Mathf.Lerp(2f, -2f, Mathf.InverseLerp(crossAt - 0.4f, crossAt + 0.4f, time)) + right * 0.4f;
                Invoke(typeof(SeanCinderGateLook), look, "LateUpdate");
            };
            p.Segments.Add((-0.15f, 3.6f));
            p.Views = new[]
            {
                new View("bystander three-quarter", G + right * 4.5f - fwd * 3.5f + Vector3.up * 2.4f, G + Vector3.up * 0.3f, 45f),
                new View("face on, low", G - fwd * 6f + right * 1.8f + Vector3.up * 1.2f, G + Vector3.up * 0.45f, 40f),
                new View("from above", G + Vector3.up * 7f - fwd * 0.3f, G, 45f),
            };
        }
    }
}
