using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class RoundBreakFreezeTests
    {
        sealed class Client : INetProvider
        {
            public bool IsHost => false;
            public bool IsNetworked => true;
            public int LocalSlot => 1;
            public int LocalPeerId => 1;
            public bool IsSeatlessReferee => false;
        }

        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [UnityTest, Timeout(60000)]
        public IEnumerator FinalViewAndRealInputsStayFrozenUntilTheHostDeadline()
        {
            var settings = InputSystem.settings;
            var background = settings.backgroundBehavior;
            var editor = settings.editorInputBehaviorInPlayMode;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keys = InputSystem.AddDevice<Keyboard>(); var mouse = InputSystem.AddDevice<Mouse>();
            InputSystem.EnableDevice(keys); InputSystem.EnableDevice(mouse);
            try
            {
                yield return MapRetrievalProbe.Load(UI.SceneFlow.Eskinita);
                var match = GameServices.Match; var round = GameServices.Round;
                var phase = HalftimePresentation.Instance; Assert.IsNotNull(phase);
                var frames = phase.GetComponent<RoundBreakFrame>();
                float until = Time.realtimeSinceStartup + 10;
                while (frames.Texture == null && Time.realtimeSinceStartup < until) { DrawFrame(); yield return null; }
                Assert.IsNotNull(frames.Texture, "The gameplay camera never supplied its final image.");
                var texture = frames.Texture; ulong before = Pixels(texture); int count = frames.CapturedFrames;
                var local = round.PlayerAt(GameLaunch.SoloSeat); local.IsBot = false; local.Intent.Parked = false;
                var reader = local.GetComponent<PlayerInputReader>();
                Vector3 position = local.transform.position; Quaternion facing = Camera.main.transform.rotation;
                var modules = EventSystem.current?.GetComponents<BaseInputModule>().Where(m => m.enabled).ToArray();
                round.EndRound(); match.BeginIntermission();
                Assert.AreSame(texture, phase.FrozenFrame);
                Assert.IsTrue(PresentationClock.BlocksInput); Assert.IsTrue(UI.RoleSwapCard.Showing);
                Assert.IsFalse(phase.HasReplay); Assert.IsFalse(BufferSkipVote.Showing);
                match.SkipBuffer(); Assert.IsFalse(match.SkipRequested);
                Object.FindAnyObjectByType<UI.RoleSwapCard>().DismissAndPractice();
                Assert.IsTrue(UI.RoleSwapCard.Showing, "A click must not dismiss the frozen break.");
                float simulationTime = Time.time;
                InputSystem.QueueStateEvent(keys, new KeyboardState(Key.W, Key.F, Key.Tab, Key.Escape));
                InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 1, delta = new Vector2(100, 100) });
                InputSystem.Update(); reader.SendMessage("Update");
                yield return new WaitForSecondsRealtime(.4f);
                Assert.AreEqual(simulationTime, Time.time);
                Assert.AreEqual(position, local.transform.position);
                Assert.AreEqual(facing, Camera.main.transform.rotation);
                Assert.IsFalse(local.Intent.Pressed(Verb.SpecialAbility));
                Assert.IsFalse(Object.FindObjectsByType<UI.PausePanel>().Any(p => p.isActiveAndEnabled));
                if (modules != null) Assert.IsTrue(modules.All(m => !m.enabled), "UI input modules must be suspended during the break.");
                Assert.AreEqual(count, frames.CapturedFrames);
                Assert.AreEqual(before, Pixels(texture), "The final world pixels changed during the freeze.");
                yield return TumpUiCapture.Capture("Round-frozen-final-view-960x540", Object.FindAnyObjectByType<UI.TumpRoundSwapView>().Canvas,
                    960, 540, false, underlays: new[] { GameObject.Find("FrozenRoundView").GetComponent<Canvas>() });
                double deadline = phase.Began + phase.Duration;
                while (SharedUltimatePhase.Now < deadline + .15) yield return null;
                Assert.AreEqual(2, match.RoundNumber); Assert.IsFalse(PresentationClock.Held);
                Assert.IsFalse(phase.Active); Assert.IsFalse(UI.RoleSwapCard.Showing);
                if (modules != null) Assert.IsTrue(modules.All(m => m.enabled));
                Assert.IsFalse(local.Intent.Pressed(Verb.SpecialAbility), "The held break-time click must not become a throw.");
            }
            finally
            {
                HalftimePresentation.Instance?.End(false);
                InputSystem.RemoveDevice(keys); InputSystem.RemoveDevice(mouse);
                settings.backgroundBehavior = background; settings.editorInputBehaviorInPlayMode = editor;
            }
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator LateClientSharesTheDeadlineWithoutAdvancingOrRestartingIt()
        {
            yield return MapRetrievalProbe.Load(UI.SceneFlow.Eskinita);
            var match = GameServices.Match; var phase = HalftimePresentation.Instance;
            var provider = NetAuthority.Provider;
            try
            {
                GameServices.Round.EndRound(); match.IsWarmupBuffer = true;
                NetAuthority.Provider = new Client();
                double began = SharedUltimatePhase.Now - 8;
                Assert.IsTrue(phase.Receive(match.PresentationMatchId, 1, 1, began, 0, false, 1));
                Assert.That(phase.Remaining, Is.InRange(1.8f, 2.1f));
                Assert.IsFalse(phase.Receive(match.PresentationMatchId, 1, 1, SharedUltimatePhase.Now, 0, false, 1));
                // A late screen with no previous camera image must capture once
                // while frozen, then keep that first image unchanged.
                var coldRoot = new GameObject("Cold late-peer frame");
                var cold = coldRoot.AddComponent<RoundBreakFrame>();
                try
                {
                    Assert.IsNull(cold.Texture); cold.Freeze();
                    double captureUntil = SharedUltimatePhase.Now + 1;
                    while (cold.Texture == null && SharedUltimatePhase.Now < captureUntil) { DrawFrame(); yield return null; }
                    Assert.IsNotNull(cold.Texture); Assert.AreEqual(1, cold.CapturedFrames);
                    yield return new WaitForSecondsRealtime(.1f);
                    Assert.AreEqual(1, cold.CapturedFrames);
                }
                finally { cold.Release(); Object.Destroy(coldRoot); }
                while (phase.Active && SharedUltimatePhase.Now < began + 10.5) yield return null;
                Assert.IsFalse(phase.Active); Assert.IsFalse(PresentationClock.Held);
                Assert.AreEqual(1, match.RoundNumber, "A client cannot advance the authoritative round.");
                Assert.IsFalse(phase.Receive(match.PresentationMatchId, 1, 1, began, 0, false, 1), "Expired packets cannot restart the break.");
                Assert.IsFalse(phase.Receive(match.PresentationMatchId, 1, 1, SharedUltimatePhase.Now, 0, false, 1), "A changed timestamp cannot reopen the consumed boundary.");
                Assert.IsFalse(GameObject.Find("FrozenRoundView")?.activeInHierarchy ?? false);
            }
            finally { phase.End(false); NetAuthority.Provider = provider; Time.timeScale = 1; }
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator AnAlreadyDisplayedViewIsCapturedBeforeItsPresenterCloses()
        {
            yield return MapRetrievalProbe.Load(UI.SceneFlow.Eskinita);
            DrawFrame();
            var root = new GameObject("Previous displayed view", typeof(RectTransform), typeof(Canvas));
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 180;
            var pictureRoot = new GameObject("Presented frame", typeof(RectTransform), typeof(RawImage));
            pictureRoot.transform.SetParent(root.transform, false);
            var picture = pictureRoot.GetComponent<RawImage>();
            var rect = picture.rectTransform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var previousView = new RenderTexture(8, 8, 0, RenderTextureFormat.ARGB32);
            previousView.Create(); Graphics.Blit(Texture2D.whiteTexture, previousView);
            picture.texture = previousView; picture.color = Color.magenta;
            Canvas.ForceUpdateCanvases();
            try
            {
                GameServices.Round.EndRound(); GameServices.Match.BeginIntermission();
                var frame = HalftimePresentation.Instance.FrozenFrame;
                var old = RenderTexture.active;
                var pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                try
                {
                    RenderTexture.active = frame;
                    pixel.ReadPixels(new Rect(frame.width / 2, frame.height / 2, 1, 1), 0, 0); pixel.Apply();
                    var actual = pixel.GetPixel(0, 0);
                    Assert.That(actual.r, Is.GreaterThan(.98f)); Assert.That(actual.g, Is.LessThan(.02f)); Assert.That(actual.b, Is.GreaterThan(.98f));
                }
                finally { RenderTexture.active = old; Object.DestroyImmediate(pixel); }
                ulong frozen = Pixels(frame);
                root.SetActive(false); previousView.Release();
                yield return new WaitForSecondsRealtime(.1f);
                Assert.AreEqual(frozen, Pixels(frame), "Closing the old presenter must not erase the frozen view.");
            }
            finally
            {
                HalftimePresentation.Instance.End(false); previousView.Release();
                Object.Destroy(previousView); Object.Destroy(root);
            }
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator EndingAnIntroductionCannotReleaseTheNewRoundBreakHold()
        {
            yield return MapRetrievalProbe.Load(UI.SceneFlow.Eskinita);
            var introduction = SharedUltimatePhase.Ensure();
            Assert.IsTrue(introduction.CanAccept(1));
            typeof(SharedUltimatePhase).GetMethod("Accept", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(introduction, new object[] { new UltimateCommit(1, 1, Vector3.zero, Vector3.forward, Vector3.forward, 0) });
            Assert.IsTrue(SharedUltimatePhase.Collecting); Assert.IsTrue(PresentationClock.Held);
            GameServices.Round.EndRound(); GameServices.Match.BeginIntermission();
            yield return null;
            Assert.IsFalse(introduction.Active);
            Assert.IsTrue(HalftimePresentation.Playing);
            Assert.IsTrue(PresentationClock.Held, "Old introduction cleanup released the new freeze's hold.");
            Assert.IsTrue(PresentationClock.BlocksInput); Assert.AreEqual(0, Time.timeScale);
            HalftimePresentation.Instance.End(false);
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator AbortingTheBreakRestoresTheRequestedClockAndUiInput()
        {
            yield return MapRetrievalProbe.Load(UI.SceneFlow.Eskinita);
            var phase = HalftimePresentation.Instance;
            var modules = EventSystem.current?.GetComponents<BaseInputModule>().Where(m => m.enabled).ToArray();
            GameServices.Round.EndRound(); GameServices.Match.BeginIntermission();
            Assert.IsTrue(PresentationClock.Held);
            PresentationClock.RequestScale(.5f); phase.End(false);
            Assert.IsFalse(PresentationClock.Held); Assert.AreEqual(.5f, Time.timeScale);
            if (modules != null) Assert.IsTrue(modules.All(m => m.enabled));
            Assert.IsFalse(GameObject.Find("FrozenRoundView")?.activeInHierarchy ?? false);
            Assert.AreEqual(1, GameServices.Match.RoundNumber);
            Time.timeScale = 1;
        }

        static ulong Pixels(RenderTexture source)
        {
            var old = RenderTexture.active;
            var image = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = source;
                image.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0); image.Apply();
                ulong hash = 1469598103934665603;
                foreach (var p in image.GetPixels32())
                    unchecked { hash = (hash ^ (uint)(p.r | p.g << 8 | p.b << 16 | p.a << 24)) * 1099511628211; }
                return hash;
            }
            finally { RenderTexture.active = old; Object.DestroyImmediate(image); }
        }

        internal static void DrawFrame()
        {
            // Batch Editor tests have no live Game View. Exercise the actual
            // gameplay camera and final-image callback through an offscreen target.
            var camera = Camera.main; Assert.IsNotNull(camera);
            var old = camera.targetTexture;
            var target = new RenderTexture(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height), 24);
            target.Create();
            try
            {
                camera.targetTexture = target; camera.Render();
                Debug.Log("[RoundFreezeFrame] pipeline=" + (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline?.name ?? "Built-in") + " camera=" + camera.name);
            }
            finally { camera.targetTexture = old; target.Release(); Object.DestroyImmediate(target); }
        }
    }
}
