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
    public class AiDiagnosticProbe
    {
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

        private IEnumerator Diagnose(GameMode mode, float seconds)
        {
            var previousMode = UI.SceneFlow.SelectedMode;
            UI.SceneFlow.SelectedMode = mode;

            Hitstop.End();
            Time.timeScale = 1.0f;

            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            while (load != null && !load.isDone) yield return null;
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

            Directory.CreateDirectory("Logs");
            File.WriteAllText($"Logs/ai-diagnostic-{mode}.txt", log.ToString());
            Debug.Log(log.ToString());

            UI.SceneFlow.SelectedMode = previousMode;

            Assert.Greater(bots.Length, 0, "The arena seated no bots.");

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
