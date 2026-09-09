# Measured gait cadence correction

The current geometry/clip regression compares both stance endpoints of the actual
imported walk and sprint on all twenty retained rigs. The previous speed calibration
was much longer than their authored travel: Berto's walk was 2.016 m against 0.769 m
per cycle, and his sprint 2.592 m against 1.152 m.

The corrected calibration derives from each rig's bind matrices, mesh floor and
world scale. The gait author reads the same 38/44-degree swing constants. The new
walk gives Berto 1.009 m of actual travel against 1.033 m calibration; sprint gives
1.152 m against 1.166 m. The tolerance covers sole thickness and root lean, not
several-fold sliding. Sean's longer legs receive their own cadence. No motor speed
or contact rule changed.

The regression was seen red, then all 467 EditMode tests passed. The current real
motion/retrieval run passed 18/18. Before/after rows are retained beside this report.
Footfall accents follow cycle distance, with cosmetic pitch randomness isolated
from the AI random stream.

The first model refinement remains unaccepted: the owner requests noticeable,
individually authored faces and silhouettes. Full skill functionality/SFX/VFX work
and exact Windows player verification remain open.
