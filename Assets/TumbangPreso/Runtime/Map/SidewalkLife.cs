using System;
using System.Collections.Generic;
using TumbangPreso.Visual;
using UnityEngine;

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
    ///     facing) over the style's no-slide stride (`GaitStyle.CycleMetres` without its Glide),
    ///     whether the body walks, is pushed aside, backs up or steps round on the spot;
    ///   * ⚠️ the stance sole is LOCKED to the pavement where it landed while the body passes over
    ///     it, the swing hip is hiked so the swing sole clears the ground, and the hips sit on the
    ///     planted leg (see Locomote: the first version dropped the hips onto the LOWER sole, which
    ///     half the time was the swing foot, dragged along at twice the body's speed);
    ///   * the arms swing opposite their own side's leg from the same phase, per role (kids big and
    ///     carried forward, passers-by at least 22 degrees, the beggar's small, the magtataho's pole
    ///     arm held within 9 degrees under the pole);
    ///   * turns ease in and out (a damped yaw), corners are rounded by a smoothed heading, and the
    ///     kids brake and re-accelerate through a juke instead of reversing in one frame.
    /// ⚠️⚠️ THE CLIPS ARE SAMPLED, NOT PLAYED THROUGH AN ANIMATOR (2026-10-01, owner: "the feet are
    /// moving but the hands arent"). The rigs' idle keys both arms, the chest and the head, and a
    /// PlayableGraph on the rig's Animator wrote that pose back over everything drawn on those four
    /// bones (in Play and in the films alike), while the legs and hips, which idle does not key,
    /// kept the drawn walk. So the arm swing, the chest's lean, the cheer, the bow and the wave
    /// never showed. Each frame now resets the seven bones to their bind pose, samples the clip
    /// (`AnimationClip.SampleAnimation`, crossfades blended by hand) and draws on top; the Animator
    /// is disabled.
    /// THE POP (owner 2026-10-01: "the sit animation is too linear and too unlively not poppy
    /// enough"): every gesture rides <see cref="Pop"/> (a wind-up the other way, a snap with an
    /// overshoot, a hold, an eased return with a small settle), landings and hops squash on the
    /// cast's own squash spring (<see cref="Spring"/>), and the head lags the chest a beat
    /// (<see cref="HeadLag"/>).
    /// ⚠️⚠️ THE BEGGAR SITS WITH A DRAWN POSE, NOT THE `sit` CLIP. The clip was a chair sit: hips
    /// 6 cm up with the legs 15 degrees below level (so they sank into the pavement and needed a
    /// 7 cm lift) and the arms held out at 45 degrees. Here the legs rest a little above level and
    /// a little apart on the carton, set down by their REAL mesh (<see cref="Hull"/>) so the thighs
    /// rest on it, and his chest block is lowered until its underside rests on the carton too
    /// (level legs join the chest at its middle, and the owner saw him "floating" 14 cm over it);
    /// a slight lean back toward the fence with the head bowed, the left hand resting on the
    /// pavement by the tin cup (its elevation solved each frame so the fist meets the ground) and
    /// the right arm out over his bundle. He breathes, nods now and then and looks up at whoever
    /// passes. Sitting down is a wind-up, a drop that speeds into the carton, a squash and a
    /// settle; standing up is a deep lean, a pop onto his feet and a stretch; the bow and the wave
    /// after a coin (the arm up to the side, the fist over the shoulder, rocking, the head tilted
    /// away so the arm passes under its overhang) start and end in the seated pose.
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
        private const float SettleBendFrom = .55f, SettleCarton = .8f, SettleBendTo = 1.12f, SitFrom = 1.45f, SitWind = .3f, SitSeconds = .62f;
        private const float ReachSeconds = .8f, StandWind = .35f, StandSeconds = .55f, PickSeconds = .7f;
        /// <summary>The seated thank-you: a bow, then a wave with the free hand, from the coin landing.</summary>
        private const float BowSeconds = 1.1f, WaveFrom = .55f, WaveSeconds = 2f;
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
            /// <summary>The rig instance the clips are sampled onto, and its rest scale (the squash rides on it).</summary>
            public GameObject Model;
            public Vector3 ModelScale = Vector3.one;
            /// <summary>The seven drawn bones, in <see cref="Bones"/> order, for the clip sampling.</summary>
            public Transform[] Drawn = new Transform[0];
            public readonly AnimationClip[] Clips = new AnimationClip[ClipNames.Length];
            public readonly float[] Weight = new float[ClipNames.Length];
            public readonly float[] ClipTime = new float[ClipNames.Length];
            public readonly float[] Rate = { 1f, 1f, 1f, 1f, 1f, 1f, 1f };
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

            // THE FOOT PLANT (see Locomote): per leg (0 LegL, 1 LegR) the world point its sole is
            // locked to through the stance, and the drawn direction it let go from at toe-off.
            public readonly Vector3[] Plant = new Vector3[2];
            public readonly bool[] Planted = new bool[2], Stance = new bool[2];
            public readonly Vector3[] Released = new Vector3[2];
            public readonly float[] ReleasedAt = new float[2];
            /// <summary>The leg bearing the weight this frame (0 LegL, 1 LegR, -1 none), for probes.</summary>
            public int StanceLeg = -1;
            /// <summary>The arm swing on top of the gait's own: a multiplier and a carry (degrees)
            /// per role (kids big and bent, the beggar's small), set in Begin.</summary>
            public float ArmGain = 1f, ArmCarry, ArmMin;

            // The pop: a squash spring on the model's scale (the cast's CharacterSquashStretch
            // numbers) and the head lagging the chest a beat (its own spring).
            public float Squash, SquashVel;
            public Quaternion HeadShown = Quaternion.identity;
            public Vector3 HeadVel;
            public bool HeadLagging;

            // The seated pose (the beggar).
            public float SeatAmount, SeatAlpha, SeatLeanExtra, SeatArms, SeatReach, SeatPush, SeatLift;
            /// <summary>Each block's corners in its bone's space (see Hull): the seat and the legs set down on the carton by them.</summary>
            public Vector3[] TorsoHull = new Vector3[0], LegHullL = new Vector3[0], LegHullR = new Vector3[0], HeadHull = new Vector3[0],
                ArmHullL = new Vector3[0], ArmHullR = new Vector3[0];
            public float LandAt = -99f, RiseAt = -99f;
            public float ThankAt = -99f;
            public float LookWeight, LookYaw, NodAt = -99f, NextNod, NextLook, LookUntil;
            public Body LookAt;
            public float JukeUntil, JukeSide, ClapUntil, CheerFrom = -99f, LaughFrom = -99f;
            public int Hops;
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
        /// The sole of the leg bearing the weight, world position, as posed this frame (probes: a
        /// planted foot that moves along the ground is a foot that slides), and which leg it is.
        /// While walking that is the leg the phase puts in stance (see Locomote), NOT the lower
        /// sole: the first probe measured the lower sole, which for half of each step was the
        /// swing foot. Standing, it is the lower sole. False without legs.
        /// </summary>
        public bool PersonSole(int i, out Vector3 sole, out bool left)
        {
            sole = Vector3.zero; left = false;
            var b = _all[i];
            if (b.LegL == null || b.LegR == null || b.Reach <= 0f) return false;
            var l = SoleOf(b, 0); var r = SoleOf(b, 1);
            left = b.StanceLeg >= 0 ? b.StanceLeg == 0 : l.y <= r.y;
            sole = left ? l : r;
            return true;
        }
        /// <summary>The other (swinging) sole's height over the body's ground, metres (probes: the swing foot's clearance).</summary>
        public float PersonSwingHeight(int i)
        {
            var b = _all[i];
            if (b.StanceLeg < 0 || b.LegL == null || b.LegR == null) return float.NaN;
            return SoleOf(b, 1 - b.StanceLeg).y - b.Position.y;
        }
        /// <summary>True while the body's feet are in the drawn walk's stance lock (probes).</summary>
        public bool PersonStepping(int i) => _all[i].StanceLeg >= 0;
        /// <summary>
        /// The limbs' swing this frame in the body's own frame, degrees forward of hanging (the
        /// arm's and the leg's on the body's LEFT and RIGHT, by where they sit): the probe's check
        /// that the arms swing, and against the same side's leg (opposite phase).
        /// </summary>
        public bool PersonLimbs(int i, out float armLeft, out float armRight, out float legLeft, out float legRight)
        {
            armLeft = armRight = legLeft = legRight = 0f;
            var b = _all[i];
            if (b.ArmL == null || b.ArmR == null || b.LegL == null || b.LegR == null) return false;
            float Forward(Transform bone, Vector3 axis)
            {
                var d = b.Root.InverseTransformDirection(bone.TransformDirection(axis));
                return Mathf.Atan2(d.z, -d.y) * Mathf.Rad2Deg;
            }
            bool armLIsLeft = SideOf(b, b.ArmL, -1f) < 0f, legLIsLeft = SideOf(b, b.LegL, -1f) < 0f;
            float al = Forward(b.ArmL, b.AlongL), ar = Forward(b.ArmR, b.AlongR);
            float ll = Forward(b.LegL, b.LegAxisL), lr = Forward(b.LegR, b.LegAxisR);
            armLeft = armLIsLeft ? al : ar; armRight = armLIsLeft ? ar : al;
            legLeft = legLIsLeft ? ll : lr; legRight = legLIsLeft ? lr : ll;
            return true;
        }
        /// <summary>The seated beggar's waving hand (his right fist) over his shoulder pivot, metres,
        /// and how far it stands out beside his head (probes). NaN unless he is waving.</summary>
        public float BeggarWaveLift { get; private set; } = float.NaN;
        public float BeggarWaveOut { get; private set; } = float.NaN;
        /// <summary>How deep the waving arm's block goes into the head's block, metres (0: clear). NaN unless waving.</summary>
        public float BeggarWaveInHead { get; private set; } = float.NaN;

        private static Vector3 SoleOf(Body b, int leg)
        {
            var bone = leg == 0 ? b.LegL : b.LegR; var axis = leg == 0 ? b.LegAxisL : b.LegAxisR;
            return bone.position + bone.TransformDirection(axis).normalized * b.Reach;
        }
        /// <summary>The lowest point of the seated beggar's legs above the carton top (probes:
        /// under zero is a leg in the carton). NaN unless he is seated.</summary>
        public float BeggarSeatClearance { get; private set; } = float.NaN;
        /// <summary>The lowest point of his seat (the chest block) above the carton top: about 0 is
        /// sitting ON it; well over 0 is floating. NaN unless he is seated.</summary>
        public float BeggarSeatRest { get; private set; } = float.NaN;
        public float Clock => _clock;
        public int Donations { get; private set; }
        public bool BeggarSeated => _beggar != null && _beggar.Phase == Seated;
        public bool BeggarThanking => _beggar != null && _clock < _beggar.Body.BowUntil;
        public Vector3 CupPosition => _cup != null ? _cup.position : BeggarSeat;
        public bool KidsOut => _kidsPhase != Hidden;
        public int Watching { get { int n = 0; foreach (var s in _spectators) if (s.Phase == Arrived) n++; return n; } }
        public int Cheering { get { int n = 0; foreach (var s in _spectators) if (_clock < s.Body.CheerUntil) n++; return n; } }
        public int Clapping { get { int n = 0; foreach (var s in _spectators) if (_clock < s.Body.ClapUntil) n++; return n; } }

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
                // A passer-by's walk swings the arms at least about 22 degrees forward (Jun-Jun's own
                // walk is 12, a match stride in a suit, which read as no swing at all on the pavement).
                if (w.Body.Gait != null) w.Body.ArmGain = Mathf.Max(1f, 22f / Mathf.Max(1f, w.Body.Gait.Walk.ArmForward));
                _spectators.Add(w); _walkers.Add(w);
            }
            if (_track.P.Length >= 2)
                for (int i = 0; i < Kids.Length; i++)
                    if (Kids[i] != null && Kids[i].Art != null)
                    {
                        var k = new Kid { Body = MakeBody(Kids[i], "Kid " + i, "kid"), Pace = .93f + .07f * (i % 3) };
                        k.Body.StepGain = .12f; k.Body.StepPitch = 1.28f + .06f * i;
                        // Kids run with big swings carried forward (no elbow on these rigs: the carry is the bend).
                        k.Body.ArmGain = 1.15f; k.Body.ArmCarry = 14f;
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

            // ⚠️⚠️ NO ANIMATOR, NO PLAYABLE GRAPH: the clips are SAMPLED onto the bones (see Sample).
            // With a graph on the rig's Animator, the idle clip's own channels (both arms, the
            // chest and the head) were written back over the drawn pose AFTER this component drew
            // it, in Play and in the films alike, while the legs and hips (which idle does not
            // key) kept theirs: owner 2026-10-01, "the feet are moving but the hands arent", and a
            // beggar whose chest, bow and wave never showed.
            var animator = model.GetComponent<Animator>();
            if (animator != null) animator.enabled = false;
            // Outside Play (the builder's probe and films) every step renders inside one editor frame,
            // and a skinned mesh re-skins once a frame unless told to: without this every still and
            // film frame showed the pose of the first. Play renders once a frame and needs nothing.
            if (!Application.isPlaying)
                foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true)) skin.forceMatrixRecalculationPerRender = true;
            b.Model = model;
            b.ModelScale = model.transform.localScale;
            for (int i = 0; i < ClipNames.Length; i++) b.Clips[i] = FindClip(look.Art.Clips, ClipNames[i]);
            Play(b, Idle, 1f);
            b.Weight[Idle] = 1f;
            Sample(b);
            Show(b, false);
            return b;
        }

        /// <summary>
        /// The clips' pose, sampled straight onto the seven bones, every frame, before anything is
        /// drawn over it: each bone back to its bind pose first (so nothing drawn last frame
        /// survives: the drawn layers are rebuilt from scratch), then the one playing clip, or
        /// during a crossfade each weighted clip sampled in turn and blended.
        /// </summary>
        private static void Sample(Body b)
        {
            if (b.Model == null) return;
            var bones = b.Drawn;
            ResetBones(b);
            int single = -1, count = 0; float total = 0f;
            for (int i = 0; i < ClipNames.Length; i++)
                if (b.Clips[i] != null && b.Weight[i] > 1e-3f) { count++; single = i; total += b.Weight[i]; }
            if (count == 0) return;
            if (count == 1) { b.Clips[single].SampleAnimation(b.Model, b.ClipTime[single]); return; }
            var rot = new Quaternion[bones.Length]; var pos = new Vector3[bones.Length];
            float sum = 0f;
            for (int i = 0; i < ClipNames.Length; i++)
            {
                if (b.Clips[i] == null || b.Weight[i] <= 1e-3f) continue;
                ResetBones(b);
                b.Clips[i].SampleAnimation(b.Model, b.ClipTime[i]);
                float w = Smooth(b.Weight[i] / total);
                float k = sum <= 0f ? 1f : w / (sum + w);
                for (int j = 0; j < bones.Length; j++)
                {
                    if (bones[j] == null) continue;
                    rot[j] = sum <= 0f ? bones[j].localRotation : Quaternion.Slerp(rot[j], bones[j].localRotation, k);
                    pos[j] = sum <= 0f ? bones[j].localPosition : Vector3.Lerp(pos[j], bones[j].localPosition, k);
                }
                sum += w;
            }
            for (int j = 0; j < bones.Length; j++)
                if (bones[j] != null) { bones[j].localRotation = rot[j]; bones[j].localPosition = pos[j]; }
        }

        private static void ResetBones(Body b)
        {
            foreach (var bone in b.Drawn)
                if (bone != null && b.Bind.TryGetValue(bone, out var rest)) { bone.localRotation = rest.rotation; bone.localPosition = rest.position; }
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
                        case "arm-left":
                            if (b.ArmL == null) { b.ArmL = bone; b.AlongL = AlongArm(binds, i, b.AlongL); }
                            if (b.ArmL == bone && b.ArmHullL.Length == 0) b.ArmHullL = Hull(skin, binds, i);
                            break;
                        case "arm-right":
                            if (b.ArmR == null) { b.ArmR = bone; b.AlongR = AlongArm(binds, i, b.AlongR); }
                            if (b.ArmR == bone && b.ArmHullR.Length == 0) b.ArmHullR = Hull(skin, binds, i);
                            break;
                        case "leg-left":
                        case "leg-right":
                            // The reach from every skin that carries the leg (the head's skin does too,
                            // and reads negative), the bone from the first.
                            if (binds != null && i < binds.Length && mesh != null)
                            {
                                float local = binds[i].inverse.MultiplyPoint3x4(Vector3.zero).y - mesh.bounds.min.y;
                                b.Reach = Mathf.Max(b.Reach, local * skin.transform.TransformVector(Vector3.up).magnitude);
                            }
                            if (bone.name == "leg-left") { if (b.LegL == null) { b.LegL = bone; b.LegAxisL = DownLeg(binds, i); } if (b.LegL == bone && b.LegHullL.Length == 0) b.LegHullL = Hull(skin, binds, i); }
                            else { if (b.LegR == null) { b.LegR = bone; b.LegAxisR = DownLeg(binds, i); } if (b.LegR == bone && b.LegHullR.Length == 0) b.LegHullR = Hull(skin, binds, i); }
                            break;
                        case "torso":
                            if (b.Torso == null) b.Torso = bone;
                            if (b.Torso == bone && b.TorsoHull.Length == 0) b.TorsoHull = Hull(skin, binds, i);
                            break;
                        case "head":
                            if (b.Head == null) b.Head = bone;
                            if (b.Head == bone && b.HeadHull.Length == 0) b.HeadHull = Hull(skin, binds, i);
                            break;
                    }
                }
            }
            if (b.Torso != null && b.Torso.parent != null && b.Torso.parent.name == "root") b.RootBone = b.Torso.parent;
            b.Drawn = new[] { b.RootBone, b.Torso, b.Head, b.ArmL, b.ArmR, b.LegL, b.LegR };
            foreach (var bone in b.Drawn)
                if (bone != null) b.Bind[bone] = (bone.localRotation, bone.localPosition);
            if (b.Reach < .05f || b.Reach > 1.5f) b.Reach = 0f;
        }

        /// <summary>The corners of the part of the mesh a bone carries, in that bone's own space
        /// (bind pose times vertex, de-duplicated to the millimetre): a posed bone's
        /// `TransformPoint` of these is where that block of the body actually is, so the seated
        /// beggar is set down by his real legs and seat, not by a guessed thickness. Empty when
        /// the mesh is not readable.</summary>
        private static Vector3[] Hull(SkinnedMeshRenderer skin, Matrix4x4[] binds, int index)
        {
            var mesh = skin.sharedMesh;
            if (mesh == null || !mesh.isReadable || binds == null || index >= binds.Length) return new Vector3[0];
            var vertices = mesh.vertices; var weights = mesh.boneWeights;
            if (weights == null || weights.Length != vertices.Length) return new Vector3[0];
            var seen = new HashSet<Vector3Int>();
            var points = new List<Vector3>();
            for (int v = 0; v < vertices.Length; v++)
            {
                if (weights[v].boneIndex0 != index || weights[v].weight0 < .5f) continue;
                var local = binds[index].MultiplyPoint3x4(vertices[v]);
                if (seen.Add(Vector3Int.RoundToInt(local * 1000f))) points.Add(local);
            }
            return points.ToArray();
        }

        /// <summary>The lowest world height of a posed bone's hull (+infinity without one).</summary>
        private static float Lowest(Transform bone, Vector3[] hull)
        {
            float low = float.PositiveInfinity;
            if (bone == null || hull == null) return low;
            foreach (var p in hull) low = Mathf.Min(low, bone.TransformPoint(p).y);
            return low;
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
            b.Planted[0] = b.Planted[1] = false; b.StanceLeg = -1;
            b.Squash = b.SquashVel = 0f; b.HeadLagging = false; b.HeadVel = Vector3.zero;
        }

        /// <summary>Crossfades to `clip` (falling back to idle when the rig lacks it) at `rate`.</summary>
        private void Play(Body b, int clip, float rate)
        {
            if (b.Clips[clip] == null) clip = Idle;
            if (b.Clips[clip] == null) return;
            if (b.Clip != clip)
            {
                if (!Loops[clip] || b.Weight[clip] < .01f) b.ClipTime[clip] = 0f;
                b.Clip = clip;
            }
            b.Rate[clip] = rate;
        }

        private void Pose(Body b, float dt)
        {
            if (!b.Shown || b.Model == null) return;
            for (int i = 0; i < ClipNames.Length; i++)
            {
                if (b.Clips[i] == null) continue;
                // The crossfade's weights ramp here and are eased where they are blended (Sample).
                b.Weight[i] = Mathf.MoveTowards(b.Weight[i], i == b.Clip ? 1f : 0f, dt / Fade);
                if (b.Weight[i] <= 0f) continue;
                float length = b.Clips[i].length;
                b.ClipTime[i] += dt * b.Rate[i];
                if (length > 1e-3f) b.ClipTime[i] = Loops[i] ? Mathf.Repeat(b.ClipTime[i], length) : Mathf.Min(b.ClipTime[i], length);
            }
            Measure(b, dt);
            // The character frame first: every drawn layer below is built in it.
            b.Root.SetPositionAndRotation(b.Position, Quaternion.Euler(0f, b.Yaw, 0f));
            Spring(b, dt);
            Sample(b);
            Locomote(b, dt);
            DrawSeat(b, dt);
            Overlays(b, dt);
            HeadLag(b, dt);
            if (b.Yoke != null) Carry(b, dt);
        }

        /// <summary>
        /// THE POP (owner 2026-10-01: "the sit animation is too linear and too unlively not poppy
        /// enough"). A squash on the model's scale, sprung exactly as the cast's
        /// `CharacterSquashStretch` springs it (stiffness 24, damping 8.5, 0.02 s substeps, volume
        /// kept: down by q, out by q/2): a kick squashes (landing on the carton, a laugh's hop
        /// landing) or stretches (the stand-up's pop, a cheer's throw), and the body rings back
        /// through its rest shape with one soft overshoot. The model's pivot is at the soles, so
        /// a squash never lifts the feet.
        /// </summary>
        private static void Spring(Body b, float dt)
        {
            if (dt > 0f)
            {
                int steps = Mathf.Clamp(Mathf.CeilToInt(dt / .02f), 1, 32);
                float step = dt / steps;
                for (int i = 0; i < steps; i++)
                {
                    b.SquashVel += (-24f * b.Squash - 8.5f * b.SquashVel) * step;
                    b.Squash += b.SquashVel * step;
                }
                if (float.IsNaN(b.Squash) || float.IsInfinity(b.Squash)) b.Squash = b.SquashVel = 0f;
                b.Squash = Mathf.Clamp(b.Squash, -.25f, .25f);
            }
            float q = b.Squash;
            b.Model.transform.localScale = Vector3.Scale(b.ModelScale, new Vector3(1f + q * .5f, 1f - q, 1f + q * .5f));
        }

        /// <summary>A squash (positive) or stretch (negative) kick, as `CharacterSquashStretch.Squash` sets it.</summary>
        private static void Kick(Body b, float amount) { b.Squash = amount; b.SquashVel = 0f; }

        /// <summary>
        /// Secondary motion: the head (and the hair on it) follows the drawn head a beat late, on
        /// an underdamped spring (about 13 rad/s, one small overshoot), so a snap of the chest
        /// (a bow, a landing, a juke, a laugh) carries through the head instead of moving it rigidly.
        /// </summary>
        private static void HeadLag(Body b, float dt)
        {
            if (b.Head == null) return;
            var target = b.Head.rotation;
            if (!b.HeadLagging || dt <= 0f) { b.HeadShown = target; b.HeadVel = Vector3.zero; b.HeadLagging = true; return; }
            int steps = Mathf.Clamp(Mathf.CeilToInt(dt / .01f), 1, 40);
            float step = dt / steps;
            for (int i = 0; i < steps; i++)
            {
                (target * Quaternion.Inverse(b.HeadShown)).ToAngleAxis(out float angle, out var axis);
                if (angle > 180f) angle -= 360f;
                var error = float.IsNaN(axis.x) || float.IsInfinity(axis.x) ? Vector3.zero : axis * angle;
                b.HeadVel += (error * 170f - b.HeadVel * 13f) * step;
                b.HeadShown = Quaternion.AngleAxis(b.HeadVel.magnitude * step, b.HeadVel.sqrMagnitude > 1e-8f ? b.HeadVel.normalized : Vector3.up) * b.HeadShown;
            }
            // Never more than 12 degrees behind: a lag, not a wobble.
            float behind = Quaternion.Angle(target, b.HeadShown);
            if (behind > 12f) b.HeadShown = Quaternion.RotateTowards(target, b.HeadShown, 12f);
            b.Head.rotation = b.HeadShown;
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

        /// <summary>How far the swing leg's hip is hiked at passing, degrees of hip roll (see Locomote).</summary>
        private const float HikeDegrees = 4.5f;
        /// <summary>The magtataho's right arm under the shoulder pole never swings further than
        /// this off its carry: its sleeve's top corner stays under the pole (see ShoulderPoleY).</summary>
        private const float PoleArmSwing = 9f;

        /// <summary>
        /// The walk and the run, drawn from the body's `GaitStyle` over whatever the clip left
        /// (the cast's `ApplyLocomotionArms`, minus the match's carry and fatigue). The cadence is
        /// the measured speed over the no-slide stride. Turning on the spot steps too (a quarter
        /// metre of foot travel per radian), so nobody pivots on frozen feet.
        ///
        /// ⚠️⚠️ THE FOOT PLANT LOCKS THE SOLE IN THE WORLD (2026-10-01). The first drawn walk only
        /// set the cadence and dropped the hips onto the LOWER sole. These legs have no knee, and
        /// with the legs' two swings mirrored the lower sole is always the one BEHIND, which for
        /// half of every step is the SWING foot: it dragged forward along the pavement at twice
        /// the body's speed, and the probe read the "planted" sole moving at the body's own speed
        /// (taho 0.80 against 0.80 m/s, kids 2.26 against 2.39). Now:
        ///   * the leg that bears the weight is chosen by the PHASE (a leg is in stance from its
        ///     footfall, fully forward, to its toe-off, fully back: the half cycle it travels back
        ///     under the body), not by which sole is lower;
        ///   * at its footfall the stance sole's world point is recorded, and through the stance
        ///     the leg is aimed from its hip straight at that point, its length setting the hip's
        ///     height, so the sole stays put while the body passes over it (the stride times the
        ///     step rate still equals the body's speed, so the stance ends where the curve does);
        ///   * the swing side's hip is hiked (a hip roll, <see cref="HikeDegrees"/>, most at
        ///     passing, none at the footfalls; the chest takes it back) so the swing sole clears
        ///     the pavement instead of scraping it, and it leaves the lock by easing from where it
        ///     let go onto the drawn curve;
        ///   * the hips come down so the lowest sole is on the pavement, which through the stance
        ///     is the planted one.
        /// ARMS: each arm swings opposite its own side's leg from the same phase (the style's own
        /// numbers), scaled per role (<see cref="Body.ArmGain"/>), the magtataho's pole arm held
        /// within <see cref="PoleArmSwing"/>.
        /// </summary>
        private void Locomote(Body b, float dt)
        {
            if (b.Gait == null || b.LegL == null || b.LegR == null || b.ArmL == null || b.ArmR == null || b.Reach <= 0f) return;
            float turning = Mathf.Abs(b.YawVel) * Mathf.Deg2Rad * .25f;
            float moving = Mathf.Max(b.Speed, turning);
            float target = b.SeatAmount > 0f ? 0f : Mathf.Clamp01((moving - .06f) / .3f);
            b.Loco = Mathf.MoveTowards(b.Loco, target, dt / (target > b.Loco ? .15f : .22f));
            b.RunW = Mathf.MoveTowards(b.RunW, b.RunTarget, dt / .2f);
            if (b.Loco <= .001f) { b.Cadence = 0f; b.Planted[0] = b.Planted[1] = false; b.Stance[0] = b.Stance[1] = false; b.StanceLeg = -1; return; }

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
            float amount = Smooth(b.Loco);
            var right = b.Root.right; var forward = b.Root.forward; var up = b.Root.up;
            // The left leg (by where it sits) bears the weight from its footfall at a quarter cycle to its toe-off at three quarters.
            bool leftStance = Mathf.Repeat(b.Phase - .25f, 1f) < .5f;

            // The hips: back to rest, the style's side shift, then the swing side hiked about the hips' middle.
            ToBind(b, b.RootBone, amount, true);
            float hike = 0f;
            if (b.RootBone != null)
            {
                b.RootBone.position += right * (pose.RootRight * b.Reach * amount);
                float passing = .5f + .5f * Mathf.Cos(4f * Mathf.PI * b.Phase);
                hike = HikeDegrees * passing * amount * (leftStance ? 1f : -1f);
                b.RootBone.RotateAround((b.LegL.position + b.LegR.position) * .5f, forward, hike);
            }
            ToBind(b, b.Torso, amount, false);
            ToBind(b, b.Head, amount, false);
            if (b.Torso != null)
                b.Torso.rotation = Quaternion.AngleAxis(pose.TorsoPitch * amount, right) * Quaternion.AngleAxis(pose.TorsoRoll * amount - hike, forward)
                                   * Quaternion.AngleAxis(pose.TorsoYaw * amount, up) * b.Torso.rotation;
            if (b.Head != null)
                b.Head.rotation = Quaternion.AngleAxis(pose.HeadPitch * amount, right) * Quaternion.AngleAxis(pose.HeadRoll * amount, forward)
                                  * Quaternion.AngleAxis(pose.HeadYaw * amount, up) * b.Head.rotation;

            // Sides by POSITION, not by bone name: the importer mirrors X.
            float sal = SideOf(b, b.ArmL, -1f), sar = SideOf(b, b.ArmR, 1f);
            SwingArm(b, b.ArmL, b.AlongL, sal, g, pose, amount);
            SwingArm(b, b.ArmR, b.AlongR, sar, g, pose, amount);

            // The legs: the stance leg locked to its planted point, the swing leg on the drawn curve.
            var legs = new[] { b.LegL, b.LegR };
            var axes = new[] { b.LegAxisL, b.LegAxisR };
            int stanceLeg = -1;
            for (int k = 0; k < 2; k++)
            {
                float side = SideOf(b, legs[k], k == 0 ? -1f : 1f);
                bool stance = (side < 0f) == leftStance;
                var curve = Limb(side, side < 0 ? pose.LegLeft : pose.LegRight, side < 0 ? pose.SplayLeft : pose.SplayRight);
                var dir = curve;
                var hip = legs[k].position;
                if (stance)
                {
                    stanceLeg = k;
                    if (!b.Stance[k] || !b.Planted[k])
                    {
                        // The footfall: the sole comes down where the drawn curve puts it.
                        b.Plant[k] = hip + b.Root.TransformDirection(curve) * b.Reach;
                        b.Plant[k].y = b.Position.y;
                        b.Planted[k] = true;
                    }
                    var d = b.Plant[k] - hip; d.y = 0f;
                    // Never further than the leg can span: past that the foot gives (a shove, a hard stop).
                    float span = b.Reach * .8f;
                    if (d.magnitude > span) { d = d.normalized * span; b.Plant[k] = new Vector3(hip.x + d.x, b.Plant[k].y, hip.z + d.z); }
                    var world = new Vector3(d.x, -Mathf.Sqrt(Mathf.Max(0f, b.Reach * b.Reach - d.sqrMagnitude)), d.z);
                    dir = b.Root.InverseTransformDirection(world.normalized);
                    b.Released[k] = dir;
                }
                else
                {
                    if (b.Stance[k]) b.ReleasedAt[k] = b.PhaseTotal;
                    // Off the lock and onto the curve over the first third of the swing, eased.
                    float into = Mathf.Abs(b.PhaseTotal - b.ReleasedAt[k]) / .5f;
                    if (b.Planted[k] && into < .35f) dir = Vector3.Slerp(b.Released[k], curve, Smooth(into / .35f));
                    else b.Planted[k] = false;
                }
                b.Stance[k] = stance;
                Aim(b, legs[k], axes[k], dir, amount);
            }
            b.StanceLeg = amount > .5f ? stanceLeg : -1;

            // The hips down (or up) so the lowest sole is on the pavement; a runner's flight on top.
            if (b.RootBone != null)
            {
                float low = float.MaxValue;
                for (int k = 0; k < 2; k++) low = Mathf.Min(low, (legs[k].position + legs[k].TransformDirection(axes[k]).normalized * b.Reach).y);
                float flight = Mathf.Max(0f, pose.RootUp) * b.Reach * amount * b.RunW;
                b.RootBone.position += up * (b.Position.y - low + flight);
            }
        }

        /// <summary>One arm's swing: the style's angle for that side (forward with the other
        /// side's leg), its swing about the carry scaled by the role's gain and carried forward by
        /// the role's carry; the pole arm held small.</summary>
        private static void SwingArm(Body b, Transform arm, Vector3 axis, float side, in Gait g, in GaitPose pose, float amount)
        {
            float angle = side < 0 ? pose.ArmLeft : pose.ArmRight;
            float carry = g.ArmCarry + b.ArmCarry * b.RunW;
            float swing = (angle - g.ArmCarry) * b.ArmGain;
            if (b.ShoulderPole && side > 0) swing = Mathf.Clamp(swing, -PoleArmSwing, PoleArmSwing);
            Aim(b, arm, axis, Limb(side, carry + swing, side < 0 ? pose.SpreadLeft : pose.SpreadRight), amount);
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

        /// <summary>How far above level the seated legs rest (degrees): the feet a little raised,
        /// so the thighs, not the heels, rest on the carton (the foot block juts 5.7 cm below the
        /// thigh at the far end of a level leg, and he rested on his heels with the thighs and
        /// seat floating over the carton: owner 2026-10-01, "the guy looks like hes floating").</summary>
        private const float SeatLegRaise = 10f;
        /// <summary>The legs' swing when he is fully down: level plus the raise.</summary>
        private const float SeatFull = 90f + SeatLegRaise;

        /// <summary>
        /// The beggar's sitting, drawn (see the class note). `SeatAlpha` is the legs' swing, 0
        /// hanging to <see cref="SeatFull"/>; the hips ride at the height that puts the legs' lowest
        /// point (their real mesh, <see cref="Hull"/>) on the ground, and on the carton's top once it
        /// is under him, so the feet slide forward along the pavement as he lowers himself and never
        /// lift or sink. ⚠️ THE SEAT IS ON THE CARTON TOO: these legs join the chest at its middle,
        /// so level legs held the chest's underside 14 cm over the carton; as he comes down the
        /// chest is lowered until its lowest corner rests on the carton (it moves down between the
        /// legs' pivots, which stay inside its width). `SeatAmount` blends the whole layer in and
        /// out at the standing ends, where it matches the idle.
        /// </summary>
        private void DrawSeat(Body b, float dt)
        {
            if (_beggar == null || b != _beggar.Body) return;
            BeggarSeatClearance = BeggarSeatRest = BeggarWaveLift = BeggarWaveOut = BeggarWaveInHead = float.NaN;
            if (b.SeatAmount <= 0f || b.LegL == null || b.LegR == null || b.RootBone == null || b.Torso == null) return;
            float a = Smooth(b.SeatAmount), s = PersonScale * b.Scale;
            float alpha = Mathf.Clamp(b.SeatAlpha, 0f, SeatFull), u = alpha / SeatFull, rad = Mathf.Min(alpha, 90f) * Mathf.Deg2Rad;
            var right = b.Root.right; var forward = b.Root.forward; var up = b.Root.up;

            ToBind(b, b.RootBone, a, true);
            ToBind(b, b.Torso, a, false);
            ToBind(b, b.Head, a, false);

            // The legs first (they set the hips' height): swung up to a little past level, a little apart.
            float sll = SideOf(b, b.LegL, -1f), slr = SideOf(b, b.LegR, 1f);
            float legDrop = 90f - alpha;
            Aim(b, b.LegL, b.LegAxisL, Reaching(sll, SeatLegSplay * u, legDrop), a);
            Aim(b, b.LegR, b.LegAxisR, Reaching(slr, SeatLegSplay * u, legDrop), a);

            // The hips: the legs' lowest point on the ground, on the carton's top as he comes down
            // onto it, plus the wind-up's rise and the stand-up's pop (SeatLift).
            float floor = b.Position.y + CartonTop * Smooth(u);
            float legLow = Mathf.Min(Lowest(b.LegL, b.LegHullL), Lowest(b.LegR, b.LegHullR));
            float hip = float.IsInfinity(legLow)
                ? (b.Reach * Mathf.Cos(rad) + SeatLegBack * s * Mathf.Sin(rad) + CartonTop * Smooth(u) - b.Reach) * a
                : (floor - legLow) * a;
            b.RootBone.position += up * (hip + b.SeatLift);

            // Breathing (seated only), a nod now and then, a look up at whoever passes.
            float seated = Smooth(Mathf.InverseLerp(.85f, 1f, u));
            float breath = Mathf.Sin(_clock * 2f * Mathf.PI * .23f) * seated;
            SeatLook(b, dt, seated);
            float look = Smooth(b.LookWeight);
            float nod = 0f;
            if (_clock - b.NodAt < 1f) nod = Pop(_clock - b.NodAt, .1f, .22f, .25f, .43f) * 9f;

            // The thank-you after a coin: a bow, then a wave with the free hand; both from and back to this pose.
            float bowAge = _clock - b.ThankAt;
            float bow = bowAge >= 0f && bowAge < BowSeconds ? Pop(bowAge, .12f, .26f, .22f, .5f) : 0f;
            float waveAge = bowAge - WaveFrom;
            float wave = waveAge >= 0f && waveAge < WaveSeconds ? Pop(waveAge, .1f, .25f, WaveSeconds - .7f, .35f) : 0f;
            float waveOn = Mathf.Clamp01(wave);
            // The landing's settle: back past rest, forward, still (a damped swing from the moment he lands).
            float land = _clock - b.LandAt;
            float settle = land >= 0f && land < 1.4f ? -9f * Mathf.Exp(-5f * land) * Mathf.Sin(11f * land) : 0f;

            // The wave arm's side (his right): the chest and head lean away from it so the raised
            // arm passes clear under the head's overhang.
            float waveSide = SideOf(b, b.ArmR, 1f) > 0 ? 1f : -1f;
            float lean = SeatLean * u + b.SeatLeanExtra + breath * 1.3f + bow * 16f + b.SeatReach * 16f + settle;
            float roll = b.SeatReach * 10f - waveSide * 10f * waveOn;
            float lookYaw = b.LookYaw * look;
            b.Torso.rotation = Quaternion.AngleAxis(lookYaw * .25f, up) * Quaternion.AngleAxis(lean * a, right)
                               * Quaternion.AngleAxis(-roll * a, forward) * b.Torso.rotation;
            if (b.Head != null)
            {
                float headPitch = SeatBow * u - look * (SeatBow + 5f) + nod + bow * 6f - breath * .6f - waveOn * 6f;
                float headRoll = -waveSide * WaveHeadTilt * waveOn;
                b.Head.rotation = Quaternion.AngleAxis(lookYaw * .75f, up) * Quaternion.AngleAxis(headPitch * a, right)
                                  * Quaternion.AngleAxis(-headRoll * a, forward) * b.Head.rotation;
            }

            // The seat: the chest lowered until its lowest corner rests on the carton (see the note).
            float sink = Smooth(Mathf.InverseLerp(.55f, 1f, u)) * a;
            float seatLow = Lowest(b.Torso, b.TorsoHull);
            if (sink > 0f && !float.IsInfinity(seatLow)) b.Torso.position += up * ((floor + .002f - seatLow) * sink);

            // The arms: at rest, the left (cup side) hand on the pavement and the right arm over his
            // bundle; while he lowers himself or gets up, both hands go down to the ground beside him
            // (SeatPush); reaching for his bundle, the right arm goes out to it (SeatReach); and the
            // wave: the right arm up to the side, the hand over the shoulder, rocking.
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
                Vector3 rest = side < 0 ? Reaching(side, SeatCupYaw, Drop(arm)) : Reaching(side, SeatBundleYaw, SeatBundleDrop);
                Vector3 push = Reaching(side, 40f, Drop(arm));
                var dir = Vector3.Slerp(rest, push, Smooth(b.SeatPush));
                if (side > 0 && b.SeatReach > 0f) dir = Vector3.SlerpUnclamped(dir, Reaching(side, 72f, 30f), b.SeatReach);
                if (side > 0 && wave != 0f)
                {
                    float t = Mathf.Max(0f, waveAge - .1f) * 2f * Mathf.PI * WaveRate;
                    var up2 = Reaching(side, WaveYaw + Mathf.Cos(t) * 10f, -(WaveLow + (WaveHigh - WaveLow) * (.5f + .5f * Mathf.Sin(t))));
                    dir = Vector3.SlerpUnclamped(dir, up2, wave);
                }
                dir.y -= breath * .012f;
                Aim(b, arm, axis, dir.normalized, arms);
                if (side > 0 && wave > .9f && b.Head != null)
                {
                    // The probe's check that the wave reads: the fist over the shoulder, and out beside the head.
                    var fist = arm.position + arm.TransformDirection(axis).normalized * armLength;
                    var outward = right * side;
                    float headOut = 0f;
                    foreach (var p in b.HeadHull) headOut = Mathf.Max(headOut, Vector3.Dot(b.Head.TransformPoint(p) - b.Head.position, outward));
                    BeggarWaveLift = fist.y - arm.position.y;
                    BeggarWaveOut = Vector3.Dot(fist - b.Head.position, outward) - headOut;
                    BeggarWaveInHead = Inside(b.Head, b.HeadHull, arm, arm == b.ArmL ? b.ArmHullL : b.ArmHullR);
                }
            }

            if (alpha > SeatFull - 8f && _beggar.Phase == Seated)
            {
                BeggarSeatClearance = Clearance(b);
                BeggarSeatRest = Lowest(b.Torso, b.TorsoHull) - (b.Position.y + CartonTop);
            }
        }

        /// <summary>The seated right arm rests out over his bundle (yaw out from ahead, degrees
        /// below level); the wave raises it to the side (yaw) rocking between two heights above
        /// level at `WaveRate` a second. ⚠️ No higher than WaveHigh: the head overhangs the
        /// shoulders by 0.3 m and the arm is 0.29 m thick, so a higher arm cut into the head even
        /// with the head tilted away (the probe measures it: SidewalkLife.BeggarWaveInHead).</summary>
        private const float SeatBundleYaw = 78f, SeatBundleDrop = 2f;
        private const float WaveYaw = 82f, WaveLow = 20f, WaveHigh = 35f, WaveRate = 2.6f, WaveHeadTilt = 20f;

        /// <summary>How deep (metres) the deepest corner of `limb`'s block goes into the box round
        /// `head`'s block, in the head's own frame (0: clear). The wave's clipping check.</summary>
        private static float Inside(Transform head, Vector3[] headHull, Transform limb, Vector3[] limbHull)
        {
            if (headHull.Length == 0 || limbHull.Length == 0) return float.NaN;
            Vector3 lo = headHull[0], hi = headHull[0];
            foreach (var p in headHull) { lo = Vector3.Min(lo, p); hi = Vector3.Max(hi, p); }
            float scale = head.lossyScale.x, deepest = 0f;
            foreach (var p in limbHull)
            {
                var q = head.InverseTransformPoint(limb.TransformPoint(p));
                float d = Mathf.Min(Mathf.Min(q.x - lo.x, hi.x - q.x), Mathf.Min(Mathf.Min(q.y - lo.y, hi.y - q.y), Mathf.Min(q.z - lo.z, hi.z - q.z)));
                deepest = Mathf.Max(deepest, d * scale);
            }
            return deepest;
        }

        /// <summary>
        /// THE POP CURVE every gesture here rides (owner 2026-10-01: "too linear and too unlively
        /// not poppy enough"): 0 at rest; over `wind` seconds a small dip the other way (the
        /// anticipation, -0.15 at its deepest); over `rise` a snap to full with an overshoot
        /// (BackOut, about 1.1); held for `hold`; over `fall` back to 0 with an ease in and out and
        /// a small settle past rest. Never linear.
        /// </summary>
        private static float Pop(float age, float wind, float rise, float hold, float fall)
        {
            if (age <= 0f) return 0f;
            if (age < wind) return -.15f * Mathf.Sin(age / wind * Mathf.PI);
            age -= wind;
            if (age < rise) return BackOut(age / rise);
            age -= rise;
            if (age < hold) return 1f;
            age -= hold;
            if (age < fall) { float x = age / fall; return 1f - Smooth(x) + .06f * Mathf.Sin(x * Mathf.PI) * x; }
            return 0f;
        }

        /// <summary>Ease out with an overshoot (the standard back curve, peak about 1.1).</summary>
        private static float BackOut(float x)
        {
            const float c = 1.70158f;
            x = Mathf.Clamp01(x) - 1f;
            return 1f + (c + 1f) * x * x * x + c * x * x;
        }

        /// <summary>Ease in: slow off the mark, fastest at the end (a drop landing hard).</summary>
        private static float EaseIn(float x) { x = Mathf.Clamp01(x); return x * x * (1.6f - .6f * x); }

        /// <summary>The lowest point of the drawn legs above the carton's top, world metres: the
        /// check that nothing sinks into it (their real mesh when it is readable).</summary>
        private float Clearance(Body b)
        {
            float low = Mathf.Min(Lowest(b.LegL, b.LegHullL), Lowest(b.LegR, b.LegHullR));
            if (!float.IsInfinity(low)) return low - (b.Position.y + CartonTop);
            float s = PersonScale * b.Scale; low = float.MaxValue;
            foreach (var (leg, axis) in new[] { (b.LegL, b.LegAxisL), (b.LegR, b.LegAxisR) })
            {
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

        /// <summary>
        /// The procedural beats over the drawn pose, each on the <see cref="Pop"/> curve (a wind-up
        /// the other way, a snap with an overshoot, a hold, an eased return with a small settle)
        /// and blended over whatever the walk or the seat drew (<see cref="AimPop"/> lets the
        /// wind-up and the overshoot through):
        ///   * the cheer: the left arm pulled down and back, then thrown up and shaken, the body
        ///     stretching on the throw;
        ///   * the clap (after the cheer): both hands forward, clapping four or five times;
        ///   * the standing bow, the head shake, the call's head lift;
        ///   * the laugh: little hops, a squash at each landing, the chest wiggling;
        ///   * the kids' juke: a snap lean back against the brake and into the turn.
        /// </summary>
        private void Overlays(Body b, float dt)
        {
            if (b.ArmL != null && b.CheerFrom > -90f && _clock - b.CheerFrom < CheerSeconds)
            {
                float age = _clock - b.CheerFrom, env = Pop(age, .12f, .22f, .62f, .44f);
                if (age >= .12f && age - dt < .12f) Kick(b, -.08f);
                float side = SideOf(b, b.ArmL, -1f);
                AimPop(b, b.ArmL, b.AlongL, Reaching(side, 48f + Mathf.Sin(age * 15f) * 12f * Mathf.Clamp01(env), -58f), env);
            }
            if (b.ArmL != null && b.ArmR != null && b.ClapUntil > _clock && _clock > b.ClapUntil - ClapSeconds)
            {
                float age = _clock - (b.ClapUntil - ClapSeconds), env = Pop(age, .08f, .16f, ClapSeconds - .52f, .28f);
                // Open and shut about 4.5 times a second; the hands meet in front of the chest.
                float open = .5f + .5f * Mathf.Cos(Mathf.Max(0f, age - .12f) * 2f * Mathf.PI * 4.5f);
                foreach (var arm in new[] { b.ArmL, b.ArmR })
                {
                    float side = SideOf(b, arm, arm == b.ArmL ? -1f : 1f);
                    AimPop(b, arm, arm == b.ArmL ? b.AlongL : b.AlongR, Reaching(side, -4f + open * 26f, 22f), env);
                }
            }
            if (b.Torso != null && _clock < b.BowUntil && b.SeatAmount <= 0f)
            {
                float env = Pop(1.4f - (b.BowUntil - _clock), .12f, .26f, .5f, .52f);
                b.Torso.rotation = Quaternion.AngleAxis(24f * env, b.Root.right) * b.Torso.rotation;
            }
            if (b.Head != null && _clock < b.ShakeUntil)
            {
                float age = 1.2f - (b.ShakeUntil - _clock), env = Mathf.Clamp01(Pop(age, .08f, .2f, .6f, .32f));
                b.Head.rotation = Quaternion.AngleAxis(Mathf.Sin(age * 14f) * 18f * env, b.Root.up) * b.Head.rotation;
            }
            if (b.LaughFrom > -90f && _clock - b.LaughFrom < LaughSeconds)
            {
                float age = _clock - b.LaughFrom, env = Mathf.Clamp01(Pop(age, .06f, .18f, .6f, .36f));
                // Hops on |sin|: a sharp landing each time, squashed as it lands.
                float hop = Mathf.Abs(Mathf.Sin(age * 9.4f)) * .07f * b.Scale * env;
                int landing = Mathf.FloorToInt(age * 9.4f / Mathf.PI);
                if (landing != b.Hops) { b.Hops = landing; if (env > .3f) Kick(b, .07f * env); }
                b.Root.position += Vector3.up * hop;
                if (b.Torso != null) b.Torso.rotation = Quaternion.AngleAxis(Mathf.Sin(age * 22f) * 7f * env, b.Root.forward) * b.Torso.rotation;
                if (b.Head != null) b.Head.rotation = Quaternion.AngleAxis(10f * env, b.Root.right) * b.Head.rotation;
            }
            if (_clock < b.JukeUntil && b.Torso != null)
            {
                // A juke: the kid snaps back against the brake and into the turn, then goes.
                float env = Pop(.4f - (b.JukeUntil - _clock), 0f, .12f, .08f, .2f);
                b.Torso.rotation = Quaternion.AngleAxis(-12f * env, b.Root.right) * Quaternion.AngleAxis(10f * env * b.JukeSide, b.Root.forward) * b.Torso.rotation;
            }
            if (b.Head != null && _clock < b.CallUntil)
            {
                float age = b.CallLength - (b.CallUntil - _clock);
                float env = Pop(age, .15f, .3f, Mathf.Max(0f, b.CallLength - .85f), .4f);
                // The head lifts to call (a smaller lift under a shoulder pole: the head's overhang sits 2 cm above it).
                b.Head.rotation = Quaternion.AngleAxis(-(b.ShoulderPole ? 2f : 12f) * env, b.Root.right) * b.Head.rotation;
            }
        }

        /// <summary>How long the cheer, the clap and the laugh last, seconds.</summary>
        private const float CheerSeconds = 1.4f, ClapSeconds = 1.25f, LaughSeconds = 1.2f;

        /// <summary><see cref="Aim"/> by an amount that may run past 0 and 1: under 0 the limb
        /// winds up the other way, over 1 it overshoots.</summary>
        private static void AimPop(Body b, Transform limb, Vector3 axis, Vector3 direction, float amount)
        {
            if (limb == null || Mathf.Abs(amount) < 1e-4f) return;
            var desired = b.Root.TransformDirection(direction);
            var current = limb.TransformDirection(axis);
            var target = Quaternion.FromToRotation(current, desired) * limb.rotation;
            limb.rotation = Quaternion.SlerpUnclamped(limb.rotation, target, amount);
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
            // A flat colour: the carton's drawing stays on the carton.
            m.mainTexture = null;
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

        /// <summary>
        /// THE CARTON'S OWN MESH (owner 2026-10-01: "the cardboard is untextured"): the flattened
        /// box, 0.62 by 0.72 m and 16 mm thick, with its far right corner torn away in a ragged
        /// curve, and UVs for `life_carton.png` (tools/author_ilalim_textures_life.py): the top face
        /// over v 0.125..1 (u across the width, v along the length, the far end at the top), the
        /// edges round the strip v 0..0.11 (the corrugated flutes). Flat-shaded; ToonSkin welds the
        /// outline normals as for every prop.
        /// </summary>
        private Mesh CartonMesh()
        {
            if (_meshes.TryGetValue("Carton", out var cached) && cached != null) return cached;
            const float hw = .31f, hl = .36f, h = .008f, v0 = .125f;
            var outline = new List<Vector2> { new Vector2(-hw, -hl), new Vector2(hw, -hl), new Vector2(hw, hl - .17f) };
            // The tear: ragged radii round the missing corner, from the right edge round to the far edge.
            float[] radii = { .165f, .182f, .158f, .186f, .17f };
            for (int k = 0; k < radii.Length; k++)
            {
                float angle = Mathf.Lerp(-80f, -170f, (k + 1f) / (radii.Length + 1f)) * Mathf.Deg2Rad;
                outline.Add(new Vector2(hw + Mathf.Cos(angle) * radii[k], hl + Mathf.Sin(angle) * radii[k]));
            }
            outline.Add(new Vector2(hw - .17f, hl));
            outline.Add(new Vector2(-hw, hl));
            var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var uvs = new List<Vector2>(); var triangles = new List<int>();
            void Tri(int a, int b, int c, Vector3 facing)
            {
                var n = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
                if (Vector3.Dot(n, facing) < 0f) { triangles.Add(a); triangles.Add(c); triangles.Add(b); }
                else { triangles.Add(a); triangles.Add(b); triangles.Add(c); }
            }
            Vector2 TopUv(Vector2 p) => new Vector2((p.x + hw) / (2f * hw), v0 + (1f - v0) * (p.y + hl) / (2f * hl));
            // The top and the underside: a fan from the middle (the outline is star-shaped from it).
            foreach (float y in new[] { h, -h })
            {
                var facing = y > 0f ? Vector3.up : Vector3.down;
                int centre = vertices.Count;
                vertices.Add(new Vector3(0f, y, 0f)); normals.Add(facing); uvs.Add(TopUv(Vector2.zero));
                foreach (var p in outline) { vertices.Add(new Vector3(p.x, y, p.y)); normals.Add(facing); uvs.Add(TopUv(p)); }
                for (int k = 0; k < outline.Count; k++) Tri(centre, centre + 1 + k, centre + 1 + (k + 1) % outline.Count, facing);
            }
            // The edges: one flat quad per outline segment, u along the perimeter.
            float perimeter = 0f;
            for (int k = 0; k < outline.Count; k++) perimeter += Vector2.Distance(outline[k], outline[(k + 1) % outline.Count]);
            float run = 0f;
            for (int k = 0; k < outline.Count; k++)
            {
                var a = outline[k]; var b = outline[(k + 1) % outline.Count];
                float length = Vector2.Distance(a, b);
                var d = (b - a).normalized;
                var outward = new Vector3(d.y, 0f, -d.x);
                if (Vector2.Dot(new Vector2(outward.x, outward.z), (a + b) * .5f) < 0f) outward = -outward;
                int i = vertices.Count;
                float u0 = run / perimeter, u1 = (run + length) / perimeter;
                vertices.Add(new Vector3(a.x, -h, a.y)); uvs.Add(new Vector2(u0, .005f));
                vertices.Add(new Vector3(b.x, -h, b.y)); uvs.Add(new Vector2(u1, .005f));
                vertices.Add(new Vector3(b.x, h, b.y)); uvs.Add(new Vector2(u1, .105f));
                vertices.Add(new Vector3(a.x, h, a.y)); uvs.Add(new Vector2(u0, .105f));
                for (int n = 0; n < 4; n++) normals.Add(outward);
                Tri(i, i + 1, i + 2, outward); Tri(i, i + 2, i + 3, outward);
                run += length;
            }
            var mesh = new Mesh { name = "Sidewalk carton" };
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uvs); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return _meshes["Carton"] = mesh;
        }

        private void BuildBeggarProps()
        {
            var f = BeggarFacing; f.y = 0f; f = f.sqrMagnitude > 1e-4f ? f.normalized : Vector3.right;
            var holder = new GameObject("Beggar's things").transform;
            holder.SetParent(transform, false);
            holder.SetPositionAndRotation(BeggarSeat, Quaternion.LookRotation(f, Vector3.up));
            var cube = Builtin("Cube.fbx"); var cylinder = Builtin("Cylinder.fbx");
            _carton = Part(holder, "Carton", CartonMesh(), Cardboard, new Vector3(0f, .008f, .12f), Vector3.one, Vector3.zero);
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
                    b.CheerFrom = _clock + lag; b.CheerUntil = b.CheerFrom + CheerSeconds;
                    b.LaughFrom = _clock + lag; b.LaughUntil = b.LaughFrom + LaughSeconds;
                    Later(lag + .05f, b, Cheer, .5f, 2f, 30f);
                    // Then the clap, once the arm is down: the hands meet on the clap sound.
                    if (Rand() < .7f) { b.ClapUntil = b.CheerUntil - .1f + ClapSeconds; Later(lag + CheerSeconds + .05f, b, Clap, .34f, 2f, 25f); }
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
                    // Turn to face the street, bend and lay the carton down, then sit on it: a wind-up
                    // (a little rise, a lean over his feet), a drop that speeds up into the carton, a
                    // squash as he lands and a settle back and forth (DrawSeat's `settle`).
                    float t = w.Clock += dt;
                    Walking(b, 0f, false);
                    b.Position = Vector3.Lerp(b.Position, BeggarSeat, 1f - Mathf.Exp(-dt * 5f));
                    Face(b, facing, dt, 200f, .16f);
                    bool bending = t >= SettleBendFrom && t < SettleBendTo;
                    Play(b, bending ? PickUpClip : Idle, .6f);
                    if (t >= SettleCarton && t - dt < SettleCarton) { SetProps(true); Sound(Pick(Carton), BeggarSeat, .32f, 1.5f, 12f, 1f, null); }
                    float wind = Mathf.Clamp01((t - (SitFrom - SitWind)) / SitWind);
                    float x = Mathf.Clamp01((t - SitFrom) / SitSeconds);
                    b.SeatAmount = Mathf.Max(b.SeatAmount, Smooth(wind));
                    b.SeatAlpha = SeatFull * EaseIn(x);
                    b.SeatLift = t < SitFrom ? .035f * Mathf.Sin(wind * Mathf.PI) : 0f;
                    b.SeatLeanExtra = t < SitFrom ? 8f * Mathf.Sin(wind * Mathf.PI) : 22f * Mathf.Sin(x * Mathf.PI) * (1f - x * .3f);
                    b.SeatPush = Mathf.Sin(Mathf.Clamp01(x * 1.15f) * Mathf.PI);
                    b.SeatArms = Smooth(x * 1.6f);
                    if (x >= 1f)
                    {
                        b.Position = BeggarSeat; b.SeatAlpha = SeatFull; b.SeatAmount = 1f; b.SeatLeanExtra = 0f; b.SeatPush = 0f; b.SeatArms = 1f; b.SeatLift = 0f;
                        b.LandAt = _clock; Kick(b, .15f);
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
                    // Reach over to his bundle; the wind-up (a deep lean over his feet, both hands to
                    // the pavement); a pop up onto his feet, a little past standing, stretching as he
                    // rises and settling; bend for the carton; go.
                    float t = w.Clock += dt;
                    Walking(b, 0f, false);
                    b.SeatReach = t < ReachSeconds ? Pop(t, .08f, .22f, .2f, .3f) : 0f;
                    float wind = Mathf.Clamp01((t - ReachSeconds) / StandWind);
                    float x = Mathf.Clamp01((t - ReachSeconds - StandWind) / StandSeconds);
                    if (t >= ReachSeconds)
                    {
                        b.SeatLeanExtra = 30f * Smooth(wind) * (1f - Smooth(x));
                        b.SeatPush = Smooth(wind) * (1f - Smooth(x));
                        b.SeatAlpha = SeatFull * (1f - BackOut(x));
                        b.SeatLift = .045f * Mathf.Sin(Mathf.Clamp01(x / .8f) * Mathf.PI);
                        b.SeatArms = 1f - Smooth((x - .35f) / .5f);
                        b.SeatAmount = 1f - Smooth((x - .75f) / .25f);
                        if (x >= .55f && x - dt / StandSeconds < .55f) Kick(b, -.1f);
                    }
                    float pick = t - ReachSeconds - StandWind - StandSeconds;
                    Play(b, pick > 0f && pick < PickSeconds ? PickUpClip : Idle, .6f);
                    if (pick >= PickSeconds * .5f && pick - dt < PickSeconds * .5f) { SetProps(false); Sound(Pick(Carton), BeggarSeat, .3f, 1.5f, 12f, 1.06f, null); }
                    if (pick >= PickSeconds + .15f)
                    {
                        b.SeatAmount = 0f; b.SeatAlpha = 0f; b.SeatReach = 0f; b.SeatPush = 0f; b.SeatArms = 0f; b.SeatLeanExtra = 0f; b.SeatLift = 0f;
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
                    Laugh(target.Body); Laugh(b);
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
                        k.Pause = .8f + (float)_rng.NextDouble() * .8f; Laugh(b);
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

        private void Laugh(Body b) { b.LaughFrom = _clock; b.LaughUntil = _clock + LaughSeconds; }

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
