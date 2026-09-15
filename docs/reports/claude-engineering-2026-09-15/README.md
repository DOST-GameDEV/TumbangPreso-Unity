# Claude engineering lane evidence, 2026-09-15

Scope: C1, C2 and C3 in `docs/CLAUDE_ENGINEERING_LANE.md`. Machine: macOS (Apple
silicon, 16 GB), Unity 6000.5.8f1 with Mac and WebGL modules only, no dotnet, python3
3.14. Every Unity launch here is `-buildTarget OSXUniversal`, not the Win64 delivery
target. `tools/run_unity_guarded.py` is Windows only, so launches used
`run_unity_mac_guarded.py` beside this file (snapshot and hash-verified restore of the
Mac persistentDataPath). The fixture sweeps used `fixture_seeds.sh`, which launches
Unity directly the same way `tools/bot_sweep.py` does. Raw Logs are local; the files
kept here are the reports and NUnit XML receipts.

Starting point: pulled ASTRAReworks `4752f610`, fast-forwarded to `0f1d997b` during the
session. Pre-fix current sweeps ran on `4752f610` plus the idle trace; everything after
the pull ran on `0f1d997b` or later. Upstream `0f1d997b` touched hero kits and
networking, not the retrieval or lunge code, so Classic arms are the cleanest before and
after comparison and Hero Strike arms carry that extra difference.

## C1: Ilalim unretrieved-slipper outlier

### What the historical record is

`docs/reports/bot-sweep-2fde55d32246.md` prints per-seed rows only for Classic on
Eskinita. The Ilalim arm's per-seed values existed only in `Logs/bot-sweep.json` on the
Windows machine and are not in the repository, so **the seed that produced 48 is not
recoverable from committed data**. The six seeds were 20260823, 1, 7, 4242, 20260904 and
99991; one of them carried all 48.

### Reproduction attempts on the historical build

`2fde55d3` was extracted with `git archive` into a scratch project, given the same
per-penalty trace (`c1/historical-2fde55d3/patch_idle_trace.py`, later the movement and
emote fields of `091f9210`), and run on this Mac.

| run | Ilalim idle penalties |
|---|---|
| whole fixture, the six sweep seeds | 0, 0, 0, 0, 0, 0 |
| Ilalim only, 12 further seeds | 0, 0, 2, 0, 2, **13** (seed 42), 0, 1, 1, 0, 0, 0 |
| seed 42 again, twice | 0, 0 |

**The historical build is not seed-reproducible on this machine.** Classic seed 20260823
measured 161 throws here against 185 in the committed report, and seed 42 went 13, 0, 0 on
one machine. The exact 48-penalty match cannot be replayed.

### What the traces show, historical and current

Every penalty line records the charged seat's plan, goal, stall time, hold state and every
slipper carrying its label with the pickup rule's own answer. **No penalty in any run
traced a stranded slipper**: every charged seat's shoe was Loose, active, on the ground
(`dy` 0.00 to 0.01) and inside the court. The penalties came from bot decisions.

1. **The one-runner yield deadlock, on both builds.** `PlanAttacker` evaluated
   `!FetchIsSafe(...) || !IHaveTheBestRun(...)`. `FetchIsSafe` returns true for a late
   clock, for stall patience and for a downed can, and the one-runner rule's own comment
   says those bots "never reach this question", but `||` still asked it. A late bot
   yielded to a higher-ranked rival even when that rival was not running.
   * historical seed 42 round 1: seat 1 `plan=Stalk stalk=10.4..14.4 idle=11..15`, yielding
     while the better-ranked seat 3 did not move (below). Both charged together.
   * historical seed 11: seat 1 `plan=Stalk stalk=4.22 idle=11`.
   * current Hero Strike Eskinita seed 4242: seat 3 `plan=Stalk stalk=10.74 idle=11`, went
     the second seat 0 picked up.
   * current Ilalim seed 20260904: seat 2 `plan=Stalk idle=11`, fetched only once seat 1 held.
2. **A fetching seat that did not move, historical seed 42 only.** Seat 3 stayed at
   `(-4.63, 0.08, 11.11)` in `plan=Fetch` for five seconds with its shoe 7.5 m away. The
   trace that ran then did not yet record move axis, velocity, stuck timers or emote
   state, and the two re-runs did not repeat the match. **Why that body did not move is
   unexplained.** Nothing in 18 current-build matches after the fix showed it.
3. Ordinary late fetches: one to three penalties while a seat that waited for a taya
   crossed the court. These are the rule working.

### Fix and regression

`32e073fd` adds `AiRetrievalRules`: the three overrides now skip the yield, which is what
the one-runner comment already specified. EditMode `AiRetrievalRulesTests` 5/5
(`c1-editmode-v1.xml` locally).

Whole fixture, six seeds each (18 matches per column):

| arm | idle before (4752f610) | idle after (091f9210) | tags before | tags after |
|---|---|---|---|---|
| Classic Eskinita | 0 (0-0) | 0 (0-0) | 146.8 (141-155) | 146.5 (138-152) |
| Hero Strike Eskinita | 0.8 (0-5), 5 total | 0 (0-0) | 132.8 (121-143) | 131.3 (123-144) |
| Hero Strike Ilalim | 0.5 (0-3), 3 total | 0 (0-0) | 132.5 (119-140) | 126.7 (117-136) |

Throws and retrievals stay inside the before range on every arm (receipts in
`c1/current-before` and `c1/current-after`). The after column includes C2; the isolation
sweep with only C1 is recorded in the execution log when it finishes.

### Bounded relationship to the historical 48

Demonstrated: the yield deadlock charged idle penalties on the historical build (seeds 42
and 11) and on the current build, and the fix removes it from 18 current matches.
**Not demonstrated**: that it produced the 48. The seed is unrecoverable, the build does
not replay, and the one historical outlier caught here also contained a non-moving fetcher
that stays unexplained. The historical attribution therefore remains open. Preserved and
untouched: raised ground, roof and pool retrieval, the 10 second off-roof penalty, pickup
radius and all penalty rules.

## C2: bot lunge decisions

Whole-match lunge traces, `AiDiagnosticProbe.{Classic,Hero}LungesAcrossAWholeMatchAreExplained`,
Eskinita, stepped at 1/60 s with time scale 1 (an ordinary 60 fps frame time for every bot
decision; only the wall clock is removed). Seeds 20260823, 1 and 7 in both modes. Each
test asserts the tracker counted the same releases as `MatchStatsCollector`.

Before (`8bf42787`, C1 fix present, lunge fix absent):

| run | lunge hits / attempts | released after the taya's own punch tagged |
|---|---|---|
| Classic 20260823 | 10/110 | 89 |
| Classic 1 | 11/125 | 107 |
| Classic 7 | 11/109 | 89 |
| Hero 20260823 | 7/112 | 91 |
| Hero 1 | 12/107 | 91 |
| Hero 7 | 10/108 | 89 |
| **total** | **61/671, 9.1%** | **556, 83%** |

Every one of those 556 had a fresh in-range charge on a taggable victim (`start=[held=0.017
... taggable=True]`), a tag scored by the same taya during the charge, and a release made
by the plan sweep rather than by `StepLungeIntent` (`heldAfterRelease` > 0). The cause:
`StepLungeIntent` opens a charge at up to `LungeRange` (2.6 m on Normal), keeps driving in,
and its punch branch tags first at `PunchRange` 1.7 m. The victim is reset, `TagTarget`
empties, the button is released and `CombatVerbs.StepLunge`, which has no cancel, dashes at
nobody. **Aim was not the main fault, and the historical 5/125 was mostly this artifact.**

Fix `eb29f871`: `AiLungeRules.PunchWillArriveFirst` refuses to open a charge when a ready
punch will be in range by the end of the hold. EditMode `AiLungeRulesTests` 3/3. No lunge,
slide or punch number changed.

After (`eb29f871`):

| run | lunge hits / attempts | punch-dumped |
|---|---|---|
| Classic 20260823 | 10/13 | 0 |
| Classic 1 | 13/19 | 2 |
| Classic 7 | 13/18 | 0 |
| Hero 20260823 | 11/14 | 2 |
| Hero 1 | 4/7 | 1 |
| Hero 7 | 6/11 | 2 |
| **total** | **57/82, 70%** | **7** |

Run-to-run noise: hits per match 7-12 before and 4-13 after; attempts 107-125 before and
7-19 after. The whole-match sweep above is the tag-rate check: Classic tags 146.8 → 146.5.
Hero Strike tags on Ilalim read 132.5 → 126.7 with overlapping ranges and an upstream kit
change between the two, so the C1-only isolation sweep decides whether that move belongs
to this fix. **Human slide and lunge feel are out of scope and were not judged.**

A tracker fault was found and fixed in `844da009`: a lunge tagging on its release frame was
counted as a punch tag. The before table separates the two cases by `heldAfterRelease`,
which that fault did not affect.

## C3: slipper lookup cost

### Method

* **Frequency**: `c3/instrument_slipper_lookups.py` rewrites every runtime
  `FindObjectsByType<Slipper>(` in a `git archive` copy of `091f9210` into a call through
  `c3/LookupMeter.cs.txt`, which counts calls and time per `file:line`. Production code in
  the checkout was never edited for this. 34 sites instrumented.
* **Workload**: `SlipperLookupCostProbe`, one whole default match each of Classic on
  Eskinita and Hero Strike on Ilalim, four bots, 1/60 s steps, time scale 1, seed 20260823.
  Offline host, so request and packet paths are not exercised (the network seat-to-motor
  and seat-to-slipper caches were not touched, per the brief).
* **Unit cost and allocation**: each query shape timed 20000 times in the loaded arena,
  allocation as the median heap growth over 1000-call batches. The calibration row
  (`new Slipper[4]`, 81.9 B) proves that counter works; the first two methods did not
  (`GC.GetAllocatedBytesForCurrentThread` read 0 B for the calibration array, pausing the
  collector throws in the editor).
* Receipts: `c3/measured/` (instrumented, 2/2), `c3/live-checkout-*` (the committed probe in
  the unmodified checkout, 2/2, meter absent).

### Result (instrumented copy, macOS editor)

| | Classic Eskinita | Hero Strike Ilalim |
|---|---|---|
| scene | 3 active + 1 parked Slipper, 3233 components | 3 + 1, 6199 components |
| lookups | 134.9 per simulated s, 2.25 per frame | 134.5 per s, 2.24 per frame |
| unit cost, active-only | 33.7 µs | 49.8 µs |
| held-list scan, same slippers | 0.09 µs | 0.10 µs |
| all sites | 0.086 ms per frame | 0.167 ms per frame |
| lane sites (AIController, CombatVerbs, Carrier) | 0.052 ms per frame | 0.096 ms per frame |
| RoundDirector.cs:408 (outside the lane) | 0.034 ms per frame | 0.067 ms per frame |
| batchmode editor frame | 1.21 ms | 1.70 ms |

Per site, Hero Strike Ilalim, calls per simulated second and the path each is on:

| site | per s | path |
|---|---|---|
| AIController `MySlipper` (1672) | 48.7 | per frame for an attacker in Fetch, Stalk or Position, plus the think tick |
| RoundDirector idle monitor (408) | 47.0 | per frame, host (outside lane) |
| AIController `TryCoverPoint` (4001) | 19.0 | per frame in Cover, per think tick for a defender |
| AIController `RivalShotIsInbound` (954) | 8.8 | think tick, plus Hero Strike ability weighing |
| StreetTripHazard (239) | 3.6 | map hazard (outside lane) |
| AIController `TryInterceptPoint` (1693) | 2.8 | defender think tick and Intercept frames |
| CombatVerbs `SweepSlideRetrieval` (628) | 1.8 | per frame while a slide is live |
| Carrier `TryPickup` (710) | 1.8 | per grab press |
| CombatVerbs `FindSlideTarget` (517) | 0.5 | per slide press |
| AIController `SlipperOwnedBy` (1244) | 0.3 | think tick |
| hero kits, `NearestFlyingSlipper`, `TryGlanceAt`, stats | < 0.1 each | per cast or rare |

Allocation: the active-only and sort-mode shapes measured 0.0 B per call at this counter's
resolution, `Include, None` 160 B. At 135 lookups a second that bounds the lookups at about
22 KB a simulated second in the worst shape, and the whole matches ran 3 to 6 gen0 collections.
Per-call µs in the site table include the meter's Stopwatch overhead; the unit rows do not.

### Decision

**No production change.** Every lane-owned slipper lookup together costs 0.05 to 0.10 ms of
a frame here, about 0.3 to 0.6 per cent of a 16.7 ms frame. A registry would recover nearly
all of it, but it would have to model active-only and include-inactive queries separately and
keep the defender's parked shoe visible to diagnostics, for a saving below the frame-to-frame
noise of a real player. Semantics are unchanged: the active-only sites still exclude the parked
shoe and `NetStateReport` still includes it.

Limits, stated plainly: measured in the macOS editor, not an IL2CPP Windows player or a lower
spec laptop; a machine three times slower would put the lane sites near 0.3 ms. The largest
single site after `MySlipper` is `RoundDirector.cs:408`, outside this lane. This says nothing
about general game performance.
