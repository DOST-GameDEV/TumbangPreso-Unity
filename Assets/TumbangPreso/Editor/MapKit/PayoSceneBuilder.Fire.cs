using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using TumbangPreso.Map;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// THE NIGHT FIRES (2026-10-19). Owner, of the Payo map at night: "could also use some torch lights or those stone
    /// pedestals with fire in them".
    ///
    /// Two new pieces, made and judged on their own first (tools/payo_pieces/fire_kit.py, review sheet
    /// Logs/payo/pieces/fire/fire_v2.png): `fire_pedestal` (a squat carved stone fire stand, 280 triangles) and
    /// `fire_torch` (a bamboo torch with a basket head, 289 triangles), one painted atlas with its normal map, worn as
    /// `payo_fire_kit` on the painted shader (LagoonPainted). The flames are NOT in the models.
    ///
    /// ⚠️ THE FLAME BELONGS TO THE VFX SESSION (owner's call, relayed 2026-10-19: "the fire VFX to the VFX session").
    /// Every fire carries a child `flame_anchor` (`FlameAnchor`) on its coals: its local up is the flame's direction and
    /// its uniform scale is the flame's size in metres (about the flame's height). `AttachFlame` is the ONE place a flame
    /// is put on an anchor: it calls the VFX session's `PayoFire.Attach` (Runtime/Map/PayoFire.cs, theirs: one shared
    /// instanced mesh and the `TumbangPreso/PayoFire` shader, all motion and its dusk turn on the GPU, its renderers off by
    /// day).
    ///
    /// THE NIGHT: every fire also carries a `PayoFlame` (no tongues), its `Order` its bearing round the pit (the bowls'
    /// rule). The four with a real light have it as their `Glow`, which `PayoNightLife.Flames` lights and flickers in that
    /// turn; `PayoFire`'s shader uses the same formula, so each flame catches on the frame its lamp does, with the bowls.
    ///
    /// WHERE (all of it outside the play walls, |x| and |z| 22, so nothing here is in a player's, a slipper's or a
    /// bot's way, and nothing has a collider):
    ///   * the paving round the pit (y -6): torches along the pit's rim lip on both side strips, between the banners and
    ///     clear of the bulul plinths, so a row of small flames rings the court across the pit; pedestals at the south
    ///     strip's two ends (its middle is the market's: coordinator, 2026-10-19, "keep your pieces out of its middle");
    ///   * up the terraces: torches stood on terrace lips found by rays (flat, dry, a drop of a metre or more beside it,
    ///     clear of every piece's bounds), spread round the valley so it sparkles at night.
    /// Every place is seated by a ray onto the ground it is meant for; a place that is not open ground is dropped and
    /// counted.
    ///
    /// LIGHTS ARE EXPENSIVE (the owner is chasing frame drops): only `FireLights` of the fires nearest the court get a
    /// real point light (their `PayoFlame.Glow`, flickered by the night's life in their turn). The rest are emissive only.
    ///
    /// `Temp/fire.off` (a file) builds the map without any of this: the before half of a cost comparison.
    /// Hooked from `Build` after `Plaza`. Returns one line for the build log.
    /// </summary>
    public static partial class PayoSceneBuilder
    {
        /// <summary>Which piece a flame stands on (the VFX session's `PayoFire.Kind` will mirror it).</summary>
        internal enum FireKind { Pedestal, Torch }

        /// <summary>The name of the flame's attach point on every fire.</summary>
        public const string FlameAnchor = "flame_anchor";
        /// <summary>The flame's foot on each piece, metres up from its origin (fire_kit.py `COAL_TOP`, the coals' middle),
        /// and the flame's size there (the anchor's scale: about the flame's height in metres, at the piece's scale 1).</summary>
        private const float PedestalCoal = 1.11f, TorchCoal = 1.97f, PedestalFlame = 0.95f, TorchFlame = 0.6f;
        private const int FireLights = 4;
        private const string FireOffFlag = "Temp/fire.off";

        private static int _fireDropped;
        private static readonly Dictionary<string, int> FireWhy = new Dictionary<string, int>();

        private static string NightFires(Transform root, Dictionary<string, Material> named)
        {
            if (File.Exists(FireOffFlag)) return "night fires: OFF (" + FireOffFlag + " exists)";
            var pedestal = Model("fire_pedestal");
            var torch = Model("fire_torch");
            if (pedestal == null || torch == null) { Warn("night fires: missing Models/fire_pedestal.glb or fire_torch.glb (tools/payo_pieces/fire_run.sh install)"); return "night fires: MISSING models"; }
            _fireDropped = 0; FireWhy.Clear();

            var imported = pedestal.GetComponentsInChildren<Renderer>(true).Select(r => r.sharedMaterial).FirstOrDefault(m => m != null);
            var body = BuildMaterial("payo_fire_kit", new JObject { ["kind"] = "painted", ["vertex_colour"] = false }, imported);

            var group = Group(root, "Fires");
            var made = new List<(GameObject go, float coal)>();

            // ---- the ground to seat on: the court platform's paving (lent a collider) and the art's land and terraces
            var lent = new List<Collider>();
            var court = root.Find("Dressing/court_platform");
            var courtColliders = new HashSet<Collider>();
            if (court != null)
                foreach (var mf in court.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (mf.sharedMesh == null || mf.GetComponent<Collider>() != null) continue;
                    var mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                    lent.Add(mc); courtColliders.Add(mc);
                }
            var taken = Taken(root);
            int plaza = 0, terrace = 0, stair = 0;
            try
            {
                using (var ground = new Ground(root))
                {
                    Physics.SyncTransforms();
                    var plazaGroup = Group(group, "paving");
                    // the lip torches: both side strips, between the banners (z -22, -10, 20, 36), the planters (z -16, 28, 41)
                    // and the bulul plinths (seated at z 6, 5.9 m half; standing at z 48.6, 4.4 m half)
                    foreach (float z in new[] { -27.0f, -4.0f, 15.5f, 32.5f })
                        for (int side = -1; side <= 1; side += 2)
                            if (FirePut(torch, plazaGroup, Nudged(taken, new Vector3(side * 31.35f, PlazaY, z), Vector3.forward, 0.3f), side > 0 ? 270.0f : 90.0f, 1.0f, false, body, courtColliders, null, taken, made)) plaza++;
                    // the south strip's two ends (its middle is the market's)
                    foreach (float s in new[] { -1.0f, 1.0f })
                        if (FirePut(pedestal, plazaGroup, Nudged(taken, new Vector3(s * 41.6f, PlazaY, -31.8f), Vector3.forward, 0.62f), 0.0f, 1.0f, true, body, courtColliders, null, taken, made)) plaza++;

                    // ---- the water stair south of the gate: pedestals at its head, torches in pairs down both its sides
                    var stairGroup = Group(group, "stair");
                    stair = StairFires(ground, pedestal, torch, stairGroup, body, taken, made);

                    // ---- up the terraces: torches on terrace lips, spread round the valley
                    var upGroup = Group(group, "terraces");
                    terrace = TerraceTorches(ground, torch, upGroup, body, taken, made);
                }
            }
            finally
            {
                foreach (var c in lent) if (c != null) Object.DestroyImmediate(c);
                Physics.SyncTransforms();
            }

            // ---- the few real lights: the fires nearest the court, given to their PayoFlame as its Glow
            var lamps = root.Find("NightLights") ?? Group(root, "NightLights");
            int lights = 0;
            foreach (var (go, coal) in made.OrderBy(m => new Vector2(m.go.transform.position.x, m.go.transform.position.z).sqrMagnitude).Take(FireLights))
            {
                var flame = go.GetComponent<PayoFlame>();
                if (flame == null) continue;
                var holder = Group(lamps, "fire light");
                holder.position = go.transform.position + Vector3.up * (coal + 0.6f);
                var light = holder.gameObject.AddComponent<Light>();
                light.type = LightType.Point; light.range = 7.5f; light.color = new Color(1.0f, 0.6f, 0.26f); light.intensity = 0.0f;
                light.shadows = LightShadows.None; light.enabled = false;
                flame.Glow = light;
                flame.Brightness = 1.8f;
                lights++;
            }

            return $"night fires: {plaza} on the paving, {stair} down the water stair, {terrace} up the terraces ({made.Count} fires, {made.Count} '{FlameAnchor}' anchors carrying PayoFire, {lights} with a real light); " +
                   $"{_fireDropped} places dropped ({string.Join(", ", FireWhy.Select(w => w.Key + " x" + w.Value))}); at: " +
                   string.Join(" ", made.Select(m => $"{(m.go.name.Contains("pedestal") ? "P" : "T")}({m.go.transform.position.x:0},{m.go.transform.position.y:0},{m.go.transform.position.z:0})"));
        }

        /// <summary>
        /// ⚠️ THE ONE PLACE A FLAME IS PUT ON A FIRE. `anchor` is the fire's `flame_anchor`: its foot on the coals, its local
        /// up the flame's direction, its uniform scale the flame's size (metres, about its height); `size` is that size in
        /// world metres (the piece's own scale included), `kind` the piece it stands on. Returns the transforms the night
        /// should show and breathe (`PayoFlame.Tongues`); the VFX session's flame lights itself, so: none.
        /// </summary>
        private static Transform[] AttachFlame(Transform anchor, float size, FireKind kind)
        {
            // The VFX session's painted flame (Runtime/Map/PayoFire.cs, theirs): size 0 takes its height from the anchor's own
            // scale, which `FirePut` sets. It lights itself on the GPU in the bowls' turn (the same formula as PayoNightLife), so
            // the night has nothing of it to switch or breathe: no tongues. The fire's PayoFlame stays for its lamp.
            PayoFire.Attach(anchor, 0.0f, kind == FireKind.Pedestal ? PayoFire.Kind.Pedestal : PayoFire.Kind.Torch);
            return System.Array.Empty<Transform>();
        }

        private static void FireNo(string why) { _fireDropped++; FireWhy[why] = FireWhy.TryGetValue(why, out int n) ? n + 1 : 1; }

        /// <summary>Every placed piece's footprint (its renderers' bounds), so a fire is never stood inside one.
        /// The land, the terraces, the court platform and anything over 25 m (a whole building's yard) are ground, not pieces.</summary>
        private static List<Bounds> Taken(Transform root)
        {
            var list = new List<Bounds>();
            // every group of pieces under the map's root (the dressing, the plaza, the flora, the stage, a market...), not
            // the ground, the night's own objects or the gameplay's invisible volumes
            var notPieces = new HashSet<string> { "Terraces", "Fires", "Night", "NightLights", "Bounds", "KillPlane", "~Match", "SpawnPoints", "Mist", "Sound", "Effects", "BreakCamera", "StandIn" };
            foreach (Transform g in root)
            {
                if (notPieces.Contains(g.name)) continue;
                foreach (var r in g.GetComponentsInChildren<Renderer>(true))
                {
                    if (r is ParticleSystemRenderer) continue;
                    var b = r.bounds;
                    float big = Mathf.Max(b.size.x, b.size.z);
                    if (big > 25.0f || big < 0.6f) continue;
                    bool ground = false;
                    for (var t = r.transform; t != null && t != root; t = t.parent)
                        if (IsLand(t.name) || t.name == "court_platform" || t.name == "Terraces") { ground = true; break; }
                    if (!ground) list.Add(b);
                }
            }
            return list;
        }

        /// <summary>A hand-picked place slid along `along` by up to 3 m either way to the nearest spot clear of every piece
        /// (unchanged if none is: the seat then drops it and says so).</summary>
        private static Vector3 Nudged(List<Bounds> taken, Vector3 at, Vector3 along, float radius)
        {
            foreach (float o in new[] { 0.0f, 1.0f, -1.0f, 2.0f, -2.0f, 3.0f, -3.0f })
                if (Clear(taken, at + along * o, radius)) return at + along * o;
            return at;
        }

        private static bool Clear(List<Bounds> taken, Vector3 at, float radius)
        {
            var me = new Bounds(at + Vector3.up * 1.0f, new Vector3(radius * 2.0f, 2.0f, radius * 2.0f));
            foreach (var b in taken) if (b.Intersects(me)) return false;
            return true;
        }

        /// <summary>One fire: the piece seated on the ground under `at` (the court's paving when `paving` is given, else the
        /// art's land through `ground`), its `flame_anchor` with a flame from `AttachFlame`, its `PayoFlame`, shadows only near
        /// the court. False (and nothing made) if the ground under it is not open, flat ground of the kind asked for.</summary>
        private static bool FirePut(GameObject prefab, Transform parent, Vector3 at, float yaw, float scale, bool isPedestal, Material body,
                                    HashSet<Collider> paving, Ground ground, List<Bounds> taken, List<(GameObject, float)> made)
        {
            float radius = (isPedestal ? 0.62f : 0.3f) * scale;
            if (!Physics.Raycast(new Vector3(at.x, at.y + 40.0f, at.z), Vector3.down, out var hit, 120.0f, ~0, QueryTriggerInteraction.Ignore)) { FireNo("nothing under it"); return false; }
            if (paving != null)
            {
                if (!paving.Contains(hit.collider)) { FireNo("not on the paving (" + hit.collider.name + ")"); return false; }
                if (Mathf.Abs(hit.point.y - at.y) > 0.15f || hit.normal.y < 0.97f) { FireNo("paving at the wrong height"); return false; }
            }
            else if (ground != null && (!ground.IsGround(hit.collider) || ground.IsWater(hit) || hit.normal.y < 0.94f)) { FireNo("not dry flat land"); return false; }
            Vector3 seat = hit.point;
            if (!Clear(taken, seat, radius)) { FireNo(paving != null ? $"inside a piece at ({seat.x:0},{seat.z:0})" : "inside a piece"); return false; }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.position = seat;
            go.transform.rotation = Quaternion.Euler(0.0f, yaw, 0.0f);
            go.transform.localScale = Vector3.one * scale;
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = body;
                r.sharedMaterials = mats;
            }
            Finish(go, true);
            bool near = new Vector2(seat.x, seat.z).magnitude < 60.0f;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = near ? ShadowCastingMode.On : ShadowCastingMode.Off;
                r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }

            // the attach point: on the coals, up the flame's way, scaled to the flame's size
            float coal = isPedestal ? PedestalCoal : TorchCoal;
            var anchor = new GameObject(FlameAnchor).transform;
            anchor.SetParent(go.transform, false);
            anchor.localPosition = new Vector3(0.0f, coal, 0.0f);
            anchor.localRotation = Quaternion.identity;
            anchor.localScale = Vector3.one * (isPedestal ? PedestalFlame : TorchFlame);
            GameObjectUtility.SetStaticEditorFlags(anchor.gameObject, 0);

            var fire = go.AddComponent<PayoFlame>();
            fire.Order = Mathf.Repeat(Mathf.Atan2(seat.x, seat.z) / (Mathf.PI * 2.0f), 1.0f);
            fire.Tongues = AttachFlame(anchor, (isPedestal ? PedestalFlame : TorchFlame) * scale, isPedestal ? FireKind.Pedestal : FireKind.Torch);
            fire.Rest = fire.Tongues.Select(t => t.localScale).ToArray();
            fire.RestYaw = fire.Tongues.Select(t => t.localEulerAngles.y).ToArray();
            fire.Height = (isPedestal ? PedestalFlame : TorchFlame) * scale;

            taken.Add(new Bounds(seat + Vector3.up, new Vector3(radius * 2.0f, 2.0f, radius * 2.0f)));
            made.Add((go, coal * scale));
            return true;
        }

        /// <summary>
        /// THE WATER STAIR (payo_water_stair: the channel that runs south from the gate, x 0, down through the town's
        /// terraces). Rows every 12 m from z -64 to -230; in each row, each side, the first dry flat open ground out from
        /// the channel (scanning |x| 2 to 16 m) gets a fire: a pedestal pair at the head (the first row placed), torches after.
        /// </summary>
        private static int StairFires(Ground ground, GameObject pedestal, GameObject torch, Transform parent, Material body, List<Bounds> taken, List<(GameObject, float)> made)
        {
            if (!ground.Any) return 0;
            int count = 0;
            bool head = true;
            for (float z = -64.0f; z >= -230.0f; z -= 12.0f)
            {
                int row = 0;
                for (int side = -1; side <= 1; side += 2)
                    for (float ax = 2.0f; ax <= 16.0f; ax += 0.5f)
                    {
                        float x = side * ax;
                        if (!ground.Down(x, z, out var hit) || !ground.IsGround(hit.collider) || ground.IsWater(hit) || hit.normal.y < 0.92f) continue;
                        if (!Clear(taken, hit.point, head ? 0.7f : 0.35f)) continue;
                        if (FirePut(head ? pedestal : torch, parent, hit.point, side > 0 ? 270.0f : 90.0f, head ? 1.0f : 1.1f, head, body, null, ground, taken, made)) { count++; row++; }
                        break;
                    }
                if (row > 0) head = false;
            }
            return count;
        }

        /// <summary>
        /// Torches on the terraces' lips: rings round the pit from 48 to 230 m out, a place every ~22 m of arc, kept where a
        /// ray finds dry flat land (not a paddy's water) with a drop of 1.1 m or more within 1.8 m on one side (the lip),
        /// then stood 0.7 m back from that edge. At least 16 m apart, at most 34. The market's terrace and the gate south of
        /// the court are kept clear.
        /// </summary>
        private static int TerraceTorches(Ground ground, GameObject torch, Transform parent, Material body, List<Bounds> taken, List<(GameObject, float)> made)
        {
            if (!ground.Any) { Warn("night fires: no land to stand terrace torches on"); return 0; }
            var rng = new System.Random(1019);
            var kept = new List<Vector3>();
            int count = 0;
            for (float radius = 48.0f; radius <= 230.0f && count < 34; radius += 13.0f)
            {
                int steps = Mathf.Max(8, Mathf.RoundToInt(2.0f * Mathf.PI * radius / 22.0f));
                float offset = (float)rng.NextDouble();
                for (int k = 0; k < steps && count < 34; k++)
                {
                    float a = (k + offset) / steps * Mathf.PI * 2.0f;
                    float r = radius + ((float)rng.NextDouble() - 0.5f) * 6.0f;
                    float x = Mathf.Sin(a) * r, z = Mathf.Cos(a) * r;
                    if (z < -30.0f && Mathf.Abs(x) < 46.0f && z > -80.0f) continue;          // the market's terrace and the gate
                    if (z < -60.0f && Mathf.Abs(x) < 20.0f) continue;                           // the water stair (StairFires)
                    if (!ground.Down(x, z, out var hit) || !ground.IsGround(hit.collider) || ground.IsWater(hit) || hit.normal.y < 0.95f) continue;
                    // a lip: the ground drops 1.1 m or more within 1.8 m one way
                    Vector3 edge = Vector3.zero;
                    for (int d = 0; d < 8 && edge == Vector3.zero; d++)
                    {
                        float b = d * Mathf.PI / 4.0f;
                        var dir = new Vector3(Mathf.Sin(b), 0.0f, Mathf.Cos(b));
                        var probe = hit.point + dir * 1.8f;
                        if (ground.Down(probe.x, probe.z, out var low) && hit.point.y - low.point.y > 1.1f) edge = dir;
                    }
                    if (edge == Vector3.zero) continue;
                    Vector3 at = hit.point - edge * 0.7f;
                    if (kept.Any(p => (p - at).sqrMagnitude < 16.0f * 16.0f)) continue;
                    if (!ground.Down(at.x, at.z, out var seat) || Mathf.Abs(seat.point.y - hit.point.y) > 0.3f) continue;
                    float yaw = Mathf.Atan2(edge.x, edge.z) * Mathf.Rad2Deg;
                    if (FirePut(torch, parent, new Vector3(at.x, seat.point.y, at.z), yaw, 1.15f, false, body, null, ground, taken, made)) { kept.Add(at); count++; }
                }
            }
            return count;
        }
    }
}
