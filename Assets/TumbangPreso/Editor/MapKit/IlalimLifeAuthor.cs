using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// THE ILALIM REBUILD'S LIFE (ILALIM-1.4, owner 2026-09-30: "make the live moving cars and
    /// pigeons + the sfx"). Called by <see cref="IlalimSceneBuilder.Build"/> after the dressing, the
    /// gameplay colliders and the props, so a rebuild reproduces all of it. Everything here is
    /// cosmetic and local, never networked, never a collider a player, the lata or a tsinelas can
    /// touch (docs/ILALIM_REWORK_GUIDE.md § 3, "Moving things").
    ///
    /// TRAFFIC is Kanto's <see cref="KantoTraffic"/> on its ROUTES network (§ ROUTES there): the
    /// court IS Taft Avenue between the walls at |z| 16.5, so no car ever enters |z| &lt; 19.
    /// Taft is a closed road for the street game (docs/Ilalim_Ng_Tulay.md § 4.4: cars stay outside
    /// |z| 16.5), and the traffic behaves the way traffic at a closure does:
    ///   * NORTH, the Padre Faura junction (signals, Taft against Padre Faura): Padre Faura runs
    ///     one-way WEST (its ONE WAY arrows), one lane through the parked cars of its east arm,
    ///     splitting at the junction into the through lane (N1, the south lane of the west arm)
    ///     and a right turn north up Taft (N3). Taft's southbound traffic from UN Avenue turns
    ///     right into Padre Faura's west arm before the closure (N4). The Taft stretch between the
    ///     junction and the court stays empty, as a closed block does.
    ///   * SOUTH: Taft's northbound traffic from Pedro Gil turns right into G. Apacible, 110 m
    ///     out (S1); the two cars placed nearer the court wait at the closure with their engines
    ///     running (S0, a stop line that never turns green: they queue, idle and honk); the bus
    ///     placed southbound drives away south, U-turns past the median's end and joins S1 (S2).
    ///   * Lanes are the street kit's: Taft x = +/-1.9 (between the median and the pier collars),
    ///     Padre Faura's lanes 3 m apart about its OSM centre line, G. Apacible's eastbound lane
    ///     1.5 m right of its centre. Every route was checked against the placed art's bounds
    ///     (vehicles, piers, collars, bents, poles, trees, parked cars) before it was written.
    ///   * The eight placed Traffic vehicles become drivers where they stand (keeping their
    ///     placed rotation relative to their route, as KantoTrafficAuthor does); thirteen copies of
    ///     them fill the routes. The parked cars (StreetLife) stay parked.
    /// SOUND is Kanto's <see cref="KantoStreetSound"/> on the traffic with Kanto's clips (the city
    /// bed, car, diesel and tricycle engines, horns, sirens), unchanged files.
    ///
    /// PIGEONS are Kanto's flock (<see cref="LagoonFlocks"/> with fauna_pigeon, KantoPigeonsAuthor's
    /// tuning), landing on PERCH LINES measured here against the real meshes: the guideway's
    /// parapet copings, the flat roofs along the street, the station roofs' near ends, and the
    /// outer edge of the pavements (outside the chalk box, clear of every prop). The power poles'
    /// crossarms and their cables are probed too, but measured no flat run long enough for a bird
    /// on the 2026-09-30 art (the build log's "Perches" line counts each kind). The flock wheels
    /// over the campus side of the street, clear of the Astral tower. `Probe` steps the traffic and
    /// the flock without PlayMode and writes life_probe.txt beside the review renders.
    /// </summary>
    internal static class IlalimLifeAuthor
    {
        private const string Tag = "[IlalimRebuild] ";
        private const float TurnRadius = 5f;

        // ------------------------------------------------------------------ street geometry

        /// <summary>Padre Faura's OSM centre line (ArtSource/ilalim/osm_layout.json), east to west.</summary>
        private static readonly Vector2[] PadreFaura =
        {
            new Vector2(89.1f, 25.6f), new Vector2(7.2f, 31.5f), new Vector2(4.4f, 31.7f), new Vector2(.6f, 31.7f),
            new Vector2(-3.3f, 31.6f), new Vector2(-6.3f, 31.6f), new Vector2(-12f, 31.8f), new Vector2(-50.9f, 33f),
            new Vector2(-78.4f, 34f), new Vector2(-83.5f, 34.1f), new Vector2(-100.6f, 34.7f), new Vector2(-132.9f, 35.8f),
            new Vector2(-145.9f, 36.2f), new Vector2(-164.2f, 36.1f),
        };
        /// <summary>G. Apacible's centre line, west to east.</summary>
        private static readonly Vector2[] Apacible = { new Vector2(4.3f, -125.4f), new Vector2(88f, -127.7f) };

        private static float ZAt(Vector2[] line, float x)
        {
            for (int k = 0; k + 1 < line.Length; k++)
            {
                var a = line[k]; var b = line[k + 1];
                if (x >= Mathf.Min(a.x, b.x) && x <= Mathf.Max(a.x, b.x))
                    return Mathf.Lerp(a.y, b.y, (x - a.x) / (b.x - a.x));
            }
            return Mathf.Abs(x - line[0].x) < Mathf.Abs(x - line[line.Length - 1].x) ? line[0].y : line[line.Length - 1].y;
        }

        /// <summary>A Padre Faura lane point: `offset` metres north of the centre line (the right
        /// side of westbound travel). ⚠️ The east arm's centre lane runs 0.35 m north of the OSM
        /// line: its parked cars stand 3.1 m out on both kerbs, and the south row's jeepney is the
        /// widest thing parked there.</summary>
        private static Vector3 Pf(float x, float offset)
        {
            float bias = .35f * Mathf.SmoothStep(0f, 1f, (x - 9f) / 8f);
            return new Vector3(x, 0f, ZAt(PadreFaura, x) + offset + bias);
        }

        private static Vector3 Ga(float x, float offset) => new Vector3(x, 0f, ZAt(Apacible, x) + offset);

        private static IEnumerable<Vector3> Arc(Vector3 centre, float radius, float fromDeg, float toDeg, int steps = 12)
        {
            for (int k = 0; k <= steps; k++)
            {
                float a = Mathf.Lerp(fromDeg, toDeg, k / (float)steps) * Mathf.Deg2Rad;
                yield return new Vector3(centre.x + radius * Mathf.Cos(a), 0f, centre.z + radius * Mathf.Sin(a));
            }
        }

        /// <summary>Padre Faura from `x0` to `x1` (westward), easing from one lane offset to another.</summary>
        private static IEnumerable<Vector3> PfShift(float x0, float x1, float from, float to)
        {
            for (float x = x0; x >= x1 - 1e-3f; x -= 1f)
                yield return Pf(x, Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, (x0 - x) / (x0 - x1))));
        }

        private static Vector3[] Clean(IEnumerable<Vector3> points)
        {
            var list = new List<Vector3>();
            foreach (var p in points) if (list.Count == 0 || Vector3.Distance(list[list.Count - 1], p) > .05f) list.Add(p);
            return list.ToArray();
        }

        private static readonly float[] WestArm = { -12f, -20f, -30f, -40f, -50.9f, -65f, -78.4f, -90f, -100.6f, -115f, -132.9f, -145.9f, -160f };

        private static KantoTraffic.Route[] Routes()
        {
            float r = TurnRadius;
            var east = new[] { 86f, 70f, 50f, 30f, 20f }.Select(x => Pf(x, 0f));
            // N1: Padre Faura straight through, into the west arm's south lane.
            var n1 = east.Concat(PfShift(17f, 8f, 0f, -3f)).Concat(new[] { 5f, 2f, -1f, -4f, -8f }.Concat(WestArm).Select(x => Pf(x, -3f)));
            // N3: Padre Faura's east arm, right turn north up Taft's east lane.
            float zs = Pf(1.9f + r, 3f).z;
            var n3 = east.Concat(PfShift(17f, 1.9f + r + .5f, 0f, 3f)).Concat(Arc(new Vector3(1.9f + r, 0, zs + r), r, -90f, -180f))
                .Concat(new[] { zs + r + 5f, 60f, 100f, 150f, 212f }.Select(z => new Vector3(1.9f, 0, z)));
            // N4: Taft southbound from UN Avenue, right turn into Padre Faura's west arm centre lane.
            float ze = Pf(-1.9f - r, 0f).z;
            var n4 = new[] { 212f, 150f, 100f, 60f, 46f }.Select(z => new Vector3(-1.9f, 0, z))
                .Concat(Arc(new Vector3(-1.9f - r, 0, ze + r), r, 0f, -90f)).Concat(new[] { -8f }.Concat(WestArm).Select(x => Pf(x, 0f)));
            // S1: Taft northbound from Pedro Gil, right turn into G. Apacible.
            float zg = Ga(1.9f + r, -1.5f).z;
            var s1 = new[] { -212f, -200f, -170f, -150f, zg - r }.Select(z => new Vector3(1.9f, 0, z))
                .Concat(Arc(new Vector3(1.9f + r, 0, zg - r), r, 180f, 90f)).Concat(new[] { 12f, 20f, 30f, 45f, 60f }.Select(x => Ga(x, -1.5f)));
            // S0: the queue at the closed court end, behind the pier row at z -19 and its collars.
            var s0 = new[] { -110f, -80f, -50f, -21.2f }.Select(z => new Vector3(1.9f, 0, z));
            // S2: southbound away from the court, a U-turn past the median's end (z -150), then S1.
            var s2 = new[] { -21.2f, -60f, -100f, -150f, -200f }.Select(z => new Vector3(-1.9f, 0, z))
                .Concat(Arc(new Vector3(0, 0, -200f), 1.9f, 180f, 360f));
            return new[]
            {
                new KantoTraffic.Route { Name = "N1 Padre Faura west", Points = Clean(n1) },
                new KantoTraffic.Route { Name = "N3 Padre Faura to Taft north", Points = Clean(n3) },
                new KantoTraffic.Route { Name = "N4 Taft south to Padre Faura", Points = Clean(n4) },
                new KantoTraffic.Route { Name = "S1 Taft north to G. Apacible", Points = Clean(s1) },
                new KantoTraffic.Route { Name = "S0 queue at the closed court", Points = Clean(s0) },
                new KantoTraffic.Route { Name = "S2 Taft south, U-turn", Points = Clean(s2) },
            };
        }
        private const int N1 = 0, N3 = 1, N4 = 2, S1 = 3, S0 = 4, S2 = 5;

        // ------------------------------------------------------------------ vehicles

        private static readonly string[] PartSuffixes = { "_body", "_trim", "_glass", "_wheels", "_round", "_livery", "_letters" };

        /// <summary>Each kind's dressing-group name, read by KantoStreetSound for its engine and horn
        /// (it matches "jeepney", "bus", "tricycle", "van", "hatch", "taxi"; the UV Express is a
        /// diesel van), its cruise speed (KantoTrafficAuthor's numbers), and the route its one
        /// placed vehicle drives.</summary>
        private static readonly Dictionary<string, (string group, float cruise, int route)> Kinds = new Dictionary<string, (string, float, int)>
        {
            ["jeepney_taft"] = ("jeepney_taft", 7.5f, S0), ["uv_express"] = ("van_uv_express", 8.5f, S0),
            ["bus_liner"] = ("bus_liner", 7f, S2), ["jeepney_green"] = ("jeepney_green", 7.5f, N4),
            ["taxi"] = ("taxi", 9.5f, N3), ["tricycle"] = ("tricycle", 5.5f, N1),
            ["sedan_grey"] = ("sedan_grey", 9.5f, N4), ["hatch_maroon"] = ("hatch_maroon", 9.5f, N1),
        };

        /// <summary>The copies: kind, route, and a point on the route it starts at.</summary>
        private static (string kind, int route, Vector3 at)[] Copies() => new[]
        {
            ("sedan_grey", N1, Pf(60f, 0f)), ("jeepney_taft", N1, Pf(-60f, -3f)), ("uv_express", N1, Pf(-115f, -3f)),
            ("hatch_maroon", N3, Pf(75f, 0f)), ("tricycle", N3, new Vector3(1.9f, 0, 120f)), ("sedan_grey", N3, new Vector3(1.9f, 0, 175f)),
            ("taxi", N4, new Vector3(-1.9f, 0, 120f)), ("uv_express", N4, new Vector3(-1.9f, 0, 175f)),
            ("bus_liner", N4, new Vector3(-1.9f, 0, 85f)), ("jeepney_taft", N4, Pf(-95f, 0f)),
            ("taxi", S1, new Vector3(1.9f, 0, -185f)), ("jeepney_green", S1, new Vector3(1.9f, 0, -158f)), ("sedan_grey", S1, Ga(35f, -1.5f)),
        };

        public static void Build(Transform root, Transform dressing)
        {
            var report = new StringBuilder();
            Traffic(root, dressing, report);
            Pigeons(root, dressing, report);
            // The sidewalk people (IlalimSidewalkAuthor): after the traffic, whose lanes they keep clear of.
            IlalimSidewalkAuthor.Build(root, dressing, report);
            Debug.Log(Tag + "Life:\n" + report);
        }

        private static void Traffic(Transform root, Transform dressing, StringBuilder report)
        {
            var parts = dressing.Find("Traffic");
            if (parts == null) { report.AppendLine("No Traffic group: no live traffic."); return; }
            var go = new GameObject("LiveTraffic");
            go.transform.SetParent(root, false);
            var traffic = go.AddComponent<KantoTraffic>();
            traffic.Routes = Routes();
            // Only the street sound reads these in route mode: the bed opens toward Padre Faura's
            // line (z 31) and the sirens pass on the four lines 31 m out, the hospital side among them.
            traffic.Road = 31f; traffic.LaneOffset = 1.9f; traffic.Extent = 128f; traffic.GroundY = 0f;
            traffic.LampMaterials = new[] { "street_lens_red", "street_lens_amber", "street_lens_green" };
            traffic.BrakeMaterial = "veh_signal_red";
            // The street kit lights only the red lens (emission 1.35, .38, .27); amber and green get
            // the same strength in their own hue, and the brake lamps a red of their own.
            traffic.LampGlow = new[] { new Color(1.35f, .38f, .27f), new Color(1.4f, .95f, .22f), new Color(.3f, 1.3f, .75f) };
            traffic.BrakeGlow = new Color(1.1f, .1f, .07f);

            // The stop lines: Padre Faura's before its east zebra (x 17.5), Taft's on its painted
            // line north of the junction (z 44.3), and the closure at the court (never green).
            var stops = new List<(int route, Vector3 at, int axis)>
            {
                (N1, Pf(17.5f, 0f), 0), (N3, Pf(17.5f, 0f), 0), (N4, new Vector3(-1.9f, 0, 44.6f), 1),
            };
            foreach (var (route, at, axis) in stops)
            {
                float along = traffic.RouteAlong(route, at, out float off);
                traffic.Routes[route].StopAlong = new[] { along };
                traffic.Routes[route].StopAxis = new[] { axis };
                if (off > .5f) Debug.LogWarning($"{Tag}Stop line {at} is {off:F2} m off {traffic.Routes[route].Name}");
            }
            var queue = traffic.Routes[S0];
            queue.StopAlong = new[] { traffic.RouteAlong(S0, queue.Points[queue.Points.Length - 1], out _) };
            queue.StopAxis = new[] { -1 };
            traffic.Routes[S2].Next = S1;
            traffic.Routes[S2].NextAlong = traffic.RouteAlong(S1, traffic.Routes[S2].Points[traffic.Routes[S2].Points.Length - 1], out _);
            // The U-turn waits 4 m short of its arc until Taft's northbound lane is clear to join.
            traffic.Routes[S2].StopAlong = new[] { traffic.RouteAlong(S2, new Vector3(-1.9f, 0, -196f), out _) };
            traffic.Routes[S2].StopAxis = new[] { -2 };

            // The placed vehicles: every part stands on the vehicle's own pose (the kit exports each
            // part relative to the vehicle origin), so parts are grouped by kind and position.
            var vehicles = new Dictionary<string, Transform>();
            var byKind = new Dictionary<string, Transform>();
            foreach (var part in parts.Cast<Transform>().ToArray())
            {
                string name = part.name;
                string suffix = PartSuffixes.FirstOrDefault(s => name.EndsWith(s, StringComparison.Ordinal));
                if (!name.StartsWith("veh_", StringComparison.Ordinal) || suffix == null) continue;
                string kind = name.Substring(4, name.Length - 4 - suffix.Length);
                if (!Kinds.TryGetValue(kind, out var spec)) { Debug.LogWarning(Tag + "Unknown vehicle kind " + kind); continue; }
                var p = part.position;
                string key = $"{kind}@{p.x:F1},{p.z:F1}";
                if (!vehicles.TryGetValue(key, out var vehicle))
                {
                    var group = parts.Find(spec.group) ?? NewGroup(parts, spec.group);
                    vehicle = new GameObject(kind).transform;
                    vehicle.SetParent(group, false);
                    vehicle.SetPositionAndRotation(part.position, part.rotation);
                    vehicles[key] = vehicle;
                    if (!byKind.ContainsKey(kind)) byKind[kind] = vehicle;
                }
                part.SetParent(vehicle, true);
            }

            var rng = new System.Random(11);
            var drivers = new List<KantoTraffic.Driver>();
            var offsets = new Dictionary<string, Quaternion>();
            var log = new StringBuilder();
            foreach (var kv in vehicles)
            {
                var vehicle = kv.Value; string kind = vehicle.name; var spec = Kinds[kind];
                float along = traffic.RouteAlong(spec.route, vehicle.position, out float off);
                if (off > 2.5f) Debug.LogWarning($"{Tag}Placed {kind} at {vehicle.position} is {off:F2} m off {traffic.Routes[spec.route].Name}");
                var heading = traffic.RouteHeading(spec.route, along);
                var offset = Quaternion.Inverse(Quaternion.LookRotation(heading, Vector3.up)) * vehicle.rotation;
                offsets[kind] = offset;
                MakeDriver(vehicle);
                drivers.Add(Driver(vehicle, spec.route, along, heading, spec.cruise, offset, rng));
                log.Append($"{kind} yaw {Mathf.Round(offset.eulerAngles.y)}; ");
            }
            foreach (var (kind, route, at) in Copies())
            {
                if (!byKind.TryGetValue(kind, out var source)) { Debug.LogWarning(Tag + "No placed " + kind + " to copy"); continue; }
                float along = traffic.RouteAlong(route, at, out _);
                var heading = traffic.RouteHeading(route, along);
                var copy = Object.Instantiate(source.gameObject, source.parent).transform;
                copy.name = kind;
                copy.SetPositionAndRotation(traffic.RoutePoint(route, along), Quaternion.LookRotation(heading, Vector3.up) * offsets[kind]);
                MakeDriver(copy);
                drivers.Add(Driver(copy, route, along, heading, Kinds[kind].cruise, offsets[kind], rng));
            }
            traffic.Drivers = drivers.ToArray();

            // Nobody starts inside anybody: per route, in order along it.
            for (int r = 0; r < traffic.Routes.Length; r++)
            {
                var on = drivers.Where(d => d.Lane == r).OrderBy(d => d.Along).ToArray();
                for (int k = 1; k < on.Length; k++)
                {
                    float gap = on[k].Along - on[k - 1].Along - (on[k].Length + on[k - 1].Length) * .5f;
                    if (gap < 1f) Debug.LogWarning($"{Tag}{on[k - 1].Body.name} and {on[k].Body.name} start {gap:F1} m apart on {traffic.Routes[r].Name}");
                }
            }

            var furniture = dressing.Find("StreetFurniture");
            traffic.Signals = furniture == null ? new Renderer[0] : furniture.Cast<Transform>()
                .Where(t => t.name.StartsWith("street_signal", StringComparison.Ordinal))
                .SelectMany(t => t.GetComponentsInChildren<Renderer>()).ToArray();

            // The street sound: Kanto's own clips and gains (KantoTrafficAuthor).
            var sound = go.AddComponent<KantoStreetSound>();
            sound.Traffic = traffic;
            sound.CityBed = KantoTrafficAuthor.Clip("kanto_city_bed", true);
            sound.EngineCar = KantoTrafficAuthor.Clip("kanto_engine_car", true);
            sound.EngineDiesel = KantoTrafficAuthor.Clip("kanto_engine_diesel", true);
            sound.EngineTricycle = KantoTrafficAuthor.Clip("kanto_engine_tricycle", true);
            sound.HornsCar = KantoTrafficAuthor.Clips("kanto_horn_car_");
            sound.HornsJeepney = KantoTrafficAuthor.Clips("kanto_horn_jeepney_");
            sound.HornsTricycle = KantoTrafficAuthor.Clips("kanto_horn_tricycle_");
            sound.HornBus = KantoTrafficAuthor.Clip("kanto_horn_bus", false);
            sound.HornTruck = KantoTrafficAuthor.Clip("kanto_horn_truck", false);
            sound.Sirens = KantoTrafficAuthor.Clips("kanto_siren_");
            sound.BedGain = 0.7f; sound.EngineGain = 0.5f; sound.HornGain = 0.8f; sound.SirenGain = 0.6f;

            int[] perRoute = new int[traffic.Routes.Length];
            foreach (var d in drivers) perRoute[d.Lane]++;
            report.AppendLine($"Traffic: {drivers.Count} drivers ({vehicles.Count} placed, {drivers.Count - vehicles.Count} copies), " +
                              $"routes {string.Join(", ", traffic.Routes.Select((r, i) => $"{r.Name} {perRoute[i]} ({r.Points.Length} points)"))}; " +
                              $"{traffic.Signals.Length} signal renderers; sound bed={sound.CityBed != null} " +
                              $"horns={sound.HornsCar.Length + sound.HornsJeepney.Length + sound.HornsTricycle.Length} sirens={sound.Sirens.Length}. " +
                              "Model offsets (one per kind, all nose-first): " + log);
        }

        private static KantoTraffic.Driver Driver(Transform body, int route, float along, Vector3 heading, float cruise, Quaternion offset, System.Random rng)
            => new KantoTraffic.Driver
            {
                Body = body, Lane = route, Along = along, Length = LengthAlong(body, heading),
                Cruise = cruise * (0.9f + 0.2f * (float)rng.NextDouble()), ModelOffset = offset,
                Jeepney = body.name.Contains("jeepney"),
            };

        private static Transform NewGroup(Transform parent, string name)
        {
            var t = new GameObject(name).transform; t.SetParent(parent, false); return t;
        }

        /// <summary>⚠️ A driver is NOT static and has NO collider (KantoTrafficAuthor.MakeDriver).</summary>
        private static void MakeDriver(Transform v)
        {
            foreach (var t in v.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
            foreach (var c in v.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        }

        private static float LengthAlong(Transform v, Vector3 dir)
        {
            Bounds? b = null;
            foreach (var r in v.GetComponentsInChildren<Renderer>())
                if (b == null) b = r.bounds; else { var bb = b.Value; bb.Encapsulate(r.bounds); b = bb; }
            if (b == null) return 4.5f;
            var s = b.Value.size;
            return Mathf.Abs(s.x * dir.x) + Mathf.Abs(s.z * dir.z);
        }

        // ------------------------------------------------------------------ pigeons

        private static void Pigeons(Transform root, Transform dressing, StringBuilder report)
        {
            var life = new GameObject("Pigeons").transform;
            life.SetParent(root, false);
            var templates = new GameObject("Templates").transform;
            templates.SetParent(life, false);
            var flocks = life.gameObject.AddComponent<LagoonFlocks>();
            flocks.BirdTemplate = LagoonCoveLife.Template("fauna_pigeon", templates);
            flocks.FishTemplates = new Transform[0];
            flocks.SchoolCentres = new Vector3[0];
            flocks.SchoolFloor = new float[0];
            flocks.WaterY = 0f;
            // ⚠️ THE SKY AREA IS THE CAMPUS SIDE OF THE STREET, 24 TO 36 m UP. The art carries no
            // colliders, so obstacle avoidance cannot see a building: the flock wheels where
            // nothing stands that high (PGH's roofs top out at 17 m, the east row's at 17), and its
            // disc stops short of the Astral tower, which rises to 70 m from x 18 east of the court.
            flocks.SkyCentre = new Vector3(-14f, 0f, 2f);
            flocks.SkyRadius = 28f;
            flocks.SkyHeight = new Vector2(24f, 36f);
            flocks.Dips = false;
            flocks.Flocks = 2; flocks.BirdsPerFlock = 8;
            flocks.FlightSpeed = new Vector2(5f, 8f);
            flocks.CourtCentre = Vector3.zero; flocks.CourtKeepOut = 18f;
            flocks.CourtHalf = 13f; flocks.CourtGroundY = 0.212f;
            flocks.MaxGroundedBirds = 8;
            flocks.LandEverySeconds = new Vector2(5f, 12f);
            flocks.LandGroup = new Vector2Int(2, 5);
            flocks.AvoidObstacles = true;
            flocks.WingFoldSweep = 84f; flocks.WingFoldDroop = 22f;
            flocks.FeatherMaterials = LagoonCoveLife.FeatherMaterials(
                ("feather_pigeon_grey", new Color(0.651f, 0.639f, 0.659f)),
                ("feather_pigeon_pale", new Color(0.776f, 0.765f, 0.780f)),
                ("feather_pigeon_dark", new Color(0.373f, 0.361f, 0.388f)));
            flocks.PerchLines = Perches(dressing, report);
            report.AppendLine($"Pigeons: template {(flocks.BirdTemplate != null ? "fauna_pigeon" : "MISSING")}, " +
                              $"{flocks.Flocks} x {flocks.BirdsPerFlock}, {flocks.PerchLines.Length / 2} perch lines, " +
                              $"{flocks.FeatherMaterials.Length} feather materials.");
        }

        /// <summary>The groups whose meshes can carry a perch or block one.</summary>
        private static readonly string[] SolidGroups =
            { "Guideway", "StreetFurniture", "Eastside", "Heritage", "SariSari", "Rooftops", "Stations", "Landmarks", "Props", "StreetLife", "ColumnSigns", "Tindahan", "Hazards" };

        private static Vector3[] Perches(Transform dressing, StringBuilder report)
        {
            var temporary = new List<MeshCollider>();
            var footprints = new List<Bounds>();
            foreach (var name in SolidGroups)
            {
                var group = dressing.Find(name);
                if (group == null) continue;
                foreach (var filter in group.GetComponentsInChildren<MeshFilter>())
                {
                    var renderer = filter.GetComponent<Renderer>();
                    if (filter.sharedMesh == null || renderer == null) continue;
                    var b = renderer.bounds;
                    if (Flat(b.center).magnitude - Mathf.Max(b.extents.x, b.extents.z) > 120f) continue;
                    // Small things standing on the ground block a pavement perch by their footprint
                    // (a MeshCollider is a shell: a ray from above cannot tell a trunk's inside).
                    if (b.min.y < 1f && b.max.y > .3f && b.size.x < 12f && b.size.z < 12f) footprints.Add(b);
                    if (filter.GetComponent<Collider>() != null) continue;
                    var query = filter.gameObject.AddComponent<MeshCollider>();
                    query.sharedMesh = filter.sharedMesh;
                    temporary.Add(query);
                }
            }
            var trees = dressing.Find("Trees");
            if (trees != null)
                foreach (var r in trees.GetComponentsInChildren<Renderer>())
                    if (r.name.Contains("wood") && r.bounds.min.y < 1f && Flat(r.bounds.center).magnitude < 60f) footprints.Add(r.bounds);
            Physics.SyncTransforms();
            var lines = new List<Vector3>();
            var counts = new Dictionary<string, int>();
            void Add(string what, List<Vector3> found) { lines.AddRange(found); counts[what] = (counts.TryGetValue(what, out int n) ? n : 0) + found.Count / 2; }
            try
            {
                var guideway = dressing.Find("Guideway");
                // The guideway's parapet copings over and beside the court, both sides.
                foreach (int side in new[] { -1, 1 })
                {
                    if (!Ridge(guideway, new Vector3(side * 4.4f, 0, 3f), new Vector3(side * 5.7f, 0, 3f), 13.2f, 9.4f, 10.8f, out var top)) continue;
                    Add("guideway parapets", Runs(guideway, new Vector3(top.x, 0, -46f), new Vector3(top.x, 0, 46f), 13.2f, top.y - .06f, top.y + .06f, .4f, 1.5f));
                }
                // The power poles' crossarms along Taft and Padre Faura, near the court.
                var furniture = dressing.Find("StreetFurniture");
                if (furniture != null)
                    foreach (Transform t in furniture)
                    {
                        if (t.name != "street_pole_steel" && t.name != "street_pole_tx_steel") continue;
                        var b = Bounds(t);
                        if (b == null || Flat(b.Value.center).magnitude > 50f) continue;
                        var c = b.Value.center; var e = b.Value.extents;
                        bool alongX = e.x >= e.z;
                        // Across the arm's depth until a line holds: its top is a few centimetres
                        // wide, and the cables strung on it (the same group) count as its top too.
                        for (int k = 0; k <= 6; k++)
                        {
                            float across = (k / 6f - .5f) * 2f * (alongX ? e.z : e.x);
                            var a = alongX ? new Vector3(b.Value.min.x, 0, c.z + across) : new Vector3(c.x + across, 0, b.Value.min.z);
                            var z = alongX ? new Vector3(b.Value.max.x, 0, c.z + across) : new Vector3(c.x + across, 0, b.Value.max.z);
                            var found = Runs(furniture, a, z, b.Value.max.y + .5f, b.Value.max.y - .35f, b.Value.max.y + .05f, .1f, .3f, .7f);
                            if (found.Count == 0) continue;
                            Add("pole crossarms", found);
                            break;
                        }
                    }
                // The flat roofs along the street, the near ones only, and never under or behind the tower.
                foreach (var name in new[] { "Eastside", "Heritage", "SariSari" })
                {
                    var group = dressing.Find(name);
                    if (group == null) continue;
                    foreach (Transform t in group)
                    {
                        var b = Bounds(t);
                        if (b == null) continue;
                        var bb = b.Value;
                        if (bb.max.y < 3f || bb.max.y > 26f || bb.min.x > 17.5f || bb.max.x < -45f || bb.max.z < -40f || bb.min.z > 50f) continue;
                        Add("roofs", Roof(t, bb));
                    }
                }
                // The stations' roofs, their ends nearest the court.
                var stations = dressing.Find("Stations");
                foreach (int end in new[] { -1, 1 })
                    if (Ridge(stations, new Vector3(-1.5f, 0, end * 100f), new Vector3(1.5f, 0, end * 100f), 22f, 16f, 20f, out var crown))
                        Add("station roofs", Runs(stations, new Vector3(crown.x, 0, end * 95.5f), new Vector3(crown.x, 0, end * 112f), 22f, crown.y - .08f, crown.y + .08f, .4f, 1.5f));
                // The pavements' outer edge, outside the chalk box, clear of every prop and post.
                foreach (int side in new[] { -1, 1 })
                    Add("pavement edges", Pavement(side * 10.45f, footprints));
            }
            finally
            {
                foreach (var q in temporary) if (q != null) Object.DestroyImmediate(q);
                Physics.SyncTransforms();
            }
            report.AppendLine("Perches: " + string.Join(", ", counts.Select(kv => $"{kv.Key} {kv.Value}")) +
                              $" ({lines.Count / 2} lines, {Length(lines):F0} m in all).");
            return lines.ToArray();
        }

        private static float Length(List<Vector3> pairs)
        {
            float sum = 0f;
            for (int k = 0; k + 1 < pairs.Count; k += 2) sum += Vector3.Distance(pairs[k], pairs[k + 1]);
            return sum;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private static Bounds? Bounds(Transform t)
        {
            Bounds? b = null;
            foreach (var r in t.GetComponentsInChildren<Renderer>())
                if (b == null) b = r.bounds; else { var bb = b.Value; bb.Encapsulate(r.bounds); b = bb; }
            return b;
        }

        /// <summary>A downward ray that counts only when the first thing it meets belongs to `owner`
        /// (anything else above means the spot is covered) and is flat.</summary>
        private static bool Hit(Transform owner, Vector3 xz, float fromY, out Vector3 point, float minNormal = .8f)
        {
            point = default;
            if (!Physics.Raycast(new Vector3(xz.x, fromY, xz.z), Vector3.down, out var hit, fromY + 5f, ~0, QueryTriggerInteraction.Ignore)) return false;
            if (hit.normal.y < minNormal || owner == null || !hit.collider.transform.IsChildOf(owner)) return false;
            point = hit.point;
            return true;
        }

        /// <summary>The highest flat point of `owner` across a short sweep (a coping, a crown).</summary>
        private static bool Ridge(Transform owner, Vector3 a, Vector3 b, float fromY, float minY, float maxY, out Vector3 top)
        {
            top = default; bool found = false;
            for (int k = 0; k <= 40; k++)
            {
                var at = Vector3.Lerp(a, b, k / 40f);
                if (!Hit(owner, at, fromY, out var p) || p.y < minY || p.y > maxY) continue;
                if (!found || p.y > top.y + .005f) { top = p; found = true; }
            }
            return found;
        }

        /// <summary>Sample a line every `step` metres and keep the runs of flat hits within the
        /// height band whose neighbours agree to 6 cm, each at least `minLength` long.</summary>
        private static List<Vector3> Runs(Transform owner, Vector3 a, Vector3 b, float fromY, float minY, float maxY, float step, float minLength, float minNormal = .8f)
        {
            var result = new List<Vector3>();
            int n = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(a, b) / step));
            Vector3? start = null, last = null;
            for (int k = 0; k <= n + 1; k++)
            {
                bool ok = false; Vector3 p = default;
                if (k <= n && Hit(owner, Vector3.Lerp(a, b, k / (float)n), fromY, out p, minNormal) && p.y >= minY && p.y <= maxY)
                    ok = last == null || Mathf.Abs(p.y - last.Value.y) < .06f;
                if (ok) { if (start == null) start = p; last = p; continue; }
                Close(result, start, last, minLength);
                start = null; last = null;
                // A hit that broke the run on height alone starts the next one.
                if (k <= n && Hit(owner, Vector3.Lerp(a, b, k / (float)n), fromY, out p, minNormal) && p.y >= minY && p.y <= maxY) { start = p; last = p; }
            }
            return result;
        }

        private static void Close(List<Vector3> result, Vector3? start, Vector3? last, float minLength)
        {
            if (start == null || last == null) return;
            var s = start.Value; var e = last.Value;
            float len = Vector3.Distance(s, e);
            if (len < minLength) return;
            // Pulled in 10 cm at each end, so a bird never stands on the very edge of a run.
            var d = (e - s) / len;
            result.Add(s + d * .1f); result.Add(e - d * .1f);
        }

        /// <summary>A roof's flat runs, sampled in rows along its longer side; the two longest nearest
        /// the street (smallest |x|) are kept.</summary>
        private static List<Vector3> Roof(Transform owner, Bounds b)
        {
            var runs = new List<(Vector3 a, Vector3 b)>();
            bool alongZ = b.size.z >= b.size.x;
            float fromY = b.max.y + 2f;
            for (float u = .6f; u < (alongZ ? b.size.x : b.size.z) - .5f; u += 1.2f)
            {
                Vector3 a = alongZ ? new Vector3(b.min.x + u, 0, b.min.z) : new Vector3(b.min.x, 0, b.min.z + u);
                Vector3 z = alongZ ? new Vector3(b.min.x + u, 0, b.max.z) : new Vector3(b.max.x, 0, b.min.z + u);
                var found = Runs(owner, a, z, fromY, b.min.y + 2.5f, b.max.y + .1f, 1f, 2.5f, .93f);
                for (int k = 0; k + 1 < found.Count; k += 2)
                {
                    // Only a roof beside the street (never over the play area), and nothing east of
                    // x 17.5, where the flight in would pass through the Astral tower.
                    if (Mathf.Abs(found[k].x) < 11f || Mathf.Abs(found[k + 1].x) < 11f) continue;
                    if (found[k].x > 17.5f || found[k + 1].x > 17.5f) continue;
                    runs.Add((found[k], found[k + 1]));
                }
            }
            var result = new List<Vector3>();
            foreach (var r in runs.OrderBy(r => Mathf.Min(Mathf.Abs(r.a.x), Mathf.Abs(r.b.x))).ThenByDescending(r => Vector3.Distance(r.a, r.b)).Take(2))
            {
                // Long runs are cut to 8 m so one roof never takes every landing.
                var s = r.a; var e = r.b; float len = Vector3.Distance(s, e);
                if (len > 8f) { var mid = (s + e) * .5f; var d = (e - s) / len; s = mid - d * 4f; e = mid + d * 4f; }
                result.Add(s); result.Add(e);
            }
            return result;
        }

        /// <summary>The pavement's outer edge (top 0.212, the contract) from z -30 to 30, split around
        /// every prop, post, trunk and gameplay collider standing on it.</summary>
        private static List<Vector3> Pavement(float x, List<Bounds> footprints)
        {
            var result = new List<Vector3>();
            const float y = .212f, step = .25f;
            Vector3? start = null, last = null;
            for (float z = -30f; z <= 30.001f; z += step)
            {
                var p = new Vector3(x, y, z);
                bool clear = !Physics.CheckBox(p + Vector3.up * .3f, new Vector3(.3f, .15f, .3f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                if (clear)
                    foreach (var f in footprints)
                        if (p.x > f.min.x - .35f && p.x < f.max.x + .35f && p.z > f.min.z - .35f && p.z < f.max.z + .35f) { clear = false; break; }
                if (clear) { if (start == null) start = p; last = p; continue; }
                Close(result, start, last, 1.2f);
                start = null; last = null;
            }
            Close(result, start, last, 1.2f);
            return result;
        }
        // ------------------------------------------------------------------ the life probe

        /// <summary>A PlayMode-free check of the life, run by IlalimSceneBuilder.RunReview: the saved
        /// scene is opened (and never saved), and the traffic's and the flock's OWN step methods are
        /// driven through reflection at 20 steps a second for 90 simulated seconds, exactly the code
        /// Update runs in Play (no players stand in the scene, so no bird is startled). It measures
        /// how far every vehicle travelled, whether any two vehicles ever overlapped (their
        /// length-by-width rectangles), whether any vehicle entered the court (|x| &lt; 7.4 and
        /// |z| &lt; 16.5), how many pigeons landed and where, and renders four views at 45 s.</summary>
        public static void Probe(string folder, string scenePath)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var sb = new StringBuilder("ILALIM LIFE PROBE (reflection-driven steps, 0.05 s, no PlayMode)\n");
            var traffic = Object.FindAnyObjectByType<KantoTraffic>();
            var flocks = Object.FindAnyObjectByType<LagoonFlocks>();
            const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic;
            if (traffic == null || flocks == null)
            {
                sb.AppendLine($"traffic {traffic != null}, flocks {flocks != null}: nothing to probe");
                File.WriteAllText(Path.Combine(folder, "life_probe.txt"), sb.ToString());
                return;
            }
            typeof(KantoTraffic).GetMethod("Start", F).Invoke(traffic, null);
            typeof(LagoonFlocks).GetMethod("Start", F).Invoke(flocks, null);
            var clock = typeof(KantoTraffic).GetField("_clock", F);
            var routes = typeof(KantoTraffic).GetMethod("UpdateRoutes", F);
            var signals = typeof(KantoTraffic).GetMethod("UpdateSignals", F);
            var avoid = typeof(LagoonFlocks).GetMethod("StepAvoidance", F);
            var birds = typeof(LagoonFlocks).GetMethod("StepBirds", F);
            var ground = typeof(LagoonFlocks).GetMethod("StepGroundLife", F);
            var mode = typeof(LagoonFlocks).GetField("_mode", F);

            int n = traffic.Drivers.Length;
            var start = new Vector3[n]; var last = new Vector3[n]; var travelled = new float[n]; var top = new float[n];
            var width = new float[n]; var waitedSteps = new int[n];
            for (int i = 0; i < n; i++)
            {
                var d = traffic.Drivers[i];
                start[i] = last[i] = d.Body.position;
                var h = traffic.RouteHeading(d.Lane, d.Along);
                var bb = Bounds(d.Body) ?? new Bounds(d.Body.position, Vector3.one * 2f);
                width[i] = Mathf.Abs(bb.size.x * h.z) + Mathf.Abs(bb.size.z * h.x);
            }
            int overlaps = 0, courtEntries = 0, landings = 0, groundedMax = 0, groundedOffLine = 0, groundedInCourt = 0;
            float closest = float.MaxValue; string closestPair = "";
            var landedAt = new List<Vector3>();
            var wasGrounded = new bool[flocks.BirdCount];
            const float dt = .05f; const int steps = 1800; bool shot = false;
            for (int s = 1; s <= steps; s++)
            {
                clock.SetValue(traffic, (float)clock.GetValue(traffic) + dt);
                routes.Invoke(traffic, new object[] { dt });
                signals.Invoke(traffic, null);
                if (flocks.AvoidObstacles) avoid.Invoke(flocks, new object[] { dt });
                birds.Invoke(flocks, new object[] { dt });
                ground.Invoke(flocks, new object[] { dt });
                Physics.SyncTransforms();
                for (int i = 0; i < n; i++)
                {
                    var p = traffic.Drivers[i].Body.position;
                    float step = Vector3.Distance(p, last[i]);
                    if (step < 5f) travelled[i] += step;                 // a wrap is a teleport, not travel
                    last[i] = p; top[i] = Mathf.Max(top[i], traffic.DriverSpeed(i));
                    if (traffic.DriverWaiting(i)) waitedSteps[i]++;
                    var f = Nose(traffic.Drivers[i].Body, traffic.Drivers[i].ModelOffset);
                    if (Overlap(p, f, traffic.Drivers[i].Length, width[i], Vector3.zero, Vector3.forward, 14.8f, 33f)) courtEntries++;
                    for (int j = i + 1; j < n; j++)
                    {
                        var q = traffic.Drivers[j].Body.position;
                        float dist = Vector3.Distance(p, q);
                        if (dist < closest) { closest = dist; closestPair = traffic.Drivers[i].Body.name + " / " + traffic.Drivers[j].Body.name; }
                        if (dist > 20f) continue;
                        if (!Overlap(p, f, traffic.Drivers[i].Length, width[i], q, Nose(traffic.Drivers[j].Body, traffic.Drivers[j].ModelOffset), traffic.Drivers[j].Length, width[j])) continue;
                        overlaps++;
                        if (overlaps <= 8) sb.AppendLine(FormattableString.Invariant($"  OVERLAP t={s * dt:F2}s {traffic.Drivers[i].Body.name} {p} / {traffic.Drivers[j].Body.name} {q}"));
                    }
                }
                var modes = (int[])mode.GetValue(flocks);
                int grounded = 0;
                for (int b = 0; b < flocks.BirdCount; b++)
                {
                    if (modes[b] != 2) { wasGrounded[b] = false; continue; }
                    grounded++;
                    var at = flocks.BirdPosition(b);
                    if (!wasGrounded[b])
                    {
                        landings++; landedAt.Add(at);
                        if (DistanceToLines(flocks.PerchLines, at) > .06f) groundedOffLine++;
                        if (Mathf.Abs(at.x) < 7f && Mathf.Abs(at.z) < 16.5f && at.y < 1f) groundedInCourt++;
                    }
                    wasGrounded[b] = true;
                }
                groundedMax = Mathf.Max(groundedMax, grounded);
                if (!shot && s >= 900)
                    for (int b = 0; b < flocks.BirdCount && !shot; b++)
                        if (modes[b] == 2) { Shots(folder, flocks.BirdPosition(b)); shot = true; sb.AppendLine(FormattableString.Invariant($"Views rendered at t={s * dt:F1}s, the pigeon view on the bird at {flocks.BirdPosition(b):F2}.")); }
            }
            sb.AppendLine(FormattableString.Invariant($"Traffic: {n} drivers over {steps * dt:F0} s. Overlapping vehicle pairs (pair-steps): {overlaps}. Vehicle-steps inside the court: {courtEntries}. Closest centres {closest:F2} m ({closestPair})."));
            for (int i = 0; i < n; i++)
            {
                var d = traffic.Drivers[i];
                sb.AppendLine(FormattableString.Invariant($"  {d.Body.name,-14} {traffic.Routes[d.Lane].Name,-30} travelled {travelled[i],6:F1} m, top {top[i]:F1} m/s, waiting {waitedSteps[i] * dt,5:F1} s, start {start[i]:F1} now {last[i]:F1}"));
            }
            sb.AppendLine(FormattableString.Invariant($"Pigeons: {flocks.BirdCount} birds, {flocks.PerchLines.Length / 2} perch lines. Landings {landings}, most on the ground at once {groundedMax}, landed off a perch line {groundedOffLine}, landed on the court {groundedInCourt}."));
            foreach (var at in landedAt.Take(40)) sb.AppendLine(FormattableString.Invariant($"  landed {at:F2}"));
            File.WriteAllText(Path.Combine(folder, "life_probe.txt"), sb.ToString());
            Debug.Log(Tag + sb);
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        /// <summary>A vehicle's travel direction, flat: its rotation without the model offset.</summary>
        private static Vector3 Nose(Transform body, Quaternion offset)
        {
            var f = body.rotation * Quaternion.Inverse(offset) * Vector3.forward; f.y = 0f;
            return f.sqrMagnitude > 1e-6f ? f.normalized : Vector3.forward;
        }

        /// <summary>Separating-axis test of two rectangles on the ground (centre, heading, length, width).</summary>
        private static bool Overlap(Vector3 pa, Vector3 ha, float la, float wa, Vector3 pb, Vector3 hb, float lb, float wb)
        {
            var d = pb - pa; d.y = 0f;
            var ra = Vector3.Cross(Vector3.up, ha); var rb = Vector3.Cross(Vector3.up, hb);
            foreach (var axis in new[] { ha, ra, hb, rb })
            {
                float projA = Mathf.Abs(Vector3.Dot(ha, axis)) * la * .5f + Mathf.Abs(Vector3.Dot(ra, axis)) * wa * .5f;
                float projB = Mathf.Abs(Vector3.Dot(hb, axis)) * lb * .5f + Mathf.Abs(Vector3.Dot(rb, axis)) * wb * .5f;
                if (Mathf.Abs(Vector3.Dot(d, axis)) > projA + projB) return false;
            }
            return true;
        }

        private static float DistanceToLines(Vector3[] lines, Vector3 at)
        {
            float best = float.MaxValue;
            for (int k = 0; k + 1 < lines.Length; k += 2)
            {
                var a = lines[k]; var ab = lines[k + 1] - a; float len2 = ab.sqrMagnitude;
                float t = len2 > 1e-8f ? Mathf.Clamp01(Vector3.Dot(at - a, ab) / len2) : 0f;
                best = Mathf.Min(best, Vector3.Distance(at, a + ab * t));
            }
            return best;
        }

        /// <summary>Four views mid-simulation, wearing the match look as the review does.</summary>
        private static void Shots(string folder, Vector3 pigeonAt)
        {
            // The pigeon view looks at a landed bird from 3.5 m, from the court's side of it.
            var toward = Flat(-pigeonAt); toward = toward.sqrMagnitude > .01f ? toward.normalized : Vector3.forward;
            var pigeonEye = pigeonAt + toward * 3.5f + Vector3.up * 1.2f;
            var camera = new GameObject("Ilalim life witness").AddComponent<Camera>();
            camera.enabled = false; camera.nearClipPlane = .05f; camera.farClipPlane = 400f; camera.fieldOfView = 58f;
            camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            camera.gameObject.AddComponent<WorldOutline>().PrototypeEnabled = true;
            WorldLookPresentation look = null;
            try
            {
                camera.gameObject.AddComponent<WorldLookCamera>();
                var sun = Object.FindObjectsByType<Light>().FirstOrDefault(l => l.type == LightType.Directional);
                var sceneRoot = GameObject.Find(IlalimSceneBuilder.SceneName);
                if (sceneRoot != null) look = WorldLookPresentation.InstallPreview(sceneRoot.transform, 0f, sun);
            }
            catch (Exception e) { Debug.LogWarning(Tag + "Life probe look failed: " + e.Message); }
            var shots = new (string name, Vector3 at, Vector3 look)[]
            {
                ("north_junction", new Vector3(0f, 4f, 14f), new Vector3(-6f, .5f, 34f)),
                ("south_queue", new Vector3(0f, 4f, -14f), new Vector3(1.9f, .5f, -40f)),
                ("pigeon", pigeonEye, pigeonAt),
            };
            foreach (var s in shots)
            {
                const int w = 1600, h = 900;
                camera.transform.SetPositionAndRotation(s.at, Quaternion.LookRotation(s.look - s.at));
                var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 4 };
                rt.Create(); camera.targetTexture = rt;
                camera.Render();
                var previous = RenderTexture.active;
                var display = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                bool write = GL.sRGBWrite; GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
                Graphics.Blit(rt, display); GL.sRGBWrite = write; RenderTexture.active = display;
                var image = new Texture2D(w, h, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, w, h), 0, 0); image.Apply();
                File.WriteAllBytes(Path.Combine(folder, $"ilalim_life_{s.name}.png"), image.EncodeToPNG());
                RenderTexture.active = previous; camera.targetTexture = null;
                RenderTexture.ReleaseTemporary(display); rt.Release();
                Object.DestroyImmediate(rt); Object.DestroyImmediate(image);
            }
            if (look != null) Object.DestroyImmediate(look.gameObject);
            Object.DestroyImmediate(camera.gameObject);
        }
    }
}
