# Retained replay coordinates and bot producer retirement

Date: 2026-10-03. Baseline: `f9552d58a`, branch `competition-laptop-gameplay`.

## Retained replay world coordinates

`MatchPoseHistory.Track.Record` and `Capture` store the root position, rotation
and scale in world coordinates. Descendant samples are local. Both playback
methods apply the sampled root as a local transform, which requires a world
identity playback stage.

Previously, `RecordedWorldView` parented that stage to its overlay owner with
`SetParent(owner, false)`. A translated, rotated or scaled owner therefore moved
and resized the recorded bodies, while replay fields retained their world
coordinates. Current normal match owners are ordinarily identity transforms;
this is a concrete latent API boundary defect, with no claim that a current
ordinary halftime screen has already exhibited it.

The stage now remains world unparented, as the existing catch reconstruction
stage does. Its owner still receives the overlay canvas. This preserves the
recorded root transforms without dividing by parent scale or approximating
rotation beneath nonuniform scale. There is no recording schema, interpolation,
teleport, hero, asset or gameplay change.

The owning lifecycle was checked: `HalftimePresentation.End`, `OnDisable` and
`OnDestroy` dispose the view; the diagnostic caller uses a `using` scope;
constructor exceptions dispose partial state, and the halftime unavailable-view
path also disposes. `RecordedWorldView.Dispose` already destroys its stage.

`RecordedWorldOwnerSpaceTests.TransformedAndCollapsedOwnersCannotChangeRetainedWorldPoses`
uses the actual replay constructor, retained source tracks and drawing route.
It checks a rotated nonuniform owner and a zero-scale-axis owner, retained body
world position/rotation/scale, unchanged owner transform, render target creation
and detached stage destruction after disposal. It requires a real rendering
device and the Eskinita scene. The original fails and the candidate passes in
the isolated native run below.

## Bot action clocks across producer disable

Previously, `AIController.OnDisable` only unsubscribed events. A practice idle
or removal transition disabled that component and cleared or parked intent,
but `_windup`, `_lungeHeld`, `_aimHeld` and remembered presses remained. Reusing
the same controller could therefore resume an old hold clock. `DoWindup` only
initializes a fresh windup when `_windup` is false; `StepLungeIntent` similarly
starts fresh only when `_lungeHeld` is negative.

The existing `ReleaseAll` private clock reset is now shared through
`ResetActionState`, which `OnDisable` also calls. This callback leaves the input
table alone, preserving shared human hero input during possession. The practice
transition continues to own input parking and gameplay consumer cancellation.
Existing incapacitation release behavior is preserved.

`BotProducerLifetimeTests.DisablingProducerRetiresOldWindupsWithoutClearingSharedHeroInput`
uses real component enable/disable callbacks after seeding the private hold
state. It covers both hero-key ownership settings, empty remembered holds,
fresh windup state after re-enable and a shared Skill2 hold with no manufactured
release. It qualifies the retirement boundary rather than autonomous bot play
or cadence. The original fails and the candidate passes in the isolated native
run below.

## Evidence and limits

[Source evidence](source-evidence.json) records inspected source identities,
the original parent statement and a bounded transform-algebra check. For a
recorded position `(2.5, 1, 3)` beneath an owner at `(8, 2, -4)`, yaw 65 degrees,
the original stage produces world-position errors of 19.9892 metres with scale
`(2, 3, 4)` and 14.3431 metres with scale `(0, 2, 3)`. The detached identity stage
preserves that position in both cases. These are source-bound mathematical
checks, not Unity execution or measured in-game footage. The native fixture
also uses pitch and roll, which this smaller algebra witness does not model.

The original AI callback has no reset call; the candidate callback resets only
private action state. This first evidence file records inspected source before
native execution; its authored-but-unrun labels describe that earlier checkpoint.
The native receipts below supersede that execution limitation. One evidence-script decoding failure
was retained and corrected in the sole bounded retry by reading the existing
Git source as UTF-8.

Diff whitespace checks pass. Combined source and full PlayTests assembly
compilation passed against the installed Unity and cached package references.
This establishes compilation, separately from native execution.

The earlier approximately 3 GiB disk restriction was removed by owner cleanup.
Approximately 35 GiB is now available, and the isolated native worker at
`C:/Users/Matthew/dev/tump-laptop-native1003` has the full assets, packages and
these imported fixtures. The parent coordinated the sole heavy Unity job. The
source checkout stays sparse.

## Native changed-case qualification

Exactly these two PlayMode cases ran on the original AI/replay source:

- `TumbangPreso.PlayTests.BotProducerLifetimeTests.DisablingProducerRetiresOldWindupsWithoutClearingSharedHeroInput`
- `TumbangPreso.PlayTests.RecordedWorldOwnerSpaceTests.TransformedAndCollapsedOwnersCannotChangeRetainedWorldPoses`

Both runs used graphics D3D11, Unity 6000.5.8f1 and the isolated native worker.
The [original XML](native-original/tests.xml) records two causal failures:
`Old throw windup survived producer disable` and
`Recorded roots require a world-identity stage`. The replay constructor had
reached Ready before the stage-parent assertion. The AI enable,
seed and disable sequence occurs in one frame before an AI update can run, so
the private clocks are deterministic. Map setup disables every input writer,
parks the actors and cancels existing carrier windups; both fixtures reset the
world before and after the case.

Only `RecordedWorldView.cs` and `AIController.cs` changed for the corresponding
candidate run. The already qualified four input-cancellation production overlays
were unchanged in both runs. The [candidate XML](native-candidate/tests.xml)
passes 2/2 on the first run, with no fixture or assertion repair. Replay reaches
both owner variants, actual `Draw`, retained world TRS and stage disposal. AI
reaches both ownership variants, fresh state after re-enable and the unchanged
shared Skill2 hold/release edge.

[Summary](native-summary.json), source hashes, plans, full Unity logs and guard
receipts are retained in `native-original` and `native-candidate`. Original job
`ba684c19870f466d827714fcde4a9d42` and candidate job
`9febb8a115174c83bc4098eb5684889a` are terminal; both receipts confirm
`preservationCompleted=true` and `leaseHeld=false`. The original duration was
13.125 seconds and the candidate duration was 15.637 seconds; these are whole
test-run durations, not a performance comparison.

There is no player, actual-peer, hardware, graphics-pixel or human approval
claim. A replay render callback and created target are not a visual pixel or
authored appearance review.
