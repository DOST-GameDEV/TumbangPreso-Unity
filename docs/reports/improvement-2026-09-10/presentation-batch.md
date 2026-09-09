# Cast, motion and neighborhood improvement batch

This is an implementation checkpoint on ASTRAReworks, not Windows release qualification.
The four-player rules, role rotation, saved IDs and two modes are preserved.

## What changed

All eighteen selectable character models received beveled grip/thumb forms, footwear
profiles and restrained clothing details using their existing palettes and bones.
Berto and Kuya Boy carry everyday towel details; Inday has a readable apron and pouch.
Canonical faces, hair and recognizable silhouettes are retained. Inday's long hand
props were removed because they were being mistaken for the palm by the attachment
and slide solver. Her existing 0.950-second slide was re-solved after that repair.

Forty walking/sprinting clips across the twenty retained roster rigs replace rigid
leg swings with smaller alternating strides and grounded root heights. Carrying and
charge retain stable hands over moving legs. This follows the earlier crossfade,
rebinding and frame-rate-independent first-person recovery fixes.

All eighteen hero body actions now have preparation, an action-specific peak and a
return timed to their live job. Their first-person curves use both hands, preserve
continuity when interrupted and cancel only the matching refused action. The forty
first-person arm meshes are baked from the actual model geometry and atlas during
roster rebuilding, including the retained custom rigs. The close-camera cross-section
is bounded without changing the world character or held slipper's presented size.

Eskinita has deeper house fronts and an authored sari-store counter and canopy.
Bayan Plaza has articulated civic/church/belfry geometry, perimeter paving and broader
shade trees. Ilalim has guideway caps, bearings, soffit rhythm and drainage details.
Trilight fill and restrained grading separate the ground from the perimeter. Original
sourced livery and the quiet court remain. See [world direction](world-direction.md).

Supernova's warning now matches its existing 5.4 m reach; Kuro's warning uses its
actual pet-centered 4.0 m footprint. Bots now recognize that existing Kuro radius.
Zack's shock ring has an open center. World lighting changes and flash lights have
less intensity while each ultimate retains its sky signature. Footsteps follow
observed grounded travel, and retrieval slides have a separate rubber scrape.

Owner-only confirmations use the private audio route. Jump and landing reach peers.
Client sound requests accept only the five owner-produced movement/intent cues,
from a seated sender near that sender's body. A new positional-producer audit covers
the direct audio paths the existing NetCue audit could not classify.

Functional UI fixes repair the custom-game viewport, short telemetry label, minimum
character-door type size and stale copy expectations. The results board reads one
observed match moment without changing scores. The character maker stays inaccessible.

## Evidence and verification at this checkpoint

- Core: 559/559. EditMode: 467/467 before the latest diagnostic-only additions.
- Targeted navigation/capture run: 18 passed, zero failed, one explicit skip.
- Clock and real-time AI diagnostics: 3/3. The 90-second round ended after 90.006
  game seconds / 91.180 real seconds, with 75 defence events and 75 collected ticks.
- PersonSwapProbe: PASS. The naked base retains its deliberate lack of face/hair;
  face ink and left-side dye checks run on Zack, restricted to head-weighted vertices.
- All eight editor checks passed in the previous presentation run. They will run
  again with the release gate. Fourteen gating source audits passed; the informational
  recording audit still flags seven existing audio files, not the two new cues.
- Eighteen real hero casts were accepted through their actual input path, including
  Magnet's loose-shoe prerequisite. [Coverage](hero-coverage.csv).

The bot sweep inherited eight rounds from saved Hero Strike rules when switching
only Mode to Classic. The probe now pins the whole default ruleset and prints the
actual round count and duration. The earlier 133-144 defence ticks per seat describe
two defending rounds, not a demonstrated scoring-clock defect. No scoring was retuned.
The [live clock trace](round-clock.csv) and [measurement](clock-query-report.txt)
record the new observation. A 10,000-call slipper query sample in the 2,032-component
arena measured 0.0568 ms/call on this editor. The allocation counter returned zero;
its support is unverified, so this is not a zero-allocation claim. Existing network
seat and slipper caches remain; no speculative registry was added.

## Review media

The videos preserve the recorded wall-clock intervals without interpolating motion.
They are silent automated captures, not human playtest approval or a multiplayer
latency test. Each cast segment is uncut; the combined reels join at cast boundaries.
Order: Sean, Zack, Dante, Cheska, Nemu, Phaister; skills 1, 2, ultimate per hero.

- [All body casts](hero-casts-observer-v5.mp4), [all first-person casts](hero-casts-owner-v5.mp4).
- [Carrying body](carry-grounded-v5.mp4), [carrying first person](carry-grounded-owner-v5.mp4).
- [Full cast turnarounds](cast-model-finish-v2.png), [baseline cast](cast-model-baseline-v1.png).
- [Comparable eye-height maps](maps-eye-height-v5.jpg).
- Ordinary play: [Eskinita](Eskinita-ordinary-v5.mp4), [Bayan](BayanPlaza-ordinary-v5.mp4), [Ilalim](IlalimNgTulay-ordinary-v5.mp4).

The v5 observer recorder suppresses private first-person meshes only during its own
render and restores the actor's world shoe. Earlier v1/v2 carry evidence did not show
the held world shoe; v4 hero observer frames included private arms in world space.
Those earlier artifacts are not evidence of final attachment quality.

Full isolated PlayMode qualification twice, the exact Windows executable, network
owner/observer exercise and player performance measurements are still pending.

The standalone results reader/layout rerun passed 3/3. Its old local cleanup could
unload InitTestScene and leave the editor running without XML; it now uses the shared
runner-preserving reset. One duplicate exact protocol assertion was removed, retaining
ChatAndLobbyChromeTests as its compiled owner. The glyph importer's automatically
serialized Android entry is retained with `overridden: 0`; it changes no platform
setting and prevents every fresh Unity import from dirtying the candidate again.
