using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

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

        [UnityTest, Timeout(90000)]
        public IEnumerator HiddenCanDoesNotBlockTheTutorialThrowInput()
        {
            yield return LoadTraining();
            var route = Object.FindFirstObjectByType<GuidedTraining>();
            Assert.IsNotNull(route);
            yield return Route(route, GuidedTraining.Lesson.Throw);
            var who = Field<CharacterMotor>(route, "_local");
            var can = Field<Lata>(route, "_lata");
            var carrier = who.GetComponent<Carrier>();
            Assert.IsFalse(can.gameObject.activeInHierarchy);
            Assert.IsFalse(who.IsDefender);
            Assert.IsTrue(who.HoldingSlipper);
            who.Intent.Parked = false;
            carrier.enabled = false;
            var step = typeof(Carrier).GetMethod("StepAttacker", BindingFlags.Instance | BindingFlags.NonPublic);
            who.Intent.Set(Verb.SpecialAbility, true);
            step.Invoke(carrier, new object[] { 0f });
            step.Invoke(carrier, new object[] { Balance.ChargeFullTime });
            Assert.IsTrue(carrier.IsCharging,
                $"Tutorial Throw must accept the actual input; upright={can.IsUpright}, hidden protection={can.ProtectionLeft}, canThrow={GameServices.Round.CanThrow(who)}");
            who.Intent.Set(Verb.SpecialAbility, false);
            step.Invoke(carrier, new object[] { .02f });
            Assert.IsFalse(who.HoldingSlipper, "Releasing the actual charge must launch the slipper.");
            yield return null;
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator ListeningHostKeepsCanDownAndRestoreThrowRestrictions()
        {
            yield return LoadTraining();
            var route = Object.FindFirstObjectByType<GuidedTraining>();
            yield return Route(route, GuidedTraining.Lesson.Throw);
            var who = Field<CharacterMotor>(route, "_local");
            var can = Field<Lata>(route, "_lata");
            Assert.IsTrue(GameServices.Round.CanThrow(who), "The offline hidden target must allow practice.");
            var net = Net.NetSession.Ensure();
            try
            {
                var start = net.StartHostAsync(18719);
                float deadline = Time.realtimeSinceStartup + 20;
                while (!start.IsCompleted && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsTrue(start.IsCompleted, "Listening host start timed out.");
                Assert.IsTrue(start.Result, net.Status);
                Assert.IsTrue(net.IsNetworked && net.IsHost);
                GameLaunch.GuidedTutorial = true; // Even a stale route flag cannot bypass host rules.
                Assert.IsFalse(GameServices.Round.CanThrow(who), "Hosted play must not inherit the hidden tutorial exception.");
                can.gameObject.SetActive(true);
                deadline = Time.realtimeSinceStartup + 6;
                while (can.IsProtected && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsFalse(can.IsProtected);
                Assert.IsTrue(GameServices.Round.CanThrow(who), "Host permits an ordinary upright, unprotected can.");
                can.HostKnockDown(2);
                Assert.IsFalse(can.IsUpright);
                Assert.IsFalse(GameServices.Round.CanThrow(who));
                can.HostRestore();
                Assert.IsTrue(can.IsProtected);
                Assert.IsFalse(GameServices.Round.CanThrow(who));
            }
            finally { net.Stop(); }
            yield return null;
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator AboveCanFlightMissesWhileBodyHeightFlightKnocksDown()
        {
            yield return LoadTraining();
            var route = Object.FindFirstObjectByType<GuidedTraining>();
            yield return Route(route, GuidedTraining.Lesson.ThrowAndRetrieve);
            route.enabled = false;
            var can = Field<Lata>(route, "_lata");
            var who = Field<CharacterMotor>(route, "_local");
            Assert.IsTrue(can.gameObject.activeInHierarchy);
            float deadline = Time.realtimeSinceStartup + 6;
            while (can.IsProtected && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsFalse(can.IsProtected);
            var shoe = who.GetComponent<Carrier>().Held;
            Assert.IsNotNull(shoe);
            shoe.enabled = false;
            var flight = typeof(Slipper).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            var mark = can.transform.position;
            shoe.HostThrow(who, mark + new Vector3(0, 3, -1.2f), Vector3.forward * 12);
            for (int i = 0; i < 12; i++) flight.Invoke(shoe, null);
            Assert.IsTrue(can.IsUpright, "An actual flying slipper metres above the can must miss.");
            shoe.HostThrow(who, mark + new Vector3(0, .2f, -1.2f), new Vector3(0, 1, 12));
            for (int i = 0; i < 12 && can.IsUpright; i++) flight.Invoke(shoe, null);
            Assert.IsFalse(can.IsUpright, "Preserve an actual flight through the can body.");
            yield return null;
        }

        [Test]
        public void EveryCanSkinHasFiniteMeasuredVerticalContactAfterReplacement()
        {
            var book = RosterBook.Load();
            Assert.IsNotNull(book);
            var root = new GameObject("CanHeightBoundary");
            try
            {
                var can = root.AddComponent<Lata>();
                for (int skin = 0; skin < Roster.Cans.Count; skin++)
                {
                    var old = root.transform.Find("Visual");
                    if (old != null) Object.DestroyImmediate(old.gameObject);
                    var art = book.CanArt(skin);
                    Assert.IsNotNull(art?.Model);
                    var model = Object.Instantiate(art.Model, root.transform); model.name = "Visual";
                    can.SkinIndex = skin;
                    float bottom = float.PositiveInfinity, top = float.NegativeInfinity;
                    foreach (var mesh in model.GetComponentsInChildren<MeshFilter>())
                    foreach (var vertex in mesh.sharedMesh.vertices)
                    {
                        float y = mesh.transform.TransformPoint(vertex).y;
                        bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y);
                    }
                    Assert.Greater(top, bottom);
                    Assert.IsTrue(can.Connects(Vector3.up * ((bottom + top) * .5f)), Roster.Cans[skin].Id);
                    Assert.IsTrue(can.Connects(Vector3.up * (top + Balance.SlipperHitRadius - .001f)));
                    Assert.IsFalse(can.Connects(Vector3.up * (top + Balance.SlipperHitRadius + .001f)));
                    Assert.IsFalse(can.Connects(Vector3.up * (bottom - Balance.SlipperHitRadius - .001f)));
                    Assert.IsFalse(can.Connects(new Vector3(can.HitWindow + .001f, (bottom + top) * .5f, 0)));
                }
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static float Flat(Vector3 a, Vector3 b)
            => Vector3.Distance(new Vector3(a.x, 0.0f, a.z), new Vector3(b.x, 0.0f, b.z));

        [UnityTest, Timeout(90000)]
        public IEnumerator ObjectLessonsDoNotCoverTheSlipperOrCanWithTutorialStars()
        {
            int mip = QualitySettings.globalTextureMipmapLimit;
            QualitySettings.globalTextureMipmapLimit = 2;
            try
            {
                yield return LoadTraining();
                var route = Object.FindFirstObjectByType<GuidedTraining>();
                var marker = Field<Component>(route, "_marker");
                var hud = Object.FindFirstObjectByType<GuidedTrainingHud>();
                foreach (var lesson in new[] { GuidedTraining.Lesson.Retrieve, GuidedTraining.Lesson.ThrowAndRetrieve, GuidedTraining.Lesson.DefenderReset })
                {
                    EnterLesson(route, GuidedTraining.Lesson.Shove);
                    Assert.IsTrue(marker.gameObject.activeSelf, "Keep the existing person-target cue.");
                    EnterLesson(route, lesson); yield return null;
                    Assert.AreEqual(lesson, route.CurrentLesson);
                    Assert.IsFalse(marker.gameObject.activeSelf, "A previous lesson must not leave a star/glow over the can or slipper.");
                    Assert.IsFalse(marker.GetComponentsInChildren<Renderer>(true).Any(r => r.enabled && r.gameObject.activeInHierarchy));
                    if (lesson != GuidedTraining.Lesson.ThrowAndRetrieve)
                        yield return TumpUiCapture.Capture("Feedback-tutorial-no-object-star-" + lesson, hud.GetComponent<Canvas>(), 960, 540, false, true);
                }
                EnterLesson(route, GuidedTraining.Lesson.Shove); yield return null;
                Assert.IsTrue(marker.gameObject.activeSelf, "Later person-target lessons still work after object lessons.");
            }
            finally { QualitySettings.globalTextureMipmapLimit = mip; }
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator CompletedTrainingAttackersReallyThrowAndProgressChromeRetires()
        {
            int mip = QualitySettings.globalTextureMipmapLimit; QualitySettings.globalTextureMipmapLimit = 2;
            try
            {
                yield return LoadTraining();
                var route = Object.FindFirstObjectByType<GuidedTraining>();
                var local = Field<CharacterMotor>(route, "_local");
                EnterLesson(route, GuidedTraining.Lesson.Emote);
                var friends = GameServices.Round.Players.Where(p => p != null && p != local && !p.IsDefender && p.gameObject.activeSelf).ToArray();
                var places = friends.Select(p => p.transform.position).ToArray();
                Assert.AreEqual(2, friends.Length);
                EnterLesson(route, GuidedTraining.Lesson.Complete);
                var hud = Object.FindFirstObjectByType<GuidedTrainingHud>();
                var progress = hud.transform.Find("ObjectiveCard/ProgressBack");
                var rail = hud.transform.Find("ObjectiveCard/RouteRail");
                Assert.IsFalse(progress.gameObject.activeSelf); Assert.IsFalse(rail.gameObject.activeSelf);
                for (int i = 0; i < friends.Length; i++)
                {
                    Assert.IsTrue(friends[i].GetComponent<AIController>().enabled);
                    Assert.IsTrue(friends[i].HoldingSlipper);
                    Assert.Less(Vector3.Distance(places[i], friends[i].transform.position), .001f);
                }
                var defender = GameServices.Round.Players.First(p => p != null && p.IsDefender);
                Assert.IsFalse(defender.GetComponent<AIController>().enabled, "Keep the friendly reset-only defender.");
                var thrown = new System.Collections.Generic.HashSet<int>();
                var shoes = Object.FindObjectsByType<Slipper>();
                float until = Time.time + 25;
                while (thrown.Count < 2 && Time.time < until)
                {
                    foreach (var shoe in shoes)
                        if (shoe.State == SlipperState.InFlight && friends.Any(p => p.PlayerSlot == shoe.ThrowerSlot)) thrown.Add(shoe.ThrowerSlot);
                    yield return null;
                }
                Assert.AreEqual(2, thrown.Count, "Both restored AIs must perform a real throw, not merely walk in place.");
                Assert.IsTrue(local.CanAct()); Assert.IsFalse(local.Intent.Parked);
                yield return TumpUiCapture.Capture("Feedback-tutorial-complete-active-attackers", hud.GetComponent<Canvas>(), 960, 540, false, true);
                EnterLesson(route, GuidedTraining.Lesson.Ready); yield return null;
                Assert.IsTrue(progress.gameObject.activeSelf); Assert.IsTrue(rail.gameObject.activeSelf);
                Assert.IsTrue(friends.All(p => !p.GetComponent<AIController>().enabled), "Reentering lessons must not retain free-play AI.");
            }
            finally { QualitySettings.globalTextureMipmapLimit = mip; }
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator CirclingAttackerKeepsSmoothDirectionAndReleasesItsTrainingSpeed()
        {
            int mip = QualitySettings.globalTextureMipmapLimit; QualitySettings.globalTextureMipmapLimit = 2;
            try
            {
                yield return LoadTraining();
                var route = Object.FindFirstObjectByType<GuidedTraining>();
                EnterLesson(route, GuidedTraining.Lesson.Punch);
                var dummy = Field<CharacterMotor>(route, "_dummy");
                var centre = GameServices.Round.Lata.transform.position;
                yield return new WaitForSeconds(1);
                Vector3 before = dummy.transform.position, previous = Vector3.zero;
                float maxTurn = 0, travelled = 0; int reversals = 0, samples = 0;
                var trace = new StringBuilder("time,x,z,step,turn\n"); float began = Time.time;
                while (Time.time < began + 4)
                {
                    yield return new WaitForFixedUpdate();
                    Vector3 at = dummy.transform.position, step = at - before; step.y = 0;
                    float turn = previous.sqrMagnitude > .000001f && step.sqrMagnitude > .000001f ? Vector3.Angle(previous, step) : 0;
                    if (turn > 90) reversals++; maxTurn = Mathf.Max(maxTurn, turn); travelled += step.magnitude;
                    trace.AppendLine(System.FormattableString.Invariant($"{Time.time-began:F4},{at.x:F5},{at.z:F5},{step.magnitude:F5},{turn:F3}"));
                    if (step.sqrMagnitude > .000001f) previous = step;
                    before = at; samples++;
                    Assert.That(Flat(at, centre), Is.InRange(2.6f, 3.4f), "The training target must keep its authored circle.");
                }
                Directory.CreateDirectory("Logs/training-orbit"); File.WriteAllText("Logs/training-orbit/motion.csv", trace.ToString());
                Debug.Log($"[TrainingOrbit] samples={samples} reversals={reversals} maxTurn={maxTurn:F2} meanSpeed={travelled/(Time.time-began):F3}");
                Assert.AreEqual(0, reversals, "A slow orbit must not reverse the body's movement every few frames.");
                Assert.Less(maxTurn, 30, "The movement heading must turn smoothly around the circle.");
                Assert.That(travelled / (Time.time-began), Is.InRange(1.1f, 1.7f), "Keep the original3m/.45rad training pace.");
                EnterLesson(route, GuidedTraining.Lesson.Shove);
                Assert.AreEqual(1, dummy.SpeedMultiplier, .001f, "The orbit's private slow must not leak to another lesson.");
                EnterLesson(route, GuidedTraining.Lesson.Punch); yield return null;
                Object.DestroyImmediate(route);
                Assert.AreEqual(1, dummy.SpeedMultiplier, .001f, "Destroying training must release its private slow.");
            }
            finally { QualitySettings.globalTextureMipmapLimit = mip; }
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator TutorialUltimateKeepsItsRenderedIntroductionWithoutTheAnnouncementBanner()
        {
            int mip = QualitySettings.globalTextureMipmapLimit; QualitySettings.globalTextureMipmapLimit = 2;
            var rules = TumbangPreso.UI.SceneFlow.SelectedRules;
            try
            {
                TumbangPreso.UI.SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
                yield return LoadTraining();
                var route = Object.FindFirstObjectByType<GuidedTraining>();
                EnterLesson(route, GuidedTraining.Lesson.Ultimate);
                var local = Field<CharacterMotor>(route, "_local");
                bool held = local.GetComponent<Carrier>().Held != null;
                TumbangPreso.Visual.UltimateIntroductionCache.WarmOne(local);
                float until = Time.realtimeSinceStartup + 15;
                while (TumbangPreso.Visual.UltimateIntroductionCache.Find(local, held) == null && Time.realtimeSinceStartup < until) yield return null;
                Assert.IsNotNull(TumbangPreso.Visual.UltimateIntroductionCache.Find(local, held));
                var commits = new[] { new UltimateCommit(local.PlayerSlot, 1, local.transform.position, local.transform.forward, Vector3.forward, 0).WithIdentity(local.AbilitySystem.Kit) };
                foreach (bool training in new[] { true, false })
                {
                    GameLaunch.GuidedTutorial = training;
                    using (var view = new TumbangPreso.CameraSystem.UltimatePhaseView(route.transform, commits, 3))
                    {
                        view.Draw(.3f);
                        var canvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include).First(c => c.name == "SharedUltimateCanvas");
                        var title = canvas.transform.Find("UltimateIdentity");
                        Assert.AreEqual(!training, title.gameObject.activeSelf);
                        Assert.IsNotNull(canvas.transform.Find("UltimateScene").GetComponent<RawImage>().texture, "Retain the actual introduction rendering.");
                        if (training) yield return TumpUiCapture.Capture("Feedback-tutorial-ultimate-no-banner", canvas, 960, 540, false);
                    }
                    yield return null;
                }
            }
            finally { GameLaunch.GuidedTutorial = true; QualitySettings.globalTextureMipmapLimit = mip; TumbangPreso.UI.SceneFlow.SetSelectedRules(rules); }
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator AbilityReadingAndSuccessfulCastsRespectTheirDelays()
        {
            var oldRules = TumbangPreso.UI.SceneFlow.SelectedRules;
            int mip = QualitySettings.globalTextureMipmapLimit; QualitySettings.globalTextureMipmapLimit = 2;
            var settings = InputSystem.settings; var background = settings.backgroundBehavior;
            var editor = settings.editorInputBehaviorInPlayMode;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard = InputSystem.AddDevice<Keyboard>(); InputSystem.EnableDevice(keyboard);
            try
            {
                TumbangPreso.UI.SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
                yield return LoadTraining();
                var route = Object.FindFirstObjectByType<GuidedTraining>();
                var local = Field<CharacterMotor>(route, "_local");
                var abilities = local.AbilitySystem; Assert.IsNotNull(abilities.Kit);
                EnterLesson(route, GuidedTraining.Lesson.AbilityInfo);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Tab)); InputSystem.Update();
                yield return new WaitForSecondsRealtime(2.1f);
                Assert.AreEqual(GuidedTraining.Lesson.AbilityInfo, route.CurrentLesson);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                yield return new WaitForSecondsRealtime(.2f);
                Assert.AreEqual(0f, Field<float>(route, "_metric"), .01f, "Releasing the description key resets the continuous read interval.");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Tab)); InputSystem.Update();
                float readAt = Time.unscaledTime;
                while (Time.unscaledTime < readAt + 2.7f) yield return null;
                Assert.IsTrue(Field<bool>(route, "_advancing"), "The authored2.5second read interval should now be complete.");
                float until = readAt + 4;
                while (route.CurrentLesson == GuidedTraining.Lesson.AbilityInfo && Time.unscaledTime < until) yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                Assert.AreEqual(GuidedTraining.Lesson.Skill1, route.CurrentLesson);
                foreach (var item in new[] {
                    (GuidedTraining.Lesson.Skill1, TumbangPreso.Abilities.HeroAbilitySystem.Slot.Skill1, Verb.Skill1, GuidedTraining.Lesson.Skill2),
                    (GuidedTraining.Lesson.Skill2, TumbangPreso.Abilities.HeroAbilitySystem.Slot.Skill2, Verb.Skill2, GuidedTraining.Lesson.Ultimate) })
                {
                    EnterLesson(route, item.Item1);
                    local.ApplyStagger(1f); local.Intent.Set(item.Item3, true); yield return null;
                    local.Intent.Set(item.Item3, false); yield return new WaitForSecondsRealtime(.6f);
                    Assert.AreEqual(item.Item1, route.CurrentLesson, "A refused cast must not finish the lesson.");
                    local.ClearStun();
                    var dummy = Field<CharacterMotor>(route, "_dummy");
                    local.Intent.AimPoint = dummy.transform.position + Vector3.up * .8f;
                    Vector3 facing = dummy.transform.position - local.transform.position; facing.y = 0;
                    local.transform.forward = facing.normalized;
                    var ability = item.Item2 == TumbangPreso.Abilities.HeroAbilitySystem.Slot.Skill1 ? abilities.Kit.Skill1 : abilities.Kit.Skill2;
                    Assert.IsNotNull(ability, abilities.HeroId + " " + item.Item1 + " has no implemented ability.");
                    Assert.IsTrue(local.CanAct(), "Prepared student cannot act.");
                    Assert.IsFalse(local.Intent.Parked || local.Intent.Locked(item.Item3), "Prepared cast input is gated.");
                    Assert.IsFalse(PresentationClock.BlocksInput, "Presentation is still holding input.");
                    // Let both press and release cross real Update frames, including hold-to-aim slots.
                    local.Intent.Set(item.Item3, true); yield return new WaitForSecondsRealtime(.15f);
                    local.Intent.Set(item.Item3, false); yield return new WaitForSecondsRealtime(.1f);
                    Assert.AreEqual(TumbangPreso.Abilities.HeroKit.CastOutcome.Cast, abilities.LastAnswer(item.Item2),
                        abilities.HeroId + " " + item.Item1 + " real cast must be accepted before this timing check can qualify.");
                    float accepted = Field<float>(route, "_castAcceptedAt"); Assert.GreaterOrEqual(accepted, 0);
                    while (Time.unscaledTime < accepted + .8f) yield return null;
                    Assert.IsFalse(Field<bool>(route, "_advancing"), "A successful cast must retain its full1second observation beat.");
                    Assert.AreEqual(item.Item1, route.CurrentLesson);
                    while (Time.unscaledTime < accepted + 1.1f) yield return null;
                    Assert.IsTrue(Field<bool>(route, "_advancing"), "The observation beat should now show completion.");
                    until = Time.unscaledTime + 4;
                    while (route.CurrentLesson == item.Item1 && Time.unscaledTime < until) yield return null;
                    Assert.AreEqual(item.Item4, route.CurrentLesson);
                }
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard); settings.backgroundBehavior = background;
                settings.editorInputBehaviorInPlayMode = editor; QualitySettings.globalTextureMipmapLimit = mip;
                TumbangPreso.UI.SceneFlow.SetSelectedRules(oldRules);
            }
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator ThreeRealJumpsFillTheLessonAndOneTimeCompletionTurnsGreen()
        {
            yield return LoadTraining();
            var route = Object.FindFirstObjectByType<GuidedTraining>();
            yield return Route(route, GuidedTraining.Lesson.Jump);
            var local = Field<CharacterMotor>(route, "_local");
            var hud = Field<GuidedTrainingHud>(route, "_hud");
            var fill = hud.GetComponentsInChildren<Image>().First(i => i.name == "ProgressFill");
            for (int jump = 1; jump <= 3; jump++)
            {
                float until = Time.realtimeSinceStartup + 5;
                while (!local.IsGrounded && Time.realtimeSinceStartup < until) yield return null;
                Assert.IsTrue(local.IsGrounded);
                local.Intent.Set(Verb.Jump, true);
                until = Time.realtimeSinceStartup + 2;
                while (Field<float>(route, "_metric") < jump && Time.realtimeSinceStartup < until) yield return null;
                local.Intent.Set(Verb.Jump, false);
                Assert.AreEqual(jump, Field<float>(route, "_metric"));
                Assert.AreEqual(jump / 3f, fill.rectTransform.anchorMax.x, .001f);
                Assert.AreEqual(GuidedTraining.Lesson.Jump, route.CurrentLesson);
                if (jump < 3)
                {
                    yield return new WaitForSecondsRealtime(.15f);
                    Assert.AreEqual(jump, Field<float>(route, "_metric"), "One airborne episode was counted twice.");
                }
            }
            Assert.AreEqual(1, fill.rectTransform.anchorMax.x);
            Assert.Greater(fill.color.g, fill.color.r, "Completed progress must turn green.");
            yield return TumpUiCapture.Capture("Feedback0930-tutorial-three-jumps", hud.GetComponent<Canvas>(), 960, 540, false, true);
            float deadline = Time.realtimeSinceStartup + 2;
            while (route.CurrentLesson == GuidedTraining.Lesson.Jump && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.AreEqual(GuidedTraining.Lesson.Throw, route.CurrentLesson);
            route.SkipFromUi();
            Assert.AreEqual(1, fill.rectTransform.anchorMax.x, .001f, "A one-time lesson completion left its progress empty.");
            Assert.Greater(fill.color.g, fill.color.r);
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator TutorialKeepsTabReadableAndWaitsThroughTheUltimateIntroduction()
        {
            var oldRules = TumbangPreso.UI.SceneFlow.SelectedRules;
            var inputSettings = InputSystem.settings;
            var background = inputSettings.backgroundBehavior;
            var editorInput = inputSettings.editorInputBehaviorInPlayMode;
            inputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            inputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.EnableDevice(keyboard);
            try
            {
                TumbangPreso.UI.SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
                yield return LoadTraining();
                var route = Object.FindFirstObjectByType<GuidedTraining>();
                yield return Route(route, GuidedTraining.Lesson.Ultimate);
                var hud = Field<GuidedTrainingHud>(route, "_hud");
                var local = Field<CharacterMotor>(route, "_local");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Tab));
                InputSystem.Update();
                Assert.IsTrue(keyboard.tabKey.isPressed, "The fixture's Tab event was not received.");
                Assert.IsTrue(Field<InputAction>(route, "_abilityInfo").IsPressed(), "The fixture's actual info binding was not received.");
                yield return null; yield return null;
                var body = hud.GetComponentsInChildren<Text>(true).First(t => t.name == "LessonBody");
                Assert.IsFalse(body.gameObject.activeSelf, "The held kit reference did not compact the tutorial.");
                var reference = GameObject.Find("HeldPowerReference").GetComponent<RectTransform>();
                Assert.IsTrue(reference.gameObject.activeInHierarchy);
                var card = (RectTransform)hud.transform.Find("ObjectiveCard");
                var readout = Object.FindFirstObjectByType<TumbangPreso.UI.TumpMatchReadout>();
                foreach (var size in new[] { new Vector2Int(960, 540), new Vector2Int(1600, 680) })
                    yield return TumpUiCapture.Capture("Feedback0930-tutorial-tab-" + size.x + "x" + size.y,
                        hud.GetComponent<Canvas>(), size.x, size.y, false, true, underlays: new[] { readout.Canvas }, inspectViewport: () =>
                        {
                            Canvas.ForceUpdateCanvases();
                            var a = new Vector3[4]; var b = new Vector3[4];
                            card.GetWorldCorners(a); reference.GetWorldCorners(b);
                            var camera = hud.GetComponent<Canvas>().worldCamera;
                            float bottom = camera.WorldToScreenPoint(a[0]).y;
                            float top = camera.WorldToScreenPoint(b[1]).y;
                            Assert.GreaterOrEqual(bottom, top + 2, "Tutorial and Tab reference overlap.");
                        });
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null; yield return null;
                Assert.IsTrue(body.gameObject.activeSelf, "The tutorial description did not return on release.");
                local.Intent.Set(Verb.Ultimate, true);
                yield return null;
                local.Intent.Set(Verb.Ultimate, false);
                float deadline = Time.realtimeSinceStartup + 8;
                while ((SharedUltimatePhase.Instance == null || !SharedUltimatePhase.Instance.Active) && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsTrue(SharedUltimatePhase.Instance != null && SharedUltimatePhase.Instance.Active, "The real input never accepted an ultimate.");
                deadline = Time.realtimeSinceStartup + 40;
                while (SharedUltimatePhase.Instance.Active && Time.realtimeSinceStartup < deadline)
                {
                    Assert.AreEqual(GuidedTraining.Lesson.Ultimate, route.CurrentLesson);
                    yield return null;
                }
                Assert.IsFalse(SharedUltimatePhase.Instance.Active);
                yield return new WaitForSecondsRealtime(2.2f);
                Assert.AreEqual(GuidedTraining.Lesson.Ultimate, route.CurrentLesson, "The post-introduction delay was skipped.");
                deadline = Time.realtimeSinceStartup + 4;
                while (route.CurrentLesson == GuidedTraining.Lesson.Ultimate && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.AreEqual(GuidedTraining.Lesson.Emote, route.CurrentLesson);
                Assert.AreEqual(20, GuidedTraining.LessonCount);
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                inputSettings.backgroundBehavior = background;
                inputSettings.editorInputBehaviorInPlayMode = editorInput;
                TumbangPreso.UI.SceneFlow.SetSelectedRules(oldRules);
            }
        }

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
                intent.Set(Verb.Lunge, true);
                yield return null;
                yield return null;
                intent.Set(Verb.Lunge, false);
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
