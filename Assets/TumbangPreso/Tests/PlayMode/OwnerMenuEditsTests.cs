using System.Collections;
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
    public sealed class OwnerMenuEditsTests
    {
        [UnitySetUp]public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown]public IEnumerator After()=>PlayModeWorld.Reset();

        /// <summary>
        /// ⚠️⚠️ THIS FIXTURE USED TO DRIVE THE SETTINGS PENNANT AND THERE IS NO PENNANT NOW.
        /// 🧑 2026-09-18: *"MAIN menu is getting revamped it will lose all buttons and will just
        /// have a tap to play or wtv text is"*, and asked where TUTORIAL, SETTINGS and QUIT
        /// should go, *"throw them away gang no need"*. What it was really asserting was that
        /// the owner's art loads, that a control reacts without its hit box moving, and that the
        /// road keeps moving with motion on and stops with it off. All three still have a
        /// subject: the plate, the one full-screen press target, and the dust and the leaves.
        /// </summary>
        [UnityTest,Timeout(120000)]
        public IEnumerator TitleStreetIsOnePressWithHerWeatherMoving()
        {
            bool boot=SceneFlow.BootedThroughSplash,offered=SceneFlow.LoginStepOffered;
            bool reduced=TumbangPreso.Settings.SettingsStore.Current.ReducedUiMotion;
            try
            {
                SceneFlow.BootedThroughSplash=false;
                yield return SceneManager.LoadSceneAsync(SceneFlow.MainMenu);yield return null;
                var canvas=GameObject.Find("OwnerHomeCanvas").GetComponent<Canvas>();
                EventSystem.current.SetSelectedGameObject(null);

                // ⚠️ ONE CONTROL, AND IT IS THE WHOLE SCREEN. A second button here would be the
                // start of growing the pennants back one at a time.
                var buttons=canvas.GetComponentsInChildren<Button>().Where(b=>b.isActiveAndEnabled).ToArray();
                Assert.AreEqual(1,buttons.Length,"The title screen is one press: "+
                    string.Join(", ",buttons.Select(b=>b.name)));
                Assert.AreEqual("StartButton",buttons[0].name);
                var surface=(RectTransform)buttons[0].transform;
                Assert.AreEqual(Vector2.zero,surface.anchorMin);Assert.AreEqual(Vector2.one,surface.anchorMax);

                var prompt=canvas.GetComponentsInChildren<Text>().Single(t=>t.name=="ContinuePrompt");
                StringAssert.Contains("to continue",prompt.text);

                var air=canvas.GetComponentInChildren<OwnerMenuAir>();
                Assert.IsNotNull(air,"Her clouds and her cast shadow are the menu's only motion in the air");
                var material=air.GetComponent<RawImage>().material;
                foreach(string texture in new[]{"_SkyMask","_Cloud","_Shadow"})
                    Assert.IsNotNull(material.GetTexture(texture),texture+" failed to load");

                TumbangPreso.Settings.SettingsStore.Current.ReducedUiMotion=true;
                foreach(var size in TumpUiCapture.PcViewports)
                    yield return TumpUiCapture.Capture("OwnerMenu-v8-"+size.x+"x"+size.y,canvas,size.x,size.y,false);
                Assert.AreEqual("menu",GameServices.Music.Current);

                TumbangPreso.Settings.SettingsStore.Current.ReducedUiMotion=false;
                yield return new WaitForSecondsRealtime(.3f);
                var dust=canvas.GetComponentInChildren<OwnerRoadDust>();
                var leaves=canvas.GetComponentInChildren<OwnerMenuLeaves>();
                yield return TumpUiCapture.Capture("OwnerMenu-v8-weather",canvas,1920,1080,false);
                Assert.Greater(dust.canvasRenderer.GetMesh().vertexCount,0);
                Assert.Greater(leaves.canvasRenderer.GetMesh().vertexCount,0,"Leaves fall off her tree");

                TumbangPreso.Settings.SettingsStore.Current.ReducedUiMotion=true;
                yield return null;yield return null;Canvas.ForceUpdateCanvases();
                Assert.AreEqual(0,dust.canvasRenderer.GetMesh().vertexCount,"Reduced motion must remove drifting dust");
                Assert.AreEqual(0,leaves.canvasRenderer.GetMesh().vertexCount,"Reduced motion must ground the leaves");

                buttons[0].onClick.Invoke();
                yield return new WaitForSecondsRealtime(.5f);
                Assert.AreEqual(SceneFlow.ModeSelect,SceneManager.GetActiveScene().name,
                    "A press anywhere on the street is the way in");
            }
            finally{SceneFlow.BootedThroughSplash=boot;SceneFlow.LoginStepOffered=offered;TumbangPreso.Settings.SettingsStore.Current.ReducedUiMotion=reduced;}
        }

        [UnityTest,Timeout(90000)]
        public IEnumerator StartupLoginStaysSilentUntilGuestRevealsHome()
        {
            bool boot=SceneFlow.BootedThroughSplash,offered=SceneFlow.LoginStepOffered;
            try
            {
                GameServices.Ensure();GameServices.Music.StopNow();
                SceneFlow.BootedThroughSplash=true;SceneFlow.LoginStepOffered=false;
                yield return SceneManager.LoadSceneAsync(SceneFlow.MainMenu);yield return null;yield return null;
                var login=Object.FindFirstObjectByType<SignInScreen>();Assert.True(login.IsOpen);
                Assert.IsNull(GameServices.Music.Current,"Startup login must not start the menu bed");
                var canvas=Find("GuestAccount").GetComponentInParent<Canvas>();
                Assert.IsFalse(canvas.GetComponentsInChildren<Button>().Any(b=>b.name=="BackButton"));
                Assert.IsEmpty(canvas.GetComponentsInChildren<Transform>().Where(t=>t.name=="FieldIcon" || t.name=="PersonIcon"),"Supplied fields already contain icons");
                foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(960,540),new Vector2Int(1280,960),new Vector2Int(3440,1440)})
                    yield return TumpUiCapture.Capture("OwnerLogin-v7-create-"+size.x+"x"+size.y,canvas,size.x,size.y,false,checkActionBounds:true);
                Find("TermsLink").onClick.Invoke();yield return null;
                Find("AcceptGuidelines").onClick.Invoke();yield return new WaitForSecondsRealtime(.2f);
                var terms=canvas.GetComponentsInChildren<Toggle>().First(t=>t.name=="TermsAcceptance");
                Assert.True(terms.isOn,"Accepting the actual Terms dialog must tick the signup checkbox");
                Assert.Greater(terms.graphic.canvasRenderer.GetAlpha(),.95f,"Terms checkmark must be visibly rendered");
                var fields=canvas.GetComponentsInChildren<InputField>();
                Assert.IsFalse(fields.Any(f=>f.name=="Email"));
                var confirmation=fields.First(f=>f.name=="ConfirmPassword");
                fields.First(f=>f.name=="Username").text="local.validation";
                // ⚠️ A PASSWORD THAT PASSES THE REAL RULES, so this case still reaches the
                // confirmation check. `OwnerFieldFault` enforces UGS's own 8-to-30 with an
                // upper, a lower, a digit and a symbol, and the old fixture's "test-only"
                // would now be refused for its length before the two were ever compared.
                fields.First(f=>f.name=="Password").text="Test-only1";
                confirmation.text="Different-1";Find("SubmitAccount").onClick.Invoke();yield return null;
                // ⚠️⚠️ THE MISMATCH IS UNDER THE FIELD IT IS ABOUT NOW, NOT IN THE SHARED LINE.
                // She redrew the login with a red line under each input; `AccountStatus` keeps
                // only what is about the whole attempt. `SignInScreen.Fail` routes the rest.
                StringAssert.Contains("do not match",canvas.GetComponentsInChildren<Text>().First(t=>t.name=="ConfirmFault").text);
                Assert.IsEmpty(canvas.GetComponentsInChildren<Text>().First(t=>t.name=="AccountStatus").text);
                confirmation.text=fields.First(f=>f.name=="Password").text;yield return null;yield return null;
                Assert.IsEmpty(canvas.GetComponentsInChildren<Text>().First(t=>t.name=="ConfirmFault").text,
                    "Live validation must clear a fault the player has fixed");
                Find("RevealConfirmation").onClick.Invoke();Assert.AreEqual(InputField.ContentType.Standard,confirmation.contentType);
                Find("RevealConfirmation").onClick.Invoke();Assert.AreEqual(InputField.ContentType.Password,confirmation.contentType);
                var divider=canvas.GetComponentsInChildren<RectTransform>().First(t=>t.name=="AccountDivider");
                var left=(RectTransform)divider.Find("LeftDivider");var right=(RectTransform)divider.Find("RightDivider");
                // ⚠️⚠️ THE TWO STROKES ARE HER OWN PIXELS NOW, AND HERS ARE NOT IDENTICAL.
                // The September 15 pass drew both as generated images so it could assert they
                // matched exactly; she drew them by hand at 241x5 and 242x6. Resizing either one
                // to make this line pass would be squashing her art to satisfy a test, so the
                // assertion is what a player can actually see: a pair, level with each other.
                Assert.That(Mathf.Abs(left.sizeDelta.x-right.sizeDelta.x),Is.LessThanOrEqualTo(2f));
                Assert.That(Mathf.Abs(left.sizeDelta.y-right.sizeDelta.y),Is.LessThanOrEqualTo(2f));
                Assert.That(Mathf.Abs((left.anchoredPosition.y-left.sizeDelta.y*.5f)
                                     -(right.anchoredPosition.y-right.sizeDelta.y*.5f)),Is.LessThanOrEqualTo(1f),
                    "The two strokes must share an optical centre line either side of OR");
                fields.First(f=>f.name=="Username").text="";fields.First(f=>f.name=="Password").text="";
                confirmation.text="";
                Find("SignInTab").onClick.Invoke();yield return null;
                // ⚠️ HER SIGN-IN SHEET RENAMES THE FIRST FIELD RATHER THAN ADDING ONE.
                Assert.AreEqual("TUMP ID",((Text)fields.First(f=>f.name=="Username").placeholder).text);
                yield return TumpUiCapture.Capture("OwnerLogin-v7-signin",canvas,1920,1080,false,checkActionBounds:true);
                Assert.IsNull(GameServices.Music.Current);
                var password=canvas.GetComponentsInChildren<InputField>().First(f=>f.name=="Password");
                password.text="local-test-only";Find("RevealPassword").onClick.Invoke();
                Assert.AreEqual(InputField.ContentType.Standard,password.contentType);
                Find("RevealPassword").onClick.Invoke();Assert.AreEqual(InputField.ContentType.Password,password.contentType);
                Assert.IsFalse(confirmation.gameObject.activeInHierarchy);
                Assert.IsFalse(canvas.GetComponentsInChildren<Button>().Any(b=>b.name=="GuestAccount"));
                Find("CreateAccountTab").onClick.Invoke();yield return null;
                Find("GuestAccount").onClick.Invoke();yield return null;yield return null;
                Assert.False(login.IsOpen);Assert.AreEqual("menu",GameServices.Music.Current);
                var sources=GameServices.Music.GetComponents<AudioSource>();
                Assert.AreEqual(1,sources.Count(s=>s.isPlaying),"One menu track should play after entering home");
                var playing=sources.First(s=>s.isPlaying);float position=playing.time;
                var home=Object.FindFirstObjectByType<TumpHomeView>();home.Suspend();yield return null;home.Resume();yield return null;
                Assert.AreEqual(1,sources.Count(s=>s.isPlaying));Assert.GreaterOrEqual(playing.time,position);
            }
            finally{SceneFlow.BootedThroughSplash=boot;SceneFlow.LoginStepOffered=offered;}
        }

        private static Button Find(string name)=>Object.FindObjectsByType<Button>().First(b=>b.name==name && b.isActiveAndEnabled);
    }
}
