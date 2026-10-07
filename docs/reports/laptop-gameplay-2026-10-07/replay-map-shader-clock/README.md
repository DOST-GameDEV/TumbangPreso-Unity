# Replay clock across map loads

A real PC integration failure found pose Time.time11.336242 versus GPU built-in
shader time2.498576 after scene changes. Earlier isolated same-scene clock tests
were insufficient. The new optional scene-frame fields save Time.timeSinceLevelLoad
separately from pose/application time. RecordedWorldView samples that saved clock
for authored shader animation; custom sky/pose clocks retain their own domains.
Old files missing the original shader clock still read, but cannot gain an exact
original epoch retroactively. No normal shader appearance or clock is changed.

Clock-only native20 candidate is exact c533 plus four focused source/test paths.
It excludes unqualified Arena effects, recorder/Archive and floor fixes. Native
PlayMode graphics test passes after two actual map loads and nonzero level time,
with an independent GPU clock read and original-time water pixel equality/pause/
seek/live restoration. Final receipt confirms21361 frozen inputs,13 original
Editor preferences and four existing profile files restored. Tests/measurements
are retained; checked-source.json proves exact candidate bytes and Git text.

This corrects clock identity; it is not every-material, full-game, cold-first-frame,
packaged or full visual-fidelity acceptance. Actual transient effects, drones,
wildlife and further package/device/peer checks remain open. PC original multi-map
failure stays in its integration evidence; nothing relabels that failed gate.
