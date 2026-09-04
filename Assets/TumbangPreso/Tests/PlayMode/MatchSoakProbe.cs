using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// The same match, over and over, watching for what only accumulates.
    ///
    /// ⚠️⚠️ EVERY OTHER MEASUREMENT IN THIS REPOSITORY IS OF A FIRST MATCH. `BotBehaviourProbe`
    /// runs a match. `MatchRunTests` runs a match. `GameplayShots` photographs a match. Nothing
    /// anywhere runs the FIFTH match after the fourth rematch, which is the shape of a tournament
    /// afternoon and the shape of every accumulated-state defect this game can have.
    ///
    /// ⚠️⚠️ AND THE CLASS IS NOT HYPOTHETICAL: THE FIRST DEFECT IT WAS WRITTEN AGAINST WAS FOUND
    /// THE SAME DAY, BY AUDIT RATHER THAN BY THIS. `MatchBootstrap` subscribed four handlers to
    /// the `DontDestroyOnLoad` `MatchDirector` and removed none, so a destroyed component went on
    /// answering `RoundStarted` in later matches, and that handler calls `ResetWorld`, which
    /// teleports all four bodies and re-hands the tsinelas. **Match five was running it five
    /// times, and nothing crashed.** `docs/TODO.md` § 143.5.
    ///
    /// ⚠️ SO WHAT THIS WATCHES IS GROWTH AND REPETITION, NOT CORRECTNESS-IN-ONE-MATCH. A single
    /// iteration passing proves nothing this suite is for; the assertions are all about the
    /// difference between iteration 1 and iteration N:
    ///
    ///   * a handler that fires twice for one round
    ///   * a match that starts already carrying the previous one's score
    ///   * live object counts that only go up
    ///   * managed memory that only goes up
    ///   * a round that never ends
    ///   * any exception at all, from anywhere, at any point
    ///   * any `MatchInvariants` violation on any observed state or transition
    ///
    /// ⚠️⚠️ IT IS IN THE DEFAULT RUN AND IS DELIBERATELY NOT BEHIND A CATEGORY. `docs/TODO.md`
    /// § 126.8d bans adding a category to keep awkward tests out of the gate, and a soak nobody
    /// runs is a soak that does not exist. The iteration count is therefore tuned to be a probe
    /// rather than an endurance run; `-tp-soak-iterations N` on the command line buys the long
    /// version without changing what gates.
    ///
    /// ⚠️⚠️ WHAT IT IS NOT: A LIVENESS MEASUREMENT, AND THE FIRST RUN'S OWN NUMBERS SAY SO.
    /// Every seat finished on a multiple of `Balance.ScoreDefensePerTick` and nothing else:
    /// 900 in a four-round Classic match is 90 defence ticks, which is a whole round of the lata
    /// never being knocked over. **At `Time.timeScale = 60` the bots effectively do not play**,
    /// which is exactly why `BotBehaviourProbe` was moved to a fixed 1/60 s step rather than a 6x
    /// scale (`CLAUDE.md` § 7.1). So do not read a score out of `Logs/soak.json` as balance
    /// evidence, and do not "fix" a low throw count here.
    ///
    /// ⚠️ THAT LIMIT DOES NOT WEAKEN WHAT THIS IS FOR. Accumulated state is about what survives
    /// a match boundary, and a quiet match crosses exactly the same boundaries as a busy one: the
    /// same subscriptions, the same teardown, the same rematch, the same statics. Liveness is
    /// `BotBehaviourProbe`'s job and it already has it.
    /// </summary>
    public class MatchSoakProbe
    {
        /// <summary>
        /// ⚠️ SIX IS THE SMALLEST NUMBER THAT CAN SEE A TREND, and that is the whole reason for
        /// the value. One match is the measurement everything else already takes; two can only
        /// say "different"; six gives a first half and a second half to compare, which is what
        /// turns "it used 40 MB" into "it is growing".
        /// </summary>
        private const int DefaultIterations = 6;

        /// <summary>Real seconds any one match may take before it is called stuck.</summary>
        private const float MatchGuardSeconds = 90.0f;

        private GameObject _root;
        private readonly List<string> _exceptions = new List<string>();

        private sealed class Iteration
        {
            public int Index;
            public int Rounds;
            public int[] FinalScores;
            public int Winner;
            public long ManagedBytes;
            public int LiveMotors;
            public int LiveGameObjects;
            public float Seconds;
            public readonly List<string> Faults = new List<string>();
        }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return PlayModeWorld.Reset();
            Application.logMessageReceived += OnLog;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Application.logMessageReceived -= OnLog;
            Time.timeScale = 1.0f;
            if (_root != null) UnityEngine.Object.DestroyImmediate(_root);
            yield return PlayModeWorld.Reset();
        }

        /// <summary>
        /// ⚠️ AN EXCEPTION ANYWHERE COUNTS, INCLUDING ONE NOTHING ASSERTED ON. A soak that only
        /// fails on its own assertions cannot see a null reference thrown inside a handler it is
        /// not watching, and that is precisely the shape a leaked subscription produces: the
        /// destroyed object throws, the frame carries on, and the match looks fine.
        /// </summary>
        private void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Assert)
                _exceptions.Add($"{type}: {message}");
        }

        [UnityTest]
        public IEnumerator RepeatedMatchesDoNotAccumulateState()
        {
            int iterations = IterationsFromCommandLine();
            var report = new List<Iteration>();

            // ⚠️ THE COUNTER IS PER ROUND, NOT PER MATCH, because a duplicate handler shows up as
            // one round announced twice rather than as a match running twice.
            var roundStarts = new Dictionary<int, int>();
            Action<int, int> counter = (round, defender) =>
            {
                roundStarts.TryGetValue(round, out int seen);
                roundStarts[round] = seen + 1;
            };

            for (int i = 1; i <= iterations; i++)
            {
                var it = new Iteration { Index = i };
                roundStarts.Clear();

                float startedAt = Time.realtimeSinceStartup;

                BuildWorld();

                // ⚠️⚠️ THE RULESET ALTERNATES, AND THAT IS COVERAGE RATHER THAN VARIETY. Classic
                // plays four rounds and Hero Strike eight (`docs/VISION.md` § 1.1), so switching
                // between them every iteration soaks the thing a bracket day actually does:
                // finish one format and start another in the same process. It also drives
                // `TournamentGuard.Apply` through a real match rather than through a unit test.
                bool tournament = (i % 2) == 1;
                if (tournament) TournamentGuard.Apply();
                else UI.SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));

                var match = GameServices.Match;
                match.RoundStarted += counter;

                // ⚠️⚠️ READ *BEFORE* THE RUNNER BEGINS, AND THE FIRST VERSION READ AFTER. This is
                // the one check only a second iteration can make (a match beginning with the
                // previous one's score), and sampling it one frame into a running match measured
                // the defence tick instead: at 60x the very first frame is already about a second
                // of game time, so seat 0 "began" iteration 1 holding 20 points and the harness
                // reported its own sampling as a leak. A check whose first run accuses the game
                // of the harness's mistake teaches everybody to distrust the harness.
                for (int slot = 0; slot < Balance.PlayerCount; slot++)
                {
                    if (match.ScoreFor(slot) != 0)
                        it.Faults.Add($"seat {slot} began iteration {i} holding " +
                                      $"{match.ScoreFor(slot)} points from a previous match");
                }

                var runner = _root.AddComponent<SliceRunner>();
                runner.Lata = GameServices.Round.Lata;
                runner.Seats = SeatArray();
                runner.Slippers = new Slipper[0];
                runner.AutoStart = false;

                Time.timeScale = 60.0f;
                runner.Begin();
                yield return null;

                var previous = Snapshot(match);
                var seenRounds = new HashSet<int>();
                float guard = 0.0f;

                while (match.MatchInProgress && guard < MatchGuardSeconds)
                {
                    guard += Time.unscaledDeltaTime;

                    var current = Snapshot(match);

                    foreach (string fault in MatchInvariants.Check(current))
                        AddOnce(it.Faults, $"round {current.RoundNumber}: {fault}");

                    // ⚠️⚠️ THE DELTA BOUND IS THE OBSERVER'S, NOT THE RULE'S, AND IT HAS TO BE
                    // COMPUTED RATHER THAN GUESSED. `IsReachableDelta` defaults to two awards
                    // because a network snapshot pair spans 200 ms at 5 Hz. This samples once a
                    // FRAME at 60x, so one step is about a second of game time when the frame
                    // rate holds and about seven when it does not, and the defence tick pays
                    // every `DefenseTickInterval`. The first run of this probe reported a
                    // perfectly legal 70-point step as a direct write for exactly that reason.
                    // Deriving the allowance from the game time that actually elapsed keeps the
                    // check able to catch a tenfold duplication while never accusing an honest
                    // frame.
                    int allowance = Mathf.CeilToInt(Time.deltaTime / Balance.DefenseTickInterval) + 2;

                    foreach (string fault in MatchInvariants.CheckTransition(
                                 previous, current, restarted: false, maxEvents: allowance))
                        AddOnce(it.Faults, $"round {previous.RoundNumber}->{current.RoundNumber}: {fault}");

                    if (current.RoundNumber >= 1) seenRounds.Add(current.RoundNumber);
                    previous = current;

                    yield return null;
                }

                Time.timeScale = 1.0f;
                it.Seconds = Time.realtimeSinceStartup - startedAt;

                if (match.MatchInProgress)
                    it.Faults.Add($"the match was still running after {MatchGuardSeconds:F0} real " +
                                  $"seconds at 60x. A round boundary is not advancing");

                match.RoundStarted -= counter;

                foreach (var pair in roundStarts)
                {
                    if (pair.Value > 1)
                        it.Faults.Add($"round {pair.Key} was announced {pair.Value} times. A " +
                                      $"handler from an earlier match is still subscribed");
                }

                it.Rounds = seenRounds.Count;
                it.FinalScores = ScoreArray(match);
                it.Winner = WinnerOf(it.FinalScores);

                // ---- tear the whole world down, the way a return to the lobby does ----
                UnityEngine.Object.DestroyImmediate(_root);
                _root = null;
                GameServices.Round.Clear();
                match.ResetForNewMatch();

                yield return null;
                yield return Resources.UnloadUnusedAssets();

                // ⚠️ MEASURED AFTER THE TEARDOWN AND AFTER A COLLECTION, so the number is what
                // the previous iteration FAILED TO RELEASE rather than what it was using while
                // it ran. A high-water mark during play is not a leak; a floor that rises is.
                GC.Collect();
                GC.WaitForPendingFinalizers();
                yield return null;

                it.ManagedBytes = GC.GetTotalMemory(false);
                it.LiveMotors = UnityEngine.Object
                    .FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None).Length;
                it.LiveGameObjects = UnityEngine.Object
                    .FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length;

                report.Add(it);
            }

            WriteReport(report, iterations);
            Assert.That(Verdict(report), Is.Empty);
        }

        // -------------------------------------------------------------------

        private List<string> Verdict(List<Iteration> report)
        {
            var faults = new List<string>();

            foreach (var it in report)
                foreach (string f in it.Faults)
                    faults.Add($"iteration {it.Index}: {f}");

            foreach (string e in _exceptions)
                faults.Add($"an exception was raised during the soak: {e}");

            if (report.Count < 2) return faults;

            var first = report[0];
            var last = report[report.Count - 1];

            // ⚠️⚠️ THE OBJECT COUNT IS THE ONE BOUND THAT MAY BE EXACT. Every iteration builds the
            // same world and destroys it, so after the teardown there must be no motors left at
            // all. Anything above zero is an object the teardown did not reach, and one per
            // iteration is the signature of a leak rather than of a slow collection.
            if (last.LiveMotors > 0)
                faults.Add($"{last.LiveMotors} CharacterMotor(s) are still alive after the last " +
                           $"iteration tore its world down");

            // ⚠️ AND THE MEMORY BOUND IS DELIBERATELY GENEROUS AND RELATIVE. `docs/TODO.md` § 16's
            // rule about the bot probe applies here: an absolute threshold set against one machine
            // is a flake waiting for a busy afternoon. What is not noise is a floor that rises
            // across every iteration, so this asks for a trend and allows a wide band.
            long growth = last.ManagedBytes - first.ManagedBytes;
            long allowed = Math.Max(first.ManagedBytes / 2, 32L * 1024 * 1024);
            if (growth > allowed)
                faults.Add($"managed memory rose from {Mb(first.ManagedBytes)} to " +
                           $"{Mb(last.ManagedBytes)} across {report.Count} matches, which is more " +
                           $"than the {Mb(allowed)} this allows for churn");

            bool monotonic = true;
            for (int i = 1; i < report.Count; i++)
                if (report[i].LiveGameObjects <= report[i - 1].LiveGameObjects) monotonic = false;

            if (monotonic && report.Count >= 3)
                faults.Add($"the live GameObject count rose on EVERY iteration " +
                           $"({string.Join(", ", report.ConvertAll(r => r.LiveGameObjects.ToString()))}). " +
                           $"A count that never once falls is a leak rather than churn");

            return faults;
        }

        private void WriteReport(List<Iteration> report, int iterations)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"sha\": \"{BuildIdentity.Current.sha}\",");
            sb.AppendLine($"  \"protocol\": {BuildIdentity.Current.protocol},");
            sb.AppendLine($"  \"iterations\": {iterations},");
            sb.AppendLine($"  \"exceptions\": {_exceptions.Count},");
            sb.AppendLine("  \"matches\": [");

            for (int i = 0; i < report.Count; i++)
            {
                var it = report[i];
                sb.Append($"    {{ \"index\": {it.Index}, \"rounds\": {it.Rounds}, ");
                sb.Append($"\"seconds\": {it.Seconds:F2}, ");
                sb.Append($"\"managedBytes\": {it.ManagedBytes}, ");
                sb.Append($"\"liveMotors\": {it.LiveMotors}, ");
                sb.Append($"\"liveGameObjects\": {it.LiveGameObjects}, ");
                sb.Append($"\"winner\": {it.Winner}, ");
                sb.Append($"\"ruleset\": \"{(it.Index % 2 == 1 ? "tournament" : "herostrike")}\", ");
                sb.Append($"\"scores\": [{string.Join(",", it.FinalScores ?? new int[0])}], ");
                sb.Append($"\"faults\": {it.Faults.Count} }}");
                sb.AppendLine(i == report.Count - 1 ? "" : ",");
            }

            sb.AppendLine("  ]");
            sb.AppendLine("}");

            try
            {
                System.IO.Directory.CreateDirectory("Logs");
                System.IO.File.WriteAllText("Logs/soak.json", sb.ToString());
                Debug.Log($"[Soak] {iterations} matches, {_exceptions.Count} exception(s). " +
                          $"Logs/soak.json");
            }
            catch (Exception e)
            {
                // A report that cannot be written must not fail a run that otherwise passed.
                Debug.LogWarning($"[Soak] could not write Logs/soak.json: {e.Message}");
            }
        }

        private static string Mb(long bytes) => $"{bytes / (1024.0 * 1024.0):F1} MB";

        private static void AddOnce(List<string> list, string fault)
        {
            // ⚠️ A VIOLATION HOLDS FOR EVERY FRAME IT IS TRUE, and this samples every frame. One
            // stuck taya would otherwise fill the report with ten thousand identical lines and
            // bury everything else.
            if (!list.Contains(fault)) list.Add(fault);
        }

        private static int IterationsFromCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-tp-soak-iterations" && int.TryParse(args[i + 1], out int n) && n > 0)
                    return n;
            return DefaultIterations;
        }

        private static MatchSnapshot Snapshot(MatchDirector match)
        {
            var owners = new string[Balance.PlayerCount];
            for (int slot = 0; slot < Balance.PlayerCount; slot++)
                owners[slot] = $"seat{slot}";

            return new MatchSnapshot(match.RoundNumber, match.TotalRounds, match.DefenderSlot,
                                     match.MatchInProgress, match.IsWarmupBuffer,
                                     ScoreArray(match), owners);
        }

        private static int[] ScoreArray(MatchDirector match)
        {
            var scores = new int[Balance.PlayerCount];
            for (int i = 0; i < scores.Length; i++) scores[i] = match.ScoreFor(i);
            return scores;
        }

        private static int WinnerOf(int[] scores)
        {
            var board = new Scoreboard();
            board.SetAll(scores);
            return board.WinningSlot();
        }

        private CharacterMotor[] SeatArray()
        {
            var seats = new CharacterMotor[Balance.PlayerCount];
            for (int slot = 0; slot < Balance.PlayerCount; slot++)
                seats[slot] = GameServices.Round.PlayerAt(slot);
            return seats;
        }

        /// <summary>
        /// The same world `MatchRunTests.BuildWorld` builds, for its stated reason: a test that
        /// loads a scene asset fails for two different causes and cannot say which.
        /// </summary>
        private void BuildWorld()
        {
            _root = new GameObject("SoakWorld");

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.SetParent(_root.transform);
            ground.transform.localScale = Vector3.one * 6.0f;

            var lataGo = new GameObject("Lata");
            lataGo.transform.SetParent(_root.transform);
            var lata = lataGo.AddComponent<Lata>();

            GameServices.Round.Clear();
            GameServices.Round.Lata = lata;

            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                var go = new GameObject($"Seat{slot}");
                go.transform.SetParent(_root.transform);

                var cc = go.AddComponent<CharacterController>();
                cc.height = 1.6f;
                cc.radius = 0.35f;
                cc.center = new Vector3(0, 0.8f, 0);

                var m = go.AddComponent<CharacterMotor>();
                m.PlayerSlot = slot;
                m.CharacterIndex = slot;
                m.IsDefender = slot == 0;

                go.AddComponent<Carrier>();
                go.AddComponent<CombatVerbs>();
                go.AddComponent<AIController>();

                float ring = Confinement.AttackerSpawnRing();
                go.transform.position = slot == 0
                    ? new Vector3(0, 0.1f, -Balance.DefenderStartOffset)
                    : new Vector3(ring * (slot - 2), 0.1f, ring);

                GameServices.Round.Register(m);
            }
        }
    }
}
