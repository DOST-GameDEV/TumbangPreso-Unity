using System;
using System.Collections.Generic;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace TumbangPreso
{
    /// <summary>
    /// SIDEWALK LIFE on the Ilalim rebuild (owner 2026-09-30: "could we have some events along the
    /// side walks like kids chasing each other around, some taho vendor, people stopping by to
    /// watch, maybe some beggar that comes sits down that you can interact with to donate to").
    /// Authored by `IlalimSidewalkAuthor` (the routes, the looks, the props' materials); the people
    /// are built here at Start, the LagoonResident way: a Classic rig from the roster, scaled by
    /// the cast's 2.38, dressed by `ToonSkin` with its own everyday palette, animated from the
    /// rig's own Kenney clips (idle, walk, sprint, sit, emote-yes, emote-no, pick-up) plus a few
    /// procedural beats (the resident's arm cheer, a bow, a head shake, a laughing hop).
    ///
    /// ⚠️⚠️ SCENERY, NEVER A PLAYER. No CharacterMotor, no collider (the rig's are destroyed), no
    /// network state: every client runs its own, like `KantoTraffic` and `LagoonFlocks`. They walk
    /// only the authored routes, which the author measured against the art and which never enter
    /// the play area (|x| &lt; 11.2, |z| &lt; 16.7), let alone the chalk box.
    ///   * KIDS: a game of tag on the south-east pavement, now and then (hidden between sessions).
    ///   * TAHO: the magtataho with his yoke and two aluminium buckets, slow along the south-west
    ///     pavement, stopping to call. ⚠️ No taho call clip exists in the repo, so the call is a
    ///     small comic popup ("TAHOOO!"), no sound.
    ///   * SPECTATORS: passers-by who walk to a spot at the court's edge (the pavement ends past
    ///     the end walls, the PGH lawn behind the fence), watch facing the court, cheer when the
    ///     can goes down and groan at a tag (<see cref="MatchFlair.Presented"/>), then walk on.
    ///   * BEGGAR: walks in, sits on a flattened carton against the PGH fence just past the
    ///     south wall with a tin cup, within reach of a player at the wall. While he sits the
    ///     match HUD offers "Give a coin" on the Interact control (<see cref="StreetInteractions"/>):
    ///     a coin arcs into the cup, a coin blip plays (the pisonet's "ui_click"), he bows and
    ///     waves and says thank you in a comic popup. ⚠️ COSMETIC: no currency, no score, no stat,
    ///     nothing networked. After a while he stands, packs up and leaves, and comes back later.
    /// People appear and vanish only at the far ends of their routes and only while that point
    /// is off the main camera's screen. <see cref="Simulate"/> is the whole step, so the builder's
    /// probe drives exactly what Play runs.
    /// </summary>
    public sealed class SidewalkLife : MonoBehaviour
    {
        [Serializable] public sealed class Look { public string Name; public RosterEntryAsset Art; public Color[] Palette; public float Scale = 1f; }
        /// <summary>A sidewalk route: Points[0] is the far end where the person appears and vanishes.
        /// Width (optional, one per point, 0 to 1) narrows the keep-right offset where the way
        /// squeezes between two posts.</summary>
        [Serializable] public sealed class Walk { public string Name; public Vector3[] Points; public float[] Width; }
        /// <summary>A place to watch from: the end of `Walk`, facing `LookAt`.</summary>
        [Serializable] public sealed class Watch { public string Name; public int Walk; public Vector3 LookAt; }

        [Header("People")]
        public Look Taho;
        public Look Beggar;
        public Look[] Kids = new Look[0];
        public Look[] Spectators = new Look[0];

        [Header("Where")]
        public Walk[] Walks = new Walk[0];
        public Watch[] Watches = new Watch[0];
        public int TahoWalk = -1, BeggarWalk = -1;
        /// <summary>The kids' pavement: they play along it; its LAST point is where they come and go.</summary>
        public Vector3[] KidTrack = new Vector3[0];
        public float KidHalfWidth = .7f;
        public Vector3 BeggarSeat;
        public Vector3 BeggarFacing = Vector3.right;
        public float BeggarReach = 2.6f;

        [Header("Props (source colours; ToonSkin dresses them)")]
        public Material Bamboo;
        public Material Aluminium, Lid, Rope, Tin, Cardboard, Coin;

        [Header("Timing and pace")]
        public int Seed = 7;
        public Vector2 KidsFirst = new Vector2(20f, 35f), KidsPlay = new Vector2(45f, 75f), KidsAway = new Vector2(70f, 130f);
        public Vector2 TahoFirst = new Vector2(4f, 10f), TahoAway = new Vector2(45f, 90f), TahoStopEvery = new Vector2(6f, 11f), TahoStop = new Vector2(3f, 5.5f);
        public Vector2 SpectatorFirst = new Vector2(2f, 10f), SpectatorAway = new Vector2(6f, 22f), SpectatorWatch = new Vector2(14f, 32f);
        public Vector2 BeggarFirst = new Vector2(10f, 18f), BeggarSits = new Vector2(80f, 140f), BeggarAway = new Vector2(50f, 100f);
        public float WalkSpeed = 1.15f, TahoSpeed = .8f, BeggarSpeed = .75f, KidRun = 2.5f;
        public string DonateAction = "Give a coin";
        public string CoinCue = "ui_click";

        // ------------------------------------------------------------------ constants

        private const float PersonScale = 2.38f, KeepRight = .35f, Fade = .22f, YokeHeight = .56f;
        /// <summary>The sit clip drops the hips to 6 cm with the legs 15 degrees below level; this
        /// sets him ON his carton rather than sunk to the knees in the pavement.</summary>
        private const float SitLift = .07f;
        private static readonly string[] ClipNames = { "idle", "walk", "sprint", "sit", "emote-yes", "emote-no", "pick-up" };
        private const int Idle = 0, WalkClip = 1, SprintClip = 2, SitClip = 3, YesClip = 4, NoClip = 5, PickUpClip = 6;
        private static readonly bool[] Loops = { true, true, true, false, true, true, false };
        /// <summary>Metres one cycle covers at the cast's scale (CharacterAnimator's .92 walk, 1.12 run).</summary>
        private const float WalkCycleMetres = .92f, RunCycleMetres = 1.12f;
        private static readonly string[] Thanks = { "SALAMAT PO!", "SALAMAT, IDOL!", "PAGPALAIN KA!", "SALAMAT, BOSS!" };

        // Walker phases.
        private const int Hidden = 0, Going = 1, Arrived = 2, Leaving = 3, Vanishing = 4, Settling = 5, Seated = 6, Rising = 7;

        // ------------------------------------------------------------------ runtime state

        private sealed class Body
        {
            public string Name, Role;
            public Transform Root, Torso, Head, ArmL;
            public Quaternion ArmLRest;
            public float Scale = 1f;
            public PlayableGraph Graph;
            public AnimationMixerPlayable Mixer;
            public readonly AnimationClip[] Clips = new AnimationClip[ClipNames.Length];
            public readonly float[] Weight = new float[ClipNames.Length];
            public int Clip = -1;
            public Vector3 Position, Heading;
            public float Yaw, Lift;
            public bool Shown, Walking;
            public string State = "hidden";
            public float CheerUntil, ShakeUntil, BowUntil, LaughUntil, CallUntil, MotionSpeed;
            public Transform Yoke;
            public Transform[] Swing = new Transform[0];
        }

        private sealed class Line
        {
            public readonly Vector3[] P;
            public readonly float[] S, W;
            public readonly float Length;
            public Line(Vector3[] points, float[] width = null)
            {
                P = points ?? new Vector3[0];
                S = new float[P.Length];
                W = new float[P.Length];
                for (int k = 0; k < P.Length; k++) W[k] = width != null && k < width.Length ? Mathf.Clamp01(width[k]) : 1f;
                for (int k = 1; k < P.Length; k++) S[k] = S[k - 1] + Vector3.Distance(P[k - 1], P[k]);
                Length = P.Length > 0 ? S[P.Length - 1] : 0f;
            }
            public Vector3 At(float s, out Vector3 tangent)
            {
                tangent = Vector3.forward;
                if (P.Length == 0) return Vector3.zero;
                if (P.Length == 1) return P[0];
                s = Mathf.Clamp(s, 0f, Length);
                int k = 1;
                while (k < P.Length - 1 && S[k] < s) k++;
                var d = P[k] - P[k - 1]; d.y = 0f;
                if (d.sqrMagnitude > 1e-8f) tangent = d.normalized;
                float span = S[k] - S[k - 1];
                return span > 1e-5f ? Vector3.Lerp(P[k - 1], P[k], (s - S[k - 1]) / span) : P[k];
            }
            public float WidthAt(float s)
            {
                if (P.Length < 2) return 1f;
                s = Mathf.Clamp(s, 0f, Length);
                int k = 1;
                while (k < P.Length - 1 && S[k] < s) k++;
                float span = S[k] - S[k - 1];
                return span > 1e-5f ? Mathf.Lerp(W[k - 1], W[k], (s - S[k - 1]) / span) : W[k];
            }
        }

        private sealed class Walker
        {
            public Body Body;
            public int Phase, Line = -1, Watch = -1, Dir = 1;
            public float Along, Timer, NextStop, Lateral;
        }

        private sealed class Kid
        {
            public Body Body;
            public float Along, Lateral, LateralTarget, Pause, Burst, NextDart, Dir = -1f, Pace = 1f;
            public bool It, Home;
        }

        private readonly List<Body> _all = new List<Body>();
        private readonly List<Walker> _walkers = new List<Walker>();
        private readonly List<Walker> _spectators = new List<Walker>();
        private readonly List<Kid> _kids = new List<Kid>();
        private Line[] _lines = new Line[0];
        private Line _track;
        private Walker _taho, _beggar;
        private int _kidsPhase;
        private float _kidsTimer, _clock, _donateReady, _coinT = -1f;
        private Vector3 _coinFrom;
        private Transform _carton, _cup, _coin;
        private System.Random _rng;
        private bool _begun;
        private CharacterMotor _local;
        private float _localScan, _slipperScan;
        private Slipper[] _slippers = new Slipper[0];
        private bool _interactHeld;

        // ------------------------------------------------------------------ probe surface

        public int PeopleCount => _all.Count;
        public string PersonName(int i) => _all[i].Name;
        public string PersonRole(int i) => _all[i].Role;
        public bool PersonShown(int i) => _all[i].Shown;
        public Vector3 PersonPosition(int i) => _all[i].Position;
        public string PersonState(int i) => _all[i].State;
        public float Clock => _clock;
        public int Donations { get; private set; }
        public bool BeggarSeated => _beggar != null && _beggar.Phase == Seated;
        public bool BeggarThanking => _beggar != null && _clock < _beggar.Body.BowUntil;
        public Vector3 CupPosition => _cup != null ? _cup.position : BeggarSeat;
        public bool KidsOut => _kidsPhase != Hidden;
        public int Watching { get { int n = 0; foreach (var s in _spectators) if (s.Phase == Arrived) n++; return n; } }
        public int Cheering { get { int n = 0; foreach (var s in _spectators) if (_clock < s.Body.CheerUntil) n++; return n; } }

        /// <summary>The whole per-frame step (Update calls it with Time.deltaTime).</summary>
        public void Simulate(float dt) { Begin(); Step(dt); }

        /// <summary>A match moment, as <see cref="MatchFlair.Presented"/> delivers it.</summary>
        public void React(MatchFlair.Kind kind, Vector3 at) => OnFlair(kind, -1, -1, at, 1f);

        // ------------------------------------------------------------------ lifecycle

        private void OnEnable() => MatchFlair.Presented += OnFlair;
        private void OnDisable() => MatchFlair.Presented -= OnFlair;
        private void Start() => Begin();

        private void Update()
        {
            Step(Time.deltaTime);
            Interact();
        }

        private void OnDestroy()
        {
            foreach (var b in _all) if (b.Graph.IsValid()) b.Graph.Destroy();
            foreach (var m in _meshes.Values) if (m != null) Kill(m);
        }

        /// <summary>Builds the people and props once (Start, or the probe's first step).</summary>
        public void Begin()
        {
            if (_begun) return;
            _begun = true;
            _rng = new System.Random(Seed);
            _lines = new Line[Walks.Length];
            for (int i = 0; i < Walks.Length; i++) _lines[i] = new Line(Walks[i]?.Points, Walks[i]?.Width);
            _track = new Line(KidTrack);

            if (Taho != null && Taho.Art != null && Valid(TahoWalk))
            {
                _taho = new Walker { Body = MakeBody(Taho, "Taho", "taho"), Line = TahoWalk, Timer = Range(TahoFirst) };
                BuildYoke(_taho.Body);
                _walkers.Add(_taho);
            }
            if (Beggar != null && Beggar.Art != null && Valid(BeggarWalk))
            {
                _beggar = new Walker { Body = MakeBody(Beggar, "Beggar", "beggar"), Line = BeggarWalk, Timer = Range(BeggarFirst) };
                BuildBeggarProps();
                _walkers.Add(_beggar);
            }
            for (int i = 0; i < Spectators.Length; i++)
            {
                if (Spectators[i] == null || Spectators[i].Art == null || Watches.Length == 0) continue;
                var w = new Walker { Body = MakeBody(Spectators[i], "Spectator " + i, "spectator"), Timer = Range(SpectatorFirst) + i * 3f };
                _spectators.Add(w); _walkers.Add(w);
            }
            if (_track.P.Length >= 2)
                for (int i = 0; i < Kids.Length; i++)
                    if (Kids[i] != null && Kids[i].Art != null)
                        _kids.Add(new Kid { Body = MakeBody(Kids[i], "Kid " + i, "kid"), Pace = .93f + .07f * (i % 3) });
            _kidsTimer = Range(KidsFirst);
        }

        private bool Valid(int walk) => walk >= 0 && walk < _lines.Length && _lines[walk].P.Length >= 2;

        // ------------------------------------------------------------------ bodies

        private Body MakeBody(Look look, string name, string role)
        {
            var root = new GameObject(name).transform;
            root.SetParent(transform, false);
            var b = new Body { Name = name, Role = role, Root = root, Scale = Mathf.Max(.3f, look.Scale) };
            _all.Add(b);
            var model = Instantiate(look.Art.Model, root, false);
            model.name = "Model";
            model.transform.localScale = Vector3.one * PersonScale * b.Scale;
            model.transform.localRotation = Quaternion.Euler(0f, CharacterVisual.PersonModelYaw, 0f);
            foreach (var c in model.GetComponentsInChildren<Collider>(true)) { c.enabled = false; Kill(c); }
            var palette = look.Palette != null && look.Palette.Length == 16 ? look.Palette : look.Art.Palette;
            ToonSkin.Apply(model, ToonSkin.PersonOutlineWidth, palette);
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "torso") b.Torso = t;
                else if (t.name == "head") b.Head = t;
                else if (t.name == "arm-left") b.ArmL = t;
            }
            if (b.ArmL != null) b.ArmLRest = b.ArmL.localRotation;

            var animator = model.GetComponent<Animator>();
            if (animator == null) animator = model.AddComponent<Animator>();
            b.Graph = PlayableGraph.Create("Sidewalk " + name);
            b.Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            b.Mixer = AnimationMixerPlayable.Create(b.Graph, ClipNames.Length);
            for (int i = 0; i < ClipNames.Length; i++)
            {
                b.Clips[i] = FindClip(look.Art.Clips, ClipNames[i]);
                if (b.Clips[i] == null) continue;
                var playable = AnimationClipPlayable.Create(b.Graph, b.Clips[i]);
                b.Graph.Connect(playable, 0, b.Mixer, i);
                b.Mixer.SetInputWeight(i, 0f);
            }
            AnimationPlayableOutput.Create(b.Graph, "Body", animator).SetSourcePlayable(b.Mixer);
            b.Graph.Play();
            Play(b, Idle, 1f);
            Show(b, false);
            return b;
        }

        private static AnimationClip FindClip(AnimationClip[] clips, string name)
        {
            if (clips == null) return null;
            foreach (var c in clips) if (c != null && string.Equals(c.name, name, StringComparison.OrdinalIgnoreCase)) return c;
            foreach (var c in clips)
            {
                if (c == null) continue;
                int cut = c.name.LastIndexOfAny(new[] { '|', '/', ':' });
                if (cut >= 0 && string.Equals(c.name.Substring(cut + 1), name, StringComparison.OrdinalIgnoreCase)) return c;
            }
            return null;
        }

        private static void Kill(UnityEngine.Object o)
        {
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }

        private void Show(Body b, bool shown)
        {
            b.Shown = shown;
            if (b.Root.gameObject.activeSelf != shown) b.Root.gameObject.SetActive(shown);
            if (!shown) { b.State = "hidden"; return; }
            // Appearing mid-stride from the current clip, never blending in from the bind pose.
            for (int i = 0; i < ClipNames.Length; i++) b.Weight[i] = i == b.Clip ? 1f : 0f;
        }

        /// <summary>Crossfades to `clip` (falling back to idle when the rig lacks it) at `rate`.</summary>
        private void Play(Body b, int clip, float rate)
        {
            if (b.Clips[clip] == null) clip = Idle;
            if (b.Clips[clip] == null) return;
            if (b.Clip != clip)
            {
                if (!Loops[clip] || b.Weight[clip] < .01f) b.Mixer.GetInput(clip).SetTime(0);
                b.Clip = clip;
            }
            b.Mixer.GetInput(clip).SetSpeed(rate);
        }

        private void Pose(Body b, float dt)
        {
            if (!b.Shown || !b.Graph.IsValid()) return;
            for (int i = 0; i < ClipNames.Length; i++)
            {
                if (b.Clips[i] == null) continue;
                b.Weight[i] = Mathf.MoveTowards(b.Weight[i], i == b.Clip ? 1f : 0f, dt / Fade);
                b.Mixer.SetInputWeight(i, b.Weight[i]);
                if (!Loops[i]) continue;
                var input = b.Mixer.GetInput(i);
                double length = b.Clips[i].length;
                if (length > 1e-3 && input.GetTime() > length) input.SetTime(input.GetTime() % length);
            }
            b.Graph.Evaluate(dt);
            Overlays(b);
            b.Root.SetPositionAndRotation(b.Position + Vector3.up * b.Lift, Quaternion.Euler(0f, b.Yaw, 0f));
            if (b.Yoke != null) Carry(b);
        }

        /// <summary>The procedural beats over the clip pose. The cheer is LagoonResident's arm, exactly.</summary>
        private void Overlays(Body b)
        {
            b.Lift = 0f;
            if (b.ArmL != null && _clock < b.CheerUntil)
            {
                float age = 1.4f - (b.CheerUntil - _clock), env = Mathf.Sin(Mathf.Clamp01(age / 1.4f) * Mathf.PI);
                b.ArmL.localRotation = b.ArmLRest * Quaternion.Euler(-80f * env, Mathf.Sin(age * 13f) * 12f * env, -20f * env);
            }
            if (b.Torso != null && _clock < b.BowUntil)
            {
                float env = Mathf.Sin(Mathf.Clamp01(1f - (b.BowUntil - _clock) / 1.4f) * Mathf.PI);
                b.Torso.localRotation *= Quaternion.Euler(-24f * env, 0f, 0f);
            }
            if (b.Head != null && _clock < b.ShakeUntil)
            {
                float age = 1.2f - (b.ShakeUntil - _clock), env = Mathf.Sin(Mathf.Clamp01(age / 1.2f) * Mathf.PI);
                b.Head.localRotation *= Quaternion.Euler(0f, Mathf.Sin(age * 14f) * 18f * env, 0f);
            }
            if (_clock < b.LaughUntil)
            {
                float age = 1.2f - (b.LaughUntil - _clock), env = Mathf.Sin(Mathf.Clamp01(age / 1.2f) * Mathf.PI);
                b.Lift = Mathf.Abs(Mathf.Sin(age * 9.4f)) * .07f * b.Scale * env;
                if (b.Torso != null) b.Torso.localRotation *= Quaternion.Euler(0f, 0f, Mathf.Sin(age * 22f) * 7f * env);
                if (b.Head != null) b.Head.localRotation *= Quaternion.Euler(10f * env, 0f, 0f);
            }
            if (b.Head != null && _clock < b.CallUntil)
            {
                float env = Mathf.Sin(Mathf.Clamp01(1f - (b.CallUntil - _clock) / 1.6f) * Mathf.PI);
                b.Head.localRotation *= Quaternion.Euler(12f * env, 0f, 0f);
            }
        }

        private void Walking(Body b, float speed, bool run)
        {
            b.MotionSpeed = speed;
            b.Walking = speed > .05f;
            if (speed < .05f) { Play(b, Idle, 1f); return; }
            int clip = run ? SprintClip : WalkClip;
            var c = b.Clips[clip];
            float cycle = (run ? RunCycleMetres : WalkCycleMetres) * b.Scale;
            float rate = c != null && c.length > 1e-3f ? speed / (cycle / c.length) : 1f;
            Play(b, clip, Mathf.Clamp(rate, .4f, 2.2f));
        }

        private void Face(Body b, Vector3 direction, float dt, float turn = 360f)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 1e-6f) return;
            float target = Quaternion.LookRotation(direction.normalized, Vector3.up).eulerAngles.y;
            b.Yaw = Mathf.MoveTowardsAngle(b.Yaw, target, turn * dt);
        }

        // ------------------------------------------------------------------ props

        private readonly Dictionary<string, Mesh> _meshes = new Dictionary<string, Mesh>();

        /// <summary>A private copy of a built-in primitive: ToonSkin's outline weld writes the
        /// mesh's tangents, and the built-in asset is shared with everything else in the game.</summary>
        private Mesh Builtin(string name)
        {
            if (_meshes.TryGetValue(name, out var mesh) && mesh != null) return mesh;
            var source = Resources.GetBuiltinResource<Mesh>(name);
            mesh = source != null ? Instantiate(source) : null;
            if (mesh != null) mesh.name = "Sidewalk " + name;
            return _meshes[name] = mesh;
        }

        private static Transform Part(Transform parent, string name, Mesh mesh, Material material, Vector3 at, Vector3 scale, Vector3 euler)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = at; t.localScale = scale; t.localRotation = Quaternion.Euler(euler);
            if (mesh == null || material == null) return t;
            t.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = t.gameObject.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            ToonSkin.Apply(r, ToonSkin.PersonOutlineWidth);
            return t;
        }

        /// <summary>The pingga (the bamboo pole) on his right shoulder, a bucket hung at each end.
        /// Built at the root, not a bone, so its pose is independent of the clip.</summary>
        private void BuildYoke(Body b)
        {
            var cylinder = Builtin("Cylinder.fbx");
            // Under the head's overhang (the rig's head starts about 0.63 m up), outboard of the torso.
            var yoke = Part(b.Root, "Yoke", null, null, new Vector3(.40f, YokeHeight, 0f), Vector3.one, Vector3.zero);
            Part(yoke, "Pole", cylinder, Bamboo, Vector3.zero, new Vector3(.05f, .78f, .05f), new Vector3(90f, 0f, 0f));
            var swing = new List<Transform>();
            foreach (float end in new[] { -.66f, .66f })
            {
                var pivot = Part(yoke, end < 0 ? "Back" : "Front", null, null, new Vector3(0f, 0f, end), Vector3.one, Vector3.zero);
                Part(pivot, "Rope", cylinder, Rope, new Vector3(0f, -.05f, 0f), new Vector3(.012f, .05f, .012f), Vector3.zero);
                Part(pivot, "Lid", cylinder, Lid, new Vector3(0f, -.105f, 0f), new Vector3(.32f, .012f, .32f), Vector3.zero);
                Part(pivot, "Bucket", cylinder, Aluminium, new Vector3(0f, -.25f, 0f), new Vector3(.30f, .14f, .30f), Vector3.zero);
                swing.Add(pivot);
            }
            b.Yoke = yoke;
            b.Swing = swing.ToArray();
        }

        private void Carry(Body b)
        {
            float walking = Mathf.Clamp01(b.MotionSpeed / .5f);
            var p = b.Yoke.localPosition;
            p.y = YokeHeight + Mathf.Sin(_clock * 8.7f) * .012f * walking;
            b.Yoke.localPosition = p;
            for (int i = 0; i < b.Swing.Length; i++)
                b.Swing[i].localRotation = Quaternion.Euler(Mathf.Sin(_clock * 4.4f + i * 1.3f) * 5f * walking, 0f, 0f);
        }

        private void BuildBeggarProps()
        {
            var f = BeggarFacing; f.y = 0f; f = f.sqrMagnitude > 1e-4f ? f.normalized : Vector3.right;
            var holder = new GameObject("Beggar's things").transform;
            holder.SetParent(transform, false);
            holder.SetPositionAndRotation(BeggarSeat, Quaternion.LookRotation(f, Vector3.up));
            var cube = Builtin("Cube.fbx"); var cylinder = Builtin("Cylinder.fbx");
            _carton = Part(holder, "Carton", cube, Cardboard, new Vector3(0f, .008f, .12f), new Vector3(.62f, .016f, .72f), Vector3.zero);
            // The cup on his left, the side toward the court and the players at the wall.
            _cup = Part(holder, "Tin cup", cylinder, Tin, new Vector3(-.22f, .05f, .55f), new Vector3(.11f, .05f, .11f), Vector3.zero);
            _coin = Part(transform, "Coin", cylinder, Coin, Vector3.zero, new Vector3(.05f, .004f, .05f), Vector3.zero);
            _carton.gameObject.SetActive(false); _cup.gameObject.SetActive(false); _coin.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ the step

        private void Step(float dt)
        {
            if (!_begun || dt <= 0f) return;
            dt = Mathf.Min(dt, .1f);
            _clock += dt;
            if (_taho != null) StepTaho(_taho, dt);
            if (_beggar != null) StepBeggar(_beggar, dt);
            foreach (var s in _spectators) StepSpectator(s, dt);
            StepKids(dt);
            StepCoin(dt);
            foreach (var b in _all) Pose(b, dt);
        }

        private float Range(Vector2 r) => r.x + (float)_rng.NextDouble() * Mathf.Max(0f, r.y - r.x);

        /// <summary>A far end may take or lose a person only while it is off the main camera's screen.</summary>
        private static bool Seen(Vector3 p)
        {
            if (!Application.isPlaying) return false;
            var camera = Camera.main;
            if (camera == null) return false;
            var v = camera.WorldToViewportPoint(p + Vector3.up);
            return v.z > 0f && v.z < 90f && v.x > -.08f && v.x < 1.08f && v.y > -.08f && v.y < 1.08f;
        }

        /// <summary>Moves a walker along its route, keeping right of its way (0 at the route's
        /// stopping end) and waiting behind anybody standing in its way.</summary>
        private void Advance(Walker w, float speed, float dt)
        {
            var line = _lines[w.Line];
            var b = w.Body;
            float ease = Mathf.Clamp01((line.Length - w.Along) / 1.5f);
            w.Lateral = KeepRight * w.Dir * ease * line.WidthAt(w.Along);
            var here = line.At(w.Along, out var tangent);
            var heading = tangent * w.Dir;
            b.Heading = heading;
            if (Blocked(b, b.Position, heading)) speed = 0f;
            w.Along = Mathf.Clamp(w.Along + speed * w.Dir * dt, 0f, line.Length);
            here = line.At(w.Along, out tangent);
            b.Position = here + Vector3.Cross(Vector3.up, tangent) * w.Lateral;
            Walking(b, speed, false);
            // On its way even while it waits, so two walkers meeting head-on never wait on each other.
            b.Walking = true;
            Face(b, tangent * w.Dir, dt);
        }

        private bool Blocked(Body self, Vector3 at, Vector3 heading)
        {
            foreach (var other in _all)
            {
                if (other == self || !other.Shown) continue;
                // Somebody coming the other way keeps right and passes; only the standing and the
                // slower ahead in the same direction are waited for (no two ever wait on each other).
                if (other.Walking && Vector3.Dot(other.Heading, heading) < -.3f) continue;
                var d = other.Position - at; d.y = 0f;
                float ahead = Vector3.Dot(d, heading);
                if (ahead <= .05f || ahead > 1.1f) continue;
                if ((d - heading * ahead).magnitude < .55f) return true;
            }
            return false;
        }

        private void Appear(Walker w)
        {
            var line = _lines[w.Line];
            w.Along = 0f; w.Dir = 1;
            w.Body.Position = line.At(0f, out var tangent) + Vector3.Cross(Vector3.up, tangent) * KeepRight * line.WidthAt(0f);
            w.Body.Yaw = Quaternion.LookRotation(tangent, Vector3.up).eulerAngles.y;
            Show(w.Body, true);
        }

        /// <summary>At the far end on the way out: vanish once unseen (or after 20 s regardless).</summary>
        private bool Gone(Walker w, float dt)
        {
            w.Timer += dt;
            Walking(w.Body, 0f, false);
            if (Seen(w.Body.Position) && w.Timer < 20f) return false;
            Show(w.Body, false);
            return true;
        }

        // ------------------------------------------------------------------ the magtataho

        private void StepTaho(Walker w, float dt)
        {
            var b = w.Body;
            switch (w.Phase)
            {
                case Hidden:
                    w.Timer -= dt;
                    if (w.Timer > 0f || Seen(_lines[w.Line].P[0])) return;
                    Appear(w); w.Phase = Going; w.NextStop = Range(TahoStopEvery); w.Timer = 0f;
                    b.State = "walking in";
                    return;
                case Going:
                case Leaving:
                    if (w.Timer > 0f)
                    {
                        w.Timer -= dt; Walking(b, 0f, false); b.State = "calling";
                        return;
                    }
                    float before = w.Along;
                    Advance(w, TahoSpeed, dt);
                    w.NextStop -= Mathf.Abs(w.Along - before);
                    b.State = w.Phase == Going ? "walking in" : "walking out";
                    var line = _lines[w.Line];
                    if (w.Phase == Going && w.Along >= line.Length - .01f)
                    {
                        w.Phase = Arrived; w.Timer = Range(TahoStop) + 4f; Call(b);
                    }
                    else if (w.Phase == Leaving && w.Along <= .01f) { w.Phase = Vanishing; w.Timer = 0f; }
                    else if (w.NextStop <= 0f && w.Along > 3f)
                    {
                        w.NextStop = Range(TahoStopEvery); w.Timer = Range(TahoStop); Call(b);
                    }
                    return;
                case Arrived:
                    w.Timer -= dt; Walking(b, 0f, false); b.State = "calling";
                    if (w.Timer <= 0f) { w.Phase = Leaving; w.Dir = -1; w.NextStop = Range(TahoStopEvery); }
                    return;
                case Vanishing:
                    if (Gone(w, dt)) { w.Phase = Hidden; w.Timer = Range(TahoAway); }
                    return;
            }
        }

        private void Call(Body b)
        {
            b.CallUntil = _clock + 1.6f;
            if (!Application.isPlaying) return;
            var camera = Camera.main;
            if (camera != null && (camera.transform.position - b.Position).sqrMagnitude > 38f * 38f) return;
            ComicPopup.Spawn(b.Position + Vector3.up * 2.2f, "TAHOOO!", UI.UiTheme.Highlight, .8f, ComicPopup.Weight.Flavour);
        }

        // ------------------------------------------------------------------ spectators

        private void StepSpectator(Walker w, float dt)
        {
            var b = w.Body;
            switch (w.Phase)
            {
                case Hidden:
                    w.Timer -= dt;
                    if (w.Timer > 0f) return;
                    int watch = FreeWatch();
                    if (watch < 0 || Seen(_lines[Watches[watch].Walk].P[0])) { w.Timer = 1f; return; }
                    w.Watch = watch; w.Line = Watches[watch].Walk;
                    Appear(w); w.Phase = Going; b.State = "walking to " + Watches[watch].Name;
                    return;
                case Going:
                    Advance(w, WalkSpeed, dt);
                    if (w.Along >= _lines[w.Line].Length - .01f) { w.Phase = Arrived; w.Timer = Range(SpectatorWatch); }
                    return;
                case Arrived:
                    w.Timer -= dt; b.State = "watching from " + Watches[w.Watch].Name;
                    Walking(b, 0f, false);
                    Face(b, Watches[w.Watch].LookAt - b.Position, dt, 200f);
                    if (w.Timer <= 0f) { w.Phase = Leaving; w.Dir = -1; b.State = "walking on"; }
                    return;
                case Leaving:
                    Advance(w, WalkSpeed, dt);
                    if (w.Along <= .01f) { w.Phase = Vanishing; w.Timer = 0f; }
                    return;
                case Vanishing:
                    if (Gone(w, dt)) { w.Phase = Hidden; w.Timer = Range(SpectatorAway); w.Watch = -1; }
                    return;
            }
        }

        private int FreeWatch()
        {
            var free = new List<int>();
            for (int i = 0; i < Watches.Length; i++)
            {
                if (!Valid(Watches[i].Walk)) continue;
                bool taken = false;
                foreach (var s in _spectators) if (s.Watch == i || (s.Line == Watches[i].Walk && s.Phase != Hidden)) taken = true;
                if (!taken) free.Add(i);
            }
            return free.Count == 0 ? -1 : free[_rng.Next(free.Count)];
        }

        private void OnFlair(MatchFlair.Kind kind, int actor, int subject, Vector3 at, float strength)
        {
            if (kind != MatchFlair.Kind.LataDown && kind != MatchFlair.Kind.Tag) return;
            foreach (var s in _spectators)
            {
                if (s.Phase != Arrived) continue;
                var b = s.Body;
                if (Vector3.Distance(b.Position, at) > 45f) continue;
                float lag = (float)_rng.NextDouble() * .25f;
                if (kind == MatchFlair.Kind.LataDown) { b.CheerUntil = _clock + 1.4f + lag; b.LaughUntil = _clock + 1.2f + lag; }
                else b.ShakeUntil = _clock + 1.2f + lag;
            }
        }

        // ------------------------------------------------------------------ the beggar

        private void StepBeggar(Walker w, float dt)
        {
            var b = w.Body;
            var facing = BeggarFacing; facing.y = 0f;
            switch (w.Phase)
            {
                case Hidden:
                    w.Timer -= dt;
                    if (w.Timer > 0f || Seen(_lines[w.Line].P[0])) return;
                    Appear(w); w.Phase = Going; b.State = "walking in";
                    return;
                case Going:
                    Advance(w, BeggarSpeed, dt);
                    if (w.Along >= _lines[w.Line].Length - .01f) { w.Phase = Settling; w.Timer = .9f; b.State = "sitting down"; }
                    return;
                case Settling:
                    w.Timer -= dt;
                    b.Position = Vector3.MoveTowards(b.Position, BeggarSeat, dt * .6f);
                    Face(b, facing, dt, 240f);
                    Play(b, w.Timer < .45f ? SitClip : Idle, 1f);
                    if (w.Timer < .45f) b.Position = Vector3.MoveTowards(b.Position, BeggarSeat + Vector3.up * SitLift, dt * .5f);
                    if (w.Timer <= 0f)
                    {
                        b.Position = BeggarSeat + Vector3.up * SitLift;
                        w.Phase = Seated; w.Timer = Range(BeggarSits); b.State = "seated";
                        SetProps(true);
                    }
                    return;
                case Seated:
                    w.Timer -= dt;
                    Play(b, SitClip, 1f);
                    Face(b, facing, dt, 240f);
                    if (w.Timer <= 0f && _coinT < 0f)
                    {
                        w.Phase = Rising; w.Timer = 1.1f; b.State = "packing up";
                        Play(b, PickUpClip, .6f);
                    }
                    return;
                case Rising:
                    w.Timer -= dt;
                    if (w.Timer < .55f) { SetProps(false); Play(b, Idle, 1f); b.Position = BeggarSeat; }
                    if (w.Timer <= 0f)
                    {
                        var line = _lines[w.Line];
                        w.Along = line.Length; w.Dir = -1; w.Phase = Leaving; b.State = "walking out";
                    }
                    return;
                case Leaving:
                    Advance(w, BeggarSpeed, dt);
                    if (w.Along <= .01f) { w.Phase = Vanishing; w.Timer = 0f; }
                    return;
                case Vanishing:
                    if (Gone(w, dt)) { w.Phase = Hidden; w.Timer = Range(BeggarAway); }
                    return;
            }
        }

        private void SetProps(bool shown)
        {
            if (_carton != null) _carton.gameObject.SetActive(shown);
            if (_cup != null) _cup.gameObject.SetActive(shown);
        }

        /// <summary>A coin for the beggar, from `from` (the giver's hand). Cosmetic: a thank-you and
        /// nothing else. False while he is not seated or the last coin is still landing.</summary>
        public bool Donate(Vector3 from)
        {
            if (!BeggarSeated || _clock < _donateReady || _coinT >= 0f) return false;
            _donateReady = _clock + 1.3f;
            Donations++;
            var b = _beggar.Body;
            b.BowUntil = _clock + 1.4f;
            b.CheerUntil = _clock + 1.4f;
            _beggar.Timer = Mathf.Max(_beggar.Timer, 6f);
            _coinFrom = from; _coinT = 0f;
            if (_coin != null) { _coin.position = from; _coin.gameObject.SetActive(true); }
            if (Application.isPlaying)
                ComicPopup.Spawn(b.Position + Vector3.up * 1.7f, Thanks[_rng.Next(Thanks.Length)], UI.UiTheme.Highlight, .9f, ComicPopup.Weight.Flavour);
            return true;
        }

        private void StepCoin(float dt)
        {
            if (_coinT < 0f || _coin == null) return;
            _coinT += dt / .45f;
            var to = CupPosition + Vector3.up * .06f;
            _coin.position = Vector3.Lerp(_coinFrom, to, Mathf.Clamp01(_coinT)) + Vector3.up * Mathf.Sin(Mathf.Clamp01(_coinT) * Mathf.PI) * .45f;
            _coin.rotation = Quaternion.Euler(_coinT * 720f, 0f, 0f);
            if (_coinT < 1f) return;
            _coinT = -1f;
            _coin.gameObject.SetActive(false);
            if (Application.isPlaying) GameServices.Audio?.PlayAtVaried(CoinCue, to, 1.25f, 1.45f, .8f);
        }

        // ------------------------------------------------------------------ the kids

        private void StepKids(float dt)
        {
            if (_kids.Count == 0) return;
            float end = _track.Length;
            switch (_kidsPhase)
            {
                case Hidden:
                    _kidsTimer -= dt;
                    if (_kidsTimer > 0f || Seen(_track.P[_track.P.Length - 1])) return;
                    for (int i = 0; i < _kids.Count; i++)
                    {
                        var k = _kids[i];
                        k.Along = end - .4f - i * .9f; k.Lateral = k.LateralTarget = (i - (_kids.Count - 1) * .5f) * .5f;
                        k.It = i == 0; k.Home = false; k.Dir = -1f; k.Burst = 0f; k.NextDart = _clock + 1f + i;
                        k.Pause = k.It ? 1.6f : 0f;
                        KidPlace(k, dt, true);
                        Show(k.Body, true);
                    }
                    _kidsPhase = Going; _kidsTimer = Range(KidsPlay);
                    return;
                case Going:
                    _kidsTimer -= dt;
                    foreach (var k in _kids) Chase(k, dt, end);
                    Separate();
                    if (_kidsTimer <= 0f) { _kidsPhase = Leaving; foreach (var k in _kids) { k.Home = true; k.Pause = 0f; } }
                    return;
                case Leaving:
                    bool anyone = false;
                    foreach (var k in _kids)
                    {
                        if (!k.Body.Shown) continue;
                        anyone = true;
                        if (k.Along < end - .05f)
                        {
                            k.Along = Mathf.Min(end, k.Along + KidRun * .8f * dt);
                            k.LateralTarget = 0f;
                            KidPlace(k, dt, false);
                            Walking(k.Body, KidRun * .8f, true);
                            k.Body.State = "running home";
                        }
                        else
                        {
                            Walking(k.Body, 0f, false);
                            if (!Seen(k.Body.Position) || _kidsTimer < -20f) Show(k.Body, false);
                        }
                    }
                    _kidsTimer -= dt;
                    if (!anyone) { _kidsPhase = Hidden; _kidsTimer = Range(KidsAway); }
                    return;
            }
        }

        /// <summary>One kid's step of tag along the track: the taya runs down the nearest, the
        /// others run away, juke past at the ends, dart sideways, stop to laugh; a tag swaps.</summary>
        private void Chase(Kid k, float dt, float end)
        {
            var b = k.Body;
            if (k.Pause > 0f)
            {
                k.Pause -= dt;
                Play(b, Idle, 1f);
                b.MotionSpeed = 0f;
                b.State = k.It ? "counting" : "laughing";
                KidPlace(k, dt, false);
                return;
            }
            float speed;
            if (k.It)
            {
                Kid target = null; float best = float.MaxValue;
                foreach (var o in _kids) if (o != k && Mathf.Abs(o.Along - k.Along) < best) { best = Mathf.Abs(o.Along - k.Along); target = o; }
                if (target == null) return;
                k.Dir = Mathf.Sign(target.Along - k.Along + 1e-4f);
                k.LateralTarget = target.Lateral;
                speed = KidRun * 1.08f;
                b.State = "it, chasing";
                if (best < .55f && Mathf.Abs(target.Lateral - k.Lateral) < .5f && target.Pause <= 0f)
                {
                    // Tagged: both laugh, the tagged kid counts, the old taya runs off.
                    target.It = true; k.It = false;
                    target.Pause = 1.4f; k.Pause = .5f;
                    target.Body.LaughUntil = _clock + 1.2f; b.LaughUntil = _clock + 1.2f;
                    k.Dir = k.Along < end * .5f ? 1f : -1f;
                    k.LateralTarget = (target.Lateral >= 0f ? -1f : 1f) * KidHalfWidth;
                    k.Burst = .8f;
                    return;
                }
            }
            else
            {
                Kid it = null;
                foreach (var o in _kids) if (o.It) it = o;
                float gap = it != null ? k.Along - it.Along : 99f;
                speed = KidRun;
                if (Mathf.Abs(gap) > 5f)
                {
                    speed = KidRun * .45f;
                    if (_rng.NextDouble() < dt * .25f) { k.Pause = .8f + (float)_rng.NextDouble() * .8f; b.LaughUntil = _clock + 1.2f; }
                }
                else k.Dir = Mathf.Abs(gap) < 1e-3f ? (_rng.NextDouble() < .5 ? -1f : 1f) : Mathf.Sign(gap);
                bool cornered = (k.Along < .8f && k.Dir < 0f) || (k.Along > end - .8f && k.Dir > 0f);
                if (cornered && it != null && Mathf.Abs(gap) < 3f)
                {
                    k.Dir = -k.Dir; k.Burst = 1f;
                    k.LateralTarget = (it.Lateral >= 0f ? -1f : 1f) * KidHalfWidth;
                }
                else if (cornered) k.Dir = -k.Dir;
                // Two runners never share a spot: the one behind swerves to the other side.
                foreach (var o in _kids)
                    if (o != k && !o.It && Mathf.Abs(o.Along - k.Along) < .9f && Mathf.Abs(o.Lateral - k.Lateral) < .5f)
                        k.LateralTarget = (k.Lateral >= o.Lateral ? 1f : -1f) * KidHalfWidth;
                if (_clock >= k.NextDart)
                {
                    k.NextDart = _clock + 1.5f + (float)_rng.NextDouble() * 3f;
                    k.LateralTarget = ((float)_rng.NextDouble() * 2f - 1f) * KidHalfWidth;
                    if (_rng.NextDouble() < .4) k.Burst = .6f;
                }
                b.State = "running";
            }
            speed *= k.Pace;
            if (k.Burst > 0f) { k.Burst -= dt; speed *= 1.3f; }
            k.Along = Mathf.Clamp(k.Along + k.Dir * speed * dt, 0f, end);
            KidPlace(k, dt, false);
            Walking(b, speed, true);
        }

        /// <summary>No two kids stand in one spot: any pair closer than 0.45 m is pushed apart
        /// across the pavement (the tag itself happens at 0.55 m, before this can stop it).</summary>
        private void Separate()
        {
            for (int i = 0; i < _kids.Count; i++)
                for (int j = i + 1; j < _kids.Count; j++)
                {
                    var a = _kids[i]; var b = _kids[j];
                    var d = a.Body.Position - b.Body.Position; d.y = 0f;
                    float gap = d.magnitude;
                    if (gap >= .45f) continue;
                    float push = (.45f - gap) * .5f + .01f;
                    float side = a.Lateral >= b.Lateral ? 1f : -1f;
                    if (Mathf.Abs(a.Lateral - b.Lateral) < 1e-3f) side = i % 2 == 0 ? 1f : -1f;
                    a.Lateral = Mathf.Clamp(a.Lateral + side * push, -KidHalfWidth, KidHalfWidth);
                    b.Lateral = Mathf.Clamp(b.Lateral - side * push, -KidHalfWidth, KidHalfWidth);
                    // Squeezed against the edge: the other one gives way along the pavement instead.
                    if (Mathf.Abs(a.Lateral - b.Lateral) < .4f) b.Along = Mathf.Clamp(b.Along + (b.Along >= a.Along ? push : -push), 0f, _track.Length);
                    KidPlace(a, 0f, false); KidPlace(b, 0f, false);
                }
        }

        private void KidPlace(Kid k, float dt, bool snap)
        {
            k.LateralTarget = Mathf.Clamp(k.LateralTarget, -KidHalfWidth, KidHalfWidth);
            float before = k.Lateral;
            if (snap) k.Lateral = k.LateralTarget;
            else if (dt > 0f) k.Lateral = Mathf.MoveTowards(k.Lateral, k.LateralTarget, 1.6f * dt);
            var p = _track.At(k.Along, out var tangent);
            var side = Vector3.Cross(Vector3.up, tangent);
            k.Body.Position = p + side * k.Lateral;
            if (snap) { k.Body.Yaw = Quaternion.LookRotation(tangent * k.Dir, Vector3.up).eulerAngles.y; return; }
            if (k.Pause > 0f || dt <= 0f) return;
            Face(k.Body, tangent * k.Dir * 2.5f + side * (k.Lateral - before) / dt, dt, 540f);
        }

        // ------------------------------------------------------------------ the interaction (Play only)

        private void Interact()
        {
            if (_beggar == null) return;
            if (Time.unscaledTime >= _localScan) { _localScan = Time.unscaledTime + .5f; _local = FindLocal(); }
            var local = _local;
            bool held = local != null && local.Intent.Pressed(Verb.Interact);
            if (local != null && CanDonate(local))
            {
                StreetInteractions.Offer(local, DonateAction);
                if (held && !_interactHeld) Donate(local.transform.position + Vector3.up * 1.1f);
            }
            _interactHeld = held;
        }

        private static CharacterMotor FindLocal()
        {
            var round = GameServices.Round;
            if (round == null || round.Players == null) return null;
            int seat = NetAuthority.IsNetworked ? NetAuthority.LocalSlot : GameLaunch.SoloSeat;
            foreach (var p in round.Players) if (p != null && !p.IsBot && p.PlayerSlot == seat) return p;
            return null;
        }

        private bool CanDonate(CharacterMotor local)
        {
            if (!BeggarSeated || local.IsStunned || PresentationClock.BlocksInput) return false;
            var d = local.transform.position - BeggarSeat; d.y = 0f;
            if (d.sqrMagnitude > BeggarReach * BeggarReach) return false;
            // The one Use key also picks up a tsinelas: a shoe in reach is always the press's job.
            if (Time.unscaledTime >= _slipperScan)
            {
                _slipperScan = Time.unscaledTime + .5f;
                _slippers = FindObjectsByType<Slipper>(FindObjectsInactive.Exclude);
            }
            foreach (var s in _slippers) if (s != null && s.CanBeGrabbedBy(local)) return false;
            return true;
        }
    }
}
