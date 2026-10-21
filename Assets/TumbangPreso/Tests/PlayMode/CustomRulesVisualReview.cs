using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class CustomRulesVisualReview
    {
        private CustomRules _savedRules;
        private CustomGameScreen _screen;

        [UnitySetUp]
        public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _savedRules = SceneFlow.SelectedRules.Clone();
        }

        [UnityTearDown]
        public IEnumerator After()
        {
            if (_screen != null) Object.Destroy(_screen.gameObject);
            if (_savedRules != null) SceneFlow.SetSelectedRules(_savedRules);
            yield return PlayModeWorld.Reset();
        }

        [UnityTest]
        public IEnumerator MatchAndPrivateRoomPagesRenderWithTheirExistingControls()
        {
            var rules = CustomGameRules.Defaults(GameMode.HeroStrike);
            rules.Rounds = 4; rules.RoundSeconds = 90; rules.Private = true;
            SceneFlow.SetSelectedRules(rules);
            SceneFlow.Networked = false;
            _screen = CustomGameScreen.Ensure(); _screen.Open();
            yield return null; yield return null;
            var canvas = (Canvas)typeof(CustomGameScreen).GetField("_canvas",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_screen);
            Assert.IsNotNull(canvas);
            foreach (var size in new[] { new Vector2Int(1920,1080), new Vector2Int(2560,1440), new Vector2Int(3840,2160) })
                yield return TumpUiCapture.Capture("Custom-rules-match-" + size.x,
                    canvas, size.x, size.y, false, checkActionBounds:true);

            var tab = (Button)typeof(CustomGameScreen).GetField("_ownerRoomTab",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_screen);
            tab.onClick.Invoke(); yield return null;
            var password = (InputField)typeof(CustomGameScreen).GetField("_password",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_screen);
            Assert.IsTrue(password.gameObject.activeInHierarchy, "The private-room field disappeared.");
            foreach (var size in new[] { new Vector2Int(1920,1080), new Vector2Int(3840,2160) })
                yield return TumpUiCapture.Capture("Custom-rules-private-room-" + size.x,
                    canvas, size.x, size.y, false, checkActionBounds:true);
            Assert.AreEqual(4, SceneFlow.SelectedRules.Rounds);
            Assert.AreEqual(90, SceneFlow.SelectedRules.RoundSeconds);
        }
    }
}
