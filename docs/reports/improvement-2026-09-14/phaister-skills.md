# Phaister: staged lunar ritual and distinct spell forms

In progress on ASTRAReworks after e6237a0d. This report records current evidence,
not acceptance of the whole six-hero rework. See ACTIVE_REWORK_LEDGER for processes.

## Reproduced faults and corrections

The baseline first curse occurred 0.00246 seconds after activation while the
visual circle was still preparing for 1.55 seconds. Under Ilalim's measured
8-metre ceiling, the moon reached 10.98 metres. Its drawn boundary radius was
1 metre despite a 10.5-metre curse, and a grounded inscription drifted 0.38492
metres as its already projected transform rotated.

Grand Coven now has an actual 1.55-second preparation before the first curse.
The ritual is owned through preparation, activation, reset and predicted rollback.
References are assigned before explicit eclipse initialization, preserving the
correct reach scale and light values. Moon placement respects overhead geometry.
Ground layers reveal through opacity and stay fixed after terrain projection.
Six controlled floating glyphs retain the grand effect without continuous motion
on every mark. Actual curse attempts pulse the centre, and hit announcements
require a successful Hex stun.

Hex uses a compact opposed-crescent binding, with a different tight woven binding
for Slow Brand. Shadow Blink leaves a vertical dark passage at departure and a
brief flat closing fold at arrival. Those are different forms from Grand Coven.
Duplicate body auras, generic cast flashes and the old large column are removed
from these paths. Materials are restrained lavender and purple ink; there is no
pasted texture or claimed traditional writing system.

## Retained animation and first-person framing

Only input timestamps of `hero-phaister-eclipse` changed in team-phaister,
team-custom and team-custom-base. Original binary chunks, pose outputs, geometry,
materials, skins and the other 35/50/50 clips are preserved. The contact now occurs
at 1.55 seconds and recovery ends at 2.12. `tools/retime_phaister_ritual.py`, its
Logs JSON receipt, the authoring recipe and procedural fallback agree.

The longer first-person hold initially brought the Loafer too close to the camera.
A subsequent right-arm pose adjustment keeps the prop visible while opening the
central view. Existing hand geometry, clothing, grip and animation interface stay.
The latest owner contact sheet was inspected; ordinary-speed videos are encoded
from recorded timestamps. This does not substitute for human play-feel approval.

## Focused evidence

- `Logs/phaister-baseline-v1.xml`: one real three-skill review passed; two new
  timing/surface contracts failed with the measurements above.
- `Logs/phaister-staged-v1.xml`: 4/4 passed, profile receipt 8ea96f85b60c.
  First curse 1.554211 seconds; moon top 7.33 metres; boundary radius 10.5 metres;
  ground drift zero. Reset/rollback removed unfinished ritual and movement root.
- `Logs/phaister-distinct-kit-v2.xml`: 2/2 passed, receipt feb7a349677b.
  Binding/passage footprint and expiry contract plus the real three-slot input
  capture. Contact sheets and six timestamped videos are under the matching folder.
- `Logs/phaister-moving-warning-baseline.xml`: 1/1 passed, receipt 70702cad619b.
  A suspected moving-caster fault was disproved: after a six-metre relocation
  during preparation, the original warned target was cursed and the target
  outside that warning stayed safe. `HeroAbility` already stores committed pose
  in `AbilityContext`. No production change was made for this hypothesis.

`NetPhaisterProbe` and `tools/net_phaister_review.py` add opt-in separate-player
evidence for a real delayed client cast and a host-rejected prediction. They trace
warning/active clocks, world centre, actual victims, duplicate visuals and sky
cleanup. Internal build v1 succeeded at 1057 MB/57 seconds, receipt b3ecacb291e9.
Its Runtime DLL SHA256 is
`6f8788910e6cebc63bba0fb7042db7bb7c71090a1c9030cdbab93236be871e72`.

The first actual three-player run used 150 ms delay in each direction on the
owner's link. It exposed a fixture fault: the outside target's x=12 teleport
clamped to the arena's 8.6-metre half width, which is inside the curse. The fixture
now places it 15 metres away along Z and records its actual location. NGO server
time also showed a local warning duration different from the animation timer;
the next trace includes Unity game time, real time and frame delta to distinguish
timebase correction from actual shortened preparation. No duration check was waived.

The rejected-cast case reproduced a genuine bug. The owner removed the refused
circle and active clock but retained eclipse weather through 96 samples in the
late observation window. Host and observer had no ritual or curse. The targeted
correction waits for host acceptance before a predicting Phaister owner replaces
global weather; hands and circle still prepare immediately. The existing accepted
PlayAbility payload now reaches that owner as presentation confirmation only,
never a second cast or spend. Remaining weather duration accounts for elapsed
preparation. A denied cast cannot erase another hero's sky because it never
replaced it.

Corrected actual-peer results:

- `net-phaister-coven-v2`: PASS across host/owner/observer with 512/501/509 samples.
  Exactly one ritual, correct fixed centre, actual nearby curse, caster/outside
  target safe, clean expiry. Accepted sky was present through 102/103/105 sampled
  active frames; `recheck-accepted-sky.json` adds that check to the recorded trace.
  Runtime SHA `14ef537590f74f0bdc317c77e351f62d5ad2446d1647e3291f9eccb5f09ac96e`.
- `net-phaister-rejected-v2`: INVALID fixture, correctly failed. A legitimate
  host meter snapshot erased the owner's forced test meter before it pressed,
  so no prediction occurred. Zero leftover graphics alone was not a pass.
- `net-phaister-rejected-v3`: PASS. Owner has seven samples of real predicted
  preparation, then no active ritual, no curse and zero late sky samples. Host
  and observer never create the rejected ritual. Eight existing named-profile
  files were restored. The fixture now arms the meter on the actual press frame.
  Runtime SHA `a7a07b259f443219658473777f381c307fa0debd23375ec40aeb7f4257da893e`.

Current internal v3 build succeeded at 1057 MB/44 seconds, receipt e67029ad7b1f.
Between v2 and v3, production behavior is unchanged: a private field and its
documentation moved into place, and the opt-in rejection fixture was corrected.
The Desktop executable was not updated. All peer helpers have exited.

Timing limit still open: measured time from the first sampled warning to local
activation was 1.36-1.41 seconds in the loaded three-player run, with approximately
0.20-second frame deltas around effect creation. The observer first sees a host
curse 1.255 seconds after its first sampled warning. Sampling after creation,
frame cost and transport arrival must be separated before claiming a full visible
1.55-second warning on every peer. The earlier local input test measured 1.554211
seconds from actual press to curse; these are different measurement boundaries.
Do not explain the whole discrepancy as NGO clock adjustment without profiling.

An exported-rotation audit covered all 18 named hero casts and found no exact
cross-hero channel duplicates. Different hashes do not establish good choreography;
the owner's distinct body/FPP cast requirement still needs visual review per kit.

## Construction cost follow-up

Profiling found measurable synchronous setup cost. An isolated Ilalim factory
recording took 84.15 ms on first use and 46.42/43.33 ms on repeats, creating 139
renderers and 20,705 vertices. This is Editor CPU evidence, not a player FPS claim.

Two independent changes preserve the authored geometry:

- Ground projection caches collider classification only for a single synchronous
  projection. It still performs the same rays, exact-coordinate height caching,
  surface precedence and rise limit. Repeated projection time fell from a mean
  12.10 ms to 5.20 ms. Three checks pass, including changed court ownership and
  a newly attached rigidbody between calls, so classification cannot remain stale.
- Ink with the same colour, emission and reveal phase shares one material inside
  that ritual instance. Different phases remain independent. The first renderer
  owns its layer's material; every piece ends with the same ritual root. This
  lowers repeated material creation and colour writes without replacing glyphs.
  Measured material setup fell from 18.27 ms to 6.63 ms; total repeated construction
  fell from 41.66 ms to 32.12 ms in this comparison. Three focused phase/contact/
  reset checks pass. No global shared material or cross-skill art reuse was added.

All generated mesh/topology fingerprints remain
`53f1bb05633930e13090246a1f02d03e5524ce2066bfba79050a0df4766db4c7`.
Every renderer's colour and emission across six sampled reveal/fade phases remains
`0076df0d32edd13eaaac6649672dd97cec96349a7ec9ebcbb88d1eed95fe39ef`.
This verifies the measured data, not a blanket claim about every rendered frame.

Receipts: construction 38ee5f037b23; projection baseline db95614c8dec;
projection cache 6c161f7a80bb; setup profile 4691e03564ea;
material phase baseline 68254c38fc2b; shared stage materials 5242019c1b56.
The cache/material multi-test runs had already warmed the effect before their
first recorded sample. Do not cite those first samples as cold-start improvements.

Standard profiler markers record projection, ghost material setup, script arcs
and tick arcs. Arc measurements contain material time, so they cannot be added
as independent costs. Exact CSV paths and the current internal build are in the
active ledger. A fresh player and reconnect investigation follow this batch.

The fresh v6 player passed the regular three-peer cast check with the caster's
actual CharacterIndex, kit ID and authoritative room pick all agreeing. The
fixture selects through the room API and initializes that accepted selection in
its preloaded arena using normal SyncPicks reconciliation before the timed cast.
This follows the established familiar fixture and avoids a kit-only substitution.
Build v6: 1057 MB/44 seconds, receipt e37fcedc3f2d; Runtime SHA
`47515c0cad199fa199bdec89fffef524f409e5da7f494b015520ac6548fd71ab`.

The corrected rejoin case, `net-phaister-rejoin-v3`, isolates an actual missing
state restoration: host and owner keep their ritual, while the returning observer
has the correct Phaister character but no circle, active clock or sky during 20
samples in the host's still-active window. This is OPEN, not a passing reconnect.
Earlier rejoin v1 changed away from Phaister because the fixture had only bound
her kit, leaving the arena's actual pick inconsistent. v5's room request alone
did not update the already preloaded test arena and timed out without a cast.
Those failures were fixture limitations, not evidence to rewrite the working
same-hero UpdateLoadout path. The current observed missing effect is distinct.

## Remaining qualification

Finish actual delayed peer cases, rejection cleanup and late-join state. Review
the complete Hex/Slow Brand and Blink/Long Stride mechanics and counterplay.
Check confined defender behavior and ordinary-speed full cast motion. Preserve
the rest of the project queue, no-thumb cast, retained outfits and saved profiles.
