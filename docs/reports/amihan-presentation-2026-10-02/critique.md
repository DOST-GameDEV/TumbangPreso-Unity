# Critique of the shipped Airburst presentation

Source read at `933c486d9`. Mechanics are authoritative and unchanged:
`AmihanRules.StormSurgeGatherSeconds` 1.5 s and `StormSurgeHalfAngle` 30 degrees
(a 60 degree fan) since `8680d9adf`; the host applies Whirled plus a 15 m/s,
7 m/s-lift carry to every body inside the fan, and launches loose slippers, at
one instant, the end of the windup. The caster is rooted (speed 0) and the
windup cannot be interrupted.

## The order of events a player actually sees

1. Press. The host accepts; the shared phase pauses the world and every player
   watches Amihan's 3.6 s introduction (`Resources/UltimateIntros/amihan.txt`,
   `HeroIntroductionScene.Amihan.cs`). The length is shared network timing.
2. Handback. `AmihanStorm` spawns its live fan and the live body clip
   `hero-amihan-storm` and first-person `storm-call` start, on the same frame.
3. 1.5 s later the wind leaves and the host launches everyone inside the fan.

## Defects

1. **The body releases one second late.** The shipped baked clip
   (`Art/characters/amihan-motion/hero-amihan-storm.anim`, 3.01 s, shove key at
   2.5 s held to 2.66 s) was authored for the old 2.5 s delay. At the real
   release (1.5 s) she is still mid-"holding back"; she shoves a full second
   after her victims are already flying. Verified in the asset's key times, not
   only in the generator.
2. **The caster's own hands release one second late.** `ViewmodelArms`
   `storm-call` (contact 2.5 s) and `StormCallClip` (shove at 2.5 s) carry the
   same stale timing on her screen.
3. **The cutscene shows the release before it happens.** Its last beat (3.1 s)
   sends a wall of wind down the court. Play then gathers for 1.5 s and
   releases again: the strike is shown twice, out of order, and opponents can
   read the cutscene as the hit having already happened.
4. **The handback pose says "released" during the dodge window.** The cutscene
   ends, and the live clip starts, with both palms already driven forward. The
   body's strongest shape is spent before the threat is real.
5. **The gather looks like blowing, not holding.** Fan streaks rush outward and
   accelerate through the windup, so the release is not a distinct event and
   there is no readable countdown.
6. **A painted wedge.** Seven filled lane meshes cover the whole 60 degree fan
   out to the court diagonal at up to 0.75 alpha. That is area, not edges
   (VISION section 2 rule 3).
7. **The release front arrives after the hit.** The wall eases out to ~14 m at
   ~0.22 s while bodies are launched at 0 s, so a far body flies before the
   front visually reaches it.
8. **The release wall overstates the area.** Its arcs span about 33 degrees each
   side against the 30 degree contact rule.
9. **The cutscene lifts her on a ribbon vortex.** It is the Venti cage the
   reference review rejects, and contradicts the grounded, braced final pose.

## Weaknesses that are not outright defects

- Four shots carry four ideas (WHO, INTENT, GATHER, RELEASE). Paete's v3
  showed that structure reads as noise.
- The palm "storm eye" (dark core in bright rings) and the lens streaks are
  generic; neither is hers.
- Opponents never get a readable picture of the lane direction from the
  cutscene; they find out after handback.
- Circular pressure rings at her feet have no direction.
- Several comments still describe 2.5 s.

## What is good and stays

- The Calle Crisologo stage, her palette (yellow-green, cream, brooch gold),
  cotton and abel-thread motifs, and the kasikus diamonds are hers.
- The fan edges as two bright lines from her feet are honest gameplay geometry.
- `WindVfx` ribbons are the right building block: edges, thinning, no `_Time`.
- The release sigil at her feet and the outward cotton are small and readable.

## The unshipped 2026-09-26 cutscene plan

[amihan-kit-2026-09-26/direction.md](../amihan-kit-2026-09-26/direction.md)
planned 6.2 s with BREATH, WHIRL and THE TAKE. Its method (one sentence, one
travelling thing, left-to-right continuity) is sound and is kept. It is not
adopted because:

- 6.2 s changes the shared phase length, which every peer derives: a network
  timing change outside this presentation-only assignment.
- THE TAKE shows real targets gripped and fleeing before their live dodge
  window: a premature hit cue for players who may still escape.
- The rise-and-hang (Xianyun) lifts a grounded character for spectacle.
- Its numbers predate the 1.5 s delay.
