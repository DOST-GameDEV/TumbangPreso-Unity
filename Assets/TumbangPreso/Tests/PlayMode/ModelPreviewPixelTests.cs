using System.Collections;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class ModelPreviewPixelTests
    {
        [UnitySetUp] public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After()=>PlayModeWorld.Reset();

        [UnityTest]
        public IEnumerator ContinuousResizeReusesTheTargetAndSettlesAtFullPanelResolution()
        {
            var owner = new GameObject("Resize ownership");
            var canvas = OwnerUiLayout.Canvas(owner.transform, "ResizeCanvas", 100);
            var panel = OwnerUiLayout.Rect(canvas.transform, "ResizePreview");
            OwnerUiLayout.Place(panel, 50, 50, 600, 400);
            var preview = panel.gameObject.AddComponent<ModelPreview>(); preview.Attach(panel);
            Canvas.ForceUpdateCanvases(); preview.StepForCapture();
            var original = preview.Target;
            Assert.IsNotNull(original);
            try
            {
                for (int i = 0; i < 8; i++)
                {
                    panel.sizeDelta = new Vector2(620 + 20 * i, 400);
                    Canvas.ForceUpdateCanvases();
                    yield return null;
                    Assert.AreSame(original, preview.Target, "Every resize pixel allocated another target.");
                    Assert.AreEqual(panel.rect.width / panel.rect.height, preview.PreviewCamera.aspect, .015f);
                }
                yield return new WaitForSecondsRealtime(.2f);
                Assert.AreNotSame(original, preview.Target);
                var settled = preview.Target;
                var corners = new Vector3[4]; panel.GetWorldCorners(corners);
                float pixels = Vector2.Distance(RectTransformUtility.WorldToScreenPoint(null, corners[0]),
                    RectTransformUtility.WorldToScreenPoint(null, corners[3]));
                Assert.AreEqual(pixels, settled.width, 2);
                yield return null;
                Assert.AreSame(settled, preview.Target, "An unchanged panel allocated again after settling.");
                panel.sizeDelta = new Vector2(810, 450); Canvas.ForceUpdateCanvases();
                preview.StepForCapture();
                Assert.AreNotSame(settled, preview.Target, "An explicit capture did not settle its requested resolution.");
            }
            finally { Object.Destroy(owner); }
        }

        [UnityTest]
        public IEnumerator PreviewTargetTracksPhysicalCanvasPixelsWithoutChangingAspect()
        {
            var owner=new GameObject("PreviewPixelReview");
            var cameraObject=new GameObject("PreviewPixelCanvasCamera");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
            var canvas=OwnerUiLayout.Canvas(owner.transform,"PreviewPixelCanvas",100);
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10;
            var panel=OwnerUiLayout.Rect(canvas.transform,"MeasuredPreview");OwnerUiLayout.Place(panel,50,50,600,400);
            var preview=panel.gameObject.AddComponent<ModelPreview>();preview.Attach(panel);
            RenderTexture output=null;
            try
            {
                foreach(int width in new[]{1920,3840,1280})
                {
                    if(output!=null){camera.targetTexture=null;output.Release();Object.Destroy(output);}
                    output=new RenderTexture(width,width*9/16,24);camera.targetTexture=output;
                    Canvas.ForceUpdateCanvases();yield return null;yield return null;preview.StepForCapture();
                    var corners=new Vector3[4];panel.GetWorldCorners(corners);
                    float physicalWidth=Vector2.Distance(camera.WorldToScreenPoint(corners[0]),camera.WorldToScreenPoint(corners[3]));
                    float physicalHeight=Vector2.Distance(camera.WorldToScreenPoint(corners[0]),camera.WorldToScreenPoint(corners[1]));
                    Assert.AreEqual(physicalWidth,preview.Target.width,2,"Preview resolution ignored the real display scale.");
                    Assert.AreEqual(physicalHeight,preview.Target.height,2);
                    Assert.AreEqual(1.5f,preview.PreviewCamera.aspect,.01f);
                }
                panel.sizeDelta=new Vector2(4500,3000);yield return null;yield return null;preview.StepForCapture();
                Assert.AreEqual(2048,preview.Target.width,"Keep the existing memory cap.");
                Assert.AreEqual(1.5f,preview.PreviewCamera.aspect,.01f,"Clamp both axes with one factor.");
            }
            finally
            {
                camera.targetTexture=null;if(output!=null){output.Release();Object.Destroy(output);}
                Object.Destroy(owner);Object.Destroy(cameraObject);
            }
        }
    }
}
