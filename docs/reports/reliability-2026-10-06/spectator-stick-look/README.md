# Spectator right-stick look and manual takeover

The existing Look action binds the pad right stick, but the spectator read only
relative mouse axes. Bind that action and use the same radial0.16 deadzone and
squared response as gameplay, at150 raw units/second. Spectator movement/look use
unscaled time so an operator can control a paused match. A deliberate stick
request releases automatic directing; centered input does not.

Mouse look still requires cursor lock. Pad look uses the action independently of
cursor lock, with the existing spectator Update overlay guard preventing menu
input from moving the view. No gameplay reader, director solve, hero presentation
or input binding changes. Releasing the stick preserves the chosen camera view.

## Native evidence

Original8596/session50263: right-stick takeover and rotation causeFAIL; centered
controlPASS. The first rotation case had a pause name but did not actually set
Time.timeScale, so it proves only the initial missing consumer. The final fixture
explicitly pauses/restores scale; its degree assertion is unchanged.

Intermediate24360/session28594: takeover/centerPASS, actual paused rotationFAIL.
The cursor-lock early return still excluded controller look. Final14756/session
19787 passes7 cases: both causes/center plus prior pad target-cycle/free/POV and
keyboard-presence controls. A subsequent release/view-retention control11316/
session41640 passes1 on unchanged production. All presses use actual virtual-pad
InputActions and the real spectator consumer.

One preparation cell initially failed on Windows default text decoding before
run-final.py or a final Unity process existed. UTF8 preparation repaired it;
there was no duplicate restart or hidden native result.

Allfour native jobs terminal; each21,146 frozen inputs/shared input/Editor
preferences/Quality/isolated seed restored,216 known metadata deltas retained
and restored exactly. Physical devices, mouse calibration, touch and human-feel
acceptance remain separate. The ongoing full spectator map/character cinematic
pass is not closed by these operator checks.
