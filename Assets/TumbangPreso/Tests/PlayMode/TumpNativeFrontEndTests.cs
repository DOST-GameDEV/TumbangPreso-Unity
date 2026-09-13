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
            var creditScroll = credits.GetComponentInChildren<ScrollRect>();
            creditScroll.verticalNormalizedPosition = 0; yield return null; yield return null;
            var lastCredit = credits.GetComponentsInChildren<Text>().Last(t => t.name == "CreditBody");
            Assert.GreaterOrEqual(lastCredit.rectTransform.rect.height + 1, lastCredit.preferredHeight, "The complete licence body must fit its own text rect.");
            var corners = new Vector3[4]; lastCredit.rectTransform.GetWorldCorners(corners);
            Assert.GreaterOrEqual(creditScroll.viewport.InverseTransformPoint(corners[0]).y + 1, creditScroll.viewport.rect.yMin,
                "The final licence must be reachable by scrolling to the end.");
            Assert.That(creditScroll.verticalScrollbar.handleRect.rect.width, Is.LessThanOrEqualTo(12), "Scrollbar must stay inside its narrow track.");
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
            yield return TumpUiCapture.Capture("NativeSignIn-create-4by3-v1", canvas, 1280, 960);
            Press("SignInBack"); yield return null;
            Assert.IsFalse(account.IsOpen);
            Assert.IsNotNull(GameObject.Find("TumpHomeCanvas"));
        }
        [UnityTest]
        public IEnumerator IllustratedPropsWaitForRealLayoutBeforeMappingTheirAnchors()
        {
            var go = new GameObject("UnlaidOutBackdrop", typeof(RectTransform), typeof(RawImage));
            var rect = (RectTransform)go.transform; rect.sizeDelta = Vector2.zero;
            try
            {
                var backdrop = go.AddComponent<TumpBackdrop>();
                var props = go.GetComponentsInChildren<Image>();
                Assert.AreEqual(2, props.Length);
                foreach (var prop in props)
                {
                    Assert.IsNotNull(prop.sprite, "The configured prop must produce an actual sprite.");
                    Assert.Greater(prop.sprite.rect.width, 0); Assert.Greater(prop.sprite.rect.height, 0);
                    foreach (var vertex in prop.sprite.vertices)
                    {
                        Assert.IsFalse(float.IsNaN(vertex.x) || float.IsInfinity(vertex.x));
                        Assert.IsFalse(float.IsNaN(vertex.y) || float.IsInfinity(vertex.y));
                    }
                }
                backdrop.SendMessage("LateUpdate");
                foreach (var prop in go.GetComponentsInChildren<Image>())
                {
                    Assert.IsFalse(prop.enabled, "Unlaid-out props must wait for positive dimensions.");
                    Assert.IsFalse(float.IsNaN(prop.rectTransform.anchorMin.x) || float.IsInfinity(prop.rectTransform.anchorMin.x));
                    Assert.IsFalse(float.IsNaN(prop.rectTransform.anchorMin.y) || float.IsInfinity(prop.rectTransform.anchorMin.y));
                }
                rect.sizeDelta = new Vector2(1920, 1080); backdrop.SendMessage("LateUpdate");
                foreach (var prop in go.GetComponentsInChildren<Image>())
                {
                    Assert.IsTrue(prop.enabled);
                    Assert.IsFalse(float.IsNaN(prop.rectTransform.anchorMin.x) || float.IsInfinity(prop.rectTransform.anchorMin.x));
                    Assert.IsFalse(float.IsNaN(prop.rectTransform.anchorMin.y) || float.IsInfinity(prop.rectTransform.anchorMin.y));
                }
                yield return null;
            }
            finally { Object.DestroyImmediate(go); }
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
