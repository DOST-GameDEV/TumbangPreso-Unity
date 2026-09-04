using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// A player file survives a save being interrupted.
    ///
    /// ⚠️⚠️ THE FAULT THESE ARE ABOUT NEVER HAPPENS IN A TEST RUN AND ALWAYS HAPPENS AT A VENUE.
    /// `File.WriteAllText` truncates the target and then writes, so a process that dies in
    /// between leaves a fragment of valid JSON. The next launch reads it, `JsonUtility` throws,
    /// the load path correctly falls back to defaults, and the player's rebinds are gone with no
    /// error anybody sees. **The fallback was never the bug; having nothing to fall back TO was.**
    ///
    /// ⚠️ THEY WRITE INTO A TEMPORARY DIRECTORY, NEVER INTO `persistentDataPath`. A test that
    /// exercises the real settings file destroys the settings of whoever ran it, and on this
    /// machine that is a person with a `Fullscreen: false` window they play in (`CLAUDE.md`
    /// § 6.2b).
    /// </summary>
    public class SafeStoreTests
    {
        private string _dir;

        [SetUp]
        public void MakeDirectory()
        {
            _dir = Path.Combine(Path.GetTempPath(), "tp-safestore-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [TearDown]
        public void RemoveDirectory()
        {
            try
            {
                if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
            }
            catch
            {
                // A leftover temp folder is not worth failing a suite over.
            }
        }

        private string Path_(string name) => Path.Combine(_dir, name);

        [Test]
        public void WhatWasWrittenIsWhatIsRead()
        {
            string path = Path_("settings.json");

            Assert.IsTrue(SafeStore.Write(path, "{\"a\":1}"));
            Assert.AreEqual("{\"a\":1}", SafeStore.Read(path));
        }

        [Test]
        public void TheSecondWriteKeepsTheFirstAsABackup()
        {
            string path = Path_("career.json");

            SafeStore.Write(path, "first");
            SafeStore.Write(path, "second");

            Assert.AreEqual("second", SafeStore.Read(path));
            Assert.IsTrue(File.Exists(path + SafeStore.BackupSuffix),
                          "no backup was kept, so an interrupted save has nothing to recover to");
            Assert.AreEqual("first", File.ReadAllText(path + SafeStore.BackupSuffix));
        }

        [Test]
        public void ATruncatedFileRecoversFromTheBackup()
        {
            // ⚠️ THIS IS THE WHOLE POINT OF THE FILE. The primary is a fragment, which is exactly
            // what a crash mid-write leaves, and the previous good version is still on disk.
            string path = Path_("settings.json");

            SafeStore.Write(path, "{\"good\":true}");
            SafeStore.Write(path, "{\"newer\":true}");

            File.WriteAllText(path, "{\"newer\":tr");   // interrupted

            LogAssert.ignoreFailingMessages = true;
            string read = SafeStore.Read(path, text => text.EndsWith("}"));
            LogAssert.ignoreFailingMessages = false;

            Assert.AreEqual("{\"good\":true}", read,
                            "a truncated primary did not fall back to the backup");
        }

        [Test]
        public void ACorruptFileWithNoBackupReadsAsNothingRatherThanAsGarbage()
        {
            string path = Path_("social.json");
            File.WriteAllText(path, "not json at all");

            LogAssert.ignoreFailingMessages = true;
            string read = SafeStore.Read(path, text => text.StartsWith("{"));
            LogAssert.ignoreFailingMessages = false;

            Assert.IsNull(read, "a corrupt file with no backup was handed back as usable");
        }

        [Test]
        public void AValidatorThatThrowsMeansNotValidRatherThanTakingTheLoadDown()
        {
            // ⚠️ `JsonUtility.FromJson` THROWS ON MALFORMED INPUT, and the predicate the callers
            // pass calls it. A validator that throws must read as "no", not as an exception out
            // of the load path, or the recovery is worse than the corruption.
            string path = Path_("career.json");
            File.WriteAllText(path, "garbage");

            LogAssert.ignoreFailingMessages = true;
            string read = SafeStore.Read(path, text => throw new InvalidOperationException("boom"));
            LogAssert.ignoreFailingMessages = false;

            Assert.IsNull(read);
        }

        [Test]
        public void AMissingDirectoryIsCreatedRatherThanFailing()
        {
            // A fresh profile, a cleaned machine or a sandboxed install can all present a missing
            // folder, and the failure is a first-run-only bug nobody can reproduce afterwards.
            string path = Path.Combine(_dir, "nested", "deeper", "settings.json");

            Assert.IsTrue(SafeStore.Write(path, "{}"),
                          "a missing directory was not created");
            Assert.AreEqual("{}", SafeStore.Read(path));
        }

        [Test]
        public void AWriteThatCannotSucceedReturnsFalseAndDoesNotThrow()
        {
            // ⚠️ A FAILED SAVE MUST NEVER INTERRUPT A MATCH. `docs/TODO.md` § 142.13: a replay,
            // log or profile write failing may not stop a tournament match starting or finishing.
            // The path below is a directory, so writing a file at it cannot work.
            string path = Path_("adirectory");
            Directory.CreateDirectory(path);

            LogAssert.ignoreFailingMessages = true;
            bool ok = SafeStore.Write(path, "anything");
            LogAssert.ignoreFailingMessages = false;

            Assert.IsFalse(ok, "a write that cannot possibly succeed reported success");
        }

        [Test]
        public void AFailedWriteLeavesNoHalfFinishedTemporaryFile()
        {
            string path = Path_("adirectory");
            Directory.CreateDirectory(path);

            LogAssert.ignoreFailingMessages = true;
            SafeStore.Write(path, "anything");
            LogAssert.ignoreFailingMessages = false;

            Assert.IsFalse(File.Exists(path + SafeStore.TempSuffix),
                           "a failed write left its temporary file for the next launch to find");
        }

        [Test]
        public void ExistsSeesAFileThatOnlySurvivesAsABackup()
        {
            string path = Path_("career.json");

            SafeStore.Write(path, "one");
            SafeStore.Write(path, "two");
            File.Delete(path);

            Assert.IsTrue(SafeStore.Exists(path),
                          "a file that survives only as a backup was reported missing");
        }
    }
}
