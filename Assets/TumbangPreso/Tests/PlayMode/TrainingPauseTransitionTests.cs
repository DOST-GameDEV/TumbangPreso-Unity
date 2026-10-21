using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class TrainingPauseTransitionTests
    {
        private GuidedTraining _route;
        private PauseWatcher _watcher;
        private bool _tutorial, _networked;

        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _tutorial = GameLaunch.GuidedTutorial; _networked = SceneFlow.Networked;
            GameLaunch.GuidedTutorial = true; SceneFlow.Networked = false;
            GameServices.Ensure();
            SceneManager.SetActiveScene(SceneManager.CreateScene(SceneFlow.Eskinita));
            var owner = new GameObject("Training transition student");
            var actor = owner.AddComponent<CharacterMotor>(); actor.enabled = false;
            var can = new GameObject("Training transition can").AddComponent<Lata>();
            _watcher = owner.AddComponent<PauseWatcher>(); _watcher.enabled = false; _watcher.Local = actor;
            _route = owner.AddComponent<GuidedTraining>();
            // Supply the real lesson's dependencies without loading the populated map.
            Field("_local", actor); Field("_lata", can);
            Field("_seats", new[] { actor }); Field("_slippers", new Slipper[0]); Field("_ready", true);
            Assert.AreEqual(GuidedTraining.Lesson.Ready, _route.CurrentLesson);
        }

        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();
            GameLaunch.GuidedTutorial = _tutorial; SceneFlow.Networked = _networked;
            PresentationClock.RequestScale(1);
        }

        private void Field(string name, object value)
            => typeof(GuidedTraining).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_route, value);

        [UnityTest] public IEnumerator PauseDuringCompletionBeatKeepsTheLessonUntilResume()
        {
            _route.SkipFromUi();
            var pause = Panel.Open<PausePanel>(_watcher); pause.Local = _watcher.Local;
            yield return null;
            Assert.IsTrue(Panel.AnyOpen); Assert.Zero(Time.timeScale);
            yield return new WaitForSecondsRealtime(1);
            Assert.AreEqual(GuidedTraining.Lesson.Ready, _route.CurrentLesson,
                "The completion coroutine entered another lesson behind the paused menu.");
            pause.Close();
            yield return AwaitLook();
            Assert.IsFalse(Panel.AnyOpen);
        }

        [UnityTest] public IEnumerator UnpausedCompletionKeepsItsExistingBeatAndAdvances()
        {
            _route.SkipFromUi();
            Assert.AreEqual(GuidedTraining.Lesson.Ready, _route.CurrentLesson);
            yield return AwaitLook();
        }

        [UnityTest] public IEnumerator RepeatedSkipDuringTheBeatAdvancesExactlyOneLesson()
        {
            _route.SkipFromUi(); _route.SkipFromUi(); _route.SkipFromUi();
            yield return AwaitLook();
            yield return new WaitForSecondsRealtime(1);
            Assert.AreEqual(GuidedTraining.Lesson.Look, _route.CurrentLesson);
        }

        private IEnumerator AwaitLook()
        {
            float until = Time.realtimeSinceStartup + 3;
            while (_route.CurrentLesson == GuidedTraining.Lesson.Ready && Time.realtimeSinceStartup < until)
                yield return null;
            Assert.AreEqual(GuidedTraining.Lesson.Look, _route.CurrentLesson);
        }
    }
}
