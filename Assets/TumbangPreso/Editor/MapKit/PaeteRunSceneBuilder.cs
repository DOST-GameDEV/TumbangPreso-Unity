using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.Run;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// PAETE'S RUN, THE SCENE: `Scenes/Maps/PaeteRun.unity`, built from the gameplay layout
    /// (`Art/PaeteRun/paete_run_layout.json`, written by `tools/author_paete_run_layout.py`).
    ///
    /// ⚠️⚠️ NOT REGISTERED ANYWHERE, ON PURPOSE (owner, 2026-10-10, of how the mode is entered: "we'll figure it out
    /// later"). It is in no map list, no menu and not in the build settings. Open it in the editor and press Play.
    ///
    /// WHAT IT MAKES
    ///  * "CollisionSolids": every solid of the layout as a SOLID collider (a `BoxCollider`, or a convex prism for a
    ///    round one). ⚠️ THEY ARE NOT DRAWN THEMSELVES ANY MORE: solids that overlap at one height flickered. What
    ///    the art does not cover is drawn once, as the union of the solids, under "SolidsDrawn"
    ///    (`PaeteRunSceneBuilder.Solids.cs`), in the art's own stone by kind.
    ///  * The LOOK: the sun, the ambient, the haze and the sky from the "PaeteRun" row of `WorldLookProfile`, so the
    ///    editor view and Play agree (see `Lighting`), and `MapCameraRange` so the cameras see the whole bowl.
    ///  * The WATER, the CLOTH, the KITES, `RunLife` and `RunSoundscape`: `PaeteRunSceneBuilder.Life.cs`.
    ///  * "Movers": one <see cref="RunMover"/> each under a <see cref="RunMovers"/>, a kinematic body with a solid
    ///    collider. ⚠️ A mover wears `Models/mover_KIND.glb` if that file exists, and a grey primitive if not; its
    ///    collision is the primitive either way. The models are the art side's (`tools/author_paete_run_art_stages.py`,
    ///    `mv_*`), each with its own origin and size, so each kind is FITTED to the layout's mover in <see cref="Dress"/>:
    ///    a hung boulder's origin is the top of its chain (9 m over the boulder), a pendulum's is its pivot, a mill wheel
    ///    is unit radius, a floating rock's and a crumbling slab's is the middle of the top, a seesaw's is its axle.
    ///    The boulders and rocks come in sizes (`_b`, `_c`); the nearest is taken and scaled the rest of the way.
    ///  * "Pads": a `JumpPad` for each geyser. "Water": a sheet of the game's water over any swim volume the art
    ///    gives no surface (the swimming itself is the mode's, from the layout).
    ///  * "Dressing": ONLY if `Art/PaeteRun/paete_run_art.json` exists: its `placements` (model, position [x, y, z],
    ///    yaw, scale), their materials by name from every `materials*.json` beside it, exactly as the Eskinita builder
    ///    does (opaque on `TumbangPreso/NearFade` with the near band shut). `"hide_solids": true` in that file stops
    ///    the grey being drawn (the collision stays).
    ///  * "~Run" with the mode, "~Match" with the game's launcher, the kill plane, the bounds, a warm clear day.
    ///
    /// From a shell with the editor open: write `build`, `review`, `probe` (or several) into `Temp/paete-run.request`;
    /// the answer is `Temp/paete-run.done` (see <see cref="PaeteRunRequestWatcher"/>). Two more words, for looking:
    /// `sun=220` builds with the key light turned to that yaw for this one build, and `unmerged` builds with every
    /// solid drawn on its own as it used to be (the "before" of the junction shots).
    /// </summary>
    public static partial class PaeteRunSceneBuilder
    {
        /// <summary>
        /// The key light's turn about the vertical, degrees (the Y of the Sun's Euler rotation: the light travels
        /// toward +Z at 0 and toward +X at 90). ⚠️ CHOSEN BY LOOKING AT REVIEW RENDERS, not taken from the art's
        /// note (its 137 put the summit's slide and the start in the shade of their own gates). See `Lighting`.
        /// </summary>
        public const float DefaultSunYaw = 215f;

        /// <summary>For one build, from the request's words: another yaw to look at, and the old way of drawing the solids.</summary>
        private static float _sunYawOnce = float.NaN;
        private static bool _unmergedOnce;
        public const string MapName = "PaeteRun";
        public const string ScenePath = "Assets/TumbangPreso/Scenes/Maps/PaeteRun.unity";
        public const string Root = "Assets/TumbangPreso/Art/PaeteRun";
        public const string LayoutPath = Root + "/paete_run_layout.json";
        private const string ArtPath = Root + "/paete_run_art.json";
        private const string GridTexture = "Assets/TumbangPreso/Scenes/Temp/prototype-grid.png";
        private const string Tag = "[PaeteRun] ";

        [Serializable] private class MatSpec { public string name, texture; public float[] tint; public float tiling; public bool foliage, emissive, glossy, cutout; }
        [Serializable] private class MatFile { public MatSpec[] materials; }
        [Serializable] private class Placement { public string model; public float[] position; public float yaw; public float scale; }
        [Serializable] private class ArtFile { public Placement[] placements; public bool hide_solids; public float[] sun; }

        internal static readonly List<string> Warnings = new List<string>();

        private static void Warn(string message)
        {
            if (Warnings.Contains(message)) return;
            Warnings.Add(message);
            Debug.LogWarning(Tag + message);
        }

        [MenuItem("Tumbang Preso/Sample Map/Build Paete Run")]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        internal static RunLayout ReadLayout()
        {
            if (!File.Exists(LayoutPath)) throw new FileNotFoundException("Missing layout " + LayoutPath + " (run tools/author_paete_run_layout.py)");
            var layout = RunLayout.Parse(File.ReadAllText(LayoutPath));
            if (layout == null) throw new InvalidOperationException("Unreadable layout " + LayoutPath);
            return layout;
        }

        /// <summary>Builds and saves the scene. Returns one line saying what was built.</summary>
        public static string Build()
        {
            Warnings.Clear();
            var layout = ReadLayout();
            ArtFile art = null;
            if (File.Exists(ArtPath))
            {
                try { art = JsonUtility.FromJson<ArtFile>(File.ReadAllText(ArtPath)); }
                catch (Exception e) { Warn("unreadable paete_run_art.json: " + e.Message); }
            }
            var named = BuildMaterials(MergeMaterials());
            var grey = GreyMaterials();
            Dress(grey, named);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject(MapName).transform;
            root.gameObject.AddComponent<MapGrade>().Set(1.02f, 1.03f, 1.08f, 1, 1.9f);
            var mapLook = Lighting(root);
            // ⚠️ THE CAMERAS SEE THE WHOLE BOWL (`MapCameraRange`): the game camera's own far plane is 240 m and this
            // course is about 300 m across with rock stacks to 600 m and clouds to 650. The ink's edges are told to
            // fade from 70 to 240 m instead of with the haze (150 to 900 m), which would line every far boulder.
            var range = root.gameObject.AddComponent<MapCameraRange>();
            range.PlayFar = 1100f; range.FreeFar = 1600f; range.InkFadeStart = 70f; range.InkFadeEnd = 240f;

            // ---- the art, if the art side has listed any
            int placed = 0, missing = 0;
            if (art != null && art.placements != null)
            {
                var dressing = Group(root, "Dressing");
                var groups = new Dictionary<string, Transform>();
                foreach (var p in art.placements)
                {
                    if (p == null || string.IsNullOrEmpty(p.model) || p.position == null || p.position.Length < 3) { Warn("a placement has no model or no position"); continue; }
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Models/{p.model}.glb");
                    if (prefab == null) { Warn("missing model Models/" + p.model + ".glb"); missing++; continue; }
                    if (!groups.TryGetValue(p.model, out var group)) groups[p.model] = group = Group(dressing, p.model);
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group);
                    go.transform.SetPositionAndRotation(new Vector3(p.position[0], p.position[1], p.position[2]), Quaternion.Euler(0, p.yaw, 0));
                    if (p.scale > 0f) go.transform.localScale = Vector3.one * p.scale;
                    Rematerial(go, named);
                    foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
                    foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.isStatic = true;
                    placed++;
                }
            }
            bool hideGrey = art != null && art.hide_solids;

            // ---- solids
            var solids = Group(root, "CollisionSolids");
            int boxes = 0, rounds = 0;
            foreach (var s in layout.solids)
            {
                if (s == null) continue;
                var go = new GameObject(s.name) { layer = 0, isStatic = true };
                go.transform.SetParent(solids, false);
                go.transform.SetPositionAndRotation(s.c, Quaternion.Euler(s.pitch, s.yaw, 0f));
                Mesh mesh;
                if (s.shape == "cyl")
                {
                    mesh = PrismMesh(s.name, s.r, s.h);
                    var collider = go.AddComponent<MeshCollider>();
                    collider.sharedMesh = mesh; collider.convex = true;
                    rounds++;
                }
                else
                {
                    mesh = BoxMesh(s.name, s.s);
                    go.AddComponent<BoxCollider>().size = s.s;
                    boxes++;
                }
                if (!_unmergedOnce) continue;
                // The old way, kept only for the "before" of the junction shots (the request's `unmerged`).
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = Grey(grey, s.kind);
                renderer.enabled = !hideGrey;
            }
            Physics.SyncTransforms();
            // The collision is the layout's, whole, whatever is drawn or hidden: counted and struck on every build.
            string collision = VerifyCollision(solids, layout);
            // The art's ground is pressed under the decks before anything asks what the art covers.
            string ground = GroundUnderDecks(root, layout);
            string drawn = hideGrey ? "the solids are not drawn (hide_solids)"
                : _unmergedOnce ? HideWhatTheArtCovers(root, solids) + " solids hidden under the art that covers them, the rest drawn EACH ON ITS OWN (unmerged: they flicker where they overlap)"
                : DrawSolidsMerged(root, layout, grey);

            // ---- movers
            var moversRoot = Group(root, "Movers");
            var manager = moversRoot.gameObject.AddComponent<RunMovers>();
            var all = new List<RunMover>();
            int modelled = 0;
            var counts = new Dictionary<string, int>();
            foreach (var m in layout.movers)
            {
                if (m == null) continue;
                counts.TryGetValue(m.kind ?? "", out int index);
                counts[m.kind ?? ""] = index + 1;
                var go = new GameObject(m.name) { layer = 0 };
                go.transform.SetParent(moversRoot, false);
                go.transform.SetPositionAndRotation(m.c, Quaternion.Euler(0f, m.yaw, 0f));
                var mover = go.AddComponent<RunMover>();
                mover.Kind = m.kind; mover.Motion = MotionOf(m.motion); mover.Rest = m.c; mover.RestYaw = m.yaw;
                mover.Axis = m.axis.sqrMagnitude > 1e-6f ? m.axis.normalized : Vector3.up;
                mover.Amp = m.amp; mover.Period = m.period; mover.Phase = m.phase; mover.Pivot = m.pivot; mover.Arm = m.arm;
                mover.Radius = m.r; mover.Size = m.s;
                var body = go.AddComponent<Rigidbody>();
                body.isKinematic = true; body.useGravity = false;
                Mesh mesh = null; Vector3 meshScale = Vector3.one; Quaternion meshTurn = Quaternion.identity;
                if (m.shape == "sphere")
                {
                    var sphere = go.AddComponent<SphereCollider>(); sphere.radius = m.r; mover.Solid = sphere;
                    mesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx"); meshScale = Vector3.one * (m.r * 2f);
                }
                else if (m.shape == "capsule")
                {
                    var capsule = go.AddComponent<CapsuleCollider>();
                    capsule.direction = 0; capsule.radius = m.r; capsule.height = m.s.x; mover.Solid = capsule;
                    // Unity's capsule stands on Y, 2 m tall and 1 m across: laid along X and stretched to the log.
                    mesh = Resources.GetBuiltinResource<Mesh>("Capsule.fbx");
                    meshTurn = Quaternion.Euler(0f, 0f, 90f); meshScale = new Vector3(m.r * 2f, m.s.x * 0.5f, m.r * 2f);
                }
                else if (m.shape == "cyl")
                {
                    mesh = PrismMesh(m.name, m.r, m.h);
                    var collider = go.AddComponent<MeshCollider>(); collider.sharedMesh = mesh; collider.convex = true; mover.Solid = collider;
                }
                else
                {
                    mesh = BoxMesh(m.name, m.s);
                    var box = go.AddComponent<BoxCollider>(); box.size = m.s; box.center = m.offset; mover.Solid = box;
                }

                if (Dress(go, m, index, named)) modelled++;
                else
                {
                    var look = new GameObject("grey");
                    look.transform.SetParent(go.transform, false);
                    look.transform.localPosition = m.offset;
                    look.transform.localRotation = meshTurn; look.transform.localScale = meshScale;
                    look.AddComponent<MeshFilter>().sharedMesh = mesh;
                    look.AddComponent<MeshRenderer>().sharedMaterial = Grey(grey, m.kind == "chain_anchor" ? "vine" : m.kind == "log" ? "wood" : m.kind == "crumble_slab" ? "sand" : "rock");
                    if (m.motion == "pendulum" && m.arm > 0.1f)
                    {
                        // The chain, so the eye can see what it hangs from. It swings with the boulder (same object).
                        var chain = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        Object.DestroyImmediate(chain.GetComponent<Collider>());
                        chain.name = "chain"; chain.transform.SetParent(go.transform, false);
                        chain.transform.localPosition = new Vector3(0f, m.arm * 0.5f, 0f);
                        chain.transform.localScale = new Vector3(0.12f, m.arm * 0.5f, 0.12f);
                        chain.GetComponent<Renderer>().sharedMaterial = Grey(grey, "post");
                    }
                }
                all.Add(mover);
            }
            manager.Movers = all.ToArray();
            string hangers = Hangers(root, layout, named, grey);

            // ---- geysers
            var padRoot = Group(root, "Pads");
            foreach (var p in layout.pads)
            {
                if (p == null) continue;
                var go = new GameObject(p.name);
                go.transform.SetParent(padRoot, false);
                go.transform.position = p.position;
                var pad = go.AddComponent<JumpPad>();
                pad.Radius = p.radius > 0f ? p.radius : 1.2f;
                pad.LaunchSpeed = p.speed;
                pad.ArenaSound = true;
            }

            // ---- the water that is seen (the swim volume is the mode's): PaeteRunSceneBuilder.Life.cs
            string water = WaterPass(root, layout, mapLook);

            // ---- the start and finish lines, to see
            var marks = Group(root, "Marks");
            // ⚠️ Only when the art is absent (owner, 2026-10-10: "this line at the start is floating"): the art paints both
            // lines into its paving, and this box, drawn over that, was a white plank hovering on the start line.
            if (layout.checkpoints.Length > 0 && !hideGrey)
            {
                var start = layout.checkpoints[0];
                var finish = layout.checkpoints[Mathf.Clamp(layout.finish, 0, layout.checkpoints.Length - 1)];
                Line(marks, "Start line", start.p + Vector3.up * 0.01f, start.yaw, grey["line"]);
                Line(marks, "Finish line", finish.p + Vector3.up * 0.01f, finish.yaw, grey["line"]);
            }

            // ---- the mode, the launcher, the kill plane, the bounds
            var run = Group(root, "~Run");
            var mode = run.gameObject.AddComponent<RunMode>();
            mode.Layout = AssetDatabase.LoadAssetAtPath<TextAsset>(LayoutPath);
            mode.Movers = manager;
            if (mode.Layout == null) Warn("the layout is not imported as a text asset: the mode will have nothing to read");
            Group(root, "~Match").gameObject.AddComponent<MatchInstaller>();

            var b = layout.bounds;
            var centre = new Vector3((b.min_x + b.max_x) * 0.5f, 0f, (b.min_z + b.max_z) * 0.5f);
            var kill = Group(root, "KillPlane");
            kill.localPosition = new Vector3(centre.x, layout.kill_y - 10f, centre.z);
            var plane = kill.gameObject.AddComponent<KillPlane>();
            plane.Height = layout.kill_y - 10f;
            // KillPlane takes its size from a child's box (and switches that child off): the default 260 m is smaller than this course.
            var shape = Group(kill, "Shape").gameObject.AddComponent<BoxCollider>();
            shape.size = new Vector3((b.max_x - b.min_x) + 200f, KillPlane.PlaneThickness, (b.max_z - b.min_z) + 200f);

            // Four walls, far outside the course, their inner faces at the layout's bounds: `MatchInstaller.MeasurePlayableBounds`
            // reads them, and every teleport in the game is clamped to what it measures.
            var bounds = Group(root, "Bounds");
            float height = b.ceiling - b.floor, midY = (b.ceiling + b.floor) * 0.5f;
            Wall(bounds, "Limit X -", new Vector3(b.min_x - 0.5f, midY, centre.z), new Vector3(1f, height, (b.max_z - b.min_z) + 2f));
            Wall(bounds, "Limit X +", new Vector3(b.max_x + 0.5f, midY, centre.z), new Vector3(1f, height, (b.max_z - b.min_z) + 2f));
            Wall(bounds, "Limit Z -", new Vector3(centre.x, midY, b.min_z - 0.5f), new Vector3((b.max_x - b.min_x) + 2f, height, 1f));
            Wall(bounds, "Limit Z +", new Vector3(centre.x, midY, b.max_z + 0.5f), new Vector3((b.max_x - b.min_x) + 2f, height, 1f));

            // ---- what moves and what is heard, none of it gameplay: PaeteRunSceneBuilder.Life.cs
            Physics.SyncTransforms();
            string life = Life(root, layout, mapLook);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Could not save " + ScenePath);
            AssetDatabase.SaveAssets();
            string summary = $"built {ScenePath}: {boxes} solid boxes and {rounds} round solids, {all.Count} movers ({modelled} wearing a model), " +
                             $"{layout.pads.Length} geysers, {layout.water.Length} pools, {layout.checkpoints.Length} checkpoints, {placed} art pieces placed, {missing} missing; " +
                             $"{collision}; {ground}; {drawn}; {hangers}; sun yaw {_sunYawUsed:0.#} at {mapLook.SunElevation:0.#} degrees; {water}; {life}";
            Debug.Log(Tag + summary);
            return summary;
        }

        /// <summary>
        /// Puts the art's model of this mover's kind on it, fitted to the layout's size. False when there is no model.
        /// The numbers are the art's own (`mv_*` in tools/author_paete_run_art_stages.py): where each model's origin is
        /// and how big it was made.
        /// </summary>
        private static bool Dress(GameObject go, RunLayout.Mover m, int index, Dictionary<string, Material> named)
        {
            string name = "mover_" + m.kind, variant = "";
            Vector3 at = Vector3.zero, scale = Vector3.one;
            switch (m.kind)
            {
                case "boulder_chain":
                {
                    // Origin: the top of the chain. The boulder's top is 9 - 0.05 r under it. Made at r 1.5; `_b` 1.9, `_c` 1.1.
                    float made = 1.5f;
                    if (m.r <= 1.3f) { variant = "_c"; made = 1.1f; } else if (m.r >= 1.7f) { variant = "_b"; made = 1.9f; }
                    float s = m.r / made;
                    scale = Vector3.one * s; at = new Vector3(0f, m.h * 0.5f + (9f - 0.05f * made) * s, 0f);
                    break;
                }
                case "pendulum":
                {
                    // Origin: the pivot, 8.2 m over the boulder's middle. Two boulders, taken in turn.
                    if (index % 2 == 1) variant = "_b";
                    float s = m.arm > 0.1f ? m.arm / 8.2f : 1f;
                    scale = Vector3.one * s; at = new Vector3(0f, m.arm, 0f);
                    break;
                }
                case "mill_wheel": scale = new Vector3(m.r, 1f, m.r); break;                       // unit radius, 0.6 thick, origin its middle
                case "log": scale = new Vector3(m.s.x / 8.5f, m.r / 0.75f, m.r / 0.75f); break;      // 8.5 long on X, r 0.75, origin its middle
                case "seesaw": scale = new Vector3(m.s.x / 3.8f, 1f, m.s.z / 4.8f); break;           // 3.8 x 4.8, origin the axle (the mover's own)
                case "crumble_slab":
                    // 7.2 across, 3.0 along, 0.5 thick, origin the middle of the top.
                    scale = new Vector3(m.s.x / 7.2f, m.s.y / 0.5f, m.s.z / 3.0f); at = new Vector3(0f, m.s.y * 0.5f, 0f);
                    break;
                case "floating_rock":
                {
                    // Origin: the middle of the top. Made at r 1.6; `_b` 1.9. Taken in turn.
                    float made = 1.6f;
                    if (index % 2 == 1) { variant = "_b"; made = 1.9f; }
                    float s = m.r / made;
                    scale = Vector3.one * s; at = new Vector3(0f, m.h * 0.5f, 0f);
                    break;
                }
                case "chain_anchor": at = new Vector3(0f, 4.0f, 0f); break;                         // origin the chain's top; the ring is 4 m under it
                default:
                    // A kind the art has not described: a 1 x 1 x 1 m model about the origin, scaled to the mover's size.
                    scale = m.shape == "sphere" ? Vector3.one * (m.r * 2f) : m.s; at = m.offset;
                    break;
            }
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Models/{name}{variant}.glb");
            if (model == null && variant.Length > 0) model = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Models/{name}.glb");
            if (model == null) return false;
            var dressed = (GameObject)PrefabUtility.InstantiatePrefab(model, go.transform);
            dressed.transform.localPosition = at; dressed.transform.localRotation = Quaternion.identity; dressed.transform.localScale = scale;
            Rematerial(dressed, named);
            foreach (var c in dressed.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            return true;
        }

        private static RunMotion MotionOf(string name)
        {
            switch (name)
            {
                case "sway": return RunMotion.Sway;
                case "pendulum": return RunMotion.Pendulum;
                case "spin": return RunMotion.Spin;
                case "roll": return RunMotion.Roll;
                case "tilt": return RunMotion.Tilt;
                case "crumble": return RunMotion.Crumble;
            }
            Warn("a mover has the unknown motion '" + name + "': it stands still");
            return RunMotion.Still;
        }

        private static void Wall(Transform parent, string name, Vector3 centre, Vector3 size)
        {
            var box = Group(parent, name).gameObject.AddComponent<BoxCollider>();
            box.center = centre; box.size = size;
        }

        private static void Line(Transform parent, string name, Vector3 at, float yaw, Material m)
        {
            var go = new GameObject(name) { isStatic = true };
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
            go.AddComponent<MeshFilter>().sharedMesh = BoxMesh(name, new Vector3(8f, 0.03f, 0.5f));
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off;
        }

        // ------------------------------------------------------------------ meshes

        /// <summary>A box with its texture laid in METRES on every face (the prototype map's way), so the grid's squares are a metre.</summary>
        private static Mesh BoxMesh(string name, Vector3 size)
        {
            var h = size * .5f;
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            void Face(Vector3 normal, Vector3 right, Vector3 up, float w, float t, float d)
            {
                int at = v.Count;
                Vector3 c = normal * d;
                v.Add(c - right * w - up * t); v.Add(c - right * w + up * t); v.Add(c + right * w + up * t); v.Add(c + right * w - up * t);
                for (int i = 0; i < 4; i++) { n.Add(normal); uv.Add(new Vector2(Vector3.Dot(v[at + i], right), Vector3.Dot(v[at + i], up))); }
                tri.AddRange(new[] { at, at + 1, at + 2, at, at + 2, at + 3 });
            }
            Face(Vector3.up, Vector3.right, Vector3.forward, h.x, h.z, h.y);
            Face(Vector3.down, Vector3.left, Vector3.forward, h.x, h.z, h.y);
            Face(Vector3.forward, Vector3.left, Vector3.up, h.x, h.y, h.z);
            Face(Vector3.back, Vector3.right, Vector3.up, h.x, h.y, h.z);
            Face(Vector3.right, Vector3.forward, Vector3.up, h.z, h.y, h.x);
            Face(Vector3.left, Vector3.back, Vector3.up, h.z, h.y, h.x);
            var mesh = new Mesh { name = "PaeteRun " + name };
            mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetUVs(0, uv); mesh.SetTriangles(tri, 0); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>An upright prism of 24 sides about the origin: a round solid, convex, so it can be a solid collider.</summary>
        private static Mesh PrismMesh(string name, float radius, float height)
        {
            const int sides = 24;
            float half = height * 0.5f;
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            for (int cap = 0; cap < 2; cap++)
            {
                float y = cap == 0 ? half : -half;
                Vector3 normal = cap == 0 ? Vector3.up : Vector3.down;
                int centre = v.Count;
                v.Add(new Vector3(0f, y, 0f)); n.Add(normal); uv.Add(Vector2.zero);
                for (int i = 0; i < sides; i++)
                {
                    float a = i * Mathf.PI * 2f / sides;
                    var p = new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius);
                    v.Add(p); n.Add(normal); uv.Add(new Vector2(p.x, p.z));
                }
                for (int i = 0; i < sides; i++)
                {
                    int a = centre + 1 + i, b = centre + 1 + (i + 1) % sides;
                    if (cap == 0) tri.AddRange(new[] { centre, b, a }); else tri.AddRange(new[] { centre, a, b });
                }
            }
            float around = Mathf.PI * 2f * radius;
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
                var p0 = new Vector3(Mathf.Cos(a0) * radius, 0f, Mathf.Sin(a0) * radius);
                var p1 = new Vector3(Mathf.Cos(a1) * radius, 0f, Mathf.Sin(a1) * radius);
                var normal = ((p0 + p1) * 0.5f).normalized;
                int at = v.Count;
                v.Add(p0 + Vector3.down * half); v.Add(p0 + Vector3.up * half); v.Add(p1 + Vector3.up * half); v.Add(p1 + Vector3.down * half);
                for (int k = 0; k < 4; k++) n.Add(normal);
                float u0 = around * i / sides, u1 = around * (i + 1) / sides;
                uv.Add(new Vector2(u0, -half)); uv.Add(new Vector2(u0, half)); uv.Add(new Vector2(u1, half)); uv.Add(new Vector2(u1, -half));
                tri.AddRange(new[] { at, at + 1, at + 2, at, at + 2, at + 3 });
            }
            var mesh = new Mesh { name = "PaeteRun " + name };
            mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetUVs(0, uv); mesh.SetTriangles(tri, 0); mesh.RecalculateBounds();
            return mesh;
        }

        // ------------------------------------------------------------------ materials

        /// <summary>The grey blockout's colours, by kind. The prototype map's grid in metres, tinted; on the map's own shader with the near band shut.</summary>
        private static Dictionary<string, Material> GreyMaterials()
        {
            string folder = Root + "/Materials";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(Root, "Materials");
            var grid = AssetDatabase.LoadAssetAtPath<Texture2D>(GridTexture);
            if (grid == null) Warn("the prototype grid texture is missing (" + GridTexture + "): the grey is flat colour");
            var fade = Shader.Find(NearFade.ShaderName);
            var standard = Shader.Find("Standard");
            var colours = new Dictionary<string, Color>
            {
                { "sand", new Color(0.90f, 0.80f, 0.58f) }, { "teal", new Color(0.30f, 0.74f, 0.72f) }, { "rock", new Color(0.66f, 0.63f, 0.60f) },
                { "wood", new Color(0.74f, 0.56f, 0.36f) }, { "vine", new Color(0.58f, 0.76f, 0.28f) }, { "post", new Color(0.46f, 0.47f, 0.52f) },
                { "basin", new Color(0.58f, 0.66f, 0.70f) }, { "line", new Color(0.98f, 0.98f, 0.96f) },
            };
            var result = new Dictionary<string, Material>();
            foreach (var pair in colours)
            {
                string path = $"{folder}/blockout_{pair.Key}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null) { m = new Material(fade != null ? fade : standard); AssetDatabase.CreateAsset(m, path); }
                m.shader = fade != null ? fade : standard;
                m.mainTexture = pair.Key == "line" ? null : grid;
                m.mainTextureScale = new Vector2(.5f, .5f);
                m.color = pair.Value;
                m.SetFloat("_Glossiness", 0.05f);
                if (fade != null) { m.SetFloat("_NearFadeStart", 0.001f); m.SetFloat("_NearFadeEnd", 0f); }
                EditorUtility.SetDirty(m);
                result[pair.Key] = m;
            }
            // The water: a plain see-through blue sheet. The art side has the game's water shaders to put here.
            string waterPath = folder + "/blockout_water.mat";
            var water = AssetDatabase.LoadAssetAtPath<Material>(waterPath);
            if (water == null) { water = new Material(standard); AssetDatabase.CreateAsset(water, waterPath); }
            water.shader = standard;
            water.SetFloat("_Mode", 3f);
            water.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha); water.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha); water.SetInt("_ZWrite", 0);
            water.DisableKeyword("_ALPHATEST_ON"); water.EnableKeyword("_ALPHABLEND_ON"); water.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            water.renderQueue = (int)RenderQueue.Transparent;
            water.color = new Color(0.20f, 0.62f, 0.86f, 0.62f);
            water.SetFloat("_Glossiness", 0.75f);
            EditorUtility.SetDirty(water);
            result["water"] = water;
            return result;
        }

        /// <summary>
        /// ⚠️ NO GREY (owner, 2026-10-10, standing on the start line on a tinted prototype grid: "fix the grey showing
        /// through the art", "remove the placeholder blocks"). The solids are the collision and were also DRAWN, as the
        /// blockout's checker in a colour by kind, on top of the art. Two things replace that. Here: every kind wears one
        /// of the ART'S OWN materials (paving for a deck, worn stone for a hop, wet stone for a slide's bed, ashlar for a
        /// post, timber for a beam), so a solid the art does not cover is still part of the place.
        /// `HideWhatTheArtCovers` then stops drawing each solid the art already draws a surface on.
        /// </summary>
        private static void Dress(Dictionary<string, Material> grey, Dictionary<string, Material> named)
        {
            var wear = new Dictionary<string, string>
            {
                { "sand", "pr_paving" }, { "teal", "pr_stone_wet" }, { "rock", "pr_stone_worn" }, { "wood", "pr_timber" },
                { "vine", "pr_timber" }, { "post", "pr_ashlar_dark" }, { "basin", "pr_stone" }, { "line", "pr_paint_white" },
            };
            foreach (var pair in wear)
            {
                if (named != null && named.TryGetValue(pair.Value, out var m) && m != null) grey[pair.Key] = m;
                else Warn("the art has no material named " + pair.Value + ": the " + pair.Key + " solids keep the blockout's grid");
            }
        }

        /// <summary>
        /// Stops drawing every solid whose TOP the art already covers: rays are cast down onto the art's own meshes at
        /// five points of the solid's upper face, and if at least four meet art within 12 cm of that face, the solid's
        /// renderer is switched off (its collider stays: the collision is never touched). A solid the art does not
        /// cover stays drawn, in the art's stone (`Dress`), so there is never a floor a racer cannot see. The art is
        /// given mesh colliders for the length of this test only.
        /// </summary>
        private static int HideWhatTheArtCovers(Transform root, Transform solids)
        {
            var dressing = root.Find("Dressing");
            if (dressing == null) return 0;
            var temporary = new List<MeshCollider>();
            var isArt = new HashSet<Collider>();
            foreach (var filter in dressing.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null || filter.GetComponent<Collider>() != null) continue;
                var collider = filter.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
                temporary.Add(collider); isArt.Add(collider);
            }
            Physics.SyncTransforms();
            int hidden = 0;
            var hits = new RaycastHit[32];
            foreach (Transform solid in solids)
            {
                var renderer = solid.GetComponent<MeshRenderer>();
                var filter = solid.GetComponent<MeshFilter>();
                if (renderer == null || filter == null || filter.sharedMesh == null) continue;
                Bounds b = filter.sharedMesh.bounds;
                int met = 0;
                for (int i = 0; i < 5; i++)
                {
                    float fx = i == 1 || i == 2 ? -0.6f : i == 0 ? 0f : 0.6f, fz = i == 1 || i == 3 ? -0.6f : i == 0 ? 0f : 0.6f;
                    Vector3 top = solid.TransformPoint(new Vector3(b.center.x + b.extents.x * fx, b.max.y, b.center.z + b.extents.z * fz));
                    int count = Physics.RaycastNonAlloc(top + Vector3.up * 0.6f, Vector3.down, hits, 0.9f, ~0, QueryTriggerInteraction.Ignore);
                    for (int k = 0; k < count; k++)
                        if (isArt.Contains(hits[k].collider) && Mathf.Abs(hits[k].point.y - top.y) <= 0.12f) { met++; break; }
                }
                if (met >= 4) { renderer.enabled = false; hidden++; }
            }
            foreach (var collider in temporary) Object.DestroyImmediate(collider);
            Physics.SyncTransforms();
            return hidden;
        }

        private static Material Grey(Dictionary<string, Material> grey, string kind)
            => grey.TryGetValue(kind ?? "", out var m) ? m : grey["rock"];

        /// <summary>Every `materials*.json` in the map's folder, in name order (the Eskinita builder's rule; later wins).</summary>
        private static List<MatSpec> MergeMaterials()
        {
            var merged = new Dictionary<string, MatSpec>();
            var order = new List<string>();
            if (!Directory.Exists(Root)) return new List<MatSpec>();
            var files = Directory.GetFiles(Root, "materials*.json").Select(f => f.Replace('\\', '/')).OrderBy(f => f, StringComparer.Ordinal);
            foreach (string file in files)
            {
                MatFile parsed = null;
                try { parsed = JsonUtility.FromJson<MatFile>(File.ReadAllText(file)); }
                catch (Exception e) { Warn("unreadable " + Path.GetFileName(file) + ": " + e.Message); }
                if (parsed == null || parsed.materials == null) continue;
                foreach (var s in parsed.materials)
                {
                    if (s == null || string.IsNullOrEmpty(s.name)) { Warn("a material in " + Path.GetFileName(file) + " has no name"); continue; }
                    if (!merged.ContainsKey(s.name)) order.Add(s.name);
                    merged[s.name] = s;
                }
            }
            return order.Select(n => merged[n]).ToList();
        }

        private static Dictionary<string, Material> BuildMaterials(List<MatSpec> specs)
        {
            var result = new Dictionary<string, Material>();
            if (specs.Count == 0) return result;
            var folder = Root + "/Materials";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(Root, "Materials");
            var standard = Shader.Find("Standard");
            var fade = Shader.Find(NearFade.ShaderName);
            var foliage = Shader.Find("TumbangPreso/KantoFoliage");
            var painted = Shader.Find("TumbangPreso/LagoonPainted");
            if (fade == null) throw new InvalidOperationException("Missing shader " + NearFade.ShaderName);
            foreach (var s in specs)
            {
                string path = $"{folder}/{s.name}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null) { m = new Material(fade); AssetDatabase.CreateAsset(m, path); }
                bool cut = (s.foliage || s.cutout) && foliage != null;
                // A hung cloth is seen from both faces: the painted shader, which can be two-sided (`_Cull` off).
                bool cloth = !cut && !s.emissive && painted != null && s.name.StartsWith("pr_cloth_", StringComparison.Ordinal);
                m.shader = cut ? foliage : s.emissive ? standard : cloth ? painted : fade;
                var tint = s.tint != null && s.tint.Length >= 3 ? new Color(s.tint[0], s.tint[1], s.tint[2], 1) : Color.white;
                float tiling = s.tiling > 0f ? s.tiling : 1f;
                m.SetColor("_Color", tint);
                Texture2D albedo = null, normal = null;
                if (!string.IsNullOrEmpty(s.texture))
                {
                    albedo = LoadTexture($"{Root}/Textures/{s.texture}_albedo.png", false, cut);
                    if (albedo == null) Warn($"missing texture Textures/{s.texture}_albedo.png (material '{s.name}' is drawn as its flat tint)");
                    if (!cut) normal = LoadTexture($"{Root}/Textures/{s.texture}_normal.png", true, false);
                }
                m.SetTexture("_MainTex", albedo);
                m.SetTextureScale("_MainTex", Vector2.one * tiling);
                m.SetTextureOffset("_MainTex", Vector2.zero);
                if (m.HasProperty("_BumpMap"))
                {
                    m.SetTexture("_BumpMap", normal);
                    m.SetTextureScale("_BumpMap", Vector2.one * tiling);
                    m.SetFloat("_BumpScale", 1f);
                    if (normal != null) m.EnableKeyword("_NORMALMAP"); else m.DisableKeyword("_NORMALMAP");
                }
                m.SetFloat("_Glossiness", s.glossy ? 0.6f : 0.08f);
                if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
                if (cut)
                {
                    m.SetFloat("_Cutoff", 0.5f);
                    m.SetFloat("_LeafFill", s.foliage ? 0.28f : 0f);
                    m.renderQueue = (int)RenderQueue.AlphaTest;
                }
                else if (s.emissive)
                {
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", tint * 0.8f);
                    m.SetTexture("_EmissionMap", albedo);
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                    m.renderQueue = -1;
                }
                else
                {
                    // The near band is shut, as on Eskinita: a racer is always beside something.
                    m.SetFloat("_NearFadeStart", 0.001f);
                    m.SetFloat("_NearFadeEnd", 0f);
                    m.SetFloat("_NearFadeCell", NearFade.DitherCellPixels);
                    m.SetFloat("_SurfaceKind", 0f);
                    m.SetFloat("_SurfaceVertexRoles", 0f);
                    m.SetFloat("_SurfaceCoordinates", 0f);
                    m.SetFloat("_SurfaceDebug", 0f);
                    m.DisableKeyword("_EMISSION");
                    m.renderQueue = -1;
                }
                if (cloth) { m.SetFloat("_Cull", (float)CullMode.Off); m.renderQueue = -1; }
                EditorUtility.SetDirty(m);
                result[s.name] = m;
            }
            return result;
        }

        private static Texture2D LoadTexture(string path, bool normal, bool cutout)
        {
            if (!File.Exists(path)) return null;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) return null;
            }
            bool dirty = false;
            var type = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (importer.textureType != type) { importer.textureType = type; dirty = true; }
            if (!normal && !importer.sRGBTexture) { importer.sRGBTexture = true; dirty = true; }
            var wrap = cutout ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            if (importer.wrapMode != wrap) { importer.wrapMode = wrap; dirty = true; }
            if (cutout && !importer.alphaIsTransparency) { importer.alphaIsTransparency = true; dirty = true; }
            if (importer.anisoLevel != 4) { importer.anisoLevel = 4; dirty = true; }
            if (!importer.mipmapEnabled) { importer.mipmapEnabled = true; dirty = true; }
            if (importer.maxTextureSize != 1024) { importer.maxTextureSize = 1024; dirty = true; }
            if (dirty) importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void Rematerial(GameObject go, Dictionary<string, Material> materials)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) { Warn($"a renderer in {go.name} ('{r.name}') has an empty material slot"); continue; }
                    string key = mats[i].name.Replace(" (Instance)", "").Trim();
                    if (materials.TryGetValue(key, out var m)) mats[i] = m;
                    else Warn($"no material spec for '{key}' (first seen on {go.name}); it keeps the importer's material");
                }
                r.sharedMaterials = mats;
            }
        }

        // ------------------------------------------------------------------ light and sky

        /// <summary>The map's look row; the code's own if the profile asset has not been given one yet.</summary>
        private static WorldLookProfile.MapLook Look()
        {
            var look = WorldLookProfile.Current.Maps.FirstOrDefault(m => m.Map == MapName);
            if (look != null) return look;
            Warn("Resources/WorldLookProfile.asset has no '" + MapName + "' row; the scene is lit from the code's row and PLAY WILL WEAR NO LOOK until the asset has it");
            var fresh = ScriptableObject.CreateInstance<WorldLookProfile>();
            look = fresh.Maps.FirstOrDefault(m => m.Map == MapName);
            Object.DestroyImmediate(fresh);
            if (look == null) throw new InvalidOperationException("WorldLookProfile has no row named " + MapName);
            return look;
        }

        /// <summary>The yaw the last build gave the sun (for its summary line).</summary>
        private static float _sunYawUsed;

        /// <summary>
        /// A GOLDEN HOUR IN A DESERT BOWL, and the same numbers Play wears (owner, 2026-10-10: "theres no WorldLook").
        /// The scene's sun and RenderSettings are written from the "PaeteRun" row of `WorldLookProfile`, as the
        /// Eskinita builder does, so the editor view and a race agree: in Play `WorldLookPresentation` finds the row
        /// by the scene's name, lerps to it and adds the blocky clouds, and the review installs it the same way. The
        /// sky is the game's own (`TumbangPreso/NeighbourhoodSky`) in a material of this map's,
        /// Art/MapAtmosphere/PaeteRunSky.mat, made by `MapAtmosphereAuthor.Apply`.
        ///
        /// ⚠️ THE YAW IS THIS BUILDER'S (<see cref="DefaultSunYaw"/>), the elevation is the row's. In Play the look
        /// moves a sun's elevation and never its azimuth, so the yaw written here is the one a race is lit from.
        /// </summary>
        private static WorldLookProfile.MapLook Lighting(Transform root)
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(root, false);
            sun.type = LightType.Directional;
            MapAtmosphereAuthor.Apply(MapName);
            var look = Look();
            float elevation = look.SunElevation > 0 ? look.SunElevation : 34f;
            _sunYawUsed = float.IsNaN(_sunYawOnce) ? DefaultSunYaw : _sunYawOnce;
            sun.transform.rotation = Quaternion.Euler(elevation, _sunYawUsed, 0f);
            sun.color = look.Sun; sun.intensity = look.SunIntensity;
            sun.shadows = LightShadows.Soft; sun.shadowStrength = look.ShadowStrength;
            sun.shadowBias = .03f; sun.shadowNormalBias = .2f;
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = look.Sky; RenderSettings.ambientEquatorColor = look.Equator;
            RenderSettings.ambientGroundColor = look.Ground;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = look.Fog; RenderSettings.fogStartDistance = look.FogStart; RenderSettings.fogEndDistance = look.FogEnd;
            var sky = RenderSettings.skybox;
            if (sky != null)
            {
                sky.SetColor("_Zenith", look.Zenith); sky.SetColor("_Horizon", look.Horizon);
                sky.SetColor("_Ground", new Color(0.66f, 0.54f, 0.40f));
                sky.SetColor("_CloudLight", look.CloudLight); sky.SetColor("_CloudShade", look.CloudShade);
                sky.SetColor("_SunColor", look.Sun); sky.SetVector("_SunDirection", -sun.transform.forward);
                EditorUtility.SetDirty(sky);
            }
            else Warn("no sky material was made (MapAtmosphereAuthor): the scene has no skybox");
            // `MapAtmosphereAuthor.Apply` sets a grade of its own: this map's is a little contrast and a saturation lift.
            root.GetComponent<MapGrade>().Set(1.02f, 1.03f, 1.08f, 1, 1.9f);
            return look;
        }

        private static Transform Group(Transform parent, string name)
        { var t = new GameObject(name).transform; t.SetParent(parent, false); return t; }

        // ------------------------------------------------------------------ review renders

        private static string NextReviewFolder()
        {
            int v = 1; while (Directory.Exists("Logs/paete-run/unity/v" + v)) v++; return "Logs/paete-run/unity/v" + v;
        }

        /// <summary>
        /// Opens the built scene and renders, at 1600 x 900 to `folder/paete_run_NAME.png`: each shot of the layout's
        /// "review" list, then this builder's own: the stepped face from the south (`look_face`), each pool
        /// (`water_NAME`), each slide from its top and its foot (`slide_NAME_top`, `_foot`), six places where solids
        /// overlap at one height (`junction_N`, the same six whichever way the solids are drawn), the first deck of
        /// every leg from close over it (`deck_legNN`), each pendulum and what it hangs from (`hang_NAME`), and the life posed
        /// once (`life_lizard`, `life_wisp`, `life_geyser`, `life_lantern`, `life_splash`).
        ///
        /// The witness camera wears what a race's camera wears: the scene's grade, the world outline with this map's
        /// ink distances, the depth texture the water reads, and the "PaeteRun" look installed the way the map preview
        /// installs it (the key light, the coloured shade, the haze and the blocky clouds). A shot's `fov` is
        /// HORIZONTAL, degrees. ⚠️ EDIT MODE: the movers stand at their rest places, the kites are not there (they are
        /// made when a race starts), and the wisps, the lizards and the plumes are POSED ONCE, not moving.
        /// </summary>
        public static string Review(string output)
        {
            if (!File.Exists(ScenePath)) throw new FileNotFoundException("The scene is not built yet: " + ScenePath);
            var layout = ReadLayout();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Directory.CreateDirectory(output);
            var sceneRoot = GameObject.Find(MapName);
            var life = sceneRoot != null ? sceneRoot.GetComponentInChildren<RunLife>() : null;

            var shots = new List<(string name, Vector3 at, Vector3 look, float fov)>();
            foreach (var s in layout.review ?? Array.Empty<RunLayout.Shot>())
                if (s != null) shots.Add((s.name, s.at, s.look, s.fov > 0f ? s.fov : 80f));
            var b = layout.bounds;
            var middle = new Vector3((b.min_x + b.max_x) * 0.5f, 20f, (b.min_z + b.max_z) * 0.5f);
            shots.Add(("look_face", middle + new Vector3(10f, 22f, 185f), middle, 70f));
            foreach (var w in layout.water)
            {
                if (w == null) continue;
                var turn = Quaternion.Euler(0f, w.yaw, 0f);
                var centre = new Vector3(w.c.x, w.surface, w.c.z);
                shots.Add(("water_" + w.name, centre + turn * new Vector3(w.s.x * 0.5f + 2.5f, 4.2f, -w.s.z * 0.3f), centre + turn * new Vector3(0f, 0f, w.s.z * 0.1f), 85f));
            }
            foreach (var chute in layout.chutes ?? Array.Empty<RunLayout.Chute>())
            {
                if (chute == null || chute.points == null || chute.points.Length < 3) continue;
                var p = chute.points; int n = p.Length;
                Vector3 down = (p[2] - p[0]).normalized, back = (p[n - 3] - p[n - 1]).normalized;
                shots.Add(("slide_" + chute.name + "_top", p[0] - down * 5f + Vector3.up * 4.5f, p[Mathf.Min(n - 1, 6)], 85f));
                shots.Add(("slide_" + chute.name + "_foot", p[n - 1] - back * 9f + Vector3.up * 4f, p[n - 1] + back * 4f, 85f));
            }
            foreach (var m in layout.movers ?? Array.Empty<RunLayout.Mover>())
                if (m != null && m.motion == "pendulum")
                {
                    // From along the course, level with the pivot: the chain, what it hangs from, and the swing's plane.
                    Vector3 along = Vector3.Cross(m.axis.sqrMagnitude > 1e-6f ? m.axis.normalized : Vector3.right, Vector3.up);
                    shots.Add(("hang_" + m.name, m.pivot + along * 11f + Vector3.down * 1.5f, m.pivot + Vector3.down * 2.5f, 80f));
                }
            // Every leg's first deck from close over it, looking down the course: where a drawn solid, the art's deck and
            // the ground meet (owner, 2026-10-10: paving flickering through paving on legs 6, 7 and 9).
            foreach (var leg in layout.legs ?? Array.Empty<RunLayout.LegInfo>())
            {
                Vector3 way = leg.c - leg.a; way.y = 0f;
                if (way.sqrMagnitude < 1f) continue;
                way.Normalize();
                shots.Add(($"deck_leg{leg.n:00}", leg.a - way * 6.5f + Vector3.up * 5.5f, leg.a + way * 3f, 85f));
            }
            var junctions = Junctions(layout, 6);
            for (int i = 0; i < junctions.Count; i++)
                shots.Add(("junction_" + (i + 1), junctions[i].at + new Vector3(3.4f, 4.4f, 3.4f), junctions[i].at, 70f));
            if (life != null)
            {
                if (life.LizardSpots.Length > 0) shots.Add(("life_lizard", life.LizardSpots[0] + new Vector3(1.5f, 1.0f, 1.5f), life.LizardSpots[0] + Vector3.up * 0.15f, 60f));
                if (life.Lanes.Length > 0)
                {
                    var lane = life.Lanes[0]; Vector3 mid = (lane.From + lane.To) * 0.5f, along = (lane.To - lane.From).normalized;
                    shots.Add(("life_wisp", mid + Vector3.Cross(Vector3.up, along) * -11f + Vector3.up * 2.5f, mid, 80f));
                }
                if (life.Geysers.Length > 0) shots.Add(("life_geyser", life.Geysers[0] + new Vector3(4f, 2.4f, 4f), life.Geysers[0] + Vector3.up * 0.8f, 70f));
                for (int i = 0; i < life.LanternAt.Length; i++)
                    if (life.LanternAt[i].y > -9000f) { shots.Add(("life_lantern", life.LanternAt[i] + new Vector3(2.4f, 0.4f, 2.4f), life.LanternAt[i], 60f)); break; }
                if (life.SlideEnds.Length > 0) shots.Add(("life_splash", life.SlideEnds[0] + new Vector3(5f, 3f, 5f), life.SlideEnds[0] + Vector3.up * 0.5f, 70f));
            }

            var camera = new GameObject("Paete Run review witness").AddComponent<Camera>();
            camera.enabled = false; camera.nearClipPlane = 0.1f; camera.farClipPlane = 2500f;
            // The water reads the depth texture; an offscreen Render() decides its passes before the water can ask.
            camera.depthTextureMode |= DepthTextureMode.Depth;
            camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            var outline = camera.gameObject.AddComponent<WorldOutline>();
            outline.PrototypeEnabled = true;
            var range = sceneRoot != null ? sceneRoot.GetComponent<MapCameraRange>() : null;
            if (range != null && range.InkFadeStart >= 0f && range.InkFadeEnd > range.InkFadeStart) outline.SetFade(range.InkFadeStart, range.InkFadeEnd);
            WorldLookPresentation worn = null;
            GameObject posed = null;
            int written = 0;
            try
            {
                var sun = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);
                try
                {
                    camera.gameObject.AddComponent<WorldLookCamera>();
                    if (sceneRoot != null) worn = WorldLookPresentation.InstallPreview(sceneRoot.transform, 0f, sun);
                    if (worn == null) Warn("the review wears NO world look (no '" + MapName + "' row found): no blocky clouds, the scene's own light only");
                }
                catch (Exception e) { Warn("the world look preview failed: " + e.Message); }
                Physics.SyncTransforms();

                foreach (var s in shots)
                {
                    var forward = s.look - s.at;
                    if (forward.sqrMagnitude < 1e-6f) { Warn($"review shot '{s.name}' looks at its own position"); continue; }
                    var up = Mathf.Abs(Vector3.Dot(forward.normalized, Vector3.up)) > 0.999f ? Vector3.forward : Vector3.up;
                    camera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(s.fov, 16f / 9f);
                    camera.transform.SetPositionAndRotation(s.at, Quaternion.LookRotation(forward, up));
                    if (life != null)
                    {
                        // Posed for this camera (the soft shapes turn to face it).
                        try { posed = life.ReviewPose(3.3f, camera); }
                        catch (Exception e) { Warn("the life could not be posed for the review: " + e.Message); life = null; }
                    }
                    var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 4 };
                    rt.Create(); camera.targetTexture = rt; camera.Render();
                    var previous = RenderTexture.active;
                    var display = RenderTexture.GetTemporary(1600, 900, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                    bool write = GL.sRGBWrite; GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
                    Graphics.Blit(rt, display); GL.sRGBWrite = write; RenderTexture.active = display;
                    var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                    image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
                    File.WriteAllBytes(Path.Combine(output, "paete_run_" + s.name + ".png"), image.EncodeToPNG());
                    RenderTexture.active = previous; camera.targetTexture = null;
                    RenderTexture.ReleaseTemporary(display); rt.Release();
                    Object.DestroyImmediate(rt); Object.DestroyImmediate(image);
                    written++;
                }
            }
            finally
            {
                if (posed != null) Object.DestroyImmediate(posed);
                if (worn != null) Object.DestroyImmediate(worn.gameObject);
                Object.DestroyImmediate(camera.gameObject);
            }
            string summary = $"{written} review renders in {output} ({JunctionCount} pairs of solids overlap with level tops at one height; {junctions.Count} of those places are the junction shots: " +
                             string.Join(", ", junctions.Select((j, i) => "junction_" + (i + 1) + " " + j.name)) + ")";
            Debug.Log(Tag + summary);
            return summary;
        }

        /// <summary>Runs a request's words (not `probe`, which the watcher hands to the play probe). Throws on anything it cannot do.</summary>
        internal static string Run(string request)
        {
            var words = request.ToLowerInvariant().Split(new[] { ' ', '\t', '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries);
            bool build = words.Contains("build"), review = words.Contains("review");
            if (!build && !review) throw new InvalidOperationException("unknown request '" + request.Trim() + "' (write build, review, probe, or several of them)");
            _sunYawOnce = float.NaN; _unmergedOnce = words.Contains("unmerged");
            foreach (string word in words)
                if (word.StartsWith("sun=", StringComparison.Ordinal) && float.TryParse(word.Substring(4), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float yaw))
                    _sunYawOnce = yaw;
            var said = new List<string>();
            var warnings = new List<string>();
            try
            {
                if (build) { said.Add(Build()); warnings.AddRange(Warnings); }
                if (review) { Warnings.Clear(); said.Add(Review(NextReviewFolder())); warnings.AddRange(Warnings.Where(w => !warnings.Contains(w))); }
            }
            finally { _sunYawOnce = float.NaN; _unmergedOnce = false; }
            string result = string.Join("; ", said);
            if (warnings.Count > 0) result += "\n" + warnings.Count + " warning(s):\n- " + string.Join("\n- ", warnings);
            return result;
        }
    }

    /// <summary>
    /// HOW A SHELL DRIVES THE OPEN EDITOR for Paete's run: write `build`, `review`, `probe` (or several; a number after
    /// `probe` is its cap in seconds) into `Temp/paete-run.request`. About once a second, when the editor is not in
    /// Play and not compiling or importing, this takes the request, refreshes the asset database, runs it and writes
    /// `Temp/paete-run.done`: `OK what was done` or `FAIL why`. The Eskinita watcher's rules, to the letter:
    ///  * if any open scene has unsaved changes NOTHING is run and the answer is `FAIL scene dirty`;
    ///  * the scenes that were open are reopened afterwards;
    ///  * a refresh that starts a compile leaves the request for after the reload;
    ///  * ⚠️ IT NEVER FIGHTS THE ESKINITA WATCHER: while `Temp/eskinita-alley.request` is waiting, or that map's probe is
    ///    running, this one waits its turn.
    /// </summary>
    [InitializeOnLoad]
    internal static class PaeteRunRequestWatcher
    {
        private const string Request = "Temp/paete-run.request", Done = "Temp/paete-run.done", Other = "Temp/eskinita-alley.request";
        private static double _nextPoll;

        static PaeteRunRequestWatcher() { EditorApplication.update += Poll; }

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < _nextPoll) return;
            _nextPoll = EditorApplication.timeSinceStartup + 1.0;
            if (!File.Exists(Request)) return;
            if (PaeteRunPlayProbe.Busy || EskinitaAlleyPlayProbe.Busy || File.Exists(Other)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;

            string result;
            try
            {
                AssetDatabase.Refresh();
                if (EditorApplication.isCompiling) return;
                string text = File.ReadAllText(Request);
                File.Delete(Request);
                if (File.Exists(Done)) File.Delete(Done);
                var words = text.ToLowerInvariant().Split(new[] { ' ', '\t', '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Contains("probe"))
                {
                    var rest = words.Where(w => w != "probe" && !int.TryParse(w, out _)).ToArray();
                    int budget = words.Select(w => int.TryParse(w, out int n) ? n : 0).FirstOrDefault(n => n > 0);
                    string before = rest.Length > 0 ? Guarded(string.Join(" ", rest)) : "";
                    PaeteRunPlayProbe.Begin(before, budget > 0 ? budget : PaeteRunPlayProbe.DefaultBudget);
                    return;
                }
                result = "OK " + Guarded(text);
            }
            catch (Exception e)
            {
                try { if (File.Exists(Request)) File.Delete(Request); } catch { /* answered below either way */ }
                result = "FAIL " + e;
            }
            try { File.WriteAllText(Done, result); }
            catch (Exception e) { Debug.LogError("[PaeteRun] could not write " + Done + ": " + e.Message); }
            if (result.StartsWith("OK")) Debug.Log("[PaeteRun] request: " + result);
            else Debug.LogError("[PaeteRun] request: " + result);
        }

        private static string Guarded(string text)
        {
            var open = new List<string>();
            string active = SceneManager.GetActiveScene().path;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isDirty) throw new InvalidOperationException("scene dirty (" + (string.IsNullOrEmpty(scene.name) ? "Untitled" : scene.name) + " has unsaved changes; nothing was run)");
                if (scene.isLoaded && !string.IsNullOrEmpty(scene.path)) open.Add(scene.path);
            }
            try { return PaeteRunSceneBuilder.Run(text); }
            finally { Restore(open, active); }
        }

        internal static void Restore(List<string> open, string active)
        {
            try
            {
                open = open.Where(File.Exists).ToList();
                if (open.Count == 0) { EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single); return; }
                EditorSceneManager.OpenScene(open[0], OpenSceneMode.Single);
                for (int i = 1; i < open.Count; i++) EditorSceneManager.OpenScene(open[i], OpenSceneMode.Additive);
                var wanted = SceneManager.GetSceneByPath(active);
                if (wanted.IsValid() && wanted.isLoaded) SceneManager.SetActiveScene(wanted);
            }
            catch (Exception e) { Debug.LogWarning("[PaeteRun] could not reopen the scenes that were open: " + e.Message); }
        }
    }
}
