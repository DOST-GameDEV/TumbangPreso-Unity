using System.Collections.Generic;
using NUnit.Framework;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class BankShotContactTests
    {
        private readonly List<GameObject> _objects = new();
        private readonly HashSet<Material> _materials = new();
        private BankShotContact Make(bool low = false, bool reduced = false)
        {
            var cue = BankShotContact.Spawn(new Vector3(2, 1, 3), low, reduced);
            Assert.IsNotNull(cue);
            _objects.Add(cue.gameObject);
            foreach (var line in cue.GetComponentsInChildren<LineRenderer>()) _materials.Add(line.sharedMaterial);
            return cue;
        }
        [TearDown] public void Cleanup()
        {
            foreach (var go in _objects) if (go != null) Object.DestroyImmediate(go);
            foreach (var material in _materials) if (material != null) Object.DestroyImmediate(material);
            _objects.Clear(); _materials.Clear();
        }
        [TestCase(false, 3)] [TestCase(true, 2)]
        public void QualityKeepsSmallAuthoredStrokesWithoutCollision(bool low, int count)
        {
            var cue = Make(low);
            var lines = cue.GetComponentsInChildren<LineRenderer>();
            Assert.AreEqual(count, lines.Length);
            Assert.IsEmpty(cue.GetComponentsInChildren<Collider>());
            foreach (var line in lines)
            {
                Assert.AreEqual(3, line.positionCount);
                Assert.IsFalse(line.useWorldSpace);
                Assert.IsNotNull(line.GetComponent<VfxRenderTag>());
                for (int i = 0; i < line.positionCount; i++)
                    Assert.Less(line.GetPosition(i).magnitude * 1.35f, .3f);
            }
        }
        [Test] public void FadeEndsWithNoVisibleStroke()
        {
            var cue = Make();
            cue.StepTo(BankShotContact.Lifetime / 2);
            foreach (var line in cue.GetComponentsInChildren<LineRenderer>())
                // LineRenderer stores gradient colors with Color32 precision.
                Assert.AreEqual(((Color)(Color32)new Color(1, 1, 1, .25f)).a, line.startColor.a, .0001f);
            cue.StepTo(BankShotContact.Lifetime);
            foreach (var line in cue.GetComponentsInChildren<LineRenderer>()) Assert.IsFalse(line.enabled);
        }
        [Test] public void ReducedMotionKeepsTheContactStationary()
        {
            var cue = Make(reduced: true);
            var origin = cue.transform.position;
            cue.StepTo(.1f);
            Assert.AreEqual(Vector3.one, cue.transform.localScale);
            Assert.AreEqual(origin, cue.transform.position);
        }
        [Test] public void NonfiniteAgeFailsClosed()
        {
            var cue = Make(); cue.StepTo(float.NaN);
            foreach (var line in cue.GetComponentsInChildren<LineRenderer>()) Assert.IsFalse(line.enabled);
        }
        [Test] public void RenderedContactFadesAgainstCourtBackground()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("Requires a graphical Editor for actual pixel evidence.");
            var cue = Make();
            var cameraGo = new GameObject("Bank contact capture"); _objects.Add(cameraGo);
            var camera = cameraGo.AddComponent<Camera>();
            camera.transform.position = cue.transform.position + new Vector3(0, .12f, -2);
            camera.transform.LookAt(cue.transform.position + Vector3.up * .04f);
            camera.orthographic = true; camera.orthographicSize = .4f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.10f, .12f, .11f);
            var target = new RenderTexture(640, 360, 24);
            var pixels = new Texture2D(640, 360, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                var output = System.Environment.GetEnvironmentVariable("TUMP_BANK_CAPTURE");
                long firstEnergy = 0;
                foreach (float age in new[] { 0f, .11f, .22f })
                {
                    cue.StepTo(age); camera.Render(); RenderTexture.active = target;
                    pixels.ReadPixels(new Rect(0, 0, 640, 360), 0, 0); pixels.Apply();
                    int gold = 0; long energy = 0;
                    foreach (var color in pixels.GetPixels32())
                        {
                        energy += Mathf.Max(0, color.r - color.b);
                        if (color.r > color.b + 30 && color.g > color.b + 20) gold++;
                    }
                    
                    if (!string.IsNullOrEmpty(output))
                    {
                        System.IO.Directory.CreateDirectory(output);
                        System.IO.File.WriteAllBytes(System.IO.Path.Combine(output,
                            "contact-" + Mathf.RoundToInt(age * 1000) + ".png"), pixels.EncodeToPNG());
                    }
                    // Expanding strokes cover more pixels; compare total gold energy, not area.
                    if (age == 0) { firstEnergy = energy; Assert.Greater(gold, 40); }
                    else Assert.Less(energy, firstEnergy);
                    if (age >= BankShotContact.Lifetime) Assert.AreEqual(0, gold);
                }
            }
            finally
            {
                camera.targetTexture = null; RenderTexture.active = previous;
                Object.DestroyImmediate(pixels); target.Release(); Object.DestroyImmediate(target);
            }
        }
        [Test] public void InvalidPositionDoesNotSpawn()
        {
            Assert.IsNull(BankShotContact.Spawn(new Vector3(float.PositiveInfinity, 0, 0), false, false));
            Assert.IsNull(BankShotContact.Spawn(new Vector3(0, float.NaN, 0), false, false));
        }
    }
}
