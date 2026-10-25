# Zack owner refinements, 2026-10-25

## Current lead-and-merge speed candidate, 2026-10-26

Based on main `903482cc1`. The new direction keeps one gold hologram ahead of
Zack toward the old spot, closing into him at the 0.48 s catch. It supersedes
the trailing-hologram candidate below. Gameplay authority, held slipper, facing,
statuses and cooldowns remain unchanged.

### Motion and polish

- Lead starts from a 0.5 share of the remaining route and closes as travel
  completes. Contour layers fill the gap back toward the real body. The travelling
  hologram uses lower opacity so the body silhouette and red hair remain more
  legible where the two overlap. Fill is .18 and edge opacity multiplier .6.
- The body stretches up to 30 percent on its local forward axis at peak speed.
  Fourteen short world-space dashes live for .09 s each. First-person dashes
  align with the view axis; reduced effects omit them.
- First-person lens feedback is added above the existing smoothed lens: a
  -3 degree reveal coefficient, +8 travel coefficient and -6 catch coefficient.
  Reduced effects omit the kick. A view switch or completed effect removes it.
- The .26 m catch coefficient creates a short overshoot and crouch settle.
  Activation, destination and catch sparks remain. The landing sound still fires
  at the 0.48 s catch; all sixteen shipped electrical WAVs are unchanged.
- Ring, tag and red floor brackets evaluate the same current-time body position,
  including the overshoot. An intermediate change read the previous rendered
  pose and introduced a one-frame lag; this was corrected before qualification.
- Lowering the lens kick alone still exposed the caster's red floor brackets.
  They now step aside only during his own first-person rewind kick. Other
  players, observer views and reduced-effects floor cues remain visible.

### Verification and review

Evidence root: `Logs/zack-backtrack-speed-20261026/`.

- Incoming `r2.xml`: 21/22. The old test counted dash objects as body copies
  and expected two children rather than sixteen. It was updated to distinguish
  two bodies from bounded short dashes, with explicit shape and cleanup checks.
- `r3.xml`: 1/1 film comparison after the lead/fill changes. The gold overlap and
  first-person floor artifact still needed work. This is a superseded study.
- `r4.xml`: 21/22. Dashes under a separate root escaped the new test traversal.
  They now belong to the rewind root, making their lifetime and cost explicit.
- `r5.xml`: 2/2 focused checks, no skips. Normal and reduced modes verify one
  leading hologram, closing gap, bounded short dashes, camera kick scope/recovery,
  own floor-cue suppression, catch marker alignment and complete retirement.
- Final `r6.xml`: **22/22 passed, no skips**. The same frozen runtime candidate
  passes Backtrack authority/held slipper, copied-body tint, normal/reduced
  lifecycle and lens checks, three history cases, thirteen spectator-view cases,
  shared tag-floor readability, actual-map marker graphics and film capture.

The `r5` native observer and first-person films were exported with actual Unity
DSP audio and reviewed at 1x, alongside contact sheets for frames 112 through
131. Both contain 162 frames, 1280x720 at 30 fps, 5.4 s, with AAC and successful
full-file decoding. Both traces have zero ring/tag/floor X/Z error at CSV
precision through the catch. The intentional catch excursion is about .152 m.
Observer dashes reach 1.295 m and first-person dashes 2.072 m in these takes.

The caster's floor cue is suppressed on the fourteen nonzero-kick pull frames;
its cast frame remains valid before the kick begins. Other players' floor cues
remain in the first-person picture. Maximum normal cost is two body copies,
fourteen dashes, twenty renderers and four skinned renderers. Reduced mode has
two bodies, zero dashes, six renderers and four skinned renderers. These counts
are not a player-performance benchmark.

Normal-speed review shows a lead silhouette closing into Zack with short speed
dashes and a catch flash, plus first-person reverse translation and a clean floor
view. The gold image still overlaps the body on the collinear first leg and
tints parts of the head through transparency; the red silhouette is discernible
and separates more clearly around the turn. Creative and listening acceptance
remain open. This work does not claim a player build or actual-peer/WAN proof.

The delivered Observer-Speed and First-Person-Speed MP4s are bit-identical copies
of the reviewed `r5` exports. The final `r6` integration run used identical
runtime/shader/test inputs and provides another pair of native recordings.
Frozen-source hashes, all sixteen sound hashes and exact preservation of the
five owner files plus QualitySettings are verified in the run receipts and
`r5-metrics.json` / `r6-metrics.json`. One guarded graphics job ran at a time.
The temporary review tab and server were closed after playback review.
Automatic cleanup review blocked removal of duplicate review MP4s; those copies
were retained without retry. Final videos, native evidence and profiles remain.

## Previous trailing single-hologram candidate, 2026-10-26

Based on main `c8af2d6ce`. The owner rejected the tether and the separate group
of four past selves, then requested one afterimage that suggests many images.
The latest direction permits small electrical sparks and stronger reverse
animation. This candidate implements that direction; creative acceptance remains
open. The earlier sections below document superseded candidates.

### Result and integration

- One travelling body and one gold hologram follow the actual recorded route.
  The hologram repeats it 55 ms later. Five contour passes on the same hologram
  use 26 ms route offsets with declining opacity. There is no group of separately
  animated historical actors, frozen departure body or persistent tether.
- A 0.06 s pose stutter leads into 0.42 s reverse travel. The catch occurs at
  0.48 s with a short settle, then all copies retire. Brief electrical sparks
  accent activation and arrival. The existing electrical sound family stays
  enabled and all sixteen shipped WAV hashes remain unchanged.
- The resource-backed hologram shader preserves texture/palette detail beneath
  gold contours and scan bands. Materials are cached by source, shadows are off
  and near-camera fading protects first-person clearance. Reduced effects use
  one shader pass and smaller bursts instead of the five-pass trail.
- Fixed the separate red taggability floor brackets: their ground projection
  now follows the drawn feet during the cosmetic rewind. The seat ring and name
  tag already used the presentation offset. Tag outcomes and accepted motor
  positions are unchanged.
- Gameplay teleport authority, held slipper, facing, statuses and cooldowns
  remain unchanged. The visual sequence adds no invulnerability.

The official [Ekko ability reference](https://www.leagueoflegends.com/en-us/champions/ekko/)
was viewed at normal speed. The useful motion cue is one bright subject collapsing
through a curved route with pose persistence and a distinct arrival, rather than
a parallel group of washed-out bodies. This candidate keeps Zack's gold identity.

### Fresh native verification and recordings

`Logs/zack-rewind-reel-20261025/final.xml`: **22/22 passed, no skips** in Unity
6000.5.8f1 with D3D11. The passing set covers Backtrack authority and held slipper,
camera-scoped body tint, normal/reduced copy lifetime and backward motion, shader
support, actual floor-cue alignment, three history cases, thirteen spectator-view
cases, shared tag-floor readability, actual-map marker graphics and both films.
All six assemblies also passed direct compilation; native import/compilation and
rendered films establish the stronger evidence for this candidate.

Earlier `initial.xml` and `refined.xml` each passed 7/7, but their multi-body art
directions were rejected. They are historical technical evidence, not the current
visual result. The older disabled-SaBubong setup failure remains documented below.

Both final takes are under `Logs/zack-rewind-reel-20261025/final/zack-skills/`:

- `02-backtrack`: fixed observer view, 8.31 m route, 15 pull frames, all 15 closer
  to the old location, all 15 travelling-body frames drawn.
- `02b-backtrack-first-person`: same route length, 15 pull frames, all 15 closer,
  zero own travelling-body frames drawn.
- Both traces have maximum route error 0.000 m and ring/tag X/Z error 0.000 m at
  CSV precision. The red floor-bracket integration is asserted separately and
  was visually checked in the observer recording.
- Maximum two body copies, six renderers, four skinned renderers and 32 transforms
  per take. Normal and reduced lifecycle checks retain the same copy bound.
  Normal mode adds four contour passes; these counts are not an FPS benchmark.
- Each export contains 162 frames, 1280x720 at 30 fps, 5.4 s, with actual Unity
  AudioRenderer capture muxed to AAC. Full decoding, frame count and duration
  passed. Native peaks are .224 observer and .261 first person, below clipping.
  Cast is video 3.767 s and catch cue 4.267 s on the 30 fps capture clock.

Both encoded final exports were reviewed at 1x through the rewind. The red
brackets stay under the moving body. The single gold hologram is clearest around
the corner and second leg; the first leg can partly occlude it behind the body.
First person shows a brief gold silhouette receding along the reverse route.
Those are observed presentation limits, not owner approval or equivalence to the
reference. Further creative refinement may change the timing, shader or poses.

One guarded graphics job ran at a time. `final-receipt.json` and
`final-metrics.json` verify frozen runtime inputs and exact preservation of the
five owner files plus QualitySettings from the start of this pass. Profiles and
recording evidence are retained. No player build, actual-peer/WAN check or new
human listening acceptance is claimed.

## Previous tether candidate verification

Technical candidate verified on main from base `f97da3be6`. The owner rejected
the electric-tether art direction after viewing the reference and supplied a new
direction: visible afterimages moving backward like Ekko, with electricity in
sound rather than a rope pulling the character. Creative motion/VFX acceptance
remains open. The earlier snapshot below is historical, including its former
sound-audition boundary; enabled audio is documented in lightning-audio.md.

The incoming rewrite uses one body, a 0.08 s snag, 0.40 s travel and 0.14 s catch,
arc-length travel over pose-history roots and a moving seat ring/tag. Accepted
teleport authority, statuses, facing, held slipper and cooldowns are unchanged.
The current tether implementation is retained as a documented creative candidate,
not an approved final look.

### Technical corrections during verification

- Corrected the recording's second leg from diagonal motion to a true L turn.
  A backtrack-only recording mode preserves the all-skill mode while avoiding
  unrelated recaptures. CSV traces now include ring and tag world positions.
- The root-only history accessor now honors the same teleport discontinuities
  as retained pose playback. Three cases verify interpolation and hard edges.
- The Afterimage held-shoe fixture waits for actual pose-history readiness after
  hierarchy changes, with a bounded wait and explicit readiness assertion. Its
  original held-slipper assertion remains intact.
- Normal-speed review found a color pop when the live body returned at 4.40 s.
  Taggable-state tint is camera-scoped, so the moving copy now uses the same
  readability scope at its drawn height. A regression case verifies application
  and restoration of its material block. Updated playback retains the same body
  coloration through the handoff to the live model.

### Native evidence

- `Logs/zack-backtrack-verify-20261025/play.xml`: 29/31 passed. The held-shoe
  fixture readiness failure was corrected. The extra legacy water-marker case
  still tries to load disabled `SaBubong`; it is unchanged at the source base
  and was not weakened, disabled or represented as a passing regression.
- `Logs/zack-backtrack-final-20261025/play.xml`: 6/6 passed, including the three
  history cases, corrected Afterimage case, Backtrack acceptance and both views.
- `Logs/zack-backtrack-scope-20261025/play.xml`: 17/17 passed, including the new
  copied-body readability case, Backtrack acceptance, 13 spectator-view checks,
  actual-map marker graphics and both final recordings. Unity compilation and
  import succeeded in 6000.5.8f1 with D3D11. No skips in either passing run.
- Unchanged kit/presentation cases retain the passing evidence from the first
  run. Pure Core rules were not changed; the prior 840/840 result is historical.

Final film root: `Logs/zack-backtrack-scope-20261025/zack-skills/`. Both takes have
162 frames at 30 fps, 1280x720, with actual Unity DSP audio. Exported MP4s contain
AAC and pass frame-count, duration and full-decode checks. Native audio is 5.3973 s
against the 5.4 s frame sequence; export padding covers the sub-frame difference.
All sixteen electrical source WAVs retain their shipped hashes.

Observer: route 8.34 m, 15 pull frames, 14 closer to the old spot, all 15 body
frames drawn with the live body hidden, maximum off-route 0.098 m. First person:
route 8.29 m, 15 pull frames, 14 closer, zero own-body frames and maximum off-route
0.107 m. Both ring/tag traces match the visual feet in X/Z to CSV precision.
Cast is video 3.767 s, catch 4.267 s and renderer restoration 4.400 s. Native mix
peaks are .224 and .261. These measurements establish playback and placement,
not creative quality or listening approval.

Both encoded exports were reviewed in a local player at 1x with sequential
captures through departure, corner, catch and restoration. The observer shows
one body being reeled along a tether, which is now explicitly rejected. The
first-person return translates backward, but a long gold line crosses the view
around 3.9-4.05 s. The next creative pass should rethink the temporal afterimage
read and first-person treatment independently, using both reference videos.

Owner visual/listening acceptance and actual-peer/WAN behavior remain open.
One guarded graphics job ran at a time. Receipts preserve exact dirty inputs and
restore the five owner files plus QualitySettings. No player build is claimed.

## Earlier refinement snapshot

Owner approved ongoing commits on main. Baseline kit: `e01da9d59`. Latest
spectator work was integrated in `cecd06182`. Bare current hands are committed
in `5105a70b4`; original SFX recipes and viewed-reference notes in `08f602c12`.

The new Backtrack draws a real-body copy accelerating backward through Zack's
recorded path for 0.42 s, with three collapsing afterframes and a matching eye
pull. The copy carries the held shoe. The accepted gameplay teleport, facing,
statuses, ownership and cooldowns remain unchanged. The normal renderers return
on the catch or interruption. This is presentation and adds no invulnerability.

Live Wire now uses two low floor cleats with insulated reels and live sockets.
The line is tensioned at 0.28 m, sagging only to 0.24 m. Its 0.40 m feet-contact
limit matches the low line with a small contact allowance; timed jumps clear it.
Protocol 183 deliberately rejects the old above-jump rule. It still has no
collider, lasts 10 s, snaps on the normal first hit and stays for the overcharged
per-player hits.

## Fresh evidence

`Logs/zack-refinement-20261025/` contains frozen source hashes and guarded input
restoration receipts. One graphics job ran at a time using the named profile.

- Core 840/840: `core/zack-refinement.trx`.
- All six assemblies compiled directly, followed by actual Unity compilation.
- EditMode 31/31: bare hands add no mesh props, powered arcs remain, aim
  preparation, loadout refresh, protocol checks and spectator codecs.
- PlayMode 19/19: the body begins at departure while gameplay is already at its
  accepted landing, the presentation advances toward the past point, the eye
  offset clears, held-shoe and authority checks remain, low-wire geometry and
  height contacts pass, spectator eye/perception cases pass and all six skills
  record through actual input.
- The recording explicitly demonstrates a timed jump clearing the low wire,
  followed by walking through it and being Zapped. Overcharged catches both
  later crossers and remains intact.
- Native sequences were inspected, including the backward body motion, low
  anchors, contact and clean first-person hands. Exports are 1280x720 at 30 fps
  with frame counts, simulation-time checks and full-file decode verification.

## Sound audition boundary

Six original stereo 48 kHz cues were synthesized locally without samples or
voices. Review MP4s mix pull/catch, string/current/zap/snap and victim Zap against
the recorded event times. Mixed peaks are below clipping. These are offline
audition mixes, not native sound capture and not enabled game assets. No drafts
were registered in Resources. Owner listening decides acceptance.

Actual live peers, WAN and human feel remain unverified for these refinements.
Earlier broader legacy failures in `verification-session3.md` remain separate;
they were not hidden or disabled. The five owner files and QualitySettings were
restored to their exact pre-run bytes. No push was requested.
