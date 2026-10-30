using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using TumbangPreso.Map;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Shape = TumbangPreso.Map.ArenaStage.Shape;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// PAYO, THE SCENE: `Scenes/Maps/Payo.unity`, built from the game's copy of the layout
    /// (`Art/Payo/payo_layout.json`, read by `Map.PayoLayout`). docs/PAYO_MAP_BRIEF.md has the owner's words.
    /// ⚠️ AN UNREGISTERED SAMPLE: it is in no map list, no menu, no picker and no build settings. It is opened and
    /// played in the editor.
    ///
    /// WHAT IT BUILDS, in the Arena's own stage system (`ArenaStage` with `Stone` set, pieces of kind Block):
    ///  * "Stage": per layout a set of SOLID BOX colliders (one box per monolith from its top down its shaft; a stair
    ///    is a body box and one tilted box, 11 degrees, no steps to snag on), a drawn stand-in monolith per block
    ///    (`ArenaStageMesh` block: a pale top, a dressed cap, a darker shaft), and the layout's features: geysers
    ///    (`JumpPad`), speed pads, stamina pickups, the slabs that move during a round (`PayoMover`), and the bots'
    ///    way points on the walk set (`MapRoutes`, baked against that layout's own collision).
    ///  * The match: `MatchInstaller`, spawn marks, the play walls, the kill plane, the catch (`ArenaFallRecovery`),
    ///    the pooled light the catch drone draws with (`ArenaFx`), the break's camera (`ArenaBreakCamera`).
    ///  * "StandIn": pit walls with the north wall broken open, sheets of mist, a plaza, a fan of rice terraces that
    ///    climbs to the south and west and falls away to the north and east, and far hills and ridges, so the
    ///    horizon is not empty and the place is open to a valley on one side. Drawn only: nobody stands on it.
    ///  * The LOOK: the "Payo" row of `WorldLookProfile` (added to the profile asset with "PayoNight" if it has
    ///    neither), and `PayoNight` and `PayoNightLife` for the half-time night, with Mayari on a card in the north sky.
    ///
    /// THE ART IS OPTIONAL, and taken the way Paete's run takes its art. If `Art/Payo/payo_art.json` exists:
    ///  * `placements` ([{model, position [x, y, z], yaw, scale}], Unity axes): `Models/MODEL.glb` placed under
    ///    "Dressing", colliders removed. A placement with `"block": ID` is instead hung on that block in every
    ///    layout it stands in, `position` then being from the middle of the block's top, so it rises and sinks
    ///    with it. A placement with `"night": true` exists only in the night half.
    ///  * a model named `payo_block_ID.glb` (its origin the middle of the block's TOP, its shaft hanging down) is
    ///    drawn for that block in place of the stand-in; `payo_block_ID_stair.glb` where the block is a flight.
    ///  * `hide_solids` true: no stand-in monolith is drawn (the art covers them) and the "StandIn" scenery is not built.
    ///  * `night`: model names whose renderers glow only at night (their emission follows the night).
    ///  * `sky`: {model, position, yaw, scale}: Mayari. She replaces the placeholder card and is shown only at night.
    ///  * `materials_payo.json` beside it names the materials (`py_*`). They are built on the shaders the Lagoon and the
    ///    alley wear (painted, foliage, rock, the cove's water), not on plain Standard: see `ArtMaterials`.
    ///
    /// DRIVEN FROM A SHELL: `Temp/payo.request` holding `build`, `review`, `probe` or `probe 240`
    /// (<see cref="PayoRequestWatcher"/>); the answer is `Temp/payo.done`.
    /// </summary>
    public static partial class PayoSceneBuilder
    {
        public const string MapName = "Payo";
        public const string ScenePath = "Assets/TumbangPreso/Scenes/Maps/Payo.unity";
        public const string Root = "Assets/TumbangPreso/Art/Payo";
        public const string LayoutPath = Root + "/payo_layout.json";
        private const string ArtPath = Root + "/payo_art.json", MaterialsPath = Root + "/materials_payo.json";
        private const string Generated = Root + "/Generated";
        private const string MayariPlaceholder = Root + "/Placeholder/payo_mayari_placeholder.png";
        private const string Tag = "[Payo] ";

        /// <summary>The sun by day: 46 degrees up (the look row's), travelling from over the valley in the north east.
        /// The moon: lower, from the north, where Mayari stands.</summary>
        // The sun stands in the valley's open end, due north (owner, 2026-10-18: "sun is off center"; it was 150, about 30
        // degrees west of north).
        public const float SunYaw = 180.0f, MoonYaw = 193.0f, MoonElevation = 22.0f;   // 22: the moonlight and its sky glow sit on the cute moon (PayoMoon, 22 degrees up), 2026-10-19
        /// <summary>The cameras see across the valley; the ink is whole on the stage and gone before the terraces.</summary>
        /// ⚠️ 3.7 km: the art's Mayari stands 2.9 km out beyond a last range at 2.7 km, and the stars ride 3.35 km out.
        private const float PlayFar = 3700.0f, FreeFar = 4200.0f, InkFadeStart = 45.0f, InkFadeEnd = 95.0f;
        private const float MoveFloorY = ArenaStage.DefaultMoveFloorY, KillPlaneY = ArenaStage.DefaultKillPlaneY;
        private const float WallHeight = 14.0f, WallThickness = 0.4f;
        /// <summary>The ground round the pit lies this far under the court (the draft: "the court stands 6 m above the pit lip").</summary>
        public const float LipY = -6.0f;
        /// <summary>The north pit wall is broken open this wide toward the valley.</summary>
        private const float NotchHalf = 12.5f;
        private const float MoverThick = 0.6f, RampThick = 2.0f;

        internal static readonly List<string> Warnings = new List<string>();

        private static void Warn(string message)
        {
            if (Warnings.Contains(message)) return;
            Warnings.Add(message);
            Debug.LogWarning(Tag + message);
        }

        [MenuItem("Tumbang Preso/Sample Map/Build Payo")]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        internal static PayoLayout ReadLayout()
        {
            if (!File.Exists(LayoutPath)) throw new FileNotFoundException("Missing layout " + LayoutPath);
            return PayoLayout.Parse(File.ReadAllText(LayoutPath));
        }

        private sealed class Art
        {
            public JObject Doc;
            public bool HideSolids;
            public readonly HashSet<string> Night = new HashSet<string>();
            /// <summary>By block id: the art's model for it, and for it as a flight of stairs.</summary>
            public readonly Dictionary<string, string> BlockModel = new Dictionary<string, string>(), StairModel = new Dictionary<string, string>();
            public readonly Dictionary<string, string> MoverModel = new Dictionary<string, string>();
            /// <summary>By block id: the turn its model is placed with (and its stair's), and the plain shaft hung under it.</summary>
            public readonly Dictionary<string, float> BlockYaw = new Dictionary<string, float>(), StairYaw = new Dictionary<string, float>();
            public readonly Dictionary<string, string> Extension = new Dictionary<string, string>();
            /// <summary>By block id: d53's few-millimetre height offset (every top a little apart, so no two share a plane and flicker).</summary>
            public readonly Dictionary<string, float> BlockDy = new Dictionary<string, float>();
            public string Geyser, SpeedPad, Plinth;
            /// <summary>The stamina orb (cell and halo) hung over each plinth as `ArenaStaminaPickup`'s own model (tools/payo_pieces/orb_build.py).</summary>
            public string Orb = "pickup_orb";
            public bool HasFires;
            /// <summary>The art says every model carries vertex colour and every material but the leaf cards must multiply by it.</summary>
            public bool VertexColour;
            public JArray Life => Doc != null ? Doc["life"] as JArray ?? new JArray() : new JArray();
            public JArray Lights => Doc != null && Doc["night"] is JObject n ? n["lights"] as JArray ?? new JArray() : new JArray();
            public JArray Placements => Doc != null ? Doc["placements"] as JArray ?? new JArray() : new JArray();
        }

        private static string Str(JToken token) => token != null && token.Type == JTokenType.String ? (string)token : null;
        private static float Num(JToken token, float fallback) => token != null && (token.Type == JTokenType.Float || token.Type == JTokenType.Integer) ? (float)token : fallback;
        private static bool Yes(JToken token) => token != null && token.Type == JTokenType.Boolean && (bool)token;

        private static GameObject Model(string name) =>
            string.IsNullOrEmpty(name) ? null : AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Models/{name}.glb");

        /// <summary>The art's model hung under a parent at a local place, wearing the map's materials, with no collider.</summary>
        private static GameObject Hang(string model, Transform parent, Vector3 at, string name, Dictionary<string, Material> named)
        {
            var prefab = Model(model);
            if (prefab == null) { if (!string.IsNullOrEmpty(model)) Warn("missing model Models/" + model + ".glb"); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.transform.localPosition = at;
            Rematerial(go, named);
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            Finish(go, false);          // a hung piece rides something that moves, or is animated: never static
            return go;
        }

        private static Art ReadArt()
        {
            var art = new Art();
            if (!File.Exists(ArtPath)) return art;
            try
            {
                art.Doc = JObject.Parse(File.ReadAllText(ArtPath));
                art.HideSolids = art.Doc["hide_solids"] != null && art.Doc["hide_solids"].Type == JTokenType.Boolean && (bool)art.Doc["hide_solids"];
                foreach (var b in art.Doc["blocks"] as JArray ?? new JArray())
                {
                    string id = Str(b["id"]);
                    if (id == null) continue;
                    if (Str(b["model"]) != null) art.BlockModel[id] = Str(b["model"]);
                    art.BlockYaw[id] = Num(b["yaw"], 0.0f);
                    // Only ever a few millimetres DOWN (the art then stays under its collision top): anything else is ignored.
                    float dy = Num(b["dy"], 0.0f);
                    if (dy < 0.0f && dy > -0.1f) art.BlockDy[id] = dy;
                    if (Str(b["extension"]) != null) art.Extension[id] = Str(b["extension"]);
                    if (b["stair"] is JObject stair && Str(stair["model"]) != null) { art.StairModel[id] = Str(stair["model"]); art.StairYaw[id] = Num(stair["yaw"], art.BlockYaw[id]); }
                }
                if (art.Doc["movers"] is JObject movers)
                    foreach (var pair in movers) if (pair.Value is JObject m && Str(m["model"]) != null) art.MoverModel[pair.Key] = Str(m["model"]);
                if (art.Doc["pads"] is JObject pads) { art.Geyser = Str(pads["jumpPads"]); art.SpeedPad = Str(pads["speedPads"]); art.Plinth = Str(pads["pickups"]); art.Orb = Str(pads["pickupOrb"]) ?? art.Orb; }
                foreach (var p in art.Placements) if (Str(p["model"]) == "fire_bowl" || Str(p["model"]) == "fire_flame") art.HasFires = true;
                art.VertexColour = art.Doc["vertex_colour"] != null;
            }
            catch (Exception e) { Warn("unreadable payo_art.json: " + e.Message); art.Doc = null; }
            return art;
        }

        // ------------------------------------------------------------------ the build

        /// <summary>Builds and saves the scene. Returns one line saying what was built.</summary>
        public static string Build()
        {
            Warnings.Clear();
            var layout = ReadLayout();
            var art = ReadArt();
            EnsureFolder(Generated);
            _vertexColour = art.VertexColour;
            var named = ArtMaterials();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject(MapName).transform;
            root.gameObject.AddComponent<MapGrade>().Set(1.0f, 1.03f, 1.06f, 1.0f, 1.9f);
            var range = root.gameObject.AddComponent<MapCameraRange>();
            range.PlayFar = PlayFar; range.FreeFar = FreeFar; range.InkFadeStart = InkFadeStart; range.InkFadeEnd = InkFadeEnd;

            var sun = Lighting(root, out var day);
            _art = art; _named = named;
            var stage = Stage(root, layout, art, named, out string stageSaid, out var nightOnly, out var nightGlow);
            Gameplay(root, layout);

            var effects = Group(root, "Effects").gameObject;
            effects.AddComponent<ArenaFx>();                                   // the catch drone draws with it; a stone stage gets no show
            var eye = Group(root, "BreakCamera").gameObject;
            var camera = eye.AddComponent<Camera>();
            camera.enabled = false; camera.depth = ArenaBreakCamera.Depth; camera.fieldOfView = 50.0f;
            camera.nearClipPlane = 0.3f; camera.farClipPlane = FreeFar;
            eye.transform.SetPositionAndRotation(new Vector3(26.0f, 22.0f, 34.0f), Quaternion.LookRotation(new Vector3(-26.0f, -22.0f, -34.0f)));
            eye.AddComponent<ArenaBreakCamera>();

            int placed = 0, missing = 0;
            Renderer[] mayari = null;
            if (art.Doc != null) placed = Dressing(root, layout, art, named, nightOnly, nightGlow, ref missing, out mayari);
            if (art.Doc != null) { try { _farSaid += "; " + FarTown(root); } catch (Exception e) { Warn("far houses: FAILED, the houses keep their own materials: " + e.Message); Debug.LogException(e); } }   // the far houses' cheap LOD1 (PayoSceneBuilder.FarTown.cs), before Night reads the glows
            if (art.Doc != null) { try { Debug.Log(Tag + Plaza(root, named)); } catch (Exception e) { Warn("plaza: FAILED, the map is built without it: " + e.Message); Debug.LogException(e); } }   // the basin's dressing (PayoSceneBuilder.Plaza.cs)
            if (art.Doc != null) { try { Debug.Log(Tag + NightFires(root, named)); } catch (Exception e) { Warn("night fires: FAILED, the map is built without them: " + e.Message); Debug.LogException(e); } }   // fire pedestals and torches for the night (PayoSceneBuilder.Fire.cs), after every other dressing so they keep clear of it
            // 2026-10-29, the owner: "go ahead and merge the plaza dressing" (PayoSceneBuilder.PlazaMerge.cs), before Night reads the glows
            if (art.Doc != null) { try { Debug.Log(Tag + PlazaMerge(root)); } catch (Exception e) { Warn("plaza merge: FAILED, the plaza keeps its pieces: " + e.Message); Debug.LogException(e); } }
            if (art.Doc != null) { try { Debug.Log(Tag + MeshLodStep(root)); } catch (Exception e) { Warn("mesh LOD: FAILED, the far pieces keep full detail: " + e.Message); Debug.LogException(e); } }   // 2026-10-30 owner: "simplify the places that are further from the arena" (PayoSceneBuilder.MeshLevels.cs), before the AO bake so it bakes every level
            if (art.Doc != null) { try { Debug.Log(Tag + BakedAOStep(root)); } catch (Exception e) { Warn("baked AO: FAILED, the map keeps the screen AO: " + e.Message); Debug.LogException(e); } }   // 2026-10-29 Payo bake test (PayoSceneBuilder.BakedAO.cs)
            // Rice on the paddies and grass on the flat ground near the court, baked onto the land just placed (docs/PAYO_BLADES.md).
            string blades = "no blades (no art)";
            if (art.Doc != null)
            {
                try { blades = PayoBladesBaker.Bake(root); }
                catch (Exception e) { blades = "blades: FAILED, the map is built without them: " + e.Message; Warn(blades); Debug.LogException(e); }
            }
            // The stand-in mist sheets (payo_standin_mist_*) went with the stand-in: the approved art (2026-10-18) has the pit's own
            // pool and falls, and the opaque bottom sheet hid them behind a pale pane.
            string scenery = art.HideSolids ? "no stand-in scenery or stand-in mist (hide_solids)" : StandIn(root, layout) + "; " + Mist(root, layout);
            string night = Night(root, layout, sun, mayari, nightOnly, nightGlow);
            night += "; " + Moon(root);   // the cute moon and Mayari of the stars as its reward (PayoSceneBuilder.Moon.cs)
            string alive;
            try { alive = Life(root, layout, stage, art); }
            catch (Exception e) { alive = "life: FAILED, the map is built without it: " + e.Message; Warn(alive); Debug.LogException(e); }
            if (art.Doc != null) _plantSaid += "; " + InkClear(root);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Could not save " + ScenePath);
            AssetDatabase.SaveAssets();
            string summary = $"built {ScenePath} from {LayoutPath} ({layout.Layouts.Count} layouts: {string.Join(", ", layout.Layouts.Select(l => l.Name))}); {stageSaid}; " +
                             $"{(art.Doc != null ? placed + " art pieces placed, " + missing + " missing; " + _plantSaid + "; " + _farSaid + "; " + _faceUp + " upside-down river meshes turned up; " + Collided + " mesh colliders on collide placements; " + blades + "; " + _terraceSaid + "; " + named.Count + " materials built of " + Specs.Count + " specs" : "no payo_art.json: the stand-in look")}; {scenery}; {night}; {alive}; " +
                             $"sun yaw {SunYaw:0} at {day.SunElevation:0} degrees, cameras to {PlayFar:0} m (play) and {FreeFar:0} m (free); NOT in any map list or build settings";
            Debug.Log(Tag + summary);
            return summary;
        }

        // ------------------------------------------------------------------ the stage

        private static Shape ShapeOf(PayoLayout.Block b) => new Shape
        {
            Exists = true, Kind = ArenaStage.ShapeKind.Block, X = b.X, Z = b.Z, SX = b.SX, SZ = b.SZ,
            Top = b.Top, Top1 = b.Stair != 0 ? b.Low : b.Top, Stair = b.Stair, Thick = Mathf.Max(1.0f, b.Top - b.ShaftTo), Sweep = 360.0f,
            // Only a block on the WALK set is a place to set a fallen body or a returned slipper down: a body left
            // on a stone that needs a running jump could be a bot, and a bot does not jump.
            Bonus = b.Reach != PayoLayout.Reach.Walk,
        };

        private static string Key(in Shape s) => FormattableString.Invariant($"{s.X:F2}_{s.Z:F2}_{s.SX:F2}_{s.SZ:F2}_{s.Top:F2}_{s.Top1:F2}_{s.Stair}_{s.Thick:F1}");

        private static ArenaStage Stage(Transform root, PayoLayout layout, Art art, Dictionary<string, Material> named,
                                        out string said, out List<GameObject> nightOnly, out List<Renderer> nightGlow)
        {
            nightOnly = new List<GameObject>(); nightGlow = new List<Renderer>();
            int count = layout.Layouts.Count;
            var order = new List<string>();
            var byId = new Dictionary<string, Shape[]>();
            for (int l = 0; l < count; l++)
                foreach (var b in layout.Layouts[l].Blocks)
                {
                    if (!byId.TryGetValue(b.Id, out var slots)) { byId[b.Id] = slots = new Shape[count]; order.Add(b.Id); }
                    slots[l] = ShapeOf(b);
                }

            // A block keeps its plan in every layout: the system depends on it (it only rises and sinks).
            foreach (var id in order)
            {
                Shape first = default;
                foreach (var s in byId[id])
                {
                    if (!s.Exists) continue;
                    if (!first.Exists) { first = s; continue; }
                    if (Mathf.Abs(s.X - first.X) > 0.001f || Mathf.Abs(s.Z - first.Z) > 0.001f || Mathf.Abs(s.SX - first.SX) > 0.001f || Mathf.Abs(s.SZ - first.SZ) > 0.001f)
                        throw new InvalidOperationException($"{Tag}block '{id}' changes its plan between layouts: a block only rises and sinks. Fix {LayoutPath}.");
                }
            }
            for (int l = 0; l < count; l++) CheckLayout(layout, layout.Layouts[l]);

            var stageObject = Group(root, "Stage");
            var stage = stageObject.gameObject.AddComponent<ArenaStage>();
            stage.Stone = true;
            stageObject.gameObject.AddComponent<ArenaFallRecovery>();
            stageObject.gameObject.AddComponent<PayoStageShow>();
            var colliders = Group(stageObject, "Colliders");
            var visuals = Group(stageObject, "Visuals");
            var features = Group(stageObject, "Features");

            var top = Stone("payo_standin_top", new Color(0.66f, 0.68f, 0.70f), 0.3f);
            var cap = Stone("payo_standin_cap", new Color(0.50f, 0.52f, 0.55f), 0.25f);
            var shaft = Stone("payo_standin_shaft", new Color(0.30f, 0.32f, 0.36f), 0.16f);
            var pieceMaterials = new[] { top, cap, shaft };
            var slab = Stone("payo_standin_slab", new Color(0.50f, 0.66f, 0.66f), 0.3f);   // a mover: stone with the verdigris of its bronze shoes

            int first0 = ArenaStage.LayoutFor(0L, 1, count);
            var written = new HashSet<string>();
            var meshes = new Dictionary<string, Mesh>();
            stage.Layouts = new ArenaStage.Layout[count];
            var colliderGroups = new Transform[count]; var visualGroups = new Transform[count]; var featureGroups = new Transform[count];
            int boxes = 0, movers = 0, pads = 0, bowls = 0, authored = 0;
            var flameKit = FlameKit();
            for (int l = 0; l < count; l++)
            {
                var one = layout.Layouts[l];
                colliderGroups[l] = Group(colliders, one.Name);
                visualGroups[l] = Group(visuals, one.Name);
                featureGroups[l] = Group(features, one.Name);
                stage.Layouts[l] = new ArenaStage.Layout { Name = one.Name, CanHeight = one.CanHeight, Colliders = colliderGroups[l].gameObject, Features = featureGroups[l].gameObject };
            }

            stage.Pieces = new ArenaStage.Piece[order.Count];
            float lowest = float.PositiveInfinity, radius = 0.0f;
            for (int i = 0; i < order.Count; i++)
            {
                string id = order[i];
                var shapes = byId[id];
                var piece = new ArenaStage.Piece { Id = id, Shapes = shapes, Solids = new GameObject[count], Holograms = new GameObject[count], Authored = new bool[count] };
                for (int l = 0; l < count; l++)
                {
                    var shape = shapes[l];
                    if (!shape.Exists) continue;
                    lowest = Mathf.Min(lowest, Mathf.Min(shape.Top, shape.Top1));
                    if (!shape.Bonus) radius = Mathf.Max(radius, Mathf.Max(Mathf.Abs(shape.X) + shape.SX * 0.5f, Mathf.Abs(shape.Z) + shape.SZ * 0.5f));

                    boxes += Collide(colliderGroups[l], id, shape);

                    string name = $"stage_{layout.Layouts[l].Name}_{id}";
                    var holder = Group(visualGroups[l], name).gameObject;
                    // The art's model of this block (a flight has its own), by the art's own table; else by the plain name.
                    string wanted = shape.Stair != 0 ? (art.StairModel.TryGetValue(id, out var sm) ? sm : "block_" + id + "_stair")
                                                     : (art.BlockModel.TryGetValue(id, out var bm) ? bm : "block_" + id);
                    var model = Model(wanted);
                    if (model == null && art.HideSolids) Warn($"hide_solids is set and block '{id}'{(shape.Stair != 0 ? " (as a stair)" : "")} has no model '{wanted}': it is NOT drawn in layout '{layout.Layouts[l].Name}'");
                    piece.Authored[l] = model != null;
                    if (model != null)
                    {
                        // The art's block: its origin is the middle of the block's top and its top face is AT the origin, so it
                        // stands a centimetre under the collision, TURNED by the art's yaw (a stair by its own: without it the
                        // flight descends the wrong way under its ramp collider).
                        float turn = shape.Stair != 0 && art.StairYaw.TryGetValue(id, out float sy) ? sy : art.BlockYaw.TryGetValue(id, out float by) ? by : 0.0f;
                        var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, holder.transform);
                        instance.name = "Model";
                        float dy = art.BlockDy.TryGetValue(id, out float bdy) ? bdy : 0.0f;
                        instance.transform.localPosition = new Vector3(shape.X, shape.Top - 0.01f + dy, shape.Z);
                        instance.transform.localRotation = Quaternion.Euler(0.0f, turn, 0.0f);
                        Rematerial(instance, named);
                        foreach (var c in instance.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
                        Finish(instance, false);
                        Fires(instance);
                        // The plain shaft under it, so a raised block shows no foot: the same origin and turn, riding with it.
                        if (art.Extension.TryGetValue(id, out string extension))
                        {
                            var shaft0 = Hang(extension, holder.transform, new Vector3(shape.X, shape.Top - 0.01f + dy, shape.Z), "Extension", named);
                            if (shaft0 != null) shaft0.transform.localRotation = Quaternion.Euler(0.0f, art.BlockYaw.TryGetValue(id, out float ey) ? ey : 0.0f, 0.0f);
                        }
                        authored++;
                    }
                    else if (!art.HideSolids)
                    {
                        string key = Key(shape);
                        if (!meshes.TryGetValue(key, out var mesh)) meshes[key] = mesh = MeshAsset($"{Generated}/stage_{key}.asset", shape, written);
                        holder.AddComponent<MeshFilter>().sharedMesh = mesh;
                        holder.AddComponent<MeshRenderer>().sharedMaterials = pieceMaterials;
                    }

                    // A fire bowl on each corner tower, at its outer corner, where nobody runs: lit at night.
                    if (!art.HasFires && id.StartsWith("tower", StringComparison.Ordinal) && shape.Stair == 0)
                    {
                        float sx = Mathf.Sign(shape.X), sz = Mathf.Sign(shape.Z);
                        var at = new Vector3(shape.X + sx * (shape.SX * 0.5f - 1.1f), shape.Top, shape.Z + sz * (shape.SZ * 0.5f - 1.1f));
                        Bowl(holder.transform, at, flameKit, cap, Mathf.Repeat(Mathf.Atan2(shape.X, shape.Z) / (Mathf.PI * 2.0f), 1.0f));
                        bowls++;
                    }

                    piece.Solids[l] = holder;
                    holder.SetActive(l == first0);
                }
                stage.Pieces[i] = piece;
            }

            // The art's pieces that ride a block.
            // `on_block`: the placement's position is where it stands in the FIRST layout of the file (liwasan); it is hung
            // on that block's drawn solid in every layout the block stands in, raised or lowered with the block's top,
            // so it travels with the block through a break and is simply not there where the block is sunk.
            foreach (var p in art.Placements)
            {
                string block = Str(p["on_block"]) ?? Str(p["block"]);
                if (block == null) continue;
                int index = order.IndexOf(block);
                if (index < 0) { Warn($"a placement names the block '{block}', which no layout has"); continue; }
                float home = float.NaN;
                for (int l = 0; l < count && float.IsNaN(home); l++) if (byId[block][l].Exists) home = byId[block][l].Top;
                for (int l = 0; l < count; l++)
                {
                    var shape = byId[block][l];
                    if (!shape.Exists) continue;
                    var parent = stage.Pieces[index].Solids[l].transform;
                    Vector3 lift = Vector3.up * (shape.Top - home);
                    if (Str(p["model"]) == "fire_flame") { if (Fire(p, parent, lift, named) != null) bowls++; continue; }
                    var go = Place(p, parent, lift, named);
                    if (go != null) { Finish(go, false); bowls += Fires(go); }
                    if (go != null && Str(p["group"]) == "night") nightOnly.Add(go);
                }
            }
            // The art's lights that ride a block (`night.lights` with `on_block`): one under that block's drawn solid in every layout it stands in.
            foreach (var light in art.Lights)
            {
                string block = Str(light["on_block"]);
                var position = light["position"] as JArray;
                if (block == null || position == null || position.Count < 3) continue;
                int index = order.IndexOf(block);
                if (index < 0) continue;
                float home = float.NaN;
                for (int l = 0; l < count && float.IsNaN(home); l++) if (byId[block][l].Exists) home = byId[block][l].Top;
                for (int l = 0; l < count; l++)
                {
                    var shape = byId[block][l];
                    if (!shape.Exists) continue;
                    Lamp(stage.Pieces[index].Solids[l].transform, new Vector3((float)position[0], (float)position[1] + shape.Top - home, (float)position[2]), Num(light["range"], 8.0f), null);
                }
            }

            // Each layout's features, and its way points, baked against its own collision alone.
            string routes = "";
            for (int l = 0; l < count; l++)
            {
                for (int k = 0; k < count; k++) colliderGroups[k].gameObject.SetActive(k == l);
                for (int k = 0; k < count; k++) featureGroups[k].gameObject.SetActive(false);
                Physics.SyncTransforms();
                var one = layout.Layouts[l];
                pads += Features(featureGroups[l], one, slab, shaft, ref movers);
                routes += (l > 0 ? ", " : "") + Routes(featureGroups[l], one);
            }
            for (int k = 0; k < count; k++) { colliderGroups[k].gameObject.SetActive(k == first0); featureGroups[k].gameObject.SetActive(k == first0); }
            Physics.SyncTransforms();
            Purge(written);

            stage.PieceMaterials = pieceMaterials;
            stage.CatchLine = layout.CatchY;
            stage.MoveFloor = MoveFloorY;
            stage.KillPlaneY = KillPlaneY;
            stage.Radius = Mathf.Max(18.0f, radius);
            // Every standing surface is above this (the lowest piston rests at -0.6): under it a body in the air has nothing to land on.
            stage.LowestUnderside = Mathf.Min(lowest, -0.6f) - 1.0f;
            stage.SinkDepth = Mathf.Max(10.0f, layout.StageTop - layout.SunkTop);
            said = $"{order.Count} blocks, {boxes} solid boxes over {count} layouts ({authored} blocks wearing an art model, the rest the stand-in{(art.HideSolids ? ", NOT drawn: hide_solids" : "")}), " +
                   $"{pads} pads and pickups, {movers} moving slabs, {bowls} fire bowls; the scene stands in '{layout.Layouts[first0].Name}'; bots' way points: {routes}";
            return stage;
        }

        /// <summary>Solid floor at the can's height under the can, and floor a spawn can be seated on under each mark
        /// (`MatchHost.SeatOnFloor` casts from 2 m over the mark down 6 m), on the walk set.</summary>
        private static void CheckLayout(PayoLayout layout, PayoLayout.Layout one)
        {
            var under = one.At(0.0f, 0.0f, out float y);
            if (under == null || under.Stair != 0 || Mathf.Abs(y - one.CanHeight) > 0.01f)
                throw new InvalidOperationException($"{Tag}layout '{one.Name}' has no level floor at canHeight {one.CanHeight} under the can. Fix {LayoutPath}.");
            var marks = new List<Vector3> { layout.Taya };
            marks.AddRange(layout.Attackers);
            foreach (var mark in marks)
            {
                var floor = one.At(mark.x, mark.z, out float top);
                if (floor == null || floor.Reach != PayoLayout.Reach.Walk || top > mark.y + 2.0f || top < mark.y - 4.0f)
                    throw new InvalidOperationException($"{Tag}layout '{one.Name}' has no walk-set floor under the mark at {mark}. Fix {LayoutPath}.");
            }
            foreach (var p in one.Pads) if (one.At(p.At.x, p.At.z, out float py) == null || Mathf.Abs(py - p.At.y) > 0.3f) Warn($"layout '{one.Name}': a geyser at {p.At} has no floor at that height under it");
            foreach (var p in one.SpeedPads) if (one.At(p.At.x, p.At.z, out float py) == null || Mathf.Abs(py - p.At.y) > 0.3f) Warn($"layout '{one.Name}': a speed pad at {p.At} has no floor at that height under it");
            foreach (var p in one.Pickups) if (one.At(p.At.x, p.At.z, out float py) == null || Mathf.Abs(py - p.At.y) > 0.3f) Warn($"layout '{one.Name}': a pickup at {p.At} has no floor at that height under it");
        }

        /// <summary>A block's collision: solid boxes, never a hollow mesh. Returns how many.</summary>
        private static int Collide(Transform group, string id, in Shape s)
        {
            var solid = Group(group, id);
            float body = s.Stair != 0 ? s.Top1 : s.Top, foot = s.Top - s.Thick;
            var box = solid.gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(s.X, (body + foot) * 0.5f, s.Z);
            box.size = new Vector3(s.SX, body - foot, s.SZ);
            if (s.Stair == 0) return 1;

            // The flight: one tilted box whose top face runs from the high edge at `Top` to the low edge at `Top1`.
            bool alongX = s.Stair == 1 || s.Stair == 2;
            float run = alongX ? s.SX : s.SZ, rise = s.Top - s.Top1, length = Mathf.Sqrt(run * run + rise * rise), tilt = Mathf.Atan2(rise, run) * Mathf.Rad2Deg;
            var ramp = Group(solid, "flight");
            Quaternion turn;
            switch (s.Stair)
            {
                case 1: turn = Quaternion.Euler(0.0f, 0.0f, -tilt); break;      // down toward +x
                case 2: turn = Quaternion.Euler(0.0f, 0.0f, tilt); break;       // down toward -x
                case 3: turn = Quaternion.Euler(tilt, 0.0f, 0.0f); break;       // down toward +z
                default: turn = Quaternion.Euler(-tilt, 0.0f, 0.0f); break;     // down toward -z
            }
            Vector3 mid = new Vector3(s.X, (s.Top + s.Top1) * 0.5f, s.Z);
            ramp.SetPositionAndRotation(mid - (turn * Vector3.up) * (RampThick * 0.5f), turn);
            ramp.gameObject.AddComponent<BoxCollider>().size = alongX ? new Vector3(length, RampThick, s.SZ) : new Vector3(s.SX, RampThick, length);
            return 2;
        }

        private static Mesh MeshAsset(string path, in Shape shape, HashSet<string> written)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool fresh = mesh == null;
            if (fresh) mesh = new Mesh();
            ArenaStageMesh.Build(shape, mesh, true);
            mesh.name = Path.GetFileNameWithoutExtension(path);
            if (fresh) AssetDatabase.CreateAsset(mesh, path); else EditorUtility.SetDirty(mesh);
            written.Add(path);
            return mesh;
        }

        private static void Purge(HashSet<string> written)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Mesh", new[] { Generated }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileName(path).StartsWith("stage_", StringComparison.Ordinal) && !written.Contains(path)) AssetDatabase.DeleteAsset(path);
            }
        }

        private static Art _art;
        private static Dictionary<string, Material> _named;
        private static string _terraceSaid = "";

        /// <summary>One layout's geysers, speed pads, pickups and moving slabs. Returns the pads and pickups made.</summary>
        private static int Features(Transform group, PayoLayout.Layout one, Material slab, Material shaft, ref int movers)
        {
            int n = 0, made = 0;
            foreach (var p in one.Pads)
            {
                var pad = Group(group, "Geyser " + n++);
                pad.position = p.At;
                var jump = pad.gameObject.AddComponent<JumpPad>();
                jump.LaunchSpeed = p.Speed;
                // The geyser is this map's own (owner, 2026-10-12: "water and steam of their own"): the amber pad of the
                // other maps is not drawn here, the art's stone bowl is, and `PayoGeyser` draws the water in it.
                jump.NoLook = true;
                pad.gameObject.AddComponent<PayoGeyser>();
                if (_art != null) Hang(_art.Geyser, pad, Vector3.zero, "Bowl", _named);
                made++;
            }
            n = 0;
            foreach (var p in one.SpeedPads)
            {
                var pad = Group(group, "SpeedPad " + n++);
                pad.SetPositionAndRotation(p.At, Quaternion.LookRotation(p.Along, Vector3.up));
                pad.gameObject.AddComponent<ArenaSpeedPad>().HalfSize = p.HalfSize;
                // The child the pad's component looks for: with it there the component draws no grey-box of its own.
                if (_art != null) Hang(_art.SpeedPad, pad, Vector3.zero, ArenaSpeedPad.ModelName, _named);
                made++;
            }
            n = 0;
            foreach (var p in one.Pickups)
            {
                var pickup = Group(group, "StaminaPickup " + n++);
                pickup.position = p.At;
                pickup.gameObject.AddComponent<ArenaStaminaPickup>();
                // The plinth is the base; the orb (pickup_cell, pickup_halo) is the component's own model, so it draws no grey-box.
                if (_art != null) Hang(_art.Plinth, pickup, Vector3.zero, "Plinth", _named);
                if (_art != null) Hang(_art.Orb, pickup, Vector3.zero, ArenaStaminaPickup.ModelName, _named);
                made++;
            }

            if (one.Movers.Count == 0) return made;
            var moversRoot = Group(group, "Movers");
            var manager = moversRoot.gameObject.AddComponent<PayoMovers>();
            var all = new List<PayoMover>();
            foreach (var m in one.Movers)
            {
                var go = new GameObject(m.Id);
                go.transform.SetParent(moversRoot, false);
                go.transform.position = m.From;
                var mover = go.AddComponent<PayoMover>();
                mover.From = m.From; mover.To = m.To; mover.Period = m.Period; mover.Phase = m.Phase; mover.Dwell = m.Dwell;
                var body = go.AddComponent<Rigidbody>();
                body.isKinematic = true; body.useGravity = false;
                // A slab that only rises and falls is the head of a piston: it stands on a shaft. One that travels is a slab.
                bool piston = Mathf.Abs(m.From.x - m.To.x) < 0.01f && Mathf.Abs(m.From.z - m.To.z) < 0.01f;
                float thick = piston ? 40.0f : MoverThick;
                var box = go.AddComponent<BoxCollider>();
                box.size = new Vector3(m.SX, thick, m.SZ); box.center = new Vector3(0.0f, -thick * 0.5f, 0.0f);
                mover.Solid = box;
                if (_art != null && _art.MoverModel.TryGetValue(m.Id, out string moverModel) && Hang(moverModel, go.transform, Vector3.zero, "Model", _named) != null)
                { all.Add(mover); movers++; continue; }
                var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.DestroyImmediate(head.GetComponent<Collider>());
                head.name = "slab"; head.transform.SetParent(go.transform, false);
                head.transform.localPosition = new Vector3(0.0f, -MoverThick * 0.5f, 0.0f); head.transform.localScale = new Vector3(m.SX, MoverThick, m.SZ);
                head.GetComponent<Renderer>().sharedMaterial = slab;
                if (piston)
                {
                    var stem = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Object.DestroyImmediate(stem.GetComponent<Collider>());
                    stem.name = "shaft"; stem.transform.SetParent(go.transform, false);
                    stem.transform.localPosition = new Vector3(0.0f, -MoverThick - (thick - MoverThick) * 0.5f, 0.0f);
                    stem.transform.localScale = new Vector3(m.SX - 0.5f, thick - MoverThick, m.SZ - 0.5f);
                    stem.GetComponent<Renderer>().sharedMaterial = shaft;
                }
                all.Add(mover);
                movers++;
            }
            manager.Movers = all.ToArray();
            return made;
        }

        /// <summary>
        /// The bots' way points for one layout (`MapRoutes`): the middle of every WALK block, and for a flight its
        /// head and three points along its foot, baked with only this layout's block collision live (no moving slab:
        /// a way that exists only while a slab is there is not a way). Returns what the bake found.
        /// </summary>
        private static string Routes(Transform group, PayoLayout.Layout one)
        {
            var nodes = new List<Vector3>();
            int core = -1;
            foreach (var b in one.Blocks)
            {
                if (b.Reach != PayoLayout.Reach.Walk) continue;
                if (b.Stair == 0)
                {
                    if (b.Id == "core") core = nodes.Count;
                    nodes.Add(new Vector3(b.X, b.Top, b.Z));
                    continue;
                }
                bool alongX = b.Stair == 1 || b.Stair == 2;
                float sign = b.Stair == 1 || b.Stair == 3 ? 1.0f : -1.0f, half = (alongX ? b.SX : b.SZ) * 0.5f - 0.6f, side = (alongX ? b.SZ : b.SX) * 0.5f - 0.6f;
                for (int k = -1; k <= 2; k++)
                {
                    // k -1, 0, 1: along the foot; k 2: the head.
                    float along = k == 2 ? -sign * half : sign * half, across = k == 2 ? 0.0f : k * side;
                    float x = b.X + (alongX ? along : across), z = b.Z + (alongX ? across : along);
                    nodes.Add(new Vector3(x, PayoLayout.HeightAt(b, x, z), z));
                }
            }
            var holder = Group(group, "Routes");
            var routes = holder.gameObject.AddComponent<MapRoutes>();
            routes.Nodes = nodes.ToArray();
            int links = routes.Bake();
            EditorUtility.SetDirty(routes);
            if (core < 0) return $"{one.Name} {nodes.Count} points, {links} links, NO core block";
            var none = new List<int>();
            int cannotReach = routes.CountWithoutWayFrom(core, none), cannotReturn = routes.CountWithoutWayTo(core);
            if (cannotReach > 0) Warn($"layout '{one.Name}': {cannotReach} way point(s) cannot be walked to from the can: " + string.Join(", ", none.Select(i => nodes[i].ToString("F1"))));
            if (cannotReturn > 0) Warn($"layout '{one.Name}': from {cannotReturn} way point(s) a bot cannot walk back to the can");
            return $"{one.Name} {nodes.Count} points, {links} links, {cannotReach} unreachable from the can, {cannotReturn} with no way back";
        }

        // ------------------------------------------------------------------ what never moves

        private static void Gameplay(Transform root, PayoLayout layout)
        {
            Group(root, "~Match").gameObject.AddComponent<MatchInstaller>();
            var kill = Group(root, "KillPlane");
            kill.localPosition = new Vector3(0.0f, KillPlaneY, 0.0f);
            kill.gameObject.AddComponent<KillPlane>().Height = KillPlaneY;
            var trigger = kill.gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(KillPlane.PlaneExtent, KillPlane.PlaneThickness, KillPlane.PlaneExtent);

            var marks = Group(root, "SpawnPoints");
            int seat = 0;
            Group(marks, "Spawn" + seat++).localPosition = layout.Taya + Vector3.up * 0.1f;
            foreach (var mark in layout.Attackers) Group(marks, "Spawn" + seat++).localPosition = mark + Vector3.up * 0.1f;

            // Four invisible walls, their inner faces on +/- wallHalf, from under the kill plane to over the slipper's
            // ceiling: a body falling down a slot is inside them all the way to the catch (the Arena's rule).
            var bounds = Group(root, "Bounds");
            float half = layout.WallHalf, top = layout.StageTop + WallHeight, bottom = KillPlaneY - 2.0f, height = top - bottom, y = (top + bottom) * 0.5f;
            float span = 2.0f * (half + WallThickness), centre = half + WallThickness * 0.5f;
            Wall(bounds, "WallWest", new Vector3(-centre, y, 0.0f), new Vector3(WallThickness, height, span));
            Wall(bounds, "WallEast", new Vector3(centre, y, 0.0f), new Vector3(WallThickness, height, span));
            Wall(bounds, "WallSouth", new Vector3(0.0f, y, -centre), new Vector3(span, height, WallThickness));
            Wall(bounds, "WallNorth", new Vector3(0.0f, y, centre), new Vector3(span, height, WallThickness));
        }

        private static void Wall(Transform parent, string name, Vector3 centre, Vector3 size)
        {
            var wall = Group(parent, name);
            wall.localPosition = centre;
            wall.gameObject.AddComponent<BoxCollider>().size = size;
        }

        // ------------------------------------------------------------------ light, sky, look

        private static Light Lighting(Transform root, out WorldLookProfile.MapLook day)
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(root, false);
            sun.type = LightType.Directional;
            EnsureLook(PayoNight.DayRow); EnsureLook(PayoNight.NightRow);
            MapAtmosphereAuthor.Apply(MapName);
            day = WorldLookProfile.Current.Find(PayoNight.DayRow);
            float elevation = day.SunElevation > 0.0f ? day.SunElevation : 46.0f;
            sun.transform.rotation = Quaternion.Euler(elevation, SunYaw, 0.0f);
            sun.color = day.Sun; sun.intensity = day.SunIntensity;
            sun.shadows = LightShadows.Soft; sun.shadowStrength = day.ShadowStrength;
            sun.shadowBias = 0.03f; sun.shadowNormalBias = 0.2f;
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = day.Sky; RenderSettings.ambientEquatorColor = day.Equator; RenderSettings.ambientGroundColor = day.Ground;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = day.Fog; RenderSettings.fogStartDistance = day.FogStart; RenderSettings.fogEndDistance = day.FogEnd;
            var sky = RenderSettings.skybox;
            if (sky != null)
            {
                sky.SetColor("_Zenith", day.Zenith); sky.SetColor("_Horizon", day.Horizon);
                sky.SetColor("_Ground", day.Fog * 0.8f);
                sky.SetColor("_CloudLight", day.CloudLight); sky.SetColor("_CloudShade", day.CloudShade);
                sky.SetColor("_SunColor", day.Sun); sky.SetVector("_SunDirection", -sun.transform.forward);
                // the setting sun shows as a disc over the valley (owner: the play area faces the valley and the sunset);
                // PayoNight.Sky swaps these for the moon at the break
                sky.SetFloat("_SunDisc", .05f); sky.SetFloat("_SunHalo", 1f); sky.SetFloat("_SunClear", .35f); sky.SetFloat("_SunCrisp", .85f);
                EditorUtility.SetDirty(sky);
            }
            else Warn("no sky material was made (MapAtmosphereAuthor): the scene has no skybox");
            root.GetComponent<MapGrade>().Set(1.0f, 1.03f, 1.06f, 1.0f, 1.9f);
            return sun;
        }

        /// <summary>The row in the authored profile, copied from the code's when the asset was saved before it existed
        /// (and only then: a later tuning of the row in the asset is never overwritten).</summary>
        private static void EnsureLook(string row)
        {
            // ⚠️ UNLIKE THE OTHER MAPS' BUILDERS THIS REWRITES ITS TWO ROWS FROM THE CODE ON EVERY BUILD, while the map is
            // being made: the rows changed when the art came, and an asset row copied once would have kept the old air.
            // Once the owner tunes a row in the asset, change this to copy only a missing row (ArenaSceneBuilder.EnsureLook).
            var profile = WorldLookProfile.Current;
            var defaults = ScriptableObject.CreateInstance<WorldLookProfile>();
            var look = defaults.Maps.FirstOrDefault(m => m.Map == row);
            Object.DestroyImmediate(defaults);
            if (look == null) throw new InvalidOperationException("WorldLookProfile has no " + row + " row in code");
            var rows = new List<WorldLookProfile.MapLook>(profile.Maps.Where(m => m.Map != row)) { look };
            profile.Maps = rows.ToArray();
            if (EditorUtility.IsPersistent(profile)) { EditorUtility.SetDirty(profile); AssetDatabase.SaveAssetIfDirty(profile); }
            Debug.Log($"{Tag}added the {row} row to the authored WorldLookProfile");
        }

        // ------------------------------------------------------------------ the night

        private sealed class FlameSet { public Mesh Tongue, Bowl; public Material Core, Outer; }

        private static FlameSet FlameKit()
        {
            var kit = new FlameSet();
            // A tongue of flame: a stretched eight-faced shape standing on its base point, one unit tall.
            kit.Tongue = Solid("payo_flame_tongue", new[]
            {
                new Vector3(0, 0, 0), new Vector3(0.5f, 0.34f, 0), new Vector3(0, 0.34f, 0.5f), new Vector3(-0.5f, 0.34f, 0), new Vector3(0, 0.34f, -0.5f), new Vector3(0, 1, 0),
            }, new[] { 0, 2, 1, 0, 3, 2, 0, 4, 3, 0, 1, 4, 5, 1, 2, 5, 2, 3, 5, 3, 4, 5, 4, 1 });
            // The bowl: an eight-sided dish on a short foot, its rim 0.8 m up.
            var points = new List<Vector3>(); var tris = new List<int>();
            float[] ringY = { 0.0f, 0.42f, 0.5f, 0.8f, 0.72f }, ringR = { 0.22f, 0.16f, 0.3f, 0.62f, 0.5f };
            for (int r = 0; r < ringY.Length; r++)
                for (int k = 0; k < 8; k++) { float a = k * Mathf.PI / 4.0f; points.Add(new Vector3(Mathf.Sin(a) * ringR[r], ringY[r], Mathf.Cos(a) * ringR[r])); }
            for (int r = 0; r + 1 < ringY.Length; r++)
                for (int k = 0; k < 8; k++)
                {
                    int a = r * 8 + k, b = r * 8 + (k + 1) % 8, c = a + 8, d = b + 8;
                    tris.AddRange(new[] { a, b, d, a, d, c });
                }
            int centre = points.Count; points.Add(new Vector3(0, 0.7f, 0));
            for (int k = 0; k < 8; k++) tris.AddRange(new[] { centre, 32 + (k + 1) % 8, 32 + k });
            kit.Bowl = Solid("payo_fire_bowl", points.ToArray(), tris.ToArray());
            kit.Core = GlowMaterial("payo_flame_core", new Color(3.2f, 2.5f, 1.0f, 0.9f));
            kit.Outer = GlowMaterial("payo_flame_outer", new Color(2.6f, 0.95f, 0.22f, 0.7f));
            return kit;
        }

        /// <summary>A flat-shaded solid saved under Generated (every face its own corners).</summary>
        private static Mesh Solid(string name, Vector3[] points, int[] triangles)
        {
            string path = $"{Generated}/{name}.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool fresh = mesh == null;
            if (fresh) mesh = new Mesh();
            var v = new Vector3[triangles.Length]; var t = new int[triangles.Length];
            for (int i = 0; i < triangles.Length; i++) { v[i] = points[triangles[i]]; t[i] = i; }
            mesh.Clear(); mesh.vertices = v; mesh.triangles = t; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            mesh.name = name;
            if (fresh) AssetDatabase.CreateAsset(mesh, path); else EditorUtility.SetDirty(mesh);
            return mesh;
        }

        private static Material GlowMaterial(string name, Color colour, Texture texture = null, bool blended = false)
        {
            string path = $"{Generated}/{name}.mat";
            var shader = Shader.Find("TumbangPreso/ArenaGlow");
            if (shader == null) { Warn("the shader TumbangPreso/ArenaGlow is missing: " + name + " is a plain sprite material"); shader = Shader.Find("Sprites/Default"); }
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            m.SetColor("_Color", colour);
            if (texture != null) m.mainTexture = texture;
            if (m.HasProperty("_VertexTint"))
            {
                m.SetFloat("_Rim", 0.0f); m.SetVector("_Near", Vector4.zero); m.SetFloat("_Fog", 0.0f); m.SetFloat("_VertexTint", 0.0f);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)(blended ? BlendMode.OneMinusSrcAlpha : BlendMode.One));
                m.SetFloat("_Cull", (float)CullMode.Off);
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        private static void Bowl(Transform parent, Vector3 at, FlameSet kit, Material stone, float order)
        {
            var bowl = Group(parent, "FireBowl");
            bowl.localPosition = at;
            bowl.gameObject.AddComponent<MeshFilter>().sharedMesh = kit.Bowl;
            bowl.gameObject.AddComponent<MeshRenderer>().sharedMaterial = stone;
            var flame = bowl.gameObject.AddComponent<PayoFlame>();
            flame.Order = order;
            var tongues = new List<Transform>();
            for (int k = 0; k < 5; k++)
            {
                var tongue = Group(bowl, "tongue " + k);
                float a = k * Mathf.PI * 2.0f / 4.0f;
                tongue.localPosition = k == 0 ? new Vector3(0.0f, 0.7f, 0.0f) : new Vector3(Mathf.Sin(a) * 0.22f, 0.7f, Mathf.Cos(a) * 0.22f);
                tongue.gameObject.AddComponent<MeshFilter>().sharedMesh = kit.Tongue;
                var r = tongue.gameObject.AddComponent<MeshRenderer>();
                r.sharedMaterial = k == 0 ? kit.Core : kit.Outer;
                r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
                tongue.gameObject.SetActive(false);
                tongues.Add(tongue);
            }
            flame.Tongues = tongues.ToArray();
            var lamp = Group(bowl, "glow");
            lamp.localPosition = new Vector3(0.0f, 1.3f, 0.0f);
            var light = lamp.gameObject.AddComponent<Light>();
            light.type = LightType.Point; light.range = 10.0f; light.color = new Color(1.0f, 0.62f, 0.26f); light.intensity = 0.0f;
            light.shadows = LightShadows.None; light.renderMode = LightRenderMode.ForcePixel; light.enabled = false;
            flame.Glow = light;
        }

        /// <summary>The night's objects: `PayoNight` (the light, the air, Mayari) and `PayoNightLife` (stars, shooting
        /// stars, fireflies, the bowls). Mayari is the art's sky piece if it gave one, else a card with the draft's picture.</summary>
        private static string Night(Transform root, PayoLayout layout, Light sun, Renderer[] mayari, List<GameObject> nightOnly, List<Renderer> nightGlow)
        {
            var holder = Group(root, "Night").gameObject;
            var night = holder.AddComponent<PayoNight>();
            holder.AddComponent<PayoHaze>();      // the aerial haze over the valley, by day and night (owner, 2026-10-18: "should add a haze")
            holder.AddComponent<PayoPaddySky>();  // the live sky's colours for the watered paddies' reflection (PayoLand, paddy_flood)
            night.Sun = sun;
            night.SunDay = sun.transform.rotation;
            night.SunNight = Quaternion.Euler(MoonElevation, MoonYaw, 0.0f);
            string who;
            if (mayari != null && mayari.Length > 0) who = "Mayari is the art's sky piece";
            else
            {
                var picture = AssetDatabase.LoadAssetAtPath<Texture2D>(MayariPlaceholder);
                if (picture == null) { who = "NO Mayari (no art sky piece and no placeholder picture at " + MayariPlaceholder + ")"; Warn(who); }
                else
                {
                    // A card far up the north sky, over the valley, facing the court. Unlit, drawn over the stars.
                    var card = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    Object.DestroyImmediate(card.GetComponent<Collider>());
                    card.name = "Mayari (placeholder card)";
                    card.transform.SetParent(holder.transform, false);
                    Vector3 at = new Vector3(40.0f, 330.0f, 760.0f);
                    card.transform.position = at;
                    card.transform.rotation = Quaternion.LookRotation(at - new Vector3(0.0f, 40.0f, 0.0f), Vector3.up);
                    card.transform.localScale = new Vector3(330.0f, 330.0f, 1.0f);
                    var r = card.GetComponent<Renderer>();
                    var m = GlowMaterial("payo_mayari_placeholder", new Color(1.25f, 1.25f, 1.25f, 1.0f), picture, true);
                    m.renderQueue = 3200;
                    r.sharedMaterial = m;
                    r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
                    r.enabled = false;
                    mayari = new[] { r };
                    who = "Mayari is a PLACEHOLDER card (the draft's picture, variant A)";
                }
            }
            night.Mayari = mayari ?? Array.Empty<Renderer>();
            foreach (var r in night.Mayari) if (r != null) r.enabled = false;
            night.NightOnly = nightOnly.ToArray();
            foreach (var go in nightOnly) if (go != null) go.SetActive(false);
            // Every material the art names as glowing at night, wherever it is worn (lit windows; the fires are flames of their own).
            var glowing = new List<Renderer>(); var glowIndex = new List<int>(); var glowColour = new List<Color>();
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    bool lantern = Lanterns.Contains(r);
                    if (mats[i].name != "py_window" && mats[i].name != "py_inlay" && !Glows.Contains(mats[i].name) && !lantern) continue;
                    if (!mats[i].HasProperty("_EmissionColor")) continue;
                    glowing.Add(r); glowIndex.Add(i);
                    // A window's night colour is its undimmed tint (its own colour was taken to 0.15); the earlier inlay's is five
                    // times its day glow; a glow of the assembled art is its own picture at 1.7 (0.1 by day); a lantern's glass a warm 1.4.
                    Color c = mats[i].HasProperty("_Color") ? mats[i].GetColor("_Color") : Color.white;
                    Color e = mats[i].GetColor("_EmissionColor");
                    glowColour.Add(mats[i].name == "py_window" ? new Color(c.r / 0.15f, c.g / 0.15f, c.b / 0.15f) * 1.5f
                                 : mats[i].name == "py_inlay" ? e * 5.0f
                                 : Glows.Contains(mats[i].name) ? new Color(1.7f, 1.7f, 1.7f) : new Color(1.4f, 0.95f, 0.5f));
                }
            }
            night.NightGlow = glowing.ToArray(); night.NightGlowIndex = glowIndex.ToArray(); night.NightGlowColour = glowColour.ToArray();
            night.Lamps = Lamps.Where(l => l != null).ToArray();

            var life = holder.AddComponent<PayoNightLife>();
            life.SkyMiddle = new Vector3(0.2f, 0.62f, 0.76f);
            var homes = new List<Vector3>(); var roam = new List<float>();
            var random = new System.Random(8128);
            float R(float a, float b) => a + (b - a) * (float)random.NextDouble();
            // Over the paddies, mostly on the climbing side where a player's eye meets them.
            // ⚠️ OVER THE GROUND THAT IS REALLY THERE: the art's land has no collider (nobody stands on it), so it is given
            // mesh colliders for this one question and they are taken away again before the scene is saved.
            var temporary = new List<MeshCollider>();
            var dressing = root.Find("Dressing");
            var land = new List<Transform>();
            if (dressing != null) foreach (Transform piece in dressing) if (IsLand(piece.name)) land.Add(piece);
            if (root.Find("Terraces") != null) land.Add(root.Find("Terraces"));
                foreach (Transform piece in land)
                {
                    foreach (var filter in piece.GetComponentsInChildren<MeshFilter>(true))
                    {
                        if (filter.sharedMesh == null) continue;
                        var collider = filter.gameObject.AddComponent<MeshCollider>();
                        collider.sharedMesh = filter.sharedMesh;
                        temporary.Add(collider);
                    }
                }
            Physics.SyncTransforms();
            int grounded = 0;
            for (int i = 0, tries = 0; i < 64 && tries < 3000; tries++)
            {
                float bearing = R(0.0f, 360.0f), r = R(40.0f, 95.0f);
                Vector3 d = ArenaStageMesh.Direction(bearing);
                float ground;
                if (temporary.Count > 0)
                {
                    if (!Physics.Raycast(d * r + Vector3.up * 400.0f, Vector3.down, out var hit, 900.0f, ~0, QueryTriggerInteraction.Ignore)) continue;
                    ground = hit.point.y; grounded++;
                    // Not far below the court, where nobody would ever see them, and not up the mountain's back.
                    if (ground < -30.0f || ground > 40.0f) continue;
                }
                else ground = TerraceHeight(r, bearing);
                homes.Add(d * r + Vector3.up * (ground + R(0.6f, 2.4f))); roam.Add(R(2.0f, 4.5f)); i++;
            }
            foreach (var collider in temporary) Object.DestroyImmediate(collider);
            // Along the pit's lip.
            for (int i = 0; i < 22; i++)
            {
                float u = R(-1.0f, 1.0f) * (layout.PitLipHalf + 2.0f), side = layout.PitLipHalf + R(1.0f, 5.0f);
                int edge = i % 4;
                var p = edge == 0 ? new Vector3(u, 0, side) : edge == 1 ? new Vector3(u, 0, -side) : edge == 2 ? new Vector3(side, 0, u) : new Vector3(-side, 0, u);
                homes.Add(p + Vector3.up * (LipY + R(0.5f, 3.0f))); roam.Add(R(1.5f, 3.0f));
            }
            // Down between the monoliths, where no layout has a block: the open air inside the play walls.
            for (int i = 0, tries = 0; i < 26 && tries < 4000; tries++)
            {
                float x = R(-layout.PitHalf + 1.0f, layout.PitHalf - 1.0f), z = R(-layout.PitHalf + 1.0f, layout.PitHalf - 1.0f);
                bool inside = false;
                foreach (var one in layout.Layouts) foreach (var b in one.Blocks) inside |= Mathf.Abs(x - b.X) < b.SX * 0.5f + 0.5f && Mathf.Abs(z - b.Z) < b.SZ * 0.5f + 0.5f;
                if (inside) continue;
                homes.Add(new Vector3(x, R(-7.0f, -0.6f), z)); roam.Add(0.45f); i++;
            }
            // A few bold ones over the court, above head height.
            for (int i = 0; i < 9; i++) { homes.Add(new Vector3(R(-9.0f, 9.0f), R(3.6f, 5.4f), R(-9.0f, 9.0f))); roam.Add(R(1.2f, 2.2f)); }
            life.Homes = homes.ToArray(); life.Roam = roam.ToArray();
            return $"night: {who}, {homes.Count} fireflies' homes, {night.NightOnly.Length} night-only art pieces, {night.NightGlow.Length} glowing renderers";
        }

        // ------------------------------------------------------------------ the stand-in scenery

        /// <summary>The ground's height on the terraced slope at a distance and a bearing from the can: it climbs to the
        /// south west and falls to the north east, in steps of 2.8 m every 6.5 m from 44 m out.</summary>
        internal static float TerraceHeight(float r, float bearing)
        {
            if (r < 44.0f) return LipY;
            int k = Mathf.Min(15, Mathf.FloorToInt((r - 44.0f) / 6.5f));
            return LipY + 2.8f * (k + 1) * Mathf.Cos((bearing - 225.0f) * Mathf.Deg2Rad);
        }

        private static string StandIn(Transform root, PayoLayout layout)
        {
            var group = Group(root, "StandIn");
            var stone = Stone("payo_standin_wall", new Color(0.42f, 0.44f, 0.46f), 0.12f);
            var plaza = Stone("payo_standin_plaza", new Color(0.56f, 0.58f, 0.58f), 0.18f);
            float pit = layout.PitHalf, floor = layout.PitFloor, outer = 44.0f;

            // The pit's walls (their inner faces), the north one broken open, and the plaza round it.
            var pitMesh = new MeshBuilder(1);
            pitMesh.Quad(0, new Vector3(-pit, LipY, -pit), new Vector3(pit, LipY, -pit), new Vector3(pit, floor, -pit), new Vector3(-pit, floor, -pit));          // south
            pitMesh.Quad(0, new Vector3(-pit, LipY, pit), new Vector3(-pit, LipY, -pit), new Vector3(-pit, floor, -pit), new Vector3(-pit, floor, pit));          // west
            pitMesh.Quad(0, new Vector3(pit, LipY, -pit), new Vector3(pit, LipY, pit), new Vector3(pit, floor, pit), new Vector3(pit, floor, -pit));              // east
            pitMesh.Quad(0, new Vector3(-NotchHalf, LipY, pit), new Vector3(-pit, LipY, pit), new Vector3(-pit, floor, pit), new Vector3(-NotchHalf, floor, pit));  // north, west stub
            pitMesh.Quad(0, new Vector3(pit, LipY, pit), new Vector3(NotchHalf, LipY, pit), new Vector3(NotchHalf, floor, pit), new Vector3(pit, floor, pit));      // north, east stub
            // The notch's two cheeks, running out to the valley.
            pitMesh.Quad(0, new Vector3(-NotchHalf, LipY, outer), new Vector3(-NotchHalf, LipY, pit), new Vector3(-NotchHalf, floor, pit), new Vector3(-NotchHalf, floor, outer));
            pitMesh.Quad(0, new Vector3(NotchHalf, LipY, pit), new Vector3(NotchHalf, LipY, outer), new Vector3(NotchHalf, floor, outer), new Vector3(NotchHalf, floor, pit));
            Draw(group, "PitWalls", pitMesh.Save($"{Generated}/payo_standin_pit.asset"), stone);

            var plazaMesh = new MeshBuilder(1);
            void Slab(float x0, float x1, float z0, float z1) =>
                plazaMesh.Quad(0, new Vector3(x0, LipY, z0), new Vector3(x0, LipY, z1), new Vector3(x1, LipY, z1), new Vector3(x1, LipY, z0));
            Slab(-outer, outer, -outer, -pit); Slab(-outer, -pit, -pit, pit); Slab(pit, outer, -pit, pit);
            Slab(-outer, -NotchHalf, pit, outer); Slab(NotchHalf, outer, pit, outer);
            Draw(group, "Plaza", plazaMesh.Save($"{Generated}/payo_standin_plaza.asset"), plaza);

            // The terraces: sixteen steps of forty plots each, every plot at its own level; water, young rice, ripe rice.
            var water = Flat("payo_standin_paddy_water", new Color(0.55f, 0.64f, 0.64f), 0.82f);
            var green = Flat("payo_standin_paddy_green", new Color(0.38f, 0.52f, 0.27f), 0.1f);
            var gold = Flat("payo_standin_paddy_gold", new Color(0.66f, 0.60f, 0.34f), 0.1f);
            var terraces = new MeshBuilder(4);
            const int plots = 40;
            int drawn = 0;
            for (int k = 0; k < 16; k++)
                for (int j = 0; j < plots; j++)
                {
                    float a0 = j * 360.0f / plots, a1 = (j + 1) * 360.0f / plots, mid = (a0 + a1) * 0.5f;
                    // The gorge the broken wall opens into: no terraces straight north for the first steps.
                    if (k < 5 && Mathf.Abs(Mathf.DeltaAngle(mid, 0.0f)) < 17.0f) continue;
                    float r0 = 44.0f + 6.5f * k, r1 = r0 + 6.5f, h = TerraceHeight(r0 + 1.0f, mid), drop = h - 9.0f;
                    Vector3 d0 = ArenaStageMesh.Direction(a0), d1 = ArenaStageMesh.Direction(a1);
                    Vector3 p00 = d0 * r0, p01 = d0 * r1, p10 = d1 * r0, p11 = d1 * r1, up = Vector3.up * h, down = Vector3.up * drop;
                    int hash = (k * 73 + j * 151 + (k * j) % 7) % 10;
                    int kind = hash < 5 ? 1 : hash < 8 ? 2 : 3;
                    terraces.Quad(kind, p00 + up, p01 + up, p11 + up, p10 + up);
                    terraces.Quad(0, p00 + up, p10 + up, p10 + down, p00 + down);       // the inner wall
                    terraces.Quad(0, p01 + up, p11 + up, p11 + down, p01 + down);       // the outer wall
                    terraces.Quad(0, p00 + up, p01 + up, p01 + down, p00 + down);
                    terraces.Quad(0, p10 + up, p11 + up, p11 + down, p10 + down);
                    drawn++;
                }
            var terraceObject = Draw(group, "Terraces", terraces.Save($"{Generated}/payo_standin_terraces.asset"), stone);
            terraceObject.GetComponent<MeshRenderer>().sharedMaterials = new[] { stone, water, green, gold };

            // The mountain behind and the valley in front: a fan from the last terrace to the far ridges.
            var hill = Flat("payo_standin_hills", new Color(0.24f, 0.38f, 0.24f), 0.04f);
            var hills = new MeshBuilder(1);
            float[] rings = { 148.0f, 200.0f, 280.0f, 390.0f, 520.0f, 700.0f, 920.0f, 1200.0f, 1550.0f, 2000.0f };
            const int spokes = 48;
            float Far(float r, float bearing)
            {
                float g = Mathf.Cos((bearing - 225.0f) * Mathf.Deg2Rad);
                float h = LipY + 2.8f * 16.0f * g + Mathf.Min(r - 148.0f, 560.0f) * 0.36f * g;
                // Across the valley the land climbs again: the ranges the moon rises over.
                if (g < 0.0f && r > 760.0f) h += (r - 760.0f) * 0.42f * -g;
                float wobble = Mathf.Sin(bearing * 0.31f + r * 0.011f) * 0.5f + Mathf.Sin(bearing * 0.13f - r * 0.004f) * 0.5f;
                return h + wobble * Mathf.Min(60.0f, (r - 148.0f) * 0.2f);
            }
            for (int ring = 0; ring + 1 < rings.Length; ring++)
                for (int j = 0; j < spokes; j++)
                {
                    float a0 = j * 360.0f / spokes, a1 = (j + 1) * 360.0f / spokes;
                    Vector3 P(float r, float a) => ArenaStageMesh.Direction(a) * r + Vector3.up * Far(r, a);
                    hills.Quad(0, P(rings[ring], a0), P(rings[ring + 1], a0), P(rings[ring + 1], a1), P(rings[ring], a1));
                }
            Draw(group, "Hills", hills.Save($"{Generated}/payo_standin_hills.asset"), hill);

            return $"stand-in scenery: pit walls with a {NotchHalf * 2.0f:0} m notch to the north, a plaza {-LipY:0} m under the court, {drawn} terrace plots, far hills to {rings[rings.Length - 1]:0} m";
        }

        /// <summary>Mist in the pit: three sheets, the lowest nearly solid, out through the notch in the north wall. Lit, so
        /// the night darkens it. ⚠️ BUILT WITH OR WITHOUT THE ART: the art's pit has no floor (its shafts and walls run to
        /// y -50 "for mist to hide"), and the sunk blocks wait under it.</summary>
        private static string Mist(Transform root, PayoLayout layout)
        {
            var group = Group(root, "Mist");
            float pit = layout.PitLipHalf, outer = 44.0f;
            float[] mistY = { layout.MistY, layout.MistY + 5.0f, layout.MistY + 10.0f }, mistA = { 0.94f, 0.4f, 0.2f };
            for (int i = 0; i < mistY.Length; i++)
            {
                var sheet = new MeshBuilder(1);
                float e = pit + 0.5f, n = outer + 30.0f;
                sheet.Quad(0, new Vector3(-e, mistY[i], -e), new Vector3(-e, mistY[i], e), new Vector3(e, mistY[i], e), new Vector3(e, mistY[i], -e));
                sheet.Quad(0, new Vector3(-NotchHalf, mistY[i], e), new Vector3(-NotchHalf, mistY[i], n), new Vector3(NotchHalf, mistY[i], n), new Vector3(NotchHalf, mistY[i], e));
                var mist = Draw(group, "Mist " + i, sheet.Save($"{Generated}/payo_standin_mist_{i}.asset"), Mist("payo_standin_mist_" + i, new Color(0.84f, 0.9f, 0.92f, mistA[i])));
                mist.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            return $"3 sheets of mist in the pit from y {layout.MistY:0}";
        }

        private static GameObject Draw(Transform parent, string name, Mesh mesh, Material material)
        {
            var go = Group(parent, name).gameObject;
            go.isStatic = true;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        /// <summary>A mesh of flat quads with their own corners, in submeshes; saved in place so its GUID survives a rebuild.</summary>
        private sealed class MeshBuilder
        {
            private readonly List<Vector3> _points = new List<Vector3>();
            private readonly List<Vector2> _uvs = new List<Vector2>();
            private readonly List<int>[] _triangles;
            public MeshBuilder(int submeshes) { _triangles = new List<int>[submeshes]; for (int i = 0; i < submeshes; i++) _triangles[i] = new List<int>(); }

            /// <summary>Seen from both sides is not wanted: the face shows on the side its corners run clockwise from.
            /// Both windings are written, so a stand-in face is never missing from where it is looked at.</summary>
            public void Quad(int submesh, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                for (int side = 0; side < 2; side++)
                {
                    int n = _points.Count;
                    _points.Add(a); _points.Add(b); _points.Add(c); _points.Add(d);
                    foreach (var p in new[] { a, b, c, d })
                        _uvs.Add(Mathf.Abs(normal.y) > 0.5f ? new Vector2(p.x, p.z) : new Vector2(p.x * normal.z - p.z * normal.x, p.y));
                    if (side == 0) _triangles[submesh].AddRange(new[] { n, n + 1, n + 2, n, n + 2, n + 3 });
                    else _triangles[submesh].AddRange(new[] { n, n + 2, n + 1, n, n + 3, n + 2 });
                }
            }

            public Mesh Save(string path)
            {
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                bool fresh = mesh == null;
                if (fresh) mesh = new Mesh();
                mesh.Clear();
                mesh.indexFormat = _points.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
                mesh.SetVertices(_points); mesh.SetUVs(0, _uvs);
                mesh.subMeshCount = _triangles.Length;
                for (int i = 0; i < _triangles.Length; i++) mesh.SetTriangles(_triangles[i], i, false);
                mesh.RecalculateNormals(); mesh.RecalculateBounds();
                mesh.name = Path.GetFileNameWithoutExtension(path);
                if (fresh) AssetDatabase.CreateAsset(mesh, path); else EditorUtility.SetDirty(mesh);
                return mesh;
            }
        }

        // ------------------------------------------------------------------ materials

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
        }

        /// <summary>The stand-in's stone: the game's near-fade surface with its near band shut (a body is always beside a
        /// monolith), a flat GREY (owner: "the stones will be more grey than sandy"; the art's ashlar picture is a warm
        /// beige and a tint cannot take that out), with the relief of the art's ashlar if the art has made one yet.</summary>
        private static Material Stone(string name, Color tint, float tiling)
        {
            string path = $"{Generated}/{name}.mat";
            var shader = Shader.Find(NearFade.ShaderName);
            if (shader == null) shader = Shader.Find("Standard");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            var normal = LoadTexture($"{Root}/Textures/ashlar_normal.png", true);
            m.SetColor("_Color", tint);
            m.SetTexture("_MainTex", null); m.SetTextureScale("_MainTex", Vector2.one * tiling);
            if (m.HasProperty("_BumpMap"))
            {
                m.SetTexture("_BumpMap", normal); m.SetTextureScale("_BumpMap", Vector2.one * tiling);
                if (normal != null) m.EnableKeyword("_NORMALMAP"); else m.DisableKeyword("_NORMALMAP");
            }
            m.SetFloat("_Glossiness", 0.08f);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0.0f);
            if (m.HasProperty("_NearFadeStart")) { m.SetFloat("_NearFadeStart", 0.001f); m.SetFloat("_NearFadeEnd", 0.0f); }
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material Flat(string name, Color colour, float smoothness)
        {
            string path = $"{Generated}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(m, path); }
            m.color = colour; m.SetFloat("_Glossiness", smoothness); m.SetFloat("_Metallic", 0.0f);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>A lit, see-through sheet (Standard in its Fade mode): it takes the ambient, so the night darkens it.</summary>
        private static Material Mist(string name, Color colour)
        {
            var m = Flat(name, colour, 0.0f);
            m.SetFloat("_Mode", 2.0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha); m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha); m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON"); m.EnableKeyword("_ALPHABLEND_ON"); m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.SetFloat("_SpecularHighlights", 0.0f); m.SetFloat("_GlossyReflections", 0.0f);
            m.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
            return m;
        }

        // ------------------------------------------------------------------ the art, when there is some

        private const string PaintedShader = "TumbangPreso/LagoonPainted", FoliageShader = "TumbangPreso/LagoonFoliage", RockShader = "TumbangPreso/LagoonRock";
        private const string WaterShader = "TumbangPreso/LagoonCoveWater", FallShader = "TumbangPreso/PayoFall";
        private const string LagoonEdgeAtlas = "Assets/TumbangPreso/Art/LagoonCove/Textures/rock_edges_atlas.png";

        private static bool _vertexColour;

        private enum TexUse { Colour, Normal, Data, Cutout }

        /// <summary>A texture imported the way the Lagoon's builder imports its own (the kind decides sRGB, wrap, alpha and
        /// compression; anisotropic 4), so a painted surface here filters as one does there.</summary>
        private static Texture2D LoadTexture(string path, TexUse use)
        {
            if (!File.Exists(path)) return null;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            bool dirty = false;
            var type = use == TexUse.Normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (importer.textureType != type) { importer.textureType = type; dirty = true; }
            bool srgb = use == TexUse.Colour || use == TexUse.Cutout;
            if (use != TexUse.Normal && importer.sRGBTexture != srgb) { importer.sRGBTexture = srgb; dirty = true; }
            if (use == TexUse.Cutout && !importer.alphaIsTransparency) { importer.alphaIsTransparency = true; dirty = true; }
            if (use == TexUse.Data && importer.textureCompression != TextureImporterCompression.Uncompressed) { importer.textureCompression = TextureImporterCompression.Uncompressed; dirty = true; }
            if (importer.anisoLevel != 4) { importer.anisoLevel = 4; dirty = true; }
            if (dirty) importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Texture2D LoadTexture(string path, bool normal) => LoadTexture(path, normal ? TexUse.Normal : TexUse.Colour);

        private static readonly Dictionary<string, JToken> Specs = new Dictionary<string, JToken>();
        /// <summary>Material names that glow at night (kind glow), cast no shadow (decals, mist), and were warned of as having no spec.</summary>
        private static readonly HashSet<string> Glows = new HashSet<string>(), NoShadow = new HashSet<string>(), Unknown = new HashSet<string>();

        /// <summary>
        /// The art's material specs by name, from `materials_payo.json`. ⚠️ NO MATERIAL IS BUILT HERE ANY MORE: the assembled
        /// art's pieces each carry their own painted atlas INSIDE their glb, so a material is built the first time a glb
        /// that wears it is placed (`BuildMaterial`), from that glb's own texture, and shared by every later piece of the
        /// same name (one material and one picture a name: the other glbs' embedded copies are never loaded by the scene).
        /// </summary>
        private static Dictionary<string, Material> ArtMaterials()
        {
            Specs.Clear(); Glows.Clear(); NoShadow.Clear(); Unknown.Clear();
            Lanterns.Clear(); Lamps.Clear(); Plants.Clear(); FarPlants.Clear(); _terraceSaid = ""; Collided = 0; _plantSaid = ""; _plantLod1 = _plantSway = _plantNoLod1 = 0; NoLod1.Clear(); _faceUp = 0;
            if (!File.Exists(MaterialsPath)) return new Dictionary<string, Material>();
            try
            {
                foreach (var token in JObject.Parse(File.ReadAllText(MaterialsPath))["materials"] as JArray ?? new JArray())
                    if (!string.IsNullOrEmpty(Str(token["name"]))) Specs[Str(token["name"])] = token;
            }
            catch (Exception e) { Warn("unreadable materials_payo.json: " + e.Message); }
            EnsureFolder(Root + "/Materials");
            return new Dictionary<string, Material>();
        }

        private static Texture Picture(Material imported, params string[] properties)
        {
            if (imported == null) return null;
            foreach (string property in properties)
                if (imported.HasProperty(property) && imported.GetTexture(property) != null) return imported.GetTexture(property);
            return null;
        }

        /// <summary>
        /// One of the art's materials, on the shaders the other maps wear, by its `kind` (or, for a spec with none, by its
        /// flags and name as the earlier art's were read):
        ///   painted        TumbangPreso/LagoonPainted: the glb's own atlas and normal, vertex colour where the spec says
        ///   glow           the same, with its picture as emission: dim by day, turned up by the night (`PayoNight.NightGlow`)
        ///   foliage card   TumbangPreso/LagoonFoliage: alpha cut, both faces
        ///   decal          painted, alpha cut, no shadow
        ///   cloud          painted and LIT, so the night darkens it
        ///   water          TumbangPreso/LagoonCoveWater, set for a flooded paddy (`PaddyWater`)
        ///   fall, foam     TumbangPreso/PayoFall: streaks that run down the art's UV
        ///   mist           a soft see-through sheet, lit (the night takes it), no shadow
        ///   unlit          the sky picture: unlit, blended, no fog, the night's to fade
        /// The picture is the file under Textures when the spec's `texture` names one that exists (the kept far pieces and
        /// plants), else the texture inside the glb that is being placed.
        /// </summary>
        private static Material BuildMaterial(string name, JToken token, Material imported)
        {
            var painted = Shader.Find(PaintedShader); var foliage = Shader.Find(FoliageShader); var fall = Shader.Find(FallShader);
            if (painted == null || foliage == null) throw new InvalidOperationException("Missing a Lagoon shader (painted or foliage): Payo wears the same ones");
            bool Flag(string key) => Yes(token[key]);
            string kind = (Str(token["kind"]) ?? Str(token["shader"]) ?? "").ToLowerInvariant();
            if (kind.Length == 0)
                kind = name == "py_mayari" || Flag("unlit") ? "unlit" : name == "py_water" ? "water" : name == "py_fall" ? "fall" : name == "py_foam" ? "foam"
                     : Flag("foliage") ? "foliage card" : Flag("emissive") ? "glow" : "painted";
            if (kind == "foliage") kind = "foliage card";
            if (kind == "payo_land") return LandMaterial(name, token);
            // A kept plant material says kind painted and foliage true: its leaves are cards.
            if (kind == "painted" && Flag("foliage")) kind = "foliage card";
            bool cut = Flag("cutout") || kind == "foliage card" || kind == "decal";
            bool vertex = token["vertex_colour"] != null ? Flag("vertex_colour") : _vertexColour;
            var t = token["tint"] as JArray;
            Color tint = t != null && t.Count >= 3 ? new Color((float)t[0], (float)t[1], (float)t[2], 1.0f)
                       : imported != null && imported.HasProperty("baseColorFactor") ? imported.GetColor("baseColorFactor").gamma : Color.white;
            tint.a = 1.0f;
            float tiling = Num(token["tiling"], 1.0f);
            string texture = Str(token["texture"]);
            Texture albedo = null, normal = null;
            if (!string.IsNullOrEmpty(texture))
            {
                albedo = LoadTexture($"{Root}/Textures/{texture}_albedo.png", cut ? TexUse.Cutout : TexUse.Colour) ?? LoadTexture($"{Root}/Textures/{texture}.png", cut ? TexUse.Cutout : TexUse.Colour);
                normal = LoadTexture($"{Root}/Textures/{texture}_normal.png", TexUse.Normal);
            }
            bool own = albedo == null;
            if (own) { albedo = Picture(imported, "baseColorTexture", "_MainTex", "_BaseMap"); normal = Picture(imported, "normalTexture", "_BumpMap"); tiling = 1.0f; }
            if (albedo == null && !string.IsNullOrEmpty(texture) && kind != "water" && kind != "cloud") Warn($"material '{name}' has no picture (neither Textures/{texture}_albedo.png nor one inside the glb): it is drawn as its flat tint");
            var look = WorldLookProfile.Current.Find(PayoNight.DayRow);

            if (kind == "unlit")
            {
                var sky = GlowMaterial(name, new Color(tint.r * 1.25f, tint.g * 1.25f, tint.b * 1.25f, 1.0f), albedo, true);
                sky.renderQueue = 3200;
                return sky;
            }
            if (kind == "water") return PaddyWater(name, look);
            if (kind == "mist")
            {
                var soft = Mist(name, new Color(0.94f, 0.98f, 1.0f, 0.42f));
                soft.mainTexture = albedo;
                NoShadow.Add(name);
                return soft;
            }

            string path = $"{Root}/Materials/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(painted); AssetDatabase.CreateAsset(m, path); }
            if ((kind == "fall" || kind == "foam") && fall != null)
            {
                m.shader = fall;
                bool foam = kind == "foam";
                m.SetColor("_Color", foam ? new Color(0.9f, 0.97f, 1.0f, 0.9f) : new Color(0.6f, 0.84f, 0.9f, 0.8f));
                m.SetFloat("_Speed", foam ? 0.35f : Num(token["speed"], 3.2f));
                m.SetFloat("_Foam", foam ? 0.85f : 0.25f);
                m.SetFloat("_VertexTint", 0.0f);
                m.renderQueue = (int)RenderQueue.Transparent;
            }
            else if (kind == "foliage card")
            {
                m.shader = foliage;
                m.SetColor("_Color", tint);
                m.SetTexture("_MainTex", albedo); m.SetTextureScale("_MainTex", Vector2.one * tiling);
                m.SetFloat("_Gain", 1.0f); m.SetFloat("_Cutoff", 0.5f); m.SetFloat("_VertexTint", vertex ? 1.0f : 0.0f); m.SetFloat("_Glossiness", 0.3f);
                m.SetFloat("_PaleOn", 0.0f);
                m.renderQueue = -1;
            }
            else
            {
                m.shader = painted;
                bool glow = kind == "glow" || kind == "emissive";
                // An old-style window (the earlier art): dark by day. A glow of the assembled art keeps its picture and only its light changes.
                Color colour = name == "py_window" ? new Color(tint.r * 0.15f, tint.g * 0.15f, tint.b * 0.15f, 1.0f) : tint;
                m.SetColor("_Color", colour);
                m.SetTexture("_MainTex", albedo); m.SetTextureScale("_MainTex", Vector2.one * tiling);
                m.SetTexture("_BumpMap", normal); m.SetTextureScale("_BumpMap", Vector2.one * tiling);
                m.SetFloat("_BumpScale", 1.0f);
                m.SetFloat("_Glossiness", Flag("glossy") ? 0.35f : 0.08f);
                m.SetFloat("_VertexTint", vertex ? 1.0f : 0.0f);
                m.SetFloat("_Cull", cut || Flag("two_sided") ? (float)CullMode.Off : (float)CullMode.Back);
                m.SetFloat("_Cutoff", kind == "decal" ? 0.35f : cut ? 0.5f : 0.0f);
                // By day a glow is a faint light of its own picture; the night's colour for it is set by `Night`.
                // `day_glow` in the spec lifts a glow that must shine by day too (the stamina orb); 0.1 otherwise.
                float day = Num(token["day_glow"], 0.1f);
                m.SetColor("_EmissionColor", glow ? new Color(day, day, day) : Color.black);
                m.SetFloat("_EmissionFromAlbedo", glow && albedo != null ? 1.0f : 0.0f);
                m.SetFloat("_Grain", 0.0f); m.SetFloat("_Haze", 0.0f); m.SetFloat("_NormalToneOn", 0.0f);
                StoneGrain(m, name);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                m.renderQueue = -1;
                if (glow) Glows.Add(name);
                if (kind == "decal") NoShadow.Add(name);
            }
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>
        /// THE STAGE STONE'S DETAIL LAYER (owner, 2026-10-18: "the textures look low res"). Each block wears one painted
        /// atlas over the whole block: 35 to 46 pixels a metre on the blocks, 64 on the court, 7 on the 40 m shafts, so up
        /// close its joints and motifs go soft, where the other maps tile small textures and stay sharp. The painted
        /// shader's grain (`_Grain`) is turned on for the stone as a COLOUR MOTTLE: `Textures/stone_mottle_albedo.png`
        /// (`tools/payo_pieces/stone_mottle.py`), soft greener and redder patches a metre or more across, box-projected
        /// at 10 m and again at 0.61 of that, turned, a few per cent either way (`_GrainHue`). The first pass's fine grey
        /// grain (`stone_grain.py`, 1.4 m) was "a bit too much ... they shouldnt be too fine. they should look like large
        /// splotches of a small random hue offset" (owner, 2026-10-18). The sharpness itself comes from the atlases
        /// (`stone_kit.py` UP: the court's core is painted at twice the pixels). The approved paint is unchanged.
        /// </summary>
        private static readonly System.Text.RegularExpressions.Regex StoneNames = new System.Text.RegularExpressions.Regex(
            "^(core|monolith|mid_|slab_|stone_|bridge_|tower_|ext_|stair_|mover_|pad_|pickup_plinth|py_court_stone)");
        public const float StoneGrainMetres = 10f, StoneGrainFine = 0.61f, StoneGrainHue = 0.06f;

        private static void StoneGrain(Material m, string name)
        {
            if (!StoneNames.IsMatch(name)) return;
            var grain = LoadTexture($"{Root}/Textures/stone_mottle_albedo.png", TexUse.Data);   // linear: its grey is 0.5, no change
            if (grain == null) { Warn("Textures/stone_mottle_albedo.png is missing: the stage stone has no colour mottle"); return; }
            m.SetFloat("_Grain", 1.0f);
            m.SetTexture("_GrainTex", grain);
            m.SetFloat("_GrainScale", StoneGrainMetres);
            m.SetFloat("_GrainFine", StoneGrainFine);
            m.SetFloat("_GrainHue", StoneGrainHue);
        }

        private const string LandShader = "TumbangPreso/PayoLand", LandTextures = Root + "/Textures/land";

        /// <summary>
        /// A land material (kind `payo_land`) on TumbangPreso/PayoLand, the port of calm_paint() in tools/payo_land_blend.py
        /// (docs/PAYO_LAND_SHADER.md). The spec's `surface` picks the shader keyword and the Blender numbers below (tile
        /// metres, relief, anti-tiling rotation, far fade, hue drift tints, roughness), each overridable from the spec:
        /// `tile` (number or [x, y, z]), `relief`, `smoothness`, `far` [start, end], `hue_warm`, `hue_cool` ([r, g, b]),
        /// `origin` (the Unity position of the Blender land origin, [x, y, z]), `vertex_colour`, `colour` ([r, g, b] sRGB),
        /// `glow`. Pictures are read from Textures/land under their tex_calm names unless the spec (or its `textures`
        /// object) names a file for `value`, `height`, `colour`, `shade`, `grass`, `macro` or `rows`.
        /// </summary>
        private static Material LandMaterial(string name, JToken token)
        {
            var shader = Shader.Find(LandShader);
            if (shader == null) { Warn("the shader " + LandShader + " is missing: the land keeps the importer's material"); return null; }
            string[] surfaces = { "slope", "wall", "paddy", "rock", "paving", "flooded", "town_wall", "water", "foam" };
            string[] keywords = { "_SURFACE_SLOPE", "_SURFACE_WALL", "_SURFACE_PADDY", "_SURFACE_ROCK", "_SURFACE_PAVING", "_SURFACE_FLOODED", "_SURFACE_TOWNWALL", "_SURFACE_WATER", "_SURFACE_FOAM" };
            string surface = (Str(token["surface"]) ?? "slope").ToLowerInvariant();
            int index = Array.IndexOf(surfaces, surface);
            if (index < 0) { Warn($"land material '{name}' has an unknown surface '{surface}': it is drawn as a slope"); index = 0; surface = "slope"; }

            // tools/payo_land_blend.py: CALM_TEX (metres, relief), CALM_ROTATE, CALM_FAR, CALM_HUE, the materials' roughness
            string prefix = surface == "slope" ? "grass" : surface == "wall" ? "earth" : surface == "town_wall" ? "bank" : surface;
            float metres = surface switch { "slope" => 20f, "wall" => 6f, "paddy" => 12f, "rock" => 16f, "paving" => 10f, "flooded" => 14f, "water" => 24f, "foam" => 10f, _ => 12f };
            Vector3 tile = surface == "town_wall" ? new Vector3(12f, 12f, 5f) : Vector3.one * metres;
            float relief = surface switch { "slope" => 0.12f, "wall" => 0.06f, "paddy" => 0.02f, "rock" => 0.22f, "paving" => 0.05f, "flooded" => 0.025f, "town_wall" => 0.097f, _ => 0.10f };
            bool rotate = surface == "slope" || surface == "rock" || surface == "flooded" || surface == "water" || surface == "foam";
            Vector2 far = surface == "wall" || surface == "town_wall" ? new Vector2(80f, 240f) : new Vector2(120f, 320f);
            // ⚠️ NOT Blender's roughness (flooded 0.12, river 0.15, foam 0.4): the built scene has no baked reflection (no
            // probe, the skybox reflection is never generated by the batch build), so a glossy surface reflects near black
            // at a grazing view and the river and the flooded paddies read dark grey where Blender's are pale (Logs/payo/unity/v1).
            // The flooded paddies now draw their own sky (the shader's watered paddies, 2026-10-18), so they take none.
            float smoothness = surface == "water" ? 0.35f : surface == "foam" ? 0.2f : 0.0f;
            (Vector3 warm, Vector3 cool) hue = surface switch
            {
                "slope" => (new Vector3(1.10f, 1.07f, 0.84f), new Vector3(0.90f, 1.0f, 1.12f)),
                "town_wall" => (new Vector3(1.06f, 1.01f, 0.92f), new Vector3(0.95f, 0.99f, 1.06f)),
                "paddy" => (new Vector3(1.08f, 1.02f, 0.84f), new Vector3(0.94f, 1.04f, 0.98f)),
                "rock" => (new Vector3(1.07f, 1.01f, 0.91f), new Vector3(0.94f, 0.99f, 1.07f)),
                "paving" => (new Vector3(1.05f, 1.01f, 0.93f), new Vector3(0.96f, 0.99f, 1.05f)),
                "flooded" => (new Vector3(1.02f, 1.0f, 0.96f), new Vector3(0.97f, 1.0f, 1.04f)),
                "wall" => (new Vector3(1.07f, 1.02f, 0.90f), new Vector3(0.95f, 0.99f, 1.06f)),
                _ => (Vector3.one, Vector3.one),
            };
            bool river = surface == "water" || surface == "foam";
            Color riverColour = surface == "foam" ? new Color(0.86f, 0.94f, 0.96f, 1f) : new Color(0.56f, 0.74f, 0.82f, 1f);
            float glow = surface == "foam" ? 0.35f : surface == "water" ? 0.25f : 0.0f;

            Vector3 Vec3(JToken v, Vector3 fallback) => v is JArray a && a.Count >= 3 ? new Vector3((float)a[0], (float)a[1], (float)a[2]) : fallback;
            if (token["tile"] is JArray) tile = Vec3(token["tile"], tile);
            else if (token["tile"] != null) tile = Vector3.one * Num(token["tile"], metres);
            relief = Num(token["relief"], relief);
            smoothness = Num(token["smoothness"], smoothness);
            if (token["far"] is JArray fa && fa.Count >= 2) far = new Vector2((float)fa[0], (float)fa[1]);
            hue = (Vec3(token["hue_warm"], hue.warm), Vec3(token["hue_cool"], hue.cool));
            Vector3 origin = Vec3(token["origin"], Vector3.zero);
            bool vertex = token["vertex_colour"] != null ? Yes(token["vertex_colour"]) : !river;
            Vector3 given = Vec3(token["colour"], new Vector3(riverColour.r, riverColour.g, riverColour.b));
            riverColour = new Color(given.x, given.y, given.z, 1f);
            glow = Num(token["glow"], glow);

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            Texture2D LandPicture(string key, string file, TexUse use, bool sixteen = false, bool clamp = false)
            {
                string named = Str(token[key]) ?? (token["textures"] is JObject tx ? Str(tx[key]) : null);
                string f = named ?? file;
                if (!Path.HasExtension(f)) f += ".png";
                string path = f.StartsWith("Assets/", StringComparison.Ordinal) ? f : $"{LandTextures}/{f}";
                var tex = LoadTexture(path, use);
                if (tex == null) { Warn($"land material '{name}': the picture {path} is missing"); return null; }
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null && (sixteen || clamp))
                {
                    bool dirty = false;
                    // ⚠️ 16 BITS KEPT: the heights and the rows map are 16-bit pngs. At 8 bits the rows' distance (0..130 m)
                    // would step every half metre, coarser than the 1.2 m row pitch, and the bump would terrace.
                    var ps = importer.GetPlatformTextureSettings("Standalone");
                    if (sixteen && (!ps.overridden || ps.format != TextureImporterFormat.R16))
                    {
                        ps.overridden = true; ps.format = TextureImporterFormat.R16;
                        importer.SetPlatformTextureSettings(ps); dirty = true;
                    }
                    if (clamp && importer.wrapMode != TextureWrapMode.Clamp) { importer.wrapMode = TextureWrapMode.Clamp; dirty = true; }
                    if (dirty) { importer.SaveAndReimport(); tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path); }
                }
                return tex;
            }
            float[] Numbers(string key, string file, int count)
            {
                if (token[key] is JArray a && a.Count >= count) return a.Take(count).Select(e => (float)e).ToArray();
                foreach (string path in new[] { $"{LandTextures}/{file}", Path.Combine(projectRoot, "Logs", "payo", "terrain", "tex_calm", file) })
                {
                    if (!File.Exists(path)) continue;
                    var words = File.ReadAllText(path).Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    var values = new List<float>();
                    foreach (string w in words)
                        if (values.Count < count && float.TryParse(w, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float v)) values.Add(v);
                    if (values.Count == count) return values.ToArray();
                }
                return null;
            }
            float Luminance(float r, float g, float b) => 0.2126f * Mathf.Pow(r, 2.2f) + 0.7152f * Mathf.Pow(g, 2.2f) + 0.0722f * Mathf.Pow(b, 2.2f);

            string matPath = $"{Root}/Materials/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, matPath); }
            m.shader = shader;
            var defaults = new Material(shader);
            m.CopyPropertiesFromMaterial(defaults);
            Object.DestroyImmediate(defaults);
            m.shaderKeywords = new[] { keywords[index] };
            m.SetFloat("_Surface", index);
            m.SetVector("_BlenderOrigin", origin);
            m.SetFloat("_VertexColour", vertex ? 1f : 0f);
            m.SetColor("_BaseColour", riverColour);
            m.SetFloat("_EmissionStrength", glow);
            m.SetFloat("_Glossiness", smoothness);
            m.SetVector("_TileMetres", tile);
            m.SetFloat("_Relief", relief);
            m.SetFloat("_AntiTileRotate", rotate ? 1f : 0f);
            m.SetFloat("_Feather", rotate ? 0.08f : 0.03f);
            m.SetVector("_FarFade", far);
            m.SetVector("_HueWarm", hue.warm);
            m.SetVector("_HueCool", hue.cool);
            m.SetFloat("_HueOn", river ? 0f : 1f);
            m.SetTexture("_MacroTex", LandPicture("macro", "macro_value", TexUse.Data));
            m.SetFloat("_MacroMetres", 96f);
            if (surface == "wall" || surface == "town_wall")
            {
                m.SetTexture("_ColourTex", LandPicture("colour", prefix + "_color", TexUse.Colour));
                m.SetTexture("_HeightTex", LandPicture("height", prefix + "_height", TexUse.Data, sixteen: true));
                // payo_land.py's WALL and TOWN_WALL: the earth and the stones take their wall's brightness, not its green
                m.SetFloat("_WallLum", surface == "wall" ? Luminance(0.40f, 0.55f, 0.31f) : Luminance(0.43f, 0.56f, 0.33f));
                m.SetFloat("_Step", Num(token["step"], 5f));
                if (surface == "wall")
                {
                    m.SetTexture("_GrassTex", LandPicture("grass", "grass_value", TexUse.Data));
                    m.SetFloat("_GrassMetres", 20f);
                    m.SetVector("_WallMean", new Vector4(0.51f, 0.39f, 0.27f, 1f));       // EARTH_MEAN, what the earth fades to
                }
                else
                {
                    m.SetTexture("_ShadeTex", LandPicture("shade", "bank_shade", TexUse.Data));
                    var mean = Numbers("wall_mean", "bank_mean.txt", 4) ?? new[] { 0.5878f, 0.5704f, 0.5370f, 0.9416f };
                    m.SetVector("_WallMean", new Vector4(mean[0], mean[1], mean[2], mean[3]));
                }
            }
            else
            {
                m.SetTexture("_ValueTex", LandPicture("value", prefix + "_value", TexUse.Data));
                m.SetTexture("_HeightTex", LandPicture("height", prefix + "_height", TexUse.Data, sixteen: true));
            }
            if (surface == "paddy" || surface == "flooded")
            {
                // WATERED PADDIES (owner, 2026-10-18): the fields tools/payo_pieces/paddy_flood.py put under water, as a world
                // map over the paddy and flooded surfaces; the shader draws the water and its sky (docs/PAYO_LAND_SHADER.md).
                m.SetTexture("_FloodTex", LandPicture("flood", "paddy_flood", TexUse.Data, clamp: true));
                var box = Numbers("flood_bounds", "paddy_flood.txt", 4);
                if (box == null) Warn($"land material '{name}': no paddy_flood.txt (x0 y0 x1 y1): the watered paddies use the shader's box");
                else m.SetVector("_FloodBounds", new Vector4(box[0], box[1], box[2], box[3]));
            }
            if (surface == "paddy")
            {
                m.SetTexture("_RowsTex", LandPicture("rows", "paddy_rows", TexUse.Data, sixteen: true, clamp: true));
                var rows = Numbers("rows_bounds", "paddy_rows.txt", 5);
                if (rows == null) Warn($"land material '{name}': no paddy_rows.txt (x0 y0 x1 y1 dmax): the rows use the shader's defaults");
                else
                {
                    m.SetVector("_RowsBounds", new Vector4(rows[0], rows[1], rows[2], rows[3]));
                    m.SetVector("_RowsShape", new Vector4(1.2f, 0.08f, rows[4], 0.05f));   // ROW_PITCH, ROW_AMP, dmax, the rows' bump
                }
            }
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            m.renderQueue = -1;
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>The map's material of a name, built from its spec the first time it is met; null where there is no spec (said once).</summary>
        private static Material MaterialOf(string name, Material imported, Dictionary<string, Material> named)
        {
            if (named.TryGetValue(name, out var m)) return m;
            if (!Specs.TryGetValue(name, out var token))
            {
                if (Specs.Count > 0 && Unknown.Add(name)) Warn($"no material spec for '{name}': it keeps the importer's material");
                return null;
            }
            m = BuildMaterial(name, token, imported);
            if (m != null) named[name] = m;
            return m;
        }

        /// <summary>
        /// The game's water (TumbangPreso/LagoonCoveWater) for a FLOODED PADDY, a shelf pool or a pit pool, set up the way
        /// the Lagoon's builder and the run's set theirs: the shader's defaults first (a saved material would otherwise
        /// keep whatever it was first given), then this map's. The shader colours by the depth STRAIGHT DOWN to what is
        /// behind the surface, and a paddy is 12 cm of water over mud: with the sea's numbers the whole sheet would sit
        /// inside the half-metre foam band, solid white. So: a foam line of 3 cm where water meets a dike, the middle
        /// colour by 30 cm and the deep one by 1.5 m, no swell (a paddy has no fetch), fine ripples, and MORE of the sky
        /// in it than a pool has (a terrace's water is a mirror: that is what a rice terrace looks like).
        /// </summary>
        private static Material PaddyWater(string name, WorldLookProfile.MapLook look)
        {
            var shader = Shader.Find(WaterShader);
            if (shader == null) { Warn("the shader " + WaterShader + " is missing: the water keeps a flat tint"); return null; }
            string path = $"{Root}/Materials/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            var defaults = new Material(shader);
            m.CopyPropertiesFromMaterial(defaults);
            Object.DestroyImmediate(defaults);
            // Paler than the cove's sea: the approved Blender pools (pool_basin, the head's plunge pool) read pale sky blue.
            m.SetColor("_ShallowColor", new Color(0.62f, 0.84f, 0.88f, 1.0f));
            m.SetColor("_MidColor", new Color(0.50f, 0.76f, 0.84f, 1.0f));
            m.SetColor("_DeepColor", new Color(0.36f, 0.62f, 0.74f, 1.0f));
            m.SetFloat("_MidDepth", 0.3f); m.SetFloat("_DeepDepth", 1.5f);
            m.SetFloat("_ShallowAlpha", 0.55f); m.SetFloat("_DeepAlpha", 0.85f); m.SetFloat("_ClarityDepth", 4.0f);
            m.SetFloat("_WaveHeight", 0.0f);
            m.SetFloat("_FoamWidth", 0.03f);
            m.SetFloat("_RippleScale", 1.1f); m.SetFloat("_RippleStrength", 0.45f);
            m.SetFloat("_CausticStrength", 0.3f); m.SetFloat("_CausticScale", 1.0f);
            m.SetFloat("_Reflection", 0.5f); m.SetFloat("_SunPath", 0.5f); m.SetFloat("_GlintStrength", 1.0f);
            if (look != null) m.SetColor("_SkyColor", Color.Lerp(look.Zenith, Color.white, 0.4f));
            m.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>
        /// What every art piece gets once it wears the map's materials, as the Lagoon's builder gives its own: shadows
        /// cast, the water's depth request on a water surface (and no shadow from it), and static flags for a piece that
        /// stands still: all of them, less batching where the shader reads the object's own transform (the rock's edge
        /// band and the foliage's per-plant nudge would silently read an identity matrix in Play).
        /// </summary>
        private static void Finish(GameObject go, bool still)
        {
            var all = (StaticEditorFlags)~0;
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
            {
                var r = t.GetComponent<Renderer>();
                bool water = false, readsTransform = false, clear = false;
                if (r != null)
                {
                    foreach (var m in r.sharedMaterials)
                    {
                        if (m == null || m.shader == null) continue;
                        water |= m.shader.name == WaterShader;
                        clear |= m.shader.name == FallShader || m.shader.name == "TumbangPreso/ArenaGlow";
                        readsTransform |= m.shader.name == RockShader || m.shader.name == FoliageShader;
                    }
                    bool decal = r.sharedMaterials.Any(m => m != null && (m.name == "py_decal" || NoShadow.Contains(m.name)));
                    r.shadowCastingMode = water || clear || decal ? ShadowCastingMode.Off : ShadowCastingMode.On;
                    if (water && r.GetComponent<WaterDepthRequest>() == null) r.gameObject.AddComponent<WaterDepthRequest>();
                }
                if (!still) continue;
                // Water is not static (the shader's waves are in its own frame), as the Lagoon's sea is not.
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, water ? 0 : readsTransform ? all & ~StaticEditorFlags.BatchingStatic : all);
            }
        }

        private static void Rematerial(GameObject go, Dictionary<string, Material> named)
        {
            if (Specs.Count == 0) return;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    var m = MaterialOf(mats[i].name.Replace(" (Instance)", "").Trim(), mats[i], named);
                    if (m != null) mats[i] = m;
                }
                r.sharedMaterials = mats;
            }
        }

        private static bool IsNight(JToken p, Art art)
        {
            string model = p["model"] != null ? (string)p["model"] : null;
            return (p["night"] != null && p["night"].Type == JTokenType.Boolean && (bool)p["night"]) || (model != null && art.Night.Contains(model));
        }

        private static GameObject Place(JToken p, Transform parent, Vector3 origin, Dictionary<string, Material> named)
        {
            string model = p["model"] != null && p["model"].Type == JTokenType.String ? (string)p["model"] : null;
            var position = p["position"] as JArray;
            if (string.IsNullOrEmpty(model) || position == null || position.Count < 3) { Warn("a placement has no model or no position"); return null; }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Models/{model}.glb");
            if (prefab == null) { Warn("missing model Models/" + model + ".glb"); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            float yaw = p["yaw"] != null && (p["yaw"].Type == JTokenType.Float || p["yaw"].Type == JTokenType.Integer) ? (float)p["yaw"] : 0.0f;
            float scale = p["scale"] != null && (p["scale"].Type == JTokenType.Float || p["scale"].Type == JTokenType.Integer) ? (float)p["scale"] : 0.0f;
            go.transform.localPosition = origin + new Vector3((float)position[0], (float)position[1], (float)position[2]);
            go.transform.localRotation = Quaternion.Euler(0.0f, yaw, 0.0f);
            if (scale > 0.0f) go.transform.localScale = Vector3.one * scale;
            // `scale3`: a piece not scaled uniformly (the cliff_wall run below the pit's open front is 1.35 tall).
            if (p["scale3"] is JArray s3 && s3.Count >= 3) go.transform.localScale = new Vector3((float)s3[0], (float)s3[1], (float)s3[2]);
            Rematerial(go, named);
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            // ⚠️ `collide: true` GIVES NO COLLIDER (owner, 2026-10-18: "the falling mechanic doesnt exist yet btw when you
            // fall off"). The court piece's pit floor (y -30) and the land under it answered `ArenaFallRecovery`'s rays,
            // which ask EVERY layer for "stage floor": over the pit `OverTheVoid` was false, so the updraft began only
            // under the blocks' lowest underside instead of 0.3 m under the deck, a slipper's void return never took the
            // void path, and a body that got past the play walls could stand on the paving outside them for good. The stage's own
            // boxes and play walls are the gameplay; the land rays that need the art's ground (life, blades) make their
            // own temporary colliders. The flag stays in the art file for a future dedicated layer.
            return go;
        }

        /// <summary>The art's fixed pieces under "Dressing" (a block's own pieces were hung on the block by `Stage`), and its sky piece.</summary>
        /// <summary>One of the art's flames as a fire the night lights: a holder with `PayoFlame`, the flame model as its one
        /// tongue (hidden by day, breathing at night) and a warm point light.</summary>
        private static GameObject Fire(JToken p, Transform parent, Vector3 origin, Dictionary<string, Material> named)
        {
            var position = p["position"] as JArray;
            if (position == null || position.Count < 3) return null;
            var holder = Group(parent, "Fire");
            Vector3 at = origin + new Vector3((float)position[0], (float)position[1], (float)position[2]);
            holder.localPosition = at;
            var model = Hang(Str(p["model"]), holder, Vector3.zero, "flame", named);
            if (model == null) { Object.DestroyImmediate(holder.gameObject); return null; }
            float scale = p["scale"] != null && (p["scale"].Type == JTokenType.Float || p["scale"].Type == JTokenType.Integer) ? (float)p["scale"] : 1.0f;
            float yaw = p["yaw"] != null && (p["yaw"].Type == JTokenType.Float || p["yaw"].Type == JTokenType.Integer) ? (float)p["yaw"] : 0.0f;
            model.transform.localScale = Vector3.one * (scale > 0.0f ? scale : 1.0f);
            model.transform.localRotation = Quaternion.Euler(0.0f, yaw, 0.0f);
            foreach (var r in model.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; }
            model.SetActive(false);
            var flame = holder.gameObject.AddComponent<PayoFlame>();
            flame.Tongues = new[] { model.transform };
            flame.Rest = new[] { model.transform.localScale };
            flame.RestYaw = new[] { yaw };
            Vector3 world = holder.position;
            flame.Order = Mathf.Repeat(Mathf.Atan2(world.x, world.z) / (Mathf.PI * 2.0f), 1.0f);
            flame.Brightness = 2.0f * Mathf.Max(1.0f, scale);
            var lamp = Group(holder, "glow");
            lamp.localPosition = new Vector3(0.0f, 0.6f * Mathf.Max(1.0f, scale), 0.0f);
            var light = lamp.gameObject.AddComponent<Light>();
            light.type = LightType.Point; light.range = 9.0f * Mathf.Max(1.0f, scale); light.color = new Color(1.0f, 0.62f, 0.26f); light.intensity = 0.0f;
            light.shadows = LightShadows.None; light.enabled = false;
            flame.Glow = light;
            return holder.gameObject;
        }

        private static readonly List<Renderer> Lanterns = new List<Renderer>();
        private static readonly List<Light> Lamps = new List<Light>();
        private static readonly List<Transform> Plants = new List<Transform>();
        /// <summary>Mesh colliders given to `collide: true` placements in this build.</summary>
        private static int Collided;

        /// <summary>
        /// THE FLAMES INSIDE A PLACED PIECE (the assembled art: child nodes whose names end "__flame" in fire_bowl, torch and
        /// hearth_pot): hidden by day, and given to a `PayoFlame` on the piece so the night lights and breathes them. A node
        /// ending "__lantern" is a lamp's glass: its renderers are turned up with the night. Returns the fires made (0 or 1).
        /// </summary>
        /// <summary>The art's ground by its pieces' names: the assembled land, and the earlier sets' mountain, slopes, shelf and paddies.</summary>
        internal static bool IsLand(string name) =>
            name.StartsWith("payo_land", StringComparison.Ordinal) || name.StartsWith("payo_mountain", StringComparison.Ordinal) || name.StartsWith("payo_slopes", StringComparison.Ordinal)
            || name.StartsWith("payo_shelf", StringComparison.Ordinal) || name.StartsWith("payo_paddy", StringComparison.Ordinal);

        private static int Fires(GameObject piece)
        {
            var tongues = new List<Transform>();
            foreach (var t in piece.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.EndsWith("__flame", StringComparison.Ordinal)) tongues.Add(t);
                else if (t.name.EndsWith("__lantern", StringComparison.Ordinal)) Lanterns.AddRange(t.GetComponentsInChildren<Renderer>(true));
            }
            if (tongues.Count == 0) return 0;
            // A flame node's origin is the PIECE's (the glb gives it no offset): hang it on a pivot at its own foot, so the
            // night stretches it from the bowl's rim and not from the ground.
            for (int k = 0; k < tongues.Count; k++)
            {
                var drawn = tongues[k].GetComponentsInChildren<Renderer>(true);
                if (drawn.Length == 0) continue;
                Bounds b = drawn[0].bounds;
                foreach (var r in drawn) b.Encapsulate(r.bounds);
                var pivot = new GameObject(tongues[k].name + " pivot").transform;
                pivot.SetParent(tongues[k].parent, false);
                pivot.SetPositionAndRotation(new Vector3(b.center.x, b.min.y, b.center.z), piece.transform.rotation);
                tongues[k].SetParent(pivot, true);
                tongues[k] = pivot;
            }
            var flame = piece.AddComponent<PayoFlame>();
            flame.Tongues = tongues.ToArray();
            flame.Rest = tongues.Select(t => t.localScale).ToArray();
            flame.RestYaw = tongues.Select(t => t.localEulerAngles.y).ToArray();
            Vector3 world = piece.transform.position;
            flame.Order = Mathf.Repeat(Mathf.Atan2(world.x, world.z) / (Mathf.PI * 2.0f), 1.0f);
            foreach (var t in tongues)
            {
                foreach (var r in t.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; }
                foreach (var x in t.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(x.gameObject, 0);
                t.gameObject.SetActive(false);
            }
            return 1;
        }

        /// <summary>
        /// One of the art's night lights (`night.lights`): a warm point light, off by day. It is given to the fire it stands
        /// at (a `PayoFlame` within 2.5 m under the same parent, or anywhere for `all`), which then lights and breathes it
        /// in its turn; a light at no fire is a lantern's, brought up by `PayoNight` with the dark.
        /// </summary>
        private static void Lamp(Transform parent, Vector3 local, float range, PayoFlame[] all)
        {
            var holder = Group(parent, "night light");
            holder.localPosition = local;
            var light = holder.gameObject.AddComponent<Light>();
            light.type = LightType.Point; light.range = Mathf.Max(3.0f, range); light.color = new Color(1.0f, 0.62f, 0.26f); light.intensity = 0.0f;
            light.shadows = LightShadows.None; light.enabled = false;
            PayoFlame nearest = null; float best = 2.5f * 2.5f;
            foreach (var flame in all ?? parent.GetComponentsInChildren<PayoFlame>(true))
            {
                if (flame == null || flame.Glow != null) continue;
                Vector3 d = flame.transform.position - holder.position; d.y = 0.0f;
                if (d.sqrMagnitude < best) { best = d.sqrMagnitude; nearest = flame; }
            }
            if (nearest != null) { nearest.Glow = light; nearest.Brightness = 2.0f; }
            else Lamps.Add(light);
        }

        /// <summary>
        /// THE TERRACES AS A FEW MESHES, NOT 2,783 OBJECTS. Every terrace placement's meshes are joined, by where they stand
        /// (a 72 m grid) and by material, into one mesh a cell with a submesh a material, saved under Generated: the art's
        /// own level of detail per placement is kept as placed (full near the pit, mid, far), the vertex colours and UVs
        /// ride along, and a cell is static. Returns false, having made nothing, if a terrace mesh cannot be read (the
        /// placements are then placed one by one, static, and drawn by instancing).
        /// </summary>
        private static bool MergeTerraces(Transform root, List<JToken> placements, Dictionary<string, Material> named, ref int missing, out string said)
        {
            said = "";
            const float cell = 72.0f;
            var buckets = new Dictionary<(int, int), Dictionary<string, List<CombineInstance>>>();
            var materialOf = new Dictionary<string, Material>();
            int joined = 0;
            foreach (var p in placements)
            {
                var prefab = Model(Str(p["model"]));
                var position = p["position"] as JArray;
                if (prefab == null || position == null || position.Count < 3) { missing++; if (prefab == null) Warn("missing model Models/" + Str(p["model"]) + ".glb"); continue; }
                var at = new Vector3((float)position[0], (float)position[1], (float)position[2]);
                float scale = Num(p["scale"], 1.0f);
                var place = Matrix4x4.TRS(at, Quaternion.Euler(0.0f, Num(p["yaw"], 0.0f), 0.0f), Vector3.one * (scale > 0.0f ? scale : 1.0f));
                var toRoot = prefab.transform.worldToLocalMatrix;
                var key = (Mathf.FloorToInt(at.x / cell), Mathf.FloorToInt(at.z / cell));
                foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                {
                    var mesh = filter.sharedMesh; var renderer = filter.GetComponent<Renderer>();
                    if (mesh == null || renderer == null) continue;
                    if (!mesh.isReadable) { said = "the terrace mesh '" + mesh.name + "' of " + prefab.name + " is not readable"; return false; }
                    var mats = renderer.sharedMaterials;
                    for (int sub = 0; sub < mesh.subMeshCount && sub < mats.Length; sub++)
                    {
                        if (mats[sub] == null) continue;
                        string name = mats[sub].name.Replace(" (Instance)", "").Trim();
                        if (!materialOf.ContainsKey(name)) materialOf[name] = MaterialOf(name, mats[sub], named) ?? mats[sub];
                        if (!buckets.TryGetValue(key, out var byMaterial)) buckets[key] = byMaterial = new Dictionary<string, List<CombineInstance>>();
                        if (!byMaterial.TryGetValue(name, out var list)) byMaterial[name] = list = new List<CombineInstance>();
                        list.Add(new CombineInstance { mesh = mesh, subMeshIndex = sub, transform = place * toRoot * filter.transform.localToWorldMatrix });
                    }
                }
                joined++;
            }
            var holder = Group(root, "Terraces");
            var written = new HashSet<string>();
            int triangles = 0;
            foreach (var bucket in buckets)
            {
                var names = bucket.Value.Keys.OrderBy(n => n).ToList();
                var parts = new List<CombineInstance>();
                var temporary = new List<Mesh>();
                foreach (string name in names)
                {
                    var one = new Mesh { indexFormat = IndexFormat.UInt32 };
                    one.CombineMeshes(bucket.Value[name].ToArray(), true, true);
                    temporary.Add(one);
                    parts.Add(new CombineInstance { mesh = one, subMeshIndex = 0, transform = Matrix4x4.identity });
                }
                string path = $"{Generated}/terrace_{bucket.Key.Item1}_{bucket.Key.Item2}.asset";
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                bool fresh = mesh == null;
                if (fresh) mesh = new Mesh();
                mesh.Clear();
                mesh.indexFormat = IndexFormat.UInt32;
                mesh.CombineMeshes(parts.ToArray(), false, false);
                mesh.RecalculateBounds();
                mesh.name = Path.GetFileNameWithoutExtension(path);
                if (fresh) AssetDatabase.CreateAsset(mesh, path); else EditorUtility.SetDirty(mesh);
                written.Add(path);
                foreach (var one in temporary) Object.DestroyImmediate(one);
                for (int sub = 0; sub < mesh.subMeshCount; sub++) triangles += (int)(mesh.GetIndexCount(sub) / 3);
                var go = Group(holder, mesh.name).gameObject;
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterials = names.Select(n => materialOf[n]).ToArray();
                Finish(go, true);
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Mesh", new[] { Generated }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileName(path).StartsWith("terrace_", StringComparison.Ordinal) && !written.Contains(path)) AssetDatabase.DeleteAsset(path);
            }
            said = $"{joined} terrace placements joined into {buckets.Count} static meshes ({triangles} triangles, {materialOf.Count} materials)";
            return true;
        }

        private static int Dressing(Transform root, PayoLayout layout, Art art, Dictionary<string, Material> named, List<GameObject> nightOnly, List<Renderer> nightGlow, ref int missing, out Renderer[] mayari)
        {
            mayari = null;
            // The terraces first, as a few joined meshes; one by one only if they cannot be joined.
            var terraces = art.Placements.Where(p => Str(p["group"]) == "terrace" && Str(p["on_block"]) == null).ToList();
            bool merged = false;
            if (terraces.Count > 0)
            {
                merged = MergeTerraces(root, terraces, named, ref missing, out _terraceSaid);
                if (!merged) Warn("the terraces were NOT joined (" + _terraceSaid + "): " + terraces.Count + " separate objects, static and instanced");
            }
            else _terraceSaid = "no terrace placements";
            string skyModel = art.Doc["sky"] is JObject skyNote ? Str(skyNote["model"]) : null;
            var skyFound = new List<Renderer>();
            var dressing = Group(root, "Dressing");
            var groups = new Dictionary<string, Transform>();
            int placed = 0;
            foreach (var p in art.Placements)
            {
                if (Str(p["on_block"]) != null || Str(p["block"]) != null) continue;
                if (merged && Str(p["group"]) == "terrace") { placed++; continue; }
                string model = p["model"] != null && p["model"].Type == JTokenType.String ? (string)p["model"] : "";
                if (!groups.TryGetValue(model, out var group)) groups[model] = group = Group(dressing, model.Length > 0 ? model : "unnamed");
                if (model == "fire_flame") { if (Fire(p, group, Vector3.zero, named) != null) placed++; else missing++; continue; }
                // A plant far from the court is not an object: it is listed for the instanced PayoFlora (Plants.cs).
                if (Str(p["group"]) == "plant" && ListFar(p, model, Str(p["family"]) == "trees")) { placed++; continue; }
                var go = Place(p, group, Vector3.zero, named);
                if (go == null) { missing++; continue; }
                // A plant gets levels of detail, and sways (`PayoLife`) only near the court; everything else that stands on the land is static.
                bool plant = Str(p["group"]) == "plant";
                Finish(go, !plant && Str(p["group"]) != "life" && model != skyModel);
                if (plant) PlantLod(go, model, Str(p["family"]) == "trees", named);
                if (model == "payo_river") FaceUp(go);
                Fires(go);
                if (model == skyModel)
                {
                    // Mayari: no shadow, never static-batched with the land, shown by the night alone.
                    foreach (var r in go.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; skyFound.Add(r); }
                }
                else if (Str(p["group"]) == "night" || (p["night"] != null && p["night"].Type == JTokenType.Boolean && (bool)p["night"])) nightOnly.Add(go);
                placed++;
            }
            if (skyFound.Count > 0) mayari = skyFound.ToArray();
            // Kept apart from _plantSaid: PlantLod rewrites that line on every near plant, the plaza's among them.
            _farSaid = FarFlora(root, named);
            foreach (var g in groups.Values.ToList()) if (g != null && g.childCount == 0) Object.DestroyImmediate(g.gameObject);
            // The art's lights that stand on the land (those on a block were hung on it by `Stage`).
            var fires = dressing.GetComponentsInChildren<PayoFlame>(true);
            var lamps = Group(root, "NightLights");
            foreach (var light in art.Lights)
            {
                var position = light["position"] as JArray;
                if (Str(light["on_block"]) != null || position == null || position.Count < 3) continue;
                Lamp(lamps, new Vector3((float)position[0], (float)position[1], (float)position[2]), Num(light["range"], 6.0f), fires);
            }
            return placed;
        }

        private static Transform Group(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }
    }
}
