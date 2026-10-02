# Boulder: persistent stone inlay on the actual shoe

Three unequal stone-coloured plates with thin gold edges now mark the real
Concussed slipper. The mesh silhouette and authored surfaces remain underneath.
This is a stable payload cue, not an invented buff timer or a replacement boulder.
The world cue follows the actual affinity through held, dropped and regrabbed
states. Snapshot-applied affinity works with a different hero. The owner's
copied mesh uses the same treatment and clears when its source changes.

The observer hides on Normal affinity or disabled shoe, respects the original
renderer visibility, and reuses one inlay across repeated loads. Materials are
owned by the existing effect lifetime helper. No gameplay, ownership, cooldown,
status, hit, protocol or recording-schema change. The only Slipper.cs change is
attaching the visual observer in Awake. HeroHazards remains untouched.

## Causal and lifecycle evidence

The original Normal control passes. The original accepted Boulder establishes
Concussed affinity, then fails because no shoe cue exists. That failure is kept.
Four candidate cases each pass in their own fresh guarded graphics process:

- Normal control preserves the mesh and authored material array.
- Actual kit activation shows the inlay; drop/regrab retains it; clear retires it;
  repeated load/clear keeps exactly one child and the original surfaces.
- Public snapshot-applied affinity on a Cheska carrier shows the same cue;
  disable/re-enable and Normal snapshot retire/restore the right state.
- Owner copy preserves the mesh/surfaces, clears on a replacement Normal shoe,
  rebinds to the loaded source, and clears without allocating a second inlay.

All four runs use unchanged inputs and the same memory guard. Outer durations
are55/30/30/30seconds, guard reason null. The compile-heavy first run peaks at
7.920GiB total including file cache; warm cases at6.339/6.376/6.336GiB. The guard's
combined total and anonymous/shared pressure condition was not reached. No
memory threshold increase, parallel Editor, tooling retry or personal PC use.

World and owner native renders were inspected: the real loafer remains visible,
with distinct unequal inlay regions and no external orb or floor effect. These
isolated close-ups use a dark fixture background and are not a lighting benchmark.
The supplementary actual-input motion check and film are recorded separately.

This qualifies visual lifecycle, not a new actual-peer transport test, gameplay
contact regression, full-map player run, Low-tier performance or human taste.
SFX is unchanged. Full Geo presentation acceptance remains open.

## Actual-input integration film

The existing public-input Boulder motion case also passes1/1 with the new
surface observer,4.917seconds of test time,35seconds outer,78paired body/owner
frames. Accepted Skill2 retains the same shoe and Concussed affinity. The owner
view shows the inlay during the lift and throughout the return to ready; its
central aiming region remains clear. [Silent before/after and half-speed film](review.mp4)
uses simulation timestamps, with the earlier motion-only candidate as baseline.

Its first supplementary launch crashed in Unity's Mono named-pipe startup
with a file-descriptor assertion before tests, exit250. No XML was produced.
The isolated profile was restored and job lease released. Native soft/hard/open
file limits were16384, with no surviving Editor/compiler. One bounded fresh
startup retry passed with the same frozen input and guard; no limit or product
change was used to turn a result green. Both attempts are retained.
