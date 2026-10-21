# Larger player highlight circle

The ordinary hollow player circle grows from 1.375 to 1.75 capsule radii,
about 27 percent larger. At a 0.4-metre capsule this is 0.55 to 0.70 metres.
Small characters still scale proportionally. The already larger taya octagon
(1.95) and scoped catchable brackets (2.1) are unchanged, as are floor/water
placement, shader, colours, pulse, visibility and all gameplay contact sizes.

## Native verification

The existing hollow-marker case now asserts actual capsule-relative X/Z scale
for each role in addition to flat geometry, no collider, visible rim, open
centre and shader role. Unity 6000.5.8f1 OpenGL Low-profile retry passes 1/1
in 3.31 seconds; outer exit 0, 40 seconds, no guard request. Both 256px isolated
top-down captures were inspected. The shipping test keeps its ordinary profile;
the isolated retry selected Low and restored the previous quality afterward.

The first ordinary-profile run timed out after 180 seconds during severe cloud
memory pressure. Its XML, failure and memory-guard receipt are retained. A
scoped Unity stop was requested during recovery, but the available exit receipt
reports test failure rather than establishing that the stop caused termination.
Only one bounded Low-profile retry was made. This is not a full-world/player,
all-quality, performance, human-taste or new network qualification.

Candidate base9590f4bd retained earlier explicit overlays; only the two owned
source files were added for this unit, with the stated candidate-only Low setup.
