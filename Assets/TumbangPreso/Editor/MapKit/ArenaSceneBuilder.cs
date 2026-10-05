using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using Newtonsoft.Json.Linq;
using TumbangPreso.Map;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using Shape = TumbangPreso.Map.ArenaStage.Shape;
using ShapeKind = TumbangPreso.Map.ArenaStage.ShapeKind;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// The Arena map (docs/ARENA_MAP_BRIEF.md, docs/ARENA_ART_BRIEF.md): a stadium floating in
    /// the night sky, whose stage of round platforms hovers over an open shaft and rearranges
    /// between rounds. Players only ever stand on the stage.
    ///
    /// THE STAGE COMES FROM tools/arena_layouts.json (written by tools/author_arena_layouts.py,
    /// which the Blender stage kit reads too, so the two cannot drift). Unity coordinates,
    /// metres, the can at the origin; a bearing is degrees clockwise from +z. Per layout: its
    /// pieces (disc, ring, arc, ramp), jump pads, speed pads and stamina pickups, the can's
    /// height, and the ids only a jump pad reaches (`bonus`). A piece keeps its id across the
    /// layouts it is in; an id a layout does not name sinks into the shaft. Any number of layouts.
    ///
    /// ⚠️ COLLIDERS ARE EXACT MESHES, ONE PER PIECE PER LAYOUT (`ArenaStageMesh`): a closed prism
    /// whose curved edges stay within 2 cm of the true arc, saved under Art/Arena/Colliders and
    /// worn by a non-convex MeshCollider on an object with no Rigidbody. Not boxes: a row of
    /// boxes along an arc leaves a stepped edge a body snags on. Not convex pieces per segment
    /// either: forty hulls a ring, each seam an edge a slipper can catch. One mesh has one flat
    /// top with no seam in it. Each layout's set sits under its own object and `ArenaStage`
    /// switches the sets.
    ///
    /// VISUALS. Each piece in each layout is `stage_LAYOUT_ID`: the art kit's model of that name
    /// under Art/Arena/Stage if there is one, else a plain mesh from the same numbers (top,
    /// sides and underside as three submeshes). Each has a twin in the hologram material.
    ///
    /// WHAT IS FIXED IN EVERY LAYOUT: the `Bounds` walls (inner faces at +/- wallHalf, read once
    /// by `MatchInstaller.MeasurePlayableBounds`), and solid floor under the can and under the
    /// spawn marks. The second is CHECKED here and the build refuses a layout that breaks it.
    /// The stage's colliders are NOT under `Bounds`: everything under that object is measured
    /// as a wall.
    ///
    /// THE ART IS OPTIONAL. Art/Arena/arena_layout.json (the Ilalim format, `ArenaArtPlacer`),
    /// tools/arena_lights.json, tools/arena_traffic.json and the crowd builder are each used if
    /// they are there. With none of them this still builds a playable scene: the stage, a grey
    /// field round the shaft, the night look.
    ///
    /// Menu: Tumbang Preso/Sample Map. Batch: TumbangPreso.EditorTools.MapKit.ArenaSceneBuilder.Run
    /// </summary>
    public static class ArenaSceneBuilder
    {
        public const string SceneName = "Arena";
        public const string ScenePath = "Assets/TumbangPreso/Scenes/Maps/" + SceneName + ".unity";
        public const string LayoutPath = "tools/arena_layouts.json";
        public const string LightsPath = "tools/arena_lights.json";
        public const string TrafficPath = "tools/arena_traffic.json";
        public const string ColliderFolder = ArenaArtPlacer.Root + "/Colliders";
        public const string GeneratedFolder = ArenaArtPlacer.Root + "/StageGenerated";
        /// <summary>Where the art kit's own stage models are looked for, by name.</summary>
        public const string StageFolder = "Stage";
        private const string MaterialFolder = ArenaArtPlacer.Root + "/Materials";
        private const string CrowdBuilderType = "TumbangPreso.EditorTools.MapKit.ArenaCrowdBuilder";
        private const string Tag = "[Arena] ";

        /// <summary>Upward launch in m/s unless a pad names its own. At `Balance.Gravity` 20 the
        /// apex is v*v/40: 15.5 gives 6.0 m, under the 12 m ceiling.</summary>
        private const float JumpPadSpeed = 15.5f;
        private const float WallThickness = 0.4f;
        /// <summary>The walls' top above the stage unless the data names `wallHeight`: the slipper's ceiling.</summary>
        private const float WallHeight = 12.0f;
        /// <summary>The grey field that stands in for the bowl when there is no art: the turf's width.</summary>
        private const float FieldWidth = 45.0f, ShaftDepth = 40.0f;

        // Flat colours for a stage with no art yet. Nothing near the team hues #f87020 and #0080e8.
        private static readonly Color TopColour = new Color(0.70f, 0.71f, 0.74f);
        private static readonly Color SideColour = new Color(0.36f, 0.38f, 0.44f);
        private static readonly Color UnderColour = new Color(0.16f, 0.17f, 0.21f);
        private static readonly Color FieldColour = new Color(0.20f, 0.33f, 0.24f);
        private static readonly Color CraftColour = new Color(0.55f, 0.60f, 0.70f);

        private struct Stats { public int Pieces, Authored, ColliderMeshes, VisualMeshes; public float Radius; }

        [MenuItem("Tumbang Preso/Sample Map/Build Arena")]
        public static void BuildFromMenu() { Build(); Open(); }

        /// <summary>⚠️ A batch run must EXIT with a failure code on an exception, or the caller
        /// reads a silent zero and a stale scene.</summary>
        public static void Run()
        {
            try { Build(); }
            catch (Exception e) { Debug.LogError(Tag + "FAILED: " + e); EditorApplication.Exit(1); return; }
            EditorApplication.Exit(0);
        }

        public static void Open() { EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single); }

        public static void Build()
        {
            if (!File.Exists(LayoutPath)) throw new FileNotFoundException("No stage layouts: " + LayoutPath);
            var doc = JObject.Parse(File.ReadAllText(LayoutPath));
            float stageTop = Number(doc["stageTop"], 0.0f);
            float pitRadius = Number(doc["pitRadius"], 40.0f);
            float wallHalf = Number(doc["wallHalf"], 22.0f);
            float wallHeight = Number(doc["wallHeight"], WallHeight);
            float catchY = Number(doc["catchY"], ArenaStage.LowestCatchY);
            var layouts = doc["layouts"] as JArray;
            if (layouts == null || layouts.Count == 0) throw new InvalidOperationException(LayoutPath + " names no layouts");
            if (catchY < ArenaStage.LowestCatchY)
                Debug.LogWarning($"{Tag}catchY {catchY} is under {ArenaStage.LowestCatchY}: a joining player's body would never be seen that low " +
                                 "(MatchRpc.AcceptMove refuses poses under -5). The stage catches at the higher line.");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject(SceneName).transform;
            // What MapAtmosphereAuthor.Apply sets for every map; written here so the two agree.
            root.gameObject.AddComponent<MapGrade>().Set(1.0f, 1.025f, 1.06f, 1.0f, 1.9f);

            var stats = Stage(root, layouts, doc["spawns"], stageTop, catchY);
            Gameplay(root, doc["spawns"], wallHalf, wallHeight, stageTop);
            BreakCamera(root, stats.Radius);

            int art = 0;
            if (ArenaArtPlacer.Exists) art = ArenaArtPlacer.Place(root);
            else
            {
                Debug.Log($"{Tag}No {ArenaArtPlacer.LayoutPath}: building the grey field in place of the stadium.");
                GreyField(root, pitRadius, stageTop);
            }

            bool crowd = Crowd(root);
            int lights = Floodlights(root);
            int craft = Traffic(root);
            Lighting(root);

            EditorSceneManager.MarkSceneDirty(scene);
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Could not save " + ScenePath);
            AssetDatabase.SaveAssets();
            AddSceneToBuildSettings();
            Debug.Log($"{Tag}Scene built: {layouts.Count} layouts, {stats.Pieces} stage pieces ({stats.Authored} art models, " +
                      $"{stats.ColliderMeshes} collider meshes, {stats.VisualMeshes} generated visual meshes), walking radius {stats.Radius:F1} m, " +
                      $"{art} art placements, crowd {(crowd ? "built" : "absent")}, {lights} floodlights, {craft} sky craft.");
        }

        // ------------------------------------------------------------------ the stage

        /// <summary>Each layout's colliders, solids, holograms, pads and pickups, and the
        /// `ArenaStage` that holds every piece's numbers.</summary>
        private static Stats Stage(Transform root, JArray layouts, JToken spawns, float stageTop, float catchY)
        {
            int count = layouts.Count;
            var names = new string[count];
            var used = new HashSet<string>();
            for (int l = 0; l < count; l++)
            {
                names[l] = (string)layouts[l]["name"];
                if (string.IsNullOrEmpty(names[l])) names[l] = "layout" + l;
                if (!used.Add(names[l])) throw new InvalidOperationException($"{Tag}Two layouts are named '{names[l]}'");
            }

            // Every id, in the order first met, with its shape in each layout that has it.
            var order = new List<string>();
            var byId = new Dictionary<string, Shape[]>();
            for (int l = 0; l < count; l++)
            {
                var bonus = new HashSet<string>();
                foreach (var id in layouts[l]["bonus"] as JArray ?? new JArray()) bonus.Add((string)id);

                foreach (var token in layouts[l]["pieces"] as JArray ?? new JArray())
                {
                    string id = (string)token["id"];
                    if (string.IsNullOrEmpty(id)) throw new InvalidOperationException($"{Tag}Layout '{names[l]}' has a piece with no id");
                    if (!byId.TryGetValue(id, out var slots)) { byId[id] = slots = new Shape[count]; order.Add(id); }
                    if (slots[l].Exists) throw new InvalidOperationException($"{Tag}Layout '{names[l]}' names the piece '{id}' twice");
                    slots[l] = ReadShape(token, $"'{id}' in layout '{names[l]}'", bonus.Contains(id));
                }
            }

            foreach (var id in order)
            {
                bool ramp = false, round = false;
                foreach (var shape in byId[id]) if (shape.Exists) { ramp |= shape.IsRamp; round |= !shape.IsRamp; }
                if (ramp && round) Debug.LogWarning($"{Tag}'{id}' is a ramp in one layout and round in another: it cannot change shape, so one sinks and the other rises.");
            }

            for (int l = 0; l < count; l++) CheckLayout(l, names[l], layouts[l], order, byId, spawns, stageTop);

            var stageObject = Group(root, "Stage");
            var stage = stageObject.gameObject.AddComponent<ArenaStage>();
            stageObject.gameObject.AddComponent<ArenaFallRecovery>();
            var colliders = Group(stageObject, "Colliders");
            var visuals = Group(stageObject, "Visuals");
            var holograms = Group(stageObject, "Holograms");
            var features = Group(stageObject, "Features");

            EnsureFolder(ColliderFolder);
            EnsureFolder(GeneratedFolder);
            var pieceMaterials = new[]
            {
                Flat("arena_stage_top", TopColour, 0.22f),
                Flat("arena_stage_side", SideColour, 0.30f),
                Flat("arena_stage_under", UnderColour, 0.10f),
            };
            var hologram = HologramMaterial();

            // The saved scene stands in the layout the stage shows before any match.
            int first = ArenaStage.LayoutFor(0L, 1, count);
            var stats = new Stats { Pieces = order.Count };
            var written = new HashSet<string>();
            // A piece that stands the same in two layouts is one mesh (the meshes are in the stage's frame).
            var colliderMeshes = new Dictionary<string, Mesh>();
            var visualMeshes = new Dictionary<string, Mesh>();
            float lowest = float.PositiveInfinity;

            stage.Layouts = new ArenaStage.Layout[count];
            var colliderGroups = new Transform[count];
            var visualGroups = new Transform[count];
            var hologramGroups = new Transform[count];
            for (int l = 0; l < count; l++)
            {
                colliderGroups[l] = Group(colliders, names[l]);
                visualGroups[l] = Group(visuals, names[l]);
                hologramGroups[l] = Group(holograms, names[l]);
                var featureGroup = Group(features, names[l]);
                Features(featureGroup, layouts[l], names[l], order, byId, l);
                colliderGroups[l].gameObject.SetActive(l == first);
                featureGroup.gameObject.SetActive(l == first);
                stage.Layouts[l] = new ArenaStage.Layout
                {
                    Name = names[l],
                    CanHeight = Number(layouts[l]["canHeight"], 0.0f),
                    Colliders = colliderGroups[l].gameObject,
                    Features = featureGroup.gameObject,
                };
            }

            stage.Pieces = new ArenaStage.Piece[order.Count];
            for (int i = 0; i < order.Count; i++)
            {
                string id = order[i];
                var shapes = byId[id];
                var piece = new ArenaStage.Piece { Id = id, Shapes = shapes, Solids = new GameObject[count], Holograms = new GameObject[count], Authored = new bool[count] };

                for (int l = 0; l < count; l++)
                {
                    var shape = shapes[l];
                    if (!shape.Exists) continue;

                    stats.Radius = Mathf.Max(stats.Radius, shape.Outer);
                    lowest = Mathf.Min(lowest, Mathf.Min(shape.Top, shape.Top1) - shape.Thick);
                    string name = $"stage_{names[l]}_{id}";
                    string key = Key(shape);

                    if (!colliderMeshes.TryGetValue(key, out var colliderMesh))
                    {
                        colliderMeshes[key] = colliderMesh = MeshAsset($"{ColliderFolder}/collider_{names[l]}_{id}.asset", shape, false, written);
                        stats.ColliderMeshes++;
                    }
                    var solid = Group(colliderGroups[l], id);
                    solid.gameObject.AddComponent<MeshCollider>().sharedMesh = colliderMesh;

                    var model = ArenaArtPlacer.FindModel(StageFolder, name);
                    piece.Authored[l] = model != null;
                    if (model != null) stats.Authored++;
                    Mesh visualMesh = null;
                    if (model == null && !visualMeshes.TryGetValue(key, out visualMesh))
                    {
                        visualMeshes[key] = visualMesh = MeshAsset($"{GeneratedFolder}/{name}.asset", shape, true, written);
                        stats.VisualMeshes++;
                    }

                    piece.Solids[l] = Visual(visualGroups[l], name, model, visualMesh, pieceMaterials, null);
                    piece.Solids[l].SetActive(l == first);
                    piece.Holograms[l] = Visual(hologramGroups[l], name, model, visualMesh, null, hologram);
                    piece.Holograms[l].SetActive(false);
                }

                stage.Pieces[i] = piece;
            }

            Purge(ColliderFolder, written);
            Purge(GeneratedFolder, written);

            stage.PieceMaterials = pieceMaterials;
            stage.CatchHeight = catchY;
            stage.Radius = stats.Radius;
            stage.LowestUnderside = lowest;
            return stats;
        }

        /// <summary>
        /// One piece as it is drawn in one layout: a holder at the stage's origin (the stage turns
        /// and lifts the holder, so an art model keeps the rotation its file gave it), carrying
        /// the art model if there is one, else the generated mesh. With `only` set, every
        /// material is that one and it casts no shadow: the hologram twin.
        /// </summary>
        private static GameObject Visual(Transform parent, string name, GameObject model, Mesh mesh, Material[] materials, Material only)
        {
            var holder = Group(parent, name).gameObject;
            if (model != null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, holder.transform);
                instance.name = "Model";
            }
            else
            {
                holder.AddComponent<MeshFilter>().sharedMesh = mesh;
                holder.AddComponent<MeshRenderer>().sharedMaterials = materials ?? new[] { only, only, only };
            }

            if (only == null) return holder;

            foreach (var renderer in holder.GetComponentsInChildren<Renderer>(true))
            {
                var all = renderer.sharedMaterials;
                for (int i = 0; i < all.Length; i++) all[i] = only;
                renderer.sharedMaterials = all;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            return holder;
        }

        /// <summary>The mesh asset at a path, made or refilled in place (so its GUID, and the
        /// scene's reference to it, survive a rebuild).</summary>
        private static Mesh MeshAsset(string path, in Shape shape, bool visual, HashSet<string> written)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool fresh = mesh == null;
            if (fresh) mesh = new Mesh();
            ArenaStageMesh.Build(shape, mesh, visual);
            mesh.name = Path.GetFileNameWithoutExtension(path);
            if (fresh) AssetDatabase.CreateAsset(mesh, path);
            else EditorUtility.SetDirty(mesh);
            written.Add(path);
            return mesh;
        }

        /// <summary>Generated meshes of an earlier build that this one did not write (a piece or
        /// a layout that is gone) are removed, so the folder is exactly what the scene uses.</summary>
        private static void Purge(string folder, HashSet<string> written)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Mesh", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string file = Path.GetFileName(path);
                if (!file.EndsWith(".asset") || !(file.StartsWith("stage_") || file.StartsWith("collider_"))) continue;
                if (!written.Contains(path)) AssetDatabase.DeleteAsset(path);
            }
        }

        /// <summary>Equal for two shapes that are the same mesh in the stage's frame.</summary>
        private static string Key(in Shape s) => string.Format(CultureInfo.InvariantCulture,
            "{0}|{1:F3}|{2:F3}|{3:F3}|{4:F3}|{5:F3}|{6:F3}|{7:F3}|{8:F3}",
            s.IsRamp ? "ramp" : "round", s.R0, s.R1, s.Full ? 0.0f : Mathf.Repeat(s.A0, 360.0f), s.Sweep, s.Top, s.Top1, s.Width, s.Thick);

        private static Shape ReadShape(JToken token, string where, bool bonus)
        {
            string kind = (string)token["kind"];
            var shape = new Shape { Exists = true, Bonus = bonus || (token["bonus"] != null && (bool)token["bonus"]), Thick = Number(token["thick"], 1.0f) };
            switch (kind)
            {
                case "disc":
                    shape.Kind = ShapeKind.Disc;
                    shape.R1 = Need(token, "r", where);
                    shape.Sweep = 360.0f;
                    shape.Top = shape.Top1 = Need(token, "top", where);
                    break;
                case "ring":
                case "arc":
                    shape.Kind = kind == "ring" ? ShapeKind.Ring : ShapeKind.Arc;
                    shape.R0 = Need(token, "r0", where);
                    shape.R1 = Need(token, "r1", where);
                    shape.Top = shape.Top1 = Need(token, "top", where);
                    shape.Sweep = 360.0f;
                    if (kind == "arc")
                    {
                        // Swept clockwise from a0 to a1; a0 may be negative.
                        shape.A0 = Need(token, "a0", where);
                        shape.Sweep = Need(token, "a1", where) - shape.A0;
                        if (shape.Sweep <= 0.0f || shape.Sweep > 360.0f) throw new InvalidOperationException($"{Tag}{where}: an arc sweeps clockwise from a0 to a1, with a1 above a0 by at most 360");
                    }
                    if (shape.R1 <= shape.R0) throw new InvalidOperationException($"{Tag}{where}: r1 must be past r0");
                    break;
                case "ramp":
                    shape.Kind = ShapeKind.Ramp;
                    shape.A0 = Need(token, "bearing", where);
                    shape.R0 = Need(token, "r0", where);
                    shape.R1 = Need(token, "r1", where);
                    shape.Top = Need(token, "z0", where);
                    shape.Top1 = Need(token, "z1", where);
                    shape.Width = Need(token, "width", where);
                    if (Mathf.Approximately(shape.R0, shape.R1)) throw new InvalidOperationException($"{Tag}{where}: a ramp needs two different radii");
                    break;
                default:
                    throw new InvalidOperationException($"{Tag}{where}: unknown kind '{kind}' (disc, ring, arc, ramp)");
            }

            if (shape.Outer <= 0.0f || shape.Thick <= 0.0f) throw new InvalidOperationException($"{Tag}{where}: a piece needs a size and a thickness");
            return shape;
        }

        /// <summary>The top under a plan point in one layout: the highest piece that covers it
        /// and is not above `ceiling`.</summary>
        private static bool FloorAt(List<string> order, Dictionary<string, Shape[]> byId, int layout, float x, float z, float ceiling,
                                    bool roundOnly, out float y)
        {
            y = float.NegativeInfinity;
            foreach (var id in order)
            {
                var shape = byId[id][layout];
                if (!shape.Exists || shape.Bonus || (roundOnly && shape.IsRamp) || !shape.Contains(x, z, 0.0f)) continue;

                float top = shape.HeightAt(x, z);
                if (top <= ceiling && top > y) y = top;
            }

            return !float.IsNegativeInfinity(y);
        }

        /// <summary>
        /// Refuse a layout with no floor under the can or under a spawn mark. `MatchHost.SeatOnFloor`
        /// casts from 2 m above the mark down 6 m, so the floor must be a round piece (never a
        /// ramp, never a bonus platform) whose plan covers the mark and whose top is within that
        /// reach. The can stands at the centre on the floor the layout names as `canHeight`.
        /// </summary>
        private static void CheckLayout(int index, string name, JToken layout, List<string> order, Dictionary<string, Shape[]> byId,
                                        JToken spawns, float stageTop)
        {
            float can = Number(layout["canHeight"], 0.0f);
            if (!FloorAt(order, byId, index, 0.0f, 0.0f, can + 0.01f, true, out float under) || Mathf.Abs(under - can) > 0.01f)
                throw new InvalidOperationException($"{Tag}Layout '{name}' has no floor at canHeight {can} under the can. Fix {LayoutPath}.");

            var marks = new List<Vector3>();
            if (spawns != null)
            {
                if (spawns["taya"] != null) marks.Add(Vector(spawns["taya"]));
                foreach (var token in spawns["attackers"] as JArray ?? new JArray()) marks.Add(Vector(token));
            }

            foreach (var mark in marks)
                if (!FloorAt(order, byId, index, mark.x, mark.z, mark.y + 2.0f, true, out float y) || y < mark.y - 4.0f)
                    throw new InvalidOperationException($"{Tag}Layout '{name}' has no floor under the mark at {mark}. Fix {LayoutPath}.");
        }

        /// <summary>
        /// One layout's jump pads, speed pads and stamina pickups, each at a bearing and a radius
        /// from the can, `y` the floor's height there. A speed pad's travel (`along`) is
        /// "tangent", clockwise round the can, or "radial", outward; its `halfSize` is
        /// [half across the travel, half along it].
        /// </summary>
        private static void Features(Transform group, JToken layout, string name, List<string> order, Dictionary<string, Shape[]> byId, int index)
        {
            Vector3 At(JToken token, string what)
            {
                float bearing = Need(token, "bearing", what), r = Need(token, "r", what), y = Number(token["y"], 0.0f);
                Vector3 at = ArenaStageMesh.Direction(bearing) * r + Vector3.up * y;
                if (!FloorAt(order, byId, index, at.x, at.z, y + 0.3f, false, out float floor) || Mathf.Abs(floor - y) > 0.3f)
                    Debug.LogWarning($"{Tag}{what} at bearing {bearing}, r {r}, y {y} has no floor at that height under it.");
                return at;
            }

            int n = 0;
            foreach (var token in layout["jumpPads"] as JArray ?? new JArray())
            {
                var pad = Group(group, "JumpPad " + n);
                pad.position = At(token, $"Layout '{name}' jump pad {n++}");
                pad.gameObject.AddComponent<JumpPad>().LaunchSpeed = Number(token["speed"], JumpPadSpeed);
            }

            n = 0;
            foreach (var token in layout["speedPads"] as JArray ?? new JArray())
            {
                string what = $"Layout '{name}' speed pad {n}";
                string along = (string)token["along"] ?? "tangent";
                if (along != "tangent" && along != "radial") throw new InvalidOperationException($"{Tag}{what}: 'along' is \"tangent\" or \"radial\", not \"{along}\"");

                var pad = Group(group, "SpeedPad " + n++);
                // The pad's forward is its travel: a bearing's own direction is outward, and a
                // quarter turn clockwise of it runs clockwise round the can.
                float yaw = Need(token, "bearing", what) + (along == "tangent" ? 90.0f : 0.0f);
                pad.SetPositionAndRotation(At(token, what), Quaternion.Euler(0.0f, yaw, 0.0f));
                var speed = pad.gameObject.AddComponent<ArenaSpeedPad>();
                var half = token["halfSize"] as JArray;
                if (half != null && half.Count >= 2) speed.HalfSize = new Vector2((float)half[0], (float)half[1]);
            }

            n = 0;
            foreach (var token in layout["pickups"] as JArray ?? new JArray())
            {
                var pickup = Group(group, "StaminaPickup " + n);
                pickup.position = At(token, $"Layout '{name}' pickup {n++}");
                pickup.gameObject.AddComponent<ArenaStaminaPickup>();
            }
        }

        // ------------------------------------------------------------------ what never moves

        /// <summary>
        /// The match installer, the kill plane, the spawn markers and the Bounds: four invisible
        /// box walls (`MatchInstaller.MeasurePlayableBounds` reads boxes) with their inner faces
        /// on +/- half. They run from under the catch line to `wallHeight` above the stage, so a
        /// body in the shaft is still inside them when the host catches it.
        /// </summary>
        private static void Gameplay(Transform root, JToken spawns, float half, float wallHeight, float stageTop)
        {
            Group(root, "~Match").gameObject.AddComponent<MatchInstaller>();
            var kill = Group(root, "KillPlane");
            kill.localPosition = new Vector3(0, KillPlane.PlaneHeight, 0);
            kill.gameObject.AddComponent<KillPlane>();
            var trigger = kill.gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(KillPlane.PlaneExtent, KillPlane.PlaneThickness, KillPlane.PlaneExtent);

            var marks = Group(root, "SpawnPoints");
            int seat = 0;
            if (spawns != null)
            {
                if (spawns["taya"] != null) Group(marks, "Spawn" + seat++).localPosition = Vector(spawns["taya"]) + Vector3.up * 0.1f;
                foreach (var token in spawns["attackers"] as JArray ?? new JArray())
                    Group(marks, "Spawn" + seat++).localPosition = Vector(token) + Vector3.up * 0.1f;
            }

            var bounds = Group(root, "Bounds");
            float top = stageTop + wallHeight, bottom = KillPlane.PlaneHeight, height = top - bottom, y = (top + bottom) * 0.5f;
            float span = 2 * (half + WallThickness), centre = half + WallThickness * 0.5f;
            Wall(bounds, "WallWest", new Vector3(-centre, y, 0), new Vector3(WallThickness, height, span));
            Wall(bounds, "WallEast", new Vector3(centre, y, 0), new Vector3(WallThickness, height, span));
            Wall(bounds, "WallSouth", new Vector3(0, y, -centre), new Vector3(span, height, WallThickness));
            Wall(bounds, "WallNorth", new Vector3(0, y, centre), new Vector3(span, height, WallThickness));
        }

        /// <summary>The break's camera (`ArenaBreakCamera` poses it every frame of a break). Its
        /// far plane takes in the whole stadium, which the game camera's 240 m does not.</summary>
        private static void BreakCamera(Transform root, float radius)
        {
            var eye = Group(root, "BreakCamera").gameObject;
            var camera = eye.AddComponent<Camera>();
            camera.enabled = false;
            camera.depth = ArenaBreakCamera.Depth;
            camera.fieldOfView = 50.0f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 1200.0f;
            var from = new Vector3(0.6f, 0.45f, 0.8f) * radius * 1.8f;
            eye.transform.SetPositionAndRotation(from, Quaternion.LookRotation(-from));
            eye.AddComponent<ArenaBreakCamera>();
        }

        /// <summary>With no art: a flat grey-green field round the shaft, its inner wall the
        /// shaft's side, drawn only. Nobody stands on it; the play walls are inside it.</summary>
        private static void GreyField(Transform root, float pitRadius, float stageTop)
        {
            var shape = new Shape { Exists = true, Kind = ShapeKind.Ring, R0 = pitRadius, R1 = pitRadius + FieldWidth, Sweep = 360.0f, Top = stageTop, Top1 = stageTop, Thick = ShaftDepth };
            var mesh = MeshAsset($"{GeneratedFolder}/arena_grey_field.asset", shape, true, new HashSet<string>());
            var field = Group(root, "GreyField").gameObject;
            field.isStatic = true;
            field.AddComponent<MeshFilter>().sharedMesh = mesh;
            var material = Flat("arena_grey_field", FieldColour, 0.05f);
            field.AddComponent<MeshRenderer>().sharedMaterials = new[] { material, material, material };
        }

        // ------------------------------------------------------------------ the other kits' hooks

        /// <summary>`ArenaCrowdBuilder.Build(Transform parent)`, if the crowd kit is in the
        /// project. Found by name, so this file compiles without it. The crowd is presentation:
        /// if its builder fails the map is still built, and the failure is in the log.</summary>
        private static bool Crowd(Transform root)
        {
            Type type = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(CrowdBuilderType, false);
                if (type != null) break;
            }

            var build = type?.GetMethod("Build", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(Transform) }, null);
            if (build == null) return false;

            try { build.Invoke(null, new object[] { Group(root, "Crowd") }); }
            catch (TargetInvocationException e) { Debug.LogError($"{Tag}The crowd builder failed, the map is built without it: {e.InnerException}"); return false; }
            return true;
        }

        /// <summary>
        /// The floodlight banks, from tools/arena_lights.json if it is there: a list (or an
        /// object with a `lights` list) of { "position": [x, y, z], "target": [x, y, z],
        /// "color": [r, g, b], "intensity": n, "range": n, "angle": degrees }, Unity coordinates;
        /// everything but the position is optional and a light with no target aims at the can.
        ///
        /// ⚠️ THEY ARE SPOTS WITH NO SHADOW, NEVER PER-PIXEL (`ForceVertex`): a pixel light is
        /// another pass over everything it reaches, and a stadium has dozens. The stage is lit by
        /// the scene's one directional key (`Lighting`); these mark where the banks are for the
        /// kits that draw their beams and flares, and add a little vertex light where a shader
        /// takes it.
        /// </summary>
        private static int Floodlights(Transform root)
        {
            if (!File.Exists(LightsPath)) return 0;

            var doc = JToken.Parse(File.ReadAllText(LightsPath));
            var list = doc as JArray ?? doc["lights"] as JArray ?? doc["floodlights"] as JArray;
            if (list == null) { Debug.LogWarning($"{Tag}{LightsPath} has no list of lights"); return 0; }

            var parent = Group(root, "Floodlights");
            int n = 0;
            foreach (var token in list)
            {
                if (token["position"] == null) continue;

                Vector3 at = Vector(token["position"]), target = token["target"] != null ? Vector(token["target"]) : Vector3.zero;
                var light = Group(parent, (string)token["name"] ?? "Floodlight " + n).gameObject.AddComponent<Light>();
                light.transform.SetPositionAndRotation(at, Quaternion.LookRotation((target - at).sqrMagnitude > 1e-4f ? target - at : Vector3.down));
                light.type = LightType.Spot;
                light.spotAngle = Number(token["angle"], 50.0f);
                light.range = Number(token["range"], Vector3.Distance(at, target) * 1.5f);
                light.intensity = Number(token["intensity"], 1.2f);
                var c = token["color"] as JArray;
                light.color = c != null && c.Count >= 3 ? new Color((float)c[0], (float)c[1], (float)c[2]) : new Color(0.92f, 0.95f, 1.0f);
                light.shadows = LightShadows.None;
                light.renderMode = LightRenderMode.ForceVertex;
                n++;
            }

            return n;
        }

        /// <summary>
        /// The sky traffic, from tools/arena_traffic.json if it is there: { "paths": [ { "name",
        /// "points": [[x, y, z], ...] (a closed loop, Unity coordinates), "speed": m/s,
        /// "count": craft on the loop, "model": a .glb under Art/Arena/Models, "size": [x, y, z]
        /// of the stand-in box when there is no model } ] }. `ArenaTraffic` moves them.
        /// </summary>
        private static int Traffic(Transform root)
        {
            if (!File.Exists(TrafficPath)) return 0;

            var doc = JToken.Parse(File.ReadAllText(TrafficPath));
            var list = doc as JArray ?? doc["paths"] as JArray;
            if (list == null) { Debug.LogWarning($"{Tag}{TrafficPath} has no list of paths"); return 0; }

            var parent = Group(root, "SkyTraffic");
            var traffic = parent.gameObject.AddComponent<ArenaTraffic>();
            var paths = new List<ArenaTraffic.Path>();
            Material standIn = null;
            int total = 0;
            foreach (var token in list)
            {
                var points = new List<Vector3>();
                foreach (var p in token["points"] as JArray ?? new JArray()) points.Add(Vector(p));
                if (points.Count < 3) { Debug.LogWarning($"{Tag}A traffic path needs three points or more; skipped"); continue; }

                string name = (string)token["name"] ?? "Path " + paths.Count;
                int count = Mathf.Clamp((int)Number(token["count"], 1.0f), 1, 64);
                var model = string.IsNullOrEmpty((string)token["model"]) ? null : ArenaArtPlacer.FindModel("Models", (string)token["model"]);
                var size = token["size"] != null ? Vector(token["size"]) : new Vector3(2.5f, 1.2f, 6.0f);
                var craft = new Transform[count];
                for (int i = 0; i < count; i++)
                {
                    GameObject go;
                    if (model != null) go = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
                    else
                    {
                        go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        go.transform.SetParent(parent, false);
                        go.transform.localScale = size;
                        Object.DestroyImmediate(go.GetComponent<Collider>());
                        if (standIn == null) standIn = Flat("arena_grey_craft", CraftColour, 0.4f);
                        go.GetComponent<Renderer>().sharedMaterial = standIn;
                    }

                    go.name = $"{name} craft {i}";
                    go.transform.position = points[0];
                    foreach (var renderer in go.GetComponentsInChildren<Renderer>()) renderer.shadowCastingMode = ShadowCastingMode.Off;
                    craft[i] = go.transform;
                }

                paths.Add(new ArenaTraffic.Path { Name = name, Points = points.ToArray(), Speed = Number(token["speed"], 18.0f), Craft = craft });
                total += count;
            }

            traffic.Paths = paths.ToArray();
            return total;
        }

        // ------------------------------------------------------------------ light, sky and haze

        /// <summary>
        /// A NIGHT MATCH. The scene as authored matches the "Arena" `WorldLookProfile` row exactly
        /// (the Lagoon Cove and Ilalim pattern), so the look's weight in Play changes only the
        /// extras it owns (ramp, grade, clouds):
        ///   * the KEY is one directional light standing in for the floodlights: high, cool
        ///     white, the row's colour, intensity and shadow strength. The row's elevation is 0,
        ///     so Play keeps this angle;
        ///   * the AMBIENT trilight (every shadow's colour here) is the row's deep navy;
        ///   * the FOG is the row's dark indigo, from past the field to well past the stands;
        ///   * the SKY (Art/MapAtmosphere/ArenaSky.mat, which `MapAtmosphereAuthor.RefreshCloudMaterials`
        ///     requires of every map in `SceneFlow.Maps`) takes the row's zenith, horizon and
        ///     cloud colours, and has no sun disc.
        /// If the sky cannot be authored (its shader or its cloud source is missing) the map is
        /// still built, under a plain dark background, and the log says so.
        /// </summary>
        private static void Lighting(Transform root)
        {
            var sun = new GameObject("FloodlightKey").AddComponent<Light>();
            sun.transform.SetParent(root, false);
            sun.type = LightType.Directional;

            try { MapAtmosphereAuthor.Apply(SceneName); }
            catch (InvalidOperationException e)
            {
                Debug.LogWarning($"{Tag}No authored sky ({e.Message}): the scene has a plain dark background.");
                RenderSettings.skybox = null;
            }

            // After Apply, which turns every directional light to a daytime sun.
            sun.transform.rotation = Quaternion.Euler(62.0f, 28.0f, 0.0f);
            sun.shadows = LightShadows.Soft;
            sun.shadowBias = 0.025f;
            sun.shadowNormalBias = 0.16f;
            RenderSettings.sun = sun;

            var look = EnsureLook();
            sun.color = look.Sun; sun.intensity = look.SunIntensity; sun.shadowStrength = look.ShadowStrength;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = look.Sky; RenderSettings.ambientEquatorColor = look.Equator; RenderSettings.ambientGroundColor = look.Ground;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = look.Fog; RenderSettings.fogStartDistance = look.FogStart; RenderSettings.fogEndDistance = look.FogEnd;

            var sky = RenderSettings.skybox;
            if (sky != null && sky.HasProperty("_Zenith"))
            {
                sky.SetColor("_Zenith", look.Zenith); sky.SetColor("_Horizon", look.Horizon); sky.SetColor("_Ground", look.Fog);
                sky.SetColor("_CloudLight", look.CloudLight); sky.SetColor("_CloudShade", look.CloudShade);
                sky.SetColor("_SunColor", look.Sun); sky.SetVector("_SunDirection", -sun.transform.forward);
                sky.SetFloat("_SunDisc", 0.0f); sky.SetFloat("_SunHalo", 0.0f);
                EditorUtility.SetDirty(sky);
            }
        }

        /// <summary>
        /// The "Arena" row of the authored look profile. The profile ASSET wins over the code's
        /// defaults, so an asset saved before the row existed has none and
        /// `WorldLookProfile.Find` answers null for this map; the code's row is copied in then,
        /// and only then (an owner's later tuning of the row is never overwritten).
        /// </summary>
        private static WorldLookProfile.MapLook EnsureLook()
        {
            var look = WorldLookProfile.Current.Find(SceneName);
            if (look != null) return look;

            var defaults = ScriptableObject.CreateInstance<WorldLookProfile>();
            look = defaults.Find(SceneName);
            Object.DestroyImmediate(defaults);
            if (look == null) throw new InvalidOperationException("WorldLookProfile has no " + SceneName + " row in code");

            var profile = WorldLookProfile.Current;
            var rows = new List<WorldLookProfile.MapLook>(profile.Maps) { look };
            profile.Maps = rows.ToArray();
            if (EditorUtility.IsPersistent(profile)) { EditorUtility.SetDirty(profile); AssetDatabase.SaveAssetIfDirty(profile); }
            Debug.Log($"{Tag}Added the {SceneName} row to the authored WorldLookProfile.");
            return look;
        }

        // ------------------------------------------------------------------ helpers

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
        }

        /// <summary>A plain Standard material of one colour, made once and kept.</summary>
        private static Material Flat(string name, Color colour, float smoothness)
        {
            EnsureFolder(MaterialFolder);
            string path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(material, path); }
            material.color = colour;
            material.SetFloat("_Glossiness", smoothness);
            material.SetFloat("_Metallic", 0.0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>The hologram's material. Made once: after that it is the artist's to tune or
        /// to give another shader, and a rebuild leaves it alone.</summary>
        private static Material HologramMaterial()
        {
            EnsureFolder(MaterialFolder);
            string path = $"{MaterialFolder}/arena_stage_hologram.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;

            var shader = Shader.Find("TumbangPreso/ArenaHologram");
            if (shader == null)
            {
                Debug.LogWarning($"{Tag}TumbangPreso/ArenaHologram is missing: the hologram is a plain transparent sprite material.");
                shader = Shader.Find("Sprites/Default");
            }

            material = new Material(shader) { color = new Color(0.45f, 0.95f, 1.0f, 0.5f), enableInstancing = true };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void Wall(Transform parent, string name, Vector3 centre, Vector3 size)
        {
            var wall = Group(parent, name);
            wall.localPosition = centre;
            wall.gameObject.AddComponent<BoxCollider>().size = size;
        }

        private static Vector3 Vector(JToken token)
        {
            var a = token as JArray;
            if (a == null || a.Count < 3) throw new InvalidOperationException($"{Tag}Expected [x, y, z], got: {token}");
            return new Vector3((float)a[0], (float)a[1], (float)a[2]);
        }

        private static float Number(JToken token, float fallback) =>
            token != null && (token.Type == JTokenType.Float || token.Type == JTokenType.Integer) ? (float)token : fallback;

        private static float Need(JToken token, string key, string where)
        {
            var value = token[key];
            if (value == null || (value.Type != JTokenType.Float && value.Type != JTokenType.Integer))
                throw new InvalidOperationException($"{Tag}{where} needs a number '{key}'. Fix {LayoutPath}.");
            return (float)value;
        }

        private static Transform Group(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        private static void AddSceneToBuildSettings()
        {
            var existing = EditorBuildSettings.scenes;
            foreach (var s in existing)
                if (s.path == ScenePath && !s.guid.Empty()) return;

            var next = new List<EditorBuildSettingsScene>();
            foreach (var s in existing) if (s.path != ScenePath) next.Add(s);
            next.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = next.ToArray();
        }
    }
}
