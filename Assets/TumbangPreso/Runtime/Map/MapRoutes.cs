using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// A MAP'S ROUTES FOR ITS BOTS: way points, and which of them can be walked between.
    ///
    /// ⚠️ WHY IT EXISTS (owner, 2026-10-09, after playing the stepped alley: "the bot ais struggle to get around this
    /// map"; asked whether a slipper on a roof should simply drop back or the bots be taught the way: "give them a
    /// route graph"; and of bodies snagged on things along the walls: "add a fix for that"). A bot has no path. It
    /// walks STRAIGHT at where it is going (`AIController.Goto`: a bearing, eight keys, a sidestep when it stops
    /// moving). On a flat open court that is enough. On a map with stairs, roofs, bridges and bounce tarps the
    /// straight line to a slipper on a roof ends under the roof, and the one to the far side of a stair ends in its
    /// flank.
    ///
    /// ⚠️ WHAT IT IS. A list of points a body can stand on (`Nodes`, written by the map's builder), and for every pair
    /// of them whether a body can WALK STRAIGHT from the first to the second (`Walk`: the floor is followed every
    /// 0.3 m, a rise of more than a step refuses it, a DROP is allowed, and a standing body must fit all the way).
    /// It is directional: walking off a roof is a route, walking up its wall is not. A bounce tarp is the one link
    /// that is not walked (`PadFrom` to `PadTo`). The shortest way between every pair is worked out ONCE, in the
    /// editor when the scene is built (`Bake`), and kept in the scene.
    ///
    /// ⚠️ WHAT A BOT ASKS (`Via`). "I am here and want to be there: where do I walk now?" If there can be walked to
    /// straight, the answer is there, and the bot is exactly what it was before this existed. Otherwise the answer
    /// is the first way point of the shortest route, taken again every `Replan` seconds from where the body then is,
    /// so a route is cut short the moment its end comes into reach.
    ///
    /// ⚠️ A MAP WITHOUT THIS COMPONENT IS UNTOUCHED: `Current` is null and `AIController` asks nothing. No rule, no
    /// message, nothing on the network: it changes only which way a bot presses its own keys.
    /// </summary>
    public sealed class MapRoutes : MonoBehaviour
    {
        /// <summary>Points a body can stand on, feet height, world space.</summary>
        public Vector3[] Nodes = System.Array.Empty<Vector3>();

        /// <summary>Bounce tarps: standing on node `PadFrom[i]` throws a body that then steers to node `PadTo[i]`.</summary>
        public int[] PadFrom = System.Array.Empty<int>();
        public int[] PadTo = System.Array.Empty<int>();

        /// <summary>Baked, row by row: what the shortest way from node a to node b costs (metres; under 0 is "no way").</summary>
        public float[] Cost = System.Array.Empty<float>();

        /// <summary>Baked, row by row: the node to walk to first on the shortest way from a to b (-1: none).</summary>
        public int[] Next = System.Array.Empty<int>();

        /// <summary>The live map's routes, or null on a map that has none.</summary>
        public static MapRoutes Current { get; private set; }

        private const float Step = 0.3f, MaxRise = 0.34f, MaxDrop = 8.0f, BodyRadius = 0.28f;
        private const float Reach = 14.0f, Replan = 0.7f, Arrive = 0.5f, PadCost = 3.0f, PadSeconds = 2.2f;
        private const float GoalSlack = 0.75f, StartSlack = 0.45f;

        private static readonly RaycastHit[] Hits = new RaycastHit[32];
        private static readonly Collider[] Over = new Collider[24];

        private sealed class Agent
        {
            public Vector3 Goal;
            public float ReplanAt = -1.0f;
            public readonly List<int> Path = new List<int>();
            public int At;
            public int PadLanding = -1;
            public int StandAt = -1;      // the goal cannot be reached at all: this way point is where to stop instead
            public float PadSince, PadUntil;
        }

        private readonly Dictionary<int, Agent> _agents = new Dictionary<int, Agent>();

        private void OnEnable() { Current = this; }
        private void OnDisable() { if (Current == this) Current = null; }

        private bool Baked => Nodes.Length > 0 && Cost.Length == Nodes.Length * Nodes.Length && Next.Length == Cost.Length;

        // ------------------------------------------------------------------ the question a bot asks

        /// <summary>
        /// Where a body at `feet` should walk now to end at `goal`. `hold` is true while it must press nothing (it has
        /// just been thrown by a tarp and is still under the ledge it is going to). Returns `goal` itself whenever the
        /// straight line will do, or nothing better is known.
        /// </summary>
        public Vector3 Via(int id, Vector3 feet, Vector3 goal, bool grounded, out bool hold)
        {
            hold = false;
            if (!Baked) { Bake(); if (!Baked) return goal; }
            if (!_agents.TryGetValue(id, out var a)) { a = new Agent(); _agents[id] = a; }
            float now = Time.time;

            if (a.PadLanding >= 0)
            {
                Vector3 landing = Nodes[a.PadLanding];
                bool landed = grounded && now > a.PadSince + 0.35f;
                if (now > a.PadUntil || landed) { a.PadLanding = -1; a.ReplanAt = -1.0f; }
                else { hold = feet.y < landing.y + 0.12f && now < a.PadSince + 0.9f; return landing; }
            }

            if (now >= a.ReplanAt || (goal - a.Goal).sqrMagnitude > 1.0f)
            {
                a.Goal = goal;
                // Staggered by the body's own id, so three bots do not all cast on one frame.
                a.ReplanAt = now + Replan + (Mathf.Abs(id) % 7) * 0.03f;
                Plan(a, feet, goal, grounded);
            }

            while (a.At < a.Path.Count)
            {
                int node = a.Path[a.At];
                Vector3 to = Nodes[node] - feet;
                float up = to.y; to.y = 0.0f;
                if (to.magnitude > Arrive || Mathf.Abs(up) > 0.7f) return Nodes[node];
                // Standing on this way point. If the next one is reached by a tarp, the tarp is about to throw the body.
                if (a.At + 1 < a.Path.Count && IsPad(node, a.Path[a.At + 1]))
                {
                    a.PadLanding = a.Path[a.At + 1];
                    a.PadSince = now; a.PadUntil = now + PadSeconds;
                    a.At += 2;
                    hold = true;
                    return Nodes[a.PadLanding];
                }
                a.At++;
            }
            return a.StandAt >= 0 ? Nodes[a.StandAt] : goal;
        }

        private void Plan(Agent a, Vector3 feet, Vector3 goal, bool grounded)
        {
            a.Path.Clear(); a.At = 0; a.StandAt = -1;
            if (!grounded) return;                                 // in the air: keep the bearing it has
            if (Walk(feet, goal, GoalSlack, StartSlack)) return;   // the straight line will do
            int n = Nodes.Length;
            float best = float.PositiveInfinity; int bestFrom = -1, bestTo = -1;
            // Which way points the goal can be walked to from: asked first, since there are usually few.
            var ends = EndsBuffer; ends.Clear();
            for (int j = 0; j < n; j++)
            {
                float d = Flat(Nodes[j], goal);
                if (d <= Reach && Walk(Nodes[j], goal, GoalSlack)) ends.Add(new KeyValuePair<int, float>(j, d));
            }
            if (ends.Count == 0)
            {
                // Nothing can walk to it even so (it lies in a place no body reaches). Go and stand at the way point
                // nearest it on its own level: next to the thing beats under it.
                // ⚠️ THE GAME SENDS BOTS TO SPOTS THIS MAP HAS A HOUSE ON (the trace, 2026-10-09: every stuck bot was "wanting
                // (8.20, 0.00, z)", the throw standoff 1.2 m outside the chalk box; the court's walls are at 8.0). The bot
                // goes to the nearest way point and STANDS there (`StandAt`); it used to carry on into the wall.
                int near = -1; float nearest = 6.0f;
                for (int j = 0; j < n; j++)
                {
                    if (Mathf.Abs(Nodes[j].y - goal.y) > 0.9f) continue;
                    float d = Flat(Nodes[j], goal);
                    if (d < nearest) { nearest = d; near = j; }
                }
                if (near < 0) { Trace(feet, goal, "no way point near the goal"); return; }
                ends.Add(new KeyValuePair<int, float>(near, nearest));
                a.StandAt = near;
            }
            for (int i = 0; i < n; i++)
            {
                float d = Flat(feet, Nodes[i]);
                if (d > Reach) continue;
                float least = float.PositiveInfinity; int leastTo = -1;
                for (int k = 0; k < ends.Count; k++)
                {
                    float c = Cost[i * n + ends[k].Key];
                    if (c < 0.0f) continue;
                    float total = d + c + ends[k].Value;
                    if (total < least) { least = total; leastTo = ends[k].Key; }
                }
                if (leastTo < 0 || least >= best) continue;
                if (!Walk(feet, Nodes[i], 0.0f, StartSlack)) continue;
                best = least; bestFrom = i; bestTo = leastTo;
            }
            if (bestFrom < 0) { Trace(feet, goal, "no way point can be walked to from here, or none of them leads there"); return; }
            int at = bestFrom, guard = 0;
            a.Path.Add(at);
            while (at != bestTo && guard++ < n)
            {
                at = Next[at * n + bestTo];
                if (at < 0) { a.Path.Clear(); return; }
                a.Path.Add(at);
            }
        }

        private static float _traceAt;

        /// <summary>
        /// In the editor, a bot that wanted a route and got none says so in `Logs/eskinita/routes_trace.txt` (at most
        /// one line a second): where it stood, where it wanted to be, and why. So "the bot is stuck" can be read
        /// afterwards instead of guessed at.
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private static void Trace(Vector3 feet, Vector3 goal, string why)
        {
            if (Time.unscaledTime < _traceAt) return;
            _traceAt = Time.unscaledTime + 1.0f;
            try
            {
                System.IO.Directory.CreateDirectory("Logs/eskinita");
                System.IO.File.AppendAllText("Logs/eskinita/routes_trace.txt",
                    $"{System.DateTime.Now:HH:mm:ss} no route: at ({feet.x:0.00}, {feet.y:0.00}, {feet.z:0.00}) wanting ({goal.x:0.00}, {goal.y:0.00}, {goal.z:0.00}): {why}" + System.Environment.NewLine);
            }
            catch (System.Exception) { }
        }

        private static readonly List<KeyValuePair<int, float>> EndsBuffer = new List<KeyValuePair<int, float>>();

        private bool IsPad(int from, int to)
        {
            for (int i = 0; i < PadFrom.Length && i < PadTo.Length; i++) if (PadFrom[i] == from && PadTo[i] == to) return true;
            return false;
        }

        private static float Flat(Vector3 a, Vector3 b) { a.y = 0.0f; b.y = 0.0f; return Vector3.Distance(a, b); }

        // ------------------------------------------------------------------ can a body walk straight from a to b

        /// <summary>
        /// True if a standing body can walk the straight line from `a` to `b`: floor under every 0.3 m of it, no rise
        /// over a step between two samples (stairs here are ramps, so they pass), any drop, room for the body, and the
        /// floor it ends on within 0.6 m of `b`'s height.
        /// </summary>
        /// <param name="shortOfEnd">Metres at the END of the line where the body need not fit (the floor is still followed).
        /// ⚠️ A GOAL IS NOT A PLACE TO STAND: a slipper comes to rest against a wall, in a corner, under an eave, on a
        /// roof's rim, where a standing body does not fit. With no slack every way point's line to such a slipper was
        /// refused, the route had no end, and the bot fell back to walking straight at it (owner, 2026-10-09, of a bot
        /// pressed against the wall under a ledge: "bot still getting stuck").</param>
        /// <param name="shortOfStart">The same at the START: a body pressed into a corner or against a drum is where it is.</param>
        public static bool Walk(Vector3 a, Vector3 b, float shortOfEnd = 0.0f, float shortOfStart = 0.0f)
        {
            Vector3 d = b - a; d.y = 0.0f;
            float length = d.magnitude;
            if (!Ground(a, a.y + 0.9f, 1.9f, out float y)) return false;
            if (length < 0.05f) return Mathf.Abs(y - b.y) < 0.6f;
            int steps = Mathf.CeilToInt(length / Step);
            for (int i = 1; i <= steps; i++)
            {
                float along = length * i / steps;
                Vector3 p = a + d * (i / (float)steps);
                if (!Ground(p, y + 0.9f, 0.9f + MaxDrop, out float g))
                {
                    if (length - along < shortOfEnd) break;         // the last of it hangs past an edge or under something
                    return false;
                }
                if (g - y > MaxRise) return false;
                y = g;
                if (along > shortOfStart && length - along >= shortOfEnd && Blocked(new Vector3(p.x, y, p.z))) return false;
            }
            return Mathf.Abs(y - b.y) < 0.8f;
        }

        private static bool Ground(Vector3 at, float fromY, float reach, out float y)
        {
            int count = Physics.RaycastNonAlloc(new Vector3(at.x, fromY, at.z), Vector3.down, Hits, reach, ~0, QueryTriggerInteraction.Ignore);
            y = float.NegativeInfinity;
            for (int i = 0; i < count; i++)
            {
                if (Hits[i].normal.y < 0.6f || Mine(Hits[i].collider)) continue;
                if (Hits[i].point.y > y) y = Hits[i].point.y;
            }
            return !float.IsNegativeInfinity(y);
        }

        private static bool Blocked(Vector3 feet)
        {
            int count = Physics.OverlapCapsuleNonAlloc(feet + Vector3.up * 0.75f, feet + Vector3.up * 1.3f, BodyRadius, Over, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++) if (!Mine(Over[i])) return true;
            return false;
        }

        /// <summary>Bodies, slippers and the can are not the map: a way is not closed because somebody stands in it.</summary>
        private static bool Mine(Collider c)
        {
            return c == null || c.GetComponentInParent<CharacterMotor>() != null || c.GetComponentInParent<Slipper>() != null
                   || c.GetComponentInParent<Lata>() != null;
        }

        // ------------------------------------------------------------------ the bake

        /// <summary>
        /// Works out every pair once. Called by the map's builder in the editor, with the collision in the scene, and
        /// kept in the scene; called here at the first question only if a scene was saved without it. Returns how many
        /// direct links were found.
        /// </summary>
        public int Bake()
        {
            int n = Nodes.Length;
            if (n == 0) return 0;
            Physics.SyncTransforms();
            var cost = new float[n * n];
            var next = new int[n * n];
            int links = 0;
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    int k = i * n + j;
                    if (i == j) { cost[k] = 0.0f; next[k] = j; continue; }
                    cost[k] = float.PositiveInfinity; next[k] = -1;
                    float d = Flat(Nodes[i], Nodes[j]);
                    if (d > Reach + 4.0f || !Walk(Nodes[i], Nodes[j])) continue;
                    cost[k] = d + Mathf.Abs(Nodes[i].y - Nodes[j].y) * 0.5f; next[k] = j; links++;
                }
            for (int p = 0; p < PadFrom.Length && p < PadTo.Length; p++)
            {
                int k = PadFrom[p] * n + PadTo[p];
                if (PadCost < cost[k]) { cost[k] = PadCost; next[k] = PadTo[p]; links++; }
            }
            for (int m = 0; m < n; m++)
                for (int i = 0; i < n; i++)
                {
                    float im = cost[i * n + m];
                    if (float.IsPositiveInfinity(im)) continue;
                    for (int j = 0; j < n; j++)
                    {
                        float through = im + cost[m * n + j];
                        if (through < cost[i * n + j]) { cost[i * n + j] = through; next[i * n + j] = next[i * n + m]; }
                    }
                }
            for (int k = 0; k < cost.Length; k++) if (float.IsPositiveInfinity(cost[k])) cost[k] = -1.0f;
            Cost = cost; Next = next;
            return links;
        }

        /// <summary>How many nodes have no way to node `to` (the builder reports it: a node nobody can leave is a trap).</summary>
        public int CountWithoutWayTo(int to, List<int> which = null)
        {
            int n = Nodes.Length, none = 0;
            for (int i = 0; i < n; i++)
                if (Cost[i * n + to] < 0.0f) { none++; which?.Add(i); }
            return none;
        }

        /// <summary>How many nodes cannot be reached from node `from`.</summary>
        public int CountWithoutWayFrom(int from, List<int> which = null)
        {
            int n = Nodes.Length, none = 0;
            for (int j = 0; j < n; j++)
                if (Cost[from * n + j] < 0.0f) { none++; which?.Add(j); }
            return none;
        }
    }
}
