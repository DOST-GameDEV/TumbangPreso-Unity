using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class CustomRulesLiveRefreshTests
    {
        private sealed class Peer : INetProvider
        {
            public bool Host;
            public bool IsHost => Host;
            public bool IsNetworked => true;
            public int LocalSlot => Host ? 0 : 1;
            public int LocalPeerId => LocalSlot;
            public bool IsSeatlessReferee => false;
        }
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private INetProvider _provider;
        private bool _networked, _pinned;
        private CustomRules _before;
        private string _savedWire;
        private int _savedFormat;
        private GameObject _rpcRoot, _screenRoot;
        private MatchRpc _rpc;
        private CustomGameScreen _screen;
        private Peer _peer;
        private object Field(string name) => typeof(CustomGameScreen).GetField(name, Hidden).GetValue(_screen);
        private CustomRules Editing => (CustomRules)Field("_editing");
        private Dictionary<string, Text> Values => (Dictionary<string, Text>)Field("_ownerValues");

        [UnitySetUp] public IEnumerator Before()
        {
            _provider = NetAuthority.Provider; _networked = SceneFlow.Networked;
            _pinned = SceneFlow.RulesPinned; _before = SceneFlow.SelectedRules.Clone();
            _savedWire = Settings.SettingsStore.Current.CustomRulesWire;
            _savedFormat = Settings.SettingsStore.Current.MatchFormat;
            yield return PlayModeWorld.Reset();
            _peer = new Peer(); NetAuthority.Provider = _peer; SceneFlow.Networked = true;
            var rules = CustomGameRules.Defaults(GameMode.HeroStrike);
            rules.Rounds = 6; rules.RoundSeconds = 90; rules.Bots = 0;
            SceneFlow.AdoptRemoteRules(rules);
            _rpcRoot = new GameObject("Rules receive callback fixture"); _rpcRoot.SetActive(false);
            _rpc = _rpcRoot.AddComponent<MatchRpc>();
            _screenRoot = new GameObject("Actual readonly rules view");
            _screen = _screenRoot.AddComponent<CustomGameScreen>(); _screen.Open();
        }
        [UnityTearDown] public IEnumerator After()
        {
            if (_screenRoot != null) Object.DestroyImmediate(_screenRoot);
            if (_rpcRoot != null) Object.DestroyImmediate(_rpcRoot);
            yield return PlayModeWorld.Reset();
            SceneFlow.AdoptRemoteRules(_before);
            if (_pinned) SceneFlow.PinSelectedRules(_before); else SceneFlow.UnpinSelectedRules();
            SceneFlow.Networked = _networked; NetAuthority.Provider = _provider;
            Settings.SettingsStore.Current.CustomRulesWire = _savedWire;
            Settings.SettingsStore.Current.MatchFormat = _savedFormat;
        }
        private static CustomRules Changed()
        {
            var rules = CustomGameRules.Defaults(GameMode.HeroStrike);
            rules.Rounds = 4; rules.RoundSeconds = 30;
            rules.Bots = CustomGameRules.MaxBots; rules.BotDifficulty = (int)Difficulty.Normal;
            rules.MapVote = true;
            return rules;
        }
        private void Receive(CustomRules rules, ulong sender = NetworkManager.ServerClientId)
        {
            using var writer = new FastBufferWriter(1024, Allocator.Temp);
            writer.WriteValueSafe(CustomGameRules.ToWire(rules));
            using var reader = new FastBufferReader(writer, Allocator.Temp);
            typeof(MatchRpc).GetMethod("OnSyncRulesMsg", Hidden).Invoke(_rpc, new object[] { sender, reader });
        }
        private void UnchangedPreferences()
        {
            Assert.AreEqual(_savedWire, Settings.SettingsStore.Current.CustomRulesWire);
            Assert.AreEqual(_savedFormat, Settings.SettingsStore.Current.MatchFormat);
        }
        [Test] public void OpenMatchPageFollowsActualRemoteRulesReceive()
        {
            Receive(Changed());
            Assert.AreEqual(4, SceneFlow.SelectedRules.Rounds, "Receive path must first adopt host rules.");
            Assert.AreEqual("4", Values["Rounds"].text, "An already-open MATCH page must display new host rounds.");
            Assert.AreEqual("30s", Values["Seconds"].text);
            Assert.IsFalse((bool)Field("_roomOpen"));
            UnchangedPreferences();
        }
        [Test] public void OpenRoomPageRefreshesBotsWithoutLosingSelectedTab()
        {
            typeof(CustomGameScreen).GetMethod("ShowOwnerRulesPage", Hidden).Invoke(_screen, new object[] { true });
            Receive(Changed());
            Assert.AreEqual(CustomGameRules.MaxBots, SceneFlow.SelectedRules.Bots);
            Assert.AreEqual("NORMAL", Values["Bots"].text, "Already-open ROOM must display current bot policy.");
            Assert.AreEqual("ON", Values["MapVote"].text);
            Assert.IsTrue((bool)Field("_roomOpen"));
            Assert.IsTrue(((RectTransform)Field("_ownerRoomPage")).gameObject.activeSelf);
            Assert.IsFalse(((RectTransform)Field("_ownerMatchPage")).gameObject.activeSelf);
            UnchangedPreferences();
        }
        [Test] public void ClosedScreenAdoptsCurrentRulesOnReopen()
        {
            _screen.Close(); Receive(Changed());
            Assert.IsFalse(_screen.IsOpen); Assert.AreEqual(6, Editing.Rounds);
            _screen.Open(); Assert.AreEqual("4", Values["Rounds"].text);
            UnchangedPreferences();
        }
        [Test] public void HostBroadcastKeepsItsWorkingCopy()
        {
            _peer.Host = true; _screen.Open(); Editing.Rounds = 12;
            _rpc.SelectRulesServerRpc(CustomGameRules.ToWire(Changed()));
            Assert.AreEqual(12, Editing.Rounds, "Host broadcast must not clobber its working copy.");
            UnchangedPreferences();
        }
        [Test] public void OfflineEditorKeepsItsWorkingCopy()
        {
            SceneFlow.Networked = false; _screen.Open(); Editing.Rounds = 12;
            Receive(Changed()); Assert.AreEqual(12, Editing.Rounds);
            UnchangedPreferences();
        }
        [Test] public void DisableAndReenableRetiresAndRestoresObservation()
        {
            _screen.enabled = false; Receive(Changed());
            Assert.AreEqual(6, Editing.Rounds);
            _screen.enabled = true;
            var rules = Changed(); rules.Rounds = 5; Receive(rules);
            Assert.AreEqual("5", Values["Rounds"].text);
            UnchangedPreferences();
        }
        [Test] public void NonHostPacketDoesNotChangeVisibleRules()
        {
            Receive(Changed(), 99);
            Assert.AreEqual(6, Editing.Rounds); Assert.AreEqual(6, SceneFlow.SelectedRules.Rounds);
            UnchangedPreferences();
        }
    }
}
