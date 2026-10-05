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
checks the focused input renderer mesh inside the actual field
rectangle with two units of rounding allowance. Native screenshots inspected.
The final screenshot can land during the caret blink; containment is established
by the real renderer mesh assertion, not an inferred visible line in that frame.
Activation selects all, so this earlier mesh covers selection; the explicit thin
caret and drawing-order check below supersedes that narrower interpretation.

All parents terminal; final22592 exits0 with19310 frozen inputs unchanged and
profiles/shared preferences/input/Quality restored.202 inherited generated GUI
metadata files are verified and restored to original source bytes after completion.
Original PNGs unchanged; Auditor/private draft preserved. No owned player/editor
or temporary browser remains. These source corrections postdate c526 package.
The current separate-machine QA host shutdown remains OPEN.

## Actual player layering correction

Exact893 Windows build10352 exits0; runtimeSHA
14c7f6bc3b4febcc5f6150a6917158577e30ef0b3ddc662f71e7aa788f676517,
258files/2691128379bytes.203 generated metadata/EOL deltas retained and restored
exactly to frozen source. One normal player12792 typesUMKB in the corrected field
but its caret and selection are invisible. InputField puts its renderer first
under the text's parent; the opaque child Plate then covered the renderer.
The893 package does not include the new layer correction.

HubField now creates a full-size TextArea after Plate and places editable text
and placeholder there. The first-sibling caret/selection renderer is consequently
after the opaque background, with the same field/hit rectangle. Searches found
no runtime dependencies on the old direct Text/Placeholder child paths.

Native8096 passes layer-order and focused selection geometry, with a visible
selection frame. Native23360 explicitly collapses selection after activation,
checks equal anchor/focus indices, measures a three-unit-wide actual caret mesh,
checks every vertex inside the field and verifies its parent layer after Plate.
The inspected final1920x1080 image visibly shows the thin caret afterUMKB inside
the field. Earlier static or selection geometry was insufficient proof of caret
visibility. Final parent exits0;19310 inputs unchanged and shared input/profile/
Quality/editor preferences restored.202 inherited generated metadata restored.
No owned player/editor/proxy/recorder/browser remains.

The893 helper used a31-character SDK profile and authentication rejected it with
10006: profile names must be at most30 characters and use alphanumerics, hyphen
or underscore. Its unavailable-services frame is a harness error, not the QA
Relay timeout and not live-network acceptance. Player12792 exits0; parent13011
restores settings/shared inputs. Use shorter unique profiles on future launches.
The real c526 public/code admission remains the current independent online proof.
