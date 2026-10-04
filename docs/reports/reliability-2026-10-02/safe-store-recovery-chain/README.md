# Usable backups survive recovery and refused Windows saves

Two actual SafeStore paths could discard the only usable backup:

1. Read recovered a validated backup but left the corrupt primary in place. The
   next normal Write rotated that corrupt primary over the good backup. Settings,
   career, wallet and social load/save callers share this service.
2. Write deleted an existing backup before trying to move the primary. A Windows
   sharing lock could refuse that move after the usable backup was already deleted.

Read now attempts to restore the validated backup over the bad or missing primary,
without rotating or modifying the backup. A refused restoration still returns the
validated fallback and includes the restoration failure in the existing warning.
The copy is not an atomic promotion: an interrupted copy could leave a partial
primary, while the usable backup remains available for another recovery.

For WindowsEditor and WindowsPlayer, Write now uses File.Replace for an existing
primary instead of deleting the old backup before replacement. The existing API,
return value, failure warning and temporary-file cleanup remain intact. Other
platforms keep their existing Write move sequence; their refused-save ordering is
unchanged and is not qualified by this unit.

## Native evidence

The four EditMode cases call actual SafeStore.Read/Write on random task-owned
temporary files. FileShare.Read creates a real Windows primary-file lock. No player
profiles, accounts, live services or shipping maps are used. Teardown deletes only
the three known owned files and removes the empty directory without recursion.

- Original run 51480: exactly 4 cases, 2 intended causal failures and 2 controls
  passed. Recovery followed by save produced a corrupt backup; refused Write under
  the owned lock removed its existing backup. Ordinary backup rotation and reading
  the fallback under a primary lock remained valid.
- Candidate run 83775: exactly 4 cases passed. Both backup-loss paths are corrected
  on the tested Windows Editor path, and the same two controls remain valid.
- Native fixture and valid 32-hex metadata were identical in both runs. Zero fixture
  or native repairs. One bounded preparation bookkeeping correction repeated the
  unchanged exact-file preparation to capture a verifiable terminal exit 0 after
  an external process-wait could not retrieve the first preparation's exit code.
  The first qualification-before snapshot was retained.
- All dependent native launches followed direct preparation exit 0. Exact 3 owned
  input hashes match MAIN/qualification, and all 12,531 protected qualification
  hashes remained unchanged.
- Unity 6000.5.8f1, batch/nographics EditMode, profile safe-store-recovery-chain1002,
  CPU 1536 MB plus 2048 MB reserve, 450-second ceiling. Both guards are terminal,
  preservation completed and no lease remains. No browser or preview was opened.

The installed Editor's .NET reference exposes File.Replace but not the modern
three-argument File.Move overwrite overload. The chosen API also documents the
replacement and backup operation. [Microsoft File.Replace documentation](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.replace).

Raw baseline/candidate XML and job receipts, owned-input manifests, case counts and
protected-input results accompany this report. Complete logs, protected snapshots
and original source remain in local Logs/safe-store-recovery-chain1002.

This accepts native Windows Editor file behavior on owned temporary paths. It does
not simulate a power interruption or qualify WindowsPlayer execution, Android,
other filesystems, owner-account persistence or a complete player build. The frozen
1002h player predates this SafeStore change.
