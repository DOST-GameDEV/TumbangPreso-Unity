using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.Tests
{
    public sealed class PortableStoreBackupTests
    {
        private string _directory, _path;
        private const string Previous = "usable:previous", Current = "usable:current", Latest = "usable:latest";
        private static bool Valid(string value) => value.StartsWith("usable:", StringComparison.Ordinal);

        [SetUp] public void Before()
        {
            string temporary = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar);
            _directory = Path.GetFullPath(Path.Combine(temporary, "tp-portable-backup-" + Guid.NewGuid().ToString("N")));
            Assert.AreEqual(temporary, Path.GetDirectoryName(_directory)); Directory.CreateDirectory(_directory);
            _path = Path.Combine(_directory, "settings.txt");
        }
        [TearDown] public void After()
        {
            if (_directory == null) return;
            Assert.AreEqual(Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar), Path.GetDirectoryName(_directory));
            foreach (string suffix in new[] { "", SafeStore.TempSuffix, SafeStore.BackupSuffix })
                if (File.Exists(_path + suffix)) File.Delete(_path + suffix);
            if (Directory.Exists(_directory)) Directory.Delete(_directory);
        }
        private bool Save(string text, RuntimePlatform platform, Func<string, bool> valid)
            => (bool)typeof(SafeStore).GetMethod("WriteAtPlatform", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { _path, text, valid, platform });
        private void Seed(RuntimePlatform platform)
        { Assert.IsTrue(Save(Previous, platform, Valid)); Assert.IsTrue(Save(Current, platform, Valid)); }

        [TestCase(RuntimePlatform.Android)]
        [TestCase(RuntimePlatform.OSXPlayer)]
        [TestCase(RuntimePlatform.LinuxPlayer)]
        public void SavingAfterAnInterruptedPrimaryRetainsTheValidatedRecoveryCopy(RuntimePlatform platform)
        {
            Seed(platform); File.WriteAllText(_path, "interrupted primary");
            Assert.IsTrue(Save(Latest, platform, Valid));
            Assert.AreEqual(Latest, File.ReadAllText(_path));
            Assert.AreEqual(Previous, File.ReadAllText(_path + SafeStore.BackupSuffix),
                "The next save replaced the last usable backup with the interrupted primary");
            // A later interruption must still be recoverable, through the ordinary read route.
            File.WriteAllText(_path, "another interrupted primary");
            LogAssert.Expect(LogType.Warning, new Regex(@"\[SafeStore\].*recovered the previous version"));
            Assert.AreEqual(Previous, SafeStore.Read(_path, Valid));
        }

        [Test] public void AThrowingPreviousValidatorCannotReplaceTheUsableBackupWithCorruptData()
        {
            Seed(RuntimePlatform.Android); File.WriteAllText(_path, "broken primary");
            Assert.IsTrue(Save(Latest, RuntimePlatform.Android, value => throw new FormatException("bad file")));
            Assert.AreEqual(Previous, File.ReadAllText(_path + SafeStore.BackupSuffix));
            Assert.AreEqual(Latest, File.ReadAllText(_path));
        }

        [Test] public void UsablePreviousPrimaryStillBecomesTheBackup()
        {
            Seed(RuntimePlatform.Android); Assert.IsTrue(Save(Latest, RuntimePlatform.Android, Valid));
            Assert.AreEqual(Current, File.ReadAllText(_path + SafeStore.BackupSuffix));
            Assert.AreEqual(Latest, File.ReadAllText(_path));
            Assert.IsFalse(File.Exists(_path + SafeStore.TempSuffix));
        }

        [Test] public void LegacyTextSaveWithoutAValidatorStillKeepsThePreviousText()
        {
            Assert.IsTrue(Save("plain first", RuntimePlatform.Android, null));
            Assert.IsTrue(Save("plain second", RuntimePlatform.Android, null));
            Assert.AreEqual("plain first", File.ReadAllText(_path + SafeStore.BackupSuffix));
            Assert.AreEqual("plain second", SafeStore.Read(_path));
        }
    }
}
