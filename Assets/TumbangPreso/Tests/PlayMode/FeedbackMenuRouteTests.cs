using System.Collections;
using NUnit.Framework;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class FeedbackMenuRouteTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [UnityTest, Timeout(90000)]
        public IEnumerator OrdinaryKeyboardKeyEntersHomeAndBackKeepsItsBackground()
        {
            var settings = InputSystem.settings;
            var background = settings.backgroundBehavior;
            var editorInput = settings.editorInputBehaviorInPlayMode;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard = InputSystem.AddDevice<Keyboard>(); InputSystem.EnableDevice(keyboard);
            try
            {
                SceneFlow.Networked = false;
                yield return SceneManager.LoadSceneAsync(SceneFlow.MainMenu);
                yield return new WaitForSecondsRealtime(.4f);
                var prompt = Object.FindFirstObjectByType<OwnerMenuPrompt>(); Assert.IsNotNull(prompt);
                Assert.IsTrue(prompt.Press.isActiveAndEnabled && prompt.Press.IsInteractable());
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Period)); InputSystem.Update();
                Assert.IsTrue(TumbangPreso.InputLayer.MenuNav.KeyboardAnyPressed);
                prompt.SendMessage("Update");
                float until = Time.realtimeSinceStartup + 60;
                while ((SceneManager.GetActiveScene().name != SceneFlow.MatchSetup || TumpHub.Current == null || HubLoading.Visible)
                    && Time.realtimeSinceStartup < until) yield return null;
                Assert.AreEqual(SceneFlow.MatchSetup, SceneManager.GetActiveScene().name);
                var hub = TumpHub.Current; Assert.IsNotNull(hub); Assert.IsInstanceOf<HubHome>(hub.Top);
                Assert.IsTrue(hub.ShowingHome);
                hub.Back(); yield return null;
                Assert.IsInstanceOf<HubMenu>(hub.Top);
                Assert.AreEqual(SceneFlow.MatchSetup, SceneManager.GetActiveScene().name, "Back from Home must not reopen the title.");
                Assert.IsTrue(hub.ShowingHome, "The hamburger must retain the Home picture underneath it.");
                hub.Back(); yield return null;
                Assert.IsInstanceOf<HubHome>(hub.Top); Assert.IsTrue(hub.ShowingHome);
                Assert.AreEqual(SceneFlow.MatchSetup, SceneManager.GetActiveScene().name);
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                settings.backgroundBehavior = background;
                settings.editorInputBehaviorInPlayMode = editorInput;
            }
        }
    }
}
