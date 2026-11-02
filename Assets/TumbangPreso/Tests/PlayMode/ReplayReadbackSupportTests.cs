using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class ReplayReadbackSupportTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [UnityTest]
        public IEnumerator ActualCaptureRetainsReadyColourFramesOnThisDevice()
        {
            var go = new GameObject("Replay format contract");
            var spectator = go.AddComponent<SpectatorCamera>();
            spectator.enabled = false;
            go.GetComponent<Camera>().enabled = false;
            var source = new RenderTexture(64, 64, 0, RenderTextureFormat.ARGB32);
            source.Create();
            var format = GraphicsFormatUtility.GetGraphicsFormat(RenderTextureFormat.RGB565, false);
            Debug.Log($"[ReplayFormat] async={SystemInfo.supportsAsyncGPUReadback} render565={SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB565)} read565={SystemInfo.IsFormatSupported(format, GraphicsFormatUsage.ReadPixels)} actualAsync={spectator.AsyncReadbackWorks}");
            var arm = typeof(SpectatorCamera).GetField("_captureReplayFrame", Private);
            var capture = typeof(SpectatorCamera).GetMethod("CaptureReplayFrame", Private);
            var ring = (IList)typeof(SpectatorCamera).GetField("_replayFrames", Private).GetValue(spectator);
            try
            {
                var colours = new[] { Color.red, Color.green, Color.blue };
                Texture2D staging = null;
                foreach (var colour in colours)
                {
                    var previous = RenderTexture.active;
                    RenderTexture.active = source;
                    GL.Clear(true, true, colour);
                    RenderTexture.active = previous;
                    arm.SetValue(spectator, true);
                    capture.Invoke(spectator, new object[] { source });
                    if (!spectator.AsyncReadbackWorks)
                    {
                        var current = (Texture2D)typeof(SpectatorCamera).GetField("_synchronousReadback", Private).GetValue(spectator);
                        if (staging != null) Assert.AreSame(staging, current, "Reuse staging across captures");
                        staging = current;
                    }
                    yield return null;
                }
                AsyncGPUReadback.WaitAllRequests();
                yield return null;
                Assert.AreEqual(3, ring.Count, $"Known-colour frames were lost; failed={spectator.FailedReadbacks}");
                for (int i = 0; i < ring.Count; i++)
                {
                    var frame = ring[i];
                    var type = frame.GetType();
                    Assert.IsFalse((bool)type.GetField("Pending").GetValue(frame));
                    // The frames stay on the GPU where the device allows (`SpectatorCamera._gpuRing`), so read one back here.
                    var texture = (Texture2D)type.GetField("Image").GetValue(frame);
                    var live = (RenderTexture)type.GetField("Live").GetValue(frame);
                    Color pixel;
                    if (live != null)
                    {
                        Assert.AreEqual(RenderTextureFormat.ARGB32, live.format);
                        var active = RenderTexture.active; RenderTexture.active = live;
                        var probe = new Texture2D(1, 1, TextureFormat.RGB24, false, true);
                        probe.ReadPixels(new Rect(320, 180, 1, 1), 0, 0); probe.Apply();
                        RenderTexture.active = active;
                        pixel = probe.GetPixel(0, 0);
                        Object.Destroy(probe);
                    }
                    else
                    {
                        Assert.AreEqual(TextureFormat.RGB565, texture.format);
                        Assert.AreEqual(640 * 360 * 2, texture.GetRawTextureData<byte>().Length);
                        pixel = texture.GetPixel(320, 180);
                    }
                    Assert.That(Vector3.Distance(new Vector3(pixel.r, pixel.g, pixel.b), new Vector3(colours[i].r, colours[i].g, colours[i].b)), Is.LessThan(.08f));
                }
                Assert.AreEqual(0, spectator.FailedReadbacks);
            }
            finally
            {
                Object.DestroyImmediate(go);
                source.Release();
                Object.DestroyImmediate(source);
            }
        }
    }
}
