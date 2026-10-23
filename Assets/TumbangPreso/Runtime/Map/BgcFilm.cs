using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.Map
{
    /// <summary>
    /// BGC (Kanto): CITY ADRENALINE. A hard, bright late morning in the grid. The streets in a time-lapse, the four
    /// arriving every way the city moves (off the back of a jeepney, skidding in on a bike, sprinting across against
    /// a braking taxi), the tower's billboard putting up the match card and naming the taya, and a traffic light going
    /// green for go. The score is a driving drum line that doubles under the rush and drops for the billboard.
    ///
    /// THE BEATS (seconds, full film / short film):
    ///   RUSH    0.0 / none   The traffic at six times its speed, four cuts, the light cycling fast: high over the
    ///                         north-east crossing (0.0), low at the north kerb as it whips past (0.8), down the east
    ///                         avenue from a tower (1.6), gliding in over the park as it slows to real time (2.4).
    ///                         BGC stamps at 2.5.
    ///   JEEP    3.2 / none   A jeepney brakes hard at the stop by the park; two jump off its back step (3.75, 4.0)
    ///                         and land running, fists up.
    ///   BIKE    4.8 / none   One shoots down the west road on a bike, skids it sideways to a stop, hops off, the
    ///                         bike clattering down, and points at the park.
    ///   FOOT    5.9 / none   The last sprints across the south road; a taxi stands on its brakes a metre short (6.3,
    ///                         the horn); they spin, a cheeky salute, and run on.
    ///   GATHER  7.0 / none   High over the park: the four run in through the gates to their marks.
    ///   BOARD   8.4 / 1.6    Low behind the four, all looking up at the north tower's billboard (8.4); then the
    ///                         board from the north kerb (8.9): the three attackers' cards slam in (8.95, 9.15,
    ///                         9.35), VS (9.6), a card spinning on "?"; it flips to the taya (9.8): TAYA. The taya
    ///                         struck, close (10.15); the three turned round on them, laughing (10.6).
    ///   GO      11.2 / 3.2   The light: amber (11.2), GREEN (11.7), go. Side on, the three drop into a sprint
    ///                         crouch, the taya squares up (12.0).
    ///   CLOSE   12.8 / 4.9   Each attacker's own screen ends on themselves; the taya's and a watcher's hold the taya.
    ///
    /// The traffic is the map's own (`KantoTraffic`), held still by the arrival's hold: this film poses each vehicle
    /// along its lane (`LaneFrame`) itself, and `End` puts every body back where it stood. The signal's lamps are lit
    /// through property blocks on its lamp slots, restored at the end. The billboard and the bike are the film's own.
    /// </summary>
    public sealed class BgcFilm : MapFilm
    {
        protected override string Region => "TAGUIG";
        protected override Color Accent => new Color(1.0f, 0.82f, 0.16f);
        protected override string Score => "sfx_film_bgc_score";
        protected override float CloseAt(bool full) => full ? 12.8f : 4.9f;

        private const float JeepAt = 3.2f, BikeAt = 4.8f, FootAt = 5.9f, GatherAt = 7.0f, BoardAt_ = 8.4f, BoardClose = 8.9f, CardsAt = 8.95f, FlipAt_ = 9.8f;
        private const float StruckCut = 10.15f, LaughCut = 10.6f, AmberAt_ = 11.2f, GreenAt_ = 11.7f, CrouchCut = 12.0f;
        private float _boardAt, _flipAt, _amberAt, _greenAt, _crouchAt;

        /// <summary>A hard, bright late morning: a deep blue sky, a high white sun, colour pushed (not a filter: the
        /// shadows go cool and short).</summary>
        private static readonly Mood Bright = new Mood
        {
            Sky = new Color(0.52f, 0.62f, 0.86f), Equator = new Color(0.66f, 0.66f, 0.7f), Ground = new Color(0.42f, 0.4f, 0.38f),
            Fog = new Color(0.72f, 0.84f, 0.98f), FogStart = 70.0f, FogEnd = 320.0f,
            Zenith = new Color(0.14f, 0.38f, 0.86f), Horizon = new Color(0.66f, 0.82f, 0.99f),
            Sun = new Color(1.0f, 0.97f, 0.9f), SunIntensity = 1.3f, SunElevation = 54.0f,
            Brightness = 1.04f, Saturation = 1.22f, Lamps = 1.0f, KeepYaw = true,
        };

        // ---- the traffic
        private KantoTraffic _traffic;
        private Transform[] _cars = new Transform[0];
        private Vector3[] _carWas = new Vector3[0];
        private Quaternion[] _carTurnWas = new Quaternion[0];
        private int[] _carLane = new int[0];
        private float[] _carAlong = new float[0], _carCruise = new float[0];
        private Quaternion[] _carOffset = new Quaternion[0];
        private int _jeep = -1, _taxi = -1;
        private Vector3 _jeepStop, _jeepDir, _taxiDir, _jeepNow;
        private float _taxiStop, _taxiLane;
        private int _taxiLaneIndex = -1;

        // ---- the signal
        private Renderer _signal;
        private int[] _lampSlot = { -1, -1, -1 };
        private Color[] _lampGlow = { new Color(1.0f, 0.15f, 0.1f), new Color(1.0f, 0.7f, 0.1f), new Color(0.2f, 1.0f, 0.45f) };
        private MaterialPropertyBlock[][] _signalWas = new MaterialPropertyBlock[0][];
        private Renderer[] _allSignals = new Renderer[0];
        private Vector3[] _lampAt = new Vector3[3];
        private Vector3 _head;
        private MaterialPropertyBlock _block;

        // ---- the arrivals
        private readonly int[] _order = new int[Seats];          // arrival order: jeep, jeep, bike, foot
        private readonly int[] _way = new int[Seats];            // 0 jeep, 1 bike, 2 foot
        private readonly Vector3[] _landAt = new Vector3[Seats], _gate = new Vector3[Seats];
        private Transform _bike;
        private Vector3 _bikeFrom, _bikeTo;

        // ---- the billboard
        private Canvas _board;
        private Vector3 _boardAt3, _boardNormal;
        private readonly RectTransform[] _cards = new RectTransform[4];
        private RectTransform _mystery, _versus, _tayaCard, _tayaBand;

        protected override void Prepare()
        {
            ChipRoles = false;
            _boardAt = Still ? 1.6f : BoardAt_;
            _flipAt = Still ? 2.4f : FlipAt_;
            _amberAt = Still ? 3.2f : AmberAt_;
            _greenAt = Still ? 3.6f : GreenAt_;
            _crouchAt = Still ? 3.9f : CrouchCut;
            TitleAt(Still ? 0.2f : 2.5f, Still ? 1.3f : 2.4f);
            TayaAt(_flipAt + 0.3f, CloseAt(!Still) - _flipAt - 0.3f);
            if (!Still) { Cut(0.8f); Cut(1.6f); Cut(2.4f); Cut(JeepAt); Cut(BikeAt); Cut(FootAt); Cut(GatherAt); Cut(BoardAt_); Cut(BoardClose); Cut(StruckCut); Cut(LaughCut); Cut(AmberAt_); Cut(CrouchCut); }
            else { Cut(_boardAt); Cut(_amberAt); Cut(_crouchAt); }
            _block = new MaterialPropertyBlock();

            TakeTraffic();
            TakeSignal();
            PlanArrivals();
            BuildBike();
            BuildBillboard();
            for (int k = 0; k < _arrivals; k++) if (!Still) ChipAt(_order[k], ArriveAt(k) + 0.35f, 1.3f);

            Clip("sfx_film_bgc_brake"); Clip("sfx_film_bgc_horn"); Clip("sfx_film_bgc_hop"); Clip("sfx_film_bgc_skid");
            Clip("sfx_film_bgc_slam"); Clip("sfx_film_bgc_flip"); Clip("sfx_film_bgc_beep"); Clip("sfx_film_bgc_go");
        }

        // ------------------------------------------------------------------ the traffic, held and posed

        private void TakeTraffic()
        {
            _traffic = FindFirstObjectByType<KantoTraffic>();
            if (_traffic == null) return;
            var drivers = _traffic.Drivers;
            int n = drivers.Length;
            _cars = new Transform[n]; _carWas = new Vector3[n]; _carTurnWas = new Quaternion[n];
            _carLane = new int[n]; _carAlong = new float[n]; _carCruise = new float[n]; _carOffset = new Quaternion[n];
            for (int i = 0; i < n; i++)
            {
                var d = drivers[i];
                if (d == null || d.Body == null) continue;
                _cars[i] = d.Body;
                _carWas[i] = d.Body.position; _carTurnWas[i] = d.Body.rotation;
                _carLane[i] = d.Lane; _carAlong[i] = d.Along; _carCruise[i] = Mathf.Max(4.0f, d.Cruise); _carOffset[i] = d.ModelOffset;
            }
            // The jeepney for the arrival: one in the stop's lane, else any.
            for (int i = 0; i < n; i++)
            {
                if (_cars[i] == null || !drivers[i].Jeepney) continue;
                if (_jeep < 0 || drivers[i].Lane == _traffic.StopLane) _jeep = i;
                if (drivers[i].Lane == _traffic.StopLane) break;
            }
            if (_jeep >= 0)
            {
                int lane = _traffic.StopLane >= 0 ? _traffic.StopLane : _carLane[_jeep];
                _traffic.LaneFrame(lane, out var origin, out var dir);
                _jeepDir = dir;
                _jeepStop = origin + dir * _traffic.StopAlong + Vector3.Cross(Vector3.up, dir) * _traffic.StopShift;
                _jeepStop.y = _traffic.GroundY;
            }
            // The taxi for the gag: one whose model is a taxi, driven along the south road toward the crossing.
            for (int i = 0; i < n; i++)
                if (_cars[i] != null && i != _jeep && _traffic.DriverModel(i).ToLowerInvariant().Contains("taxi")) { _taxi = i; break; }
            if (_taxi < 0) for (int i = 0; i < n; i++) if (_cars[i] != null && i != _jeep && !drivers[i].Jeepney) { _taxi = i; break; }
            for (int l = 0; l < KantoTraffic.Lanes; l++)
            {
                _traffic.LaneFrame(l, out var o, out var dir);
                if (Mathf.Abs(dir.x) < 0.9f || o.z - Centre.z > -15.0f) continue;   // along X, on the south side
                if (dir.x < 0.0f) { _taxiLaneIndex = l; _taxiDir = dir; _taxiLane = o.z; }
            }
            if (_taxiLaneIndex >= 0)
            {
                _traffic.LaneFrame(_taxiLaneIndex, out var o, out var dir);
                _taxiStop = Vector3.Dot(new Vector3(Centre.x + 2.6f, 0.0f, o.z) - o, dir);
            }
        }

        /// <summary>How far the traffic has run since the film began, in seconds of its own speed: six times as fast
        /// through the rush, easing to real time by 3.2 s, then real time.</summary>
        private float Run(float age)
        {
            if (Still) return age;
            if (age < 2.4f) return 6.0f * age;
            float u = Mathf.Min(age - 2.4f, 0.8f);
            float eased = 6.0f * u - 5.0f * u * u / 1.6f;
            return 14.4f + eased + Mathf.Max(0.0f, age - 3.2f);
        }

        private void Traffic(float age)
        {
            if (_traffic == null) return;
            float run = Run(age), span = 2.0f * (_traffic.Extent + 4.0f);
            for (int i = 0; i < _cars.Length; i++)
            {
                var body = _cars[i];
                if (body == null || i == _jeep || i == _taxi) continue;
                int lane = _carLane[i];
                if (lane < 0 || lane >= KantoTraffic.Lanes) continue;
                _traffic.LaneFrame(lane, out var origin, out var dir);
                float along = _carAlong[i] + _carCruise[i] * run;
                along = Mathf.Repeat(along + _traffic.Extent + 4.0f, span) - _traffic.Extent - 4.0f;
                Vector3 at = origin + dir * along;
                // Nothing drives through the runner's crossing while they cross it.
                if (!Still && age > FootAt - 0.6f && age < GatherAt + 0.3f && Mathf.Abs(dir.x) > 0.9f && origin.z - Centre.z < -15.0f && Mathf.Abs(at.x - Centre.x + 1.0f) < 9.0f)
                    at -= dir * 18.0f;
                at.y = _traffic.GroundY;
                body.SetPositionAndRotation(at, Quaternion.LookRotation(dir, Vector3.up) * _carOffset[i]);
            }
            Jeep(age);
            Taxi(age);
        }

        private void Jeep(float age)
        {
            if (_jeep < 0) return;
            var body = _cars[_jeep];
            // In from up the lane at speed, standing on its brakes into the stop, a rock forward on its nose.
            float t = Still ? 1.0f : Mathf.Clamp01((age - (JeepAt - 1.0f)) / 1.5f);
            float eased = 1.0f - (1.0f - t) * (1.0f - t) * (1.0f - t);
            Vector3 at = _jeepStop - _jeepDir * 26.0f * (1.0f - eased);
            _jeepNow = at;
            float dip = Still ? 0.0f : 3.0f * Hit(age - (JeepAt + 0.5f), 0.06f, 5.0f);
            at.y = _traffic.GroundY;
            Vector3 right = Vector3.Cross(Vector3.up, _jeepDir);
            body.SetPositionAndRotation(at, Quaternion.AngleAxis(dip, right) * Quaternion.LookRotation(_jeepDir, Vector3.up) * _carOffset[_jeep]);
            if (Crossed(JeepAt + 0.05f)) Cue("sfx_film_bgc_brake", 0.75f);
        }

        private void Taxi(float age)
        {
            if (_taxi < 0 || _taxiLaneIndex < 0) return;
            var body = _cars[_taxi];
            _traffic.LaneFrame(_taxiLaneIndex, out var origin, out var dir);
            // Up the south road at speed, braking hard to stand a metre short of the runner, then waiting.
            float t = Still ? 1.0f : Mathf.Clamp01((age - (FootAt - 0.7f)) / 1.1f);
            float eased = 1.0f - Mathf.Pow(1.0f - t, 2.6f);
            float along = _taxiStop - 30.0f * (1.0f - eased);
            Vector3 at = origin + dir * along; at.y = _traffic.GroundY;
            Vector3 right = Vector3.Cross(Vector3.up, dir);
            float dip = Still ? 0.0f : 3.5f * Hit(age - (FootAt + 0.4f), 0.05f, 4.5f);
            body.SetPositionAndRotation(at, Quaternion.AngleAxis(dip, right) * Quaternion.LookRotation(dir, Vector3.up) * _carOffset[_taxi]);
            if (Crossed(FootAt + 0.25f)) Cue("sfx_film_bgc_brake", 0.85f, 1.1f);
            if (Crossed(FootAt + 0.45f)) { Cue("sfx_film_bgc_horn", 0.8f); Punch(0.6f); }
        }

        // ------------------------------------------------------------------ the signal

        private void TakeSignal()
        {
            if (_traffic == null) return;
            _allSignals = _traffic.Signals ?? new Renderer[0];
            // The signal on the north-east corner (the court's north side faces it), else the first.
            float best = float.PositiveInfinity;
            foreach (var r in _allSignals)
            {
                if (r == null) continue;
                float d = Vector3.Distance(r.bounds.center, Centre + new Vector3(18.0f, 3.0f, 16.5f));
                if (d < best) { best = d; _signal = r; }
            }
            _signalWas = new MaterialPropertyBlock[_allSignals.Length][];
            for (int s = 0; s < _allSignals.Length; s++)
            {
                var r = _allSignals[s];
                if (r == null) continue;
                var mats = r.sharedMaterials;
                _signalWas[s] = new MaterialPropertyBlock[mats.Length];
                for (int k = 0; k < mats.Length; k++)
                {
                    var b = new MaterialPropertyBlock();
                    r.GetPropertyBlock(b, k);
                    _signalWas[s][k] = b;
                }
            }
            if (_signal == null) return;
            var materials = _signal.sharedMaterials;
            for (int k = 0; k < 3; k++)
                for (int m = 0; m < materials.Length; m++)
                    if (materials[m] != null && materials[m].name.StartsWith(_traffic.LampMaterials[k])) { _lampSlot[k] = m; break; }
            // Where each lamp is: its submesh's bounds (a batched mesh keeps its submeshes in world space).
            var filter = _signal.GetComponent<MeshFilter>();
            var mesh = filter != null ? filter.sharedMesh : null;
            for (int k = 0; k < 3; k++)
            {
                _lampAt[k] = _signal.bounds.center + Vector3.up * (1.5f - 0.35f * k);
                if (mesh == null || _lampSlot[k] < 0) continue;
                int index = _signal is MeshRenderer batched && batched.isPartOfStaticBatch ? batched.subMeshStartIndex + _lampSlot[k] : _lampSlot[k];
                if (index >= mesh.subMeshCount) continue;
                var b = mesh.GetSubMesh(index).bounds;
                _lampAt[k] = _signal.isPartOfStaticBatch ? b.center : _signal.transform.TransformPoint(b.center);
            }
            // The head hangs at the end of the arm (film r1: a lamp's submesh centre is between the two faces of a
            // head, not on it): from the pole out along the arm to the far edge of its bounds, under its top.
            Vector3 pole = _signal.transform.position, mid = _signal.bounds.center;
            Vector3 arm = mid - pole; arm.y = 0.0f;
            _head = pole + arm * 2.0f; _head.y = _signal.bounds.max.y - 1.1f;
            MatchArrivalPresentation.Trace($"BGC film signal {_signal.name} slots {_lampSlot[0]} {_lampSlot[1]} {_lampSlot[2]} lamps {_lampAt[0]:F1} {_lampAt[1]:F1} {_lampAt[2]:F1}");
        }

        /// <summary>Which lamp (0 red, 1 amber, 2 green) the signal shows: a fast flicker through the rush, red
        /// through the arrivals and the billboard, amber, then green for go.</summary>
        private int LitLamp(float age, int signal)
        {
            if (!Still && age < 3.2f) return (int)(Run(age) / 2.0f + signal) % 3 == 0 ? 2 : (int)(Run(age) / 2.0f + signal) % 3 == 1 ? 1 : 0;
            if (age < _amberAt) return 0;
            if (age < _greenAt) return 1;
            return 2;
        }

        private void Signals(float age)
        {
            for (int s = 0; s < _allSignals.Length; s++)
            {
                var r = _allSignals[s];
                if (r == null) continue;
                int lit = r == _signal ? LitLamp(age, 0) : LitLamp(age, s + 1);
                var materials = r.sharedMaterials;
                for (int k = 0; k < 3; k++)
                {
                    int slot = -1;
                    for (int m = 0; m < materials.Length; m++) if (materials[m] != null && materials[m].name.StartsWith(_traffic.LampMaterials[k])) { slot = m; break; }
                    if (slot < 0) continue;
                    r.GetPropertyBlock(_block, slot);
                    bool on = k == lit;
                    _block.SetColor("_EmissionColor", on ? _lampGlow[k] * 3.0f : Color.black);
                    if (materials[slot].HasProperty("_Color")) _block.SetColor("_Color", on ? materials[slot].GetColor("_Color") : materials[slot].GetColor("_Color") * 0.25f);
                    r.SetPropertyBlock(_block, slot);
                }
            }
            if (Crossed(_amberAt)) Cue("sfx_film_bgc_beep", 0.6f);
            if (Crossed(_greenAt)) { Cue("sfx_film_bgc_go", 0.95f); Punch(0.9f); Flash(new Color(0.4f, 1.0f, 0.55f), 0.2f); }
        }

        // ------------------------------------------------------------------ the arrivals

        private int _arrivals;

        private float ArriveAt(int k) => _way[_order[k]] == 0 ? JeepAt + 0.55f + 0.25f * k : _way[_order[k]] == 1 ? BikeAt : FootAt;

        private void PlanArrivals()
        {
            // Attackers first, the taya last (on foot, late: nobody knows yet it is the taya).
            _arrivals = 0;
            for (int i = 1; i <= Seats; i++) { int s = (Taya + i) % Seats; if (Body(s) != null) _order[_arrivals++] = s; }
            for (int k = 0; k < _arrivals; k++)
            {
                int s = _order[k];
                _way[s] = k == _arrivals - 1 ? 2 : k < 2 ? 0 : 1;
            }
            // The gates in the park's fence: north for the jeepney's two, west for the bike, south for the runner.
            for (int k = 0; k < _arrivals; k++)
            {
                int s = _order[k];
                Vector3 gate = _way[s] == 0 ? new Vector3(Mark(s).x * 0.5f, 0.0f, 13.6f) : _way[s] == 1 ? new Vector3(-13.6f, 0.0f, 0.4f) : new Vector3(0.3f, 0.0f, -13.6f);
                _gate[s] = Centre + gate; _gate[s].y = Floor(_gate[s], Centre.y);
            }
        }

        private void BuildBike()
        {
            // A blocky toon bike in the rider's own colour: two wheels, a frame, a seat, the bars.
            _bikeFrom = Centre + new Vector3(-20.6f, 0.0f, 18.0f);
            _bikeTo = Centre + new Vector3(-18.6f, 0.0f, 2.2f);
            _bikeFrom.y = Floor(_bikeFrom, Centre.y); _bikeTo.y = Floor(_bikeTo, Centre.y);
            var root = new GameObject("Film bike").transform;
            root.SetParent(transform, false);
            Own(root.gameObject);
            int rider = -1;
            for (int s = 0; s < Seats; s++) if (Body(s) != null && _way[s] == 1) rider = s;
            Color paint = rider >= 0 ? PlayerIdentity.Colour(rider) : new Color(0.9f, 0.2f, 0.2f), tyre = new Color(0.12f, 0.12f, 0.14f), steel = new Color(0.75f, 0.78f, 0.8f);
            void Part(PrimitiveType shape, Vector3 at, Vector3 size, Color colour, Vector3 euler)
            {
                var go = Prop(shape, "Bike part", Vector3.zero, Quaternion.identity, size, colour);
                go.transform.SetParent(root, false);
                go.transform.localPosition = at; go.transform.localRotation = Quaternion.Euler(euler);
            }
            foreach (float z in new[] { -0.52f, 0.52f })
            {
                Part(PrimitiveType.Cylinder, new Vector3(0.0f, 0.33f, z), new Vector3(0.66f, 0.04f, 0.66f), tyre, new Vector3(0.0f, 0.0f, 90.0f));
                Part(PrimitiveType.Cylinder, new Vector3(0.0f, 0.33f, z), new Vector3(0.3f, 0.05f, 0.3f), steel, new Vector3(0.0f, 0.0f, 90.0f));
            }
            Part(PrimitiveType.Cube, new Vector3(0.0f, 0.52f, 0.0f), new Vector3(0.07f, 0.07f, 0.9f), paint, new Vector3(-12.0f, 0.0f, 0.0f));
            Part(PrimitiveType.Cube, new Vector3(0.0f, 0.5f, -0.25f), new Vector3(0.07f, 0.42f, 0.07f), paint, new Vector3(-15.0f, 0.0f, 0.0f));
            Part(PrimitiveType.Cube, new Vector3(0.0f, 0.5f, 0.45f), new Vector3(0.07f, 0.45f, 0.07f), paint, new Vector3(18.0f, 0.0f, 0.0f));
            Part(PrimitiveType.Cube, new Vector3(0.0f, 0.74f, -0.3f), new Vector3(0.14f, 0.05f, 0.24f), tyre, Vector3.zero);
            Part(PrimitiveType.Cube, new Vector3(0.0f, 0.76f, 0.5f), new Vector3(0.5f, 0.04f, 0.04f), steel, Vector3.zero);
            _bike = root;
        }

        /// <summary>The bike's ride: down the west road, a sideways skid to a stop, then laid down when its rider
        /// hops off. Returns where the rider sits and the bike's heading.</summary>
        private void Bike(float age, out Vector3 seat, out float yaw, out float lean)
        {
            float t = Still ? 1.0f : Mathf.Clamp01((age - (BikeAt - 0.6f)) / 1.2f);
            float eased = 1.0f - (1.0f - t) * (1.0f - t);
            Vector3 at = Vector3.Lerp(_bikeFrom, _bikeTo, eased);
            Vector3 way = _bikeTo - _bikeFrom; way.y = 0.0f;
            float heading = Quaternion.LookRotation(way).eulerAngles.y;
            float skid = Span(t, 0.7f, 1.0f);
            yaw = heading + 70.0f * skid;
            lean = Still ? 0.0f : -20.0f * Mathf.Sin(skid * Mathf.PI) * (1.0f - Span(age, BikeAt + 0.3f, BikeAt + 0.5f));
            float down = Still ? 1.0f : Span(age, BikeAt + 0.45f, BikeAt + 0.75f);
            if (_bike != null)
            {
                _bike.SetPositionAndRotation(at, Quaternion.Euler(0.0f, yaw, Mathf.Lerp(lean, 82.0f, down)));
                _bike.gameObject.SetActive(Still || age >= BikeAt - 0.7f);
            }
            seat = at + Vector3.up * 0.12f;
            if (Crossed(BikeAt - 0.05f)) Cue("sfx_film_bgc_skid", 0.75f);
            if (Crossed(BikeAt + 0.7f)) Cue("sfx_film_bgc_hop", 0.5f, 1.3f);
        }

        private void Arrivals(float age)
        {
            Bike(age, out var bikeSeat, out var bikeYaw, out var bikeLean);
            for (int k = 0; k < _arrivals; k++)
            {
                int s = _order[k];
                if (!Posable(s)) continue;
                Greet(s, 0.001f);
                float at = ArriveAt(k);
                if (Still || age >= _boardAt) { BoardPose(s, age); continue; }
                if (age >= GatherAt)
                {
                    // In through the gates at a run, to the marks.
                    ShowSeat(s, true);
                    float p = Mathf.Clamp01((age - GatherAt - 0.05f * k) / (BoardAt_ - GatherAt - 0.25f));
                    Walk(s, Vector3.Lerp(_gate[s], Mark(s), 0.45f), Mark(s), p, MarkYaw(s));
                    Act(s, Running(s, age));
                    continue;
                }
                int way = _way[s];
                if (way == 0)
                {
                    // On the jeepney's back step until the hop, then down and running for the park.
                    // Riding the back step as it comes in, then off it at the stop.
                    Vector3 side = Vector3.Cross(Vector3.up, _jeepDir);
                    Vector3 back = _jeepStop - _jeepDir * 3.1f;
                    Vector3 step = (_jeep >= 0 ? _jeepNow : _jeepStop) - _jeepDir * 3.1f + side * (k == 0 ? 0.35f : -0.35f) + Vector3.up * 0.35f;
                    Vector3 land = back - _jeepDir * 1.2f + side * (k == 0 ? 0.6f : -0.5f);
                    land.y = Floor(land, Centre.y);
                    float hop = age - at;
                    ShowSeat(s, age >= JeepAt - 1.0f);
                    if (hop < 0.0f) { Place(s, step, Yaw(-_jeepDir) + (k == 0 ? 40.0f : -40.0f)); Animate(s, false); }
                    else if (hop < 0.38f)
                    {
                        float p = hop / 0.38f;
                        Place(s, Vector3.Lerp(step, land, Smooth(p)) + Vector3.up * Mathf.Sin(p * Mathf.PI) * 0.6f, Yaw(-_jeepDir), -8.0f);
                        Animate(s, false);
                        if (Crossed(at + 0.36f)) Cue("sfx_film_bgc_hop", 0.6f, 0.95f + 0.1f * k);
                    }
                    else
                    {
                        Vector3 toPark = (Centre - land); toPark.y = 0.0f;
                        Vector3 run = land + toPark.normalized * 3.0f;
                        Walk(s, land, run, Mathf.Clamp01((hop - 0.45f) / 1.2f), Yaw(toPark));
                    }
                    Act(s, hop < 0.0f ? Hanging(s, age) : Landed(s, hop));
                }
                else if (way == 1)
                {
                    ShowSeat(s, age >= BikeAt - 0.7f);
                    if (age < BikeAt + 0.45f)
                    {
                        // Riding: crouched over the bars, then leaning into the skid.
                        Place(s, bikeSeat, bikeYaw, 18.0f, bikeLean);
                        Animate(s, false);
                        var si = Side(s); var f = Facing(s);
                        Act(s, new Feel { Lean = 26.0f, Nod = -12.0f, Left = f + Vector3.down * 0.25f - si * 0.3f, Right = f + Vector3.down * 0.25f + si * 0.3f, Arms = 1.0f });
                    }
                    else
                    {
                        // Off and pointing at the park.
                        float off = age - (BikeAt + 0.45f);
                        Vector3 stand = bikeSeat + Quaternion.Euler(0.0f, bikeYaw, 0.0f) * new Vector3(0.7f, 0.0f, 0.0f);
                        stand.y = Floor(stand, Centre.y);
                        Vector3 toPark = Centre - stand; toPark.y = 0.0f;
                        Place(s, stand + Vector3.up * Mathf.Sin(Mathf.Clamp01(off / 0.3f) * Mathf.PI) * 0.35f, Yaw(toPark));
                        Animate(s, false);
                        Vector3 park = Centre + Vector3.up * 1.0f;
                        Act(s, new Feel { Lean = 8.0f, Nod = -6.0f, Right = (park - Chest(s)).normalized + Vector3.up * 0.3f, Left = Vector3.up * 0.8f - Side(s) * 0.6f, Arms = 1.0f });
                    }
                }
                else
                {
                    // The runner: across the south road, a spin and a salute at the taxi, and on.
                    ShowSeat(s, age >= FootAt - 0.5f);
                    Vector3 from = new Vector3(Centre.x - 2.6f, 0.0f, _taxiLane - 6.5f), mid = new Vector3(Centre.x - 1.2f, 0.0f, _taxiLane + 0.3f), to = new Vector3(Centre.x - 0.4f, 0.0f, _taxiLane + 6.0f);
                    from.y = Floor(from, Centre.y); mid.y = Floor(mid, Centre.y); to.y = Floor(to, Centre.y);
                    float run = age - (FootAt - 0.5f);
                    if (run < 0.95f)
                    {
                        Vector3 p = Vector3.Lerp(from, mid, Mathf.Clamp01(run / 0.95f));
                        Place(s, p, Yaw(mid - from), 14.0f); Animate(s, true, 1.6f); Ground(s, p.y);
                        Act(s, Running(s, age));
                    }
                    else if (run < 1.6f)
                    {
                        // Stopped dead in the lane by the taxi: a spin to it, a salute.
                        float q = (run - 0.95f) / 0.65f;
                        Vector3 toTaxi = (_taxi >= 0 ? _cars[_taxi].position : mid + Vector3.right * 3.0f) - mid; toTaxi.y = 0.0f;
                        Place(s, mid + Vector3.up * 0.18f * Hit(q, 0.1f, 6.0f), Mathf.LerpAngle(Yaw(mid - from), Yaw(toTaxi), Back(q * 2.5f, 1.4f)));
                        Animate(s, false);
                        var f = Facing(s); var si = Side(s);
                        var salute = new Feel { Lean = -8.0f, Nod = -8.0f, Tilt = 10.0f, Right = Vector3.up * 0.9f + f * 0.3f - si * 0.5f, Left = Vector3.down - si * 0.2f, Arms = 1.0f };
                        var startle = Big(Flinch(s, run - 0.95f), 1.3f);
                        Act(s, Feel.Lerp(startle, salute, Span(q, 0.2f, 0.45f)));
                    }
                    else
                    {
                        float q = Mathf.Clamp01((run - 1.6f) / 0.8f);
                        Vector3 p = Vector3.Lerp(mid, to, q);
                        Place(s, p, Yaw(to - mid), 14.0f); Animate(s, true, 1.6f); Ground(s, p.y);
                        Act(s, Running(s, age));
                    }
                }
            }
        }

        private static float Yaw(Vector3 dir) { dir.y = 0.0f; return dir.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(dir).eulerAngles.y : 0.0f; }

        private Feel Hanging(int s, float age)
        {
            // On the back step, one hand on the rail, leaning out, the other arm waving: the jeepney way.
            var f = Facing(s); var si = Side(s);
            float wave = Mathf.Sin(age * 10.0f);
            return new Feel { Lean = -10.0f, Rock = 14.0f, Nod = -10.0f, Left = Vector3.up * 0.9f + si * 0.2f, Right = Vector3.up * 0.6f + si * (0.6f + 0.4f * wave), Arms = 1.0f };
        }

        private Feel Landed(int s, float hop)
        {
            // The jump: arms up; landing in a crouch; then a fist pump as they run.
            var si = Side(s); var f = Facing(s);
            var air = new Feel { Lean = -12.0f, Nod = -16.0f, Left = Vector3.up * 0.8f - si, Right = Vector3.up * 0.8f + si, Arms = 1.0f };
            var land = new Feel { Lean = 32.0f, Nod = 8.0f, Left = f * 0.6f + Vector3.down - si * 0.4f, Right = f * 0.6f + Vector3.down + si * 0.4f, Arms = 0.9f };
            if (hop < 0.38f) return air;
            var after = Feel.Lerp(land, Big(FistPump(s, hop), 1.2f), Span(hop, 0.55f, 0.75f));
            return Feel.Lerp(air, after, Span(hop, 0.38f, 0.45f));
        }

        private Feel Running(int s, float age)
        {
            var si = Side(s); var f = Facing(s);
            float pump = Mathf.Sin(age * 14.0f + s);
            return new Feel { Lean = 16.0f, Nod = -6.0f, Left = f * pump - Vector3.up * 0.6f - si * 0.2f, Right = -f * pump - Vector3.up * 0.6f + si * 0.2f, Arms = 0.8f };
        }

        // ------------------------------------------------------------------ the billboard

        private void BuildBillboard()
        {
            // On the face of the tower north of the park, high over the street, square to the court.
            Vector3 from = Centre + new Vector3(0.0f, 13.0f, 14.0f);
            _boardAt3 = Centre + new Vector3(0.0f, 13.0f, 31.0f); _boardNormal = Vector3.back;
            int count = Physics.RaycastNonAlloc(from, Vector3.forward, Hits, 60.0f, ~0, QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
                if (World(Hits[i].collider) && Hits[i].distance < nearest && Mathf.Abs(Hits[i].normal.y) < 0.3f) { nearest = Hits[i].distance; _boardAt3 = Hits[i].point; _boardNormal = Hits[i].normal; }
            _boardNormal.y = 0.0f; _boardNormal.Normalize();
            // Stood off the facade on brackets (film r1 set it into the wall: the balconies came through it).
            Vector3 face = _boardAt3 + _boardNormal * 1.4f;
            _boardAt3 = face;
            var turn = Quaternion.LookRotation(-_boardNormal);
            // The frame: a dark backing with a yellow rim, on two brackets.
            Prop(PrimitiveType.Cube, "Billboard back", face - _boardNormal * 0.15f, turn, new Vector3(14.6f, 8.3f, 0.25f), new Color(0.1f, 0.1f, 0.13f));
            Prop(PrimitiveType.Cube, "Billboard rim top", face + Vector3.up * 4.15f, turn, new Vector3(14.8f, 0.22f, 0.4f), Accent);
            Prop(PrimitiveType.Cube, "Billboard rim bottom", face - Vector3.up * 4.15f, turn, new Vector3(14.8f, 0.22f, 0.4f), Accent);
            var across = Vector3.Cross(Vector3.up, _boardNormal);
            foreach (float side in new[] { -5.5f, 5.5f })
                foreach (float high in new[] { -2.6f, 2.6f })
                    Prop(PrimitiveType.Cube, "Billboard bracket", face + across * side + Vector3.up * high - _boardNormal * 0.8f, turn, new Vector3(0.18f, 0.18f, 1.4f), new Color(0.3f, 0.3f, 0.33f));

            var go = new GameObject("Film billboard", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            Own(go);
            _board = go.AddComponent<Canvas>();
            _board.renderMode = RenderMode.WorldSpace;
            _board.vertexColorAlwaysGammaSpace = true;
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(1600, 900);
            rect.localScale = Vector3.one * (14.2f / 1600.0f);
            rect.SetPositionAndRotation(face + _boardNormal * 0.02f, Quaternion.LookRotation(-_boardNormal));
            var screen = Panel(rect, "Screen", new Color(0.09f, 0.16f, 0.42f));
            OwnerUiLayout.Fill(screen.rectTransform);
            // The three attackers' cards on the left, VS, the taya's card on the right (a "?" until the flip).
            int c = 0;
            for (int k = 0; k < _arrivals; k++)
            {
                int s = _order[k];
                if (s == Taya) continue;
                _cards[c] = Card(rect, "Card " + s, s, new Vector2(-560 + 275 * c, 30), new Vector2(255, 380), UiTheme.Offense);
                c++;
            }
            _versus = Word(rect, "VS", "VS", 240, Accent).rectTransform;
            _versus.anchoredPosition = new Vector2(250, 30);
            _mystery = Card(rect, "Mystery", -1, new Vector2(570, 60), new Vector2(380, 560), new Color(0.3f, 0.3f, 0.35f));
            _tayaCard = Card(rect, "Taya card", Taya, new Vector2(570, 60), new Vector2(380, 560), UiTheme.Defense);
            var band = Panel(rect, "Taya band", UiTheme.Defense);
            _tayaBand = band.rectTransform;
            _tayaBand.sizeDelta = new Vector2(460, 130); _tayaBand.anchoredPosition = new Vector2(570, -315);
            var word = Word(_tayaBand, "Word", "TAYA", 104, Color.white);
            OwnerUiLayout.Fill(word.rectTransform);
            word.fontStyle = FontStyle.Italic;
        }

        private Image Panel(RectTransform parent, string name, Color colour)
        {
            var image = OwnerUiLayout.Rect(parent, name).gameObject.AddComponent<Image>();
            image.color = colour; image.raycastTarget = false;
            return image;
        }

        private Text Word(RectTransform parent, string name, string words, int size, Color colour)
        {
            var text = OwnerUiLayout.Text(parent, name, words, size, OwnerUiLayout.TypeRole.Display);
            text.alignment = TextAnchor.MiddleCenter; text.color = colour; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow; text.verticalOverflow = VerticalWrapMode.Overflow;
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = UiTheme.InGameOutline; outline.effectDistance = new Vector2(6, -6);
            text.rectTransform.sizeDelta = new Vector2(600, size * 1.3f);
            return text;
        }

        /// <summary>A card: a frame in `edge`, the seat's portrait (or a big "?" for -1), a seat-colour bar under it.</summary>
        private RectTransform Card(RectTransform parent, string name, int seat, Vector2 at, Vector2 size, Color edge)
        {
            var frame = Panel(parent, name, edge).rectTransform;
            frame.sizeDelta = size; frame.anchoredPosition = at;
            var inner = Panel(frame, "Inner", new Color(0.13f, 0.14f, 0.2f)).rectTransform;
            inner.anchorMin = Vector2.zero; inner.anchorMax = Vector2.one; inner.offsetMin = new Vector2(10, 34); inner.offsetMax = new Vector2(-10, -10);
            if (seat < 0)
            {
                var q = Word(inner, "Mark", "?", (int)(size.y * 0.55f), new Color(1.0f, 0.95f, 0.8f));
                OwnerUiLayout.Fill(q.rectTransform);
            }
            else
            {
                var body = Body(seat);
                string id = body != null ? body.CharacterName().ToLowerInvariant().Replace(' ', '_') : "";
                var face = UI.Hub.HubKit.Picture(inner, "Face", UI.Hub.HubKit.Portrait(id));
                OwnerUiLayout.Fill(face.rectTransform);
                var bar = Panel(frame, "Seat", PlayerIdentity.Colour(seat)).rectTransform;
                bar.anchorMin = new Vector2(0, 0); bar.anchorMax = new Vector2(1, 0); bar.pivot = new Vector2(0.5f, 0);
                bar.sizeDelta = new Vector2(-20, 20); bar.anchoredPosition = new Vector2(0, 8);
            }
            return frame;
        }

        private void Board(float age)
        {
            if (_board == null) return;
            // The cards slam in one after another, VS, the mystery card spinning, then the flip to the taya.
            for (int c = 0; c < _cards.Length; c++)
            {
                if (_cards[c] == null) continue;
                float since = age - ((Still ? _boardAt + 0.1f : CardsAt) + 0.2f * c);
                float into = Still ? 1.0f : Mathf.Clamp01(since / 0.16f);
                _cards[c].localScale = Vector3.one * (since < 0.0f ? 0.0f : Mathf.Lerp(1.6f, 1.0f, into) + (since > 0.16f ? 0.08f * Mathf.Exp(-(since - 0.16f) * 12.0f) * Mathf.Sin((since - 0.16f) * 40.0f) : 0.0f));
                if (Crossed(CardsAt + 0.2f * c)) Cue("sfx_film_bgc_slam", 0.6f, 1.0f + 0.08f * c);
            }
            float vsAt = Still ? _boardAt + 0.5f : CardsAt + 0.65f;
            float vs = age - vsAt;
            _versus.localScale = Vector3.one * (vs < 0.0f ? 0.0f : Mathf.Lerp(2.0f, 1.0f, Mathf.Clamp01(vs / 0.14f)) * (1.0f + 0.06f * Mathf.Sin(age * 9.0f)));
            if (Crossed(vsAt)) Cue("sfx_film_bgc_slam", 0.8f, 0.8f);
            float flip = age - _flipAt;
            float spin = Still ? 0.0f : Mathf.Max(0.0f, age - (vsAt + 0.05f)) * 720.0f * (1.0f - Span(age, _flipAt - 0.35f, _flipAt));
            bool shown = age >= vsAt + 0.05f || Still;
            _mystery.gameObject.SetActive(shown && flip < 0.0f);
            _mystery.localRotation = Quaternion.Euler(0.0f, spin, 0.0f);
            _tayaCard.gameObject.SetActive(flip >= 0.0f);
            _tayaCard.localRotation = Quaternion.Euler(0.0f, Mathf.Lerp(90.0f, 0.0f, Back(Mathf.Clamp01(flip / 0.25f), 1.6f)), 0.0f);
            _tayaBand.gameObject.SetActive(flip >= 0.15f);
            _tayaBand.localScale = Vector3.one * Mathf.Lerp(1.8f, 1.0f, Mathf.Clamp01((flip - 0.15f) / 0.14f));
            if (Crossed(_flipAt)) { Cue("sfx_film_bgc_flip", 0.9f); Punch(0.8f); ChipRoles = true; }
        }

        // ------------------------------------------------------------------ the frame

        protected override void Stage(float age)
        {
            Tint(Bright, 1.0f);
            Traffic(age);
            Signals(age);
            Board(age);
            if (!Still && age < JeepAt - 1.0f)
            {
                // Nobody has arrived yet: the city alone.
                for (int s = 0; s < Seats; s++) if (Posable(s)) { ShowSeat(s, false); Place(s, Mark(s), MarkYaw(s)); Animate(s, false); }
            }
            else Arrivals(age);

            if (Still) { StageShort(age); return; }
            if (age < JeepAt) { Beat = "rush"; Rush(age); return; }
            if (age < BikeAt) { Beat = "jeep"; JeepShot(age); return; }
            if (age < FootAt) { Beat = "bike"; BikeShot(age); return; }
            if (age < GatherAt) { Beat = "foot"; FootShot(age); return; }
            if (age < BoardAt_) { Beat = "gather"; GatherShot(age); return; }
            if (age < AmberAt_) { Beat = age < StruckCut ? "board" : age < LaughCut ? "struck" : "laugh"; BoardShots(age); return; }
            Beat = age < CrouchCut ? "light" : "go";
            GoShots(age);
        }

        private void StageShort(float age)
        {
            if (age < _boardAt) { Beat = "establish"; Shoot(Centre + new Vector3(9.0f, 7.0f, -14.0f), Centre + new Vector3(0.0f, 1.0f, 2.0f), 50.0f); }
            else if (age < _amberAt) { Beat = "board"; BoardShots(age); }
            else { Beat = age < _crouchAt ? "light" : "go"; GoShots(age); }
        }

        /// <summary>On the marks for the billboard: every head turned up to it; the three turned to it. After the flip:
        /// the taya struck and sagging, the three cheering and laughing at them; at the green, the three drop into a
        /// sprint crouch facing the taya and the taya squares up.</summary>
        private void BoardPose(int s, float age)
        {
            ShowSeat(s, true);
            bool attacker = s != Taya;
            Vector3 board = _boardAt3 + Vector3.up * 0.0f;
            // The three turned to the board until the flip, then round again to laugh at the taya.
            float turned = attacker ? 1.0f - Span(age, _flipAt + 0.25f, _flipAt + 0.45f) : 0.0f;
            float yaw = Mathf.LerpAngle(MarkYaw(s), Yaw(board - Mark(s)), turned);
            Place(s, Mark(s), yaw);
            Animate(s, false);
            Ground(s, Mark(s).y);
            Vector3 up = (board - Chest(s)).normalized;
            float flip = age - _flipAt;
            var si = Side(s); var f = Facing(s);
            Feel feel;
            if (age >= _crouchAt)
            {
                float t = age - _crouchAt;
                feel = attacker
                    ? Big(new Feel { Lean = 38.0f, Nod = -26.0f, Left = -f * 0.7f + Vector3.down * 0.5f - si * 0.4f, Right = f * 0.9f + Vector3.down * 0.4f + si * 0.2f, Arms = 1.0f, Hop = 0.04f * Mathf.Abs(Mathf.Sin(t * 9.0f)) }, 1.0f)
                    : Defiant(s, Centre + new Vector3(0.0f, 1.3f, 9.0f));
            }
            else if (flip < 0.0f)
            {
                // Looking up at the board, the three bouncing on their toes.
                float bob = Mathf.Abs(Mathf.Sin(age * 6.0f + s));
                feel = new Feel { Lean = -6.0f, Nod = -34.0f, Turn = 0.0f, Left = Vector3.down + f * 0.2f - si * 0.3f, Right = Vector3.down + f * 0.2f + si * 0.3f, Arms = 0.6f, Hop = attacker ? 0.05f * bob : 0.0f };
            }
            else if (!attacker)
            {
                var shock = Big(Shock(s), 1.4f);
                shock.Hop = 0.2f * Hit(flip, 0.07f, 6.0f);
                feel = flip < 0.3f ? Feel.Lerp(new Feel(), shock, Smooth(flip / 0.08f)) : Feel.Lerp(shock, Big(Slump(s, Mathf.Sin((flip - 0.3f) * 2.4f)), 1.3f), Smooth((flip - 0.3f) / 0.45f));
            }
            else
            {
                int kind = 0;
                for (int o = 0; o < Seats; o++) if (o != Taya && o != s && Body(o) != null && Mark(o).x < Mark(s).x) kind++;
                Feel own = kind == 0 ? Big(LaughAt(s, Chest(Taya), flip, true), 1.5f) : kind == 1 ? Big(DoubledOver(s, flip), 1.4f) : Big(FistPump(s, flip), 1.35f);
                feel = Feel.Lerp(Big(Cheer(s, flip, 0.4f + 0.04f * s), 1.3f), own, Smooth((flip - 0.45f - 0.1f * kind) / 0.25f));
            }
            Act(s, feel);
        }

        private static Feel Big(Feel f, float k)
        {
            f.Lean *= k; f.Twist *= k; f.Rock *= k; f.Nod *= k; f.Tilt *= k; f.Hop *= k;
            return f;
        }

        // ------------------------------------------------------------------ the shots

        private void Rush(float age)
        {
            if (age < 0.8f)
            {
                // High over the north-east crossing, looking straight down at the traffic streaming through it.
                float p = age / 0.8f;
                Vector3 x = Centre + new Vector3(22.0f, 0.0f, 22.0f);
                Shoot(x + new Vector3(-4.0f, Mathf.Lerp(30.0f, 27.0f, p), -8.0f), x + new Vector3(0.0f, 0.0f, 1.0f), 50.0f, Mathf.Lerp(-8.0f, -4.0f, p));
            }
            else if (age < 1.6f)
            {
                // Low at the north kerb, the cars whipping past the lens.
                float p = (age - 0.8f) / 0.8f;
                Vector3 eye = Centre + new Vector3(Mathf.Lerp(4.0f, 2.5f, p), 0.55f, 18.7f);
                Shoot(eye, eye + new Vector3(-12.0f, 0.4f, 3.2f), 56.0f, 3.0f);
            }
            else if (age < 2.4f)
            {
                // From a tower top down the east avenue, the park at the end of it.
                float p = (age - 1.6f) / 0.8f;
                // (Film r1 at 30 m had a rooftop across half the frame: higher.)
                Vector3 eye = Centre + new Vector3(Mathf.Lerp(46.0f, 42.0f, p), Mathf.Lerp(44.0f, 41.0f, p), Mathf.Lerp(-30.0f, -26.0f, p));
                Shoot(eye, Centre + new Vector3(10.0f, 0.0f, 6.0f), 48.0f);
            }
            else
            {
                // Gliding in over the park's south-east corner to the court as the city slows to real time.
                float p = Smooth((age - 2.4f) / 0.8f);
                Vector3 eye = Centre + new Vector3(Mathf.Lerp(20.0f, 14.0f, p), Mathf.Lerp(16.0f, 10.0f, p), Mathf.Lerp(-22.0f, -16.0f, p));
                Shoot(eye, Centre + new Vector3(0.0f, 0.5f, 3.0f), 50.0f);
            }
        }

        private void JeepShot(float age)
        {
            // Low at the back of the stop, front three-quarter on the jeepney's tail: the brake, the two off the back.
            float p = Smooth((age - JeepAt) / (BikeAt - JeepAt));
            Vector3 side = Vector3.Cross(Vector3.up, _jeepDir);
            Vector3 back = _jeepStop - _jeepDir * 3.1f;
            Vector3 eye = back - _jeepDir * Mathf.Lerp(6.5f, 5.6f, p) - side * 2.6f + Vector3.up * 1.1f;
            Shoot(eye, back + Vector3.up * 0.9f - _jeepDir * 0.6f, 50.0f, -2.0f);
        }

        private void BikeShot(float age)
        {
            // Low at the kerb ahead of the bike: it comes at the lens and skids broadside, the rider hops off.
            Vector3 eye = _bikeTo + new Vector3(2.4f, 0.55f, -3.4f);
            Vector3 aim = Vector3.Lerp(_bikeFrom, _bikeTo, Smooth(Mathf.Clamp01((age - (BikeAt - 0.6f)) / 1.2f))) + Vector3.up * 0.7f;
            Shoot(eye, aim, 46.0f, 2.0f);
        }

        private void FootShot(float age)
        {
            // Down the south road, the taxi coming at the runner and standing on its brakes; the runner crossing.
            float p = Smooth((age - FootAt) / (GatherAt - FootAt));
            Vector3 eye = new Vector3(Centre.x - 7.5f, 1.2f, _taxiLane - 3.8f);
            Shoot(eye, new Vector3(Centre.x + Mathf.Lerp(0.5f, -0.5f, p), 0.9f, _taxiLane + 0.6f), 50.0f, -1.5f);
        }

        private void GatherShot(float age)
        {
            float p = Smooth((age - GatherAt) / (BoardAt_ - GatherAt));
            // (Film r3 from 14 m up: four specks; r4 low in a corner: an empty plaza.) Behind the runner coming in at
            // the south gate, up the court: the three converging on their marks beyond, everyone in one frame.
            Shoot(Centre + new Vector3(Mathf.Lerp(-2.4f, -2.0f, p), Mathf.Lerp(2.3f, 2.0f, p), Mathf.Lerp(-15.0f, -12.5f, p)), Centre + new Vector3(0.0f, 0.8f, 4.5f), 50.0f);
        }

        private void BoardShots(float age)
        {
            if (!Still && age < BoardClose)
            {
                // Low behind the four, all looking up at the tower: the board lit over their heads.
                float p = Smooth((age - _boardAt) / (BoardClose - _boardAt));
                Vector3 eye = Mark(Taya) + new Vector3(-1.6f, Mathf.Lerp(0.9f, 1.0f, p), -3.6f);
                Vector3 aim = Vector3.Lerp(Centre + new Vector3(0.0f, 2.5f, 9.0f), _boardAt3, 0.8f);
                Shoot(eye, aim, 52.0f);
                return;
            }
            if (Still || age < StruckCut)
            {
                // The board itself, from the north kerb looking up: the cards slam in, VS, the spin, the flip.
                float p = Still ? 0.0f : Smooth((age - BoardClose) / (StruckCut - BoardClose));
                Vector3 eye = _boardAt3 + _boardNormal * Mathf.Lerp(17.0f, 15.5f, p);
                eye.y = Centre.y + 2.0f;
                Shoot(eye, _boardAt3 + Vector3.up * 0.2f, 40.0f, -2.0f);
                return;
            }
            if (age < LaughCut)
            {
                // The taya, struck, from the front and low.
                Framed("board taya", Taya, 0.7f, (age - StruckCut) / (LaughCut - StruckCut), out var te, out var ta, out var tf);
                Shoot(te, ta, tf);
                return;
            }
            // The three, turned round on the taya, laughing: front three-quarter from the south-east, their points
            // crossing the frame toward the taya just off it (the lesson of Sitangkai's r5 and r6).
            float q = Smooth((age - LaughCut) / (AmberAt_ - LaughCut));
            Vector3 e = Centre + new Vector3(Mathf.Lerp(4.6f, 4.3f, q), 1.45f, Mathf.Lerp(3.9f, 4.3f, q));
            Shoot(ClearEye(Centre + Vector3.up, e), Centre + new Vector3(-0.5f, 0.9f, 8.6f), 50.0f, 1.0f);
        }

        private void GoShots(float age)
        {
            if (age < _crouchAt && _signal != null)
            {
                // Close on the light's head, from below and in front of it: amber, then green.
                Vector3 toward = Centre - _head; toward.y = 0.0f; toward.Normalize();
                float q = Smooth((age - _amberAt) / (_crouchAt - _amberAt));
                Shoot(_head + toward * Mathf.Lerp(4.6f, 4.0f, q) + Vector3.down * 2.2f, _head, 32.0f, -6.0f);
                return;
            }
            // Front three-quarter on the three, low (film r1 behind the taya: their back filled the frame; r3 and r4
            // side on: everyone small at the edges): dropped into a sprint crouch, coming at the lens.
            float p = Still ? 0.0f : Smooth((age - _crouchAt) / (CloseAt(!Still) - _crouchAt));
            Vector3 eye = Centre + new Vector3(Mathf.Lerp(3.8f, 3.4f, p), 0.8f, Mathf.Lerp(4.4f, 4.9f, p));
            Shoot(ClearEye(Centre + Vector3.up, eye), Centre + new Vector3(-0.4f, 0.7f, 8.8f), 52.0f, -2.0f);
        }

        protected override void TayaShot(float local, out Vector3 eye, out Vector3 aim, out float fov) => Framed("taya close", Taya, 0.7f, local, out eye, out aim, out fov);

        protected override void Restore()
        {
            for (int i = 0; i < _cars.Length; i++) if (_cars[i] != null) _cars[i].SetPositionAndRotation(_carWas[i], _carTurnWas[i]);
            for (int s = 0; s < _allSignals.Length; s++)
            {
                var r = _allSignals[s];
                if (r == null || _signalWas[s] == null) continue;
                for (int k = 0; k < _signalWas[s].Length; k++) r.SetPropertyBlock(_signalWas[s][k], k);
            }
        }
    }
}
