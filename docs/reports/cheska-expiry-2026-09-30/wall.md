# Glacial Wall breaks across its own arc

The previous wall disappeared into eight tiny fragments at one center. The
native baseline covered only0.93m/1.14m for a4.2metre arc. The new breakup uses
15individually specified chunks at the five authored slabs' real transforms,
with differing heights, proportions, directions and spin. They split, fall and
shrink/thaw over0.8seconds. The existing authored shard mesh and translucent
material family remain; no new solid props, flash, fog, light or sound is added.

HeroHazards changes only its visual call to pass the actual wall Transform.
Three-hit rules, expiry, immediate collision retirement, authority and the
existing replicated flair remain unchanged. The old restraint-thaw path is
untouched. Three-slab/split walls select the corresponding outer/center recipes.

## Checks and critique

Unity6000.5.8f1 Linux OpenGL, real Eskinita, isolated profiles:

- Baseline2/2fail the new spatial acceptance: the center puff cannot span the wall.
- Final2/2pass7.42seconds: third accepted hit and a rotated local replicated-flair
  entry produce15pieces spanning4.38m/4.54m in wall space. The first two hits keep
  collision; the third removes it immediately. Debris has no Collider/Rigidbody,
  repeated Shatter produces no duplicate and the complete effect retires.
- Real-time screenshots were sparse under readback overhead. A separate capture-
  only pass samples the unchanged native timeline every0.05seconds for review;
  it does not add another distinct gameplay acceptance case or a performance claim.
- No new OOM or memory guard. [Raw checks and captures](wall-checks/).

[Native timeline film](wall-checks/wall-native-v1.mp4) includes0.5second opening
and0.3second ending stills around the0.8second breakup. Intermediate frames were
inspected: gaps open across the whole former wall, chunks fall instead of becoming
another attack, and late chips clear quickly. The fixed review camera sees the
existing FREEZE billboard from behind; that text is not a new effect or a new
billboard defect demonstrated by this probe.

The concept sheet helped compare distribution, but its opaque smooth chunks,
intact initial columns and persistent puddles were rejected. This implementation
reuses the game's angular translucent shard; it does not claim exact fracture
triangles cut from each original slab. Human taste, physical-device performance,
refreshed player and actual network peers remain separate. No new wire semantics.
