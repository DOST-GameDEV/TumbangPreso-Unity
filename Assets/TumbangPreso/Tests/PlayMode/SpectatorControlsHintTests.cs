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
        [UnityTest] public IEnumerator HiddenControlsRetainTheBoundRestoreHint()
        {
            _view.Tick(null, true, false, false, false);
            Assert.IsTrue(_label.enabled, "Hiding controls also erased the only restore hint.");
            StringAssert.Contains(Hud.KeyLabelFor("SpectatorControls"), _label.text);
            StringAssert.Contains("show controls", _label.text);
            StringAssert.DoesNotContain("AUTOPILOT", _label.text);
            yield return TumpUiCapture.Capture("spectator-restore-hint-960", _view.Canvas, 960, 540, false);
            yield return TumpUiCapture.Capture("spectator-restore-hint-wide", _view.Canvas, 1600, 680, false);
        }
        [Test] public void VisibleControlsKeepTheirHideAndCleanFeedHints()
        {
            _view.Tick(null, true, false, false, true);
            Assert.IsTrue(_label.enabled); StringAssert.Contains("hide controls", _label.text);
            StringAssert.Contains(Hud.KeyLabelFor("CleanFeed"), _label.text);
        }
        [Test] public void PlayersDoNotReceiveTheSpectatorRestoreHint()
        { _view.Tick(null, false, false, false, false); Assert.IsFalse(_label.enabled); }
    }
}
