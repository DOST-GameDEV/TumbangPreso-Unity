using System.Collections;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class PeerConnectionNoticeTests
    {
        private sealed class Client : INetProvider
        {
            public bool IsHost => false;
            public bool IsNetworked => true;
            public int LocalSlot => -1;
            public int LocalPeerId => 5;
            public bool IsSeatlessReferee => false;
        }
        private GameObject _root;
        private Canvas _canvas;
        private PeerConnectionNotice _notice;
        private INetProvider _provider;
        private bool _spectating;
        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _provider = NetAuthority.Provider; NetAuthority.Provider = new Client();
            _spectating = GameLaunch.Spectator; GameLaunch.Spectator = true;
            _root = new GameObject("Connection notice fixture", typeof(RectTransform), typeof(Canvas));
            _canvas = _root.GetComponent<Canvas>(); _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _notice = PeerConnectionNotice.Create((RectTransform)_root.transform);
            _notice.Present("P2 · Maya DISCONNECTED · BOT TAKES OVER");
        }
        [UnityTearDown] public IEnumerator After()
        {
            ScreenTakeover.Unregister(_notice); Time.timeScale = 1;
            Object.Destroy(_root); NetAuthority.Provider = _provider; GameLaunch.Spectator = _spectating;
            yield return PlayModeWorld.Reset();
        }
        [UnityTest] public IEnumerator HiddenCutsceneCanvasPreservesTheFullReactionWindow()
        {
            _canvas.enabled = false;
            _notice.Present("P2 · Maya DISCONNECTED · BOT TAKES OVER");
            yield return new WaitForSecondsRealtime(.25f);
            Assert.AreEqual(12, _notice.VisibleSecondsLeft, .01f);
            Assert.IsFalse(_notice.IsShowing);
            _canvas.enabled = true;
            yield return new WaitForSecondsRealtime(.25f);
            Assert.IsTrue(_notice.IsShowing);
            Assert.Less(_notice.VisibleSecondsLeft, 11.9f);
            Assert.AreEqual(1, PresentationClock.RequestedScale, .001f, "A notice must not pause the room itself.");
        }
        [UnityTest] public IEnumerator FadedHudAndCoveringMenusDoNotSpendTheReactionWindow()
        {
            var group = _root.AddComponent<CanvasGroup>(); group.alpha = 0;
            _notice.Present("P2 · Maya DISCONNECTED · BOT TAKES OVER");
            yield return new WaitForSecondsRealtime(.2f);
            Assert.AreEqual(12, _notice.VisibleSecondsLeft, .01f);
            group.alpha = 1;
            ScreenTakeover.Register(_notice, () => true);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.AreEqual(12, _notice.VisibleSecondsLeft, .01f);
            ScreenTakeover.Unregister(_notice);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.IsTrue(_notice.IsShowing);
            Assert.Less(_notice.VisibleSecondsLeft, 11.9f);
        }
        [UnityTest] public IEnumerator AnAcceptedPauseAcknowledgesTheNoticeAndLeavingSpectatorClearsIt()
        {
            yield return null; Assert.IsTrue(_notice.IsShowing);
            PresentationClock.RequestScale(0);
            yield return null;
            Assert.IsFalse(_notice.IsShowing); Assert.AreEqual(0, _notice.VisibleSecondsLeft);
            PresentationClock.RequestScale(1); _notice.Present("Another connection ended.");
            GameLaunch.Spectator = false;
            yield return null;
            Assert.IsFalse(_notice.IsShowing); Assert.AreEqual(0, _notice.VisibleSecondsLeft);
        }
    }
}
