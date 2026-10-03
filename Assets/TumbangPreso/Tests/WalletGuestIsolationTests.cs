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
    public sealed class WalletGuestIsolationTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private PlayerAccount _previousAccount, _account;
        private CareerStore _previousCareer;
        private WalletStore _previousWallet, _wallet;
        private GameObject _accountObject, _walletObject;
        private object _primaryCache;
        private readonly Dictionary<string, byte[]> _originalFiles = new Dictionary<string, byte[]>();
        private byte[] _primaryBytes, _backupBytes;

        [SetUp]
        public void Before()
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-tp-profile"),
                "Guest wallet checks require an isolated profile.");
            Assert.IsFalse(NetIdentity.IsOnline, "Guest handovers must stay local.");
            _previousAccount = GameServices.Account;
            _previousCareer = GameServices.Career;
            _previousWallet = WalletStore.Instance;
            foreach (string suffix in new[] { "", SafeStore.BackupSuffix, SafeStore.TempSuffix })
            {
                string path = WalletStore.Path + suffix;
                _originalFiles[path] = File.Exists(path) ? File.ReadAllBytes(path) : null;
            }
            _accountObject = new GameObject("Dormant guest wallet account");
            _accountObject.SetActive(false);
            _account = _accountObject.AddComponent<PlayerAccount>();
            SetAccount("wallet-test-primary");
            typeof(GameServices).GetProperty("Account").SetValue(null, _account);
            typeof(GameServices).GetProperty("Career").SetValue(null, null);
            CreateWallet();
            _primaryCache = Cache;
            _primaryCache.GetType().GetField("OwnerId").SetValue(_primaryCache, _account.PlayerId);
            _primaryCache.GetType().GetField("Known").SetValue(_primaryCache, true);
            _primaryCache.GetType().GetField("Wallet").SetValue(_primaryCache, new Wallet
            {
                Balance = 321,
                Owned = new List<string> { "retained-owned-item" },
                Claimed = new List<string> { "retained-claimed-task" },
                PaidMatchIds = new List<string> { "retained-paid-match" }
            });
            Invoke("Save");
            CapturePrimaryFiles();
        }

        [TearDown]
        public void After()
        {
            DestroyWallet();
            if (_accountObject != null) Object.DestroyImmediate(_accountObject);
            typeof(GameServices).GetProperty("Account").SetValue(null, _previousAccount);
            typeof(GameServices).GetProperty("Career").SetValue(null, _previousCareer);
            typeof(WalletStore).GetProperty("Instance").SetValue(null, _previousWallet);
            foreach (var file in _originalFiles)
            {
                if (file.Value == null) { if (File.Exists(file.Key)) File.Delete(file.Key); }
                else File.WriteAllBytes(file.Key, file.Value);
            }
            _originalFiles.Clear();
        }

        private object Cache => typeof(WalletStore).GetField("_cache", Hidden).GetValue(_wallet);
        private Wallet SavedWallet => (Wallet)Cache.GetType().GetField("Wallet").GetValue(Cache);
        private void Invoke(string method) => typeof(WalletStore).GetMethod(method, Hidden).Invoke(_wallet, null);
        private void SetAccount(string id) => typeof(PlayerAccount).GetField("_profile", Hidden).SetValue(_account,
            AccountRules.Normalise(new AccountProfile { PlayerId = id, DisplayName = "Primary" }));
        private void CreateWallet()
        {
            _walletObject = new GameObject("Dormant guest wallet store");
            _walletObject.SetActive(false);
            _wallet = _walletObject.AddComponent<WalletStore>();
            // Native EditMode does not run these runtime subscriptions automatically.
            Invoke("Awake"); Invoke("OnEnable");
            Assert.AreSame(_account, typeof(WalletStore).GetField("_hookedAccount", Hidden).GetValue(_wallet));
        }
        private void DestroyWallet()
        {
            if (_walletObject == null) return;
            Invoke("OnDisable"); Object.DestroyImmediate(_walletObject);
            _walletObject = null;
        }
        private void CapturePrimaryFiles()
        {
            _primaryBytes = File.ReadAllBytes(WalletStore.Path);
            _backupBytes = File.Exists(WalletStore.Path + SafeStore.BackupSuffix)
                ? File.ReadAllBytes(WalletStore.Path + SafeStore.BackupSuffix) : null;
        }
        private void AssertPrimaryFilesUnchanged()
        {
            CollectionAssert.AreEqual(_primaryBytes, File.ReadAllBytes(WalletStore.Path),
                "A temporary guest overwrote the primary wallet on disk.");
            string backup = WalletStore.Path + SafeStore.BackupSuffix;
            CollectionAssert.AreEqual(_backupBytes, File.Exists(backup) ? File.ReadAllBytes(backup) : null,
                "A temporary guest rotated the wallet's recovery backup.");
        }
        private void AssertPrimaryWalletRestored()
        {
            Assert.IsTrue(_wallet.Known); Assert.AreEqual(321, _wallet.Balance);
            Assert.IsTrue(_wallet.Owns("retained-owned-item"));
            CollectionAssert.AreEqual(new[] { "retained-claimed-task" }, SavedWallet.Claimed);
            CollectionAssert.AreEqual(new[] { "retained-paid-match" }, SavedWallet.PaidMatchIds);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void GuestEntryHidesThePrimaryWalletAndPreservesItsFiles(bool unclaimedWallet)
        {
            if (unclaimedWallet)
            {
                _primaryCache.GetType().GetField("OwnerId").SetValue(_primaryCache, "");
                Invoke("Save"); CapturePrimaryFiles();
            }
            _account.SignInAsGuest("TournamentGuest");
            Assert.IsFalse(_wallet.Known, "The guest inherited the primary account's cached balance.");
            Assert.AreEqual(-1, _wallet.Balance);
            Assert.IsFalse(_wallet.Owns("retained-owned-item"));
            Assert.IsFalse(WalletStore.CanTransact);
            AssertPrimaryFilesUnchanged();
        }

        [Test]
        public void ReturningFromRepeatedGuestsRestoresThePrimaryWalletAndNotifiesSubscribers()
        {
            object notifiedCache = null;
            _wallet.Changed += () => notifiedCache = Cache;
            _account.SignInAsGuest("TournamentGuest");
            _account.SignInAsGuest("NextGuest");
            _account.LeaveGuest();
            Assert.AreSame(_primaryCache, Cache, "Guest handover discarded the primary wallet cache.");
            Assert.AreSame(_primaryCache, notifiedCache, "Wallet subscribers missed the restored primary balance.");
            AssertPrimaryWalletRestored(); AssertPrimaryFilesUnchanged();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AWalletCreatedDuringGuestPlayRestoresOnlyTheMatchingPrimary(bool foreignSavedOwner)
        {
            if (foreignSavedOwner)
            {
                _primaryCache.GetType().GetField("OwnerId").SetValue(_primaryCache, "another-real-account");
                Invoke("Save"); CapturePrimaryFiles();
            }
            DestroyWallet();
            _account.SignInAsGuest("TournamentGuest");
            CreateWallet();
            Assert.IsFalse(_wallet.Known); Assert.IsFalse(_wallet.Owns("retained-owned-item"));
            AssertPrimaryFilesUnchanged();
            _account.LeaveGuest();
            if (foreignSavedOwner)
            {
                Assert.IsFalse(_wallet.Known, "A saved foreign wallet was exposed to the restored account.");
                Assert.IsFalse(_wallet.Owns("retained-owned-item"));
            }
            else { AssertPrimaryWalletRestored(); AssertPrimaryFilesUnchanged(); }
        }

        [Test]
        public void ReenablingAfterTheGuestHandsBackReconcilesThePrimaryWallet()
        {
            _account.SignInAsGuest("TournamentGuest");
            Invoke("OnDisable");
            _account.LeaveGuest(); // The disabled store intentionally misses this notification.
            Invoke("OnEnable");
            Assert.AreSame(_primaryCache, Cache);
            AssertPrimaryWalletRestored(); AssertPrimaryFilesUnchanged();
        }

        [Test]
        public void ARealAccountReplacementStillRetiresThePreviousWallet()
        {
            SetAccount("wallet-test-new-primary");
            Invoke("OnAccountChanged");
            Assert.AreNotSame(_primaryCache, Cache);
            Assert.IsFalse(_wallet.Known); Assert.AreEqual(-1, _wallet.Balance);
            Assert.IsFalse(_wallet.Owns("retained-owned-item"));
            StringAssert.Contains("wallet-test-new-primary", File.ReadAllText(WalletStore.Path));
        }
    }
}
