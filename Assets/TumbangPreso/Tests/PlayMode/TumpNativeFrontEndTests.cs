using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class TumpNativeFrontEndTests
    {
        [UnitySetUp] public IEnumerator Before() { yield return PlayModeWorld.Reset(); }
        [UnityTearDown] public IEnumerator After() { yield return PlayModeWorld.Reset(); }

        [UnityTest]
        public IEnumerator TitlePlayCreditsAndSettingsReturnThroughNativeViews()
        {
            yield return SceneManager.LoadSceneAsync(SceneFlow.MainMenu); yield return null;
            var home = GameObject.Find("TumpHomeCanvas").GetComponent<Canvas>();
            Assert.IsEmpty(home.GetComponentsInChildren<PaperSkin>(true));
            Assert.AreEqual(5, home.GetComponentsInChildren<Button>().Length, "Four choices and a credits link keep the title quiet.");
            foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(1280, 720), new Vector2Int(1280, 960) })
                yield return TumpUiCapture.Capture("NativeHome-" + size.x + "x" + size.y, home, size.x, size.y, false);
            Press("CreditsButton"); yield return null;
            var credits = GameObject.Find("TumpCreditsCanvas").GetComponent<Canvas>();
            Assert.IsFalse(home.gameObject.activeSelf);
            Assert.AreEqual(CreditsContent.CcByCredits.Length + CreditsContent.CourtesyCredits.Length,
                credits.GetComponentsInChildren<Text>().Count(t => t.name == "CreditBody"));
            yield return TumpUiCapture.Capture("NativeCredits-v1", credits, 1920, 1080);
            credits.GetComponentInChildren<ScrollRect>().verticalNormalizedPosition = 0; yield return null;
            yield return TumpUiCapture.Capture("NativeCredits-licenses-v1", credits, 1280, 960);
            Press("CreditsBack"); yield return null;
            Assert.IsTrue(home.gameObject.activeSelf);
            Press("SettingsButton"); yield return null;
            Assert.IsNotNull(GameObject.Find("TumpSettingsCanvas"));
            Press("TumpSettingsBack"); yield return null;
            Assert.IsTrue(home.gameObject.activeSelf);
            Press("StartButton"); yield return null; yield return null;
            var play = GameObject.Find("TumpPlayCanvas").GetComponent<Canvas>();
            Assert.IsEmpty(play.GetComponentsInChildren<PaperSkin>(true));
            Assert.AreEqual(6, play.GetComponentsInChildren<Image>().Count(i => i.name.StartsWith("ModePortrait") && i.sprite != null));
            Press("ClassicButton"); yield return null;
            Assert.AreEqual(GameMode.Classic, SceneFlow.SelectedMode);
            Assert.IsFalse(play.GetComponentsInChildren<Button>(true).First(b => b.name == "RankedButton").gameObject.activeSelf);
            yield return TumpUiCapture.Capture("NativePlay-Classic-v1", play, 1920, 1080);
            Press("HeroStrikeButton"); yield return null;
            Assert.IsTrue(Find("RankedButton").interactable);
            yield return TumpUiCapture.Capture("NativePlay-Hero-v1", play, 1280, 960);
            Press("BackButton"); yield return null; yield return null;
            Assert.IsNotNull(GameObject.Find("TumpHomeCanvas"));
        }

        [UnityTest]
        public IEnumerator NativeSignInKeepsRealFieldsValidationAndBackWithoutSendingCredentials()
        {
            yield return SceneManager.LoadSceneAsync(SceneFlow.MainMenu); yield return null;
            var account = Object.FindFirstObjectByType<ConvertedMainMenu>().GetComponent<SignInScreen>();
            account.Install(); account.Open(); yield return null;
            var canvas = GameObject.Find("TumpSignInCanvas").GetComponent<Canvas>();
            Assert.IsEmpty(canvas.GetComponentsInChildren<PaperSkin>(true));
            var user = canvas.GetComponentsInChildren<InputField>().First(i => i.name == "Username");
            var password = canvas.GetComponentsInChildren<InputField>().First(i => i.name == "Password");
            Assert.AreEqual(InputField.ContentType.Password, password.contentType);
            user.SetTextWithoutNotify(""); password.SetTextWithoutNotify("");
            Press("SubmitAccount"); yield return null;
            Assert.AreEqual("Enter a username.", canvas.GetComponentsInChildren<Text>().First(t => t.name == "AccountStatus").text);
            yield return TumpUiCapture.Capture("NativeSignIn-error-v1", canvas, 1920, 1080);
            Press("CreateAccountTab"); yield return null;
            Assert.AreEqual("Create account", Find("SubmitAccount").GetComponentInChildren<Text>().text);
            yield return TumpUiCapture.Capture("NativeSignIn-create-v1", canvas, 1280, 720);
            Press("SignInBack"); yield return null;
            Assert.IsFalse(account.IsOpen);
            Assert.IsNotNull(GameObject.Find("TumpHomeCanvas"));
        }
        private static Button Find(string name) => Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b => b.name == name && b.isActiveAndEnabled);
        private static void Press(string name)
        {
            var button = Find(name); Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)button.transform; var canvas = button.GetComponentInParent<Canvas>();
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center)) };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            Assert.IsNotEmpty(hits, name);
            Assert.AreEqual(button, hits[0].gameObject.GetComponentInParent<Button>(), name + " covered by " + hits[0].gameObject.name);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }
    }
}
