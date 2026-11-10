using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace TumbangPreso.PlayTests
{
    public sealed class SpectatorControlsHintTests
    {
        private GameObject _root; private TumpMatchReadout _view; private Text _label;
        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset(); GameServices.Ensure();
            _root = new GameObject("Spectator hint fixture");
            _view = _root.AddComponent<TumpMatchReadout>(); _view.Build(_root.transform);
            _label = _view.Canvas.GetComponentsInChildren<Text>(true).Single(t => t.name == "SpectatorReadout");
            yield return null;
        }
        [UnityTearDown] public IEnumerator After()
        { if (_root != null) Object.Destroy(_root); yield return PlayModeWorld.Reset(); }
        [Test] public void HiddenControlsLeaveTheSpectatorViewClear()
        {
            _view.Tick(null, true, false, false, false);
            Assert.IsFalse(_label.enabled);
            Assert.IsFalse(_view.Canvas.GetComponentsInChildren<Text>(true).Any(t => t.name == "SpectatorViewMode"));
        }
        [Test] public void VisibleControlsListTheCloseAndCleanFeedBindings()
        {
            _view.Tick(null, true, false, false, true);
            Assert.IsFalse(_label.enabled);
            var panel = _view.Canvas.GetComponentsInChildren<RectTransform>(true)
                .Single(t => t.name == "SpectatorControlsPanel");
            Assert.IsTrue(panel.GetComponentsInChildren<RectTransform>().Any(t => t.name == "CleanFeedKey"));
            Assert.IsTrue(panel.GetComponentsInChildren<RectTransform>().Any(t => t.name == "SpectatorControlsClose"));
        }
        [Test] public void PlayersDoNotReceiveTheSpectatorRestoreHint()
        { _view.Tick(null, false, false, false, false); Assert.IsFalse(_label.enabled); }
        [UnityTest] public IEnumerator ControlsAreGroupedAndCloseWithoutLeavingAFooter()
        {
            _view.Tick(null, true, false, false, true);
            var panel = _view.Canvas.GetComponentsInChildren<RectTransform>(true)
                .Single(t => t.name == "SpectatorControlsPanel");
            Assert.IsTrue(panel.gameObject.activeSelf);
            CollectionAssert.AreEquivalent(new[] { "WATCH", "FLY", "MATCH TIME", "CAMERA TOOLS" },
                panel.GetComponentsInChildren<Text>().Where(t => t.name == "GroupHeading").Select(t => t.text).ToArray());
            Assert.IsFalse(_label.enabled);
            yield return TumpUiCapture.Capture("spectator-controls-grouped-960", _view.Canvas, 960, 540, false);
            yield return TumpUiCapture.Capture("spectator-controls-grouped-wide", _view.Canvas, 1600, 680, false);
            _view.Tick(null, true, false, false, false);
            Assert.IsFalse(panel.gameObject.activeSelf);
            Assert.IsFalse(_label.enabled);
        }
    }
}
