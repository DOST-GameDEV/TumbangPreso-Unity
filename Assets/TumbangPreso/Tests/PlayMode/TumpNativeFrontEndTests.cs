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
            foreach (var size in new[] { new Vector2Int(960, 540), new Vector2Int(1280, 720), new Vector2Int(1366, 768),
                new Vector2Int(1920, 1080), new Vector2Int(1920, 1200), new Vector2Int(1280, 960),
                new Vector2Int(2560, 1440), new Vector2Int(3440, 1440), new Vector2Int(3840, 1080), new Vector2Int(3840, 2160) })
                yield return TumpUiCapture.Capture("NativeHome-" + size.x + "x" + size.y, home, size.x, size.y, false, checkActionBounds: true);
            Press("CreditsButton"); yield return null;
            var credits = GameObject.Find("OwnerCreditsCanvas").GetComponent<Canvas>();
            Assert.IsFalse(home.gameObject.activeSelf);
            Assert.AreEqual(CreditsContent.CcByCredits.Length + CreditsContent.CourtesyCredits.Length,
                credits.GetComponentsInChildren<Text>().Count(t => t.name == "CreditBody"));
            foreach(var size in TumpUiCapture.PcViewports)
                yield return TumpUiCapture.Capture("StudioCredits-team-"+size.x+"x"+size.y,credits,size.x,size.y,false,checkActionBounds:true);
            var creditScroll = credits.GetComponentInChildren<ScrollRect>();
            creditScroll.verticalNormalizedPosition = 0; yield return null; yield return null;
            var lastCredit = credits.GetComponentsInChildren<Text>().Last(t => t.name == "CreditBody");
            Debug.Log($"[OwnerCreditScroll] normalized={creditScroll.verticalNormalizedPosition} content={creditScroll.content.rect.height} viewport={creditScroll.viewport.rect.height} lastHeight={lastCredit.rectTransform.rect.height} preferred={lastCredit.preferredHeight}");
            var creditLayout=creditScroll.content.GetComponent<VerticalLayoutGroup>();
            float childHeight=creditLayout.padding.vertical+Mathf.Max(0,creditScroll.content.childCount-1)*creditLayout.spacing;
            foreach(RectTransform child in creditScroll.content)childHeight+=child.rect.height;
            Debug.Log($"[OwnerCreditLayout] actualChildren={childHeight} preferred={creditLayout.preferredHeight} padding={creditLayout.padding.bottom} contentPos={creditScroll.content.anchoredPosition} lastPos={lastCredit.rectTransform.anchoredPosition}");
            yield return TumpUiCapture.Capture("OwnerCredits-bottom-diagnostic-v2",credits,1920,1080,false);
            Assert.GreaterOrEqual(lastCredit.rectTransform.rect.height + 1, lastCredit.preferredHeight, "The complete licence body must fit its own text rect.");
            var corners = new Vector3[4]; lastCredit.rectTransform.GetWorldCorners(corners);
            var contentCorners=new Vector3[4];creditScroll.content.GetWorldCorners(contentCorners);
            Debug.Log($"[OwnerCreditCoordinates] contentRect={creditScroll.content.rect} pivot={creditScroll.content.pivot} anchors={creditScroll.content.anchorMin}/{creditScroll.content.anchorMax} contentScale={creditScroll.content.localScale} lastScale={lastCredit.rectTransform.localScale} lastPivot={lastCredit.rectTransform.pivot} contentBottom={creditScroll.viewport.InverseTransformPoint(contentCorners[0])} lastBottom={creditScroll.viewport.InverseTransformPoint(corners[0])} finalNormalized={creditScroll.verticalNormalizedPosition} finalContent={creditScroll.content.rect.height} finalPos={creditScroll.content.anchoredPosition} finalLast={lastCredit.rectTransform.anchoredPosition}/{lastCredit.rectTransform.rect}");
            Assert.GreaterOrEqual(creditScroll.viewport.InverseTransformPoint(corners[0]).y + 1, creditScroll.viewport.rect.yMin,
                "The final licence must be reachable by scrolling to the end.");
            Assert.That(creditScroll.verticalScrollbar.handleRect.rect.width, Is.LessThanOrEqualTo(12), "Scrollbar must stay inside its narrow track.");
            foreach(var size in TumpUiCapture.PcViewports)
            {
                creditScroll.verticalNormalizedPosition=0;
                yield return TumpUiCapture.Capture("StudioCredits-licenses-"+size.x+"x"+size.y,credits,size.x,size.y,false,checkActionBounds:true);
            }
            Press("CreditsBack"); yield return null;
            Assert.IsTrue(home.gameObject.activeSelf);
            Press("SettingsButton"); yield return null;
            Assert.IsNotNull(GameObject.Find("OwnerSettingsCanvas"));
            Press("TumpSettingsBack"); yield return null;
            Assert.IsTrue(home.gameObject.activeSelf);
            Press("StartButton"); yield return null; yield return null;
            var play = GameObject.Find("OwnerPlayCanvas").GetComponent<Canvas>();
            Assert.IsEmpty(play.GetComponentsInChildren<PaperSkin>(true));
            Assert.AreEqual(6, play.GetComponentsInChildren<Image>().Count(i => i.name.StartsWith("ModePortrait") && i.sprite != null));
            Press("ClassicButton"); yield return null;
            Assert.AreEqual(GameMode.Classic, SceneFlow.SelectedMode);
            Assert.IsFalse(play.GetComponentsInChildren<Button>(true).First(b => b.name == "RankedButton").gameObject.activeInHierarchy);
            yield return TumpUiCapture.Capture("OwnerPlay-Classic-v1", play, 1920, 1080, false);
            Press("HeroStrikeButton"); yield return null;
            Assert.IsTrue(Find("RankedButton").interactable);
            foreach (var size in new[] { new Vector2Int(960, 540), new Vector2Int(1280, 720), new Vector2Int(1366, 768),
                new Vector2Int(1920, 1080), new Vector2Int(1920, 1200), new Vector2Int(1280, 960),
                new Vector2Int(2560, 1440), new Vector2Int(3440, 1440), new Vector2Int(3840, 1080), new Vector2Int(3840, 2160) })
                yield return TumpUiCapture.Capture("CourtPlay-Hero-" + size.x + "x" + size.y, play, size.x, size.y, false, checkActionBounds: true);
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

        [UnityTest]
        public IEnumerator OwnerLoadingKeepsSourceArtProgressAndOptionalStories()
        {
            var owner=new GameObject("LoadingVisualFixture");owner.SetActive(false);
            var loading=owner.AddComponent<SplashScreen>();
            const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            typeof(SplashScreen).GetMethod("BuildOwnerLoadingSurface",flags).Invoke(loading,null);
            typeof(SplashScreen).GetMethod("SetFade",flags).Invoke(loading,new object[]{0f});
            typeof(SplashScreen).GetMethod("SetLoadingStage",flags).Invoke(loading,new object[]{"loading characters",.45f});
            for(int i=0;i<10;i++)
            {typeof(SplashScreen).GetMethod("UpdateLoadingAnimation",flags).Invoke(loading,null);yield return null;}
            var canvas=GameObject.Find("OwnerLoadingCanvas").GetComponent<Canvas>();
            Assert.IsEmpty(canvas.GetComponentsInChildren<StreetGraphic>(true));
            var progress=canvas.GetComponentsInChildren<Image>().First(image=>image.name=="ActualProgress");
            Assert.Greater(progress.fillAmount,0);Assert.LessOrEqual(progress.fillAmount,.45f);
            Assert.AreEqual("GETTING THE PLAYERS READY",canvas.GetComponentsInChildren<Text>().First(text=>text.name=="LoadingStatus").text);
            yield return TumpUiCapture.Capture("OwnerLoading-v1",canvas,1920,1080,false);
            Press("LoadingStories");yield return null;
            var story=canvas.GetComponentsInChildren<Text>().First(text=>text.name=="StoryText");var before=story.text;
            Assert.IsNotEmpty(before);Press("LoadingStoryNext");Assert.AreNotEqual(before,story.text);
            yield return TumpUiCapture.Capture("OwnerLoading-story-v1",canvas,1280,720,false);
            Press("LoadingStoryClose");yield return null;
            Assert.False(story.gameObject.activeInHierarchy);
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
