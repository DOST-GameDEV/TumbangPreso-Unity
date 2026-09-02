using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// A bot does not throw through the person standing in front of it.
    ///
    /// ⚠️⚠️ NOTHING IN THE PORT ASKED THE QUESTION AT ALL. `ai_controller.gd::_lane_blocked()`
    /// walks the arc the slipper will actually fly and asks the same thing the flight itself
    /// asks, sample by sample; this project released the throw regardless. A bot with a body
    /// directly between it and the can hit that body every single time, and it reads as an AI
    /// that cannot aim rather than as one with no idea anybody is there.
    ///
    /// ⚠️ THE TEST DRIVES THE PRIVATE METHOD BY REFLECTION ON PURPOSE. Making it public to test
    /// it would invite a caller outside the AI, and the whole reason this heuristic lives in
    /// `AIController` rather than in the balance package is that it is a bot's guess about the
    /// world, not a rule that decides an outcome.
    /// </summary>
    public class AiLaneTests
    {
        [UnityTest]
        public IEnumerator ABodyInTheLaneBlocksTheThrowAndAClearLaneDoesNot()
        {
            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            for (int i = 0; i < 20; i++) yield return null;

            var round = GameServices.Round;
            Assert.IsNotNull(round, "The arena registered no round.");

            AIController bot = null;

            foreach (var a in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None))
            {
                bot = a;
                break;
            }

            Assert.IsNotNull(bot, "No bot seat to ask.");

            var lane = typeof(AIController).GetMethod(
                "LaneBlocked", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(lane, "AIController has no LaneBlocked; the lane check is missing.");

            // ⚠️ THE THROW IS AIMED AT THE MARK, WHICH IS WHERE THE CAN STANDS. Aiming anywhere
            // else would be testing a shot the bot never takes.
            Vector3 mark = round.Lata != null ? round.Lata.transform.position : Vector3.zero;

            var me = bot.GetComponent<CharacterMotor>();
            CharacterMotor blocker = null;

            foreach (var who in round.Players)
            {
                if (who == null || who == me) continue;
                blocker = who;
                break;
            }

            Assert.IsNotNull(blocker, "The arena has only one seat.");

            // ⚠️⚠️ `Teleport`, NEVER A TRANSFORM WRITE, AND SEVERAL FIXED STEPS AFTERWARDS. A
            // CharacterController fights a direct position write, and the motor also pins every
            // seat to its spawn for the first few physics steps. The first version of this test
            // set three positions by hand, watched all three snap back, and asserted against
            // where the bodies were NOT.
            foreach (var who in round.Players)
            {
                if (who == null || who == me || who == blocker) continue;
                who.Teleport(mark + new Vector3(30.0f, 0.0f, 0.0f));
            }

            blocker.Teleport(mark + new Vector3(40.0f, 0.0f, 0.0f));
            me.Teleport(mark + new Vector3(0.0f, 0.0f, Balance.ConfinementRadius + 1.5f));

            for (int i = 0; i < 8; i++) yield return new WaitForFixedUpdate();

            // Wherever the bodies actually settled is what the lane is measured against.
            Vector3 origin = me.transform.position;

            bool clear = (bool)lane.Invoke(bot, new object[] { origin, mark, 1.0f });
            Assert.IsFalse(clear, "A completely clear lane was reported as blocked, so a bot " +
                                  "in this position would never release a throw.");

            // Now put somebody squarely on the line, a quarter of the way along it where the
            // arc is still low enough to meet a standing body.
            blocker.Teleport(Vector3.Lerp(origin, mark, 0.25f));

            for (int i = 0; i < 8; i++) yield return new WaitForFixedUpdate();

            Vector3 at = blocker.transform.position;

            // ⚠️ RE-AIMED THROUGH WHERE THE BODY ENDED UP. The arena is a real street and a
            // teleport can settle a metre off; asserting against the requested point rather
            // than the reached one is how the first attempt failed for the wrong reason.
            Vector3 through = mark + (at - mark).normalized * ((at - mark).magnitude * 4.0f);
            through.y = origin.y;

            bool blocked = (bool)lane.Invoke(bot, new object[] { through, mark, 1.0f });
            Assert.IsTrue(blocked,
                $"A body at {at} between {through} and {mark} did not block the lane, so the " +
                "bot throws straight through it.");
        }
    }
}
