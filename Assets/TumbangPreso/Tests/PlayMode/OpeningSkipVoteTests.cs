using System.Collections;
using System.IO;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CursorMode = TumbangPreso.UI.CursorMode;
using InputDevice = UnityEngine.InputSystem.InputDevice;

namespace TumbangPreso.PlayTests
{
    public sealed class OpeningSkipVoteTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [UnityTest, Timeout(180000)]
        public IEnumerator BanaueSkipUsesOneSmallControlAndRestoresTheHud()
        {
            bool cinematic = SettingsStore.Current.CinematicCameraMotion, reduced = SettingsStore.Current.ReducedUiMotion;
            bool captured = CursorMode.WantsCapture;
            var settings = InputSystem.settings;
            var background = settings.backgroundBehavior;
            var focus = settings.editorInputBehaviorInPlayMode;
            Mouse mouse = null;
            InputSystemUIInputModule module = null;
            UnityEngine.InputSystem.Utilities.ReadOnlyArray<InputDevice>? devices = null;
            try
            {
                settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                CursorMode.Capture();
                SettingsStore.Current.CinematicCameraMotion = true; SettingsStore.Current.ReducedUiMotion = false;
                SceneFlow.Networked = false; SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
                yield return SceneManager.LoadSceneAsync(SceneFlow.Payo);
                float until = Time.realtimeSinceStartup + 60;
                while (!OpeningSkipVote.Showing && Time.realtimeSinceStartup < until) yield return null;
                Assert.IsTrue(MatchArrivalPresentation.Rolling, "The opening reached the screen.");
                Assert.IsTrue(OpeningSkipVote.Showing, "The skip button is up while it plays.");
                Assert.IsFalse(CursorMode.WantsCapture, "The visible skip button left the pointer captured by gameplay.");
                yield return new WaitForSecondsRealtime(1.6f);

                var canvas = GameObject.Find("OpeningSkipCanvas").GetComponent<Canvas>();
                Assert.IsTrue(canvas.isActiveAndEnabled && canvas.enabled, "Its canvas survives the film hiding the HUD.");
                Assert.GreaterOrEqual(canvas.sortingOrder, 150, "It sits above the curtain.");
                var vote = Object.FindFirstObjectByType<OpeningSkipVote>();
                StringAssert.Contains(">>", vote.Words);
                var control = GameObject.Find("OpeningSkip").GetComponent<RectTransform>();
                Assert.LessOrEqual(control.rect.width, 112);
                Assert.IsNotNull(control.Find("Votes"));
                Assert.IsNotEmpty(control.Find("Binding").GetComponent<Text>().text, "The control must show its live binding.");
                var filmOverlay = GameObject.Find("Map film overlay");
                Assert.IsNotNull(filmOverlay);
                Assert.AreEqual(0, filmOverlay.transform.Find("Skip").GetComponent<CanvasGroup>().alpha,
                    "The old local skip ring must not duplicate the shared vote.");
                yield return GameplayShots.Render(Camera.main, "banaue-skip", true,
                    outDir: "Logs/multiplayer-fixes-20261104");

                // Drive the actual UI input module. Calling Cast directly cannot prove clickability.
                var events = EventSystem.current;
                module = events.GetComponent<InputSystemUIInputModule>();
                Assert.IsNotNull(module);
                mouse = InputSystem.AddDevice<Mouse>();
                devices = module.actionsAsset.devices;
                module.actionsAsset.devices = new InputDevice[] { mouse };
                Canvas.ForceUpdateCanvases();
                Vector2 point = RectTransformUtility.WorldToScreenPoint(null, control.TransformPoint(control.rect.center));
                var hits = new System.Collections.Generic.List<RaycastResult>();
                events.RaycastAll(new PointerEventData(events) { position = point }, hits);
                Assert.IsNotEmpty(hits, "The visible control has no pointer hit.");
                Assert.AreSame(control.gameObject, hits[0].gameObject, "Another graphic covers the skip control.");
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
                yield return null; yield return null; yield return null;
                Assert.IsFalse(MatchArrivalPresentation.Rolling, "A solo press ends the opening.");
                Assert.IsFalse(OpeningSkipVote.Showing, "And the button goes with it.");
                Assert.IsFalse(Map.MatchOpening.HidesUi, "The HUD comes back.");
                until = Time.realtimeSinceStartup + 15;
                while (GameServices.Round?.RoundActive != true && Time.realtimeSinceStartup < until) yield return null;
                Assert.IsTrue(GameServices.Round.RoundActive, "Skipping the opening still starts the round.");
                Assert.IsTrue(CursorMode.WantsCapture, "Gameplay did not recover the pointer after skipping.");
                Assert.IsTrue(Hud.Instance.GetComponent<TumpMatchReadout>().Canvas.isActiveAndEnabled,
                    "Banaue's live readout must be visible after the opening.");
            }
            finally
            {
                SettingsStore.Current.CinematicCameraMotion = cinematic; SettingsStore.Current.ReducedUiMotion = reduced;
                if (module != null && module.actionsAsset != null) module.actionsAsset.devices = devices;
                if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
                settings.backgroundBehavior = background;
                settings.editorInputBehaviorInPlayMode = focus;
                if (captured) CursorMode.Capture(); else CursorMode.Release();
            }
        }
    }
}
