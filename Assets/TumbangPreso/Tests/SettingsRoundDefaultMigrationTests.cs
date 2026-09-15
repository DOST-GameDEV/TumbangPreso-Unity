using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class SettingsRoundDefaultMigrationTests
    {
        [Test]
        public void FormerUntouchedClassicPresetUpgradesOnce()
        {
            var old = CustomGameRules.Defaults(GameMode.Classic); old.Rounds = 4;
            string wire = CustomGameRules.ToWire(old);
            var settings = JsonUtility.FromJson<GameSettings>("{\"CustomRulesWire\":\"" + wire + "\"}");
            settings.Validate();
            Assert.AreEqual(8, CustomGameRules.Parse(settings.CustomRulesWire, GameMode.Classic).Rounds);
            Assert.AreEqual(1, settings.MatchDefaultsRevision);
            settings.CustomRulesWire = wire; settings.Validate();
            Assert.AreEqual(4, CustomGameRules.Parse(settings.CustomRulesWire, GameMode.Classic).Rounds,
                "A later explicit custom four-round choice must survive validation.");
        }
        [TestCase(GameMode.Classic, 60)]
        [TestCase(GameMode.HeroStrike, 90)]
        public void ExistingCustomAndOtherModeRulesKeepTheirValues(GameMode mode, int seconds)
        {
            var chosen = CustomGameRules.Defaults(mode); chosen.Rounds = 4; chosen.RoundSeconds = seconds;
            string wire = CustomGameRules.ToWire(chosen);
            var settings = new GameSettings { CustomRulesWire = wire }; settings.Validate();
            Assert.AreEqual(wire, settings.CustomRulesWire);
        }
    }
}
