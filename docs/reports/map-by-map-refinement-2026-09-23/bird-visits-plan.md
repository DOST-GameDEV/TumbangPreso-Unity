# Perched bird visits: research and Eskinita unit, 2026-09-24

Baseline d914a3497. All six ground animals are individually implemented/reviewed.
REFINE-2.7 remains open. Work on Eskinita's visits first; keep Lagoon's already
qualified aerial wandering, every original bird mesh/clip and other maps intact.

## Cause and references

The current non-aerial Bird method always visits Route[1], flies a straight line
at constant speed, instantly levels its body at landing and deactivates at the
same nearby departure point. Eskinita's three departures are only four horizontal
metres from the perch. Random wait times do not vary those visible paths. The
source bird rigs have wings/head/body/tail but no articulated legs, so sliding a
pigeon over the floor would be a poor substitute for an authored walk.

- [Wild Bird Club of the Philippines, urban birds](https://birdwatch.ph/2013/07/03/10-most-common-urban-birds/):
  local context for the maya/tree sparrow and pied fantail, ground/tree foraging
  and the distinctive fantail. The older article's taxonomy is not our current
  naming authority. No photos, calls or text are copied into the game.
- [National Museum Zoology Division](https://www.nationalmuseum.gov.ph/2022/02/14/fantail-in-love/):
  Philippine fantail, insect eating, fan tail and vegetated urban/coastal context.
  Keep a lookout/tail-flick personality; do not turn it into another seed-pecking
  pigeon. No nesting aggression or new player attack mechanic.
- [Cornell tree-sparrow life history](https://www.allaboutbirds.org/guide/Eurasian_Tree_Sparrow/lifehistory):
  picking/gleaning across ground and vegetation supports short foraging pauses.
  Its habitat discussion is North American, not evidence for Philippine density
  or frequency. No exact game timing is claimed as a biological measurement.
- [Audubon rock pigeon](https://www.audubon.org/field-guide/bird/rock-pigeon):
  ground foraging and ledge/bridge context. Retain planted pecks and clear fly-to
  transitions; without leg articulation do not add a fake gliding ground walk.

The earlier BlueTwelve and House House primary design references remain useful:
clear intent and transitions, fitted to a real place. The Alba search only found
studio/contact material; no detailed animation technique is inferred from it.
No embedded reference video was watched or used as a claimed motion observation.

## Eskinita implementation plan

1. Opt in only this map's three visiting birds. Bake a small choice of supported
   clear landing spots near each existing site, excluding the lata centre. Keep
   sparse private scheduling and refuse arrival on a nearby person.
2. Vary arrival side and destination, using a curved approach with a gentle final
   settle and gradual body leveling. Native wing clips remain; Maya is a quicker
   flutter, kalapati a steadier beat, fantail between them. These are art timings.
3. Depart away from a close player/accepted impact, accelerate upward and continue
   well beyond the court rather than disappearing four metres from the perch.
   Flight through distant scenery remains allowed by the existing owner contract.
4. Maya and kalapati keep grounded peck/look pauses with distinct dwell/peck rates.
   Fantail uses watch/tail flicks instead of the seed-peck loop. Preserve feet and
   the existing fan mesh. No added collisions, rewards, game RNG or messages.
5. Freeze movement/pose with existing scaled time. Keep old default behavior for
   all other maps until their individual visit review. Never rewrite Lagoon flight.

## One native review

Use the existing Record and GameplayShots helpers, with fixed cameras that can
see the old nearby disappearance. Record temporary legacy visitors and new real
visitors in the same scene, each species individually. Inspect perched/flight
poses and 25 percent grey; verify departure continuity/distance, multiple legal
sites, reactions and pause. No new capture framework. One bounded fixture repair
maximum, initially zero used. Keep the result only after actual image review.

Then independently fit/review Bayan, Ilalim and SaBubong visits. Do not close the
parent from this one map. All older bot, body/FPP, ultimate, map integration and
final native/peer/replay requirements remain assigned.
