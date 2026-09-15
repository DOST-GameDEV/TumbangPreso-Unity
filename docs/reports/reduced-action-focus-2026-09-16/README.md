# Stationary focus with reduced interface motion

Painted/text actions previously used scale as their sole selected/pressed cue.
With reduced motion enabled, the scale is correctly fixed at1, but keyboard and
controller navigation has no visible feedback. The pre-fix render showed zero
changed control pixels on selection;43 distant sky sample changes were unrelated.

OwnerUiMotion now shows a small stationary side arrow using the existing StreetIcon
grammar. It sits outside the supplied button, has no pointer hit area and disappears
when focus is lost, the action is disabled or normal motion is restored. Pressed
feedback changes the marker ink immediately. Original artwork, label colors, hit
targets and normal scale motion remain intact.

An initial underline passed the code check but was rejected on visual inspection:
it sat in the narrow gap above Tutorial and looked like a stray border. It was
replaced with the side marker. The final marker's focused1920x1080 render changed
199pixels, zero inside supplied Play artwork and zero outside the button's nearby
control region. The rendered state and cleared state were checked directly.

Focused v3 passed1/1, guard138497c541f3. Expanded small-window, preference-toggle
and text-action checks PASS1/1 in v4, guard777f9e0d2f3e. The side marker was inspected
at1920x1080 and960x540. Normal focus still scales the art; turning reduced motion
back on restores exact scale1 and the stationary marker. Disabled/deselected cues
disappear, and the text action emits visible non-interactive marker geometry.
Nativev24 predates this small UI change;
its full demo-loop evidence remains separately preserved, not mislabelled as
testing this marker. No repeated complete UI matrix or Desktop update is required.
