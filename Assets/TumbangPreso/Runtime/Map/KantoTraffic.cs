using System;
using System.Collections.Generic;
using TumbangPreso.Abilities;
using TumbangPreso.Visual;
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
    /// ⚠️ THE ONE EXCEPTION IS `HitsPlayers` (§ THE LIVE ROAD below), off unless a map's author
    /// switches it on: the Ilalim rebuild's Taft is crossed by players, and there every peer must
    /// see the same taxi at the same time, because the host decides who it hit.
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

        // ⚠️⚠️ § ROUTES, THE SECOND NETWORK (ILALIM-1.4, owner 2026-09-30: "make the live moving
        // cars"). The Ilalim ng Tulay rebuild is not a # grid: its court IS Taft Avenue, so Taft
        // carries cars only outside the play walls, and they turn into and out of Padre Faura and
        // G. Apacible (Editor/MapKit/IlalimLifeAuthor.cs). With `Routes` empty (Kanto) nothing
        // below changes and the grid drives exactly as before. With routes, a Driver's `Lane` is
        // its ROUTE index and `Along` is metres from the route's first point. Each route is a
        // polyline in world space (its own y is the road), may turn, obeys its own stop lines
        // (each tied to a signal axis, or to none: a closed road, the queue at the court), and
        // hands its vehicles on to `Next` at its end (or re-enters them at its own start when
        // clear, the grid's rule). Car following reads the vehicle ahead on the same route by
        // `Along`, and ANY vehicle ahead within `FollowReach` of the path by world position, so
        // two routes that share a street keep their gaps without a merge model. Turns cap the
        // speed from their radius (`TurnGrip`, m/s^2 sideways).
        [Serializable] public sealed class Route
        {
            public string Name;
            public Vector3[] Points = Array.Empty<Vector3>();
            public float[] StopAlong = Array.Empty<float>();   // ascending, metres along the route
            public int[] StopAxis = Array.Empty<int>();        // per stop: 0 along X, 1 along Z, -1 always red, -2 yield to Next
            public int Next = -1;                               // -1: re-enter at this route's start
            public float NextAlong;                             // where on Next a vehicle continues
        }
        public Route[] Routes = Array.Empty<Route>();
        public float TurnGrip = 2.2f, FollowReach = 1.8f;
        // The material names of a signal head's three lamps and of a vehicle's brake lamp. A lamp
        // whose material carries no emission of its own (the Ilalim kits light only the red lens)
        // takes the matching `LampGlow` / `BrakeGlow` instead; left empty or black, nothing changes.
        public string[] LampMaterials = { "signal_red", "signal_amber", "signal_green" };
        public string BrakeMaterial = "signal_red";
        public Color[] LampGlow = Array.Empty<Color>();
        public Color BrakeGlow = Color.black;

        // ⚠️⚠️ § THE LIVE ROAD (owner 2026-10-04, moving the Ilalim court off Taft into the campus
        // lot: "make it so the players can still cross over and they ragdoll when they get hit by a
        // car"). OFF BY DEFAULT: Kanto's traffic never meets a player and stays exactly as it was.
        // Routes mode only; `IlalimLifeAuthor` switches it on.
        //   * THE HIT is the game's own knockdown: `CharacterMotor.ApplyTrip` (the fall
        //     `StreetTripHazard` gives) and a throw along the car's travel through
        //     `CharacterMotor.ApplyResolvedImpact`. Contact is resolved ONCE BY THE HOST
        //     (`NetAuthority.ShouldResolve`); the trip reaches a remote owner in the host's status
        //     stream and the throw through `MatchRpc.BroadcastImpact`, the message every ability's
        //     knockback already uses. Nothing new is on the wire. The sound goes out through
        //     `NetCue`; the popup and the dust are drawn by each peer when it sees the fall.
        //   * ⚠️ SO THE HOST'S TAXI HAS TO BE EVERYBODY'S TAXI. A client run from its own clock
        //     would be thrown by a car it cannot see and walk through the one it can. With this on,
        //     the traffic is stepped in FIXED steps from the session's shared clock (Netcode's
        //     server time, already synchronised, no message of ours), from the authored starts and a
        //     seed taken from that clock, so every peer computes the same street. See `UpdateLocked`.
        //   * A car below `HitMinSpeed` (creeping up to a red light, waiting in the queue) hits
        //     nobody: players cross between the stopped cars.
        //   * A player who is immune to stuns is not felled (both calls refuse), and one inside the
        //     body's own get-up grace (`CharacterMotor.IsTripImmune`) is left alone, as every hazard
        //     leaves him: he was put down in the lane and has to be able to walk out of it.
        //   * BOTS read `HazardMap`: each moving vehicle carries one `HazardVolume` disc over its nose
        //     and the road just ahead of it, so a bot's walk to its goal bends round a passing car
        //     the way it bends round a pier. A stopped vehicle carries none.
        public bool HitsPlayers;
        /// <summary>Slower than this a vehicle fells nobody: a walk's pace, a car easing up to a stop line.</summary>
        public float HitMinSpeed = 2f;
        /// <summary>The throw along the car's travel, m/s, at `HitMinSpeed` and at 9 m/s (a car's
        /// cruise) and over; `Balance.MaxKnockbackSpeed` (16) still caps what the body takes.</summary>
        public Vector2 HitThrow = new Vector2(15f, 25f);   // a launch (CharacterMotor.AsLaunch): 4 to 10 m of slide
        /// <summary>Thrown this much to the side the player stood on, m/s, so he lands out of the lane.</summary>
        public float HitAside = 5f;
        /// <summary>The lift, m/s: under `Balance.MaxKnockbackLift` (7), so a hero's own launch stays the higher one.</summary>
        public float HitLift = 11f;      // about 3 m up, a second in the air
        /// <summary>Seconds down: `StreetTripHazard.TripDuration`, the game's one fall.</summary>
        public float HitTrip = 2.5f;
        /// <summary>One car cannot hit the same player again within this, seconds: its own length passing over him.</summary>
        public float HitCooldown = 1.5f;
        /// <summary>A player's body, metres across the ground from his centre (the cast's capsule).</summary>
        public float HitBodyRadius = .35f;
        public string HitPopup = "BEEP BEEP!";

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

        // ---- § THE LIVE ROAD: the shared clock's fixed steps, each vehicle's box, the bots' discs.
        /// <summary>The fixed step, seconds. 30 a second: a car at 9 m/s moves 0.3 m a step, and the
        /// pose drawn between two steps runs on at the vehicle's speed, so nothing stutters.</summary>
        private const float LockStep = 1f / 30f;
        /// <summary>The street is re-seeded from the shared clock every 20 minutes, so a peer that
        /// joins late has at most that much to step through (36000 steps; see `LockCatchUpSeconds`
        /// for how they are spread). ⚠️ At the boundary every vehicle returns to its authored start on
        /// every peer at once: one visible reset in a match that runs past it.</summary>
        private const double LockEpochSeconds = 1200.0;
        /// <summary>
        /// ⚠️⚠️ THE CATCH-UP IS BOXED IN TIME, NOT IN STEPS (owner, 2026-10-04: the game is "primarily
        /// not laggy for the server host but it is for players joining too"). It was 600 steps a
        /// frame whatever they cost: each step is every vehicle against every other, about 20
        /// microseconds for the 28 of the Ilalim street on the development PC and several times that
        /// on a weak one, so a joining laptop spent 40 to 60 ms of every frame on a street it
        /// could not see yet, for up to 60 frames. Now a frame spends at most this long on it,
        /// and never fewer than `LockCatchUpFloor` steps (two seconds of street a frame, so it
        /// always gains on the clock). The steps themselves, their order and their arithmetic are
        /// untouched: every peer still comes out with the same street, only later or sooner.
        /// `HubLoading` holds the match curtain until the street is caught up (`CatchingUp`, with
        /// its own time limit) and says so through `Covered`, which lets a frame spend more.
        /// </summary>
        private const double LockCatchUpSeconds = 0.004, LockCatchUpCoveredSeconds = 0.020;
        private const int LockCatchUpFloor = 60;
        /// <summary>Further behind the clock than this, in steps (1.5 s), the vehicles are not
        /// drawn or moved until the street is caught up: nobody may see or touch a street that is
        /// minutes old and running at a hundred times its speed.</summary>
        private const int LockFarBehind = 45;
        /// <summary>Set by the loading curtain while it covers the scene: nobody is playing, so
        /// the catch-up may take most of a frame.</summary>
        public static bool Covered;
        private static int _catchingUp;
        /// <summary>True while any live road in the scene is still stepping through what it
        /// missed. The loading curtain waits on it, so the first frame a player sees is the
        /// street every other peer sees.</summary>
        public static bool CatchingUp => _catchingUp > 0;
        private bool _counted;
        private const long NoEpoch = long.MinValue, SoloEpoch = long.MinValue + 1;
        private long _lockEpoch = NoEpoch, _lockSteps;
        private double _lockTime;
        private bool _quiet, _caughtUp = true;
        private int[] _lane0;
        private float[] _along0, _pitch;
        private Vector3[] _poseAt, _poseDir;    // per driver: where its body was last drawn, and its travel direction
        private Vector4[] _box;                 // per driver, about its body's origin: centre forward, centre right, half length, half width
        private float[] _boxTop;                // per driver: its roof over the road
        private GameObject[] _hazards;
        private Transform[] _solids;            // per driver: its body's box as a real collider (the live road only)
        private readonly Dictionary<CharacterMotor, float> _nextHit = new Dictionary<CharacterMotor, float>();
        private readonly Dictionary<CharacterMotor, float> _nextShown = new Dictionary<CharacterMotor, float>();
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private void Start()
        {
            _random = new System.Random(Guid.NewGuid().GetHashCode());
            BuildLanes();
            if (RouteMode) BuildRoutes();
            int n = Drivers.Length;
            _speed = new float[n]; _shift = new float[n]; _dwell = new float[n]; _brake = new float[n]; _stopState = new int[n];
            _pitch = new float[n]; _poseAt = new Vector3[n]; _poseDir = new Vector3[n];
            _lane0 = new int[n]; _along0 = new float[n];
            for (int i = 0; i < n; i++) { _lane0[i] = Drivers[i].Lane; _along0[i] = Drivers[i].Along; _poseDir[i] = Vector3.forward; }
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
                    int slot = SlotOf(_vehicleRenderers[i][r], BrakeMaterial);
                    var mat = slot >= 0 ? _vehicleRenderers[i][r].sharedMaterials[slot] : null;
                    if (mat == null || !mat.HasProperty(EmissionId)) slot = -1;
                    else
                    {
                        var glow = mat.GetColor(EmissionId);
                        _brakeBase[i][r] = glow.maxColorComponent < .001f && BrakeGlow.maxColorComponent > 0f ? BrakeGlow : glow;
                    }
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
                _lampSlots[s] = new[] { SlotOf(r, LampMaterials[0]), SlotOf(r, LampMaterials[1]), SlotOf(r, LampMaterials[2]) };
                var f = r.transform.forward;
                _signalAxis[s] = Mathf.Abs(f.x) > Mathf.Abs(f.z) ? 0 : 1;
                for (int k = 0; k < 3; k++)
                {
                    int slot = _lampSlots[s][k];
                    if (slot >= 0 && r.sharedMaterials[slot].HasProperty(EmissionId))
                        _lampEmission[k] = r.sharedMaterials[slot].GetColor(EmissionId);
                    if (LampGlow != null && LampGlow.Length == 3) _lampEmission[k] = LampGlow[k];
                }
            }
            _clock = Range(0f, CycleSeconds);
            EaseIntoStops();
            if (LiveRoad) BuildLiveRoad();
        }

        /// <summary>A vehicle placed just short of a stop line starts no faster than it can stop at it.</summary>
        private void EaseIntoStops()
        {
            if (!RouteMode) return;
            for (int i = 0; i < Drivers.Length; i++)
            {
                var d = Drivers[i];
                if (d.Body == null || d.Lane < 0 || d.Lane >= Routes.Length) continue;
                foreach (float stop in Routes[d.Lane].StopAlong ?? Array.Empty<float>())
                {
                    float room = stop - d.Along - d.Length * .5f;
                    if (room < -.5f) continue;
                    _speed[i] = Mathf.Min(_speed[i], Mathf.Sqrt(2f * 2.8f * Mathf.Max(0f, room - 1f)));
                    break;
                }
            }
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

        // ---- read by the street sound (KantoStreetSound): nothing here changes the traffic.
        public int DriverCount => Drivers.Length;
        public Vector3 DriverPosition(int i) => Drivers[i].Body != null ? Drivers[i].Body.position : Vector3.zero;
        public float DriverSpeed(int i) => _speed != null ? _speed[i] : 0f;
        public float DriverCruise(int i) => Drivers[i].Cruise;
        /// <summary>The vehicle's model (its dressing group's name: sedan_red, jeepney, tricycle...).</summary>
        public string DriverModel(int i) => Drivers[i].Body != null && Drivers[i].Body.parent != null ? Drivers[i].Body.parent.name : "";
        /// <summary>True while the vehicle waits at a light, in a queue, or at the jeepney stop.</summary>
        public bool DriverWaiting(int i) => _speed != null && _speed[i] < .3f;
        /// <summary>The road axis the vehicle drives along: 0 = X, 1 = Z (matches GreenStarted).</summary>
        public int DriverAxis(int i) => RouteMode
            ? (_head != null && i < _head.Length && Mathf.Abs(_head[i].x) > Mathf.Abs(_head[i].z) ? 0 : 1)
            : Drivers[i].Lane >= 4 ? 1 : 0;
        /// <summary>Fired when a road axis (0 = along X, 1 = along Z) turns green.</summary>
        public event Action<int> GreenStarted;

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
            if (LiveRoad && Application.isPlaying && Drivers.Length > 0)
            {
                // The live road keeps its own clock (UpdateLocked): in a session it is the shared one,
                // which does not stop for a held frame, so the street runs on behind a cutscene
                // instead of jumping when it ends. Nobody is hit while the world is held.
                UpdateLocked(Mathf.Clamp(dt, 0f, .05f));
                UpdateHazards();
                if (dt > 0f && !PresentationClock.Held) UpdateHits();
                return;
            }
            if (dt <= 0f || Drivers.Length == 0) return;
            dt = Mathf.Min(dt, .05f);
            _clock += dt;
            if (RouteMode) { UpdateRoutes(dt); UpdateSignals(); return; }
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
            if (_lastPhase >= 0)
                for (int axis = 0; axis < 2; axis++)
                {
                    int was = axis == 0 ? _lastPhase / 3 : _lastPhase % 3, now = PhaseFor(axis);
                    if (now == 1 && was != 1) GreenStarted?.Invoke(axis);
                }
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

        // ------------------------------------------------------------------ routes (§ ROUTES)

        private bool RouteMode => Routes != null && Routes.Length > 0;
        private float[][] _cum, _cap;           // per route: metres at each point, the turn speed cap there
        private Vector3[] _pos, _head;          // per driver, this frame: position and flat heading

        private void BuildRoutes()
        {
            _cum = new float[Routes.Length][]; _cap = new float[Routes.Length][];
            for (int r = 0; r < Routes.Length; r++)
            {
                var p = Routes[r].Points ?? Array.Empty<Vector3>();
                int n = p.Length;
                _cum[r] = new float[n]; _cap[r] = new float[n];
                for (int k = 1; k < n; k++) _cum[r][k] = _cum[r][k - 1] + Vector3.Distance(p[k - 1], p[k]);
                for (int k = 0; k < n; k++)
                {
                    _cap[r][k] = float.MaxValue;
                    if (k == 0 || k == n - 1) continue;
                    var a = p[k] - p[k - 1]; var b = p[k + 1] - p[k]; a.y = 0; b.y = 0;
                    float angle = Vector3.Angle(a, b) * Mathf.Deg2Rad;
                    if (angle < .02f) continue;
                    // The radius of the arc these two chords sample: chord / turned angle.
                    float radius = Mathf.Min(a.magnitude, b.magnitude) / angle;
                    _cap[r][k] = Mathf.Sqrt(TurnGrip * Mathf.Max(radius, .5f));
                }
            }
        }

        private float RouteLength(int r) => _cum[r].Length > 0 ? _cum[r][_cum[r].Length - 1] : 0f;

        /// <summary>The index of the point that starts the segment holding `along` (clamped).</summary>
        private int SegmentAt(int r, float along)
        {
            var cum = _cum[r]; int lo = 0, hi = cum.Length - 1;
            if (hi <= 0) return 0;
            if (along <= 0f) return 0;
            if (along >= cum[hi]) return hi - 1;
            while (hi - lo > 1) { int mid = (lo + hi) >> 1; if (cum[mid] <= along) lo = mid; else hi = mid; }
            return lo;
        }

        /// <summary>A point on a route; before its start and past its end it runs on straight.</summary>
        public Vector3 RoutePoint(int r, float along)
        {
            if (_cum == null) BuildRoutes();
            var p = Routes[r].Points; var cum = _cum[r]; int n = p.Length;
            if (n == 0) return Vector3.zero;
            if (n == 1) return p[0];
            int k = SegmentAt(r, along);
            float span = cum[k + 1] - cum[k];
            if (span < 1e-4f) return p[k];
            return p[k] + (p[k + 1] - p[k]) * ((along - cum[k]) / span);
        }

        /// <summary>The route's flat travel direction at `along`, smoothed over 2.4 m so a car
        /// turns through an arc's corners instead of snapping at each.</summary>
        public Vector3 RouteHeading(int r, float along)
        {
            var d = RoutePoint(r, along + 1.2f) - RoutePoint(r, along - 1.2f); d.y = 0;
            return d.sqrMagnitude > 1e-6f ? d.normalized : Vector3.forward;
        }

        /// <summary>Metres along a route of the point on it nearest `at` (for the builder).</summary>
        public float RouteAlong(int r, Vector3 at, out float distance)
        {
            if (_cum == null) BuildRoutes();
            var p = Routes[r].Points; float best = float.MaxValue, along = 0f;
            for (int k = 0; k + 1 < p.Length; k++)
            {
                var ab = p[k + 1] - p[k]; ab.y = 0; float len2 = ab.sqrMagnitude;
                var ap = at - p[k]; ap.y = 0;
                float t = len2 > 1e-8f ? Mathf.Clamp01(Vector3.Dot(ap, ab) / len2) : 0f;
                float dist = (ap - ab * t).magnitude;
                if (dist < best) { best = dist; along = _cum[r][k] + t * Mathf.Sqrt(len2); }
            }
            distance = best;
            return along;
        }

        private void UpdateRoutes(float dt)
        {
            int n = Drivers.Length;
            if (_pos == null || _pos.Length != n) { _pos = new Vector3[n]; _head = new Vector3[n]; }
            for (int i = 0; i < n; i++)
            {
                var d = Drivers[i];
                if (d.Body == null || d.Lane < 0 || d.Lane >= Routes.Length) continue;
                _pos[i] = RoutePoint(d.Lane, d.Along); _head[i] = RouteHeading(d.Lane, d.Along);
            }
            for (int i = 0; i < n; i++)
            {
                var d = Drivers[i];
                if (d.Body == null || d.Lane < 0 || d.Lane >= Routes.Length) continue;
                StepRoute(i, dt);
            }
        }

        private void StepRoute(int i, float dt)
        {
            var d = Drivers[i]; int r = d.Lane; var route = Routes[r];
            float v = _speed[i];
            const float amax = 1.6f, bcomf = 2.8f, s0 = 2.2f, headway = 1.1f;
            float gap = float.MaxValue, vAhead = v;
            for (int j = 0; j < Drivers.Length; j++)
            {
                var o = Drivers[j];
                if (j == i || o.Body == null || o.Lane < 0 || o.Lane >= Routes.Length) continue;
                float g;
                if (o.Lane == r)
                {
                    float da = o.Along - d.Along;
                    if (da <= 0f || da > 90f) continue;
                    g = da - (o.Length + d.Length) * .5f;
                }
                else
                {
                    var off = _pos[j] - _pos[i]; off.y = 0f;
                    float ahead = Vector3.Dot(off, _head[i]);
                    if (ahead <= 0f || ahead > 60f) continue;
                    g = ahead - (o.Length + d.Length) * .5f;
                    // Just ahead, a body counts as far as it reaches across this lane (a bus in a U-turn
                    // sweeps both lanes); further out, only what is in the lane.
                    float reach = g < 6f ? FollowReach + .8f + .5f * o.Length * Mathf.Abs(Vector3.Cross(_head[i], _head[j]).y) : FollowReach;
                    if ((off - _head[i] * ahead).magnitude > reach) continue;
                    // Oncoming and crossing vehicles are the signals' business, not a gap, unless one
                    // is already across this vehicle's path just ahead (a U-turn, a late turner).
                    if (Vector3.Dot(_head[i], _head[j]) < .2f && g > 6f) continue;
                }
                if (g < gap) { gap = g; vAhead = _speed[j]; }
            }
            // The first stop line ahead: through on green, a stopped "vehicle" on red, and on amber
            // only if there is room to stop comfortably. Axis -1 never turns green.
            var stops = route.StopAlong ?? Array.Empty<float>();
            for (int k = 0; k < stops.Length; k++)
            {
                float toLine = stops[k] - d.Along - d.Length * .5f;
                if (toLine < -.5f) continue;
                int axis = route.StopAxis != null && k < route.StopAxis.Length ? route.StopAxis[k] : -1;
                int phase = axis == -2 ? (NextClear(route, i) ? 1 : 0) : axis < 0 ? 0 : PhaseFor(axis);
                if (phase == 1) break;
                bool committed = phase == 2 && toLine < v * v / (2f * bcomf) * .8f;
                if (!committed && toLine + s0 < gap) { gap = toLine + s0; vAhead = 0f; }
                break;
            }
            // Turns: the fastest speed from which each capped point ahead can still be met.
            float v0 = d.Cruise, look = v * v / (2f * bcomf) + 6f;
            var cum = _cum[r]; var cap = _cap[r];
            for (int k = SegmentAt(r, d.Along); k < cum.Length && cum[k] - d.Along <= look; k++)
            {
                if (cap[k] == float.MaxValue) continue;
                float dist = Mathf.Max(0f, cum[k] - d.Along);
                v0 = Mathf.Min(v0, Mathf.Sqrt(cap[k] * cap[k] + 2f * bcomf * dist));
            }
            v0 = Mathf.Max(v0, .5f);
            float sStar = s0 + Mathf.Max(0f, v * headway + v * (v - vAhead) / (2f * Mathf.Sqrt(amax * bcomf)));
            // ⚠️ The fourth power by hand, not Mathf.Pow: the live road is stepped on every peer and
            // has to come out the same on each, and `pow` is the C runtime's, which differs by CPU.
            float pace = v / v0; pace *= pace; pace *= pace;
            float accel = amax * (1f - pace) - (gap < float.MaxValue ? amax * (sStar / Mathf.Max(gap, .1f)) * (sStar / Mathf.Max(gap, .1f)) : 0f);
            accel = Mathf.Clamp(accel, -7f, amax);
            v = Mathf.Max(0f, v + accel * dt);
            d.Along += v * dt;
            // ⚠️ A line that never turns green (the closure) is never crossed, whatever the speed.
            for (int k = 0; k < stops.Length; k++)
            {
                int axis = route.StopAxis != null && k < route.StopAxis.Length ? route.StopAxis[k] : -1;
                if (axis != -1 || d.Along - v * dt + d.Length * .5f > stops[k] + .5f) continue;
                if (d.Along + d.Length * .5f > stops[k]) { d.Along = stops[k] - d.Length * .5f; v = 0f; }
                break;
            }
            _speed[i] = v;
            _brake[i] = Mathf.MoveTowards(_brake[i], accel < -.6f || v < .2f ? 1f : 0f, dt * 6f);

            float length = RouteLength(r);
            if (route.Next >= 0 && route.Next < Routes.Length && route.Next != r)
            {
                // Handed on, speed kept: the next route starts where this one ends.
                if (d.Along >= length) { d.Along = route.NextAlong + (d.Along - length); d.Lane = r = route.Next; }
            }
            else if (d.Along > length + 4f)
            {
                // Off the far end: back in at the start (out of sight) when the entry is clear.
                if (EntryClear(r, -4f, i)) { d.Along = -4f; _speed[i] = d.Cruise * .8f; }
                else d.Along = length + 4f;
            }

            _pitch[i] = Mathf.Clamp(-accel * .45f, -1.2f, 2.2f);
            // The live road steps many times a frame and draws once (UpdateLocked).
            if (!_quiet) Pose(i, 0f);
        }

        /// <summary>Draws a vehicle on its route, `lead` seconds on from its last step at its own speed.</summary>
        private void Pose(int i, float lead)
        {
            var d = Drivers[i]; int r = d.Lane;
            float along = d.Along + _speed[i] * lead;
            var pos = RoutePoint(r, along);
            var dir = RouteHeading(r, along);
            var right = Vector3.Cross(Vector3.up, dir);
            d.Body.SetPositionAndRotation(pos, Quaternion.AngleAxis(_pitch[i], right) * Quaternion.LookRotation(dir, Vector3.up) * d.ModelOffset);
            ApplyBrakeLights(i);
            _poseAt[i] = pos; _poseDir[i] = dir;
        }

        /// <summary>A yield line (axis -2): clear when nothing on the route this one joins is within
        /// 30 m before, or 10 m past, the point where it joins (a U-turn into a live lane).</summary>
        private bool NextClear(Route route, int self)
        {
            if (route.Next < 0) return true;
            for (int j = 0; j < Drivers.Length; j++)
            {
                var o = Drivers[j];
                if (j == self || o.Body == null || o.Lane != route.Next) continue;
                if (o.Along > route.NextAlong - 30f && o.Along < route.NextAlong + 10f) return false;
            }
            return true;
        }

        private bool EntryClear(int r, float along, int self)
        {
            var at = RoutePoint(r, along);
            for (int j = 0; j < Drivers.Length; j++)
            {
                var o = Drivers[j];
                if (j == self || o.Body == null) continue;
                var off = o.Body.position - at; off.y = 0f;
                if (off.magnitude < (o.Length + Drivers[self].Length) * .5f + 12f) return false;
                // ⚠️ Nobody re-enters a route while a vehicle joining it has passed its yield line
                // (the bus in the S2 U-turn). A car let in behind a turning bus met it nose to nose
                // mid-turn, each braking for the other, and both stood there for good (v11 probe).
                if (o.Lane >= 0 && o.Lane < Routes.Length && o.Lane != r && Routes[o.Lane].Next == r && PastYield(o, Routes[o.Lane]))
                    return false;
            }
            return true;
        }

        private static bool PastYield(Driver o, Route route)
        {
            var stops = route.StopAlong ?? Array.Empty<float>();
            for (int k = 0; k < stops.Length; k++)
                if (route.StopAxis != null && k < route.StopAxis.Length && route.StopAxis[k] == -2 && o.Along + o.Length * .5f > stops[k] - .5f)
                    return true;
            return false;
        }

        // ------------------------------------------------------------------ the live road (§ THE LIVE ROAD)

        private bool LiveRoad => HitsPlayers && RouteMode;

        /// <summary>Each vehicle's box, measured once from its meshes in its own travel frame (the
        /// body is rigid, so the box rides with it), and one hazard disc per vehicle for the bots.</summary>
        private void BuildLiveRoad()
        {
            int n = Drivers.Length;
            _box = new Vector4[n]; _boxTop = new float[n];
            for (int i = 0; i < n; i++)
            {
                var d = Drivers[i];
                _box[i] = new Vector4(0f, 0f, d.Length * .5f, .9f); _boxTop[i] = 1.6f;
                if (d.Body == null) continue;
                var f = d.Body.rotation * Quaternion.Inverse(d.ModelOffset) * Vector3.forward; f.y = 0f;
                f = f.sqrMagnitude > 1e-6f ? f.normalized : Vector3.forward;
                var right = Vector3.Cross(Vector3.up, f);
                var origin = d.Body.position;
                _poseAt[i] = origin; _poseDir[i] = f;
                float f0 = float.MaxValue, f1 = float.MinValue, r0 = float.MaxValue, r1 = float.MinValue, top = float.MinValue;
                foreach (var filter in d.Body.GetComponentsInChildren<MeshFilter>())
                {
                    if (filter.sharedMesh == null) continue;
                    var b = filter.sharedMesh.bounds; var m = filter.transform.localToWorldMatrix;
                    for (int c = 0; c < 8; c++)
                    {
                        var corner = b.center + Vector3.Scale(b.extents, new Vector3((c & 1) == 0 ? -1f : 1f, (c & 2) == 0 ? -1f : 1f, (c & 4) == 0 ? -1f : 1f));
                        var p = m.MultiplyPoint3x4(corner) - origin;
                        float a = Vector3.Dot(p, f), s = Vector3.Dot(p, right);
                        f0 = Mathf.Min(f0, a); f1 = Mathf.Max(f1, a); r0 = Mathf.Min(r0, s); r1 = Mathf.Max(r1, s); top = Mathf.Max(top, p.y);
                    }
                }
                if (f1 <= f0) continue;
                _box[i] = new Vector4((f0 + f1) * .5f, (r0 + r1) * .5f, (f1 - f0) * .5f, (r1 - r0) * .5f);
                _boxTop[i] = top;
            }
            // Outside Play (the author's probes call Start by hand) nothing is added to the scene.
            if (!Application.isPlaying) return;
            _hazards = new GameObject[n];
            for (int i = 0; i < n; i++)
            {
                if (Drivers[i].Body == null) continue;
                var go = new GameObject("Road hazard " + i);
                go.transform.SetParent(transform, false);
                // Its half width and a step's room, or half its length up to 3 m; a bot ignores a disc over 4 (AiTuning.HazardAvoidMaxRadius).
                HazardVolume.Attach(go, Mathf.Max(_box[i].w + .6f, Mathf.Min(_box[i].z, 3f)), -1);
                go.SetActive(false);
                _hazards[i] = go;
            }
            // ⚠️ THE VEHICLES ARE SOLID ON THE LIVE ROAD (owner, 2026-10-04, first play of the lot
            // court: "the cars are just pass through ... so u can go through them when theyre not
            // moving"). The hit above is a test, not a body: a queued car at a red light was air.
            // Each vehicle now carries its measured box as a kinematic collider that rides with
            // it, so a stopped car blocks a walk and a throw, and a roof can be stood on. A MOVING
            // car still fells whoever it reaches: the hit's box is this one grown by a body's
            // radius, so the fall lands before the two overlap.
            _solids = new Transform[n];
            for (int i = 0; i < n; i++)
            {
                if (Drivers[i].Body == null) continue;
                var go = new GameObject("Road solid " + i);
                go.transform.SetParent(transform, false);
                var body = go.AddComponent<Rigidbody>();
                body.isKinematic = true; body.useGravity = false;
                var box = go.AddComponent<BoxCollider>();
                float top = Mathf.Max(_boxTop[i], .6f);
                box.center = new Vector3(_box[i].y, top * .5f, _box[i].x);
                box.size = new Vector3(_box[i].w * 2f, top, _box[i].z * 2f);
                _solids[i] = go.transform;
            }
            UpdateSolids();
        }

        private void UpdateSolids()
        {
            if (_solids == null) return;
            for (int i = 0; i < _solids.Length; i++)
            {
                var t = _solids[i];
                if (t == null) continue;
                t.SetPositionAndRotation(_poseAt[i], Quaternion.LookRotation(_poseDir[i], Vector3.up));
            }
            // A held opening or hitstop has no fixed step to refresh these
            // moved boxes. Keep queries at the visible vehicles immediately,
            // including creation rather than leaving phantom solids at origin.
            if (!_solidsQueryable || PresentationClock.Held || Time.deltaTime <= 0f)
            {
                Physics.SyncTransforms();
                _solidsQueryable = true;
            }
        }

        private bool _solidsQueryable;

        /// <summary>
        /// The live road's step. ⚠️ EVERY PEER MUST COME OUT WITH THE SAME STREET, so nothing here
        /// reads this peer's frame time once a session is live: the street's age is the shared
        /// clock's (Netcode's synchronised server time), cut into epochs; at an epoch's start every
        /// vehicle stands at its authored start with a speed drawn from a seed that is the epoch's
        /// number; from there it is `LockStep` at a time, the same arithmetic on every peer. A peer
        /// that arrives late steps through what it missed, `LockCatchUp` steps a frame, and hits
        /// nobody until it has caught up. Offline there is nobody to agree with: the same steps, on
        /// this peer's own game clock (so a pause stops the street), from a seed of its own.
        /// ⚠️ CLOSE, NOT BIT-EXACT ACROSS CPU FAMILIES: the turn caps come from an arc cosine at
        /// Start. A difference there is a last digit of a speed, a few centimetres over an epoch.
        /// </summary>
        private void UpdateLocked(float dt)
        {
            var nm = Unity.Netcode.NetworkManager.Singleton;
            bool shared = NetAuthority.IsNetworked && nm != null && nm.IsListening;
            if (shared)
            {
                double now = nm.ServerTime.Time;
                long epoch = (long)Math.Floor(now / LockEpochSeconds);
                if (epoch != _lockEpoch) { Reseed(unchecked((int)(epoch * 7919L) + 17)); _lockEpoch = epoch; }
                _lockTime = now - epoch * LockEpochSeconds;
            }
            else
            {
                // A session that ended under this peer keeps the street it had; only a fresh scene seeds.
                if (_lockEpoch == NoEpoch) { Reseed(Guid.NewGuid().GetHashCode()); _lockTime = 0.0; }
                _lockEpoch = SoloEpoch;
                _lockTime += dt;
            }
            // The shared clock is an estimate on a client and may step back a hair: a step is never undone.
            long target = (long)(_lockTime / LockStep);
            bool far = target - _lockSteps > LockFarBehind;
            if (_lockSteps < target)
            {
                // Boxed in time (see `LockCatchUpSeconds`); the clock is read every eighth step.
                double began = Time.realtimeSinceStartupAsDouble, box = Covered ? LockCatchUpCoveredSeconds : LockCatchUpSeconds;
                int done = 0;
                _quiet = true;
                while (_lockSteps < target)
                {
                    _clock += LockStep; UpdateRoutes(LockStep); _lockSteps++; done++;
                    if (done >= LockCatchUpFloor && (done & 7) == 0 && Time.realtimeSinceStartupAsDouble - began > box) break;
                }
                _quiet = false;
            }
            _caughtUp = _lockSteps >= target;
            Count(!_caughtUp);
            // Still far behind: the street is not shown stepping through what it missed (nothing
            // is posed, so the vehicles, their solids and their brake lights wait where they are).
            if (far && !_caughtUp) return;
            float lead = _caughtUp ? Mathf.Clamp((float)(_lockTime - _lockSteps * (double)LockStep), 0f, LockStep) : 0f;
            for (int i = 0; i < Drivers.Length; i++)
            {
                var d = Drivers[i];
                if (d.Body == null || d.Lane < 0 || d.Lane >= Routes.Length) continue;
                Pose(i, lead);
            }
            UpdateSignals();
        }

        /// <summary>This road's part of `CatchingUp`.</summary>
        private void Count(bool catching)
        {
            if (catching == _counted) return;
            _counted = catching;
            _catchingUp += catching ? 1 : -1;
        }

        private void OnDisable() => Count(false);

        /// <summary>Every vehicle back at its authored start, speeds and the signal clock drawn from `seed`.</summary>
        private void Reseed(int seed)
        {
            _random = new System.Random(seed);
            for (int i = 0; i < Drivers.Length; i++)
            {
                var d = Drivers[i];
                d.Lane = _lane0[i]; d.Along = _along0[i];
                _speed[i] = d.Cruise * Range(.5f, 1f); _brake[i] = 0f; _pitch[i] = 0f;
            }
            _clock = Range(0f, CycleSeconds);
            _lastPhase = -1;
            _lockSteps = 0;
            EaseIntoStops();
        }

        /// <summary>The bots' disc rides over each moving vehicle's nose and the road it is about to
        /// cover (0.35 s of travel); a stopped vehicle has none, so bots cross between queued cars.</summary>
        private void UpdateHazards()
        {
            UpdateSolids();
            if (_hazards == null) return;
            for (int i = 0; i < _hazards.Length; i++)
            {
                var go = _hazards[i];
                if (go == null) continue;
                bool moving = _caughtUp && _speed[i] >= HitMinSpeed;
                if (go.activeSelf != moving) go.SetActive(moving);
                if (!moving) continue;
                var right = Vector3.Cross(Vector3.up, _poseDir[i]);
                go.transform.position = _poseAt[i] + _poseDir[i] * (_box[i].x + _box[i].z * .5f + _speed[i] * .35f) + right * _box[i].y;
            }
        }

        /// <summary>
        /// Each moving vehicle's box against each player, flat, under its roof. On the resolving
        /// peer a touch is the hit (see § THE LIVE ROAD); on every other peer the same touch, on a
        /// player the host has just felled, draws the popup and the dust the host drew for itself.
        /// Four players by a few dozen vehicles: no physics query, no allocation.
        /// </summary>
        private void UpdateHits()
        {
            if (!_caughtUp || _box == null) return;
            var round = GameServices.Round;
            var players = round != null ? round.Players : null;
            if (players == null) return;
            bool resolve = NetAuthority.ShouldResolve();
            float now = Time.time;
            for (int k = 0; k < players.Count; k++)
            {
                var who = players[k];
                if (who == null || !who.isActiveAndEnabled) continue;
                var at = who.transform.position;
                for (int i = 0; i < Drivers.Length; i++)
                {
                    if (Drivers[i].Body == null || _speed[i] < HitMinSpeed) continue;
                    var d = at - _poseAt[i];
                    // Over its roof (a jump, a flying hero) or well under the road: no contact.
                    if (d.y > _boxTop[i] || d.y < -1.5f) continue;
                    var dir = _poseDir[i]; var right = Vector3.Cross(Vector3.up, dir);
                    float along = Vector3.Dot(d, dir) - _box[i].x, side = Vector3.Dot(d, right) - _box[i].y;
                    // A watching peer sees the fall a moment after the host decided it: the car has moved on.
                    float slack = resolve ? 0f : 1.5f;
                    if (Mathf.Abs(along) > _box[i].z + HitBodyRadius + slack || Mathf.Abs(side) > _box[i].w + HitBodyRadius + slack * .3f) continue;
                    if (resolve) Hit(i, who, at, dir, right, side, now);
                    else if (who.IsTripped && who.TripLeft > HitTrip - .6f) Show(who, at, now);
                    break;
                }
            }
        }

        private void Hit(int i, CharacterMotor who, Vector3 at, Vector3 dir, Vector3 right, float side, float now)
        {
            if (_nextHit.TryGetValue(who, out float next) && now < next) return;
            // The body's own grace after a get-up, the one window every hazard reads (StreetTripHazard).
            if (who.IsTripImmune) return;
            _nextHit[who] = now + HitCooldown;
            bool wasDown = who.IsTripped;
            who.ApplyTrip(HitTrip);
            // Immune to stuns (a hero's carapace): `ApplyTrip` refused, and so would the throw. The car passes.
            if (!who.IsTripped) return;
            float throwSpeed = Mathf.Lerp(HitThrow.x, HitThrow.y, Mathf.InverseLerp(HitMinSpeed, 9f, _speed[i]));
            // After the trip, which zeroes the walk: the throw is the body's external velocity.
            // A LAUNCH, past the knockback caps (`CharacterMotor.AsLaunch`): "i wanna feel like im getting launched".
            who.ApplyResolvedImpact(CharacterMotor.AsLaunch(dir * throwSpeed + right * (side >= 0f ? HitAside : -HitAside) + Vector3.up * HitLift));
            Visual.WindTumble.Attach(who)?.Throw(HitTrip);
            // `hit_body` is a body meeting something hard, the cue a trip already plays; pitched lower, for a car.
            NetCue.PlayVaried("hit_body", at, .7f, .9f, 1f);
            if (!wasDown) Show(who, at, now);
        }

        /// <summary>The fall's announcement, as `StreetTripHazard` announces a trip: a comic popup and a dust burst.</summary>
        private void Show(CharacterMotor who, Vector3 at, float now)
        {
            if (_nextShown.TryGetValue(who, out float next) && now < next) return;
            _nextShown[who] = now + HitTrip;
            ComicPopup.Spawn(at + Vector3.up * 1f, HitPopup, UI.UiTheme.Danger, 1.2f);
            // The body is the air's while it flies: the same tumble on every peer that sees the fall.
            Visual.WindTumble.Attach(who)?.Throw(HitTrip);
            ImpactBurst.SpawnAt(at);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (RouteMode)
            {
                for (int r = 0; r < Routes.Length; r++)
                {
                    var p = Routes[r].Points; if (p == null) continue;
                    Gizmos.color = Color.HSVToRGB(r * .17f % 1f, .8f, 1f);
                    for (int k = 0; k + 1 < p.Length; k++) Gizmos.DrawLine(p[k] + Vector3.up * .2f, p[k + 1] + Vector3.up * .2f);
                    if (Routes[r].StopAlong != null) foreach (float s in Routes[r].StopAlong) Gizmos.DrawWireSphere(RoutePoint(r, s), .6f);
                }
                return;
            }
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
