using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class CloseFeedbackViewProbe
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        private const int Width = 960, Height = 540;

        private static Camera View(string name)
        {
            var camera = new GameObject(name).AddComponent<Camera>();
            camera.enabled = false; camera.fieldOfView = 95; camera.nearClipPlane = .05f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            camera.cullingMask = 1 << 30;
            return camera;
        }

        private static RectInt Capture(Camera camera, string name)
        {
            var target = RenderTexture.GetTemporary(Width, Height, 24, RenderTextureFormat.ARGB32);
            var pixels = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            var old = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                ComicPopup.PrepareView(camera); camera.Render(); RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); pixels.Apply();
                Directory.CreateDirectory("Logs/close-feedback");
                File.WriteAllBytes("Logs/close-feedback/" + name + ".png", pixels.EncodeToPNG());
                int minX = Width, minY = Height, maxX = -1, maxY = -1;
                var colors = pixels.GetPixels32();
                for (int y = 0; y < Height; y++) for (int x = 0; x < Width; x++)
                {
                    var c = colors[y * Width + x];
                    if (c.r + c.g + c.b < 36) continue;
                    minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                    minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
                }
                var rect = maxX < minX ? new RectInt() : new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
                Debug.Log("[CloseFeedback] " + name + " ink=" + rect);
                return rect;
            }
            finally
            {
                camera.targetTexture = null; RenderTexture.active = old;
                RenderTexture.ReleaseTemporary(target); Object.DestroyImmediate(pixels);
            }
        }

        [UnityTest]
        public IEnumerator CloseCaptionStaysReadableWithoutCoveringTheView()
        {
            var random = Random.state;
            var camera = View("Close caption camera"); camera.tag = "MainCamera";
            try
            {
                ComicPopup.Score(new Vector3(0, -1.35f, .8f), "TAGGED!", Color.white, 1.4f);
                yield return new WaitForSeconds(.18f);
                var popup = Object.FindAnyObjectByType<ComicPopup>();
                Assert.IsNotNull(popup);
                typeof(ComicPopup).GetField("_tiltAngle", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(popup, 0f);
                foreach (var node in popup.GetComponentsInChildren<Transform>()) node.gameObject.layer = 30;
                camera.transform.LookAt(popup.transform.position);
                Canvas.ForceUpdateCanvases();
                var near = Capture(camera, "caption-near");
                Assert.Greater(near.width, 60, "A cap must not simply hide the score event");
                Assert.LessOrEqual(near.width, Width * .4f, "Close tag text covers too much of the first-person view");
                Assert.LessOrEqual(near.height, Height * .13f);
                camera.transform.position = popup.transform.position - Vector3.forward * 8;
                camera.transform.LookAt(popup.transform.position);
                var far = Capture(camera, "caption-far");
                Assert.Greater(far.width, 25, "Distant observers still need a readable caption");
                Assert.Less(far.width, near.width);
                camera.orthographic = true; camera.orthographicSize = 1;
                var ortho = Capture(camera, "caption-ortho");
                Assert.Greater(ortho.width, 25);
                Assert.LessOrEqual(ortho.width, Width * .4f);
                Assert.LessOrEqual(ortho.height, Height * .13f);
            }
            finally { Random.state = random; Object.Destroy(camera.gameObject); }
        }

        [UnityTest]
        public IEnumerator ConfettiCannotBecomeALargeForegroundBlock()
        {
            var random = Random.state;
            var camera = View("Close confetti camera");
            try
            {
                HeroHazards.SpawnConfettiShower(Vector3.zero, 1);
                var ribbon = GameObject.Find("ConfettiRibbon"); Assert.IsNotNull(ribbon);
                ribbon.GetComponent<Rigidbody>().isKinematic = true;
                ribbon.layer = 30; ribbon.transform.rotation = Quaternion.Euler(90, 0, 0);
                ribbon.transform.position = new Vector3(0, 0, .7f);
                yield return null;
                var near = Capture(camera, "confetti-near");
                Assert.Greater(near.width, 3, "The nearby celebration should remain visible");
                Assert.LessOrEqual(Mathf.Max(near.width, near.height), Height * .035f,
                    "One confetti ribbon occupies too much of the view");
                ribbon.transform.position = new Vector3(0, 0, .15f);
                var crossing = Capture(camera, "confetti-crossing-camera");
                Assert.AreEqual(0, crossing.width, "Confetti crossing the camera must fade out");
                ribbon.transform.position = new Vector3(0, 0, 3);
                var far = Capture(camera, "confetti-far");
                Assert.Greater(far.width, 2, "World celebration was lost for an observer");
                camera.orthographic = true; camera.orthographicSize = .5f;
                var ortho = Capture(camera, "confetti-ortho");
                Assert.Greater(ortho.width, 3);
                Assert.LessOrEqual(Mathf.Max(ortho.width, ortho.height), Height * .035f);
            }
            finally { Random.state = random; Object.Destroy(camera.gameObject); }
        }

        [UnityTest]
        public IEnumerator CloseImpactPointsStaySmall()
        {
            var camera = View("Close impact camera");
            ImpactBurst.SpawnAt(new Vector3(0, -ImpactBurst.Height, .4f));
            var root = GameObject.Find("~ImpactBurst");
            try
            {
                root.layer = 30;
                var system = root.GetComponent<ParticleSystem>();
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var emission = system.emission; emission.enabled = false;
                system.SetParticles(new[] { new ParticleSystem.Particle {
                    position = Vector3.zero, startSize = ImpactBurst.PointRadius * 2,
                    startColor = Color.white, startLifetime = ImpactBurst.Lifetime,
                    remainingLifetime = ImpactBurst.Lifetime } }, 1);
                system.Pause();
                yield return null;
                var ink = Capture(camera, "impact-near");
                Assert.Greater(ink.width, 2, "Impact confirmation disappeared");
                Assert.LessOrEqual(Mathf.Max(ink.width, ink.height), Height * .04f,
                    "A nearby impact point covers too much of the view");
            }
            finally { Object.Destroy(root); Object.Destroy(camera.gameObject); }
        }

        [UnityTest]
        public IEnumerator ParticipantCaptionUsesTheSameRandomDrawAndDedupePath()
        {
            var before = Random.state;
            var participant = new GameObject("Caption participant").AddComponent<CharacterMotor>();
            try
            {
                var state = Random.state;
                ComicPopup.Spawn(Vector3.zero, "TAGGED!", Color.white, 1.4f, ComicPopup.Weight.Cast);
                var normal = Random.state;
                ComicPopup.Spawn(Vector3.zero, "TAGGED!", Color.white, 1.4f, ComicPopup.Weight.Cast);
                Assert.AreEqual(normal, Random.state, "A deduplicated caption must not draw another tilt");
                Object.DestroyImmediate(Object.FindAnyObjectByType<ComicPopup>().gameObject);
                Random.state = state;
                ComicPopup.Spawn(Vector3.zero, "TAGGED!", Color.white, 1.4f, ComicPopup.Weight.Cast, participant);
                Assert.AreEqual(normal, Random.state, "Participant visibility changed the shared random stream");
                ComicPopup.Spawn(Vector3.zero, "TAGGED!", Color.white, 1.4f, ComicPopup.Weight.Cast, participant);
                Assert.AreEqual(normal, Random.state);
                Assert.AreEqual(1, Object.FindObjectsByType<ComicPopup>().Length);
                yield return null;
            }
            finally { Random.state = before; Object.Destroy(participant.gameObject); }
        }
    }
}
