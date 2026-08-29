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
            while (load != null && !load.isDone) yield return null;

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
    }
}
