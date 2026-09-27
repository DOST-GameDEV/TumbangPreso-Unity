using System;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// Seabird flocks over Lagoon Cove and fish schools over its reefs, steered as boids
    /// (separation, alignment, cohesion) plus soft pulls that keep each group in its volume.
    /// Now and then a bird lands on the play area; a thrown slipper through it bursts it into feathers.
    /// </summary>
    // ⚠️ Ambient life is scenery. Every bird, fish and feather here is local only, never on the
    // wire, never a collider, never a physics body, and never reads or writes a gameplay random
    // stream. Two peers see different birds and that is correct: nothing a player does depends on
    // them. The only physics use is read-only: one downward raycast to find the sand when a bird
    // picks a landing spot.
    public sealed class LagoonFlocks : MonoBehaviour
    {
        // All models face their local +Z, up +Y, metres.
        public Transform BirdTemplate;          // inactive; children "body", "wing_l", "wing_r" (wing origins at the shoulder)
        public Transform[] FishTemplates;       // inactive; children "body", "tail" (tail origin at the joint)
        public float WaterY;                    // world y of the water surface
        public Vector3 SkyCentre;               // centre of the birds' area (the cove)
        public float SkyRadius = 110f;          // horizontal radius birds roam in
        public Vector2 SkyHeight = new Vector2(10f, 34f); // y band above WaterY birds fly in
        public int Flocks = 3, BirdsPerFlock = 7;
        public Vector3[] SchoolCentres;         // world points over the reefs, each at mid-water
        public float[] SchoolFloor;             // per school: world y of the seabed under it
        public float SchoolRadius = 7f;
        public int FishPerSchool = 10;
        public Vector3 CourtCentre; public float CourtKeepOut = 18f;
        public float CourtHalf = 13f;           // half side of the square a bird may land in, around CourtCentre
        public float CourtGroundY = 0f;         // ground height used when the landing raycast finds nothing

        /// <summary>How many bird slots exist (for the soundscape). Stable for the whole match.</summary>
        public int BirdCount => _birdCount;
        /// <summary>World position of bird <paramref name="i"/> (for the soundscape).</summary>
        public Vector3 BirdPosition(int i) =>
            _birdPos != null && i >= 0 && i < _birdCount ? _birdPos[i] : transform.position;
        /// <summary>False while bird <paramref name="i"/> is burst and waiting to respawn.</summary>
        public bool BirdVisible(int i) => _mode != null && i >= 0 && i < _birdCount && _mode[i] != ModeDead;
        /// <summary>Raised with the burst point whenever a slipper bursts a bird.</summary>
        public event Action<Vector3> BirdBurst;
        /// <summary>How many birds slippers have burst since Start (for probes).</summary>
        public int Bursts { get; private set; }

        // ⚠️ Bird numbers. 7 to 11 m/s is a gull or tern cruising, not a pigeon dart. The
        // acceleration cap is what turns a boid swarm into wide arcs: at 3.2 m/s² and 9 m/s the
        // tightest turn is about 25 m across, so a flock changing its mind reads as a long bank.
        private const float BirdMinSpeed = 7f, BirdMaxSpeed = 11f, BirdAccel = 3.2f;
        private const float BirdMaxClimb = 3f, BirdMaxDive = 4f, BirdSeparation = 4f;
        // ⚠️ Birds stay at least this far above the water inside CourtKeepOut, so a flock never
        // crosses a player's eye line low over the court (owner brief, 2026-09-27). A bird that
        // is landing, grounded or taking off is exempt: landing on the court is the point of it.
        private const float CourtClearance = 14f;
        // ⚠️ How low a dipping flock skims the open water. Only the dip lowers the floor; the
        // court clearance above still applies during a dip.
        private const float DipHeight = 3.5f;
        // ⚠️ Fish numbers. 0.6 to 1.6 m/s is a reef school cruising; a startle bursts to about
        // 3 m/s for a second and a half and the ordinary cohesion brings the school back.
        private const float FishMinSpeed = .6f, FishMaxSpeed = 1.6f, FishScatterSpeed = 3.2f;
        private const float FishAccel = 1.4f, FishScatterAccel = 9f, FishSeparation = .6f;
        private const float FishScatterTime = 1.4f, FishRegroupTime = 3f;
        // ⚠️ A hitch (a scene load, an alt-tab) can hand one frame half a second. Integrating
        // that whole step flings a bird through its volume; clamping it just slows that frame.
        private const float MaxStep = 1f / 20f;

        // Grounded birds and the slipper hit (owner, 2026-09-27: "like how csgo chickens work").
        private const int ModeFlying = 0, ModeLanding = 1, ModeGrounded = 2, ModeTakeoff = 3, ModeDead = 4;
        // ⚠️ Owner, 2026-09-27, after the first pass (every 25 to 60 s, two down at most): "make
        // birds land more frequently". Now a landing is tried every LandEvery seconds, the first
        // within a few seconds of the map loading, and up to three may be down at once.
        private const int MaxGrounded = 3;
        private static readonly Vector2 LandEvery = new Vector2(6f, 14f);
        private const float PlayerStartle = 3f, SlipperStartle = 2f;
        // ⚠️ 4 m/s separates a thrown slipper from one skidding to rest or nudged. The speed is
        // measured from the slipper's own transform between frames, so it works identically on
        // the host and on every client (a client's slipper is driven by snapshots, not physics).
        private const float ThrownSpeed = 4f, HitRadius = .45f, LowFlightHit = 3f;
        private const float RescanSeconds = 1f;
        private const int SlipperCapacity = 32;
        private const int FeatherCapacity = 160;

        private System.Random _random;

        // Birds, one slot per bird, flock members contiguous.
        private int _birdCount;
        private Transform[] _birdRoot, _birdBody, _wingL, _wingR;
        private Quaternion[] _wingLRest, _wingRRest;
        private Vector3[] _birdPos, _birdVel;
        private float[] _bank, _heading, _flapPhase, _flapRate, _glideLeft, _wingAngle;
        private int[] _flapsLeft, _mode, _flockOf;

        // Per-bird state for landing, the ground and the respawn wait.
        private Vector3[] _landAt;
        private float[] _stateTime, _stayLeft, _actLeft, _actTime, _yawFrom, _yawTo, _pitch;
        private int[] _act;
        private Vector3[] _hopFrom, _hopTo;
        private float _nextLanding;

        private int _flockCount;
        private int[] _flockStart, _flockSize;
        private Vector3[] _flockGoal;
        private float[] _flockGoalTimer, _dipLeft;
        private float _nextDip;

        // Fish, one slot per fish, school members contiguous.
        private int _fishCount;
        private Transform[] _fishRoot, _tail;
        private Quaternion[] _tailRest;
        private Vector3[] _fishPos, _fishVel;
        private float[] _tailPhase, _fishCruise;

        private int _schoolCount;
        private int[] _schoolStart, _schoolSize;
        private Vector3[] _schoolCentre, _schoolGoal;
        private float[] _schoolLow, _schoolHigh, _schoolGoalTimer, _scatterLeft, _regroupLeft, _nextStartle;

        // Cached scene lookups, refreshed at most once a second.
        private float _rescanLeft;
        private CharacterMotor[] _motors = Array.Empty<CharacterMotor>();
        private readonly Slipper[] _slippers = new Slipper[SlipperCapacity];
        private readonly Vector3[] _slipperPrev = new Vector3[SlipperCapacity];
        private readonly Vector3[] _slipperNow = new Vector3[SlipperCapacity];
        private readonly Slipper[] _slipperScratch = new Slipper[SlipperCapacity];
        private readonly Vector3[] _prevScratch = new Vector3[SlipperCapacity];
        private int _slipperCount;

        // Feather pool.
        private Mesh _featherMesh;
        private Material[] _featherMats;
        private Material[] _ownedMats;
        private int _featherBuilt;
        private readonly Transform[] _fT = new Transform[FeatherCapacity];
        private readonly Vector3[] _fPos = new Vector3[FeatherCapacity], _fVel = new Vector3[FeatherCapacity];
        private readonly Vector3[] _fSpinAxis = new Vector3[FeatherCapacity], _fSide = new Vector3[FeatherCapacity];
        private readonly Quaternion[] _fRot = new Quaternion[FeatherCapacity];
        private readonly float[] _fAge = new float[FeatherCapacity], _fLife = new float[FeatherCapacity];
        private readonly float[] _fSpin = new float[FeatherCapacity], _fSize = new float[FeatherCapacity];
        private readonly float[] _fPhase = new float[FeatherCapacity], _fFloor = new float[FeatherCapacity];
        private readonly bool[] _fPuff = new bool[FeatherCapacity];

        private void Start()
        {
            // ⚠️ A private stream, seeded fresh, exactly as AmbientLife does: the flocks never
            // consume a number from a throw, a bot decision, a match seed or any shared stream.
            // It also means each peer's birds are its own, so a burst is seen only by the peers
            // whose bird happened to be there. Acceptable for a cosmetic.
            _random = new System.Random(Guid.NewGuid().GetHashCode());
            SpawnBirds();
            SpawnFish();
            BuildFeathers();
            _nextLanding = Range(3f, 8f);
        }

        private void OnDestroy()
        {
            if (_featherMesh != null) Destroy(_featherMesh);
            if (_ownedMats != null) foreach (var m in _ownedMats) if (m != null) Destroy(m);
        }

        private float Range(float a, float b) => a + (float)_random.NextDouble() * (b - a);

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        // ---------------------------------------------------------------- spawning

        private Transform Spawn(Transform template, string name)
        {
            var clone = Instantiate(template.gameObject, transform, false);
            clone.name = name;
            // ⚠️ Disabled, not destroyed: a bird or fish must never block a throw or a body.
            foreach (var collider in clone.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            clone.SetActive(true);
            return clone.transform;
        }

        private static Transform FindChild(Transform root, string name)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child.name == name) return child;
                var deeper = FindChild(child, name);
                if (deeper != null) return deeper;
            }
            return null;
        }

        private void SpawnBirds()
        {
            _flockCount = BirdTemplate == null ? 0 : Mathf.Max(0, Flocks);
            int perFlock = Mathf.Max(0, BirdsPerFlock);
            int n = _birdCount = _flockCount * perFlock;
            _birdRoot = new Transform[n]; _birdBody = new Transform[n]; _wingL = new Transform[n]; _wingR = new Transform[n];
            _wingLRest = new Quaternion[n]; _wingRRest = new Quaternion[n];
            _birdPos = new Vector3[n]; _birdVel = new Vector3[n];
            _bank = new float[n]; _heading = new float[n]; _flapPhase = new float[n];
            _flapRate = new float[n]; _glideLeft = new float[n]; _wingAngle = new float[n];
            _flapsLeft = new int[n]; _mode = new int[n]; _flockOf = new int[n];
            _landAt = new Vector3[n]; _stateTime = new float[n]; _stayLeft = new float[n];
            _actLeft = new float[n]; _actTime = new float[n]; _yawFrom = new float[n]; _yawTo = new float[n];
            _pitch = new float[n]; _act = new int[n]; _hopFrom = new Vector3[n]; _hopTo = new Vector3[n];
            _flockStart = new int[_flockCount]; _flockSize = new int[_flockCount];
            _flockGoal = new Vector3[_flockCount]; _flockGoalTimer = new float[_flockCount]; _dipLeft = new float[_flockCount];
            _nextDip = Range(20f, 40f);

            int b = 0;
            for (int f = 0; f < _flockCount; f++)
            {
                _flockStart[f] = b; _flockSize[f] = perFlock;
                float angle = Range(0f, Mathf.PI * 2f);
                var home = SkyCentre + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * SkyRadius * Range(.3f, .7f);
                home.y = WaterY + Mathf.Lerp(SkyHeight.x, SkyHeight.y, Range(.45f, .85f));
                home.y = Mathf.Max(home.y, WaterY + CourtClearance + 2f);
                // Start circling the cove, so the first seconds already read as a glide.
                var tangent = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * (_random.NextDouble() < .5 ? 1f : -1f);
                for (int k = 0; k < perFlock; k++, b++)
                {
                    var root = Spawn(BirdTemplate, "Seabird " + f + "." + k);
                    _birdRoot[b] = root; _flockOf[b] = f;
                    _birdBody[b] = FindChild(root, "body");
                    _wingL[b] = FindChild(root, "wing_l"); _wingR[b] = FindChild(root, "wing_r");
                    if (_wingL[b] != null) _wingLRest[b] = _wingL[b].localRotation;
                    if (_wingR[b] != null) _wingRRest[b] = _wingR[b].localRotation;
                    _birdPos[b] = home + new Vector3(Range(-5f, 5f), Range(-2f, 2f), Range(-5f, 5f));
                    _birdVel[b] = tangent * Range(8f, 10f);
                    _heading[b] = Mathf.Atan2(_birdVel[b].x, _birdVel[b].z) * Mathf.Rad2Deg;
                    // ⚠️ Per-bird rate and a staggered first glide: seven wings beating in step
                    // read as one machine, not seven birds.
                    _flapRate[b] = Range(3.2f, 4.1f);
                    _glideLeft[b] = Range(0f, 3f);
                    _wingAngle[b] = 6f;
                    root.SetPositionAndRotation(_birdPos[b], Quaternion.LookRotation(_birdVel[b]));
                }
                PickFlockGoal(f);
            }
        }

        private void SpawnFish()
        {
            int valid = 0;
            if (FishTemplates != null) foreach (var t in FishTemplates) if (t != null) valid++;
            _schoolCount = valid == 0 || SchoolCentres == null ? 0 : SchoolCentres.Length;
            int perSchool = Mathf.Max(0, FishPerSchool);
            _fishCount = _schoolCount * perSchool;
            var species = new Transform[valid];
            if (valid > 0) { int n = 0; foreach (var t in FishTemplates) if (t != null) species[n++] = t; }

            _fishRoot = new Transform[_fishCount]; _tail = new Transform[_fishCount]; _tailRest = new Quaternion[_fishCount];
            _fishPos = new Vector3[_fishCount]; _fishVel = new Vector3[_fishCount];
            _tailPhase = new float[_fishCount]; _fishCruise = new float[_fishCount];
            _schoolStart = new int[_schoolCount]; _schoolSize = new int[_schoolCount];
            _schoolCentre = new Vector3[_schoolCount]; _schoolGoal = new Vector3[_schoolCount];
            _schoolLow = new float[_schoolCount]; _schoolHigh = new float[_schoolCount];
            _schoolGoalTimer = new float[_schoolCount]; _scatterLeft = new float[_schoolCount];
            _regroupLeft = new float[_schoolCount]; _nextStartle = new float[_schoolCount];

            int offset = valid > 0 ? _random.Next(valid) : 0, i = 0;
            for (int s = 0; s < _schoolCount; s++)
            {
                var centre = SchoolCentres[s];
                float floor = SchoolFloor != null && s < SchoolFloor.Length ? SchoolFloor[s] : centre.y - 3f;
                float low = floor + .4f, high = WaterY - .5f;
                if (low > high) { float mid = (low + high) * .5f; low = mid; high = mid; }
                _schoolCentre[s] = centre; _schoolLow[s] = low; _schoolHigh[s] = high;
                _schoolStart[s] = i; _schoolSize[s] = perSchool;
                _nextStartle[s] = Range(15f, 60f);
                // ⚠️ Mostly one species per school, with the odd stray: a real reef school is
                // one kind, and a rainbow of every template reads as a spawner.
                var main = species[(offset + s) % valid];
                for (int k = 0; k < perSchool; k++, i++)
                {
                    var template = valid > 1 && _random.NextDouble() < .12 ? species[_random.Next(valid)] : main;
                    var root = Spawn(template, "Reef fish " + s + "." + k);
                    _fishRoot[i] = root;
                    _tail[i] = FindChild(root, "tail");
                    if (_tail[i] != null) _tailRest[i] = _tail[i].localRotation;
                    float a = Range(0f, Mathf.PI * 2f), r = Range(0f, SchoolRadius * .4f);
                    _fishPos[i] = new Vector3(centre.x + Mathf.Cos(a) * r, Range(low, high), centre.z + Mathf.Sin(a) * r);
                    float h = Range(0f, Mathf.PI * 2f);
                    _fishVel[i] = new Vector3(Mathf.Cos(h), 0f, Mathf.Sin(h));
                    _fishCruise[i] = Range(.8f, 1.3f);
                    _tailPhase[i] = Range(0f, Mathf.PI * 2f);
                    root.SetPositionAndRotation(_fishPos[i], Quaternion.LookRotation(_fishVel[i]));
                }
                PickSchoolGoal(s);
            }
        }

        // ⚠️ One shared mesh and three shared materials, made once. Feathers are pooled
        // GameObjects built on demand up to FeatherCapacity, so a burst after warm-up allocates
        // nothing. The materials come from the bird's own "body" renderer so the feathers are
        // the bird's colour; the cream and grey copies multiply its _Color (the toon shader's
        // tint) and are the only materials this component owns.
        private void BuildFeathers()
        {
            Material body = null;
            if (_birdCount > 0)
            {
                var source = _birdBody[0] != null ? _birdBody[0].GetComponentInChildren<Renderer>(true) : null;
                if (source == null) source = _birdRoot[0].GetComponentInChildren<Renderer>(true);
                if (source != null) body = source.sharedMaterial;
            }
            if (body == null) return;
            _featherMesh = BuildBox(new Vector3(.05f, .012f, .11f));
            var cream = new Material(body) { name = "Feather cream" };
            var grey = new Material(body) { name = "Feather grey" };
            if (cream.HasProperty("_Color")) cream.color = cream.color * new Color(1f, .95f, .84f, 1f);
            if (grey.HasProperty("_Color")) grey.color = grey.color * new Color(.72f, .7f, .68f, 1f);
            _ownedMats = new[] { cream, grey };
            _featherMats = new[] { body, cream, grey };
        }

        private static Mesh BuildBox(Vector3 size)
        {
            var h = size * .5f;
            var v = new Vector3[24]; var nrm = new Vector3[24]; var tri = new int[36];
            Vector3[] normals = { Vector3.up, Vector3.down, Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
            for (int face = 0; face < 6; face++)
            {
                var nn = normals[face];
                var u = face < 2 ? Vector3.right : face < 4 ? Vector3.forward : Vector3.right;
                var w = Vector3.Cross(nn, u);
                for (int c = 0; c < 4; c++)
                {
                    float su = c == 0 || c == 3 ? -1f : 1f, sw = c < 2 ? -1f : 1f;
                    var p = nn + u * su + w * sw;
                    v[face * 4 + c] = Vector3.Scale(p, h);
                    nrm[face * 4 + c] = nn;
                }
                int t = face * 6, o = face * 4;
                // ⚠️ Unity's front face is the one where cross(b - a, c - a) points at the viewer.
                // Here cross(u, w) = cross(u, cross(n, u)) = n, so (c0, c1, c2) faces outward.
                tri[t] = o; tri[t + 1] = o + 1; tri[t + 2] = o + 2;
                tri[t + 3] = o; tri[t + 4] = o + 2; tri[t + 5] = o + 3;
            }
            var mesh = new Mesh { name = "Feather" };
            mesh.vertices = v; mesh.normals = nrm; mesh.triangles = tri;
            mesh.RecalculateBounds();
            return mesh;
        }

        // ---------------------------------------------------------------- goals

        // ⚠️ "Open water" is judged only as "well away from the court": the cove is the sky
        // area, and the court is the one place a low bird is wrong.
        private Vector3 OpenWaterPoint(float minFromCourt)
        {
            for (int k = 0; k < 12; k++)
            {
                float a = Range(0f, Mathf.PI * 2f);
                var p = SkyCentre + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * SkyRadius * Range(.25f, .7f);
                if (Flat(p - CourtCentre).magnitude >= minFromCourt) return p;
            }
            var away = Flat(SkyCentre - CourtCentre);
            away = away.sqrMagnitude > .01f ? away.normalized : Vector3.forward;
            return SkyCentre + away * SkyRadius * .6f;
        }

        // Centroid and mean velocity of the flock's FLYING members only; false when none fly.
        private bool FlockMean(int f, out Vector3 centroid, out Vector3 velocity)
        {
            centroid = Vector3.zero; velocity = Vector3.zero; int n = 0;
            int start = _flockStart[f], end = start + _flockSize[f];
            for (int b = start; b < end; b++)
            {
                if (_mode[b] != ModeFlying) continue;
                centroid += _birdPos[b]; velocity += _birdVel[b]; n++;
            }
            if (n == 0) { centroid = _flockGoal[f]; return false; }
            centroid /= n; velocity /= n;
            return true;
        }

        private void PickFlockGoal(int f)
        {
            FlockMean(f, out var centroid, out _);
            Vector3 goal = SkyCentre;
            // ⚠️ A goal at least 40 m off makes the flock commit to a long curve; a near goal
            // makes it circle on the spot.
            for (int k = 0; k < 6; k++)
            {
                float a = Range(0f, Mathf.PI * 2f);
                goal = SkyCentre + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * SkyRadius * Range(.1f, .75f);
                if (Flat(goal - centroid).sqrMagnitude > 40f * 40f) break;
            }
            goal.y = WaterY + Mathf.Lerp(SkyHeight.x, SkyHeight.y, Range(.3f, .9f));
            if (Flat(goal - CourtCentre).magnitude < CourtKeepOut + 15f) goal.y = Mathf.Max(goal.y, WaterY + CourtClearance + 3f);
            _flockGoal[f] = goal;
            _flockGoalTimer[f] = Range(14f, 26f);
        }

        private void PickSchoolGoal(int s)
        {
            float a = Range(0f, Mathf.PI * 2f), r = Range(.2f, .55f) * SchoolRadius;
            var c = _schoolCentre[s];
            float low = _schoolLow[s], high = _schoolHigh[s];
            float y = high - low > .6f ? Range(low + .3f, high - .3f) : (low + high) * .5f;
            _schoolGoal[s] = new Vector3(c.x + Mathf.Cos(a) * r, y, c.z + Mathf.Sin(a) * r);
            // ⚠️ A new goal every few seconds is the "gentle collective turn": every fish
            // seeks the same point, so the whole school swings together.
            _schoolGoalTimer[s] = Range(4f, 9f);
        }

        // ---------------------------------------------------------------- simulation

        private void Update()
        {
            // ⚠️ Time.deltaTime is 0 while paused (timeScale 0): return before any timer or
            // state moves, so everything resumes exactly where it stopped.
            float raw = Time.deltaTime;
            if (raw <= 0f || _birdPos == null || _fishPos == null) return;
            float dt = raw > MaxStep ? MaxStep : raw;

            _rescanLeft -= raw;
            if (_rescanLeft <= 0f) { _rescanLeft = RescanSeconds; Rescan(); }
            ReadSlippers();

            StepBirds(dt);
            StepGroundLife(dt);
            // The slipper speed is measured over the real frame, not the clamped one.
            CheckSlipperHits(raw);
            StepFish(dt);
            StepFeathers(dt);
        }

        // ⚠️ Players and slippers are found at most once a second: FindObjectsByType walks the
        // scene and allocates, and a slipper or player appearing a second late only means a bird
        // notices it a second late. Slippers keep their previous position across a rescan (matched
        // by reference), so a rescan can never fake a fast slipper.
        private void Rescan()
        {
            _motors = FindObjectsByType<CharacterMotor>();
            var found = FindObjectsByType<Slipper>();
            int count = Mathf.Min(found.Length, SlipperCapacity);
            for (int k = 0; k < count; k++)
            {
                var s = found[k];
                _slipperScratch[k] = s;
                var prev = s.transform.position;
                for (int j = 0; j < _slipperCount; j++)
                    if (ReferenceEquals(_slippers[j], s)) { prev = _slipperPrev[j]; break; }
                _prevScratch[k] = prev;
            }
            for (int k = 0; k < count; k++) { _slippers[k] = _slipperScratch[k]; _slipperPrev[k] = _prevScratch[k]; _slipperScratch[k] = null; }
            for (int k = count; k < _slipperCount; k++) _slippers[k] = null;
            _slipperCount = count;
        }

        private void ReadSlippers()
        {
            for (int k = 0; k < _slipperCount; k++)
            {
                var s = _slippers[k];
                _slipperNow[k] = s != null ? s.transform.position : _slipperPrev[k];
            }
        }

        private void StepBirds(float dt)
        {
            if (_flockCount == 0) return;
            bool anyDipping = false;
            for (int f = 0; f < _flockCount; f++)
            {
                FlockMean(f, out var centroid, out _);
                if (_dipLeft[f] > 0f)
                {
                    _dipLeft[f] -= dt;
                    bool reached = Flat(centroid - _flockGoal[f]).sqrMagnitude < 12f * 12f && centroid.y < WaterY + DipHeight + 4f;
                    // Climbing back is just an ordinary high goal: the floor rises and the flock follows.
                    if (reached || _dipLeft[f] <= 0f) { _dipLeft[f] = 0f; PickFlockGoal(f); }
                    else anyDipping = true;
                }
                else
                {
                    _flockGoalTimer[f] -= dt;
                    if (_flockGoalTimer[f] <= 0f || Flat(centroid - _flockGoal[f]).sqrMagnitude < 18f * 18f) PickFlockGoal(f);
                }
            }
            _nextDip -= dt;
            if (_nextDip <= 0f && !anyDipping)
            {
                // ⚠️ One flock at a time, now and then: a dip is an event the eye catches, and
                // three flocks skimming at once would just be birds flying low.
                int f = _random.Next(_flockCount);
                var p = OpenWaterPoint(CourtKeepOut + 25f);
                p.y = WaterY + DipHeight + .5f;
                _flockGoal[f] = p;
                _dipLeft[f] = 16f;
                _nextDip = Range(28f, 55f);
            }

            float blendBank = 1f - Mathf.Exp(-3f * dt), blendWing = 1f - Mathf.Exp(-10f * dt);
            float skyEdge = SkyRadius * .8f, ceiling = WaterY + SkyHeight.y, courtFloor = WaterY + CourtClearance;
            for (int f = 0; f < _flockCount; f++)
            {
                int start = _flockStart[f], end = start + _flockSize[f];
                bool dipping = _dipLeft[f] > 0f;
                float floor = WaterY + (dipping ? DipHeight : SkyHeight.x);
                var goal = _flockGoal[f];
                for (int i = start; i < end; i++)
                {
                    if (_mode[i] != ModeFlying) continue;
                    var pos = _birdPos[i]; var vel = _birdVel[i];
                    Vector3 sep = Vector3.zero, ali = Vector3.zero, coh = Vector3.zero; int n = 0;
                    for (int j = start; j < end; j++)
                    {
                        if (j == i || _mode[j] != ModeFlying) continue;
                        var d = _birdPos[j] - pos; float sq = d.sqrMagnitude;
                        if (sq < BirdSeparation * BirdSeparation && sq > 1e-4f) sep -= d / sq;
                        ali += _birdVel[j]; coh += _birdPos[j]; n++;
                    }
                    var steer = sep * 6f;
                    if (n > 0) { steer += (ali / n - vel) * .35f; steer += (coh / n - pos) * .08f; }
                    var toGoal = goal - pos;
                    if (toGoal.sqrMagnitude > 1e-4f) steer += (toGoal.normalized * 9f - vel) * .5f;
                    // Flapping pulls forward a little, gliding bleeds a little: the speed breathes with the wings.
                    if (vel.sqrMagnitude > 1e-4f) steer += vel.normalized * (_flapsLeft[i] > 0 ? .9f : -.4f);
                    if (steer.sqrMagnitude > BirdAccel * BirdAccel) steer = steer.normalized * BirdAccel;

                    // ⚠️ Volume keeping is a separate, stronger budget added after the boid cap, so
                    // a flock's own wishes can never out-vote the edge of the sky or the court.
                    var keep = Vector3.zero;
                    var fromSky = Flat(pos - SkyCentre); float skyDist = fromSky.magnitude;
                    if (skyDist > skyEdge) keep -= fromSky / skyDist * (skyDist - skyEdge) * .25f;
                    if (pos.y < floor + 2f) keep.y += (floor + 2f - pos.y) * .8f;
                    if (pos.y > ceiling - 2f) keep.y -= (pos.y - (ceiling - 2f)) * .8f;
                    // Look two seconds ahead so the climb starts before the bird reaches the court, not over it.
                    var ahead = pos + vel * 2f;
                    var fromCourt = Flat(ahead - CourtCentre); float courtDist = fromCourt.magnitude;
                    if (courtDist < CourtKeepOut + 10f && pos.y < courtFloor + 3f)
                    {
                        keep.y += (courtFloor + 3f - pos.y) * 1.2f;
                        if (pos.y < courtFloor && courtDist > 1e-3f) keep += fromCourt / courtDist * (CourtKeepOut + 12f - courtDist) * .5f;
                    }
                    if (keep.sqrMagnitude > 4f * BirdAccel * BirdAccel) keep = keep.normalized * (2f * BirdAccel);

                    vel += (steer + keep) * dt;
                    vel.y = Mathf.Clamp(vel.y, -BirdMaxDive, BirdMaxClimb);
                    var flat = Flat(vel); float hs = flat.magnitude;
                    float hMin = Mathf.Sqrt(Mathf.Max(BirdMinSpeed * BirdMinSpeed - vel.y * vel.y, 1f));
                    float hMax = Mathf.Sqrt(Mathf.Max(BirdMaxSpeed * BirdMaxSpeed - vel.y * vel.y, 1f));
                    if (hs < 1e-3f) { float h = _heading[i] * Mathf.Deg2Rad; flat = new Vector3(Mathf.Sin(h), 0f, Mathf.Cos(h)); hs = 1f; }
                    flat *= Mathf.Clamp(hs, hMin, hMax) / hs;
                    vel = new Vector3(flat.x, vel.y, flat.z);
                    pos += vel * dt;
                    _birdVel[i] = vel; _birdPos[i] = pos;
                    PoseFlying(i, dt, vel, blendBank, .6f);
                    StepWings(i, dt, vel.y, blendWing);
                }
            }
        }

        // ⚠️ Bank from the measured turn rate, into the turn: a right turn (positive yaw) drops
        // the right wing, which is a negative roll about +Z in Unity.
        private void PoseFlying(int i, float dt, Vector3 vel, float blendBank, float pitchScale)
        {
            if (Flat(vel).sqrMagnitude > 1e-4f)
            {
                float heading = Mathf.Atan2(vel.x, vel.z) * Mathf.Rad2Deg;
                float yawRate = Mathf.DeltaAngle(_heading[i], heading) / dt;
                _heading[i] = heading;
                _bank[i] = Mathf.Lerp(_bank[i], Mathf.Clamp(-yawRate * 1.1f, -45f, 45f), blendBank);
            }
            else _bank[i] = Mathf.Lerp(_bank[i], 0f, blendBank);
            float hs = Flat(vel).magnitude, pitch = -Mathf.Atan2(vel.y * pitchScale, Mathf.Max(hs, .5f)) * Mathf.Rad2Deg;
            _birdRoot[i].SetPositionAndRotation(_birdPos[i], Quaternion.Euler(pitch, _heading[i], _bank[i]));
        }

        // ⚠️ Flap in bursts of 3 or 4 beats, then glide. A bird that never stops flapping reads
        // as a toy; the glide is most of what a seabird does. A climbing bird glides less.
        private void StepWings(int i, float dt, float climb, float blend)
        {
            float angle;
            if (_flapsLeft[i] > 0)
            {
                _flapPhase[i] += dt * _flapRate[i] * Mathf.PI * 2f;
                if (_flapPhase[i] >= Mathf.PI * 2f)
                {
                    _flapPhase[i] -= Mathf.PI * 2f;
                    if (--_flapsLeft[i] <= 0) { _flapPhase[i] = 0f; _glideLeft[i] = climb > 1f ? Range(.6f, 1.4f) : Range(1.4f, 3.6f); }
                }
                // Up to 48 degrees on the upstroke, down to minus 28 on the downstroke.
                angle = _flapsLeft[i] > 0 ? 10f + 38f * Mathf.Sin(_flapPhase[i]) : Mathf.Lerp(_wingAngle[i], 6f, blend);
            }
            else
            {
                _glideLeft[i] -= dt;
                if (_glideLeft[i] <= 0f) { _flapsLeft[i] = _random.NextDouble() < .5 ? 3 : 4; _flapPhase[i] = 0f; }
                // A glide holds a slight dihedral, not a flat plank.
                angle = Mathf.Lerp(_wingAngle[i], 6f, blend);
            }
            SetWings(i, angle);
        }

        // ⚠️ Wings roll about the body's forward axis. Positive Z roll lifts +X, so the left wing
        // (at -X) takes the negative angle to rise with the right one.
        private void SetWings(int i, float angle)
        {
            _wingAngle[i] = angle;
            if (_wingL[i] != null) _wingL[i].localRotation = _wingLRest[i] * Quaternion.Euler(0f, 0f, -angle);
            if (_wingR[i] != null) _wingR[i].localRotation = _wingRRest[i] * Quaternion.Euler(0f, 0f, angle);
        }

        // ---------------------------------------------------------------- landing, the ground, takeoff

        private void StepGroundLife(float dt)
        {
            if (_birdCount == 0) return;
            _nextLanding -= dt;
            if (_nextLanding <= 0f)
            {
                _nextLanding = Range(LandEvery.x, LandEvery.y);
                int down = 0;
                for (int i = 0; i < _birdCount; i++) if (_mode[i] == ModeLanding || _mode[i] == ModeGrounded) down++;
                // ⚠️ At most MaxGrounded on the ground: a few birds on the court are life, a crowd is a hazard to read around.
                if (down < MaxGrounded) TryStartLanding();
            }
            float blendBank = 1f - Mathf.Exp(-3f * dt), blendWing = 1f - Mathf.Exp(-10f * dt);
            for (int i = 0; i < _birdCount; i++)
            {
                switch (_mode[i])
                {
                    case ModeLanding: StepLanding(i, dt, blendBank, blendWing); break;
                    case ModeGrounded: StepGrounded(i, dt, blendWing); break;
                    case ModeTakeoff: StepTakeoff(i, dt, blendBank, blendWing); break;
                    case ModeDead: StepDead(i, dt); break;
                }
            }
        }

        private void TryStartLanding()
        {
            int start = _random.Next(_birdCount), pick = -1;
            for (int k = 0; k < _birdCount; k++)
            {
                int i = (start + k) % _birdCount;
                if (_mode[i] == ModeFlying) { pick = i; break; }
            }
            if (pick < 0 || !FindLandingSpot(out var spot)) return;
            _mode[pick] = ModeLanding; _landAt[pick] = spot; _stateTime[pick] = 0f; _flapsLeft[pick] = 0; _glideLeft[pick] = 99f;
        }

        // ⚠️ The ground is found with a read-only downward raycast from above the landing square,
        // triggers ignored, because the court's sand is not guaranteed to sit at one height. A
        // spot on a steep face or near a player is rejected; no hit falls back to CourtGroundY.
        private bool FindLandingSpot(out Vector3 spot)
        {
            float top = Mathf.Max(CourtGroundY, WaterY) + 40f;
            for (int k = 0; k < 8; k++)
            {
                var p = new Vector3(CourtCentre.x + Range(-CourtHalf, CourtHalf), top, CourtCentre.z + Range(-CourtHalf, CourtHalf));
                float y = CourtGroundY;
                if (Physics.Raycast(p, Vector3.down, out var hit, 80f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    if (hit.normal.y < .75f) continue;
                    y = hit.point.y;
                }
                spot = new Vector3(p.x, y, p.z);
                if (!NearPlayer(spot, PlayerStartle * 2f)) return true;
            }
            spot = Vector3.zero;
            return false;
        }

        private float GroundAt(Vector3 at, float fallback)
        {
            var origin = new Vector3(at.x, Mathf.Max(at.y, fallback) + 1.5f, at.z);
            if (Physics.Raycast(origin, Vector3.down, out var hit, 8f,Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && hit.normal.y >= .75f)
                return hit.point.y;
            return fallback;
        }

        private bool NearPlayer(Vector3 at, float radius)
        {
            float r2 = radius * radius;
            foreach (var m in _motors)
            {
                if (m == null || !m.isActiveAndEnabled) continue;
                var d = m.transform.position - at;
                if (Mathf.Abs(d.y) < 3f && d.x * d.x + d.z * d.z < r2) return true;
            }
            return false;
        }

        private bool LooseSlipperNear(Vector3 at, float radius)
        {
            float r2 = radius * radius;
            for (int k = 0; k < _slipperCount; k++)
            {
                var s = _slippers[k];
                if (s == null || s.State == SlipperState.Held) continue;
                var d = _slipperNow[k] - at;
                if (Mathf.Abs(d.y) < 2f && d.x * d.x + d.z * d.z < r2 && (_slipperNow[k] - _slipperPrev[k]).sqrMagnitude > 1e-6f) return true;
            }
            return false;
        }

        // A long glide in: seek the spot with arrival slowing, flare with a few beats at the end.
        private void StepLanding(int i, float dt, float blendBank, float blendWing)
        {
            _stateTime[i] += dt;
            var to = _landAt[i] - _birdPos[i]; float dist = to.magnitude;
            if (dist < .25f || _stateTime[i] > 30f)
            {
                if (dist >= .25f) { BeginTakeoff(i); return; }
                _birdPos[i] = _landAt[i]; _birdVel[i] = Vector3.zero;
                _mode[i] = ModeGrounded; _stayLeft[i] = Range(8f, 20f); _act[i] = 0; _actLeft[i] = Range(.6f, 1.6f); _pitch[i] = 0f;
                _bank[i] = 0f; _flapsLeft[i] = 0;
                _birdRoot[i].SetPositionAndRotation(_birdPos[i], Quaternion.Euler(0f, _heading[i], 0f));
                return;
            }
            var desired = to / dist * Mathf.Clamp(dist * .6f, 1.2f, 9f);
            var accel = (desired - _birdVel[i]) * 2f;
            if (accel.sqrMagnitude > 49f) accel = accel.normalized * 7f;
            _birdVel[i] += accel * dt;
            _birdPos[i] += _birdVel[i] * dt;
            if (dist < 3f && _flapsLeft[i] == 0 && _glideLeft[i] > 50f) { _flapsLeft[i] = 3; _flapPhase[i] = 0f; _flapRate[i] = Range(4.2f, 5f); }
            PoseFlying(i, dt, _birdVel[i], blendBank, .4f);
            if (_flapsLeft[i] > 0) StepWings(i, dt, 0f, blendWing);
            else SetWings(i, Mathf.Lerp(_wingAngle[i], 6f, blendWing));
        }

        // ⚠️ On the ground a bird is a little street character: wings tucked, short hops, pecks
        // and head turns, never standing frozen, never walking off the landing square.
        private void StepGrounded(int i, float dt, float blendWing)
        {
            SetWings(i, Mathf.Lerp(_wingAngle[i], -14f, blendWing));
            _stayLeft[i] -= dt;
            var pos = _birdPos[i];
            if (_stayLeft[i] <= 0f || NearPlayer(pos, PlayerStartle) || LooseSlipperNear(pos, SlipperStartle)) { BeginTakeoff(i); return; }

            _actTime[i] += dt;
            float yaw = _heading[i], pitch = 0f, lift = 0f;
            switch (_act[i])
            {
                case 1: // hop
                {
                    float t = Mathf.Clamp01(_actTime[i] / .32f);
                    pos = Vector3.Lerp(_hopFrom[i], _hopTo[i], t);
                    lift = Mathf.Sin(t * Mathf.PI) * .12f;
                    break;
                }
                case 2: // peck: two quick dips of the whole body
                {
                    float t = Mathf.Clamp01(_actTime[i] / .7f);
                    pitch = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 2f)) * 38f;
                    break;
                }
                case 3: // turn
                {
                    float t = Mathf.Clamp01(_actTime[i] / .4f);
                    yaw = Mathf.LerpAngle(_yawFrom[i], _yawTo[i], t * t * (3f - 2f * t));
                    _heading[i] = yaw;
                    break;
                }
            }
            _birdPos[i] = pos;
            _birdRoot[i].SetPositionAndRotation(pos + Vector3.up * lift, Quaternion.Euler(pitch, yaw, 0f));

            _actLeft[i] -= dt;
            if (_actLeft[i] > 0f) return;
            // Next act. Mostly pecks and hops, a turn now and then, a pause in between.
            _actTime[i] = 0f;
            double roll = _random.NextDouble();
            if (_act[i] != 0) { _act[i] = 0; _actLeft[i] = Range(.4f, 1.4f); return; }
            if (roll < .35)
            {
                float h = _heading[i] * Mathf.Deg2Rad;
                var next = pos + new Vector3(Mathf.Sin(h), 0f, Mathf.Cos(h)) * Range(.2f, .5f);
                if (Mathf.Abs(next.x - CourtCentre.x) > CourtHalf || Mathf.Abs(next.z - CourtCentre.z) > CourtHalf)
                { BeginTurn(i, Mathf.Atan2(CourtCentre.x - pos.x, CourtCentre.z - pos.z) * Mathf.Rad2Deg); return; }
                next.y = GroundAt(next, pos.y);
                _act[i] = 1; _hopFrom[i] = pos; _hopTo[i] = next; _actLeft[i] = .32f;
            }
            else if (roll < .75) { _act[i] = 2; _actLeft[i] = .7f; }
            else BeginTurn(i, _heading[i] + (_random.NextDouble() < .5 ? -1f : 1f) * Range(40f, 120f));
        }

        private void BeginTurn(int i, float toYaw)
        {
            _act[i] = 3; _actTime[i] = 0f; _actLeft[i] = .4f; _yawFrom[i] = _heading[i]; _yawTo[i] = toYaw;
        }

        private void BeginTakeoff(int i)
        {
            float h = _heading[i] * Mathf.Deg2Rad;
            _mode[i] = ModeTakeoff; _stateTime[i] = 0f;
            _birdVel[i] = new Vector3(Mathf.Sin(h), 0f, Mathf.Cos(h)) * 2f + Vector3.up * 3f;
            _flapsLeft[i] = 8; _flapPhase[i] = 0f; _flapRate[i] = Range(4.4f, 5.2f);
            _landAt[i] = _birdPos[i];
        }

        // Beat hard, climb steeply, then hand the bird back to its flock once it has flying speed.
        private void StepTakeoff(int i, float dt, float blendBank, float blendWing)
        {
            _stateTime[i] += dt;
            var vel = _birdVel[i];
            var flat = Flat(vel); var fwd = flat.sqrMagnitude > 1e-4f ? flat.normalized : Vector3.forward;
            var desired = fwd * 8.5f + Vector3.up * 3.5f;
            var accel = desired - vel;
            if (accel.sqrMagnitude > 36f) accel = accel.normalized * 6f;
            vel += accel * dt;
            _birdVel[i] = vel; _birdPos[i] += vel * dt;
            if (_flapsLeft[i] < 2) _flapsLeft[i] = 2;
            PoseFlying(i, dt, vel, blendBank, .6f);
            StepWings(i, dt, vel.y, blendWing);
            if ((vel.magnitude >= 7f && _birdPos[i].y - _landAt[i].y > 5f) || _stateTime[i] > 6f)
            {
                _mode[i] = ModeFlying;
                _flapRate[i] = Range(3.2f, 4.1f);
            }
        }

        private void StepDead(int i, float dt)
        {
            _stayLeft[i] -= dt;
            if (_stayLeft[i] > 0f) return;
            // ⚠️ Respawn out of sight: behind the flock, away from the court, at the flock's
            // height. It rejoins through ordinary cohesion, never popping in beside anyone.
            int f = _flockOf[i];
            bool flying = FlockMean(f, out var centroid, out var velocity);
            var away = Flat(centroid - CourtCentre);
            away = away.sqrMagnitude > .01f ? away.normalized : Vector3.forward;
            var p = centroid + away * 30f + Vector3.up * 4f;
            var fromSky = Flat(p - SkyCentre);
            if (fromSky.magnitude > SkyRadius * .9f)
            {
                var edge = SkyCentre + fromSky.normalized * SkyRadius * .9f;
                p = new Vector3(edge.x, p.y, edge.z);
            }
            p.y = Mathf.Max(p.y, WaterY + CourtClearance + 2f);
            if (!flying || Flat(velocity).sqrMagnitude < 1f) velocity = new Vector3(-away.z, 0f, away.x) * 9f;
            _birdPos[i] = p; _birdVel[i] = velocity;
            _heading[i] = Mathf.Atan2(velocity.x, velocity.z) * Mathf.Rad2Deg; _bank[i] = 0f;
            _mode[i] = ModeFlying; _flapsLeft[i] = 0; _glideLeft[i] = Range(0f, 2f); _flapRate[i] = Range(3.2f, 4.1f);
            _birdRoot[i].gameObject.SetActive(true);
            _birdRoot[i].SetPositionAndRotation(p, Quaternion.Euler(0f, _heading[i], 0f));
        }

        // ---------------------------------------------------------------- the slipper hit

        // ⚠️⚠️ The slipper is NEVER affected. This reads its transform position and its public
        // State, and nothing else: no collider, no physics, no change to its flight, its state or
        // anything networked. The hit is a segment test against last frame's position, so a fast
        // slipper cannot tunnel through a bird between frames.
        private void CheckSlipperHits(float frameDt)
        {
            float thrown2 = ThrownSpeed * frameDt * ThrownSpeed * frameDt;
            float lowLine = Mathf.Max(WaterY, CourtGroundY) + LowFlightHit;
            for (int k = 0; k < _slipperCount; k++)
            {
                var s = _slippers[k];
                var a = _slipperPrev[k]; var b = _slipperNow[k];
                _slipperPrev[k] = b;
                // A carried slipper moves as fast as its runner; only a free one can hit.
                if (s == null || s.State == SlipperState.Held || (b - a).sqrMagnitude < thrown2) continue;
                for (int i = 0; i < _birdCount; i++)
                {
                    int mode = _mode[i];
                    if (mode == ModeDead) continue;
                    if (mode == ModeFlying && _birdPos[i].y > lowLine) continue;
                    var body = _birdBody[i] != null ? _birdBody[i].position : _birdPos[i];
                    if (DistanceToSegmentSq(body, a, b) > HitRadius * HitRadius) continue;
                    Burst(i, body);
                }
            }
        }

        private static float DistanceToSegmentSq(Vector3 p, Vector3 a, Vector3 b)
        {
            var ab = b - a; float len2 = ab.sqrMagnitude;
            float t = len2 > 1e-8f ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / len2) : 0f;
            return (a + ab * t - p).sqrMagnitude;
        }

        private void Burst(int i, Vector3 at)
        {
            _mode[i] = ModeDead;
            _stayLeft[i] = Range(20f, 40f);
            _birdRoot[i].gameObject.SetActive(false);
            Bursts++;
            // Feathers settle on the sand under the burst, or on the water surface over the cove.
            float floor = Mathf.Max(GroundAt(at, Mathf.Max(CourtGroundY, WaterY)), WaterY);
            if (floor > at.y) floor = at.y - .05f;
            if (_featherMats != null)
            {
                int count = _random.Next(25, 36);
                for (int k = 0; k < count; k++) EmitFeather(at, floor, false);
                for (int k = 0; k < 6; k++) EmitFeather(at, floor, true);
            }
            BirdBurst?.Invoke(at);
        }

        // ---------------------------------------------------------------- feathers

        private int TakeFeatherSlot()
        {
            int oldest = 0; float leastLeft = float.MaxValue;
            for (int k = 0; k < _featherBuilt; k++)
            {
                float left = _fLife[k] - _fAge[k];
                if (_fLife[k] <= 0f) return k;
                if (left < leastLeft) { leastLeft = left; oldest = k; }
            }
            if (_featherBuilt < FeatherCapacity)
            {
                var go = new GameObject("Feather");
                go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = _featherMesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = _featherMats[_featherBuilt % _featherMats.Length];
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                go.SetActive(false);
                _fT[_featherBuilt] = go.transform;
                return _featherBuilt++;
            }
            return oldest;
        }

        // ⚠️ Feathers fling out and up, then air drag wins and they drift down slowly with a
        // side-to-side flutter, tumbling, and shrink away over 2 to 3.5 s. The puff is a handful
        // of larger pieces that swell and vanish in half a second, the "pop" of the burst.
        private void EmitFeather(Vector3 at, float floor, bool puff)
        {
            int k = TakeFeatherSlot();
            var dir = new Vector3(Range(-1f, 1f), Range(-.2f, 1f), Range(-1f, 1f));
            if (dir.sqrMagnitude < 1e-3f) dir = Vector3.up;
            dir.Normalize();
            _fPuff[k] = puff;
            _fPos[k] = at + dir * (puff ? .05f : .1f);
            _fVel[k] = puff ? dir * Range(.4f, 1f) : dir * Range(2.5f, 6f) + Vector3.up * Range(1f, 3f);
            _fRot[k] = Quaternion.Euler(Range(0f, 360f), Range(0f, 360f), Range(0f, 360f));
            var axis = new Vector3(Range(-1f, 1f), Range(-1f, 1f), Range(-1f, 1f));
            _fSpinAxis[k] = axis.sqrMagnitude > 1e-3f ? axis.normalized : Vector3.up;
            _fSpin[k] = Range(200f, 720f);
            var side = new Vector3(Range(-1f, 1f), 0f, Range(-1f, 1f));
            _fSide[k] = side.sqrMagnitude > 1e-3f ? side.normalized : Vector3.right;
            _fPhase[k] = Range(0f, Mathf.PI * 2f);
            _fAge[k] = 0f;
            _fLife[k] = puff ? Range(.35f, .55f) : Range(2f, 3.5f);
            _fSize[k] = puff ? Range(2.4f, 3.4f) : Range(.8f, 1.3f);
            _fFloor[k] = floor;
            var t = _fT[k];
            t.SetPositionAndRotation(_fPos[k], _fRot[k]);
            t.localScale = Vector3.one * (puff ? _fSize[k] * .4f : _fSize[k]);
            t.gameObject.SetActive(true);
        }

        private void StepFeathers(float dt)
        {
            float drag = Mathf.Exp(-3.2f * dt), spinDrag = Mathf.Exp(-.8f * dt);
            for (int k = 0; k < _featherBuilt; k++)
            {
                if (_fLife[k] <= 0f) continue;
                _fAge[k] += dt;
                float u = _fAge[k] / _fLife[k];
                var t = _fT[k];
                if (u >= 1f) { _fLife[k] = 0f; t.gameObject.SetActive(false); continue; }
                float scale;
                if (_fPuff[k])
                {
                    _fPos[k] += _fVel[k] * dt;
                    scale = _fSize[k] * (u < .3f ? Mathf.Lerp(.4f, 1f, u / .3f) : 1f - (u - .3f) / .7f);
                }
                else
                {
                    // Gravity at a third of g against a strong drag gives a slow terminal drift.
                    _fVel[k] += Vector3.up * (-3.3f * dt);
                    _fVel[k] *= drag;
                    _fPhase[k] += dt * 5f;
                    var flutter = _fSide[k] * (Mathf.Sin(_fPhase[k]) * .7f);
                    _fPos[k] += (_fVel[k] + flutter) * dt;
                    if (_fPos[k].y < _fFloor[k] + .01f) { _fPos[k].y = _fFloor[k] + .01f; _fVel[k] = Vector3.zero; _fSpin[k] = 0f; }
                    _fSpin[k] *= spinDrag;
                    _fRot[k] = Quaternion.AngleAxis(_fSpin[k] * dt, _fSpinAxis[k]) * _fRot[k];
                    scale = _fSize[k] * (u < .6f ? 1f : 1f - (u - .6f) / .4f);
                }
                t.SetPositionAndRotation(_fPos[k], _fRot[k]);
                t.localScale = Vector3.one * Mathf.Max(scale, .001f);
            }
        }

        // ---------------------------------------------------------------- fish

        private void StepFish(float dt)
        {
            float turnBlend = 1f - Mathf.Exp(-6f * dt);
            for (int s = 0; s < _schoolCount; s++)
            {
                int start = _schoolStart[s], end = start + _schoolSize[s];
                if (end <= start) continue;
                _schoolGoalTimer[s] -= dt;
                if (_schoolGoalTimer[s] <= 0f) PickSchoolGoal(s);

                var centroid = Vector3.zero;
                for (int i = start; i < end; i++) centroid += _fishPos[i];
                centroid /= end - start;

                _nextStartle[s] -= dt;
                if (_nextStartle[s] <= 0f)
                {
                    // ⚠️ A startle is a velocity kick away from the school's own middle, then the
                    // ordinary cohesion regroups it: no scripted return path to go wrong.
                    _nextStartle[s] = Range(25f, 60f);
                    _scatterLeft[s] = FishScatterTime;
                    _regroupLeft[s] = FishScatterTime + FishRegroupTime;
                    for (int i = start; i < end; i++)
                    {
                        var away = _fishPos[i] - centroid;
                        if (away.sqrMagnitude < 1e-4f) { float h = Range(0f, Mathf.PI * 2f); away = new Vector3(Mathf.Cos(h), 0f, Mathf.Sin(h)); }
                        away.y *= .3f;
                        _fishVel[i] = away.normalized * FishScatterSpeed * Range(.7f, 1f);
                    }
                }
                bool scattering = _scatterLeft[s] > 0f, regrouping = !scattering && _regroupLeft[s] > 0f;
                if (_scatterLeft[s] > 0f) _scatterLeft[s] -= dt;
                if (_regroupLeft[s] > 0f) _regroupLeft[s] -= dt;

                var centre = _schoolCentre[s]; var goal = _schoolGoal[s];
                float low = _schoolLow[s], high = _schoolHigh[s], edge = SchoolRadius * .8f;
                float accel = scattering ? FishScatterAccel : FishAccel;
                float maxSpeed = scattering || regrouping ? FishScatterSpeed : FishMaxSpeed;
                float maxRise = scattering ? 1.2f : .5f;
                for (int i = start; i < end; i++)
                {
                    var pos = _fishPos[i]; var vel = _fishVel[i];
                    var steer = Vector3.zero;
                    if (!scattering)
                    {
                        Vector3 sep = Vector3.zero, ali = Vector3.zero, coh = Vector3.zero; int n = 0;
                        for (int j = start; j < end; j++)
                        {
                            if (j == i) continue;
                            var d = _fishPos[j] - pos; float sq = d.sqrMagnitude;
                            if (sq < FishSeparation * FishSeparation && sq > 1e-5f) sep -= d / sq;
                            ali += _fishVel[j]; coh += _fishPos[j]; n++;
                        }
                        steer += sep * .35f;
                        if (n > 0)
                        {
                            steer += (ali / n - vel) * .8f;
                            steer += (coh / n - pos) * (regrouping ? .9f : .35f);
                        }
                        var toGoal = goal - pos;
                        if (toGoal.sqrMagnitude > 1e-4f) steer += (toGoal.normalized * _fishCruise[i] - vel) * .6f;
                        if (steer.sqrMagnitude > accel * accel) steer = steer.normalized * accel;
                    }
                    var keep = Vector3.zero;
                    var fromCentre = Flat(pos - centre); float dist = fromCentre.magnitude;
                    if (dist > edge) keep -= fromCentre / dist * (dist - edge) * 1.5f;
                    if (pos.y < low + .3f) keep.y += (low + .3f - pos.y) * 3f;
                    if (pos.y > high - .3f) keep.y -= (pos.y - (high - .3f)) * 3f;

                    vel += (steer + keep) * dt;
                    if (scattering) vel *= 1f - Mathf.Min(1f, .6f * dt); // the burst spends itself
                    vel.y = Mathf.Clamp(vel.y, -maxRise, maxRise);
                    float speed = vel.magnitude;
                    if (speed < 1e-3f) { vel = _fishRoot[i].forward * FishMinSpeed; speed = FishMinSpeed; }
                    else if (speed < FishMinSpeed) { vel *= FishMinSpeed / speed; speed = FishMinSpeed; }
                    else if (speed > maxSpeed) { vel *= maxSpeed / speed; speed = maxSpeed; }
                    pos += vel * dt;
                    // ⚠️ The seabed and the surface are hard limits, not suggestions: a fish poking
                    // out of the water or through a reef reads worse than a stall. The pull above
                    // keeps this clamp to millimetres in practice.
                    pos.y = Mathf.Clamp(pos.y, low, high);
                    _fishVel[i] = vel; _fishPos[i] = pos;

                    var root = _fishRoot[i];
                    var look = new Vector3(vel.x, vel.y * .7f, vel.z);
                    var rotation = look.sqrMagnitude > 1e-6f ? Quaternion.Slerp(root.rotation, Quaternion.LookRotation(look), turnBlend) : root.rotation;
                    root.SetPositionAndRotation(pos, rotation);

                    // ⚠️ The tail beats faster and wider as the fish swims faster, which is what
                    // makes a startle read as effort rather than a speed-up.
                    _tailPhase[i] += dt * (5f + speed * 7f);
                    if (_tailPhase[i] > Mathf.PI * 2f) _tailPhase[i] -= Mathf.PI * 2f;
                    if (_tail[i] != null)
                        _tail[i].localRotation = _tailRest[i] * Quaternion.Euler(0f, (14f + speed * 6f) * Mathf.Sin(_tailPhase[i]), 0f);
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Sky band: the floor and ceiling rings of the birds' cylinder.
            Gizmos.color = new Color(1f, .85f, .3f);
            DrawCircle(new Vector3(SkyCentre.x, WaterY + SkyHeight.x, SkyCentre.z), SkyRadius);
            DrawCircle(new Vector3(SkyCentre.x, WaterY + SkyHeight.y, SkyCentre.z), SkyRadius);
            // The court keep-out: no flying bird below this ring inside it.
            Gizmos.color = new Color(1f, .3f, .2f);
            DrawCircle(new Vector3(CourtCentre.x, WaterY + CourtClearance, CourtCentre.z), CourtKeepOut);
            // The landing square, drawn at the fallback ground height.
            Gizmos.DrawWireCube(new Vector3(CourtCentre.x, CourtGroundY, CourtCentre.z), new Vector3(CourtHalf * 2f, .05f, CourtHalf * 2f));
            // The dip height, for reading how low a dipping flock skims.
            Gizmos.color = new Color(1f, .6f, .2f, .5f);
            DrawCircle(new Vector3(SkyCentre.x, WaterY + DipHeight, SkyCentre.z), SkyRadius * .7f);
            if (SchoolCentres == null) return;
            Gizmos.color = new Color(.3f, 1f, .5f);
            for (int s = 0; s < SchoolCentres.Length; s++)
            {
                var c = SchoolCentres[s];
                float floor = SchoolFloor != null && s < SchoolFloor.Length ? SchoolFloor[s] : c.y - 3f;
                float low = floor + .4f, high = WaterY - .5f;
                DrawCircle(new Vector3(c.x, low, c.z), SchoolRadius);
                DrawCircle(new Vector3(c.x, high, c.z), SchoolRadius);
                Gizmos.DrawLine(new Vector3(c.x, floor, c.z), new Vector3(c.x, WaterY, c.z));
            }
        }

        private static void DrawCircle(Vector3 centre, float radius)
        {
            const int segments = 48;
            var previous = centre + new Vector3(radius, 0f, 0f);
            for (int k = 1; k <= segments; k++)
            {
                float a = k * Mathf.PI * 2f / segments;
                var next = centre + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }
#endif
    }
}
