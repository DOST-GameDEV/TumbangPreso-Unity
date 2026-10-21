using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.Map;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// PAYO'S LIFE AND SOUND (owner, 2026-10-12: "theres a lack of liveliness and character to it. and the map essentially
    /// looks empty"). Everything here is local presentation: no collider, nothing sent, nothing gameplay reads.
    ///  * `PayoLife`: wind in every plant, the sea of cloud drifting, smoke from the houses, mist at the falls' feet,
    ///    and who is out by day and from dusk.
    ///  * BIRDS on the Lagoon's flock system (`LagoonFlocks`, set up as the Ilalim's pigeons and the run's kites are):
    ///    egrets that work the paddies, a pair of hawks over the valley, swiftlets from dusk. The Lagoon's seabird model
    ///    on materials of this map's own.
    ///  * CHICKENS by the two houses nearest the court (`AlleyChickens`, the alley's own models, materials and voices),
    ///    on a patch of ground measured flat.
    ///  * `PayoSound`: the beds, the falls, the hawk and the rooster, the stone, the gong, the geyser, from real recordings
    ///    built by `tools/build_payo_sfx.py`. A clip that is not built yet is left empty and said so.
    ///  * The bubble spirit that catches a fallen body (`PayoBubbleSpiritCatch`, owner 2026-10-18), built in code. The
    ///    hawk before it (`PayoCatchBird`, `catch_bird.glb`) is kept but no longer put in the scene.
    /// ⚠️ NOT HERE YET: villagers, carabao and a dog. They wait on the art's "life" list.
    /// </summary>
    public static partial class PayoSceneBuilder
    {
        private const string Seabird = "Assets/TumbangPreso/Art/LagoonCove/Fauna/fauna_seabird.glb";
        private const string AlleyRoot = "Assets/TumbangPreso/Art/EskinitaAlley";
        private const string AmbienceFolder = "Assets/TumbangPreso/Art/audio/ambience";

        private static AudioClip Clip(string file)
        {
            string path = $"{AmbienceFolder}/{file}.wav";
            if (!File.Exists(path)) return null;
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        /// <summary>A long loop: one channel, uncompressed in memory, as the run's beds are imported.</summary>
        private static AudioClip Bed(string file)
        {
            string path = $"{AmbienceFolder}/{file}.wav";
            if (!File.Exists(path)) { Warn("no recording " + file + ".wav yet (tools/build_payo_sfx.py makes it): that part of the soundscape is silent"); return null; }
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer != null)
            {
                var settings = importer.defaultSampleSettings;
                if (!importer.forceToMono || settings.loadType != AudioClipLoadType.CompressedInMemory)
                {
                    importer.forceToMono = true;
                    settings.loadType = AudioClipLoadType.CompressedInMemory; settings.compressionFormat = AudioCompressionFormat.ADPCM;
                    importer.defaultSampleSettings = settings;
                    importer.SaveAndReimport();
                }
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        private static AudioClip[] Clips(string stem)
        {
            var list = new List<AudioClip>();
            for (int i = 1; i <= 12; i++) { var clip = Clip($"{stem}_{i}"); if (clip == null) break; list.Add(clip); }
            return list.ToArray();
        }

        /// <summary>The Lagoon's seabird on this map's own materials (the Lagoon's are never touched), recoloured by how
        /// light each of its parts is: an inactive copy under a holder whose origin is the bird's lowest point, exactly as
        /// `LagoonCoveLife.Template` makes one (children "body", "wing_l", "wing_r").</summary>
        private static Transform BirdTemplate(Transform parent, string kind, Color pale, Color mid, Color dark)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Seabird);
            if (prefab == null) { Warn("no bird model (" + Seabird + "): no " + kind); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = "model";
            var painted = Shader.Find(PaintedShader);
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    string key = mats[i].name.Replace(" (Instance)", "").Trim();
                    string path = $"{Generated}/payo_bird_{kind}_{key}.mat";
                    var source = mats[i];
                    Color c = (source.HasProperty("baseColorFactor") ? source.GetColor("baseColorFactor") : source.color).gamma;
                    var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (m == null) { m = new Material(painted); AssetDatabase.CreateAsset(m, path); }
                    m.shader = painted;
                    float light = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
                    m.SetColor("_Color", light > 0.75f ? pale : light > 0.45f ? mid : dark);
                    m.SetTexture("_MainTex", null); m.SetFloat("_Glossiness", 0.1f);
                    m.enableInstancing = true;
                    EditorUtility.SetDirty(m);
                    mats[i] = m;
                }
                r.sharedMaterials = mats;
                r.shadowCastingMode = ShadowCastingMode.On;
            }
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            var holder = new GameObject(kind).transform;
            holder.SetParent(parent, false);
            float low = float.MaxValue;
            foreach (var r in go.GetComponentsInChildren<Renderer>()) low = Mathf.Min(low, r.bounds.min.y);
            go.transform.SetParent(holder, true);
            if (low < float.MaxValue) go.transform.localPosition += new Vector3(0, go.transform.position.y - low, 0);
            holder.gameObject.SetActive(false);
            return holder;
        }

        private static LagoonFlocks Flock(Transform life, string name, Transform template)
        {
            var holder = Group(life, name);
            var flocks = holder.gameObject.AddComponent<LagoonFlocks>();
            flocks.BirdTemplate = template;
            flocks.FishTemplates = new Transform[0]; flocks.SchoolCentres = new Vector3[0]; flocks.SchoolFloor = new float[0];
            flocks.FeatherMaterials = new Material[0];
            flocks.WaterY = 0f; flocks.Dips = false; flocks.AvoidObstacles = false;
            // The stage: nothing flies low over the play (the flock keeps 14 m over `WaterY` inside this radius).
            flocks.CourtCentre = Vector3.zero; flocks.CourtKeepOut = 34f;
            return flocks;
        }

        /// <summary>The art's land given mesh colliders for the questions this file asks of it (where a paddy is, whether a
        /// patch is flat, where a figure's feet are); `Dispose` takes away again the colliders it made (the `collide`
        /// placements keep theirs). WATER is told by the land's own material under the hit: the 2026-10-18 land is
        /// chunks (payo_land_x*_y*) with one submesh a material, and a paddy's water is its `py_land_flooded` (an older
        /// art's whole pieces named payo_paddy* or payo_shelf_water* still count).</summary>
        private sealed class Ground : IDisposable
        {
            private readonly List<MeshCollider> _made = new List<MeshCollider>();
            private readonly Dictionary<Collider, (int[] ends, bool[] water)> _land = new Dictionary<Collider, (int[], bool[])>();
            private readonly HashSet<Collider> _wholeWater = new HashSet<Collider>();
            public Ground(Transform root)
            {
                var dressing = root.Find("Dressing");
                if (dressing == null) return;
                var pieces = new List<Transform>();
                foreach (Transform group in dressing)
                {
                    if (IsLand(group.name)) pieces.Add(group);
                    else foreach (Transform piece in group) if (IsLand(piece.name)) pieces.Add(piece);
                }
                if (root.Find("Terraces") != null) pieces.Add(root.Find("Terraces"));
                foreach (Transform piece in pieces)
                {
                    bool whole = piece.name.StartsWith("payo_paddy") || piece.name.StartsWith("payo_shelf_water");
                    foreach (var filter in piece.GetComponentsInChildren<MeshFilter>(true))
                    {
                        var mesh = filter.sharedMesh;
                        if (mesh == null) continue;
                        var collider = filter.GetComponent<MeshCollider>();
                        if (collider == null)
                        {
                            collider = filter.gameObject.AddComponent<MeshCollider>();
                            collider.sharedMesh = mesh;
                            _made.Add(collider);
                        }
                        if (whole) { _wholeWater.Add(collider); continue; }
                        var mats = filter.GetComponent<Renderer>() != null ? filter.GetComponent<Renderer>().sharedMaterials : new Material[0];
                        var ends = new int[mesh.subMeshCount]; var water = new bool[mesh.subMeshCount];
                        int total = 0;
                        for (int sub = 0; sub < mesh.subMeshCount; sub++)
                        {
                            total += (int)(mesh.GetIndexCount(sub) / 3); ends[sub] = total;
                            string n = sub < mats.Length && mats[sub] != null ? mats[sub].name : "";
                            water[sub] = n.Contains("flooded") || n.Contains("paddy_water") || n == "py_water";
                        }
                        _land[collider] = (ends, water);
                    }
                }
                // The `collide` placements (the court platform's paving, the pools, the head) are ground too, never water.
                foreach (var collider in dressing.GetComponentsInChildren<MeshCollider>(true))
                    if (!_land.ContainsKey(collider) && !_wholeWater.Contains(collider)) _land[collider] = (new int[0], new bool[0]);
                Physics.SyncTransforms();
            }
            public bool Any => _land.Count > 0 || _wholeWater.Count > 0;
            public bool AnyWater => _wholeWater.Count > 0 || _land.Values.Any(l => l.water.Any(w => w));
            public bool IsGround(Collider c) => c != null && (_land.ContainsKey(c) || _wholeWater.Contains(c));
            /// <summary>Whether a hit is on a paddy's water (the material of the triangle hit).</summary>
            public bool IsWater(RaycastHit hit)
            {
                if (hit.collider == null) return false;
                if (_wholeWater.Contains(hit.collider)) return true;
                if (!_land.TryGetValue(hit.collider, out var l)) return false;
                for (int sub = 0; sub < l.ends.Length; sub++) if (hit.triangleIndex < l.ends[sub]) return l.water[sub];
                return false;
            }
            public bool Down(float x, float z, out RaycastHit hit) =>
                Physics.Raycast(new Vector3(x, 400f, z), Vector3.down, out hit, 900f, ~0, QueryTriggerInteraction.Ignore);
            /// <summary>The land under a point, within `reach` above or below it (the highest land surface in that span).</summary>
            public bool Under(Vector3 at, float reach, out RaycastHit ground)
            {
                ground = default; bool found = false;
                foreach (var hit in Physics.RaycastAll(at + Vector3.up * reach, Vector3.down, reach * 2f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (!IsGround(hit.collider)) continue;
                    if (!found || hit.point.y > ground.point.y) { ground = hit; found = true; }
                }
                return found;
            }
            public void Dispose() { foreach (var c in _made) if (c != null) Object.DestroyImmediate(c); _made.Clear(); }
        }

        /// <summary>
        /// THE ART'S "life" LIST AS FIGURES THE GAME MOVES (`PayoActors`): each entry's model stood at its place and facing,
        /// its kind told to the component, which knows what each kind does. A kind this does not know is left standing
        /// still and said; a model that is not there is warned of and skipped. A new json re-places them all.
        /// </summary>
        private static string Actors(Transform lifeRoot, Art art, out HashSet<string> kinds)
        {
            kinds = new HashSet<string>();
            var list = art.Life;
            if (list.Count == 0) return "no figures (the art's json has no life list)";
            var holder = Group(lifeRoot, "Figures");
            var actors = holder.gameObject.AddComponent<PayoActors>();
            var made = new List<PayoActors.Actor>();
            int missing = 0, unknown = 0, seated = 0, kept = 0, dropped = 0, airborne = 0;
            float moved = 0f;
            // The list was authored on an earlier land: each figure's feet are set again on the land under it now (when the
            // land is within 3 m of where it was put; farther off it stands on something that is not land, a porch, and keeps its height).
            using (var ground = new Ground(lifeRoot.parent))
            foreach (var entry in list)
            {
                string kind = Str(entry["kind"]) ?? "", model = Str(entry["model"]) ?? kind;
                var position = entry["position"] as Newtonsoft.Json.Linq.JArray;
                if (position == null || position.Count < 3) continue;
                var prefab = Model(model);
                if (prefab == null) { missing++; Warn("the life list names the model '" + model + "', which is not in Models"); continue; }
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, holder);
                go.name = kind + " " + made.Count;
                var at = new Vector3((float)position[0], (float)position[1], (float)position[2]);
                if (ground.Under(at, 3f, out var feet)) { moved = Mathf.Max(moved, Mathf.Abs(feet.point.y - at.y)); at.y = feet.point.y; seated++; }
                // ⚠️ NOT IN THE AIR (owner, 2026-10-19: "floating models out here"): the list predates this land, so a figure
                // with no land near its feet keeps its height only if something is under them (a porch, paving); otherwise
                // it is set on flat land below (within 60 m), or left out.
                else if (Physics.Raycast(at + Vector3.up * 0.5f, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore)) kept++;
                else if (ground.Under(at + Vector3.down * 57f, 60f, out var below) && below.normal.y > 0.85f)
                { moved = Mathf.Max(moved, Mathf.Abs(below.point.y - at.y)); at.y = below.point.y; seated++; dropped++; }
                else { Object.DestroyImmediate(go); airborne++; continue; }
                float yaw = entry["yaw"] != null && (entry["yaw"].Type == Newtonsoft.Json.Linq.JTokenType.Float || entry["yaw"].Type == Newtonsoft.Json.Linq.JTokenType.Integer) ? (float)entry["yaw"] : 0f;
                go.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
                Rematerial(go, _named);
                foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
                Finish(go, false);
                kinds.Add(kind);
                if (!PayoActors.TryKind(kind, out var k)) { unknown++; Warn("the life list has the kind '" + kind + "', which PayoActors does not know: it stands still"); continue; }
                made.Add(new PayoActors.Actor { Name = go.name, Kind = k, Root = go.transform, Home = at, Yaw = yaw });
            }
            actors.Actors = made.ToArray();
            EditorUtility.SetDirty(actors);
            return $"{made.Count} figures from the art's life list ({string.Join(", ", made.GroupBy(a => a.Kind).Select(g => g.Count() + " " + g.Key))}; {missing} models missing, {unknown} of an unknown kind; {seated} set on the land (at most {moved:0.00} m moved, {dropped} of them brought down from the air), {kept} kept at their height on something, {airborne} left out with no land under them)";
        }

        private static string Life(Transform root, PayoLayout layout, ArenaStage stage, Art art)
        {
            var lifeRoot = Group(root, "Life");
            var life = lifeRoot.gameObject.AddComponent<PayoLife>();
            var dressing = root.Find("Dressing");
            var said = new List<string>();

            // ---- wind in the plants, the sea of cloud
            var plants = new List<Transform>();
            Transform clouds = null;
            var houses = new List<Transform>();
            if (dressing != null)
                foreach (Transform group in dressing)
                {
                    string n = group.name;
                    if (n.StartsWith("payo_clouds") && group.childCount > 0) clouds = group.GetChild(0);
                    else if (n.StartsWith("house") || n.StartsWith("payo_house")) foreach (Transform instance in group) houses.Add(instance);
                }
            // The plants are the art's own group "plant" (trees, palms, bushes): each sways about its foot.
            plants.AddRange(Plants.Where(t => t != null));
            life.Plants = plants.ToArray();
            life.Clouds = clouds;
            if (clouds != null) foreach (var t in clouds.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
            said.Add($"{plants.Count} plants sway, the sea of cloud {(clouds != null ? "drifts" : "is ABSENT")}");

            // ---- the figures, from the art's own list
            said.Add(Actors(lifeRoot, art, out var kinds));

            // ---- smoke from the cooking fires (or, with none placed, from the roofs of the houses nearest the court), mist at the falls' feet
            var smoke = new List<Vector3>();
            foreach (string hearth in new[] { "hearth_pot", "prop_cook_fire" })
            {
                var hearths = dressing != null ? dressing.Find(hearth) : null;
                if (hearths != null) foreach (Transform fire in hearths) smoke.Add(fire.position + Vector3.up * 0.9f);
            }
            smoke = smoke.OrderBy(p => p.sqrMagnitude).Take(10).ToList();
            foreach (var house in houses.OrderBy(h => h.position.sqrMagnitude).Take(smoke.Count > 0 ? 0 : 9))
            {
                var b = new Bounds(house.position, Vector3.zero);
                foreach (var r in house.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
                smoke.Add(new Vector3(b.center.x, b.max.y - 0.15f, b.center.z));
            }
            life.Smoke = smoke.ToArray();
            var feet = FallFeet(dressing);
            // The assembled art's falls are pieces of their own (fall_great, fall_medium): each one's foot is the bottom of its bounds.
            foreach (string fall in new[] { "fall_great", "fall_medium" })
            {
                var falls = dressing != null ? dressing.Find(fall) : null;
                if (falls == null) continue;
                foreach (Transform piece in falls)
                {
                    var b = new Bounds(piece.position, Vector3.zero); bool any = false;
                    foreach (var r in piece.GetComponentsInChildren<Renderer>()) { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
                    if (any) feet.Add(new Vector3(b.center.x, b.min.y + 0.5f, b.center.z));
                }
            }
            life.FallFeet = feet.OrderBy(p => p.sqrMagnitude).Take(8).ToArray();
            said.Add($"smoke from {smoke.Count} houses, mist at {life.FallFeet.Length} of {feet.Count} falls' feet");

            // ---- birds
            var templates = Group(lifeRoot, "Templates");
            var dayOnly = new List<GameObject>(); var duskOn = new List<GameObject>();
            int perched = 0;
            using (var ground = new Ground(root))
            {
                // Egrets: white, long in the wing, working the flooded paddies on the climbing side.
                // (Only where the art's life list brings no egrets or hawks of its own.)
                var egret = kinds.Contains("egret") ? null : BirdTemplate(templates, "egret", new Color(0.97f, 0.97f, 0.94f), new Color(0.93f, 0.93f, 0.9f), new Color(0.2f, 0.2f, 0.18f));
                if (egret != null)
                {
                    var flocks = Flock(lifeRoot, "Egrets", egret);
                    flocks.SkyCentre = new Vector3(-30f, 0f, -38f); flocks.SkyRadius = 60f; flocks.SkyHeight = new Vector2(16f, 34f);
                    flocks.Flocks = 2; flocks.BirdsPerFlock = 4; flocks.FlightSpeed = new Vector2(5f, 8f); flocks.BirdScale = 1.25f;
                    flocks.MaxGroundedBirds = 5; flocks.LandEverySeconds = new Vector2(6f, 15f); flocks.LandGroup = new Vector2Int(1, 3);
                    flocks.WingFoldSweep = 84f; flocks.WingFoldDroop = 18f;
                    var perches = new List<Vector3>();
                    var random = new System.Random(733);
                    for (int tries = 0; tries < 2500 && perches.Count < 28 && ground.AnyWater; tries++)
                    {
                        float bearing = (float)random.NextDouble() * 360f, r = 38f + (float)random.NextDouble() * 60f;
                        Vector3 d = ArenaStageMesh.Direction(bearing) * r;
                        // Standing IN a paddy: the first thing under the sky there must be the paddy's water.
                        if (!ground.Down(d.x, d.z, out var hit) || !ground.IsWater(hit) || hit.point.y < -20f || hit.point.y > 45f) continue;
                        Vector3 along = ArenaStageMesh.Direction(bearing + 90f);
                        perches.Add(hit.point - along * 0.3f + Vector3.up * 0.02f); perches.Add(hit.point + along * 0.3f + Vector3.up * 0.02f);
                    }
                    flocks.PerchLines = perches.ToArray();
                    if (perches.Count == 0) flocks.MaxGroundedBirds = 0;
                    perched = perches.Count / 2;
                    EditorUtility.SetDirty(flocks);
                    dayOnly.Add(flocks.gameObject);
                }

                // A pair of hawks, high over the valley.
                var hawk = kinds.Contains("hawk") ? null : BirdTemplate(templates, "hawk", new Color(0.62f, 0.33f, 0.16f), new Color(0.46f, 0.27f, 0.14f), new Color(0.22f, 0.15f, 0.1f));
                if (hawk != null)
                {
                    var flocks = Flock(lifeRoot, "Hawks", hawk);
                    flocks.SkyCentre = new Vector3(45f, 0f, 95f); flocks.SkyRadius = 95f; flocks.SkyHeight = new Vector2(34f, 72f);
                    flocks.Flocks = 1; flocks.BirdsPerFlock = 2; flocks.FlightSpeed = new Vector2(6f, 9f); flocks.BirdScale = 2.0f;
                    flocks.MaxGroundedBirds = 0;
                    dayOnly.Add(flocks.gameObject);
                }

                // Swiftlets: small, dark and quick, out over the pit from dusk.
                var swift = BirdTemplate(templates, "swiftlet", new Color(0.2f, 0.2f, 0.24f), new Color(0.14f, 0.14f, 0.17f), new Color(0.08f, 0.08f, 0.1f));
                if (swift != null)
                {
                    var flocks = Flock(lifeRoot, "Swiftlets", swift);
                    flocks.SkyCentre = new Vector3(0f, 0f, 12f); flocks.SkyRadius = 58f; flocks.SkyHeight = new Vector2(15f, 32f);
                    flocks.Flocks = 2; flocks.BirdsPerFlock = 9; flocks.FlightSpeed = new Vector2(9f, 13f); flocks.BirdScale = 0.42f;
                    flocks.MaxGroundedBirds = 0; flocks.CourtKeepOut = 26f;
                    flocks.gameObject.SetActive(false);
                    duskOn.Add(flocks.gameObject);
                }
                said.Add($"flocks on the Lagoon's system: {(egret != null ? "8 egrets (" + perched + " places in the paddies to stand), " : "")}{(hawk != null ? "2 hawks, " : "")}18 swiftlets from dusk");

                if (!kinds.Contains("chicken")) said.Add(Chickens(lifeRoot, houses, ground));
            }
            life.DayOnly = dayOnly.ToArray(); life.DuskOn = duskOn.ToArray();
            EditorUtility.SetDirty(life);

            said.Add(Sound(root, layout, life, houses));
            said.Add(Catcher(stage));
            return "life: " + string.Join("; ", said);
        }

        /// <summary>The foot of each fall in the art's `payo_falls`: its sheets are told apart by where they stand (a 4 m
        /// grid joins what touches), and a sheet's foot is the middle of its lowest metre. Empty without the model.</summary>
        private static List<Vector3> FallFeet(Transform dressing)
        {
            var feet = new List<Vector3>();
            var falls = dressing != null ? dressing.Find("payo_falls") : null;
            if (falls == null) return feet;
            var points = new List<Vector3>();
            foreach (var filter in falls.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) continue;
                var r = filter.GetComponent<Renderer>();
                var mesh = filter.sharedMesh;
                var vertices = mesh.vertices;
                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    // Only the falling water, not the foam pools.
                    var m = r != null && sub < r.sharedMaterials.Length ? r.sharedMaterials[sub] : null;
                    if (m != null && m.name.Contains("foam")) continue;
                    foreach (int index in mesh.GetTriangles(sub).Distinct()) points.Add(filter.transform.TransformPoint(vertices[index]));
                }
            }
            if (points.Count == 0) return feet;
            const float cell = 4f;
            var cells = new Dictionary<Vector2Int, List<Vector3>>();
            foreach (var p in points)
            {
                var key = new Vector2Int(Mathf.FloorToInt(p.x / cell), Mathf.FloorToInt(p.z / cell));
                if (!cells.TryGetValue(key, out var list)) cells[key] = list = new List<Vector3>();
                list.Add(p);
            }
            var seen = new HashSet<Vector2Int>();
            foreach (var start in cells.Keys.ToList())
            {
                if (!seen.Add(start)) continue;
                var sheet = new List<Vector3>(); var queue = new Queue<Vector2Int>(); queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    var at = queue.Dequeue();
                    sheet.AddRange(cells[at]);
                    for (int dx = -1; dx <= 1; dx++)
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            var next = new Vector2Int(at.x + dx, at.y + dz);
                            if (cells.ContainsKey(next) && seen.Add(next)) queue.Enqueue(next);
                        }
                }
                float low = sheet.Min(p => p.y), high = sheet.Max(p => p.y);
                if (high - low < 2.5f) continue;                       // a stream on the flat is not a fall
                var bottom = sheet.Where(p => p.y < low + 1.0f).ToList();
                Vector3 foot = Vector3.zero; foreach (var p in bottom) foot += p;
                feet.Add(foot / bottom.Count + Vector3.up * 0.4f);
            }
            return feet;
        }

        /// <summary>A rooster, two hens and two chicks by each of the two houses nearest the court, on a patch of the art's
        /// ground measured flat (a 0.2 m grid within 5 cm of level): the alley's own birds, materials and voices.</summary>
        private static string Chickens(Transform lifeRoot, List<Transform> houses, Ground ground)
        {
            if (!ground.Any || houses.Count == 0) return "no chickens (no houses or no ground in the art)";
            var materials = new Dictionary<string, Material>();
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { AlleyRoot + "/Materials" }))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (m != null && m.name.StartsWith("ekc_", StringComparison.Ordinal)) materials[m.name] = m;
            }
            if (materials.Count == 0) return "no chickens (the alley's chicken materials are not in the project)";
            var chickens = lifeRoot.gameObject.AddComponent<AlleyChickens>();
            var groups = new List<AlleyChickens.Group>();
            var random = new System.Random(412);
            const float cell = 0.2f, half = 2.8f;
            foreach (var house in houses.OrderBy(h => h.position.sqrMagnitude).Take(6))
            {
                if (groups.Count >= 2) break;
                // A patch beside the house, on the side toward the court.
                Vector3 toward = -house.position; toward.y = 0f;
                Vector3 home = house.position + toward.normalized * 4.2f;
                if (!ground.Down(home.x, home.z, out var floor) || floor.normal.y < 0.97f) continue;
                int width = Mathf.RoundToInt(half * 2f / cell), height = width, open = 0;
                var walk = new bool[width * height];
                for (int j = 0; j < height; j++)
                    for (int i = 0; i < width; i++)
                    {
                        float x = home.x - half + (i + 0.5f) * cell, z = home.z - half + (j + 0.5f) * cell;
                        bool ok = ground.Down(x, z, out var hit) && hit.normal.y >= 0.97f && Mathf.Abs(hit.point.y - floor.point.y) < 0.05f && !ground.IsWater(hit);
                        walk[j * width + i] = ok; if (ok) open++;
                    }
                if (open < 150) continue;
                var holder = Group(lifeRoot, "Chickens " + groups.Count);
                var birds = new List<Transform>(); var size = new List<float>(); var rooster = new List<bool>(); var follow = new List<int>();
                int hen = -1;
                foreach (string model in new[] { "rooster", "hen", "hen_white", "chick", "chick" })
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{AlleyRoot}/Models/chicken_{model}.glb");
                    if (prefab == null) continue;
                    // On an open cell near the middle.
                    Vector3 at = new Vector3(home.x, floor.point.y, home.z);
                    for (int tries = 0; tries < 60; tries++)
                    {
                        int i = random.Next(width), j = random.Next(height);
                        if (!walk[j * width + i]) continue;
                        at = new Vector3(home.x - half + (i + 0.5f) * cell, floor.point.y, home.z - half + (j + 0.5f) * cell);
                        break;
                    }
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, holder);
                    go.transform.SetPositionAndRotation(at, Quaternion.Euler(0, (float)random.NextDouble() * 360f, 0));
                    go.transform.localScale = Vector3.one * (0.95f + (float)random.NextDouble() * 0.1f);
                    EskinitaAlleyLifeAuthor.Dress(go, materials, Warn, "chickens");
                    bool chick = model == "chick";
                    if (model.StartsWith("hen", StringComparison.Ordinal)) hen = birds.Count;
                    birds.Add(go.transform); size.Add(chick ? 0.35f : model == "rooster" ? 1f : 0.85f); rooster.Add(model == "rooster"); follow.Add(chick ? hen : -1);
                }
                groups.Add(new AlleyChickens.Group
                {
                    Name = "hamlet " + groups.Count, Home = new Vector3(home.x, floor.point.y, home.z), FloorY = floor.point.y, Roam = half,
                    GridOrigin = new Vector2(home.x - half, home.z - half), Cell = cell, Width = width, Height = height, Walkable = walk,
                    Perches = new Vector3[0], Entries = new[] { new Vector3(home.x, floor.point.y, home.z) },
                    Birds = birds.ToArray(), Size = size.ToArray(), Rooster = rooster.ToArray(), Follow = follow.ToArray(),
                });
            }
            chickens.Groups = groups.ToArray();
            chickens.FeatherMaterials = LagoonCoveLife.FeatherMaterials(("feather_manok_red", new Color(.722f, .333f, .184f)),
                ("feather_manok_white", new Color(.965f, .945f, .894f)), ("feather_manok_gold", new Color(.941f, .714f, .227f)));
            // Their voices: the alley's own recordings, through the alley's own voice pool.
            var sound = new GameObject("Voices").AddComponent<AlleyLifeSound>();
            sound.transform.SetParent(lifeRoot, false);
            var banks = new List<AlleyLifeSound.Bank>();
            void Bank(string cue, string stem, float gain, float near, float far)
            {
                var clips = Clips("sfx_alley_" + stem);
                if (clips.Length > 0) banks.Add(new AlleyLifeSound.Bank { Cue = cue, Clips = clips, Gain = gain, Near = near, Far = far });
            }
            Bank("chicken_cluck", "chicken_cluck", .4f, 1.5f, 16f); Bank("rooster_crow", "rooster_crow", .65f, 3f, 60f);
            Bank("chicken_squawk", "chicken_squawk", .55f, 2f, 26f); Bank("chicken_flap", "amb_wings", .45f, 2f, 20f);
            sound.Banks = banks.ToArray();
            chickens.Sound = sound;
            EditorUtility.SetDirty(chickens);
            return groups.Count > 0 ? $"{groups.Sum(g => g.Birds.Length)} chickens in {groups.Count} hamlets ({string.Join(", ", groups.Select(g => g.Home.ToString("F0")))}), {banks.Count} voice banks"
                                    : "NO chickens: no flat open patch was found beside the six houses nearest the court";
        }

        /// <summary>The soundscape's clips and places. A clip `tools/build_payo_sfx.py` has not built is left empty and warned of.</summary>
        private static string Sound(Transform root, PayoLayout layout, PayoLife life, List<Transform> houses)
        {
            var sound = Group(root, "Sound").gameObject.AddComponent<PayoSound>();
            sound.Wind = Bed("payo_bed_wind"); sound.Day = Bed("payo_bed_day"); sound.Night = Bed("payo_bed_night"); sound.Village = Bed("payo_bed_village");
            Vector3 gate = new Vector3(0f, 6f, -60f);
            sound.GongAt = gate;
            sound.VillageAt = houses.Count > 0 ? houses.OrderBy(h => h.position.sqrMagnitude).First().position : gate;
            sound.Grind = Bed("payo_bed_grind");
            sound.Lock = Clips("sfx_payo_lock"); sound.Gong = Clips("sfx_payo_gong"); sound.Geyser = Clips("sfx_payo_geyser"); sound.Hawk = Clips("sfx_payo_hawk");
            var loops = new List<PayoSound.Loop>(); var shots = new List<PayoSound.Shot>();
            var water = Bed("payo_bed_fall");
            if (water != null && life.FallFeet.Length > 0) loops.Add(new PayoSound.Loop { Name = "falling water", Clip = water, Points = life.FallFeet, Gain = 0.4f, Near = 6f, Far = 70f });
            void Shot(string name, string stem, Vector3[] points, float gain, float weight, float near, float far, float scatter, float lift, PayoSound.When when)
            {
                var clips = Clips(stem);
                if (clips.Length == 0 || points.Length == 0) { if (clips.Length == 0) Warn("no recording " + stem + "_1.wav yet: no " + name); return; }
                shots.Add(new PayoSound.Shot { Name = name, Clips = clips, Points = points, Gain = gain, Weight = weight, Near = near, Far = far, Scatter = scatter, Lift = lift, When = when });
            }
            Shot("a hawk's cry over the valley", "sfx_payo_hawk", new[] { new Vector3(45f, 50f, 95f), new Vector3(-20f, 45f, 70f) }, 0.26f, 0.6f, 14f, 260f, 20f, 0f, PayoSound.When.Day);
            Shot("a rooster in the hamlets", "sfx_alley_rooster_crow", houses.OrderBy(h => h.position.sqrMagnitude).Take(4).Select(h => h.position).ToArray(), 0.3f, 0.45f, 4f, 120f, 3f, 0.5f, PayoSound.When.Day);
            Shot("a frog close by", "sfx_payo_frog", new[] { new Vector3(-36f, LipY, -6f), new Vector3(36f, LipY, 4f), new Vector3(4f, LipY, -38f), new Vector3(-10f, LipY, 38f) }, 0.3f, 1.0f, 3f, 70f, 6f, 0f, PayoSound.When.Night);
            Shot("an owl far off", "sfx_payo_owl", new[] { new Vector3(-70f, 30f, -70f), new Vector3(60f, 10f, -80f) }, 0.24f, 0.4f, 12f, 260f, 15f, 0f, PayoSound.When.Night);
            sound.Loops = loops.ToArray(); sound.Shots = shots.ToArray();
            EditorUtility.SetDirty(sound);
            int beds = new[] { sound.Wind, sound.Day, sound.Night, sound.Village, sound.Grind }.Count(c => c != null);
            return $"sound: {beds} of 5 beds, {loops.Count} loops, {shots.Count} kinds of shot, lock {sound.Lock.Length}, gong {sound.Gong.Length}, geyser {sound.Geyser.Length}, hawk {sound.Hawk.Length} recordings";
        }

        /// <summary>
        /// What catches a fallen body, in place of the Arena's drone: the bubble spirit (owner, 2026-10-18, of the hawk:
        /// "does not fit the cutesy look of the game at all"). Built in code at run time, so the scene only carries the
        /// chooser; the catch, the carry and the freeze are untouched.
        /// </summary>
        private static string Catcher(ArenaStage stage)
        {
            if (stage == null) return "the catch is still the Arena's stand-in drone (no stage)";
            // Kanlit (owner's pick 2026-10-18, variant A): a carved bulul wood buddy under a gabi leaf, its vine hugs the body.
            bool kanlit = Shader.Find(PayoKanlit.BuddyShader) != null && Shader.Find(PayoKanlit.VineShader) != null
                && Resources.Load<TextAsset>(PayoKanlitMeshes.Folder + "kanlit") != null && Resources.Load<TextAsset>(PayoKanlitMeshes.Folder + "kanlit_vine") != null;
            if (kanlit)
            {
                stage.gameObject.AddComponent<PayoKanlitCatch>();
                var drone = stage.GetComponent<ArenaFallRecovery>();
                if (drone != null) drone.NoDrone = true;
                return "the catch is Kanlit (PayoKanlitCatch); the Arena's drone is not drawn";
            }
            Warn("Kanlit's shaders or baked meshes were not found: falling back to the bubble spirit");
            bool shaders = Shader.Find(PayoBubbleSpirit.SpiritShader) != null && Shader.Find(PayoBubbleSpirit.BubbleShader) != null && Shader.Find(PayoBubbleSpirit.SplashShader) != null;
            if (!shaders) { Warn("the bubble spirit's shaders were not found: the catch is still the Arena's drone"); return "the catch is still the Arena's drone (no spirit shaders)"; }
            stage.gameObject.AddComponent<PayoBubbleSpiritCatch>();
            var recovery = stage.GetComponent<ArenaFallRecovery>();
            if (recovery != null) recovery.NoDrone = true;
            return "the catch is the bubble spirit (PayoBubbleSpiritCatch); the Arena's drone is not drawn";
        }

        /// <summary>The hawk that caught a fallen body before the bubble spirit (2026-10-12 to 2026-10-18). Not called; kept to bring back.</summary>
        private static string Hawk(ArenaStage stage, Art art)
        {
            var model = Model("catch_bird");
            if (model == null || stage == null) return "the catch is still the Arena's stand-in drone (no Models/catch_bird.glb yet)";
            var template = (GameObject)PrefabUtility.InstantiatePrefab(model, stage.transform);
            template.name = "HawkTemplate";
            Rematerial(template, _named);
            foreach (var c in template.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            Finish(template, false);
            template.SetActive(false);
            var bird = stage.gameObject.AddComponent<PayoCatchBird>();
            bird.Template = template;
            // The art's own numbers: its talons hang `carry_point` from its origin, and they hold the body's shoulders (1.35 m up).
            var carry = art != null && art.Doc != null && art.Doc["catch"] is Newtonsoft.Json.Linq.JObject c0 ? c0["carry_point"] as Newtonsoft.Json.Linq.JArray : null;
            if (carry != null && carry.Count >= 3) { bird.Above = 1.35f - (float)carry[1]; bird.CarryBack = (float)carry[2]; }
            var recovery = stage.GetComponent<ArenaFallRecovery>();
            if (recovery != null) recovery.NoDrone = true;
            int wings = template.GetComponentsInChildren<Transform>(true).Count(t => t.name.ToLowerInvariant().Contains("wing"));
            if (wings == 0) Warn("catch_bird.glb has no node with 'wing' in its name: the hawk carries without beating its wings");
            return $"the catch is the hawk (catch_bird.glb, {wings} wing nodes); the Arena's drone is not drawn";
        }
    }
}
