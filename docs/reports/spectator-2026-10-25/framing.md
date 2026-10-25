# Body, map and placed-effect framing

The spectator now measures the current rendered body instead of using one chest
height for the whole cast. Cached renderer lists refresh when a seat changes body
or its model is replaced; moving, crouching and scaled meshes supply live bounds.
The actual camera FOV and aspect ratio determine the distance needed to fit them.

Third-person follow clips its sightline against walls and low roofs, frames the
selected body and cycles by seat order after a direct POV selection. Automatic
views retain the existing deliberate hold and manual takeover. Quiet views frame
the live players and can together from a three-quarter bearing along the court's
long axis. A failed angle gets another validated view, including below a roof;
there is no unconditional jump to an assumed clear sky. Sightline queries reuse
buffers instead of allocating a raycast array per query.

Presented ability bounds are captured at the existing cast confirmation before
its event fires, scoped to the ability, match and round. Rago and Paete reuse their
actual placement solvers; court effects use the playable court. Later caster
movement or aim cannot move the recorded placement. The director includes these
bounds, secondary bodies and objectives when fitting the lens.

Validation uses native Unity graphics with named profiles:

- `framing-covered-edit.xml`: 26/26, including real lens projection, tall/wide
  bounds, narrow aspects, translated elevation, low roofs, scenery and transit.
- `framing-play.xml`: 33/33 focused actual-view, body replacement, placed ultimate,
  keyboard/controller and perspective checks.
- `map-body-play.xml`: 26/26. All 21 authored roster bodies fit the lens at their
  own sizes. All six current maps load through their full automatic opening and
  enter first-person, third-person and automatic spectator views.
- Final composition rerun checks every player's body corners in the quiet frame
  on all six maps, plus the pacing regression. Its fresh result and capture hashes
  are in [the receipt](framing-validation.json).

The map fixture stops actors after normal arrival to isolate camera behavior.
Final framing comparisons disable cinematic camera motion only in the test and
restore the setting; the earlier full-opening pass is retained. Native images
were inspected, including Arena, Eskinita, Ilalim ng Tulay, Kanto and Banaue.
These are local graphics fixtures, not natural multiplayer acceptance.

Remaining spectator work includes private hallucination/reticle and world-filter
parity, regular skill/flight coverage and further live event transitions. This
unit does not complete the full spectator or network assignment.
