# Stoke Step authored motion refinement

The named Sean dash clip now braces through its real 0.18-second anticipation,
uses opposing limbs during the short burst, catches on the leading foot and
settles by 0.8 seconds. It replaces the legacy symmetrical wing-like Flame Rush
pose. Mechanics, first-person clip, effects and sound are unchanged.

Authoring reports a 34-degree torso peak, 54 samples and zero floor penetration
at sampled grounded beats. Original binary prefix, mesh/material/rig tables and
all 35 other animations are unchanged. Sean is the only changed GLB.

## Native evidence

The existing actual Skill1 input probe passed 1/1 with the new imported clip.
The CSV identifies hero-sean-dash and thrust-fire and confirms acceptance.
33 body and 33 owner frames were produced. Inspected body samples show the
smaller brace, opposing leg positions and upright settlement; sampled torso peak
is 30.34 degrees versus the baseline 64.8. The native imported motion differs
as intended, rather than falling back to an unchanged kick.

The final launch exited 0 in 50 seconds with no memory guard request. This is
fixed-simulation offline capture, not real-time performance or target-player
qualification. Actual frame times remain in CSV. The comparison video uses the
0.1-second simulation spacing; it is silent and labelled accordingly. The first
real-time capture failed before useful coverage, and the fixed baseline received
a memory-guard request despite completing its test. Those limits are retained.

The standalone Blender verifier could not resolve the imported action by exact
name in Blender 4.3.2; its failure is retained, not called a pass. Native Unity
imported-pose evidence and direct GLB preservation checks provide the scoped
validation here. Human taste, full-speed device play and a refreshed player build
remain unqualified. No broader hero or Feedback umbrella is marked complete.

Candidate-only capture-clock patch is included for reproducibility. It was not
applied to the shipping capture helper. Profiles restored and generated
whitespace-only metadata restored after recording the diffs.
