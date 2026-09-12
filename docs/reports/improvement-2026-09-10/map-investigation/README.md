# Unresolved slipper-flight investigation

**2026-09-12 continuation:** the original files below remain unchanged historical
evidence. A corrected isolated fixture now distinguishes retained Carrier.Held
from the overhead-flight ground query. Both reproduce separately and the focused
correction passes4/4 including raised-floor and roof-recovery contracts. See
[current investigation](../../improvement-2026-09-12/map-retrieval.md).
This does not establish the cause of the historical48-idle-penalty outlier.

These text files preserve an exploratory fixture and an unsuccessful runtime
patch. Neither is compiled or applied. The patch did not resolve the fixture's
observed position reset and was reverted before the map checkpoint.

Observed in the actual Ilalim scene: GroundY(2,.5,-2)=0 and
GroundY(2,3.6,-2)=9.040001. The fixture released a slipper at (2,3.6,-2), then
observed it still InFlight near (-2.58,.55,8.64) after0.1s. That suggests another
position/throw writer or unresolved fixture interference may be involved. The
ground-query observation is real; attributing this flight reset to it is not yet
proven. The broader historical48-idle-penalty case is also not explained.

Continue by isolating all intent/AI drivers and tracing state/position/throw
writers per physics frame. Preserve real raised-ground and unreachable-roof
recovery contracts. The files are included so a new checkout can reproduce the
investigation without relying on this machine's ignored Logs directory.
