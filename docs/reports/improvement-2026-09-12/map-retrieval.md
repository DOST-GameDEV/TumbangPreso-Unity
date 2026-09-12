# Under-guideway flight and direct release

Source baseline: 0f08e9921c96148bf0c332d09b8392497f0cc82e, ASTRAReworks.
This is a focused correction, not complete map, network or release qualification.

## Two separate causes, measured separately

The archived direct HostThrow experiment mixed a carrier-reference defect with a
ground query defect. MapRetrievalProbe now disables AIController, PlayerInputReader,
DebugPlayerSwitcher and ReadyGate, clears/parks intent and cancels old charge before
testing. The actual carrier and slipper physics remain active.

1. Direct `Slipper.HostThrow(owner, ...)` passed the same owner into
   `ReleasePreviousHolder`. That helper deliberately keeps an unchanged holder
   during grabs/snapshots, so the throw left `Carrier.Held` pointing at the flying
   shoe. `Carrier.RideAnchor` in FixedUpdate/LateUpdate could then overwrite its
   position. Baseline assertion fails immediately on the retained reference.
   Normal runtime throws currently enter through Carrier.HostThrowAt, which
   clears Held afterward; the direct-call defect explains the exploratory fixture,
   not an established ordinary-input failure of that carrier path.
2. `Carrier.HostThrowAt` clears Held correctly, but its throw from (2,3.6,-2)
   becomes Loose after the first physics step and returns to (-1.8,.0918,9).
   GroundY scans six metres above the flight and selects the 9.04 m guideway top.
   FixedUpdate therefore calls Land; the unreachable-surface recovery returns the
   shoe to its owner. This reproduces with carrier ownership already cleared.

The correction makes a throw detach its previous carrier independently, and
samples flight support below the step's maximum previous/current height. Using
the previous height preserves fast descending contact with raised ground. The
sample is passed to Land so landing does not rescan an overhead roof. The broad
GroundY placement query and unreachable-roof recovery remain available elsewhere.

## Evidence

All launches use `tools/run_unity_guarded.py`, Unity 6000.5.8f1, Win64, fresh
PlayMode XML and a single MapRetrievalProbe filter. Seventeen existing profile
files were restored and hash-verified after every run.

| Run | Result | Meaning |
|---|---|---|
| Logs/map-retrieval-before.log | Compile failure | Initial fixture called a private cancel method; corrected with test-side reflection, no runtime changes |
| Logs/map-retrieval-before-v2.xml | 0 passed, 2 failed | Independent direct-holder and actual carrier-flight failures on unchanged runtime |
| Logs/map-retrieval-after-v1.xml | 4 passed, 0 failed | Both reproductions pass; fast descent onto a 1 m slab and unreachable 3 m roof recovery also pass |

The corrected carrier flight travels from x=2 to x=3.2730 over five 0.02 s steps,
with y rising from 3.6 to 3.7942. Carrier.Held remains null and state remains
InFlight while the unchanged broad GroundY still reports 9.04. Original and
corrected traces are in the corresponding Logs/map-retrieval-*/ folders and copied
beside this report. The broad query returning the roof is no longer used to decide
whether an under-guideway flight has landed.

## Boundaries

Additional regression checks:Core562/562, fresh graphical EditMode489/489, all14
gating source audits pass. The informational audio audit flags7 files. The first
EditMode attempt used -nographics and failed two near-light fixture checks; the
graphical rerun passes without changing those tests or runtime lighting.
Checks.RunAll also passes8/8 in Logs/continuation-checks-v1.log. Test-generated
arm tangents, quality-setting changes and regenerated map IDs were restored to
the initial clean bytes. No final Windows build or full isolated gate is claimed.

No evidence connects these defects to the historical 48-idle-penalty run. Its
aggregate report survives, but the original seed-specific resting-position trace
has not been located in this checkout. Keep that investigation open. Broader
wall/kerb/trunk/pillar/monument/kiosk route coverage, real FPP, all quality profiles,
both modes, separate-process throwing and exact-player review remain required.
The earlier unsuccessful patch and fixture stay preserved as historical evidence
under improvement-2026-09-10/map-investigation.
