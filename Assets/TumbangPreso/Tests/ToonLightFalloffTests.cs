using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Tests
{
    public sealed class ToonLightFalloffTests
    {
        [TestCase("TumbangPreso/Toon")]
        [TestCase("TumbangPreso/ToonTransparent")]
        public void PointLightFallsOffBeforeItsRangeAndDoesNotTintBeyondIt(string shaderName)
        {
            const int layer = 31;
            var owned = new List<Object>();
            var ambient = RenderSettings.ambientLight;
            var ambientMode = RenderSettings.ambientMode;
            bool fog = RenderSettings.fog;
            int pixelLights = QualitySettings.pixelLightCount;
            var active = RenderTexture.active;
            try
            {
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = Color.black;
                RenderSettings.fog = false;
                QualitySettings.pixelLightCount = 4;
                var material = new Material(Shader.Find(shaderName));
                owned.Add(material);
                material.SetColor("_Color", new Color(.35f,.25f,.2f));
                material.SetFloat("_OutlineWidth",0);
                material.SetFloat("_RimStrength",0);
                material.SetFloat("_UsePalette",0);
                var surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
                owned.Add(surface);
                surface.layer = layer;
                surface.transform.position = Vector3.zero;
                surface.transform.localScale = new Vector3(10,.05f,2);
                surface.GetComponent<Renderer>().sharedMaterial = material;

                var sun = new GameObject("Falloff test key"); owned.Add(sun);
                var key = sun.AddComponent<Light>();
                key.type = LightType.Directional; key.color = Color.white; key.intensity = .08f;
                key.cullingMask = 1 << layer;
                sun.transform.rotation = Quaternion.Euler(90,0,0);
                var pointObject = new GameObject("Bounded green light"); owned.Add(pointObject);
                var point = pointObject.AddComponent<Light>();
                point.type = LightType.Point; point.color = Color.green;
                point.intensity = 4; point.range = 2; point.renderMode = LightRenderMode.ForcePixel;
                point.cullingMask = 1 << layer; pointObject.transform.position = new Vector3(-4,1,0);

                var cameraObject = new GameObject("Falloff review camera"); owned.Add(cameraObject);
                var camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false; camera.orthographic = true; camera.orthographicSize = 3;
                camera.aspect = 2; camera.cullingMask = 1 << layer;
                camera.transform.position = new Vector3(0,8,0);
                camera.transform.rotation = Quaternion.Euler(90,0,0);
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
                camera.renderingPath = RenderingPath.Forward; camera.allowHDR = true;
                var target = new RenderTexture(512,256,24,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear);
                owned.Add(target); target.Create(); camera.targetTexture = target;
                var pixels = new Texture2D(512,256,TextureFormat.RGBAFloat,false,true); owned.Add(pixels);
                var review = System.Environment.GetEnvironmentVariable("TUMP_LIGHT_REVIEW");
                if (!string.IsNullOrEmpty(review)) review = Path.Combine(review,shaderName.Replace('/','_'));
                Color[] Render(bool enabled, string phase)
                {
                    point.enabled = enabled;
                    camera.Render();
                    RenderTexture.active = target;
                    pixels.ReadPixels(new Rect(0,0,512,256),0,0); pixels.Apply();
                    if (!string.IsNullOrEmpty(review))
                    {
                        Directory.CreateDirectory(review);
                        File.WriteAllBytes(Path.Combine(review,phase + ".png"),pixels.EncodeToPNG());
                    }
                    return new[] { pixels.GetPixel(85,128),pixels.GetPixel(426,128) };
                }
                var before = Render(false,"point-off");
                var after = Render(true,"point-near");
                pointObject.transform.position = new Vector3(-4,1.90f,0);
                var insideEdge = Render(true,"point-near-range-edge");
                float near = after[0].g-before[0].g;
                float far = after[1].g-before[1].g;
                float edge = insideEdge[0].g-before[0].g;
                if (!string.IsNullOrEmpty(review))
                    File.WriteAllText(Path.Combine(review,"measurements.txt"),$"near green gain={near:R}; inner-edge gain={edge:R}; outside-range gain={far:R}");
                Assert.Greater(near,.02f,"The fixture did not illuminate its near surface.");
                Assert.Less(edge,near*.20f,
                    "Light near the end of its range must fade, not retain a minimum toon-band contribution.");
                Assert.Less(Mathf.Abs(far),.005f,
                    "The same renderer is receiving colored light on pixels beyond the light's range.");
            }
            finally
            {
                RenderTexture.active = active;
                for(int i=owned.Count-1;i>=0;i--) if(owned[i]!=null) Object.DestroyImmediate(owned[i]);
                RenderSettings.ambientLight = ambient; RenderSettings.ambientMode = ambientMode;
                RenderSettings.fog = fog; QualitySettings.pixelLightCount = pixelLights;
            }
        }
    }
}
