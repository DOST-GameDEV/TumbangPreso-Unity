# Airburst current Wiki delay, angle and map reach

The live non-proposed Wiki Airburst cell now says1.5seconds and a60degree fan.
Code still used2.5seconds and70degrees. The shared Core gather is now1.5seconds,
and its half-angle30degrees. Both gameplay contact and the existing warning use
that same half-angle. In-game descriptions reflect the adopted rules. Whirled,
body/slipper lift, carry tuning, shot credit, cost15 and stable ability ID remain.

The cast still starts its committed delay after the shared introduction reservation
is released. It does not apply effects during reservation, spend the meter again,
or release before the1.5second windup finishes. Protocol114 requires matching
players for these changed gameplay/counterplay semantics; no packet field changed.

## Map-wide reach bug

The authored Lagoon Cove builder declares playable bounds X[-16,16],Z[-13,24].
Its diagonal is48.9m. The previous fixed40m contact range could miss legal players
on the opposite side, while the warning's separate26m range understated reach.

Range now uses the measured court diagonal with the existing40m minimum. The
engine-free helper rejects NaN/infinite/reversed/collapsed or overflowing bounds.
The contact gate and existing AmihanStormFan use the same value. This changes
only the warning's range binding; existing fan materials, density, motion and
art direction remain. No map, model, authored animation or audio assets changed.

## Evidence and limits

- Core final2/2: current costs/timing/angle and measured-range/malformed-bound rules.
- Native first3/3: real held slipper and body stay unaffected at1.49s and launch
  at1.51s; real body/slipper contacts distinguish29.9from30.1degrees; reserved
  introduction starts that same delay afterward without a second meter spend.
- Native additional1/1: a synthetic floor with the actual Lagoon-sized bounds
  places two attacker seats more than40m apart. The player is Whirled and the
  slipper enters flight. The spawned warning component uses the exact same
  measured range. Global playable bounds are restored afterward.

Four distinct native cases pass across retained receipts, not one final4/4suite.
No native failure or fixture/tooling repair occurred.650final hashes have no drift,
and six owned runtime/test inputs match the native candidate. The guarded launch
preserved named profiles and shared input preferences. Both jobs are terminal.
Exact XML/manifests/CoreTRX are in airburst-wiki-checks; raw logs remain in isolated
Logs/feedback-0930/airburst-*.log. Source08916a9af plus explicit owned overlays,
native editor6000.5.8f1/D3D11, baseaecc0ee2 imported candidate.

This proves release/selection/flight-state and warning-range binding. It does not
claim actual authored-map film, human feel, current player, real peer transport,
lossy delivery, reconnect or Windows/Android qualification. The frozen103player
cannot qualify114. Unchanged prior carry/landing/authority evidence is reused.
