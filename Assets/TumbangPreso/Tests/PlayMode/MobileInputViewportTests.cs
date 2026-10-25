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
    }
}
