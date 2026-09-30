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
    /// Authored by `IlalimSidewalkAuthor` (the routes, the looks, the props' materials, the sounds);
    /// the people are built here at Start, the LagoonResident way: a Classic rig from the roster,
    /// scaled by the cast's 2.38, dressed by `ToonSkin` with its own everyday palette.
    ///
    /// ⚠️⚠️ HOW THEY MOVE (owner 2026-10-01: "you should probably fix the animations for the walking
    /// and sitting of these characters", on a seated beggar whose legs were hidden and whose arms
    /// stuck out stiffly). The rig has seven rigid bones and no knee or elbow, so, exactly as the
    /// cast's own `CharacterAnimator.LocomotionArms` does, the walk and the run are DRAWN BONE BY
    /// BONE over the idle clip, not played from the shared `walk` and `sprint` clips:
    ///   * each body walks with a `GaitStyle`: the kids and the passers-by with their rig's own
    ///     entry in `GaitStyles` (Totoy, Bebang, Tikboy, Maring, Jun-Jun, Aling Nena), the
    ///     magtataho and the beggar with their own (<see cref="TahoGait"/>, <see cref="BeggarGait"/>);
    ///   * the cadence is the MEASURED ground speed (the body's own displacement, signed along its
    ///     facing) over the style's no-slide stride (`GaitStyle.CycleMetres` without its Glide), so
    ///     the planted foot does not slide, whether the body walks, is pushed aside, backs up or
    ///     steps round on the spot; the hips drop by the lift of the more vertical leg so both
    ///     soles meet the pavement (the cast's foot plant);
    ///   * turns ease in and out (a damped yaw), corners are rounded by a smoothed heading, and the
    ///     kids brake and re-accelerate through a juke instead of reversing in one frame.
    /// ⚠️⚠️ THE BEGGAR SITS WITH A DRAWN POSE, NOT THE `sit` CLIP. The clip was a chair sit: hips
    /// 6 cm up with the legs 15 degrees below level (so they sank into the pavement and needed a
    /// 7 cm lift) and the arms held out at 45 degrees. Here the legs rest level and a little apart
    /// on the carton, the hips at the height that puts the backs of the thighs ON it (never in
    /// it), a slight lean back toward the fence with the head bowed, the left hand resting on the
    /// pavement by the tin cup (its elevation solved each frame so the fist meets the ground) and
    /// the right forearm over his lap. He breathes, nods now and then and looks up at whoever
    /// passes. Sitting down and standing up are one continuous swing of the legs from hanging to
    /// level with the hips following the legs' lowest point (so the feet never leave the ground
    /// and never sink), and the bow and wave after a coin start and end in the seated pose.
    ///
    /// ⚠️⚠️ SCENERY, NEVER A PLAYER. No CharacterMotor, no collider (the rig's are destroyed), no
    /// network state: every client runs its own, like `KantoTraffic` and `LagoonFlocks`. They walk
    /// only the authored routes, which the author measured against the art and which never enter
    /// the play area (|x| &lt; 11.2, |z| &lt; 16.7), let alone the chalk box.
    ///   * KIDS: a game of tag on the south-east pavement, now and then (hidden between sessions).
    ///   * TAHO: the magtataho with his pole and two aluminium buckets, slow along the south-west
    ///     pavement, stopping to call. The call is `sfx_taho_call_1..3` (synthesised, see
    ///     `tools/synth_ilalim_life_sfx.py`) with the small comic popup ("TAHOOO!") kept.
    ///   * SPECTATORS: passers-by who walk to a spot at the court's edge (the pavement ends past
    ///     the end walls, the PGH lawn behind the fence), watch facing the court, cheer when the
    ///     can goes down and groan at a tag (<see cref="MatchFlair.Presented"/>), then walk on.
    ///   * BEGGAR: his OWN voxel model (npc-beggar.glb, `tools/build_beggar_voxel.py`: the
    ///     cast's pipeline on character-male-e's skeleton), not a cast rig. Walks in, lays his
    ///     carton down against the PGH fence just past the south wall and sits on it with a tin
    ///     cup, a tied bundle and a plastic bag beside him, within reach of a player at the wall.
    ///     While he sits the match HUD offers "Give a coin" on the Interact control
    ///     (<see cref="StreetInteractions"/>): a coin arcs into the cup and clinks, he bows and
    ///     waves from where he sits and murmurs "salamat po" (the popup says the same). ⚠️
    ///     COSMETIC: no currency, no score, no stat, nothing networked. After a while he stands,
    ///     picks up his things and leaves, and comes back later.
    /// THE SOUNDS (owner 2026-10-01: "can you add a "Tahoooooo" voice sfx for the taho guy?" and
    /// "as much as possible all these liveliness-adding character need sounds"): footsteps (the
    /// game's own `step_rubber`, pitched per person), the call, the kids' giggles and "Taya!", the
    /// watchers' cheers, claps and groans, the coin in the tin, the beggar's murmur, the carton, the
    /// buckets in step, and the pigeons' coos and wing flaps (<see cref="Pigeons"/>). All play on
    /// this component's own small pool of 3D voices, as `KantoStreetSound` plays the street: the
    /// same pause-menu SFX slider, ducked to nothing for a replay, logarithmic rolloff with short
    /// reaches, low priority and gains under the match's, and a cooldown on everything that could
    /// repeat. Sound randomness has its own `System.Random`, so the life's story is identical with
    /// or without it.
    /// People appear and vanish only at the far ends of their routes and only while that point
    /// is off the main camera's screen. <see cref="Simulate"/> is the whole step, so the builder's
    /// probe drives exactly what Play runs.
    /// </summary>
    public sealed class SidewalkLife : MonoBehaviour
    {
        [Serializable] public sealed class Look { public string Name; public RosterEntryAsset Art; public Color[] Palette; public float Scale = 1f; public Wear Wear = new Wear(); }

        /// <summary>
        /// Head-level and shoulder-level extras over a Classic rig, so a sidewalk person can stop
        /// reading as the cast member whose rig he borrows (owner 2026-09-30: the beggar "came out
        /// as Mang Kanor"). Chunky boxes in the rig's own units (the .glb's, before the cast's
        /// 2.38), each skinned rigidly to one bone like the rig's own parts, outlined by ToonSkin.
        /// All off by default: a Look saved before this existed wears nothing extra.
        /// </summary>
        [Serializable] public sealed class Wear
        {
            public HatKind Hat = HatKind.None;
            /// <summary>The top of the rig's hair (rig units), measured per rig by the author.</summary>
            public float HairTop = .67f;
            public Color HatColour = Color.grey, HatTrim = Color.grey;
            /// <summary>A bimpo (a small face towel) over the left shoulder.</summary>
            public bool Towel;
            public Color TowelColour = Color.white, TowelStripe = Color.grey;
            /// <summary>A stubble band on the jaw, below the mouth.</summary>
            public bool Stubble;
            public Color StubbleColour = Color.grey;
        }
        public enum HatKind { None, Cap, StrawHat }

        /// <summary>How the magtataho carries his pingga. Waist: the committed look (the pole at
        /// waist height on his right, a bucket before and behind). Shoulder: the real way, the
        /// pole balanced on his right shoulder front to back under the head's overhang, one hand
        /// steadying it, the buckets hanging at knee to hip height and swinging as he walks.</summary>
        public enum TahoCarryStyle { Waist, Shoulder }
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
        /// <summary>The beggar's belongings beside his carton: a tied cloth bundle and a plastic
        /// bag. Optional: a SidewalkLife authored without them (null) builds neither.</summary>
        public Material Bundle, BundleKnot, Bag;

        [Header("Sound (Art/audio/ambience; tools/synth_ilalim_life_sfx.py)")]
        public AudioClip[] TahoCall = new AudioClip[0];
        public AudioClip[] KidGiggle = new AudioClip[0], KidTaya = new AudioClip[0];
        public AudioClip[] Cheer = new AudioClip[0], Clap = new AudioClip[0], Groan = new AudioClip[0];
        public AudioClip[] Salamat = new AudioClip[0], CoinTin = new AudioClip[0], Carton = new AudioClip[0];
        public AudioClip[] Bucket = new AudioClip[0];
        public AudioClip[] PigeonCoo = new AudioClip[0], PigeonFlap = new AudioClip[0];
        /// <summary>The footstep: the game's own `step_rubber` (tsinelas on pavement).</summary>
        public AudioClip Footstep;
        /// <summary>The map's pigeon flock, read (never changed) for coos and flush flaps.</summary>
        public LagoonFlocks Pigeons;
        /// <summary>One knob over every life sound (1 is the authored balance).</summary>
        [Range(0f, 1f)] public float SoundGain = 1f;

        /// <summary>REVIEW ONLY (the author's filmed events): lets the TAHOOO and thank-you popups
        /// spawn outside Play, where the film steps <see cref="Simulate"/> by hand. Never set in
        /// the game; false keeps Play's behaviour exactly.</summary>
        public static bool FilmPopups;

        /// <summary>REVIEW ONLY: every sound the life starts is also written to <see cref="SoundLog"/>
        /// (the films mix their soundtracks from it). Never set in the game.</summary>
        public static bool RecordSounds;
        public struct Heard { public float Time; public string Clip; public Vector3 At; public float Gain, Pitch, Near, Far; }
        public static readonly List<Heard> SoundLog = new List<Heard>();

        [Header("Options awaiting the owner (defaults are the committed look)")]
        public TahoCarryStyle TahoCarry = TahoCarryStyle.Waist;

        [Header("Timing and pace")]
        public int Seed = 7;
        public Vector2 KidsFirst = new Vector2(20f, 35f), KidsPlay = new Vector2(45f, 75f), KidsAway = new Vector2(70f, 130f);
        public Vector2 TahoFirst = new Vector2(4f, 10f), TahoAway = new Vector2(45f, 90f), TahoStopEvery = new Vector2(6f, 11f), TahoStop = new Vector2(3f, 5.5f);
        public Vector2 SpectatorFirst = new Vector2(2f, 10f), SpectatorAway = new Vector2(6f, 22f), SpectatorWatch = new Vector2(14f, 32f);
        public Vector2 BeggarFirst = new Vector2(10f, 18f), BeggarSits = new Vector2(80f, 140f), BeggarAway = new Vector2(50f, 100f);
        public float WalkSpeed = 1.15f, TahoSpeed = .8f, BeggarSpeed = .75f, KidRun = 2.5f;
        public string DonateAction = "Give a coin";
        /// <summary>The coin's cue when no <see cref="CoinTin"/> clip is authored.</summary>
        public string CoinCue = "ui_click";

        // ------------------------------------------------------------------ constants

        private const float PersonScale = 2.38f, KeepRight = .35f, Fade = .22f, YokeHeight = .56f;
        /// <summary>The shoulder carry, measured off the rig times the cast's 2.38: the head's
        /// underside is at 0.816 m and overhangs 0.54 m either side; a hanging arm's top is its
        /// pivot, 0.685 m. So the pole (0.03 m radius) rides at 0.765 in the gap between the two,
        /// its top 2 cm under the head and 5 cm over the arms, just outboard of the torso.
        /// ⚠️ NO HAND ON THE POLE. A raised arm was tried (2026-09-30, the pole held at 0.74 with
        /// the right arm pitched up): these arms are 0.38 m thick, so the pole ran through the
        /// sleeve wherever the arm rose to meet it. The arms swing with the walk instead, and the
        /// pole balances on the shoulder as the vendors carry it on a long walk.</summary>
        private const float ShoulderPoleX = .40f, ShoulderPoleY = .765f;
        private static readonly string[] ClipNames = { "idle", "walk", "sprint", "sit", "emote-yes", "emote-no", "pick-up" };
        private const int Idle = 0, WalkClip = 1, SprintClip = 2, SitClip = 3, YesClip = 4, NoClip = 5, PickUpClip = 6;
        private static readonly bool[] Loops = { true, true, true, false, true, true, false };
        /// <summary>The thank-you, in the words of the murmur that plays with it (owner direction
        /// 2026-10-01: one line everywhere, matched to the voice).</summary>
        private const string Thanks = "SALAMAT PO!";

        // The beggar's seated pose, measured off npc-beggar.glb in its own units (before the 2.38).
        /// <summary>The leg block's back face behind the hip pivot (z -0.086 against the pivot's
        /// -0.029, 0.057) plus the 0.009 its lower corner rolls down when the level leg is splayed
        /// (measured offline on the mesh): with the legs level this is how high the pivot must
        /// ride for the thighs to rest ON the carton, not in it.</summary>
        private const float SeatLegBack = .066f;
        /// <summary>Shoulder pivot to fist end, and half the arm block's thickness.</summary>
        private const float SeatArmLength = .284f, SeatArmHalf = .062f;
        /// <summary>The carton's top above the pavement (BuildBeggarProps: 0.016 thick).</summary>
        private const float CartonTop = .016f;
        /// <summary>How the seated body is drawn (degrees): each leg level and this far out from
        /// straight ahead; the chest leaning back toward the fence; the head bowed; the right
        /// forearm over his lap (yaw in toward the middle, elevation below level); the left arm's
        /// yaw out toward the cup (its elevation is solved so the fist meets the pavement).</summary>
        private const float SeatLegSplay = 9f, SeatLean = -5f, SeatBow = 13f;
        private const float SeatLapYaw = -16f, SeatLapDrop = 16f, SeatCupYaw = 26f;
        /// <summary>The beggar's beats, seconds: turning to face out and laying the carton
        /// (the carton down at SettleCarton), then sitting (SitSeconds). Packing up: reaching for
        /// his bundle, standing (StandSeconds), then bending for the carton.</summary>
        private const float SettleBendFrom = .55f, SettleCarton = .8f, SitFrom = 1.3f, SitSeconds = 1.35f;
        private const float ReachSeconds = .8f, StandSeconds = 1.45f, PickSeconds = .7f;
        /// <summary>The seated thank-you: a bow, then a wave with the free hand, from the coin landing.</summary>
        private const float BowSeconds = 1.1f, WaveFrom = .55f, WaveSeconds = 1.5f;
        /// <summary>Metres a sidewalk corner is rounded over, either side (the smoothed heading).</summary>
        private const float Round = .6f;
        /// <summary>How fast a kid can change its run along the pavement (m/s per s): a juke brakes,
        /// plants and goes, instead of reversing in one frame.</summary>
        private const float KidAccel = 15f;

        /// <summary>
        /// THE MAGTATAHO'S WALK. A man carrying two full buckets on a pole over one shoulder all
        /// morning: short, even, unhurried steps, a little forward lean under the load, a slight
        /// roll onto each stance foot, the chest held square (a twist would swing the pole across
        /// the pavement), the right arm under the pole swinging less than the left, no bounce.
        /// </summary>
        internal static readonly GaitStyle TahoGait = new GaitStyle
        {
            Name = "magtataho",
            Walk = new Gait
            {
                LegForward = 30, LegBack = 28, LegSnap = .9f, Stance = 3,
                ArmSpread = 20, ArmForward = 16, ArmBack = 14, ArmCarry = 2, ArmSnap = .95f, ArmLag = .11f, ArmFavour = -.45f,
                Lean = 5, Roll = 2.5f, RollDelay = .04f, Twist = 1.5f, HeadPitch = -2, HeadSteady = .75f, Bounce = .008f, Sway = .035f, Glide = 1f,
            },
            Run = new Gait
            {
                LegForward = 36, LegBack = 34, LegSnap = .95f, Stance = 3,
                ArmSpread = 20, ArmForward = 22, ArmBack = 18, ArmCarry = 4, ArmSnap = 1, ArmLag = .1f, ArmFavour = -.45f,
                Lean = 8, Roll = 2, Twist = 2, HeadPitch = -2, HeadSteady = .75f, Bounce = .012f, Sway = .03f, Glide = 1f,
            },
        };

        /// <summary>
        /// THE BEGGAR'S WALK. An old, thin, tired man: small shuffling steps with the feet kept low,
        /// stooped forward with the head down, the arms hanging close and hardly swinging, a slow
        /// side-to-side sway over each foot.
        /// </summary>
        internal static readonly GaitStyle BeggarGait = new GaitStyle
        {
            Name = "beggar",
            Walk = new Gait
            {
                LegForward = 22, LegBack = 20, LegSnap = .85f, Stance = 4,
                ArmSpread = 15, ArmForward = 9, ArmBack = 7, ArmCarry = 6, ArmSnap = .9f, ArmLag = .12f,
                Lean = 9, Roll = 3, RollDelay = .06f, Twist = 2, HeadPitch = 9, HeadSteady = .35f, Bounce = .004f, Sway = .045f, Glide = 1f,
            },
            Run = new Gait
            {
                LegForward = 30, LegBack = 28, LegSnap = .9f, Stance = 4,
                ArmSpread = 16, ArmForward = 16, ArmBack = 12, ArmCarry = 8, ArmSnap = 1, ArmLag = .12f,
                Lean = 12, Roll = 2.5f, Twist = 3, HeadPitch = 6, HeadSteady = .4f, Bounce = .01f, Sway = .035f, Glide = 1f,
            },
        };

        // Walker phases.
        private const int Hidden = 0, Going = 1, Arrived = 2, Leaving = 3, Vanishing = 4, Settling = 5, Seated = 6, Rising = 7;

        // ------------------------------------------------------------------ runtime state

        private sealed class Body
        {
            public string Name, Role;
            public Transform Root, Torso, Head, ArmL, ArmR, LegL, LegR, RootBone;
            public bool ShoulderPole;
            public float Scale = 1f;
            public PlayableGraph Graph;
            public AnimationMixerPlayable Mixer;
            public readonly AnimationClip[] Clips = new AnimationClip[ClipNames.Length];
            public readonly float[] Weight = new float[ClipNames.Length];
            public int Clip = -1;
            public Vector3 Position, Heading;
            public float Yaw, YawVel;
            public bool Shown, Walking;
            public string State = "hidden";
            public float CheerUntil, ShakeUntil, BowUntil, LaughUntil, CallUntil, CallLength = 1.6f, MotionSpeed;
            public Transform Yoke;
            public Transform[] Swing = new Transform[0];

            // The drawn gait (see the class note).
            public readonly Dictionary<Transform, (Quaternion rotation, Vector3 position)> Bind = new Dictionary<Transform, (Quaternion, Vector3)>();
            public Vector3 AlongL = Vector3.left, AlongR = Vector3.right, LegAxisL = Vector3.down, LegAxisR = Vector3.down;
            /// <summary>Hip pivot to sole, world metres.</summary>
            public float Reach;
            public GaitStyle Gait;
            public float Phase, PhaseTotal, Cadence, Loco, RunW, RunTarget, Speed, AlongSpeed;
            public int Foot;
            public Vector3 LastPosition;
            public bool HasLast;
            public float StepGain = .16f, StepPitch = 1f;
            public float SwingPhase, SwingFree;

            // The seated pose (the beggar).
            public float SeatAmount, SeatAlpha, SeatLeanExtra, SeatArms, SeatReach, SeatPush;
            public float ThankAt = -99f;
            public float LookWeight, LookYaw, NodAt = -99f, NextNod, NextLook, LookUntil;
            public Body LookAt;
            public float JukeUntil, JukeSide;
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
            /// <summary>The way's direction at `s`, rounded over `r` either side: a corner turns
            /// over 2r of walking instead of in one step (the keep-right offset turns with it, so
            /// the body never jumps sideways at a corner).</summary>
            public Vector3 Heading(float s, float r)
            {
                At(s, out var tangent);
                if (P.Length < 2) return tangent;
                var d = At(Mathf.Min(Length, s + r), out _) - At(Mathf.Max(0f, s - r), out _); d.y = 0f;
                return d.sqrMagnitude > 1e-6f ? d.normalized : tangent;
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
            public float Along, Timer, NextStop, Lateral, Clock;
        }

        private sealed class Kid
        {
            public Body Body;
            public float Along, Lateral, LateralTarget, Pause, Burst, NextDart, Dir = -1f, Pace = 1f, Vel;
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
        private Transform _carton, _cup, _coin, _bundle, _bag;
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
        /// <summary>0 to 1: how much of the drawn walk is on the body (probes).</summary>
        public float PersonLocomotion(int i) => _all[i].Loco;
        /// <summary>The body's measured ground speed, m/s (probes).</summary>
        public float PersonSpeed(int i) => _all[i].Speed;
        public string PersonGait(int i) => _all[i].Gait != null ? _all[i].Gait.Name : "";
        /// <summary>
        /// The lower of the body's two soles, world position, as posed this frame (probes: a
        /// planted foot that moves along the ground is a foot that slides). False without legs.
        /// </summary>
        public bool PersonSole(int i, out Vector3 sole, out bool left)
        {
            sole = Vector3.zero; left = false;
            var b = _all[i];
            if (b.LegL == null || b.LegR == null || b.Reach <= 0f) return false;
            var l = b.LegL.position + b.LegL.TransformDirection(b.LegAxisL).normalized * b.Reach;
            var r = b.LegR.position + b.LegR.TransformDirection(b.LegAxisR).normalized * b.Reach;
            left = l.y <= r.y;
            sole = left ? l : r;
            return true;
        }
        /// <summary>The lowest point of the seated beggar's legs and hips above the carton top
        /// (probes: under zero is a leg in the carton). NaN unless he is seated.</summary>
        public float BeggarSeatClearance { get; private set; } = float.NaN;
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
            Voices();
        }

        private void OnDestroy()
        {
            foreach (var b in _all) if (b.Graph.IsValid()) b.Graph.Destroy();
            foreach (var m in _meshes.Values) if (m != null) Kill(m);
            foreach (var m in _tints.Values) if (m != null) Kill(m);
        }

        /// <summary>Builds the people and props once (Start, or the probe's first step).</summary>
        public void Begin()
        {
            if (_begun) return;
            _begun = true;
            _rng = new System.Random(Seed);
            _soundRng = new System.Random(Seed * 31 + 5);
            _lines = new Line[Walks.Length];
            for (int i = 0; i < Walks.Length; i++) _lines[i] = new Line(Walks[i]?.Points, Walks[i]?.Width);
            _track = new Line(KidTrack);

            if (Taho != null && Taho.Art != null && Valid(TahoWalk))
            {
                _taho = new Walker { Body = MakeBody(Taho, "Taho", "taho"), Line = TahoWalk, Timer = Range(TahoFirst) };
                _taho.Body.Gait = TahoGait; _taho.Body.StepGain = .17f; _taho.Body.StepPitch = .9f;
                BuildYoke(_taho.Body);
                _walkers.Add(_taho);
            }
            if (Beggar != null && Beggar.Art != null && Valid(BeggarWalk))
            {
                _beggar = new Walker { Body = MakeBody(Beggar, "Beggar", "beggar"), Line = BeggarWalk, Timer = Range(BeggarFirst) };
                _beggar.Body.Gait = BeggarGait; _beggar.Body.StepGain = .12f; _beggar.Body.StepPitch = .85f;
                BuildBeggarProps();
                _walkers.Add(_beggar);
            }
            for (int i = 0; i < Spectators.Length; i++)
            {
                if (Spectators[i] == null || Spectators[i].Art == null || Watches.Length == 0) continue;
                var w = new Walker { Body = MakeBody(Spectators[i], "Spectator " + i, "spectator"), Timer = Range(SpectatorFirst) + i * 3f };
                w.Body.StepPitch = .96f + .05f * i;
                _spectators.Add(w); _walkers.Add(w);
            }
            if (_track.P.Length >= 2)
                for (int i = 0; i < Kids.Length; i++)
                    if (Kids[i] != null && Kids[i].Art != null)
                    {
                        var k = new Kid { Body = MakeBody(Kids[i], "Kid " + i, "kid"), Pace = .93f + .07f * (i % 3) };
                        k.Body.StepGain = .12f; k.Body.StepPitch = 1.28f + .06f * i;
                        _kids.Add(k);
                    }
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
            // The rig's own walk for a cast rig; the magtataho and the beggar are given theirs in Begin.
            b.Gait = GaitStyles.For(look.Art.Model.name);
            // In the bind pose, before any clip moves a bone.
            Bones(b, model);
            if (look.Wear != null) Dress(b, look.Wear);

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

        /// <summary>
        /// The seven bones the visible skin is bound to (never a name search over every child),
        /// their bind pose, each limb's own axis and the leg's reach, the way the cast's
        /// `CharacterAnimator.ResolveSwingBones` and `CalibrateGait` measure them.
        /// </summary>
        private static void Bones(Body b, GameObject model)
        {
            foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (skin.bones == null) continue;
                var mesh = skin.sharedMesh;
                var binds = mesh != null ? mesh.bindposes : null;
                for (int i = 0; i < skin.bones.Length; i++)
                {
                    var bone = skin.bones[i];
                    if (bone == null) continue;
                    switch (bone.name)
                    {
                        case "arm-left": if (b.ArmL == null) { b.ArmL = bone; b.AlongL = AlongArm(binds, i, b.AlongL); } break;
                        case "arm-right": if (b.ArmR == null) { b.ArmR = bone; b.AlongR = AlongArm(binds, i, b.AlongR); } break;
                        case "leg-left":
                        case "leg-right":
                            // The reach from every skin that carries the leg (the head's skin does too,
                            // and reads negative), the bone from the first.
                            if (binds != null && i < binds.Length && mesh != null)
                            {
                                float local = binds[i].inverse.MultiplyPoint3x4(Vector3.zero).y - mesh.bounds.min.y;
                                b.Reach = Mathf.Max(b.Reach, local * skin.transform.TransformVector(Vector3.up).magnitude);
                            }
                            if (bone.name == "leg-left") { if (b.LegL == null) { b.LegL = bone; b.LegAxisL = DownLeg(binds, i); } }
                            else if (b.LegR == null) { b.LegR = bone; b.LegAxisR = DownLeg(binds, i); }
                            break;
                        case "torso": if (b.Torso == null) b.Torso = bone; break;
                        case "head": if (b.Head == null) b.Head = bone; break;
                    }
                }
            }
            if (b.Torso != null && b.Torso.parent != null && b.Torso.parent.name == "root") b.RootBone = b.Torso.parent;
            foreach (var bone in new[] { b.RootBone, b.Torso, b.Head, b.ArmL, b.ArmR, b.LegL, b.LegR })
                if (bone != null) b.Bind[bone] = (bone.localRotation, bone.localPosition);
            if (b.Reach < .05f || b.Reach > 1.5f) b.Reach = 0f;
        }

        /// <summary>The arm bone's local axis that runs shoulder to fist, measured (the importer mirrors X).</summary>
        private static Vector3 AlongArm(Matrix4x4[] binds, int index, Vector3 fallback)
        {
            if (binds == null || index >= binds.Length) return fallback;
            var boneToModel = binds[index].inverse;
            float side = boneToModel.MultiplyPoint3x4(Vector3.zero).x;
            float axis = boneToModel.MultiplyVector(Vector3.right).x;
            if (Mathf.Abs(side) < 1e-4f || Mathf.Abs(axis) < 1e-4f) return fallback;
            return Mathf.Sign(side) == Mathf.Sign(axis) ? Vector3.right : Vector3.left;
        }

        /// <summary>The leg bone's own axis that points at the model's floor in the bind pose.</summary>
        private static Vector3 DownLeg(Matrix4x4[] binds, int index)
        {
            if (binds == null || index >= binds.Length) return Vector3.down;
            var local = binds[index].MultiplyVector(Vector3.down);
            return local.sqrMagnitude > 1e-8f ? local.normalized : Vector3.down;
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
            b.HasLast = false; b.Speed = b.AlongSpeed = 0f; b.YawVel = 0f;
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
            Measure(b, dt);
            // The character frame first: every drawn layer below is built in it.
            b.Root.SetPositionAndRotation(b.Position, Quaternion.Euler(0f, b.Yaw, 0f));
            b.Graph.Evaluate(dt);
            Locomote(b, dt);
            DrawSeat(b, dt);
            Overlays(b);
            if (b.Yoke != null) Carry(b, dt);
        }

        /// <summary>The body's own ground speed from where it actually went this step, and the part
        /// of it along its facing (negative backing up). Smoothed a little: a sideways shove between
        /// two kids is a step, not a spike.</summary>
        private static void Measure(Body b, float dt)
        {
            var moved = b.HasLast ? b.Position - b.LastPosition : Vector3.zero;
            moved.y = 0f;
            b.LastPosition = b.Position; b.HasLast = true;
            if (dt <= 1e-5f) return;
            float k = 1f - Mathf.Exp(-dt / .08f);
            var facing = Quaternion.Euler(0f, b.Yaw, 0f) * Vector3.forward;
            b.Speed += (moved.magnitude / dt - b.Speed) * k;
            b.AlongSpeed += (Vector3.Dot(moved, facing) / dt - b.AlongSpeed) * k;
        }

        // ------------------------------------------------------------------ the drawn walk

        /// <summary>
        /// The walk and the run, drawn from the body's `GaitStyle` over whatever the clip left
        /// (the cast's `ApplyLocomotionArms`, minus the match's carry and fatigue). The cadence is
        /// the measured speed over the no-slide stride. Turning on the spot steps too (a quarter
        /// metre of foot travel per radian), so nobody pivots on frozen feet.
        /// </summary>
        private void Locomote(Body b, float dt)
        {
            if (b.Gait == null || b.LegL == null || b.LegR == null || b.ArmL == null || b.ArmR == null || b.Reach <= 0f) return;
            float turning = Mathf.Abs(b.YawVel) * Mathf.Deg2Rad * .25f;
            float moving = Mathf.Max(b.Speed, turning);
            float target = b.SeatAmount > 0f ? 0f : Mathf.Clamp01((moving - .06f) / .3f);
            b.Loco = Mathf.MoveTowards(b.Loco, target, dt / (target > b.Loco ? .15f : .22f));
            b.RunW = Mathf.MoveTowards(b.RunW, b.RunTarget, dt / .2f);
            if (b.Loco <= .001f) { b.Cadence = 0f; return; }

            var g = Gait.Lerp(b.Gait.Walk, b.Gait.Run, b.RunW);
            float stride = b.Gait.CycleMetres(b.Reach, b.RunW) / Mathf.Max(1f, g.Glide);
            float along = Mathf.Abs(b.AlongSpeed) > turning ? b.AlongSpeed : turning;
            b.Cadence = along / Mathf.Max(.1f, stride);
            b.PhaseTotal += b.Cadence * dt;
            b.Phase = Mathf.Repeat(b.PhaseTotal, 1f);
            // A footfall at a quarter and three quarters of the cycle (GaitStyle's "passing" at 0 and .5).
            int foot = Mathf.FloorToInt(b.PhaseTotal * 2f - .5f);
            if (foot != b.Foot)
            {
                b.Foot = foot;
                if (b.Loco > .6f) Footfall(b);
            }

            var pose = b.Gait.Evaluate(b.Phase, b.RunW, _clock, Mathf.Abs(b.Cadence));
            float amount = b.Loco;
            ToBind(b, b.RootBone, amount, true);
            ToBind(b, b.Torso, amount, false);
            ToBind(b, b.Head, amount, false);
            var right = b.Root.right; var forward = b.Root.forward; var up = b.Root.up;
            if (b.Torso != null)
                b.Torso.rotation = Quaternion.AngleAxis(pose.TorsoPitch * amount, right) * Quaternion.AngleAxis(pose.TorsoRoll * amount, forward)
                                   * Quaternion.AngleAxis(pose.TorsoYaw * amount, up) * b.Torso.rotation;
            if (b.Head != null)
                b.Head.rotation = Quaternion.AngleAxis(pose.HeadPitch * amount, right) * Quaternion.AngleAxis(pose.HeadRoll * amount, forward)
                                  * Quaternion.AngleAxis(pose.HeadYaw * amount, up) * b.Head.rotation;
            // Sides by POSITION, not by bone name: the importer mirrors X.
            float sal = SideOf(b, b.ArmL, -1f), sar = SideOf(b, b.ArmR, 1f), sll = SideOf(b, b.LegL, -1f), slr = SideOf(b, b.LegR, 1f);
            Aim(b, b.ArmL, b.AlongL, Limb(sal, sal < 0 ? pose.ArmLeft : pose.ArmRight, sal < 0 ? pose.SpreadLeft : pose.SpreadRight), amount);
            Aim(b, b.ArmR, b.AlongR, Limb(sar, sar < 0 ? pose.ArmLeft : pose.ArmRight, sar < 0 ? pose.SpreadLeft : pose.SpreadRight), amount);
            Aim(b, b.LegL, b.LegAxisL, Limb(sll, sll < 0 ? pose.LegLeft : pose.LegRight, sll < 0 ? pose.SplayLeft : pose.SplayRight), amount);
            Aim(b, b.LegR, b.LegAxisR, Limb(slr, slr < 0 ? pose.LegLeft : pose.LegRight, slr < 0 ? pose.SplayLeft : pose.SplayRight), amount);
            // The foot plant: the hips come down by the lift of the more vertical leg, so a sole is on the pavement.
            if (b.RootBone != null)
            {
                float lift = Mathf.Min(SoleLift(b, b.LegL, b.LegAxisL), SoleLift(b, b.LegR, b.LegAxisR));
                b.RootBone.position += up * (pose.RootUp * b.Reach * amount - lift) + right * (pose.RootRight * b.Reach * amount);
            }
        }

        private static void ToBind(Body b, Transform bone, float amount, bool position)
        {
            if (bone == null || !b.Bind.TryGetValue(bone, out var rest)) return;
            bone.localRotation = Quaternion.Slerp(bone.localRotation, rest.rotation, amount);
            if (position) bone.localPosition = Vector3.Lerp(bone.localPosition, rest.position, amount);
        }

        /// <summary>Which side of the body a bone is on, from where it actually sits (-1 left, +1 right).</summary>
        private static float SideOf(Body b, Transform bone, float fallback)
        {
            float x = b.Root.InverseTransformPoint(bone.position).x;
            return Mathf.Abs(x) < 1e-3f ? fallback : Mathf.Sign(x);
        }

        /// <summary>A limb's direction in the character frame: `outward` degrees off the centre
        /// line, then `forward` degrees ahead of hanging (the cast's PoseLimb).</summary>
        private static Vector3 Limb(float side, float forward, float outward)
        {
            float o = outward * Mathf.Deg2Rad;
            return Quaternion.AngleAxis(-forward, Vector3.right) * new Vector3(side * Mathf.Sin(o), -Mathf.Cos(o), 0f);
        }

        /// <summary>A limb's direction in the character frame from a heading `yaw` degrees out from
        /// straight ahead (toward the limb's own side) and `drop` degrees below level.</summary>
        private static Vector3 Reaching(float side, float yaw, float drop)
        {
            float y = yaw * Mathf.Deg2Rad, e = drop * Mathf.Deg2Rad;
            return new Vector3(side * Mathf.Cos(e) * Mathf.Sin(y), -Mathf.Sin(e), Mathf.Cos(e) * Mathf.Cos(y));
        }

        /// <summary>Turns a limb so its axis points along `direction` (character frame), by the
        /// smallest rotation from where it is, so its twist stays and its pivot never moves.</summary>
        private static void Aim(Body b, Transform limb, Vector3 axis, Vector3 direction, float amount)
        {
            if (limb == null || amount <= 0f) return;
            var desired = b.Root.TransformDirection(direction);
            var current = limb.TransformDirection(axis);
            var target = Quaternion.FromToRotation(current, desired) * limb.rotation;
            limb.rotation = Quaternion.Slerp(limb.rotation, target, Mathf.Clamp01(amount));
        }

        /// <summary>How far above its hip's rest height's floor this leg's sole sits, world metres.</summary>
        private static float SoleLift(Body b, Transform leg, Vector3 axis)
        {
            var down = b.Root.InverseTransformDirection(leg.TransformDirection(axis)).normalized;
            return b.Reach * (1f - Mathf.Clamp01(-down.y));
        }

        private void Walking(Body b, float speed, bool run)
        {
            b.MotionSpeed = speed;
            b.Walking = speed > .05f;
            b.RunTarget = run ? 1f : 0f;
            // Under the drawn walk the clip is the idle: the walk replaces every bone while it
            // moves, and the idle is what it hands back to when it stops.
            Play(b, Idle, 1f);
        }

        /// <summary>Turns toward `direction`, easing in and out (a damped yaw) and never faster
        /// than `turn` degrees a second.</summary>
        private void Face(Body b, Vector3 direction, float dt, float turn = 300f, float ease = .11f)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 1e-6f || dt <= 0f) return;
            float target = Quaternion.LookRotation(direction.normalized, Vector3.up).eulerAngles.y;
            b.Yaw = Mathf.SmoothDampAngle(b.Yaw, target, ref b.YawVel, ease, turn, dt);
        }

        // ------------------------------------------------------------------ the seated pose

        /// <summary>
        /// The beggar's sitting, drawn (see the class note). `SeatAlpha` is the legs' swing, 0
        /// hanging to 90 level; the hips ride at the height that puts the legs' lowest point on the
        /// ground (on the carton once it is under him), so the feet slide forward along the
        /// pavement as he lowers himself and never lift or sink. `SeatAmount` blends the whole
        /// layer in and out at the standing ends, where it matches the idle.
        /// </summary>
        private void DrawSeat(Body b, float dt)
        {
            if (_beggar == null || b != _beggar.Body) return;
            BeggarSeatClearance = float.NaN;
            if (b.SeatAmount <= 0f || b.LegL == null || b.LegR == null || b.RootBone == null || b.Torso == null) return;
            float a = Mathf.Clamp01(b.SeatAmount), s = PersonScale * b.Scale;
            float alpha = Mathf.Clamp(b.SeatAlpha, 0f, 90f), u = alpha / 90f, rad = alpha * Mathf.Deg2Rad;
            var right = b.Root.right; var forward = b.Root.forward; var up = b.Root.up;

            ToBind(b, b.RootBone, a, true);
            ToBind(b, b.Torso, a, false);
            ToBind(b, b.Head, a, false);

            // The hips: the swung legs' lowest point (the hip end's back edge once they rise past
            // about 18 degrees) on the ground, and on the carton's top as he comes down onto it.
            float hip = b.Reach * Mathf.Cos(rad) + SeatLegBack * s * Mathf.Sin(rad) + CartonTop * Smooth(u);
            b.RootBone.position += up * ((hip - b.Reach) * a);

            // Breathing (seated only), a nod now and then, a look up at whoever passes.
            float seated = Smooth(Mathf.InverseLerp(.85f, 1f, u));
            float breath = Mathf.Sin(_clock * 2f * Mathf.PI * .23f) * seated;
            SeatLook(b, dt, seated);
            float nod = 0f;
            if (_clock - b.NodAt < 1f) nod = Mathf.Sin(Mathf.Clamp01(_clock - b.NodAt) * Mathf.PI) * 9f;

            // The thank-you after a coin: a bow, then a wave with the free hand; both from and back to this pose.
            float bowAge = _clock - b.ThankAt;
            float bow = bowAge >= 0f && bowAge < BowSeconds ? Mathf.Sin(bowAge / BowSeconds * Mathf.PI) : 0f;
            float waveAge = bowAge - WaveFrom;
            float wave = waveAge >= 0f && waveAge < WaveSeconds ? Mathf.Clamp01(waveAge / .25f) * Mathf.Clamp01((WaveSeconds - waveAge) / .35f) : 0f;
            wave = Smooth(wave);

            float lean = SeatLean * u + b.SeatLeanExtra + breath * 1.3f + bow * 14f + b.SeatReach * 16f;
            float roll = b.SeatReach * 10f;
            float lookYaw = b.LookYaw * b.LookWeight;
            b.Torso.rotation = Quaternion.AngleAxis(lookYaw * .25f, up) * Quaternion.AngleAxis(lean * a, right)
                               * Quaternion.AngleAxis(-roll * a, forward) * b.Torso.rotation;
            if (b.Head != null)
            {
                float headPitch = SeatBow * u - b.LookWeight * (SeatBow + 5f) + nod + bow * 6f - breath * .6f;
                b.Head.rotation = Quaternion.AngleAxis(lookYaw * .75f, up) * Quaternion.AngleAxis(headPitch * a, right) * b.Head.rotation;
            }

            // The legs: level (at 90) and a little apart; hanging at 0.
            float sll = SideOf(b, b.LegL, -1f), slr = SideOf(b, b.LegR, 1f);
            float legDrop = 90f - alpha;
            Aim(b, b.LegL, b.LegAxisL, Reaching(sll, SeatLegSplay * u, legDrop), a);
            Aim(b, b.LegR, b.LegAxisR, Reaching(slr, SeatLegSplay * u, legDrop), a);

            // The arms: at rest, the left (cup side) hand on the pavement, the right forearm over his
            // lap; while he lowers himself or gets up, both hands go down to the ground beside him
            // (SeatPush); reaching for his bundle, the right arm goes out to it (SeatReach).
            if (b.ArmL == null || b.ArmR == null) return;
            float armLength = SeatArmLength * s, armHalf = SeatArmHalf * s, ground = b.Position.y + .004f;
            float Drop(Transform arm)
            {
                float sin = (arm.position.y - ground - armHalf) / armLength;
                return Mathf.Asin(Mathf.Clamp(sin, -.3f, 1f)) * Mathf.Rad2Deg;
            }
            float arms = Mathf.Clamp01(b.SeatArms) * a;
            foreach (var arm in new[] { b.ArmL, b.ArmR })
            {
                var axis = arm == b.ArmL ? b.AlongL : b.AlongR;
                float side = SideOf(b, arm, arm == b.ArmL ? -1f : 1f);
                Vector3 rest = side < 0 ? Reaching(side, SeatCupYaw, Drop(arm)) : Reaching(side, SeatLapYaw, SeatLapDrop);
                Vector3 push = Reaching(side, 40f, Drop(arm));
                var dir = Vector3.Slerp(rest, push, Mathf.Clamp01(b.SeatPush));
                if (side > 0 && b.SeatReach > 0f) dir = Vector3.Slerp(dir, Reaching(side, 72f, 30f), b.SeatReach);
                if (side > 0 && wave > 0f)
                    dir = Vector3.Slerp(dir, Reaching(side, 62f + Mathf.Sin(waveAge * 2f * Mathf.PI * 2.4f) * 14f, -38f), wave);
                dir.y -= breath * .012f;
                Aim(b, arm, axis, dir.normalized, arms);
            }

            if (alpha > 80f && _beggar.Phase == Seated) BeggarSeatClearance = Clearance(b);
        }

        /// <summary>The lowest point of the drawn legs (their back face) above the carton's top,
        /// world metres: the check that nothing sinks into it.</summary>
        private float Clearance(Body b)
        {
            float s = PersonScale * b.Scale, low = float.MaxValue;
            foreach (var (leg, axis) in new[] { (b.LegL, b.LegAxisL), (b.LegR, b.LegAxisR) })
            {
                // The leg's back face, the side that faces down once the leg is level.
                var along = leg.TransformDirection(axis).normalized;
                var under = Vector3.ProjectOnPlane(Vector3.down, along);
                under = under.sqrMagnitude > 1e-6f ? under.normalized : Vector3.down;
                foreach (float t in new[] { 0f, 1f })
                    low = Mathf.Min(low, (leg.position + along * b.Reach * t + under * SeatLegBack * s).y);
            }
            return low - (b.Position.y + CartonTop);
        }

        private void SeatLook(Body b, float dt, float seated)
        {
            if (seated <= 0f) { b.LookWeight = Mathf.MoveTowards(b.LookWeight, 0f, dt * 3f); return; }
            if (_clock >= b.NextLook && _clock >= b.LookUntil)
            {
                b.LookAt = null;
                float best = 36f;
                foreach (var o in _all)
                {
                    if (o == b || !o.Shown) continue;
                    var d = o.Position - b.Position; d.y = 0f;
                    if (d.sqrMagnitude < best && Vector3.Dot(d, b.Root.forward) > -.3f * d.magnitude) { best = d.sqrMagnitude; b.LookAt = o; }
                }
                if (b.LookAt != null) { b.LookUntil = _clock + 2.2f + Rand() * 1.8f; b.NextLook = b.LookUntil + 3f + Rand() * 4f; }
                else b.NextLook = _clock + .5f;
            }
            Vector3? at = null;
            if (b.LookAt != null && b.LookAt.Shown && _clock < b.LookUntil) at = b.LookAt.Position;
            if (_local != null && Application.isPlaying)
            {
                var d = _local.transform.position - b.Position; d.y = 0f;
                if (d.sqrMagnitude < 16f) at = _local.transform.position;
            }
            float want = 0f;
            if (at.HasValue)
            {
                var d = at.Value - b.Position; d.y = 0f;
                float yaw = Vector3.SignedAngle(b.Root.forward, d, Vector3.up);
                if (Mathf.Abs(yaw) < 100f) { want = seated; b.LookYaw = Mathf.MoveTowardsAngle(b.LookYaw, Mathf.Clamp(yaw, -55f, 55f), dt * 120f); }
            }
            b.LookWeight = Mathf.MoveTowards(b.LookWeight, want, dt / .45f);
            if (b.LookWeight < .05f && _clock >= b.NextNod)
            {
                b.NodAt = _clock; b.NextNod = _clock + 6.5f + Rand() * 6f;
            }
        }

        private static float Smooth(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }

        // ------------------------------------------------------------------ overlays

        /// <summary>The procedural beats over the drawn pose, each easing in from and out to it.</summary>
        private void Overlays(Body b)
        {
            if (b.ArmL != null && _clock < b.CheerUntil)
            {
                // An arm thrown up and shaken (LagoonResident's cheer), blended from wherever the arm is.
                float age = 1.4f - (b.CheerUntil - _clock), env = Mathf.Sin(Mathf.Clamp01(age / 1.4f) * Mathf.PI);
                float side = SideOf(b, b.ArmL, -1f);
                Aim(b, b.ArmL, b.AlongL, Reaching(side, 24f + Mathf.Sin(age * 13f) * 12f, -68f), env);
            }
            if (b.Torso != null && _clock < b.BowUntil && b.SeatAmount <= 0f)
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
                float hop = Mathf.Abs(Mathf.Sin(age * 9.4f)) * .07f * b.Scale * env;
                b.Root.position += Vector3.up * hop;
                if (b.Torso != null) b.Torso.localRotation *= Quaternion.Euler(0f, 0f, Mathf.Sin(age * 22f) * 7f * env);
                if (b.Head != null) b.Head.localRotation *= Quaternion.Euler(10f * env, 0f, 0f);
            }
            if (_clock < b.JukeUntil && b.Torso != null)
            {
                // A juke: the kid leans back against the brake and into the turn, then goes.
                float age = .4f - (b.JukeUntil - _clock), env = Mathf.Sin(Mathf.Clamp01(age / .4f) * Mathf.PI);
                b.Torso.rotation = Quaternion.AngleAxis(-12f * env, b.Root.right) * Quaternion.AngleAxis(10f * env * b.JukeSide, b.Root.forward) * b.Torso.rotation;
            }
            if (b.Head != null && _clock < b.CallUntil)
            {
                float env = Mathf.Sin(Mathf.Clamp01(1f - (b.CallUntil - _clock) / b.CallLength) * Mathf.PI);
                // The head lifts to call (a smaller lift under a shoulder pole: the head's overhang sits 2 cm above it).
                b.Head.rotation = Quaternion.AngleAxis(-(b.ShoulderPole ? 2f : 12f) * env, b.Root.right) * b.Head.rotation;
            }
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

        private readonly Dictionary<Color, Material> _tints = new Dictionary<Color, Material>();

        /// <summary>A runtime copy of the carton's material in `colour` (never saved as an asset).</summary>
        private Material Tint(Color colour)
        {
            if (_tints.TryGetValue(colour, out var m) && m != null) return m;
            m = Cardboard != null ? new Material(Cardboard) : new Material(Shader.Find("Standard"));
            m.name = "Sidewalk wear #" + ColorUtility.ToHtmlStringRGB(colour);
            m.color = colour;
            m.SetFloat("_Glossiness", .05f);
            return _tints[colour] = m;
        }

        /// <summary>
        /// Puts the Look's <see cref="Wear"/> on a body in its bind pose. Each piece is a box
        /// given in the rig's own units in the .glb's frame (x toward the character's LEFT, z
        /// toward the face), placed under the root and then handed to its bone with its world
        /// pose kept, so the clip carries it.
        /// </summary>
        private void Dress(Body b, Wear wear)
        {
            if (wear.Hat == HatKind.None && !wear.Towel && !wear.Stubble) return;
            var cube = Builtin("Cube.fbx");
            float s = PersonScale * b.Scale;
            Transform Box(Transform bone, string name, Color colour, Vector3 centre, Vector3 size, Vector3 euler = default)
            {
                var t = Part(b.Root, name, cube, Tint(colour), new Vector3(-centre.x, centre.y, centre.z) * s, size * s, euler);
                if (bone != null) t.SetParent(bone, true);
                return t;
            }
            float top = wear.HairTop;
            switch (wear.Hat)
            {
                case HatKind.Cap:
                    // A plain worn cap: the crown over the hair, the peak straight out over the face.
                    Box(b.Head, "Cap", wear.HatColour, new Vector3(0f, top - .016f, -.004f), new Vector3(.372f, .078f, .364f));
                    Box(b.Head, "Cap peak", wear.HatTrim, new Vector3(0f, top - .046f, .222f), new Vector3(.30f, .022f, .13f), new Vector3(6f, 0f, 0f));
                    break;
                case HatKind.StrawHat:
                    // A boxy buri hat: a wide brim, a crown and a dark band.
                    Box(b.Head, "Hat brim", wear.HatColour, new Vector3(0f, top - .064f, 0f), new Vector3(.56f, .026f, .54f));
                    Box(b.Head, "Hat crown", wear.HatColour, new Vector3(0f, top - .004f, -.002f), new Vector3(.352f, .11f, .352f));
                    Box(b.Head, "Hat band", wear.HatTrim, new Vector3(0f, top - .036f, -.002f), new Vector3(.362f, .03f, .362f));
                    break;
            }
            if (wear.Towel)
            {
                // The bimpo over his left shoulder: a flap down the chest, one down the back, a
                // stripe across the front flap's end; the fold on top hides under the head.
                Box(b.Torso, "Towel front", wear.TowelColour, new Vector3(.085f, .292f, .1f), new Vector3(.105f, .15f, .024f));
                Box(b.Torso, "Towel stripe", wear.TowelStripe, new Vector3(.085f, .238f, .1f), new Vector3(.109f, .028f, .028f));
                Box(b.Torso, "Towel back", wear.TowelColour, new Vector3(.085f, .292f, -.152f), new Vector3(.105f, .15f, .024f));
                Box(b.Torso, "Towel fold", wear.TowelColour, new Vector3(.085f, .356f, -.026f), new Vector3(.105f, .03f, .276f));
            }
            if (wear.Stubble)
                Box(b.Head, "Stubble", wear.StubbleColour, new Vector3(0f, .372f, .163f), new Vector3(.27f, .052f, .012f));
        }

        /// <summary>The pingga (the bamboo pole) at his waist, a bucket hung at each end.
        /// Built at the root, not a bone, so its pose is independent of the clip.</summary>
        private void BuildYoke(Body b)
        {
            if (TahoCarry == TahoCarryStyle.Shoulder) { BuildShoulderPole(b); return; }
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

        /// <summary>The real carry (the default since 2026-09-30): the pingga balanced on his right
        /// shoulder, running front to back. The cast's heads overhang the shoulders (the head's
        /// underside is 0.816 m up at the cast's scale, 0.54 m either side), so the pole rides just
        /// under that rim and just outboard of the torso, over the top of the hanging arm (see
        /// ShoulderPoleY). One bucket hangs in front and one behind on a short chunky rod, their
        /// bottoms at knee height and lids at the hip.</summary>
        private void BuildShoulderPole(Body b)
        {
            var cylinder = Builtin("Cylinder.fbx");
            var yoke = Part(b.Root, "Yoke", null, null, new Vector3(ShoulderPoleX, ShoulderPoleY, 0f), Vector3.one, Vector3.zero);
            Part(yoke, "Pole", cylinder, Bamboo, Vector3.zero, new Vector3(.06f, .76f, .06f), new Vector3(90f, 0f, 0f));
            var swing = new List<Transform>();
            foreach (float end in new[] { -.62f, .62f })
            {
                var pivot = Part(yoke, end < 0 ? "Back" : "Front", null, null, new Vector3(0f, 0f, end), Vector3.one, Vector3.zero);
                Part(pivot, "Rod", cylinder, Rope, new Vector3(0f, -.13f, 0f), new Vector3(.035f, .10f, .035f), Vector3.zero);
                Part(pivot, "Lid", cylinder, Lid, new Vector3(0f, -.245f, 0f), new Vector3(.32f, .012f, .32f), Vector3.zero);
                Part(pivot, "Bucket", cylinder, Aluminium, new Vector3(0f, -.395f, 0f), new Vector3(.30f, .14f, .30f), Vector3.zero);
                swing.Add(pivot);
            }
            b.Yoke = yoke;
            b.Swing = swing.ToArray();
            b.ShoulderPole = true;
            // Carried BY THE SHOULDERS: on the torso bone (still in its bind pose here), so the
            // walk's bob and sway move the pole with the head above it and the 2 cm between them hold.
            if (b.Torso != null) yoke.SetParent(b.Torso, true);
        }

        /// <summary>
        /// The buckets IN STEP: while he walks they swing fore and aft once a step, a little behind
        /// the footfall (the pole flexes as the weight lands), and roll with the stride; when he
        /// stops they swing on by themselves at a hanging bucket's own rate and settle.
        /// </summary>
        private void Carry(Body b, float dt)
        {
            float walking = b.Loco;
            // After a walk the free swing starts at the walk's size and dies away over about 2 s.
            b.SwingFree = walking > .5f ? 1f : Mathf.MoveTowards(b.SwingFree, 0f, dt / 2.2f);
            b.SwingPhase += dt * 1.15f;
            float free = Mathf.Sin(b.SwingPhase * 2f * Mathf.PI) * b.SwingFree * (1f - walking);
            float step = Mathf.Sin((2f * b.Phase - .12f) * 2f * Mathf.PI) * walking;
            float roll = Mathf.Sin((b.Phase - .08f) * 2f * Mathf.PI) * walking;
            if (b.ShoulderPole)
            {
                // Front and back swing opposite ways along the walk, a little sideways roll on top.
                for (int i = 0; i < b.Swing.Length; i++)
                {
                    float sign = i == 0 ? 1f : -1f;
                    b.Swing[i].localRotation = Quaternion.Euler((step * 7f + free * 5f) * sign, 0f, roll * 3f);
                }
                return;
            }
            var p = b.Yoke.localPosition;
            p.y = YokeHeight + Mathf.Sin(b.Phase * 4f * Mathf.PI) * .012f * walking;
            b.Yoke.localPosition = p;
            for (int i = 0; i < b.Swing.Length; i++)
                b.Swing[i].localRotation = Quaternion.Euler(step * 5f + free * 4f, 0f, 0f);
        }

        private void BuildBeggarProps()
        {
            var f = BeggarFacing; f.y = 0f; f = f.sqrMagnitude > 1e-4f ? f.normalized : Vector3.right;
            var holder = new GameObject("Beggar's things").transform;
            holder.SetParent(transform, false);
            holder.SetPositionAndRotation(BeggarSeat, Quaternion.LookRotation(f, Vector3.up));
            var cube = Builtin("Cube.fbx"); var cylinder = Builtin("Cylinder.fbx");
            _carton = Part(holder, "Carton", cube, Cardboard, new Vector3(0f, .008f, .12f), new Vector3(.62f, .016f, .72f), Vector3.zero);
            // The cup on his left, the side toward the court and the players at the wall, just
            // inside where his left hand rests on the pavement.
            _cup = Part(holder, "Tin cup", cylinder, Tin, new Vector3(-.26f, .05f, .62f), new Vector3(.11f, .05f, .11f), Vector3.zero);
            _coin = Part(transform, "Coin", cylinder, Coin, Vector3.zero, new Vector3(.05f, .004f, .05f), Vector3.zero);
            _carton.gameObject.SetActive(false); _cup.gameObject.SetActive(false); _coin.gameObject.SetActive(false);
            // His things on his right, off the carton's edge: a soft tied bundle and a plastic bag.
            var sphere = Builtin("Sphere.fbx");
            if (Bundle != null)
            {
                // A cloth tied at the top: the two ears of the knot stand up out of it.
                _bundle = Part(holder, "Bundle", sphere, Bundle, new Vector3(.74f, .09f, .06f), new Vector3(.28f, .18f, .24f), new Vector3(0f, 18f, 0f));
                if (BundleKnot != null)
                {
                    Part(_bundle, "Knot", sphere, BundleKnot, new Vector3(0f, .56f, 0f), new Vector3(.4f, .45f, .42f), Vector3.zero);
                    Part(_bundle, "Knot ear", sphere, BundleKnot, new Vector3(-.17f, .95f, 0f), new Vector3(.24f, .75f, .28f), new Vector3(0f, 0f, 32f));
                    Part(_bundle, "Knot ear", sphere, BundleKnot, new Vector3(.18f, .92f, .03f), new Vector3(.24f, .72f, .28f), new Vector3(0f, 0f, -30f));
                }
                _bundle.gameObject.SetActive(false);
            }
            if (Bag != null)
            {
                _bag = Part(holder, "Plastic bag", sphere, Bag, new Vector3(.66f, .115f, -.24f), new Vector3(.19f, .23f, .14f), new Vector3(0f, -24f, 4f));
                Part(_bag, "Tied handles", sphere, Bag, new Vector3(0f, .52f, 0f), new Vector3(.42f, .3f, .36f), Vector3.zero);
                _bag.gameObject.SetActive(false);
            }
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
            StepLater();
            StepPigeons(dt);
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
        /// stopping end) and waiting behind anybody standing in its way. The keep-right offset
        /// and the facing follow the ROUNDED heading, so a corner is walked round, not snapped.</summary>
        private void Advance(Walker w, float speed, float dt)
        {
            var line = _lines[w.Line];
            var b = w.Body;
            float ease = Mathf.Clamp01((line.Length - w.Along) / 1.5f);
            w.Lateral = KeepRight * w.Dir * ease * line.WidthAt(w.Along);
            var heading = line.Heading(w.Along, Round) * w.Dir;
            b.Heading = heading;
            if (Blocked(b, b.Position, heading)) speed = 0f;
            w.Along = Mathf.Clamp(w.Along + speed * w.Dir * dt, 0f, line.Length);
            var here = line.At(w.Along, out _);
            var tangent = line.Heading(w.Along, Round);
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
            var tangent = line.Heading(0f, Round);
            w.Body.Position = line.At(0f, out _) + Vector3.Cross(Vector3.up, tangent) * KeepRight * line.WidthAt(0f);
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

        /// <summary>The call: "Ta-hoooooo!" from where he stands (its length sets the head's lift),
        /// with the comic popup.</summary>
        private void Call(Body b)
        {
            var clip = Pick(TahoCall);
            b.CallLength = clip != null ? Mathf.Clamp(clip.length, 1f, 3f) : 1.6f;
            b.CallUntil = _clock + b.CallLength;
            Sound(clip, b.Position + Vector3.up * 1.55f, .62f, 4f, 45f, .98f + Rand() * .04f, b);
            if (!Application.isPlaying && !FilmPopups) return;
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
                    Face(b, Watches[w.Watch].LookAt - b.Position, dt, 160f, .25f);
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
                if (kind == MatchFlair.Kind.LataDown)
                {
                    b.CheerUntil = _clock + 1.4f + lag; b.LaughUntil = _clock + 1.2f + lag;
                    Later(lag + .05f, b, Cheer, .5f, 2f, 30f);
                    if (Rand() < .7f) Later(lag + .5f, b, Clap, .34f, 2f, 25f);
                }
                else
                {
                    b.ShakeUntil = _clock + 1.2f + lag;
                    if (Rand() < .8f) Later(lag + .1f, b, Groan, .42f, 2f, 26f);
                }
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
                    if (w.Along >= _lines[w.Line].Length - .01f) { w.Phase = Settling; w.Clock = 0f; b.State = "sitting down"; }
                    return;
                case Settling:
                {
                    // Turn to face the street, bend and lay the carton down, then sit on it.
                    float t = w.Clock += dt;
                    Walking(b, 0f, false);
                    b.Position = Vector3.MoveTowards(b.Position, BeggarSeat, dt * .6f);
                    Face(b, facing, dt, 200f, .16f);
                    bool bending = t >= SettleBendFrom && t < SitFrom - .15f;
                    Play(b, bending ? PickUpClip : Idle, .6f);
                    if (t >= SettleCarton && t - dt < SettleCarton) { SetProps(true); Sound(Pick(Carton), BeggarSeat, .32f, 1.5f, 12f, 1f, null); }
                    float u = Mathf.Clamp01((t - SitFrom) / SitSeconds);
                    b.SeatAlpha = 90f * Smooth(u);
                    b.SeatAmount = Mathf.Clamp01(b.SeatAlpha / 10f);
                    // Leaning over the feet as the hips go down, settling back as they land.
                    b.SeatLeanExtra = 20f * Mathf.Sin(u * Mathf.PI) * (1f - u * .3f);
                    b.SeatPush = Mathf.Sin(Mathf.Clamp01(u * 1.15f) * Mathf.PI);
                    b.SeatArms = Smooth(u * 1.6f);
                    if (u >= 1f)
                    {
                        b.Position = BeggarSeat; b.SeatAlpha = 90f; b.SeatAmount = 1f; b.SeatLeanExtra = 0f; b.SeatPush = 0f; b.SeatArms = 1f;
                        w.Phase = Seated; w.Timer = Range(BeggarSits); b.State = "seated";
                        b.NextNod = _clock + 3f + Rand() * 3f; b.NextLook = _clock + 1f;
                    }
                    return;
                }
                case Seated:
                    w.Timer -= dt;
                    Walking(b, 0f, false);
                    Face(b, facing, dt, 200f, .16f);
                    if (w.Timer <= 0f && _coinT < 0f && _clock > b.ThankAt + BowSeconds + WaveFrom + WaveSeconds)
                    {
                        w.Phase = Rising; w.Clock = 0f; b.State = "packing up";
                    }
                    return;
                case Rising:
                {
                    // Reach over to his bundle, get up (hands to the pavement, lean, push), bend for the carton, go.
                    float t = w.Clock += dt;
                    Walking(b, 0f, false);
                    b.SeatReach = t < ReachSeconds ? Mathf.Sin(t / ReachSeconds * Mathf.PI) : 0f;
                    float u = Mathf.Clamp01((t - ReachSeconds) / StandSeconds);
                    if (t >= ReachSeconds)
                    {
                        b.SeatAlpha = 90f * (1f - Smooth(u));
                        b.SeatAmount = Mathf.Clamp01(b.SeatAlpha / 10f);
                        b.SeatLeanExtra = 30f * Mathf.Sin(u * Mathf.PI);
                        b.SeatPush = Mathf.Sin(Mathf.Clamp01(u * 1.3f) * Mathf.PI);
                        b.SeatArms = 1f - Smooth((u - .55f) / .45f);
                    }
                    float pick = t - ReachSeconds - StandSeconds;
                    Play(b, pick > 0f && pick < PickSeconds ? PickUpClip : Idle, .6f);
                    if (pick >= PickSeconds * .5f && pick - dt < PickSeconds * .5f) { SetProps(false); Sound(Pick(Carton), BeggarSeat, .3f, 1.5f, 12f, 1.06f, null); }
                    if (pick >= PickSeconds + .15f)
                    {
                        b.SeatAmount = 0f; b.SeatAlpha = 0f; b.SeatReach = 0f; b.SeatPush = 0f; b.SeatArms = 0f; b.SeatLeanExtra = 0f;
                        var line = _lines[w.Line];
                        w.Along = line.Length; w.Dir = -1; w.Phase = Leaving; b.State = "walking out";
                    }
                    return;
                }
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
            if (_bundle != null) _bundle.gameObject.SetActive(shown);
            if (_bag != null) _bag.gameObject.SetActive(shown);
        }

        /// <summary>A coin for the beggar, from `from` (the giver's hand). Cosmetic: a thank-you and
        /// nothing else. False while he is not seated or the last coin is still landing.</summary>
        public bool Donate(Vector3 from)
        {
            if (!BeggarSeated || _clock < _donateReady || _coinT >= 0f) return false;
            _donateReady = _clock + 1.3f;
            Donations++;
            var b = _beggar.Body;
            // The bow starts as the coin lands in the cup (it flies 0.45 s).
            b.ThankAt = _clock + .42f;
            b.BowUntil = b.ThankAt + WaveFrom + WaveSeconds;
            _beggar.Timer = Mathf.Max(_beggar.Timer, 6f);
            _coinFrom = from; _coinT = 0f;
            if (_coin != null) { _coin.position = from; _coin.gameObject.SetActive(true); }
            if (Application.isPlaying || FilmPopups)
                ComicPopup.Spawn(b.Position + Vector3.up * 1.7f, Thanks, UI.UiTheme.Highlight, .9f, ComicPopup.Weight.Flavour);
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
            if (CoinTin != null && CoinTin.Length > 0) Sound(Pick(CoinTin), to, .5f, 1.5f, 16f, .97f + Rand() * .06f, null);
            else if (Application.isPlaying) GameServices.Audio?.PlayAtVaried(CoinCue, to, 1.25f, 1.45f, .8f);
            if (_beggar != null) Later(.35f, _beggar.Body, Salamat, .55f, 1.5f, 14f);
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
                        k.It = i == 0; k.Home = false; k.Dir = -1f; k.Burst = 0f; k.NextDart = _clock + 1f + i; k.Vel = 0f;
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
                            k.Dir = 1f;
                            k.Vel = Mathf.MoveTowards(k.Vel, KidRun * .8f, KidAccel * dt);
                            k.Along = Mathf.Clamp(k.Along + k.Vel * dt, 0f, end);
                            k.LateralTarget = 0f;
                            KidPlace(k, dt, false);
                            Walking(k.Body, Mathf.Abs(k.Vel), true);
                            k.Body.State = "running home";
                        }
                        else
                        {
                            k.Vel = 0f;
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
        /// others run away, juke past at the ends, dart sideways, stop to laugh; a tag swaps.
        /// The run along the pavement is a velocity that brakes and builds up (KidAccel), so a
        /// change of direction is a juke, not a flip.</summary>
        private void Chase(Kid k, float dt, float end)
        {
            var b = k.Body;
            if (k.Pause > 0f)
            {
                k.Pause -= dt;
                Walking(b, 0f, false);
                b.State = k.It ? "counting" : "laughing";
                k.Vel = Mathf.MoveTowards(k.Vel, 0f, KidAccel * dt);
                k.Along = Mathf.Clamp(k.Along + k.Vel * dt, 0f, end);
                KidPlace(k, dt, false);
                return;
            }
            float speed, dirBefore = k.Dir;
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
                    if (_clock >= _nextTaya) { _nextTaya = _clock + 1.5f; Sound(Pick(KidTaya), b.Position + Vector3.up * 1f, .5f, 2f, 28f, .98f + Rand() * .06f, b); }
                    Later(.3f, target.Body, KidGiggle, .42f, 2f, 24f, true);
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
                    if (_rng.NextDouble() < dt * .25f)
                    {
                        k.Pause = .8f + (float)_rng.NextDouble() * .8f; b.LaughUntil = _clock + 1.2f;
                        Later(.05f, b, KidGiggle, .38f, 2f, 24f, true);
                    }
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
                // A near miss: a squeal now and then.
                if (it != null && Mathf.Abs(gap) < 1.1f && it.Pause <= 0f && _clock >= _nextShriek && Rand() < dt * 1.5f)
                {
                    _nextShriek = _clock + 4f;
                    Sound(Pick(KidGiggle), b.Position + Vector3.up * 1f, .38f, 2f, 24f, 1.02f + Rand() * .06f, b);
                }
            }
            speed *= k.Pace;
            if (k.Burst > 0f) { k.Burst -= dt; speed *= 1.3f; }
            if (k.Dir != dirBefore && Mathf.Abs(k.Vel) > 1f)
            {
                b.JukeUntil = _clock + .4f;
                b.JukeSide = k.LateralTarget >= k.Lateral ? 1f : -1f;
            }
            k.Vel = Mathf.MoveTowards(k.Vel, k.Dir * speed, KidAccel * dt);
            k.Along = Mathf.Clamp(k.Along + k.Vel * dt, 0f, end);
            if ((k.Along <= 0f && k.Vel < 0f) || (k.Along >= end && k.Vel > 0f)) k.Vel = 0f;
            KidPlace(k, dt, false);
            Walking(b, Mathf.Abs(k.Vel), true);
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
            if (snap) { k.Body.Yaw = Quaternion.LookRotation(tangent * k.Dir, Vector3.up).eulerAngles.y; k.Body.YawVel = 0f; return; }
            if (dt <= 0f) return;
            // Facing where the kid is actually running; while it brakes to a stop, toward where it means to go.
            float run = Mathf.Abs(k.Vel) > .4f ? k.Vel : k.Dir * .4f;
            Face(k.Body, tangent * run + side * (k.Lateral - before) / dt, dt, 720f, .06f);
        }

        // ------------------------------------------------------------------ sound

        private sealed class Voice { public AudioSource Source; public Body Follow; public float Gain; }
        private const int VoiceCount = 10;
        private Voice[] _voices = new Voice[0];
        private int _nextVoice;
        private System.Random _soundRng;
        private readonly Dictionary<AudioClip[], int> _lastPick = new Dictionary<AudioClip[], int>();
        private float _nextTaya, _nextShriek, _nextGiggle, _nextBucket, _nextCoo = 2f, _nextFlap;
        private bool[] _flushing = new bool[0];
        private struct Pending { public float At; public Body Body; public AudioClip[] Set; public float Gain, Near, Far; public bool Giggle; }
        private readonly List<Pending> _later = new List<Pending>();

        private float Rand() => (float)(_soundRng ?? (_soundRng = new System.Random(Seed * 31 + 5))).NextDouble();

        /// <summary>One clip of a set, never the same one twice running.</summary>
        private AudioClip Pick(AudioClip[] set)
        {
            if (set == null || set.Length == 0) return null;
            _lastPick.TryGetValue(set, out int last);
            int i = (int)(Rand() * set.Length) % set.Length;
            if (set.Length > 1 && i == last - 1) i = (i + 1) % set.Length;
            _lastPick[set] = i + 1;
            return set[i];
        }

        /// <summary>A voice from `body`'s head in `delay` seconds (giggles share one cooldown).</summary>
        private void Later(float delay, Body body, AudioClip[] set, float gain, float near, float far, bool giggle = false)
        {
            if (set == null || set.Length == 0) return;
            _later.Add(new Pending { At = _clock + delay, Body = body, Set = set, Gain = gain, Near = near, Far = far, Giggle = giggle });
        }

        private void StepLater()
        {
            for (int i = _later.Count - 1; i >= 0; i--)
            {
                var p = _later[i];
                if (_clock < p.At) continue;
                _later.RemoveAt(i);
                if (p.Body != null && !p.Body.Shown) continue;
                if (p.Giggle) { if (_clock < _nextGiggle) continue; _nextGiggle = _clock + 1.3f; }
                var at = p.Body != null ? p.Body.Position + Vector3.up * (p.Body.Role == "kid" ? 1f : 1.45f) : BeggarSeat;
                Sound(Pick(p.Set), at, p.Gain, p.Near, p.Far, .97f + Rand() * .06f, p.Body);
            }
        }

        /// <summary>A footfall: the rubber step, pitched and weighted per person, and the magtataho's
        /// buckets now and then in step.</summary>
        private void Footfall(Body b)
        {
            if (Footstep != null)
            {
                float run = Mathf.Lerp(1f, 1.25f, b.RunW);
                Sound(Footstep, b.Position, b.StepGain * run, 1f, 13f, b.StepPitch * (.94f + Rand() * .12f), null);
            }
            if (b.Yoke != null && _clock >= _nextBucket && Rand() < .45f)
            {
                _nextBucket = _clock + .9f;
                Sound(Pick(Bucket), b.Swing.Length > 0 ? b.Swing[0].position : b.Position + Vector3.up * .5f, .14f, 1.5f, 12f, .96f + Rand() * .08f, b);
            }
        }

        /// <summary>The pigeons: a wing-flap burst when birds flush off a perch or the ground (one
        /// sound per flush, however many birds go), and a coo from a settled bird every few seconds.</summary>
        private void StepPigeons(float dt)
        {
            var flock = Pigeons;
            if (flock == null) return;
            int n = flock.BirdCount;
            if (_flushing.Length != n) _flushing = new bool[n];
            for (int i = 0; i < n; i++)
            {
                bool going = flock.BirdTakingOff(i);
                if (going && !_flushing[i] && _clock >= _nextFlap)
                {
                    _nextFlap = _clock + .8f + Rand() * .6f;
                    Sound(Pick(PigeonFlap), flock.BirdPosition(i), .42f, 3f, 32f, .95f + Rand() * .1f, null);
                }
                _flushing[i] = going;
            }
            _nextCoo -= dt;
            if (_nextCoo > 0f || n == 0) return;
            _nextCoo = 2.5f + Rand() * 4.5f;
            var ear = Ear();
            int pick = -1; float best = float.MaxValue;
            for (int k = 0; k < 8; k++)
            {
                int i = (int)(Rand() * n) % n;
                if (!flock.BirdSettled(i)) continue;
                float d = ear.HasValue ? (flock.BirdPosition(i) - ear.Value).sqrMagnitude : k;
                if (d < best) { best = d; pick = i; }
            }
            if (pick >= 0) Sound(Pick(PigeonCoo), flock.BirdPosition(pick), .3f, 2f, 18f, .93f + Rand() * .14f, null);
        }

        private static Vector3? Ear()
        {
            if (!Application.isPlaying) return null;
            var camera = Camera.main;
            return camera != null ? camera.transform.position : (Vector3?)null;
        }

        /// <summary>
        /// Starts `clip` at `at` on one of the life's own 3D voices (Play only; the films log it).
        /// ⚠️ THE STREET'S MIX, NOT THE MATCH'S: the pause menu's SFX slider, ducked to nothing
        /// during a replay (the director's `IsInReplayMix`, which only mutes its own pool), held
        /// while the game is paused, logarithmic rolloff from `near` to `far` metres, no Doppler,
        /// priority 200 (the first to give way when the mixer runs short of voices), and a gain
        /// under the match's cues. A sound whose listener is well past `far` is not started.
        /// </summary>
        private void Sound(AudioClip clip, Vector3 at, float gain, float near, float far, float pitch, Body follow)
        {
            if (clip == null) return;
            gain *= SoundGain;
            if (RecordSounds) SoundLog.Add(new Heard { Time = _clock, Clip = clip.name, At = at, Gain = gain, Pitch = pitch, Near = near, Far = far });
            if (!Application.isPlaying || AudioListener.pause || gain <= .001f) return;
            var ear = Ear();
            if (ear.HasValue && (ear.Value - at).sqrMagnitude > far * far * 1.3f) return;
            if (_voices.Length == 0) BuildVoices();
            Voice voice = null;
            for (int i = 0; i < _voices.Length && voice == null; i++)
            {
                var v = _voices[(_nextVoice + i) % _voices.Length];
                if (!v.Source.isPlaying) voice = v;
            }
            if (voice == null) voice = _voices[_nextVoice];
            _nextVoice = (Array.IndexOf(_voices, voice) + 1) % _voices.Length;
            voice.Source.Stop();
            voice.Source.transform.position = at;
            voice.Source.clip = clip;
            voice.Source.pitch = pitch;
            voice.Source.minDistance = near;
            voice.Source.maxDistance = far;
            voice.Follow = follow;
            voice.Gain = gain;
            voice.Source.volume = gain * Mix();
            voice.Source.Play();
        }

        private static float Mix()
        {
            var director = GameServices.Audio;
            if (director == null) return 1f;
            return director.IsInReplayMix ? 0f : director.SfxVolume;
        }

        private void BuildVoices()
        {
            _voices = new Voice[VoiceCount];
            for (int i = 0; i < VoiceCount; i++)
            {
                var go = new GameObject("SidewalkVoice" + i);
                go.transform.SetParent(transform, false);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = false;
                src.spatialBlend = 1f;
                src.rolloffMode = AudioRolloffMode.Logarithmic;
                src.dopplerLevel = 0f;
                src.priority = 200;
                _voices[i] = new Voice { Source = src };
            }
        }

        /// <summary>Voices in the air follow their person and track the slider and a replay duck.</summary>
        private void Voices()
        {
            if (_voices.Length == 0) return;
            float mix = Mix();
            foreach (var v in _voices)
            {
                if (!v.Source.isPlaying) { v.Follow = null; continue; }
                v.Source.volume = v.Gain * mix;
                if (v.Follow != null && v.Follow.Shown) v.Source.transform.position = v.Follow.Position + Vector3.up * (v.Follow.Role == "kid" ? 1f : 1.45f);
            }
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
