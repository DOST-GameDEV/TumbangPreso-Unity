using System;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Diagnostics;
using TumbangPreso.Net;
using TumbangPreso.Settings;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class NetIceAdmissionRoleTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private sealed class Role : INetProvider
        {
            public bool IsNetworked { get; set; } = true;
            public bool IsHost { get; set; }
            public int LocalSlot { get; set; }
            public int LocalPeerId => LocalSlot;
            public bool IsSeatlessReferee => false;
        }
        private GameObject _root;
        private NetIceProbe _probe;
        private INetProvider _previousProvider;
        private MatchRpc _previousRpc;
        private RoundDirector _previousRound;
        private object _previousSettings, _previousJoining;
        private FieldInfo _settings, _joining;
        private Role _role;
        private Action _step;
        private int Cheska => Roster.IndexIn(Roster.HeroPeople, "cheska");
        private int Sean => Roster.IndexIn(Roster.HeroPeople, "sean");

        [SetUp] public void Before()
        {
            _previousProvider = NetAuthority.Provider; _previousRpc = MatchRpc.Instance;
            _previousRound = GameServices.Round;
            _settings = typeof(SettingsStore).GetField("_current", BindingFlags.Static | BindingFlags.NonPublic);
            _previousSettings = _settings.GetValue(null);
            _joining = typeof(NetIceProbe).GetField("_joiningSean", BindingFlags.Static | BindingFlags.NonPublic);
            _previousJoining = _joining?.GetValue(null);
            Assert.IsNull(NetSession.Instance, "No transport may dispatch this dormant setup check");
            SettingsStore.OverrideForTests(new GameSettings());
            _root = new GameObject("Dormant ice admission setup"); _root.SetActive(false);
            _probe = _root.AddComponent<NetIceProbe>(); var rpc = _root.AddComponent<MatchRpc>();
            typeof(MatchRpc).GetProperty("Instance").SetValue(null, rpc);
            typeof(GameServices).GetProperty("Round").SetValue(null, null);
            typeof(NetIceProbe).GetField("_seanStarted", Hidden).SetValue(_probe, Time.realtimeSinceStartup);
            _step = (Action)Delegate.CreateDelegate(typeof(Action), _probe, typeof(NetIceProbe).GetMethod("UpdateSean", Hidden));
            _role = new Role(); NetAuthority.Provider = _role;
        }
        [TearDown] public void After()
        {
            NetAuthority.Provider = _previousProvider;
            typeof(MatchRpc).GetProperty("Instance").SetValue(null, _previousRpc);
            typeof(GameServices).GetProperty("Round").SetValue(null, _previousRound);
            _settings?.SetValue(null, _previousSettings);
            if (_joining != null) _joining.SetValue(null, _previousJoining);
            Object.DestroyImmediate(_root);
        }
        private bool PickSent => (bool)typeof(NetIceProbe).GetField("_pickSent", Hidden).GetValue(_probe);
        private void Intent(bool joining)
        {
            // Configure derives this intention from -tp-join / -tp-lobbyjoin.
            // The original has no role latch; the same UpdateSean still runs for its baseline.
            _joining?.SetValue(null, joining);
            SettingsStore.Current.CharacterPick = joining ? Sean : Cheska;
        }

        [TestCase(true)]
        [TestCase(false)]
        public void JoiningPeerKeepsSeanUntilItsAssignedClientSeat(bool temporaryHost)
        {
            // Normal-lobby entry first hosts locally; direct join is already a
            // listening client while LocalSlot still has its pre-admission zero.
            Intent(true); _role.IsHost = temporaryHost; _role.LocalSlot = 0;
            _step();
            Assert.AreEqual(Sean, SettingsStore.Current.CharacterPick,
                "A pre-admission seat zero overwrote the configured Sean pick with Cheska");
            Assert.IsFalse(PickSent, "Pre-admission startup consumed the only lobby pick attempt");
            _role.IsHost = false; _role.LocalSlot = 1; _step();
            Assert.AreEqual(Sean, SettingsStore.Current.CharacterPick); Assert.IsTrue(PickSent);
        }

        [Test] public void IntendedHostStillSelectsCheskaFromItsHostSeat()
        {
            Intent(false); _role.IsHost = true; _role.LocalSlot = 0;
            _step(); Assert.AreEqual(Cheska, SettingsStore.Current.CharacterPick); Assert.IsTrue(PickSent);
        }

        [Test] public void AlreadyAdmittedClientStillSelectsSeanFromItsClientSeat()
        {
            Intent(true); _role.IsHost = false; _role.LocalSlot = 1;
            _step(); Assert.AreEqual(Sean, SettingsStore.Current.CharacterPick); Assert.IsTrue(PickSent);
        }

        [Test] public void OfflineJoiningProcessKeepsConfiguredPickAndDoesNotConsumeSelection()
        {
            Intent(true); _role.IsNetworked = false; _role.IsHost = true; _role.LocalSlot = 0;
            _step(); Assert.AreEqual(Sean, SettingsStore.Current.CharacterPick); Assert.IsFalse(PickSent);
        }
    }
}
