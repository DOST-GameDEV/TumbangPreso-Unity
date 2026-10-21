# Cryo owner revision and timed Boulder

## Implemented values

- Cold Feet: radius2.5m, aim range0.5–5m, full aim reach after1second. The field
  retains its7.5second lifetime and35second cooldown.
- Frostbite: a15second window. Every throw during it receives its own one-hit
  Frozen payload; a throw neither consumes nor refreshes the window. Throws after
  expiry are normal. Held-shoe activation,35second cooldown and2.5second Frozen remain.
- Glacial Wall:10second lifetime, three hits to shatter, range1.5–4m,1second aim
  ramp,5m nominal arc length and3m curve radius. Five contiguous segment centres
  replace endpoint spacing. Authored ice meshes overlap slightly at the outer
  face, so rendered surfaces and their convex colliders close the same gaps.
- Latest Wiki Boulder window:15seconds on the exact loaded shoe. Drop/regrab
  cannot refresh it. A timely held throw keeps its flight payload; expiry before
  launch, even before the next Update, prevents an expired payload. Replacement
  affinity retires the old deadline. Simulation pause preserves the deadline.

Protocol147 gates these changed gameplay rules. Existing reliable slipper state
carries host Boulder expiry; no packet layout expansion was needed.

## Evidence

Original15second requirements fail2/2: Frostbite ended at10seconds and Boulder
had no expiry. Candidate load/lifecycle cases pass14/14, then two added pause and
launch-boundary cases pass2/2. Those earlier runs predate the later all-throw request.

The later request baseline proves the former first-throw consumption and2.3m
radius. Cold Feet's five-second post-exit timer already passes. Its shove control
initially missed because the fixture used a hardcoded victim position after the
caster had settled; no product-duration conclusion is drawn from that failure.
The corrected control places the victim relative to the actual caster and
requires a recorded landed hit.

The current full load/status suites pass20/20 (33.0925691seconds). Four additional
field/recovery cases pass4/4 (.6217289seconds). For the compact wall, the old
five-metre preview misses36of243 collision rays across three body-height samples.
The compact candidate passes all243rays and the final three scoped tests3/3
(.979223seconds). Native front/side images inspected: the gaps are closed. The
same visible ice supplies collision; no invisible filler wall was introduced.
Current Core number assertions are recorded separately.

All successful native jobs exit0, restore isolated settings, preserve frozen
inputs and show no OOM increase. Failed baselines are retained above.

## Chilled report remains open

The shared Chilled constant remains5seconds. Actual isolated shove Chilled ends
after5seconds; actual Cold Feet Chilled ends5seconds after leaving, while the
field remains alive. Standing inside continuously refreshes that timer. Absolute
Zero alone intentionally applies7.5seconds alongside its2.5second freeze to leave
5seconds after thaw. No global Chilled duration change was made here.
The owner's report of an unexpectedly long slow has not been reproduced. Online
versus offline and post-exit context were requested; do not label the bug fixed.

These are local native/managed checks, not fresh147actual-peer, packaged-player,
all-terrain or human-feel acceptance. Older146peer results do not qualify147.
