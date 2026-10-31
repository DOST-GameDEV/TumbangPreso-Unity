using UnityEngine;
using Part = TumbangPreso.Visual.ZackKitPaint.Part;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// MOMENTUM, PAINTED: what `ZackMomentumLook` draws with its `Painted` switch on. Owner, 2026-10-26: *"Its benefit and VFX
    /// currently barely register"*. The old look hung thin polygon rings on the court, a wire lattice and heel lines on him.
    ///
    /// THE GAMEPLAY SHAPE THE PICTURE TELLS THE TRUTH ABOUT. Running pays 8 percent at once and climbs to 25 over 2 s, in stages at
    /// 0.7, 1.4 and 2.0 s; a stop past 0.25 s loses it all. It touches nobody, so nothing here implies an area. The picture is
    /// GEARS, and the stage can be counted at any moment:
    ///   * THE WAKE: one strand of b13d's silk per stage (ankle, knee, hip), each stage longer than the last, all of them left
    ///     on the road behind him where he actually ran. Every point is aged in the shader, so it swells and is eaten where it lies.
    ///   * THE SHIFT: z4's strike under him, one z4 bolt per gear thrown back along the road, a b13d burst and a kick of the approved
    ///     cloud. The third shift's cloud opens as a cone behind him, the sonic boom.
    ///   * THE STOP: the wake stays where it lies and is eaten; a skid puff. Nothing pops.
    /// Nothing is drawn on his body or in front of it (owner: *"this weird small torch flame/nitro effect to the arms and body of
    /// zack. need to get rid of those"*). A bystander and a target see all of it; his own first person sees none of it, because
    /// all of it is behind him (the look keeps his lens and edge cues).
    ///
    /// ⚠️ GPU-DRIVEN. The silk ages in `ZackTrail.shader`, the bolt snaps by swapping a prebuilt mesh, the burst flies in
    /// `ZackSparks.shader`, the dust moves in `FxStudyCloud.shader`. A frame sets transforms and property blocks and writes one
    /// slot of a route ring. Nothing is allocated after the constructor.
    ///
    /// ⚠️ WHERE A REUSED PIECE DOES NOT QUITE FIT. A silk leg is one chord and one bow, so on a tight turn a long strand cuts the
    /// corner a little.
    /// </summary>
    public sealed class ZackMomentumPaint
    {
        private const int Strands = 3, Keep = 120, Bolts = 3;
        private const float Interval = 0.01f;
        // Height off the road, how far out to his side its tail lies, width of the silk and of its core, per strand. All of it low, at his feet:
        // the first build stacked them ankle, knee and hip, and a short strand coming off a hip is a nitro exhaust.
        private static readonly float[] Height = { 0.14f, 0.20f, 0.20f }, Out = { 0f, -0.55f, 0.55f }, Wide = { 0.62f, 0.56f, 0.56f }, CoreWide = { 0.22f, 0.19f, 0.19f };
        // How many seconds of his run each strand holds, by stage (index stage 1 to 3).
        private static readonly float[,] Hold = { { 0.28f, 0.42f, 0.62f }, { 0f, 0.34f, 0.52f }, { 0f, 0f, 0.46f } };
        private static readonly Color DustLit = new Color(0.90f, 0.82f, 0.68f), DustShade = new Color(0.70f, 0.58f, 0.44f);
        private const float ShiftLife = 1.0f;
        // Metres behind the middle of his feet that the wake's root sits (just off the back of the shoe), how high it leaves the road (the shoe's heel,
        // under his trouser hem), seconds of road the root used to stop short by, metres its root fades over, and how fast the root lets go of him once he stands.
        private const float HeelBack = 0.20f, HeelHeight = 0.09f, RootLag = 0.07f, RootFade = 0.10f, LetGo = 2f;

        private readonly Part[] _silk = new Part[Strands], _core = new Part[Strands], _bolt = new Part[Bolts], _cross = new Part[Bolts];
        private readonly Part _star, _burst;
        private readonly FxCloud _kick;
        private readonly Vector3[] _route = new Vector3[Keep];
        private readonly float[] _routeClock = new float[Keep];
        private int _routeHead, _routeCount;
        private readonly bool[] _live = new bool[Strands];
        private readonly float[] _hold = new float[Strands], _extra = new float[Strands], _headClock = new float[Strands];
        private float _lastAge, _running, _nextRecord, _idle, _speed = 6f;
        private Vector3 _last, _heading = Vector3.forward;
        private readonly Vector3[] _root = new Vector3[Strands];
        private bool _started;
        private int _shown;
        // The last kick (a shift, or a stop's skid).
        private float _shiftAge = -100f;
        private int _shiftStep;
        private Vector3 _shiftAt, _shiftFwd, _shiftRight;
        private FxCloud.Emitter _kickEmitter;
        private bool _kickSet;

        public ZackMomentumPaint(Transform parent)
        {
            var go = new GameObject("ZackMomentumPaint") { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var root = go.transform;
            for (int i = 0; i < Strands; i++)
            {
                _silk[i] = new Part("wake-" + i, root, ZackKitPaint.Even, ZackKitPaint.Silk);
                _core[i] = new Part("wake-core-" + i, root, ZackKitPaint.Even, ZackKitPaint.Silk);
            }
            for (int i = 0; i < Bolts; i++)
            {
                _bolt[i] = new Part("shift-bolt-" + i, root, ZackKitPaint.BoltShape(i, false), ZackKitPaint.Bolt);
                _cross[i] = new Part("shift-cross-" + i, root, ZackKitPaint.BoltShape(i, true), ZackKitPaint.Bolt);
            }
            _star = new Part("shift-star", root, ZackKitPaint.FloorSquare, ZackKitPaint.Strike);
            _burst = new Part("shift-burst", root, ZackKitPaint.Burst, ZackKitPaint.Sparks);
            _kick = FxCloud.Build(root, "shift-dust", 11, 72, DustLit, DustShade, DustShade, 0f, 0.05f, 0.40f);
            VfxRenderTag.Attach(_kick.Piece.Go);
            _kick.Piece.Frame(0f, 0f);
        }

        public void Dispose() { _kick.Dispose(); }

        /// <summary>Draw calls live this frame, for the bench's cost line.</summary>
        public int LiveDraws
        {
            get
            {
                int n = 0;
                for (int i = 0; i < Strands; i++) { if (_silk[i].Shown) n++; if (_core[i].Shown) n++; }
                for (int i = 0; i < Bolts; i++) { if (_bolt[i].Shown) n++; if (_cross[i].Shown) n++; }
                if (_star.Shown) n++;
                if (_burst.Shown) n++;
                if (_kick.Piece.Go.activeSelf) n++;
                return n;
            }
        }

        private Vector3 RouteAt(float clock)
        {
            if (_routeCount == 0) return _last;
            for (int n = 0; n < _routeCount; n++)
            {
                int i = (_routeHead - n + Keep) % Keep;
                if (_routeClock[i] > clock) continue;
                if (n == 0) return _route[i];
                int newer = (i + 1) % Keep;
                float span = _routeClock[newer] - _routeClock[i];
                return Vector3.Lerp(_route[i], _route[newer], span > 1e-5f ? (clock - _routeClock[i]) / span : 0f);
            }
            return _route[(_routeHead - _routeCount + 1 + Keep) % Keep];
        }

        /// <summary>
        /// One frame: `age` a running clock in seconds, `step` the Momentum stage this screen counts (0 to 3), `at` his feet, `fwd` his facing.
        /// </summary>
        public void Pose(float age, int step, Vector3 at, Vector3 fwd, bool eyes, bool reduced)
        {
            fwd.y = 0f; fwd = fwd.sqrMagnitude > 1e-4f ? fwd.normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            float dt = _started ? Mathf.Max(0f, age - _lastAge) : 0f;
            _lastAge = age;
            Vector3 moved = _started ? at - _last : Vector3.zero;
            moved.y = 0f;
            float speed = dt > 1e-4f ? moved.magnitude / dt : 0f;
            _started = true;
            // A jump of metres in a frame is a teleport (a Backtrack, a respawn): the wake lets go where it is.
            if (speed >= 30f) { _routeCount = 0; speed = 0f; for (int i = 0; i < Strands; i++) _live[i] = false; }
            bool moving = speed > 1f;
            _last = at;
            if (moving)
            {
                _running += dt; _idle = 0f;
                _speed = Mathf.Lerp(_speed, speed, 1f - Mathf.Exp(-dt * 6f));
                // The way he is travelling, not the way he faces: "behind him" has to mean behind the run while he strafes, and it holds still through a stop.
                Vector3 turned = Vector3.Lerp(_heading, moved / (speed * dt), 1f - Mathf.Exp(-dt * 18f));
                if (turned.sqrMagnitude > 1e-4f) _heading = turned.normalized;
            }
            else _idle += dt;
            if (_routeCount == 0 || (moving && _running >= _nextRecord))
            {
                _nextRecord = _running + Interval;
                _routeHead = (_routeHead + 1) % Keep;
                _route[_routeHead] = at; _routeClock[_routeHead] = _running;
                _routeCount = Mathf.Min(_routeCount + 1, Keep);
            }

            // A shift, or a stop that loses two gears or more.
            if (step > _shown) Shift(age, step, at, fwd, right);
            else if (step == 0 && _shown >= 2) Skid(age, at, fwd, right);
            _shown = step;

            StepWake(age, dt, step, eyes, reduced);
            StepShift(age, eyes, reduced);
        }

        private void StepWake(float age, float dt, int step, bool eyes, bool reduced)
        {
            for (int i = 0; i < Strands; i++)
            {
                bool want = step > i && !eyes && (!reduced || i == 0);
                if (want)
                {
                    if (!_live[i]) { _live[i] = true; _hold[i] = 0f; _extra[i] = 0f; }
                    _headClock[i] = _running;
                    _root[i] = _last - _heading * HeelBack;
                    _hold[i] = Mathf.MoveTowards(_hold[i], Hold[i, Mathf.Clamp(step, 1, 3) - 1], dt * 0.8f);
                    // Standing inside the grace it starts to go, and comes straight back if he runs on.
                    _extra[i] = _idle > 0.03f ? _extra[i] + dt : Mathf.MoveTowards(_extra[i], 0f, dt * 4f);
                }
                else if (_live[i]) { _extra[i] += dt; if (_extra[i] > _hold[i] + 0.06f) _live[i] = false; }
                if (!_live[i] || _hold[i] < 0.02f) { _silk[i].Hide(); _core[i].Hide(); continue; }

                // ⚠️ THE ROOT IS LIVE, NOT A RECORDED POINT. It sits at his heels this very frame while he has the gear, and stays where it lay once he
                // loses it. Owner, of a head read 0.07 s back down the route: *"should probably attach the trail closer"*. At a run that gap was half a metre.
                // The far end stays where it was (`lead` is that old gap, eased in so a new strand does not open at full reach), so the wake gets no longer.
                float hold = _hold[i], lead = RootLag * FxStudyKit.Run(hold, 0.02f, 0.20f), clock = _headClock[i];
                Vector3 up = Vector3.up * Height[i];
                Vector3 head = _root[i] + Vector3.up * HeelHeight, tail = Behind(RouteAt(clock - lead - hold) + up + Vector3.up * 0.14f, head);
                // Standing, a point's age climbs and the root would swell around his shins. It backs away along the road as it is eaten, and returns if he runs on.
                head = Vector3.MoveTowards(head, tail, Mathf.Min(_extra[i] * LetGo, Vector3.Distance(head, tail) * 0.45f));
                Vector3 chord = head - tail;
                Vector3 across = Vector3.Cross(Vector3.up, chord.sqrMagnitude > 1e-4f ? chord.normalized : Vector3.forward);
                // A boat's wake: the strands open away from his line as they fall behind.
                tail += across * Out[i]; head += across * (Out[i] * 0.2f);
                // Secondary motion: each strand sways on its own beat, so the three never read as rails.
                Vector3 middle = Behind(RouteAt(clock - (lead + hold) * 0.5f) + up + Vector3.up * 0.05f, head) + across * (Out[i] * 0.55f)
                               + across * (Mathf.Sin(age * (5.1f + 1.3f * i) + i * 2.1f) * (0.05f + 0.10f * hold));
                float length = _silk[i].Lay(tail, head, middle);
                _core[i].Lay(tail, head, middle);
                float stretch = Mathf.Max(0.2f, length / ZackKitPaint.Long);
                ZackKitPaint.DressLeg(_silk[i], _core[i], i + 1, length, stretch, Mathf.Max(3f, _speed), _extra[i], 1f, age);
                // The silk's life is sized for this strand's length, not for a shoe's flight.
                // Thin where it leaves and widest well behind him. Wide at the root and pointed at the tip is a flame; this is the reverse.
                // Every age carries `lead`, because the root is now that much younger than the point these lives were sized from, and the tail dies where it did.
                // The floor on full width keeps a strand just born thin too; a short strand at full width right at his heels is a torch.
                var grow = new Vector4(0.10f, lead + Mathf.Max(0.16f, hold * 0.55f), 0.80f, lead + Mathf.Max(0.21f, hold));
                // The root's fade is a fixed few centimetres of road, not a share of the strand: a share of a long strand was another 40 cm of gap.
                // Backing away from him it is eaten from that end as well, so the end it leaves is a taper and never a blunt cut.
                var ends = new Vector4(0f, Mathf.Clamp(RootFade / Mathf.Max(0.01f, length), 0.01f, 0.14f) + Mathf.Min(0.35f, _extra[i] * 1.2f), 0.30f, 0f);
                _silk[i].Vec("_Ends", ends); _core[i].Vec("_Ends", ends);
                _silk[i].Vec("_Grow", grow); _silk[i].Vec("_Die", new Vector4(lead + hold * 0.6f, lead + hold, 0f, 0f)); _silk[i].Facing(Wide[i], i * 60f + 30f);
                _core[i].Vec("_Grow", grow); _core[i].Vec("_Die", new Vector4(lead + hold * 0.4f, lead + hold * 0.8f, 0f, 0f)); _core[i].Facing(CoreWide[i], i * 60f + 75f);
                _silk[i].Frame(age, 1f); _core[i].Frame(age, 1f);
            }
        }

        // A doubled-back run lays old road ahead of him. The wake is pushed back level with its root rather than drawn forward through his legs.
        private Vector3 Behind(Vector3 point, Vector3 root)
        {
            float ahead = Vector3.Dot(point - root, _heading);
            return ahead > 0f ? point - _heading * ahead : point;
        }

        private void Shift(float age, int step, Vector3 at, Vector3 fwd, Vector3 right)
        {
            _shiftAge = age; _shiftStep = step; _shiftAt = at; _shiftFwd = fwd; _shiftRight = right;
            Vector3 source = at + Vector3.up * 0.12f - fwd * 0.2f;
            var e = FxCloud.Emitter.Puff(source, -fwd * (0.9f + 0.5f * step) + Vector3.up * (0.35f + 0.2f * step), age, 0.10f + 0.04f * step, 0.46f + 0.06f * step);
            e.Spread = 0.25f + 0.08f * step; e.Widen = 0.20f + 0.14f * step; e.Weave = 0.08f;
            // Few sizes and all of them big enough to overlap: small balls strung out read as beads, not dust.
            e.Small = 0.10f + 0.02f * step; e.Large = 0.20f + 0.07f * step; e.Gone = 0.06f;
            e.PeakLow = 0.25f; e.PeakHigh = 0.45f; e.SharpLow = 1.0f; e.SharpHigh = 2.0f;
            if (step >= 3)
            {
                // The sonic boom: the same dust opening as a cone behind him.
                e.Rise = -fwd * 1.5f + Vector3.up * 0.45f; e.Widen = 1.25f; e.Spread = 0.7f; e.Large = 0.38f; e.Small = 0.14f; e.To = age + 0.30f; e.Life = 0.62f;
            }
            _kickEmitter = e; _kickSet = true;
        }

        private void Skid(float age, Vector3 at, Vector3 fwd, Vector3 right)
        {
            _shiftAge = age; _shiftStep = 0; _shiftAt = at; _shiftFwd = fwd; _shiftRight = right;
            var e = FxCloud.Emitter.Puff(at + Vector3.up * 0.1f + fwd * 0.2f, fwd * 0.5f + Vector3.up * 0.45f, age, 0.12f, 0.55f);
            e.Spread = 0.5f; e.Widen = 0.35f; e.Weave = 0.06f; e.Small = 0.09f; e.Large = 0.24f; e.Gone = 0.06f;
            e.PeakLow = 0.25f; e.PeakHigh = 0.45f; e.SharpLow = 1.0f; e.SharpHigh = 2.0f;
            _kickEmitter = e; _kickSet = true;
        }

        private void StepShift(float age, bool eyes, bool reduced)
        {
            float since = age - _shiftAge;
            if (_kickSet && since < ShiftLife && !eyes) { _kick.Piece.Frame(0f, 1f); _kick.Pose(age, _kickEmitter); }
            else _kick.Piece.Frame(0f, 0f);
            bool shift = _shiftStep > 0 && since < 0.5f && !eyes;
            if (shift)
            {
                float yaw = Mathf.Atan2(_shiftFwd.x, _shiftFwd.z) * Mathf.Rad2Deg + 22f;
                ZackKitPaint.PoseStrike(_star, _shiftAt, yaw, 0.8f + 0.42f * _shiftStep, since, 0.06f, 0.34f, age);
                ZackKitPaint.PoseBurst(_burst, _shiftAt + Vector3.up * 0.2f - _shiftFwd * 0.2f, Vector3.up - _shiftFwd * 0.8f, _shiftAge, age, 0.6f + 0.2f * _shiftStep);
            }
            else { _star.Hide(); _burst.Hide(); }
            for (int i = 0; i < Bolts; i++)
            {
                float local = (since - i * 0.035f) / 0.20f;
                if (!shift || i >= _shiftStep || local < 0f || local >= 1f || (reduced && i > 0)) { _bolt[i].Hide(); _cross[i].Hide(); continue; }
                float sign = i == 0 ? 0.5f : i == 1 ? -1f : 1f;
                Vector3 from = _shiftAt + _shiftRight * (0.14f * sign) + Vector3.up * 0.14f - _shiftFwd * 0.15f;
                Vector3 to = from - _shiftFwd * (1.1f + 0.45f * _shiftStep) + _shiftRight * (sign * 0.55f) + Vector3.up * 0.04f;
                LayBolt(_bolt[i], _cross[i], from, to, local, Mathf.FloorToInt(since * 20f) + i * 2, i, age, 1f);
            }
        }

        /// <summary>
        /// One of z4's bolts from `from` to `to`, drawn out in the first quarter of `local` (0 to 1) and let go from its root in the second half:
        /// its flat side stays upright and its crossed copy lies flat, so one of the two is broad to whoever looks.
        /// </summary>
        public static void LayBolt(Part bolt, Part cross, Vector3 from, Vector3 to, float local, int strike, int salt, float age, float opacity, float width = 1f)
        {
            Vector3 along = to - from;
            float length = along.magnitude;
            Vector3 x = along / Mathf.Max(1e-4f, length), z = Vector3.Cross(x, Vector3.up);
            if (z.sqrMagnitude < 1e-4f) z = Vector3.forward;
            z.Normalize();
            var turn = Quaternion.LookRotation(z, Vector3.Cross(z, x));
            int shape = strike + salt * 2;
            float drawn = FxStudyKit.EaseOut(FxStudyKit.Run(local, 0f, 0.25f)) * 1.1f, tail = FxStudyKit.Ease(FxStudyKit.Run(local, 0.5f, 1f)) * 1.1f - 0.1f;
            float jump = FxStudyKit.Hash(strike, 5 + salt), stretch = length / ZackKitPaint.BoltLength;
            float thick = Mathf.Clamp(Mathf.Sqrt(stretch), 0.5f, 1f) * width;
            for (int k = 0; k < 2; k++)
            {
                bool crossed = k == 1;
                var piece = crossed ? cross : bolt;
                piece.Mesh = ZackKitPaint.BoltShape(shape, crossed);
                piece.At.SetPositionAndRotation(from, turn);
                piece.At.localScale = new Vector3(stretch, thick, thick);
                piece.Window(tail, drawn, 0.03f);
                piece.Tile(0.8f * stretch, 1f, jump + (crossed ? 0.37f : 0f), 0f);
                piece.Frame(age, (crossed ? 0.9f : 1f) * opacity, 0.8f * FxStudyKit.Run(local, 0.7f, 1f));
            }
        }
    }
}
