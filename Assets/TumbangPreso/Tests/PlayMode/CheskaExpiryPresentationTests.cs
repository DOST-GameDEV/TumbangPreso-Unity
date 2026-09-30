using System.Collections;
using System.IO;
using NUnit.Framework;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class CheskaExpiryPresentationTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [UnityTest]
        public IEnumerator NovaKeepsItsSeparateWave()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = Vector3.down;
            var root = FrostSurfacePresentation.Nova(Vector3.zero, 2, renderOnly: true);
            var wave = root.GetComponent<FrostSurfacePresentation>();
            wave.enabled = false;
            try
            {
                foreach (float age in new[] { 0f, .2f, .5f })
                {
                    wave.StepTo(age);
                    foreach (var renderer in root.GetComponentsInChildren<Renderer>())
                        Assert.AreEqual(0, renderer.sharedMaterial.GetFloat("_Thaw"));
                }
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(floor); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator AssignedLifetimeKeepsFullBoundaryAndThawsInPatches()
        {
            int mip = QualitySettings.globalTextureMipmapLimit;
            QualitySettings.globalTextureMipmapLimit = 2;
            GameObject root = null;
            GameObject eye = null;
            RenderTexture target = null;
            try
            {
                yield return MapRetrievalProbe.Load("Eskinita", Core.GameMode.HeroStrike);
                root = new GameObject("Field expiry review");
                root.transform.position = GameServices.Round.Lata.transform.position + new Vector3(-3, 0, -2);
                var field = FrostSurfacePresentation.Build(root.transform, 2.3f, 7.5f);
                field.enabled = false;
                eye = new GameObject("Field expiry review camera");
                var camera = eye.AddComponent<Camera>();
                eye.transform.position = root.transform.position + new Vector3(4, 5, -6);
                eye.transform.LookAt(root.transform.position);
                camera.fieldOfView = 48;
                target = new RenderTexture(960, 540, 24, RenderTextureFormat.ARGB32);
                target.Create(); camera.targetTexture = target;
                var skin = root.transform.Find("FrozenSkin").GetComponent<Renderer>().sharedMaterial;
                var edge = root.transform.Find("FrostEdge").GetComponent<Renderer>().sharedMaterial;
                var position = root.transform.position;
                int fullCoverage = 0, lateCoverage = 0;
                foreach (var child in root.GetComponentsInChildren<Transform>()) child.gameObject.layer = 31;
                Directory.CreateDirectory("Logs/cheska-expiry-captures");
                foreach (float remaining in new[] { 3f, .7f, .65f, .6f, .55f, .5f, .45f, .4f, .35f, .3f, .25f, .2f, .15f, .1f, .05f, 0f })
                {
                    field.StepTo(field.Duration - remaining);
                    yield return null;
                    camera.Render();
                    var previous = RenderTexture.active;
                    var image = new Texture2D(960, 540, TextureFormat.RGB24, false);
                    try
                    {
                        RenderTexture.active = target;
                        image.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); image.Apply();
                        string label = remaining.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
                        File.WriteAllBytes("Logs/cheska-expiry-captures/remaining-" + label + ".png", image.EncodeToPNG());
                    }
                    finally { RenderTexture.active = previous; Object.Destroy(image); }
                    int mask = camera.cullingMask;
                    var flags = camera.clearFlags; var background = camera.backgroundColor;
                    camera.cullingMask = 1 << 31;
                    camera.clearFlags = CameraClearFlags.SolidColor;
                    camera.backgroundColor = Color.black;
                    camera.Render();
                    previous = RenderTexture.active;
                    var pixels = new Texture2D(960, 540, TextureFormat.RGB24, false);
                    try
                    {
                        RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); pixels.Apply();
                        int coverage = 0;
                        foreach (var pixel in pixels.GetPixels32()) if (pixel.g > 8 && pixel.b > 8) coverage++;
                        if (remaining == 3f) fullCoverage = coverage;
                        if (remaining == .2f) lateCoverage = coverage;
                    }
                    finally { RenderTexture.active = previous; Object.Destroy(pixels); camera.cullingMask = mask; camera.clearFlags = flags; camera.backgroundColor = background; }
                    Assert.AreEqual(position, root.transform.position);
                    Assert.That(edge.GetFloat("_Opacity"), Is.EqualTo(Mathf.Clamp01(remaining / .1f)).Within(.001f));
                }
                Assert.IsTrue(skin.HasProperty("_Thaw"), "The current uniform fade has no spatial thaw phase.");
                Debug.Log($"[FrostThaw] full={fullCoverage} late={lateCoverage}");
                Assert.That(fullCoverage, Is.GreaterThan(5000));
                Assert.That(lateCoverage, Is.InRange(fullCoverage * .02f, fullCoverage * .6f), "Thaw must open the rendered surface while retaining its edge.");
                foreach (float duration in new[] { 5f, 7.5f })
                {
                    field.Duration = duration;
                    field.StepTo(duration - .2f);
                    float first = skin.GetFloat("_Thaw");
                    Assert.That(first, Is.GreaterThan(.5f));
                    field.StepTo(.3f);
                    Assert.AreEqual(0, skin.GetFloat("_Thaw"));
                    field.StepTo(duration - .2f);
                    Assert.AreEqual(first, skin.GetFloat("_Thaw"), "Restored/recorded sampling cannot depend on a prior frame.");
                }
            }
            finally
            {
                if (eye != null) Object.DestroyImmediate(eye);
                if (target != null) { target.Release(); Object.DestroyImmediate(target); }
                if (root != null) Object.DestroyImmediate(root);
                QualitySettings.globalTextureMipmapLimit = mip;
            }
        }
    }
}
