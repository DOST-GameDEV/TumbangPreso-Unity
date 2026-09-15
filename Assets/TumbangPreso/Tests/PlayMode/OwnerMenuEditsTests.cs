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

        [UnityTest,Timeout(120000)]
        public IEnumerator MainCompositionKeepsArtworkControlsAndRoadMotionAcrossPcWindows()
        {
            bool boot=SceneFlow.BootedThroughSplash,offered=SceneFlow.LoginStepOffered;
            bool reduced=TumbangPreso.Settings.SettingsStore.Current.ReducedUiMotion;
            try
            {
                SceneFlow.BootedThroughSplash=false;
                yield return SceneManager.LoadSceneAsync(SceneFlow.MainMenu);yield return null;
                var canvas=GameObject.Find("OwnerHomeCanvas").GetComponent<Canvas>();
                EventSystem.current.SetSelectedGameObject(null);
                TumbangPreso.Settings.SettingsStore.Current.ReducedUiMotion=true;
                foreach(var size in TumpUiCapture.PcViewports)
                    yield return TumpUiCapture.Capture("OwnerMenu-v7-"+size.x+"x"+size.y,canvas,size.x,size.y,false,checkActionBounds:true);
                Assert.AreEqual("menu",GameServices.Music.Current);
                foreach(var art in canvas.GetComponentsInChildren<Image>().Where(i=>i.name=="PaintedArtwork"))
                {
                    Assert.IsNotNull(art.sprite,"Owner-supplied button failed to load");
                    Assert.True(art.preserveAspect);
                    Assert.AreEqual(art.transform.localScale.x,art.transform.localScale.y);
                }
                var button=Find("SettingsButton");var hit=(RectTransform)button.transform;
                var position=hit.anchoredPosition;var bounds=hit.sizeDelta;
                TumbangPreso.Settings.SettingsStore.Current.ReducedUiMotion=false;
                button.OnSelect(new BaseEventData(EventSystem.current));
                yield return new WaitForSecondsRealtime(.35f);
                var painted=button.transform.Find("PaintedArtwork");
                Assert.Greater(painted.localScale.x,1);Assert.AreEqual(painted.localScale.x,painted.localScale.y);
                Assert.AreEqual(position,hit.anchoredPosition);Assert.AreEqual(bounds,hit.sizeDelta);
                button.OnPointerDown(new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left});
                yield return new WaitForSecondsRealtime(.2f);
                Assert.Less(painted.localScale.x,1);Assert.AreEqual(painted.localScale.x,painted.localScale.y);
                button.OnPointerUp(new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left});
                button.OnDeselect(new BaseEventData(EventSystem.current));
                var dust=canvas.GetComponentInChildren<OwnerRoadDust>();
                yield return TumpUiCapture.Capture("OwnerMenu-v7-dust",canvas,1920,1080,false);
                var mesh=dust.canvasRenderer.GetMesh();Assert.Greater(mesh.vertexCount,0);
                TumbangPreso.Settings.SettingsStore.Current.ReducedUiMotion=true;
                yield return null;yield return null;Canvas.ForceUpdateCanvases();
                Assert.AreEqual(0,dust.canvasRenderer.GetMesh().vertexCount,"Reduced motion must remove drifting dust");
                button.onClick.Invoke();yield return null;yield return null;
                Assert.False(canvas.gameObject.activeInHierarchy,"Settings must open from the real painted button");
                Assert.IsNotNull(Object.FindFirstObjectByType<ConvertedSettingsPanel>());
                Find("SettingsCredits").onClick.Invoke();yield return null;yield return null;
                Assert.IsNotNull(GameObject.Find("OwnerCreditsCanvas"));
                Find("CreditsBack").onClick.Invoke();yield return null;
                Find("TumpSettingsBack").onClick.Invoke();yield return null;
                Assert.True(canvas.gameObject.activeInHierarchy);
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
                var fields=canvas.GetComponentsInChildren<InputField>();
                Assert.IsFalse(fields.Any(f=>f.name=="Email"));
                var confirmation=fields.First(f=>f.name=="ConfirmPassword");
                fields.First(f=>f.name=="Username").text="local.validation";
                fields.First(f=>f.name=="Password").text="test-only";
                confirmation.text="different";Find("SubmitAccount").onClick.Invoke();yield return null;
                StringAssert.Contains("do not match",canvas.GetComponentsInChildren<Text>().First(t=>t.name=="AccountStatus").text);
                Find("RevealConfirmation").onClick.Invoke();Assert.AreEqual(InputField.ContentType.Standard,confirmation.contentType);
                Find("RevealConfirmation").onClick.Invoke();Assert.AreEqual(InputField.ContentType.Password,confirmation.contentType);
                var divider=canvas.GetComponentsInChildren<RectTransform>().First(t=>t.name=="AccountDivider");
                var left=(RectTransform)divider.Find("LeftDivider");var right=(RectTransform)divider.Find("RightDivider");
                Assert.AreEqual(left.sizeDelta,right.sizeDelta);Assert.AreEqual(left.anchoredPosition.y,right.anchoredPosition.y);
                fields.First(f=>f.name=="Username").text="";fields.First(f=>f.name=="Password").text="";
                Find("SignInTab").onClick.Invoke();yield return null;
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
