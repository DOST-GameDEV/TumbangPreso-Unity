using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class DeferredHeroViewTests
    {
        private Settings.GameSettings _settings;
        [UnitySetUp]public IEnumerator Before()
        {
            _settings=Settings.SettingsStore.Current;yield return PlayModeWorld.Reset();GameServices.Ensure();
            Settings.SettingsStore.OverrideForTests(new Settings.GameSettings{PlayerName="Deferred hero control"});
            yield return HubFlowTests.OpenHome();
        }
        [UnityTearDown]public IEnumerator After()
        {yield return PlayModeWorld.Reset();Settings.SettingsStore.OverrideForTests(_settings);}
        private static IEnumerator Ready(TumpHub hub)
        {
            int frames=0;while(!(hub.Top is HubHero)&&frames++<40)yield return null;
            Assert.IsInstanceOf<HubHero>(hub.Top);Assert.Greater(frames,2);
        }
        [UnityTest]public IEnumerator HomeStaysVisibleUntilCompleteHeroAndThenFocusesPrimary()
        {
            var hub=TumpHub.Current;var home=hub.Top;string settings=JsonUtility.ToJson(Settings.SettingsStore.Current);
            var hero=hub.Push<HubHero>();Assert.AreSame(home,hub.Top);Assert.IsTrue(home.gameObject.activeInHierarchy);
            Assert.IsTrue(hero.gameObject.activeInHierarchy);
            yield return null;Assert.AreSame(home,hub.Top);Assert.IsTrue(hub.ShowingHome);
            var groups=hero.GetComponentsInParent<CanvasGroup>();
            Assert.IsTrue(groups.Any(g=>g.alpha==0&&!g.interactable&&!g.blocksRaycasts));
            foreach(var graphic in hero.GetComponentsInChildren<Graphic>())
                Assert.AreEqual(0,graphic.canvasRenderer.GetInheritedAlpha(),.0001f,"Preparing graphics must stay invisible.");
            var hits=new System.Collections.Generic.List<RaycastResult>();
            foreach(var raycaster in hub.Canvas.GetComponentsInChildren<GraphicRaycaster>())
                raycaster.Raycast(new PointerEventData(EventSystem.current){position=new Vector2(Screen.width/2f,Screen.height/2f)},hits);
            Assert.IsFalse(hits.Any(h=>h.gameObject.transform.IsChildOf(hero.transform)),"Preparing graphics must not take Home pointer input.");
            var preview=hero.GetComponentInChildren<ModelPreview>(true);
            if(preview!=null)
            {
                var camera=(Camera)typeof(ModelPreview).GetField("_camera",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(preview);
                Assert.IsNotNull(camera);Assert.IsFalse(camera.enabled);
                Assert.IsFalse(preview.enabled);
            }
            yield return Ready(hub);
            Assert.IsTrue(hero.gameObject.activeInHierarchy);Assert.IsFalse(home.gameObject.activeInHierarchy);
            Assert.IsNotNull(hero.GetComponentsInChildren<Button>().Single(b=>b.name=="HeroPrimary"));
            Assert.AreEqual("HeroPrimary",EventSystem.current.currentSelectedGameObject.name);
            Assert.IsTrue(hero.GetComponentInChildren<ModelPreview>().enabled);
            Assert.IsTrue(hero.GetComponentsInChildren<HubSlap>(true).Any(s=>s.enabled),"Entrance animation must restart on reveal.");
            Assert.AreEqual(settings,JsonUtility.ToJson(Settings.SettingsStore.Current));
        }
        [UnityTest]public IEnumerator BackCancelsPendingHeroWithoutLeavingItsPreview()
        {
            var hub=TumpHub.Current;var home=hub.Top;int before=Object.FindObjectsByType<ModelPreview>(FindObjectsInactive.Include).Length;
            var hero=hub.Push<HubHero>();yield return null;yield return null;hub.Back();
            for(int n=0;n<12;n++)yield return null;
            Assert.AreSame(home,hub.Top);Assert.IsTrue(home.gameObject.activeInHierarchy);Assert.IsTrue(hero==null);
            Assert.AreEqual(before,Object.FindObjectsByType<ModelPreview>(FindObjectsInactive.Include).Length);
            Assert.IsFalse(Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include).Any(g=>g.name=="Preparing hub view"));
        }
        [UnityTest]public IEnumerator AnotherRouteCancelsPreparationAndRepeatedHeroPressDoesNotDuplicate()
        {
            var hub=TumpHub.Current;var first=hub.Push<HubHero>();Assert.AreSame(first,hub.Push<HubHero>());
            yield return null;var menu=hub.Push<HubMenu>();
            for(int n=0;n<12;n++)yield return null;
            Assert.AreSame(menu,hub.Top);Assert.IsTrue(first==null);hub.Home();
            var second=hub.Push<HubHero>(h=>h.ShopMode=true);yield return Ready(hub);
            Assert.AreSame(second,hub.Top);Assert.IsTrue(second.ShopMode);
            Assert.AreEqual(1,hub.Canvas.GetComponentsInChildren<HubHero>(true).Length);
        }
    }
}
