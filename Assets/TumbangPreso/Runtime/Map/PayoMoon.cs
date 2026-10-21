using System;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// BUWAN, PAYO'S CUTE MOON, AND MAYARI OF THE STARS. The owner, 2026-10-19: "the mayari could use a rework. take a look
    /// at the godess in the paete ult, i want something like that but moon base. maybe instead of her showing at all times,
    /// do an easter egg similar to the balloon slipper in Arena where we have a cartoony moon with a cute face, hit it
    /// enough times and mayari appears in the sky, looking like moving stars, + a meteor/falling star shower".
    ///
    /// WHAT IS IN THE SKY. At night (the second half, `PayoNight.Factor`) a storybook crescent moon with a cute face
    /// rises over the valley's open end: its face on the inner curve looking sideways, big glossy eyes, blush, a nose
    /// on the inner edge, soft craters, a striped nightcap with a gold star (`tools/payo_pieces/moon_cute.py`, sheet
    /// `Logs/payo/pieces/moon/moon_v5.png`). (The first, a round moon with arms on a cloud, was refused by the owner in
    /// game: "does not look like a moon at all".) The always-shown Mayari cut-out of the art is hidden by the builder:
    /// she is now the reward. The moon bobs, sways and blinks.
    ///
    /// THE EASTER EGG, AS A RULE (<see cref="HostOfferThrow"/>), THE BALLOON'S RULE: the moon is 2.9 km away and a slipper
    /// flies 17 m, so the hit is decided, honestly, at the THROW, by the host: a human seat, a plain slipper, at
    /// <see cref="MinCharge"/> of full charge or more, at full night, whose sight line is within <see cref="ConeDegrees"/>
    /// of the moon's middle and at least <see cref="MinElevationDegrees"/> above level, aimed at nothing inside the play
    /// area. Such a throw leaves play, is DRAWN flying up to the moon as a comet (<see cref="FlightSeconds"/>), strikes it,
    /// and is set back loose on the stage by `ArenaFallRecovery` at the nearest standable point to where its line left the
    /// play area. Score, the can, tags and every rule are untouched: it is a missed throw that came back. Ordinary play
    /// cannot trigger it for the balloon's reasons: a throw at the can, a body or a block aims INSIDE the play area, a bot is
    /// refused by seat and a loaded ability throw by its affinity; and by day there is no moon.
    ///
    /// WHAT A HIT DOES. The moon is knocked back and squashes, wobbles, its nightcap flops, star beads and gold stars fly
    /// off where it was hit, and it pulls a face: squeezed "ouch" (>), from the third hit it rests worried (a small
    /// startled dot eye, a brow, a sweat drop), the fourth leaves it dizzy (a swirl, stars round its head). At rest its
    /// eye is a peaceful closed lid: no face has a big open eye (owner: "the moon eyes open looks so creepy").
    ///
    /// THE REWARD, on the <see cref="RevealHits"/>th hit. Dizzy for <see cref="DizzySeconds"/>, then a ring of light runs
    /// out of it and MAYARI THE GODDESS forms round it as a GHOST in the sky (owner: "model an actual godess like the
    /// paete ult. then ghost effect her, place her in the sky, and make stars glow across her sillhouete"): a modelled
    /// figure (`goddess.bytes`, `PayoMayariGhost.shader`), see-through with a glowing rim, her hem dissolving, stars
    /// twinkling and drifting across her and along her outline, holding the moon between her cupped hands; she forms from
    /// the moon outward. A shower of falling stars runs all round her (`PayoMoonSky.shader`). The moon beams (^ ^).
    /// Her model passed to another session on 2026-10-19 (the owner's call); the contract it must keep is
    /// `GoddessHands`/`GoddessScale` here and the vertex data in `PayoMayariGhost.shader`'s header.
    /// HOW IT SETTLES (the owner left it to us): she stays <see cref="HoldSeconds"/>, long enough to be seen from anywhere
    /// on the court between two plays, then dissolves from the hem up into star dust as the moon falls asleep (closed
    /// lid, zzz) for <see cref="SleepSeconds"/>; asleep it cannot be hit (throws pass as ordinary throws) so the reward is
    /// a moment, not a strobe. Then it wakes with no hits on it and can be won again. A new match starts it fresh.
    ///
    /// WHAT IS ON THE WIRE: ONE small host message, `PayoMoon` (`MatchRpc.PayoMoon.cs`, also in the late-join snapshot),
    /// exactly the balloon's: the hits so far, whether a reward is running, and the last event (a hit with its origin, line
    /// and launch time on the shared clock). Every peer draws the flight, the reaction and the whole reward from that: the
    /// reward's every beat is a function of the revealing hit's launch time on `ArenaFx.SharedClock`, so a peer that joins
    /// in the middle sees her as far along as everybody else. A client sends nothing.
    ///
    /// ⚠️ GPU-DRIVEN (owner's standing rule): six baked static meshes (`PayoMoonMeshes`), three shaders; every meteor,
    /// burst piece, face, the nightcap, her hair and hem and her stars are moved in the shaders. A frame is a few transforms and property blocks.
    /// Mayari and the shower are drawn only while the reward runs, the burst for 1.4 s after a hit, the comet in flight.
    /// Its own CPU cost is kept in <see cref="Ticks"/>.
    /// </summary>
    [DefaultExecutionOrder(890)]
    public sealed class PayoMoon : MonoBehaviour
    {
        public static PayoMoon Instance { get; private set; }

        public const string MoonShader = "TumbangPreso/PayoMoon", SkyShader = "TumbangPreso/PayoMoonSky", GhostShader = "TumbangPreso/PayoMayariGhost";

        // ------------------------------------------------------------------ the rule's numbers

        /// <summary>The hit that brings her.</summary>
        public const int RevealHits = 5;
        /// <summary>The least charge, 0 to 1, that reaches it: "at full range", as the balloon.</summary>
        public const float MinCharge = 0.9f;
        /// <summary>How far, in degrees, the sight line may be from the moon's middle. Seen from the court the moon's
        /// crescent is about 6 degrees each way of its middle (320 m radius at 2.9 km): the cone covers it and a
        /// little round it.</summary>
        public const float ConeDegrees = 7.5f;
        /// <summary>The least the sight line climbs (the moon's middle is 22 degrees up).</summary>
        public const float MinElevationDegrees = 10.0f;
        public const float AimSlack = 1.0f;
        /// <summary>The drawn flight from the hand to the moon, and from the strike until the slipper is back on the stage.</summary>
        public const float FlightSeconds = 1.1f, ReturnSeconds = 1.4f;
        /// <summary>The reward's beats, from the revealing strike: dizzy, then Mayari for `HoldSeconds` and her leaving, then sleep.</summary>
        public const float DizzySeconds = 1.4f, HoldSeconds = 22.0f, LeaveSeconds = 4.5f, SleepSeconds = 40.0f;
        /// <summary>From the start of the reward (her first star) to the shower's start, and how long the wide-eyed face holds.</summary>
        private const float ShowerFrom = 0.8f, WowSeconds = 2.6f;
        private const int MaxFlights = 4;

        // ------------------------------------------------------------------ where it hangs (the scene builder sets these)

        /// <summary>Degrees from +z toward +x (the moon's light, `PayoSceneBuilder.MoonYaw` less 180), degrees up, metres
        /// out from `Court`, and the moon's radius in metres.</summary>
        public float Bearing = 13.0f, Elevation = 22.0f, Distance = 2900.0f, Radius = 320.0f;
        /// <summary>The shower is typed in moon radii round a moon of radius 1; this draws it over the sky round her.</summary>
        public const float ShowerScale = 0.66f;
        /// <summary>MAYARI THE GODDESS (`tools/payo_pieces/mayari_goddess.py`): her model's units to the sky piece's (moon
        /// radii), and the point between her cupped hands in her model (Unity space), which is put on the moon. At 1.8 she
        /// is about 7.7 moon radii, 2.5 km, tall: from the ranges at the horizon to about 46 degrees up, the moon in her hands.</summary>
        public const float GoddessScale = 1.8f;
        public static readonly Vector3 GoddessHands = new Vector3(0.0f, 2.05f, 0.82f);
        /// <summary>What it faces and is measured from: the middle of the court at eye height.</summary>
        public Vector3 Court = new Vector3(0.0f, 1.7f, 0.0f);

        /// <summary>The middle of the moon at full night, in the world: what a throw is aimed at.</summary>
        public Vector3 Centre => Court + Direction(Bearing, Elevation) * Distance;

        // ------------------------------------------------------------------ the wire

        public enum Face { Rest, Worried, Ouch, Dizzy, Wow, Happy, Sleepy }

        /// <summary>What travels: the moon's shared state and its last event (the balloon's message, the same fields).</summary>
        public struct Wire
        {
            public const byte None = 0, Hit = 1, RevealHit = 2;
            public byte Hits, Kind;
            /// <summary>True while a reward runs (Mayari, then the sleep); `Launch` is then the revealing throw's launch.</summary>
            public bool Revealing;
            public int Serial, Seat;
            public Vector3 Origin, Direction;
            public double Launch;
        }

        private struct Flight
        {
            public Vector3 Origin, Direction;
            public float Age;
            public int Number, Seat;
            public bool Reveals, Live;
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

        // The count (flights in the air included), what the moon is showing, the reward.
        private int _hits, _shown, _serial;
        private double _revealLaunch = double.NaN;
        private long _match;
        private Wire _last;
        private readonly Flight[] _flights = new Flight[MaxFlights];

        // The pieces.
        private Transform _root, _body, _burst, _comet;
        private MeshRenderer _moonR, _glowR, _burstR, _mayariR, _showerR, _cometR;
        private MaterialPropertyBlock _block;
        private static Material _moonMat, _skyMat, _ghostMat;
        private bool _built, _warned;

        // The motion.
        private Spring _squash, _wobble, _push, _ride, _arms, _crown;
        private Face _face = Face.Rest;
        private float _faceLeft, _burstAge = -1.0f, _flash, _clock, _blinkAt = 2.0f, _blink = 1.0f;
        private bool _ringSaid, _sleepSaid;

        private static readonly int PoseId = Shader.PropertyToID("_Pose"), GlowId = Shader.PropertyToID("_Glow"), TId = Shader.PropertyToID("_T"),
            SeenId = Shader.PropertyToID("_Seen"), BrightId = Shader.PropertyToID("_Bright"), MainTexId = Shader.PropertyToID("_MainTex");

        public static long Ticks;
        public static int Frames;
        /// <summary>A probe's switch: true draws nothing (everything else runs), so its render cost can be told apart. Never set in play.</summary>
        public static bool HideForMeasure;
        private static int _countedFrame = -1;

        public int Hits => _hits;
        public bool Revealing => !double.IsNaN(_revealLaunch);
        public Face Showing => _face;
        /// <summary>Seconds since Mayari's first star (under 0 before; NaN with no reward running).</summary>
        public float RevealAge => Revealing ? (float)(Now - RevealStart(_revealLaunch)) : float.NaN;

        private static double Now => ArenaFx.SharedClock;
        private static double RevealStart(double launch) => launch + FlightSeconds + DizzySeconds;
        private static double RewardEnd(double launch) => RevealStart(launch) + HoldSeconds + LeaveSeconds + SleepSeconds;

        public static Vector3 Direction(float bearing, float elevation)
        {
            float b = bearing * Mathf.Deg2Rad, e = elevation * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(b) * Mathf.Cos(e), Mathf.Sin(e), Mathf.Cos(b) * Mathf.Cos(e));
        }

        /// <summary>How much of the night it is up for: none by day, risen at full night (`PayoNight`'s Mayari curve).</summary>
        public static float Seen
        {
            get
            {
                float t = Mathf.Clamp01((PayoNight.Factor - 0.45f) / 0.5f);
                return t * t * (3.0f - 2.0f * t);
            }
        }

        // ------------------------------------------------------------------ lifecycle

        private void Awake()
        {
            Instance = this;
            _block = new MaterialPropertyBlock();
            Build();
        }

        private void OnEnable() { Instance = this; }

        private void OnDisable() { if (Instance == this) Instance = null; }

        private void Build()
        {
            if (_built) return;
            var moonShader = Shader.Find(MoonShader);
            var skyShader = Shader.Find(SkyShader);
            var ghostShader = Shader.Find(GhostShader);
            if (moonShader == null || skyShader == null || ghostShader == null || PayoMoonMeshes.Moon == null || PayoMoonMeshes.Goddess == null)
            {
                if (!_warned) { _warned = true; Debug.LogWarning("[Payo] the moon is not drawn: its shaders or meshes are missing (Resources/Shaders/PayoMoon*.shader, Resources/Map/PayoMoon/)"); }
                return;
            }
            if (_moonMat == null) _moonMat = new Material(moonShader) { name = "Payo moon (run time)", hideFlags = HideFlags.DontSave, mainTexture = PayoMoonMeshes.Atlas };
            if (_skyMat == null) _skyMat = new Material(skyShader) { name = "Payo moon sky (run time)", hideFlags = HideFlags.DontSave };
            if (_ghostMat == null) _ghostMat = new Material(ghostShader) { name = "Payo Mayari ghost (run time)", hideFlags = HideFlags.DontSave };

            _root = new GameObject("Moon sky").transform;
            _root.SetParent(transform, false);
            _body = Piece("Buwan", _root, PayoMoonMeshes.Moon, _moonMat, out _moonR);
            Piece("Glow", _root, PayoMoonMeshes.Glow, _skyMat, out _glowR);
            _burst = Piece("Burst", _root, PayoMoonMeshes.Burst, _skyMat, out _burstR);
            var her = Piece("Mayari", _root, PayoMoonMeshes.Goddess, _ghostMat, out _mayariR);
            her.localScale = Vector3.one * GoddessScale;
            her.localPosition = -GoddessHands * GoddessScale;
            Piece("Shower", _root, PayoMoonMeshes.Meteors, _skyMat, out _showerR).localScale = Vector3.one * ShowerScale;
            _comet = Piece("Comet", transform, PayoMoonMeshes.Comet, _skyMat, out _cometR);
            _burstR.enabled = _mayariR.enabled = _showerR.enabled = _cometR.enabled = false;
            _built = true;
        }

        private static Transform Piece(string name, Transform parent, Mesh mesh, Material material, out MeshRenderer renderer)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            renderer.allowOcclusionWhenDynamic = false;
            return go.transform;
        }

        // ------------------------------------------------------------------ THE RULE (host)

        /// <summary>
        /// The rule and nothing else: would a throw from `origin` aimed at `aimPoint` with this charge be a throw at the
        /// moon whose middle is `centre`? Pure, so a probe and the host ask the same thing.
        /// </summary>
        public static bool IsAimedAtMoon(Vector3 centre, Vector3 origin, Vector3 aimPoint, float charge)
        {
            if (charge < MinCharge) return false;
            Vector3 sight = aimPoint - origin;
            float reach = sight.magnitude;
            if (reach < 0.5f || !float.IsFinite(reach)) return false;
            sight /= reach;
            if (Mathf.Asin(Mathf.Clamp(sight.y, -1.0f, 1.0f)) * Mathf.Rad2Deg < MinElevationDegrees) return false;
            if (Vector3.Angle(sight, centre - origin) > ConeDegrees) return false;
            return reach >= ArenaBalloon.PlayExit(origin, sight) - AimSlack;
        }

        /// <summary>
        /// HOST, from `Carrier.HostThrowAt` just after the slipper has been thrown: if this throw was at the moon, the
        /// slipper is taken out of play, its hit is counted and told to every peer, and it is booked to come back. True
        /// when it was taken. On every other map there is no instance and this is one null test.
        /// </summary>
        public static bool HostOfferThrow(Slipper thrown, CharacterMotor thrower, Vector3 origin, Vector3 aimPoint, float charge, SlipperAffinity affinity)
        {
            var moon = Instance;
            if (moon == null) return false;
            return moon.HostTake(thrown, thrower, origin, aimPoint, charge, affinity);
        }

        /// <summary>True when the host would count a hit now: built, at full night, no reward running, room for one more.</summary>
        private bool Open => _built && isActiveAndEnabled && NetAuthority.ShouldResolve() && Seen >= 0.999f && !Revealing && _hits < RevealHits;

        private bool HostTake(Slipper thrown, CharacterMotor thrower, Vector3 origin, Vector3 aimPoint, float charge, SlipperAffinity affinity)
        {
            if (!Open) return false;
            if (thrown == null || thrower == null || thrower.IsBot || affinity != SlipperAffinity.Normal) return false;
            if (!IsAimedAtMoon(Centre, origin, aimPoint, charge)) return false;

            var recovery = ArenaFallRecovery.Instance;
            if (recovery == null) return false;
            Vector3 sight = (aimPoint - origin).normalized;
            Vector3 left = origin + sight * ArenaBalloon.PlayExit(origin, sight);
            if (!recovery.HostSendAway(thrown, left, FlightSeconds + ReturnSeconds)) return false;

            HostCount(thrown.SeatOfOrigin, origin, sight);
            return true;
        }

        private void HostCount(int seat, Vector3 origin, Vector3 sight)
        {
            _hits++;
            bool reveals = _hits >= RevealHits;
            double launch = Now;
            if (reveals) _revealLaunch = launch;
            _last = new Wire
            {
                Hits = (byte)_hits, Kind = reveals ? Wire.RevealHit : Wire.Hit, Revealing = reveals, Serial = ++_serial,
                Seat = seat, Origin = origin, Direction = sight, Launch = launch,
            };
            Launch(_last, 0.0f);
            Net.MatchRpc.Instance?.BroadcastPayoMoon(_last);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// A PROBE'S HIT (editor and development builds, offline host only): counts one hit from `origin` as if a slipper
        /// had been thrown at the moon, with no slipper. `Temp/moon.request` (`PayoMoonShots`) films the reactions and the
        /// reward with it. False when the rule's state would refuse a real throw (day, a reward running).
        /// </summary>
        public bool DevHit(Vector3 origin, int seat = 0)
        {
            if (!Open || NetAuthority.IsNetworked) return false;
            HostCount(seat, origin, (Centre - origin).normalized);
            return true;
        }
#endif

        /// <summary>HOST: the state for a peer that has just arrived (no event: it replays no flight).</summary>
        public Wire HostSnapshot()
        {
            var state = _last;
            state.Hits = (byte)_hits; state.Serial = _serial; state.Kind = Wire.None;
            state.Revealing = Revealing;
            if (Revealing) state.Launch = _revealLaunch;
            return state;
        }

        private void HostTick()
        {
            if (!NetAuthority.ShouldResolve()) return;
            // A new match starts with a fresh moon, quietly.
            var match = GameServices.Match;
            long id = match != null ? match.PresentationMatchId : 0L;
            if (id == _match) return;
            _match = id;
            if (_hits == 0 && !Revealing) return;
            Snap(0, false, 0.0);
            _last = new Wire { Serial = ++_serial, Launch = Now };
            Net.MatchRpc.Instance?.BroadcastPayoMoon(_last);
        }

        // ------------------------------------------------------------------ what a client is told

        /// <summary>
        /// CLIENT: the host's state. A fresh hit is played from where it has got to (its launch is on the shared clock);
        /// a reward is joined wherever it has got to; anything else, and a late joiner's snapshot, is shown as it stands.
        /// </summary>
        public void Apply(in Wire state)
        {
            if (NetAuthority.IsHost || !_built) return;
            int hits = Mathf.Clamp(state.Hits, 0, RevealHits);
            if (state.Serial == _serial && state.Kind != Wire.None) return;
            bool fresh = state.Serial != _serial;
            _serial = state.Serial;
            _last = state;
            float age = (float)(Now - state.Launch);

            if (fresh && (state.Kind == Wire.Hit || state.Kind == Wire.RevealHit) && age > -1.0f && age < FlightSeconds + 2.0f)
            {
                _hits = hits;
                if (state.Kind == Wire.RevealHit) _revealLaunch = state.Launch;
                Launch(state, Mathf.Max(0.0f, age));
                return;
            }
            // Not a hit this peer saw begin: put right only what differs.
            double launch = state.Revealing ? state.Launch : 0.0;
            if (hits != _hits || state.Revealing != Revealing || (state.Revealing && Math.Abs(launch - _revealLaunch) > 0.01)) Snap(hits, state.Revealing, launch);
        }

        /// <summary>The moon as it stands, at once: for a late joiner and a new match.</summary>
        private void Snap(int hits, bool revealing, double launch)
        {
            for (int i = 0; i < _flights.Length; i++) _flights[i].Live = false;
            _hits = hits; _shown = hits;
            _revealLaunch = revealing && Now < RewardEnd(launch) ? launch : double.NaN;
            if (!Revealing && revealing) { _hits = 0; _shown = 0; }
            _faceLeft = 0.0f; _burstAge = -1.0f;
            _ringSaid = _sleepSaid = Revealing && Now - RevealStart(_revealLaunch) > 0.0;
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
                Age = age, Number = Mathf.Clamp(state.Hits, 1, RevealHits), Seat = state.Seat, Reveals = state.Kind == Wire.RevealHit, Live = true,
            };
            if (age < 0.25f) ArenaFx.Cue("sfx_arena_balloon_fly", state.Origin, 1.05f, 1.15f);
        }

        /// <summary>The crescent's thick middle, in the sky piece's own space (moon radii): its bounding middle, the
        /// origin, is the empty inside of the curve, so a slipper lands HERE, on its cheek under the eyes.</summary>
        private static readonly Vector3 Cheek = new Vector3(0.3f, 0.0f, 0.36f);

        /// <summary>Where on the moon a slipper thrown from `origin` lands: on its cheek, a little toward the side it came from.</summary>
        private Vector3 ImpactPoint(Vector3 origin)
        {
            if (_root == null) return Centre;
            Vector3 side = _root.InverseTransformDirection((origin - _root.position).normalized);
            return _root.TransformPoint(Cheek + new Vector3(side.x, side.y, 0.0f) * 0.12f);
        }

        private static Vector3 Path(Vector3 from, Vector3 direction, Vector3 to, float u)
        {
            Vector3 lead = from + direction * ((to - from).magnitude * 0.45f);
            float v = 1.0f - u;
            return from * (v * v) + lead * (2.0f * v * u) + to * (u * u);
        }

        private void Flights(float dt)
        {
            bool drawn = false;
            for (int i = 0; i < _flights.Length; i++)
            {
                if (!_flights[i].Live) continue;
                ref var flight = ref _flights[i];
                flight.Age += dt;
                Vector3 target = ImpactPoint(flight.Origin);
                float u = Mathf.Clamp01(flight.Age / FlightSeconds);
                float eased = 1.0f - (1.0f - u) * (1.0f - u);
                if (u >= 1.0f) { flight.Live = false; Strike(flight, target); continue; }
                if (drawn || _comet == null) continue;
                // One comet is drawn (the newest flights overlap rarely): its head on the path, its tail along the way it came.
                Vector3 head = Path(flight.Origin, flight.Direction, target, eased);
                Vector3 back = Path(flight.Origin, flight.Direction, target, Mathf.Max(0.0f, eased - 0.04f));
                Vector3 along = head - back;
                if (along.sqrMagnitude < 1e-6f) along = flight.Direction;
                float away = (head - flight.Origin).magnitude;
                float size = 0.8f + away * 0.02f;
                _comet.SetPositionAndRotation(head, Quaternion.LookRotation(along.normalized, Vector3.up));
                _comet.localScale = new Vector3(size, size, size * 3.0f);
                drawn = true;
            }
            if (_cometR != null) _cometR.enabled = drawn && !HideForMeasure;
        }

        private static readonly string[] Words = { "BOINK!", "ARAY!", "UY!", "HALA!" };
        private static readonly string[] Squeaks = { "sfx_arena_balloon_squeak_a", "sfx_arena_balloon_squeak_b", "sfx_arena_balloon_squeak_c" };

        private void Strike(in Flight flight, Vector3 at)
        {
            int n = flight.Number;
            _shown = Mathf.Max(_shown, n);
            float k = 1.0f + 0.2f * (n - 1);
            _squash.V -= 1.6f * k;
            _wobble.V += (flight.Seat % 2 == 0 ? 1.0f : -1.0f) * 75.0f * k;
            _push.V += 2.4f * k;
            _ride.V += 0.8f * k;
            _arms.V += 9.0f * k;
            _crown.V += 6.0f * (flight.Seat % 2 == 0 ? 1.0f : -1.0f) * k;
            _flash = 0.22f + 0.05f * n;
            bool dizzy = n >= RevealHits - 1;
            Wear(dizzy ? Face.Dizzy : Face.Ouch, flight.Reveals ? DizzySeconds + 0.3f : dizzy ? 2.2f : 1.3f);

            // The burst, on the face where it was hit.
            if (_burst != null && _root != null)
            {
                _burst.localPosition = _root.InverseTransformPoint(at) + new Vector3(0.0f, 0.0f, 0.05f);
                _burstAge = 0.0f;
            }
            ArenaFx.CueFlat(Squeaks[(n - 1) % Squeaks.Length], 1.05f + 0.06f * n, 1.12f + 0.06f * n, 0.85f);
            ArenaFx.CueFlat("sfx_arena_balloon_boing", 1.15f - 0.04f * n, 1.2f - 0.04f * n, 0.7f + 0.05f * n);

            var view = ArenaFx.View;
            if (view != null)
            {
                Vector3 eye = view.transform.position;
                string word = flight.Reveals ? "WAAAH!" : Words[(n - 1) % Words.Length];
                ComicPopup.Spawn(eye + (at + Vector3.up * Radius * 1.2f - eye).normalized * 14.0f, word, new Color(1.0f, 0.86f, 0.36f), 2.2f + 0.2f * n, ComicPopup.Weight.Cast);
            }
        }

        private void Wear(Face face, float seconds) { _face = face; _faceLeft = Mathf.Max(0.0f, seconds); }

        // ------------------------------------------------------------------ the frame

        private static float Wave(double clock, double period, double phase = 0.0) => (float)Math.Sin((clock / period + phase) * (2.0 * Math.PI));

        private static float Hash(long n)
        {
            ulong z = (ulong)n * 0x9E3779B97F4A7C15UL;
            z ^= z >> 31; z *= 0xBF58476D1CE4E5B9UL; z ^= z >> 29;
            return (z & 0xFFFFFFUL) / 16777216.0f;
        }

        private void LateUpdate()
        {
            long began = System.Diagnostics.Stopwatch.GetTimestamp();
            if (!_built) { Build(); if (!_built) return; }
            float dt = ArenaFx.Step;
            _clock += dt;
            double clock = Now;

            HostTick();
            Flights(dt);

            // The reward, where it has got to (the same on every peer, from the revealing throw's launch).
            float rel = float.NaN;
            if (Revealing)
            {
                if (clock >= RewardEnd(_revealLaunch)) { _revealLaunch = double.NaN; _hits = 0; _shown = 0; _face = Face.Rest; _faceLeft = 0; }
                else rel = (float)(clock - RevealStart(_revealLaunch));
            }

            float seen = Seen;
            bool up = seen > 0.0f && !HideForMeasure;
            _moonR.enabled = up; _glowR.enabled = up;

            // Hanging: it rises over the valley's open end with the night, faces the court.
            float elevation = Elevation - (1.0f - seen) * 28.0f;
            Vector3 at = Court + Direction(Bearing, elevation) * Distance;
            _root.SetPositionAndRotation(at, Quaternion.LookRotation((Court - at).normalized, Vector3.up));
            _root.localScale = Vector3.one * Radius;

            // The face it wears.
            if (_faceLeft > 0.0f) { _faceLeft -= dt; }
            Face face = _face;
            if (_faceLeft <= 0.0f)
            {
                if (!float.IsNaN(rel) && rel >= 0.0f)
                    face = rel < WowSeconds ? Face.Wow : rel < HoldSeconds + 0.8f ? Face.Happy : Face.Sleepy;
                else if (!float.IsNaN(rel) && rel >= -DizzySeconds) face = Face.Dizzy;
                else face = _shown >= 3 ? Face.Worried : Face.Rest;
                _face = face;
            }

            // The beats of the reward that happen once.
            if (!float.IsNaN(rel))
            {
                if (rel >= 0.0f && !_ringSaid)
                {
                    _ringSaid = true;
                    if (rel < 1.0f)
                    {
                        _squash.V += 1.2f; _arms.V += 14.0f; _ride.V += 1.2f;
                        ArenaFx.CueFlat("sfx_arena_pa_chime", 1.15f, 1.15f, 0.9f);
                        ArenaFx.CueFlat("sfx_arena_balloon_hiss", 1.6f, 1.6f, 0.35f);
                        var view = ArenaFx.View;
                        if (view != null)
                        {
                            Vector3 eye = view.transform.position;
                            ComicPopup.Spawn(eye + (at + Vector3.up * Radius * 3.0f - eye).normalized * 14.0f, "MAYARI!", new Color(0.86f, 0.80f, 1.0f), 3.2f, ComicPopup.Weight.Cast);
                        }
                    }
                }
                if (rel >= HoldSeconds + 0.8f && !_sleepSaid)
                {
                    _sleepSaid = true;
                    if (rel < HoldSeconds + 2.0f) { _arms.V -= 6.0f; ArenaFx.CueFlat("sfx_arena_balloon_hiss", 0.7f, 0.7f, 0.3f); }
                }
            }
            else { _ringSaid = false; _sleepSaid = false; }

            // The springs.
            _squash.Step(dt, 1.6f, 0.14f);
            _wobble.Step(dt, 2.1f, 0.12f);
            _push.Step(dt, 0.9f, 0.25f);
            _ride.Step(dt, 0.8f, 0.22f);
            _arms.Step(dt, 1.4f, 0.16f);
            _crown.Step(dt, 2.6f, 0.10f);
            _flash = Mathf.MoveTowards(_flash, 0.0f, dt * 1.2f);

            // ---- THE IDLE, a function of the shared clock: a bob, a sway, a wave now and then, a blink.
            long slot = (long)Math.Floor(clock / 11.0);
            float into = (float)(clock - slot * 11.0) - (1.0f + Hash(slot + 31) * 5.0f);
            float wave = Hash(slot) < 0.45f && into > 0.0f && into < 3.0f ? Mathf.Sin(into / 3.0f * Mathf.PI) : 0.0f;
            bool asleep = face == Face.Sleepy;
            bool cheering = face == Face.Happy || face == Face.Wow;
            float bob = (asleep ? 0.03f : 0.06f) * Wave(clock, asleep ? 7.0 : 5.0) + _ride.X * 0.1f;
            float sway = (asleep ? 6.0f : 3.0f) * Wave(clock, asleep ? 9.0 : 7.3, 0.2) + Mathf.Clamp(_wobble.X, -16.0f, 16.0f);
            float stretch = Mathf.Clamp(0.02f * Wave(clock, 3.7, 0.4) + _squash.X * 0.1f, -0.28f, 0.28f);
            float wide = 1.0f / Mathf.Sqrt(1.0f + stretch);
            _body.localPosition = new Vector3(0.0f, bob, -Mathf.Clamp(_push.X, -1.0f, 2.0f) * 0.25f);
            _body.localRotation = Quaternion.Euler(Mathf.Clamp(_push.X, -1.0f, 2.0f) * -6.0f, 0.0f, sway);
            _body.localScale = new Vector3(wide, 1.0f + stretch, wide);

            // The blink, now and then (not asleep, not with ^ ^ or > < eyes: the shader blinks only the open ones).
            _blinkAt -= dt;
            if (_blinkAt <= 0.0f) { _blinkAt = 2.2f + Hash((long)(clock * 10.0)) * 3.0f; _blink = 0.0f; }
            _blink = Mathf.MoveTowards(_blink, 1.0f, dt * 7.0f);
            float lid = _blink < 0.5f ? Mathf.Lerp(1.0f, 0.08f, _blink * 2.0f) : Mathf.Lerp(0.08f, 1.0f, (_blink - 0.5f) * 2.0f);

            float arms = Mathf.Clamp(_arms.X * 0.06f, -0.5f, 1.4f) + wave * 0.5f * (1.0f + Mathf.Sin(_clock * 9.0f) * 0.4f)
                         + (cheering ? 0.55f + 0.25f * Mathf.Sin(_clock * 4.0f) : 0.0f) + (asleep ? -0.25f : 0.0f);
            float glow = 1.0f + _flash + (cheering ? 0.22f : 0.0f) + (asleep ? -0.12f : 0.0f);

            _moonR.GetPropertyBlock(_block);
            _block.SetVector(PoseId, new Vector4((float)face, arms, Mathf.Clamp(_crown.X * 0.05f, -0.5f, 0.5f), lid));
            _block.SetFloat(GlowId, glow);
            if (PayoMoonMeshes.Atlas != null) _block.SetTexture(MainTexId, PayoMoonMeshes.Atlas);
            _moonR.SetPropertyBlock(_block);

            float idle = (float)(clock % 3600.0);
            float revealAge = float.IsNaN(rel) ? -100.0f : rel;
            float leaveAge = float.IsNaN(rel) ? -100.0f : rel - HoldSeconds;

            // The glow disc, and the ring as she comes.
            Sky(_glowR, revealAge, -100.0f, revealAge, idle, seen, 1.0f + _flash * 1.5f + (cheering ? 0.6f : 0.0f) - (asleep ? 0.3f : 0.0f));

            // The burst.
            if (_burstAge >= 0.0f) { _burstAge += dt; if (_burstAge > 1.4f) _burstAge = -1.0f; }
            _burstR.enabled = up && _burstAge >= 0.0f;
            if (_burstR.enabled) Sky(_burstR, -100.0f, -100.0f, _burstAge, idle, seen, 1.0f);

            // Mayari and the shower, only while she is in the sky.
            bool her = up && !float.IsNaN(rel) && rel > -0.2f && rel < HoldSeconds + LeaveSeconds + 0.5f;
            _mayariR.enabled = her;
            _showerR.enabled = her && rel > ShowerFrom - 0.1f && rel < ShowerFrom + 22.0f;
            if (her) Sky(_mayariR, revealAge, leaveAge, 0.0f, idle, seen, 1.0f);
            if (_showerR.enabled) Sky(_showerR, revealAge, leaveAge, rel - ShowerFrom, idle, seen, 1.0f);
            if (_cometR.enabled) Sky(_cometR, -100.0f, -100.0f, 0.0f, idle, 1.0f, 1.0f);

            Ticks += System.Diagnostics.Stopwatch.GetTimestamp() - began;
            if (_countedFrame != Time.frameCount) { _countedFrame = Time.frameCount; Frames++; }
        }

        private void Sky(MeshRenderer r, float reveal, float leave, float oneOff, float clock, float seen, float bright)
        {
            r.GetPropertyBlock(_block);
            _block.SetVector(TId, new Vector4(reveal, leave, oneOff, clock));
            _block.SetFloat(SeenId, seen);
            _block.SetFloat(BrightId, bright);
            r.SetPropertyBlock(_block);
        }
    }
}
