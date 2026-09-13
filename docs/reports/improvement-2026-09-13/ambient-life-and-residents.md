# Ambient life and rooftop resident detail

Owner rejected the first cats/dogs as too similar, oversized and not cute. The
current street-animalstudy-v7 rebuilds feline proportions: lower/shorter body,
short legs, broad face, tiny cheek pads and shorter ears. Aspins retain longer
muzzles, different builds/ears and coats. Six native models and three original
birds retain editable Blender sources and imported serialized animation clips.

Cosmetic actors use their own randomly seeded stream. Birds vary waits/arrival
chance, perch/peck and leave on player proximity. Ground animals idle/walk/run
along measured routes outside the court. Cats/dogs live in the three street maps;
only birds visit Sa Bubong. The owner permits birds to pass through scenery in
flight. Ground routes retain full-body clearance from walls/trees/props.

Dogs occasionally lift a hind leg beside a measured tree/surface, with90-180s
initial wait and120-240s cooldown after a pause. A nearby person interrupts the
brief action and its faint stream immediately. The actual imported BackR bone
sets which side faces the surface, rather than assuming glTF handedness.

## Evidence and criticism

- Logs/ambient-life-v7.xml:3/3 focused runtime probes passed. Includes all4maps,
  six ground animals,12staged bird approaches and3dog-pause interruptions.
- Gait-contact.csv samples actual baked skin against a short ground support ray.
  All six actors move their legs36-44degrees; drawn gaps range-.006 to+.0023m.
  The previous bounds-based placement left dogs roughly5-7cm above the ground;
  seating the actual neutral skin removed that defect.
- Dog-pause CSV records raised-leg motion around67degrees, facing the surface,
  interruption and actual retreat. Staging validates response/animation, not a
  distribution study of natural arrival times or the rarity schedule.
- Native comparison Logs/animal-scale-v7.png places cat/aspin beside retained
  Maring. Live front views in Logs/ambient-life-v7 confirm the smaller cat face
  and body relationship. This is improved local ambient art; it does not imply
  the full map/game/animation plan is complete.
- Earlier failures remain in Logs/ambient-life-v1/v2. V1 found real material/
  clearance problems; V2's player stopped outside the proximity radius, so the
  test now approaches by measured distance rather than waiting a fixed time.

GLB multi-primitive skin normalization uses the existing byte-preserving tool,
with a same-input proof in Logs/ambient-normalizer-proof/result.json. Native
export now sets actual PBR colours, not only Blender viewport colours. No package
or global quality workaround was introduced.

## Rooftop resident corners

Three original potted plants, a reused paint pail, nested wash basins and a small
flush stairhouse leaf painting give the shade/utility corners a maintained
resident character. The first broad leaves looked like diamond flags; their
volumes were revised before import. Seven pots are grouped near the shelter,
stairs and a real table surface, outside the court and swimming route.

First author gate reported30 unsupported pieces: individual leaves/pail handles,
a wall painting and a table plant parented outside its actual support assembly.
Plants are now single assembled meshes preserving every material/vertex, the
table plant belongs to its supporting shade assembly, and the wall painting has
a measured3mm attachment offset. Authorv2 passes the all-map geometry check.
Static views in Logs/roof-residents-review-v1 show planted bases and clear paths.

The roof still needs full runtime/cast/effect/network review. Static pool review
also exposed a real material defect: NearFade replaced the procedural ceramic
shader, removing the intended tile pattern. The preservation tag fixes this;
NearFadeTests passed14/14 focused checks and Logs/roof-residents-review-v2 shows
the actual joints visible through the water. The water remains restrained rather
than bright opaque paint. Its local swimmer wakes were previously exercised in
the focused both-mode ordinary-speed water-detail probe1/1.

Gating source audits passed14/14 after this batch. Non-gating cue-audio audit
still flags7historical files; this is not a claim of complete audio qualification.
Whole-cast swimming/recovery binding passed1/1 containing all18people, both modes:
Logs/whole-cast-water-recovery-v3. All rigs advance their serialized swim loop,
settled owner eyes stay at.390m, accepted mash recovery returns every rig to its
feet; measured recovery contact spans-.0444 to+.035m across people/samples.
This is rig binding and pose coverage; real outer-fence descent/input was proved
by the preceding focused roof probes, not simulated by this fixture.

V1 incorrectly expected a2.5s automatic get-up; the game deliberately requires
mashing before its later auto-recovery deadline. V2 corrected that staging and
passed, but its witness inherited FPP self-hiding. V3 renders the witness body
and hides only private owner arms during that camera's synchronous capture,
then restores both. Owner views retain their normal visibility. Body/owner and
bracing views show the retained blocky cast, water submersion and supported palms.
No new Windows build or current separate-process roof/pool proof yet.
