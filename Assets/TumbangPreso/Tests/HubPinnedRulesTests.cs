using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class HubPinnedRulesTests
    {
        private CustomRules _rules;
        private bool _pinned;
        private string _settings;

        [SetUp] public void Before()
        {
            _rules = CustomGameRules.Parse(CustomGameRules.ToWire(SceneFlow.SelectedRules), SceneFlow.SelectedMode);
            _rules.Password = SceneFlow.SelectedRules.Password;
            _pinned = SceneFlow.RulesPinned;
            _settings = JsonUtility.ToJson(SettingsStore.Current);
        }

        [TearDown] public void After()
        {
            SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
            JsonUtility.FromJsonOverwrite(_settings, SettingsStore.Current);
        }

        [TestCase(GameMode.Classic, 2)]
        [TestCase(GameMode.HeroStrike, 1)]
        public void PassiveHomeChoicePreservesDeliberatelyPinnedRules(GameMode mode, int savedChoice)
        {
            SettingsStore.Current.HubQueueChoice = savedChoice;
            var rules = CustomGameRules.Defaults(mode);
            rules.Rounds = 6; rules.RoundSeconds = 45;
            SceneFlow.PinSelectedRules(rules);
            string wire = CustomGameRules.ToWire(SceneFlow.SelectedRules);
            string preference = SettingsStore.Current.CustomRulesWire;
            HubHome.ApplyChoice();
            Assert.AreEqual(mode, SceneFlow.SelectedMode);
            Assert.IsTrue(SceneFlow.RulesPinned);
            Assert.AreEqual(wire, CustomGameRules.ToWire(SceneFlow.SelectedRules));
            Assert.AreEqual(preference, SettingsStore.Current.CustomRulesWire);
        }

        [Test] public void OrdinaryUnpinnedHomeStillFollowsTheSavedCard()
        {
            SceneFlow.AdoptRemoteRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            SceneFlow.UnpinSelectedRules();
            SettingsStore.Current.HubQueueChoice = 1;
            HubHome.ApplyChoice();
            Assert.AreEqual(GameMode.Classic, SceneFlow.SelectedMode);
            Assert.IsFalse(SceneFlow.RulesPinned);
        }

        [Test] public void ExplicitCardChoiceCanReplacePinnedMode()
        {
            SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            SettingsStore.Current.HubQueueChoice = 2;
            HubHome.Choice = 2;
            Assert.AreEqual(GameMode.HeroStrike, SceneFlow.SelectedMode);
            Assert.IsFalse(SceneFlow.RulesPinned);
        }
    }
}
