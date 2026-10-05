# Accurate browser feedback and editable text coordinates

Actual c526 Windows controls caught two UI defects: the initial online query frame
reported no public rooms before a reply and sharper glyph sampling displaced the
code-field caret. The original typed-code screenshot remains in the adjacent
[current player report](../current-ui-online151/README.md). Typed-code admission
still succeeded. The generic text change needed a stricter editable-field boundary.

## Corrections

ServerQuery distinguishes finding rooms, a confirmed empty result and unavailable
services. Only the current browse generation can update the status after awaits.
Existing lookup spacing, query options, listings and network ownership remain.
The controller passes the message through IHubHost and the view redraws changed
empty-state text even when the list still has zero rooms. It keeps confirmed-empty
copy during routine polling to avoid a repeated loading/empty flicker.

CrispUiText retains native rendering for any text inside an InputField. Its shared
cached generator must agree with InputField caret/selection coordinates. Other
static labels retain sharper sampling. HubField also turns off ink-bound alignment
for editable text: the caret uses line metrics and the prior display-font ink
alignment made it extend above the field even after native sampling was restored.
No font, declared size, input navigation or supplied artwork is replaced.

## Focused native evidence

The first combined EditMode run has15 passes: seven glyph/input checks and eight
browser state/lifetime checks. Its attempted actual-view check fails because
runtime Destroy is forbidden in EditMode; that check was moved to PlayMode.
The raw16-case failure receipt is retained; no16-pass claim is made.

The first real PlayMode check validates browser message refresh but then catches
caret containment failure at localy10 above the actual code field. This is the
product boundary that justified disabling ink-bound alignment in HubField.
Final native22592 passes the same actualHome->JOIN view check. It verifies loading
and unavailable text with an unchanged empty list, focuses a real typedUMKB field,
forces a visible caret mesh and checks every caret vertex inside the actual field
rectangle with two units of rounding allowance. Native screenshots inspected.
The final screenshot can land during the caret blink; containment is established
by the real renderer mesh assertion, not an inferred visible line in that frame.

All parents terminal; final22592 exits0 with19310 frozen inputs unchanged and
profiles/shared preferences/input/Quality restored.202 inherited generated GUI
metadata files are verified and restored to original source bytes after completion.
Original PNGs unchanged; Auditor/private draft preserved. No owned player/editor
or temporary browser remains. These source corrections postdate c526 package.
The current separate-machine QA host shutdown remains OPEN.
