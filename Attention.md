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

Last reviewed 2026-09-03, branch `ui-redesign`.

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
DANTE, CHESKA, SEAN, ZACK, NEMU and PHAISTER, plus `sfx_ult_theme_*`. Every one of the 117 sound
effects in the project is currently **synthesised** by `generate_hero_audio.py` as a placeholder.

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
