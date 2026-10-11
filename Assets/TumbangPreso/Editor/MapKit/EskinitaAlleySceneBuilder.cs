using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// SAMPLE MAP: Eskinita Alley, a tight, stepped Manila back alley. A from-scratch rebuild of
    /// the shipped Eskinita, modelled in Blender and assembled here from a layout file, the way
    /// <see cref="KantoSceneBuilder"/> assembles Kanto. Nothing here makes geometry except the
    /// gameplay markers.
    ///
    /// ⚠️⚠️ NOT REGISTERED ANYWHERE, ON PURPOSE. It is in no map list (`SceneFlow.Maps`,
    /// `GameLaunch.Maps`), not in the build settings and not in `MapGeometryCheck`, so the
    /// shipped Eskinita, its scene and its tests are untouched. It is opened and played from the
    /// editor. The one runtime trace is its own row in `WorldLookProfile` ("EskinitaAlley"),
    /// which Play finds by the scene's name.
    ///
    /// WHAT IT READS, all under Assets/TumbangPreso/Art/EskinitaAlley/:
    ///  * `eskinita_alley_layout.json`: gameplay measures, materials, placements, bounce pads,
    ///    spawn marks and review shots. Every coordinate is already in UNITY axes, metres.
    ///    Optional extras this builder understands: `"sun":[yawDegrees]` turns the key light
    ///    (see <see cref="DefaultSunYaw"/>), and a pad may carry `model`, `paint`, `prefix`,
    ///    `model_half` and `round` to wear a pad of its own (see `Pads`).
    ///  * every `materials*.json` beside it, MERGED with the layout's own list (the layout is
    ///    read last and wins; a name defined twice is reported).
    ///  * `Models/NAME.glb` for each placement, `Models/collision.glb` for ALL of the map's
    ///    collision, `Textures/NAME_albedo.png` (+ `_normal.png`) for the materials.
    ///
    /// ⚠️ MATERIALS ARE BUILT HERE, NOT IMPORTED (Kanto's rule). The .glb files carry material
    /// NAMES only; each name becomes one shared asset under `Materials/` and every imported
    /// renderer is re-pointed by name. Opaque surfaces wear `TumbangPreso/NearFade`: Standard
    /// lighting with albedo x tint and tiling, the near-camera dissolve, and THE SKY WINDOW (a
    /// cutscene opens the sky through anything on that shader, which is why walls and roofs must
    /// be on it). Leaves and other cut-outs wear `TumbangPreso/KantoFoliage`. An EMISSIVE
    /// material wears `Standard`, because NearFade has no emission term: it glows and it does not
    /// open for the sky window, so keep "emissive" to lamps and signs, never a wall or a roof.
    ///
    /// ⚠️ COLLISION IS ONE FILE. Every mesh in `collision.glb` becomes a non-convex MeshCollider
    /// with no renderer, under "Collision" on the Default layer (what Kanto's ground is on, and
    /// what `MatchHost.SeatOnFloor` and the cameras cast against: all layers). Placed models get
    /// no colliders at all.
    ///
    /// Menu: Tumbang Preso/Sample Map. From a shell with the editor open: write `build`,
    /// `review` or `build review` into `Temp/eskinita-alley.request` (see
    /// <see cref="EskinitaAlleyRequestWatcher"/>).
    /// </summary>
    public static partial class EskinitaAlleySceneBuilder
    {
        // ⚠️ THIS IS ESKINITA NOW (owner, 2026-10-09: "push this to the map pool, replace the old eskinita"). The scene is
        // written OVER the shipped `Eskinita.unity` (same file, same GUID, same place in the map list and the build
        // settings, same id on the wire), under the shipped name, so nothing that names the map had to change. The old
        // scene is kept as `Scenes/Vault/EskinitaClassic_2026-10-09.unity` and in git. The art folder keeps its name.
        public const string MapName = "Eskinita";
        public const string ScenePath = "Assets/TumbangPreso/Scenes/Maps/Eskinita.unity";
        private const string Root = "Assets/TumbangPreso/Art/EskinitaAlley";
        private const string LayoutPath = Root + "/eskinita_alley_layout.json";
        private const string Tag = "[EskinitaAlley] ";

        /// <summary>
        /// The key light's turn about the vertical when the layout gives none, degrees (the Y of
        /// the Sun's Euler rotation; the light travels toward +Z at 0 and toward +X at 90). The
        /// alley runs along Z, entered by the gate at +Z and climbing toward -Z, so 208 puts the
        /// sun low behind the right shoulder of somebody walking in: it rakes UP the alley and a
        /// little across it, lights the painted stair risers and every face seen on the way up,
        /// lights the walls on the -X side, and lays the +X side's shadows across the floor.
        /// 152 lights the other side instead; 28 or -28 looks into the sun from the gate.
        /// </summary>
        public const float DefaultSunYaw = 208f;

        [Serializable] private class GameplaySpec { public float half_x, half_z; public float[] can; public float box_y, wall_height; }
        [Serializable] private class MatSpec { public string name, texture; public float[] tint; public float tiling; public bool foliage, emissive, glossy, cutout; }
        [Serializable] private class MatFile { public MatSpec[] materials; }
        [Serializable] private class Placement { public string model; public float[] position; public float yaw; public float scale; }
        [Serializable] private class PadSpec { public float[] position; public float radius, speed; public string model, paint, prefix; public float model_half; public bool round; }
        [Serializable] private class ShotSpec { public string name; public float[] at, look; public float fov; }
        [Serializable] private class Layout { public GameplaySpec gameplay; public MatSpec[] materials; public Placement[] placements; public PadSpec[] pads; public ShotSpec[] review; public float[] sun; public RouteSpec routes; public SolidSpec solids; }
        [Serializable] private class SolidBox { public float[] c, s; }
        [Serializable] private class SolidWedge { public float[] p; }
        [Serializable] private class SolidSpec { public SolidBox[] boxes; public SolidWedge[] wedges; }
        [Serializable] private class RouteNode { public float[] p; }
        [Serializable] private class RoutePad { public int a, b; }
        [Serializable] private class RouteSpec { public RouteNode[] nodes; public RoutePad[] pads; }

        /// <summary>What went wrong without stopping the build: each line once. The request
        /// watcher writes them into its answer, so the art side reads them without the Console.</summary>
        private static readonly List<string> Warnings = new List<string>();

        private static void Warn(string message)
        {
            if (Warnings.Contains(message)) return;
            Warnings.Add(message);
            Debug.LogWarning(Tag + message);
        }

        [MenuItem("Tumbang Preso/Sample Map/Build Eskinita Alley")]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [MenuItem("Tumbang Preso/Sample Map/Render Eskinita Alley Review")]
        public static void ReviewFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Review(NextReviewFolder());
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static Layout ReadLayout(out string json)
        {
            if (!File.Exists(LayoutPath)) throw new FileNotFoundException("Missing layout " + LayoutPath);
            json = File.ReadAllText(LayoutPath);
            var layout = JsonUtility.FromJson<Layout>(json);
            if (layout == null) throw new InvalidOperationException("Unreadable layout " + LayoutPath);
            if (layout.gameplay == null) layout.gameplay = new GameplaySpec();
            var g = layout.gameplay;
            if (g.half_x <= 0f) { g.half_x = 13.6f; Warn("layout gameplay.half_x missing, using 13.6"); }
            if (g.half_z <= 0f) { g.half_z = 17.5f; Warn("layout gameplay.half_z missing, using 17.5"); }
            if (g.wall_height <= 0f) g.wall_height = 16f;
            if (g.can == null || g.can.Length < 3) { g.can = new[] { 0f, g.box_y, 0f }; Warn("layout gameplay.can missing, using (0, box_y, 0)"); }
            return layout;
        }

        /// <summary>Builds and saves the scene. Returns one line saying what was built.</summary>
        public static string Build()
        {
            Warnings.Clear();
            var layout = ReadLayout(out string json);
            var g = layout.gameplay;
            var materials = BuildMaterials(MergeMaterials(layout.materials));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject(MapName).transform;
            root.gameObject.AddComponent<MapGrade>();
            var dressing = Group(root, "Dressing");
            var groups = new Dictionary<string, Transform>();
            int placed = 0, missing = 0;
            foreach (var p in layout.placements ?? new Placement[0])
            {
                if (string.IsNullOrEmpty(p.model) || p.position == null || p.position.Length < 3) { Warn("a placement has no model or no position"); continue; }
                if (p.model == "collision") { Warn("'collision' is listed as a placement; it is the collision file and is not drawn"); continue; }
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Models/{p.model}.glb");
                if (prefab == null) { Warn("missing model Models/" + p.model + ".glb"); missing++; continue; }
                if (!groups.TryGetValue(p.model, out var group)) groups[p.model] = group = Group(dressing, p.model);
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group);
                go.transform.SetPositionAndRotation(new Vector3(p.position[0], p.position[1], p.position[2]), Quaternion.Euler(0, p.yaw, 0));
                // 0 is a layout that did not say: as modelled.
                if (p.scale > 0f) go.transform.localScale = Vector3.one * p.scale;
                Rematerial(go, materials);
                // No colliders on anything placed: collision.glb is the whole of the map's collision.
                foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
                foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.isStatic = true;
                placed++;
            }
            foreach (var t in dressing.GetComponentsInChildren<Transform>(true)) t.gameObject.isStatic = true;

            var colliders = Collision(root, out float lowest);
            string solids = Solids(root, layout.solids);
            int spawns = Gameplay(root, g, ParseSpawns(json), colliders, lowest);
            int pads = Pads(root, layout.pads);
            string routes = Routes(root, layout.routes);
            Lighting(root, layout.sun);
            // ---- LIVE CHICKENS (EskinitaAlleyLifeAuthor.cs, runtime AlleyChickens): visual only, no colliders,
            // not networked. After the dressing, collision and pads, which it measures its walkable floor against.
            string life = EskinitaAlleyLifeAuthor.Build(root, materials, colliders, new Vector3(g.can[0], g.can[1], g.can[2]), Warn);
            // ---- STREET LIFE on the road outside the arch (EskinitaAlleyStreetAuthor.cs, runtime AlleyStreet + SidewalkLife):
            // visual only, no colliders, nothing inside the play bounds. Its own line goes to Logs/eskinita/street_build.txt.
            EskinitaAlleyStreetAuthor.Build(root, colliders, Warn);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Could not save " + ScenePath);
            AssetDatabase.SaveAssets();
            string summary = $"built {ScenePath}: {placed} pieces placed, {missing} missing, {materials.Count} materials, " +
                             $"{colliders.Count} collision meshes, {solids}, {pads} pads, {spawns} spawn marks; {routes}; {life}";
            Debug.Log(Tag + summary);
            return summary;
        }

        // ------------------------------------------------------------------ routes for the bots

        /// <summary>
        /// The layout's way points become a `MapRoutes` on the scene, BAKED here against the collision just built (which
        /// pairs can be walked straight, and the shortest way between every two). Node 0 is the can's floor by the
        /// layout's convention: a node with no way to it, or none from it, is reported.
        /// </summary>
        private static string Routes(Transform root, RouteSpec spec)
        {
            if (spec == null || spec.nodes == null || spec.nodes.Length == 0) return "no routes";
            var go = new GameObject("Routes");
            go.transform.SetParent(root, false);
            var routes = go.AddComponent<MapRoutes>();
            routes.Nodes = spec.nodes.Select(n => new Vector3(n.p[0], n.p[1], n.p[2])).ToArray();
            routes.PadFrom = (spec.pads ?? new RoutePad[0]).Select(p => p.a).ToArray();
            routes.PadTo = (spec.pads ?? new RoutePad[0]).Select(p => p.b).ToArray();
            int links = routes.Bake();
            var noWayIn = new List<int>(); var noWayOut = new List<int>();
            routes.CountWithoutWayFrom(0, noWayIn); routes.CountWithoutWayTo(0, noWayOut);
            string At(int i) => $"{i} ({routes.Nodes[i].x:0.0}, {routes.Nodes[i].y:0.0}, {routes.Nodes[i].z:0.0})";
            if (noWayIn.Count > 0) Warn("routes: no way FROM the can to node(s) " + string.Join("; ", noWayIn.Select(At)));
            if (noWayOut.Count > 0) Warn("routes: no way TO the can from node(s) " + string.Join("; ", noWayOut.Select(At)));
            EditorUtility.SetDirty(routes);
            return $"routes: {routes.Nodes.Length} way points, {links} direct links, {noWayIn.Count} unreachable from the can, {noWayOut.Count} with no way back";
        }

        // ------------------------------------------------------------------ materials

        /// <summary>
        /// Every `materials*.json` in the map's folder, in name order, then the layout's own list.
        /// Later wins. A name that two sources define differently is reported; the same spec twice
        /// is not a clash.
        /// </summary>
        private static List<MatSpec> MergeMaterials(MatSpec[] fromLayout)
        {
            var merged = new Dictionary<string, MatSpec>();
            var order = new List<string>();
            var from = new Dictionary<string, string>();
            void Take(MatSpec[] specs, string source)
            {
                if (specs == null) return;
                foreach (var s in specs)
                {
                    if (s == null || string.IsNullOrEmpty(s.name)) { Warn("a material in " + source + " has no name"); continue; }
                    if (merged.TryGetValue(s.name, out var earlier))
                    {
                        if (JsonUtility.ToJson(earlier) != JsonUtility.ToJson(s))
                            Warn($"material '{s.name}' is defined in {from[s.name]} and again, differently, in {source}; {source} wins");
                    }
                    else order.Add(s.name);
                    merged[s.name] = s; from[s.name] = source;
                }
            }
            var files = Directory.GetFiles(Root, "materials*.json").Select(f => f.Replace('\\', '/')).OrderBy(f => f, StringComparer.Ordinal);
            foreach (string file in files)
            {
                MatFile parsed = null;
                try { parsed = JsonUtility.FromJson<MatFile>(File.ReadAllText(file)); }
                catch (Exception e) { Warn("unreadable " + Path.GetFileName(file) + ": " + e.Message); }
                if (parsed != null) Take(parsed.materials, Path.GetFileName(file));
            }
            Take(fromLayout, Path.GetFileName(LayoutPath));
            return order.Select(n => merged[n]).ToList();
        }

        private static Dictionary<string, Material> BuildMaterials(List<MatSpec> specs)
        {
            var folder = Root + "/Materials";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(Root, "Materials");
            var standard = Shader.Find("Standard");
            var fade = Shader.Find(NearFade.ShaderName);
            var foliage = Shader.Find("TumbangPreso/KantoFoliage");
            if (fade == null) throw new InvalidOperationException("Missing shader " + NearFade.ShaderName);
            if (foliage == null) throw new InvalidOperationException("Missing shader TumbangPreso/KantoFoliage");
            var result = new Dictionary<string, Material>();
            foreach (var s in specs)
            {
                string path = $"{folder}/{s.name}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null) { m = new Material(fade); AssetDatabase.CreateAsset(m, path); }
                bool cut = s.foliage || s.cutout;
                m.shader = cut ? foliage : s.emissive ? standard : fade;
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
                    // The leaf fill is light THROUGH a leaf. A grille or a hung cloth is not lit that way.
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
                    // ⚠️ THE NEAR BAND IS SHUT ON THIS MAP (owner, 2026-10-09, standing on a ledge with the house beside
                    // him stippled away: "distance fade effect is now being applied to everything"). Every surface here
                    // wears this shader so that Paete's cutscene can open the sky through the walls (`_SkyReveal`), and
                    // that is all it is wanted for: with the ordinary 1.8 m band on, any wall, eave or post a body stood
                    // next to dissolved, and in an alley a body is always next to a wall. A start of a millimetre
                    // keeps the sky window and never fades for nearness (the prototype map's walls do the same).
                    m.SetFloat("_NearFadeStart", 0.001f);
                    m.SetFloat("_NearFadeEnd", 0f);
                    m.SetFloat("_NearFadeCell", NearFade.DitherCellPixels);
                    // No authored surface family: the painted texture is the whole surface.
                    m.SetFloat("_SurfaceKind", 0f);
                    m.SetFloat("_SurfaceVertexRoles", 0f);
                    m.SetFloat("_SurfaceCoordinates", 0f);
                    m.SetFloat("_SurfaceDebug", 0f);
                    m.DisableKeyword("_EMISSION");
                    m.renderQueue = -1;
                }
                EditorUtility.SetDirty(m);
                result[s.name] = m;
            }
            return result;
        }

        /// <summary>sRGB (a normal map is not), Repeat (Clamp for a cut-out), aniso 4, mip maps,
        /// at most 1024. Null when the file is not there.</summary>
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
                r.shadowCastingMode = r.name.StartsWith("wire", StringComparison.OrdinalIgnoreCase) ? ShadowCastingMode.Off : ShadowCastingMode.On;
            }
        }

        // ------------------------------------------------------------------ collision

        /// <summary>
        /// `Models/collision.glb`: one plain GameObject per mesh, carrying a non-convex
        /// MeshCollider and nothing drawn. The model is read, not instantiated, so there is no
        /// renderer to strip and no prefab link in the scene.
        /// </summary>
        private static List<MeshCollider> Collision(Transform root, out float lowest)
        {
            var result = new List<MeshCollider>();
            lowest = 0f;
            var group = Group(root, "Collision");
            group.gameObject.layer = 0;
            string path = Root + "/Models/collision.glb";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) { Warn("missing Models/collision.glb: THE MAP HAS NO FLOOR AND NO WALLS"); return result; }
            var origin = prefab.transform;
            foreach (var mf in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                var go = new GameObject(mf.name) { layer = 0, isStatic = true };
                go.transform.SetParent(group, false);
                go.transform.localPosition = origin.InverseTransformPoint(mf.transform.position);
                go.transform.localRotation = Quaternion.Inverse(origin.rotation) * mf.transform.rotation;
                go.transform.localScale = mf.transform.lossyScale;
                var collider = go.AddComponent<MeshCollider>();
                collider.convex = false;
                collider.sharedMesh = mf.sharedMesh;
                lowest = Mathf.Min(lowest, collider.bounds.min.y);
                result.Add(collider);
            }
            if (result.Count == 0) Warn("Models/collision.glb has no meshes");
            Physics.SyncTransforms();
            return result;
        }

        /// <summary>
        /// ⚠️ THE SAME COLLISION AGAIN, AS SOLIDS (owner, 2026-10-09, in play: "bug, slipper ended up under the map").
        /// `collision.glb` is one hollow shell: a floor is a single face with nothing behind it, so a fast slipper that
        /// got through one lay inside the terrace for good. The layout also lists every piece of it: each box becomes a
        /// `BoxCollider` and each stair a CONVEX wedge, under "CollisionSolids". They stand exactly where the shell's
        /// faces are, so nothing walks differently; but a thing that ends up inside one is pushed back out, and a
        /// thick solid is not tunnelled the way a face is. The shell stays: the editor's bakes cast against it.
        /// </summary>
        private static string Solids(Transform root, SolidSpec spec)
        {
            if (spec == null || spec.boxes == null) { Warn("the layout lists no solids: the collision is a hollow shell only"); return "no solids"; }
            var group = Group(root, "CollisionSolids");
            group.gameObject.layer = 0; group.gameObject.isStatic = true;
            int boxes = 0, wedges = 0;
            foreach (var b in spec.boxes)
            {
                if (b == null || b.c == null || b.s == null || b.c.Length < 3 || b.s.Length < 3) continue;
                var box = group.gameObject.AddComponent<BoxCollider>();
                box.center = new Vector3(b.c[0], b.c[1], b.c[2]);
                box.size = new Vector3(Mathf.Max(0.02f, b.s[0]), Mathf.Max(0.02f, b.s[1]), Mathf.Max(0.02f, b.s[2]));
                boxes++;
            }
            int[] tris = { 0, 2, 1, 0, 3, 2, 4, 5, 6, 4, 6, 7, 0, 1, 5, 0, 5, 4, 1, 2, 6, 1, 6, 5, 2, 3, 7, 2, 7, 6, 3, 0, 4, 3, 4, 7 };
            foreach (var w in spec.wedges ?? new SolidWedge[0])
            {
                if (w == null || w.p == null || w.p.Length < 24) continue;
                var points = new Vector3[8];
                for (int i = 0; i < 8; i++) points[i] = new Vector3(w.p[i * 3], w.p[i * 3 + 1], w.p[i * 3 + 2]);
                var mesh = new Mesh { name = "Stair wedge " + wedges };
                mesh.SetVertices(points); mesh.SetTriangles(tris, 0); mesh.RecalculateBounds();
                var go = new GameObject("Wedge " + wedges) { layer = 0, isStatic = true };
                go.transform.SetParent(group, false);
                var collider = go.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh; collider.convex = true;
                wedges++;
            }
            Physics.SyncTransforms();
            return $"{boxes} solid boxes and {wedges} wedges";
        }

        /// <summary>
        /// The walkable surface under a mark, by casting down through the collision. A stepped
        /// alley has roofs and awnings over its floor, so every surface on the way down is found
        /// and the one taken is the one nearest <paramref name="near"/> in height that has a
        /// body's headroom over it.
        /// </summary>
        private static bool FloorUnder(List<MeshCollider> colliders, float x, float z, float near, out float y)
        {
            var surfaces = new List<float>();
            float from = near + 40f, stop = near - 40f;
            for (int guard = 0; guard < 32 && from > stop; guard++)
            {
                float best = float.NegativeInfinity;
                var ray = new Ray(new Vector3(x, from, z), Vector3.down);
                foreach (var c in colliders)
                    if (c.Raycast(ray, out var hit, from - stop) && hit.point.y > best) best = hit.point.y;
                if (float.IsNegativeInfinity(best)) break;
                surfaces.Add(best);
                from = best - 0.02f;
            }
            y = near;
            bool found = false; float distance = float.MaxValue;
            for (int i = 0; i < surfaces.Count; i++)
            {
                // surfaces run from the top down: the one before is the nearest thing overhead.
                if (i > 0 && surfaces[i - 1] - surfaces[i] < 1.9f) continue;
                float d = Mathf.Abs(surfaces[i] - near);
                if (d < distance) { distance = d; y = surfaces[i]; found = true; }
            }
            return found;
        }

        // ------------------------------------------------------------------ gameplay

        /// <summary>JsonUtility cannot read an array of arrays, so "spawns":[[x,y,z],...] is read by hand.</summary>
        private static List<Vector3> ParseSpawns(string json)
        {
            var result = new List<Vector3>();
            var key = Regex.Match(json, "\"spawns\"\\s*:\\s*\\[");
            if (!key.Success) return result;
            int start = key.Index + key.Length, depth = 1, end = start;
            while (end < json.Length && depth > 0) { char c = json[end]; if (c == '[') depth++; else if (c == ']') depth--; end++; }
            var numbers = Regex.Matches(json.Substring(start, Math.Max(0, end - start - 1)), "-?\\d+(\\.\\d+)?([eE][-+]?\\d+)?")
                .Cast<Match>().Select(m => float.Parse(m.Value, CultureInfo.InvariantCulture)).ToList();
            if (numbers.Count % 3 != 0) Warn("layout spawns is not a list of [x, y, z]; the trailing numbers are dropped");
            for (int i = 0; i + 2 < numbers.Count; i += 3) result.Add(new Vector3(numbers[i], numbers[i + 1], numbers[i + 2]));
            return result;
        }

        private static int Gameplay(Transform root, GameplaySpec g, List<Vector3> marks, List<MeshCollider> colliders, float lowest)
        {
            var can = new Vector3(g.can[0], g.can[1], g.can[2]);
            var match = Group(root, "~Match");
            match.gameObject.AddComponent<MatchInstaller>();

            // 12 m under the lowest thing a body can stand on (the alley steps down as well as up).
            var kill = Group(root, "KillPlane");
            kill.localPosition = new Vector3(0, Mathf.Min(0f, lowest) - 12f, 0);
            kill.gameObject.AddComponent<KillPlane>();
            var trigger = kill.gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(KillPlane.PlaneExtent, KillPlane.PlaneThickness, KillPlane.PlaneExtent);

            // Four walls, 1 m thick, their inner faces at +/-half_x and +/-half_z, from y -2 up.
            // `MatchInstaller.MeasurePlayableBounds` reads these boxes per side.
            var bounds = Group(root, "Bounds");
            float h = g.wall_height, midY = -2f + h * 0.5f;
            if (lowest < -2f) Warn($"the collision goes down to y {lowest:0.##}, under the bounds walls' foot at y -2");
            foreach (float side in new[] { -1f, 1f })
            {
                var x = Group(bounds, "Limit X " + side).gameObject.AddComponent<BoxCollider>();
                x.center = new Vector3(side * (g.half_x + 0.5f), midY, 0); x.size = new Vector3(1, h, 2 * g.half_z + 2);
                var z = Group(bounds, "Limit Z " + side).gameObject.AddComponent<BoxCollider>();
                z.center = new Vector3(0, midY, side * (g.half_z + 0.5f)); z.size = new Vector3(2 * g.half_x + 2, h, 1);
            }

            // Chalk: the 14 m square and the two throwing lines, round the can, 12 mm over the box.
            // Its own plain bright material, in this map's folder.
            string chalkPath = Root + "/Materials/alley_chalk.mat";
            var chalkMat = AssetDatabase.LoadAssetAtPath<Material>(chalkPath);
            if (chalkMat == null) { chalkMat = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(chalkMat, chalkPath); }
            chalkMat.color = new Color(0.99f, 0.99f, 0.97f); chalkMat.SetFloat("_Glossiness", 0.05f);
            chalkMat.EnableKeyword("_EMISSION"); chalkMat.SetColor("_EmissionColor", new Color(0.18f, 0.18f, 0.17f));
            EditorUtility.SetDirty(chalkMat);
            var chalk = Group(root, "Chalk");
            float y = g.box_y + 0.012f, r = Balance.ConfinementRadius;
            foreach (float side in new[] { -1f, 1f })
            {
                Line(chalk, "Court X", new Vector3(can.x + side * r, y, can.z), new Vector3(0.14f, 0.012f, r * 2 + 0.14f), chalkMat);
                Line(chalk, "Court Z", new Vector3(can.x, y, can.z + side * r), new Vector3(r * 2 + 0.14f, 0.012f, 0.14f), chalkMat);
                Line(chalk, "Throwing line", new Vector3(can.x, y, can.z + side * Confinement.ThrowingLine()), new Vector3(10, 0.012f, 0.07f), chalkMat);
            }

            // Spawn marks. From the layout as given; with none, Kanto's (the taya at the can, three
            // attackers on the spawn ring), each stood on whatever stair or terrace is under it.
            var spawns = Group(root, "SpawnPoints");
            if (marks.Count == 0)
            {
                marks.Add(can);
                for (int i = 1; i < 4; i++) marks.Add(new Vector3(can.x + (i - 2) * 3, can.y, can.z - Confinement.AttackerSpawnRing()));
                for (int i = 0; i < marks.Count; i++)
                {
                    var m = marks[i];
                    if (FloorUnder(colliders, m.x, m.z, can.y, out float floor)) m.y = floor;
                    else Warn($"no collision under default spawn {i} at ({m.x:0.##}, {m.z:0.##}); it stays at the can's height");
                    m.y += 0.1f;
                    marks[i] = m;
                }
            }
            for (int i = 0; i < marks.Count; i++) Group(spawns, "Spawn" + i).localPosition = marks[i];
            return marks.Count;
        }

        private static void Line(Transform parent, string name, Vector3 at, Vector3 size, Material m)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = at; go.transform.localScale = size;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var rend = go.GetComponent<Renderer>();
            if (m != null) rend.sharedMaterial = m;
            rend.shadowCastingMode = ShadowCastingMode.Off;
        }

        // ------------------------------------------------------------------ bounce pads

        /// <summary>
        /// One `JumpPad` per pad, at the tarp's top surface. ⚠️ LEFT ALONE A PAD WEARS THE
        /// PAVEMENT PAD'S LOOK (`JumpPad`'s defaults: the amber modelled plate, the rising white
        /// frames, the two climbing chevrons, a gold halo and a small point light), scaled so its
        /// plate is `2 x radius` square. That is drawn ON TOP of the tarp: the plate is opaque, so
        /// the middle of the tarp is covered and its stripes show only round the edge. It is the
        /// simplest thing that works and it reads at once as "this throws you". A tarp that
        /// should be the pad itself needs a model of its own: give the pad `"model"` (a
        /// `Models/NAME.glb` with parts named `prefix` + base, cushion, chevron, ring),
        /// `"paint"` (a `Textures/NAME_albedo.png` atlas), and optionally `"prefix"`,
        /// `"model_half"` (half the model's width, metres) and `"round"`.
        /// </summary>
        private static int Pads(Transform root, PadSpec[] pads)
        {
            if (pads == null || pads.Length == 0) return 0;
            var group = Group(root, "Pads");
            int n = 0;
            foreach (var p in pads)
            {
                if (p == null || p.position == null || p.position.Length < 3) { Warn("a pad has no position"); continue; }
                n++;
                var go = new GameObject("Bounce " + n);
                go.transform.SetParent(group, false);
                go.transform.localPosition = new Vector3(p.position[0], p.position[1], p.position[2]);
                var pad = go.AddComponent<JumpPad>();
                if (p.radius > 0f) pad.Radius = p.radius;
                pad.LaunchSpeed = p.speed > 0f ? p.speed : 11f;
                pad.ArenaSound = true;      // the Arena pad's own launch sound, at the owner's word
                if (!string.IsNullOrEmpty(p.model))
                {
                    pad.Model = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Models/{p.model}.glb");
                    if (pad.Model == null) Warn($"pad {n}: missing model Models/{p.model}.glb, it wears the pavement pad");
                }
                if (!string.IsNullOrEmpty(p.paint))
                {
                    pad.Paint = LoadTexture($"{Root}/Textures/{p.paint}_albedo.png", false, false);
                    if (pad.Paint == null) Warn($"pad {n}: missing paint Textures/{p.paint}_albedo.png");
                }
                if (!string.IsNullOrEmpty(p.prefix)) pad.PartPrefix = p.prefix;
                if (p.model_half > 0f) pad.ModelHalfSize = p.model_half;
                pad.Round = p.round;
            }
            return n;
        }

        // ------------------------------------------------------------------ light and sky

        /// <summary>The map's look row; the code's own if the profile asset has not been given one yet.</summary>
        private static WorldLookProfile.MapLook Look()
        {
            var look = WorldLookProfile.Current.Maps.FirstOrDefault(m => m.Map == MapName);
            if (look != null) return look;
            Warn("Resources/WorldLookProfile.asset has no 'EskinitaAlley' row; the scene is lit from the code's row and PLAY WILL WEAR NO LOOK until the asset has it");
            var fresh = ScriptableObject.CreateInstance<WorldLookProfile>();
            look = fresh.Maps.FirstOrDefault(m => m.Map == MapName);
            Object.DestroyImmediate(fresh);
            if (look == null) throw new InvalidOperationException("WorldLookProfile has no row named " + MapName);
            return look;
        }

        /// <summary>
        /// A WARM LATE AFTERNOON, and the same numbers Play wears: the scene's sun and
        /// RenderSettings are written from the "EskinitaAlley" row of `WorldLookProfile`, so the
        /// editor view and a match agree (in Play `WorldLookPresentation` lerps to that row and
        /// adds the blocky clouds; the review installs it the same way). The sky is the game's
        /// own (`TumbangPreso/NeighbourhoodSky`) in a material of this map's,
        /// Art/MapAtmosphere/EskinitaAlleySky.mat, made by `MapAtmosphereAuthor.Apply`.
        /// </summary>
        private static void Lighting(Transform root, float[] sunSpec)
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(root, false);
            sun.type = LightType.Directional;
            MapAtmosphereAuthor.Apply(MapName);
            var look = Look();
            float elevation = look.SunElevation > 0 ? look.SunElevation : 32f;
            float yaw = sunSpec != null && sunSpec.Length > 0 ? sunSpec[0] : DefaultSunYaw;
            sun.transform.rotation = Quaternion.Euler(elevation, yaw, 0);
            sun.color = look.Sun; sun.intensity = look.SunIntensity;
            sun.shadows = LightShadows.Soft; sun.shadowStrength = look.ShadowStrength;
            sun.shadowBias = .025f; sun.shadowNormalBias = .16f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = look.Sky; RenderSettings.ambientEquatorColor = look.Equator;
            RenderSettings.ambientGroundColor = look.Ground;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = look.Fog; RenderSettings.fogStartDistance = look.FogStart; RenderSettings.fogEndDistance = look.FogEnd;
            var sky = RenderSettings.skybox;
            if (sky != null)
            {
                sky.SetColor("_Zenith", look.Zenith); sky.SetColor("_Horizon", look.Horizon);
                sky.SetColor("_CloudLight", look.CloudLight); sky.SetColor("_CloudShade", look.CloudShade);
                sky.SetColor("_SunColor", look.Sun); sky.SetVector("_SunDirection", -sun.transform.forward);
                EditorUtility.SetDirty(sky);
            }
            // A little contrast and a saturation lift, so painted walls hold their colour in the warm haze.
            root.GetComponent<MapGrade>().Set(1.02f, 1.04f, 1.10f, 1, 1.9f);
        }

        private static Transform Group(Transform parent, string name)
        { var t = new GameObject(name).transform; t.SetParent(parent, false); return t; }
    }
}
