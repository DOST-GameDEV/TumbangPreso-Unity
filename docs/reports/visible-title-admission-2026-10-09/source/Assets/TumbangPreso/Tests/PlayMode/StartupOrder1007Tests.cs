using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class StartupOrder1007Tests
    {
        private Settings.GameSettings _settings;
        private bool _boot,_offered;
        [UnitySetUp] public IEnumerator Before()
        {
            _settings=Settings.SettingsStore.Current; _boot=SceneFlow.BootedThroughSplash; _offered=SceneFlow.LoginStepOffered;
            yield return PlayModeWorld.Reset(); GameServices.Ensure();
            Settings.SettingsStore.OverrideForTests(new Settings.GameSettings{PlayerName="Startup order",AccountHasPassword=false,ReducedUiMotion=false});
            SceneFlow.BootedThroughSplash=false; SceneFlow.LoginStepOffered=false;
            HubSceneVideo.ForcedHero="phaister";
        }
        [UnityTearDown] public IEnumerator After()
        {
            HubSceneVideo.ForcedHero=null;
            yield return PlayModeWorld.Reset(); Settings.SettingsStore.OverrideForTests(_settings);
            SceneFlow.BootedThroughSplash=_boot; SceneFlow.LoginStepOffered=_offered;
        }

        [UnityTest]
        public IEnumerator LogosLoginFiveSecondMainThenFirstHomeAnimation()
            => Sequence(false);

        [UnityTest]
        public IEnumerator SlowAdmissionStillLeavesFiveVisibleSecondsBeforeFirstHome()
            => Sequence(true);

        private IEnumerator Sequence(bool slowAdmission)
        {
            yield return SceneManager.LoadSceneAsync(SceneFlow.Splash);
            bool studio=false; float until=Time.realtimeSinceStartup+40;
            while(SceneManager.GetActiveScene().name!=SceneFlow.MainMenu && Time.realtimeSinceStartup<until)
            {
                studio|=GameObject.Find("StudioIntroCanvas")!=null;
                Assert.IsNull(GameObject.Find("OwnerLoadingCanvas"),"Retired illustrated loading must never precede login");
                Assert.IsNull(GameObject.Find("SplashCanvas"),"Retired splash loading surface");
                yield return null;
            }
            Assert.IsTrue(studio,"BH Studios intro must remain before login");
            Assert.AreEqual(SceneFlow.MainMenu,SceneManager.GetActiveScene().name);
            var login=Object.FindFirstObjectByType<SignInScreen>(); Assert.IsNotNull(login); Assert.IsTrue(login.IsOpen);
            if(slowAdmission)
                login.Closed += () => System.Threading.Thread.Sleep(500);
            var loginCanvas=GameObject.Find("OwnerSignInCanvas").GetComponent<Canvas>();
            var title=Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include).Single(c=>c.name=="OwnerHomeCanvas");
            Assert.IsFalse(title.gameObject.activeInHierarchy,"Main loading view must not precede login");
            foreach(int width in new[]{1920,1280})
                yield return TumpUiCapture.Capture("StartupLoginFirst1007-"+width,loginCanvas,width,width==1920?1080:720,false);
            var guest=loginCanvas.GetComponentsInChildren<Button>().Single(b=>b.name=="GuestAccount");
            var eventSystem=EventSystem.current; var rect=(RectTransform)guest.transform;
            var click=new PointerEventData(eventSystem){button=PointerEventData.InputButton.Left,
                position=RectTransformUtility.WorldToScreenPoint(loginCanvas.worldCamera,rect.TransformPoint(rect.rect.center))};
            Canvas.ForceUpdateCanvases();var hits=new System.Collections.Generic.List<RaycastResult>();eventSystem.RaycastAll(click,hits);
            Assert.IsNotEmpty(hits); var receiver=ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
            Assert.AreEqual(guest.gameObject,receiver);ExecuteEvents.Execute(receiver,click,ExecuteEvents.pointerClickHandler);
            until=Time.realtimeSinceStartup+20;
            while(login.IsOpen && Time.realtimeSinceStartup<until) yield return null;
            Assert.IsFalse(login.IsOpen);
            float titleBegan=Time.realtimeSinceStartup;
            Assert.IsTrue(title.gameObject.activeInHierarchy,"Main/title loading must be visibly shown after login");
            foreach(int width in new[]{1920,1280})
                yield return TumpUiCapture.Capture("StartupMainLoading1007-"+width,title,width,width==1920?1080:720,false);
            until=Time.realtimeSinceStartup+60;
            while(SceneManager.GetActiveScene().name!=SceneFlow.MatchSetup && Time.realtimeSinceStartup<until) yield return null;
            Assert.AreEqual(SceneFlow.MatchSetup,SceneManager.GetActiveScene().name);
            Assert.GreaterOrEqual(Time.realtimeSinceStartup-titleBegan,4.95f,"Main/title must remain visible at least five seconds");
            until=Time.realtimeSinceStartup+15;
            while((TumpHub.Current==null || !TumpHub.Current.ShowingHome) && Time.realtimeSinceStartup<until) yield return null;
            Assert.IsTrue(TumpHub.Current.ShowingHome);
            var video=Object.FindFirstObjectByType<HubSceneVideo>(); Assert.IsNotNull(video);
            until=Time.realtimeSinceStartup+15;
            while((!video.ShowingVideo || !video.Player.isPlaying) && Time.realtimeSinceStartup<until) yield return null;
            Assert.IsTrue(video.ShowingVideo); Assert.IsTrue(video.Player.isPlaying);
            long frame=video.Player.frame; double time=video.Player.time;
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.Greater(video.Player.frame,frame+2,"First Home arrival must advance decoded frames without navigating away");
            Assert.Greater(video.Player.time,time+.3,"First arrival playback time must advance");
            Debug.Log("[StartupOrder1007] studio->login->main-loading->home; loadingSeconds="+(Time.realtimeSinceStartup-titleBegan)+" firstFrames="+frame+"->"+video.Player.frame);
            foreach(int width in new[]{1920,1280})
                yield return TumpUiCapture.Capture("StartupFirstHomePlaying1007-"+width,TumpHub.Current.Canvas,width,width==1920?1080:720,false);
        }
    }
}
