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
    /// The three things 🧑 reported about the guided route on 2026-09-02, each measured rather
    /// than argued about. `docs/TODO.md` § 124.3 and § 124.4.
    ///
    /// ⚠️⚠️ `TutorialDefenderProbe` EXISTS AND WAS GREEN THROUGHOUT, WHICH IS WHY THIS FILE IS A
    /// SECOND ONE RATHER THAN THREE MORE CASES IN THAT ONE. That probe asks the question the
    /// 2026-08-29 report raised — is the student the taya, and can the channel finish — and the
    /// answer is yes on both. **The faults this time were one gate further in and invisible to
    /// every one of its assertions**: the can was somewhere else, the can was on its side, and a
    /// press that touched nobody still ticked the lesson off. Keeping them apart keeps each
    /// file's failure message pointing at one cause.
    ///
    /// ⚠️ THE ROUTE IS WALKED, NEVER JUMPED INTO. `ApplyVerbLock` is cumulative over every lesson
    /// up to the current one, so entering a late lesson directly arrives with a verb set built
    /// from `Look` alone and refuses the verb under test for a reason the real route never has.
    /// `TutorialDefenderProbe` records the same rule.
    /// </summary>
    public class TutorialLessonHonestyProbe
    {
        [TearDown]
        public void TearDown() => Quiesce();

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

        private static IEnumerator Route(GuidedTraining route, GuidedTraining.Lesson upTo)
        {
            for (var step = GuidedTraining.Lesson.Look; step <= upTo; step++)
            {
                EnterLesson(route, step);
                yield return null;
            }
        }

        private static IEnumerator LoadTraining()
        {
            Quiesce();

            GameLaunch.GuidedTutorial = true;
            GameLaunch.AllBots = false;
            GameLaunch.Spectator = false;

            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            for (int i = 0; i < 60; i++) yield return null;

            // ⚠️ THE READER IS SWITCHED OFF, LIKE `TutorialDefenderProbe`. It writes the whole
            // intent from real devices every Update, so a driven press would be cleared before
            // the verb read it and this would measure the absence of a keyboard.
            foreach (var reader in Object.FindObjectsByType<PlayerInputReader>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (reader != null) reader.enabled = false;
        }

        private static float Flat(Vector3 a, Vector3 b)
            => Vector3.Distance(new Vector3(a.x, 0.0f, a.z), new Vector3(b.x, 0.0f, b.z));

        // -------------------------------------------------------------------

        /// <summary>
        /// § 124.4. A shove that touches nobody must not tick the lesson off.
        ///
        /// ⚠️⚠️ THE OLD TICK READ `ShoveCooldownLeft > _baselineCooldown + 0.05f`, WHICH IS THE
        /// VERB HAVING FIRED. `CombatVerbs.StepShove` spends the cooldown before it searches the
        /// cone, so a press into empty air completed the lesson. 🧑: *"sometimes some tasks get
        /// marked even if u dont rlly do them like pushing ppl (as long as u click push it gets
        /// marked as done)"*.
        ///
        /// ⚠️ THE DUMMY IS MOVED RATHER THAN DELETED. `Balance.ShoveRange` is 1.6 m and the
        /// lesson stands it at 1.40; 9 m away it is still a live, taggable body in
        /// `round.Players`, so this measures the CONE and not the absence of a target.
        /// </summary>
        [UnityTest]
        public IEnumerator AShoveThatHitsNobodyDoesNotCompleteTheLesson()
        {
            yield return LoadTraining();

            var route = Object.FindFirstObjectByType<GuidedTraining>();
            Assert.IsNotNull(route, "the guided route did not install; this is an ordinary match.");

            yield return Route(route, GuidedTraining.Lesson.Shove);
            Assert.AreEqual(GuidedTraining.Lesson.Shove, route.CurrentLesson);

            var local = Field<CharacterMotor>(route, "_local");
            var dummy = Field<CharacterMotor>(route, "_dummy");
            Assert.IsNotNull(local);
            Assert.IsNotNull(dummy);

            var verbs = local.GetComponent<CombatVerbs>();
            Assert.IsNotNull(verbs);

            dummy.Teleport(local.transform.position + local.transform.forward * 9.0f);
            yield return null;

            float landedBefore = verbs.LastShoveLandedAt;
            var intent = local.Intent;

            // Six real press edges: down for two frames, up for two, which is what the reader
            // produces and what `JustPressed` needs.
            for (int press = 0; press < 6; press++)
            {
                intent.Set(Verb.Grab, true);
                yield return null;
                yield return null;
                intent.Set(Verb.Grab, false);
                yield return null;
                yield return null;
            }

            // The lesson advances on a 0.70 s beat, so give it more than that to be wrong in.
            for (float t = 0.0f; t < 1.2f; t += Time.deltaTime) yield return null;

            Assert.AreEqual(landedBefore, verbs.LastShoveLandedAt, 0.0001f,
                "a shove landed on somebody 9 m away, so this probe is measuring the wrong thing.");

            Assert.AreEqual(GuidedTraining.Lesson.Shove, route.CurrentLesson,
                "SHOVE was ticked off by six presses that touched nobody. The lesson must read "
                + "CombatVerbs.LastShoveLandedAt, which is written inside ApplyShoveTo, and not a "
                + "cooldown that is spent before the cone is searched. docs/TODO.md 124.4.");
        }

        /// <summary>
        /// § 124.4 and § 124.3, in one measurement.
        ///
        /// ⚠️⚠️ A REAL TAG COMPLETES `Lesson.Punch`, AND `RoundDirector.ResolveTag` REFUSES
        /// OUTRIGHT WHILE THE LATA IS DOWN (`if (Lata == null || !Lata.IsUpright) return;`). So
        /// this case proves BOTH halves at once: that the lesson reads the tag event, and that
        /// entering PUNCH stands the can up. The second is what was broken — the lesson inherited
        /// whatever `DefenderReset` had left, and `DefenderReset` ends with the can knocked over.
        ///
        /// ⚠️ THE CAN IS PUT DOWN ON PURPOSE FIRST, which is the state a skipped or abandoned
        /// reset leaves and is one keypress away in the real route (`N` completes any lesson).
        /// </summary>
        [UnityTest]
        public IEnumerator ATagCompletesPunchEvenWhenTheResetWasSkipped()
        {
            yield return LoadTraining();

            var route = Object.FindFirstObjectByType<GuidedTraining>();
            Assert.IsNotNull(route);

            var lata = Field<Lata>(route, "_lata");
            Assert.IsNotNull(lata);

            // Walk as far as the ultimate, then knock the can over and jump straight to PUNCH.
            // That is exactly what holding N through ROLE SWAP: DEFENDER does.
            yield return Route(route, GuidedTraining.Lesson.Ultimate);

            lata.HostRestore();
            for (float t = 0.0f; t < Balance.ThrowRestoreCooldown + 0.2f; t += Time.deltaTime)
                yield return null;
            lata.HostKnockDown(-1);
            yield return null;
            Assert.IsFalse(lata.IsUpright, "the can refused to go over, so the skip is not simulated.");

            EnterLesson(route, GuidedTraining.Lesson.Punch);
            yield return null;

            Assert.IsTrue(lata.IsUpright,
                "PUNCH began with the can on its side. RoundDirector.ResolveTag opens with "
                + "'if (Lata == null || !Lata.IsUpright) return;', so every punch and every lunge "
                + "for the rest of the route is refused in silence. docs/TODO.md 124.3.");

            var local = Field<CharacterMotor>(route, "_local");
            var dummy = Field<CharacterMotor>(route, "_dummy");
            Assert.IsNotNull(local);
            Assert.IsNotNull(dummy);
            Assert.IsTrue(local.IsDefender, "PUNCH must make the student the taya.");

            // ⚠️ THROUGH THE RULES, NOT AROUND THEM. `ResolveTag` is the one function that decides
            // a tag happened and the one that raises `Tagged`; calling it is what the punch itself
            // does two lines after it finds a victim in the cone.
            GameServices.Round.ResolveTag(local, dummy);

            for (float t = 0.0f; t < 1.4f; t += Time.deltaTime) yield return null;

            Assert.AreNotEqual(GuidedTraining.Lesson.Punch, route.CurrentLesson,
                "a tag the match resolved did not complete PUNCH. The lesson reads "
                + "RoundDirector.Tagged now; if that subscription is gone the lesson can only be "
                + "left with the skip key. docs/TODO.md 124.4.");
        }

        /// <summary>
        /// § 124.3 fault (1). The taya lesson has to put the student beside the can it is asking
        /// them to stand up.
        ///
        /// ⚠️⚠️ `Lata.HostRestore` MOVES THE CAN, and `BecomeDefender` measures the student's
        /// landing spot against `_lata.transform.position`. The restore used to run one frame
        /// LATER, inside `ArmDefenderReset`, so on any route where an earlier lesson had knocked
        /// the can off its mark the student was set down beside the patch of road it had just
        /// left. `Carrier.StepDefender` needs `Balance.InteractionRadius`, 1.6 m, and holding the
        /// key outside it does nothing and says nothing. 🧑: *"u can[t] raise can"*.
        ///
        /// ⚠️ THE CAN IS DISPLACED BY 6 m, which is well inside the arena and nearly four times
        /// the interaction radius, so the assertion cannot pass by luck.
        /// </summary>
        [UnityTest]
        public IEnumerator TheTayaLessonStandsYouWithinReachOfADisplacedCan()
        {
            yield return LoadTraining();

            var route = Object.FindFirstObjectByType<GuidedTraining>();
            Assert.IsNotNull(route);

            var local = Field<CharacterMotor>(route, "_local");
            var lata = Field<Lata>(route, "_lata");
            Assert.IsNotNull(local);
            Assert.IsNotNull(lata);

            yield return Route(route, GuidedTraining.Lesson.Ultimate);

            // Roll the can away from its mark, the way a knockdown in the THROW or PEKTUS lesson
            // does, and leave it there.
            Vector3 mark = lata.transform.position;
            lata.transform.position = mark + new Vector3(6.0f, 0.0f, 0.0f);
            yield return null;

            EnterLesson(route, GuidedTraining.Lesson.DefenderReset);
            yield return null;

            float reach = Flat(local.transform.position, lata.transform.position);

            var lines = new StringBuilder();
            lines.AppendLine("ROLE SWAP: DEFENDER, with the can moved 6 m off its mark first.");
            lines.AppendLine($"  can displaced to      = {mark + new Vector3(6.0f, 0.0f, 0.0f)}");
            lines.AppendLine($"  can ended at          = {lata.transform.position}");
            lines.AppendLine($"  student ended at      = {local.transform.position}");
            lines.AppendLine($"  flat distance to can  = {reach:0.000} m");
            lines.AppendLine($"  InteractionRadius     = {Balance.InteractionRadius}");
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/tutorial-reach.txt", lines.ToString());
            Debug.Log("[TutorialReach]\n" + lines);

            Assert.LessOrEqual(reach, Balance.InteractionRadius,
                "the taya lesson put the student out of reach of the can it is asking them to "
                + "stand up, so holding the pickup key does nothing and nothing says why. The "
                + "restore has to happen BEFORE the role is applied, because it teleports the "
                + "can to its mark. docs/TODO.md 124.3.\n" + lines);
        }
    }
}
