# Zack owner refinements, 2026-10-25

## Latest Backtrack rewrite verification

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
