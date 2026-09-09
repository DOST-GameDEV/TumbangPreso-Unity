using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
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
            settings.GetComponentsInChildren<Button>().First(b => b.name == "BackButton").onClick.Invoke();
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
            picker.GetComponentsInChildren<Button>().First(b => b.name == "BackButton").onClick.Invoke();
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
        private static void Press(string name) => ActiveButton(name).onClick.Invoke();
    }
}
