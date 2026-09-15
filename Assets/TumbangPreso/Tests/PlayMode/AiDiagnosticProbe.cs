using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// One round, at real speed, with every bot's decision written down.
    ///
    /// ⚠️⚠️ THIS EXISTS BECAUSE A PASS/FAIL CANNOT SAY *WHICH* PART OF A BOT IS WRONG.
    /// `BotBehaviourProbe` measures the outcome of a whole match and reports, for example,
    /// three throws and 679 unretrieved-slipper penalties. That is a true and damning number
    /// and it still does not say whether the attackers never chose to fetch, chose to fetch
    /// and could not reach, or fetched and then never released the charge. Those are three
    /// different repairs.
    ///
    /// ⚠️ AND IT RUNS AT 1x ON PURPOSE, unlike the match probe. The AI thinks in `Update` on
    /// `Time.deltaTime`, so at a high time scale it gets one decision per several simulated
    /// seconds and every plan it makes looks stale. Anything measured up there is partly a
    /// measurement of the harness. A single round at real speed is slower to run and is the
    /// only reading that is entirely about the bots.
    /// </summary>
    [Category(WallClock)]
    public class AiDiagnosticProbe
    {
        /// <summary>
        /// ⚠️⚠️ THE PAIR THAT MAKES A FULL-SUITE RESULT MEAN ANYTHING. `docs/TODO.md` § 126.8:
        /// the full PlayMode run came back 42, 41 and then 56 red with the red set moving, and a
        /// gate whose red set moves is not measuring the code. `PlayModeWorld.Reset` has the
        /// mechanism and why BOTH hooks are needed rather than one.
        /// </summary>
        [UnitySetUp]
        public IEnumerator ResetWorldBefore() => PlayModeWorld.Reset();

        [UnityTearDown]
        public IEnumerator ResetWorldAfter() => PlayModeWorld.Reset();

        /// <summary>
        /// The category that keeps this class out of the default PlayMode run.
        ///
        /// ⚠️⚠️ THIS IS `docs/TODO.md` § 6 BEING DECIDED RATHER THAN RE-DIAGNOSED, AND THE
        /// ENTRY ASKED FOR EXACTLY THAT: *"a decision, not a bug hunt"*. Three data points now,
        /// all the same shape: `OneClassicRoundAtRealSpeedIsFullyExplained` failed at **21.6 s**,
        /// then **29.9 s**, then **37.6 s** against a 20.0 s bound, and passed on immediate
        /// re-runs with nothing changed between them. 37.6 is not a near miss; it is a round
        /// that spent most of its wall clock somewhere other than this test.
        ///
        /// ⚠️ THE TESTS ARE NOT DELETED AND MUST NOT BE. They are the only thing in the harness
        /// that explains WHY a bot did something rather than how much, and § 7 is explicit that
        /// the answer to a slow suite is never to delete the measured tests. What changes is
        /// cadence: they are run deliberately, when somebody is going to read the report, on a
        /// machine that is not also compiling something.
        ///
        /// ⚠️⚠️ `[Explicit]` WAS TRIED FIRST AND DOES NOT WORK HERE. A PlayMode run in batch
        /// mode with the attribute in place still reported both of these among 60 tests. The
        /// exclusion has to live in the COMMAND, which is why the default PlayMode line in
        /// `CLAUDE.md` § 7 and `docs/TESTING.md` carries `-testCategory "!WallClock"`. Do not
        /// re-add the attribute believing it changes anything.
        ///
        /// ⚠️ AND THE COST IS REAL. This class is roughly **80 real seconds** of a PlayMode
        /// suite, at 1x by design, and a red result from it carries no information: the next
        /// session spends a full run learning that again. That is the worst outcome the entry
        /// names.
        ///
        /// Run it on purpose:
        /// <code>
        /// Unity.exe -batchmode -runTests -projectPath . -testPlatform PlayMode \
        ///   -testCategory "WallClock" -testResults Logs/ai.xml -logFile Logs/ai.log
        /// </code>
        /// </summary>
        public const string WallClock = "WallClock";

        [TearDown]
        public void TearDown()
        {
            Hitstop.End();
            Time.timeScale = 1.0f;
        }

        [UnityTest]
        public IEnumerator OneClassicRoundAtRealSpeedIsFullyExplained()
        {
            yield return Diagnose(GameMode.Classic, 40.0f);
        }

        [UnityTest]
        public IEnumerator OneHeroRoundAtRealSpeedIsFullyExplained()
        {
            yield return Diagnose(GameMode.HeroStrike, 40.0f);
        }

        /// <summary>
        /// C2: whole matches of lunges at ordinary 1x simulation speed. The world is stepped at a
        /// fixed 1/60 s with time scale 1, so every bot decision sees exactly the frame time a
        /// 60 fps player's game gives it; only the wall clock is removed. The 40 s wall-clock
        /// diagnostics above carry the same tracker as a real-time cross-check.
        /// </summary>
        [UnityTest, Timeout(900000)]
        public IEnumerator ClassicLungesAcrossAWholeMatchAreExplained()
        {
            yield return TraceLunges(GameMode.Classic, "Eskinita");
        }

        [UnityTest, Timeout(900000)]
        public IEnumerator HeroLungesAcrossAWholeMatchAreExplained()
        {
            yield return TraceLunges(GameMode.HeroStrike, "Eskinita");
        }

        private static int LungeSeed()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-tp-bot-seed" && int.TryParse(args[i + 1], out int seed))
                    return seed;
            return 20260823;
        }

        private IEnumerator TraceLunges(GameMode mode, string map)
        {
            const float step = 1.0f / 60.0f;
            var previousRules = UI.SceneFlow.SelectedRules.Clone();
            bool previousPin = UI.SceneFlow.RulesPinned;
            bool previousAllBots = GameLaunch.AllBots;
            UI.SceneFlow.PinSelectedRules(CustomGameRules.Defaults(mode));
            GameLaunch.AllBots = true;
            UnityEngine.Random.InitState(LungeSeed());
            Hitstop.End();
            Time.timeScale = 1.0f;

            var load = SceneManager.LoadSceneAsync(map, LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");
            for (int i = 0; i < 25; i++) yield return null;

            var round = GameServices.Round;
            var match = GameServices.Match;
            var runner = Object.FindFirstObjectByType<SliceRunner>();
            Assert.IsNotNull(round);
            Assert.IsNotNull(match);
            Assert.IsNotNull(runner);

            UnityEngine.Random.InitState(LungeSeed());
            Time.captureDeltaTime = step;
            for (int i = 0; i < 120; i++) yield return null;
            runner.Begin();

            var tracker = new LungeTracker();
            int frames = 0;
            try
            {
                while (match.MatchInProgress && frames < 64000)
                {
                    frames++;
                    tracker.Sample(round, match, frames, step);
                    yield return null;
                }
            }
            finally
            {
                Time.captureDeltaTime = 0.0f;
                UI.SceneFlow.AdoptRemoteRules(previousRules);
                if (previousPin) UI.SceneFlow.PinSelectedRules(previousRules); else UI.SceneFlow.UnpinSelectedRules();
                GameLaunch.AllBots = previousAllBots;
            }

            var record = GameServices.Stats != null ? GameServices.Stats.Last : null;
            int attempts = 0, hits = 0;
            if (record?.Players != null)
                foreach (var p in record.Players)
                    if (p != null) { attempts += p.LungeAttempts; hits += p.LungeHits; }

            var log = new StringBuilder();
            tracker.Unwatch();
            log.AppendLine($"lunge trace  ·  {mode}  ·  {map}  ·  seed {LungeSeed()}  ·  {frames} frames at 1/60 s, time scale 1");
            log.AppendLine($"collector: {hits}/{attempts} lunge hits/attempts; tracker saw {tracker.Attempts} releases");
            log.Append(tracker.Describe());
            Directory.CreateDirectory("Logs");
            File.WriteAllText($"Logs/ai-lunge-trace-{mode}-{map}.txt", log.ToString());
            Debug.Log(log.ToString());

            Assert.IsFalse(match.MatchInProgress, $"{mode} on {map}: the traced match did not finish.");
            // The trace is only evidence if it counts the same releases the collector counted.
            Assert.AreEqual(attempts, tracker.Attempts,
                "The lunge tracker and MatchStatsCollector disagree about how many lunges were released.");
        }

        private IEnumerator Diagnose(GameMode mode, float seconds)
        {
            var previousMode = UI.SceneFlow.SelectedMode;
            UI.SceneFlow.SelectedMode = mode;

            Hitstop.End();
            Time.timeScale = 1.0f;

            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");
            for (int i = 0; i < 25; i++) yield return null;

            var round = GameServices.Round;
            Assert.IsNotNull(round);

            var runner = Object.FindFirstObjectByType<SliceRunner>();
            Assert.IsNotNull(runner);
            runner.Begin();

            var bots = Object.FindObjectsByType<AIController>(FindObjectsSortMode.None);
            var planTime = new Dictionary<string, float>();
            var log = new StringBuilder();

            log.AppendLine($"ai diagnostic  ·  {mode}  ·  {bots.Length} bots  ·  1x");

            var lunges = new LungeTracker();
            int lungeFrame = 0;
            float elapsed = 0.0f;
            float nextSample = 0.0f;
            int throws = 0;
            var wasFlying = new HashSet<Slipper>();
            var escaped = new HashSet<Slipper>();
            var strayed = new HashSet<CharacterMotor>();

            // Per slipper: how long it has been continuously Loose, the worst such spell, and
            // what its owner was doing at the worst moment. A spell longer than the tournament
            // grace period is exactly what posts an unretrieved-slipper penalty, so this names
            // the slipper and the reason rather than only counting the fine.
            var looseFor = new Dictionary<Slipper, float>();
            var worstLoose = new Dictionary<Slipper, float>();
            var worstWhy = new Dictionary<Slipper, string>();

            while (elapsed < seconds)
            {
                float dt = Time.unscaledDeltaTime;
                elapsed += dt;
                lunges.Sample(round, GameServices.Match, ++lungeFrame, Time.deltaTime);

                foreach (var bot in bots)
                {
                    if (bot == null) continue;
                    string key = $"{bot.GetComponent<CharacterMotor>().PlayerSlot}:{bot.Plan}";
                    planTime.TryGetValue(key, out float held);
                    planTime[key] = held + dt;
                }

                foreach (var seat in round.Players)
                {
                    if (seat == null) continue;

                    bool away = Mathf.Abs(seat.transform.position.x) > AIController.PlayableHalfX + 0.5f
                             || Mathf.Abs(seat.transform.position.z) > AIController.PlayableHalfZ + 0.5f;

                    if (away && strayed.Add(seat))
                        log.AppendLine($"!! t={elapsed:F1} seat {seat.PlayerSlot} LEFT THE ARENA at " +
                                       $"{seat.transform.position} act={seat.CanAct()} " +
                                       $"held={seat.HoldingSlipper}");
                    if (!away) strayed.Remove(seat);
                }

                foreach (var slipper in Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None))
                {
                    if (slipper == null) continue;
                    if (slipper.State == SlipperState.InFlight)
                    {
                        if (wasFlying.Add(slipper)) throws++;
                    }
                    else wasFlying.Remove(slipper);

                    // First moment a slipper leaves the playable rectangle, with the state and
                    // velocity that took it there. Out of bounds is unrecoverable for the owner,
                    // so naming the frame it happens on names the verb responsible.
                    bool outside = Mathf.Abs(slipper.transform.position.x) > AIController.PlayableHalfX + 0.5f
                                || Mathf.Abs(slipper.transform.position.z) > AIController.PlayableHalfZ + 0.5f;
                    if (outside && escaped.Add(slipper))
                    {
                        log.AppendLine($"!! t={elapsed:F1} slipper own={slipper.OwnerSlot} LEFT THE ARENA at " +
                                       $"{slipper.transform.position} state={slipper.State} " +
                                       $"holder={(slipper.Holder != null ? slipper.Holder.PlayerSlot.ToString() : "none")} " +
                                       $"holderAt={(slipper.Holder != null ? slipper.Holder.transform.position.ToString() : "-")} " +
                                       $"vel={slipper.Velocity} spin={slipper.PektusSpin:F2} " +
                                       $"affinity={slipper.Affinity} thrower={slipper.ThrowerSlot}");
                    }
                    if (!outside) escaped.Remove(slipper);

                    if (slipper.State != SlipperState.Loose)
                    {
                        looseFor[slipper] = 0.0f;
                        continue;
                    }

                    looseFor.TryGetValue(slipper, out float held);
                    held += dt;
                    looseFor[slipper] = held;

                    worstLoose.TryGetValue(slipper, out float worst);
                    if (held <= worst) continue;

                    worstLoose[slipper] = held;

                    var owner = round.PlayerAt(slipper.OwnerSlot);
                    var ownerBot = owner != null ? owner.GetComponent<AIController>() : null;
                    worstWhy[slipper] =
                        $"own={slipper.OwnerSlot} plan={(ownerBot != null ? ownerBot.Plan.ToString() : "human/none")} " +
                        $"ownerAct={(owner != null && owner.CanAct())} " +
                        $"d3={(owner != null ? Vector3.Distance(owner.transform.position, slipper.transform.position) : -1.0f):F2} " +
                        $"grabbable={(owner != null && slipper.CanBeGrabbedBy(owner))} " +
                        $"slipperY={slipper.transform.position.y:F2}";
                }

                if (elapsed >= nextSample)
                {
                    nextSample += 2.0f;

                    foreach (var bot in bots)
                    {
                        if (bot == null) continue;

                        var motor = bot.GetComponent<CharacterMotor>();
                        var carrier = motor.GetComponent<Carrier>();

                        log.AppendLine(
                            $"t={elapsed:F1} seat={motor.PlayerSlot} plan={bot.Plan} " +
                            $"def={motor.IsDefender} act={motor.CanAct()} " +
                            $"held={motor.HoldingSlipper} charging={(carrier != null && carrier.IsCharging)} " +
                            $"charge={(carrier != null ? carrier.ChargeRatio : 0.0f):F2} " +
                            $"canThrow={round.CanThrow(motor)} " +
                            $"axis={motor.Intent.MoveAxis} pos={motor.transform.position}");
                    }

                    log.AppendLine($"    lataUpright={round.Lata?.IsUpright} throws={throws} " +
                                   $"roundActive={round.RoundActive}");
                    log.AppendLine("    " + SlipperLine());
                }

                yield return null;
            }

            log.AppendLine();
            log.AppendLine("worst continuous LOOSE spell per slipper:");
            foreach (var kvp in worstLoose)
            {
                worstWhy.TryGetValue(kvp.Key, out string why);
                log.AppendLine($"  {kvp.Value:F1}s  {why}");
            }

            log.AppendLine();
            log.AppendLine("plan occupancy (seconds):");
            foreach (var kvp in planTime)
                log.AppendLine($"  {kvp.Key,-24} {kvp.Value:F1}s");

            // -------------------------------------------------------------------
            // § THE SABOTAGE LEDGER
            //
            // ⚠️⚠️ THE MEASUREMENT 🧑 ASKED FOR IS NOT "BOTS SHOVED LESS". His words, 2026-09-03:
            // the bots *"follow players around only to push them, even when the shove has no
            // meaningful effect on the game"*, and the brief that followed says the important
            // number is *"every shove they chose had an intelligible objective reason"*. A count
            // that went down could just as easily mean the rule got tighter than the game.
            //
            // ⚠️⚠️ SO THE THREE COLUMNS THAT MATTER ARE THE PURSUIT ONES. `SabotageRules
            // .MaxPursuitSeconds` is 1.90 s and `MaxApproachRange` is 3.20 m, both derived rather
            // than typed (`docs/TODO.md` § 134.2). **If the longest pursuit in a whole round is
            // under the cap and the plans entered are close to the shoves attempted, the bot is
            // taking opportunities rather than manufacturing them**, which is the whole claim.
            //
            // ⚠️ AND THE VETO DISTRIBUTION IS WHY A LOW COUNT IS TRUSTWORTHY. `SabotageVeto`
            // names every refusal, so a run reporting mostly `OutOfApproachRange` is a bot that
            // simply was not near anybody, while one reporting mostly `EndpointStaysSafe` is the
            // projection doing its job.
            // -------------------------------------------------------------------
            log.AppendLine();
            log.AppendLine("sabotage ledger:");

            int plans = 0, attempts = 0;
            float longestPursuit = 0.0f, totalPursuit = 0.0f;

            foreach (var bot in bots)
            {
                if (bot == null) continue;

                plans += bot.SabotagePlansEntered;
                attempts += bot.SabotageShovesAttempted;
                totalPursuit += bot.TotalSabotagePursuitSeconds;
                longestPursuit = Mathf.Max(longestPursuit, bot.LongestSabotagePursuit);

                log.AppendLine(
                    $"  seat {bot.GetComponent<CharacterMotor>()?.PlayerSlot}  "
                    + $"plans {bot.SabotagePlansEntered}  shoves {bot.SabotageShovesAttempted}  "
                    + $"longest pursuit {bot.LongestSabotagePursuit:F2}s  "
                    + $"time pursuing {bot.TotalSabotagePursuitSeconds:F1}s  "
                    + $"last veto {bot.LastSabotageProjection.Veto}");
            }

            log.AppendLine($"  TOTAL  plans {plans}  shoves {attempts}  "
                           + $"longest pursuit {longestPursuit:F2}s  "
                           + $"time pursuing {totalPursuit:F1}s");
            log.AppendLine($"  bounds: max pursuit {SabotageRules.MaxPursuitSeconds:F2}s  "
                           + $"max approach {SabotageRules.MaxApproachRange:F2}m  "
                           + $"danger radius {SabotageRules.DangerRadius:F2}m  "
                           + $"min closure {SabotageRules.MinClosure:F2}m");

            // ⚠️⚠️ THE VETO DISTRIBUTION IS WHAT MAKES A LOW COUNT READABLE. Without it a run
            // reporting zero shoves is indistinguishable from a rule that refuses everything, and
            // the rule this replaced carried a comment recording exactly that reading as a
            // symptom. See `AIController.SabotageVetoes`.
            var vetoNames = (SabotageVeto[])System.Enum.GetValues(typeof(SabotageVeto));
            var vetoTotals = new int[vetoNames.Length];
            int projections = 0;

            foreach (var bot in bots)
            {
                if (bot == null) continue;

                projections += bot.SabotageProjectionsMade;
                for (int i = 0; i < vetoNames.Length && i < bot.SabotageVetoes.Length; i++)
                    vetoTotals[i] += bot.SabotageVetoes[i];
            }

            log.AppendLine($"  candidate shoves projected: {projections}");

            for (int i = 0; i < vetoNames.Length; i++)
            {
                if (vetoTotals[i] == 0) continue;
                log.AppendLine($"    {vetoNames[i],-22} {vetoTotals[i]}");
            }

            if (projections == 0)
                log.AppendLine("    (nothing was ever projected: no rival came within "
                               + $"{SabotageRules.MaxApproachRange:F2} m while a shove was "
                               + "affordable and off cooldown)");

            log.AppendLine();
            log.Append(lunges.Describe());

            Directory.CreateDirectory("Logs");
            File.WriteAllText($"Logs/ai-diagnostic-{mode}.txt", log.ToString());
            Debug.Log(log.ToString());

            UI.SceneFlow.SelectedMode = previousMode;

            Assert.Greater(bots.Length, 0, "The arena seated no bots.");

            // ⚠️⚠️ THE PURSUIT CEILING IS ASSERTED, NOT ONLY PRINTED, AND IT IS THE ONE NUMBER
            // THAT DIRECTLY ANSWERS THE REPORT. 🧑 watched bots TAIL players; a pursuit that ran
            // longer than `SabotageRules.MaxPursuitSeconds` would mean the clock in
            // `AIController.StepSabotageClocks` is not being stepped, and the behaviour is back
            // whatever the counts say. A small tolerance covers one think tick of overshoot.
            Assert.LessOrEqual(longestPursuit, SabotageRules.MaxPursuitSeconds + 0.25f,
                $"a bot pursued a sabotage target for {longestPursuit:F2}s against a "
                + $"{SabotageRules.MaxPursuitSeconds:F2}s cap. See `docs/TODO.md` § 134.2.");

            // ⚠️ THE 1x READING IS A DIFFERENT CLAIM FROM THE MATCH PROBE'S. `BotBehaviourProbe`
            // runs at 6x, where the AI gets fewer decisions per simulated second, so it can only
            // assert floors. This one runs at real speed, so it can assert the two invariants
            // that must hold in the build a person plays.
            foreach (var seat in round.Players)
            {
                if (seat == null) continue;

                Assert.LessOrEqual(Mathf.Abs(seat.transform.position.x),
                    AIController.PlayableHalfX + 0.1f,
                    $"{mode}: seat {seat.PlayerSlot} finished outside the arena on X.");
                Assert.LessOrEqual(Mathf.Abs(seat.transform.position.z),
                    AIController.PlayableHalfZ + 0.1f,
                    $"{mode}: seat {seat.PlayerSlot} finished outside the arena on Z.");
            }

            // ⚠️ TWICE THE TOURNAMENT GRACE PERIOD, NOT THE GRACE PERIOD ITSELF. A bot that is
            // evading, stunned or waiting out a taya can legitimately leave its tsinelas lying
            // for longer than ten seconds and take the fine for it. What this rejects is a
            // slipper that is not merely unfetched but UNFETCHABLE, which is the failure that
            // reported spells of 22 s and longer while the owner stood a metre away.
            float strandedCeiling = Balance.SlipperUnretrievedGracePeriod * 2.0f;

            foreach (var kvp in worstLoose)
            {
                worstWhy.TryGetValue(kvp.Key, out string why);
                Assert.Less(kvp.Value, strandedCeiling,
                    $"{mode}: a tsinelas stayed loose for {kvp.Value:F1}s, past twice the " +
                    $"{Balance.SlipperUnretrievedGracePeriod:F0}s grace period. That is a piece " +
                    $"of ammunition its owner cannot reach rather than one it has not fetched. " +
                    $"At the worst moment: {why}");
            }
        }

        // -------------------------------------------------------------------
        // § C2: EVERY LUNGE, FROM THE CHARGE TO THE AUTHORITATIVE OUTCOME
        //
        // docs/CLAUDE_ENGINEERING_LANE.md C2. BotBehaviourProbe's lunge column is the
        // collector's LungeHits/LungeAttempts and it cannot say whether a miss was a bad aim, a
        // target that stopped being taggable, a charge released because the plan changed, or a
        // lunge the rules simply could not land. This records the taya's charge, the AI's
        // intended victim, the facing at release and every frame of the live sweep against the
        // victim, and reads the collector's own counters as the outcome so the trace is checked
        // against the number it explains.
        // -------------------------------------------------------------------

        private sealed class LungeTracker
        {
            private sealed class Live
            {
                public int Slot, Frame, Round, ChargeFrames;
                public string Reason, Plan, Victim;
                public CharacterMotor VictimBody;
                public Vector3 From, Forward, VictimAt, VictimVel;
                public float Power, Distance, Bearing, Lateral, Time;
                public bool VictimTaggable, AnyTaggable;
                public float MinVictim = float.MaxValue, MinVictimTaggable = float.MaxValue, MinAnyTaggable = float.MaxValue;
                public bool VictimLeftTaggable, Hit;
                public int HitsBefore;
                // Three frames of slack: the collector's hit lands inside CombatVerbs.Update,
                // which can run after this sample on the last live frame.
                public float ActiveLeft = Balance.LungeActiveTime + 3.0f / 60.0f;
                public string Start = "-";
                public float HeldAfter, PunchCooldown;
                public int TagsDuringCharge;
            }

            private readonly Dictionary<int, int> _charging = new Dictionary<int, int>();
            private readonly Dictionary<int, bool> _cooling = new Dictionary<int, bool>();
            private readonly Dictionary<int, string> _planAtCharge = new Dictionary<int, string>();
            private readonly Dictionary<int, string> _startAtCharge = new Dictionary<int, string>();
            private readonly Dictionary<int, int> _tagsBySlot = new Dictionary<int, int>();
            private readonly Dictionary<int, int> _tagsAtCharge = new Dictionary<int, int>();
            private readonly Dictionary<int, int> _hitsWhileCharging = new Dictionary<int, int>();
            private readonly Dictionary<int, int> _tagsWhileCharging = new Dictionary<int, int>();
            private RoundDirector _watched;

            private void Watch(RoundDirector round)
            {
                if (_watched == round) return;
                if (_watched != null) _watched.Tagged -= OnTagged;
                _watched = round;
                round.Tagged += OnTagged;
            }

            public void Unwatch() { if (_watched != null) _watched.Tagged -= OnTagged; _watched = null; }

            private void OnTagged(int taya, int victim)
            {
                _tagsBySlot.TryGetValue(taya, out int n);
                _tagsBySlot[taya] = n + 1;
            }
            private readonly List<Live> _live = new List<Live>();
            private readonly List<Live> _done = new List<Live>();
            public readonly List<string> Lines = new List<string>();

            private static readonly System.Reflection.BindingFlags Private =
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;

            public int Attempts => _done.Count + _live.Count;

            public void Sample(RoundDirector round, MatchDirector match, int frame, float dt)
            {
                if (round == null) return;
                Watch(round);

                foreach (var live in _live) Step(live, round, dt);
                for (int i = _live.Count - 1; i >= 0; i--)
                {
                    if (_live[i].ActiveLeft > 0.0f) continue;
                    Finish(_live[i]);
                    _done.Add(_live[i]);
                    _live.RemoveAt(i);
                }

                foreach (var taya in round.Players)
                {
                    if (taya == null) continue;
                    var verbs = taya.GetComponent<CombatVerbs>();
                    if (verbs == null) continue;
                    int slot = taya.PlayerSlot;
                    var ai = taya.GetComponent<AIController>();

                    bool charging = taya.IsDefender && verbs.ObservedLungeCharge >= 0.0f;
                    _charging.TryGetValue(slot, out int chargeFrames);
                    if (charging && chargeFrames == 0)
                    {
                        _planAtCharge[slot] = ai != null ? ai.Plan.ToString() : "human";
                        _startAtCharge[slot] = DescribeStart(taya, ai);
                        _tagsAtCharge[slot] = _tagsBySlot.TryGetValue(slot, out int tc) ? tc : 0;
                    }
                    int heldFrames = charging ? chargeFrames + 1 : chargeFrames;

                    _cooling.TryGetValue(slot, out bool wasCooling);
                    bool cooling = taya.IsDefender && verbs.LungeCooldownLeft > 0.0f;
                    _cooling[slot] = cooling;

                    // The collector and tag counts as of the previous sample: SweepLungeTag runs in the same
                    // CombatVerbs.Update that releases, so a lunge that tags on its first live frame has already
                    // been counted by the time this sample sees the cooldown edge.
                    if (cooling && !wasCooling && chargeFrames > 0)
                        _live.Add(Open(taya, verbs, ai, round, match, frame, chargeFrames, dt));

                    _charging[slot] = charging ? heldFrames : 0;
                    if (charging)
                    {
                        _hitsWhileCharging[slot] = LungeHits(slot);
                        _tagsWhileCharging[slot] = _tagsBySlot.TryGetValue(slot, out int tw) ? tw : 0;
                    }
                }
            }

            private Live Open(CharacterMotor taya, CombatVerbs verbs, AIController ai, RoundDirector round,
                              MatchDirector match, int frame, int chargeFrames, float dt)
            {
                var l = new Live
                {
                    Slot = taya.PlayerSlot, Frame = frame, Round = match != null ? match.RoundNumber : -1,
                    ChargeFrames = chargeFrames, Time = round.TimeLeft,
                    Plan = ai != null ? ai.Plan.ToString() : "human",
                };
                l.From = (Vector3)(typeof(CombatVerbs).GetField("_lungeFrom", Private)?.GetValue(verbs) ?? taya.transform.position);
                l.Forward = taya.transform.forward; l.Forward.y = 0.0f; l.Forward.Normalize();
                float charge = chargeFrames * dt;
                l.Power = Mathf.Clamp(charge / Balance.LungeChargeTime, Balance.LungeMinPower, 1.0f);

                var victim = ai != null ? typeof(AIController).GetField("_lastTagTarget", Private)?.GetValue(ai) as CharacterMotor : null;
                l.VictimBody = victim;
                l.Victim = victim != null ? victim.PlayerSlot.ToString() : "none";
                foreach (var p in round.Players)
                    if (p != null && p != taya && !p.IsDefender && p.IsTaggable()) l.AnyTaggable = true;

                if (victim != null)
                {
                    l.VictimAt = victim.transform.position;
                    l.VictimVel = victim.Velocity; l.VictimVel.y = 0.0f;
                    l.VictimTaggable = victim.IsTaggable();
                    Vector3 to = victim.transform.position - l.From; to.y = 0.0f;
                    l.Distance = to.magnitude;
                    l.Bearing = to.sqrMagnitude > 0.0001f ? Vector3.Angle(l.Forward, to) : 0.0f;
                    l.Lateral = Vector3.Cross(l.Forward, to).magnitude;
                }

                string planAtCharge = _planAtCharge.TryGetValue(taya.PlayerSlot, out var p0) ? p0 : "-";
                if (ai == null) l.Reason = "human";
                else if (l.Plan != "Hunt") l.Reason = $"released-by-plan-change({planAtCharge}->{l.Plan})";
                else if (victim == null || !l.VictimTaggable) l.Reason = "no-taggable-victim";
                else if (charge >= AiTuning.LungeHoldTime + 0.45f - dt * 0.5f) l.Reason = "hold-timeout";
                else if (charge >= AiTuning.LungeHoldTime - dt * 0.5f) l.Reason = "cone-release";
                else l.Reason = "short-release";

                l.HitsBefore = _hitsWhileCharging.TryGetValue(taya.PlayerSlot, out int hb) ? hb : LungeHits(taya.PlayerSlot);
                l.Start = _startAtCharge.TryGetValue(taya.PlayerSlot, out var s0) ? s0 : "-";
                l.PunchCooldown = verbs.PunchCooldownLeft;
                l.TagsDuringCharge = (_tagsWhileCharging.TryGetValue(taya.PlayerSlot, out int tn) ? tn : 0)
                                     - (_tagsAtCharge.TryGetValue(taya.PlayerSlot, out int t0) ? t0 : 0);
                l.HeldAfter = ai != null ? (float)(typeof(AIController).GetField("_lungeHeld", Private)?.GetValue(ai) ?? 0.0f) : 0.0f;
                return l;
            }

            /// <summary>The AI's own lunge bookkeeping on the first charging frame. `_lungeHeld`
            /// near one frame means StepLungeIntent opened a fresh charge after its range check;
            /// anything larger means the charge resumed from an earlier, unfinished hold.</summary>
            private static string DescribeStart(CharacterMotor taya, AIController ai)
            {
                if (ai == null) return "human";
                float held = (float)(typeof(AIController).GetField("_lungeHeld", Private)?.GetValue(ai) ?? 0.0f);
                var victim = typeof(AIController).GetField("_lastTagTarget", Private)?.GetValue(ai) as CharacterMotor;
                if (victim == null) return $"held={held:F3} victim=none";
                Vector3 to = victim.transform.position - taya.transform.position; to.y = 0.0f;
                return $"held={held:F3} victim={victim.PlayerSlot} dist={to.magnitude:F2} taggable={victim.IsTaggable()} range={AiTuning.For(ai.SeatDifficulty ?? AIController.ActiveDifficulty).LungeRange:F1}";
            }

            private static void Step(Live l, RoundDirector round, float dt)
            {
                var taya = round.PlayerAt(l.Slot);
                if (taya == null) { l.ActiveLeft = 0.0f; return; }
                Vector3 a = new Vector3(l.From.x, 0, l.From.z);
                Vector3 b = new Vector3(taya.transform.position.x, 0, taya.transform.position.z);
                foreach (var p in round.Players)
                {
                    if (p == null || p == taya || p.IsDefender) continue;
                    Vector3 t = new Vector3(p.transform.position.x, 0, p.transform.position.z);
                    float d = Segment(t, a, b);
                    bool taggable = p.IsTaggable();
                    if (taggable) l.MinAnyTaggable = Mathf.Min(l.MinAnyTaggable, d);
                    if (p != l.VictimBody) continue;
                    l.MinVictim = Mathf.Min(l.MinVictim, d);
                    if (taggable) l.MinVictimTaggable = Mathf.Min(l.MinVictimTaggable, d);
                    else if (l.VictimTaggable) l.VictimLeftTaggable = true;
                }
                l.ActiveLeft -= dt;
            }

            private void Finish(Live l)
            {
                int hits = LungeHits(l.Slot) - l.HitsBefore;
                string outcome = hits > 0 ? "HIT" : "miss";
                Lines.Add($"frame={l.Frame} round={l.Round} left={l.Time:F1} taya={l.Slot} plan={l.Plan} reason={l.Reason} " +
                          $"charge={l.ChargeFrames} power={l.Power:F2} victim={l.Victim} victimTaggable={l.VictimTaggable} anyTaggable={l.AnyTaggable} " +
                          $"dist={l.Distance:F2} bearing={l.Bearing:F1} lateral={l.Lateral:F2} victimSpeed={l.VictimVel.magnitude:F2} " +
                          $"minVictim={Fmt(l.MinVictim)} minVictimWhileTaggable={Fmt(l.MinVictimTaggable)} minAnyTaggable={Fmt(l.MinAnyTaggable)} " +
                          $"victimLeftTaggable={l.VictimLeftTaggable} heldAfterRelease={l.HeldAfter:F3} punchCooldown={l.PunchCooldown:F2} tagsDuringCharge={l.TagsDuringCharge} start=[{l.Start}] outcome={outcome}");
                l.Hit = hits > 0;
            }

            private static string Fmt(float v) => v == float.MaxValue ? "-" : v.ToString("F2");

            private static int LungeHits(int slot)
            {
                var stats = GameServices.Stats;
                if (stats == null) return 0;
                var lines = typeof(MatchStatsCollector).GetField("_lines", Private)?.GetValue(stats) as System.Array;
                var line = lines != null && slot >= 0 && slot < lines.Length ? lines.GetValue(slot) : null;
                if (line == null) return 0;
                var member = line.GetType().GetField("LungeHits");
                if (member != null) return (int)member.GetValue(line);
                var prop = line.GetType().GetProperty("LungeHits");
                return prop != null ? (int)prop.GetValue(line) : 0;
            }

            private static float Segment(Vector3 p, Vector3 a, Vector3 b)
            {
                Vector3 ab = b - a;
                float len = ab.sqrMagnitude;
                if (len < 1e-6f) return Vector3.Distance(p, a);
                float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / len);
                return Vector3.Distance(p, a + ab * t);
            }

            public string Describe()
            {
                var sb = new StringBuilder();
                int hits = 0;
                var byReason = new SortedDictionary<string, int[]>();
                foreach (var l in _done)
                {
                    bool hit = l.Hit;
                    if (hit) hits++;
                    string key = l.Reason.StartsWith("released-by-plan-change") ? "released-by-plan-change" : l.Reason;
                    if (!byReason.TryGetValue(key, out var c)) byReason[key] = c = new int[2];
                    c[0]++; if (hit) c[1]++;
                }
                sb.AppendLine($"lunge trace: {_done.Count} completed lunges, {hits} hits");
                foreach (var kv in byReason) sb.AppendLine($"  {kv.Key,-28} {kv.Value[1]}/{kv.Value[0]}");
                foreach (var line in Lines) sb.AppendLine("  " + line);
                return sb.ToString();
            }
        }

        private static string SlipperLine()
        {
            var sb = new StringBuilder("slippers: ");
            foreach (var s in Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None))
            {
                if (s == null) continue;
                var owner = GameServices.Round?.PlayerAt(s.OwnerSlot);
                float d3 = owner != null
                    ? Vector3.Distance(owner.transform.position, s.transform.position)
                    : -1.0f;
                sb.Append($"[own={s.OwnerSlot} {s.State} at " +
                          $"{s.transform.position.x:F2},{s.transform.position.y:F2},{s.transform.position.z:F2} " +
                          $"d3={d3:F2} grabbable={(owner != null && s.CanBeGrabbedBy(owner))}] ");
            }
            return sb.ToString();
        }
    }
}
