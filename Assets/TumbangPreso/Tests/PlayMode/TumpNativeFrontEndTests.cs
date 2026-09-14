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
            var home = GameObject.Find("OwnerHomeCanvas").GetComponent<Canvas>();
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
            Assert.IsNotNull(GameObject.Find("OwnerHomeCanvas"));
        }

        [UnityTest]
        public IEnumerator NativeSignInKeepsRealFieldsValidationAndBackWithoutSendingCredentials()
        {
            yield return SceneManager.LoadSceneAsync(SceneFlow.MainMenu); yield return null;
            var account = Object.FindFirstObjectByType<ConvertedMainMenu>().GetComponent<SignInScreen>();
            account.Install(); account.Open(); yield return null;
            var canvas = GameObject.Find("OwnerSignInCanvas").GetComponent<Canvas>();
            Assert.IsEmpty(canvas.GetComponentsInChildren<PaperSkin>(true));
            var user = canvas.GetComponentsInChildren<InputField>().First(i => i.name == "Username");
            var password = canvas.GetComponentsInChildren<InputField>().First(i => i.name == "Password");
            Assert.AreEqual(InputField.ContentType.Password, password.contentType);
            user.SetTextWithoutNotify(""); password.SetTextWithoutNotify("");
            Press("SubmitAccount"); yield return null;
            Assert.AreEqual("Enter a username.", canvas.GetComponentsInChildren<Text>().First(t => t.name == "AccountStatus").text);
            yield return TumpUiCapture.Capture("OwnerSignIn-error-v1", canvas, 1920, 1080, false);
            Press("CreateAccountTab"); yield return null;
            Assert.AreEqual("CREATE", Find("SubmitAccount").GetComponentInChildren<Text>().text);
            yield return TumpUiCapture.Capture("OwnerSignIn-create-v1", canvas, 1280, 720, false);
            yield return TumpUiCapture.Capture("OwnerSignIn-create-4by3-v1", canvas, 1280, 960, false);
            Press("SignInBack"); yield return null;
            Assert.IsFalse(account.IsOpen);
            Assert.IsNotNull(GameObject.Find("OwnerHomeCanvas"));
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

        [UnityTest]
        public IEnumerator OwnerAccountUsesExactArtworkTypeColoursAndWorkingTerms()
        {
            bool reduced=Settings.SettingsStore.Current.ReducedUiMotion;
            bool hadPassword=Settings.SettingsStore.Current.AccountHasPassword;
            Settings.SettingsStore.Current.ReducedUiMotion=true;
            try
            {
                yield return SceneManager.LoadSceneAsync(SceneFlow.MainMenu);yield return null;
                var account=Object.FindFirstObjectByType<ConvertedMainMenu>().GetComponent<SignInScreen>();
                Settings.SettingsStore.Current.AccountHasPassword=false;
                account.Install();account.OpenAtBoot();yield return new WaitForSecondsRealtime(.4f);
                var canvas=GameObject.Find("OwnerSignInCanvas").GetComponent<Canvas>();
                Assert.False(canvas.GetComponentsInChildren<Button>(true).Any(button=>button.name=="SignInBack"),
                    "Startup login must not construct a Back button.");
                Assert.IsEmpty(canvas.GetComponentsInChildren<TumpSurface>(true),"The previous visual generator still owns the form.");
                var fields=canvas.GetComponentsInChildren<InputField>();Assert.AreEqual(3,fields.Length);
                var username=fields.First(field=>field.name=="Username");
                username.text="Stardust_Destroyer84";
                Assert.AreEqual(OwnerUiTheme.Current.Reading,username.textComponent.font);
                Assert.AreEqual(Color.black,username.textComponent.color);
                var hint=(Text)fields.First(field=>field.name=="Email").placeholder;
                Assert.AreEqual(OwnerUiTheme.Current.Accent,hint.font);
                Assert.AreEqual((Color)new Color32(188,135,73,255),hint.color);
                Assert.AreEqual((Color)new Color32(144,18,25,255),Find("SubmitAccount").GetComponentInChildren<Text>().color);
                Assert.AreEqual(Color.white,Find("GuestAccount").GetComponentInChildren<Text>().color);
                var logo=canvas.GetComponentsInChildren<Image>().First(image=>image.name=="OriginalOwnerLogo");
                Assert.AreEqual(new Vector2(407,273),logo.sprite.rect.size);Assert.True(logo.preserveAspect);
                Assert.AreEqual(407f/273,logo.rectTransform.rect.width/logo.rectTransform.rect.height,.001f);
                yield return TumpUiCapture.Capture("OwnerStartupSignUp-v1",canvas,1920,1080,false);
                foreach(var name in new[]{"SubmitAccount","GuestAccount"})
                {
                    var label=Find(name).GetComponentInChildren<Text>();
                    Assert.GreaterOrEqual(label.cachedTextGenerator.characterCountVisible,label.text.Length,
                        name+" text was silently truncated by its font line metrics.");
                }
                Press("TermsLink");yield return new WaitForSecondsRealtime(.35f);
                var terms=GameObject.Find("OwnerTermsCanvas").GetComponent<Canvas>();
                Assert.IsNotNull(terms.GetComponentInChildren<ScrollRect>());
                Assert.That(string.Join(" ",terms.GetComponentsInChildren<Text>().Select(label=>label.text)),Does.Contain("PLAY FAIR"));
                yield return TumpUiCapture.Capture("OwnerStartupTerms-v1",terms,1280,720,false,false,new[]{canvas});
                var termsScroll=terms.GetComponentInChildren<ScrollRect>();
                Assert.LessOrEqual(termsScroll.content.rect.height,termsScroll.viewport.rect.height+1,
                    "The short default guidelines should fit without hiding the final paragraph.");
                Press("AcceptGuidelines");yield return null;
                Assert.True(account.IsOpen,"Reading terms closed the account form.");
                Assert.True(canvas.GetComponentInChildren<Toggle>().isOn);
                var password=fields.First(field=>field.name=="Password");password.text="test only";
                Press("RevealPassword");Assert.AreEqual(InputField.ContentType.Standard,password.contentType);
                Press("RevealPassword");Assert.AreEqual(InputField.ContentType.Password,password.contentType);
                Press("SignInTab");yield return null;
                Assert.AreEqual(2,canvas.GetComponentsInChildren<InputField>().Length);
                yield return TumpUiCapture.Capture("OwnerStartupSignIn-v1",canvas,1920,1080,false);
                string playerId=GameServices.Account?.PlayerId;
                Press("GuestAccount");yield return null;
                Assert.False(account.IsOpen);Assert.IsEmpty(password.text,"A closed account view retained password text.");
                Assert.AreEqual(playerId,GameServices.Account?.PlayerId,"Startup Guest must not replace the current progress identity.");
            }
            finally{Settings.SettingsStore.Current.ReducedUiMotion=reduced;Settings.SettingsStore.Current.AccountHasPassword=hadPassword;}
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
