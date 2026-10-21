# Earthbound incoming distance

Current Wiki halves Dante's shove/knockback distance. Baseline actual motor checks
showed the same impulse/carry travel as a neutral kit; Classic control passed.

A default-one kit property now supplies Dante's0.5distance multiplier only in
Hero Strike. Incoming horizontal impulse uses sqrt(0.5), after the existing cap.
Held carry scales both speed and held time by sqrt(0.5), preserving the same
friction-tail relationship. Vertical lift stays unchanged. Existing host-to-owner
impact/carry delivery applies it at the simulating motor once; ordinary walking,
Classic and other kits remain unchanged. Protocol106 separates old clients.

Final4/4native checks pass4.44seconds. Impulse travel is1.116m versus2.28m;
held carry2.693m versus5.70m. These approximate half under discrete physics.
A capped large impact retains vertical lift and approximately half horizontal
travel; Classic stays equal. No new OOM or memory stop.

Affected authored Airburst court interaction passes1/1in8.82seconds. Dante now
travels7.374m to z5.374 with the passive, rather than the earlier9m edge-clamped
result before Earthbound existed. Landed frame inspected. Preserve the old
Airburst result as historical evidence, not the new Dante travel claim.
No actual remote peer or new player build is claimed. Other Dante Wiki changes
remain open, and existing ward/barrier presentation is untouched.
