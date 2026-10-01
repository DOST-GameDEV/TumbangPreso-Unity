using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// Turns Kanto's placed lane traffic into `KantoTraffic` drivers (docs/KANTO_DESIGN_GUIDE.md
    /// § 12.2). Called by `KantoSceneBuilder.Build` after the dressing is placed.
    ///
    /// * A vehicle is a DRIVER when it stands in a driving lane (within 1.2 m of one of the eight
    ///   lane lines; the two lanes of a road are 3.6 m apart). Cars parked at the kerbs stand
    ///   3.95 m off the centre line, 2.15 m from the lane, and stay parked; so does the tricycle
    ///   rank.
    /// * ⚠️ A driver is NOT STATIC and has NO COLLIDER: static batching bakes a transform, and
    ///   traffic must never be something a player, the lata or a tsinelas can touch.
    /// * The jeepney waiting at the stop (it stands in the kerb bay, right of its lane) becomes a
    ///   driver dwelling at the stop, and sets the stop's lane, position and pull-over distance.
    /// * Each driver keeps its PLACED rotation relative to its lane (ModelOffset). The log prints
    ///   those offsets per model: one value per model proves every car of that model was placed
    ///   nose-first; two values 180 degrees apart would mean some drive backwards.
    /// </summary>
    internal static class KantoTrafficAuthor
    {
        // Cruise speeds, m/s (about 34 km/h for a car on a city grid; buses and jeepneys slower,
        // tricycles slowest, so queues form behind them the way they do on a real street).
        private static readonly Dictionary<string, float> Cruise = new Dictionary<string, float>
        {
            ["sedan_red"] = 9.5f, ["sedan_cream"] = 9.5f, ["hatch_mint"] = 9.5f, ["taxi"] = 9.5f,
            ["pickup_mustard"] = 8.5f, ["van_delivery"] = 8.5f,
            ["jeepney"] = 7.5f, ["bus_city"] = 7f, ["tricycle"] = 5.5f,
        };

        public static void Build(Transform root, Transform dressing)
        {
            var go = new GameObject("Traffic");
            go.transform.SetParent(root, false);
            var traffic = go.AddComponent<KantoTraffic>();
            var rng = new System.Random(7);
            var drivers = new List<KantoTraffic.Driver>();
            var offsets = new Dictionary<string, HashSet<int>>();
            var ys = new List<float>();
            Transform stopJeepney = null;

            foreach (Transform group in dressing)
            {
                if (!Cruise.TryGetValue(group.name, out float cruise)) continue;
                foreach (Transform v in group)
                {
                    int lane = traffic.LaneAt(v.position);
                    if (lane < 0)
                    {
                        if (group.name == "jeepney") stopJeepney = v;
                        continue;
                    }
                    traffic.LaneFrame(lane, out var origin, out var dir);
                    var offset = Quaternion.Inverse(Quaternion.LookRotation(dir, Vector3.up)) * v.rotation;
                    drivers.Add(new KantoTraffic.Driver
                    {
                        Body = v, Lane = lane, Along = Vector3.Dot(v.position - origin, dir),
                        Length = LengthAlong(v, dir), Cruise = cruise * (0.9f + 0.2f * (float)rng.NextDouble()),
                        ModelOffset = offset, Jeepney = group.name == "jeepney",
                    });
                    if (!offsets.TryGetValue(group.name, out var set)) offsets[group.name] = set = new HashSet<int>();
                    set.Add(Mathf.RoundToInt(offset.eulerAngles.y / 5f) * 5 % 360);
                    ys.Add(v.position.y);
                    MakeDriver(v);
                }
            }

            // The stop: the lane whose right side the waiting jeepney stands on, 1.5 to 3 m out.
            if (stopJeepney != null)
            {
                for (int l = 0; l < KantoTraffic.Lanes; l++)
                {
                    traffic.LaneFrame(l, out var origin, out var dir);
                    var right = Vector3.Cross(Vector3.up, dir);
                    var rel = stopJeepney.position - origin;
                    float side = Vector3.Dot(rel, right);
                    if (side < 1.5f || side > 3f) continue;
                    float along = Vector3.Dot(rel, dir);
                    if (Mathf.Abs(along) > traffic.Extent) continue;
                    traffic.StopLane = l; traffic.StopAlong = along; traffic.StopShift = side;
                    drivers.Add(new KantoTraffic.Driver
                    {
                        Body = stopJeepney, Lane = l, Along = along, Length = LengthAlong(stopJeepney, dir),
                        Cruise = Cruise["jeepney"], Jeepney = true,
                        ModelOffset = Quaternion.Inverse(Quaternion.LookRotation(dir, Vector3.up)) * stopJeepney.rotation,
                    });
                    MakeDriver(stopJeepney);
                    break;
                }
            }

            traffic.Drivers = drivers.ToArray();
            traffic.GroundY = ys.Count > 0 ? ys.OrderBy(y => y).ElementAt(ys.Count / 2) : 0f;
            var signals = dressing.Find("traffic_signal");
            traffic.Signals = signals != null ? signals.GetComponentsInChildren<Renderer>() : new Renderer[0];

            // The street sound (owner: "needs sfx, bustling city ambience, car engine and driving
            // sounds, beep/horns"): tools/build_kanto_street_audio.py's clips, played by
            // KantoStreetSound from the traffic's own state.
            var sound = go.AddComponent<KantoStreetSound>();
            sound.Traffic = traffic;
            sound.CityBed = Clip("kanto_city_bed", true);
            sound.EngineCar = Clip("kanto_engine_car", true);
            sound.EngineDiesel = Clip("kanto_engine_diesel", true);
            sound.EngineTricycle = Clip("kanto_engine_tricycle", true);
            // Every numbered variant the tool wrote (owner: "more bustling, add some sirens here and
            // louder and more variety of beeps"), and the louder gains set explicitly: a scene
            // built earlier keeps its saved values, so the new defaults alone would not reach it.
            sound.HornsCar = Clips("kanto_horn_car_");
            sound.HornsJeepney = Clips("kanto_horn_jeepney_");
            sound.HornsTricycle = Clips("kanto_horn_tricycle_");
            sound.HornBus = Clip("kanto_horn_bus", false);
            sound.HornTruck = Clip("kanto_horn_truck", false);
            sound.Sirens = Clips("kanto_siren_");
            sound.BedGain = 0.7f; sound.EngineGain = 0.5f; sound.HornGain = 0.8f; sound.SirenGain = 0.6f;

            int[] perLane = new int[KantoTraffic.Lanes];
            foreach (var d in drivers) perLane[d.Lane]++;
            Debug.Log($"[Kanto] Traffic: {drivers.Count} drivers, lanes [{string.Join(", ", perLane)}], " +
                      $"stop lane {traffic.StopLane} at {traffic.StopAlong:0.0} (pull-over {traffic.StopShift:0.00} m), " +
                      $"{traffic.Signals.Length} signal renderers, ground y {traffic.GroundY:0.00}, " +
                      $"sound bed={sound.CityBed != null} engines={(sound.EngineCar != null ? 1 : 0) + (sound.EngineDiesel != null ? 1 : 0) + (sound.EngineTricycle != null ? 1 : 0)} horns={sound.HornsCar.Length + sound.HornsJeepney.Length + sound.HornsTricycle.Length + (sound.HornBus != null ? 1 : 0) + (sound.HornTruck != null ? 1 : 0)} sirens={sound.Sirens.Length}. Model offsets (yaw, deg): " +
                      string.Join("; ", offsets.Select(kv => kv.Key + " " + string.Join("/", kv.Value.OrderBy(a => a)))));
        }

        /// <summary>⚠️ Loops import as PCM (a compressed loop can carry encoder padding at its
        /// seam, a click every lap) and NOTHING is normalized: the authoring tool set the bed,
        /// engines and horns at different peaks on purpose, and Unity's default normalize would
        /// flatten that balance.</summary>
        internal static AudioClip Clip(string name, bool loop)
        {
            string path = $"Assets/TumbangPreso/Art/audio/ambience/{name}.wav";
            if (AssetImporter.GetAtPath(path) is AudioImporter importer)
            {
                bool dirty = false;
                var so = new SerializedObject(importer);
                var normalize = so.FindProperty("m_Normalize");
                if (normalize != null && normalize.boolValue) { normalize.boolValue = false; so.ApplyModifiedPropertiesWithoutUndo(); dirty = true; }
                var s = importer.defaultSampleSettings;
                if (loop && s.compressionFormat != AudioCompressionFormat.PCM)
                { s.compressionFormat = AudioCompressionFormat.PCM; s.loadType = AudioClipLoadType.DecompressOnLoad; importer.defaultSampleSettings = s; dirty = true; }
                if (dirty) importer.SaveAndReimport();
            }
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) Debug.LogWarning("[Kanto] Missing street sound " + path);
            return clip;
        }

        /// <summary>Every clip named prefix1, prefix2, ... in order, until one is missing.</summary>
        internal static AudioClip[] Clips(string prefix)
        {
            var list = new List<AudioClip>();
            for (int i = 1; i < 32; i++)
            {
                if (AssetImporter.GetAtPath($"Assets/TumbangPreso/Art/audio/ambience/{prefix}{i}.wav") == null) break;
                var c = Clip(prefix + i, false);
                if (c != null) list.Add(c);
            }
            return list.ToArray();
        }

        private static void MakeDriver(Transform v)
        {
            foreach (var t in v.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = false;
            foreach (var c in v.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
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
    }
}
