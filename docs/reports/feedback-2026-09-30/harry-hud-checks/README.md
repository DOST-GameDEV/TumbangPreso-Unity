# Harry HUD layout and warning pass

The supplied mockup separates three bottom-left status cards, a central action
panel with progress inside it, a higher persistent warning panel, top match bar,
right event feed and bottom-right powers. The existing native art/font family is
retained. The bar has 10% base scale and longer cards; the round backing now fits
its actual pip count. The bare “Leave the can ring” text is replaced by the warning region, matching
Harry’s specific removal screenshot. The power-info hint is also hidden in the
new layout. Rooted uses its
real Interact binding with Remove/Removing wording and retained hold progress.

Penalty warnings persist until their clock clears and change when penalized.
Input refusals cover illegal throw/tag/run and actual failed cast requirements.
The latter exposed a missing feedback path: a valid actor's blocked cast expired
from the existing buffer without recording an answer. Expiry now records feedback
without changing cast timing or rules; a corrected successful cast clears it.

## Checks and limits

The first native run passes three retained cases: actual keyboard/pad/touch Rooted
hold including completion, reset reach/toggle behavior, and large-score bounds.
Two new fixtures initially failed because one called a client-only receiver on a
host and another left staged input parked. One bounded fixture repair corrected
those callers. The two corrected layout/input cases pass. A separate native case
passes actual empty-hand Skim press/expiry, no cast/cost, three-entry event-feed
layout and successful corrected cast clearing the warning. Six distinct passing
cases across three runs, not a single six-case run. No new OOM or input drift.

Small and short-wide native frames at normal/120% HUD scale were inspected.
Status/action/warning regions do not overlap; progress is inside its background;
feed plates remain separate. Rooted's large third-person body is existing game
behavior, not a new camera change. The staged flair capture includes pink
particles whose origin was not diagnosed here; it is not a whole-scene VFX pass.

New announcement bonuses and 2.5-second duration are still a separate pending
rule unit. Do not mark the combined HUD row Done yet. No new player, actual-peer,
physical-device or human approval claim. Protocol 119 is unchanged by this UI work.
