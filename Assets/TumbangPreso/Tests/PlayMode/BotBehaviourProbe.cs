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
        /// ⚠️ 6x, AND THE NUMBER IS A COMPROMISE THAT IS WORTH STATING. The AI decides in
        /// `Update` on `Time.deltaTime`, so every extra multiple of time scale is a bot that
        /// thinks less often per simulated second. At 12x a whole match ran in 34 seconds and
        /// reported 3 throws; at 6x the same code reports several times that, and
        /// `AiDiagnosticProbe` at 1x reports more again. None of those numbers is wrong: they
        /// are the same bots at three different effective reaction rates. 6x keeps a four round
        /// match inside a minute of wall clock while leaving the bots enough decisions that a
        /// failure here is about them rather than about the harness, and every assertion below
        /// is a FLOOR, so a slower reaction rate can only make it harder to pass.
        /// </summary>
        private const float MatchTimeScale = 6.0f;

        [TearDown]
        public void TearDown()
        {
            Hitstop.End();
            Time.timeScale = 1.0f;
        }

        [UnityTest]
        public IEnumerator ClassicBotsPlayAWholeMatch()
        {
            yield return RunMatch(GameMode.Classic);
        }

        [UnityTest]
        public IEnumerator HeroStrikeBotsPlayAWholeMatchAndUseTheirKits()
        {
            yield return RunMatch(GameMode.HeroStrike);
        }

        private IEnumerator RunMatch(GameMode mode)
        {
            var previousMode = UI.SceneFlow.SelectedMode;
            UI.SceneFlow.SelectedMode = mode;

            Hitstop.End();
            Time.timeScale = 1.0f;

            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            while (load != null && !load.isDone) yield return null;
            for (int i = 0; i < 25; i++) yield return null;

            var round = GameServices.Round;
            var match = GameServices.Match;
            Assert.IsNotNull(round, "The arena registered no round.");
            Assert.IsNotNull(match, "The arena registered no match.");

            var runner = Object.FindFirstObjectByType<SliceRunner>();
            Assert.IsNotNull(runner, "MatchInstaller built no SliceRunner to drive the match.");

            var tally = new Tally(mode);
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

            runner.Begin();
            Time.timeScale = MatchTimeScale;

            // ⚠️ THE GUARD IS UNSCALED WALL CLOCK. A guard measured in scaled time cannot
            // expire when the thing that has gone wrong is the clock itself.
            float guard = 0.0f;
            float strayX = 0.0f;
            float strayZ = 0.0f;
            float bodyX = 0.0f;
            float bodyZ = 0.0f;
            var escapes = new List<string>();

            while (match.MatchInProgress && guard < 240.0f)
            {
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

                yield return null;
            }

            Time.timeScale = 1.0f;

            var log = new StringBuilder();
            log.AppendLine($"bot behaviour probe  ·  {mode}");
            log.AppendLine($"wall clock {guard:F1}s  ·  match in progress at exit: {match.MatchInProgress}");
            log.AppendLine(tally.Describe());
            log.AppendLine($"furthest a body reached: x {bodyX:F2} of {AIController.PlayableHalfX:F1}  " +
                           $"z {bodyZ:F2} of {AIController.PlayableHalfZ:F1}");
            log.AppendLine($"furthest a free slipper reached: x {strayX:F2}  z {strayZ:F2}");
            foreach (string escape in escapes) log.AppendLine("  escaped: " + escape);
            for (int i = 0; i < seats.Count; i++)
                log.AppendLine($"seat {i} travelled {travelled[i]:F1} m  final score {match.ScoreFor(i)}");

            Directory.CreateDirectory("Logs");
            File.WriteAllText($"Logs/bot-behaviour-{mode}.txt", log.ToString());
            Debug.Log(log.ToString());

            UI.SceneFlow.SelectedMode = previousMode;

            // ---- THE MATCH ITSELF ------------------------------------------------------
            Assert.IsFalse(match.MatchInProgress,
                $"{mode}: the match never ended inside {guard:F0}s of wall clock. See " +
                $"Logs/bot-behaviour-{mode}.txt.");

            for (int slot = 0; slot < Balance.PlayerCount; slot++)
                Assert.IsTrue(tally.Defended[slot],
                    $"{mode}: seat {slot} never defended, so the rotation stalled.");

            // ---- THEY LEAVE SPAWN ------------------------------------------------------
            for (int i = 0; i < seats.Count; i++)
                Assert.Greater(travelled[i], 20.0f,
                    $"{mode}: seat {i} covered {travelled[i]:F1} m across a whole match, which " +
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
                $"{mode}: a body reached x {bodyX:F2} against a half width of " +
                $"{AIController.PlayableHalfX:F1}, so somebody left the arena.");

            Assert.LessOrEqual(bodyZ, AIController.PlayableHalfZ + 0.1f,
                $"{mode}: a body reached z {bodyZ:F2} against a half depth of " +
                $"{AIController.PlayableHalfZ:F1}, so somebody left the arena.");

            Assert.LessOrEqual(strayX, AIController.PlayableHalfX + 0.5f,
                $"{mode}: a free slipper reached x {strayX:F2}, so a piece of ammunition " +
                "is somewhere no attacker is allowed to walk to.");

            Assert.LessOrEqual(strayZ, AIController.PlayableHalfZ + 0.5f,
                $"{mode}: a free slipper reached z {strayZ:F2}, so a piece of ammunition " +
                "is somewhere no attacker is allowed to walk to.");

            // ---- THE ATTACKING LOOP ----------------------------------------------------
            // ⚠️ FLOORS WELL ABOVE ONE, BECAUSE ONE OF EACH IS NOT A LOOP. The measured shape
            // of a healthy match at this time scale is 55 to 105 throws and about as many
            // retrievals; the shape of the broken one this probe was written to catch was 3
            // throws and 7 retrievals in four whole rounds. Twenty separates those two by a
            // wide margin in both directions.
            Assert.Greater(tally.Throws, 20,
                $"{mode}: only {tally.Throws} slippers were thrown in four rounds. A healthy " +
                "match at this time scale throws upwards of fifty.");

            Assert.Greater(tally.Retrievals, 20,
                $"{mode}: only {tally.Retrievals} loose slippers were picked back up, so the " +
                "retrieval half of the loop is stalling and rounds run out of ammunition.");

            Assert.Greater(tally.LataKnocks, 0,
                $"{mode}: the lata was never knocked over in four rounds. The bots throw but " +
                "cannot hit, which is an aim or a lane problem rather than a plan problem.");

            Assert.Greater(tally.LataRestores, 0,
                $"{mode}: the taya never righted the lata after a knockdown.");

            // ---- THE DEFENDING LOOP ----------------------------------------------------
            Assert.Greater(tally.Tags, 0,
                $"{mode}: no attacker was tagged in four whole rounds, so the taya never " +
                "closed on a vulnerable retriever.");

            // ---- THE TOURNAMENT RULES --------------------------------------------------
            // ⚠️⚠️ THE CEILING IS GENEROUS ON PURPOSE AND THE REASONING MATTERS MORE THAN THE
            // NUMBER. Both penalties are charged ONCE PER SECOND for as long as the violation
            // lasts, so the count is a duration and not an event count. A match where the
            // retrieval loop is genuinely dead posts one every second for every attacker for
            // the whole match: the measurement that started this work read 679 in Classic and
            // 686 in Hero Strike, against roughly 1,080 attacker-seconds available. A match
            // where the loop works but the bots occasionally take the fine to avoid a tag reads
            // between 0 and about 80, and the same build measured 0 in Classic and 75 in Hero
            // Strike in the same run. Hero kits stun far more often, which is most of that gap.
            //
            // So 200 is not "roughly right": it is above anything a working loop has produced
            // and far below anything a dead one can produce. A tighter number would be a
            // measurement of the personality roll rather than of the game.
            Assert.Less(tally.CampPenalties, 200,
                $"{mode}: {tally.CampPenalties} can-camping penalties across the match. The " +
                "defender is parking inside the ring instead of guarding the approach.");

            Assert.Less(tally.IdlePenalties, 200,
                $"{mode}: {tally.IdlePenalties} unretrieved-slipper penalties. Attackers are " +
                "not reaching their tsinelas at all, which is the retrieval loop being dead " +
                "rather than the bots being cautious.");

            // ---- THE HERO KITS ---------------------------------------------------------
            if (mode != GameMode.HeroStrike) yield break;

            Assert.IsTrue(tally.SawAnyKit,
                "Hero Strike seated nobody with a hero kit, so the mode did not install.");

            Assert.Greater(tally.SkillUses, 0,
                "Hero Strike: not one skill was used in four rounds, so every bot ignored its " +
                "own kit.");

            // ⚠️ A CEILING AS WELL AS A FLOOR, BECAUSE SPAM IS THE OTHER FAILURE. A bot that
            // holds E down uses a skill every time the cooldown lifts, which over a six minute
            // match is several hundred activations and is exactly what "uses skills without
            // spamming uselessly" rules out.
            Assert.Less(tally.SkillUses, 260,
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
                _lastUltimate.TryGetValue(kit, out float previous);
                if (previous > HeroKit.UltimateMax * 0.5f && kit.UltimateCharge <= 0.01f)
                    UltimateUses++;
                _lastUltimate[kit] = kit.UltimateCharge;
            }

            public string Describe()
                => $"lata knocks {LataKnocks}  tags {Tags}  sabotages {Sabotages}\n" +
                   $"throws {Throws}  retrievals {Retrievals}  lata restores {LataRestores}\n" +
                   $"camp penalties {CampPenalties}  idle penalties {IdlePenalties}\n" +
                   $"skill uses {SkillUses}  ultimate uses {UltimateUses}  kits seen {SawAnyKit}";
        }
    }
}
