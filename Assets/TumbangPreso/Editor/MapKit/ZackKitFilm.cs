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
    /// ZACK'S REWORKED BACKTRACK, FULL THROTTLE AND LIVE WIRE, OLD BESIDE NEW, ON THE COURT. Eskinita opened as a PREVIEW
    /// scene (the owner's open scene is not touched and nothing is saved), in the map's own daylight, no bloom, real hero
    /// models for scale, sixty pictures a second of look time. Every picture is four rows, one per view (his eyes, the
    /// target's eyes, side on, from above), and two columns: on the left each look with its `Painted` switch OFF (as the
    /// rework left it), on the right with it ON. Both are in the court at once and only one column is shown at a time.
    ///
    /// ⚠️ STAGED, AND SAID SO. No match runs and nothing is decided. Bodies are still poses moved along the rule's own
    /// numbers. Each look is driven through its own code:
    ///   * BACKTRACK: `ZackBacktrackLook.Stage` on both sides, stepped by `StagedStep`, his body placed at `VisualFeet`. The
    ///     drawn body, the lead hologram and the past moments need a match's pose history (`MatchPoseHistory`) and are in
    ///     NEITHER column; they are the same either way. The old line spark bursts are runtime objects that animate only in
    ///     Play and are not in the old column; the old streaks are posed for a third-person camera in all four views;
    ///   * FULL THROTTLE: `ZackThrottleLook.Stage` on both sides, stepped by `StagedStep`. The vignette and the fist crackle
    ///     need his camera rig and first-person arms and are in neither; the old end fizzle is a runtime object and is not in
    ///     the old column;
    ///   * LIVE WIRE: `ZackLiveWireLook.Build(..., renderOnly: true, painted)` on both sides, told its state by `Present` and
    ///     stepped by `StepTo`, exactly as `ZackLiveWire` does in a match. The reel highlight is drawn only in his own view,
    ///     as it is in a match. The old trip sparks are runtime objects and are not in the old column.
    ///
    /// Full Throttle has a fifth row, `ring close`: the ground circle from 2.5 m off his feet.
    ///
    /// `btmark`: BACKTRACK READY, the return-point mark (`ZackBacktrackEcho.Stage`, drawn by `ZackBacktrackMark`) while he runs
    /// and then stands. Only the NEW column has it: the old walking echo needs a match's pose history.
    ///
    /// Request: lines in `Temp/zack-kit-film.request`: `ping` (answered at once), or `<ability|all> <tag>`, where ability is
    /// `backtrack`, `btmark`, `throttle`, `wire` (lit, a trip, the start of dark), `wirerearm` (the end of dark, the crawl, the relight),
    /// `wirereel` (lit, aimed, reeled in), `wireover` (overcharged), or several joined by commas.
    /// Frames go to `Logs/paete-ability-film/zackkit_<ability>_<tag>_frames/` with `times.txt`, then
    /// `done_zackkit_<tag>.txt` (draw calls, triangles and materials, old and new, at their worst frame) or `failed_...`.
    /// </summary>
    [InitializeOnLoad]
    internal static class ZackKitFilmPoll
    {
        private const string Request = "Temp/zack-kit-film.request";
        private static double _next;
        static ZackKitFilmPoll() { EditorApplication.update += Poll; }

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
                if (words[0] == "ping")
                {
                    Directory.CreateDirectory(ZackKitFilm.OutDir);
                    File.WriteAllText(Path.Combine(ZackKitFilm.OutDir, "done_zackkit_ping.txt"), DateTime.Now.ToString("HH:mm:ss"));
                    continue;
                }
                ZackKitFilm.Run(words[0], words.Length > 1 ? words[1] : "r1");
            }
        }
    }

    internal static class ZackKitFilm
    {
        internal const string OutDir = "Logs/paete-ability-film";
        private const string Map = "Assets/TumbangPreso/Scenes/Maps/Eskinita.unity";
        private const int CellWidth = 480, CellHeight = 270, Rate = 60;
        private const float Step = 1f / Rate;
        private const float Base = 100f;
        internal static readonly string[] Abilities = { "backtrack", "btmark", "throttle", "wire", "wirerearm", "wirereel", "wireover" };
        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private const float Eye = 1.55f;

        private struct View
        {
            public string Name; public Vector3 Eye, Aim; public float Fov; public GameObject[] Hide;
            public View(string name, Vector3 eye, Vector3 aim, float fov, params GameObject[] hide) { Name = name; Eye = eye; Aim = aim; Fov = fov; Hide = hide; }
        }

        private sealed class Plan
        {
            public string Name;
            public float Seconds;
            // Poses both columns for the time, for the view (0 is his own eyes) seen from that eye.
            public Action<float, int, Vector3> Pose = (t, view, eye) => { };
            public Func<View[]> Views;
            // Told the film's time before the views are asked for (a staging whose views follow something other than a body).
            public Action<float> Clock = t => { };
            // What each column shows (bodies and effect) and what is counted (the effect alone).
            public GameObject Old, New, OldFx, NewFx;
            public readonly List<GameObject> Roots = new List<GameObject>();
            public readonly List<string> Notes = new List<string>();
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

            public GameObject Person(Plan plan, Transform under, string id, Vector3 at, float yaw)
            {
                var holder = In(new GameObject("~ZackKit-" + id), plan);
                if (under != null) holder.transform.SetParent(under, false);
                holder.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
                var art = RosterBook.Load()?.FindPersonArt(id);
                if (art == null || art.Model == null) { Report.AppendLine("  " + id + "'s model was not found"); return holder; }
                var model = (GameObject)Object.Instantiate(art.Model, holder.transform);
                model.transform.localRotation = Quaternion.Euler(0f, CharacterVisual.PersonModelYaw, 0f);
                model.transform.localScale = Vector3.one * CharacterVisual.PersonScale;
                ToonSkin.Apply(model, ToonSkin.PersonOutlineWidth, art.Palette);
                foreach (var animator in model.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
                return holder;
            }
        }

        private static UnityEngine.Rendering.CommandBuffer _clockOpaque, _clockAlpha;

        // As `QuickReworkFilm`: `Camera.Render` writes the editor's own `_Time`, so the film's goes in through command buffers.
        private static void SetTime(float t)
        {
            float T = Base + t;
            var time = new Vector4(T / 20f, T, T * 2f, T * 3f);
            Shader.SetGlobalVector("_Time", time);
            foreach (var cb in new[] { _clockOpaque, _clockAlpha })
            {
                if (cb == null) continue;
                cb.Clear();
                cb.SetGlobalVector("_Time", time);
                cb.SetGlobalVector("unity_DeltaTime", new Vector4(Step, Rate, Step, Rate));
            }
        }

        private struct Cost { public int Calls, Triangles, Materials; }

        // What a side's effect draws at this moment: every enabled mesh or line renderer under it, a draw per material, its
        // triangles (a line's are two a segment). An estimate (it counts what is switched on, not what the camera culls),
        // the same way for both sides. The bodies are not counted.
        private static Cost Count(GameObject root)
        {
            var cost = new Cost();
            if (root == null) return cost;
            var materials = new HashSet<Material>();
            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || r.forceRenderingOff || r is SkinnedMeshRenderer) continue;
                if (r is LineRenderer line) cost.Triangles += Mathf.Max(0, line.positionCount - 1) * 2;
                else if (r is MeshRenderer)
                {
                    var mesh = r.GetComponent<MeshFilter>()?.sharedMesh;
                    if (mesh != null) for (int s = 0; s < mesh.subMeshCount; s++) cost.Triangles += (int)(mesh.GetIndexCount(s) / 3);
                }
                else continue;
                cost.Calls += Mathf.Max(1, r.sharedMaterials.Length);
                foreach (var m in r.sharedMaterials) if (m != null) materials.Add(m);
            }
            cost.Materials = materials.Count;
            return cost;
        }

        internal static void Run(string ability, string tag)
        {
            Directory.CreateDirectory(OutDir);
            tag = new string((tag ?? "r1").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray()).ToLowerInvariant();
            if (tag.Length == 0) tag = "r1";
            string donePath = Path.Combine(OutDir, "done_zackkit_" + tag + ".txt"), failedPath = Path.Combine(OutDir, "failed_zackkit_" + tag + ".txt");
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
                report.AppendLine("map: " + Map + (lit ? ", lit by its own lighting settings" : ", its lighting could not be put in force")
                    + "; no bloom; " + Rate + " pictures a second; rows: his eyes, the target's eyes, side, above; columns: OLD (switch off) | NEW (switch on)");

                var camGo = new GameObject("~ZackKitCamera");
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
                    if (v.Hide != null) foreach (var h in v.Hide) if (h != null) h.SetActive(false);
                    cam.fieldOfView = v.Fov;
                    var dir = v.Aim - v.Eye;
                    var up = Mathf.Abs(dir.normalized.y) > 0.95f ? Vector3.forward : Vector3.up;
                    cam.transform.SetPositionAndRotation(v.Eye, Quaternion.LookRotation(dir, up));
                    cam.targetTexture = plain;
                    cam.Render();
                    var keep = RenderTexture.active; RenderTexture.active = plain;
                    cell.ReadPixels(new Rect(0, 0, CellWidth, CellHeight), 0, 0); cell.Apply();
                    RenderTexture.active = keep;
                    if (v.Hide != null) foreach (var h in v.Hide) if (h != null) h.SetActive(true);
                    return cell.GetPixels();
                }

                foreach (string name in wanted)
                {
                    var plan = new Plan { Name = name };
                    report.AppendLine(name + ":");
                    try
                    {
                        Build(stage, plan);
                        string frames = Path.Combine(OutDir, "zackkit_" + name + "_" + tag + "_frames");
                        if (Directory.Exists(frames)) foreach (string old in Directory.GetFiles(frames)) File.Delete(old);
                        Directory.CreateDirectory(frames);
                        plan.Pose(0f, 1, Vector3.zero);
                        var first = plan.Views();
                        int rows = first.Length;
                        var sheet = new Texture2D(CellWidth * 2, CellHeight * rows, TextureFormat.RGB24, false);
                        var times = new System.Text.StringBuilder();
                        times.AppendLine("rows: " + string.Join("|", first.Select(v => v.Name)) + "; columns: old|new");
                        Cost worstOld = default, worstNew = default;
                        int count = Mathf.RoundToInt(plan.Seconds * Rate) + 1;
                        for (int f = 0; f < count; f++)
                        {
                            float t = f * Step;
                            SetTime(t);
                            // The views follow the bodies, so the stage is posed once for the time before they are asked for.
                            plan.Clock(t);
                            plan.Pose(t, 1, Vector3.zero);
                            var views = plan.Views();
                            for (int side = 0; side < 2; side++)
                            {
                                plan.Old.SetActive(side == 0);
                                plan.New.SetActive(side == 1);
                                for (int v = 0; v < views.Length; v++)
                                {
                                    plan.Pose(t, v, views[v].Eye);
                                    if (v == 1)
                                    {
                                        var cost = Count(side == 0 ? plan.OldFx : plan.NewFx);
                                        if (side == 0) { if (cost.Calls > worstOld.Calls || cost.Triangles > worstOld.Triangles) worstOld = cost; }
                                        else if (cost.Calls > worstNew.Calls || cost.Triangles > worstNew.Triangles) worstNew = cost;
                                    }
                                    SetTime(t);
                                    sheet.SetPixels(side * CellWidth, (rows - 1 - v) * CellHeight, CellWidth, CellHeight, Shoot(views[v]));
                                }
                            }
                            plan.Old.SetActive(true);
                            plan.New.SetActive(true);
                            sheet.Apply();
                            File.WriteAllBytes(Path.Combine(frames, "f" + f.ToString("0000") + ".png"), sheet.EncodeToPNG());
                            times.AppendLine(t.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture));
                        }
                        Object.DestroyImmediate(sheet);
                        File.WriteAllText(Path.Combine(frames, "times.txt"), times.ToString());
                        report.AppendLine("  " + count + " frames in " + frames.Replace('\\', '/'));
                        report.AppendLine("  cost at its worst frame, OLD: " + worstOld.Calls + " draw calls, " + worstOld.Triangles + " triangles, " + worstOld.Materials + " materials");
                        report.AppendLine("  cost at its worst frame, NEW: " + worstNew.Calls + " draw calls, " + worstNew.Triangles + " triangles, " + worstNew.Materials + " materials");
                        foreach (var note in plan.Notes) report.AppendLine("  " + note);
                    }
                    catch (Exception failure)
                    {
                        report.AppendLine("  FAILED: " + failure.GetType().Name + ": " + failure.Message);
                        Debug.LogError("[ZackKitFilm] " + name + ": " + failure);
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
                Debug.LogError("[ZackKitFilm] " + failure);
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
                    string to = File.Exists(donePath) ? donePath : failedPath;
                    if (clear != null) { clear.Invoke(null, new object[] { active }); File.AppendAllText(to, "the owner's open scene had been marked changed by a spawn and was cleared\n"); }
                    else File.AppendAllText(to, "the owner's open scene was marked changed by a spawn (nothing of the film's is left in it)\n");
                }
            }
        }

        // ------------------------------------------------------------------ the stagings

        private static void Build(Stage s, Plan p)
        {
            p.Old = Root(s, p, "old"); p.New = Root(s, p, "new");
            p.OldFx = Root(s, p, "old-fx"); p.OldFx.transform.SetParent(p.Old.transform, false);
            p.NewFx = Root(s, p, "new-fx"); p.NewFx.transform.SetParent(p.New.transform, false);
            switch (p.Name)
            {
                case "backtrack": BuildBacktrack(s, p); break;
                case "btmark": BuildBacktrackMark(s, p); break;
                case "throttle": BuildThrottle(s, p); break;
                case "wire": BuildWire(s, p, WireShow.Trip); break;
                case "wirerearm": BuildWire(s, p, WireShow.Rearm); break;
                case "wirereel": BuildWire(s, p, WireShow.Reel); break;
                case "wireover": BuildWire(s, p, WireShow.Overcharged); break;
                default: throw new ArgumentException("no such ability: " + p.Name + " (" + string.Join(", ", Abilities) + ")");
            }
        }

        private static GameObject Root(Stage s, Plan p, string name)
        {
            var go = s.In(new GameObject("~ZackKit-" + name), p);
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            return go;
        }

        private static GameObject ModelOf(GameObject holder) => holder.transform.childCount > 0 ? holder.transform.GetChild(0).gameObject : holder;

        // ------------------------------------------------------------------ BACKTRACK

        private static void BuildBacktrack(Stage s, Plan p)
        {
            // He ran 3 s up the left side and across the top of the court, 18 m at 6 m/s, turning one corner; he presses at
            // the far end and is pulled back the way he came.
            Vector3 to = s.Ground(-4f, -6f), corner = s.Ground(-4f, 4f), from = s.Ground(4f, 4f);
            var path = new[] { to, Vector3.Lerp(to, corner, 0.5f), corner, Vector3.Lerp(corner, from, 0.5f), from };
            float yaw = 90f;
            var zackOld = s.Person(p, p.Old.transform, "zack", from, yaw);
            var zackNew = s.Person(p, p.New.transform, "zack", from, yaw);
            // The taya, off the route's first leg, watching it.
            var taya = s.Person(p, null, "dante", s.Ground(-7.5f, -0.5f), 60f);
            var oldLook = ZackBacktrackLook.Stage(p.OldFx.transform, ModelOf(zackOld), from, to, path, false);
            var newLook = ZackBacktrackLook.Stage(p.NewFx.transform, ModelOf(zackNew), from, to, path, true);
            const float press = 0.25f;
            p.Seconds = press + ZackBacktrackLook.PullSeconds + ZackBacktrackLook.CatchSeconds + 0.55f;
            p.Notes.Add("staged: pressed at " + press.ToString("0.00") + " s after an 18 m run with one corner (" + ZackRules.BacktrackSeconds
                + " s at 6 m/s); the rewind is the look's own " + ZackBacktrackLook.PullSeconds.ToString("0.00") + " s pull and "
                + ZackBacktrackLook.CatchSeconds.ToString("0.00") + " s catch");
            p.Notes.Add("in neither column: the drawn body's rim, the lead hologram and the past moments (they need a match's pose history; the switch does not change them)");
            p.Notes.Add("new column: " + newLook.PaintedEchoes + " echoes stood on the route (each two draws of one body baked at the cast)");
            p.Notes.Add("not in the old column: its line spark bursts at the let-go, the catch and each past moment (runtime objects that animate only in Play)");
            Vector3 Feet(ZackBacktrackLook look, float t) => t < press ? from : look.VisualFeet;
            Vector3 fwd = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            p.Pose = (t, view, eye) =>
            {
                foreach (var (look, body) in new[] { (oldLook, zackOld), (newLook, zackNew) })
                {
                    look.gameObject.SetActive(t >= press);
                    if (t >= press) look.StagedStep(t - press, view == 0, eye);
                    var at = Feet(look, t);
                    body.transform.position = new Vector3(at.x, s.Ground(at.x, at.z).y, at.z);
                }
            };
            p.Views = () =>
            {
                var at = zackNew.transform.position;
                Vector3 centre = s.Ground(0f, -1f) + Vector3.up * 0.9f;
                return new[]
                {
                    new View("his eyes", at + Vector3.up * Eye + fwd * 0.12f, at + Vector3.up * 1.1f + fwd * 6f, 70f, zackOld, zackNew),
                    new View("the taya's eyes", taya.transform.position + Vector3.up * 1.6f, s.Ground(-1f, 2f) + Vector3.up * 0.9f, 62f, taya),
                    new View("side", centre + new Vector3(2f, 1.8f, -14f), centre, 52f),
                    new View("above", centre + new Vector3(0f, 19f, -0.5f), centre, 52f),
                };
            };
        }

        // ------------------------------------------------------------------ BACKTRACK READY (the return-point mark)

        private static void BuildBacktrackMark(Stage s, Plan p)
        {
            // He runs a 4.5 m circle at 6 m/s (from before the film starts, so there are 3 s of route behind him), then stops
            // and stands: the spot runs after him 3 s behind, then walks up to where he stopped and goes when it is closer than
            // Backtrack's minimum. The mark is asked for exactly as the kit asks: the spot is where his feet were 3 s ago.
            Vector3 centre = s.Ground(0f, 0f);
            const float radius = 4.5f, speed = 6f, stopAt = 3.4f;
            float w = speed / radius;
            float Run(float t) => Mathf.Min(t, stopAt);
            Vector3 Lap(float t) { float a = w * Run(t); var at = centre + new Vector3(Mathf.Sin(a), 0f, -Mathf.Cos(a)) * radius; return s.Ground(at.x, at.z); }
            Quaternion Facing(float t) { float a = w * Run(t); return Quaternion.LookRotation(new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)), Vector3.up); }
            var zackOld = s.Person(p, p.Old.transform, "zack", Lap(0f), 0f);
            var zackNew = s.Person(p, p.New.transform, "zack", Lap(0f), 0f);
            var taya = s.Person(p, null, "dante", centre, 0f);
            var mark = ZackBacktrackEcho.Stage(p.NewFx.transform);
            p.Seconds = stopAt + ZackRules.BacktrackSeconds + 0.8f;
            p.Notes.Add("staged: a 6 m/s lap of a 4.5 m circle, stopping at " + stopAt.ToString("0.00") + " s and standing; Backtrack ready throughout; "
                + "the spot is his feet " + ZackRules.BacktrackSeconds + " s ago, shown while it is at least " + ZackRules.BacktrackMinDistance + " m from him");
            p.Notes.Add("old column: nothing (the walking echo it replaces needs a match's pose history)");
            Vector3 Spot(float t) => Lap(t - ZackRules.BacktrackSeconds);
            p.Pose = (t, view, eye) =>
            {
                foreach (var body in new[] { zackOld, zackNew }) body.transform.SetPositionAndRotation(Lap(t), Facing(t));
                var spot = Spot(t);
                var away = spot - Lap(t); away.y = 0f;
                mark.StagedStep(t, away.magnitude >= ZackRules.BacktrackMinDistance, spot, view == 0 ? eye : Lap(t) + Vector3.up * Eye, view == 0);
            };
            p.Views = () =>
            {
                var at = zackNew.transform.position;
                return new[]
                {
                    // His eyes, turned to look at the spot (he would glance back for it).
                    new View("his eyes", at + Vector3.up * Eye, Glance(at + Vector3.up * Eye, CurrentSpot()), 70f, zackOld, zackNew),
                    new View("the taya's eyes", taya.transform.position + Vector3.up * 1.6f, CurrentSpot() + Vector3.up * 0.4f, 62f, taya),
                    new View("side", centre + new Vector3(1.5f, 2.6f, -13f), centre + Vector3.up * 0.6f, 52f),
                    // The mark from 3 m, looking down at it as a player standing near it would.
                    new View("mark close", CurrentSpot() + new Vector3(1.6f, 1.6f, -2.2f), CurrentSpot() + Vector3.up * 0.25f, 50f),
                };
            };
            Vector3 CurrentSpot() => Spot(_filmNow);
            // He glances toward the spot, never more than 35 degrees down (a player does not stare at his own shoes).
            Vector3 Glance(Vector3 eye, Vector3 spot)
            {
                var flat = spot - eye; flat.y = 0f;
                if (flat.sqrMagnitude < 1e-4f) flat = zackNew.transform.forward;
                float down = Mathf.Min(Mathf.Atan2(eye.y - spot.y, flat.magnitude), 35f * Mathf.Deg2Rad);
                return eye + flat.normalized * 4f - Vector3.up * Mathf.Tan(down) * 4f;
            }
            p.Clock = t => _filmNow = t;
        }

        private static float _filmNow;

        // ------------------------------------------------------------------ FULL THROTTLE

        private static void BuildThrottle(Stage s, Plan p)
        {
            // He laps the taya at 7 m/s on a 4.5 m circle (he is always turning, so the clock ring that turns with his
            // facing and the strike left where he cast both show), casting a moment in and holding the lock to its end.
            Vector3 centre = s.Ground(0f, 0f);
            const float radius = 4.5f, speed = 7f, cast = 0.30f;
            float w = speed / radius;
            Vector3 Lap(float t) { float a = w * t; var at = centre + new Vector3(Mathf.Sin(a), 0f, -Mathf.Cos(a)) * radius; return s.Ground(at.x, at.z); }
            Quaternion Facing(float t) { float a = w * t; return Quaternion.LookRotation(new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)), Vector3.up); }
            var zackOld = s.Person(p, p.Old.transform, "zack", Lap(0f), 0f);
            var zackNew = s.Person(p, p.New.transform, "zack", Lap(0f), 0f);
            var taya = s.Person(p, null, "dante", centre, 0f);
            var oldLook = ZackThrottleLook.Stage(p.OldFx.transform, zackOld.transform, false);
            var newLook = ZackThrottleLook.Stage(p.NewFx.transform, zackNew.transform, true);
            oldLook.StagedPath = newLook.StagedPath = Lap;
            p.Seconds = cast + ZackRules.FullThrottleSeconds + 0.6f;
            p.Notes.Add("staged: a 7 m/s lap of a 4.5 m circle round the taya, cast at " + cast.ToString("0.00") + " s, the lock runs its "
                + ZackRules.FullThrottleSeconds + " s unbroken");
            p.Notes.Add("a fifth row, ring close: the ground circle from 2.5 m off his feet");
            p.Notes.Add("in neither column: the first-person vignette and the fist crackle (his camera rig and arms); not in the old column: its end fizzle (a runtime object)");
            p.Pose = (t, view, eye) =>
            {
                foreach (var body in new[] { zackOld, zackNew }) body.transform.SetPositionAndRotation(Lap(t), Facing(t));
                var toZack = zackNew.transform.position - taya.transform.position; toZack.y = 0f;
                if (toZack.sqrMagnitude > 1e-4f) taya.transform.rotation = Quaternion.LookRotation(toZack);
                oldLook.StagedStep(t, cast, view == 0);
                newLook.StagedStep(t, cast, view == 0);
            };
            p.Views = () =>
            {
                var at = zackNew.transform.position; var fwd = zackNew.transform.forward;
                return new[]
                {
                    new View("his eyes", at + Vector3.up * Eye + fwd * 0.12f, at + Vector3.up * 0.6f + fwd * 4f, 70f, zackOld, zackNew),
                    new View("the taya's eyes", taya.transform.position + Vector3.up * 1.6f, at + Vector3.up * 1.0f, 62f, taya),
                    new View("side", centre + new Vector3(1.5f, 2.6f, -13f), centre + Vector3.up * 0.9f, 52f),
                    new View("above", centre + new Vector3(0f, 16f, -0.5f), centre, 50f),
                    // The ground circle from 2.5 m off his feet, behind his shoulder, looking down at it.
                    new View("ring close", at - fwd * 1.8f + zackNew.transform.right * 1.6f + Vector3.up * 0.9f, at + Vector3.up * 0.05f, 55f),
                };
            };
        }

        // ------------------------------------------------------------------ LIVE WIRE

        private enum WireShow { Trip, Rearm, Reel, Overcharged }

        private static void BuildWire(Stage s, Plan p, WireShow show)
        {
            // A 5 m wire across the lane, strung by Zack from his feet; an attacker runs at it from 4 m out.
            Vector3 a = s.Ground(-2.5f, 0f), b = s.Ground(2.5f, 0f), middle = (a + b) * 0.5f;
            var zackAt = s.Ground(-3.4f, -0.9f);
            var zackOld = s.Person(p, p.Old.transform, "zack", zackAt, 70f);
            var zackNew = s.Person(p, p.New.transform, "zack", zackAt, 70f);
            Vector3 runFrom = s.Ground(0.6f, 4.5f), runTo = s.Ground(0.2f, -3.5f);
            var attacker = s.Person(p, null, "cheska", runFrom, 180f);
            var field = new WorldEffectSnapshot.Field
            {
                Type = WorldEffectSnapshot.Kind.LiveWire, EventId = 7, Owner = 0, Position = a, Forward = (b - a).normalized, Radius = Vector3.Distance(a, b),
                Duration = ZackRules.LiveWireLifeBound, Remaining = ZackRules.LiveWireLifeBound, SecondScale = show == WireShow.Overcharged ? 1f : 0f,
                Path = new[] { a, b },
            };
            var looks = new[] { ZackLiveWireLook.Build(p.OldFx.transform, field, true, false), ZackLiveWireLook.Build(p.NewFx.transform, field, true, true) };
            Vector3 hand = zackAt + Vector3.up * 1.0f + Quaternion.Euler(0f, 70f, 0f) * Vector3.forward * 0.35f;
            // The wire's own clock for each staging: when it was strung, when it tripped (if it does), when it is reeled.
            const float strung = 0.20f;
            float tripAt = -1f, reelAt = -1f, offset = 0f;
            const float runSpeed = 5f;
            float crossAt = Vector3.Distance(runFrom, new Vector3(runFrom.x, runFrom.y, 0f)) / runSpeed;
            switch (show)
            {
                case WireShow.Trip: tripAt = 1.40f; p.Seconds = 3.2f; break;
                // Picked up 3.2 s into its dark: the last 1.8 s of the crawl, the relight and a moment lit.
                case WireShow.Rearm: tripAt = 1.40f; offset = 4.6f; p.Seconds = 2.6f; break;
                case WireShow.Reel: reelAt = 1.20f; p.Seconds = 1.20f + TumbangPreso.Abilities.ZackLiveWire.ReelSeconds + 0.5f; break;
                default: p.Seconds = 2.2f; break;
            }
            bool runs = show == WireShow.Trip;
            float runStart = strung + tripAt - crossAt;
            p.Notes.Add("staged: a " + Vector3.Distance(a, b).ToString("0.0") + " m wire strung at " + strung.ToString("0.00") + " s"
                + (show == WireShow.Trip ? "; the attacker runs at it at " + runSpeed + " m/s and crosses at " + (strung + tripAt).ToString("0.00") + " s, it goes dark for "
                   + ZackRules.LiveWireDarkSeconds + " s" : "")
                + (show == WireShow.Rearm ? "; picked up " + (offset - tripAt).ToString("0.0") + " s into its dark: the crawl, the relight" : "")
                + (show == WireShow.Reel ? "; he looks at it (the highlight is drawn in his own view only) and reels it in at " + (strung + reelAt).ToString("0.00") + " s" : "")
                + (show == WireShow.Overcharged ? "; strung while Overclocked (doubled)" : ""));
            p.Notes.Add("not in the old column: its trip sparks (a runtime object, and a replay's wire draws none)");
            var tripped = new bool[2]; var reeled = new bool[2];
            p.Pose = (t, view, eye) =>
            {
                if (runs)
                {
                    float run = Mathf.Max(0f, t - runStart) * runSpeed;
                    var at = Vector3.MoveTowards(runFrom, runTo, run);
                    attacker.transform.position = new Vector3(at.x, s.Ground(at.x, at.z).y, at.z);
                }
                float age = Mathf.Max(0f, t - strung) + offset;
                for (int k = 0; k < 2; k++)
                {
                    var look = looks[k];
                    look.gameObject.SetActive(t >= strung);
                    if (t < strung) continue;
                    bool dark = tripAt >= 0f && age >= tripAt && age - tripAt < ZackRules.LiveWireDarkSeconds;
                    float rearm = dark ? Mathf.Clamp01((age - tripAt) / ZackRules.LiveWireDarkSeconds) : 1f;
                    if (tripAt >= 0f && !tripped[k] && age >= tripAt)
                    {
                        tripped[k] = true;
                        look.Present(false, 1f, show == WireShow.Overcharged, hand, false);
                        look.StepTo(tripAt);
                        look.Present(true, 0f, false, hand, false);
                        // (Picked up after the trip, there was no flash to see.)
                        if (offset <= tripAt) look.Discharge();
                    }
                    look.Present(dark, rearm, show == WireShow.Overcharged, hand, show == WireShow.Reel && view == 0 && (reelAt < 0f || age < reelAt));
                    if (reelAt >= 0f && !reeled[k] && age >= reelAt) { reeled[k] = true; look.StepTo(reelAt); look.Reel(); }
                    look.StepTo(age);
                }
            };
            p.Views = () =>
            {
                var along = (b - a).normalized;
                var zackEye = zackAt + Vector3.up * Eye;
                var attackerEye = attacker.transform.position + Vector3.up * 1.6f;
                var seen = middle + Vector3.up * ZackRules.LiveWireSagHeight;
                return new[]
                {
                    new View("his eyes", zackEye, seen + along * 0.6f, 62f, zackOld, zackNew),
                    new View("the attacker's eyes", attackerEye, seen, 62f, attacker),
                    new View("side", middle + new Vector3(0.4f, 1.1f, -6.5f), seen, 45f),
                    new View("above", middle + new Vector3(0f, 9f, -0.4f), middle, 45f),
                };
            };
        }
    }
}
