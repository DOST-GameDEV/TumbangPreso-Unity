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
        [UnityTest] public IEnumerator ChilledDescriptionStaysOnOneReadableLine()
        {
            Paint(); yield return null; Canvas.ForceUpdateCanvases();
            var tip = _canvas.GetComponentsInChildren<Text>().Single(t => t.name == "StatusTooltip");
            Assert.AreEqual("Reduced Movement Speed", tip.text);
            Assert.LessOrEqual(tip.preferredWidth, tip.rectTransform.rect.width);
            Assert.LessOrEqual(tip.preferredHeight, tip.rectTransform.rect.height);
            Assert.GreaterOrEqual(tip.fontSize, 28);
            yield return TumpUiCapture.Capture("StatusTypography-Chilled", _canvas, 960, 540, false);
        }
        [UnityTest] public IEnumerator DenseStatusStackKeepsTextInsideCardsAtBothHudScales()
        {
            _actor.ApplyWhirled(30); _actor.ApplyRooted(30); _actor.ApplyConcussed(30);
            _actor.ApplyFeared(Vector3.zero, 30); _actor.ApplyDisoriented(30); _actor.ApplyVulnerable(30);
            _actor.ApplyDrained(30); _actor.ApplyHexed(30); _actor.ApplyHaunted();
            foreach (float scale in new[] { 1f, 1.2f })
            {
                Settings.SettingsStore.Current.HudScale = scale; Paint(); yield return null; Canvas.ForceUpdateCanvases();
                var chips = _canvas.GetComponentsInChildren<RectTransform>().Where(t => t.name.StartsWith("StatusChip")).ToArray();
                Assert.AreEqual(10, chips.Length);
                foreach (var chip in chips)
                {
                    var tip = chip.GetComponentsInChildren<Text>().Single(t => t.name == "StatusTooltip");
                    Assert.LessOrEqual(tip.preferredHeight, tip.rectTransform.rect.height + 1, tip.text);
                    var title = chip.GetComponentsInChildren<Text>().Single(t => t.name == "StatusName");
                    Assert.LessOrEqual(title.preferredWidth, title.rectTransform.rect.width + 1, title.text);
                    Assert.Less(tip.rectTransform.anchoredPosition.y + tip.rectTransform.rect.height * .5f,
                        title.rectTransform.anchoredPosition.y - title.rectTransform.rect.height * .5f);
                }
                yield return TumpUiCapture.Capture("StatusTypography-Dense-" + scale, _canvas, 960, 540, false);
            }
        }
    }
}
