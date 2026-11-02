using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class StatusTypographyTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _owner;
        private Canvas _canvas;
        private TumpMatchReadout _view;
        private CharacterMotor _actor;
        private bool _motion;
        private float _scale;
        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _motion = Settings.SettingsStore.Current.ReducedUiMotion; _scale = Settings.SettingsStore.Current.HudScale;
            Settings.SettingsStore.Current.ReducedUiMotion = true; Settings.SettingsStore.Current.HudScale = 1;
            _owner = new GameObject("Status typography review");
            _canvas = OwnerUiLayout.Canvas(_owner.transform, "StatusTypographyCanvas", 100);
            _view = _owner.AddComponent<TumpMatchReadout>();
            typeof(TumpMatchReadout).GetField("_root", Private).SetValue(_view, (RectTransform)_canvas.transform);
            typeof(TumpMatchReadout).GetMethod("BuildStatusChips", Private).Invoke(_view, null);
            var actor = new GameObject("Chilled player"); actor.transform.SetParent(_owner.transform);
            _actor = actor.AddComponent<CharacterMotor>(); _actor.enabled = false;
            _actor.PlayerSlot = 1; _actor.RoundActive = true; _actor.ApplyChilled(30);
            yield return null;
        }
        [UnityTearDown] public IEnumerator After()
        {
            if (_canvas != null) Object.DestroyImmediate(_canvas.gameObject);
            if (_owner != null) Object.DestroyImmediate(_owner);
            Settings.SettingsStore.Current.ReducedUiMotion = _motion; Settings.SettingsStore.Current.HudScale = _scale;
            yield return PlayModeWorld.Reset();
        }
        private void Paint() => typeof(TumpMatchReadout).GetMethod("PaintStatusChips", Private).Invoke(_view, new object[] { _actor, true });
        private static int SlotOf(StatusKind kind)
        {
            for (int i = 0; i < StatusRules.All.Count; i++) if (StatusRules.All[i].Kind == kind) return i;
            return -1;
        }
        private int Landing => (int)typeof(TumpMatchReadout).GetProperty("StatusLandingSlot", Private).GetValue(_view);
        /// <summary>Pretends the current words have been up for this long, so a test does not wait in real time.</summary>
        private void Age(float seconds)
        {
            var at = typeof(TumpMatchReadout).GetField("_statusLandingAt", Private);
            at.SetValue(_view, (float)at.GetValue(_view) - seconds);
        }
        private void Step(float seconds) => typeof(CharacterMotor).GetMethod("StepStatuses", Private).Invoke(_actor, new object[] { seconds });

        [UnityTest] public IEnumerator LandingShowsItsRingNameDescriptionAndWholeSeconds()
        {
            _actor.ClearStatuses(); _actor.ApplyWhirled(2.5f);
            Assert.IsTrue(_actor.IsWhirled);
            Paint(); yield return null; Canvas.ForceUpdateCanvases();
            var chip = _canvas.GetComponentsInChildren<RectTransform>().Single(t => t.name == "StatusChip" + SlotOf(StatusKind.Whirled));
            Assert.IsTrue(chip.gameObject.activeSelf, "The ring is on while the status is.");
            var name = chip.GetComponentsInChildren<Text>().Single(t => t.name == "StatusName");
            Assert.AreEqual("WHIRLED", name.text, "The name is the existing text in upper case.");
            Assert.IsTrue(name.isActiveAndEnabled, "A landing shows its name over the ring.");
            Assert.AreEqual(StatusRules.For(StatusKind.Whirled).Tooltip, chip.GetComponentsInChildren<Text>().Single(t => t.name == "StatusTooltip").text);
            Assert.AreEqual("3", chip.GetComponentsInChildren<Text>().Single(t => t.name == "StatusTime").text, "Whole seconds, rounded up.");
            var icon = chip.GetComponentsInChildren<Image>(true).Single(t => t.name == "StatusIcon");
            Assert.IsTrue(icon.isActiveAndEnabled && icon.sprite != null, "The effect keeps its own icon, unchanged.");
            Assert.IsTrue(chip.GetComponentsInChildren<Image>(true).Single(t => t.name == "StatusRim").enabled, "The landed ring wears the cut rim.");
        }

        [UnityTest] public IEnumerator ZappedLastsTooBrieflyForWordsButStillGetsItsRing()
        {
            _actor.ClearStatuses(); Paint(); yield return null;
            _actor.ApplyZapped(2.5f);
            Assert.IsTrue(_actor.IsZapped);
            Paint(); yield return null;
            Assert.AreEqual(-1, Landing, "An ability lock runs about a second: the ring pops and no words are shown.");
            var chip = _canvas.GetComponentsInChildren<RectTransform>().Single(t => t.name == "StatusChip" + SlotOf(StatusKind.Zapped));
            Assert.IsTrue(chip.gameObject.activeSelf);
            var icon = chip.GetComponentsInChildren<Image>(true).Single(t => t.name == "StatusIcon");
            var fallback = chip.GetComponentsInChildren<OwnerUiGlyph>(true).Single(t => t.name == "StatusLock");
            Assert.IsTrue((icon.isActiveAndEnabled && icon.sprite != null) || fallback.isActiveAndEnabled,
                "An ability lock must retain a visible icon when its sprite is unavailable.");
        }

        [UnityTest] public IEnumerator ExpiredStatusDoesNotLeaveARing()
        {
            _actor.ClearStatuses(); Paint(); yield return null;
            Assert.IsFalse(_canvas.GetComponentsInChildren<Text>().Any(t => t.name == "StatusName" && t.isActiveAndEnabled));
            _actor.ApplyWhirled(1);
            Assert.IsTrue(_actor.IsWhirled);
            Step(1.1f);
            Assert.IsFalse(_actor.IsWhirled);
            Paint(); yield return null;
            Assert.IsFalse(_canvas.GetComponentsInChildren<Text>().Any(t => t.name == "StatusName" && t.isActiveAndEnabled));
            Assert.IsFalse(_canvas.GetComponentsInChildren<RectTransform>(true).Single(t => t.name == "StatusChip" + SlotOf(StatusKind.Whirled)).gameObject.activeSelf);
        }

        [UnityTest] public IEnumerator ChilledDescriptionWrapsAt440AndTheNameSitsAboveIt()
        {
            Paint(); yield return null; Canvas.ForceUpdateCanvases();
            var tip = _canvas.GetComponentsInChildren<Text>().Single(t => t.name == "StatusTooltip" && t.isActiveAndEnabled);
            Assert.AreEqual("Reduced Movement Speed", tip.text);
            Assert.AreEqual(440, tip.rectTransform.rect.width, .01f);
            Assert.LessOrEqual(tip.preferredHeight, tip.rectTransform.rect.height + 1);
            var name = tip.transform.parent.GetComponentsInChildren<Text>().Single(t => t.name == "StatusName");
            Assert.GreaterOrEqual(name.rectTransform.anchoredPosition.y, tip.rectTransform.anchoredPosition.y + tip.rectTransform.rect.height,
                "The name rises to sit above the description.");
            yield return TumpUiCapture.Capture("StatusTypography-Chilled", _canvas, 960, 540, false);
        }

        [UnityTest] public IEnumerator EveryDescriptionFitsItsBoxAndEveryNameFitsItsWidth()
        {
            _actor.ApplyWhirled(30); _actor.ApplyRooted(30); _actor.ApplyConcussed(30);
            _actor.ApplyFeared(Vector3.zero, 30); _actor.ApplyDisoriented(30); _actor.ApplyVulnerable(30);
            _actor.ApplyDrained(30); _actor.ApplyHexed(30); _actor.ApplyHaunted();
            Paint(); yield return null; Canvas.ForceUpdateCanvases();
            var chips = _canvas.GetComponentsInChildren<RectTransform>(true).Where(t => t.name.StartsWith("StatusChip")).ToArray();
            Assert.AreEqual(StatusRules.All.Count, chips.Length);
            foreach (var chip in chips)
            {
                var tip = chip.GetComponentsInChildren<Text>(true).Single(t => t.name == "StatusTooltip");
                Assert.LessOrEqual(tip.preferredHeight, tip.rectTransform.rect.height + 1, tip.text);
                var title = chip.GetComponentsInChildren<Text>(true).Single(t => t.name == "StatusName");
                Assert.LessOrEqual(title.preferredWidth, title.rectTransform.rect.width + 1, title.text);
                Assert.GreaterOrEqual(title.rectTransform.anchoredPosition.y, tip.rectTransform.anchoredPosition.y + tip.rectTransform.rect.height);
            }
            yield return TumpUiCapture.Capture("StatusTypography-Dense", _canvas, 960, 540, false);
        }

        [UnityTest] public IEnumerator LandingsTakeTurnsSoonestToExpireFirst()
        {
            _actor.ClearStatuses(); Paint(); yield return null;
            _actor.ApplyWhirled(30); _actor.ApplyRooted(10); _actor.ApplyConcussed(20);
            Paint();
            Assert.AreEqual(SlotOf(StatusKind.Rooted), Landing, "The effect with least time left goes first.");
            Age(2.1f); Paint();
            Assert.AreEqual(SlotOf(StatusKind.Concussed), Landing, "Others are waiting, so each turn is about two seconds.");
            Age(2.1f); Paint();
            Assert.AreEqual(SlotOf(StatusKind.Whirled), Landing);
            Age(2.1f); Paint();
            Assert.AreEqual(SlotOf(StatusKind.Whirled), Landing, "The last one holds for three seconds.");
            Age(1f); Paint();
            Assert.AreEqual(-1, Landing, "And then the words go.");
        }

        [UnityTest] public IEnumerator WordsNeverOutlastTheirEffectAndAnEffectThatEndsWhileWaitingIsDropped()
        {
            _actor.ClearStatuses(); Paint(); yield return null;
            _actor.ApplyConcussed(2.5f); _actor.ApplyWhirled(2f);
            Paint();
            Assert.AreEqual(SlotOf(StatusKind.Whirled), Landing);
            Step(2.6f); Paint();
            Assert.AreEqual(-1, Landing, "The words left with the effect, and the one that ended while waiting was not shown late.");
        }

        [UnityTest] public IEnumerator UnderOneAndAHalfSecondsLeftOnlyPopsTheRing()
        {
            _actor.ClearStatuses(); Paint(); yield return null;
            _actor.ApplyWhirled(1.2f); Paint();
            Assert.AreEqual(-1, Landing, "No time to read, so no words.");
            Assert.IsTrue(_canvas.GetComponentsInChildren<RectTransform>().Single(t => t.name == "StatusChip" + SlotOf(StatusKind.Whirled)).gameObject.activeSelf,
                "The ring is still there, and popped.");
        }

        [UnityTest] public IEnumerator ARefreshedEffectDoesNotShowItsWordsAgain()
        {
            _actor.ClearStatuses(); Paint(); yield return null;
            _actor.ApplyRooted(10); Paint();
            Assert.AreEqual(SlotOf(StatusKind.Rooted), Landing);
            Age(3.1f); Paint();
            Assert.AreEqual(-1, Landing);
            _actor.ApplyRooted(10); Paint();
            Assert.AreEqual(-1, Landing, "Putting the time back is not a new landing.");
        }
    }
}
