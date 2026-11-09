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
        public IEnumerator TitleSubmitIsReleasedBeforeHomeCanReceiveIt() => SubmitRoute(false);

        [UnityTest, Timeout(90000)]
        public IEnumerator TitlePadSubmitIsReleasedBeforeHomeCanReceiveIt() => SubmitRoute(true);

        [UnityTest, Timeout(90000)]
        public IEnumerator ShopPopupTakesPadFocusAndReturnsItToTheOpener()
        {
            SceneFlow.Networked = false;
            GameLaunch.Reset();
            yield return SceneManager.LoadSceneAsync(SceneFlow.MatchSetup);
            float until = Time.realtimeSinceStartup + 45;
            while ((TumpHub.Current == null || HubLoading.Visible) && Time.realtimeSinceStartup < until) yield return null;
            var hub = TumpHub.Current;
            Assert.IsNotNull(hub);
            Assert.IsInstanceOf<HubHome>(hub.Top);
            var events = UnityEngine.EventSystems.EventSystem.current;
            var module = events.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            var opener = GameObject.Find("ShopButton");
            Assert.IsNotNull(opener);
            var settings = InputSystem.settings;
            var background = settings.backgroundBehavior;
            var editorInput = settings.editorInputBehaviorInPlayMode;
            var pad = InputSystem.AddDevice<Gamepad>();
            try
            {
                settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                InputSystem.EnableDevice(pad);
                events.SetSelectedGameObject(opener);
                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.South));
                InputSystem.Update();
                TumbangPreso.InputLayer.LastInputDevice.Sample();
                Assert.IsTrue(module.submit.action.WasPerformedThisFrame());
                module.Process();
                Assert.IsInstanceOf<HubShopPopup>(hub.Top);
                Assert.AreSame(hub.Top.FirstFocus.gameObject, events.currentSelectedGameObject);
                InputSystem.QueueStateEvent(pad, new GamepadState());
                InputSystem.Update();
                yield return new WaitForSecondsRealtime(.4f);
                yield return TumpUiCapture.Capture("TJ-shop-focus-refined-1280x720", hub.Canvas, 1280, 720,
                    checkPalette: false, checkActionBounds: true);
                hub.Back();
                Assert.IsInstanceOf<HubHome>(hub.Top);
                Assert.AreSame(opener, events.currentSelectedGameObject);
                hub.Push<HubHero>(hero => hero.ShopMode = true);
                until = Time.realtimeSinceStartup + 35;
                while (!(hub.Top is HubHero) && Time.realtimeSinceStartup < until) yield return null;
                Assert.IsInstanceOf<HubHero>(hub.Top);
                yield return TumpUiCapture.Capture("TJ-hero-contrast-refined-1600x680", hub.Canvas, 1600, 680,
                    checkPalette: false, checkActionBounds: true);
            }
            finally
            {
                InputSystem.RemoveDevice(pad);
                settings.backgroundBehavior = background;
                settings.editorInputBehaviorInPlayMode = editorInput;
            }
        }

        private IEnumerator SubmitRoute(bool usePad)
        {
            var settings = InputSystem.settings;
            var background = settings.backgroundBehavior;
            var editorInput = settings.editorInputBehaviorInPlayMode;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard = InputSystem.AddDevice<Keyboard>(); InputSystem.EnableDevice(keyboard);
            var pad = InputSystem.AddDevice<Gamepad>(); InputSystem.EnableDevice(pad);
            void Submit(bool held)
            {
                if (usePad) InputSystem.QueueStateEvent(pad, held ? new GamepadState().WithButton(GamepadButton.South) : new GamepadState());
                else InputSystem.QueueStateEvent(keyboard, held ? new KeyboardState(Key.Enter) : new KeyboardState());
                InputSystem.Update();
                TumbangPreso.InputLayer.LastInputDevice.Sample();
            }
            int choice = HubHome.Choice;
            try
            {
                SceneFlow.Networked = false; GameLaunch.Reset();
                yield return SceneManager.LoadSceneAsync(SceneFlow.MainMenu);
                yield return new WaitForSecondsRealtime(.4f);
                Assert.IsFalse(Net.NetIdentity.IsOnline, "No live matchmaking identity in this check.");
                // The title is the automatic Home-loading surface and has nothing to press.
                Submit(true);
                yield return new WaitForSecondsRealtime(.4f);
                Assert.AreEqual(SceneFlow.MainMenu, SceneManager.GetActiveScene().name,
                    "A held Submit must not skip the title's loading view.");
                Submit(false);
                float until = Time.realtimeSinceStartup + 60;
                while ((SceneManager.GetActiveScene().name != SceneFlow.MatchSetup || TumpHub.Current == null || HubLoading.Visible)
                    && Time.realtimeSinceStartup < until) yield return null;
                var hub = TumpHub.Current; Assert.IsNotNull(hub); Assert.IsInstanceOf<HubHome>(hub.Top);
                yield return new WaitForSecondsRealtime(.3f);
                Assert.IsFalse(HubQueueWatch.QueueRoom, "Opening the game is not consent to queue.");
                Assert.IsFalse(Net.Matchmaker.Current.IsQueueing);
                HubHome.Choice = 1;
                var play = GameObject.Find("PlayButton"); Assert.IsNotNull(play);
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(play);
                Submit(true);
                var module = UnityEngine.EventSystems.EventSystem.current.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                Assert.IsNotNull(module);
                Assert.IsTrue(module.submit.action.WasPerformedThisFrame(), "Fresh synthetic Submit reaches the real UI action.");
                // Process in the injected input frame, before the automatic next update clears it.
                module.Process();
                yield return null; yield return null;
                Assert.IsTrue(HubQueueWatch.QueueRoom, "A fresh deliberate Submit still activates PLAY.");
                Submit(false);
            }
            finally
            {
                TumpHub.Current?.Host.CancelQueue(); Net.NetSession.Instance?.Stop(); HubQueueWatch.End();
                SceneFlow.Networked = false; HubHome.Choice = choice;
                InputSystem.RemoveDevice(pad);
                InputSystem.RemoveDevice(keyboard);
                settings.backgroundBehavior = background;
                settings.editorInputBehaviorInPlayMode = editorInput;
            }
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator TitleArrivesHomeByItselfAndBackKeepsItsBackground()
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
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Period)); InputSystem.Update();
                Assert.IsTrue(TumbangPreso.InputLayer.MenuNav.KeyboardAnyPressed);
                yield return new WaitForSecondsRealtime(.4f);
                Assert.AreEqual(SceneFlow.MainMenu, SceneManager.GetActiveScene().name, "A key must not skip the title's loading view.");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
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
