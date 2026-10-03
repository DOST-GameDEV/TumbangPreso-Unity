using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace TumbangPreso.PlayTests
{
    public sealed class HauntedSightVisualTests
    {
        [TestCase(16f / 9f)]
        [TestCase(4f / 3f)]
        public void SameDepthPlaneHasRadialPurpleFalloff(float aspect)
        {
            var image = Render(true, aspect);
            try
            {
                string folder = System.Environment.GetEnvironmentVariable("TUMP_HAUNT_CAPTURE");
                if (!string.IsNullOrEmpty(folder))
                    File.WriteAllBytes(Path.Combine(folder, aspect > 1.5f ? "wide.png" : "standard.png"), image.EncodeToPNG());
                var centre = image.GetPixel(image.width / 2, image.height / 2);
                var edge = image.GetPixel(image.width - 4, image.height / 2);
                Assert.Greater(centre.grayscale, edge.grayscale + .1f, "The equal-eye-depth plane must fall off with radial distance, not remain flat.");
                Assert.Greater(edge.b, edge.g + .025f, "Far nearsight must retain Nemu purple instead of black.");
                Assert.Greater(edge.r, edge.g + .01f);
                Assert.That(edge.a, Is.EqualTo(1).Within(.01));
            }
            finally { Object.DestroyImmediate(image); }
        }

        [Test]
        public void UnhauntedImageRemainsUniform()
        {
            var image = Render(false, 16f / 9f);
            try
            {
                var centre = image.GetPixel(image.width / 2, image.height / 2);
                var edge = image.GetPixel(image.width - 4, image.height / 2);
                Assert.That(centre.r, Is.EqualTo(edge.r).Within(.005));
                Assert.That(centre.g, Is.EqualTo(edge.g).Within(.005));
                Assert.That(centre.b, Is.EqualTo(edge.b).Within(.005));
                Assert.Greater(centre.grayscale, .5f);
            }
            finally { Object.DestroyImmediate(image); }
        }

        private static Texture2D Render(bool haunted, float aspect)
        {
            int height = 360, width = Mathf.RoundToInt(height * aspect);
            var shader = Shader.Find("TumbangPreso/ColourGrade"); Assert.IsNotNull(shader);
            var material = new Material(shader);
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            var depth = new Texture2D(2, 2, TextureFormat.RFloat, false, true);
            var output = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var oldZ = Shader.GetGlobalVector("_ZBufferParams");
            var prior = RenderTexture.active;
            try
            {
                source.SetPixels(new[] { Color.white*.75f,Color.white*.75f,Color.white*.75f,Color.white*.75f });
                var pixels = source.GetPixels(); for(int i=0;i<pixels.Length;i++) pixels[i].a=1;
                source.SetPixels(pixels); source.Apply();
                depth.SetPixels(new[] { Color.black,Color.black,Color.black,Color.black }); depth.Apply();
                // Controlled eye depth 5m, independent of platform reversed-Z conventions.
                Shader.SetGlobalVector("_ZBufferParams",new Vector4(0,0,0,.2f));
                material.SetTexture("_CameraDepthTexture",depth);
                material.SetFloat("_HauntedSight",haunted?1:0);
                float tan = Mathf.Tan(85 * Mathf.Deg2Rad * .5f);
                material.SetVector("_HauntedViewScale",new Vector4(tan*aspect,tan,0,0));
                Graphics.Blit(source,output,material,0);
                RenderTexture.active=output;
                var result=new Texture2D(width,height,TextureFormat.RGBA32,false,true);
                result.ReadPixels(new Rect(0,0,width,height),0,0);result.Apply();return result;
            }
            finally
            {
                Shader.SetGlobalVector("_ZBufferParams",oldZ);RenderTexture.active=prior;
                Object.DestroyImmediate(material);Object.DestroyImmediate(source);Object.DestroyImmediate(depth);
                output.Release();Object.DestroyImmediate(output);
            }
        }
    }
}
