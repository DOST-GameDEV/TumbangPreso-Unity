using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using TumbangPreso.Map;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// SAMPLE MAP: the Arena grey-box (docs/ARENA_MAP_BRIEF.md, ARENA-1.1). Boxes only, flat
    /// greys: it exists to prove the layout rotation, the fall, the pads and the pickup before
    /// any art.
    ///
    /// EVERY NUMBER COMES FROM tools/arena_greybox_layout.json, which the Blender view reads
    /// too, so the two cannot drift. Unity coordinates, metres. A piece keeps its id across the
    /// layouts it is in (that is how `ArenaStage` knows it travels); an id a layout does not
    /// name is absent from that layout and sinks into the pit. Any number of layouts.
    ///
    /// WHAT IS FIXED IN EVERY LAYOUT: the `Bounds` walls (inner faces at +/- wallHalf, read once
    /// by `MatchInstaller.MeasurePlayableBounds`), and solid floor under the can and under the
    /// spawn marks. The second is CHECKED here and the build refuses a layout that breaks it.
    ///
    /// The stage's colliders are NOT under `Bounds`: everything under that object is measured
    /// as a wall.
    ///
    /// Menu: Tumbang Preso/Sample Map. Batch: TumbangPreso.EditorTools.MapKit.ArenaSceneBuilder.Run
    /// </summary>
    public static class ArenaSceneBuilder
    {
        public const string ScenePath = "Assets/TumbangPreso/Scenes/Maps/Arena.unity";
        public const string LayoutPath = "tools/arena_greybox_layout.json";
        private const string MaterialFolder = "Assets/TumbangPreso/Art/Arena/Materials";
        private const string Tag = "[Arena] ";

        /// <summary>Upward launch in m/s unless a pad names its own. At `Balance.Gravity` 20 the
        /// apex is v*v/40: 15.5 gives 6.0 m, under the 12 m ceiling.</summary>
        private const float JumpPadSpeed = 15.5f;
        private const float WallThickness = 0.4f;

        // Flat greys, one material per kind, and nothing near the team hues #f87020 and #0080e8.
        private static readonly Dictionary<string, Color> KindColours = new Dictionary<string, Color>
        {
            ["slab"] = new Color(0.56f, 0.56f, 0.58f),
            ["dais"] = new Color(0.72f, 0.71f, 0.68f),
            ["ramp"] = new Color(0.46f, 0.47f, 0.50f),
            ["bridge"] = new Color(0.40f, 0.41f, 0.44f),
            ["pit"] = new Color(0.10f, 0.10f, 0.12f),
            ["stand"] = new Color(0.34f, 0.34f, 0.38f),
            ["screen"] = new Color(0.08f, 0.10f, 0.12f),
            ["booth"] = new Color(0.52f, 0.47f, 0.40f),
            ["rig"] = new Color(0.20f, 0.20f, 0.22f),
            ["tower"] = new Color(0.28f, 0.28f, 0.31f),
        };

        private struct Box { public string Id, Kind; public Vector3 Center, Size, Euler; }

        [MenuItem("Tumbang Preso/Sample Map/Build Arena Grey-box")]
        public static void BuildFromMenu() { Build(); Open(); }

        public static void Run() { Build(); EditorApplication.Exit(0); }

        public static void Open() { EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single); }

        public static void Build()
        {
            var doc = JObject.Parse(File.ReadAllText(LayoutPath));
            float stageTop = Number(doc["stageTop"], 0.0f);
            float wallHalf = Number(doc["wallHalf"], 14.0f);
            float wallHeight = Number(doc["wallHeight"], 12.0f);
            float pitY = Number(doc["pitY"], -6.0f);
            var layouts = doc["layouts"] as JArray;
            if (layouts == null || layouts.Count == 0) throw new InvalidOperationException(LayoutPath + " names no layouts");

            var materials = new Dictionary<string, Material>();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Arena").transform;
            root.gameObject.AddComponent<MapGrade>().Set(1.0f, 1.0f, 1.0f, 1.0f, 1.9f);

            int pieces = Stage(root, layouts, doc["spawns"], stageTop, pitY, materials);
            Pit(root, wallHalf, stageTop, pitY, materials);
            int surround = Surround(root, doc["surround"] as JArray, materials);
            Gameplay(root, doc["spawns"], wallHalf, wallHeight, stageTop, pitY);
            Lighting(root);

            var eye = Group(root, "BreakCamera").gameObject;
            var camera = eye.AddComponent<Camera>();
            camera.enabled = false;
            camera.depth = ArenaBreakCamera.Depth;
            camera.fieldOfView = 50.0f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 240.0f;
            eye.transform.SetPositionAndRotation(new Vector3(15, 11, 21), Quaternion.LookRotation(new Vector3(-15, -10.5f, -21)));
            eye.AddComponent<ArenaBreakCamera>();

            EditorSceneManager.MarkSceneDirty(scene);
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Could not save " + ScenePath);
            AssetDatabase.SaveAssets();
            AddSceneToBuildSettings();
            Debug.Log($"{Tag}Scene built: {layouts.Count} layouts, {pieces} stage pieces, {surround} surround boxes, {materials.Count} materials.");
        }

        // ------------------------------------------------------------------ the stage

        /// <summary>The moving pieces, twice (a collider and a visual each), each layout's pads
        /// and pickups, and the `ArenaStage` that holds the poses.</summary>
        private static int Stage(Transform root, JArray layouts, JToken spawns, float stageTop, float pitY,
                                 Dictionary<string, Material> materials)
        {
            int count = layouts.Count;
            var stageObject = Group(root, "Stage");
            var stage = stageObject.gameObject.AddComponent<ArenaStage>();
            stageObject.gameObject.AddComponent<ArenaFallRecovery>();
            var colliders = Group(stageObject, "Colliders");
            var visuals = Group(stageObject, "Visuals");
            var features = Group(stageObject, "Features");

            // Every id, in the order first met, with its box in each layout that has it.
            var order = new List<string>();
            var byId = new Dictionary<string, Box?[]>();
            var perLayout = new List<Box>[count];
            float highest = stageTop;
            for (int l = 0; l < count; l++)
            {
                perLayout[l] = new List<Box>();
                var list = layouts[l]["pieces"] as JArray;
                if (list == null) continue;

                foreach (var token in list)
                {
                    var box = ReadBox(token);
                    if (string.IsNullOrEmpty(box.Id)) throw new InvalidOperationException($"Layout {l} has a piece with no id");
                    if (!byId.TryGetValue(box.Id, out var slots)) { byId[box.Id] = slots = new Box?[count]; order.Add(box.Id); }
                    if (slots[l].HasValue) throw new InvalidOperationException($"Layout {l} names the piece '{box.Id}' twice");
                    slots[l] = box;
                    perLayout[l].Add(box);
                    highest = Mathf.Max(highest, box.Center.y + box.Size.y * 0.5f);
                }
            }

            for (int l = 0; l < count; l++) CheckMarks(l, (string)layouts[l]["name"], perLayout[l], spawns, stageTop);

            // The saved scene stands in the layout the stage shows before any match.
            int first = ArenaStage.LayoutFor(0L, 1, count);
            stage.Pieces = new ArenaStage.Piece[order.Count];
            for (int i = 0; i < order.Count; i++)
            {
                var slots = byId[order[i]];
                Box any = default;
                foreach (var slot in slots) if (slot.HasValue) { any = slot.Value; break; }

                var poses = new ArenaStage.Pose[count];
                for (int l = 0; l < count; l++)
                {
                    // An absent pose keeps a real box's numbers so it is never a zero-sized transform.
                    var box = slots[l] ?? any;
                    poses[l] = new ArenaStage.Pose { Exists = slots[l].HasValue, Position = box.Center, Size = box.Size, Euler = box.Euler };
                }

                var collider = Group(colliders, order[i]);
                collider.gameObject.AddComponent<BoxCollider>();
                var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.name = order[i];
                visual.transform.SetParent(visuals, false);
                Object.DestroyImmediate(visual.GetComponent<Collider>());
                visual.GetComponent<Renderer>().sharedMaterial = Flat(any.Kind, materials);

                foreach (var t in new[] { collider, visual.transform })
                {
                    t.SetPositionAndRotation(poses[first].Position, Quaternion.Euler(poses[first].Euler));
                    t.localScale = poses[first].Size;
                    t.gameObject.SetActive(poses[first].Exists);
                }

                stage.Pieces[i] = new ArenaStage.Piece { Id = order[i], Collider = collider, Visual = visual.transform, Poses = poses };
            }

            stage.Layouts = new ArenaStage.Layout[count];
            for (int l = 0; l < count; l++)
            {
                string name = (string)layouts[l]["name"] ?? ("layout" + l);
                var group = Group(features, $"Layout {l} {name}");
                Features(group, layouts[l]);
                group.gameObject.SetActive(l == first);
                stage.Layouts[l] = new ArenaStage.Layout { Name = name, CanHeight = Number(layouts[l]["canHeight"], 0.0f), Features = group.gameObject };
            }

            // Deep enough that the tallest piece is under the pit floor when it is gone.
            stage.PitDepth = highest - pitY + 1.0f;
            return order.Count;
        }

        /// <summary>One layout's jump pads, speed pads and stamina pickups. A jump pad is either
        /// [x, y, z] or { "center": [x, y, z], "speed": n }.</summary>
        private static void Features(Transform group, JToken layout)
        {
            int index = 0;
            foreach (var token in layout["jumpPads"] as JArray ?? new JArray())
            {
                bool detailed = token.Type == JTokenType.Object;
                var pad = Group(group, "JumpPad " + index++);
                pad.position = Vector(detailed ? token["center"] : token);
                pad.gameObject.AddComponent<JumpPad>().LaunchSpeed = detailed ? Number(token["speed"], JumpPadSpeed) : JumpPadSpeed;
            }

            index = 0;
            foreach (var token in layout["speedPads"] as JArray ?? new JArray())
            {
                var pad = Group(group, "SpeedPad " + index++);
                pad.SetPositionAndRotation(Vector(token["center"]), Quaternion.Euler(0, Number(token["yaw"], 0.0f), 0));
                var half = token["halfSize"] as JArray;
                var speed = pad.gameObject.AddComponent<ArenaSpeedPad>();
                if (half != null && half.Count >= 2) speed.HalfSize = new Vector2((float)half[0], (float)half[1]);
            }

            index = 0;
            foreach (var token in layout["pickups"] as JArray ?? new JArray())
            {
                var pickup = Group(group, "StaminaPickup " + index++);
                pickup.position = Vector(token);
                pickup.gameObject.AddComponent<ArenaStaminaPickup>();
            }
        }

        /// <summary>
        /// Refuse a layout with no floor under the can or under a spawn mark. `MatchHost.SeatOnFloor`
        /// casts from 2 m above the mark down 6 m, so the floor must be a flat piece whose plan
        /// covers the mark and whose top is within that reach.
        /// </summary>
        private static void CheckMarks(int index, string name, List<Box> boxes, JToken spawns, float stageTop)
        {
            var marks = new List<Vector3> { new Vector3(0, stageTop, 0) };
            if (spawns != null)
            {
                if (spawns["taya"] != null) marks.Add(Vector(spawns["taya"]));
                foreach (var token in spawns["attackers"] as JArray ?? new JArray()) marks.Add(Vector(token));
            }

            foreach (var mark in marks)
            {
                bool floor = false;
                foreach (var box in boxes)
                {
                    if (Mathf.Abs(Mathf.DeltaAngle(box.Euler.x, 0)) > 0.5f || Mathf.Abs(Mathf.DeltaAngle(box.Euler.z, 0)) > 0.5f) continue;

                    Vector3 local = Quaternion.Inverse(Quaternion.Euler(box.Euler)) * (mark - box.Center);
                    float top = box.Center.y + box.Size.y * 0.5f;
                    if (Mathf.Abs(local.x) <= box.Size.x * 0.5f && Mathf.Abs(local.z) <= box.Size.z * 0.5f
                        && top <= mark.y + 2.0f && top >= mark.y - 4.0f) { floor = true; break; }
                }

                if (!floor) throw new InvalidOperationException($"{Tag}Layout {index} '{name}' has no floor under the mark at {mark}. Fix {LayoutPath}.");
            }
        }

        // ------------------------------------------------------------------ what never moves

        /// <summary>The pit that shows through the gaps: a floor at pitY and four sides up to the
        /// stage top, drawn only. Nothing stands on it; a fallen body is caught above it.</summary>
        private static void Pit(Transform root, float half, float stageTop, float pitY, Dictionary<string, Material> materials)
        {
            var pit = Group(root, "Pit");
            var dark = Flat("pit", materials);
            float depth = stageTop - pitY;
            Drawn(pit, "Floor", new Vector3(0, pitY - 0.25f, 0), new Vector3(2 * half + 1, 0.5f, 2 * half + 1), Vector3.zero, dark);
            foreach (float side in new[] { -1f, 1f })
            {
                Drawn(pit, "Side X " + side, new Vector3(side * (half + 0.25f), pitY + depth * 0.5f, 0), new Vector3(0.5f, depth, 2 * half), Vector3.zero, dark);
                Drawn(pit, "Side Z " + side, new Vector3(0, pitY + depth * 0.5f, side * (half + 0.25f)), new Vector3(2 * half, depth, 0.5f), Vector3.zero, dark);
            }
        }

        /// <summary>The static boxes outside the walls (stands, screens, the booth, the rig): the
        /// same in every layout, one shared flat material per kind, no colliders.</summary>
        private static int Surround(Transform root, JArray boxes, Dictionary<string, Material> materials)
        {
            var parent = Group(root, "Surround");
            parent.gameObject.isStatic = true;
            if (boxes == null) return 0;

            foreach (var token in boxes)
            {
                var box = ReadBox(token);
                var go = Drawn(parent, string.IsNullOrEmpty(box.Id) ? box.Kind : box.Id, box.Center, box.Size, box.Euler, Flat(box.Kind, materials));
                go.isStatic = true;
            }

            return boxes.Count;
        }

        /// <summary>The match installer, the kill plane, the spawn markers and the Bounds. The
        /// walls' inner faces stand on +/- half and run from the pit floor to wallHeight above the
        /// stage, so a body in the pit is still inside them when the host catches it.</summary>
        private static void Gameplay(Transform root, JToken spawns, float half, float wallHeight, float stageTop, float pitY)
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
            float top = stageTop + wallHeight, height = top - pitY, y = (top + pitY) * 0.5f;
            float span = 2 * (half + WallThickness), centre = half + WallThickness * 0.5f;
            Wall(bounds, "WallWest", new Vector3(-centre, y, 0), new Vector3(WallThickness, height, span));
            Wall(bounds, "WallEast", new Vector3(centre, y, 0), new Vector3(WallThickness, height, span));
            Wall(bounds, "WallSouth", new Vector3(0, y, -centre), new Vector3(span, height, WallThickness));
            Wall(bounds, "WallNorth", new Vector3(0, y, centre), new Vector3(span, height, WallThickness));
        }

        private static void Lighting(Transform root)
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(root, false);
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(52, 35, 0);
            sun.color = new Color(1.0f, 0.97f, 0.92f);
            sun.intensity = 1.1f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.7f;
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.66f, 0.72f);
            RenderSettings.ambientEquatorColor = new Color(0.52f, 0.53f, 0.55f);
            RenderSettings.ambientGroundColor = new Color(0.30f, 0.30f, 0.32f);
            RenderSettings.fog = false;
        }

        // ------------------------------------------------------------------ helpers

        private static Material Flat(string kind, Dictionary<string, Material> materials)
        {
            if (string.IsNullOrEmpty(kind) || !KindColours.ContainsKey(kind)) kind = "slab";
            if (materials.TryGetValue(kind, out var known)) return known;

            if (!AssetDatabase.IsValidFolder(MaterialFolder))
            {
                Directory.CreateDirectory(MaterialFolder);
                AssetDatabase.Refresh();
            }

            string path = $"{MaterialFolder}/arena_grey_{kind}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(material, path); }
            material.color = KindColours[kind];
            material.SetFloat("_Glossiness", 0.08f);
            material.SetFloat("_Metallic", 0.0f);
            EditorUtility.SetDirty(material);
            materials[kind] = material;
            return material;
        }

        private static GameObject Drawn(Transform parent, string name, Vector3 centre, Vector3 size, Vector3 euler, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(centre, Quaternion.Euler(euler));
            go.transform.localScale = size;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private static void Wall(Transform parent, string name, Vector3 centre, Vector3 size)
        {
            var wall = Group(parent, name);
            wall.localPosition = centre;
            wall.gameObject.AddComponent<BoxCollider>().size = size;
        }

        private static Box ReadBox(JToken token) => new Box
        {
            Id = (string)token["id"],
            Kind = (string)token["kind"],
            Center = Vector(token["center"]),
            Size = Vector(token["size"]),
            Euler = token["rotation"] != null ? Vector(token["rotation"]) : Vector3.zero,
        };

        private static Vector3 Vector(JToken token)
        {
            var a = token as JArray;
            if (a == null || a.Count < 3) throw new InvalidOperationException($"{Tag}Expected [x, y, z] in {LayoutPath}, got: {token}");
            return new Vector3((float)a[0], (float)a[1], (float)a[2]);
        }

        private static float Number(JToken token, float fallback) =>
            token != null && (token.Type == JTokenType.Float || token.Type == JTokenType.Integer) ? (float)token : fallback;

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
