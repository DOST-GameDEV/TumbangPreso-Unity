using System.Collections;
using NUnit.Framework;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class AttachedEffectVisibilityTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        [UnityTest] public IEnumerator AuraOnPreviewLayerIsVisibleToTheHostsCamera()
        {
            var host = new GameObject("Layered effect host"); host.layer = TumbangPreso.UI.ModelPreview.PreviewLayer;
            var cameraGo = new GameObject("Scoped effect camera"); var camera = cameraGo.AddComponent<Camera>();
            camera.enabled = false; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            camera.cullingMask = 1 << host.layer; camera.transform.position = new Vector3(0, .9f, -4);
            var target = new RenderTexture(256, 256, 24); target.Create(); camera.targetTexture = target;
            var aura = AbilityVfx.AttachAura(host.transform, AbilityVfx.Aura.ElectricSpark, 2);
            try
            {
                var particles = aura.GetComponent<ParticleSystem>(); var renderer = aura.GetComponent<ParticleSystemRenderer>();
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particles.useAutoRandomSeed = false; particles.randomSeed = 9; particles.Simulate(.3f, true, true, true);
                Assert.Greater(particles.particleCount, 0); Assert.IsNotNull(renderer.sharedMaterial);
                Assert.IsTrue(renderer.sharedMaterial.shader.isSupported);
                camera.Render(); var old = RenderTexture.active; RenderTexture.active = target;
                var pixels = new Texture2D(256, 256, TextureFormat.RGB24, false);
                int lit = 0;
                try
                {
                    pixels.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); pixels.Apply();
                    foreach (var pixel in pixels.GetPixels32()) if (pixel.r + pixel.g + pixel.b > 12) lit++;
                }
                finally { RenderTexture.active = old; Object.Destroy(pixels); }
                Assert.AreEqual(host.layer, aura.layer, "Attached effect did not inherit its host's rendering layer.");
                Assert.Greater(lit, 2, "The real particle renderer was culled from the scoped preview camera.");
            }
            finally
            {
                camera.targetTexture = null; Object.DestroyImmediate(cameraGo); Object.DestroyImmediate(host);
                target.Release(); Object.Destroy(target);
            }
            yield return null;
        }
    }
}
