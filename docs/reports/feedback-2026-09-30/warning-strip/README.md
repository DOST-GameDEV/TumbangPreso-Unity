# Thin warning strip

Live Feedback requests a thinner, single-line warning with the same opacity as
the action message. The warning now uses a 48-unit base height instead of 90,
natural text width up to the existing 1100-unit safe bound, 16-unit side padding
and 4-unit vertical padding. Its red identity remains, while alpha equals the
contextual action plate through HudDraw.Plate. No warning rules or copy changed.

The minimum 28-unit type and existing crowded-status width bound remain. At
extremely constrained widths the text can wrap rather than disappear or shrink
below the font floor. Normal and enlarged tested layouts use one line.

## Validation

Native baseline fails on actual height 90 versus the requested thin-strip bound.
The final native Low-profile case passes 1/1: real refused throw text, one line,
font size at least 28, contained text/panel, matching action alpha and spectator
hiding. Four UI-only captures were inspected: 960x540 and 1600x680 at HUD scales
1.0 and 1.2. They show native UI on a neutral background, not a full gameplay
composition or performance test.

The first final launch stalled under severe memory pressure before producing
results and exited 247 after 362 seconds; the guard restored profiles. One
bounded candidate-only retry selected the existing Low graphics profile and
restored the prior profile afterward. It passed in 8.62 seconds; the outer
launch still recorded a memory-guard request, so clean resource headroom is not
claimed. The shipping test retains its ordinary profile. No player refresh,
all-status stress matrix, high-quality or human approval claim.
