# Ignition Cannon impact, 2026-09-25

Native v59 review: the large orange dome labelled `blast_fire` is Supernova's
4.8m staged blast, not Ignition Cannon. The latter routes through
`Slipper.TriggerAffinityImpact` with radius2.6m and `ExplosionStyle.Slipper`.
The staged ordinary slipper impact is a beige smoke puff and six-point comic
pop. That keeps a normal throw small, but it leaves the fire-loaded skill's
impact without Sean's planned five-point ignition identity. Preserve the
normal throw's bare treatment and Supernova's larger authored event.

Use the existing ExplosionLook style seam to add only an Ignition style. Keep
the skill's2.6m radius, knockback13, stun1.4, host authority, neutral stun
element and physical slipper impact sound. Its separate cast sound already
identifies preparation. Draw a small five-point warm rim at the actual radius,
short embers, and the existing Ignition BoltHead flipbook instead of the beige
smoke. No large fireball, extra gameplay field or new renderer/shader system.
The projectile stays its current ember-marked slipper. This stages a clear
build-up, impact and falloff without borrowing Supernova's dome.

The design follows the cloud per-skill plan and Riot's developer account of
readability across graphics quality: the primary shape conveys what happened,
while supporting light and particles stay secondary
([VALORANT Shaders and Gameplay Clarity](https://www.riotgames.com/en/news/valorant-shaders-and-gameplay-clarity)).
It is adapted to TUMP's blocky court and five-point Sean motif.

Author one local variant. Bump the existing AbilityShowcaseProbe image version
and add its Ignition impact beside ordinary Slipper and Supernova, then inspect
the same eye/overhead cameras and25percent grey. The probe covers staged
geometry and the built-in blowout threshold, not live throw timing, audio,
network peers or full sensory approval. Stop after the focused visual verdict;
do not grow a new capture framework or rerun unchanged gameplay suites.

First authored v60 read: the five-point floor mark was clear from above but
the BoltHead sheet read as a tiny dot at eye height. It was rejected. V61 adds
a short five-sided ember core below1m high, with the same2.6m radius and no
Supernova dome. Inspect the eye result once; do not tune fixture thresholds.

V61 made the center visible, but its five-sided spire read as a little pyramid,
too close to an earth shard for Sean's fire theme. The final local study uses
the same tapered flame-tongue construction as Sean's retained fire trail,
over the unchanged five-point floor mark. V62 compares this in the same native
cameras. Pick the clearer v61/v62 image and stop art iterations for this unit.

V62's actual eye image was also too small. The reason is measurable in source:
`VfxShapes.Tongue` has0.35unit base radius, while ExplosionVfxAnim was told its
mesh radius was1. V63 corrects that mesh-radius argument, retaining the .85m
height cap and2.6m gameplay footprint. This is the last local candidate for
this unit; choose v63 if it improves the eye read without dominating the court,
otherwise restore the clearer v61 cone and record the limit.

Final selection: restore v61's short five-sided ember. V63 enlarged the tapered
flame tongue but still read as a thin stroke next to the can, while v61 conveyed
a small ignition at eye height. The gameplay radius remains2.6m and the upright
piece remains under1m. No fourth art candidate or extra Unity run. The capture
filename version is reserved at v64 so a future run cannot overwrite the
rejected or selected comparison images; its runtime shape equals v61.
