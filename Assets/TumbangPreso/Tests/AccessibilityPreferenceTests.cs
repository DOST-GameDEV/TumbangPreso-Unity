using NUnit.Framework;
using TumbangPreso.Settings;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class AccessibilityPreferenceTests
    {
        [Test] public void OldProfilesKeepTheOriginalLensAndHoldControls()
        {
            var settings = JsonUtility.FromJson<GameSettings>("{}");
            settings.Validate();
            Assert.AreEqual(95f, settings.FirstPersonFov);
            Assert.AreEqual(1f, settings.HudScale); Assert.IsFalse(settings.LargerText);
            Assert.IsFalse(settings.ToggleSprint); Assert.IsFalse(settings.ToggleRestore);
        }
        [TestCase(float.NaN, 95f)]
        [TestCase(float.PositiveInfinity, 95f)]
        [TestCase(-100f, 75f)]
        [TestCase(300f, 110f)]
        public void LensPreferenceAlwaysRemainsFiniteAndBounded(float input, float expected)
        {
            var settings = new GameSettings { FirstPersonFov = input }; settings.Validate();
            Assert.AreEqual(expected, settings.FirstPersonFov);
        }
        [TestCase(float.NaN, 1f)] [TestCase(float.PositiveInfinity, 1f)]
        [TestCase(-100f, 1f)] [TestCase(300f, 1.2f)]
        public void HudScaleCannotInvalidateCanvasGeometry(float input, float expected)
        {
            var settings = new GameSettings { HudScale = input }; settings.Validate();
            Assert.AreEqual(expected, settings.HudScale);
        }
    }
}
