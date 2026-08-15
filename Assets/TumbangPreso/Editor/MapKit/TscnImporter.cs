using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// Converts a Godot `.tscn` map into a Unity scene, node for node.
    ///
    /// ⚠️⚠️ THIS CONVERTS THE OUTPUT, NOT THE BUILDER, AND THAT IS THE WHOLE POINT. The maps
    /// are produced by roughly 190 KB of Python (`build_eskinita.py`, `build_bayan_plaza.py`,
    /// `mapkit.py`). Reimplementing that in C# would be weeks of work and would produce an
    /// arena that is *similar* to the one the game was balanced on. The `.tscn` those builders
    /// emit is plain text listing every node with an explicit transform, so converting it
    /// gives a genuine one-to-one copy of the arena that actually shipped: same pieces, same
    /// positions, same walls.
    ///
    /// ⚠️ THE HANDEDNESS FLIP IS THE ONLY THING THAT CAN SILENTLY RUIN THIS. Godot is
    /// right-handed with -Z forward; Unity is left-handed with +Z forward. Get it wrong and the
    /// map looks plausible and is MIRRORED, which nobody notices until a player who knows the
    /// arena says the sari-sari store is on the wrong side. The conversion is a mirror on Z:
    /// positions negate Z, and the basis is rebuilt so forward stays forward.
    ///
    /// ⚠️ AND THE WALLS ARE LOAD-BEARING, NOT DECORATION. `Bounds/WallEast` and `WallWest` sit
    /// at x = +/-8.6 and that number is a hard bound on the arena: the AI standoff ring has to
    /// fit inside it, and when it did not, bots jammed against the wall and most of the
    /// offence in the match disappeared. This importer reports the wall positions it found so
    /// ArenaCheck can be run against the real geometry rather than against a default.
    ///
    /// Run:
    ///   Unity.exe -batchmode -quit -nographics -projectPath . \
    ///             -executeMethod TumbangPreso.EditorTools.MapKit.TscnImporter.ImportAll
    /// </summary>
    public static class TscnImporter
    {
        private const string SourceDir = "MapSource";
        private const string OutDir = "Assets/TumbangPreso/Scenes/Maps";
        private const string ArtRoot = "Assets/TumbangPreso/Art";
        private const string ResultPath = "Logs/map-import.txt";

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        [MenuItem("Tumbang Preso/Import Godot Maps")]
        public static void ImportAllFromMenu() => Execute();

        public static void ImportAll() => EditorApplication.Exit(Execute() ? 0 : 1);

        private static bool Execute()
        {
            var report = new StringBuilder();
            bool ok = true;

            report.AppendLine("MAP IMPORT (Godot .tscn -> Unity scene)");
            report.AppendLine();

            if (!Directory.Exists(SourceDir))
            {
                report.AppendLine($"FAIL: no {SourceDir}/ directory. Copy the .tscn files there.");
                Write(report);
                return false;
            }

            Directory.CreateDirectory(OutDir);

            foreach (var path in Directory.GetFiles(SourceDir, "*.tscn"))
            {
                try
                {
                    ok &= ImportOne(path, report);
                }
                catch (Exception e)
                {
                    report.AppendLine($"FAIL {Path.GetFileName(path)}: {e.Message}");
                    report.AppendLine(e.StackTrace);
                    ok = false;
                }
                report.AppendLine();
            }

            report.AppendLine(ok ? "RESULT: OK." : "RESULT: FAILED.");
            Write(report);

            AssetDatabase.Refresh();
            return ok;
        }

        private static void Write(StringBuilder sb)
        {
            try
            {
                Directory.CreateDirectory("Logs");
                File.WriteAllText(ResultPath, sb.ToString());
            }
            catch { }

            Debug.Log(sb.ToString());
        }

        // -------------------------------------------------------------------
        // PARSING
        // -------------------------------------------------------------------

        private sealed class ExtRes
        {
            public string Id;
            public string GodotPath;
            public string Type;
        }

        private sealed class SubRes
        {
            public string Id;
            public string Type;
            public readonly Dictionary<string, string> Props = new Dictionary<string, string>();
        }

        private sealed class NodeDef
        {
            public string Name;
            public string Type;
            public string Parent;
            public string InstanceExtId;
            public readonly Dictionary<string, string> Props = new Dictionary<string, string>();
        }

        private static readonly Regex ExtRe = new Regex(
            @"^\[ext_resource\s+type=""(?<type>[^""]+)""\s+(?:uid=""[^""]*""\s+)?path=""(?<path>[^""]+)""\s+id=""(?<id>[^""]+)""\]");

        private static readonly Regex SubRe = new Regex(
            @"^\[sub_resource\s+type=""(?<type>[^""]+)""\s+id=""(?<id>[^""]+)""\]");

        private static readonly Regex NodeRe = new Regex(
            @"^\[node\s+name=""(?<name>[^""]*)""(?:\s+type=""(?<type>[^""]*)"")?(?:\s+parent=""(?<parent>[^""]*)"")?(?:\s+instance=ExtResource\(""(?<inst>[^""]+)""\))?");

        private static readonly Regex PropRe = new Regex(@"^(?<key>[A-Za-z_][A-Za-z0-9_/]*)\s*=\s*(?<val>.+)$");

        private static bool ImportOne(string tscnPath, StringBuilder report)
        {
            string mapName = Path.GetFileNameWithoutExtension(tscnPath);
            report.AppendLine($"-- {mapName} --");

            var ext = new Dictionary<string, ExtRes>();
            var subs = new Dictionary<string, SubRes>();
            var nodes = new List<NodeDef>();

            NodeDef currentNode = null;
            SubRes currentSub = null;

            foreach (var raw in File.ReadAllLines(tscnPath))
            {
                string line = raw.Trim();
                if (line.Length == 0) continue;

                var m = ExtRe.Match(line);
                if (m.Success)
                {
                    currentNode = null; currentSub = null;
                    ext[m.Groups["id"].Value] = new ExtRes
                    {
                        Id = m.Groups["id"].Value,
                        GodotPath = m.Groups["path"].Value,
                        Type = m.Groups["type"].Value,
                    };
                    continue;
                }

                m = SubRe.Match(line);
                if (m.Success)
                {
                    currentNode = null;
                    currentSub = new SubRes { Id = m.Groups["id"].Value, Type = m.Groups["type"].Value };
                    subs[currentSub.Id] = currentSub;
                    continue;
                }

                m = NodeRe.Match(line);
                if (m.Success)
                {
                    currentSub = null;
                    currentNode = new NodeDef
                    {
                        Name = m.Groups["name"].Value,
                        Type = m.Groups["type"].Success ? m.Groups["type"].Value : "",
                        Parent = m.Groups["parent"].Success ? m.Groups["parent"].Value : null,
                        InstanceExtId = m.Groups["inst"].Success ? m.Groups["inst"].Value : null,
                    };
                    nodes.Add(currentNode);
                    continue;
                }

                if (line.StartsWith("[")) { currentNode = null; currentSub = null; continue; }

                var p = PropRe.Match(line);
                if (!p.Success) continue;

                if (currentNode != null) currentNode.Props[p.Groups["key"].Value] = p.Groups["val"].Value.Trim();
                else if (currentSub != null) currentSub.Props[p.Groups["key"].Value] = p.Groups["val"].Value.Trim();
            }

            report.AppendLine($"   parsed: {ext.Count} ext, {subs.Count} sub, {nodes.Count} nodes");

            return Build(mapName, ext, subs, nodes, report);
        }

        // -------------------------------------------------------------------
        // BUILDING
        // -------------------------------------------------------------------

        private static bool Build(string mapName, Dictionary<string, ExtRes> ext,
                                  Dictionary<string, SubRes> subs, List<NodeDef> nodes,
                                  StringBuilder report)
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);

            // Godot paths ("." and "Bounds/WallEast") to the Unity transform we made for them.
            var byPath = new Dictionary<string, Transform>();
            int instanced = 0, missing = 0, primitives = 0;
            var missingPaths = new HashSet<string>();

            foreach (var n in nodes)
            {
                GameObject go = null;

                // An instanced model: this is the bulk of a map.
                if (n.InstanceExtId != null && ext.TryGetValue(n.InstanceExtId, out var res))
                {
                    string assetPath = ToUnityAssetPath(res.GodotPath);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                    if (prefab != null)
                    {
                        go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                        instanced++;
                    }
                    else
                    {
                        // ⚠️ A MISSING MODEL BECOMES A VISIBLE MARKER, NOT A SILENT GAP. An
                        // absent building leaves a hole in a wall that reads as a level design
                        // choice; a magenta box reads as a missing asset, which is what it is.
                        go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        go.transform.localScale = Vector3.one * 0.75f;
                        missing++;
                        missingPaths.Add(res.GodotPath);
                    }
                }

                // A box mesh authored inline (the road slab, the chalk lines).
                if (go == null && n.Props.TryGetValue("mesh", out var meshRef))
                {
                    string id = SubId(meshRef);
                    if (id != null && subs.TryGetValue(id, out var sub) && sub.Type == "BoxMesh")
                    {
                        go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
                        go.transform.localScale = ParseVec3(sub.Props.TryGetValue("size", out var s) ? s : "Vector3(1, 1, 1)");
                        primitives++;
                    }
                }

                // A collision shape. ⚠️ THE WALLS AND THE FLOOR COME THROUGH HERE and they are
                // gameplay, not scenery: the walls bound the arena and the floor is what every
                // body stands on.
                if (go == null && n.Props.TryGetValue("shape", out var shapeRef))
                {
                    string id = SubId(shapeRef);
                    if (id != null && subs.TryGetValue(id, out var sub) && sub.Type == "BoxShape3D")
                    {
                        go = new GameObject(n.Name);
                        var box = go.AddComponent<BoxCollider>();
                        box.size = ParseVec3(sub.Props.TryGetValue("size", out var s) ? s : "Vector3(1, 1, 1)");
                        primitives++;
                    }
                }

                // ⚠️⚠️ THE LIGHT AND THE ATMOSPHERE ARE THE MAP'S LOOK AND THEY WERE DROPPED
                // ENTIRELY. Both arenas converted with ZERO lights and Unity's default skybox,
                // so the whole game rendered under flat grey ambient while the Godot build sits
                // in a warm late afternoon: an amber key light at 1.15, warm ambient at 1.65,
                // and a haze that starts at 14 m. That is not a finishing touch, it is the hour
                // the game is set at, chosen deliberately, and its absence is most of why the
                // port "doesn't look like the game".
                if (go == null && n.Type == "DirectionalLight3D")
                {
                    go = new GameObject(n.Name);
                    ApplyDirectionalLight(go.AddComponent<Light>(), n);
                    report.AppendLine("   converted the key light");
                }

                if (go == null && n.Type == "WorldEnvironment")
                {
                    go = new GameObject(n.Name);
                    ApplyEnvironment(n, subs, ext, report);
                }

                if (go == null) go = new GameObject(n.Name);
                go.name = n.Name;

                // Parent before writing the local transform, or the transform is applied in
                // the wrong space and every child drifts.
                Transform parent = null;
                if (n.Parent != null && n.Parent != "." && byPath.TryGetValue(n.Parent, out var pt)) parent = pt;
                else if (n.Parent == "." && byPath.TryGetValue(".", out var root)) parent = root;

                go.transform.SetParent(parent, worldPositionStays: false);

                if (n.Props.TryGetValue("transform", out var t)) ApplyGodotTransform(go.transform, t);
                else { go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity; }

                string myPath = n.Parent == null ? "." :
                                (n.Parent == "." ? n.Name : n.Parent + "/" + n.Name);
                byPath[myPath] = go.transform;
            }

            report.AppendLine($"   built: {instanced} models, {primitives} primitives, {missing} MISSING");
            foreach (var p in missingPaths) report.AppendLine($"      missing model: {p}");

            ReportWalls(byPath, report);

            // ⚠️ THE GAMEPLAY IS INSTALLED BY A COMPONENT, NOT AUTHORED INTO THE MAP. The map
            // is regenerated from the Godot source whenever the builders change, so anything
            // placed here by hand would be lost on the next import. One component means both
            // arenas are wired identically by construction rather than by remembering to do
            // the second one.
            var installer = new GameObject("~Match");
            installer.AddComponent<MatchInstaller>();
            report.AppendLine("   installed the match rig");

            // ⚠️⚠️ THE COLOUR PASS WAS BUILT AND NEVER CALLED. `EnvColourPass` is a full port of
            // `env_toon_pass.gd` — seeded facade paint, foliage variation, the road's
            // warm-neutral correction and the sampay wind — and NOTHING in the project ever
            // attached it. Both arenas therefore rendered in the kit's own factory colours: a
            // white-and-green suburb where the game is set on a painted Manila street. That is
            // the single largest visual difference between the two builds and it is invisible
            // in any check that is not a screenshot.
            //
            // ⚠️ ON THE MAP ROOT, because it walks its own children. Attaching it to the match
            // rig would find nothing at all and report success.
            if (byPath.TryGetValue(".", out var mapRoot))
            {
                mapRoot.gameObject.AddComponent<Visual.EnvColourPass>();
                report.AppendLine("   attached the environment colour pass");
            }
            else
            {
                report.AppendLine("   WARNING: no map root, so the street keeps the kit colours");
            }

            string outPath = $"{OutDir}/{mapName}.unity";
            bool saved = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, outPath);
            report.AppendLine(saved ? $"   wrote {outPath}" : $"   FAILED to write {outPath}");

            return saved && missing == 0;
        }

        /// <summary>
        /// The map's key light, from `DirectionalLight3D`.
        ///
        /// ⚠️ LATE AFTERNOON, CHOSEN DELIBERATELY. The Godot scene's own comment calls it "the
        /// hour children actually play": an amber key at 1.15 energy with soft, wide shadows.
        /// A white light at 1.0 renders the same geometry as a completely different game.
        ///
        /// ⚠️ AND THE SHADOW SETTINGS COME ACROSS TOO. `shadow_opacity = 0.62` is what stops
        /// the shadows reading as black holes in the road, and the bias values were tuned
        /// against this geometry: a default bias on a low-poly street acnes every flat surface.
        /// </summary>
        private static void ApplyDirectionalLight(Light light, NodeDef n)
        {
            light.type = LightType.Directional;
            light.color = ParseColor(n.Props.TryGetValue("light_color", out var c) ? c : null,
                                     Color.white);

            light.intensity = Prop(n, "light_energy", 1.0f);
            light.bounceIntensity = Prop(n, "light_indirect_energy", 1.0f);

            bool shadows = !n.Props.TryGetValue("shadow_enabled", out var s) || s.Trim() == "true";
            light.shadows = shadows ? LightShadows.Soft : LightShadows.None;

            // Godot's bias is in world units against a normalised depth; Unity's is a fraction.
            // These are the shipped values scaled to Unity's ranges rather than re-tuned.
            light.shadowBias = Mathf.Clamp01(Prop(n, "shadow_bias", 0.05f));
            light.shadowNormalBias = Mathf.Clamp(Prop(n, "shadow_normal_bias", 1.0f) * 0.15f,
                                                 0.0f, 3.0f);

            light.shadowStrength = Mathf.Clamp01(Prop(n, "shadow_opacity", 1.0f));
            light.shadowNearPlane = 0.2f;
            light.renderMode = LightRenderMode.ForcePixel;
        }

        /// <summary>
        /// The map's `Environment`: ambient, fog, exposure and the sky.
        ///
        /// ⚠️ GODOT'S AMBIENT ENERGY IS NOT UNITY'S. Godot multiplies its ambient colour by
        /// `ambient_light_energy` (1.65 here); Unity's flat ambient takes a single colour and
        /// an intensity that only applies in the skybox mode. The colour is therefore
        /// pre-multiplied so the two engines put the same amount of light on a wall.
        ///
        /// ⚠️ AND THE FOG IS LINEAR WITH A NEAR START. `fog_depth_begin = 14` is close, and
        /// that is the point: it separates the near street from the far one and is a large part
        /// of the depth the arena reads with. Unity's exponential default at the same colour
        /// looks like weather instead.
        /// </summary>
        private static void ApplyEnvironment(NodeDef n, Dictionary<string, SubRes> subs,
                                             Dictionary<string, ExtRes> ext, StringBuilder report)
        {
            if (!n.Props.TryGetValue("environment", out var raw)) return;

            string id = SubId(raw);
            if (id == null || !subs.TryGetValue(id, out var env)) return;

            var ambient = ParseColor(env.Props.TryGetValue("ambient_light_color", out var a)
                                     ? a : null, new Color(0.62f, 0.58f, 0.52f));

            float energy = SubProp(env, "ambient_light_energy", 1.0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = ambient * energy;
            RenderSettings.ambientIntensity = 1.0f;

            bool fog = env.Props.TryGetValue("fog_enabled", out var f) && f.Trim() == "true";

            RenderSettings.fog = fog;

            if (fog)
            {
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogColor = ParseColor(
                    env.Props.TryGetValue("fog_light_color", out var fc) ? fc : null,
                    new Color(0.92f, 0.78f, 0.60f));

                RenderSettings.fogStartDistance = SubProp(env, "fog_depth_begin", 14.0f);
                RenderSettings.fogEndDistance = SubProp(env, "fog_depth_end", 58.0f);
            }

            ApplySky(env, subs, ext, report);

            report.AppendLine($"   environment: ambient {ColorUtility.ToHtmlStringRGB(ambient)}" +
                              $" x{energy:0.00}, fog {(fog ? "on" : "off")}");
        }

        /// <summary>
        /// ⚠️ THE SKY IS A PANORAMA THE MAP SHIPS, not Unity's procedural default. The default
        /// is a clean blue gradient with a white sun; this one is the painted afternoon sky the
        /// backdrop art was drawn against, and the two disagree about what time it is.
        /// </summary>
        private static void ApplySky(SubRes env, Dictionary<string, SubRes> subs,
                                     Dictionary<string, ExtRes> ext, StringBuilder report)
        {
            string panoramaPath = null;

            foreach (var sub in subs.Values)
            {
                if (sub.Type != "PanoramaSkyMaterial") continue;
                if (!sub.Props.TryGetValue("panorama", out var pano)) continue;

                string extId = ExtId(pano);
                if (extId != null && ext.TryGetValue(extId, out var res))
                    panoramaPath = ToUnityAssetPath(res.GodotPath);
            }

            if (panoramaPath == null) return;

            var texture = AssetDatabase.LoadAssetAtPath<Texture>(panoramaPath);
            if (texture == null)
            {
                report.AppendLine($"   MISSING sky panorama: {panoramaPath}");
                return;
            }

            const string matPath = "Assets/TumbangPreso/Art/models/materials/Sky.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);

            if (material == null)
            {
                material = new Material(Shader.Find("Skybox/Panoramic"));
                AssetDatabase.CreateAsset(material, matPath);
            }

            material.SetTexture("_MainTex", texture);
            material.SetFloat("_Mapping", 1.0f);          // latitude-longitude, as Godot stores it
            material.SetFloat("_ImageType", 0.0f);        // 360 degrees
            material.SetFloat("_Exposure", 1.0f);

            EditorUtility.SetDirty(material);

            RenderSettings.skybox = material;
            report.AppendLine("   sky: panorama material bound");
        }

        private static float SubProp(SubRes res, string key, float fallback) =>
            res.Props.TryGetValue(key, out var s) ? F(s) : fallback;

        private static float Prop(NodeDef n, string key, float fallback) =>
            n.Props.TryGetValue(key, out var s) ? F(s) : fallback;

        private static Color ParseColor(string value, Color fallback)
        {
            if (string.IsNullOrEmpty(value)) return fallback;

            var m = Regex.Match(value, @"Color\(([^)]*)\)");
            if (!m.Success) return fallback;

            var parts = m.Groups[1].Value.Split(',');
            if (parts.Length < 3) return fallback;

            return new Color(F(parts[0]), F(parts[1]), F(parts[2]),
                             parts.Length > 3 ? F(parts[3]) : 1.0f);
        }

        /// <summary>
        /// ⚠️ THE WALL FACES ARE A HARD BOUND ON THE ARENA and they are reported so
        /// ArenaCheck can be run against the real geometry. Violating the standoff bound does
        /// not look like a bounds bug: bots pin against a wall reaching for a goal inside it,
        /// and it gets reported as broken pathfinding while most of the offence quietly
        /// disappears.
        /// </summary>
        private static void ReportWalls(Dictionary<string, Transform> byPath, StringBuilder report)
        {
            foreach (var key in new[] { "Bounds/WallEast", "Bounds/WallWest",
                                        "Bounds/WallNorth", "Bounds/WallSouth" })
            {
                if (!byPath.TryGetValue(key, out var t)) continue;
                report.AppendLine($"   {key}: x={t.position.x:F2} z={t.position.z:F2}");
            }
        }

        // -------------------------------------------------------------------
        // CONVERSION
        // -------------------------------------------------------------------

        private static string ToUnityAssetPath(string godotPath)
        {
            // res://assets/models/kits/town/road.glb -> Assets/TumbangPreso/Art/models/...
            string rel = godotPath.Replace("res://", "");
            if (rel.StartsWith("assets/")) rel = rel.Substring("assets/".Length);
            return $"{ArtRoot}/{rel}";
        }

        private static string SubId(string value)
        {
            var m = Regex.Match(value, @"SubResource\(""([^""]+)""\)");
            return m.Success ? m.Groups[1].Value : null;
        }

        private static string ExtId(string value)
        {
            var m = Regex.Match(value ?? "", @"ExtResource\(""([^""]+)""\)");
            return m.Success ? m.Groups[1].Value : null;
        }

        private static Vector3 ParseVec3(string value)
        {
            var m = Regex.Match(value, @"Vector3\(([^)]*)\)");
            if (!m.Success) return Vector3.one;

            var parts = m.Groups[1].Value.Split(',');
            if (parts.Length < 3) return Vector3.one;

            return new Vector3(F(parts[0]), F(parts[1]), F(parts[2]));
        }

        private static float F(string s) =>
            float.TryParse(s.Trim(), NumberStyles.Float, Inv, out var v) ? v : 0.0f;

        /// <summary>
        /// Godot `Transform3D(xx,xy,xz, yx,yy,yz, zx,zy,zz, ox,oy,oz)` to a Unity local
        /// transform.
        ///
        /// ⚠️⚠️ THE MIRROR IS THE WHOLE CONVERSION AND IT IS EASY TO GET SUBTLY WRONG. Godot is
        /// right-handed with -Z forward; Unity is left-handed with +Z forward. Mirroring on Z
        /// means positions negate Z, the right and up columns negate their z component, and
        /// the FORWARD column is rebuilt from Godot's -Z basis column rather than its +Z. Skip
        /// that last part and the map builds mirrored: entirely plausible, and wrong in a way
        /// only somebody who knows the arena will catch.
        /// </summary>
        private static void ApplyGodotTransform(Transform t, string value)
        {
            var m = Regex.Match(value, @"Transform3D\(([^)]*)\)");
            if (!m.Success) return;

            var p = m.Groups[1].Value.Split(',');
            if (p.Length < 12) return;

            var gx = new Vector3(F(p[0]), F(p[1]), F(p[2]));   // Godot basis X column
            var gy = new Vector3(F(p[3]), F(p[4]), F(p[5]));   // Godot basis Y column
            var gz = new Vector3(F(p[6]), F(p[7]), F(p[8]));   // Godot basis Z column
            var origin = new Vector3(F(p[9]), F(p[10]), F(p[11]));

            float sx = gx.magnitude, sy = gy.magnitude, sz = gz.magnitude;
            if (sx > 0.0001f) gx /= sx;
            if (sy > 0.0001f) gy /= sy;
            if (sz > 0.0001f) gz /= sz;

            // Mirror on Z, and take forward from Godot's -Z.
            var uRight = new Vector3(gx.x, gx.y, -gx.z);
            var uUp = new Vector3(gy.x, gy.y, -gy.z);
            var uForward = new Vector3(-gz.x, -gz.y, gz.z);

            t.localPosition = new Vector3(origin.x, origin.y, -origin.z);

            t.localRotation = (uForward.sqrMagnitude > 0.0001f && uUp.sqrMagnitude > 0.0001f)
                ? Quaternion.LookRotation(uForward, uUp)
                : Quaternion.identity;

            // Preserve any non-uniform scale the builder baked into the basis.
            t.localScale = new Vector3(
                Mathf.Approximately(sx, 0.0f) ? 1.0f : sx,
                Mathf.Approximately(sy, 0.0f) ? 1.0f : sy,
                Mathf.Approximately(sz, 0.0f) ? 1.0f : sz);

            // Unused, but kept so the intent of uRight is explicit rather than implied.
            _ = uRight;
        }
    }
}
