using NUnit.Framework;
using TumbangPreso.Settings;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class GraphicsProfileTests
    {
        [Test]
        public void OlderSettingsKeepAUsableDefaultAndNewPicksSurviveSaving()
        {
            var older = JsonUtility.FromJson<GameSettings>("{\"Fullscreen\":false}");
            older.Validate();
            Assert.AreEqual(GraphicsProfiles.Default, older.GraphicsQuality);

            older.GraphicsQuality = 0;
            var restored = JsonUtility.FromJson<GameSettings>(JsonUtility.ToJson(older));
            restored.Validate();
            Assert.AreEqual(0, restored.GraphicsQuality);

            restored.GraphicsQuality = -100;
            restored.Validate();
            Assert.AreEqual(0, restored.GraphicsQuality);
        }

        [Test]
        public void ADiscardRestoresGraphicsWithoutChangingTheChosenRenderStyle()
        {
            var original = SettingsStore.Current;
            var snapshot = JsonUtility.FromJson<GameSettings>(JsonUtility.ToJson(original));
            var edited = JsonUtility.FromJson<GameSettings>(JsonUtility.ToJson(original));
            snapshot.GraphicsQuality = 2;
            edited.GraphicsQuality = 0;
            try
            {
                SettingsStore.OverrideForTests(edited);
                edited.Apply();
                float lowDistance = QualitySettings.shadowDistance;
                SettingsStore.Restore(snapshot);
                Assert.Greater(QualitySettings.shadowDistance, lowDistance);
                Assert.AreEqual(snapshot.RenderStyle, SettingsStore.Current.RenderStyle);
                Assert.AreEqual(AntiAliasModes.Of(snapshot.AntiAliasMode).Samples,
                                AntiAliasModes.RequestedSamples);
                Assert.GreaterOrEqual(lowDistance,
                    Core.Balance.ConfinementRadius * Mathf.Sqrt(8f),
                    "Low quality must still ground the full competitive court.");
            }
            finally
            {
                SettingsStore.OverrideForTests(original);
                original.Apply();
            }
        }
    }
}
