using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class HiddenPreviewFrameTests
    {
        [UnitySetUp]public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown]public IEnumerator After()=>PlayModeWorld.Reset();
        [UnityTest]public IEnumerator HiddenModelBindingAndFirstRenderRestoreAmbientAndKeepCameraDisabled()
        {
            yield return RosterBook.Warmup();
            var owner=new GameObject("Owned hidden preview",typeof(RectTransform),typeof(Canvas));owner.SetActive(false);
            var panel=new GameObject("Panel",typeof(RectTransform));panel.transform.SetParent(owner.transform,false);
            ((RectTransform)panel.transform).sizeDelta=new Vector2(640,800);
            var mode=RenderSettings.ambientMode;var colour=RenderSettings.ambientLight;float intensity=RenderSettings.ambientIntensity;
            try
            {
                var preview=panel.AddComponent<ModelPreview>();preview.Attach((RectTransform)panel.transform);
                var art=RosterBook.Load().FindPersonArt("dante");Assert.IsNotNull(art);
                preview.Show(art.Model,art.Clips,art.Palette,art.PetModel);
                Assert.AreEqual(mode,RenderSettings.ambientMode);Assert.AreEqual(colour,RenderSettings.ambientLight);Assert.AreEqual(intensity,RenderSettings.ambientIntensity);
                yield return preview.PrepareHiddenFrame();
                var flags=BindingFlags.NonPublic|BindingFlags.Instance;
                var camera=(Camera)typeof(ModelPreview).GetField("_camera",flags).GetValue(preview);
                var texture=(RenderTexture)typeof(ModelPreview).GetField("_texture",flags).GetValue(preview);
                Assert.IsNotNull(texture);Assert.IsTrue(texture.IsCreated());Assert.IsFalse(camera.enabled);
                Assert.AreEqual(mode,RenderSettings.ambientMode);Assert.AreEqual(colour,RenderSettings.ambientLight);Assert.AreEqual(intensity,RenderSettings.ambientIntensity);
                var previous=RenderTexture.active;RenderTexture.active=texture;
                var pixels=new Texture2D(texture.width,texture.height,TextureFormat.RGBA32,false);
                try
                {
                    pixels.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);pixels.Apply();
                    int visible=0;foreach(var pixel in pixels.GetPixels32())if(pixel.a>32)visible++;
                    Assert.Greater(visible,texture.width*texture.height/100,"The actual owned hidden render must contain the model.");
                }
                finally{RenderTexture.active=previous;Object.Destroy(pixels);}
                owner.SetActive(true);yield return null;Assert.IsTrue(camera.enabled);
                Assert.AreEqual(ModelPreview.PreviewAmbient,RenderSettings.ambientLight);
                owner.SetActive(false);yield return null;
                Assert.AreEqual(mode,RenderSettings.ambientMode);Assert.AreEqual(colour,RenderSettings.ambientLight);Assert.AreEqual(intensity,RenderSettings.ambientIntensity);
            }
            finally{Object.DestroyImmediate(owner);}
        }
    }
}
