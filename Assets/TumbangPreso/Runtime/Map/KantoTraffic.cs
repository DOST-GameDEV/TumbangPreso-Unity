using System;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// KANTO'S STREET TRAFFIC: the cars, taxis, vans, buses, jeepneys and tricycles placed in the
    /// driving lanes DRIVE them, queue behind each other, stop at the signals, which cycle with
    /// their lamps lit per phase, and jeepneys pull in at the stop by the park. Owner, 2026-09-27:
    /// *"kanto is basically done, needs ... liveliness like moving cars/traffic"*
    /// (docs/KANTO_DESIGN_GUIDE.md § 12.2).
    ///
    /// ⚠️⚠️ VISUAL ONLY. Nothing here is networked, has a collider, or touches gameplay, the lata or
    /// a tsinelas: every lane lies on a road centre line 22 m out, so the nearest lane is 20.2 m
    /// from the court centre and the play walls stand at +/-13. Each client runs its own traffic
    /// from its own clock (like AmbientLife); nobody needs to see the same taxi at the same time.
    ///
    /// THE NETWORK is the map's # grid (tools/author_kanto_blockout.py): four road centre lines,
    /// x = +/-Road and z = +/-Road, each running to +/-Extent, with one lane each side at
    /// LaneOffset. ⚠️ RIGHT-HAND TRAFFIC, derived rather than assumed: a lane sits on the RIGHT of
    /// its travel direction (Vector3.Cross(up, dir)). The Blender scene drives on the right, and
    /// the glTF import mirrors X, which flips both the scene's handedness and "right" together, so
    /// the placed cars in Unity still sit right of their noses; the builder assigns each placed car
    /// to its lane by position and keeps its exact placed rotation RELATIVE to the lane
    /// (ModelOffset), so whichever axis a model's nose uses, it drives nose first.
    ///
    /// MOTION is a small car-following model (the Intelligent Driver Model's shape): each vehicle
    /// accelerates toward its cruise speed and brakes to keep a gap that grows with speed to the
    /// vehicle ahead in its lane, or to the stop line of a red or amber light. A vehicle that
    /// leaves the far end of its lane re-enters at the near end, out in the haze (fog 40..230 m),
    /// when there is room.
    ///
    /// SIGNALS alternate the two road axes: green, amber, then a short all-red. Each signal head
    /// faces along one axis (its local +Z) and shows that axis's phase through a
    /// MaterialPropertyBlock on its lamp slots, so no shared material is written. Vehicles' own
    /// red lamp slot doubles as their brake lights and brightens while they slow down.
    /// </summary>
    public sealed class KantoTraffic : MonoBehaviour
    {
        [Serializable] public sealed class Driver
        {
            public Transform Body;
            public int Lane;
            public float Along;
            public float Length = 4.5f;
            public float Cruise = 9f;
            public Quaternion ModelOffset = Quaternion.identity;
            public bool Jeepney;
        }

        public float Road = 22f, LaneOffset = 1.8f, Extent = 128f;
        public float GroundY = 0f;
        public float StopBack = 9f;            // stop line, metres before an intersection's centre
        public Driver[] Drivers = Array.Empty<Driver>();
        public Renderer[] Signals = Array.Empty<Renderer>();
        // The jeepney stop by the park: its lane, where along the lane, and how far it pulls over.
        public int StopLane = -1;
        public float StopAlong, StopShift = 2.2f;
        public Vector2 StopDwell = new Vector2(5f, 10f);
        public float GreenSeconds = 14f, AmberSeconds = 3f, AllRedSeconds = 1.5f;

        // Lanes: 0..3 run along X (lines z = -Road, +Road; dir +X, -X), 4..7 along Z.
        private const int LaneCount = 8;
        private readonly Vector3[] _origin = new Vector3[LaneCount], _dir = new Vector3[LaneCount];
        private readonly int[] _axis = new int[LaneCount];
        private readonly float[][] _crossings = new float[LaneCount][];

        private float[] _speed, _shift, _dwell, _brake;
        private int[] _stopState;               // 0 none, 1 pulling in, 2 dwelling, 3 pulling out, 4 served
        private int[][] _order;                 // per lane, driver indices sorted by Along
        private int[] _orderCount;
        private System.Random _random;
        private float _clock;
        private MaterialPropertyBlock _block;
        private int[][] _lampSlots;             // per signal: red, amber, green material indices
        private int[] _signalAxis;
        private Renderer[][] _vehicleRenderers;
        private int[][] _brakeSlots;
        private Color[][] _brakeBase;           // each brake slot's authored emission, read once (sharedMaterials allocates)
        private Color[] _lampEmission = new Color[3];
        private int _lastPhase = -1;
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private void Start()
        {
            _random = new System.Random(Guid.NewGuid().GetHashCode());
            BuildLanes();
            int n = Drivers.Length;
            _speed = new float[n]; _shift = new float[n]; _dwell = new float[n]; _brake = new float[n]; _stopState = new int[n];
            _order = new int[LaneCount][]; _orderCount = new int[LaneCount];
            for (int l = 0; l < LaneCount; l++) _order[l] = new int[n];
            _vehicleRenderers = new Renderer[n][]; _brakeSlots = new int[n][]; _brakeBase = new Color[n][];
            for (int i = 0; i < n; i++)
            {
                var d = Drivers[i];
                if (d.Body == null) continue;
                _speed[i] = d.Cruise * Range(.5f, 1f);
                foreach (var c in d.Body.GetComponentsInChildren<Collider>()) c.enabled = false;
                _vehicleRenderers[i] = d.Body.GetComponentsInChildren<Renderer>();
                _brakeSlots[i] = new int[_vehicleRenderers[i].Length];
                _brakeBase[i] = new Color[_vehicleRenderers[i].Length];
                for (int r = 0; r < _vehicleRenderers[i].Length; r++)
                {
                    int slot = SlotOf(_vehicleRenderers[i][r], "signal_red");
                    var mat = slot >= 0 ? _vehicleRenderers[i][r].sharedMaterials[slot] : null;
                    if (mat == null || !mat.HasProperty(EmissionId)) slot = -1;
                    else _brakeBase[i][r] = mat.GetColor(EmissionId);
                    _brakeSlots[i][r] = slot;
                }
                // A jeepney that starts AT the stop begins its dwell there.
                if (d.Jeepney && d.Lane == StopLane && Mathf.Abs(d.Along - StopAlong) < 3f)
                { _stopState[i] = 2; _dwell[i] = Range(StopDwell.x, StopDwell.y); _shift[i] = StopShift; _speed[i] = 0; }
            }
            _block = new MaterialPropertyBlock();
            _lampSlots = new int[Signals.Length][]; _signalAxis = new int[Signals.Length];
            for (int s = 0; s < Signals.Length; s++)
            {
                var r = Signals[s];
                if (r == null) continue;
                _lampSlots[s] = new[] { SlotOf(r, "signal_red"), SlotOf(r, "signal_amber"), SlotOf(r, "signal_green") };
                var f = r.transform.forward;
                _signalAxis[s] = Mathf.Abs(f.x) > Mathf.Abs(f.z) ? 0 : 1;
                for (int k = 0; k < 3; k++)
                {
                    int slot = _lampSlots[s][k];
                    if (slot >= 0 && r.sharedMaterials[slot].HasProperty(EmissionId))
                        _lampEmission[k] = r.sharedMaterials[slot].GetColor(EmissionId);
                }
            }
            _clock = Range(0f, CycleSeconds);
        }

        private float Range(float a, float b) => a + (float)_random.NextDouble() * (b - a);
        private float CycleSeconds => 2f * (GreenSeconds + AmberSeconds + AllRedSeconds);

        private static int SlotOf(Renderer r, string material)
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
                if (mats[i] != null && mats[i].name.StartsWith(material, StringComparison.Ordinal)) return i;
            return -1;
        }

        private void BuildLanes()
        {
            int l = 0;
            for (int axis = 0; axis < 2; axis++)
                foreach (float line in new[] { -Road, Road })
                    foreach (float sign in new[] { 1f, -1f })
                    {
                        var dir = axis == 0 ? new Vector3(sign, 0, 0) : new Vector3(0, 0, sign);
                        var right = Vector3.Cross(Vector3.up, dir);
                        var linePoint = axis == 0 ? new Vector3(0, GroundY, line) : new Vector3(line, GroundY, 0);
                        _origin[l] = linePoint + right * LaneOffset; _dir[l] = dir; _axis[l] = axis;
                        // The two cross streets this lane passes, as Along values, in travel order.
                        float a = Vector3.Dot(new Vector3(-Road, 0, -Road) - _origin[l], dir);
                        float b = Vector3.Dot(new Vector3(Road, 0, Road) - _origin[l], dir);
                        _crossings[l] = a < b ? new[] { a, b } : new[] { b, a };
                        l++;
                    }
        }

        public const int Lanes = LaneCount;

        /// <summary>A lane's frame: the point at Along = 0 and the unit travel direction. Shared with
        /// the scene builder so placement and runtime use one geometry.</summary>
        public void LaneFrame(int lane, out Vector3 origin, out Vector3 dir)
        {
            if (_dir[0] == Vector3.zero) BuildLanes();
            origin = _origin[lane]; dir = _dir[lane];
        }

        /// <summary>The lane a placed vehicle stands in, by position alone (the two lanes of a road
        /// are 2 x LaneOffset apart and parked cars sit 2 m further out), or -1.</summary>
        public int LaneAt(Vector3 at, float tolerance = 1.2f)
        {
            if (_dir[0] == Vector3.zero) BuildLanes();
            int best = -1; float bestD = float.MaxValue;
            for (int l = 0; l < LaneCount; l++)
            {
                var off = at - _origin[l]; off -= _dir[l] * Vector3.Dot(off, _dir[l]); off.y = 0;
                if (off.magnitude < bestD) { bestD = off.magnitude; best = l; }
            }
            return bestD < tolerance ? best : -1;
        }

        /// <summary>0 all red, 1 green, 2 amber, for road axis 0 (X) or 1 (Z).</summary>
        private int PhaseFor(int axis)
        {
            float half = GreenSeconds + AmberSeconds + AllRedSeconds;
            float t = Mathf.Repeat(_clock, CycleSeconds);
            bool mine = axis == 0 ? t < half : t >= half;
            if (!mine) return 0;
            float u = t % half;
            return u < GreenSeconds ? 1 : u < GreenSeconds + AmberSeconds ? 2 : 0;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || Drivers.Length == 0) return;
            dt = Mathf.Min(dt, .05f);
            _clock += dt;
            SortLanes();
            for (int l = 0; l < LaneCount; l++)
            {
                int phase = PhaseFor(_axis[l]);
                for (int k = 0; k < _orderCount[l]; k++) Step(_order[l][k], l, k, phase, dt);
            }
            UpdateSignals();
        }

        // Insertion sort by Along, per lane: a handful of vehicles, almost sorted from last frame.
        private void SortLanes()
        {
            for (int l = 0; l < LaneCount; l++) _orderCount[l] = 0;
            for (int i = 0; i < Drivers.Length; i++)
            {
                var d = Drivers[i];
                if (d.Body == null || d.Lane < 0 || d.Lane >= LaneCount) continue;
                var list = _order[d.Lane]; int c = _orderCount[d.Lane]++;
                int j = c - 1;
                while (j >= 0 && Drivers[list[j]].Along > d.Along) { list[j + 1] = list[j]; j--; }
                list[j + 1] = i;
            }
        }

        private void Step(int i, int lane, int rank, int phase, float dt)
        {
            var d = Drivers[i];
            float v = _speed[i], v0 = d.Cruise;
            const float amax = 1.6f, bcomf = 2.8f, s0 = 2.2f, headway = 1.1f;
            // Gap to the vehicle ahead (sorted ascending, so the next index is ahead).
            float gap = float.MaxValue, vAhead = v;
            if (rank + 1 < _orderCount[lane])
            {
                int a = _order[lane][rank + 1];
                gap = Drivers[a].Along - d.Along - (Drivers[a].Length + d.Length) * .5f;
                vAhead = _speed[a];
            }
            // A red or amber light ahead is a stopped "vehicle" at the stop line, unless an amber
            // is too close to stop for comfortably, in which case the vehicle carries on through.
            if (phase != 1)
                foreach (float cross in _crossings[lane])
                {
                    float line = cross - StopBack;
                    float toLine = line - d.Along - d.Length * .5f;
                    if (toLine < -.5f) continue;
                    bool committed = phase == 2 && toLine < v * v / (2f * bcomf) * .8f;
                    if (!committed && toLine < gap) { gap = toLine + s0; vAhead = 0f; }
                    break;
                }
            // The jeepney stop: pull in, dwell, pull out.
            if (d.Jeepney && lane == StopLane)
            {
                float toStop = StopAlong - d.Along;
                if (_stopState[i] == 0 && toStop > 0 && toStop < 40f) _stopState[i] = 1;
                if (_stopState[i] == 1)
                {
                    if (toStop + s0 < gap) { gap = toStop + s0; vAhead = 0f; }
                    _shift[i] = Mathf.MoveTowards(_shift[i], StopShift * Mathf.Clamp01(1f - (toStop - 2f) / 14f), dt * 1.2f);
                    if (toStop < .6f && v < .3f) { _stopState[i] = 2; _dwell[i] = Range(StopDwell.x, StopDwell.y); }
                }
                else if (_stopState[i] == 2)
                {
                    gap = 0f; vAhead = 0f; _speed[i] = 0f; v = 0f;
                    _dwell[i] -= dt;
                    if (_dwell[i] <= 0f) _stopState[i] = 3;
                }
                else if (_stopState[i] == 3)
                {
                    _shift[i] = Mathf.MoveTowards(_shift[i], 0f, dt * .9f);
                    if (_shift[i] <= 0f) _stopState[i] = 4;
                }
            }
            float sStar = s0 + Mathf.Max(0f, v * headway + v * (v - vAhead) / (2f * Mathf.Sqrt(amax * bcomf)));
            float accel = amax * (1f - Mathf.Pow(v / Mathf.Max(v0, .1f), 4f)) - (gap < float.MaxValue ? amax * (sStar / Mathf.Max(gap, .1f)) * (sStar / Mathf.Max(gap, .1f)) : 0f);
            accel = Mathf.Clamp(accel, -7f, amax);
            if (_stopState[i] == 2) accel = 0f;
            v = Mathf.Max(0f, v + accel * dt);
            _speed[i] = v;
            d.Along += v * dt;
            _brake[i] = Mathf.MoveTowards(_brake[i], accel < -.6f || v < .2f ? 1f : 0f, dt * 6f);

            // Off the far end: re-enter at the near end when the entry is clear.
            if (d.Along > Extent + 4f)
            {
                float entry = -Extent - 4f;
                bool clear = true;
                for (int k = 0; k < _orderCount[lane]; k++)
                {
                    var o = Drivers[_order[lane][k]];
                    if (o != d && Mathf.Abs(o.Along - entry) < (o.Length + d.Length) * .5f + 6f) { clear = false; break; }
                }
                if (clear) { d.Along = entry; _speed[i] = d.Cruise * .8f; _stopState[i] = 0; _shift[i] = 0f; }
                else d.Along = Extent + 4f;
            }

            var right = Vector3.Cross(Vector3.up, _dir[lane]);
            var pos = _origin[lane] + _dir[lane] * d.Along + right * _shift[i];
            pos.y = GroundY;
            // A little nose dip under braking and squat under throttle: weight, not decoration.
            float pitch = Mathf.Clamp(-accel * .45f, -1.2f, 2.2f);
            var heading = Quaternion.LookRotation(_dir[lane], Vector3.up);
            var lean = Quaternion.AngleAxis(pitch, right);
            // Easing toward the kerb turns the nose a touch toward it.
            var steer = Quaternion.AngleAxis(_stopState[i] == 1 ? 6f : _stopState[i] == 3 ? -6f : 0f, Vector3.up);
            d.Body.SetPositionAndRotation(pos, lean * steer * heading * d.ModelOffset);
            ApplyBrakeLights(i);
        }

        private void ApplyBrakeLights(int i)
        {
            var renderers = _vehicleRenderers[i];
            if (renderers == null) return;
            for (int r = 0; r < renderers.Length; r++)
            {
                int slot = _brakeSlots[i][r];
                if (slot < 0) continue;
                renderers[r].GetPropertyBlock(_block, slot);
                _block.SetColor(EmissionId, _brakeBase[i][r] * Mathf.Lerp(.35f, 2.2f, _brake[i]));
                renderers[r].SetPropertyBlock(_block, slot);
            }
        }

        private void UpdateSignals()
        {
            int phase = PhaseFor(0) * 3 + PhaseFor(1);
            if (phase == _lastPhase) return;
            _lastPhase = phase;
            for (int s = 0; s < Signals.Length; s++)
            {
                var r = Signals[s];
                if (r == null || _lampSlots[s] == null) continue;
                int p = PhaseFor(_signalAxis[s]);
                int lit = p == 1 ? 2 : p == 2 ? 1 : 0;       // green slot, amber slot, else red
                for (int k = 0; k < 3; k++)
                {
                    int slot = _lampSlots[s][k];
                    if (slot < 0) continue;
                    var baseMat = r.sharedMaterials[slot];
                    if (baseMat == null) continue;
                    r.GetPropertyBlock(_block, slot);
                    bool on = k == lit;
                    _block.SetColor(EmissionId, on ? _lampEmission[k] * 2.4f : Color.black);
                    if (baseMat.HasProperty(ColorId)) _block.SetColor(ColorId, on ? baseMat.GetColor(ColorId) : baseMat.GetColor(ColorId) * .28f);
                    r.SetPropertyBlock(_block, slot);
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_dir[0] == Vector3.zero) BuildLanes();
            for (int l = 0; l < LaneCount; l++)
            {
                Gizmos.color = _axis[l] == 0 ? Color.yellow : Color.cyan;
                Gizmos.DrawLine(_origin[l] - _dir[l] * Extent, _origin[l] + _dir[l] * Extent);
                foreach (float c in _crossings[l]) Gizmos.DrawWireSphere(_origin[l] + _dir[l] * (c - StopBack), .6f);
            }
        }
#endif
    }
}
