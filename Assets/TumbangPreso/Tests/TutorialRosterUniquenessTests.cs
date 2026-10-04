using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class TutorialRosterUniquenessTests
    {
        private GameObject _root;
        private MatchInstaller _installer;
        private bool _guided, _allBots, _spectator;
        private int _seat, _pick;
        private GameMode _mode;
        private INetProvider _provider;

        [SetUp] public void Before()
        {
            _guided = GameLaunch.GuidedTutorial; _allBots = GameLaunch.AllBots;
            _spectator = GameLaunch.Spectator; _seat = GameLaunch.SoloSeat;
            _pick = Settings.SettingsStore.Current.CharacterPick; _mode = SceneFlow.SelectedMode;
            _provider = NetAuthority.Provider; NetAuthority.Provider = null;
            GameLaunch.AllBots = false; GameLaunch.Spectator = false;
            _root = new GameObject("Tutorial roster policy"); _root.SetActive(false);
            _installer = _root.AddComponent<MatchInstaller>();
        }
        [TearDown] public void After()
        {
            Object.DestroyImmediate(_root);
            GameLaunch.GuidedTutorial = _guided; GameLaunch.AllBots = _allBots;
            GameLaunch.Spectator = _spectator; GameLaunch.SoloSeat = _seat;
            Settings.SettingsStore.Current.CharacterPick = _pick; SceneFlow.SelectedMode = _mode;
            NetAuthority.Provider = _provider;
        }
        private int Pick(int seat) => (int)typeof(MatchInstaller)
            .GetMethod("AiCharacterIndex", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(_installer, new object[] { seat });

        [TestCase(0), TestCase(1), TestCase(2), TestCase(3), TestCase(4)]
        [TestCase(5), TestCase(6), TestCase(7), TestCase(8)]
        public void EveryTutorialStudentGetsThreeDistinctHeroPartners(int pick)
        {
            Assert.Less(pick, Roster.HeroPeople.Count);
            SceneFlow.SelectedMode = GameMode.HeroStrike; GameLaunch.GuidedTutorial = true;
            Settings.SettingsStore.Current.CharacterPick = pick;
            for (int human = 0; human < Balance.PlayerCount; human++)
            {
                GameLaunch.SoloSeat = human;
                var seen = new HashSet<int> { pick };
                for (int slot = 0; slot < Balance.PlayerCount; slot++)
                    if (slot != human)
                        Assert.IsTrue(seen.Add(Pick(slot)), $"Student {pick} at seat {human} repeats a tutorial partner at {slot}");
            }
        }
        [Test] public void OrdinaryHeroMatchKeepsItsExistingSelectionPolicy()
        {
            SceneFlow.SelectedMode = GameMode.HeroStrike; GameLaunch.GuidedTutorial = false;
            GameLaunch.SoloSeat = 1; Settings.SettingsStore.Current.CharacterPick = 4;
            for (int slot = 0; slot < Balance.PlayerCount; slot++)
                Assert.AreEqual(MatchInstaller.ResolveAiCharacterIndex(slot, 4, GameMode.HeroStrike), Pick(slot));
        }
        [Test] public void ClassicTutorialPartnersRemainDistinct()
        {
            SceneFlow.SelectedMode = GameMode.Classic; GameLaunch.GuidedTutorial = true;
            GameLaunch.SoloSeat = 1; Settings.SettingsStore.Current.CharacterPick = 4;
            var seen = new HashSet<int> { 4 };
            for (int slot = 0; slot < Balance.PlayerCount; slot++)
                if (slot != 1) Assert.IsTrue(seen.Add(Pick(slot)));
        }
    }
}
