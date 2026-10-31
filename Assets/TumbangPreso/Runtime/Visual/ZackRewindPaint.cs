using System.Collections.Generic;
using UnityEngine;
using Part = TumbangPreso.Visual.ZackKitPaint.Part;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// BACKTRACK, PAINTED: what `ZackBacktrackLook` draws with its `Painted` switch on, in place of the fourteen line
    /// renderer streaks and the line spark bursts. The body pull, the lead hologram and the past moments the owner asked for
    /// stay `ZackBacktrackLook`'s own and are not touched.
    ///
    /// THE GAMEPLAY SHAPE THE PICTURE TELLS THE TRUTH ABOUT. The rule snaps him in one frame to where he was 3 s ago
    /// (`ZackRules.BacktrackSeconds`); the presentation pulls his drawn body back along the ROUTE HE REALLY RAN in 0.42 s
    /// (`ZackBacktrackLook.TravelSeconds`), corners and all, which is anything from a step to about fifteen metres. Nothing
    /// is hit, nothing is left on the court. So:
    ///   * THE TRAIL is b13d's silk body and core, laid along the route BEHIND the rewinding body, one leg for each straight
    ///     run between the route's real corners (Bank Shot's legs between walls, the same V at each corner). Every point is
    ///     aged by when the body passed it, so it swells and is eaten WHERE IT LIES: the trail is history and never turns.
    ///     It rides at his chest so it reads as his body's wake, not a mark on the road.
    ///   * THE LET-GO is z4's own pair at the spot he leaves: the strike on the road eaten outward, and his AFTERIMAGE (his
    ///     body baked as it was at the cast) with its cracks, eaten from the feet up within a quarter of a second.
    ///   * THE CATCH is z4's smaller landing strike under the old spot, and b13d's spark bursts at both ends and at each past
    ///     moment he folds into.
    /// Who sees what: the caster, from his own eyes, sees the trail stretch ahead of him back to where he was (it opens
    /// square to his eye and dissolves within 2.2 m of the lens, b13d's `_Near`), the strikes on the ground, and the
    /// afterimage only once he is well clear of it. A target and a bystander see all of it: a gold wake that runs the real
    /// route and dies on it in a third of a second, so the read is "he went back THAT way".
    ///
    ///   * THE ECHOES (owner, 2026-10-29: "for back track can you try adding a multiple ghost/afterimage effect too"): up to
    ///     five more of z4's afterimages, spaced evenly along the route at points he really passed through, each turned the
    ///     way he was running there. Each appears as the rewinding body passes its spot and is eaten from the feet up a
    ///     quarter of a second later, so they ripple along the path behind the pull. They wear the ONE body baked at the
    ///     cast (no new bake, no per-frame mesh work: a frame sets their transforms and property blocks), so all of them hold
    ///     the pose he had when he pressed, not the pose he had at that spot; the past moments are the ones in his real pose.
    ///     They go from gold, the oldest-run first, to a pale gold near white at the old spot, a third there.
    ///     COST CAP: at most `MaxEchoes` (5), each two draws (body and cracks) of one baked body; at most about four are on
    ///     show at once, since each lives 0.32 s and the pull passes them in 0.42 s.
    ///
    /// ⚠️ WHERE A REUSED PIECE DOES NOT FIT. z4's streak is a 2 m blade sized to a sidestep; laid over a 15 m rewind it would
    /// be stretched seven times, so it is not used here. The silk's own ageing does that job at any length. b13d's strands,
    /// circle head and embers belong to a thrown shoe and are left out.
    /// </summary>
    public sealed class ZackRewindPaint
    {
        private const int MaxLegs = 4, Bursts = 6;
        private const float Chest = 0.95f, LegLinger = 0.40f, CornerDegrees = 25f, MinLeg = 0.35f;
        private readonly Transform _root;
        private readonly Vector3[] _route;
        private readonly float[] _distance;
        private readonly float _length;
        private readonly Vector3 _from, _to;
        private readonly bool _calm;

        private sealed class Leg { public Part Body, Core; public float D0, D1, Entered = -1f, Left = -1f; public int Index; }
        private readonly List<Leg> _legs = new List<Leg>();
        private readonly Part _strike, _land, _ghost, _cracks;
        public const int MaxEchoes = 5;
        private const float EchoSpacing = 1.2f, EchoWhole = 0.06f, EchoGone = 0.32f, EchoOpacity = 0.32f;
        private static readonly Color PaleGold = new Color(1.00f, 0.95f, 0.78f);
        private Part[] _echoBody = new Part[0], _echoCracks = new Part[0];
        private float[] _echoAt = new float[0], _echoBorn = new float[0];
        private Vector3[] _echoFeet = new Vector3[0];
        /// <summary>How many echoes this rewind stood on its route (for the film's report).</summary>
        public int Echoes => _echoBody.Length;
        private readonly Part[] _bursts = new Part[Bursts];
        private readonly float[] _burstBorn = new float[Bursts];
        private readonly Vector3[] _burstAt = new Vector3[Bursts];
        private int _nextBurst;
        private bool _ghostShown, _landed;
        private float _landedAt = -1f;

        /// <summary>
        /// `route` runs from where he leaves (index 0) to where he lands, as `ZackBacktrackLook` sealed it. `model` is his
        /// body as posed at the cast, baked once for the afterimage (null leaves it out).
        /// </summary>
        public ZackRewindPaint(Transform parent, Vector3[] route, float[] distance, GameObject model, bool calm)
        {
            _route = route; _distance = distance; _calm = calm;
            _length = distance != null && distance.Length > 0 ? distance[distance.Length - 1] : 0f;
            _from = route != null && route.Length > 0 ? route[0] : Vector3.zero;
            _to = route != null && route.Length > 0 ? route[route.Length - 1] : _from;
            var go = new GameObject("ZackRewindPaint") { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _root = go.transform;
            SplitLegs();
            _strike = new Part("letgo-strike", _root, ZackKitPaint.FloorSquare, ZackKitPaint.Strike);
            _land = new Part("landing-strike", _root, ZackKitPaint.FloorSquare, ZackKitPaint.Strike);
            for (int i = 0; i < Bursts; i++) { _bursts[i] = new Part("burst-" + i, _root, ZackKitPaint.Burst, ZackKitPaint.Sparks); _burstBorn[i] = -1f; }
            // The afterimage stands where he leaves, baked in the space of its own holder there.
            var ghostAt = new GameObject("afterimage") { hideFlags = HideFlags.DontSave }.transform;
            ghostAt.SetParent(_root, false); ghostAt.SetPositionAndRotation(_from, Quaternion.identity);
            var mesh = ZackKitPaint.GhostMeshFor(model);
            _ghostShown = mesh != null && ZackKitPaint.Bake(model, ghostAt, mesh);
            _ghost = new Part("afterimage-body", ghostAt, _ghostShown ? mesh : null, ZackKitPaint.Ghost);
            _cracks = new Part("afterimage-cracks", ghostAt, _ghostShown ? mesh : null, ZackKitPaint.Cracks);
            if (_ghostShown) StandEchoes(mesh, model);
            Burst(_from + Vector3.up * 0.2f, 0f);
        }

        // One leg for each straight run between corners sharper than 25 degrees; at most four, the sharpest corners first.
        private void SplitLegs()
        {
            if (_route == null || _route.Length < 2 || _length < 0.05f) return;
            var corners = new List<(int i, float turn)>();
            for (int i = 1; i < _route.Length - 1; i++)
            {
                var a = _route[i] - _route[i - 1]; var b = _route[i + 1] - _route[i]; a.y = b.y = 0f;
                if (a.sqrMagnitude < 1e-4f || b.sqrMagnitude < 1e-4f) continue;
                float turn = Vector3.Angle(a, b);
                if (turn > CornerDegrees) corners.Add((i, turn));
            }
            corners.Sort((x, y) => y.turn.CompareTo(x.turn));
            var cuts = new List<float> { 0f, _length };
            foreach (var c in corners)
            {
                if (cuts.Count >= MaxLegs + 1) break;
                float d = _distance[c.i];
                bool clear = true; foreach (float other in cuts) if (Mathf.Abs(other - d) < MinLeg) { clear = false; break; }
                if (clear) cuts.Add(d);
            }
            cuts.Sort();
            for (int k = 0; k < cuts.Count - 1; k++)
            {
                var leg = new Leg { D0 = cuts[k], D1 = cuts[k + 1], Index = k };
                leg.Body = new Part("trail-body-" + k, _root, ZackKitPaint.Even, ZackKitPaint.Silk);
                leg.Core = new Part("trail-core-" + k, _root, ZackKitPaint.Even, ZackKitPaint.Silk);
                _legs.Add(leg);
            }
        }

        // The echoes: evenly along the route between an eighth and seven eighths of it, never closer than 1.2 m to each other,
        // each turned from the way he faced at the cast to the way he was running at its spot.
        private void StandEchoes(Mesh mesh, GameObject model)
        {
            if (_length < EchoSpacing * 2f) return;
            int count = Mathf.Min(MaxEchoes, Mathf.FloorToInt(_length * 0.76f / EchoSpacing) + 1);
            if (count < 1) return;
            var cast = model.transform.forward; cast.y = 0f;
            if (cast.sqrMagnitude < 1e-4f) cast = Vector3.forward;
            _echoBody = new Part[count]; _echoCracks = new Part[count];
            _echoAt = new float[count]; _echoBorn = new float[count]; _echoFeet = new Vector3[count];
            for (int k = 0; k < count; k++)
            {
                float share = count == 1 ? 0.5f : Mathf.Lerp(0.12f, 0.88f, k / (count - 1f)), d = share * _length;
                // He ran toward where he leaves, so his heading at a spot points back along the route toward its start.
                var heading = At(d - 0.3f) - At(d + 0.3f); heading.y = 0f;
                if (heading.sqrMagnitude < 1e-4f) heading = cast;
                var holder = new GameObject("echo-" + k) { hideFlags = HideFlags.DontSave }.transform;
                holder.SetParent(_root, false);
                _echoFeet[k] = At(d);
                holder.SetPositionAndRotation(_echoFeet[k], Quaternion.Euler(0f, Vector3.SignedAngle(cast, heading, Vector3.up), 0f));
                _echoBody[k] = new Part("echo-body-" + k, holder, mesh, ZackKitPaint.Ghost);
                _echoCracks[k] = new Part("echo-cracks-" + k, holder, mesh, ZackKitPaint.Cracks);
                // Gold where the oldest of the run is, going to a pale gold near white at the old spot. (Broad white reads
                // as overexposed: the body only gets near it; the cracks carry the white.)
                float t = count == 1 ? 0.5f : k / (count - 1f);
                var body = Color.Lerp(ZackKitPaint.Gold, PaleGold, t);
                _echoBody[k].Colour("_Deep", Color.Lerp(ZackKitPaint.Amber, ZackKitPaint.Gold, t));
                _echoBody[k].Colour("_Body", body); _echoBody[k].Colour("_Hot", body);
                _echoBody[k].Colour("_HaloColor", Color.Lerp(ZackKitPaint.AmberVeil, body, t));
                _echoAt[k] = d; _echoBorn[k] = -1f;
            }
        }

        private void PoseEchoes(float age, float d, Vector3 eye, bool eyes)
        {
            for (int k = 0; k < _echoBody.Length; k++)
            {
                if (_echoBorn[k] < 0f && d >= _echoAt[k]) _echoBorn[k] = age;
                float since = _echoBorn[k] >= 0f ? age - _echoBorn[k] : -1f;
                if (since < 0f || since >= EchoGone) { _echoBody[k].Hide(); _echoCracks[k].Hide(); continue; }
                // Grows in over a few frames (nothing pops), whole for a moment, then eaten from the feet up.
                float enter = FxStudyKit.EaseOut(FxStudyKit.Run(since, 0f, 0.03f));
                float eaten = FxStudyKit.Run(since, EchoWhole, EchoGone);
                float near = eyes ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.1f, 1.8f, Vector3.Distance(_echoFeet[k] + Vector3.up * 1.2f, eye))) : 1f;
                _echoCracks[k].Tile(0.9f, 0.9f, 0.13f + 0.27f * ((Mathf.FloorToInt(age * ZackKitPaint.Strikes) + k) % 4), 0.41f);
                _echoBody[k].Frame(age, EchoOpacity * enter * near, eaten);
                _echoCracks[k].Frame(age, 0.95f * enter * near, eaten);
            }
        }

        /// <summary>The route at `d` metres from where he leaves.</summary>
        private Vector3 At(float d)
        {
            if (_route == null || _route.Length == 0) return Vector3.zero;
            if (_route.Length == 1 || d <= 0f) return _route[0];
            int i = 1;
            while (i < _route.Length - 1 && _distance[i] < d) i++;
            float span = _distance[i] - _distance[i - 1], u = span > 1e-5f ? Mathf.Clamp01((d - _distance[i - 1]) / span) : 1f;
            return Vector3.Lerp(_route[i - 1], _route[i], u);
        }

        /// <summary>A burst of sparks thrown up at `at`, from `now` on the rewind's clock.</summary>
        public void Burst(Vector3 at, float now)
        {
            int i = _nextBurst++ % Bursts;
            _burstAt[i] = at; _burstBorn[i] = now;
        }

        /// <summary>
        /// One frame. `age` is the rewind's own clock (0 at the cast), `travelled` the share of the route the drawn body has
        /// rewound, `catchAt` when it reaches the old spot, `eye` this screen's camera, `eyes` whether that camera is his.
        /// </summary>
        public void Pose(float age, float travelled, float revealEnd, float catchAt, Vector3 eye, bool eyes)
        {
            float d = Mathf.Clamp01(travelled) * _length;
            float open = FxStudyKit.EaseOut(FxStudyKit.Run(age, 0f, 0.10f));
            // THE TRAIL, leg by leg, from where the body entered the leg to where it is (or to the corner it left by).
            foreach (var leg in _legs)
            {
                if (leg.Entered < 0f && d > leg.D0 + 1e-3f) leg.Entered = age;
                if (leg.Left < 0f && (d >= leg.D1 - 1e-3f && leg.D1 < _length - 1e-3f || age >= catchAt && leg.Entered >= 0f)) leg.Left = Mathf.Min(age, catchAt);
                if (leg.Entered < 0f) { leg.Body.Hide(); leg.Core.Hide(); continue; }
                float since = leg.Left >= 0f ? age - leg.Left : 0f;
                if (leg.Left >= 0f && since >= LegLinger) { leg.Body.Hide(); leg.Core.Hide(); continue; }
                float head = Mathf.Clamp(d, leg.D0, leg.D1);
                float length = head - leg.D0;
                if (length < 0.02f) { leg.Body.Hide(); leg.Core.Hide(); continue; }
                Vector3 tail = At(leg.D0) + Vector3.up * Chest, tip = At(head) + Vector3.up * Chest, middle = At((leg.D0 + head) * 0.5f) + Vector3.up * Chest;
                leg.Body.Lay(tail, tip, middle); leg.Core.Lay(tail, tip, middle);
                // Its points' ages are their times: the body's own speed over this leg, so when it passed each point comes back.
                float lasted = Mathf.Max(1e-3f, (leg.Left >= 0f ? leg.Left : age) - leg.Entered);
                float speed = Mathf.Max(1f, length / lasted);
                ZackKitPaint.DressLeg(leg.Body, leg.Core, leg.Index, length, length / ZackKitPaint.Long, speed, since, open, age + 1.4f);
            }

            // THE LET-GO: the strike under where he leaves, and his afterimage there, whole for a moment then eaten from the feet.
            ZackKitPaint.PoseStrike(_strike, _from, 25f, 1.7f, age - revealEnd, 0.08f, 0.30f, age);
            float cut = age - revealEnd;
            float ghostEaten = FxStudyKit.Run(cut, 0.04f, 0.26f);
            float near = eyes ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.1f, 1.8f, Vector3.Distance(_from + Vector3.up * 1.2f, eye))) : 1f;
            bool ghost = _ghostShown && cut >= 0f;
            _cracks.Tile(0.9f, 0.9f, 0.13f + 0.27f * (Mathf.FloorToInt(age * ZackKitPaint.Strikes) % 4), 0.41f);
            _ghost.Frame(age, ghost ? 0.34f * near : 0f, ghostEaten);
            _cracks.Frame(age, ghost ? 0.95f * near : 0f, ghostEaten);

            // THE ECHOES, rippling along the route behind the pull.
            PoseEchoes(age, d, eye, eyes);

            // THE CATCH.
            if (!_landed && age >= catchAt)
            {
                _landed = true; _landedAt = catchAt;
                Burst(_to + Vector3.up * 0.12f, catchAt);
                if (!_calm) Burst(_to + Vector3.up * 1.1f, catchAt + 0.02f);
            }
            ZackKitPaint.PoseStrike(_land, _to, 200f, 1.3f, _landedAt >= 0f ? age - _landedAt + 0.04f : -1f, 0.07f, 0.26f, age);
            for (int i = 0; i < Bursts; i++) ZackKitPaint.PoseBurst(_bursts[i], _burstAt[i], Vector3.up, _burstBorn[i], age);
        }

        /// <summary>How long after the cast something may still be on show: the last leg's points live 0.4 s past the catch.</summary>
        public static float After(float catchAt) => catchAt + LegLinger;

        public void Dispose()
        {
            if (_root == null) return;
            if (Application.isPlaying) Object.Destroy(_root.gameObject); else Object.DestroyImmediate(_root.gameObject);
        }
    }
}
