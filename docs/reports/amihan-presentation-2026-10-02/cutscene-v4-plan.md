# Airburst v4: her cutscene, her personality, her VFX

Owner, October 3, after v3.7: *"thoroughly revise cutscene i want her personality to show
in it as well as give it its own feel"*, *"thoroughly revise vfx"*, *"reserach first and
analyze many diff cutscenes in genshin honkai and othber games"*, *"analyze for vfx animation
epxression feel"*, *"look at direction too"*, *"dont copy wanderer completley give her her own
version"*. Earlier: *"it looks like shapes are floating and not vfx"*.

Built on October 3 (see "What was built" at the end); the beat sheet below is the plan it followed.

## Who she is (the brief the cutscene answers)

From [CHARACTER_ORIGINS](../../CHARACTER_ORIGINS.md): grew up behind her family's loom in
Vigan, threw slippers down a cobbled street between kalesas, learned to read the wind off the
abel hung out to air. **Bright, proud of her town, cannot stand a stalled game; she is the one
moving and calling.** Her flaw: **she commits before the play is ready.** Select line:
*Reads the wind. Gets there first.*

**The v3 cutscene does not show any of that.** It is a slow, solemn ritual: she stands,
cups her hands, weaves an emblem, the world darkens, glyphs hang in the air. That reads as a
calm priestess. She is an impatient, cocky street kid. The VFX problem and the personality
problem have the same cause: the cutscene is about a symbol, not about her.

## Research: what was watched and what it teaches

Method as in [research.md](research.md): muted footage, decoded frames drawn into timestamped
contact sheets, no audio heard. Every observation is still-frame evidence at the cited time.
Earlier passes (Jean, Venti, Xianyun, Miks) are in [airburst-v3.md](airburst-v3.md) and
[research.md](research.md).

| Reference | Frames (s) | Direction | Animation and expression | VFX | Feel, and the rule for her |
|---|---|---|---|---|---|
| [Feixiao, HSR](https://www.youtube.com/watch?v=u4y2OfVi_-g) | 1 to 17.5 | Illustrated splash card (2.5); macro of the weapon charging (5.5); extreme close-up of her eyes (7.0); then the action back in the real fight space | The eye close-up is the decision beat: one held look before she commits | The world is swapped for a teal wind tunnel; repeated dash strikes as long streak lines; the hit lands on the real enemy | **One held close-up of intent, then speed.** The wind tunnel is directional and moving, never static |
| [Rappa, HSR](https://www.youtube.com/watch?v=rExCn3RWn4U) | 3 to 15.5 | Title card with the move name (3.0); a long held ready pose (4 to 10.5); then a full style break (12 to 15.5) | The ready pose IS the personality: a ninja hand sign, a cocky lean, a grin | Graffiti: paint splatter, halftone dots, hand-drawn strokes, all screen-space graphic, not 3D objects | **A personal graphic language belongs in screen space** (cards, wipes, panels). That is why her floating 3D glyphs failed |
| [Kimberly, SF6 Critical Art](https://www.youtube.com/watch?v=O3QL657KwQo) | 0.3 to 18.3 | Fast cuts, each a different trick | She thrusts a boombox at the lens (1.6), sprays paint in a face (8.0), teleports with spray cans (4.2) | Spray paint and graffiti tags as her VFX; the finisher frame is her signature tag behind a pose (15.7 to 18.3) | **Personality comes from props of her life and a cheeky trick, and it ends on a signature pose.** |
| [Persona 5 All-Out Attacks](https://www.youtube.com/watch?v=PRNCg6ichcU) | 2 to 35 | Two-colour graphic field (red and black), silhouettes, every character ends on their own pose | Each finisher pose is pure character: Morgana sitting in a chair (20.0), Ryuji's slouch (29.0) | Flat shapes, no particles | **A finish pose in a flat graphic frame, under a second, says more about a character than any effect.** |
| [Varesa, Genshin](https://www.youtube.com/watch?v=VeLsbbUD3VU) | 0.9 to 3.2 | The whole burst is about 2 s: a wrestling-TV entrance stage with her name in big lettering (1.5), speed lines (1.7), she dives off the top of the frame (1.9 to 2.3), straight back to the world for the hit (2.5) | Bright, eager, posing for the crowd | Her colours as speed lines and a lightning slam | **Her hobby/identity is the stage.** A bright, eager character gets a SHORT, punchy burst, not a long ritual |
| [ZZZ ultimates](https://www.youtube.com/watch?v=TeEDsEBhUY4) | 4 to 52 | Letterboxed cut-ins of about 1 s per shot: hero close-up, expression close-up, a huge word ("OBLITERATE") behind the hero (20 to 28) | One expression close-up per ultimate | Flat colour backdrops with rim light; line-art frames (12.0); the damage happens back in the 3D fight | **Letterbox bars say "cutscene" instantly; cut-ins are short and graphic; the hit is in-world.** |
| [Bridget, GG Strive](https://www.youtube.com/watch?v=sDfpNPhFfdU) | 30 to 41 | Super cut-in about 0.5 s (37.0), then the move plays in the fight | Her companion bear Roger carries the joke: the giant bear pummels (40 to 41) | Fire wheels on yo-yos; the bear is the effect | **A companion or prop can carry the personality and the effect at once.** For her: the abel cloth |
| Earlier: Jean, Venti, Xianyun (Genshin), Miks (VALORANT) | see v3 docs | Sheets with bright rims, the world steps back, a cultural motif carries scale | | | Kept: wind as moving sheets and streaks. Rejected now: the darkening veil and the emblem-as-light |

### What every good one shares

1. **Three to five shots, one idea each**, and at least one of them is about the character's
   face or body attitude, not about the power.
2. **Graphic style lives in screen space** (cards, letterbox, wipes, flat backdrops). In-world
   effects are things that move: streaks, cloth, debris, particles.
3. **Props and companions from the character's own life** carry personality (boombox, teddy
   bear, wrestling ring, graffiti).
4. **It ends on a pose**, and that pose is the character's attitude.
5. **Length fits the temperament:** quick characters get quick bursts (Varesa about 2 s).

## Diagnosis of v3.7/v3.8 (films reviewed frame by frame)

| Problem | Evidence | Cause |
|---|---|---|
| Shapes float | Diamond rings, the floor kasikus, the green glory chevrons and four-point star glints in `Logs/amihan-v37-films/airburst-fx/owner` | 3D outlines with no motion source, which read as UI stamps |
| No personality | She stands and cups her hands for 3 s | The story beat is a ritual, not her |
| Too long for her | 5.6 s, the longest gap before play resumes | Built to match Paete (6.5 s), who is a slow spirit-guardian |
| The face never acts | Voxel face is a fixed smile texture | No expression system exists |
| Darkening veil | Grade drops while she weaves | Borrowed from Paete's spirit language; she is a sunny Vigan afternoon |

## The v4 concept

**Sentence.** *Amihan cannot wait: she whistles the monsoon down like calling a teammate, it
arrives as her family's abel cloth, and she lets it fly before anyone is ready.*

**Her own feel:** warm afternoon light (no darkening), quick and bouncy timing, a cheeky
attitude, a cloth companion. Wind is never a symbol; it is something that arrives, pulls,
snaps and passes.

### Beat sheet, about 4.4 s (owner to choose; see questions)

| t (s) | Shot | Her body and face | Effects (all in motion) | Sound |
|---|---|---|---|---|
| 0.00 | **COLD OPEN**, close and low: an abel cloth on a line snaps in the breeze, letterbox bars slide in | She bursts into frame from the side, already running; camera whip-pans with her | Cloth snapping; dust kicked by her slippers | Cloth snap, a quick flute pickup |
| 0.45 | **READ**, medium close-up | Skids to a stop, licks a finger and holds it up; eyes narrow (face swap: squint) | The cloth goes slack; everything holds still for 0.3 s | Silence, one beat |
| 0.85 | | **IMPATIENT:** her foot taps twice on the beat, then a shrug | Still nothing | Two taps |
| 1.15 | **CALL**, punch-in | Two-finger whistle (face swap: cheeks puffed) | The wind ANSWERS: streaks race in along the street from behind camera, dust and cotton running at her; the cloth snaps toward her | Sharp whistle, the wind swell |
| 1.55 | **CATCH**, low fast orbit | She catches the cloth as it tears off the line, spins once, it wraps her arm and trails like a sail | Cloth companion: a long abel band with overlap and follow-through; thin streaks wrapping the spin | Cloth whip, loom knock |
| 2.45 | **CARD**, screen-space cut-in, 0.5 s | Pointing down the lane, hip cocked, grin (face swap: grin or wink) | Flat warm backdrop with a binakol band pattern sliding across in screen space; AIRBURST lettering; no 3D glyphs | Flute motif, three notes |
| 2.95 | **COMMIT**, over the shoulder, down the lane | Her flaw: she throws it early. A short wind-up, then the cloth cracked forward like a whip, both palms driving | The cloth unfurls into the WARP: bands of abel and streaks racing down the lane; camera rides the wind one beat behind | Crack, the release |
| 3.45 | **HIT**, wide on the real lane | | Real players in the fan bowled over (`WindTumble`), cotton and dust blown through, the cloth bands sweep the court | Release swell, cotton puffs |
| 3.95 | **POSE**, medium | Hands on hips (or a thumbs-up), cloth settling round her shoulders like a scarf: *gets there first* | The cloth falls; motes drift past | Final flute note |
| 4.40 | Hand back to play | | Live fan at age 0 | Cut into the live mix |

Rules unchanged: the 60-degree map-wide fan, the 10 m ring, host contact,
`StormSurgeDelaySeconds` 0, play resumes on the hit.

## VFX language for her whole kit (the "own feel" rules)

1. **Three materials only.** Abel cloth (the hero element, solid and woven), air streaks
   (thin, fast, short-lived, always travelling), cotton and dust (particles that are pushed by
   the air). No outline shapes, star glints, rings, glory, emblems or veils in 3D space.
2. **Every effect has a source and a direction.** If it does not move with or away from
   something, it is cut.
3. **Graphic language only in screen space:** letterbox, the cut-in card, a band wipe. Never
   floating in the scene.
4. **Warm grade, never darker.** Vigan afternoon: lift the warm light, add contrast on the
   cloth.
5. **Timing:** anticipation, a short hold, then a fast snap (two-frame holds on impacts),
   cloth and hair follow through for 0.3 to 0.5 s after each move.
6. **Body as face:** voxel limbs act in big readable angles. Add a small cutscene-only face
   set (squint, puffed whistle, grin, wink) as texture swaps if the owner agrees.

Kit pass to match:

| Ability | Revision |
|---|---|
| Airburst live fan | Replace the comb streaks and chevrons with the cloth warp bands and travelling streaks; dust and cotton blown in the fan's direction |
| Featherfall | Done in v3.8: seated float pose and a cotton-white swirl under her soles; next: a short trailing sash from her waist (cloth companion) |
| Drift | Cloth sash trail plus dust kicked from slippers; drop any ring at the heels |
| Whirlwind | Keep the woven wall; remove any static outline; add debris carried along its front |

## Build order

1. Face set (if approved): four textures plus a `CharacterVisual` swap hook used only by the
   introduction scene.
2. Cutscene body: new clips for run-in, finger read, foot tap, whistle, catch-spin, point,
   whip-throw, hands-on-hips (`HeroAbilityClips.Amihan.cs`).
3. Cloth companion: one long `AbelCloth` band with spring follow-through, reused by the
   catch, the warp and the end pose.
4. Camera: five shots with a whip-pan and letterbox (`HeroIntroductionScene.Amihan.cs`).
5. Screen-space card: a UI overlay layer for the cut-in with the binakol band.
6. Strip the remaining v3 density layer (`HeroIntroductionScene.AmihanBurst.cs`): glints,
   flashes, veil.
7. Sound: whistle, foot taps and cloth snaps added to `tools/build_amihan_ult_audio.py`.
8. Films and a frame-by-frame review against the beat sheet before showing the owner.

## Owner decisions (October 3)

1. Length about 4.4 s.
2. Cutscene-only face set approved: squint, puffed whistle, grin, wink.
3. The card reads AIRBURST.
4. She calls the wind with a two-finger whistle.

## What was built (v4, October 3, films r1 to r11)

Evidence: `Logs/amihan-v4r11-films` (frames, cues, timing), videos `Logs/amihan-v4-share/amihan_airburst_v4.mp4` and
`amihan_skills_v4.mp4`. Checks: PlayMode Airburst film, skills film, `AirburstReleasesOnTheHandBackAndLeavesNoFan` and
`UltimateIntroductionProbe` Amihan pass (11/11); EditMode `AmihanAirburstPresentationTests` pass (10/10); 0 logged errors.
The owner has not watched v4 yet.

* **Cutscene, 4.4 s** (`HeroIntroductionScene.Amihan.cs`, `tools/author_ultimate_intros.py` amihan()): the beat sheet
  above as built. A warm Vigan street with the abel on a clothesline; her run-in and skid; the read (free arm out, squint),
  two foot taps and a shrug; the two-finger whistle; the street's wind arriving as travelling streaks with dust; the gust
  torn off the line to her hand and whirled; the AIRBURST card; the commit from behind her over the lane; the release at
  3.85 and a high final shot of her hands-on-hips wink with the thrown players flying. Release 3.85, play resumes 0.55 s
  later (`AmihanStorm.CutsceneTail` unchanged); the live clip `hero-amihan-storm` re-baked to start in the finish pose.
* **Face set** (`VoxelFace.cs`, `Editor/AmihanFaceAuthor.cs`, `Resources/Models/AmihanFace`): squint, whistle, grin and
  wink are baked copies of her own `head-mesh` (old eye and mouth blocks reused as the new blocks, vertex count and layout
  unchanged), each drawn by its own skinned renderer beside the copied head. The first implementation, a card laid over
  the face, read as a box (owner: *"theres a box on the face of amihan"*); swapping the head's mesh stopped rendering.
* **Card** (`HeroIntroductionScene.AmihanBurst.cs`, `Shaders/AmihanCardText.shader`): letterbox, a flat teal backdrop
  behind her, cream and gold stripes, AIRBURST in Darumadrop One behind her shoulders (depth tested), torn away at 2.86.
* **No woven cloth anywhere** (owner on r4/r5: *"not a fan of the sash shit"*, *"refine her vfx in a diff way"*): the live
  fan's cloth front, skirt and sashes, the Drift sash and the Whirlwind's woven wall are hidden; the fan's mass is a dust
  and cotton wave rolling along the front; the chevron burst and sigil flash are gone; cotton tufts are round puffs.
  The cloth on the clothesline is the only cloth left, as a street prop.
* **Featherfall**: her own seated float (`AmihanFlightPose`), a cotton-white swirl under her soles, three wider updraft
  strands at her sides, cotton lifting past her; no sash.
* **Sound** (`tools/build_amihan_ult_audio.py`): the theme re-timed to 4.4 s (steps, skid, taps, whistle, the wind's
  answer, the rip of the card, the release crack, a bright note on the wink). Provisional, not heard by the owner.

Open: the face blocks are small at mid distance; the read pose's out-flung arm is short on her body; Drift and Whirlwind
were not refilmed after their cloth was hidden; the victim's own blown camera is still unfilmed.
