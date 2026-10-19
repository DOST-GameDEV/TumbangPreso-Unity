using System;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TumbangPreso.Visual
{
    public sealed partial class FxStudyZackBank
    {
        /// <summary>
        /// BANK SHOT IN A MATCH: the approved b13d flight (silk trail, circle head; owner 2026-10-18, "zacks bankshot is good,
        /// lock it in") laid on a REAL thrown shoe instead of the bench's fixed route. Every piece is dressed by the bench's
        /// own `Dress*` methods, so what is drawn is the approved look; this class only says WHERE, from what the shoe did.
        ///
        /// ⚠️ IT IS FED, IT ASKS NOTHING. `Observe` is handed the shoe's position, velocity, how many powered banks it still
        /// carries and whether it is in flight, which every peer already has (the host simulates them and every client is
        /// sent them with the shoe's state). `ZackBankFlight` feeds it in a match and a film can feed it a staged flight.
        ///
        /// ⚠️ A TRAIL IS HISTORY. The owner, of the bench's b8: "the trail just turns around". So nothing here is ever
        /// re-pointed when the shoe turns. It keeps where the shoe has been (a short record of its own), and:
        ///   * a LEG runs from where the shoe left a hand or a wall to the shoe. When the shoe banks, that leg is closed
        ///     where it met the wall and stays there, its points going on ageing until they are eaten (the bench's V at
        ///     the wall); the next leg grows out of the wall behind the shoe;
        ///   * the real flight falls and curves, where the bench's route is straight: each leg is bowed along the path the
        ///     shoe actually took (`ZackTrail.shader`, `_Bend`), so every point is drawn where the shoe was at that age;
        ///   * a STRAND lies along the path behind the point it let go of, bowed the same way, and stays there;
        ///   * when the shoe lands, is caught, or pays its last bank, nothing new is born and everything already drawn
        ///     dissipates where it lies (the bench's own deaths); nothing blinks off but the head, as on the bench.
        ///
        /// ⚠️ THE PICTURE TELLS THE TRUTH. The star is burnt into a wall only where a wall is found behind the bank point
        /// (a bank off the court's open edge gets the sparks alone), and the bank point is where the shoe's path before
        /// and after meet, not wherever the shoe happened to be on the frame it was noticed.
        ///
        /// Made once per shoe and kept: its pieces wear the bench's shared meshes and materials, and a throw that is not
        /// the first makes nothing. The root must stand at the world's origin (the pieces are placed in its space).
        /// </summary>
        public sealed class Live : IDisposable
        {
            // How long after the flight ends something can still be on show: a leg's last point lives 0.34 s, a strand let
            // go at the end 0.16 s more, the spines are eaten in 0.22 s, the star in 0.7 s.
            private const float Linger = 0.75f;
            // A turn sharper than this between two looks at the shoe is a bank (25 degrees). A spun shoe turns about one
            // degree a frame at most.
            private const float TurnCos = 0.906f;
            // The spines are Quick Circuit's bolt and cannot bend: they ride the last 0.22 s of the path, a chord short
            // enough to stay inside the body (it bows about 0.12 m off a falling path, the body is 0.75 m across).
            private const float SpineSpan = 0.22f;
            private const int Keep = 1024, LegSlots = 4;

            private readonly Transform _root;
            private readonly PhysicsScene _physics;

            // Where the shoe has been: time, place, velocity, metres flown. A ring, oldest at `_first`.
            private readonly float[] _t = new float[Keep], _s = new float[Keep];
            private readonly Vector3[] _p = new Vector3[Keep], _v = new Vector3[Keep];
            private int _first, _count;

            private sealed class Leg
            {
                public Transform At, SpineAt;
                public Part Body, Core;
                public readonly Part[,] Spine = new Part[MaxCharges, Shapes];
                public bool Used, Closed, First;
                public int Index;
                public float T0, TEnd, S0;
                public Vector3 P0, PEnd;
            }
            private readonly Leg[] _legs = new Leg[LegSlots];
            private Leg _now;
            private readonly Part[] _strand = new Part[Strands], _short = new Part[2], _heads = new Part[Cards], _ember = new Part[Embers];
            private readonly Part[] _scorch = new Part[MaxCharges], _sparks = new Part[MaxCharges];
            private readonly Vector3[] _markAt = new Vector3[MaxCharges], _markNormal = new Vector3[MaxCharges];
            private readonly float[] _markFlown = new float[MaxCharges];
            private readonly bool[] _markWall = new bool[MaxCharges];
            private int _marks, _charges, _paid;
            private bool _active, _live;
            private float _start, _spent = float.PositiveInfinity;

            public bool Intact => _root != null;

            /// <summary>`parent` null stands it at the root of `scene`; `physics` is where walls are looked for.</summary>
            public Live(Transform parent, Scene scene, PhysicsScene physics)
            {
                Make();
                _physics = physics;
                var go = new GameObject("ZackBankFlight") { hideFlags = HideFlags.DontSave };
                if (parent != null) go.transform.SetParent(parent, false);
                else if (scene.IsValid() && scene.isLoaded && go.scene != scene) SceneManager.MoveGameObjectToScene(go, scene);
                _root = go.transform;
                for (int l = 0; l < LegSlots; l++)
                {
                    var leg = new Leg();
                    leg.At = new GameObject("leg-" + l) { hideFlags = HideFlags.DontSave }.transform; leg.At.SetParent(_root, false);
                    leg.SpineAt = new GameObject("spines-" + l) { hideFlags = HideFlags.DontSave }.transform; leg.SpineAt.SetParent(_root, false);
                    leg.Body = new Part("trail-body", leg.At, _even, _trailMat);
                    leg.Core = new Part("trail-core", leg.At, _even, _trailMat);
                    for (int c = 0; c < MaxCharges; c++)
                        for (int s = 0; s < Shapes; s++) leg.Spine[c, s] = new Part("spine-" + c + "-" + s, leg.SpineAt, _spines[s], _boltMat);
                    _legs[l] = leg;
                }
                for (int i = 0; i < Strands; i++) _strand[i] = new Part("strand-" + i, _root, Table[i].Flat ? _strandFlat : _strandUp, _trailMat);
                _short[0] = new Part("bulb", _root, _bulb, _trailMat); _short[1] = new Part("bulb-flat", _root, _bulbFlat, _trailMat);
                for (int i = 0; i < Cards; i++) _heads[i] = new Part("head-card-" + i, _root, _card, _rimMat);
                for (int i = 0; i < Embers; i++) _ember[i] = new Part("embers-" + i, _root, _sparkMesh, _sparkMat);
                for (int c = 0; c < MaxCharges; c++)
                {
                    _scorch[c] = new Part("scorch-" + c, _root, _square, _scorchMat);
                    _sparks[c] = new Part("sparks-" + c, _root, _sparkMesh, _sparkMat);
                }
                foreach (var renderer in _root.GetComponentsInChildren<MeshRenderer>(true)) VfxRenderTag.Attach(renderer.gameObject);
                Hide();
            }

            // ------------------------------------------------------------ what the shoe did

            /// <summary>One look at the shoe. `poweredLeft` is the banks it still carries (Overclock 2, a load 1, spent 0).</summary>
            public void Observe(float time, Vector3 at, Vector3 velocity, int poweredLeft, bool inFlight)
            {
                if (!Intact) return;
                poweredLeft = Mathf.Clamp(poweredLeft, 0, MaxCharges);
                if (!_active)
                {
                    if (inFlight && poweredLeft > 0) Begin(time, at, velocity, poweredLeft);
                    return;
                }
                int last = Idx(_count - 1);
                float lastT = _t[last]; Vector3 lastP = _p[last], lastV = _v[last];
                if (!inFlight) { End(lastT); return; }
                float dt = time - lastT;
                if (dt <= 1e-5f) return;
                bool moved = (at - lastP).sqrMagnitude > 1e-8f;
                int reported = Mathf.Clamp(_charges - poweredLeft, 0, _charges);
                bool pays = reported > _paid;
                // Between two packets on a client nothing new is known.
                if (!moved && !pays) return;
                // A correction or a reset is not a flight: it ends where the shoe was last truly seen flying.
                float plausible = Mathf.Max(2f, Mathf.Max(velocity.magnitude, lastV.magnitude) * dt * 3f + 0.5f);
                if ((at - lastP).magnitude > plausible) { End(lastT); return; }
                Vector3 hOld = Flat(lastV), hNew = Flat(velocity);
                bool turned = moved && hOld.sqrMagnitude > 1f && hNew.sqrMagnitude > 1f && Vector3.Dot(hOld.normalized, hNew.normalized) < TurnCos;
                if (turned || pays)
                {
                    float tb = lastT; Vector3 b = lastP;
                    if (turned)
                    {
                        BankPoint(lastT, lastP, lastV, time, at, velocity, out tb, out b);
                        if (tb > lastT + 1e-5f && tb < time - 1e-5f) Push(tb, b, velocity);
                    }
                    if (moved) Push(time, at, velocity);
                    if (pays)
                    {
                        // The wall's normal is the way the bank turned the shoe.
                        Vector3 n = hNew - hOld;
                        n = n.sqrMagnitude > 0.25f ? n.normalized : hOld.sqrMagnitude > 1e-4f ? -hOld.normalized : Vector3.zero;
                        for (; _paid < reported; _paid++) Mark(_paid, b, n, tb - _start);
                    }
                    if (_paid >= _charges) { End(tb); return; }
                    Split(tb, b);
                    return;
                }
                // A sharp change that no fall explains (the lid on the sky): the leg ends at the last point before it.
                bool kinked = Mathf.Abs(velocity.y - lastV.y + Balance.Gravity * dt) > 4f;
                Push(time, at, velocity);
                if (kinked) Split(lastT, lastP);
            }

            private void Begin(float time, Vector3 at, Vector3 velocity, int poweredLeft)
            {
                _first = 0; _count = 0;
                Push(time, at, velocity);
                foreach (var leg in _legs) leg.Used = false;
                _start = time; _spent = float.PositiveInfinity;
                _charges = poweredLeft; _paid = 0; _marks = 0;
                _active = _live = true;
                // One spine runs down the trail's middle; two run a hand apart, one high and one low (the bench's).
                foreach (var leg in _legs)
                    for (int c = 0; c < MaxCharges; c++)
                        for (int s = 0; s < Shapes; s++)
                            leg.Spine[c, s].At.localPosition = Vector3.up * (_charges > 1 ? (c == 0 ? SpineApart : -SpineApart) : 0f);
                _now = NewLeg(time, at, 0, true);
            }

            private void End(float tEnd)
            {
                if (_now != null && !_now.Closed) { _now.Closed = true; _now.TEnd = tEnd; _now.PEnd = PosAt(tEnd); }
                _now = null;
                _active = false;
                _spent = tEnd - _start;
            }

            private void Split(float t, Vector3 at)
            {
                if (_now != null && !_now.Closed) { _now.Closed = true; _now.TEnd = t; _now.PEnd = at; }
                _now = NewLeg(t, at, _paid, false);
            }

            private Leg NewLeg(float t0, Vector3 p0, int index, bool first)
            {
                Leg pick = null;
                foreach (var leg in _legs) if (!leg.Used) { pick = leg; break; }
                if (pick == null) foreach (var leg in _legs) if (leg != _now && (pick == null || leg.T0 < pick.T0)) pick = leg;
                pick.Used = true; pick.Closed = false; pick.First = first; pick.Index = index;
                pick.T0 = t0; pick.P0 = p0; pick.S0 = DistAt(t0);
                return pick;
            }

            // A bank: the star and sparks. The star is burnt into a wall, so it is drawn only where one stands behind the bank.
            private void Mark(int c, Vector3 at, Vector3 normal, float flown)
            {
                if (c < 0 || c >= MaxCharges) return;
                _markFlown[c] = flown; _markAt[c] = at; _markNormal[c] = normal; _markWall[c] = false;
                if (normal.sqrMagnitude > 0.5f && _physics.IsValid()
                    && _physics.Raycast(at + normal * 0.4f, -normal, out var hit, 1.0f, ~0, QueryTriggerInteraction.Ignore)
                    && Vector3.Dot(hit.normal, normal) > 0.5f)
                { _markAt[c] = hit.point; _markNormal[c] = hit.normal; _markWall[c] = true; }
                _marks = Mathf.Max(_marks, c + 1);
            }

            /// <summary>
            /// Where and when the shoe met the wall, between a look before the bank and a look after it: the time `a` after
            /// the first look at which, going on as it was and then coming away at its new speed, it would be where it was
            /// seen the second time. (Taking either look as the bank point puts the corner up to a frame's flight off the wall.)
            /// </summary>
            private static void BankPoint(float t0, Vector3 p0, Vector3 v0, float t1, Vector3 p1, Vector3 v1, out float tb, out Vector3 b)
            {
                Vector3 h0 = Flat(v0), h1 = Flat(v1), r = Flat(p1 - p0);
                float span = t1 - t0, s1 = h1.magnitude;
                // |r - h0 a| = s1 (span - a), squared: a quadratic in a.
                float qa = h0.sqrMagnitude - s1 * s1, qb = -2f * Vector3.Dot(r, h0) + 2f * s1 * s1 * span, qc = r.sqrMagnitude - s1 * s1 * span * span;
                float best = -1f, bestFit = -2f;
                void Try(float a)
                {
                    if (!(a >= 0f && a <= span)) return;
                    Vector3 after = r - h0 * a;
                    float fit = after.sqrMagnitude > 1e-8f && s1 > 1e-3f ? Vector3.Dot(after.normalized, h1 / s1) : 1f;
                    if (fit > bestFit) { bestFit = fit; best = a; }
                }
                if (Mathf.Abs(qa) > 1e-4f)
                {
                    float disc = qb * qb - 4f * qa * qc;
                    if (disc >= 0f) { float root = Mathf.Sqrt(disc); Try((-qb + root) / (2f * qa)); Try((-qb - root) / (2f * qa)); }
                }
                else if (Mathf.Abs(qb) > 1e-6f) Try(-qc / qb);
                if (best < 0f)
                {
                    // Two lines on the ground, crossed.
                    float cross = h0.x * h1.z - h0.z * h1.x;
                    best = Mathf.Abs(cross) > 1e-3f ? Mathf.Clamp((r.x * h1.z - r.z * h1.x) / cross, 0f, span) : 0f;
                }
                tb = t0 + best;
                b = p0 + h0 * best + Vector3.up * (v0.y * best - 0.5f * Balance.Gravity * best * best);
            }

            // ------------------------------------------------------------ the record

            private int Idx(int k) => (_first + k) % Keep;

            private void Push(float t, Vector3 p, Vector3 v)
            {
                float s = 0f;
                if (_count > 0) { int last = Idx(_count - 1); s = _s[last] + Vector3.Distance(_p[last], p); }
                if (_count == Keep) { _first = (_first + 1) % Keep; _count--; }
                int i = Idx(_count);
                _t[i] = t; _p[i] = p; _v[i] = v; _s[i] = s;
                _count++;
            }

            // Where the shoe was at `t`, its velocity and metres flown. Before the first look it is run back along its first
            // velocity (the bench's strands also reach back past his hand); after the last it is where it was last seen.
            private void Sample(float t, out Vector3 p, out Vector3 v, out float s)
            {
                int first = Idx(0), last = Idx(_count - 1);
                if (t <= _t[first]) { v = _v[first]; float back = t - _t[first]; p = _p[first] + v * back; s = _s[first] + v.magnitude * back; return; }
                if (t >= _t[last]) { p = _p[last]; v = _v[last]; s = _s[last]; return; }
                int lo = 0, hi = _count - 1;
                while (hi - lo > 1) { int mid = (lo + hi) / 2; if (_t[Idx(mid)] <= t) lo = mid; else hi = mid; }
                int a = Idx(lo), b = Idx(hi);
                float f = Mathf.InverseLerp(_t[a], _t[b], t);
                p = Vector3.Lerp(_p[a], _p[b], f); v = Vector3.Lerp(_v[a], _v[b], f); s = Mathf.Lerp(_s[a], _s[b], f);
            }
            private Vector3 PosAt(float t) { Sample(t, out var p, out _, out _); return p; }
            private float DistAt(float t) { Sample(t, out _, out _, out var s); return s; }
            // How far the path between `t0` and `t1` bows off the straight line between its ends, times four (`_Bend`).
            private Vector3 Bow(float t0, float t1, Vector3 p0, Vector3 p1) => (PosAt((t0 + t1) * 0.5f) - (p0 + p1) * 0.5f) * 4f;
            private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
            private static Vector3 Direction(Vector3 v, Vector3 fallback) => v.sqrMagnitude > 1e-6f ? v.normalized : fallback;

            private Leg LegAt(float t)
            {
                Leg found = null;
                foreach (var leg in _legs)
                    if (leg.Used && leg.T0 <= t + 1e-5f && (!leg.Closed || t < leg.TEnd) && (found == null || leg.T0 > found.T0)) found = leg;
                return found;
            }

            // ------------------------------------------------------------ drawing it

            /// <summary>Poses everything for `time`, seen from `eye`. False once nothing is left on show.</summary>
            public bool Pose(float time, Vector3 eye)
            {
                if (!_live || !Intact || _count == 0) return false;
                float flown = time - _start, age = flown + Throw;
                if (!_active && flown - _spent > Linger) { Hide(); return false; }
                int lastI = Idx(_count - 1);
                float tLast = _t[lastI]; Vector3 pLast = _p[lastI], vLast = _v[lastI];
                Vector3 going = Direction(vLast, Vector3.forward);
                float open = FxStudyKit.EaseOut(FxStudyKit.Run(flown, 0f, 0.10f));
                int strike = Mathf.FloorToInt(age * Strikes);
                float spentAt = _active ? float.PositiveInfinity : _spent;

                // THE LEGS, each from where it began to the shoe or to the wall it ended at, bowed along the real flight.
                foreach (var leg in _legs)
                {
                    if (!leg.Used) continue;
                    float since = leg.Closed ? time - leg.TEnd : 0f;
                    if (leg.Closed && since >= 0.40f) { HideLeg(leg, age); leg.Used = false; continue; }
                    if (leg.Index >= _charges) { HideLeg(leg, age); continue; }
                    float tHead = leg.Closed ? leg.TEnd : tLast;
                    Vector3 head = leg.Closed ? leg.PEnd : pLast;
                    Sample(tHead, out _, out Vector3 vHead, out _);
                    Vector3 chord = head - leg.P0;
                    float length = chord.magnitude, lasted = tHead - leg.T0;
                    Vector3 dir = length > 1e-3f ? chord / length : Direction(vHead, going);
                    float stretch = Mathf.Max(0.04f, length / Long);
                    float snap = FxStudyKit.Pop(FxStudyKit.Run(time - leg.T0, 0f, 0.10f), 0.22f), across = Mathf.Lerp(0.35f, 1f, snap);
                    leg.At.localPosition = head + Vector3.up * 0.04f;
                    leg.At.localRotation = Quaternion.LookRotation(dir);
                    leg.At.localScale = new Vector3(across, across, stretch);
                    // Its points' ages are their times: the chord's speed, so the shoe's own time at each point comes back.
                    float speed = lasted > 1e-3f && length > 1e-3f ? length / lasted : Mathf.Max(1f, vHead.magnitude);
                    Vector3 bow = Bow(leg.T0, tHead, leg.P0, head);
                    var bend = new Vector4(bow.x, bow.y, bow.z, Long * stretch);
                    leg.Body.Vec("_Bend", bend); leg.Core.Vec("_Bend", bend);
                    DressLeg(leg.Body, leg.Core, leg.Index, length, stretch, speed, since, open, age);

                    // Its spines, on the last short stretch of the path (a bolt cannot bow); a closed leg's stay where they
                    // were and let go of their tails as they are eaten, as the bench's do at the wall.
                    Vector3 tail = PosAt(Mathf.Max(leg.T0, tHead - SpineSpan)), back = head - tail;
                    float spineLength = back.magnitude, spineStretch = Mathf.Max(0.04f, spineLength / Long);
                    leg.SpineAt.localPosition = head + Vector3.up * 0.04f;
                    leg.SpineAt.localRotation = Quaternion.LookRotation(spineLength > 1e-3f ? back / spineLength : dir);
                    leg.SpineAt.localScale = new Vector3(across, across, spineStretch);
                    float edgeOn = EdgeOn(leg.SpineAt, eye);
                    float tailU = leg.Closed ? FxStudyKit.Ease(FxStudyKit.Run(since, 0f, 0.20f)) * 1.1f - 0.1f : -0.1f;
                    for (int c = 0; c < MaxCharges; c++)
                    {
                        int pick = (strike + c) % Shapes;
                        for (int s = 0; s < Shapes; s++)
                            DressSpine(leg.Spine[c, s], tailU, 1.1f, spineStretch, strike, c, leg.Index, c >= leg.Index && c < _charges && s == pick ? edgeOn : 0f,
                                       leg.Closed ? FxStudyKit.Run(since, 0f, 0.22f) : 0.12f, age);
                    }
                }

                // THE STRANDS, on the bench's beat. Each lies back along the path behind the point it rides or let go at.
                for (int i = 0; i < Strands; i++)
                {
                    var strand = _strand[i];
                    float since0 = StrandBeat(i, flown, out float bornAt, out float life);
                    bool alive = since0 >= 0f && bornAt < spentAt && life >= 0f && life < 1f;
                    var bornLeg = alive ? LegAt(_start + bornAt) : null;
                    if (bornLeg == null || bornLeg.Index >= _charges) { strand.Frame(age, 0f); continue; }
                    float held = StrandHeld;
                    float legEnd = bornLeg.Closed ? bornLeg.TEnd - _start : float.PositiveInfinity;
                    float letGo = Mathf.Min(bornAt + held * Cycle, legEnd);
                    bool riding = flown < letGo;
                    float peeled = riding ? 0f : Mathf.Clamp01((flown - letGo) / ((1f - held) * Cycle));
                    if (!riding && flown - letGo >= (1f - held) * Cycle) { strand.Frame(age, 0f); continue; }
                    float until = Mathf.Min(_start + (riding ? flown : letGo - 0.001f), tLast);
                    Sample(until, out Vector3 at, out Vector3 velocity, out float metres);
                    float reach = bornLeg.First ? 1e6f : Mathf.Max(0.02f, metres - bornLeg.S0);
                    StrandShape(i, life, peeled, reach, out float length, out float thick);
                    float from = until - length / Mathf.Max(1f, velocity.magnitude);
                    if (!bornLeg.First) from = Mathf.Max(from, bornLeg.T0);
                    Vector3 tail = PosAt(from), toTail = tail - at;
                    Vector3 dir = toTail.sqrMagnitude > 1e-6f ? -toTail.normalized : Direction(velocity, going);
                    Vector3 bow = Bow(until, from, at, tail);
                    // (u runs from the shoe to the strand's free tail, down its -z: its length along +z is negative.)
                    strand.Vec("_Bend", new Vector4(bow.x, bow.y, bow.z, -length));
                    DressStrand(strand, i, at + Vector3.up * (0.04f + Table[i].High * 1.8f), dir, length, thick, peeled, life, age);
                }

                // THE SHORT TRAIL AND THE HEAD ride the shoe while it is loaded; the head never shows on a leg not yet flown.
                Vector3 lead = pLast + Vector3.up * 0.04f;
                float onThisLeg = _now == null || _now.First ? 1e6f : Mathf.Max(0f, _s[lastI] - _now.S0);
                for (int k = 0; k < 2; k++) DressBulb(_short[k], k, lead, going, onThisLeg, _active ? 0.95f * open : 0f, age);
                int frame = Mathf.FloorToInt(age * 60f);
                Vector3 toEye = (lead - eye).normalized;
                for (int i = 0; i < Cards; i++) DressCard(_heads[i], i, lead, toEye, -going, frame, _active, open, age);

                // THE EMBERS, a burst every 0.12 s left where the shoe was.
                for (int i = 0; i < Embers; i++)
                {
                    float born = EmberBorn(flown, i);
                    Sample(Mathf.Min(_start + born, tLast), out Vector3 bornAt, out Vector3 bornVelocity, out _);
                    DressEmber(_ember[i], bornAt, Direction(bornVelocity, going), Throw + born, born >= 0f && born < spentAt, age);
                }

                // THE BANKS: the star in the wall (where there is one) and the sparks thrown off it.
                for (int c = 0; c < MaxCharges; c++)
                {
                    bool made = c < _marks;
                    float since = made ? flown - _markFlown[c] : -1f;
                    Vector3 normal = made && _markNormal[c].sqrMagnitude > 0.5f ? _markNormal[c] : Vector3.forward;
                    DressBank(_scorch[c], _sparks[c], c, made ? _markAt[c] : pLast, normal, since, Throw + (made ? _markFlown[c] : 0f), made && since >= 0f, age);
                    if (!made || !_markWall[c]) _scorch[c].Frame(age, 0f);
                }
                return true;
            }

            private static void HideLeg(Leg leg, float age)
            {
                leg.Body.Frame(age, 0f); leg.Core.Frame(age, 0f);
                foreach (var spine in leg.Spine) spine.Frame(age, 0f);
            }

            /// <summary>Everything off at once, and nothing in flight (the shoe left play, or this is being put away).</summary>
            public void Hide()
            {
                _active = _live = false; _now = null;
                if (!Intact) return;
                foreach (var leg in _legs) { if (leg == null) continue; HideLeg(leg, 0f); leg.Used = false; }
                foreach (var part in _strand) part?.Frame(0f, 0f);
                foreach (var part in _short) part?.Frame(0f, 0f);
                foreach (var part in _heads) part?.Frame(0f, 0f);
                foreach (var part in _ember) part?.Frame(0f, 0f);
                foreach (var part in _scorch) part?.Frame(0f, 0f);
                foreach (var part in _sparks) part?.Frame(0f, 0f);
            }

            public void Dispose()
            {
                if (_root == null) return;
                if (Application.isPlaying) Object.Destroy(_root.gameObject); else Object.DestroyImmediate(_root.gameObject);
            }
        }
    }
}
