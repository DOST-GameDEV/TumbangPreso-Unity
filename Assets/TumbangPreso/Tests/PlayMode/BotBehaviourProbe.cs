using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Do the bots actually PLAY, in both modes, for a whole match?
    ///
    /// ⚠️⚠️ REPORTED AS *"ai is broken af in every game mode"*, AND EVERY EXISTING SUITE COULD
    /// PASS WHILE THAT WAS TRUE. `BotMotionProbe` proves they walk. `AiLaneTests` drives one
    /// heuristic by reflection. `MatchRunTests` proves the rounds advance. Not one of them asks
    /// whether a slipper was ever thrown, whether the lata was ever knocked over, whether the
    /// taya ever tagged anybody, or whether a hero ever pressed a skill. A match where four
    /// bots jog in circles for six minutes passes all three.
    ///
    /// So this runs the real arena, from the real installer, to the end of a real four-round
    /// match, in BOTH modes, and counts what happened. The counters are written to
    /// `Logs/bot-behaviour-*.txt` in full, because "the AI is bad" has a dozen causes and a
    /// pass/fail cannot separate them.
    ///
    /// ⚠️ THE MATCH IS STARTED THROUGH THE SCENE'S OWN `SliceRunner`, NOT BY CALLING
    /// `StartMatch`. `MatchInstaller` leaves `AutoStart` off because the ready gate owns the
    /// opening, and a probe has nobody to press R. Calling `Begin()` is exactly what the gate
    /// does, so this measures the arrangement that ships instead of a hand-wired one.
    ///
    /// ⚠️ AND THE CLOCK IS 12x, NOT 60x. `Time.maximumDeltaTime` caps how many physics steps a
    /// frame may run, so past roughly 16x the extra scale buys nothing and the AI, which thinks
    /// in `Update` on `Time.deltaTime`, starts making one decision per several simulated
    /// seconds. That is a measurement of the harness rather than of the bots.
    /// </summary>
    public class BotBehaviourProbe
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
        /// How much game time one frame of this probe advances.
        ///
        /// ⚠️⚠️ THIS REPLACED `Time.timeScale = 6` AND THE DIFFERENCE IS THE WHOLE POINT OF THE
        /// PROBE. Under a time scale the match was stepped in REAL time, so the number of frames
        /// a match got depended on how fast the machine happened to run, and the AI decides in
        /// `Update` on `Time.deltaTime`. Two runs of the SAME BUILD, with the SAME SEED, back to
        /// back, measured **530 and then 83** unretrieved-slipper penalties. Every number this
        /// probe printed was therefore a liveness signal and nothing more: `docs/TODO.md` § 10
        /// recorded that it could not answer a comparison at all, and § 0 and § 5 were both
        /// blocked on it, because "is this cooldown better than that one" is a comparison.
        ///
        /// `Time.captureDeltaTime` makes `Time.deltaTime` a constant, so a frame advances the
        /// same slice of game time whatever the machine is doing. The wall clock stops being an
        /// input to the result.
        ///
        /// ⚠️⚠️ 1/60 s, AND IT WAS MEASURED RATHER THAN REASONED. THE FIRST ATTEMPT USED 1/30
        /// AND IT CHANGED THE BOTS. The old 6x run's `deltaTime` was estimated at about 0.033 s
        /// from a wall clock, that estimate was wrong, and the run that proved it is worth
        /// recording: at 1/30 a Classic match reported **9 throws, 0 tags and 673
        /// unretrieved-slipper penalties**, against 47 throws, 52 tags and 0 penalties on the
        /// same code the day before. The AI decides once per `Update` on `Time.deltaTime`, so
        /// the step IS the reaction rate, and halving it does not halve the numbers: a bot that
        /// re-decides half as often loses a 2.5 s charge to an interruption it would otherwise
        /// have steered around, and the effects compound.
        ///
        /// 1/60 reproduces the shipped numbers. ⚠️ **THE STEP IS NOW A TUNING CONSTANT OF THE
        /// AI, NOT A HARNESS DETAIL.** Changing it changes what the bots do, so treat it exactly
        /// like `AiTuning`: if it moves, every recorded figure in `Logs/bot-behaviour-*.txt`
        /// moves with it and none of them may be compared across the change.
        ///
        /// ⚠️ IT COSTS WALL CLOCK AND THAT IS THE PRICE OF A COMPARABLE NUMBER. A Classic match
        /// is 4 rounds of 90 s, so 21600 frames; Hero Strike is 8 rounds, so 43200.
        /// </summary>
        /// <summary>
        /// How much game time one frame of this probe advances.
        ///
        /// ⚠️⚠️ 1/60 s, AND IT IS NOT A ROUND NUMBER, IT IS THE ONLY VALUE THE BOTS PLAY AT.
        /// Three have been measured on this build and two of them are unplayable:
        ///
        /// | frame step | Update : FixedUpdate | what the bots did |
        /// |---|---|---|
        /// | 1/30 s | 1 : 1.67 | 9 throws, 0 tags, 673 idle penalties in a Classic match |
        /// | **1/60 s** | **1.2 : 1** | **the shipped figures: 40 to 90 throws, kits cast** |
        /// | 0.02 s | 1 : 1 | 18 throws, **0 skill uses**, three seats travelling 190 m |
        /// | 1/60 s with physics pinned to match | 1 : 1 | 20 retrievals, under the floor |
        ///
        /// ⚠️⚠️ READ THE TWO 1:1 ROWS TOGETHER: THE COLLAPSE FOLLOWS THE RATIO, NOT THE RATE.
        /// Both ways of making a frame carry exactly one physics step break the bots, at two
        /// different rates, while the mismatched 60-against-50 they ship with is healthy. That is
        /// not a fact about this probe, it is a fact about the AI, and `docs/TODO.md` § 17 is the
        /// entry it earned: a player whose machine renders at 50 fps is in the collapsing
        /// configuration.
        ///
        /// `Time.captureDeltaTime` makes `Time.deltaTime` a constant, so a frame advances the
        /// same slice of game time whatever the machine is doing. What it does NOT do is pin
        /// `Time.unscaledTime`, which is why `Hitstop` had to be taught about captures before
        /// two runs could be compared at all (§ 16).
        ///
        /// ⚠️ THE STEP IS A TUNING CONSTANT OF THE AI, NOT A HARNESS DETAIL. If it moves, every
        /// figure in `Logs/bot-behaviour-*.txt` moves with it and none may be compared across the
        /// change.
        /// </summary>
        private const float FixedStep = 1.0f / 60.0f;

        [TearDown]
        public void TearDown()
        {
            Hitstop.End();
            Time.timeScale = 1.0f;

            // ⚠️⚠️ THIS MUST BE CLEARED OR IT LEAKS INTO EVERY TEST THAT RUNS AFTER THIS ONE.
            // `Time.captureDeltaTime` is global and persists across scene loads, so a suite that
            // ran this probe first would silently pin every later PlayMode test to 30 frames a
            // second of game time regardless of what it was measuring.
            Time.captureDeltaTime = 0.0f;

            // ⚠️ AND THE SWEEP'S RATE WITH IT, for the same reason and with worse consequences:
            // a leaked overclock rate is a BALANCE change that every later test would measure
            // without knowing. See `OverheadPassWindow.AppliedRate`.
            OverheadPassWindow.RestoreAppliedRate();
        }

        /// <summary>
        /// ⚠️ THE DEFAULT MAP. Eskinita is the one every previous number in
        /// `Logs/bot-behaviour-*.txt` was measured on, so it stays the default and a second map
        /// is an addition rather than a replacement.
        /// </summary>
        private const string DefaultMap = "Eskinita";

        [UnityTest, Timeout(MatchTimeoutMs)]
        public IEnumerator ClassicBotsPlayAWholeMatch()
        {
            yield return RunMatch(GameMode.Classic, DefaultMap);
        }

        /// <summary>
        /// ⚠️⚠️ THE 180 s DEFAULT IS NOT A BOUND ON THIS PROBE, IT IS A COIN FLIP, AND WHEN IT
        /// LOSES IT TAKES OTHER TESTS DOWN WITH IT. Measured 2026-08-26 on an otherwise idle
        /// machine: 174.2 s here and 170.2 s on the bridge, against NUnit's 180 s default. Both
        /// pass alone and the bridge one timed out inside a full suite run on the same build.
        ///
        /// ⚠️ AND A TIMEOUT HERE IS NOT A LOCAL FAILURE. `CarryTests.TheViewmodelCarriesItsOwn
        /// SlipperInFirstPerson` and `LandedHighlightTests.TurningTheHighlightOffUnlightsASlipper
        /// AlreadyResting` both went red in that same run and both pass on their own: an aborted
        /// match leaves a live arena and the `DontDestroyOnLoad` directors mid-round, and the next
        /// test's slipper is teleported home under it. One flaky clock read as three bugs.
        ///
        /// ⚠️ 420 s IS A CEILING, NOT A BUDGET. The probe is a liveness floor and the whole match
        /// is the measurement; this number exists so a busy machine cannot turn it red, not so a
        /// slower one can be tolerated. If a run ever approaches it, the probe has stopped
        /// stepping the world at the rate `RunMatch` sets and that is the bug.
        /// </summary>
        private const int MatchTimeoutMs = 420000;

        [UnityTest, Timeout(MatchTimeoutMs)]
        public IEnumerator HeroStrikeBotsPlayAWholeMatchAndUseTheirKits()
        {
            yield return RunMatch(GameMode.HeroStrike, DefaultMap);
        }

        /// <summary>
        /// The same Hero Strike match on Ilalim ng Tulay, which is the only map with a mechanic
        /// of its own.
        ///
        /// ⚠️⚠️ THE HARNESS HAD NEVER RUN A MATCH ON A SECOND MAP AT ALL, and two separate
        /// entries in `docs/TODO.md` are arguments that map geometry changes Hero Strike
        /// outcomes: § 4 (Bayan Plaza's monument inside the defender's box) and
        /// `docs/Ilalim_Ng_Tulay.md` § 1 (why the other two maps feel wrong for the mode).
        /// Nothing measured either claim, because every probe loaded Eskinita.
        ///
        /// ⚠️⚠️ THIS IS NOT THE `docs/TODO.md` § 5 A/B AND MUST NOT BE REPORTED AS ONE. That
        /// entry wants the overclock window compared at different values, and this probe cannot
        /// answer a comparison: read the seeding note in `RunMatch`, where two runs of the SAME
        /// seeded build measured **530 and 83** unretrieved-slipper penalties back to back
        /// because the match is stepped in real time and the bots think in frames. What this
        /// run does is exercise the map's own code path — the flyby, the overclock window, the
        /// eight pillar hazards and the trip hazards — against the same LIVENESS FLOORS, so a
        /// map that breaks the loop is caught. A difference in the counts between the two maps
        /// is noise until the probe steps the world by hand.
        /// </summary>
        [UnityTest, Timeout(MatchTimeoutMs)]
        public IEnumerator HeroStrikeBotsPlayAWholeMatchUnderTheBridge()
        {
            yield return RunMatch(GameMode.HeroStrike, "IlalimNgTulay");
        }

        /// <summary>
        /// The same match, twice, to measure how far apart two identical runs land.
        ///
        /// ⚠️⚠️ IT STARTED LIFE ASSERTING THEY WOULD BE IDENTICAL AND THEY ARE NOT. `docs/TODO.md`
        /// § 10 closed on the argument that a seed plus `Time.captureDeltaTime` removes the clock
        /// as an input; this is the first thing that ever ran one configuration twice, and it
        /// found **18 skill uses and 43 throws against 37 and 83** on one build, one seed, one
        /// session. Two real holes were found and closed by chasing it (`Hitstop`'s real-time
        /// deadline, and the first match of a session never being warmed up), and the runs are
        /// still not identical. § 16 carries what is known and what is not.
        ///
        /// ⚠️⚠️ SO IT MEASURES THE NOISE FLOOR INSTEAD, AND THAT IS THE NUMBER AN A/B ACTUALLY
        /// NEEDS. Eight matches at the shipped settings spread from 58 to 100 throws around a
        /// mean near 80, which is about **20 per cent**. A single run per arm therefore cannot
        /// resolve anything smaller than roughly half that spread again, and every open balance
        /// question in `docs/TODO.md` that wants an A/B (§ 0, § 5) has to buy repeats instead:
        /// three runs an arm brings the error on the mean down to about 11 per cent.
        ///
        /// ⚠️ WHAT IT GATES IS A COLLAPSE, NOT A DIFFERENCE. 40 per cent is twice the observed
        /// spread: two runs further apart than that are not noise, they are a build where
        /// something has stopped working for one of them, which is exactly what the 1:1 frame
        /// experiments in § 17 looked like.
        /// </summary>
        [UnityTest, Timeout(MatchTimeoutMs * 2), Category("WallClock")]
        public IEnumerator TwoIdenticalMatchesLandInsideTheNoiseFloor()
        {
            yield return RunMatch(GameMode.HeroStrike, "IlalimNgTulay", "twin-a");
            string first = _lastReport;
            int firstThrows = _lastTally.Throws;
            int firstSkills = _lastTally.SkillUses;

            yield return RunMatch(GameMode.HeroStrike, "IlalimNgTulay", "twin-b");
            string second = _lastReport;
            int secondThrows = _lastTally.Throws;
            int secondSkills = _lastTally.SkillUses;

            float throwSpread = Spread(firstThrows, secondThrows);
            float skillSpread = Spread(firstSkills, secondSkills);

            var report = new StringBuilder();
            report.AppendLine("TWO RUNS OF ONE BUILD, ONE SEED, ONE SESSION.");
            report.AppendLine();
            report.AppendLine($"throws     {firstThrows,6} {secondThrows,6}   spread {throwSpread:P0}");
            report.AppendLine($"skill uses {firstSkills,6} {secondSkills,6}   spread {skillSpread:P0}");
            report.AppendLine();
            report.AppendLine(first == second
                ? "The two reports are identical. If this ever prints, docs/TODO.md section 16 "
                  + "can be closed and the sweep can be read at n = 1."
                : "The two reports differ. Section 16 is still open; size an A/B with repeats.");
            report.AppendLine();
            report.AppendLine("--- first ---");
            report.AppendLine(first);
            report.AppendLine("--- second ---");
            report.AppendLine(second);

            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/determinism.txt", report.ToString());
            Debug.Log(report.ToString());

            Assert.Less(throwSpread, 0.40f,
                $"two runs of the same match threw {firstThrows} and {secondThrows} slippers, "
                + $"{throwSpread:P0} apart. The measured noise floor is about 20 per cent, so "
                + "this is not noise: something works in one run and not the other. Read "
                + "Logs/determinism.txt.");
        }

        /// <summary>Difference as a fraction of the larger of the two, so it is symmetric.</summary>
        private static float Spread(int a, int b)
        {
            int high = Mathf.Max(a, b);
            return high <= 0 ? 0.0f : Mathf.Abs(a - b) / (float)high;
        }

        /// <summary>
        /// The overclock sweep `docs/TODO.md` § 5 has owed since 2026-08-25, plus the proof that
        /// the harness can answer it.
        ///
        /// ⚠️⚠️ IT RUNS THE SHIPPED RATE TWICE, FIRST AND LAST, AND ASSERTS THE TWO REPORTS
        /// ARE IDENTICAL. That is not a formality. Every earlier attempt at this comparison was
        /// refused on the grounds that the probe measured the machine as much as the build
        /// (§ 10: two runs of the same seeded build, back to back, reported **530 and then 83**
        /// unretrieved-slipper penalties), and a sweep run on a harness whose determinism is
        /// ASSUMED is exactly the mistake `docs/TODO.md` § 13 is about. If the two ends of this
        /// run disagree, the middle of it means nothing and the test says so instead of printing
        /// a table somebody would quote.
        ///
        /// ⚠️ WALL CLOCK IS DROPPED BEFORE THE COMPARISON and everything else is included:
        /// frames, every score event, both stray-distance figures, per-seat travel and per-seat
        /// score. A determinism check that only compared the headline counts would pass a run
        /// that diverged in the last round.
        ///
        /// ⚠️ THE RATES. 1.0 is the window OFF, which is the honest floor for "is this
        /// mechanic worth anything". 2.25 is the midpoint, so a result that is not monotonic is
        /// visible rather than interpolated. 3.5 is what ships, derived from
        /// `OverheadPassWindow.OverclockSeconds` = 6.75 s.
        ///
        /// ⚠️⚠️ IT IS `WallClock` BECAUSE IT IS A REPORT, NOT A GATE, and that category is
        /// what `docs/TODO.md` § 7 named for exactly this: four whole Hero Strike matches is
        /// about eleven minutes, and the default PlayMode run may not grow by that. Run it with
        /// `./tools/verify.sh wallclock`, or on its own with
        /// `-testCategory "WallClock" -testFilter TumbangPreso.PlayTests.BotBehaviourProbe`.
        /// The category means "excluded from the default run"; its two members are excluded for
        /// different reasons, `AiDiagnosticProbe` because it is real-time and this because it is
        /// long.
        ///
        /// ⚠️ AND IT ASSERTS ALMOST NOTHING ABOUT THE OUTCOME ON PURPOSE. Which rate is right
        /// is a judgement about how much a map mechanic should be worth, and the point of the
        /// run is to put numbers in front of that judgement. What it does gate is the two things
        /// that would make the numbers lies: that the harness is deterministic, and that the
        /// rate it was told to apply is the rate the window actually applied.
        /// </summary>
        [UnityTest, Timeout(SweepTimeoutMs), Category("WallClock")]
        public IEnumerator TheOverclockWindowSweep()
        {
            const string Arena = "IlalimNgTulay";

            float shipped = ShippedRate;
            var rates = new[] { shipped, 1.0f, 2.25f, shipped };
            var labels = new[] { "ship-a", "off", "mid", "ship-b" };
            var rows = new List<string>();
            var reports = new string[rates.Length];

            var sweep = new StringBuilder();
            sweep.AppendLine("THE OVERCLOCK WINDOW, MEASURED. docs/TODO.md section 5.");
            sweep.AppendLine();
            sweep.AppendLine($"Hero Strike, {Arena}, {MatchRules.RoundCountFor(GameMode.HeroStrike)} rounds, " +
                             $"seeded, stepped at {FixedStep * 1000.0f:F1} ms.");
            sweep.AppendLine($"The window is {PassSeconds:F2} s of every 24 s. A rate r saves " +
                             $"PassSeconds * (r - 1) seconds of cooldown per pass, whatever the cooldown is.");
            sweep.AppendLine();
            sweep.AppendLine($"{"rate",6} {"saves",7} {"skills",7} {"ults",5} {"knocks",7} {"tags",5} " +
                             $"{"throws",7} {"retr",5} {"restores",9} {"idlePen",8} {"frames",7}");
            sweep.AppendLine(new string('-', 92));

            for (int i = 0; i < rates.Length; i++)
            {
                OverheadPassWindow.SetAppliedRateForMeasurement(rates[i]);

                Assert.AreEqual(rates[i], OverheadPassWindow.AppliedRate, 0.0001f,
                    "the window did not take the rate it was handed, so this row would be a lie");

                yield return RunMatch(GameMode.HeroStrike, Arena, labels[i]);

                reports[i] = _lastReport;
                var t = _lastTally;

                rows.Add($"{rates[i],6:F2} {PassSeconds * (rates[i] - 1.0f),6:F2}s " +
                         $"{t.SkillUses,7} {t.UltimateUses,5} {t.LataKnocks,7} {t.Tags,5} " +
                         $"{t.Throws,7} {t.Retrievals,5} {t.LataRestores,9} {t.IdlePenalties,8} " +
                         $"{_lastFrames,7}   ({labels[i]})");

                // ⚠️ WRITTEN AFTER EVERY ROW, NOT AT THE END. Each row is three minutes of
                // matches, and `RunMatch` asserts liveness floors of its own: a run that goes
                // red at the third rate would otherwise throw away the two that had already been
                // measured, and the next reader would have nothing to look at but the failure.
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/overclock-sweep.txt", sweep.ToString() +
                                  string.Join("\n", rows) + "\n");
            }

            OverheadPassWindow.RestoreAppliedRate();

            foreach (string row in rows) sweep.AppendLine(row);

            bool deterministic = reports[0] == reports[3];

            sweep.AppendLine();
            sweep.AppendLine(deterministic
                ? "DETERMINISM: the two runs at the shipped rate are identical, line for line, so "
                  + "the differences above are the rate and nothing else."
                : "DETERMINISM: THE TWO RUNS AT THE SHIPPED RATE DISAGREE. Everything above is noise.");

            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/overclock-sweep.txt", sweep.ToString());
            Debug.Log(sweep.ToString());

            Assert.IsTrue(deterministic,
                "two runs of the same build at the same rate produced different matches, so this "
                + "probe still cannot answer a comparison and docs/TODO.md section 5 stays open. "
                + "Read Logs/overclock-sweep.txt and the two Logs/bot-behaviour-*-ship-*.txt.");
        }

        /// <summary>Four whole Hero Strike matches. See the sweep's own note on why it is
        /// `WallClock`.</summary>
        private const int SweepTimeoutMs = 1500000;

        /// <summary>⚠️ ALIASES SO THE SWEEP READS AS ARITHMETIC RATHER THAN AS A LOOKUP. Both are
        /// the shipped constants and neither is a second copy of them.</summary>
        private static float ShippedRate => OverheadPassWindow.OverclockRate;
        private static float PassSeconds => OverheadPassWindow.PassSeconds;

        /// <summary>The tally of the last match `RunMatch` ran, for a caller comparing several.
        /// ⚠️ Written at the END of a match, so a run that threw leaves the previous one here
        /// rather than a half-filled one.</summary>
        private Tally _lastTally;

        /// <summary>The last match's report, verbatim, so two runs can be compared as text.</summary>
        private string _lastReport = "";

        /// <summary>Frames the last match took. ⚠️ The one number that proves two runs simulated
        /// the same amount of game time; the wall clock proves nothing.</summary>
        private int _lastFrames;

        private IEnumerator RunMatch(GameMode mode, string map, string label = null)
        {
            var previousMode = UI.SceneFlow.SelectedMode;
            UI.SceneFlow.SelectedMode = mode;

            // ⚠️⚠️ ALL FOUR SEATS ARE BOTS, AND UNTIL 2026-08-26 ONE OF THEM WAS NOT.
            // `GameLaunch.SoloSeat` defaults to 1, so seat 1 got a `PlayerInputReader` in a run
            // with no human at the keyboard and simply stood still: it travelled 23.1 m, 68.3 m
            // and 69.1 m in the three matches on the day this was found, against 460 to 1190 m
            // for the others, and scored 30 to 50 against 3000 to 6700. Every per-seat figure in
            // this report was diluted by a seat that could not play, and the travel floor below
            // was set just low enough not to notice.
            bool previousAllBots = GameLaunch.AllBots;
            GameLaunch.AllBots = true;

            // ⚠️ SEEDED. Personality rolls, loiter beats and the AI's tie-breaks all draw from
            // `UnityEngine.Random`, so an unseeded run varies for one more reason than it has to.
            //
            // ✅ **AND THE SEED IS NOW ENOUGH, WHICH IT WAS NOT UNTIL 2026-08-26.** The note that
            // stood here said the opposite in as many words: the match was stepped in real time
            // at 6x, so the number of frames depended on the machine and two seeded runs of one
            // build measured 530 and then 83 unretrieved-slipper penalties. `FixedStep` and
            // `Time.captureDeltaTime` remove the clock as an input, so seed plus fixed step is a
            // reproducible run. See `FixedStep`.
            //
            // ⚠️ THE SEED IS ARBITRARY AND MUST NOT BE TUNED TO MAKE A RUN PASS. If a real
            // regression lands, change the CODE. That was true when the numbers were noisy and
            // it is more true now that they are not: a seed picked to make a red run green is a
            // measurement of nothing.
            UnityEngine.Random.InitState(20260823);

            Hitstop.End();
            Time.timeScale = 1.0f;

            var load = SceneManager.LoadSceneAsync(map, LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");
            for (int i = 0; i < 25; i++) yield return null;

            var round = GameServices.Round;
            var match = GameServices.Match;
            Assert.IsNotNull(round, "The arena registered no round.");
            Assert.IsNotNull(match, "The arena registered no match.");

            var runner = Object.FindFirstObjectByType<SliceRunner>();
            Assert.IsNotNull(runner, "MatchInstaller built no SliceRunner to drive the match.");

            var tally = new Tally(mode);
            int totalRounds = MatchRules.RoundCountFor(mode);
            tally.Subscribe(match, round);

            var seats = new List<CharacterMotor>(round.Players);
            Assert.AreEqual(Balance.PlayerCount, seats.Count,
                "The arena did not seat four players.");

            var startAt = new Vector3[seats.Count];
            var lastAt = new Vector3[seats.Count];
            for (int i = 0; i < seats.Count; i++)
            {
                startAt[i] = seats[i].transform.position;
                lastAt[i] = startAt[i];
            }

            var travelled = new float[seats.Count];
            var slipperWasLoose = new Dictionary<Slipper, bool>();

            // ⚠️⚠️ SEEDED A SECOND TIME, HERE, AND THE FIRST SEED IS NOT ENOUGH ON ITS OWN.
            // The one above runs before `LoadSceneAsync`, so every draw the LOAD makes comes out
            // of the same stream: decorations, personality rolls and anything a one-time
            // initialiser does on the first scene of a session. If the first load in a session
            // consumes a different NUMBER of draws from the second, every draw after it is
            // shifted and the match that follows is a different match. Re-seeding at the last
            // moment before the whistle pins the MATCH regardless of what the load did.
            //
            // ⚠️ IT DOES NOT PIN THE CAST, which is picked during the load, and that is why the
            // report prints every seat's hero and model below. A difference there is visible
            // instead of being an unexplained difference in the counts.
            UnityEngine.Random.InitState(20260823);

            // ⚠️⚠️ THE STEP IS SET AFTER THE SCENE HAS SETTLED, NOT BEFORE. The 25 warm-up
            // frames above run at whatever rate the editor gives them, and pinning the step
            // across a scene load makes the load itself take a fixed number of frames rather
            // than as many as it needs.
            Time.timeScale = 1.0f;
            Time.captureDeltaTime = FixedStep;

            // ⚠️ THE PHYSICS RATE IS LEFT ALONE, AND SO IS THE MISMATCH. See `FixedStep`: every
            // attempt to make one frame carry exactly one physics step, at either rate, took the
            // bots below this file's own liveness floors.

            // ⚠️⚠️ 120 MORE FRAMES BEFORE THE WHISTLE, AND THEY BOUGHT THE FIRST MATCH OF A
            // SESSION ITS PLAY. This started as an attempt to align the physics PHASE: at 1/60 s
            // a frame against a 0.02 s step a frame carries 0, 1 or 2 `FixedUpdate`s, and where
            // in that cycle a match starts is inherited from the previous scene.
            // `Time.time - Time.fixedTime` never fell below the threshold, so the loop always
            // runs its full 120 and the phase it reports is 8 to 9 ms either way: **the
            // alignment does not work and the entry says so.**
            //
            // What it did do is measurable and worth keeping. Before it, the first match in a
            // session was reliably the worst one: 58 throws and 28 skill uses against 92 and 37
            // for the second, on the same build and seed. With it, the first match reports 100
            // throws, 41 skill uses and 144 idle penalties, which is the healthiest run this
            // probe has recorded. Two seconds of settled frames is the cheapest possible answer
            // to a cold start, and the 25 above were not enough.
            //
            // ⚠️ IT IS STILL NOT DETERMINISM. What is left is measured by
            // `TwoIdenticalMatchesLandInsideTheNoiseFloor`, and `docs/TODO.md` § 16 carries what
            // is known and what is not.
            int aligned = 0;
            while (aligned < 120 && Time.time - Time.fixedTime > 0.0005f)
            {
                aligned++;
                yield return null;
            }

            float phase = Time.time - Time.fixedTime;

            runner.Begin();

            // ⚠️ THE GUARD IS A FRAME COUNT NOW, NOT WALL CLOCK, AND THAT IS THE SAME CHANGE AS
            // EVERYTHING ELSE HERE. A guard measured in seconds is a second way for the machine's
            // speed to decide the result: on a slow run it would fire part-way through a match
            // that was progressing perfectly well, and the failure would read as a dead loop.
            // A frame budget expires at exactly the same point in the simulation every time.
            //
            // 90 s a round at `FixedStep` is 5400 frames, so a Hero Strike match is 43200 plus
            // the between-round handoffs. 64000 leaves about 48 per cent of headroom over the
            // longest legitimate match, which is a match that has genuinely stopped advancing
            // rather than a slow one.
            const int frameBudget = 64000;
            int frames = 0;
            float guard = 0.0f;
            float strayX = 0.0f;
            float strayZ = 0.0f;
            float bodyX = 0.0f;
            float bodyZ = 0.0f;
            var escapes = new List<string>();

            while (match.MatchInProgress && frames < frameBudget)
            {
                frames++;
                guard += Time.unscaledDeltaTime;

                for (int i = 0; i < seats.Count; i++)
                {
                    if (seats[i] == null) continue;

                    Vector3 now = seats[i].transform.position;
                    travelled[i] += Vector3.Distance(new Vector3(now.x, 0.0f, now.z),
                                                     new Vector3(lastAt[i].x, 0.0f, lastAt[i].z));
                    lastAt[i] = now;

                    bodyX = Mathf.Max(bodyX, Mathf.Abs(now.x));
                    bodyZ = Mathf.Max(bodyZ, Mathf.Abs(now.z));
                }

                foreach (var slipper in Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None))
                {
                    if (slipper == null) continue;

                    slipperWasLoose.TryGetValue(slipper, out bool wasLoose);

                    if (wasLoose && slipper.State == SlipperState.Held) tally.Retrievals++;
                    tally.NoteFlight(slipper);

                    // ⚠️ THE "WAS LOOSE" FLAG IS WRITTEN BEFORE ANY EARLY EXIT BELOW. Skipping
                    // it for held slippers left the flag stuck true for the rest of the match,
                    // so the retrieval counter incremented every frame and reported 90,885
                    // retrievals against 72 throws.
                    slipperWasLoose[slipper] = slipper.State == SlipperState.Loose;

                    // ⚠️ A HELD SLIPPER IS NOT MEASURED AGAINST THE WALL. It rides a hand
                    // anchor roughly 0.6 m in front of a body, so a player standing legally on
                    // the edge holds it fractionally past the line. Measuring that would fail
                    // the wall for doing its job. What has to stay inside is every slipper the
                    // game expects somebody to walk to.
                    if (slipper.State == SlipperState.Held) continue;

                    strayX = Mathf.Max(strayX, Mathf.Abs(slipper.transform.position.x));
                    strayZ = Mathf.Max(strayZ, Mathf.Abs(slipper.transform.position.z));

                    bool outside = Mathf.Abs(slipper.transform.position.x) > AIController.PlayableHalfX + 0.5f
                                || Mathf.Abs(slipper.transform.position.z) > AIController.PlayableHalfZ + 0.5f;

                    if (outside && escapes.Count < 12)
                        escapes.Add($"slipper own={slipper.OwnerSlot} at {slipper.transform.position} " +
                                    $"state={slipper.State} vel={slipper.Velocity} " +
                                    $"affinity={slipper.Affinity}");
                }

                tally.SampleKits(seats);

                // ⚠️ BOTH PER FRAME, AND `WatchFaces` IS IDEMPOTENT ON PURPOSE. The seats are
                // rebuilt between rounds, so a one-time subscription outside this loop would hook
                // the first round's bodies and count nothing for the seven after it.
                tally.WatchFaces(seats);
                tally.SampleFeet(seats);

                yield return null;
            }

            Time.captureDeltaTime = 0.0f;
            Time.timeScale = 1.0f;

            var log = new StringBuilder();
            log.AppendLine($"bot behaviour probe  ·  {mode}  ·  {map}");

            // ⚠️ THE FRAME COUNT IS THE REPRODUCIBLE NUMBER AND THE WALL CLOCK IS NOT. Both are
            // printed because the second one is how you notice the machine is struggling, but
            // only the first should ever be compared between two runs.
            log.AppendLine($"{frames} frames at {FixedStep * 1000.0f:F1} ms  ·  " +
                           $"{frames * FixedStep:F1}s simulated  ·  {guard:F1}s wall clock  ·  " +
                           $"match in progress at exit: {match.MatchInProgress}");

            // ⚠️ THE STARTING PHASE IS PART OF THE MEASUREMENT, not a diagnostic. Two runs that
            // began at different points of the frame-to-step cycle are two different experiments
            // (§ 17), so a reader comparing two reports has to be able to see that they did not.
            log.AppendLine($"physics phase at the whistle: {phase * 1000.0f:F3} ms " +
                           $"after {aligned} aligning frame(s)");
            log.AppendLine(tally.Describe());
            // ⚠️ THE CAST IS PART OF THE MEASUREMENT. Two runs with different heroes in the
            // seats are two different experiments, and until this line existed that difference
            // was invisible in a report full of counts.
            for (int i = 0; i < seats.Count; i++)
            {
                var kit = seats[i] != null ? seats[i].AbilitySystem?.Kit : null;
                var visual = seats[i] != null
                    ? seats[i].GetComponentInChildren<Visual.CharacterVisual>() : null;

                log.AppendLine($"seat {i} hero {(kit != null ? kit.HeroId : "-"),-12} " +
                               $"model {(visual != null && visual.Model != null ? visual.Model.name : "-")}");
            }

            log.AppendLine($"furthest a body reached: x {bodyX:F2} of {AIController.PlayableHalfX:F1}  " +
                           $"z {bodyZ:F2} of {AIController.PlayableHalfZ:F1}");
            log.AppendLine($"furthest a free slipper reached: x {strayX:F2}  z {strayZ:F2}");
            foreach (string escape in escapes) log.AppendLine("  escaped: " + escape);
            for (int i = 0; i < seats.Count; i++)
                log.AppendLine($"seat {i} travelled {travelled[i]:F1} m  final score {match.ScoreFor(i)}");

            Directory.CreateDirectory("Logs");

            string suffix = string.IsNullOrEmpty(label) ? "" : "-" + label;
            File.WriteAllText($"Logs/bot-behaviour-{mode}-{map}{suffix}.txt", log.ToString());
            Debug.Log(log.ToString());

            _lastTally = tally;
            _lastFrames = frames;

            // ⚠️ THE HEADER LINE IS DROPPED FROM THE COMPARABLE TEXT ON PURPOSE. It carries the
            // WALL CLOCK, which is the one number in the report that is allowed to differ
            // between two identical runs; comparing it would make the determinism check fail on
            // the machine being busy, which is the exact fault the fixed step removed.
            var comparable = new StringBuilder();
            foreach (string line in log.ToString().Split('\n'))
                if (!line.Contains("wall clock")) comparable.AppendLine(line.TrimEnd());

            _lastReport = comparable.ToString();

            UI.SceneFlow.SelectedMode = previousMode;
            GameLaunch.AllBots = previousAllBots;

            // ---- THE MATCH ITSELF ------------------------------------------------------
            Assert.IsFalse(match.MatchInProgress,
                $"{mode} on {map}: the match never ended inside {frames} frames " +
                $"({frames * FixedStep:F0}s simulated). See " +
                $"Logs/bot-behaviour-{mode}-{map}.txt.");

            for (int slot = 0; slot < Balance.PlayerCount; slot++)
                Assert.IsTrue(tally.Defended[slot],
                    $"{mode} on {map}: seat {slot} never defended, so the rotation stalled.");

            // ---- THEY LEAVE SPAWN ------------------------------------------------------
            // ⚠️ THE FLOOR ROSE FROM 20 m TO 150 m WITH `GameLaunch.AllBots`, AND THE OLD VALUE
            // WAS NOT CAUTIOUS, IT WAS BLIND. 20 m was low enough to pass the parked human seat
            // that used to sit in every run at 23 m. With four real bots the observed spread is
            // 460 to 1190 m over a Classic match, so 150 m is comfortably under anything a
            // playing bot does and comfortably over anything a stuck one does.
            for (int i = 0; i < seats.Count; i++)
                Assert.Greater(travelled[i], 150.0f,
                    $"{mode} on {map}: seat {i} covered {travelled[i]:F1} m across a whole match, which " +
                    "is a bot that is stuck rather than one that is playing.");

            // ---- NOTHING LEAVES THE ARENA ----------------------------------------------
            // ⚠️ THE WALL IS A REGRESSION GUARD NOW. A body outside the playable rectangle
            // takes its tsinelas with it, throws from out there, and strands the ammunition
            // somewhere no goal a bot is allowed to set can reach. It reads as a broken AI and
            // it is a missing collision bound. See CharacterMotor.Confine.
            //
            // ⚠️ HALF A METRE OF SLACK. The clamp lands a body ON the line and a capsule's own
            // radius can read fractionally past it for a frame.
            Assert.LessOrEqual(bodyX, AIController.PlayableHalfX + 0.1f,
                $"{mode} on {map}: a body reached x {bodyX:F2} against a half width of " +
                $"{AIController.PlayableHalfX:F1}, so somebody left the arena.");

            Assert.LessOrEqual(bodyZ, AIController.PlayableHalfZ + 0.1f,
                $"{mode} on {map}: a body reached z {bodyZ:F2} against a half depth of " +
                $"{AIController.PlayableHalfZ:F1}, so somebody left the arena.");

            Assert.LessOrEqual(strayX, AIController.PlayableHalfX + 0.5f,
                $"{mode} on {map}: a free slipper reached x {strayX:F2}, so a piece of ammunition " +
                "is somewhere no attacker is allowed to walk to.");

            Assert.LessOrEqual(strayZ, AIController.PlayableHalfZ + 0.5f,
                $"{mode} on {map}: a free slipper reached z {strayZ:F2}, so a piece of ammunition " +
                "is somewhere no attacker is allowed to walk to.");

            // ---- THE ATTACKING LOOP ----------------------------------------------------
            // ⚠️ FLOORS WELL ABOVE ONE, BECAUSE ONE OF EACH IS NOT A LOOP. The measured shape
            // of a healthy match at this time scale is 55 to 105 throws and about as many
            // retrievals; the shape of the broken one this probe was written to catch was 3
            // throws and 7 retrievals in four whole rounds. Twenty separates those two by a
            // wide margin in both directions.
            Assert.Greater(tally.Throws, 20,
                $"{mode} on {map}: only {tally.Throws} slippers were thrown in {totalRounds} rounds. A healthy " +
                "match at this time scale throws upwards of fifty.");

            Assert.Greater(tally.Retrievals, 20,
                $"{mode} on {map}: only {tally.Retrievals} loose slippers were picked back up, so the " +
                "retrieval half of the loop is stalling and rounds run out of ammunition.");

            Assert.Greater(tally.LataKnocks, 0,
                $"{mode} on {map}: the lata was never knocked over in {totalRounds} rounds. The bots throw but " +
                "cannot hit, which is an aim or a lane problem rather than a plan problem.");

            Assert.Greater(tally.LataRestores, 0,
                $"{mode} on {map}: the taya never righted the lata after a knockdown.");

            // ---- THE DEFENDING LOOP ----------------------------------------------------
            Assert.Greater(tally.Tags, 0,
                $"{mode} on {map}: no attacker was tagged in four whole rounds, so the taya never " +
                "closed on a vulnerable retriever.");

            // ---- THE TOURNAMENT RULES --------------------------------------------------
            // ⚠⚠ THE CEILING SEPARATES "DEAD" FROM "ALIVE" AND NOTHING FINER, BECAUSE
            // NOTHING FINER IS MEASURABLE HERE. Both penalties are charged ONCE PER SECOND for
            // as long as the violation lasts, so the count is a DURATION, not an event count,
            // and it therefore scales with how much of the match the bots spent in violation.
            //
            // The two populations, measured on this project rather than guessed:
            //
            //   * A DEAD retrieval loop posts one every second for every attacker for the whole
            //     match. 679 in Classic and 686 in Hero Strike when the tsinelas were stranding
            //     on rooftops; 661 to 687 when the bots were walking round hazards they could
            //     not path around. The ceiling available is roughly 1,080 attacker-seconds.
            //   * A LIVE loop on this same build, across seeded back-to-back runs, measured
            //     **1, 83, 110, 170, 207, 243, 467 and 530**. That spread is the clock, not the
            //     game: see the note on the seed above.
            //
            // ⚠⚠ SO THE OLD CEILING OF 200 SAT INSIDE THE LIVE POPULATION AND FAILED ON THE
            // DICE, which is worse than no assertion: it trains whoever sees it red to re-run
            // rather than to look. 600 is above every live measurement and comfortably below
            // every dead one.
            //
            // ⚠️ **IF YOU WANT A TIGHTER NUMBER, MAKE THE PROBE DETERMINISTIC FIRST** by
            // stepping the world by hand instead of running it at 6x against the wall clock.
            // Until then the FLOORS above are the assertions that carry weight.
            int deadLoopFloor = 600 * totalRounds / Balance.Rounds;

            Assert.Less(tally.CampPenalties, deadLoopFloor,
                $"{mode} on {map}: {tally.CampPenalties} can-camping penalties across the match. The " +
                "defender is parking inside the ring instead of guarding the approach.");

            Assert.Less(tally.IdlePenalties, deadLoopFloor,
                $"{mode} on {map}: {tally.IdlePenalties} unretrieved-slipper penalties. Attackers are " +
                "not reaching their tsinelas at all, which is the retrieval loop being dead " +
                "rather than the bots being cautious.");

            // ---- THE FACE AND THE FEET -------------------------------------------------
            //
            // ⚠️⚠️ THESE TWO FLOORS EXIST BECAUSE THE FEATURE THEY GUARD SHIPPED BROKEN AND
            // NOTHING NOTICED FOR A WEEK. Bot emote code was written, merged and played, and it
            // fired zero times: `AIController` runs at execution order -130 and writes a movement
            // axis every frame, `EmotePlayer` runs at 0 and cancels on any non-zero axis, so
            // every clip was cancelled by its own bot before a frame of it was drawn. Every
            // number in this report was byte-identical on both sides of that fault.
            //
            // ⚠️ A FLOOR OF ZERO IS THE RIGHT SHAPE HERE, NOT A TUNED BAND. The failure mode is
            // "this does not happen at all", not "this happens at the wrong rate", and a floor
            // that tries to pin the rate would fail on the dice instead (see the long note on
            // `deadLoopFloor` above for what that costs a reader).
            Assert.Greater(tally.Emotes, 0,
                $"{mode} on {map}: not one bot emoted in {totalRounds} rounds. Either the safety " +
                "gate in AIController.SafeToEmote never opens, or the hold is being cancelled by " +
                "the bot's own movement again, which is the fault the hold exists to fix.");

            Assert.Greater(tally.Hops, 0,
                $"{mode} on {map}: not one bot left the ground in {totalRounds} rounds. Bots walked " +
                "at one height for the whole port before AIController section THE FEET LEAVE THE " +
                "GROUND, and a body that never jumps is visible in a still frame.");

            // ⚠️ AND A CEILING ON THE EMOTES, BECAUSE THE OTHER FAILURE IS A BOT THAT STOPS
            // PLAYING TO DANCE. An emote is a self-inflicted stun: the hold is up to 2.3 s and
            // the cooldown at least 9, so about nine per seat per round is the arithmetic
            // ceiling if every roll wins and every moment is safe. Measured on 2026-08-28: 0.69
            // per seat per round in Hero Strike, 0.38 in Classic. Four per seat per round is far
            // above both and far below a bot that has stopped playing.
            Assert.Less(tally.Emotes, 4 * Balance.PlayerCount * totalRounds,
                $"{mode} on {map}: {tally.Emotes} emotes across the match. A bot standing still to " +
                "celebrate this often is not expressive, it is out of position.");

            // ---- THE HERO KITS ---------------------------------------------------------
            if (mode != GameMode.HeroStrike) yield break;

            Assert.IsTrue(tally.SawAnyKit,
                "Hero Strike seated nobody with a hero kit, so the mode did not install.");

            Assert.Greater(tally.SkillUses, 0,
                $"Hero Strike: not one skill was used in {totalRounds} rounds, so every bot ignored its " +
                "own kit.");

            // ⚠️ A CEILING AS WELL AS A FLOOR, BECAUSE SPAM IS THE OTHER FAILURE. A bot that
            // holds E down uses a skill every time the cooldown lifts, which over a six minute
            // match is several hundred activations and is exactly what "uses skills without
            // spamming uselessly" rules out.
            Assert.Less(tally.SkillUses, 260 * totalRounds / Balance.Rounds,
                $"Hero Strike: {tally.SkillUses} skill activations across the match is a bot " +
                "firing on cooldown rather than on an opportunity.");
        }

        /// <summary>Everything the match reported about itself, in one place.</summary>
        private sealed class Tally
        {
            private readonly GameMode _mode;
            private readonly HashSet<Slipper> _inFlight = new HashSet<Slipper>();
            private readonly Dictionary<HeroAbility, float> _lastCooldown =
                new Dictionary<HeroAbility, float>();
            private readonly Dictionary<HeroKit, float> _lastUltimate =
                new Dictionary<HeroKit, float>();

            public readonly bool[] Defended = new bool[Balance.PlayerCount];

            public int LataKnocks, Tags, Sabotages, CampPenalties, IdlePenalties;
            public int Throws, Retrievals, LataRestores, SkillUses, UltimateUses;
            public bool SawAnyKit;

            // -------------------------------------------------------------------
            // ⚠️⚠️ EMOTES AND HOPS ARE COUNTED BECAUSE THIS PROBE COULD NOT SEE EITHER OF THEM,
            // AND THE THING IT COULD NOT SEE HAD BEEN BROKEN THE WHOLE TIME. Bot emote code
            // shipped, fired never (`AIController` § THE FACE has the execution-order cause), and
            // every number in this report was identical on both sides of that fault. A row here
            // is the difference between "the celebration is tuned to be rare" and "the
            // celebration does not exist", which is exactly the pair a reader cannot tell apart
            // without one.
            //
            // ⚠️ HOPS ARE COUNTED OFF THE RESOLVED BODY, NOT OFF THE KEY. A tripped bot MASHES
            // jump to get up (`AIController.Update`), and those presses are refused by
            // `CanAct`: counting the key would report hundreds of hops per match, all of them
            // fictional. A grounded body whose vertical velocity turns positive has actually
            // left the ground and can have done it no other way.
            // -------------------------------------------------------------------
            public int Emotes, Hops;

            public Tally(GameMode mode) => _mode = mode;

            public void Subscribe(MatchDirector match, RoundDirector round)
            {
                match.RoundStarted += (_, defender) =>
                {
                    if (defender >= 0 && defender < Defended.Length) Defended[defender] = true;
                };

                match.Scored += (_, e) =>
                {
                    switch (e)
                    {
                        case ScoreEvent.LataKnocked: LataKnocks++; break;
                        case ScoreEvent.Tag: Tags++; break;
                        case ScoreEvent.Sabotage: Sabotages++; break;
                        case ScoreEvent.TayaCampPenalty: CampPenalties++; break;
                        case ScoreEvent.UnretrievedSlipperPenalty: IdlePenalties++; break;
                    }
                };

                round.LataRestored += () => LataRestores++;
            }

            /// <summary>
            /// ⚠️ SUBSCRIBED PER SEAT AND SEPARATELY FROM `Subscribe`, because the seats do not
            /// exist yet when the directors do. `MatchInstaller` builds the bodies after the
            /// match, so hooking these in `Subscribe` would hook nothing and report a flat zero,
            /// which is the exact failure mode this counter was added to detect.
            /// </summary>
            public void WatchFaces(List<CharacterMotor> seats)
            {
                foreach (var seat in seats)
                {
                    var face = seat != null ? seat.GetComponent<Social.EmotePlayer>() : null;
                    if (face == null || !_watched.Add(face)) continue;

                    face.EmoteStarted += _ => Emotes++;
                }
            }

            private readonly HashSet<Social.EmotePlayer> _watched =
                new HashSet<Social.EmotePlayer>();

            /// <summary>
            /// One count per take-off. ⚠️ THE PREVIOUS SAMPLE IS PER SEAT, so four bodies do not
            /// share one edge detector and cancel each other's jumps out.
            /// </summary>
            public void SampleFeet(List<CharacterMotor> seats)
            {
                foreach (var seat in seats)
                {
                    if (seat == null) continue;

                    _airborne.TryGetValue(seat, out bool wasUp);
                    bool isUp = !seat.IsGrounded && seat.Velocity.y > 0.5f;

                    if (isUp && !wasUp) Hops++;
                    _airborne[seat] = isUp;
                }
            }

            private readonly Dictionary<CharacterMotor, bool> _airborne =
                new Dictionary<CharacterMotor, bool>();

            /// <summary>
            /// One count per FLIGHT, not one per frame and not one per slipper.
            ///
            /// ⚠️ THE SET HAS TO BE EMPTIED AGAIN, and the first version of this never was. A
            /// slipper that is only ever ADDED counts once for its whole life, so a match with
            /// four tsinelas could not report more than four throws however many were made:
            /// it read "throws 3" beside "retrievals 88", which is arithmetic nobody should
            /// have believed. Leaving flight is what makes the next throw a new event.
            /// </summary>
            public void NoteFlight(Slipper slipper)
            {
                if (slipper.State == SlipperState.InFlight)
                {
                    if (_inFlight.Add(slipper)) Throws++;
                }
                else _inFlight.Remove(slipper);
            }

            /// <summary>
            /// A skill use is a cooldown that went UP. There is no activation event to listen
            /// to, and counting "is on cooldown" would count one press once per frame.
            /// </summary>
            public void SampleKits(List<CharacterMotor> seats)
            {
                if (_mode != GameMode.HeroStrike) return;

                foreach (var seat in seats)
                {
                    var system = seat != null ? seat.AbilitySystem : null;
                    if (system?.Kit == null) continue;

                    SawAnyKit = true;

                    Count(system.Kit.Skill1);
                    Count(system.Kit.Skill2);
                    CountUltimate(system.Kit);
                }
            }

            private void Count(HeroAbility ability)
            {
                if (ability == null) return;

                _lastCooldown.TryGetValue(ability, out float previous);
                if (ability.CooldownRemaining > previous + 0.01f) SkillUses++;
                _lastCooldown[ability] = ability.CooldownRemaining;
            }

            /// <summary>
            /// ⚠️ AN ULTIMATE IS COUNTED BY ITS CHARGE EMPTYING, NOT BY A COOLDOWN. Every
            /// ultimate in the game is authored with `Cooldown = 0` because the CHARGE is its
            /// cost, so the cooldown test that works for E and Q can never fire for F and
            /// reported a flat zero for a whole match. `TryActivateUltimate` sets the charge
            /// back to 0 on success, and a full bar dropping to empty happens on no other path.
            /// </summary>
            private void CountUltimate(HeroKit kit)
            {
                // ⚠️ THE THRESHOLD IS THE KIT'S OWN COST, NOT `UltimateMax`. Since 2026-08-25
                // each hero pays a different price (90 for Nemu up to 150 for Zack), so half of
                // the shared 100 is above Nemu's whole meter and would have counted every one
                // of her casts twice while missing Zack's entirely.
                _lastUltimate.TryGetValue(kit, out float previous);
                if (previous > kit.UltimateCost * 0.5f && kit.UltimateCharge <= 0.01f)
                    UltimateUses++;
                _lastUltimate[kit] = kit.UltimateCharge;
            }

            public string Describe()
                => $"lata knocks {LataKnocks}  tags {Tags}  sabotages {Sabotages}\n" +
                   $"throws {Throws}  retrievals {Retrievals}  lata restores {LataRestores}\n" +
                   $"camp penalties {CampPenalties}  idle penalties {IdlePenalties}\n" +
                   $"skill uses {SkillUses}  ultimate uses {UltimateUses}  kits seen {SawAnyKit}\n" +
                   $"emotes {Emotes}  hops {Hops}";
        }
    }
}
