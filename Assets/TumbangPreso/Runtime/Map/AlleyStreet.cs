using System;
using System.Text;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// THE QUIET ROAD outside the Eskinita Alley's arch (owner, 2026-10-09, looking over the low wall
    /// at a bare slab: "road and street life is missing. mostly the occasional tricycle, jeep, people
    /// walking around, taho vendors, children playing. not as bustling traffic as the city maps").
    /// This component is the VEHICLES only: now and then a tricycle, less often a jeepney, one lane
    /// each way, with long gaps where the road is empty. The people on the sidewalks are a
    /// <see cref="SidewalkLife"/> the same builder configures (Editor/MapKit/EskinitaAlleyStreetAuthor.cs).
    ///
    /// WHY NOT <see cref="KantoTraffic"/>: that is a car-following model for a street that is always
    /// full (every vehicle that leaves one end re-enters the other at once, and none can overtake,
    /// so a few vehicles on a short road become one convoy that passes every 25 s). A barangay road
    /// is a SCHEDULE: mostly nothing, then one tricycle. So this is the Arena's way
    /// (<see cref="TumbangPreso.Map.ArenaTraffic"/>): every vehicle's place is a CLOSED-FORM function
    /// of one clock, with no state at all.
    ///   * Each lane's time is cut into slots of <see cref="Slot"/> seconds. A hash of (seed, lane,
    ///     slot) says whether that slot sends a vehicle (<see cref="Chance"/>), which kind (by
    ///     <see cref="Kind.Weight"/>) and how late in the slot it sets off (up to <see cref="Jitter"/>).
    ///   * It appears at one end of the road (x = -End eastbound, +End westbound: beyond the houses
    ///     and the haze), drives the lane at its kind's steady speed and is put away at the other end.
    ///   * Starts in one lane are at least Slot - Jitter seconds apart, which is more than the
    ///     slowest kind loses to the fastest over the whole road, so nobody catches anybody.
    /// ⚠️ THE CLOCK: in a session, Netcode's server time (already shared, no message of ours), so
    /// every peer sees the same tricycle at the same place; offline this peer's own game clock from
    /// a random start, so a pause stops the road and two solo matches do not open alike.
    ///
    /// ⚠️⚠️ VISUAL ONLY. No collider, nothing networked, nothing gameplay reads, and nothing here
    /// comes nearer the play area than the near lane (its centre is 3.5 m outside the bounds wall at
    /// z +17.5). A vehicle that is not on the road is INACTIVE and parked far under the map, so the
    /// whole cost of an empty road is two hashes a lane a frame.
    /// </summary>
    public sealed class AlleyStreet : MonoBehaviour
    {
        [Serializable]
        public sealed class Kind
        {
            public string Name;
            /// <summary>Metres a second, steady.</summary>
            public float Speed = 5f;
            /// <summary>How often this kind is the one a slot sends, against the other kinds.</summary>
            public float Weight = 1f;
            /// <summary>The engine's shake: metres up and down, and how many times a second.</summary>
            public float Bob = .005f, BobRate = 9f;
            /// <summary>The model's own turn from "nose along +Z" (the kits model the nose along X).</summary>
            public Quaternion ModelOffset = Quaternion.identity;
            /// <summary>Four bodies: [lane * 2 + (slot is odd)]. Two a lane, because the vehicle of
            /// one slot may still be on the road when the next slot's sets off.</summary>
            public Transform[] Bodies = Array.Empty<Transform>();
        }

        public Kind[] Kinds = Array.Empty<Kind>();
        /// <summary>The road runs along X. A vehicle appears and is put away at x = -End and +End.</summary>
        public float End = 66f;
        /// <summary>Lane 0 drives toward +X, lane 1 toward -X. Right-hand traffic: lane 0 is the one
        /// on the right of +X, which is the lower z (Vector3.Cross(up, +X) is -Z).</summary>
        public float[] LaneZ = { 21f, 24.2f };
        public float[] LaneY = { -.92f, -.92f };
        /// <summary>Seconds a slot; the most a vehicle sets off after its slot begins; the share of slots that send one.</summary>
        public float Slot = 22f, Jitter = 6f;
        [Range(0f, 1f)] public float Chance = .34f;
        public int Seed = 20261009;

        private static readonly Vector3 Parked = new Vector3(0f, -500f, 0f);
        private double _clock;
        private bool[][] _used;
        private bool[][] _shown;

        private void Awake()
        {
            // Offline: somewhere in the first hour of the schedule, so no two solo matches open on the same road.
            _clock = (uint)Guid.NewGuid().GetHashCode() % 3600u;
        }

        private void Update()
        {
            var nm = Unity.Netcode.NetworkManager.Singleton;
            if (NetAuthority.IsNetworked && nm != null && nm.IsListening) _clock = nm.ServerTime.Time;
            else _clock += Time.deltaTime;
            Pose(_clock);
        }

        public double Clock => _clock;

        /// <summary>0 to 1 from (seed, lane, slot, which number is wanted). Integer arithmetic only,
        /// so every peer and every CPU gets the same road.</summary>
        private float Hash(int lane, long slot, int salt)
        {
            unchecked
            {
                uint h = (uint)Seed * 0x9E3779B1u;
                h ^= (uint)(lane + 1) * 0x85EBCA6Bu;
                h ^= (uint)slot * 0xC2B2AE35u;
                h ^= (uint)(slot >> 32) * 0x27D4EB2Fu;
                h ^= (uint)(salt + 1) * 0x165667B1u;
                h ^= h >> 15; h *= 0x2C1B3C6Du;
                h ^= h >> 12; h *= 0x297A2D39u;
                h ^= h >> 15;
                return (h & 0xFFFFFFu) / 16777216f;
            }
        }

        /// <summary>What slot `slot` of `lane` sends: false for nothing; else the kind and how
        /// many seconds after the slot begins it sets off.</summary>
        public bool Sends(int lane, long slot, out int kind, out float after)
        {
            kind = -1; after = 0f;
            if (Kinds.Length == 0 || Hash(lane, slot, 0) >= Chance) return false;
            float total = 0f;
            foreach (var k in Kinds) if (k != null) total += Mathf.Max(0f, k.Weight);
            if (total <= 0f) return false;
            float pick = Hash(lane, slot, 1) * total;
            for (int i = 0; i < Kinds.Length; i++)
            {
                if (Kinds[i] == null) continue;
                pick -= Mathf.Max(0f, Kinds[i].Weight);
                if (pick < 0f) { kind = i; break; }
            }
            if (kind < 0) kind = Kinds.Length - 1;
            after = Hash(lane, slot, 2) * Mathf.Clamp(Jitter, 0f, Slot);
            return Kinds[kind] != null;
        }

        /// <summary>Lane `lane`'s own time at `clock`: the two lanes' slots are half a slot apart.</summary>
        private double LaneTime(int lane, double clock) => clock + lane * Slot * .5;

        /// <summary>
        /// Puts every vehicle where it is at `clock` and puts away the ones that are not on the
        /// road. The whole component: Update calls it with the clock, the builder's motion sheet
        /// with times of its own.
        /// </summary>
        public void Pose(double clock)
        {
            if (Slot < 1f || LaneZ == null || LaneZ.Length < 2) return;
            if (_used == null || _used.Length != Kinds.Length)
            {
                _used = new bool[Kinds.Length][]; _shown = new bool[Kinds.Length][];
                for (int i = 0; i < Kinds.Length; i++)
                {
                    int n = Kinds[i] != null && Kinds[i].Bodies != null ? Kinds[i].Bodies.Length : 0;
                    _used[i] = new bool[n]; _shown[i] = new bool[n];
                    // Whatever the scene was saved with, the first frame decides.
                    for (int b = 0; b < n; b++) _shown[i][b] = Kinds[i].Bodies[b] != null && Kinds[i].Bodies[b].gameObject.activeSelf;
                }
            }
            for (int i = 0; i < _used.Length; i++) for (int b = 0; b < _used[i].Length; b++) _used[i][b] = false;

            for (int lane = 0; lane < 2; lane++)
            {
                double time = LaneTime(lane, clock);
                long now = (long)Math.Floor(time / Slot);
                float dir = lane == 0 ? 1f : -1f;
                for (int back = 0; back < 3; back++)
                {
                    long slot = now - back;
                    if (!Sends(lane, slot, out int kind, out float after)) continue;
                    var k = Kinds[kind];
                    float since = (float)(time - (slot * (double)Slot + after));
                    if (since < 0f || k.Speed <= .1f || since * k.Speed > 2f * End) continue;
                    int index = lane * 2 + (int)(((slot % 2) + 2) % 2);
                    if (k.Bodies == null || index >= k.Bodies.Length || k.Bodies[index] == null || _used[kind][index]) continue;
                    _used[kind][index] = true;
                    var body = k.Bodies[index];
                    float bob = Mathf.Sin((float)(clock % 1000.0) * k.BobRate * 2f * Mathf.PI + index) * k.Bob;
                    var at = new Vector3(dir * (since * k.Speed - End), LaneY[Mathf.Min(lane, LaneY.Length - 1)] + bob, LaneZ[lane]);
                    body.SetPositionAndRotation(at, Quaternion.LookRotation(new Vector3(dir, 0f, 0f), Vector3.up) * k.ModelOffset);
                    if (!_shown[kind][index]) { body.gameObject.SetActive(true); _shown[kind][index] = true; }
                }
            }

            for (int i = 0; i < _used.Length; i++)
                for (int b = 0; b < _used[i].Length; b++)
                {
                    if (_used[i][b] || !_shown[i][b]) continue;
                    var body = Kinds[i].Bodies[b];
                    _shown[i][b] = false;
                    if (body == null) continue;
                    // Far under the map as well as inactive: whatever reads its position (the engine sound) finds it out of reach.
                    body.position = Parked;
                    body.gameObject.SetActive(false);
                }
        }

        /// <summary>How many vehicles are on the road now (after a <see cref="Pose"/>).</summary>
        public int OnRoad
        {
            get
            {
                int n = 0;
                if (_shown != null) foreach (var row in _shown) foreach (bool s in row) if (s) n++;
                return n;
            }
        }

        /// <summary>The vehicle on the road nearest `to`, or null (the builder's motion sheet looks at it).</summary>
        public Transform Nearest(Vector3 to)
        {
            Transform best = null; float d = float.MaxValue;
            if (_shown == null) return null;
            for (int i = 0; i < _shown.Length; i++)
                for (int b = 0; b < _shown[i].Length; b++)
                {
                    if (!_shown[i][b] || Kinds[i].Bodies[b] == null) continue;
                    float sq = (Kinds[i].Bodies[b].position - to).sqrMagnitude;
                    if (sq < d) { d = sq; best = Kinds[i].Bodies[b]; }
                }
            return best;
        }

        /// <summary>One line: what is on the road and where (the builder's log).</summary>
        public string Describe()
        {
            var s = new StringBuilder();
            if (_shown != null)
                for (int i = 0; i < _shown.Length; i++)
                    for (int b = 0; b < _shown[i].Length; b++)
                    {
                        if (!_shown[i][b] || Kinds[i].Bodies[b] == null) continue;
                        var p = Kinds[i].Bodies[b].position;
                        s.Append(Kinds[i].Name).Append(b < 2 ? " east x " : " west x ").Append(p.x.ToString("0.0")).Append("; ");
                    }
            return s.Length == 0 ? "road empty" : s.ToString();
        }

        /// <summary>The next time at or after `from` (searched a second at a time, up to `within`
        /// seconds on) at which some vehicle is within `reach` metres of x = `x`; -1 for none.
        /// Closed form, no posing: the motion sheet uses it to find a moment worth looking at.</summary>
        public double NextPassing(double from, float x, float reach, float within)
        {
            for (double t = from; t <= from + within; t += 1.0)
                for (int lane = 0; lane < 2; lane++)
                {
                    double time = LaneTime(lane, t);
                    long now = (long)Math.Floor(time / Slot);
                    float dir = lane == 0 ? 1f : -1f;
                    for (int back = 0; back < 3; back++)
                    {
                        long slot = now - back;
                        if (!Sends(lane, slot, out int kind, out float after)) continue;
                        float since = (float)(time - (slot * (double)Slot + after));
                        if (since < 0f || since * Kinds[kind].Speed > 2f * End) continue;
                        if (Mathf.Abs(dir * (since * Kinds[kind].Speed - End) - x) <= reach) return t;
                    }
                }
            return -1.0;
        }
    }
}
