# Opening camera refinement

Owner request: critique the opening sequence and substantially refine its camera.
This supersedes the current presentation unit until the opening is refined.

## Current critique from source

- The map shot centres the mean player spawn, rather than the objective. It also
  clamps every authored map to16m height/30m distance, flattening map differences.
- Every portrait uses4.6m and the same14-degree orbit, regardless of model size,
  framing or aspect ratio. Obstruction handling can push that fixed shot as close
  as0.8m instead of choosing a clearer direction.
- Portraits cut without transition treatment. All four poses start during the
  establishing shot, so the character reveals do not own their gestures.
- After the fourth portrait, the camera jumps to another wide view, waits, then
  returns to gameplay. That redundant detour weakens the finish.
- The1060x200 caption competes with the character's lower silhouette.

These are source findings, not a claim that every map was filmed. A direct
Eskinita capture bypasses the heavy HOME sequence to seek native baseline pixels.

## Direction

Keep a short objective-centred establishing move, four readable character
introductions, then one deliberate return to the player's view. Preserve the
three-second countdown and the internal loaded-peer barrier.

- Establish for2.8s using the actual can, authored map heading and a restrained
  eased pan/dolly. Preserve map-specific distances and under-roof constraints.
- Give each character1.1s. Fit the real drawn bounds with head/feet margin rather
  than a fixed distance. Prefer front-facing clear angles; test alternate yaw
  and elevation before accepting a cramped obstruction-limited view.
- Use short fade-through-ink cuts between portraits, with no stored previous
  frame, duplicated actor or rapid cross-court fly-through. The individual pose
  grows after the cut and has time to settle. Reduce repetitive orbit motion.
- Remove the second wide shot. Ease from the last portrait into the saved player
  view over1.4s; restore owner visibility/arms safely near the end.
- Make the caption smaller and keep it away from the full-body framing.
- Reduced motion retains a stable overview and avoids moving cuts. Cancellation,
  camera settings, held world time and fresh input must restore correctly.

Target camera duration8.6s. The owner's later October3 correction shortens the
countdown to3 /2 /1 /GO!, matching the announcement. No custom map-vote policy,
network payload or character model changes. Protocol143 prevents mixed countdown
timings from releasing gameplay holds at different moments.

## Acceptance

Native framing where the environment permits; deterministic phase/framing and
occlusion checks; interruption and reduced-motion checks; original readiness
controls. Keep rendering and actual-peer/player acceptance distinct. Preserve
resource guards and failed evidence. No unchanged full-HOME graphics retries.
