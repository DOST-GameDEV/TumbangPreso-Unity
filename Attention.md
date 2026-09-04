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

Last reviewed 2026-09-04, branch `abilities-rework`.

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

✅ **ANSWERED 2026-09-04: YES.** The recoloured sheets may be committed to this public repository. They already were (`Resources/UI/input/glyphs_key_v1.png` and its siblings), so nothing moves; what changes is that it is a decision on the record rather than an assumption. ⚠️ **The raw pack stays uncommitted** (`scratchpad/input-icons/` is gitignored): the answer was about the derivatives.
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
and waiting in `Asset_Sourcing.md` § 9. ✅ **ANSWERED 2026-09-04: YES.** The CC0 derivatives may stay in the public repository. CC0 is a
public domain dedication, so there is nothing to add to the credits screen for them.

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

## 11 · ✅ DONE 2026-09-04: the two downloads landed and are wired

⚠️⚠️ **BOTH ARRIVED AND BOTH ARE IN THE GAME. NOTHING HERE IS OUTSTANDING.** 🧑 signed in to
Freesound in the session's browser and asked for the sixteen to be fetched from there
(*"i will login to freesound in ur browser and u download everything"*), and uploaded the
jeepney himself. **This section is kept rather than deleted because the credential rule that
produced it has not changed**: a session may not create an account, and the next asset behind a
login is blocked in exactly the same way until a person signs in.

| What | Where it is | State |
|---|---|---|
| The 16 Freesound recordings | `scratchpad/asset-src/freesound/` (gitignored) | ✅ All sixteen, verified against § 5.2's stated format and duration to three decimal places. **42 cues re-sourced** by `tools/build_ability_audio.py`: all 18 `sfx_cast_*`, all 12 `sfx_var_*`, and 12 named elemental cues. Three recordings are downloaded and deliberately unused, each with its reason in that file's `KEPT` |
| The jeepney | `scratchpad/asset-src/sketchfab/jeepney.glb` (gitignored) | ✅ **In the map as delivered**: 74,170 triangles, 17 materials, its own colours. It replaces the distant north `van` on Ilalim ng Tulay exactly as § 7.1 asks. ⚠️ **The first version was decimated to 3,000 and recoloured onto the map palette and was rejected on sight** (*"u ate all its colors and design"*); `CLAUDE.md` § 6.0 is the rule that came out of it. `tools/build_jeepney.py` REFUSES to copy the model unless the CC BY credit is already in `CreditsContent` |

⚠️ **THE OLD INSTRUCTION, KEPT SO THE RULE SURVIVES THE TASK:** a session must not try to fetch
an account-gated download on its own, and must not report one as blocked work. Check the folders;
if the files are there, do the wiring, and if they are not, leave them alone.

| What | Where it lands | What to do once it is there |
|---|---|---|
| The 16 Freesound recordings | `scratchpad/asset-src/freesound/` (gitignored) | `tools/build_ability_audio.py` holds the mapping. They are the elemental beds for all 18 `sfx_cast_*` and 12 `sfx_var_*` cues, which is why those 30 are still synthesised. CC0, no credit line. |
| The jeepney | `scratchpad/asset-src/sketchfab/` (gitignored) | 74.2K triangles as delivered; the optimise, material merge and warm recolour are scriptable. ⚠️⚠️ **CC BY: the credit line in `Asset_Sourcing.md` § 9 must reach the credits screen IN THE SAME COMMIT that ships the model.** |


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

### 12.1 ✅ ANSWERED 2026-09-04: KHAKI IS NOT IN THE ARTWORK AND IS DELETED

🧑 supplied `logo.jpg` and said *"this dont look khaki"*. Re-measuring its flat fills agrees: the
logo holds **six** colours and every one was already a constant.

| Measured | Share | Constant it confirms |
|---|---|---|
| `#980010` | 9.5% | Deep red `#980715` |
| `#F8D098` | 7.6% | Honey Quartz `#FCD39F` |
| `#D0C800` | 5.6% | Chartreuse `#D6CE01` |
| `#F88040` | 1.6% | Persimmon `#FD8041` |
| `#F0B020` | 1.1% | Golden `#F5B521` |
| `#C02808` | 1.1% | Rim red `#C32E0D` |

⚠️ **`BrandKhaki` also had zero call sites**: one mention in the repository, its own definition. A
colour that is invented rather than measured AND draws nothing is a decision waiting to be made
wrongly by whoever reaches for it first, so it is gone. `UiTheme` carries the measurement where the
constant used to be.

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


---

## 14 · Plug in every controller you can find, and say what happened

**`docs/TODO.md` § 138.4 step 4 and § 142.5.** This is the last open step of the controller work
and it is the only one that cannot be done from a command line: **nobody has held a pad against
this project except the one on this desk.** § 138.3 has been blunt about that since it was written,
and the fallback shipped on 2026-09-04 makes it matter more rather than less.

**What is already done, so none of it needs repeating:**

- **A pad Unity recognises works everywhere**, and now includes backing out of a screen (B), the
  pause menu (Start) and the whole front end's focus and prompts.
- **A pad Unity does NOT recognise is no longer dead.** `InputLayer.GenericPadBridge` drives it
  through a guessed button order, and the guess is drawn and rebindable on the new
  **SETTINGS, CONTROLS, CONTROLLER MAP** screen.
- **The guess is asserted in EditMode against a synthetic joystick**, which proves the wiring and
  says nothing at all about whether the button order is right for any real device.

**What is asked of you:** for each controller you can lay hands on, including the cheap ones and
anything with an X/D switch, plug it in, open SETTINGS, CONTROLS, CONTROLLER MAP and answer three
things.

1. **Does the map name your pad, or does it say the game does not recognise it?** The line under
   the drawing says which.
2. **Press each button in turn and say which callouts are wrong.** On a recognised pad they should
   all be right; on an unrecognised one the face buttons are the likely ones, because the
   PlayStation-style families report them in a different order from the Xbox-style ones.
3. **Say what the pad is**, in whatever detail is easy: the name on the box, or the manufacturer
   and product strings the log prints when it is not recognised (search the player log for
   `[Controller]`).

⚠️ **A pad that works is worth reporting too.** One tested pad written down beats four assumed
ones, and the list of what has actually been tried is the whole deliverable here.

⚠️ **If a wheel, a flight stick or an arcade stick makes the game act possessed**, that is the
known cost of the fallback and there is a switch for it: SETTINGS, CONTROLS, **Unrecognised
controllers**, which only appears when such a device is attached. Say if you hit it, because it
means the row needs to be easier to find.

---

## 15 · The tournament rulings ✅ TAKEN 2026-09-04, and the two they created

**`docs/TODO.md` § 143.3 and § 143.9.**

### 15.1 ✅ A BRACKET LOBBY IS NOT PASSWORD-LOCKED

🧑 2026-09-04: *"bracket lobby isnt password locked bcz u should be able to join wtv lobby in
tournaments (yes we host tournaments)"*.

`TournamentPreset.Rules()` sets `Private = false` and an empty password, which is what it already
did. **The difference is that it is now a decision with a reason attached rather than a
placeholder inheriting the shipped default**, and the reason is that an open lobby is how a
bracket is actually run: an operator moves players between stations and nobody should be stopped
by a password nobody wrote down.

### 15.2 ✅ A PLAYER WHO LEAVES MAY REJOIN

🧑 2026-09-04: *"let someone rejoin if they leave"*.

So the ruling is **reconnect, not forfeit**. ⚠️ That settles the question § 140.5 could not answer
from the code (a drop and a quit are the same event on the wire, so the game cannot tell an
accident from an alt-F4) **by making them the same case on purpose**: both may come back, and
nothing has to distinguish them.

---

## 16 · The rejoin follow-ups ✅ ANSWERED 2026-09-04

### 16.1 ✅ A BOT AT THE ABSENT PLAYER'S SKILL LEVEL TAKES THE SEAT

🧑: *"let ai on same skill level as them take over"*.

**So it is the bot option, with the difficulty matched to the player rather than to the lobby.**
`AIController.ApplyDifficulty` and the `Difficulty` tiers already exist, and `BotFill` already
drives an unfilled seat, so the seat-filling half is built. What is not built is the MATCHING: the
game has no notion of "this player's skill level" to hand the bot, and `Rating` is a ladder number
rather than a difficulty tier.

⚠️⚠️ **AND ONE THING FALLS OUT OF THIS THAT NOBODY ASKED FOR AND SOMEBODY WILL NOTICE: a bot can
lose you points you would not have lost, or win you points you did not earn.** `MatchRecord.IsBot`
already marks a bot seat, but a seat that was HUMAN and then became a bot part way through is
neither, and the career line for that match currently has no way to say so. **A rating that counts
a bot's stretch as the player's own is a ladder nobody trusts**, which is the same argument
`docs/TODO.md` § 128 makes about the rating not reading the bot flag. Decide that when the seat
handover is built, not before.

### 16.2 ✅ MEASURED 2026-09-04: A SEATLESS REFEREE RUNS A MATCH TODAY, AND RUNNING ONE FOUND A BUG

🧑: *"thats a real problem, is there any other way to structure lan network matches? host sided
shit might be shitty (maybe if we will rework host logic or replace it lets work on a new branch
called lan rework"*.

**Agreed that it is the real problem, and the answer may already be half built rather than a
rewrite.**

⚠️⚠️ **`NetAuthority.IsSeatlessReferee` ALREADY EXISTS AND IS DOCUMENTED AS NOT A CORNER CASE.**
Its own note: *"A DEDICATED SERVER IS A REFEREE WITH NO SEAT, and that is not a corner case for the
supported Linux server build."* Every host-authoritative path in the game asks `ShouldResolve()`
rather than "am I player 1", and **every point in the game is created in one function**
(`MatchDirector.AddScore`), so a refereeing process that holds no seat needs no new authority
model. That is the whole reason the seam was written that way.

**What that buys at a venue:** the operator's laptop referees the bracket. **No player is the
host, so no player leaving can end a match**, and § 16.1's bot handover covers the seat. The
ruling this section was asking for stops being needed, which is the best kind of answer to a
ruling.

**What it costs, honestly:**

- A fifth process on the LAN, and something has to launch and point players at it.
- ⚠️ **Nobody has ever run one.** `CLAUDE.md` § 4a records the Linux server build as supported and
  `FUTURE.md` notes there is no active deployment, so "it compiles" is the whole of what is known.
- ⚠️ The referee becomes the single point of failure instead of the host. That is **strictly
  better** (it is a machine nobody is playing on, sitting still, on mains power, not being
  alt-tabbed) but it is not zero.

⚠️ **THE BRANCH WAS TO BE `lan-rework` AND IT IS NOT NEEDED.** The instruction was: before any
code, **measure whether a seatless referee actually starts and runs a match today**, because the
architecture claims it can and nothing has ever checked. *"If it does, this is configuration and
a launch path rather than a rework."*

✅ **IT DOES.** `tools/referee_run.py` puts one `-tp-dedicated` process and two `-tp-join`
clients on a real link and compares all three `NetStateReport` files. On `64718d3`:

```
referee   : role HOST    networked True  slot -1  round 1  active True  defender 0  hash 4570D8E8
client1   : role CLIENT  networked True  slot -1  round 1  active True  defender 0  hash 85208A38
client2   : role CLIENT  networked True  slot  0  round 1  active True  defender 0  hash 85208A38
```

**The referee refereed a live round and both clients agreed with it and with each other**: same
defender, same roster, same taya, identical discrete-state hash. So the answer to this section is
the good one: **the operator's laptop referees the bracket, no player is the host, and no player
leaving can end a match.** 🧑 2026-09-04: *"no need to make lan rework branch if it works"*,
*"js push it to main"*, so it did.

⚠️⚠️ **AND THE FIRST RUN FOUND A REAL DEFECT, WHICH IS THE ARGUMENT FOR MEASURING FIRST.**
`client1` came back holding `local slot: -1`, the referee's own value: **the first player to join
a dedicated server was admitted as a spectator with no seat.** `LobbySession` treated peer 1 as
"the server itself" in three places, and NGO's server is peer **0**; peer 1 is the first client.
Seven existing tests asserted the wrong number and so could never have caught it.
`docs/TODO_Archive.md` § 143.21 has the whole thing. `LobbySession.RefereePeerId` is the constant
now, and `ALanListenHostIsNeverTreatedAsARefereeAndKeepsItsSeat` guards the LAN case, which is
untouched by any of it because a listen host is not `IsDedicated`.

⚠️ **WHAT IS STILL NOT MEASURED:** a referee on a SECOND machine (this run was three processes on
one), and a referee surviving a client dropping and rejoining mid-match. Both are
`tools/referee_run.py` plus a scenario, not new code.


---

## 17 · The 2026-09-05 nationals pass: three rulings and one thing to feel

Everything else in that pass is code, is tested and is in `docs/TODO.md` §§ 145, 146 and 147.
These four cannot be settled by a probe, and each one is written here rather than in the queue for
the reason `docs/TODO.md` says out loud: **a human-only row in the implementation queue is how the
queue stops being read.**

### 17.1 ⚠️⚠️ THE RECONNECT-OR-FORFEIT RULING, WHICH IS STILL NOT TAKEN

`docs/TODO.md` § 143.9 built every piece of deterministic software behaviour around host loss:
the failure is one named path on every peer, a peer that loses its host **stops being able to
decide anything** (`MatchAbandon.AuthorityRevoked`, which closed a real hole where four clients
losing one host became four referees), the reason names host loss rather than "disconnected", and
the player reaches a screen they can act from.

**What no amount of code can answer is what the BRACKET does about it.** § 140.5 already records
that a drop and a quit are the same event on the wire, so this is not a detection problem:

- A match whose HOST drops at round 3 of 4: **replayed from the start, resumed from the score, or
  forfeited?**
- A PLAYER dropping and coming back: the seat is held and a bot at their rating finishes it
  (§ 144.7, built). **Does the returning player get their chair back mid-match, or is the match
  theirs to lose?**
- Is there a time limit on either?

⚠️ **THE CODE DOES NOT PRESUME AN ANSWER AND MUST NOT START.** `SeatHandover.RatingMovesFor`
already refuses to move a ladder on a match somebody was not present for, which is the one part of
this that is a fairness rule rather than a tournament ruling.

⚠️⚠️ **AND THE OPERATOR'S LAPTOP CAN REFEREE**, which changes the shape of the question rather
than answering it: § 16.2 above measured a `-tp-dedicated` process running a real match with two
clients. **On a refereed bracket, no player leaving can end a match at all.** That may make the
ruling above nearly moot for the nationals, and it is a venue decision (one more laptop per
station) rather than a code one.

### 17.2 ⚠️⚠️ THE RETRIEVAL SLIDE HAS TO BE FELT, AND THE NUMBERS ARE DERIVED RATHER THAN FINAL

`docs/TODO.md` § 146. The attacker's right click did **nothing at all** before this, the key, the
pad's left trigger and the touch layer's LUNGE button were inert for three of the four players in
every round, and it is now a committed slide into your own tsinelas.

**Every number is solved from one the game already had** and the arithmetic is in § 146.2: it buys
about **a third of a second** over walking, which is less than one taya decision, and it commits
the body for **exactly the taya's whole punish cycle** so a perfect read can always be cashed in.
Bots use it and the seeded sweep can measure whether it changed anything.

⚠️ **WHAT THAT CANNOT SETTLE IS 🧑'S OWN TEST:** *"I can safely approach and pick this up
normally, OR I can commit to the fast retrieval because I think I can get away with it."* If it
turns out nobody uses it, the recovery is too long; if normal retrieval stops happening, the
commitment is too cheap. **Both are one constant and both are `Attention.md`'s call, not a
probe's.** Play a Classic round as an attacker and say which it is.

⚠️ **IT PLAYS THE LUNGE CLIP** because both moves are a body-led dash and the rig has one. A slide
of its own is art work.

### 17.3 ⚠️ THE `ui_*` DC OFFSET IS STILL YOUR CALL AND IS OFF THE QUEUE

Carried over from § 144.3 rather than new. `ui_click` sits at a DC offset of **-0.121**, which is
a thump on every press of the three most-heard sounds in the game, and the fix is one line that
subtracts the mean. ⚠️ **It rewrites files you asked for back BY NAME** (`CLAUDE.md` § 6), so it
needs your yes and not a commit. It was in the implementation queue and should not have been.

### 17.4 ⚠️ A NATIONALS CERTIFICATION HAS TO BE RUN FROM THE WINDOWS MACHINE

Not a defect and not fixable from here. `tools/qualify.py` resolves Unity and dotnet per machine
now and names its build target in the report rather than assuming `Win64` (§ 145.5), but
`CLAUDE.md` § 7's Mac has **no Windows Standalone module and no dotnet**, so:

- `GameBuilder.BuildWindows` has no target to write there. **The Windows player and the .apk for
  a nationals candidate come off the Windows laptop.**
- `NetSession.ProtocolVersion` moved to **24** on 2026-09-05 (the seat handover's rating,
  § 144.7), so `CLAUDE.md` § 4a applies: **both players are rebuilt from that commit and shipped
  together**, or they refuse each other correctly and it reads as a bug.
