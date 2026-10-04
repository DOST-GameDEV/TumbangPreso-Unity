# Restore near-original jump physics

Owner request: upward launch5.75m/s, character gravity20m/s², maximum falling
speed25m/s. These replace launch8/gravity32/fall26 from the earlier same-day
playtest revision. Projectile gravity remains20. Walk/run speeds, stamina,
shove/lunge and slide rules are unchanged. Protocol141 requires matching peers.

Ideal continuous flat-ground trajectory is0.8265625m high and0.575s airborne;
actual character-controller collision and50Hz steps affect measured values.

Four focused managed movement-contract checks pass, including exact new
constants and unchanged role speeds, stamina, fatigue and committed-movement
rules. Shared compiler servers were shut down afterward.

Native3/3 passes in6.406s. Measured flat-ground jump0.8850m high and0.5800s
airborne; actual long-fall velocity reaches and remains25.0000m/s. All eight
Classic/Hero Strike attacker/defender walk/run samples remain exactly2.5/5 and
3.75/7.5m/s. Final runtime exit0/no guard, treeRSS3,329,032,192 and total container
7,403,995,136bytes. Frozen source hashes match. Compiler completed/reloaded assemblies, then hit the
container-headroom guard during import; retained rather than called a pass.
The first runtime uses those compiled assemblies after scoped idle cleanup.
Both private EditorSettings and QualitySettings are preserved/restored by the
job trap; production settings are untouched. No graphics or player claim.
