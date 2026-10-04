# Preserve match history when a server write fails

Baseline:67d0980dd8d33d120bb3784938376f9c9554e712, October4.

The submit endpoint saved the career profile before match history. The profile
includes AppliedMatchIds. If its write succeeded but the following history write
failed, resubmission found the applied id and skipped the history write forever.
The profile counted the match but history contained no corresponding record.

The endpoint now saves history before publishing the profile's applied marker.
A failed history write leaves the marker unpublished. A failed profile write
leaves the history saved; retry counts the profile once and remember() retains
one history entry. Existing duplicate and offline behavior is unchanged.

`node tools/test_match_record_persistence.js` invokes the actual exported endpoint
with a fresh local Cloud Save stand-in per scenario and no service token. Its
one-shot injected errors reject actual setProtectedItem awaits. No production
test hook, SDK installation, network request or live profile is involved.

Original:1 failure/4 controls. The history-write-failure scenario completed its
retry with zero history entries instead of one. Corrected:5/5. Controls cover
profile-write failure, duplicate reward/history suppression, retaining an earlier
different record and refusal to reward/write offline records. The current travel
contract remains6/6 against actual Core and the existing digest contract passes.
Zero tooling retries. No Unity or player launch.

This is a write-order correction, not an atomic transaction or concurrent-writer
solution. It prevents the reproduced future partial-write failure. It does not
recover entries already missing behind persisted applied markers. SDK/service
deployment and actual packaged delivery remain open; no remote deployment is
claimed by this unit.
