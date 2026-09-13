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
        public IEnumerator HomeKeepsTheTitleCalmAndSettingsReturn()
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

            CollectionAssert.AreEquivalent(new[]{"StartButton","TutorialButton","SettingsButton","QuitButton","CreditsButton"},
                GameObject.Find("HomeCanvas").GetComponentsInChildren<Button>().Select(button=>button.name));
            Assert.IsFalse(home.GetComponentsInChildren<Button>().Any(button=>button.name=="CustomDoor"));
            yield return UiRuntimeShots.Capture("Home-brand-v1",1920,1080);
            yield return UiRuntimeShots.Capture("Home-brand-v1-720p",1280,720);
            yield return UiRuntimeShots.Capture("Home-brand-v1-shortwide",1920,820);
        }

        [UnityTest]
        public IEnumerator PreparationRemainsReachableFromThePracticeLobby()
        {
            yield return SceneManager.LoadSceneAsync(SceneFlow.MainMenu);
            yield return new WaitForSecondsRealtime(.3f);
            Press("StartButton");yield return new WaitForSecondsRealtime(.4f);
            Press("ClassicButton");yield return null;
            Press("PracticeButton");yield return new WaitForSecondsRealtime(.6f);
            Assert.AreEqual(SceneFlow.MatchSetup,SceneManager.GetActiveScene().name);
            Press("ProfileButton");yield return null;
            Assert.IsTrue(Object.FindFirstObjectByType<PlayerHub>().IsOpen);
            Press("HubClose");yield return null;yield return null;
            Press("CharacterButton");yield return null;yield return null;
            var picker=Object.FindFirstObjectByType<ConvertedCharacterSelect>();
            Assert.IsNotNull(picker,"Preparation must still expose the real character/equipment picker.");
            Assert.IsFalse(picker.GetComponentsInChildren<Button>(true).Any(button=>button.name=="CustomDoor"));
            var pickerBack=picker.GetComponentsInChildren<Button>().Single(button=>button.name=="BackButton");
            Debug.Log("[HomeFlow] First named Back="+Hierarchy(ActiveButton("BackButton").transform)+
                "; actual picker Back="+Hierarchy(pickerBack.transform));
            yield return UiRuntimeShots.Capture("Picker-from-lobby-brand-v1",1920,1080);
            Press(pickerBack);yield return null;yield return null;
            Assert.IsNotNull(ActiveButton("CharacterButton"));
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
            Assert.IsNotNull(ActiveButton("HeroStrikeButton"),"Game choice stays visible beside its access routes.");
            Press("HeroStrikeButton");
            yield return null;
            Assert.IsNotNull(ActiveButton("RankedButton"));
            Assert.IsNotNull(GameObject.Find("PlayChoiceCanvas/TumpMark").GetComponent<Image>().sprite,
                "A texture imported as Default must not become a white logo rectangle.");
            yield return UiRuntimeShots.Capture("Play-brand-v2",1920,1080);
            yield return UiRuntimeShots.Capture("Play-brand-v2-720p",1280,720);
            yield return UiRuntimeShots.Capture("Play-brand-v2-4by3",1200,900);
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
        private static string Hierarchy(Transform node)
        { string path=node.name;while(node.parent!=null){node=node.parent;path=node.name+"/"+path;}return path; }

        private static void Press(string name) => Press(ActiveButton(name));
        private static void Press(Button button)
        {
            Canvas.ForceUpdateCanvases();
            string name=button.name;
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
