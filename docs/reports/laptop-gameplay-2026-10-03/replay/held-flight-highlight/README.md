# Historical held and flight slipper highlighting

Date: 2026-10-03. Baseline source: `f9f7ecdfd3b4545305c58eb978a63c3e6e06ddfc`.
Original native causal failures are confirmed, the separate original transition
control passes, and the candidate passes all three scoped native cases.

## Defect contract

A viewer's own shoe receives the selected landed highlight after a genuine
flight ends. A historical held or flying pose must use the ordinary ownership
highlight choice, while the live current landing remains highlighted.

`RecordedWorldView` copies the source renderers' current property blocks.
`MatchPoseHistory.Track.ReadAccent` only captures character accents, and replay
drawing skips coat and accent writes for props without a recorded character
coat. This leaves the source's current landed rim strength, colour and outline
on the past slipper pose. The two independent native original cases confirm
this source mechanism.

The ownership choice is local to each viewer. Canonical recordings must not
copy the recording host's personal rim onto other viewers' shoes. The current
ordinary owner rim strength is zero; the landed rim strength is 0.85. The
expected historical choice must continue to use the shared live constants and
ownership wiring rather than introduce new tuning.

## Baseline question and stopping condition

The original run used exactly two cases in `TumbangPreso.PlayTests.RecordedSlipperHighlightTests`:
`CurrentLandingHighlightCannotPaintHistoricalHeldSlipper` and
`CurrentLandingHighlightCannotPaintHistoricalFlyingSlipper`, with graphics on
the isolated native worker. It imported the new fixture only and preserved the
already qualified replay stage and AI production source. No highlight product
source had changed.

The fixture starts with the actual local seat's equipped shoe and validates
its existing ordinary rim. It retains held and flight samples, performs a real
throw and waits up to two simulated seconds for a genuine loose landing. The
live renderer must then carry the selected landed rim. It draws the historical
held and flight poses and reads the copied renderer's rim and outline, with a
live-renderer preservation control. The two cases establish held and flight
failures independently rather than hiding the second behind the first
assertion. Both hooks reset the world; the highlight setting, launch flags and
selected/pinned rules are restored in a finally block.

The [original XML](native-original/tests.xml) reports two causal failures:
historical InFlight and Held copies each retained `0.85000002384185791` instead
of the ordinary rim `0`. Both cases passed their setup, ownership, actual
throw/landing, live 0.85 renderer and replay lookup checks first. There was no
fixture repair or original retry. Original job
`5d12db518ee548c9bf82c518e8b3cab7` is terminal with preservation completed and
its lease released, confirmed by the retained receipt. Plans and complete
Unity output remain in the isolated worker's original run folder. Curated XML,
terminal receipts and source hashes are retained for publication.

## Narrow correction and review regression

The candidate shares the existing live slipper highlight writer. Historical
Held and InFlight use landed=false and the viewer's live `_glowOn` ownership
wiring. The live Slipper reference is retained before catalogue art fallback,
so substituting recorded art cannot lose the local ownership choice. A missing
or destroyed source defaults to no owner glow. No host personal colour enters
the recording.

Review found a cross-state persistence error in the first unexecuted candidate:
overriding Held/Flight would leave that override on a later Loose frame.
The final correction therefore caches each slipper copy surface's exact
original property block after art setup, restores it before ordinary per-frame
replay writes, then applies the Held/Flight override. This preserves property
presence and material defaults, including catalogue fallback with an empty
block. It introduces bounded constructor storage, not per-frame block creation.
Recorded Sean/Zack attached effects own separate renderers/materials and sample
after these writes; they do not write into the copied slipper model blocks.

The added original-compatible control
`HistoricalLooseRestoresInitialHighlightAfterHeldAndFlightFrames` records a
genuine Loose sample after landing, draws Held, Flight and then Loose, and checks
the original cloned 0.85 rim and blue outline return. It was added because of
review, after the original two causal cases; the old two results did not qualify
this transition. The two existing entrypoints keep their original setup and
expected non-loose assertions. Only the new control ran on original source,
reusing the earlier two causal failures; the candidate then ran all three.

The recording lacks an explicit landed-from-flight bit. Loose continues the
original cloned property-block behavior; this correction does not infer
historical landing, drop, scatter or teleport semantics. Viewer settings,
source art, mechanics, recording format and network protocol are preserved.

[Source proof](source-proof.json) records original worker and Git source hashes,
their equality apart from line endings and the original fixture hash. The
review correction and added control have separately recorded current hashes;
no first-candidate native execution is claimed.

## Final native qualification

The [original control XML](native-original-control/tests.xml) passes 1/1 on
unchanged original Slipper and qualified-stage-only RecordedWorldView source.
Job `9a5c0c1467f34ae2a9e2bee0ff84a504` establishes that the initial copied Loose
highlight survives preceding Held and Flight draws in the original behavior.
It does not infer whether every historical Loose sample originated in a flight.

The [candidate XML](native-candidate/tests.xml) passes 3/3 on its first run:
the separate Held and Flight causal cases plus the same current-fixture Loose
transition control. Job `1c7251eb7b574c5f9d184d2fd392fa97` reaches the ordinary
non-loose rim, ink outline, preserved 0.85 live landing highlight and the return
to the original copied Loose choice. There was no native candidate fixture,
assertion or tooling repair. The cross-state restoration correction was made
during source review before this first candidate run.

[Native summary](native-summary.json) records all three runs and their case
names. Original causal job, original-control job and candidate job are terminal;
each receipt confirms preservation completed and its lease released. The
candidate [input hashes](native-candidate/input-hashes.json) match the frozen
two production files. The summary also checks current native-worker and primary
source/fixture hashes against the final frozen snapshot. Original two-case
fixture provenance and the subsequently added control are kept distinct.

All runs used native Unity 6000.5.8f1 Editor PlayMode with graphics D3D11 on the
isolated laptop worker. The preceding stage/AI unit was committed separately as
`67cfe70a4`. Only the two highlight production files changed for this candidate.
Raw complete logs and isolated profiles remain in the worker; publish the curated
XML, receipts and hash summaries, not those logs or profile files. This qualifies
the checked local rendering-property and state-transition behavior. There is no
pixel, autonomous-AI, peer, player, hardware or human taste claim.
