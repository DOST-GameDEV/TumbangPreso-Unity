using System;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// The Arena's slipper balloon: the giant inflatable mascot moored over the south canopy
    /// (the holo kit, tools/author_arena_holo.py, `balloon`). The owner, 2026-10-05: "the
    /// balloon should be animated btw, and make it an easter egg when you try to throw a slipper
    /// at full range directed towards it you can actually hit it, and itll react like the
    /// balloon cow in overwatch. do it enough times and itll pop", and of the slipper: "the
    /// slipper will just spawn/tp back to the nearest edge to be able to retrieve it".
    ///
    /// HOW IT MOVES. The kit delivers it as RIGID PARTS, each with its pivot where it turns:
    /// the body (its own axes: x across, y heel to toe), two arms, two legs, two scarf tails,
    /// five ropes (pivot at the anchor on the canopy, y along the rope), a heap of skin for
    /// when it is popped. `ArenaHoloAuthor` hands them to this component; on Awake they are
    /// hung under one rig at the winch:
    ///   * the RIG leans about the winch and rides along its tether (the sway and the bob);
    ///   * the BODY squashes and stretches along its own length, keeping its volume, and
    ///     swells as it is strained;
    ///   * each LOBE turns about the middle of the round end that sits inside the body, so
    ///     a join cannot open however far it swings;
    ///   * each ROPE is aimed from its anchor at its ring on the body and stretched to reach,
    ///     every frame, so a rope is always fast at both ends.
    /// All of it is closed-form waves of the clock every peer shares (`ArenaFx.SharedClock`)
    /// plus a few damped springs that a hit, a knocked can or the crowd kicks. No vertex
    /// shader: the map's shaders cannot be compiled outside the owner's editor, and a rig of
    /// transforms costs a dozen matrix writes a frame.
    ///
    /// THE EASTER EGG, AS A RULE (<see cref="HostOfferThrow"/>). The balloon is 212 m from the
    /// stage and a slipper flies 17 m, so the hit is decided, honestly, at the THROW, by the
    /// host: a human seat, a plain slipper, at <see cref="MinCharge"/> of full charge or more,
    /// whose sight line is within <see cref="ConeDegrees"/> of the middle of the balloon and
    /// at least <see cref="MinElevationDegrees"/> above level, aimed at nothing inside the
    /// play area (the aim point is at or past where that line leaves the walls or the
    /// ceiling). Such a throw is not flown as a throw: the slipper leaves play, is DRAWN
    /// flying up to the balloon (<see cref="FlightSeconds"/>), strikes it, and is set back
    /// loose on the stage by `ArenaFallRecovery` at the nearest standable point to where its
    /// line left the play area. Nothing about score, the can, tags or a rule is touched: a
    /// thrown slipper that hits the balloon is a missed throw that came back.
    ///
    /// WHY ORDINARY PLAY CANNOT TRIGGER IT. From anywhere on the stage the middle of the
    /// balloon is 32 to 41 degrees above level to the south-south-west, so the cone starts 22
    /// degrees up. A throw at the can, a body or a platform aims at a point INSIDE the play
    /// area, which the third test refuses whatever its angle; a bot is refused by seat; and a
    /// loaded ability throw (fire, zap, frost, a bank, a skim) is refused by its affinity.
    ///
    /// WHAT IS ON THE WIRE: ONE small host message, `ArenaBalloon` (`MatchRpc.ArenaBalloon.cs`,
    /// also in the late-join snapshot): the hits so far, whether it is popped, and the last
    /// event (a hit with its origin, line and launch time; a re-inflation). Every peer draws
    /// the flight, the reaction and the pop from that; a late joiner gets the count and the
    /// popped state and no replay. A client sends nothing. Offline the host half runs alone.
    /// </summary>
    [DefaultExecutionOrder(880)]
    public sealed class ArenaBalloon : MonoBehaviour
    {
        public static ArenaBalloon Instance { get; private set; }

        // ------------------------------------------------------------------ the rule's numbers

        /// <summary>The hit that pops it.</summary>
        public const int PopHits = 5;
        /// <summary>The least charge, 0 to 1, that reaches it: "at full range".</summary>
        public const float MinCharge = 0.9f;
        /// <summary>How far, in degrees, the sight line may be from the middle of the balloon.
        /// Seen from the stage the body is 5 degrees to each side and 8 up and down, the arms 8
        /// to each side: the cone covers the whole of it and a degree round it.</summary>
        public const float ConeDegrees = 10.0f;
        /// <summary>The least the sight line climbs. Every line inside the cone already climbs
        /// 22 degrees or more; this says so on its own line.</summary>
        public const float MinElevationDegrees = 20.0f;
        /// <summary>How much short of the play area's edge the aim point may be, metres.</summary>
        public const float AimSlack = 1.0f;
        /// <summary>The drawn flight from the hand to the balloon.</summary>
        public const float FlightSeconds = 1.1f;
        /// <summary>From the strike until the slipper is back on the stage.</summary>
        public const float ReturnSeconds = 1.4f;
        /// <summary>Popped, it comes back at the next round's start or after this long.</summary>
        public const float ReinflateSeconds = 75.0f;
        /// <summary>The least it stays popped, so the pop is always seen through.</summary>
        public const float MinPoppedSeconds = 6.0f;

        private const float BurstLead = 0.45f, InflateSeconds = 2.4f, LimpSeconds = 0.9f, ScrapDelay = 1.1f;
        private const int MaxFlights = 4;

        // ------------------------------------------------------------------ what the scene builder bakes

        public Transform Body, ArmWave, ArmRest, LegLeft, LegRight, Scrap;
        public Transform[] Tails = Array.Empty<Transform>();
        public Transform[] Ropes = Array.Empty<Transform>();
        /// <summary>The body's renderer and which of its materials is the painted skin.</summary>
        public Renderer Skin;
        public int SkinMaterial;
        /// <summary>The four faces, happy first (tools/author_arena_textures_holo.py, BALLOON_FACES), and their emission maps.</summary>
        public Texture2D[] Faces = Array.Empty<Texture2D>();
        public Texture2D[] FaceGlows = Array.Empty<Texture2D>();
        /// <summary>The middle of the body at rest, in the world: what a throw is aimed at.</summary>
        public Vector3 Centre;

        private enum Face { Happy, Worried, Ouch, Dizzy }

        /// <summary>What travels: the whole of the balloon's shared state and its last event.</summary>
        public struct Wire
        {
            public const byte None = 0, Hit = 1, PoppingHit = 2, Reinflate = 3;
            public byte Hits, Kind;
            public bool Popped;
            public int Serial, Seat;
            public Vector3 Origin, Direction;
            public double Launch;
        }

        private struct Flight
        {
            public Vector3 Origin, Direction;
            public float Age;
            public int Number, Seat;
            public bool Pops, Live;
        }

        private struct Spring
        {
            public float X, V;
            public void Step(float dt, float hertz, float damping)
            {
                float w = hertz * 2.0f * Mathf.PI;
                V += (-w * w * X - 2.0f * damping * w * V) * dt;
                X += V * dt;
            }
        }

        // The host's count (flights in the air included) and what the body is showing.
        private int _hits, _shown, _serial;
        private bool _popped, _popPending, _roundBegan;
        private double _poppedAt;
        private int _round = -1;
        private long _match;
        private Wire _last;

        private readonly Flight[] _flights = new Flight[MaxFlights];
        private readonly float[] _skyUntil = new float[Core.CompanionSeats.BodyCount];

        // The rig.
        private Transform _rig;
        private Transform[] _lobes, _ties;
        private Vector3[] _lobeAt, _ropeDirection, _tieRest;
        private Quaternion[] _lobeTurn, _ropeTurn;
        private Vector3[] _ropeScale;
        private float[] _ropeLength;
        private Vector3 _rigAt, _bodyAt, _bodyScale, _tether;
        private Quaternion _bodyTurn;
        private Renderer[] _skins;
        private MaterialPropertyBlock _block;
        private bool _built;

        // The motion.
        private Spring _leanX, _leanZ, _ride, _squash, _wobble, _arms, _legs, _tails, _size;
        private float _swell, _burst = -1.0f, _inflate = -1.0f, _limp, _scrapIn = -1.0f, _faceLeft, _leak, _creak, _crowdWas, _poppedShown;
        private Face _face = Face.Happy, _faceWorn = (Face)(-1);
        private float _clock;

        private static readonly int MainTexId = Shader.PropertyToID("_MainTex"), EmissionMapId = Shader.PropertyToID("_EmissionMap"),
                                    EmissionStrengthId = Shader.PropertyToID("_EmissionStrength"), ColourId = Shader.PropertyToID("_Color");
        private float _restEmission = 1.0f;

        public int Hits => _hits;
        public bool Popped => _popped || _popPending;
        /// <summary>How many hits the body is showing: the count less the slippers still in the air.</summary>
        public int ShownHits => _shown;
        /// <summary>True while the body is drawn (not popped, or coming back).</summary>
        public bool BodyShown => _skins != null && _skins.Length > 0 && _skins[0] != null && _skins[0].enabled;

        // ------------------------------------------------------------------ lifecycle

        private void Awake()
        {
            Instance = this;
            _block = new MaterialPropertyBlock();
            Build();
        }

        private void OnEnable()
        {
            Instance = this;
            MatchFlair.Presented += OnFlair;
        }

        private void OnDisable()
        {
            MatchFlair.Presented -= OnFlair;
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Hangs the parts under one rig at the winch. The parts arrive as flat siblings, each
        /// where the kit modelled it; from here the body rides the rig, the lobes ride the body,
        /// and a marker on the body stands where each rope is tied.
        /// </summary>
        private void Build()
        {
            if (_built || Body == null || Ropes == null || Ropes.Length == 0 || Ropes[0] == null) return;

            _rigAt = Ropes[0].position;
            _rig = new GameObject("Balloon rig").transform;
            _rig.SetParent(transform, false);
            _rig.SetPositionAndRotation(_rigAt, Quaternion.identity);
            if (Centre == Vector3.zero) Centre = Body.position;
            _tether = (Body.position - _rigAt).normalized;

            Body.SetParent(_rig, true);
            _bodyAt = Body.localPosition; _bodyTurn = Body.localRotation; _bodyScale = Body.localScale;

            _lobes = new[] { ArmWave, ArmRest, LegLeft, LegRight, Tails.Length > 0 ? Tails[0] : null, Tails.Length > 1 ? Tails[1] : null };
            _lobeAt = new Vector3[_lobes.Length];
            _lobeTurn = new Quaternion[_lobes.Length];
            for (int i = 0; i < _lobes.Length; i++)
            {
                if (_lobes[i] == null) continue;
                _lobes[i].SetParent(Body, true);
                _lobeAt[i] = _lobes[i].localPosition;
                _lobeTurn[i] = _lobes[i].localRotation;
            }

            int ropes = Ropes.Length;
            _ties = new Transform[ropes];
            _tieRest = new Vector3[ropes];
            _ropeDirection = new Vector3[ropes];
            _ropeTurn = new Quaternion[ropes];
            _ropeScale = new Vector3[ropes];
            _ropeLength = new float[ropes];
            for (int i = 0; i < ropes; i++)
            {
                var rope = Ropes[i];
                if (rope == null) continue;
                // The rope's mesh runs along its own y from the anchor: its far end is its length.
                var filter = rope.GetComponentInChildren<MeshFilter>();
                float length = 0.0f;
                if (filter != null && filter.sharedMesh != null)
                {
                    // Whatever the importer's node is turned to: the furthest corner of its bounds along the rope.
                    var bounds = filter.sharedMesh.bounds;
                    for (int corner = 0; corner < 8; corner++)
                    {
                        var point = bounds.center + Vector3.Scale(bounds.extents, new Vector3((corner & 1) != 0 ? 1 : -1, (corner & 2) != 0 ? 1 : -1, (corner & 4) != 0 ? 1 : -1));
                        length = Mathf.Max(length, Vector3.Dot(filter.transform.TransformPoint(point) - rope.position, rope.up));
                    }
                }
                _ropeLength[i] = length > 1.0f ? length : 30.0f;
                _ropeDirection[i] = rope.up;
                _ropeTurn[i] = rope.rotation;
                _ropeScale[i] = rope.localScale;
                _ties[i] = new GameObject("Rope tie " + i).transform;
                _ties[i].SetParent(Body, false);
                _ties[i].position = rope.position + rope.up * _ropeLength[i];
                _tieRest[i] = _ties[i].position;
            }

            var skins = new System.Collections.Generic.List<Renderer>();
            skins.AddRange(Body.GetComponentsInChildren<Renderer>(true));
            _skins = skins.ToArray();
            if (Skin == null) Skin = Body.GetComponentInChildren<Renderer>(true);
            if (Skin != null)
            {
                var materials = Skin.sharedMaterials;
                if (SkinMaterial < 0 || SkinMaterial >= materials.Length) SkinMaterial = 0;
                var skin = materials.Length > 0 ? materials[SkinMaterial] : null;
                if (skin != null && skin.HasProperty(EmissionStrengthId)) _restEmission = skin.GetFloat(EmissionStrengthId);
            }

            if (Scrap != null) Scrap.gameObject.SetActive(false);
            _built = true;
        }

        // ------------------------------------------------------------------ the clock

        private static double Now => ArenaFx.SharedClock;

        // ------------------------------------------------------------------ THE RULE (host)

        /// <summary>
        /// Where a sight line from `origin` along `direction` (a unit vector) leaves the play
        /// area: the distance to the nearest of the four walls and the slipper's ceiling. 0 when
        /// the origin is already outside.
        /// </summary>
        public static float PlayExit(Vector3 origin, Vector3 direction)
        {
            float exit = float.PositiveInfinity;
            Axis(origin.x, direction.x, AIController.PlayableMinX, AIController.PlayableMaxX, ref exit);
            Axis(origin.z, direction.z, AIController.PlayableMinZ, AIController.PlayableMaxZ, ref exit);
            Axis(origin.y, direction.y, float.NegativeInfinity, AIController.PlayableCeilingY, ref exit);
            return float.IsPositiveInfinity(exit) ? 0.0f : Mathf.Max(0.0f, exit);
        }

        private static void Axis(float at, float speed, float low, float high, ref float exit)
        {
            if (at <= low || at >= high) { exit = 0.0f; return; }
            if (speed > 1e-5f) exit = Mathf.Min(exit, (high - at) / speed);
            else if (speed < -1e-5f && !float.IsNegativeInfinity(low)) exit = Mathf.Min(exit, (low - at) / speed);
        }

        /// <summary>
        /// The rule and nothing else: would a throw from `origin` aimed at `aimPoint` with this
        /// charge be a throw at the balloon? Pure, so the probe and the host ask the same thing.
        /// </summary>
        public static bool IsAimedAtBalloon(Vector3 centre, Vector3 origin, Vector3 aimPoint, float charge)
        {
            if (charge < MinCharge) return false;
            Vector3 sight = aimPoint - origin;
            float reach = sight.magnitude;
            if (reach < 0.5f || !float.IsFinite(reach)) return false;
            sight /= reach;

            if (Mathf.Asin(Mathf.Clamp(sight.y, -1.0f, 1.0f)) * Mathf.Rad2Deg < MinElevationDegrees) return false;
            if (Vector3.Angle(sight, centre - origin) > ConeDegrees) return false;
            // Aimed at nothing in play: the aim point is where the line leaves the play area, or past it.
            return reach >= PlayExit(origin, sight) - AimSlack;
        }

        /// <summary>
        /// HOST, from `Carrier.HostThrowAt` just after the slipper has been thrown: if this throw
        /// was at the balloon, the slipper is taken out of play, its hit is counted and told to
        /// every peer, and it is booked to come back. True when it was taken. On every other map
        /// there is no instance and this is one null test.
        /// </summary>
        public static bool HostOfferThrow(Slipper thrown, CharacterMotor thrower, Vector3 origin, Vector3 aimPoint, float charge, SlipperAffinity affinity)
        {
            var balloon = Instance;
            if (balloon == null) return false;
            return balloon.HostTake(thrown, thrower, origin, aimPoint, charge, affinity);
        }

        private bool HostTake(Slipper thrown, CharacterMotor thrower, Vector3 origin, Vector3 aimPoint, float charge, SlipperAffinity affinity)
        {
            if (!_built || !isActiveAndEnabled || !NetAuthority.ShouldResolve()) return false;
            if (thrown == null || thrower == null || thrower.IsBot || affinity != SlipperAffinity.Normal) return false;
            if (_popped || _popPending || _inflate >= 0.0f || _hits >= PopHits) return false;
            if (!IsAimedAtBalloon(Centre, origin, aimPoint, charge)) return false;

            var recovery = ArenaFallRecovery.Instance;
            if (recovery == null) return false;

            Vector3 sight = (aimPoint - origin).normalized;
            Vector3 left = origin + sight * PlayExit(origin, sight);
            if (!recovery.HostSendAway(thrown, left, FlightSeconds + ReturnSeconds)) return false;

            _hits++;
            bool pops = _hits >= PopHits;
            if (pops) _popPending = true;
            _last = new Wire
            {
                Hits = (byte)_hits, Kind = pops ? Wire.PoppingHit : Wire.Hit, Popped = pops, Serial = ++_serial,
                Seat = thrown.SeatOfOrigin, Origin = origin, Direction = sight, Launch = Now,
            };
            Launch(_last, 0.0f);
            Net.MatchRpc.Instance?.BroadcastArenaBalloon(_last);
            return true;
        }

        /// <summary>HOST: the state for a peer that has just arrived (no event: it replays nothing).</summary>
        public Wire HostSnapshot()
        {
            var state = _last;
            state.Hits = (byte)_hits; state.Popped = Popped; state.Serial = _serial; state.Kind = Wire.None;
            return state;
        }

        private void HostTick()
        {
            if (!NetAuthority.ShouldResolve()) return;

            // A new match starts with a whole balloon, quietly.
            var match = GameServices.Match;
            long id = match != null ? match.PresentationMatchId : 0L;
            if (id != _match)
            {
                _match = id;
                if (_hits != 0 || Popped)
                {
                    _hits = 0; _popped = false; _popPending = false;
                    _last = new Wire { Serial = ++_serial, Launch = Now };
                    Snap(0, false);
                    Net.MatchRpc.Instance?.BroadcastArenaBalloon(_last);
                }
            }

            int round = match != null ? match.RoundNumber : 0;
            if (round != _round && GameServices.Round != null && GameServices.Round.RoundActive)
            {
                _round = round;
                _roundBegan = true;
            }
            if (!_popped) { _roundBegan = false; return; }

            double gone = Now - _poppedAt;
            if (gone < MinPoppedSeconds || (!_roundBegan && gone < ReinflateSeconds)) return;
            _roundBegan = false;

            _hits = 0; _popped = false;
            _last = new Wire { Kind = Wire.Reinflate, Serial = ++_serial, Launch = Now };
            BeginInflate(0.0f);
            Net.MatchRpc.Instance?.BroadcastArenaBalloon(_last);
        }

        // ------------------------------------------------------------------ what a client is told

        /// <summary>
        /// CLIENT: the host's state. A fresh event is played from where it has got to (its
        /// launch is on the shared clock); anything older, and a late joiner's snapshot, is
        /// shown as it stands with no replay.
        /// </summary>
        public void Apply(in Wire state)
        {
            if (NetAuthority.IsHost || !_built) return;

            int hits = Mathf.Clamp(state.Hits, 0, PopHits);
            if (state.Serial == _serial && state.Kind != Wire.None) return;
            bool fresh = state.Serial != _serial;
            _serial = state.Serial;
            _last = state;
            float age = (float)(Now - state.Launch);

            if (fresh && (state.Kind == Wire.Hit || state.Kind == Wire.PoppingHit) && age > -1.0f && age < FlightSeconds + 2.0f)
            {
                _hits = hits;
                if (state.Kind == Wire.PoppingHit) _popPending = true;
                Launch(state, Mathf.Max(0.0f, age));
                return;
            }

            if (fresh && state.Kind == Wire.Reinflate && age > -1.0f && age < InflateSeconds)
            {
                _hits = 0; _popped = false; _popPending = false;
                BeginInflate(Mathf.Max(0.0f, age));
                return;
            }

            // Not an event this peer saw begin: only put right what differs, so a snapshot sent
            // for somebody else's arrival never cuts a reaction short here.
            if (hits != _hits || state.Popped != Popped) Snap(hits, state.Popped);
        }

        /// <summary>The balloon as it stands, at once: for a late joiner and a new match.</summary>
        private void Snap(int hits, bool popped)
        {
            for (int i = 0; i < _flights.Length; i++) _flights[i].Live = false;
            _hits = hits; _shown = popped ? 0 : hits;
            _popped = popped; _popPending = false;
            _burst = -1.0f; _inflate = -1.0f; _scrapIn = -1.0f; _faceLeft = 0.0f;
            _limp = popped ? 1.0f : 0.0f;
            if (popped) _poppedAt = Now;
            ShowBody(!popped);
            if (Scrap != null) { Scrap.gameObject.SetActive(popped); Scrap.localScale = Vector3.one; }
            _swell = SwellFor(_shown);
            _face = RestingFace();
        }

        // ------------------------------------------------------------------ a hit, from launch to strike

        private void Launch(in Wire state, float age)
        {
            int slot = -1;
            for (int i = 0; i < _flights.Length && slot < 0; i++) if (!_flights[i].Live) slot = i;
            if (slot < 0) slot = 0;
            _flights[slot] = new Flight
            {
                Origin = state.Origin, Direction = state.Direction.sqrMagnitude > 0.5f ? state.Direction.normalized : (Centre - state.Origin).normalized,
                Age = age, Number = Mathf.Clamp(state.Hits, 1, PopHits), Seat = state.Seat, Pops = state.Kind == Wire.PoppingHit, Live = true,
            };
            if (age < 0.25f) ArenaFx.Cue("sfx_arena_balloon_fly", state.Origin, 0.97f, 1.05f);
            if (state.Seat >= 0 && state.Seat < _skyUntil.Length) _skyUntil[state.Seat] = _clock + FlightSeconds + ReturnSeconds + 2.0f - age;
        }

        /// <summary>True while that seat's slipper is on its way back from the balloon:
        /// `ArenaFallRecovery` then draws its return as a fall from the sky.</summary>
        public static bool ReturningFromSky(int seatOfOrigin)
        {
            var balloon = Instance;
            return balloon != null && seatOfOrigin >= 0 && seatOfOrigin < balloon._skyUntil.Length && balloon._clock < balloon._skyUntil[seatOfOrigin];
        }

        /// <summary>Where on the balloon a slipper thrown from `origin` lands: on its skin, on the side it came from.</summary>
        private Vector3 ImpactPoint(Vector3 origin)
        {
            Vector3 here = Body != null ? Body.position : Centre;
            Vector3 toward = origin - here;
            return here + toward.normalized * 9.0f;
        }

        /// <summary>The drawn path: out of the hand along the sight line, bending up to the balloon.</summary>
        private static Vector3 Path(Vector3 from, Vector3 direction, Vector3 to, float u)
        {
            Vector3 lead = from + direction * ((to - from).magnitude * 0.45f);
            float v = 1.0f - u;
            return from * (v * v) + lead * (2.0f * v * u) + to * (u * u);
        }

        private void Flights(ArenaFx fx, float dt)
        {
            for (int i = 0; i < _flights.Length; i++)
            {
                if (!_flights[i].Live) continue;
                ref var flight = ref _flights[i];
                flight.Age += dt;
                Vector3 target = ImpactPoint(flight.Origin);
                // It leaves fast and arrives slower: a throw, not a rocket.
                float u = Mathf.Clamp01(flight.Age / FlightSeconds);
                float eased = 1.0f - (1.0f - u) * (1.0f - u);
                if (u >= 1.0f)
                {
                    flight.Live = false;
                    Strike(fx, flight, target);
                    continue;
                }

                if (fx == null) continue;
                Vector3 head = Path(flight.Origin, flight.Direction, target, eased);
                Vector3 tail = Path(flight.Origin, flight.Direction, target, Mathf.Max(0.0f, eased - 0.10f));
                // Sized by its distance from the stage so it shrinks on the screen but is never lost.
                float away = (head - flight.Origin).magnitude;
                float width = 0.16f + away * 0.012f;
                fx.DrawBeam(tail, head, width * 0.2f, width, ArenaFx.Gold, 0.9f);
                fx.DrawBillboard(ArenaFx.Cell.Star, head, width * 4.0f * (0.8f + 0.2f * Mathf.Sin(flight.Age * 40.0f)), ArenaFx.White, 0.95f);
                fx.DrawBillboard(ArenaFx.Cell.Dot, head, width * 2.2f, ArenaFx.Gold, 0.8f);
                if (fx.Rand() < dt * 40.0f)
                    fx.Emit(ArenaFx.Cell.Dot, ArenaFx.Mode.Billboard, tail + fx.RandDirection() * width, Vector3.down * 1.5f, ArenaFx.Gold, 0.7f, 0.5f, width * 1.4f, width * 0.3f);
            }
        }

        private static readonly string[] Words = { "BOING!", "SQUEAK!", "ARAY!", "HALA!", "POP!" };
        private static readonly string[] Squeaks = { "sfx_arena_balloon_squeak_a", "sfx_arena_balloon_squeak_b", "sfx_arena_balloon_squeak_c" };

        private void Strike(ArenaFx fx, in Flight flight, Vector3 at)
        {
            if (_popped && !_popPending) return;       // a snapshot overtook it

            int n = flight.Number;
            _shown = Mathf.Max(_shown, n);
            float k = 1.0f + 0.22f * (n - 1);          // each hit is a little bigger

            // It is pushed back along the throw, and rings down from there.
            Vector3 push = at - flight.Origin; push.y = 0.0f;
            push = push.sqrMagnitude > 1e-4f ? push.normalized : Vector3.back;
            _leanX.V += push.x * 26.0f * k;
            _leanZ.V += push.z * 26.0f * k;
            _ride.V += 9.0f * k;
            _squash.V -= 1.5f * k;
            _wobble.V += (flight.Seat % 2 == 0 ? 1.0f : -1.0f) * 70.0f * k;
            _arms.V += 190.0f * k; _legs.V -= 150.0f * k; _tails.V += 260.0f * k;
            _size.V += 0.5f * k;

            bool dizzy = n >= PopHits - 1;
            Wear(dizzy ? Face.Dizzy : Face.Ouch, dizzy ? 2.2f : 1.3f);

            ArenaFx.CueFlat(Squeaks[(n - 1) % Squeaks.Length], 0.92f + 0.06f * n, 0.98f + 0.06f * n, 0.9f);
            ArenaFx.CueFlat("sfx_arena_balloon_boing", 1.06f - 0.04f * n, 1.1f - 0.04f * n, 0.75f + 0.05f * n);
            if (n >= 3) { ArenaFx.CueFlat("sfx_arena_balloon_creak", 0.95f, 1.05f, 0.8f); _creak = 2.5f; }
            ArenaCrowdAudio.Laugh(n);
            ArenaCrowd.Excite(0.45f + 0.1f * n, 1.6f + 0.3f * n);
            if (n >= 3) ArenaCrowd.Wave();

            if (fx != null)
            {
                fx.Flash(at, 6.0f, 26.0f * k, ArenaFx.White, 0.9f, 0.3f);
                fx.Sparks(at, flight.Origin - at, 80.0f, 22 + 5 * n, 14.0f, 38.0f, ArenaFx.Gold, 0.95f, 0.5f, 1.1f, 1.4f, 14.0f);
                fx.Dots(at, flight.Origin - at, 85.0f, 12 + 3 * n, 8.0f, 22.0f, ArenaFx.Magenta, 0.9f, 1.0f, 2.0f, 2.4f, 9.0f, 1.0f);
                fx.Dots(at, flight.Origin - at, 85.0f, 12 + 3 * n, 8.0f, 22.0f, ArenaFx.Cyan, 0.9f, 1.0f, 2.0f, 2.4f, 9.0f, 1.0f);
                for (int i = 0; i < ArenaFx.Count(5 + n); i++)
                    fx.Glint(at + fx.RandDirection() * fx.Rand(4.0f, 16.0f), fx.Rand(5.0f, 11.0f), ArenaFx.White, 0.95f, fx.Rand(0.35f, 0.9f));
            }

            // The word, in the game's own popup, drawn near the eye on the line to the balloon
            // (a popup is half a metre tall: at the balloon it would be a dot).
            var view = ArenaFx.View;
            if (view != null && !flight.Pops)
            {
                Vector3 eye = view.transform.position;
                ComicPopup.Spawn(eye + (at + Vector3.up * 26.0f - eye).normalized * 14.0f, Words[(n - 1) % (Words.Length - 1)],
                                 new Color(0.97f, 0.82f, 0.16f), 2.2f + 0.2f * n, ComicPopup.Weight.Cast);
            }

            if (flight.Pops) { _burst = BurstLead; _size.V += 1.4f; }
        }

        // ------------------------------------------------------------------ the pop, and coming back

        private void Burst(ArenaFx fx)
        {
            _popped = true; _popPending = false;
            _poppedAt = Now; _poppedShown = 0.0f;
            _shown = 0; _swell = 0.0f; _faceLeft = 0.0f;
            Vector3 at = Body != null ? Body.position : Centre;
            ShowBody(false);
            _scrapIn = ScrapDelay;

            ArenaFx.CueFlat("sfx_arena_balloon_pop", 1.0f, 1.0f, 1.2f);
            ArenaCrowdAudio.BalloonPopped();
            ArenaFx.CueFlat("sfx_arena_pyro", 0.9f, 0.96f, 0.7f);
            ArenaCrowd.Excite(1.0f, 5.5f);
            ArenaCrowd.Wave();
            ArenaAmbience.Stinger();

            var view = Camera.main;
            var rig = view != null ? view.GetComponent<CameraSystem.CameraRig>() : null;
            if (rig != null) rig.Shake(1.0f, 0.9f);

            if (fx != null)
            {
                fx.Flash(at, 30.0f, 170.0f, ArenaFx.White, 1.0f, 0.5f);
                fx.Flash(at, 20.0f, 110.0f, ArenaFx.Gold, 0.8f, 0.9f);
                // The skin: big yellow and red rags thrown every way, falling slowly.
                var yellow = new Color(0.95f, 0.85f, 0.10f);
                var red = new Color(0.85f, 0.16f, 0.2f);
                for (int i = 0; i < ArenaFx.Count(70); i++)
                {
                    Vector3 d = fx.RandDirection(0.2f);
                    float size = fx.Rand(3.0f, 8.0f);
                    fx.Emit(ArenaFx.Cell.Disc, ArenaFx.Mode.Billboard, at + d * fx.Rand(4.0f, 24.0f), d * fx.Rand(18.0f, 60.0f), i % 4 == 0 ? red : yellow, 0.95f,
                            fx.Rand(2.4f, 4.6f), size, size * 0.6f, size * fx.Rand(0.35f, 0.9f), size * 0.3f, 9.0f, 1.6f);
                }
                // Small slipper-shaped bits: long ovals, tumbling down.
                for (int i = 0; i < ArenaFx.Count(60); i++)
                {
                    Vector3 d = fx.RandDirection(0.5f);
                    fx.Emit(ArenaFx.Cell.Disc, ArenaFx.Mode.Stretch, at + d * fx.Rand(2.0f, 18.0f), d * fx.Rand(22.0f, 70.0f), i % 3 == 0 ? ArenaFx.White : ArenaFx.Gold, 0.95f,
                            fx.Rand(2.0f, 4.2f), 1.5f, 1.2f, 3.6f, 3.0f, 12.0f, 1.2f);
                }
                fx.Dots(at, Vector3.up, 180.0f, 90, 20.0f, 75.0f, ArenaFx.Magenta, 0.95f, 2.0f, 4.5f, 2.6f, 8.0f, 1.4f);
                fx.Dots(at, Vector3.up, 180.0f, 90, 20.0f, 75.0f, ArenaFx.Cyan, 0.95f, 2.0f, 4.5f, 2.6f, 8.0f, 1.4f);
                fx.Dots(at, Vector3.up, 180.0f, 70, 20.0f, 75.0f, ArenaFx.Lime, 0.95f, 2.0f, 4.5f, 2.6f, 8.0f, 1.4f);
                fx.Sparks(at, Vector3.up, 180.0f, 80, 40.0f, 120.0f, ArenaFx.White, 1.0f, 0.5f, 1.2f, 2.2f, 10.0f, 1.0f);
                for (int i = 0; i < ArenaFx.Count(16); i++)
                    fx.Glint(at + fx.RandDirection() * fx.Rand(8.0f, 46.0f), fx.Rand(8.0f, 20.0f), ArenaFx.White, 0.95f, fx.Rand(0.4f, 1.2f));
                for (int k = 0; k < 3; k++)
                    fx.Firework(at + fx.RandDirection(0.3f) * fx.Rand(30.0f, 60.0f), k == 1 ? ArenaFx.Magenta : ArenaFx.Gold, 2.4f);
            }

            var eyes = ArenaFx.View;
            if (eyes != null)
            {
                Vector3 eye = eyes.transform.position;
                ComicPopup.Spawn(eye + (at - eye).normalized * 14.0f, Words[Words.Length - 1], new Color(1.0f, 0.36f, 0.3f), 3.6f, ComicPopup.Weight.Cast);
            }
        }

        private void BeginInflate(float age)
        {
            _popped = false; _popPending = false;
            _inflate = Mathf.Clamp(age, 0.0f, InflateSeconds);
            _shown = 0; _swell = 0.0f; _burst = -1.0f; _scrapIn = -1.0f;
            _size = default; _squash = default;
            ShowBody(true);
            Wear(Face.Worried, InflateSeconds * 0.7f - age);
            if (age < 0.4f) ArenaFx.CueFlat("sfx_arena_balloon_hiss", 1.0f, 1.0f, 0.9f);
            if (Scrap != null) Scrap.gameObject.SetActive(false);
        }

        private void ShowBody(bool shown)
        {
            if (_skins == null) return;
            foreach (var skin in _skins) if (skin != null) skin.enabled = shown;
        }

        // ------------------------------------------------------------------ faces and strain

        private static float SwellFor(int hits) => 0.032f * Mathf.Clamp(hits, 0, PopHits - 1);

        /// <summary>What it wears between reactions: happy, or worried once it is strained.</summary>
        private Face RestingFace() => _shown >= 3 ? Face.Worried : Face.Happy;

        private void Wear(Face face, float seconds)
        {
            _face = face;
            _faceLeft = Mathf.Max(0.0f, seconds);
        }

        private void DressSkin(float strain)
        {
            if (Skin == null) return;
            int index = (int)_face;
            bool swap = _face != _faceWorn && index < Faces.Length && Faces[index] != null;
            Skin.GetPropertyBlock(_block, SkinMaterial);
            if (swap)
            {
                _block.SetTexture(MainTexId, Faces[index]);
                if (index < FaceGlows.Length && FaceGlows[index] != null) _block.SetTexture(EmissionMapId, FaceGlows[index]);
                _faceWorn = _face;
            }
            // Strained, it flushes and its own light beats: slowly at three hits, fast at four.
            float beat = strain > 0.0f ? 0.5f + 0.5f * Mathf.Sin(_clock * (5.0f + 9.0f * strain)) : 0.0f;
            _block.SetFloat(EmissionStrengthId, _restEmission * (1.0f + strain * (0.25f + 0.45f * beat)));
            _block.SetColor(ColourId, Color.Lerp(Color.white, new Color(1.0f, 0.80f, 0.74f), strain * (0.5f + 0.5f * beat)));
            Skin.SetPropertyBlock(_block, SkinMaterial);
        }

        // ------------------------------------------------------------------ the crowd and the can

        private void OnFlair(MatchFlair.Kind kind, int actor, int subject, Vector3 at, float strength)
        {
            // A knocked can: it bounces on its tethers and throws its arms up with the stands.
            if (kind != MatchFlair.Kind.LataDown || Popped) return;
            _ride.V += 7.0f; _squash.V += 0.9f; _arms.V += 150.0f; _legs.V += 90.0f; _tails.V += 120.0f;
        }

        // ------------------------------------------------------------------ the frame

        /// <summary>A sine of the shared clock, -1 to 1.</summary>
        private static float Wave(double clock, double period, double phase = 0.0) => (float)Math.Sin((clock / period + phase) * (2.0 * Math.PI));

        /// <summary>0 to 1, the same on every peer for the same number.</summary>
        private static float Hash(long n)
        {
            ulong z = (ulong)n * 0x9E3779B97F4A7C15UL;
            z ^= z >> 31; z *= 0xBF58476D1CE4E5B9UL; z ^= z >> 29;
            return (z & 0xFFFFFFUL) / 16777216.0f;
        }

        private void LateUpdate()
        {
            if (!_built) { Build(); if (!_built) return; }

            float dt = ArenaFx.Step;
            _clock += dt;
            var fx = ArenaFx.Instance;
            double clock = Now;

            HostTick();
            Flights(fx, dt);

            if (_burst >= 0.0f)
            {
                _burst -= dt;
                if (_burst < 0.0f) { _burst = -1.0f; Burst(fx); }
            }

            if (_popped)
            {
                _poppedShown += dt;
                _limp = Mathf.MoveTowards(_limp, 1.0f, dt / LimpSeconds);
                if (_scrapIn >= 0.0f)
                {
                    _scrapIn -= dt;
                    if (_scrapIn < 0.0f && Scrap != null) { Scrap.gameObject.SetActive(true); _scrapIn = -1.0f; _poppedShown = 0.0f; }
                }
                // The heap lands with a little wobble of its own.
                if (Scrap != null && Scrap.gameObject.activeSelf)
                {
                    float settle = Mathf.Exp(-_poppedShown * 3.0f) * Mathf.Cos(_poppedShown * 14.0f);
                    Scrap.localScale = new Vector3(1.0f + 0.2f * settle, Mathf.Max(0.05f, 1.0f - 0.5f * settle) * Mathf.Clamp01(_poppedShown * 5.0f + 0.05f), 1.0f + 0.2f * settle);
                }
                PoseRopes();
                return;
            }

            _limp = Mathf.MoveTowards(_limp, 0.0f, dt / (InflateSeconds * 0.6f));

            // The springs.
            _leanX.Step(dt, 0.42f, 0.16f); _leanZ.Step(dt, 0.42f, 0.16f);
            _ride.Step(dt, 0.8f, 0.22f);
            _squash.Step(dt, 1.7f, 0.13f);
            _wobble.Step(dt, 2.3f, 0.12f);
            _arms.Step(dt, 1.3f, 0.16f); _legs.Step(dt, 1.55f, 0.15f); _tails.Step(dt, 1.05f, 0.11f);
            _size.Step(dt, 1.2f, 0.3f);

            // The crowd getting to its feet gives it a small bounce.
            float crowd = ArenaCrowd.Level;
            if (crowd > 0.7f && _crowdWas <= 0.7f) { _ride.V += 4.0f; _arms.V += 80.0f; }
            _crowdWas = crowd;

            if (_faceLeft > 0.0f)
            {
                _faceLeft -= dt;
                if (_faceLeft <= 0.0f) _face = RestingFace();
            }

            // Strain: it swells with each hit, trembles from the third, leaks and creaks.
            float swellTarget = _burst >= 0.0f ? SwellFor(PopHits - 1) + 0.16f * (1.0f - _burst / BurstLead) : SwellFor(_shown);
            _swell = Mathf.MoveTowards(_swell, swellTarget, dt * (_burst >= 0.0f ? 1.0f : 0.12f));
            float strain = Mathf.Clamp01((_shown - 2) / (float)(PopHits - 3));
            if (_burst >= 0.0f) strain = 1.0f;
            float tremble = strain * 0.006f * Mathf.Sin(_clock * 57.0f) * (0.6f + 0.4f * Mathf.Sin(_clock * 7.3f));
            if (strain > 0.0f && fx != null && Body != null)
            {
                _leak -= dt;
                if (_leak <= 0.0f)
                {
                    // A puff of air from the seam under the strap, on the side that faces the stage.
                    _leak = fx.Rand(0.5f, 1.3f) / (0.5f + strain);
                    Vector3 seam = Body.TransformPoint(new Vector3(fx.Rand(-12.0f, 12.0f), 6.0f, -11.5f));
                    fx.Dots(seam, -Body.forward, 25.0f, 5, 6.0f, 14.0f, ArenaFx.White, 0.6f, 0.6f, 1.2f, 2.2f, -2.0f, 1.5f);
                }
                _creak -= dt;
                if (_creak <= 0.0f) { _creak = fx.Rand(3.5f, 7.0f) / (0.5f + strain); ArenaFx.CueFlat("sfx_arena_balloon_creak", 0.9f, 1.1f, 0.35f + 0.25f * strain); }
            }

            // Coming back up: from a rag to its size, overshooting once.
            float grown = 1.0f;
            if (_inflate >= 0.0f)
            {
                _inflate += dt;
                float u = Mathf.Clamp01(_inflate / InflateSeconds);
                grown = Mathf.Lerp(0.06f, 1.0f, u * u * (3.0f - 2.0f * u)) + 0.10f * Mathf.Sin(u * Mathf.PI * 5.0f) * (1.0f - u) * u * 4.0f;
                if (u >= 1.0f) { _inflate = -1.0f; _size.V += 0.35f; _ride.V += 5.0f; _arms.V += 120.0f; }
            }

            // ---- THE IDLE, all of it a function of the shared clock.
            // A small gesture now and then: one slot every 13 s, most of them taken.
            long slot = (long)Math.Floor(clock / 13.0);
            float pick = Hash(slot), starts = 1.0f + Hash(slot + 7919) * 5.0f;
            float into = (float)(clock - slot * 13.0) - starts;
            const float gestureSeconds = 4.2f;
            float gesture = into > 0.0f && into < gestureSeconds ? Mathf.Sin(into / gestureSeconds * Mathf.PI) : 0.0f;   // 0, up to 1, back to 0
            int which = pick < 0.34f ? 0 : pick < 0.52f ? 1 : pick < 0.68f ? 2 : pick < 0.82f ? 3 : 4;                  // wave, look, kick, shimmy, nothing

            float wave = which == 0 ? gesture * (10.0f + 17.0f * Mathf.Sin(into * 2.0f * Mathf.PI * 1.6f)) : 0.0f;
            float look = which == 1 ? gesture * 7.0f * Mathf.Sin(into / gestureSeconds * 2.0f * Mathf.PI) : 0.0f;
            float kick = which == 2 ? gesture * 15.0f * Mathf.Sin(into * 2.0f * Mathf.PI * 1.25f) : 0.0f;
            float shimmy = which == 3 ? gesture * 2.2f * Mathf.Sin(into * 2.0f * Mathf.PI * 2.4f) : 0.0f;

            // The rig: a slow lean on two axes out of step, and a ride up and down its tether.
            float leanX = 1.5f * Wave(clock, 9.0) + 0.7f * Wave(clock, 5.3, 0.3) + _leanX.X + shimmy;
            float leanZ = 1.1f * Wave(clock, 12.3, 0.25) + 0.5f * Wave(clock, 6.7, 0.6) + _leanZ.X;
            float ride = 0.7f * Wave(clock, 7.0, 0.1) + _ride.X;
            // A lean of (x, z) degrees tips the top of the balloon that way.
            var lean = new Vector3(leanX, 0.0f, leanZ);
            float tilt = Mathf.Min(lean.magnitude, 24.0f);
            _rig.SetPositionAndRotation(_rigAt + _tether * ride, tilt > 1e-4f ? Quaternion.AngleAxis(tilt, Vector3.Cross(Vector3.up, lean.normalized)) : Quaternion.identity);

            // The body: breathing, the hit's jiggle, the swell. Volume kept: longer is thinner.
            float stretch = Mathf.Clamp(0.014f * Wave(clock, 5.2, 0.4) + _squash.X + tremble, -0.3f, 0.3f);
            float size = Mathf.Max(0.02f, grown * (1.0f + _swell + Mathf.Clamp(_size.X, -0.2f, 0.25f)));
            float wide = 1.0f / Mathf.Sqrt(1.0f + stretch);
            Body.localScale = Vector3.Scale(_bodyScale, new Vector3(size * wide, size * (1.0f + stretch), size * wide));
            Body.localPosition = _bodyAt * Mathf.Lerp(grown, 1.0f, 0.35f);
            Body.localRotation = _bodyTurn * Quaternion.Euler(0.0f, look, Mathf.Clamp(_wobble.X, -9.0f, 9.0f) + 0.6f * Wave(clock, 3.9, 0.7));

            // The lobes, each about its own pivot, in the body's frame: z is front to back, x across.
            float arms = Mathf.Clamp(_arms.X, -38.0f, 38.0f), legs = Mathf.Clamp(_legs.X, -30.0f, 30.0f), tails = Mathf.Clamp(_tails.X, -45.0f, 45.0f);
            Turn(0, Quaternion.AngleAxis(wave + arms + 3.0f * Wave(clock, 4.3), Vector3.forward) * Quaternion.AngleAxis(arms * 0.3f, Vector3.right));
            Turn(1, Quaternion.AngleAxis(-arms * 0.8f + 3.0f * Wave(clock, 5.1, 0.4), Vector3.forward) * Quaternion.AngleAxis(-arms * 0.3f, Vector3.right));
            Turn(2, Quaternion.AngleAxis(legs + kick + 5.0f * Wave(clock, 3.9), Vector3.right));
            Turn(3, Quaternion.AngleAxis(-legs * 0.7f - kick + 5.0f * Wave(clock, 3.9, 0.5), Vector3.right));
            Turn(4, Quaternion.AngleAxis(tails + 7.0f * Wave(clock, 2.7), Vector3.forward) * Quaternion.AngleAxis(tails * 0.4f + 4.0f * Wave(clock, 3.4, 0.2), Vector3.right));
            Turn(5, Quaternion.AngleAxis(tails * 0.7f + 7.0f * Wave(clock, 3.1, 0.35), Vector3.forward) * Quaternion.AngleAxis(tails * 0.3f + 4.0f * Wave(clock, 2.9, 0.6), Vector3.right));

            DressSkin(strain);
            PoseRopes();
        }

        private void Turn(int lobe, Quaternion about)
        {
            var part = _lobes[lobe];
            if (part == null) return;
            part.localPosition = _lobeAt[lobe];
            part.localRotation = about * _lobeTurn[lobe];
        }

        /// <summary>
        /// Every rope from its anchor to its ring. Popped (`_limp` 1), the ring's end drops to
        /// the canopy and the winch reels the slack in: a short length lying on the roof.
        /// </summary>
        private void PoseRopes()
        {
            for (int i = 0; i < Ropes.Length; i++)
            {
                var rope = Ropes[i];
                if (rope == null || _ties[i] == null) continue;
                Vector3 anchor = rope.position;
                Vector3 tie = _popped ? _tieRest[i] : _ties[i].position;
                if (_limp > 0.0f)
                {
                    Vector3 flat = _tieRest[i] - anchor; flat.y = 0.0f;
                    Vector3 lying = anchor + flat.normalized * (_ropeLength[i] * 0.28f) + Vector3.up * 0.4f;
                    // It whips: down fast, a bounce, then still.
                    float fall = 1.0f - Mathf.Abs(Mathf.Cos(_limp * Mathf.PI * 1.5f)) * (1.0f - _limp);
                    tie = Vector3.Lerp(tie, lying, Mathf.Clamp01(_popped ? fall : _limp));
                }

                Vector3 along = tie - anchor;
                float length = along.magnitude;
                if (length < 0.01f) continue;
                rope.rotation = Quaternion.FromToRotation(_ropeDirection[i], along / length) * _ropeTurn[i];
                var scale = _ropeScale[i];
                rope.localScale = new Vector3(scale.x, scale.y * length / _ropeLength[i], scale.z);
            }
        }
    }
}
