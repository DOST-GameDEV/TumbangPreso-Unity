using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class RoomArrowChoiceTests
    {
        [UnitySetUp] public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After()=>PlayModeWorld.Reset();

        [UnityTest]
        public IEnumerator RoomChoicesCycleInPlaceThroughPointerAndSubmit()
        {
            string map=SceneFlow.SelectedMap; var mode=SceneFlow.SelectedMode;
            try
            {
                yield return HubFlowTests.OpenHome();
                var hub=TumpHub.Current; var host=hub.Push<HubHost>();
                yield return new WaitForSecondsRealtime(.5f); Canvas.ForceUpdateCanvases();
                Assert.IsEmpty(host.GetComponentsInChildren<HubDropdown>(),"Room choices must not open lists");
                var choices=host.GetComponentsInChildren<HubArrowChoice>(); Assert.AreEqual(3,choices.Length);
                CollectionAssert.AreEqual(SceneFlow.MapRegistry.Select(x=>x.Name),choices.Single(x=>x.name=="MapChoice").Options);
                CollectionAssert.AreEqual(new[]{"CLASSIC","HERO STRIKE"},choices.Single(x=>x.name=="ModeChoice").Options);
                CollectionAssert.AreEqual(new[]{"PUBLIC","FRIENDS ONLY","PRIVATE"},choices.Single(x=>x.name=="VisibilityChoice").Options);
                foreach(var choice in choices)
                {
                    int callbackCount=0; choice.Changed+=_=>callbackCount++;
                    choice.Set(0,true); callbackCount=0;
                    int first=choice.Value;
                    for(int i=1;i<=choice.Options.Length;i++)
                    {
                        // Both mouse and touch use the standard pointer-click route.
                        Click(hub.Canvas,choice.Next,i%2==0?3:-1);
                        yield return null;
                        int expected=i%choice.Options.Length;
                        Assert.AreEqual(expected,choice.Value); Assert.AreEqual(i,callbackCount);
                        Assert.AreEqual(choice.Options[expected],choice.GetComponentsInChildren<Text>().Single(x=>x.name=="CurrentValue").text);
                        Assert.AreSame(host,hub.Top,"Arrows must not push a popup");
                        Assert.IsNull(Object.FindFirstObjectByType<HubChoicePopup>());
                        if(choice.name=="MapChoice") Assert.AreEqual(SceneFlow.MapRegistry[expected].Id,SceneFlow.SelectedMap,"Live map callback");
                    }
                    Assert.AreEqual(first,choice.Value,"Wrap to the first option");
                    EventSystem.current.SetSelectedGameObject(choice.Previous.gameObject);
                    ExecuteEvents.Execute(choice.Previous.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);
                    yield return null;
                    Assert.AreEqual(choice.Options.Length-1,choice.Value,"Keyboard/pad Submit follows Previous");
                    var next=choice.Previous.FindSelectableOnRight();
                    Assert.AreSame(choice.Next,next,"Horizontal navigation must reach the matching next arrow");
                    Assert.AreEqual(choice.Options.Length+1,callbackCount,"Each action fires one callback");
                }
                foreach(int width in new[]{1920,1280})
                    yield return TumpUiCapture.Capture("RoomArrows1006-"+width,hub.Canvas,width,width==1920?1080:720,false,checkActionBounds:true);
                hub.Back(); yield return null;
                Assert.IsFalse(hub.Top is HubHost,"Back must leave the form normally");
            }
            finally
            {
                SceneFlow.SelectedMap=map; SceneFlow.SelectedMode=mode;
            }
        }

        private static void Click(Canvas canvas,Button button,int pointer)
        {
            Canvas.ForceUpdateCanvases();
            var rect=(RectTransform)button.transform;
            var data=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,pointerId=pointer,
                position=RectTransformUtility.WorldToScreenPoint(canvas.worldCamera,rect.TransformPoint(rect.rect.center))};
            var hits=new System.Collections.Generic.List<RaycastResult>(); EventSystem.current.RaycastAll(data,hits);
            Assert.IsNotEmpty(hits,button.name+" has no pointer hit");
            var receiver=ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
            Assert.AreEqual(button.gameObject,receiver,button.name+" hit is covered by another control");
            ExecuteEvents.Execute(receiver,data,ExecuteEvents.pointerClickHandler);
        }
    }
}
