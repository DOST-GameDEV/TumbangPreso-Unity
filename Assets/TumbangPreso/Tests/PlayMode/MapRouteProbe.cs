using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    [Category("WallClock")]
    public sealed class MapRouteProbe
    {
        private bool _bots, _spectator, _pinned;
        private int _seat;
        private CustomRules _rules;
        private static string Output => Environment.GetEnvironmentVariable("TUMP_MAP_ROUTES") ?? "Logs/map-routes-v1";
        private const float Step = .5f;

        [UnitySetUp] public IEnumerator Before()
        {
            _bots = GameLaunch.AllBots; _spectator = GameLaunch.Spectator; _seat = GameLaunch.SoloSeat;
            _pinned = SceneFlow.RulesPinned; _rules = SceneFlow.SelectedRules.Clone();
            yield return PlayModeWorld.Reset(); Directory.CreateDirectory(Output);
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();
            GameLaunch.AllBots = _bots; GameLaunch.Spectator = _spectator; GameLaunch.SoloSeat = _seat;
            SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator GroundGridAndRepresentativeObstacleRoutesRemainRetrievable()
        {
            var report = new StringBuilder("map,walkable,connected,clear_shoe_samples,unreachable_samples\n");
            var routes = new StringBuilder("map,target,shoe_x,shoe_y,shoe_z,route_nodes,seconds,picked_up\n");
            try
            {
                foreach (string map in new[] { SceneFlow.Eskinita, SceneFlow.BayanPlaza, SceneFlow.IlalimNgTulay })
                {
                    yield return MapRetrievalProbe.Load(map);
                    var who = GameServices.Round.PlayerAt(1);
                    var cc = who.GetComponent<CharacterController>();
                    Object.FindFirstObjectByType<CameraRig>().SetAimSource(AimSource.Movement);
                    // This measures map geometry, not opponent avoidance. Keep
                    // other seats registered but out of the physical experiment.
                    foreach (var other in GameServices.Round.Players.Where(p => p != who))
                    {
                        other.GetComponent<CharacterController>().enabled = false;
                        other.enabled = false;
                        other.transform.position = new Vector3(50 + other.PlayerSlot * 3, 0, 50);
                    }
                    Physics.SyncTransforms();
                    var nodes = new Dictionary<Vector2Int, Vector3>();
                    float hx = AIController.PlayableHalfX, hz = AIController.PlayableHalfZ;
                    int ixMax = Mathf.FloorToInt((hx - cc.radius - .1f) / Step);
                    int izMax = Mathf.FloorToInt((hz - cc.radius - .1f) / Step);
                    for (int x = -ixMax; x <= ixMax; x++)
                    for (int z = -izMax; z <= izMax; z++)
                    {
                        if (!Floor(new Vector2(x * Step, z * Step), out var feet)) continue;
                        if (ClearBody(feet, cc)) nodes.Add(new Vector2Int(x, z), feet);
                    }
                    Assert.Greater(nodes.Count, 100, map + " has no useful walkable sample grid.");
                    Vector3 start = new Vector3(0, 0, 8.5f);
                    var startKey = nodes.OrderBy(p => (p.Value - start).sqrMagnitude).First().Key;
                    var parents = Flood(nodes, startKey, cc);
                    var connected = parents.Keys.ToArray();
                    var samples = new List<Vector3>();
                    var bad = new List<string>();
                    for (float x = -hx + Balance.SlipperHitRadius + .03f; x < hx - Balance.SlipperHitRadius; x += Step)
                    for (float z = -hz + Balance.SlipperHitRadius + .03f; z < hz - Balance.SlipperHitRadius; z += Step)
                    {
                        if (!Floor(new Vector2(x, z), out var floor)) continue;
                        if (Physics.OverlapSphere(floor + Vector3.up * .24f, Balance.SlipperHitRadius, ~0,
                            QueryTriggerInteraction.Ignore).Any(c => Blocks(c, floor.y + .025f))) continue;
                        var shoe = floor + Vector3.up * .1f;
                        samples.Add(shoe);
                        float distance = connected.Min(k => (nodes[k] + Vector3.up * cc.skinWidth - shoe).sqrMagnitude);
                        if (distance > Balance.PickupRadius * Balance.PickupRadius)
                            bad.Add(FormattableString.Invariant($"{x:F3},{shoe.y:F3},{z:F3},nearest={Mathf.Sqrt(distance):F3}"));
                    }
                    report.AppendLine($"{map},{nodes.Count},{parents.Count},{samples.Count},{bad.Count}");
                    File.WriteAllLines(Path.Combine(Output, map + "-unreachable.txt"), bad);
                    Assert.IsEmpty(bad, map + " has clear resting samples without a connected pickup approach.");

                    var targets = new Dictionary<string, Vector3>
                    {
                        ["west wall"] = samples.OrderBy(p => p.x).ThenBy(p => Mathf.Abs(p.z)).First(),
                        ["east wall"] = samples.OrderByDescending(p => p.x).ThenBy(p => Mathf.Abs(p.z)).First(),
                        ["south wall"] = samples.OrderBy(p => p.z).ThenBy(p => Mathf.Abs(p.x)).First(),
                        ["north wall"] = samples.OrderByDescending(p => p.z).ThenBy(p => Mathf.Abs(p.x)).First()
                    };
                    foreach (var collider in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
                    {
                        if (!collider.enabled || collider.isTrigger) continue;
                        var b = collider.bounds;
                        if (b.size.y < 1 || b.size.x > 7 || b.size.z > 7 || Mathf.Abs(b.center.x) >= hx || Mathf.Abs(b.center.z) >= hz) continue;
                        string path = PathOf(collider.transform);
                        string kind = path.Contains("Broadleaf") ? "tree trunk" : path.Contains("Pillar") ? "guideway pillar" :
                            path.Contains("Monument") ? "monument" : path.Contains("Kiosk") ? "kiosk" :
                            path.Contains("Cart") || path.Contains("Tricycle") ? "cart" : null;
                        if (kind == null || targets.ContainsKey(kind)) continue;
                        Vector3 behind = new Vector3(b.center.x, 0, b.min.z - .3f);
                        targets[kind] = samples.OrderBy(p => (p - behind).sqrMagnitude).First();
                    }
                    foreach (var target in targets)
                    {
                        GameServices.Round.BeginRound();
                        who.Intent.Clear(); who.Intent.Parked = false;
                        who.Teleport(nodes[startKey]);
                        var shoe = Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None).First(s => s.SeatOfOrigin == 1);
                        shoe.HostForceEquip(who);
                        shoe.HostThrow(who, target.Value + Vector3.up * .5f, Vector3.down);
                        for (int i = 0; i < 50 && shoe.State == SlipperState.InFlight; i++) yield return new WaitForFixedUpdate();
                        Assert.AreEqual(SlipperState.Loose, shoe.State, target.Key + " setup did not reach a resting place.");
                        var resting = shoe.transform.position;
                        var end = connected.OrderBy(k => (nodes[k] + Vector3.up * cc.skinWidth - resting).sqrMagnitude).First();
                        var path = new List<Vector3>();
                        for (var key = end; key != startKey; key = parents[key]) path.Add(nodes[key]);
                        path.Add(nodes[startKey]); path.Reverse();
                        int waypoint = 0;
                        float began = Time.realtimeSinceStartup;
                        var driver = who.gameObject.AddComponent<RouteInput>();
                        driver.Drive = () =>
                        {
                            while (waypoint < path.Count - 1 && FlatDistance(who.transform.position, path[waypoint]) < .22f) waypoint++;
                            Vector3 delta = path[waypoint] - who.transform.position;
                            who.Intent.Move = FlatDistance(who.transform.position, path[waypoint]) < .12f ? Vector2.zero : new Vector2(delta.x, delta.z).normalized;
                            who.Intent.AimPoint = path[waypoint]; who.Intent.FaceAimPoint = true;
                            who.Intent.Set(Verb.Grab, Vector3.Distance(who.transform.position, resting) < Balance.PickupRadius - .025f);
                        };
                        try
                        {
                            while (who.GetComponent<Carrier>().Held != shoe && Time.realtimeSinceStartup - began < 28) yield return null;
                            bool picked = who.GetComponent<Carrier>().Held == shoe;
                            routes.AppendLine(FormattableString.Invariant($"{map},{target.Key},{resting.x:F3},{resting.y:F3},{resting.z:F3},{path.Count},{Time.realtimeSinceStartup - began:F3},{picked}"));
                            Assert.IsTrue(picked, $"{map}/{target.Key}: real motor stopped at {who.transform.position}, shoe={resting}, waypoint={waypoint}/{path.Count}.");
                        }
                        finally { driver.enabled = false; Object.Destroy(driver); who.Intent.Clear(); }
                    }
                    yield return PlayModeWorld.Reset();
                }
            }
            finally
            {
                File.WriteAllText(Path.Combine(Output, "grid.csv"), report.ToString());
                File.WriteAllText(Path.Combine(Output, "routes.csv"), routes.ToString());
            }
        }

        private static float FlatDistance(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;
        private static string PathOf(Transform t) { string p = t.name; while (t.parent != null) { t = t.parent; p = t.name + "/" + p; } return p; }
        private static bool Blocks(Collider c, float allowedTop)
            => c.GetComponentInParent<CharacterMotor>() == null && c.GetComponentInParent<Slipper>() == null &&
                c.GetComponentInParent<Lata>() == null && c.bounds.max.y > allowedTop;
        private static bool Floor(Vector2 at, out Vector3 floor)
        {
            var hits = Physics.RaycastAll(new Vector3(at.x, 1.15f, at.y), Vector3.down, 2, ~0, QueryTriggerInteraction.Ignore)
                .Where(h => h.normal.y > .6f && Blocks(h.collider, -2)).OrderBy(h => h.distance).ToArray();
            floor = hits.Length == 0 ? Vector3.zero : hits[0].point;
            return hits.Length > 0;
        }
        private static bool ClearBody(Vector3 feet, CharacterController cc)
            => !Physics.OverlapCapsule(feet + Vector3.up * (cc.radius + .06f),
                feet + Vector3.up * (cc.height - cc.radius + .06f), cc.radius + .02f, ~0, QueryTriggerInteraction.Ignore)
                .Any(c => Blocks(c, feet.y + cc.stepOffset + .02f));
        private static Dictionary<Vector2Int, Vector2Int> Flood(Dictionary<Vector2Int, Vector3> nodes, Vector2Int start, CharacterController cc)
        {
            var parent = new Dictionary<Vector2Int, Vector2Int> { [start] = start };
            var queue = new Queue<Vector2Int>(); queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var key = queue.Dequeue(); var a = nodes[key];
                foreach (var offset in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                {
                    var next = key + offset;
                    if (parent.ContainsKey(next) || !nodes.TryGetValue(next, out var b) || Mathf.Abs(a.y - b.y) > cc.stepOffset) continue;
                    Vector3 delta = b - a;
                    if (Physics.CapsuleCastAll(a + Vector3.up * (cc.radius + .06f), a + Vector3.up * (cc.height - cc.radius + .06f),
                        cc.radius + .02f, delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Ignore)
                        .Any(h => Blocks(h.collider, Mathf.Min(a.y, b.y) + cc.stepOffset + .02f))) continue;
                    parent[next] = key; queue.Enqueue(next);
                }
            }
            return parent;
        }
        [DefaultExecutionOrder(-300)]
        private sealed class RouteInput : MonoBehaviour
        {
            public Action Drive;
            private void Update() => Drive?.Invoke();
        }
    }
}
