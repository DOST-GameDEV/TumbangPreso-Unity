using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Map;
using TumbangPreso.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// THE PAYO BREAK DIRECTOR'S EDITOR TOOL (2026-10-19, `PayoBreakDirector`). Request in `Temp/breakcam.request`,
    /// answer in `Temp/breakcam.done`:
    ///   `survey`            in edit mode, on the open Payo scene: where the map's landmarks stand (names, bounds), so the
    ///                       shots can be written against them. Nothing is changed.
    ///   `clip`              in Play: every shot of the director over every pair of layouts a break can travel between,
    ///                       and the half-time sequence, sampled finely, checked against the map: sphere casts along the
    ///                       path with a margin against every collider AND against temporary mesh colliders laid over
    ///                       every drawn mesh (the land has no colliders: the fall needs it so), a check that the eye is
    ///                       not inside any closed mesh (rays with back faces on), and not inside the inflated bounds of
    ///                       any compact solid (a statue, a house, a stone, a pillar). The moving stones are posed at each
    ///                       sample with `ArenaStage.PoseTravel`. Reports shots x layouts, samples and the least clearance.
    ///   `film LABEL`        in Play: each shot scrubbed through its own break (the stage posed on the same clock) and the
    ///                       half-time sequence with the night held on `PayoNight.DuskAt`, frames written to
    ///                       `Logs/payo/unity/breakcam/LABEL/`, then contact sheets per shot.
    ///   `live LABEL`        in Play: a real match, two real breaks ended early by the round's own clock (an ordinary
    ///                       break and half time), frames off the break's own camera, and the hand-back to the game camera
    ///                       measured (metres and degrees between the director's last pose and the round's first).
    /// It waits while another Payo request or probe is pending or the editor is playing, refuses on a dirty scene, and
    /// puts the open scenes back.
    /// </summary>
    [InitializeOnLoad]
    internal static class PayoBreakShots
    {
        private const string Request = "Temp/breakcam.request", Done = "Temp/breakcam.done", Root = "Logs/payo/unity/breakcam";
        private const string K = "PayoBreakShots.";
        private static IEnumerator _driver;
        private static int _lastFrame = -1;
        private static double _nextPoll;
        private static readonly List<string> Log = new List<string>();

        private static string Phase { get => SessionState.GetString(K + "phase", ""); set => SessionState.SetString(K + "phase", value); }
        private static string Verb => SessionState.GetString(K + "verb", "film");
        private static string Label => SessionState.GetString(K + "label", "breakcam");
        private static string Extra => SessionState.GetString(K + "extra", "");
        private static double Now => EditorApplication.timeSinceStartup;

        internal static bool Busy => Phase.Length > 0;

        static PayoBreakShots() { EditorApplication.update += Tick; }

        private static void Tick()
        {
            if (Phase.Length == 0) { Poll(); return; }
            try
            {
                if (Phase == "entering")
                {
                    if (EditorApplication.isPlaying) { Phase = "playing"; _driver = Run(); _lastFrame = -1; SessionState.SetString(K + "t", Now.ToString("R", CultureInfo.InvariantCulture)); }
                    return;
                }
                if (Phase == "playing")
                {
                    if (!EditorApplication.isPlaying || _driver == null) { Say("FAIL: Play ended or scripts reloaded"); Leave(); return; }
                    if (Now > double.Parse(SessionState.GetString(K + "t", "0"), CultureInfo.InvariantCulture) + 280.0) { Say("FAIL: 280 s in Play"); Leave(); return; }
                    if (EditorApplication.isPaused) EditorApplication.isPaused = false;
                    if (Time.frameCount == _lastFrame) return;
                    _lastFrame = Time.frameCount;
                    bool more;
                    try { more = _driver.MoveNext(); }
                    catch (Exception e) { Say("FAIL: " + e); more = false; }
                    if (!more) Leave();
                    return;
                }
                if (Phase == "exiting")
                {
                    if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;
                    var open = SessionState.GetString(K + "open", "").Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    PayoRequestWatcher.Restore(open, SessionState.GetString(K + "active", ""));
                    Answer();
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[Payo] break shots threw: " + e);
                Say("FAIL: " + e.Message);
                try { if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode(); } catch { }
                Answer();
            }
        }

        private static bool OthersPending()
        {
            // The VFX session holds this while it installs and films (coordinator, 2026-10-19).
            if (File.Exists("Temp/cutscene-film.lock")) return true;
            // Another worker's request waits its turn only if it was written before ours: two watchers that each wait for
            // every other request would otherwise wait for each other for ever (seen 2026-10-19).
            var mine = File.Exists(Request) ? File.GetLastWriteTimeUtc(Request) : DateTime.MaxValue;
            foreach (var f in Directory.Exists("Temp") ? Directory.GetFiles("Temp", "*.request") : Array.Empty<string>())
                if (!f.Replace('\\', '/').EndsWith("breakcam.request", StringComparison.Ordinal) && File.GetLastWriteTimeUtc(f) <= mine) return true;
            return PayoPerfProbe.Busy || PayoPlayProbe.Busy || PayoPaddyShots.Busy || PayoPlazaShots.Busy || PayoMoonShots.Busy;
        }

        private static void Poll()
        {
            if (Now < _nextPoll) return;
            _nextPoll = Now + 1.0;
            if (!File.Exists(Request)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (OthersPending()) return;
            string text = File.ReadAllText(Request);
            File.Delete(Request);
            if (File.Exists(Done)) File.Delete(Done);
            Log.Clear();
            var words = text.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            SessionState.SetString(K + "verb", words.Length > 0 ? words[0] : "film");
            string label = new string((words.Length > 1 ? words[1] : "breakcam").Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray());
            SessionState.SetString(K + "label", label.Length > 0 ? label : "breakcam");
            SessionState.SetString(K + "extra", words.Length > 2 ? string.Join(" ", words.Skip(2)) : "");

            if (Verb == "survey")
            {
                try { Survey(); } catch (Exception e) { Say("FAIL: " + e); }
                Answer();
                return;
            }

            var open = new List<string>();
            string active = SceneManager.GetActiveScene().path;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isDirty) { Say("FAIL: scene dirty (" + scene.name + "); nothing was run"); Answer(); return; }
                if (scene.isLoaded && !string.IsNullOrEmpty(scene.path)) open.Add(scene.path);
            }
            SessionState.SetString(K + "open", string.Join("|", open));
            SessionState.SetString(K + "active", active ?? "");
            EditorSceneManager.OpenScene(PayoSceneBuilder.ScenePath, OpenSceneMode.Single);
            Phase = "entering";
            EditorApplication.EnterPlaymode();
        }

        private static void Leave() { _driver = null; Phase = "exiting"; EditorApplication.ExitPlaymode(); }

        private static void Say(string line) { Log.Add(line); }

        private static void Answer()
        {
            bool failed = Log.Any(l => l.StartsWith("FAIL", StringComparison.Ordinal));
            try { File.WriteAllText(Done, (failed ? "FAIL" : "OK") + " " + Verb + " " + Label + "\n" + string.Join("\n", Log) + "\n"); } catch { }
            Phase = "";
        }

        private static string V(Vector3 v) => $"({v.x:0.0},{v.y:0.0},{v.z:0.0})";

        // ------------------------------------------------------------------ survey (edit mode)

        private static void Survey()
        {
            var scene = SceneManager.GetActiveScene();
            Say("scene " + scene.path);
            var stage = Object.FindFirstObjectByType<ArenaStage>();
            if (stage != null)
            {
                Say($"stage at {V(stage.transform.position)} radius {stage.Radius:0.0} layouts {stage.LayoutCount} pieces {stage.Pieces.Length} sink {stage.SinkDepth}");
                for (int l = 0; l < stage.LayoutCount; l++)
                {
                    var b = new Bounds(); bool any = false; float top = -99f;
                    foreach (var p in stage.Pieces)
                    {
                        if (p?.Shapes == null || l >= p.Shapes.Length || !p.Shapes[l].Exists) continue;
                        var s = p.Shapes[l];
                        top = Mathf.Max(top, Mathf.Max(s.Top, s.Top1));
                        var c = new Vector3(s.X, s.Top, s.Z);
                        if (!any) { b = new Bounds(c, new Vector3(s.SX, 0, s.SZ)); any = true; } else b.Encapsulate(new Bounds(c, new Vector3(s.SX, 0, s.SZ)));
                    }
                    Say($"  layout {l} {stage.Layouts[l].Name}: blocks span {V(b.min)}..{V(b.max)}, highest top {top:0.0}, can {stage.Layouts[l].CanHeight:0.0}");
                }
            }
            var night = Object.FindFirstObjectByType<PayoNight>();
            if (night != null)
            {
                Vector3 toSun = -(night.SunDay * Vector3.forward), toMoon = -(night.SunNight * Vector3.forward);
                Say($"sun (day) towards {V(toSun)} bearing {Mathf.Atan2(toSun.x, toSun.z) * Mathf.Rad2Deg:0} elev {Mathf.Asin(toSun.y) * Mathf.Rad2Deg:0}; night light from {V(toMoon)}; lamps {night.Lamps.Length}, glow {night.NightGlow.Length}, nightOnly {night.NightOnly.Length}");
            }
            var moon = Object.FindFirstObjectByType<PayoMoon>();
            if (moon != null) Say($"moon bearing {moon.Bearing} elev {moon.Elevation} dist {moon.Distance} centre {V(moon.Centre)}");
            var flames = Object.FindObjectsByType<PayoFlame>(FindObjectsSortMode.None);
            Say($"flames {flames.Length}: " + string.Join(" ", flames.Select(f => f.name + V(f.transform.position))));

            if (Extra.Contains("brief")) goto Dress;
            string[] keys = { "bulul", "fall_", "head_fall", "house", "cliff", "gate", "court_platform", "fire_bowl", "torch", "lantern", "Terraces", "pool_basin", "offering", "gong", "pillar", "column", "post", "wall", "tree_", "Plaza", "lift", "ferry", "piston", "Geyser", "Spawn" };
            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var key in keys)
            {
                var hits = all.Where(t => t.name.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0 && (t.parent == null || t.parent.name.IndexOf(key, StringComparison.OrdinalIgnoreCase) < 0)).Take(60).ToList();
                if (hits.Count == 0) continue;
                var sb = new StringBuilder();
                sb.Append($"[{key}] {hits.Count}: ");
                foreach (var t in hits)
                {
                    var rs = t.GetComponentsInChildren<Renderer>(true);
                    if (rs.Length == 0) { sb.Append($"{t.name}@{V(t.position)} "); continue; }
                    var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                    sb.Append($"{t.name}[{V(b.min)}..{V(b.max)}] ");
                }
                Say(sb.ToString());
            }
            // Each dressing group's instances near the court (within 140 m), with their bounds.
            Dress:
            var dressing = GameObject.Find("Payo/Dressing");
            if (dressing != null)
                foreach (Transform group in dressing.transform)
                {
                    var sb = new StringBuilder();
                    int shown = 0, total = 0;
                    foreach (Transform inst in group)
                    {
                        total++;
                        var rs = inst.GetComponentsInChildren<Renderer>(true);
                        if (rs.Length == 0) continue;
                        var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                        if (new Vector2(b.center.x, b.center.z).magnitude > 140f || shown >= 40) continue;
                        shown++;
                        sb.Append($"{V(b.center)}s{V(b.size)} ");
                    }
                    var cols = group.GetComponentsInChildren<Collider>(true).Length;
                    Say($"DRESS {group.name}: {total} ({cols} colliders) near: {sb}");
                }
            foreach (var c in Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(c => !c.isTrigger).Take(160))
                Say($"COL {c.GetType().Name} {c.transform.parent?.name}/{c.name} layer {LayerMask.LayerToName(c.gameObject.layer)} {V(c.bounds.min)}..{V(c.bounds.max)}");
            // The top of the hierarchy, with the size of each root's drawn world.
            foreach (var root in scene.GetRootGameObjects())
            {
                var rs = root.GetComponentsInChildren<Renderer>(true);
                var cs = root.GetComponentsInChildren<Collider>(true);
                string kids = string.Join(",", root.transform.Cast<Transform>().Take(40).Select(c => c.name));
                Say($"ROOT {root.name}: {rs.Length} renderers, {cs.Length} colliders; children {kids}");
            }
        }

        // ------------------------------------------------------------------ Play

        private const int ClipLayer = 31;
        private const float Dt = 1.0f / 30.0f, SweepRadius = 0.45f, BoundsMargin = 0.25f;

        private static IEnumerator Wait(float seconds) { for (float t = Time.realtimeSinceStartup; Time.realtimeSinceStartup - t < seconds;) yield return null; }

        private static IEnumerator Run()
        {
            if (Verb == "live") { var l = Live(); while (l.MoveNext()) yield return l.Current; yield break; }
            float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 30f && (ArenaStage.Instance == null || PayoBreakDirector.Instance == null || GameServices.Round == null || !GameServices.Round.RoundActive)) yield return null;
            var w = Wait(1.0f); while (w.MoveNext()) yield return null;
            var stage = ArenaStage.Instance; var director = PayoBreakDirector.Instance;
            if (stage == null) { Say("FAIL: no ArenaStage"); yield break; }
            if (director == null) { Say("FAIL: the director did not install itself"); yield break; }
            var rig = Object.FindAnyObjectByType<CameraRig>();
            if (rig != null) rig.enabled = false;
            foreach (var b in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None)) b.enabled = false;
            if (Verb == "clip") { var c = Clip(stage, director); while (c.MoveNext()) yield return c.Current; }
            else if (Verb == "film") { var f = Film(stage, director); while (f.MoveNext()) yield return f.Current; }
            else Say("FAIL: verb not known: " + Verb);
        }

        private static void PoseStage(ArenaStage stage, in ArenaStage.BreakBeats b)
        {
            stage.PoseTravel(b.From, b.To, b.Travel, Mathf.Clamp01((b.Age - b.ScanStart) / 0.5f), stage.ScanRadius(b), ArenaStage.UndockJolt(b.Age - b.Undock));
        }

        // ------------------------------------------------------------------ clip

        private sealed class Track
        {
            public string Name;
            public int Samples, Sweeps, Inside, InBounds;
            public float Surface = 99f, Box = 99f;
            public string SurfaceAt = "", BoxAt = "", FirstFail = "";
            public readonly Dictionary<string, (int n, float t0, float t1)> Hits = new Dictionary<string, (int, float, float)>();
            public void Hit(string what, float t) { Hits[what] = Hits.TryGetValue(what, out var h) ? (h.n + 1, Mathf.Min(h.t0, t), Mathf.Max(h.t1, t)) : (1, t, t); }
        }

        private static readonly string[] ClipSkip = { "sky", "cloud", "haze", "wisp", "ray", "star", "firefl", "mist", "foam", "spray", "smoke", "blade", "flora", "cover_", "night life", "meteor", "fx", "glow", "hologram", "flame" };

        private static bool Skip(Renderer r)
        {
            string n = r.gameObject.name.ToLowerInvariant();
            string p = r.transform.parent != null ? r.transform.parent.name.ToLowerInvariant() : "";
            foreach (var k in ClipSkip)
                if (n.Contains(k) || p.Contains(k)) return true;
            if (PayoBreakDirector.GameplayItem(r)) return true;
            var size = r.bounds.size;
            return Mathf.Max(size.x, Mathf.Max(size.y, size.z)) > 2500f;
        }

        private static IEnumerator Clip(ArenaStage stage, PayoBreakDirector director)
        {
            // Temporary mesh colliders over every drawn mesh, on a layer of their own, so the sweeps meet what is drawn
            // (the land has no colliders of its own). Made in Play only; gone with it.
            var made = new List<GameObject>();
            var stones = new List<Renderer>();
            int skipped = 0;
            var solids = new HashSet<Renderer>();
            var batched = new HashSet<Mesh>();
            foreach (var piece in stage.Pieces)
                if (piece?.Solids != null)
                    foreach (var solid in piece.Solids)
                        if (solid != null) foreach (var r in solid.GetComponentsInChildren<MeshRenderer>(true)) solids.Add(r);
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                bool stone = solids.Contains(r);
                if (!stone && r.transform.IsChildOf(stage.transform)) continue;   // holograms and pads: not solid
                if (stone) stones.Add(r);
                if (!stone && (!r.enabled || !r.gameObject.activeInHierarchy)) continue;
                if (!stone && Skip(r)) { skipped++; continue; }
                var filter = r.GetComponent<MeshFilter>();
                var mesh = filter != null ? filter.sharedMesh : null;
                if (mesh == null || mesh.vertexCount < 3) continue;
                var lod = r.GetComponentInParent<LODGroup>();
                if (lod != null)
                {
                    // Only a LOD group's first level: the far one stands in the same place.
                    var lods = lod.GetLODs();
                    if (lods.Length > 1 && !lods[0].renderers.Contains(r)) continue;
                }
                var go = new GameObject("~clip probe " + r.name) { layer = ClipLayer, hideFlags = HideFlags.DontSave };
                // A statically batched renderer's mesh in Play is the scene's combined mesh, already in world space:
                // one probe per combined mesh, at the world's origin.
                if (r.isPartOfStaticBatch) { if (!batched.Add(mesh)) { Object.Destroy(go); continue; } }
                else go.transform.SetParent(r.transform, false);
                var mc = go.AddComponent<MeshCollider>();
                try { mc.sharedMesh = mesh; } catch { Object.Destroy(go); continue; }
                made.Add(go);
            }
            var compact = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => PayoBreakDirector.CompactSolid(r, stage)).Select(r => r.bounds).ToList();
            Say($"clip: {made.Count} temporary mesh colliders ({skipped} effect/sky renderers left out), {compact.Count} compact solids, {stones.Count} stone renderers");
            yield return null;

            bool backfaces = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;
            int mask = 1 << ClipLayer;
            int layouts = stage.LayoutCount;
            float d = ArenaStage.BreakSeconds;
            var tracks = new Dictionary<string, Track>();
            var prev = new Dictionary<string, Vector3>();
            int pairs = 0;
            var shots = new List<(int shot, int variant)>();
            for (int s = 0; s < PayoBreakDirector.ShotCount; s++) { shots.Add((s, 0)); shots.Add((s, 1)); }
            shots.Add((PayoBreakDirector.Halftime, 0)); shots.Add((PayoBreakDirector.Still, 0));
            var up = new RaycastHit[1];
            var boxes = new List<Bounds>();
            for (int from = 0; from < layouts; from++)
            for (int to = 0; to < layouts; to++)
            {
                if (from == to) continue;
                pairs++;
                prev.Clear();
                int steps = Mathf.RoundToInt(d / Dt);
                for (int k = 0; k <= steps; k++)
                {
                    float t = Mathf.Min(d, k * Dt);
                    var beats = PayoBreakDirector.Beats(from, to, t, d, false);
                    PoseStage(stage, beats);
                    Physics.SyncTransforms();
                    boxes.Clear(); boxes.AddRange(compact);
                    foreach (var r in stones) if (r != null && r.enabled && r.gameObject.activeInHierarchy) boxes.Add(r.bounds);
                    foreach (var (shot, variant) in shots)
                    {
                        float seatFrom = shot == PayoBreakDirector.Halftime ? PayoBreakDirector.HalftimeReturn(beats) : d - PayoBreakDirector.HandbackSeconds;
                        bool perSeat = shot != PayoBreakDirector.Still && t > seatFrom - Dt;
                        string name = shot == PayoBreakDirector.Halftime ? "halftime" : shot == PayoBreakDirector.Still ? "still" : PayoBreakDirector.ShotNames[shot] + (variant == 0 ? " (side A)" : " (side B)");
                        if (!tracks.TryGetValue(name, out var track)) tracks[name] = track = new Track { Name = name };
                        for (int seat = 0; seat < (perSeat ? 4 : 1); seat++)
                        for (int view = 0; view < (perSeat ? 2 : 1); view++)
                        {
                            string key = $"{shot}/{variant}/{seat}/{view}";
                            var frame = director.FrameAt(shot, variant, beats, seat, 0, view == 1);
                            Vector3 eye = frame.Eye;
                            string where = $"{stage.Layouts[from].Name}>{stage.Layouts[to].Name} t {t:0.00} seat {seat}{(view == 1 ? " tpp" : "")} eye {V(eye)}";
                            track.Samples++;
                            if (!prev.TryGetValue(key, out var last)) prev.TryGetValue($"{shot}/{variant}/0/0", out last);
                            if (last != Vector3.zero)
                            {
                                Vector3 step = eye - last;
                                if (step.magnitude > 1e-3f && Physics.SphereCast(last, SweepRadius, step.normalized, out var hit, step.magnitude, mask, QueryTriggerInteraction.Collide))
                                { track.Sweeps++; track.Hit("sweep " + hit.collider.name, t); if (track.FirstFail.Length == 0) track.FirstFail = $"sweep hit {hit.collider.name} at {where}"; }
                            }
                            prev[key] = eye;
                            // Inside a closed mesh, or under the land: the first thing straight up is a back face.
                            if (Physics.RaycastNonAlloc(eye, Vector3.up, up, 400f, mask, QueryTriggerInteraction.Collide) > 0 && Vector3.Dot(up[0].normal, Vector3.up) > 0.0f)
                            { track.Inside++; track.Hit("inside " + up[0].collider.name, t); if (track.FirstFail.Length == 0) track.FirstFail = $"under/inside {up[0].collider.name} at {where}"; }
                            // Clearance to the nearest drawn surface, up to 4 m.
                            float lo = 0f, hi = 4f;
                            if (!Physics.CheckSphere(eye, hi, mask, QueryTriggerInteraction.Collide)) lo = hi;
                            else for (int i = 0; i < 7; i++) { float mid = (lo + hi) * 0.5f; if (Physics.CheckSphere(eye, mid, mask, QueryTriggerInteraction.Collide)) hi = mid; else lo = mid; }
                            if (lo < track.Surface) { track.Surface = lo; track.SurfaceAt = where; }
                            float box = 99f;
                            foreach (var b in boxes) { float sq = b.SqrDistance(eye); if (sq < box * box) box = Mathf.Sqrt(sq); }
                            if (box < track.Box) { track.Box = box; track.BoxAt = where; }
                            if (box < BoundsMargin) { track.InBounds++; track.Hit("bounds", t); if (track.FirstFail.Length == 0) track.FirstFail = $"within {BoundsMargin} m of a solid's bounds at {where}"; }
                        }
                    }
                }
                yield return null;
            }
            Physics.queriesHitBackfaces = backfaces;
            foreach (var go in made) if (go != null) Object.Destroy(go);

            int total = 0, fails = 0; float least = 99f, leastBox = 99f;
            foreach (var tr in tracks.Values)
            {
                total += tr.Samples;
                int f = tr.Sweeps + tr.Inside + tr.InBounds; fails += f;
                least = Mathf.Min(least, tr.Surface); leastBox = Mathf.Min(leastBox, tr.Box);
                Say($"{(f > 0 ? "FAIL " : "  ok ")}{tr.Name}: {tr.Samples} samples, sweeps through a surface {tr.Sweeps}, inside {tr.Inside}, in a solid's bounds {tr.InBounds}; " +
                    $"least clearance to a drawn surface {tr.Surface:0.00} m ({tr.SurfaceAt}); to a compact solid's bounds {tr.Box:0.00} m ({tr.BoxAt}){(tr.FirstFail.Length > 0 ? "; first: " + tr.FirstFail : "")}");
                foreach (var h in tr.Hits.OrderByDescending(x => x.Value.n).Take(6)) Say($"      {h.Key}: {h.Value.n} samples, t {h.Value.t0:0.00} to {h.Value.t1:0.00}");
            }
            Say($"SUMMARY: {tracks.Count} paths ({PayoBreakDirector.ShotCount} shots x 2 sides, half time, the still) x {pairs} layout pairs, hand-backs to all 4 spawns in first and third person, " +
                $"{total} samples at {1f / Dt:0} Hz; failures {fails}; least clearance to a drawn surface {least:0.00} m, to a compact solid's bounds {leastBox:0.00} m (sweep ball {SweepRadius} m)");
        }

        // ------------------------------------------------------------------ film

        private static IEnumerator Film(ArenaStage stage, PayoBreakDirector director)
        {
            var breakCam = Object.FindAnyObjectByType<ArenaBreakCamera>(FindObjectsInactive.Include);
            var cam = breakCam != null ? breakCam.GetComponent<Camera>() : null;
            if (cam == null) { Say("FAIL: no break camera"); yield break; }
            var night = Object.FindAnyObjectByType<PayoNight>();
            string folder = $"{Root}/{Label}";
            Directory.CreateDirectory(folder);
            int from = 0, to = 1;
            var words = Extra.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in words) if (word.StartsWith("pair=", StringComparison.Ordinal) && word.Length >= 8) { from = word[5] - '0'; to = word[7] - '0'; }
            var only = words.Where(x => x.StartsWith("shot=", StringComparison.Ordinal)).Select(x => int.Parse(x.Substring(5), CultureInfo.InvariantCulture)).ToList();
            int variant = words.Contains("mirror") ? 1 : 0;
            int frames = 16;
            float d = ArenaStage.BreakSeconds;
            var big = new RenderTexture(960, 540, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var small = new RenderTexture(480, 270, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var list = new List<int>();
            for (int s = 0; s < PayoBreakDirector.ShotCount; s++) list.Add(s);
            list.Add(PayoBreakDirector.Halftime);
            if (only.Count > 0) list = only;
            Say($"film: pair {stage.Layouts[from].Name}>{stage.Layouts[to].Name}, side {(variant == 0 ? "A" : "B")}");
            foreach (int shot in list)
            {
                string name = shot == PayoBreakDirector.Halftime ? "halftime" : shot == PayoBreakDirector.Still ? "still" : PayoBreakDirector.ShotNames[shot].Replace(' ', '_');
                var sheet = new Texture2D(480 * 4, 270 * ((frames + 3) / 4), TextureFormat.RGB24, false);
                for (int k = 0; k < frames; k++)
                {
                    float t = d * k / (frames - 1);
                    if (night != null) night.Hold = shot == PayoBreakDirector.Halftime ? PayoNight.DuskAt(t, d) : 0f;
                    // Frames for the night and its life to take the hold.
                    for (int f = 0; f < 3; f++) yield return null;
                    var beats = PayoBreakDirector.Beats(from, to, t, d, shot == PayoBreakDirector.Halftime);
                    PoseStage(stage, beats);
                    var frame = director.FrameAt(shot, variant, beats, 1, 0);
                    cam.transform.SetPositionAndRotation(frame.Eye, frame.Rotation);
                    cam.fieldOfView = frame.Fov;
                    var keep = cam.targetTexture;
                    cam.targetTexture = big; cam.Render(); cam.targetTexture = keep;
                    Graphics.Blit(big, small);
                    var prevActive = RenderTexture.active;
                    RenderTexture.active = big;
                    var tex = new Texture2D(960, 540, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); tex.Apply();
                    File.WriteAllBytes($"{folder}/{name}_{k:00}.png", tex.EncodeToPNG());
                    Object.Destroy(tex);
                    RenderTexture.active = small;
                    int col = k % 4, row = k / 4;
                    sheet.ReadPixels(new Rect(0, 0, 480, 270), col * 480, sheet.height - 270 * (row + 1));
                    RenderTexture.active = prevActive;
                    if (k % 5 == 0 || shot == PayoBreakDirector.Halftime) Say($"{name} t {t:0.00}: eye {V(frame.Eye)} fov {frame.Fov:0} night {PayoNight.Factor:0.00}");
                }
                sheet.Apply();
                string path = $"{folder}/sheet_{name}.png";
                File.WriteAllBytes(path, sheet.EncodeToPNG());
                Object.Destroy(sheet);
                Say("sheet " + path);
            }
            if (night != null) { night.Hold = -1f; night.Apply(0f); }
            big.Release(); small.Release();
        }

        // ------------------------------------------------------------------ live

        private static IEnumerator Live()
        {
            float t0 = Time.realtimeSinceStartup;
            var rules = CustomGameRules.Defaults(SceneFlow.SelectedRules != null ? SceneFlow.SelectedRules.Mode : GameMode.HeroStrike);
            rules.Rounds = 6; rules.RoundSeconds = CustomGameRules.MinRoundSeconds;
            SceneFlow.PinSelectedRules(rules);
            while (Time.realtimeSinceStartup - t0 < 60f && (GameServices.Round == null || !GameServices.Round.RoundActive || PresentationClock.Held)) yield return null;
            var round = GameServices.Round; var match = GameServices.Match;
            if (round == null || !round.RoundActive) { Say("FAIL: no live round"); yield break; }
            var stage = ArenaStage.Instance;
            // The round's own clock run out, as the map's F9 does (internal to the game's assembly).
            var end = typeof(RoundDirector).GetMethod("ApplyPresentationTime", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            string folder = $"{Root}/{Label}";
            Directory.CreateDirectory(folder);
            var rt = new RenderTexture(960, 540, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            int breaks = 0;
            while (breaks < 3 && Time.realtimeSinceStartup - t0 < 250f)
            {
                var w = Wait(1.5f); while (w.MoveNext()) yield return null;
                int completed = match.RoundNumber;
                end?.Invoke(round, new object[] { 0f });
                float began = Time.realtimeSinceStartup;
                var hp = HalftimePresentation.Instance;
                while (Time.realtimeSinceStartup - began < 40f && (hp == null || !hp.StageShowPlaying)) { hp = HalftimePresentation.Instance; yield return null; }
                if (hp == null || !hp.StageShowPlaying) { Say($"FAIL: no break show after round {completed}"); yield break; }
                breaks++;
                int shot = hp.IsHalftime ? PayoBreakDirector.Halftime : PayoBreakDirector.Pick(hp.MatchId, hp.CompletedRound);
                string name = hp.IsHalftime ? "halftime" : PayoBreakDirector.ShotNames[shot].Replace(' ', '_');
                Say($"break after round {completed}: {(hp.IsHalftime ? "HALF TIME" : "ordinary")}, match {hp.MatchId}, shot {name}, side {PayoBreakDirector.Variant(hp.MatchId, hp.CompletedRound)}");
                if (stage.TryBreak(out var real))
                {
                    var mine = PayoBreakDirector.Beats(real.From, real.To, real.Age, real.Duration, real.Halftime);
                    Say($"  beats real/director's copy: scan {real.ScanStart:0.00}/{mine.ScanStart:0.00} undock {real.Undock:0.00}/{mine.Undock:0.00} move {real.MoveStart:0.00}-{real.MoveEnd:0.00}/{mine.MoveStart:0.00}-{mine.MoveEnd:0.00} reveal {real.Reveal:0.00}/{mine.Reveal:0.00} duration {real.Duration:0.00}");
                }
                float next = 0f; int k = 0;
                Vector3 lastEye = Vector3.zero; Quaternion lastRot = Quaternion.identity; float lastFov = 0f;
                while (hp.Active && hp.StageShowPlaying)
                {
                    var cam = ArenaBreakCamera.Showing;
                    if (cam != null && hp.StageShowAge >= next)
                    {
                        next += 0.5f;
                        var keep = cam.targetTexture; cam.targetTexture = rt; cam.Render(); cam.targetTexture = keep;
                        var prevActive = RenderTexture.active; RenderTexture.active = rt;
                        var tex = new Texture2D(960, 540, TextureFormat.RGB24, false);
                        tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); tex.Apply();
                        RenderTexture.active = prevActive;
                        File.WriteAllBytes($"{folder}/b{breaks}_{name}_{k++:00}.png", tex.EncodeToPNG());
                        Object.Destroy(tex);
                    }
                    if (cam != null) { lastEye = cam.transform.position; lastRot = cam.transform.rotation; lastFov = cam.fieldOfView; }
                    yield return null;
                }
                Say($"  {k} frames; director's last age {PayoBreakDirector.LastAge:0.00}, night {PayoNight.Factor:0.00}; guard frames so far {PayoBreakDirector.GuardFrames} (most {PayoBreakDirector.GuardMost:0.00} m)");
                began = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - began < 15f && !(round.RoundActive && !PresentationClock.Held)) yield return null;
                for (int f = 0; f < 3; f++) yield return null;
                var rig = Object.FindAnyObjectByType<CameraRig>();
                var game = rig != null ? rig.Camera : Camera.main;
                if (game != null)
                {
                    var me = rig != null ? rig.Following : null;
                    Say($"  HAND-BACK: break camera's last {V(lastEye)} fov {lastFov:0.0}; round camera {V(game.transform.position)} fov {game.fieldOfView:0.0}: " +
                        $"{Vector3.Distance(lastEye, game.transform.position):0.00} m and {Quaternion.Angle(lastRot, game.transform.rotation):0.0} degrees apart; " +
                        $"eye over feet {(me != null ? (game.transform.position.y - me.transform.position.y).ToString("0.00") : "?")} m (director assumes {PayoBreakDirector.EyeOverFloor:0.00}); mode {(rig != null ? rig.Mode.ToString() : "?")}");
                }
            }
            rt.Release();
        }
    }
}
