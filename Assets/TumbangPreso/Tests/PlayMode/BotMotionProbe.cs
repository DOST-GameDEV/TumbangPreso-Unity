using System.Collections;
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
    /// Do the bots actually WALK?
    ///
    /// ⚠️⚠️ REPORTED AS *"ALL BOTS DONT MOVE"* AND NOTHING IN THE SUITE ASKED. `AiLaneTests`
    /// drives one private heuristic by reflection, `SteeringTests` checks the steering maths,
    /// and `MatchRunTests` runs a whole match without ever measuring a bot's DISPLACEMENT. Each
    /// of those can pass with four seats standing perfectly still, which is exactly what
    /// shipped.
    ///
    /// This writes the whole picture to `Logs/bot-motion.txt` — plan, intent, CanAct and metres
    /// travelled per seat per second — because "they do not move" has at least four different
    /// causes and a pass/fail alone does not separate them.
    /// </summary>
    public class BotMotionProbe
    {
        [UnityTest]
        public IEnumerator BotsWalkOnceTheRoundIsLive()
        {
            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            for (int i = 0; i < 20; i++) yield return null;

            var round = GameServices.Round;
            Assert.IsNotNull(round, "The arena registered no round.");

            round.BeginRound();

            for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();

            var bots = Object.FindObjectsByType<AIController>(FindObjectsSortMode.None);
            Assert.Greater(bots.Length, 0, "The arena seated no bots at all.");

            var start = new Vector3[bots.Length];
            var travelled = new float[bots.Length];
            var last = new Vector3[bots.Length];

            for (int i = 0; i < bots.Length; i++)
            {
                start[i] = bots[i].transform.position;
                last[i] = start[i];
            }

            var log = new StringBuilder();
            log.AppendLine("bot motion probe");
            log.AppendLine($"seats={bots.Length} roundActive={round.RoundActive}");

            // Six seconds of real match time, sampled every 30 physics steps.
            for (int step = 0; step < 300; step++)
            {
                yield return new WaitForFixedUpdate();

                for (int i = 0; i < bots.Length; i++)
                {
                    Vector3 now = bots[i].transform.position;
                    travelled[i] += Vector3.Distance(new Vector3(now.x, 0, now.z),
                                                     new Vector3(last[i].x, 0, last[i].z));
                    last[i] = now;
                }

                if (step % 30 != 29) continue;

                for (int i = 0; i < bots.Length; i++)
                {
                    var motor = bots[i].GetComponent<CharacterMotor>();
                    log.AppendLine(
                        $"t={(step + 1) * Time.fixedDeltaTime:F2} seat={motor.PlayerSlot} " +
                        $"plan={bots[i].Plan} canAct={motor.CanAct()} " +
                        $"roundActive={motor.RoundActive} stunned={motor.IsStunned} " +
                        $"def={motor.IsDefender} axis={motor.Intent.MoveAxis} " +
                        $"pos={motor.transform.position} travelled={travelled[i]:F2}");
                }
            }

            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/bot-motion.txt", log.ToString());

            for (int i = 0; i < bots.Length; i++)
            {
                Assert.Greater(travelled[i], 1.0f,
                    $"Seat {bots[i].GetComponent<CharacterMotor>().PlayerSlot} covered " +
                    $"{travelled[i]:F2} m in six seconds of a live round. See " +
                    "Logs/bot-motion.txt.");
            }
        }
    }
}
