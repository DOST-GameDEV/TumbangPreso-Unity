using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using TumbangPreso.UI;

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

        [UnityTest, Timeout(90000)]
        public IEnumerator ARealBotSprintsAndSlidesToScoopItsShoe()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita, skipReadyArrival: true);
            var round = GameServices.Round;
            var can = round.Lata;
            var bot = round.PlayerAt(2);
            var brain = bot.GetComponent<AIController>();
            var carrier = bot.GetComponent<Carrier>();
            var shoe = carrier.Held;
            Assert.IsNotNull(shoe);

            float floor = can.Mark.y;
            var runUp = new Vector3(0, floor + .04f, -8.4f);
            var pickup = new Vector3(0, floor + shoe.RestHeight, -2.5f);
            bot.Teleport(runUp);
            bot.transform.rotation = Quaternion.identity;
            round.PlayerAt(GameServices.Match.DefenderSlot).Teleport(new Vector3(2.8f, floor + .04f, -2.5f));
            shoe.HostDisarm();
            shoe.transform.position = pickup;
            Physics.SyncTransforms();
            yield return new WaitForSeconds(can.ProtectionLeft + .05f);
            can.HostKnockDown(-1);
            Hitstop.End();

            var camera = new GameObject("Slide observer").AddComponent<Camera>();
            camera.CopyFrom(Camera.main);
            camera.enabled = false;
            camera.fieldOfView = 52f;
            var random = Random.state;
            Random.InitState(8607);
            try
            {
                yield return SlideFrame(camera, bot, "bot-runup");
                bot.Intent.Parked = false;
                brain.enabled = true;
                bool sprinted = false, slid = false, scooped = false, captured = false;
                float slideSpeed = 0f, slideAt = 0f;
                float deadline = Time.time + 7f;
                while (Time.time < deadline && (!scooped || bot.ReworkSliding))
                {
                    yield return null;
                    sprinted |= bot.Stamina.IsSprinting;
                    if (bot.ReworkSliding)
                    {
                        if (!slid) slideAt = Time.time;
                        slid = true;
                        slideSpeed = Mathf.Max(slideSpeed, bot.ReworkFlatSpeed);
                        if (!scooped && carrier.Held == shoe)
                        {
                            scooped = true;
                            Assert.IsTrue(bot.Intent.Pressed(Verb.Grab), "The bot must press Grab to retrieve while sliding.");
                        }
                        if (scooped && !captured && Time.time - slideAt >= .19f)
                        {
                            var animator = bot.GetComponentInChildren<Visual.CharacterAnimator>();
                            var weight = typeof(Visual.CharacterAnimator).GetField("_slideWeight", BindingFlags.Instance | BindingFlags.NonPublic);
                            if (animator != null && (float)weight.GetValue(animator) > .65f)
                            {
                                yield return SlideFrame(camera, bot, "bot-scoop");
                                captured = true;
                            }
                        }
                    }
                }
                Assert.IsTrue(sprinted && slid && scooped, $"Expected the live bot to sprint, slide and scoop. Plan {brain.Plan}, held={carrier.Held == shoe}, speed={bot.ReworkFlatSpeed:F2}, position={bot.transform.position}.");
                Assert.IsTrue(captured, "The same slide state must drive the third-person pose.");
                Assert.Greater(slideSpeed, Stamina.MovementSpeed(false, true));
                Assert.IsFalse(bot.ReworkSliding);
                yield return new WaitForSeconds(.25f);
                yield return SlideFrame(camera, bot, "bot-recovery");
            }
            finally
            {
                Random.state = random;
                Object.Destroy(camera.gameObject);
            }
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator BotsChooseLanesForEachCourtAndTheUnguardedTayaPatrols()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita, skipReadyArrival: true);
            var round = GameServices.Round;
            var can = round.Lata;
            var defender = round.PlayerAt(GameServices.Match.DefenderSlot);
            var attacker = round.PlayerAt(2);
            float floor = can.Mark.y;
            attacker.Teleport(new Vector3(AIController.PlayableMaxX - 1f, floor + .04f, 0));
            var brain = attacker.GetComponent<AIController>();
            var spot = typeof(AIController).GetMethod("ThrowSpot", BindingFlags.Instance | BindingFlags.NonPublic);
            Vector3 lane = (Vector3)spot.Invoke(brain, null);
            Assert.IsFalse(Confinement.IsInsideBox(lane.x, lane.z), "A selected throw spot must be outside the chalk.");
            Assert.Greater(Mathf.Abs(lane.z), Mathf.Abs(lane.x),
                $"The tight street should favour an open end. Chose {lane}, walls {AIController.PlayableNearestX:F2}/{AIController.PlayableNearestZ:F2}.");

            foreach (var who in round.Players)
            {
                if (who == defender) continue;
                var shoe = who.GetComponent<Carrier>().Held;
                if (shoe == null) continue;
                shoe.HostDisarm();
                shoe.transform.position = new Vector3(who.PlayerSlot, floor + shoe.RestHeight, -9f);
            }
            defender.Teleport(can.Mark + Vector3.right * (Balance.TayaCampClearRadius + .7f));
            defender.Intent.Parked = false;
            defender.GetComponent<AIController>().enabled = true;
            Vector3 previous = defender.transform.position;
            float travelled = 0;
            float deadline = Time.time + 4.5f;
            while (Time.time < deadline)
            {
                yield return new WaitForFixedUpdate();
                Vector3 current = defender.transform.position;
                travelled += Vector3.Distance(previous, current);
                previous = current;
            }
            Assert.Greater(travelled, 4f, "A defender with no armed threat should change posts.");
            Vector3 gap = defender.transform.position - can.transform.position;
            gap.y = 0f;
            Assert.Greater(gap.magnitude, Balance.TayaCampRadius, "The patrol must clear the can's camping penalty.");

            yield return PlayModeWorld.Reset();
            yield return MapRetrievalProbe.Load(SceneFlow.Arena, skipReadyArrival: true);
            var arena = GameServices.Round;
            float arenaFloor = arena.Lata.Mark.y;
            var arenaAttacker = arena.PlayerAt(2);
            arenaAttacker.Teleport(new Vector3(Confinement.Radius + 1f, arenaFloor + .04f, 0));
            arena.PlayerAt(GameServices.Match.DefenderSlot).Teleport(new Vector3(-2.5f, arenaFloor + .04f, 0));
            Vector3 openSide = (Vector3)spot.Invoke(arenaAttacker.GetComponent<AIController>(), null);
            Assert.IsFalse(Confinement.IsInsideBox(openSide.x, openSide.z));
            Assert.Greater(openSide.x, Mathf.Abs(openSide.z),
                $"Arena has room to use the nearby side. Chose {openSide}.");
        }

        private static IEnumerator SlideFrame(Camera camera, CharacterMotor bot, string name)
        {
            camera.transform.position = bot.transform.position + new Vector3(-2.8f, 1.25f, 1.1f);
            camera.transform.LookAt(bot.transform.position + Vector3.up * .67f);
            float previous = Time.timeScale;
            Time.timeScale = 0f;
            try
            {
                yield return GameplayShots.Render(camera, name, false, "Logs/bot-slide-20261109", bot, 960, 540);
            }
            finally { Time.timeScale = previous; }
        }

        [UnityTest]
        public IEnumerator WaitingAttackerFindsANewLaneWhileTheCanIsDown()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita, skipReadyArrival: true);
            var round = GameServices.Round;
            var can = round.Lata;
            var attacker = round.PlayerAt(2);
            attacker.Teleport(can.Mark + new Vector3(0, .05f, -9f));
            yield return new WaitForSeconds(can.ProtectionLeft + .05f);
            can.HostKnockDown(-1);
            Hitstop.End();
            Assert.IsFalse(can.IsUpright);
            attacker.Intent.Parked = false;
            var brain = attacker.GetComponent<AIController>();
            brain.enabled = true;
            var arrived = typeof(AIController).GetField("_arrived", BindingFlags.Instance | BindingFlags.NonPublic);
            float deadline = Time.time + 12f;
            while (!(bool)arrived.GetValue(brain) && Time.time < deadline) yield return null;
            Assert.IsTrue((bool)arrived.GetValue(brain));
            Vector3 post = attacker.transform.position;
            deadline = Time.time + 5f;
            while (Vector3.Distance(attacker.transform.position, post) < 1.3f && Time.time < deadline)
                yield return null;
            Assert.GreaterOrEqual(Vector3.Distance(attacker.transform.position, post), 1.3f,
                "Waiting for a reset should create a new lane, beyond an idle shuffle.");
            Assert.IsFalse(can.IsUpright);
            Assert.IsTrue(attacker.HoldingSlipper);
        }

        [UnityTest]
        public IEnumerator TayaCanEmoteInASafeWindowAndBreaksOffForATaggableAttacker()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita, GameMode.HeroStrike, skipReadyArrival: true);
            var round = GameServices.Round;
            var can = round.Lata;
            var taya = round.PlayerAt(GameServices.Match.DefenderSlot);
            var opponent = round.Players.First(who => who != taya);
            var shoe = opponent.GetComponent<Carrier>().Held;
            Assert.IsNotNull(shoe);
            foreach (var player in round.Players)
                if (player != taya) player.Teleport(can.Mark + new Vector3(player.PlayerSlot, .06f, -9f));

            taya.Teleport(can.Mark + new Vector3(Balance.TayaCampClearRadius + .9f, .06f, 0));
            var art = RosterBook.Load().FindPersonArt("rafi");
            taya.GetComponent<Visual.CharacterVisual>().ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
            taya.Intent.Parked = false;
            for (int i = 0; i < 12; i++) yield return new WaitForFixedUpdate();
            Assert.IsTrue(taya.IsGrounded);
            var emotes = taya.GetComponent<Social.EmotePlayer>();
            Assert.IsTrue(emotes.HasEmoteClip("dance"));
            var brain = taya.GetComponent<AIController>();
            typeof(AIController).GetField("_wantedEmote", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(brain, "dance");
            brain.enabled = true;
            float deadline = Time.time + .4f;
            while (!emotes.IsEmoting && Time.time < deadline) yield return null;
            Assert.AreEqual("dance", emotes.Current, "An unthreatened defender outside the camping radius may emote.");
            opponent.Teleport(can.Mark + new Vector3(2, .06f, 1.1f));
            Assert.IsTrue(opponent.IsTaggable());
            deadline = Time.time + .35f;
            while (emotes.IsEmoting && Time.time < deadline) yield return null;
            Assert.IsFalse(emotes.IsEmoting, "A defender should abandon the emote when an attacker becomes taggable.");
        }

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
