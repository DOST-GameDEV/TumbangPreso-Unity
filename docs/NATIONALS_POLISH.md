# Nationals polish: finish, polish, differentiate, playtest

## 0. MISSION

The current strategic roadmap for game feel, presentation and Nationals polish.
The systems-expansion era has supplied enough machinery. The next win is making a
player remember the moment they ran back for a slipper with the taya closing in.
**Finish, polish, differentiate, playtest.** Both Classic and Hero Strike ship;
Classic remains the tournament ruleset unless the human changes that decision.

This file decides **what matters next and why**. It is not another execution queue.
When an increment is selected, engineering belongs in [TODO.md](TODO.md), animation
and Blender work in [ASTRA.md](../ASTRA.md), and human judgments in
[Attention.md](../Attention.md). Reuse the existing entry where one exists. Do not
copy this whole roadmap into those queues or track completion here. ASTRA and
CLAUDE below are ownership lanes, not instructions to contact another conversation.

UI/HUD design is outside this roadmap. So are new modes, heroes, abilities, meters,
controls and progression. [FUTURE.md](FUTURE.md) is the retired systems-era roadmap;
[INSPIRATION.md](INSPIRATION.md) remains research and reasoning, not a next-work order.
[CLAUDE.md](../CLAUDE.md) governs repository work and [VISION.md](VISION.md) governs
the product. Existing balance and authority contracts stay intact.

**Evidence boundary, 2026-09-08, source `7612a04c8a1a`:** this is a targeted
source/asset-reference and existing-report evaluation, not a fresh playtest. No
Unity or Blender run, new motion capture, or listening approval was performed.
Older local lineup and Ilalim captures were inspected as historical context only;
they predate the latest art, map and animation work and cannot certify today's look.
Below, **confirmed** means visible in current code or serialized references;
**recorded** means an earlier observation; **judge in play** is a proposed quality
test, not a defect claimed from a screenshot nobody took.

## 1. CURRENT STATE EVALUATION

### ALREADY STRONG

- **The retrieval premise is specific and worth protecting.** A safe throw creates
  an exposed possession, then a dangerous return. Classic does not need a power
  system to produce a memorable play. Street Hype already recognizes skill without
  changing the score.
- **The feedback infrastructure is substantial.** `MatchFlair` distributes world
  presentation; `NetCue` carries sound; `HitFeel` supplies victim-specific camera
  holds and directional impact; `Hitstop` supplies bounded shared punctuation.
  Camera response, throw anticipation, block/bank/near-miss recognition, replay
  interest and ultimate introductions already exist. Protect their authority and
  deduplication rather than replacing the stack.
- **Animation is authored, not absent.** All eighteen hero actions are on their
  respective rigs and both custom rigs; all twenty shipped character entries carry
  `slide`. [Roster import evidence](reports/roster-clip-import-v5.json) resolves the
  serialized clips. The six motion identities in ASTRA.md are meaningfully different
  in their pose design. This proves coverage and import, not excellence in motion.
- **There is already a Filipino world to finish.** Lata label art, sourced tsinelas,
  sari-sari dressing, pisonet, pares, the LRT and the intact sourced jeepney are
  concrete identities. Wholesale asset replacement would discard that investment.

### GOOD FOUNDATION, WEAK PRESENTATION

The clearest confirmed mismatch is **retrieval's body versus its owner's view**:
`ViewmodelArms.PlayAction("slide")` still chooses `LungeClip`. The body now reaches
low; the first-person player gets a combat dash gesture. The sound also blurs the
choice: slide requests `dash`, and shove's `bump_swing` aliases that same recording.

Hero VFX have a mixed finish, not six missing families. TODO § 131.3 records sourced
formation/impact work in five families; § 131.6 records unfinished compositions.
Current `HeroHazards.CreateThunderstrike` still builds a flat, saturated
`ThunderShockRing` star under the drawn lightning. Shape variety exists, but the
largest remaining flat shape can dominate the more authored parts.

The audio listener and remote wind-up/slide cues were fixed in TODO § 151. The
remaining question is whether those spatial cues are useful under the music and
abilities, especially across the court. That is an ear test, not another listener
implementation. Sourced recordings remain provisional until heard in play.

### BIGGEST QUALITY GAPS

1. **The signature retrieval is not yet a coherent action across body, hand and
   sound.** This is more damaging than a background prop lacking detail.
2. **Animation approval has stopped at structural evidence.** TODO § 151.16 records
   frame-zero-only character probes; § 151.20 records a clip-count comparison that
   now rejects valid rigs. A good import cannot tell us whether a hand clips, a
   transition snaps, or the recovery reads as commitment. Those remain unverified.
3. **The finish varies between layers.** Authored casts can sit beside unfinished
   VFX shapes and reused sound meanings. One coherent hero beats six more effects.
   The map equivalent is scenery that looks specific but reacts with a generic
   combat sound: `StreetParesInteractive` currently uses `slipper_bounce` and an
   `ImpactBurst` when a character touches it.

### POTENTIAL SIGNATURE STRENGTHS

An unmistakable tin knock followed by a desperate low retrieval and a narrowly
missed tag; a curved bank whose path is readable to someone watching behind the
player; a hero power that opens a retrieval opportunity rather than eclipsing the
street game; and a neighborhood that responds briefly, then lets the chase breathe.
Those are stronger reasons to remember this entry than its account or ranked stack.

## 2. POLISH NORTH STAR

- **The slipper is the promise; retrieval is the payoff.** Keep the shoe, taya and
  escape route readable from release through recovery.
- **Commitment has a beginning and an aftermath.** The reach, tag and shove must
  disclose what a body is doing before effects announce its consequence.
- **The lata has the clearest sound in the street.** Its first knockdown deserves
  more presentation priority than ordinary impacts or incidental scenery.
- **Each hero has a verb-shaped silhouette.** Sean drives, Zack snaps and carves,
  Dante plants, Cheska draws and holds, Nemu drifts and pulls, Phaister performs.
  More authored, not more noisy.
- **The neighborhood frames the action.** Spend cultural detail at landmarks and
  roadside edges; preserve quiet ground around a loose slipper.
- **Another player must understand the same event.** Local polish that disappears
  for the defender, joiner or spectator is unfinished polish.

## 3. CORE MOMENTS

These are quality bars for existing actions, not orders to add a new effect to each.
The throw/lata/retrieval/chase sequence gets the majority of effort.

### Throw, consequence, exposed slipper

**Charge:** body and viewmodel wind-up plus the relayed `throw_charge` already
provide anticipation. Excellent means power builds visibly in the held shoe and
shoulder without constant camera vibration; the taya can hear preparation while
looking elsewhere. Judge short legal charge, long hold and release while moving.

**Release and flight:** `Carrier` already plays the throw action, varied release
cue and replicated flair. The hand should let go exactly when the world slipper
leaves, with one crisp release and a readable spinning/curving silhouette. Judge
the transition into its bounce and rest across a light and dark road patch. Do not
alter flight, collision or aim to repair presentation.

**Lata knockdown:** the existing replicated 45 ms freeze and directional camera
punch are the right foundation (`Lata.AnnounceUprightChange`). Preserve the
human-preferred `lata_impact` and `lata_knockdown` recordings. Make the initial tin
attack, tipping motion and settling tail read as one consequence; clear transient
effects promptly so the loose shoe becomes the next focal point. A distant observer
should see the opening without receiving the nearby player's camera violence.
The current camera punch passes strength 0.8 without distance attenuation; judge
that distant-view cost before making the impact stronger.

**Lata reset:** channel-start/completion cues, grab motion and restoration already
exist. A defender's effort must end on a clear upright-can beat, distinct from
knocking it down. Judge cancellation and sabotage too: the final sound must not
promise safety before the authoritative reset completes. No cinematic pause here.

**Bank shot, near miss, block/deflect:** `Slipper` and `MatchFlair` already recognize
these separately. A bank should show contact then redirected flight, rather than
only announcing the trick afterwards. A near miss needs a small passing/bounce
read that never sounds like a successful tin hit. A block should show the defender
meeting the shoe and its changed path; protect existing burst, flash and body
squash. Keep all three shorter and quieter than a knockdown. Evaluate the actual
trajectory and sound, with no new labels or Street Hype redesign.

### Enter, commit, escape or get caught

**Approaching the exposed slipper:** the tension should move from the throw to the
gap between shoe and taya. Existing landed-shoe presentation and approaching
footsteps need to survive road texture, shade and skill overlap. Judge from normal
eye height; an overhead screenshot cannot establish retrieval readability. Do not
add an omniscient chase cue that reveals an unseen opponent.

**Normal pickup:** keep it a valid, calmer choice. The existing grab, pickup cue and
carry attachment should communicate possession once, without a celebratory freeze
or repeated sound. The hand/shoe join must not look like a teleport through the arm.

**Committed retrieval slide:** anticipation is a deliberate low entry; action is a
reach toward this slipper; impact is the actual pickup if it succeeds; audio is a
scrape then catch; recovery is a vulnerable rise. A failed slide still has the skid
and recovery, but no catch confirmation. The authored 0.95 s body clip and its
0.14/0.25/0.342 s contact samples are the review starting points, not new gameplay
timers. Match the first-person arm to that commitment, not the lunge. Keep camera
motion low and brief enough to track the taya throughout.

**Taya chase, lunge and punch:** existing charge poses and differentiated melee
actions should make pursuit feel intentional. The stationary punch needs a close
arm-led tag read; the lunge needs body-led commitment and a visibly spent recovery
when it misses. Judge foot planting through sprint/turn/action blends and whether
approaching steps communicate distance before the defender enters view. A bot
deciding to chase is not evidence that its animation sells aggression.

**Tag and tiny escape:** tag already has burst, freeze, flash, camera response and
voice through `MatchFlair`. Let contact win that instant; do not prolong punishment
with extra holds. A tiny escape should be readable from the missed reach and the
attacker's continuing momentum, followed by space in the mix. Do not invent a new
escape detector or reward system to celebrate it.

**Shove, trip and recovery:** shove's outward arm motion exists and must remain
different from punch and slide. Match the push, displaced body and sound; judge
hit versus miss. Trips remain relevant in supported contexts, but TODO § 151.6
already measured Ilalim's live trip hazard outside the competitive box. Do not add
more hazards. Downed poses and get-up should disclose loss and return of control,
without lengthening the existing mash/recovery rules for animation.

**Street Hype:** protect the existing recognition of banks, curves, close calls
and blocks. Polish the action that earns the reaction, not its display. Routine
touches of a roadside prop must not borrow the sound of a scored event.

### Hero Strike and the match's ending

**Skill 1, skill 2, ultimate:** review a hero's complete three-cast sequence in
order. The normal skill needs an obvious direction or footprint; the second must
show a different job, such as Sean chambering a future throw rather than firing an
immediate blast. The ultimate earns the strongest silhouette and one dominant
payoff. Pair the body, existing first-person action, effect onset, impact sound and
return to locomotion. Judge with effects hidden first, then in a contested retrieval.

**Round transition:** existing countdown, voice and music transitions should clear
the previous round's visual/audio residue and make the new taya's presence apparent
in the world. Preserve the requested clean music cuts; do not introduce fades.

**Final victory:** `MatchResult` already has result-audio handling, so do not re-add
a jingle. Judge whether the winner's body and final sound actually conclude the
match. `CharacterAnimator` still maps the emote relabelled victory to `crouch`;
that is a concrete placeholder, but not proof the result sequence calls it. Trace
the existing ending before selecting one character's celebration pose. Keep this
below retrieval and hero coherence, and leave the results UI alone.

## 4. VISUAL / ANIMATION / CHARACTER PASS

**Protect proportions and canonical faces.** The small voxel cast, large readable
heads and specific hair/clothing are an identity, not a reason to replace rigs with
a realistic library. Preserve fixed skin tones, faces and silhouettes. Inspect seam,
accessory and material consistency in motion before adding detail that disappears
at match distance. Keep the existing Generic rig setup and authored action names.

The highest-value art review is **Sean's retrieval slide through entry, contact and
recovery**. ASTRA task 3 already corrected his short-arm reach with torso roll; an
angle shared across rigs was not a shared reach. Verify that correction in the game
before reauthoring it. Separately, `character-female-a` has a recorded accessory/
sleeve floor-contact ambiguity; judge that rig alone rather than reopening twenty.

Then approve **one hero's three casts per session**, starting with Sean as the
complete presentation reference. Preserve the authored distinctions: Zack's lateral
carve and aimed call must not become Sean's thrust; Dante's grounded widened stance
must not become another leap; Cheska's exact stop must not become a flourish; Nemu's
weightless drift must not gain a heavy stomp; Phaister's flourish must not become
Cheska's shortest-path gesture. Extreme angular-speed measurements are not a polish
score. Watch full-speed silhouettes, hands through torsos, foot clearance and blends.

Locomotion, throw, grab, shove, lunge, punch and reactions already have action paths.
Review transitions around the selected move, including interruption and remote
playback, rather than commission a wholesale locomotion set. Hero first-person
actions also already exist in `ViewmodelArms`; ASTRA task 6 asks for alignment, not
eighteen missing arm clips.

Keep new ultimate cinematic authoring out of the first batch. Existing introductions
are present, while ASTRA task 8 records no agreed cinematic action hook. First make
an existing ultimate cast exceptional. Do not stretch cast timing or commission six
unreachable intro clips to fill that architectural ambiguity.

## 5. VFX / CAMERA / GAME-FEEL PASS

Use the existing stack. Shared `Hitstop` is bounded to 20-80 ms and ignores overlap;
`HitFeel` holds only the victim's view, while the world keeps moving. They solve
different problems. Do not globalize ordinary hits or add caster feedback that
reveals offscreen victims. Existing camera holds were repaired for drift in § 150;
judge repeated impacts at the end of a chase before increasing any strength.

The first isolated VFX target is **Zack's ThunderShockRing**: retain the lightning
star's directionality, break up the flat filled appearance, and allow the existing
bolt/contact to lead. Keep gameplay radius and the sourced bolt intact. Later,
select one unfinished composition from § 131.6: Carapace's body plates, Barricade's
shard silhouette, or Sean's ultimate impact. An unused sprite sheet is an ingredient,
not an automatic replacement order.

Phaister's current ward already conforms to the sidewalk. A flat sourced quad was
rejected for losing that property; do not retry it. Nemu's actual ultimate uses
`SpawnKuroUnbound`, not the old `SpawnSeanceVoid` still reached by a showcase path.
Judge the live ability rather than polish a probe-only effect.

Follow VISION § 2: normal skill radius guidance is **1.6-2.3 m**; trails are governed
by their live cap, not one disc. The older sourcing document's radius range is not
the current bar. Preserve authoritative per-ability geometry; compare to the
[commit-stamped footprint report](reports/ability-footprint-8327a1ec7671.md) and
refresh it only when that geometry changes. Keep the **12% white-frame ceiling**,
but also demand visible lata, chalk, players and loose slippers in overlapping
effects. Passing a luminance bound alone does not establish readability.

TODO § 151.8 already measured maximum-effects load and found no accumulating leak.
Its warm cost is a diagnostic reading, not target-device certification. Do not start
a pooling rewrite or simplify sourced models without a new measured player problem.

## 6. AUDIO PASS

**First, distinguish decisions.** Audition a dry retrieval scrape/catch against the
current shove sound in a contested pickup. Pitch variation already exists and does
not make one recording mean two actions. Keep the preferred can recordings as the
reference against which throw, flight/bounce, block, tag and reset are mixed.

**Second, make the corrected space useful.** `AudioDirector` now follows the active
camera; world voices use a 2-32 m linear rolloff. Attention § 18 records the far
diagonal concern and the difference between first-person and third-person ears.
Listen to approach, behind-camera landing, sprint and slide from both sides of the
same play, including a joiner. Tune one cue family only if it becomes inaudible or
misleading. Do not compensate for a sound nobody has listened to by raising all SFX.

**Third, establish hero and environment hierarchy.** The 42 elemental replacements
and remaining earlier sourced cues are provisional (Asset_Sourcing § 5.5), not
blanket-approved. One hero audition should distinguish cast preparation, skill hit,
ultimate and tail without simply getting louder at each step. Pisonet's score-sting
misuse was fixed; its pitched click still needs a coin-sound judgment. Pares should
not impersonate a bouncing slipper. These are specific environmental sound choices,
not a new ambience system.

Music already has a continuous late-round pressure lift and announcement ducking.
The two delivered beds do not become adaptive scoring merely by adding a new track.
Judge a full ending with the OST active: approaching feet and tin must survive the
lift, the final victory must register, and the deliberate cuts must be clean.
Quiet between events is useful. Reject constant voice chatter or global chase loops.
Human-recorded tsinelas, neighborhood sounds and Tagalog exertions can be a later
identity pass, one source family at a time (Attention § 9).

## 7. MAPS / ENVIRONMENT PASS

Read [Art_Direction.md](Art_Direction.md) before choosing new environment colors:
offense orange and defense blue identify roles, not decoration. Preserve authored
assets under CLAUDE § 6.0, including the jeepney's original materials and livery;
do not recolor or decimate it to enforce a generic palette. Some old art/map prose
still describes Godot-era geometry. Current Unity scenes, builders and later fixes
decide what exists.

### ESKINITA

**Strength:** the intimate sari-sari, sampay and kanal street is the best immediate
frame for Classic's personal chase. Its narrow lateral space makes the two long
approaches important. **Recorded roughness already addressed:** § 134.14 grounded
cars and extended the geometry gate; do not list floating cars as still broken.

**Next opportunity:** one roadside composition pass beside the nearest existing
sari-sari frontage. Judge the contrast between richly dressed edge and quiet
retrieval ground, facade/material repetition, roof/cable silhouettes and whether
the ambient bed identifies a neighborhood. Hero Strike needs a crowded-lane frame
here, because an acceptable effect in open space can consume this narrow view.
Do not widen the map as a polish fix. Done is one coherent landmark visible from
the first throw position without obscuring a shoe or taya; a reaction is optional
and should be omitted unless it improves that actual view.

### BAYAN PLAZA

**Strength:** the open barangay square, monument, church/basketball vocabulary and
trees can provide a calmer, more public counterpoint to Eskinita. **Correction:**
`BayanPlazaMonumentFix` already removed the blocking intrusion into the defender's
box; the older map document is history on that point.

**Next opportunity:** one monument-side approach. The fix deliberately leaves a
small walkable visual overlap rather than move the whole arranged corner. Judge
that seam at eye height, plus whether the landmark anchors the broad ground or
leaves it visually anonymous. Inspect foreground paving, tree shade and ambience
before filling empty space with props. Preserve collision and the open floor. Done
is an unmistakable plaza view and an understandable monument edge with no new
obstacle or retrieval occlusion. Do not replan the entire square from an old defect.

### ILALIM NG TULAY

**Strength:** the carriageway-as-box, overhead guideway, PC Express, pisonet/pares
edge and jeepney provide the richest local specificity. Train motion and a moving
field-recording source already exist. `LrtTrainFlyby` now defaults to a rare
300-second interval; do not restore the old document's 24-second rhythm or retune
its gameplay-linked pass while adding ambience.

**First concern is retrieval continuity.** The
[six-seed report](reports/bot-sweep-2fde55d32246.md), on its recorded commit, has
0-48 idle penalties in Hero Strike on Ilalim, with almost all the dead time in one
seed. That is a location to investigate, not proof of an unreachable slipper or
broken map. Trace that recorded outlier before a broad prop pass or balance change.

**Presentation opportunity:** one pisonet/pares frontage audition after the route
check. It already reacts; the question is whether the sounds and bursts make it
feel like a food/computer street or another fight. Judge shop light and signage
against the bridge shadow, material consistency at the asphalt/pavement boundary,
and environmental noise during an ultimate. The jeepney's metallic factors and
reflection probe were repaired in § 151.5; approve the finish visually before
changing lighting again. Done is one legible, recognizable roadside zone whose
reactions leave room for footsteps, can and slipper. No new hazards or rules.

## 8. NATIONALS PRIORITY TIERS

### S TIER

**Retrieval coherence and the first lata payoff.** Huge player/judge impact; narrow
art, hand-animation and cue work on existing hooks; moderate integration risk.
The small motion-review tool is a prerequisite that earns its cost only by answering
these moves. A playable reference sequence outranks more isolated screenshots.

### A TIER

**One complete hero, one unfinished dominant effect, and one real map problem at a
time.** Approve Sean's three casts and their existing arm/VFX timing; finish Zack's
flat shock star; locate Ilalim's retrieval outlier. Then choose one map-side pass
from § 7 or the `character-female-a` slide contact review. High visible return,
bounded scope; stop a map or animation pass if it starts requiring balance changes.

### B TIER

**Finishing the supporting beats.** One character's locomotion/action seam, one
block/bank contact pass, one existing ending/celebration, or one pisonet/pares audio
pair. Useful once the central sequence works; lower impact than fixing the player's
own retrieval. Evaluate a concrete captured problem before opening an asset task.

### C TIER

**Small decorative rewards.** One background motion accent, one nonessential prop
material seam, or one human-recorded neighborhood accent. Low gameplay return and
easy to overdo. Only pursue after the main views and sound hierarchy pass; never
turn the whole street into constantly moving effects.

### DO NOT DO

No new systems by default; no UI tasks; no global hitstop on every hit; no larger
skill footprints, stacked floor planes or white flashes to simulate quality; no
automatic sourcing sweep; no reauthoring all rigs; no six-hero cinematic project
before its hook and benefit are agreed; no extra traffic or hazards that change
decisions; no reviving old FUTURE/INSPIRATION prompts as the default work order.
Do not retune slide recovery from bot usage alone or repeat a broad probe suite
that already answers the question.

### SPECULATIVE - HUMAN APPROVAL REQUIRED

No new gameplay idea is recommended by this evaluation. A geometry change that
creates new bank routes, a recurring environmental bonus, or a longer shared
ultimate freeze would change play and needs a separate human decision. None is
part of the polish order above.

## 9. NATIONALS QUALITY BAR

The **first 30 seconds in the arena** should establish a recognizable street,
grounded moving characters and a readable can. The **first knockdown** should make
the player react and immediately spot the retrieval opening. The **first chase**
should make both ordinary pickup and risky slide understandable. The **first hero
skill** should disclose its job and caster; the **first ultimate** should supply
one signature silhouette and consequence without hiding the street game.

Use one full Classic match and one Hero Strike match as the final cohesion check,
with at least one other person watching from the player's shoulder or the existing
spectator view. Ask them to explain a bank, a failed lunge and a retrieval escape
without coaching. Ask afterwards what they remember besides the powers. If the
answer is not a particular street-game moment, the next batch still belongs in S
tier. These are proposed acceptance observations, not playtests completed today.

Use existing replay/highlight evidence where it captures the needed event. Record
the build commit, mode, map and viewpoint with judgments; approvals belong in
Attention.md, not another report stream here. Mechanical passes do not substitute
for the human's timing, taste and listening approval.

## FIRST POLISH BATCH

Ordered recommendations, not a parallel work assignment. Select one available
increment per session, recheck its queue entry, and stop at its stopping condition.
If a human decision is pending, choose an independent item rather than invent its
answer. Ask before credit-consuming external work; never perform a usage reset.

1. **HUMAN: establish the retrieval reference.**
   **Problem:** the corrected spatial mix and authored slide have not been felt.
   **Why:** they decide whether this game has its promised tension.
   **Scope:** one Classic round on Eskinita with ordinary pickups, successful slides
   and a deliberately failed slide; hear shove and slide from the defender's side.
   **Done:** name the confusing beat, or explicitly approve the existing choice and
   recovery. **Verify:** a short captured sequence with build/viewpoint and the
   human's judgment. Use Attention § 17.2 and § 18; no balance edits in this increment.

2. **CLAUDE: make one moving slide reviewable.**
   **Problem:** frame-zero probes cannot approve animation, and clip-count equality
   rejects valid added actions. **Why:** art revisions need trustworthy pictures.
   **Scope:** TODO § 151.16's motion-strip support, including the slide contact
   samples, plus § 151.20's relevant base-clip preservation assertion.
   **Done:** Sean's slide shows entry, contacts and recovery through the game shader
   and camera; extra clips pass while a missing required base clip fails.
   **Verify:** one versioned strip and the focused assertion. No general capture
   framework or roster-wide probe campaign.

3. **ASTRA: approve and, only where needed, refine Sean's retrieval slide.**
   **Problem:** short-arm reach was corrected numerically, not judged in motion.
   **Why:** this is the signature action on the rig with the clearest known reach
   challenge. **Scope:** Sean's existing `slide` entry/contact/recovery only, under
   ASTRA task 3; preserve duration, rig and gameplay values.
   **Done:** a low skid, readable reach and vulnerable rise with no visible floor
   penetration or abrupt return; keep the clip if it already passes.
   **Verify:** increment 2's strip plus full-speed player/observer motion. If an
   export changes, rebuild roster references and verify the resolved clip.

4. **CLAUDE: give the existing first-person slide its own reaching motion.**
   **Problem:** `slide` selects `LungeClip`. **Why:** the owner currently sees the
   wrong decision. **Scope:** ASTRA engineering context task 4, one arm action and
   its blend into carry/recovery; keep movement and existing network verb intact.
   **Done:** low reach and recovery agree with the body; failed pickup has no false
   catch; lunge remains distinct. **Verify:** successful and failed slides in FPP,
   with remote body comparison and the action path exercised on keyboard, pad and
   touch. No input or UI redesign.

5. **SHARED: separate slide and shove by ear.**
   **Problem:** `dash` and `bump_swing` resolve to one recording. **Why:** the taya
   needs to hear whether an attacker is retrieving or pushing.
   **Scope:** audition one slide scrape/catch treatment using existing sources or
   a human recording, then integrate only the approved cue distinction. Human
   chooses sound; CLAUDE handles alias, timing and relay.
   **Done:** the two decisions are distinguishable without drowning footsteps or
   implying pickup on a miss. **Verify:** blind alternation, then a contested pickup
   heard once on host and joiner. Route judgment to Attention § 18.1 and selected
   implementation to TODO; do not bulk-regenerate audio.

6. **SHARED: make the first lata knockdown the reference payoff.**
   **Problem:** repaired distributed feedback has no current cohesive feel approval.
   **Why:** this is the first spectator reaction and the chase's starting gun.
   **Scope:** one knockdown sequence, align existing tin attack/tip/settle and nearby
   camera punctuation; preserve the preferred recordings and replicated event.
   **Done:** one unmistakable hit, no doubled cue, then a clear retrieval opening.
   **Verify:** nearby thrower, defender and distant observer hear/see the same
   consequence; compare against unchanged feedback and retain it if already better.
   Human approves, CLAUDE changes only a demonstrated mismatch (TODO § 151.3 context).

7. **ASTRA: review Sean's three existing casts as one motion language.**
   **Problem:** imported actions and deformation metrics do not establish readability.
   **Why:** one approved hero supplies the quality reference for later hero sessions.
   **Scope:** ASTRA § 1A, Flame Rush, Ignition Cannon and Supernova only; judge coil,
   extension, stop and recovery within their existing timings.
   **Done:** rush, chambering and vertical slam read differently without VFX, and the
   ultimate feels strongest without enlarging its effect.
   **Verify:** versioned strips and full-speed gameplay after increment 2; compare
   existing FPP actions and record any engineering mismatch instead of editing it.

8. **CLAUDE: finish Zack's existing ThunderShockRing composition.**
   **Problem:** the flat saturated star competes with authored lightning (§ 131.6).
   **Why:** one dominant shape can cheapen the first ultimate despite good casts.
   **Scope:** the ground shock's visual shape/value/decay only; retain the star
   identity, bolt, gameplay footprint and all cast timing.
   **Done:** the bolt/contact leads, the ground opens visually between branches,
   and the lata and escape route remain visible.
   **Verify:** current live ultimate in a solo and overlapping-effects view on
   Eskinita, the white-frame gate, and human before/after judgment. Route to the
   existing TODO § 131.6 entry; no new VFX framework.
