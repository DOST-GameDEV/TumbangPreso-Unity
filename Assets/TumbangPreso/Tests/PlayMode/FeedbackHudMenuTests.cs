using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class FeedbackHudMenuTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [UnityTest, Timeout(90000)]
        public IEnumerator EscapeOpensAndClosesTheMenuWithoutReplacingTheHomePicture()
        {
            var input = InputSystem.settings;
            var background = input.backgroundBehavior; var editor = input.editorInputBehaviorInPlayMode;
            bool reduced = TumbangPreso.Settings.SettingsStore.Current.ReducedUiMotion;
            input.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            input.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            TumbangPreso.Settings.SettingsStore.Current.ReducedUiMotion = true;
            var keys = InputSystem.AddDevice<Keyboard>(); InputSystem.EnableDevice(keys);
            try
            {
                SceneFlow.Networked = false; GameLaunch.Reset();
                yield return SceneManager.LoadSceneAsync(SceneFlow.MatchSetup);
                float until = Time.realtimeSinceStartup + 60;
                while ((TumpHub.Current == null || HubLoading.Visible) && Time.realtimeSinceStartup < until) yield return null;
                var hub = TumpHub.Current; Assert.IsNotNull(hub); Assert.IsInstanceOf<HubHome>(hub.Top);
                var video = Object.FindFirstObjectByType<HubSceneVideo>(); Assert.IsNotNull(video);
                yield return null;
                var image = video.GetComponent<RawImage>(); Assert.IsTrue(image.enabled); Assert.IsNotNull(image.texture);
                var texture = image.texture;
                var setup = Object.FindFirstObjectByType<ConvertedMatchSetup>(); Assert.IsNotNull(setup);
                InputSystem.QueueStateEvent(keys, new KeyboardState(Key.Escape)); InputSystem.Update(); setup.SendMessage("Update");
                yield return null;
                Assert.IsInstanceOf<HubMenu>(hub.Top); Assert.AreEqual(SceneFlow.MatchSetup, SceneManager.GetActiveScene().name);
                Assert.IsTrue(image.enabled); Assert.AreSame(texture, image.texture);
                InputSystem.QueueStateEvent(keys, new KeyboardState()); InputSystem.Update(); yield return null;
                InputSystem.QueueStateEvent(keys, new KeyboardState(Key.Escape)); InputSystem.Update(); setup.SendMessage("Update");
                yield return null;
                Assert.IsInstanceOf<HubHome>(hub.Top); Assert.AreEqual(SceneFlow.MatchSetup, SceneManager.GetActiveScene().name);
                Assert.IsTrue(image.enabled); Assert.AreSame(texture, image.texture);
            }
            finally
            {
                InputSystem.RemoveDevice(keys); input.backgroundBehavior = background; input.editorInputBehaviorInPlayMode = editor;
                TumbangPreso.Settings.SettingsStore.Current.ReducedUiMotion = reduced;
            }
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator RealStaminaMeshDrainsFromItsTopAndKeepsItsLowerEnd()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita);
            GameServices.Round.BeginRound();
            var who = GameServices.Round.PlayerAt(1); who.enabled = false;
            who.Stamina.Spend(1);
            yield return new WaitForSecondsRealtime(.2f);
            var ring = GameObject.Find("StaminaMeter").GetComponent<HudRing>(); Assert.IsNotNull(ring);
            Assert.That(ring.Fill, Is.InRange(.97f, .99f));
            var before = FilledBounds(ring);
            who.Stamina.Spend(Balance.StaminaMax * .5f - 1);
            yield return null;
            Assert.AreEqual(.5f, ring.Fill, .001f);
            var after = FilledBounds(ring);
            Assert.Less(after.max.y, before.max.y - 20, "The visible top end must retreat downward.");
            Assert.AreEqual(before.min.y, after.min.y, 2f, "The lower end remains anchored.");
            yield return TumpUiCapture.Capture("Stamina-top-drain-960x540", GameObject.Find("OwnerMatchCanvas").GetComponent<Canvas>(), 960, 540, false, true);
        }

        static Bounds FilledBounds(HudRing ring)
        {
            Canvas.ForceUpdateCanvases();
            var mesh = ring.canvasRenderer.GetMesh();
            Assert.IsNotNull(mesh);
            {
                var vertices = mesh.vertices; var colors = mesh.colors32; Color32 fill = ring.color;
                var points = vertices.Where((v, i) => colors[i].r == fill.r && colors[i].g == fill.g && colors[i].b == fill.b).ToArray();
                Assert.IsNotEmpty(points, "The real CanvasRenderer must contain visible stamina fill.");
                var bounds = new Bounds(points[0], Vector3.zero); foreach (var point in points) bounds.Encapsulate(point); return bounds;
            }
        }
    }
}
