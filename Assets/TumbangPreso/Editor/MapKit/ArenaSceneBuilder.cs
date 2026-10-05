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
    /// THE ART IS OPTIONAL. Art/Arena/arena_layout.json (written by tools/export_arena_unity.py,
    /// placed by `ArenaArtPlacer`), the stage and prop models beside it, tools/arena_lights.json,
    /// tools/arena_traffic.json and the crowd builder are each used if they are there. With none
    /// of them this still builds a playable scene: the stage, a grey field round the shaft, the
    /// night look.
    ///
    /// THE FRAME. The game's: north is +z, a bearing b at radius r is x = r sin b, z = r cos b.
    /// The export delivers every model in it. The kits' own data files (lights, traffic, rows)
    /// are in Blender's (z up, y north) and are turned here by `B`: Blender (x, y, z) is (x, z, y).
    ///
    /// ⚠️ THE CAMERAS SEE FURTHER HERE THAN ON ANY OTHER MAP (`MapCameraRange`, on the root, which
    /// the game camera and a spectator's adopt in their Start): the stadium is 480 m across and
    /// its city reaches 2387 m from the can. See `PlayFar` for why the game camera takes less
    /// than the whole of it.
    ///
    /// Menu: Tumbang Preso/Sample Map. Batch: TumbangPreso.EditorTools.MapKit.ArenaSceneBuilder.Run,
    /// .RunReview (build, then the review renders: ArenaSceneBuilder.Review.cs).
    /// </summary>
    public static partial class ArenaSceneBuilder
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
        /// <summary>Where the stage kit's props are: arena_jump_pad, arena_speed_pad, arena_pickup, arena_drone.</summary>
        public const string PropsFolder = "Props";
        private const string SkyShader = "TumbangPreso/ArenaSky";
        private const string SkyTexture = ArenaArtPlacer.Root + "/Textures/arena_city_sky.png";
        private const string TrainObject = "city_train", CraftPrefix = "city_craft_";

        /// <summary>
        /// ⚠️ THE GAME CAMERA'S FAR PLANE IS 1300 m, NOT THE WHOLE MAP'S 2500. `WorldOutline`'s
        /// edges and ambient occlusion read a 16-bit depth whose step is far / 65536 everywhere:
        /// 2 cm at 1300 m, 3.8 cm at 2500 m (over the occlusion pass's 3 cm bias, and enough to
        /// speckle an edge within 2 m of the lens; at the other maps' 240 m it is 4 mm). And
        /// nothing past 1215 m can be seen from the stage: the furthest landmark tower's top is
        /// 1090 m from the can, the high barge lane (r 1060, 590 up) 1213 m, the city floor
        /// through the shaft at most 950 m, and the far ring of towers (to 2130 m) stands under
        /// the stadium's rim seen from the can, as the city kit built it. A camera that LEAVES
        /// the stadium (a spectator's, the break's, the review's) takes `FreeFar`: the farthest
        /// vertex of the city is 2387 m from the can.
        /// </summary>
        public const float PlayFar = 1300.0f, FreeFar = 2500.0f;

        /// <summary>
        /// Where the ink of `WorldOutline` fades out on this map, metres from the eye. Its own
        /// rule is the fog's distances, which here run to the city (180 to 3400 m) and would draw
        /// an edge on every seat row and every spectator of every stand. The stage is 44 m
        /// across (62 m corner to corner inside the walls): the ink is whole to 45 m, so on
        /// anything a player is playing against, and gone by 85 m, where the lower bowl begins.
        /// </summary>
        public const float InkFadeStart = 45.0f, InkFadeEnd = 85.0f;

        /// <summary>A beam is whole at its lamp and gone this share of the way to where the bank
        /// aims: 0.55 ends it about 30 m up over the field, never on the stage.</summary>
        private const float BeamShare = 0.55f;
        /// <summary>How many times wider and taller than its lamp face a beam is at its far end.</summary>
        private static readonly Vector2 BeamFar = new Vector2(1.55f, 3.5f);
        /// <summary>The four lights over the stage: how far out (a share of the stage's radius),
        /// how high (over the 12 m the slipper flies to), how strong.</summary>
        private const float StageLightOut = 0.65f, StageLightHeight = 15.0f, StageLightIntensity = 0.3f;
        /// <summary>The kit's speed pad is this half size (across, along) and its jump pad this half width, metres.</summary>
        private static readonly Vector2 SpeedPadHalf = new Vector2(0.8f, 1.5f);
        private const float JumpPadHalf = 0.9f;
        /// <summary>The jump pad's light on this map: the kit's teal (clear of #f87020 and #0080e8).</summary>
        private static readonly Color JumpGlow = new Color(0.30f, 0.95f, 0.85f);
        private const string MaterialFolder = ArenaArtPlacer.Root + "/Materials";
        private const string CrowdBuilderType = "TumbangPreso.EditorTools.MapKit.ArenaCrowdBuilder";
        private const string Tag = "[Arena] ";

        /// <summary>Upward launch in m/s unless a pad names its own. At `Balance.Gravity` 20 the
        /// apex is v*v/40: 15.5 gives 6.0 m, under the 12 m ceiling.</summary>
        private const float JumpPadSpeed = 15.5f;
        private const float WallThickness = 0.4f;
        /// <summary>The fall under the stage (owner, 2026-10-05: "you need to fall further"):
        /// `MatchRpc.AcceptMove` believes a pose down to `MoveFloorY` on this map, and the kill
        /// plane and the walls' feet are at `KillPlaneY`. The catch itself is the data's `catchY`.</summary>
        private const float MoveFloorY = ArenaStage.DefaultMoveFloorY, KillPlaneY = ArenaStage.DefaultKillPlaneY;
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
            float catchY = Number(doc["catchY"], ArenaStage.DefaultCatchY);
            var layouts = doc["layouts"] as JArray;
            if (layouts == null || layouts.Count == 0) throw new InvalidOperationException(LayoutPath + " names no layouts");
            if (catchY < MoveFloorY + ArenaStage.CatchMargin)
                Debug.LogWarning($"{Tag}catchY {catchY} is under {MoveFloorY + ArenaStage.CatchMargin}: a joining player's body would never be seen that low " +
                                 $"(on this map MatchRpc.AcceptMove refuses poses under {MoveFloorY}, and the catch needs {ArenaStage.CatchMargin} m above that). " +
                                 "The stage catches at the higher line.");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject(SceneName).transform;
            // What MapAtmosphereAuthor.Apply sets for every map; written here so the two agree.
            root.gameObject.AddComponent<MapGrade>().Set(1.0f, 1.025f, 1.06f, 1.0f, 1.9f);
            var range = root.gameObject.AddComponent<MapCameraRange>();
            range.PlayFar = PlayFar; range.FreeFar = FreeFar;
            range.InkFadeStart = InkFadeStart; range.InkFadeEnd = InkFadeEnd;

            // The kit materials first: the stage's pieces and props wear them too.
            bool hasArt = ArenaArtPlacer.Exists;
            if (hasArt) ArenaArtPlacer.Prepare(); else ArenaArtPlacer.Forget();

            var stats = Stage(root, layouts, doc["spawns"], stageTop, catchY);
            Gameplay(root, doc["spawns"], wallHalf, wallHeight, stageTop);
            Effects(root);
            BreakCamera(root, stats.Radius);

            int art = 0;
            if (hasArt) art = ArenaArtPlacer.Place(root);
            else
            {
                Debug.Log($"{Tag}No {ArenaArtPlacer.LayoutPath}: building the grey field in place of the stadium.");
                GreyField(root, pitRadius, stageTop);
            }

            bool crowd = Crowd(root);
            int lights = Floodlights(root, stats.Radius);
            int craft = Traffic(root);
            // ---- HOLO KIT (art, 2026-10-05): what moves in it (tools/arena_holo_motion.json) is baked onto the
            // Holo group the art placer made. One call; everything else is in ArenaHoloAuthor.cs. ----
            if (hasArt) ArenaHoloAuthor.Attach(root);
            // ---- end of the holo kit's block ----
            Lighting(root);
            ArenaArtPlacer.ReportUnmatched();

            EditorSceneManager.MarkSceneDirty(scene);
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Could not save " + ScenePath);
            AssetDatabase.SaveAssets();
            AddSceneToBuildSettings();
            Debug.Log($"{Tag}Scene built: {layouts.Count} layouts, {stats.Pieces} stage pieces ({stats.Authored} art models, " +
                      $"{stats.ColliderMeshes} collider meshes, {stats.VisualMeshes} generated visual meshes), walking radius {stats.Radius:F1} m, " +
                      $"{art} art placements, crowd {(crowd ? "built" : "absent")}, {lights} floodlight beams, {craft} sky craft, " +
                      $"cameras to {PlayFar} m (play) and {FreeFar} m (free).");
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
            stageObject.gameObject.AddComponent<ArenaFallRecovery>().DroneTemplate = DroneTemplate(stageObject);
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
            stage.CatchLine = catchY;
            stage.MoveFloor = MoveFloorY;
            stage.KillPlaneY = KillPlaneY;
            stage.Radius = stats.Radius;
            stage.LowestUnderside = lowest;
            return stats;
        }

        /// <summary>
        /// The stage kit's catch drone, left INACTIVE under the stage for `ArenaFallRecovery` to
        /// copy at run time: the model as imported, already wearing the map's materials (a copy
        /// made in Play cannot ask the asset database for them). Null without the model, and
        /// then `ArenaDrone` draws its grey-box.
        /// </summary>
        private static GameObject DroneTemplate(Transform stage)
        {
            var model = ArenaArtPlacer.FindModel(PropsFolder, "arena_drone");
            if (model == null) return null;

            var template = (GameObject)PrefabUtility.InstantiatePrefab(model, stage);
            template.name = "DroneTemplate";
            ArenaArtPlacer.Dress(template, false);
            template.SetActive(false);
            return template;
        }

        /// <summary>The kit's model of a pad or a pickup as the child `Model` its component looks
        /// for (`ArenaSpeedPad.ModelName`), wearing the map's materials. Nothing without the model:
        /// the component then builds its grey-box.</summary>
        private static void PropModel(Transform parent, string file, Vector3 scale)
        {
            var model = ArenaArtPlacer.FindModel(PropsFolder, file);
            if (model == null) return;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
            instance.name = ArenaSpeedPad.ModelName;
            instance.transform.localScale = scale;
            ArenaArtPlacer.Dress(instance, false);
        }

        /// <summary>
        /// One piece as it is drawn in one layout: a holder at the stage's origin (the stage turns
        /// and lifts the holder; an art model is in the game's own frame as imported, so it hangs
        /// there with no rotation of its own), carrying the art model if there is one, else the
        /// generated mesh. With `only` set, every material is that one and it casts no shadow:
        /// the hologram twin. Without, an art model wears the kit's materials and casts and takes
        /// shadows (the players' shadows fall on the stage).
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

            if (only == null)
            {
                if (model != null) ArenaArtPlacer.Dress(holder, true);
                return holder;
            }

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
        /// and is not above `ceiling`. A can or a spawn mark never counts a bonus platform (only a
        /// jump pad reaches one); a pad or a pickup does (`bonusToo`), because the lofts carry
        /// pickups as the reward for the jump.</summary>
        private static bool FloorAt(List<string> order, Dictionary<string, Shape[]> byId, int layout, float x, float z, float ceiling,
                                    bool roundOnly, out float y, bool bonusToo = false)
        {
            y = float.NegativeInfinity;
            foreach (var id in order)
            {
                var shape = byId[id][layout];
                if (!shape.Exists || (shape.Bonus && !bonusToo) || (roundOnly && shape.IsRamp) || !shape.Contains(x, z, 0.0f)) continue;

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
                if (!FloorAt(order, byId, index, at.x, at.z, y + 0.3f, false, out float floor, true) || Mathf.Abs(floor - y) > 0.3f)
                    Debug.LogWarning($"{Tag}{what} at bearing {bearing}, r {r}, y {y} has no floor at that height under it.");
                return at;
            }

            int n = 0;
            foreach (var token in layout["jumpPads"] as JArray ?? new JArray())
            {
                var pad = Group(group, "JumpPad " + n);
                pad.position = At(token, $"Layout '{name}' jump pad {n++}");
                var jump = pad.gameObject.AddComponent<JumpPad>();
                jump.LaunchSpeed = Number(token["speed"], JumpPadSpeed);
                // This map's own pad: the stage kit's round teal one. Without the kit, `JumpPad`'s
                // defaults stand, which are the pavement pad.
                var padModel = ArenaArtPlacer.FindModel(PropsFolder, "arena_jump_pad");
                var padPaint = AssetDatabase.LoadAssetAtPath<Texture2D>(ArenaArtPlacer.Root + "/Textures/arena_stage_props.png");
                if (padModel != null && padPaint != null)
                {
                    jump.Model = padModel; jump.Paint = padPaint; jump.PartPrefix = "jump_";
                    jump.ModelHalfSize = JumpPadHalf; jump.Round = true; jump.GlowColour = JumpGlow;
                    jump.SurfaceShader = ArenaArtPlacer.PaintedShader;
                }
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
                // The kit's pad is 1.6 by 3.0 m; it is drawn the size the layout's pad acts on.
                PropModel(pad, "arena_speed_pad", new Vector3(speed.HalfSize.x / SpeedPadHalf.x, 1.0f, speed.HalfSize.y / SpeedPadHalf.y));
            }

            n = 0;
            foreach (var token in layout["pickups"] as JArray ?? new JArray())
            {
                var pickup = Group(group, "StaminaPickup " + n);
                pickup.position = At(token, $"Layout '{name}' pickup {n++}");
                pickup.gameObject.AddComponent<ArenaStaminaPickup>();
                PropModel(pickup, "arena_pickup", Vector3.one);
            }
        }

        // ------------------------------------------------------------------ what never moves

        /// <summary>
        /// The match installer, the kill plane, the spawn markers and the Bounds: four invisible
        /// box walls (`MatchInstaller.MeasurePlayableBounds` reads boxes) with their inner faces
        /// on +/- half. They run from under the KILL PLANE to `wallHeight` above the stage, so a
        /// body falling down the shaft is inside them all the way to the catch (`catchY`, about
        /// y -22) and past it: a body that drifted out under a wall's foot would be refused by
        /// `MatchRpc.AcceptMove` for being outside the walls, and the host would never see it
        /// reach the catch. The plane itself is this map's own (`KillPlaneY`), far under the
        /// catch: the last resort.
        /// </summary>
        private static void Gameplay(Transform root, JToken spawns, float half, float wallHeight, float stageTop)
        {
            Group(root, "~Match").gameObject.AddComponent<MatchInstaller>();
            var kill = Group(root, "KillPlane");
            kill.localPosition = new Vector3(0, KillPlaneY, 0);
            kill.gameObject.AddComponent<KillPlane>().Height = KillPlaneY;
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
            float top = stageTop + wallHeight, bottom = KillPlaneY - 2.0f, height = top - bottom, y = (top + bottom) * 0.5f;
            float span = 2 * (half + WallThickness), centre = half + WallThickness * 0.5f;
            Wall(bounds, "WallWest", new Vector3(-centre, y, 0), new Vector3(WallThickness, height, span));
            Wall(bounds, "WallEast", new Vector3(centre, y, 0), new Vector3(WallThickness, height, span));
            Wall(bounds, "WallSouth", new Vector3(0, y, -centre), new Vector3(span, height, WallThickness));
            Wall(bounds, "WallNorth", new Vector3(0, y, centre), new Vector3(span, height, WallThickness));
        }

        /// <summary>
        /// The map's effects: the pooled sparks, rings and streaks every effect here is drawn
        /// with (`ArenaFx`), the transformation's show (`ArenaShow`) and the ambient and reactive
        /// effects (`ArenaAmbience`). Each also installs itself at run time if a scene built
        /// before it existed is played (`ArenaFx.Ensure`), so this is for a rebuilt scene to
        /// carry them plainly, where they can be found and tuned.
        /// </summary>
        private static void Effects(Transform root)
        {
            var effects = Group(root, "Effects").gameObject;
            effects.AddComponent<ArenaFx>();
            effects.AddComponent<ArenaShow>();
            effects.AddComponent<ArenaAmbience>();
            // The crowd's sound and the stadium's PA (`ArenaAmbience` adds it to a scene built before it existed).
            effects.AddComponent<ArenaCrowdAudio>();
        }

        /// <summary>The break's camera (`ArenaBreakCamera` poses it every frame of a break). It
        /// rises over the stage and carries no outline pass, so its far plane is the whole map's
        /// (`FreeFar`).</summary>
        private static void BreakCamera(Transform root, float radius)
        {
            var eye = Group(root, "BreakCamera").gameObject;
            var camera = eye.AddComponent<Camera>();
            camera.enabled = false;
            camera.depth = ArenaBreakCamera.Depth;
            camera.fieldOfView = 50.0f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = FreeFar;
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

        /// <summary>A point of a kit's own data file, which is in the BLENDER frame (metres, z
        /// up, y north): Blender (x, y, z) is Unity (x, z, y). The same rule the export proves
        /// for the models (tools/export_arena_unity.py).</summary>
        private static Vector3 B(JToken token)
        {
            var v = Vector(token);
            return new Vector3(v.x, v.z, v.y);
        }

        /// <summary>
        /// THE FLOODLIGHTS, as the players see them: a BEAM through the haze from each of the 24
        /// banks tools/arena_lights.json lists under `roof.floodlight_banks` (the roof kit
        /// measured each bank's `position`, the middle of its 9.5 by 1.9 m lamp face, and its
        /// `aim_point` on the stage; Blender frame), and four small lights over the stage.
        ///
        /// A BEAM IS EIGHT TRIANGLES: an open fan from the lamp face, widening, whole at the lamp
        /// and gone by `BeamShare` of the way to its aim, so it stops above the field and never
        /// washes over the play. One mesh, one added material (TumbangPreso/ArenaGlow: no depth
        /// write, soft sides, faded out near the eye so a camera flown through one is not
        /// blinded), all 24 batched: one draw call.
        ///
        /// ⚠️ THE BANKS ARE NOT LIGHTS. The first build made each a Light: 24 lamps a frame to
        /// cull and sort, lighting nothing a shader here takes per pixel. The stadium is lit by
        /// the ONE directional key (`Lighting`), which stands in for all of them, and the lamp
        /// faces glow by their own emission (arena_roof_lamp, strength 8). What a real light
        /// buys is the stage standing out from the field (the art brief's rule 8), so there are
        /// four, low over the stage, with no shadow and never per pixel (`ForceVertex`:
        /// TumbangPreso/ArenaPainted takes them per vertex in its one pass).
        /// </summary>
        private static int Floodlights(Transform root, float stageRadius)
        {
            var parent = Group(root, "Floodlights");
            for (int i = 0; i < 4; i++)
            {
                var light = Group(parent, "StageLight " + i).gameObject.AddComponent<Light>();
                light.transform.localPosition = ArenaStageMesh.Direction(45.0f + 90.0f * i) * (stageRadius * StageLightOut) + Vector3.up * StageLightHeight;
                light.type = LightType.Point;
                light.range = StageLightHeight + stageRadius * 1.2f;
                light.intensity = StageLightIntensity;
                light.color = new Color(0.93f, 0.96f, 1.0f);
                light.shadows = LightShadows.None;
                light.renderMode = LightRenderMode.ForceVertex;
            }

            if (!File.Exists(LightsPath)) return 0;

            var doc = JToken.Parse(File.ReadAllText(LightsPath));
            var banks = doc["roof"] != null ? doc["roof"]["floodlight_banks"] as JArray : null;
            if (banks == null) { Debug.LogWarning($"{Tag}{LightsPath} has no roof.floodlight_banks list: no beams"); return 0; }

            var shader = Shader.Find(ArenaArtPlacer.GlowShader);
            if (shader == null) { Debug.LogWarning($"{Tag}{ArenaArtPlacer.GlowShader} is missing: no beams"); return 0; }

            EnsureFolder(GeneratedFolder);
            string meshPath = GeneratedFolder + "/arena_floodlight_beam.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            bool fresh = mesh == null;
            if (fresh) mesh = new Mesh();
            BeamMesh(mesh);
            mesh.name = "arena_floodlight_beam";
            if (fresh) AssetDatabase.CreateAsset(mesh, meshPath); else EditorUtility.SetDirty(mesh);

            EnsureFolder(MaterialFolder);
            string materialPath = MaterialFolder + "/arena_floodlight_beam.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            // Made once: after that its colour and strength are the artist's to tune, and a rebuild leaves them alone.
            if (material == null)
            {
                material = new Material(shader);
                material.SetColor("_Color", new Color(0.82f, 0.90f, 1.0f, 0.085f));
                material.SetFloat("_Rim", 1.5f);
                material.SetVector("_Near", new Vector4(20.0f, 70.0f, 0.0f, 0.0f));
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)BlendMode.One);
                material.SetFloat("_Cull", (float)CullMode.Off);
                material.SetFloat("_Fog", 1.0f);
                material.renderQueue = (int)RenderQueue.Transparent + 20;
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.shader = shader;
            material.enableInstancing = true;

            int n = 0;
            foreach (var bank in banks)
            {
                if (bank["position"] == null || bank["aim_point"] == null) continue;

                Vector3 at = B(bank["position"]), aim = B(bank["aim_point"]);
                if ((aim - at).sqrMagnitude < 1.0f) continue;

                var beam = Group(parent, (string)bank["name"] ?? "Beam " + n);
                beam.SetPositionAndRotation(at, Quaternion.LookRotation(aim - at, Vector3.up));
                // The mesh is a unit fan: one lamp face wide, one tall, one long.
                beam.localScale = new Vector3(Number(bank["face_width_m"], 9.5f), Number(bank["face_height_m"], 1.9f), Vector3.Distance(at, aim) * BeamShare);
                beam.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = beam.gameObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                GameObjectUtility.SetStaticEditorFlags(beam.gameObject, StaticEditorFlags.BatchingStatic);
                n++;
            }

            return n;
        }

        /// <summary>
        /// One beam, in its own space: it leaves along +z from a 1 by 1 opening at the origin and
        /// is `BeamFar` times wider and taller one unit out (the object's scale makes that the
        /// lamp face and the beam's length). Four open sides, each with its own outward normal
        /// (the shader's soft sides read it), alpha 1 at the lamp and 0 at the far end.
        /// </summary>
        private static void BeamMesh(Mesh mesh)
        {
            var near = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(-0.5f, 0.5f, 0) };
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var colours = new List<Color>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            for (int side = 0; side < 4; side++)
            {
                Vector3 a = near[side], b = near[(side + 1) % 4];
                Vector3 a1 = new Vector3(a.x * BeamFar.x, a.y * BeamFar.y, 1.0f), b1 = new Vector3(b.x * BeamFar.x, b.y * BeamFar.y, 1.0f);
                // The opening runs counter-clockwise seen from behind the lamp, so this is outward.
                Vector3 normal = Vector3.Cross(b - a, a1 - a).normalized;
                int first = vertices.Count;
                vertices.AddRange(new[] { a, b, b1, a1 });
                for (int k = 0; k < 4; k++) normals.Add(normal);
                colours.AddRange(new[] { Color.white, Color.white, new Color(1, 1, 1, 0), new Color(1, 1, 1, 0) });
                uvs.AddRange(new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) });
                triangles.AddRange(new[] { first, first + 1, first + 2, first, first + 2, first + 3 });
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetColors(colours);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
        }

        /// <summary>
        /// THE SKY TRAFFIC AND THE TRAIN, from tools/arena_traffic.json (the city kit's): `lanes`,
        /// each a closed loop of `points_blender`, a `speed_mps`, a `count` of craft, what each is
        /// (`craft`, cycled: kotse, dyip, barge, which are Models/city_craft_KIND.glb, nose along
        /// +z) and where each starts (`phase`, a share of the loop). `ArenaTraffic` moves them.
        ///
        /// ⚠️ `points_blender`, NOT `points_unity`. The city kit wrote the second as (-x, z, -y),
        /// which is where a raw glTF export lands: half a turn from the game's frame, in which
        /// the stadium, the layouts and every other number of this map stand (see `B`).
        ///
        /// The lane named for the train (`craft` is `city_train`) is not a path. The train is
        /// one model already bent to its rail loop, placed with the city; it is turned about the
        /// can at the lane's speed over the loop's radius, the way the loop's points run.
        /// </summary>
        private static int Traffic(Transform root)
        {
            if (!File.Exists(TrafficPath)) return 0;

            var doc = JToken.Parse(File.ReadAllText(TrafficPath));
            var lanes = doc["lanes"] as JArray;
            if (lanes == null) { Debug.LogWarning($"{Tag}{TrafficPath} has no list of lanes"); return 0; }

            var parent = Group(root, "SkyTraffic");
            var traffic = parent.gameObject.AddComponent<ArenaTraffic>();
            var paths = new List<ArenaTraffic.Path>();
            var spinners = new List<ArenaTraffic.Spinner>();
            Material standIn = null;
            int total = 0;
            foreach (var lane in lanes)
            {
                string name = (string)lane["name"] ?? "Lane " + paths.Count;
                var points = new List<Vector3>();
                foreach (var p in lane["points_blender"] as JArray ?? new JArray()) points.Add(B(p));
                if (points.Count < 3) { Debug.LogWarning($"{Tag}Traffic lane '{name}' needs three points_blender or more; skipped"); continue; }

                float speed = Number(lane["speed_mps"], 18.0f);
                var kinds = lane["craft"] as JArray ?? new JArray();
                if (kinds.Count > 0 && (string)kinds[0] == TrainObject)
                {
                    var train = root.Find("Dressing/Train/" + TrainObject);
                    if (train == null) { Debug.LogWarning($"{Tag}The traffic file has the train's lane but the city placed no {TrainObject}"); continue; }

                    float radius = 0.0f;
                    foreach (var p in points) radius += new Vector2(p.x, p.z).magnitude / points.Count;
                    // Which way the loop's points run: a bearing that grows is clockwise, which is a positive yaw.
                    float turn = Mathf.DeltaAngle(Mathf.Atan2(points[0].x, points[0].z) * Mathf.Rad2Deg, Mathf.Atan2(points[1].x, points[1].z) * Mathf.Rad2Deg);
                    if (radius < 1.0f) continue;
                    spinners.Add(new ArenaTraffic.Spinner { Body = train, DegreesPerSecond = Mathf.Sign(turn) * speed / radius * Mathf.Rad2Deg });
                    continue;
                }

                int count = Mathf.Clamp((int)Number(lane["count"], 1.0f), 1, 64);
                var phases = new List<float>();
                foreach (var phase in lane["phase"] as JArray ?? new JArray()) phases.Add((float)phase);
                var craft = new Transform[count];
                for (int i = 0; i < count; i++)
                {
                    string kind = kinds.Count > 0 ? (string)kinds[i % kinds.Count] : null;
                    var model = string.IsNullOrEmpty(kind) ? null : ArenaArtPlacer.FindModel("Models", CraftPrefix + kind);
                    GameObject go;
                    if (model != null)
                    {
                        go = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
                        ArenaArtPlacer.Dress(go, false);
                    }
                    else
                    {
                        go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        go.transform.SetParent(parent, false);
                        go.transform.localScale = new Vector3(2.5f, 1.2f, 6.0f);
                        Object.DestroyImmediate(go.GetComponent<Collider>());
                        if (standIn == null) standIn = Flat("arena_grey_craft", CraftColour, 0.4f);
                        var renderer = go.GetComponent<Renderer>();
                        renderer.sharedMaterial = standIn;
                        renderer.shadowCastingMode = ShadowCastingMode.Off;
                    }

                    go.name = $"{name} {kind ?? "craft"} {i}";
                    go.transform.position = points[0];
                    craft[i] = go.transform;
                }

                paths.Add(new ArenaTraffic.Path { Name = name, Points = points.ToArray(), Speed = speed, Craft = craft, Phases = phases.ToArray() });
                total += count;
            }

            traffic.Paths = paths.ToArray();
            traffic.Spinners = spinners.ToArray();
            return total;
        }

        // ------------------------------------------------------------------ light, sky and haze

        /// <summary>
        /// A NIGHT MATCH. The scene as authored matches the "Arena" `WorldLookProfile` row exactly
        /// (the Lagoon Cove and Ilalim pattern), so the look's weight in Play changes only the
        /// extras it owns (ramp, grade, edges):
        ///   * the KEY is one directional light standing in for the floodlights: high, cool
        ///     white, the row's colour, intensity and shadow strength. The row's elevation is 0,
        ///     so Play keeps this angle. It is the only light that casts a shadow, and only the
        ///     stage and the players cast one (`ArenaArtPlacer`);
        ///   * the AMBIENT trilight (every shadow's colour here) is the row's deep navy: low, so
        ///     the stands and the city are carried by what glows, and sit back from the stage;
        ///   * the FOG is the row's dark indigo, linear from 180 m to 3400 m: the bowl is clear,
        ///     a tower 900 m out keeps 78 per cent of itself, distance reads and nothing is hidden;
        ///   * the SKY is the city kit's painted night panorama (`Sky`).
        /// ⚠️ NOT THROUGH `MapAtmosphereAuthor.Apply`, which every daylight map uses: it gives the
        /// scene a gradient sky with a daylight cloud photograph and turns every directional
        /// light to an afternoon sun. This map has ONE sky, the painting; the material is kept
        /// where that author looks for every map's (Art/MapAtmosphere/ArenaSky.mat).
        /// BLOOM is `ColourGrade`'s, under the Standard lighting style only (threshold 2.2, the
        /// look profile's): the lamp faces (8), the hull's glow (7), the pads' lights (3) and the
        /// stage's rim (3.2) pass it; the screens, the LED rows and the city (0.7 to 1.6) do not.
        /// </summary>
        private static void Lighting(Transform root)
        {
            var sun = new GameObject("FloodlightKey").AddComponent<Light>();
            sun.transform.SetParent(root, false);
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(62.0f, 28.0f, 0.0f);
            sun.shadows = LightShadows.Soft;
            sun.shadowBias = 0.025f;
            sun.shadowNormalBias = 0.16f;
            RenderSettings.sun = sun;

            var look = EnsureLook();
            sun.color = look.Sun; sun.intensity = look.SunIntensity; sun.shadowStrength = look.ShadowStrength;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientIntensity = 1.0f;
            RenderSettings.ambientSkyColor = look.Sky; RenderSettings.ambientEquatorColor = look.Equator; RenderSettings.ambientGroundColor = look.Ground;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = look.Fog; RenderSettings.fogStartDistance = look.FogStart; RenderSettings.fogEndDistance = look.FogEnd;
            RenderSettings.skybox = Sky(look, sun);
            if (RenderSettings.skybox == null) Debug.LogWarning($"{Tag}No painted sky: the scene has a plain dark background.");
        }

        /// <summary>
        /// The sky: Art/MapAtmosphere/ArenaSky.mat on TumbangPreso/ArenaSky, drawing
        /// Art/Arena/Textures/arena_city_sky.png (u is the bearing / 360 from north, clockwise).
        /// The texture is imported whole (4096 across), with no mipmaps, wrapping round the
        /// compass and clamped at the poles. The zenith and horizon colours are the look row's:
        /// the shader does not draw them, the look system reads them (see the shader). Its tint
        /// and exposure are the neutral pair every map's sky has, which `SkyEvent` moves in Play.
        /// </summary>
        private static Material Sky(WorldLookProfile.MapLook look, Light sun)
        {
            var shader = Shader.Find(SkyShader);
            if (shader == null) { Debug.LogWarning($"{Tag}{SkyShader} is missing"); return null; }

            var panorama = File.Exists(SkyTexture) ? ArenaArtPlacer.ImportTexture(SkyTexture, false, true, false, false, false, 4096) : null;
            if (panorama == null) { Debug.LogWarning($"{Tag}{SkyTexture} is missing"); return null; }

            const string folder = "Assets/TumbangPreso/Art/MapAtmosphere";
            EnsureFolder(folder);
            string path = folder + "/" + SceneName + "Sky.mat";
            var sky = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (sky == null) { sky = new Material(shader); AssetDatabase.CreateAsset(sky, path); }
            sky.shader = shader;
            sky.SetTexture("_MainTex", panorama);
            sky.SetColor("_Zenith", look.Zenith);
            sky.SetColor("_Horizon", look.Horizon);
            sky.SetColor("_Tint", new Color(0.5f, 0.5f, 0.5f));
            sky.SetFloat("_Exposure", 1.0f);
            sky.SetVector("_SunDirection", -sun.transform.forward);
            EditorUtility.SetDirty(sky);
            return sky;
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

        /// <summary>The hologram's material. Made once: after that its numbers are the artist's
        /// to tune and a rebuild leaves them alone. Its two pictures are the stage kit's and are
        /// given again on every build: the hologram's hexagon lines and the deck it turns into.</summary>
        private static Material HologramMaterial()
        {
            EnsureFolder(MaterialFolder);
            string path = $"{MaterialFolder}/arena_stage_hologram.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("TumbangPreso/ArenaHologram");
                if (shader == null)
                {
                    Debug.LogWarning($"{Tag}TumbangPreso/ArenaHologram is missing: the hologram is a plain transparent sprite material.");
                    shader = Shader.Find("Sprites/Default");
                }

                material = new Material(shader) { color = new Color(0.45f, 0.95f, 1.0f, 0.5f), enableInstancing = true };
                AssetDatabase.CreateAsset(material, path);
            }

            if (material.HasProperty("_HoloTex") && File.Exists(ArenaArtPlacer.Root + "/Textures/arena_stage_holo.png"))
                material.SetTexture("_HoloTex", ArenaArtPlacer.ImportTexture("arena_stage_holo.png", false, false, true, true, false));
            if (material.HasProperty("_DeckTex") && File.Exists(ArenaArtPlacer.Root + "/Textures/arena_stage_deck.png"))
                material.SetTexture("_DeckTex", ArenaArtPlacer.ImportTexture("arena_stage_deck.png", false, false, true, false, false));
            EditorUtility.SetDirty(material);
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
