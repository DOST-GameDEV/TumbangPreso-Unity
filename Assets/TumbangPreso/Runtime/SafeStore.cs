using System;
using System.IO;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// Reading and writing a small player file without being able to destroy it.
    ///
    /// ⚠️⚠️ EVERY SAVE IN THIS GAME WAS A `File.WriteAllText` STRAIGHT ONTO THE LIVE FILE, AND
    /// THAT IS SAFE UNTIL THE ONE TIME IT IS NOT. `WriteAllText` truncates the target and then
    /// writes; if the process dies in between (a crash, a venue's power, somebody closing the
    /// laptop) the file on disk is a truncated fragment of valid JSON. **The next launch reads
    /// it, `JsonUtility` throws, and the load path quite correctly falls back to defaults**,
    /// which means the player's settings, their rebinds and their career are gone.
    ///
    /// ⚠️ THE EXISTING FALLBACK IS RIGHT AND IS NOT THE PROBLEM. `GameSettings.Load` already
    /// catches and uses defaults, with a note saying a build that refuses to start over a
    /// preferences file is worse than one running on defaults. That is correct. What was missing
    /// is any way to NOT reach that fallback: the choice was between the corrupt file and
    /// nothing, and there was no third copy.
    ///
    /// So a write here is three steps, and the target is never open for writing:
    ///
    ///     1. write the whole thing to `path.tmp`
    ///     2. move the current `path` to `path.bak`, replacing any previous backup
    ///     3. move `path.tmp` onto `path`
    ///
    /// A crash at any point leaves either the old file or the new one, whole, plus a backup.
    /// ⚠️ **Step 3 is a MOVE rather than a copy on purpose**: a move within one directory is the
    /// closest thing to atomic the filesystem offers, and a copy has the same truncation window
    /// the original code had.
    ///
    /// ⚠️⚠️ AND A READ FALLS BACK TO THE BACKUP BEFORE IT FALLS BACK TO DEFAULTS. That is the
    /// half that turns this from a nicety into a fix: without it, the extra file is written and
    /// never used.
    ///
    /// ⚠️ NOTHING HERE MAY REFUSE A MATCH. `docs/TODO.md` § 143.13: a replay, log or profile
    /// write failing must not stop a tournament match starting or finishing unless the data is
    /// genuinely required. Every method returns a value rather than throwing, and the callers
    /// keep their existing "use defaults" behaviour.
    /// </summary>
    public static class SafeStore
    {
        public const string TempSuffix = ".tmp";
        public const string BackupSuffix = ".bak";

        /// <summary>
        /// Write <paramref name="contents"/> to <paramref name="path"/>, keeping the previous
        /// version as a backup. Returns false and logs rather than throwing.
        /// </summary>
        public static bool Write(string path, string contents)
        {
            if (string.IsNullOrEmpty(path)) return false;

            string temp = path + TempSuffix;
            string backup = path + BackupSuffix;

            try
            {
                // ⚠️ THE DIRECTORY IS CREATED RATHER THAN ASSUMED. `Application.persistentDataPath`
                // normally exists, and "normally" is doing a lot of work: a fresh user profile, a
                // machine where the folder was cleaned, and a sandboxed Android install can all
                // present a missing directory, and the failure is a first-run-only bug that
                // nobody can reproduce afterwards.
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

                File.WriteAllText(temp, contents);

                if (File.Exists(path))
                {
                    if (File.Exists(backup)) File.Delete(backup);
                    File.Move(path, backup);
                }

                File.Move(temp, path);
                return true;
            }
            catch (Exception e)
            {
                // ⚠️ A FAILED SAVE IS A WARNING AND NEVER AN EXCEPTION OUT OF THIS METHOD. A full
                // disk, a read-only profile and a file locked by a sync client are all real, all
                // outside the game's control, and none of them is a reason to interrupt a match.
                Debug.LogWarning($"[SafeStore] could not write {path}: {e.Message}");

                // Leave nothing half-written behind for the next launch to find.
                TryDelete(temp);
                return false;
            }
        }

        /// <summary>
        /// The file's contents, the backup's contents, or null.
        ///
        /// ⚠️ `valid` IS THE CALLER'S TEST AND NOT THIS METHOD'S, because "corrupt" means
        /// different things per file: settings are corrupt when `JsonUtility` returns null, a
        /// career file is corrupt when its owner id is missing. Passing the predicate in keeps
        /// the recovery here and the meaning there.
        /// </summary>
        public static string Read(string path, Func<string, bool> valid = null)
        {
            string backup = path + BackupSuffix;

            string primary = TryRead(path);
            if (primary != null && (valid == null || SafeValid(valid, primary)))
                return primary;

            string fallback = TryRead(backup);
            if (fallback != null && (valid == null || SafeValid(valid, fallback)))
            {
                // ⚠️⚠️ IT SAYS SO LOUDLY. A silent recovery is a corruption nobody investigates,
                // and the second time it happens there is no backup left to recover from.
                Debug.LogWarning($"[SafeStore] {Path.GetFileName(path)} was unreadable or invalid; " +
                                 $"recovered the previous version from {BackupSuffix}. Something " +
                                 $"interrupted a save.");
                return fallback;
            }

            if (primary != null)
                Debug.LogWarning($"[SafeStore] {Path.GetFileName(path)} is present but invalid and " +
                                 $"there is no usable backup. Falling back to defaults.");

            return null;
        }

        /// <summary>Whether anything usable exists at this path.</summary>
        public static bool Exists(string path)
            => !string.IsNullOrEmpty(path) &&
               (File.Exists(path) || File.Exists(path + BackupSuffix));

        private static bool SafeValid(Func<string, bool> valid, string contents)
        {
            // A predicate that throws on a corrupt file must read as "not valid" rather than
            // taking the load path down with it.
            try
            {
                return valid(contents);
            }
            catch
            {
                return false;
            }
        }

        private static string TryRead(string path)
        {
            try
            {
                return File.Exists(path) ? File.ReadAllText(path) : null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SafeStore] could not read {path}: {e.Message}");
                return null;
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // Nothing useful to do, and it must not turn a failed save into a thrown one.
            }
        }
    }
}
