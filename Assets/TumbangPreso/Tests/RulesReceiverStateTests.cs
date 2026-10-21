using System;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class RulesReceiverStateTests
    {
        private sealed class Peer : INetProvider
        {
            public bool Host;
            public bool IsHost => Host;
            public bool IsNetworked => true;
            public int LocalSlot => 1;
            public int LocalPeerId => 1;
            public bool IsSeatlessReferee => false;
        }
        private GameObject _root;
        private MatchRpc _rpc;
        private INetProvider _previous;
        private CustomRules _rules;
        private string _settings;
        private Peer _peer;
        private bool _networked;
        private bool _pinned;

        [SetUp] public void Before()
        {
            _previous = NetAuthority.Provider; _peer = new Peer(); NetAuthority.Provider = _peer;
            _rules = SceneFlow.SelectedRules.Clone(); _networked = SceneFlow.Networked;
            _pinned = SceneFlow.RulesPinned;
            _settings = JsonUtility.ToJson(Settings.SettingsStore.Current);
            SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            SceneFlow.Networked = true;
            _root = new GameObject("Dormant rules receiver without lobby view");
            _root.SetActive(false); _rpc = _root.AddComponent<MatchRpc>();
        }
        [TearDown] public void After()
        {
            UnityEngine.Object.DestroyImmediate(_root);
            SceneFlow.AdoptRemoteRules(_rules); SceneFlow.Networked = _networked;
            if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
            JsonUtility.FromJsonOverwrite(_settings, Settings.SettingsStore.Current);
            NetAuthority.Provider = _previous;
        }
        private void Deliver(ulong sender, CustomRules rules, bool empty = false)
        {
            using var writer = new FastBufferWriter(256, Allocator.Temp);
            if (!empty) writer.WriteValueSafe(CustomGameRules.ToWire(rules));
            using var reader = new FastBufferReader(writer, Allocator.Temp);
            typeof(MatchRpc).GetMethod("OnSyncRulesMsg", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_rpc, new object[] { sender, reader });
        }
        [TestCase(GameMode.HeroStrike, 4, 30)]
        [TestCase(GameMode.Classic, 6, 45)]
        public void HostRulesUpdateStateBeforeNotifyingViewsWithoutSavingLocalPreferences(GameMode mode, int rounds, int seconds)
        {
            string preference = Settings.SettingsStore.Current.CustomRulesWire;
            var remote = CustomGameRules.Defaults(mode); remote.Rounds = rounds; remote.RoundSeconds = seconds;
            int observedRounds = -1;
            Action<string> observer = wire => observedRounds = SceneFlow.SelectedRoundCount;
            MatchRpc.OnRulesChanged += observer;
            try { Deliver(0, remote); }
            finally { MatchRpc.OnRulesChanged -= observer; }
            Assert.AreEqual(rounds, SceneFlow.SelectedRoundCount, "Receiving rules depended on a lobby view applying them.");
            Assert.AreEqual(rounds, observedRounds, "Views were notified before shared rules state was ready.");
            Assert.AreEqual(mode, SceneFlow.SelectedMode); Assert.AreEqual(seconds, SceneFlow.SelectedRoundSeconds);
            Assert.AreEqual(preference, Settings.SettingsStore.Current.CustomRulesWire, "Remote rules overwrote local saved preferences.");
        }
        [Test] public void AnotherPeerCannotChangeRules()
        {
            var remote = CustomGameRules.Defaults(GameMode.HeroStrike); remote.Rounds = 4;
            Deliver(9, remote); Assert.AreEqual(8, SceneFlow.SelectedRoundCount);
        }
        [Test] public void EmptyPacketCannotChangeRules()
        {
            Deliver(0, CustomGameRules.Defaults(GameMode.HeroStrike), true);
            Assert.AreEqual(8, SceneFlow.SelectedRoundCount);
        }
        [Test] public void ListenHostIgnoresItsOwnRulesMessage()
        {
            _peer.Host = true;
            var remote = CustomGameRules.Defaults(GameMode.HeroStrike); remote.Rounds = 4;
            Deliver(0, remote); Assert.AreEqual(8, SceneFlow.SelectedRoundCount);
        }
    }
}
