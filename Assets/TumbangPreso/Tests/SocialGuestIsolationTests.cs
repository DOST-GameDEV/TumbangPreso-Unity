using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class SocialGuestIsolationTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private PlayerAccount _previousAccount, _account;
        private SocialStore _previousSocial, _social;
        private GameObject _accountObject, _socialObject;
        private object _primaryCache;
        private readonly Dictionary<string, byte[]> _originalFiles = new Dictionary<string, byte[]>();
        private byte[] _primaryBytes, _backupBytes;
        private static string Path => System.IO.Path.Combine(ProfilePaths.Root, "social.json");

        [SetUp]
        public void Before()
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-tp-profile"),
                "Guest social checks require an isolated profile.");
            Assert.IsFalse(NetIdentity.IsOnline, "Guest handovers must stay local.");
            _previousAccount = GameServices.Account;
            _previousSocial = SocialStore.Instance;
            foreach (string suffix in new[] { "", SafeStore.BackupSuffix, SafeStore.TempSuffix })
            {
                string path = Path + suffix;
                _originalFiles[path] = File.Exists(path) ? File.ReadAllBytes(path) : null;
            }
            _accountObject = new GameObject("Dormant guest social account");
            _accountObject.SetActive(false);
            _account = _accountObject.AddComponent<PlayerAccount>();
            SetAccount("social-test-primary");
            typeof(GameServices).GetProperty("Account").SetValue(null, _account);
            typeof(SocialStore).GetProperty("Instance").SetValue(null, null);
            CreateSocial();
            _primaryCache = Cache;
            _social.List.Friends.Clear(); _social.List.Blocked.Clear();
            _social.List.Friends.Add(new FriendRef { PlayerId = "retained-friend", Handle = "FRIEND#4417" });
            _social.List.Blocked.Add("retained-block");
            Invoke("Save"); CapturePrimaryFiles();
        }

        [TearDown]
        public void After()
        {
            DestroySocial();
            if (_accountObject != null) Object.DestroyImmediate(_accountObject);
            typeof(GameServices).GetProperty("Account").SetValue(null, _previousAccount);
            typeof(SocialStore).GetProperty("Instance").SetValue(null, _previousSocial);
            foreach (var file in _originalFiles)
            {
                if (file.Value == null) { if (File.Exists(file.Key)) File.Delete(file.Key); }
                else File.WriteAllBytes(file.Key, file.Value);
            }
            _originalFiles.Clear();
        }

        private object Cache => typeof(SocialStore).GetField("_cache", Hidden).GetValue(_social);
        private void Invoke(string method) => typeof(SocialStore).GetMethod(method, Hidden).Invoke(_social, null);
        private void SetAccount(string id) => typeof(PlayerAccount).GetField("_profile", Hidden).SetValue(_account,
            AccountRules.Normalise(new AccountProfile { PlayerId = id, DisplayName = "Primary" }));
        private void CreateSocial()
        {
            _socialObject = new GameObject("Dormant guest social store");
            _socialObject.SetActive(false);
            _social = _socialObject.AddComponent<SocialStore>();
            Invoke("Awake"); Invoke("OnEnable");
            Assert.AreSame(_social, SocialStore.Instance);
            Assert.AreSame(_account, typeof(SocialStore).GetField("_hookedAccount", Hidden).GetValue(_social));
        }
        private void DestroySocial()
        {
            if (_socialObject == null) return;
            Invoke("OnDisable"); Invoke("OnDestroy"); Object.DestroyImmediate(_socialObject);
            _socialObject = null;
        }
        private void CapturePrimaryFiles()
        {
            _primaryBytes = File.ReadAllBytes(Path);
            _backupBytes = File.Exists(Path + SafeStore.BackupSuffix)
                ? File.ReadAllBytes(Path + SafeStore.BackupSuffix) : null;
        }
        private void AssertPrimaryFilesUnchanged()
        {
            CollectionAssert.AreEqual(_primaryBytes, File.ReadAllBytes(Path));
            string backup = Path + SafeStore.BackupSuffix;
            CollectionAssert.AreEqual(_backupBytes, File.Exists(backup) ? File.ReadAllBytes(backup) : null);
        }
        private void AssertPrimaryListRestored()
        {
            Assert.AreEqual("retained-friend", _social.List.Friends.Single().PlayerId,
                "The returning offline owner lost their cached friends.");
            Assert.IsTrue(SocialRules.IsBlocked(_social.List, "retained-block"),
                "The returning offline owner lost the cached block used by LAN admission.");
            AssertPrimaryFilesUnchanged();
        }

        [Test]
        public void GuestEntryHidesThePrimaryFriendsAndBlocksWithoutRewritingTheirFiles()
        {
            _account.SignInAsGuest("TournamentGuest");
            Assert.IsEmpty(_social.List.Friends); Assert.IsEmpty(_social.List.Blocked);
            AssertPrimaryFilesUnchanged();
        }

        [Test]
        public void ReturningFromRepeatedGuestsRestoresPrimaryFriendsAndBlocks()
        {
            object notifiedCache = null;
            _social.Changed += () => notifiedCache = Cache;
            _account.SignInAsGuest("TournamentGuest");
            _account.SignInAsGuest("NextGuest");
            _account.LeaveGuest();
            Assert.AreSame(_primaryCache, Cache, "Guest handover discarded the primary social cache.");
            Assert.AreSame(_primaryCache, notifiedCache, "Social subscribers missed the restored cached friends.");
            AssertPrimaryListRestored();
        }

        [TestCase("matching")]
        [TestCase("foreign")]
        [TestCase("unclaimed")]
        public void AStoreCreatedDuringGuestPlayRestoresOnlyTheExactPrimaryOwner(string savedOwner)
        {
            if (savedOwner != "matching")
            {
                Cache.GetType().GetField("OwnerId").SetValue(Cache,
                    savedOwner == "foreign" ? "another-real-account" : "");
                File.WriteAllText(Path, JsonUtility.ToJson(Cache, true));
                CapturePrimaryFiles();
            }
            DestroySocial();
            _account.SignInAsGuest("TournamentGuest");
            CreateSocial();
            Assert.IsEmpty(_social.List.Friends); Assert.IsEmpty(_social.List.Blocked);
            AssertPrimaryFilesUnchanged();
            _account.LeaveGuest();
            if (savedOwner == "matching") AssertPrimaryListRestored();
            else
            {
                Assert.IsEmpty(_social.List.Friends, "A foreign or unclaimed list crossed the restored account boundary.");
                Assert.IsEmpty(_social.List.Blocked);
                AssertPrimaryFilesUnchanged();
            }
        }

        [Test]
        public void TheFirstListReadRestoresPrimaryAfterAMissedGuestReturnNotification()
        {
            _account.SignInAsGuest("TournamentGuest");
            Invoke("OnDisable");
            _account.LeaveGuest();
            int notifications = 0;
            _social.Changed += () => notifications++;
            AssertPrimaryListRestored();
            Assert.AreSame(_primaryCache, Cache);
            Assert.AreEqual(1, notifications, "The restored rail should notify once; ordinary reads must remain quiet.");
        }

        [Test]
        public void ARealAccountReplacementStillRetiresThePreviousFriendsAndBlocks()
        {
            SetAccount("social-test-new-primary");
            Assert.IsEmpty(_social.List.Friends); Assert.IsEmpty(_social.List.Blocked);
            Assert.AreNotSame(_primaryCache, Cache);
            AssertPrimaryFilesUnchanged();
        }
    }
}
