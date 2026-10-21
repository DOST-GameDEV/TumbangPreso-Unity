using System;
using System.Text;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// The Eskinita Alley's LIVE chickens (owner, 2026-10-09, looking at a rooster statue: "if you're
    /// gonna make chickens, make them live, make them like the birds in lagoon cove"). The same kind
    /// of life as a landed bird of <see cref="LagoonFlocks"/> (a little act machine: pause, step,
    /// peck, turn; startled by a body within 3 m or a moving slipper within 2 m, the whole group
    /// going together), for birds that never fly: they WALK a few steps with a bobbing head, peck,
    /// look about in jerks, scratch, the rooster crows, and a startle sends them off in a flapping,
    /// hopping run, or fluttering up onto a low perch, after which they settle and wander back.
    /// ⚠️ A THROWN SLIPPER THROUGH ONE BURSTS IT INTO FEATHERS, exactly as a Lagoon bird (owner,
    /// 2026-10-09, after playing: "the chickens dont explode when hit"; of the Lagoon's birds he had
    /// asked "like how csgo chickens work"). It is gone for 20 to 40 s, the Lagoon's wait, and then
    /// WALKS back in from one of its group's hidden entries (inside the tepee, behind a crate stack
    /// or a drum: there is no flock in the sky to rejoin), never popping in beside anyone.
    /// They also scatter at what `AmbientLife`'s animals react to (the can going down, a thunder
    /// strike, an ice shatter: `MatchFlair`), and at a running dog of `AlleyPets`.
    /// </summary>
    // ⚠️ SCENERY, exactly as the Lagoon's birds are. Local only: never on the wire, never a collider,
    // never a physics body, never a gameplay random stream (a private one, seeded fresh). Two peers
    // see different chickens and that is correct, because nothing a player, a slipper or the can
    // does depends on them. They read players and slippers; they touch nothing.
    // ⚠️ NO NAVMESH AND NO RAYCASTS AT RUNTIME. Where a group may stand is BAKED by the map builder
    // (EskinitaAlleyLifeAuthor) into a small grid per group: flat cells of that group's own terrace,
    // clear of walls, stairs, props, bounce tarps and the chalk square. Every step and every flight
    // ends on a walkable cell and every walked line is checked against the grid, so a bird cannot
    // leave its terrace however it is chased.
    // ⚠️ THE MODEL'S PARTS, found by name anywhere under each bird (tools/author_eskinita_chickens.py):
    // "torso" (an empty at the hips: pitched to peck), under it "head" (origin at the neck base),
    // "wing_l", "wing_r" (origins at the shoulders, modelled FOLDED); and "leg_l", "leg_r" (origins at
    // the hips) beside the torso. The bird faces its +Z; its origin is on the ground. A missing part
    // is simply not posed.
    public sealed class AlleyChickens : MonoBehaviour
    {
        [Serializable]
        public sealed class Group
        {
            public string Name;
            public Vector3 Home;                    // world, on the floor, on a walkable cell
            public float FloorY;
            public float Roam = 2.4f;
            public Vector2 GridOrigin;              // world x, z of the corner of cell (0, 0)
            public float Cell = .2f;
            public int Width, Height;
            public bool[] Walkable = Array.Empty<bool>();          // Height rows of Width
            public Vector3[] Perches = Array.Empty<Vector3>();     // low tops an adult may flutter up onto
            public Transform[] Birds = Array.Empty<Transform>();
            public float[] Size = Array.Empty<float>();            // 1 a rooster, about .85 a hen, about .35 a chick
            public bool[] Rooster = Array.Empty<bool>();
            public int[] Follow = Array.Empty<int>();              // the bird of this group a chick keeps to, or -1
            // Hidden floor points (inside the tepee, behind a crate stack) a burst bird walks back in from. May be empty.
            public Vector3[] Entries = Array.Empty<Vector3>();
        }

        public Group[] Groups = Array.Empty<Group>();
        // The painted feather of the Lagoon's birds, in the chickens' colours (the builder fills it). Empty: no puff.
        public Material[] FeatherMaterials = Array.Empty<Material>();
        // The Lagoon's own numbers (LagoonFlocks.PlayerStartle, SlipperStartle, ThrownSpeed).
        public float PlayerStartle = 3f, SlipperStartle = 2f;

        /// <summary>Stand-in bodies for a probe or the editor's motion sheet: each point startles as
        /// a player there would. Never serialized, never set by the game.</summary>
        [NonSerialized] public Vector3[] ProbeThreats = Array.Empty<Vector3>();
        /// <summary>Where the alley's dogs are RUNNING this frame (`AlleyPets` writes it): each frightens as a player does.</summary>
        [NonSerialized] public Vector3[] Scares = Array.Empty<Vector3>();
        /// <summary>The alley's animal voices (clucks, the crow, the squawk, wings). May be null: silent.</summary>
        public AlleyLifeSound Sound;
        /// <summary>How many times a group has been startled since Begin (for probes).</summary>
        public int Startles { get; private set; }
        /// <summary>How many birds slippers have burst since Begin (for probes), as `LagoonFlocks.Bursts`.</summary>
        public int Bursts { get; private set; }
        /// <summary>Raised with the burst point whenever a slipper bursts a bird, as `LagoonFlocks.BirdBurst`.</summary>
        public event Action<Vector3> BirdBurst;

        /// <summary>Bursts bird `index` of group `group` as a slipper would (probes and the motion sheet).</summary>
        public void BurstForReview(int group, int index)
        {
            if (_birds == null) return;
            foreach (var b in _birds) if (b.G == group && b.Index == index && b.Act != ActDead) Burst(b, b.Pos + Vector3.up * (.2f * b.Size));
        }

        private const int ActIdle = 0, ActWalk = 1, ActPeck = 2, ActLook = 3, ActScratch = 4, ActCrow = 5,
            ActAlert = 6, ActRun = 7, ActFlutter = 8, ActDead = 9, ActReturn = 10;
        private static readonly string[] ActNames = { "idle", "walk", "peck", "look", "scratch", "crow", "alert", "RUN", "FLUTTER", "BURST (gone)", "walking back in" };
        private const float MaxStep = 1f / 20f, RescanSeconds = 1f, ThrownSpeed = 4f, HitRadius = .38f;
        private const int SlipperCapacity = 16, FeatherCapacity = 128;

        private sealed class Bird
        {
            public int G, Index, Follow;
            public float Size, StepLen, Scale = 1f;
            public bool Rooster;
            public Transform Root, Torso, Head, WingL, WingR, LegL, LegR;
            public Quaternion TorsoRest, HeadRest, WingLRest, WingRRest, LegLRest, LegRRest;
            public Vector3 TorsoAt, HeadAt;
            public Vector3 Pos, To, ArcFrom;
            public float Heading, Stride, Speed;
            public int Act, Perch = -1, PerchGoal = -1, Dips;
            public float ActTime, ActLen, ArcHeight, Flap;
            public float LookYaw, LookRoll, LookLeft;
            public bool Startled, ThenPeck;
            public float DeadLeft;
            public float React, Ignore, CalmLeft, Panic;
            public Vector3 ThreatAt, PanicAt;
            // The pose, eased every step toward what the act asks for.
            public float Pitch, HeadPitch, HeadYaw, HeadRoll, Wing, LegLAngle, LegRAngle, Lift, Roll, Bob;
        }

        private System.Random _random;
        private Bird[] _birds;
        private int[][] _groupBirds, _perchOwner;
        private float _rescanLeft;
        private CharacterMotor[] _motors = Array.Empty<CharacterMotor>();
        private readonly Slipper[] _slippers = new Slipper[SlipperCapacity];
        private readonly Vector3[] _slipperPrev = new Vector3[SlipperCapacity], _slipperNow = new Vector3[SlipperCapacity];
        private int _slipperCount;

        private Mesh _featherMesh;
        private int _featherBuilt, _featherNext;
        private readonly Transform[] _fT = new Transform[FeatherCapacity];
        private readonly Vector3[] _fVel = new Vector3[FeatherCapacity], _fAxis = new Vector3[FeatherCapacity];
        private readonly float[] _fAge = new float[FeatherCapacity], _fLife = new float[FeatherCapacity], _fFloor = new float[FeatherCapacity],
            _fPhase = new float[FeatherCapacity], _fSize = new float[FeatherCapacity];

        private void OnEnable() => Visual.MatchFlair.Presented += OnImpact;
        private void OnDisable() => Visual.MatchFlair.Presented -= OnImpact;

        // The peer-visible outcomes `AmbientLife`'s animals react to. A group near one scatters.
        private void OnImpact(Visual.MatchFlair.Kind kind, int actor, int subject, Vector3 at, float strength)
        {
            if (_birds == null) return;
            float radius = kind == Visual.MatchFlair.Kind.LataDown ? 6f : kind == Visual.MatchFlair.Kind.Thunder ? 12f : kind == Visual.MatchFlair.Kind.IceShatter ? 5f : 0f;
            if (radius <= 0f) return;
            for (int g = 0; g < Groups.Length; g++)
                if (Groups[g] != null && (Groups[g].Home - at).sqrMagnitude < radius * radius) StartleGroup(g, at);
        }

        private void Say(string cue, Vector3 at, float gain = 1f) { if (Sound != null) Sound.Play(cue, at + Vector3.up * .15f, gain); }

        private void Start()
        {
            // ⚠️ A private stream, seeded fresh, as LagoonFlocks and AmbientLife do.
            Begin(Guid.NewGuid().GetHashCode());
        }

        private void OnDestroy()
        {
            if (_featherMesh != null) Destroy(_featherMesh);
        }

        private void Update()
        {
            // Time.deltaTime is 0 while paused: nothing moves and everything resumes where it stopped.
            float raw = Time.deltaTime;
            if (raw <= 0f || _birds == null) return;
            Step(raw > MaxStep ? MaxStep : raw);
        }

        private float Range(float a, float b) => a + (float)_random.NextDouble() * (b - a);
        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
        private static Vector3 Dir(float yaw) { float h = yaw * Mathf.Deg2Rad; return new Vector3(Mathf.Sin(h), 0f, Mathf.Cos(h)); }

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

        /// <summary>Reads the birds where the builder stood them and starts their lives. Start calls
        /// it; the editor's motion sheet calls it with a fixed seed and then steps by hand.</summary>
        public void Begin(int seed)
        {
            _random = new System.Random(seed);
            int total = 0;
            foreach (var g in Groups) if (g != null && g.Birds != null) foreach (var t in g.Birds) if (t != null) total++;
            _birds = new Bird[total];
            _groupBirds = new int[Groups.Length][];
            _perchOwner = new int[Groups.Length][];
            int n = 0;
            for (int gi = 0; gi < Groups.Length; gi++)
            {
                var g = Groups[gi];
                int count = g == null || g.Birds == null ? 0 : g.Birds.Length;
                var slots = new int[count];
                _perchOwner[gi] = new int[g == null || g.Perches == null ? 0 : g.Perches.Length];
                for (int k = 0; k < _perchOwner[gi].Length; k++) _perchOwner[gi][k] = -1;
                for (int k = 0; k < count; k++)
                {
                    slots[k] = -1;
                    var t = g.Birds[k];
                    if (t == null) continue;
                    var b = new Bird { G = gi, Index = k, Root = t };
                    b.Size = g.Size != null && k < g.Size.Length ? Mathf.Clamp(g.Size[k], .2f, 1.5f) : 1f;
                    b.Rooster = g.Rooster != null && k < g.Rooster.Length && g.Rooster[k];
                    b.Follow = g.Follow != null && k < g.Follow.Length ? g.Follow[k] : -1;
                    b.StepLen = .1f * Mathf.Lerp(.42f, 1f, b.Size);
                    b.Torso = FindChild(t, "torso"); b.Head = FindChild(t, "head");
                    b.WingL = FindChild(t, "wing_l"); b.WingR = FindChild(t, "wing_r");
                    b.LegL = FindChild(t, "leg_l"); b.LegR = FindChild(t, "leg_r");
                    if (b.Torso != null) { b.TorsoRest = b.Torso.localRotation; b.TorsoAt = b.Torso.localPosition; }
                    if (b.Head != null) { b.HeadRest = b.Head.localRotation; b.HeadAt = b.Head.localPosition; }
                    if (b.WingL != null) b.WingLRest = b.WingL.localRotation;
                    if (b.WingR != null) b.WingRRest = b.WingR.localRotation;
                    if (b.LegL != null) b.LegLRest = b.LegL.localRotation;
                    if (b.LegR != null) b.LegRRest = b.LegR.localRotation;
                    b.Pos = t.position; b.Heading = t.eulerAngles.y; b.Scale = t.localScale.x;
                    if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
                    // Staggered first acts: a group that starts in step reads as one machine.
                    b.Act = ActIdle; b.ActLen = Range(.1f, 1.8f);
                    slots[k] = n; _birds[n++] = b;
                }
                _groupBirds[gi] = slots;
            }
            _rescanLeft = 0f; _slipperCount = 0; Startles = 0; Bursts = 0;
        }

        /// <summary>One step of every bird. Update calls it with the frame's time; the editor's
        /// motion sheet calls it by hand.</summary>
        public void Step(float dt)
        {
            if (_birds == null || dt <= 0f) return;
            _rescanLeft -= dt;
            if (_rescanLeft <= 0f) { _rescanLeft = RescanSeconds; Rescan(); }
            for (int k = 0; k < _slipperCount; k++) _slipperNow[k] = _slippers[k] != null ? _slippers[k].transform.position : _slipperPrev[k];
            CheckSlipperHits(dt);
            for (int i = 0; i < _birds.Length; i++) StepBird(_birds[i], dt);
            for (int k = 0; k < _slipperCount; k++) _slipperPrev[k] = _slipperNow[k];
            StepFeathers(dt);
        }

        /// <summary>One line per bird: group, act, place. For probes and the motion sheet's log.</summary>
        public string Describe()
        {
            if (_birds == null) return "not begun";
            var s = new StringBuilder();
            foreach (var b in _birds)
                s.Append(Groups[b.G].Name).Append('.').Append(b.Index).Append(' ').Append(ActNames[b.Act]).Append(b.Perch >= 0 ? "(perched)" : "")
                    .Append(" (").Append(b.Pos.x.ToString("0.00")).Append(", ").Append(b.Pos.y.ToString("0.00")).Append(", ").Append(b.Pos.z.ToString("0.00")).Append(")  ");
            return s.ToString();
        }

        // ⚠️ Players and slippers are looked for at most once a second (FindObjectsByType walks the
        // scene and allocates). A slipper keeps its previous position across a rescan, so a rescan
        // can never fake a fast one.
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

        // ---------------------------------------------------------------- the ground

        private bool Walkable(Group g, float x, float z)
        {
            int i = Mathf.FloorToInt((x - g.GridOrigin.x) / g.Cell), j = Mathf.FloorToInt((z - g.GridOrigin.y) / g.Cell);
            return i >= 0 && j >= 0 && i < g.Width && j < g.Height && j * g.Width + i < g.Walkable.Length && g.Walkable[j * g.Width + i];
        }

        /// <summary>True when the whole straight line stays on walkable cells.</summary>
        private bool Clear(Group g, Vector3 a, Vector3 b)
        {
            float length = Flat(b - a).magnitude;
            int steps = Mathf.Max(1, Mathf.CeilToInt(length / (g.Cell * .4f)));
            for (int k = 1; k <= steps; k++)
            {
                var p = Vector3.Lerp(a, b, k / (float)steps);
                if (!Walkable(g, p.x, p.z)) return false;
            }
            return true;
        }

        private bool Crowded(Bird b, Vector3 at)
        {
            foreach (int slot in _groupBirds[b.G])
            {
                if (slot < 0 || _birds[slot] == b || _birds[slot].Act == ActDead) continue;
                var o = _birds[slot];
                if (Flat(o.Pos - at).sqrMagnitude < .07f || (o.Act == ActWalk && Flat(o.To - at).sqrMagnitude < .07f)) return true;
            }
            return false;
        }

        // ---------------------------------------------------------------- what frightens them

        private bool Threat(Bird b, float radius, out Vector3 at)
        {
            float best = radius * radius; bool any = false; at = Vector3.zero;
            foreach (var m in _motors)
            {
                if (m == null || !m.isActiveAndEnabled) continue;
                var p = m.transform.position; var d = p - b.Pos;
                float d2 = d.x * d.x + d.z * d.z;
                if (Mathf.Abs(d.y) < 2.5f && d2 < best) { best = d2; at = p; any = true; }
            }
            if (ProbeThreats != null)
                foreach (var p in ProbeThreats)
                {
                    var d = p - b.Pos; float d2 = d.x * d.x + d.z * d.z;
                    if (Mathf.Abs(d.y) < 2.5f && d2 < best) { best = d2; at = p; any = true; }
                }
            if (Scares != null)
                foreach (var p in Scares)
                {
                    var d = p - b.Pos; float d2 = d.x * d.x + d.z * d.z;
                    if (Mathf.Abs(d.y) < 1.5f && d2 < best && d2 < 2.6f * 2.6f) { best = d2; at = p; any = true; }
                }
            if (any) return true;
            if (b.Panic > 0f) { at = b.PanicAt; return true; }
            float s2 = SlipperStartle * SlipperStartle;
            for (int k = 0; k < _slipperCount; k++)
            {
                var s = _slippers[k];
                if (s == null || s.State == SlipperState.Held) continue;
                var d = _slipperNow[k] - b.Pos;
                if (Mathf.Abs(d.y) < 2f && d.x * d.x + d.z * d.z < s2 && (_slipperNow[k] - _slipperPrev[k]).sqrMagnitude > 1e-6f) { at = _slipperNow[k]; return true; }
            }
            return false;
        }

        // ⚠️ THE WHOLE GROUP GOES (the Lagoon's rule for a landed group): one bird seeing the body is
        // enough, and each reacts a beat apart so they do not leave as one object.
        private void StartleGroup(int g, Vector3 at)
        {
            Startles++;
            bool cried = false;
            foreach (int slot in _groupBirds[g])
            {
                if (slot < 0) continue;
                var b = _birds[slot];
                if (b.Startled || b.Act >= ActRun || b.Ignore > 0f) continue;   // (a burst or returning bird is not there to startle)
                b.Startled = true; b.React = Range(0f, .28f); b.ThreatAt = at;
                if (!cried && b.Size > .6f) { cried = true; Say("chicken_squawk", b.Pos, .9f); }
            }
        }

        private void BeginFlee(Bird b, Vector3 from)
        {
            var g = Groups[b.G];
            if (b.Perch >= 0) return;
            var away = Flat(b.Pos - from);
            away = away.sqrMagnitude > 1e-4f ? away.normalized : Dir(b.Heading);
            float baseYaw = Mathf.Atan2(away.x, away.z) * Mathf.Rad2Deg, now = Flat(b.Pos - from).magnitude;
            bool adult = b.Size > .6f;

            // Up onto something low, if there is something and the body is not standing at it.
            int perch = -1;
            if (adult)
                for (int k = 0; k < g.Perches.Length; k++)
                {
                    if (_perchOwner[b.G][k] >= 0) continue;
                    float reach = Flat(g.Perches[k] - b.Pos).magnitude;
                    if (reach < .3f || reach > 3.2f || Flat(g.Perches[k] - from).magnitude < 1.5f) continue;
                    if (perch < 0 || reach < Flat(g.Perches[perch] - b.Pos).magnitude) perch = k;
                }

            float best = now + .4f; var pick = b.Pos; bool found = false;
            for (int k = 0; k < 18; k++)
            {
                float yaw = baseYaw + (k == 0 ? 0f : Range(-115f, 115f));
                var t = b.Pos + Dir(yaw) * Range(1.1f, 3.2f) * Mathf.Lerp(.6f, 1f, b.Size);
                if (!Clear(g, b.Pos, t)) continue;
                float score = Flat(t - from).magnitude + Range(0f, .3f);
                if (score > best) { best = score; pick = t; found = true; }
            }

            if (perch >= 0 && (!found || _random.NextDouble() < .5))
            {
                _perchOwner[b.G][perch] = b.Index;
                BeginFlutter(b, g.Perches[perch], .28f, perch);
                return;
            }
            if (found)
            {
                b.Act = ActRun; b.ActTime = 0f; b.ActLen = 6f; b.To = pick;
                b.Speed = 2.7f * Mathf.Lerp(.62f, 1f, b.Size);
                return;
            }
            // Cornered: a flapping jump on the spot, turned away, and a moment's nerve before the next.
            b.Heading = baseYaw;
            BeginFlutter(b, b.Pos, .3f * Mathf.Lerp(.5f, 1f, b.Size), -1);
            b.Ignore = Range(1.4f, 2.6f);
        }

        private void BeginFlutter(Bird b, Vector3 to, float height, int perchGoal)
        {
            b.Act = ActFlutter; b.ActTime = 0f;
            if (b.Size > .6f) Say("chicken_flap", b.Pos, .8f);
            b.ArcFrom = b.Pos; b.To = to; b.ArcHeight = height; b.PerchGoal = perchGoal;
            b.ActLen = .42f + Flat(to - b.Pos).magnitude * .16f + Mathf.Abs(to.y - b.Pos.y) * .25f;
            if (b.Perch >= 0) { _perchOwner[b.G][b.Perch] = -1; b.Perch = -1; }
        }

        // ---------------------------------------------------------------- one bird

        private void StepBird(Bird b, float dt)
        {
            var g = Groups[b.G];
            b.ActTime += dt;
            if (b.Panic > 0f) b.Panic -= dt;
            if (b.Ignore > 0f) b.Ignore -= dt;
            if (b.Act == ActDead) { StepDead(b, dt); return; }

            bool calm = b.Act < ActRun;      // not a run, a flutter or the walk back in
            if (calm)
            {
                if (b.Perch >= 0)
                {
                    // Up on its perch it only watches: it comes down when nobody has been near for a while.
                    if (Threat(b, PlayerStartle + 1.5f, out var near)) { b.CalmLeft = Mathf.Max(b.CalmLeft, Range(3f, 6f)); b.ThreatAt = near; }
                    else b.CalmLeft -= dt;
                    b.Startled = false;
                }
                else if (b.Startled)
                {
                    b.React -= dt;
                    if (b.React <= 0f) { b.Startled = false; BeginFlee(b, b.ThreatAt); }
                }
                else if (b.Ignore <= 0f && Threat(b, PlayerStartle, out var at)) StartleGroup(b.G, at);
            }

            float pitch = 0f, headPitch = 0f, headYaw = 0f, headRoll = 0f, wing = 0f, legL = 0f, legR = 0f, lift = 0f, roll = 0f, bob = 0f;
            float t = b.ActLen > 0f ? Mathf.Clamp01(b.ActTime / b.ActLen) : 1f;
            bool done = b.ActTime >= b.ActLen;
            switch (b.Act)
            {
                case ActIdle:
                    headYaw = b.LookYaw; headRoll = b.LookRoll;
                    break;
                case ActWalk:
                {
                    done = Advance(b, b.Speed, 320f, dt);
                    float swing = Mathf.Sin(b.Stride * Mathf.PI);
                    legL = 27f * swing; legR = -27f * swing;
                    roll = 3.5f * swing; pitch = 3f;
                    // ⚠️ THE CHICKEN'S HEAD: it holds still in the air while the body walks under it,
                    // then darts forward to the next hold. One dart per step.
                    float f = b.Stride - Mathf.Floor(b.Stride);
                    bob = (.5f - f) * b.StepLen * .85f;
                    lift = Mathf.Abs(swing) * .006f * b.Size;
                    if (!done && b.ActTime > 8f) done = true;
                    break;
                }
                case ActPeck:
                {
                    // Two or three quick dips: the body tips at the hips and the neck reaches the rest of the way.
                    float d = Mathf.Abs(Mathf.Sin(t * Mathf.PI * b.Dips));
                    d = d * d * (3f - 2f * d);
                    pitch = 6f + 31f * d; headPitch = 48f * d;
                    break;
                }
                case ActLook:
                    // Jerks, never a sweep: a new angle is snapped to and held.
                    b.LookLeft -= dt;
                    if (b.LookLeft <= 0f) { b.LookLeft = Range(.3f, .8f); b.LookYaw = Range(-68f, 68f); b.LookRoll = Range(-16f, 16f); }
                    headYaw = b.LookYaw; headRoll = b.LookRoll; headPitch = -4f;
                    break;
                case ActScratch:
                {
                    // Two raking kicks back, one foot then the other, then it pecks at what it turned up.
                    float k = t * 2f; bool left = k < 1f; float kick = Mathf.Max(0f, Mathf.Sin((k - Mathf.Floor(k)) * Mathf.PI * 2f));
                    if (left) legL = 52f * kick; else legR = 52f * kick;
                    pitch = 12f; headPitch = 14f; roll = left ? -3f : 3f;
                    break;
                }
                case ActCrow:
                {
                    // Up on his toes, chest out, head thrown back, wings shivering. (No sound yet.)
                    float up = Mathf.Clamp01(t * 5f) * Mathf.Clamp01((1f - t) * 5f);
                    pitch = -22f * up; headPitch = -34f * up; lift = .012f * up;
                    wing = up * (15f + 5f * Mathf.Sin(b.ActTime * 38f));
                    break;
                }
                case ActAlert:
                {
                    var to = Flat(b.ThreatAt - b.Pos);
                    float bearing = to.sqrMagnitude > .01f ? Mathf.DeltaAngle(b.Heading, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg) : 0f;
                    headYaw = Mathf.Clamp(bearing, -75f, 75f); pitch = -7f; headPitch = -6f;
                    wing = 10f * (1f - t);
                    break;
                }
                case ActRun:
                {
                    // ⚠️ THE SCATTER: a hopping run with the wings beating, neck stretched out ahead.
                    done = Advance(b, b.Speed, 900f, dt) || b.ActTime >= b.ActLen;
                    b.Flap += dt * 9.5f * Mathf.PI * 2f;
                    float swing = Mathf.Sin(b.Stride * Mathf.PI * .5f);
                    legL = 42f * swing; legR = -42f * swing;
                    lift = Mathf.Abs(Mathf.Sin(b.Stride * Mathf.PI * .25f)) * .07f * Mathf.Lerp(.5f, 1f, b.Size);
                    wing = 56f + 30f * Mathf.Sin(b.Flap);
                    pitch = 15f; headPitch = -20f;
                    if (done)
                    {
                        if (Threat(b, PlayerStartle, out var still)) { BeginFlee(b, still); done = false; }
                        else { b.Act = ActAlert; b.ActTime = 0f; b.ActLen = Range(.9f, 1.8f); b.CalmLeft = 0f; done = false; }
                    }
                    break;
                }
                case ActReturn:
                {
                    // Back from wherever a burst bird goes: out of its hiding place onto the floor, a little hurried.
                    done = Advance(b, b.Speed, 500f, dt);
                    float swing = Mathf.Sin(b.Stride * Mathf.PI);
                    legL = 27f * swing; legR = -27f * swing; roll = 3.5f * swing; pitch = 3f;
                    float f = b.Stride - Mathf.Floor(b.Stride);
                    bob = (.5f - f) * b.StepLen * .85f;
                    if (done || b.ActTime > 8f) { b.Pos = new Vector3(b.To.x, g.FloorY, b.To.z); Begin(b, ActLook, Range(1f, 1.8f)); b.LookLeft = 0f; done = false; }
                    break;
                }
                case ActFlutter:
                {
                    float s = t * t * (3f - 2f * t);
                    var p = Vector3.Lerp(b.ArcFrom, b.To, s);
                    p.y = Mathf.Lerp(b.ArcFrom.y, b.To.y, s) + b.ArcHeight * 4f * t * (1f - t);
                    var flat = Flat(b.To - b.ArcFrom);
                    if (flat.sqrMagnitude > .01f) b.Heading = Mathf.MoveTowardsAngle(b.Heading, Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg, 900f * dt);
                    b.Pos = p;
                    b.Flap += dt * 11f * Mathf.PI * 2f;
                    wing = 58f + 30f * Mathf.Sin(b.Flap);
                    pitch = -16f * Mathf.Sin(t * Mathf.PI); headPitch = 8f; legL = legR = 24f * Mathf.Sin(t * Mathf.PI);
                    if (done)
                    {
                        b.Pos = b.To;
                        b.Perch = b.PerchGoal; b.PerchGoal = -1;
                        b.Act = ActAlert; b.ActTime = 0f; b.ActLen = Range(.8f, 1.6f);
                        b.CalmLeft = b.Perch >= 0 ? Range(4f, 8f) : 0f;
                        done = false;
                    }
                    break;
                }
            }
            if (done && b.Act < ActRun) NextAct(b);
            // A bird fluttering in from nothing (a group with no hidden entry) grows as it comes down.
            float grow = b.Act == ActFlutter && b.ArcFrom.y > b.To.y + .3f && b.ArcHeight < .06f ? Mathf.Clamp01(b.ActTime / .5f) : 1f;
            if (b.Root.localScale.x != b.Scale * grow) b.Root.localScale = Vector3.one * (b.Scale * grow);

            // Ease the pose: the head and wings quickly (a bird's head snaps), the body a little slower.
            float fast = 1f - Mathf.Exp(-24f * dt), slow = 1f - Mathf.Exp(-12f * dt), snap = 1f - Mathf.Exp(-40f * dt);
            b.Pitch = Mathf.Lerp(b.Pitch, pitch, slow); b.Roll = Mathf.Lerp(b.Roll, roll, slow);
            b.HeadPitch = Mathf.Lerp(b.HeadPitch, headPitch, fast);
            b.HeadYaw = Mathf.Lerp(b.HeadYaw, headYaw, fast); b.HeadRoll = Mathf.Lerp(b.HeadRoll, headRoll, fast);
            b.Wing = Mathf.Lerp(b.Wing, wing, snap);
            b.LegLAngle = Mathf.Lerp(b.LegLAngle, legL, fast); b.LegRAngle = Mathf.Lerp(b.LegRAngle, legR, fast);
            b.Lift = Mathf.Lerp(b.Lift, lift, fast); b.Bob = Mathf.Lerp(b.Bob, bob, snap);

            b.Root.SetPositionAndRotation(b.Pos + Vector3.up * b.Lift, Quaternion.Euler(0f, b.Heading, 0f));
            if (b.Torso != null) b.Torso.localRotation = b.TorsoRest * Quaternion.Euler(b.Pitch, 0f, b.Roll);
            if (b.Head != null)
            {
                b.Head.localRotation = b.HeadRest * Quaternion.Euler(b.HeadPitch, b.HeadYaw, b.HeadRoll);
                b.Head.localPosition = b.HeadAt + new Vector3(0f, 0f, b.Bob);
            }
            // Positive Z roll lifts +X: the right wing (on +X, the bird facing +Z) takes the angle, the left its negative.
            if (b.WingL != null) b.WingL.localRotation = b.WingLRest * Quaternion.Euler(0f, 0f, -b.Wing);
            if (b.WingR != null) b.WingR.localRotation = b.WingRRest * Quaternion.Euler(0f, 0f, b.Wing);
            if (b.LegL != null) b.LegL.localRotation = b.LegLRest * Quaternion.Euler(b.LegLAngle, 0f, 0f);
            if (b.LegR != null) b.LegR.localRotation = b.LegRRest * Quaternion.Euler(b.LegRAngle, 0f, 0f);
        }

        /// <summary>Turns toward the goal and steps at it. True on arrival.</summary>
        private static bool Advance(Bird b, float speed, float turnRate, float dt)
        {
            var to = Flat(b.To - b.Pos); float dist = to.magnitude;
            if (dist < .02f) return true;
            float want = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            b.Heading = Mathf.MoveTowardsAngle(b.Heading, want, turnRate * dt);
            float off = Mathf.Abs(Mathf.DeltaAngle(b.Heading, want));
            float move = Mathf.Min(dist, speed * dt * (off < 40f ? 1f : .12f));
            b.Pos += to / dist * move; b.Stride += move / b.StepLen;
            return false;
        }

        private void Begin(Bird b, int act, float length)
        {
            b.Act = act; b.ActTime = 0f; b.ActLen = length;
            if (act != ActLook && act != ActIdle) { b.LookYaw = 0f; b.LookRoll = 0f; }
        }

        private bool BeginWalk(Bird b, Vector3 to, float speedScale)
        {
            var g = Groups[b.G];
            if (!Walkable(g, to.x, to.z) || !Clear(g, b.Pos, to) || Crowded(b, to)) return false;
            to.y = g.FloorY;
            Begin(b, ActWalk, 0f); b.To = to;
            b.Speed = .42f * Mathf.Lerp(.55f, 1f, b.Size) * speedScale;
            return true;
        }

        // Next act. Mostly steps and pecks, a look about now and then, a pause in between: never frozen.
        private void NextAct(Bird b)
        {
            var g = Groups[b.G];
            if (b.ThenPeck) { b.ThenPeck = false; b.Dips = 2; Begin(b, ActPeck, Range(.8f, 1.1f)); return; }
            double roll = _random.NextDouble();

            if (b.Perch >= 0)
            {
                if (b.CalmLeft <= 0f)
                {
                    // Back down: onto a walkable cell beside the perch, the one nearest home of a few tries.
                    var down = g.Home; float best = float.MaxValue; bool found = false;
                    for (int k = 0; k < 14; k++)
                    {
                        var p = b.Pos + Dir(Range(0f, 360f)) * Range(.45f, 1.3f);
                        if (!Walkable(g, p.x, p.z)) continue;
                        float d = Flat(p - g.Home).sqrMagnitude;
                        if (d < best) { best = d; down = p; found = true; }
                    }
                    if (found) { down.y = g.FloorY; BeginFlutter(b, down, .12f, -1); return; }
                    b.CalmLeft = 2f;
                }
                if (b.Rooster && roll < .22) { Begin(b, ActCrow, Range(1.6f, 2.2f)); Say("rooster_crow", b.Pos); }
                else if (roll < .7) { Begin(b, ActLook, Range(1.2f, 2.6f)); b.LookLeft = 0f; }
                else { Begin(b, ActIdle, Range(.5f, 1.4f)); b.LookYaw = Range(-25f, 25f); b.LookRoll = 0f; }
                return;
            }

            // A chick keeps to its hen: it scurries after her when she has gone on.
            if (b.Follow >= 0 && b.Follow < _groupBirds[b.G].Length && _groupBirds[b.G][b.Follow] >= 0)
            {
                var hen = _birds[_groupBirds[b.G][b.Follow]];
                float apart = Flat(hen.Pos - b.Pos).magnitude;
                if (hen.Perch < 0 && hen.Act != ActDead && (apart > .75f || roll < .3))
                {
                    for (int k = 0; k < 8; k++)
                    {
                        var p = hen.Pos + Dir(Range(0f, 360f)) * Range(.24f, .5f);
                        if (BeginWalk(b, p, apart > 1.3f ? 3.2f : apart > .75f ? 1.9f : 1f)) return;
                    }
                }
            }

            float fromHome = Flat(b.Pos - g.Home).magnitude;
            if (roll < .36 || fromHome > g.Roam * .8f)
            {
                var home = Flat(g.Home - b.Pos);
                float homeward = Mathf.Atan2(home.x, home.z) * Mathf.Rad2Deg;
                for (int k = 0; k < 10; k++)
                {
                    float yaw = fromHome > g.Roam * .6f ? homeward + Range(-50f, 50f) : k < 5 ? b.Heading + Range(-75f, 75f) : Range(0f, 360f);
                    var p = b.Pos + Dir(yaw) * Range(.3f, 1.25f) * Mathf.Lerp(.5f, 1f, b.Size);
                    if (BeginWalk(b, p, 1f)) return;
                }
                // A narrow strip defeats a guessed bearing: any nearby point of the floor it can walk straight to.
                for (int k = 0; k < 12; k++)
                {
                    var p = new Vector3(b.Pos.x + Range(-1.2f, 1.2f), g.FloorY, b.Pos.z + Range(-1.2f, 1.2f));
                    if (Flat(p - b.Pos).sqrMagnitude > .06f && BeginWalk(b, p, 1f)) return;
                }
                Begin(b, ActLook, Range(1f, 1.8f)); b.LookLeft = 0f;
            }
            else if (roll < .66)
            {
                b.Dips = _random.NextDouble() < .5 ? 2 : 3; Begin(b, ActPeck, .42f * b.Dips + Range(0f, .2f));
                if (b.Size > .6f && _random.NextDouble() < .3) Say("chicken_cluck", b.Pos, .7f);
            }
            else if (roll < .8) { Begin(b, ActLook, Range(1.2f, 2.4f)); b.LookLeft = 0f; }
            else if (roll < .89) { Begin(b, ActScratch, .8f); b.ThenPeck = true; }
            else if (roll < .94 && b.Rooster) { Begin(b, ActCrow, Range(1.6f, 2.2f)); Say("rooster_crow", b.Pos); }
            else { Begin(b, ActIdle, Range(.4f, 1.3f)); b.LookYaw = Range(-25f, 25f); b.LookRoll = Range(-8f, 8f); }
        }

        // ---------------------------------------------------------------- the slipper

        // ⚠️ As the Lagoon measures it: the slipper's own transform between frames (identical on the
        // host and on a client, whose slipper is driven by snapshots), 4 m/s or more, not carried.
        // The slipper is only READ: it flies on untouched. The bird bursts.
        private void CheckSlipperHits(float dt)
        {
            float thrown2 = ThrownSpeed * dt * ThrownSpeed * dt;
            for (int k = 0; k < _slipperCount; k++)
            {
                var s = _slippers[k];
                var a = _slipperPrev[k]; var c = _slipperNow[k];
                if (s == null || s.State == SlipperState.Held || (c - a).sqrMagnitude < thrown2) continue;
                foreach (var b in _birds)
                {
                    if (b.Act == ActDead) continue;
                    var body = b.Pos + Vector3.up * (.2f * b.Size);
                    var ab = c - a; float len2 = ab.sqrMagnitude;
                    float u = len2 > 1e-8f ? Mathf.Clamp01(Vector3.Dot(body - a, ab) / len2) : 0f;
                    if ((a + ab * u - body).sqrMagnitude > HitRadius * HitRadius) continue;
                    Burst(b, body);
                    StartleGroup(b.G, a);
                }
            }
        }

        // The Lagoon's burst: the bird is hidden, 25 to 35 feathers and a puff of down fly, and it waits 20 to 40 s.
        private void Burst(Bird b, Vector3 at)
        {
            if (b.Perch >= 0) { _perchOwner[b.G][b.Perch] = -1; b.Perch = -1; }
            if (b.PerchGoal >= 0) { _perchOwner[b.G][b.PerchGoal] = -1; b.PerchGoal = -1; }
            b.Act = ActDead; b.ActTime = 0f; b.Startled = false; b.Panic = 0f;
            b.DeadLeft = Range(20f, 40f);
            b.Root.gameObject.SetActive(false);
            Bursts++;
            Puff(at, Groups[b.G].FloorY, b.Size > .6f ? _random.Next(25, 36) : 10);
            Say("chicken_squawk", at);
            Say("chicken_flap", at, .8f);
            BirdBurst?.Invoke(at);
        }

        // ⚠️ IT COMES BACK ON FOOT. A Lagoon bird respawns out of sight behind its flock and flies in; a
        // chicken has no flock in the sky, so it reappears INSIDE one of its group's hidden entries (the
        // tepee, a crate stack, a drum: placed models have no collision and hide it) and walks out to
        // the nearest floor. It picks the entry farthest from everybody and waits while anyone stands
        // within 3 m of it. A group with no entry uses its floor cell farthest from everybody, and the
        // bird grows from nothing there over half a second as it flutters down: the least bad pop.
        private void StepDead(Bird b, float dt)
        {
            b.DeadLeft -= dt;
            if (b.DeadLeft > 0f) return;
            var g = Groups[b.G];
            Vector3 from = g.Home; float best = -1f; bool hidden = false;
            if (g.Entries != null)
                foreach (var e in g.Entries)
                {
                    float d = NearestBody(e);
                    if (d > best) { best = d; from = e; hidden = true; }
                }
            if (!hidden)
                for (int k = 0; k < 24; k++)
                {
                    var p = g.Home + Dir(Range(0f, 360f)) * Range(.5f, g.Roam);
                    if (!Walkable(g, p.x, p.z)) continue;
                    float d = NearestBody(p);
                    if (d > best) { best = d; from = p; }
                }
            if (best < 3f) { b.DeadLeft = 3f; return; }
            // The floor cell nearest the entry.
            Vector3 to = g.Home; float near = float.MaxValue;
            for (int j = 0; j < g.Height; j++)
                for (int i = 0; i < g.Width; i++)
                {
                    if (j * g.Width + i >= g.Walkable.Length || !g.Walkable[j * g.Width + i]) continue;
                    var c = new Vector3(g.GridOrigin.x + (i + .5f) * g.Cell, g.FloorY, g.GridOrigin.y + (j + .5f) * g.Cell);
                    float d = Flat(c - from).sqrMagnitude;
                    if (d < near) { near = d; to = c; }
                }
            from.y = g.FloorY;
            b.Pos = from; b.To = to; b.Heading = Mathf.Atan2(to.x - from.x, to.z - from.z) * Mathf.Rad2Deg;
            b.Root.gameObject.SetActive(true);
            b.Root.SetPositionAndRotation(b.Pos, Quaternion.Euler(0f, b.Heading, 0f));
            b.Pitch = b.HeadPitch = b.Wing = b.Lift = 0f;
            if (hidden) { b.Act = ActReturn; b.ActTime = 0f; b.ActLen = 0f; b.Speed = .6f * Mathf.Lerp(.55f, 1f, b.Size); }
            else { b.Pos = to + Vector3.up * .5f; BeginFlutter(b, to, .05f, -1); }
        }

        private float NearestBody(Vector3 at)
        {
            float best = 99f;
            foreach (var m in _motors)
            {
                if (m == null || !m.isActiveAndEnabled) continue;
                best = Mathf.Min(best, Flat(m.transform.position - at).magnitude);
            }
            if (ProbeThreats != null) foreach (var p in ProbeThreats) best = Mathf.Min(best, Flat(p - at).magnitude);
            return best;
        }

        // ---------------------------------------------------------------- feathers

        private void Puff(Vector3 at, float floor, int count)
        {
            if (FeatherMaterials == null || FeatherMaterials.Length == 0) return;
            if (_featherMesh == null) _featherMesh = FeatherCard(.06f, .15f);
            for (int n = 0; n < count; n++)
            {
                int i = _featherNext; _featherNext = (_featherNext + 1) % FeatherCapacity;
                if (_fT[i] == null)
                {
                    if (_featherBuilt >= FeatherCapacity) continue;
                    var go = new GameObject("Feather") { hideFlags = HideFlags.DontSave };
                    go.transform.SetParent(transform, false);
                    go.AddComponent<MeshFilter>().sharedMesh = _featherMesh;
                    var r = go.AddComponent<MeshRenderer>();
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    _fT[i] = go.transform; _featherBuilt++;
                }
                var material = FeatherMaterials[_random.Next(FeatherMaterials.Length)];
                if (material == null) continue;
                _fT[i].GetComponent<MeshRenderer>().sharedMaterial = material;
                _fT[i].gameObject.SetActive(true);
                _fT[i].SetPositionAndRotation(at + new Vector3(Range(-.06f, .06f), Range(-.04f, .08f), Range(-.06f, .06f)), Quaternion.Euler(Range(0f, 360f), Range(0f, 360f), Range(0f, 360f)));
                float yaw = Range(0f, Mathf.PI * 2f);
                _fVel[i] = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw)) * Range(.5f, 1.8f) + Vector3.up * Range(.8f, 2.4f);
                _fAxis[i] = new Vector3(Range(-1f, 1f), Range(-1f, 1f), Range(-1f, 1f)).normalized;
                _fAge[i] = 0f; _fLife[i] = Range(1.6f, 2.8f); _fFloor[i] = floor + .01f; _fPhase[i] = Range(0f, 6.28f); _fSize[i] = Range(.7f, 1.2f);
            }
        }

        // A burst outward, then air drag takes it and it rocks down like a leaf, lies a moment and shrinks away.
        private void StepFeathers(float dt)
        {
            for (int i = 0; i < FeatherCapacity; i++)
            {
                var t = _fT[i];
                if (t == null || !t.gameObject.activeSelf) continue;
                _fAge[i] += dt;
                if (_fAge[i] >= _fLife[i]) { t.gameObject.SetActive(false); continue; }
                var p = t.position;
                if (p.y > _fFloor[i])
                {
                    var v = _fVel[i];
                    v.y -= 5f * dt; v *= Mathf.Exp(-2.4f * dt);
                    if (v.y < -.55f) v.y = -.55f;
                    _fVel[i] = v;
                    float sway = Mathf.Sin(_fAge[i] * 5f + _fPhase[i]) * .25f;
                    p += (v + new Vector3(sway, 0f, Mathf.Cos(_fAge[i] * 4f + _fPhase[i]) * .2f)) * dt;
                    if (p.y < _fFloor[i]) p.y = _fFloor[i];
                    t.SetPositionAndRotation(p, Quaternion.AngleAxis(260f * dt, _fAxis[i]) * t.rotation);
                }
                float left = _fLife[i] - _fAge[i];
                t.localScale = Vector3.one * (_fSize[i] * Mathf.Clamp01(left / .35f));
            }
        }

        /// <summary>A feather card along +Z, arched a little along its length (the Lagoon's card,
        /// smaller). Drawn two-sided by the material.</summary>
        private static Mesh FeatherCard(float width, float length)
        {
            const int rows = 3;
            var v = new Vector3[(rows + 1) * 2]; var uv = new Vector2[v.Length]; var nrm = new Vector3[v.Length];
            var tri = new int[rows * 6];
            for (int r = 0; r <= rows; r++)
            {
                float t = r / (float)rows, arch = Mathf.Sin(t * Mathf.PI) * length * .1f;
                for (int c = 0; c < 2; c++)
                {
                    int k = r * 2 + c;
                    v[k] = new Vector3((c - .5f) * width, arch, (t - .5f) * length);
                    uv[k] = new Vector2(c, t); nrm[k] = Vector3.up;
                }
            }
            for (int r = 0, q = 0; r < rows; r++)
            {
                int a = r * 2, b = a + 1, d = a + 2, e = a + 3;
                tri[q++] = a; tri[q++] = d; tri[q++] = b;
                tri[q++] = b; tri[q++] = d; tri[q++] = e;
            }
            var mesh = new Mesh { name = "Chicken feather", hideFlags = HideFlags.DontSave };
            mesh.vertices = v; mesh.uv = uv; mesh.normals = nrm; mesh.triangles = tri;
            mesh.RecalculateBounds();
            return mesh;
        }

        private void OnDrawGizmosSelected()
        {
            if (Groups == null) return;
            foreach (var g in Groups)
            {
                if (g == null || g.Walkable == null) continue;
                Gizmos.color = new Color(.3f, .9f, .4f, .5f);
                for (int j = 0; j < g.Height; j++)
                    for (int i = 0; i < g.Width; i++)
                        if (j * g.Width + i < g.Walkable.Length && g.Walkable[j * g.Width + i])
                            Gizmos.DrawCube(new Vector3(g.GridOrigin.x + (i + .5f) * g.Cell, g.FloorY + .01f, g.GridOrigin.y + (j + .5f) * g.Cell), new Vector3(g.Cell * .8f, .01f, g.Cell * .8f));
                Gizmos.color = Color.yellow;
                if (g.Perches != null) foreach (var p in g.Perches) Gizmos.DrawWireSphere(p, .12f);
            }
        }
    }
}
