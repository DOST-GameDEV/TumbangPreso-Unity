using System.Collections;
using TumbangPreso.UI.Hub;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class UiEdgeQualityReview
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [UnityTest]
        public IEnumerator ActualHomeControlsRenderAt1080pQhdAnd4k()
        {
            yield return HubFlowTests.OpenHome();
            foreach (var size in new[] { new Vector2Int(1920,1080), new Vector2Int(2560,1440), new Vector2Int(3840,2160) })
                yield return TumpUiCapture.Capture("UI-edge-quality-" + size.x + "x" + size.y,
                    TumpHub.Current.Canvas, size.x, size.y, false, checkActionBounds:true);
            Assert.IsInstanceOf<HubHome>(TumpHub.Current.Top);
        }
    }
}
