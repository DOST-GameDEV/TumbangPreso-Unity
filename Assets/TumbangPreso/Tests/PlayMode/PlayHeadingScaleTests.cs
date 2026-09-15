using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class PlayHeadingScaleTests
    {
        [UnitySetUp] public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After()=>PlayModeWorld.Reset();
        [UnityTest]
        public IEnumerator PlayHeadingRemainsVisibleAcrossPcSizes()
        {
            var owner=new GameObject("PlayHeadingReview");owner.AddComponent<CourtPlayView>().Build(owner.transform);
            var canvas=GameObject.Find("OwnerPlayCanvas").GetComponent<Canvas>();
            var title=canvas.GetComponentsInChildren<Text>().First(t=>t.name=="Heading");
            var cameraObject=new GameObject("HeadingReviewCamera");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
            RenderTexture target=null;
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10;
            try
            {
                Directory.CreateDirectory("Logs/play-heading-review");
                foreach(var size in TumpUiCapture.PcViewports)
                {
                    if(target!=null){camera.targetTexture=null;target.Release();Object.Destroy(target);}
                    target=new RenderTexture(size.x,size.y,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;
                    Canvas.ForceUpdateCanvases();yield return null;yield return null;
                    camera.Render();
                    int count=title.cachedTextGenerator.characterCountVisible;
                    Debug.Log("[HeadingScale] size="+size+" height="+title.rectTransform.rect.height+" scale="+canvas.scaleFactor+" preferred="+title.preferredHeight+
                        " adjusted="+title.GetPixelAdjustedRect()+" characters="+count+" vertices="+title.canvasRenderer.GetMesh()?.vertexCount);
                    var previous=RenderTexture.active;RenderTexture.active=target;
                    var pixels=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);
                    pixels.ReadPixels(new Rect(0,0,size.x,size.y),0,0);pixels.Apply();
                    File.WriteAllBytes("Logs/play-heading-review/fixed-"+size.x+"x"+size.y+".png",pixels.EncodeToPNG());Object.Destroy(pixels);RenderTexture.active=previous;
                    Assert.GreaterOrEqual(count,title.text.Length,"The actual Play heading disappeared at "+size);
                    Assert.Greater(title.canvasRenderer.GetMesh().vertexCount,0);
                }
            }
            finally{camera.targetTexture=null;if(target!=null){target.Release();Object.Destroy(target);}Object.Destroy(owner);Object.Destroy(cameraObject);}
        }
    }
}
