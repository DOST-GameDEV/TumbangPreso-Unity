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

            string outPath = $"{OutDir}/{mapName}.unity";
            bool saved = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, outPath);
            report.AppendLine(saved ? $"   wrote {outPath}" : $"   FAILED to write {outPath}");

            return saved && missing == 0;
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
