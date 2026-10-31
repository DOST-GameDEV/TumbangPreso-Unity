using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class ConnectionUiReviewTests
    {
        private sealed class Viewer : INetProvider
        {
            public bool IsHost => false; public bool IsNetworked => true;
            public int LocalSlot => -1; public int LocalPeerId => 5; public bool IsSeatlessReferee => false;
        }
        private GameObject _owner;
        private Texture2D _background;
        private INetProvider _provider;
        private bool _spectator;
        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset(); GameServices.Ensure();
            _provider = NetAuthority.Provider; _spectator = GameLaunch.Spectator;
            _owner = new GameObject("Connection UI review");
        }
        [UnityTearDown] public IEnumerator After()
        {
            if (_owner != null) Object.Destroy(_owner); if (_background != null) Object.Destroy(_background);
            NetAuthority.Provider = _provider; GameLaunch.Spectator = _spectator; Time.timeScale = 1;
            yield return PlayModeWorld.Reset();
        }
        [UnityTest] public IEnumerator MatchNoticeAndConnectionBadgeShareTheHudWithoutCoveringEachOther()
        {
            NetAuthority.Provider = new Viewer(); GameLaunch.Spectator = true;
            GameServices.Round.Clear();
            for (int i = 0; i < 4; i++)
            {
                var body = new GameObject("Review player " + i, typeof(CharacterController)); body.transform.SetParent(_owner.transform);
                var actor = body.AddComponent<CharacterMotor>(); actor.enabled = false;
                actor.PlayerSlot = i; actor.CharacterIndex = i; actor.IsPerson = true;
                GameServices.Round.Register(actor);
            }
            var view = _owner.AddComponent<TumpMatchReadout>(); view.Build(_owner.transform);
            var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, "-tp-ui-background");
            if (at >= 0 && at + 1 < args.Length)
            {
                Assert.IsTrue(File.Exists(args[at + 1]), "Review background is missing.");
                _background = new Texture2D(2, 2); Assert.IsTrue(_background.LoadImage(File.ReadAllBytes(args[at + 1])));
                var backdrop = OwnerUiLayout.Rect(view.Canvas.transform, "CapturedGameplayReview").gameObject.AddComponent<RawImage>();
                backdrop.texture = _background; backdrop.raycastTarget = false; backdrop.transform.SetAsFirstSibling();
                var fit = backdrop.gameObject.AddComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = (float)_background.width / _background.height;
            }
            view.Tick(null, true, false, true, false);
            var notice = view.Canvas.GetComponentInChildren<PeerConnectionNotice>(true);
            var icon = view.Canvas.GetComponentInChildren<ConnectionQualityIcon>(true);
            Assert.AreEqual("MatchScores", icon.transform.parent.name, "Connection quality must travel with the scoreboard.");
            foreach (var shape in new[] { new Vector2Int(960, 540), new Vector2Int(1600, 680) })
            {
                foreach (var tier in new[] { ConnectionQualityDisplay.Tier.Mild, ConnectionQualityDisplay.Tier.Severe })
                {
                    notice.Present("P2 · Maya DISCONNECTED · SEAT RESERVED", 1, "Maya", false, false);
                    yield return null;
                    yield return TumpUiCapture.Capture("network-layout2-" + tier + "-" + shape.x + "x" + shape.y,
                        view.Canvas, shape.x, shape.y, false, inspectViewport: () =>
                        {
                            typeof(ConnectionQualityIcon).GetField("_drawn", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(icon, tier);
                            icon.SetVerticesDirty(); icon.Rebuild(CanvasUpdate.PreRender);
                        });
                }
            }
            Assert.IsTrue(notice.IsShowing);
            Assert.AreEqual(1, PresentationClock.RequestedScale, .001f);
        }
        [UnityTest, Timeout(90000)] public IEnumerator HostLossHasOneClearReturnActionOnTheActualHomeScreen()
        {
            yield return HubFlowTests.OpenHome();
            MatchAbandon.Note(null, false);
            typeof(MatchAbandon).GetProperty(nameof(MatchAbandon.RoundNumber)).GetSetMethod(true).Invoke(null, new object[] { 1 });
            var hub = TumpHub.Current;
            var notice = hub.Push<HubDisconnectedNotice>(n => n.Message = MatchAbandon.PlayerLine);
            yield return null;
            var buttons = notice.GetComponentsInChildren<Button>(true).Where(b => b.gameObject.activeInHierarchy).ToArray();
            Assert.AreEqual(1, buttons.Length); Assert.AreSame(buttons[0], notice.FirstFocus);
            StringAssert.Contains("BACK TO HOME", buttons[0].GetComponentInChildren<Text>().text);
            foreach (var shape in new[] { new Vector2Int(960, 540), new Vector2Int(1600, 680) })
                yield return TumpUiCapture.Capture("network-host-loss2-" + shape.x + "x" + shape.y, hub.Canvas, shape.x, shape.y, false);
            buttons[0].onClick.Invoke();
            Assert.IsInstanceOf<HubHome>(hub.Top); Assert.IsNull(hub.Find<HubDisconnectedNotice>());
        }
    }
}
