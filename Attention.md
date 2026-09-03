# Attention: the things only a person can do

**This file is the list of work that is blocked on a human, and nothing else.** If an item can be
built, measured, rendered or tested from a command line, it does not belong here: it belongs in
[`docs/TODO.md`](docs/TODO.md) and it gets done.

Read [`CLAUDE.md`](CLAUDE.md) § 2.1 for why the split matters. *"Do not hand work back that you
could do yourself. Scenes can be built from code, matches can be run and measured headlessly, and
compilation, tests, probes, renders and builds all run from the command line. Hand back only a
human judgement ('does this FEEL right', 'is this the art we want') or a credential."*

Every item below says **what is already done**, so nobody redoes it, and **what exactly is being
asked of you**, so it is one sitting rather than a project.

Last reviewed 2026-09-03, branch `abilities-rework`.

---

## 1 · Watch a phone and a PC actually join each other

**`docs/TODO.md` § 130.8. This is the only unverified claim in the crossplay work.**

**Already done, so do not redo it:** the architecture was checked rather than assumed
(§ 130.1: `ApproveConnection` reads the protocol, capacity and the block list, and nothing about a
device goes on the wire). Two real phone-side defects were found and fixed: a failed sign-in cached
for the life of the process (§ 130.2) and `Shader.WarmupAllShaders()` ANR-ing the app before it
reached the menu (§ 130.5). Casual quick match was banded by platform so a phone and a PC could
never meet through the front door, and it is one crossplay pool now (§ 130.4).

**What is asked of you:** open the Windows player on the PC and the .apk on the handset, host on
one, and join with the code on the other. Play one round. That is the whole test.

> ⚠️ **The emulator cannot answer this and it is not worth trying.** The AVD is NAT'd onto
> `10.0.2.x` and can never receive the host's LAN broadcast. **Use the relay join code**, which is
> the path that works across networks anyway.

> ⚠️ **Both players must come from the same commit.** `NetSession.ProtocolVersion` moved this
> session; if the two builds on your machines are from different commits they will refuse each
> other **correctly**, and it reads exactly like a bug. Check the corner of both screens: they
> print the same version.

---

## 2 · Look at the taya's floor ring in greyscale and say whether it reads

**`docs/TODO.md` § 127.3, Phase 16.1.**

**Already done:** the marker is a **shape** rather than a second colour, which is the accessible
answer: the taya gets a RING and an attacker gets a DISC, so the two survive a photograph and a
colourblind player. The frame is taken and measured, not described:
`Logs/shots-play/role-markers-v1.png` and `-grey.png`. The ring is **1,909 px**, at Rec. 601
luminance **101** against asphalt near **60**. `scratchpad/greyscale.py` re-runs the measurement.

**What is asked of you:** look at the grey frame and say whether you can tell the taya from an
attacker at a glance. That is an eye, not a number, and the number cannot settle it.

> ⚠️ **A witness camera framing the LOCAL player photographs no marker at all, and that is not a
> bug.** `CharacterNameplate` hides the ring entirely for the local first-person body, deliberately.
> Judge it on another seat.

---

## 3 · Decide the caption size

**`docs/TODO.md` § 121.8, and it is the last thing holding `AspectRatioProbes` red (§ 130.15).**

**Already done:** the walk that entry asked for. **33 sites** use `PaperKit.Caption`, of which
**6 restate a value that is already on screen above them** and **11 are the only place a fact
appears at all**. `MenuKit.MinReadableUnits` is 18 and `PaperKit.Caption` is 16, and both files
state their number as a decision rather than an accident.

**The recommendation is a SECOND constant for the eleven, not raising the one.** Raising
`PaperKit.Caption` grows every caption in the front end by an eighth, including the six that are
restating something the player can already read.

**What is asked of you:** open a build, look at a caption that is the only carrier of its fact
(the door captions on the character screen are the clearest), and say whether 16 is too small.

> ⚠️ **Do not let anybody change the constant without you**, and do not let anybody lower the
> probe's floor to make the red go away. § 126.13 says so in as many words.

---

## 4 · Accounts, money and the trailer

**`docs/FUTURE.md` Phase 18.** None of this is code.

- **itch.io** page: the account, the page copy, the screenshots, the build upload.
- **Discord**: the server, or the decision not to have one.
- **Steam**: the $100 Direct fee, and whether it is being spent at all this year.
- **The trailer's edit.** Footage can be captured headlessly and the game can be driven to any
  frame you want; **which shots and in what order is yours.**

---

## 5 · Two things a probe cannot see, that need you at a launch

### 5.1 The door into the hub

**`docs/TODO.md` § 96.** You commissioned the player hub and then could not find the way into it.
`UiClickProbe.EveryButtonIsReachable` can prove nothing is covering the door and has caught new
chrome blocking a screen three times. **It cannot tell anybody that a door nobody looks at is a
door nobody finds.**

**What is asked of you:** launch the game, and without being told where it is, get to your profile.
Say out loud what you expected to press. That sentence is the fix.

### 5.2 The two lobby controls you reported dead

**`docs/TODO.md` § 72.** You reported two lobby controls doing nothing. Every headless check says
both are alive and wired, which means either the report is about a state the checks do not build,
or the controls are covered by something at your window shape.

**What is asked of you:** if it happens again, say **which two**, and what the screen looked like
(fullscreen or windowed, in a lobby or in practice). One screenshot closes it.

---

## 6 · Run the UGS suite from the editor

**`UgsServicesProbe`, eight cases.** They ask a live service whether it answers: does Relay
allocate, does Lobby create, do the four cloud endpoints reply.

**They cannot be run headlessly and that is now recorded in the code rather than in a comment.**
`NetIdentity` refuses UGS sign-in in batch mode by design (no display, no Hub session token), so
in a `-batchmode` run the suite reports **SKIPPED with its reason** instead of eight false reds.

**What is asked of you:** when a relink happens or online play is ever suspected, open Unity, open
**Window > General > Test Runner**, switch to **PlayMode**, and run `UgsServicesProbe`. It needs a
person in front of it because it needs a signed-in editor.

⚠️ **And there is one thing to look at while you are there.** In the 2026-09-03 headless run,
`LobbyCreatesAndIsCleanedUp` reached the service and came back **`(401) Unauthorized`** rather than
being skipped with the other six. That is the expected answer for a batch-mode run with no session,
so it is probably nothing. **If it says 401 from the editor as well, the project link or the
Lobby service's settings need looking at**, and that is the one failure mode that would break
online play for everybody without breaking LAN for anybody. The UGS project is
`dcf0831e-a5f4-43b4-832e-b687f13a3569`, org `matthewtlabrador`.

---

## 7 · Two licence calls on bought and free art

### 7.1 The control-icon pack

The keyboard and controller glyphs now drawn in the tutorial are **vryell's "Controllers and
Keyboard"**, which you bought. The sheets are recoloured into this game's warm palette on the way
in (`tools/build_input_glyphs.py`), because the pack ships in blue and navy and `CLAUDE.md` § 6.4
forbids both in any UI layer.

**What is asked of you:** decide whether the recoloured sheets may be committed to this repository.
`DOST-GameDEV/TumbangPreso-Unity` is on GitHub, and a bought pack's art in a public repo is a
redistribution question rather than a use question. Shipping them inside the compiled player is
certainly fine; committing the source PNGs may not be. **The raw pack is deliberately NOT
committed** (`scratchpad/input-icons/` is gitignored); only the recoloured derivatives are.

### 7.2 Whatever the asset hunt comes back with

Every asset that arrives from the search brief needs its **licence read by a person before it goes
in**, and the credits line written. Free-for-commercial-use is the bar; CC-BY needs the attribution
line actually added to the credits screen.

⚠️ **THE FIRST BATCH IS IN AND IT IS ENTIRELY CC0, SO THERE IS NOTHING TO ADD TO THE CREDITS
SCREEN YET.** `Assets/TumbangPreso/Resources/Vfx/SOURCES.txt` carries the licence for all twelve
ability sheets beside the art, and the twenty-four sourced sound cues that remain are Kenney CC0.
Three replacements were rejected by ear and their preferred old WAVs are restored (§ 13). **No CC BY
asset ships today.** The one that would is the jeepney in § 11.2, and its credit line is written
and waiting in `Asset_Sourcing.md` § 9. **What is still asked of you is to read the two source
lines in `SOURCES.txt` and say the derivatives may stay in a public repo**, which is the same
question § 7.1 asks about the glyphs and is a far easier yes: CC0 is a public domain dedication.

---

## 8 · The judgements no probe in this repository can make

These are standing, not one-off. `CLAUDE.md` § 6.2a: **a green layout probe is not a good screen.**

- **Does a screen READ.** Seven readability faults were true at once while every probe was green.
  Renders land in `Logs/shots-runtime/` and `Logs/ui/`; the probe asks whether the screen is a
  screen, the picture asks whether it can be read.
- **Is a model finished.** The voxel cast is being built character by character
  (`docs/Port_Plan.md` § 8 is the replacement queue). Ask before treating any of it as disposable.
- **Does the game FEEL right.** Balance numbers are asserted in a second; whether a shove feels
  like a shove is a controller in your hands.

---

## 9 · Recording that has to be done by people, not sourced

**The six heroes' voice lines.** `AudioCues` reserves `hero_<name>_grunt` and `hero_<name>_ult` for
DANTE, CHESKA, SEAN, ZACK, NEMU and PHAISTER, plus `sfx_ult_theme_*`. The asset pass began with 117
sound effects synthesised by `generate_hero_audio.py`; twenty-four sourced replacements remain
today, while three were restored to their preferred pre-pass versions after a played comparison.

Most of the 117 can be replaced from free libraries. **The Tagalog callouts cannot**, and they are
the ones that make the game sound like it is from here. That is a microphone and an afternoon.

---

## 10 · The one measurement that needs a handset

**Phase 15, the 30 FPS cap and the thermal and battery settings.** The option, its bounds and its
effect are all assertable headlessly and are being built. **What cannot be measured on this machine
is what it does to a real phone's frame time, temperature and battery over twenty minutes.**

**What is asked of you:** once the .apk is on the handset, play two ten-minute sessions, one with
the cap on and one off, and say whether the phone gets hot and whether the battery moves
differently. A number from the desktop is not that measurement.

---

## 11 · Two downloads that need an account, and neither can be worked around

**`docs/TODO.md` § 131.5 and § 131.6.** The asset pass got everything that is fetchable without a
login and stopped at the two that are not. Both are named in `docs/Asset_Sourcing.md` and both are
free; what they need is a person who is signed in.

**Already done, so do not redo it:** every CC0 source in `Asset_Sourcing.md` § 2 and § 5.1 is
downloading automatically through `tools/fetch_asset_sources.py`, including PVFX Foundry, which
needed itch.io's three-request name-your-own-price flow reverse engineered. Twelve recoloured VFX
sheets and twenty-four sourced sound replacements are in; three preferred old cues are restored.

### 11.1 The sixteen Freesound recordings

`Asset_Sourcing.md` § 5.2 lists sixteen CC0 recordings by direct link: the fire whoosh, three ice
takes, two thunder takes, two earthquake takes, the electric crackle, the dark magic loop and the
tin can. **Every one of those URLs answers `302` to `https://freesound.org/home/login/`.** They
are the elemental beds for all eighteen `sfx_cast_*` and all twelve `sfx_var_*` cues, which is why
those thirty are still synthesised while the twenty-four accepted physical/UI replacements are
sourced recordings.

**What is asked of you:** sign in to Freesound, download the sixteen files in § 5.2, and drop them
into `scratchpad/asset-src/freesound/`. That folder is gitignored and
`tools/build_ability_audio.py` is where the mapping goes. They are CC0, so no credit line is
needed and they may be committed as derivatives.

### 11.2 The jeepney

`Asset_Sourcing.md` § 7 and § 7.1. Maclin Macalindong's CC BY jeepney is the culturally specific
silhouette meant to replace the distant north `van` on Ilalim ng Tulay. **Sketchfab requires a
signed-in account to download a model**, and the API needs a token from one.

**What is asked of you:** download the .glb or .fbx and drop it into
`scratchpad/asset-src/sketchfab/`. It is **74.2K triangles as delivered**, so it does not enter the
map as it comes: the optimise, material merge and warm recolour are all scriptable and are the
part that does not need you. **It is CC BY, so the credit line in `Asset_Sourcing.md` § 9 has to
reach the credits screen in the same commit that ships the model.**

> ⚠️ **Neither of these is a blocker on anything else.** The rest of § 131 is done or open on work
> that needs no account. If neither download ever happens, the honest fallback is that the thirty
> elemental cues stay synthesised and Ilalim ng Tulay keeps its generic van, and both should then
> be written off in `Asset_Sourcing.md` rather than left looking pending.

---

## 12 · The work-in-progress logo, and the font call

**`docs/TODO.md` § 133.** 🧑 2026-09-03: *"I think darumadrop can be our main font, in next chat
ask it to use a font that would fit with darumadrop as well as overhaul the ui of everything in
lobby as well as login, with this work in progress logo which i will attach"*.

**Already done, so do not redo it:** the diagnosis. `DarumadropOne-Regular.ttf` ships one weight,
so every `FontStyle.Bold` in the front end is legacy `Text` faking it by drawing each glyph twice
at an offset, and it is worst at the 18-unit floor where most of the game's words live. § 132.8 has
the render that proves it: in one 2x frame the non-bold body is crisp and the bold caption above it
is smeared. The TAB tray is swept; the other forty sites are not.

✅ **BOTH OF THE THINGS THIS SECTION USED TO ASK FOR ARE DONE, 2026-09-03.**

1. ✅ **The logo is in the repository.** You dropped four files in `~/Downloads/claude/` and they
   are committed unchanged under `Assets/TumbangPreso/Art/ui/brand/source/`: the colour logo, the
   mono wordmark, the textured mono wordmark, and the tsinelas-with-a-hit mark.
   `tools/build_brand_art.py` keys the white page to alpha and recolours the mono master per
   screen; `tools/read_brand_palette.py` read the palette straight out of the pixels.
2. ✅ **The font call was delegated and made.** *"u figure out as well what secondary font to
   use"*. It is **Nunito**, SIL OFL 1.1, Regular and Bold.
   `Assets/TumbangPreso/Art/ui/fonts/SOURCES.txt` carries the licence, the three faces that were
   rejected, and the measurements that decided it.

---

**What is asked of you now: one small decision and two looks.**

### 12.1 Confirm one colour, because it is the only one that was not measured

**Khaki.** Your five named swatches are Honey Quartz, Chartreuse, Persimmon, Khaki and Army, and
**four of the five appear as actual fills in the artwork**, so those four were read straight off
the pixels and cross-checked against a second drawing. Khaki appears in neither, because the logo
never needed a quiet mid-tone.

It is `#E8C77E` today, which is Honey Quartz mixed 72:28 toward Army, the two swatches it sits
between on your own strip. Ink measures 9.2:1 on it, so it works. It may simply not be your colour.

**What is asked of you:** the FIRST logo you sent had two swatch strips beside it with the hexes
printed on them. **Drop that image into `Assets/TumbangPreso/Art/ui/brand/source/`** and the real
value gets read off it in one command. If you would rather not bother, say so and `#E8C77E` stands.

> ⚠️ **Nothing is blocked on this.** It is one constant, `UiTheme.BrandKhaki`, and every surface
> that uses it names the constant rather than the hex.

### 12.2 Say whether 16 is still too small, because it is now the ONLY thing red

⚠️⚠️ **AND IT ACTUALLY IS NOW, WHICH IT WAS NOT WHEN THIS WAS LAST WRITTEN.** `docs/TODO.md`
§ 130.15 has claimed for a while that the character screen's only remaining red is the caption at
16. **It was five labels**, and the probe could only ever name one of them because it asserted
inside a loop and NUnit stops at the first failure. Made to report all five with their lettering,
it turned out three were the ability KEYCAPS authored at 13, which is 8.7 physical pixels at 720p
on the one label in the game that is pure instruction. Those are fixed. **So the question below is
finally the only thing standing between `AspectRatioProbes` and green.**

**`docs/TODO.md` § 121.8, which is also § 3 of this file.** It could not be answered until the font
changed, because `PaperKit.Caption`'s 16 and `MenuKit.MinReadableUnits`' 18 were both measured
against Darumadrop. Nunito's x-height is within 2.2 per cent of Darumadrop's so the sizes should
hold, **but that is arithmetic and you have eyes.**

**What is asked of you:** open a build, find a caption that is the only carrier of its fact (the
door captions on the character screen are clearest), and say whether 16 is too small now.

### 12.3 And the standing one: launch it and try to find your profile

**§ 5.1 of this file and `docs/TODO.md` § 96.** § 133.8 says to ship this pass with the question
**queued rather than answered**, and not to read a green probe as an answer to it. The design moves
the door to an identity chip in the TOP RIGHT carrying your face, which is where every live game
puts it. Whether that works is not something any probe here can decide.

> ⚠️ **NOTHING ELSE ABOUT § 133 IS WAITING ON YOU.** The scope is set (front end only, the in-match
> HUD untouched), the palette is set and now measured, the order is set (lobby, settings, character
> select, login), and the brief is *not overwhelming, easy to look at, quirky like the logo*.
> `docs/Front_End_Design.md` is the whole design written out, including the recurring marks you and
> Paul Andrei asked for.

---

## 13 · Listen to the remaining sourced SFX and name any that should go back

**`docs/Asset_Sourcing.md` § 5.5 and `docs/TODO.md` § 131.5b.** The source pass changed twenty-seven
WAV files. After hearing the build, you rejected the can hit (`lata_impact`), can down
(`lata_knockdown`, also reached by the `can_knockdown` alias) and button hover (`ui_hover`). Their
old WAVs are restored byte-for-byte and the generator is prevented from replacing them again.

The other twenty-four replacements are **provisional rather than approved as one batch**. The full
list and alias targets are in `Asset_Sourcing.md` § 5.5, and every pre-pass file remains recoverable
at `ee8bced^` (`c5b6ff9`).

**What is asked of you:** play normally and name any sound that is worse than the one you remember.
The cue name is enough if you know it; otherwise say the event, such as "landing", "score", "back
button" or "slipper bounce". The next session can identify the target and restore only that file.
Do not ask for or accept a rollback of the whole asset commit, because that would also remove good
VFX and unrelated accepted audio.
