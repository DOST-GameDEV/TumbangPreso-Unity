using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// The taya half of the guided route, measured instead of argued about.
    ///
    /// ⚠️⚠️ THIS IS THE TEST `docs/TODO.md` § 76 ASKED FOR AND CLOSED ITSELF WITHOUT. That entry
    /// traced eight gates by reading, found all eight passing, and closed on 🧑's word that the
    /// build was fixed — while saying outright that *"a report closing a bug nobody deliberately
    /// fixed is a bug that can come back"*. It came back on 2026-08-29: *"can hold x to reset
    /// here"*, *"u also cant tag"*, and the diagnosis he offered with it, *"i think bug in
    /// tutorial is it still treats u as attacker even tho its asking u to test defender shit"*.
    ///
    /// ⚠️ SO THE REPORT IS THE POINT OF THE FILE, exactly like `TrainingStreetProbe`. It prints
    /// every gate on the reset path and every gate on the tag path with its live value, so the
    /// next person starts from a number rather than from a screenshot. The assertions are the
    /// two things he actually reported.
    ///
    /// ⚠️ THE INTENT IS DRIVEN, NOT THE KEYBOARD. § 76 asked for this specifically: `InputIntent`
    /// is the seam the bots already use, it needs no input system, and it is the same surface
    /// `PlayerInputReader` writes. The reader is switched off first so it cannot overwrite the
    /// hold on its own `Update`.
    /// </summary>
    public class TutorialDefenderProbe
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

        private const string OutPath = "Logs/tutorial-defender.txt";

        [TearDown]
        public void TearDown() => Quiesce();

        /// <summary>End any match still running, in the DIRECTORS. See `TrainingStreetProbe`.</summary>
        private static void Quiesce()
        {
            foreach (var route in Object.FindObjectsByType<GuidedTraining>(FindObjectsSortMode.None))
                if (route != null) Object.DestroyImmediate(route);

            GameLaunch.GuidedTutorial = false;
            GameServices.Round?.EndRound();
            GameServices.Match?.ResetForNewMatch();
            GameServices.Round?.ResetForNewMatch();
        }

        private static T Field<T>(object target, string name)
        {
            var f = target.GetType().GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(f, $"GuidedTraining has no field '{name}'; this probe is out of date.");
            return (T)f.GetValue(target);
        }

        private static void EnterLesson(GuidedTraining route, GuidedTraining.Lesson lesson)
        {
            var m = route.GetType().GetMethod("EnterLesson",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(m, "GuidedTraining.EnterLesson is gone; this probe is out of date.");
            m.Invoke(route, new object[] { lesson });
        }

        [UnityTest]
        public IEnumerator TheTayaLessonMakesYouTheTayaAndTheCanCanBeReset()
        {
            Quiesce();

            GameLaunch.GuidedTutorial = true;
            GameLaunch.AllBots = false;
            GameLaunch.Spectator = false;

            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            for (int i = 0; i < 60; i++) yield return null;

            var route = Object.FindFirstObjectByType<GuidedTraining>();
            Assert.IsNotNull(route, "the guided route did not install; this is an ordinary match.");

            var local = Field<CharacterMotor>(route, "_local");
            var lata = Field<Lata>(route, "_lata");
            Assert.IsNotNull(local);
            Assert.IsNotNull(lata);

            var lines = new StringBuilder();
            lines.AppendLine("THE GUIDED TAYA LESSON, gate by gate.");
            lines.AppendLine();
            lines.AppendLine($"local seat {local.PlayerSlot}, defender={local.IsDefender} " +
                             $"(before the lesson: it should be an ATTACKER here)");

            // ⚠️ WALKED TO, NOT JUMPED TO. `ApplyVerbLock` is cumulative over every lesson up to
            // the current one, so entering DefenderReset directly would arrive with a verb set
            // built from Look alone and refuse Grab for a reason the real route never has.
            for (var step = GuidedTraining.Lesson.Look;
                 step <= GuidedTraining.Lesson.DefenderReset;
                 step++)
            {
                EnterLesson(route, step);
                yield return null;
            }

            Assert.AreEqual(GuidedTraining.Lesson.DefenderReset, route.CurrentLesson);

            // `ArmDefenderReset` restores, waits `ThrowRestoreCooldown + 0.08`, then knocks the
            // can over. Give it that plus a margin, and let the roll settle.
            float wait = Balance.ThrowRestoreCooldown + 1.4f;
            for (float t = 0.0f; t < wait; t += Time.deltaTime) yield return null;

            bool armed = Field<bool>(route, "_defenderResetArmed");

            var intent = local.Intent;
            float flat = Vector3.Distance(
                new Vector3(local.transform.position.x, 0.0f, local.transform.position.z),
                new Vector3(lata.transform.position.x, 0.0f, lata.transform.position.z));

            lines.AppendLine();
            lines.AppendLine("on the frame the lesson is armed:");
            lines.AppendLine($"  local.IsDefender      = {local.IsDefender}");
            lines.AppendLine($"  local.RoundActive     = {local.RoundActive}");
            lines.AppendLine($"  local.CanAct()        = {local.CanAct()}");
            lines.AppendLine($"  intent.Parked         = {intent.Parked}");
            lines.AppendLine($"  Grab locked           = {intent.Locked(Verb.Grab)}");
            lines.AppendLine($"  SpecialAbility locked = {intent.Locked(Verb.SpecialAbility)}");
            lines.AppendLine($"  Lunge locked          = {intent.Locked(Verb.Lunge)}");
            lines.AppendLine($"  lata.IsUpright        = {lata.IsUpright}");
            lines.AppendLine($"  lata.IsProtected      = {lata.IsProtected}");
            lines.AppendLine($"  _defenderResetArmed   = {armed}");
            lines.AppendLine($"  flat distance to can  = {flat:0.000} m " +
                             $"(InteractionRadius {Balance.InteractionRadius})");
            lines.AppendLine($"  round.Players count   = {GameServices.Round.Players.Count}");
            lines.AppendLine($"  match.DefenderSlot    = {GameServices.Match.DefenderSlot}");
            lines.AppendLine($"  match.RoundNumber     = {GameServices.Match.RoundNumber}");

            // ---- hold the pickup key, the way a player does ---------------------------
            //
            // ⚠️ THE READER IS SWITCHED OFF FIRST. It writes the whole intent from real devices
            // every Update, so a driven hold would be cleared before `Carrier.Update` read it and
            // the probe would measure the absence of a keyboard rather than the reset path.
            foreach (var reader in Object.FindObjectsByType<PlayerInputReader>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (reader != null) reader.enabled = false;

            var carrier = local.GetComponent<Carrier>();
            Assert.IsNotNull(carrier);

            float hold = lata.ResetChannelTime + 1.0f;
            float bestChannel = 0.0f;

            for (float t = 0.0f; t < hold; t += Time.deltaTime)
            {
                intent.Set(Verb.Grab, true);
                yield return null;
                bestChannel = Mathf.Max(bestChannel, carrier.ChannelRatio);
                if (lata.IsUpright) break;
            }

            lines.AppendLine();
            lines.AppendLine($"after holding Grab for up to {hold:0.00} s:");
            lines.AppendLine($"  best ChannelRatio     = {bestChannel:0.000}");
            lines.AppendLine($"  lata.IsUpright        = {lata.IsUpright}");
            lines.AppendLine($"  lesson now            = {route.CurrentLesson}");

            Directory.CreateDirectory("Logs");
            File.WriteAllText(OutPath, lines.ToString());
            Debug.Log($"[TutorialDefender] wrote {OutPath}\n{lines}");

            // ---- the two things he reported -------------------------------------------

            Assert.IsTrue(local.IsDefender,
                "ROLE SWAP: DEFENDER did not make the local seat the taya, which is 🧑's own "
                + "diagnosis: \"it still treats u as attacker even tho its asking u to test "
                + "defender shit\". Everything the lesson teaches is gated on this one flag: "
                + "Carrier.Update routes to StepAttacker, and CombatVerbs refuses the tag.\n"
                + lines);

            Assert.IsTrue(armed,
                "the lesson never armed, so it could not have completed however the can ended "
                + "up. ArmDefenderReset restores the can, waits out the restore protection and "
                + "knocks it down; a refused knockdown leaves this false.\n" + lines);

            Assert.IsTrue(lata.IsUpright,
                $"holding Grab for {hold:0.00} s beside the can did not stand it up, which is "
                + "\"can hold x to reset here\". The channel reached "
                + $"{bestChannel:0.000} of 1.\n" + lines);
        }

        /// <summary>
        /// The role follows the LESSON, whichever way the route is walked.
        ///
        /// ⚠️⚠️ THIS IS THE CASE THE FIRST PROBE COULD NOT SEE, AND 🧑 NAMED IT: *"i think its bcz
        /// the role doesnt change in between those phases"*, and then *"at throwing stage they
        /// should be allowed to be attacker and do shit, but the moment theyre asked to raise can
        /// or tag, they should be given defender role"*.
        ///
        /// `DefenderReset` was the only lesson that made you the taya. `Punch` and `Lunge` follow
        /// it, set no role, and `CombatVerbs` refuses both on `!_motor.IsDefender` — so **any
        /// route that reached them without passing through `DefenderReset` asked for two verbs the
        /// player could not perform, and refused every press in silence.** That route is one
        /// keypress away: `N` completes the current lesson.
        ///
        /// ⚠️ IT WALKS THE ROUTE TWICE, STRAIGHT THROUGH AND WITH THE RESET SKIPPED, because the
        /// straight walk is the one that already passed. The skip is the player's path.
        /// </summary>
        [UnityTest]
        public IEnumerator TheRoleFollowsTheLessonEvenWhenTheResetIsSkipped()
        {
            Quiesce();

            GameLaunch.GuidedTutorial = true;
            GameLaunch.AllBots = false;
            GameLaunch.Spectator = false;

            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            for (int i = 0; i < 60; i++) yield return null;

            var route = Object.FindFirstObjectByType<GuidedTraining>();
            Assert.IsNotNull(route);

            var local = Field<CharacterMotor>(route, "_local");
            Assert.IsNotNull(local);

            var report = new StringBuilder();
            report.AppendLine("THE ROLE EACH LESSON PUTS YOU IN:");
            report.AppendLine();

            // ⚠️ THE WHOLE ROUTE, IN ORDER, WITH NOTHING PERFORMED. Every lesson is entered and
            // immediately left, which is exactly what holding N does.
            var wrong = new System.Collections.Generic.List<string>();

            for (var step = GuidedTraining.Lesson.Look;
                 step < GuidedTraining.Lesson.Complete;
                 step++)
            {
                EnterLesson(route, step);
                yield return null;

                bool taya = step == GuidedTraining.Lesson.DefenderReset
                         || step == GuidedTraining.Lesson.Punch
                         || step == GuidedTraining.Lesson.Lunge;

                report.AppendLine($"  {step,-14} wants {(taya ? "TAYA    " : "attacker")} "
                                  + $"got {(local.IsDefender ? "TAYA" : "attacker")}");

                if (local.IsDefender != taya)
                    wrong.Add($"{step} asks for {(taya ? "the taya's" : "an attacker's")} verbs "
                              + $"while the player is {(local.IsDefender ? "the taya" : "an attacker")}");

                // ⚠️ AND THE STUDENT CAN ACT, EXCEPT IN THE ONE LESSON WHOSE SUBJECT IS NOT
                // BEING ABLE TO. A lesson that begins with the player held by the PREVIOUS one is
                // an objective with the means to meet it taken away — that is what
                // `ClearTheLastLessonsMess` is for. `TripRecovery` opens by calling
                // `_local.ApplyTrip()` on purpose: being on the road IS the exercise, and the
                // whole lesson is mashing out of it.
                //
                // ⚠️ THE EXEMPTION IS BY LESSON, NOT BY "IS SOMETHING ALREADY WRONG". Whitelisting
                // the state rather than the step is how a real stun leaking in from step 12 would
                // be waved through as expected.
                if (step != GuidedTraining.Lesson.TripRecovery && !local.CanAct())
                    wrong.Add($"{step} begins with the player unable to act");
            }

            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/tutorial-roles.txt", report.ToString());
            Debug.Log("[TutorialRoles]\n" + report);

            Assert.IsEmpty(wrong,
                "the role must follow the lesson, not whichever lesson happened to run before:\n  "
                + string.Join("\n  ", wrong) + "\n" + report);
        }
    }
}
