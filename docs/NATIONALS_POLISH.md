# Nationals polish: the current implementation design

## Visual communication and appeal pass (VISUAL-1), 2026-09-23

Owner direction, 2026-09-23: make TUMP more visually appealing and satisfying to play
without making it more realistic, make the in-game HUD minimalist, professional and easy
to look at, and explore on-screen effects and indicators that work together and are not
distracting (Sepak U was named as the example). The status queue is
[TODO VISUAL-1](TODO.md#current-implementation-queue). This section owns the design.

This supersedes the 2026-09-01 "do not touch the in-match HUD" scope note for this pass.
It does not reopen the painted front end, and it changes no rule, timing or network
contract.

### V0. Why: what the current captures show

Sources: `reports/full-backlog-2026-09-21/map-evidence/*-native-v37.png`,
`lagoon-deck-live-v3-baseline.png`, `accessibility-evidence/accessibility-hud-large-1280x960.png`,
`black-ui-outline-evidence/spectator-manual-flight.png`,
`close-feedback-2026-09-16/v32/*` and `direct-gameplay-2026-09-16/v28/*`. Re-verify each on
the current build before fixing it.

1. **The rule that defines the game has no picture.** `Design.md` § 0: the tension is the
   retrieval. An attacker is taggable while inside the box, holding their slipper, with
   the lata upright (`CharacterMotor.IsTaggable` plus `Lata.IsUpright`). Today that state
   is one orange text line at the bottom prompt ("You can be tagged",
   `TumpMatchReadout.Prompts`) plus a 0.36 s edge flash at onset (`TumpHudEffects`). The
   chalk does not change, the world does not change, and the state flips without the
   attacker doing anything when the taya finishes a restore.
2. **The taya cannot see who is catchable.** Nothing marks a taggable attacker for the
   taya. `CharacterNameplate` colours rings and tags by seat and shapes the taya's ring;
   it carries no vulnerability state. The taya's only scoring verb has no target cue at
   8 to 12 m, where chibi hands are a few pixels.
3. **Timers are sentences.** Restore progress (taya only, a bar under the prompt), can
   protection ("Protected · 1.1s"), throw and tag cooldowns ("THROW CD · 1.0s",
   "TAG CD · 0.7s"), the fetch penalty ("Fetch your slipper · -5 / second"), slipper
   return ("Slipper returning · 2.0s"), charge and pektus ("Release to throw",
   "Pektus left · 34%"). VISION § 3 says the match HUD carries no sentences.
4. **Some states are said six times and others never.** `Lata.BuildDownBeacon` records
   that "lata down" fires as a world popup, centre alert, card title, objective line,
   toast and crosshair. Can state is also drawn twice on the HUD ("Can upright" in
   `TumpMatchReadout`, "CAN PROTECTED" in `OffscreenIndicators`). Meanwhile attackers get
   no picture of the restore countdown that decides their escape.
5. **Four colour systems compete.** Role (Offense orange, Defense blue), seat identity
   (`PlayerIdentity.Colour`), hero ability palettes and UI chrome all appear on the same
   frame with no stated hierarchy.
6. **Key objects lose against their backgrounds.** White chalk on Bayan's light paving is
   the least visible line on screen although it decides safety. The lata is a speck at
   distance and sometimes absent from spectator frames (VISION § 2 rule 5). A black
   slipper is the highest-contrast mass in FPP and a dark slipper on Eskinita asphalt is
   easy to lose.
7. **Flat light.** `Toon.shader` `LightingToon` gives `albedo x light x lerp(_ShadowBand
   0.45, 1, band)`, and the surface shader adds the scene ambient on top. Map trilight
   ambient is about 0.55 to 0.66 at sky and equator (the lagoon up to 0.80) against a sun
   of about 1.1 (`MapAtmosphereAuthor.Apply`, `LagoonBuilder`), so lit to shadow works out
   near 1.5 : 1 and the shadow side is the same hue slightly darker. Specular is zero on everything, including the
   metal lata. Rim is an unmasked view-angle lerp. Fog starts at 68 to 85 m on an arena
   about 40 m across, so there is no depth separation.
8. **The floor says nothing.** 40 to 50 percent of each FPP frame is an even low-contrast
   plane. Nothing on the ground leads the eye to the lata.
9. **The screen layer crowds the lower centre.** Arms and held slipper fill about 12
   percent of the frame with ink several times heavier than any character's, and the
   ability tiles and white key boxes sit on top of them. The HUD takes roughly 13 to 17
   percent of the frame in ordinary play, most of it four score slabs
   (`OwnerScoreStrip`, `Color32(35,29,33,210)`, reads cool grey over sky).
10. **Some effects sit outside the art style.** Confetti is scaled primitives on
    rigidbodies (`HeroHazards` near line 4048); `CanContactAccent` and the fire/zap
    slipper trails use `Sprites/Default`. v32 shows large same-size flat pink squares
    between camera and action; identify them before changing anything.
11. **The characters are the best-looking asset** (`rafi-block-hair-evidence/hero-lineup-quarter.png`)
    and are small in play. Everything else should frame them, the lata and the tsinelas.

### V1. The information model

What each player needs to know, what answers it today, and which task closes the gap.

| Question | Rule source | Today | Gap | Task |
|---|---|---|---|---|
| Am I safe? (attacker) | inside box + holding own slipper + lata upright | orange prompt text, 0.36 s onset flash | no world or peripheral state; flips on the taya's restore | 1.1 |
| Where is the nearest safety? (attacker) | box edge at `ConfinementRadius` 7.0, square | static chalk | the exit is never shown | 1.1 |
| Who can I tag, and are they in reach? (taya) | same rule on other bodies; punch 1.7 m / 75 deg, lunge 1.3 m | nothing | no target cue | 1.2 |
| When does danger come back? (attacker) | restore channel 1.36 to 1.67 s, then 1.25 s protection | taya-only bar; scoreboard word "Resetting can" | attackers cannot time an escape | 1.2 |
| Where is my slipper, how long do I have? | own-slipper lock; fetch grace then -5/s; roof and lagoon return | owner glow, landed rim, recall mark; countdowns as text | penalty and return clocks are text | 1.3 |
| Can I throw, how hard, which curve? | holding, outside box, post-restore cooldown; charge 0.35 to 1 over 2.5 s; pektus | crosshair greys; dotted arc; bar and two text lines; cooldown text | four texts away from where the eye is | 1.6 |
| What is flying at the can? (taya, spectators) | 17.6 to 19.4 m/s slippers | fire and zap affinity trails only | ordinary throws are hard to read in FPP | 1.10 |
| Is the can up, down or protected? (all) | `Lata.IsUpright` gates four rules | on-object rim pulse and collar (good), plus six more copies | repetition instead of emphasis | 1.5 |
| Whose round is it? (all) | taya = `(round - 1) % 4` | "Round 1 / 8", chip word "Defender" | rotation has no ceremony | 1.13 |
| What is my opponent about to do? | windups, lunge | clip poses, unmeasured at distance | unknown at 8 to 12 m | 1.12 |
| Who is winning? (all) | cumulative score | four large slabs | heavy for what it says | 1.4 |

### V2. The signal language

Principles, each already argued somewhere in this repository and now applied everywhere:

1. **World first, HUD second, text last.** The can is the signal (`Lata.BuildDownBeacon`),
   the chalk is the truth (`Design.md` § 2). Sentences live behind the TAB hold and in
   training only (VISION § 3).
2. **One owner per signal.** A state has one canonical place plus at most one moment beat.
   Emphasis is contrast, not repetition.
3. **Timers live on the thing they time.** A restore ring on the can, a drain on the
   protection collar, a sweep on the reticle, a ring on your own slipper marker.
4. **Personal cues are per viewer; shared facts are shared.** The taya's target cue is the
   taya's; the restore ring is everybody's.
5. **Show the exit, not only the danger.**
6. **Change what exists before adding geometry.** The owner rejected a beacon on the can
   and neon rims; brighten, desaturate, outline or thicken existing objects first.
7. **Attention budget.** Per moment one dominant signal, at most two secondary, the rest
   ambient. Multiple necessary warnings may coexist; decoration yields.

Colour hierarchy. A colour from a higher band is never used as decoration in a lower one.

| Band | Colours | Carries |
|---|---|---|
| 1. Rule state | Offense `#f87020`, Defense `#0080e8` | role, the armed box, the taya's target cue, the taya badge |
| 2. Identity | `PlayerIdentity.Colour` per seat | rings, chip rings, own-slipper marker, flight streak |
| 3. Power | each hero's palette | ability VFX only |
| 4. Chrome | UI brand palette, black in-game outlines | HUD plates and type |
| 5. Structure | chalk white or charcoal, cream, ink | court, rules, neutral marks |

Shape meaning. Starburst: accepted contact only. Ring: a zone or a timer. Chevron:
off-screen location. Dotted: prediction. Streak: travel. Puff: ground contact. Chalk
stroke: rules and court.

Sepak U (Good Knight Collective, official itch.io screenshots): the whole persistent HUD
sits in one band; the ball keeps the brightest colour even during a super; spectacle is
drawn in the same ink language as the characters, sometimes in monochrome; the HUD hides
during a super; interstitials desaturate the world behind gold on black. Take the
principles, not the layout.

### V3. Implementation routes

Each route extends existing owners. No new UI framework, no new presentation framework.
Every new look lever is a global or per-map parameter whose "off" value reproduces
today's look, so the owner can reject a treatment without a code change.

**1.1 Danger made visible.**
- A runtime court boundary presentation built from `Balance.ConfinementRadius` and the
  lata mark height, drawn a few millimetres above the authored chalk with the Toon depth
  bias, so it cannot disagree with the rule on any map. States: lata down (chalk at rest),
  lata upright (chalk a step brighter, box "armed"), the restore moment (one short
  brightening sweep from the can outward, the only animated beat). No glow columns.
- Per viewer, while the local attacker is taggable: a thin low-alpha Defense-blue frame
  in the lower corners (not a full vignette), a subtle low audio layer through
  `AudioDirector`, and the nearest boundary segment brightened under that viewer only as
  the exit. Reduced motion keeps the frame static.
- Crossing out of the box with the slipper: a small chalk puff at the feet, a soft
  swish, the frame clears. The escape is the retrieval's win and should feel like one.
- Keep `TumpHudEffects`' onset flash; drop the prompt sentence once the cues read in
  greyscale and muted review.

**1.2 The taya's view and the shared restore clock.**
- Extends `TODO.md` § 127 (the taya is a ring, an attacker a disc). For the local taya
  only: taggable attackers get a Defense-blue rim through the existing `_RimColor` / `_RimStrength` property-block path (`CharacterVisual`), and their
  nameplate ring switches to a "catchable" shape. In punch or lunge reach inside the arc,
  the reticle shows a ready tick. Spectators see a quieter version.
- A world restore ring on the lata's collar filling with `Carrier.ChannelRatio`, visible
  to everyone; attackers read it as the countdown to danger. After restore, the
  protection collar drains with `Lata.ProtectionLeft`. Replay records both.

**1.3 Timers on objects.** Own-slipper marker gains a ring for the fetch grace and the
penalty state (`TournamentRules.IsSlipperWarning`), and for roof and lagoon returns.
Edge chevron in the seat colour when your slipper is off-screen. No text in ordinary play.

**1.4 In-game HUD, minimalist and professional** (`TumpMatchReadout` and its partials).
- One centred top bar: two chips either side of the clock. Chip: portrait in its seat
  ring, score in tabular numerals, one state badge (in hand, away, retrieving, caught).
  Role as a badge: can badge in Defense blue for the taya, slipper badge in Offense orange
  for attackers. Local player: gold underline. Fixed seat order in the match; a small
  crown marks the leader. Rounds as pips or chalk tallies. Can state as one glyph under
  the clock. Replaces the score slabs and the corner can text. References: Splatoon's
  player-icon bar, Valorant's top bar.
- Bottom centre (Hero Strike): Q and E share one tile shape, F a notched ring (VISION § 3),
  keycap as a corner badge from the live binding, digits only in the last 3 s. Stamina as
  a slim arc above the cluster (Classic: the arc alone), shown while draining or
  refilling, 150 ms in and 400 ms out. Clear of the arms.
- Right edge: at most three pictogram feed lines, fading after about 4 s.
- Remove from ordinary play: "Slipper in hand", "Guard the can", "Can upright", the
  permanent TAB hint (first round or idle only), centre warm-up sentences (one short lower
  caption during warm-up). No sandbox or version strings in an ordinary match.
- Type follows `TODO.md` § 133 and `FONT_USAGE.md`. One plate style (none, or warm ink `#1C0F06` at 55 to 70 percent in the owner's
  chamfer), black outlines, Darumadrop for clock, names and moments, the supporting face
  for small labels, three sizes at most, one spacing unit, one icon stroke weight.
- Budget: permanent HUD under about 8 percent of the frame at 1920x1080, measured from
  canvas rects; the central 30 percent box holds only the reticle and transients. Check
  1920x1080, 1280x720 and the owner's short wide window, HUD scale 100 and 120, high
  contrast, keyboard, pad and touch prompts.
- "+100" appears at the source chip and merges; totals are correct immediately.

**1.5 One signal language across effects.** Write the meaning, shape, colour and place
table from V2 into code comments at each owner and make the existing indicators obey it.
Fold the off-screen can indicator into the chevron family without text. At most one
full-screen tint at a time with a priority across `DamageVignette`, `DownedVignette`,
`FrostVignette` and caught or moment grades. Non-critical HUD recedes to 0 to 30 percent
during an accepted ultimate and in replay. Cut the six "lata down" copies to the can,
the bar glyph and one moment beat. Experiments, each compared against today and kept only
if clearer: peripheral speed lines during sprint and dash (off in reduced motion); a brief
lower-saturation world grade during a shared ultimate while players, lata and slippers
keep full colour; sound visualisation as low-alpha edge pips for footsteps already
audible, for muted or deaf play, with the owner choosing the default.

**1.6 The reticle as the personal-state hub.** Replace the "+" text glyph with a drawn
reticle: charge ring (0.35 to 1), pektus direction tick, cooldown sweep, greyed when a
throw would be refused (existing rule), taya ready tick (1.2). The bottom prompt keeps
only rare verbs (get up, break free, reset).

**1.7 FPP viewmodel** (`ViewmodelArms`, `CameraRig`). Lower and slightly smaller rest
pose, lane right of the reticle clear; held slipper lit with its own rim; arms on the same
toon bands as the world (check no property block flattens them, `TODO_Archive.md` § 87);
ink weight matched on screen to a nearby character; a fixed viewmodel FOV if the arms
currently follow the 75 to 110 slider. Throw: cock back inside the existing charge
window, 2 to 3 frame release snap, follow-through past centre, settle; the apparent
release always matches the real launch from the sight line.

**1.8 Toon lighting.** Still two bands, still Built-in, still `Toon.shader`.
- Ambient down and tinted: aim for lit to shadow near 2 to 2.5 : 1 by lowering trilight
  intensity per map in `MapAtmosphereAuthor.Apply` rather than darkening the grade.
- Per-map shadow tint so shade shifts hue (violet grey at noon, warmer at dusk), low
  saturation, away from both role hues.
- Rim masked to the upper, away-from-light side (TF2), characters and hero props only,
  strength up to about 0.25.
- A hard one or two band highlight for metal: lata rims and end caps, never across the
  owner-drawn labels; optional gloss on tsinelas rubber.
- A subtle vertical gradient on characters (feet about 10 to 15 percent darker, gone by
  the chest) so the eye goes to hands, face and slipper.
- Ground-contact darkening by height above the local floor (within about 0.5 m) on
  environment; soft blob contact shadows under players, lata and dropped slippers. No SSAO.
- Fog toward the horizon colour starting nearer the arena edge so far buildings recede;
  the court stays crisp.
- Tune `_BandEdge` against band popping first. Surface and outline normals stay separate.
- Consider an afternoon sun on maps where it fits: tumbang preso is an after-school game,
  and a lower sun gives longer shadows (structure on the empty floor) and natural rim.

**1.9 Court and ground as the stage.** Per-map court medium chosen for contrast, the way
the game is really drawn: chalk on dark asphalt, charcoal (uling) or brick on light
paving. Chalk with slight wobble, broken edges and smudges; an obvious scuffed home
circle; large soft low-contrast floor variation (sun-bleach, wet patches, oil, drain
covers, road paint, tyre marks, leaves at the edges) with negative space around the
circle and a slight value lift toward the court centre. Decals only; no collision or
route change.

**1.10 Hero objects.** With 1.8 and 1.9 the lata must be findable in every reference
frame; when far or behind cover rely on the glyph and chevron, not a bigger mesh or a
permanent neon rim. A ground landing marker while the can is airborne after a knockdown,
driven by real state. A short ink flight streak on every thrown slipper in the thrower's
seat colour, visible to all, so the taya can read incoming throws.

**1.11 Effects in the ink language.** Sources and licences follow `TODO.md` § 131 and
`Asset_Sourcing.md`. Two-tone flat shapes with ink edges; 12 fps
flipbooks for graphic effects where it helps (`VfxFlipbook`), smooth trails; dissipation
by erosion, shrink or breaking into dots. Chalk dust from the circle on knockdown and from
skids on court lines; cartoon puffs for landings and slides. Replace confetti cubes with
fluttering paper shapes spawned outside the camera's central cone. Move
`CanContactAccent` and slipper trails off `Sprites/Default`, keeping the open centre and
the hit, grounded and restored distinction; profile before pooling. Each hero keeps a
distinct main shape; Dante's orbiting protectors stay.

**1.12 The exchange as one performance, and opponent readability.** Check the existing
PRESENTATION-1 sequence against this beat sheet and change only what differs. Knockdown:
existing 60 ms hitstop, metal transient aligned to contact, one ink starburst for 2 to 3
frames, can flash for one frame, topple with chalk dust, thrower's reticle tick and "+100"
at their chip, then own-slipper markers light and the restore state begins. Tag: contact
hitstop, victim desaturates (caught), taya confirm, victim returns. Block: thunk, small
push, a pictogram for the taya. Escape: 1.1's puff and swish. Misses, cancels, blocks and
failed pickups must look different from success. Check that a remote attacker's charge
pose and a taya's lunge windup read at 8 to 12 m; strengthen poses before adding markers,
and never show an opponent's aim line.

**1.13 Round rhythm.** Round start: the new taya's chip and badge swap with one clear
beat, the taya's name tag and ring change, the box arms. Round end: a short hold, scores
settle into the chips. Halftime keeps the compact popup over the court; chalk accents are
allowed on it. Match end keeps the result board.

**1.14 Environment appeal and life** (with PRESENTATION-1.5 and 152.4). Building base
darkening and top light from 1.8, window glass as a sky-reflection gradient instead of
flat dark, roof-edge highlights, distinct landmarks, phase-varied ambient motion, fiesta
banderitas overhead where a map suits them (never covering the lata in spectator views),
event-driven reactions to big plays. Lagoon stilt houses stay fixed; boats bob.

**1.15 Spectator, ultimates, audio.** Spectator framing with hysteresis, establish then
move close, show the consequence; complete ultimates for all seven heroes through the
shared phase without consuming Phaister's live warning; audio attack, body and tail per
event, one peak at a time across world, local, announcer and music; captions intact.

Per-change checks rather than separate tasks: choose simulation, unscaled or recorded
time for each new element and record what replay needs; no new timeScale writers; no
stranded freeze on teardown; moving-camera shimmer check for new chalk, decals, rim and
highlights; reduced settings keep source, ownership, danger, state changes and contact
acknowledgement; frame time and draw calls on the crowded reference frame for anything
screen-filling or per event.

### V4. Evidence and stopping rule

- Baseline once: an ordinary FPP exchange on Bayan Plaza, a crowded Hero Strike ultimate
  from spectator, a quiet establishing view, an Eskinita dark-asphalt frame with a dropped
  slipper, and a HUD frame at 1280x720, versioned `look-baseline-v1`. Reuse recent
  captures where the camera matches.
- Per item: implement, capture the same frames (`look-1.1-v1`, `v2`), a 25 percent
  greyscale thumbnail, decide, commit, next. At most three variants for a subjective call.
- Squint checks: at 25 percent size in greyscale, point to the lata, both slippers and all
  four players; in the "am I safe" frames, a stranger can tell safe from taggable muted.
- Tests only for behaviour that changed (HUD state bindings, indicator truth, replay
  recording of new elements). A passing test is not evidence of looking better.
- A fresh internal Windows player per batch, never the Desktop target. Owner taste
  approval is recorded separately and never blocks the next batch.

### V5. Coverage of the earlier VP-01 to VP-25 draft

VP-01 grammar (V2); VP-02 1.8, 1.9, 1.10, 1.5; VP-03 to 07 and 09 1.12; VP-08 1.10, 1.11,
1.12; VP-10 and 11 1.11; VP-12 1.5; VP-13 1.8; VP-14 and 16 1.9, 1.14; VP-15, 22, 23 and 24
per-change checks; VP-17 1.7; VP-18 and 19 1.15; VP-20 1.15; VP-21 1.4; VP-25 V4.

### V6. References

Game references are observations of shipped games, not measured claims.

- Valve, *Illustrative Rendering in Team Fortress 2* (NPAR 2007): value and saturation
  hierarchy toward characters, rim from above, gradients, simplified backgrounds.
  https://steamcdn-a.akamaihd.net/apps/valve/2007/NPAR07_IllustrativeRenderingInTeamFortress2.pdf
- Sepak U, Good Knight Collective: https://teamgoodknight.itch.io/sepak-u and
  `reports/presentation-pass-2026-09-21/reference-research.md`.
- Splatoon (player-state top bar, the world as feedback); Valorant and Overwatch 2
  (minimal top bar, bottom-centre ability bar with integrated keycaps); Rocket League (the
  ball is always findable); Fall Guys (almost no persistent HUD, quick celebrations);
  Fortnite (optional sound visualisation); Crossy Road (voxel readability through contact
  darkening); Wind Waker and Hi-Fi Rush (graphic smoke, puffs and comic accents).
- Martin Jonasson and Petri Purho, *Juice it or lose it*: https://www.youtube.com/watch?v=Fy0aCDmgnxg
- Jan Willem Nijman, *The Art of Screenshake*: https://www.youtube.com/watch?v=AJdEqssNZ-U
- Dominic Kao, *The Effects of Juiciness in an Action RPG*, Entertainment Computing 34
  (2020): compare intensities; more is not always better. https://doi.org/10.1016/j.entcom.2020.100359
- Earlier technique references kept for the per-change checks: Guilty Gear Xrd GDC talk,
  Daniel Holden on springs and inertial easing, Riot VFX and VALORANT clarity articles,
  Unity 6 mipmaps, Xbox Accessibility Guidelines 117 and 118, Riot performance profiling.



## Current delivery design, 2026-09-21

The owner requested a thorough plan and documentation cleanup, then a major research-driven expansion using Sepak U and Blue Lock, with complementary spectacle and animation throughout play. This section is that design, grounded in the actual checkout. It is not a completion claim. The single execution order/status lives in [TODO](TODO.md#current-implementation-queue); the exact next action, revisions, owned jobs and blockers live in [the ledger](ACTIVE_REWORK_LEDGER.md). The [full brief](reports/presentation-pass-2026-09-21/implementation-brief.md) defines the intended experience. Ordinary creative/engineering decisions are already authorized. Improve this design when observation warrants it and record the reason.

### Research-driven presentation direction

The owner's latest direction is a major creative expansion: add or refine animation, VFX, SFX, cameras, UI and supporting reactions wherever they make play more expressive and easier to understand. Many things may happen together when they complement one another. **Do not interpret readability as a request for a quiet or minimal game.** Noticeable sports graphics, striking character performances and busy action are intentional. Preserve the simple rules and give their consequences more personality.

The [research receipt](reports/presentation-pass-2026-09-21/reference-research.md) distinguishes inspected footage, developer statements and our proposals. Sepak U's sampled scoring frame combines large type, score, impact geometry, a bright ball and movement residue; its following rally frame gives attention back to the ball and bodies. Blue Lock: Rivals offers character-specific presentation and ability identity. Team Reptile's limited-animation direction and Odyssey's emphasis on cohesive sporting actions support stronger authored poses and material interactions. These observations guide TUMP; they are not recipes to copy wholesale.

The current private introductions are useful studies, not the new quality ceiling. A pose plus a small effect and one camera is not automatically a complete character moment. Build the relationship between anticipation, action, contact, consequence and recognition. Connect the cinematic to the real battlefield and the next decision. Keep the approved blocky cast, familiar faces and street-game identity.

#### How simultaneous layers cooperate

Every layer must answer at least one useful question: who acted, what moved, where it will matter, when it becomes active, what actually connected, what changed, or why this moment deserves attention. Multiple layers may answer the same question in different senses: a planted shoulder, sharp rubber slap, compressed hit shape and brief personal camera impulse can reinforce one contact. Several unrelated bursts, full-screen messages and unrelated tones competing for different claims cannot.

Compose three scales together. The **physical event** belongs to the body, shoe or can. **Directional support** describes travel, force and relevant space. **Recognition** adds the player identity, credited change or exceptional achievement. The screen can be dense at a decisive moment, but its brightest shapes, fastest motion and strongest sound should lead to the important action rather than pull toward different corners.

Use distinct phases: readable preparation, decisive accent, visible aftermath and a deliberate handoff to the next action. A broad trail can coexist with compact debris and bold type if they occupy different space and decay at useful times. Begin with roughly 60–120ms for the sharpest visual accent, a few tenths of a second for the physical tail, and about 0.8–1.3s for earned recognition. These are authoring ranges, not extra input delays or mandatory durations. Tune them against actual play.

The mix and compositor must arbitrate **emphasis**, not delete all simultaneous events. Several heroes can retain their silhouettes and truthful warning boundaries while decorative haze gets quieter near the can or a threatened player. Keep one dominant central announcement, with parallel world consequences and the existing side feed. Higher importance may replace a lower banner; stale banners should not wait in a long queue. Source identity, shape, depth, frequency range and timing are all tools before removing content.

Keep seat identity separate from elemental color. Use persistent player number/color for attribution, the actual hero's shape/material/motion for powers, and role/state symbols for danger. Color alone must not mean all three. Lower settings preserve actionable information and the intended punch; reduce secondary debris, translucent coverage or light cost before erasing the effect's meaning.

#### One signature ordinary exchange

Author a complete Classic exchange first, with the other players active, and carry the same physical language into Hero Strike. This is the first showcase of the richer plan, not a plain prerequisite before all excitement is reserved for ultimates.

**Set and throw.** Give the body a grounded weight shift and a clear throwing silhouette. Let the FPP hand visibly secure the slipper, load it and release with the real input. A short directional smear and air displacement can reinforce the release; their source is the moving shoe/hand, not a generic screen burst. The charge indicator settles with the existing aim rules. Cancellation unwinds to the actual held state without a fake throw, resource spend or success sound. Keep the unshaken aim origin and established sight-line launch.

**Rubber meets tin.** At accepted contact, synchronize can rotation/wobble, a sharp material transient, a short directional shape, bounded flecks, personal hit confirmation and score-row attribution. A low body sound and tin resonance can give weight without making three sounds feel like three hits. Retain the compact tin-contact foundation and refine it in motion. A routine hit need not fill the entire screen, but it should feel authored and satisfying. The effect's opening points along actual incoming/rebound motion; the player can still see where the slipper goes.

**First or exceptional knockdown.** The first credited knockdown of a round can earn a larger FIRST KNOCKDOWN treatment. A bank, late play or lead-changing hit may use its own short stamped phrase when the recorded facts support it. Use assertive type, a player accent, a brief score accent and a coordinated sound phrase. In active FPP, place the graphic clear of the aim/retrieval path; in spectator/replay it can be larger and partially staged behind the action. This is recognition of an accepted outcome, not a new reward or invented rally win. A can knockdown starts TUMP's next chase; do not pause everybody to admire it.

**Flight, landing and pickup.** Preserve a clear leading shoe and a shorter, less dominant trail behind it. Let the trail give way to a material landing scuff, brief settling motion and the low owner locator. Keep the real silhouette visible. The pickup has a reach, grip and carry-settle linked to accepted possession; the local locator resolves into the held state, not a celebratory flare that hides the taya. A failed or out-of-range pickup gives concise private refusal feedback, not a successful grab animation. Distinguish ordinary grip, retrieval slide and Zack's recall.

**Restore reverses the situation.** Make the taya's body and hands explain the reset channel. The can steadies into an upright contact beat; the collar and marker change with the actual protection/state. Give the restore a recognizable sound that survives a busy mix. The armed attacker inside the square should feel the reversal through a brief danger-entry cue, the taya's approach and a changed contextual prompt. Do not suggest that can protection makes the attacker safe.

**Chase, escape or catch.** Couple foot plants, cloth/rubber movement and the taya's actual approach. A lunge has a clear preparation/commitment silhouette and directional rush; a whiff has a short air tail and honest recovery, without a hit burst. A successful block has one body response and one deflection episode, not repeated impulses while colliders overlap. An earned close call gets a small release of tension, with larger recognition reserved for a qualifying episode/run. The catch gets a distinct contact signature, the victim's recorded reconstruction and the taya's immediate ability to continue.

Review the sequence with sound, muted and at ordinary speed. A muted viewer should still understand possession and the restore-to-danger reversal; an audio listener should distinguish throw, tin contact, landing, restore, block, whiff and tag without needing a different announcer sentence for every action.

#### Animation throughout the experience

Animation is allowed wherever it carries intent, state, personality or consequence. It is not limited to six ultimates. Prioritize connected transitions and physical reactions over adding isolated flourishes.

- **Locomotion:** starts, braking, turns, strafe/backpedal, sprint fatigue, jump/landing and carrying versus empty hands. Plant feet and align surface sounds/dust with contact. Express effort through the rig's actual capabilities; these rigs have no knees, so root lowering must use the measured support solution rather than burying feet.
- **Object work:** grip adjustment, charge, release, cancel, normal pickup, slide pickup, recall arrival, disarm and can restoration. Body, FPP and possession must agree. Preserve the simple hands, approved rigs, bone paths, GUIDs and animation bindings.
- **Confrontation:** taya readiness, punch/lunge follow-through, block recoil, rival shove, directional stagger, trip/get-up and escape relief. Match actual force, allowed recovery and input. A more dramatic pose must not move the live hitbox or add a lockout.
- **Character presence:** short idle/focus/recovery attitudes, restrained head/torso attention toward visible relevant play, and existing emote/companion reactions. Kuro can use its established expression system. Do not introduce an unrelated facial or roster redesign just to force a cinematic close-up.
- **Gameplay UI:** animated arrival and resolution of a pickup prompt, a stateful can icon, score-row credit, a chain milestone, a role handoff, countdown and replay transition. Current totals stay authoritative/readable; an animated badge can travel into a score row without showing fictitious live totals.
- **Round and match punctuation:** orient toward the next taya, a brief character response to a earned result, a winner tableau with the actual winner and a clean path back to play. Keep the existing menu/login artwork intact; animation may improve transitions without repainting approved art or altering controller mappings.

For every action, specify start, committed/active portion, contact if any, interruption and return. Reuse the same clock and state facts in body/FPP/world presentation. Author strong poses, asymmetry and timing contrast; do not make every action the same smooth hand raise. Camera shake is not a substitute for motion in the body or prop.

#### Gameplay UI and the two recognition channels

The side feed remains the compact record of accepted events. Its three rows, short expiry and dedupe are useful because it should not become a delayed transcript. The central channel is deliberately expressive. It tells players and viewers why a moment matters: first knockdown, a qualifying accuracy milestone, double/triple catch, a meaningful late play or a stable lead change. Exact triggers and bonus rules remain in section D; no new hype meter is introduced.

Give central graphics an authored entrance, a readable hold and a purposeful exit. Use the approved type family with a clear supporting face. A larger word, player accent, motion stroke and compact score badge can coexist when they form one composition. Do not put a big opaque rectangle over the local throw line, important warning or return route. Threat, recovery and readiness information can temporarily outrank a celebratory graphic without erasing the world event.

At each point of the exchange, the player should be able to answer: which slipper is mine, do I have it, can I be tagged now, what is the can doing, who caused that, and what is my next legal action? Let prompts change with those answers. Use current input-action glyphs, not hardcoded keys. Preserve controller/touch behavior and public role identity. No inference of a chain bonus from two score snapshots, and no repeated celebration when a reliable event is delivered again.

Build a small set of coherent moment treatments in the existing HUD/presentation path rather than an unrelated notification framework. Shared cues can coordinate a graphic, material sound, camera/body response and world reaction; each viewer chooses the appropriate composition from the same accepted facts.

#### Six performances, with distinct motion and payoff

Keep the common phase boundary in section E, but make the scenes more than six colored variants. Each needs its own silhouette, shot purpose, material behavior, sound phrase and transition into the real ability. Existing private bodies/effects/audio captures are prototypes to build on, not permission to skip live integration or choreography.

**Sean: compression and explosive direction.** Set the weight, draw heat toward the body, hold a loaded silhouette, then hand off into the actual launch. Use a low/medium composition that contains the support and release direction. The main payoff belongs to the live descent, grounded impact, displacement and fire aftermath. Distinguish rush, empowered slipper and ultimate through their motion and material scale. No fake complete jump before the real one, and no camera shake that substitutes for a readable landing.

**Phaister: deliberate control of a growing eclipse.** Use a readable face/hat silhouette and an asymmetric gesture becoming a larger overhead composition. Tie its movement to the hands and the live ritual destination. A short identity treatment can reinforce the held command. The existing full 1.55-second live warning must remain after the shared phase; the private scene must not secretly consume it. Make boundary completion, actual curse pulses and exit from the field visually different states. Avoid turning every secondary cue into another rune-covered disc.

**Zack: precision, stillness and a sudden break.** Build fine electricity from the actual conducting hand, hold tension, then make one decisive release toward the publicly committed action. Use angular composition and a sharp timing change instead of constant jitter. Preserve aim commitment and the actual warning/contact; a cinematic cannot guarantee a hit. Keep sprint/footwork, magnetic recall/charged slipper behavior and the ultimate distinguishable in FPP, body and sound.

**Nemu: a relationship becomes an imposing presence.** Stage Nemu and the retained small Kuro together: look, offer/draw inward, transformation and the live anchor/pull. The full form must remain recognizably Kuro and fit the shot. Frame actual rendered bounds rather than shrinking the approved form or clipping its head. Preserve an escape direction and the meaningful field boundary when play resumes. Hand the form over once; replaying the growth after the intro would weaken the transformation. Keep veil, possession and unbound behavior distinct, with truthful return/control cues.

**Dante: weight and ground response.** Set a stable stance, load the torso and striking shoulder, then make ground pressure build toward the actual traveling fissure. The camera should communicate feet, weight and direction. Show propagation and affected bodies rather than hiding them under one explosion. Retain the approved orbiting protectors; their assembly/break state is a different idea from a fissure. His ordinary actions need their own preparation/contact/settle, not the ultimate rendered smaller.

**Cheska: precise formation followed by a clean release.** Keep the shoulders controlled and place readable ice between the actual hands, away from the face. Let discrete formation clicks and a restrained movement phrase accumulate into the outward nova. The live result is the truth: freezing/displacing affected bodies and slippers, with the can/routes still visible. Distinguish sheet, barricade and nova by orientation, build sequence and aftermath. No full white wash standing in for impact.

For all six, include caster, affected opponent, uninvolved participant and spectator compositions. Preserve held props and aim on entry/exit. Add close inserts, cut-ins or a stylized impact frame only when they clarify that hero's performance and fit the shared timing/comfort contract. Do not force the same number of cuts or identical camera travel onto every character. Simultaneous casts share one interval and preserve all accepted outcomes; prioritize composition without playing four full introductions in a row.

#### Sound, crowd and the existing street

Plan each important action as a sound phrase: preparation, material contact/body, tail and the opening it leaves for the next cue. World sound, personal feedback and broadcast recognition have different jobs. More layers are welcome when they create one recognizable action. Avoid several peaks making one collision sound like several hits, or every skill competing in the same frequency range.

Use the retained recordings, hero identities and source attribution first. The current native capture path records actual game output and aligns it with frame timestamps; use that for mix review rather than assuming a waveform or successful cue lookup sounds good. A clean peak measurement is not listening approval. Independent master/SFX/music/announcer controls and muted-feedback alternatives remain necessary. Optional spoken lines that are absent from the repository remain absent; no synthetic claim that new VO was recorded.

Recognition can use short tonal/drum accents, a restrained crowd swell or an existing announcer line with priority. Repeated routine actions need material variation and motion detail more than another sentence. In a dense overlap, preserve contact/danger transients, soften competing beds, and let lower-priority tails finish or fade. During an ultimate, shape tension into the live release; do not repeat the same theme/morph when control returns.

Add small reactions to existing spaces where they help: nearby bystanders looking/leaning/reacting to an earned moment, a bounded dust response, cloth or light responding to a strong effect, and a brief atmosphere change that resolves. Keep those reactions outside gameplay collision and the important silhouettes. This is a targeted life/response pass on current maps, not a reopening of map expansion or permission to replace approved environment art.

#### Spectator storytelling and all-player highlights

A spectator needs the developing play, not merely the newest event. Establish the square, can, taya and relevant slipper; then show the retriever's commitment, restoration and chase outcome. Prefer a stable shot that includes a second threat over a decorative close-up that misses it. Use clear player/role attribution and larger earned graphics where they help explain the exchange. Manual camera input retains ownership until autopilot is requested again.

The live view and replay have different editorial freedom. Replays may show alternate angles, a short reaction insert, selective slow motion and a stronger impact composition, but must preserve the actual contact, possession, actor identity and result. Build the full retained state/event clip and its canonical delivery as section F specifies; a timestamp or an enlarged low-quality frame buffer is not completion.

Halftime should package a complete play with a concise reason it matters, then standings and next-taya orientation. Keep the total pacing budget and all participants synchronized. Missing/corrupt/late content gets a truthful fallback at the same deadline. Round/match-end poses and UI transitions can be emphatic because they sit at a real break; ordinary can hits must keep the next chase readable.

#### How the plan merges into the existing backlog

Keep PRESENTATION-1 through PRESENTATION-5 and their linked legacy IDs. Add concrete child checks beneath them, never replace the queue with a separate research list. Preserve every existing numbered task, deferred map/new-character/Inday/U8 entry, reservation and historical receipt. Update the active order in TODO and exact continuation in the ledger. The reference receipt records findings only.

Finish a representative rich ordinary exchange and the shared moment/feedback contracts before expanding disconnected flourishes. Qualify the already committed host-chain bookkeeping. Then connect truthful recognition/rewards and the shared ultimate phase where C4 permits, carry complete performances across all six kits and their ordinary skills, and join them to spectator/replay/halftime. Independent art can continue while a specific network handback is pending; a local preview must not be mislabeled as the shared feature.

Use a review test with four active players and overlapping important actions, not only isolated hero showcases. At minimum, inspect normal speed with game audio, muted playback, a compressed small view, Low/reduced settings and the relevant owner/opponent/spectator views. Check whether an unfamiliar viewer can identify the can state, slipper ownership, legal danger, acting player and consequence. Also check preserved control, physics, scores/resources, exact warning time, repeated-use comfort and cleanup. The target is a vivid readable match, not an empty screen and not an effects-count target.


### The finished experience

Classic and Hero Strike both make the same exchange compelling: a responsive throw hits tin, the shoe settles somewhere readable, its owner retrieves it, the restored can makes the return dangerous, and the taya's reach ends in a clear escape or catch. A new viewer should understand that danger before reading a celebration. Hero Strike adds six distinct character performances without obscuring that game. Halftime shows a retained, understandable play to every participant, then returns everyone to the next round together. No hype currency, power rewards for streaks, new movement verb, fake collision, prolonged tag penalty or extra taya lockout.

### What the current game already supplies

Source was reconciled from 3b9c28d6 through upstream 2c89eb50 and friend shader commit f4c819f9. The camera/lifecycle branch at fe9baa16 and ability branch at dd44acd1 were already ancestors. Their useful implementation stays; this is not a blank project. Own-only normal pickups, recall UI, distinct body/FPP skill clips, extensive six-kit work, spectator direction, manual replay, score/flair/audio dispatch and guarded native runners already existed. Eighteen default hero actions have now accepted actual input in a capture run; that proves the selected route, not artistic finish.

The first local increments added a low shader locator, compact tin contact, personal hit confirmation, side feed, objective marker, stable seat colours/numbers, ownership guards, victim reconstruction, comfort settings and shove/lunge sound distinctions. Keep those changes and their qualified checkpoints, and refine weaknesses against the expanded creative direction. Do not restart them to enact this plan in chronological order. Exact implementation/verification status belongs in the queue, ledger and report.

The actual base hit/tag awards are 100; both modes default to eight 90-second rounds. The ordinary buffer is currently 5 seconds. Phaister's real ritual warning is 1.55 seconds. The current network protocol is 42. Recheck these facts if dependencies change; none is permission to restore an older implementation or downgrade a later version.

### Dependency order and why

1. Finish the current coherent exchange/catch checkpoint, including the corrected real-peer witness and newest audio changes. Preserve the known native fallback.
2. Complete the ordinary exchange with other players active: release-to-flight, landing-to-pickup, restore-to-threat, lunge/block/whiff, escape and catch. Settle public identity and remove conflicting messages as part of the same story.
3. Establish accepted-event identity, throw episodes, exact contact data and shared simulation-time ownership. These are concrete dependencies for chains, ultimate scenes and replay, not a separate general framework project.
4. Wire capped chain rewards and the central milestone channel. Where ownership permits, prove the common ultimate phase with Sean and Phaister. They expose physical launch and preserved live-warning risks early.
5. Complete the six authored ultimate profiles and refine each hero's two ordinary skills where actual captures show a gap. Follow with Zack, Nemu, Dante and Cheska; change that sequence when a discovered dependency warrants it.
6. Extend the same history into retained candidate clips, canonical participant playback and halftime. Refine the existing spectator director around the developing exchange, not the most recent score event.
7. Qualify coherent full matches and repeated use on the actual Windows artifact, with relevant peers, settings and views. Human taste remains a distinct review.

Work can overlap across safe files/workspaces. A needed feature dependency comes first; unrelated historical investigations, a perfect map and the entire device cross-product do not. Do not accumulate a large dependent rewrite on a failing base.

### A. Complete exchange and truthful readability

**Possession.** Keep one shared ownership predicate in Slipper. Normal/slide pickup, force equip, credited throw, AI selection, host requests, disarm, round changes, reconnect, Zack's recall, practice and tutorial must agree. SeatOfOrigin is stable identity; OwnerSlot is mutable ownership. Ownerless stock is deliberate one-seat offline/guided training equipment, never a general live-match exception. Environmental displacement with no credited thrower remains legitimate.

**Locator and identity.** Refine the integrated shader beam instead of adding a competing marker. The current 0.48 m / 0.24 m low form and small ground pool are the starting implementation. It follows loose relevance, holding/flight/parking, depth and near-pickup fading. Preserve Off and the personal highlight preference as an accessibility accent. Public seat identity stays stable across world, feed and scoreboard; local preference must not recolour other players' elemental powers. Add a compact shared identity mark where number/colour alone fails in real views, without turning placeholder icons into another illustration project. The taya badge is separate from player identity.

**Objective.** One tracked can marker owns upright/down/restoring/protected state; the local prompt separately owns whether this player can throw, pick up or restore. Protection belongs to the can, not to an armed attacker. Use a close can collar, not a bubble suggesting area safety. Reuse the complete real tag predicates, including can upright, immunity, role, possession, round and confinement. Stun is not automatically immunity.

**Throw.** Preserve the sight-line launch, movement/early-hold accuracy behaviour, Pektus and the guide's intended uncertainty. Fit hand/body preparation, wrist release, short trail and follow-through to the accepted action. Preserve underlying aim and launch origin under all shake/impact settings. Fix the visual hand-to-flight connection without moving authority's projectile through walls.

**Tin contact.** Keep a compact metal/rubber transient, a low contact accent, real can toppling and short aftermath. The scorer gets personal confirmation; others get the world event and attribution. No routine confetti, victim-style camera punch on everyone, stacked global pauses or multiple words for one knockdown. Protected contact, a body block and a miss have different meanings and cues.

**Landing and pickup.** Finish the short trail into an honest settle/contact, then hand attention to the low locator. Accepted pickup aligns reach, sound, held shoe and disappearing locator while preserving running/slide movement. No success cue on a refused grab. Keep actual reach, carrier, motor and FPP state consistent.

**Restore and chase.** Tin snap, marker transition and taya motion must read together, without a celebration lock. An attacker gets a catch warning only when actually catchable. Use a brief peripheral transition and readable persistent action information. Preserve footsteps and restore sound. No omniscient proximity radar or permanent full-screen danger wash.

**Block, lunge and whiff.** Distinguish preparation, committed reach, actual contact or miss and recovery. Preserve human reach/speed/cooldown rules. The lunge rush differs from the short shove swipe and slide scrape. Observer action replication supplies the same tell; pose restoration must not replay an onset. A whiff has no success impact. The redirected slipper remains locatable.

**Escape and close call.** Extend HighlightWatch/MatchHighlights rather than add another cosmetic counter. Ordinary safe exit gives local relief. Exceptional recognition requires a recent legally threatening committed attempt, actual catchability, unobstructed relevant contact geometry and successful exit. Distance to an idle taya or a down can is insufficient. Bind one retrieval episode to its accepted throw, so dropping/regrabbing or repeatedly crossing the boundary cannot farm recognition. Tag/new round/new throw closes or resets the episode. No automatic close-call score bonus is planned.

**Checkpoint.** Use the same Eskinita exchange in both modes with P3/P4 active, including legal block, genuine whiff, protected-can contact, restore while armed inside, ordinary escape and actual catch. Include body/owner views and recorded timing. A staged capture is labelled staged; it is not freeform evidence.

### B. Accepted moments, time and contact

Extend accepted gameplay producers and MatchFlair rather than create another score or ability authority. A small immutable moment record carries match/round identity, event/attempt/cast sequence, actor/subject, authoritative timing, contact position/direction and outcome. Extra fields belong only to events that need them. Input/prediction can present preparation; only accepted outcomes celebrate success, award points or label a successful play.

Record a real release origin/attempt when HostThrow accepts a credited throw. Use it for distance and accuracy; the thrower's later position is not the origin. Keep the can's upright-cycle identity separate from throw and round identity. Capture exact tag contact before teleport, including both root/pose states and the held item. Use existing network state for recovery rather than inventing a timer.

The current body recorder has eight seconds of bounded transform history. Its pure render copies cannot run gameplay scripts/colliders or own live meshes/materials. Extend this foundation with necessary props/events. It is not yet a complete world replay recorder. Keep exact event keyframes and recorded intermediate poses; never rerun physics to rediscover the outcome.

Use active simulation time for chain windows, cooldowns and round rules; presentation/network time for synchronized phase deadlines; and a separate replay clock. Retain mapping to existing manual replay wall-clock timestamps instead of silently changing their meaning. Correlate records by event identity, not by comparing incompatible clocks.

### C. Victim catch, taya confirmation and return

The current 1.1-second reconstruction is a prototype starting point, not a locked duration. Show a short recorded lead-in, accepted contact and actual follow-through/reaction from a readable three-quarter view. Reanchor reaction at contact rather than depicting teleport as another hit. Retain real blocky bodies and the held shoe. No invented slam, tackle, anatomy or authoritative rewind. Choose a side from approach/hand geometry and occlusion; avoid a decorative orbit.

Only the victim's existing non-actionable recovery can host a takeover. Clamp playback to remaining recovery and end before control returns. Late events do not restart five seconds. Bounded event-before-state handling needs real-peer proof; simulated ordering alone does not establish delivery. Exact authoritative contact metadata replaces nearest-history heuristics where latency prevents faithful reconstruction.

The taya keeps the normal camera, contact sound, hand follow-through, score and earned milestone, with no extra lockout. Other participants get world reaction and attribution. The spectator follows the live consequence instead of abandoning a second developing chase for the victim's replay.

Playback never invokes score, abilities, announcer events or gameplay callbacks. Restore visibility on allocation/render failure, disable, role/camera switch, round/match end and scene teardown. Reduced/cinematic-camera settings change the view, never the penalty. Plain physical recovery does not inherit elemental frost graphics.

Required checks: repeated catches; real actor/victim/uninvolved participant and spectator paths; late/duplicate events; short remaining recovery; taya movement/next catch; held-item visibility; body/pose fidelity; camera/input/lens restoration; interrupted playback and repeated-match cleanup.

### D. Chains, points and separate HUD channels

Keep deterministic rules in the engine-free Core package, owned by MatchDirector and accepted gameplay events. The feed and replay selector are never score authorities.

For each attacker, a legal credited direct throw knockdown advances once. A true miss or defensive block resets that player's sequence. Another player's score does not erase it. A non-scoring flight whose original can cycle was consumed can be no-contest; an actual later legal knockdown still counts. Do not prematurely void a live shot. Resolve once with explicit hit/block/no-contest/miss precedence and no duplicate can-cycle award. Invalid input is not a launched miss. Tag/new round resets. Ultimate/field knockdowns keep base rewards without masquerading as accurate throws.

For the taya, retain distinct victim identities and last qualifying active-play time. Start with 8 seconds between different catches. The same victim neither advances nor refreshes the chain. A can knockdown need not erase it. Round/role changes reset it; three distinct victims is the natural ceiling.

Start rewards at 0 for the first action; +10 for a second hit/catch; +20 for a third consecutive throw hit; +25 maximum for fourth/later throw hits; +25 for a third distinct catch. Tune from actual scoring influence. No multipliers, power buffs or extra ultimate charge. A base action advances normal resources once.

Use explicit appended bonus reasons through MatchDirector.AddScore, preserving existing enum values. Replicated totals remain authoritative. Correlate awards/milestones with accepted event IDs so duplicate delivery cannot repeat recognition. Reason/value and actual count must reach clients consistently. Do not infer a bonus from two snapshots or emit another fake Tag/LataKnocked event. Wire/compatibility changes are a named C4 integration dependency.

The side feed has at most three recent short entries with expiry/dedupe, without footsteps, passive ticks, routine releases or stale pickup messages. Central milestones sit under the timer, away from the crosshair. Start with third/fifth-hit accuracy milestones and double/triple catches; higher milestones replace lower ones. Threat, recovery and cast information outrank celebration. Longer streak milestones can be tuned for rarity while the per-hit bonus remains capped.

Close-call recognition stays separate from points. Lead changes need anti-flutter handling for ties/passive ticks. Use a short escalating motif and sparse voice, not more volume or a permanent excitement bar.

Required checks: independent players, intervening scorers, misses/blocks, consumed cycles, duplicate contacts/messages, tag/round reset, repeated victim, eight-second boundary during pause, caps, exact client totals, unchanged base ultimate gain, and replay/recognition unable to award score.

### E. Shared ultimate phase and authored performances

Choose one host-owned presentation phase rather than stretching Hitstop. It has match/round/phase identity, authoritative start/end and accepted cast records. Acceptance validates and commits cost once; actual ability execution waits for release. Do not create a live hazard first and pretend the movie preceded it. Preserve the full existing playable warning afterward.

Start with one common 2.8-second phase: distinct hero composition/performance until roughly 2.4 seconds, then about 0.4 seconds returning each viewer to the saved battlefield/aim. These are authoring targets. A common boundary simplifies simultaneous casts without requiring identical choreography.

Collect casts accepted in the same authoritative simulation boundary into one cohort before closing its interval. Preserve every accepted cost/effect and resolution order. No four-movie playlist or deadline extension for later requests. Once closed, new ultimate requests during pause are explicitly refused without cost. Clear discrete buffered actions and require fresh release/press; held movement can resume normally. UI/menu/network processing stays responsive. Reduced-camera viewers get the same pause and no extra live aiming time.

Audit concrete writers/consumers: Hitstop, CameraRig, pause menus, spectator clock control, round/anti-stall clocks, motor/physics, slipper flight, cooldowns, statuses, windups and ongoing fields. One phase owner coordinates them and restores the correct prior/current requested clock state, including spectator pause/slow rate. Never blindly restore1.0. Network/UI/presentation continues appropriately while simulation timers stop together. Bound cleanup/timeouts on round/match end, disconnect, late join, scene change and presentation failure.

A late client joins the current phase age or a truthful reduced fallback with the same end, rather than starting another full scene. Reconnect restores state without replaying an old intro. Match/round/phase IDs reject stale messages. The request-refusal/prediction findings matter where they intersect this path; resolve that narrow dependency with its owner, not a duplicate full C4 audit.

For valid back-to-back casts after resume, preserve acceptance/effect timing. Within a short repeat window use the full-duration in-world/cut-in treatment with less camera travel. This reduces camera whiplash but not cumulative pause time. Measure cast frequency and paused time per representative match, state the tradeoff and refine presentation without inventing an ability cooldown.

**Sean.** A low planted compression, inward heat and tightening sound, then a loaded launch silhouette. End at the pose entering the actual leap, without a fake complete jump first. Real descent, grounded impact, displacement and fire field carry the main payoff. Preserve physical phase timing and routes.

**Phaister.** Clear witch silhouette, deliberate gesture and one growing eclipse composition. The final gesture becomes the full existing 1.55-second playable ritual, without idle reset or an already-harmful frozen circle. Distinguish build, completed boundary, actual curse pulses and leaving the field.

**Zack.** Only the accepted committed aimed release gets the scene. Use an angular medium composition, fine electrical buildup, controlled stillness and one decisive release. Reorient to the public committed area, then preserve live warning/contact. A miss stays a miss; charged throws remain a distinct aftermath.

**Nemu.** Frame Nemu with retained small Kuro. A held exchange and inward motion lead into the existing monster form, then the live anchor/pull and readable escape direction. Preserve the approved cute/blocky ghost. No realistic horror, invented props or generic explosion.

**Dante.** Frame planted feet, torso and ground; load weight into the forward strike. A tightening ground response leads into the actual traveling fissure and affected players. Preserve footprint/collision truth and the protector baseline. Fitted details stay separate from the fissure's form language.

**Cheska.** Controlled stillness, readable hand silhouette and fine ice formation following one precise gesture. A crisp release enters the outward nova. Show actual freezes/displacement, including slippers, while keeping can/routes visible. No whiteout as a substitute for contact.

For each, author/review body and FPP preparation, shot, sound phrase, moving geometry, return pose, live execution, miss/refusal/interruption and consequence. Caster, others and spectator can have different compositions but one phase boundary. Do not reveal private pre-commit aim or unseen opponents through a cut. The two ordinary skills retain distinct clips/jobs and receive targeted refinements where complete interactions are weak. Never replace a good kit because an old table calls it a candidate.

Required checks: accepted/invalid/uncommitted input, preserved warning, all six default kits, relevant roles, simultaneous cohort, rapid succession, resource/score invariants, interrupted/late state, reduced settings and busy play. One phase prototype or finished hero does not close this workstream.

### F. Canonical highlights and halftime

**Owner visual correction, 2026-09-21:** halftime is a popup over the visible court,
not a separate full-screen standings page. The green treatment was rejected.
Use image-generated ideation to choose a compact, authored graphic treatment with
clear hierarchy, automatic entrance/settle/exit and concise scores/next-taya
orientation. Preserve canonical replay and shared deadlines underneath; replace
the presentation without dropping their implementation or qualification tasks.


Choose shared state/event clips rendered locally as the primary delivery path. Reuse body reconstruction for meaningful alternate angles without requiring every participant to have recorded the same camera. Keep captured-frame manual replay as an honest fallback/reference, not final quality merely because its overlay is larger.

Extend bounded history with can pose/state/protection, slipper identity/pose/state/holder, relevant accepted ability/field transitions, identities, exact contacts and necessary audio. Preserve recorded state and interpolate; never simulate physics/scoring/abilities in playback. Exact contact/transition keyframes survive compression. Copy only render data.

Host events/episodes create windows with roughly 2 seconds of lead-in and sufficient aftermath. Preserve complete data into a small shortlist, initially three clips, before rolling history overwrites it. Retain the shortlist across rounds through halftime; raw rolling history can reset safely. Clear on match/rematch identity change, not every round.

Rank story completeness/readability before spectacle: actual dangerous retrieval/outcome, bank hit with consequence, restore into different catches, meaningful lead change or ultimate conversion. MatchHighlights remains metadata; a timestamp alone is not a retained clip. Do not label an incomplete montage as a full triple catch or speed up an incomprehensible long chain.

Use stable roster/rig/prop references plus changed transform channels/events, not transmitted mesh assets. Start with 20 Hz body samples rendered smoothly at display rate and exact contact keys. Measure fidelity/size on the real rigs before final compression. Initial budgets: three retained candidates, 2 MiB per encoded clip and bounded assembly/transfer queues. If a candidate cannot fit without losing truthful contact, choose a suitable complete one or a still/standings fallback. Never silently lower quality to an unusable buffer. Record dedicated/headless-render limitations.

Pre-stage candidate content during play through bounded reliable chunks and readiness acknowledgements. Identify match/round/clip/version and verify payloads. Host chooses the canonical clip; clients do not select from incomplete local logs. Late/incomplete/unsupported content gets a truthful contact still, explanation and standings at the same phase times, never indefinite loading. Chunk/bandwidth limits must respect and be measured against the installed transport.

Replay has its own world-sound route, retaining material/contact cues and variation without relaying them or replaying announcer/award callbacks. Show REPLAY, stable identity and one concise reason. Preserve setup, contact and consequence; slow only the useful key moment. Do not replay the entire frozen ultimate introduction as active-play footage.

For default eight rounds, halftime follows round 4 and lasts about 10 seconds total: one complete replay package, brief standings and next-taya orientation. It replaces the ordinary transition, including card/countdown. Ordinary gaps target 3 seconds; initial loading/readiness stays independent. Custom matches of 6+ rounds matches use the middle non-final break; shorter custom matches keep ordinary transitions. No new control/settings burden. Eight rounds target 28 seconds of total between-round time rather than 35.

Refine SpectatorDirector's existing interests/stories around can, taya, retriever, shoe and safety. Stable wide orientation first; tighter shots only when helpful. No automatic live replay, new spectator-induced global pause or cuts on every score. Manual input keeps ownership until autopilot is requested again.

Required checks: round-one retention to halftime; actual participant delivery; late/missing/corrupt fallback; custom lengths/no final-round halftime; shared end; faithful contacts/possession/fields; no score/resource changes; separate audio/UI; manual takeover; memory/readback/queue bounds and rematch cleanup.

### G. Sound, comfort and physical continuity

Keep world event, personal feedback and sparse broadcast recognition separate. Material/action signatures need attack/body/tail without three layers sounding like three hits. Preserve sources/licences and mix headroom. New short shove and lunge rush are starting refinements; listen to the mix rather than just inspect a waveform or successful lookup. Review throw, landing, pickup, protected tin, restore, block, lunge whiff, tag and escape together.

Independent shake, cinematic camera, flash and announcer controls preserve alternative threat information and authoritative timing. Cosmetic offsets do not own aim/origin. Audio/camera/replay variation uses presentation-owned randomness. No repeated full-screen flashes, roll, FOV pumping, permanent tension wash or constant crowd/announcer chatter.

Movement, stops, feet, jump/landing, grip, charge cancellation, pickup, slide, shove, punch, lunge, stagger and recovery connect at ordinary speed in body/FPP. Preserve rigs, simple hands, approved models and mappings. Add no rooting/input delay to fit a pose. Reuse clips only for actually shared actions, not unrelated hero powers.

### Implementation ownership map

- Exchange work extends Slipper, Carrier, CombatVerbs, Lata, CharacterAnimator,
  ViewmodelArms and the current native HUD. No parallel possession/feedback system.
- Catch/replay extends MatchPoseHistory and CatchReconstruction, then existing
  MatchHighlights/HighlightWatch, SpectatorCamera and SpectatorDirector. Current
  body history must gain prop/event retention before it can serve full highlights.
- Chain rules live in Core; MatchDirector/RoundDirector and accepted Slipper/CombatVerbs
  outcomes own awards and reset boundaries. HUD/audio only consume accepted results.
- Ultimate orchestration integrates HeroKit, HeroAbility and HeroAbilitySystem with
  a small shared phase owner, existing time/camera/input handling and six authored
  profiles. It does not replace kits or create another generic game manager.
- Existing MatchRpc message registration and NetSession compatibility need bounded
  C4 handback. Existing WorldEffectSnapshot is a reference for reconstructable
  public fields, not a reason to duplicate its authority.
- Pacing integrates MatchDirector/SliceRunner, RoleSwapCard, BufferSkipVote and the
  existing balance/rules definitions. Initial readiness stays separate.

### H. Validation and delivery gates

Use development ASTRAReworks and the detached validation worktree already created. Freeze coherent inputs and record base/patch/new files, selected cases, command/profile, logs/XML/artifact and owned handle. No live sync. Restore only identified test/importer rewrites, especially QualitySettings, after owned jobs stop. Keep writable caches/profiles/outputs isolated. One heavy workload is default; bounded real-peer tests use known headroom, not performance certification under competing capture jobs.

Choose checks by actual risk: Core for deterministic chain rules, focused PlayMode for action/camera/state/lifecycle, native builds for assets and real routes, actual peers for delivery/authority. Require fresh nonzero XML with intended cases. Preserve baseline and fixture failures separately. Reuse unchanged valid evidence; no automatic full-suite/rebuild matrix.

At integration, inspect busy Classic/Hero Strike matches, repeated catch/ultimate comfort, cumulative cinematic time, relevant overlap, compressed/muted footage, sound identities, Low effects and representative PC aspects. Check control return, role/round/rematch changes, memory/queue cleanup and frame-time spikes. Staged captures, spawning and source inspection are not human feel approval.

Deliver exact tested build/source identities, ordinary-speed clips/captures and concise receipts. Separate later unverified edits and retain a tested fallback. Push scoped checkpoints to ASTRAReworks, preserving unrelated dirty work and friend authorship. Never replace Desktop or publish main.

### Ownership dependencies and deferred work

C1-C3 have an explicit handback. Old ratio/registry investigations are retired from automatic execution; the exact 48-idle attribution is historical unresolved, not a prerequisite. Preserve fixes and the measured no-rewrite result.

Owner handback 2026-09-21 ("get everything done u are the only agent working on this") assigns the remaining C4 dependencies here. Complete the necessary score/event/phase/replay authority and compatibility work while preserving earlier evidence. Fix concrete prediction-refusal defects and validate their real transport paths; the historical audit is not a second backlog or a reason to stop independent work.

The later2026-09-21 owner correction resumes every genuinely unfinished TODO,
including broad map work, Rafi/seventh hero/water-village expansion, retained Inday
FPP work and remaining U8/network qualification. These follow the saved presentation
checkpoint in the canonical TODO order; the earlier deferral is no longer a scope
exclusion. Preserve assets, concepts, IDs and rejected-alternative evidence. Do not
redo completed menu/login/controller artwork or revive speculative optimizations.

## Preserved earlier direction and evidence

The sections below retain useful recipes, rejected alternatives and historical evidence. Their demo-day, map-first and UI-out-of-scope scheduling is superseded by this design and the canonical TODO. Historical anchors stay valid. The exact prior file is preserved in [planning intake](reports/presentation-pass-2026-09-21/planning-intake/NATIONALS_POLISH.md).

## 0. MISSION

Historical mission context follows. Current order is in TODO.md; current source,
jobs and next action are in ACTIVE_REWORK_LEDGER.md.

The current strategic roadmap for game feel, presentation and Nationals polish.
The systems-expansion era has supplied enough machinery. The next win is making a
player remember the moment they ran back for a slipper with the taya closing in.
**Make the core coherent, then author the whole experience.** Both modes ship;
Classic remains the tournament ruleset unless the human changes that decision.

This file decides **what matters next and why**. It is not another execution queue.
When an increment is selected, engineering belongs in [TODO.md](TODO.md), animation
and Blender work in [ASTRA.md](../ASTRA.md), and human judgments in
[Attention.md](../Attention.md). Reuse the existing entry where one exists. Do not
copy this whole roadmap into those queues or track completion here. ASTRA and
CLAUDE below are ownership lanes, not instructions to contact another conversation.

The former UI/HUD exclusion is superseded for gameplay HUD, milestones and replay.
New modes, heroes, meters, controls and progression remain outside this pass. [FUTURE.md](FUTURE.md) is the retired systems-era roadmap;
[INSPIRATION.md](INSPIRATION.md) remains research and reasoning, not a next-work order.
[AGENTS.md](../AGENTS.md) governs repository work and [VISION.md](VISION.md) governs
the product. Existing balance and authority contracts stay intact.

**Evidence boundary, revised 2026-09-09 from `58e0f57f`, game source still
`7612a04c8a1a`:** this preserves the earlier source/asset-reference and report
evaluation, with selective source checks, not a fresh playtest. New art, sound and
staging directions are proposals, not freshly observed defects. No
Unity or Blender run, new motion capture, or listening approval was performed.
The earlier evaluation inspected old lineup and Ilalim captures as history only;
they predate the latest art, map and animation work and cannot certify today's look.
Below, **confirmed** means visible in current code or serialized references;
**recorded** means an earlier observation; **judge in play** is a proposed quality
test, not a defect claimed from a screenshot nobody took.

## 1. DIRECTOR'S VERDICT AND CURRENT STATE

**Yes, the first roadmap was too conservative.** Executed well, it would chiefly
make this a cleaner version of the same game. Its best insight was the retrieval
loop; its mistake was letting isolated repairs stand in for production direction.
Keep those prerequisites, then invest in a complete audiovisual sequence, distinct
places, a cast with presence and an ending worth remembering. A competition win
cannot be certified by a plan; the ambition is a visible difference in ordinary
play, not a longer list of closed issues.

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

### FIVE QUALITY GAPS TO ADDRESS

1. **Retrieval contradicts itself across body, hand and sound.** Confirmed:
   `ViewmodelArms.PlayAction("slide")` selects `LungeClip`; slide requests `dash`,
   and shove's `bump_swing` aliases that recording. Low retrieval reads as combat.
2. **Animation approval stops at structural evidence.** TODO section 151.16 records
   frame-zero probes; section 151.20's clip-count comparison rejects valid rigs.
   Import cannot approve clipping, transitions or recovery. Those remain unverified.
3. **Finish varies between layers.** TODO section 131.3 records five sourced VFX
   families, while section 131.6 leaves compositions unfinished. Confirmed:
   `HeroHazards.CreateThunderstrike` still draws the flat `ThunderShockRing` star;
   `StreetParesInteractive` reacts with `slipper_bounce` and `ImpactBurst`.
4. **Map-wide presentation lacks an approved target here.** Roadside repairs did
   not define lighting, depth or ordinary camera composition. This is a planning
   gap, not a fresh finding that every map looks bad.
5. **The whole experience lacks current feel approval.** Sound hierarchy, camera
   recovery, transitions and ending have not been judged together. TODO section 151
   fixed the listener and remote wind-up/slide cues; their usefulness under music
   and abilities needs ears, not another implementation. Sourced sounds remain
   provisional until heard in play.

### FIVE THINGS PEOPLE SHOULD REMEMBER

- The tin crack, sudden opening, low retrieval and taya's missed reach as one scene.
- A bank whose contact and changed flight can be followed without an explanation.
- Three Filipino places, each with its own light, depth and sound.
- Six heroes whose posture, timing and powers belong to the same authored person.
- A final play that resolves into a character response and clean musical closure.

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

### What the two modes should feel like

**Classic: tactile, intimate street rivalry.** Sun and shade frame a small can,
rubber skims the road, approaching feet tighten the space, and a missed hand makes
a tiny escape thrilling. Ordinary pickup stays calm; banks, commitment and timing
supply the spectacle. Its sparse mix, grounded contact and neighborhood presence
are a complete aesthetic, with the same quality of transitions and ending as Hero
Strike. Simplicity should feel chosen at every beat.

**Hero Strike: six strong personalities interrupt the same street tension.** The
street remains visible beneath the power. A cast has recognizable preparation,
a readable job, one peak and an aftermath returning attention to possession.
The pleasure is seeing a hero create or deny a retrieval opening. Coordinated
motion, material, sound and timing make powers feel expensive.

**Screenshot identity:** voxel silhouettes, authored can labels, sourced footwear,
chalk on quiet road, locally specific facades, strong depth and short comic-like
impact punctuation. Their composition should identify this game beyond its voxel
style. Preserve canonical art and the established toon/outline treatment.

## 3. CORE MOMENTS

Quality bars for existing actions, not orders to add an effect to every beat.
The throw/lata/retrieval/chase sequence gets the majority of effort.

### Throw, consequence, exposed slipper

**Charge/release:** body and viewmodel wind-up plus relayed `throw_charge` already
provide anticipation. Power builds through shoe and shoulder without vibration;
the taya can hear preparation while looking elsewhere. `Carrier` already plays
the throw, varied release cue and replicated flair. Hand release must coincide
with the world slipper leaving; flight, bank, bounce and rest stay readable across
light and dark road. Judge short legal charge, long hold and moving release.
Do not alter flight, collision or aim to repair presentation.

**Lata knockdown:** preserve the replicated 45 ms freeze and directional punch
(`Lata.AnnounceUprightChange`) and preferred `lata_impact` / `lata_knockdown` sounds.
Tin attack, tipping and settling should read as one consequence. Clear transients
so the shoe becomes the next focus. Camera strength 0.8 has no distance attenuation;
judge the distant observer before strengthening it. The opening should be clear
without imposing the nearby player's camera violence on the whole court.

**Reset:** existing channel cues, grab and restoration end on one clear upright-can
beat. Judge cancellation and sabotage: no final cue may promise safety before the
authoritative completion. No cinematic pause.

**Bank / near miss / block:** `Slipper` and `MatchFlair` already recognize them.
Show bank contact then redirected flight, a small passing read for a miss, and the
defender meeting the shoe for a block. Preserve block burst, flash and body squash;
keep these beats below knockdown prominence. No false tin hit, new labels or Street
Hype redesign. Improve the action that earns recognition, not its display.

### Enter, commit, escape or get caught

**Approach / normal pickup:** attention shifts to shoe-taya distance. Landed-shoe
presentation and footsteps must survive texture, shade and powers at eye height.
No omniscient chase sound revealing unseen opponents. Ordinary grab remains calm,
confirms possession once and joins hand to shoe without apparent teleportation.

**Committed slide:** low entry, reach toward this shoe, catch only on real pickup,
then vulnerable rise. A failure retains skid and recovery without catch confirmation.
The authored 0.95 s clip and 0.14/0.25/0.342 s contact samples are review points, not
new gameplay timers. Align FPP with that body commitment; keep the taya trackable.

**Chase / lunge / punch / tag:** foot planting and sprint-turn-action blends sell
pursuit. Stationary punch is arm-led; lunge spends the body and has a readable missed
recovery. Approaching steps communicate distance. `MatchFlair` already supplies
tag burst, freeze, flash, camera and voice: let contact win, then return control
without extra holds. A tiny escape reads from the missed reach and continued
momentum, followed by space in the mix. No new escape detector or reward system.

**Shove / trip / recovery:** shove is an outward push, distinct from punch and slide,
with different hit/miss sound. Trips and get-up disclose loss and return of control
without lengthening mash rules. TODO section 151.6 measured Ilalim's live trip hazard
outside the competitive box; no added hazards. Review transitions around one
selected family, including interruption and remote playback.

### Hero Strike and the ending

**Skills / ultimate:** review a hero's three casts in order, body/FPP, effects,
audio and return together. The first skill discloses direction/footprint; the second
shows its own job. Sean's chambering is not an immediate blast. The ultimate earns
one peak. Judge with effects hidden, then during contested retrieval.

**Round / match closure:** existing countdown, voice and music transitions clear
the previous round and establish the new taya. Preserve requested clean music cuts.
`MatchResult` already handles result audio; do not re-add a jingle. `CharacterAnimator`
maps the emote relabelled victory to `crouch`, but that does not prove the ending
calls it. Trace the live path before selecting a celebration. This is A tier after
the core reference, with results UI untouched.

## 4. CHARACTER DIRECTION AND HERO SIGNATURES

Protect voxel proportions, canonical skin, faces, hair geometry and identities.
No wholesale redesign or retargeting. The highest-value model pass fixes a
visible gameplay-distance silhouette, clothing overlap or material separation
problem on one character. Review lineup and real camera distance before details.
Improve pose, clothing readability or shader response within those constraints;
canonical hair is not a silhouette-cleanup free-for-all. Keep existing Generic
rigs and authored action names.

Sean's slide is the first motion reference: ASTRA task 3 corrected short-arm reach
with torso roll. Verify it before reauthoring it. `character-female-a`'s recorded
accessory/sleeve floor-contact ambiguity is a separate single-rig review. Later,
select one idle-to-movement or locomotion-to-action family on one character when
it strengthens presence; do not commission six locomotion sets by default.

### Compact hero grammar

Motion below builds on ASTRA section 1's authored work; paired VFX/audio direction
is the proposed next quality bar. Preserve established accents and role colors.
Shared finish means readable onset, material intent and clean decay, not one
builder recolored six ways.

- **Sean: propelled confidence.** Hip coil, one-axis extension, braced arrest;
  arms rake back. Fire sweeps and tears along travel, with a compact body and sparse
  embers. Pressure build, breathy release and brief hot crack support the motion.
  Supernova's signature is the body becoming the descending impact. Ignition
  chambers a future throw; old sourcing prose calling it a projectile must not
  turn it into an immediate blast. Avoid bloom balls and sustained fire noise.
- **Zack: bladed electrical precision.** Side-on counter-rotation, lateral skate,
  asymmetric aimed call. Branching paths and discontinuous light differ from
  Sean's continuous fire. Tense chatter resolves into a dry crack and short decay.
  Bolt Sprint has no impact beat; do not force one onto locomotion. Thunderstrike
  should be remembered as aimed bolt/contact, not its flat saturated ground star.
- **Dante: planted mass.** Low head, wide braced base, downward force; the carapace
  widens him rather than replaying the stomp. Opaque broken planes, fissures and
  restrained dust carry weight. A compressed low body sound with a distinct crack
  supplies impact without sustained rumble. Titan Fissure splits the ground;
  no leap, glowing shield bubble or generic round explosion.
- **Cheska: exact control.** Upright body, one forearm drawing a plane, precise stop
  and held shape. Facets, tapered shards and crisp boundaries make ice solid.
  A fine formation sound locks into a short crystalline attack and sparse tail.
  Glacial Nova's release-to-stillness is its signature; avoid blue smoke, whiteout
  and Phaister-like ornament.
- **Nemu: unsettling weightlessness.** Limbs lead the torso; she rises without a
  push-off and is pulled inward. Dark negative space, inward wisps and the live
  Kuro presence carry supernatural identity. Inward air and an uncanny hollow
  release should identify her without purple light. No heavy stomp, generic cloud
  or automatic impact hold on Phantom Veil.
- **Phaister: theatrical witchcraft.** Off-axis flourish, deliberate presentation,
  open held finish; Shadow Blink's instant departure is the exception. Written
  sigils, torn vertical wisps and sparse overhead corona belong to distinct
  actions. Incantatory texture, stamped attack and clipped magical tail suggest
  performance. Grand Coven earns its flourish and reveal without stacked circles.
  No Nemu-like drifting blink or Cheska-like shortest-path ritual.

Test both ways: hide VFX and identify movement; hide the body and distinguish the
effect/audio family. Custom characters borrowing a kit must retain its tells.
Review one hero's full three-cast sequence, then select only necessary corrections.
`ViewmodelArms` already has hero actions; ASTRA task 6 is alignment, not eighteen
missing clips. Do not change hit timing, range or recovery rules to fit a pose.
Extreme angular-speed measurements are not a polish score.

### Ultimates: explicit two-phase investment

**Phase A, CLAUDE: establish the reusable presentation contract.** ASTRA task 8
records no cinematic action name. Existing `UltimateStarted` / presentation flow
and introductions already work (TODO section 134.7); do not replace them or redesign
their UI. Select the smallest route: stage the existing cast when it supplies the
pose, or expose a named separate presentation action where it truly needs one.
A shared integration hook must permit different staging, not mandate a shared pose.

The contract states accepted-cast trigger, action selection, authoritative onset,
local/opponent/spectator camera behavior, existing audio/VFX cues, deduplication,
overlapping ultimates, interruption/round-end cleanup and recovery. Exercise one
real cast on host/joiner and the existing observer path, plus refusal and
interruption, before commissioning a cinematic clip. No longer freeze, new input
lock or altered damage window. ASTRA section 2's proposed global cinematic freeze
is not authorization to expand shared hitstop. Reject any version requiring it
and keep the signature within existing play timing.

**Phase B, ASTRA then CLAUDE then HUMAN: one authored signature at a time.** Start
with Sean's kit reference. One session reviews or authors only his ultimate
motion/pose on the agreed hook; a separate bounded integration aligns camera,
effect and sound. The other five follow their own grammar and release/recovery.
Do not stretch existing cast clips to hide a missing intro hook. A brief lighting
response is optional; existing weather may already do the job. Decline extra
lighting if it washes out role colors or hides the shoe.

Advance only after the signature works in contested retrieval from caster and
opponent viewpoints. If the hook adds no visible value, retain the existing cast
and direct its staging. Missing cinematic support must block unreachable assets,
not improvements to live casts.

## 5. VFX / CAMERA / GAME-FEEL PASS

**Direct emphasis before adding response.** Sound, body and camera should agree
on which instant matters. Tune timing, direction, duration, recovery and distance
before amplitude. The core lata/retrieval camera pass belongs in S tier; the wider
action-family pass is A. Use `CameraRig` and existing spectator/replay interest.

- **Release / pickup:** small arm/camera timing agreement where needed. Possession
  gets clarity, not a celebratory hold or automatic FOV kick.
- **Lata / tag:** contact-aligned directional punctuation, then quick recovery to
  shoe and opponent. Judge the distant-view cost of lata's unattenuated punch.
- **Slide / lunge:** restrained inertia and stable horizon; track the taya through
  entry and recovery. Judge misses, repeated use, and mouse, pad and touch comfort.
- **Skills / ultimate:** emphasize the hero's real release or consequence, not
  every particle onset. Distinct timing may require less camera movement.
- **Start / ending / spectator:** establish place and retain the final action when
  control is already outside live play. During play, keep caster, consequence and
  retrieval opening in context instead of cutting to the biggest effect.

Reject forced live-player cuts, aim displacement, horizon rolls, nausea-inducing
inertia, constant shake and automatic FOV pumping. Keep stronger framing in existing
spectator/replay contexts where appropriate. Do not add settings/UI here or assume
an unbuilt reduced-effects setting exists.

Use the existing stack. Shared `Hitstop` is bounded to 20-80 ms and ignores overlap;
`HitFeel` holds only the victim's view, while the world keeps moving. They solve
different problems. Do not globalize ordinary hits or add caster feedback that
reveals offscreen victims. Existing camera holds were repaired for drift in § 150;
judge repeated impacts at the end of a chase before increasing any strength.

The first repair within Zack's hero pass is **ThunderShockRing**: retain the lightning
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

## 6. AUDIO DIRECTION

**S tier within the core sequence:** space, preparation, release, tin, opening,
approaching feet, scrape/catch, tag or escape, release of tension. Flight connects
hand to contact without a constant whistle. The can transient commands knockdown;
its tail yields to footsteps and retrieval. Missed slides never confirm a catch.

Direct loudness, frequency and duration together. Tin needs a clear upper attack,
approaching steps a usable midrange, and hero weight must not smother either.
Trim competing tails and ambience before raising foreground gain. Leave holes in
the mix rather than sounding every beat. These are audition hypotheses, not fixed
EQ numbers. Preserve preferred can recordings; section 4 gives each hero its own
preparation, peak and decay. An ultimate is not simply a louder skill.

**First audition:** slide scrape/catch against shove in contested pickup. Pitch
variation exists but cannot make one recording mean two decisions. Human chooses
the distinction; engineering integrates the approved timing and relay.

**Space:** `AudioDirector` follows the active camera; world voices use **2-32 m
linear rolloff**. Attention section 18 records far-diagonal and FPP/TPP questions.
Hear approach, behind-camera landing and slide from both sides, including a joiner.
Tune one family only when inaudible or misleading, not all SFX. The 42 elemental
replacements and earlier sourced cues remain provisional (Asset_Sourcing section
5.5). Pisonet's score-sting misuse is fixed; its pitched click needs a coin-sound
judgment. Pares should not impersonate a bouncing slipper.

Music already has late-round pressure lift and announcement ducking. Hear a full
ending with the OST: feet and tin survive the lift, victory registers, deliberate
cuts remain clean. No new adaptive-score system, music fades, constant chatter or
global chase loops. Human-recorded tsinelas, neighborhood sounds or Tagalog exertion
can serve S/A identity when filling a specific gap, one family at a time (Attention
section 9). Judge headphones and ordinary speakers for nearby watchers as well.

## 7. MAPS / ENVIRONMENT PASS

**Each map needs a final art-direction pass, delivered in small increments.**
The unit of direction is the whole map; the unit of work is one lighting/composition
pass, one landmark or one frontage. A prettier corner alone does not finish a map.
For each, select three existing gameplay views: first throw toward the lata,
retrieval at eye height, and the defender's reverse view, plus one existing
spectator angle. Stage foreground edges, readable action midground and a background
landmark. At least one should make a strong hero shot from the ordinary camera.

**Players, lata, loose shoe and chalk first; landmark second; dressing last.**
Start with key-light direction, ambient fill, broad shadow shapes, contact grounding
and material response. Check slippers in sunlight and shade, can metal/label
separation, asphalt versus pavement, cloth versus hard surfaces and characters
against facades. Use existing rendering controls before new props or shaders.
No automatic PBR conversion, wet-road makeover, volumetric/bloom upgrade or renderer
replacement. A new rendering feature needs visible benefit and measured cost on
target hardware, including the supported lower setting. Warm probe cost alone
cannot approve it. Cooler shadow does not mean saturated defense-blue streets.

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

**Direction:** close neighborhood warmth and claustrophobic chase. Warm roadside
life, neutral readable ground, layered facades, sampay, roofs and cables frame the
long approaches. Light should distinguish the near sari-sari frontage from the road
and far layer without an orange wash. Keep rich edges and broad quiet lane values.
The sound is low domestic/shop life with occasional distant activity, not chatter
over footsteps. Existing cloth or prop motion is a peripheral accent only.

**First increment:** one lighting/composition pass across the three views, then
frontage refinement if needed. Done when the normal and reverse views identify a
neighborhood, the near/far layers separate, and the retrieval line survives both
shade and narrow-lane Hero Strike overlap. Do not widen the map. A decorative
reaction is optional; the stronger whole-map view is the S-tier outcome.

### BAYAN PLAZA

**Strength:** the open barangay square, monument, church/basketball vocabulary and
trees can provide a calmer, more public counterpoint to Eskinita. **Correction:**
`BayanPlazaMonumentFix` already removed the blocking intrusion into the defender's
box; the older map document is history on that point.

**Direction:** open civic space and confident sunlight. Broad sunlight/shadow
rhythm, a clear monument and church/tree silhouettes create a public barangay
feeling. Foreground paving leads into open action space and a far landmark. Empty
ground is a positive part of this identity. Use a more distant, open street bed
than Eskinita and sparse canopy motion; no extra stalls or clutter to fill frames.

**First increment:** one monument-side lighting/composition pass checked from the
reverse defense view. The fix retains a small walkable visual overlap: judge the
seam, do not replan collision. Done is deliberate open ground, grounded characters,
a clear landmark and a view distinct from Eskinita without map text.

### ILALIM NG TULAY

**Strength:** the carriageway-as-box, overhead guideway, PC Express, pisonet/pares
edge and jeepney provide the richest local specificity. Train motion and a moving
field-recording source already exist. `LrtTrainFlyby` now defaults to a rare
300-second interval; do not restore the old document's 24-second rhythm or retune
its gameplay-linked pass while adding ambience.

**Direction:** cool-neutral bridge shadow, restrained shop-light pockets and urban
weight. Guideway mass sits above a clear carriageway; pisonet/pares/PC Express frame
the edge, the jeepney anchors a distant silhouette. Depth and selective highlights
reveal metal without road darkness or glare. Avoid a neon reskin. A subdued urban
bed yields to footsteps, while the existing rare train provides the interruption.

**Foundation concern is retrieval continuity.** The
[six-seed report](reports/bot-sweep-2fde55d32246.md), on its recorded commit, has
0-48 idle penalties in Hero Strike on Ilalim, with almost all the dead time in one
seed. That is a location to investigate, not proof of an unreachable slipper or
broken map. Trace that recorded outlier before polishing the implicated retrieval
area; independent lighting direction need not wait. Do not infer a balance change.

**First art increment:** shadow-to-shop composition across the three gameplay
views, then one pisonet/pares frontage audition after the route check. It already
reacts; the question is whether the sounds and bursts make it
feel like a food/computer street or another fight. Judge shop light and signage
against the bridge shadow, material consistency at the asphalt/pavement boundary,
and environmental noise during an ultimate. The jeepney's metallic factors and
reflection probe were repaired in § 151.5; approve the finish visually before
changing lighting again. Done is one legible, recognizable roadside zone whose
reactions leave room for footsteps, can and slipper. No new hazards or rules.

**Life is selective direction, not mandatory animation everywhere.** Prefer an
existing reaction with recognizable cause, brief local response and quiet afterward.
Do not trigger a gag to compete with a tin hit or chase. Still frames and mix come
first. One meaningful shop response can earn A tier; another decorative background
motion accent remains C. No automatic traffic, moving everything or hidden players.

## 8. REVISED NATIONALS PRIORITIES

Historical tiers: the current TODO order supersedes their scheduling. Map-wide
art direction below is deferred, not the second active task.

### Two layers, one quality reference

**Foundation / coherence** removes contradictory signals: body/FPP mismatch,
misleading sounds, unreadable ground, broken blends or missing remote feedback.
Fix the relevant blocker. Motion capture earns its place by answering a selected
move, not by becoming a tooling project.

**Transformative polish** gives a whole sequence and place deliberate rhythm:
light and composition, motion and sound identity, camera emphasis, quiet and payoff.
Start when its own prerequisites work, without waiting for every minor defect to
close. Each increment states its intended perceptual change and ends in comparable
gameplay evidence. An improvement visible only in an isolated preview is unfinished.

First approve the core sequence on Eskinita; extend that standard to its ordinary
map views and one hero's complete presentation. Carry the quality bar across maps
and heroes, not the same treatment. Start and ending must bookend the experience.
Tiers describe impact, not task size. Release/security blockers keep their separate
priority; this presentation plan does not demote them.

### S TIER: the production-value change

1. **Complete throw/lata/retrieval/chase reference.** Foundation: body/FPP, sound
   meaning and motion evidence agree. Transformative: author anticipation, tin
   payoff, footsteps, contact and recovery as one scene. Audio and camera direction
   are part of S, not optional later fixes. Highest repeated player/judge return;
   narrow integration protects control and authority.
2. **Map-wide art direction, Eskinita first.** Deliberate lighting, composition,
   material response and Filipino specificity in ordinary views. Establish one
   map, then give Bayan Plaza and Ilalim their distinct treatments in separate
   passes. This outranks scattered seam repair; readability and device cost gate it.
3. **One complete Hero Strike reference, Sean first.** Character presence, three
   casts, body/FPP transitions, VFX and sonic identity, with the existing ultimate
   as the peak. Several small sessions deliver one result. Phase A of the ultimate
   contract is a prerequisite only when a new presentation action is selected.

### A TIER: carry the standard through the experience

- **Five remaining hero treatments and Phase B ultimate signatures**, one hero and
  one art/integration task at a time. Zack's star repair belongs within his grammar,
  not ahead of whole-game identity just because it is easy to name.
- **Transitions and match closure.** Around one selected family, review idle to
  sprint, sprint to throw, throw to recovery, pickup to sprint, slide/get-up, hit
  to locomotion and cast exits, including interruption and remote playback. Start
  establishes the street; new taya begins without stale effects; final play resolves
  into character response and existing music/audio closure. Trace `MatchResult`
  and victory-to-`crouch` before selecting one celebration. No results UI, extra
  jingle, live-player camera cut or timer change. The last impression matters.
- **Supporting sound/camera hierarchy and meaningful environmental reactions** in
  full play with the OST. One bank/block family, one map ambience or one shop
  response can strengthen the whole scene after the reference is approved.
- **Ilalim's route outlier**, a foundation investigation rather than promised
  spectacle. Reproduce the recorded seed and log the slipper's rest position;
  escalate demonstrated obstruction, not an inferred map or balance defect.

### B TIER: observed local roughness

One rig's accessory contact, material seam or noncentral action mismatch surviving
main passes. Promote it if it obscures a player, misstates possession or blocks a
selected S-tier sequence. Do not repair every model because a metric is unusual.

### C TIER: optional decorative rewards

Background motion accents, detail invisible at match distance and extra dressing.
Human-recorded audio is not automatically C: a distinctive foreground source can
serve S/A, while another background accent can wait. Remove tasks whose only value
is being measurable or whose addition duplicates an existing beat.

### DO NOT DO BEFORE NATIONALS

The former blanket HUD/scoring/freeze exclusions below are historical. The owner
now authorizes own-only retrieval, capped chains, gameplay HUD, shared cinematic
phases and halftime. Unrelated new mechanics/destructive changes remain outside scope.

No new modes, heroes, abilities, meters, controls, progression, hazards or UI/HUD;
no wholesale roster, audio-library or renderer replacement; speculative pooling,
asset decimation or sourcing sweep; six unreachable cinematics; larger footprints,
stacked floor planes or white flashes; constant shake, chatter or street motion;
wet/glossy everything; arbitrary prop density; generic capture frameworks or broad
repeat probes without a new question. Orphan-file cleanup and comment-count repairs
remain engineering housekeeping, not transformative polish. Do not revive old
FUTURE/INSPIRATION prompts as the default order.

No new gameplay is recommended. Changes to routes, collision, scoring, control or
freeze duration need a separate human gameplay decision and are outside this plan.
Do not retune slide recovery from bot usage alone. Preserve the geometry, white-frame
and overlap constraints in section 5 throughout every art pass.

## 9. HUMAN QUALITY BAR AND STOPPING RULES

The first view establishes a Filipino street; the first knockdown creates a clear
opening; the first chase sells ordinary pickup and risky slide; the first skill
shows its job; the ultimate has one signature; the ending resolves the final play.
Use existing player/replay/capture tools. No large new test campaign.

**Ten ordinary screenshots:** sample ten times without selecting pretty frames
from an unedited segment. Record commit, mode, map and viewpoint. Count frames with
intentional silhouettes, depth, landmark, color/value hierarchy and VFX coverage;
compare before/after and name repeated failures. **8/10 appealing ordinary frames**
is an initial working aspiration, not a measured result or permission for two
unreadable frames. Necessary in-view gameplay information must remain readable
throughout. Apply this as each map pass lands; a staged hero shot cannot certify it.

**Thirty-second clip:** use ordinary unedited gameplay with natural audio. When a
bank, knockdown, retrieval, missed lunge or ultimate occurs, can an uncoached watcher
understand it and react? Two or three reactions are an aspiration when those events
occur, not a quota requiring extra spectacle during quiet play. If nothing eventful
occurs, judge pacing in a longer segment rather than inventing events. Ask what was
memorable without suggesting the answer. Watch muted for motion clarity, listen
without the picture for sound identity, then judge them together.

**Player and judge:** include FPP thrower, defender/opponent, joiner and existing
spectator view where relevant. Someone behind the player should follow the same
lata-to-shoe-to-taya story. Spectator framing keeps cause and consequence in view,
not just the largest explosion; maps supply readable backdrops. Headphones alone
cannot approve a room-facing presentation. Compare the same action, map, viewpoint,
mix level and supported quality setting. Retain a change for perceptible benefit,
not asset count; revert if it costs tracking, comfort or clarity.

**Whole experience:** once references work, judge one full Classic match and one
Hero Strike match with a watcher, including start and ending. Classic must stand
on its own without powers; Hero Strike should still be remembered for street-game
decisions. Judge each remaining map as its pass lands. Mechanical checks prove
wiring; humans approve timing, taste, sound, comfort and competitive readability.
Record actual approvals in Attention.md with build/viewpoint, using section 17.2
for slide feel, section 18 for spatial mix, section 13 for sourced cues and section
9 for recordings. Reuse existing entries; do not duplicate an approval ledger here.
Nothing in this revision claims those judgments or playtests are complete.

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
   Close the batch by watching the resulting throw-to-retrieval/chase sequence with
   the OST, so separate successful fixes become one approved timing/mix reference.

## NEXT: MAKE THE REFERENCE TRANSFORMATIVE

After the first six increments, select a transformative S-tier outcome rather
than filling the schedule with B-tier repairs. These are ownership lanes, never
instructions to contact another conversation. Each selected task enters its
existing queue; do not copy all future proposals into TODO or ASTRA.

- **ASTRA:** one hero's full presentation review (Sean first, ASTRA section 1A),
  then only the necessary motion correction. Done when the three jobs read without
  VFX in full-speed in-engine evidence. A separate model pass is another task.
- **CLAUDE:** one approved cast's FPP/body/VFX/audio timing or transition seam.
  Done when owner and opponent see the same release and recovery in a player.
- **SHARED:** one map's lighting/composition pass across section 7's three views,
  Eskinita first. CLAUDE owns scene/builder/shader integration; ASTRA owns one
  selected Blender asset only if needed. Human judges depth, mood and clear ground.
  This does not widen ASTRA.md into scene-code work. A landmark is a separate task.
- **CLAUDE then ASTRA:** one Phase A ultimate contract increment, then one hero's
  Phase B motion/pose on the agreed hook, then separate integration and human review.
- **SHARED:** one match-ending review, then one approved body/audio/camera correction
  through existing hooks. Done when final action and closure feel connected.
- **CLAUDE:** Zack's ThunderShockRing shape/value/decay within his hero pass
  (TODO section 131.6). Retain bolt, star direction, footprint and timing; compare
  solo and Eskinita overlap, plus the white-frame gate. Bolt/contact leads and
  the ground opens between branches. No new VFX framework.

One Plus-sized session owns one hero review, animation family, model, map view
family or integration seam. Stop with evidence and a decision; integration and
human approval are explicit dependencies, not assumed outcomes. Keep useful
existing work when it passes. Ask before credit-consuming external work, never
reset usage, and never contact another conversation.
