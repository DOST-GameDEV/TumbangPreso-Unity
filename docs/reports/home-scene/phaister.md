# HOME background scene: Phaister, "Gulatin si Nemu"

The second per-hero HOME loop, after Zack at Sa Bubong (`README.md` in this folder). Source is
`ArtSource/home-scene/src/phaister/`, composition `PhaisterScene`, 1920x1080, 30 fps, 54 s (her own clock, `src/phaister/time.ts`).
The method is `docs/HOME_SCREEN_ANIMATION_METHOD.md`; this file is the direction, the research
behind it and the numbers.

🧑 2026-09-24, on the first version (a night-time magic trick at a generic tulay):

- *"make sure it actually loooks like the map too but i like the handrawn style already of map i
  just want u to match it more to the features of our actual map like the pc express and shit"*
- *"pls give her more action sequences like zack, he actually moves somewhere"*
- *"make it actually reflect our game"*, *"thoroughly direct how her sequence should look like"*,
  *"i want it to be better than zack's"*, *"think abt how it would like tell a story about tump"*
- *"make her background reflect the actual map more but keep the stylized drawing of it, dont do
  3d assets"*
- *"make her use her ult in the end or smth ... casting a big ass magic circle in the map and her
  eyes are glowing pink and she's floating"*
- *"the signs we have in the game are not final so js imagine it on ur own, research PH store
  signs"*, with two photos: a pylon of stacked lightboxes, and a sari-sari row under soft-drink
  privilege signs with red-and-yellow painted kerbs and bunting overhead.

So the first version's idea (a three-act trick in one unbroken night shot) is kept only where it
still earns its place. Its set was a generic tulay, it had no game in it, and she never went
anywhere. This version is a full round of tumbang preso on the real Ilalim ng Tulay, told as a
short story with a punchline, ending on Grand Coven.

---

## 1 · The story

**Logline.** Phaister has one goal under the tulay: make Nemu look surprised. Nemu is the taya,
and Nemu is never surprised. Phaister plays a whole round of tumbang preso to do it (the throw, the
can going down, the run for her tsinelas, the tag she escapes), and when none of that works she
casts her ultimate over the entire street. Nemu still does not flinch. Kuro does.

**Why this story and not another.** Every piece of it is already canon; nothing is invented:

| Canon | Where | What it gives the loop |
|---|---|---|
| *"Phaister keeps trying to catch her by surprise. Nemu quietly saves those attempts for the walk home."* and *"Phaister wishes she would at least look surprised."* | `docs/CHARACTER_ORIGINS.md`, Nemu and Phaister | The whole premise, and the punchline |
| *"Phaister may try to get a reaction out of Nemu"* | `LORE.md` | Confirms the pairing is the intended rivalry |
| Nemu: *"Looks distracted. Already knows your next move."* Kuro: *"a curious little shadow at her shoulder"* | `docs/CHARACTER_ORIGINS.md` | Nemu never looks at the throw, and is already standing where Phaister runs |
| Nemu *"never touches"* the road; *"her limbs arrive before her torso"*; the slowest cast in the game | `ASTRA.md` 1E | How Nemu moves: floating, drifting, carried |
| Phaister *"invite a chase, leave a false opening, close the circle"* | `docs/CHARACTER_ORIGINS.md` | The three acts: the throw invites the chase, the blink is the false opening, Grand Coven closes the circle |
| Phaister *"SHE PERFORMS"*, the flourish, *"held for the room"*; Shadow Blink has no flourish | `ASTRA.md` 1F | Her acting in every beat, and the one beat where she does not perform |
| Grand Coven: a 10.5 m circle centred on HER that covers the whole 14 by 14 box, drawn in stages over 1.55 s; night comes down 16 frames after her arms reach the sky; pink and violet line art, floating glyphs, a gold corona overhead | `PhaisterHeroKit.cs`, `HeroHazards.CovenCircleBuild`, `SpawnGrandCovenEclipse` | The finale is her REAL ultimate, drawn from the game's own inscription table rather than an invented one |
| The rules of the street game: the throw, the can down, the taya resets it before tagging, the thrower fetches her tsinelas and runs home | `docs/Design.md`, `VISION.md` § 0 | Act II is an actual round, so a player watching the menu is watching the game they are about to play |
| The PC Express overclock pad, the train every 24 s whose pass is a Hero Strike window | `docs/Ilalim_Ng_Tulay.md` § 3.5, § 4 | Each map quirk is a beat, not wallpaper |

**What the viewer should come away with, in order:** this is a street game with a can and a
slipper; this is a real street in Manila; she is a showoff and it works; the taya is unbothered and
that is funny; her ultimate is enormous; the little ghost is the only one who reacts.

### 1.1 Why it is better than the Zack loop, and not the same loop

Zack's loop is a power fantasy with a clean arc: calm, charge, throw, hit, chase, home. It has no
second character with a point of view (Sean only flinches), no joke, and no ultimate. This one is
built to beat it on the four things a home screen is actually judged on:

| | Zack at Sa Bubong | Phaister, "Gulatin si Nemu" |
|---|---|---|
| **Story** | One hero shows his power | Two characters with a relationship, a goal, three escalating attempts and a punchline |
| **The game in it** | A throw and a chase | A whole round: throw, can down, taya resets, retrieval, tag attempt, escape home, plus a map mechanic (the overclock pad, where the ricochet lands) and the train window |
| **Place** | A painted roofdeck | The shipped map's own layout, in metres: its guideway, its columns, PC Express, the hoop, the pad, the pisonet and pares cart, at their real coordinates |
| **Peak** | Impact frames and TUMP! | Her real ultimate across the whole street, floating, eyes pink, night falling, the circle building ring by ring |
| **Camera** | Eleven cuts | A camera that performs with her: one wide shot that holds the whole throw and ricochet, a tracking run, one impact frame, and a crane that climbs out from under the guideway to see the circle whole |
| **Hour** | Dusk | Golden hour that her ultimate turns into night, and the night that gives the calm half of the loop its mood; the loop seam is the night lifting |

---

## 2 · The place: Ilalim ng Tulay, drawn

⚠️⚠️ **THE SET IS DRAWN, NOT RENDERED FROM THE MAP'S MESHES.** 🧑: *"keep the stylized drawing of
it, dont do 3d assets"*. The characters, the can and the tsinelas remain the game's own models (the
method's § 2 is not negotiable). Everything else is flat blocky shapes, authored in metres and
projected by hand through `src/phaister/view.ts`, the same technique as the first version, so a
moving camera sees true perspective.

⚠️⚠️ **BUT THE LAYOUT IS THE MAP'S, NUMBER FOR NUMBER**, read out of
`Assets/TumbangPreso/Editor/MapKit/IlalimNgTulayBuilder.cs` and `docs/Ilalim_Ng_Tulay.md` rather
than eyeballed off a screenshot. A player who has thrown on that map should recognise the frame:

| Feature | In the map | In the loop |
|---|---|---|
| Carriageway | 14 m, \|x\| <= 7, flat mid-value asphalt | Same, with two potholes at \|x\| = 3.4 and a manhole |
| Kerbs | \|x\| 6.65 to 7.0, 0.15 m, **painted white on top: they ARE the east and west chalk** | Same; the white kerb line is the court's long sides |
| Chalk | North and south lines at z = +/-7, throwing lines at z = +/-8, the can at the origin in its circle | Same, hand-chalked, wobbling, doubled where gone over |
| Pavements | \|x\| 7 to 11, 0.212 m up | Same; every prop lives here |
| Guideway | 10.5 m deck over the road centre, soffit 8.0 m, top 9.04 m, two tracks at x = +/-2.35 | Same: it is the ceiling of every street-level frame and it throws the diagonal shadow band across the road |
| Columns | 1.4 m square at x = +/-4.45, z = +/-10 and +/-19, T caps under the deck | Same, with BAWAL UMIHI DITO placards |
| The consist | Three city cars, 15.6 m, 18 m/s, every 24 s | Same speed; seen from the pavements above the deck edge, its shadow and window light sweep the road |
| PC Express | The authored showroom on the WEST wall, centred at z = 5.5 | Same place: red lightbox, white lettering, glass front, the RGB overclock pad on the pavement outside it at (-9, 5.5) |
| Bridge hoop | Ring beside the south west column at (-8.9, -10), rim about 3 m up | Same place, as set dressing (the throw no longer uses it) |
| Pisonet, pares cart | East pavement, x 9.7 and 8.8 | Same side, same order |
| Poles and wires | Utility poles on both pavements, spans over the street | Same |
| Beyond the walls | The road continues to intersections at \|z\| = 31, traffic lights, parked cars, a jeepney north | Same, drawn smaller and flatter with distance |

### 2.1 The signs: invented, researched, and PH

The map's current signs are placeholders (🧑: *"not final so js imagine it on ur own"*). Research
behind the new set:

- **Privilege signs.** Sari-sari stores carry a sponsor's panel (soft drink, telco, soap) with the
  store's own name in a band under it; the name is usually the owner's, or a family joke
  (*"2 Sisters"*, *"3 Brothers"*). [Privilege sign](https://en.wikipedia.org/wiki/Privilege_sign),
  [Sari-sari store](https://en.wikipedia.org/wiki/Sari-sari_store). The owner's second photo is
  exactly this: brand panel, white name band, painted brand script on the wall under the window
  grille, and a red-and-yellow striped kerb.
- **Tarpaulins everywhere.** Cheap, fast, weatherproof inkjet tarps are the default sign medium,
  maximalist, often plain black letters on a telco colour, and they outnumber everything else.
  [Grafiko Filipino](https://www.researchgate.net/publication/381808665_Grafiko_Filipino_A_Study_on_Philippine_Visual_Language_and_Graphic_Design_Culture),
  [Inquirer, *Subliminal Strokes*](https://lifestyle.inquirer.net/35342/subliminal-strokes-sign-painting/),
  [Neocha, *Sign of the Times*](https://neocha.com/magazine/sign-of-the-times/).
- **Hand-painted lettering** survives on older shops, tyre shops and carinderias: a "Gothic" sans
  with pinched corners from de-rounded brushwork and flicked stroke ends, irregular on purpose
  ("karaoke aesthetic"). [Eye on Design](https://eyeondesign.aiga.org/filipino-sign-painting-typography-is-awesome-and-other-insights-from-the-hardworking-goodlooking-duo/).
- **Jeepney route boards** are neon letters on black, endpoints split by a dash (*"CUBAO -
  GILMORE"*). [Cubao Free typeface](https://www.behance.net/gallery/66665127/Cubao-Free-Display-Typeface),
  [3D Academy on route signs](https://3d-universal.com/en/blogs/understanding-jeepney-route-signs-in-manila.html).
- **Pylon stacks.** The owner's first photo: a steel frame of six stacked lightboxes, each a
  different business in a different face, some faded, phone numbers on half of them.
- **Banderitas** stay strung across a street long after its fiesta.
  [Discover Philippines](https://www.discoverphilippines.org/p/the-filipino-banderitas).
- **Gilmore is the computer strip**: PC shops since 1997, clustered at Gilmore and Aurora.
  [The Urban Roamer](https://www.theurbanroamer.com/the-it-hub-in-gilmore-avenue/).

The street's signs, all fictional except PC Express (which the map already carries):

| Where | Sign | Kind |
|---|---|---|
| West, z 5.5 | **PC EXPRESS** | The map's authored showroom: red lightbox, white letters |
| West corner, z -14 | A **pylon stack**: GILMORE TECH HUB / RAM SSD GPU MURANG PRESYO / CP REPAIR UNLOCK / PA-XEROX PA-PRINT / DENTAL CLINIC with a number | Six lightboxes in six faces, the owner's photo |
| West, z -4 | **COMPUTER REPAIR** blade sign, **LAPTOP SA MURANG HALAGA** tarp | Tarp, black letters on yellow |
| East, z -12 | **ALING NENA STORE** under a red soft-drink privilege panel (a fictional *"KOLA"*), red-and-yellow striped kerb in front | The owner's sari-sari photo |
| East, z 3.5 | **PISONET, P1 = 5 MINS** | Hand-painted board |
| East, z -5 | **PARES MAMI, 24 HRS**, an A-board | Hand-painted |
| East, z 12 | **VULCANIZING** painted on a tyre, **WATER REFILLING** tarp | Hand-painted, tarp |
| Column | **BAWAL UMIHI DITO**, and a barangay tarp: **LIGA NG TUMBANG PRESO, BRGY. MARIANA** | Placard, tarp |
| Overhead | Faded banderitas across the street at four spans | Bunting |
| Jeepney north | Route board **CUBAO - GILMORE**, neon on black | Route board |

⚠️ **NO BLUE IN ANYTHING DRAWN IN CODE** (`CLAUDE.md` § 6.4, the method). PC Express keeps its red and
white only. The map's mint concrete becomes a warm sage (red channel at or above blue) under golden
light; the telco tarps use their green and yellow. The characters' own palettes are authored art and
are never touched.

---

## 3 · The loop, beat by beat (30 fps, 1620 frames, 54.0 s)

`src/phaister/beats.ts` is the timetable. If this table and that file disagree, the file is right.

⚠️⚠️ **54 s, NOT THE SHARED 42, AND EVERY ATTEMPT HAS A REACTION.** 🧑: *"not make it too fast and
give each scene time to breathe"*, *"make her more expressive and like make it showcase her
personaliyt"*. At 42 s the whole round and the ultimate had 21 s and read as a rush. Her loop runs on
its own clock (`src/phaister/time.ts`); every attempt is set up, lands, and is followed by her acting
about it before the next begins. Those reactions are the personality, taken from her lore: a
showwoman who *"enjoys the audience almost as much as the contest"* and *"wishes [Nemu] would at least
look surprised"*.

Coordinates are the map's: x east, z north, the can at the origin, the south throwing line at
z = -8. Phaister's mark is (-0.9, -8.7), just behind the line; Nemu floats beside the can.

### Act I · Ang Tira (the throw). Golden hour.

⚠️⚠️ **REMADE 2026-09-24 BECAUSE IT DID NOT MAKE SENSE.** 🧑 on the first master: *"why does she throw a
slipper and it just floats on her hand and then it flies to a wall FOR NO REASON"*, *"the place she picks up
slipper from isnt really the right place"*, *"thoroughy check out direction of it coz it dont make sense"*.
The first version levitated the slipper, flung it the wrong way through the bridge hoop, round a column
and back (3.4 s in the air, most of it a few pixels wide), and the ricochet onto the pad happened off
screen, so the run to the pad had no visible reason. **The rule it broke, for every future beat: each
link of cause and effect must be SEEN.** A trick has to be one anyone reads at a glance; a thrown thing
flies like a thrown thing (under a second, a real arc, real bounces); and where an object comes to rest
must be on screen if a character later goes there for it.

| Time | Beat | What happens | Camera |
|---|---|---|---|
| 0.0 | calm | The street at golden hour, the loop seam: Phaister on the south line with her tsinelas, Nemu by the can down the court looking at nothing, Kuro bobbing at her shoulder (happy for a moment). Banderitas, PC Express, the column pair | Low, south of the line, looking up the court under the deck; a slow arc |
| 3.0 | `dare` | She points the tsinelas back over her shoulder at Nemu: *you*. Nemu does not look. Kuro gives a cat smile | Over her right shoulder, Nemu held in the frame |
| 4.4 | `boast` | She turns to US: a hand flat on her chest, chin up, then an off-axis flourish and a little dip. *Watch this.* | Front on, medium |
| 5.8 | `blind` | She pulls her brim down over her eyes with her free hand: *no looking* | In on her face |
| 7.4 | `twirl` | Eyes still covered, she twirls the tsinelas round one finger at her hip, taking her time | Pulls out to THE WIDE: low, south east of her, looking 36 degrees west of north. Her in front turned to us, the can and Nemu down the court to the right, the overclock pad and PC Express beyond her to the left |
| 10.15 | `release` | A flick behind her back, her head never turning. It lobs over her head down the court | The same wide, held |
| 11.1 | `klang` | **KLANG!** One impact frame; the can flies. Nemu does not blink. Kuro goes dizzy (x x). The slipper kicks up off the can to the north west, bounces twice on the road, hops the kerb and skids to rest on the overclock pad in front of PC Express (12.3) | The same wide: the whole ricochet is in frame |
| 11.4 | `tada` | Arms open to the room, still not having looked | |
| 12.5 | `sag` | She turns and looks back up the court at Nemu. Nothing. Her shoulders drop | A slow push |

### Act II · Ang Takbuhan (the run)

| Time | Beat | What happens | Camera |
|---|---|---|---|
| 13.4 | `run` | She sprints for the tsinelas, to the spot on the overclock pad where the wide shot showed it stop | Running alongside, ahead of her, PC Express and the signs behind her |
| 13.6 to 14.8 | `reset`, `upright` | Behind her, Nemu drifts to the can and sets it upright with one finger | In the background |
| 15.5 | `scoop` | The slide onto the pad; OVERCLOCK | Low, in front |
| 16.4 | `block` | She turns for home and Nemu is already in the lane | From the carriageway: a side-on face-off |
| 16.8 to 18.9 | `warn`, `juke` | The train's shadow and hum; she feints, Nemu mirrors her without effort; the overclock ribbons trail her | Pushing in, dutching |
| 19.0 | `tag` | Nemu reaches; the train roars over, window light down the pavement | |
| 19.15 | `blink` | Shadow Blink: gone, no flourish. Nemu holds a curl of smoke. Kuro's eyes sparkle | |
| 19.8 | `safe` | Thrown open across the south line, arms wide | From the west pavement, south of the line |
| 21.0 | `deadpan` | Nemu turns, slowly, and looks at her. Nothing. Her head tips: *seriously?* | Two-shot, slow push |
| 22.3 | `stomp` | A little stamp of frustration, then arms folded, face turned away: the huff | |

### Act III · Grand Coven

| Time | Beat | What happens | Camera |
|---|---|---|---|
| 23.6 | `resolve` | Brim pulled down | East of her |
| 24.4 | `knuckles` | Hands together at her chest; then she walks to the middle of the line | The lens walks with her |
| 26.4 | `rise` | Off the road, arms low and out | Rising with her, round to the north |
| 27.4 | `ignite` | Her own eyes go hot pink, two flickers then full; the chalk court burns pink | Dutched push onto her face |
| 28.0 | `sky` | Arms thrown up in a V | |
| 28.5 to 31.0 | `build` | Sixteen frames later the night comes down from the top of the sky and the circle builds in stages, the game's own table, over the whole street | The crane, out from under the deck |
| 31.0 | `stamp` | It stamps: a shockwave, the banderitas whip, the can jumps, the curse closes on Nemu | Shake |
| 31.9 | `well` | *Well?* Palms up | Cut: her |
| 33.0 | `nothing` | Nemu, held in the curse, turns her head. Nothing | Cut: Nemu |
| 34.1 | `kuro` | Kuro shoots up behind her shoulder, sparkle-eyed, mouth an "o": **!** | Snap push onto Kuro |
| 35.2 | `take` | Her double take | Cut: her |
| 36.0 | `clap` | Delight: she claps | |
| 36.8 | `tipK` | And tips her hat to Kuro. Kuro's eyes turn to hearts; then he goes shy behind Nemu | Cut: Kuro |

### The calm · Gabi sa Tulay (night under the bridge)

| Time | Beat | What happens |
|---|---|---|
| 38.2 | `descend` | She floats down onto her mark; the circle keeps turning on the road, dimming; glyphs drift up into the stars |
| 39.8 | `free` | The curse lets Nemu go; she drifts back beside the can and looks at nothing again |
| 40 to 50 | calm | Doodled sigils off a fingertip; a hat tip to us at 43.0 (Kuro's heart eyes again); an ordinary train at 46.0, her hat held in its draught; the slipper floated in a tiny hex at 49.0. Kuro happy, cheeky and sleepy by turns |
| 50.4 to 54.0 | `dawn` | Grand Coven ends: the night lifts, the circle folds back into the chalk lines, the signs go off, the gold returns: frame 0 |

Sound cues, if the hub wants them: dare 3.0 s, throw 10.15 s, KLANG! 11.1 s, ricochet bounces 11.5 s and 11.8 s, skid 12.1 s, overclock
15.5 s, train 16.8 s and 19.0 s, blink 19.15 s, stomp 22.3 s, rise 26.4 s, ignite 27.4 s, night 28.5 s,
stamp 31.0 s, Kuro 34.1 s, clap 36.0 s, train 46.0 s, dawn 50.4 s.

---

## 4 · Safe areas

Same hub, same rules as `README.md` § 5: faces, the can, TRES!, KLANG! and the circle's centre
inside x 240 to 1680 and y 150 to 948; the left column (x < 560), the top band (y < 190) and the
mode card and PLAY (x 1480 to 1860, y 590 to 1030) stay quiet. Looking north up the street puts the
west pavement (PC Express, the pylon) under the left column and the east pavement under the mode
card, which is why the busiest lettering is placed far down the street where it is small.

---

## 5 · How it is built

- `src/phaister/view.ts`: a real camera (position, yaw, PITCH, roll, focal length) with polygons
  clipped at the near plane, because the crane looks down and the tracking shot runs a metre from the
  facades. Figures behind the lens are culled rather than projected (projected, one drew as a giant
  hat across the frame).
- `src/phaister/map.ts`: the map's numbers. `set.tsx` draws the street from them: the ground and the
  chalk court, the rows (mid-rises behind single-storey glass shop boxes, PC Express), the deck with
  its ribbed soffit and the train, then every near thing (columns, poles, lamps, banderitas, cars,
  the jeepney, the hoop, the signs, the stalls) depth-sorted together with the cast every frame. Every
  surface colour is written once, at golden hour, and passes through `lit()` (two hard sun bands) and
  `tone()` (the night), so the street changes hour as one.
- `src/phaister/signs.tsx` and `lettering.tsx`: the invented signs, hand-lettered by a brush skeleton
  with wobble, outline and drop shadow. 🧑, on the map's shop signs the same day: *"dont outright use
  fonts its so ugly"*. The lettered SOUND words (TRES!, KLANG!, Kuro's "!") keep the display face, as
  Zack's TUMP! does.
- `src/phaister/beats.ts` (the timetable), `perform.ts` (Phaister), `nemu.ts` (Nemu, Kuro, the can),
  `camera.ts` (the shot list), `fx.tsx` (hex, trail, TRES!, KLANG!, the one impact frame, speed lines,
  overclock ribbons, train light, rift, smoke, Grand Coven, corona, shockwave, shackle).
- ⚠️⚠️ **EVERY POSE IS KEYED BY HAND. NO GAME CLIPS.** 🧑: *"make ur own animation"*, *"zack had his
  own animation made"*, *"it wont be as dynamic if u js reuse animations she has"*. Her glb carries
  sprint, slide and her three casts; none is used. Each performer is a POSE TRACK (key poses with the
  ease into each) plus cycles layered on top (run, walk, breath), so every hand-over is continuous by
  construction. `actor.tsx` gained clip-to-pose blending (`clipMix`), kept for later loops, unused here.
- The cast: `team-phaister.glb` with `tsinelas_tsinelas.glb`, `team-nemu.glb` (palette from
  `person_team-nemu.tres`), `pet-nemu-ghost.glb` (Kuro) and `lata_metal.obj`, all through
  `src/three/actor.tsx`. `Board.tsx` is the test board: frame 2 is every one of Kuro's faces.

### 5.1 Kuro, cuter and more expressive, in the game too

🧑: *"can u make kuro's expressions cuter both ingame and in the animation"*, *"make it more expressive
hehe"*. The board found three faults on his restored body: his blush used the eyes' violet palette
slot, so he read as four eyes; his resting mouth was a pupil-sized square; and three of eleven idle
gestures had a face at all. `tools/cute_kuro.py` fixes it on the model itself (idempotent; the rage
form and his motion are digested before and after and must not change):

- blush to palette slot 13, the warm peach of Nemu's own palette, smaller, out and down to his cheeks;
- a smaller, flatter resting mouth;
- seven new authored parts under `KuroExpressions`, built like the first three: happy `^ ^` eyes,
  sleepy eyes, heart eyes, sparkle glints, a grin, an "o", a tongue.

`GhostPetCompanion.ExpressionFor` is now the one table of faces: CatSmile, GoofyDizzy and ShyPout as
before, plus HappyHop (happy eyes and a grin), CuriousPeek (sparkles and an "o"), SleepySnooze
(sleepy eyes), CheekyGiggle (happy eyes, cat mouth, tongue), HeartbeatPulse (heart eyes),
TwirlSpin (a happy "wheee") and OrbitArc (sparkles and a smile): every gesture has a face.
`KuroIdleClipAuthor.RebindRetainedIdles` rebaked the trailer clips; `KuroFormTests` covers every face
and asserts every named part exists: 11 of 11 passed (`Logs/kuro-form.xml`, 2026-09-24). In the loop
he wears the same parts: dizzy at KLANG!, curious at the blink, the sparkle-eyed "o" of the
punchline, heart eyes at her hat tip, shy behind Nemu, happy, cheeky and sleepy in the calm.

## 6 · Review and checks

The method's § 5, unchanged: the motion audit on every full render, per-beat sheets, the seam
measured, and the PlayMode run in the real hub. Results are recorded here as they land.

⚠️ **The master render stalled, and why (2026-09-24).** Frames 1080 to 1349 (the descent and the
calm's close hat tip) would not render as one chunk: at concurrency 4 it timed out three times with
"Target closed", and at concurrency 2 it froze at its 209th frame with two headless tabs at ~474 s of
CPU and no error. Every frame of that range rendered alone in 2 to 3 s, so no frame is broken. The
cause is accumulation: `drawActor` reads every figure back as a PNG data URL, up to 4096 px square
on a close-up, and a tab rendering frame after frame piles them up until it does nothing but
collect garbage. `scripts/chunks.mjs` now renders each chunk as 45-frame pieces, each in its own
fresh browser (~10 to 16 s a piece), at concurrency 2; `master:phaister` and `draft:phaister` pass
those values. Do not raise them without re-measuring a close-up stretch. Separately, the render's
headless Chrome is set in Windows Graphics settings to the RTX 4050 (it had been on the default).

**Results, 2026-09-24 (the remade throw).** Master: 1620 frames, 1920x1080 at 30 fps. `smooth.py` flags
only the intended frames (the KLANG! impact at 11.1 s and the punchline cuts at 31.9, 33.0, 34.1, 35.2
and 37.3 s); a one-frame camera jump at 19.57 s, where the standoff shot followed her position through
the blink, was found and fixed (`camera.ts`, block shot). Seam: frame 1619 against frame 0 differs by
0.78, below an ordinary frame step (1.14). Shipped copy: crf 23, **35.1 MB** (crf 22 had been 41.8 MB,
over the 38 MB budget), SSIM 0.980 against the crf 17 master.
