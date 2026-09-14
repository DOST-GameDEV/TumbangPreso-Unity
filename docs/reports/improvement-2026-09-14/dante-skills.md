# Dante: distinct skills within a demonic stone theme

Owner prefers clean dark forms, selective glowing fractures and purposeful motion.
The noisy texture experiment was rejected and its new surface helper deleted.
Do not restore it. Each skill and each component needs authored detail, rather
than one pattern repeated over the whole kit. Bernardo Carpio's mountain-struggle
motif informs the paired stone and ground force; the demonic details are Dante's
fiction. PHILIPPINE_ABILITY_DIRECTION.md records the source and interpretation.

Seismic Stomp now hits at0.30seconds, matching the retained body contact and FPP.
The caster stays grounded. Normal Stomp shoves rivals and kicks loose slippers
outward; Long Tremor trips rivals and keeps bodies/slippers nearby. Ground pressure
and branching fractures have separate contact, heat and cooling phases. The
warning uses the equipped radius exactly once. Duplicate eruptions, cube sparks
and the extra green held-key marker are removed from Dante's path.

Carapace retains its fitted carved armor and adds three closed shield fragments
orbiting the torso, with opening/closing motion and molten seams. Owner approved
the orbiting shields, then allowed optional visual refinements if useful. The
current correction changes only repetitive markings on FITTED armor: distinct
fractures for keystone, left/right wings, back and flanks, with plain shoulder
planes. Orbit geometry, markings, materials and motion remain unchanged in that
correction. The focused image/visibility check passed for both variants; front/back/side
images were inspected. The front view was moved between orbiting stones so the
fitted armor can actually be assessed.

Titan Fissure opens a forward break between two hewn, hooked stone faces. They
rise, move slightly apart, retain real convex collision and leave a central path.
At contact, nearby players receive a short punch and gentle2.4second ground rumble
with two decaying aftershocks. It translates the camera slightly without rotating
aim or changing simulation time. The new pillars and four small fault stones
rattle subtly. The caster/rear stay outside the forward hit. Early green column
and heavy chromatic wash are removed. The five-second stone expiry remains.

The7existing Dante sounds have contact-aligned preparation, armor locking and
recovery. Source material/attribution and can/UI cues remain. Finite samples,
clipping, endpoints and level limits passed; listening approval is not claimed.

## Evidence and corrections

- Earlier Q baseline launched its caster about2.62m and did not move a Loose shoe.
  Caster exclusion and Long Tremor's trip replace those incorrect displacements.
- The first attempt to start loose-shoe flight produced a misleading8.13m distance:
  it was a TELEPORT, not a successful kick. Unity's initial sphere overlap returns
  a synthetic reverse normal and zero point. BounceOffObstacles used that zero
  point as a wall contact. The corrected path resolves a real local surface,
  rejects ground/escaping contacts and preserves true wall rebound. A focused
  ground/wall test and the actual Q test now require outward flight. The previous
  any-distance result must not be presented as valid flight proof.
- Latest clean-orbit/quake pass:7/7focused PlayMode tests passed. Covers both armor
  variants, orbit/camera/cleanup, bounded earthquake without aim rotation, actual
  Q/Long Tremor warning/contact/outward flight, pillar gap/expiry, ground-overlap
  kick and wall rebound/escape, existing hit-freeze anchor and real3skill motion.
  Guard profile receipt59e73222ceaa. Actual Q caster0m/target0.848m/shoe10.79999m
  outward at0.30364s; Long Tremor target/shoe0m, triptrue, contact0.30481s.
- The7test pass follows the earlier focused mobile-cast gait, caption-camera and
  forward-hit regressions. No full suites were run. Normal-speed recordings keep
  measured frame intervals; they are not performance benchmarks or device QA.
- Fresh INTERNAL v3 build succeeded. RuntimeSHA
  `88f1cae6fca88b4ae23966da6af2ac8c2778981356afce1cddf44cc632f9d11f`.
  Five actual3process cases passed with150ms EACH direction to the owning client:
  Stomp, Long Tremor, Carapace, Heavy Plating and Fissure. They verify actual
  selected variants, one-charge spend, outward flight, trip, immunity/movement/
  expiry, three orbiting protectors, two pillars and per-peer quake response.
  Settled position agreement is within0.11m. Fissure rumble measured3.49mm host,
  6.84mm owner and4.74mm observer. The rear stays still. Both wards block the
  actual incoming hit, allow movement and permit stun again after expiry.
- Earlier failed network fixtures looked for a nonexistent defender shoe, compared
  local countdowns and allowed the observer's late spawn to overwrite a host-only
  target pose. Corrected fixture uses player3's actual shoe, shared server-clock
  cast windows and initialization on the target's owning peer. No displacement,
  immunity or peer-agreement assertion was relaxed.

The later fitted-armor marking change is cosmetic and follows the v3 binary.
Its image/visibility check passed. Updated INTERNAL v4 built successfully1057MB
in42seconds, with runtimeSHA `e1ef74a15c6db59c25406391d11c1ca86c0da7ffc51fb1c229a4bf959841a4c6`.
The only runtime difference after v3 peer proof is fitted armor markings and
comment formatting. Mechanics and the approved orbiting shields remain unchanged. No Desktop update. Inday FPP remains deferred, and the remaining kits,
movement, spectator, engineering and final UI work stay active.
