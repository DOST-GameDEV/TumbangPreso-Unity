using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.Tests
{
    public sealed class SafeStoreRecoveryChainTests
    {
        private string _directory, _path;
        private const string First = "usable:first", Second = "usable:second", Corrupt = "interrupted primary";

        [SetUp] public void Before()
        {
            string tempRoot = System.IO.Path.GetFullPath(System.IO.Path.GetTempPath());
            _directory = System.IO.Path.GetFullPath(System.IO.Path.Combine(tempRoot,
                "tp-safe-recovery-chain-" + Guid.NewGuid().ToString("N")));
            Assert.AreEqual(tempRoot.TrimEnd(System.IO.Path.DirectorySeparatorChar),
                System.IO.Path.GetDirectoryName(_directory), "Test directory escaped its owned temporary root.");
            Directory.CreateDirectory(_directory);
            _path = System.IO.Path.Combine(_directory, "cache.txt");
        }

        [TearDown] public void After()
        {
            if (_directory == null) return;
            Assert.AreEqual(System.IO.Path.GetFullPath(System.IO.Path.GetTempPath()).TrimEnd(System.IO.Path.DirectorySeparatorChar),
                System.IO.Path.GetDirectoryName(_directory));
            // Delete only these three owned files; never recursively delete a computed directory.
            foreach (string suffix in new[] { "", SafeStore.TempSuffix, SafeStore.BackupSuffix })
                if (File.Exists(_path + suffix)) File.Delete(_path + suffix);
            if (Directory.Exists(_directory)) Directory.Delete(_directory);
        }

        private static bool Valid(string text) => text.StartsWith("usable:", StringComparison.Ordinal);
        private void Seed()
        {
            Assert.IsTrue(SafeStore.Write(_path, First));
            Assert.IsTrue(SafeStore.Write(_path, Second));
        }
        private string Recover()
        {
            LogAssert.Expect(LogType.Warning, new Regex(@"\[SafeStore\].*recovered the previous version"));
            return SafeStore.Read(_path, Valid);
        }

        [Test] public void RecoveredPrimaryThenNormalSaveKeepsTheLastUsableBackup()
        {
            Seed(); File.WriteAllText(_path, Corrupt);
            Assert.AreEqual(First, Recover());
            Assert.IsTrue(SafeStore.Write(_path, "usable:latest"));
            Assert.AreEqual(First, File.ReadAllText(_path + SafeStore.BackupSuffix),
                "The next normal save rotated the corrupt primary over the usable recovery backup.");
        }

        [Test] public void OrdinarySaveKeepsItsUsablePreviousVersion()
        {
            Seed();
            Assert.AreEqual(Second, SafeStore.Read(_path, Valid));
            Assert.AreEqual(First, File.ReadAllText(_path + SafeStore.BackupSuffix));
        }

        [Test] public void LockedPrimaryStillReturnsTheValidatedBackupWithoutDamagingIt()
        {
            Seed(); File.WriteAllText(_path, Corrupt);
            using (var locked = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Assert.AreEqual(First, Recover());
                Assert.AreEqual(First, File.ReadAllText(_path + SafeStore.BackupSuffix));
                Assert.AreEqual(Corrupt, File.ReadAllText(_path), "The owned lock did not protect the primary.");
            }
        }

        [Test] public void RefusedSaveUnderAPrimaryLockPreservesItsUsableBackup()
        {
            Seed();
            using (var locked = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                LogAssert.Expect(LogType.Warning, new Regex(@"\[SafeStore\] could not write"));
                Assert.IsFalse(SafeStore.Write(_path, "usable:latest"));
                Assert.AreEqual(Second, File.ReadAllText(_path));
                Assert.IsTrue(File.Exists(_path + SafeStore.BackupSuffix),
                    "A refused primary replacement deleted the existing usable backup.");
                Assert.AreEqual(First, File.ReadAllText(_path + SafeStore.BackupSuffix));
                Assert.IsFalse(File.Exists(_path + SafeStore.TempSuffix));
            }
        }
    }
}
