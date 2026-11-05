# Banaue lag spikes, Banaue bugs, Paete escape and review of 1cf46fdd7 (2026-11-05)

Owner report: Banaue is "ultra laggy" with frequent lag spikes. Review scope: the
multiplayer fixes on main `1cf46fdd7` (see `../multiplayer-fixes-2026-11-04/`).

## Root cause (measured, not inferred)

`FrameBudgetProbe` gained an opt-in `-tp-spike-scripts` switch. During a `-tp-trace` run
it records every script invocation marker and writes `MAP-spikes.txt`, naming the scripts
behind each frame over 35 ms. It also turns on gated timing inside `MatchPoseHistory`.

In a development player (Payo, four bots, 1920x1080, High tier):

- nearly every spike was `MatchPoseHistory.LateUpdate` at 83-119 ms, allocating about 15 MB;
- within that, `MatchReplayArchive.Sample` -> `Retain` took 77-92 ms, of which
  `RecordedMatchClip.Encode()` took 69-87 ms for a clip of about 60 KB. `Encode` writes
  every recorded float and then decodes the whole clip again to validate it. This ran on
  the host's main thread 1.4 s after every catch moment;
- pose track rebuilds were cheap (14 bones, 0.2 ms), and replay segment flushes ran every
  3 s while the spikes did not line up with them.

## Changes

- `MatchReplayArchive.Retain` encodes on a worker thread. `PublishEncoded` (Update) then
  does the unchanged hand-off: `RecordedClip`, shortlist and capacity, `RetainedClip`,
  `StageReplay`. Results from another match, or after the archive is disabled, are dropped.
  `InvalidDataException` still sets `LastSkip`; any other encode failure is logged.
- `WorldGround.TryBelow` steps down nearest hits instead of collecting all hits. The 64-slot
  buffer added in f341ea52b fell back to allocating `RaycastAll` under stacked terraces,
  in per-frame callers (contact shadows, dust, the 192-point throw ring). It gives the same
  answer: backface queries are off, and a cast starting inside a collider does not report it.
- `UltimatePhaseView` rescans live arms, effects, particles, callouts and fields at 10 Hz
  rather than with about fourteen whole-scene `FindObjectsByType` calls every frame, and
  forces a rescan when each phase starts.

## Evidence (`Logs/banaue-perf-20261105/`)

| Payo, 60 s | before (`dev-fixed`) | after (`dev-after`) |
|---|---|---|
| frames > 80 ms | 10 | 0 |
| frames > 50 ms | 12 | 2 |
| p99.9 / max | 101 / 104 ms | 43 / 67 ms |
| GC | 8.7 MB/s | 7.3 MB/s |

Tests: EditMode `TerrainPresentationTests` 4/4. Exact PlayMode cases 4/4:
`ReplayProbeCatchContactTests.ProbeContactRetainsAnActualCatchClip`,
`ReplayArchiveRecoveryTests.UninterruptedArchiveStillRetainsANewActualCatch`,
`SharedUltimatePhaseTests.LiveBlindfoldSkillsAndCompanionsCannotObscureTheUltimatePicture`,
`SharedUltimatePhaseTests.YasminOnBanaueUsesTheTrimmedIntroductionAndReturnsTheHud`.

## Review notes on 1cf46fdd7

- The voodoo validator change is bounded: the slot is checked by `ValidBody`, and mark and
  reach sources remain player seats.
- The round card, the blindfold epoch guard, catch reconstruction for companions and the
  trimmed Yasmin shot selection read correctly. No regression found beyond the two
  per-frame costs fixed above.

## Limits and remaining hitches

- Bot benchmark only. Not measured in a networked match, and not with a human aiming the
  throw guide during an ultimate.
- One-off first-use hitches of 30-67 ms remain in the first seconds of a match:
  `HeroAbilitySystem.Update` (first cast), `Slipper.FixedUpdate`,
  `DanteBoulderStone.LateUpdate`. Banaue's edge catchers (Kanlit, hawk, bubble spirit)
  also load lazily on the first fall. These are candidates for loading-screen prewarm.
- The local replay flush still costs 12-22 ms every 3 s (`archive.Local`), and clients
  decode a received highlight on the main thread.

## Banaue bug sweep

Logs from every Banaue probe, plus a full four-round bot match (`fullmatch2/`, 420 s, rounds 1-4, breaks, halftime
night, result), had no warnings or exceptions after the fix below. The previous runs logged exactly one Banaue warning:

- `Renderer 'house_long far' is registered with more than one LODGroup`. The FarTown builder puts a house's low copy
  beside its group when the mesh is on the house root, and `PayoSceneBuilder.MeshLevels` then gave that copy a second
  LODGroup (`_lod1`, `_lod2`). The scene now chains near, far, far_lod1, far_lod2 in the house's own group and drops the
  stray group. The builder skips any renderer an existing LODGroup already references, so a rebuild cannot repeat it.

## Paete ultimate: the struggle and the escape (owner request)

Holding F while rooted now reads as working free on every screen and every Hero Strike body:

- `CharacterMotor.PresentedBreakFreeProgress`: the real hold clock on the owner and the host; any other peer times the
  replicated struggle flag on the same 7 s. No wire change (protocol stays 189). Escape is still decided by the host only.
- `PaeteRootBands` peel (0 to 1): creeper and leaves tear first, flower at 0.42, outer root unwinds from its tip
  (0.34-0.78) with the loose end flailing, then the inner root (0.58-0.96). The break-out snap is unchanged.
- `PaeteEmbraceFx.PeelOff` debris and a smaller crack at each milestone (`PaeteRootCoil.StepPeeledOff`).
- Bands are measured to each body (`PaeteRootCoil.MeasureLegs`, the leg-bone vertices) against Sean, who the keys were
  laid round. Measured ratios to Sean (width, depth, height): Nemu 0.70/0.72/0.74, Phaister 1.30/0.97/0.85,
  Dante 0.85/1.10/0.84, Amihan 0.81/0.93/0.93, Cheska 0.87/0.82/0.85, Rafi 0.85/0.97/0.94, Zack 0.90/0.81/0.83.
- `CharacterAnimator.RootedSquirm`: a procedural layer over `rooted-struggle` (whose speed now rises from 1.1 to 2.0).
  The trunk wrench, side bend and head thrash widen and quicken with progress; the hands jam down at the thighs and
  tear up in turn; past half the knees come up; near the end the body bucks. It stands down for throws and the heave.

Tests (exact, PlayMode 4/4): new `SkillReceiptTests.WatchedPrisonerPeelsWithTheReplicatedStruggleAndKeepsItsProgress`,
new `SkillReceiptTests.RootBandsAreMeasuredToEachHeroStrikeBody`, existing
`ReceivedRootsOwnTheirRestraintWithoutASentryAndAcceptLateFacingOnce` and
`RemoteInteractionHoldsNeedHostTimeAndExpireWithInputOrEpoch`. The look has not been reviewed by eye or captured.

## Measured but not fixed (next work)

- Ultimate start freezes 100-300 ms. Laps (`ultlaps/`): `new HeroIntroductionScene` is 50 ms for Nemu and 217 ms for
  Dante, `ChooseShot` 12-18 ms; everything else is under 10 ms. Prewarm or cache each picked hero's scene at load.
- Field capture at 20 Hz costs 2.3-3.0 ms per sample (about 20 `FindObjectsByType` in `RecordedSpecialFields` and
  `WorldEffectSnapshot`). A per-type live registry would remove it.
- One-off hitches: halftime start 87 ms, catch replay start 44 ms, match end 475 ms, first ability cast 30-45 ms.
