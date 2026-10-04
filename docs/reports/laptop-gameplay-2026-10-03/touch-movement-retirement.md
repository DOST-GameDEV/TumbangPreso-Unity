# Retiring a local reader clears its cached touch movement

Source review found that the existing focus/disable path clears InputIntent and
TouchInput.LookDelta, but leaves static TouchInput.Move unchanged. The enabled
reader's next Update copies the cached touch stick value back into its actor,
including after a reader re-enable. TouchStick releases its cache on PointerUp
or disable; this finding isolates the reader's retirement callback when neither
stick release has occurred.

The focused source correction clears only TouchInput.Move from CancelPendingInput
when this reader owns the local touch context. All offline reader seats qualify,
since SoloProvider.LocalSlot is 0 while the supported solo/debug reader can drive
another seat. In a networked session only the local slot may clear this shared
axis. A remote reader's disable/focus callback must leave local touch input alone.
No global focused flag, broad input blocking, backend change, network change or
blanket TouchInput.ReleaseAll is part of the proposal.

## Focused fixture

`TouchMovementRetirementTests.cs` has five authored cases:

- Focus loss clears the cached touch axis and its next reader frame.
- Reader disable/re-enable cannot reuse the old touch axis.
- Ordinary sustained touch movement remains held.
- Fresh touch movement after the focus recovery callback reaches the reader.
- A remote reader's disable and focus callbacks preserve local touch movement.

The fixture uses actual static TouchInput.Move, enabled PlayerInputReader.Update
and Unity focus-message dispatch. Motor simulation is disabled. The remote
ownership control uses an INetProvider seam with local slot 1 and remote slot 2;
it is not a network peer. Native execution is coordinated only by the parent.
Metadata GUID: `1a903dfce6e14129bcb779a4852d132e`.

## Current evidence

Original native `laptop-touch-movement-original1003` ran exactly five cases:
two causal cached-axis failures and all three passing movement/ownership
controls. Focus loss and reader disable each left TouchInput.Move at `(0, 1)`
instead of `(0, 0)`. Fresh post-focus input, ordinary sustained movement and the
remote reader ownership control passed. No fixture repair was required.

The parent ran Unity 6000.5.8f1 in the isolated native checkout with a named guard
profile, GPU classification, 2048 MB job memory and 1024 MB reserve. Fresh XML and
the original terminal guard receipt are retained in
[touch-movement-native](touch-movement-native/). Original XML records the actual
test window 2026-10-03 03:45:06Z; guard ended at 03:45:10Z with exit 2, preservation
completed and lease released. This is the expected product failure signature,
not a tooling launch failure or zero-case run.

Candidate `laptop-touch-movement-candidate1003` ran the identical five named
cases once: five passed, zero failed. Its fresh XML records the actual test window
2026-10-03 03:51:49Z; guard ended at 03:51:53Z with exit 0, preservation completed
and lease released. Candidate XML and terminal receipt are retained beside the
original evidence. No fixture repair or assertion change was required.

The native cases qualify cached-axis retirement, normal/fresh movement and
remote callback preservation. Earlier unchanged button-retirement evidence
remains separate and is reused; no unchanged tournament or full input suite
was repeated for this axis-only boundary change.

Physical OS focus, app suspension, real pointer-cancel delivery, stick visuals,
keyboard/controller background behavior, live peers and player builds remain
separate checks. This unit addresses the cached touch-axis boundary directly.
