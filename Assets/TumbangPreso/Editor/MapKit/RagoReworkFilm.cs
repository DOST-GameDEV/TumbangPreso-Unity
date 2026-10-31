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
    /// RAGO'S REWORKED LAVA RAM, MOLTEN THROW AND MAGMA FIST, OLD BESIDE NEW, ON THE COURT. Eskinita opened as a PREVIEW
    /// scene (the owner's open scene is not touched and nothing is saved), in the map's own daylight, no bloom, real hero
    /// models for scale, sixty pictures a second of look time. Every picture is four rows, one per view (his eyes, the
    /// target's eyes, side on, from above), and two columns: on the left the look with its switch OFF (as the rework left
    /// it), on the right with it ON. Both are in the court at once and only one is shown for each half of the picture.
    ///
    /// ⚠️ STAGED, AND SAID SO. No match runs and nothing is decided. Bodies are still poses moved along the rule's own sums
    /// (the RAM's held charge and slide, the launch, the fist's knock-up and push). Each look is driven through its own code:
    ///   * LAVA RAM new: `SeanRamFire.Stage`, stepped by `Advance`. LAVA RAM old: the c3 bench object posed with exactly the
    ///     arithmetic `SeanStokeFire` uses in a match (written out in `OldRam`, because that class draws only in Play);
    ///   * MOLTEN THROW: `SeanIgnitionVisual.Recorded(shoe, painted)` on the held shoe, and the approved s2 flight on both sides
    ///     (`SeanThrowFire.NewLook`, the same in both). The old Ignition blast at the can is a runtime object that does not
    ///     animate outside Play, so it is NOT in the old column; the report says so;
    ///   * MAGMA FIST: `SeanMagmaFist.Stage` with the look named, stepped by `Step`. The old Ignition blast at the punch is the
    ///     same runtime object and is taken out as it is made; the old ten spheres are in.
    /// The first-person arms are not drawn outside Play, so the caster's view of the throw holds a second shoe where his
    /// hand would be, as `SeanThrowFilm` does.
    ///
    /// `fistboard` is the BOARD for MAGMA FIST's fist of fire: the three `SeanFireFistLook.Variant`s cast side by side on the court
    /// (A mostly flame, B balanced, C a strong core, left to right), one column, his view and a three-quarter view.
    ///
    /// Request: lines in `Temp/rago-rework-film.request`: `ping` (answered at once), or `<ram|ramfull|throw|fist|fistboard|all> <tag>`.
    /// Frames go to `Logs/paete-ability-film/ragorework_<ability>_<tag>_frames/` with `times.txt`, then
    /// `done_ragorework_<tag>.txt` (draw calls, triangles and materials, old and new, at their worst frame) or `failed_...`.
    /// </summary>
    [InitializeOnLoad]
    internal static class RagoReworkFilmPoll
    {
        private const string Request = "Temp/rago-rework-film.request";
        private static double _next;
        static RagoReworkFilmPoll() { EditorApplication.update += Poll; }

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
                    Directory.CreateDirectory(RagoReworkFilm.OutDir);
                    File.WriteAllText(Path.Combine(RagoReworkFilm.OutDir, "done_ragorework_ping.txt"), DateTime.Now.ToString("HH:mm:ss"));
                    continue;
                }
                RagoReworkFilm.Run(words[0], words.Length > 1 ? words[1] : "r1");
            }
        }
    }

    internal static class RagoReworkFilm
    {
        internal const string OutDir = "Logs/paete-ability-film";
        private const string Map = "Assets/TumbangPreso/Scenes/Maps/Eskinita.unity";
        private const int CellWidth = 480, CellHeight = 270, Rate = 60;
        private const float Step = 1f / Rate;
        private const float Base = 100f;
        internal static readonly string[] Abilities = { "ram", "ramfull", "throw", "fist", "fistboard" };
        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private struct View
        {
            public string Name; public Vector3 Eye, Aim; public float Fov; public GameObject[] Hide;
            public View(string name, Vector3 eye, Vector3 aim, float fov, params GameObject[] hide) { Name = name; Eye = eye; Aim = aim; Fov = fov; Hide = hide; }
        }

        private sealed class Plan
        {
            public string Name;
            public float Seconds;
            public Action<float> Pose = _ => { };
            public Func<View[]> Views;
            public GameObject Old, New;
            // A board has neither: one column, every look in it at once.
            public GameObject Board;
            public readonly List<GameObject> Roots = new List<GameObject>();
            public readonly List<string> Notes = new List<string>();
            public Action Release = () => { };
            public Action Finish = () => { };
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
                var holder = In(new GameObject("~RagoRework-" + id), plan);
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

            // The real tsinelas under `parent`, its sole's middle on the holder and its length along the holder's z.
            public MeshFilter Shoe(Transform parent, out GameObject holder)
            {
                holder = new GameObject("~RagoRework-tsinelas");
                holder.transform.SetParent(parent, false);
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

            // Runs `make` and takes out anything it left at the root of the owner's open scene (a runtime object that cannot
            // animate outside Play). Returns how many were taken out.
            public int Contain(Action make)
            {
                var active = SceneManager.GetActiveScene();
                var before = new HashSet<GameObject>(active.isLoaded ? active.GetRootGameObjects() : new GameObject[0]);
                make();
                int taken = 0;
                if (active.isLoaded)
                    foreach (var go in active.GetRootGameObjects())
                        if (!before.Contains(go)) { Object.DestroyImmediate(go); taken++; }
                return taken;
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

        // What a side draws at this moment: every enabled mesh renderer under it, a draw per material, its mesh's triangles.
        // An estimate (it counts what is switched on, not what the camera culls), the same way for both sides.
        private static Cost Count(GameObject root)
        {
            var cost = new Cost();
            if (root == null) return cost;
            var materials = new HashSet<Material>();
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>())
            {
                if (!r.enabled || r.forceRenderingOff) continue;
                var mesh = r.GetComponent<MeshFilter>()?.sharedMesh;
                cost.Calls += Mathf.Max(1, r.sharedMaterials.Length);
                foreach (var m in r.sharedMaterials) if (m != null) materials.Add(m);
                if (mesh != null) for (int s = 0; s < mesh.subMeshCount; s++) cost.Triangles += (int)(mesh.GetIndexCount(s) / 3);
            }
            cost.Materials = materials.Count;
            return cost;
        }

        internal static void Run(string ability, string tag)
        {
            Directory.CreateDirectory(OutDir);
            tag = new string((tag ?? "r1").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray()).ToLowerInvariant();
            if (tag.Length == 0) tag = "r1";
            string donePath = Path.Combine(OutDir, "done_ragorework_" + tag + ".txt"), failedPath = Path.Combine(OutDir, "failed_ragorework_" + tag + ".txt");
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

                var camGo = new GameObject("~RagoReworkCamera");
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
                        string frames = Path.Combine(OutDir, "ragorework_" + name + "_" + tag + "_frames");
                        if (Directory.Exists(frames)) foreach (string old in Directory.GetFiles(frames)) File.Delete(old);
                        Directory.CreateDirectory(frames);
                        var first = plan.Views();
                        int rows = first.Length;
                        int cols = plan.Board != null ? 1 : 2;
                        var sheet = new Texture2D(CellWidth * cols, CellHeight * rows, TextureFormat.RGB24, false);
                        var times = new System.Text.StringBuilder();
                        times.AppendLine("rows: " + string.Join("|", first.Select(v => v.Name)) + (cols == 2 ? "; columns: old|new" : "; one column"));
                        Cost worstOld = default, worstNew = default;
                        int count = Mathf.RoundToInt(plan.Seconds * Rate) + 1;
                        for (int f = 0; f < count; f++)
                        {
                            float t = f * Step;
                            SetTime(t);
                            plan.Pose(t);
                            var views = plan.Views();
                            for (int side = 0; side < cols; side++)
                            {
                                if (plan.Old != null) plan.Old.SetActive(side == 0);
                                if (plan.New != null) plan.New.SetActive(side == 1);
                                var cost = Count(plan.Board != null ? plan.Board : side == 0 ? plan.Old : plan.New);
                                if (side == 0) { if (cost.Calls > worstOld.Calls || cost.Triangles > worstOld.Triangles) worstOld = cost; }
                                else if (cost.Calls > worstNew.Calls || cost.Triangles > worstNew.Triangles) worstNew = cost;
                                for (int v = 0; v < views.Length; v++)
                                {
                                    SetTime(t);
                                    sheet.SetPixels(side * CellWidth, (rows - 1 - v) * CellHeight, CellWidth, CellHeight, Shoot(views[v]));
                                }
                            }
                            if (plan.Old != null) plan.Old.SetActive(true);
                            if (plan.New != null) plan.New.SetActive(true);
                            sheet.Apply();
                            File.WriteAllBytes(Path.Combine(frames, "f" + f.ToString("0000") + ".png"), sheet.EncodeToPNG());
                            times.AppendLine(t.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture));
                        }
                        Object.DestroyImmediate(sheet);
                        File.WriteAllText(Path.Combine(frames, "times.txt"), times.ToString());
                        report.AppendLine("  " + count + " frames in " + frames.Replace('\\', '/'));
                        if (plan.Board != null) report.AppendLine("  cost at its worst frame, ALL THREE together (divide by 3 for one fist): " + worstOld.Calls + " draw calls, " + worstOld.Triangles + " triangles, " + worstOld.Materials + " materials");
                        else report.AppendLine("  cost at its worst frame, OLD: " + worstOld.Calls + " draw calls, " + worstOld.Triangles + " triangles, " + worstOld.Materials + " materials");
                        if (plan.Board == null) report.AppendLine("  cost at its worst frame, NEW: " + worstNew.Calls + " draw calls, " + worstNew.Triangles + " triangles, " + worstNew.Materials + " materials");
                        plan.Finish();
                        foreach (var note in plan.Notes) report.AppendLine("  " + note);
                    }
                    catch (Exception failure)
                    {
                        report.AppendLine("  FAILED: " + failure.GetType().Name + ": " + failure.Message);
                        Debug.LogError("[RagoReworkFilm] " + name + ": " + failure);
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
                Debug.LogError("[RagoReworkFilm] " + failure);
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
            switch (p.Name)
            {
                case "ram": BuildRam(s, p, hits: true); break;
                case "ramfull": BuildRam(s, p, hits: false); break;
                case "throw": BuildThrow(s, p); break;
                case "fist": BuildFist(s, p); break;
                case "fistboard": BuildFistBoard(s, p); break;
                default: throw new ArgumentException("no such ability: " + p.Name + " (ram, ramfull, throw, fist, fistboard)");
            }
        }

        private static GameObject Side(Stage s, Plan p, string name)
        {
            var go = s.In(new GameObject("~RagoRework-" + name), p);
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            return go;
        }

        private static Vector3 Head(GameObject body) => body.transform.position + Vector3.up * 1.62f;

        /// <summary>
        /// `SeanStokeFire` as a match runs it, line for line, minus its `Application.isPlaying` gate: the press starts its
        /// clock, the launch is told when it comes, and STOKE STEP's own 0.18 s brace and 0.37 s travel decide when the wake
        /// thinks he has planted. This is the rework's LAVA RAM as it plays now.
        /// </summary>
        private sealed class OldRam
        {
            private readonly FxStudySeanStoke _fx;
            private readonly GameObject _holder;
            private readonly Transform _body;
            private Vector3 _from, _dir;
            private float _age, _front, _movedAt, _stoppedAt = -1f;
            private bool _launched, _live;

            public OldRam(Transform parent, Transform body)
            {
                _body = body;
                _holder = new GameObject("SeanStokeFire");
                _holder.transform.SetParent(parent, false);
                _fx = new FxStudySeanStoke(_holder.transform, AmihanWhirlwind.DayLight);
                _holder.SetActive(false);
            }
            public void Press(Vector3 dir)
            {
                _dir = dir; _from = _body.position;
                _holder.transform.SetPositionAndRotation(_from, Quaternion.LookRotation(dir, Vector3.up));
                _age = 0f; _front = 0f; _movedAt = 0f; _stoppedAt = -1f; _launched = false; _live = true;
                _fx.Restart(); _holder.SetActive(true);
            }
            public void Launch() { _launched = true; _movedAt = _age; }
            public void Advance(float dt, Vector3 eye)
            {
                if (!_live) return;
                _age += dt;
                if (!_launched && _age > FxStudySeanStoke.Brace + 0.25f) { End(); return; }
                float since = _age - FxStudySeanStoke.Brace;
                if (_launched && since >= 0f)
                {
                    float now = Vector3.Dot(_body.position - _from, _dir);
                    if (now > _front + 0.004f) { _front = now; _movedAt = _age; }
                    else if (_stoppedAt < 0f && since > 0.06f && _age - _movedAt > 0.07f) _stoppedAt = _movedAt;
                }
                float stopped = since - FxStudySeanStoke.Travel;
                if (_stoppedAt >= 0f) stopped = Mathf.Max(stopped, _age - _stoppedAt);
                _fx.Pose(_age, eye, Mathf.Max(0f, _front), stopped);
                if (_age > FxStudySeanStoke.Life + 0.1f) End();
            }
            private void End() { _live = false; _holder.SetActive(false); }
            public void Release() => _fx.Release();
        }

        private static void BuildRam(Stage s, Plan p, bool hits)
        {
            Vector3 fwd = Vector3.forward, right = Vector3.right;
            Vector3 S = s.Ground(0f, -4f);
            var sean = s.Person(p, "sean", S, 0f);
            // The target: 5 m ahead on his line (hit), or off his line watching an 8 m charge go past (full).
            Vector3 T0 = hits ? s.Ground(0f, 1f) : s.Ground(2.6f, 0.5f);
            var target = s.Person(p, "dante", T0, hits ? 180f : 225f);
            p.Old = Side(s, p, "old"); p.New = Side(s, p, "new");
            var old = new OldRam(p.Old.transform, sean.transform);
            var fire = SeanRamFire.Stage(p.New.transform, sean.transform);
            p.Release = () => { old.Release(); if (fire != null) Object.DestroyImmediate(fire.gameObject); };

            const float press = 0.20f;
            float launch = press + SeanRules.RamMinBraceSeconds, hold = SeanRules.RamHoldSeconds;
            float stopAt = hits ? Mathf.Max(0f, (T0 - S).magnitude - (SeanRules.RamContactReach + 0.45f)) : SeanRules.RamTravel;
            float launchSpeed = SeanRules.RamLaunchSpeed, F = Balance.Friction;
            p.Seconds = 2.6f;
            p.Notes.Add("staged: brace at " + press.ToString("0.00") + " s, launch at " + launch.ToString("0.00") + " s, held " + SeanRules.RamSpeed
                + " m/s for " + hold.ToString("0.000") + " s then slid; " + (hits ? "stops dead " + stopAt.ToString("0.00") + " m on, target thrown " + SeanRules.RamLaunch + " m"
                : "the whole " + SeanRules.RamTravel + " m, nobody in the way"));
            p.Notes.Add("old = the c3 bench posed by `SeanStokeFire`'s own arithmetic; new = `SeanRamFire`");

            float Gone(float since)
            {
                if (since <= 0f) return 0f;
                if (since < hold) return SeanRules.RamSpeed * since;
                float u = Mathf.Min(since - hold, SeanRules.RamSpeed / F);
                return SeanRules.RamSpeed * hold + SeanRules.RamSpeed * u - 0.5f * F * u * u;
            }
            float contactAt = -1f;
            bool pressed = false, launched = false, contacted = false;
            float last = 0f;
            Vector3 SideEye() => S + fwd * 4f + right * 8f + Vector3.up * 2.0f;

            p.Pose = t =>
            {
                float dt = t - last; last = t;
                float d = Mathf.Min(Gone(t - launch), stopAt);
                sean.transform.position = S + fwd * d;
                if (hits && !contacted && d >= stopAt - 1e-4f && t > launch) { contacted = true; contactAt = t; }
                if (hits && contactAt >= 0f)
                {
                    float u = Mathf.Min(t - contactAt, launchSpeed / F);
                    target.transform.position = T0 + fwd * (launchSpeed * u - 0.5f * F * u * u);
                }
                if (!pressed && t >= press) { pressed = true; old.Press(fwd); fire.StagedBrace(fwd); dt = t - press; }
                if (!launched && t >= launch) { launched = true; old.Launch(); fire.StagedLaunch(); }
                if (contacted && contactAt == t) fire.StagedContact(target.transform.position);
                if (pressed) { old.Advance(Mathf.Max(0f, dt), SideEye()); fire.Advance(Mathf.Max(0f, dt), SideEye()); }
            };
            p.Views = () =>
            {
                Vector3 mid = S + fwd * 4f;
                Vector3 seanEye = Head(sean) + fwd * 0.2f, targetEye = Head(target) + target.transform.forward * 0.2f;
                return new[]
                {
                    new View("his eyes", seanEye, seanEye + fwd * 5f - Vector3.up * 0.6f, 70f, sean),
                    new View("target's eyes", targetEye, sean.transform.position + Vector3.up * 1.0f, 60f, target),
                    new View("side", SideEye(), mid + Vector3.up * 0.7f, 45f),
                    new View("above", mid + Vector3.up * 12f - fwd * 4f, mid, 50f),
                };
            };
        }

        private static void BuildThrow(Stage s, Plan p)
        {
            Vector3 fwd = Vector3.forward, right = Vector3.right;
            Vector3 S = s.Ground(0f, -4f);
            var sean = s.Person(p, "sean", S, 0f);
            // The can: the map's own if the scene holds one, else 7 m ahead of him.
            Vector3 can = s.Ground(0f, 3f);
            Lata found = null;
            foreach (var root in s.Scene.GetRootGameObjects()) { found = root.GetComponentInChildren<Lata>(true); if (found != null) break; }
            if (found != null) can = found.transform.position;
            p.Notes.Add(found != null ? "the can is the map's own, at " + can : "the map scene holds no can: the throw lands on the court where it would stand, 7 m out");
            var taya = s.Person(p, "dante", can + right * 1.6f + fwd * 0.4f, 200f);
            p.Old = Side(s, p, "old"); p.New = Side(s, p, "new");

            // Per side: the held shoe at his hip, a first-person stand-in in front of his eyes, and the approved s2 flight.
            var sides = new List<(GameObject shoe, GameObject eyeShoe, SeanIgnitionVisual held, SeanIgnitionVisual eyeHeld, FxStudySeanThrow flight)>();
            for (int k = 0; k < 2; k++)
            {
                var parent = (k == 0 ? p.Old : p.New).transform;
                var filter = s.Shoe(parent, out var shoe);
                var eyeFilter = s.Shoe(parent, out var eyeShoe);
                var held = filter != null ? SeanIgnitionVisual.Recorded(filter, k == 1) : null;
                var eyeHeld = eyeFilter != null ? SeanIgnitionVisual.Recorded(eyeFilter, k == 1) : null;
                var flightHolder = new GameObject("flight"); flightHolder.transform.SetParent(parent, false);
                sides.Add((shoe, eyeShoe, held, eyeHeld, SeanThrowFire.NewLook(flightHolder.transform)));
            }
            p.Release = () => { foreach (var x in sides) x.flight?.Release(); };
            p.Notes.Add("old held = the three tongues; new held = the s2 board's held fire; the flight and the burst are the approved s2 on BOTH sides");
            p.Notes.Add("NOT IN THE OLD COLUMN: the Ignition blast `SeanImpact.CanContact` laid at the can (a runtime object, it does not animate outside Play); the new look drops it");

            const float throwAt = 0.70f, speed = 18f;
            Vector3 hand = S + right * 0.40f + Vector3.up * 0.95f + fwd * 0.70f;
            Vector3 hit = can + Vector3.up * 0.25f;
            float flight = (hit - hand).magnitude / speed;
            p.Seconds = throwAt + flight + 1.2f;
            p.Notes.Add("staged: held to " + throwAt + " s, then flat at " + speed + " m/s (" + flight.ToString("0.00") + " s) into the can");
            bool landed = false;
            Vector3 SeanEye() => Head(sean) + fwd * 0.2f;
            p.Pose = t =>
            {
                Vector3 going = (hit - hand).normalized;
                Vector3 at = t < throwAt ? hand : Vector3.Lerp(hand, hit, Mathf.Clamp01((t - throwAt) / flight));
                bool flying = t >= throwAt && t < throwAt + flight;
                foreach (var x in sides)
                {
                    if (x.shoe != null)
                    {
                        x.shoe.transform.SetPositionAndRotation(at, t < throwAt ? Quaternion.Euler(0f, 0f, 70f) : Quaternion.LookRotation(going));
                        x.shoe.SetActive(t < throwAt + flight);
                    }
                    if (x.eyeShoe != null)
                    {
                        x.eyeShoe.transform.SetPositionAndRotation(SeanEye() + new Vector3(0.26f, -0.32f, 0.55f), Quaternion.Euler(10f, -20f, 0f));
                        x.eyeShoe.SetActive(t < throwAt);
                    }
                    if (x.held != null) { x.held.StepTo(t); x.held.gameObject.SetActive(t < throwAt); }
                    if (x.eyeHeld != null) x.eyeHeld.StepTo(t);
                    if (x.flight != null)
                    {
                        // Posed from the first frame, so its pieces are hidden until the throw rather than left as built.
                        if (flying) x.flight.Record(at, t);
                        if (t >= throwAt && !flying && !landed) { x.flight.Record(hit, t); x.flight.Impact(s.Ground(hit.x, hit.z), t); }
                        x.flight.Pose(t, SeanEye(), flying ? at : (t < throwAt ? hand : hit), flying, going);
                    }
                }
                if (t >= throwAt + flight) landed = true;
            };
            p.Views = () =>
            {
                Vector3 mid = Vector3.Lerp(S, can, 0.5f);
                var eyeShoes = sides.Select(x => x.eyeShoe).ToArray();
                var others = eyeShoes.Concat(new[] { taya }).ToArray();
                return new[]
                {
                    new View("his eyes", SeanEye(), can + Vector3.up * 0.3f, 70f, sean),
                    new View("target's eyes", Head(taya), sean.transform.position + Vector3.up * 1.0f, 60f, others),
                    new View("side", mid + right * 8f + Vector3.up * 1.8f, mid + Vector3.up * 0.7f, 45f, eyeShoes),
                    new View("above", mid + Vector3.up * 12f - fwd * 4f, mid, 50f, eyeShoes),
                };
            };
        }

        private static void BuildFist(Stage s, Plan p)
        {
            Vector3 fwd = Vector3.forward, right = Vector3.right;
            Vector3 S = s.Ground(0f, -4f);
            var sean = s.Person(p, "sean", S, 0f);
            Vector3 spot = s.Ground(0f, 1f);
            var target = s.Person(p, "dante", spot, 180f);
            p.Old = Side(s, p, "old"); p.New = Side(s, p, "new");
            SeanMagmaFist oldFist = null, newFist = null;
            int taken = 0;
            p.Release = () =>
            {
                if (oldFist != null) Object.DestroyImmediate(oldFist.gameObject);
                if (newFist != null) Object.DestroyImmediate(newFist.gameObject);
            };
            const float cast = 0.20f;
            float punch = cast + SeanRules.FistWarnSeconds, hitAt = punch + SeanRules.FistHitDelay;
            p.Seconds = punch + 2.0f;
            p.Notes.Add("staged: released at " + cast + " s, the puddle warns " + SeanRules.FistWarnSeconds + " s, the fist hits at " + hitAt.ToString("0.00")
                + " s; the target is thrown " + SeanRules.FistKnockUpHeight + " m up and " + SeanRules.FistPush + " m away (the can is taken to be behind him)");
            p.Notes.Add("old = `SeanMagmaFist.Stage(painted: false)`; new = `Stage(painted: true)`; the ring under the target is the same in both and is not staged");
            p.Notes.Add("the contact hold (the fist pausing on a touched body) is not staged: nobody owns these fists, so nobody is touched");
            bool made = false;
            float last = 0f;
            p.Pose = t =>
            {
                float dt = t - last; last = t;
                if (!made && t >= cast)
                {
                    made = true;
                    oldFist = SeanMagmaFist.Stage(spot, S, false, p.Old.transform);
                    newFist = SeanMagmaFist.Stage(spot, S, true, p.New.transform);
                    dt = t - cast;
                }
                if (made)
                {
                    // The old punch lays the runtime Ignition blast at the root of the open scene; it is taken out at once.
                    if (oldFist != null) taken += s.Contain(() => oldFist.Step(dt));
                    if (newFist != null) taken += s.Contain(() => newFist.Step(dt));
                }
                if (t >= hitAt)
                {
                    float u = t - hitAt;
                    float up = Mathf.Max(0f, SeanRules.FistLiftSpeed * u - 0.5f * Balance.CharacterGravity * u * u);
                    float w = Mathf.Min(u, SeanRules.FistPushSpeed / Balance.Friction);
                    float along = SeanRules.FistPushSpeed * w - 0.5f * Balance.Friction * w * w;
                    target.transform.position = spot + fwd * along + Vector3.up * up;
                }
            };
            p.Finish = () => p.Notes.Add("runtime objects (the old Ignition blast) taken out of the open scene as they were made: " + taken);
            p.Views = () =>
            {
                Vector3 mid = Vector3.Lerp(S, spot, 0.6f);
                return new[]
                {
                    new View("his eyes", Head(sean) + fwd * 0.2f, spot + Vector3.up * 0.8f, 70f, sean),
                    new View("target's eyes", Head(target), sean.transform.position + Vector3.up * 1.0f, 70f, target),
                    new View("side", mid + right * 7f + Vector3.up * 1.8f, spot + Vector3.up * 1.0f, 45f),
                    new View("above", spot + Vector3.up * 10f - fwd * 3f, spot, 50f),
                };
            };
        }
        private static void BuildFistBoard(Stage s, Plan p)
        {
            Vector3 fwd = Vector3.forward, right = Vector3.right;
            Vector3 S = s.Ground(0f, -4f);
            var sean = s.Person(p, "sean", S, 0f);
            p.Board = Side(s, p, "board");
            var variants = new[] { SeanFireFistLook.Variant.C_Core };
            var spots = new[] { s.Ground(-2.7f, 1f), s.Ground(0f, 1f), s.Ground(2.7f, 1f) };
            var fists = new SeanMagmaFist[3];
            p.Release = () => { foreach (var f in fists) if (f != null) Object.DestroyImmediate(f.gameObject); };
            const float cast = 0.20f;
            p.Seconds = cast + SeanRules.FistWarnSeconds + 1.55f;
            p.Notes.Add("board: C_Core, the approved look (`SeanFireFistLook.Variant`); each cast from in front of it at "
                + cast + " s, the fist at " + (cast + SeanRules.FistWarnSeconds).ToString("0.00") + " s; nobody on the pools");
            bool made = false;
            float last = 0f;
            p.Pose = t =>
            {
                float dt = t - last; last = t;
                if (!made && t >= cast)
                {
                    made = true;
                    for (int k = 0; k < 3; k++)
                        fists[k] = SeanMagmaFist.Stage(spots[k], new Vector3(spots[k].x, S.y, S.z), true, p.Board.transform, variants[k]);
                    dt = t - cast;
                }
                if (made) foreach (var f in fists) if (f != null) s.Contain(() => f.Step(dt));
            };
            p.Views = () => new[]
            {
                new View("his eyes", Head(sean) + fwd * 0.2f, s.Ground(0f, 1f) + Vector3.up * 1.2f, 62f, sean),
                new View("three-quarter", S + right * 5.5f + fwd * 1.5f + Vector3.up * 2.4f, s.Ground(0f, 1f) + Vector3.up * 1.1f, 55f),
            };
        }
    }
}
