# HOME background scene: Phaister under the tulay

The second per-hero HOME loop, after Zack at Sa Bubong (`README.md` in this folder). Source is
`ArtSource/home-scene/src/phaister/`, composition `PhaisterScene`, 1920x1080, 30 fps, 42 s.
The method is `docs/HOME_SCREEN_ANIMATION_METHOD.md`; this file is the research, the design and
the numbers. 🧑 2026-09-24: *"i decided to make diff character lobby screens and the current one
will js be random"*, *"thoroughly think abt how to give phaister her OWN animation that doesnt
look too similar to zack's"*, *"reserach first"*.

## 1 · Research, and what each finding decided

| Source | Finding | What it decided |
|---|---|---|
| `LORE.md` hero table | She *"enjoys the audience almost as much as the contest"* and *"treats a good setup like a performance whose reveal should arrive at exactly the right time"*. Deliberate flourishes, readable sigils, an open finish. | Her hero moment is a **trick performed for us**, not an attack. |
| `docs/CHARACTER_ORIGINS.md` | Grew up on **Capul, Northern Samar**, *"listening to stories about a moon that could disappear into a serpent's mouth"*. Plays at **Ilalim ng Tulay** under the rail line, where the audience *"was difficult to impress"*. Select line: *"Sets the stage. Lets you discover the trick."* Her lunar writing is fictional, not Inabaknon script. | Her place is the tulay at night, the moon is in the sky, and her sigils are the game's own ward geometry, never a real script. |
| [National Museum, *Abaknon and their view of the world*](https://www.nationalmuseum.gov.ph/2021/10/29/abaknon-and-their-view-of-the-world/) | *"lunar eclipse (bakunawa) occurs because the moon is swallowed by a snake, that is why old folks would command the snake to let go of the moon."* | The Turn of her trick is an eclipse: the moon is swallowed. |
| [Bakunawa, Wikipedia](https://en.wikipedia.org/wiki/Bakunawa) and [Esplanade, *Bakunawa and the Seven Moons*](https://www.esplanade.com/offstage/arts/bakunawa-and-the-seven-moons) | In the wider Visayan telling, people **bang pots and pans** and shout until the serpent lets the moon go. | **The can is the noise.** Knocking the lata down is what frees the moon. This ties tumbang preso to her Capul memory with no invented belief: the noise-making is the documented response, and a tin can on a street court is the pot. The serpent is never drawn as a monster (`LORE.md`: no threat to the neighbourhood); the eclipse is a shadow that bites the moon and lets go. |
| [No Film School on *The Prestige*](https://nofilmschool.com/how-the-prestige-is-a-magic-trick) and [the novel's three acts](https://en.wikipedia.org/wiki/The_Prestige_(novel)) | A trick has three acts: **the Pledge** (show something ordinary), **the Turn** (make it do something extraordinary), **the Prestige** (bring it back). | Her loop is built in those three acts (section 3). |
| [Misdirection (magic), Wikipedia](https://en.wikipedia.org/wiki/Misdirection_(magic)) | The performer directs attention to one thing to hide another; the audience looks where the performer looks. | She looks UP at the moon and the camera follows her gaze, and the train is the curtain: the trick happens while every eye is on the train. |
| `ASTRA.md` 1F | *"SHE PERFORMS"*: every cast passes through an off-axis FLOURISH and finishes front-on, chest open, *"held for the room"*. The blink is her one exception: no flourish. Grand Coven has the longest anticipation of the six kits: 16 frames between the arms reaching the sky and the night coming down. | The sigil is drawn the long way round; the reveal is front-on and held; the vanish has no flourish; the night comes down 16 frames after her arms reach up. |
| `docs/Ilalim_Ng_Tulay.md`, the shipped map (`IlalimNgTulay-native-v37.png`), [Gilmore station](https://en.wikipedia.org/wiki/Gilmore_station_(LRT)) | LRT-2 is an elevated guideway along Aurora Boulevard; Gilmore is the computer-shop strip. The map: the guideway runs ALONG the road on T piers, a pisonet and a pares cart on one pavement, a train every 24 s. | The set looks DOWN the road under the guideway, and a train crosses in the loop twice. |
| [Long take, No Film School](https://nofilmschool.com/long-take), [Lensrentals on one-shots](https://www.lensrentals.com/blog/2025/05/breaking-down-one-shots-in-cinematography/) | An unbroken shot reads as determined and deliberate and lets the viewer watch every move uninterrupted; its value is the choreography. | **Her whole loop is one shot.** A trick shown with no cut is the one with nothing hidden in the edit; Zack's is cut-heavy, so this is also the clearest single difference between the two. |
| [Persona 5's UI panel](https://personacentral.com/persona-5-panel-concept-development-ui/) | Lighting changes fluidly to lead the eye: the high-priority space is lit, the rest drops low. | A **follow spot**: the street is dim and a warm pool of light rides with whoever the stage belongs to, her, then the empty can, then her again. It is also stagecraft, which is her. |
| Valorant home screens ([Clove, E8A2](https://www.youtube.com/watch?v=9pE5Jd6sdfc); [loops are plain MP4s](https://dotesports.com/valorant/news/how-to-customize-your-valorant-home-screen-with-custom-wallpaper)) | One agent per act, each in their own place and mood, pre-rendered as a looping video. | Confirms the per-hero loop and the random pick; the Zack loop's research (`README.md` § 2) still holds for the general rules. |
| `UiTheme.HeroWitch` `e828c5`, `HeroWitchBright` `f444d4` | Her accent in the shipping game. | Her sigils, smoke and eye glow are exactly these. |
| `team-phaister.glb`, read with a script, not guessed | Eyes are palette slot 8 on the face plane (bind z 0.160) at y 0.465 to 0.497 (the top row is her lashes); the mouth is the same slot lower down. The hat reaches y 1.0, so she is 1.27x Zack's height. | `EYE_BAND['team-phaister']` is 0.455 to 0.505; her figure camera frames the hat. |

## 2 · Why this is not a second Zack loop

The method's first rule is *"reuse the method, not the piece"*. Every row here is a deliberate
opposite, not a re-skin.

| | Zack at Sa Bubong | Phaister under the tulay |
|---|---|---|
| Hour and light | Dusk, the sun behind him | Night, the moon, shop signs and one sodium lamp |
| Composition | Frontal, a flat horizon behind a centred hero | One-point perspective down the road, the guideway overhead as a diagonal ceiling |
| Editing | Cut-heavy: close-up, impact frames, hand insert, reverse, side tracking, whip pans | **One unbroken shot.** A magician proves there is no trick in the editing; the camera orbits, tilts to the moon and back, and never cuts |
| The hero moment | Power builds, eyes ignite, he throws, TUMP!, he runs for it and is chased | A three-act trick for an audience: she presents the can, misdirects to the moon and the train, vanishes, reappears behind the can, and the can's clang frees the moon |
| Transitions | Flat-colour impact frames, whip pans | Stage language: the passing train is the curtain, her blink's torn rift, the ward stamping down |
| Light as direction | The whole set lit by the sunset | A follow spot on a dark street: the light says whose moment it is |
| Power at rest | Sparks at his fists | A faint doodled sigil fading off her fingertip |
| Idle personality | Tsinelas tricks: toss, flip, strap spin, a static shake | Showmanship: a hat tip to us, a look at the moon, a false opening (turns away down the road, then a sly glance back), and she does not play with her slipper at all |
| Sound word | TUMP! from the throw | KLANG! from the can, the noise that makes the serpent let go |

## 3 · The loop (30 fps, 1260 frames, 42.0 s)

`src/phaister/beats.ts` is the timetable; if this table and that file disagree, the file is right.

| Time (s) | Beat | Act | What happens |
|---|---|---|---|
| 0.0 to 3.0 | | calm | The hub arrives. She idles by the can: weight, cape, a doodled sigil fading |
| 3.0 | `present` | Pledge | She steps to the can and presents it to us, palm open: an ordinary tin |
| 4.8 | `rap` | Pledge | She raps its lid twice: see, solid |
| 6.2 | `gaze` | Pledge | She looks up past us at the moon; the camera tilts up with her eyes |
| 7.2 | `bite` | Turn | A shadow starts across the moon; the street dims toward plum |
| 8.4 | `sigil` | Turn | She draws her sigil the long way round, the camera orbits her; her eyes kindle orchid |
| 10.6 | `train` | Turn | A train's lamp appears down the guideway; she conducts it in with one hand |
| 11.8 | `curtain` | Turn | The train passes overhead, window light strobing across the street; under it she folds inward and is gone in a curl of smoke; the ring drops onto the chalk round the can |
| 12.6 | `empty` | Turn | The stage is empty, the moon swallowed. Held |
| 13.6 | `reveal` | Prestige | She is behind the can, front-on, arms open, held for the room |
| 14.6 | `klang` | Prestige | A flick; the ring flares, the can jumps and clatters down: KLANG! |
| 15.0 | `release` | Prestige | The noise does it: the shadow lets the moon go and the light comes back |
| 16.0 | `bow` | Prestige | Her bow |
| 17.6 | `home` | calm | She blinks back to her mark (no flourish); the can rolls upright into its ring |
| 23.0 | `hat` | calm | She tips her hat to us |
| 27.5 | `moon` | calm | She looks up at the moon for a while |
| 31.5 | `train2` | calm | An ordinary train passes; she holds her hat against its draft |
| 35.5 | `tease` | calm | The false opening: she turns away down the road, then glances back at us over her shoulder |

## 4 · Safe areas

Same hub, same rules as `README.md` § 5: face, can, sigil and KLANG! inside x 240 to 1680 and
y 150 to 948; left column, top band and the mode card and PLAY area stay quiet. The guideway's
dark underside sits under the top band and the left column on purpose.

Sound cues, if the hub wants them: rap 4.8 s, bite 7.2 s, train lamp 10.6 s, train overhead
11.8 s, reveal 13.6 s, KLANG! 14.6 s, moon released 15.0 s, blink home 17.6 s, train 31.5 s.

## 5 · How it was made, reviewed and checked

**Files.** `src/phaister/`: `beats.ts` (the timetable), `view.ts` (the hand-projected camera: the set
is authored in metres so a one-shot camera can orbit, tilt, dolly and whip without a cut),
`set.tsx` and `life.tsx` (the street), `perform.ts` (her performance, the can, the stage state),
`camera.ts`, `fx.tsx` (ward, rift, corona, KLANG!, light), `PhaisterScene.tsx`, and `Board.tsx`
(her test board). `src/three/actor.tsx` gained her palette, a per-model eye band, a per-hero eye
colour, game props (`lata_metal.obj`, KALAWANG) and `measureActor`, which finds her hand on screen
without rendering so her sigils follow the path her hand really took.

**The review loop, as it went.** Every row is a fault found on a contact sheet or a frame and fixed:

| Seen | Fix |
|---|---|
| The moon hidden behind a roofline; buildings too tall; the near pier a flat brown wall | Two and three storeys, the moon moved into the gap, pier moved back and weathered |
| Her hat a black slab whenever she bent, leaned back or bowed | Lens lowered to 1.35 m; every lean kept under about 20 degrees; the bow became a curtsey |
| The ward hung over her face for the whole Turn | It hangs beside her cheek, where her hand draws it |
| KLANG! lettered over her, the can flying across her face | KLANG! to the can's left, the can flies left |
| Arms raised past 45 degrees disappeared behind her hair (test board) | Every raised pose is a V to the sides |
| No train visible at all | The guideway moved 5.3 m off the lens, the train re-timed to clear her hat, window light added on the road |
| An empty street next to Zack's full roof (🧑: *"atleast same level or EVEN better"*) | Lamps under the guideway and their pools, string lights, a jeepney, the pares cart with steam, the pisonet, puddles, pier weather, a barangay tarpaulin, moths |
| A still camera (🧑: *"use dynamic movement"*) | Arcing drift, a crane on her gaze, orbit, a dutched dolly into her face with a punch as her eyes kindle, a whip-tilt to the train, whip-pans with motion blur, a KLANG! punch |

**Checks.** `python scripts/smooth.py` on the half-res draft flagged only the eyes-kindle punch
(frames 296 to 297, intended, then lengthened from 6 to 10 frames); on the 1080p master it flags
only frame 295, the same intended punch. The seam (last frame against the first) differs by 1.4 grey
levels against a median of 1.1 between neighbouring frames.

**Shipped.** `Resources/UI/home/phaister-home-loop.mp4` at crf 22 (33.9 MB, SSIM 0.974 against
the crf 17 master; crf 21 was 41.2 MB, over the budget) and `phaister-home-poster.png` (frame 0).
`HubSceneVideoTests` in PlayMode, 2026-09-24: 5 of 5 passed (`Logs/homescene-phaister.xml`),
including Phaister's loop preparing and advancing in the real hub and the pick reaching both heroes
and falling back to Zack. The in-hub captures at 1920x1080 and 1600x680 were inspected: she stands
in the gap between the left column and the mode card, and nothing busy sits under a button.

**Not yet claimed.** The owner's taste verdict on the loop, and its look on a phone build.

Render: `npm run refs`, `npm run draft:phaister`, `npm run master:phaister`, `npm run ship:phaister`.
