using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TumbangPreso.Core;
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
    /// THE ALLEY'S LIVE CHICKENS (owner, 2026-10-09, at the rooster statue: "if you're gonna make
    /// chickens, make them live, make them like the birds in lagoon cove"). Called by
    /// `EskinitaAlleySceneBuilder.Build` after the dressing, the collision and the bounce pads exist,
    /// because, like `LagoonCoveLife`, everything it hands the runtime is measured from what is
    /// already in the scene:
    ///   * each GROUP is anchored to a thing (the tepee stand, the dama table) where there is one,
    ///     and its WALKABLE GRID is baked here: 0.2 m cells of the group's own terrace floor, found
    ///     by casting at the map's collision, kept only where the floor is flat and at the terrace's
    ///     height (so never a stair), clear of walls, outside the chalk square, off every bounce
    ///     tarp and outside the bounds of every placed prop, then flood-filled from the group's
    ///     home so every cell can be walked to;
    ///   * its PERCHES are the tops of the low things beside it (the tepee's ridge pole, a crate
    ///     stack, the dama table, a bench).
    /// The birds are `Models/chicken_*.glb` (tools/author_eskinita_chickens.py), wearing the map's
    /// own materials by name (`materials_chickens.json`), with no colliders and nothing static.
    /// `AlleyChickens` (runtime) gives them their life. ⚠️ Cosmetic and local: not networked, no
    /// colliders, no NavMesh, nothing gameplay reads.
    ///
    /// The cats, the dogs, the animal voices and the alley's ambience are EskinitaAlleyPetsAuthor's,
    /// called from the end of `Build` here.
    ///
    /// THE MOTION SHEET: write a version number (or nothing) into `Temp/eskinita-chickens.request`
    /// with the editor open and out of Play. The built scene is opened, the birds are stepped BY
    /// HAND in edit mode for 18 s (a stand-in body walks at each group from the sixth second to the
    /// eighth), ten moments are rendered per subject into `Logs/eskinita/chickens_motion_vN*.png`,
    /// the scenes that were open are reopened, and `Temp/eskinita-chickens.done` says what was
    /// written. Nothing is saved. It waits while `Temp/eskinita-alley.request` is unanswered.
    /// </summary>
    internal static class EskinitaAlleyLifeAuthor
    {
        private const string Root = "Assets/TumbangPreso/Art/EskinitaAlley";
        private const float Cell = .2f;

        private struct Spec
        {
            public string Name, Anchor;      // Anchor: a placed model the group gathers by, or null
            public Vector3 Near;             // where the group is wanted (Unity axes)
            public float Roam;
            public string[] Birds;           // model names; "chick" follows the first hen of its group
        }

        // ⚠️ `Near` is tried as given AND mirrored in x, and the side with more room wins (an anchored
        // side before an unanchored one). The brief's point for the dama table was across the alley
        // from the table. Its point for the court's edge, (-7.2, 0, 6.5), is the sari-sari store's
        // front, and its mirror is under the east stair flight: outside the chalk square the court's
        // edge is a 1 m strip along each wall, mostly taken by props and the two flights, so the
        // pair lives in the free stretch of it by the bench (west) or the drums (east), whichever is longer.
        private static readonly Spec[] Specs =
        {
            new Spec { Name = "tepee", Anchor = "prop_manok_cage", Near = new Vector3(-4.2f, .9f, -14.4f), Roam = 2.6f, Birds = new[] { "rooster", "hen", "hen_white" } },
            new Spec { Name = "dama", Anchor = "prop_table_dama", Near = new Vector3(3.3f, -.9f, 12f), Roam = 2.6f, Birds = new[] { "hen_white", "chick", "chick" } },
            new Spec { Name = "court", Anchor = null, Near = new Vector3(-7.45f, 0f, 1.7f), Roam = 2.8f, Birds = new[] { "rooster_white", "hen" } },
        };

        // Things an adult may flutter up onto: the top centre of the model's bounds is a real surface.
        // (Not the water drums: the top of a drum's bounds is the dipper left on its lid, and a rooster stood on the dipper.)
        private static readonly string[] PerchModels = { "prop_crates_stack", "prop_table_dama", "prop_bench_wood" };
        // What a hen can come back out from behind, or from inside: placed models have no collision and hide her.
        private static readonly string[] HidingModels = { "prop_crates_stack", "prop_water_drum", "prop_water_drum_yellow", "prop_sacks", "prop_tires_stack",
            "prop_jerrycans", "prop_pail_stack", "prop_kariton", "prop_plant_pots_a", "prop_plant_pots_b", "prop_plant_pots_c" };
        // The tepee's ridge pole top, in the PROP's own Unity axes (Blender (0.3, 0.05, 0.685) in tools/author_eskinita_props.py).
        private static readonly Vector3 TepeeRidge = new Vector3(-.3f, .685f, -.05f);

        private sealed class Baked
        {
            public Vector3 Home; public float FloorY; public Vector2 Origin; public int Width, Height; public bool[] Walk; public int Cells;
        }

        public static string Build(Transform root, Dictionary<string, Material> materials, List<MeshCollider> colliders, Vector3 can, Action<string> warn)
        {
            var dressing = root.Find("Dressing");
            // Everything placed that stands on a floor: a bird may not walk through it (placed models have no collision).
            var props = new List<(string model, Transform t, Bounds b)>();
            if (dressing != null)
                foreach (Transform group in dressing)
                {
                    if (!group.name.StartsWith("prop_") && !group.name.StartsWith("tree_")) continue;
                    foreach (Transform instance in group)
                    {
                        var rs = instance.GetComponentsInChildren<Renderer>();
                        if (rs.Length == 0) continue;
                        var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                        props.Add((group.name, instance, b));
                    }
                }
            var pads = root.GetComponentsInChildren<JumpPad>().Select(p => (at: p.transform.position, r: p.Radius + .7f)).ToList();

            var life = new GameObject("Life").transform;
            life.SetParent(root, false);
            var chickens = life.gameObject.AddComponent<AlleyChickens>();
            var groups = new List<AlleyChickens.Group>();
            var said = new List<string>();
            int birds = 0;
            foreach (var spec in Specs)
            {
                // Where: by the anchor if the map has one near either candidate point, stepped out toward the alley's middle.
                Baked best = null; bool bestAnchored = false;
                foreach (float mirror in new[] { 1f, -1f })
                {
                    var near = new Vector3(spec.Near.x * mirror, spec.Near.y, spec.Near.z);
                    bool anchored = false;
                    if (spec.Anchor != null)
                    {
                        var anchors = props.Where(p => p.model == spec.Anchor && Vector3.Distance(p.t.position, near) < 6f).OrderBy(p => Vector3.Distance(p.t.position, near)).ToList();
                        if (anchors.Count > 0) { var a = anchors[0].t.position; near = new Vector3(a.x - Mathf.Sign(a.x) * 1.4f, a.y, a.z); anchored = true; }
                    }
                    var baked = Bake(near, spec.Roam, colliders, props, pads, can);
                    if (baked == null) continue;
                    // By its anchor before anywhere else; between two of a kind, where there is more floor.
                    if (best == null || (anchored && !bestAnchored) || (anchored == bestAnchored && baked.Cells > best.Cells)) { best = baked; bestAnchored = anchored; }
                }
                if (best == null || best.Cells < 12) { warn($"chickens: no floor for the '{spec.Name}' group near ({spec.Near.x:0.#}, {spec.Near.y:0.#}, {spec.Near.z:0.#}); it is left out"); continue; }

                var g = new AlleyChickens.Group
                {
                    Name = spec.Name, Home = best.Home, FloorY = best.FloorY, Roam = spec.Roam, GridOrigin = best.Origin, Cell = Cell,
                    Width = best.Width, Height = best.Height, Walkable = best.Walk,
                };
                // Perches: low tops within reach of the group, outside the chalk square.
                var perches = new List<Vector3>();
                foreach (var p in props)
                {
                    Vector3 top;
                    if (p.model == "prop_manok_cage") top = p.t.TransformPoint(TepeeRidge);
                    else if (Array.IndexOf(PerchModels, p.model) >= 0) top = new Vector3(p.b.center.x, p.b.max.y, p.b.center.z);
                    else continue;
                    float height = top.y - best.FloorY;
                    if (height < .25f || height > 1.15f || Flat(top - best.Home).magnitude > spec.Roam + .9f) continue;
                    if (InChalk(top.x, top.z, can, 0f)) continue;
                    perches.Add(top);
                }
                g.Perches = perches.ToArray();
                // Entries: things a burst bird can come back out of (see AlleyChickens.StepDead). The tepee's own inside first.
                var entries = new List<Vector3>();
                foreach (var p in props)
                {
                    Vector3 at;
                    if (p.model == "prop_manok_cage") at = p.t.TransformPoint(new Vector3(TepeeRidge.x, 0f, TepeeRidge.z));
                    else if (Array.IndexOf(HidingModels, p.model) >= 0) at = new Vector3(p.b.center.x, best.FloorY, p.b.center.z);
                    else continue;
                    if (Mathf.Abs(p.b.min.y - best.FloorY) > .25f || Flat(at - best.Home).magnitude > spec.Roam + 1.2f || InChalk(at.x, at.z, can, 0f)) continue;
                    at.y = best.FloorY;
                    entries.Add(at);
                }
                g.Entries = entries.ToArray();

                // The birds, stood on cells near home, a hand apart, each facing its own way.
                var holder = new GameObject(spec.Name).transform;
                holder.SetParent(life, false);
                var random = new System.Random(spec.Name.GetHashCode() ^ 5417);
                var stood = new List<Vector3>(); var list = new List<Transform>(); var size = new List<float>(); var rooster = new List<bool>(); var follow = new List<int>();
                int hen = Array.FindIndex(spec.Birds, n => n.StartsWith("hen"));
                foreach (string model in spec.Birds)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Models/chicken_{model}.glb");
                    if (prefab == null) { warn($"chickens: missing model Models/chicken_{model}.glb (run tools/author_eskinita_chickens.py)"); continue; }
                    bool chick = model == "chick";
                    var at = Stand(best, random, stood, chick && hen >= 0 && hen < stood.Count ? stood[hen] : best.Home, chick ? .55f : 1.3f);
                    stood.Add(at);
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, holder);
                    go.name = $"{spec.Name} {model} {list.Count}";
                    go.transform.SetPositionAndRotation(at, Quaternion.Euler(0, (float)random.NextDouble() * 360f, 0));
                    go.transform.localScale = Vector3.one * (.95f + (float)random.NextDouble() * .1f);
                    Dress(go, materials, warn, "chickens");
                    foreach (string part in new[] { "torso", "head", "wing_l", "wing_r", "leg_l", "leg_r" })
                        if (Find(go.transform, part) == null) warn($"chickens: chicken_{model}.glb has no part named '{part}'; it will not be posed");
                    list.Add(go.transform);
                    size.Add(chick ? .35f : model.StartsWith("rooster") ? 1f : .85f);
                    rooster.Add(model.StartsWith("rooster"));
                    follow.Add(chick ? hen : -1);
                }
                g.Birds = list.ToArray(); g.Size = size.ToArray(); g.Rooster = rooster.ToArray(); g.Follow = follow.ToArray();
                groups.Add(g);
                birds += list.Count;
                said.Add($"{spec.Name} {list.Count} at ({best.Home.x:0.0}, {best.Home.y:0.0}, {best.Home.z:0.0}) on {best.Cells} cells with {perches.Count} perch(es), {entries.Count} way(s) back in");
            }
            chickens.Groups = groups.ToArray();
            // The Lagoon's painted feather, in the chickens' colours (red-brown, cream white, gold).
            chickens.FeatherMaterials = LagoonCoveLife.FeatherMaterials(
                ("feather_manok_red", new Color(.722f, .333f, .184f)), ("feather_manok_white", new Color(.965f, .945f, .894f)),
                ("feather_manok_gold", new Color(.941f, .714f, .227f)));
            EditorUtility.SetDirty(chickens);
            // ---- CATS, DOGS, THE ANIMAL VOICES AND THE ALLEY'S AMBIENCE (EskinitaAlleyPetsAuthor.cs)
            string more = EskinitaAlleyPetsAuthor.Build(root, life, materials, colliders, can, props, pads, chickens, warn);
            return $"chickens: {birds} birds in {groups.Count} groups ({string.Join("; ", said)}); {more}";
        }

        /// <summary>A placed animal wears the map's own materials by name, casts a shadow, and has NO collider:
        /// it must never block or push a body, a slipper or the can.</summary>
        internal static void Dress(GameObject go, Dictionary<string, Material> materials, Action<string> warn, string what)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    string key = mats[i].name.Replace(" (Instance)", "").Trim();
                    if (materials.TryGetValue(key, out var m)) mats[i] = m;
                    else warn($"{what}: no material spec for '{key}'; it keeps the importer's material");
                }
                r.sharedMaterials = mats;
                r.shadowCastingMode = ShadowCastingMode.On;
            }
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private static Transform Find(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        private static bool InChalk(float x, float z, Vector3 can, float margin)
        {
            float r = Balance.ConfinementRadius + margin;
            return Mathf.Abs(x - can.x) < r && Mathf.Abs(z - can.z) < r;
        }

        private static bool Cast(List<MeshCollider> colliders, Vector3 from, Vector3 direction, float length, out RaycastHit nearest)
        {
            nearest = default; bool any = false; float best = float.MaxValue;
            var ray = new Ray(from, direction);
            foreach (var c in colliders)
                if (c != null && c.Raycast(ray, out var hit, length) && hit.distance < best) { best = hit.distance; nearest = hit; any = true; }
            return any;
        }

        private static readonly Vector3[] Around =
        {
            Vector3.right, Vector3.left, Vector3.forward, Vector3.back,
            new Vector3(.7071f, 0, .7071f), new Vector3(-.7071f, 0, .7071f), new Vector3(.7071f, 0, -.7071f), new Vector3(-.7071f, 0, -.7071f),
        };

        /// <summary>The walkable grid round a point: see the class summary for what a cell must be.</summary>
        private static Baked Bake(Vector3 near, float roam, List<MeshCollider> colliders, List<(string model, Transform t, Bounds b)> props,
            List<(Vector3 at, float r)> pads, Vector3 can)
        {
            if (!Cast(colliders, near + Vector3.up * .7f, Vector3.down, 1.6f, out var floorHit)) return null;
            float floor = floorHit.point.y;
            int n = Mathf.CeilToInt(2f * roam / Cell) + 1;
            var origin = new Vector2(near.x - n * Cell * .5f, near.z - n * Cell * .5f);
            var ok = new bool[n * n];
            var low = props.Where(p => p.b.min.y < floor + .75f && p.b.max.y > floor + .03f && Flat(p.b.center - near).magnitude < roam + 12f).ToList();
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    float x = origin.x + (i + .5f) * Cell, z = origin.y + (j + .5f) * Cell;
                    if ((x - near.x) * (x - near.x) + (z - near.z) * (z - near.z) > roam * roam) continue;
                    if (InChalk(x, z, can, .16f)) continue;
                    if (pads.Any(p => Mathf.Abs(p.at.y - floor) < 2.5f && (p.at.x - x) * (p.at.x - x) + (p.at.z - z) * (p.at.z - z) < p.r * p.r)) continue;
                    if (low.Any(p => x > p.b.min.x - .16f && x < p.b.max.x + .16f && z > p.b.min.z - .16f && z < p.b.max.z + .16f)) continue;
                    // Flat floor at the terrace's own height: a stair, a kerb or a step up is none of those.
                    if (!Cast(colliders, new Vector3(x, floor + .5f, z), Vector3.down, 1f, out var hit)) continue;
                    if (Mathf.Abs(hit.point.y - floor) > .04f || hit.normal.y < .97f) continue;
                    // ⚠️ AND OPEN ABOVE: the short cast above starts INSIDE a stair flight or a ledge's block and finds the
                    // floor under it (the first bake stood the court's pair inside the east stair). From 1.5 m up, the
                    // first thing met must be that same floor.
                    if (!Cast(colliders, new Vector3(x, floor + 1.5f, z), Vector3.down, 2f, out var open) || Mathf.Abs(open.point.y - floor) > .04f) continue;
                    bool wall = false;
                    foreach (var d in Around)
                        if (Cast(colliders, new Vector3(x, floor + .1f, z), d, .26f, out _) || Cast(colliders, new Vector3(x, floor + .32f, z), d, .26f, out _)) { wall = true; break; }
                    if (wall) continue;
                    ok[j * n + i] = true;
                }
            // Home: the good cell nearest the wanted point, then only what can be walked to from it.
            int start = -1; float nearest = 1.8f * 1.8f;
            for (int k = 0; k < ok.Length; k++)
            {
                if (!ok[k]) continue;
                float x = origin.x + (k % n + .5f) * Cell, z = origin.y + (k / n + .5f) * Cell;
                float d = (x - near.x) * (x - near.x) + (z - near.z) * (z - near.z);
                if (d < nearest) { nearest = d; start = k; }
            }
            if (start < 0) return null;
            var keep = new bool[ok.Length]; var queue = new Queue<int>();
            keep[start] = true; queue.Enqueue(start); int cells = 0;
            while (queue.Count > 0)
            {
                int k = queue.Dequeue(); cells++;
                int i = k % n, j = k / n;
                void Try(int a, int b) { if (a < 0 || b < 0 || a >= n || b >= n) return; int q = b * n + a; if (ok[q] && !keep[q]) { keep[q] = true; queue.Enqueue(q); } }
                Try(i + 1, j); Try(i - 1, j); Try(i, j + 1); Try(i, j - 1);
            }
            // Home is the middle of what was kept (its cell nearest the centroid), so the group is not pinned to an edge.
            Vector2 sum = Vector2.zero;
            for (int k = 0; k < keep.Length; k++) if (keep[k]) sum += new Vector2(origin.x + (k % n + .5f) * Cell, origin.y + (k / n + .5f) * Cell);
            sum /= cells;
            int home = start; float hd = float.MaxValue;
            for (int k = 0; k < keep.Length; k++)
            {
                if (!keep[k]) continue;
                var c = new Vector2(origin.x + (k % n + .5f) * Cell, origin.y + (k / n + .5f) * Cell);
                float d = (c - sum).sqrMagnitude;
                if (d < hd) { hd = d; home = k; }
            }
            return new Baked
            {
                Home = new Vector3(origin.x + (home % n + .5f) * Cell, floor, origin.y + (home / n + .5f) * Cell),
                FloorY = floor, Origin = origin, Width = n, Height = n, Walk = keep, Cells = cells,
            };
        }

        private static Vector3 Stand(Baked baked, System.Random random, List<Vector3> taken, Vector3 round, float within)
        {
            var cells = new List<Vector3>();
            for (int k = 0; k < baked.Walk.Length; k++)
                if (baked.Walk[k]) cells.Add(new Vector3(baked.Origin.x + (k % baked.Width + .5f) * Cell, baked.FloorY, baked.Origin.y + (k / baked.Width + .5f) * Cell));
            for (float apart = .45f; apart > .05f; apart -= .1f, within += .5f)
            {
                var good = cells.Where(c => Flat(c - round).magnitude <= within && taken.All(t => Flat(t - c).magnitude >= apart)).ToList();
                if (good.Count > 0) return good[random.Next(good.Count)];
            }
            return baked.Home;
        }

        // ------------------------------------------------------------------ the motion sheet

        private const int Tile = 640, TileH = 400, Columns = 5;
        private static readonly float[] Moments = { .6f, 2.2f, 3.8f, 5.4f, 6.5f, 7.0f, 7.6f, 9.5f, 13f, 18f };

        /// <summary>Steps the birds by hand in edit mode and renders ten moments of each subject.
        /// The caller opens and reopens scenes; nothing is saved.</summary>
        internal static string Motion(int version)
        {
            EditorSceneManager.OpenScene(EskinitaAlleySceneBuilder.ScenePath, OpenSceneMode.Single);
            var chickens = Object.FindObjectsByType<AlleyChickens>(FindObjectsSortMode.None).FirstOrDefault();
            if (chickens == null || chickens.Groups.Length == 0) throw new InvalidOperationException("the built scene has no chickens (build it first)");
            chickens.Begin(20261009);
            Directory.CreateDirectory("Logs/eskinita");

            // Subjects: every group from the alley's middle, then a close look at the first bird of each.
            var subjects = new List<(string name, int group, int bird)>();
            for (int g = 0; g < chickens.Groups.Length; g++) subjects.Add((chickens.Groups[g].Name, g, -1));
            for (int g = 0; g < chickens.Groups.Length; g++) subjects.Add((chickens.Groups[g].Name + "_close", g, 0));
            if (chickens.Groups.Length > 1 && chickens.Groups[1].Birds.Length > 1) subjects.Add((chickens.Groups[1].Name + "_chick_close", 1, chickens.Groups[1].Birds.Length - 1));

            var camera = new GameObject("Chicken motion witness") { hideFlags = HideFlags.DontSave }.AddComponent<Camera>();
            camera.enabled = false; camera.nearClipPlane = .05f; camera.farClipPlane = 400;
            try { camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene(); camera.gameObject.AddComponent<WorldOutline>().PrototypeEnabled = true; }
            catch (Exception e) { Debug.LogWarning("[EskinitaAlley] chickens motion: no grade or outline on the witness: " + e.Message); }
            var bodies = new List<Transform>();
            foreach (var g in chickens.Groups)
            {
                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.hideFlags = HideFlags.DontSave; body.name = "stand-in body";
                Object.DestroyImmediate(body.GetComponent<Collider>());
                body.transform.localScale = new Vector3(.5f, .85f, .5f);
                body.SetActive(false);
                bodies.Add(body.transform);
            }
            int rows = Mathf.CeilToInt(Moments.Length / (float)Columns);
            var sheets = subjects.Select(_ => new Texture2D(Tile * Columns, TileH * rows, TextureFormat.RGB24, false)).ToList();
            var log = new StringBuilder();
            try
            {
                float time = 0f; const float dt = 1f / 30f;
                for (int m = 0; m < Moments.Length; m++)
                {
                    while (time < Moments[m] - 1e-4f)
                    {
                        time += dt;
                        // The stand-in body walks down the alley at each group's home from 4.4 m off, 6.0 s to 8.0 s, then is gone.
                        bool on = time >= 6f && time < 8f;
                        var threats = new Vector3[on ? chickens.Groups.Length : 0];
                        for (int g = 0; g < chickens.Groups.Length; g++)
                        {
                            var home = chickens.Groups[g].Home;
                            var along = new Vector3(0, 0, Mathf.Abs(home.z) > .5f ? -Mathf.Sign(home.z) : 1f);
                            var at = home + along * Mathf.Max(.7f, 4.4f - (time - 6f) * 2.6f) - new Vector3(Mathf.Sign(home.x) * .5f, 0, 0);
                            bodies[g].gameObject.SetActive(on);
                            bodies[g].position = at + Vector3.up * .85f;
                            if (on) threats[g] = at;
                        }
                        chickens.ProbeThreats = threats;
                        chickens.Step(dt);
                    }
                    log.Append($"t {Moments[m]:0.0}: ").AppendLine(chickens.Describe());
                    for (int s = 0; s < subjects.Count; s++)
                    {
                        var g = chickens.Groups[subjects[s].group];
                        var side = new Vector3(-Mathf.Sign(g.Home.x), 0, 0);
                        if (subjects[s].bird < 0)
                        {
                            camera.fieldOfView = 40f;
                            var eye = g.Home + side * 3.7f + Vector3.up * 1.9f;
                            camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(g.Home + Vector3.up * .2f - eye));
                        }
                        else
                        {
                            var bird = g.Birds[subjects[s].bird];
                            float scale = g.Size[subjects[s].bird] < .6f ? .45f : 1f;
                            camera.fieldOfView = 30f;
                            var look = bird.position + Vector3.up * .2f * scale;
                            var eye = look + (side * 1.25f + new Vector3(0, .32f, .35f)) * scale;
                            camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye));
                        }
                        var pixels = Shot(camera, Tile, TileH);
                        sheets[s].SetPixels32(m % Columns * Tile, (rows - 1 - m / Columns) * TileH, Tile, TileH, pixels);
                    }
                }
                var written = new List<string>();
                // One file per subject (large enough to read a pose), and all of them stacked in the one the brief names.
                var all = new Texture2D(Tile * Columns, TileH * rows * subjects.Count, TextureFormat.RGB24, false);
                for (int s = 0; s < subjects.Count; s++)
                {
                    sheets[s].Apply();
                    string path = $"Logs/eskinita/chickens_motion_v{version}_{subjects[s].name}.png";
                    File.WriteAllBytes(path, sheets[s].EncodeToPNG());
                    written.Add(path);
                    all.SetPixels32(0, (subjects.Count - 1 - s) * TileH * rows, Tile * Columns, TileH * rows, sheets[s].GetPixels32());
                }
                all.Apply();
                File.WriteAllBytes($"Logs/eskinita/chickens_motion_v{version}.png", all.EncodeToPNG());
                Object.DestroyImmediate(all);
                File.WriteAllText($"Logs/eskinita/chickens_motion_v{version}.txt",
                    "moments (s): " + string.Join(", ", Moments.Select(t => t.ToString("0.0"))) + "; a stand-in body walks at every group from 6.0 s to 8.0 s\n" +
                    "startles: " + chickens.Startles + "\n" + log);
                return $"chickens motion v{version}: {subjects.Count} subjects x {Moments.Length} moments, {chickens.Startles} startles; Logs/eskinita/chickens_motion_v{version}.png, .txt and " +
                       string.Join(", ", written.Select(Path.GetFileName));
            }
            finally
            {
                chickens.ProbeThreats = Array.Empty<Vector3>();
                foreach (var b in bodies) if (b != null) Object.DestroyImmediate(b.gameObject);
                foreach (var t in sheets) Object.DestroyImmediate(t);
                Object.DestroyImmediate(camera.gameObject);
            }
        }

        internal static Color32[] Shot(Camera camera, int width, int height)
        {
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 4 };
            rt.Create(); camera.targetTexture = rt; camera.Render();
            var previous = RenderTexture.active;
            var display = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            bool write = GL.sRGBWrite; GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
            Graphics.Blit(rt, display); GL.sRGBWrite = write; RenderTexture.active = display;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
            var pixels = image.GetPixels32();
            RenderTexture.active = previous; camera.targetTexture = null;
            RenderTexture.ReleaseTemporary(display); rt.Release();
            Object.DestroyImmediate(rt); Object.DestroyImmediate(image);
            return pixels;
        }
    }

    /// <summary>The shell's door to <see cref="EskinitaAlleyLifeAuthor.Motion"/>: see that class.
    /// It gives the editor back as it found it and refuses when a scene has unsaved changes, the
    /// way <see cref="EskinitaAlleyRequestWatcher"/> does, and it lets that watcher go first.</summary>
    [InitializeOnLoad]
    internal static class EskinitaAlleyChickensWatcher
    {
        private const string Request = "Temp/eskinita-chickens.request", Done = "Temp/eskinita-chickens.done", Alley = "Temp/eskinita-alley.request";
        private static double _nextPoll;

        static EskinitaAlleyChickensWatcher() { EditorApplication.update += Poll; }

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < _nextPoll) return;
            _nextPoll = EditorApplication.timeSinceStartup + 1.0;
            if (!File.Exists(Request) || File.Exists(Alley) || EskinitaAlleyPlayProbe.Busy) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            string result;
            try
            {
                string text = File.ReadAllText(Request).Trim();
                File.Delete(Request);
                if (File.Exists(Done)) File.Delete(Done);
                var number = text.Split(' ').FirstOrDefault(w => int.TryParse(w, out _));
                if (!int.TryParse(number, out int version)) { version = 1; while (File.Exists($"Logs/eskinita/chickens_motion_v{version}.png") || File.Exists($"Logs/eskinita/pets_motion_v{version}.txt")) version++; }
                var open = new List<string>();
                string active = SceneManager.GetActiveScene().path;
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene.isDirty) throw new InvalidOperationException("scene dirty (" + (string.IsNullOrEmpty(scene.name) ? "Untitled" : scene.name) + " has unsaved changes; nothing was run)");
                    if (scene.isLoaded && !string.IsNullOrEmpty(scene.path)) open.Add(scene.path);
                }
                // "N" renders both; "N chickens" or "N pets" only that one.
                try
                {
                    var said = new List<string>();
                    if (!text.Contains("pets")) said.Add(EskinitaAlleyLifeAuthor.Motion(version));
                    if (!text.Contains("chickens")) said.Add(EskinitaAlleyPetsAuthor.Motion(version));
                    result = "OK " + string.Join("; ", said);
                }
                finally { EskinitaAlleyRequestWatcher.Restore(open, active); }
            }
            catch (Exception e)
            {
                try { if (File.Exists(Request)) File.Delete(Request); } catch { /* answered below either way */ }
                result = "FAIL " + e;
            }
            try { File.WriteAllText(Done, result); }
            catch (Exception e) { Debug.LogError("[EskinitaAlley] could not write " + Done + ": " + e.Message); }
            if (result.StartsWith("OK")) Debug.Log("[EskinitaAlley] " + result); else Debug.LogError("[EskinitaAlley] " + result);
        }
    }
}
