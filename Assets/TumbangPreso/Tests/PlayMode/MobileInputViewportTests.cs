using System.Collections;
using NUnit.Framework;
using TumbangPreso.InputLayer;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class MobileInputViewportTests
    {
        [UnityTest] public IEnumerator TouchSettingsHaveWorkingSwipeControlsAndCutoutInsets()
        {
            yield return PlayModeWorld.Reset();
            string saved=JsonUtility.ToJson(Settings.SettingsStore.Current);
            var root=new GameObject("Touch settings journey");
            var view=root.AddComponent<TumpSettingsView>();
            try
            {
                view.Open(root.transform,()=>{},()=>{},()=>{});
                typeof(TumpSettingsView).GetField("_device",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)
                    .SetValue(view,InputDeviceKind.Touch);
                view.ShowSection(0);yield return null;
                var canvas=(Canvas)typeof(TumpSettingsView).GetField("_canvas",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(view);
                var sensitivity=System.Array.Find(Object.FindObjectsByType<UnityEngine.UI.Slider>(FindObjectsInactive.Exclude),
                    slider=>slider.name=="TouchSensitivityValue");
                Assert.IsNotNull(sensitivity,"The touch page must expose the swipe setting used by actual look input.");
                sensitivity.value=2.5f;
                Assert.AreEqual(2.5f,Settings.SettingsStore.Current.MouseSensitivity,.001f);
                var invert=System.Array.Find(Object.FindObjectsByType<UnityEngine.UI.Toggle>(FindObjectsInactive.Exclude),
                    toggle=>toggle.name=="TouchInvertYValue");
                Assert.IsNotNull(invert);invert.isOn=!invert.isOn;
                Assert.AreEqual(invert.isOn,Settings.SettingsStore.Current.InvertY);
                var safe=canvas.GetComponentInChildren<UiSafeArea>();Assert.IsNotNull(safe);
                safe.Fit(new Rect(90,24,2220,1056),new Vector2(2400,1080));
                Canvas.ForceUpdateCanvases();
                var design=(RectTransform)safe.transform.GetChild(0);safe.KeepDesignInside(design);
                Assert.AreEqual(90f/2400,((RectTransform)safe.transform).anchorMin.x,.0001f);
                Assert.LessOrEqual(design.rect.width*design.localScale.x,((RectTransform)safe.transform).rect.width+.01f);
                Assert.LessOrEqual(design.rect.height*design.localScale.y,((RectTransform)safe.transform).rect.height+.01f);
                Assert.IsTrue(view.Session.Dirty);
                view.Session.Discard();
                Assert.AreEqual(JsonUtility.FromJson<Settings.GameSettings>(saved).MouseSensitivity,Settings.SettingsStore.Current.MouseSensitivity);
            }
            finally { Object.DestroyImmediate(root);Settings.SettingsStore.Restore(JsonUtility.FromJson<Settings.GameSettings>(saved)); }
            yield return PlayModeWorld.Reset();
        }
        [TestCase(960)] [TestCase(1200)] [TestCase(2400)]
        public void EqualScreenFractionProducesEqualLook(float width)
        {
            Assert.Less(Vector2.Distance(new Vector2(60,30),TouchLookArea.LookUnits(new Vector2(width/3,width/6),width)),.0001f);
        }
        [UnityTest] public IEnumerator ForeignPointerCannotMoveOrReleaseLookAndGlobalCancelRetiresIt()
        {
            yield return PlayModeWorld.Reset();
            var root=new GameObject("Look ownership",typeof(RectTransform));var look=root.AddComponent<TouchLookArea>();
            var owner=new PointerEventData(EventSystem.current){pointerId=4,delta=Vector2.right*20};
            var other=new PointerEventData(EventSystem.current){pointerId=8,delta=Vector2.up*200};
            try
            {
                look.OnPointerDown(owner);look.OnPointerDown(other);look.OnDrag(other);Assert.AreEqual(Vector2.zero,TouchInput.LookDelta);
                look.OnDrag(owner);var value=TouchInput.LookDelta;Assert.Greater(value.x,0);
                look.OnPointerUp(other);Assert.AreEqual(value,TouchInput.LookDelta);
                TouchInput.ReleaseAll();look.OnDrag(owner);Assert.AreEqual(Vector2.zero,TouchInput.LookDelta);
                look.OnPointerDown(other);look.OnDrag(other);Assert.Greater(TouchInput.LookDelta.y,0);
                look.enabled=false;Assert.AreEqual(Vector2.zero,TouchInput.LookDelta);
            }
            finally { Object.DestroyImmediate(root);TouchInput.ReleaseAll(); }
        }
        [UnityTest] public IEnumerator CutoutInsetsEdgeContentWithoutMovingTheAimCentre()
        {
            yield return PlayModeWorld.Reset();
            var owner=new GameObject("Safe viewport fixture");var hud=owner.AddComponent<Hud>();
            var readout=hud.GetComponent<TumpMatchReadout>();var canvas=readout.Canvas;
            var safe=canvas.GetComponentInChildren<UiSafeArea>();
            try
            {
                Canvas.ForceUpdateCanvases();safe.Fit(new Rect(100,30,2160,1050),new Vector2(2400,1080));Canvas.ForceUpdateCanvases();
                var rect=(RectTransform)safe.transform;
                Assert.AreEqual(100f/2400,rect.anchorMin.x,.0001f);Assert.AreEqual(30f/1080,rect.anchorMin.y,.0001f);
                Assert.AreEqual(2260f/2400,rect.anchorMax.x,.0001f);
                var aim=canvas.GetComponentInChildren<HudReticle>();Assert.AreSame(canvas.transform,aim.transform.parent);
                Assert.AreEqual(new Vector2(.5f,.5f),aim.rectTransform.anchorMin);
            }
            finally { Object.DestroyImmediate(owner); }
            yield return PlayModeWorld.Reset();
        }
        [UnityTest] public IEnumerator CurrentKitsUseDragAimOnlyForTheirActualHeldAbilitiesInBothRoles()
        {
            yield return PlayModeWorld.Reset();GameServices.Ensure();bool active=TouchInput.Active;TouchInput.Active=true;
            var body=new GameObject("Mobile current kit policy",typeof(CharacterController));
            var actor=body.AddComponent<CharacterMotor>();actor.enabled=false;actor.PlayerSlot=1;actor.IsPerson=true;
            var system=actor.AbilitySystem??body.AddComponent<Abilities.HeroAbilitySystem>();system.enabled=false;
            var face=new GameObject("Current skill touch",typeof(RectTransform),typeof(CanvasGroup));
            var painter=face.AddComponent<OwnerTouchSurface>();var button=face.AddComponent<TouchButton>();
            var pointer=new PointerEventData(EventSystem.current){pointerId=7,delta=Vector2.right*24};
            int aimed=0,immediate=0;
            try
            {
                foreach(var hero in Core.Roster.HeroPeople)
                {
                    system.BindHero(hero.Id);
                    foreach(bool defending in new[]{false,true})
                    {
                        system.Kit.SetRole(defending,new Abilities.AbilityContext(actor,null,null));
                        foreach(var verb in new[]{Verb.Skill1,Verb.Skill2,Verb.Ultimate})
                        {
                            var ability=verb==Verb.Skill1?system.Kit.Skill1:verb==Verb.Skill2?system.Kit.Skill2:system.Kit.Ultimate;
                            button.BindOwnerPresentation(InputCatalogue.For(verb),face.GetComponent<CanvasGroup>(),painter,null,null);
                            button.RefreshIcon(actor);TouchInput.ReleaseAll();button.OnPointerDown(pointer);button.OnDrag(pointer);
                            Assert.IsTrue(TouchInput.Pressed(verb));
                            if(ability!=null&&ability.HoldToAim){aimed++;Assert.Greater(TouchInput.LookDelta.x,0,hero.Id+" "+verb+" role="+defending);}
                            else {immediate++;Assert.AreEqual(Vector2.zero,TouchInput.LookDelta,hero.Id+" "+verb+" role="+defending);}
                            button.OnPointerUp(pointer);Assert.IsFalse(TouchInput.Pressed(verb));
                        }
                    }
                }
                Assert.Greater(aimed,0);Assert.Greater(immediate,0);
            }
            finally { Object.DestroyImmediate(face);Object.DestroyImmediate(body);TouchInput.ReleaseAll();TouchInput.Active=active; }
            yield return PlayModeWorld.Reset();
        }
    }
}
