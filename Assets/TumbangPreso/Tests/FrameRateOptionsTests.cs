using NUnit.Framework;
using TumbangPreso.Settings;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class FrameRateOptionsTests
    {
        [Test]
        public void FrameLimitFollowsDisplaySyncAndRetainsItsPreference()
        {
            int sync=QualitySettings.vSyncCount,target=Application.targetFrameRate,setting=FrameRateOptions.Current;
            float fixedStep=Time.fixedDeltaTime;
            try
            {
                QualitySettings.vSyncCount=0;FrameRateOptions.Apply(120);
                Assert.AreEqual(120,Application.targetFrameRate);Assert.IsFalse(FrameRateOptions.ControlledByDisplay);
                QualitySettings.vSyncCount=1;FrameRateOptions.Apply(120);
                Assert.AreEqual(-1,Application.targetFrameRate);Assert.AreEqual(120,FrameRateOptions.Current);
                Assert.IsTrue(FrameRateOptions.ControlledByDisplay);
                QualitySettings.vSyncCount=0;FrameRateOptions.Apply(FrameRateOptions.Current);
                Assert.AreEqual(120,Application.targetFrameRate);
                FrameRateOptions.Apply(0);Assert.AreEqual(-1,Application.targetFrameRate);
                Assert.AreEqual(fixedStep,Time.fixedDeltaTime,"Display pacing changed the gameplay simulation step");
            }
            finally{QualitySettings.vSyncCount=sync;FrameRateOptions.Apply(setting);Application.targetFrameRate=target;}
        }
        [TestCase(-1)][TestCase(7)][TestCase(9999)]
        public void UnsupportedSavedLimitsUseTheSafeExistingDefault(int value)
            =>Assert.AreEqual(FrameRateOptions.Default,FrameRateOptions.Normalize(value));
        [TestCase("50",50)][TestCase("144",144)][TestCase("bad",0)][TestCase("-1",0)]
        public void ExplicitOperatorCapRemainsIndependentOfTheMenuChoices(string argument,int expected)
            =>Assert.AreEqual(expected,FrameRateOptions.ReadOperatorLimit(new[]{"game","-tp-framecap",argument}));
    }
}
