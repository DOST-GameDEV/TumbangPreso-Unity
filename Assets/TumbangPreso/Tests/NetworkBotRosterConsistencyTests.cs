using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class NetworkBotRosterConsistencyTests
    {
        private sealed class Peer : INetProvider
        {
            public bool IsNetworked { get; set; } = true;
            public bool IsHost { get; set; } = true;
            public int LocalSlot { get; set; }
            public int LocalPeerId => LocalSlot + 1;
            public bool IsSeatlessReferee => false;
        }
        private GameObject _root;
        private MatchInstaller _installer;
        private INetProvider _previousProvider;
        private bool _guided, _spectator, _allBots;
        private int _pick, _seat;
        private GameMode _mode;
        private Peer _peer;
        [SetUp] public void Before()
        {
            _previousProvider = NetAuthority.Provider; _guided = GameLaunch.GuidedTutorial;
            _spectator = GameLaunch.Spectator; _allBots = GameLaunch.AllBots;
            _pick = Settings.SettingsStore.Current.CharacterPick; _seat = GameLaunch.SoloSeat;
            _mode = SceneFlow.SelectedMode;
            _peer = new Peer(); NetAuthority.Provider = _peer;
            GameLaunch.GuidedTutorial = GameLaunch.Spectator = GameLaunch.AllBots = false;
            GameLaunch.SoloSeat = 1;
            _root = new GameObject("Network bot roster selection"); _root.SetActive(false);
            _installer = _root.AddComponent<MatchInstaller>();
        }
        [TearDown] public void After()
        {
            Object.DestroyImmediate(_root); NetAuthority.Provider = _previousProvider;
            GameLaunch.GuidedTutorial = _guided; GameLaunch.Spectator = _spectator;
            GameLaunch.AllBots = _allBots; GameLaunch.SoloSeat = _seat;
            Settings.SettingsStore.Current.CharacterPick = _pick; SceneFlow.SelectedMode = _mode;
        }
        private int Pick(int slot) => (int)typeof(MatchInstaller)
            .GetMethod("AiCharacterIndex", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(_installer, new object[] { slot });

        [TestCase(GameMode.Classic), TestCase(GameMode.HeroStrike)]
        public void HostAndClientChooseTheSameEmptySeatDespiteDifferentLocalPicks(GameMode mode)
        {
            SceneFlow.SelectedMode = mode;
            var host = new int[Balance.PlayerCount]; Settings.SettingsStore.Current.CharacterPick = 1;
            for (int slot = 0; slot < host.Length; slot++) host[slot] = Pick(slot);
            _peer.IsHost = false; _peer.LocalSlot = 1; Settings.SettingsStore.Current.CharacterPick = 7;
            for (int slot = 0; slot < host.Length; slot++)
            {
                Assert.AreEqual(host[slot], Pick(slot), $"{mode} bot seat {slot} changes with the viewing peer's settings");
                Assert.AreEqual(MatchInstaller.ResolveAiCharacterIndex(slot, -1, mode), Pick(slot));
            }
        }
        [TestCase(GameMode.Classic), TestCase(GameMode.HeroStrike)]
        public void OfflineBotsKeepTheSavedHumanPickRotation(GameMode mode)
        {
            _peer.IsNetworked = false; SceneFlow.SelectedMode = mode;
            Settings.SettingsStore.Current.CharacterPick = 4;
            for (int slot = 0; slot < Balance.PlayerCount; slot++)
                Assert.AreEqual(MatchInstaller.ResolveAiCharacterIndex(slot, 4, mode), Pick(slot));
        }
    }
}
