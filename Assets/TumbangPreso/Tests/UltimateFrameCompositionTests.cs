using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace TumbangPreso.Tests
{
    public sealed class UltimateFrameCompositionTests
    {
        [TestCase(0f)]
        [TestCase(.25f)]
        [TestCase(1f)]
        public void SharedFrameIsOpaqueButCanvasFadeStillWorks(float sourceAlpha)
        {
            var roundProperty=typeof(GameServices).GetProperty("Round",BindingFlags.Public|BindingFlags.Static);
            var priorRound=GameServices.Round;
            var owner=new GameObject("Composition-only actor");
            var roundRoot=new GameObject("Composition-only round");
            var cameraRoot=new GameObject("Composition UI camera",typeof(Camera));
            var target=new RenderTexture(128,128,24);
            var source=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
            Texture2D result=null;Canvas canvas=null;Material ownedMaterial=null;
            try
            {
                var actor=owner.AddComponent<CharacterMotor>();actor.enabled=false;actor.PlayerSlot=0;
                var round=roundRoot.AddComponent<RoundDirector>();round.enabled=false;round.Register(actor);
                roundProperty.SetValue(null,round);
                // No model or clip is needed to exercise the exact shared frame consumer.
                var view=new UltimatePhaseView(owner.transform,new[]{new UltimateCommit(0,1,Vector3.zero,Vector3.forward,Vector3.forward,0)},3);
                var field=typeof(UltimatePhaseView).GetField("_picture",BindingFlags.Instance|BindingFlags.NonPublic);
                var picture=(RawImage)field.GetValue(view);Assert.IsNotNull(picture);
                ownedMaterial=typeof(UltimatePhaseView).GetField("_pictureMaterial",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(view) as Material;
                canvas=picture.GetComponentInParent<Canvas>();canvas.transform.Find("UltimateIdentity").gameObject.SetActive(false);
                var camera=cameraRoot.GetComponent<Camera>();camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;
                camera.backgroundColor=Color.red;camera.cullingMask=1<<31;camera.enabled=false;
                canvas.GetComponent<CanvasScaler>().enabled=false;
                canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
                foreach(var child in canvas.GetComponentsInChildren<Transform>(true))child.gameObject.layer=31;
                source.SetPixels(new[]{new Color(0,0,1,sourceAlpha),new Color(0,0,1,sourceAlpha),new Color(0,0,1,sourceAlpha),new Color(0,0,1,sourceAlpha)});source.Apply();
                picture.texture=source;picture.color=Color.white;picture.enabled=true;
                var group=canvas.GetComponent<CanvasGroup>();group.alpha=1;
                Canvas.ForceUpdateCanvases();camera.Render();result=Read(target);
                var full=result.GetPixel(64,64);
                TestContext.WriteLine($"source alpha={sourceAlpha}; full frame pixel={full}");
                Assert.Greater(full.b,.9f,"A camera frame must not become see-through because its render texture contains transparency.");
                Assert.Less(full.r,.05f,"The underlying live red view leaked into the full ultimate frame.");
                Object.DestroyImmediate(result);result=null;
                group.alpha=.5f;Canvas.ForceUpdateCanvases();camera.Render();result=Read(target);
                var fade=result.GetPixel(64,64);TestContext.WriteLine($"intentional canvas fade={fade}");
                Assert.Greater(fade.r,.4f);Assert.Greater(fade.b,.4f);
                Assert.That(fade.a,Is.EqualTo(1).Within(.01f),"An opaque underlying frame keeps opaque composed alpha during the fade.");
                Assert.Less(Mathf.Abs(fade.r-fade.b),.1f,"Keep the deliberate canvas handoff fade.");
            }
            finally
            {
                roundProperty.SetValue(null,priorRound);
                if(result!=null)Object.DestroyImmediate(result);
                if(canvas!=null)Object.DestroyImmediate(canvas.gameObject);
                if(ownedMaterial!=null)Object.DestroyImmediate(ownedMaterial);
                cameraRoot.GetComponent<Camera>().targetTexture=null;
                Object.DestroyImmediate(owner);Object.DestroyImmediate(roundRoot);Object.DestroyImmediate(cameraRoot);
                Object.DestroyImmediate(source);target.Release();Object.DestroyImmediate(target);
            }
        }
        private static Texture2D Read(RenderTexture target)
        {
            var prior=RenderTexture.active;
            try{RenderTexture.active=target;var image=new Texture2D(128,128,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,128,128),0,0);image.Apply();return image;}
            finally{RenderTexture.active=prior;}
        }
    }
}
