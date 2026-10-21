# Natural ambient life: research and first implementation, 2026-09-24

REFINE-2.7 remains open. First unit: Eskinita's tan aspin only. Current baseline
02617e9d2 includes the finished storefronts and refreshed street-map cards.

## Observed cause

AmbientLife.GroundAnimal advances through a route array in order, reverses at
each end and sometimes waits. Authoring discards the sampled area except for its
longest path. Random wait durations cannot hide the repeated patrol. It also
treats every player within three metres as a threat, including stationary people,
and snaps the animal the last nine centimetres onto each grid point.

## Research and interpretation

- [BlueTwelve's Stray development account](https://blog.playstation.com/?p=367025):
  the producer describes real animal references and close animator/programmer work
  on movement and transitions. Read the article, not the embedded clips. For TUMP,
  believable intent, weight and transitions matter more than adding random motion.
  Preserve the blocky model and its measured leg cycle, not Stray's realistic mesh.
- [House House's GDC session abstract](https://www.gdcvault.com/play/1027183/Google-Maps-Not-Greyboxes-Digital):
  real place references constrain and suggest play opportunities. Only the public
  abstract was read, not the talk. Our inference: animal activities should relate
  to actual sheltered edges, trees and views of the court, not abstract waypoints.
- [Biswas et al., free-ranging dog resting sites](https://arxiv.org/abs/2309.09056):
  the abstract describes 6,047 observations of 284 dogs in India, with shade,
  disturbance and visibility affecting site choice. This supports qualitative
  site selection only. It does not establish Philippine frequencies or durations.
- The PLOS street-dog testing article was located, but the targeted text fetch
  failed. No specific human-approach claim or timing is derived from it.

The design below is an authored game interpretation, not an animal simulation or
a claim to have watched footage that was not accessible.

## First unit contract

1. Opt in only aspin-tan in Eskinita. Keep all other animals, clips, geometry,
   colliders, map lighting, private cosmetic randomness and match rules unchanged.
2. Retain a connected habitat graph inside its existing west-side area. Bake
   support and full body clearance using the existing conservative surface queries.
   Keep multiple paths, choose useful clear activity sites, and retain a valid
   route fallback for legacy scenes. Author only this scene and animal.
3. Choose investigate, watch and unhurried transit goals with recent-visit memory.
   Face the court while watching; lower/look the actual head while investigating.
   Keep rare surface marking tied to its existing real surface and cooldown.
4. Ease arrival and stride-calibrated acceleration. Traverse supported edges
   without teleporting at every sample. Move the head after the existing graph
   evaluates, with no accumulating transform offset or changes to source clips.
5. Close intrusion or a fast approach prompts a brief retreat along safe links.
   A person standing farther away can be watched without repeated panic. Accepted
   impact events still interrupt, with quiet time and recovery to an activity.
6. Freeze with scaled presentation time. No added colliders, network messages,
   gameplay RNG use, combat, rewards or new player interactions.

## Validation boundary

Use the existing native review camera/recording helper. One focused case records
the legacy and new behavior from the same follow camera, checks reachable activity
transitions, reaction/recovery and pause, and saves state samples. Inspect normal
speed motion, contact and a grey thumbnail. Do not run every map or rebuild a
capture framework. One tooling repair maximum; genuine product defects are fixed.
Record the exact question and stop condition before the guarded Unity run.

## Preserved coverage order

After this dog, independently inspect Eskinita's tabby; then Bayan's cream aspin
and ginger cat; Ilalim's patched aspin and tuxedo cat. Review each map's perched
maya/kalapati/fantail visits separately. Lagoon's bounded flight/glide work is
already implemented and stays unless an actual defect appears. SaBubong has no
ground animals. Final all-map/live/replay/performance gates remain REFINE-2.10/P7.
Bots, per-character motion, later ultimate performances and all older TODO IDs
remain assigned. This unit does not close the map parents or overall goal.
