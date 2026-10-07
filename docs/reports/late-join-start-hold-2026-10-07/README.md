# Late-join opening hold

The Feedback report says rejoining or late-joining players remain frozen.
A fresh client always opened its own introduction, then held simulation until
receiving a countdown. A host already playing never sends that old countdown
again. Rebinding a seat could also reopen the same introduction.

ReadyGate now adopts an already-confirmed host match instead of waiting for a
past countdown. It cancels only its own opening, consumes its old countdown and
leaves round state, scoring and match advancement to the existing host snapshots.
It preserves the requested pause, real Frozen status, intermission inactivity
and an incoming shared ultimate hold. Hosts retain their ordinary start barrier.
There is no wire change; protocol 153 is unchanged.

## Evidence

- Original production source: four failures and one pre-start control pass.
- First candidate: eight focused native cases pass.
- Expanded integration: seventeen pass, one existing countdown assertion fails.
- That assertion also fails unchanged on the original production source: it
  expected six cues although the shipped countdown is 3,2,1,GO. The test now
  checks those exact four strings, retaining malformed-message, deduplication
  and reopening checks.
- Final combined native run: eighteen pass, zero failures or skips. This includes
  actual owner CharacterMotor displacement, pause/Frozen preservation, incoming
  ultimate ownership, pre-start/host controls and completed-arrival actor freeze.
- The restored managed toolchain passed 731 Core tests on the base revision.
  Core production source is unchanged by this correction.

Raw NUnit receipts, failures and source hashes are retained here. Runs use native
Unity 6000.5.8f1 with OpenGL on Linux and named isolated profiles. The profile guard
confirmed restoration after each terminal run. Initial import metadata changes
were captured separately and restored; they are not part of this patch.

These tests exercise native state/lifecycle and motor behavior. Separate local
standalone peer and normal keyboard evidence follows below. Windows/WAN and
human acceptance remain open.

## Internal player build

An actual Linux standalone player built successfully with the unchanged checked
runtime source: 2,865 MB, protocol 153, base d1e3845 plus this dirty patch.
Two graphics-enabled build attempts exhausted the cloud memory limit. Headless
build execution completed shader compilation with the same 83 shaders and 157
warmup variants, then encountered a full disk at managed linking. Removing unused
new-project prewarm caches and a verified redundant installer allowed the warmed
retry to finish in 67 seconds. Editor, licenses, source and project caches remain.
This is an internal Linux artifact, not a replacement for the owner's desktop
build or proof of Windows compatibility. Standalone peer checks are separate.


## Real local transport and keyboard input

A headless host and a separate OpenGL client used the exact internal player.
The client joined after the host had been listening for 40 seconds, exited and
reconnected with the same isolated identity. Normal PlayerInputReader was used
on the client, with native OS D/W key holds rather than injected movement intent.
Both 70-second client reports ended as CLIENT, networked True and round active
True on Eskinita. The rejoined client's forward movement was visually inspected
in the same live round; before/after screenshots are retained.

The recorded 18.6 m and 20.3 m travel totals include any spawn/round changes; they are
not isolated movement-speed measurements. Cloud software rendering was only
about 3 FPS and the unrelated UGS Wire websocket failed, although local UDP was
connected. These results establish functional local recovery, not target-device
performance, Relay/WAN compatibility or human acceptance.

Earlier all-bot receipts are retained in the cloud workspace: the first client
had zero owner travel, so that run was not counted as a movement pass. An extra
keyboard sample visibly moved but outlived its host, so its disconnected final
report was rejected and the lifetime-correct pair above was run instead.

The latest incoming menu work was then fast-forwarded to 19bf07dc without changing
the corrected ReadyGate. All 18 native checks passed again on that integrated
source. The standalone artifact above remains explicitly based on d1e3845.

Publication integration at f3e4c013 also passes all 18 native controls.
