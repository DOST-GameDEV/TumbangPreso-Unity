# Spectator action controls work without a keyboard

Target cycling, free flight and POV have existing gamepad bindings. StepKeys
returned before reading those actions whenever Keyboard.current was null. A pad
operator without a keyboard could not use those controls. Remove only that
obsolete device-presence gate; bindings, actions, follow geometry and solver are
unchanged.

Original native12812/session51130 reproduces all three pad-only cause failures.
Its keyboard-present control also failed because this native session had no
keyboard device. The fixture now adds a virtual keyboard only when none exists
and restores/removes only its own state. Corrected original control10804/session
84802 passes. The three original cause assertions are unchanged.

Candidate native21912/session89691 passes all4 cases through the actual bound
InputActions and SpectatorCamera.StepKeys. Each targeted action press is asserted
before checking the resulting target/free/POV state. No physical keyboard device
was removed: only its current pointer is scoped/restored for the absent-keyboard
cases, and a fixture-owned virtual keyboard supplies the presence control.

Allthree jobs terminal; each21,144 frozen inputs/shared input/Editor preferences/
Quality/isolated seed restored,216 known native metadata deltas retained/restored
exactly. No physical controller, real operator feel, touch or map/hero cinematic
acceptance is claimed. Right-stick look/manual director takeover remains the next
operator-control unit. Laptop owns director/scenery/transit work separately.
