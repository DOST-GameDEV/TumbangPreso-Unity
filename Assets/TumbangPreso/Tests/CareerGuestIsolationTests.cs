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
    public sealed class CareerGuestIsolationTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private PlayerAccount _previousAccount, _account;
        private CareerStore _previousCareer, _career;
        private GameObject _accountObject, _careerObject;
        private object _primaryCache;
        private readonly Dictionary<string, byte[]> _originalFiles = new Dictionary<string, byte[]>();
        private byte[] _primaryBytes;
        private byte[] _backupBytes;

        [SetUp]
        public void Before()
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-tp-profile"),
                "Guest career checks require an isolated profile.");
            Assert.IsFalse(NetIdentity.IsOnline, "These account transitions must stay local.");
            _previousAccount = GameServices.Account;
            _previousCareer = CareerStore.Instance;
            foreach (string suffix in new[] { "", SafeStore.BackupSuffix, SafeStore.TempSuffix })
            {
                string path = CareerStore.Path + suffix;
                _originalFiles[path] = File.Exists(path) ? File.ReadAllBytes(path) : null;
            }
            _accountObject = new GameObject("Dormant guest career account");
            _accountObject.SetActive(false);
            _account = _accountObject.AddComponent<PlayerAccount>();
            SetAccount("guest-test-primary");
            typeof(GameServices).GetProperty("Account").SetValue(null, _account);
            _careerObject = new GameObject("Guest career ownership check");
            _careerObject.SetActive(false);
            _career = _careerObject.AddComponent<CareerStore>();
            Invoke("Awake"); // EditMode does not run this runtime subscription automatically.
            _primaryCache = Cache;
            _primaryCache.GetType().GetField("OwnerId").SetValue(_primaryCache, _account.PlayerId);
            _career.Profile.Xp = 321;
            Records("History").Clear(); Records("Queue").Clear();
            var retained = new MatchRecord { MatchId = "primary-pending-match" };
            Records("History").Add(retained); Records("Queue").Add(retained);
            _primaryCache.GetType().GetField("QueueWitness").SetValue(_primaryCache,
                new List<string> { "primary-witness" });
            _primaryCache.GetType().GetField("InMatchSinceUtc").SetValue(_primaryCache, "primary-marker");
            Invoke("Save");
            _primaryBytes = File.ReadAllBytes(CareerStore.Path);
            _backupBytes = File.Exists(CareerStore.Path + SafeStore.BackupSuffix)
                ? File.ReadAllBytes(CareerStore.Path + SafeStore.BackupSuffix) : null;
        }

        [TearDown]
        public void After()
        {
            if (_careerObject != null)
            {
                Invoke("OnDestroy");
                Object.DestroyImmediate(_careerObject);
            }
            if (_accountObject != null) Object.DestroyImmediate(_accountObject);
            typeof(GameServices).GetProperty("Account").SetValue(null, _previousAccount);
            typeof(CareerStore).GetProperty("Instance").SetValue(null, _previousCareer);
            foreach (var file in _originalFiles)
            {
                if (file.Value == null) { if (File.Exists(file.Key)) File.Delete(file.Key); }
                else File.WriteAllBytes(file.Key, file.Value);
            }
            _originalFiles.Clear();
        }

        private object Cache => typeof(CareerStore).GetField("_cache", Hidden).GetValue(_career);
        private List<MatchRecord> Records(string name)
            => (List<MatchRecord>)Cache.GetType().GetField(name).GetValue(Cache);
        private void Invoke(string method) => typeof(CareerStore).GetMethod(method, Hidden).Invoke(_career, null);
        private void SetAccount(string id) => typeof(PlayerAccount).GetField("_profile", Hidden).SetValue(_account,
            AccountRules.Normalise(new AccountProfile { PlayerId = id, DisplayName = "Primary" }));
        private void AssertPrimaryFilesUnchanged()
        {
            CollectionAssert.AreEqual(_primaryBytes, File.ReadAllBytes(CareerStore.Path),
                "A temporary guest overwrote the primary career on disk.");
            string backup = CareerStore.Path + SafeStore.BackupSuffix;
            CollectionAssert.AreEqual(_backupBytes, File.Exists(backup) ? File.ReadAllBytes(backup) : null,
                "A temporary guest rotated the primary career's recovery backup.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EnteringATemporaryGuestPreservesThePrimaryCareerFile(bool unclaimedCareer)
        {
            if (unclaimedCareer)
            {
                _primaryCache.GetType().GetField("OwnerId").SetValue(_primaryCache, "");
                Invoke("Save");
                _primaryBytes = File.ReadAllBytes(CareerStore.Path);
                _backupBytes = File.ReadAllBytes(CareerStore.Path + SafeStore.BackupSuffix);
            }
            _account.SignInAsGuest("TournamentGuest");
            Assert.IsTrue(_account.IsGuest);
            Assert.Zero(_career.History.Count, "The guest inherited the primary account's history.");
            Assert.Zero(_career.QueuedCount);
            AssertPrimaryFilesUnchanged();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TakingBackTheDeviceRestoresPrimaryHistoryAndPendingUploads(bool unclaimedCareer)
        {
            if (unclaimedCareer)
            {
                _primaryCache.GetType().GetField("OwnerId").SetValue(_primaryCache, "");
                Invoke("Save");
                _primaryBytes = File.ReadAllBytes(CareerStore.Path);
                _backupBytes = File.ReadAllBytes(CareerStore.Path + SafeStore.BackupSuffix);
            }
            object notifiedCache = null;
            _career.Changed += () => notifiedCache = Cache;
            _account.SignInAsGuest("TournamentGuest");
            _account.SignInAsGuest("NextGuest");
            AssertPrimaryFilesUnchanged();
            _account.LeaveGuest();
            Assert.AreEqual("guest-test-primary", _account.PlayerId);
            Assert.AreSame(_primaryCache, Cache, "The guest handover discarded the primary career cache.");
            Assert.AreSame(_primaryCache, notifiedCache, "Career subscribers were not notified of the restored primary career.");
            Assert.AreEqual(321, _career.Profile.Xp);
            Assert.AreEqual("primary-pending-match", _career.History.Single().MatchId);
            Assert.AreEqual(1, _career.QueuedCount);
            CollectionAssert.AreEqual(new[] { "primary-witness" },
                (List<string>)Cache.GetType().GetField("QueueWitness").GetValue(Cache));
            Assert.AreEqual("primary-marker", Cache.GetType().GetField("InMatchSinceUtc").GetValue(Cache));
            if (unclaimedCareer)
            {
                Assert.AreEqual("guest-test-primary", Cache.GetType().GetField("OwnerId").GetValue(Cache));
                StringAssert.Contains("\"OwnerId\":\"guest-test-primary\"", File.ReadAllText(CareerStore.Path));
                StringAssert.Contains("primary-pending-match", File.ReadAllText(CareerStore.Path));
            }
            else AssertPrimaryFilesUnchanged();
        }

        [Test]
        public void GuestMatchWritesLeaveThePrimaryCareerFileUntouched()
        {
            _account.SignInAsGuest("TournamentGuest");
            _career.NoteMatchStarted(true);
            _career.Record(new MatchRecord
            {
                MatchId = "guest-local-match", Online = true, Mode = "Classic", Rounds = 1,
                PlayedUtc = "2026-10-03T00:00:00Z", DurationSeconds = 30,
                Players = new[] { new PlayerMatchStats
                    { PlayerId = _account.PlayerId, Slot = 0, Placement = 1, Score = 10 } }
            }, "guest-witness");
            AssertPrimaryFilesUnchanged();
        }

        [Test]
        public void ARealAccountReplacementStillRetiresThePreviousCareer()
        {
            SetAccount("guest-test-new-primary");
            Invoke("OnAccountChanged");
            Assert.AreNotSame(_primaryCache, Cache);
            Assert.Zero(_career.History.Count); Assert.Zero(_career.QueuedCount);
            Assert.Zero(_career.Profile.Xp);
            StringAssert.Contains("guest-test-new-primary", File.ReadAllText(CareerStore.Path));
        }
    }
}
