using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
    /// PHAISTER'S KIT, FILMED IN ITS THREE LOOKS SIDE BY SIDE (`PhaisterLook`): TODAY'S | DIRECTION A, MOONLIT MOTHS |
    /// DIRECTION B, SLEEPY COSMOS. The owner asked to see both directions built before he picks one. On Eskinita opened as a
    /// PREVIEW scene (the owner's open scene is not touched and nothing is saved), in the map's own daylight, no bloom (no
    /// game camera blooms), 60 pictures a second, real hero models for scale. Modelled on `AmihanKitFilm`.
    ///
    /// Every picture is a grid: four rows (the caster's eyes, the other player's eyes, side, above) by three columns, the
    /// same moment and the same camera.
    ///
    /// ⚠️ STAGED, AND SAID SO. No match runs and nothing animates outside Play, so bodies are still poses and every look is
    /// stepped on the film's clock:
    ///   * teleport: she releases at 0 s and is moved to the destination at once (the rule is instant). TODAY'S is
    ///     `PhaisterSwarm` and `PhaisterArrivalSeal` stepped by their own `StepTo`; her body is hidden from 0 to the
    ///     swarm's `Reveal` (in a match the swarm hides it through `CharacterVisual`, which a preview body does not have).
    ///     The aim mark before the release is NOT filmed (today's `PhaisterAimSigil` runs off `Update` only).
    ///   * hex: the mark lands at 0 s; the film shows its birth, the middle of the fuse, the arming at 10 s and its fraying
    ///     away unused at 25 s. TODAY'S is `VoodooCursePresenter.PoseHexLines`, the very code a match runs, turned to each
    ///     camera; its centre is the presenter's sum with the 1.8 m capsule fallback (a preview body has no controller).
    ///
    /// Request: lines in `Temp/phaister-kit-film.request`: `ping` (answered at once), or `&lt;teleport|hex|all&gt;[,...] &lt;tag&gt;`.
    /// Frames: `Logs/paete-ability-film/phaisterkit_&lt;ability&gt;_&lt;tag&gt;_frames/` with `times.txt`, then
    /// `done_phaisterkit_&lt;tag&gt;.txt` (with costs per column and any shader errors) or `failed_phaisterkit_&lt;tag&gt;.txt`.
    /// </summary>
    [InitializeOnLoad]
    internal static class PhaisterKitFilmPoll
    {
        private const string Request = "Temp/phaister-kit-film.request";
        private static double _next;
        static PhaisterKitFilmPoll() { EditorApplication.update += Poll; }

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
                if (words[0] == "ping") { Directory.CreateDirectory(PhaisterKitFilm.OutDir); File.WriteAllText(Path.Combine(PhaisterKitFilm.OutDir, "done_phaisterkit_ping.txt"), DateTime.Now.ToString("HH:mm:ss")); continue; }
                PhaisterKitFilm.Run(words[0], words.Length > 1 ? words[1] : "p1");
            }
        }
    }

    internal static class PhaisterKitFilm
    {
        internal const string OutDir = "Logs/paete-ability-film";
        private const string Map = "Assets/TumbangPreso/Scenes/Maps/Eskinita.unity";
        private const int CellWidth = 640, CellHeight = 360, Rate = 60, Columns = 3;
        private const float Base = 100f;
        private static readonly string[] Abilities = { "teleport", "hex" };
        private static readonly string[] ColumnNames = { "TODAY", "A moonlit moths", "B sleepy cosmos" };
        private static readonly string[] Shaders = { "PhaisterMoths", "PhaisterGhost", "PhaisterCosmos", "PhaisterPassiveMoths" };
        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private struct View
        {
            public string Name; public Func<Vector3> Eye, Aim; public float Fov; public GameObject[] Hide;
            /// <summary>Shown for this row only (a wall for the depth test), hidden for every other row.</summary>
            public GameObject Only;
            public View(string name, Func<Vector3> eye, Func<Vector3> aim, float fov, params GameObject[] hide) { Name = name; Eye = eye; Aim = aim; Fov = fov; Hide = hide; Only = null; }
        }

        private sealed class Plan
        {
            public string Name;
            public readonly List<(float from, float to)> Segments = new List<(float, float)>();
            public Action<float> Pose = _ => { };
            /// <summary>Per column, after `Pose`, before that column's pictures.</summary>
            public Action<int, float> PoseColumn = (_, __) => { };
            /// <summary>Turns whatever faces the lens to this view's camera, per column.</summary>
            public Action<int, float, Transform> Face = (_, __, ___) => { };
            public View[] Views;
            public readonly List<GameObject> Roots = new List<GameObject>();
            public readonly List<GameObject>[] Cols = { new List<GameObject>(), new List<GameObject>(), new List<GameObject>() };
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

            public GameObject Person(Plan plan, string id, Vector3 at, float yaw, out Transform model)
            {
                model = null;
                var holder = In(new GameObject("~PhaisterKit-" + id), plan);
                holder.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
                var art = RosterBook.Load()?.FindPersonArt(id);
                if (art == null || art.Model == null) { Report.AppendLine("  " + id + "'s model was not found"); return holder; }
                var go = (GameObject)Object.Instantiate(art.Model, holder.transform);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.Euler(0f, CharacterVisual.PersonModelYaw, 0f);
                go.transform.localScale = Vector3.one * CharacterVisual.PersonScale;
                ToonSkin.Apply(go, ToonSkin.PersonOutlineWidth, art.Palette);
                foreach (var animator in go.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
                model = go.transform;
                return holder;
            }

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
            tag = new string((tag ?? "p1").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray()).ToLowerInvariant();
            if (tag.Length == 0) tag = "p1";
            string donePath = Path.Combine(OutDir, "done_phaisterkit_" + tag + ".txt"), failedPath = Path.Combine(OutDir, "failed_phaisterkit_" + tag + ".txt");
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
            var keepLook = PhaisterLook.Direction;
            try
            {
                foreach (string name in Shaders)
                {
                    var shader = Resources.Load<Shader>("Shaders/" + name);
                    if (shader == null) { report.AppendLine("SHADER MISSING: " + name); continue; }
                    foreach (var m in ShaderUtil.GetShaderMessages(shader))
                        report.AppendLine("shader " + name + " " + m.severity + ": " + m.message + " (line " + m.line + ")");
                }
                stage.Scene = EditorSceneManager.OpenPreviewScene(Map);
                if (!stage.Scene.IsValid()) throw new InvalidOperationException("No map to film on.");
                stage.Physics = stage.Scene.GetPhysicsScene();
                lit = Unsupported.SetOverrideLightingSettings(stage.Scene);
                Shader.SetGlobalFloat("_CharacterSmoothShade", ToonSkin.CharacterSmoothShade);
                Shader.SetGlobalFloat("_FxStudyBloom", 0f);
                report.AppendLine("map: " + Map + (lit ? ", lit by its own lighting settings" : ", its lighting could not be put in force") + "; no bloom; " + Rate + " pictures a second; columns " + string.Join(" | ", ColumnNames));

                var camGo = new GameObject("~PhaisterKitCamera");
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

                Color[] Shoot(View v, int column, Vector3 eye)
                {
                    var hid = new List<GameObject>();
                    if (v.Hide != null && column < v.Hide.Length && v.Hide[column] != null && v.Hide[column].activeSelf) { v.Hide[column].SetActive(false); hid.Add(v.Hide[column]); }
                    if (v.Only != null) v.Only.SetActive(true);
                    cam.fieldOfView = v.Fov;
                    var dir = v.Aim() - eye;
                    var up = Mathf.Abs(dir.normalized.y) > 0.95f ? Vector3.forward : Vector3.up;
                    cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(dir, up));
                    cam.targetTexture = plain;
                    cam.Render();
                    var keep = RenderTexture.active; RenderTexture.active = plain;
                    cell.ReadPixels(new Rect(0, 0, CellWidth, CellHeight), 0, 0); cell.Apply();
                    RenderTexture.active = keep;
                    foreach (var g in hid) g.SetActive(true);
                    if (v.Only != null) v.Only.SetActive(false);
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
                            var costs = new List<string>();
                            for (int c = 0; c < Columns; c++) { plan.PoseColumn(c, at); plan.Face(c, at, cam.transform); costs.Add(ColumnNames[c] + " " + Cost(plan.Cols[c])); }
                            report.AppendLine("  cost at " + at.ToString("0.00") + " s, before batching: " + string.Join("; ", costs));
                        }

                        string frames = Path.Combine(OutDir, "phaisterkit_" + name + "_" + tag + "_frames");
                        if (Directory.Exists(frames)) foreach (string old in Directory.GetFiles(frames)) File.Delete(old);
                        Directory.CreateDirectory(frames);
                        int rows = plan.Views.Length;
                        var sheet = new Texture2D(CellWidth * Columns, CellHeight * rows, TextureFormat.RGB24, false);
                        var times = new System.Text.StringBuilder();
                        times.AppendLine("rows top to bottom: " + string.Join("|", plan.Views.Select(v => v.Name)) + "; columns: " + string.Join("|", ColumnNames));
                        int f = 0;
                        foreach (var (from, to) in plan.Segments)
                        {
                            int count = Mathf.RoundToInt((to - from) * Rate) + 1;
                            for (int i = 0; i < count; i++, f++)
                            {
                                float t = from + i / (float)Rate;
                                SetTime(t);
                                plan.Pose(t);
                                for (int column = 0; column < Columns; column++)
                                {
                                    var off = new List<GameObject>();
                                    for (int other = 0; other < Columns; other++)
                                        if (other != column) foreach (var g in plan.Cols[other]) if (g != null && g.activeSelf) { g.SetActive(false); off.Add(g); }
                                    plan.PoseColumn(column, t);
                                    for (int v = 0; v < rows; v++)
                                    {
                                        SetTime(t);
                                        Vector3 eye = plan.Views[v].Eye();
                                        cam.transform.position = eye;
                                        cam.transform.rotation = Quaternion.LookRotation(plan.Views[v].Aim() - eye, Vector3.up);
                                        plan.Face(column, t, cam.transform);
                                        sheet.SetPixels(column * CellWidth, (rows - 1 - v) * CellHeight, CellWidth, CellHeight, Shoot(plan.Views[v], column, eye));
                                    }
                                    foreach (var g in off) g.SetActive(true);
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
                        Debug.LogError("[PhaisterKitFilm] " + name + ": " + failure);
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
                Debug.LogError("[PhaisterKitFilm] " + failure);
            }
            finally
            {
                PhaisterLook.Direction = keepLook;
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
                    if (clear != null) { clear.Invoke(null, new object[] { active }); File.AppendAllText(File.Exists(donePath) ? donePath : failedPath, "the owner's open scene had been marked changed and was cleared\n"); }
                    else File.AppendAllText(File.Exists(donePath) ? donePath : failedPath, "the owner's open scene was marked changed (nothing of the film's is left in it)\n");
                }
            }
        }

        // Draw calls as renderers times their sub-meshes (a line renderer counts one), triangles as their meshes hold,
        // distinct materials: only what is active and not forced off. The bodies' toon renderers are not counted.
        private static string Cost(IEnumerable<GameObject> roots)
        {
            int draws = 0, lines = 0; long tris = 0; var mats = new HashSet<Material>();
            foreach (var root in roots)
            {
                if (root == null || !root.activeInHierarchy) continue;
                foreach (var r in root.GetComponentsInChildren<Renderer>(false))
                {
                    if (!r.enabled || r.forceRenderingOff) continue;
                    if (r is LineRenderer) { lines++; draws++; if (r.sharedMaterial != null) mats.Add(r.sharedMaterial); continue; }
                    var mesh = r.TryGetComponent<MeshFilter>(out var filter) ? filter.sharedMesh : null;
                    if (mesh == null) continue;
                    if (r.GetComponent<VfxRenderTag>() == null && r.sharedMaterial != null && r.sharedMaterial.shader.name.Contains("Toon")) continue;
                    int subs = Mathf.Max(1, Mathf.Min(mesh.subMeshCount, r.sharedMaterials.Length));
                    int passes = r.sharedMaterial != null ? Mathf.Max(1, r.sharedMaterial.passCount) : 1;
                    draws += subs * passes;
                    for (int s = 0; s < subs; s++) tris += mesh.GetIndexCount(s) / 3;
                    foreach (var m in r.sharedMaterials) if (m != null) mats.Add(m);
                }
            }
            return draws + " draws (" + lines + " lines), " + tris + " triangles, " + mats.Count + " materials";
        }

        private static void Build(Stage s, Plan p)
        {
            switch (p.Name)
            {
                case "teleport": BuildTeleport(s, p); return;
                case "hex": BuildHex(s, p, false); return;
                case "hexrun": BuildHex(s, p, true); return;
                case "drain": BuildDrain(s, p); return;
                case "passive": BuildPassive(s, p); return;
            }
            throw new ArgumentException("No such ability: " + p.Name + " (teleport, hex or all)");
        }

        private static void SetHidden(Transform model, bool hidden)
        {
            if (model == null) return;
            foreach (var r in model.GetComponentsInChildren<Renderer>(true)) r.forceRenderingOff = hidden;
        }

        // ---------------------------------------------------------------- TELEPORT

        private static void BuildTeleport(Stage s, Plan p)
        {
            Vector3 up = Vector3.up;
            Vector3 S = s.Ground(0f, -2f), D = s.Ground(1.4f, 3.2f);
            Vector3 fwd = D - S; fwd.y = 0f; fwd.Normalize();
            Vector3 right = Vector3.Cross(up, fwd).normalized;
            float yaw = Quaternion.LookRotation(fwd).eulerAngles.y;
            Vector3 mid = Vector3.Lerp(S, D, 0.5f);

            var bodies = new GameObject[Columns];
            var models = new Transform[Columns];
            for (int c = 0; c < Columns; c++) { bodies[c] = s.Person(p, "phaister", S, yaw, out models[c]); p.Cols[c].Add(bodies[c]); }
            var observer = s.Person(p, "dante", mid - right * 4.2f + fwd * 0.6f, Quaternion.LookRotation(right).eulerAngles.y, out _);

            PhaisterSwarm swarm = null; GameObject seal = null;
            p.Cols[0].AddRange(s.Capture(p, () => swarm = PhaisterSwarm.Play(bodies[0].transform, S, D, fwd)));
            p.Cols[0].AddRange(s.Capture(p, () => seal = PhaisterArrivalSeal.Create(D, fwd)));
            PhaisterMothRibbon ribbon = null; PhaisterCosmosPortal portal = null;
            p.Cols[1].AddRange(s.Capture(p, () => ribbon = PhaisterMothRibbon.Play(models[1], S, D)));
            p.Cols[2].AddRange(s.Capture(p, () => portal = PhaisterCosmosPortal.Play(models[2], S, D)));
            if (swarm == null || ribbon == null || portal == null) throw new InvalidOperationException("A teleport look was not built.");
            var sealFx = seal != null ? seal.GetComponent<PhaisterArrivalSeal>() : null;
            var effects = new[] { new List<GameObject>(p.Cols[0].Skip(1)), new List<GameObject>(p.Cols[1].Skip(1)), new List<GameObject>(p.Cols[2].Skip(1)) };
            float[] reveal = { PhaisterSwarm.Reveal, PhaisterMothRibbon.Reveal, PhaisterCosmosPortal.Reveal };
            float[] life = { PhaisterSwarm.Life, PhaisterMothRibbon.Life, PhaisterCosmosPortal.Life };
            s.Report.AppendLine("  STAGED: " + (D - S).magnitude.ToString("0.0") + " m teleport released at 0 s; she is at the far end from 0 s (the rule is instant). Observer 4.2 m to the side of the path.");

            p.Pose = t =>
            {
                foreach (var b in bodies) b.transform.position = t < 0f ? S : D;
            };
            p.PoseColumn = (c, t) =>
            {
                bool on = t >= 0f && t < life[c];
                foreach (var g in effects[c]) if (g != null) g.SetActive(on);
                if (on)
                {
                    if (c == 0) { swarm.StepTo(t); if (sealFx != null) sealFx.StepTo(t); }
                    else if (c == 1) ribbon.StepTo(t);
                    else portal.StepTo(t);
                }
                SetHidden(models[c], t >= 0f && t < reveal[c]);
            };
            p.CostAt = new[] { 0.12f, 0.30f };
            p.Segments.Add((-0.15f, 1.40f));
            p.Views = new[]
            {
                new View("caster's eyes", () => bodies[0].transform.position + up * 1.45f + fwd * 0.1f, () => bodies[0].transform.position + fwd * 5f + up * 0.4f, 75f, bodies),
                new View("other player's eyes (4 m to the side)", () => observer.transform.position + up * 1.4f, () => mid + up * 0.9f, 70f, observer, observer, observer),
                new View("side", () => mid + right * 8.5f - fwd * 1.0f + up * 2.4f, () => mid + up * 0.8f, 55f),
                new View("above", () => mid + up * 11f - fwd * 2.5f, () => mid, 55f),
            };
        }

        // ---------------------------------------------------------------- DRAIN AND THE PASSIVE (`PhaisterDrainLook`, `PhaisterPassiveLook`)

        /// <summary>
        /// STAGED like the rest of this film: still poses. The left column is driven through `PhaisterDrainPresenter.Timeline`, the
        /// clock a match runs, from scripted replicated values two frames late, and its victim's-eyes row draws what the victim's
        /// own screen draws. Middle: the same with the reach broken at 1.2 s. Right: the passive (the bodies stand and the look is
        /// told they run, so the gap opening between runners is not shown). The report runs the clock through each ending.
        /// </summary>
        private static void BuildDrain(Stage s, Plan p)
        {
            Vector3 up = Vector3.up, fwd = Vector3.forward, right = Vector3.right;
            Vector3 S = s.Ground(0f, -2f), V = s.Ground(0.6f, 3.4f);
            var her = s.Person(p, "phaister", S, 0f, out _);
            var victim = s.Person(p, "zack", V, 180f, out var victimModel);
            float top = float.MinValue;
            foreach (var r in victimModel.GetComponentsInChildren<Renderer>()) top = Mathf.Max(top, r.bounds.max.y);
            float crown = top - V.y;
            s.Report.AppendLine("  crown measured on the victim's model: " + crown.ToString("0.00") + " m above the feet (fallback " + PhaisterDrainPresenter.FallbackCrown + ")");
            foreach (string line in ClockChecks()) s.Report.AppendLine("  " + line);
            Vector3 head = V + up * crown;
            Vector3 hand = S + up * 1.15f + (V - S).normalized * 0.38f;
            Vector3 ownEye = V + up * 1.45f - fwd * 0.1f;
            var roots = new GameObject[3];
            for (int c = 0; c < 3; c++) { roots[c] = s.In(new GameObject("~PhaisterKit-drain-" + c), p); p.Cols[c].Add(roots[c]); }
            var whole = new PhaisterDrainLook(roots[0].transform);
            var broke = new PhaisterDrainLook(roots[1].transform);
            var drag = new PhaisterPassiveLook(roots[2].transform);
            var haste = new PhaisterPassiveLook(roots[2].transform);
            float last = -1f;
            s.Report.AppendLine("  STAGED: her reach starts at 0 s on a victim " + (V - S).magnitude.ToString("0.0") + " m away; marked at 2 s; drained at 3.5 s; refill at 6 s.");
            p.Pose = t => { };
            p.PoseColumn = (c, t) => { };
            p.Face = (c, t, camT) =>
            {
                if (t < 0f) { roots[c].SetActive(false); return; }
                roots[c].SetActive(true);
                bool own = (camT.position - ownEye).sqrMagnitude < 0.01f;
                if (c == 0) whole.Pose(ClockAt(t), -1f, head, hand, camT.position, own);
                else if (c == 1) { if (t < 3.2f) broke.Pose(t, 1.2f, head, hand, camT.position, own); else broke.Hide(); }
                else
                {
                    float dt = Mathf.Clamp(t - last, 1f / 240f, 0.05f); last = t;
                    bool on = t < 5.0f;
                    drag.PoseVictim(t, V, -fwd, Vector3.zero, on, own, dt);
                    haste.PoseHaste(t, S, V, fwd * 5.5f, fwd, on, dt);
                }
            };
            p.CostAt = new[] { 1.5f, 3.0f, 3.52f, 3.60f, 3.80f, 4.20f, 4.50f, 5.5f };
            p.Segments.Add((-0.10f, 6.55f));
            p.Views = new[]
            {
                new View("caster's eyes", () => S + up * 1.45f + fwd * 0.1f, () => V + up * 1.9f, 60f, her, her, her),
                new View("victim's own screen", () => ownEye, () => S + up * 1.4f, 75f, victim, victim, victim),
                new View("side", () => Vector3.Lerp(S, V, 0.5f) + right * 6.5f - fwd * 0.8f + up * 2.6f, () => Vector3.Lerp(S, V, 0.5f) + up * 1.3f, 55f),
                new View("above", () => Vector3.Lerp(S, V, 0.5f) + up * 9.5f - fwd * 3.5f, () => Vector3.Lerp(S, V, 0.5f) + up * 1f, 50f),
            };
        }

        /// <summary>What a peer would know at `t` of a drain that runs to its end, with its snapshots `late` seconds old.</summary>
        private static PhaisterDrainPresenter.Known Scripted(float t, float late)
        {
            float k = Mathf.Max(0f, t - late);
            float reach = VoodooRules.ReachSeconds, snap = reach + VoodooRules.DrainDelaySeconds;
            return new PhaisterDrainPresenter.Known
            {
                Reaching = k < reach, ReachElapsed = k, ReachSucceeded = k >= reach,
                Marked = k >= reach && k < snap, MarkAge = k - reach,
                Drained = k >= snap && k < snap + StatusRules.DrainedSeconds, DrainedLeft = snap + StatusRules.DrainedSeconds - k,
            };
        }

        private static float ClockAt(float t)
        {
            var clock = PhaisterDrainPresenter.Timeline.Start;
            for (float at = 0f; at < t; at += 1f / 60f) clock.Step(Scripted(at, 2f / 60f), 1f / 60f);
            return clock.T;
        }

        /// <summary>The presenter's clock through each ending, in its own numbers, printed in the film's report.</summary>
        private static IEnumerable<string> ClockChecks()
        {
            const float Dt = 1f / 60f;
            string Say(string name, bool good, string detail) => (good ? "clock PASS " : "clock FAIL ") + name + ": " + detail;

            var a = PhaisterDrainPresenter.Timeline.Start; float back = 0f;
            for (float t = 0f; t < 7f && !a.Over; t += Dt) { float was = a.T; a.Step(Scripted(t, 0.1f), Dt); back = Mathf.Max(back, was - a.T); }
            yield return Say("full drain, snapshots 0.1 s late", a.BrokeAt < 0f && a.Over && back == 0f, "ended at " + a.T.ToString("0.00") + ", brokeAt " + a.BrokeAt + ", never ran back");

            var b = PhaisterDrainPresenter.Timeline.Start;
            for (float t = 0f; t < 1.2f; t += Dt) b.Step(new PhaisterDrainPresenter.Known { Reaching = true, ReachElapsed = t }, Dt);
            b.Step(new PhaisterDrainPresenter.Known(), Dt);
            yield return Say("reach snapped at 1.2 s", Mathf.Abs(b.BrokeAt - 1.2f) < 0.05f, "brokeAt " + b.BrokeAt.ToString("0.00"));

            var c = PhaisterDrainPresenter.Timeline.Start;
            for (float t = 0f; t < 2f; t += Dt) c.Step(new PhaisterDrainPresenter.Known { Reaching = true, ReachElapsed = t }, Dt);
            for (float t = 0f; t < 0.3f; t += Dt) c.Step(new PhaisterDrainPresenter.Known { ReachSucceeded = true }, Dt);
            bool waited = c.BrokeAt < 0f;
            c.Step(new PhaisterDrainPresenter.Known { ReachSucceeded = true, Marked = true, MarkAge = 0.3f }, Dt);
            yield return Say("mark's snapshot 0.3 s late", waited && c.BrokeAt < 0f, "no false break, T " + c.T.ToString("0.00"));

            var d = PhaisterDrainPresenter.Timeline.Start;
            for (float t = 0f; t < 2f; t += Dt) d.Step(new PhaisterDrainPresenter.Known { Reaching = true, ReachElapsed = t }, Dt);
            for (float t = 0f; t < 0.5f; t += Dt) d.Step(new PhaisterDrainPresenter.Known { ReachSucceeded = true, Marked = true, MarkAge = t }, Dt);
            for (float t = 0f; t < 0.6f; t += Dt) d.Step(new PhaisterDrainPresenter.Known { ReachSucceeded = true }, Dt);
            yield return Say("mark cleansed after 0.5 s", d.BrokeAt >= 0f, "brokeAt " + d.BrokeAt.ToString("0.00"));
        }

        // ---------------------------------------------------------------- THE PASSIVE, RUNNING (`PhaisterPassiveLook`)

        /// <summary>
        /// STAGED: bodies are posed by the `sprint` and `idle` clips and slid along their paths; no match runs. Both run circles at
        /// the rule's own speeds, so the wake is seen curving. Columns: running | standing | running with Hex and Drain on the
        /// victim at once, to see that neither is buried. A mark lands at 0.8 s and ends at 5 s. Row two is the victim's own screen.
        /// </summary>
        private static void BuildPassive(Stage s, Plan p)
        {
            Vector3 up = Vector3.up;
            Vector3 C = s.Ground(0f, 1f);
            const float MarkAt = 0.8f, MarkEnd = 5f, Speed = 4.5f, RHer = 2.8f, RThem = 4.4f, Step = 1f / 60f;
            float themShare = VoodooRules.PassiveSpeedScale(false), herShare = VoodooRules.PassiveSpeedScale(true);
            var hers = new GameObject[Columns]; var thems = new GameObject[Columns];
            var herModels = new Transform[Columns]; var themModels = new Transform[Columns];
            var herLook = new PhaisterPassiveLook[Columns]; var themLook = new PhaisterPassiveLook[Columns]; var ownLook = new PhaisterPassiveLook[Columns];
            var ownRoots = new GameObject[Columns]; var themRoots = new GameObject[Columns];
            var themNow = new Vector3[Columns]; var herNow = new Vector3[Columns]; var themWay = new Vector3[Columns];
            for (int c = 0; c < Columns; c++)
            {
                hers[c] = s.Person(p, "phaister", C, 0f, out herModels[c]);
                thems[c] = s.Person(p, "zack", C, 0f, out themModels[c]);
                p.Cols[c].Add(hers[c]); p.Cols[c].Add(thems[c]);
                var root = s.In(new GameObject("~PhaisterKit-passive-" + c), p); p.Cols[c].Add(root);
                themRoots[c] = s.In(new GameObject("~PhaisterKit-passive-them-" + c), p); p.Cols[c].Add(themRoots[c]);
                ownRoots[c] = s.In(new GameObject("~PhaisterKit-passive-own-" + c), p); p.Cols[c].Add(ownRoots[c]);
                herLook[c] = new PhaisterPassiveLook(root.transform);
                themLook[c] = new PhaisterPassiveLook(themRoots[c].transform);
                ownLook[c] = new PhaisterPassiveLook(ownRoots[c].transform);
                herLook[c].BindLegs(herModels[c]); themLook[c].BindLegs(themModels[c]); ownLook[c].BindLegs(themModels[c]);
            }
            AnimationClip sprint = null, idle = null;
            var art = RosterBook.Load()?.FindPersonArt("zack");
            if (art != null && art.Model != null)
                foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(art.Model)))
                    if (sub is AnimationClip clip) { if (clip.name == "sprint") sprint = clip; else if (clip.name == "idle") idle = clip; }
            s.Report.AppendLine("  STAGED: clips sampled and bodies slid along circles. sprint clip: " + (sprint != null ? sprint.length.ToString("0.00") + " s" : "NOT FOUND"));
            var drainRoot = s.In(new GameObject("~PhaisterKit-passive-drain"), p); p.Cols[2].Add(drainRoot);
            var hexRoot = s.In(new GameObject("~PhaisterKit-passive-hex"), p); p.Cols[2].Add(hexRoot);
            var drain = new PhaisterDrainLook(drainRoot.transform);
            var hex = new PhaisterHexSigil(hexRoot.transform);
            hex.Bind(themModels[2]);
            float Ran(float t, float share) { float during = Mathf.Clamp(t - MarkAt, 0f, MarkEnd - MarkAt); return Speed * (Mathf.Max(t, 0f) - during) + Speed * share * during; }
            Vector3 Around(float dist, float radius, float offset, out Vector3 way)
            {
                float a = dist / radius + offset;
                way = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
                return s.Ground(C.x + Mathf.Cos(a) * radius, C.z + Mathf.Sin(a) * radius);
            }
            p.PoseColumn = (c, t) =>
            {
                bool running = c != 1, on = t >= MarkAt && t < MarkEnd;
                Vector3 herAt, themAt, herW, themW;
                if (running)
                {
                    herAt = Around(Ran(t, herShare), RHer, 3.6f, out herW);
                    themAt = Around(Ran(t, themShare), RThem, 2.9f, out themW);
                }
                else
                {
                    herAt = s.Ground(C.x - 2.2f, C.z - 1.6f); themAt = s.Ground(C.x + 1.4f, C.z + 1.8f);
                    herW = themAt - herAt; herW.y = 0f; herW.Normalize(); themW = -herW;
                }
                hers[c].transform.SetPositionAndRotation(herAt, Quaternion.LookRotation(herW));
                thems[c].transform.SetPositionAndRotation(themAt, Quaternion.LookRotation(themW));
                var pose = running ? sprint : idle;
                if (pose != null)
                {
                    float clipAt = Mathf.Repeat(Mathf.Max(t, 0f), pose.length);
                    if (herModels[c] != null) pose.SampleAnimation(herModels[c].gameObject, clipAt);
                    if (themModels[c] != null) pose.SampleAnimation(themModels[c].gameObject, Mathf.Repeat(clipAt + 0.21f, pose.length));
                }
                float mark = Mathf.Max(0f, t - MarkAt);
                Vector3 themMove = running ? themW * Speed * (on ? themShare : 1f) : Vector3.zero, herMove = running ? herW * Speed * (on ? herShare : 1f) : Vector3.zero;
                themLook[c].PoseVictim(mark, themAt, themW, themMove, on, false, Step);
                ownLook[c].PoseVictim(mark, themAt, themW, themMove, on, true, Step);
                herLook[c].PoseHaste(mark, herAt, themAt, herMove, herW, on, Step);
                if (c != 2) hex.DarkenBody();
                themNow[c] = themAt; herNow[c] = herAt; themWay[c] = themW;
            };
            p.Face = (c, t, camT) =>
            {
                bool own = (camT.position - (themNow[c] + up * 1.45f - themWay[c] * 0.1f)).sqrMagnitude < 0.01f;
                ownRoots[c].SetActive(own); themRoots[c].SetActive(!own);
                if (c != 2) return;
                if (t < MarkAt) { drain.Hide(); hex.Hide(); return; }
                float m = t - MarkAt;
                Vector3 hand = herNow[c] + up * 1.15f + (themNow[c] - herNow[c]).normalized * 0.38f;
                drain.Pose(m * 1.2f, -1f, themNow[c] + up * 2.0f, hand, camT.position, own);
                FxStudyPhaisterHex.Timeline(m, false, out float re, out float mk, out float st, out float he, out float fr);
                hex.Pose(re, mk, st, he, fr, themNow[c], 1.35f, hand, t + 100f, camT.position, own);
            };
            p.CostAt = new[] { 2.5f };
            p.Segments.Add((0f, 6.2f));
            // Reads the column being drawn: `Shoot` calls Eye for each column in turn, after that column's PoseColumn.
            int shooting = 0;
            var pose0 = p.PoseColumn;
            p.PoseColumn = (c, t) => { shooting = c; pose0(c, t); };
            p.Views = new[]
            {
                new View("another player, 7 m off", () => C + new Vector3(-7.5f, 1.45f, -2.5f), () => C + up * 0.7f, 62f),
                new View("victim's own screen", () => themNow[shooting] + up * 1.45f - themWay[shooting] * 0.1f, () => themNow[shooting] + up * 1.3f + themWay[shooting] * 5f, 75f, thems[0], thems[1], thems[2]),
                new View("side", () => C + new Vector3(7.5f, 3.0f, -6f), () => C + up * 0.6f, 45f),
                new View("above", () => C + new Vector3(0f, 11f, -5f), () => C, 50f),
            };
        }

        // ---------------------------------------------------------------- HEX

        private static void BuildHex(Stage s, Plan p, bool run)
        {
            Vector3 up = Vector3.up, fwd = Vector3.forward, right = Vector3.right;
            Vector3 S = s.Ground(0f, -2f), V = s.Ground(0.6f, 4.2f);
            var her = s.Person(p, "phaister", S, 0f, out _);
            var victim = s.Person(p, "zack", V, 180f, out var victimModel);
            // The presenter's centre: capsule height plus 0.95 m, with its own 1.8 m fallback.
            Vector3 centre = V + up * (1.8f + 0.95f);

            var oldRoot = s.In(new GameObject("~PhaisterKit-hex-today"), p);
            var mark = VoodooCursePresenter.NewThread(oldRoot.transform, "VoodooMark", 48);
            var stitchA = VoodooCursePresenter.NewThread(oldRoot.transform, "VoodooMarkStitchA", 2);
            var stitchB = VoodooCursePresenter.NewThread(oldRoot.transform, "VoodooMarkStitchB", 2);
            var block = new MaterialPropertyBlock();
            p.Cols[0].Add(oldRoot);
            var aRoot = s.In(new GameObject("~PhaisterKit-hex-stab"), p);
            var bRoot = s.In(new GameObject("~PhaisterKit-hex-fray"), p);
            var a = new PhaisterHexSigil(aRoot.transform);
            var b = new PhaisterHexSigil(bRoot.transform);
            p.Cols[1].Add(aRoot); p.Cols[2].Add(bRoot);
            a.Bind(victimModel); b.Bind(victimModel);
            Vector3 hand = S + up * 1.0f + (V - S).normalized * 0.45f;
            const float BodyHeight = 1.35f; // `VoodooCursePresenter.BodyMarkHeight`
            // The fifth row looks past a wall standing between its camera and the victim, covering half of him: what is behind it
            // must be hidden and what is clear of it must show. Only that row sees the wall.
            Vector3 wallEye = V + right * 6.0f - fwd * 2.0f + up * 2.0f;
            var wall = s.In(GameObject.CreatePrimitive(PrimitiveType.Cube), p);
            wall.name = "~PhaisterKit-wall";
            Object.DestroyImmediate(wall.GetComponent<Collider>());
            Vector3 wallAt = Vector3.Lerp(wallEye, V + up * 1f, 0.5f);
            wall.transform.SetPositionAndRotation(new Vector3(wallAt.x, V.y + 2f, wallAt.z) + Vector3.Cross(up, (V - wallEye).normalized) * 0.75f, Quaternion.LookRotation(new Vector3(V.x - wallEye.x, 0f, V.z - wallEye.z)));
            wall.transform.localScale = new Vector3(1.5f, 4f, 0.15f);
            wall.SetActive(false);
            Vector3 runDir = (right * 0.8f + fwd * 0.6f).normalized;
            Vector3 VNow = V;
            s.Report.AppendLine("  STAGED (not Play): the bench's squeezed timeline on a real hero on the map" + (run ? ", who SLIDES at 2 m/s in a still pose (nothing animates outside Play)" : "") + ". Column 0 the old mark, no glow, 1 the rebuilt hex to the stab, 2 to the fray. The victim's row passes ownView as the game would. Row 5 is behind a wall.");
            p.Pose = t =>
            {
                VNow = run ? V + runDir * (Mathf.PingPong(2f * Mathf.Max(t, 0f), 7f) - 3.5f) : V;
                victim.transform.position = VNow;
            };
            p.PoseColumn = (c, t) => { if (c == 0) oldRoot.SetActive(t >= 0f && t < 3.6f); if (c != 1) a.DarkenBody(); if (c != 2) b.DarkenBody(); };
            p.Face = (c, t, camT) =>
            {
                if (c == 0)
                {
                    if (t < 0f || t >= 3.6f) return;
                    float mk = Mathf.Max(0f, t - 2f) / 1.6f * VoodooRules.HexArmSeconds;
                    VoodooCursePresenter.PoseHexLines(mark, stitchA, stitchB, block, centre, camT.right, camT.up, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.18f)), Mathf.Clamp01(mk / VoodooRules.HexArmSeconds), false, t);
                    return;
                }
                if (t < 0f) { (c == 1 ? a : b).Hide(); return; }
                FxStudyPhaisterHex.Timeline(t, c == 1, out float re, out float mk2, out float st, out float he, out float fr);
                bool own = Vector3.Distance(camT.position, VNow + up * 1.45f) < 0.5f;
                (c == 1 ? a : b).Pose(re, mk2, st, he, fr, VNow, BodyHeight, hand, t + 100f, camT.position, own);
            };
            p.CostAt = new[] { 4.0f };
            p.Segments.Add((-0.10f, FxStudyPhaisterHex.TimelineSeconds(true)));
            p.Views = new[]
            {
                new View("caster's eyes", () => S + up * 1.45f + fwd * 0.1f, () => VNow + up * 1.0f, 60f, her, her, her),
                new View("victim's eyes", () => VNow + up * 1.45f - fwd * 0.1f, () => S + up * 1.4f, 75f, victim, victim, victim),
                new View("side", () => V + right * 6.0f - fwd * 2.0f + up * 3.0f, () => VNow + up * 0.8f, 42f),
                new View("above", () => V + up * 9.5f - fwd * 3.5f, () => VNow + up * 0.6f, 42f),
                new View("behind a wall", () => wallEye, () => V + up * 0.8f, 42f) { Only = wall },
            };
        }
    }
}
