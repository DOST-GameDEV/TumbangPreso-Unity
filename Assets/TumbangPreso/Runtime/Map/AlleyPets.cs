using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// The Eskinita Alley's LIVE cats and dogs, and the chase that breaks out between them now and
    /// then (owner, 2026-10-09: "can you make actual moving cats and dogs, with cat and dog chases as
    /// a background life event. sfx too"). The shipped maps' street animals (`AmbientLife`: skinned
    /// voxel aspin and pusakal on authored routes) have no sitting, no perching, no sound and no way
    /// for two animals to act on each other, and their look is the older boxy one, so the alley has
    /// its own: the round models of tools/author_eskinita_pets.py, posed part by part the way the
    /// alley's chickens are (`AlleyChickens`), on the same kind of baked floor.
    ///
    /// CALM. A dog lies in the shade, sits, scratches, sniffs along to another spot, trots, and wags
    /// at a player who stands near (it never follows). A cat sits, loafs, grooms, stretches, walks,
    /// hops up onto a low perch and down again; a LEDGE cat lives up on one ledge and walks its length.
    /// A cat gives way to a player who walks right up to it (up a perch if one is in reach).
    ///
    /// THE EVENT, every <see cref="ChaseEvery"/> seconds (once or twice a round): a dog notices a
    /// cat and barks, the cat bolts along the floor and escapes UP a perch, the dog skids in below,
    /// barks up at it for a few seconds, gives up with a whine and trots home. Now and then the
    /// joke runs the other way (the cat stands its ground, swats, and the dog backs off), or the dog
    /// runs through the chickens instead (they scatter: a running dog frightens them as a player does).
    ///
    /// A thrown slipper through a cat or a dog makes it hiss or yelp and bolt. It is never burst.
    /// </summary>
    // ⚠️ SCENERY. Local only, never on the wire, never a collider, never a physics body, a private
    // random stream. Two peers see different animals and that is correct: nothing gameplay reads them.
    // ⚠️ NO NAVMESH AND NO RAYCASTS AT RUNTIME. The builder (EskinitaAlleyPetsAuthor) bakes one grid
    // of the alley's floors from collision.glb (walkable cells and their heights: the terraces and
    // the wall to wall stairs, never the chalk square, a bounce tarp, a prop or a wall's foot) and
    // the perches with the floor cell beside each. Every route is an A* path over that grid, pulled
    // straight where the straight line stays on it.
    public sealed class AlleyPets : MonoBehaviour
    {
        [Serializable]
        public sealed class Pet
        {
            public string Name;
            public Transform Root;
            public bool Dog;
            public Vector3 Home;
            public Vector3[] Spots = Array.Empty<Vector3>();   // floor cells it rests at (a dog's are in a wall's shade)
            public bool Ledge;                                 // lives on the line LedgeA..LedgeB and never leaves it
            public Vector3 LedgeA, LedgeB;
        }

        [Serializable]
        public sealed class Perch
        {
            public Vector3 Top;     // where a cat sits
            public Vector3 Foot;    // the walkable floor cell beside it
        }

        public Pet[] Pets = Array.Empty<Pet>();
        public Perch[] Perches = Array.Empty<Perch>();
        public Vector2 GridOrigin;                      // world x, z of the corner of cell (0, 0)
        public float Cell = .25f;
        public int Width, Height;
        public bool[] Walkable = Array.Empty<bool>();   // Height rows of Width
        public float[] Heights = Array.Empty<float>();  // the floor's y per cell
        public AlleyChickens Chickens;
        public AlleyLifeSound Sound;
        /// <summary>Seconds between events. A round is a few minutes: this is once or twice in it.</summary>
        public Vector2 ChaseEvery = new Vector2(75f, 160f);

        /// <summary>Stand-in bodies for a probe or the editor's motion sheet. Never set by the game.</summary>
        [NonSerialized] public Vector3[] ProbeThreats = Array.Empty<Vector3>();
        /// <summary>How many events have started since Begin (for probes).</summary>
        public int Events { get; private set; }
        /// <summary>True while an event runs.</summary>
        public bool EventRunning => _event != EventNone;
        /// <summary>The dog and the cat of the event now running or last run (the cat is null when the dog ran at the chickens).</summary>
        public Transform EventDog { get; private set; }
        public Transform EventCat { get; private set; }

        private const int ActStand = 0, ActSit = 1, ActLie = 2, ActGroom = 3, ActStretch = 4, ActMove = 5, ActJump = 6,
            ActAlert = 7, ActSkid = 8, ActBark = 9, ActSwat = 10, ActArch = 11, ActBack = 12, ActWag = 13;
        private static readonly string[] ActNames = { "stand", "sit", "lie", "groom", "stretch", "move", "JUMP", "alert", "SKID", "BARK", "SWAT", "arch", "back-off", "wag" };
        private const int EventNone = 0, EventChase = 1, EventSwat = 2, EventChickens = 3;
        private const float MaxStep = 1f / 20f, RescanSeconds = 1f, ThrownSpeed = 4f;
        private const int SlipperCapacity = 16;

        private sealed class Actor
        {
            public Pet Data; public int Index;
            public Transform Root, Trunk, Head, Tail, Fl, Fr, Bl, Br;
            public Quaternion TrunkRest, HeadRest, TailRest, FlRest, FrRest, BlRest, BrRest;
            public Vector3 TrunkAt;
            public float Hip, Span;
            public Vector3 Pos; public float Heading;
            public int Act; public float Time, Len;
            public readonly List<Vector3> Path = new List<Vector3>();
            public int PathAt; public float Speed, Stride; public bool Sniff;
            public int Perch = -1, PerchGoal = -1;
            public Vector3 JumpFrom, JumpTo; public float JumpHeight;
            public Vector3 Face; public bool Facing;
            public float Quiet, UpLeft, BarkLeft, Wag, Jolt, LookYaw, LookLeft, PurrLeft, Hurt;
            public bool Busy;       // in the event: its own calm choices are off
            public int Then = -1;   // the act to take on arriving
            // The pose, eased every step toward what the act asks for.
            public float Pitch, Drop, Lift, HeadPitch, HeadYaw, HeadRoll, TailPitch, TailYaw, AFl, AFr, ABl, ABr, FrontScale = 1f;
        }

        private System.Random _random;
        private Actor[] _actors;
        private int[] _region;
        private float _nextEvent, _rescanLeft, _eventTime, _repath;
        private int _event, _phase, _dog = -1, _cat = -1, _eventPerch = -1;
        private Vector3 _eventGoal;
        private CharacterMotor[] _motors = Array.Empty<CharacterMotor>();
        private readonly Slipper[] _slippers = new Slipper[SlipperCapacity];
        private readonly Vector3[] _slipperPrev = new Vector3[SlipperCapacity], _slipperNow = new Vector3[SlipperCapacity];
        private int _slipperCount;
        private Vector3[] _scares = Array.Empty<Vector3>();
        // A* scratch, allocated once.
        private float[] _cost; private int[] _from, _stamp; private int _search;
        private readonly List<int> _open = new List<int>();
        private readonly List<Vector3> _raw = new List<Vector3>();

        private void OnEnable() => Visual.MatchFlair.Presented += OnImpact;
        private void OnDisable() => Visual.MatchFlair.Presented -= OnImpact;

        private void Start() => Begin(Guid.NewGuid().GetHashCode());

        private void Update()
        {
            float raw = Time.deltaTime;
            if (raw <= 0f || _actors == null) return;
            Step(raw > MaxStep ? MaxStep : raw);
        }

        private float Range(float a, float b) => a + (float)_random.NextDouble() * (b - a);
        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
        private static Vector3 Dir(float yaw) { float h = yaw * Mathf.Deg2Rad; return new Vector3(Mathf.Sin(h), 0f, Mathf.Cos(h)); }
        private static float Yaw(Vector3 v) => Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
        private void Say(string cue, Vector3 at, float gain = 1f) { if (Sound != null) Sound.Play(cue, at + Vector3.up * .2f, gain); }

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

        /// <summary>Reads the animals where the builder stood them and starts their lives.</summary>
        public void Begin(int seed)
        {
            _random = new System.Random(seed);
            var list = new List<Actor>();
            for (int i = 0; i < Pets.Length; i++)
            {
                var p = Pets[i];
                if (p == null || p.Root == null) continue;
                var a = new Actor { Data = p, Index = list.Count, Root = p.Root };
                a.Trunk = FindChild(p.Root, "trunk"); a.Head = FindChild(p.Root, "head"); a.Tail = FindChild(p.Root, "tail");
                a.Fl = FindChild(p.Root, "leg_fl"); a.Fr = FindChild(p.Root, "leg_fr"); a.Bl = FindChild(p.Root, "leg_bl"); a.Br = FindChild(p.Root, "leg_br");
                if (a.Trunk != null) { a.TrunkRest = a.Trunk.localRotation; a.TrunkAt = a.Trunk.localPosition; a.Hip = a.TrunkAt.y; }
                if (a.Head != null) a.HeadRest = a.Head.localRotation;
                if (a.Tail != null) a.TailRest = a.Tail.localRotation;
                if (a.Fl != null) a.FlRest = a.Fl.localRotation;
                if (a.Fr != null) a.FrRest = a.Fr.localRotation;
                if (a.Bl != null) a.BlRest = a.Bl.localRotation;
                if (a.Br != null) a.BrRest = a.Br.localRotation;
                a.Span = a.Fl != null && a.Bl != null ? Mathf.Abs(a.Fl.localPosition.z - a.Bl.localPosition.z) : a.Hip * 1.4f;
                a.Pos = p.Root.position; a.Heading = p.Root.eulerAngles.y;
                a.Act = p.Dog ? ActLie : ActSit; a.Len = Range(2f, 7f);
                list.Add(a);
            }
            _actors = list.ToArray();
            int n = Width * Height;
            _cost = new float[n]; _from = new int[n]; _stamp = new int[n]; _region = new int[n];
            // Regions: which cells can be walked between, so an event never starts on two islands.
            int next = 0; var queue = new Queue<int>();
            for (int k = 0; k < n; k++) _region[k] = -1;
            for (int k = 0; k < n; k++)
            {
                if (_region[k] >= 0 || k >= Walkable.Length || !Walkable[k]) continue;
                _region[k] = next; queue.Enqueue(k);
                while (queue.Count > 0)
                {
                    int c = queue.Dequeue(), ci = c % Width, cj = c / Width;
                    for (int d = 0; d < 4; d++)
                    {
                        int ni = ci + (d == 0 ? 1 : d == 1 ? -1 : 0), nj = cj + (d == 2 ? 1 : d == 3 ? -1 : 0);
                        if (!Step(ci, cj, ni, nj)) continue;
                        int q = nj * Width + ni;
                        if (_region[q] < 0) { _region[q] = next; queue.Enqueue(q); }
                    }
                }
                next++;
            }
            _event = EventNone; _dog = _cat = -1; Events = 0;
            _nextEvent = Range(ChaseEvery.x * .35f, ChaseEvery.x);
            _rescanLeft = 0f; _slipperCount = 0;
        }

        /// <summary>One step of every animal and of the event. Update calls it; so does the motion sheet.</summary>
        public void Step(float dt)
        {
            if (_actors == null || dt <= 0f) return;
            _rescanLeft -= dt;
            if (_rescanLeft <= 0f) { _rescanLeft = RescanSeconds; Rescan(); }
            for (int k = 0; k < _slipperCount; k++) _slipperNow[k] = _slippers[k] != null ? _slippers[k].transform.position : _slipperPrev[k];
            CheckSlipperHits(dt);
            if (_event == EventNone)
            {
                _nextEvent -= dt;
                if (_nextEvent <= 0f) { _nextEvent = Range(ChaseEvery.x, ChaseEvery.y); TryStartEvent(-1); }
            }
            else StepEvent(dt);
            int running = 0;
            foreach (var a in _actors) { StepActor(a, dt); if (a.Data.Dog && a.Act == ActMove && a.Speed > 2.4f) running++; }
            for (int k = 0; k < _slipperCount; k++) _slipperPrev[k] = _slipperNow[k];
            // ⚠️ A RUNNING DOG FRIGHTENS THE CHICKENS as a player does: they are told where it is.
            if (Chickens != null)
            {
                if (_scares.Length != running) _scares = new Vector3[running];
                int s = 0;
                foreach (var a in _actors) if (a.Data.Dog && a.Act == ActMove && a.Speed > 2.4f) _scares[s++] = a.Pos;
                Chickens.Scares = _scares;
            }
        }

        /// <summary>Starts an event now if a dog and a cat are placed for one (the motion sheet and probes).
        /// kind: 1 the chase, 2 the cat stands and swats, 3 the dog runs through the chickens, -1 any.</summary>
        public bool StageEvent(int kind) => _event == EventNone && TryStartEvent(kind);

        /// <summary>One line per animal: act and place; then the event's phase.</summary>
        public string Describe()
        {
            if (_actors == null) return "not begun";
            var s = new StringBuilder();
            foreach (var a in _actors)
                s.Append(a.Data.Name).Append(' ').Append(ActNames[a.Act]).Append(a.Act == ActMove ? (a.Speed > 2.4f ? "(RUN)" : a.Speed > 1f ? "(trot)" : "(walk)") : "")
                    .Append(a.Perch >= 0 ? "(up)" : "").Append(" (").Append(a.Pos.x.ToString("0.0")).Append(", ").Append(a.Pos.y.ToString("0.0")).Append(", ").Append(a.Pos.z.ToString("0.0")).Append(")  ");
            s.Append(_event == EventNone ? "no event" : "EVENT " + _event + " phase " + _phase);
            return s.ToString();
        }

        private void Rescan()
        {
            _motors = FindObjectsByType<CharacterMotor>();
            var found = FindObjectsByType<Slipper>();
            int count = Mathf.Min(found.Length, SlipperCapacity);
            var prev = new Vector3[count];
            for (int k = 0; k < count; k++)
            {
                prev[k] = found[k].transform.position;
                for (int j = 0; j < _slipperCount; j++) if (ReferenceEquals(_slippers[j], found[k])) { prev[k] = _slipperPrev[j]; break; }
            }
            for (int k = 0; k < SlipperCapacity; k++)
            {
                _slippers[k] = k < count ? found[k] : null;
                if (k < count) _slipperPrev[k] = prev[k];
            }
            _slipperCount = count;
        }

        // ---------------------------------------------------------------- the floor

        private int CellOf(Vector3 p)
        {
            int i = Mathf.FloorToInt((p.x - GridOrigin.x) / Cell), j = Mathf.FloorToInt((p.z - GridOrigin.y) / Cell);
            if (i < 0 || j < 0 || i >= Width || j >= Height) return -1;
            int k = j * Width + i;
            return k < Walkable.Length && Walkable[k] ? k : -1;
        }

        private Vector3 Centre(int k) => new Vector3(GridOrigin.x + (k % Width + .5f) * Cell, Heights[k], GridOrigin.y + (k / Width + .5f) * Cell);

        /// <summary>May a body step from cell (i, j) to its neighbour (ni, nj)? Both floor, nearly level (a stair, not a drop).</summary>
        private bool Step(int i, int j, int ni, int nj)
        {
            if (ni < 0 || nj < 0 || ni >= Width || nj >= Height) return false;
            int a = j * Width + i, b = nj * Width + ni;
            if (b >= Walkable.Length || !Walkable[a] || !Walkable[b] || Mathf.Abs(Heights[a] - Heights[b]) > .22f) return false;
            // No cutting a corner: a diagonal needs both of its sides.
            if (ni != i && nj != j && (!Walkable[j * Width + ni] || !Walkable[nj * Width + i])) return false;
            return true;
        }

        private float FloorAt(Vector3 p, float fallback)
        {
            int k = CellOf(p);
            return k >= 0 ? Heights[k] : fallback;
        }

        private bool Clear(Vector3 a, Vector3 b)
        {
            float length = Flat(b - a).magnitude;
            int steps = Mathf.Max(1, Mathf.CeilToInt(length / (Cell * .4f)));
            int last = CellOf(a);
            if (last < 0) return false;
            for (int s = 1; s <= steps; s++)
            {
                int k = CellOf(Vector3.Lerp(a, b, s / (float)steps));
                if (k < 0 || Mathf.Abs(Heights[k] - Heights[last]) > .22f) return false;
                last = k;
            }
            return true;
        }

        private int NearestCell(Vector3 p, float within)
        {
            int k = CellOf(p);
            if (k >= 0) return k;
            int ci = Mathf.FloorToInt((p.x - GridOrigin.x) / Cell), cj = Mathf.FloorToInt((p.z - GridOrigin.y) / Cell), r = Mathf.CeilToInt(within / Cell);
            float best = within * within; int pick = -1;
            for (int j = cj - r; j <= cj + r; j++)
                for (int i = ci - r; i <= ci + r; i++)
                {
                    if (i < 0 || j < 0 || i >= Width || j >= Height || !Walkable[j * Width + i]) continue;
                    var c = Centre(j * Width + i);
                    float d = Flat(c - p).sqrMagnitude;
                    if (d < best && Mathf.Abs(c.y - p.y) < 1.2f) { best = d; pick = j * Width + i; }
                }
            return pick;
        }

        private bool SameRegion(Vector3 a, Vector3 b)
        {
            int ka = NearestCell(a, 1f), kb = NearestCell(b, 1f);
            return ka >= 0 && kb >= 0 && _region[ka] == _region[kb];
        }

        /// <summary>An A* route over the grid from a to b, pulled straight where the line stays on the floor.
        /// False when there is none.</summary>
        private bool Route(Vector3 from, Vector3 to, List<Vector3> path)
        {
            path.Clear();
            int start = NearestCell(from, 1f), goal = NearestCell(to, 1f);
            if (start < 0 || goal < 0 || _region[start] != _region[goal]) return false;
            _search++; _open.Clear();
            _cost[start] = 0f; _from[start] = -1; _stamp[start] = _search; _open.Add(start);
            var goalAt = Centre(goal);
            bool found = start == goal;
            int guard = 0;
            while (!found && _open.Count > 0 && guard++ < 20000)
            {
                int at = 0; float best = float.MaxValue;
                for (int o = 0; o < _open.Count; o++)
                {
                    float f = _cost[_open[o]] + Flat(Centre(_open[o]) - goalAt).magnitude;
                    if (f < best) { best = f; at = o; }
                }
                int c = _open[at]; _open[at] = _open[_open.Count - 1]; _open.RemoveAt(_open.Count - 1);
                if (c == goal) { found = true; break; }
                int ci = c % Width, cj = c / Width;
                for (int dj = -1; dj <= 1; dj++)
                    for (int di = -1; di <= 1; di++)
                    {
                        if ((di == 0 && dj == 0) || !Step(ci, cj, ci + di, cj + dj)) continue;
                        int q = (cj + dj) * Width + ci + di;
                        float g = _cost[c] + (di != 0 && dj != 0 ? 1.4142f : 1f) * Cell;
                        if (_stamp[q] == _search && g >= _cost[q]) continue;
                        bool fresh = _stamp[q] != _search;
                        _cost[q] = g; _from[q] = c; _stamp[q] = _search;
                        if (fresh || !_open.Contains(q)) _open.Add(q);
                    }
            }
            if (!found) return false;
            _raw.Clear();
            for (int c = goal; c >= 0; c = _from[c]) { _raw.Add(Centre(c)); if (c == start) break; }
            _raw.Reverse();
            // Pull it straight: from each kept point, the farthest later point the straight line reaches.
            int i = 0;
            var here = _raw.Count > 0 ? _raw[0] : from;
            while (i < _raw.Count - 1)
            {
                int far = i + 1;
                for (int j = _raw.Count - 1; j > i + 1; j--) if (Clear(here, _raw[j])) { far = j; break; }
                path.Add(_raw[far]); here = _raw[far]; i = far;
            }
            if (path.Count == 0) path.Add(goalAt);
            return true;
        }

        // ---------------------------------------------------------------- what they notice

        private bool Near(Vector3 at, float radius, out Vector3 who)
        {
            float best = radius * radius; bool any = false; who = Vector3.zero;
            foreach (var m in _motors)
            {
                if (m == null || !m.isActiveAndEnabled) continue;
                var p = m.transform.position; var d = p - at;
                float d2 = d.x * d.x + d.z * d.z;
                if (Mathf.Abs(d.y) < 2.2f && d2 < best) { best = d2; who = p; any = true; }
            }
            if (ProbeThreats != null)
                foreach (var p in ProbeThreats)
                {
                    var d = p - at; float d2 = d.x * d.x + d.z * d.z;
                    if (Mathf.Abs(d.y) < 2.2f && d2 < best) { best = d2; who = p; any = true; }
                }
            return any;
        }

        // The peer-visible outcomes `AmbientLife`'s animals react to: the can going down, a thunder strike, an ice shatter.
        private void OnImpact(Visual.MatchFlair.Kind kind, int actor, int subject, Vector3 at, float strength)
        {
            if (_actors == null) return;
            float radius = kind == Visual.MatchFlair.Kind.LataDown ? 6f : kind == Visual.MatchFlair.Kind.Thunder ? 12f : kind == Visual.MatchFlair.Kind.IceShatter ? 5f : 0f;
            if (radius <= 0f) return;
            foreach (var a in _actors)
            {
                if (a.Busy || a.Quiet > 0f || (a.Pos - at).sqrMagnitude > radius * radius) continue;
                a.Quiet = 4f;
                if (a.Data.Dog) { Do(a, ActBark, 1.2f); a.Face = at; a.Facing = true; a.BarkLeft = .1f; }
                else if (a.Perch < 0 && !a.Data.Ledge) Bolt(a, at);
            }
        }

        private void CheckSlipperHits(float dt)
        {
            float thrown2 = ThrownSpeed * dt * ThrownSpeed * dt;
            for (int k = 0; k < _slipperCount; k++)
            {
                var s = _slippers[k];
                var a = _slipperPrev[k]; var c = _slipperNow[k];
                if (s == null || s.State == SlipperState.Held || (c - a).sqrMagnitude < thrown2) continue;
                foreach (var pet in _actors)
                {
                    if (pet.Hurt > 0f) continue;
                    var body = pet.Pos + Vector3.up * (pet.Hip + pet.Lift);
                    var ab = c - a; float len2 = ab.sqrMagnitude;
                    float u = len2 > 1e-8f ? Mathf.Clamp01(Vector3.Dot(body - a, ab) / len2) : 0f;
                    float reach = pet.Data.Dog ? .42f : .32f;
                    if ((a + ab * u - body).sqrMagnitude > reach * reach) continue;
                    // ⚠️ It yelps or hisses and bolts. It is NOT burst, and the slipper is only read.
                    pet.Hurt = 3f;
                    Say(pet.Data.Dog ? "dog_yelp" : "cat_hiss", pet.Pos);
                    if (pet.Busy) EndEvent();
                    if (!pet.Data.Ledge) Bolt(pet, a); else { Do(pet, ActArch, 1.5f); pet.Face = a; pet.Facing = true; }
                }
            }
        }

        /// <summary>Away from `from`, fast: a cat up the nearest perch that is not beside the danger, anyone else to its farthest spot.</summary>
        private void Bolt(Actor a, Vector3 from)
        {
            if (a.Perch >= 0)
            {
                // Already up: it only flattens and stares.
                Do(a, ActArch, 1.4f); a.Face = from; a.Facing = true; a.UpLeft = Mathf.Max(a.UpLeft, Range(8f, 14f));
                return;
            }
            if (!a.Data.Dog)
            {
                int perch = BestPerch(a.Pos, from, 9f);
                if (perch >= 0 && Route(a.Pos, Perches[perch].Foot, a.Path)) { Go(a, 4.2f, false); a.PerchGoal = perch; a.Then = ActJump; return; }
            }
            Vector3 pick = a.Pos; float best = 0f;
            foreach (var s in a.Data.Spots)
            {
                float d = Flat(s - from).magnitude;
                if (d > best && SameRegion(a.Pos, s)) { best = d; pick = s; }
            }
            if (best > 2f && Route(a.Pos, pick, a.Path)) { Go(a, a.Data.Dog ? 3.4f : 4.2f, false); a.Then = ActAlert; a.Face = from; }
            else { Do(a, ActArch, 1.2f); a.Face = from; a.Facing = true; }
        }

        private int BestPerch(Vector3 from, Vector3 danger, float within)
        {
            int pick = -1; float best = float.MaxValue;
            for (int k = 0; k < Perches.Length; k++)
            {
                if (Occupied(k)) continue;
                var foot = Perches[k].Foot;
                float mine = Flat(foot - from).magnitude;
                if (mine > within || Mathf.Abs(foot.y - from.y) > 1.2f || Flat(foot - danger).magnitude < mine * .8f + .6f || !SameRegion(from, foot)) continue;
                if (mine < best) { best = mine; pick = k; }
            }
            return pick;
        }

        private bool Occupied(int perch)
        {
            foreach (var a in _actors) if (a.Perch == perch || a.PerchGoal == perch) return true;
            return false;
        }

        // ---------------------------------------------------------------- the event

        private bool TryStartEvent(int kind)
        {
            // A dog and a ground cat on the same floor, both at ease, within a run of each other.
            int dog = -1, cat = -1; float best = 16f;
            for (int d = 0; d < _actors.Length; d++)
            {
                var D = _actors[d];
                if (!D.Data.Dog || D.Busy || D.Act == ActMove || D.Hurt > 0f) continue;
                for (int c = 0; c < _actors.Length; c++)
                {
                    var C = _actors[c];
                    if (C.Data.Dog || C.Data.Ledge || C.Busy || C.Perch >= 0 || C.Act == ActMove || C.Act == ActJump || C.Hurt > 0f) continue;
                    float apart = Flat(C.Pos - D.Pos).magnitude;
                    if (apart < 1.2f || apart >= best || !SameRegion(D.Pos, C.Pos)) continue;
                    best = apart; dog = d; cat = c;
                }
            }
            if (kind < 0)
            {
                double roll = _random.NextDouble();
                kind = roll < .62 ? EventChase : roll < .82 ? EventSwat : EventChickens;
            }
            if (kind == EventChickens)
            {
                // Any calm dog with a chicken group on its floor.
                if (Chickens == null) return false;
                for (int d = 0; d < _actors.Length; d++)
                {
                    var D = _actors[d];
                    if (!D.Data.Dog || D.Busy || D.Hurt > 0f) continue;
                    foreach (var g in Chickens.Groups)
                    {
                        if (g == null || Flat(g.Home - D.Pos).magnitude > 14f || !SameRegion(D.Pos, g.Home) || !Route(D.Pos, g.Home, D.Path)) continue;
                        _event = EventChickens; _phase = 0; _eventTime = 0f; _dog = d; _cat = -1; _eventGoal = g.Home; Events++;
                        EventDog = D.Root; EventCat = null;
                        D.Busy = true; Do(D, ActAlert, .7f); D.Face = g.Home; D.Facing = true;
                        Say("dog_bark", D.Pos);
                        return true;
                    }
                }
                return false;
            }
            if (dog < 0) return false;
            _event = kind; _phase = 0; _eventTime = 0f; _dog = dog; _cat = cat; _eventPerch = -1; Events++;
            var dogA = _actors[dog]; var catA = _actors[cat];
            EventDog = dogA.Root; EventCat = catA.Root;
            dogA.Busy = catA.Busy = true;
            Do(dogA, ActAlert, .9f); dogA.Face = catA.Pos; dogA.Facing = true;
            Do(catA, ActArch, .9f); catA.Face = dogA.Pos; catA.Facing = true;
            Say("dog_bark", dogA.Pos);
            return true;
        }

        private void EndEvent()
        {
            foreach (var a in _actors) a.Busy = false;
            _event = EventNone; _dog = _cat = -1; _eventPerch = -1;
        }

        private void DogHome(Actor dog)
        {
            Vector3 home = dog.Data.Spots.Length > 0 ? dog.Data.Spots[_random.Next(dog.Data.Spots.Length)] : dog.Data.Home;
            if (Route(dog.Pos, home, dog.Path)) { Go(dog, 1.5f, false); dog.Then = ActLie; }
            else Do(dog, ActSit, Range(4f, 8f));
        }

        private void StepEvent(float dt)
        {
            _eventTime += dt;
            var dog = _actors[_dog]; var cat = _cat >= 0 ? _actors[_cat] : null;
            if (_event == EventChickens)
            {
                if (_phase == 0 && dog.Act != ActAlert)
                {
                    _phase = 1;
                    if (Route(dog.Pos, _eventGoal, dog.Path)) { Go(dog, 3.6f, false); Say("dog_scrabble", dog.Pos); } else { EndEvent(); return; }
                }
                else if (_phase == 1 && dog.Act != ActMove)
                {
                    _phase = 2; Do(dog, ActBark, 1.6f); dog.BarkLeft = .1f; dog.Facing = false;
                }
                else if (_phase == 2 && dog.Act != ActBark)
                {
                    Say("dog_pant", dog.Pos, .8f); DogHome(dog); EndEvent();
                }
                if (_eventTime > 30f) EndEvent();
                return;
            }

            if (_event == EventSwat)
            {
                switch (_phase)
                {
                    case 0:     // the dog has seen it: it comes over at a trot, and the cat does not move
                        if (dog.Act == ActAlert) break;
                        _phase = 1;
                        if (Route(dog.Pos, cat.Pos, dog.Path)) Go(dog, 1.9f, false); else { EndEvent(); return; }
                        Do(cat, ActArch, 20f); cat.Face = dog.Pos; cat.Facing = true;
                        break;
                    case 1:
                        cat.Face = dog.Pos;
                        if (Flat(dog.Pos - cat.Pos).magnitude < .75f || dog.Act != ActMove)
                        {
                            _phase = 2; _eventTime = 0f;
                            Do(cat, ActSwat, .7f); cat.Face = dog.Pos; cat.Facing = true;
                            Say("cat_hiss", cat.Pos);
                            Do(dog, ActStand, .25f); dog.Face = cat.Pos; dog.Facing = true;
                        }
                        break;
                    case 2:
                        if (_eventTime < .25f) break;
                        _phase = 3; _eventTime = 0f;
                        Say("dog_yelp", dog.Pos);
                        Do(dog, ActBack, .8f); dog.Face = cat.Pos; dog.Facing = true;
                        break;
                    case 3:
                        if (dog.Act == ActBack) break;
                        Say("dog_whine", dog.Pos);
                        DogHome(dog);
                        Do(cat, ActSit, 2f); cat.Then = ActGroom;
                        EndEvent();
                        break;
                }
                if (_eventTime > 30f) EndEvent();
                return;
            }

            switch (_phase)
            {
                case 0:     // noticed: a beat of staring, then it is off
                    if (_eventTime < .9f) break;
                    _phase = 1; _eventTime = 0f; _repath = 0f;
                    _eventPerch = BestPerch(cat.Pos, dog.Pos, 30f);
                    bool away = false;
                    if (_eventPerch >= 0 && Route(cat.Pos, Perches[_eventPerch].Foot, cat.Path)) { Go(cat, 4.4f, false); cat.PerchGoal = _eventPerch; cat.Then = ActJump; away = true; }
                    else
                    {
                        _eventPerch = -1;
                        Vector3 pick = cat.Pos; float best = 0f;
                        foreach (var s in cat.Data.Spots) { float d = Flat(s - dog.Pos).magnitude; if (d > best && SameRegion(cat.Pos, s)) { best = d; pick = s; } }
                        if (best > 1f && Route(cat.Pos, pick, cat.Path)) { Go(cat, 4.4f, false); cat.Then = ActArch; away = true; }
                    }
                    if (!away) { EndEvent(); return; }
                    Say("dog_scrabble", dog.Pos);
                    Say("cat_meow", cat.Pos, .8f);
                    break;
                case 1:     // the run: the dog goes where the cat is, a moment behind
                    _repath -= dt;
                    bool up = cat.Perch >= 0;
                    if (_repath <= 0f && _eventTime > .3f)
                    {
                        _repath = .35f;
                        var target = up || cat.Act == ActJump ? Perches[_eventPerch].Foot : cat.Pos;
                        if (Route(dog.Pos, target, dog.Path)) Go(dog, 3.7f, false);
                    }
                    var goal = _eventPerch >= 0 ? Perches[_eventPerch].Foot : cat.Pos;
                    bool arrived = Flat(dog.Pos - goal).magnitude < .85f;
                    if ((up || (_eventPerch < 0 && cat.Act != ActMove)) && (arrived || (dog.Act != ActMove && _eventTime > 1f)))
                    {
                        _phase = 2; _eventTime = 0f;
                        Do(dog, ActSkid, .5f); dog.Face = up ? Perches[_eventPerch].Top : cat.Pos; dog.Facing = true;
                        Say("dog_scrabble", dog.Pos, .8f);
                        if (up) { Do(cat, ActArch, 1.6f); cat.Face = dog.Pos; cat.Facing = true; Say("cat_hiss", cat.Pos); }
                    }
                    else if (_eventTime > 14f) { _phase = 3; _eventTime = 99f; }
                    break;
                case 2:     // below the perch: it barks up at the cat for a few seconds
                    if (dog.Act == ActSkid) break;
                    _phase = 3; _eventTime = 0f;
                    Do(dog, ActBark, Range(3f, 5f)); dog.BarkLeft = .15f;
                    dog.Face = cat.Perch >= 0 ? Perches[cat.Perch].Top : cat.Pos; dog.Facing = true;
                    Say("dog_growl", dog.Pos, .8f);
                    break;
                case 3:     // and gives up
                    if (dog.Act == ActBark && _eventTime < 90f) break;
                    Say("dog_whine", dog.Pos);
                    DogHome(dog);
                    cat.UpLeft = Range(14f, 28f);
                    EndEvent();
                    break;
            }
        }

        // ---------------------------------------------------------------- one animal

        private void Do(Actor a, int act, float length)
        {
            a.Act = act; a.Time = 0f; a.Len = length; a.Facing = false; a.Then = -1;
            if (act != ActMove) { a.Path.Clear(); a.Sniff = false; }
        }

        private void Go(Actor a, float speed, bool sniff)
        {
            a.Act = ActMove; a.Time = 0f; a.Len = 60f; a.PathAt = 0; a.Speed = speed; a.Sniff = sniff; a.Facing = false; a.Then = -1; a.PerchGoal = -1;
        }

        private void Jump(Actor a, Vector3 to, int perch)
        {
            a.Act = ActJump; a.Time = 0f; a.Facing = false; a.Then = -1;
            a.JumpFrom = a.Pos; a.JumpTo = to;
            float rise = Mathf.Abs(to.y - a.Pos.y);
            a.JumpHeight = .12f + rise * .25f; a.Len = .32f + rise * .22f + Flat(to - a.Pos).magnitude * .12f;
            if (a.Perch >= 0) a.Perch = -1;
            a.PerchGoal = perch;
        }

        private void StepActor(Actor a, float dt)
        {
            a.Time += dt;
            if (a.Quiet > 0f) a.Quiet -= dt;
            if (a.Hurt > 0f) a.Hurt -= dt;
            if (a.Jolt > 0f) a.Jolt = Mathf.Max(0f, a.Jolt - dt * 5f);
            bool cat = !a.Data.Dog;
            float sitPitch = cat ? 30f : 38f;
            float pitch = 0f, drop = 0f, lift = 0f, headPitch = 0f, headYaw = 0f, headRoll = 0f, tailPitch = 0f, tailYaw = 0f;
            float fl = 0f, fr = 0f, bl = 0f, br = 0f, frontScale = 1f;
            bool done = a.Time >= a.Len;
            bool calm = a.Act <= ActStretch || a.Act == ActWag;

            // A player beside it: the dog wags, the cat gives way (unless it is up out of reach, or busy).
            if (!a.Busy && calm && a.Hurt <= 0f)
            {
                if (a.Data.Dog)
                {
                    if (a.Act != ActWag && a.Quiet <= 0f && Near(a.Pos, 3f, out var friend))
                    {
                        int was = a.Act;
                        Do(a, ActWag, Range(2.5f, 5f)); a.Face = friend; a.Facing = was == ActStand; a.Then = was;
                        if (_random.NextDouble() < .3) Say("dog_pant", a.Pos, .7f);
                    }
                }
                else if (a.Perch < 0 && !a.Data.Ledge && Near(a.Pos, 1.5f, out var body)) Bolt(a, body);
                else if (a.Perch < 0 || a.Data.Ledge || true)
                {
                    // A resting cat purrs when somebody stands by it without sending it off.
                    a.PurrLeft -= dt;
                    if ((a.Act == ActSit || a.Act == ActLie) && a.PurrLeft <= 0f && Near(a.Pos, 3f, out _)) { a.PurrLeft = Range(5f, 9f); Say("cat_purr", a.Pos, .7f); }
                }
            }

            switch (a.Act)
            {
                case ActStand:
                    Look(a, dt, ref headYaw, ref headRoll);
                    tailYaw = a.Data.Dog ? 8f * Mathf.Sin(a.Time * 3f) : 0f;
                    break;
                case ActSit:
                    pitch = -sitPitch; drop = a.Span * Mathf.Sin(sitPitch * Mathf.Deg2Rad);
                    bl = br = -78f; headPitch = sitPitch * .85f;
                    Look(a, dt, ref headYaw, ref headRoll);
                    tailPitch = cat ? 70f : 40f; tailYaw = cat ? 14f * Mathf.Sin(a.Time * 1.3f) : 0f;
                    break;
                case ActLie:
                    // A dog flat out in the shade; a cat in a loaf, its front paws tucked away under it.
                    drop = a.Hip * (cat ? .56f : .62f);
                    fl = fr = bl = br = -84f; frontScale = cat ? .05f : 1f;
                    headPitch = a.Data.Dog ? 14f + 2f * Mathf.Sin(a.Time * 1.8f) : 4f;
                    Look(a, dt, ref headYaw, ref headRoll); headYaw *= .5f;
                    tailPitch = cat ? 80f : 60f; tailYaw = cat ? 20f : (Mathf.Sin(a.Time * .9f) > .92f ? 25f : 0f);
                    break;
                case ActGroom:
                    pitch = -sitPitch; drop = a.Span * Mathf.Sin(sitPitch * Mathf.Deg2Rad);
                    bl = br = -78f;
                    if (cat) { fr = -72f; headPitch = sitPitch + 34f + 9f * Mathf.Sin(a.Time * 9f); headRoll = 14f; tailPitch = 70f; }
                    // A dog's scratch: a hind foot drumming at its ear.
                    else { bl = -112f + 16f * Mathf.Sin(a.Time * 52f); headPitch = sitPitch; headRoll = -24f; headYaw = -18f; tailPitch = 40f; }
                    break;
                case ActStretch:
                {
                    // Front end down, rump up, the front legs out ahead, then up again.
                    float s = Mathf.Sin(Mathf.Clamp01(a.Time / a.Len) * Mathf.PI);
                    pitch = 22f * s; fl = fr = -58f * s; bl = br = 8f * s; headPitch = -30f * s; tailPitch = -20f * s;
                    break;
                }
                case ActWag:
                    if (a.Then == ActSit) { pitch = -sitPitch; drop = a.Span * Mathf.Sin(sitPitch * Mathf.Deg2Rad); bl = br = -78f; headPitch = sitPitch * .85f; }
                    else if (a.Then == ActLie) { drop = a.Hip * .62f; fl = fr = bl = br = -84f; headPitch = -6f; }
                    tailYaw = 32f * Mathf.Sin(a.Time * 26f);
                    if (a.Then != ActStand) { var to = Flat(a.Face - a.Pos); headYaw = Mathf.Clamp(Mathf.DeltaAngle(a.Heading, Yaw(to)), -70f, 70f); }
                    if (done) { int back = a.Then < 0 ? ActStand : a.Then; Do(a, back, Range(4f, 9f)); a.Quiet = Range(6f, 12f); done = false; }
                    break;
                case ActMove:
                {
                    bool arrived = Follow(a, dt);
                    float stride = a.Speed > 2.4f ? (cat ? .8f : 1.1f) : a.Speed > 1f ? (cat ? .4f : .6f) : (cat ? .28f : .45f);
                    float phase = a.Stride / stride * Mathf.PI * 2f;
                    if (a.Speed > 2.4f)
                    {
                        // A bound: the front pair reaches, the hind pair drives, the back flexes with it.
                        fl = -8f + 44f * Mathf.Sin(phase); fr = -8f + 44f * Mathf.Sin(phase + .5f);
                        bl = 8f + 44f * Mathf.Sin(phase + 2.6f); br = 8f + 44f * Mathf.Sin(phase + 3.1f);
                        pitch = 6f * Mathf.Sin(phase + 1f); lift = Mathf.Abs(Mathf.Sin(phase * .5f)) * a.Hip * .22f;
                        headPitch = -8f; tailPitch = cat ? 62f : 48f;
                    }
                    else
                    {
                        float amp = a.Speed > 1f ? 32f : 24f;
                        fl = br = amp * Mathf.Sin(phase); fr = bl = -amp * Mathf.Sin(phase);
                        lift = Mathf.Abs(Mathf.Sin(phase)) * a.Hip * .03f;
                        if (a.Sniff) { pitch = 10f; headPitch = 46f + 5f * Mathf.Sin(a.Time * 11f); headYaw = 14f * Mathf.Sin(a.Time * 2.3f); }
                        tailYaw = a.Data.Dog ? 12f * Mathf.Sin(phase) : 0f; tailPitch = cat ? 12f : 0f;
                    }
                    done = false;
                    if (arrived)
                    {
                        int then = a.Then, perch = a.PerchGoal;
                        if (then == ActJump && perch >= 0) Jump(a, Perches[perch].Top, perch);
                        else if (then == ActLie) { Do(a, ActLie, Range(10f, 24f)); if (a.Data.Dog) Say("dog_pant", a.Pos, .6f); }
                        else if (then >= 0) { var face = a.Face; Do(a, then, then == ActArch || then == ActAlert ? 1.4f : Range(4f, 9f)); a.Face = face; a.Facing = then == ActArch || then == ActAlert; }
                        else Do(a, ActStand, Range(.6f, 1.6f));
                    }
                    break;
                }
                case ActJump:
                {
                    float t = Mathf.Clamp01(a.Time / a.Len), s = t * t * (3f - 2f * t);
                    var p = Vector3.Lerp(a.JumpFrom, a.JumpTo, s);
                    p.y = Mathf.Lerp(a.JumpFrom.y, a.JumpTo.y, s) + a.JumpHeight * 4f * t * (1f - t);
                    var flat = Flat(a.JumpTo - a.JumpFrom);
                    if (flat.sqrMagnitude > .0025f) a.Heading = Mathf.MoveTowardsAngle(a.Heading, Yaw(flat), 900f * dt);
                    a.Pos = p;
                    fl = fr = -52f * (1f - t); bl = br = 50f * Mathf.Sin(t * Mathf.PI);
                    pitch = (a.JumpTo.y > a.JumpFrom.y ? -24f : 20f) * Mathf.Cos(t * Mathf.PI * .5f); tailPitch = 50f;
                    if (done)
                    {
                        a.Pos = a.JumpTo; a.Perch = a.PerchGoal; a.PerchGoal = -1;
                        if (a.Perch >= 0 && a.UpLeft <= 0f) a.UpLeft = Range(12f, 30f);
                        Do(a, ActSit, Range(2f, 5f)); done = false;
                    }
                    break;
                }
                case ActAlert:
                    pitch = -4f; headPitch = -10f; tailPitch = a.Data.Dog ? -10f : -15f;
                    break;
                case ActSkid:
                {
                    // All four braced, sliding the last of it.
                    float t = Mathf.Clamp01(a.Time / a.Len);
                    var slide = a.Pos + Dir(a.Heading) * (1.6f * (1f - t) * dt);
                    if (CellOf(slide) >= 0) a.Pos = new Vector3(slide.x, FloorAt(slide, a.Pos.y), slide.z);
                    fl = fr = -38f; bl = br = -26f; pitch = -10f; drop = a.Hip * .18f; headPitch = -12f; tailPitch = 30f;
                    break;
                }
                case ActBark:
                {
                    a.BarkLeft -= dt;
                    if (a.BarkLeft <= 0f) { a.BarkLeft = Range(.45f, 1f); a.Jolt = 1f; Say("dog_bark", a.Pos); }
                    float up = a.Facing ? Mathf.Clamp((a.Face.y - a.Pos.y - a.Hip) * 40f, 0f, 34f) : 0f;
                    pitch = -6f - 5f * a.Jolt; headPitch = -18f - up - 14f * a.Jolt; lift = .03f * a.Jolt;
                    fl = fr = -8f * a.Jolt; tailPitch = -12f; tailYaw = 10f * Mathf.Sin(a.Time * 14f);
                    break;
                }
                case ActSwat:
                    fr = -62f + 34f * Mathf.Sin(a.Time * 30f); pitch = -8f; headPitch = 6f; tailPitch = -20f; lift = a.Hip * .1f;
                    break;
                case ActArch:
                    // Up on its toes, tail up, head low and flat: a cat that means it. (A dog only stiffens.)
                    lift = cat ? a.Hip * .12f : 0f; headPitch = cat ? 16f : -6f; tailPitch = cat ? -22f : -10f; pitch = cat ? 4f : -3f;
                    break;
                case ActBack:
                {
                    var back = a.Pos - Dir(a.Heading) * (1.1f * dt);
                    if (CellOf(back) >= 0) a.Pos = new Vector3(back.x, FloorAt(back, a.Pos.y), back.z);
                    float phase = a.Time * 16f;
                    fl = br = 20f * Mathf.Sin(phase); fr = bl = -20f * Mathf.Sin(phase);
                    pitch = -5f; headPitch = 14f; tailPitch = 70f; drop = a.Hip * .1f;
                    break;
                }
            }
            if (a.Facing)
            {
                var to = Flat(a.Face - a.Pos);
                if (to.sqrMagnitude > .01f) a.Heading = Mathf.MoveTowardsAngle(a.Heading, Yaw(to), 480f * dt);
            }
            if (done && !a.Busy) Next(a);
            else if (done && a.Busy && (a.Act == ActAlert || a.Act == ActSkid || a.Act == ActBark || a.Act == ActBack || a.Act == ActSwat))
            { var face = a.Face; bool facing = a.Facing; Do(a, ActStand, 30f); a.Face = face; a.Facing = facing; }

            float fast = 1f - Mathf.Exp(-22f * dt), slow = 1f - Mathf.Exp(-9f * dt);
            a.Pitch = Mathf.Lerp(a.Pitch, pitch, slow); a.Drop = Mathf.Lerp(a.Drop, drop, slow); a.Lift = Mathf.Lerp(a.Lift, lift, fast);
            a.HeadPitch = Mathf.Lerp(a.HeadPitch, headPitch, fast); a.HeadYaw = Mathf.Lerp(a.HeadYaw, headYaw, slow); a.HeadRoll = Mathf.Lerp(a.HeadRoll, headRoll, slow);
            a.TailPitch = Mathf.Lerp(a.TailPitch, tailPitch, slow); a.TailYaw = Mathf.Lerp(a.TailYaw, tailYaw, fast);
            a.AFl = Mathf.Lerp(a.AFl, fl, fast); a.AFr = Mathf.Lerp(a.AFr, fr, fast); a.ABl = Mathf.Lerp(a.ABl, bl, fast); a.ABr = Mathf.Lerp(a.ABr, br, fast);
            a.FrontScale = Mathf.Lerp(a.FrontScale, frontScale, slow);

            a.Root.SetPositionAndRotation(a.Pos + Vector3.up * a.Lift, Quaternion.Euler(0f, a.Heading, 0f));
            if (a.Trunk != null)
            {
                a.Trunk.localPosition = a.TrunkAt + Vector3.down * a.Drop;
                a.Trunk.localRotation = a.TrunkRest * Quaternion.Euler(a.Pitch, 0f, 0f);
            }
            // A leg's angle is meant in the WORLD (0 hangs straight down, + swings back): the trunk's pitch is taken out again.
            if (a.Head != null) a.Head.localRotation = a.HeadRest * Quaternion.Euler(a.HeadPitch, a.HeadYaw, a.HeadRoll);
            // A tail's pitch is meant as + DOWN AND BACK (it is modelled standing up): the turn about the side axis is its negative.
            if (a.Tail != null) a.Tail.localRotation = a.TailRest * Quaternion.Euler(-a.TailPitch, a.TailYaw, 0f);
            if (a.Fl != null) { a.Fl.localRotation = a.FlRest * Quaternion.Euler(a.AFl - a.Pitch, 0f, 0f); a.Fl.localScale = Vector3.one * a.FrontScale; }
            if (a.Fr != null) { a.Fr.localRotation = a.FrRest * Quaternion.Euler(a.AFr - a.Pitch, 0f, 0f); a.Fr.localScale = Vector3.one * a.FrontScale; }
            if (a.Bl != null) a.Bl.localRotation = a.BlRest * Quaternion.Euler(a.ABl - a.Pitch, 0f, 0f);
            if (a.Br != null) a.Br.localRotation = a.BrRest * Quaternion.Euler(a.ABr - a.Pitch, 0f, 0f);
        }

        private void Look(Actor a, float dt, ref float yaw, ref float roll)
        {
            a.LookLeft -= dt;
            if (a.LookLeft <= 0f) { a.LookLeft = Range(1.2f, 4f); a.LookYaw = Range(-55f, 55f); }
            yaw = a.LookYaw; roll = 0f;
        }

        /// <summary>Walks the path. True when its end is reached.</summary>
        private bool Follow(Actor a, float dt)
        {
            if (a.Data.Ledge)
            {
                var goal = a.Path.Count > 0 ? a.Path[0] : a.Pos;
                var to = goal - a.Pos; float d = to.magnitude;
                if (d < .03f) return true;
                a.Heading = Mathf.MoveTowardsAngle(a.Heading, Yaw(to), 300f * dt);
                float off = Mathf.Abs(Mathf.DeltaAngle(a.Heading, Yaw(to)));
                float step = Mathf.Min(d, a.Speed * dt * (off < 40f ? 1f : .1f));
                a.Pos += to / d * step; a.Stride += step;
                return false;
            }
            float left = a.Speed * dt;
            while (left > 0f && a.PathAt < a.Path.Count)
            {
                var to = Flat(a.Path[a.PathAt] - a.Pos); float d = to.magnitude;
                if (d < .04f) { a.PathAt++; continue; }
                float want = Yaw(to);
                a.Heading = Mathf.MoveTowardsAngle(a.Heading, want, (a.Speed > 2.4f ? 900f : 360f) * dt);
                float off = Mathf.Abs(Mathf.DeltaAngle(a.Heading, want));
                float step = Mathf.Min(d, left * (off < 50f ? 1f : .15f));
                var p = a.Pos + to / d * step;
                a.Pos = new Vector3(p.x, Mathf.MoveTowards(a.Pos.y, FloorAt(p, a.Pos.y), 3f * dt), p.z);
                a.Stride += step; left -= off < 50f ? step : left;
                if (step >= d - 1e-4f) a.PathAt++;
            }
            if (a.PathAt >= a.Path.Count)
            {
                a.Pos = new Vector3(a.Pos.x, FloorAt(a.Pos, a.Pos.y), a.Pos.z);
                return true;
            }
            return false;
        }

        // What it does next, at ease.
        private void Next(Actor a)
        {
            double roll = _random.NextDouble();
            if (a.Then == ActGroom) { Do(a, ActGroom, Range(2.5f, 5f)); return; }
            if (a.Data.Ledge)
            {
                if (roll < .3)
                {
                    // Along its ledge to somewhere else on it.
                    var p = Vector3.Lerp(a.Data.LedgeA, a.Data.LedgeB, Range(0f, 1f));
                    if ((p - a.Pos).magnitude > .5f) { Go(a, .32f, false); a.Path.Clear(); a.Path.Add(p); return; }
                }
                if (roll < .5) Do(a, ActSit, Range(6f, 14f));
                else if (roll < .75) Do(a, ActLie, Range(10f, 24f));
                else if (roll < .9) { Do(a, ActSit, .1f); a.Then = ActGroom; }
                else Do(a, ActStretch, 2.2f);
                if (_random.NextDouble() < .05) Say("cat_meow", a.Pos, .6f);
                return;
            }
            if (a.Perch >= 0)
            {
                a.UpLeft -= a.Len;
                if (a.UpLeft <= 0f && !Near(a.Pos, 2.5f, out _))
                {
                    // Down again, to the floor cell beside the perch.
                    var foot = Perches[a.Perch].Foot;
                    Jump(a, foot, -1); a.Then = -1;
                    return;
                }
                if (roll < .45) Do(a, ActSit, Range(4f, 9f));
                else if (roll < .8) Do(a, ActLie, Range(6f, 14f));
                else { Do(a, ActSit, .1f); a.Then = ActGroom; }
                return;
            }
            if (a.Data.Dog)
            {
                if (roll < .26) Do(a, ActLie, Range(6f, 14f));
                else if (roll < .4) Do(a, ActSit, Range(4f, 9f));
                else if (roll < .5) { Do(a, ActSit, .1f); a.Then = ActGroom; }
                else if (roll < .92 && a.Data.Spots.Length > 0)
                {
                    // To another of its spots: nose down along the way, or at a trot.
                    var spot = a.Data.Spots[_random.Next(a.Data.Spots.Length)];
                    bool sniff = _random.NextDouble() < .55;
                    if (Flat(spot - a.Pos).magnitude > .8f && Route(a.Pos, spot, a.Path)) { Go(a, sniff ? .5f : 1.5f, sniff); a.Then = _random.NextDouble() < .5 ? ActLie : ActSit; }
                    else Do(a, ActStand, Range(1.5f, 4f));
                }
                else Do(a, ActStand, Range(1.5f, 4f));
                if (a.Act == ActStand && _random.NextDouble() < .06) Say("dog_whine", a.Pos, .5f);
                return;
            }
            // A ground cat.
            if (roll < .2) Do(a, ActSit, Range(5f, 10f));
            else if (roll < .36) Do(a, ActLie, Range(8f, 16f));
            else if (roll < .47) { Do(a, ActSit, .1f); a.Then = ActGroom; }
            else if (roll < .55) Do(a, ActStretch, 2.2f);
            else if (roll < .76)
            {
                // Up onto something low and near, for a while.
                int perch = -1; float best = 6f;
                for (int k = 0; k < Perches.Length; k++)
                {
                    float d = Flat(Perches[k].Foot - a.Pos).magnitude;
                    if (d < best && !Occupied(k) && Mathf.Abs(Perches[k].Foot.y - a.Pos.y) < .3f && SameRegion(a.Pos, Perches[k].Foot) && !Near(Perches[k].Foot, 2.5f, out _)) { best = d; perch = k; }
                }
                if (perch >= 0 && Route(a.Pos, Perches[perch].Foot, a.Path)) { Go(a, .45f, false); a.PerchGoal = perch; a.Then = ActJump; a.UpLeft = Range(15f, 40f); }
                else Do(a, ActSit, Range(4f, 8f));
            }
            else if (a.Data.Spots.Length > 0)
            {
                var spot = a.Data.Spots[_random.Next(a.Data.Spots.Length)];
                if (Flat(spot - a.Pos).magnitude > .8f && Route(a.Pos, spot, a.Path)) { Go(a, .42f, false); a.Then = ActSit; }
                else Do(a, ActStand, Range(1.5f, 3f));
            }
            else Do(a, ActStand, Range(1.5f, 3f));
            if (_random.NextDouble() < .04) Say("cat_meow", a.Pos, .6f);
        }

        private void OnDrawGizmosSelected()
        {
            if (Walkable == null || Heights == null) return;
            Gizmos.color = new Color(.3f, .6f, 1f, .4f);
            for (int k = 0; k < Walkable.Length && k < Heights.Length; k++)
                if (Walkable[k]) Gizmos.DrawCube(new Vector3(GridOrigin.x + (k % Width + .5f) * Cell, Heights[k] + .01f, GridOrigin.y + (k / Width + .5f) * Cell), new Vector3(Cell * .8f, .01f, Cell * .8f));
            Gizmos.color = Color.magenta;
            if (Perches != null) foreach (var p in Perches) { Gizmos.DrawWireSphere(p.Top, .1f); Gizmos.DrawLine(p.Top, p.Foot); }
        }
    }
}
