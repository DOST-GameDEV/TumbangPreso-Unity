# Phaister's overhaul: direction, every film, every verdict (HERO-10)

The method is `docs/HERO_KIT_METHOD.md` section 7: film it in a match, look at the frames, fix, film again, send versioned mp4s,
record the owner's words here and act on them. The plan is [plan.md](plan.md); the footage is [research.md](research.md).

## 0. The references, looked at again (2026-09-27, second pass)

Owner, while v7 was being reviewed: *"check out thhe plan as wel as the references too and keep on refinining how it looks"*.
The footage was stepped again in the built-in browser (the frames drawn onto a canvas, because the hidden pane does not paint the
`<video>` element itself). What each one says about the v7 films below:

| Reference, moment | What is on screen | What it says to v7 |
|---|---|---|
| Seele 7.9 s | a held close-up of one eye, a violet glint on it | THE EYE shot must SEE HER EYES: v7's close-up put the forming eye over her mouth and the brim over her eyes |
| Seele 8.3 to 8.7 s | violet streaks and butterflies at the lens; her red ribbon whipping across the foreground | the near layer is thin, dark shapes crossing close to the lens, not a butterfly the size of half the frame (v7 1.73 s) |
| Seele 10.2 to 10.4 s | concentric strokes spiralling round the hit | the maelstrom should read as STROKES turning round the eye, not only as bodies orbiting |
| Seele 10.6 s | the impact frame: the whole picture inverted to near white with black ink splashes | v7's "impact" is a dark disc behind the eye; the real one is the whole frame, two tones, for two frames |
| Castorice 2.5 s | tall wavy violet ribbons climbing round her in a column | SURGE has no column; the power has nothing vertical |
| Castorice 5 to 7 s | the domain: the ground turns into a field of tiny lights, a moon in the sky | v7's night is a dim wall; the court itself should light up in small points round her |
| Castorice 23 s | a calm half-lidded close-up with one horizontal light streak across the eye | her face in THE EYE: calm, her slits lit, one streak |
| Castorice 25 s | butterflies with GLOWING edges on diagonals, speed streaks behind | her butterflies read as black crows in play (v7 OMEN wide): the edge and the body need light |
| Castorice 27 s | the final burst's silhouette is a butterfly inside the flash | keep the butterfly flash; it must sit IN the burst, not float as a pink sticker |

## 1. Film v7 (merged HEAD `ba78d5767`, first film after the merge): what the frames show

Both probes green (moved, cursed, exposed; saw the scene, pulled; round clock 89.600 under the whole cutscene).

**VANISHING ACT**
- The hold shows NOTHING on her screen or the court: no aim mark at all at 5 m (the generic ward was not visible on Bayan Plaza's
  paving), and her body just stands (no tell).
- The release: on her own screen a single moth crossed the lens at half the frame's size, pink and blocky.
- The smoke puffs are flat translucent SQUARES (a splat quad seen edge-on), and the arrival's puffs sat in front of her own lens as a
  purple sheet over a third of the frame (1.5 and 1.7 s).
- The arrival crouch is a bow: the torso pitched 22 degrees and the head 20 more, so she lands like a faceplant, not knitting up.
- From the court the swarm is a small spray; nothing marks the path it took.

**MANIKA MISCHIEF**
- The doll leaves her hand at the RELEASE while the clip is still unhooking and pricking it (the prick is at 0.26 s and the throw
  at 0.48 s of a clip that starts on the release): the doll flies half a second before her arm throws.
- Her first-person hands never show the doll during the aim: the tell is invisible on her own screen.
- Held, the doll in her first-person left hand reads (orange, the victim's colour, X eyes). Keep.
- Not built: the doll of themselves at the victim's screen edge. Not filmed: the miss.

**SPOTLIGHT PIN**
- The film's "her screen" for the pin was a camera 3 m behind the taya at head height: her back filled the frame. Useless.
- The sigils that sweep the cone do not show at 7 m at all. The light pins do not show.
- The moonlight reads as a pale glass tube round each victim.

**OMEN, the cutscene**
- 0.4 s: a sunny plaza for the first half second (the night arrives at 0.73).
- THE EYE: the brim cuts across her eyes, the eye forms in front of her MOUTH, and at 2.73 s it covers her whole face. A black
  butterfly fills half the frame at 1.73 s.
- THE THROW: over her shoulder her hair and brim fill a third of the frame; the "impact" is a dark disc and a pink butterfly sticker.
- The ending lands on the eye and the maelstrom with nobody in it: the real targets are never shown. The eye lands at a fixed spot
  4 m ahead, not where she aimed.

**OMEN, in play**
- The hold shows no height (plan section 4.4; the owner must see how high it will hang before he lets go).
- The maelstrom's butterflies read as black crows on the dimmed court.
- No screen veils (plan rows 12 and 13).

## 2. v8, what changes (in the order it was built)

| # | What | Why (the v7 frame, or the reference) |
|---|---|---|
| 1 | Her own aim picture for VANISHING ACT and OMEN (`PhaisterAimSigil`): her lunar sigil written clockwise on the court where she will arrive, three moths circling it; for OMEN the ring on the court, a ghost of the eye at the height it will hang and a line of lights down to the court | v7 hold: nothing on screen |
| 2 | Her body's tells while she aims, one pose per skill (`hero-phaister-*-aim`): wrists crossed at her chest (VANISHING ACT), the doll up at her chin being pricked (MANIKA MISCHIEF), one hand up measuring the height (OMEN) | v7 hold: she just stands |
| 3 | Soft smoke (`Shaders/SoftPuff`): round, frayed, lit from nowhere, never a quad; none in front of her own lens; a trail of it along the swarm's path | v7: squares, a sheet over her lens |
| 4 | Her own screen never gets an insect at the lens: anything within a metre of her eye shrinks away, and four small moths cross the frame's edges instead | v7 1.1 s |
| 5 | The arrival re-keyed: revealed crouched and upright, arms wrapped round herself, rising into the hand on the brim | v7 1.4 s: a bow |
| 6 | MANIKA: the prick moves into the hold, the release clip is the throw itself (the flick at 0.08 s, the doll leaves with it); the doll is in her hand, both views, all through the hold | v7: the doll left 0.48 s before the throw |
| 7 | MANIKA: the victim sees a doll of themselves at the edge of their screen, its head being twisted, while the curse lasts | plan 4.2, not built |
| 8 | SPOTLIGHT PIN: a crescent stroke wipes across the cone left to right, dark body and a lit middle (Paete's brush-stroke lesson: light alone vanishes on a pale court); bigger inked sigils | v7: invisible |
| 9 | SPOTLIGHT PIN: moonlight as a soft shaft (`Shaders/MoonShaft`): bright in the middle and at the court, soft at its sides, streaks falling down it, a ring of her runes at their feet, the pin standing in the court | v7: a glass tube |
| 10 | OMEN in play: the butterflies' bodies glow and their edges catch light (Castorice); screen veils on her screen and a caught player's (plan rows 12, 13); a black butterfly perched on every player in reach through the cast, flying into the maelstrom when it opens | v7: crows; no veils |
| 11 | The probe films the holds long enough to judge the aim, the pin from over her shoulder, and the doll's miss | v7's framing |

## 3. OMEN's cutscene, v8 (5.0 s): what it says and why each part is there

One sentence: **"The omen pours out of her, she throws it, and it marks everyone it will take."** The travelling thing is the
butterflies, flowing left to right in every shot. Body keys and storyboard shots: `tools/author_ultimate_intros.py` `phaister()`;
pieces: `HeroIntroductionScene.Phaister.cs` (stage, eye, butterflies), `.PhaisterBurst.cs` (the air), `.PhaisterMark.cs` (the
targets, the cameras, the impact frame, the grade).

| Beat | Time | Picture | Camera | Sound (`sfx_ult_theme_phaister`) | Answers |
|---|---|---|---|---|---|
| SURGE | 0.00-1.30 | her night falls at once; the court lights up in small points out from her feet; she rises, ribbons whipping up; butterflies pour out and wheel clockwise; five ribbons of her light climb round her; three strokes whip round at her hands; four butterflies cross the lens left to right | orbits her CLOCKWISE seen from above, pushing in (v7 orbited the other way) | cut-in whoomph, rising wind, circling wings, a low glass chord as the column climbs | Castorice's domain and column, Seele's streaks, Castorice's near layer |
| THE EYE | 1.30-2.40 | from below her chin: her whole face under the brim; her ink eyes light violet (1.38), a glint (1.44), a streak across both; butterflies stream left to right into her palms, low in front of her chest; the eye forms tiny, glitching, pulsing, growing; her head tips, smirking, and the left eye's light blinks off (the wink, 2.2) | a slow pull back | the glint's tick, wings sucked in, the glitch climbing, a throb, her motif on the wink | Seele's eye close-up, Castorice's calm face, Hu Tao's cheeky beat |
| THE THROW | 2.40-3.20 | she hurls it; it streaks left to right to WHERE SHE AIMED (the commit's aim, height and all), a stroke of her light behind it; lands at 3.00: two frames of the whole picture inside out with her ink splashing from the eye; the butterfly burst; the shock ring; the ring and her runes written round it | from her right, a little behind, wide enough for her and the spot; easing toward the landing | the throw's climbing whoosh, two frames of silence, the whump, the chord, a tearing crack | Seele's impact frame and stroke, Castorice's emblem burst |
| THE MARK | 3.20-5.00 | the maelstrom starts round the still-unstable eye; one black butterfly goes to EACH real player in reach and settles over their head; each flinches, turns to the eye and leans away from its draw | one crane round the eye, clockwise, rising until every marked player, the eye and her are in frame; never nearer than 2.3 m to a body | the maelstrom's wind and wings, a flutter and a glass tone as each mark settles | the method's section 6 (the ending on the real targets) |

**Why it does not show the pull.** OMEN keeps its 2.2 s cast after the hand-back (the owner on ABILITY-2's numbers: *"those can
stay"*), so a cutscene that pulled them in would be followed by play pulling them in again, which the method forbids (section 6,
rule 7: never shown twice). The black butterfly is the omen itself in the folklore she draws on: it lands on the one who will be
taken. So the cutscene ends on the omen landing on the real players, and play continues from exactly there: the eye still
unstable at the size the live cast starts at, the marks already over their heads (`PhaisterOmen` starts them perched), the
maelstrom already turning (the cast's butterflies start on their orbits, not out of her sleeves). They can still run for 2.2 s.

**Length.** 5.0 s (was 4.0; the owner: *"try with shorter first like 3-5 seconds, we adjust if needed"*). Protocol 62: every peer
derives the shared phase's length from these tables.

## 4. v3, the voodoo kit in a match: films v12 to v14 (2026-09-28)

`PhaisterKitPlayProbe.FilmHerVoodooKitInAMatch` (her screen, then over the caster's shoulder; the court) and
`FilmHerVoodooOnAVictimsScreen` (the victim's own screen with the HUD; the court). Videos `Logs/phaister-share/`
`phaister_voodoo_kit_v14.mp4` and `phaister_voodoo_victim_v14.mp4`, sent 2026-09-28; `phaister_voodoo_victim_v16.mp4`, sent 2026-09-29. The owner's verdict is owed.

| Film | What the frames showed | What changed |
|---|---|---|
| v12 | the thread reads across the court (crimson and violet cords with crawling stitches); her reach barely reads on her small arms and the trunk turned the WRONG way for a right-hand reach; on her screen the hand stayed low and the thread came from below the lens; the marks over the victim were too small to see; the thread to a victim's own eyes stood as a column through their lens; the HEXED flair put dizzy stars and a word block in the victim's lens; the HUD was not in the victim's frames; the staged snap reached the taya (the nearest-her-facing rule was right, the staging wrong) | the trunk turns so the reaching shoulder leads, she leans back and braces; her first-person hand rises into view and the thread leaves it; a victim's own screen sees the thread arrive below the middle of their view; the knot and the button twice the size; the HEXED flair dropped for a stitched band across the eyes seen by everyone else; the HUD composited into the victim film; the snap staged with the victim alone in her cone |
| v13 | DRAIN reads: the lean, the cord, the knot over them, the wring; the snap frays and falls back; the phantoms lie among the real slippers; HEX's arm, raised to head height, is hidden behind her head and hair from every side; the HUD canvases were on the Default layer and the film's UI camera culled them | HEX reaches OUT and a little up with the elbow wide, the doll in front of her chest; the UI camera draws whatever layer each canvas is on |
| v14 | the victim's HUD shows the DRAINED card and the crossed pins beside the reticle; HEX's arm still reads weakly on this rig (the head is most of the silhouette): the violet cord and the button carry the read | sent for the owner's eye |
| v15 | the owner on v14's victim screen: *"doesnt even connect"*; the thread stopped in the air in front of the victim | the thread runs off the bottom of the victim's view into them, DRAIN low and wide, HEX higher and tighter, tapered for its last half metre |
| v16 (2026-09-29) | HEX's violet thread leaves her palm and runs down off the bottom of the victim's view into them, from the lock through the 2 s hold; the HEXED card follows; drained, hexed and phantoms all true (`claims.csv`) | sent, `Logs/phaister-share/phaister_voodoo_victim_v16.mp4` |
| owner on v16 (2026-09-29) | *"dont make the pulling thing look like a physical line i want it to look like sucking aura or smth"*, *"it sucks rn ur implementation"*, *"i want u to make her hold up her voodoo too towards the person when markingt hem"* | the thread is gone: `VoodooSoulDraw` sucks their aura out of them in wisps (`Shaders/VoodooWisp`) into the ultimate's doll at hand size, held out in her left hand in both views; the body and first-person reach poses rebuilt round the doll (v17) |
| v17 | the doll is in her hand in both views and wisps come off the victim; but nothing showed for the first second (the fade was tied to distance travelled), the wisps crawled, DRAIN's read as red confetti, nothing glowed round the victim, the doll sat at her palm by her cheek, and from behind DRAIN's lean read as falling over | fade by age; a faster suck; bigger, softer wisps; a glow clinging round the victim; the doll pushed out past her palm; half the lean |
| v18 | the gulp on the mark reads right (one flow ripped out of them into the doll); the hold still read as scattered petals, because most wisps lingered | most wisps flow steadily (a few cling), twice as many, a tighter DRAIN twist, a stronger glow round the victim, the doll further out |
| ult film v4 (2026-09-29) | the new cutscene (plan 9.8b): the offering, THE CIRCLE as a glowing layered disc (`Shaders/VoodooCircle`: fangs, rim, runes, star, stitches, void) with pins and lightning, the monster lowered out of it on four strings, the drop, the puppet; clock frozen through it. Faults seen: the offering close-up is all brim with the doll over her face; the night reads grey-blue, not a replaced world; the eye in the void does not show; the landing pose reads as toppled | sent, `phaister_voodoo_doll_ult_v4_small.mp4`; the four faults are the next pass |
| ult film v5 (2026-09-29) | the offering now shows her face and grin with the doll in her left fist at her side; the night reads as a replaced violet sky with an ember horizon (`Shaders/VoodooNight`, unlit, no fog); the eye opens in the void as an almond with thick stitched lids, a hot fill and an ember slit; the landing slumps. Faults: the dark floor z-fought under Bayan Plaza's raised paving (only a dithered band showed); in THE PUPPET's close-up the lens stood on the opponents' side, which was behind its ear when they stood off its shoulder | the floor lifted onto the court's own surface (`Slipper.GroundY`); the close-up lens between its front and its look, the head (and a little of the chest) turned to the real opponents |
| ult film v6 (2026-09-29) | the four v4 faults gone: her face and the doll in the offering, the replaced night on sky and floor, the eye, the slumped landing, the puppet's face (button, X, grin) filling the close-up; clock frozen (89.520 at both ends) | sent, `phaister_voodoo_doll_ult_v6_small.mp4`; the owner's verdict owed |
| owner on v6 (2026-09-29) | *"I like the eye but the execution is weird"*, *"dont make her raise it up"*, *"i want her to look like she starts flying and is casting"*, *"i want the handheld vodoo to look diff from ult too"*, *"show transition from day to dark too"*, *"when the eye opens the theres like a lot of vfx"*, *"I want the portal (eye) to be diff from the thing that controls it too"* (a marionette-control photo), Flins as the reference, *"phaister's ult does not have a terrifying feeel at all eh"*, an ink impact frame | plan 9.8c, THE PUPPETEER |
| ult film v7 | the day dies on screen; she rises and casts; the seam splits and tears; the eye locks on the lens with an ink frame; the gloves pull the doll out upside down and lower it; its head comes round before its body; the drop and the puppet with ink frames; in play the portal shuts and the control hangs over its head. Faults: the dome's cap showed at t 0; her close push-up burned pink (the light under her too hot, the lens too close); the open eye read as a flat pink symbol; the burst was hidden inside the disc; the gloves read grey | the dome's edge starts above the cap; the light at a third and the lens further back; the eye a dark bloodshot white, an iris and a black slit; the lens thrown out sideways at the burst; the gloves lit from inside |
| ult film v8 | the burst reads (rays, lightning, the circle seen racing out); her new handheld doll reads in the reach review. Faults: a glove's sleeve stabbed down through the lens while the glove was still inside the eye; the eye still flooded red and the slit read as two pale bars | sleeves aimed from above the eye |
| ult film v9 | the sleeves right; the slit dark; the eye's white still washed red by the bloom of the iris and lids | the iris and lids under the bloom threshold, a wider cat's slit |
| ult film v10 | the eye reads: a dark bloodshot white, a burning iris, a black cat's slit; but a flare at the lock washed it red for the frames before the ink frame | the lock flare dropped (the ink frame is the hit) |
| ult film v11 | every beat of plan 9.8c on screen, clock frozen (89.540 at both ends); `VoodooDollBodyTests`, `PhaisterVoodooKitTests` 8/8; `ChatAndLobbyChromeTests` 7/7 | sent, `phaister_voodoo_doll_ult_v11_small.mp4`; the owner's verdict owed |
| v19 | DRAIN: a crimson aura round the victim streaming off them toward her doll; HEX: a violet cloud off their head drawn across to her; her screen shows the doll held out at them | sent, `phaister_voodoo_kit_v19.mp4` and `phaister_voodoo_victim_v19.mp4` |
| owner on v19 (2026-09-29) | *"this animation dont look that good yet"*, *"lock in thoroughly improve also hthe doll is floating"*, *"soul draw sucks"*; on sound: *"all ur skill sfx suck shit"*, *"can we delete all skill abilities sfx"* | the doll had been pushed 0.42 m past her palm into the air, and the aura read as a smear. Redesign: the doll GRIPPED in her fist (its feet below the top of the hand) and RAISED at them; the victim's own shape drawn as a see-through GHOST (`Shaders/VoodooGhost`, their skinned meshes baked each frame) dragged out of their body toward the doll, fine motes torn off it into the doll; yanked in on the mark, snapped back on a break. Every hero skill sound deleted |
| review v20 (`ReviewHerReachFromEverySide`, a staged front, side, behind and her-screen still set at five moments) | the doll at her chest read as hugged and the hat hid it from behind; no ghost on any frame (a `Graphics.DrawMesh` queued in `LateUpdate` is in no camera rendered earlier in the frame) | the ghost drawn by real renderers; the doll arm raised up and out; a larger first-person doll |
| review v21 | DRAIN's doll up at her face reads; HEX's arm at 56 degrees put the hand and doll behind her head and hat; the ghost drew but barely left their body, so it read as a blob on them; the motes streaked like scratches | HEX's arm at 38; the ghost out 0.3 to 1.25 m, a thinner fill under a wider rim; round motes; the behind camera raised |
| review v22 and films v22 | HEX: a violet see-through copy of the victim steps out of them toward her with motes streaming off it; DRAIN's ghost smeared its head away | the smear capped; films sent, `phaister_voodoo_kit_v22.mp4` and `phaister_voodoo_victim_v22.mp4`; the owner's verdict owed |
