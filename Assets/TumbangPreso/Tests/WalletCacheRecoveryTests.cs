using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class WalletCacheRecoveryTests
    {
        [Serializable]
        private sealed class SavedCache
        {
            public string OwnerId;
            public bool Known = true;
            public Wallet Wallet;
        }

        [TestCase("recover")]
        [TestCase("primary")]
        [TestCase("foreign")]
        [TestCase("missing")]
        public void LoadRecoversAParseableWalletWithoutCrossingTheAccountBoundary(string scenario)
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-tp-profile"),
                "Only a named validation profile may supply this fixture's wallet files.");
            Assert.IsFalse(WalletStore.CanTransact, "Reload checks must not use the live service.");
            const BindingFlags hidden = BindingFlags.NonPublic | BindingFlags.Static;
            var token = typeof(NetIdentity).GetField("_overrideTokenForTesting", hidden);
            string originalToken = (string)token.GetValue(null);
            var originalInstance = WalletStore.Instance;
            string path = WalletStore.Path;
            var files = new Dictionary<string, byte[]>();
            foreach (string suffix in new[] { "", SafeStore.BackupSuffix, SafeStore.TempSuffix })
                files[path + suffix] = File.Exists(path + suffix) ? File.ReadAllBytes(path + suffix) : null;
            GameObject root = null;
            try
            {
                NetIdentity.OverrideForTesting("wallet-recovery-owner");
                string owner = CareerStore.LocalPlayerId;
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                if (scenario == "missing") File.Delete(path + SafeStore.BackupSuffix);
                else File.WriteAllText(path + SafeStore.BackupSuffix, JsonUtility.ToJson(new SavedCache
                {
                    OwnerId = scenario == "foreign" ? "another-wallet-owner" : owner,
                    Wallet = new Wallet { Balance = 77 }
                }));
                string primary = scenario == "primary"
                    ? JsonUtility.ToJson(new SavedCache { OwnerId = owner, Wallet = new Wallet { Balance = 99 } })
                    : "{\"OwnerId\":\"unfinished\",\"Wallet\":{\"Balance\":";
                File.WriteAllText(path, primary);
                var installed = new Dictionary<string, byte[]>();
                foreach (string file in files.Keys) installed[file] = File.Exists(file) ? File.ReadAllBytes(file) : null;

                root = new GameObject("Dormant wallet reload"); root.SetActive(false);
                var wallet = root.AddComponent<WalletStore>();
                typeof(WalletStore).GetMethod("Load", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(wallet, null);
                bool accepted = scenario == "recover" || scenario == "primary";
                Assert.AreEqual(accepted, wallet.Known, "A valid backup was lost or another account's wallet was adopted.");
                Assert.AreEqual(scenario == "recover" ? 77 : scenario == "primary" ? 99 : -1, wallet.Balance);
                Assert.IsFalse(wallet.Busy, "A local reload started a transaction.");
                Assert.AreEqual(0, wallet.LastPaid);
                foreach (var file in installed)
                {
                    Assert.AreEqual(file.Value != null, File.Exists(file.Key), "Reading changed cache file existence.");
                    if (file.Value != null) CollectionAssert.AreEqual(file.Value, File.ReadAllBytes(file.Key), "Reading rewrote saved bytes.");
                }
            }
            finally
            {
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
                typeof(WalletStore).GetProperty("Instance").SetValue(null, originalInstance);
                NetIdentity.OverrideForTesting(originalToken);
                foreach (var file in files)
                {
                    if (file.Value == null) File.Delete(file.Key);
                    else File.WriteAllBytes(file.Key, file.Value);
                }
            }
        }
    }
}
