using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    /// <summary>
    /// ⚠️⚠️ PAETE'S HANDS: THE VINES OF HIS OWN ARMS GROW.
    ///
    /// Owner, 2026-10-06, of a first try that stood green tendrils and a flower ON his braids: *"i like the extending
    /// vines on paete but its just attached to his arm, its not actually the vine from his arm extending"*, and of
    /// creatures on every hero's hands: *"we were trying to reserve the pet idea only for nemu ... i need something for
    /// the bare hands"*. So nothing is added to him here. His first-person forearm is four braided strands of vine
    /// (four separate pieces of the one arm mesh), and THOSE are what move.
    ///
    /// ⚠️⚠️ EACH STRAND GROWS ALONG ITS OWN WAY. Two builds were refused. The first bent each strand about its root, all the
    /// time: *"kinda gross looking because they're kinda just flailing around like tentacles.. the vines dont stretch"*.
    /// The second moved the four as one stiff braid: *"this whole iteration looks bland, they can move separately but make
    /// it so they grow in the direction they look like theyre growing into"*. So: every strand is on its own clock and
    /// its own spring, and what each one does is GROW, sliding out along the very path it already winds through the
    /// braid and on past its tip in the direction that tip points, the new length uncurling like a fern as it comes;
    /// and it draws back the same way. Nothing swings from a root.
    /// ⚠️ THE TWO ARMS ARE NOT A PAIR. Of that build: *"the vines on the two arms always move in sync, when throwing, both
    /// arms extend"*. Each arm now runs its own clock at its own pace, takes its strands in the opposite order, answers
    /// a fall and a landing a beat after the other and less hard, and walks on the other foot. And a throw is the RIGHT
    /// arm's alone (it is the one that throws): the left only flinches in.
    ///
    ///   STANDING   the four lengthen and shorten a little, never in step; every few seconds one or two sprout out long,
    ///              feel the air, and draw back in;
    ///   WALKING    they pulse out in turn with his footfalls;
    ///   SPRINTING  drawn in short, tips swept back;
    ///   TAKE-OFF   all four snap in short;
    ///   FALLING    they shoot out one after another, each to its own length, long and shivering;
    ///   LANDING    driven in by the impact and sprung back out past rest, each in its own time (harder from a longer fall);
    ///   A SLIPPER  the right arm, which holds it, keeps still; two of the left's reach across toward it;
    ///   WINDING UP the left's draw in one by one with the charge; THE THROW shoots the RIGHT arm's out in a ripple
    ///              while the left's flinch in;
    ///   TAGGED     drawn in, tips hanging;
    ///   LIANA LEAP his strands are drawn in and curled tight for the wind-up, then lie as they are while new wood
    ///              grows on from their tips (below); they do not themselves shoot out (owner, 2026-10-07: *"i dont see the updated arm
    ///              stretch from first person"*). The kit's own lengthening of the forearm (`SetReachStretch`) and the
    ///              vines in the world carry on from the tips.
    ///              ⚠️ AND THEY GO ALL THE WAY (owner, the same day, of that: *"in game the actual vine is still made
    ///              separately from his arms.. i need the arms to stretch to form the leap's vine"*).
    ///              `Visual.PaeteVineReach` tells these hands where the vine catches and how far out it is
    ///              (`ReachTo`), and each strand GROWS ON from its own tip to that very point in the world,
    ///              the four winding round each other as they go, and comes back the same way.
    ///              ⚠️ NEW WOOD IS GROWN, THE OLD IS NOT PULLED. The first build of this slid each strand's own
    ///              ninety points out along eight metres, and of that: *"textures get really distorted and the
    ///              mesh gets really weird"*. The strand now stays as it is and a round limb is built on from
    ///              its tip each frame (`BuildReach`): the strand's own thickness, the arm's own material, and
    ///              the strand's own patch of paint REPEATED along the length, never stretched.
    ///   ANY OTHER CAST they hold still.
    ///
    /// ⚠️ HOW: THE ARM'S OWN VERTICES ARE MOVED. Each arm draws a private copy of its mesh. Its triangles are sorted into
    /// connected pieces once; the four long ones that reach the tip are the strands. For each, the middle line it follows
    /// above `GrowFrom` is measured (`Line`), and every point remembers how far along that line it sits and where it
    /// stands off it. Growing by a share moves each point to that much further along the SAME line, and beyond the line's
    /// end out along its last direction on a gentle arc; the point's offset, its normal and its ink normal are turned to
    /// the line's direction there, so the shape, the shading and the outline follow. The shared mesh asset is never
    /// written, and the arm gets its own mesh back when this goes.
    /// </summary>
    public sealed class PaeteVineHands : ViewmodelArms.HandCompanion
    {
        /// <summary>The height along the arm (elbow 0, tip 0.84) above which a strand can slide and grow.</summary>
        public const float GrowFrom = .34f;
        /// <summary>How much longer than itself a strand grows: sprouting while idle, in a fall, on a throw.</summary>
        public const float SproutGrow = .6f, FallGrow = 1.1f, ThrowGrow = .9f;
        private const int Steps = 12;

        private sealed class Strand
        {
            public int[] Points;                 // indices into the arm's vertices, only those above GrowFrom
            public float[] Along;                // each point's distance along the strand's line
            public Vector3[] Off;                // each point's offset from the line there
            public readonly Vector3[] Line = new Vector3[Steps];      // the line it follows through the braid
            public readonly Vector3[] Way = new Vector3[Steps];       // the line's direction at each step
            public readonly float[] Far = new float[Steps];           // distance along the line at each step
            public Vector3 Outward;              // out from the braid's middle, at its tip
            public float Length;
            public ViewmodelArms.Spring Grow, Curl;
            public float Thick;                  // how far its surface stands off its line, near the tip
            public Vector2 PaintFrom, PaintTo;   // the patch of the arm's painting it wears
            public float Beat, Share;            // its own clock offset, and its own share of any growth (no two alike)
        }

        private sealed class Arm
        {
            public Transform Transform; public MeshFilter Filter; public Mesh Own, Shared;
            public Vector3[] RestPoints, RestNormals, Points, Normals;
            public Vector4[] RestInk, Ink;
            public Vector2[] RestPaint;
            public readonly List<Strand> Strands = new List<Strand>();
            public bool Right;
            // The limbs grown on from the strands' tips in a leap: one mesh for the arm's four.
            public Mesh Reach; public MeshRenderer ReachRenderer;
            public Vector3[] ReachPoints, ReachNormals; public Vector4[] ReachInk; public Vector2[] ReachPaint;
        }

        private const int ReachRings = 30, ReachSides = 6;

        private readonly Arm[] _arms = new Arm[2];

        // What `Visual.PaeteVineReach` last said: whose leap, where each arm's vine catches, how far out it is (0 to 1).
        private static CharacterMotor _reachCaster;
        private static readonly Vector3[] ReachAnchor = new Vector3[2];
        /// <summary>The grown limbs' renderers made so far (the newest first-person arms'), for whoever must light them with the arms (`PaeteGroundCall`'s glow).</summary>
        public static readonly List<Renderer> ReachRenderers = new List<Renderer>(2);
        private static float _reachOut;
        private static int _reachFrame = -100;

        /// <summary>The leap's vine, this frame: `caster`'s `left` (or right) arm reaches `anchor` in the world, `extend` of the way.</summary>
        public static void ReachTo(CharacterMotor caster, bool left, Vector3 anchor, float extend)
        {
            _reachCaster = caster; ReachAnchor[left ? 0 : 1] = anchor; _reachOut = Mathf.Clamp01(extend); _reachFrame = Time.frameCount;
        }
        private float _clock, _fallSpeed, _sproutLeft, _nextSprout = 2.5f, _limp, _whip, _landed = 9f, _left = 9f;
        private int _sproutArm, _sproutA, _sproutB;
        private bool _grounded = true, _groundedLast = true, _carrying, _charging, _wasCasting;
        /// <summary>Seconds into a Liana Leap, or below 0. The leap's own clock: 0.12 s of tell, out by 0.26, home at 0.62.</summary>
        private float _leap = -1f;
        private const float LeapTell = .12f, LeapHome = .62f, LeapOver = 1.0f, LeapGrow = 1.25f;
        private float _sinceLand = 9f, _lateKick, _pendingKick;
        /// <summary>The right arm answers a fall, a landing and a take-off this much after the left, and this much less.</summary>
        private const float RightLate = .11f, RightShare = .7f;

        public override bool Build(ViewmodelArms arms)
        {
            _arms[0] = Take(arms.LeftHandForProps(), false);
            _arms[1] = Take(arms.RightHandForProps(), true);
            return _arms[0] != null && _arms[1] != null;
        }

        private static Arm Take(Transform armTransform, bool right)
        {
            if (armTransform == null) return null;
            var filter = armTransform.GetComponent<MeshFilter>();
            var shared = filter != null ? filter.sharedMesh : null;
            if (shared == null || !shared.isReadable) return null;
            var arm = new Arm { Transform = armTransform, Filter = filter, Shared = shared, Right = right };
            arm.RestPoints = shared.vertices; arm.RestNormals = shared.normals;
            var tangents = shared.tangents;
            if (arm.RestNormals == null || arm.RestNormals.Length != arm.RestPoints.Length) return null;
            arm.RestInk = tangents != null && tangents.Length == arm.RestPoints.Length ? tangents : null;
            arm.Points = (Vector3[])arm.RestPoints.Clone(); arm.Normals = (Vector3[])arm.RestNormals.Clone();
            arm.Ink = arm.RestInk != null ? (Vector4[])arm.RestInk.Clone() : null;
            var paint = shared.uv;
            arm.RestPaint = paint != null && paint.Length == arm.RestPoints.Length ? paint : null;
            FindStrands(arm, shared.triangles);
            if (arm.Strands.Count == 0) return null;
            arm.Own = Object.Instantiate(shared);
            arm.Own.name = shared.name + " (living)";
            arm.Own.MarkDynamic();
            // Room for a strand at full growth: the arm is never culled for it.
            var bounds = shared.bounds; bounds.Expand(80f); arm.Own.bounds = bounds;
            filter.sharedMesh = arm.Own;
            MakeReach(arm);
            return arm;
        }

        /// <summary>
        /// Sorts the arm's triangles into connected pieces (points at one place count as one), keeps the long ones that
        /// run from below `GrowFrom` to the tip (his braided strands), and measures the line each one follows.
        /// </summary>
        private static void FindStrands(Arm arm, int[] triangles)
        {
            int count = arm.RestPoints.Length;
            var parent = new int[count];
            for (int i = 0; i < count; i++) parent[i] = i;
            int Root(int i) { while (parent[i] != i) { parent[i] = parent[parent[i]]; i = parent[i]; } return i; }
            var place = new Dictionary<Vector3Int, int>();
            for (int i = 0; i < count; i++)
            {
                var p = arm.RestPoints[i];
                var key = new Vector3Int(Mathf.RoundToInt(p.x * 5000f), Mathf.RoundToInt(p.y * 5000f), Mathf.RoundToInt(p.z * 5000f));
                if (place.TryGetValue(key, out int first)) parent[Root(i)] = Root(first); else place[key] = i;
            }
            for (int t = 0; t + 2 < triangles.Length; t += 3)
            {
                parent[Root(triangles[t])] = Root(triangles[t + 1]);
                parent[Root(triangles[t + 1])] = Root(triangles[t + 2]);
            }
            var pieces = new Dictionary<int, List<int>>();
            for (int i = 0; i < count; i++)
            {
                int root = Root(i);
                if (!pieces.TryGetValue(root, out var list)) pieces[root] = list = new List<int>();
                list.Add(i);
            }
            float top = float.MinValue;
            foreach (var p in arm.RestPoints) top = Mathf.Max(top, p.y);
            Vector3 middle = Vector3.zero;
            var sum = new Vector3[Steps]; var hits = new int[Steps];
            foreach (var piece in pieces.Values)
            {
                float low = float.MaxValue, high = float.MinValue;
                foreach (int i in piece) { low = Mathf.Min(low, arm.RestPoints[i].y); high = Mathf.Max(high, arm.RestPoints[i].y); }
                if (low > GrowFrom - .2f || high < top - .12f) continue;
                var free = new List<int>();
                foreach (int i in piece) if (arm.RestPoints[i].y > GrowFrom) free.Add(i);
                if (free.Count < 8) continue;
                var strand = new Strand { Points = free.ToArray() };
                // ITS LINE: the middle of the strand at each of twelve heights from `GrowFrom` to its tip.
                float span = Mathf.Max(.05f, high - GrowFrom);
                for (int s = 0; s < Steps; s++) { sum[s] = Vector3.zero; hits[s] = 0; }
                foreach (int i in free)
                {
                    int s = Mathf.Clamp(Mathf.RoundToInt((arm.RestPoints[i].y - GrowFrom) / span * (Steps - 1)), 0, Steps - 1);
                    sum[s] += arm.RestPoints[i]; hits[s]++;
                }
                int known = -1;
                for (int s = 0; s < Steps; s++)
                {
                    if (hits[s] > 0) { strand.Line[s] = sum[s] / hits[s]; known = s; }
                    else if (known >= 0) strand.Line[s] = strand.Line[known] + Vector3.up * (span / (Steps - 1) * (s - known));
                    else strand.Line[s] = new Vector3(0f, GrowFrom + span * s / (Steps - 1), 0f);
                    strand.Line[s].y = GrowFrom + span * s / (Steps - 1);
                }
                for (int s = 0; s < Steps; s++)
                {
                    Vector3 way = strand.Line[Mathf.Min(Steps - 1, s + 1)] - strand.Line[Mathf.Max(0, s - 1)];
                    strand.Way[s] = way.sqrMagnitude > 1e-8f ? way.normalized : Vector3.up;
                    strand.Far[s] = s == 0 ? 0f : strand.Far[s - 1] + Vector3.Distance(strand.Line[s], strand.Line[s - 1]);
                }
                strand.Length = Mathf.Max(.05f, strand.Far[Steps - 1]);
                strand.Along = new float[free.Count]; strand.Off = new Vector3[free.Count];
                for (int n = 0; n < free.Count; n++)
                {
                    Vector3 p = arm.RestPoints[free[n]];
                    float f = Mathf.Clamp((p.y - GrowFrom) / span * (Steps - 1), 0f, Steps - 1.001f);
                    int s = (int)f; float u = f - s;
                    strand.Along[n] = Mathf.Lerp(strand.Far[s], strand.Far[s + 1], u);
                    strand.Off[n] = p - Vector3.Lerp(strand.Line[s], strand.Line[s + 1], u);
                }
                // How thick it is over its last third, and the patch of paint its free length wears.
                float thick = 0f; int counted = 0;
                Vector2 paintLow = new Vector2(9f, 9f), paintHigh = new Vector2(-9f, -9f);
                for (int n = 0; n < free.Count; n++)
                {
                    if (strand.Along[n] > strand.Length * .55f && strand.Along[n] < strand.Length * .9f) { thick += strand.Off[n].magnitude; counted++; }
                    if (arm.RestPaint != null) { paintLow = Vector2.Min(paintLow, arm.RestPaint[free[n]]); paintHigh = Vector2.Max(paintHigh, arm.RestPaint[free[n]]); }
                }
                strand.Thick = counted > 0 ? Mathf.Clamp(thick / counted, .015f, .12f) : .05f;
                // Well inside the patch, so a neighbour's paint never bleeds in at the edges.
                Vector2 inset = (paintHigh - paintLow) * .22f;
                strand.PaintFrom = paintLow + inset; strand.PaintTo = paintHigh - inset;
                middle += strand.Line[Steps - 1];
                arm.Strands.Add(strand);
            }
            if (arm.Strands.Count == 0) return;
            middle /= arm.Strands.Count;
            for (int k = 0; k < arm.Strands.Count; k++)
            {
                var strand = arm.Strands[k];
                Vector3 outward = strand.Line[Steps - 1] - middle;
                outward -= strand.Way[Steps - 1] * Vector3.Dot(outward, strand.Way[Steps - 1]);
                strand.Outward = outward.sqrMagnitude > 1e-6f ? outward.normalized : Vector3.right;
                strand.Beat = k * 1.9f + (arm.Right ? 3.7f : 0f);
                strand.Share = 1f + .22f * ((k * 7 + (arm.Right ? 3 : 0)) % 4 - 1.5f) / 1.5f;
            }
        }

        public override void Destroy()
        {
            foreach (var arm in _arms)
            {
                if (arm == null) continue;
                // Only if the arm still wears ours: a change of hero has already given it another mesh.
                if (arm.Filter != null && arm.Filter.sharedMesh == arm.Own) arm.Filter.sharedMesh = arm.Shared;
                HandCompanionProp.Kill(arm.Own);
                if (arm.ReachRenderer != null) HandCompanionProp.Kill(arm.ReachRenderer.gameObject);
                HandCompanionProp.Kill(arm.Reach);
            }
            _arms[0] = _arms[1] = null;
        }

        public override void Step(ViewmodelArms arms, in ViewmodelArms.HandMood mood, float dt)
        {
            if (_arms[0] == null || _arms[1] == null) return;
            _clock += dt; _landed += dt; _left += dt;

            // ---------------- what has just happened
            float landKick = 0f, leaveKick = 0f;
            if (mood.Grounded != _grounded)
            {
                if (!mood.Grounded) { leaveKick = 3.5f; _left = 0f; }
                else { landKick = 5f + 11f * Mathf.Clamp01(_fallSpeed / 9f); _landed = 0f; }
                _grounded = mood.Grounded;
            }
            _fallSpeed = mood.Grounded ? 0f : Mathf.Max(0f, -mood.VerticalSpeed);
            bool charging = mood.Carrying && mood.Charge >= 0f;
            if (_carrying && !mood.Carrying && _charging) _whip = .7f;
            _carrying = mood.Carrying; _charging = charging;
            if (mood.Casting && !_wasCasting && mood.Action == "vine-reach") _leap = 0f;
            _wasCasting = mood.Casting;
            float leapBefore = _leap;
            if (_leap >= 0f) { _leap += dt; if (_leap > LeapOver) _leap = -1f; }
            _limp = Mathf.MoveTowards(_limp, mood.Tagged ? 1f : 0f, dt / (mood.Tagged ? .3f : .7f));
            _whip = Mathf.Max(0f, _whip - dt);

            float falling = mood.Grounded ? 0f : Mathf.Clamp01((_fallSpeed - 1.5f) / 6f);
            _sinceLand = mood.Grounded != _groundedLast && mood.Grounded ? 0f : _sinceLand + dt;
            _groundedLast = mood.Grounded;
            float stride = mood.Grounded && !mood.Tagged ? Mathf.Clamp01(mood.Walk) : 0f;
            bool quiet = mood.Grounded && !mood.Tagged && !mood.Casting && !mood.Carrying && stride < .3f;
            StepSprout(quiet, dt);
            if (landKick > 0f) _pendingKick = landKick;
            _lateKick = 0f;
            if (_pendingKick > 0f && _sinceLand >= RightLate) { _lateKick = _pendingKick; _pendingKick = 0f; }

            bool reaching = _reachCaster != null && _reachCaster == arms.BoundCharacter && Time.frameCount - _reachFrame <= 1 && _reachOut > .002f;
            for (int a = 0; a < 2; a++)
            {
                var arm = _arms[a];
                // The arm's own mesh may have been given back to it (a rebuild of the arms): take it again.
                if (arm.Filter.sharedMesh != arm.Own) { if (arm.Filter.sharedMesh != arm.Shared) continue; arm.Filter.sharedMesh = arm.Own; }
                bool holds = arm.Right && mood.Carrying;
                // The view's own directions, in this arm's space: down the screen (to hang) and back to the player.
                Vector3 down = arm.Transform.InverseTransformDirection(arms.transform.TransformDirection(Vector3.down));
                Vector3 back = arm.Transform.InverseTransformDirection(arms.transform.TransformDirection(Vector3.back));
                Vector3 across = arm.Transform.InverseTransformDirection(arms.transform.TransformDirection(arm.Right ? Vector3.left : Vector3.right));

                // This arm's own pace, order and lateness.
                float pace = arm.Right ? .71f : .9f, late = arm.Right ? RightLate : 0f, hard = arm.Right ? RightShare : 1f;
                float armLand = arm.Right ? _lateKick : landKick, armLeave = arm.Right ? 0f : leaveKick;
                for (int k = 0; k < arm.Strands.Count; k++)
                {
                    var strand = arm.Strands[k];
                    float own = strand.Share;
                    int turnIndex = arm.Right ? arm.Strands.Count - 1 - k : k;
                    // STANDING: each lengthens and shortens a little on its own slow clock.
                    float grow = .05f + .09f * Mathf.Max(0f, Mathf.Sin(_clock * pace + strand.Beat)) * own;
                    float curl = 1.2f, hang = 0f, sweep = 0f, reach = 0f, shiver = 0f;
                    if (_leap >= 0f)
                    {
                        // The right arm throws first, as his body does; each strand a hair after the one before.
                        float behind = (arm.Right ? 0f : .05f) + turnIndex * .012f, at = _leap - behind;
                        if (at < LeapTell) { grow = -.3f * Mathf.Clamp01(at / .09f); curl = 3.6f; }
                        // ⚠️ THE STRANDS THEMSELVES DO NOT GROW IN A LEAP. They did (out long, as in a fall), and beside the
                        // limbs that now carry the vine they were a second set of spikes standing off his arms (owner,
                        // 2026-10-07: "can you get rid of the original arms extending and spiking when the leap is cast").
                        else if (at < LeapHome) { grow = 0f; curl = .1f; }
                        else { grow = 0f; curl = 1.6f; }
                        // The crack of the whip: a kick outward the moment it leaves.
                    }
                    else if (mood.Casting) { grow = 0f; curl = 0f; }
                    else if (mood.Tagged) { grow = -.12f; hang = 3.5f; }
                    else if (!mood.Grounded)
                    {
                        // Drawn in on the way up; on the way down they shoot out one after another, each to its own length.
                        float turn = Mathf.Clamp01(falling * 1.6f - turnIndex * .16f - late * 2.2f);
                        grow = Mathf.Lerp(-.22f, FallGrow * own * hard, turn * turn * (3f - 2f * turn));
                        curl = Mathf.Lerp(1.2f, .15f, turn); shiver = turn;
                    }
                    else if (holds) { grow = 0f; curl = .6f; }
                    else if (charging) { grow = -.3f * Mathf.Clamp01(mood.Charge * 1.5f - k * .15f); shiver = .3f * Mathf.Clamp01(mood.Charge); }
                    else if (_whip > 0f)
                    {
                        // The throw is the right arm's: out in a ripple, the first strand first. The left only flinches in.
                        float since = .7f - _whip - k * .05f;
                        if (arm.Right) { grow = since > 0f ? ThrowGrow * own * Mathf.Exp(-since * 5.5f) : 0f; curl = .2f; }
                        else grow = -.16f * Mathf.Exp(-Mathf.Max(0f, since) * 4f);
                    }
                    else if (mood.Carrying) { if (k < 2) { grow = .42f * own; reach = 2.4f; curl = .5f; } }     // two of the left's reach across to it
                    else if (_sproutLeft > 0f && a == _sproutArm && (k == _sproutA || k == _sproutB))
                    {
                        // A sprout: out long, a hold while the tip feels about, and back in.
                        float u = 1f - _sproutLeft / SproutSeconds;
                        float outAndIn = Mathf.Clamp01(u / .3f) * Mathf.Clamp01((1f - u) / .25f);
                        outAndIn = outAndIn * outAndIn * (3f - 2f * outAndIn);
                        grow += SproutGrow * own * outAndIn * (k == _sproutB ? .6f : 1f);
                        curl = Mathf.Lerp(curl, .4f + Mathf.Sin(_clock * 3.1f + k) * 1.4f, outAndIn);
                    }
                    if (stride > .01f && !holds)
                    {
                        // They pulse out in turn with his footfalls; in a sprint they are drawn in and swept back.
                        float step = Mathf.Max(0f, Mathf.Sin(mood.GaitPhase * Mathf.PI * 2f + k * 1.57f + (arm.Right ? Mathf.PI : 0f)));
                        grow += (Mathf.Lerp(.16f, .08f, mood.Run) * step - .14f * mood.Run) * stride;
                        sweep = Mathf.Lerp(.4f, 2.4f, mood.Run) * stride;
                    }
                    if (shiver > 0f) grow += Mathf.Sin(_clock * 41f + k * 2.3f) * .02f * shiver;

                    strand.Grow.Target = grow; strand.Curl.Target = curl;
                    // The landing drives each in, a beat apart; the take-off snaps them short.
                    if (armLand > 0f) strand.Grow.Speed -= armLand * own * hard;
                    if (armLeave > 0f) strand.Grow.Speed -= armLeave * own;
                    // Springy wood, each a little slower than the last: it overshoots and settles in its own time.
                    strand.Grow.Step((arm.Right ? 96f : 120f) - 14f * turnIndex, 9f - .6f * turnIndex, dt);
                    strand.Curl.Step(50f, 9f, dt);

                    // The way the NEW length bends as it comes: a fern's curl outward from the braid, and whatever the
                    // air does to it (hanging when limp, swept back in a sprint, across to the slipper).
                    if (reaching)
                    {
                        // THE LEAP: the strand itself holds, a little drawn out, and new wood is grown on from its tip to
                        // where the vine catches (`BuildReach`, below the loop).
                        strand.Grow.Snap(0f);
                        Lay(arm, strand, 0f, Vector3.zero);
                        continue;
                    }
                    Vector3 bend = strand.Outward * strand.Curl.Value + down * (hang * _limp) + back * sweep + across * reach;
                    Lay(arm, strand, Mathf.Clamp(strand.Grow.Value, -.45f, 1.6f), bend);
                }
                arm.Own.SetVertices(arm.Points); arm.Own.SetNormals(arm.Normals);
                if (arm.Ink != null) arm.Own.SetTangents(arm.Ink);
                if (arm.ReachRenderer != null)
                {
                    arm.ReachRenderer.enabled = reaching;
                    if (reaching)
                        BuildReach(arm, arm.Transform.InverseTransformPoint(arms.transform.TransformPoint(arms.DrawnFromWorld(ReachAnchor[a]))), _reachOut, _clock);
                }
            }
        }

        private const float SproutSeconds = 3.2f;

        private void StepSprout(bool quiet, float dt)
        {
            if (!quiet) { _sproutLeft = 0f; _nextSprout = _clock + 1.5f; return; }
            if (_sproutLeft > 0f) { _sproutLeft -= dt; if (_sproutLeft <= 0f) _nextSprout = _clock + Random.Range(1.5f, 3.5f); return; }
            if (_clock < _nextSprout) return;
            _sproutLeft = SproutSeconds; _sproutArm = Random.Range(0, 2); _sproutA = Random.Range(0, 4);
            _sproutB = Random.value < .5f ? (_sproutA + 2) % 4 : -1;
        }

        /// <summary>The mesh the leap's limbs are built in: four tubes of `ReachRings` rings, wearing the arm's own material.</summary>
        private static void MakeReach(Arm arm)
        {
            var source = arm.Filter.GetComponent<MeshRenderer>();
            if (source == null || arm.Strands.Count == 0) return;
            int strands = arm.Strands.Count, perStrand = ReachRings * (ReachSides + 1);
            arm.ReachPoints = new Vector3[strands * perStrand]; arm.ReachNormals = new Vector3[strands * perStrand];
            arm.ReachInk = new Vector4[strands * perStrand]; arm.ReachPaint = new Vector2[strands * perStrand];
            var triangles = new int[strands * (ReachRings - 1) * ReachSides * 6];
            int t = 0;
            for (int k = 0; k < strands; k++)
                for (int r = 0; r < ReachRings - 1; r++)
                    for (int side = 0; side < ReachSides; side++)
                    {
                        int a = k * perStrand + r * (ReachSides + 1) + side, b = a + ReachSides + 1;
                        // Wound so the OUTSIDE is the front (`cross(v1 - v0, v2 - v0)` points out of the limb).
                        triangles[t++] = a; triangles[t++] = a + 1; triangles[t++] = b;
                        triangles[t++] = a + 1; triangles[t++] = b + 1; triangles[t++] = b;
                    }
            arm.Reach = new Mesh { name = "Paete reach" };
            arm.Reach.MarkDynamic();
            arm.Reach.SetVertices(arm.ReachPoints); arm.Reach.SetNormals(arm.ReachNormals);
            arm.Reach.SetTangents(arm.ReachInk); arm.Reach.SetUVs(0, arm.ReachPaint);
            arm.Reach.SetTriangles(triangles, 0);
            arm.Reach.bounds = new Bounds(Vector3.zero, Vector3.one * 200f);
            var go = new GameObject("Paete reach");
            go.transform.SetParent(arm.Transform, false);
            go.layer = arm.Transform.gameObject.layer;
            go.AddComponent<MeshFilter>().sharedMesh = arm.Reach;
            arm.ReachRenderer = go.AddComponent<MeshRenderer>();
            arm.ReachRenderer.sharedMaterial = source.sharedMaterial;
            arm.ReachRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            arm.ReachRenderer.receiveShadows = false;
            arm.ReachRenderer.enabled = false;
            ReachRenderers.RemoveAll(r => r == null);
            ReachRenderers.Add(arm.ReachRenderer);
        }

        /// <summary>
        /// Grows a limb on from each strand's tip to the point `to` (the arm's own space), `out_` of the way there. It
        /// leaves along the strand's own direction and turns to the point over its first stretch; the four wind round
        /// the line between; each is as thick as its strand until its last hand's length, where it comes to a point.
        /// Its paint is the strand's own patch, run back and forth along the length (a repeat with no seam).
        /// </summary>
        private static void BuildReach(Arm arm, Vector3 to, float out_, float clock)
        {
            if (arm.Reach == null) return;
            int perStrand = ReachRings * (ReachSides + 1);
            for (int k = 0; k < arm.Strands.Count; k++)
            {
                var strand = arm.Strands[k];
                Vector3 ahead = strand.Way[Steps - 1];
                // ⚠️ THE JOIN (owner, 2026-10-07: "fix the transition from original arm to arm extension"). The limb began at
                // the strand's very tip, where the strand has already narrowed to a point: a round limb stood on a spike,
                // with a step between them. It now begins well BACK along the strand, runs up the strand's own line as a
                // sleeve a little fatter than the strand (so the strand's taper and point are inside it), and only then
                // leaves for the catch. Seen from his eyes the strand simply thickens into the limb.
                const int SleeveRings = 8;
                float sleeve = strand.Length * .42f;
                Vector3 tip = strand.Line[Steps - 1];
                Vector3 run = to - tip;
                float far = run.magnitude;
                Vector3 way = far > 1e-4f ? run / far : ahead;
                float length = Mathf.Max(.001f, far * out_);
                float turnOver = Mathf.Min(length * .3f, .5f);
                Vector3 sideA = Vector3.Cross(way, Vector3.up); if (sideA.sqrMagnitude < 1e-6f) sideA = Vector3.right; sideA.Normalize();
                Vector3 sideB = Vector3.Cross(way, sideA);
                float point = Mathf.Min(length * .5f, strand.Thick * 7f);
                for (int r = 0; r < ReachRings; r++)
                {
                    Vector3 centre, wayNow; float radius, past;
                    if (r < SleeveRings)
                    {
                        // Up the strand's own line, from `sleeve` back to its tip.
                        float back = sleeve * (1f - r / (float)SleeveRings);
                        float at = strand.Length - back;
                        int s1 = 0;
                        while (s1 < Steps - 2 && strand.Far[s1 + 1] < at) s1++;
                        float u1 = Mathf.InverseLerp(strand.Far[s1], strand.Far[s1 + 1], at);
                        centre = Vector3.Lerp(strand.Line[s1], strand.Line[s1 + 1], u1);
                        wayNow = Vector3.Slerp(strand.Way[s1], strand.Way[s1 + 1], u1).normalized;
                        // Buried in the strand at its start, swelling to the limb's own girth by the tip.
                        radius = strand.Thick * Mathf.Lerp(.55f, 1.12f, r / (float)SleeveRings) * Mathf.Clamp01(out_ * 6f);
                        past = -back;
                    }
                    else
                    {
                        // Rings bunch a little at both ends, where the limb turns and where it comes to its point.
                        float u = (r - SleeveRings) / (float)(ReachRings - 1 - SleeveRings);
                        past = length * (u * u * (3f - 2f * u) * .5f + u * .5f);
                        float turned01 = turnOver > 1e-4f ? Mathf.Clamp01(past / turnOver) : 1f;
                        float eased = turned01 * turned01 * (3f - 2f * turned01);
                        wayNow = Vector3.Slerp(ahead, way, eased).normalized;
                        centre = tip + way * past + (ahead - way) * (turnOver * .35f * (1f - turned01) * turned01);
                        float wind = k * 1.57f + past * 2.2f;
                        float wide = strand.Thick * 1.1f * Mathf.Clamp01(past / .25f) * Mathf.Clamp01((length - past) / .25f);
                        centre += (sideA * Mathf.Cos(wind) + sideB * Mathf.Sin(wind)) * wide;
                        radius = strand.Thick * 1.12f * Mathf.Clamp01((length - past) / point) * (1f + .08f * Mathf.Sin(past * 9f + k + clock * 3f));
                    }
                    Vector3 across = Vector3.Cross(wayNow, sideA); if (across.sqrMagnitude < 1e-6f) across = sideB; across.Normalize();
                    Vector3 other = Vector3.Cross(wayNow, across);
                    // The strand's patch, back and forth every 0.4 of length, and once round the limb.
                    float run01 = Mathf.PingPong((past + sleeve) / .4f, 1f);
                    for (int side = 0; side <= ReachSides; side++)
                    {
                        float turn = side / (float)ReachSides * Mathf.PI * 2f;
                        Vector3 normal = across * Mathf.Cos(turn) + other * Mathf.Sin(turn);
                        int i = k * perStrand + r * (ReachSides + 1) + side;
                        arm.ReachPoints[i] = centre + normal * radius;
                        arm.ReachNormals[i] = normal;
                        arm.ReachInk[i] = new Vector4(normal.x, normal.y, normal.z, 1f);
                        arm.ReachPaint[i] = new Vector2(Mathf.Lerp(strand.PaintFrom.x, strand.PaintTo.x, Mathf.PingPong(side / (float)ReachSides * 2f, 1f)),
                            Mathf.Lerp(strand.PaintFrom.y, strand.PaintTo.y, run01));
                    }
                }
            }
            arm.Reach.SetVertices(arm.ReachPoints); arm.Reach.SetNormals(arm.ReachNormals);
            arm.Reach.SetTangents(arm.ReachInk); arm.Reach.SetUVs(0, arm.ReachPaint);
        }

        /// <summary>
        /// Moves one strand's points `grow` further along its own line (a share of its length; negative draws it in),
        /// and past the line's end out along its last direction on an arc that bends the way `bend` says (radians for
        /// each unit of new length).
        /// </summary>
        private static void Lay(Arm arm, Strand strand, float grow, Vector3 bend)
        {
            Vector3 tip = strand.Line[Steps - 1], ahead = strand.Way[Steps - 1];
            // The bend is across the tip's direction; no tighter than a curl that closes on itself.
            bend -= ahead * Vector3.Dot(bend, ahead);
            float sharp = Mathf.Min(bend.magnitude, 9f);
            Vector3 way = sharp > 1e-4f ? bend.normalized : strand.Outward;
            Vector3 axis = Vector3.Cross(ahead, way);
            float scale = 1f + grow;
            var points = strand.Points;
            for (int n = 0; n < points.Length; n++)
            {
                int index = points[n];
                float far = strand.Along[n] * scale;
                // Where it stood on the line, to turn its offset from that direction to the new one.
                float was = strand.Along[n];
                int s0 = 0;
                while (s0 < Steps - 2 && strand.Far[s0 + 1] < was) s0++;
                float u0 = Mathf.InverseLerp(strand.Far[s0], strand.Far[s0 + 1], was);
                Vector3 wayWas = Vector3.Slerp(strand.Way[s0], strand.Way[s0 + 1], u0);

                Vector3 centre, wayNow;
                if (far <= strand.Length)
                {
                    int s1 = 0;
                    while (s1 < Steps - 2 && strand.Far[s1 + 1] < far) s1++;
                    float u1 = Mathf.InverseLerp(strand.Far[s1], strand.Far[s1 + 1], far);
                    centre = Vector3.Lerp(strand.Line[s1], strand.Line[s1 + 1], u1);
                    wayNow = Vector3.Slerp(strand.Way[s1], strand.Way[s1 + 1], u1);
                }
                else
                {
                    float past = far - strand.Length, angle = sharp * past;
                    if (sharp > 1e-3f)
                    {
                        centre = tip + ahead * (Mathf.Sin(angle) / sharp) + way * ((1f - Mathf.Cos(angle)) / sharp);
                        wayNow = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, axis) * ahead;
                    }
                    else { centre = tip + ahead * past; wayNow = ahead; }
                }
                Quaternion turned = Quaternion.FromToRotation(wayWas, wayNow);
                arm.Points[index] = centre + turned * strand.Off[n];
                arm.Normals[index] = turned * arm.RestNormals[index];
                if (arm.Ink != null)
                {
                    Vector4 ink = arm.RestInk[index];
                    Vector3 welded = turned * new Vector3(ink.x, ink.y, ink.z);
                    arm.Ink[index] = new Vector4(welded.x, welded.y, welded.z, ink.w);
                }
            }
        }
    }
}
