# Zack speedster verification, 2026-10-25

State: implemented locally, uncommitted on main at `1395ce099`. Nothing was
committed or pushed. Owner visual/audio approval and actual peer acceptance are
still open. This is not a release or full regression-gate claim.

## Completed in this session

- Real Unity compilation and import succeeded in 6000.5.8f1 with D3D11.
- Baked and bound all five Zack clips. The author checks every path against the
  real rig and refuses clips with fewer than 21 curve bindings.
- Backtrack checks floor, court bounds and walls against the recorded path.
  Its predicted teleport still calls the ownership-enforcing motor API and keeps
  the held slipper and statuses.
- Afterimage draws a mesh-only slipper when its caster holds one. Native coverage
  supplies a real held slipper and checks the resulting copy, contact and cleanup.
- Momentum and Overclock drive the electricity on the existing first-person
  hands. Live Wire's held preparation now recognizes its current action name.
- Wire crossing height is interpolated at the crossing plane, so a descending
  flyer is not hit merely because a later endpoint is low enough.
- The bolt's landing target is the free palm. The introduction remains 4.5 s.
- The inspect cards use equal widths, keeping OVERCLOCK on one line while showing
  the complete approved descriptions in both roles at 1600x680.
- Loadout text fits its existing budgets. Retired variants keep their saved IDs
  and historical budget metadata; closed sidegrades do not enable them.
- HUMAN's Zack voice table matches current HeroLines. Draft SFX remain silent and
  unregistered. The owner explicitly confirmed shared timer-only stun recovery.

## Retired tests

The old Quick Circuit, Bank Shot and Closed Circuit fixture files and metadata
remain deleted with their retired plumbing. This session also removed the six
Amped-Up objective-cooldown tests and their deleted-message helpers, plus the
last Magnet recall-trace test and its unreferenced runtime helper and metadata.

Old match-long Overclock, ability-only Zapped and ultimate Zap-ring assertions
were replaced with current timed-state, stun, immunity and no-ring behavior.
The old sprint-speed test now exercises Momentum's three steps, multiplicative
stack and reset. Supported saved-ID, retired-flag rejection and old-replay
compatibility checks remain meaningful coverage.

## Executed evidence

All receipts, frozen dirty-input hashes and XML are under
`Logs/zack-rework-20261025-session3/`.

- Core: 840/840, `core/zack-core-final.trx`.
- Actual Unity compile and five-clip binding: `bake-retry.log`, exit 0.
- Focused repaired EditMode: 43/43, `edit-zack-fixed.xml`.
- Focused gameplay, fields, timed recovery, replay retention and first film:
  19/19, `zack-final-play-film.xml`.
- Latest film and inspect checks passed in `final-review.xml`; that run was
  4/5 because the Sean-only movement fixture still attempted an unsettled cast.
  The other two movement cases passed there. After settling the actual body on
  the court, the remaining case passed 1/1 in `movement-final.xml`.
- The current source compiled in that final native run after the obsolete
  recall helper and test were removed.
- Six final takes are in `film-v4/zack-skills/`. Recording uses an end-of-frame
  callback, checks saved time against simulation and locks the review controls
  to keep movement across the fixed camera. The public camera omits the private
  return echo; the victim view composites the actual HUD and stun frame.
- MP4 exports are 1280x720 at 30 fps, with exact frame-count and full-decode
  checks. Durations: Momentum 4.8 s, Backtrack 5.4 s, Afterimage 3.8 s, Live Wire
  9 s, Overclock 10 s and victim Zapped 2.6 s. The ultimate includes its authored
  introduction followed by the fixed court view. SFX are intentionally silent.
- Native frame sequences and both inspect captures were visually inspected.
  Normal-speed human feel and sound selection remain owner judgments.

## Broader failures retained honestly

The initial broad EditMode selection was 149/165. The repaired Zack availability,
glyph and held-preparation checks pass in the focused run. Unchanged failures
remain around old Nemu/Rafi expectations, spectator replay/autopilot rules,
Cheska text/telegraph values, authored arm assets and generic cast fixtures.

The initial broad PlayMode selection was 68/89. Removed or rewritten Zack tests
explain part of that result; the current Zack cases were qualified separately.
Unresolved cases outside this kit remain in RecordedEffectsTests (Sean contact,
Dante stable-rig identity and effect 101) and SkillReceiptTests (largest unit
buffer, familiar lifetime, verb-refund pool, free recall and Voodoo sync). Their
failed XML is preserved. They were not disabled, weakened or called passing.

## Protocol, preservation and limits

Protocol 182 and replay schema 16 remain the candidate contracts. Source audits
were compared against HEAD: the same nine payload findings and six finite-value
findings remain. The authority audit's extra other-body entry is the local
`motor` alias for `ctx.Motor` in Backtrack; Teleport enforces ownership, as on the
existing predicted route. This source analysis is not actual peer evidence.

One Unity job ran at a time, with graphics, the named guarded profile and frozen
runtime inputs. The canonical dirty checkout was used, not an isolated worktree.
The wrapper preserved the five owner files and QualitySettings bytes and each
guard verified existing profile and shared input preference restoration.

Actual host/owner/observer peers, reconnect/rejoin in live transport, Relay/WAN,
platform builds, physical controls and representative performance remain
unverified. Balance and human readability still need owner play and review.
Publication needs owner instruction and resolution of main versus NETWORKFIX.

No browser tabs or preview servers were opened. Native jobs and encoding jobs
are complete. Automatic approval review blocked removal of the temporary HEAD
audit copy in the chat's work directory; it was retained without another deletion
route. Source and essential failure evidence were preserved.
