using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class HomeFlowTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [UnityTest]
        public IEnumerator HomePreparesItsLoopAndEveryPreparationDoorReturns()
        {
            yield return SceneManager.LoadSceneAsync(SceneFlow.MainMenu);
            yield return new WaitForSecondsRealtime(0.5f);
            var home = Object.FindFirstObjectByType<HomeScreen>();
            Assert.IsNotNull(home);
            var illustration = home.GetComponentInChildren<IllustratedBackdrop>();
            if (illustration == null)
                illustration = Object.FindFirstObjectByType<IllustratedBackdrop>();
            Assert.IsNotNull(illustration);
            Assert.IsTrue(illustration.HasArtwork, "The illustrated placeholder must ship with its artwork.");
            Assert.IsTrue(illustration.HasIndependentLayers, "Tree and sun must be independent animated layers.");
            home.SendMessage("OnApplicationFocus", true);
            float before = illustration.Phase;
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.AreNotEqual(before, illustration.Phase, "The illustrated scene must advance in the foreground.");

            Press("SettingsButton");
            yield return null;
            var settings = Object.FindFirstObjectByType<ConvertedSettingsPanel>();
            Assert.IsNotNull(settings);
            Press("BackButton");
            yield return null;
            yield return null;
            Assert.IsTrue(ActiveButton("StartButton").isActiveAndEnabled);

            Press("ProfileButton");
            yield return null;
            Assert.IsTrue(Object.FindFirstObjectByType<PlayerHub>().IsOpen);
            Press("HubClose");
            yield return null;
            yield return null;
            Assert.IsTrue(GameObject.Find("HomeCanvas").GetComponent<Canvas>().enabled);

            Press("GearButton");
            yield return null;
            yield return null;
            var picker = Object.FindFirstObjectByType<ConvertedCharacterSelect>();
            Assert.IsNotNull(picker, "Gear must reach the actual equipment picker.");
            yield return UiRuntimeShots.Capture("Gear-before-overhaul-v1", 1920, 1080);
            Assert.IsFalse(picker.GetComponentsInChildren<Button>(true).Any(b => b.name == "CustomDoor"),
                           "The withdrawn maker must not acquire a new door through Home.");
            Press("BackButton");
            yield return null;
            Assert.IsFalse(picker.gameObject.activeSelf);
            Assert.IsTrue(GameObject.Find("HomeCanvas").GetComponent<Canvas>().enabled);
            Assert.IsNotNull(ActiveButton("StartButton"));

            yield return UiRuntimeShots.Capture("Home-v4", 1920, 1080);
            yield return UiRuntimeShots.Capture("Home-v4-shortwide", 1920, 820);
        }

        [UnityTest]
        public IEnumerator PlaySeparatesRulesFromRoutesAndNeverPromisesAClassicLadder()
        {
            yield return SceneManager.LoadSceneAsync(SceneFlow.MainMenu);
            yield return new WaitForSecondsRealtime(0.25f);
            Press("StartButton");
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.AreEqual(SceneFlow.ModeSelect, SceneManager.GetActiveScene().name);
            Assert.IsNotNull(ActiveButton("TutorialButton"));
            Press("ClassicButton");
            yield return null;
            Assert.IsNotNull(ActiveButton("PracticeButton"));
            Assert.IsNotNull(ActiveButton("CustomButton"));
            Assert.IsFalse(Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .Any(b => b.name == "RankedButton" && b.isActiveAndEnabled));
            Press("BackButton");
            yield return null;
            Press("HeroStrikeButton");
            yield return null;
            Assert.IsNotNull(ActiveButton("RankedButton"));
            yield return UiRuntimeShots.Capture("Play-routes-v3", 1920, 1080);
            Press("RankedButton");
            yield return new WaitForSecondsRealtime(2f);
            Assert.AreEqual(SceneFlow.MatchSetup, SceneManager.GetActiveScene().name);
            Assert.IsFalse(PlaySelectionScreen.RequestedLobbyMode.HasValue);
            Assert.IsFalse(Net.NetSession.Instance != null && Net.NetSession.Instance.IsNetworked,
                           "Opening the ranked destination must not open a LAN room.");
        }

        private static Button ActiveButton(string name) => Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
            .First(b => b.name == name && b.isActiveAndEnabled);

        // A callback can pass while artwork or another canvas consumes every real press.
        // Exercise the top raycast target before dispatching the pointer event to it.
        private static void Press(string name)
        {
            Canvas.ForceUpdateCanvases();
            var button = ActiveButton(name);
            var rect = (RectTransform)button.transform;
            var canvas = button.GetComponentInParent<Canvas>();
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
            var pointer = new PointerEventData(EventSystem.current) { position = position, button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.IsNotEmpty(hits, name + " has no raycast target.");
            Assert.AreEqual(button, hits[0].gameObject.GetComponentInParent<Button>(),
                name + " is covered by " + hits[0].gameObject.name);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }

        [UnityTest]
        public IEnumerator LoadingStoryCanBeOpenedAdvancedAndClosedWithoutSkippingReadiness()
        {
            yield return SceneManager.LoadSceneAsync(SceneFlow.Splash);
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.IsNotNull(Object.FindFirstObjectByType<SplashScreen>());
            Press("LoadingArtButton");
            yield return null;
            var story = GameObject.Find("LoadingStoryRoot");
            Assert.IsNotNull(story);
            var text = story.GetComponentsInChildren<Text>().First(t => t.text.Contains("TUMP's world"));
            string first = text.text;
            Press("LoadingStoryNext");
            yield return null;
            Assert.AreNotEqual(first, text.text);
            yield return new WaitForSecondsRealtime(15.1f);
            Assert.AreEqual(SceneFlow.Splash, SceneManager.GetActiveScene().name,
                "Reading must hold the loading screen past its maximum random dwell.");
            Press("LoadingStoryClose");
            yield return null;
            Assert.IsFalse(story.activeSelf);
            float end = Time.realtimeSinceStartup + 60f;
            while (SceneManager.GetActiveScene().name != SceneFlow.MainMenu && Time.realtimeSinceStartup < end)
                yield return null;
            Assert.AreEqual(SceneFlow.MainMenu, SceneManager.GetActiveScene().name);
            Assert.IsNotNull(ActiveButton("StartButton"));
        }
    }
}
