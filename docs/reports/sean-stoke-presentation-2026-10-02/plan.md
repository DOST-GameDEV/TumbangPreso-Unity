# Sean Stoke Step presentation

Owner authorized full-roster research-led presentation refinement. References and
production principles are in ../hero-reference-footage-2026-10-02/sean.md.

## Baseline and limits

Candidate 9590f4bd shares the relevant Sean kit, authored clip and capture code
with publication b556115a. Existing actual input probe accepted Stoke Step. The
first real-time attempt captured one frame after 9.4 seconds of rendering and
failed its coverage assertion before the cast. A single candidate-only repair
used fixed simulation time, preserving real/game timestamps. It passed 1/1 with
32 body and 32 owner frames. This is offline motion review, not performance or
real-time play qualification. The memory guard requested stop during the run;
Unity completed the case and restored profiles before exiting. No clean resource
claim. The recorded motor speed excludes the independent impulse; zero in that
column is not proof that the skill did not travel.

Inspected body and owner samples show the retained Flame Rush pose: torso peaks
at 64.8 degrees, both arms sweep back, then a long unfolding recovery. The
shorter adopted two-metre Stoke Step deserves a compact brace and a distinct
leading-foot catch. Current owner view keeps the lane open; preserve that.

## One coherent authored unit

Own tools/author_hero_action.py Sean dash entry only and
Assets/TumbangPreso/Art/characters/persons/team-sean.glb named hero-sean-dash
animation only. No geometry, materials, rig, other clips, mechanics, SFX,
first-person action or protocol changes.

Beats: quiet stance at 0; brace at .08; loaded opposite foot at .16; launch at
.18 matching authority; compact extension .27; leading-foot catch .43; absorb
.55; settle .70; neutral .80. Use asymmetric opposing limbs and a torso peak
below 40 degrees. The rig has no knees: solve floor contact, never lower the
hips through the ground. Keep the slipper in its existing hand.

Validate all non-target GLB data unchanged, existing rig/floor checks, native
side/quarter strips and real accepted-cast output with the same explicit fixed
simulation limitation. Do not turn a green assertion into human taste approval.
