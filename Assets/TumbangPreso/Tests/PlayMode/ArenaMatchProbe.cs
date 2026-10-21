using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Map;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// A WHOLE BOT MATCH ON THE ARENA (docs/ARENA_MAP_BRIEF.md, ARENA-1.1). `ArenaStageProbe`
    /// holds each layout still and walks it; this plays the map the way a lobby would: the scene
    /// loads through `SceneManager`, the arrival and the ready countdown run, four bots play
    /// every round on the layout the match id picks, and every break transforms the stage.
    /// It writes Logs/arena/unity/match_probe.txt (a PASS or a FAIL per item) and pictures
    /// (Logs/arena/unity/match_*.png).
    ///
    /// THE ROUND CLOCK AND THE ROUND COUNT ARE THE GAME'S OWN CUSTOM RULES (`CustomRules.Rounds`,
    /// `CustomRules.RoundSeconds`, pinned through `SceneFlow.PinSelectedRules`); no director is
    /// touched. Five rounds, so every layout is played once and the stage changes four times.
    ///
    /// ⚠️ IT RUNS FAST THROUGH A FIXED 1/60 s FRAME, NOT THROUGH `Time.timeScale`. The bots decide
    /// in `Update` on `Time.deltaTime`, and `BotBehaviourProbe.FixedStep` records what a time
    /// scale does to them (at 6x one build measured 530 and then 83 penalties; at a 1/30 s frame
    /// they stop tagging). `Time.captureDeltaTime` with the frame cap lifted plays as many
    /// 1/60 s frames a second as the machine draws, so the match is quicker than real time and
    /// the bots are the shipped ones. The report prints the speed it reached. The breaks run on
    /// the host's real clock and take their real eight seconds.
    ///
    /// Optional environment: TUMP_ARENA_MATCH_MODE (Classic, the default, or HeroStrike),
    /// TUMP_ARENA_MATCH_SECONDS (a round's length, 30 to 180, default 60),
    /// TUMP_ARENA_MATCH_ROUNDS (default 5), TUMP_ARENA_MATCH_OUT (the report's name, default
    /// match_probe), TUMP_ARENA_MATCH_HUMAN=1 (seat 1 is an idle player, for first-person HUD pictures).
    /// </summary>
    [Category("WallClock")]
    public sealed class ArenaMatchProbe
    {
        private const string Folder = "Logs/arena/unity";
        private const int Width = 1600, Height = 900;
        private const float FrameStep = 1.0f / 60.0f;
        /// <summary>A layout whose bots fall more often than this (per bot, per minute of live
        /// play) is called a FAIL: a fall costs about twelve seconds (3.3 falling on the updraft, 3.5 carried, 5 frozen), so one a minute is a fifth
        /// of a bot's round. The probe's own line, not a rule of the game.</summary>
        private const float FallRateLimit = 1.0f;
        /// <summary>A layout whose bots spend more than this share of live play standing still
        /// with a goal more than 3 m away is called a FAIL.</summary>
        private const float StallShareLimit = 0.15f;

        private static readonly RaycastHit[] Hits = new RaycastHit[24];

        private bool _bots, _spectator, _pinned;
        private int _seat, _frameRate, _vsync;
        private CustomRules _rules;
        private readonly List<string> _errors = new List<string>();

        [UnitySetUp]
        public IEnumerator Before()
        {
            _bots = GameLaunch.AllBots; _spectator = GameLaunch.Spectator; _seat = GameLaunch.SoloSeat;
            _pinned = SceneFlow.RulesPinned; _rules = SceneFlow.SelectedRules.Clone();
            _frameRate = Application.targetFrameRate; _vsync = QualitySettings.vSyncCount;
            yield return PlayModeWorld.Reset();
            Directory.CreateDirectory(Folder);
            Application.logMessageReceived += OnLog;
        }

        [UnityTearDown]
        public IEnumerator After()
        {
            Application.logMessageReceived -= OnLog;
            Time.captureDeltaTime = 0.0f;
            Time.timeScale = 1.0f;
            Application.targetFrameRate = _frameRate; QualitySettings.vSyncCount = _vsync;
            yield return PlayModeWorld.Reset();
            GameLaunch.AllBots = _bots; GameLaunch.Spectator = _spectator; GameLaunch.SoloSeat = _seat;
            SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
        }

        private void OnLog(string message, string stack, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert) return;
            if (_errors.Count >= 60) return;
            string first = stack != null ? stack.Split('\n').FirstOrDefault(l => l.Contains("TumbangPreso")) ?? "" : "";
            _errors.Add($"{type}: {message.Split('\n')[0]} {first.Trim()}");
        }

        private sealed class Fall
        {
            public int Round, Layout, Seat;
            public bool Defender, Launched, Stunned, Jumped, Boosted;
            public string Plan = "", Piece = "";
            public Vector3 Left, Caught, Down;
            public float At, Lowest, Airborne, CarriedFor = -1, FrozenFor = -1, MovingAfter = -1, LeftSpeed;
            public bool OnFloor, Tagged, Interrupted;
            public int Phase; // 1 carried, 2 frozen, 3 waiting to move, 4 done
            public float Mark;
        }

        /// <summary>A body settled well above the layout's floor: on another body, or on something that is not the stage.</summary>
        private sealed class Perch
        {
            public int Round, Layout, Seat, On = -1;
            public float At, Seconds, Height, Offset;
            public Vector3 Where;
            public string Plan = "", State = "", Under = "";
        }

        private sealed class SeatTrack
        {
            public Fall Open;
            public Vector3 LastGrounded, LastVelocity;
            public float LastGroundedAt, LastLaunchAt = -99, MaxRise;
            public bool HadGround, WasBoosted, WasStunned, Below;
            public float LastVy, SampleY;
            public bool SampleGrounded;
            public Perch Up;
            public readonly Queue<Vector3> Trail = new Queue<Vector3>();
        }

        private sealed class RoundRecord
        {
            public int Round, Defender, Wanted, Applied;
            public string Layout = "";
            public readonly List<string> Lines = new List<string>();
            public readonly List<string> Faults = new List<string>();
            public float Began, Ended = -1;
            public double BreakBegan = -1, BreakEnded = -1;
            public bool Travelled;
            /// <summary>The stage was seen moving in halftime's package (its replay and standings), before the show.</summary>
            public bool TravelledEarly;
            public int Knocks, Restores, Tags;
        }

        [UnityTest, Timeout(2400000)]
        public IEnumerator ABotMatchPlaysEveryLayoutAndEveryBreak()
        {
            var mode = string.Equals(Environment.GetEnvironmentVariable("TUMP_ARENA_MATCH_MODE"), "HeroStrike", StringComparison.OrdinalIgnoreCase)
                ? GameMode.HeroStrike : GameMode.Classic;
            int roundSeconds = Mathf.Clamp(EnvInt("TUMP_ARENA_MATCH_SECONDS", 60), CustomGameRules.MinRoundSeconds, CustomGameRules.MaxRoundSeconds);
            int rounds = Mathf.Clamp(EnvInt("TUMP_ARENA_MATCH_ROUNDS", 5), 3, CustomGameRules.MaxRounds);
            string outName = Environment.GetEnvironmentVariable("TUMP_ARENA_MATCH_OUT");
            if (string.IsNullOrEmpty(outName)) outName = "match_probe";

            // The pictures are named by round and layout, and the order changes with the match id.
            if (outName == "match_probe") foreach (var stale in Directory.GetFiles(Folder, "match_*.png")) File.Delete(stale);

            var rules = CustomGameRules.Defaults(mode);
            rules.Rounds = rounds; rules.RoundSeconds = roundSeconds;
            SceneFlow.PinSelectedRules(rules);
            // TUMP_ARENA_MATCH_HUMAN=1 leaves seat 1 to a player who never touches the keys, so the
            // pictures named _hud are that player's own first-person view and HUD at each round's
            // start. Its bot figures are three bots' and one statue's: read the all-bots run for those.
            bool human = Environment.GetEnvironmentVariable("TUMP_ARENA_MATCH_HUMAN") == "1";
            GameLaunch.AllBots = !human; GameLaunch.Spectator = false; GameLaunch.SoloSeat = 1;
            LogAssert.ignoreFailingMessages = true;
            UnityEngine.Random.InitState(20261005);

            GameServices.Ensure();
            var match = GameServices.Match;
            var round = GameServices.Round;
            Assert.IsNotNull(match, "No MatchDirector."); Assert.IsNotNull(round, "No RoundDirector.");

            var records = new List<RoundRecord>();
            var pending = new Queue<RoundRecord>();
            RoundRecord current = null;
            bool ended = false; int winner = -2;
            float clock = 0.0f; // game seconds of live play and of anything else that is not held
            var scored = new Dictionary<ScoreEvent, int>();
            int breaks = 0;

            Action<int, int> onRound = (number, defender) =>
            {
                if (current != null && current.BreakBegan >= 0 && current.BreakEnded < 0) current.BreakEnded = Time.realtimeSinceStartupAsDouble;
                current = new RoundRecord { Round = number, Defender = defender, Began = clock };
                records.Add(current); pending.Enqueue(current);
            };
            Action<int, int> onBreak = (next, defender) =>
            {
                breaks++;
                if (current != null) { current.Ended = clock; current.BreakBegan = Time.realtimeSinceStartupAsDouble; }
            };
            Action<int> onEnd = slot => { ended = true; winner = slot; if (current != null && current.Ended < 0) current.Ended = clock; };
            Action<int, ScoreEvent> onScore = (slot, e) => { scored.TryGetValue(e, out int n); scored[e] = n + 1; };
            Action<int, int> onTag = (defender, attacker) => { if (current != null) current.Tags++; };
            Action onRestore = () => { if (current != null) current.Restores++; };
            Action<bool> onUpright = upright => { if (!upright && current != null) current.Knocks++; };
            match.RoundStarted += onRound; match.IntermissionStarted += onBreak; match.MatchEnded += onEnd; match.Scored += onScore;
            round.Tagged += onTag; round.LataRestored += onRestore;

            // What `MatchHost.SeatOnFloor`'s own ray sees at the reset itself. Subscribed once the
            // match runs, so it is called after `SliceRunner.OnRoundStarted` in the same frame.
            CharacterMotor[] resetSeats = null;
            Action<int, int> onRoundAfter = (number, defender) =>
            {
                if (current == null || resetSeats == null) return;
                foreach (var who in resetSeats)
                {
                    if (who == null) continue;
                    Vector3 feet = who.transform.position;
                    var seen = Physics.RaycastAll(new Vector3(feet.x, who.SpawnPosition.y, feet.z) + Vector3.up * MatchHost.SpawnFloorProbeHeight, Vector3.down, MatchHost.SpawnFloorProbeDepth, ~0, QueryTriggerInteraction.Ignore)
                        .OrderBy(h => h.distance).Take(4).Select(h => $"{h.collider.name}{(h.collider.transform.IsChildOf(who.transform) ? " (its own body)" : "")} at y {h.point.y:F2}");
                    current.Lines.Add($"at the reset itself: seat {who.PlayerSlot} put at ({feet.x:F2}, {feet.y:F2}, {feet.z:F2}); a ray down from 2 m over its mark meets: {string.Join(", then ", seen)}");
                }
            };

            float realBegan = Time.realtimeSinceStartup;
            Lata lata = null;
            LateRecorder recorder = null;
            Camera witness = null;
            var report = new StringBuilder();
            // `seen` false: the bots never did the thing in this match, so the item is neither a pass nor a fail.
            var verdicts = new List<(string item, bool pass, string why, bool seen)>();
            try
            {
                var load = SceneManager.LoadSceneAsync(SceneFlow.Arena, LoadSceneMode.Single);
                Assert.IsNotNull(load, "The Arena scene is not in the build settings.");
                yield return ProbeWait.Done(load, "Arena scene load");

                // The arrival's shots and the ready countdown, as a player sits through them.
                float waited = 0.0f;
                while (waited < 90.0f && (records.Count == 0 || PresentationClock.Held || !round.RoundActive)) { waited += Time.unscaledDeltaTime; yield return null; }
                Assert.IsTrue(records.Count > 0 && round.RoundActive && !PresentationClock.Held,
                    $"No live round within 90 s of loading the Arena (rounds started {records.Count}, held {PresentationClock.Held}, round active {round.RoundActive}).");
                float arrival = Time.realtimeSinceStartup - realBegan;

                var stage = ArenaStage.Instance;
                Assert.IsNotNull(stage, "No ArenaStage in the loaded scene.");
                var main = Camera.main;
                Assert.IsNotNull(main, "No main camera in the loaded scene.");
                lata = round.Lata;
                if (lata != null) lata.UprightChanged += onUpright;
                var seats = round.Players.ToArray();
                resetSeats = seats; match.RoundStarted += onRoundAfter;
                var brains = seats.Select(s => s != null ? s.GetComponent<AIController>() : null).ToArray();
                var goalField = typeof(AIController).GetField("_goal", BindingFlags.Instance | BindingFlags.NonPublic);

                witness = new GameObject("ArenaMatchWitness").AddComponent<Camera>();
                witness.CopyFrom(main); witness.enabled = false; witness.targetTexture = null; witness.depth = -20.0f;
                witness.gameObject.AddComponent<TumbangPreso.Visual.ColourGrade>().AdoptFromScene();

                int layoutCount = stage.LayoutCount;
                var jumpPads = new JumpPad[layoutCount][]; var speedPads = new ArenaSpeedPad[layoutCount][]; var pickups = new ArenaStaminaPickup[layoutCount][];
                var pickupWas = new bool[layoutCount][];
                for (int l = 0; l < layoutCount; l++)
                {
                    var features = stage.Layouts[l].Features;
                    jumpPads[l] = features.GetComponentsInChildren<JumpPad>(true);
                    speedPads[l] = features.GetComponentsInChildren<ArenaSpeedPad>(true);
                    pickups[l] = features.GetComponentsInChildren<ArenaStaminaPickup>(true);
                    pickupWas[l] = pickups[l].Select(p => true).ToArray();
                }

                var live = new float[layoutCount];
                var jumpFires = new int[layoutCount]; var speedFires = new int[layoutCount]; var pickupTakes = new int[layoutCount];
                var stalled = new float[layoutCount];
                var stallSpots = new Dictionary<(int layout, int x, int z), float>();
                var falls = new List<Fall>();
                var perches = new List<Perch>();
                var tracks = seats.Select(s => new SeatTrack()).ToArray();
                var travelled = new float[seats.Length]; var lastAt = seats.Select(s => s != null ? s.transform.position : Vector3.zero).ToArray();
                int uncaught = 0; float lowestSeen = 0.0f; string lowestWho = "";
                float offFloorWorst = 0.0f; string offFloorWhere = "";
                float nextSample = 0.0f;
                int frames = 0, liveFrames = 0;

                recorder = new GameObject("Arena match recorder").AddComponent<LateRecorder>();
                recorder.OnLate = dt =>
                {
                    frames++;
                    if (stage == null || round == null) return;
                    bool held = PresentationClock.Held;
                    if (!held) clock += Time.deltaTime;
                    int layout = stage.Applied;
                    if (current != null && stage.Travelling && held) current.Travelled = true;
                    var breakNow = HalftimePresentation.Instance;
                    if (current != null && stage.Travelling && breakNow != null && breakNow.Active && !breakNow.StageShowPlaying) current.TravelledEarly = true;
                    bool play = round.RoundActive && !held && match.MatchInProgress && layout >= 0;
                    if (play) { live[layout] += Time.deltaTime; liveFrames++; }

                    // Pickups: an orb that was there and is not.
                    if (layout >= 0)
                        for (int k = 0; k < pickups[layout].Length; k++)
                        {
                            bool there = pickups[layout][k] != null && pickups[layout][k].isActiveAndEnabled && pickups[layout][k].Available;
                            if (play && pickupWas[layout][k] && !there && pickups[layout][k] != null && pickups[layout][k].isActiveAndEnabled) pickupTakes[layout]++;
                            pickupWas[layout][k] = there;
                        }

                    bool sample = play && clock >= nextSample;
                    if (sample) nextSample = clock + 0.5f;

                    for (int i = 0; i < seats.Length; i++)
                    {
                        var who = seats[i]; var track = tracks[i];
                        if (who == null || !who.gameObject.activeInHierarchy) continue;
                        Vector3 at = who.transform.position;
                        if (play) { var step = at - lastAt[i]; step.y = 0.0f; if (step.magnitude < 3.0f) travelled[i] += step.magnitude; }
                        lastAt[i] = at;
                        if (at.y < lowestSeen) { lowestSeen = at.y; lowestWho = $"seat {i} at ({at.x:F1}, {at.y:F2}, {at.z:F1}) in round {match.RoundNumber}"; }

                        // A body under the line where the host refuses its poses was not caught in time.
                        if (at.y < ArenaStage.MoveFloorY) { if (!track.Below) { track.Below = true; uncaught++; } }
                        else if (at.y > -1.0f) track.Below = false;

                        float vy = who.Velocity.y;
                        if (play && layout >= 0)
                        {
                            // A jump pad: an upward speed no jump gives, beside a pad.
                            if (track.LastVy < Balance.JumpVelocity + 2.0f && vy >= Balance.JumpVelocity + 2.0f)
                                foreach (var pad in jumpPads[layout])
                                    if (pad != null && Flat(pad.transform.position, at) < pad.Radius + 1.5f && vy >= pad.LaunchSpeed * 0.5f)
                                    { jumpFires[layout]++; track.LastLaunchAt = clock; break; }
                            // A speed pad: a boost that begins beside one.
                            if (!track.WasBoosted && who.IsSpeedBoosted)
                                foreach (var pad in speedPads[layout])
                                    if (pad != null && Flat(pad.transform.position, at) < Mathf.Max(pad.HalfSize.x, pad.HalfSize.y) + 2.0f) { speedFires[layout]++; break; }
                        }
                        track.LastVy = vy; track.WasBoosted = who.IsSpeedBoosted;

                        if (who.IsGrounded && !who.IsEdgeRecovering)
                        {
                            track.LastGrounded = at; track.LastGroundedAt = clock; track.HadGround = true; track.MaxRise = 0.0f; track.LastVelocity = who.Velocity;
                        }
                        else track.MaxRise = Mathf.Max(track.MaxRise, vy);

                        var fall = track.Open;
                        if (fall == null)
                        {
                            if (who.EdgeKind == EdgeRecoveryKind.Drone)
                            {
                                Vector3 leftVelocity = track.LastVelocity; leftVelocity.y = 0.0f;
                                fall = track.Open = new Fall
                                {
                                    Round = match.RoundNumber, Layout = layout, Seat = i, Defender = who.IsDefender,
                                    Launched = clock - track.LastLaunchAt < 6.5f, Stunned = track.WasStunned, Jumped = track.MaxRise > 2.0f,
                                    Boosted = who.IsSpeedBoosted, Plan = brains[i] != null ? brains[i].Plan.ToString() : "-",
                                    Left = track.HadGround ? track.LastGrounded : at, Caught = at, At = clock, Lowest = at.y,
                                    Airborne = clock - track.LastGroundedAt, LeftSpeed = leftVelocity.magnitude, Phase = 1, Mark = clock,
                                };
                                if (layout >= 0 && Ground(stage.Layouts[layout].Colliders.transform, fall.Left + Vector3.up * 0.6f, 2.0f, out var under)) fall.Piece = under.collider.name;
                                falls.Add(fall);
                            }
                        }
                        else
                        {
                            fall.Lowest = Mathf.Min(fall.Lowest, at.y);
                            if (match.RoundNumber != fall.Round || !match.MatchInProgress) { fall.Interrupted = true; track.Open = null; }
                            else if (fall.Phase == 1 && who.EdgeKind != EdgeRecoveryKind.Drone)
                            {
                                fall.CarriedFor = clock - fall.Mark; fall.Down = at; fall.Tagged = who.IsTagged; fall.Mark = clock; fall.Phase = 2;
                                fall.OnFloor = layout >= 0 && Ground(stage.Layouts[layout].Colliders.transform, at + Vector3.up * 0.5f, 1.0f, out var landed)
                                               && Mathf.Abs(landed.point.y - at.y) < 0.2f;
                            }
                            else if (fall.Phase == 2 && !who.IsTagged) { fall.FrozenFor = clock - fall.Mark; fall.Mark = clock; fall.Phase = 3; }
                            else if (fall.Phase == 3 && Flat(at, fall.Down) > 0.5f) { fall.MovingAfter = clock - fall.At; fall.Phase = 4; track.Open = null; }
                            else if (fall.Phase == 3 && !round.RoundActive) { fall.Interrupted = true; track.Open = null; }
                        }
                        track.WasStunned = who.IsStunned && !who.IsEdgeRecovering;

                        if (!sample) continue;
                        // A grounded body is on the layout's floor: not sunk into a platform, not floating over it.
                        // Two samples running at one height: `IsGrounded` is still true on the frame a hit
                        // or a launch lifts a body, and one such frame read as a body standing in the air.
                        bool settled = who.IsGrounded && !who.IsEdgeRecovering && track.SampleGrounded && Mathf.Abs(at.y - track.SampleY) < 0.03f;
                        track.SampleGrounded = who.IsGrounded && !who.IsEdgeRecovering; track.SampleY = at.y;
                        bool floored = Ground(stage.Layouts[layout].Colliders.transform, at + Vector3.up * 0.6f, 4.0f, out var floor);
                        if (settled && floored && at.y - floor.point.y > 0.6f)
                        {
                            if (track.Up == null)
                            {
                                track.Up = new Perch { Round = match.RoundNumber, Layout = layout, Seat = i, At = clock, Height = at.y - floor.point.y, Where = at,
                                                       Plan = brains[i] != null ? brains[i].Plan.ToString() : "-",
                                                       State = (who.IsDefender ? "taya " : "") + (who.IsTagged ? "tagged " : "") + (who.IsStunned ? "stunned " : "") + (who.IsTripped ? "tripped " : "") };
                                float best = 1.2f;
                                for (int j = 0; j < seats.Length; j++)
                                {
                                    if (j == i || seats[j] == null) continue;
                                    Vector3 other = seats[j].transform.position;
                                    if (Flat(other, at) < best && at.y - other.y > 0.8f && at.y - other.y < 1.8f)
                                    { best = Flat(other, at); track.Up.On = j; track.Up.Offset = best; track.Up.Under = (seats[j].IsDefender ? "taya " : "") + (seats[j].IsTagged ? "tagged " : "") + (seats[j].IsStunned ? "stunned " : "") + (seats[j].IsEdgeRecovering ? "carried " : ""); }
                                }
                                perches.Add(track.Up);
                            }
                            track.Up.Seconds = clock - track.Up.At + 0.5f;
                        }
                        else track.Up = null;
                        if (settled && floored)
                        {
                            float off = at.y - floor.point.y;
                            if (Mathf.Abs(off) > Mathf.Abs(offFloorWorst))
                            {
                                // What it is really standing on, whatever that belongs to.
                                string on = "nothing";
                                foreach (var met in Physics.RaycastAll(at + Vector3.up * 0.6f, Vector3.down, 1.2f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
                                    if (!met.collider.transform.IsChildOf(who.transform)) { on = $"{met.collider.name} (under {met.collider.transform.root.name}) at y {met.point.y:F2}"; break; }
                                offFloorWorst = off; offFloorWhere = $"seat {i} at ({at.x:F1}, {at.y:F2}, {at.z:F1}) over {floor.collider.name} at {floor.point.y:F2}, layout '{stage.Layouts[layout].Name}', round {match.RoundNumber} t {clock:F0}; standing on {on}";
                            }
                        }
                        // Standing still with somewhere to go: three seconds, under 0.6 m, a goal over 3 m away.
                        track.Trail.Enqueue(at);
                        while (track.Trail.Count > 7) track.Trail.Dequeue();
                        bool free = !who.IsStunned && !who.IsEdgeRecovering && !who.IsTagged;
                        if (!free) { track.Trail.Clear(); continue; }
                        if (track.Trail.Count < 7 || brains[i] == null || goalField == null) continue;
                        var goal = (Vector3)goalField.GetValue(brains[i]);
                        if (Flat(track.Trail.Peek(), at) < 0.6f && Flat(goal, at) > 3.0f)
                        {
                            stalled[layout] += 0.5f;
                            var key = (layout, Mathf.RoundToInt(at.x / 2.0f) * 2, Mathf.RoundToInt(at.z / 2.0f) * 2);
                            stallSpots.TryGetValue(key, out float seconds); stallSpots[key] = seconds + 0.5f;
                        }
                    }
                };

                // From here the frame is a fixed 1/60 s and the cap is off.
                QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;
                Time.captureDeltaTime = FrameStep;
                float playBegan = Time.realtimeSinceStartup;

                bool breakShots = false;
                // The guard is game time and real time. Not frames: a break draws thousands of zero-length ones.
                float budget = rounds * roundSeconds * 1.5f + 60.0f;
                while (!ended && clock < budget && Time.realtimeSinceStartup - realBegan < 2100.0f)
                {
                    if (pending.Count > 0)
                    {
                        var record = pending.Dequeue();
                        // Two frames: the stage's LateUpdate has shown the layout, physics has the colliders.
                        yield return null; yield return new WaitForFixedUpdate(); yield return null;
                        Describe(record, stage, seats, lata, match);
                        float can = stage.CanHeight;
                        string tag = $"{(outName == "match_probe" ? "match" : outName)}_r{record.Round}_{record.Layout}";
                        Place(witness, new Vector3(2.6f, can + 1.5f, -2.9f), new Vector3(0.0f, can + 0.3f, 0.0f), 50.0f);
                        yield return GameplayShots.Render(witness, tag + "_can", false, Folder, null, Width, Height);
                        Place(witness, new Vector3(10.0f, can + 8.0f, 19.0f), new Vector3(0.0f, can + 0.5f, 3.5f), 50.0f);
                        yield return GameplayShots.Render(witness, tag + "_spawn", false, Folder, null, Width, Height);
                        main = Camera.main;
                        if (main != null) yield return GameplayShots.Render(main, tag + "_hud", true, Folder, null, Width, Height);
                        continue;
                    }

                    // Three pictures across the first break, from the camera the player is shown.
                    var hp = HalftimePresentation.Instance;
                    if (!breakShots && hp != null && hp.Active && !hp.IsHalftime)
                    {
                        breakShots = true;
                        int from = ArenaStage.LayoutFor(hp.MatchId, hp.CompletedRound, layoutCount), to = ArenaStage.LayoutFor(hp.MatchId, hp.CompletedRound + 1, layoutCount);
                        string tag = $"{(outName == "match_probe" ? "match" : outName)}_break_{stage.Layouts[from].Name}_to_{stage.Layouts[to].Name}";
                        foreach (var (age, name) in new[] { (1.5f, "1_hologram"), (4.6f, "2_mid_travel"), (7.3f, "3_settled") })
                        {
                            while (hp.Active && SharedUltimatePhase.Now - hp.Began < age) yield return null;
                            if (!hp.Active) { report.AppendLine($"  (the break ended before its picture at {age} s)"); break; }
                            var breakCamera = Object.FindFirstObjectByType<ArenaBreakCamera>();
                            var shown = breakCamera != null && breakCamera.GetComponent<Camera>().enabled ? breakCamera.GetComponent<Camera>() : Camera.main;
                            yield return GameplayShots.Render(shown, $"{tag}_{name}", true, Folder, null, Width, Height);
                        }
                        continue;
                    }

                    yield return null;
                }

                float realPlay = Time.realtimeSinceStartup - playBegan;
                Time.captureDeltaTime = 0.0f;
                recorder.OnLate = null;
                for (int i = 0; i < 20; i++) yield return null;

                // ------------------------------------------------------------ the report
                float liveTotal = live.Sum();
                report.AppendLine($"ARENA MATCH PROBE, {SceneFlow.Arena}, {mode}, {rounds} rounds of {roundSeconds} s (CustomRules, pinned), {(human ? "three bots and an idle player in seat 1 (the _hud pictures are that player's view)" : "four bots (GameLaunch.AllBots)")}");
                report.AppendLine($"Arrival and countdown took {arrival:F1} s. Then {frames} frames at a fixed 1/60 s: {clock:F0} s of game time ({liveTotal:F0} s of live rounds) in {realPlay:F0} s of real time, " +
                                  $"breaks included; live play ran at about {(liveFrames > 0 && realPlay > 0 ? liveTotal / Mathf.Max(1.0f, realPlay - (float)records.Where(r => r.BreakBegan >= 0 && r.BreakEnded >= 0).Sum(r => r.BreakEnded - r.BreakBegan)) : 0):F1}x real time.");
                report.AppendLine($"Match ended: {ended} (winner seat {winner}); rounds started {records.Count}; breaks {breaks}; scores {string.Join(", ", Enumerable.Range(0, Balance.PlayerCount).Select(s => match.ScoreFor(s)))}.");
                report.AppendLine($"Travelled in live play: {string.Join(", ", travelled.Select((t, i) => $"seat {i} {t:F0} m"))}.");
                report.AppendLine();

                void Verdict(string item, bool pass, string why, bool seen = true) { verdicts.Add((item, pass, why, seen)); }

                int distinct = records.Select(r => r.Applied).Distinct().Count();
                int changes = 0; for (int i = 1; i < records.Count; i++) if (records[i].Applied != records[i - 1].Applied) changes++;
                Verdict("ROUNDS", ended && records.Count == rounds && changes >= 2,
                    $"{records.Count} of {rounds} rounds started, the match {(ended ? "ended" : "DID NOT END")}, {distinct} different layouts, {changes} changes of layout");

                report.AppendLine("ROUNDS");
                bool agree = true, spawns = true, canOk = true;
                foreach (var record in records)
                {
                    report.AppendLine($"  round {record.Round}: layout {record.Applied} '{record.Layout}' (the match id asks for {record.Wanted}), taya seat {record.Defender}, " +
                                      $"live {record.Began:F0} to {(record.Ended >= 0 ? record.Ended.ToString("F0") : "?")} s; can knocked {record.Knocks}, restored {record.Restores}, tags {record.Tags}" +
                                      (record.BreakBegan >= 0 ? $"; break after it {(record.BreakEnded >= 0 ? (record.BreakEnded - record.BreakBegan).ToString("F2") + " s" : "never ended")}, stage seen travelling {record.Travelled}" : ""));
                    foreach (var line in record.Lines) report.AppendLine("    " + line);
                    foreach (var fault in record.Faults)
                    {
                        report.AppendLine("    FAULT " + fault);
                        if (fault.StartsWith("layout", StringComparison.Ordinal)) agree = false;
                        else if (fault.StartsWith("can", StringComparison.Ordinal)) canOk = false;
                        else spawns = false;
                    }
                }
                Verdict("LAYOUT: colliders and visuals agree at every round start", agree && records.Count > 0, agree ? "every round" : "see the FAULT lines under ROUNDS");
                Verdict("SPAWNS: every seat stood on floor at every round start", spawns && records.Count > 0, spawns ? "every seat, every round" : "see the FAULT lines under ROUNDS");
                Verdict("CAN ON ITS MARK at every can height", canOk && records.Count > 0, canOk ? "upright on the centre floor at each round start" : "see the FAULT lines under ROUNDS");
                report.AppendLine();

                report.AppendLine("FALLS (game seconds)");
                bool fallsOk = uncaught == 0;
                foreach (var fall in falls)
                {
                    string how = fall.Launched ? "after a jump pad" : fall.Stunned ? "while stunned or tripped" : fall.Jumped ? "jumped" : "walked or was pushed";
                    string after = fall.Interrupted ? "the round ended first"
                        : fall.Phase < 4 ? $"UNFINISHED at phase {fall.Phase}"
                        : $"carried {fall.CarriedFor:F2} s, set down at ({fall.Down.x:F1}, {fall.Down.y:F2}, {fall.Down.z:F1}) {(fall.OnFloor ? "on floor" : "NOT ON FLOOR")}, " +
                          $"{(fall.Tagged ? "frozen" : "NOT FROZEN")} {fall.FrozenFor:F2} s, moving again {fall.MovingAfter:F1} s after the catch";
                    report.AppendLine($"  t {fall.At,5:F0} round {fall.Round} '{stage.Layouts[Mathf.Max(0, fall.Layout)].Name}' seat {fall.Seat}{(fall.Defender ? " (taya)" : "")} plan {fall.Plan}: left {fall.Piece} at ({fall.Left.x:F1}, {fall.Left.y:F1}, {fall.Left.z:F1}) " +
                                      $"at {fall.LeftSpeed:F1} m/s, {how}, {fall.Airborne:F2} s in the air; caught at ({fall.Caught.x:F1}, {fall.Caught.y:F2}, {fall.Caught.z:F1}), lowest {fall.Lowest:F2}; {after}");
                    if (fall.Lowest <= ArenaStage.MoveFloorY) fallsOk = false;
                    // The updraft (`ArenaFallRecovery.Updraft`): from a deck to the catch is over 3 s. Under 2.5 s in
                    // the air is the game's own gravity, which reaches the line in 1.4 s.
                    if (fall.Airborne < 2.5f) { fallsOk = false; report.AppendLine($"    TOO FAST: {fall.Airborne:F2} s in the air, the updraft's fall is 3.0 to 3.5 s"); }
                    if (fall.Interrupted) continue;
                    if (fall.Phase >= 2 && (!fall.OnFloor || !fall.Tagged)) fallsOk = false;
                    if (fall.Phase >= 3 && Mathf.Abs(fall.FrozenFor - StatusRules.TaggedSeconds) > 0.6f) fallsOk = false;
                    if (fall.Phase < 4 && clock - fall.At > 20.0f) fallsOk = false;
                }
                if (falls.Count == 0) report.AppendLine("  none");
                report.AppendLine($"  bodies seen under y {ArenaStage.MoveFloorY} (not caught in time): {uncaught}; the lowest any body reached: {lowestSeen:F2} ({lowestWho})");
                Verdict($"FALLS: every fall caught above y {ArenaStage.MoveFloorY:F0} (the catch is at {ArenaStage.CatchY:F1}), set down on floor, frozen the tag's 5 s, then moving", fallsOk,
                    falls.Count == 0 ? "no bot fell, so nothing was exercised here (ArenaStageProbe drops one)" : $"{falls.Count} falls, {falls.Count(f => f.Phase == 4)} followed to the end, {falls.Count(f => f.Interrupted)} cut short by a round's end, {uncaught} uncaught",
                    falls.Count > 0 || uncaught > 0);
                report.AppendLine();

                report.AppendLine("FALL RATE AND STALLS, per layout (four bots)");
                bool rateOk = true, stallOk = true;
                for (int l = 0; l < layoutCount; l++)
                {
                    if (live[l] <= 0.0f) { report.AppendLine($"  {stage.Layouts[l].Name,-10} not played"); continue; }
                    int count = falls.Count(f => f.Layout == l);
                    float botMinutes = live[l] * seats.Length / 60.0f, rate = count / botMinutes, share = stalled[l] / (live[l] * seats.Length);
                    if (rate > FallRateLimit) rateOk = false;
                    if (share > StallShareLimit) stallOk = false;
                    string spots = string.Join("; ", stallSpots.Where(s => s.Key.layout == l).OrderByDescending(s => s.Value).Take(3).Select(s => $"({s.Key.x}, {s.Key.z}) {s.Value:F0} s"));
                    report.AppendLine($"  {stage.Layouts[l].Name,-10} {live[l],4:F0} s live, {count} falls = {rate:F2} per bot per minute; stalled {stalled[l]:F0} bot-seconds = {share * 100.0f:F1}% of bot time" +
                                      (spots.Length > 0 ? $", most at {spots}" : ""));
                }
                Verdict($"FALL RATE: no layout over {FallRateLimit:F1} falls per bot per minute", rateOk, $"{falls.Count} falls in {liveTotal * seats.Length / 60.0f:F1} bot-minutes overall = {(liveTotal > 0 ? falls.Count / (liveTotal * seats.Length / 60.0f) : 0):F2} per bot per minute");
                Verdict($"STALLS: no layout with bots stood still with a far goal over {StallShareLimit * 100.0f:F0}% of the time", stallOk, $"{stalled.Sum():F0} bot-seconds overall");
                report.AppendLine($"  a grounded body's worst height off the floor under it (sampled twice a second): {offFloorWorst:+0.00;-0.00} m ({offFloorWhere})");
                foreach (var perch in perches)
                    report.AppendLine($"  PERCH t {perch.At:F0} round {perch.Round} '{stage.Layouts[perch.Layout].Name}' seat {perch.Seat} ({perch.State.Trim()}; plan {perch.Plan}) settled {perch.Height:F2} m over the floor at ({perch.Where.x:F1}, {perch.Where.y:F2}, {perch.Where.z:F1}) for about {perch.Seconds:F1} s, " +
                                      (perch.On >= 0 ? $"on seat {perch.On} ({perch.Under.Trim()}), {perch.Offset:F2} m off its axis" : "on no other body"));
                Verdict("STANDING: no grounded body more than 0.25 m off the floor under it", Mathf.Abs(offFloorWorst) <= 0.25f,
                    $"worst {offFloorWorst:+0.00;-0.00} m; {perches.Count} times a body settled more than 0.6 m up, {perches.Sum(x => x.Seconds):F1} s in all");
                report.AppendLine();

                int knocks = records.Sum(r => r.Knocks), restores = records.Sum(r => r.Restores), tags = records.Sum(r => r.Tags);
                scored.TryGetValue(ScoreEvent.LataKnocked, out int knockScores); scored.TryGetValue(ScoreEvent.Tag, out int tagScores);
                Verdict("CAN: knocked down and restored", knocks > 0 && restores > 0, $"went down {knocks} times ({knockScores} LataKnocked awards), restored {restores} times");
                Verdict("TAG: a tag happened", tags > 0, $"{tags} tags ({tagScores} Tag awards)");
                report.AppendLine("SCORE EVENTS: " + (scored.Count == 0 ? "none" : string.Join(", ", scored.OrderBy(s => s.Key.ToString()).Select(s => $"{s.Key} {s.Value}"))));
                report.AppendLine();

                var played = records.Where(r => r.BreakBegan >= 0).ToList();
                bool breaksOk = breaks == rounds - 1 && played.Count == breaks && played.All(r => r.BreakEnded >= 0 && Math.Abs(r.BreakEnded - r.BreakBegan - HalftimePresentation.DurationFor(HalftimePresentation.IsMiddleBreak(r.Round, rounds))) < 1.0 && r.Travelled && !r.TravelledEarly);
                Verdict("BREAKS: each round's end plays the break, the stage travels, the next round starts", breaksOk,
                    $"{breaks} breaks for {rounds} rounds; lengths {string.Join(", ", played.Select(r => r.BreakEnded >= 0 ? (r.BreakEnded - r.BreakBegan).ToString("F2") : "open"))} s (the map's break is {ArenaStage.BreakSeconds:F0} s; its halftime is {HalftimePresentation.HalftimeDuration:F0} s of replay and standings and then that show, {HalftimePresentation.DurationFor(true):F0} s){(played.Any(r => r.TravelledEarly) ? "; THE STAGE MOVED DURING HALFTIME'S REPLAY OR STANDINGS" : "")}; pictures {(breakShots ? "written (match_break_*.png)" : "NOT written")}");

                report.AppendLine("PADS AND PICKUPS, fires per layout (jump pads / speed pads / stamina pickups taken)");
                for (int l = 0; l < layoutCount; l++)
                    report.AppendLine($"  {stage.Layouts[l].Name,-10} has {jumpPads[l].Length} / {speedPads[l].Length} / {pickups[l].Length}; fired {jumpFires[l]} / {speedFires[l]} / {pickupTakes[l]}");
                // The bots do not go looking for a pad or an orb; they only cross one. `ArenaStageProbe` stands a body on each.
                Verdict("JUMP PADS fire in bot play", jumpFires.Sum() > 0, $"{jumpFires.Sum()} launches", jumpFires.Sum() > 0);
                Verdict("SPEED PADS fire in bot play", speedFires.Sum() > 0, $"{speedFires.Sum()} boosts begun on a pad", speedFires.Sum() > 0);
                Verdict("STAMINA PICKUPS are taken in bot play", pickupTakes.Sum() > 0, $"{pickupTakes.Sum()} taken", pickupTakes.Sum() > 0);
                report.AppendLine();

                Verdict("LOG: no exception or error", _errors.Count == 0, _errors.Count == 0 ? "none" : $"{_errors.Count} (listed below)");
                if (_errors.Count > 0)
                {
                    report.AppendLine("EXCEPTIONS AND ERRORS IN THE LOG");
                    foreach (var group in _errors.GroupBy(e => e).OrderByDescending(g => g.Count()).Take(20)) report.AppendLine($"  {group.Count()} x {group.Key}");
                    report.AppendLine();
                }
            }
            finally
            {
                match.RoundStarted -= onRoundAfter;
                match.RoundStarted -= onRound; match.IntermissionStarted -= onBreak; match.MatchEnded -= onEnd; match.Scored -= onScore;
                round.Tagged -= onTag; round.LataRestored -= onRestore;
                if (lata != null) lata.UprightChanged -= onUpright;
                if (recorder != null) Object.Destroy(recorder.gameObject);
                if (witness != null) Object.Destroy(witness.gameObject);
                Time.captureDeltaTime = 0.0f;
            }

            var head = new StringBuilder();
            int failed = verdicts.Count(v => v.seen && !v.pass), unseen = verdicts.Count(v => !v.seen);
            head.AppendLine((failed == 0 ? "PASS" : $"FAIL ({failed} of {verdicts.Count} items)") + (unseen > 0 ? $", {unseen} not seen in this match" : ""));
            foreach (var (item, pass, why, seen) in verdicts) head.AppendLine($"{(!seen ? "NOT SEEN" : pass ? "PASS" : "FAIL")}  {item}: {why}");
            head.AppendLine();
            string text = head.ToString() + report;
            File.WriteAllText($"{Folder}/{outName}.txt", text);
            Debug.Log(text);
            Assert.AreEqual(0, failed, "The Arena match probe failed:\n" + text);
        }

        private static int EnvInt(string name, int fallback)
            => int.TryParse(Environment.GetEnvironmentVariable(name), out int value) ? value : fallback;

        private static float Flat(Vector3 a, Vector3 b)
        {
            a.y = 0.0f; b.y = 0.0f;
            return Vector3.Distance(a, b);
        }

        private static void Place(Camera camera, Vector3 at, Vector3 look, float fov)
        {
            camera.transform.SetPositionAndRotation(at, Quaternion.LookRotation(look - at));
            camera.fieldOfView = fov;
        }

        /// <summary>The highest top of the layout's own colliders under a point, within `depth`.</summary>
        private static bool Ground(Transform floor, Vector3 from, float depth, out RaycastHit best)
        {
            best = default;
            bool found = false;
            int count = Physics.RaycastNonAlloc(from, Vector3.down, Hits, depth, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (!Hits[i].collider.transform.IsChildOf(floor)) continue;
                if (found && Hits[i].point.y <= best.point.y) continue;
                best = Hits[i];
                found = true;
            }
            return found;
        }

        /// <summary>
        /// WHAT IS DRAWN AGAINST WHAT IS STOOD ON, over the whole layout. A 0.4 m grid of rays
        /// straight down: one against the layout's colliders, one against the drawn pieces'
        /// own meshes (given a collider for the length of this call only). A cell agrees when
        /// both tops are within 0.12 m. It is a HOLE when the drawing shows a walkable top and
        /// nothing collides there, an UNSEEN FLOOR the other way round. Only a patch counts
        /// (the cell and its four neighbours): a rim's glow strip or a skirt's lip is not a
        /// place a body can stand.
        /// </summary>
        private static void Surfaces(RoundRecord record, ArenaStage stage, int applied, int drawn)
        {
            const float cell = 0.4f, above = 14.0f, depth = 24.0f, close = 0.12f;
            var floor = stage.Layouts[applied].Colliders.transform;
            var added = new List<MeshCollider>();
            foreach (var piece in stage.Pieces)
            {
                var shown = piece.Solids[applied];
                if (shown == null || !shown.activeInHierarchy) continue;
                foreach (var filter in shown.GetComponentsInChildren<MeshFilter>())
                {
                    if (filter.sharedMesh == null || filter.GetComponent<Collider>() != null) continue;
                    var collider = filter.gameObject.AddComponent<MeshCollider>();
                    collider.sharedMesh = filter.sharedMesh;
                    added.Add(collider);
                }
            }
            Physics.SyncTransforms();

            float half = Mathf.Ceil(stage.Radius + 1.0f);
            int n = Mathf.RoundToInt(2.0f * half / cell) + 1;
            // 0 nothing, 1 agree, 2 drawn only, 3 collided only, 4 both at different heights
            var kind = new byte[n, n];
            var gap = new float[n, n];
            int agree = 0;
            for (int ix = 0; ix < n; ix++)
                for (int iz = 0; iz < n; iz++)
                {
                    var origin = new Vector3(-half + ix * cell, above, -half + iz * cell);
                    bool stood = Ground(floor, origin, depth, out var solid);
                    bool seen = false; float top = float.NegativeInfinity;
                    var ray = new Ray(origin, Vector3.down);
                    foreach (var collider in added)
                        if (collider.Raycast(ray, out var hit, depth) && hit.normal.y > 0.7f && hit.point.y > top) { top = hit.point.y; seen = true; }
                    if (stood && seen) { gap[ix, iz] = top - solid.point.y; kind[ix, iz] = (byte)(Mathf.Abs(gap[ix, iz]) <= close ? 1 : 4); }
                    else if (seen) kind[ix, iz] = 2;
                    else if (stood) kind[ix, iz] = 3;
                    if (kind[ix, iz] == 1) agree++;
                }

            foreach (var collider in added) Object.DestroyImmediate(collider);
            Physics.SyncTransforms();

            var patches = new int[5]; var where = new string[5]; float worstGap = 0.0f;
            var samples = new List<string>();
            for (int ix = 1; ix < n - 1; ix++)
                for (int iz = 1; iz < n - 1; iz++)
                {
                    byte k = kind[ix, iz];
                    if (k < 2 || kind[ix - 1, iz] != k || kind[ix + 1, iz] != k || kind[ix, iz - 1] != k || kind[ix, iz + 1] != k) continue;
                    patches[k]++;
                    if (k == 4 && Mathf.Abs(gap[ix, iz]) > Mathf.Abs(worstGap)) worstGap = gap[ix, iz];
                    if (where[k] == null) where[k] = $"({-half + ix * cell:F1}, {-half + iz * cell:F1})";
                    if (k == 4 && patches[k] % 6 == 1 && samples.Count < 8)
                    {
                        float x = -half + ix * cell, z = -half + iz * cell;
                        Ground(floor, new Vector3(x, above, z), depth, out var under);
                        samples.Add($"r {Mathf.Sqrt(x * x + z * z):F1} bearing {Mathf.Repeat(Mathf.Atan2(x, z) * Mathf.Rad2Deg, 360.0f):F0}: drawn {under.point.y + gap[ix, iz]:F2}, collider {under.point.y:F2} ({under.collider.name})");
                    }
                }

            float area = cell * cell;
            record.Lines.Add($"{drawn} pieces drawn. Drawn top against collider top on a {cell} m grid: {agree * area:F0} m2 agree within {close} m; in patches a body could stand on, " +
                             $"drawn with no collider {patches[2] * area:F1} m2, collider with nothing drawn {patches[3] * area:F1} m2, both but at different heights {patches[4] * area:F1} m2");
            if (agree == 0) record.Faults.Add("layout: nothing drawn agrees with any collider");
            if (patches[2] * area > 1.0f) record.Faults.Add($"layout: {patches[2] * area:F1} m2 of drawn floor has no collider under it (first at {where[2]}): a body would fall through what it sees");
            if (patches[3] * area > 1.0f) record.Faults.Add($"layout: {patches[3] * area:F1} m2 of collider has no floor drawn on it (first at {where[3]}): a body would stand on nothing");
            if (patches[4] * area > 1.0f) record.Faults.Add($"layout: {patches[4] * area:F1} m2 is drawn at a different height from its collider (first at {where[4]}, worst {worstGap:+0.00;-0.00} m): {string.Join("; ", samples)}");
        }

        /// <summary>
        /// One round's start, two frames after `RoundStarted`: which layout stands, whether what
        /// is drawn is what is collided with, where each seat is and what is under it, and the can.
        /// </summary>
        private static void Describe(RoundRecord record, ArenaStage stage, CharacterMotor[] seats, Lata lata, MatchDirector match)
        {
            int count = stage.LayoutCount, applied = stage.Applied;
            record.Applied = applied;
            record.Wanted = ArenaStage.LayoutFor(match.PresentationMatchId, record.Round, count);
            record.Layout = applied >= 0 ? stage.Layouts[applied].Name : "none";
            if (applied < 0) { record.Faults.Add("layout: none applied"); return; }
            if (applied != record.Wanted) record.Faults.Add($"layout: {applied} is applied but the match id and the round ask for {record.Wanted}");
            if (stage.Travelling) record.Faults.Add("layout: the visuals are still travelling at the round's start");

            for (int l = 0; l < count; l++)
            {
                if (stage.Layouts[l].Colliders.activeSelf != (l == applied)) record.Faults.Add($"layout: the colliders of '{stage.Layouts[l].Name}' are {(l == applied ? "off" : "on")}");
                if (stage.Layouts[l].Features.activeSelf != (l == applied)) record.Faults.Add($"layout: the pads and pickups of '{stage.Layouts[l].Name}' are {(l == applied ? "off" : "on")}");
            }

            // Each piece: only this layout's solid is drawn, at rest, no hologram, and it fills
            // the same box as its collider.
            int drawn = 0;
            var floor = stage.Layouts[applied].Colliders.transform;
            foreach (var piece in stage.Pieces)
            {
                bool exists = piece.Shapes[applied].Exists;
                for (int l = 0; l < count; l++)
                {
                    var solid = piece.Solids[l];
                    if (solid != null && solid.activeSelf != (exists && l == applied))
                        record.Faults.Add($"layout: piece '{piece.Id}' of '{stage.Layouts[l].Name}' is {(solid.activeSelf ? "drawn" : "not drawn")}");
                    var hologram = piece.Holograms[l];
                    if (hologram != null && hologram.activeSelf) record.Faults.Add($"layout: the hologram of '{piece.Id}' is still up");
                }
                var collider = floor.Find(piece.Id);
                if (exists != (collider != null)) { record.Faults.Add($"layout: piece '{piece.Id}' {(exists ? "has no collider" : "has a collider it should not")}"); continue; }
                if (!exists) continue;

                var shown = piece.Solids[applied];
                if (shown == null) { record.Faults.Add($"layout: piece '{piece.Id}' has no visual"); continue; }
                if (shown.transform.localPosition.sqrMagnitude > 1e-6f || Quaternion.Angle(shown.transform.localRotation, Quaternion.identity) > 0.01f)
                    record.Faults.Add($"layout: piece '{piece.Id}' is drawn off its rest pose ({shown.transform.localPosition}, {shown.transform.localEulerAngles})");
                drawn++;
            }
            Surfaces(record, stage, applied, drawn);

            for (int i = 0; i < seats.Length; i++)
            {
                var who = seats[i];
                if (who == null) { record.Faults.Add($"seat {i} is missing"); continue; }
                Vector3 at = who.transform.position;
                if (!Ground(floor, at + Vector3.up * 0.6f, 2.0f, out var under))
                { record.Faults.Add($"seat {i} at ({at.x:F2}, {at.y:F2}, {at.z:F2}) has no floor under it"); continue; }
                float off = at.y - under.point.y;
                record.Lines.Add($"seat {i}{(who.IsDefender ? " (taya)" : "")} at ({at.x:F2}, {at.y:F2}, {at.z:F2}) on {under.collider.name} at {under.point.y:F2} ({off:+0.00;-0.00} m)");
                if (Mathf.Abs(off) > 0.15f) record.Faults.Add($"seat {i} is {(off > 0 ? "floating" : "sunk")} {Mathf.Abs(off):F2} m at ({at.x:F2}, {at.y:F2}, {at.z:F2}) over {under.collider.name}");
            }

            if (lata == null) { record.Faults.Add("can: no Lata"); return; }
            Vector3 can = lata.transform.position;
            float want = stage.Layouts[applied].CanHeight;
            bool onFloor = Ground(floor, new Vector3(can.x, want + 2.0f, can.z), 6.0f, out var canFloor);
            var canRenderers = lata.GetComponentsInChildren<Renderer>();
            float bottom = canRenderers.Length > 0 ? canRenderers.Min(r => r.bounds.min.y) : can.y;
            record.Lines.Add($"can at ({can.x:F2}, {can.y:F2}, {can.z:F2}), upright {lata.IsUpright}; the layout's can floor is {want:F2}, the floor under it {(onFloor ? canFloor.point.y.ToString("F2") + " on " + canFloor.collider.name : "NONE")}; its drawn bottom is at {bottom:F2}");
            if (!lata.IsUpright) record.Faults.Add("can: not upright at the round's start");
            if (Flat(can, Vector3.zero) > 0.05f) record.Faults.Add($"can: {Flat(can, Vector3.zero):F2} m off the centre");
            if (!onFloor || Mathf.Abs(canFloor.point.y - want) > 0.03f) record.Faults.Add($"can: no floor at {want:F2} under it");
            else if (Mathf.Abs(bottom - want) > 0.12f) record.Faults.Add($"can: its drawn bottom is {bottom - want:+0.00;-0.00} m from its floor ({(bottom > want ? "floating" : "sunk")})");
        }
    }
}
