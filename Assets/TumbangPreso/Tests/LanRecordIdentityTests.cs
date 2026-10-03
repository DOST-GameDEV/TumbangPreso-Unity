using System;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.Settings;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class LanRecordIdentityTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _root;
        private NetSession _session;
        private NetworkManager _network;
        private PlayerAccount _account, _previousAccount;
        private string _settings;
        [Serializable] private sealed class Hello { public string AccountPlayerId; public string Token; }

        [SetUp] public void Before()
        {
            _settings = JsonUtility.ToJson(SettingsStore.Current);
            SettingsStore.Current.AccountPlayerId = "cached-profile-id";
            _previousAccount = GameServices.Account;
            _root = new GameObject("Dormant identity serialization"); _root.SetActive(false);
            _account = _root.AddComponent<PlayerAccount>();
            typeof(GameServices).GetProperty("Account").SetValue(null, _account);
            _network = _root.AddComponent<NetworkManager>(); _network.NetworkConfig = new NetworkConfig();
            _session = _root.AddComponent<NetSession>();
            typeof(NetSession).GetField("_nm", Hidden).SetValue(_session, _network);
            Assert.IsFalse(_account.IsSignedIn);
        }

        [TearDown] public void After()
        {
            typeof(GameServices).GetProperty("Account").SetValue(null, _previousAccount);
            Object.DestroyImmediate(_root);
            JsonUtility.FromJsonOverwrite(_settings, SettingsStore.Current);
        }

        private Hello Serialize(bool relay, bool signedIn)
        {
            typeof(NetSession).GetProperty("IsRelay").SetValue(_session, relay);
            typeof(PlayerAccount).GetProperty("IsSignedIn").SetValue(_account, signedIn);
            typeof(NetSession).GetMethod("ConfigureClientHello", Hidden).Invoke(_session, null);
            return JsonUtility.FromJson<Hello>(Encoding.UTF8.GetString(_network.NetworkConfig.ConnectionData));
        }

        [Test] public void LanHelloCarriesTheCachedLocalProfileWithoutSigningIn()
        {
            var hello = Serialize(false, false);
            Assert.AreEqual(_account.PlayerId, hello.AccountPlayerId);
            Assert.AreNotEqual(hello.Token, hello.AccountPlayerId, "Connection and cached profile identities stay separate.");
            Assert.IsFalse(_account.IsSignedIn);
        }

        [Test] public void UnsignedRelayDoesNotClaimACachedAccount()
        { Assert.AreEqual("", Serialize(true, false).AccountPlayerId); }

        [Test] public void SignedRelayKeepsItsAccountIdentity()
        { Assert.AreEqual(_account.PlayerId, Serialize(true, true).AccountPlayerId); }

        [Test] public void RepeatedIntroductionRetainsIdentityWithoutGrantingHandleTrust()
        {
            var lobby = new LobbySession();
            var first = lobby.Admit(1, "same-connection-token", "Local player");
            first.AccountPlayerId = "local-profile-id";
            var second = lobby.Admit(1, "same-connection-token", "Local player");
            Assert.AreEqual("local-profile-id", second.AccountPlayerId);
            Assert.AreEqual(AccountRules.HandleCheck.NotAsked, second.HandleTrust);
            var replacement = lobby.Admit(2, "same-connection-token", "Local player");
            Assert.AreEqual("local-profile-id", replacement.AccountPlayerId);
            Assert.AreEqual(second.Seat, replacement.Seat);
            Assert.AreEqual(AccountRules.HandleCheck.NotAsked, replacement.HandleTrust);
        }
    }
}
