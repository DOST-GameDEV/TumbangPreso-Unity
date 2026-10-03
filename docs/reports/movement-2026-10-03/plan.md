# Explicit movement revision

Use the owner's final October 3 numbers in both Classic and Hero Strike:

- Attacker walk 3.75 m/s, run 5.625 m/s.
- Defender walk 5 m/s, run 7.5 m/s.
- Character picks no longer multiply base movement. Active status, ability,
  terrain and committed-steering effects remain separate.
- A nominal 1 m, 0.75s jump uses body launch velocity 16/3 m/s and body gravity
  128/9 m/s squared. Slipper/projectile gravity stays 20 m/s squared and terminal
  fall speed stays 26 m/s. This avoids changing throw trajectories accidentally.
- Stamina capacity 100, drain and regeneration 40/s, regeneration delay 1s,
  sprint-start floor 20. Fatigue lasts 2.5s with no walking slow; sprint and
  regeneration remain blocked during it.
- Defender full lunge travels 3.5 m, with the existing 0.5s charge. Cooldown is
  0.5s at minimum/tap power and 2.5s at full power, interpolated between them.
  The active sweep lasts through the resulting impulse decay. Both local and
  host-resolved paths use the same power-to-cooldown calculation.
- Slide stays 1.75 m, costs 25 stamina, has a 2.45s cooldown and keeps 35% steering.
  Its total steering commitment remains 0.95s; it is decoupled from the changed
  defender recovery values. A retrievable loose slipper must be ahead; otherwise
  the same attacker input attempts a shove, not an unrestricted dash.

Update ordinary animation reference speed, tutorial orbit reference, pursuit and
activity thresholds to read the explicit role speeds. Keep unrelated companion
flight and throw-presentation references stable. Increase the movement packet
budget from 28 to 30 m/s to retain the existing greater-than-two-times-lunge
impulse margin. Protocol 137 requires matching rebuilt clients.

## Acceptance

The first managed integration run had 700 passes and eight failures. These exposed
superseded literal expectations and the two derived lunge constraints: active
sweep duration and movement-budget headroom. Correct those constraints, retain the
original result, and check the new owner contract. Final managed suite passes
708/708. Four distinct native physical cases are now qualified; see the result report
for measured values, both fixture failures and remaining player/peer limits.
