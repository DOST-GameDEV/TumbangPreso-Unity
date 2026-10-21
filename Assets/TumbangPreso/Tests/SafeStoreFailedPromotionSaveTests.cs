using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.Tests
{
    public sealed class SafeStoreFailedPromotionSaveTests
    {
        private string _directory, _path;
        private const string First = "usable:first", Second = "usable:second", Corrupt = "interrupted primary";

        [SetUp] public void Before()
        {
            string tempRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar);
            _directory = Path.GetFullPath(Path.Combine(tempRoot, "tp-failed-promotion-save-" + Guid.NewGuid().ToString("N")));
            Assert.AreEqual(tempRoot, Path.GetDirectoryName(_directory)); Directory.CreateDirectory(_directory);
            _path = Path.Combine(_directory, "cache.txt");
        }
        [TearDown] public void After()
        {
            if (_directory == null) return;
            Assert.AreEqual(Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar), Path.GetDirectoryName(_directory));
            foreach (string suffix in new[] { "", SafeStore.TempSuffix, SafeStore.BackupSuffix })
                if (File.Exists(_path + suffix)) File.Delete(_path + suffix);
            if (Directory.Exists(_directory)) Directory.Delete(_directory);
        }
        private static bool Valid(string text) => text.StartsWith("usable:", StringComparison.Ordinal);
        private void Seed()
        { Assert.IsTrue(SafeStore.Write(_path, First)); Assert.IsTrue(SafeStore.Write(_path, Second)); }
        private bool ValidatedSave(string contents)
        {
            // The original public API compiles unchanged; the candidate's explicit overload
            // supplies the same usability rule as the existing production Read callers.
            var overload = typeof(SafeStore).GetMethod("Write", new[] { typeof(string), typeof(string), typeof(Func<string, bool>) });
            return overload == null ? SafeStore.Write(_path, contents)
                : (bool)overload.Invoke(null, new object[] { _path, contents, (Func<string, bool>)Valid });
        }

        [Test] public void FailedPromotionThenReleasedLockAndValidatedSavePreserveTheUsableBackup()
        {
            Seed(); File.WriteAllText(_path, Corrupt);
            using (var locked = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                LogAssert.Expect(LogType.Warning, new Regex(@"\[SafeStore\].*recovered the previous version"));
                Assert.AreEqual(First, SafeStore.Read(_path, Valid));
                Assert.AreEqual(Corrupt, File.ReadAllText(_path));
                Assert.AreEqual(First, File.ReadAllText(_path + SafeStore.BackupSuffix));
            }
            Assert.IsTrue(ValidatedSave("usable:latest"));
            Assert.AreEqual(First, File.ReadAllText(_path + SafeStore.BackupSuffix),
                "A later save rotated the still-corrupt primary over the recovery backup after its lock was released.");
        }

        [Test] public void ValidatedSaveStillRotatesAUsablePrimaryAsItsPreviousVersion()
        {
            Seed(); Assert.IsTrue(ValidatedSave("usable:latest"));
            Assert.AreEqual(Second, File.ReadAllText(_path + SafeStore.BackupSuffix));
            Assert.AreEqual("usable:latest", File.ReadAllText(_path));
        }

        [Test] public void LegacyTwoArgumentTextWritesKeepTheirExistingBackupContract()
        {
            Assert.IsTrue(SafeStore.Write(_path, "plain:first")); Assert.IsTrue(SafeStore.Write(_path, "plain:second"));
            Assert.AreEqual("plain:second", SafeStore.Read(_path));
            Assert.AreEqual("plain:first", File.ReadAllText(_path + SafeStore.BackupSuffix));
        }
    }
}
