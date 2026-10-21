# Batch C ink effects, 2026-09-23

The down-can marker was a radius .95 emissive red collar parented to the rotating
can. It became an upright crescent, unrelated to a grab animation. HeroObjects now
suppresses that legacy collar; its off value restores it. Actual can art, tilt,
shared clocks, ground footprint and glyph remain.

CanContactAccent keeps its open centre, cream knock versus rising gold restore,
and .38s life. It now shares one ink-edge material per effect and erodes its edges.
Tin particles use small two-tone chips. Fire slippers keep their broad warm ribbon;
electric slippers keep two thin unequal rails. Both use InkFlight rather than
Sprites/Default, with their original timing/colour/authority.

PaperConfetti replaces the dormant HeroHazards shower helper's cubes/rigidbodies
with six-vertex folded sheets and deterministic flutter/terminal fall. No global
Random draws, colliders or rigidbodies. Spawn banks clear the main camera centre;
shader near/size/central-view limits apply independently to every viewing camera.
Reduced effects caps paper at8, normal at48 (default24). No confetti was added to
ordinary can contacts. InkEffects0 disables new dust/paper and restores plain strokes.

CourtContactDust uses existing can_knockdown, land and slide_scrape world cues,
not new predicted success events. Actual support is sampled, no air/water-floor
puffs when support is beyond reach. Life .46s; quiet small lobes shrink/fade. Replay
samples saved sound timestamps with the same seed, hides the live dust and restores
visibility after rendering. A slide scrape is the existing owner-predicted movement
cue, not proof of a successful pickup. Chalk-line crossing dust is still pending.

## Evidence and limits

Focused PlayMode case v2 passed1/1,16.214s. Checks actual knockdown, legacy collar
off/on, ground sampling, no dust from unknown cues/air, RNG preservation, six-vertex
paper/no physics, central spawn, reduced count, shader support, off and cleanup.
Guard5aab87facefa restored4existing profile files and1Editor preference. Known
four-path generation churn restored; original two UI metas preserved/excluded.

v1 failed because Sample(0) applied a nonzero flutter phase after safe placement.
Production fix subtracts the initial phase offset. Failure XML is retained.
Capture tooling repairs0/1; no fixture repair or assertion weakening was needed.

Personally inspected all three960x540 frames and their25percent greyscale copies.
Keep first look: remove dominant red crescent; small dust and strokes leave the
can clear; paper is peripheral and reduced in comfort mode. These are staged
same-camera pictures, not normal-speed native motion or full multiplayer proof.
No new native build/full regression. Integrated motion/replay/performance stays P7.
