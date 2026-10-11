using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
    /// STREET LIFE on the road outside the Eskinita Alley's arch (owner, 2026-10-09, looking over the
    /// low wall at a bare slab with one parked tricycle: "road and street life is missing. mostly the
    /// occasional tricycle, jeep, people walking around, taho vendors, children playing. not as
    /// bustling traffic as the city maps"). Called by `EskinitaAlleySceneBuilder.Build` after the
    /// dressing and the collision exist. Everything it makes is under one "Street" object.
    ///
    /// THE ROAD (tools/author_eskinita_alley.py draws it; Unity axes): it runs along X far past the
    /// map both ways. Asphalt z +18.9 to +26.1 at y -0.92, the centre line at z +22.5, a kerbed
    /// sidewalk each side (z +17.5 to +18.9 by the alley, broken by the arch's apron between x -5
    /// and +5; z +26.1 to +27.5 across the road) with its top at y -0.80. Heights are MEASURED here
    /// by casting at the placed ground model, with those numbers as the fallback.
    ///
    /// WHAT IS REUSED, AND WHAT IS NEW
    ///  * VEHICLES: the Ilalim rebuild's kit as it stands (Art/IlalimRebuild/Models/veh_*.glb, the
    ///    part files of a tricycle and the two jeepneys) wearing the Ilalim builder's own material
    ///    assets by name (Art/IlalimRebuild/Materials/veh_*.mat). The tricycle is taken without its
    ///    lettering part, which names an Ilalim street. They are driven by `AlleyStreet` (new,
    ///    Runtime/Map): a schedule in closed form from one clock, the Arena traffic's way, because
    ///    `KantoTraffic` is a model of a street that is always full (see `AlleyStreet`).
    ///  * PEOPLE: `SidewalkLife` itself, unchanged, configured for this road: the magtataho with
    ///    his pole and two buckets (its own shoulder carry), three children playing tag on the
    ///    alley's side of the road, and three passers-by. The rigs, palettes, hat and towel are the
    ///    Ilalim sidewalk's (`IlalimSidewalkAuthor`), from the roster book; the prop materials are
    ///    the Ilalim life's own assets (Art/IlalimRebuild/Life/sidewalk_*.mat).
    ///  * SOUND: the people carry `SidewalkLife`'s own voices (the taho call, giggles, footsteps:
    ///    existing clips). The vehicles' engine putter is `KantoStreetSound` itself with the
    ///    existing engine loops at a low gain, no bed, no horns and no sirens. ⚠️ It reads its
    ///    vehicles from a `KantoTraffic`, so the Street carries one that is DISABLED and is only a
    ///    list of the bodies `AlleyStreet` moves (a disabled KantoTraffic never runs Start or
    ///    Update, and reports every speed as 0, so each engine idles at its authored pitch). Set
    ///    <see cref="EngineSound"/> false to build the road silent.
    ///
    /// ⚠️⚠️ VISUAL ONLY, AND NEVER IN THE MAP. No collider on anything here, nothing networked that
    /// is not the shared clock, no NavMesh, nothing gameplay reads. Every walked line keeps to
    /// z +17.9 or more (the play bounds end at +17.5): nobody comes through the arch. One passer-by
    /// stops on the apron in the arch's mouth, looks in and walks on. The children play on the
    /// sidewalk and the apron and never set foot in a lane, so no vehicle ever meets one.
    /// People and vehicles appear and leave at x = +/-62 or further, and `SidewalkLife` only lets
    /// one appear or vanish while that spot is off the main camera's screen.
    ///
    /// THE MOTION SHEET: write a version number (or nothing; a second number is the first moment's
    /// time in the people's story, else the liveliest 40 s of the first 7 minutes is found) into
    /// `Temp/eskinita-street.request` with the editor open and out of Play. The built scene is
    /// opened, the people are stepped BY HAND in edit mode and the vehicles posed from the clock,
    /// ten moments over 40 s are rendered from a roof, from the arch both ways along the road,
    /// from straight above and close on a vehicle and on a person, into
    /// `Logs/eskinita/street_motion_vN_*.png` and `.txt`, the scenes that were open are reopened,
    /// and `Temp/eskinita-street.done` says what was written. Nothing is saved.
    /// </summary>
    internal static class EskinitaAlleyStreetAuthor
    {
        /// <summary>False builds the vehicles silent (no KantoTraffic body list, no KantoStreetSound).</summary>
        private const bool EngineSound = true;

        private const string VehicleModels = "Assets/TumbangPreso/Art/IlalimRebuild/Models";
        private const string VehicleMaterials = "Assets/TumbangPreso/Art/IlalimRebuild/Materials";
        private const string LifeMaterials = "Assets/TumbangPreso/Art/IlalimRebuild/Life";
        private const string Tag = "[EskinitaAlley] ";

        // The road, as the art script was told it (fallbacks; the heights are measured).
        private const float RoadY = -.92f, WalkY = -.80f, ApronY = -.90f;
        private const float NearKerb = 18.9f, FarKerb = 26.1f, Bounds = 17.5f;
        private const float ApronHalf = 5f;
        /// <summary>Lane 0 (toward +X) and lane 1 (toward -X). The near lane is 0.3 m out from its
        /// lane's middle, clear of whatever stands at the near kerb.</summary>
        private static readonly float[] LaneZ = { 21f, 24.2f };
        private const float RoadEnd = 66f, WalkEnd = 62f;
        /// <summary>The model's nose is along its local -X (the kits model it along Blender +X and
        /// the glTF import mirrors X), so a quarter turn brings it to +Z.</summary>
        private const float NoseYaw = 90f;

        private static readonly (string name, string group, string[] parts, float speed, float weight, float bob, float rate)[] Fleet =
        {
            // No "_letters": the kit's lettering is an Ilalim street's TODA.
            ("tricycle", "tricycle", new[] { "veh_tricycle_body", "veh_tricycle_glass", "veh_tricycle_round", "veh_tricycle_trim", "veh_tricycle_wheels" }, 5f, .74f, .006f, 9f),
            ("jeepney_green", "jeepney_green", new[] { "veh_jeepney_green_body", "veh_jeepney_green_glass", "veh_jeepney_green_livery", "veh_jeepney_green_round", "veh_jeepney_green_trim", "veh_jeepney_green_wheels" }, 6.3f, .13f, .004f, 6f),
            ("jeepney_taft", "jeepney_taft", new[] { "veh_jeepney_taft_body", "veh_jeepney_taft_glass", "veh_jeepney_taft_livery", "veh_jeepney_taft_round", "veh_jeepney_taft_trim", "veh_jeepney_taft_wheels" }, 6.3f, .13f, .004f, 6f),
        };

        /// <summary>Role, roster rig, scale, the garment slots rewritten (the Ilalim sidewalk's
        /// measured slots), and what is worn over the rig.</summary>
        private static readonly (string role, string id, float scale, (int slot, string hex)[] dress, int wear)[] Cast =
        {
            ("taho", "kuya_boy", 1f, new[] { (4, "e6dfcc"), (5, "6b6444") }, 0),
            ("kid", "totoy", .72f, new[] { (1, "e3c84a"), (5, "7a3b3b"), (2, "4f8a4a"), (11, "efe9dc") }, 0),
            ("kid", "bebang", .70f, new[] { (5, "e58fae"), (9, "6a3d52") }, 0),
            ("kid", "tikboy", .74f, new[] { (5, "8a3446"), (2, "3f6a3a") }, 0),
            // A neighbour on her way home (the Ilalim passer-by as she is).
            ("walker", "aling_nena", 1f, new[] { (5, "e3d6bb"), (11, "7a3446") }, 0),
            // A man out in the sun under a buri hat, a bimpo over his shoulder.
            ("walker", "bayan", 1f, new[] { (1, "ded8c4"), (9, "54503f") }, 1),
            // A student: a white blouse over a dark skirt.
            ("walker", "maring", 1f, new[] { (2, "f2efe4"), (5, "39414f"), (7, "4a4038") }, 0),
        };

        private static Color Hex(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }

        public static string Build(Transform root, List<MeshCollider> colliders, Action<string> warn)
        {
            var said = new List<string>();
            var street = new GameObject("Street").transform;
            street.SetParent(root, false);
            var dressing = root.Find("Dressing");
            var probes = new List<GameObject>();
            try
            {
                var ground = GroundProbes(dressing, colliders, probes);
                float Height(float x, float z, float fallback)
                {
                    // The highest thing under the point that is a road or a sidewalk (never a roof, an awning or a tree).
                    float best = float.NegativeInfinity;
                    var ray = new Ray(new Vector3(x, RoadY + 1.3f, z), Vector3.down);
                    foreach (var c in ground)
                        if (c != null && c.Raycast(ray, out var hit, 2f) && hit.point.y > best && hit.point.y < RoadY + .45f) best = hit.point.y;
                    return float.IsNegativeInfinity(best) ? fallback : best;
                }
                Vector3 OnGround(float x, float z)
                {
                    bool road = z > NearKerb && z < FarKerb;
                    bool apron = z <= NearKerb && Mathf.Abs(x) < ApronHalf + .25f;
                    return new Vector3(x, Height(x, z, road ? RoadY : apron ? ApronY : WalkY), z);
                }

                said.Add(Vehicles(street, OnGround, warn));
                said.Add(People(street, OnGround, warn));
                said.Add(Clear(street, dressing, warn));
            }
            finally
            {
                foreach (var p in probes) if (p != null) Object.DestroyImmediate(p);
            }
            string summary = "street: " + string.Join("; ", said);
            Debug.Log(Tag + summary);
            try { Directory.CreateDirectory("Logs/eskinita"); File.WriteAllText("Logs/eskinita/street_build.txt", summary.Replace("; ", "\n") + "\n"); }
            catch (Exception e) { Debug.LogWarning(Tag + "street: could not write Logs/eskinita/street_build.txt: " + e.Message); }
            return summary;
        }

        /// <summary>The map's collision, and a collider (never saved: destroyed by the caller) on
        /// every mesh of the placed ground model, which is where the road and the sidewalks are.</summary>
        private static List<Collider> GroundProbes(Transform dressing, List<MeshCollider> colliders, List<GameObject> probes)
        {
            var result = new List<Collider>();
            if (colliders != null) result.AddRange(colliders.Where(c => c != null));
            if (dressing == null) return result;
            foreach (Transform group in dressing)
            {
                if (group.name.IndexOf("ground", StringComparison.OrdinalIgnoreCase) < 0 && group.name.IndexOf("steps", StringComparison.OrdinalIgnoreCase) < 0) continue;
                foreach (var filter in group.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter.sharedMesh == null) continue;
                    var go = new GameObject("street ground probe") { hideFlags = HideFlags.DontSave };
                    go.transform.SetPositionAndRotation(filter.transform.position, filter.transform.rotation);
                    go.transform.localScale = filter.transform.lossyScale;
                    var c = go.AddComponent<MeshCollider>();
                    c.convex = false; c.sharedMesh = filter.sharedMesh;
                    probes.Add(go); result.Add(c);
                }
            }
            Physics.SyncTransforms();
            return result;
        }

        // ------------------------------------------------------------------ vehicles

        private static string Vehicles(Transform street, Func<float, float, Vector3> onGround, Action<string> warn)
        {
            var holder = new GameObject("Vehicles").transform;
            holder.SetParent(street, false);
            var road = holder.gameObject.AddComponent<AlleyStreet>();
            road.End = RoadEnd;
            road.LaneZ = (float[])LaneZ.Clone();
            // The lane's own height, measured at the arch and at both ends (a road is flat; the lowest is the asphalt).
            road.LaneY = LaneZ.Select(z => new[] { -30f, 0f, 30f }.Min(x => onGround(x, z).y)).ToArray();
            var kinds = new List<AlleyStreet.Kind>();
            var missing = new HashSet<string>();
            int bodies = 0, renderers = 0;
            foreach (var spec in Fleet)
            {
                var prefabs = new List<GameObject>();
                foreach (string part in spec.parts)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{VehicleModels}/{part}.glb");
                    if (prefab == null) warn($"street: missing vehicle part {VehicleModels}/{part}.glb"); else prefabs.Add(prefab);
                }
                if (prefabs.Count == 0) { warn($"street: no parts for the {spec.name}; it is left out"); continue; }
                // The group's name is what the engine sound reads the kind from ("tricycle", "jeepney").
                var group = new GameObject(spec.group).transform;
                group.SetParent(holder, false);
                var kind = new AlleyStreet.Kind
                {
                    Name = spec.name, Speed = spec.speed, Weight = spec.weight, Bob = spec.bob, BobRate = spec.rate,
                    ModelOffset = Quaternion.Euler(0f, NoseYaw, 0f), Bodies = new Transform[4],
                };
                for (int i = 0; i < 4; i++)
                {
                    var body = new GameObject($"{spec.name} {(i < 2 ? "east" : "west")} {i % 2}").transform;
                    body.SetParent(group, false);
                    foreach (var prefab in prefabs)
                    {
                        var part = (GameObject)PrefabUtility.InstantiatePrefab(prefab, body);
                        part.transform.localPosition = Vector3.zero; part.transform.localRotation = Quaternion.identity;
                        foreach (var r in part.GetComponentsInChildren<Renderer>(true))
                        {
                            var mats = r.sharedMaterials;
                            for (int m = 0; m < mats.Length; m++)
                            {
                                if (mats[m] == null) continue;
                                string key = mats[m].name.Replace(" (Instance)", "").Trim();
                                var built = AssetDatabase.LoadAssetAtPath<Material>($"{VehicleMaterials}/{key}.mat");
                                if (built != null) mats[m] = built; else missing.Add(key);
                            }
                            r.sharedMaterials = mats;
                            r.shadowCastingMode = ShadowCastingMode.On;
                            renderers++;
                        }
                        // ⚠️ Never a collider, never static: nothing a body, a slipper or the can could touch, and it moves.
                        foreach (var c in part.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
                        foreach (var t in part.GetComponentsInChildren<Transform>(true)) t.gameObject.isStatic = false;
                    }
                    // Put away until the schedule sends it (AlleyStreet shows and parks them).
                    body.position = new Vector3(0f, -500f, 0f);
                    body.gameObject.SetActive(false);
                    kind.Bodies[i] = body;
                    bodies++;
                }
                kinds.Add(kind);
            }
            road.Kinds = kinds.ToArray();
            if (missing.Count > 0) warn("street: no built material in " + VehicleMaterials + " for " + string.Join(", ", missing.OrderBy(n => n)) + " (the Ilalim builder writes them); those slots keep the importer's material");
            EditorUtility.SetDirty(road);

            string sound = "vehicles silent";
            if (EngineSound && kinds.Count > 0)
            {
                // KantoStreetSound, engines only. Its vehicle list is a KantoTraffic that never runs (see the class note).
                var ears = new GameObject("Engine sound (KantoStreetSound; the KantoTraffic is a DISABLED body list)");
                ears.transform.SetParent(holder, false);
                var list = ears.AddComponent<KantoTraffic>();
                list.enabled = false;
                list.Road = (LaneZ[0] + LaneZ[1]) * .5f; list.Extent = RoadEnd; list.GroundY = road.LaneY[0];
                list.Drivers = kinds.SelectMany(k => k.Bodies.Select(b => new KantoTraffic.Driver { Body = b, Lane = 0, Along = 0f, Cruise = k.Speed, Length = 3f, ModelOffset = k.ModelOffset })).ToArray();
                var engines = ears.AddComponent<KantoStreetSound>();
                engines.Traffic = list;
                engines.CityBed = null;
                engines.EngineCar = KantoTrafficAuthor.Clip("kanto_engine_car", true);
                engines.EngineDiesel = KantoTrafficAuthor.Clip("kanto_engine_diesel", true);
                engines.EngineTricycle = KantoTrafficAuthor.Clip("kanto_engine_tricycle", true);
                engines.HornsCar = new AudioClip[0]; engines.HornsJeepney = new AudioClip[0]; engines.HornsTricycle = new AudioClip[0];
                engines.HornBus = null; engines.HornTruck = null; engines.Sirens = new AudioClip[0];
                engines.BedGain = 0f; engines.HornGain = 0f; engines.SirenGain = 0f;
                // Kanto's bustle plays its engines at 0.5; a quiet road's putter is well under that.
                engines.EngineGain = .3f; engines.EngineVoices = 3;
                sound = $"engine putter through KantoStreetSound at gain {engines.EngineGain:0.00} (tricycle loop {(engines.EngineTricycle != null)}, diesel loop {(engines.EngineDiesel != null)}), no horns";
            }
            float mean = road.Slot / Mathf.Max(.01f, road.Chance);
            return $"{kinds.Count} vehicle kinds ({string.Join(", ", kinds.Select(k => $"{k.Name} {k.Speed:0.#} m/s x{k.Weight:0.##}"))}), {bodies} bodies ({renderers} renderers, all inactive until sent), " +
                   $"lanes z {LaneZ[0]:0.0} east and {LaneZ[1]:0.0} west at y {road.LaneY[0]:0.00}/{road.LaneY[1]:0.00}, from x -{RoadEnd:0} to +{RoadEnd:0}; a lane sends one about every {mean:0} s (slot {road.Slot:0} s, chance {road.Chance:0.00}); {sound}";
        }

        // ------------------------------------------------------------------ people

        private static string People(Transform street, Func<float, float, Vector3> onGround, Action<string> warn)
        {
            var go = new GameObject("People");
            go.transform.SetParent(street, false);
            var life = go.AddComponent<SidewalkLife>();
            var book = RosterBook.Load();
            if (book == null) warn("street: no roster book (RosterBook.Load); the road has no people");
            var looks = new List<(string role, SidewalkLife.Look look)>();
            foreach (var (role, id, scale, dress, wear) in Cast)
            {
                var art = book != null ? book.FindPersonArt(id) : null;
                if (art == null || art.Model == null) { warn("street: no roster art for " + id + "; that person is left out"); continue; }
                var palette = (art.Palette != null && art.Palette.Length == 16 ? art.Palette : new Color[16]).ToArray();
                foreach (var (slot, hex) in dress) palette[slot] = Hex(hex);
                var look = new SidewalkLife.Look { Name = $"{role} ({id})", Art = art, Palette = palette, Scale = scale };
                if (wear == 1)
                    look.Wear = new SidewalkLife.Wear
                    {
                        // Bayan's hair top, measured by the Ilalim sidewalk author.
                        Hat = SidewalkLife.HatKind.StrawHat, HairTop = .671f, HatColour = Hex("cdb57c"), HatTrim = Hex("5e4a32"),
                        Towel = true, TowelColour = Hex("e9e3d2"), TowelStripe = Hex("7d8c6a"),
                    };
                looks.Add((role, look));
            }
            life.Taho = looks.FirstOrDefault(l => l.role == "taho").look;
            life.Beggar = null;
            life.Kids = looks.Where(l => l.role == "kid").Select(l => l.look).ToArray();
            life.Spectators = looks.Where(l => l.role == "walker").Select(l => l.look).ToArray();
            life.TahoCarry = SidewalkLife.TahoCarryStyle.Shoulder;

            Material Mat(string name)
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>($"{LifeMaterials}/{name}.mat");
                if (m == null) warn($"street: missing {LifeMaterials}/{name}.mat (the Ilalim sidewalk author writes it)");
                return m;
            }
            life.Bamboo = Mat("sidewalk_bamboo"); life.Aluminium = Mat("sidewalk_aluminium"); life.Lid = Mat("sidewalk_lid");
            life.Rope = Mat("sidewalk_rope"); life.Cardboard = Mat("sidewalk_carton");

            // The Ilalim life's own clips. No bed and no animals here: the alley's soundscape is somebody else's.
            life.TahoCall = KantoTrafficAuthor.Clips("sfx_taho_call_");
            life.KidGiggle = KantoTrafficAuthor.Clips("sfx_life_kid_giggle_");
            life.KidTaya = KantoTrafficAuthor.Clips("sfx_life_kid_taya_");
            life.Cheer = KantoTrafficAuthor.Clips("sfx_life_cheer_");
            life.Clap = KantoTrafficAuthor.Clips("sfx_life_clap_");
            life.Groan = KantoTrafficAuthor.Clips("sfx_life_groan_");
            life.Bucket = KantoTrafficAuthor.Clips("sfx_life_bucket_");
            life.Footstep = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/TumbangPreso/Resources/Sfx/step_rubber.wav");
            life.Pigeons = null;
            // The Ilalim street lifts its people over a city (1.8); this road is quiet, so they sit lower.
            life.Loudness = 1.2f;

            // ---- where. Each line's FIRST point is where its person appears and leaves, far up the road.
            // The alley's side, east of the arch: 0.5 m out from the wall so a head clears it both ways.
            const float nearZ = 18.4f, farZ = 26.8f, kidZ = 18.35f;
            Vector3[] Line(params (float x, float z)[] points) => points.Select(p => onGround(p.x, p.z)).ToArray();
            // ⚠️ Two of these people do not fit side by side on a 1.4 m sidewalk (a head is a metre wide), and a walker
            // WAITS behind anybody standing on his line: the first sheet had the student stood in the vendor's buckets
            // for as long as he called. So the vendor's last steps take him in against the wall, and the passer-by's
            // line runs along the kerb: she walks past him while he calls.
            const float kerbZ = 18.55f, wallZ = 18f;
            var taho = Line((WalkEnd, nearZ), (30f, nearZ), (12f, nearZ), (8.6f, nearZ), (6.8f, wallZ));
            var arch = Line((WalkEnd + 2f, kerbZ), (30f, kerbZ), (12f, kerbZ), (ApronHalf + .6f, kerbZ), (ApronHalf - .1f, kerbZ), (2.8f, 18.3f));
            var far = Line((-WalkEnd, farZ), (-30f, farZ), (0f, farZ), (30f, farZ), (WalkEnd, farZ));
            life.Walks = new[]
            {
                new SidewalkLife.Walk { Name = "the alley's side, east", Points = taho },
                new SidewalkLife.Walk { Name = "to the arch's mouth", Points = arch },
                new SidewalkLife.Walk { Name = "the far sidewalk", Points = far },
            };
            life.TahoWalk = 0; life.BeggarWalk = -1;
            life.Watches = new[]
            {
                // He stops on the apron and looks in up the alley (toward the can's end), then walks back the way he came.
                new SidewalkLife.Watch { Name = "the arch", Walk = 1, LookAt = new Vector3(0f, .3f, 4f) },
                // The whole road, end to end: she waits a moment out of sight at the far end and comes back.
                new SidewalkLife.Watch { Name = "the far end of the road", Walk = 2, LookAt = new Vector3(WalkEnd + 30f, 0f, farZ) },
            };
            // The children: tag along the alley's side west of the arch and across its apron. The LAST point is
            // where they come from and run home to.
            life.KidTrack = Line((1.6f, kidZ), (-(ApronHalf - .1f), kidZ), (-(ApronHalf + .6f), kidZ), (-12f, kidZ), (-21f, kidZ));
            life.KidHalfWidth = .45f;

            // ---- when: a quiet road. Somebody is on it most of the time, never a crowd.
            life.Seed = 1009;
            life.TahoFirst = new Vector2(10f, 30f); life.TahoAway = new Vector2(70f, 140f);
            life.TahoStopEvery = new Vector2(9f, 16f); life.TahoStop = new Vector2(3f, 5.5f);
            life.SpectatorFirst = new Vector2(3f, 20f); life.SpectatorAway = new Vector2(15f, 50f); life.SpectatorWatch = new Vector2(6f, 14f);
            life.KidsFirst = new Vector2(8f, 20f); life.KidsPlay = new Vector2(60f, 110f); life.KidsAway = new Vector2(40f, 90f);
            EditorUtility.SetDirty(life);

            // Nobody in the map, nobody in a lane: every line, sampled, with the room each walker takes either side of it.
            int inMap = 0, inLane = 0;
            void Check(Vector3[] line, float half)
            {
                for (int k = 1; k < line.Length; k++)
                    for (float t = 0f; t <= 1f; t += .05f)
                    {
                        var p = Vector3.Lerp(line[k - 1], line[k], t);
                        if (p.z - half < Bounds + .1f) inMap++;
                        if (p.z + half > NearKerb && p.z - half < FarKerb) inLane++;
                    }
            }
            Check(taho, .35f); Check(arch, .35f); Check(far, .35f); Check(life.KidTrack, life.KidHalfWidth);
            if (inMap > 0) warn($"street: {inMap} samples of the people's lines are inside the play bounds (z under {Bounds + .1f:0.0})");
            if (inLane > 0) warn($"street: {inLane} samples of the people's lines are on the carriageway");
            return $"{looks.Count} people (the magtataho {(life.Taho != null ? "kuya_boy" : "MISSING")}, {life.Kids.Length} children, {life.Spectators.Length} passers-by) on SidewalkLife: " +
                   $"taho line {Length(taho):0} m, arch line {Length(arch):0} m, far sidewalk {Length(far):0} m, children's run {Length(life.KidTrack):0} m; " +
                   $"heights sidewalk {taho[1].y:0.00}, apron {arch[arch.Length - 1].y:0.00}, far sidewalk {far[2].y:0.00}; " +
                   $"sound: call {life.TahoCall.Length}, giggle {life.KidGiggle.Length}, taya {life.KidTaya.Length}, footstep {(life.Footstep != null)}";
        }

        private static float Length(Vector3[] p)
        {
            float s = 0f;
            for (int k = 1; k < p.Length; k++) s += Vector3.Distance(p[k - 1], p[k]);
            return s;
        }

        /// <summary>What stands in the way: every placed prop or tree whose foot is in a lane's swept
        /// width or on a walked line is named, so the art side can move it (nothing here steers round it).</summary>
        private static string Clear(Transform street, Transform dressing, Action<string> warn)
        {
            if (dressing == null) return "no dressing to check";
            var life = street.GetComponentInChildren<SidewalkLife>();
            var lines = new List<(Vector3[] points, float half, string name)>();
            if (life != null)
            {
                foreach (var w in life.Walks) lines.Add((w.Points, .8f, w.Name));
                lines.Add((life.KidTrack, life.KidHalfWidth + .45f, "the children's run"));
            }
            var hits = new List<string>();
            foreach (Transform group in dressing)
            {
                bool tree = group.name.StartsWith("tree_"), prop = group.name.StartsWith("prop_");
                if (!tree && !prop) continue;
                if (group.name == "prop_gate_arch" || group.name.StartsWith("prop_banderitas") || group.name.StartsWith("prop_laundry")) continue;
                foreach (Transform instance in group)
                {
                    var at = instance.position;
                    if (at.z < Bounds - .5f || at.z > 28.5f || at.y > .6f) continue;
                    // A tree's bounds are its crown; its trunk is at its origin. A prop is its bounds.
                    float x0 = at.x - .35f, x1 = at.x + .35f, z0 = at.z - .35f, z1 = at.z + .35f;
                    if (prop)
                    {
                        var rs = instance.GetComponentsInChildren<Renderer>();
                        if (rs.Length == 0) continue;
                        var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                        x0 = b.min.x; x1 = b.max.x; z0 = b.min.z; z1 = b.max.z;
                    }
                    string where = null;
                    for (int lane = 0; lane < LaneZ.Length && where == null; lane++)
                        if (z1 > LaneZ[lane] - 1.15f && z0 < LaneZ[lane] + 1.15f && x1 > -RoadEnd && x0 < RoadEnd) where = lane == 0 ? "the near lane" : "the far lane";
                    foreach (var (points, half, name) in lines)
                    {
                        if (where != null) break;
                        for (int k = 1; k < points.Length && where == null; k++)
                        {
                            float ax = Mathf.Min(points[k - 1].x, points[k].x), bx = Mathf.Max(points[k - 1].x, points[k].x);
                            float az = Mathf.Min(points[k - 1].z, points[k].z) - half, bz = Mathf.Max(points[k - 1].z, points[k].z) + half;
                            if (x1 > ax && x0 < bx && z1 > az && z0 < bz) where = name;
                        }
                    }
                    if (where != null) hits.Add($"{group.name} at ({at.x:0.0}, {at.z:0.0}) in {where}");
                }
            }
            if (hits.Count > 0) warn("street: these stand where the road's life passes (nothing steers round them; move them or tell the street author): " + string.Join("; ", hits));
            return hits.Count == 0 ? "lanes and walked lines are clear of placed props" : hits.Count + " placed prop(s) in the way (see the warning)";
        }

        // ------------------------------------------------------------------ the motion sheet

        private const int Tile = 640, TileH = 400, Columns = 5;
        private static readonly float[] Moments = { 4f, 8f, 12f, 16f, 20f, 24f, 28f, 32f, 36f, 40f };
        private const float ScanStep = 1f / 15f, ScanSeconds = 420f;

        /// <summary>How much there is to look at near the arch: people shown within 16 m of it
        /// along the road, the magtataho and the children counting double.</summary>
        private static float Lively(SidewalkLife life)
        {
            float score = 0f;
            for (int i = 0; i < life.PeopleCount; i++)
            {
                if (!life.PersonShown(i)) continue;
                var p = life.PersonPosition(i);
                if (Mathf.Abs(p.x) > 16f) continue;
                score += life.PersonRole(i) == "spectator" ? 1f : 2f;
            }
            return score;
        }

        private static SidewalkLife OpenAndFind(out AlleyStreet road)
        {
            EditorSceneManager.OpenScene(EskinitaAlleySceneBuilder.ScenePath, OpenSceneMode.Single);
            road = Object.FindObjectsByType<AlleyStreet>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
            var life = road != null && road.transform.parent != null ? road.transform.parent.GetComponentInChildren<SidewalkLife>(true) : null;
            if (road == null || life == null) throw new InvalidOperationException("the built scene has no Street (build it first)");
            return life;
        }

        /// <summary>Steps the people by hand in edit mode, poses the vehicles from the clock and
        /// renders ten moments of each subject. The caller reopens scenes; nothing is saved.</summary>
        internal static string Motion(int version, float startAt)
        {
            var life = OpenAndFind(out var road);
            // The people's story: where to start looking.
            if (startAt < 0f)
            {
                var scores = new List<float>();
                float t = 0f, acc = 0f; int inSecond = 0;
                while (t < ScanSeconds)
                {
                    life.Simulate(ScanStep); t += ScanStep;
                    acc += Lively(life); inSecond++;
                    if (inSecond == 15) { scores.Add(acc / 15f); acc = 0f; inSecond = 0; }
                }
                float best = -1f; int bestAt = 0;
                for (int s = 0; s + 40 <= scores.Count; s++)
                {
                    float sum = 0f; for (int k = 0; k < 40; k++) sum += scores[s + k];
                    if (sum > best) { best = sum; bestAt = s; }
                }
                startAt = bestAt;
                // A fresh story, walked to that second in the same steps.
                life = OpenAndFind(out road);
            }
            for (float t = 0f; t < startAt - 1e-4f; t += ScanStep) life.Simulate(ScanStep);
            // The vehicles: a clock on which something passes the arch about half way through the 40 s.
            // A jeepney for choice (the rarer sight, and the bigger thing to judge against the arch); else whatever passes.
            double pass = -1.0;
            for (long slot = 40; slot < 4000 && pass < 0; slot++)
                for (int lane = 0; lane < 2 && pass < 0; lane++)
                    if (road.Sends(lane, slot, out int kind, out float after) && road.Kinds[kind].Name.StartsWith("jeepney"))
                        pass = slot * (double)road.Slot + after - lane * road.Slot * .5 + road.End / road.Kinds[kind].Speed;
            if (pass < 0) pass = road.NextPassing(1000.0, 0f, 3f, 3000f);
            double clock0 = pass < 0 ? 1000.0 : pass - 20.0;

            Directory.CreateDirectory("Logs/eskinita");
            var subjects = new[] { "roof", "arch_west", "arch_east", "above", "vehicle", "person" };
            var camera = new GameObject("Street motion witness") { hideFlags = HideFlags.DontSave }.AddComponent<Camera>();
            camera.enabled = false; camera.nearClipPlane = .05f; camera.farClipPlane = 600;
            try { camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene(); camera.gameObject.AddComponent<WorldOutline>().PrototypeEnabled = true; }
            catch (Exception e) { Debug.LogWarning(Tag + "street motion: no grade or outline on the witness: " + e.Message); }
            WorldLookPresentation look = null;
            try
            {
                var sun = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);
                var sceneRoot = GameObject.Find(EskinitaAlleySceneBuilder.MapName);
                camera.gameObject.AddComponent<WorldLookCamera>();
                if (sceneRoot != null) look = WorldLookPresentation.InstallPreview(sceneRoot.transform, 0f, sun);
            }
            catch (Exception e) { Debug.LogWarning(Tag + "street motion: the world look preview failed: " + e.Message); }

            int rows = Mathf.CeilToInt(Moments.Length / (float)Columns);
            var sheets = subjects.Select(_ => new Texture2D(Tile * Columns, TileH * rows, TextureFormat.RGB24, false)).ToList();
            var log = new StringBuilder();
            var vehicleEye = new Vector3(0f, 1f, 30f); var vehicleLook = new Vector3(0f, 0f, 22f);
            var personEye = new Vector3(4f, .6f, 21f); var personLook = new Vector3(4f, 0f, 18.3f);
            try
            {
                float time = 0f; const float dt = 1f / 30f;
                for (int m = 0; m < Moments.Length; m++)
                {
                    while (time < Moments[m] - 1e-4f) { time += dt; life.Simulate(dt); }
                    road.Pose(clock0 + time);
                    log.Append($"t {Moments[m]:0}: road: ").Append(road.Describe()).Append(" people: ");
                    int nearest = -1; float nearestD = float.MaxValue;
                    for (int i = 0; i < life.PeopleCount; i++)
                    {
                        if (!life.PersonShown(i)) continue;
                        var p = life.PersonPosition(i);
                        log.Append($"{life.PersonName(i)} [{life.PersonState(i)}] ({p.x:0.0}, {p.y:0.00}, {p.z:0.0}) {life.PersonSpeed(i):0.0} m/s; ");
                        float d = Mathf.Abs(p.x) + (life.PersonRole(i) == "taho" ? -6f : 0f);
                        if (d < nearestD) { nearestD = d; nearest = i; }
                    }
                    log.AppendLine();
                    // The close looks follow whatever is nearest the arch; with nothing there they keep their last place.
                    var body = road.Nearest(new Vector3(0f, RoadY, 22.5f));
                    if (body != null)
                    {
                        vehicleLook = body.position + Vector3.up * .9f;
                        // From the kerb it is not driving by, a little ahead of it, so its nose and its side both show.
                        float side = body.position.z < 22.5f ? -1f : 1f;
                        float ahead = body.position.z < 22.5f ? 1f : -1f;
                        vehicleEye = body.position + new Vector3(ahead * 4.5f, 1.5f, -side * 6.5f);
                    }
                    if (nearest >= 0)
                    {
                        var p = life.PersonPosition(nearest);
                        personLook = p + Vector3.up * .85f;
                        personEye = p + new Vector3(1.6f, 1f, 4.4f);
                    }
                    for (int s = 0; s < subjects.Length; s++)
                    {
                        Vector3 eye, at; float fov = 55f; var up = Vector3.up;
                        switch (subjects[s])
                        {
                            case "roof": eye = new Vector3(9f, 4.2f, 13f); at = new Vector3(0f, -.9f, 22f); fov = 60f; break;
                            case "arch_west": eye = new Vector3(3.4f, .75f, 17.9f); at = new Vector3(-30f, -.2f, 22.4f); fov = 62f; break;
                            case "arch_east": eye = new Vector3(-3.4f, .75f, 17.9f); at = new Vector3(30f, -.2f, 22.4f); fov = 62f; break;
                            case "above": eye = new Vector3(0f, 34f, 22.6f); at = new Vector3(0f, -.9f, 22.5f); fov = 50f; up = Vector3.forward; break;
                            case "vehicle": eye = vehicleEye; at = vehicleLook; fov = 38f; break;
                            default: eye = personEye; at = personLook; fov = 32f; break;
                        }
                        camera.fieldOfView = fov;
                        camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(at - eye, up));
                        sheets[s].SetPixels32(m % Columns * Tile, (rows - 1 - m / Columns) * TileH, Tile, TileH, Shot(camera));
                    }
                }
                var written = new List<string>();
                for (int s = 0; s < subjects.Length; s++)
                {
                    sheets[s].Apply();
                    string path = $"Logs/eskinita/street_motion_v{version}_{subjects[s]}.png";
                    File.WriteAllBytes(path, sheets[s].EncodeToPNG());
                    written.Add(Path.GetFileName(path));
                }
                File.WriteAllText($"Logs/eskinita/street_motion_v{version}.txt",
                    "moments (s): " + string.Join(", ", Moments.Select(t => t.ToString("0"))) + $"; the people's story from its second {startAt:0}, the road's clock from {clock0:0}\n" + log);
                return $"street motion v{version}: {subjects.Length} subjects x {Moments.Length} moments, people from {startAt:0} s, road clock {clock0:0}; Logs/eskinita/street_motion_v{version}.txt and " + string.Join(", ", written);
            }
            finally
            {
                if (look != null) Object.DestroyImmediate(look.gameObject);
                foreach (var t in sheets) Object.DestroyImmediate(t);
                Object.DestroyImmediate(camera.gameObject);
            }
        }

        private static Color32[] Shot(Camera camera)
        {
            var rt = new RenderTexture(Tile, TileH, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 4 };
            rt.Create(); camera.targetTexture = rt; camera.Render();
            var previous = RenderTexture.active;
            var display = RenderTexture.GetTemporary(Tile, TileH, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            bool write = GL.sRGBWrite; GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
            Graphics.Blit(rt, display); GL.sRGBWrite = write; RenderTexture.active = display;
            var image = new Texture2D(Tile, TileH, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, Tile, TileH), 0, 0); image.Apply();
            var pixels = image.GetPixels32();
            RenderTexture.active = previous; camera.targetTexture = null;
            RenderTexture.ReleaseTemporary(display); rt.Release();
            Object.DestroyImmediate(rt); Object.DestroyImmediate(image);
            return pixels;
        }
    }

    /// <summary>The shell's door to <see cref="EskinitaAlleyStreetAuthor.Motion"/>: see that class.
    /// It gives the editor back as it found it and refuses when a scene has unsaved changes, the
    /// way <see cref="EskinitaAlleyRequestWatcher"/> does, and it lets that watcher and the
    /// chickens' go first.</summary>
    [InitializeOnLoad]
    internal static class EskinitaAlleyStreetWatcher
    {
        private const string Request = "Temp/eskinita-street.request", Done = "Temp/eskinita-street.done";
        private const string Alley = "Temp/eskinita-alley.request", Chickens = "Temp/eskinita-chickens.request";
        private static double _nextPoll;

        static EskinitaAlleyStreetWatcher() { EditorApplication.update += Poll; }

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < _nextPoll) return;
            _nextPoll = EditorApplication.timeSinceStartup + 1.0;
            if (!File.Exists(Request) || File.Exists(Alley) || File.Exists(Chickens) || EskinitaAlleyPlayProbe.Busy) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            string result;
            try
            {
                var words = File.ReadAllText(Request).Split(new[] { ' ', '\t', '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries);
                File.Delete(Request);
                if (File.Exists(Done)) File.Delete(Done);
                int version = 0;
                if (words.Length == 0 || !int.TryParse(words[0], out version) || version < 1)
                { version = 1; while (File.Exists($"Logs/eskinita/street_motion_v{version}.txt")) version++; }
                float startAt = -1f;
                if (words.Length > 1 && float.TryParse(words[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float given)) startAt = Mathf.Max(0f, given);
                var open = new List<string>();
                string active = SceneManager.GetActiveScene().path;
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene.isDirty) throw new InvalidOperationException("scene dirty (" + (string.IsNullOrEmpty(scene.name) ? "Untitled" : scene.name) + " has unsaved changes; nothing was run)");
                    if (scene.isLoaded && !string.IsNullOrEmpty(scene.path)) open.Add(scene.path);
                }
                try { result = "OK " + EskinitaAlleyStreetAuthor.Motion(version, startAt); }
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
