# A later validated save preserves a backup after failed restoration

The prior recovery unit proved successful primary restoration and a blocked
restoration's immediate fallback return. It did not cover a later save after that
restoration failed. Under a real Windows primary lock, Read returned the validated
backup but could not repair the corrupt primary. After releasing the lock, ordinary
Write replaced that primary and rotated its corrupt contents over the good backup.

SafeStore retains its exact two-argument Write method and adds an explicit
validator-taking overload. On Windows, the overload reads the existing primary and
applies the caller's usability predicate. An unusable or unreadable primary is
replaced with a null File.Replace backup argument, retaining the existing backup.
A usable primary still becomes the previous-version backup. The legacy method uses
the existing behavior, including write-only plain text such as FailureBundle.

SettingsStore, CareerStore, SocialStore and WalletStore Save now capture their
current path once and provide the same JsonUtility usability predicate as their
existing Read. No owner checks, schema, cache, profile aliases or non-Windows write
sequence change. Validated Windows writes add a primary read and validation; this
unit does not measure performance.

## Native evidence

Three EditMode cases call actual SafeStore.Read/Write on random owned temporary
files. FileShare.Read creates the actual primary lock. The frozen fixture's writer
adapter uses the existing two-argument API on the original source and the exact
three-argument overload on the candidate. That avoids compiling a baseline against
an API it does not yet expose. This qualifies the writer boundary; it does not
runtime-test the four stores or their account flows.

- Original run 45036: exactly 3 cases, 1 intended causal failure and 2 controls
  passed. Failed restoration, lock release and later save left the backup corrupt.
  Usable-primary rotation and legacy plain-text backup behavior remained valid.
- Candidate run 35856: exactly 3 cases passed. The validated writer preserves the
  recovery backup after failed promotion, still rotates a usable primary and leaves
  the legacy two-argument plain-text contract intact.
- Identical fixture and valid 32-hex metadata in both runs. Zero fixture, tooling or
  native repairs. The five production source files compiled in the candidate run.
- Every dependent launch followed direct preparation exit 0. Exact 7 owned inputs
  match MAIN/qualification; all 12,535 protected qualification hashes remained
  unchanged, including the direct lunge, frame-context and touch lifecycle units.
- Unity 6000.5.8f1, batch/nographics EditMode, profile safe-store-failed-promotion1002,
  CPU 1536 MB plus 2048 MB reserve, 450-second ceiling. Both guards are terminal,
  preservation completed and no lease remains. Teardown deletes only known owned
  files and the empty directory without recursion. No browser or preview opened.

Raw baseline/candidate XML and job receipts, input manifests, case counts and
protected-input result accompany this report. Full logs, original source snapshots
and protected hashes remain in local Logs/safe-store-failed-promotion1002.

Acceptance is Windows Editor temporary-file behavior. WindowsPlayer execution,
Android/non-Windows replacement behavior, real account persistence and interrupted
power are not qualified. The frozen 1002h player predates this change.
