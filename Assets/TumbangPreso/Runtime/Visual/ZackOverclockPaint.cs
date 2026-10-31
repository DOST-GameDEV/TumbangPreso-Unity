using UnityEngine;
using Part = TumbangPreso.Visual.ZackKitPaint.Part;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// OVERCLOCKED, PAINTED: what `ZackOverclockLook` draws with its `Painted` switch on. The old look was lines crawling on his
    /// body, gold copies of him over his own back and an ink line at his feet, and it blinked off at zero.
    ///
    /// THE GAMEPLAY SHAPE THE PICTURE TELLS THE TRUTH ABOUT. 25 s of fuel: 30 percent faster, faster cooldowns, free sprint. It
    /// touches nobody by itself (the zap is its own look), so the arcs land where nothing is hit and are too brief and too random
    /// to read as an area. The picture is AN OVERLOADED MACHINE, more charge than one body holds, spilling out:
    ///   * THE SCORCH: where an arc lands, a small dark puff of the approved cloud rises and is left behind. It is the value a pale
    ///     street lacks: lemon bolts read against it without bloom. A plume dragged at his heels was tried first and cut: from a
    ///     chase camera it was a dark slab over his legs.
    ///   * THE ARCS: z4's bolts jump from the air beside him down to the road on an irregular beat, each landing z4's strike and
    ///     b13d's sparks. The top of each stays with him and the foot stays where it struck, so it stretches as he runs on.
    ///   * THE DUMP: every few arcs a short silence, then a tent of bolts from over his head to the road all round him.
    ///   * THE LAST 5 S: arcs misfire and die in the air and the silences lengthen.
    ///   * ZERO: one last tent, whose scorches rise and finish where they are. Nothing blinks off.
    /// Nothing is on his body (owner: *"this weird small torch flame/nitro effect to the arms and body of zack"*) and no copy of
    /// him is drawn. His own first person: the arcs land ahead of him where he can see them.
    ///
    /// ⚠️ GPU-DRIVEN, as `ZackMomentumPaint`: prebuilt meshes, transforms and property blocks; nothing allocated after the constructor.
    /// </summary>
    public sealed class ZackOverclockPaint
    {
        /// <summary>Seconds a release takes to finish: the last of the plume's balls and the blowout.</summary>
        public const float EndSeconds = 1.5f;

        private const int Arcs = 6, Puffs = 3;
        private const float ArcLife = 0.17f, StarLife = 0.42f;
        private static readonly Color ScorchLit = new Color(0.36f, 0.31f, 0.28f), ScorchShade = new Color(0.22f, 0.16f, 0.13f);

        private readonly Part[] _bolt = new Part[Arcs], _cross = new Part[Arcs], _star = new Part[Arcs], _burst = new Part[Arcs];
        private readonly FxCloud[] _puff = new FxCloud[Puffs];
        private readonly FxCloud.Emitter[] _puffSet = new FxCloud.Emitter[Puffs];
        private readonly float[] _puffAge = { -100f, -100f, -100f };
        private int _nextPuff;
        // Each arc: when it fired (-100 for none), how far along its length it reaches (below 1 is a misfire), its star's size,
        // where its foot struck, its top's offset from him, whether it belongs to a tent.
        private readonly float[] _born = new float[Arcs], _reach = new float[Arcs], _size = new float[Arcs];
        private readonly Vector3[] _foot = new Vector3[Arcs], _top = new Vector3[Arcs];
        private readonly bool[] _crown = new bool[Arcs];
        private int _slot, _count, _dumpIn = 3;
        private float _next, _lastAge, _endAge = -100f;
        private bool _started, _ended;
        private Vector3 _last;

        public ZackOverclockPaint(Transform parent)
        {
            var go = new GameObject("ZackOverclockPaint") { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var root = go.transform;
            for (int i = 0; i < Arcs; i++)
            {
                _bolt[i] = new Part("arc-" + i, root, ZackKitPaint.BoltShape(i, false), ZackKitPaint.Bolt);
                _cross[i] = new Part("arc-cross-" + i, root, ZackKitPaint.BoltShape(i, true), ZackKitPaint.Bolt);
                _star[i] = new Part("arc-star-" + i, root, ZackKitPaint.FloorSquare, ZackKitPaint.Strike);
                _burst[i] = new Part("arc-burst-" + i, root, ZackKitPaint.Burst, ZackKitPaint.Sparks);
                _born[i] = -100f;
            }
            for (int i = 0; i < Puffs; i++)
            {
                _puff[i] = FxCloud.Build(root, "overclock-scorch-" + i, 21 + i, 24, ScorchLit, ScorchShade, ScorchShade, 0f, 0.08f, 0.30f);
                VfxRenderTag.Attach(_puff[i].Piece.Go);
                _puff[i].Piece.Frame(0f, 0f);
            }
        }

        public void Dispose() { for (int i = 0; i < Puffs; i++) _puff[i].Dispose(); }

        /// <summary>Draw calls live this frame, for the bench's cost line.</summary>
        public int LiveDraws
        {
            get
            {
                int n = 0;
                for (int i = 0; i < Arcs; i++)
                {
                    if (_bolt[i].Shown) n++;
                    if (_cross[i].Shown) n++;
                    if (_star[i].Shown) n++;
                    if (_burst[i].Shown) n++;
                }
                for (int i = 0; i < Puffs; i++) if (_puff[i].Piece.Go.activeSelf) n++;
                return n;
            }
        }

        /// <summary>One frame: `age` a running clock, `at` his feet, `fwd` his facing, `left` seconds of fuel, `over` true from the moment it ended.</summary>
        public void Pose(float age, Vector3 at, Vector3 fwd, float left, bool over, bool eyes, bool reduced)
        {
            fwd.y = 0f; fwd = fwd.sqrMagnitude > 1e-4f ? fwd.normalized : Vector3.forward;
            float dt = _started ? Mathf.Max(0f, age - _lastAge) : 0f;
            _lastAge = age;
            _last = at;
            if (!_started) { _started = true; _next = age; }

            if (over && !_ended)
            {
                _ended = true; _endAge = age;
                Dump(age, at, fwd, Arcs, reduced ? 2.0f : 2.5f);
            }
            float stutter = over ? 1f : 1f - Mathf.Clamp01(left / 5f);
            if (!over && age >= _next)
            {
                _count++;
                if (--_dumpIn <= 0 && stutter <= 0f)
                {
                    Dump(age, at, fwd, reduced ? 3 : 4, 2.2f);
                    _dumpIn = 7 + (_count % 4);
                    _next = age + 0.42f;
                }
                else
                {
                    Fire(age, at, fwd, eyes, stutter);
                    float gap = Mathf.Lerp(0.09f, 0.34f, FxStudyKit.Hash(_count, 41));
                    // The arc before a dump is followed by the silence that sets it up.
                    if (_dumpIn == 1 && stutter <= 0f) gap = 0.36f;
                    if (stutter > 0f) gap *= Mathf.Lerp(1.2f, 2.4f, FxStudyKit.Hash(_count, 43));
                    _next = age + gap * (reduced ? 2f : 1f);
                }
            }
            StepArcs(age, at, reduced);
            StepPuffs(age, eyes);
        }

        private void Fire(float age, Vector3 at, Vector3 fwd, bool eyes, float stutter)
        {
            int n = _count;
            float side = FxStudyKit.Hash(n, 31) < 0.5f ? -1f : 1f;
            // Off his sides and behind him; in his own eyes, ahead of him where he can see it.
            float bearing = eyes ? side * Mathf.Lerp(16f, 55f, FxStudyKit.Hash(n, 32)) : side * Mathf.Lerp(60f, 165f, FxStudyKit.Hash(n, 32));
            Vector3 dir = Quaternion.Euler(0f, bearing, 0f) * fwd;
            float radius = eyes ? Mathf.Lerp(2.5f, 4f, FxStudyKit.Hash(n, 33)) : Mathf.Lerp(1.5f, 2.5f, FxStudyKit.Hash(n, 33));
            bool misfire = stutter > 0f && FxStudyKit.Hash(n, 34) < 0.35f + 0.5f * stutter;
            int i = _slot; _slot = (_slot + 1) % Arcs;
            _born[i] = age; _crown[i] = false;
            _foot[i] = at + dir * radius;
            _top[i] = dir * (eyes ? 1.0f : 0.55f) + Vector3.up * Mathf.Lerp(0.75f, 1.35f, FxStudyKit.Hash(n, 35));
            _reach[i] = misfire ? Mathf.Lerp(0.35f, 0.6f, FxStudyKit.Hash(n, 36)) : 1f;
            _size[i] = Mathf.Lerp(0.85f, 1.3f, FxStudyKit.Hash(n, 37));
            if (!misfire) Scorch(age, _foot[i], 0.8f);
        }

        private void Dump(float age, Vector3 at, Vector3 fwd, int bolts, float radius)
        {
            float start = FxStudyKit.Hash(_count, 51) * 360f;
            for (int k = 0; k < bolts; k++)
            {
                int i = _slot; _slot = (_slot + 1) % Arcs;
                Vector3 dir = Quaternion.Euler(0f, start + k * 360f / bolts + 18f * FxStudyKit.Hash(_count + k, 52), 0f) * fwd;
                // Staggered by a frame or two so the tent is struck, not switched on.
                _born[i] = age + k * 0.012f; _crown[i] = true;
                _foot[i] = at + dir * (radius * Mathf.Lerp(0.85f, 1.1f, FxStudyKit.Hash(_count + k, 53)));
                _top[i] = Vector3.up * 2.35f + dir * 0.70f;
                _reach[i] = 1f;
                _size[i] = 1.5f;
                if (k < Puffs) Scorch(_born[i], _foot[i], 1f);
            }
        }

        private void StepArcs(float age, Vector3 at, bool reduced)
        {
            for (int i = 0; i < Arcs; i++)
            {
                float since = age - _born[i];
                float life = _crown[i] ? ArcLife * 1.5f : ArcLife;
                float local = since / life;
                if (since < 0f || local >= 1f) { _bolt[i].Hide(); _cross[i].Hide(); }
                else
                {
                    Vector3 top = at + _top[i];
                    Vector3 foot = Vector3.Lerp(top, _foot[i] + Vector3.up * 0.02f, _reach[i]);
                    int strike = Mathf.FloorToInt(since * 28f) + i * 3;
                    // It flickers as it holds: arcing, not a drawn line.
                    float flick = reduced || Mathf.Repeat(since, 0.05f) < 0.033f ? 1f : 0.45f;
                    // Up close a full-width tent bolt filled a chase camera's frame and hid him.
                    ZackMomentumPaint.LayBolt(_bolt[i], _cross[i], top, foot, local, strike, i, age, flick, _crown[i] ? 0.7f : 1f);
                }
                bool landed = _reach[i] >= 1f && since >= 0.02f && since < StarLife;
                if (landed) ZackKitPaint.PoseStrike(_star[i], _foot[i], i * 53f + _born[i] * 91f, _size[i], since - 0.02f, 0.05f, StarLife - 0.05f, age);
                else _star[i].Hide();
                if (_reach[i] >= 1f && !reduced && since >= 0f) ZackKitPaint.PoseBurst(_burst[i], _foot[i] + Vector3.up * 0.04f, Vector3.up, _born[i] + 0.02f, age, 0.55f + 0.25f * _size[i]);
                else _burst[i].Hide();
            }
        }

        // A small dark puff where an arc struck, on the oldest of the three clouds. A strike more than the three can hold takes the oldest.
        private void Scorch(float born, Vector3 foot, float scale)
        {
            int i = _nextPuff; _nextPuff = (_nextPuff + 1) % Puffs;
            var e = FxCloud.Emitter.Puff(foot + Vector3.up * 0.08f, Vector3.up * (0.75f * scale) + (foot - _last).normalized * 0.2f, born + 0.02f, 0.26f, 0.55f);
            e.Spread = 0.28f * scale; e.Widen = 0.22f * scale; e.Weave = 0.05f;
            e.Small = 0.09f * scale; e.Large = 0.22f * scale; e.Gone = 0.05f;
            e.PeakLow = 0.3f; e.PeakHigh = 0.5f; e.SharpLow = 1.0f; e.SharpHigh = 2.0f;
            _puffSet[i] = e; _puffAge[i] = born;
        }

        private void StepPuffs(float age, bool eyes)
        {
            for (int i = 0; i < Puffs; i++)
            {
                if (age - _puffAge[i] < 0.9f) { _puff[i].Piece.Frame(0f, 1f); _puff[i].Pose(age, _puffSet[i]); }
                else _puff[i].Piece.Frame(0f, 0f);
            }
        }
    }
}
