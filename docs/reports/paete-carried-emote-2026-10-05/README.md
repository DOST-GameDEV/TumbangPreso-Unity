# Paete carried slipper during emotes

The actual T-pose/dance/bow recording confirmed the floating slipper on Paete.
The accepted carried object stayed owned throughout: this was placement, not
loss of possession. The original support anchor sits0.082813m above the sampled
weighted branch-palm surface. It inherited the0.0617 bone-local human-hand lift.

The scoped correction recognizes the actual Paete source-model asset, measures
the top of its existing distal weighted hand region and gives that irregular
surface1cm WORLD-space clearance. Every other source model keeps its authored
hand lift. Existing PalmCentre callers retain the original centre semantics.
No model, mesh, material, emote clip, ability or networking change is included.

The first surface-only candidate was7.28mm below the wider sampled nearby
branch envelope. That failed assertion is retained. The clearance candidate
measures2.72mm above it and passes the original independent3cm-bound assertion.

Two older CarryTests failed both candidate and original89cc9f29 production
source: their HostGrab setup is refused and their observed idle anchor does not
move. Headless and graphical candidate attempts are retained, followed by the
original-source2/2 failure control. These failures are not fixed or counted as
passes here. The final qualification uses the active match fixture, explicit
model swaps and real emote/carry recording rather than weakening those tests.

Final native3/3 pass (135.6s process): branch surface, Paete/Bayan model swaps
without losing the held object, and real T-pose/dance/bow recording. Actual
owner-view frames were inspected. The support point now follows the thinner
palm rather than floating over it. This is Linux native/film evidence; current
Windows player, separate-peer visual and human acceptance remain open.
